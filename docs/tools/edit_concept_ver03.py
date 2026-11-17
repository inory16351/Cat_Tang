# 컨셉 기획서 Ver02 → Ver03 (2026-11-17)
# Ver02는 생성 뒤 손으로 고친 부분이 있어 생성기(build_concept_v2.js)를 다시 돌리지 않고 python-pptx로 편집
# - 전투방 4~5개 · 상인 15% / 하트 판매
# - 새 슬라이드: 인게임 그림체(브롤 스타즈풍) · 타격 연출과 조준선 (04 비주얼 가이드), 경험치와 레벨 업 (05 레벨 디자인)
# - 대쉬 → 대시, 가운데 점(·) → " / "
# 실행: python edit_concept_ver03.py <Ver02.pptx> <Ver03.pptx>
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pptx import Presentation
from pptx_kit import Editor, xp_slide, card, text, bullets, picture, MUTED

SRC, DST = sys.argv[1], sys.argv[2]
IMG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "images", "v3")
ed = Editor(Presentation(SRC))

# ---------- 버전 · 날짜 ----------
ed.replace_everywhere("Ver02", "Ver03")
ed.rep(1, "Ver 02", "Ver 03")
ed.rep(1, "작성 정승운 · 2026.11.16", "작성 정승운 · 2026.11.17")

# ---------- 9. 챕터 · 스테이지 구성과 보상 ----------
ed.rep(9, "각 스테이지는 3~4개의 전투방 존재", "각 스테이지는 4~5개의 전투방 존재")
k = 0
for sh, p in ed.paras_of(ed.prs.slides[8].shapes):
    t = ed.ptext(p)
    if t == "전투방" and p.runs:
        k += 1
        ed.set_text(p, ["전투방 1", "전투방 2~3", "전투방 4"][k - 1] if k <= 3 else t)
    elif t == "(전투방)" and p.runs:
        ed.set_text(p, "(전투방 5)")
ed.log.append(f"OK   s9: 스테이지 흐름 그림 번호 {k}")
ed.rep_run(9, "스테이지마다 일정 확률로 등장 · 골드로 총탄 / 총 / 연산 판매",
           "전투방 클리어 시 15% 확률로 등장 · 골드로 총탄 / 총 / 연산 / 하트 판매")

# ---------- 새 슬라이드 (뒤에서부터 끼워 번호가 안 밀리게) ----------
xp_slide(ed, 9, "05  레벨 디자인", IMG)

# 04 비주얼 가이드: 인게임 그림체
s = ed.insert_slide_after(7, "04  비주얼 가이드", "인게임 그림체: 브롤 스타즈풍")
picture(s, f"{IMG}/ingame_brawl.png", 0.6, 1.6, 7.3, 3.85, fit="contain", align="l")
text(s, 0.6, 5.5, 7.3, 0.28, "프로토타입 인게임 (브롤풍 셰이더 적용)", size=10, color=MUTED)
text(s, 0.6, 5.9, 7.3, 0.9, "하데스풍 잉크 · 해칭 대신, 플랫하면서 가시성이 높은 그림체로 확정 (2026-11-17)",
     size=14, spacing=1.15)
card(s, 8.15, 1.6, 4.55, 5.25)
bullets(s, 8.4, 1.72, 4.1, 5.05, [
    {"t": "그림자", "head": True, "before": 0},
    "1단으로 딱 끊고 같은 색상을 어둡게 (보라 그림자 없음)",
    {"t": "고양이 · 몬스터", "head": True},
    "배경과 따로 칠함 · 채도를 올림",
    "검은 외곽선 + 얇은 흰 림",
    {"t": "배경", "head": True},
    "잔무늬를 뭉개 캐릭터가 튀게",
    {"t": "색 규칙", "head": True},
    "맵 = 따뜻하게 · 몬스터 = 차가운 보라 · 청회색 + 빨간 바깥 테두리",
    "쏘면 터지는 물체 = 주황 ↔ 노랑으로 맥동하는 테두리",
], size=13, after=3)

# 04 비주얼 가이드: 타격 연출과 조준선
s = ed.insert_slide_after(8, "04  비주얼 가이드", "타격 연출과 조준선")
picture(s, f"{IMG}/trait_hits.png", 0.6, 1.6, 6.6, 3.4, fit="contain", align="l")
text(s, 0.6, 5.03, 6.6, 0.28, "특성 타격 (프로토타입 인게임): 되감기 · 종말 · 지연 · 예지 · 미련 · 영원", size=10, color=MUTED)
picture(s, f"{IMG}/cursor_invert.png", 0.6, 5.4, 6.6, 1.15, fit="contain", align="l")
text(s, 0.6, 6.58, 6.6, 0.28, "반전 조준선: 밑색에 따라 안쪽 색이 바뀌어 어디서나 보임", size=10, color=MUTED)
card(s, 7.5, 1.6, 5.2, 5.25)
bullets(s, 7.75, 1.72, 4.75, 5.05, [
    {"t": "특성 타격", "head": True, "before": 0},
    "아이콘 대신 타격 자체로 개성 · 특성마다 고유 색과 고유 타격",
    "맞은 적의 몸도 특성 색으로 반응 · 궤적도 특성 색",
    {"t": "여러 특성이 켜져 있으면", "head": True},
    "날아가는 탄 = 가장 많이 모은 특성 (2단계 이상)",
    "맞은 자리 = 켜진 특성 전부 · 대표는 크게, 나머지는 작게",
    {"t": "극한 회피", "head": True},
    "회색 화면 + 슬로우 · 고양이와 이펙트만 제 색 · 하늘색 리본",
    {"t": "조준 커서", "head": True},
    "FPS식 반전 조준선 · 가운데 점은 늘 빨강",
], size=13, after=3)

# ---------- 용어 · 구분자 ----------
ed.replace_everywhere("대쉬", "대시")
ed.slash_dots()
ed.prs.save(DST)
print("\n".join(ed.log))
