# 총탄 · 총 기획서 → 2장 해금 반영 새 버전 (2026-11-25)
# - 총탄 Ver03 → Ver04: 미련 특성 개편(공명 사격 강화) · 등급별 종 수 · 2장 해금 탄 6 (여정 / 마무리 묶음) · Unlock 테이블
# - 총   Ver02 → Ver03: 시작 총 고르기 없음 · 총포상 해금 규칙 · 스킬 10종 · 2장 해금 총 2 (가스등 랜턴 / 기관차)
# 근거: Art/tools/specs/ch2_unlock_spec.md (2026-11-25 확정, 가격 · 총 수치 · 탄 / 총 설명은 초안) · Game/Assets/Data/Tables/*.csv
# 옛 버전 pptx(손으로 고친 것 포함)를 읽어 고치고 새 파일로 저장 (생성기 build_item_ppts.js는 Ver01 시절이라 쓰지 않음)
# 아이콘: Game/Assets/Resources/Icons/bullets/bullet_1047~1052.png · guns/gun_2009 · 2010.png 가 있으면 넣고, 없으면 자리 표시
#   → 아이콘이 생기면 이 스크립트를 다시 돌리면 됨
# 실행: python edit_items_ch2_unlock.py   (어느 폴더에서든)
import copy, os, sys, uuid
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lxml import etree
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.shapes import MSO_SHAPE
from pptx_kit import Editor, NS, card, text, bullets, chip, picture, bar, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, _rgb

TOOLS = os.path.dirname(os.path.abspath(__file__))
DOCS = os.path.normpath(os.path.join(TOOLS, ".."))
ICONS = os.path.normpath(os.path.join(DOCS, "..", "Game", "Assets", "Resources", "Icons"))
OUT_DIR = os.environ.get("OUT_DIR", DOCS)
DATE = "2026.11.25"
G = {"Common": ("일반", "9AA0A6"), "Rare": ("희귀", "4F8CE0"), "Epic": ("영웅", "9B5DE5"), "Legendary": ("전설", "E0A63A")}
WRAITH = "6F7CFF"   # 미련 특성 색 (푸른 원혼빛)
missing_icons = []


def icon_or_slot(s, kind, iid, x, y, size, col):
    p = os.path.join(ICONS, kind, f"{kind[:-1]}_{iid}.png")
    if os.path.exists(p):
        picture(s, p, x, y, size, size)
        return True
    missing_icons.append(os.path.relpath(p, os.path.join(DOCS, "..")))
    sh = card(s, x, y, size, size, FLOOR, line=col, line_w=1.25)
    sh.line.dash_style = 4   # 점선
    text(s, x, y, size, size, [{"t": "아이콘\n제작 중", "after": 0}], size=10, color=MUTED, align="c", valign="m", spacing=1.0)
    return False


def new_section_after(prs, after_name, name, slides):
    """after_name 섹션 바로 뒤에 새 섹션을 만들고 slides(슬라이드 객체)를 그 섹션으로 옮김"""
    lst = prs.part._element.find(".//p14:sectionLst", NS)
    ids = {s.slide_id for s in slides}
    for sec in lst:
        for e in list(sec.find("p14:sldIdLst", NS)):
            if int(e.get("id")) in ids:
                e.getparent().remove(e)
    sec = etree.Element(f"{{{NS['p14']}}}section")
    sec.set("name", name); sec.set("id", "{" + str(uuid.uuid4()).upper() + "}")
    sl = etree.SubElement(sec, f"{{{NS['p14']}}}sldIdLst")
    for s in slides:
        etree.SubElement(sl, f"{{{NS['p14']}}}sldId").set("id", str(s.slide_id))
    for old in lst:
        if old.get("name") == after_name:
            old.addnext(sec); return
    raise KeyError(after_name)


def title_of(slide):
    """제목 = 쪽 번호 필드가 없는 첫 자리 표시자"""
    for sh in slide.placeholders:
        if sh.has_text_frame and not sh._element.findall(".//a:fld", NS):
            return sh.text_frame.text
    return ""


def slide_index(prs, title):
    for i, s in enumerate(prs.slides, 1):
        if title_of(s) == title:
            return i
    raise KeyError(title)


def set_cell(cell, paras):
    """셀 문단마다 첫 run만 남기고 글을 바꿈 (서식 유지)"""
    ps = cell.text_frame.paragraphs
    for p, t in zip(ps, paras):
        Editor.set_text(p, t)


