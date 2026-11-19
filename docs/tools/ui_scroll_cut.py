# 스크롤바 그림 (Codex, 마젠타 바탕, prompts/scrollbar.txt) → Resources/UI (2026-11-19)
#  scroll_track.png  = 왼쪽 홈 (위아래 놋쇠 마개, 가운데는 늘림 — 9분할 테두리는 ResourceTextureImport)
#  scroll_handle.png = 오른쪽 손잡이에서 가운데 손잡이 줄무늬를 지운 것 (늘려도 줄무늬가 안 뭉개지게)
#  scroll_grip.png   = 손잡이 줄무늬 조각 (GameUI가 손잡이 가운데에 얹음)
# 실행: python docs/tools/ui_scroll_cut.py docs/images/ui_src
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI')
SCALE = 0.5


def keyed(img):
    return Image.fromarray(key_magenta(np.array(img.convert('RGBA'))))


def save(img, name):
    w, h = img.size
    img = img.resize((max(1, round(w * SCALE)), max(1, round(h * SCALE))), Image.LANCZOS)
    img.save(os.path.join(OUT, name))
    print(name, img.size)


if __name__ == '__main__':
    src = Image.open(os.path.join(sys.argv[1], 'scrollbar.png'))
    W, H = src.size
    track = keyed(src.crop((0, 0, W // 2, H)))
    track = track.crop(track.getbbox())
    save(track, 'scroll_track.png')

    handle = keyed(src.crop((W // 2, 0, W, H)))
    handle = handle.crop(handle.getbbox())
    a = np.array(handle)
    h, w = a.shape[:2]
    # 줄무늬 = 가운데 근처에서 어두운 픽셀이 바깥 테두리 말고 안쪽에 있는 줄
    inner = a[:, w // 4: 3 * w // 4, :3].astype(int).sum(axis=2)
    dark_rows = [y for y in range(h // 4, 3 * h // 4) if (inner[y] < 250).sum() > (w // 2) * 0.3]
    g0, g1 = min(dark_rows) - 6, max(dark_rows) + 7
    grip = Image.fromarray(a[g0:g1].copy())
    plain = a.copy()
    ref = a[g0 - 10]                                    # 줄무늬 바로 위의 깨끗한 한 줄로 덮음
    for y in range(g0, g1):
        plain[y] = ref
    save(Image.fromarray(plain), 'scroll_handle.png')
    save(grip, 'scroll_grip.png')
