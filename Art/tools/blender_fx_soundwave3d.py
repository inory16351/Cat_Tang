# 4장 종지기 음파 고리용 3D 조각 (2026-11-19 사용자: 파동 날리는 원도 이미지랑 블렌더로 이펙트 제대로)
#   "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx_soundwave3d.py
# 결과: Game/Assets/Resources/Kit/Ch4/FX_SoundSeg.fbx — 음파 고리 한 조각 (Unity SoundRing이 둘레를 따라 36개를 늘어놓음)
#   - 크기 1 x 1: 블렌더 X = 둘레 방향(-0.5 ~ 0.5), Y = 고리 띠를 가로지르는 방향(-0.5 안쪽 ~ +0.5 바깥), Z = 높이
#   - 단면: 띠 가운데보다 조금 바깥(물결이 나아가는 쪽)이 높이 0.22로 솟은 물마루, 양 끝은 바닥(0.02)으로 내려앉음 → 위에서 보면 띠, 옆에서 보면 솟은 파동
#   - 둘레 방향 양 끝이 살짝 아래로 휨(조각끼리 이어지는 자리가 끊겨 보이지 않게)
#   - UV: U = 둘레 0 ~ 1/3 (음파 띠 그림의 물결 하나), V = 0 안쪽 ~ 1 바깥 (그림 아래 = 안쪽, 위 = 바깥)
import bpy, bmesh, math, os

OUT = r"C:/Project/Cat_Tang/Game/Assets/Resources/Kit/Ch4"
NU, NV = 10, 12


def height(v):
    """v: 0 안쪽 ~ 1 바깥. 바깥쪽으로 치우친 물마루 (앞으로 밀려가는 파동)"""
    crest = 0.58
    if v < crest:
        t = v / crest
        return 0.02 + 0.2 * (math.sin(t * math.pi / 2) ** 2)
    t = (v - crest) / (1 - crest)
    return 0.02 + 0.2 * (math.cos(t * math.pi / 2) ** 1.5)


bpy.ops.wm.read_factory_settings(use_empty=True)
bm = bmesh.new()
uvl = bm.loops.layers.uv.new("UVMap")
grid = []
for i in range(NU + 1):
    u = i / NU
    x = u - 0.5
    sag = 0.035 * (2 * abs(x)) ** 2          # 둘레 양 끝을 살짝 내림
    row = []
    for j in range(NV + 1):
        v = j / NV
        row.append(bm.verts.new((x, v - 0.5, max(0.0, height(v) - sag))))
    grid.append(row)
# 바닥 면을 따로 두지 않음 (위에서만 보는 판): 아래에서 보이면 안 되니 위 면만
for i in range(NU):
    for j in range(NV):
        f = bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
        for loop, (ii, jj) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))):
            loop[uvl].uv = (ii / NU / 3.0, jj / NV)   # 음파 띠 그림의 물결 하나 = 그림 폭의 1/3 (Codex 그림은 물결 3개)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
# 위를 보게 (노멀 +Z)
for f in bm.faces:
    if f.normal.z < 0:
        f.normal_flip()
me = bpy.data.meshes.new("FX_SoundSeg")
bm.to_mesh(me); bm.free()
for p in me.polygons:
    p.use_smooth = True
ob = bpy.data.objects.new("FX_SoundSeg", me)
bpy.context.scene.collection.objects.link(ob)
ob.select_set(True)
bpy.context.view_layer.objects.active = ob
os.makedirs(OUT, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "FX_SoundSeg.fbx"), use_selection=True, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'},
                         use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
print("EXPORTED FX_SoundSeg", len(me.polygons))
print("DONE")
