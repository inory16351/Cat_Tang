# 핸드오프 — 1장 적 · 보스 · Tripo 모델 · 리깅 · 적 탄 이펙트

마지막 갱신: 2026-11-16 (밤, 2차). 이전 핸드오프(연출 · HUD · 판 구조)는 git 기록 `459df5c`의 HANDOFF.md 참고.

## 작업 원칙 (사용자 지시)

- **브랜치는 `main` 하나.** 작업 브랜치 만들지 않음. 이 PC 계정(Btmdo)은 푸시 권한이 없어 **푸시는 사용자가 직접** 함.
- **오브젝트는 웬만하면 Unity MCP로 에디터에서 직접 만들어 프리팹 · 씬에 저장.** 런타임에 코드로 도형을 조립하는 방식은 줄일 것. (지금 방 · 벽 · 소품 · 문 · 상인은 아직 `RoomBuilder`가 런타임에 도형으로 만듦 → 바꿔야 함)
- **Tripo 모델은 플랫 컬러(명암 없는 고유색) 그림으로 뽑고 툰 셰이더(`CatTang/Toon`)로 음영.** GPT · Codex 그림은 Tripo에 넣기 전에 디노이즈(`Art/tools/denoise.py`).
- 질문 · 답변 · UI 문구는 전부 한국어.

## 이번에 한 것 (전부 main에 커밋됨, 미푸시)

### 게임 로직
- **1장 일반 적 3종** (`Enemy.cs` · `EnemyFactory.cs`): 태엽 무법자(조준선 0.5초 → 1발) · 시계추 깡패(돌진 예고 0.6초 → 돌진) · 모래시계 폭탄병(0.8초 점멸 → 반경 2m 자폭). 수치는 1장 기획서.
- **강화형**(중간 보스 뒤 스테이지 3부터): 무법자+ 조준당하면 구르며 사격 · 깡패+ 회전 휘두르기 · 폭탄병+ 모래 장판 · 체력 1.5배.
- **패턴 고르기**(`BossBrain.cs`의 `PatternPicker`): 거리에 맞는 패턴 + 거리 무관 패턴 동일 확률, 직전 패턴 제외, 패턴별 조건(하수인 호출은 필드 2명 이하).
- **중간 보스 · 보스**(`Ch1Bosses.cs`): 태엽 현상금 사냥꾼(산탄 · 회전 난사 · 수배 저격 · 돌진 태클 벽 충돌 기절 · 하수인 호출 · 현상금 표식) / 녹은 시계 거인(휘두르기 · 웅덩이 · 12방향 숫자 탄 · 되감기 폭발 · 시간 지연 구역 · 초침 탄 · 체력 75/50/25% 시각 패턴). 중간 보스 방은 스테이지 2와 3 사이(`RunManager.BuildMidBoss`).
- **바닥 예고**(`Telegraph.cs`): 원 · 직선 · 부채꼴, 방향 추적.
- **웨이브**: 스테이지 1은 4~6마리 → 4는 6~9, 방마다 2~3번, 다음 웨이브 1초 예고.
- **맵 키트 조립 코드**(`Kit.cs`, `RoomBuilder.KitProps`): `Resources/Kit/Ch1/{Wall, Cliff, Floor, ClockTower, Keg, Wagon, Crates, Fence, MeltClock, BarCounter}` 프리팹이 있으면 벽 모듈 반복 · 바닥 타일 · 소품 배치. 화약통(쏘면 폭발, 연쇄) · 부서지는 엄폐물(적 탄 막음) · 술집 카운터 상인(솟아오름, 확률 15%). **키트 프리팹은 아직 없음** → 지금은 도형으로 나옴.

### 적 탄 이펙트
- Codex 손그림 스프라이트 6종(`docs/images/fx_src/enemy_bullets.png` → `docs/tools/fx_bullets_cut.py` → `Resources/FX/bullets/`). `EnemyProjectile.Style` = Orb · Revolver · Pellet · Sniper · Needle · Numeral(로마 숫자). 진행 방향으로 머리가 향하게 카메라를 봄.

