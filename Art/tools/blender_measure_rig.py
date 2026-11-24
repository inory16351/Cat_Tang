# 리깅된 FBX의 쉬는 자세 신체 비율 측정 (머리 · 목 · 골반 · 무릎 · 발 · 손 높이, 머리 폭, 전체 키)
# 사용: blender -b --factory-startup -P blender_measure_rig.py -- <a.fbx> [<b.fbx> ...]
import bpy, sys, json
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
for path in argv:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    pts = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    zmin = min(p.z for p in pts); zmax = max(p.z for p in pts)
    out = {"file": path, "mesh_h": round(zmax - zmin, 4)}
    if arm:
        bones = {}
        for b in arm.data.bones:
            h = arm.matrix_world @ b.head_local; t = arm.matrix_world @ b.tail_local
            bones[b.name] = (round(h.x, 3), round(h.y, 3), round(h.z - zmin, 3), round(t.z - zmin, 3))
        out["bones"] = bones
    print("MEASURE " + json.dumps(out, ensure_ascii=False))
