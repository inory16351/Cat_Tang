# 떠 있는 마을 섬을 잇는 온전한 다리와 시간 속으로 무너지는 다리를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_bridge.py
"""Two deterministic bridge modules; stock Blender only.

Metres, length along Y, deck connection planes at Y=+-5 and Z=0.
The pivot is the deck datum (0, 0, 0), NOT the lowest dangling fragment.
Only this script's two FBXs and one review PNG are written.
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
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_bridge_preview.png'
ROLES = ('plank', 'wood', 'wood_light', 'wood_weathered', 'wood_dark',
         'canvas', 'iron', 'brass', 'gold', 'gold_drip', 'clock_face',
         'glass', 'lantern_glow', 'sand_glow', 'stardust')
HARD = set(ROLES) - {'canvas', 'glass', 'lantern_glow', 'sand_glow', 'stardust'}
BOARD_ROLES = ('plank', 'wood', 'wood_light', 'wood_weathered')
PALETTE = {
    'plank': (.65, .37, .16), 'wood': (.43, .22, .085),
    'wood_light': (.81, .59, .32), 'wood_weathered': (.57, .46, .33),
    'wood_dark': (.22, .115, .055), 'canvas': (.88, .77, .52),
    'iron': (.16, .17, .145), 'brass': (.72, .46, .14),
    'gold': (.98, .67, .16), 'gold_drip': (1.0, .48, .06),
    'clock_face': (.98, .91, .72), 'glass': (.48, .51, .37),
    'lantern_glow': (1.0, .86, .39), 'sand_glow': (1.0, .69, .20),
    'stardust': (1.0, .97, .84),
}
MATS = {}
PARTS = []
DECK = []
FLOATING = []
SEED = 48173


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
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Batch solids by role before beveling to avoid hundreds of operators."""

    def __init__(self, label, verts, faces, role, bevel=True):
        self.label, self.role = label, role
        self.verts = [Vector(v) for v in verts]
        self.faces = faces
        self.bevel = bevel and role in HARD
        PARTS.append(self)

    def transform(self, loc=(0, 0, 0), rot=(0, 0, 0)):
        matrix = Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4()
        self.verts = [matrix @ v for v in self.verts]
        return self


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    return Part(label, [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def tube(label, points, radius, role, sides=6, bevel=False):
    assert radius >= .02, 'Details must be at least 0.04 m across'
    points = [Vector(p) for p in points]
    verts = []
    for i, point in enumerate(points):
        tangent = (points[min(i+1,len(points)-1)] - points[max(0,i-1)]).normalized()
        reference = Vector((1,0,0)) if abs(tangent.x) < .9 else Vector((0,1,0))
        u = tangent.cross(reference).normalized()
        v = tangent.cross(u)
        verts += [point + radius*(u*math.cos(j*math.tau/sides) +
                                 v*math.sin(j*math.tau/sides)) for j in range(sides)]
    faces = [tuple(reversed(range(sides)))]
    faces += [(i*sides+j, i*sides+(j+1)%sides,
               (i+1)*sides+(j+1)%sides, (i+1)*sides+j)
              for i in range(len(points)-1) for j in range(sides)]
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    return Part(label, verts, faces, role, bevel)


def ring(label, loc, radius, role, plane='YZ', squash=1.0):
    """Closed eight-sided ring, four-sided 0.04 m wire; no open seam."""
    verts = []
    for i in range(8):
        a = i*math.tau/8
        for j in range(4):
            b = j*math.tau/4
            r = radius + .02*math.cos(b)
            p = (r*math.cos(a), r*math.sin(a)*squash, .02*math.sin(b))
            xyz = (p[2],p[0],p[1]) if plane == 'YZ' else (p[0],p[2],p[1])
            verts.append(Vector(loc)+Vector(xyz))
    faces = [(i*4+j, i*4+(j+1)%4, ((i+1)%8)*4+(j+1)%4,
              ((i+1)%8)*4+j) for i in range(8) for j in range(4)]
    return Part(label, verts, faces, role, bevel=False)


def mote(label, loc, radius=.025, role='stardust'):
    x, y, z = loc
    return Part(label, [(x+radius,y,z), (x,y+radius,z), (x-radius,y,z),
                        (x,y-radius,z), (x,y,z+radius*1.5), (x,y,z-radius*1.5)],
                [(i,(i+1)%4,k) for k in (4,5) for i in range(4)], role, False)


def sag(y):
    return -.25*(1-(y/5)**2)


def deck_board(y0, y1, index, snapped=0):
    """Cross plank with analytic sag and flush ends; splintered inner edge."""
    offset = .018*math.sin(index*2.7) if index % 5 == 2 else 0
    left, right = -1.6+max(0,offset), 1.6+min(0,offset)
    if snapped:
        # Polygon runs along a ragged fracture, entirely inside the 1.5 m stub.
        edge = y1 if snapped > 0 else y0
        outer = y0 if snapped > 0 else y1
        xs = [left, -.96, -.48, -.08, .39, .91, right]
        jagged = [(x,edge-snapped*(.015,.13,.025,.18,.045,.16,.01)[i])
                  for i,x in enumerate(xs)]
        polygon = [(left,outer),(right,outer)] + list(reversed(jagged))
    else:
        polygon = [(left,y0),(right,y0),(right,y1),(left,y1)]
    n = len(polygon)
    verts = [(x,y,sag(y)+dz) for dz in (-.14,0) for x,y in polygon]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    part = Part('Snapped cross plank' if snapped else 'Cross plank',
                verts,faces,BOARD_ROLES[index%4])
    DECK.append(part)
    # Broad nail heads live outside the clear 2.6 m walking strip.
    if index % 2 == 0:
        for x in (-1.39,1.39):
            box('Flush iron nail', (x,(y0+y1)/2,sag((y0+y1)/2)-.012),
                (.055,.055,.04),'iron',bevel=False)


def side_beam(x, a, b, snapped=False):
    # Solid bent rectangular beam, not a round log; no post in walking space.
    ys = [a+(b-a)*i/10 for i in range(11)]
    verts = [(x+dx,y,sag(y)+dz) for y in ys
             for dx,dz in ((-.13,-.33),(.13,-.33),(.13,-.12),(-.13,-.12))]
    if snapped:
        start = 0 if a > 0 else 40
        for k in range(4):
            old = verts[start+k]
            verts[start+k] = (
                old[0], (a if a > 0 else b) +
                (1 if a > 0 else -1)*(.02,.12,.04,.16)[k], old[2])
    faces = [(3,2,1,0)]
    faces += [(i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j)
              for i in range(10) for j in range(4)]
    faces.append(tuple(range(40,44)))
    Part('Dark longitudinal side beam',verts,faces,'wood_dark')


def lantern(x, y, lit):
    # Bracket projects ALONG Y, keeping all metal outside |X| < 1.30.
    direction = -1 if y > 0 else 1
    ly = y+direction*.38
    tube('Forged lantern bracket',[(x,y,1.89),(x,ly,1.89),(x,ly,1.77)],
         .025,'iron',sides=4,bevel=True)
    ring('Lantern hanging bail',(x,ly,1.72),.065,'iron',plane='XZ')
    box('Lantern glass panes',(x,ly,1.43),(.18,.18,.32),'glass')
    # The visible bulb is recessed between the frame corners, in front of glass.
    box('Lantern bulb' if lit else 'Cold lantern interior',
        (x,ly+.099,1.43),(.085,.04,.19),'lantern_glow' if lit else 'glass')
    for dx in (-.108,.108):
        for dy in (-.108,.108):
            tube('Lantern iron corner',[(x+dx,ly+dy,1.23),(x+dx,ly+dy,1.63)],
                 .022,'iron',sides=4,bevel=True)
    box('Lantern base',(x,ly,1.22),(.26,.26,.06),'iron')
    # Pitched, chunky cap reads as a western oil lantern at gameplay scale.
    verts = [(-.13,-.13,0),(.13,-.13,0),(.13,.13,0),(-.13,.13,0),
             (-.065,-.065,.10),(.065,-.065,.10),(.065,.065,.10),(-.065,.065,.10)]
    Part('Lantern sloped hood',verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),
         (1,2,6,5),(2,3,7,6),(3,0,4,7)],'iron').transform((x,ly,1.64))
    box('Brass lamp foot',(x,ly,1.275),(.18,.18,.04),'brass')


