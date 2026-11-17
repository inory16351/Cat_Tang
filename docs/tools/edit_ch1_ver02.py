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

# ---------- 3. 1장 개요 ----------
rep(3, "녹아내린 시계(장식) · 화약통 · 부서지는 엄폐물(마차 · 상자 · 울타리)",
    "녹아내린 시계(장식) · 화약통 · 부서지는 엄폐물 · 방 사이 나무 문 · 회복 하트")
rep(3, "태엽 무법자 · 시계추 깡패 · 모래시계 폭탄병",
    "태엽 무법자 · 시계추 깡패 · 모래시계 폭탄병 (차가운 보라 · 청회색 + 빨간 바깥 테두리)")
rep(3, "고양이 사수의 여정이 시작되는 첫 챕터. ", "고양이 사수의 여정이 시작되는 첫 챕터")
rep(3, "서부극 배경 위로 녹아내린 시계가 하나둘 보이기 시작함", "서부극 배경 위로 녹아내린 시계가 하나둘 드러나는 마을")

rep(3, "일반 적", "일반 몬스터")

# ---------- 4. 스테이지 구성 ----------
rep(4, "술집 앞 대로", "술집 홀")
rep(4, "전투방 10개 절차적 생성 → 스테이지 장소에 맞는 방 중에서 무작위로 뽑음",
    "방마다 큰 지형지물 1~3개로 방 모양 변화 (바위 · 마차 잔해 · 급수탑 · 우물 등)")
add_after(4, "방마다 큰 지형지물 1~3개로 방 모양 변화 (바위 · 마차 잔해 · 급수탑 · 우물 등)",
          "벽: 황무지 = 절벽 · 마을 = 건물 정면 8종을 무작위로 이은 거리")
rep(4, "방 크기 1.5~2 화면 · 방마다 웨이브 2~3번 · 다음 웨이브는 1초 예고 후 등장",
    "방 크기 28×24m · 웨이브 2~3번 · 다음 웨이브 1초 예고")
rep(4, "화약통을 포함한 오브젝트들은 각 맵별로 정해져 있는 후보 자리 중 일부에만 놓임",
    "화약통 등 오브젝트는 방마다 정해진 후보 자리 중 일부에만 배치")
rep(4, "전투방의 전투 종료 시 15% 확률로 한쪽 바닥에서 술집 카운터가 솟아오르며 상인 등장",
    "전투 종료 시 15% 확률로 바닥에서 술집 카운터가 솟아오르며 상인 등장")
rep(4, "황무지 절벽 낭떠러지는 장식 / 보이지 않는 벽으로 막혀 있음", "황무지 절벽 낭떠러지는 장식 · 보이지 않는 벽으로 차단")
add_after(4, "황무지 절벽 낭떠러지는 장식 · 보이지 않는 벽으로 차단",
          "방 사이 나무 문: 전투 시작 시 쾅 닫힘 → 클리어 시 부서짐")

# ---------- 5. 일반 몬스터 ----------
rep(5, "거리를 두고 리볼버로 한 발씩 견제", "6~9m 유지 · 옆걸음하며 단발 · 3점사")
rep(5, "시계추를 들고 일직선으로 돌진", "비스듬히 돌아 들어와 돌진 · 붙으면 내리찍기")
rep(5, "빠르게 달려와 붙은 뒤 폭발", "도약해 자폭하거나 모래 폭탄 투척")
rep(5, "2초마다 1발 (예고 0.5초)", "단발 예고 0.5초 · 3점사 예고 0.6초 (발당 6)")
rep(5, "피해 8 / 체력 24 (기본 탄 3발)", "피해 8 · 체력 24 (기본 탄 3발)")
rep(5, "돌진 예고 0.6초", "돌진 예고 0.6초 · 내리찍기 2.6m")
rep(5, "피해 12 / 체력 48 (기본 탄 6발)", "피해 12 · 체력 48 (기본 탄 6발)")
rep(5, "붙으면 0.8초 점멸 후 반경 2m 폭발", "도약 자폭: 착지 후 0.8초 · 반경 2m")

add_after(5, "도약 자폭: 착지 후 0.8초 · 반경 2m", "모래 폭탄: 예고 1초 · 반경 1.8m · 피해 7")

# ---------- 6. 강화형 ----------
rep(6, "03  일반 적", "03  일반 몬스터")
rep(6, "고양이가 조준하면 옆으로 회피 후 단발 사격", "고양이가 조준하면 옆으로 대시(잔상) 후 한 발 사격")
rep(6, "4초 쿨 / 3m 구르기 / 1발", "4초 쿨 · 3m 대시 · 1발")

# ---------- 7. 중간 보스 ----------
rep(7, "스테이지 2와 3 사이의 별도 방", "스테이지 2 마지막 방 뒤 술집 · 걸어 들어가면 문이 닫히고 전투")
# 줄바꿈(a:br)으로 나뉜 두 줄: 줄마다 run 텍스트를 따로 바꿈
for sh, p in paras(prs.slides[6]):
    rs = p.runs
    if len(rs) == 2 and rs[0].text == "술집 앞 큰길 30×14m ":
        rs[0].text = "술집 홀 28×24m · 판자 벽"
        rs[1].text = "벽 = 돌진 태클 기절에 활용 · 승리 시 뒷문 복도로 스테이지 3 연결"
        log.append("OK  s7: 보스 맵 두 줄")
