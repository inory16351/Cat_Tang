# 시계탑의 파괴형 문, 통로, 포털과 보상 문을 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_gates.py
"""Chapter 4 clockwork kit; deterministic geometry, gray FBX, colored review only."""
import hashlib
import math
import random
from pathlib import Path
import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_gates_preview.png'
PALETTE = {
'steel': (.49,.65,.77), 'steel_dark': (.21,.29,.39),
'iron': (.30,.36,.44), 'iron_dark': (.10,.15,.23),
'silver': (.78,.87,.94), 'brass': (.78,.51,.20),
'brass_dark': (.36,.24,.12), 'gold': (1,.74,.29),
'copper': (.74,.36,.21), 'navy_paint': (.12,.23,.42),
'navy_dark': (.055,.095,.19), 'red_lacquer': (.67,.065,.10),
'red_dark': (.29,.04,.065), 'gem_red': (.95,.12,.16),
'wood': (.38,.28,.23), 'wood_dark': (.16,.12,.15),
'stone': (.44,.49,.60), 'stone_dark': (.22,.27,.37),
'dial_face': (.92,.89,.73), 'dial_mark': (.08,.115,.18),
'glass': (.38,.69,.79), 'starlight': (.84,.91,1),
'night_sky': (.025,.055,.14), 'porcelain': (.88,.90,.94),
'ribbon_red': (.75,.09,.14), 'velvet': (.25,.06,.14),
'lamp_warm': (1,.67,.28),
}
HARD = set(PALETTE) - {'glass','night_sky','starlight','velvet','ribbon_red'}
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
        if role == 'glass':
            shader.inputs['Roughness'].default_value = .24
            shader.inputs['Transmission Weight'].default_value = .65
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
        if name in ('Corridor_S3', 'Corridor_S4'):
            assert high.z <= 1.1001, (name, high)
        # The first component is the exact 4.6 x 10.4 foundation in every stage.
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
        obj['chapter'], obj['front'] = 'Inside the Clockwork', '+Y'
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


def preview():
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.show_shadows, shade.show_specular_highlight = False, False
    shade.show_cavity, shade.cavity_type = True, 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.05, .85
    shade.background_type = 'WORLD'
    shade.background_color = (.012, .020, .04)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.012, .020, .04)
    camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 52
    target = Vector((-.3, 0, 1.2))
    elevation = math.radians(55)
    camera.location = target + Vector((0, 60 * math.cos(elevation), 60 * math.sin(elevation)))
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    helpers = [camera]
    matrices = {obj: obj.matrix_world.copy() for _, objects, _, _ in ASSETS for obj in objects}

    def caption(body, loc, size=.34, align='CENTER', role='silver'):
        data = bpy.data.curves.new('Review typography', 'FONT')
        data.body, data.align_x, data.size = body, align, size
        data.materials.append(MATS[role])
        obj = bpy.data.objects.new('Review caption', data)
        scene.collection.objects.link(obj)
        obj.location, obj.rotation_euler = (-loc[0], loc[1], loc[2]), camera.rotation_euler
        helpers.append(obj)

    try:
        for role, mat in MATS.items():
            # Workbench stores linear RGB; these swatches are authored in sRGB.
            rgb = [v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4
                   for v in PALETTE[role]]
            mat.diffuse_color = (*rgb, 1)
        for name, objects, _, _ in ASSETS:
            if name == 'RewardDoor':
                transform = Matrix.Translation(Vector((-18.3, 0, 0)))
            else:
                stage = int(name[-1])
                y = (stage - 2.5) * 8
                x = -13 if name.startswith('Gate') else (-2 if name.startswith('Corridor') else 9)
                # Review-only quarter-turn gives the long corridors room across each cell.
                rot = Matrix.Rotation(math.pi / 2 if name.startswith('Corridor') else 0, 4, 'Z')
                transform = Matrix.Translation(Vector((-x, y, 0))) @ rot
                caption(name, (x, y + 3.65, -.08), .33)
            for obj in objects:
                obj.matrix_world = transform @ matrices[obj]
        for stage, title in enumerate(('SPRING STOREHOUSE', 'GEAR ENGINE ROOM', 'BELL TOWER', 'BEHIND THE DIAL'), 1):
            caption(f'0{stage} / {title}', (-17.7, (stage - 2.5) * 8 - 2.5, 0), .32, 'LEFT', 'gold')
        caption('CHAPTER 04 / INSIDE THE CLOCKWORK', (-17.7, -17.0, 0), .55, 'LEFT')
        caption('NIGHT NAVY / SILVER / BRASS  -  STAGE CONNECTION KIT', (-17.7, -15.8, 0), .29, 'LEFT', 'gold')
        caption('REWARD', (18.3, -3.2, 0), .35, role='gold')
        caption('CLOCK-CASE DOOR', (18.3, 2.4, -.1), .31)
        caption('HINGED LEAVES', (18.3, 3.0, -.1), .24, role='gold')
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        scene.camera = None
        for obj, matrix in matrices.items():
            obj.matrix_world = matrix
        for obj in helpers:
            bpy.data.objects.remove(obj, do_unlink=True)
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)
    for _, _, path, digest in ASSETS:
        assert hashlib.sha256(path.read_bytes()).hexdigest() == digest, 'Preview changed export'

