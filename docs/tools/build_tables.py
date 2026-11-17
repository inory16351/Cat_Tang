# Cat_Tang 데이터 테이블 생성기
# - 관리용: docs/data/Cat_Tang_Data.xlsx (시트 여러 개)
# - 게임용: Game/Assets/Data/Tables/*.csv (UTF-8)
# 규칙: 열 타입은 string · enum · int · float만. 효과는 Effect 테이블(Type + Value)로 분리.
# ID 구간: 탄 1001~ · 총 2001~ · 특성 3001~ · 코스튬 4001~ · 연산 5001~ · 스킬 6001~ · 효과 9001~
import csv, os, sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
XLSX = os.path.join(ROOT, "docs", "data", "Cat_Tang_Data.xlsx")
CSV_DIR = os.path.join(ROOT, "Game", "Assets", "Data", "Tables")

# ---------------------------------------------------------------- enum
ENUMS = {
    "Grade": [
        ("Common", "일반"), ("Rare", "희귀"), ("Epic", "영웅"), ("Legendary", "전설"),
    ],
    "PlaceCond": [
        ("Odd", "판정 시각이 홀수"), ("Even", "짝수"), ("MultipleOf", "CondValue의 배수 (0 · 음수 포함)"),
        ("Prime", "소수 (2 · 3 · 5 · 7 · 11 · 13 …)"), ("Negative", "음수"), ("Zero", "0시"),
        ("Exact", "CondValue와 같음"), ("GreaterThan", "CondValue 초과"), ("LessThan", "CondValue 미만"),
        ("None", "조건 없음 (깨어나지 않는 기본 탄)"),
    ],
    "Trigger": [
        ("Passive", "깨어나 있는 동안 항상"), ("RunStart", "런 시작 때 1번"), ("OnFire", "이 탄을 쏠 때"),
        ("OnHit", "이 탄이 맞았을 때"), ("OnKill", "이 탄으로 처치했을 때"), ("OnReload", "장전할 때"),
        ("OnReloadEnd", "장전이 끝날 때"), ("OnChanceDodge", "확률 회피에 성공했을 때"),
        ("OnJustDodge", "저스트 회피 때"), ("OnSkill", "총 스킬을 쓸 때"), ("OnSilhouette", "회상 실루엣이 나타날 때"),
        ("OnTimeLockEnd", "시간에 갇힌 적이 풀릴 때"), ("OnExecute", "처형했을 때"),
        ("OnRicochet", "탄이 튕길 때"), ("OnPierce", "탄이 관통할 때"),
    ],
    "EffectCond": [
        ("None", "조건 없음"), ("Slot12", "12시 칸에 있을 때"), ("TargetMarked", "대상에 추억 표식이 있음"),
        ("MarkStacksAtLeast", "대상의 추억 표식이 CondValue개 이상"), ("TargetSlowed", "대상이 둔화 상태"),
        ("TargetTimeLocked", "대상이 시간에 갇힘"), ("TargetBoss", "대상이 보스"),
        ("TargetHpBelow", "대상 체력이 CondValue% 이하"), ("JudgeEven", "판정 시각이 짝수"),
        ("JudgeNegative", "판정 시각이 음수"), ("JudgeOver", "판정 시각이 CondValue 초과"),
        ("JudgeAtMost", "판정 시각이 CondValue 이하"), ("KilledByDelayed", "지연 피해로 처치"),
    ],
    "Target": [
        ("Self", "자신(플레이어)"), ("HitTarget", "맞은 적"), ("Area", "주변 적 (반경 = Value2)"),
        ("NextBullet", "다음 차례 탄"), ("Neighbors", "양옆 칸 탄"), ("AllBullets", "실린더의 모든 탄"),
        ("OwnBullets", "이 총이 담당하는 칸의 탄"),
    ],
    "EffectType": [
        ("DamageAdd", "피해 +Value (비율)"), ("AttackSpeedAdd", "연사 속도 +Value"), ("Knockback", "밀쳐냄 (거리 Value)"),
        ("ExtraShot", "탄 Value발 추가 (피해 Value2)"), ("Ricochet", "Value번 튕김"), ("RicochetByJudgeTime", "판정 시각 절댓값만큼 튕김 (최대 Value)"),
        ("Pierce", "Value명 관통"), ("PierceDamageGrowth", "관통할 때마다 피해 +Value"), ("Split", "Value개로 갈라짐 (피해 Value2)"),
        ("Slow", "둔화 Value (지속 Duration)"), ("Stun", "경직 (반경 Value2, 지속 Duration)"), ("TimeLock", "시간에 가둠: 정지 Duration, 받는 피해 +Value"),
        ("Burn", "초당 피해 Value (지속 Duration)"), ("DefenseDown", "방어력 -Value (지속 Duration)"), ("EnemyAttackSpeedDown", "적 공격 속도 -Value (지속 Duration)"),
        ("Explosion", "폭발 피해 Value (반경 Value2)"), ("DelayedDamage", "Duration초 뒤 피해 Value (Value2 > 0이면 그 반경으로 퍼짐)"),
        ("Homing", "적을 따라감"), ("SilhouetteShot", "고양이 실루엣이 1발 더 (피해 Value)"), ("SilhouetteFollow", "실루엣이 Duration초 동안 사격을 따라 함"),
        ("MemoryMark", "추억 표식 1개, 표식당 그 적에게 주는 피해 +Value (최대 Value2개)"), ("MarkBurst", "표식을 터뜨려 피해 Value"),
        ("DodgeChance", "피해를 Value 확률로 회피 (지속 Duration, 0이면 상시)"), ("BulletRefund", "이 칸의 탄을 다시 장전 (Value발)"),
        ("Execute", "일반 적 처형, 보스는 피해 +Value"), ("HeatGain", "열기 +Value칸"), ("JustDodgeWindowAdd", "저스트 회피 판정 +Value"),
        ("CritNextShots", "다음 Value발 치명타"), ("CritChance", "치명타 확률 +Value"), ("CritChanceAdd", "[능력치] 치명타 확률 +Value"), ("CritDamageAdd", "[능력치] 치명타 피해 +Value"), ("DefenseAdd", "[능력치] 방어력 +Value"), ("ProcChanceAdd", "[능력치] 효과 발동 확률 +Value (배율)"), ("MoveSpeedAdd", "[능력치] 이동 속도 +Value"), ("BossDamageAdd", "보스에게 주는 피해 +Value"), ("AreaDamageAdd", "폭발 · 범위 피해 +Value"), ("StatusDamageAdd", "상태 이상(정지 · 둔화 · 가둠 · 화상 · 표식) 적에게 주는 피해 +Value"), ("ControlDurationAdd", "정지 · 둔화 지속 +Value"),
        ("DamageVsStoppedAdd", "정지된 적에게 피해 +Value"), ("UnawakenedDamageAdd", "깨어나지 못한 탄 공격력 +Value"),
        ("AwakenedDamageAdd", "깨어난 탄 공격력 +Value"), ("UnawakenedTraitCount", "깨어나지 못한 탄도 특성 Value개 켜짐"),
        ("NextBulletAttackSpeedAdd", "다음 탄 연사 속도 +Value"), ("TrailSilhouetteShot", "맞은 자리에 궤적이 남고 Duration초 뒤 실루엣이 그 궤적으로 1발 (피해 Value)"),
        ("TraitCountBonus", "특성 Value2를 Value개 더 셈"),
        ("JudgeTimeAdd", "판정 시각 +Value"), ("InstantReload", "즉시 장전"), ("TimeSlowField", "주변 시간 감속 Value (반경 Value2, 지속 Duration)"),
        ("GlobalTimeStop", "모든 적 정지 Duration초 (재사용 대기 Value2초)"), ("ClockBurst", "시계 12방향으로 1발씩, 각 방향은 그 시각 칸의 탄 (피해 Value)"),
        ("DamageByNegativeJudge", "판정 시각이 0보다 1 낮을 때마다 피해 +Value"), ("DamagePerUnawakened", "깨어나지 못한 탄 1개당 피해 +Value"),
        ("ReverseCylinder", "다음 장전은 12 → 1 역순 발사"), ("PermanentDamageGrowth", "런 동안 이 탄 공격력 +Value (최대 Value2)"),
        ("DamageByDistance", "거리 1m당 피해 +Value (최대 Value2)"), ("DamageAddStack", "누적될 때마다 피해 +Value"),
        ("DamageTakenAdd", "받는 피해 +Value"), ("MaxHpAdd", "최대 체력 +Value (비율)"), ("GrantRandomOp", "무작위 연산 Value개 지급"),
        ("GrantBullet", "탄 Value(Id) 지급"), ("OpDropRateAdd", "연산 드랍 +Value"), ("TraitThresholdReduce", "특성 6단계 필요 수 -Value"),
        ("MemoryShardGainAdd", "시간의 조각 획득 +Value"), ("DodgeCooldown", "대시 충전 시간 +Value (비율, 음수 = 빨라짐)"),
    ],
    "OpType": [("Add", "더하기"), ("Sub", "빼기"), ("Mul", "곱하기"), ("Div", "나누기 (반올림, .5는 0에서 먼 쪽)")],
    "SkillAction": [
        ("BackstepShot", "뒤로 도약하며 조준 방향으로 사격"), ("TurnShot", "앞을 쏘고 홱 돌아 뒤를 쏨"),
        ("VaultShot", "적을 뛰어넘으며 아래로 사격"), ("Parry", "적 투사체를 쏴서 튕겨 냄"), ("SlowField", "모래 장판"),
        ("ChargeShot", "모아서 관통 1발"), ("SummonSilhouette", "실루엣 소환"), ("TimeStop", "범위 시간 정지"),
    ],
    "UnlockType": [
        ("Default", "처음부터"), ("ClearZone", "구역 CondValue 첫 클리어"), ("ExecuteCount", "누적 처형 수"),
        ("JustDodgeCount", "누적 극한 회피 수"), ("KillBoss", "구역 Value 보스 처치"), ("TraitTier6", "특성 6단계 달성 횟수"),
    ],
}

