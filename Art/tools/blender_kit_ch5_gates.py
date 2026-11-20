# 시간의 끝: 파괴형 문, 통로, 포털과 천상의 보상 문을 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_gates.py
"""Chapter 5 collapsing cosmos kit; deterministic geometry, gray FBX, colored review only."""
import hashlib
import math
import random
from pathlib import Path
import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_gates_preview.png'
PALETTE = {
    'void_stone': (.47,.35,.66), 'void_stone_dark': (.17,.12,.28),
    'marble': (.83,.80,.94), 'marble_dark': (.40,.36,.54),
    'violet': (.57,.29,.79), 'violet_dark': (.25,.12,.38),
    'indigo': (.19,.19,.39), 'night_sky': (.055,.04,.14),
    'starlight': (.91,.95,1), 'crack_gold': (1,.85,.40),
    'gold': (.94,.67,.27), 'gold_dark': (.43,.28,.16),
    'mirror': (.56,.78,.91), 'mirror_back': (.22,.31,.45),
    'crystal': (.70,.46,.95), 'magenta': (.89,.18,.62),
    'wood': (.48,.35,.43), 'wood_dark': (.24,.17,.29),
    'iron': (.31,.33,.47), 'iron_dark': (.13,.15,.24),
    'steel': (.61,.67,.79), 'brass': (.66,.48,.30),
    'dial_face': (.90,.85,.73), 'dial_mark': (.15,.11,.25),
    'sand': (.67,.55,.65), 'red_lacquer': (.51,.15,.30),
    'cloth': (.38,.23,.53), 'lamp_warm': (1,.69,.34),
    'porcelain': (.95,.91,.98), 'velvet': (.30,.12,.40),
}
HARD = set(PALETTE) - {'night_sky', 'starlight', 'crack_gold', 'cloth', 'velvet'}
MATS, PARTS, ASSETS = {}, [], []



def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], out.inputs['Surface'])
        MATS[role] = mat
    OUTPUT.mkdir(parents=True, exist_ok=True)


def recalc(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), obj.name + ': open component'
    assert all(face.calc_area() > 1e-11 for face in bm.faces), obj.name + ': zero area face'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for face in obj.data.polygons:
        face.use_smooth = False