def rivet(x, y, z, role='silver', radius=.045):
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


def gear(center, radius, teeth=12, role='brass', spokes=6, horizontal=False, axle=True):
    start = len(PARTS)
    arc_band('Toothed gear rim', (0,0,0), radius, radius, .69, 0,
             math.tau, .10, role, teeth*4, teeth)
    cylinder('Gear dark hub', (0,0,0), radius*.25, .14, 'brass_dark',
             8, rot=(math.pi/2,0,0), bevel=False)
    if axle:
        cylinder('Gear silver axle', (0,.09,0), radius*.11, .08, 'silver',
                 6, rot=(math.pi/2,0,0), bevel=False)
    for j in range(spokes):
        t=j*math.tau/spokes
        beam('Gear broad spoke', (radius*.19*math.cos(t),0,radius*.19*math.sin(t)),
             (radius*.76*math.cos(t),0,radius*.76*math.sin(t)),
             max(.06,radius*.15), .08, role)
    transform = Matrix.Translation(Vector(center))
    if horizontal:
        transform @= Matrix.Rotation(math.pi/2,4,'X')
    for obj in PARTS[start:]:
        obj.data.transform(transform)


def spring(center, radius=.37, turns=2.3, wire=.045):
    cx,cy,cz=center
    points=[]
    for j in range(49):
        t=j/48
        r=radius*(.22+.78*t)
        angle=turns*math.tau*t
        points.append((cx+r*math.cos(angle),cy,cz+r*math.sin(angle)))
    tube('Coiled steel mainspring', points, wire, 'steel', 5)
    cylinder('Spring brass spindle', (cx,cy,cz), .075, .08, 'brass',
             n=8, rot=(math.pi/2,0,0), bevel=False)


def bell(center, scale=1):
    """Hollow turned bell with flared lip, dark inner mouth and clapper."""
    x,y,z=center
    profile=[(.08,.44),(.13,.39),(.16,.26),(.21,.10),(.29,.045),
             (.29,0),(.23,0),(.16,.10),(.105,.28),(.075,.34)]
    n=12
    verts=[(x+r*scale*math.cos(j*math.tau/n),y+r*scale*math.sin(j*math.tau/n),
            z+h*scale) for r,h in profile for j in range(n)]
    faces=[(i*n+j,i*n+(j+1)%n,((i+1)%len(profile))*n+(j+1)%n,
            ((i+1)%len(profile))*n+j) for i in range(len(profile)) for j in range(n)]
    obj=mesh('Flared hollow copper bell',verts,faces,'copper',False)
    obj.data.materials.append(MATS['brass_dark'])
    obj.data.materials.append(MATS['brass'])
    for face in obj.data.polygons:
        if face.index//n >= 6:
            face.material_index=1
        elif face.index//n in (4,5):
            face.material_index=2
    cylinder('Bell clapper',(x,y,z-.01*scale),.055*scale,.12*scale,
             'iron',n=8,bevel=False)
    cylinder('Bell suspension',(x,y,z+.49*scale),.04*scale,.14*scale,
             'iron',n=6,bevel=False)