# ---------------------------------------------------------------- 등급
GRADE = [
    # Grade, Name, Color, BulletDropWeight, GunDropWeight
    ("Common", "일반", "9AA0A6", 60, 55),
    ("Rare", "희귀", "4F8CE0", 28, 30),
    ("Epic", "영웅", "9B5DE5", 10, 12),
    ("Legendary", "전설", "E0A63A", 2, 3),
]

# ---------------------------------------------------------------- 효과
# Id, Trigger, Cond, CondValue, Chance, Target, Type, Value, Value2, Duration, Desc
E = []
def eff(i, trig, typ, val=0.0, val2=0.0, dur=0.0, chance=1.0, target="Self", cond="None", cv=0, desc=""):
    E.append((i, trig, cond, cv, chance, target, typ, val, val2, dur, desc))

# 특성 단계 효과 (9101~)
eff(9101, "OnFire", "SilhouetteShot", 0.5, chance=0.10, desc="탄을 쏠 때 10% 확률로 고양이 실루엣이 같은 방향으로 1발 (피해 50%)")
eff(9102, "OnFire", "SilhouetteShot", 0.5, chance=0.20, desc="실루엣 확률 20%")
eff(9103, "OnSilhouette", "SilhouetteFollow", dur=3.0, desc="실루엣이 3초 동안 남아 내 사격을 따라 함")
eff(9111, "OnHit", "MemoryMark", 0.03, 10, target="HitTarget", desc="맞힐 때마다 추억 표식, 표식당 그 적에게 피해 +3% (최대 10개)")
eff(9112, "OnHit", "MemoryMark", 0.05, 10, target="HitTarget", desc="표식당 +5%")
eff(9113, "OnHit", "MarkBurst", 3.0, target="HitTarget", cond="MarkStacksAtLeast", cv=10, desc="표식 10개인 적을 맞히면 표식을 터뜨려 피해 300%")
eff(9121, "OnHit", "TimeLock", 0.30, dur=1.0, chance=0.08, target="HitTarget", desc="8% 확률로 시간 속에 가둠: 1초 정지, 받는 피해 +30%")
eff(9122, "OnHit", "TimeLock", 0.30, dur=1.0, chance=0.15, target="HitTarget", desc="확률 15%")
eff(9123, "OnTimeLockEnd", "Explosion", 1.0, 3.0, target="Area", desc="갇힌 적이 풀릴 때 시간 파편 폭발 (피해 100%, 반경 3)")
eff(9131, "Passive", "DodgeChance", 0.08, desc="피해를 8% 확률로 회피 (잠깐 되감기는 연출)")
eff(9132, "Passive", "DodgeChance", 0.15, desc="회피 15%")
eff(9133, "OnChanceDodge", "BulletRefund", 1, desc="확률 회피에 성공하면 현재 칸 탄을 다시 장전")
eff(9141, "OnHit", "Execute", 1.0, chance=0.05, target="HitTarget", cond="TargetHpBelow", cv=20, desc="체력 20% 이하 일반 적을 5% 확률로 처형, 보스에게는 그 확률로 피해 +100%")
eff(9142, "OnHit", "Execute", 1.0, chance=0.10, target="HitTarget", cond="TargetHpBelow", cv=20, desc="확률 10%")
eff(9143, "OnExecute", "HeatGain", 1, desc="처형하면 열기 1칸")
eff(9151, "OnHit", "DelayedDamage", 0.30, dur=1.0, target="HitTarget", desc="1초 뒤 피해 30% 한 번 더")
eff(9152, "OnHit", "DelayedDamage", 0.60, dur=1.0, target="HitTarget", desc="1초 뒤 피해 60%")
eff(9153, "OnHit", "DelayedDamage", 0.60, 2.5, 1.0, target="HitTarget", desc="1초 뒤 피해 60%가 반경 2.5로 퍼짐")
eff(9161, "Passive", "JustDodgeWindowAdd", 0.20, desc="대시 회피 판정 +20%")
eff(9162, "Passive", "JustDodgeWindowAdd", 0.40, desc="판정 +40%")
eff(9163, "OnJustDodge", "CritNextShots", 3, desc="대시로 공격을 피하면 다음 3발 치명타")
eff(9171, "Passive", "ControlDurationAdd", 0.20, desc="정지 · 둔화 지속 +20%")
eff(9172, "Passive", "ControlDurationAdd", 0.40, desc="지속 +40%")
eff(9173, "Passive", "DamageVsStoppedAdd", 1.0, desc="정지된 적에게 주는 피해 2배")
eff(9181, "Passive", "UnawakenedDamageAdd", 0.15, desc="비활성화 탄 공격력 +15%")
eff(9182, "Passive", "UnawakenedDamageAdd", 0.30, desc="+30%")
eff(9183, "Passive", "UnawakenedTraitCount", 1, desc="비활성화 탄도 첫 번째 특성이 켜짐")

