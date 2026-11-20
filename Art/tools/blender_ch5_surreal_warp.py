# 5장 벽과 사물을 꿈처럼 휘고 녹이는 비파괴 백업 기반 후처리.
"""Blender 5.3: -b --factory-startup -P Art/tools/blender_ch5_surreal_warp.py.

Uses the village dream_volume lattice vocabulary on existing FBX geometry.
The default importer leaves a +90-degree X object rotation: vertex-local Y
is height. Work in the file's shared Z-up asset frame, then invert each
original object matrix. Never apply transforms or move fracture-piece origins.
Backups are immutable inputs; preview colours are assigned only after export.
"""

import hashlib
import json
import math
import random
import shutil
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
BACKUP = ROOT / 'Art/models/ch5_unwarped'
SPECS = ROOT / 'Art/tools/specs'
PREVIEW = SPECS / 'blender_ch5_surreal_warp_preview.png'
AUDIT = SPECS / 'blender_ch5_surreal_warp_audit.json'
PREFIXES = ('Kit_Wall', 'Kit_Cover', 'Kit_LM_', 'Kit_Warped',
            'Kit_MirrorShard', 'Kit_RiftCrack', 'Kit_Gate_', 'Kit_Portal_',
            'Kit_Corridor_', 'Kit_RewardDoor', 'Kit_FD_',
            'Kit_BA_HandSpire', 'Kit_BA_FloatingNumeral')
SKIP = ('Kit_W_', 'Kit_EP_', 'Kit_BP_', 'Kit_Floor', 'Kit_FX_',
        'Kit_BA_DialRing', 'Kit_Ch5_')
REPRESENTATIVES = ('Kit_Wall_MemoryWest', 'Kit_LM_ClockHeart',
                   'Kit_Wall_Mirror', 'Kit_WarpedKeg',
                   'Kit_FD_StationClock', 'Kit_Portal_S4',
                   'Kit_Gate_S2', 'Kit_Corridor_S2')