### Tripo 모델 · 리깅
- 적 8종(`Ch1_Outlaw/Brute/Bomber`, `_Plus` 3종, `Ch1_MidBoss`, `Ch1_Boss`): Tripo 생성 → 리토포(약 1만 면) → Auto Rig(Mixamo) → FBX(Mixamo 프리셋 · 2K · 제자리) → `Art/tools/tripo_import.py` → `Game/Assets/Art/Characters/Ch1/` (Humanoid).
- 프리팹 빌더: Unity 메뉴 `CatTang/1장 적 프리팹 만들기 (Tripo)` (`Editor/EnemyModelBuilder.cs`) → `Resources/Enemies/*.prefab`. 툰 재질 + 원본 색 텍스처, 애니메이터(Idle · Run = 고양이 클립 리타깃, Attack · Special = AI 모션 키워드).
- AI 모션(Tripo 라이브러리에 있음): Firing Revolver · Running Tackle · Giant Spinning · Hip-Fired Shotgun · Creature Shaking. 클립은 `Ch1_Brute.fbx`(revolver · charges · swings) · `Ch1_Outlaw.fbx`(shotgun · revolver · shakes)에 들어 있음.
- 주의: 빌더 끝에서 컨트롤러 · 프리팹을 `ImportAsset(ForceUpdate)` 해야 실행 중 컨트롤러가 안 비어 보임(처리됨). 중간 보스 아바타는 무법자 뼈 매핑을 복사해야 휴머노이드로 인식됨(이미 처리, FBX 재가져오면 다시 필요).

### Tripo 작업 ID (studio.tripo3d.ai/workspace/{generate|retopology|rigging|animate}/<id>)
| 모델 | id | 상태 |
|---|---|---|
| 깡패 | 4f169a1d-a0bf-4791-a395-82fa0e6ffae9 | 리깅 · 모션 · 내보냄 |
| 무법자 | e3735cac-e419-4b15-a14a-6972deabc5e3 | 리깅 · 모션 · 내보냄 |
| 폭탄병 | 5333eaf8-41ee-467a-a552-4e206ff3ad56 | 리깅 · 내보냄 |
| 무법자+ | 058714ba-cd89-4d82-b144-fd466f610e23 | 리깅 · 내보냄 |
| 깡패+ | 2b887397-da75-4c37-aa5a-381e02bef504 | 리깅 · 내보냄 |
| 폭탄병+ | 7adf055a-04fd-4341-888f-4dcc5115a7a8 | 리깅 · 내보냄 |
| 중간 보스 | 5da44b06-bd2f-4dbb-97e1-7f832ca799c5 | 리깅 · 내보냄 (플랫 컬러) |
| 보스 | 2df4ffc0-066f-4129-8811-50ff78d2f18c | 리깅 · 내보냄 (플랫 컬러) |
| 화약통 | 237ba7ff-c892-4212-b01d-7323413c186e | 리토포 3천 · 미내보냄 |
| 포장마차 | 2e68a3bb-f6c3-43b3-b78c-4d62106fb2a5 | 리토포 5천 · 미내보냄 |
| 상자 | e89a41ea-69d6-4a5e-bbc4-d2d770ce9393 | 리토포 3천 · 미내보냄 |
| 울타리 | 0e1a1515-003e-4d18-ab2f-a0f2dd29b2e0 | 리토포 3천 · 미내보냄 |
| 녹은 시계 | 1a4a0393-242e-4c29-accf-47aa7c880011 | 리토포 시작 여부 확인 필요 |
| 술집 카운터 | 0f75fec6-3de1-4985-a5bc-a3a88111b128 | 생성만 됨 · 리토포 안 함 |
| 맵 타일 4종 | — | 입력 이미지만 있음(`Art/tripo_input/ch1/tiles/*_dn.png`), Tripo 생성 전 |