# 탄 고유 효과 (9201~)
eff(9201, "OnHit", "Knockback", 0.5, target="HitTarget", desc="맞은 적을 살짝 밀쳐냄")
eff(9202, "OnFire", "ExtraShot", 1, 0.3, desc="옆으로 1발 더 (피해 30%)")
eff(9203, "OnHit", "Ricochet", 1, desc="1번 튕김")
eff(9204, "OnHit", "Slow", 0.20, dur=1.5, target="HitTarget", desc="둔화 20%, 1.5초")
eff(9205, "OnHit", "DefenseDown", 0.10, dur=3.0, target="HitTarget", desc="맞은 적 방어력 -10%, 3초")
eff(9206, "OnHit", "Split", 2, 0.4, desc="맞으면 조각 2개로 갈라짐 (피해 40%)")
eff(9207, "OnHit", "Burn", 0.05, dur=3.0, target="HitTarget", desc="3초 동안 초당 피해 5%")
eff(9208, "OnFire", "DamageAdd", 0.20, target="NextBullet", desc="다음 탄 피해 +20%")
eff(9209, "OnHit", "Pierce", 1, desc="1명 관통")
eff(9210, "OnReload", "HeatGain", 0.1, desc="장전할 때 깨어나 있으면 열기 +0.1칸")
eff(9211, "OnKill", "Explosion", 0.5, 2.0, target="Area", desc="처치하면 꽃잎 폭발 (피해 50%, 반경 2)")
eff(9212, "OnHit", "EnemyAttackSpeedDown", 0.20, dur=2.0, target="HitTarget", desc="적 공격 속도 -20%, 2초")
eff(9213, "OnFire", "NextBulletAttackSpeedAdd", 0.30, target="NextBullet", desc="다음 탄 연사 속도 +30%")
eff(9214, "OnHit", "TimeLock", 0.0, dur=0.5, chance=0.25, target="HitTarget", desc="25% 확률로 0.5초 시간에 가둠")
eff(9215, "OnHit", "RicochetByJudgeTime", 6, desc="판정 시각 절댓값만큼 튕김 (최대 6)")
eff(9216, "OnFire", "Stun", 0, 3.0, 0.5, target="Area", desc="쏠 때 종소리, 반경 3 적 0.5초 경직")
eff(9217, "Passive", "Homing", 1, desc="가장 가까운 적을 따라감")
eff(9218, "OnHit", "Explosion", 1.5, 3.0, target="Area", desc="큰 폭발 (피해 150%, 반경 3)")
eff(9219, "OnHit", "DamageAdd", 0.25, target="HitTarget", cond="TargetMarked", desc="추억 표식이 있는 적에게 피해 +25%")
eff(9220, "OnHit", "TimeLock", 0.0, dur=1.0, chance=0.30, target="HitTarget", cond="TargetSlowed", desc="둔화된 적을 30% 확률로 1초 시간에 가둠")
eff(9221, "OnFire", "DodgeChance", 0.10, dur=2.0, desc="쏜 뒤 2초 동안 회피 +10%")
eff(9222, "OnKill", "HeatGain", 0.25, desc="처치하면 열기 +0.25칸")
eff(9223, "Passive", "CritChance", 1.0, cond="Slot12", desc="12시 칸에 있으면 치명타 확정")
eff(9224, "OnHit", "DamageAdd", 0.30, target="HitTarget", cond="TargetBoss", desc="보스에게 피해 +30%")
eff(9225, "OnHit", "TrailSilhouetteShot", 1.0, dur=1.5, desc="맞은 자리에 탄 궤적이 남고, 1.5초 뒤 실루엣이 그 궤적으로 1발 더 (피해 100%)")
eff(9227, "OnFire", "SilhouetteShot", 1.0, chance=0.35, desc="35% 확률로 실루엣이 이 탄을 그대로 한 번 더 쏨")
eff(9228, "Passive", "TraitCountBonus", 1, 3001, desc="회상 특성을 1개 더 셈")
eff(9229, "OnHit", "Pierce", 2, desc="2명 관통")
eff(9230, "OnPierce", "PierceDamageGrowth", 0.25, desc="관통할 때마다 피해 +25%")
eff(9231, "Passive", "JudgeTimeAdd", 1, target="Neighbors", desc="양옆 칸 탄의 판정 시각 +1")
eff(9232, "OnFire", "BulletRefund", 1, chance=0.25, desc="25% 확률로 이 탄을 소모하지 않음")
eff(9233, "OnJustDodge", "InstantReload", 1, desc="저스트 회피하면 즉시 장전")
eff(9234, "OnHit", "DelayedDamage", 1.0, dur=2.0, target="HitTarget", desc="2초 뒤 피해 100% 한 번 더")
eff(9235, "OnKill", "Explosion", 0.8, 3.0, target="Area", cond="KilledByDelayed", desc="지연 피해로 처치하면 기적 소리와 함께 폭발 (피해 80%, 반경 3)")
eff(9236, "Passive", "ControlDurationAdd", 0.5, target="AllBullets", desc="실린더의 모든 탄 정지 · 둔화 지속 +50%")
eff(9237, "OnReload", "TimeSlowField", 0.5, 4.0, 2.0, target="Area", desc="장전할 때 반경 4 시간 감속 50%, 2초")
eff(9238, "OnFire", "GlobalTimeStop", 0, 20, 1.0, desc="쏘면 1초 동안 모든 적의 시간이 멈춤 (재사용 대기 20초)")
eff(9239, "Passive", "DamageVsStoppedAdd", 0.5, desc="정지된 적에게 피해 +50%")
eff(9240, "OnReload", "ClockBurst", 0.5, desc="장전할 때 시계의 12방향으로 1발씩 사방 발사, 각 방향은 그 시각 칸의 탄으로 (피해 50%)")
eff(9241, "Passive", "TraitCountBonus", 1, 3001, desc="회상 특성을 1개 더 셈")
eff(9242, "Passive", "DamageByNegativeJudge", 0.10, desc="판정 시각이 0보다 1 낮을 때마다 피해 +10%")
eff(9243, "OnHit", "Slow", 0.40, dur=2.0, target="HitTarget", desc="둔화 40%, 2초")
eff(9244, "OnHit", "HeatGain", 1, desc="맞히면 열기 1칸")
eff(9245, "Passive", "DamagePerUnawakened", 0.15, cond="Slot12", desc="12시 칸에 있으면 깨어나지 못한 탄 1개당 피해 +15%")
eff(9246, "OnReload", "ReverseCylinder", 1, desc="깨어나 있으면 다음 장전은 12 → 1 역순으로 발사")
eff(9247, "OnChanceDodge", "BulletRefund", 1, chance=0.5, desc="확률 회피 성공 시 50% 확률로 현재 칸 탄 재장전")
eff(9248, "OnKill", "PermanentDamageGrowth", 1, 30, cond="TargetTimeLocked", desc="시간에 갇힌 적을 처치하면 런 동안 이 탄 공격력 +1 (최대 30)")
eff(9249, "OnHit", "TimeLock", 0.0, dur=1.0, chance=0.12, target="HitTarget", desc="12% 확률로 1초 시간에 가둠")
# 능력치 · 피해 유형 탄 (9250~): 깨어나 있으면 전역 능력치로 더해짐
eff(9250, "Passive", "CritChanceAdd", 0.06, desc="치명타 확률 +6%")
eff(9251, "Passive", "DefenseAdd", 6, desc="방어력 +6")
eff(9252, "Passive", "ProcChanceAdd", 0.10, desc="효과 발동 확률 +10%")
eff(9253, "Passive", "CritDamageAdd", 0.30, desc="치명타 피해 +30%")
eff(9254, "Passive", "BossDamageAdd", 0.25, desc="보스에게 주는 피해 +25%")
eff(9255, "Passive", "MoveSpeedAdd", 0.10, desc="이동 속도 +10%")
eff(9256, "Passive", "AreaDamageAdd", 0.35, desc="폭발 · 범위 피해 +35%")
eff(9257, "OnKill", "Explosion", 0.6, 2.5, desc="처치하면 그 자리에 폭죽처럼 폭발 (피해 60%, 반경 2.5)")
eff(9258, "Passive", "StatusDamageAdd", 0.30, desc="정지 · 둔화 · 가둠 · 화상 · 표식이 있는 적에게 피해 +30%")
eff(9259, "Passive", "CritChanceAdd", 0.12, desc="치명타 확률 +12%")
eff(9260, "Passive", "CritDamageAdd", 0.50, desc="치명타 피해 +50%")
eff(9261, "Passive", "DefenseAdd", 15, desc="방어력 +15")
eff(9262, "OnJustDodge", "HeatGain", 0.5, desc="저스트 회피하면 열기 +0.5칸")