def shift_group(shapes, dy=0, dh=None):
    for sh in shapes:
        sh.top = sh.top + Inches(dy)
        if dh is not None:
            sh.height = Inches(dh)


# ====================================================================== 총탄 Ver04
BULLETS = [
    # id, 이름, 등급, 공격, 조건, 특성, 연산, 효과[(태그, 글, 새?)], 설명(초안)
    (1047, "연착", "Rare", 12, "5 미만", "지연 / 미련", 2,
     [("명중", "둔화 30%, 2초", False), ("상시", "둔화된 적에게 공명 피해 +30%", False)], "오지 않는 기차를 기다리며 젖어 가는 시간"),
    (1048, "환승역", "Epic", 14, "짝수", "미련 / 망각", 1,
     [("명중", "시간에 갇힌 적을 맞히면 가둠이 반경 4 안 다른 적에게 1초 옮겨 붙음", True)], "갈아타는 곳마다 붙잡힌 시간이 옮겨 탄다"),
    (1049, "끝나지 않는 여정", "Legendary", 16, "6시", "미련 / 영원", 0,
     [("상시", "활성화된 모든 탄의 공명에 둔화 40%, 1.5초", False), ("상시", "공명 반경 +1", False)], "도착하지 않는 여행의 긴 여운"),
    (1050, "레일 위의 불꽃", "Rare", 12, "3 초과", "종말 / 예지", 2,
     [("치명타", "치명타가 터지면 그 자리 폭발 (피해 50%, 반경 2)", False)], "바퀴가 레일을 긁을 때 튀는 불꽃"),
    (1051, "연기 고리", "Epic", 15, "짝수", "지연 / 종말", 2,
     [("처치", "처치하면 연기 고리 폭발 (피해 80%, 반경 3)", False), ("처치", "다음 탄 피해 +30%", False)], "기관차 굴뚝이 뱉은 둥근 연기"),
    (1052, "마지막 기차", "Legendary", 18, "11시", "종말 / 미련", 0,
     [("상시", "기차처럼 직선으로 돌진하는 굵은 탄: 모든 적 관통, 관통할 때마다 피해 +20%, 끝에서 반경 4 공명 폭발", True)], "놓쳐 버린 막차가 선로를 달려 온다"),
]
BUNDLES = [
    ("여정", "제어", 60, "둔화 / 가둠으로 오래 붙잡아 두는 탄", BULLETS[:3]),
    ("마무리", "화력", 80, "관통 / 폭발로 끝을 내는 탄", BULLETS[3:]),
]


def nb(t):
    """'반경 3' / '피해 80%' / '+30%' 같은 짧은 수치 묶음이 줄 끝에서 갈리지 않게 줄바꿈 없는 빈칸으로"""
    import re
    return re.sub(r"(반경|피해|둔화|폭발) (?=[0-9+])", lambda m: m.group(1) + " ", t)


def bullet_card(s, b, x, y, w, h):
    iid, name, grade, dmg, cond, traits, slots, effs, desc = b
    gname, col = G[grade]
    card(s, x, y, w, h, CARD, line=col, line_w=1.5)
    icon_or_slot(s, "bullets", iid, x + 0.2, y + 0.2, 1.3, col)
    text(s, x + 1.65, y + 0.2, w - 1.8, 0.7, [{"t": name, "after": 0}], size=17, color=col, bold=True, valign="m", spacing=1.0)
    chip(s, gname, x + 1.65, y + 0.98, 0.7, h=0.3, fill=col, color=BG, size=10.5)
    text(s, x + 0.2, y + 1.68, w - 0.4, 0.62, [
        {"runs": [("조건 ", {"c": MUTED}), (cond, {"b": True}), ("   특성 ", {"c": MUTED}), (traits, {"b": True})], "after": 2},
        {"runs": [("공격력 ", {"c": MUTED}), (str(dmg), {"b": True}), ("   연산 슬롯 ", {"c": MUTED}), (str(slots), {"b": True})], "after": 0},
    ], size=12)
    bar(s, x + 0.2, y + 2.38, w - 0.4, 0.015, PURPLE)
    paras = []
    for tag, t, new in effs:
        runs = [(f"[{tag}] ", {"c": GOLD, "b": True})]
        if new:
            runs.append(("[새] ", {"c": TEAL, "b": True}))
        runs.append((nb(t), {}))
        paras.append({"runs": runs, "after": 6})
    text(s, x + 0.2, y + 2.52, w - 0.4, h - 3.55, paras, size=12.5, spacing=1.12)
    text(s, x + 0.2, y + h - 0.92, w - 0.4, 0.75, [
        {"t": f"“{desc}”", "after": 1, "c": TXT},
        {"t": "설명 초안", "after": 0, "c": MUTED, "size": 9.5},
    ], size=11, color=MUTED, valign="b", spacing=1.05)


