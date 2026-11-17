# 나열 구분자 가운데 점(·) → " / " (1장 Ver02와 같은 규칙). 사용: python slash_dots.py <pptx>
import sys
from pptx import Presentation

def slash(t):
    return t.replace(" · ", " / ").replace("· ", "/ ").replace(" ·", " /").replace("·", "/")

def fix(shapes):
    n = 0
    for sh in shapes:
        if sh.shape_type == 6:
            n += fix(sh.shapes); continue
        if sh.has_text_frame:
            for p in sh.text_frame.paragraphs:
                for r in p.runs:
                    if "·" in r.text: r.text = slash(r.text); n += 1
    return n

prs = Presentation(sys.argv[1])
n = sum(fix(s.shapes) for s in prs.slides)
for s in prs.slides:
    if s.has_notes_slide:
        for p in s.notes_slide.notes_text_frame.paragraphs:
            for r in p.runs:
                if "·" in r.text: r.text = slash(r.text); n += 1
for lay in list(prs.slide_layouts) + [prs.slide_master]:
    n += fix(lay.shapes)
prs.save(sys.argv[1])
print("dots ->", n)