# 총 고유 효과 (9301~)
eff(9301, "Passive", "DamageAdd", 0.20, target="OwnBullets", cond="JudgeEven", desc="판정 시각이 짝수인 탄 피해 +20%")
eff(9302, "Passive", "AttackSpeedAdd", 0.15, target="OwnBullets", cond="JudgeAtMost", cv=6, desc="판정 시각 6 이하 탄 연사 +15%")
eff(9303, "OnRicochet", "DamageAddStack", 0.10, target="OwnBullets", desc="이 총의 탄이 튕길 때마다 피해 +10%")
eff(9304, "Passive", "DamageAdd", 0.30, target="OwnBullets", cond="JudgeNegative", desc="판정 시각이 음수인 탄 피해 +30%")
eff(9305, "Passive", "DamageAdd", 0.35, target="OwnBullets", cond="JudgeOver", cv=12, desc="판정 시각 12 초과 탄 피해 +35%")
eff(9306, "OnHit", "DamageByDistance", 0.02, 0.4, target="OwnBullets", desc="이 총의 탄은 멀리 있는 적일수록 피해 증가 (1m당 +2%, 최대 +40%)")
eff(9307, "OnFire", "SilhouetteShot", 0.5, chance=0.10, target="OwnBullets", desc="이 총의 탄을 쏠 때 10% 확률로 실루엣이 1발 더 (피해 50%)")
eff(9308, "OnSkill", "HeatGain", 0.5, desc="[전역] 스킬을 쓰면 열기 +0.5칸")
eff(9310, "OnReloadEnd", "Stun", 0, 6.0, 0.5, target="Area", desc="[전역] 장전이 끝날 때 반경 6 적 0.5초 정지")
eff(9311, "OnHit", "Knockback", 1.2, target="OwnBullets", desc="이 총의 탄에 맞은 적을 크게 밀쳐냄")
eff(9312, "Passive", "DodgeCooldown", -0.20, desc="[전역] 대시 충전 시간 -20%")
eff(9313, "Passive", "Ricochet", 1, target="OwnBullets", desc="이 총의 탄이 1번 튕김")
eff(9314, "OnReload", "ReverseCylinder", 0, desc="[전역] 장전할 때마다 사격 순서를 뒤집음 (1→12 ↔ 12→1)")
eff(9315, "OnHit", "Pierce", 1, target="OwnBullets", desc="이 총의 탄이 적 1명 관통")
eff(9316, "OnHit", "TimeLock", 0.3, dur=1.0, chance=0.10, target="OwnBullets", desc="이 총의 탄이 10% 확률로 적을 1초 시간에 가둠")