def bullet_deck():
    src = os.path.join(DOCS, "1159_총탄_기획서_20261118_정승운_Ver03.pptx")
    dst = os.path.join(OUT_DIR, "1159_총탄_기획서_20261125_정승운_Ver04.pptx")
    ed = Editor(Presentation(src))
    prs = ed.prs
    ed.rep(1, "작성 정승운 / 2026.11.17 / 수치는 1차안", f"작성 정승운 / {DATE} / 수치는 1차안")

    # 등급: 종 수에 2장 해금 탄 포함
    n = slide_index(prs, "등급")
    for old, new in (("13종", "15종"), ("10종", "12종"), ("8종", "10종")):
        ed.rep(n, old, new)
    text(prs.slides[n - 1], 0.6, 6.45, 12.1, 0.35, "종 수는 2장 해금 탄 포함 (희귀 / 영웅 / 전설 각 2종). 해금 탄은 탄약 공방에서 묶음을 산 뒤부터 등장",
         size=12, color=MUTED)
    # 탄 목록 제목: 기본 탄 수로 명시
    for i, s in enumerate(prs.slides, 1):
        t = title_of(s)
        for g, c in (("희귀", 13), ("영웅", 10), ("전설", 8)):
            if t.startswith(f"{g} 탄 ({c}종)"):
                ed.rep(i, t, t.replace(f"({c}종)", f"(기본 {c}종)"))

    # 특성 표: 미련 행
    n = slide_index(prs, "특성 9종 (2/2)")
    tbl = [sh for sh in prs.slides[n - 1].shapes if sh.has_table][0].table
    row = [r for r in tbl.rows if r.cells[0].text.startswith("미련")][0]
    set_cell(row.cells[0], ["미련", "공명 사격이 더 세고 넓어지며, 끝내 원혼이 울린다"])
    set_cell(row.cells[1], ["공명 피해 30% → 45%"])
    set_cell(row.cells[2], ["공명 피해 60% / 반경 2 → 2.6"])
    set_cell(row.cells[3], ["공명 0.6초 뒤 그 자리에 푸른 원혼 3~4개가 피어나 울림 (반경 3 / 공명 피해 100%)"])
    ed.log.append("OK   미련 행 교체")

    # 새 슬라이드: 미련 개편
    s = ed.insert_slide_after(n, "", "미련: 공명 사격 강화")
    card(s, 0.6, 1.45, 4.1, 3.55, PURPLE)
    text(s, 0.85, 1.62, 3.6, 0.4, "공명 사격", size=17, color=GOLD, bold=True)
    bullets(s, 0.85, 2.12, 3.65, 2.8, [
        "활성화된 탄을 그 탄의 차례에 직접 쏠 때 발동",
        "맞은 자리에 범위 피해",
        "기본 반경 2 / 피해 30%",
        "굵은 궤적 / 금빛 섬광 / 탄 색 폭발 / 화면 흔들림",
    ], size=14, after=8)
    tiers = [("2단계", "공명 피해 30% → 45%", None),
             ("4단계", "공명 피해 60%\n반경 2 → 2.6", None),
             ("6단계", "공명 0.6초 뒤 그 자리에 푸른 원혼 3~4개가 피어나 울림\n반경 3 / 공명 피해의 100%", "새 효과")]
    for k, (a, b, tag) in enumerate(tiers):
        x = 4.95 + k * 2.62
        card(s, x, 1.45, 2.47, 3.55, CARD, line=WRAITH if k == 2 else CARD, line_w=1.5)
        text(s, x + 0.22, 1.62, 2.0, 0.45, a, size=20, color=WRAITH, bold=True)
        if tag:
            chip(s, tag, x + 1.45, 1.7, 0.85, h=0.3, fill=WRAITH, color=BG, size=10)
        text(s, x + 0.22, 2.3, 2.05, 2.6, b, size=15.5, spacing=1.2)
    card(s, 0.6, 5.2, 12.13, 1.55, FLOOR)
    sw = card(s, 0.85, 5.45, 0.5, 0.5, WRAITH, radius=0.25)
    text(s, 1.55, 5.37, 4.3, 1.3, [
        {"t": "특성 색 = 푸른 원혼빛", "b": True, "c": TXT, "after": 3},
        {"t": "망각(청백) / 영원(청록)과 구분되는 남보라 쪽 푸른빛 (#6F7CFF 계열)", "c": MUTED, "size": 11.5},
    ], size=13)
    text(s, 6.2, 5.37, 6.3, 1.3, [
        {"t": "바뀐 점", "b": True, "c": GOLD, "after": 3},
        {"t": "비활성화 탄 보너스 → 활성화 탄의 공명 강화", "after": 2},
        {"t": "미련을 가진 기존 탄 7발(DEAR / 녹슨 태엽 / 모래시계 / 빛바랜 사진 / 황혼 / 오딘의 눈동자 / 마지막 페이지)은 특성만 그대로", "c": MUTED, "size": 11.5, "after": 0},
    ], size=13)
    s.notes_slide.notes_text_frame.text = "이펙트 FX_LingerWraith: 반경 3 / 0.6초 피어오름 → 울림(퍼지는 파동) → 0.4초 흩어짐, 푸른 원혼 3~4"

    # 새 섹션: 2장 해금 탄 (탄 목록 마지막 쪽 뒤)
    last = max(i for i, sl in enumerate(prs.slides, 1) if title_of(sl).startswith("전설 탄"))
    made = []
    for k, (bn, kind, cost, line, items) in enumerate(BUNDLES):
        s = ed.insert_slide_after(last + k, "", f"탄약 공방 묶음 {k + 1}: {bn} ({kind})")
        made.append(s)
        card(s, 0.6, 1.4, 12.13, 0.62, PURPLE)
        text(s, 0.85, 1.4, 11.7, 0.62, [{"runs": [
            ("묶음 값  ", {"c": GOLD, "b": True}), (f"{cost} 조각", {"b": True}), ("  (초안)", {"c": MUTED, "size": 11}),
            ("        ", {}), (line, {}),
            ("        등급 희귀 / 영웅 / 전설 각 1", {"c": MUTED, "size": 12}),
        ], "after": 0}], size=14, valign="m")
        for i, b in enumerate(items):
            bullet_card(s, b, 0.6 + i * 4.1, 2.2, 3.93, 4.6)
        s.notes_slide.notes_text_frame.text = (
            "2장 클리어 퀘스트로 탄약 공방이 열리면 시간의 조각으로 묶음을 사서 해금. 산 뒤부터 이 탄들이 판의 드랍 / 상점에 섞여 나옴 (첫 묶음 무료 없음). "
            "이름 / 생김새만 2장 기억(비 오는 승강장 / 기차) 결, 기능은 스토리와 무관. [새] = 새 메커닉")
    new_section_after(prs, "탄 목록", "2장 해금 탄", made)

    # 데이터 테이블: Unlock 행 추가 (행 간격 0.98 → 0.82)
    n = slide_index(prs, "데이터 테이블 구조")
    s = prs.slides[n - 1]
    rows = {}
    for sh in s.shapes:
        if sh.is_placeholder or sh.top < Inches(1.4) or sh.top > Inches(6.4):
            continue
        k = round((sh.top / 914400 - 1.5) / 0.98)
        rows.setdefault(k, []).append(sh)
    for k, shs in rows.items():
        for sh in shs:
            is_card = sh.shape_type == 1
            sh.top = Inches(1.45 + k * 0.82 + (0 if is_card else 0.12))
            if is_card:
                sh.height = Inches(0.7)
    y = 1.45 + 5 * 0.82
    card(s, 0.6, y, 12.13, 0.7, CARD)
    text(s, 0.9, y + 0.12, 2.4, 0.45, "Unlock", size=15, color=GOLD, bold=True)
    text(s, 3.3, y + 0.12, 9.2, 0.5, "Id / Facility / Name / Cost / ReqChapter / Items (탄 / 총 Id를 | 로 이음) / Desc", size=13)
    ed.log.append("OK   데이터 테이블 Unlock 행")

    prs.save(dst)
    return dst, ed.log


