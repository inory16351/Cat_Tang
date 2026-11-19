# 사라지는 서부 마을의 부유섬 가장자리, 작은 섬, 광장과 길 9종을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_island.py
"""Deterministic village surroundings, metres / Z-up; stock Blender 5.x.

Terrain datums are authored, never normalized to the complete object's bounds:
edge caps and islet caps are Z=0; edge +Y is OUTWARD, footprint Y=-3..+1.
Plaza's explicit Z=.06 walking surface takes precedence over the contradictory
bottom-origin/thickness wording: its .12 m slab straddles ground, -.06..+.06.
The origin remains the ground placement datum (0,0,0). Path tiles along Y.
Only review-time diffuse colours differ from the all-grey FBX materials.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Village'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_island_preview.png'
ROLES = (
    'wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
    'roof_shingle', 'roof_tin', 'adobe', 'adobe_light', 'stone', 'stone_dark',
    'rock', 'rock_dark', 'earth', 'root', 'sand', 'sand_glow', 'brass', 'gold',
    'gold_drip', 'iron', 'rust', 'canvas', 'cloth_red', 'cloth_cream',
    'paint_rose', 'paint_sage', 'glass', 'window_lit', 'lantern_glow',
    'clock_face', 'cactus', 'cactus_dark', 'stardust',
)
PALETTE = {
    'wood': (.43, .24, .12), 'wood_dark': (.18, .095, .055),
    'wood_light': (.72, .48, .25), 'wood_weathered': (.49, .41, .28),
    'plank': (.62, .36, .17), 'roof_shingle': (.48, .22, .14),
    'roof_tin': (.57, .53, .45), 'stone': (.66, .57, .43),
    'stone_dark': (.31, .28, .22), 'rock': (.49, .37, .28),
    'rock_dark': (.23, .19, .15), 'earth': (.49, .25, .13),
    'root': (.29, .15, .07), 'sand': (.83, .66, .42),
    'sand_glow': (1.0, .73, .26), 'brass': (.69, .44, .12),
    'gold': (.96, .66, .16), 'gold_drip': (1.0, .48, .10),
    'iron': (.16, .15, .13), 'rust': (.62, .25, .10),
    'paint_rose': (.70, .34, .27), 'paint_sage': (.48, .51, .29),
    'window_lit': (1.0, .86, .44), 'lantern_glow': (1.0, .58, .16),
    'clock_face': (.93, .85, .64), 'cactus': (.39, .47, .17),
    'cactus_dark': (.20, .29, .10), 'stardust': (1.0, .94, .72),
}
HARD = set(ROLES) - {'root', 'sand', 'sand_glow', 'gold_drip', 'stardust',
                      'cactus', 'cactus_dark', 'clock_face'}
MATS = {}
PARTS = []
TAU = math.tau


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        mat.node_tree.nodes.clear()
        shader = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.materials.append(MATS[role])
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    if bevel and role in HARD:
        mod = obj.modifiers.new('Worn edges .025m / two segments', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(42)
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (v / 2 for v in size)
    vertices = [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)]
    faces = [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
             (2,3,7,6), (3,0,4,7)]
    return transform(mesh(label, vertices, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0)):
    vertices = [(radius*math.cos(j*TAU/n), radius*math.sin(j*TAU/n), z)
                for z in (-depth/2, depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return transform(mesh(label, vertices, faces, role), loc, rot)


def tube(label, points, radii, role, n=5):
    """Closed tapered polygonal strand; .04 m minimum diameter."""
    if isinstance(radii, (float, int)):
        radii = [radii]*len(points)
    vertices = []
    for i, p in enumerate(points):
        tangent = (Vector(points[min(i+1,len(points)-1)]) -
                   Vector(points[max(0,i-1)])).normalized()
        reference = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = reference.cross(tangent).normalized()
        v = tangent.cross(u)
        vertices += [tuple(Vector(p)+max(.02,radii[i])*(
            u*math.cos(j*TAU/n)+v*math.sin(j*TAU/n))) for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n,len(points)*n)))
    return mesh(label, vertices, faces, role)


def lump(label, loc, size, role):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.87,1.11)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    obj = mesh(label, [tuple(v.co) for v in bm.verts],
               [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return obj


def sparkle(loc, size=.07):
    x,y,z = loc
    vertices = [(x+size,y,z),(x,y+size,z),(x-size,y,z),(x,y-size,z),
                (x,y,z+size*1.6),(x,y,z-size*1.6)]
    return mesh('Stardust', vertices,
                [(j,(j+1)%4,p) for p in (4,5) for j in range(4)],
                'stardust', bevel=False)


def ring(label, loc, inner, outer, depth, role, start=0, end=TAU,
         n=32, rot=(0,0,0)):
    """Annular solid, optionally an open broken arc; local XY plane."""
    closed = abs(end-start-TAU) < 1e-6
    count = n if closed else n+1
    vertices = [(r*math.cos(start+(end-start)*j/n),
                 r*math.sin(start+(end-start)*j/n), z)
                for r,z in ((inner,-depth/2),(outer,-depth/2),
                            (outer,depth/2),(inner,depth/2)) for j in range(count)]
    faces = [(k*count+j,k*count+(j+1)%count,
              ((k+1)%4)*count+(j+1)%count,((k+1)%4)*count+j)
             for k in range(4) for j in range(n)]
    if not closed:
        faces += [(0,count,2*count,3*count),
                  (count-1,4*count-1,3*count-1,2*count-1)]
    return transform(mesh(label, vertices, faces, role), loc, rot)


def sector(label, inner, outer, a, b, z0, z1, role, steps=1):
    angles = [a+(b-a)*i/steps for i in range(steps+1)]
    polygon = [(outer*math.cos(t),outer*math.sin(t)) for t in angles]
    polygon += [(inner*math.cos(t),inner*math.sin(t)) for t in reversed(angles)]
    n = len(polygon)
    vertices = [(x,y,z) for z in (z0,z1) for x,y in polygon]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label,vertices,faces,role)


def gear(loc, radius=.5, teeth=10, rot=(0,0,0)):
    start = len(PARTS)
    ring('Brass gear open hub', (0,0,0), radius*.33, radius*.78,
         .12, 'brass', n=teeth*2)
    for i in range(teeth):
        a = i*TAU/teeth
        box('Broad gear tooth', (radius*.86*math.cos(a),radius*.86*math.sin(a),0),
            (radius*.33,radius*.25,.12), 'brass', rot=(0,0,a))
    for obj in PARTS[start:]:
        transform(obj,loc,rot)


def tuft(x,y,z=0, scale=1):
    # Dry grass uses existing warm wood roles; no invented grass material.
    for i in range(3):
        h = scale*(.24+.08*(i%2))
        box('Dry grass block', (x+(i-1)*.09*scale,y,z+h*.48),
            (.065*scale,.075*scale,h),
            ('wood_light','wood_weathered','sand')[i],
            rot=(0,(i-1)*.30,random.uniform(-.4,.4)))


def crumbs(loc, count=8, direction=(0,.4,-1), span=1):
    """Crumbling trail: detached grains shrink along their travel direction."""
    for i in range(count):
        t = (i+1)/count
        p = Vector(loc)+Vector(direction)*span*t
        p += Vector((random.uniform(-.18,.18),random.uniform(-.12,.12),0))
        s = .16*(1-t)+.045
        lump('Vanishing sand grain',p,(s,s,s*1.4),'sand_glow')
    sparkle(Vector(loc)+Vector(direction)*span+Vector((.13,.1,.15)),.05)


def strata(width, depth, hanging, edge=False):
    """One closed, layered hanging solid. All cap vertices are exactly Z=0.

    Irregular superellipse ends have neither a flat cut nor a vertical end cap.
    Successive rings change phase/radius and triangulation, including +/-X ends.
    Geometry tapers inward under every side, terminating at one rocky point.
    """
    n = 24
    cy = -1 if edge else 0
    exponent = .40 if edge else 1.0
    outline = []
    for j in range(n):
        a = TAU*j/n
        c,s = math.cos(a),math.sin(a)
        jitter = 1 if j%6 == 0 else random.uniform(.90,1.0)
        x = width/2*math.copysign(abs(c)**exponent,c)*jitter
        y = depth/2*math.copysign(abs(s)**exponent,s)*jitter
        outline.append((x,cy+y))
    shoulder = .60 if not edge else .68
    levels = [(0,1),(-.14,1),(-shoulder,.95),(-hanging*.24,.85),
              (-hanging*.40,.69),(-hanging*.58,.52),
              (-hanging*.76,.34),(-hanging*.91,.16)]
    vertices = []
    for k,(z,scale) in enumerate(levels):
        for j,(x,y) in enumerate(outline):
            wobble = 1 if k < 2 else random.uniform(.90,1.08)
            shift = (1-scale)*.45
            vertices.append((x*scale*wobble+shift,
                             cy+(y-cy)*scale*wobble-(1-scale)*.22,
                             z if k < 2 else z+random.uniform(-.13,.13)))
    inner = len(vertices)
    vertices += [(x*.72,cy+(y-cy)*.72,0) for x,y in outline]
    vertices.append((0,cy,0))
    center = len(vertices)-1
    faces = [(center,inner+j,inner+(j+1)%n) for j in range(n)]
    indices = [0]*n
    for j in range(n):
        faces.append((j,(j+1)%n,inner+(j+1)%n,inner+j))
        # Small rim exposures, not radial stripes across the sandy interior.
        indices.append(0 if j%7 not in (2,5) else (2 if j%7 == 2 else 5))
    bands = (0,1,2,3,4,2,3)
    for k in range(len(levels)-1):
        for j in range(n):
            a,b,c,d = k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j
            # Two triangles per facet: a naturally broken rock silhouette.
            faces += [(a,d,c),(a,c,b)]
            role = bands[k]
            if k > 1 and j%5 == 0:
                role = 5
            indices += [role,role]
    vertices.append((.45,cy-.22,-hanging))
    tip = len(vertices)-1
    faces += [((len(levels)-1)*n+j,tip,(len(levels)-1)*n+(j+1)%n)
              for j in range(n)]
    indices += [3 if j%3 else 4 for j in range(n)]
    obj = mesh('Continuous flat sand cap and hanging strata',vertices,faces,
               'sand',bevel=False)
    for role in ('earth','rock','rock_dark','stone','stone_dark'):
        obj.data.materials.append(MATS[role])
    for poly,index in zip(obj.data.polygons,indices):
        poly.material_index = index
    cap = obj.vertex_groups.new(name='Terrain_cap_Z0')
    cap.add(list(range(n))+list(range(inner,center+1)),1,'REPLACE')
    return outline


def hanging_roots(width, hanging, cy=0, edge=False, count=7):
    for i in range(count):
        x = (i/(count-1)-.5)*width*.83
        y = (.58+random.uniform(-.2,.12)) if edge else (
            math.sqrt(max(.04,1-(x/(width/2))**2))*width*.34)
        y += cy
        length = hanging*random.uniform(.43,.85)
        start = (x,y,-.30)
        mid = (x+random.uniform(-.25,.25),y+.20,-length*.35)
        lower = (x*.70+.15,y-.08,-length*.77)
        end = (x*.63+.30,y+.10,-length)
        tube('Dangling kinked root',[start,mid,lower,end],
             [.13,.095,.065,.025],'root')
        if i%2 == 0:
            tube('Forked hanging root',[mid,(mid[0]-.36,mid[1]+.12,mid[2]-.5),
                                       (mid[0]-.48,mid[1]+.06,mid[2]-1.0)],
                 [.065,.045,.02],'root')


def falling_chunks(width, count=4, edge=False):
    for i in range(count):
        x = (i/(count-1)-.5)*width*.83
        y = random.uniform(1.35,1.95) if edge else width*.48+random.uniform(.1,.4)
        z = -1.4-i*.72
        s = .68-i*.09
        lump('Detached falling stratum', (x,y,z), (s,s*.8,s*1.2),
             ('rock','stone','rock_dark','earth')[i%4])
        crumbs((x,y,z+.42),5,(.18,.15,-1),.9)


def wagon_wheel():
    start = len(PARTS)
    ring('Broken wagon wood rim',(0,0,0),.66,.86,.15,'wood_dark',
         start=.25,end=4.85,n=16)
    ring('Broken iron tyre',(0,0,0),.86,.92,.12,'iron',start=.25,end=4.85,n=16)
    cylinder('Wheel axle boss',(0,0,0),.18,.26,'rust',n=10)
    for i in range(8):
        a = .30+i*.61
        tube('Broken wheel spoke',[(.14*math.cos(a),.14*math.sin(a),0),
                                  (.70*math.cos(a),.70*math.sin(a),0)],
             .065,'wood_dark')
    for obj in PARTS[start:]:
        transform(obj,(-1.18,.88,-1.12),(math.pi/2,0,-.13))


def edge_a():
    strata(6,4,9,edge=True)
    hanging_roots(6,8,edge=True)
    for x,y in ((-2.3,-1.5),(-.6,-.4),(2.2,-.7),(.7,-2.2)):
        tuft(x,y)
    for i in range(4):
        # Local plank Y extent passes beyond the +Y lip; broken ends are jagged.
        x = -.9+i*.42
        length = (1.8,1.43,2.1,1.62)[i]
        obj = box('Broken boardwalk overhang',(x,.40,.075),(.36,length,.15),
                  'plank',rot=(0,0,(i-1.5)*.035))
        # Chipped end corners, no paper-thin decorative splinters.
        for v in obj.data.vertices:
            if v.co.y > 1.05:
                v.co.y -= random.uniform(0,.20)
        cylinder('Old iron board nail',(x,-.12,.15),.025,.045,'iron',n=6)
    wagon_wheel()
    falling_chunks(6,4,edge=True)


def dissolving_fence():
    for i,x in enumerate((-2.4,-1.25,-.10,1.05,2.20)):
        h = (1.10,1.12,1.00,.67,.28)[i]
        box('Fence post surviving base',(x,.50,h/2),(.19,.20,h),
            'wood_weathered',rot=(0,.04*(i-2),0))
        box('Fence iron collar',(x,.50,.18),(.23,.24,.08),'iron')
        if i < 3:
            for z in (.45,.86):
                box('Lip fence rail',(x+.51,.49,z),(1.04,.13,.13),
                    'wood_light' if z > .5 else 'wood')
    # Four separate ascending layers, with decreasing size and population.
    for layer,count in enumerate((5,4,3,2)):
        for j in range(count):
            s = .27-layer*.055
            x = 1.1+j*.28+random.uniform(-.08,.08)
            z = .95+layer*.43+random.uniform(-.08,.08)
            box('Dissolving fence top',(x,.5+layer*.10,z),(s,s*.55,s*.75),
                ('wood_weathered','plank','sand_glow')[j%3],
                rot=(.1*j,.18*layer,.25*j))
    crumbs((2.2,.65,1.1),7,(.25,.15,1),1.4)


def sagging_clock():
    """Thick bent dial, not a rigid upright clock; its front drapes down +Y."""
    def bend(x,y,z=0):
        return (1.12+x,.42+y,.13+z-1.85*max(0,y-.12)**1.35)

    n = 32
    vertices = []
    # Center has a tiny finite ring, avoiding degenerate radial quads.
    for dz in (-.075,0):
        for r in (.045,.36,.72,.82):
            vertices += [bend(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),dz)
                         for j in range(n)]
    faces,indices = [],[]
    for side in range(2):
        base = side*4*n
        faces.append(tuple(base+j for j in range(n)))
        indices.append(0)
        for k in range(3):
            for j in range(n):
                faces.append((base+k*n+j,base+k*n+(j+1)%n,
                              base+(k+1)*n+(j+1)%n,base+(k+1)*n+j))
                indices.append(1 if k == 2 else 0)
    for j in range(n):
        faces.append((3*n+j,3*n+(j+1)%n,7*n+(j+1)%n,7*n+j))
        indices.append(1)
    obj = mesh('Sagging pocket watch membrane',vertices,faces,'clock_face',False)
    obj.data.materials.append(MATS['brass'])
    for p,i in zip(obj.data.polygons,indices):
        p.material_index = i
    for j in range(12):
        a = j*TAU/12
        p0 = bend(.57*math.sin(a),.57*math.cos(a),.04)
        p1 = bend(.68*math.sin(a),.68*math.cos(a),.04)
        tube('Bent gold hour marker',[p0,p1],.035,'gold',n=4)
    for x,y in ((-.30,.33),(.15,.55)):
        tube('Frozen sagging clock hand',[bend(0,0,.06),bend(x,y,.06)],
             .035,'iron',n=4)
    for x,length in ((-.42,.42),(.12,.90),(.40,.64)):
        p = bend(x,.68,.02)
        tube('Molten clock drip',[p,(p[0]+.04,p[1]+.16,p[2]-.23),
                                 (p[0]-.06,p[1]+.12,p[2]-length)],
             [.085,.065,.03],'gold_drip')
        lump('Gold drop',(p[0]-.06,p[1]+.12,p[2]-length),(.13,.13,.20),'gold_drip')


def edge_b():
    strata(6,4,9,edge=True)
    hanging_roots(6,8,edge=True,count=6)
    dissolving_fence()
    gear((-.95,-.30,-.055),.66,12,rot=(.30,.12,.15))
    sagging_clock()
    for i in range(9):
        x = random.uniform(-2.25,2.25)
        z = random.uniform(-4.5,-.8)
        y = .80+(z+.8)*.10
        tube('Glowing stratum crystal',[(x,y,z),(x+.10,y+.26,z+.32)],
             [.13,.025],'sand_glow',n=5)
    for x,y in ((-2,-1.6),(.2,-1.8),(2,-.7)):
        tuft(x,y)
    falling_chunks(6,3,edge=True)


def dead_tree():
    trunk = [(-.55,.15,0),(-.30,.30,.90),(.20,.57,1.90),(.85,.84,2.80),
             (1.45,1.10,3.25)]
    tube('Dead tree leaning into void',trunk,[.25,.21,.15,.09,.025],
         'wood_weathered',n=6)
    for points,radii in (
        ([trunk[1],(-1.15,.45,1.60),(-1.55,.64,2.20)],[.13,.08,.025]),
        ([trunk[2],(.95,.65,2.25),(1.30,.76,2.70)],[.12,.07,.025]),
        ([trunk[3],(.50,1.0,3.25),(.25,1.10,3.60)],[.07,.045,.02]),
        ([(-1.15,.45,1.60),(-1.5,.30,1.80)],[.065,.025]),
    ):
        tube('Broken dead tree bough',points,radii,'wood_weathered')
    for i in range(4):
        x = -.6+(i-1.5)*.3
        tube('Exposed tree root gripping lip',[(-.55,.15,.16),(x,.48,-.12),
             (x+.15,.87,-1.3),(x+.32,.72,-3.1-i*.37)],
             [.13,.11,.065,.025],'root')
    for layer,count in enumerate((4,3,2,1)):
        for i in range(count):
            s = .18-layer*.036
            box('Tree crown dissolving splinter',(1.4+i*.17,1.1,3.50+layer*.29),
                (s,.07,s*1.2),'wood_weathered',rot=(.1,.3,i*.3))
    crumbs((1.4,1.1,3.5),7,(.12,.08,1),1.2)


def edge_c():
    strata(6,4,9,edge=True)
    hanging_roots(6,8.8,edge=True,count=8)
    for loc,size,role in (
        ((-1.65,.10,.62),(1.95,1.6,1.5),'rock'),
        ((1.52,.20,.76),(2.1,1.8,1.9),'stone'),
        ((2.24,-.65,.30),(1.2,1.3,.85),'rock_dark'),
        ((.73,-.65,.29),(.9,1,.85),'rock_dark'),
    ):
        lump('Lip outcrop boulder',loc,size,role)
    dead_tree()
    for x,y in ((-2.1,-1.7),(.5,-1.8),(2.25,-1.8)):
        tuft(x,y)
    falling_chunks(6,3,edge=True)


def float_s():
    strata(2.5,2.5,3.1)
    hanging_roots(2.5,2.8,count=4)
    # Tiny saguaro: recognizable stepped arms and contrasting cactus ribs.
    tube('Tiny cactus trunk',[(.1,-.10,0),(.1,-.10,.94)],.13,'cactus',n=7)
    tube('Cactus left arm',[(.08,-.10,.42),(-.28,-.10,.42),(-.28,-.10,.76)],
         .085,'cactus')
    tube('Cactus right arm',[(.1,-.10,.57),(.39,-.10,.57),(.39,-.10,.88)],
         .075,'cactus_dark')
    tube('Cactus front rib',[(.10,.025,.13),(.10,.025,.85)],.022,'cactus_dark')
    tuft(-.60,.12,scale=.9)
    tuft(.65,-.35,scale=.85)
    lump('Islet rim stone',(-.7,-.4,.12),(.5,.44,.35),'stone')
    lump('Small islet exposed rock',(.72,.42,.20),(.65,.60,.55),'rock')
    lump('Small islet dark stone',(-.42,.62,.19),(.60,.50,.50),'rock_dark')
    crumbs((.72,.9,-.15),8,(.15,.25,-1),1.45)


def boardwalk_fragment(width=2.8, length=2.5):
    for x in (-width*.36,width*.36):
        box('Boardwalk dark bearer',(x,0,.085),(.18,length,.17),'wood_dark')
    count = int(length/.30)
    for i in range(count):
        y = -length/2+(i+.5)*length/count
        box('Weathered boardwalk plank',((.10 if i == 0 else 0),y,.20),
            (width-(.28 if i == 0 else 0),length/count-.035,.16),
            ('plank','wood','wood_light','wood_weathered')[i%4],
            rot=(0,0,.015*(i%3-1)))
        if i%2 == 0:
            for x in (-width*.36,width*.36):
                cylinder('Boardwalk iron nail',(x,y,.28),.025,.045,'iron',n=6)


def lantern(loc):
    start = len(PARTS)
    cylinder('Lantern brass foot',(0,0,.06),.20,.12,'brass',n=8)
    box('Fallen lantern flame',(0,0,.29),(.22,.22,.32),'lantern_glow')
    for x in (-.15,.15):
        for y in (-.15,.15):
            box('Lantern iron corner',(x,y,.29),(.045,.045,.34),'iron')
    cylinder('Lantern flared cap',(0,0,.49),.22,.12,'rust',n=8)
    ring('Lantern carry loop',(0,0,.66),.09,.14,.055,'iron',n=12,
         rot=(math.pi/2,0,0))
    for obj in PARTS[start:]:
        transform(obj,loc,(0,1.18,.38))


def float_m():
    strata(5,4.7,4.1)
    hanging_roots(5,3.9,count=6)
    boardwalk_fragment()
    for x in (-1.1,1.1):
        box('Hitching upright',(x,-.85,.76),(.22,.24,1.35),'wood')
        box('Hitching brass cap',(x,-.85,1.45),(.28,.29,.08),'brass')
        ring('Hitching iron tether ring',(x,-.68,.92),.10,.15,.06,'iron',n=12,
             rot=(math.pi/2,0,0))
    box('Hitching cross rail',(0,-.85,1.22),(2.65,.18,.20),'wood_weathered')
    lantern((.45,.72,.40))
    for x,y,role in ((-1.8,.8,'stone'),(1.75,.4,'rock_dark'),(-1.5,-1.5,'rock')):
        lump('Islet scattered rock',(x,y,.20),(.7,.6,.6),role)
    tuft(1.4,-1.4)
    falling_chunks(4.6,3)


def dissolving_shack():
    """Two incomplete walls, actual window opening, broken corrugated roof."""
    boardwalk_fragment(4.7,3.4)
    # Back wall is visible from +Y through its missing front wall.
    for i in range(13):
        x = -2.10+i*.35
        h = 2.25+random.uniform(-.16,.14)
        if -.5 < x < .9:
            # An actual opening, not a glowing rectangle glued over boards.
            box('Shack wall below window',(x,-1.34,.56),(.31,.14,.56),
                ('wood','wood_light','wood_weathered')[i%3])
            box('Shack wall above window',(x,-1.34,2.02),(.31,.14,.47),
                ('wood','wood_dark','wood')[i%3])
        else:
            box('Shack rear vertical plank',(x,-1.34,.28+h/2),(.31,.14,h),
                ('wood','wood_dark','wood_light','wood_weathered')[i%4])
        if i%3 == 0:
            box('Shack rear batten',(x-.17,-1.23,1.27),(.06,.07,1.90),'wood_dark')
    box('Window warm lit pane',(.17,-1.34,1.31),(1.25,.06,.91),'window_lit')
    for x in (-.53,.87):
        box('Window brass side frame',(x,-1.21,1.31),(.09,.14,1.12),'brass')
    for z in (.78,1.85):
        box('Window rose lintel',(.17,-1.20,z),(1.49,.18,.10),'paint_rose')
    box('Window dark mullion',(.17,-1.19,1.31),(.07,.12,1.02),'wood_dark')
    box('Window sill',(.17,-1.14,.77),(1.62,.28,.12),'wood_light')
    # Left return wall; progressively lower tops create a ruined silhouette.
    for i in range(9):
        y = -1.30+i*.35
        h = 2.28-.07*i+random.uniform(-.12,.08)
        box('Shack return wall plank',(-2.28,y,.28+h/2),(.14,.31,h),
            ('wood_weathered','wood','wood_light','wood_dark')[i%4])
    for x,y in ((-2.28,-1.34),(-2.28,1.48),(2.28,-1.34)):
        box('Shack corner post',(x,y,1.38),(.20,.22,2.2),'wood_dark')
        box('Shack gold post collar',(x,y,.48),(.25,.27,.08),'gold')
    box('Shack false front cornice',(0,-1.34,2.55),(4.8,.28,.18),'paint_rose')
    box('Shack cornice gold line',(0,-1.18,2.59),(4.65,.06,.06),'gold')
    for x in (-1.8,-1.15,-.5,.15,.8,1.45):
        box('Cornice bracket',(x,-1.12,2.39),(.10,.20,.18),'wood_light')
    # Individual tin strips, bent/cracked trailing ends, alternating rust seams.
    for i in range(8):
        x = -2.05+i*.39
        # A broad missing roof bay exposes the window at the review angle.
        length = .42 if i in (4,5,6) else random.uniform(1.5,2.5)
        cy = -1.18 if i in (4,5,6) else -.28
        box('Remaining corrugated tin strip',(x,cy,2.58),(.36,length,.08),
            'roof_tin',rot=(.10,0,0))
        tube('Tin standing seam',[(x-.15,cy-length/2,2.54),
                                  (x-.15,cy+length/2,2.62)],
             .025,'rust' if i%3 == 0 else 'iron')
    # Four dissolving layers; detached large roof planks give way to grains.
    for layer,count in enumerate((7,5,3,2)):
        for i in range(count):
            s = .44-layer*.10
            x = random.uniform(-2.0,1.6)
            y = random.uniform(-1.2,.8)
            z = 3.0+layer*.53+random.uniform(-.08,.08)
            box('Shack ascending fragment',(x,y,z),(s,.14+max(0,s-.12),.08),
                ('roof_tin','wood','plank','sand_glow')[i%4],
                rot=(.16+i*.1,layer*.18,i*.4))
    crumbs((.2,-.4,3.15),9,(.12,.1,1),1.85)


def float_l():
    strata(8,7.5,5.1)
    hanging_roots(8,5,count=8)
    dissolving_shack()
    for x,y,role in ((2.8,.8,'rock'),(-2.8,1.7,'stone'),(1.4,2.7,'rock_dark')):
        lump('Shack island rock',(x,y,.26),(1.2,1,.8),role)
    for x,y in ((2.4,-1.9),(-2.6,-2),(1.4,2.4)):
        tuft(x,y,scale=1.25)
    falling_chunks(7,3)


def debris():
    # Exactly 24 detached fragments, including the two multi-part brass gears.
    # Four layers with fewer, smaller fragments higher up. The final bounds
    # translate (never scale) to keep the origin at the bounding-box center.
    index = 0
    for layer,count in enumerate((9,7,5,3)):
        for i in range(count):
            a = i*2.40+layer*.7
            radius = (1.20,1.15,.90,.50)[layer]
            loc = (radius*math.cos(a),radius*math.sin(a),
                   -1.24+layer*.83+random.uniform(-.10,.10))
            s = (.59,.38,.23,.11)[layer]
            if index in (2,10):
                gear(loc,s*.48,teeth=7,rot=(.7,.25,a))
            elif index in (3,11):
                box('Floating clock hand',loc,(.07,s*1.15,.055),'iron',
                    rot=(.25,.5,a))
            elif index == 4:
                box('Playing card size gold numeral bar',loc,(.09,.15,.04),
                    'gold',rot=(.3,.5,a))
            elif index in (5,12,16,20):
                lump('Floating glowing sand clump',loc,(s,s*.8,s*.9),'sand_glow')
            elif layer == 3 or index in (8,18):
                sparkle(loc,max(.025,s*.36))
            else:
                box('Floating plank or shingle',loc,(s,s*.52,max(.04,s*.15)),
                    ('plank','roof_shingle','wood_weathered','wood_light')[index%4],
                    rot=(.3+i*.08,.26*layer,a))
            index += 1
    coords = [v.co for obj in PARTS for v in obj.data.vertices]
    lo = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    shift = -(hi+lo)/2
    for obj in PARTS:
        transform(obj,shift)


def apply_modifiers(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def dial_inlay():
    """Boolean recesses ensure every clock role really is flush at Z=.06.

    Cutters are removed, inserts are retained. No coplanar stacked faces:
    floor, numerals, hands, hub, bezel and puddle share one top plane.
    """
    face = cylinder('Six metre clock dial',(0,0,0),2.81,.12,'clock_face',n=64)
    ring('Brass clock bezel',(0,0,0),2.81,3,.12,'brass',n=64)
    inserts = []
    for j in range(12):
        a = j*TAU/12
        inserts.append(box('Gold hour numeral bar',
                           (2.44*math.sin(a),2.44*math.cos(a),.02),
                           (.13,.34,.08),'gold',rot=(0,0,-a),bevel=False))
    # Twelve is -Y (away from the +Y viewer), so the readable review dial
    # shows 11:59. Hour = 359.5 degrees, minute = 354 degrees clockwise.
    for label,length,width,angle in (
        ('Hour hand 11:59',1.48,.12,359.5),
        ('Minute hand 11:59',2.13,.08,354),
    ):
        a = math.radians(angle)
        inserts.append(box(label,(-length/2*math.sin(a),-length/2*math.cos(a),.02),
                           (width,length,.08),'iron',rot=(0,0,math.pi-a),bevel=False))
    inserts.append(cylinder('Clock gold pivot',(0,0,.02),.16,.08,'gold',n=12))
    # One molten lobe occupies the SE clock edge, with several rounding lobes.
    for loc,size in (((1.65,-1.65,.02),(.75,.70,.08)),
                     ((1.85,-1.60,.02),(.43,.38,.08)),
                     ((1.50,-1.90,.02),(.42,.30,.08))):
        inserts.append(cylinder('Flush molten dial puddle',(0,0,loc[2]),size[0]/2,
                                size[2],'gold_drip',n=12))
        inserts[-1].data.transform(Matrix.Diagonal((1,size[1]/size[0],1,1)))
        transform(inserts[-1],(loc[0],loc[1],0))
    for insert in inserts:
        apply_modifiers(insert)
    apply_modifiers(face)
    # Later inserts get priority at overlapping clock hands / puddle lobes.
    targets = [face]
    for insert in inserts:
        cutter = insert.copy()
        cutter.data = insert.data.copy()
        bpy.context.collection.objects.link(cutter)
        for v in cutter.data.vertices:
            v.co.z = (v.co.z-.02)*8
        for target in targets:
            assert target.data.vertices, 'Clock inlay erased: '+target.name
            # Broad phase reduces expensive booleans for distant hour bars.
            tlo,thi = bounds(target)
            clo,chi = bounds(cutter)
            if any(thi[k] < clo[k] or chi[k] < tlo[k] for k in (0,1)):
                continue
            bpy.context.view_layer.objects.active = target
            mod = target.modifiers.new('True flush inlay recess','BOOLEAN')
            mod.operation,mod.solver,mod.object = 'DIFFERENCE','EXACT',cutter
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cutter,do_unlink=True)
        targets.append(insert)


def plaza():
    # No hidden full-disc base beneath the rings: .12 m overall thickness.
    for j in range(28):
        a = TAU*j/28
        sector('Stone plaza paving block',4.52,6,a+.003,a+TAU/28-.003,
               -.06,.06,'stone' if j%3 else 'stone_dark',steps=2)
    for j in range(36):
        a = TAU*j/36
        sector('Radial plank plaza floor',3.01,4.50,a+.003,a+TAU/36-.003,
               -.06,.06,('plank','wood','wood_light','wood_weathered')[j%4])
    dial_inlay()
    for direction in range(4):
        a = direction*math.pi/2
        start = len(PARTS)
        for i in range(4):
            box('Cardinal boardwalk stub',(0,6.15+i*.27,0),
                (2.4,.235,.12),('plank','wood_weathered','wood_light','wood')[i])
        for obj in PARTS[start:]:
            transform(obj,rot=(0,0,a))


def path_board(y, role, cracked=False):
    """Closed plank with real flush iron face patches, without coplanar nails.

    Shared top vertices make wood and nail heads one continuous floor plane.
    A second plank has two disconnected halves with a chunky jagged split.
    """
    sets = [(-1.2,-1.075,-1.005,1.005,1.075,1.2)]
    if cracked:
        sets = [(-1.2,-1.075,-1.005,-.065),(.065,1.005,1.075,1.2)]
    for xs in sets:
        ys = [y-.1075,y-.035,y+.035,y+.1075]
        nx,ny = len(xs),len(ys)
        vertices = []
        for z in (.04,.08):
            for j,yy in enumerate(ys):
                for xx in xs:
                    if abs(xx) < .1:
                        xx += math.copysign((.035,.005,.045,.015)[j],xx)
                    vertices.append((xx,yy,z))
        faces,indices = [],[]
        layer = nx*ny
        for j in range(ny-1):
            for i in range(nx-1):
                a = j*nx+i
                faces.append((a,a+nx,a+nx+1,a+1))
                indices.append(0)
                faces.append((a+layer,a+1+layer,a+nx+1+layer,a+nx+layer))
                nail = abs(abs((xs[i]+xs[i+1])/2)-1.04) < .001 and j == 1
                indices.append(1 if nail else 0)
        perimeter = list(range(nx))
        perimeter += [j*nx+nx-1 for j in range(1,ny)]
        perimeter += [(ny-1)*nx+i for i in reversed(range(nx-1))]
        perimeter += [j*nx for j in reversed(range(1,ny-1))]
        for j,a in enumerate(perimeter):
            b = perimeter[(j+1)%len(perimeter)]
            faces.append((a,b,b+layer,a+layer))
            indices.append(0)
        obj = mesh('Cracked crosswise board' if cracked else 'Crosswise path board',
                   vertices,faces,role)
        obj.data.materials.append(MATS['iron'])
        for poly,index in zip(obj.data.polygons,indices):
            poly.material_index = index


def path():
    """Exact X +/-1.2, Y +/-2, Z=0..08. End gaps repeat at .25 m pitch."""
    for x in (-1.04,1.04):
        box('Continuous dark path bearer',(x,0,.02),(.18,4,.04),'wood_dark')
    for i in range(16):
        y = -2+(i+.5)*.25
        role = ('plank','wood','wood_light','wood_weathered')[i%4]
        if i == 6:
            # One missing board reveals the side beams. Leave both tile ends.
            continue
        path_board(y,role,cracked=(i == 11))


SPECS = [
    ('Edge_A',edge_a,8000), ('Edge_B',edge_b,8000), ('Edge_C',edge_c,8000),
    ('FloatIsle_S',float_s,6000), ('FloatIsle_M',float_m,6000),
    ('FloatIsle_L',float_l,12000), ('Debris',debris,6000),
    ('Plaza',plaza,12000), ('Path',path,6000),
]


def bounds(obj):
    return [Vector(tuple(fn(v.co[k] for v in obj.data.vertices) for k in range(3)))
            for fn in (min,max)]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def visible_materials(obj):
    """Projected material coverage from the actual 55-degree review direction.

    Ray occlusion avoids counting hidden cliff backs, buried clock parts or
    undersides of paving. Area shares are a practical richness regression.
    """
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    a = math.radians(55)
    view = Vector((0,math.cos(a),math.sin(a)))
    distance = max(obj.dimensions)*4+2
    areas = {}
    for index,t in enumerate(tris):
        facing = t.normal.dot(view)
        if facing <= 0 or t.area < 1e-10:
            continue
        center = sum((verts[k] for k in t.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.005)
        if hit == index:
            role = obj.data.materials[t.material_index].name
            areas[role] = areas.get(role,0)+t.area*facing
    total = sum(areas.values())
    assert total > 0, obj.name
    return {role:area/total for role,area in areas.items()}


def finish(name, budget):
    # Preserve datums while applying real bevels before joining / export.
    for obj in PARTS:
        apply_modifiers(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name,obj.data.name = name,name+'_Mesh'
    if triangles(obj) > budget:
        mod = obj.modifiers.new('Preserve strata and role boundaries / budget','DECIMATE')
        mod.ratio = (budget-150)/triangles(obj)
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.update()
    bpy.context.view_layer.update()
    assert triangles(obj) <= budget, (name,triangles(obj),budget)
    assert obj.location.length < 1e-7, name+' origin is not the placement datum'
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), name+' has an open fragment'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    lo,hi = bounds(obj)
    if name.startswith(('Edge_','FloatIsle_')):
        group = obj.vertex_groups['Terrain_cap_Z0'].index
        cap = [v for v in obj.data.vertices if any(g.group == group for g in v.groups)]
        assert len(cap) >= 20 and all(abs(v.co.z) < 1e-6 for v in cap), name
        if name.startswith('Edge_'):
            assert abs(min(v.co.y for v in cap)+3) < 1e-5, name
            assert abs(max(v.co.y for v in cap)-1) < 1e-5, name
            assert abs(lo.z+9) < .001, (name,lo.z)
        obj['terrain_surface_z'] = 0.0
    if name in ('Plaza','Path'):
        assert abs(hi.z-(.06 if name == 'Plaza' else .08)) < 1e-5, (name,hi.z)
        assert abs(lo.z-(-.06 if name == 'Plaza' else 0)) < 1e-5, (name,lo.z)
    if name == 'Path':
        assert abs(lo.x+1.2) < 1e-5 and abs(hi.x-1.2) < 1e-5
        assert abs(lo.y+2) < 1e-5 and abs(hi.y-2) < 1e-5
        obj['tile_direction'],obj['tile_pitch_m'] = '+Y',4.0
    if name == 'Debris':
        # Bevels shorten tilted extremities asymmetrically; center AFTER bevel.
        shift = -(lo+hi)/2
        obj.data.transform(Matrix.Translation(shift))
        obj.data.update()
        lo,hi = bounds(obj)
        assert (lo+hi).length < 1e-5, (lo,hi)
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color) == (.5,.5,.5,1)
        nodes = mat.node_tree.nodes
        assert tuple(nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert len(nodes) == 2 and not any(n.type.startswith('TEX_') for n in nodes)
    coverage = visible_materials(obj)
    assert len(coverage) >= 5, (name,coverage)
    assert max(coverage.values()) <= .55, (name,'dominant visible role',coverage)
    print('VISIBLE '+name+' '+str({k:round(v,3) for k,v in sorted(
        coverage.items(),key=lambda item:-item[1])}),flush=True)
    obj['front'] = '+Y outward to void' if name.startswith('Edge_') else '+Y'
    obj['triangle_count'],obj['seed'] = triangles(obj),73117+SPECS.index(
        next(s for s in SPECS if s[0] == name))*7919
    destination = OUTPUT / ('Kit_V_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',
          flush=True)
    return obj


def preview(objects):
    """One labelled row, flat material colours; never re-export review colours."""
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
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.5,1.2
    shading.show_specular_highlight = False
    shading.background_type,shading.background_color = 'WORLD',(.040,.033,.030)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.040,.033,.030)
    old_locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role,(.6,.5,.4)),1)
        gap = 1.1
        total = sum(obj.dimensions.x for obj in objects)+gap*(len(objects)-1)
        x = -total/2
        for obj in objects:
            lo,hi = bounds(obj)
            obj.location.x = x-lo.x
            x += hi.x-lo.x+gap
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',total+2
        target = Vector((0,0,-1.8))
        a = math.radians(55)
        camera.location = target+Vector((0,60*math.cos(a),60*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.41
            data.materials.append(MATS['stardust'])
            caption = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(caption)
            caption.location = (obj.location.x,4.9,-9.3)
            caption.rotation_euler = camera.rotation_euler
            helpers.append(caption)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper,do_unlink=True)
        for obj,loc in zip(objects,old_locations):
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
    for i,(name,build,budget) in enumerate(SPECS):
        PARTS = []
        random.seed(73117+i*7919)
        build()
        exported.append(finish(name,budget))
    assert len(scene.objects) == 9 and all(obj.type == 'MESH' for obj in scene.objects)
    preview(exported)
    assert len(scene.objects) == 9 and all(obj.type == 'MESH' for obj in scene.objects)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
