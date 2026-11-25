# 미련 원혼 3D (2026-11-25 사용자: "유령 하이퍼 3D로 만들어서 더 유령 같은 느낌으로")
#   Hyper3D Rodin 생성(Art/models/fx_wraith_h3d/wraith_h3d.glb, 두건 쓴 떠다니는 유령 · 연기 꼬리) → 줄이고 맞춰서 FX 메시로
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_ghost.py
# 결과: Game/Assets/Art/FX/Meshes/fx_c2_ghost.fbx (원점 = 꼬리 끝 바닥, Unity +Y 높이 1, 얼굴 = Unity +Z) → Unity 메뉴 CatTang/FX 메시 가져오기
#   Unity (ux, uy, uz) → Blender (-ux, -uz, uy)  (blender_fx_ch2unlock.py와 같은 내보내기 설정)
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Art/models/fx_wraith_h3d/wraith_h3d.glb"
OUT = ROOT / "Game/Assets/Art/FX/Meshes/fx_c2_ghost.fbx"
PREVIEW = ROOT / "Art/tools/specs/blender_fx_ghost_preview.png"
TARGET_FACES = 3200

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SRC))
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in [o for o in bpy.context.scene.objects if o.type != "MESH"]: bpy.data.objects.remove(o)
ob.name = ob.data.name = "fx_c2_ghost"

# 줄이기
n0 = len(ob.data.polygons)
if n0 > TARGET_FACES:
    d = ob.modifiers.new("dec", "DECIMATE"); d.ratio = TARGET_FACES / n0
    bpy.ops.object.modifier_apply(modifier="dec")

# 재질 하나 (유니티 FX 재질이 덮음) · UV는 남겨 둠
ob.data.materials.clear()
m = bpy.data.materials.new("Ghost"); m.diffuse_color = (0.75, 0.8, 1, 1); ob.data.materials.append(m)

# 맞추기: 바닥(가장 낮은 점) = 0, 가로 가운데 = 원점, 높이 1
vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
h = mx.z - mn.z
c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in ob.data.vertices:
    v.co = (v.co - c) / h
ob.data.update()
for p in ob.data.polygons: p.use_smooth = True
print("GHOST faces", n0, "->", len(ob.data.polygons), "size", (mx - mn) / h, flush=True)

OUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT), use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH"}, mesh_smooth_type="FACE",
    add_leaf_bones=False, bake_anim=False)
print("EXPORTED", OUT, flush=True)

# 미리보기: 왼쪽 = 정면(Blender -Y 쪽에서, Unity +Z 얼굴이 보여야 함), 오른쪽 = 옆
sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.render.resolution_x, sc.render.resolution_y = 1000, 560
sc.render.filepath = str(PREVIEW)
side = ob.copy(); side.data = ob.data; sc.collection.objects.link(side)
ob.location.x = -0.6; side.location.x = 0.6; side.rotation_euler.z = math.radians(90)
cam = bpy.data.cameras.new("c"); cam.type = "ORTHO"; cam.ortho_scale = 2.4
co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
co.location = (0, -6, 0.5); co.rotation_euler = (math.radians(90), 0, 0)
bpy.ops.render.render(write_still=True)