# ====================================================================== 총 Ver03
GUNS = [
    (2009, "가스등 랜턴", "Epic", 70, ("×1.05", "0.3초", "1.3초", "12m", "8%", "+20%"),
     ("랜턴 던지기", 12, ["조준한 곳에 랜턴을 던져 반경 4 빛 장판 4초", "장판 안의 적 둔화 30%", "장판 안에서 터지는 공명 반경 +1"]),
     "비 오는 승강장을 밝히던 가스등을 총에 매달았다"),
    (2010, "기관차", "Legendary", 80, ("×1.35", "0.38초", "1.6초", "16m", "10%", "+40%"),
     ("증기 돌진", 10, ["조준 방향으로 8m 돌진하며 경로의 적에게 관통 사격", "끝에서 증기 폭발 (반경 3 / 피해 150%)", "돌진 중 무적"]),
     "보일러처럼 달아오르는 대구경 총신"),
]


def gun_card(s, g, x, y, w, h):
    gid, name, grade, cost, st, (sk, cd, sk_lines), desc = g
    gname, col = G[grade]
    card(s, x, y, w, h, CARD, line=col, line_w=1.5)
    icon_or_slot(s, "guns", gid, x + 0.25, y + 0.25, 1.75, col)
    text(s, x + 2.25, y + 0.22, 2.6, 0.5, name, size=22, color=col, bold=True, valign="m")
    chip(s, gname, x + w - 1.05, y + 0.32, 0.8, h=0.32, fill=col, color=BG, size=11)
    text(s, x + 2.25, y + 0.78, w - 2.5, 0.35, [{"runs": [("묶음 ", {"c": MUTED}), (f"'{name}'", {"b": True}), (f"  /  {cost} 조각", {}), ("  (초안)", {"c": MUTED, "size": 10})], "after": 0}], size=12)
    labels = [("공격력", st[0]), ("발사 간격", st[1]), ("장전", st[2]), ("사거리", st[3]), ("치명 확률", st[4]), ("치명 피해", st[5])]
    for k, (a, b) in enumerate(labels):
        cx, cy = x + 2.25 + (k % 3) * 1.12, y + 1.2 + (k // 3) * 0.5
        text(s, cx, cy, 1.08, 0.48, [{"t": a, "c": MUTED, "size": 9.5, "after": 0}, {"t": b, "b": True, "after": 0}], size=13, spacing=1.0)
    text(s, x + 2.25, y + 2.2, 3.4, 0.25, "수치 초안", size=9.5, color=MUTED)
    card(s, x + 0.25, y + 2.55, w - 0.5, 1.75, PURPLE)
    text(s, x + 0.45, y + 2.65, 3.5, 0.4, [{"runs": [("스킬  ", {"c": MUTED, "size": 12}), (sk, {"b": True, "c": GOLD})], "after": 0}], size=16)
    text(s, x + w - 1.95, y + 2.68, 1.5, 0.35, f"재사용 {cd}초", size=12, color=MUTED, align="r")
    bullets(s, x + 0.45, y + 3.12, w - 0.9, 1.15, sk_lines, size=12.5, after=3)
    text(s, x + 0.25, y + 4.42, w - 0.5, 0.35, [{"runs": [("손별 효과  ", {"c": GOLD, "b": True}), ("없음", {})], "after": 0}], size=12)
    text(s, x + 0.25, y + h - 0.6, w - 0.5, 0.45, [
        {"runs": [(f"“{desc}”", {"c": TXT}), ("   설명 초안", {"c": MUTED, "size": 9.5})], "after": 0}], size=11.5, valign="b")


def gun_deck():
    src = os.path.join(DOCS, "1159_총_기획서_20261117_정승운_Ver02.pptx")
    dst = os.path.join(OUT_DIR, "1159_총_기획서_20261125_정승운_Ver03.pptx")
    ed = Editor(Presentation(src))
    prs = ed.prs
    ed.rep(1, "작성 정승운 / 2026.11.17 / 수치는 1차안", f"작성 정승운 / {DATE} / 수치는 1차안")

    # 규칙: 시작 총 고르기 없음 · 총포상 해금
    n = slide_index(prs, "총 규칙")
    ed.rep(n, "시작 = 양손 기본 총 (효과 없음)", "시작 = 양손 기본 총 (효과 없음). 시작 총 고르기 없음")
    # '없는 것' 머리글 앞에 '해금' 머리글 + 본문 한 줄 (기존 문단 서식 복사)
    tb = [sh for sh in prs.slides[n - 1].shapes if sh.has_text_frame and "없는 것" in sh.text_frame.text][0]
    ps = tb.text_frame.paragraphs
    head = [p for p in ps if Editor.ptext(p) == "없는 것"][0]
    body = [p for p in ps if Editor.ptext(p) == "연산 슬롯 (연산은 탄에)"][0]
    for src_p, t in ((head, "해금"), (body, "기본 8종은 처음부터 등장 / 새 총은 총포상에서 묶음을 산 뒤부터 판에 등장 (2장 클리어 퀘스트로 총포상 해금)")):
        np_ = copy.deepcopy(src_p._p)
        head._p.addprevious(np_)
        Editor.set_text(type(head)(np_, head._parent), t)
    for p in tb.text_frame.paragraphs:          # 한 줄 늘어난 만큼 글 크기 · 간격을 줄여 꼬리말과 겹치지 않게
        p.space_after = Pt(5)
        for r in p.runs:
            r.font.size = Pt(14)
    ed.log.append("ADD  총 규칙: 해금")

    # 스킬: 2열 × 5줄로 다시 놓고 2장 해금 총 스킬 2개 추가
    n = slide_index(prs, "스킬 8종 (쿨타임)")
    ed.rep(n, "스킬 8종 (쿨타임)", "스킬 10종 (쿨타임)")
    s = prs.slides[n - 1]
    groups = {}
    for sh in s.shapes:
        if sh.is_placeholder:
            continue
        row = round((sh.top / 914400 - 1.45) / 1.32 - 0.2)
        groups.setdefault(row, []).append(sh)
    for row, shs in groups.items():
        base = 1.45 + row * 1.32
        for sh in shs:
            off = sh.top / 914400 - base
            if sh.shape_type == 1:     # 카드
                sh.top = Inches(1.45 + row * 1.04); sh.height = Inches(0.95)
            elif off < 0.4:            # 이름 · 쿨타임
                sh.top = Inches(1.45 + row * 1.04 + 0.12)
            else:                      # 설명
                sh.top = Inches(1.45 + row * 1.04 + 0.5)
    for k, g in enumerate(GUNS):
        sk, cd, _ = g[5]
        x, y = 0.6 + k * 6.13, 1.45 + 4 * 1.04
        card(s, x, y, 5.95, 0.95, CARD, line=G[g[2]][1], line_w=1.25)
        text(s, x + 0.3, y + 0.12, 3.5, 0.4, [{"runs": [(sk, {"b": True, "c": GOLD}), (f"   {g[1]} / 2장 해금", {"c": MUTED, "size": 11})], "after": 0}], size=16)
        text(s, x + 4.4, y + 0.12, 1.3, 0.4, f"{cd}초", size=14, color=MUTED, align="r")
        short = {"랜턴 던지기": "반경 4 빛 장판 4초: 안의 적 둔화 30% / 안에서 터지는 공명 반경 +1",
                 "증기 돌진": "8m 돌진하며 관통 사격 / 끝에서 증기 폭발 (반경 3, 150%) / 돌진 중 무적"}[sk]
        text(s, x + 0.3, y + 0.5, 5.4, 0.42, short, size=12)

    # 새 섹션: 2장 해금 총
    last = max(i for i, sl in enumerate(prs.slides, 1) if title_of(sl).startswith("총 8종"))
    for i, sl in enumerate(prs.slides, 1):
        t = title_of(sl)
        if t.startswith("총 8종"):
            ed.rep(i, t, t.replace("총 8종", "기본 총 8종"))
    s = ed.insert_slide_after(last, "", "총포상 묶음: 가스등 랜턴 / 기관차")
    for k, g in enumerate(GUNS):
        gun_card(s, g, 0.6 + k * 6.13, 1.45, 5.95, 5.35)
    s.notes_slide.notes_text_frame.text = (
        "2장 클리어 퀘스트(보안관 결투)로 총포상이 열리면 시간의 조각으로 묶음을 사서 해금. 묶음 1개 = 총 1자루, 묶음 이름 = 총 이름. "
        "산 뒤부터 판의 드랍 / 상점에 섞여 나옴 (첫 묶음 무료 없음). 이름 / 생김새만 2장 기억 결, 기능은 스토리와 무관. "
        "이펙트: FX_LanternField(랜턴 장판) / FX_SteamDash(돌진 꼬리) / FX_SteamBlast(끝 폭발)")
    new_section_after(prs, "총 목록", "2장 해금 총", [s])

    prs.save(dst)
    return dst, ed.log


if __name__ == "__main__":
    for fn in (bullet_deck, gun_deck):
        out, log = fn()
        print("##", os.path.basename(out))
        print("\n".join(l for l in log if not l.startswith("OK")) or "  (모두 OK)")
    if missing_icons:
        print("아이콘 없음 → 자리 표시:", ", ".join(sorted(set(missing_icons))))
