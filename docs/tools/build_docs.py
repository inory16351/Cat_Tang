# 데이터 테이블(build_tables.py)에서 총탄 · 총 · 코스튬 기획서(md)와 PPT용 JSON을 만든다
import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import build_tables as T

DOCS = os.path.join(T.ROOT, "docs")
AUTHOR = "정승운"
GRADE_KO = {g[0]: g[1] for g in T.GRADE}
EFF = {e[0]: e for e in T.E}
TRAIT = {t[0]: t[1] for t in T.TRAIT}
SKILL = {s[0]: s for s in T.SKILL}
COND_KO = {"Odd": "홀수", "Even": "짝수", "Prime": "소수", "Negative": "음수", "Zero": "0시"}
TRIG_KO = {"Passive": "상시", "RunStart": "런 시작", "OnFire": "발사", "OnHit": "명중", "OnKill": "처치", "OnReload": "장전",
           "OnPerfectReload": "완벽 장전", "OnReloadEnd": "장전 끝", "OnChanceDodge": "확률 회피", "OnJustDodge": "저스트 회피", "OnSkill": "스킬",
           "OnSilhouette": "실루엣 등장", "OnTimeLockEnd": "가둠 해제", "OnExecute": "처형", "OnRicochet": "튕김", "OnPierce": "관통"}
UNLOCK_KO = {"Default": "처음부터", "ClearZone": "구역 {v} 첫 클리어", "ExecuteCount": "누적 처형 {v}회",
             "PerfectReloadCount": "누적 완벽 장전 {v}회", "JustDodgeCount": "누적 극한 회피 {v}회", "KillBoss": "구역 {v} 보스 처치", "TraitTier6": "특성 6단계 {v}회 달성"}

def cond_text(ct, cv):
    if ct in COND_KO: return COND_KO[ct]
    if ct == "None": return "없음 (기본 탄)"
    return {"MultipleOf": f"{cv}의 배수", "Exact": f"{cv}시", "GreaterThan": f"{cv} 초과", "LessThan": f"{cv} 미만"}[ct]

def eff_text(eid):
    if not eid: return ""
    e = EFF[eid]
    return f"[{TRIG_KO[e[1]]}] {e[10]}"

def hand_list(g):
    """총 손별 효과: 오른손 (8, 9) · 왼손 (10, 11)"""
    r = [eff_text(x) for x in g[8:10] if x]
    l = [eff_text(x) for x in g[10:12] if x]
    return ([f"오른손: {' / '.join(r)}"] if r else []) + ([f"왼손: {' / '.join(l)}"] if l else [])

def hand_text(g):
    return "<br>".join(hand_list(g)) or "없음"

def table(head, rows):
    out = ["| " + " | ".join(head) + " |", "|" + "---|" * len(head)]
    out += ["| " + " | ".join(str(c) for c in r) + " |" for r in rows]
    return "\n".join(out)

def schema(sheet):
    cols = next(s[1] for s in T.SHEETS if s[0] == sheet)
    return table(["열", "타입"], [c.split(":") for c in cols])

HEADER = f"- 작성: {AUTHOR} · 2026-11-14 · 상태: 초안\n- 표기: **[확정]** = 사용자가 정한 것 · **[1차안]** = 수치 · 목록 1차본 (플레이 테스트로 조정)\n- 데이터: `docs/data/Cat_Tang_Data.xlsx` (관리) · `Game/Assets/Data/Tables/*.csv` (게임) · 생성 스크립트 `docs/tools/build_tables.py`\n"

