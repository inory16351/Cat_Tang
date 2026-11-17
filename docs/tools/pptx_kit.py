# python-pptx 편집 도우미 (edit_*_ver0N.py 공용)
# - 문단 바꾸기 / 뒤에 문단 추가 (기존 서식 유지)
# - 가운데 점(·) → " / " (2026-11-17 사용자 규칙)
# - 새 슬라이드 끼워 넣기 + 섹션 등록, 컨셉 기획서 Ver02 팔레트로 카드 · 글 · 그림(비율 유지) · 글머리표 그리기
import copy
from lxml import etree
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.text.text import _Paragraph
from PIL import Image

FONT = "Noto Sans KR"
BG, CARD, TXT, MUTED = "17131F", "2C2440", "F3EDE2", "A99FB5"
GOLD, RED, TEAL, PURPLE, LAV, FLOOR = "E0A63A", "C2343B", "5FA8B8", "3B2D5C", "8C7AA8", "221B2E"
NS = {"a": "http://schemas.openxmlformats.org/drawingml/2006/main",
      "p": "http://schemas.openxmlformats.org/presentationml/2006/main",
      "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
      "p14": "http://schemas.microsoft.com/office/powerpoint/2010/main"}


class Editor:
    def __init__(self, prs):
        self.prs = prs
        self.log = []

    # ---------- 글 바꾸기 ----------
    @staticmethod
    def paras_of(shapes):
        for sh in shapes:
            if sh.shape_type == 6:
                yield from Editor.paras_of(sh.shapes); continue
            if sh.has_text_frame:
                for p in sh.text_frame.paragraphs:
                    yield sh, p
            if getattr(sh, "has_table", False) and sh.has_table:
                for row in sh.table.rows:
                    for c in row.cells:
                        for p in c.text_frame.paragraphs:
                            yield sh, p

    @staticmethod
    def ptext(p):
        return "".join(r.text for r in p.runs)

    @staticmethod
    def set_text(p, new):
        runs = p.runs
        runs[0].text = new
        for r in runs[1:]:
            r._r.getparent().remove(r._r)

    def rep(self, n, old, new, partial=False):
        """슬라이드 n에서 문단 텍스트가 old인(partial이면 old를 포함한) 문단을 new로"""
        hit = 0
        for sh, p in self.paras_of(self.prs.slides[n - 1].shapes):
            t = self.ptext(p)
            if not p.runs:
                continue
            if t == old or (partial and old in t):
                self.set_text(p, t.replace(old, new) if partial else new)
                hit += 1
        self.log.append(f"{'OK  ' if hit else 'MISS'} s{n}: {old[:28]} → {new[:36]}")
        return hit

    def rep_run(self, n, old, new):
        """run 하나 안의 글만 바꿈 (서식이 다른 run이 섞인 문단용)"""
        hit = 0
        for sh, p in self.paras_of(self.prs.slides[n - 1].shapes):
            for r in p.runs:
                if old in r.text:
                    r.text = r.text.replace(old, new); hit += 1
        self.log.append(f"{'OK  ' if hit else 'MISS'} s{n} run: {old[:28]} → {new[:36]}")

    def add_after(self, n, anchor, new):
        for sh, p in self.paras_of(self.prs.slides[n - 1].shapes):
            if self.ptext(p) == anchor:
                np_ = copy.deepcopy(p._p)
                p._p.addnext(np_)
                self.set_text(_Paragraph(np_, p._parent), new)
                self.log.append(f"ADD  s{n}: {new[:40]}")
                return
        self.log.append(f"MISS-ADD s{n}: {anchor[:30]}")

    def replace_everywhere(self, old, new, layouts=True):
        n = 0
        for s in self.prs.slides:
            for sh, p in self.paras_of(s.shapes):
                for r in p.runs:
                    if old in r.text:
                        r.text = r.text.replace(old, new); n += 1
        if layouts:
            for lay in list(self.prs.slide_layouts) + [self.prs.slide_master]:
                for sh, p in self.paras_of(lay.shapes):
                    for r in p.runs:
                        if old in r.text:
                            r.text = r.text.replace(old, new); n += 1
        self.log.append(f"전체 {old} → {new}: {n}곳")

    def slash_dots(self):
        def slash(t):
            return t.replace(" · ", " / ").replace("· ", "/ ").replace(" ·", " /").replace("·", "/")
        n = 0
        targets = [s.shapes for s in self.prs.slides] + [l.shapes for l in list(self.prs.slide_layouts) + [self.prs.slide_master]]
        for shapes in targets:
            for sh, p in self.paras_of(shapes):
                for r in p.runs:
                    if "·" in r.text:
                        r.text = slash(r.text); n += 1
        for s in self.prs.slides:
            if s.has_notes_slide:
                for p in s.notes_slide.notes_text_frame.paragraphs:
                    for r in p.runs:
                        if "·" in r.text:
                            r.text = slash(r.text); n += 1
        self.log.append(f"가운데 점 → / : {n}곳")

    # ---------- 새 슬라이드 ----------
    def insert_slide_after(self, n, kicker, title):
        """슬라이드 n(1부터) 바로 뒤에 같은 레이아웃 슬라이드를 넣고, 같은 섹션에 등록.
        kicker(작은 금색 글) · 제목 · 쪽 번호는 슬라이드 n의 도형을 복사해 씀"""
        prs = self.prs
        src = prs.slides[n - 1]
        s = prs.slides.add_slide(src.slide_layout)
        for ph in list(s.placeholders):
            ph._element.getparent().remove(ph._element)
        for sh in src.shapes:
            el = sh._element
            keep = False
            if sh.is_placeholder:
                keep = True                                     # 제목
            elif sh.has_text_frame and el.findall(".//a:fld", NS):
                keep = True                                     # 쪽 번호
            elif sh.has_text_frame and sh.top < Inches(0.5) and sh.text_frame.text.strip()[:2].isdigit():
                keep = True                                     # kicker
            if keep:
                s.shapes._spTree.append(copy.deepcopy(el))
        for sh in s.shapes:
            if not sh.has_text_frame or sh._element.findall(".//a:fld", NS):
                continue                                        # 쪽 번호는 필드라 그대로
            self.set_text(sh.text_frame.paragraphs[0], title if sh.is_placeholder else kicker)
        # 순서: 새 슬라이드(맨 뒤)를 n 뒤로
        lst = prs.slides._sldIdLst
        ids = list(lst)
        new_id = ids[-1]
        lst.remove(new_id)
        lst.insert(n, new_id)
        # 섹션 등록
        src_id = ids[n - 1].get("id")
        for sec in prs.part._element.iterfind(".//p14:section", NS):
            lst14 = sec.find("p14:sldIdLst", NS)
            for e in list(lst14):
                if e.get("id") == src_id:
                    ne = etree.SubElement(lst14, f"{{{NS['p14']}}}sldId")
                    ne.set("id", new_id.get("id"))
                    lst14.remove(ne)
                    e.addnext(ne)
        self.log.append(f"NEW  s{n + 1}: {title}")
        return s