def clock_badge(x, y):
    # Small sagging pocket-watch plaque on the gate's +Y face.
    ring('Pocket watch brass bezel',(x,y+.151,.70),.10,'brass',plane='XZ')
    box('Cream watch dial',(x,y+.149,.70),(.14,.04,.14),'clock_face')
    for dx,dz in ((0,.067),(.067,0),(0,-.067),(-.067,0)):
        box('Gold hour marker',(x+dx,y+.178,.70+dz),(.04,.04,.04),'gold',bevel=False)
    tube('Frozen clock hand',[(x-.042,y+.187,.75),(x,y+.187,.70),
                             (x+.047,y+.187,.70)],.02,'iron',sides=4)
    tube('Molten watch drip',[(x+.07,y+.17,.64),(x+.08,y+.17,.54),
                             (x+.05,y+.17,.48)],.025,'gold_drip',sides=5)


def post(x, y, gate):
    base = sag(y)
    height = 2.02 if gate else 1.29
    box('Tall gate post' if gate else 'Rail post',
        (x,y,base+(height-.25)/2),(.28,.28,height+.25),'wood_dark')
    box('Iron post cap',(x,y,base+height),(.30,.30,.08),'iron')
    box('Iron post shoe',(x,y,base-.10),(.29,.29,.20),'iron')
    for z in (.54,1.10):
        ring('Brass rope eye',(x+math.copysign(.045,x),y,base+z),.082,'brass')
    if gate:
        lantern(x,y,True)  # Material switched to glass for the broken state below.
        clock_badge(x,y)