# 코스튬 효과 (9401~)
eff(9401, "Passive", "AttackSpeedAdd", 0.15, desc="연사 속도 +15%")
eff(9402, "Passive", "DamageTakenAdd", 0.15, desc="받는 피해 +15%")
eff(9403, "Passive", "UnawakenedDamageAdd", 0.50, desc="깨어나지 못한 탄 공격력 +50%")
eff(9404, "Passive", "AwakenedDamageAdd", -0.10, desc="깨어난 탄 공격력 -10%")
eff(9405, "RunStart", "GrantRandomOp", 1, desc="런 시작 때 무작위 연산 1개")
eff(9406, "Passive", "OpDropRateAdd", 0.30, desc="연산 드랍 +30%")
eff(9407, "Passive", "Ricochet", 1, target="AllBullets", desc="모든 탄 1번 튕김")
eff(9408, "Passive", "MaxHpAdd", -0.20, desc="최대 체력 -20%")
eff(9409, "Passive", "DamageAdd", 0.25, target="AllBullets", cond="JudgeAtMost", cv=0, desc="판정 시각 0 이하 탄 공격력 +25%")
eff(9410, "RunStart", "GrantBullet", 1018, desc="런 시작 때 '개기일식' 지급")
eff(9411, "Passive", "TraitThresholdReduce", 1, desc="특성 6단계가 5개로 켜짐")
eff(9412, "Passive", "MaxHpAdd", -0.15, desc="최대 체력 -15%")
eff(9413, "Passive", "MemoryShardGainAdd", 0.50, desc="시간의 조각 획득 +50%")
eff(9414, "Passive", "DamageTakenAdd", 0.20, desc="받는 피해 +20%")

# ---------------------------------------------------------------- 특성
TRAIT = [
    # Id, Name, Desc, IconKey
    (3001, "회상", "고양이 실루엣이 나타나 함께 쏜다", "trait_recall"),
    (3002, "추억", "같은 적을 계속 맞히면 추억 표식이 새겨진다", "trait_memory"),
    (3003, "망각", "적을 시간 속에 가둬 멈추고 더 아프게 한다", "trait_oblivion"),
    (3004, "되감기", "피해를 맞기 직전의 순간으로 되감는다 (회피)", "trait_rewind"),
    (3005, "종말", "끝이 가까운 적을 처형하고 보스에게 강한 피해", "trait_end"),
    (3006, "지연", "일정 시간 후 추가 피해가 들어간다", "trait_delay"),
    (3007, "예지", "대시 회피 판정이 늘어난다", "trait_foresight"),
    (3008, "영원", "멈춘 시간이 더 오래 이어진다", "trait_eternity"),
    (3009, "미련", "비활성화 탄에 보너스 효과가 붙는다", "trait_lingering"),
]
TRAIT_TIER = []  # Id, TraitId, Tier, EffectId
_tid = 31001
for trait_id, base in zip(range(3001, 3010), range(9101, 9190, 10)):
    rows = [(2, base), (4, base + 1)]
    rows += [(6, base + 2)] if trait_id == 3006 else [(6, base + 1), (6, base + 2)]
    for tier, e in rows:
        TRAIT_TIER.append((_tid, trait_id, tier, e)); _tid += 1

# ---------------------------------------------------------------- 탄 (36)
# Id, Name, Grade, Damage, CondType, CondValue, Trait1, Trait2, OpSlots, Effect1, Effect2, IconKey, Desc
B = []
def blt(i, name, grade, dmg, ct, cv, t1, t2, slots, e1, e2=0, desc=""):
    B.append((i, name, grade, dmg, ct, cv, t1, t2, slots, e1, e2, f"bullet_{i}", desc))