# ---------- 그리기 ----------
def _rgb(h):
    return RGBColor.from_string(h)


def card(s, x, y, w, h, fill=CARD, line=None, line_w=1.0, radius=0.12):
    sh = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
    sh.adjustments[0] = min(0.5, radius / min(w, h))
    sh.fill.solid(); sh.fill.fore_color.rgb = _rgb(fill)
    sh.line.color.rgb = _rgb(line or fill); sh.line.width = Pt(line_w)
    sh.shadow.inherit = False
    sh.text_frame.text = ""
    return sh


def bar(s, x, y, w, h, fill):
    sh = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
    sh.fill.solid(); sh.fill.fore_color.rgb = _rgb(fill)
    sh.line.fill.background(); sh.shadow.inherit = False
    return sh


def _style_run(r, size, color, bold):
    f = r.font
    f.name = FONT; f.size = Pt(size); f.bold = bold; f.color.rgb = _rgb(color)
    rPr = r._r.get_or_add_rPr()
    rPr.set("lang", "ko-KR")
    for tag in ("a:ea", "a:cs"):
        el = rPr.find(tag, NS)
        if el is None:
            el = etree.SubElement(rPr, f"{{{NS['a']}}}{tag[2:]}")
        el.set("typeface", FONT)


def text(s, x, y, w, h, items, size=14, color=TXT, bold=False, align="l", valign="t", spacing=1.1, after=4):
    """items: 문자열 하나, 또는 문단 목록. 문단 = 문자열 | dict(t | runs=[(글, dict)], b, c, size, bullet, sub)"""
    tb = s.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = tb.text_frame
    tf.word_wrap = True
    tf.margin_left = tf.margin_right = tf.margin_top = tf.margin_bottom = 0
    tf.vertical_anchor = {"t": MSO_ANCHOR.TOP, "m": MSO_ANCHOR.MIDDLE, "b": MSO_ANCHOR.BOTTOM}[valign]
    if isinstance(items, str):
        items = [items]
    for i, it in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        d = {"t": it} if isinstance(it, str) else it
        p.alignment = {"l": PP_ALIGN.LEFT, "c": PP_ALIGN.CENTER, "r": PP_ALIGN.RIGHT}[d.get("align", align)]
        p.line_spacing = spacing
        p.space_after = Pt(d.get("after", after))
        if d.get("before"):
            p.space_before = Pt(d["before"])
        sz = d.get("size", size - (2 if d.get("sub") else 0))
        runs = d.get("runs") or [(d["t"], {})]
        for txt_, o in runs:
            r = p.add_run(); r.text = txt_
            _style_run(r, o.get("size", sz), o.get("c", d.get("c", MUTED if d.get("sub") else color)), o.get("b", d.get("b", bold)))
        if d.get("bullet"):
            ind = Pt(12 if d.get("sub") else 15)
            pPr = p._p.get_or_add_pPr()
            pPr.set("marL", str(int(ind * (2 if d.get("sub") else 1))))
            pPr.set("indent", str(-int(ind)))
            bu = etree.SubElement(pPr, f"{{{NS['a']}}}buFont"); bu.set("typeface", "Arial")
            bc = etree.SubElement(pPr, f"{{{NS['a']}}}buChar"); bc.set("char", "•")
    return tb


