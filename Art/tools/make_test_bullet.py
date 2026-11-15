# Blender 배치: 셰이더 테스트용 리볼버 총알 메시(탄피 + 테두리 홈 + 탄두 + 장식 링)를 만들어 FBX로 내보낸다
# 실행: blender -b -P make_test_bullet.py -- <out.fbx>
import bpy, bmesh, sys, math

out = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)

def lathe(name, profile, segs=48):
    """profile: [(r, z), ...] 아래에서 위로. z축 기준으로 돌려 만든다."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    verts = [bm.verts.new((r, 0, z)) for r, z in profile]
    edges = [bm.edges.new((verts[i], verts[i + 1])) for i in range(len(verts) - 1)]
    bmesh.ops.spin(bm, geom=verts + edges, cent=(0, 0, 0), axis=(0, 0, 1), angle=math.tau, steps=segs, use_duplicate=False)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for p in me.polygons:
        p.use_smooth = True
    return ob

# 단위: 1 = 총알 길이 약 1
R = 0.16
case = lathe("Case", [
    (0.0, 0.0), (R * 1.08, 0.0), (R * 1.08, 0.05),  # 바닥 테두리(림)
    (R * 0.92, 0.07), (R * 0.92, 0.09),              # 테두리 홈
    (R, 0.11), (R, 0.58), (R * 0.97, 0.60),          # 탄피 몸통
])
head_profile = [(R * 0.97, 0.60)]
for i in range(1, 13):  # 오지브 탄두
    t = i / 12
    head_profile.append((R * 0.97 * math.cos(t * math.pi / 2) ** 0.8, 0.60 + 0.42 * math.sin(t * math.pi / 2)))
head_profile[-1] = (0.0, 1.02)
head = lathe("Head", head_profile)
ring = lathe("Ring", [(R * 1.0, 0.36), (R * 1.12, 0.37), (R * 1.12, 0.45), (R * 1.0, 0.46)])

# 재질 슬롯 3개 (탄피 · 탄두 · 링) → Unity에서 색만 바꿔 쓴다
for ob, mat in ((case, "M_Case"), (head, "M_Head"), (ring, "M_Ring")):
    m = bpy.data.materials.get(mat) or bpy.data.materials.new(mat)
    ob.data.materials.append(m)
bpy.ops.object.select_all(action="DESELECT")
for ob in (case, head, ring):
    ob.select_set(True)
bpy.context.view_layer.objects.active = case
bpy.ops.object.join()
case.name = "TestBullet"
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", out, len(case.data.polygons), "faces")