def gate_crates():
    pieces=[]
    # Six shallow crates. Their three planks share jagged cuts exactly.
    for row in range(2):
        for col in range(3):
            x0,x1=-2.3+col*4.6/3,-2.3+(col+1)*4.6/3
            z0=row*1.3
            cuts=[[(x0,z0),(x1,z0)]]
            for z in (z0+.43,z0+.87):
                cuts.append(fracture((x0,z),(x1,z),.045,3))
            cuts.append([(x0,z0+1.3),(x1,z0+1.3)])
            for k in range(3):
                start=len(PARTS)
                obj=prism('Splintered crate plank',cuts[k]+list(reversed(cuts[k+1])),
                          -.16,.055,'wood',True,'wood_dark')
                restore_interfaces(obj,cuts[k:k+2],-.16,.055)
                mid=z0+(.215,.65,1.085)[k]
                for x in (x0+.12,x1-.12):
                    box('Crate iron strap',(x,.092,mid),(.12,.075,.42),'iron',False)
                    rivet(x,.143,mid,'brass',.04)
                if k in (0,2):
                    box('Crate end molding',((x0+x1)/2,.08,z0+(.055 if k==0 else 1.245)),
                        (x1-x0,.05,.11),'wood_dark',False)
                gate_piece(start,pieces)
    for x in (-1.53,0,1.53):
        start=len(PARTS)
        # Flattened coils fit the physics slab, but remain separate flying parts.
        spring((x,.10,1.95),.43,2.1,.042)
        box('Spring keeper',(x,.095,1.95),(.10,.12,.40),'iron_dark',False)
        rivet(x,.157,1.95,'silver',.047)
        gate_piece(start,pieces)
    return pieces


def gate_shutter():
    pieces=[]
    for row in range(6):
        for side in (-1,1):
            start=len(PARTS)
            z=(row+.5)*2.6/6
            box('Iron shutter slat',(side*1.15,-.03,z),(2.3,.22,2.6/6),'navy_paint')
            box('Shutter folded lip',(side*1.15,.095,z-.16),(2.3,.06,.08),'steel_dark',False)
            box('Shutter silver rail',(side*1.15,.13,z+.15),(2.3,.035,.055),'steel',False)
            for x in (side*.20,side*2.13):
                rivet(x,.155,z,'silver')
            if row in (0,5):
                box('Red shutter safety tab',(side*2.08,.15,z),(.17,.04,.18),'red_lacquer',False)
            gate_piece(start,pieces)
    for j in range(8):
        start=len(PARTS)
        a,b=j*math.tau/8,(j+1)*math.tau/8
        arc_band('Fractured gear lock',(0,.115,1.3),.85,.85,.65,a,b,.09,
                 'brass',12,3)
        t=(a+b)/2
        beam('Broken gear lock spoke',(.15*math.cos(t),.115,1.3+.15*math.sin(t)),
             (.63*math.cos(t),.115,1.3+.63*math.sin(t)),.14,.08,'brass_dark')
        rivet(.72*math.cos(t),.173,1.3+.72*math.sin(t),'gold',.045)
        if j==0:
            cylinder('Lock central boss',(0,.12,1.3),.20,.09,'brass',12,
                     rot=(math.pi/2,0,0),bevel=False)
            box('Lock red keyway',(0,.174,1.3),(.07,.025,.23),'red_lacquer',False)
        gate_piece(start,pieces)
    return pieces


def gate_bells():
    pieces=[]
    for col in range(9):
        x=-2.16+col*.54
        # Complementary bent breaks: shared kink rather than separated cuts.
        cut=[(x-.07,1.27),(x,1.34),(x+.07,1.30)]
        for row in range(2):
            start=len(PARTS)
            path=([(x-.07,0),(x+.07,0)]+list(reversed(cut)) if row==0 else
                  cut+[(x+.07,2.6),(x-.07,2.6)])
            obj=prism('Bent copper portcullis bar',path,-.10,.10,'copper',True,'brass_dark')
            restore_interfaces(obj,[cut],-.10,.10)
            z=.51 if row==0 else 2.12
            box('Bar broad collar',(x,.11,z),(.21,.07,.14),'brass',False)
            rivet(x,.16,z,'steel',.04)
            gate_piece(start,pieces)
    for z in (.10,1.3,2.50):
        start=len(PARTS)
        box('Portcullis cross rail',(0,0,z),(4.6,.24,.20),'copper')
        box('Rail silver inlay',(0,.13,z),(4.6,.04,.05),'steel',False)
        for x in (-2.16,-1.08,0,1.08,2.16):
            rivet(x,.165,z,'brass',.05)
        gate_piece(start,pieces)
    start=len(PARTS)
    bell((0,0,1.93),.82)
    # Compress depth to fit the gate, keeping the bell's flared silhouette.
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Diagonal((1,.52,1,1)))
    gate_piece(start,pieces)
    return pieces