def bullets(s, x, y, w, h, items, size=14, color=TXT, **kw):
    out = []
    for it in items:
        d = {"t": it} if isinstance(it, str) else dict(it)
        if not d.get("head"):
            d["bullet"] = True
        else:
            d.setdefault("b", True); d.setdefault("c", GOLD); d.setdefault("before", 6)
        out.append(d)
    return text(s, x, y, w, h, out, size=size, color=color, **kw)


def chip(s, t, x, y, w, h=0.36, fill=GOLD, color=BG, size=12):
    sh = card(s, x, y, w, h, fill, radius=h / 2)
    text(s, x, y, w, h, [{"t": t, "after": 0}], size=size, color=color, bold=True, align="c", valign="m")
    return sh


def picture(s, path, x, y, w, h, fit="contain", align="c"):
    """비율 유지: contain = 상자 안에 맞춤 / cover = 상자를 채우고 넘치는 쪽을 잘라 냄"""
    iw, ih = Image.open(path).size
    if fit == "contain":
        k = min(w / iw, h / ih); dw, dh = iw * k, ih * k
        dx = x if align == "l" else x + (w - dw) / 2
        return s.shapes.add_picture(path, Inches(dx), Inches(y + (h - dh) / 2), Inches(dw), Inches(dh))
    k = max(w / iw, h / ih); dw, dh = iw * k, ih * k
    pic = s.shapes.add_picture(path, Inches(x), Inches(y), Inches(w), Inches(h))
    cx, cy = (dw - w) / dw / 2, (dh - h) / dh / 2
    pic.crop_left = pic.crop_right = cx
    pic.crop_top = pic.crop_bottom = cy
    return pic