def loose_end(x, y, z, direction=1):
    tube('Dangling frayed rope',[(x,y,z),(x,y+direction*.10,z-.24),
         (x-.045,y+direction*.20,z-.51),(x+.025,y+direction*.17,z-.68)],
         .035,'canvas')
    for dx in (-.035,.035):
        tube('Rope end strand',[(x+dx,y+direction*.17,z-.64),
             (x+dx*1.5,y+direction*.20,z-.79)],.02,'canvas',sides=4)


def chain(x, y):
    for i in range(3):
        ring('Loose iron chain link',(x,y,sag(y)-.36-i*.135),.062,'iron',
             plane='XZ' if i%2 else 'YZ',squash=1.4)


def vanishing_gate_fragments():
    # Four rising layers, diminishing and sparse; all above the outside posts.
    for end in (-1,1):
        for layer,count in enumerate((3,2,1,1)):
            for j in range(count):
                side = -1 if (j+layer)%2 else 1
                size = (.105,.082,.061,.045)[layer]
                x = side*(1.43+random.uniform(-.025,.025))
                y = end*(4.74+random.uniform(-.07,.07))
                z = 2.18+layer*.20+random.uniform(0,.035)
                box('Rising cap fragment',(x,y,z),(size,size,size),
                    ('wood_light','iron','brass')[j%3],
                    (.13*layer,.19*j,.27*layer))
                mote('Rising time sand',(x-.045,y-.035,z+.09),.023,'sand_glow')