Tripo 크레딧 약 1,465. 생성 55 · 리토포 40 · 리깅 20 · AI 모션 20.
Tripo 웹 요령: 직접 URL로 열면 고면수 모델 로딩에 20초쯤 걸림 → 로딩 전에 버튼을 누르면 무시됨(크레딧 줄었는지로 확인). 생성이 끝나면 화면이 다른 모델로 넘어가니 내보내기는 생성이 다 끝난 뒤에.

## 2026-11-16 밤 2차 작업 (커밋됨)

- **건카타 이펙트 누락 원인 = 정적 캐시.** 프로젝트가 플레이 진입 시 도메인 리로드를 끔(`DisableDomainReload`) → 정적 필드가 판을 넘어 남는데, 런타임에 만든 재질 · 메시는 플레이 종료 때 파괴됨. `GunkataFx.Init`이 `inited`만 보고 재초기화를 안 해서 **두 번째 플레이부터 총구 섬광 전부 안 나옴**. `GunkataFx.Init` · `FxFlipbook.Load` · `FxMeshes`가 죽은 재질 · 메시면 다시 만들게 고침. 새 정적 캐시를 만들 땐 꼭 `if (!cached)`(유니티 null 비교)로 확인할 것.
- **폭탄병 도약 자폭**: 사거리 7m 안이면 웅크림 0.35초(착지 자리에 작은 노란 조준 표시) → 고양이가 있던 자리로 포물선 도약 0.55초 → 착지 먼지 → 예고 원이 작게 시작해 0.8초 동안 반경 2m까지 커짐(`Telegraph.Grow()`) → 다 커지면 폭발.
- **점프 모션**: `Art/Characters/Ch1/Anim/Ch1_BomberJump.anim` — 휴머노이드 근육 커브로 직접 만든 클립(웅크림 → 이륙 → 공중 웅크림 → 착지). 메뉴 `CatTang/폭탄병 점프 클립 만들기`, 빌더가 폭탄병 Special에 연결. 키 표는 `EnemyModelBuilder.JumpKeys`.
- **3D 폭발** `GunkataFx.Explosion`: 섬광 덩어리 → 불덩이 무리 → 그을음 · 모래 덩어리 → 바닥 충격 고리 → 모래 파편. 탄 폭발 효과(`Effects.Explosion`)는 탄 색으로 물든 버전.
- **타격 · 총 효과 · 닷지 원을 3D 툰 불꽃으로 통일**: `HitImpact`(탄 색 섬광 + 튀는 가시 + 고리, 치명타 = 금빛 네 갈래 별) · `DodgeBurst`(바닥 고리 두 겹 + 몸 감싸는 고리 + 사방 빛살, 닷지 액션 보라 / 확률 회피 청록) · `Flare`(공명 · 범위 기절 · 완벽 장전 등). `FxMesh.Tint(색)`로 탄 색을 입힘.
- **고리 메시 법선이 안쪽으로 뒤집혀 있었음**(`FxMeshes.Torus`) → 툰 불꽃 고리가 검은 테로만 보였음. 고침(건카타 충격 고리도 이제 밝게 나옴).
- **몬스터 크기 — 신체 비율 측정 후**: 고양이 머리(목~정수리 0.80m, 키 2.17m, 2.7등신) 기준으로 각 모델 머리 크기를 맞추고 몸은 모델 원래 비율대로. 폭탄병 1.8 · 폭탄병+ 2.2 · 무법자 2.7 · 무법자+ 3.0 · 깡패 2.8 · 깡패+ 3.2(머리가 작아 어깨 기준) · 중간 보스 3.9 · 보스 6.5. 콜라이더도 실제 폭에 맞춤(`EnemyFactory`), 강화형은 판정 1.15배. 측정 근거는 `EnemyModelBuilder.Models` 주석.
- **외곽선** 2.5 → 4.

## 2026-11-16 밤 3차 작업 — 블렌더 VFX · 이펙트 프리팹 · 극한 회피 (커밋됨)

