# 컨셉 기획서 Ver03 → Ver04 (2026-11-27 게임과 맞춤)
# 보상 문 방식 · 레벨 업 후보 연산 · 특성 단계 교체 · 미련(공명 강화) · 예지(극한 회피) · 총 스킬 1개 · 마을(새벽 섬) · 성장 요소 · 코스튬 효과
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from pptx import Presentation
from pptx_kit import Editor
import deck_ops as O

DOCS = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(DOCS, "1159_컨셉_기획서_20261117_정승운_Ver03.pptx")
OUT = os.path.join(DOCS, "1159_컨셉_기획서_20261127_정승운_Ver04.pptx")

prs = Presentation(SRC)
ed = Editor(prs)
S = prs.slides
ed.rep(1, "작성 정승운 / 2026.11.17", "작성 정승운 / 2026.11.27")
ed.rep(1, "Ver 03", "Ver 04")

# 11 챕터 / 스테이지 구성과 보상: 스테이지 끝 보상 문 (하데스식)
s = S[10]
ed.rep(11, "모든 적을 처치하면 스테이지 클리어 처리 후 보상 지급", "마지막 방을 비우면 그 스테이지 보상 지급 + 다음 스테이지 보상 문 2~3개 등장")
O.set_text(O.shape(s, "text_165"), "보상 문")
O.set_text(O.shape(s, "text_166"), "3개 중 1개 선택")
O.set_text(O.shape(s, "text_175"), "확정 지급 문")
O.set_text(O.shape(s, "text_176"), "고르기 없이 바로 지급")
ed.rep(11, "전투방 클리어 시 15% 확률로 등장 / 골드로 총탄 / 총 / 연산 / 하트 판매", "전투방 클리어 시 15% 확률로 등장 / 장마다 최소 1번 / 골드로 총탄 / 총 / 연산 / 하트 판매")
O.set_text(O.shape(s, "text_162"), "보상 / 다음 보상 문")

# 12 레벨 업 후보에 연산
ed.rep(12, "총 / 방어구 강화 중", "총 / 연산 / 방어구 강화 중", partial=True)

# 18 특성
ed.rep(18, "동일한 특성이 2, 4, 6개 모이면 단계별 효과 발동", "동일한 특성이 2, 4, 6개 모이면 단계별 효과 발동 (높은 단계 효과가 낮은 단계를 대신함)")
ed.rep(18, "대시 회피 판정 증가", "극한 회피 판정 증가")
ed.rep(18, "비활성화 탄에 보너스 효과 추가", "공명 사격 강화 / 끝내 원혼이 울림")

# 19 총: 스킬 1개
ed.rep(19, "총이 액티브 스킬을 활성화 → 캐릭터는 최대 2개의 추가 기술 보유", "총마다 액티브 스킬 1개 → 양손 중 고유 스킬이 있는 총의 스킬을 우클릭으로 사용")

# 21 마을
s = S[20]
O.set_title(s, "마을: 새벽에 사라져 가는 섬 마을")
O.swap_picture(s, O.shape(s, "img_398"), os.path.join(DOCS, "images", "village", "map_all.png"))
O.set_text(O.shape(s, "text_401"), "녹아 사라지는 서부 마을 / 섬 여러 개를 다리로 이음 / 별밤 아래 지평선만 밝아 오는 새벽")
O.set_text(O.shape(s, "text_402"), "장을 넘을 때마다 다리가 이어져 섬 / 시설 / NPC 해금")

# 22 해금되는 성장 요소
s = S[21]
O.set_text(O.shape(s, "text_407"), "기억의 방")
O.set_text(O.shape(s, "text_408"), "화력 / 생존 / 손재주 3갈래 노드\n시간의 조각으로 단계 강화")
O.set_text(O.shape(s, "text_412"), "새 아이템")
O.set_text(O.shape(s, "text_413"), "탄약 공방 / 총포상 묶음 구매\n산 뒤부터 판에 등장")
O.set_text(O.shape(s, "text_418"), "업적 조건 달성 후 구매\n장점과 단점이 함께 있는 효과")
O.set_text(O.shape(s, "text_425"), "코스튬마다 외형과 함께 판의 출발점을 바꾸는 효과 (능력치 / 시작 아이템 / 규칙)")

ed.replace_everywhere("컨셉 기획서 Ver03", "컨셉 기획서 Ver04")
ed.slash_dots()
prs.save(OUT)
print("\n".join(ed.log)); print("saved", OUT)
