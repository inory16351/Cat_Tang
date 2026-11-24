# 자객 고양이 리깅 모델 정리 (2026-11-24, 거울 망토 칼 휘두르기)
#   Hyper3D Rodin rigging(human, 59f4910b…) → Art/models/incoming/hyper3d_assassin/rigged/base_shaded.fbx (+ shaded.png)
#   가져오면 누워 있음(위 = -Y, 앞 = -X) → 서게 돌리고 Z 90° (위 = +Z, 앞 = -Y) 움츠린 키 0.8 · 발 = 바닥 · 가운데 맞춤
#   오른손 IK(팔 · 아래팔 2마디) 목표 = 빈 오브젝트 'AsHandTarget' → 휘두르기는 이 목표를 반달 길로 움직임 (칼빛과 같은 길 → 동작 = 이펙트)
#   칼끝 = 오른손에 붙은 단검 꼭짓점 중 손에서 가장 먼 점 → 오른손 뼈 기준 위치를 'tip_local'로 저장
#   결과: Art/models/incoming/hyper3d_assassin/rigged/assassin_rig.blend · 확인 그림 rig_check.png
#   blender -b --factory-startup -P Art/tools/blender_assassin_rig.py
import bpy, math
from mathutils import Vector, Matrix

D = r"C:/Project/Cat_Tang/Art/models/incoming/hyper3d_assassin/rigged"
H_TARGET = 0.8

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=D + "/base_shaded.fbx")
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
arm.name = "AsRig"

# 1) 세우기: 위 = -Y → +Z (X축 -90°), 루트(아마추어 · 부모 없는 것)에 곱한 뒤 적용
roots = [o for o in bpy.data.objects if o.parent is None]
rot = Matrix.Rotation(math.radians(90), 4, 'Z') @ Matrix.Rotation(math.radians(-90), 4, 'X')   # 세운 뒤 앞(-X였음)을 -Y로 (네 방향 렌더로 확인)
for o in roots:
    o.matrix_world = rot @ o.matrix_world
bpy.context.view_layer.update()

# 2) 크기 · 자리: 메쉬 전체 범위로 키 0.8 · 발 바닥 · XY 가운데
def world_bounds():
    dg = bpy.context.evaluated_depsgraph_get()
    vs = []
    for m in meshes:
        ev = m.evaluated_get(dg); me = ev.to_mesh()
        vs += [ev.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    return mn, mx
mn, mx = world_bounds()
sc = H_TARGET / (mx.z - mn.z)
c = (mn + mx) / 2
fix = Matrix.Translation(Vector((0, 0, 0))) @ Matrix.Scale(sc, 4) @ Matrix.Translation(Vector((-c.x, -c.y, -mn.z)))
for o in roots:
    o.matrix_world = fix @ o.matrix_world
bpy.context.view_layer.update()
for o in roots:
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mn, mx = world_bounds()
print("BOUNDS", [round(x, 3) for x in mn], [round(x, 3) for x in mx])

# 3) 앞 방향 확인: 머리(Head 뼈)가 몸통보다 -Y 쪽이면 앞 = -Y (움츠려 앞으로 숙인 자세)
pb = arm.pose.bones
head = arm.matrix_world @ arm.data.bones["Head"].head_local
hips = arm.matrix_world @ arm.data.bones["Hips"].head_local
print("HEAD", [round(x, 3) for x in head], "HIPS", [round(x, 3) for x in hips])

# 4) 칼끝: 오른손 · 손가락 뼈 가중치가 큰 꼭짓점 중 손목에서 가장 먼 점
rh_names = {b.name for b in arm.data.bones if b.name.startswith("RightHand")}
model = max(meshes, key=lambda m: len(m.data.vertices))
gidx = {g.index for g in model.vertex_groups if g.name in rh_names}
wrist = arm.matrix_world @ arm.data.bones["RightHand"].head_local
best, bd = None, 0
for v in model.data.vertices:
    w = sum(g.weight for g in v.groups if g.group in gidx)
    if w < 0.6:
        continue
    p = model.matrix_world @ v.co
    d = (p - wrist).length
    if d > bd:
        bd, best = d, p
hand_m = arm.matrix_world @ arm.data.bones["RightHand"].matrix_local
tip_local = hand_m.inverted() @ best
arm["tip_local"] = list(tip_local)
print("TIP", [round(x, 3) for x in best], "dist", round(bd, 3))

# 5) 오른손 IK: 아래팔에 IK(2마디) → 목표 빈 오브젝트
tgt = bpy.data.objects.new("AsHandTarget", None); bpy.context.scene.collection.objects.link(tgt)
tgt.location = arm.matrix_world @ arm.data.bones["RightHand"].head_local
ik = pb["RightForeArm"].constraints.new('IK'); ik.target = tgt; ik.chain_count = 2
arm["rest_hand"] = list(tgt.location)

bpy.ops.wm.save_as_mainfile(filepath=D + "/assassin_rig.blend")

# 확인 그림: 정면 · 옆
sc_ = bpy.context.scene
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc_.collection.objects.link(cam); sc_.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 1.4
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); sc_.collection.objects.link(sun); sun.rotation_euler = (0.8, 0.2, 0.6)
w = bpy.data.worlds.new("w"); sc_.world = w; w.color = (0.8, 0.8, 0.8)
sc_.render.resolution_x = sc_.render.resolution_y = 400
mk = bpy.data.objects.new("tipmark", None); mk.empty_display_type = 'SPHERE'; mk.location = best; sc_.collection.objects.link(mk)
bm = bpy.data.meshes.new("dot"); import bmesh
b_ = bmesh.new(); bmesh.ops.create_icosphere(b_, subdivisions=2, radius=0.03); b_.to_mesh(bm); b_.free()
dot = bpy.data.objects.new("dot", bm); dot.location = best; sc_.collection.objects.link(dot)
dm = bpy.data.materials.new("red"); dm.use_nodes = True; dm.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1, 0, 0, 1); dm.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 5; dot.data.materials.append(dm)
for tag, loc in (("front", (0, -4, 0.4)), ("side", (4, 0, 0.4))):
    cam.location = loc; cam.rotation_euler = (Vector((0, 0, 0.4)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    sc_.render.filepath = D + f"/rig_check_{tag}.png"; bpy.ops.render.render(write_still=True)
print("DONE")
