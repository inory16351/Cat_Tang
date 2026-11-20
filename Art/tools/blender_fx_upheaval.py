# 땅 솟음 바위 (2026-11-20 사용자: "땅 솟아오르는 형태도 블렌더로 구워서, 잠깐 동안 남는 형태로")
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_upheaval.py
# 결과: Game/Assets/Resources/FX/Upheaval/fx_upheaval_{0,1,2}.fbx — 반지름 1m 기준 한 무더기
#   재질 칸 3개 (유니티에서 코드로 칠함): Rock = 바위 옆면, RockTop = 깨진 윗면(밝게), Glow = 바닥 가운데 갈라진 틈(빨갛게 빛남)
#   바위 판 7~9개가 가운데에서 바깥으로 비스듬히 젖혀져 솟은 모양(땅이 터져 갈라짐) + 작은 돌 덩이 + 바닥 별 모양 틈
import bpy, bmesh, math, random
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Game/Assets/Resources/FX/Upheaval"
PREVIEW = ROOT / "Art/tools/specs/blender_fx_upheaval_preview.png"

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgb):
    m = bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1); return m

def slab(bm, center, rng, h, tilt_dir, tilt, twist, top_mat, side_mat):
    """바위 판: 들쭉날쭉한 다각 기둥, 위는 좁고 비스듬히 깨짐, 바깥으로 tilt만큼 젖힘"""
    sides = rng.randint(4, 6)
    r0 = rng.uniform(0.22, 0.36)
    base = []
    for i in range(sides):
        a = i / sides * math.tau + rng.uniform(-0.25, 0.25) + twist
        rr = r0 * rng.uniform(0.75, 1.25) * (1.6 if i % 2 == 0 else 1.0)   # 납작한 판 모양 (한 축으로 길게)
        base.append(Vector((math.cos(a) * rr, math.sin(a) * rr, -0.25)))
    top = []
    cut = Vector((rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), 1)).normalized()   # 깨진 윗면 기울기
    for p in base:
        q = Vector((p.x * rng.uniform(0.6, 0.85), p.y * rng.uniform(0.6, 0.85), h))
        q.z += (q.x * cut.x + q.y * cut.y) * 1.4
        top.append(q)
    rot = Matrix.Rotation(tilt, 4, Vector((-tilt_dir.y, tilt_dir.x, 0)))
    vb = [bm.verts.new(rot @ p + center) for p in base]
    vt = [bm.verts.new(rot @ p + center) for p in top]
    f = bm.faces.new(vt); f.material_index = top_mat
    for i in range(sides):
        j = (i + 1) % sides
        f = bm.faces.new((vb[i], vb[j], vt[j], vt[i])); f.material_index = side_mat
    f = bm.faces.new(list(reversed(vb))); f.material_index = side_mat

def chunk(bm, center, rng, s, top_mat, side_mat):
    verts = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=s)["verts"]   # 울퉁불퉁하게 늘려 돌 덩이로
    for v in verts:
        v.co = Vector((v.co.x * rng.uniform(0.7, 1.3), v.co.y * rng.uniform(0.7, 1.3), v.co.z * rng.uniform(0.5, 0.9))) + center
    for f in {f for v in verts for f in v.link_faces}:
        f.material_index = top_mat if f.normal.z > 0.5 else side_mat

def crack(bm, rng, glow):
    """바닥 가운데 별 모양으로 갈라진 틈 (빛나는 납작한 판)"""
    n = rng.randint(7, 9)
    c = bm.verts.new((0, 0, 0.02))
    ring = []
    for i in range(n * 2):
        a = i / (n * 2) * math.tau + rng.uniform(-0.08, 0.08)
        r = rng.uniform(0.95, 1.15) if i % 2 == 0 else rng.uniform(0.12, 0.2)
        ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0.02)))
    for i in range(len(ring)):
        f = bm.faces.new((c, ring[i], ring[(i + 1) % len(ring)])); f.material_index = glow

def make(k):
    rng = random.Random(1000 + k * 77)
    me = bpy.data.meshes.new(f"fx_upheaval_{k}")
    ob = bpy.data.objects.new(f"fx_upheaval_{k}", me)
    bpy.context.scene.collection.objects.link(ob)
    for name, rgb in (("Rock", (0.3, 0.27, 0.28)), ("RockTop", (0.55, 0.5, 0.48)), ("Glow", (1, 0.1, 0.1))):
        me.materials.append(mat(name, rgb))
    bm = bmesh.new()
    crack(bm, rng, 2)
    n = rng.randint(7, 9)
    for i in range(n):
        a = i / n * math.tau + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        r = rng.uniform(0.35, 0.8)
        slab(bm, d * r, rng, rng.uniform(0.28, 0.6) * (1.15 - r * 0.4), d, math.radians(rng.uniform(38, 62)), a, 1, 0)
    for i in range(rng.randint(4, 6)):
        a = rng.uniform(0, math.tau); r = rng.uniform(0.7, 1.15)
        chunk(bm, Vector((math.cos(a) * r, math.sin(a) * r, 0.05)), rng, rng.uniform(0.08, 0.15), 1, 0)
    bm.normal_update()
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = False
    return ob

def export(ob):
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=str(OUT / (ob.name + ".fbx")), use_selection=True,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
        object_types={"MESH"}, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    print("EXPORTED", ob.name, len(ob.data.polygons), flush=True)

def preview(objs):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 1500, 520
    sc.render.filepath = str(PREVIEW)
    for i, o in enumerate(objs): o.location.x = (i - 1) * 3.0
    cam = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = (0, -7.5, 4.2); co.rotation_euler = (math.radians(62), 0, 0)
    bpy.ops.render.render(write_still=True)

reset()
objs = [make(k) for k in range(3)]
for o in objs: export(o)
preview(objs)