R, M, O, W, D_, L, F, T, U = 3001, 3002, 3003, 3004, 3005, 3006, 3007, 3008, 3009  # 회상 추억 망각 되감기 종말 지연 예지 영원 미련
# 기본 탄: 판을 시작할 때 12칸 모두 이 탄. 효과 · 특성 없음, 드랍 · 상점에 나오지 않음
blt(1000, "탄환", "Common", 8, "None", 0, 0, 0, 0, 0, desc="아무 효과 없는 기본 탄")
# 일반 12
blt(1001, "티 타임", "Common", 10, "Odd", 0, R, D_, 2, 9201, desc="오후의 찻잔에서 피어오른 한 발")
blt(1002, "쌍둥이 초침", "Common", 10, "Even", 0, R, L, 1, 9202, desc="초침 두 개가 나란히 달린 탄")
blt(1003, "모래바람", "Common", 10, "MultipleOf", 3, O, T, 2, 9203, desc="쏠 때마다 모래가 흩날린다")
blt(1004, "짙어진 그림자", "Common", 11, "MultipleOf", 4, L, T, 1, 9204, desc="저물수록 짙어지는 그림자")
blt(1005, "추억미화", "Common", 10, "Prime", 0, D_, M, 2, 9205, desc="아름답게 덧칠된 기억")
blt(1006, "부서진 맹세", "Common", 12, "LessThan", 8, R, W, 0, 9206, desc="지키지 못한 맹세의 조각")
blt(1007, "꺼져 가는 등불", "Common", 10, "GreaterThan", 6, L, D_, 2, 9207, desc="꺼져 가는 등불의 마지막 빛")
blt(1008, "DEAR[ ]", "Common", 10, "Exact", 3, M, U, 2, 9208, desc="받는 이를 비워 둔 편지")
blt(1009, "노스탤지어", "Common", 11, "MultipleOf", 6, W, F, 1, 9209, desc="돌아갈 수 없는 곳을 향한 그리움")
blt(1010, "녹슨 태엽", "Common", 10, "Odd", 0, U, O, 2, 9210, desc="태엽 열쇠가 꽂힌 녹슨 탄")
blt(1011, "드라이드 플라워", "Common", 10, "Even", 0, F, M, 1, 9211, desc="말려 둔 꽃, 멈춘 계절")
blt(1012, "모래시계", "Common", 11, "Negative", 0, T, U, 2, 9212, desc="작은 모래시계가 든 투명한 탄")
# 희귀 10
blt(1013, "시계태엽 오르골", "Rare", 12, "Even", 0, F, R, 2, 9213, desc="쏘면 오르골 소리가 난다")
blt(1014, "얼어붙은 시간", "Rare", 12, "GreaterThan", 12, T, O, 1, 9214, desc="서리 낀 시계판이 박힌 탄")
blt(1015, "평행세계", "Rare", 12, "Negative", 0, W, L, 1, 9215, desc="거울 너머의 다른 어제")
blt(1016, "종탑의 메아리", "Rare", 12, "MultipleOf", 6, D_, F, 2, 9216, desc="작은 종이 매달린 탄")
blt(1017, "새벽여명", "Rare", 13, "Exact", 1, M, R, 0, 9217, desc="새벽빛을 따라 날아간다")
blt(1018, "개기일식", "Rare", 12, "Zero", 0, L, D_, 2, 9218, desc="해가 사라지는 순간에만 피어나는 불꽃")
blt(1019, "빛바랜 사진", "Rare", 12, "Prime", 0, M, U, 1, 9219, desc="흐릿한 사진 한 장이 감긴 탄")
blt(1020, "고독", "Rare", 12, "MultipleOf", 4, O, T, 2, 9220, desc="홀로 멈춘 초침")
blt(1021, "아지랑이", "Rare", 12, "Odd", 0, W, F, 1, 9221, desc="쏘면 아지랑이처럼 흩어진다")
blt(1022, "황혼", "Rare", 13, "LessThan", 4, D_, U, 2, 9222, desc="저무는 해의 마지막 빛")
# 영웅 8
blt(1023, "천문박명", "Epic", 15, "Exact", 12, D_, F, 1, 9223, 9224, desc="밤과 낮 사이, 별이 사라지는 시간")
blt(1024, "여우비", "Epic", 14, "Prime", 0, T, L, 1, 9225, desc="맑은 하늘의 비, 지나간 자리에 궤적이 남는다")
blt(1025, "타임 루프 엔딩", "Epic", 14, "Odd", 0, R, T, 2, 9227, 9228, desc="같은 순간을 몇 번이고 되풀이한다")
blt(1026, "세월", "Epic", 15, "MultipleOf", 4, D_, M, 0, 9229, 9230, desc="꿰뚫을수록 깊어지는 세월")
blt(1027, "오딘의 눈동자", "Epic", 14, "Even", 0, M, U, 2, 9231, desc="옆 칸의 시간을 꿰뚫어 본다")
blt(1028, "건망증", "Epic", 14, "Negative", 0, W, O, 1, 9232, 9233, desc="쏜 것을 잊고 다시 장전한다")
blt(1029, "야행열차", "Epic", 16, "GreaterThan", 18, L, R, 0, 9234, 9235, desc="밤을 달리는 열차의 늦은 기적 소리")
blt(1030, "타임 패러독스", "Epic", 14, "Zero", 0, T, F, 1, 9236, 9237, desc="어긋난 시간이 서로를 붙잡는다")
# 전설 6
blt(1031, "열한 시 오십구 분", "Legendary", 18, "Exact", 11, O, T, 0, 9238, 9239, desc="끝 직전에 멈춘 시간")
blt(1032, "그해 여름", "Legendary", 17, "Exact", 1, R, M, 0, 9240, 9241, desc="장전할 때 시계의 모든 방향으로 여름이 쏟아진다")
blt(1033, "여름의 지속", "Legendary", 16, "Negative", 0, L, T, 1, 9242, 9243, desc="녹아 흘러내리는 시계의 여름")
blt(1034, "마지막 페이지", "Legendary", 18, "Exact", 12, D_, U, 2, 9244, 9245, desc="이야기의 마지막 장")
blt(1035, "변혁의 총탄", "Legendary", 16, "Prime", 0, W, L, 1, 9246, 9247, desc="시간을 거꾸로 돌리는 한 발")
blt(1036, "레테의 물결", "Legendary", 16, "Odd", 0, O, F, 2, 9248, 9249, desc="건너면 모든 것을 잊는 강")
# 능력치 · 피해 유형 탄 10 (일반 3 · 희귀 3 · 영웅 2 · 전설 2)
blt(1037, "회중시계의 눈금", "Common", 10, "MultipleOf", 2, F, D_, 1, 9250, desc="눈금 하나까지 놓치지 않는 눈")
blt(1038, "낡은 회색 코트", "Common", 10, "Odd", 0, T, W, 2, 9251, desc="수많은 겨울을 버틴 코트 자락")
blt(1039, "네잎클로버 책갈피", "Common", 10, "Prime", 0, M, F, 1, 9252, desc="책 사이에 끼워 둔 행운")
blt(1040, "정오의 결투", "Rare", 12, "Exact", 6, D_, R, 1, 9253, desc="그림자가 사라지는 순간 먼저 뽑는 자")
blt(1041, "철길 위의 동전", "Rare", 12, "Even", 0, L, D_, 2, 9254, desc="기차가 지나간 뒤 납작해진 동전")
blt(1042, "바람개비", "Rare", 12, "GreaterThan", 3, W, R, 1, 9255, desc="달리면 더 빨리 도는 종이 바람개비")
blt(1043, "불꽃놀이 마지막 발", "Epic", 14, "MultipleOf", 3, L, M, 1, 9256, 9257, desc="축제가 끝나기 전 가장 큰 불꽃")
blt(1044, "멈춘 맥박", "Epic", 14, "LessThan", 6, O, D_, 2, 9258, desc="멈춰 선 것들에게 더 깊이 박힌다")
blt(1045, "운명의 동전 던지기", "Legendary", 17, "Exact", 7, F, D_, 1, 9259, 9260, desc="앞면이면 치명, 뒷면이어도 치명")
blt(1046, "녹슬지 않는 갑주", "Legendary", 16, "Negative", 0, T, W, 2, 9261, 9262, desc="시간이 비켜 간 낡은 갑옷 조각")

