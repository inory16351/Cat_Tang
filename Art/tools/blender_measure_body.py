# 리깅된 캐릭터 FBX의 신체 비율 측정 — 뼈 높이 + 뼈별(가중치 최대) 메쉬 덩어리 크기
# 뼈 이름은 mixamorig: 접두어를 떼고 비교 (원본 CatHero = Mixamo, Hyper3D = 접두어 없음)
# 사용: blender -b --factory-startup -P blender_measure_body.py -- <out.json> <a.fbx> [<b.fbx> ...]
import bpy, sys, json
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_path, files = argv[0], argv[1:]

GROUPS = {   # 뼈 이름 → 부위
    "head": ["Head", "HeadTop_End", "Neck"],
    "torso": ["Hips", "Spine", "Spine1", "Spine2"],
    "upleg": ["LeftUpLeg", "RightUpLeg"],
    "leg": ["LeftLeg", "RightLeg"],
    "foot": ["LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase", "LeftToe_End", "RightToe_End"],
    "uparm": ["LeftArm", "RightArm", "LeftShoulder", "RightShoulder"],
    "forearm": ["LeftForeArm", "RightForeArm"],
}

def pct(vals, p):
    s = sorted(vals); return s[min(len(s) - 1, max(0, int(p * (len(s) - 1))))]

res = []
for path in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    allp = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    zmin = min(p.z for p in allp); zmax = max(p.z for p in allp)
    H = zmax - zmin
    bone = {}
    for b in arm.data.bones:
        n = b.name.split(":")[-1]
        h = arm.matrix_world @ b.head_local; t = arm.matrix_world @ b.tail_local
        bone[n] = (h.x, h.y, h.z - zmin, t.x, t.y, t.z - zmin)
    # 가중치 최대 뼈로 정점 나눔
    part = {k: [] for k in GROUPS}
    owner = {n: k for k, ns in GROUPS.items() for n in ns}
    for o in meshes:
        names = {g.index: g.name.split(":")[-1] for g in o.vertex_groups}
        for v in o.data.vertices:
            if not v.groups: continue
            g = max(v.groups, key=lambda g: g.weight)
            k = owner.get(names.get(g.group, ""))
            if k: part[k].append(o.matrix_world @ v.co)
    dims = {}
    for k, ps in part.items():
        if len(ps) < 20: continue
        xs = [p.x for p in ps]; ys = [p.y for p in ps]; zs = [p.z - zmin for p in ps]
        dims[k] = {"n": len(ps), "w": pct(xs, .98) - pct(xs, .02), "d": pct(ys, .98) - pct(ys, .02),
                   "z0": pct(zs, .01), "z1": pct(zs, .99)}
    # 머리 폭: 머리 뼈 시작 ~ +머리길이 40% 띠 (모자 챙 아래 뺨 · 볼)
    hz = bone["Head"][2]
    band = [p for p in part["head"] if hz <= p.z - zmin <= hz + 0.4 * (H - hz)]
    if band:
        xs = [p.x for p in band]
        dims["cheek_w"] = pct(xs, .97) - pct(xs, .03)
    # 몸 전체 폭 (어깨 높이 띠)
    sz = bone["LeftArm"][2]
    band = [p for p in allp if abs(p.z - zmin - sz) < 0.03 * H]
    xs = [p.x for p in band]; dims["shoulder_band_w"] = pct(xs, .98) - pct(xs, .02)
    def L(a, b):  # 두 뼈 머리 사이 거리
        return (Vector(bone[a][:3]) - Vector(bone[b][:3])).length
    seg = {
        "H": H,
        "crotch": (bone["LeftUpLeg"][2] + bone["RightUpLeg"][2]) / 2,
        "knee": (bone["LeftLeg"][2] + bone["RightLeg"][2]) / 2,
        "ankle": (bone["LeftFoot"][2] + bone["RightFoot"][2]) / 2,
        "hips": bone["Hips"][2], "neck": bone["Neck"][2], "headb": bone["Head"][2],
        "thigh": (L("LeftUpLeg", "LeftLeg") + L("RightUpLeg", "RightLeg")) / 2,
        "shin": (L("LeftLeg", "LeftFoot") + L("RightLeg", "RightFoot")) / 2,
        "spine": L("Hips", "Neck"), "neckl": L("Neck", "Head"),
        "uparm": (L("LeftArm", "LeftForeArm") + L("RightArm", "RightForeArm")) / 2,
        "forearm": (L("LeftForeArm", "LeftHand") + L("RightForeArm", "RightHand")) / 2,
        "shoulder_w": abs(bone["LeftArm"][0] - bone["RightArm"][0]),
        "hip_w": abs(bone["LeftUpLeg"][0] - bone["RightUpLeg"][0]),
    }
    res.append({"file": path, "seg": seg, "dims": dims, "bones": {k: v for k, v in bone.items() if "Hand" not in k or k.endswith("Hand")}})
json.dump(res, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("MEASURED", len(res))
