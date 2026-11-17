# 서부 개척지의 녹아내리는 대형 엄폐물 11종을 생성하고 FBX로 저장합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_landmarks.py
"""Deterministic, texture-free, ground-centred chapter-one landmark kit.

No input assets or add-ons are needed. Every exported asset is a single mesh;
construction meshes are deliberately chunky enough for a high gameplay camera.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch1'
COLORS = {
    'wood_dark': (0.33, 0.19, 0.12), 'wood': (0.55, 0.33, 0.19),
    'wood_light': (0.72, 0.48, 0.28), 'iron': (0.24, 0.22, 0.25),
    'brass': (0.85, 0.62, 0.25), 'gold_drip': (1.0, 0.78, 0.22),
    'rock': (0.66, 0.34, 0.22), 'rock_dark': (0.46, 0.22, 0.15),
    'rock_light': (0.8, 0.52, 0.34), 'sand': (0.84, 0.64, 0.4),
    'hay': (0.9, 0.74, 0.35), 'canvas': (0.85, 0.76, 0.6),
    'cloth_red': (0.7, 0.15, 0.13), 'cloth_teal': (0.25, 0.5, 0.5),
    'bone': (0.92, 0.88, 0.78), 'water': (0.3, 0.55, 0.65),
    'ore': (0.45, 0.4, 0.42),
}
MATS = {}
PARTS = []
RNG = random.Random(0)
SERIAL = 0
HARD = {'wood_dark', 'wood', 'wood_light', 'iron', 'brass'}


def material_setup():
    for name, color in COLORS.items():
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (*color, 1)
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*color, 1)
        bsdf.inputs['Roughness'].default_value = 0.85
        bsdf.inputs['Metallic'].default_value = 0.0
        MATS[name] = mat


def mesh(label, verts, faces, mat, bevel=False):
    global SERIAL
    SERIAL += 1
    data = bpy.data.meshes.new(f'{label}_{SERIAL}')
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(data.name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[mat])
    if bevel and mat in HARD:
        mod = obj.modifiers.new('Hand softened edges', 'BEVEL')
        mod.width = 0.025
        mod.segments = 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(35)
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    m = Matrix.Translation(Vector(loc))
    from mathutils import Euler
    m = m @ Euler(rot).to_matrix().to_4x4()
    obj.data.transform(m)
    return obj


def box(label, loc, size, mat='wood', rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    obj = mesh(label, verts, [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
                             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], mat, bevel)
    return transform(obj, loc, rot)


def cone(label, loc, radius, depth, mat='wood', top=None, n=10,
         rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(i * math.tau / n), r * math.sin(i * math.tau / n), z)
             for r, z in [(radius, -depth / 2), (top, depth / 2)] for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    return transform(mesh(label, verts, faces, mat, bevel), loc, rot)


def rod(label, a, b, radius, mat='wood', end=None, n=8):
    a, b = Vector(a), Vector(b)
    obj = cone(label, (0, 0, 0), radius, (b - a).length, mat, end, n)
    rotation = (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4()
    obj.data.transform(Matrix.Translation((a + b) / 2) @ rotation)
    return obj


def beam(label, a, b, width, depth=None, mat='wood'):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth or width, (b - a).length), mat)
    obj.data.transform(Matrix.Translation((a + b) / 2) @
                       (b - a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def lump(label, loc, size, mat='rock', irregular=0.12, subdivisions=2):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
    for v in bm.verts:
        v.co *= 1 + RNG.uniform(-irregular, irregular)
        v.co.x *= size[0] / 2
        v.co.y *= size[1] / 2
        v.co.z *= size[2] / 2
        v.co += Vector(loc)
        # Contact rocks and hay stay above the common ground plane.
        v.co.z = max(0.0, v.co.z)
    bm.verts.ensure_lookup_table()
    verts = [tuple(v.co) for v in bm.verts]
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return mesh(label, verts, faces, mat)


def tube(label, points, radii, mat='wood_dark', n=7, bevel=False):
    """A continuous faceted tube, including closed ends and crooked joints."""
    verts = []
    previous_x = None
    for k, p in enumerate(points):
        tangent = Vector(points[min(k + 1, len(points) - 1)]) - Vector(points[max(0, k - 1)])
        tangent.normalize()
        # Transport the section frame along the branch. Independent track
        # quaternions can flip their roll at near-vertical inflections, twisting
        # a trunk into self-intersecting sheets instead of a solid wooden limb.
        if previous_x is None:
            reference = Vector((0, 1, 0)) if abs(tangent.y) < .95 else Vector((1, 0, 0))
            basis_x = reference.cross(tangent).normalized()
        else:
            basis_x = previous_x - tangent * previous_x.dot(tangent)
            if basis_x.length < .001:
                reference = Vector((0, 1, 0)) if abs(tangent.y) < .95 else Vector((1, 0, 0))
                basis_x = reference.cross(tangent)
            basis_x.normalize()
        basis_y = tangent.cross(basis_x).normalized()
        previous_x = basis_x
        for j in range(n):
            a = j * math.tau / n
            v = Vector(p) + radii[k] * (basis_x * math.cos(a) + basis_y * math.sin(a))
            if k == 0 and abs(p[2]) < 1e-6:
                v.z = 0.0
            verts.append(tuple(v))
    faces = [tuple(reversed(range(n)))]
    for k in range(len(points) - 1):
        for j in range(n):
            a, b = k * n + j, k * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    faces.append(tuple(range((len(points) - 1) * n, len(points) * n)))
    return mesh(label, verts, faces, mat, bevel)


def ring(label, loc, radius, thickness, width, mat='iron', axis='Z', n=14):
    """Square-section hoop: cheap, readable rims instead of dense tori."""
    verts = []
    for z, r in [(-width / 2, radius - thickness), (-width / 2, radius),
                 (width / 2, radius), (width / 2, radius - thickness)]:
        for j in range(n):
            verts.append((r * math.cos(j * math.tau / n),
                          r * math.sin(j * math.tau / n), z))
    faces = []
    for k in range(4):
        for j in range(n):
            faces.append((k * n + j, k * n + (j + 1) % n,
                          ((k + 1) % 4) * n + (j + 1) % n, ((k + 1) % 4) * n + j))
    rot = (0, math.pi / 2, 0) if axis == 'X' else ((math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0))
    return transform(mesh(label, verts, faces, mat, True), loc, rot)


def drip(x, y, z, length=0.38, lean=(0.02, 0.01)):
    tube('Gold melting trail', [(x, y, z), (x + lean[0], y + lean[1], z - length * .4),
                               (x + lean[0] * 1.5, y + lean[1] * 1.5, z - length)],
         [.07, .045, .045], 'gold_drip', 7)
    lump('Gold hanging drop', (x + lean[0] * 1.5, y + lean[1] * 1.5, z - length),
         (.15, .13, .19), 'gold_drip', .02)


def wheel(loc, radius=.58, axis='X', broken=False):
    x, y, z = loc
    ring('Iron tire', loc, radius, .065, .16, 'iron', axis, 12)
    ring('Timber wheel rim', loc, radius - .055, .09, .18, 'wood_light', axis, 12)
    rot = (0, math.pi / 2, 0) if axis == 'X' else ((math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0))
    cone('Wheel hub', loc, .13, .27, 'wood_dark', n=10, rot=rot)
    for j in range(8):
        if broken and j in (1, 2, 3):
            continue
        a = j * math.tau / 8
        r = radius - .12
        offset = (0, r * math.cos(a), r * math.sin(a)) if axis == 'X' else (
            (r * math.cos(a), 0, r * math.sin(a)) if axis == 'Y' else (r * math.cos(a), r * math.sin(a), 0))
        beam('Wheel spoke', loc, tuple(Vector(loc) + Vector(offset)), .065, .08, 'wood_light')


def barrel(loc, scale=1, lying=False):
    start = len(PARTS)
    s = scale
    tube('Barrel staves', [(0, 0, -.4*s), (0, 0, 0), (0, 0, .4*s)],
         [.27*s, .34*s, .27*s], 'wood', 10, True)
    cone('Barrel lid', (0, 0, .4*s), .26*s, .055*s, 'wood_light', n=10)
    for z in [-.27, .27]:
        ring('Barrel iron binding', (0, 0, z*s), .315*s, .04*s, .07*s, n=10)
    rotation = Matrix.Rotation(math.pi / 2 if lying else 0, 4, 'Y')
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Translation(Vector(loc)) @ rotation)


def crate(loc, size=(.65, .6, .65), mat='wood'):
    x, y, z = loc
    sx, sy, sz = size
    box('Crate solid core', loc, size, mat)
    for a in [-1, 1]:
        box('Crate face upright', (x+a*sx*.38, y+sy/2+.025, z), (.075, .055, sz), 'wood_light')
    beam('Crate diagonal strap', (x-sx*.4, y+sy/2+.055, z-sz*.38),
         (x+sx*.4, y+sy/2+.055, z+sz*.38), .075, .045, 'wood_dark')


def rock_spire():
    # Seven contiguous courses per hoodoo make its horizontal erosion readable.
    for cx, cy, height, base in [(-.75, .1, 3.6, .9), (.85, -.3, 2.7, .76), (.4, .95, 1.9, .66)]:
        n = 9
        phase = RNG.random()
        rings = []
        for k in range(8):
            t = k/7
            radius = base * [1, .82, .72, .65, .57, .62, .84, .65][k]
            ox, oy = .13*math.sin(t*4+phase), .09*math.sin(t*6)
            rings.append([(cx+ox+radius*(1+RNG.uniform(-.12,.12))*math.cos(j*math.tau/n),
                           cy+oy+radius*.84*(1+RNG.uniform(-.1,.1))*math.sin(j*math.tau/n),
                           height*t) for j in range(n)])
        for k in range(7):
            verts = rings[k]+rings[k+1]
            faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
            faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
            mesh('Sandstone erosion course', verts, faces, ['rock','rock_light','rock','rock_dark','rock','rock_light','rock'][k])
    for x,y,s in [(-1.55,-.65,.85),(1.5,.65,.8),(-.85,1.25,.7),(1.35,-1,.75)]:
        lump('Loose talus', (x,y,.25), (s,s*.8,.65), 'rock_dark')
    drip(-.77,.59,3.44,.55, (.025,.035))


def boulder_pile():
    for x,y,z,size,mat in [(-.85,-.25,.7,(1.75,1.65,1.65),'rock_dark'),
                           (.55,-.4,.75,(1.8,1.65,1.8),'rock'),
                           (-.1,.55,1.35,(1.9,1.55,1.65),'rock'),
                           (1.05,.65,.5,(1.15,1.1,1.1),'rock_dark'),
                           (-1.15,.7,.35,(1.15,1.1,.85),'rock')]:
        lump('Faceted sandstone boulder', (x,y,z), size, mat, .18)
    for j in range(6):
        a = j*2.4
        p = (-.95,-.8,.65)
        q = (-.95+.5*math.cos(a),-.8+.4*math.sin(a),1.25+RNG.random()*.5)
        rod('Dry bush twig', p,q,.035,'wood_dark',.013,6)
        rod('Dry bush fork', tuple(Vector(p).lerp(Vector(q),.65)),
            (q[0]+.19,q[1]+.13,q[2]+.12),.018,'wood_dark',.006,5)
    # Broad forehead, long nasal bone, dark recessed sockets and sweeping horns.
    lump('Cow skull forehead', (.25,1.12,.73), (.65,.24,.5), 'bone', .04)
    lump('Cow skull long nose', (.25,1.24,.48), (.3,.25,.55), 'bone', .04)
    for sign in [-1,1]:
        lump('Skull eye socket', (.25+sign*.19,1.255,.74), (.17,.055,.16), 'rock_dark', .01)
        tube('Cow horn', [(.25+sign*.26,1.1,.88),(.25+sign*.55,1.06,1.01),
                          (.25+sign*.7,1.04,1.26)], [.11,.07,.012], 'bone', 7)
    drip(.67,.7,1.94,.32)


def wagon_wreck():
    start = len(PARTS)
    # Build a complete wagon in local coordinates, then topple the whole assembly.
    box('Wagon underframe', (0,0,.6), (2.55,1.15,.2), 'wood_dark')
    for j in range(6):
        box('Uneven wagon floor plank', (-1.12+j*.45,0,.76), (.43,1.3,.13),
            'wood_light' if j%3==0 else 'wood', (0,RNG.uniform(-.018,.018),0))
    for side in [-1,1]:
        box('Wagon bed side', (0,side*.64,.99), (2.55,.11,.42), 'wood')
    for x in [-.9,.9]:
        rod('Wagon axle', (x,-.88,.48),(x,.88,.48),.075,'iron')
    wheel((-.9,.83,.5),.51,'Y')
    wheel((.9,-.83,.5),.51,'Y',True)
    for x in [-1.1,-.35,.4,1.1]:
        points=[(x,.65*math.cos(j*math.pi/8),1.13+.85*math.sin(j*math.pi/8)) for j in range(9)]
        tube('Exposed wagon hoop', points,[.045]*9,'wood_light',6,True)
    # A thick canvas panel with a conspicuous jagged missing section.
    verts=[]
    for x in [-1.16,-.4,.38,1.15]:
        for j in range(7):
            a=(j+1)*math.pi/8
            verts.append((x,.68*math.cos(a),1.13+.89*math.sin(a)))
    faces=[]
    for i in range(3):
        for j in range(6):
            if (i,j) in {(1,3),(1,4),(2,4),(2,5)}:
                continue
            a=i*7+j
            faces.append((a,a+7,a+8,a+1))
    cloth=mesh('Torn canvas wagon cover',verts,faces,'canvas')
    solid=cloth.modifiers.new('Canvas edge thickness','SOLIDIFY'); solid.thickness=.035
    rotation=Matrix.Rotation(math.radians(65),4,'X')
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Translation(Vector((-.55,.1,0))) @ rotation)
    # Set the overturned shell on the ground before placing the spilled cargo.
    lowest=min(v.co.z for obj in PARTS[start:] for v in obj.data.vertices)
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Translation((0,0,-lowest)))
    wheel((1.48,.82,.13),.59,'Z',True)
    barrel((1.5,-.5,.33),.9,True)
    barrel((.7,1.05,.36),.8,False)
    crate((1.6,-1.13,.3),(.64,.6,.6))
    # Attach the melt to the overturned canvas in world space, then let it hang
    # vertically under gravity rather than rotating the drip with the wreck.
    anchor = Matrix.Translation(Vector((-.55,.1,-lowest))) @ rotation @ Vector((-.75,-.47,1.85))
    drip(*anchor, .29)


def mine_cart():
    for x in [-1.8,-.9,0,.9,1.8]:
        box('Rail sleeper',(x,0,.11),(.25,2.05,.22),'wood_dark', (0,0,RNG.uniform(-.025,.025)))
    for y in [-.68,.68]:
        box('Iron rail',(0,y,.26),(4.3,.12,.18),'iron')
    for x in [-.62,.62]:
        for y in [-.69,.69]:
            cone('Cart flanged wheel',(x,y,.49),.27,.16,'iron',n=10,rot=(math.pi/2,0,0))
    box('Cart chassis',(0,0,.62),(1.9,1.4,.2),'wood_dark')
    # Closed tapered box, with ore above the rim instead of a hollow gameplay gap.
    verts=[(x,y,z) for z,sx,sy in [(.68,.78,.55),(1.45,1.02,.72)]
           for x,y in [(-sx,-sy),(sx,-sy),(sx,sy),(-sx,sy)]]
    mesh('Iron ore cart body',verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'iron',True)
    for y in [-.73,.73]:
        box('Cart rim',(0,y,1.46),(2.1,.085,.12),'wood')
        for x in [-.8,0,.8]:
            box('Cart side timber',(x,y,1.08),(.13,.09,.65),'wood')
    for i in range(9):
        x=(i%3-1)*.53; y=(i//3-1)*.4
        lump('Big ore lump',(x,y,1.52+(.15 if i==4 else 0)),(.65,.56,.55),'ore',.2)
    for i in range(4):
        box('Stacked mine timber',(-1.35,.25,.22+i*.2),(.4,1.55,.18),'wood', (0,0,RNG.uniform(-.035,.035)))
    beam('Leaning pickaxe handle',(.65,.95,.1),(.1,.81,1.58),.07,mat='wood_light')
    tube('Pickaxe iron head',[(-.42,.82,1.43),(-.14,.82,1.62),(.18,.82,1.65),(.49,.82,1.5)],
         [.025,.07,.07,.018],'iron',6,True)
    box('Lantern post',(1.69,-.55,.94),(.12,.12,1.88),'wood_dark')
    beam('Lantern bracket',(1.69,-.55,1.87),(1.31,-.55,1.87),.07,mat='iron')
    box('Lantern glowing glass',(1.31,-.55,1.53),(.22,.22,.3),'brass')
    for z in [1.34,1.72]:
        cone('Lantern cap',(1.31,-.55,z),.2,.09,'iron',n=6)
    for x in [1.18,1.44]:
        beam('Lantern cage',(x,-.68,1.36),(x,-.68,1.7),.035,mat='iron')
    drip(.83,.8,1.45,.3)


def dead_tree():
    tube('Twisting dead trunk',[(0,0,0),(-.14,-.08,.7),(.05,.05,1.45),(-.18,.04,2.15),
                              (.05,.03,2.85),(-.12,.12,3.45),(.08,.15,3.98)],
         [.49,.38,.29,.25,.19,.11,.018],'wood_dark',8,True)
    branches=[([(-.05,0,1.25),(-.68,.12,1.9),(-1.05,.08,2.15),(-1.42,.24,2.6)],[.23,.16,.09,.018]),
              ([(-.15,.04,2.1),(.58,.02,2.45),(1.04,-.08,2.48),(1.4,-.12,2.87)],[.21,.14,.085,.016]),
              ([(0,.03,2.8),(.43,.62,3.04),(.64,.83,3.6)],[.16,.09,.015]),
              ([(-.1,.04,2.12),(-.45,-.6,2.64),(-.92,-.94,2.88),(-1.05,-1.04,3.2)],[.18,.12,.06,.012]),
              ([(-.11,.12,3.4),(-.64,.16,3.58),(-.83,.12,3.87)],[.1,.07,.014])]
    for points,radii in branches:
        tube('Crooked bare branch',points,radii,'wood_dark',7,True)
    tube('Forked branch tip',[(1.02,-.08,2.48),(1.13,.2,2.85),(1.06,.34,3.08)],
         [.08,.045,.012],'wood',6,True)
    for j in range(8):
        a=j*math.tau/8
        x,y=1.12*math.cos(a),1.1*math.sin(a)
        lump('Tree root ring stone',(x,y,.16),(.62,.5,.4),'rock_dark' if j%3==0 else 'rock')
    for j in range(4):
        a=j*math.tau/4+.4
        tube('Exposed tree root',[(0,0,.32),(.6*math.cos(a),.6*math.sin(a),.14),
                                 (.95*math.cos(a),.95*math.sin(a),.04)], [.2,.1,.025],'wood_dark',6,True)
    tube('Hanging frayed rope',[(-1.02,.11,2.18),(-1.07,.15,1.8),(-1.02,.13,1.3),(-1.09,.12,.95)],
         [.035]*4,'canvas',7)
    for j in range(3):
        rod('Rope fray',(-1.09,.12,.98),(-1.18+j*.07,.14,.78+abs(j-1)*.07),.013,'canvas',.004,5)
    drip(1.39,-.12,2.84,.43)


def water_tower():
    for x in [-1,1]:
        for y in [-1,1]:
            beam('Splayed tower leg',(x*1.26,y*1.26,.04),(x*.86,y*.86,2.54),.22,mat='wood_dark')
    for y in [-1,1]:
        beam('Tower X brace',(-1.16,y*1.16,.35),(.88,y*.88,2.24),.12,mat='wood')
        beam('Tower X brace',(1.16,y*1.16,.35),(-.88,y*.88,2.24),.12,mat='wood')
    for x in [-1,1]:
        beam('Tower side brace',(x*1.15,-1.15,.4),(x*.88,.88,2.22),.12,mat='wood')
    box('Tank deck',(0,0,2.46),(2.5,2.5,.18),'wood_dark')
    cone('Tank solid interior',(0,0,3.22),1.07,1.4,'wood_dark',n=16)
    # Real individual vertical staves, rather than a plain cylinder texture.
    for j in range(16):
        a=j*math.tau/16
        box('Tank vertical stave',(1.095*math.cos(a),1.095*math.sin(a),3.22),
            (.14,.43,1.38),['wood','wood_light','wood'][j%3],(0,0,a))
    for z in [2.66,3.22,3.8]:
        ring('Tank iron hoop',(0,0,z),1.19,.055,.1,'iron',n=16)
    cone('Tank conical roof',(0,0,4.12),1.36,.77,'wood_dark',top=.035,n=16)
    for j in range(8):
        a=j*math.tau/8
        rod('Roof shingle seam',(1.33*math.cos(a),1.33*math.sin(a),3.76),
            (.12*math.cos(a),.12*math.sin(a),4.49),.025,'wood',.012,5)
    for x in [-.34,.34]:
        beam('Tower ladder side',(x,1.53,.04),(x,1.23,2.9),.075,mat='wood_light')
    for j in range(9):
        z=.24+j*.3
        y=1.53-z/2.9*.3
        beam('Tower ladder rung',(-.34,y,z),(.34,y,z),.065,mat='wood_light')
    tube('Water spout pipe',[(.92,.22,2.88),(1.45,.22,2.88),(1.55,.22,2.69),(1.55,.22,1.55)],
         [.095]*4,'iron',8,True)
    lump('Small water puddle',(1.45,.3,.035),(.75,1.05,.07),'water',.15)
    drip(.42,1.12,3.63,.42)


def stagecoach():
    # Front is +Y: the front bench and smaller front axle clearly identify it.
    box('Coach chassis',(0,0,.65),(3.6,1.55,.2),'wood_dark')
    for y in [-.65,.65]:
        rod('Coach axle',(-1.93,y,.61),(1.93,y,.61),.075,'iron')
        for x in [-1.86,1.86]:
            wheel((x,y,.61),.59,'X')
    box('Red coach cabin',(0,-.13,1.37),(3.02,1.33,1.26),'cloth_red')
    for x in [-1.55,1.55]:
        for y in [-.82,.56]:
            box('Cabin corner post',(x,y,1.43),(.12,.12,1.47),'wood_dark')
    for x in [-1.57,1.57]:
        box('Coach side upper window',(x,-.2,1.69),(.035,.75,.43),'iron')
        for y in [-.6,.2]:
            box('Window brass side trim',(x,y,1.69),(.065,.06,.52),'brass')
        for z in [1.44,1.94]:
            box('Window brass sill',(x,-.2,z),(.065,.85,.065),'brass')
        box('Cabin brass waist trim',(x,-.13,1.11),(.065,1.36,.075),'brass')
    for x in [-.73,.73]:
        box('Front coach window',(x,.553,1.69),(.73,.04,.4),'iron')
    box('Coach roof',(0,-.13,2.12),(3.38,1.55,.18),'wood_dark')
    for x in [-1.59,1.59]:
        box('Roof luggage rail',(x,-.16,2.28),(.07,1.5,.1),'brass')
    for x in [-.85,.12]:
        crate((x,-.25,2.4),(.72,.85,.46),'wood_dark')
    lump('Tied canvas roof bundle',(.98,-.2,2.4),(.86,.85,.46),'canvas',.06)
    for x in [.76,1.16]:
        tube('Bundle rope tie',[(x,.2,2.21),(x,.2,2.48),(x,-.2,2.64),(x,-.6,2.48),(x,-.6,2.21)],
             [.025]*5,'wood_light',5,True)
    box('Driver platform',(0,.98,.86),(2.36,.42,.14),'wood_dark')
    box('Driver padded bench',(0,.87,1.2),(2.25,.48,.18),'wood_dark')
    box('Driver bench back',(0,.61,1.41),(2.25,.12,.47),'cloth_red')
    drip(-1.61,.47,1.98,.32)


def well():
    # A dark recessed centre gives the opening depth, while a solid bottom closes it.
    cone('Well dark recess',(0,0,.18),.76,.34,'rock_dark',n=12,bevel=False)
    for course in range(3):
        for j in range(10):
            a=(j+(course%2)*.5)*math.tau/10
            box('Well stone block',(.85*math.cos(a),.85*math.sin(a),.19+course*.27),
                (.36,.58,.27),'rock_light',(0,0,a),False)
    ring('Well dark stone rim',(0,0,.96),1.08,.3,.21,'rock_dark',n=12)
    for x in [-1.05,1.05]:
        box('Well roof post',(x,0,1.4),(.17,.19,2.8),'wood_dark')
    rod('Well windlass',(-1.14,0,1.64),(1.14,0,1.64),.115,'wood_light',n=10)
    rod('Crank arm',(1.25,0,1.64),(1.25,0,1.29),.055,'iron')
    rod('Crank grip',(1.25,0,1.29),(1.47,0,1.29),.07,'wood')
    tube('Well winding rope',[(.2,0,1.66),(.2,.025,1.2),(.2,.025,.68)], [.029]*3,'canvas',6)
    # Broad individually overlapping roof courses, each pair forming a gable.
    for sign in [-1,1]:
        for j in range(3):
            y=sign*(.21+j*.34)
            z=2.88-abs(y)*.55
            box('Well roof shingle course',(0,y,z),(2.74,.43,.1),
                'wood_dark' if j%2==0 else 'wood',(sign*-.5,0,0))
    cone('Well bucket',(-.6,.88,1.18),.18,.35,'wood',top=.23,n=8)
    ring('Bucket binding',(-.6,.88,1.25),.225,.035,.045,'iron',n=8)
    tube('Bucket iron handle',[(-.81,.88,1.32),(-.81,.88,1.62),(-.39,.88,1.62),(-.39,.88,1.32)],
         [.022]*4,'iron',6,True)
    box('Trough bottom',(.14,-1.22,.16),(1.8,.49,.2),'wood_dark')
    for y in [-1.48,-.96]:
        box('Trough side',(.14,y,.37),(1.92,.09,.43),'wood')
    for x in [-.77,1.05]:
        box('Trough end',(x,-1.22,.37),(.1,.5,.43),'wood_light')
    box('Trough water',(.14,-1.22,.4),(1.66,.41,.025),'water',bevel=False)
    drip(.63,1.04,1.01,.3)


def hay_cart():
    box('Farm cart underframe',(-.55,-.15,.62),(2.65,1.65,.2),'wood_dark')
    for j in range(6):
        box('Farm cart bed plank',(-1.68+j*.46,-.15,.79),(.44,1.8,.13),'wood',
            (0,RNG.uniform(-.018,.018),RNG.uniform(-.012,.012)))
    for x in [-1.67,.57]:
        rod('Farm cart axle',(x,-1.21,.5),(x,.93,.5),.075,'iron')
        for y in [-1.14,.87]:
            wheel((x,y,.51),.5,'Y')
    for x in [-1.75,.7]:
        box('Hay cart end board',(x,-.15,1.06),(.12,1.8,.48),'wood')
    for y in [-.99,.69]:
        box('Hay cart low side',(-.55,y,.98),(2.7,.1,.3),'wood')
    for x,y,z,s in [(-1.13,-.12,1.23,(1.4,1.6,.95)),(.03,-.12,1.25,(1.4,1.65,1.05)),
                     (-.56,-.12,1.75,(1.7,1.4,1.0))]:
        lump('Heaped loose hay',(x,y,z),s,'hay',.14)
    for x,y,z in [(1.38,-.68,.36),(1.38,.21,.36),(1.38,-.24,1.07)]:
        box('Square compressed hay bale',(x,y,z),(.98,.78,.69),'hay', (0,0,RNG.uniform(-.03,.03)),False)
        for yy in [y-.22,y+.22]:
            box('Bale binding top',(x,yy,z+.35),(.99,.035,.025),'wood_dark')
            for xx in [x-.495,x+.495]:
                box('Bale binding side',(xx,yy,z),(.025,.035,.69),'wood_dark')
    beam('Pitchfork wooden handle',(-.66,.1,1.35),(-.36,.18,2.4),.065,mat='wood_dark')
    beam('Pitchfork iron socket',(-.36,.18,2.35),(-.32,.18,2.53),.065,mat='iron')
    beam('Pitchfork crossbar',(-.57,.18,2.53),(-.07,.18,2.53),.045,mat='iron')
    for x in [-.55,-.39,-.23,-.07]:
        rod('Pitchfork tine',(x,.18,2.53),(x+.04,.18,2.83),.025,'iron',.009,6)
    drip(.35,.69,1.5,.35)


def market_stall():
    for x in [-1.85,1.85]:
        for y in [-.94,.94]:
            box('Market canopy post',(x,y,1.27),(.14,.14,2.54),'wood_dark')
    box('Solid market counter',(0,.12,.55),(3.65,1.18,1.1),'wood')
    for x in [-1.45,-.73,0,.73,1.45]:
        box('Counter front board',(x,.738,.54),(.68,.1,1.04),'wood_light',
            (0,RNG.uniform(-.015,.015),RNG.uniform(-.015,.015)))
    box('Market countertop',(0,.12,1.13),(3.95,1.4,.16),'wood_dark')
    for i in range(3):
        x=-1.18+i*1.18
        box('Produce crate base',(x,.17,1.25),(.95,.9,.14),'wood')
        for yy in [-.3,.64]:
            box('Produce crate wall',(x,yy,1.39),(1.0,.06,.24),'wood_light')
        for xx in [x-.48,x+.48]:
            box('Produce crate end',(xx,.17,1.39),(.07,.9,.24),'wood_light')
        for j in range(5):
            lump('Chunky market produce',(x+(j%3-1)*.23,-.01+(j//3)*.3,1.49),
                 (.31,.32,.29),['cloth_red','hay','cloth_teal'][i],.06,1)
    # Shallow pitched awning, with alternating solid coloured fabric strips.
    for j in range(8):
        x=-1.89+j*.54
        verts=[(x-.27,-1.2,2.36),(x+.27,-1.2,2.36),(x+.27,0,2.7),(x-.27,0,2.7),
               (x-.27,1.23,2.36),(x+.27,1.23,2.36),
               (x-.27,1.23,2.14+(.035 if j%2 else 0)),(x+.27,1.23,2.14+(.035 if j%2 else 0))]
        obj=mesh('Striped canopy',verts,[(0,1,2,3),(3,2,5,4),(4,5,7,6)],'canvas' if j%2==0 else 'cloth_teal')
        mod=obj.modifiers.new('Canopy thickness','SOLIDIFY'); mod.thickness=.035
    for x in [-1.03,-.37]:
        rod('Sign iron hanger',(x,1.02,2.43),(x,1.02,1.99),.022,'iron',n=6)
    box('Hanging stall sign',(-.7,1.04,1.89),(1.12,.12,.38),'wood_light',(0,0,.035))
    # A simple raised three-lobed fruit emblem reads without tiny lettering.
    for dx,dz in [(-.16,0),(.14,0),(0,.08)]:
        lump('Sign fruit emblem',(-.7+dx,1.115,1.88+dz),(.2,.045,.18),'cloth_red',.01,1)
    drip(1.54,1.26,2.31,.33)


def lumber_pile():
    for x in [-1.38,.8]:
        box('Lumber ground sleeper',(x,-.37,.1),(.22,1.62,.2),'wood_dark')
    for layer in range(4):
        for j in range(3):
            box('Stacked sawn plank',(-.3+RNG.uniform(-.08,.08),-.95+j*.45,.27+layer*.21),
                (3.45+RNG.uniform(-.18,.18),.41,.17),['wood','wood_light'][j%2],
                (0,0,RNG.uniform(-.012,.012)))
    for j in range(3):
        y=-.85+j*.49
        rod('Stacked round log',(-1.85,y,1.12),(1.22,y,1.12),.235,'wood_dark',n=9)
        for x in [-1.855,1.225]:
            cone('Visible log cut face',(x,y,1.12),.217,.025,'wood_light',n=9,rot=(0,math.pi/2,0))
            cone('Log end heartwood',(x+(-.016 if x<0 else .016),y,1.12),.1,.012,'wood',n=8,rot=(0,math.pi/2,0))
    # A full-size trestle in front gives this pile an instantly readable workshop use.
    for x in [-.98,.53]:
        for y in [.45,1.08]:
            beam('Sawhorse splayed leg',(x,y,.02),(x,.77,.8),.12,mat='wood')
    box('Sawhorse top',(-.23,.77,.84),(2.04,.21,.18),'wood_dark')
    # Saw blade has a chunky serrated lower edge and a wooden grip at one end.
    verts=[(-.76,.75,1.07),(.35,.75,1.27),(.35,.75,1.04)]
    for j in range(12):
        verts.append((.35-j*.1,.75,1.04-j*.015-(.07 if j%2 else 0)))
    obj=mesh('Saw toothed steel blade',verts,[tuple(range(len(verts)))],'iron',True)
    mod=obj.modifiers.new('Saw blade thickness','SOLIDIFY'); mod.thickness=.035
    ring('Saw wooden handle',(.52,.75,1.18),.2,.075,.09,'wood_light','Y',8)
    for i in range(2):
        box('Leaning loose board',(1.52+i*.24,.3, .68),(.25,.16,1.56),'wood_light' if i else 'wood',
            (math.radians(-25),math.radians(16+i*6),math.radians(-8)))
    drip(-1.32,.33,1.31,.35)


SPECS = [
    ('Kit_LM_RockSpire', (5.0,4.0,3.6), rock_spire),
    ('Kit_LM_BoulderPile', (4.0,3.5,2.2), boulder_pile),
    ('Kit_LM_WagonWreck', (5.0,3.5,2.2), wagon_wreck),
    ('Kit_LM_MineCart', (4.5,3.0,2.0), mine_cart),
    ('Kit_LM_DeadTree', (3.5,3.5,4.0), dead_tree),
    ('Kit_LM_WaterTower', (4.0,4.0,4.6), water_tower),
    ('Kit_LM_Stagecoach', (5.0,2.6,2.6), stagecoach),
    ('Kit_LM_Well', (3.2,3.2,2.9), well),
    ('Kit_LM_HayCart', (5.0,3.4,2.4), hay_cart),
    ('Kit_LM_MarketStall', (4.5,3.0,2.7), market_stall),
    ('Kit_LM_LumberPile', (4.5,2.6,1.7), lumber_pile),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish(name, limits):
    # Apply every component's bevel before joining: its 0.025 m width is in metres.
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + '_Mesh'

    # Dense wheel/bracing combinations get a bounded reduction after beveling.
    # Material boundaries remain locked so the cel-shaded colour blocking survives.
    count = triangles(obj)
    if count > 5800:
        mod = obj.modifiers.new('Landmark triangle budget', 'DECIMATE')
        mod.ratio = 5650 / count
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if triangles(obj) > 6000:
        raise RuntimeError(f'{name}: triangle budget exceeded: {triangles(obj)}')

    coords = [v.co for v in obj.data.vertices]
    low = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
    high = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    span = high-low
    # Keep every finished prop at its requested height; only compress horizontal
    # axes when procedural irregularity or a rim would cross the allowed bounds.
    sx, sy, sz = min(1, limits[0]/span.x), min(1, limits[1]/span.y), limits[2]/span.z
    centre = (low+high)/2
    for v in coords:
        v.x = (v.x-centre.x)*sx
        v.y = (v.y-centre.y)*sy
        v.z = (v.z-low.z)*sz
    # A common mathematically flat support plane for the lowest contact vertices.
    for v in coords:
        if v.z < .00001:
            v.z = 0
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.update()
    obj.location = (0,0,0)
    obj.rotation_euler = (0,0,0)
    obj.scale = (1,1,1)
    bpy.context.view_layer.update()
    dims = tuple(obj.dimensions)
    assert dims[0] <= limits[0]+.02 and dims[1] <= limits[1]+.02, (name,dims,limits)
    assert abs(dims[2]-limits[2]) < .002, (name,dims,limits)
    assert abs(min(v.co.z for v in obj.data.vertices)) < 1e-5
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert any(m and m.name == 'gold_drip' for m in obj.data.materials), name
    obj['footprint_m'] = list(limits[:2])
    obj['height_m'] = limits[2]
    obj['front'] = '+Y'
    obj['chapter'] = 'Dusty frontier, melting gold'
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    destination = OUTPUT / f'{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 4) for d in dims)}', flush=True)
    return obj


def main():
    global PARTS, RNG
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    material_setup()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    exported = []
    for index, (name, limits, build) in enumerate(SPECS):
        PARTS = []
        RNG = random.Random(71023+index*7919)
        build()
        exported.append(finish(name, limits))
    assert len(exported) == 11
    assert len([o for o in bpy.context.scene.objects if o.type == 'MESH']) == 11
    assert not any(o.type in {'CAMERA','LIGHT'} for o in bpy.context.scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