def xp_slide(ed, after, kicker, img_dir):
    """경험치 · 레벨 업 · 체력 회복 슬라이드 (공통 레벨 디자인 · 컨셉 기획서 공용)"""
    s = ed.insert_slide_after(after, kicker, "런 안 성장: 경험치와 레벨 업")
    j = lambda n: f"{img_dir}/{n}"
    # 왼쪽 위: 경험치
    card(s, 0.6, 1.6, 6.3, 2.5)
    text(s, 0.85, 1.72, 3.0, 0.4, "경험치", size=17, color=GOLD, bold=True)
    picture(s, j("ui_xp_orb.png"), 6.2, 1.72, 0.5, 0.42)
    bullets(s, 0.85, 2.2, 5.8, 1.2, [
        "적을 처치하면 경험치 보석이 튀어나와 고양이에게 빨려 옴",
        "보석: 일반 적 2 / 강화형 3 / 중간 보스 20",
        "필요 경험치 = 12 + 6 × (레벨 − 1) / 넘친 만큼 이어짐",
    ], size=12.5, after=3)
    picture(s, j("ui_xp_bar.png"), 0.85, 3.42, 4.3, 0.42, align="l")
    text(s, 5.25, 3.42, 1.55, 0.42, "장전 HUD 밑\n경험치 바", size=9.5, color=MUTED, valign="m", spacing=1.0, after=0)
    # 왼쪽 아래: 레벨 업 3택
    card(s, 0.6, 4.25, 6.3, 2.6, PURPLE)
    text(s, 0.85, 4.37, 2.5, 0.4, "레벨 업 3택", size=17, color=GOLD, bold=True)
    picture(s, j("ui_level_up.png"), 3.6, 4.4, 3.1, 0.38, align="r")
    bullets(s, 0.85, 4.9, 5.85, 1.9, [
        "게임이 멈추고 3개 중 1개 선택 (세피리아식)",
        "후보: 체력 회복 / 골드 / 시간의 조각 / 총탄 / 총 / 방어구 강화 중 서로 다른 3개",
        "체력 회복 = 최대 체력 30% (다쳤을수록 잘 나옴)",
        "골드 = 스테이지 보상의 50% / 시간의 조각 = 60%",
    ], size=12.5, after=3)
    # 오른쪽: 체력 회복
    card(s, 7.2, 1.6, 5.5, 5.25, CARD, line=RED, line_w=1.5)
    text(s, 7.45, 1.72, 3.0, 0.4, "체력 회복", size=17, color=RED, bold=True)
    picture(s, j("ui_heal.png"), 11.55, 1.72, 0.95, 1.25, align="r")
    rows = [("하트 드롭", "적 처치 시 일반 2% / 강화형 3%\n중간 보스는 확정", "최대 체력 20%"),
            ("상인 하트", "25골드 / 상인마다 3개", "최대 체력 20%"),
            ("레벨 업", "3택 중 체력 회복", "최대 체력 30%")]
    for k, (a, b, c) in enumerate(rows):
        y = 3.05 + k * 1.02
        card(s, 7.45, y, 5.0, 0.9, PURPLE if k % 2 == 0 else FLOOR)
        text(s, 7.65, y, 1.2, 0.9, a, size=13, color=GOLD, bold=True, valign="m")
        text(s, 8.85, y, 2.25, 0.9, b, size=11.5, valign="m", spacing=1.05, after=0)
        text(s, 11.05, y, 1.3, 0.9, c, size=12, bold=True, valign="m", align="r")
    text(s, 7.45, 6.12, 5.0, 0.6, "하트는 다가가면 빨려 와 회복 / 체력이 가득이면 빨려 오지 않음",
         size=11, color=MUTED, valign="m")
    return s
