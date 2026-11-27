# 보스 등장 연출 기획서용 조사 (2026-11-27, 코드 기준)

보스 등장 연출 기획서를 만들 때 쓸 자료. 출처는 `Game/Assets/Scripts/Game/BossIntro.cs`(BI)와 `BossIntroCards.cs`(BIC).

## 1. 보스 목록 (BI:54-266, 이름과 칭호는 2026-11-21 사용자 확정)
| 장 | 구분 | id | 칭호 / 이름 | 영어 | 카드 |
|---|---|---|---|---|---|
|1|중간|ch1_mid|황야의 무법자 / 와인더 잭|OUTLAW OF THE WASTELAND / WINDER JACK|Wanted|
|1|보스|ch1_boss|불타는 시계탑 / 달리오스|THE BURNING CLOCKTOWER / DALIOS|Melt|
|2|중간|ch2_mid|승강장의 지배자 / 컨덕터 반|LORD OF THE PLATFORM / CONDUCTOR VAN|Board|
|2|보스|ch2_boss|탈선한 종착역 / 로코모스|THE DERAILED TERMINUS / LOCOMOS|Rail|
|3|중간|ch3_mid|반사의 연출가 / 일루시오|DIRECTOR OF REFLECTIONS / ILLUSIO|Curtain|
|3|보스|ch3_boss|호수의 기억 / 모비 글래스|MEMORY OF THE LAKE / MOBY GLASS|Shatter|
|4|중간|ch4_mid|시계탑의 단두대 / 기로틴|GUILLOTINE OF THE TOWER / GUILLOTINE|NoirSlash|
|4|보스|ch4_boss|시간의 수문장 / 크로노 & 템포|GUARDIANS OF TIME / CHRONO & TEMPO|TwinSplit|
|5|중간|ch5_mid|(칭호 없음) / 미지의 존재|UNKNOWN|Glitch|
|5|보스|ch5_boss|영원의 지배자 / 시간의 주인|RULER OF ETERNITY / LORD OF TIME|Dial|

- 크로노 = 시침, 템포 = 분침. 이 두 이름은 AI가 정했고 **사용자 확인 전**이다.
- 상체만 크게 보여 주는 거대 보스(bust, 60%): ch1_boss / ch2_boss / ch4_boss / ch5_boss.
- 등장 연출 화면의 글자는 영어다. 체력바에는 한국어 이름이 나온다.

## 2. 공통 흐름
1. **발동**: `RunManager.SpawnWave`에서 보스를 생성한 뒤 `BossIntro.Play` 실행(RunManager.cs:243, 250).
2. **준비**:
   - 적 행동 / 미니맵 정지. 플레이어 입력 잠금.
   - 보스가 고양이 쪽으로 회전.
   - 등장 위치: 위 / 뒤 / 땅속(높이 × 1.3 아래)에서 진입.
   - HUD 숨김. 음악은 0.4초 동안 0.3으로 줄이고 `intro_riser` 재생.
3. **컷**:
   - 보스마다 (샷, 시간, 전환) 목록을 실제 시간 기준으로 재생.
   - 첫 샷 동안 레터박스 118px가 0.22초에 걸쳐 닫힘.
   - 전환 종류: Hard / Flash(흰색, 느와르 보스는 강조색) / Whip(55도 회전과 속도선 0.13초, `intro_whoosh`) / Wipe(-16도 사선 띠).
   - 로우 앵글을 빼면 카메라가 최소 10도 내려다봄. 이동 샷에는 SpeedRamp(1.55 → 0.45 → 1.55)를 적용.
   - 시야를 가리는 소품은 숨기고, 마지막 샷에서는 고양이도 숨김. 마지막 샷은 항상 정면.
4. **정지 카드**:
   - timeScale을 0으로. 음악을 0.05초에 0으로 줄여 한 박자 침묵을 만듦.
   - 효과음: `intro_impact`, `intro_stinger_<장>`. 흰 번쩍임과 함께 레터박스가 열림.
   - 표시 시간: 보스 1.8초 / 중간 보스 1.6초. 퇴장은 0.22초 동안 페이드와 6% 확대.
   - 그림 = `Resources/UI/BossIntro/<id>.png`. 그림이 없으면 글자만 나옴.
