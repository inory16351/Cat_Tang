# 자객 고양이 잔상 자세 모델 (2026-11-24, 거울 망토 그림자 베기) — Hyper3D GLB → 바닥 0 · 가운데 맞춤 · 앞 = -Y(블렌더) → FBX + 미리보기
#   blender -b --factory-startup -P Art/tools/blender_assassin_pose.py
#   원화 Art/tripo_input/assassin/assassin_<자세>.jpg (내장 브라우저 ChatGPT) → Rodin Gen-2.5 Quad 12k
#   결과 Game/Assets/Resources/Costumes/assassin_<자세>.fbx  (두 자세 같은 배율: 움츠린 자세 높이 = H_WINDUP)
import bpy, os, math
from mathutils import Vector
ROOT = r"C:/Project/Cat_Tang"
SRC = ROOT + "/Art/models/incoming/hyper3d_assassin"
OUT = ROOT + "/Game/Assets/Resources/Costumes"      # Resources.Load("Costumes/assassin_<자세>")로 바로 씀
H_WINDUP = 0.8                     # 주인공 키(블렌더 약 0.98)보다 조금 낮게 — 움츠린 자세
os.makedirs(OUT, exist_ok=True)
scale = None
for pose in ("windup", "slash"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=f"{SRC}/{pose}/base.glb")
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    if len(obs) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o != ob: bpy.data.objects.remove(o)
    vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    if scale is None: scale = H_WINDUP / (mx.z - mn.z)
    c = (mn + mx) / 2
    for v in ob.data.vertices:
        p = ob.matrix_world @ v.co
        v.co = Vector(((p.x - c.x) * scale, (p.y - c.y) * scale, (p.z - mn.z) * scale))
    ob.matrix_world.identity(); ob.name = "assassin_" + pose
    print("POSE", pose, "size", [round((mx[i] - mn[i]) * scale, 3) for i in range(3)])
    bpy.ops.export_scene.fbx(filepath=f"{OUT}/assassin_{pose}.fbx", use_selection=False, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=True, path_mode='COPY', embed_textures=False)
    # 미리보기: 정면(-Y) · 옆(+X) 두 장
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); bpy.context.scene.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 1.6
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); bpy.context.scene.collection.objects.link(sun); sun.rotation_euler = (0.8, 0.2, 0.6)
    sc = bpy.context.scene; sc.camera = cam; sc.render.resolution_x = sc.render.resolution_y = 512
    w = bpy.data.worlds.new("w"); sc.world = w; w.color = (0.8, 0.8, 0.8)
    for tag, loc in (("front", (0, -5, 0.45)), ("side", (5, 0, 0.45)), ("top", (2.5, -4, 4))):
        cam.location = loc; cam.rotation_euler = (Vector((0, 0, 0.4)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = f"{SRC}/{pose}/preview_{tag}.png"; bpy.ops.render.render(write_still=True)
print("DONE")