# ---------------------------------------------------------------- 총탄 기획서
def bullet_doc():
    s = [f"# Cat_Tang 총탄 기획서\n\n{HEADER}\n---\n"]
    s.append("""## 1. 개요 [확정]
- 탄 = 아이템, 실린더 12칸 = 인벤토리. 놓는 자리가 곧 빌드.
- 탄 하나 = **자리 조건 1개 + 특성 2개 + 고유 효과 + 공격력 + 연산 슬롯 0~2칸.**
- 판정 시각(놓인 시각에 연산 적용)이 자리 조건에 맞으면 **활성화.**
  - 활성화된 탄: 특성 + 고유 효과 + 공격력
  - 못 활성화된 탄: 공격력만
- **적용 범위**
  - 탄 고유 효과 [확정]: 활성화되어 있으면 **모든 사격에 적용** (발사 · 명중 · 처치 · 장전 · 상시). 같은 효과는 활성화된 탄 수만큼 겹친다.
  - **그 탄을 직접 쏠 때는 강화**: 효과가 한 번 더 걸리고(확률 · 위력 2배) **공명 사격**(굵은 두 겹 궤적 · 금빛 섬광 · 맞은 자리 탄 색 폭발 피해 30% · 화면 흔들림)이 나간다.
  - 특성: 단계는 **활성화된 탄**의 특성으로 세고, 켜진 효과는 **모든 탄**(비활성화된 탄 포함)에 적용.
  - 총 고유 효과: 그 총이 담당하는 칸(오른손 홀수 · 왼손 짝수)의 탄에만. 스킬 · 장전 끝처럼 탄과 무관한 효과만 전역 (총 기획서).
- 이름은 시간과 관련된 몽환적 · 시적 표현. "~탄"으로 통일하지 않고, 등급이 높을수록 고유한 이름.
- 아이콘도 같은 방향: 꿈속 같은 시적 분위기, 탄마다 색 · 재질 · 실루엣이 확실히 다름.
""")
    s.append("## 2. 등급 [확정]\n")
    s.append(table(["등급", "탄 수", "고유 효과", "드랍 가중치 [1차안]", "색"],
                   [(g[1], sum(1 for b in T.B if b[2] == g[0]), "1개" if g[0] in ("Common", "Rare") else "최대 2개", g[3], f"#{g[2]}") for g in T.GRADE]))
    s.append("\n\n- 등급이 높을수록 고유 효과가 복잡해진다 (세피리아 · 스컬 아이템 방식).\n  - 일반: 단순한 한 줄 효과\n  - 희귀: 조건이 붙은 효과\n  - 영웅: 다른 칸 · 다른 시스템과 엮이는 효과\n  - 전설: 규칙을 바꾸는 효과\n- 연산 슬롯 수는 등급과 무관. 효과가 강한 탄은 슬롯이 적을 수 있다.\n")
    s.append("\n## 3. 자리 조건 [확정]\n")
    s.append(table(["CondType", "뜻"], [(k, d) for k, d in T.ENUMS["PlaceCond"]]))
    s.append("\n\n- 판정 규칙(12 초과 · 0 이하 값 유지, 반올림, 0시 만능 등)은 컨셉 기획서 8.2.\n")
    s.append("\n## 4. 특성 (9종) [확정 · 수치는 1차안]\n- 같은 특성의 활성화된 탄이 2 · 4 · 6개면 단계 상승. 2 · 4는 수치, 6은 새 효과.\n\n")
    rows = []
    for t in T.TRAIT:
        tiers = {2: [], 4: [], 6: []}
        for row in T.TRAIT_TIER:
            if row[1] == t[0]: tiers[row[2]].append(EFF[row[3]][10])
        six = tiers[6][-1]
        rows.append((t[1], t[2], tiers[2][0], tiers[4][0], six))
    s.append(table(["특성", "느낌", "2개", "4개", "6개 (새 효과)"], rows))
    s.append("\n\n## 5. 고유 효과 발동 시점 [확정]\n- 발사 시 · 명중 시 · 처치 시 · 장전 시. 그 밖에 상시(Passive)와 회피 · 처형 같은 상황 트리거를 쓴다.\n- 효과 데이터는 Effect 테이블에 `발동 시점 + 조건 + 대상 + 타입 + 값`으로 기록.\n")
    s.append(f"\n## 6. 탄 목록 ({len(T.B) - 1}종 + 기본 탄) [1차안]\n\n- 1037~1046은 능력치(치명타 · 방어력 · 발동 확률 · 이동 속도) · 피해 유형(보스 · 범위 · 상태 이상) 탄. 아이콘 시트: `images/icons/sheet_bullets_stats.png`\n")
    for g in T.GRADE:
        s.append(f"\n### {g[1]}\n\n![{g[1]} 아이콘 시트](images/icons/sheet_bullets_{g[0].lower()}.png)\n\n")
        rows = []
        for b in T.B:
            if b[2] != g[0]: continue
            effs = " / ".join(x for x in (eff_text(b[9]), eff_text(b[10])) if x)
            icon = f'<img src="images/icons/bullets_toon/bullet_{b[0]}.png" width="48">'
            rows.append((icon, b[0], f"**{b[1]}**", cond_text(b[4], b[5]), (f"{TRAIT[b[6]]} · {TRAIT[b[7]]}" if b[6] else "-"), b[8], b[3], effs))
        s.append(table(["", "Id", "이름", "자리 조건", "특성", "슬롯", "공격력", "고유 효과"], rows))
        s.append("\n")
    s.append("\n## 7. 연산 (10종) [확정 범위 · 가중치는 1차안]\n\n")
    s.append(table(["Id", "연산", "드랍 가중치"], [(o[0], o[1], o[4]) for o in T.OP]))
    s.append("\n\n## 8. 데이터 테이블\n")
    for sh in ("Bullet", "Trait", "TraitTier", "Effect", "Op"):
        s.append(f"\n### {sh}\n\n{schema(sh)}\n")
    s.append("\n## 9. 정해야 할 것\n- [ ] 수치 조정 (플레이 테스트)\n- [ ] 아이콘 최종본\n- [ ] 탄 드랍 위치 (보상 방 · 상점 · 보스)\n")
    return "\n".join(s) if False else "".join(x if x.endswith("\n") else x + "\n" for x in s)

