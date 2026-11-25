# 3장 클리어 퀘스트 '거울 마을' 키트 — 선 거울 · 광장 큰 거울 · 물웅덩이 · 노인 집 물건 4 · 노인 실루엣, 2026-11-25
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_quest.py
# Blender (X,Y,Z) = Unity (X,Z,Y); front is south (-Y), Z up.
import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3Quest'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_quest_preview.png'
TAU = math.tau
ROLES = ('wood', 'wood_dark', 'wood_light', 'gold', 'brass', 'bronze', 'iron', 'iron_dark', 'stone', 'stone_dark', 'moss', 'mirror_glass', 'mirror_back', 'gem_rose', 'gem_amber', 'ivory', 'velvet', 'fabric', 'fabric_dark', 'leather', 'poster', 'paper', 'clock_face', 'sand', 'glass', 'window_lit', 'curtain', 'thread_red', 'shadow', 'shadow_blur', 'water_edge')
HARD = set(ROLES) - {'mirror_glass', 'fabric', 'paper', 'velvet', 'window_lit', 'moss', 'thread_red', 'mirror_back', 'glass', 'shadow_blur', 'curtain', 'sand', 'shadow', 'poster', 'water_edge', 'fabric_dark'}
PALETTE = {  # Preview only: exported Principled materials remain flat grey.
    'wood': (0.55, 0.36, 0.24),
    'wood_dark': (0.3, 0.19, 0.14),
    'wood_light': (0.75, 0.58, 0.4),
    'gold': (1, 0.8, 0.35),
    'brass': (0.85, 0.64, 0.32),
    'bronze': (0.6, 0.42, 0.26),
    'iron': (0.25, 0.26, 0.34),
    'iron_dark': (0.13, 0.13, 0.2),
    'stone': (0.62, 0.6, 0.72),
    'stone_dark': (0.36, 0.34, 0.48),
    'moss': (0.35, 0.55, 0.42),
    'mirror_glass': (0.75, 0.88, 1),
    'mirror_back': (0.2, 0.22, 0.35),
    'gem_rose': (1, 0.45, 0.7),
    'gem_amber': (1, 0.65, 0.2),
    'ivory': (0.95, 0.92, 0.82),
    'velvet': (0.5, 0.12, 0.3),
    'fabric': (0.45, 0.38, 0.3),
    'fabric_dark': (0.26, 0.22, 0.2),
    'leather': (0.45, 0.28, 0.2),
    'poster': (0.9, 0.72, 0.5),
    'paper': (0.95, 0.9, 0.78),
    'clock_face': (1, 0.95, 0.82),
    'sand': (0.95, 0.8, 0.5),
    'glass': (0.6, 0.8, 0.95),
    'window_lit': (1, 0.82, 0.5),
    'curtain': (0.7, 0.3, 0.35),
    'thread_red': (0.9, 0.2, 0.25),
    'shadow': (0.07, 0.05, 0.12),
    'shadow_blur': (0.25, 0.2, 0.35),
    'water_edge': (0.4, 0.55, 0.8),
}
MATS = {}
PARTS = []