rep(7, "산탄 사거리를 잡으려고 계속 다가오며 압박 (이속 4m/초)", "산탄 사거리를 잡으려고 계속 다가오는 압박 (이속 4m/초)")
rep(7, "태엽 무법자들의 대장. 산탄총을 사용해 접근을 제지하며 전투", "태엽 무법자들의 대장. 산탄총으로 접근을 막는 근거리 압박형")

# ---------- 8. 현상금 표식 ----------
rep(8, "숫자 표식(2~4)이 붙음", "숫자 표식(2~4) 부착")
rep(8, "그동안 쏘거나 장전해서", "그동안 쏘거나 장전해")
rep(8, "바늘 위치를 옮김", "바늘 위치 조정")
rep(8, "장전하면 첫 칸(1시)으로 돌아감", "장전하면 첫 칸(1시)으로 복귀")
rep(8, "지금 바늘은 5시 → 한발 격발 시 6시 판정", "현재 바늘 5시 → 한 발 쏘면 6시 판정(피해)")

# ---------- 9. 중간 보스 패턴 ----------
rep(9, "앞으로 산탄을 부채꼴로 발사. 가까울수록 많이 맞음", "앞으로 산탄을 부채꼴로 발사. 가까울수록 명중 수 증가")
rep(9, "한 바퀴 반을 돌며 양쪽으로 쉬지 않고 공격", "한 바퀴 반을 돌며 양쪽으로 연사")
rep(9, "휘파람을 불어 태엽 무법자를 불러냄", "휘파람으로 태엽 무법자 호출")
rep(9, "하수인 호출은 거리와 상관없이 동일 가중치 부여(단 필드의 몬스터가 2명 이하일때만 사용)",
    "하수인 호출은 거리 무관 동일 가중치 (필드 몬스터 2명 이하일 때만)")

# ---------- 10. 보스 ----------
rep(10, "보스맵", "보스 맵")
rep(10, "느리게 걸어서 고양이를 추적 (이속 3m/초)", "느린 걸음으로 고양이 추적 (이속 3m/초)")
rep(10, "1장의 출구를 지키는 시간의 존재. 녹아내린 회중시계 얼굴에 시각을 띄우고, 거대한 시침과 분침을 휘두름",
    "1장의 출구를 지키는 시간의 존재. 녹아내린 회중시계 얼굴에 시각을 띄우고 거대한 시침 · 분침을 휘두르는 거인")

# ---------- 11. 시각 패턴 ----------
rep(11, "예고 동안 보스는 멈추고 초침 탄만 계속 → 바늘을 맞추려고 쏜 탄도 보스에게 들어감",
    "예고 동안 보스는 멈추고 초침 탄만 계속 → 바늘을 맞추려고 쏜 탄도 보스에게 명중")
rep(11, "발사 시각: 쏘면 한 칸 넘어가고, 장전하면 첫 칸으로 돌아감", "발사 시각: 쏘면 한 칸 이동 · 장전하면 첫 칸으로 복귀")

# ---------- 12. 상시 탄막 ----------
rep(12, "녹은 웅덩이 솟기", "녹은 웅덩이 소환")
rep(12, "패턴과 별개로 전투 내내 계속", "패턴과 별개로 전투 내내 지속")
rep(12, "거리에 따라 동일확률로 패턴 발동", "거리에 따라 동일 확률로 패턴 발동")

# ---------- 13. 보스 패턴 ----------
rep(13, "1시부터 12시까지 차례로 쏴서 나선형태로 탄막 사격", "1시부터 12시까지 차례로 쏘는 나선 탄막")
rep(13, "원 안의 시간이 느려지고 그 안으로 탄이 쏟아짐", "원 안의 시간이 느려지고 탄이 쏟아지는 구역")
rep(13, "웅덩이: 원 밖으로 대쉬, 남은 웅덩이는 피해 다님", "웅덩이: 원 밖으로 대시 · 남은 웅덩이 회피")
rep(13, "웅덩이 지속시간 6초 · 밟으면 이속 30% 감소", "웅덩이 지속 6초 · 밟으면 이속 30% 감소")

# ---------- 14. 수치 ----------
rep(14, "06  1장 수치", "06  수치 기획")
rep(14, "2초마다 1발 · 예고 0.5초", "단발 · 3점사(발당 6)")
rep(14, "돌진 예고 0.6초", "돌진 · 내리찍기 2.6m")
rep(14, "0.8초 점멸 후 반경 2m 폭발", "도약 자폭 2m · 모래 폭탄 1.8m")
rep(14, "목표 60초 · 경기장 30×14m", "목표 60초 · 술집 홀 28×24m")
add_after(14, "상인 15% · 챕터당 최소 1번", "하트 체력 20% · 드롭 6~10%")
rep(3, "술집 바텐더", "술집 바텐더 · 하트 판매(25골드, 3개)")
rep(7, "1080 (목표 처치 시간 60초)", "1080 (목표 처치 시간 60초) · 처치 시 하트 확정")

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