# ---------------------------------------------------------------- 총 기획서
def gun_doc():
    s = [f"# Cat_Tang 총 기획서\n\n{HEADER}\n---\n"]
    s.append("""## 1. 개요 [확정]
- 총도 드랍 아이템. 아무 손에나 장착 (오른손 = 홀수 칸, 왼손 = 짝수 칸 담당).
- 총마다 다른 것: **기본 수치 · 고유 효과 · 스킬 1개** (스컬의 머리처럼). 양손 합쳐 스킬 2개.
- **손별 효과 [확정]:** 같은 총도 오른손 효과와 왼손 효과가 다르다 (오른손 = 공격, 왼손 = 보조 쪽). 기본 총(보안관의 리볼버)은 양손 모두 효과 없음.
- **효과 적용 범위 [확정]:** 그 손이 담당하는 칸의 탄에만 (오른손 = 홀수 칸, 왼손 = 짝수 칸). 스킬 · 장전 끝처럼 탄과 무관한 효과만 전역 (`[전역]` 표시).
- **양손 2자루만 [확정]:** 보관함에 넣거나 팔 수 없다. 주우면 교체 창에서 왼손에 쥐기 · 오른손에 쥐기 · 버리기. 빠진 총 · 버린 총은 바닥에 떨어져 다시 주울 수 있다. 시작은 양손 기본 총.
- 총에는 연산 슬롯이 없다 (연산은 탄에).
- 등급 4단계 (일반 · 희귀 · 영웅 · 전설). 높을수록 고유 효과가 복잡.
- 교체는 전투 밖에서만.

## 2. 기본 수치 [1차안]
| 열 | 뜻 |
|---|---|
| DamageMul | 탄 공격력 배율 |
| FireInterval | 발사 간격 (초) — 모든 총 0.3초로 통일 (2026-11-18), 총 개성은 사거리로 |
| ReloadTime | 장전 시간 (초) |
| Range | 사거리 (m) — 이보다 먼 적은 안 맞음. 조준 커서가 사거리 밖이면 흐려지고 사거리 끝에 빨간 점 |

## 3. 스킬 [확정: 쿨타임 방식]
""")
    s.append(table(["Id", "스킬", "쿨타임", "내용"], [(k[0], k[1], f"{k[3]}초", k[7]) for k in T.SKILL]))
    s.append("\n\n## 4. 총 목록 (8종) [1차안]\n\n![총 아이콘 시트](images/icons/sheet_guns.png)\n\n")
    rows = []
    for g in T.GUN:
        effs = hand_text(g)
        icon = f'<img src="images/icons/guns_toon/gun_{g[0]}.png" width="64">'
        rows.append((icon, g[0], f"**{g[1]}**", GRADE_KO[g[2]], f"×{g[3]}", f"{g[6]:g}m", f"{g[5]}초", SKILL[g[7]][1], effs))
    s.append(table(["", "Id", "이름", "등급", "공격력", "사거리", "장전", "스킬", "고유 효과"], rows))
    s.append("\n\n## 5. 데이터 테이블\n")
    for sh in ("Gun", "Skill"):
        s.append(f"\n### {sh}\n\n{schema(sh)}\n")
    s.append("\n## 6. 정해야 할 것\n- [ ] 수치 조정 (플레이 테스트)\n- [ ] 아이콘 · 3D 모델\n- [ ] 총 드랍 위치\n")
    return "".join(x if x.endswith("\n") else x + "\n" for x in s)