PALETTE = {
    'void_stone': (.29, .22, .43), 'void_stone_dark': (.10, .075, .19),
    'marble': (.76, .69, .88), 'marble_dark': (.37, .29, .48),
    'violet': (.47, .24, .65), 'violet_dark': (.19, .10, .32),
    'indigo': (.12, .13, .30), 'night_sky': (.035, .045, .13),
    'starlight': (.92, .95, 1), 'crack_gold': (1, .64, .12),
    'gold': (.94, .58, .15), 'gold_dark': (.43, .25, .08),
    'mirror': (.50, .78, .89), 'mirror_back': (.16, .24, .38),
    'crystal': (.65, .40, .89), 'magenta': (.92, .15, .55),
    'wood': (.40, .27, .42), 'wood_dark': (.17, .11, .21),
    'iron': (.32, .35, .45), 'iron_dark': (.10, .12, .19),
    'steel': (.56, .65, .73), 'brass': (.66, .44, .17),
    'dial_face': (.88, .81, .67), 'dial_mark': (.16, .11, .23),
    'sand': (.64, .48, .62), 'red_lacquer': (.47, .10, .26),
    'cloth': (.38, .18, .47), 'lamp_warm': (1, .70, .30),
    'porcelain': (.88, .83, .93), 'velvet': (.27, .08, .29),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1


def meshes():
    return sorted((o for o in bpy.context.scene.objects if o.type == 'MESH'),
                  key=lambda o: o.name)


def bounds(points):
    points = list(points)
    return (Vector(tuple(min(p[i] for p in points) for i in range(3))),
            Vector(tuple(max(p[i] for p in points) for i in range(3))))


def world_points(objects):
    return [o.matrix_world @ v.co for o in objects for v in o.data.vertices]


def tri_count(data):
    faces = data.faces if isinstance(data, bmesh.types.BMesh) else data.polygons
    return sum(len(f.verts if hasattr(f, 'verts') else f.vertices) - 2 for f in faces)


def topology(bm):
    return {key: sum(1 for e in bm.edges if test(e)) for key, test in (
        ('boundary', lambda e: e.is_boundary),
        ('nonmanifold', lambda e: len(e.link_faces) > 2),
        ('wire', lambda e: e.is_wire))}


def canonical_materials(obj):
    """FBX importer material discovery order can vary across Blender processes."""
    old = list(obj.data.materials)
    order = sorted(range(len(old)), key=lambda i: (old[i].name if old[i] else '', i))
    remap = {old_index: new_index for new_index, old_index in enumerate(order)}
    indices = [p.material_index for p in obj.data.polygons]
    for new_index, old_index in enumerate(order):
        obj.data.materials[new_index] = old[old_index]
    for polygon, index in zip(obj.data.polygons, indices):
        polygon.material_index = remap.get(index, index)


def canonical_bmesh(bm):
    # Stable geometric ordering also fixes edge-split budget tie breaking when
    # FBX importer batches source polygons by a different material order.
    order = {v: i for i, v in enumerate(sorted(bm.verts, key=lambda v: tuple(v.co)))}
    bm.verts.sort(key=order.__getitem__)
    bm.verts.index_update()
    order = {e: i for i, e in enumerate(sorted(bm.edges,
        key=lambda e: tuple(sorted(v.index for v in e.verts))))}
    bm.edges.sort(key=order.__getitem__)
    bm.edges.index_update()
    order = {f: i for i, f in enumerate(sorted(bm.faces,
        key=lambda f: (tuple(sorted(v.index for v in f.verts)), f.material_index)))}
    bm.faces.sort(key=order.__getitem__)
    bm.faces.index_update()


def densify(obj):
    """Conforming edge splits, with custom-data interpolation and a hard 3x cap.

    Prefer longest edges, then repeat on their children. Trial subdivisions
    measure actual triangulated cost, including the neighbours of split edges.
    No decimation, welding, UV replacement, or material-slot rewriting.
    """
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    before = tri_count(bm)
    limit = 3 * before
    matrix = obj.matrix_world
    for _ in range(7):
        canonical_bmesh(bm)
        bm.edges.ensure_lookup_table()
        bm.edges.index_update()
        candidates = sorted(
            ((matrix.to_3x3() @ (e.verts[0].co - e.verts[1].co)).length, e.index)
            for e in bm.edges
            if (matrix.to_3x3() @ (e.verts[0].co - e.verts[1].co)).length > .25001)
        candidates.reverse()
        if not candidates or tri_count(bm) >= limit:
            break

        def trial(count):
            result = bm.copy()
            result.edges.ensure_lookup_table()
            bmesh.ops.subdivide_edges(result,
                edges=[result.edges[i] for _, i in candidates[:count]],
                cuts=1, use_grid_fill=True, smooth=0)
            return result

        result = trial(len(candidates))
        if tri_count(result) > limit:
            result.free()
            low, high = 0, len(candidates)
            while low + 1 < high:
                mid = (low + high) // 2
                test = trial(mid)
                fits = tri_count(test) <= limit
                test.free()
                if fits:
                    low = mid
                else:
                    high = mid
            if not low:
                break
            result = trial(low)
        bm.free()
        bm = result
    assert tri_count(bm) <= limit
    # Freeze triangulation while faces are still planar. This avoids a warped
    # concave n-gon choosing a different diagonal on FBX export or in Unity.
    bmesh.ops.triangulate(bm, faces=list(bm.faces), quad_method='BEAUTY', ngon_method='BEAUTY')
    canonical_bmesh(bm)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return before, tri_count(obj.data)


def smooth(a, b, x):
    t = max(0., min(1., (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


class Lattice:
    def __init__(self, name, low, high):
        self.low, self.high = low, high
        self.size = high - low
        self.center = (low + high) / 2
        self.height = max(self.size.z, .001)
        self.wall = name.startswith(('Kit_Wall', 'Kit_Gate_', 'Kit_Portal_',
                                     'Kit_Corridor_', 'Kit_RewardDoor'))
        self.corridor = name.startswith('Kit_Corridor_')
        self.passage = name.startswith(('Kit_Gate_', 'Kit_Portal_',
                                       'Kit_Corridor_', 'Kit_RewardDoor',
                                       'Kit_Wall_StarArch'))
        self.floating = name.startswith(('Kit_FD_', 'Kit_BA_FloatingNumeral'))
        self.seed = int.from_bytes(hashlib.sha256((name + '.fbx').encode()).digest()[:8], 'big')
        rng = random.Random(self.seed)
        self.sign = 1 if self.seed % 2 else -1
        self.lean = self.sign * rng.uniform(.20, .32) * self.height          # 2026-11-20 사용자: 왜곡이 안 보임 → 2~3배 (마을 건물만큼)
        self.twist = self.sign * math.radians(rng.uniform(22, 38))
        self.sag = rng.uniform(.14, .24) * self.height
        self.phase = rng.uniform(0, math.tau)
        self.frequency = rng.uniform(4.0, 6.5) / max(self.size.x, .25)
        self.amplitude = rng.uniform(.05, .08) * max(.1, min(max(self.size.x, self.size.y), self.height))
        self.gain = Vector((1, 1, 1))

    def raw(self, point):
        x, y, z = point
        cx, cy, cz = self.center
        t = max(0., min(1., (z - self.low.z) / self.height))
        if not self.floating and t <= .08:
            return point.copy()
        lock = 1 if self.floating else smooth(.08, .30, t)
        xx, yy = x - cx, y - cy
        h = 2 * (t - .5) if self.floating else t
        # Centre pivots for floating debris. Its lower half bends as well.
        bend = self.lean * (h * abs(h) if self.floating else t * t)
        twist = self.twist * h
        bulge = .16 * math.sin(math.pi * t)
        if self.height > 1.25 * max(self.size.x, self.size.y):
            bulge -= .24 * math.sin(math.pi * t) ** 4
        bx, by = xx * (1 + bulge), yy * (1 + bulge)
        co, si = math.cos(twist), math.sin(twist)
        dx = bx * co - by * si - xx + bend
        dy = bx * si + by * co - yy
        phase = self.phase
        # Low frequency analytic noise: continuous across all disconnected parts
        # and material seams, so decorations follow their supporting surfaces.
        w = self.amplitude
        dx += w * math.sin(2.7 * t + yy / max(self.size.y, .25) + phase)
        dy += w * math.sin(3.1 * t + xx / max(self.size.x, .25) * 2.3 + phase)
        melt = self.sag * (.5 + .5 * math.sin(xx * self.frequency + phase))
        if self.size.x > 3:
            corner = smooth(.20, .50, self.sign * xx / self.size.x)
            melt += .16 * self.height * corner
        dz = -t ** 3 * melt
        dz += w * .25 * math.sin(xx / max(self.size.x, .25) * 3 + phase) * math.sin(math.pi * t)
        if self.passage:
            # Keep the centre third straight; blend over a broad shoulder to
            # avoid a crease. Also forbid jambs from moving toward the lane.
            pocket = smooth(1 / 6, .48, abs(xx) / max(self.size.x, .001))
            dx *= pocket
            dy *= pocket
            dz *= pocket
            dx = max(0, dx) if xx > 0 else min(0, dx)
            if self.corridor:
                # Only gentle side-detail wobble/melt, no bending/twisting the
                # walking surface. Protect Z=.02 bridge slabs even if zmin<0.
                floor_lock = smooth(.10, .55, z)
                dx = w * .25 * math.sin(yy * .65 + phase) * pocket * floor_lock
                dx = max(0, dx) if xx > 0 else min(0, dx)
                dy = w * .15 * math.sin(xx + phase) * pocket * floor_lock
                dz *= .40 * floor_lock
        return point + Vector((dx, dy, dz)) * lock

    def fit(self, points):
        """Limit displacement, not the original mesh size or object transform.

        Thin wall depth would otherwise grow several times under a full twist.
        One common gain per axis keeps all parts on the same smooth lattice.
        Leave 1% headroom under the requested 12% total extent increase.
        """
        gains = [1., 1., 1.]
        for p in points:
            d = self.raw(p) - p
            for i in range(3):
                margin = self.size[i] * .18          # 크기 증가 허용 약 36% (예전 12%라 변형이 거의 안 보였음)
                low = self.low[i] - margin
                high = self.high[i] + margin
                if i == 2 and not self.floating:
                    low = self.low[i]
                if d[i] > 1e-10:
                    gains[i] = min(gains[i], max(0, (high - p[i]) / d[i]))
                elif d[i] < -1e-10:
                    gains[i] = min(gains[i], max(0, (low - p[i]) / d[i]))
        self.gain = Vector(gains)

    def __call__(self, point):
        d = self.raw(point) - point
        return point + Vector(tuple(d[i] * self.gain[i] for i in range(3)))

    def determinant(self, p):
        epsilon = max(self.height * .0001, .00001)
        columns = []
        for i in range(3):
            step = Vector((0, 0, 0))
            step[i] = epsilon
            columns.append((self(p + step) - self(p - step)) / (2 * epsilon))
        return Matrix(columns).transposed().determinant()


def snapshot(objects):
    return [{'name': o.name,
             'vertices': [tuple(o.matrix_world @ v.co) for v in o.data.vertices],
             'faces': [tuple(p.vertices) for p in o.data.polygons],
             'indices': [p.material_index for p in o.data.polygons],
             'materials': [m.name if m else 'void_stone' for m in o.data.materials]}
            for o in objects]


def process(path, preview_names):
    source = BACKUP / path.name
    if not source.exists():
        # Exclusive creation: even a partial run must never overwrite a backup.
        with source.open('xb') as dst, path.open('rb') as src:
            shutil.copyfileobj(src, dst)
        shutil.copystat(path, source)
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    reset()
    bpy.ops.import_scene.fbx(filepath=str(source))  # Default importer, on purpose.
    objects = meshes()
    assert objects, f'No meshes: {path.name}'
    for obj in objects:
        canonical_materials(obj)
    low, high = bounds(world_points(objects))
    original = snapshot(objects) if path.stem in preview_names else None
    transforms = {o.name: o.matrix_world.copy() for o in objects}
    slots = {o.name: [m.name if m else None for m in o.data.materials] for o in objects}
    uv_layers = {o.name: [uv.name for uv in o.data.uv_layers] for o in objects}
    counts = [densify(o) for o in objects]
    points = world_points(objects)
    lattice = Lattice(path.stem, low, high)
    lattice.fit(points)
    # Check the actual deformation Jacobian at mesh vertices and a regular
    # interior grid; a positive determinant rules out local lattice foldovers.
    probes = points[::max(1, len(points) // 1500)]
    probes += [low + Vector(tuple((high[i] - low[i]) * (v[i] / (2 if i == 1 else 8)) for i in range(3)))
               for v in ((x, y, z) for x in range(9) for y in range(3) for z in range(9))]
    source_triangles = []
    inherited_slivers = 0
    for obj in objects:
        vertices = [obj.matrix_world @ v.co for v in obj.data.vertices]
        for face in obj.data.polygons:
            a, b, c = (vertices[i] for i in face.vertices)
            normal = (b - a).cross(c - a)
            longest_squared = max((b - a).length_squared, (c - b).length_squared,
                                  (a - c).length_squared)
            # Existing bevel exports contain almost-collinear subpixel slivers.
            # Their normals are dominated by float32 cancellation. Test real
            # surfaces; use the positive lattice Jacobian for inherited slivers.
            if (normal.length > 1e-8 * max(high - low) ** 2
                    and normal.length > 1e-5 * longest_squared):
                source_triangles.append((a, b, c, normal.normalized()))
            else:
                inherited_slivers += 1

    def face_orientation():
        minimum = 1.
        for a, b, c, normal in source_triangles:
            aa, bb, cc = lattice(a), lattice(b), lattice(c)
            warped = (bb - aa).cross(cc - aa)
            agreement = normal.dot(warped.normalized()) if warped.length > 1e-12 else -1.
            minimum = min(minimum, agreement)
        return minimum

    minimum_jacobian = min(lattice.determinant(p) for p in probes)
    minimum_orientation = face_orientation()
    # Fade the entire warp only if the geometry's proportions create a fold.
    attempts = 0
    while minimum_jacobian < .15 or minimum_orientation < .05:
        attempts += 1
        assert attempts <= 12, (path.name, 'unsafe deformation', minimum_orientation)
        lattice.gain *= .85
        minimum_jacobian = min(lattice.determinant(p) for p in probes)
        minimum_orientation = face_orientation()
    locked = 0
    max_displacement = 0.
    for obj in objects:
        matrix = transforms[obj.name]
        inverse = matrix.inverted()
        for vertex in obj.data.vertices:
            p = matrix @ vertex.co
            q = lattice(p)
            max_displacement = max(max_displacement, (q - p).length)
            if not lattice.floating and (p.z - low.z) / lattice.height <= .08:
                assert (q - p).length < 1e-7
                locked += 1
                continue  # Preserve original bottom vertex bytes exactly.
            vertex.co = inverse @ q
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        before_topology = topology(bm)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        assert topology(bm) == before_topology
        assert all(math.isfinite(c) for v in bm.verts for c in v.co)
        bm.to_mesh(obj.data)
        bm.free()
        # Imported split normals describe the straight input, not the warp.
        if obj.data.has_custom_normals:
            obj.data.normals_split_custom_set([(0, 0, 0)] * len(obj.data.loops))
        obj.data.update()
        assert [m.name if m else None for m in obj.data.materials] == slots[obj.name]
        assert [uv.name for uv in obj.data.uv_layers] == uv_layers[obj.name]
    after_low, after_high = bounds(world_points(objects))
    ratios = [(after_high[i] - after_low[i]) / max(high[i] - low[i], 1e-7) for i in range(3)]
    assert all(.75 < r <= 1.40 for r in ratios), (path.name, ratios)   # 2026-11-20 세게 (예전 1.12)
    after = snapshot(objects) if original else None
    count = sum(tri_count(o.data) for o in objects)
    assert count <= 3 * sum(c[0] for c in counts)
    # bake_space_transform also bakes the export axis conversion. Exporting
    # imported +90 X matrices verbatim would accumulate another +90 on import,
    # despite compensating vertex positions. Undo the import basis on objects
    # and bake it into vertices so BOTH the mesh and node transforms round-trip.
    import_basis = Matrix.Rotation(math.pi / 2, 4, 'X')
    for obj in objects:
        obj.data.transform(import_basis)
        obj.matrix_world = transforms[obj.name] @ import_basis.inverted()
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    temporary = SPECS / '_ch5_warp_roundtrip.fbx'
    bpy.ops.export_scene.fbx(filepath=str(temporary), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
        use_custom_props=True)
    # Reimport EVERY candidate before replacing a game asset. Check Blender's
    # axis conversion, all fracture-piece origins, mesh counts and role names.
    reset()
    bpy.ops.import_scene.fbx(filepath=str(temporary))
    exported = meshes()
    for obj in exported:
        canonical_materials(obj)
    assert {o.name for o in exported} == set(transforms)
    export_low, export_high = bounds(world_points(exported))
    bbox_error = max((export_low - after_low).length, (export_high - after_high).length)
    assert bbox_error < .0002, (path.name, bbox_error)
    matrix_error = max(abs(o.matrix_world[i][j] - transforms[o.name][i][j])
                       for o in exported for i in range(4) for j in range(4))
    assert matrix_error < .0002, (path.name, 'transform', matrix_error)
    assert sum(tri_count(o.data) for o in exported) == count
    for obj in exported:
        assert [m.name if m else None for m in obj.data.materials] == slots[obj.name]
        assert [uv.name for uv in obj.data.uv_layers] == uv_layers[obj.name]
    assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash
    temporary.replace(path)
    record = dict(name=path.stem, source_sha256=source_hash,
                  original_tris=sum(c[0] for c in counts), warped_tris=count,
                  original_bounds=[list(low), list(high)],
                  warped_bounds=[list(export_low), list(export_high)],
                  size_ratios=ratios, gain=list(lattice.gain), seed=lattice.seed,
                  min_sampled_jacobian=minimum_jacobian,
                  min_face_normal_agreement=minimum_orientation,
                  inherited_numerical_slivers=inherited_slivers,
                  max_displacement=max_displacement, bottom_locked_vertices=locked,
                  object_count=len(exported), material_slots=slots,
                  uv_layers=uv_layers, roundtrip_bbox_error=bbox_error,
                  roundtrip_transform_error=matrix_error,
                  floating=lattice.floating, corridor=lattice.corridor)
    print(f'WARPED {path.stem} {count}', flush=True)
    return record, (original, after) if original else None


def render_preview(pairs):
    reset()
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 2400, 1800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.studiolight_rotate_z = .35
    shading.color_type = 'MATERIAL'
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor = 1.2
    shading.curvature_valley_factor = 1.0
    shading.show_object_outline = True
    shading.object_outline_color = (.035, .025, .06)
    shading.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Warp review world')
    scene.world.color = (.027, .023, .046)
    scene.view_settings.view_transform = 'Standard'
    scene.display.render_aa = '32'
    camera_data = bpy.data.cameras.new('Warp review camera')
    camera = bpy.data.objects.new('Warp review camera', camera_data)
    scene.collection.objects.link(camera)
    direction = Vector((8, 17, 14)).normalized()
    camera.location = direction * 70
    camera.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = 32
    scene.camera = camera
    rotation = camera.rotation_euler.to_matrix()
    right, up = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0))

    def material(name):
        mat = bpy.data.materials.get(name)
        if mat is None:
            mat = bpy.data.materials.new(name)
            mat.diffuse_color = (*PALETTE.get(name, (.55, .45, .65)), 1)
        return mat

    def text(label, x, y, size, role='starlight'):
        data = bpy.data.curves.new(label, 'FONT')
        data.body, data.size, data.align_x = label, size, 'CENTER'
        obj = bpy.data.objects.new(label, data)
        scene.collection.objects.link(obj)
        obj.location = right * x + up * y + direction * 4
        obj.rotation_euler = camera.rotation_euler
        data.materials.append(material(role))

    text('CHAPTER 05  /  SURREAL WARP', 0, 10.65, .53)
    text('BENT SILHOUETTES  /  MELTING CROWNS  /  PROTECTED PASSAGES', 0, 10.05, .22, 'gold')
    for index, (name, (before, after)) in enumerate(pairs.items()):
        col, row = index % 2, index // 2
        group_x = -8 if col == 0 else 8
        group_y = 7.45 - row * 5.05
        text(name.removeprefix('Kit_'), group_x, group_y + 1.60, .28, 'gold')
        yaw = math.pi / 2 if 'Corridor' in name else 0
        rotate = Matrix.Rotation(yaw, 4, 'Z')
        both = [rotate @ Vector(v) for group in (before, after)
                for item in group for v in item['vertices']]
        x0, x1 = min(v.dot(right) for v in both), max(v.dot(right) for v in both)
        y0, y1 = min(v.dot(up) for v in both), max(v.dot(up) for v in both)
        scale = min(6.7 / max(x1 - x0, .01), 3.55 / max(y1 - y0, .01))
        for label, data, offset in (('BEFORE', before, -3.7), ('AFTER', after, 3.7)):
            anchor = (right * (group_x + offset - scale * (x0 + x1) / 2)
                      + up * (group_y - .5 - scale * (y0 + y1) / 2))
            for item in data:
                mesh = bpy.data.meshes.new(item['name'] + label)
                mesh.from_pydata([anchor + (rotate @ Vector(v)) * scale for v in item['vertices']], [], item['faces'])
                mesh.update()
                obj = bpy.data.objects.new(item['name'] + label, mesh)
                scene.collection.objects.link(obj)
                for role in item['materials']:
                    mesh.materials.append(material(role))
                for polygon, mat_index in zip(mesh.polygons, item['indices']):
                    polygon.material_index = mat_index
            text(label, group_x + offset, group_y - 2.65, .21,
                 'mirror' if label == 'BEFORE' else 'crack_gold')
    scene.render.filepath = str(PREVIEW)
    bpy.ops.render.render(write_still=True)


def main():
    BACKUP.mkdir(parents=True, exist_ok=True)
    SPECS.mkdir(parents=True, exist_ok=True)
    paths = sorted(p for p in OUTPUT.glob('Kit_*.fbx')
                   if p.name.startswith(PREFIXES) and not p.name.startswith(SKIP))
    assert paths, f'No matching FBX files in {OUTPUT}'
    available = {p.stem for p in paths}
    chosen = [n for n in REPRESENTATIVES if n in available]
    chosen += [p.stem for p in paths if p.stem not in chosen][:8 - len(chosen)]
    records, captured = [], {}
    for path in paths:
        record, pair = process(path, set(chosen))
        records.append(record)
        if pair:
            captured[path.stem] = pair
        AUDIT.write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
    render_preview({name: captured[name] for name in chosen})
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
