# 리깅 안 된 Hyper3D 메쉬에 비슷한 체형의 리깅된 모델 뼈 · 가중치를 옮김 (Hyper3D 크레딧 없이 4008 잠옷 리깅, 2026-11-24)
# 두 모델을 키 · 가운데로 맞춘 뒤, 뼈는 그대로 쓰고 가중치는 가장 가까운 면에서 보간 (Data Transfer)
# 사용: blender -b --factory-startup -P blender_rig_transfer.py -- <rigged.fbx> <unrigged.fbx> <out.fbx>
import bpy, sys
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:]
rigged, unrigged, out = argv

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=rigged)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
src = [o for o in bpy.context.scene.objects if o.type == "MESH"]
before = set(bpy.context.scene.objects)
bpy.ops.import_scene.fbx(filepath=unrigged)
new = [o for o in bpy.context.scene.objects if o not in before]
dst = [o for o in new if o.type == "MESH"]
for o in new:
    if o.type != "MESH": bpy.data.objects.remove(o)
bpy.context.view_layer.update()

def bbox(objs):
    ps = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    mn = Vector((min(p.x for p in ps), min(p.y for p in ps), min(p.z for p in ps)))
    mx = Vector((max(p.x for p in ps), max(p.y for p in ps), max(p.z for p in ps)))
    return mn, mx

# 새 메쉬를 기존 모델 키 · 바닥 · 가운데에 맞춤
smn, smx = bbox(src); dmn, dmx = bbox(dst)
k = (smx.z - smn.z) / (dmx.z - dmn.z)
sc = Vector(((smn.x + smx.x) / 2, (smn.y + smx.y) / 2, smn.z))
dc = Vector(((dmn.x + dmx.x) / 2, (dmn.y + dmx.y) / 2, dmn.z))
M = Matrix.Translation(sc) @ Matrix.Scale(k, 4) @ Matrix.Translation(-dc)
for o in dst:
    o.matrix_world = M @ o.matrix_world
bpy.context.view_layer.update()
bpy.ops.object.select_all(action="DESELECT")
for o in dst: o.select_set(True)
bpy.context.view_layer.objects.active = dst[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
print(f"ALIGN k={k:.4f} src_h={smx.z - smn.z:.4f}")

# 기존 메쉬를 하나로 (가중치 원본)
bpy.ops.object.select_all(action="DESELECT")
for o in src: o.select_set(True)
bpy.context.view_layer.objects.active = src[0]
if len(src) > 1: bpy.ops.object.join()
srcm = bpy.context.view_layer.objects.active

for o in dst:
    for g in srcm.vertex_groups:
        if g.name not in o.vertex_groups: o.vertex_groups.new(name=g.name)
    m = o.modifiers.new("DT", "DATA_TRANSFER")
    m.object = srcm
    m.use_vert_data = True
    m.data_types_verts = {"VGROUP_WEIGHTS"}
    m.vert_mapping = "POLYINTERP_NEAREST"
    m.layers_vgroup_select_src = "ALL"; m.layers_vgroup_select_dst = "NAME"
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier="DT")
    # 정점마다 가중치 4개까지 · 합 1
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True)
    bpy.ops.object.mode_set(mode="WEIGHT_PAINT")
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    o.parent = arm
    o.matrix_parent_inverse = arm.matrix_world.inverted()
    am = o.modifiers.new("Armature", "ARMATURE"); am.object = arm

bpy.data.objects.remove(srcm)
bpy.ops.object.select_all(action="DESELECT")
for o in dst + [arm]: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=False, path_mode="AUTO", mesh_smooth_type="FACE")
print("RIGGED", out)
