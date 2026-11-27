# 총탄 기획서 Ver04 → Ver05 · 총 기획서 Ver03 → Ver04 (2026-11-27 게임 데이터와 맞춤)
# 총탄: 평행세계 = 전설 / 유도 (사용자: 코드대로) · 새벽여명 = 1명 관통 · 레테의 물결 10명당 +1 (최대 10) · 추억 표식 최대 5 / 6단계 +7%
#       단계 효과 = 교체 (사용자: 기획서 유지, 코드를 고침) · 저스트 회피 → 극한 회피 · 희귀 12 / 전설 9 (기본)
# 총: 발사 간격 0.3초 통일 · 사거리 / 공격력 / 치명 = Gun.csv · 은빛 메아리 관통 메아리 · 마지막 종소리 유도 추가 ·
#     스킬은 개념만 (사용자: 개별 스킬 미기획) · 증기 돌진 8m (사용자: 코드 유지)
# 실행: python edit_items_ver05.py
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from pptx import Presentation
from pptx.util import Pt
from pptx_kit import Editor
import deck_ops as O

DOCS = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ICON = os.path.abspath(os.path.join(DOCS, "..", "Game", "Assets", "Resources", "Icons"))


def bullet_deck():
    src = os.path.join(DOCS, "1159_총탄_기획서_20261125_정승운_Ver04.pptx")
    out = os.path.join(DOCS, "1159_총탄_기획서_20261127_정승운_Ver05.pptx")
    prs = Presentation(src)
    ed = Editor(prs)
    S = prs.slides
    ed.rep(1, "작성 정승운 / 2026.11.25 / 수치는 1차안", "작성 정승운 / 2026.11.27 / 수치는 1차안")

    # 03 효과 적용 범위: 단계 효과 = 교체
    ed.add_after(3, "비활성화 탄도 포함", "단계 효과는 교체: 4단계가 2단계를 대신하고, 6단계는 4단계 효과 + 새 효과")

    # 04 등급 종 수 (Bullet.csv: 일반 16 / 희귀 14 / 영웅 12 / 전설 11, 2장 해금 탄 포함)
    ed.rep(4, "15종", "14종")
    ed.rep(4, "10종", "11종")

    # 05 · 06 특성 표
    ed.rep(5, "맞힐 때마다 추억 표식, 표식당 그 적에게 피해 +3% (최대 10개)", "맞힐 때마다 추억 표식, 표식당 그 적에게 피해 +3% (최대 5개)")
    ed.rep(5, "표식 10개인 적을 맞히면 표식을 터뜨려 피해 300%", "표식당 +7% / 표식 5개가 다 쌓인 적을 5번 더 맞히면 표식을 터뜨려 피해 300%")
    ed.rep(6, "저스트 회피 판정 +20%", "극한 회피 판정 +20%")
    ed.rep(6, "저스트 회피 후 다음 3발 치명타", "극한 회피 후 다음 3발 치명타")
    ed.rep(6, "다가올 공격을 미리 본다 (저스트 회피)", "다가올 공격을 미리 본다 (극한 회피)")
    ed.replace_everywhere("[저스트 회피] 저스트 회피하면", "[극한 회피] 극한 회피하면")

    # 희귀 1/3: 평행세계 자리에 바람개비, 희귀 3/3 지움
    s11, s12, s13 = S[10], S[11], S[12]
    O.set_text(O.shape(s11, "text_149"), "바람개비")
    O.set_text(O.shape(s11, "text_150"), "3 초과 / 되감기 / 회상\n공격력 12 / 슬롯 1")
    O.set_text(O.shape(s11, "text_151"), "[상시] 이동 속도 +10%")
    O.swap_picture(s11, O.shape(s11, "icon_148"), os.path.join(ICON, "bullets", "bullet_1042.png"))
    O.set_title(s11, "희귀 탄 (기본 12종) 1/2")
    O.set_title(s12, "희귀 탄 (기본 12종) 2/2")
    # 새벽여명 = 1명 관통 (자동 추적은 총 '마지막 종소리'만)
    O.set_text(O.shape(s11, "text_161"), "[명중] 1명 관통")

    # 전설: 평행세계 추가, 레테의 물결
    s16, s17 = S[15], S[16]
    O.set_title(s16, "전설 탄 (기본 9종) 1/2")
    O.set_title(s17, "전설 탄 (기본 9종) 2/2")
    O.set_text(O.shape(s16, "text_281"), "[처치] 시간에 갇힌 적을 10명 처치할 때마다 판 동안 이 탄 공격력 +1 (최대 10)\n[명중] 12% 확률로 1초 시간에 가둠")
    src_shapes = [O.shape(s17, n) for n in ("card_287", "icon_288", "text_289", "text_290", "text_291")]
    card, icon, name, cond, eff = O.clone(s17, src_shapes, dx=4.10)
    O.set_text(name, "평행세계")
    O.set_text(cond, "음수 / 되감기 / 지연\n공격력 16 / 슬롯 2")
    O.set_text(eff, "[상시] 가장 가까운 적을 따라감")
    O.swap_picture(s17, icon, os.path.join(ICON, "bullets", "bullet_1015.png"))

    O.delete_slide(prs, s13)
    ed.slash_dots()
    prs.save(out)
    print("\n".join(ed.log)); print("saved", out)