5. **이름 / 칭호 글자**:
   - "CHAPTER n · BOSS/MID BOSS" 줄과 영어 칭호가 0.22초 동안 -260px 위치에서 밀려 들어옴.
   - +0.08초: 영어 이름(168pt, 기울임, BlackHanSans)이 1.9배에서 1배로 0.14초 만에 찍힘. `e_slam_1` 재생.
   - +0.22초: 한국어 줄 등장.
   - 카드가 떠 있는 동안 전체가 14px 흘러감.
6. **종료**:
   - timeScale 1로 복귀. 보스 곡 처음부터 재생, 음량은 0.9초 동안 1로 복귀(건너뛰면 0.4초).
   - 카메라는 0.4초 동안 원래 추적 위치로.
   - `Meta.MarkIntroSeen(id)` 기록.
   - 입력 잠금은 1프레임 뒤 해제(건너뛰기 Space가 대시로 먹히는 것 방지). 보스 패턴은 1.0초 동안 보류.
7. **총 시간**:
   - 샷 합계: 보스 전부 4.2초. 중간 보스는 1장 3.4초 / 2~4장 2.4초 / 5장 2.7초.
   - 보스 전체 약 6.6초, 중간 보스 약 4.6~5.6초.
8. **건너뛰기**:
   - 처음 만날 때는 건너뛸 수 없다. 두 번째부터 Space로 샷과 카드를 모두 건너뜀.
   - 본 기록은 저장 슬롯마다 따로 남음.
   - 건너뛸 수 있을 때만 오른쪽 위에 SPACE 키캡과 "건너뛰기 ▶▶"를 맥동 표시.
   - 따로 다시 보는 기능은 없다.

## 3. 보스별 연출 (샷과 초)
- **와인더 잭**
  - 샷: Feet 0.8 → Profile 0.7 → ShotDown 1.9.
  - 착지 링 예고 후 9m 높이에서 낙하 착지. 재장전.
  - 샷 진행 0.36 지점에서 카메라를 저격 → 카메라가 회전하며 날아가 바닥에 떨어진 채로 올려다봄.
  - 카드 Wanted: 현상수배 포스터가 떨어지고, 0.56초에 빨간 "DEAD OR ALIVE" 도장.
- **달리오스**
  - 샷: LowRise 1.4 → BehindBoss 1.2(Flash) → Pullback 1.6.
  - 녹은 바닥에서 솟아오름. 금빛 베기, 슬로 0.4로 내려찍기 폭발.
  - 카드 Melt: 녹은 금 방울 18개. 이름이 늘어졌다가 굳음.
- **컨덕터 반**
  - 샷: HighOrbit 1.0 → QuickDraw 1.4(Wipe).
  - 9m 뒤에서 증기와 함께 돌진. 호루라기, 빨강 / 초록 신호 번쩍임, 슬로 0.45.
  - 카드 Board: 반전식 출발 안내판. 글자가 0.07초 간격으로 돌다 멈춤. `intro_flip`, "PLATFORM n".
- **로코모스**
  - 샷: Wide 1.0 → Chase 1.6(Whip) → Crash 1.6(Flash).
  - 14m 거리에서 돌진, 3.2m 들이받기. 슬로로 석탄 폭발.
  - 카드 Rail: 기차 띠가 들어와 불꽃을 튀기며 급정거.
- **일루시오**
  - 샷: LowRise 1.0 → HeroOrbit 0.7 → Portrait 0.7.
  - 0.25 지점에서 번쩍임과 연기 속에 인사하며 등장.
  - 거울 분신 둘이 동작을 따라 하다가 0.75초 뒤 유리처럼 깨짐. 카메라 쪽으로 카드를 던짐.
  - 카드 Curtain: 커튼 테두리, 스포트라이트, 전구가 깜빡이는 간판.