def gate_glass():
    pieces=[]
    count=12
    boundary=[(2.3*.89*math.cos(j*math.tau/count),
               1.3+1.3*.89*math.sin(j*math.tau/count)) for j in range(count)]
    center=(0,1.3)
    for j in range(count):
        start=len(PARTS)
        a,b=boundary[j],boundary[(j+1)%count]
        # Radial facets share their exact vertices: intact until physics bursts.
        obj=prism('Sharp clock glass shard',[center,a,b],-.065,.065,'glass',False,'steel_dark')
        obj.data.materials.append(MATS['silver'])
        obj.data.polygons[2].material_index=2
        if j in (1,3,7,10):
            ax,az=a; bx,bz=b
            tube('Broad pane glint',[(ax*.78,.075,1.3+(az-1.3)*.78),
                                     (bx*.72,.075,1.3+(bz-1.3)*.72)],.022,'silver',4)
        if j in (2,5):
            t=(j+.4)*math.tau/count
            length=.93 if j==2 else 1.30
            beam('Clock broken hand',(0,.103,1.3),
                 (length*math.cos(t),.103,1.3+length*.62*math.sin(t)),.07,.05,'dial_mark')
        if j==0:
            cylinder('Clock red pivot',(0,.113,1.3),.10,.06,'red_lacquer',12,
                     rot=(math.pi/2,0,0),bevel=False)
        gate_piece(start,pieces)
    for j in range(count):
        start=len(PARTS)
        a,b=j*math.tau/count,(j+1)*math.tau/count
        # Polygonal ring inner edge matches the glass dodecagon exactly.
        arc_band('Clock steel fracture sector',(0,0,1.3),2.3,1.3,.89,
                 a,b,.28,'steel',1)
        t=(a+b)/2
        x,z=2.3*.945*math.cos(t),1.3+1.3*.945*math.sin(t)
        box('Raised dial numeral',(x,.155,z),(.065,.04,.145),'dial_mark',False,
            rot=(0,-t+math.pi/2,0))
        rivet(2.3*.96*math.cos(a+.09),.155,1.3+1.3*.96*math.sin(a+.09),'brass',.035)
        gate_piece(start,pieces)
    return pieces


def splinters(stage):
    result=[]
    for j in range(3):
        length=.26+j*.04
        profile=[(-.035,1.3-length/2),(.035,1.3-.05),
                 (.035,1.3+length/2),(-.035,1.3+.05)]
        obj=prism('Hidden debris template',profile,-.025,.025,
                  {1:'wood',2:'navy_paint',3:'copper',4:'glass'}[stage],False)
        obj=join([obj],f'Splinter_{j}')
        activate(obj)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
        result.append(obj)
    return result


def floor(role):
    box('Corridor foundation',(0,0,-.09),(4.6,10.4,.22),role)


def crate(center, size=.55):
    x,y,z=center
    box('Shelf crate dark carcass',(x,y,z),(size,size,size),'wood_dark',False)
    for j in range(3):
        box('Shelf crate plank',(x,y+size*.51,z+(j-1)*size/3),
            (size,.04,size*.29),'wood',False)
    for dx in (-size*.34,size*.34):
        box('Shelf crate strap',(x+dx,y+size*.56,z),(.06,.055,size),'iron',False)
    box('Shelf red inventory seal',(x,y+size*.58,z),(.12,.04,.12),'red_lacquer',False)


def corridor_store():
    floor('wood_dark')
    for j in range(8):
        box('Floor long plank',(-2.0125+j*.575,0,.035),(.55,10.4,.03),
            'wood' if j%3 else 'navy_paint',False)
    for y in (-4.85,-2.4,0,2.4,4.85):
        box('Floor iron cross strap',(0,y,.055),(4.6,.12,.04),'iron',False)
    for side in (-1,1):
        x=side*2.60
        for y in (-4.7,-2.35,0,2.35,4.7):
            box('Rack upright',(x,y,1.10),(.16,.18,2.2),'wood_dark')
            for z in (.32,1.20,2.05):
                box('Rack iron collar',(x,y,z),(.21,.23,.10),'iron',False)
        for z in (.34,1.23):
            box('Rack wooden shelf',(x,0,z),(.60,10.0,.12),'wood',False)
            box('Rack brass shelf edge',(side*2.31,0,z),(.05,10.0,.08),'brass',False)
        for j,y in enumerate((-3.5,-1.17,1.17,3.5)):
            # Side-facing shelf cargo, all geometry within the 60 cm edge band.
            start=len(PARTS)
            crate((0,0,0),.48)
            transform=Matrix.Translation(Vector((x,y,.64 if j%2 else 1.53))) @ Matrix.Rotation(side*math.pi/2,4,'Z')
            for obj in PARTS[start:]:
                obj.data.transform(transform)
            start=len(PARTS)
            spring((0,0,0),.30,2,.04)
            transform=Matrix.Translation(Vector((x,y,1.59 if j%2 else .72))) @ Matrix.Rotation(side*math.pi/2,4,'Z')
            for obj in PARTS[start:]:
                obj.data.transform(transform)


