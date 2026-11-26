# 4장 스토리 맵 '오르골 톱니' 숫자 배지 테두리 — 맵 황동 톱니(W_Gear)와 같은 결의 톱니 고리 그림
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_ui_gear_badge.py
# 결과: Game/Assets/Resources/UI/badge/badge_gear.png (512², 투명 배경, 가운데 = 숫자 자리 어두운 법랑)
import math
from pathlib import Path

import bmesh
import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Game/Assets/Resources/UI/badge/badge_gear.png'
TEETH, R_TIP, R_ROOT, R_RIM_IN, R_FACE = 14, 1.0, 0.84, 0.66, 0.62

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def mat(name, color, metal=0.0, rough=0.5, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    if emit:
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = emit
    return m


BRASS = mat('brass', (0.78, 0.52, 0.17), 0.9, 0.32)
BRASS_DK = mat('brass_dark', (0.36, 0.2, 0.06), 0.8, 0.45)
ENAMEL = mat('enamel', (0.03, 0.015, 0.012), 0.0, 0.6)
GOLD = mat('gold_line', (1.0, 0.78, 0.32), 1.0, 0.2, 0.25)


def solid(name, outline, holes, z0, z1, material, bevel=0.012):
    bm = bmesh.new()
    def ring(pts, z):
        return [bm.verts.new((x, y, z)) for x, y in pts]
    # 바깥 + 안쪽 구멍을 브리지: 같은 점 수로 맞춰 링 두 개를 잇는 판
    top_o, bot_o = ring(outline, z1), ring(outline, z0)
    n = len(outline)
    if holes:
        top_i, bot_i = ring(holes, z1), ring(holes, z0)
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((top_o[i], top_o[j], top_i[j], top_i[i]))
            bm.faces.new((bot_o[j], bot_o[i], bot_i[i], bot_i[j]))
            bm.faces.new((top_i[i], top_i[j], bot_i[j], bot_i[i]))
    else:
        bm.faces.new(top_o)
        bm.faces.new(list(reversed(bot_o)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot_o[i], bot_o[j], top_o[j], top_o[i]))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    ob.data.materials.append(material)
    if bevel:
        mod = ob.modifiers.new('bev', 'BEVEL')
        mod.width, mod.segments, mod.limit_method = bevel, 2, 'ANGLE'
    return ob


def circle(r, n):
    return [(r * math.cos(t * math.tau / n), r * math.sin(t * math.tau / n)) for t in range(n)]


# 톱니 윤곽: 이빨마다 8점 (뿌리 → 경사 → 끝 평평 → 경사), 점 수를 원 링과 같게
per = 8
pts = []
for k in range(TEETH):
    base = k * math.tau / TEETH
    step = math.tau / TEETH
    prof = [(0.00, R_ROOT), (0.14, R_ROOT), (0.27, R_TIP), (0.36, R_TIP),
            (0.50, R_TIP), (0.63, R_TIP), (0.76, R_ROOT), (0.88, R_ROOT)]
    for f, r in prof:
        a = base + f * step + math.pi / 2
        pts.append((r * math.cos(a), r * math.sin(a)))
N = len(pts)


def same_angle(r):
    # 톱니 윤곽 점과 같은 각도의 원 (브리지 면이 꼬이지 않게)
    return [(r * x / math.hypot(x, y), r * y / math.hypot(x, y)) for x, y in pts]


solid('GearRing', pts, same_angle(R_RIM_IN), 0.0, 0.16, BRASS, 0.0)
solid('GearInnerLip', circle(R_RIM_IN + 0.005, 96), circle(R_FACE, 96), 0.0, 0.13, BRASS_DK, 0.0)
solid('Face', circle(R_FACE + 0.01, 96), None, 0.0, 0.08, ENAMEL, 0.0)
solid('GoldLine', circle(R_FACE - 0.035, 96), circle(R_FACE - 0.06, 96), 0.08, 0.095, GOLD, 0.0)
# 이빨 사이 리벳 (황동 고리 위 작은 점)
for k in range(TEETH):
    a = (k + 0.5) * math.tau / TEETH + math.pi / 2
    r = (R_ROOT + R_RIM_IN) / 2
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.028, location=(r * math.cos(a), r * math.sin(a), 0.16), segments=12, ring_count=6)
    bpy.context.object.data.materials.append(GOLD)

# 카메라: 정수리 직교, 톱니 끝이 그림 가장자리 살짝 안쪽
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
cam.data.type, cam.data.ortho_scale = 'ORTHO', 2.12
cam.location = (0, 0, 5)
scene.collection.objects.link(cam)
scene.camera = cam

# 조명: 왼쪽 위 키 + 약한 채움 (맵의 따뜻한 램프 결)
key = bpy.data.objects.new('key', bpy.data.lights.new('key', 'SUN'))
key.data.energy, key.data.color = 4.0, (1.0, 0.9, 0.75)
key.rotation_euler = (math.radians(38), math.radians(-28), 0)
fill = bpy.data.objects.new('fill', bpy.data.lights.new('fill', 'SUN'))
fill.data.energy, fill.data.color = 1.2, (0.75, 0.8, 1.0)
fill.rotation_euler = (math.radians(-30), math.radians(25), 0)
for l in (key, fill):
    scene.collection.objects.link(l)
world = bpy.data.worlds.new('w')
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.35, 0.3, 0.25, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.6
scene.world = world

r = scene.render
r.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
r.resolution_x = r.resolution_y = 512
r.film_transparent = True
r.image_settings.file_format = 'PNG'
r.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'Standard'
OUT.parent.mkdir(parents=True, exist_ok=True)
r.filepath = str(OUT)
bpy.ops.render.render(write_still=True)
print('[gear badge]', OUT)
