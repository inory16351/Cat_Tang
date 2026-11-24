# 여러 리깅 FBX를 쉬는 자세 정면 직교 렌더 — 목 뼈 높이를 같게 맞춰 나란히 (비율 비교용)
# 사용: blender -b --factory-startup -P blender_proportion_sheet.py -- <out_dir> <fbx>|<tex>[|<목 배율>] ...
#   각 모델을 <out_dir>/sheet_<i>.png 로 저장 (가로 붙이기는 파이썬에서)
import bpy, sys, math, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_dir, items = argv[0], argv[1:]
NECK_PX = 360          # 목 높이를 그림에서 몇 px로
RES = (520, 720)

for i, it in enumerate(items):
    parts = it.split("|")
    fbx, tex = parts[0], parts[1]
    mul = float(parts[2]) if len(parts) > 2 else 1.0     # 목 높이 배율 (키 맞춤 확인용)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if arm: arm.data.pose_position = "REST"
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if tex and os.path.exists(tex):
        img = bpy.data.images.load(tex)
        mat = bpy.data.materials.new("M"); mat.use_nodes = True
        nt = mat.node_tree; bsdf = nt.nodes["Principled BSDF"]
        ti = nt.nodes.new("ShaderNodeTexImage"); ti.image = img
        nt.links.new(ti.outputs["Color"], bsdf.inputs["Base Color"])
        for o in meshes: o.data.materials.clear(); o.data.materials.append(mat)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in meshes:
        e = o.evaluated_get(dg); pts += [o.matrix_world @ v.co for v in e.data.vertices]
    zmin = min(p.z for p in pts)
    cx = (min(p.x for p in pts) + max(p.x for p in pts)) / 2
    neck = None
    if arm:
        b = next((b for b in arm.data.bones if b.name.split(":")[-1] == "Neck"), None)
        if b: neck = (arm.matrix_world @ b.head_local).z - zmin
    if not neck: neck = (max(p.z for p in pts) - zmin) * 0.62
    neck /= mul
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "FLAT"; sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = RES
    sc.render.film_transparent = False
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = "ORTHO"
    # 세로 RES[1] px = neck * RES[1]/NECK_PX 월드
    cam.data.ortho_scale = neck * RES[1] / NECK_PX * (RES[0] / RES[1] if RES[0] > RES[1] else 1)
    cam.data.sensor_fit = "VERTICAL"; cam.data.ortho_scale = neck * RES[1] / NECK_PX
    viewH = neck * RES[1] / NECK_PX
    cam.location = Vector((cx, -10, zmin + viewH / 2 - viewH * 0.04))
    cam.rotation_euler = (math.radians(90), 0, 0)
    sc.render.filepath = os.path.join(out_dir, f"sheet_{i}.png")
    bpy.ops.render.render(write_still=True)
    print("SHEET", i, fbx, "neck", neck)