def corridor_engine():
    floor('steel_dark')
    # Riveted plates flank a recessed, visible gear train down the center.
    for row in range(8):
        y=-4.55+row*1.3
        for side in (-1,1):
            box('Riveted silver floor plate',(side*1.37,y,.035),(1.81,1.25,.03),'steel',False)
            for dx in (.67,):
                cylinder('Plate floor rivet',(side*1.37+dx,y+.45,.06),.045,.025,'iron',6,bevel=False)
    box('Sunken gear track',(0,0,.026),(.86,10.4,.012),'iron_dark',False)
    for x in (-.43,.43):
        box('Gear track brass rail',(x,0,.052),(.06,10.4,.05),'brass',False)
    box('Recessed gear drive shaft',(0,0,.005),(.09,10.4,.04),'brass_dark',False)
    for j in range(8):
        start=len(PARTS)
        profile=[]
        for k in range(32):
            t=k*math.tau/32
            r=.43*(1.06 if k%4 in (1,2) else 1)
            profile.append((r*math.cos(t),r*math.sin(t)))
        prism('Inset floor gear',profile,-.04,.04,'brass',False)
        cylinder('Floor gear axle',(0,.065,0),.105,.05,'steel_dark',6,
                 rot=(math.pi/2,0,0),bevel=False)
        transform=Matrix.Translation(Vector((0,-4.55+j*1.3,.005))) @ Matrix.Rotation(math.pi/2,4,'X')
        for obj in PARTS[start:]:
            obj.data.transform(transform)
    for side in (-1,1):
        x=side*2.60
        box('Engine side foundation',(x,0,.15),(.60,10.4,.30),'navy_paint')
        # Gears lie in the side-wall YZ plane, leaving the walking strip open.
        for j in range(6):
            y=-4.05+j*1.62
            start=len(PARTS)
            gear((0,0,0),.82 if j%2==0 else .76,8,spokes=4)
            transform=Matrix.Translation(Vector((x,y,1.12))) @ Matrix.Rotation(math.pi/2,4,'Z')
            for obj in PARTS[start:]:
                obj.data.transform(transform)
        for y in (-4.75,0,4.75):
            cylinder('Engine pipe riser',(x,y,1.17),.10,2.10,'iron',8,bevel=False)
            for z in (.45,1.75):
                cylinder('Red pipe coupling',(x,y,z),.15,.17,'red_lacquer',8,bevel=False)
        tube('Long iron steam pipe',[(x,-5.1,2.12),(x,5.1,2.12)],.095,'iron',6)


def stars(side):
    x=side*2.61
    box('Night sky beneath edge',(x,0,-.29),(.57,10.4,.06),'night_sky',False)
    for j in range(10):
        y=-4.85+j*1.06
        xx=x+random.uniform(-.18,.18)
        box('Visible starlight',(xx,y,-.248),(.055,.11,.024),'starlight',False)
        if j%3==0:
            box('Star cross ray',(xx,y,-.245),(.14,.045,.028),'starlight',False)