GUNS = {  # Gun.csv 2001~2008 (발사 간격 0.3초 통일, 개성 = 사거리 · 공격력 · 치명)
    "text_42": "공격력 ×1 / 발사 0.3초 / 장전 1.2초 / 12m / 치명 0%",
    "text_49": "공격력 ×1.25 / 발사 0.3초 / 장전 1.4초 / 9m / 치명 5% +30%",
    "text_56": "공격력 ×1.3 / 발사 0.3초 / 장전 1초 / 7m / 치명 10% +10%",
    "text_63": "공격력 ×1 / 발사 0.3초 / 장전 1.2초 / 12m / 치명 6% +15%",
    "text_70": "공격력 ×1 / 발사 0.3초 / 장전 1.3초 / 12m / 치명 6% +15%",
    "text_77": "공격력 ×1 / 발사 0.3초 / 장전 1.8초 / 20m / 치명 8% +45%",
    "text_84": "공격력 ×1.15 / 발사 0.3초 / 장전 1.2초 / 13m / 치명 10% +25%",
    "text_91": "공격력 ×1.2 / 발사 0.3초 / 장전 1초 / 15m / 치명 12% +35%",
}


def gun_deck():
    src = os.path.join(DOCS, "1159_총_기획서_20261125_정승운_Ver03.pptx")
    out = os.path.join(DOCS, "1159_총_기획서_20261127_정승운_Ver04.pptx")
    prs = Presentation(src)
    ed = Editor(prs)
    S = prs.slides
    ed.rep(1, "작성 정승운 / 2026.11.25 / 수치는 1차안", "작성 정승운 / 2026.11.27 / 수치는 1차안")
    ed.rep(1, "기본 수치 / 고유 효과 / 스킬", "기본 수치 / 고유 효과 / 스킬 개념")

    # 02 총 규칙
    ed.rep(2, "기본 수치: 공격력 배율 / 발사 간격 / 장전 시간 / 사거리", "기본 수치: 공격력 배율 / 장전 시간 / 사거리 / 치명 확률 / 치명 피해 (발사 간격은 0.3초 통일, 개성은 사거리로)")
    ed.rep(2, "스킬 1개 (스컬식). 양손 합쳐 2개, 쿨타임 방식", "스킬 1개 (스컬식), 우클릭 / 쿨타임 방식. 양손 중 고유 스킬이 있는 총의 스킬(오른손 먼저), 없으면 백스텝 샷")

    # 03 스킬: 개별 스킬 7개는 기획 전이라 개념만
    s3 = S[2]
    O.set_title(s3, "총 스킬 (개념)")
    keep = {"Text 1"}
    for sh in list(s3.shapes):
        if sh.is_placeholder or sh.name in keep:
            continue
        O.delete(sh)                                                   # 예전 스킬 카드 10장 (글 · 상자 전부)
    from pptx_kit import card, text, chip, bullets, GOLD, LAV, BG, TXT, MUTED, PURPLE
    card(s3, 0.6, 1.6, 5.95, 4.9, fill=PURPLE)
    text(s3, 0.85, 1.75, 5.5, 0.45, [{"t": "스킬 규칙", "b": True, "c": GOLD, "size": 18}])
    bullets(s3, 0.85, 2.3, 5.5, 4.0, [
        "총마다 액티브 스킬 1개 (스컬식 스킬 교체)",
        "우클릭으로 사용 / 쿨타임 방식",
        "양손 중 고유 스킬이 있는 총의 스킬 (오른손 먼저)",
        "고유 스킬이 없는 총 = 공통 스킬 '백스텝 샷'",
        "총을 바꿔 쥐면 스킬도 같이 바뀜 → 빌드 선택 요소",
        "총 고유 효과의 [스킬] 조건은 이 스킬 사용 기준",
    ], size=14.5, after=8)
    card(s3, 6.75, 1.6, 5.95, 1.45)
    text(s3, 7.0, 1.72, 5.5, 0.4, [{"t": "백스텝 샷   기본 총 8종 공통", "b": True, "c": GOLD, "size": 15}])
    text(s3, 7.0, 2.15, 5.5, 0.85, ["재사용 6초 / 조준 반대쪽으로 4m 뒤로 뛰며 2발 / 뛰는 순간 무적"], size=12.5)
    card(s3, 6.75, 3.2, 5.95, 1.45)
    text(s3, 7.0, 3.32, 5.5, 0.4, [{"t": "랜턴 던지기   가스등 랜턴", "b": True, "c": GOLD, "size": 15}])
    text(s3, 7.0, 3.75, 5.5, 0.85, ["재사용 12초 / 반경 4 빛 장판 4초 / 안의 적 둔화 30% / 안에서 터지는 공명 반경 +1"], size=12.5)
    card(s3, 6.75, 4.8, 5.95, 1.7)
    text(s3, 7.0, 4.92, 5.5, 0.4, [{"t": "증기 돌진   기관차", "b": True, "c": GOLD, "size": 15}])
    text(s3, 7.0, 5.35, 5.5, 1.1, ["재사용 10초 / 8m 돌진하며 관통 사격 / 끝에서 증기 폭발 (반경 3 / 피해 150%) / 돌진 중 무적"], size=12.5)
    s3.notes_slide.notes_text_frame.text = "기본 총 8종의 개별 스킬(Skill.csv 6002~6008)은 데이터 자리만 있음. 기획 확정 전까지 백스텝 샷 공통"

    # 04 · 05 기본 총 카드
    for n in (4, 5):
        for sh in S[n - 1].shapes:
            if sh.name in GUNS:
                O.set_text(sh, GUNS[sh.name])
            if sh.has_text_frame and sh.text_frame.text.startswith("스킬: ") and sh.text_frame.text != "스킬: 백스텝 샷":
                O.set_text(sh, "스킬: 백스텝 샷 (공통)")
    ed.rep(4, "스킬: 백스텝 샷", "스킬: 백스텝 샷 (공통)")
    ed.rep(4, "오른손: [튕김] 이 총의 탄이 튕길 때마다 피해 +10%", "오른손: [관통] 이 총의 탄이 적을 꿰뚫을 때마다 은빛 메아리 폭발 (피해 40%, 반경 1.5)")
    ed.rep(4, "왼손: [상시] 이 총의 탄이 1번 튕김", "왼손: [명중] 이 총의 탄이 적 1명 관통")
    ed.add_after(5, "오른손: [명중] 이 총의 탄이 10% 확률로 적을 1초 시간에 가둠", "오른손: [상시] 이 총의 탄이 가장 가까운 적을 따라감")

    # 06 총포상 묶음: 증기 돌진 8m
    ed.rep(6, "조준 방향으로 6m 돌진하며 경로의 적에게 관통 사격", "조준 방향으로 8m 돌진하며 경로의 적에게 관통 사격")
    ed.slash_dots()
    prs.save(out)
    print("\n".join(ed.log)); print("saved", out)


if __name__ == "__main__":
    bullet_deck()
    gun_deck()
