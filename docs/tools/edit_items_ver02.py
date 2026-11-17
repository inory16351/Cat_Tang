# 총 · 총탄 · 코스튬 기획서 PPT → 새 버전 (2026-11-17)
# 완벽 장전 삭제 반영: 총 2008 '마지막 종소리' 왼손 = 장전이 끝날 때 발동, 코스튬 4005 '거울 망토' 해금 = 누적 극한 회피 100회
# 원본(Cat_Tang_*.pptx)은 그대로 두고 1159_<이름>_20261117_정승운_Ver02.pptx로 저장. 가운데 점(·) → " / "
# 실행: python edit_items_ver02.py   (docs/ 기준)
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pptx import Presentation
from pptx_kit import Editor

DOCS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
JOBS = {
    "총": [("스킬 · 완벽 장전 효과만 전역", "스킬 · 장전 끝 효과만 전역"),
           ("[완벽 장전] [전역] 완벽 장전 때 반경 6 적 0.5초 정지", "[장전 끝] [전역] 장전이 끝날 때 반경 6 적 0.5초 정지")],
    "총탄": [("스킬 · 완벽 장전 효과만 전역", "스킬 · 장전 끝 효과만 전역")],
    "코스튬": [("해금: 누적 완벽 장전 100회", "해금: 누적 극한 회피 100회")],
}
for name, reps in JOBS.items():
    src = os.path.join(DOCS, f"Cat_Tang_{name}_기획서.pptx")
    dst = os.path.join(DOCS, f"1159_{name}_기획서_20261117_정승운_Ver02.pptx")
    ed = Editor(Presentation(src))
    for old, new in reps:
        ed.replace_everywhere(old, new, layouts=False)
    ed.replace_everywhere("작성 정승운 · 2026.11 ·", "작성 정승운 · 2026.11.17 ·", layouts=False)
    ed.slash_dots()
    left = sum("완벽 장전" in r.text for s in ed.prs.slides for _, p in ed.paras_of(s.shapes) for r in p.runs)
    ed.prs.save(dst)
    print(f"## {name} → {os.path.basename(dst)} (남은 '완벽 장전' {left}곳)")
    print("\n".join(ed.log))