def corridor_belltower():
    floor('stone_dark')
    for row in range(10):
        for col in range(3):
            box('Stone floor slab',(-1.53+col*1.53,-4.68+row*1.04,.035),
                (1.49,1.00,.03),'stone' if (row+col)%3 else 'steel_dark',False)
    for x in (-1.72,1.72):
        box('Floor brass inlay',(x,0,.058),(.07,10.4,.025),'brass',False)
    for side in (-1,1):
        stars(side)
        x=side*2.60
        for j,y in enumerate((-4.55,-2.28,0,2.28,4.55)):
            box('Bell tower stone pier',(x,y,.40),(.60,.52,.80),'stone')
            box('Parapet cap',(x,y,.83),(.60,.64,.12),'stone_dark',False)
            if j in (0,2,4):
                box('Bell post',(x,y,.96),(.08,.10,.28),'iron',False)
                beam('Bell hanging arm',(x,y,1.055),(x,y+.43,1.055),.06,.06,'iron')
                bell((x,y+.43,.73),.55)
        for y in (-3.42,-1.14,1.14,3.42):
            # Low broken parapet sections leave large night-sky gaps.
            box('Low parapet section',(x,y,.23),(.55,1.23,.46),'stone_dark',False)
            box('Parapet brass crest',(x,y,.48),(.58,1.25,.06),'brass',False)


def corridor_catwalk():
    # Open grated floor: beams define the exact floor envelope and Z=.02.
    for x in (-2.18,0,2.18):
        box('Catwalk structural stringer',(x,0,-.09),(.24,10.4,.22),'steel')
    for j in range(27):
        y=-5.13+j*10.26/26
        box('Open grating crossbar',(0,y,-.02),(4.6,.14,.08),'iron_dark',False)
    for j in range(11):
        x=-2.2+j*.44
        box('Open grating longitudinal bar',(x,0,-.02),(.055,10.4,.08),'iron_dark',False)
    for y in (-4.9,-2.45,0,2.45,4.9):
        box('Catwalk cross girder',(0,y,-.12),(4.6,.18,.16),'steel_dark',False)
        for x in (-2.12,2.12):
            cylinder('Catwalk brass bolt',(x,y,.045),.07,.05,'brass',6,bevel=False)
    for side in (-1,1):
        stars(side)
        x=side*2.60
        for y in (-4.9,-2.45,0,2.45,4.9):
            box('Steel guardrail post',(x,y,.53),(.14,.14,1.06),'steel')
            box('Red post band',(x,y,.87),(.17,.17,.13),'red_lacquer',False)
        for z in (.33,1.02):
            box('Low steel guardrail',(x,0,z),(.13,10.4,.12),'steel',False)
        for y in (-3.675,-1.225,1.225,3.675):
            for slope in (-1,1):
                beam('Railing X brace',(x,y-1.10,.35 if slope==1 else .95),
                     (x,y+1.10,.95 if slope==1 else .35),.07,.07,'iron')


def portal_posts(role='iron'):
    for side in (-1,1):
        x=side*2.77
        box('Portal plinth',(x,0,.16),(.94,1.10,.32),'steel_dark')
        box('Portal pillar',(x,0,1.66),(.70,.76,2.70),role)
        for z in (.46,2.63):
            box('Portal red collar',(x,0,z),(.78,.86,.16),'red_lacquer',False)
        box('Pillar front inset',(x,.39,1.50),(.37,.04,1.65),'navy_paint',False)
        for z in (.64,2.38):
            rivet(x,.445,z,'brass',.07)


def portal_store():
    for side in (-1,1):
        x=side*2.77
        box('Store post foot',(x,0,.13),(.94,1.10,.26),'iron')
        box('Heavy timber post',(x,0,1.66),(.82,.90,2.90),'wood_dark')
        box('Post wood inset',(x,.465,1.60),(.46,.04,1.92),'wood',False)
        for z in (.42,2.68):
            box('Iron timber bracket',(x,0,z),(.89,1.0,.22),'iron',False)
            for dx in (-.23,.23):
                rivet(x+dx,.52,z,'silver',.055)
    box('Heavy timber lintel',(0,0,3.31),(6.38,1.0,.62),'wood_dark')
    box('Lintel iron strap',(0,.52,3.40),(6.38,.12,.12),'iron',False)
    for x in (-2.8,2.8):
        rivet(x,.585,3.40,'brass',.07)
    # The hanging sign remains entirely above the required 2.9 m clearance.
    for x in (-.23,.23):
        tube('Hanging sign chain',[(x,.53,3.96),(x,.53,3.72)],.035,'iron',5)
    start=len(PARTS)
    gear((0,0,0),.48,12,spokes=5,axle=False)
    for obj in PARTS[start:]:
        obj.data.transform(Matrix.Translation(Vector((0,.58,3.46))) @
                           Matrix.Diagonal((1,.25,1,1)))
    box('Store crown block',(0,0,3.92),(1.24,.70,.15),'navy_paint')


