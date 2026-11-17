# 공통 레벨 디자인 기획서 Ver02 → Ver03 (2026-11-17)
# 경험치 · 레벨 업 3택 · 하트 드롭 확률 반영. 실행: python edit_common_ver03.py <Ver02.pptx> <Ver03.pptx>
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pptx import Presentation
from pptx_kit import Editor, xp_slide

SRC, DST = sys.argv[1], sys.argv[2]
IMG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "images", "v3")
ed = Editor(Presentation(SRC))

# ---------- 버전 ----------
ed.replace_everywhere("Ver02", "Ver03")
ed.rep(1, "Ver 02", "Ver 03")

# ---------- 05 보상과 상인 ----------
ed.rep(8, "체력 회복: 적 처치 시 낮은 확률로 하트 드롭 (체력 20%) / 중간 보스 처치 시 확정 / 상인 판매",
       "하트: 일반 2% / 강화형 3% / 중간 보스 확정 드롭 (체력 20%)")
ed.add_after(8, "보상은 스테이지 마지막 전투방 클리어 시 지급",
             "레벨 업 시 3개 중 1개 선택 (경험치는 적 처치로 획득)")
xp_slide(ed, 8, "05  보상과 상인", IMG)

ed.slash_dots()
ed.prs.save(DST)
print("\n".join(ed.log))
