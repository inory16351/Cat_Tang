# 새벽의 꿈 마을용 신비한 나무와 발광 소품 8종, 검토용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_mystic.py
"""Self-contained, deterministic Blender 5.3 kit. Metres, Z up, +Y front.

All export materials are opaque, flat grey ROLE placeholders. Exposed hearts,
paper seams and hollow bells make the luminous roles visible without alpha.
The cool palette and absence of loose dust intentionally follow this kit's
overrides of _village_common.md. Only designed stones/lanterns float.
All exports finish before any review-only colours or placements are applied.
"""

import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Village'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_mystic_preview.png'
TAU = math.tau
ROLES = (
    'bark_pale', 'bark_dark', 'leaf_crystal', 'crystal_blue', 'crystal_core',
    'rift_glow', 'moon', 'glass_blue', 'petal_glow', 'rock', 'rock_dark',
    'earth', 'cactus', 'cactus_dark', 'stone', 'stone_dark', 'iron', 'brass',
    'gold', 'clock_face', 'canvas', 'lantern_glow', 'stardust',
)
HARD = {'bark_pale', 'bark_dark', 'rock', 'rock_dark', 'stone', 'stone_dark',
        'moon', 'iron', 'brass', 'gold', 'clock_face'}
PALETTE = {
    'bark_pale': (.73, .78, .81), 'bark_dark': (.25, .22, .32),
    'leaf_crystal': (.42, .70, .77), 'crystal_blue': (.27, .48, .76),
    'crystal_core': (.69, .95, 1.0), 'rift_glow': (.88, 1.0, .98),
    'moon': (.87, .88, .96), 'glass_blue': (.20, .39, .55),
    'petal_glow': (.88, .60, .83), 'rock': (.45, .49, .58),
    'rock_dark': (.22, .26, .36), 'earth': (.31, .24, .26),
    'cactus': (.39, .52, .40), 'cactus_dark': (.16, .30, .28),
    'stone': (.58, .60, .67), 'stone_dark': (.30, .32, .42),
    'iron': (.12, .16, .23), 'brass': (.66, .46, .21),
    'gold': (.96, .72, .33), 'clock_face': (.95, .89, .72),
    'canvas': (.81, .77, .64), 'lantern_glow': (1.0, .77, .40),
    'stardust': (.99, .94, .77),
}
MATS = {}
PARTS = []
CARVINGS = []


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Emission Strength'].default_value = 0
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Batch disconnected solids; face_roles supports actual exposed inlays."""

    def __init__(self, label, verts, faces, role, bevel=False, face_roles=None):
        assert role in ROLES
        self.label = label
        self.verts = [Vector(v) for v in verts]
        self.faces = list(faces)
        self.roles = list(face_roles) if face_roles else [role]*len(faces)
        assert len(self.faces) == len(self.roles)
        assert set(self.roles) <= set(ROLES)
        self.bevel = bevel and role in HARD
        PARTS.append(self)

    def transform(self, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
        matrix = (Matrix.Translation(Vector(loc)) @
                  Euler(rot).to_matrix().to_4x4() @
                  Matrix.Diagonal(Vector((*scale, 1))))
        self.verts = [matrix @ v for v in self.verts]
        return self


def move_group(start, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    for part in PARTS[start:]:
        part.transform(loc, rot, scale)


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s/2 for s in size)
    return Part(label,
                [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                 (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    n = len(polygon)
    verts = [(x,y+d,z) for d in (-depth/2, depth/2) for x,z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1) % n,(j+1) % n+n,j+n) for j in range(n)]
    return Part(label, verts, faces, role, bevel)


def lathe(label, rings, role, n=10, bevel=False):
    verts = [(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),z)
             for z,r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1) % n,(i+1)*n+(j+1) % n,(i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    return Part(label,verts,faces,role,bevel)


def cylinder(label, loc, radius, depth, role, n=10, rot=(0,0,0), bevel=True):
    return lathe(label,[(-depth/2,radius),(depth/2,radius)],role,n,bevel).transform(loc,rot)


def tube(label, points, radii, role, n=7, bevel=False):
    """Tapered polygonal sweep; parallel-transport frame avoids branch twists."""
    points = [Vector(p) for p in points]
    if isinstance(radii,(float,int)):
        radii = [radii]*len(points)
    assert len(points) == len(radii)
    verts = []
    previous = None
    for i,p in enumerate(points):
        tangent = (points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        if previous is None:
            ref = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
            u = ref.cross(tangent).normalized()
        else:
            u = (previous-tangent*previous.dot(tangent)).normalized()
        previous = u
        v = tangent.cross(u)
        verts += [tuple(p+radii[i]*(u*math.cos(j*TAU/n)+v*math.sin(j*TAU/n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1) % n,(i+1)*n+(j+1) % n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n,len(points)*n)))
    return Part(label,verts,faces,role,bevel)


def beam(label, a, b, width, depth, role):
    a,b = Vector(a),Vector(b)
    part = box(label,(0,0,0),(width,depth,(b-a).length),role)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm,subdivisions=detail,radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.94,1.06)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    part = Part(label,[tuple(v.co) for v in bm.verts],
                [tuple(v.index for v in f.verts) for f in bm.faces],role)
    bm.free()
    return part


def hoop(label, loc, radius, band, depth, role, n=16, rot=(0,0,0), bevel=True):
    # Ring axis is Y; front surface is positive Y.
    verts = [(r*math.cos(j*TAU/n),y,r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2),(radius,-depth/2),
                         (radius,depth/2),(radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j,k*n+(j+1) % n,((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label,verts,faces,role,bevel).transform(loc,rot)


def leaf(label, a, b, width, role='cactus_dark'):
    """Thick folded pointed blade, broad enough to read from above."""
    a,b = Vector(a),Vector(b)
    d = b-a
    u = d.cross(Vector((0,0,1)))
    if u.length < .01:
        u = Vector((1,0,0))
    u.normalize()
    mid = a+d*.48
    ridge = mid+Vector((0,0,width*.32))
    verts = [a,mid+u*width/2,b,mid-u*width/2,ridge,
             mid-Vector((0,0,.045))]
    return Part(label,verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),
                             (1,0,5),(2,1,5),(3,2,5),(0,3,5)],role)


def crystal(label, a, b, radius, role='crystal_blue'):
    """Six-sided prism with top/forward exposed core facets, never hidden inside."""
    a,b = Vector(a),Vector(b)
    length = (b-a).length
    n = 6
    verts = [(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),z)
             for z,r in ((0,radius*.7),(length*.68,radius)) for j in range(n)]
    verts.append((0,0,length))
    faces = [tuple(reversed(range(n)))]
    faces += [(j,(j+1) % n,(j+1) % n+n,j+n) for j in range(n)]
    faces += [(j+n,(j+1) % n+n,12) for j in range(n)]
    roles = [role]*len(faces)
    roles[2] = 'crystal_core'  # one broad longitudinal facet
    roles[8] = 'crystal_core'
    part = Part(label,verts,faces,role,face_roles=roles)
    matrix = Matrix.Translation(a) @ Vector((0,0,1)).rotation_difference(
        (b-a).normalized()).to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def moss_rock(width, depth, height=.32):
    lump('Low faceted earth bed',(0,0,height*.45),(width,depth,height),'earth',2)
    for x,y,s in ((-.29,.18,.53),(.27,-.24,.57),(.25,.28,.42)):
        lump('Raised moss cushion',(x*width,y*depth,height*.85),
             (s*width,s*depth,height*.38),'cactus',1)
    for j in range(5):
        a = j*TAU/5+.24
        lump('Perimeter stone',(width*.40*math.cos(a),depth*.40*math.sin(a),height*.30),
             (.23,.21,.19),'rock' if j % 2 else 'rock_dark')


def clock_fruit(loc, radius=.28):
    x,y,z = loc
    cylinder('Clock enamel fruit',loc,radius*.85,.10,'clock_face',n=12,rot=(math.pi/2,0,0))
    hoop('Old brass watch bezel',(x,y+.035,z),radius,.065,.13,'brass',n=12)
    for j in range(4):
        a = j*TAU/4
        box('Clock hour stud',(x+radius*.61*math.sin(a),y+.10,z+radius*.61*math.cos(a)),
            (.05,.045,.065),'gold',rot=(0,a,0))
    beam('Frozen minute',(x,y+.14,z),(x-radius*.35,y+.14,z+radius*.48),.045,.045,'iron')
    beam('Frozen hour',(x,y+.15,z),(x+radius*.44,y+.15,z+.035),.05,.045,'iron')
    hoop('Watch chain bail',(x,y,z+radius+.04),.085,.04,.065,'brass',n=8,bevel=False)


def chain(label, a, b):
    """Readable 5 cm chain spine and a few large alternating links."""
    tube(label,[a,b],.025,'iron',n=5)
    a,b = Vector(a),Vector(b)
    count = max(2,round((b-a).length/.25))
    for j in range(count):
        p = a.lerp(b,(j+.5)/count)
        hoop(label+' oval link',(0,0,0),.065,.04,.045,'brass',n=5,
             rot=(0,0,math.pi/2*(j % 2)),bevel=False).transform(
                 scale=(.8,1,1.2)).transform(p)


def crown_pad(loc, size, angle=0):
    """Flat faceted leaf rosette with a visible luminous centre on its top."""
    n = 8
    rings = [(-.16,.64),(0,1),(.25,.52),(.29,.27)]
    verts = [(size[0]*r*.5*math.cos(j*TAU/n),size[1]*r*.5*math.sin(j*TAU/n),z)
             for z,r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1) % n,(i+1)*n+(j+1) % n,(i+1)*n+j)
              for i in range(3) for j in range(n)]
    faces.append(tuple(range(3*n,4*n)))
    roles = ['leaf_crystal']*len(faces)
    roles[-1] = 'crystal_core'
    # The inner crown facets expose the heart from the steep review camera.
    for j in range(n):
        roles[1+2*n+j] = 'crystal_core'
    Part('Crystal leaf cluster',verts,faces,'leaf_crystal',face_roles=roles).transform(loc,(0,0,angle))
    x,y,z = loc
    for j in range(3):
        a = angle+j*TAU/3
        leaf('Broad pointed crystal leaf',(x,y,z+.10),
             (x+size[0]*.57*math.cos(a),y+size[1]*.57*math.sin(a),z+.04),
             .48,'leaf_crystal')


def dream_tree_crystal():
    moss_rock(2.7,2.6,.25)
    # One strong twisted bole plus a second climbing root/ridge; no thin wire tree.
    points = []
    for j in range(11):
        t = j/10
        a = -.6+t*TAU*1.12
        points.append((.28*math.sin(a),.21*math.cos(a),.20+5.15*t))
    tube('Ancient spiralling pale bole',points,[.55-.30*j/10 for j in range(11)],
         'bark_pale',n=9,bevel=True)
    winding = []
    for j in range(16):
        t = j/15
        a = -.3+t*TAU*1.25
        winding.append((.39*math.cos(a),.36*math.sin(a),.27+t*4.9))
    tube('Corkscrew raised bark ridge',winding,[.19-.10*j/15 for j in range(16)],
         'bark_pale',n=6,bevel=True)
    for j in range(7):
        a = j*TAU/7
        end = Vector((1.75*math.cos(a),1.65*math.sin(a),.11))
        tube('Lifted arching buttress root',[(0,0,.60),
             (.68*math.cos(a),.64*math.sin(a),.82),
             (1.25*math.cos(a),1.18*math.sin(a),.51),end],
             [.24,.19,.11,.045],'bark_pale',n=6,bevel=True)
        if j % 2 == 0:
            p = (1.24*math.cos(a+.23),1.20*math.sin(a+.23),.48+.10*(j % 3))
            lump('Designed floating root stone',p,(.39,.34,.36),'rock')
            crystal('Root stone luminous seam',Vector(p)+Vector((0,.13,-.07)),
                    Vector(p)+Vector((.05,.16,.16)),.065)
    # Three broad lower scars split the silver silhouette into readable values.
    for z,x in ((1.25,-.18),(2.45,.15),(3.5,-.06)):
        leaf('Deep old trunk scar',(x,.38,z-.28),(x+.12,.36,z+.36),.16,'bark_dark')
    pads = [(-1.42,-.90,5.34,1.8,1.6),(.05,-1.4,5.80,2.0,1.6),
            (1.42,-.65,5.43,1.85,1.8),(-1.48,.68,5.72,1.9,1.7),
            (.10,.38,6.22,2.1,1.9),(1.36,.91,5.91,1.9,1.6),
            (-.38,1.52,5.35,1.9,1.35)]
    for j,(x,y,z,w,d) in enumerate(pads):
        tube('Great spreading crown limb',[(.05,0,3.35+j*.07),
             (x*.52,y*.52,4.72),(x,y,z-.10)], [.22,.15,.07],
             'bark_pale',n=6,bevel=True)
        crown_pad((x,y,z),(w,d),j*.7)
        if j in (0,2,5):
            anchor = Vector((x*.85,y*.85,4.97))
            fruit = Vector((x*.91,y*.85+.10,3.77+.13*j))
            chain('Pocket watch fruit chain',anchor,fruit+Vector((0,0,.37)))
            clock_fruit(fruit,.29)
        # Caught stars intersect a branch: no detached particle pass.
        p = Vector((x*.52,y*.52,4.72))
        lump('Stardust knot fused to branch',p,(.19,.19,.23),'stardust')
        if j in (0,3,5):
            lump('Stardust caught in crown fork',(x+.15,y+.20,z+.23),
                 (.18,.18,.20),'stardust')
    crystal('Half dissolved crown splinter',(.30,.10,5.95),(.55,.04,6.91),.18,'leaf_crystal')


def lantern(loc, radius=.25, paper=True):
    """Four broad paper lobes separated by open seams showing a luminous body."""
    start = len(PARTS)
    r = radius
    if paper:
        rings = [(-1.08*r,.27*r),(-.48*r,.94*r),(.48*r,.94*r),(1.08*r,.27*r)]
        lathe('Exposed lantern glow core',[(z,rad*.83) for z,rad in rings],
              'lantern_glow',n=8)
        for j in range(4):
            angles = [j*TAU/4 + t*math.radians(52)/2 for t in (-1,0,1)]
            verts = [(rad*math.cos(a),rad*math.sin(a),z)
                     for inset in (0,.04) for z,rad0 in rings
                     for a in angles for rad in (max(.025,rad0-inset),)]
            stride = len(rings)*3
            faces = []
            for k in range(len(rings)-1):
                for a in range(2):
                    i = k*3+a
                    faces += [(i,i+1,i+4,i+3),
                              (stride+i+3,stride+i+4,stride+i+1,stride+i)]
                for a in (0,2):
                    i = k*3+a
                    faces.append((i,i+3,stride+i+3,stride+i))
            for k in (0,(len(rings)-1)*3):
                for a in range(2):
                    i = k+a
                    faces.append((i,stride+i,stride+i+1,i+1))
            Part('Round paper shell lobe',verts,faces,'canvas')
    else:
        lathe('Old lantern open flame', [(-r,.22*r),(-.65*r,.64*r),
              (.65*r,.64*r),(r,.20*r)], 'lantern_glow',n=8)
        for j in range(4):
            a = j*TAU/4+.4
            x,y = r*.77*math.cos(a),r*.77*math.sin(a)
            tube('Lantern cage upright',[(x,y,-r),(x,y,r)],.028,'brass',n=5)
    for sign in (-1,1):
        lathe('Antique brass lantern cap',[(sign*r*.96,r*.43),
              (sign*r*1.26,r*.20)],'brass',n=10,bevel=True)
    hoop('Lantern suspension eye',(0,0,r*1.36),.072,.04,.05,'brass',n=6,bevel=False)
    if paper:
        tube('Short ceremonial tassel',[(0,0,-r*1.20),(0,0,-r*1.62)],.035,'iron',n=5)
        lump('Luminous tassel weight',(0,0,-r*1.67),(.10,.10,.15),'lantern_glow')
    move_group(start,loc)


def dream_tree_willow():
    # The pool is opaque geometry with a glow channel, rather than an emissive
    # glass material: glass_blue is a body role, rift_glow supplies the light.
    lump('Pool dark stone rim',(0,.27,.12),(4.6,4.25,.26),'rock_dark',2)
    lathe('Still glass pool',[(-.04,1),(.04,1.05)],'glass_blue',n=18).transform(
        (0,.34,.22),scale=(2.05,1.75,1))
    hoop('Pool pale luminous ring',(.08,.36,.273),1.17,.055,.045,
         'rift_glow',n=20,rot=(math.pi/2,0,0),bevel=False).transform(scale=(1.45,1.12,1))
    for j in range(5):
        a = -.15+j*.63
        lump('Moon stepping stone',(1.42*math.cos(a),.36+1.25*math.sin(a),.32),
             (.56,.46,.20),'moon',1)
    trunk = [(-.30,-.25,.22),(-.62,-.38,1.1),(-.18,-.38,2.3),
             (-.42,-.12,3.4),(-.12,-.15,4.25),(.12,0,5.45)]
    tube('Gnarled dark willow trunk',trunk,[.55,.53,.40,.32,.23,.09],
         'bark_dark',n=9,bevel=True)
    for j in range(6):
        a = j*TAU/6+.4
        tube('Roots sinking into glass pool',[(-.30,-.25,.66),
             (.78*math.cos(a),.73*math.sin(a),.48),
             (1.35*math.cos(a),1.22*math.sin(a),.20)],
             [.21,.12,.055],'bark_dark',n=6,bevel=True)
    for j in range(8):
        a = j*TAU/8
        x,y = 1.65*math.cos(a),1.65*math.sin(a)
        z = 5.18+.30*math.sin(a*2)
        tube('Arched willow bough',[(-.12,0,3.65),
             (x*.55,y*.55,5.30),(x,y,z)], [.22,.13,.055],
             'bark_dark',n=6,bevel=True)
        leaf('Moon-silver split bark',(x*.40,y*.40,4.51),
             (x*.63,y*.63,5.10),.12,'bark_pale')
        for k in range(3):
            angle = a+(k-1)*.15
            xx,yy = (1.70+.13*k)*math.cos(angle),(1.70+.13*k)*math.sin(angle)
            bottom = .48+.32*((j+k) % 4)
            # Connected ribbon spine supports all beads. Different end heights
            # and curved tips preserve spaces through the willow's curtain.
            points = [(x*.93,y*.93,z), (xx,yy,z-.85),
                      (xx*1.06,yy*1.06,(z+bottom)*.5),
                      (xx*.95,yy*.95,bottom+.20),(xx*.88,yy*.88,bottom)]
            role = 'rift_glow' if (j+k) % 3 else 'petal_glow'
            tube('Drooping continuous luminous ribbon',points,[.045,.05,.043,.038,.03],role,n=4)
            for m in range(5):
                t = (m+.6)/5*(len(points)-1)
                index = min(int(t),len(points)-2)
                p = Vector(points[index]).lerp(Vector(points[index+1]),t-index)
                lump('Strand attached glowing bead',p,
                     (.17,.17,.25), 'petal_glow' if (j+k+m) % 3 == 0 else 'rift_glow')
            leaf('Willow crystalline shoulder leaf',(x*.87,y*.87,z),
                 (xx*1.12,yy*1.12,z-.40),.22,'leaf_crystal')
    tube('Lantern branch',[(-.3,-.1,3.1),(.50,.65,3.9),(1.0,1.2,3.95)],
         [.17,.11,.05],'bark_dark',n=6,bevel=True)
    chain('Old willow lantern chain',(1.0,1.2,3.95),(1.0,1.2,3.37))
    lantern((1.0,1.2,2.92),.31,paper=False)
    # Ancient knot has a pale inner rim and an actual dark hollow centre.
    hoop('Willow age knot',(-.26,.10,2.05),.28,.08,.12,'bark_pale',n=10)


def sapling(loc=(0,0,0), scale=1):
    start = len(PARTS)
    tube('Young curved silver trunk',[(0,0,0),(-.12,0,.66),(.08,.02,1.33),
         (.02,0,2.08)],[.15,.12,.085,.04],'bark_pale',n=7,bevel=True)
    for j,(x,y,z) in enumerate(((-.61,-.10,1.38),(.55,-.31,1.67),
                              (-.37,.44,1.94),(.53,.44,1.24),(.04,0,2.30))):
        tube('Bulb bearing sapling branch',[(0,0,.70+j*.13),
             (x*.8,y*.8,z-.28),(x,y,z)], [.085,.055,.025],
             'bark_pale',n=5,bevel=True)
        lathe('Bud dark calyx',[(-.08,.10),(.05,.19),(.13,.10)],
              'cactus_dark',n=6).transform((x,y,z))
        lump('Round luminous dream bulb',(x,y,z+.17),(.44,.43,.49),
             'petal_glow' if j % 2 else 'crystal_core',2)
        leaf('Sapling folded leaf',(x*.7,y*.7,z-.23),(x*1.3+.13,y*1.2,z-.10),
             .21,'cactus_dark')
    for j in range(4):
        a = j*TAU/4
        tube('Young exposed root',[(0,0,.18),(.24*math.cos(a),.24*math.sin(a),.10),
             (.43*math.cos(a),.43*math.sin(a),.025)],[.09,.06,.025],
             'bark_pale',n=5,bevel=True)
    move_group(start,loc,scale=(scale,scale,scale))


def dream_tree_small():
    lump('Mossy sapling boulder',(0,0,.26),(1.9,1.75,.58),'rock',2)
    for j in range(4):
        a = j*TAU/4+.3
        lump('Dark moss on rock',(math.cos(a)*.48,math.sin(a)*.44,.43),
             (.71,.61,.24),'cactus_dark')
    lump('Boulder shaded toe',(.39,.38,.17),(.78,.71,.29),'rock_dark')
    sapling((0,0,.43))


def spirit_lanterns():
    moss_rock(1.55,1.35,.19)
    cylinder('Stone post socket',(0,0,.26),.31,.36,'moon',n=8)
    cylinder('Brass post foot',(0,0,.47),.19,.13,'brass',n=8)
    tube('Crooked iron spirit post',[(0,0,.36),(-.08,0,1.65),(.10,.02,2.88),
         (-.05,0,3.59),(-.60,.04,3.70),(-1.04,.06,3.52)],
         [.095,.075,.075,.065,.055,.045],'iron',n=9,bevel=True)
    for z in (.71,1.49,2.71,3.40):
        cylinder('Brass post collar',(0,0,z),.115,.10,'brass',n=8)
    # Distinct crooked arms distribute lanterns in depth as well as in height.
    anchors = [(-.99,.06,3.53),(-.68,-.60,3.20),(.68,-.55,3.40),
               (.99,.35,3.04),(.28,1.0,2.71)]
    heights = [2.88,2.23,2.69,2.02,1.49]
    for j,(anchor,z) in enumerate(zip(anchors,heights)):
        x,y,az = anchor
        if j:
            tube('Crooked lantern support arm',[(.02,0,az-.14),(x*.65,y*.65,az+.08),anchor],
                 [.058,.048,.03],'iron',n=9,bevel=True)
        radius = .23+.02*(j % 3)
        chain('Hanging paper lantern chain',anchor,(x,y,z+radius*1.50))
        lantern((x,y,z),radius)
    for j,(x,y,z) in enumerate(((-.91,.70,3.61),(.57,-.72,3.93))):
        lantern((x,y,z),.19+j*.014)


def bell(label, loc, radius, role):
    """Downward, thick-walled scalloped flower with a luminous inner throat."""
    start = len(PARTS)
    n = 8
    profile = [(0,.26),(-.33,.43),(-.70,.65),(-1.0,1.0)]
    verts = []
    for inner in (False,True):
        for k,(zz,rr) in enumerate(profile):
            for j in range(n):
                z = radius*zz
                if k == len(profile)-1:
                    z += .045*(j % 2)
                r = radius*rr-(.045 if inner else 0)
                verts.append((r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),z))
    stride = len(profile)*n
    faces,roles = [],[]
    for inner in (0,1):
        for k in range(len(profile)-1):
            for j in range(n):
                i = inner*stride+k*n+j
                jj = inner*stride+k*n+(j+1) % n
                faces.append((i,jj,jj+n,i+n))
                roles.append(role if inner == 0 else 'crystal_core')
    for k in (0,(len(profile)-1)*n):
        for j in range(n):
            i,jj = k+j,k+(j+1) % n
            faces.append((i,i+stride,jj+stride,jj))
            roles.append('crystal_core')
    Part(label,verts,faces,role,face_roles=roles)
    tube('Flower luminous hanging pistil',[(0,0,-radius*.28),(0,0,-radius*1.12)],
         .027,'crystal_core',n=5)
    lump('Bell flower clapper',(0,0,-radius*1.10),(.13,.13,.16),'crystal_core')
    for j in range(3):
        a = j*TAU/3
        leaf('Bell flower calyx',(0,0,.045),(.13*math.cos(a),.13*math.sin(a),-.10),
             .12,'cactus_dark')
    move_group(start,loc)


def bell_flowers():
    moss_rock(2.25,2.20,.21)
    positions = [(-.65,-.38,.79),(.00,-.55,.96),(.65,-.28,.76),
                 (-.64,.35,.94),(.08,.18,.86),(.68,.43,.98),(-.12,.74,.75)]
    for j,(x,y,z) in enumerate(positions):
        tip = Vector((x+.18*(-1)**j,y+.10,z-.06))
        tube('Arched flower stem',[(x,y,.15),(x-.10,y,z*.76),
             (x+.08*(-1)**j,y+.04,z+.03),tip], [.04,.034,.03,.025],
             'cactus_dark',n=5)
        for sign in (-1,1):
            leaf('Broad basal flower leaf',(x,y,.20),
                 (x+sign*.33,y+.14,.45),.23,'cactus_dark')
        bell('Hollow drooping dream bell',tip,.24+.025*(j % 2),
             'petal_glow' if j % 3 else 'crystal_core')


def numeral(text, loc, height=.24):
    start = len(PARTS)
    for j,char in enumerate(text):
        x = (j-(len(text)-1)/2)*height*.46
        if char == 'I':
            box('Carved numeral I',(x,0,0),(.045,.055,height),'stone_dark',bevel=False)
        elif char == 'X':
            for sign in (-1,1):
                beam('Carved numeral X',(x-sign*height*.20,0,-height*.5),
                     (x+sign*height*.20,0,height*.5),.045,.055,'stone_dark')
        elif char == 'V':
            for sign in (-1,1):
                beam('Carved numeral V',(x,0,-height*.5),
                     (x+sign*height*.23,0,height*.5),.045,.055,'stone_dark')
    move_group(start,loc)
    # These bars are cutters, not raised dark lettering. Their fresh cut faces
    # inherit stone_dark, making the actual engraving readable at game scale.
    for part in PARTS[start:]:
        for v in part.verts:
            v.y = .40+(v.y-loc[1])*2.5
        CARVINGS.append(part)
    del PARTS[start:]


def arch_wedge(start, end, role, broken=False):
    angles = [start+(end-start)*j/3 for j in range(4)]
    polygon = [(1.91*math.cos(a),2.46+1.91*math.sin(a)) for a in angles]
    if broken:
        a = end
        polygon += [(r*math.cos(a+da),2.46+r*math.sin(a+da))
                    for r,da in ((1.80,.045),(1.68,-.09),(1.54,.035))]
    polygon += [(1.34*math.cos(a),2.46+1.34*math.sin(a)) for a in reversed(angles)]
    extrude('Fractured arch voussoir' if broken else 'Ancient arch voussoir',
            polygon,.77,role)


def rune_arch():
    for side in (-1,1):
        box('Separate stone arch footing',(side*1.59,0,.13),(.86,1.12,.26),'stone_dark')
        for j in range(5):
            x = side*(1.58+.035*math.sin(j*2))
            box('Staggered ancient masonry',(x,0,.49+j*.42),(.62,.80,.39),
                'stone' if (j+side) % 3 else 'stone_dark',rot=(0,.015*side,0))
            # Inlay is recessed within a thick, dark shallow glyph channel.
            y,z = .412,.49+j*.42
            tube('Recessed rune channel',[(x-.11,y,z+.10),(x+.12,y,z),
                 (x-.07,y,z-.11)],.052,'stone_dark',n=4)
            tube('Rune glow inside channel',[(x-.11,y+.018,z+.10),(x+.12,y+.018,z),
                 (x-.07,y+.018,z-.11)],.027,'rift_glow',n=4)
    # Large missing blocks leave a broken top silhouette. The moon keystone
    # remains on the left shoulder; nothing generates detached crumbling dust.
    for j in (0,1,2,6,7,8):
        arch_wedge(j*math.pi/9+.022,(j+1)*math.pi/9-.022,
                   'moon' if j == 6 else ('stone_dark' if j % 3 == 0 else 'stone'),
                   broken=j in (2,6))
    extrude('Moon keystone at broken crown',[(-.60,3.72),(-.76,4.30),
            (-.36,4.48),(-.27,3.84)],.90,'moon')
    for x,z,text in ((-1.02,3.73,'XI'),(-1.57,2.63,'IX'),(1.59,2.65,'III'),(1.08,3.72,'I')):
        numeral(text,(x,.47,z),.24)
    for side in (-1,1):
        # Nested fragments of a stopped clock track reinforce the broken arch.
        for k in range(3):
            a = (k+.4)*.25+(0 if side == 1 else math.pi-.82)
            x,z = 1.68*math.cos(a),2.46+1.68*math.sin(a)
            box('Brass clock track hour index',(x,.435,z),(.06,.05,.17),
                'brass',rot=(0,math.pi/2-a,0))
    # Disk is three separate substantial shards with real gaps, set into the
    # surviving left pier, plus a broad metal cradle (no transparent shader).
    cx,cz = -1.58,1.42
    hoop('Cracked glass disc brass setting',(cx,.49,cz),.45,.065,.12,'brass',n=16)
    for a,b in ((.06,1.96),(2.06,4.05),(4.16,TAU-.04)):
        polygon = [(cx+.025*math.cos((a+b)/2),cz+.025*math.sin((a+b)/2))]
        polygon += [(cx+.375*math.cos(t),cz+.375*math.sin(t))
                    for t in [a+(b-a)*j/5 for j in range(6)]]
        extrude('Cracked glass disc shard',polygon,.075,'glass_blue',y=.53,bevel=False)
    tube('Disc inner luminous fissure',[(cx-.02,.59,cz-.31),(cx+.015,.59,cz),
         (cx+.29,.59,cz+.16)],.025,'rift_glow',n=4)
    vine = [(-1.77,.45,.25),(-1.87,.44,1.04),(-1.48,.44,1.82),
            (-1.85,.44,2.57),(-1.49,.44,3.24),(-1.22,.44,3.62)]
    tube('Climbing ancient ivy',vine,.045,'cactus_dark',n=5)
    for j in range(11):
        t = j/10*(len(vine)-1)
        index = min(int(t),len(vine)-2)
        p = Vector(vine[index]).lerp(Vector(vine[index+1]),t-index)
        side = (-1)**j
        leaf('Ivy broad folded leaf',p,p+Vector((side*.25,.05,.17)),.25,'cactus_dark')
    for x,y,s in ((-.87,.17,.49),(.92,-.20,.37),(-1.83,-.39,.35)):
        lump('Fallen arch masonry',(x,y,.18),(s,.52,.31),'stone')


def float_rock_crystal():
    n = 10
    verts = []
    # Flat organic top over a faceted, tapered underside. No filler pedestal.
    rings = [(-1.62,.12),(-1.12,.53),(-.50,1.02),(-.12,1.35),
             (.20,1.35),(.31,1.25)]
    radial = [1+random.uniform(-.09,.09) for _ in range(n)]
    for z,r in rings:
        for j in range(n):
            a = j*TAU/n
            verts.append((r*radial[j]*math.cos(a),r*radial[j]*math.sin(a),
                          z+(.035*math.sin(j*2) if z < .2 else 0)))
    faces = [tuple(reversed(range(n)))]
    roles = ['rock_dark']
    for k in range(len(rings)-1):
        for j in range(n):
            faces.append((k*n+j,k*n+(j+1) % n,(k+1)*n+(j+1) % n,(k+1)*n+j))
            roles.append('earth' if k == 4 else ('rock_dark' if (j+k) % 3 == 0 else 'rock'))
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    roles.append('earth')
    Part('Inverted floating island cone',verts,faces,'rock',bevel=True,face_roles=roles)
    for j in range(5):
        a = j*TAU/5
        lump('Flat island moss patch',(.63*math.cos(a),.63*math.sin(a),.35),
             (1.22,1.10,.16),'cactus' if j % 2 else 'cactus_dark')
    sapling((-.11,-.06,.39),.57)
    for j in range(7):
        a = j*TAU/7+.17
        r = .98+.12*(j % 2)
        p = Vector((r*math.cos(a),r*math.sin(a),-.20))
        crystal('Underside hanging crystal stalactite',p,
                p+Vector((.25*math.cos(a),.25*math.sin(a),-.66-.17*(j % 3))),
                .19+.035*(j % 2))
    for j in range(4):
        a = j*TAU/4+.4
        tube('Mineral island dangling root',[(.95*math.cos(a),.95*math.sin(a),.08),
             (.98*math.cos(a),.98*math.sin(a),-.49),
             (.70*math.cos(a),.70*math.sin(a),-.93)], [.065,.045,.025],
             'bark_dark',n=5,bevel=True)


def glow_mushrooms():
    moss_rock(1.54,1.51,.16)
    positions = [(-.42,-.33,.69,.26),(.04,-.39,.98,.30),(.44,-.23,.61,.24),
                 (-.47,.23,.92,.28),(.09,.19,.76,.29),(.49,.29,1.03,.30),
                 (-.18,.55,.56,.22),(-.20,-.04,.45,.20)]
    for j,(x,y,z,r) in enumerate(positions):
        r *= .86
        lean = .08*(-1)**j
        tube('Tall moon mushroom stem',[(x,y,.13),(x+lean,y,z*.67),(x,y,z)],
             [.065,.052,.075],'moon',n=7,bevel=True)
        # The annular glow lip and forward hanging spots remain visible from
        # above while still belonging to the underside of each cap.
        lathe('Faceted blue mushroom umbrella',[(0,r*.97),(.10,r),
              (.23,r*.58),(.27,r*.13)],'crystal_blue',n=10).transform((x,y,z))
        lathe('Luminous mushroom underside',[(0,r*1.16),(.045,r*1.18)],
              'crystal_core',n=10).transform((x,y,z-.028))
        for k in range(5):
            a = k*TAU/5
            xx,yy = x+r*1.11*math.cos(a),y+r*1.11*math.sin(a)
            lump('Glow spot on cap underside',(xx,yy,z-.055),(.14,.14,.15),'crystal_core')
        if j in (1,3,5):
            cylinder('Mushroom dark stem collar',(x,y,z-.13),.092,.065,
                     'cactus_dark',n=8,bevel=False)


# name, W/D/H metres, centre pivot exception, triangle limit, builder
SPECS = (
    ('DreamTree_Crystal',(5,5,7),False,12000,dream_tree_crystal),
    ('DreamTree_Willow',(5,5,6.5),False,12000,dream_tree_willow),
    ('DreamTree_Small',(2,2,3),False,6000,dream_tree_small),
    ('SpiritLanterns',(3,3,4),False,6000,spirit_lanterns),
    ('BellFlowers',(2.4,2.4,1),False,6000,bell_flowers),
    ('RuneArch',(4,1.2,4.5),False,12000,rune_arch),
    ('FloatRock_Crystal',(3,3,3.5),True,6000,float_rock_crystal),
    ('GlowMushrooms',(1.6,1.6,1.2),False,6000,glow_mushrooms),
)


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def assembled_mesh(name):
    bpy.ops.object.select_all(action='DESELECT')
    objects = []
    for bevel in (True,False):
        verts,faces,indices = [],[],[]
        for part in PARTS:
            if part.bevel != bevel:
                continue
            offset = len(verts)
            verts.extend(part.verts)
            faces.extend(tuple(offset+i for i in f) for f in part.faces)
            indices.extend(ROLES.index(r) for r in part.roles)
        if not verts:
            continue
        data = bpy.data.meshes.new(name+'_parts')
        data.from_pydata(verts,[],faces)
        for role in ROLES:
            data.materials.append(MATS[role])
        for face,index in zip(data.polygons,indices):
            face.material_index = index
            face.use_smooth = False
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(name+'_parts',data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Ancient worn edges 0.025m two segments','BEVEL')
            mod.width,mod.segments = .025,2
            mod.limit_method,mod.angle_limit = 'ANGLE',math.radians(45)
            mod.use_clamp_overlap = True
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
        objects.append(obj)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name,obj.data.name = name,name+'_Mesh'
    return obj


def material_coverage(obj):
    """Occlusion-aware projected area at the 55-degree +Y review angle."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    view = Vector((0,math.cos(math.radians(55)),math.sin(math.radians(55))))
    distance = max(obj.dimensions)*4
    areas = defaultdict(float)
    for index,tri in enumerate(tris):
        facing = tri.normal.dot(view)
        if facing <= 0 or tri.area < 1e-10:
            continue
        center = sum((verts[i] for i in tri.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.01)
        if hit == index:
            areas[obj.data.materials[tri.material_index].name] += tri.area*facing
    total = sum(areas.values())
    assert total > 0, obj.name
    return {r:a/total for r,a in areas.items()}


def carve_numerals():
    if not CARVINGS:
        return
    verts,faces = [],[]
    for part in CARVINGS:
        offset = len(verts)
        verts.extend(part.verts)
        faces.extend(tuple(offset+i for i in face) for face in part.faces)
    data = bpy.data.meshes.new('Clock numeral engraving cutters')
    data.from_pydata(verts,[],faces)
    for role in ROLES:
        data.materials.append(MATS[role])
    for face in data.polygons:
        face.material_index = ROLES.index('stone_dark')
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    cutter = bpy.data.objects.new('Temporary numeral cutters',data)
    bpy.context.collection.objects.link(cutter)
    try:
        # Cut individual masonry solids. Cutting an assembled ruin would also
        # intersect its overlapping ivy, metal settings and inlaid glyphs.
        for part in PARTS:
            if not set(part.roles) <= {'stone','stone_dark','moon'}:
                continue
            lo = [min(v[i] for v in part.verts) for i in range(3)]
            hi = [max(v[i] for v in part.verts) for i in range(3)]
            nearby = any(all(max(v[i] for v in cut.verts) >= lo[i] and
                            min(v[i] for v in cut.verts) <= hi[i] for i in range(3))
                         for cut in CARVINGS)
            if not nearby:
                continue
            block_data = bpy.data.meshes.new('Temporary masonry for carving')
            block_data.from_pydata(part.verts,[],part.faces)
            for role in ROLES:
                block_data.materials.append(MATS[role])
            for face,role in zip(block_data.polygons,part.roles):
                face.material_index = ROLES.index(role)
            bm = bmesh.new()
            bm.from_mesh(block_data)
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
            bm.to_mesh(block_data)
            bm.free()
            block = bpy.data.objects.new('Temporary masonry for carving',block_data)
            bpy.context.collection.objects.link(block)
            try:
                bpy.context.view_layer.objects.active = block
                mod = block.modifiers.new('Recessed clock numerals','BOOLEAN')
                mod.operation,mod.solver,mod.object = 'DIFFERENCE','EXACT',cutter
                mod.use_self = True
                bpy.ops.object.modifier_apply(modifier=mod.name)
                part.verts = [v.co.copy() for v in block.data.vertices]
                part.faces = [tuple(f.vertices) for f in block.data.polygons]
                part.roles = [block.data.materials[f.material_index].name
                              for f in block.data.polygons]
            finally:
                result_data = block.data
                bpy.data.objects.remove(block,do_unlink=True)
                bpy.data.meshes.remove(result_data)
                if block_data != result_data and block_data.users == 0:
                    bpy.data.meshes.remove(block_data)
    finally:
        bpy.data.objects.remove(cutter,do_unlink=True)
        bpy.data.meshes.remove(data)


def finish(name, dims, centered, budget, seed):
    carve_numerals()
    obj = assembled_mesh(name)
    # Preserve the authored silhouettes; budget overflow is an authoring error,
    # rather than silently decimating the flower lips or lantern openings.
    assert triangles(obj) <= budget, (name,'triangle budget',triangles(obj),budget)
    lo,hi = bounds(obj)
    span,mid = hi-lo,(hi+lo)/2
    for v in obj.data.vertices:
        for i in range(3):
            anchor = mid[i] if i < 2 or centered else lo[i]
            v.co[i] = (v.co[i]-anchor)*dims[i]/span[i]
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), name+' has open/nonmanifold edges'
    bad = [f for f in bm.faces if f.calc_area() <= 1e-12]
    assert not bad, (name,'degenerate faces',[(f.material_index,tuple(f.calc_center_median())) for f in bad[:8]])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    lo,hi = bounds(obj)
    assert all(abs(obj.dimensions[i]-dims[i]) < 1e-4 for i in range(3)), name
    assert abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5, name
    assert abs(lo.z+hi.z if centered else lo.z) < 1e-5, name+' wrong pivot'
    assert obj.location.length < 1e-6 and tuple(obj.scale) == (1,1,1), name
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    assert len(used) >= 5 and used <= set(ROLES), (name,used)
    coverage = material_coverage(obj)
    print('VISIBLE '+name+' '+str({r:round(s,3) for r,s in
          sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    assert len(coverage) >= (6 if budget == 12000 else 5), (name,coverage)
    assert max(coverage.values()) <= .555, (name,'dominant material',coverage)
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color) == (.5,.5,.5,1)
        nodes = mat.node_tree.nodes
        shader = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
        assert len(nodes) == 2 and tuple(shader.inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert shader.inputs['Emission Strength'].default_value == 0
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    obj['front'] = '+Y'
    obj['origin_convention'] = 'center' if centered else 'bottom center'
    obj['dimensions_m'],obj['triangle_count'],obj['seed'] = list(dims),triangles(obj),seed
    destination = OUTPUT / ('Kit_V_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    """One row, Workbench flat material colours, 55 degrees, 1600 x 900."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1600,900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light,shading.color_type = 'FLAT','MATERIAL'
    shading.show_shadows,shading.show_cavity = False,True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.25,1.05
    shading.show_specular_highlight,shading.show_object_outline = False,True
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review dawn void')
    scene.world.color = (.095,.13,.21)
    locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role],1)
        gap = .65
        total = sum(spec[1][0] for spec in SPECS)+gap*(len(objects)-1)
        cursor = -total/2
        for obj,(_,dims,centered,_,_) in zip(objects,SPECS):
            obj.location = (cursor+dims[0]/2,0,dims[2]/2+.45 if centered else 0)
            cursor += dims[0]+gap
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',total+1.1
        target = Vector((0,0,2.0))
        a = math.radians(55)
        camera.location = target+Vector((0,45*math.cos(a),45*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            # Split long names onto two lines to avoid adjacent label collisions.
            data.body = obj.name.replace('DreamTree_','DreamTree\n').replace('FloatRock_','FloatRock\n')
            data.align_x,data.align_y,data.size = 'CENTER','TOP',.24
            data.materials.append(MATS['moon'])
            label = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(label)
            label.location = (obj.location.x,3.8,-.30)
            label.rotation_euler = camera.rotation_euler
            helpers.append(label)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper,do_unlink=True)
        for obj,loc in zip(objects,locations):
            obj.location = loc
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS, CARVINGS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    exported = []
    for i,(name,dims,centered,budget,build) in enumerate(SPECS):
        PARTS = []
        CARVINGS = []
        seed = 71893+i*1009
        random.seed(seed)
        build()
        exported.append(finish(name,dims,centered,budget,seed))
    assert len(scene.objects) == 8 and all(o.type == 'MESH' for o in scene.objects)
    preview(exported)
    assert len(scene.objects) == 8 and all(o.type == 'MESH' for o in scene.objects)
    assert all(tuple(m.diffuse_color) == (.5,.5,.5,1) for m in MATS.values())
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
