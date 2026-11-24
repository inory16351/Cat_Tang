# 코스튬 모델(Hyper3D 리깅 FBX)을 원본 주인공(CatHero) 신체 비율에 맞게 뼈 단위로 변형 → 쉬는 자세로 굳혀 FBX 저장
# 측정: blender_measure_body.py · blender_proportion_sheet.py (목 높이 맞춘 정면 비교)
# 사용: blender -b --factory-startup -P blender_costume_proportion.py -- <in.fbx> <out.fbx> '<json 배율>'
#   배율 키 = 뼈 이름(mixamorig: 접두어 없이) → 숫자(균일) 또는 [x, y, z](뼈 로컬, y = 뼈 길이 방향)
#   "_noinherit": 부모 배율을 물려받지 않을 뼈 이름들 (예: 팔 길이만 늘리고 손 크기는 그대로)
import bpy, sys, json

argv = sys.argv[sys.argv.index("--") + 1:]
src, dst, cfg = argv[0], argv[1], json.loads(argv[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]

def short(n): return n.split(":")[-1]
pb = {short(b.name): b for b in arm.pose.bones}

for n in cfg.get("_noinherit", []):
    if n in pb: arm.data.bones[pb[n].name].inherit_scale = "NONE"
for n, v in cfg.items():
    if n.startswith("_") or n not in pb: continue
    pb[n].scale = (v, v, v) if isinstance(v, (int, float)) else tuple(v)
bpy.context.view_layer.update()

# 메쉬에 변형 굳힘 (아마추어 모디파이어 복제 → 하나 적용)
for o in meshes:
    mod = next((m for m in o.modifiers if m.type == "ARMATURE"), None)
    if not mod: continue
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    if o.data.shape_keys: bpy.ops.object.shape_key_remove(all=True, apply_mix=True)
    bpy.ops.object.modifier_copy(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    o.select_set(False)

# 지금 자세를 쉬는 자세로
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode="OBJECT")
for b in arm.data.bones: b.inherit_scale = "FULL"

# 키 맞춤 (선택): "_neck" = 목 뼈가 있어야 할 높이 (원본 CatHero 목 0.6301 × 키 배율). 발바닥 = 0
#   원본도 블렌더에서 같은 단위(키 약 0.98)로 읽히므로 Unity에서는 주인공과 같은 ModelScale만 곱하면 됨
for o in bpy.context.scene.objects: o.select_set(o in meshes or o == arm)
if "_neck" in cfg:
    bpy.context.view_layer.update()
    zmin = min((o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices)
    nb = next(b for b in arm.data.bones if short(b.name) == "Neck")
    neck = (arm.matrix_world @ nb.head_local).z - zmin
    k = cfg["_neck"] / neck
    for o in bpy.context.scene.objects:
        if o.parent is None and (o == arm or o in meshes):
            o.location.z -= zmin
            o.matrix_world = __import__("mathutils").Matrix.Scale(k, 4) @ o.matrix_world
    bpy.context.view_layer.update()
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
    zmin2 = min((o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices)
    if abs(zmin2) > 1e-4:   # 발바닥을 0에 (부모 · 회전 섞인 FBX라 한 번 더)
        for o in bpy.context.scene.objects:
            if o.parent is None and (o == arm or o in meshes): o.location.z -= zmin2
        bpy.context.view_layer.update()
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
        bpy.context.view_layer.update()
    zmin2 = min((o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices)
    zmax2 = max((o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices)
    print(f"FIT scale={k:.4f} neck={(arm.matrix_world @ nb.head_local).z - zmin2:.4f} height={zmax2 - zmin2:.4f} floor={zmin2:.4f}")

bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=False, path_mode="AUTO", mesh_smooth_type="FACE")
print("PROPORTIONED", dst)
