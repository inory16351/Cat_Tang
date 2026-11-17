# 조준 커서 · 레벨 업 글자 (Codex, 마젠타 바탕) → Resources/UI
#  cursor_target.png → UI/cursor_target.png (놋쇠 조준기 커서, 가운데 맞춰 정사각, 256) + UI/cursor_target_fill.png · cursor_target_edge.png (반전 조준선용: 안쪽 칠 흰색 / 외곽선 검정) · cursor_target_dot.png (가운데 빨간 마름모)   프롬프트 prompts/cursor_target.txt
#  level_up_text.png → UI/level_up_text.png ('LEVEL UP' 글자, 내용 상자, 가로 1024 이하) 프롬프트 prompts/level_up_text.txt
#  bullet_hole.png   → FX/bullet_hole.png   (총알 비 탄자국, 정사각 128)                  프롬프트 prompts/bullet_hole.txt
# 실행: python docs/tools/ui_cursor_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')


def keyed(path):
    return Image.fromarray(key_magenta(np.array(Image.open(path).convert('RGBA'))))


if __name__ == '__main__':
    src = sys.argv[1]
    p = os.path.join(src, 'cursor_target.png')
    if os.path.exists(p):
        im = keyed(p)
        l, t, r, b = im.getbbox()
        cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2
        im = im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))
        im.thumbnail((256, 256), Image.LANCZOS)
        im.save(os.path.join(OUT, 'cursor_target.png')); print('cursor', im.size)
        # 반전 조준선용 두 장 (AimCursor · CatTang/UIInvert): fill = 안쪽 칠만 순수 흰색(밑색 반전), edge = 외곽선만 검정(그냥 그림)
        a = np.array(im).astype(np.float32)
        lum = (a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114) / 255
        alpha = a[..., 3]
        # 가운데 빨간 마름모 (반전 안 하고 늘 빨강, 2026-11-17 사용자): 진홍 픽셀 + 그 안 흰 반짝임까지, 가운데 칸 안에서만
        H, W = lum.shape
        yy, xx = np.mgrid[0:H, 0:W]
        center = (np.abs(xx - W / 2) < W * 0.09) & (np.abs(yy - H / 2) < H * 0.09)
        red = (a[..., 0] > 140) & (a[..., 1] < 110) & (a[..., 2] < 120)
        dot = center & (red | (lum > 0.75)) & (alpha > 20)
        fill = np.clip((lum - 0.2) / 0.12, 0, 1) * alpha * (~dot)   # 밝은 칠 (명암 띠 없이 한 덩어리), 마름모 빼고
        dot_img = a.copy(); dot_img[..., 3] = np.where(dot, alpha, 0)
        Image.fromarray(dot_img.astype(np.uint8)).save(os.path.join(OUT, 'cursor_target_dot.png'))
        edge = np.clip((0.3 - lum) / 0.12, 0, 1) * alpha            # 어두운 외곽선
        w = np.full(lum.shape, 255, np.float32); k = np.full(lum.shape, 12, np.float32)
        Image.fromarray(np.dstack([w, w, w, fill]).astype(np.uint8)).save(os.path.join(OUT, 'cursor_target_fill.png'))
        Image.fromarray(np.dstack([k, k, k, edge]).astype(np.uint8)).save(os.path.join(OUT, 'cursor_target_edge.png'))
        print('cursor fill / edge / dot')
    p = os.path.join(src, 'level_up_text.png')
    if os.path.exists(p):
        im = keyed(p)
        im = im.crop(im.getbbox())
        im.thumbnail((1024, 1024), Image.LANCZOS)
        im.save(os.path.join(OUT, 'level_up_text.png')); print('level_up', im.size)
    p = os.path.join(src, 'bullet_hole.png')
    if os.path.exists(p):
        im = keyed(p)
        l, t, r, b = im.getbbox()
        cx, cy, half = (l + r) / 2, (t + b) / 2, max(r - l, b - t) / 2
        im = im.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))
        im.thumbnail((128, 128), Image.LANCZOS)
        im.save(os.path.join(OUT, '..', 'FX', 'bullet_hole.png')); print('bullet_hole', im.size)