# ---------------------------------------------------------------- 연산
OP = [
    # Id, Name, OpType, Operand, DropWeight
    (5001, "+1", "Add", 1, 100), (5002, "+2", "Add", 2, 60), (5003, "+3", "Add", 3, 30),
    (5004, "-1", "Sub", 1, 100), (5005, "-2", "Sub", 2, 60), (5006, "-3", "Sub", 3, 30),
    (5007, "×2", "Mul", 2, 60), (5008, "×3", "Mul", 3, 30),
    (5009, "÷2", "Div", 2, 60), (5010, "÷3", "Div", 3, 30),
]

# ---------------------------------------------------------------- 스킬
SKILL = [
    # Id, Name, Action, Cooldown, Value, Value2, Duration, Desc
    (6001, "백스텝 샷", "BackstepShot", 6.0, 2, 4.0, 0.0, "조준 반대쪽으로 4m 도약하며 2발"),
    (6002, "앞뒤 네 발", "TurnShot", 7.0, 4, 0.0, 0.0, "앞을 2발 쏘고 홱 돌아 뒤로 2발"),
    (6003, "넘어가며 쏘기", "VaultShot", 8.0, 3, 0.0, 0.0, "적을 뛰어넘으며 아래로 3발, 쏠 때마다 반동으로 더 뜸"),
    (6004, "반사탄 패링", "Parry", 6.0, 1.5, 0.3, 0.0, "0.3초 안에 적 투사체를 쏘면 피해 150%로 되돌려 보냄"),
    (6005, "모래 장판", "SlowField", 12.0, 0.4, 4.0, 4.0, "반경 4 모래 장판, 적 둔화 40%, 4초"),
    (6006, "조준 사격", "ChargeShot", 10.0, 4.0, 99, 0.0, "모아서 모든 적을 관통하는 1발 (피해 400%)"),
    (6007, "회상의 실루엣", "SummonSilhouette", 15.0, 0.6, 0.0, 4.0, "고양이 실루엣을 불러 4초 동안 함께 사격 (피해 60%)"),
    (6008, "시간 정지", "TimeStop", 25.0, 0, 8.0, 2.0, "반경 8 적의 시간을 2초 동안 멈춤"),
]

# ---------------------------------------------------------------- 총 (8)
GUN = [
    # Id, Name, Grade, DamageMul, FireInterval, ReloadTime, Range, SkillId, RightEffect1, RightEffect2, LeftEffect1, LeftEffect2, IconKey, Desc, CritChance, CritDamage
    # 오른손 = 홀수 칸 · 공격, 왼손 = 짝수 칸 · 보조 (손별 효과 수치는 임시)
    (2001, "보안관의 리볼버", "Common", 1.00, 0.30, 1.2, 12.0, 6001, 0, 0, 0, 0, "gun_2001", "영웅이 처음 쥔 기본 총", 0.00, 0.00),
    (2002, "녹슨 콜트", "Common", 1.30, 0.42, 1.4, 12.0, 6002, 9301, 0, 9311, 0, "gun_2002", "무겁고 느리지만 묵직한 한 발", 0.05, 0.30),
    (2003, "쌍둥이 데린저", "Rare", 0.75, 0.18, 1.0, 9.0, 6003, 9302, 0, 9312, 0, "gun_2003", "작고 빠른 쌍둥이 권총", 0.10, 0.10),
    (2004, "은빛 메아리", "Rare", 1.00, 0.30, 1.2, 12.0, 6004, 9303, 0, 9313, 0, "gun_2004", "튕길수록 커지는 은빛 메아리", 0.06, 0.15),
    (2005, "백투더 퓨처", "Rare", 1.00, 0.32, 1.3, 11.0, 6005, 9304, 0, 9314, 0, "gun_2005", "손잡이 속 모래가 거꾸로 흐른다", 0.06, 0.15),
    (2006, "지평선", "Epic", 1.80, 0.65, 1.8, 18.0, 6006, 9305, 9306, 9315, 0, "gun_2006", "지평선 끝까지 닿는 긴 총신", 0.08, 0.45),
    (2007, "동경", "Epic", 1.10, 0.28, 1.2, 12.0, 6007, 9307, 0, 9308, 0, "gun_2007", "노인이 동경한 영웅의 그림자가 따라 쏜다", 0.10, 0.25),
    (2008, "마지막 종소리", "Legendary", 1.20, 0.30, 1.0, 13.0, 6008, 9316, 0, 9310, 0, "gun_2008", "당기면 마지막 종이 울리고 시간이 멈춘다", 0.12, 0.35),
]