def portal_engine():
    portal_posts()
    # A genuine half-gear outline, flattened above a 4.6 x 2.9 clear opening.
    arc_band('Half gear arch',(0,0,2.99),2.82,.96,.83,0,math.pi,.72,
             'brass',64,16)
    arc_band('Arch silver inner molding',(0,.40,2.99),2.82*.84,.96*.84,.95,
             0,math.pi,.06,'silver',24)
    for j in range(9):
        t=(j+.5)*math.pi/9
        rivet(2.82*.93*math.cos(t),.43,2.99+.96*.93*math.sin(t),'brass_dark',.065)
    box('Gear arch red keystone',(0,.44,3.91),(.26,.10,.28),'red_lacquer')


def portal_belltower():
    for side in (-1,1):
        x=side*2.79
        box('Stone portal plinth',(x,0,.15),(.98,1.10,.30),'stone_dark')
        for j in range(4):
            box('Stone pier block',(x,0,.63+j*.67),(.84,.86,.65),'stone')
        box('Stone pier brass collar',(x,0,2.88),(.94,.94,.12),'brass',False)
        # Pointed voussoirs, all arch curvature above the clearance rectangle.
        outer=[(side*3.20,2.96),(side*1.77,3.78),(side*.15,4.16)]
        inner=[(side*2.34,2.96),(side*1.38,3.34),(0,3.81)]
        for k in range(2):
            for j in range(3):
                t0,t1=j/3,(j+1)/3
                def mix(a,b,t): return tuple(a[i]+(b[i]-a[i])*t for i in range(2))
                profile=[mix(outer[k],outer[k+1],t0),mix(outer[k],outer[k+1],t1),
                         mix(inner[k],inner[k+1],t1),mix(inner[k],inner[k+1],t0)]
                prism('Pointed dark stone voussoir',profile,-.43,.43,
                      'stone_dark' if j%2==0 else 'stone')
    prism('Brass-edged arch keystone',[(-.19,4.19),(.19,4.19),(.14,3.78),(-.14,3.78)],
          -.48,.48,'stone_dark')
    box('Keystone gold inset',(0,.505,4.01),(.11,.05,.23),'brass',False)
    bell((0,.02,3.16),1.0)


def portal_dial():
    # Open lower bezel provides a usable doorway on two structural girder legs.
    for side in (-1,1):
        x=side*2.78
        box('Girder portal foot',(x,0,.13),(.96,1.1,.26),'steel_dark')
        box('Girder web',(x,0,1.68),(.19,.68,2.90),'steel_dark',False)
        for dx in (-.31,.31):
            box('Girder flange',(x+dx,0,1.68),(.12,.82,2.90),'steel')
        for z in (.40,1.30,2.25,2.89):
            box('Girder connection plate',(x,.44,z),(.73,.08,.19),'navy_paint',False)
            for dx in (-.24,.24):
                rivet(x+dx,.50,z,'silver',.052)
    arc_band('Clock bezel brass arch',(0,0,2.99),2.89,1.10,.84,
             0,math.pi,.62,'brass',40)
    for r,y,role in ((.99,.35,'gold'),(.85,.35,'brass_dark')):
        arc_band('Bezel stacked molding',(0,y,2.99),2.89*r,1.10*r,.975,
                 0,math.pi,.07,role,32)
    for j in range(13):
        t=j*math.pi/12
        x,z=2.89*.925*math.cos(t),2.99+1.10*.925*math.sin(t)
        box('Bezel raised hour mark',(x,.39,z),(.075,.07,.17),'dial_mark',False,
            rot=(0,math.pi/2-t,0))
    for x in (-2.79,2.79):
        box('Bezel red accent',(x,.42,2.99),(.22,.09,.14),'red_lacquer',False)


def scroll(x,z,side=1,scale=1,y=.23):
    points=[(x+side*scale*a,y,z+scale*b) for a,b in
            ((0,0),(.12,.22),(.34,.26),(.46,.12),(.37,-.02),(.24,.06))]
    tube('Gold case filigree',points,.038*scale,'gold',5)


