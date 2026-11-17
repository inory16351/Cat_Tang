# 1장 기획서 Ver01 → Ver02: 2026-11-17까지 바뀐 내용 반영 + 어투 통일 (명사형 개조식)
import copy, sys
from pptx import Presentation

SRC, DST = sys.argv[1], sys.argv[2]
prs = Presentation(SRC)
log = []


def paras(slide):
    for sh in slide.shapes:
        if sh.has_text_frame:
            for p in sh.text_frame.paragraphs:
                yield sh, p
        if sh.has_table:
            for row in sh.table.rows:
                for c in row.cells:
                    for p in c.text_frame.paragraphs:
                        yield sh, p


def ptext(p):
    return "".join(r.text for r in p.runs)


def set_text(p, new):
    runs = p.runs
    runs[0].text = new
    for r in runs[1:]:
        r._r.getparent().remove(r._r)


def rep(n, old, new, partial=False):
    """슬라이드 n에서 문단 텍스트가 old인(partial이면 old를 포함한) 문단을 new로"""
    s = prs.slides[n - 1]
    hit = 0
    for sh, p in paras(s):
        t = ptext(p)
        if not p.runs:
            continue
        if (t == old) or (partial and old in t):
            set_text(p, t.replace(old, new) if partial else new)
            hit += 1
    log.append(f"{'OK ' if hit else 'MISS'} s{n}: {old[:30]} → {new[:40]}")


def add_after(n, anchor, new):
    """anchor 문단 뒤에 같은 서식의 문단 추가"""
    s = prs.slides[n - 1]
    for sh, p in paras(s):
        if ptext(p) == anchor:
            np_ = copy.deepcopy(p._p)
            p._p.addnext(np_)
            from pptx.text.text import _Paragraph
            q = _Paragraph(np_, p._parent)
            set_text(q, new)
            log.append(f"ADD s{n}: {new[:40]}")
            return
    log.append(f"MISS-ADD s{n}: {anchor[:30]}")


# ---------- 전체: 버전 · 꼬리말 ----------
for i, s in enumerate(prs.slides, 1):
    for sh, p in paras(s):
        t = ptext(p)
        if p.runs and "Ver01" in t:
            set_text(p, t.replace("Ver01", "Ver02"))
rep(1, "Ver 01", "Ver 02")
rep(1, "작성 정승운 · 2026.11.16", "작성 정승운 · 2026.11.17")

rep(3, "절차적으로 생성한 전투방을 판마다 무작위로 선택", "판마다 전투방을 절차적으로 생성")

# ---------- 5. 챕터 구성 ----------
rep(5, "스테이지 2와 3 사이의 별도 방에서 등장", "스테이지 2 마지막 방 뒤 건물로 걸어 들어가 전투 (1장: 술집)")

rep(5, "전투방 3~4개로 구성", "전투방 4~5개로 구성")   # 2026-11-17 사용자: 게임(4~5개)에 맞춤

# ---------- 6. 스테이지와 전투방 ----------
# 흐름 그림: 전투방 3개 + (전투방) → 4~5개로 읽히게 번호
k = 0
for sh, p in paras(prs.slides[5]):
    t = ptext(p)
    if t == "전투방" and p.runs:
        k += 1; set_text(p, ["전투방 1", "전투방 2~3", "전투방 4"][k - 1] if k <= 3 else t)
    elif t == "(전투방)" and p.runs:
        set_text(p, "(전투방 5)")
log.append(f"OK  s6: 흐름 그림 번호 {k}")
rep(6, "방 크기 1.5~2 화면 / 카메라가 고양이를 따라 이동", "방 크기 28×24m(1.5~2 화면) / 카메라가 고양이를 따라 이동")
add_after(6, "방 크기 28×24m(1.5~2 화면) / 카메라가 고양이를 따라 이동",
          "방에 들어서면 문이 위에서 쾅 닫힘 → 방 클리어 시 문이 부서지며 다음 방으로 길이 열림")

# ---------- 7. 전투방 제작 규칙 ----------
rep(7, "챕터마다 전투방을 절차적으로 생성해 두고 판마다 무작위로 선택", "판마다 전투방을 절차적으로 생성")
rep(7, "스테이지 장소에 맞는 방 중에서만 선택", "큰 지형지물 1~3개로 방 모양 변화 / 벽은 장소에 맞는 모듈(1장: 절벽 · 건물 정면)을 무작위로 이어 붙임")
rep(7, "모든 방은 높낮이 없는 평지 / 지형 변화는 장애물로만 표현", "모든 방은 높낮이 없는 평지 / 지형 변화는 지형지물 · 장애물로만 표현")

# ---------- 8. 보상과 상인 ----------
rep(8, "골드로 총탄 / 총 / 연산 판매", "골드로 총탄 / 총 / 연산 / 하트 판매")
add_after(8, "보상은 스테이지 마지막 전투방 클리어 시 지급",
          "체력 회복: 적 처치 시 낮은 확률로 하트 드롭 (체력 20%) / 중간 보스 처치 시 확정 / 상인 판매")

# ---------- 9. 적 설계 규칙 ----------
add_after(9, "모든 공격 전에 예고 모션", "공격 2~3종을 거리별 가중치로 섞어 사용 / 직전 공격은 확률을 낮춰 반복 방지")
add_after(9, "공격 2~3종을 거리별 가중치로 섞어 사용 / 직전 공격은 확률을 낮춰 반복 방지",
          "배경과 다른 색 계열 + 빨간 바깥 테두리로 한눈에 구분")
rep(9, "겉모습만으로 강화 여부를 바로 알 수 있게", "겉모습만으로 강화 여부 바로 구분")

# ---------- 10 · 11. 용어 ----------
for n in (10, 11):
    s_ = prs.slides[n - 1]
    for sh, p in paras(s_):
        for r in p.runs:
            if "대쉬" in r.text:
                r.text = r.text.replace("대쉬", "대시"); log.append(f"OK  s{n}: 대쉬 → 대시")

for lay in list(prs.slide_layouts) + [prs.slide_master]:
    for sh in lay.shapes:
        if sh.has_text_frame:
            for p in sh.text_frame.paragraphs:
                for r in p.runs:
                    if "Ver01" in r.text:
                        r.text = r.text.replace("Ver01", "Ver02"); log.append("OK  layout footer")

# ---------- 가운데 점(·) → / (2026-11-17 사용자: 가운데 점 전부 / 로) ----------
def slash(t):
    return t.replace(" · ", " / ").replace("· ", "/ ").replace(" ·", " /").replace("·", "/")
n_dot = 0
def fix_runs(shapes):
    global n_dot
    for sh in shapes:
        if sh.shape_type == 6:                      # 그룹
            fix_runs(sh.shapes); continue
        frames = []
        if sh.has_text_frame: frames.append(sh.text_frame)
        if getattr(sh, "has_table", False) and sh.has_table:
            frames += [c.text_frame for row in sh.table.rows for c in row.cells]
        for tf in frames:
            for p in tf.paragraphs:
                for r in p.runs:
                    if "·" in r.text:
                        r.text = slash(r.text); n_dot += 1
for s_ in prs.slides:
    fix_runs(s_.shapes)
    if s_.has_notes_slide:
        for p in s_.notes_slide.notes_text_frame.paragraphs:
            for r in p.runs:
                if "·" in r.text: r.text = slash(r.text); n_dot += 1
for lay in list(prs.slide_layouts) + [prs.slide_master]:
    fix_runs(lay.shapes)
log.append(f"가운데 점 → / : {n_dot}곳")

prs.save(DST)
print("\n".join(log))
