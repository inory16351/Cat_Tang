# 새벽의 부서진 시간 마을 소품 12종과 검토용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_dream.py
"""Deterministic broken-time scatter kit; stock Blender 5.3, metres, +Y front.

Export shaders are flat grey role placeholders, including all luminous roles.
The user's cool-role and no-floating-dust overrides supersede the common spec.
Only substantial designed fragments float; there is no generic particle pass.
All twelve exports precede the review-only colour/placement changes.
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
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_dream_preview.png'
TAU = math.tau
ROLES = (
    'wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
    'stone', 'stone_dark', 'rock', 'earth', 'sand_glow', 'brass', 'gold',
    'gold_drip', 'iron', 'rust', 'clock_face', 'cactus_dark', 'stardust',
    'crystal_blue', 'crystal_core', 'rift_glow', 'moon', 'glass_blue',
)
HARD = set(ROLES) - {
    'sand_glow', 'stardust', 'rift_glow', 'crystal_core', 'glass_blue',
    'crystal_blue', 'cactus_dark', 'earth', 'gold_drip',
}
PALETTE = {
    'wood': (.48, .25, .105), 'wood_dark': (.19, .09, .055),
    'wood_light': (.77, .53, .28), 'wood_weathered': (.54, .41, .31),
    'plank': (.64, .38, .19), 'stone': (.68, .60, .48),
    'stone_dark': (.25, .28, .34), 'rock': (.45, .48, .55),
    'earth': (.36, .22, .16), 'sand_glow': (1, .76, .27),
    'brass': (.64, .38, .12), 'gold': (1, .69, .19),
    'gold_drip': (1, .47, .075), 'iron': (.13, .17, .22),
    'rust': (.62, .22, .10), 'clock_face': (.96, .91, .75),
    'cactus_dark': (.19, .32, .21), 'stardust': (1, .94, .69),
    'crystal_blue': (.36, .66, .91), 'crystal_core': (.74, .97, 1),
    'rift_glow': (.90, 1, 1), 'moon': (.83, .87, .96),
    'glass_blue': (.25, .48, .64),
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
    start = len(PARTS)
    cylinder('Clock iron back', (0,-.045,0), radius,.13,'iron',n=24,rot=(math.pi/2,0,0))
    cylinder('Cream enamel dial', (0,.045,0),radius*.86,.07,'clock_face',n=24,rot=(math.pi/2,0,0))
    hoop('Brass clock bezel',(0,.075,0),radius,.13*radius,.15,'brass')
    hoop('Gold inner bezel',(0,.12,0),radius*.84,.045,.06,'gold')
    for j in range(12):
        a = j*TAU/12
        box('Gold hour index',(radius*.69*math.sin(a),.13,radius*.69*math.cos(a)),
            (.045,.05,radius*.12),'gold',rot=(0,a,0))
    if hands:
        beam('Minute hand',(0,.19,0),(-radius*.32,.19,radius*.55),.065,.055,'iron')
        beam('Hour hand',(0,.20,0),(radius*.44,.20,radius*.18),.09,.06,'iron')
        cylinder('Clock pivot',(0,.24,0),.07,.06,'gold',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc)


def gear(loc, radius, depth=.20, role='brass', teeth=16, broken=(), rot=(0,0,0)):
    """Spoked, genuinely hollow gear; separate trapezoidal teeth can break off."""
    start = len(PARTS)
    hoop('Gear annulus',(0,0,0),radius*.85,radius*.20,depth,role,n=teeth*2)
    hoop('Gold chased gear lip',(0,depth*.53,0),radius*.79,.045,.05,'gold',n=teeth*2)
    cylinder('Iron axle hub',(0,0,0),radius*.23,depth*1.65,'iron',n=12,rot=(math.pi/2,0,0))
    for j in range(6):
        a = j*TAU/6
        beam('Gold gear spoke',(radius*.19*math.cos(a),0,radius*.19*math.sin(a)),
             (radius*.70*math.cos(a),0,radius*.70*math.sin(a)),
             max(.07,radius*.11),depth*.72,'gold')
    for j in range(teeth):
        a = j*TAU/teeth
        width = TAU/teeth*.32
        if j in broken:
            # A visible stepped stub, rather than simply omitting every tooth.
            outer = radius*(.89 if j % 2 else .94)
        else:
            outer = radius
        poly = [(r*math.cos(a+da),r*math.sin(a+da)) for r,da in
                ((radius*.82,-width),(outer,-width*.72),
                 (outer,width*.72),(radius*.82,width))]
        extrude('Broken tooth' if j in broken else 'Broad gear tooth',poly,depth,role)
    for j in (1,6,10):
        a = j*TAU/teeth
        sector('Rust scar on gear',radius*.66,radius*.84,a-.13,a+.15,.05,'rust',y=depth*.57,steps=2)
    cylinder('Axle retaining button',(0,depth,0),radius*.12,.07,'brass',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc,rot)


def crystal(label, a, b, radius, core=True):
    """Faceted solid with an exposed inset core, readable with opaque shaders."""
    a,b = Vector(a),Vector(b)
    length = (b-a).length
    verts = [(r*math.cos(j*TAU/6),r*math.sin(j*TAU/6),z)
             for z,r in ((0,radius*.77),(length*.74,radius)) for j in range(6)]
    verts.append((radius*.14,-radius*.10,length))
    faces = [tuple(reversed(range(6)))]
    faces += [(j,(j+1) % 6,(j+1) % 6+6,j+6) for j in range(6)]
    faces += [(j+6,(j+1) % 6+6,12) for j in range(6)]
    # +Y facet is j=1. Inset is a real material region, not a hidden core.
    if core:
        outer = faces[2]
        mid = sum((Vector(verts[i]) for i in outer),Vector())/4
        inner = []
        for i in outer:
            v = mid+(Vector(verts[i])-mid)*.90
            v.y -= radius*.03
            inner.append(len(verts))
            verts.append(tuple(v))
        faces[2] = tuple(inner)
        faces += [(outer[j],outer[(j+1) % 4],inner[(j+1) % 4],inner[j]) for j in range(4)]
    part = Part(label,verts,faces,'crystal_blue',False)
    # A separate front sliver fills the inset, making the role unambiguous.
    if core:
        inset = [Vector(verts[i]) for i in faces[2]]
        core_verts = [tuple(v+Vector((0,dy,0))) for dy in (-.022,.022) for v in inset]
        core_faces = [(3,2,1,0),(4,5,6,7)]
        core_faces += [(j,(j+1) % 4,(j+1) % 4+4,j+4) for j in range(4)]
        Part(label+' exposed core',core_verts,core_faces,'crystal_core',False)
    # Minimal Z alignment keeps the exposed core on +Y, including vertical
    # prisms (track/up quaternions can introduce a 180-degree roll there).
    align = Vector((0,0,1)).rotation_difference((b-a).normalized())
    matrix = Matrix.Translation(a) @ align.to_matrix().to_4x4()
    for p in PARTS[-2 if core else -1:]:
        p.verts = [matrix @ v for v in p.verts]
    return part


def cracked_base(width, depth, height=.22, roles=('stone_dark','rock','earth')):
    """Four separate heaved slabs with actual dark gaps; no floating dust."""
    for j,(x,y) in enumerate(((-.25,-.25),(.25,-.25),(-.25,.25),(.25,.25))):
        box('Heaved foundation slab',(x*width,y*depth,height*.53),
            (width*.46,depth*.45,height),roles[j % len(roles)],
            rot=(.035*(j-1),.045*(2-j),.035*(-1)**j))


def minute_hand():
    """The ornate giant minute hand alone. Local Z = length, tip at 0, ring at the tail."""
    outline = [(0,0),(-.47,1.03),(-.22,.91),(-.19,4.45),
               (-.37,4.76),(-.22,5.01),(-.14,5.50),(.14,5.50),
               (.22,5.01),(.37,4.76),(.19,4.45),(.22,.91),(.47,1.03)]
    extrude('Gigantic iron minute hand',outline,.20,'iron')
    extrude('Gold lancet inlay',[(0,.38),(-.13,1.02),(-.065,4.30),
                                (0,4.55),(.065,4.30),(.13,1.02)],.055,'gold',y=.125)
    hoop('Oversize open brass counterweight',(0,0,5.86),.72,.18,.25,'brass',n=24)
    hoop('Counterweight inner gold chase',(0,.15,5.86),.51,.055,.055,'gold',n=24)
    cylinder('Counterweight riveted pivot',(0,.20,4.78),.18,.13,'brass',n=10,rot=(math.pi/2,0,0))
    for side in (-1,1):
        for z in (3.65,4.15):
            scroll('Gold hand filigree',(side*.18,.17,z),.27,side)
    for z in (1.40,2.50,3.30):
        box('Iron blade rust scar',(.14,.115,z),(.075,.05,.32),'rust',rot=(0,.14,0))


def clock_hand_giant():
    cracked_base(1.9,2.0,.32,('earth','rock','earth'))
    for x,y,z,s in ((-.67,-.10,.59,.24),(.65,.32,.73,.27),(.36,-.57,.45,.19)):
        lump('Impact rock chip frozen above crack',(x,y,z),(s,s*.8,s*.85),'rock')
    start = len(PARTS)
    minute_hand()
    # The hand rises 35 degrees from the terrain along the long Y footprint.
    move_group(start,(0,-2.70,.08),(-math.radians(55),0,0),scale=(1,-1,1))
    for side in (-1,1):
        box('Impact upturned stone',(side*.70,-1.91,.32),(.48,.88,.28),
            'rock',rot=(side*.16,side*.31,side*.27))


def clock_hand_float():
    """2026-11-18 user: no slabs; the same hand floats and slowly circles the plaza clock tower.
    Lies flat with the gold inlay face up (+Z), tip toward +Y, counterweight ring toward -Y."""
    start = len(PARTS)
    minute_hand()
    # Seen from above while it flies: an enamel dial inside the open counterweight and a
    # glowing time-crystal on the pivot (also meets the 5-role ornate rule).
    cylinder('Counterweight enamel dial',(0,-.02,5.86),.53,.07,'clock_face',n=20,rot=(math.pi/2,0,0))
    lump('Pivot time-crystal',(0,.30,4.78),(.22,.16,.22),'crystal_core')
    move_group(start,(0,0,0),(math.radians(90),0,0))


def gear_half():
    cracked_base(3.7,1.10,.18,('earth','stone_dark','rock'))
    start = len(PARTS)
    # A true buried silhouette: trim every main-gear solid at terrain Z=0.
    gear((-.18,-.12,.82),1.91,.27,'brass',teeth=22,broken=(2,3,7,8,15))
    kept = []
    for p in PARTS[start:]:
        if max(v.z for v in p.verts) <= .06:
            continue
        if min(v.z for v in p.verts) < .06:
            bm = bmesh.new()
            vs = [bm.verts.new(v) for v in p.verts]
            for f in p.faces:
                bm.faces.new([vs[i] for i in f])
            bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                                   dist=1e-7,plane_co=(0,0,.06),plane_no=(0,0,1),
                                   clear_inner=True,clear_outer=False)
            boundary = [e for e in bm.edges if e.is_boundary]
            if boundary:
                bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
            bm.verts.ensure_lookup_table()
            bm.verts.index_update()
            p.verts = [v.co.copy() for v in bm.verts]
            p.faces = [tuple(v.index for v in f.verts) for f in bm.faces]
            bm.free()
        kept.append(p)
    PARTS[start:] = kept
    gear((.94,.40,.60),.78,.19,'brass',teeth=13,broken=(3,),rot=(.23,.12,-.16))
    for x in (-1.55,-.88,1.56):
        box('Broken gear tooth resting in soil',(x,.31,.19),(.28,.30,.27),'rust',rot=(.1,.2,x*.3))
    cylinder('Large hub gold boss',(-.18,.18,.82),.27,.13,'gold',n=10,rot=(math.pi/2,0,0))


def gear_stack_float():
    # Gears are XY plates stacked along their shared Z axle. The whole joined
    # assembly has a centre pivot so the game's Y-axis spin remains centred.
    for z,r,role,angle in ((-.43,1.10,'brass',.04),(0,.84,'gold',.19),(.43,.64,'rust',-.14)):
        gear((0,0,z),r,.16,role,teeth=16,broken=(3,10),rot=(math.pi/2,0,angle))
    cylinder('Shared iron axle',(0,0,0),.12,1.28,'iron',n=10)
    for z in (-.60,.61):
        cylinder('Brass axle locknut',(0,0,z),.21,.12,'brass',n=6)
    for z in (-.22,.22):
        hoop('Blue time spacer',(0,0,z),.27,.09,.075,'crystal_blue',n=12,rot=(math.pi/2,0,0))
        cylinder('Luminous exposed axle collar',(0,0,z+.055),.16,.06,'crystal_core',n=8)


def clock_shards():
    # One irregular central island + seven irregular perimeter shards = eight.
    # Adjacent shards share the SAME jagged radial boundary before separation.
    angles = [-.09,.77,1.77,2.58,3.58,4.35,5.39]
    radii = [.35,.46,.34,.45,.31,.48,.39]
    boundaries = []
    for j,a in enumerate(angles):
        boundaries.append([(radii[j]*math.cos(a),radii[j]*math.sin(a)),
                           (.70*math.cos(a+.09*(-1)**j),.70*math.sin(a+.09*(-1)**j)),
                           (.89*math.cos(a-.055*(-1)**j),.89*math.sin(a-.055*(-1)**j)),
                           (1.25*math.cos(a),1.25*math.sin(a))])
    central = [b[0] for b in boundaries]
    extrude('Central shattered enamel island',central,.18,'clock_face',y=-.08)
    for j in range(7):
        tube('Light leaking around central fracture',
             [(x,.025,z) for x,z in (central[j],central[(j+1) % 7])],.022,'rift_glow',n=4)
    for j in range(7):
        a,b = angles[j],angles[(j+1) % 7]+(TAU if j == 6 else 0)
        mid = (a+b)/2
        start = len(PARTS)
        arc = [(1.25*math.cos(t),1.25*math.sin(t)) for t in
               [a+(b-a)*k/5 for k in range(1,5)]]
        polygon = boundaries[j]+arc+list(reversed(boundaries[(j+1) % 7]))
        extrude('Jagged enamel clock shard '+str(j+1),polygon,.18,'clock_face')
        sector('Shattered brass bezel',1.23,1.36,a+.012,b-.012,.23,'brass',steps=5)
        sector('Shattered gold rim',1.18,1.24,a+.025,b-.025,.055,'gold',y=.14,steps=5)
        for edge in (boundaries[j],boundaries[(j+1) % 7]):
            tube('Thin fracture light sliver',[(x,.115,z) for x,z in edge],.023,'rift_glow',n=4)
        for k in range(12):
            t = k*TAU/12+math.pi/2
            wrapped = t
            while wrapped < a:
                wrapped += TAU
            if a+.045 < wrapped < b-.045:
                numeral(('XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI')[(-k) % 12],
                        (1.02*math.cos(t),.15,1.02*math.sin(t)),.19,'gold',angle=t-math.pi/2)
        offset = .12+(.04 if j % 3 == 0 else 0)
        move_group(start,(offset*math.cos(mid),(.13,-.06,.08,-.10,.07,.02,.16)[j],
                          offset*math.sin(mid)),rot=(.025*(-1)**j,.025*(j-3),.015*j))
    # Both hands are physically split, with disconnected substantial tips.
    beam('Broken minute hand root',(0,.23,0),(-.20,.23,.35),.10,.07,'iron')
    beam('Separated minute hand tip',(-.27,.32,.48),(-.53,.32,.94),.08,.07,'iron')
    beam('Broken hour hand root',(.03,.26,-.02),(.29,.26,-.16),.12,.08,'iron')
    beam('Separated hour hand tip',(.42,.34,-.23),(.77,.34,-.39),.10,.08,'iron')
    cylinder('Broken clock gold pivot',(0,.30,0),.12,.10,'gold',n=10,rot=(math.pi/2,0,0))
    box('Rust hinge still on rim',(-1.27,.12,-.30),(.15,.10,.33),'rust',rot=(0,-.2,0))


def glass_bulb(label, rings, missing=()):
    """Thick individual glass panels; omitted front panels expose the sand."""
    n = 10
    for row in range(len(rings)-1):
        z0,r0 = rings[row]
        z1,r1 = rings[row+1]
        for j in range(n):
            if (row,j) in missing:
                continue
            a,b = j*TAU/n,(j+1)*TAU/n
            verts = [(r*math.cos(t),r*math.sin(t),z) for dr in (0,-.045)
                     for r,z,t in ((r0+dr,z0,a),(r0+dr,z0,b),(r1+dr,z1,b),(r1+dr,z1,a))]
            Part(label,verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),
                              (2,3,7,6),(3,0,4,7)],'glass_blue',False)


def hourglass_broken():
    box('Carved stone pedestal',(0,0,.14),(1.65,1.65,.28),'stone')
    box('Dark stone pedestal bevel band',(0,0,.33),(1.49,1.49,.12),'stone_dark')
    for x in (-.52,0,.52):
        extrude('Pedestal carved gold hourglass',[(x-.09,.13),(x+.09,.13),(x,.25)],.06,'gold',y=.85)
    for z in (.50,3.05):
        if z < 1:
            cylinder('Octagonal brass frame foot',(0,0,z),.87,.18,'brass',n=12)
        else:
            # An open crown leaves the rising sand readable from above.
            hoop('Open gold hourglass crown',(0,0,z),.87,.19,.18,
                 'gold',n=12,rot=(math.pi/2,0,0))
        hoop('Gold frame molding',(0,0,z+.10),.84,.07,.07,'gold',n=12,rot=(math.pi/2,0,0))
    for j in range(4):
        a = math.pi/4+j*TAU/4
        x,y = .78*math.cos(a),.78*math.sin(a)
        rod('Hourglass carved brass column',(x,y,.60),(x,y,2.96),.065,'brass',n=8)
        for z in (.77,1.73,2.83):
            cylinder('Gold post collar',(x,y,z),.10,.09,'gold',n=8)
    glass_bulb('Intact lower blue glass bulb',[(.66,.54),(1.02,.62),(1.40,.30),(1.72,.10)],
               missing=((0,2),(1,2)))
    glass_bulb('Cracked upper blue glass bulb',[(1.73,.10),(2.04,.28),(2.49,.61),(2.94,.54)],
               missing=((1,1),(1,2),(2,1),(2,2)))
    cylinder('Lower glowing sand bed',(0,0,.73),.46,.09,'sand_glow',n=12,top=.34)
    # Thick ribbon rises OUT through the broken upper bulb; not a dust trail.
    points = [(0,.13,1.73),(.14,.31,2.09),(.39,.52,2.42),(.74,.51,2.69),
              (.90,.32,3.02),(.85,.11,3.34),(.63,.03,3.56)]
    tube('Impossible upward sand ribbon',points,.105,'sand_glow',n=6)
    lump('Floating cohesive sand clump',(.59,.02,3.67),(.52,.43,.37),'sand_glow')
    hoop('Iron hourglass neck clamp',(0,0,1.73),.17,.06,.09,'iron',n=10,rot=(math.pi/2,0,0))
    # Two large shards are recognisable parts of the shattered bulb.
    extrude('Suspended blue bulb shard',[(.43,2.49),(.63,2.78),(.71,2.45)],.07,'glass_blue',y=.63)
    tube('Bright broken glass seam',[(.22,.22,2.03),(.34,.48,2.25),(.53,.59,2.47)],.024,'rift_glow',n=4)


def time_rift():
    cracked_base(1.48,.58,.18,('stone_dark','rock','stone_dark'))
    # Bottom of luminous tear is 0.4m above the stone footing.
    path = [(-.12,.43),(.16,.79),(-.18,1.18),(.21,1.57),(-.13,2.0),(.18,2.43),(.02,2.98)]
    widths = [.08,.16,.13,.17,.15,.11,.025]
    polygon = [(x-w,z) for (x,z),w in zip(path,widths)]
    polygon += [(x+w,z) for (x,z),w in reversed(list(zip(path,widths)))]
    extrude('White cyan zigzag tear in air',polygon,.13,'rift_glow',y=.02,bevel=False)
    for side in (-1,1):
        for j in range(4):
            z = .58+j*.57
            x = side*(.19+.035*(j % 2))
            crystal('Rift growing edge splinter',(x,0,z),
                    (x+side*(.20+.045*(j % 2)),.02,z+.48),.105,core=(j % 2 == 0))
    # Clock debris aims at the slit: a gear, a Roman IX and a bent hand.
    gear((-.56,.11,1.19),.26,.09,'brass',teeth=10,broken=(2,),rot=(.3,.4,.25))
    numeral('IX',(.55,.15,2.14),.30,'gold',angle=-.38)
    beam('Clock hand pulled into rift',(.63,.17,.55),(.13,.03,1.03),.09,.08,'iron')
    lump('Exposed rift footing mineral',(-.41,.19,.17),(.43,.34,.22),'earth')


def pendulum_frozen():
    box('Grandfather clock broad plinth',(0,0,.16),(1.86,.80,.32),'wood_dark')
    box('Gold plinth molding',(0,.04,.34),(1.92,.85,.09),'gold')
    box('Pedestal weathered apron',(0,.43,.19),(1.60,.06,.16),'wood_weathered')
    for x in (-.77,.77):
        box('Open grandfather clock upright',(x,-.13,2.11),(.20,.28,3.52),'wood_dark')
        box('Gold post front inlay',(x,.025,2.11),(.065,.055,3.35),'gold')
        box('Post base brass shoe',(x,-.11,.53),(.24,.31,.25),'brass')
    # Back rails and a broken arch leave most of the pendulum aperture empty.
    box('Broken back brace',(-.28,-.24,1.13),(1.11,.15,.15),'wood_weathered',rot=(0,-.19,0))
    extrude('Broken clock crest',[(-.94,3.67),(-.94,3.96),(-.66,4.10),(-.38,4.29),
                                  (-.08,4.40),(.12,4.28),(.03,4.10),(.23,4.01),
                                  (.10,3.89),(.58,3.82),(.94,3.90),(.94,3.67)],.36,'wood_dark',y=-.12)
    for side in (-1,1):
        scroll('Grandfather gold crest scroll',(side*.51,.11,3.90),.24,side)
    clock((0,.13,3.42),.48)
    # Pivot to bob is exactly 40deg in the builder's metre coordinates.
    pivot = Vector((-.52,.14,2.98))
    bob = pivot+Vector((math.sin(math.radians(40))*2.10,0,-math.cos(math.radians(40))*2.10))
    rod('Pendulum rod stopped at forty degrees',pivot,bob,.055,'brass',n=8)
    cylinder('Large gold pendulum bob',bob,.40,.16,'gold',n=16,rot=(math.pi/2,0,0))
    hoop('Brass bob circumference',bob+Vector((0,.10,0)),.41,.07,.065,'brass',n=16)
    cylinder('Bob iron engraved boss',bob+Vector((0,.15,0)),.11,.055,'iron',n=8,rot=(math.pi/2,0,0))
    # Three flat shards collectively read as a fallen cracked front pane.
    start = len(PARTS)
    extrude('Fallen glass left pane',[(-.68,-.40),(-.14,-.40),(.03,.03),(-.25,.12),(-.68,.37)],.06,'glass_blue')
    extrude('Fallen glass right pane',[(-.08,-.40),(.66,-.40),(.66,.34),(.11,.05)],.06,'glass_blue')
    extrude('Fallen glass upper pane',[(-.65,.44),(-.25,.20),(.08,.12),(.62,.42)],.06,'glass_blue')
    move_group(start,(0,.51,.07),(math.pi/2,0,.11))


def melt_watch_rock():
    lump('Broad faceted boulder',(0,0,.51),(1.72,1.20,1.09),'rock',detail=2)
    lump('Dark cleft in rock',(-.53,.15,.30),(.47,.70,.49),'stone_dark')
    start = len(PARTS)
    clock((0,0,0),.65)
    hoop('Pocket watch winding bow',(0,0,.83),.17,.055,.09,'brass',n=12)
    cylinder('Watch crown',(0,0,.68),.105,.12,'gold',n=8)
    # Bend the ENTIRE watch surface, numerals, bezel and hands together across
    # a rounded ledge. The upper portion lies on top, the lower droops forward.
    for p in PARTS[start:]:
        bent = []
        for v in p.verts:
            x,d,t = v
            if t >= -.08:
                y,z = -.21-t*.72,1.08+.06*math.sin(t*2)
                normal = Vector((0,.15,1)).normalized()
            else:
                angle = min(1.48,(-t-.08)*2.55)
                y = -.21+.058+.40*math.sin(angle)
                z = 1.075-.40*(1-math.cos(angle))
                normal = Vector((0,math.sin(angle),math.cos(angle)))
            bent.append(Vector((x*(1+.10*max(0,-t)),y,z))+d*normal)
        p.verts = bent
    for x,length in ((-.38,.33),(-.08,.57),(.25,.43)):
        tube('Molten watch gold dripping over ledge',[(x,.46,.94),(x+.03,.55,.70),
              (x-.015,.56,.94-length)],.065,'gold_drip',n=6)
        lump('Gold drip rounded end',(x-.015,.56,.94-length),(.15,.13,.17),'gold_drip')
    # Bold interlocked links snake down onto the ground, not a thin line.
    for j in range(11):
        t = j/10
        hoop('Trailing pocket watch chain link',(-.47+t*.98,-.56+.21*math.sin(t*5),
             .10+max(0,.30-t)*2),.095,.04,.055,'brass',n=10,
             rot=(math.pi/2 if j % 2 == 0 else .4,0,.4*t))
    lump('Moss at watch rock foot',(.57,.29,.15),(.40,.35,.18),'cactus_dark')


def stairs_float():
    # Length is X; player sees broad step faces and the ripped masonry end.
    for j in range(4):
        x,z = -1.04+j*.70,-.75+j*.48
        box('Chunky stone stair riser',(x,0,z),(.70,1.39,.39),'stone')
        for k in range(4):
            box('Individual stair tread plank',(x,(k-1.5)*.34,z+.23),(.75,.32,.12),
                ('plank','wood_light','wood_weathered','plank')[(k+j) % 4])
        for y in (-.61,.61):
            cylinder('Tread brass spike',(x,y,z+.30),.04,.045,'brass',n=6)
    for y in (-.60,.60):
        beam('Torn dark wood stair stringer',(-1.36,y,-1.03),(1.36,y,.83),.20,.18,'wood_dark')
    for x,z in ((-.95,-.39),(-.23,.10),(.47,.59)):
        rod('Remaining iron railing upright',(x,-.68,z),(x,-.68,z+.68),.045,'iron')
    tube('Broken iron stair handrail',[(-1.28,-.68,.08),(-.95,-.68,.32),
          (-.23,-.68,.80),(.47,-.68,1.28),(.69,-.63,1.22)],.06,'iron',n=6)
    for x,y,z in ((1.39,.57,.43),(1.31,-.28,.60),(1.04,.04,.24)):
        lump('Masonry clinging to ripped upper stair',(x,y,z),(.54,.55,.57),'rock')
    lump('Soil trapped in torn stair stringers',(1.23,.39,.29),(.72,.78,.52),'earth')
    for y in (-.52,.34):
        extrude('Splintered stringer end',[(1.15,.39),(1.67,.65),(1.32,.67),(1.57,.89),(1.13,.73)],
                .13,'wood_light',y=y)


def time_crystal():
    cracked_base(1.65,1.40,.22,('stone_dark','rock','stone_dark'))
    for a,b,r in (((-.13,-.18,.13),(-.22,-.17,2.11),.30),
                   ((-.53,.05,.12),(-.74,.05,1.39),.25),
                   ((.40,.12,.13),(.65,.17,1.68),.29),
                   ((.08,.44,.12),(.18,.55,.95),.23),
                   ((.46,-.39,.12),(.74,-.47,.78),.21)):
        crystal('Tall exposed time crystal',a,b,r)
    # Opaque-body compatible dial: actually recessed into the +Y hex facet.
    # It overlaps the exposed core window, and a blue lip partly occludes it.
    start = len(PARTS)
    cylinder('Enamel dial trapped in crystal',(-.18,.045,1.13),.17,.045,
             'clock_face',n=16,rot=(math.pi/2,0,0))
    hoop('Inset dial mineral socket',(-.18,.075,1.13),.21,.04,.075,'crystal_blue',n=16)
    hoop('Ancient brass rim inside crystal',(-.18,.082,1.13),.175,.027,.04,'brass',n=16)
    beam('Trapped minute hand',(-.18,.106,1.13),(-.23,.106,1.23),.045,.045,'iron')
    beam('Trapped hour hand',(-.18,.111,1.13),(-.08,.111,1.10),.045,.045,'iron')
    for dx,dz in ((0,.13),(0,-.13),(.13,0),(-.13,0)):
        box('Trapped dial gold index',(-.18+dx,.102,1.13+dz),(.04,.04,.04),'gold')
    box('Blue mineral lip enclosing trapped dial',(-.18,.11,.96),(.39,.08,.085),'crystal_blue',rot=(0,.13,0))
    move_group(start,(0,.06,0))
    for x,y in ((-.54,.48),(.58,.43)):
        lump('Old gold mineral seam',(x,y,.20),(.27,.25,.12),'gold')


def numeral_stones():
    cracked_base(2.39,1.27,.16,('earth','rock','stone_dark'))
    for text,loc,height,role,tilt in (
            ('XII',(-.68,-.08,.58),.92,'stone',-.19),
            ('IX',(.22,-.23,.56),.98,'stone_dark',.16),
            ('III',(.88,.24,.47),.86,'moon',-.14)):
        start = len(PARTS)
        # Chunky extruded bars, far thicker than the clock-face numeral helper.
        pitch = height*.32
        for i,char in enumerate(text):
            x = (i-(len(text)-1)/2)*pitch
            if char == 'I':
                box('Carved standing numeral I',(x,0,0),(height*.18,.30,height),role)
                box('Gold inlay on I',(x,.16,.05),(.045,.045,height*.71),'gold')
            else:
                for s in (-1,1):
                    beam('Carved stone numeral X',(x-s*height*.19,0,-height/2),
                         (x+s*height*.19,0,height/2),height*.16,.30,role)
                    beam('Gold inlay on X',(x-s*height*.15,.17,-height*.37),
                         (x+s*height*.15,.17,height*.37),.045,.045,'gold')
        move_group(start,loc,(.08,tilt,.10*tilt))
        lump('Moss around numeral feet',(loc[0],loc[1]+.19,.23),(.49,.38,.25),'cactus_dark')


def moon_dial():
    cylinder('Moon dial stone footing',(0,0,.11),.54,.22,'stone_dark',n=10)
    cylinder('Moon dial brass foot',(0,0,.28),.31,.14,'brass',n=10)
    lathe('Slender turned iron sculpture pole',[(.33,.13),(.55,.095),(2.11,.065),(2.22,.12)],'iron',n=10)
    for z in (.57,1.29,2.08):
        cylinder('Pole gilded collar',(0,0,z),.13,.10,'gold',n=10)
    # A robust crescent, explicitly two joined arcs with shared sharp tips.
    outer = [(.79*math.cos(a),2.67+.79*math.sin(a)) for a in
             [math.pi/3+j*(4*math.pi/3)/20 for j in range(21)]]
    # An offset circular concave arc connects bottom tip to top tip.
    inner = [(.34+.74*math.cos(a),2.67+.74*math.sin(a)) for a in
             [4.638+(1.645-4.638)*j/16 for j in range(1,16)]]
    extrude('Pale silver crescent moon',outer+inner,.18,'moon')
    tube('Gold crescent outer edge',[(x,.12,z) for x,z in outer],.025,'gold',n=5)
    start = len(PARTS)
    clock((0,0,0),.34,hands=False)
    # The sundial tilts up toward the 55-degree camera, with a solid gnomon.
    extrude('Iron triangular sundial gnomon',[(-.04,-.15),(.19,-.15),(-.04,.22)],.07,'iron',y=.17)
    move_group(start,(.08,.32,2.34),(math.radians(30),0,0))
    tube('Swept brass bell arm',[(-.72,0,2.02),(-.52,0,2.18),(0,0,2.12),(.57,0,2.12)],.045,'brass',n=6)
    for x,z in ((-.66,1.91),(-.35,1.76),(.52,1.83)):
        rod('Bell suspension',(x,0,2.10),(x,0,z),.025,'brass')
        lathe('Tiny hanging brass bell',[(z-.18,.12),(z-.12,.10),(z,.055)],'brass',n=8)
        PARTS[-1].transform((x,0,0))
        cylinder('Gold bell clapper',(x,0,z-.20),.045,.09,'gold',n=6)
    # Large enough to read, attached to a small iron stalk, not a dust mote.
    rod('Star mounting pin',(-.18,0,3.25),(-.18,0,3.42),.025,'iron')
    star = [(-.18+.18*(1 if j % 2 == 0 else .40)*math.sin(j*math.pi/4),
             3.45+.18*(1 if j % 2 == 0 else .40)*math.cos(j*math.pi/4)) for j in range(8)]
    extrude('Crystal core star finial',star,.10,'crystal_core')


# name, exact final review/export bounds, pivot convention, deterministic builder
SPECS = (
    ('ClockHand_Giant', (2,7,4), False, clock_hand_giant),
    ('Gear_Half', (4,1.2,3), False, gear_half),
    ('GearStack_Float', (2.4,2.4,1.4), True, gear_stack_float),
    ('ClockShards', (3,1.5,3), True, clock_shards),
    ('Hourglass_Broken', (2,2,4), False, hourglass_broken),
    ('TimeRift', (1.6,.6,3.2), False, time_rift),
    ('Pendulum_Frozen', (2,1,4.5), False, pendulum_frozen),
    ('MeltWatch_Rock', (1.8,1.4,1.4), False, melt_watch_rock),
    ('StairsFloat', (3,1.6,2.4), True, stairs_float),
    ('TimeCrystal', (1.8,1.6,2.2), False, time_crystal),
    ('NumeralStones', (2.5,1.5,1.3), False, numeral_stones),
    ('MoonDial', (1.8,1.8,3.6), False, moon_dial),
    ('ClockHand_Float', None, True, clock_hand_float),   # appended last: earlier seeds unchanged; None = authored metres (Unity fits length)
)


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def material_coverage(obj):
    """Occlusion-aware projected triangle area from the actual game angle."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    view = Vector((0,math.cos(math.radians(55)),math.sin(math.radians(55))))
    distance = max(obj.dimensions)*4
    areas = defaultdict(float)
    for i,t in enumerate(tris):
        facing = t.normal.dot(view)
        if facing <= 0 or t.area < 1e-10:
            continue
        center = sum((verts[j] for j in t.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.01)
        if hit == i:
            areas[obj.data.materials[t.material_index].name] += t.area*facing
    total = sum(areas.values())
    assert total > 0, obj.name
    return {role:area/total for role,area in areas.items()}


def assembled_mesh(name):
    objects = []
    for bevel in (True,False):
        verts,faces,indices = [],[],[]
        for p in PARTS:
            if p.bevel != bevel:
                continue
            offset = len(verts)
            verts.extend(p.verts)
            faces.extend(tuple(offset+i for i in f) for f in p.faces)
            indices.extend([ROLES.index(p.role)]*len(p.faces))
        if not verts:
            continue
        data = bpy.data.meshes.new(name+'_parts')
        data.from_pydata(verts,[],faces)
        for role in ROLES:
            data.materials.append(MATS[role])
        for f,index in zip(data.polygons,indices):
            f.material_index = index
        bm = bmesh.new()
        bm.from_mesh(data)
        # Remove collapsed faces produced by the half-buried gear's ground cut.
        bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=list(bm.edges))
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(name+'_parts',data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Worn edges 0.025m two segments','BEVEL')
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


def finish(name, dims, centered, seed):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    if triangles(obj) > 6000:
        mod = obj.modifiers.new('Prop budget preserving role boundaries','DECIMATE')
        mod.ratio,mod.delimit = 5750/triangles(obj),{'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= 6000, (name,triangles(obj))
    lo,hi = bounds(obj)
    span,mid = hi-lo,(hi+lo)/2
    dims = dims or tuple(span)
    for v in obj.data.vertices:
        for i in range(3):
            anchor = mid[i] if i < 2 or centered else lo[i]
            v.co[i] = (v.co[i]-anchor)*dims[i]/span[i]
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    # Bevel intersections at the burial plane can leave zero-area slivers.
    bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), name+' has open/nonmanifold edges'
    bad_faces = [f for f in bm.faces if f.calc_area() <= 1e-12]
    assert not bad_faces, (name,'degenerate faces',[(f.calc_area(),f.material_index,
                          tuple(f.calc_center_median())) for f in bad_faces[:6]])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    lo,hi = bounds(obj)
    assert all(abs(obj.dimensions[i]-dims[i]) < 1e-4 for i in range(3)), (name,obj.dimensions)
    assert abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5, name
    assert abs(lo.z+hi.z if centered else lo.z) < 1e-5, name+' incorrect pivot'
    assert obj.location.length < 1e-6 and tuple(obj.scale) == (1,1,1), name
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    assert len(used) >= 5 and used <= set(ROLES), (name,used)
    coverage = material_coverage(obj)
    print('VISIBLE '+name+' '+str({r:round(s,3) for r,s in
          sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    assert len(coverage) >= 5, (name,'insufficient visible roles',coverage)
    assert max(coverage.values()) <= .555, (name,'dominant material',coverage)
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color) == (.5,.5,.5,1)
        nodes = mat.node_tree.nodes
        shader = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
        assert tuple(shader.inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert len(nodes) == 2, mat.name+' unexpected shader effect'
        assert shader.inputs['Emission Strength'].default_value == 0, mat.name
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    obj['front'] = '+Y'
    obj['origin_convention'] = 'center' if centered else 'bottom center'
    obj['dimensions_m'],obj['triangle_count'],obj['seed'] = list(dims),triangles(obj),seed
    if name in {'GearStack_Float','StairsFloat'}:
        obj['whole_piece_animation'] = 'Y rotation' if name == 'GearStack_Float' else 'vertical bob'
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
    """Single 1600x900 row. Colours/transforms are restored, FBXs untouched."""
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
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.5,1.1
    shading.show_specular_highlight,shading.show_object_outline = False,True
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.09,.115,.19)
    locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role],1)
        gap = .62
        total = sum(o['dimensions_m'][0] for o in objects)+gap*(len(objects)-1)
        cursor = -total/2
        for obj,(_,_,centered,_) in zip(objects,SPECS):
            dims = obj['dimensions_m']
            obj.location = (cursor+dims[0]/2,0,dims[2]/2+.32 if centered else 0)
            cursor += dims[0]+gap
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',total+1.25
        target = Vector((0,0,1.35))
        a = math.radians(55)
        camera.location = target+Vector((0,40*math.cos(a),40*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.205
            data.materials.append(MATS['moon'])
            label = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(label)
            label.location = (obj.location.x,4.6,-.25)
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
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    exported = []
    for i,(name,dims,centered,build) in enumerate(SPECS):
        PARTS = []
        seed = 93187+i*1009
        random.seed(seed)
        build()
        exported.append(finish(name,dims,centered,seed))
    assert len(scene.objects) == len(SPECS) and all(o.type == 'MESH' for o in scene.objects)
    preview(exported)
    assert len(scene.objects) == len(SPECS) and all(o.type == 'MESH' for o in scene.objects)
    assert all(tuple(m.diffuse_color) == (.5,.5,.5,1) for m in MATS.values())
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