# ---------------------------------------------------------------- 코스튬 기획서
def costume_doc():
    s = [f"# Cat_Tang 코스튬 기획서\n\n{HEADER}\n---\n"]
    s.append("""## 1. 개요 [확정]
- 코스튬(복장) = 해금 요소. 거점(서부 여관 2층 방)에서 한 벌 골라 런 시작.
- 첫 버전 **8벌.**
- 효과 종류: **능력치 보정 · 시작 아이템 · 규칙 변경.** 장점과 단점을 함께 둬서 런의 출발점을 바꾼다.
- 해금: **업적 조건 달성 → 시간의 조각으로 구매.**
- 외형: 몸은 같고 **옷 · 모자 메시를 교체** (실루엣이 달라짐).

## 2. 코스튬 목록 (8벌) [1차안]

""")
    rows = []
    for c in T.COSTUME:
        effs = " / ".join(EFF[e][10] for e in c[5:8] if e) or "없음"
        rows.append((c[0], f"**{c[1]}**", UNLOCK_KO[c[2]].format(v=c[3]), c[4], effs, c[9]))
    s.append(table(["Id", "이름", "해금 조건", "시간의 조각", "효과", "한 줄"], rows))
    s.append("\n\n## 3. 데이터 테이블\n\n### Costume\n\n" + schema("Costume") + "\n")
    s.append("\n## 4. 정해야 할 것\n- [ ] 수치 조정\n- [ ] 코스튬 디자인 시안 (옷 · 모자)\n- [ ] 해금 조건 난이도\n")
    return "".join(x if x.endswith("\n") else x + "\n" for x in s)

def ppt_json():
    return {
        "author": AUTHOR,
        "grades": [{"key": g[0], "name": g[1], "color": g[2], "weight": g[3], "count": sum(1 for b in T.B if b[2] == g[0])} for g in T.GRADE],
        "traits": [{"name": t[1], "desc": t[2], "t2": EFF[[r for r in T.TRAIT_TIER if r[1] == t[0] and r[2] == 2][0][3]][10],
                    "t4": EFF[[r for r in T.TRAIT_TIER if r[1] == t[0] and r[2] == 4][0][3]][10],
                    "t6": EFF[[r for r in T.TRAIT_TIER if r[1] == t[0] and r[2] == 6][-1][3]][10]} for t in T.TRAIT],
        "bullets": [{"id": b[0], "name": b[1], "grade": b[2], "dmg": b[3], "cond": cond_text(b[4], b[5]),
                     "traits": (f"{TRAIT[b[6]]} · {TRAIT[b[7]]}" if b[6] else "-"), "slots": b[8],
                     "effects": [eff_text(x) for x in (b[9], b[10]) if x]} for b in T.B],
        "ops": [{"name": o[1], "weight": o[4]} for o in T.OP],
        "skills": [{"name": k[1], "cd": k[3], "desc": k[7]} for k in T.SKILL],
        "guns": [{"id": g[0], "name": g[1], "grade": g[2], "mul": g[3], "interval": g[4], "reload": g[5], "range": g[6],
                  "skill": SKILL[g[7]][1], "effects": hand_list(g), "desc": g[13]} for g in T.GUN],
        "costumes": [{"id": c[0], "name": c[1], "unlock": UNLOCK_KO[c[2]].format(v=c[3]), "cost": c[4],
                      "effects": [EFF[e][10] for e in c[5:8] if e], "desc": c[9]} for c in T.COSTUME],
    }

if __name__ == "__main__":
    for name, fn in (("Cat_Tang_총탄_기획서.md", bullet_doc), ("Cat_Tang_총_기획서.md", gun_doc), ("Cat_Tang_코스튬_기획서.md", costume_doc)):
        p = os.path.join(DOCS, name)
        text = fn()   # 먼저 만들고 씀 (2026-11-18: 만들다 오류가 나면 파일이 비던 문제)
        open(p, "w", encoding="utf-8").write(text)
        print("wrote", p)
    jp = os.path.join(DOCS, "data", "items_for_ppt.json")
    json.dump(ppt_json(), open(jp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("wrote", jp)