def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Batch solid components before the shared 0.025m/two-segment bevel."""

    def __init__(self, label, verts, faces, role, bevel=True):
        assert role in ROLES, role
        self.label, self.role = label, role
        self.verts = [Vector(v) for v in verts]
        self.faces = faces
        self.uvs = None
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
    return Part(label, [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    """Solid polygon in XZ; +Y is its decorated/front surface."""
    n = len(polygon)
    verts = [(x, y+d, z) for d in (-depth/2, depth/2) for x, z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    return Part(label, verts, faces, role, bevel)


def lathe(label, rings, role, n=12, bevel=True):
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, bevel)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), top=None):
    return lathe(label, [(-depth/2, radius),
                        (depth/2, radius if top is None else top)], role, n).transform(loc, rot)


def tube(label, points, radius, role, n=5, bevel=False):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i+1, len(points)-1)]) -
                   Vector(points[max(0, i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = ref.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [tuple(Vector(point) + radius*(u*math.cos(j*TAU/n) +
                                                v*math.sin(j*TAU/n))) for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n, len(points)*n)))
    return Part(label, verts, faces, role, bevel)


def rod(label, a, b, radius, role, n=6, bevel=True):
    return tube(label, [a,b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role):
    a, b = Vector(a), Vector(b)
    part = box(label, (0,0,0), (width,depth,(b-a).length), role)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def hoop(label, loc, radius, band, depth, role, n=24, rot=(0,0,0)):
    """Open ring in XZ, axis Y; no disk hiding the negative space."""
    verts = [(r*math.cos(j*TAU/n), y, r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role).transform(loc, rot)


def sector(label, inner, outer, start, end, depth, role, y=0, steps=4):
    angles = [start+(end-start)*j/steps for j in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    return extrude(label, polygon, depth, role, y)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.91,1.09)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    result = Part(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return result


def scroll(label, loc, radius=.3, flip=1):
    x,y,z = loc
    points = []
    for j in range(11):
        t = j/10
        a, r = -.8+t*math.pi*1.9, radius*(1-.73*t)
        points.append((x+flip*r*math.cos(a),y,z+r*math.sin(a)))
    return tube(label, points, .025 if radius < .3 else .035, 'gold')


def numeral(text, loc, height=.28, role='gold', angle=0):
    """Bold mesh bars, no font dependency. I, V and X are actual silhouettes."""
    start = len(PARTS)
    pitch = height*.45
    for i, char in enumerate(text):
        x = (i-(len(text)-1)/2)*pitch
        w = max(.04,height*.15)
        if char == 'I':
            box('Numeral I', (x,0,0), (w,.065,height), role)
        elif char == 'X':
            for s in (-1,1):
                beam('Numeral X', (x-s*height*.18,0,-height/2),
                     (x+s*height*.18,0,height/2), w,.065,role)
        elif char == 'V':
            for s in (-1,1):
                beam('Numeral V', (x,0,-height/2),
                     (x+s*height*.21,0,height/2), w,.065,role)
    move_group(start, loc, (0,angle,0))


def clock(loc, radius, hands=True):
    """South-facing dial; both hands fixed at 11:59, no font dependency."""
    start = len(PARTS)
    cylinder('Clock iron back', (0,.025,0),radius,.10,'iron',n=24,rot=(math.pi/2,0,0))
    cylinder('Cream enamel dial',(0,-.045,0),radius*.88,.04,'clock_face',n=24,rot=(math.pi/2,0,0))
    hoop('Brass clock bezel',(0,-.06,0),radius,.12*radius,.08,'brass')
    hoop('Gold inner bezel',(0,-.085,0),radius*.86,.04,.04,'gold')
    for j in range(12):
        a = j*TAU/12
        box('Dark hour index',(radius*.70*math.sin(a),-.095,radius*.70*math.cos(a)),
            (.04,.04,max(.04,radius*.14)),'iron_dark',rot=(0,a,0),bevel=False)
    if hands:
        for label, minute, length in (('Minute hand',59,.71),('Hour hand',11+59/60,.49)):
            a = minute*TAU/(60 if label.startswith('Minute') else 12)
            beam(label,(0,-.13,0),(radius*length*math.sin(a),-.13,radius*length*math.cos(a)),.04,.04,'iron_dark')
        cylinder('Clock pivot',(0,-.16,0),.04,.04,'gold',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc)


# ======================= Chapter 3 quest builders =======================
SHOULDER_R = Vector((-.205, .015, 1.365))
GLASS_OBJECTS = {}
GLASS_CHECKS = {}


def arch(width, height, rise, expand=0, steps=16):
    """CCW in XZ, hence its front normal is -Y. Exact glass extents."""
    w, h, r = width/2+expand, height/2+expand, rise+expand
    spring = height/2-rise
    return [(-w,-h),(w,-h)] + [(w*math.cos(j*math.pi/steps),
            spring+r*math.sin(j*math.pi/steps)) for j in range(steps+1)]


def contour_band(label, inner, outer, y, depth, role):
    n = len(inner)
    verts = [(x,y+d,z) for d in (-depth/2,depth/2) for poly in (inner,outer) for x,z in poly]
    faces = []
    for i in range(n):
        j = (i+1) % n
        faces += [(i,j,n+j,n+i), (2*n+i,3*n+i,3*n+j,2*n+j),
                  (n+i,n+j,3*n+j,3*n+i), (i,2*n+i,2*n+j,j)]
    return Part(label,verts,faces,role)


def flat_glass(label, polygon, plane='XZ', offset=-.12, uv_bounds=None):
    """Single connected fan. UVs belong to vertices and survive rigid transforms."""
    n = len(polygon)
    cx = sum(p[0] for p in polygon)/n
    cy = sum(p[1] for p in polygon)/n
    coords = [(cx,cy)] + polygon
    verts = [(x,offset,y) if plane == 'XZ' else (x,y,offset) for x,y in coords]
    p = Part(label,verts,[(0,i+1,(i+1) % n+1) for i in range(n)],'mirror_glass',False)
    x0,x1,y0,y1 = uv_bounds or (min(x for x,y in polygon),max(x for x,y in polygon),
                               min(y for x,y in polygon),max(y for x,y in polygon))
    p.uvs = [((x-x0)/(x1-x0),(y-y0)/(y1-y0)) for x,y in coords]
    return p


def finial(loc, role='gold', radius=.12, height=.32):
    return lathe('Urn finial',[(0,radius*.65),(.12*height,radius),(.3*height,radius*.65),
                 (.65*height,radius),(.88*height,radius*.5),(height,.025)],role,n=10).transform(loc)


def sunburst(loc, radius, rays=12):
    x,y,z = loc
    cylinder('Sunburst center',(x,y,z),radius*.54,.08,'gold',n=16,rot=(math.pi/2,0,0))
    for j in range(rays):
        a = j*TAU/rays
        dx,dz = math.sin(a),math.cos(a)
        beam('Chunky sun ray',(x+dx*radius*.55,y,z+dz*radius*.55),
             (x+dx*radius,y,z+dz*radius),.055,.07,'gold')


def mirror_frame(width, height, rise, center, tilt, plaza=False):
    start = len(PARTS)
    shape = arch(width,height,rise)
    # Front of the solid backing is y=-.11; glass is y=-.12: exact .01 gap.
    extrude('Solid mirror backing',shape,.08,'mirror_back',y=-.07,bevel=False)
    flat_glass('Unbroken live reflection',shape,uv_bounds=(-width/2,width/2,-height/2,height/2))
    for a,b,y,d,role in ((0,.065,-.12,.13,'bronze'),(.065,.17,-.09,.18,'velvet' if plaza else 'wood'),
                          (.17,.24,-.075,.21,'gold')):
        contour_band('Layered arched moulding',arch(width,height,rise,a),arch(width,height,rise,b),y,d,role)
    # Relief discs along the stiles and arch follow the same plane as the glass.
    for x in (-width/2-.115,width/2+.115):
        for j in range(7 if plaza else 5):
            z = -height/2+.22+j*(height-rise-.3)/(6 if plaza else 4)
            cylinder('Raised carved sun disc',(x,-.205,z),.055,.045,'gold',n=8,rot=(math.pi/2,0,0))
    for j in range(1,8):
        a = math.pi*j/8
        cylinder('Arch gold stud',((width/2+.11)*math.cos(a),-.205,
                 height/2-rise+(rise+.11)*math.sin(a)),.05,.045,'gold',n=8,rot=(math.pi/2,0,0))
    for side in (-1,1):
        for z in (-height/2+.04,height/2-rise):
            scroll('Corner gold filigree',(side*(width/2+.08),-.25,z),.16,side)
    crest = height/2+.35
    if plaza:
        sunburst((0,-.10,crest),.68,18)
        clock((0,-.21,crest),.45)
        for x,z in ((-.58,crest),(0,crest+.63),(.58,crest)):
            lump('Crest rose cabochon',(x,-.22,z),(.15,.10,.18),'gem_rose')
    else:
        sunburst((0,-.10,crest-.09),.29)
        lump('Rose heart of sun',(0,-.21,crest-.09),(.22,.12,.25),'gem_rose')
        for side in (-1,1):
            lump('Amber crest stud',(side*.34,-.18,crest-.2),(.13,.10,.15),'gem_amber')
    # Negative X rotation tips the top north, and the front normal upward.
    move_group(start,loc=(0,0,center),rot=(-math.radians(tilt),0,0))


def stand_mirror():
    for z,w,d,h,role in ((.08,2.4,1.2,.16,'stone_dark'),(.22,2.28,1.1,.12,'stone'),
                         (.295,2.34,1.14,.05,'gold'),(.335,2.17,1.02,.03,'stone')):
        box('Stepped cheval plinth',(0,0,z),(w,d,h),role)
    for x in (-1.01,1.01):
        for y in (-.43,.43):
            lump('Bronze lion paw',(x,y,.085),(.30,.33,.16),'bronze')
            for dx in (-.07,.0,.07):
                rod('Paw toe ridge',(x+dx,y-.12,.075),(x+dx,y-.05,.13),.022,'bronze',n=5,bevel=False)
        lathe('Dark turned upright',[(.35,.14),(.45,.105),(.62,.09),(2.13,.085),
              (2.3,.13),(2.4,.10)],'wood_dark',n=10).transform((x,0,0))
        for z in (.48,.66,2.13,2.32):
            cylinder('Gold post collar',(x,0,z),.12,.065,'gold',n=10)
        finial((x,0,2.4),radius=.11,height=.32)
        rod('Brass pivot spindle',(x-.1,0,1.85),(x+.1,0,1.85),.085,'brass',n=10)
        lump('Pivot knob',(x,-.11,1.85),(.16,.13,.17),'brass')
    mirror_frame(1.6,2.2,.35,1.85,30)


def plaza_mirror():
    # The explicitly requested 2.2m radius takes precedence over nominal 3.6m width.
    for z,r,h,role in ((.10,2.2,.20,'stone'),(.28,1.9,.16,'stone_dark'),(.44,1.6,.16,'ivory')):
        cylinder('Round plaza step',(0,0,z),r,h,role,n=48)
        hoop('Pedestal gold edge',(0,0,z+h/2),r,.055,.045,'gold',n=48,rot=(math.pi/2,0,0))
    for j in range(24):
        a = j*TAU/24
        box('Top step diamond mosaic',(1.43*math.cos(a),1.43*math.sin(a),.53),(.15,.15,.045),
            'gem_rose' if j % 2 else 'gem_amber',rot=(0,0,a+math.pi/4))
    for x in (-1.68,1.68):
        for z,r,h,role in ((.61,.22,.18,'gold'),(.76,.18,.12,'bronze'),(3.65,.25,.16,'gold'),(3.8,.28,.14,'gold')):
            cylinder('Column moulding',(x,.10,z),r,h,role,n=12)
        cylinder('Ivory column shaft',(x,.10,2.18),.15,2.76,'ivory',n=12)
        for j in range(8):
            a=j*TAU/8
            rod('Fluted ivory relief',(x+.147*math.cos(a),.10+.147*math.sin(a),.9),
                (x+.147*math.cos(a),.10+.147*math.sin(a),3.5),.025,'ivory',n=5,bevel=False)
        finial((x,.10,3.87),radius=.16,height=.4)
        rod('Column mirror pivot',(x-.18,0,2.7),(x+.18,0,2.7),.11,'brass',n=10)
    mirror_frame(2.6,3.6,.6,2.7,25,plaza=True)


def mirror_puddle():
    polygon=[]
    for j in range(12):
        a=j*TAU/12
        r=random.uniform(1.72,2.0)
        polygon.append((r*math.cos(a),r*math.sin(a)))
    # A square projection preserves equal texel scale; V points north (+Y).
    side=max(max(x for x,y in polygon)-min(x for x,y in polygon),
             max(y for x,y in polygon)-min(y for x,y in polygon))
    cx=(min(x for x,y in polygon)+max(x for x,y in polygon))/2
    cy=(min(y for x,y in polygon)+max(y for x,y in polygon))/2
    flat_glass('Puddle live reflection',polygon,'XY',.02,(cx-side/2,cx+side/2,cy-side/2,cy+side/2))
    # Opaque backing top is .01 below the water plane.
    n=len(polygon)
    Part('Puddle backing',[(0,0,.01)]+[(x,y,.01) for x,y in polygon],
         [(0,i+1,(i+1)%n+1) for i in range(n)],'mirror_back',False)
    # Radial distance is slightly larger than .12 near the sharpest corners.
    outer=[(x*(1+.12/math.hypot(x,y)),y*(1+.12/math.hypot(x,y))) for x,y in polygon]
    verts=[(x,y,.015) for poly in (polygon,outer) for x,y in poly]
    Part('Thin irregular water rim',verts,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],'water_edge',False)
    for j in range(8):
        a=j*TAU/8+.16
        r=2.02+random.uniform(-.07,.08)
        size=random.uniform(.25,.43)
        p=lump('Low rim stone',(r*math.cos(a),r*math.sin(a),.065),
               (size,size*.8,.12),'stone' if j%2 else 'stone_dark')
        for v in p.verts:
            v.z=max(.004,min(.13,v.z))
    for j in range(3):
        a=.6+j*TAU/3
        for k in range(3):
            lump('Low moss tuft',(2.0*math.cos(a)+k*.065,2.0*math.sin(a),.045),(.19,.14,.08),'moss')


def gear(loc, radius=.105, role='brass', upright=False):
    start=len(PARTS)
    hoop('Gear open hub',(0,0,0),radius*.8,.04,.055,role,n=12)
    for j in range(8):
        a=j*TAU/8
        box('Gear tooth',(radius*.84*math.sin(a),0,radius*.84*math.cos(a)),
            (.055,.06,.06),role,rot=(0,a,0),bevel=False)
    move_group(start,loc,rot=(0,0,0) if upright else (math.pi/2,0,0))


def workbench(second=False):
    top='wood_dark' if second else 'wood'
    box('Thick workbench top',(0,0,.87),(1.6,.8,.16),top)
    for x in (-.78,.78):
        box('Pale end grain strip',(x,0,.88),(.055,.82,.13),'wood_light')
    for y in (-.39,.39):
        box('Bench raised edge strip',(0,y,.88),(1.6,.045,.13),'wood_light')
    for x in (-.64,.64):
        for y in (-.27,.27):
            lathe('Turned workbench leg',[(0,.08),(.07,.10),(.13,.067),(.33,.05),(.38,.08),
                  (.47,.065),(.74,.075),(.8,.09)],'wood_dark',n=8).transform((x,y,0))
            cylinder('Brass leg ferrule',(x,y,.095),.084,.065,'brass',n=8)
    box('Lower shelf',(0,0,.29),(1.37,.64,.09),'wood')
    for x in (-.64,.64):
        beam('Diagonal bench brace',(x,-.27,.36),(x,.27,.77),.065,.065,'iron' if second else 'wood_light')
    for j in range(4):
        box('Apron carved panel',(-.55+j*.36,-.355,.73),(.27,.045,.10),'wood_light')
        cylinder('Apron brass pin',(-.55+j*.36,-.39,.73),.027,.04,'brass',n=6,rot=(math.pi/2,0,0))
    # Visible rolled leather tools below the top, with contrasting straps.
    cylinder('Rolled leather tool wrap',(-.3,-.06,.42),.09,.48,'leather',n=10,rot=(0,math.pi/2,0))
    for x in (-.45,-.16):
        hoop('Tool roll brass strap',(x,-.06,.42),.10,.035,.05,'brass',n=10,rot=(0,0,math.pi/2))
    lathe('Brass oil can',[(0,.08),(.13,.08),(.19,.045),(.23,.035)],'brass',n=10).transform((.4,.1,.335))
    tube('Oil can spout',[(.42,.1,.55),(.53,.10,.61),(.58,.10,.65)],.022,'iron',n=5)


def cuckoo_bench():
    workbench()
    box('Vice base',(.64,-.13,1.0),(.24,.25,.10),'iron_dark')
    for x in (.55,.73):
        box('Vice jaw',(x,-.13,1.09),(.07,.23,.15),'iron')
    rod('Vice screw',(.43,-.17,1.04),(.83,-.17,1.04),.035,'brass')
    rod('Vice handle',(.82,-.17,.93),(.82,-.17,1.15),.025,'iron')
    start=len(PARTS)
    extrude('Cuckoo house', [(-.23,0),(.23,0),(.23,.46),(0,.67),(-.23,.46)],.23,'wood',y=0)
    extrude('Pale carved facade',[(-.195,.055),(.195,.055),(.195,.44),(0,.6),(-.195,.44)],.045,'wood_light',y=-.137)
    for side in (-1,1):
        beam('Steep carved roof',(0,0,.72),(side*.32,0,.42),.10,.40,'wood_dark')
        for j in range(4):
            lump('Roof carved leaf',(side*(.065+j*.067),-.21,.66-j*.063),(.11,.065,.075),'wood_light')
        for j in range(3):
            lump('Facade carved leaf',(side*.162,-.18,.13+j*.11),(.08,.045,.11),'wood')
    clock((0,-.19,.27),.13)
    box('Cuckoo door dark opening',(0,-.173,.49),(.14,.04,.16),'wood_dark',bevel=False)
    box('Open little door',(.11,-.24,.49),(.11,.045,.16),'wood',rot=(0,0,-.95))
    lump('Popping ivory cuckoo',(0,-.28,.50),(.12,.20,.11),'ivory')
    lump('Cuckoo head',(0,-.37,.55),(.08,.09,.09),'ivory')
    extrude('Amber cuckoo beak',[(-.035,.54),(.035,.54),(0,.59)],.08,'gem_amber',y=-.425)
    lump('Gold roof bird',(0,0,.75),(.18,.10,.09),'gold')
    lump('Gold bird head',(.07,-.015,.80),(.08,.08,.08),'gold')
    move_group(start,loc=(-.13,.13,.95))
    for x in (-.24,.0):
        tube('Visible hanging clock chain',[(x,-.18,1.0),(x,-.47,.86),(x,-.47,.52)],.022,'brass',n=5)
        lathe('Pinecone clock weight',[(0,.025),(.045,.07),(.10,.085),(.19,.06),(.23,.025)],'bronze',n=8).transform((x,-.47,.32))
        for z in (.39,.45,.50):
            cylinder('Pinecone relief scales',(x,-.47,z),.082,.04,'brass',n=8)
    for loc in ((-.59,-.17,.98),(.27,-.24,.98),(.34,.25,.98)):
        gear(loc,.08)
    for x in (-.52,-.46):
        beam('Clockmaker tweezers',(x,-.29,.99),(x+.07,-.05,.99),.04,.04,'iron')
    rod('Magnifier support',(-.6,.26,.95),(-.6,.26,1.2),.027,'brass')
    hoop('Magnifier brass rim',(-.6,.24,1.28),.12,.04,.055,'brass',n=16,rot=(.5,0,0))
    cylinder('Magnifier glass',(-.6,.24,1.28),.085,.04,'glass',n=16,rot=(math.pi/2+.5,0,0))


def hourglass_bench():
    workbench(True)
    start=len(PARTS)
    for z in (.04,.70):
        cylinder('Brass hourglass end plate',(0,0,z),.28,.08,'brass',n=16)
        cylinder('Gold plate moulding',(0,0,z+.025),.29,.045,'gold',n=16)
    for j in range(3):
        a=j*TAU/3+math.pi/2
        lathe('Turned gold hourglass pillar',[(.08,.045),(.15,.06),(.22,.035),(.52,.035),
              (.59,.06),(.66,.045)],'gold',n=8).transform((.235*math.cos(a),.235*math.sin(a),0))
    # Open-front glass shells expose the sand without making the glass non-planar mirrors.
    # Ordinary 'glass' is a separate role; live reflection restrictions apply only to mirror_glass.
    for upper in (False,True):
        rings=[(.37,.035),(.44,.10),(.55,.19),(.64,.17)] if upper else [(.10,.17),(.18,.19),(.30,.10),(.37,.035)]
        n=14
        verts=[(r*math.cos(-.15+j*(math.pi+ .3)/(n-1)),r*math.sin(-.15+j*(math.pi+.3)/(n-1)),z) for z,r in rings for j in range(n)]
        Part('Rear glass bulb shell',verts,[(i*n+j,i*n+j+1,(i+1)*n+j+1,(i+1)*n+j)
             for i in range(3) for j in range(n-1)],'glass',False)
        for side in (-1,1):
            tube('Thick cut glass outline',[(side*r,0,z) for z,r in rings],.023,'glass',n=5)
    lathe('Full bottom sand',[(.10,.15),(.15,.16),(.23,.12),(.28,.025)],'sand',n=14,bevel=False)
    lathe('Remaining upper sand',[(.42,.035),(.50,.11),(.54,.15)],'sand',n=12,bevel=False)
    rod('Falling sand stream',(0,0,.27),(0,0,.43),.021,'sand',n=5,bevel=False)
    # Dark zigzag inset against the front edge of the upper cutaway bulb.
    tube('Cracked upper bulb groove',[(-.14,-.07,.61),(-.09,-.14,.57),(-.04,-.13,.55),
         (-.07,-.11,.51),(.0,-.085,.47),(.04,-.045,.43)],.021,'iron_dark',n=5)
    move_group(start,loc=(-.1,.06,.95))
    lump('Spilled sand heap',(.40,-.19,.98),(.42,.25,.07),'sand')
    for j in range(5):
        lump('Scattered sand clump',(.22+j*.08,-.30+random.uniform(-.03,.03),.972),(.05,.05,.045),'sand')
    for loc in ((-.60,-.22,.98),(.56,.25,.98)):
        gear(loc,.09)
    box('Notebook leather back',(.46,.09,.985),(.28,.22,.05),'leather',rot=(0,0,.18))
    box('Ivory clockmaker notebook',(.46,.09,1.02),(.25,.20,.04),'ivory',rot=(0,0,.18),bevel=False)
    box('Notebook red bookmark',(.48,.01,1.045),(.04,.16,.04),'thread_red',bevel=False)


def coat_poster():
    # Coat rack to the west of the easel, broad feet and turned column.
    cx=-.55
    lathe('Turned dark coat rack',[(.08,.11),(.18,.13),(.28,.065),(1.62,.055),
          (1.72,.10),(1.81,.06)],'wood_dark',n=10).transform((cx,.10,0))
    for j in range(3):
        a=j*TAU/3
        beam('Rack tripod foot',(cx,.10,.25),(cx+.38*math.cos(a),.10+.38*math.sin(a),.055),.095,.09,'wood_dark')
        lump('Rack bronze foot',(cx+.38*math.cos(a),.10+.38*math.sin(a),.055),(.16,.16,.10),'bronze')
    for z in (.25,1.5,1.73):
        cylinder('Rack collar',(cx,.10,z),.085,.07,'brass',n=10)
    finial((cx,.1,1.78),radius=.09,height=.16)
    for side in (-1,1):
        tube('Curled brass coat hook',[(cx,.10,1.58),(cx+side*.18,.10,1.72),
             (cx+side*.25,.10,1.72),(cx+side*.27,.10,1.81)],.032,'brass',n=6)
    # Heavy worn duster, shaped shoulders, wide lapels, five chunky vertical folds.
    poly=[(-.28,.38),(-.3,.47),(-.24,1.3),(-.32,1.53),(-.13,1.62),(.13,1.62),
          (.32,1.53),(.24,1.3),(.30,.41),(.16,.37),(.03,.43),(-.10,.36)]
    start=len(PARTS)
    extrude('Long hanging duster',poly,.18,'fabric',y=-.10,bevel=False)
    for side in (-1,1):
        beam('Duster sleeve',(side*.26,-.09,1.49),(side*.38,-.09,.92),.14,.18,'fabric')
        extrude('Raised dark coat lapel',[(side*.04,1.22),(side*.16,1.58),(side*.06,1.56)],.045,'fabric_dark',y=-.22,bevel=False)
    for j in range(5):
        x=-.21+j*.105
        tube('Raised coat fold',[(x,-.21,.46+random.uniform(0,.09)),(x*.9,-.23,.86),(x*.75,-.205,1.26)],
             .026,'fabric_dark' if j%2 else 'fabric',n=5)
    box('Worn elbow patch',(-.35,-.20,1.10),(.13,.045,.18),'fabric_dark',rot=(0,.16,0),bevel=False)
    box('Duster lower patch',(.17,-.21,.66),(.14,.045,.18),'fabric_dark',bevel=False)
    box('Leather coat belt',(0,-.235,1.05),(.47,.05,.075),'leather')
    hoop('Belt brass buckle',(.06,-.27,1.05),.065,.025,.04,'brass',n=4,rot=(0,math.pi/4,0))
    move_group(start,loc=(cx,0,0))
    start=len(PARTS)
    lathe('Cowboy hat crown',[(0,.17),(.15,.15),(.19,.11)],'leather',n=12)
    cylinder('Broad cowboy hat brim',(0,0,0),.28,.045,'leather',n=16)
    cylinder('Gold hat band',(0,0,.055),.17,.055,'gold',n=12)
    move_group(start,loc=(cx-.20,-.06,1.78),rot=(.15,-.24,.10))
    # Poster beside the rack: raised pictorial relief, deliberately no lettering.
    ex=.55
    for x in (ex-.37,ex+.37):
        beam('Easel front leg',(x,-.01,.04),(ex+(x-ex)*.75,.22,1.95),.075,.075,'wood')
    beam('Easel rear leg',(ex,.75,.04),(ex,.22,1.87),.085,.085,'wood_dark')
    box('Easel support ledge',(ex,-.08,.56),(1.12,.24,.09),'wood_light')
    box('Movie poster panel',(ex,.055,1.25),(.90,.055,1.30),'poster',bevel=False)
    for x in (ex-.48,ex+.48):
        box('Poster carved side frame',(x,.025,1.25),(.075,.13,1.45),'wood_dark')
    for z in (.55,1.95):
        box('Poster frame horizontal',(ex,.025,z),(1.03,.13,.075),'wood_dark')
        for side in (-1,1):
            box('Gold frame corner',(ex+side*.44,-.055,z),(.13,.045,.11),'gold')
    cylinder('Large setting sun',(ex+.16,-.001,1.58),.25,.045,'gem_amber',n=24,rot=(math.pi/2,0,0))
    extrude('Western mesa relief',[(ex-.43,.65),(ex+.43,.65),(ex+.43,1.13),(ex+.28,1.13),
            (ex+.23,1.27),(ex+.03,1.27),(ex-.03,1.05),(ex-.20,1.05),(ex-.27,1.18),(ex-.43,1.18)],.04,'bronze',y=-.015)
    # Raised, unmistakable gunslinger outline: hat, coat, legs and outstretched arm.
    extrude('Poster gunslinger coat',[(ex-.15,.84),(ex+.10,.84),(ex+.04,1.36),(ex-.10,1.36)],.045,'shadow',y=-.058,bevel=False)
    cylinder('Poster gunslinger head',(ex-.03,-.066,1.43),.075,.04,'shadow',n=12,rot=(math.pi/2,0,0))
    box('Poster cowboy hat brim',(ex-.03,-.095,1.49),(.27,.04,.05),'shadow',bevel=False)
    box('Poster cowboy hat crown',(ex-.03,-.085,1.54),(.15,.04,.09),'shadow',bevel=False)
    for dx in (-.09,.07):
        beam('Poster silhouette leg',(ex+dx*.6,-.074,.90),(ex+dx,-.074,.68),.06,.045,'shadow')
    beam('Poster silhouette arm',(ex+.03,-.074,1.31),(ex+.24,-.074,1.22),.065,.045,'shadow')
    # Film projector, crate, reels with real negative-space spokes.
    px,py=ex,-.59
    box('Projector crate',(px,py,.22),(.58,.43,.44),'wood')
    for dx in (-.24,0,.24):
        box('Crate raised slat',(px+dx,py-.23,.22),(.045,.045,.43),'wood_light')
    for z in (.07,.37):
        box('Crate dark band',(px,py-.26,z),(.60,.045,.055),'wood_dark')
    box('Vintage projector body',(px,py,.64),(.39,.29,.34),'iron_dark')
    for dx in (-.16,.16):
        box('Projector brass corner',(px+dx,py-.16,.64),(.045,.04,.27),'brass')
    for dx in (-.16,.16):
        hoop('Open film reel',(px+dx,py,.96),.17,.045,.06,'iron',n=16)
        cylinder('Reel hub',(px+dx,py,.96),.045,.08,'brass',n=8,rot=(math.pi/2,0,0))
        for j in range(4):
            a=j*TAU/4
            rod('Reel spoke',(px+dx,py,.96),(px+dx+.14*math.cos(a),py,.96+.14*math.sin(a)),.023,'iron',n=5,bevel=False)
    cylinder('Projector lens barrel',(px,py-.22,.65),.105,.22,'brass',n=12,rot=(math.pi/2,0,0))
    cylinder('Projector glass lens',(px,py-.34,.65),.083,.04,'glass',n=12,rot=(math.pi/2,0,0))
    tube('Brass projector crank',[(px+.20,py,.60),(px+.31,py,.60),(px+.31,py,.72),(px+.38,py,.72)],.025,'brass',n=5)


def sewing_window():
    # Freestanding window, four individually framed warm panes.
    wy=.38
    for x in (-.76,.76):
        box('Window side stile',(x,wy,1.06),(.10,.18,2.0),'wood_light')
        box('Window foot',(x,wy,.065),(.26,.58,.13),'wood_dark')
        finial((x,wy,2.06),radius=.08,height=.16)
    for z in (.11,2.01):
        box('Window horizontal frame',(0,wy,z),(1.6,.18,.10),'wood_light')
        box('Frame gold moulding',(0,wy-.11,z),(1.54,.045,.045),'gold')
    for x in (-.35,.35):
        for z in (.59,1.51):
            box('Lit window pane',(x,wy,z),(.64,.055,.85),'window_lit',bevel=False)
    box('Window central mullion',(0,wy-.06,1.06),(.065,.10,1.82),'ivory')
    box('Window cross mullion',(0,wy-.06,1.06),(1.40,.10,.065),'ivory')
    rod('Curtain rod',(-.90,wy-.14,2.10),(.90,wy-.14,2.10),.035,'brass',n=8)
    for side in (-1,1):
        for j in range(4):
            x=side*(.53+j*.073)
            tube('Heavy draped curtain fold',[(x,wy-.20,2.05),(x*.97,wy-.25,1.7),
                 (side*.71,wy-.27,1.14),(x,wy-.23,.42)],.065,'curtain',n=6)
        box('Curtain gold tie',(side*.72,wy-.33,1.13),(.19,.08,.065),'gold')
    box('Wide wooden sill',(0,wy-.08,.17),(1.78,.40,.12),'wood')
    box('Little paper note',(.48,wy-.24,.25),(.25,.18,.04),'paper',rot=(0,0,-.18),bevel=False)
    # Sewing table and ornate cast iron treadle stand, placed in front of window.
    y=-.34
    box('Sewing table top',(0,y,.86),(1.10,.57,.11),'wood')
    for x in (-.52,.52):
        box('Sewing table pale end',(x,y,.86),(.055,.57,.11),'wood_light')
    for side in (-1,1):
        for yy in (y-.20,y+.20):
            beam('Cast iron splayed leg',(side*.38,yy,.81),(side*.46,yy,.07),.065,.065,'iron_dark')
        start=len(PARTS)
        scroll('Cast iron leg curl',(0,0,0),.22,side)
        # Shared gold scroll geometry recoloured as cast iron for this machine.
        for p in PARTS[start:]:
            p.role='iron_dark'
        move_group(start,loc=(side*.42,y,.48),rot=(0,0,math.pi/2))
        rod('Iron stand foot',(side*.46,y-.29,.06),(side*.46,y+.29,.06),.045,'iron_dark',n=6)
    box('Treadle plate',(0,y-.02,.19),(.62,.37,.055),'iron_dark')
    for j in range(6):
        box('Treadle brass rib',(-.25+j*.10,y-.02,.23),(.04,.32,.04),'brass')
    rod('Treadle drive rod',(.38,y+.10,.22),(.38,y+.1,.95),.026,'iron')
    # Antique machine: bed, column, overhanging neck, wheel and needle.
    box('Sewing machine bed',(0,y,.955),(.80,.34,.075),'iron_dark')
    box('Gold machine bed decal',(0,y-.18,.96),(.71,.04,.045),'gold')
    lathe('Machine upright',[(0,.11),(.08,.105),(.27,.075),(.33,.095)],'iron_dark',n=10).transform((.22,y,.99),scale=(1,.8,1))
    box('Curved sewing machine arm',(-.03,y,1.31),(.55,.16,.12),'iron_dark')
    lump('Machine round elbow',(.21,y,1.29),(.21,.18,.20),'iron_dark')
    box('Needle head',(-.29,y,1.22),(.11,.17,.22),'iron_dark')
    for x in (-.24,-.02,.19):
        scroll('Machine gold decal',(x,y-.105,1.30),.055,1)
    hoop('Brass machine hand wheel',(.38,y,1.21),.18,.045,.06,'brass',n=16,rot=(0,0,math.pi/2))
    for j in range(4):
        a=j*TAU/4
        rod('Hand wheel spoke',(.38,y,1.21),(.38,y+.145*math.cos(a),1.21+.145*math.sin(a)),.023,'brass',n=5,bevel=False)
    cylinder('Red thread spool',(.12,y,1.45),.065,.15,'thread_red',n=12)
    for z in (1.38,1.53):
        cylinder('Ivory spool cap',(.12,y,z),.085,.04,'ivory',n=12)
    tube('Thread to needle',[(.12,y-.065,1.46),(-.22,y-.10,1.38),(-.29,y-.11,1.12),(-.29,y-.11,1.01)],.02,'thread_red',n=5)
    rod('Steel sewing needle',(-.29,y,1.16),(-.29,y,1.0),.021,'iron',n=5)
    box('Needle presser foot',(-.29,y-.05,1.0),(.10,.12,.04),'brass')
    for j in range(3):
        box('Folded fabric layers',(-.19,y-.12,.99+j*.025),(.31,.27,.04),'fabric' if j%2 else 'fabric_dark',bevel=False)


def old_man_shadow():
    # The explicit silhouette colour requirement overrides the five-role prop rule.
    # His anatomical right is -X because he faces south. No right-arm geometry here.
    for x in (-.095,.095):
        cylinder('Thin trouser leg',(x,.025,.28),.052,.47,'shadow',n=8)
        box('Worn silhouette shoe',(x,0,.055),(.13,.27,.11),'shadow',bevel=False)
    n=12
    rings=[(.48,.245,.01),(.69,.235,.005),(1.06,.16,.02),(1.26,.19,.025),(1.37,.19,.015)]
    verts=[]
    for row,(z,r,y) in enumerate(rings):
        for j in range(n):
            a=j*TAU/n
            tatter=(.0,.07,-.025,.10,.02,-.015,.085,.0,.045,-.02,.06,.015)[j] if row==0 else 0
            verts.append((r*math.cos(a),y+r*.61*math.sin(a),z+tatter))
    faces=[tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    Part('Frayed long overcoat',verts,faces,'shadow',False)
    # Follow the uneven hem with a thin soft band instead of a bright decorative trim.
    for j in range(n):
        a,b=Vector(verts[j]),Vector(verts[(j+1)%n])
        rod('Smudged coat hem',a,b,.024,'shadow_blur',n=5,bevel=False)
    lump('Hunched upper back',(0,.055,1.31),(.37,.27,.26),'shadow',detail=2)
    cylinder('Bent neck',(0,-.055,1.435),.067,.16,'shadow',n=8,rot=(.20,0,0))
    for side in (-1,1):
        extrude('Raised coat collar',[(side*.025,1.37),(side*.14,1.36),(side*.11,1.49),(side*.035,1.47)],
                .12,'shadow',y=-.06,bevel=False)
    # Face is deliberately featureless: a soft patch with concentric dark falloff.
    lump('Old man faceless head',(0,-.09,1.575),(.225,.225,.275),'shadow',detail=2)
    lump('Diffuse face smudge',(0,-.192,1.57),(.17,.036,.19),'shadow_blur',detail=2)
    lump('Soft dark face center',(0,-.209,1.57),(.11,.021,.13),'shadow',detail=2)
    for j in range(3):
        rod('Sparse hair tuft',(-.04+j*.035,-.07,1.68),(-.065+j*.045,-.04,1.72-random.uniform(0,.015)),.021,'shadow',n=5,bevel=False)
    tube('Hanging left sleeve',[(.19,.015,1.355),(.25,-.01,1.08),(.245,-.04,.82)],.062,'shadow',n=8)
    cylinder('Soft left cuff',(.245,-.04,.82),.064,.055,'shadow_blur',n=8)
    lump('Thin left hand',(.245,-.055,.735),(.075,.08,.15),'shadow')
    for part in PARTS:
        for v in part.verts:
            v.z = min(1.72, v.z)
    print('SHOULDER_R ' + ' '.join(f'{v:.3f}' for v in SHOULDER_R),flush=True)


def old_man_arm():
    # Top ring is exactly the pivot plane; bottom tip is precisely -.68.
    profile=[(z,r*random.uniform(.97,1.03)) for z,r in
             [(-.52,.048),(-.46,.055),(-.27,.060),(-.05,.065),(0,.055)]]
    lathe('Hanging right sleeve',profile,
          'shadow',n=9,bevel=False)
    cylinder('Soft right cuff',(0,0,-.515),.053,.045,'shadow_blur',n=9)
    lathe('Thin bony right hand',[(-.68,.018),(-.655,.033),(-.56,.037),(-.535,.026)],
          'shadow',n=8,bevel=False).transform(scale=(1,.68,1))
    rod('Bony thumb',(.026,-.005,-.565),(.055,-.005,-.62),.021,'shadow',n=5,bevel=False)


SPECS = (
    # name, optional dimensions, centered, builder, triangle budget
    ('StandMirror', None, False, stand_mirror, 7000),
    ('PlazaMirror', None, False, plaza_mirror, 9500),
    ('MirrorPuddle', None, False, mirror_puddle, 2500),
    ('CuckooBench', None, False, cuckoo_bench, 6000),
    ('HourglassBench', None, False, hourglass_bench, 6000),
    ('CoatPoster', None, False, coat_poster, 6000),
    ('SewingWindow', None, False, sewing_window, 6000),
    ('OldManShadow', None, False, old_man_shadow, 3000),
    ('OldManArm', None, False, old_man_arm, 600),
)




def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min, max)]


def parts_object(name, parts, bevel=False, glass=False):
    verts, faces, indices, vertex_uvs = [], [], [], []
    for p in parts:
        offset = len(verts)
        verts.extend(p.verts)
        vertex_uvs.extend(p.uvs or [(0.,0.)] * len(p.verts))
        faces.extend(tuple(offset+i for i in f) for f in p.faces)
        indices.extend([ROLES.index(p.role)] * len(p.faces))
    if not verts:
        return None
    data = bpy.data.meshes.new(name + '_parts')
    data.from_pydata(verts, [], faces)
    for role in ROLES:
        data.materials.append(MATS[role])
    for f,index in zip(data.polygons,indices):
        f.material_index = index
    if not glass:
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=list(bm.edges))
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
    uv = data.uv_layers.new(name='UVMap', do_init=False)
    for item in uv.data:
        item.uv = (0., 0.)
    if glass:
        for loop in data.loops:
            uv.data[loop.index].uv = vertex_uvs[loop.vertex_index]
    obj = bpy.data.objects.new(name + '_parts',data)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if bevel:
        mod = obj.modifiers.new('Worn edges','BEVEL')
        mod.width,mod.segments = .025,2
        mod.limit_method,mod.angle_limit = 'ANGLE',math.radians(45)
        mod.use_clamp_overlap = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)
    return obj


def assembled_mesh(name):
    # Copy the platform batching/bevel framework, deferring reflection geometry.
    objects=[]
    for bevel in (True,False):
        group=[p for p in PARTS if p.bevel == bevel and p.role != 'mirror_glass']
        obj=parts_object(name,group,bevel)
        if obj:
            objects.append(obj)
    glass_parts=[p for p in PARTS if p.role == 'mirror_glass']
    if glass_parts:
        assert len(glass_parts) == 1, 'Exactly one planar reflection region'
        glass=parts_object(name+'_protected_glass',glass_parts,glass=True)
        assert triangles(glass) <= 64
        GLASS_OBJECTS[name]=glass
        GLASS_CHECKS[name]=[(v.copy(),uv) for v,uv in zip(glass_parts[0].verts,glass_parts[0].uvs)]
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:
        bpy.ops.object.join()
    obj=bpy.context.object
    obj.name,obj.data.name=name,name+'_Mesh'
    return obj


def validate_mesh(obj, name, offset, budget):
    me=obj.data
    assert len(me.uv_layers)==1 and me.uv_layers[0].name=='UVMap', name
    assert triangles(obj)<=budget, (name,triangles(obj),budget)
    bad = [(p.index, p.area, me.materials[p.material_index].name) for p in me.polygons
           if len(p.vertices)<3 or p.area<=1e-12]
    assert not bad, (name, bad[:8])
    uv=me.uv_layers[0]
    roles={me.materials[p.material_index].name for p in me.polygons}
    assert len(roles)>=(2 if name in ('OldManShadow','OldManArm') else 5), (name,roles)
    glass_faces=[]
    for p in me.polygons:
        if me.materials[p.material_index].name=='mirror_glass':
            glass_faces.append(p)
        else:
            assert all(uv.data[i].uv.length<1e-6 for i in p.loop_indices), (name,'Non-glass UV')
    if name in GLASS_CHECKS:
        expected=GLASS_CHECKS[name]
        assert len(glass_faces)==len(expected)-1 and len(glass_faces)<=64
        normal=glass_faces[0].normal
        point=me.vertices[me.loops[glass_faces[0].loop_start].vertex_index].co
        for p in glass_faces:
            assert p.normal.dot(normal)>.99999, (name,'Glass winding')
            for li in p.loop_indices:
                v=me.vertices[me.loops[li].vertex_index].co
                assert abs((v-point).dot(normal))<1e-5, (name,'Nonplanar glass')
                matches=[coord for pos,coord in expected if (pos-offset-v).length<1e-5]
                assert matches and (uv.data[li].uv-Vector(matches[0])).length<1e-5, (name,'Glass UV changed')
        assert normal.z>.99 if name=='MirrorPuddle' else normal.y<-.8, (name,'Front normal')
    lo,hi=bounds(obj)
    if name not in ('MirrorPuddle','OldManShadow','OldManArm'):
        assert abs(lo.z)<1e-5 and abs(lo.x+hi.x)<1e-5 and abs(lo.y+hi.y)<1e-5, name
    if name=='MirrorPuddle':
        assert hi.z-lo.z<=.15 and min(v.co.z for v in me.vertices)>=0, name
    if name=='OldManArm':
        assert abs(hi.z)<1e-5 and abs(lo.z+.68)<1e-5, name
    print('CHECKED '+name+' UVMap / origins / budget',flush=True)


def finish(name, centered, budget):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    glass = GLASS_OBJECTS.pop(name, None)
    solid_budget = budget - (triangles(glass) if glass else 0)
    if triangles(obj) > solid_budget:
        mod = obj.modifiers.new('Budget', 'DECIMATE')
        mod.ratio, mod.delimit = (solid_budget * .96) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Material boundaries can constrain collapse. Recheck the actual result.
    for attempt in range(3):
        if triangles(obj) <= solid_budget:
            break
        mod = obj.modifiers.new('Budget remainder', 'DECIMATE')
        mod.ratio, mod.delimit = (solid_budget * .94) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= solid_budget, (name, triangles(obj), solid_budget)
    # Collapse/bevel can leave zero-area slivers at narrow moulding corners.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-7, edges=list(bm.edges))
    dead = [f for f in bm.faces if f.calc_area() <= 1e-8]
    if dead:
        bmesh.ops.delete(bm, geom=dead, context='FACES_ONLY')
    bm.to_mesh(obj.data)
    bm.free()
    if glass:
        # Joining AFTER decimation prevents any glass edge/UV collapse.
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        glass.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.join()
    lo, hi = bounds(obj)
    mid = (hi + lo) / 2
    # Only the water/character attachment origins retain authored coordinates.
    keep_origin = name in ('MirrorPuddle', 'OldManShadow', 'OldManArm')
    offset = Vector((0,0,0)) if keep_origin else Vector((mid.x,mid.y,mid.z if centered else lo.z))
    if not keep_origin:
        for v in obj.data.vertices:
            v.co.x -= mid.x
            v.co.y -= mid.y
            v.co.z -= mid.z if centered else lo.z
    obj.data.update()
    validate_mesh(obj, name, offset, budget)
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    destination = OUTPUT / ('Kit_Q_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 3) for d in obj.dimensions)}', flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 2400, 1500
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    shading = scene.display.shading
    shading.light, shading.color_type = 'STUDIO', 'MATERIAL'
    shading.show_cavity, shading.show_object_outline = True, True
    shading.object_outline_color = (0,0,0)
    shading.background_type = 'WORLD'
    shading.show_shadows = True
    shading.show_specular_highlight = False
    shading.cavity_type = 'BOTH'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.12, .1, .22)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    by_name={obj.name:obj for obj in objects}
    arm=by_name['OldManArm']
    man=by_name['OldManShadow']
    row1=[by_name[n] for n in ('StandMirror','PlazaMirror','MirrorPuddle')]
    row2=[by_name[n] for n in ('CuckooBench','HourglassBench','CoatPoster','SewingWindow','OldManShadow')]
    bpy.context.view_layer.update()
    def lay(row, y, gap):
        # Object.dimensions are local-axis extents; rotated props need world bounds.
        extents = []
        for obj in row:
            xs = [(obj.matrix_world @ Vector(c)).x for c in obj.bound_box]
            extents.append((min(xs),max(xs)))
        total = sum(hi-lo for lo,hi in extents) + gap * (len(row) - 1)
        x = -total / 2
        for obj,(lo,hi) in zip(row,extents):
            obj.location = (x-lo,y,0)
            x += hi-lo+gap
        return total
    lay(row1, 3.0, 1.8)
    lay(row2, -3.5, .95)
    arm.location = man.location + SHOULDER_R
    bpy.context.view_layer.update()
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    a = math.radians(55)
    target = Vector((0,0,0))
    cam.location = target + Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    # Fit transformed bounds in camera space, allowing for the tall post tops.
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    corners = [inv @ (obj.matrix_world @ Vector(c)) for obj in objects for c in obj.bound_box]
    left,right = min(v.x for v in corners),max(v.x for v in corners)
    low,high = min(v.y for v in corners),max(v.y for v in corners)
    center = Vector(((left+right)/2,(low+high)/2,0))
    cam.location += cam.rotation_euler.to_matrix() @ center
    aspect = scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale = max(right-left,(high-low)*aspect)*1.12
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    print('PREVIEW ' + str(PREVIEW), flush=True)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    out = []
    for i, (name, dims, centered, build, budget) in enumerate(SPECS):
        PARTS = []
        random.seed(51150 + i * 977)
        build()
        out.append(finish(name, centered, budget))
    preview(out)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
