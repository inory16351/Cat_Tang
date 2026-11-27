# python-pptx 도형 단위 편집 도우미 (2026-11-27, 기획서 최신화용). 글 바꾸기는 pptx_kit.Editor와 같이 씀
# - 도형 찾기 / 여러 줄 글 넣기(줄마다 기존 첫 run 서식 유지) / 도형 지우기 / 복사해서 옮기기 / 그림 바꾸기 / 슬라이드 지우기
import copy
from pptx.util import Emu

E = 914400
NS_P14 = "http://schemas.microsoft.com/office/powerpoint/2010/main"


def shape(slide, name):
    for sh in slide.shapes:
        if sh.name == name:
            return sh
    raise KeyError(name)


def title_of(slide):
    for sh in slide.shapes:
        if sh.is_placeholder and sh.placeholder_format.type in (1, 3):     # TITLE / CENTER_TITLE
            return sh
    for sh in slide.shapes:
        if sh.name == "Text 1":
            return sh
    return None


def set_title(slide, t):
    set_text(title_of(slide), t)


def set_text(sh, text):
    """여러 줄(\\n) 글을 넣음. 줄마다 원래 같은 순번 문단의 서식(없으면 마지막 문단)을 씀"""
    tf = sh.text_frame
    paras = list(tf.paragraphs)
    lines = text.split("\n")
    proto = [copy.deepcopy(p._p) for p in paras]
    body = paras[0]._p.getparent()
    for p in paras:
        body.remove(p._p)
    for i, line in enumerate(lines):
        el = copy.deepcopy(proto[min(i, len(proto) - 1)])
        rs = el.findall("{http://schemas.openxmlformats.org/drawingml/2006/main}r")
        for r in rs[1:]:
            el.remove(r)
        for br in el.findall("{http://schemas.openxmlformats.org/drawingml/2006/main}br"):
            el.remove(br)
        if rs:
            rs[0].find("{http://schemas.openxmlformats.org/drawingml/2006/main}t").text = line
        else:
            from lxml import etree
            A = "http://schemas.openxmlformats.org/drawingml/2006/main"
            r = etree.SubElement(el, f"{{{A}}}r")
            etree.SubElement(r, f"{{{A}}}t").text = line
            end = el.find(f"{{{A}}}endParaRPr")
            if end is not None:
                el.remove(end); el.append(end)
        body.append(el)


def delete(sh):
    el = sh._element
    el.getparent().remove(el)


def clone(slide, shapes, dx=0.0, dy=0.0):
    """같은 슬라이드에 도형들을 복사해 (dx, dy 인치) 옮겨 붙임. 그림은 같은 그림 관계를 그대로 씀. 새 도형 목록을 돌려줌"""
    tree = slide.shapes._spTree
    out = []
    for sh in shapes:
        el = copy.deepcopy(sh._element)
        tree.append(el)
        new = slide.shapes[-1]
        new.left = Emu(int(sh.left + dx * E))
        new.top = Emu(int(sh.top + dy * E))
        out.append(new)
    # 도형 id 겹침 방지
    ids = set()
    mx = max(int(s.shape_id) for s in slide.shapes)
    for s in slide.shapes:
        cnv = s._element.find(".//{http://schemas.openxmlformats.org/presentationml/2006/main}cNvPr")
        if cnv is None:
            continue
        i = int(cnv.get("id"))
        if i in ids:
            mx += 1
            cnv.set("id", str(mx))
        ids.add(int(cnv.get("id")))
    return out


def swap_picture(slide, pic, path):
    """그림 도형의 그림만 바꿈 (자리 · 크기 그대로)"""
    _, rid = slide.part.get_or_add_image_part(path)
    blip = pic._element.find(".//{http://schemas.openxmlformats.org/drawingml/2006/main}blip")
    blip.set("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}embed", rid)


def delete_slide(prs, slide):
    sldIdLst = prs.slides._sldIdLst
    for sid in list(sldIdLst):
        rid = sid.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
        if prs.part.related_part(rid) is slide.part:
            num = sid.get("id")
            sldIdLst.remove(sid)
            prs.part.drop_rel(rid)
            # 섹션 목록에서도 뺌
            for e in prs.part._element.iter(f"{{{NS_P14}}}sldId"):
                if e.get("id") == num:
                    e.getparent().remove(e)
            return
    raise ValueError("slide not found")


def move_slide(prs, slide, new_index):
    sldIdLst = prs.slides._sldIdLst
    for sid in list(sldIdLst):
        rid = sid.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
        if prs.part.related_part(rid) is slide.part:
            sldIdLst.remove(sid)
            sldIdLst.insert(new_index, sid)
            return


def all_text(prs):
    out = []
    for i, s in enumerate(prs.slides, 1):
        for sh in s.shapes:
            if sh.has_text_frame:
                out.append((i, sh.name, sh.text_frame.text))
            if getattr(sh, "has_table", False) and sh.has_table:
                for r in sh.table.rows:
                    for c in r.cells:
                        out.append((i, sh.name, c.text))
    return out
