# 5장 바닥 텍스처 v4 — 청크 (2026-11-20 사용자: 어지러운 패턴을 한 그림에 섞으니 가시성만 떨어짐 → 패턴 하나씩 담은 바닥 여러 장을 불규칙한 청크로 섞음)
#   python Art/tools/ground_texture_ch5.py → Game/Assets/Art/Env/Ch5/ground_ch5_<a~f>.png (1024)
# - 원본: Codex 그림 Art/env_src/ch5/chunks/ground_ch5_<키>.png (프롬프트 Art/tools/specs/ground_ch5_chunk_<키>.txt, 공통 _ground_ch5_chunk_common.txt)
#   a 판석 (바탕 = _BaseMap) · b 대리석 체커 · c 판자 · d 역 타일 · e 시계판 석판 · f 자갈
# - 게임에서는 셰이더 CatTang/RiftFloor가 8m → 4m → 2m로 무작위로 쪼갠 네모 칸마다 한 장을 골라 90° 회전 · 위치를 바꿔 깔고, 칸 경계에 가는 금 균열
# - 예전 원본: v3 초현실 패치워크 한 장 = ground_ch5_surreal_codex.png, v2 깨진 판석 = ground_ch5_broken_codex.png, 4×4 체커 = ground_ch5_gpt.png
import os
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
# v6 (2026-11-20 사용자: 바닥 · 벽 재구성, 그리니치 천문대 컨셉 — 시간은 유지): Codex 새 바닥 6장 (프롬프트 Art/tools/specs/ground_ch5_obs_<키>.txt)
#   a 남색 판석 · b 남색 대리석 체커 · c 푸른 헤링본 마루 · d 별자리 타일 · e 24시 문자판 석판 · f 천구 좌표 격자. 이미 푸른 계열이라 색 돌리기 안 함
SRC = os.path.join(ROOT, 'Art', 'env_src', 'ch5', 'observatory')
RECOLOR = False
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Env', 'Ch5')
KEYS = ['a_slate', 'b_marble', 'c_parquet', 'd_starchart', 'e_dial', 'f_grid']   # 예전 보라 세트(chunks/): a_flagstone · b_checker · c_planks · d_station · e_clock · f_cobble
# 배색 (2026-11-20 사용자: 보라색이 별로 → 푸른 계열로 더 몽환적 · 신비롭게): 그림마다 색상환 가운데 값을 목표 색상으로 돌림(그림 안 색 차이는 그대로)
TARGET_HUE = {'a': 214, 'b': 222, 'c': 200, 'd': 208, 'e': 226, 'f': 218}   # 남청 · 청회 · 청록빛 회색 사이
SAT_MUL = 0.95
HUE_KEEP = 0.35


def to_blue(im, key):
    hsv = np.asarray(im.convert('HSV')).astype(np.float32)
    h = hsv[..., 0] * 360 / 255
    # 가운데 색상 (원형 평균)
    ang = np.deg2rad(h)
    mid = np.rad2deg(np.arctan2(np.sin(ang).mean(), np.cos(ang).mean())) % 360
    dev = (h - mid + 180) % 360 - 180                                   # 가운데에서 벗어난 정도
    h = (TARGET_HUE[key] + dev * HUE_KEEP) % 360                         # 색 차이를 줄여 푸른 계열로 모음 (보라 · 초록 얼룩 X)
    hsv[..., 0] = h * 255 / 360
    hsv[..., 1] = np.clip(hsv[..., 1] * SAT_MUL, 0, 255)
    return Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB')
os.makedirs(OUT, exist_ok=True)

for k in KEYS:
    src = os.path.join(SRC, f'ground_ch5_{k}.png')
    if not os.path.exists(src):
        print('없음', src)
        continue
    im = Image.open(src).convert('RGB')
    if im.size != (1024, 1024):
        im = im.resize((1024, 1024), Image.LANCZOS)
    if RECOLOR:
        im = to_blue(im, k[0])
    dst = os.path.join(OUT, f'ground_ch5_{k[0]}.png')
    im.save(dst)
    print(dst, im.size)