- **블렌더 설치**(winget, 5.3). VFX 메시 스크립트 `Art/tools/blender_fx.py` (백그라운드 실행, Unity 좌표로 설계 → 블렌더 좌표 변환해 FBX). 원본 `Game/Assets/Art/FX/Meshes/fx_*.fbx` → Unity 메뉴 `CatTang/FX 메시 가져오기 (블렌더)` → `Resources/FX/Meshes/fx_*.asset` (FBX 하위 메시는 MCP로 참조가 안 돼서 독립 자산으로 뽑음).
  - 공통 조각: 가시 3 · 불꽃 3 · 불덩이 3 · 고리 → `FxMeshes.Spike/Flame/Ball/Torus`가 먼저 씀 (건카타 · 총구 · 폭발 · 닷지 전부 블렌더 메시로 바뀜, 없으면 코드 메시)
  - 타격용: 만화 충격 별 · 튀는 왕관 · 파편 3 · 4갈래 반짝임 · 초승달 · 불씨 (+ 지금은 안 쓰는 아이콘 모양들: 고양이 고리 · 꽃 · 나선 · 시계 · 눈 · ∞)
- **이펙트 프리팹은 MCP로 에디터에서 직접 만든 오브젝트** (`Resources/FX/Prefabs/`): 루트 `FxEffect` + 조각 `FxPart`(수명 · 지연 · 이동 · 회전 · 카메라 보기 · 탄 색 여부). 런타임은 `FxEffect.Spawn(이름, 자리, 방향, 크기, 탄 색)`만 함.
  - `FX_Hit` · `FX_HitCrit`(금빛 별 · 십자 · 고리) · `FX_DodgePerfect` · `FX_WarnGlint`
  - `FX_Trait_*` 9종 — 사용자: 아이콘 말고 타격 자체로 개성, 칼바람 증강 느낌. 회상 = 잔상 타격 메아리 · 추억 = 꽃잎 파편 + 오래 남는 자국 · 망각 = 얼음 가시가 박혀 멈춤 · 되감기 = 안으로 빨려 든 뒤 터짐 · 종말 = X자 베기 + 위에서 떨어지는 칼날 · 지연 = 작게 → 0.35초 뒤 위에서 떨어져 크게 · 예지 = 긴 관통 + 십자 반짝임 · 영원 = 파편이 떠서 멈춤 · 미련 = 불씨가 피어오름. 특성 있는 탄은 일반 타격 대신 이것(치명타면 금빛 겹침).
  - 주의: MonoBehaviour는 파일 이름 = 클래스 이름이어야 프리팹에 저장됨 (`FxPart`를 `FxEffect.cs`에 넣었다가 참조가 비어 고침).
- **극한 회피(닷지 액션) — 젠레스 존 제로 참고**: 순간 파란 섬광 → 화면 회색 · 차가운 필터 · 색수차 · 비네트(`ScreenFx`, 런타임 전역 볼륨, 카메라 후처리를 켬) → 슬로우 0.45초 + 회복 0.35초, 파란 잔상, `FX_DodgePerfect`(바닥 고리 · 몸 감싸는 고리 · 차례로 터지는 반짝임 · 빛살).
- **적 공격 예고 섬광**: 모든 Telegraph가 끝나기 0.15초 전 주황 십자 반짝임(`FX_WarnGlint`) — 이때 피하면 극한 회피. 폭탄병 착지 조준 표시는 제외(`NoGlint()`).
- 확인 안 됨: 회색 화면은 캐릭터 · 이펙트까지 다 회색이 됨(ZZZ처럼 캐릭터만 색을 남기려면 별도 렌더 레이어 필요). 실제 전투에서 이펙트 수가 많을 때 성능은 안 재 봄.

## 남은 다음 작업