- **모비 글래스**
  - 샷: Top 1.3 → Rise 1.5(Whip) → HeroLow 1.4(Flash).
  - 위에서 본 물결 → 수면을 뚫고 솟구침.
  - 카드 Shatter: 방사형 균열 18개, 거울에 비친 잔상, 광택이 지나감. 그림은 가로로 긴 고래.
- **기로틴**
  - 샷: JumpCut 1.0 → Crash 1.4(Flash). 느와르 연출.
  - 각도 4개를 하드컷으로 이어 붙임. 빨간 글리치 순간이동, 빨간 X 베기.
  - 카드 NoirSlash: 검은 화면, -18도 빨간 사선, 흰 반쪽이 밀려 들어옴, 핏자국.
- **크로노 & 템포**
  - 샷: ClockSweep 1.0 → Duel 0.55 → Duel 0.5 → Profile 0.8(Flash) → Push 1.35(Whip).
  - ClockSweep: 시곗바늘처럼 돌며 내려와 착지.
  - Duel: 대검 클로즈업, 이어서 레이피어 찌르기 클로즈업.
  - Profile: 서로 엇갈려 베며 지나간 뒤 어깨를 나란히(±2.3m), 슬로 0.3.
  - Push: 정면 결의 포즈.
  - 카드 TwinSplit: 흰 반쪽과 검은 반쪽이 맞부딪침. 가운데 큰 "&"를 두고 이름이 좌우로 갈림.
- **미지의 존재**
  - 샷: OverShoulder 0.75 → HeroOrbit 0.95(Flash) → QuickDraw 1.0. 글리치 연출.
  - 고양이 모델에 런타임으로 자세를 줌: 모자챙 → 리볼버 3바퀴 → 카메라를 향해 쌍권총 속사 → 뒤로 유리 깨짐.
  - 카드 Glitch: RGB 분리, 주사선, 뒤섞인 글자가 이름으로 정리됨. "CHAPTER 5 · ???".
- **시간의 주인**
  - 샷: WormEye 1.2 → Stutter 1.5 → Vertigo 1.5(Whip).
  - 소환진 위로 9m 높이에서 천천히 하강. 허공 돔.
  - Stutter: 째깍거리며 7번 끊어 도는 궤도.
  - Vertigo: 돌리 줌. 슬로 0.4로 대낫 베기.
  - 카드 Dial: 시계판 바늘이 돌다가 11:59에 멈춤. 0.58초에 마지막 1분을 째깍 하고 넘긴 뒤 종소리.

## 4. 설정 연동
- 흔들림 × `GameSettings.ShakeScale`.
- 번쩍임과 글리치 × `FlashScale`. "번쩍임 줄이기"를 켜면 0.3.

## 5. 소리
- 연출 효과음: `Resources/Audio/Sfx/intro_riser` / `intro_whoosh_1·2` / `intro_impact` / `intro_stinger_1~5` / `intro_flip` / `intro_glitch`.
  - Stable Audio로 생성했고 아직 귀로 들어 보지 않았다.
  - 파일이 없으면 예전 소리로 대체.
- 음악 흐름: 0.3으로 줄임 → 카드에서 침묵 → 보스 곡을 처음부터 재생.

## 6. 그림
- `docs/images/boss_intro`는 비어 있다. GIF 15개와 `docs/boss_intro_mp4`의 MP4 12개는 커밋 9dfecfe4에서 용량 때문에 지웠다.
  - 복구: `git show 9dfecfe4^:docs/images/boss_intro/<파일>`.
- 남아 있는 그림:
  - 카드 그림: `Game/Assets/Resources/UI/BossIntro/ch1_mid.png` ~ `ch5_boss.png`. ch3_boss는 가로로 긴 고래. ch5_boss는 망토 쓴 사신 컨셉이라 인게임 갑옷 모델과 조금 다름.
  - `docs/images/check/boss_intro_freeze.jpg`
  - `illusio_model*`
