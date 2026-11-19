# 메인 로비 그림 (Codex CLI — 프롬프트 docs/images/ui_src/prompts/lobby_bg.txt · lobby_ui.txt) → Resources/UI/Lobby
#  lobby_key.png    → key.png      (주인공이 있는 키 일러스트, 가운데 16:9로 잘라 1920x1080 — 있으면 로비가 이걸 씀)
#  lobby_bg.png     → bg.png       (가운데 16:9로 잘라 1920x1080)
#  lobby_fg.png     → fg.png       (마젠타 빼고, 같은 16:9 자르기 → 1920x1080, 앞 시차 층)
#  title_1159.png   → title.png    (마젠타 빼고 내용 상자로, 가로 1400 이하)
#  lobby_button.png → button.png · button_hover.png (위 · 아래 반쪽, 내용 상자, 가로 900 이하)
#  slot_card.png    → slot_card.png (내용 상자, 세로 900 이하)
#  lobby_icons.png  → icon_play / icon_new / icon_delete / icon_quit (4칸, 160 이하)
#  (2차, 일러스트에 맞춘 그림 — prompts/lobby_ui2.txt, 있으면 1차를 덮어씀)
#  title2_1159.png  → title.png
#  menu_button.png  → menu_button.png · menu_button_hover.png
#  panel_frame.png  → panel.png
#  slot_card2.png   → slot_card.png
#  ui_parts.png     → slider_track / slider_knob / check_off / check_on / icon_back / icon_delete (6칸)
#  (3차, 2026-11-18 사용자 "플랫하게 뽑은 뒤 코딩으로 효과" — prompts/lobby_flat.txt, 있으면 위를 덮어씀. 효과는 UIFlatFx 셰이더)
#  flat_title.png → title.png · flat_button.png → menu_button.png (hover 그림 없음, 셰이더 _Hover) · flat_panel.png → panel.png
#  flat_card.png → slot_card.png · flat_parts.png → slider_track / slider_knob / check_off / check_on / icon_back / icon_delete
# 실행: python docs/tools/ui_lobby_cut.py docs/images/ui_src  (그 다음 python docs/tools/ui_lobby_ink.py)
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from fx_sheet_cut import key_magenta  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Assets', 'Resources', 'UI', 'Lobby')


def keyed(im):
    return Image.fromarray(key_magenta(np.array(im.convert('RGBA'))))


def crop169(im):
    w, h = im.size
    th = int(w * 9 / 16)
    if th > h:
        tw = int(h * 16 / 9)
        x = (w - tw) // 2
        return im.crop((x, 0, x + tw, h))
    y = (h - th) // 2
    return im.crop((0, y, w, y + th))


def trim(im, maxsize):
    im = im.crop(im.getbbox())
    im.thumbnail(maxsize, Image.LANCZOS)
    return im


def save(im, name):
    im.save(os.path.join(OUT, name))
    print(name, im.size)


if __name__ == '__main__':
    src = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)
    p = lambda n: os.path.join(src, n)
    if os.path.exists(p('lobby_key.png')):
        save(crop169(Image.open(p('lobby_key.png')).convert('RGB')).resize((1920, 1080), Image.LANCZOS), 'key.png')
    if os.path.exists(p('lobby_bg.png')):
        save(crop169(Image.open(p('lobby_bg.png')).convert('RGB')).resize((1920, 1080), Image.LANCZOS), 'bg.png')
    if os.path.exists(p('lobby_fg.png')):
        save(crop169(keyed(Image.open(p('lobby_fg.png')))).resize((1920, 1080), Image.LANCZOS), 'fg.png')
    if os.path.exists(p('title_1159.png')):
        save(trim(keyed(Image.open(p('title_1159.png'))), (1400, 900)), 'title.png')
    if os.path.exists(p('lobby_button.png')):
        im = keyed(Image.open(p('lobby_button.png')))
        w, h = im.size
        save(trim(im.crop((0, 0, w, h // 2)), (900, 300)), 'button.png')
        save(trim(im.crop((0, h // 2, w, h)), (900, 300)), 'button_hover.png')
    if os.path.exists(p('slot_card.png')):
        save(trim(keyed(Image.open(p('slot_card.png'))), (700, 900)), 'slot_card.png')
    if os.path.exists(p('lobby_icons.png')):
        im = keyed(Image.open(p('lobby_icons.png')))
        w, h = im.size
        for i, n in enumerate(['icon_play', 'icon_new', 'icon_delete', 'icon_quit']):
            save(trim(im.crop((w * i // 4, 0, w * (i + 1) // 4, h)), (160, 160)), n + '.png')

    # ---------- 2차 ----------
    if os.path.exists(p('title2_1159.png')):
        save(trim(keyed(Image.open(p('title2_1159.png'))), (1400, 900)), 'title.png')
    if os.path.exists(p('menu_button.png')):
        im = keyed(Image.open(p('menu_button.png')))
        w, h = im.size
        save(trim(im.crop((0, 0, w, h // 2)), (900, 300)), 'menu_button.png')
        save(trim(im.crop((0, h // 2, w, h)), (900, 300)), 'menu_button_hover.png')
    if os.path.exists(p('panel_frame.png')):
        save(trim(keyed(Image.open(p('panel_frame.png'))), (1500, 1000)), 'panel.png')
    if os.path.exists(p('slot_card2.png')):
        save(trim(keyed(Image.open(p('slot_card2.png'))), (700, 900)), 'slot_card.png')
    if os.path.exists(p('ui_parts.png')):
        im = keyed(Image.open(p('ui_parts.png')))
        w, h = im.size
        for i, n in enumerate(['slider_track', 'slider_knob', 'check_off', 'check_on', 'icon_back', 'icon_delete']):
            save(trim(im.crop((w * i // 6, 0, w * (i + 1) // 6, h)), (256, 256)), n + '.png')

    # ---------- 3차: 플랫 (효과는 셰이더) ----------
    ft = p('flat_title_dots.png') if os.path.exists(p('flat_title_dots.png')) else p('flat_title.png')   # 콜론 = 점 두 개 (사용자: 시계 빼고 점으로)
    if os.path.exists(ft):
        save(trim(keyed(Image.open(ft)), (1400, 900)), 'title.png')
    if os.path.exists(p('flat_button.png')):
        save(trim(keyed(Image.open(p('flat_button.png'))), (1000, 300)), 'menu_button.png')
        hv = os.path.join(OUT, 'menu_button_hover.png')
        for f in (hv, hv + '.meta'):
            if os.path.exists(f): os.remove(f)
    if os.path.exists(p('flat_panel.png')):
        save(trim(keyed(Image.open(p('flat_panel.png'))), (1500, 1000)), 'panel.png')
    if os.path.exists(p('flat_card.png')):
        save(trim(keyed(Image.open(p('flat_card.png'))), (700, 1000)), 'slot_card.png')
    if os.path.exists(p('flat_parts.png')):
        im = keyed(Image.open(p('flat_parts.png')))
        w, h = im.size
        for i, n in enumerate(['slider_track', 'slider_knob', 'check_off', 'check_on', 'icon_back', 'icon_delete']):
            save(trim(im.crop((w * i // 6, 0, w * (i + 1) // 6, h)), (256, 256)), n + '.png')
