# 공통 레벨 디자인 Ver03 → Ver04 · 1~5장 기획서 새 버전 (2026-11-27 게임과 맞춤)
# - 보스 이름: 2026-11-21 확정 이름 · 칭호 (BossIntro.cs) — 녹은 시계 거인 → 달리오스 등
# - 방 크기: 장 / 스테이지마다 커짐 (RoomBuilder.StageRooms)
# - 공통: 장 이름 (Chapter.csv) · 보상 문 · 레벨 업 후보 연산 · 문 안내 화살표 · 보스 문 첫 진입 = 스토리 맵
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from pptx import Presentation
from pptx_kit import Editor
import deck_ops as O

DOCS = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DATE = "2026.11.27"

BOSS = {   # 장: [(옛 이름, 새 이름, 칭호)]
    1: [("태엽 현상금 사냥꾼", "와인더 잭", "황야의 무법자"), ("녹은 시계 거인", "달리오스", "불타는 시계탑")],
    2: [("태엽 역장", "컨덕터 반", "승강장의 지배자"), ("멈춘 기관차 거인", "로코모스", "탈선한 종착역")],
    3: [("신기루 마술사", "일루시오", "반사의 연출가"), ("거울 고래", "모비 글래스", "호수의 기억")],
    4: [("진자 집행관", "기로틴", "시계탑의 단두대"), ("시침 / 분침 쌍둥이", "크로노 & 템포", "시간의 수문장")],
    5: [("거울 속의 고양이", "미지의 존재", ""), ("시간의 주인", "시간의 주인", "영원의 지배자")],
}
ROOMS = {1: "28×24 → 38×31m", 2: "34×29 → 40×33m", 3: "36×30 → 42×35m"}
FILES = {
    1: ("1159_1장_기획서_20261117_정승운_Ver02.pptx", "02", "03"),
    2: ("1159_2장_기획서_20261125_정승운_Ver04.pptx", "04", "05"),
    3: ("1159_3장_기획서_20261118_정승운_Ver01.pptx", "01", "02"),
    4: ("1159_4장_기획서_20261120_정승운_Ver02.pptx", "02", "03"),
    5: ("1159_5장_기획서_20261119_정승운_Ver01.pptx", "01", "02"),
}


def bump(ed, old, new):
    ed.replace_everywhere(f"Ver{old}", f"Ver{new}")
    ed.replace_everywhere(f"Ver {old}", f"Ver {new}")


def cover_date(prs, ed):
    for sh, p in ed.paras_of(prs.slides[0].shapes):
        t = ed.ptext(p)
        if t.startswith("작성 정승운 / 2026."):
            ed.set_text(p, f"작성 정승운 / {DATE}")


def chapter(ch):
    src, old, new = FILES[ch]
    prs = Presentation(os.path.join(DOCS, src))
    ed = Editor(prs)
    cover_date(prs, ed)
    bump(ed, old, new)
    mid, boss = BOSS[ch]
    # 개요 슬라이드: 이름 칸은 '칭호 이름'
    for (o, n, title) in (mid, boss):
        for sh, p in ed.paras_of(prs.slides[2].shapes):
            if ed.ptext(p) == o:
                ed.set_text(p, f"{title} {n}" if title else n)
        if o != n:
            ed.replace_everywhere(o, n, layouts=False)
    if ch in ROOMS:
        ed.replace_everywhere("방 크기 28×24m /", f"방 크기 {ROOMS[ch]} (스테이지 1 → 4) /", layouts=False)
    ed.slash_dots()
    out = os.path.join(DOCS, f"1159_{ch}장_기획서_20261127_정승운_Ver{new}.pptx")
    prs.save(out)
    print(f"--- {ch}장"); print("\n".join(l for l in ed.log)); print("saved", out)


def common():
    prs = Presentation(os.path.join(DOCS, "1159_공통_레벨디자인_기획서_20261117_정승운_Ver03.pptx"))
    ed = Editor(prs)
    S = prs.slides
    cover_date(prs, ed)
    bump(ed, "03", "04")
    # 04 장 이름 (Chapter.csv)
    O.set_text(O.shape(S[3], "text_59"), "신기루 사막")
    O.set_text(O.shape(S[3], "text_63"), "시계탑 속")
    # 05 보스: 스토리 맵
    ed.add_after(5, "쓰러뜨리면 다음 챕터로 이동", "보스 문에 처음 들어서면 스토리 맵(기억 속 장소)을 거쳐 보스 방으로")
    # 06 스테이지 / 전투방
    O.set_text(O.shape(S[5], "text_116"), "보상 / 다음 보상 문")
    O.set_text(O.shape(S[5], "text_117"), "갈림길 없이 한 줄로 연결 / 보상은 스테이지 끝에서만 지급 / 방을 비우면 다음 방 문에 화살표 안내")
    ed.rep(6, "방 크기 28×24m(1.5~2 화면) / 카메라가 고양이를 따라 이동", "방 크기는 장 / 스테이지마다 커짐 (1장 스테이지 1 28×24m → 5장 스테이지 4 46×39m) / 카메라가 고양이를 따라 이동", partial=True)
    # 08 보상
    O.set_text(O.shape(S[7], "text_179"), "보상 문")
    O.set_text(O.shape(S[7], "text_180"), "3개 중 1개 선택")
    O.set_text(O.shape(S[7], "text_189"), "확정 지급 문")
    O.set_text(O.shape(S[7], "text_190"), "고르기 없이 바로 지급")
    ed.rep(8, "보상은 스테이지 마지막 전투방 클리어 시 지급", "스테이지 끝에 다음 스테이지 보상 문 2~3개 → 고른 문의 보상을 그 스테이지 끝에서 지급")
    # 09 레벨 업 후보
    ed.rep(9, "총 / 방어구 강화 중", "총 / 연산 / 방어구 강화 중", partial=True)
    ed.slash_dots()
    out = os.path.join(DOCS, "1159_공통_레벨디자인_기획서_20261127_정승운_Ver04.pptx")
    prs.save(out)
    print("--- 공통"); print("\n".join(ed.log)); print("saved", out)


if __name__ == "__main__":
    common()
    for c in range(1, 6):
        chapter(c)