1. **명암 그림으로 뽑은 6종(기본 3종 + 강화형 3종)을 플랫 컬러로 다시 생성.** GPT에 "같은 디자인, 명암 · 하이라이트 · 빛 없이 고유색만, 외곽선 유지"로 다시 그리기 → 디노이즈 → Tripo 생성 → 리토포 1만 → 리깅 → 내보내기 → `tripo_import.py` → 빌더.
2. **공격 모션을 공격에 맞게 다시.** 사용자: "돌진 공격 같은 모션이 안 어울린다." 지금 AI 모션은 4~5초짜리라 0.8~1초로 빨리 돌림. Tripo 모션 길이 슬라이더를 1~2초로 줄이고, 동작별로 짧고 분명한 프롬프트로 다시 만들 것 (예: 돌진 = 몸을 낮추고 어깨로 들이받으며 앞으로 달려 나감 · 사격 = 총을 들어 한 발 쏘고 반동 · 자폭 = 웅크렸다 부풀어 오름 · 휘두르기 = 양팔로 크게 한 바퀴). 클립 이름 키워드는 `EnemyModelBuilder.Anims`에 맞추기.
3. **맵을 지금 구울 것.** 남은 소품 내보내기 + 맵 타일 4종 Tripo 생성 · 리토포 · 내보내기 → 빌더로 `Resources/Kit/Ch1/*` 프리팹 → **MCP로 방 프리팹을 직접 생성**: 1장 기획서대로 전투방 10개(서부 거리 · 마을 광장 · 황무지 절벽길 · 건물 내부, 방 크기 1.5~2 화면, 화약통 · 엄폐물 후보 자리) + 중간 보스 술집 앞 큰길(30×14m, 양옆 건물) + 보스 원형 광장(지름 24m). 런타임(`RunManager` · `RoomBuilder`)은 스테이지 장소에 맞는 방 프리팹을 골라 놓기만 하게 바꿀 것.

## 확인 안 된 것

- 실제 판을 처음부터 끝까지(스테이지 1 → 중간 보스 → 스테이지 3 · 4 → 보스) 이어서 해 보지 않았음. 각 적 · 보스 · 패턴은 따로 소환해서 확인함.
- 폭탄병 도약 · 커지는 예고 · 3D 폭발은 화면으로 확인함. 화약통 연쇄 · 엄폐물 파괴 · 시간 지연 구역 · 되감기 폭발은 아직.
- 몬스터를 키운 뒤 보스 · 중간 보스 패턴 사거리(휘두르기 반경 · 손 판정 등)와 적 탄 발사 높이는 다시 맞추지 않음. 몸에 비해 작아 보이면 조정.
- 폭탄병 HP바는 도약 중 모델을 따라 올라가지 않음(바닥 높이에 남음).
- 적 FBX가 하나에 약 25MB, `Game/Assets/Art/Characters/Ch1` 206MB. 푸시가 무거우면 Git LFS 고려.

## 주요 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Game/Enemy.cs` | 일반 적 3종 · 강화형 행동, 애니메이터 연결(`Anim`, `AnimBusy`) |
| `Scripts/Game/EnemyFactory.cs` | 적 생성(프리팹 있으면 모델, 없으면 도형), 강화형, 중간 보스 · 보스 |
| `Scripts/Game/BossBrain.cs` | 패턴 고르기 · 페이즈 · 패턴 실행 공통 |
| `Scripts/Game/Ch1Bosses.cs` | 중간 보스 · 보스 패턴, 웅덩이 |
| `Scripts/Game/Telegraph.cs` | 바닥 예고 |
| `Scripts/Game/EnemyProjectile.cs` | 적 탄 스프라이트 6종 |
| `Scripts/Game/Kit.cs` | 맵 키트 조립 · 화약통 · 엄폐물 · 솟아오르기 |
| `Editor/EnemyModelBuilder.cs` | Tripo 모델 → 툰 프리팹 + 애니메이터 |
| `Art/tools/` | `denoise.py` · `sheet_cut.py` · `tripo_import.py` |
| `Art/tripo_input/ch1/` | Tripo 입력 이미지(적 · 강화형 · 보스 · 소품 · 타일) |