def build(broken=False):
    random.seed(SEED)
    if broken:
        # 1.5 m stubs imply a 7 m opening: the central 6 m AND two 0.5 m
        # fracture margins are absent. No hidden complete deck/beam/rope remains.
        for end in (-1,1):
            for i in range(5):
                a = 3.5+i*.30
                b = a+.282 if i < 4 else 5.0
                y0,y1 = (a,b) if end > 0 else (-b,-a)
                deck_board(y0,y1,i,(-end if i == 0 else 0))
        spans = [(-5,-3.5),(3.5,5)]
        post_ys = (-4.78,-3.66,3.66,4.78)
    else:
        for i in range(34):
            a = -5+i*10/34
            b = -5+(i+1)*10/34
            deck_board(a if i == 0 else a+.009,
                       b if i == 33 else b-.009,i)
        spans = [(-5,5)]
        post_ys = (-4.78,-1.62,1.62,4.78)
    for side in (-1,1):
        x = side*1.45
        for a,b in spans:
            side_beam(x,a,b,broken)
        for y in post_ys:
            first = len(PARTS)
            post(x,y,abs(y)>4)
            if broken:
                for part in PARTS[first:]:
                    if part.role == 'lantern_glow':
                        part.role = 'glass'
        for a,b in zip(post_ys[:-1],post_ys[1:]):
            if broken and a < 0 < b:
                continue
            for level in (.54,1.10):
                points = []
                for i in range(9):
                    t = i/8
                    y = a+(b-a)*t
                    z = (1-t)*sag(a)+t*sag(b)+level-.13*4*t*(1-t)
                    points.append((x,y,z))
                tube('Thick sagging rope rail',points,.052,'canvas')
        # Sparse vertical ties make the suspension structure readable.
        for y in ((-4.1,-3.8,3.8,4.1) if broken else (-3.8,-2.7,-.8,.8,2.7,3.8)):
            tube('Suspension tie',[(x,y,sag(y)-.13),(x,y,sag(y)+.81)],
                 .027,'canvas',sides=5)
        for y in (-4.37,4.37):
            loose_end(x,y,sag(y)-.20,1 if y < 0 else -1)
            chain(x,y)
        if broken:
            for end in (-1,1):
                y = end*3.66
                for level in (.54,1.10):
                    # Broken rail curls down from the last surviving post.
                    tube('Snapped rail hanging into void',[(x,y,sag(y)+level),
                         (x,end*3.32,sag(y)+level-.25),
                         (x-.05,end*3.13,sag(y)+level-.72),
                         (x+.035,end*3.18,sag(y)+level-1.18)],.052,'canvas')
                loose_end(x,end*3.19,-.38,-end)
        for i in range(9):
            y = -4.5+i*1.125
            if broken and abs(y) < 3.5:
                continue
            mote('Rail stardust',(side*1.50,y,sag(y)+1.27+random.uniform(0,.13)))
    vanishing_gate_fragments()
    if broken:
        random.seed(SEED+1)
        for i in range(18):
            # Each disconnected plank has its own seeded roll/pitch/yaw;
            # the middle planks sink furthest, leaving the passage visibly open.
            t = (i+.5)/18
            y = -3.03+6.06*t+random.uniform(-.11,.11)
            x = random.uniform(-.38,.38)
            z = -.48-.84*math.sin(math.pi*t)+random.uniform(-.18,.10)
            part = box('Drifting missing plank',(x,y,z),
                       (random.uniform(1.76,2.25),random.uniform(.19,.27),.14),
                       BOARD_ROLES[i%4],
                       (random.uniform(-.60,.60),random.uniform(-.55,.55),
                        random.uniform(-.44,.44)))
            FLOATING.append(part)
        for end in (-1,1):
            for strand in range(3):
                for i in range(8):
                    t = i/7
                    mote('Sand trailing from snapped deck',
                         ((strand-1)*.75+random.uniform(-.10,.10),
                          end*(3.45-.80*t)+random.uniform(-.07,.07),
                          -.18-t*.95+random.uniform(-.07,.07)),
                         .035-.012*t,'sand_glow')


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def visible_materials(obj):
    obj.data.calc_loop_triangles()
    tris = list(obj.data.loop_triangles)
    verts = [v.co.copy() for v in obj.data.vertices]
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    view = Vector((0,math.cos(math.radians(55)),math.sin(math.radians(55))))
    areas = defaultdict(float)
    for i,tri in enumerate(tris):
        facing = tri.normal.dot(view)
        if facing <= 0:
            continue
        center = sum((verts[j] for j in tri.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*40,-view,40.01)
        if hit == i:
            areas[obj.data.materials[tri.material_index].name] += tri.area*facing
    total = sum(areas.values())
    assert total > 0
    return {role:area/total for role,area in areas.items()}


def finish(name, broken):
    # Validate semantic geometry BEFORE batching, independent of render framing.
    assert len(DECK) == (10 if broken else 34)
    assert len(FLOATING) == (18 if broken else 0)
    for end in (-5,5):
        terminal = [v for p in DECK for v in p.verts if abs(v.y-end)<1e-6]
        assert terminal and abs(max(v.z for v in terminal)) < 1e-6
    for part in PARTS:
        assert all(abs(v.x)<=1.60001 and abs(v.y)<=5.00001 for v in part.verts), part.label
        if part not in DECK and part not in FLOATING:
            # Every high railing/decoration stays outside the walking strip.
            assert all(abs(v.x)>=1.2999 or v.z<=.06 for v in part.verts), part.label
    if broken:
        assert all(abs(v.y)>=3.4999 for p in DECK for v in p.verts)
        assert not any(p.role == 'lantern_glow' for p in PARTS)
    buckets = defaultdict(list)
    for part in PARTS:
        buckets[(part.role,part.bevel)].append(part)
    objects = []
    for (role,bevel),parts in buckets.items():
        verts,faces = [],[]
        for part in parts:
            offset = len(verts)
            verts.extend(part.verts)
            faces.extend(tuple(offset+i for i in face) for face in part.faces)
        data = bpy.data.meshes.new(role+' solids')
        data.from_pydata(verts,[],faces)
        data.materials.append(MATS[role])
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(role+' solids',data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Worn edges 0.02m / 2 segments','BEVEL')
            mod.width,mod.segments = .02,2
            # Leave the exact lip seam square, with the remaining wood edges
            # beveled. A bevel across a sloping deck endpoint lowers its top.
            weights = data.attributes.new('bevel_weight_edge','FLOAT','EDGE')
            for edge in data.edges:
                seam = any(abs(abs(data.vertices[i].co.y)-5)<1e-6
                           for i in edge.vertices)
                weights.data[edge.index].value = 0 if seam else 1
            mod.limit_method = 'WEIGHT'
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
        objects.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name,obj.data.name = name,name+'_Mesh'
    # Keep the prop budget without changing its dimensions or module datum.
    if triangles(obj) > 6000:
        protected = obj.vertex_groups.new(name='Preserve module lips and width')
        protected.add([v.index for v in obj.data.vertices],0,'REPLACE')
        protected.add([v.index for v in obj.data.vertices
                       if abs(v.co.x)>1.5999 or abs(v.co.y)>4.9999],1,'REPLACE')
        mod = obj.modifiers.new('Bridge prop triangle budget','DECIMATE')
        mod.ratio,mod.delimit = 5850/triangles(obj),{'MATERIAL'}
        mod.vertex_group,mod.vertex_group_factor = protected.name,1000
        mod.invert_vertex_group = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), name+' contains an open solid'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    assert triangles(obj) <= 6000, (name,triangles(obj))
    assert obj.location.length < 1e-6 and tuple(obj.scale) == (1,1,1)
    assert abs(obj.dimensions.x-3.2)<1e-4 and abs(obj.dimensions.y-10)<1e-4, (name,tuple(obj.dimensions))
    for end in (-5,5):
        terminal = [v.co for v in obj.data.vertices
                    if abs(v.co.y-end)<1e-5 and v.co.z>-.15]
        assert terminal and abs(max(v.z for v in terminal))<1e-5, (
            name,'deck end datum',end,[tuple(v) for v in terminal])
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    used = {obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used) >= 5 and {'sand_glow','stardust','canvas','brass'} <= used
    assert ('lantern_glow' in used) == (not broken)
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert len(mat.node_tree.nodes)==2
        shader = next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        assert tuple(shader.inputs['Base Color'].default_value)==(.5,.5,.5,1)
    coverage = visible_materials(obj)
    assert len(coverage)>=5 and max(coverage.values())<=.55, (name,coverage)
    print('VISIBLE '+name+' '+str({r:round(s,3) for r,s in coverage.items()}),flush=True)
    obj['origin_convention'] = 'deck center datum; end deck tops at Z=0'
    obj['length_axis'],obj['walkway_width_m'] = 'Y',2.6
    obj['connection_y_m'],obj['deck_sag_m'] = [-5.0,5.0],.25
    obj['broken'],obj['floating_planks'],obj['seed'] = broken,len(FLOATING),SEED
    obj['triangle_count'],obj['vanish_layers'] = triangles(obj),4
    destination = OUTPUT / ('Kit_V_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    """Review colours/placement only AFTER exports; restore them on completion."""
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
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.4,1.1
    shading.show_specular_highlight = False
    shading.show_object_outline = True
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.045,.038,.030)
    old_locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role],1)
        for obj,x in zip(objects,(-2.45,2.45)):
            obj.location.x = x
        target = Vector((0,0,.35))
        a = math.radians(55)
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',19.2
        camera.location = target+Vector((0,35*math.cos(a),35*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.30
            data.materials.append(MATS['clock_face'])
            caption = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(caption)
            caption.location = (obj.location.x,5.8,-.08)
            caption.rotation_euler = camera.rotation_euler
            helpers.append(caption)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper,do_unlink=True)
        for obj,location in zip(objects,old_locations):
            obj.location = location
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    exported = []
    for name,broken in (('Bridge',False),('BridgeBroken',True)):
        PARTS.clear()
        DECK.clear()
        FLOATING.clear()
        build(broken)
        exported.append(finish(name,broken))
    assert len(scene.objects)==2 and all(o.type=='MESH' for o in scene.objects)
    preview(exported)
    assert len(scene.objects)==2 and all(o.type=='MESH' for o in scene.objects)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
