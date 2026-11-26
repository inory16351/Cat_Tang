# 4장 쌍둥이 보스 '멈춘 톱니' · '순서대로 명중' 과녁: 공중에 뜬 세운 톱니 (2026-11-26 사용자: 배지만 떠서 무엇을 맞히는지 모름)
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_floatgear.py
# 결과: Game/Assets/Art/Characters/Ch4/Kit_FloatGear.fbx (지름 1.8m, 두께 0.36m, 앞면 = +Y, 원점 = 바닥 가운데)
#  → Unity 메뉴 CatTang/4장 공중 톱니만 만들기 → Resources/Kit/Ch4/FloatGear.prefab (Ch4Kit FloatGear가 띄워 돌림)
# 재질 = blender_kit_ch4_props.py와 같은 역할 이름(회색) → ModelPalette.Ch4가 색을 입힘
import math
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy  # noqa: E402
import blender_kit_ch4_props as K  # noqa: E402

TAU = math.tau


def float_gear():
    start = len(K.PARTS)
    K.gear('Twelve brass teeth', .70, .86, .52, .26, 12, 'brass')
    K.ring('Dark brass rim', .72, .16, -.15, .30, 'brass_dark', n=48)
    for j in range(6):
        a = TAU * j / 6
        K.box('Six steel spokes', (.32 * math.cos(a), .32 * math.sin(a), 0), (.52, .10, .18), 'steel', rot=(0, 0, a))
    for side in (-1, 1):
        K.cylinder('Gold hub', (0, 0, side * .14), .20, .10, 'gold', n=20, bevel=True)
        K.cylinder('Red jewel axle', (0, 0, side * .2), .10, .05, 'gem_red', n=12)
    for j in range(12):
        a = TAU * (j + .5) / 12
        K.cylinder('Rim rivet', (.62 * math.cos(a), .62 * math.sin(a), .16), .035, .04, 'gold', n=8)
        K.cylinder('Rim rivet back', (.62 * math.cos(a), .62 * math.sin(a), -.16), .035, .04, 'gold', n=8)
    # 세움: 바퀴 축(Z)을 앞(+Y)으로 — 앞면이 카메라 쪽
    for obj in K.PARTS[start:]:
        K.place(obj, (0, 0, .9), (math.pi / 2, 0, 0))


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    K.materials()
    K.OUTPUT.mkdir(parents=True, exist_ok=True)
    K.PARTS = []
    random.seed(44017)
    float_gear()
    K.finish('FloatGear', (1.8, .36, 1.8), 5000)
    print('DONE', flush=True)


main()