def mesh(label, verts, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    PARTS.append(obj)
    recalc(obj)
    if bevel and role in HARD:
        mod = obj.modifiers.new('Handmade edge bevel', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(35)
        mod.use_clamp_overlap = True
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, bevel=True, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
    return place(mesh(label, [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
                              (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)],
                      [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
                       (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], role, bevel), loc, rot)


def prism(label, profile, back, front, role, bevel=True, back_role=None):
    n = len(profile)
    obj = mesh(label, [(x, y, z) for y in (back, front) for x, z in profile],
               [tuple(reversed(range(n))), tuple(range(n, n * 2))] +
               [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)],
               role, bevel)
    if back_role:
        obj.data.materials.append(MATS[back_role])
        obj.data.polygons[0].material_index = 1
        for face in list(obj.data.polygons)[2:]:
            face.material_index = 1
    return obj


def cylinder(label, loc, radius, height, role, n=8, top=None, rot=(0, 0, 0), bevel=True):
    top = radius if top is None else top
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for r, z in ((radius, -height / 2), (top, height / 2)) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    faces += [(j, (j + 1) % n, (j + 1) % n + n, j + n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def tube(label, points, radius, role, n=6):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i + 1, len(points) - 1)]) -
                   Vector(points[max(0, i - 1)])).normalized()
        ref = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        side = ref.cross(tangent).normalized()
        other = tangent.cross(side)
        verts += [tuple(Vector(point) + radius *
                        (side * math.cos(j * math.tau / n) + other * math.sin(j * math.tau / n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(len(verts) - n, len(verts)))]
    faces += [(i * n + j, i * n + (j + 1) % n, (i + 1) * n + (j + 1) % n,
               (i + 1) * n + j) for i in range(len(points) - 1) for j in range(n)]
    return mesh(label, verts, faces, role, bevel=False)


def apply_modifiers(obj):
    activate(obj)
    had_modifiers = bool(obj.modifiers)
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if had_modifiers:
        # A clamped bevel can meet itself on a shallow inlay. Remove its collapsed
        # edges before joining, without welding separate physics fragments.
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
        bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-7)
        bm.to_mesh(obj.data)
        bm.free()
        recalc(obj)


def join(objects, name, origin=(0, 0, 0)):
    assert objects
    for obj in objects:
        apply_modifiers(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    bpy.context.scene.cursor.location = origin
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    recalc(obj)
    return obj


def restore_interfaces(obj, paths, back, front):
    """Reference wood gate technique: bevel perimeter, restore shared cut faces."""
    apply_modifiers(obj)
    corners = [Vector(p) for path in paths for p in path]
    segments = [(Vector(a), Vector(b)) for path in paths for a, b in zip(path, path[1:])]
    for v in obj.data.vertices:
        p = Vector((v.co.x, v.co.z))
        corner = min(corners, key=lambda c: (c - p).length_squared)
        if (corner - p).length <= .043:
            target = corner
        else:
            candidates = []
            for a, b in segments:
                d = b - a
                t = min(1, max(0, (p - a).dot(d) / d.length_squared))
                candidates.append(a + t * d)
            target = min(candidates, key=lambda c: (c - p).length_squared)
        if (target - p).length <= .043:
            v.co.x, v.co.z = target
            if abs(v.co.y - back) <= .023:
                v.co.y = back
            elif abs(v.co.y - front) <= .023:
                v.co.y = front
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-6)
    bmesh.ops.dissolve_limit(bm, angle_limit=1e-5, verts=list(bm.verts),
                           edges=list(bm.edges), use_dissolve_boundaries=False)
    bm.to_mesh(obj.data)
    bm.free()
    recalc(obj)


def fracture(a, b, amplitude=.06, teeth=2):
    a, b = Vector(a), Vector(b)
    d = b - a
    normal = Vector((-d.y, d.x)).normalized()
    return [tuple(a)] + [tuple(a.lerp(b, j / (teeth * 2)) + normal *
                              amplitude * (1 if j % 2 else -1))
                         for j in range(1, teeth * 2)] + [tuple(b)]


def gate_piece(start, pieces):
    obj = join(PARTS[start:], f'Piece_{len(pieces):02d}')
    activate(obj)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    pieces.append(obj)


def world_vertices(objects):
    bpy.context.view_layer.update()
    return [obj.matrix_world @ v.co for obj in objects for v in obj.data.vertices]


def bounds(objects):
    verts = world_vertices(objects)
    return (Vector(tuple(min(v[i] for v in verts) for i in range(3))),
            Vector(tuple(max(v[i] for v in verts) for i in range(3))))


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def validate(name, objects):
    low, high = bounds(objects)
    dims = high - low
    used_roles = {obj.data.materials[p.material_index].name
                  for obj in objects for p in obj.data.polygons}
    assert len(used_roles) >= 4, (name, 'Needs four role materials', used_roles)
    for obj in objects:
        recalc(obj)
        assert obj.type == 'MESH' and obj.parent is None
        assert all(abs(s - 1) < 1e-6 for s in obj.scale)
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        for mat in obj.data.materials:
            assert mat.name in PALETTE
            assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
            assert tuple(mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value) == (.5, .5, .5, 1)
            assert all(node.type != 'TEX_IMAGE' for node in mat.node_tree.nodes)
    if name.startswith('Gate'):
        count = len(objects) - 3
        assert 10 <= count <= 24
        assert [o.name for o in objects] == [f'Piece_{i:02d}' for i in range(count)] + [f'Splinter_{i}' for i in range(3)]
        assert abs(low.x + 2.3) < 1e-4 and abs(high.x - 2.3) < 1e-4, (name, low, high)
        assert abs(low.z) < 1e-4 and 2.4 <= high.z <= 2.7, (name, low, high)
        assert dims.y <= .3501 and abs((high.y + low.y) / 2) < 1e-5, (name, dims)
        for obj in objects:
            verts = [v.co for v in obj.data.vertices]
            centre = Vector(tuple((min(v[i] for v in verts) + max(v[i] for v in verts)) / 2 for i in range(3)))
            assert centre.length < 1e-5, (obj.name, centre)
        for obj in objects[-3:]:
            assert (obj.location - Vector((0, 0, 1.3))).length < 1e-5
            assert .2 <= max(obj.dimensions) <= .45
        budget = 6000
    elif name.startswith('Corridor'):
        assert len(objects) == 1 and objects[0].location.length < 1e-6
        assert abs(low.y + 5.2) < 1e-4 and abs(high.y - 5.2) < 1e-4, (name, low, high)
        assert low.x >= -2.9001 and high.x <= 2.9001 and high.z <= 2.5
        if name == 'Corridor_S3':
            assert high.z <= 1.1001, (name, high)
        # The bridge also retains the specified flush floor elevation.
        assert any(abs(v.z - .02) < 1e-5 for v in world_vertices(objects))
        budget = 7000
    elif name.startswith('Portal'):
        assert len(objects) == 1 and objects[0].location.length < 1e-6
        assert low.x >= -3.4001 and high.x <= 3.4001
        assert low.y >= -.6001 and high.y <= .6001
        assert abs(low.z) < 1e-4 and 3.4 <= high.z <= 4.2
        for v in world_vertices(objects):
            assert not (-2.2999 < v.x < 2.2999 and v.z < 2.8999), (name, 'Obstructed opening', v)
        # Test triangulated faces, too: no beam may span across the clear aperture.
        for obj in objects:
            obj.data.calc_loop_triangles()
            for tri in obj.data.loop_triangles:
                coords = [obj.matrix_world @ obj.data.vertices[i].co for i in tri.vertices]
                if max(p.z for p in coords) < 2.9:
                    assert min(p.x for p in coords) >= 2.2999 or max(p.x for p in coords) <= -2.2999
        budget = 4000
    else:
        assert [obj.name for obj in objects] == ['Frame', 'Leaf_L', 'Leaf_R']
        assert abs(dims.x - 3) < 1e-4 and abs(dims.z - 3) < 1e-4 and abs(low.z) < 1e-4
        assert .45 <= dims.y <= .55, (name, low, high, dims)
        for obj, side in zip(objects[1:], (-1, 1)):
            assert abs(obj.location.x - side * 1.115) < 1e-5
            assert abs(obj.location.y) < 1e-6 and abs(obj.location.z) < 1e-6
        budget = 5000
    count = sum(triangles(obj) for obj in objects)
    assert count <= budget, (name, count, budget)
    return count, tuple(round(v, 4) for v in dims)


def export(name, objects):
    count, dims = validate(name, objects)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        obj['chapter'], obj['front'] = 'The Collapsing End of Time', '+Y'
    bpy.context.view_layer.objects.active = objects[0]
    path = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert path.is_file() and path.stat().st_size > 1024
    print(f'EXPORTED {name} {len(objects)} {count} {dims}', flush=True)
    ASSETS.append((name, objects, path, hashlib.sha256(path.read_bytes()).hexdigest()))
    # Exact flat physics names are reused in the next FBX; review names are unique.
    for obj in objects:
        obj.name = name + '__' + obj.name


def rivet(x, y, z, role='starlight', radius=.045):
    return cylinder('Broad rivet', (x, y, z), radius, .025, role,
                    n=6, rot=(math.pi / 2, 0, 0), bevel=False)


def beam(label, a, b, width, depth, role, bevel=False):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0, 0, 0), (width, depth, (b-a).length), role, bevel)
    obj.data.transform(Matrix.Translation((a+b)/2) @
                       (b-a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return obj


def arc_band(label, center, rx, rz, inner, a, b, depth, role,
             steps=16, teeth=0):
    """Closed annular strip in XZ, with optional chunky outward teeth."""
    cx, cy, cz = center
    full = abs(b-a-math.tau) < 1e-6
    count = steps if full else steps+1
    verts = []
    for i in range(count):
        t = a+(b-a)*i/steps
        tooth = 1.065 if teeth and i % 4 in (1, 2) else 1
        for scale, y in ((tooth, -depth/2), (inner, -depth/2),
                         (tooth, depth/2), (inner, depth/2)):
            verts.append((cx+rx*scale*math.cos(t), cy+y,
                          cz+rz*scale*math.sin(t)))
    faces = []
    for i in range(steps):
        j = (i+1) % count
        p, q = i*4, j*4
        faces += [(p,q,q+1,p+1), (p+2,p+3,q+3,q+2),
                  (p,p+2,q+2,q), (p+1,q+1,q+3,p+3)]
    if not full:
        faces += [(0,1,3,2), tuple((count-1)*4+k for k in (0,2,3,1))]
    return mesh(label, verts, faces, role, False)


def transform_since(start, matrix):
    for obj in PARTS[start:]:
        obj.data.transform(matrix)


def star(x, y, z, radius=.16, role='starlight', horizontal=False):
    profile = []
    for j in range(8):
        t = j * math.tau / 8
        r = radius if j % 2 == 0 else radius * .30
        profile.append((r * math.cos(t), r * math.sin(t)))
    obj = prism('Raised four point star', profile, -.022, .022, role, False)
    matrix = Matrix.Translation(Vector((x, y, z)))
    if horizontal:
        matrix @= Matrix.Rotation(math.pi / 2, 4, 'X')
    obj.data.transform(matrix)
    return obj


def jewel(center, width, height, role='crystal'):
    x, y, z = center
    verts = [(x, y, z-height/2), (x-width/2,y,z), (x,y-width*.32,z),
             (x+width/2,y,z), (x,y+width*.32,z), (x,y,z+height/2)]
    return mesh('Faceted floating shard', verts,
                [(0,2,1),(0,3,2),(0,4,3),(0,1,4),
                 (5,1,2),(5,2,3),(5,3,4),(5,4,1)],role,False)


def crack(points, role='crack_gold', radius=.024):
    return tube('Raised gold fracture', points, radius, role, 4)


def scroll(x, z, side=1, scale=1, y=.13):
    return tube('Baroque gold volute',
                [(x+side*scale*a,y,z+scale*b) for a,b in
                 ((0,0),(.12,.20),(.32,.24),(.43,.10),(.34,-.04),(.22,.04))],
                max(.021,.028*scale),'gold',5)


def finish_piece(start, pieces):
    gate_piece(start, pieces)
    obj = pieces[-1]
    roles = {obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(roles) >= 4, (obj.name, roles)


def fan_boundary(rx, rz, cz):
    # Twelve angular rays tile a rectangular pane without a hairline gap.
    return [(-rx,cz-rz),(-rx/3,cz-rz),(rx/3,cz-rz),(rx,cz-rz),
            (rx,cz-rz/3),(rx,cz+rz/3),(rx,cz+rz),
            (rx/3,cz+rz),(-rx/3,cz+rz),(-rx,cz+rz),
            (-rx,cz+rz/3),(-rx,cz-rz/3)]


def gate_mirror():
    pieces=[]
    center=(.13,1.35)
    outline=fan_boundary(2.04,1.08,1.30)
    for j,a in enumerate(outline):
        start=len(PARTS)
        b=outline[(j+1)%len(outline)]
        prism('Complementary angular mirror shard',[center,a,b],-.10,.075,
              'mirror',False,'mirror_back')
        # Broad angular reflection planes, rather than an unbroken blue sheet.
        midpoint=((a[0]+b[0])/2,(a[1]+b[1])/2)
        prism('Mirror reflected half facet',[center,a,midpoint],.073,.113,
              'mirror_back' if j%3 else 'marble',False)
        # A gold fault belongs to this shard only; no cross-piece decoration.
        mid=Vector(center).lerp(Vector(a),.60)
        crack([(center[0],.14,center[1]),(mid.x+.035,.14,mid.y),
               (a[0],.14,a[1])])
        cx=(center[0]+a[0]+b[0])/3; cz=(center[1]+a[1]+b[1])/3
        beam('Broad silver reflection',(cx-.08,.14,cz-.10),
             (cx+.08,.14,cz+.10),.045,.04,'starlight')
        finish_piece(start,pieces)
    # Eight separate frame bars touch exactly, while the pane sits inside them.
    for side in (-1,1):
        for row in range(2):
            start=len(PARTS)
            x=side*2.17; z=.65+row*1.30
            box('Broken baroque jamb',(x,0,z),(.26,.28,1.30),'gold')
            box('Jamb recessed shadow',(x,.147,z),(.13,.04,1.13),'gold_dark',False)
            box('Jamb bright molding',(x-side*.095,.147,z),(.045,.04,1.30),'crack_gold',False)
            scroll(x-side*.03,z-.10,-side,.52,.17)
            jewel((x,.153,z+.30),.12,.23,'crystal')
            finish_piece(start,pieces)
    for top in (False,True):
        for side in (-1,1):
            start=len(PARTS)
            x=side*1.02; z=2.49 if top else .11
            box('Broken gilded cross molding',(x,0,z),(2.04,.28,.22),'gold')
            box('Cross molding recess',(x,.146,z),(1.92,.04,.07),'violet_dark',False)
            scroll(x-.20,z,1,.42,.17)
            star(x+.34,.17,z,.09,'starlight')
            jewel((x-.42,.15,z),.12,.17,'mirror')
            finish_piece(start,pieces)
    return pieces


def shared_cells(cols=4, rows=3, width=4.6, height=2.6):
    """A deterministic irregular grid; neighboring cells share every cut vertex."""
    grid=[]
    for row in range(rows+1):
        line=[]
        for col in range(cols+1):
            x=-width/2+col*width/cols
            z=row*height/rows
            if 0<col<cols: x+=random.uniform(-.14,.14)
            if 0<row<rows: z+=random.uniform(-.13,.13)
            line.append((x,z))
        grid.append(line)
    for row in range(rows):
        for col in range(cols):
            corners=[grid[row][col],grid[row][col+1],
                     grid[row+1][col+1],grid[row+1][col]]
            # A shared kink is computed from its endpoints, independent of order.
            path=[]
            for a,b in zip(corners,corners[1:]+corners[:1]):
                path.append(a)
                if a[0]==b[0] and abs(a[0])==width/2: continue
                if a[1]==b[1] and a[1] in (0,height): continue
                lo,hi=sorted((a,b))
                mid=((lo[0]+hi[0])/2+.055,(lo[1]+hi[1])/2+.045)
                path.append(mid)
            yield col,row,path


def gate_memories():
    pieces=[]
    roles=('wood','steel','sand','brass')
    darks=('wood_dark','iron_dark','marble_dark','gold_dark')
    for col,row,path in shared_cells():
        start=len(PARTS)
        obj=prism('Interlocked chapter remnant',path,-.12,.08,roles[col],True,darks[col])
        restore_interfaces(obj,[path+[path[0]]],-.12,.08)
        cx=sum(p[0] for p in path)/len(path); cz=sum(p[1] for p in path)/len(path)
        if col==0:
            for dz in (-.20,.20):
                box('Saloon plank groove',(cx,.101,cz+dz),(.65,.042,.05),'wood_dark',False)
            box('Saloon iron strap',(cx-.24,.12,cz),(.10,.06,.47),'iron',False)
            rivet(cx-.24,.16,cz+.13,'gold',.05)
        elif col==1:
            box('Station enamel remnant',(cx,.108,cz),(.58,.05,.42),'indigo',False)
            box('Station chrome inset',(cx,.14,cz),(.35,.04,.08),'steel',False)
            rivet(cx-.39,.13,cz,'gold',.05)
        elif col==2:
            box('Sandstone violet relief',(cx,.104,cz),(.46,.045,.43),'violet_dark',False)
            star(cx,.14,cz,.17,'gold')
        else:
            arc_band('Clock gear remnant inset',(cx,.115,cz),.31,.31,.63,
                     -.2,2.7,.055,'gold',8,2)
            rivet(cx,.15,cz,'steel',.075)
        crack([(cx-.30,.15,cz-.26),(cx-.12,.15,cz-.06),(cx-.18,.15,cz+.05)])
        finish_piece(start,pieces)
    # Separate turnstile arms provide clear station silhouettes over the collage.
    for side in (-1,1):
        start=len(PARTS)
        beam('Station turnstile steel bar',(0,.115,1.29),(side*1.73,.115,1.45),
             .15,.085,'steel')
        box('Iron turnstile boss',(side*.12,.11,1.31),(.24,.13,.23),'iron_dark')
        box('Golden weld collar',(side*.75,.165,1.36),(.12,.04,.17),'crack_gold',False)
        rivet(side*.12,.175,1.31,'gold',.07)
        finish_piece(start,pieces)
    return pieces


def gate_islands():
    pieces=[]
    for col,row,path in shared_cells():
        start=len(PARTS)
        obj=prism('Interlocked faceted void rock',path,-.13,.075,'void_stone',True,'void_stone_dark')
        restore_interfaces(obj,[path+[path[0]]],-.13,.075)
        cx=sum(p[0] for p in path)/len(path); cz=sum(p[1] for p in path)/len(path)
        # A large inset facet breaks up the visible stone, rather than micro-noise.
        inset=[(cx+(x-cx)*.66,cz+(z-cz)*.66) for x,z in path]
        n=len(inset)
        verts=[(x,.076,z) for x,z in inset]+[(cx+.08,.13,cz-.06)]
        faces=[tuple(reversed(range(n)))]+[(i,(i+1)%n,n) for i in range(n)]
        facet=mesh('Rock raised triangular facets',verts,faces,'marble_dark',False)
        facet.data.materials.append(MATS['void_stone_dark'])
        facet.data.materials.append(MATS['violet_dark'])
        for face in facet.data.polygons: face.material_index=face.index%3
        jewel((cx,.11,cz),.30,.51,'crystal')
        if row!=1: jewel((cx+.20,.11,cz+.09),.16,.27,'mirror')
        crack([(cx-.27,.13,cz-.20),(cx-.06,.13,cz-.07),(cx-.13,.13,cz+.24)])
        finish_piece(start,pieces)
    for row in range(3):
        start=len(PARTS)
        z=.42+row*.84
        # Bands are independently flung physics parts, not one joined overlay.
        crack([(-2.27,.14,z),(-1.3,.14,z+.09),(-.28,.14,z-.045),
               (.7,.14,z+.08),(1.5,.14,z-.055),(2.27,.14,z)],radius=.025)
        for x in (-1.35,0,1.35):
            box('Energy band gold clasp',(x,.11,z),(.15,.10,.18),'gold',False)
            box('Clasp dark cut',(x,.164,z),(.055,.04,.10),'void_stone_dark',False)
            star(x,.17,z,.07,'starlight')
        finish_piece(start,pieces)
    return pieces


def gate_clock():
    pieces=[]
    n=10; center=(0,1.3)
    angles=[0,.40,.85,math.pi/2,2.30,math.pi,3.55,4.05,math.pi*1.5,5.50]
    ring=[(2.3*.87*math.cos(t),1.3+1.3*.87*math.sin(t)) for t in angles]
    for j,a in enumerate(ring):
        start=len(PARTS)
        b=ring[(j+1)%n]
        prism('Broken clock face wedge',[center,a,b],-.10,.065,'dial_face',False,'violet_dark')
        t=(angles[j]+(angles[j+1] if j+1<n else math.tau))/2
        inset=[(x*.72,1.3+(z-1.3)*.72) for x,z in [center,a,b]]
        prism('Dial inner indigo sector',inset,.066,.089,'indigo',False)
        crack([(0,.115,1.3),(a[0]*.6+.03,.115,1.3+(a[1]-1.3)*.6),
               (a[0],.115,a[1])])
        x,z=1.65*math.cos(t),1.3+.90*math.sin(t)
        box('Raised numeral hour bar',(x,.105,z),(.085,.045,.24),'dial_mark',False,
            rot=(0,math.pi/2-t,0))
        star(.77*math.cos(t),.12,1.3+.44*math.sin(t),.085,'gold')
        finish_piece(start,pieces)
    for j in range(n):
        start=len(PARTS)
        a,b=angles[j],angles[j+1] if j+1<n else math.tau
        arc_band('Fractured gold clock bezel',(0,0,1.3),2.3,1.3,.87,a,b,.26,'gold',1)
        arc_band('Bezel recessed dark band',(0,.14,1.3),2.3*.965,1.3*.965,.94,
                 a,b,.04,'gold_dark',1)
        t=(a+b)/2
        x,z=2.3*.935*math.cos(t),1.3+1.3*.935*math.sin(t)
        rivet(x,.17,z,'steel',.048)
        # Four roles per removable bezel sector.
        star(x,.17,z,.075,'crack_gold')
        finish_piece(start,pieces)
    start=len(PARTS)
    beam('Fallen clock hand',(-1.46,.115,.77),(1.26,.115,1.63),.14,.09,'dial_mark')
    beam('Hand gilded spine',(-1.38,.171,.80),(1.18,.171,1.60),.045,.04,'gold')
    cylinder('Hand pivot',(0,.12,1.23),.17,.10,'violet',10,rot=(math.pi/2,0,0),bevel=False)
    rivet(0,.195,1.23,'starlight',.085)
    finish_piece(start,pieces)
    return pieces


def splinters(stage):
    result=[]
    for j in range(3):
        length=.26+j*.06
        path=[(-.04,1.3-length/2),(.04,1.3-.04),(.04,1.3+length/2),(-.04,1.3+.03)]
        obj=prism('Hidden debris',path,-.03,.03,
                  {1:'mirror',2:'wood',3:'void_stone',4:'dial_face'}[stage],False)
        obj=join([obj],f'Splinter_{j}')
        activate(obj)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
        result.append(obj)
    return result


def floor(role='marble_dark'):
    box('Floor foundation',(0,0,-.09),(4.6,10.4,.22),role)


def floor_crack(y, x=0, length=4.4):
    crack([(x-length/2,y-.08,.06),(x-length*.21,y+.11,.06),
           (x-.10,y-.07,.06),(x+length*.20,y+.09,.06),
           (x+length/2,y-.06,.06)])


def gear(center, radius=.7, role='gold', broken=True, horizontal=False, low=False):
    start=len(PARTS)
    arc_band('Broken toothed wheel',(0,0,0),radius,radius,.64,
             .17,5.50 if broken else math.tau+.17,.12,role,16 if low else 24,4 if low else 6)
    for j in range(3 if low else 5):
        t=.30+j*(1.80 if low else 1.10)
        beam('Gear broad spoke',(.10*math.cos(t),0,.10*math.sin(t)),
             (radius*.74*math.cos(t),0,radius*.74*math.sin(t)),.12,.10,'brass')
    cylinder('Gear dark hub',(0,0,0),radius*.23,.18,'gold_dark',8,
             rot=(math.pi/2,0,0),bevel=False)
    rivet(0,.12,0,'steel',radius*.11)
    transform=Matrix.Translation(Vector(center))
    if horizontal: transform @= Matrix.Rotation(math.pi/2,4,'X')
    transform_since(start,transform)


def corridor_mirror():
    floor()
    for row in range(8):
        y=-4.55+row*1.30
        for col in range(3):
            x=-1.53+col*1.53
            box('Marble mirror hall tile',(x,y,.035),(1.47,1.24,.03),
                'marble' if (row+col)%3 else 'mirror_back',False)
        if row%2==0:
            star(0,y,.067,.38,'violet',True)
            star(0,y,.090,.16,'gold',True)
        floor_crack(y+.44)
    for side in (-1,1):
        x=side*2.60
        for j,y in enumerate((-4.15,-2.075,0,2.075,4.15)):
            box('Marble shard pedestal foot',(x,y,.10),(.60,.68,.20),'marble_dark')
            box('Marble shard pedestal',(x,y,.42),(.40,.45,.53),'marble')
            box('Pedestal gilt capital',(x,y,.73),(.56,.59,.12),'gold',False)
            jewel((x,y,1.27),.31,.62,'mirror')
            jewel((x-side*.08,y+.13,1.67),.15,.29,'crystal')
            # Local XZ frames turn into corridor-facing YZ silhouettes.
            start=len(PARTS)
            for s in (-1,1):
                beam('Leaning broken mirror jamb',(s*.61,0,0),(s*.73,0,1.75),.11,.13,'gold')
            beam('Broken mirror frame crown',(-.73,0,1.75),(.16,0,1.94),.12,.13,'gold')
            beam('Floating frame segment',(.34,0,2.04),(.73,0,1.90),.12,.13,'gold_dark')
            prism('Mirror frame surviving corner',[(-.55,.35),(-.50,1.50),(-.18,.95)],
                  -.065,.065,'mirror',False,'mirror_back')
            crack([(-.57,.095,.36),(-.35,.095,.69),(-.46,.095,1.07)])
            transform_since(start,Matrix.Translation(Vector((x,y,.16))) @
                            Matrix.Rotation(side*math.pi/2,4,'Z') @
                            Matrix.Rotation((-.09 if j%2 else .09),4,'Y'))


def corridor_memories():
    floor('void_stone_dark')
    for zone in range(4):
        y=-3.9+zone*2.6
        if zone==0:
            for col in range(8):
                box('Saloon plank floor',(-2.0125+col*.575,y,.035),(.545,2.55,.03),
                    'wood' if col%3 else 'wood_dark',False)
            for yy in (y-.90,y+.90):
                box('Saloon floor iron strap',(0,yy,.06),(4.6,.07,.04),'iron',False)
        elif zone==1:
            for col in range(4):
                for row in range(2):
                    box('Station tile',(-1.725+col*1.15,y-.65+row*1.30,.035),
                        (1.10,1.25,.03),'steel' if (col+row)%2 else 'indigo',False)
            for x in (-1.55,1.55):
                box('Station inset iron rail',(x,y,.055),(.09,2.60,.05),'iron',False)
        elif zone==2:
            for col in range(3):
                box('Sandstone paving',(-1.53+col*1.53,y,.035),(1.49,2.55,.03),
                    'sand' if col%2 else 'marble_dark',False)
            star(0,y,.066,.45,'violet',True)
        else:
            for col in range(3):
                box('Clock riveted plate',(-1.53+col*1.53,y,.035),(1.49,2.55,.03),'indigo',False)
                for yy in (y-.95,y+.95):
                    cylinder('Clock plate rivet',(-1.53+col*1.53,yy,.062),.055,.04,'brass',6,bevel=False)
            gear((0,y,-.04),.72,'gold',horizontal=True)
    for y in (-2.60,0,2.60): floor_crack(y)
    for side in (-1,1):
        x=side*2.60
        box('Memory ledge',(x,0,.10),(.60,10.4,.20),'void_stone')
        # Each chapter fragment is bounded by the 60 cm wall band.
        start=len(PARTS)
        for s in (-1,1):
            box('Saloon rail post',(s*.92,0,.86),(.13,.30,1.55),'wood_dark')
        for zz in (.65,1.46):
            box('Saloon railing', (0,0,zz),(2.05,.18,.13),'wood')
        for xx in (-.55,0,.55): box('Saloon spindle',(xx,0,1.04),(.09,.13,.72),'wood',False)
        box('Saloon gilded rail band',(0,.12,1.46),(2.05,.04,.055),'gold',False)
        transform_since(start,Matrix.Translation(Vector((x,-3.9,.10))) @
                        Matrix.Rotation(side*math.pi/2,4,'Z') @ Matrix.Rotation(.055,4,'Y'))
        for yy in (-1.90,-.70):
            box('Station bench legs',(x,yy,.36),(.42,.12,.54),'iron')
        box('Station bench seat',(x,-1.3,.66),(.53,2.10,.14),'wood',False)
        box('Station bench back',(x+side*.17,-1.3,1.03),(.14,2.04,.61),'steel')
        box('Bench violet enamel',(x-side*.09,-1.3,1.05),(.06,1.80,.25),'violet',False)
        for row in range(3):
            box('Sandstone leaning pillar',(x,1.30+row*.035,.51+row*.58),(.47,.64,.55),'sand',
                rot=(.035,0,0))
        box('Pillar marble foot',(x,1.30,.24),(.60,.84,.25),'marble_dark')
        crack([(x-side*.25,1.1,.48),(x-side*.25,1.4,.83),(x-side*.25,1.2,1.36)])
        start=len(PARTS)
        box('Clock wall panel',(0,-.075,.92),(2.20,.18,1.73),'violet_dark')
        gear((0,.09,1.15),.90,'brass')
        crack([(-.90,.15,.40),(-.55,.15,.72),(-.68,.15,.98)])
        transform_since(start,Matrix.Translation(Vector((x,3.90,.12))) @
                        Matrix.Rotation(side*math.pi/2,4,'Z'))


def corridor_islands():
    # No continuous foundation: these are actual separate bridge slabs, with
    # 0.15 m daylight gaps and exactly coplanar Z=.02 walking surfaces.
    gap=.15; count=8; pitch=10.4/count; length=pitch-gap
    for row in range(count):
        y=-5.2+pitch*(row+.5)
        lo=y-length/2; hi=y+length/2
        if row==0: lo=-5.2
        if row==count-1: hi=5.2
        box('Floating stone bridge slab',(0,(lo+hi)/2,-.09),(4.6,hi-lo,.22),
            'void_stone' if row%2 else 'marble_dark')
        for side in (-1,1):
            box('Flush marble bridge inset',(side*1.18,(lo+hi)/2,.001),(2.12,hi-lo-.12,.038),
                'marble' if row%3 else 'violet',False)
        # Nothing lies above the common walking plane; cracks are inlaid flush.
        crack([(-2.13,y-.25,-.004),(-.8,y+.10,-.004),(0,y-.04,-.004),
               (.85,y+.18,-.004),(2.13,y-.10,-.004)])
        # Faceted rock masses taper below, never obstructing the bridge top.
        for side in (-1,1):
            jewel((side*1.60,y,-.42),1.80,.78,'void_stone_dark')
            jewel((side*.62,y+.18,-.58),.82,.70,'violet_dark')
            jewel((side*2.36,y-.13,-.45),.70,.72,'void_stone_dark')
    for side in (-1,1):
        x=side*2.60
        box('Void visible beneath bridge edge',(x,0,-.85),(.60,10.4,.06),'night_sky',False)
        for j in range(14):
            y=-4.95+j*.76
            star(x+random.uniform(-.17,.17),y,-.80,.07 if j%3 else .12,'starlight',True)
        for j,y in enumerate((-4.75,-2.38,0,2.38,4.75)):
            box('Broken railing plinth',(x,y,.10),(.60,.58,.20),'marble_dark')
            height=.58 if j%2 else .84
            # Angular truncated posts are deliberately below 1.1 m.
            prism('Broken marble railing post',[(x-.18,.20),(x+.18,.20),
                  (x+.18,height-.08),(x+.03,height+.06),(x-.18,height)],
                  y-.18,y+.18,'marble')
            box('Post gold collar',(x,y,.34),(.41,.43,.08),'gold',False)
        for y in (-3.55,-1.18,1.18,3.55):
            box('Low fractured parapet',(x,y,.19),(.46,.95,.38),'void_stone')
            crack([(x-side*.24,y-.39,.28),(x-side*.24,y-.10,.18),
                   (x-side*.24,y+.14,.32)])


def corridor_clock():
    # The floor is genuinely recessed along the gear track: a continuous top
    # slab would cover the gold gears even when they are modeled correctly.
    for side in (-1,1):
        box('Clock floor side foundation',(side*1.40,0,-.09),(1.80,10.4,.22),'violet_dark',False)
    box('Deep gear channel foundation',(0,0,-.145),(1.0,10.4,.11),'gold_dark',False)
    for row in range(8):
        y=-4.55+row*1.30
        for side in (-1,1):
            box('Indigo clock heart floor plate',(side*1.40,y,.035),(1.80,1.24,.03),'indigo',False)
            for dx in (-.68,.68):
                cylinder('Floor gold rivet',(side*1.40+dx,y+.42,.06),.055,.04,'brass',5,bevel=False)
        box('Sunken gear channel',(0,y,-.12),(.88,1.30,.06),'gold_dark',False)
        start=len(PARTS)
        gear((0,0,0),.39,'gold',horizontal=True,low=True)
        transform_since(start,Matrix.Translation(Vector((0,y,-.05))) @ Matrix.Diagonal((1,1,.45,1)))
        floor_crack(y+.48)
    for x in (-.47,.47):
        box('Gear channel gold lip',(x,0,.04),(.07,10.4,.04),'gold',False)
    for side in (-1,1):
        x=side*2.60
        box('Clock heart wall socle',(x,0,.14),(.60,10.4,.28),'gold_dark')
        for j,y in enumerate((-4.2,-2.1,0,2.1,4.2)):
            start=len(PARTS)
            box('Fractured violet wall panel',(0,-.09,1.02),(1.92,.18,1.72),'violet_dark',False)
            box('Panel gold upper molding',(-.25,-.075,1.92),(1.45,.20,.09),'gold',False)
            gear((0,.09,1.14),.89,('gold','brass','gold_dark')[j%3])
            crack([(-.86,.16,.41),(-.54,.16,.70),(-.65,.16,1.05)])
            crack([(.20,.16,1.85),(.43,.16,1.63),(.32,.16,1.45)])
            # A broken tooth hangs away from the wheel, within the wall band.
            box('Detached clock tooth',(.82,.04,2.03),(.24,.15,.20),'gold',False,rot=(0,.28,0))
            transform_since(start,Matrix.Translation(Vector((x,y,.08))) @
                            Matrix.Rotation(side*math.pi/2,4,'Z'))


def portal_base(side, role='marble_dark'):
    x=side*2.82
    box('Portal monumental foot',(x,0,.13),(.96,1.10,.26),role)
    box('Portal stepped foot molding',(x,0,.31),(.84,.95,.10),'gold',False)
    return x


def gilt_arch(label, center, rx, rz, inner, a, b, depth, steps):
    """A gold frame has a dark outer casing and bright inner molding."""
    obj=arc_band(label,center,rx,rz,inner,a,b,depth,'gold',steps)
    for role in ('gold_dark','violet_dark','crack_gold'):
        obj.data.materials.append(MATS[role])
    # arc_band emits back, front, outside, inside quads per angular step.
    for face in obj.data.polygons:
        face.material_index=(2,0,1,3)[face.index%4] if face.index<steps*4 else 1
    return obj


def portal_mirror():
    for side in (-1,1):
        x=portal_base(side)
        box('Mirror portal marble jamb',(x,0,1.64),(.72,.78,2.62),'marble')
        box('Gilded jamb front panel',(x,.42,1.66),(.58,.10,2.56),'gold')
        box('Jamb deep violet recess',(x,.48,1.64),(.33,.04,2.23),'violet_dark',False)
        for z in (.70,1.43,2.18):
            scroll(x-side*.06,z,-side,.66,.50)
            star(x,.52,z+.18,.10,'starlight')
        for z in (.49,2.91):
            box('Baroque capital',(x,0,z),(.90,.95,.14),'gold')
        crack([(x-.20,.54,2.51),(x+.06,.54,2.68),(x-.04,.54,2.89)])
    for j in range(5):
        a=j*math.pi/5+.012; b=(j+1)*math.pi/5-.012
        gilt_arch('Cracked mirror arch gold casing',(0,0,2.97),3.11,.91,.79,a,b,.78,5)
        arc_band('Arch dark recessed channel',(0,.42,2.97),3.11*.95,.91*.95,.90,
                 a,b,.045,'gold_dark',5)
        arc_band('Arch bright inside molding',(0,.45,2.97),3.11*.81,.91*.81,.975,
                 a,b,.05,'crack_gold',5)
        t=(a+b)/2
        star(2.82*math.cos(t),.48,2.97+.82*math.sin(t),.12,'marble')
    jewel((0,.02,4.015),.30,.33,'starlight')
    for side in (-1,1): scroll(side*.29,3.91,side,.72,.43)


def portal_memories():
    for side in (-1,1):
        x=portal_base(side,'void_stone_dark')
        if side<0:
            box('Half saloon timber door jamb',(x,0,1.67),(.75,.86,2.68),'wood_dark')
            box('Saloon carved front plank',(x,.45,1.68),(.46,.06,2.42),'wood',False)
            for z in (.63,1.70,2.80):
                box('Old saloon iron bracket',(x,0,z),(.82,.94,.15),'iron',False)
                rivet(x,.50,z,'gold',.075)
            beam('Crooked saloon lintel',(-3.12,0,3.10),(.18,0,3.75),.32,.84,'wood_dark',True)
            beam('Lintel carved wood inset',(-2.94,.45,3.16),(.12,.45,3.76),.10,.06,'wood')
        else:
            for dx in (-.27,.27):
                box('Station iron arch flange',(x+dx,0,1.69),(.16,.86,2.72),'iron')
            box('Station iron arch web',(x,0,1.69),(.52,.36,2.72),'iron_dark',False)
            for z in (.62,1.66,2.77):
                box('Station riveted connection',(x,.46,z),(.74,.08,.21),'steel',False)
                for dx in (-.22,.22): rivet(x+dx,.52,z,'steel',.06)
            arc_band('Half station iron arch',(0,0,2.99),3.18,.95,.85,0,math.pi/2+.04,.83,'iron',12)
            for j in range(6):
                t=(j+.5)*math.pi/12
                rivet(2.96*math.cos(t),.45,2.99+.87*math.sin(t),'steel',.07)
    # The central weld is thick, readable, and wholly above doorway clearance.
    prism('Void stone welded keystone',[(-.56,3.50),(.31,3.55),(.63,3.89),(.06,4.08),(-.42,3.94)],
          -.47,.47,'void_stone',True,'void_stone_dark')
    crack([(-.42,.51,3.71),(-.11,.51,3.80),(-.20,.51,3.95),(.12,.51,4.00)])
    star(.19,.52,3.75,.15,'gold')


def portal_islands():
    for side in (-1,1):
        x=portal_base(side,'void_stone_dark')
        profile=[(side*2.43,.36),(side*3.21,.36),(side*3.30,2.72),
                 (side*2.78,2.98),(side*2.35,2.87)]
        prism('Leaning void stone monolith',profile,-.43,.43,'void_stone',True,'void_stone_dark')
        prism('Monolith inset dark facet',[(side*2.49,.60),(side*3.01,.61),
              (side*3.13,2.63),(side*2.61,2.72)],.435,.47,'violet_dark',False)
        crack([(x,.50,.58),(x-.13,.50,1.14),(x+.10,.50,1.49),(x-.03,.50,2.15)])
        for z in (.86,1.75,2.56): star(x,.52,z,.15,'gold')
        jewel((x,.49,2.45),.20,.31,'crystal')
    # Highest monolith point 2.98, hovering lintel underside 3.28: 0.30 m air.
    prism('Hovering monolith lintel',[(-3.20,3.28),(3.19,3.28),(3.28,3.73),
          (1.66,3.91),(-.64,3.86),(-3.31,3.71)],-.46,.46,'void_stone',True,'void_stone_dark')
    prism('Lintel gold rune inset',[(-2.78,3.39),(2.78,3.39),(2.74,3.68),(-2.77,3.68)],
          .463,.49,'violet_dark',False)
    for x in (-2.45,-1.63,-.81,0,.81,1.63,2.45): star(x,.52,3.54,.15,'gold')
    crack([(-1.90,.51,3.83),(-1.60,.51,3.68),(-1.73,.51,3.48)])


def portal_clock():
    for side in (-1,1):
        x=portal_base(side,'violet_dark')
        box('Violet clock heart pillar',(x,0,1.65),(.79,.82,2.60),'violet_dark')
        box('Pillar marble inset',(x,.44,1.65),(.38,.05,2.20),'marble_dark',False)
        for dx in (-.285,.285):
            box('Pillar gilt fluting',(x+dx,.44,1.65),(.065,.07,2.60),'gold',False)
        for z in (.52,2.82):
            box('Clock portal capital',(x,0,z),(.90,.94,.16),'gold')
        crack([(x-.11,.50,.80),(x+.13,.50,1.12),(x-.03,.50,1.42)])
    for j in range(8):
        a=j*math.pi/8+.010; b=(j+1)*math.pi/8-.010
        dz=.14 if j==3 else 0
        dx=.08 if j==3 else 0
        gilt_arch('Cracked clock bezel sector',(dx,0,2.97+dz),3.10,1.0,.80,a,b,.77,5)
        for r,role,y in ((.96,'gold_dark',.42),(.82,'crack_gold',.45)):
            arc_band('Clock bezel concentric molding',(dx,y,2.97+dz),3.10*r,r,.97,
                     a,b,.05,role,5)
        for k in range(2):
            t=a+(b-a)*(k+.5)/2
            x,z=dx+2.79*math.cos(t),2.97+dz+.90*math.sin(t)
            box('Clock arch numeral tick',(x,.47,z),(.085,.055,.21),'dial_mark',False,
                rot=(0,math.pi/2-t,0))
    jewel((-.42,.04,4.04),.21,.22,'crystal')


def reward_door():
    start=len(PARTS)
    for side in (-1,1):
        x=side*1.34
        box('Celestial marble foot',(x,0,.10),(.32,.50,.20),'marble_dark')
        box('Celestial marble jamb',(x,0,1.24),(.32,.44,2.28),'marble')
        box('Jamb violet recessed stripe',(x,.235,1.28),(.18,.04,2.12),'violet_dark',False)
        for z in (.44,1.18,1.94):
            scroll(x,z,-side,.43,.25)
            star(x,.27,z+.18,.07,'starlight')
        for dx in (-.115,.115):
            box('Jamb gold edge',(x+dx,.23,1.25),(.045,.04,2.30),'gold',False)
    box('Celestial marble cornice',(0,0,2.38),(3.0,.50,.18),'marble')
    box('Celestial gold crown rail',(0,.245,2.38),(3.0,.04,.07),'gold',False)
    box('Celestial marble threshold',(0,0,.055),(2.38,.48,.11),'marble_dark')
    box('Threshold star gold inlay',(0,.15,.116),(2.20,.10,.04),'gold',False)
    # Clock floats 0.10 m above the cornice; maximum Z is exactly 3 m.
    cylinder('Hovering clock gold bezel',(0,0,2.785),.215,.34,'gold',20,
             rot=(math.pi/2,0,0))
    cylinder('Hovering ivory dial',(0,.19,2.785),.18,.04,'dial_face',20,
             rot=(math.pi/2,0,0),bevel=False)
    for j in range(8):
        t=j*math.tau/8
        box('Crown clock numeral',(.145*math.sin(t),.225,2.785+.145*math.cos(t)),
            (.04,.04,.06),'dial_mark',False,rot=(0,t,0))
    beam('Crown clock hand',(0,.24,2.785),(.095,.24,2.86),.04,.04,'dial_mark')
    crack([(-.14,.25,2.83),(-.05,.25,2.80),(-.01,.25,2.72),(.13,.25,2.71)],radius=.021)
    for side in (-1,1):
        scroll(side*.49,2.51,side,.78,.22)
        star(side*.86,.25,2.55,.10,'starlight')
    result=[join(PARTS[start:],'Frame')]
    for side,name in ((-1,'Leaf_L'),(1,'Leaf_R')):
        start=len(PARTS)
        x=side*.565
        box('Violet celestial leaf',(x,0,1.27),(1.10,.16,2.12),'violet')
        box('Recessed leaf star map',(x,.10,1.26),(.91,.045,1.93),'violet_dark')
        for dx in (-.49,.49):
            box('Leaf golden stile',(x+dx,.14,1.27),(.07,.06,2.12),'gold',False)
        for z in (.25,2.29):
            box('Leaf golden cross rail',(x,.14,z),(1.07,.06,.07),'gold',False)
        prism('Mirror diamond window',[(x,1.24),(x+.30,1.64),(x,2.04),(x-.30,1.64)],
              .125,.16,'mirror',False,'mirror_back')
        tube('Diamond gold leading',[(x,.18,1.24),(x+.30,.18,1.64),(x,.18,2.04),
             (x-.30,.18,1.64),(x,.18,1.24)],.024,'gold',4)
        nodes=[(x-.29,.55),(x+.17,.73),(x-.10,1.00),(x+.30,1.16)]
        crack([(xx,.158,zz) for xx,zz in nodes],'gold',.022)
        for xx,zz in nodes: star(xx,.18,zz,.075,'starlight')
        for xx,zz in ((x-.33,2.14),(x+.33,2.10),(x-.35,1.17)):
            star(xx,.18,zz,.055,'starlight')
        cylinder('Golden leaf handle',(side*.15,.20,1.14),.065,.10,'gold',8,
                 rot=(math.pi/2,0,0),bevel=False)
        for z in (.58,1.95):
            cylinder('Gold celestial hinge',(side*1.115,0,z),.07,.23,'gold',8)
        obj=join(PARTS[start:],name,(side*1.115,0,0))
        obj['hinge_axis']='local Z'
        result.append(obj)
    return result


def preview():
    """Review-only transforms/palette; export hashes and gray state are restored."""
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light,shade.color_type='FLAT','MATERIAL'
    shade.show_shadows,shade.show_specular_highlight=False,False
    shade.show_cavity,shade.cavity_type=True,'BOTH'
    shade.curvature_ridge_factor,shade.curvature_valley_factor=1.1,.85
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review cosmos')
    scene.world.color=(.016,.012,.035)
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera=camera
    camera.data.type='ORTHO'; camera.data.ortho_scale=42
    angle=math.radians(55)
    camera.location=(0,60*math.cos(angle),60*math.sin(angle))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    right=Vector((-1,0,0)); up=Vector((0,-math.sin(angle),math.cos(angle)))
    helpers=[camera]
    matrices={obj:obj.matrix_world.copy() for _,objects,_,_ in ASSETS for obj in objects}

    def caption(text,x,y,size=.28,role='marble',align='CENTER'):
        data=bpy.data.curves.new('Review type','FONT')
        data.body,data.align_x,data.size=text,align,size
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('Review caption',data)
        scene.collection.objects.link(obj)
        obj.location=right*x+up*y
        obj.rotation_euler=camera.rotation_euler
        helpers.append(obj)

    try:
        for role,mat in MATS.items():
            rgb=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in PALETTE[role]]
            mat.diffuse_color=(*rgb,1)
        for name,objects,_,_ in ASSETS:
            if name=='RewardDoor':
                x,y,scale=17.1,-.5,1.65
                rot=Matrix.Identity(4)
            else:
                stage=int(name[-1]); y=7.0-(stage-1)*5.05
                if name.startswith('Gate'): x,scale=-14.3,1.25
                elif name.startswith('Corridor'): x,scale=-3.5,.72
                else: x,scale=7.7,.98
                rot=Matrix.Rotation(math.pi/2 if name.startswith('Corridor') else 0,4,'Z')
            matrix=rot @ Matrix.Diagonal((scale,scale,scale,1))
            verts=[matrix @ p for p in world_vertices(objects)]
            cx=(min(v.dot(right) for v in verts)+max(v.dot(right) for v in verts))/2
            cy=(min(v.dot(up) for v in verts)+max(v.dot(up) for v in verts))/2
            shift=right*(x-cx)+up*(y-cy)
            matrix=Matrix.Translation(shift) @ matrix
            for obj in objects:
                obj.matrix_world=matrix @ matrices[obj]
                if 'Splinter_' in obj.name: obj.hide_render=True
        caption('CHAPTER 05   /   THE COLLAPSING END OF TIME',-19.6,10.65,.52,'marble','LEFT')
        caption('VIOLET COSMOS   /   GOLD FAULTS   /   SHATTERED MEMORIES',-19.6,9.90,.24,'gold','LEFT')
        for x,title in ((-14.3,'PRE-FRACTURED GATE'),(-3.5,'10.4 m CORRIDOR'),(7.7,'CLEAR PORTAL')):
            caption(title,x,9.35,.25,'gold')
        titles=('SHATTERED MIRROR THRESHOLD','JUMBLED MEMORY CORRIDOR',
                'DRIFTING DEBRIS ISLANDS','COLLAPSING CLOCK HEART')
        for stage,title in enumerate(titles,1):
            y=7.0-(stage-1)*5.05
            caption(f'0{stage}  /  {title}',-19.6,y-2.34,.26,'gold','LEFT')
        caption('CELESTIAL',17.1,3.15,.32,'gold')
        caption('REWARD DOOR',17.1,2.55,.30,'marble')
        caption('HINGED LEAVES',17.1,-3.80,.24,'gold')
        caption('13 ASSETS  /  +Y FRONT  /  GRAY FBX MATERIALS',17.1,-4.35,.16,'marble')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj,matrix in matrices.items():
            obj.matrix_world=matrix
            obj.hide_render=False
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)
    for _,_,path,digest in ASSETS:
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest,'Preview changed export'


def main():
    global PARTS
    setup()
    gates=(gate_mirror,gate_memories,gate_islands,gate_clock)
    corridors=(corridor_mirror,corridor_memories,corridor_islands,corridor_clock)
    portals=(portal_mirror,portal_memories,portal_islands,portal_clock)
    for stage in range(1,5):
        random.seed(550071+stage*7919)
        PARTS=[]
        pieces=gates[stage-1]()
        low,high=bounds(pieces)
        for obj in pieces: obj.location.y-=(low.y+high.y)/2
        export(f'Gate_S{stage}',pieces+splinters(stage))
        PARTS=[]
        corridors[stage-1]()
        export(f'Corridor_S{stage}',[join(PARTS,f'Corridor_S{stage}')])
        PARTS=[]
        portals[stage-1]()
        export(f'Portal_S{stage}',[join(PARTS,f'Portal_S{stage}')])
    PARTS=[]
    random.seed(55971)
    export('RewardDoor',reward_door())
    assert len(ASSETS)==13
    assert set(bpy.context.scene.objects)=={obj for _,objects,_,_ in ASSETS for obj in objects}
    preview()
    print('DONE',flush=True)


if __name__=='__main__':
    main()