# ---------------------------------------------------------------- 코스튬 (8)
COSTUME = [
    # Id, Name, UnlockType, UnlockValue, Cost, Effect1, Effect2, Effect3, ModelKey, Desc
    (4001, "보안관 코트", "Default", 0, 0, 0, 0, 0, "costume_sheriff", "영웅의 기본 복장"),
    (4002, "무법자 판초", "ClearZone", 1, 30, 9401, 9402, 0, "costume_poncho", "빠르게 쏘고 더 아프게 맞는다"),
    (4003, "장의사 정장", "ExecuteCount", 50, 40, 9403, 9404, 0, "costume_undertaker", "깨어나지 못한 탄을 위한 옷"),
    (4004, "시계공의 앞치마", "ClearZone", 3, 60, 9405, 9406, 0, "costume_apron", "연산을 들고 시작한다"),
    (4005, "거울 망토", "JustDodgeCount", 100, 50, 9407, 9408, 0, "costume_mirror", "모든 탄이 한 번 튕긴다"),
    (4006, "모래 순례자 로브", "KillBoss", 2, 50, 9409, 9410, 0, "costume_pilgrim", "0시 이하의 시간을 걷는 자"),
    (4007, "기억의 스카프", "TraitTier6", 1, 80, 9411, 9412, 0, "costume_scarf", "특성 6단계가 하나 빨리 켜진다"),
    (4008, "노인의 잠옷", "ClearZone", 5, 120, 9413, 9414, 0, "costume_pajama", "현실에서 온 옷. 시간의 조각을 더 모은다"),
]

# ---------------------------------------------------------------- 시트 정의
SHEETS = [
    ("Grade", ["Grade:enum", "Name:string", "Color:string", "BulletDropWeight:int", "GunDropWeight:int"], GRADE),
    ("Bullet", ["Id:int", "Name:string", "Grade:enum", "Damage:int", "CondType:enum", "CondValue:int", "Trait1:int", "Trait2:int",
                "OpSlots:int", "Effect1:int", "Effect2:int", "IconKey:string", "Desc:string"], B),
    ("Trait", ["Id:int", "Name:string", "Desc:string", "IconKey:string"], TRAIT),
    ("TraitTier", ["Id:int", "TraitId:int", "Tier:int", "EffectId:int"], TRAIT_TIER),
    ("Effect", ["Id:int", "Trigger:enum", "Cond:enum", "CondValue:int", "Chance:float", "Target:enum", "Type:enum",
                "Value:float", "Value2:float", "Duration:float", "Desc:string"], E),
    ("Op", ["Id:int", "Name:string", "OpType:enum", "Operand:int", "DropWeight:int"], OP),
    ("Gun", ["Id:int", "Name:string", "Grade:enum", "DamageMul:float", "FireInterval:float", "ReloadTime:float", "Range:float",
             "SkillId:int", "RightEffect1:int", "RightEffect2:int", "LeftEffect1:int", "LeftEffect2:int", "IconKey:string", "Desc:string",
             "CritChance:float", "CritDamage:float"], GUN),
    ("Skill", ["Id:int", "Name:string", "Action:enum", "Cooldown:float", "Value:float", "Value2:float", "Duration:float", "Desc:string"], SKILL),
    ("Costume", ["Id:int", "Name:string", "UnlockType:enum", "UnlockValue:int", "Cost:int", "Effect1:int", "Effect2:int", "Effect3:int",
                 "ModelKey:string", "Desc:string"], COSTUME),
]

def validate():
    eids = {e[0] for e in E}
    enum = {k: {v[0] for v in vals} for k, vals in ENUMS.items()}
    errs = []
    assert len(B) == 47 and len(GUN) == 8 and len(COSTUME) == 8 and len(TRAIT) == 9
    for b in B:
        if b[2] not in enum["Grade"] or b[4] not in enum["PlaceCond"]: errs.append(f"enum {b}")
        for e in (b[9], b[10]):
            if e and e not in eids: errs.append(f"bullet {b[0]} effect {e}")
    for g in GUN:
        for e in g[8:12]:
            if e and e not in eids: errs.append(f"gun {g[0]} effect {e}")
    for c in COSTUME:
        for e in c[5:8]:
            if e and e not in eids: errs.append(f"costume {c[0]} effect {e}")
    for e in E:
        if e[1] not in enum["Trigger"] or e[2] not in enum["EffectCond"] or e[5] not in enum["Target"] or e[6] not in enum["EffectType"]:
            errs.append(f"effect enum {e}")
    for t in TRAIT_TIER:
        if t[3] not in eids: errs.append(f"tier {t}")
    from collections import Counter
    grades = Counter(b[2] for b in B)
    traits = Counter(t for b in B for t in (b[6], b[7]))
    print("bullet grades", dict(grades))
    print("trait use", {k: traits[k] for k in sorted(traits)})
    if len({e[0] for e in E}) != len(E): errs.append("duplicate effect id")
    if errs:
        print("\n".join(errs)); sys.exit(1)

def write():
    os.makedirs(os.path.dirname(XLSX), exist_ok=True)
    os.makedirs(CSV_DIR, exist_ok=True)
    wb = Workbook()
    wb.remove(wb.active)
    head_fill = PatternFill("solid", fgColor="2C2440")
    for name, cols, rows in SHEETS:
        ws = wb.create_sheet(name)
        ws.append(cols)
        for c in ws[1]:
            c.font = Font(bold=True, color="F3EDE2"); c.fill = head_fill
        for r in rows:
            ws.append(list(r))
        for i, col in enumerate(cols, 1):
            width = max(len(str(col)), *(len(str(r[i - 1])) for r in rows)) if rows else len(col)
            ws.column_dimensions[ws.cell(1, i).column_letter].width = min(max(10, width * 1.4), 70)
        ws.freeze_panes = "B2"
        with open(os.path.join(CSV_DIR, f"{name}.csv"), "w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow(cols)
            w.writerows(rows)
    ws = wb.create_sheet("Enum")
    ws.append(["Enum:string", "Key:string", "Desc:string"])
    for c in ws[1]:
        c.font = Font(bold=True, color="F3EDE2"); c.fill = head_fill
    enum_rows = [(k, key, d) for k, vals in ENUMS.items() for key, d in vals]
    for r in enum_rows:
        ws.append(list(r))
    ws.column_dimensions["A"].width = 16; ws.column_dimensions["B"].width = 26; ws.column_dimensions["C"].width = 60
    with open(os.path.join(CSV_DIR, "Enum.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f); w.writerow(["Enum:string", "Key:string", "Desc:string"]); w.writerows(enum_rows)
    wb.save(XLSX)
    print("saved", XLSX, "and", CSV_DIR)

if __name__ == "__main__":
    validate()
    write()
