# 리깅 확인: 팔 내리고 · 다리 들고 · 고개 돌린 자세로 정면/옆 렌더 (변형 뒤 가중치가 멀쩡한지)
# 사용: blender -b --factory-startup -P blender_pose_test.py -- <out.png> <fbx>|<tex> ...
import bpy, sys, math, os
from mathutils import Vector, Euler

argv = sys.argv[sys.argv.index("--") + 1:]
out, items = argv[0], argv[1:]
frames = []
for i, it in enumerate(items):
    fbx, tex = it.split("|")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    img = bpy.data.images.load(tex)
    mat = bpy.data.materials.new("M"); mat.use_nodes = True
    nt = mat.node_tree; ti = nt.nodes.new("ShaderNodeTexImage"); ti.image = img
    nt.links.new(ti.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    for o in meshes: o.data.materials.clear(); o.data.materials.append(mat)
    arm.data.pose_position = "POSE"
    pb = {b.name.split(":")[-1]: b for b in arm.pose.bones}
    def rot(n, axis, deg):
        if n in pb:
            b = pb[n]; b.rotation_mode = "XYZ"
            e = list(b.rotation_euler); e["XYZ".index(axis)] += math.radians(deg); b.rotation_euler = e
    # 뼈 로컬 축은 리그마다 다르니 월드 기준으로: 팔은 아래로, 왼다리 앞으로, 고개 옆으로
    def world_rot(n, axis_world, deg):
        if n not in pb: return
        b = pb[n]; b.rotation_mode = "QUATERNION"
        M = (arm.matrix_world @ b.bone.matrix_local).to_3x3().normalized()
        from mathutils import Quaternion
        q = Quaternion(M.inverted() @ Vector(axis_world), math.radians(deg))
        b.rotation_quaternion = q @ b.rotation_quaternion
    world_rot("LeftArm", (0, 1, 0), 35); world_rot("RightArm", (0, 1, 0), -35)
    world_rot("LeftForeArm", (0, 0, 1), 60); world_rot("RightForeArm", (0, 0, 1), -60)
    world_rot("LeftUpLeg", (1, 0, 0), -50); world_rot("LeftLeg", (1, 0, 0), 60)
    world_rot("Head", (0, 0, 1), 25); world_rot("Spine1", (1, 0, 0), -10)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    pts = [o.matrix_world @ v.co for o in meshes for v in o.evaluated_get(dg).data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (mn + mx) / 2; h = max(mx.z - mn.z, mx.x - mn.x) * 1.1
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = 360, 420
    cam = bpy.data.objects.new("C", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = "ORTHO"; cam.data.ortho_scale = h
    for k, ang in enumerate((25, 90)):
        a = math.radians(ang)
        cam.location = c + Vector((math.sin(a), -math.cos(a), 0)) * 10
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(os.path.dirname(out), f"_pose_{i}_{k}.png"); sc.render.filepath = p
        bpy.ops.render.render(write_still=True); frames.append(p)
print("POSED", len(frames))