def reward_door():
    start=len(PARTS)
    for side in (-1,1):
        x=side*1.34
        box('Clock case foot',(x,0,.10),(.32,.50,.20),'wood_dark')
        box('Clock case jamb',(x,0,1.24),(.32,.44,2.30),'wood_dark')
        for dx in (-.105,.105):
            box('Case gold molding',(x+dx,.235,1.22),(.05,.03,2.26),'gold',False)
        for z in (.48,1.32,2.12):
            scroll(x,z,-side,.42,.25)
    box('Clock case cornice',(0,0,2.42),(3.0,.50,.22),'wood_dark')
    box('Case cornice gold line',(0,.255,2.44),(3.0,.03,.065),'gold',False)
    box('Clock case threshold',(0,0,.055),(2.38,.48,.11),'brass',False)
    # Small relief dial crowns the case, fitting the exact 3 m height.
    cylinder('Crown dial dark case',(0,0,2.64),.36,.34,'wood_dark',24,
             rot=(math.pi/2,0,0))
    cylinder('Crown gold dial rim',(0,.20,2.64),.32,.06,'gold',24,
             rot=(math.pi/2,0,0),bevel=False)
    cylinder('Crown ivory clock face',(0,.24,2.64),.267,.03,'dial_face',24,
             rot=(math.pi/2,0,0),bevel=False)
    for j in range(12):
        t=j*math.tau/12
        box('Dial numeral tick',(.22*math.sin(t),.265,2.64+.22*math.cos(t)),
            (.04,.025,.07),'dial_mark',False,rot=(0,t,0))
    for t,length in ((-.55,.18),(1.0,.135)):
        beam('Clock dial hand',(0,.275,2.64),(length*math.sin(t),.275,2.64+length*math.cos(t)),
             .04,.025,'dial_mark')
    for side in (-1,1):
        scroll(side*.45,2.57,side,.75,.22)
    result=[join(PARTS[start:],'Frame')]
    for side,name in ((-1,'Leaf_L'),(1,'Leaf_R')):
        start=len(PARTS)
        x=side*.565
        box('Door leaf dark frame',(x,0,1.27),(1.10,.16,2.12),'wood_dark')
        box('Door red lacquer panel',(x,.095,.72),(.91,.05,.82),'red_lacquer')
        box('Door glass window',(x,.095,1.73),(.91,.05,.97),'glass',False)
        for dx in (-.49,.49):
            box('Leaf brass stile',(x+dx,.135,1.27),(.075,.07,2.12),'brass',False)
        for z in (.25,1.18,2.29):
            box('Leaf gold cross rail',(x,.14,z),(1.07,.075,.07),'gold',False)
        # Leaded windows and curled gold relief read clearly from a high camera.
        tube('Window gold diamond',[(x,.151,1.27),(x+.33,.151,1.73),
                                    (x,.151,2.20),(x-.33,.151,1.73),(x,.151,1.27)],
             .026,'gold',4)
        scroll(x-.18,.67,1,.64,.16)
        scroll(x+.18,.67,-1,.64,.16)
        cylinder('Door brass handle',(side*.15,.19,1.14),.065,.10,'brass',8,
                 rot=(math.pi/2,0,0),bevel=False)
        for z in (.58,1.95):
            cylinder('Leaf brass barrel hinge',(side*1.115,0,z),.07,.23,'brass',8)
        obj=join(PARTS[start:],name,(side*1.115,0,0))
        obj['hinge_axis']='local Z'
        result.append(obj)
    return result


def main():
    global PARTS
    setup()
    gates=(gate_crates,gate_shutter,gate_bells,gate_glass)
    corridors=(corridor_store,corridor_engine,corridor_belltower,corridor_catwalk)
    portals=(portal_store,portal_engine,portal_belltower,portal_dial)
    for stage in range(1,5):
        random.seed(440071+stage*7919)
        PARTS=[]
        pieces=gates[stage-1]()
        low,high=bounds(pieces)
        for obj in pieces:
            obj.location.y-=(low.y+high.y)/2
        export(f'Gate_S{stage}',pieces+splinters(stage))
        PARTS=[]
        corridors[stage-1]()
        export(f'Corridor_S{stage}',[join(PARTS,f'Corridor_S{stage}')])
        PARTS=[]
        portals[stage-1]()
        export(f'Portal_S{stage}',[join(PARTS,f'Portal_S{stage}')])
    PARTS=[]
    random.seed(44971)
    export('RewardDoor',reward_door())
    assert len(ASSETS)==13
    assert set(bpy.context.scene.objects)=={o for _,objects,_,_ in ASSETS for o in objects}
    preview()
    print('DONE',flush=True)


if __name__=='__main__':
    main()
