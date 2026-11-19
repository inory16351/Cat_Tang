# 핸드오프 — 1장 적 · 보스 · Tripo 모델 · 리깅 · 적 탄 이펙트

마지막 갱신: 2026-11-19 (시설 화면 스크롤바 그림 연결 · 빌드 표 복사 — "2026-11-19 — 윈도우 빌드" 절). 그 전: 2026-11-18 밤 (마을 오브젝트 정리 뒤 씬 다시 지음 · 확인 — "(마지막, 다른 세션에서 이어서)" 항목). 그 전: 2026-11-18 밤 (새 마을: 다리로 이어진 섬 5개 · 장 해금 · NPC 5 — "2026-11-18 (밤 2) 새 마을" 절). 그 전: 2026-11-18 밤 늦게 (아웃게임 개편 · 메인 로비 · 마을 — "2026-11-18 (밤 늦게)" 절). 그 전: 2026-11-18 저녁 (3장 맵 키트 · 거울 반사 바닥 — "2026-11-18 (저녁)" 절). 그 전: 2026-11-18 오후 (상인 · 추억 · 3장 구현 시작). 그 전: 2026-11-18 (3장 레벨 디자인 확정 · 3장 기획서 Ver01 — 구현 전, 아래 "3장 구현 할 일" 참고). 이전 핸드오프(연출 · HUD · 판 구조)는 git 기록 `459df5c`의 HANDOFF.md 참고.

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
| 건물 벽 (Wall) | e3468f4d-05ba-4556-9dd0-8ff2e4a355f5 | 생성 · 내보냄 (5,764면, 리토포 불필요) → Kit_Wall |
| 절벽 (Cliff) | c9aa57c8-d85d-4694-bc66-ddb876ea7028 | 리토포 4,637 · 내보냄 → Kit_Cliff |
| 바닥 (Floor) | 8046bd04-5b38-4e3d-bee4-290b8c764fa2 | 리토포 3,000 · 내보냄 → Kit_Floor |
| 시계탑 (ClockTower) | 80ee52ed-938a-4b0a-b703-0be9f3f1edf3 | 리토포 7,867 · 내보냄 → Kit_ClockTower |

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

## 2026-11-16 밤 4차 — 맵 섹션 (Tripo + Codex 도형 모델링)

- **섹션 나눔**: 디테일이 중요한 것만 Tripo(시계탑 · 건물 벽 · 절벽 · 바닥 타일 · 포장마차 · 녹은 시계 · 술집 카운터), 나머지는 **Codex가 블렌더 스크립트로 도형 중심 모델링** (사용자: Tripo 토큰 절약, SOUL/ALONE 방식 참고 — `codex exec -s workspace-write -`에 명세 파일).
  - 명세 `Art/tools/specs/kit_simple_spec.md` · `kit_props_spec.md` → 스크립트 `Art/tools/blender_kit_simple.py`(판자벽 · 보도 · 모래주머니 · 술통 더미 · 말 묶는 기둥 · 물통 · 표지판 · 가로등 · 바위 3 · 난간 · 술집 문) · `blender_kit_props.py`(화약통 · 상자 · 울타리). 재질 색만(텍스처 없음), 빌더가 텍스처 없으면 재질 색을 씀.
  - Tripo 기록(리토포 버전) 열람은 유료라 예전 리토포는 못 꺼냄 → 다시 리토포함. 포장마차 · 녹은 시계는 리토포 버전이 열려서 그대로 내보냄.
- Tripo 업로드는 내장 브라우저에 파일 업로드가 없어 **Claude in Chrome(이 PC의 Chrome)** 으로 함. 다른 PC의 "Browser 2"가 연결돼 있을 수 있으니 `select_browser`로 이 PC 것을 고를 것.
- 빌더 메뉴 `CatTang/1장 적 프리팹 만들기 (Tripo)` → `Resources/Kit/Ch1/*` 21개. 지금 방 생성(`RoomBuilder`)이 키트를 자동으로 씀(바닥 타일 · 벽 모듈 · 소품).
- 남음: 카운터 내보내기, **MCP로 방 프리팹 직접 조립**(전투방 10 · 중간 보스 큰길 · 보스 광장), Codex 조각(판자벽 · 보도 · 모래주머니 등)을 방 배치에 쓰기.

## 2026-11-17 — 바닥 · 닷지 레이어 · 술집 흐름 · 적 랜덤성 · 지도 (커밋됨)

- **이음매 없는 바닥**: `Art/tools/ground_texture.py`로 이어지는 흙 · 술집 판자 텍스처, `Kit.TileFloor`가 방마다 큰 판 하나 + 바닥 조각 흩뿌리기(Codex 블렌더 `blender_kit_ground.py`).
- **닷지 Vivid 레이어(8)**: PC_Renderer에 후처리 뒤 RenderObjects 2개. 회색 화면에서도 고양이 · 이펙트는 색 유지(`VividLayer`). 스프라이트는 `CatTang/Sprite` 셰이더(LightMode 태그 필수).
- **중간 보스 술집**: 스테이지 2 마지막 방 뒤에 술집 홀을 붙여 지음 → 걸어 들어가고 뒷문 복도로 스테이지 3에 이어짐(순간이동 없음).
- **보스 기믹 숫자**: `BigLabel` 화면 배지(어두운 원 + 금테, 바뀔 때 튐, 위험 시 맥동).
- **일반 적 패턴(사용자 확정)**: 무법자 단발 + 3점사 + (강화형) 옆 대시(실루엣, 닷지 발동 안 함) · 옆걸음 · 재배치 / 깡패 돌진(발 구르기 클립 `Ch1_BruteStomp` + 흙먼지) + 내리찍기 · 우회 접근 / 폭탄병 도약 자폭 + 모래 폭탄 투척. 가중 랜덤, 직전 공격 ×0.25. 이동 방향을 바라봄. `Scripts/Game/EnemyMoves.cs`.
- **텔레그래프**: 진홍 분홍 / 보라, 어두운 그림자 판 + 차오르는 색 판 + 테두리 밑 잉크 선.
- **방 모양**: `RoomBuilder.AddShape` — 사각 15% · 모서리 홈 35% · L자 30% · 가운데 섬 20%(보스 · 술집 제외). 막힌 덩어리는 안쪽 면에 절벽/건물 벽 모듈, `RoomInfo.blocks`(월드 XZ Rect). `RunManager.ClampToRoom`이 밖으로 밀어냄(바깥 벽 쪽으로는 안 밂), 적 생성 · 소품도 피함.
- **M 지도**: `Scripts/UI/MapView.cs` — GPT 양피지 판 · 방 타일 · 고양이 머리 아이콘(`Resources/UI/map/`, 프롬프트 `docs/images/ui_src/prompts/map_ui.txt`). 방 사이는 방 판과 같은 색 굵은 띠로 이음(세피리아식), 막힌 곳 어둡게, 안 간 방 흐림, 지금 방 금테.

## 2026-11-17 오후 — 절차적 벽 · 지형지물 · 자폭병 던지기 · 브릿지

- **원칙(사용자)**: Tripo는 몬스터 · 캐릭터(복잡한 형태 · 리깅 · 모션)만. 벽 · 바닥 · 소품은 블렌더 + Codex CLI(`codex exec -s workspace-write -` < `Art/tools/specs/*_spec.md`).
- **절차적 건물 줄**: `Kit.BuildingRow` — 건물 정면 8종(`blender_kit_facades.py`)을 벽 길이에 맞춰 무작위로 이어 붙임(같은 건물 연속 X, 높이 ±, 물러남 0~0.7, 18% 골목, 남쪽 벽 낮춤). 절벽은 조각마다 높이 · 들여쓰기 · 폭 흔들기. 복도 벽도 TileWall로 덮음.
- **방 28×24로 확대**(`RoomBuilder.RoomSize`, `CellSize` 38×34, `CellDepth` 34, 술집 = 방 크기).
- **방 모양 = 큰 지형지물**: 덩어리로 깎던 방식은 "갈색 상자 · 이상함"이라 버림. `RoomBuilder.AddShape`가 방마다 1~3개 배치(모서리 쪽 틈 0.3~2.2m 또는 안쪽, 90°±15°, 문 앞 십자 ±2.6 · 가운데 반지름 4 비움). 황무지 = 바위 기둥 · 바위 더미 · 포장마차 잔해 · 광산 수레 · 죽은 나무, 마을 = 급수탑 · 역마차 · 우물 · 건초 수레 · 노점 · 목재 더미(`blender_kit_landmarks.py`, 빌더에서 4~6m로 키움). 발자국은 `RoomInfo.blocks`.
- **자폭병 던지기**: 원인 = 투척에 자폭 떨기 클립(Attack)을 쓰고 예비 동작 없이 바로 폭탄이 날아감. `MakeThrowClip`(`Ch1_BomberThrow`, 0.34초 손 떠남) + 애니메이터 `Throw` 트리거/상태(빌더) + `BomberThrow`가 제자리에서 젖혔다 놓은 뒤 폭탄 비행. 폭탄 모델 `Kit_SandBomb`(모래주머니 + 모래시계 뚜껑 + 불꽃 심지, `blender_kit_sandbomb.py`). SetTrigger("Throw") → Throw 상태로 가는 것은 확인. 한 번 AI 투척 때 Attack 상태가 잡힌 적이 있음 → 남아 있던 Attack 트리거일 수 있으니 `BomberThrow` 시작에 `anim.ResetTrigger("Attack")` 추가를 검토.
- **브릿지**: Tripo CLI 0.11.2(로그인 · `tripo doctor` 통과, 1000 크레딧). Unity Tripo Bridge 1.0.14 = `Game/Packages/com.tripo3d.unitybridge`(메뉴 Tools > Tripo Bridge). Blender Tripo Bridge 1.1.0 = Blender 5.3 사용자 애드온, 활성화됨(ws 60600).

## 2026-11-17 — 나무 문 · 부서지는 연출

- **모델**: `Art/tools/blender_kit_woodgate.py`(Codex, 명세 `specs/kit_woodgate_spec.md`) → `Kit_WoodGate.fbx` 4.6×2.6m. 세로 판자 8장을 지그재그 부러진 선으로 2~3토막씩 미리 쪼갬 + 앞면 가로대 2 · 대각 버팀목 · 금빛 흘러내림, 뒷면 쇠띠 2 + 리벳. 조각 32개(`Piece_XX`) + 가시 틀 3개(`Splinter_X`), 3.2천 삼각형. 빌더 표에 `Kit_WoodGate` 추가 → `Resources/Kit/Ch1/WoodGate.prefab`.
- **`Scripts/Game/WoodGate.cs`**: `WoodGate.Make`(충돌 상자 + 모델, 정면이 방 안쪽) · `Close`(위에서 0.2초 낙하 쾅 → 흙먼지 · 살짝 눌림 · 흔들림) · `Break`(0.28초 덜컹 → 조각마다 복제해 고양이 반대쪽으로 날림, 가시 12~16개, 흙먼지 · 부스러기 · 흔들림). 조각은 `GateDebris`(물리 엔진 없이 중력 · 회전 · 바닥 튕김 → 2~3초 뒤 가라앉아 사라짐). 키트 없으면 예전 색 상자.
- `RoomBuilder`의 일반 방 `Gate` · 술집 `SaloonGate` · `BackGate` 전부 이것. `RunManager`는 `SetActive` 대신 `WoodGate.Close/Break`(술집 문 열림도 부서짐).
- 확인: 플레이에서 슬로모션으로 닫힘 · 덜컹 · 폭발 · 정리(조각 0개) 확인, 에러 없음. 술집 문은 화면으로 따로 안 봄.

## 2026-11-17 — 하트 회복 · 배경 톤 · 적 외곽선

- **하트**(`Scripts/Game/HeartPickup.cs`): 적 처치 시 일반 2% · 강화형 3%(2026-11-17 하향, 처음엔 6% · 10%) · 중간 보스 확정(`RunManager.OnEnemyKilled`). 튀어나와 떨어진 뒤 떠서 돌고, 고양이가 1.6m 안에 오면 빨려 와 최대 체력 20% 회복(가득이면 안 빨려 옴). 붉은 후광(맥동) + 바닥 붉은 점광원 + 가끔 반짝임. 모델은 블렌더 + Codex `blender_kit_heart.py` → `Resources/Kit/Ch1/Heart.prefab`(×1.4로 띄움, 재질 채도 1.15 · 밝기 1.12).
- **상인**: 예전 "체력 회복 40% (50골드)" 버튼 → "하트 · 체력 20% (25골드)", 상인당 3개(`Merchant.heartsLeft`).
- **배경 톤 낮추기**(→ 아래에서 되돌림): 툰 셰이더에 `_Saturation` · `_Brightness` 추가. `Editor/EnvTone.cs`(메뉴 `CatTang/배경 톤 낮추기`)가 맵 키트 재질 채도 0.55(밝기는 1 — 고양이 · 적이 어두워서 바닥을 어둡게 하면 오히려 묻힘). 하트 · 모래 폭탄 · 화약통은 제외. 빌더(`ToonFor`)도 키트를 만들 때 같은 값을 넣음.
- **적 외곽선**: 안쪽 선은 예전처럼 어두운 잉크, **빨간 띠(0.78, 0.07, 0.12 · 3.5px)는 실루엣 바깥에만**. 뒤집은 겹 외곽선은 팔 · 몸이 겹친 안쪽에도 생겨 빨강이 너무 많았음(사용자: 눈 아픔) → 툰 셰이더 몸 · 외곽선 패스가 스텐실(`_OuterStencilRef`, 적 = 4)을 찍고, 새 패스 `OuterOutline`(LightMode `CatTangOuterOutline`)이 스텐실 바깥에만 그림. 이 패스는 PC_Renderer의 RenderObjects 기능 "적 바깥 테두리"가 불투명 다음에 그림(메뉴 `CatTang/적 바깥 테두리 렌더러 기능 추가`). 빌더 `EnemyEdge`가 적 재질에 자동 적용.
- **적 배색 2안 확정**: 툰 셰이더 `_Recolor*`(색 범위 치환) — 적 몸의 적갈색(355°±27°)만 차가운 보라 · 청회색(255~270°)으로, 금빛 금속 · 선명한 진홍 포인트는 유지. `Editor/EnemyPalette.cs`(메뉴 `CatTang/적 배색/1안 · 2안 · 원래`), 빌더가 적 재질에 2안을 넣음. 비교 이미지 `docs/images/compare/`.
- **맵 채도는 원래대로 되돌림**(사용자): `EnvTone.Saturation = 1`. 적이 차가운 색이 되어 주황 맵과 갈리므로 배경을 낮출 필요가 없어짐. 셰이더 값 · 메뉴는 남겨 둠.

## 2026-11-17 — 2장 모델링 (Tripo CLI · 블렌더 + Codex) · 배색 분리 · 폭발물 테두리 (커밋됨)

- **원칙(사용자)**: 맵 = 따뜻하게, 몬스터 = 차갑게(보라 · 청회색 + 빨간 바깥 테두리). 바깥 테두리 = 위협 신호 — 몬스터 빨강, **쏘면 터지는 물체 = 주황 ↔ 노랑 맥동**. 규칙은 `Art/README.md` 5 · 6절.
- **폭발물 테두리**: `Scripts/Game/DangerEdge.cs` — 적과 같은 `CatTangOuterOutline` 패스, 스텐실 5, 두께 5px, 1.1초 주기 맥동(재질 인스턴스). 1장 화약통은 `PowderKeg.Make`가, 2장 `SteamValve` · `FloatLuggage` · `MineCart`는 빌더(`EnemyModelBuilder.Explosives`)가 프리팹에 붙임. 플레이에서 화약통 · 광차 · 짐 테두리 확인.
- **캐릭터 8종(Tripo CLI, H3.1 + rig v3.0 Mixamo, 마리당 약 65 크레딧)**: 원화는 ChatGPT 웹(정면 A자 · 플랫 컬러 시트) → `Art/tools/keep_main.py`(옆 조각 지움) → `denoise.py` → `tripo make ... --model tripo-v3.1` → `Art/tools/tripo_cli_import.py` → `Art/Characters/Ch2/Ch2_*.fbx`(Humanoid) → 메뉴 `CatTang/2장 프리팹 만들기 (Tripo + 블렌더)` → `Resources/Enemies/Ch2_*`.
  - 차장 · 짐꾼 · 신호수 · 강화형 3종 · 태엽 역장(중간 보스) · 멈춘 기관차 거인(보스). 입력 이미지 `Art/tripo_input/ch2/`, Tripo 원본 `Art/models/ch2/`(git 제외).
  - 모션은 아직 1장 클립을 키워드로 돌려 씀(`EnemyModelBuilder.Anims`의 Ch2_* 줄). 전용 공격 모션은 다음 작업.
- **맵 키트(블렌더 + Codex, 회색 + 역할 재질 → `ModelPalette.Ch2`)**: 명세 `Art/tools/specs/kit_ch2_{station,props,landmarks}_spec.md` + 공통 `_ch2_common.md`.
  - 벽면 8: 역사 · 대합실 · 창고 · 기관고 · 승강장 캐노피 · 매표소 · 벽 · 둑(Cliff). 소품 10: 증기 밸브 · 멈춘 짐 · 광차 · 선로 · 벤치 · 짐 더미 · 상자 · 매표 부스(상인) · 신호등 · 역 시계. 지형지물 8: 객차(옆면 열림) · 기관차 · 급수탑 · 석탄 더미 · 짐 수레 · 신호 갠트리 · 전차대(보스 바닥 24m) · 승강장 가장자리.
  - 바닥: `Art/tools/ground_texture_ch2.py` → `Art/Env/Ch2/ground_ch2.png`(포석) → `Resources/Kit/Ch2/Ground.mat`.
- **빌더**: `SrcDir`가 챕터마다 바뀜(`Ch1Src` · `Ch2Src`), `ModelPalette.Active`. 2장은 `BuildCh2`(`Models2` 표).
- **2장 방 조립 (`Scripts/Game/Ch2Kit.cs`)**: `Ch2Kit.Is(ch)`(2장 + 키트 있음)면 `Kit.BuildingRow`가 장소별 정면 세트(승강장 = 캐노피 · 대합실 · 매표소 · 역사 / 객차 = 캐노피 · 대합실 · 창고 / 기관고 · 보스 = 기관고 · 창고 · 벽, 캐노피는 북쪽 벽에만, 남쪽 벽 ×0.55 높이), 스테이지 1은 둑(Cliff). `RoomBuilder.AddShape` = 장소별 지형지물 + 객차 방은 `Ch2Kit.Coaches`(객차 2~3량, 열린 면이 방 가운데, 등판 · 양 끝만 충돌). `KitProps` = `Ch2Kit.Props`(선로 한 줄 · 광차 · 증기 밸브 · 공중에 멈춘 짐 · 엄폐물 · 역 시계 / 신호등, 보스 = 전차대 + 모서리 신호등). 상인 = `TicketBooth`.
- **2장 폭발물 동작**: `SteamVent`(0.5초 예고 선 → 정면 6m × 1.5m 증기 1.5초, 적 40 · 고양이 15, 쏜 뒤 파이프만 남고 테두리 꺼짐 `DangerEdge.Off`) · `FallingLuggage`(→ 아래에서 `FlyingLuggage`로 바뀜: 맞은 방향으로 날아가 폭발) · `RunawayCart`(0.3초 예고 선 → 선로 따라 12m/초, 끝에서 부서짐, 적 50). 플레이에서 셋 다 발동 · 적 피해 확인(적 탄에 맞아도 터짐).
- 플레이 확인: 2장으로 바꿔 스테이지 1~4 · 보스 방을 지어 화면으로 봄. 바닥 납작 조각(전차대 · 선로 · 승강장 가장자리)은 외곽선 1(판자 선 지글거림 줄임).
- **남음**: ~~2장 몬스터를 2장 모델로~~(아래 '2장 몬스터 연결'에서 함), 2장 중간 보스(대합실 · 태엽 역장, `HasMidBoss`는 1장만), 2장 보스 패턴, 지나가는 열차(12초), 2장 전용 공격 모션. 표 `Chapters`의 2장 이름이 아직 '무너지는 마을'.

## 2026-11-17 — 2장 몬스터 연결 (커밋됨)

- **`Scripts/Game/EnemyCh2.cs`**(Enemy partial): `Enemy.Kind`에 `Conductor` · `Porter` · `Signal`. 수치는 레벨 디자인 md 2장 값 그대로(상수 표가 파일 위쪽), 체력 · 피해에 챕터 배율 안 곱함(차장 60 · 짐꾼 160 · 신호수 60 / 피해 10 · 15 · 12), 강화형 1.5배는 `MakeElite`.
  - **차장**: 7~10m 유지 · 옆걸음(`KeepRange` — 무법자 이동을 떼어 낸 공용 함수), 2.5초마다 조준선 3개 0.6초 → 승차권 3발(부채 30°, 탄속 9). **차장+ 검표 펀치**: 6초 쿨, 굵은 진홍 조준선이 0.45초 따라오다 고정 → 엄폐물 뚫는 큰 승차권(탄속 15), 맞으면 `PlayerController.dashLockUntil` 2초(HUD 차오르는 대시 칸이 붉게 깜빡임).
  - **짐꾼**: `Health.BlockFrom = PorterBlocks` — 정면 ±60°에서 온 고양이 탄은 `PlayerController` 사격 루프에서 벽처럼 멈춤(쇳빛 불꽃, 관통 · 튕김 없음). 몸 돌리는 속도 150°/초로 느려 옆 · 뒤로 돌면 뚫림. 내리찍기: 부채꼴 예고 0.7초(전방 90° · 2.5m) → 피해 15 + 밀림 → 1.2초 빈틈(방패 꺼짐, 모델 앞으로 숙임). **짐꾼+ 트렁크 던지기**: 8초 쿨, 예고 0.8초 직선 10m × 1.4 → `Kit/Ch2/FloatLuggage`를 빙글 돌리며 16m/초(벽에서 부서짐, 피해 15), 던진 뒤 3초 방패 없음. 트렁크 비행은 RunManager가 돌려서 짐꾼이 죽어도 끝까지 감.
  - **신호수**: 8m 유지, 4초마다 던지기 클립 0.34초 → `SignalBlast` 2개(고양이 자리 + 둘레 2.5~3.5m). 포물선 0.35초 → 작은 신호등(기둥 + 등 + 점광원) 노랑 1.5초, 끝 0.35초 빨강 빠른 깜빡임 → 폭발 반경 2m 피해 12. 신호수가 죽어도 이미 던진 건 터짐. **신호수+ 정지 구역** `StopZone`: 반경 2m · 3초, 청록 원 + 느리게 도는 시곗바늘, 안에 든 적 탄 `frozenUntil` → 끝나면 원래 방향으로 다시 날아감. 고양이 탄은 즉발이라 대상 아님(기획서 '양쪽 모두' 중 고양이 쪽은 미구현).
- `EnemyProjectile`: `Style.Ticket`(스프라이트 `Resources/FX/bullets/ticket.png`, 생성 스크립트 `docs/tools/fx_ticket_bullet.py`) · `All` 목록 · `frozenUntil` · `pierceCover` · `OnHitPlayer`(실제로 체력이 깎였을 때만).
- `EnemyFactory.RandomMinion`: 2장이면 차장 40% · 짐꾼 30% · 신호수 30%, 모델 `Ch2_*`(강화형 `_Plus`).
- `RunManager`: 중간 보스가 없는 장(지금은 2장)도 스테이지 3부터 강화형. `Data/Tables/Chapter.csv` 2장 이름 '멈춘 기차역' · 보스 이름 '멈춘 기관차 거인'(체력은 2장 보스 패턴이 생길 때까지 1300 유지, 기획 값 4860).
- **플레이 확인**: 2장 스테이지 3 방에 강화형 3종을 소환 → 공격 · 피해 들어옴, 펀치 쿨 · 트렁크 쿨이 돎, 정지 구역 생성과 그 안에 멈춘 승차권을 화면으로 봄, 펀치 탄 맞으면 대시 2초 잠김 확인, 짐꾼 방패 판정(정면 막음 · 뒤 통과) 확인, 콘솔 에러 없음.
- **확인 안 됨**: 실제 마우스 사격으로 짐꾼 정면이 막히는 화면(판정 함수만 확인), 신호등 노랑 → 빨강 화면, 판을 처음부터 이어서 2장 웨이브. 고양이가 밀림(`Health.Knockback`)으로 벽을 넘어가는 문제는 1장부터 있던 것 — 테스트 중 방 밖으로 밀려 떨어진 적 있음(단, 이번 테스트는 방에 들어가지 않고 `BuildStage`만 불러 `current`가 비어 있던 상태).
- 테스트 방법: 플레이 중 `rm.chapter = 2`, `Chapter` 속성 = `Tables.Chapter(2)`(리플렉션), `BuildStage(n, RewardKind)` 리플렉션 호출 → `EnemyFactory.Conductor/Porter/Signal(pos, ch, elite)`를 `rm.AddEnemy`. 고양이 체력은 `ResetHp(100000)`으로.

## 2026-11-17 — 짐꾼 모션 · 2장 적 패턴 다양화 · 추적 · 공중 짐 · 몬스터 공격 반응 (커밋됨)

- **짐꾼 모션 원인**: Tripo 자동 리깅이 몸 앞 트렁크를 팔(주로 오른팔뚝)에 붙여 스키닝 → 고양이 대기 · 달리기 클립을 돌려 쓰면 트렁크가 접히거나 몸속으로 사라짐.
- **`Editor/PorterRig.cs`** (빌더가 짐꾼 빌드 때 자동 실행, 메뉴 `CatTang/2장 짐꾼만 다시 만들기`):
  - `SplitTrunk`: 원본 비율 상자(앞 0.085~0.32 · 높이 0.17~0.70 · 좌우 ±0.27, 강화형 ±0.29 — 블렌더에서 정점 분포를 그려 정함)로 트렁크 삼각형을 골라 **몸 메시(`_BodyMesh`)와 트렁크 메시(`_TrunkMesh`, 전부 새 뼈 `Trunk` = Spine2 자식)로 나눔** → 프리팹 `Mesh/TrunkSkin` 렌더러. (뼈를 줄여 숨기는 방식은 경계가 늘어나 부채꼴 자국이 생겨 버림)
  - 전용 클립 5개(`Art/Characters/Ch2/Anim/Ch2_Porter{,_Plus}{Idle,Walk,Slam,Throw,Bash}.anim`): 팔 근육은 모델 기본 자세값(= 트렁크 든 자세, `HumanPoseHandler`로 읽음) 고정 + 다리 · 허리만 키. 애니메이터 Idle/Run = 대기 · 걷기, Attack = 방패 돌진, Special = 내리찍기(0.7초 쾅 → 빈틈 동안 숙임), Throw = 트렁크 던지기(0.8초 놓음).
  - `Scripts/Game/FootGround.cs`: 애니메이션 뒤 가장 낮은 발 뼈를 바닥에 붙임(다른 체형에 클립을 돌려 쓰면 떠 보였음). 기준 높이는 빌드 때 메시를 구워 잼.
- **트렁크 던지기**: 손의 트렁크를 그대로 구워(`DetachTrunk`) 날리고 `TrunkSkin`을 끔 → 3초 뒤 새 트렁크가 튀어나옴(`ShowTrunk`). 트렁크 없을 땐 방패 판정도 없음.
- **2장 적 패턴(가중 랜덤, 직전 ×0.25)** — `EnemyCh2.cs` 머리 주석에 전부:
  - 차장: 3갈래(기본) · 연속 검표(따라오는 조준선 → 움직임을 앞질러 3연발) · 흩뿌리기(가까울 때 5장 60°) + 4.5m 안이면 뒤로 깡충 → 흩뿌리기. 강화형 검표 펀치 그대로.
  - 짐꾼: 내리찍기(가까움) · 방패 돌진(3.5~7.5m, 예고 0.5초 → 5m 12m/초, 피해 12 + 크게 밀림, 끝나고 0.5초 빈틈). 몸 방향과 걷는 방향이 60° 넘게 어긋나면 거의 제자리에서 돌아섬(미끄러짐 X), 가끔 버티고 섬, 등 뒤를 잡히면 2.5~4초에 한 번 발을 굴러 빠르게 돌아섬.
  - 신호수: 신호등 2개(기본) · 건널목 차단(고양이 앞 가로줄 4개, 반경 1.3) · 따라오는 신호(0.45초 간격 3개) + 3.8m 안이면 발밑에 신호등을 떨구고 도망. 강화형 정지 구역은 기본 공격 · 발밑 신호등에만.
- **추적 다듬기(모든 하수인)**: `Enemy.Separate` — 몸 반지름 합보다 가까우면 서로 밀어냄(뭉쳐 오지 않게, 1장 적에도 적용). 돌진 · 도약 · 대시 중엔 안 밂.
- **밀림이 벽을 뚫던 문제 수정**: `Health.Knockback`이 고양이는 CharacterController로 밀어 방 밖으로 안 나감(1장부터 있던 문제, 짐꾼 돌진으로 자주 생겼음).
- **공중에 멈춘 짐 = `FlyingLuggage`**(사용자: 위에서 떨어지는 건 별로): 맞으면 0.12초 움찔 → 맞은 방향으로 15m/초 날아가다 적 · 벽에 부딪히거나 12m 끝에서 폭발(반경 1.5 · 적 40 · 고양이 15), 근처 폭발물도 연쇄. 기획서 2장 '쏘면 0.6초 뒤 낙하'는 바뀜 → 기획서 고칠 것.
- **몬스터 공격에도 반응**(사용자): `Scripts/Game/PropHit.cs` `PropHit.Area` — 깡패 돌진 · 회전 · 내리찍기, 폭탄병 자폭 · 모래 폭탄, 짐꾼 내리찍기 · 돌진 · 트렁크, 신호등 폭발, 1장 보스 `BossFx.Blast`가 엄폐물 · 폭발물(화약통 · 증기 밸브 · 공중 짐 · 광차)을 맞힘. `Health.HitFrom`으로 맞은 방향을 남겨 공중 짐이 그쪽으로 날아감(고양이 탄 · 적 탄도 방향을 남김). 1장 보스의 다른 패턴(휘두르기 · 태클 등)은 아직 안 넣음.
- **확인**: 짐꾼 대기 · 걷기 · 내리찍기 · 던지기 · 돌진 자세를 멈춰 놓고 화면으로 봄(트렁크가 두 손 사이에 붙어 있음, 발 바닥에 붙음), 트렁크 떼어 내기 · 구운 트렁크 크기(1.3 × 1.4 × 0.6m) 확인. 실제 2장 웨이브(강화형 8마리)에서 공격 종류가 바뀌어 나오고(연속 검표 · 건널목 · 따라오는 신호 · 도망 · 트렁크 던지기 · 펀치 대시 잠김) 콘솔 에러 없음. 공중 짐: 맞은 방향으로 날아가 적에 부딪혀 40 피해, 몬스터 범위 공격으로 발동 확인.
- **확인 안 됨**: 직접 조작으로 판 하나를 처음부터 끝까지, 걷기 발 미끄러짐 정도(걷기 클립 속도는 이동 속도와 안 맞춰 봄), 짐꾼 모션은 멈춘 화면으로만 봄(움직이는 모습은 사용자 확인 필요).

## 2026-11-17 — 경험치 · 레벨 업 3택 (세피리아식, 커밋됨)

- `Scripts/Game/RunManagerXp.cs`(RunManager를 partial로) · `XpOrb.cs`: 적 처치 → 경험치 보석(일반 2 · 강화형 3 · 중간 보스 20, 최종 보스 없음)이 튀어나와 0.35초 뒤 고양이에게 빨려 옴.
  보석 모델 = **블렌더 + Codex 3D**(명세 `Art/tools/specs/kit_xpgem_spec.md` → `Art/tools/blender_kit_xpgem.py` → `Kit_XpGem.fbx` 408삼각형 → 빌더 표 `Kit_XpGem` → `Resources/Kit/Ch1/XpGem`, 1.7배로 띄워 빙글빙글). Codex 그림 `UI/xp_orb`는 모델이 없을 때만.
  하트 · 경험치 보석은 `Kit.Shared`로 장과 상관없이 1장 모델을 씀(예전엔 2장에서 하트가 빨간 공으로 나왔음). 빌더 `EnemyModelBuilder.BuildCh1Only(이름)`로 키트 하나만 다시 빌드. 필요 경험치 12 + 6 × (레벨 - 1), 넘친 만큼 이어짐, 한 판 동안 유지.
- 레벨 업 → 게임 멈추고 `ShowRewardPick` 3택: 체력 회복(최대 30%, 다쳤을수록 잘 나옴) / 골드(스테이지 보상의 50%) / 시간의 조각(60%) / 총탄 / 총(교체 창) / 방어구 강화(`ArmorUp`, 스테이지 보상과 공용) 중 서로 다른 3개, 가중 랜덤. 다른 창이 열려 있으면 닫힌 뒤(`GameUI.ScreenOpen`), 여러 레벨이 한꺼번에 오르면 차례로.
- HUD: 대시 칸 아래 경험치 바(`GameUI.BuildXpBar`, 그림 `UI/xp_bar` + 왼쪽 배지에 레벨 숫자, 채움은 틀 위 홈 안쪽 `XpSlot`). UI 그림은 **Codex CLI**(사용자 지시): 프롬프트 `docs/images/ui_src/prompts/xp_ui.txt`(참고 그림 ref_hud.png) → `xp_bar.png` · `heal_icon.png`(체력 회복 카드) · `xp_orb.png`(마젠타 바탕) → `python docs/tools/ui_xp_cut.py docs/images/ui_src`로 잘라 `Resources/UI/`.
- 하트 드랍 하향(사용자): 일반 2% · 강화형 3%(중간 보스 확정 그대로). 1장 기획서 Ver02 '드롭 2~3%'로 고침.
- 2장 기획서 슬라이드 5(오브젝트): 공중에 멈춘 짐 = '피격 시 피격 방향과 반대로 날아가 폭발' · '범위형 피해 / 반경 1.5m', 그림도 고양이 → 짐 → 적 쪽 폭발로, 공통 줄에 '증기 밸브 / 짐 / 광차는 몬스터 공격에도 발동'. 레벨 디자인 md · 생성기(build_ch2_ppt.js)도 맞춤.
- 경험치 · 레벨 업은 아직 기획서(컨셉 · 공통 레벨 디자인)에 없음 → 수치 확정 후 반영할 것.

## 2026-11-17 밤 — 2장 굳은 승객 석상 → 하수인으로 깨어남 (커밋됨)

- 처음엔 반투명 회청색 실루엣으로 만들었다가 사용자: **"반투명 X, 석상 느낌으로" + "모습 더 다양하게"** → 불투명 돌 석상(받침대 위) 10종으로 바꿈.
- **모델(블렌더 + Codex)**: 명세 `Art/tools/specs/kit_ch2_passengers_spec.md`(첫 4종) + `kit_ch2_passengers_v2_spec.md`(석상 · 받침대 · 6종 추가) → `Art/tools/blender_kit_ch2_passengers.py` → `Art/Characters/Ch2/Kit_Passenger_*.fbx`. 미리보기 `Art/tools/specs/kit_ch2_passengers_preview.png`.
  - 자세 10종: 짐 든 남자 · 모자 잡은 여자 · 뛰다 멈춘 아이 · 회중시계 신사 · 신문 읽는 남자 · 손수건 흔드는 여자 · 보따리 진 노부인 · 아이 끄는 엄마(받침 하나에 둘) · 트렁크에 앉아 하품 · 표 들고 달리는 남자.
  - 한 석상 = 몸 · 소품 · 받침 쐐기 조각 `Piece_XX` 16~18개 + 금 선 `Crack_XX` 10개 + 돌 조각 틀 `Shard_X` 3개, 재질 `statue` · `statue_dark` · `statue_base` · `crack`(배색표 `ModelPalette.Ch2`).
- **빌더**: `Models2` 표에 `Kit_Passenger_*` 10줄(받침 포함 키 1.7~2.6m), 메뉴 `CatTang/2장 굳은 승객만 다시 만들기`. 몸은 보통 툰, `Crack_*`만 빛나는 흰 재질(`Passenger_Crack.mat`, CatTang/Silhouette 불투명) → `Resources/Kit/Ch2/Passenger_*.prefab` + `FrozenPassenger` 컴포넌트.
- **배치** (`Ch2Kit.Props`): 보스 방 뺀 모든 방에 6~8개, 한 방 안에선 자세가 안 겹치게 섞어 차례로, 크기 ±8%, 무작위 방향. 돌이라 **몸통 굵기 BoxCollider(받침 발자국의 60%)** → 고양이 · 고양이 탄 · 적 탄을 막음(부서지지 않는 엄폐물). 적 이동은 물리를 안 써서 석상을 통과함.
- **깨어남** (`Scripts/Game/FrozenPassenger.cs`, `RunManager.SpawnWave`): 웨이브 예고 1초 동안 고양이와 4.5m 넘게 떨어진 석상을 골라 웨이브의 60%까지 → 금이 머리부터 하나씩 톡 튀어나오며 빛남(작은 섬광) → 뒤 40%는 떨림 + 받침 둘레 돌가루 → 깨짐: 조각이 바깥 · 위로 흩날리고(나무 문 `GateDebris` 재사용, 받침 조각은 낮게) 돌 조각 12~16개 + 섬광 · 돌가루 · 살짝 흔들림 → 그 자리에서 하수인. 나머지는 예전처럼 바닥 원.
- **플레이 확인**: 2장 스테이지 2 지어 방 들어감 → 방 5개에 석상 30개, 첫 웨이브 6마리 중 4마리가 석상에서 나옴(30 → 26), 금 선 · 돌가루 · 깨진 조각이 바닥에 흩어지는 화면 확인, 콘솔 에러 없음.
- **확인 안 됨**: 직접 조작으로 석상에 막히는 느낌(엄폐물로 너무 많이 막는지), 석상 색이 바닥과 잘 갈리는지(밝은 회베이지, 툰 배경 톤), 사운드(효과음 시스템 없음).
- 다른 아이디어(색 빠짐 · 고양이 주변만 색, 공중 입자 반응, 유리처럼 깨지는 처치, 클리어 시 잠깐 시간 흐름 등)는 이번엔 안 넣기로 함.

## 2026-11-17 밤 — HUD: 경험치 바 위치 · 머리 위 대시 `<<<` (커밋됨)

- **경험치 바**(사용자: 장전 UI 아래로): 왼쪽 위 대시 칸 아래 → **화면 아래 가운데, 장전 HUD(시계 · 실린더) 밑**(`GameUI.BuildXpBar`, 바닥에서 6px). 장전 HUD는 그만큼 올림(`HudView.lift` = `GameUI.HudLift` = 경험치 바 높이 + 8), 위에 붙은 다음 탄 글 · 열기 글 · 상호작용 안내도 같이 올림. 마을에선 경험치 바 안 만듦.
- **머리 위 남은 대시 `<<<`** (`Scripts/UI/HeadDash.cs`): 그림은 **Codex CLI**(사용자 지시) — 프롬프트 `docs/images/ui_src/prompts/head_dash.txt`(참고 ref_icon.png) → `head_dash.png`(흰 몸 + 짙은 외곽선 '<' 한 칸, 마젠타 바탕) → `python docs/tools/ui_dash_cut.py docs/images/ui_src` → `Resources/UI/head_dash.png`. 3칸(대시 최대 수만큼, 5칸까지)을 겹쳐 `<<<`.
  - 찬 칸 밝은 청록빛 흰색 · 차오르는 칸 어두운 회색에서 진행만큼 밝아짐(검표 펀치 잠김이면 붉게 깜빡) · 빈 칸 흐린 어두운 색, 다 차면 톡 튐. 창이 열려 있거나 고양이가 죽으면 숨김.
  - 처음엔 화면 UI(44px)로 만들었다가 사용자: **"너무 크고 안 어울림, 좀 줄이고 비네트 맞춰"** → 30px로 줄이고 **월드 빌보드 스프라이트**로 바꿈 → 후처리(극한 회피 회색 · 피격 붉은 비네트 · 색수차)를 화면과 똑같이 받음. 벽에 안 가려지게 `CatTang/Sprite`에 `_ZTest` 속성 추가(기본 LEqual 그대로, HeadDash만 Always). 카메라 거리와 상관없이 화면 크기 일정.
- **머리 위 패턴 UI는 대시 줄 위로**: `BigLabel`이 고양이를 따라다니면(1장 중간 보스 현상금 표식) offset 높이 대신 `HeadDash.TryAbove`로 `<<<` 바로 위에 붙음.
- 왼쪽 위 대시 아이콘 3칸은 그대로 둠(사용자 확정: 머리 위 `<<<`와 둘 다 유지).
- **플레이 확인**: 경험치 바가 장전 HUD 밑에 들어간 화면, `<<<`(찬 1 · 차오름 1 · 빈 1, 잠김 깜빡) · 그 위 현상금 배지 화면, 극한 회피 회색 화면에서 `<<<`도 같이 회색이 되는 것 확인, 콘솔 에러 없음.
- **확인 안 됨**: 직접 조작 중 보이는 느낌(적 · 이펙트와 겹칠 때 읽히는지 — 큰 폭발 섬광은 Vivid 레이어라 위를 덮음), 해상도가 다를 때.

## 2026-11-17 밤 — 머리 위 대시 개수 · 실루엣 하늘색 · 실루엣 크기 버그 · 1장 보스 바닥 시계판 · 이펙트 1~14(특성 4~9 남음) · 완벽 장전 삭제 · 표적지 커서 (커밋됨)

- **머리 위 `<<<`는 지금 쓸 수 있는 대시 수만큼만**(사용자): 차오르는 · 빈 칸은 안 그림, 충전 진행은 왼쪽 위 아이콘(그대로 유지, 사용자 확정). 새로 찬 칸은 톡 튀며 나타남, 검표 펀치 잠김이면 붉게 깜빡.
- **실루엣 하늘색**(사용자: 총알 먹은 뒤 뜨는 효과 → 회상 실루엣 크게 · 하늘색, 대시도 하늘색): 그 효과는 `SilhouetteShot`(회상 계열 탄 · 특성 9101/9102/9227/9307 — 쏠 때 확률로 고양이 실루엣이 옆에서 1발 더) · `TrailSilhouetteShot`(9225). 진홍 `SilCrimson` → 회상 `SilRecall`(진한 하늘색, 1.15배) · 대시 `SilDash`(옅은 하늘색 꼬리) · 대시 끝 `SilDashEnd`. `Afterimage.SpawnAt(…, scale)` 크기 배율 추가.
- **실루엣 총이 팔에서 떨어져 보이던 버그**(사용자 지적): `Afterimage`가 스킨 메시를 구울 때 부모 배율(고양이 `Body` 1.8배)을 다시 안 곱해서 몸만 0.55배로 발 쪽으로 쪼그라들고, 제 위치에 놓인 총만 팔에서 떨어져 보였음 → 구운 몸에 `smr.lossyScale`을 곱함. 대시 · 극한 회피 · 궁 분신 잔상 전부 원래 크기로 고쳐짐.
- **1장 보스 '시각 패턴'**(사용자: 기믹이 잘 안 보임, 중간 보스 현상금 표식은 그대로):
  - `Scripts/Game/FloorClock.cs`: 보스 방 바닥에 큰 금빛 시계판(반지름 = 방 짧은 변 / 2 - 1.5, 6~10m)이 퍼지며 나타나고, 큰 시곗바늘이 12시부터 목표 시각까지 째깍째깍 돌아가 멈춤. 그 숫자 자리(반지름 0.72배)가 안전 지대(초록 원 반경 2m). 마지막 1초는 빠르게 깜빡.
  - 그림은 **Codex**: `docs/images/ui_src/prompts/boss_floor_clock.txt`(참고 ref_hud.png) → `boss_floor_clock.png`(로마 숫자 시계판 고리, 가운데 비움) · `boss_floor_hand.png`(큰 바늘) → `python docs/tools/ui_boss_clock_cut.py docs/images/ui_src` → `Resources/FX/`.
  - 사용자: **시간 늘리고, 보스 얼굴 숫자 없애고, 안전 지대가 몇 시에 뜨는지 보고 맞추게** → 제한 시간 5초 → 8초(`Ch1Boss.TrialTime`), 얼굴 숫자 · 안내 글의 시각 없앰(안내는 "안전 지대의 시각에 발사 시각을 맞춰라"만). 중간에 넣었던 HUD 시계 목표 시각 고리도 답을 알려 주는 셈이라 뺌.
- **플레이 확인**: 보스 방에서 시각 패턴 강제 실행 → 바닥 시계판 · 바늘 IV · IV 자리 안전 지대 화면, 6초 지나도 시계판 유지 · 얼굴 숫자 빔, 실루엣(대시 · 회상) 하늘색 · 총이 손에 붙은 화면, 머리 위 `<<`(3칸 중 1칸 씀), 콘솔 에러 없음.
- **확인 안 됨**: 직접 조작으로 시각 패턴 파훼 · 실패 판정(로직은 안 바꿈), 시계판이 다른 장판(웅덩이 · 시간 지연 구역)과 겹칠 때 읽히는지.
- `Game/Assets/_Recovery/`: 작업 중 Unity가 한 번 끊겼다 다시 붙으며 생긴 씬 복구 폴더 — 안 건드림, 필요 없으면 지울 것.
- **이펙트 개편**(사용자): 1번 방식(종류별 모양 + 궤적 + 박자, 3D 툰 유지)으로 하되 **효과 하나하나 확인받고 만들 것**. 14가지 중 4개씩 묻는 중.
  - 1차 확정(1~4): **일반 타격** = 모양 유지 · 1.3배(`GunkataFx.HitScale`, 고양이 탄만) / **치명타** = 일반 타격(`FX_Hit` 프리팹)을 진홍 1.8배 + 45° 비튼 흰빛 1.1배로 겹치고 진홍 · 흰빛 빛살 8개(`GunkataFx.CritImpact`). `FX_HitCrit` 프리팹은 고양이 피격 이펙트로만 남음 / **폭발** = 그대로 / **완벽 장전** = 기능 삭제(아래).
  - **치명타 전용 피해 숫자**: Codex `docs/images/ui_src/prompts/crit_digits.txt`(참고 ref_digit.png) → `crit_digits.png`(5×2칸) → `python docs/tools/ui_crit_digits_cut.py docs/images/ui_src` → `Resources/UI/digits_crit/d0~d9`(위 뜨거운 흰 노랑 · 아래 진홍, 금테, 기울임). `Health.TakeDamage(dmg, fromDelayed, crit)` → `DamageNumber.Spawn(pos, dmg, crit)`: 1.35배 · 더 세게 튐 · 처음 0.35초 떨림 · 0.95초. 그림이 없으면 일반 숫자를 붉게.
  - **피해 숫자 전체 1.5배**(사용자: 잘 안 보임): 0.4 · 0.5 · 0.62 → 0.6 · 0.75 · 0.93.
- **완벽 장전 삭제**(사용자: 사실상 없는 기능): R 키 · `TryPerfectReload` · 1.5배 다음 탄 · 금빛 섬광 · `OnPerfectReload` 효과 호출 · HUD "지금 R!" · 도움말 줄 삭제. `OnReloadEnd`는 인자 없는 `Action`으로(HudView · ReloadMotion 맞춤). 
  - 사용자 결정: 총 2008 '마지막 종소리' 효과 9310 → **장전이 끝날 때** 반경 6 적 0.5초 정지(새 트리거 `OnReloadEnd`, `FinishReload`에서 발동 — 즉시 장전으로 끝나도 발동) · 코스튬 4005 '거울 망토' 해금 → **누적 극한 회피 100회**(`UnlockType.JustDodgeCount`; 해금 조건은 원래 데이터에만 있고 코드는 조각 값만 봄 — 세는 코드는 아직 없음). `Data/Tables` CSV와 원본 `docs/tools/build_tables.py` 둘 다 고침.
  - 기획서 md(총 · 총탄 · 컨셉 · 코스튬 · 목록 초안) 문구 정리, 컨셉 기획서 결정 기록에 한 줄 추가. **PPT 기획서(.pptx)는 아직 안 고침.**
- **이펙트 2차 확정(5~6)**: **총구 섬광** = 그대로 / **극한 회피** = 몸을 감싸던 큰 고리 조각 `GptDodge`를 `FX_DodgePerfect` 프리팹에서 뺌(MCP, 작은 고리 섬광 `GptDodgeEcho` · 불티 `BakedSpark`는 남김) + **리본 궤적** `Scripts/Game/DodgeRibbon.cs`: 대시 시작 자리(`PlayerController.dashFrom`) → 지금 자리를 몸 둘레로 한 바퀴 반 휘감는 하늘색 리본 두 가닥, 0.16초 그려지고 0.28초에 꼬리부터 사라짐, 실제 시간 · Vivid 레이어(회색 화면에서도 제 색), 알파 섞기(밝은 모래 위에서도 하늘색).
- **이펙트 3차 확정(7~10)** — `Scripts/Game/ExtraFx.cs` (예전엔 넷 다 `GunkataFx.Flare` 색만 바꿈):
  - **확률 회피**: 고양이 잔상 두 개가 화면 좌우로 0.85m 빗겨나며 사라짐(`Afterimage.DashIn`) + 옆으로 스치는 바람 가시 3~4개(`GunkataFx.Streak`). 고리 없음.
  - **범위 기절**(`Effects.AreaStun`): 발밑에서 금빛 흙먼지 띠(바닥에 눕힌 고리 선 + 띠를 따라 흙먼지 뭉치)가 0.3초에 범위 끝까지 쓸고 나감 + 닿은 적마다 금빛 번쩍.
  - **범위 둔화**(`Effects.AreaSlow`): 바닥에 푸른 모래 나선 팔 4개가 점점 느려지며 돌다 0.9초에 가라앉음.
  - **레벨 업**(`RunManagerXp.AddXp`): 청록 빛기둥 4.5m + 경험치 보석 모델 6개가 몸 둘레를 돌며 솟음 + 머리 위(대시 줄 위) **'LEVEL UP' 글자**(사용자: 영어로, 글자 구워서) — Codex `prompts/level_up_text.txt` → `Resources/UI/level_up_text.png`. 실제 시간으로 돌고 **3택 창은 0.7초 뒤에 뜸**(`levelFxUntil`, 이펙트가 보이게).
- **조준 커서 '표적지'**(사용자: 표적지 느낌 마우스 포인터) — `Scripts/UI/AimCursor.cs`: Codex `prompts/cursor_target.txt` → `Resources/UI/cursor_target.png`(고리 3개는 선만, 가운데 빨간 점, 12 · 3 · 6 · 9 눈금). 전투 중엔 윈도 커서를 숨기고 84px로 마우스를 따라감, 창 · 마을에선 원래 커서. 쏠 때 툭 커졌다 비틀리며 돌아옴, 장전 중엔 돌며 흐려짐. 자르기 `python docs/tools/ui_cursor_cut.py docs/images/ui_src`.
- **플레이 확인(3차)**: 레벨 업(빛기둥 · 보석 · 글자) · 범위 기절 흙먼지 띠 · 범위 둔화 나선 · 확률 회피 잔상 · 커서 화면, 콘솔 에러 없음. **확인 안 됨**: 실제로 마우스를 움직이며 커서(이 테스트는 마우스가 게임 창 밖), 3택 창 0.7초 지연이 답답하지 않은지.
- **이펙트 4차 확정(11~14)**:
  - **12 적 공격 예고 섬광** = 그대로.
  - **13 총알 비**(불릿 파티 궁): 탄착마다 하늘에서 비스듬히 내리꽂히는 금빛 줄(`RainDropFx`, 0.09초) → 바닥엔 고리 대신 불똥 · 흙먼지 + **작은 탄자국**(Codex `prompts/bullet_hole.txt` → `Resources/FX/bullet_hole.png`, 1.4초 뒤 사라짐 `FadeOut`). 예전 `GunkataFx.Impact` 플립북은 안 씀.
  - **14 관통 · 튕김**: 관통 = 탄도 방향으로 길쭉하게 뚫고 나가는 불꽃 꼬리(`ExtraFx.Pierce`), 튕김 = 꺾인 자리에서 지그재그 불똥 + 흰 번쩍(`ExtraFx.Ricochet`).
  - **11 탄 특성 타격 9종**: 비교표 `docs/images/compare/trait_hits_compare.png`(9개를 3×3으로 터뜨려 세 순간을 찍음)를 보여 주고 하나씩 묻는 중.
    - 확정: **특성마다 고유 색**(`GunkataFx.TraitColor`: 회상 하늘 · 추억 분홍 · 망각 청백 · 되감기 보라 · 종말 진홍 · 지연 호박 · 예지 금 · 영원 청록 · 미련 잿빛 주황, 탄 색은 궤적 선에만) — 전엔 탄 색이라 9종이 다 주황으로 비슷했음.
    - 확정(프리팹을 MCP로 고침): **1 회상** = 첫 타 1.5배 + 메아리가 0.12초 · 0.24초에 점점 작게(`EchoCrown2` 추가, 고리 1.9배) / **2 추억** = 꽃잎 4 → 8개, 0.75~0.95초로 천천히 사방 · 아래로, 자국 1초 / **3 망각** = 얼음 가시가 0.5초 박혀 있다가 0.48초에 조각 6개(`Shatter0~5`)로 깨짐, 고리 2.2 → 1.4.
    - 4~9는 아래 '이어서' 절에서 확정 · 구현함.
  - 고유 색 · 회상 · 추억 · 망각 바뀐 모습은 화면 한 장으로만 봄(색 구분 확인). 세부 움직임은 사용자 확인 필요.

## 2026-11-17 밤 (이어서) — 고리 정리 · 특성 타격 4~9 · 적 몸 반응 · 특성 탄도 · 여러 특성 동시 (커밋됨)

- **고리 정리**(사용자: "너무 다 고리 효과 같다" → 의미 있는 곳만): 프리팹의 `Ring` 조각을 `FX_Hit` · `FX_HitCrit` · 특성 타격 9종에서 전부 뺌(MCP). 코드 `GunkataFx`의 일반 타격 · 치명타 대체 경로 · 총구 얇은 고리 · 실린더 고리 · `Burst` 충격 고리 · 닷지 대체 경로 고리 삭제. `Flare(…, ring)`는 범위 효과(시간 정지 `PlayerController` 414줄 · `StopZone`)만 고리. **남긴 고리**: 폭발 바닥 고리 · 예고 섬광 `FX_WarnGlint`.
- **특성 타격 4~9 확정 · 구현**(사용자 하나씩 확인, Codex 그림 + 블렌더 메시 사용 허락):
  - 4 **되감기**(도타2 페이스리스 보이드 타임 락 참고): 맞은 적의 보라 잔상이 탄도 뒤 1.1m에서 미끄러져 되돌아옴(`Afterimage.DashIn`) + 보라 결정이 박혀 굳었다(`hold`) 0.34초에 깨짐 + 큰 시곗바늘 둘이 거꾸로 휙(블렌더 `fx_hand`).
  - 5 **종말**: 진홍 불꽃에서 해골이 솟아 입 벌리고 타 사라짐(Codex `cx_doom_skull` 8프레임) + 불꽃 혀 4.
  - 6 **지연**(트레이서 펄스 밤처럼 붙었다 터짐, 메이플 블래스터 폭발 참고): 충전 폭탄(블렌더 `fx_charge`)이 적에 붙어(`FxEffect.Follow`) 점점 빨리 깜빡이다(`blink`) 0.45초에 날카로운 호박색 폭발(Codex `cx_delay_burst`) + 바늘 8.
  - 7 **예지**: 금빛 눈 문양이 머리 쪽에서 번쩍 뜸(Codex `cx_foresight_eye`) + 탄도 쪽 금빛 선.
  - 8 **영원**(사용자: 정지 시간만 늘리니 이펙트 따로 없어도 됨): 청록 일반 타격만. **정지된 적 추가 피해는 초록 숫자로 따로**(Codex `digits_eternity`, `DamageNumber.SpawnBonus`, `Health.TakeDamage(…, bonusFrac)`) — 본 숫자 = 기본 몫, 초록 = `DamageVsStoppedAdd`로 늘어난 몫(영원 6단계 2배 · 열한 시 오십구 분 +50% 둘 다).
  - 9 **미련**: 잿빛 주황 불씨 7개가 적에 붙어 0.8초 깜빡이며 타다 재로 흩날림 + 연기.
  - 망각 색을 확실한 파랑(0.42, 0.75, 1)으로(전엔 거의 흰색).
- **적 몸 반응**(사용자: 머리 위 문양만 말고 맞은 적 자체에 피드백): `Health.TraitTint(색, 시간, 세기, 깜빡임, 지연)` · `FlashLater` — 회상 하늘 · 추억 분홍 · 망각 청백 · 되감기 보라 지직 · 종말 진홍 + **몸에 진홍 불길 `FX_Trait_DoomBurn`(따라붙음)** · 지연 호박 깜빡 → 폭발 순간 흰 번쩍 · 예지 금 · 미련 그을림 깜빡.
- **특성 탄도**(사용자: 얼음이면 궤적도 파랗고 얼음 덩어리 날아가듯, **덩어리는 작게 — 리볼버 느낌**): `Tracer`가 특성 탄이면 빨강으로 끌지 않고 특성 색(심지), 번짐은 이 탄 자신의 특성 색. `GunkataFx.TraitBolt`: 작은 특성 모양 덩어리가 0.04~0.1초에 총구 → 탄착(`TraitBoltMove`), 지나가며 부스러기(얼음 = 서리 조각 떨어짐 · 종말 = 불꽃 · 미련 = 연기 · 나머지 불똥). 회상 별 · 추억 꽃 · 망각 얼음 덩어리+조각 둘 · 되감기 시곗바늘 · 종말 불덩이 · 지연 충전 폭탄 · 예지 바늘 · 영원 없음.
- **여러 특성 동시**(사용자 확정): **날아가는 탄 = 플레이어가 모은 특성 개수가 가장 많은 것(세트 2단계 이상, 동률이면 랜덤)**, 없으면 이 탄의 특성. **맞은 자리 = 켜진 특성 전부 + 이 탄의 특성**: 대표는 그대로 + 몸 반응, 나머지는 0.75배로 둘레에 비껴(`GunkataFx.TraitHitAll`, `PlayerController.ShotTraits`).
- 특성 타격은 맞은 자리에서 **카메라 쪽으로 0.7m 당겨** 그림(큰 몬스터 몸에 묻혀 안 보이던 문제).
- `FxPart` 새 값: `hold`(톡 나타나 버티다 끝에 오그라듦) · `holdHeat` · `blink`. `FxEffect.Follow(대상)`.
- 그림 · 메시 만드는 법: Codex 프롬프트 `docs/images/fx_src/prompts/trait_{delay_burst,doom_skull,foresight_eye}.txt`(참고 `ref_style.png`) → `python docs/tools/fx_sheet_cut.py docs/images/fx_src/<시트>.png cx_<이름> 8`. 초록 숫자 `docs/images/ui_src/prompts/eternity_digits.txt` → `python docs/tools/ui_eternity_digits_cut.py docs/images/ui_src`. 블렌더 `Art/tools/blender_fx.py -- fx_hand fx_charge` → 메뉴 `CatTang/FX 메시 가져오기 (블렌더)`.
- `DamageNumber`: 플레이 사이에 정적 캐시 재질이 사라져 마젠타 상자가 뜨던 문제 → 재질이 죽었으면 다시 만듦.
- **플레이 확인**: 적 5마리에 4~9를 한꺼번에 터뜨려 0.1 · 0.3 · 0.5초 화면, 여러 특성(망각 대표 + 종말 · 지연 / 종말 대표 + 미련) 탄도 · 타격 화면, 초록 숫자, 콘솔 에러 없음. 비교 이미지 `docs/images/compare/trait_hits_v2.png`.
- **확인 안 됨**: 실제 실린더로 특성 세트를 켜고 직접 쏘는 것(함수를 직접 불러 확인함 — `ShotTraits`는 직접 안 돌려 봄), 연사할 때 겹쳐서 너무 어지러운지, 동률 랜덤으로 탄도 모양이 쏠 때마다 바뀌는 게 괜찮은지, 회상 · 추억 · 망각(1~3) 몸 반응 세기.

## 브롤 스타즈풍 그림체 — **기본으로 확정** (사용자 2026-11-17: "하데스 느낌 말고 브롤 스타즈, 플랫하면서 가시성 높게". 예전 하데스풍 툰 셰이더 코드는 지우지 말고 냅둘 것)

- 방식: 기존 `CatTang/Toon`(`Art/Shaders/CatTangToon.shader`)은 그대로 두고, **전역 스위치 `_CatTangBrawl`(기본 0 = 지금 모습 그대로)**일 때만 `FragBrawl`로 그림. 재질 · 빌더 안 건드림. 원본 셰이더 백업은 커밋 `38329f9`.
- **기본 켜짐**(2026-11-17 확정): `BrawlStyle`가 플레이 시작 · 끝에 `_CatTangBrawl = 1`, 에디터 화면도 `Editor/BrawlStyleEditor.cs`가 켬. 플레이 중 **F8**은 예전 하데스풍과 비교용 전환. 켜져 있으면 고양이 · 적 렌더러에 `_BrawlActor = 1`(머티리얼 프로퍼티 블록, 0.5초마다 새 적도) → 캐릭터 · 적과 배경을 다르게 칠함. 부서지는 소품(화약통 · 상자)은 배경 취급.
- 1차 값: 그림자 1단(빛 0.29에서 딱 끊김) · 그림자 색 = 같은 색상을 어둡게(캐릭터 명도 0.62 · 채도 +, 배경 명도 0.58, 보라 그림자 없음) · 캐릭터 채도 ×1.3 · 명도 ×1.1 · 텍스처 밉 +0.5 / 배경 채도 ×1.05 · 명도 ×0.93 · 텍스처 밉 +1.5(무늬 뭉갬) · 캐릭터 · 적 외곽선 검정 3.5px(적 바깥 빨간 테두리는 그대로, 그 바깥) · 배경 외곽선은 원래 · 캐릭터만 얇고 단단한 흰 림(0.28) + 작은 하이라이트 점(0.4).
- 비교 이미지 `docs/images/compare/brawl_style_v1.png`(같은 장면 지금 / 시험).
- **2차 (사용자: 더 플랫하게)**: 밝은 면 경계 0.29 → 0.13(그림자는 몸 뒤쪽에만) · 그림자 명도 캐릭터 0.78 · 배경 0.72 · 환경광은 방향 없이 고르게 · 하이라이트 끔 · 림 0.12로 아주 얇게 · 텍스처 밉 캐릭터 +1.5 · 배경 +3(바닥 금 · 무늬 거의 사라짐). 비교 `brawl_style_v2.png`. → **사용자: 1차가 나음 → 1차 값으로 되돌림**.
- **3차 (사용자: 파스텔 톤이라 전체가 뿌옇다)**: 1차에서 캐릭터 밝기 덧셈(+0.05) 빼고 명도 ×1.0 · 채도 ×1.25 + 0.04, 배경 채도 ×1.08 · 명도 ×0.95 · 텍스처 밉 캐릭터 +0.25 · 배경 +0.75(뭉개서 색이 평균으로 섞이며 바래던 것 줄임) · 그림자 명도 0.62 · 마지막에 채도 ×1.06. 대비 늘리기(S 곡선)를 넣었다가 빛 세기 > 1이라 절벽이 새빨갛고 고양이가 새까매져 뺌. 비교 `brawl_style_v3.png`. **지금 셰이더 값 = 3차 = 기본.**
- 다음: 사용자 의견 받아 값 조정 → 확정되면 기본값으로 바꿀지(재질 · 빌더 반영), 고양이 색(어두운 망토라 덜 튐) · 적 배색 2안과 어울리는지, 이펙트 툰 불꽃 셰이더도 맞출지.

## 2026-11-17 밤 — 커서 다시 · 반전 조준선 · 불릿 파티 총알 비 뺌 · 회색 폭탄 (커밋됨)

- **조준 커서 다시**(사용자: 표적지 안 어울림): HUD와 같은 놋쇠 쐐기 4개(12 · 3 · 6 · 9시, 안쪽을 가리킴) + 가운데 진홍 마름모, 굵은 검은 외곽선, 고리 없음. Codex `docs/images/ui_src/prompts/cursor_target.txt`(참고 `ref_hud.png`) → `python docs/tools/ui_cursor_cut.py docs/images/ui_src`. 예전 그림 · 프롬프트는 `docs/images/ui_src/old/`, `prompts/old_cursor_target_v1.txt`. 크기 · 반동 · 장전 회전은 그대로(`AimCursor.Size` 84). 미리보기 `docs/images/compare/cursor_v2.png`.
- **불릿 파티(건카타 궁) 총알 비 뺌**(사용자: 특성도 없는데 비가 내림 → 궁에서 빼고 **바닥 탄자국은 남김**): 하늘 줄 없이 원 안 바닥 탄자국 · 흙먼지 · 불똥만 한 타에 7개(`ExtraFx.GroundHit`, `PlayerController.UltHolesPerTick`). `ExtraFx.RainDrop`(하늘 줄 포함)은 코드에 남겨 둠.
- **커서 = FPS식 반전 조준선**(사용자: 커서가 밑 색과 비슷해 안 보임 → 실제 FPS 조준선처럼 영역별 반전): 새 UI 셰이더 `Art/Shaders/UIInvert.shader`(`CatTang/UIInvert`, 블렌드 `OneMinusDstColor OneMinusSrcColor` = 픽셀마다 밑색 반전, 재질 `Resources/UI/CursorInvert.mat`). 커서 그림을 두 장으로 나눔(`ui_cursor_cut.py`가 만듦): `cursor_target_fill.png`(안쪽 칠만 순수 흰색 → 반전) + `cursor_target_edge.png`(외곽선만 검정 → 그냥 그림, 반전된 안쪽과 배경을 가름). 화면 읽기 없음. 장전 중엔 알파 0.6(반전도 흐려짐). 확인: 주황 바닥 → 옅은 하늘, 보라 적 → 옅은 노랑빛, 어둠 → 흰색. 미리보기 `docs/images/compare/cursor_invert.png`.
  - 거쳐 간 방식: ① 밑 색 평균을 읽어 대비 색으로 통째로 바꾸기(보조 카메라 + `AsyncGPUReadback`) → 사용자가 픽셀 단위 반전을 원해 뺌. 그 전 `ScreenCapture` 방식은 에디터 게임 창이 실제 해상도보다 작게 보이면 엉뚱한 곳을 읽었음.
  - 원래 놋쇠 그림(`cursor_target.png`)은 반전 그림이 없을 때 대비용으로 남음.
  - **2차 (사용자: 채도 · 명도 최대로 선명하게, 기본색은 흰색에 가깝게, 가운데 점은 빨강 유지)**: 반전 층 위에 같은 칠을 `Art/Shaders/UIBoost.shader`(`CatTang/UIBoost`, 블렌드 2배 곱하기, 재질 `Resources/UI/CursorBoost.mat`, `_Gain` 2)로 한 번 더 → 반전 색이 2배 밝아지며 넘친 채널이 잘려 거의 흰색 + 선명한 기운(금빛 HUD 위에선 파랑). 가운데 마름모는 칠에서 빼서 `cursor_target_dot.png`로 반전 없이 늘 빨강. 층 순서: 반전 → 밝히기 → 검은 테 → 빨간 점. 미리보기 `docs/images/compare/cursor_invert_v2.png`. 더 흰색으로 = `_Gain`은 2가 최대라 밝히기 층을 하나 더 겹치면 됨.
- **바닥 회색 공 = 폭탄병 모래 폭탄**(사용자 제보): 1장 일반 적 3종(`EnemyFactory.Outlaw · Brute · Bomber`)이 `e.chapter`를 안 넣어 `Kit.Prefab`이 null → 재질 없는 기본 구를 던졌음. 장 정보 넣음 + 최후 대비 구도 모래색으로.
- **확인 안 됨**: 실제 마우스로 움직이며 커서 크기 · 반동 · 반전이 중간 회색 바닥에서도 보이는지, 궁을 직접 써서 탄자국 모습, 폭탄병이 실제로 모래 폭탄 모델을 던지는지(코드만 고침).

## 2026-11-17 밤 — 2장 중간 보스 · 보스 · 지나가는 열차 · 전용 모션 (커밋됨)

- **`Scripts/Game/Ch2Bosses.cs`**: `Ch2MidBoss`(태엽 역장) · `Ch2Boss`(멈춘 기관차 거인) + 도우미 `ToyTrain`(호루라기 열차 · 지나가는 열차 공용) · `SmokeRing`(연기 링, 빈틈 45°) · `EmberPatch`(석탄 불씨 장판). 수치는 레벨 디자인 md 2장 값 그대로, 챕터 피해 배율 안 곱함.
  - 패턴 고르기 2장 규칙 '거리별 후보 70/30' = `PatternPicker.PickWeighted`(거리 맞는 묶음 70% · 거리 무관 30%). 1장은 예전대로 동일 확률(`BossBrain.PickNext` 가상 함수).
  - 역장 빨간불은 `Forced()`로 패턴 사이에 끼어듦(첫 발동 12초, 이후 25초). 머리 위 신호등 배지(노랑 → 빨강 → 초록), 표 탄 6발 = `EnemyProjectile.shootable` → 고양이 탄 줄에 걸리면 깨짐(`EnemyProjectile.ShootDown`, `PlayerController` 사격에서 호출).
  - 기관차 거인: 객차 4량(`Resources/Kit/Ch2/BossCoach`, 크롬 ChatGPT 원화 `Art/tripo_input/ch2/boss_coach*.png` → 크롬 Tripo H3.1 55크레딧 → `Art/Characters/Ch2/Ch2_BossCoach.fbx`)이 보스 뒤로 지나온 길을 따라옴(경기장 밖으로 나가는 만큼은 둘레를 따라 감음). 객차 회전(1페이즈) · 2페이즈 상시 회전 = 반경 약 11m 한 바퀴 6초. 출발 시각 = `PlayerController.OnFired`로 발사 칸 == 시각 && 같은 프레임 보스 정면(±70°) 명중이면 저지 → 기절 3초, 바늘이 그 시각이면 시계 글자 초록. 실패 = 26m/초 전속 돌진 최대 체력 40%.
  - `Chapter.csv` 2장 보스 체력 4860.
- **대합실**: `RoomBuilder.BuildSaloon`이 2장이면 `WaitingHall`(벽 = 2장 Wall 모듈, 술집 문 없음) + `Ch2Kit.WaitingHall`(벤치 줄 좌우 3줄 · 증기 밸브 2 · 역 시계). `RunManager.HasMidBoss` = 1 · 2장, 안내 문구는 `MidBossName`.
- **지나가는 열차**: `Ch2Kit.PassingTrain`(선로 있는 방 루트에 붙음) — 방 전투 중 6초 뒤 첫 열차, 이후 12초, 예고 1.5초 → `ToyTrain` 28m/초(적 60 · 고양이 20, 광차 · 짐 발동). 열차 몸 = 블렌더 + Codex `Kit_TrainEngine`(9m) · `Kit_TrainCar`(8m) → `Resources/Kit/Ch2/TrainEngine · TrainCar`(명세 `Art/tools/specs/kit_ch2_train_spec.md`, 미리보기 `kit_ch2_train_preview.png`, 새 역할 재질 `train_glow` = 주황), 없으면 상자.
- **전용 모션 `Editor/Ch2Motion.cs`**(메뉴 `CatTang/2장 전용 모션 클립 만들기`, 2장 빌드 때 자동): 고양이 idle 기준 덧셈 근육 커브 → `Art/Characters/Ch2/Anim/Ch2M_*.anim` 8개. 차장 Attack 승차권 튕기기 · 신호수 Attack 신호등 던지기 · 역장 Attack 펀치 / Special 신호봉 / Run 걷기 · 거인 Attack 버팀 / Special 연결봉 / Run 무거운 걸음(`Ch2Motion.WalkFor`).
- **플레이 확인(MCP, 리플렉션으로 방 지음)**: 대합실에 역장 등장 · 차장 호출 · 개찰 펀치 · 노란불 배지 · 초록불 → 콘솔 에러 없음. 보스: 연기 링 빈틈 · 석탄 포격 · 출발 시각 저지 → 기절 · 2페이즈 객차 둘레 회전 · 객차 모델 표시. 열차: 예고 → 통과(느린 시간으로 봄). 거인 Special에 `Ch2M_GiantRods` 재생 확인.
- **확인 안 됨**: 빨간불 움직임 벌칙(도구 호출 사이가 느려 그 3초를 못 잡음, 코드만), 직접 조작으로 역장 · 거인 한 판, 모션이 보기 좋은지(멈춘 화면 한두 장뿐), 열차가 실제 선로 위로 지나가는 모습(모델 자체는 화면으로 확인), 처음부터 2장 끝까지 이어서.

## 2026-11-18 — 보스 패턴 동일 확률 · 2장 보스 경기장 32m · 거대한 시계 바닥 · 연기 링 3겹 이펙트

- **패턴 고르기 = 모든 보스 공통 '거리에 맞는 패턴 + 거리 무관 패턴 동일 확률, 직전 제외'**(사용자 확정). 70/30(`PickWeighted` · `PickNext`) 지움. md · 2장 기획서 Ver03 · `build_level_ppts.js` 문구도.
- **2장 보스 경기장 지름 24 → 32m**(사용자: 패턴 설계상 좁음): `Ch2Kit.BossArenaDiameter` · 방 `BossRoomSize` 40×38, 원형 경계 `Ch2Kit.ArenaFence`(보이지 않는 벽 48조각, 모서리로 빠져 객차 · 증기를 피하던 구멍을 막음), 거인 돌진도 원 안으로, 고양이 시작 자리도 원 안(`RunManager.BuildStage`). 객차 둘레 반경 약 14.8m.
- **바닥 `Kit_Arena`**(블렌더 + Codex, 명세 `Art/tools/specs/kit_ch2_arena_spec.md`, 스크립트 `blender_kit_ch2_arena.py`, 미리보기 `kit_ch2_arena_preview.png`): 1차 방사형 판자 · 2차 돌 구덩이 + 판자 다리 모두 사용자 "타일과 안 맞음" → 3차 = **거대한 시계**: 원판 없이 방 타일이 그대로 문자판, 그 위에 갈색 테두리(나무 · 녹 빛 블록 번갈아, 사용자 요청) + 시각 막대 눈금(12 · 3 · 6 · 9는 놋쇠, 둥근 슬롯은 사용자 요청으로 뺌) · 놋쇠 띠 · +자 선로(12-6 · 3-9 줄) · 바닥에 누운 브레게식 바늘 둘(금 테두리 검은 날 · 속 빈 놋쇠 달 · 창끝 · 마름모 상감, 사용자: 더 고급스럽게) · 겹친 놋쇠 축. 바늘 · 테두리 손질은 Codex 없이 스크립트 직접 수정(블렌더 `--factory-startup -P`로 다시 뽑음, 바늘이 선로와 겹치면 스크립트가 assert). 빌더 표 `Kit_Arena`(FlatFloor), 밑면 = 바닥(y 0).
- **연기 링 3겹**(사용자: 거리로 컨트롤하는 감각 · 너무 잦고 빠름): 5초마다 0.6초 간격 3개, 4.5m/초, 빈틈 45°가 겹마다 같은 쪽으로 40~70° 밀림. 겉모습 = 이펙트 프리팹 `Resources/FX/Prefabs/FX_SmokePuff`(MCP로 만듦, 블렌더 구운 `bk_smoke` 플립북, 수명 3.6초) 30개가 링 따라 퍼짐 + 옅은 앞 선.
- **확인**: 원형 경계(밀어도 반경 15.5에서 멈춤) · 시작 자리 반경 13 · 연기 3겹 화면(느린 시간), 콘솔 에러 없음. **안 됨**: 직접 조작으로 3겹 통과 손맛 · 난이도.

## 2026-11-18 — 레벨 업 연산 · 고유명사 설명 상자

- **연산 보상**: 레벨 업 3택 후보에 연산(가중치 2.0, `RunManagerXp.LvReward.Op`). 스테이지 문 이름 '총탄 강화' → '연산'(`RoomDoor.NameOf`).
- **고유명사** (`Scripts/UI/Terms.cs`): 낱말 표 = 특성 표 9종 + `Terms.Fixed`의 '판정 시각'만 (사용자: 특성 설명 + 연산은 판정 시각 정도, 보면 알거나 장르에 익숙한 개념은 설명 안 함). 모든 `UIKit.Label` 글에 `TermText`가 붙어 고유명사를 파랑(#2E9BE6)으로 칠함.
  - 설명은 낱말 호버 링크가 아니라(사용자: 호버링 UI라 불편) **세피리아 아이템 툴팁처럼 `CursorTip` 옆에 작은 설명 상자를 최대 4개 쌓음**(`CursorTip.FillSide`, 오른쪽에 자리가 없으면 왼쪽). 처음 만든 낱말 호버 방식(TermHover)은 지움. 특성 상자는 롤체 시너지식(`Terms.TraitBox`, 사용자 요청): 제목 = 이름 + 2 / 4 / 6(닿은 단계 금빛) + 깨어난 수, 본문 = 한 줄 설명 + 단계별 효과(켜짐 금빛 · 꺼짐 회색) + 다음 단계까지 남은 수. 데이터는 `TraitTier.csv` · `Revolver.TraitCounts`(TraitHud 툴팁과 같은 계산).
- **레벨 업 가중치**(사용자: 방어구 줄이고 총탄 · 연산 · 총 올림): 총탄 2.8 · 연산 2.6 · 총 1.8 · 골드 1.4 · 시간의 조각 1.2 · 방어구 0.9 · 체력 회복 0.2 + 4 × 잃은 비율(최대 2.5). 칸 비율(잃은 체력 0%): 총탄 23 · 연산 22 · 총 17 · 골드 14 · 조각 12 · 방어구 10 · 회복 2%.
- **용어 바꿈**(사용자): 탄의 '깨어남 / 깨어난' → '활성화 / 활성화된', '잠듦 / 잠든' → '비활성화 / 비활성화된'. 게임 문구(C#) · 데이터 생성기 `build_tables.py` → CSV · xlsx 다시 뽑음 · md 기획서 · `총탄` · `코스튬` 기획서 Ver03(20261118). 2장 굳은 승객이 '하수인으로 깨어남'은 뜻이 달라 그대로.
- **확인**: 레벨 업 3택에 연산 카드, 총탄 카드 툴팁 옆 설명 상자 4개(연산 슬롯 · 특성 · 지연 · 영원) 화면, 콘솔 에러 없음.

## 2026-11-18 (오후) — 상인 고침 · 추억 표식 · 3장 구현 시작

### 상인 (모든 챕터)
- **등장 규칙**(`RunManager.TryMerchant` · `SpawnMerchant` · `MerchantSpot`): 방을 비울 때마다 15%, 챕터에서 못 만났으면 **스테이지 2 마지막 방에 확정**(챕터 바뀌면 다시). 예전 '스테이지 보상 뒤에만 · 스테이지 3 끝 보장'은 지움. 자리 = 벽 3.5m 안쪽 후보 중 화면 안(아래 30% HUD 피함) · 고양이와 7m 쯤 · 막힘 · 소품 피함.
- **화면 밖 화살표** `Scripts/UI/EdgeMarker.cs` (Codex 그림 `Resources/UI/merchant_arrow.png`, 프롬프트 `docs/images/ui_src/prompts/merchant_arrow.txt`, 자르기 `docs/tools/ui_merchant_cut.py`) — 메달 안에 코인, 상인 쪽으로 돌아감.
- **상인 모습**(사용자: 사람 말고 시간에 잠식된 몽환적 존재): ChatGPT 원화 → Tripo H3.1 + 리깅 → `Art/Characters/Merchant/Ch{1,2}_Merchant.fbx` → 메뉴 `CatTang/상인 프리팹 만들기 (Tripo)` → `Resources/Merchants/Ch{1,2}_Merchant.prefab` (고양이 idle 클립, 적 테두리 · 배색 안 씀). 1장 = 녹아내리는 회중시계 머리 바텐더(카운터 뒤), 2장 = 얼어붙은 불꽃 랜턴 머리 매표원(지붕 있는 창구라 **옆**에 섬). 키 2.9 · 3.0m (고양이 2.27m, 카운터 1.3m 너머로 보이게 측정 후). 3장은 프리팹이 없으면 고양이 모델을 좌우 반전(거울 속 고양이). 매표원 아바타는 자동 매핑이 손가락 뼈를 겹쳐 깨져서 1장 바텐더 매핑을 빌림(빌더에 들어 있음). 원화 `Art/tripo_input/merchant/`.
- 카운터 · 창구는 늘 남쪽(카메라)을 보게 바꿈.

### 추억
- 머리 위 표식(`MemoryBadge`) 1.8배. **6단계 = 표식당 +7%**(새 효과 9114, `build_tables.py` → CSV · xlsx, 총탄 기획서 md). PPT 기획서엔 아직 반영 안 함.

### 2장 짐꾼
- 정면 탄 완전 차단 → **피해 33%**(`PlayerController.ShieldDamageFrac`). 2장 기획서 PPT는 아직 '막음'.

### 3장 (신기루 사막 = 거울 세계) — 구현 시작
- **데이터**: `Chapter.csv` 3행 = 신기루 사막 · 거울 고래 7020 (손으로 고침, 생성기 없음).
- **떠다니는 거울 조각** `Scripts/Game/MirrorShard.cs`: 보스 · 중간 보스 방 빼고 방마다 3개, 폭 2.5m · 0.8m/초 표류 · 초당 10° 회전, 콜라이더 없이 선분 교차로 판정. 고양이 탄(즉발)은 `PlayerController` 탄도를 구간으로 나눠 한 번 꺾임 + 거울 15° 회전, 궤적도 꺾여 그림. 적 탄은 `EnemyProjectile`에서 한 번 튕김. 겉모습은 `Kit/Ch3/MirrorShard` 프리팹이 있으면 그걸, 없으면 임시 판.
- **일반 적 5종 + 강화형** `Scripts/Game/EnemyCh3.cs` (md 수치 그대로): 거울 사수(반사탄 벽 · 거울 1번 튕김, + 3갈래) · 빛가루 법사(구슬 5개 → 유도, + 죽을 때 8방향) · 거울 너머 검객(2초 잠복 무적 → 고양이 4m 안 거울 0.7초 빛남 → 튀어나와 베기, + 2연타) · 유리 기사(정면 120° 되튕김 `Health.ReflectFrom`, 4발에 깨짐 5초, + 조각 폭발) · 신기루 분신술사(8초마다 분신 2 · 자리 섞기, 분신 1발 소멸 · 보상 없음 `Enemy.noReward`, 본체 죽으면 분신 소멸, + 분신 3 · 장판). 방마다 3~4종 섞음(`EnemyFactory.RandomCh3`). `EnemyProjectile`에 `bounces` · `homing` · `Retarget` 추가.
- **모델** (사수 · 법사 · 검객 · 기사 · 분신술사, 모두 들어옴 — Tripo 잔액 25): ChatGPT 원화 5장(`Art/tripo_input/ch3/`) → Tripo(`Art/models/ch3/`) → `Art/Characters/Ch3/` → 메뉴 `CatTang/3장 적 프리팹 만들기 (Tripo)` → `Resources/Enemies/Ch3_*`. 공격 클립은 1장 · 고양이 것 임시. 강화형 전용 모델 없음(크레딧 부족 — 기본 모델 키움 · 색 변형).
- **이펙트** (사용자: 그림 말고 블렌더 3D로): 새 블렌더 메시 `fx_glass_0~2`(얇은 유리 파편) · `fx_diamond`(신기루 마름모) · `fx_glint`(긴 반사 빛살) — `Art/tools/blender_fx.py` → 메뉴 `CatTang/FX 메시 가져오기 (블렌더)`. 이 메시 + 기존 `fx_ring` · `fx_star` · `fx_sparkle`로 MCP에서 프리팹 `FX_Ch3_MirrorFlash`(빛살 2 · 고리 · 반짝임 · 유리 부스러기 5) · `FX_Ch3_GlassBurst`(별 심지 · 고리 · 빛살 · 유리 파편 12 · 마름모 4가 돌며 튐) · `FX_Ch3_Stardust`(반짝임 12가 소용돌이치며 오름 · 마름모 5). 툰 불꽃 재질에 색: 유리 = 옅은 하늘빛, 빛가루 = 분홍 라벤더 (`Enemy.Fx3`). 쓰는 곳: 거울 반사 · 기사 되튕김 = 반사 섬광, 기사 거울 깨짐 · 검객 튀어나옴 · 법사+ 구슬 폭발 = 유리 파열, 검객 잠복 · 분신 생김/터짐 · 구슬 생김 = 빛가루. (처음 만든 플립북 그림 버전은 지움)
- **확인함**: 컴파일 · 반사 각도 계산 · 기사 4발 반사 후 깨짐 · 분신 3개 생김 / 본체 죽으면 분신 소멸 + 장판 · 검객 잠복(무적 · 숨김) · 상인 1 · 2장 화면 · 화면 밖 화살표 · 이펙트 3종 화면(느린 시간).
- **안 됨 / 다음**: 3장 키트(바닥 · 엄폐물 · 기억 수정 · 분수 샘 · 거울 조각 겉모습 · 상인 선 거울) 블렌더 + Codex, 3장 전용 공격 모션, 강화형 모델(크레딧 충전 필요), 3장 바닥이 임시 단색이라 옅은 이펙트가 덜 보임, 마술사 · 거울 고래, 3장 중간 보스 방 연결(`HasMidBoss`는 아직 2장까지), 직접 조작으로 반사 손맛. Unity가 한 번 D3D12 GPU 오류로 꺼졌음(코드와 무관).

## 2026-11-18 (저녁) — 3장 맵 키트 (블렌더 + Codex) · 방 조립 · 기억 수정 · 분수 샘

- **명세**: 공통 `Art/tools/specs/_ch3_common.md`(재질 이름 23개 · 좌표 · 내보내기 · 미리보기 PNG) + `kit_ch3_{walls,props,landmarks}_spec.md` → Codex → `Art/tools/blender_kit_ch3_*.py` → `Art/Characters/Ch3/Kit_*.fbx`. 미리보기 `Art/tools/specs/blender_kit_ch3_*_preview.png`. 컨셉 시안 `docs/images/ch3/ch3_room.jpg` 기준.
  - 벽면 7: 사암 기슭(Cliff, 스테이지 1) · 무너진 열주 · 아치 벽(안에 거울) · 거울 액자 벽 · 기본 벽 · 떠도는 섬 가장자리 · 구름 둑 + 거꾸로 박힌 집(스테이지 4).
  - 소품 10: 기억 수정(폭발물) · 분수 샘 · 선 거울(엄폐물, 검객 출몰용) · 상인 선 거울 · 모래 바위 · 무너진 기둥 · 떠다니는 거울 조각 · 바닥 조각 3(조약돌 · 수정 조각 · 물결).
  - 지형지물 7: 아치 폐허 · 큰 수정 · 깨진 모래시계 · 떠 있는 바위섬 · 거울 문 · 시계 오벨리스크 · 모래 언덕.
- **배색** `ModelPalette.Ch3`: 기획 색(청록 · 연보라 · 분홍 노을)과 '맵 = 따뜻하게' 규칙이 부딪혀서 → 돌 · 대리석 · 금은 장미 사암 · 크림 · 금, 청록은 물에만, 흰 빛은 수정 · 거울에만(보라 X). 거울면 · 수정 속 · 빛가루는 빌더 `GlowMats`가 빛나는 단색(CatTang/Silhouette)으로 바꿔 끼움.
- **바닥** `Art/tools/ground_texture_ch3.py` → `Art/Env/Ch3/ground_ch3.png`(노을 비친 거울 호수 판석, 2m 판 · 금빛 줄눈) · `ground_ch3_cloud.png`(스테이지 4 구름) → 빌더가 `Resources/Kit/Ch3/Ground · GroundCloud.mat`(2장 Ground.mat 복사).
- **빌더**: 메뉴 `CatTang/3장 키트 만들기 (블렌더)` (`EnemyModelBuilder.Kit3` 표 → `Resources/Kit/Ch3/*`). 기억 수정은 `Explosives`(주황 테두리).
- **`Scripts/Game/Ch3Kit.cs`**: `Ch3Kit.Is(ch)`(3장 + 키트 있음). 장소별 벽면(`Facades`: 스테이지 2 = 1장 건물 정면을 `@1/` 표시로 빌려 **좌우 반전** + 거울 액자 벽, 3 = 섬 가장자리 · 열주, 4 = 구름 둑, 중간 보스 · 보스 = 열주 · 아치 · 거울 액자) · 지형지물(`Landmarks`) · 바닥 재질/조각(`GroundName` · `GroundWeights`) · 소품(`Props`: 기억 수정 2~4 · 분수 샘 1~2 · 엄폐물 3~5 내구 40). `Kit.Prefab`이 `@N/이름`이면 N장 키트를 씀.
  - **기억 수정 `MemoryCrystal`**: 맞으면(몬스터 공격 포함) 0.18초 떨림 → 반경 3m 빛가루 폭발 적 50 · 고양이 18, `PropHit.Area`로 연쇄. 이펙트 = FX_Ch3 유리 파열 · 빛가루 + 분홍 폭발.
  - **분수 샘 `FountainWall`**: 부서지지 않음(체력 100만, 맞을 때마다 다시 채움). 맞으면 반경 1.5m 물기둥(청록 원통 + 물보라, Health 없는 캡슐 콜라이더 → 양쪽 탄이 벽처럼 멈춤) 3초, 다시 쓰려면 8초. 솟을 때 안에 있던 고양이는 밖으로 밈.
  - 상인: 3장은 `MerchantMirror`(선 거울)이 솟고 거울 속 고양이가 그 앞 발판에 섬.
  - 떠다니는 거울 조각은 키트 모델로 바뀜(`MirrorShard.Make`가 아랫변 0.6m로 띄움).
- **사용자 피드백 반영 (같은 날)**
  - "거울보단 모래사장 느낌" → **실시간 거울 반사**: `Scripts/Game/MirrorReflection.cs`(메인 카메라에 붙는 반사 카메라, 바닥 면 기준 뒤집은 시점 · 비스듬한 자르기 면 · 반 해상도 · 후처리/그림자 끔 → 전역 `_CatTangMirrorTex`) + 새 셰이더 `Shaders/CatTangMirrorFloor.shader`(바닥 그림 위에 반사를 화면 좌표로 섞음, 잔물결 일렁임, 세기 0.85). `RoomBuilder.BuildStage`가 3장 거울 바닥 스테이지(구름 바닥 제외)에서만 켬. 반사 카메라가 그릴 때는 툰 셰이더 외곽선 · 바깥 테두리 패스를 건너뜀(`_CatTangMirrorPass`, 안 그러면 검은 덩어리) + `GL.invertCulling`. 바닥 그림도 하늘 반사(하늘빛 · 연보라 · 분홍 노을 + 흰 구름, 흰 유리 이음매)로, 호수 방에서 모래 언덕 빼고 물결 · 수정 조각 늘림. **주의**: 3장 바닥에 보라 · 하늘빛이 들어가 '맵 = 따뜻하게' 규칙에서 벗어남(사용자 요청 우선, 적은 빨간 바깥 테두리로 구분).
  - 기슭 벽이 각진 판석 = '갈색 상자' → 둥근 바위 더미로 다시(`kit_ch3_shore_v2_spec.md`). 남쪽 기슭은 0.5배 높이.
  - "단색 객체가 많다, 화려하게 / 하이퍼 3D로" → Hyper3D(Rodin)는 이 환경에 연결 안 됨(생성기는 Tripo뿐, 잔액 25), 그래서 **Codex 2차 '화려하게'**: `_ch3_detail_v2.md`(조각마다 재질 5~8 · 한 재질 55% 넘지 않게 · 몰딩 · 세로 홈 · 부조 띠 · 모자이크 상감 · 보석 박기 · 금 덩굴) + `kit_ch3_{props,landmarks,walls}_v2_spec.md`. 새 재질 `glaze_teal · coral · ivory · bronze · rose_gold · gem_rose · gem_amber · crystal_rose · tile_cream` (`ModelPalette.Ch3`).
  - 게임 안에서 사암이 갈색으로 칙칙 → 사암 · 바위 배색을 밝은 분홍 쪽으로(그늘 면 0.62배가 돼도 분홍이 남게).
  - 분수 물기둥: 단색 원통 → 속 기둥 · 겉 껍질 · 위로 흐르는 물보라 띠 4 · 꼭대기 물방울 왕관. 기억 수정 폭발은 회색 그을음 빼고 분홍 · 금빛 반짝임.
- **확인함(플레이 화면)**: 스테이지 1~4 방 생성(기슭 · 반전된 마을 · 섬 가장자리 밑면 · 구름 둑), 거울 반사(벽 · 선 거울 · 거울판 · 아치 · 고양이 · 마을 건물), 분수 물기둥 솟음, 기억 수정 폭발 + 연쇄 피해 50, 콘솔 에러 없음.
- **확인 안 됨**: 반사 카메라 성능(화면을 두 번 그림, 프레임 시간 안 잼), 물기둥 2차 겉모습 · 수정 2차 폭발 이펙트 화면, 물기둥이 실제 탄을 막는 손맛, 상인 선 거울 + 거울 속 고양이 화면, 보스 방(지금은 오벨리스크 · 분수만, 거울 제단은 고래 구현 때), 스테이지 2 반전된 1장 건물의 외곽선 · 그림자가 이상한지 자세히.

## 2026-11-18 (밤) — 상인 거울 유령 · Health 단색 버그 · 3장 두 보스 (진행 중, 커밋 안 함)

- **Health 단색 버그 수정**(`Health.cs`): 매 프레임 렌더러 단위로 `_BaseColor`를 첫 재질 색으로 덮어 다재질 물체(분수 · 수정 · 엄폐물, 1 · 2장 엄폐물 포함)가 늘 단색이었음 → 재질 여러 개면 효과 있을 때만 재질별 블록, 끝나면 지움. 분수 색 정상 확인.
- **상인**(사용자: 가면 + 반투명 + 오라): `Scripts/Game/MirrorGhost.cs` + 셰이더 `Shaders/CatTangMirrorGhost.shader`(원래 색 + 거울빛 + 금 림 + 흐르는 반짝임 띠, 반투명), 가면 `Kit/Ch3/MerchantMask`를 머리 뼈에, 발밑 빛 고리 2 · 도는 반짝임 3 · 빛가루. `RoomBuilder.MerchantFigure`가 3장 거울 속 고양이에 적용. 화면 확인함.
- **사격 봉인 · 삼켜진 칸**: `PlayerController.fireLockUntil/FireLocked`, `Revolver.lost[13]`(쏘면 헛방).
- **보스 모델(블렌더 + Codex, 리깅 없음, 조각을 코드로 움직임)**: `Art/tools/blender_kit_ch3_bosses.py`(명세 `kit_ch3_bosses_spec.md`) → `Kit/Ch3/Ch3_Magician`(Body · Head · ArmL · ArmR, 4m) · `Ch3_Whale`(Body · Tail · FinL · FinR · Jaw, 14m). 보스 소품 `blender_kit_ch3_bossprops.py` → MerchantMask · SwordBox · MagicSword · MirrorPanel · Dove · NumberCard · MemoryShard · Facade_Curtain · Facade_MirrorHall · Altar(제단 테두리 지름 40m). 빌더 `Kit3` 표 · `KeepOrigin` · fit 'd'(길이) · 보스 빨간 테두리 · `boss_*` 배색.
- **`Scripts/Game/Ch3Bosses.cs`**: `Ch3MidBoss`(카드 부채 · 칼 꽂기 상자 · 비둘기 · 거울 상자 · 분신술사 호출 · 빛가루 순간이동 · 진짜 찾기 · 거울 시계) / `Ch3Boss`(반사 빛줄기 · 물결 링 · 기억 비 · 2페이즈 바닥 깨짐 + 비행 · 패턴 8 · 기믹 4종 쿨 20초) + `PuppetAnim` · `WhaleAnim` · `RippleRing` · `DoveFollow`. `EnemyFactory.MidBoss3/Boss3/AttachKit3`. `RunManager.HasMidBoss` 3장까지. 거울 극장 = `RoomBuilder.BuildSaloon` hall3(뒷벽 커튼 · 옆 거울 벽 · 장애물 없음), 보스 방 48×46 + Altar + 원형 경계 + 고정 거울 조각 8(`MirrorShard.SpawnAnchored`).
- **확인함**: 컴파일, 극장 생성 · 마술사 등장 · 거울 상자 패턴 화면, 진짜 찾기 분신 4 생성.
- **극장 36×32 확인함**: 남쪽에서 들어오면 중심이 북쪽으로 4m 옮겨져 남쪽 벽이 복도 끝과 맞음, 뒷문 복도 6m · 스테이지 3도 옮긴 중심 기준으로 붙어 겹침 없음.
- **진짜 찾기 = 거울 속에 비친 시계**(사용자: 숫자 카드 X → 바닥 시계 자리, 그다음 "거울 속에 시계가 비친 느낌으로", 숫자 배치 **좌우 반전** 선택): `ReflectedClock`(Ch3Bosses.cs) + 셰이더 `Shaders/CatTangMirrorClock.shader` — 1장 로마 숫자 고리 그림(`FX/boss_floor_clock`)의 밝기만 써서 거울빛(옅은 하늘 · 흰빛 + 도는 분홍 노을)으로 칠하고, UV 물결 일렁임 · 비스듬한 반짝임 띠 · 가운데에서 번지며 떠오름(0.5초) · 가라앉아 사라짐. 판 x 크기 음수 = 좌우 반전(3시 = 서쪽, 9시 = 동쪽, 숫자도 거울 글자). 반경 7m, 방 가운데. 마술사 + 분신 4가 시각 5곳(진짜 = 발동 순간 발사 시각)의 숫자 바로 바깥(반경 8.3m)에 섬 → 숫자를 가리지 않아 시계 전체가 화면에 없어도 읽힘. 카드 · 숫자 배지 지움, 카운트다운은 고양이 머리 위 그대로.
  - 분신 · 고래 기믹 표적(`Ch3Fx.Target`)에 `PlayerOnlyTarget` → 적 탄(소환된 분신술사 탄 등)은 통과(`EnemyProjectile`). 전엔 적 탄에 분신이 터져 반격 탄이 나갔음.
  - `BossBrain.Own(go)`: 패턴 중 만든 물체를 보스가 사라질 때 같이 지움(시계 · 분신).
  - **확인함(플레이, 리플렉션)**: 발사 시각 3시 → 마술사가 서쪽, 9시 → 동쪽 · 분신 4 생성, 가짜 맞힘 → 그 분신만 터짐(3 남음), 진짜 맞힘 → 분신 소멸 · 기절 · 시계 가라앉음, 콘솔 에러 없음.
- **로마 숫자 → 아라비아 숫자 (사용자 2026-11-18: 모든 보스)**: 거울 글자로 뒤집힌 로마 숫자가 다른 숫자로 읽히던 문제도 같이 해결.
  - 바닥 시계판 그림 `FX/boss_floor_clock.png`(1장 시각 패턴 · 3장 거울 시계 공용): Codex로 같은 고리에 숫자만 1~12로 다시 그림(프롬프트 `docs/images/ui_src/prompts/boss_floor_clock_arabic.txt`, 예전 그림 `boss_floor_clock_roman.png`) → `ui_boss_clock_cut.py`. 숫자 줄 비율 다시 잼 0.727 → `FloorClock.NumeralRing` 0.72 그대로.
  - 1장 보스 12방향 숫자 탄(`EnemyProjectile` Numeral) 글자 1~12.
  - 3장 블렌더 모델: `digit_geometry`(Arial Black 글꼴 → 메시, 붙여서 닫힌 메시) — 고래 등 시계 · 제단 테두리 판 12 · 마술사 카드 숫자. 두 스크립트 다시 뽑고 메뉴 `CatTang/3장 키트 만들기 (블렌더)`로 프리팹 갱신. 명세 md도 Arabic으로.
  - 2장은 원래 숫자 없음(경기장 = 막대 눈금, 헤드라이트 시계 = '3시' 글자). 1장 녹은 시계 거인 Tripo 텍스처의 작은 시계 무늬(로마 숫자)는 화면에서 안 읽힐 만큼 작아 그대로.
  - **확인함(플레이 화면)**: 1장 바닥 시계판 1~12, 3장 거울 시계 1~12 거울 글자 · 발사 시각 2시 → 마술사가 반전된 2시 자리(북서), 콘솔 에러 없음. 고래 · 제단 숫자는 블렌더 미리보기로만 봄.
- **3장 일반 몬스터 탄 · 불똥 = 블렌더로 구운 3D**(사용자: 2D 말고 블렌더로 구워서, 고른 것 = 불똥 · 스파크 + 적 탄, 3장만): `Art/tools/blender_bake_fx.py`에 세트 `c3orb`(깎은 장미빛 보석 + 기울어진 빛 고리 + 반짝임, 반복 16장 · 72° 대칭으로 이어짐) · `c3shard`(+X로 뾰족한 유리 조각이 구름, 반복) · `c3spark`(4갈래 섬광 → 유리 조각 10이 돌며 튐 + 고리, 한 번) → `Resources/FX/anim/bk_c3*`. `EnemyProjectile`: 3장이면 구슬(Orb) · 산탄(Pellet)이 구운 프레임을 초당 20장 반복(구슬은 고리가 늘 같은 쪽으로 서 있게), 탄 맞음 · 벽 튕김 불똥 = `bk_c3spark`(`Spark()`), 다른 장은 예전 그대로. 3장 보스의 Orb 탄도 같이 바뀜. 화면 확인함(느린 시간).
  - 그대로인 2D: 기사 돌진 흙먼지(Dust, 이번에 안 고름), 기억 수정 폭발의 FxBurst 3개(오브젝트라 범위 밖).
- **분수 샘 = 한 번 터지는 물 폭발**(사용자: 물기둥 지속 말고 물이 한 번 폭발하듯): 물기둥 벽(3초 · 탄 막음) 지움. 맞으면 반경 3m 적 피해 25 + 밀쳐냄 9, 적 탄 씻김(`EnemyProjectile.WashAway`), 고양이는 피해 없이 밀려남 6, 쿨 6초. 겉모습 = **진짜 3D 물 `WaterBurst`**(사용자: 구운 3D · Codex 그림 둘 다 물 같지 않음 → 블렌더 3D로 확실히 물처럼): 블렌더 회전체 메시 `fx_water_column` · `fx_water_crown`(버섯 왕관) · `fx_water_drop`(꼬리 물방울) · `fx_water_splash`(밑동 물보라 왕관) — `Art/tools/blender_fx.py` → 메뉴 `CatTang/FX 메시 가져오기` → `Resources/FX/Meshes`. 새 셰이더 `Shaders/CatTangWater.shader`(정면 밝은 물 · 비스듬한 쪽 짙은 물 · 가는 흰 테 · 위로 흐르는 흰 물결 줄 · 반짝 하이라이트 · 꼭대기 거품 · 화면 픽셀 두께 청록 외곽선, `_Fade`). 움직임 약 1.2초: 물기둥 0.18초에 3.6m 솟음 → 출렁 → 무너짐 / 왕관이 피어 퍼지며 내려앉음 / 물방울 18개 포물선(중력, 꼬리는 날아온 쪽) → 바닥에 닿으면 작은 물보라 / 밑동 물보라 퍼짐. 그림 버전 `fx_c3water` · 구운 `bk_c3water`는 메시가 없을 때 대신 쓰는 용도로 남김 + 바닥 물결 고리 2(`RippleRing`, 피해 0이면 판정 없이 겉모습만). 레벨 디자인 md 분수 샘 줄도 고침. **수치(25 · 3m · 6초 · 밀쳐냄)는 Claude가 정함 — 사용자 확인 필요.** 화면 · 피해 100→75 · 탄 5→0 확인함.
- **종말 맞음 = 불길 없음**(사용자: 맞을 때 불타는 효과 빼고 처형 때만): `GunkataFx.TraitHit` · `TraitHitAll`에서 종말은 진홍 일반 타격(`FX_Hit`, 종말 색)만 + 몸 진홍빛 잠깐(지직거림 없음). 진홍 불길 `FX_Trait_Doom` · 해골 · 몸 불길 `FX_Trait_DoomBurn`은 처형(`DoomExecute`)에서만. `HideSkull` 지움. 사용자 플레이 중이라 Unity 컴파일 · 화면 확인은 안 함(스크립트 검사만 오류 0).
  - 사용자가 물은 연기 정체(종말 5 · 망각 2 빌드): 빨간 연기 = 종말 타격 프리팹의 구운 불 · 연기 플립북(진홍으로 물듦) + 몸 불길의 연기, 파란 연기 = 망각 타격 프리팹의 구운 불똥(`bk_spark`, 얼음 파랑으로 물듦). 일반 타격 `FX_Hit`에도 연기 조각(`BakedSmoke`)이 있어 탄 · 특성 색으로 물듦.
- **치명타 = 해골 · 불꽃**(사용자): `GunkataFx.CritSkull`(예전 종말 처형 연출 `FX_Trait_Doom` 해골 · 불길 + 몸 불길)을 `PlayerController` 치명타 타격에. `GunkataFx.Init` internal.
- **종말 처형 · 보스 추가 피해 = X자 상처 (게임에 들어가 있음)** `ExecutionFx.Rend`: 그림 + 3D (사용자: 해골처럼 Codex 그림이랑 블렌더 3D 합쳐서) — Codex 시트 `cx_rend` 8장(`docs/images/fx_src/prompts/execute_rend.txt`, 자르기 `fx_sheet_cut.py --strict` = 진홍이 안 지워지는 마젠타 키) + 블렌더 `fx_slash`(`blender_fx.py` `m_slash`) 두 획 흰 심지 빛줄기 + 핏방울 가시 + 히트스톱 0.1초(보스 0.06) + 흔들림. 크기 = 몸통 안(모델 렌더러 폭 · 높이 중 작은 쪽, 사용자: 너무 큼). 검은 블랙아웃은 뺌(사용자: 바로 죽으니 죽을 때 검은 안개랑 합쳐지면 충분). 자체 시계(deltaTime / timeScale)라 히트스톱 · 녹화 중에도 같은 빠르기.
- **망각 새 이펙트 = 미리보기만, 게임엔 아직 연결 안 함**(사용자: GIF 보고 확정하면 넣기) `OblivionFx.cs`: 타격 = 기존 망각 타격에서 파란 불똥(BakedSpark) 뺌 / 가둠 `Encase` = 블렌더 각진 얼음 덩어리 `fx_ice_block`(몸 크기 비례, 모델 렌더러만) + Codex 얼음 우리 `cx_ice_lock`(마지막 장 유지) / 6단계 `IceBloom` = 블렌더 얼음 꽃잎 `fx_ice_petal` 16장 피어남 → 하얗게 차오름 → 번쩍 깨짐(각진 조각 `fx_ice_chunk_0~2` · 유리 조각 · 별 섬광 · 반짝임 · 서리 고리 3 · 짧은 멈춤) + Codex 얼음 꽃 `cx_ice_bloom`(스컬 나이아스 참고). 얼음 재질 = `CatTang/Water` 셰이더에 얼음 색. **연결할 곳(확정 후)**: `GunkataFx.TraitHit` 3003 → `OblivionFx.Hit`, `Health.ApplyTimeLock` 호출부(망각 가둠) → `Encase(대상, 시간, 6단계 여부)`, `PlayerController.TimeLockEnded` 폭발 겉모습 → 꽃.
- **망각 이펙트 게임에 들어감 (사용자 2026-11-18 승인: "6단계부턴 아예 꽃으로, 얼음 감옥에서 풀릴 때도 깨지게 ㄱ")**: `PlayerController` TimeLock 효과 → `OblivionFx.Lock` (적 하나에 하나, `Health.TimeLocked` 동안 유지). 1~5단계 = 얼음 감옥 → 풀리거나 죽으면 깨짐(조각 12 · 별 섬광 · 고리 · 작은 흔들림). 6단계(`OblivionFx.Tier6` = OnTimeLockEnd 폭발 있음) = 감옥 대신 가둠 순간부터 얼음 꽃(꽃 그림 바닥에 눕힘) → 풀리면 0.1초 하얗게 차올라 크게 깨짐; 6단계 폭발 피해는 그대로(`Effects.AreaDamage`), 예전 폭발 겉모습은 얼음 연출을 못 쓸 때만. 망각 타격 파란 불똥 끔(`GunkataFx.NoSpark`). 컴파일 · GIF 확인함(오류 0). **직접 조작으로는 확인 안 함** (실제 망각 탄으로 가둠 · 연장 · 가둠 중 죽음 · 보스).
- **미리보기 GIF** `docs/images/fx_preview/`: `execute_rend.gif` · `oblivion_hit.gif` · `oblivion_lock.gif` · `oblivion_bloom_shatter.gif`. 녹화 도구 `Scripts/Game/FxRecorder.cs`(게임 시간 고정 간격 + 메인 카메라를 렌더 텍스처로 찍음, HUD 안 찍힘) + `docs/tools/fx_frames_to_gif.py`.
- **사고**: 플레이 중에 `CatTang/FX 메시 가져오기`를 돌렸다가(쓰이고 있는 메시를 제자리에서 덮어씀) Unity가 D3D12 오류로 꺼짐 → `FxMeshImporter`가 플레이 중엔 안 돌게 막음. MonoBehaviour 필드 초기화에서 `new MaterialPropertyBlock()` 쓰면 Unity 오류(Begin에서 만들 것).
- **남은 것**: 거울 고래 플레이 확인 (기믹 4종 · 2페이즈 · 빛줄기). 테스트 중 플레이 모드에서 스크립트를 고치면 Unity가 다시 컴파일해 상태가 날아감 — 꼭 멈추고 고칠 것. 리플렉션으로 장을 바꿀 땐 `chapter` 필드만 말고 `Chapter` 속성(`Tables.Chapter(3)`)도 바꿔야 3장 방이 지어짐.

## 2026-11-18 — 3장 레벨 디자인 확정 · 3장 기획서 Ver01 (구현 전, 기획만)

사용자와 질문으로 전부 확정 (1 · 2장 때와 같은 방식). **코드는 아직 하나도 안 건드림.** 정본은 `docs/Cat_Tang_레벨_디자인.md`의 '챕터 3' · '수치 기준 (챕터 3)' · '상인 공통 규칙' · '신기루 마술사' · '거울 고래' 절.

- **테마**: 3장 = **신기루 사막 = 거울 세계** (사용자: 거울 세계로 본격적으로 몽환적으로, 메이플 소멸의 여로 · 에스페라 참고). 모티브 '반사'. 원래 4장 '거울 미궁'은 빠지고 **4장 테마 미정**.
- **흐름**: 거울 호수 기슭 → 거울 속 서부 마을(1 · 2장 장소 좌우 반전 + 빛가루) → 중간 보스 거울 극장 → 떠도는 섬 → 뒤집힌 하늘 → 보스 거울 제단(원형 지름 40m).
- **방 기믹**: 떠다니는 거울 조각 3개(양쪽 탄 1번 반사, 몸 통과, 쏘면 15° 회전). 오브젝트: 기억 수정(폭발) · 분수 샘(물기둥 벽) · 신기루 엄폐물. 상인: 거울 속 고양이.
- **일반 적 5종**: 거울 사수 · 빛가루 법사 · 거울 너머 검객 · 유리 기사(정면 반사, 4발에 거울 깨짐 → 5초) · 신기루 분신술사. 강화형 공격 하나씩.
- **중간 보스 신기루 마술사**: 진짜 찾기(25초 쿨, 발동 때 1초 사격 봉인 → 발동 순간 발사 시각 숫자 카드가 진짜) + 거울 시계(체력 66 · 33%, 시계 1~6개, 홀수 = 위에 서기 · 짝수 = 피하기, 6초, 실패 30%). 패턴: 카드 부채 · 칼 꽂기 상자 · 모자 속 비둘기 · 거울 상자 · 분신술사 호출.
- **보스 거울 고래**: 기믹 4종(거울에 비친 시각 · 고래의 노래 · 흩어진 기억 조각 · 기억 삼키기)을 **20초 쿨로 무작위 반복**, 쿨이 차면 기믹 우선, 기믹 중엔 쿨 정지 (1 · 2장의 체력 75/50/25% 확정과 다름). 2페이즈(50%) 거울이 깨지고 공중 비행 → 일반 패턴 묶음 교체(1페이즈 수면형 4 · 2페이즈 공중형 4). 상시 탄막: 반사 빛줄기(내내) + 물결 링(1페이즈) / 기억 비(2페이즈).
- **수치**: 2장보다 피해 +25%, 중간 보스 3960 · 보스 7020, 웨이브 6~8 → 8~11.
- **세부 수치 중 사용자가 직접 고르지 않은 값**(Claude가 2장 비율로 채움, 기획서에는 확정으로 들어감 — 사용자가 보고 고칠 수 있게): 일반 적 이속 · 사거리 · 탄속, 강화형 수치, 마술사 패턴 수치 · 진짜 찾기 쿨 25초 · 제한 6초 · 반격 탄, 고래 패턴 수치 · 기믹 제한 시간(8 · 4 · 10초) · 실패 피해(40% · 조각당 7%) · 기억 삼키기 3칸 · 12초, 기억 수정 · 분수 샘 수치.
- **기획서**: `docs/1159_3장_기획서_20261118_정승운_Ver01.pptx` (20장, 생성기 `docs/tools/build_ch3_ppt.js`, 실행은 `NODE_PATH=<pptxgenjs@3.12 · image-size@1 있는 node_modules> node build_ch3_ppt.js`). 컨셉 시안 5장 `docs/images/ch3/` (Codex 이미지 생성, 명세 `Art/tools/specs/ch3_concept_spec.md`).

### 같이 정한 공통 변경 (코드 수정 필요)
- **상인 등장 (모든 챕터)**: 지금 코드는 스테이지 보상 뒤에만 15%(`RunManager.AfterReward`), 보장은 `stageIndex == Stages - 2`(스테이지 3 끝)이라 사용자가 한 번도 못 봄. → **방을 비울 때마다 15%**(`RoomCleared`), 스테이지 1 · 2에서 못 만났으면 **스테이지 2 마지막 방(중간 보스 직전)에 확정**, 고양이 · 카메라에서 보이는 쪽에 솟고 화면 밖이면 **가장자리 화살표** + 토스트. 지금은 사람 모델 없이 카운터만 솟음(`RoomBuilder.MerchantNpc`).
- **2장 트렁크 짐꾼**: 정면 120° 탄 완전 차단 → **피해 33%만** 받음(사용자: 완전 차단 불쾌). `EnemyCh2.cs` 짐꾼 정면 판정. 2장 기획서 PPT(Ver03)엔 아직 '정면 탄 막음'으로 남아 있음 → 2장 Ver04 때 반영.

### 3장 구현 할 일 (순서 제안)
1. 상인 수정 · 짐꾼 33% (작고 바로 체감됨).
2. 3장 데이터: `build_tables.py`의 챕터 · 적 · 보스 표에 3장 추가 → CSV · xlsx.
3. 3장 키트: 거울 호수 바닥(하늘 반사) · 신기루 엄폐물 · 기억 수정 · 분수 샘 · 선 거울 · 떠다니는 거울판 → 블렌더 + Codex (`Resources/Kit/Ch3/*`), 원칙대로 MCP로 프리팹.
4. 떠다니는 거울 조각 기믹(탄 반사: 고양이 탄은 즉발 판정이라 반사 처리 방식부터 정해야 함 — `Revolver` 사격 경로에 거울 교차 검사 추가).
5. 일반 적 5종 · 강화형 (Tripo 모델 · 리깅, `EnemyCh3.cs` 새로), 분신술사 분신 · 검객 거울 출몰.
6. 신기루 마술사 (`Ch3Bosses.cs`): 진짜 찾기 사격 봉인 · 숫자 카드 · 거울 시계(바닥 거울 덮기 연출 + 시계 판정).
7. 거울 고래: 기믹 쿨 스케줄러(`BossBrain`에 '기믹 쿨 · 기믹 우선 · 기믹 중 쿨 정지' 추가 — 1 · 2장은 체력 구간 방식이라 따로), 2페이즈 거울 깨짐 연출 · 공중 이동, 상시 탄막 3종, 실린더 칸 사라짐(기억 삼키기 — `Revolver`에 칸 비활성 상태 필요).
8. 4장 테마 정하기 (기획).

## 2026-11-18 (밤) — 탄 이펙트 정리 · 사거리 · 자동 추적 · 효과 이펙트 6종 (커밋 안 함)
- **원칙(사용자)**: 탄 이펙트는 추가 피해 · 효과가 실제로 발동할 때만. 평소 탄은 궤적 · 총구 섬광 · 작은 일반 타격만. 특성 덩어리(`TraitBolt`) · 맞을 때마다 특성 타격(`TraitHitAll`)은 안 부름 (함수는 남겨 둠).
- **튕김 치명타**: 튕긴 탄은 치명타를 물려받지 않고 튕길 때마다 따로 굴림. 관통은 같은 탄이라 그대로 공유.
- **사거리**: 총 연사 간격 0.3초 통일, 개성은 사거리(데린저 7 · 콜트 9 · 12 · 동경 13 · 종소리 15 · 지평선 20m) + 공격력 재조정. 조준 커서가 다음 총 사거리 밖이면 흐려지고 사거리 끝에 빨간 점(`AimCursor.RangeMark`). UI 연사 표기 → 사거리.
- **자동 추적**: 전설 총 마지막 종소리 오른손(9317)만. 새벽여명(희귀 탄)은 1명 관통. 궤적은 조준 방향에서 표적으로 휘는 곡선(`Tracer.SpawnPath`, 판정은 곧은 선).
- **은빛 메아리**: 오른손 = 관통할 때마다 은빛 메아리 폭발(피해 40% · 반경 1.5), 왼손 = 1명 관통. 이펙트 `FX_PierceEcho` = Codex 시트 `cx_pierce_echo`(`docs/images/fx_src/prompts/pierce_echo.txt`) + 블렌더 `fx_echo_star` · `fx_echo_blade`(`blender_fx.py`), MCP로 프리팹 조립. 미리보기 `docs/images/fx_preview/pierce_echo.gif`.
- **DEAR[ ] 다음 탄 +20%**: 강해진 탄 궤적이 굵고 하얗게. **세월 관통 성장**: 뚫은 자리부터 덧그린 궤적이 굵어짐. **레테의 물결**: 시간에 갇힌 적 10명당 +1(최대 10), 오를 때 HUD 그 칸 번쩍 + '공격력 +1'.
- **조건부 추가 피해 금빛 숫자**: 보스 · 표식 · 상태 이상 · 거리 · 판정 시각 · 12시 칸 등으로 늘어난 몫은 금빛 숫자로 따로(`Health.TakeDamage` condFrac, `DamageNumber.SpawnCond`). 금빛 전용 글꼴 없이 일반 숫자를 물들임.
- **데이터**: 원본은 `docs/tools/build_tables.py` → CSV · xlsx. `build_docs.py`는 손으로 고친 기획서 내용(추억 6단계 '+7%' 등)을 덮어써서 안 돌리고 md를 직접 고침.

## 2026-11-18 (밤 늦게) — 아웃게임 개편 · 메인 로비(11:59) · 저장 슬롯 · 키트 마을 · 깨어남 연출 (커밋 안 함)

- **게임 이름 = 11:59** (캣탕은 작업명). 정본 기획: `docs/Cat_Tang_컨셉_기획서.md` 5.2 · 9 · 결정 기록.
- **저장 슬롯 3개** `Core/Meta.cs`: 키 = `cattang.s{슬롯}g{세대}.*` (지우기 = 세대 +1, PlayerPrefs는 키 목록을 못 얻어서). `Meta.ActiveSlot` · `BeginSlot` · `DeleteSlot` · `SlotChapters/Shards/Runs/LastPlayed`. **예전 `cattang.*` 저장은 안 읽음** (개발 중 데이터라 옮기지 않음).
- **시설 레벨 삭제**: `FacilityLevel` · `TryUpgradeFacility` · `GradeInPool` · `StartWithOp`(공방 3단계 시작 연산) 없앰. 시설 열림 = 기억의 방 · 탄약 공방 처음부터, 총포상 1장, 의상실 2장(`FacilityUnlockChapter`). 기억 노드 = 처음 3개, 장마다 +2(`MemoryNodesOpen`).
- **해금 묶음** 새 표 `Data/Tables/Unlock.csv`(`Tables.UnlockGroups`, Items = `|`로 구분): 탄 10묶음(6001 낡은 탄띠 = 일반 15발 무료) · 총 4묶음. `Meta.BulletUnlocked/GunUnlocked` → `RunManager.RandomBullet/RandomGun/BossOptions`가 해금한 것만 냄 (하나도 없으면 일반으로 대체). **`docs/tools/build_tables.py`엔 아직 Unlock 표가 없음** — 표 생성기를 다시 돌려도 Unlock.csv는 따로 남지만 xlsx엔 없음.
- **코스튬 = 조건 달성 시 구매**: `Meta.CostumeProgress/CostumeCondMet/CostumeCondText`. 카운터 `Meta.AddCount` — 처형(`PlayerController` Execute, 보스 제외) · 극한 회피(`JustDodge`) · 특성 6단계(사격 때 최고 특성이 6단계면 1). 장 클리어 · 보스는 `ChaptersCleared`로.
- **시설 화면** `GameUI.OpenFacility`: 머리글 = 지키는 존재 이름 · 조각. 탄약 공방 = 묶음 카드 4열(`GroupContent`), 총포상 = 위 묶음 + 아래 해금한 총 시작 총 고르기, 의상실 = 조건 · 진행(예: 12/50) · 구매.
- **메인 로비** `Scenes/Lobby.unity` + `UI/LobbyScreen.cs` (빌드 목록 Lobby 0 · Village 1 · Run 2, 메뉴 `Cat_Tang/Build Lobby Scene`): **키 일러스트(주인공 쌍권총 고양이 왼쪽 · 녹는 회중시계 11:59 · 녹아내리는 서부 마을, 하데스풍 굵은 잉크 · 해칭 · 강한 명암, 참고 그림 = 턴어라운드)** · 제목 타이포(오른쪽 위) · 슬롯 카드(오른쪽 아래) · 버튼 · 아이콘 전부 Codex 그림(`docs/images/ui_src/prompts/lobby_key.txt` · `lobby_ui.txt`, 예전 파스텔 bg/fg는 key가 없을 때만 → `docs/tools/ui_lobby_cut.py` → `Resources/UI/Lobby/`). 연출: 배경 숨쉬기 · 마우스 시차 · 흐르는 안개 · 떠오르는 모래 반짝이 · 제목이 내려와 출렁 후 1초마다 똑딱 · 카드 차례로 떠오름 · 마우스 올리면 커짐 · 슬롯 고르면 라벤더 안개로 덮이며 마을로. 그림이 없으면 글자 · 기본 스킨으로 대체.
- **로비 3차 (사용자: 일러에 맞춘 타이포 · 버튼 · 패널, 플랫하게 뽑고 코딩으로 효과, 그려지는 연출)**: 메뉴 = 새로 시작(슬롯 고르기, 쓴 슬롯은 두 번 눌러 덮어쓰기) · 이어하기(쓴 슬롯만, 없으면 꺼짐 — 이걸 눌러야 슬롯이 나옴) · 설정(전체 · 음악 음량, 전체 화면, 게임 끝내기 — `Core/GameSettings.cs`, `Music.SetVolume`). UI 그림 = Codex 플랫(`prompts/lobby_flat.txt`, 단색 + 검은 외곽선) + 셰이더 `Resources/UI/Lobby/UIFlatFx.shader`(녹은 금 그라데이션 · 열기 일렁임 · 어두운 칠 해칭 · 아래 그늘 · 윗변 하이라이트 · 빛 훑기 · `_Hover`), 칸마다 재질 따로(`LobbyScreen.FxMat`, `HoverScale.fx`). 2차 칠한 그림(`lobby_ui2.txt`)은 사용자가 별로라 안 씀(원본은 ui_src에 남음). **등장 연출**: 양피지 → 잉크 선이 한 줄씩 그어짐(`docs/tools/ui_lobby_ink.py`: 진한 윤곽선만 골라 선마다 펜이 주인공에 가까운 순서로 출발, 선 위로만 퍼지는 거리로 순서 → `reveal_ink.png`, 해칭 · 어두운 칠은 근처 선 뒤) → 색 번짐(`reveal_color.png`, 가장자리 녹은 금빛) → 제목 쾅 → 메뉴. 셰이더 `UIReveal.shader`. 약 5초, 클릭 · 키로 건너뜀.
- **로비 4차**: 제목 콜론 = 점 두 개(`docs/images/ui_src/flat_title_dots.png`, 시계 지움). 비네트(`LobbyScreen.BuildVignette`, 절차 텍스처: 가장자리 · 오른쪽 메뉴 자리 · 아래 땅) · 그림자(`DropShadow`: 같은 그림 검게 세 겹, 제목 · 메뉴 판 · 패널 · 카드). VFX `UI/LobbyVfx.cs`(그림 좌표에 붙음): 해 뒤 도는 햇살 두 겹 + 빛무리 숨쉬기, 녹는 시계 아래 금 방울이 맺혔다 늘어나 떨어짐, 총구 네 갈래 반짝임, 모래 바람 불똥, 제목 쾅 때 불꽃 46개, 제목 녹은 방울. `UIFlatFx`는 리니어 색공간이라 감마로 바꿔 계산(안 그러면 바래고 판이 올리브색). 화면 캡처 도구 `Dev/UiCapture.cs`(UI 포함 PNG + times.txt).
- **로비 5차 — 그리는 과정 겹치기 + 얼굴 줌아웃** (사용자: 선이 균일하게 그어지면 그림 같지 않음 → 과정 그림 여러 장 겹치기, 옛 갱플랭크 로그인 화면처럼 얼굴부터): `LobbyScreen.BuildArt/AddStage/StepStages`. 단계 그림 `docs/tools/ui_lobby_stages.py` → `Resources/UI/Lobby/stage1~5.png`: 1 파란 연필 밑그림 · 2 연필 스케치 = Codex 이미지 편집(`prompts/lobby_stages.txt`, key를 넣어 같은 구도), 3 잉크 외곽선 · 4 잉크 + 해칭 · 5 평면 색 = key에서 계산(자리 정확). 드러나는 순서 마스크: `reveal_radial`(얼굴에서 퍼짐) · `reveal_ink`(선 따라) · `reveal_color`. 앞 단계는 다음 단계가 덮을 때 옅어졌다 사라짐. 카메라: 얼굴(key 0.255, 0.33) 2.3배 → 1.06배, 6.2초. 전체 약 7.4초, 클릭 · 키로 건너뜀.
- **로비 6차 — 선이 한 획씩** (사용자: 색이 더해지는 느낌 말고 선이 하나씩 추가되며 완성): `docs/tools/ui_lobby_strokes.py <선 그림> <출력> <문턱>` — 선 그림을 세선화(Zhang-Suen)해 이어진 선 덩어리를 한 획으로, 펜 여러 자루(큰 선 3자루 · 작은 결 12자루)가 얼굴에서 가까운 순서 · 손에서 가까운 획을 골라 끝에서 끝으로 그음 → `reveal_pencil.png`(연필 스케치 stage2) · `reveal_lineart.png`(Codex 잉크 선화 `prompts/lobby_lineart.txt` → stage3). 명암 덩어리(key_ink)는 획으로 안 쪼개짐(조각남) — 선 그림에만 쓸 것. `reveal_*` 가면은 sRGB 끔(`ResourceTextureImport`).
- **로비 7차**: 연필 = join 모드(이어진 선 덩어리째, `ui_lobby_strokes.py stage2.png reveal_pencil.png 0.22 join` — cut 모드는 갈림길마다 끊은 짧은 획), 잉크 선화도 join으로 얼굴부터 순차, 연필이 오른쪽 풍경을 그릴 즈음(3.3초) 잉크가 얼굴에서 시작. **로토스코핑**: `UIReveal`에 `_Boil`(px) · `_BoilFps` — 초당 12번 바뀌는 잡음으로 그림이 몇 픽셀 일렁임(선 단계 2.2px, 색 1.2px, 완성 그림 0.9px · 8fps로 계속), 단계 진행 · 카메라 줌도 12장/초로 끊음. 전체 약 7.7초. → 이후 사용자 "떨리는 작열 효과 빼기, 과함": 라인 보일(`_Boil`)만 끔(사용자: 떨림만 빼고 나머지 냅둬 — 금빛 가장자리는 되살림). 12장/초 진행 유지. **맨 종이부터 천천히**: 0~0.8초 빈 종이, 밑그림 0.8 · 연필 1.2~6.2 · 잉크 5.0~8.4 · 해칭 7.6 · 색 8.2 · 완성 9.2 · 제목 10.1, 전체 10.6초, 줌 0.8초부터 9초간.
- **로비 8차 — trace 모드** (사용자: 맨 종이부터 시작 안 함 — 얼굴~몸이 4.5만 픽셀짜리 한 선 덩어리라 0.8초 만에 그려지던 문제): `ui_lobby_strokes.py <그림> <출력> <문턱> trace <펜 수> <속도>` — 뼈대를 갈림길 사이 조각으로 나누고, 갈림길로 이어진 조각을 이웃으로 묶어 펜이 이어진 길을 따라 한 줄씩. 연필 `stage2.png reveal_pencil.png 0.22 trace 8 1800`, 잉크 `stage3.png reveal_lineart.png 0.3 trace 5 1600`. 확인 도구: `LobbyScreen.DebugAge`(연출 시각 고정), `UiCapture.BeginLobby(count, step, folder, from)`(시각 고정 캡처 → 정확한 GIF).
- **로비 9차 — 실제 선 긋기(마스크 없음)** (사용자: 마스크 말고 선 하나하나가 그려지게, 원으로 퍼지지 말고 여기저기서 그려지며 합쳐지게): `docs/tools/ui_lobby_vector.py <선 그림> <출력 txt> <문턱> <펜 수> <속도> <최소 길이>` — 뼈대 → 갈림길 사이 선을 점 경로로(RDP) + 굵기 → 펜이 갈림길에서 가장 자연스럽게 이어지는 선을 따라 계속 긋고, 이어진 게 없으면 그림 어딘가 무작위 자리로 건너뜀(초반엔 고양이 쪽을 조금 더) → `Resources/UI/Lobby/strokes_pencil.txt`(stage2, 펜 5) · `strokes_ink.txt`(stage3 Codex 선화, 펜 4). 게임: `UI/LobbyStrokes.cs`(MaskableGraphic, 진행도만큼 경로를 실제 선 메시로, 양 끝 가늘게 — **CanvasRenderer 꼭 같이 붙일 것**). 순서: 빈 종이 → 연필 획 0.8~6.2 → 잉크 획 4.4~8.4 → 해칭 · 평면 색 알파로 겹침 8.0~9.4 → 완성 9.3. 원형 가면 단계(파란 밑그림 · reveal_* 가면)는 안 씀(파일은 남음).
  - 이후 사용자: 그림 단계는 좋고 타이밍만 빠르게, 흑백 디테일은 아까처럼 → 전체 8.2초(연필 0.5~4.3 · 잉크 3.0~5.8 · 흑백 해칭 `stage4` reveal_radial 5.2~6.1 · 흑백 잠깐 · 색 6.4~7.1 · 완성 7.0 · 제목 7.8). 이어서 색(`stage5`)도 흑백 디테일처럼 reveal_radial로 퍼지며 입혀짐(6.3~7.2, 가장자리 금빛), 완성 7.1.
- **키트 마을** `Editor/VillageKitBuilder.cs` (메뉴 `Cat_Tang/Build Village (Kit)`, `Build All Scenes`도 이걸 씀): 1장 키트 프리팹으로 북 여관(기억의 방) · 서 총포상 · 동 탄약 공방 · 의상실 · 남 시간의 문(나무 문 + 녹은 시계 + 등불), 우물 광장 · 녹은 시계 4 · 랜드마크 · 바위 둑 + 보이지 않는 벽. 바닥 = 판과 같은 이어지는 흙(`Kit.GroundMesh` public으로). 시설 앞 `NpcSpot_*` · 문 옆 `NpcSpot_Gatekeeper` = **NPC 모델 자리(아직 빈 자리)**. 예전 도형 마을 `SceneBuilders.BuildVillageInternal`은 안 씀.
- **깨어남 연출** `Game/VillageWake.cs` (`VillageManager.Start`에서): 라벤더 안개 화면이 걷히며 누운 고양이 둘레로 연기 → 몸을 일으키고 연기가 바깥으로 흩어짐 → 카메라 물러남(`CameraFollow.SnapZoom`) → 조작. 약 2.7초.
- **확인함**: 컴파일, 로비 → 슬롯 3 새로 시작 → 마을 진입 · 연기 · 일어남 · 조작 풀림, 해금 확인(1013 잠김 · 1001 풀림), 탄약 공방 묶음 화면.
- **안 됨 / 다음**: NPC 5 원화(ChatGPT, 사람 얼굴 X) → Tripo 모델 → `NpcSpot_*`에 배치 · 마을에서 ESC로 로비 돌아가기 · build_tables.py에 Unlock 표 · PPT 기획서 반영 · 직접 조작으로 마을 동선 확인.

## 2026-11-18 (밤 2) 새 마을 — 다리로 이어진 섬 5개 · 장 해금 · NPC 5 · 첫 흐름 (커밋 안 함)

사용자: 1장 키트 재활용 X, '소멸의 여로' 느낌(분위기만) → 새벽 별밤 허공의 섬 여러 개를 다리로 잇고, 장을 넘을 때마다 섬 · 컨텐츠가 열림. 기획서 `docs/1159_마을_기획서_20261118_정승운_Ver01.pptx` (14장, 생성기 `docs/tools/build_village_ppt.js`, 그림 `docs/images/village/`).
- **흐름**: 새 슬롯 = 마을 없이 바로 1장 전투(`Meta.FirstRunPending` → `LobbyScreen.EnterCo`가 Run 로드) → 판이 끝나면 마을에서 깨어나 뻐꾸기 첫 대화. 판은 상시 1장부터(기존 그대로). **해금** `Meta.FacilityUnlockChapter = {1,3,2,2}`: 1장 = 기억의 방(노드), 2장 = 탄약 공방 · 의상실 섬, 3장 = 총포상 섬.
- **총탄 · 총**: 지금 있는 건 전부 기본 해금 — `Data/Tables/Unlock.csv`를 머리글만 남김(어느 묶음에도 없는 아이템 = 해금, `Meta.ItemUnlocked`). 묶음이 없으면 상점에 '새 탄/총은 아직 만드는 중' 문구. 새 총탄 · 총을 만들면 Unlock.csv에 묶음 추가. (예전 묶음 10 + 4는 git 기록에 있음)
- **마을 씬** `Editor/VillageKitBuilder.cs` `BuildVillage` (메뉴 `Cat_Tang/Build Village (Kit)`): 섬 5(`Isle` 가운데 34×30 · 문 섬 남 · 동 탄약 공방 · 북동 의상실 · 서 총포상), 섬 윗면 = 타원 메시(`Art/Env/Village/Island_*.asset`) + 마을 흙 텍스처, 가장자리 = 바스러지는 절벽 조각 둘레 배치 + 안쪽 보이지 않는 벽(다리 입구는 비움), 다리 4(`IslandBridge`: 잠김 = 끊긴 다리 + 양 끝 막음 + 건너편 NPC 숨김, 처음 열린 귀환 때 `Reconnect` 연출), 허공 판(`void_village.png`, 새벽 별밤 · 은하수), 떠 있는 조각 섬 · 부스러기, 새벽 조명(복숭아빛 해 + 3색 환경광). 텍스처 `Art/tools/ground_texture_village.py`.
- **마을 키트(블렌더 + Codex)**: 명세 `Art/tools/specs/_village_common.md` + `kit_village_{buildings,island,props,bridge}_spec.md` → `Art/tools/blender_kit_village_*.py` → `Art/Characters/Village/Kit_V_*.fbx` → 메뉴 `CatTang/마을 키트 만들기 (블렌더)` (`EnemyModelBuilder.KitVillage`, 배색 `ModelPalette.Village`, 창 · 등불 · 모래 빛 · 빛가루 = 발광 단색 `VillageGlowMats`, 가장자리 · 섬 · 다리 · 광장은 `KeepOrigin`). 건물 5 · 섬 9 · 다리 2 · 소품 15.
- **건물 위 떠오르는 조각**(사용자: 건물 위 벽돌은 파티클로 천천히 계속 올라가게): 모델의 떠 있던 조각은 지움(`kit_village_buildings_nofloat_spec.md`), `VillageKitBuilder.RisingBits`가 건물마다 파티클 2개(벽돌 · 판자 메시 조각 7~11초 천천히 돌며 상승 → 작아져 사라짐 + 빛나는 모래 알갱이). 재질 `Art/Env/Village/RisingBits.mat`.
- **NPC 5**: ChatGPT 원화(`Art/tripo_input/npc/`, 프롬프트 `prompts.md`) → Tripo 웹 H3.1 생성 → 리토포 1만 면 → 오토 리그(Mixamo; 문지기 · 재봉사는 '휴머노이드 안 맞을 수 있음' 경고 무시하고 진행, 아바타 정상) → FBX → `tripo_import.py` → `Art/Characters/Npc/Npc_*.fbx` → 메뉴 `CatTang/마을 NPC 프리팹 만들기 (Tripo)` (`EnemyModelBuilder.BuildNpcs`, 상인과 같은 `BuildIdleFigure` · 고양이 idle) → `Resources/Npcs/Npc_*.prefab` → `NpcSpot_*`(`PlaceNpc`, 메뉴 `Cat_Tang/Place Village NPCs`). 키 뻐꾸기 2.1 · 대장장이 2.7 · 보안관 2.9 · 재봉사 2.7 · 문지기 3.8m. Tripo 작업 id: 뻐꾸기 cb09bf7a · 대장장이 05ad56c8 · 보안관 71261fdc · 재봉사 c3e13c1d · 문지기 fb999f09 (studio.tripo3d.ai/workspace/rigging/<id>). 크레딧 1375 남음.
- **이야기** `Game/VillageStory.cs` (깨어남 뒤, 안 본 장면만, `Meta.StorySeen/MarkStory`): intro(뻐꾸기) · ch1(뻐꾸기, 기억의 방) · ch2(다리 연결 → 대장장이 · 재봉사) · ch3(다리 → 보안관). 말하는 NPC를 카메라가 비춤(`CameraFollow.FocusOn`). 대화 창 `GameUI.Talk`(E · 스페이스 · 엔터 · 클릭). 대사 문구는 초안.
- **새벽 조명 · 색보정**(사용자: 조명에 파란빛으로 새벽 느낌): `VillageKitBuilder.DawnLighting` — 해 = 옅은 푸른빛(0.84, 0.87, 1), 환경광 3색(남색)을 SH에 직접 구움(`RenderSettings.ambientProbe`, 툰 셰이더가 SampleSH로 읽음). 전역 볼륨 `Art/Env/Village/DawnVolume.asset`(없을 때만 빌더가 만듦 — 에디터에서 고친 값 유지): 화이트 밸런스 −6 · 그늘 파랗게/밝은 곳 따뜻하게(ShadowsMidtonesHighlights) · 채도 +16 · 옅은 푸른 필터 · 블룸 · 남색 비네트. 스플릿 토닝은 화면이 보랏빛 회색으로 죽어서 뺌. 카메라 후처리 켬. 가로등 = 바닥 가산 빛 웅덩이(`lamp_pool.png`, `LampPool.mat`) — URP 추가 조명이 정점 단위라 툰 셰이더(픽셀 추가 조명만 읽음)에 점광원이 안 먹어서. **주의**: 위치 지정 캡처(manage_camera view_position)는 임시 카메라라 색보정이 안 보임, 게임 카메라로 확인할 것. 비교 그림 `docs/images/village/dawn_light.png`.
- **(밤 3) 몽환 강화 — 완료, 커밋 안 함** (사용자: 새벽 더 세게 · 부서진 시간 / 신비한 나무 · 오브젝트 · 바깥은 뒤집힌 밤하늘 · 보이는 초승달 · 섬 불규칙 도형 · 건물 휘고 비틀린 불규칙 도형(레퍼런스: 크쥐비 도멕 · 꼬인 굴뚝 · 해비타트 67) + 몇 군데서 파티클이 하늘로):
  - 바닥: 한때 푸른 흙으로 바꿨다가 사용자가 "서부 느낌이 빠짐" → **원래 서부 흙색**으로 되돌림(깨진 시계 고리 무늬만 크림색으로 남김, `ground_texture_village.py`). 섬 흙 · 바위 배색도 원래 따뜻한 색(`ModelPalette.Village`).
  - 밤하늘 = Codex 그림 `docs/images/village_src/night_sky.png` → `void_village.png`(위 새벽 띠 잘라 4×4 거울 반복). 초승달 `crescent_moon.png` → `moon.png`(마젠타 → 투명), 섬 사이 틈 2곳에 카메라를 향한 판(`CrescentMoon`). 구름층 2겹(크게 · 옅게, `CloudScroll`) · 별빛 반짝임 · 섬 빛가루(`SkyLayers` · `IslandMotes`, `spark.png`). 절차 생성 밤하늘은 `docs/images/village_src/void_village_procedural.png`로만 남김(안 씀).
  - 섬 윤곽: `Shape` = 타원 × 큰 물결 4겹 + 곶 3 + 파인 만 1(최소 0.78), 가장자리 조각은 둘레 길이 기준 배치.
  - 키트: 부서진 시간 12(`kit_village_dream_spec.md`, `DreamDecor` 배치) · 신비한 나무 · 오브젝트 8(`kit_village_mystic_spec.md`, `MysticDecor` + `FreeSpot` 빈자리, 나무엔 `PetalRise`) · 건물 = 휘고 비틀린 형태(`kit_village_buildings_crooked_spec.md`, 그 전 단계 `_ruin_` · `_nofloat_` 명세는 기록용). 떠 있는 것 `Game/DreamFloat.cs`.
  - 건물 파티클: 위쪽 절반에서 조각 · 빛 모래(`RisingBits`) + 가장 높은 곳 2~3곳(꼬인 굴뚝 꼭대기 등)에서 기둥처럼 솟음(`RiseSpots`, 메시 정점에서 자동으로 고름).
  - 새벽 색보정 최종값(`DawnProfile`, 에셋 `Art/Env/Village/DawnVolume.asset` — 없을 때만 만들어짐): 해 (0.92, 0.9, 0.98) · 화이트 밸런스 −3 · 그늘 파랗게(0.7, 0.83, 1.26) · 밝은 곳 따뜻하게 · 채도 +20 · 필터 거의 흰색. 바닥이 따뜻해서 푸른빛은 그늘 · 바위 · 환경광으로만.
  - 확인함(게임 카메라): 광장 주변 몽환 오브젝트, 여관 · 의상실 굴뚝에서 솟는 파티클, 섬 불규칙 윤곽, 초승달. 참고 그림 `docs/images/village/dream_plaza.png` · `dream_overview.png`.
  - **(마지막, 다른 세션에서 이어서) 오브젝트 정리 + 색감 되돌림 — 코드만 고침, 씬은 아직 다시 안 지음**: 사용자가 고른 화면(`docs/images/village/dream_plaza.png` 직전 단계, 서부 흙 + 푸른 새벽)의 색감 = 해 (0.8, 0.85, 1) · 화이트 밸런스 −8 · 중간톤 (0.95, 0.97, 1.04) · 밝은 곳 (1.08, 1, 0.92) · 채도 +16 · 필터 (0.9, 0.94, 1)로 `DawnProfile`/`DawnSunColor`를 되돌렸고, "불필요하게 오브젝트가 너무 많음" → `DreamDecor`(섬마다 1~3개 + 허공 3개) · `MysticDecor`(가운데 4 · 나머지 섬 1~2, 뜬 바위 3) · 풀 흩뿌림 0.08 → 0.03 · 가운데 섬 벤치 1 · 상자 · 선인장 1 줄임. `Art/Env/Village/DawnVolume.asset`은 지워 둠(빌드 때 새 값으로 생김).
    **씬 다시 지음 · 확인함 (2026-11-18 밤, 이어서)**: `BuildVillage()` 실행 → 씬 저장, `DawnVolume.asset` 새 값으로 생김. 게임 카메라(3번 슬롯 임시 세대 97 · 3장 클리어 상태, 끝나고 세대 1로 되돌림)로 가운데 섬 · 총포상 · 탄약 공방 · 의상실 · 문 섬을 봄 → 오브젝트 양 적당, NPC 5 모두 자리에 있고 길을 막지 않음, 플레이 중 콘솔 에러 없음. 전체 그림 `docs/images/village/decor_trim_overview.png`. 더 덜어 내진 않음. 작은 점: 대장장이 옆에 통 더미가 붙어 있음, 문 섬은 건물 위 조각 파티클이 문 앞을 꽤 덮음(줌을 당기면 화면 가까이 와서 크게 보임) — 사용자가 보고 거슬리면 `RisingBits` 양 · 문 건물 제외 검토.
    - **이어서 (사용자)**: ① 꽃 · 버섯 · 정령 등불 · 수정 나무의 흙 받침 + 둘레 돌(`moss_rock`, 재질 earth)이 땅과 따로 놀아서 → 모델은 그대로, 프리팹 만들 때 받침 윗면 높이만큼 땅에 묻음(`EnemyModelBuilder.SinkBed` · `SubmeshTop`, 0.18~0.26m) → 식물이 땅에서 바로 자람. ② 대장장이 옆 통 더미 섬 로컬 (−3.5, 5.8) → (−4.6, 6.4) (대장장이와 1.6 → 2.7m). 마을 키트 · 씬 다시 만듦, 게임 화면 확인함, 에러 없음.
    - **이어서 2 (사용자)**: ③ 뻐꾸기 자리(`NpcSpot_MemoryRoom`)가 문 옆 가로등에 붙어 있어 월드 (−3.4, 3.2) → (−5.2, 3.6). ④ **흙먼지(`FxBurst.Kind.Dust`) = 적이 사라질 때 검은 안개(`bk_mist`)와 같은 회보라 · 보라 테두리**: 셰이더 `CatTangFX` `dust()`에 색을 넣고(bk_mist 그림 색을 리니어로), 흙색을 주던 곳(적 돌진 · 착지 · 내려찍기 · 2장 적 · 하트 · 문 · 석상 · 신호등 · 탄자국)은 `FxBurst.MistDust`(흰색 = 물들이지 않음)로. 따로 색을 준 먼지(3장 빛가루 · 분홍 · 하늘빛, 2장 보스 증기 `Ch2Fx.Smoke`)는 예전처럼 밝은 연기에 물듦. 화면에서 안개와 나란히 비교함. ⑤ **땅에 박힌 시곗바늘 → 시계탑 둘레를 떠서 도는 시곗바늘**: 블렌더 `blender_kit_village_dream.py`에 `ClockHand_Float`(돌판 · 튄 돌 없이 바늘만 눕힘 + 추 안 시계판 · 축 시간 수정, 크기 None = 만든 치수 그대로 — 맨 뒤에 붙여 다른 12개 시드 그대로) → `Kit_V_ClockHand_Float`(길이 5.6m, KeepOrigin). `VillageKitBuilder.OrbitHand`: 시계탑 위 4.6m 축(`ClockHand_Orbit`, `DreamFloat` 초당 4도 = 90초 한 바퀴 · 느린 오르내림)에 바늘 가운데 반경 5.4m, 끝이 바깥(끝 ≈ 8m, 시계탑 1.4m · 여관 벽 10m와 안 닿음). **바늘 끝 황금빛 모래선** = TrailRenderer 13초(반투명 금빛 단색 — 가산 · 반짝이 그림 바둑판 재질은 밝은 흙바닥에서 안 보였음) + 끝에서 흩날려 가라앉는 반투명 반짝이 모래알 파티클. 예전 `ClockHand_Giant`는 마을에서 안 씀(키트엔 남음). 화면 확인함, 에러 없음.
  - 남은 것: 기획서(`build_village_ppt.js`)에 이번 변화(불규칙 섬 · 비틀린 건물 · 몽환 오브젝트 · 밤하늘) 반영 안 함, 직접 조작으로 걸어 다니며 오브젝트에 막히는 곳 확인, 섬 모양이 바뀐 뒤 다리 · 경계 벽 손맛.
- **마을 ESC 메뉴** `GameUI.OpenVillageMenu`: 계속하기 · 메인 로비로(라벤더 안개 → Lobby).
- **확인함(플레이)**: 컴파일, ESC → 로비, 첫 방문 뻐꾸기 대화, 2장 클리어 상태에서 다리 이어짐 → 대장장이 대화, 잠긴 다리 끊김 · 문 섬 다리 열림, 건물 위 조각 상승. 테스트는 3번 슬롯 임시 세대(97~99)로 하고 원래 세대 1로 되돌림(3번 슬롯은 원래 빈 슬롯).
- **안 됨 / 다음**: 직접 조작으로 다리 건너기 · 섬 경계 벽 손맛, 새 슬롯 → 바로 1장 → 귀환 전체 흐름을 로비부터 이어서(코드만), NPC 크기 · 위치 측정 후 조정, 대사 다듬기, PPT 손보기(14쪽 그림 위쪽 회색 띠), 새 총탄 · 총 묶음(Unlock.csv), `build_tables.py`에 Unlock 표 반영.

## 2026-11-19 — 효과음 · 배경음악 (audio-studio-mcp = Stable Audio 3 로컬) · 관통 치명타 개별 (커밋 안 함)

- **도구**: `C:/Project/audio-studio-mcp` (MIT 코드 + Stable Audio 3 medium 가중치, Stability AI Community License — 사용자가 상업 등록 제출함, 연 매출 100만 달러 미만 무료, 생성물로 다른 생성 AI 학습 금지). RTX 5060(sm_120) 때문에 PyTorch를 **2.7.1+cu128**로 바꿈 (`install.ps1`을 다시 돌리면 cu126으로 돌아가 생성 실패 → `uv pip install --python .venv\Scripts\python.exe torch==2.7.1 torchaudio==2.7.1 --index-url https://download.pytorch.org/whl/cu128 --reinstall-package torch --reinstall-package torchaudio`). Claude Code 등록 = `~/.claude.json` mcpServers `audio-studio` (새 세션부터 도구 보임).
- **생성기** `Art/tools/gen_audio.py [sfx|bgm|all] [이름…]`: 원본 WAV → `Art/audio_src/{sfx,bgm}/`, 다듬은 OGG → `Resources/Audio/Sfx/<이름>[_n].ogg`(무음 자르기 · 모노 · 피크 −1dB) · `Resources/Audio/<bgm>.ogg`(−16 LUFS · 1.5초 겹쳐 끊김 없는 반복). 시드 고정(1159), 마음에 안 들면 `SEED`에 이름별 시드를 넣고 그 이름만 다시.
  - 효과음 14종(23파일): shot×3 · reload · reload_end · dash×2 · justdodge · hit×3 · crit×2 · enemy_die×2 · player_hurt×2 · explosion×2 · heart · levelup · door · merchant.
  - 배경음악 10곡(각 2분): bgm_village · bgm_stage_1~3 · bgm_midboss_1~3 · bgm_boss_1~3. 예전 Gemini 곡 `bgm_stage/boss.mp3`는 `Art/audio_src/old_gemini/`로 옮김.
- **코드**: `Core/Sfx.cs`(2D 16성부, 여러 파일이면 무작위, 0.06초 안 같은 소리 겹침 제한, `Sfx.Volume`) — 연결: 사격 · 장전 시작/끝 · 대시 · 극한 회피(`PlayerController`), 타격 · 치명타(`ApplyHit`), 적 사망(`Enemy.Die`), 고양이 피격(`Health`), 폭발(`GunkataFx.Explosion`), 하트 · 레벨 업 · 문(`RunManager.EnterDoor`) · 상인 등장. `Music.PlayFor(kind, 장)` = bgm_{stage|midboss|boss}_{장} → 없으면 1장 곡(4 · 5장). 중간 보스 뒤 스테이지 3에서 맵 곡으로 돌아옴. `Editor/ResourceAudioImport.cs`: 배경음악 스트리밍 · 효과음 풀어 두기.
- **관통 치명타**: 관통한 적마다 치명타를 따로 굴림(`PlayerController.Shoot`의 `RollCrit`), 확정 치명타(critShots)는 첫 적만. `dmg`는 치명타 전 피해. 공명 · 은빛 메아리 범위 피해는 예전처럼 첫 적이 치명타면 배율이 붙음(`shotCrit`, 사용자: 유지).
- **UI · 시설 소리 (2026-11-19 완료, 파일 16개)**: `UIKit.MakeButton` 클릭 `ui_click` + 마우스 올림 `ui_hover`(`UiSound`, 못 누르는 버튼은 조용), `CloseButton` `ui_close`, 로비 버튼 · `HoverScale`도 같은 소리. 시설 열림 `fac_memory/fac_gun/fac_ammo/fac_wardrobe`(`GameUI.OpenFacility`), 시간의 문 `fac_gate` · 출발 `gate_depart`, 구매 `ui_buy` / 실패 · 잠긴 시설 `ui_deny`, 대화 글자 3개마다 `talk`, 다리 이어짐 `bridge`. 생성 주의: MCP 서버(작업 스레드)나 비동기 루프 안에서 모델을 올리면 safetensors를 읽다 access violation으로 죽음 → `gen_audio.py`는 기본으로 같은 프로세스 메인 스레드에서 모델을 먼저 올리고 도구 함수를 바로 부름(`--mcp`면 예전 방식). Unity가 켜져 있으면 가상 메모리 부족(os error 1455)이 날 수 있음(페이징 파일 12GB). 클릭 · 마우스 올림은 0.03초짜리 째깍.
- **배경음악 = Gemini로 교체 (사용자: Stable Audio 곡은 아쉬움, 효과음은 괜찮음)**: Gemini(자동 모드) '음악 만들기'로 10곡(각 2분 40초~3분) → 원본 MP3 `Art/audio_src/gemini/` → 끝 무음 자르기 · −16 LUFS · 48kHz OGG → `Resources/Audio/bgm_*.ogg` 덮어씀(이름 같아 코드 그대로). Stable Audio 곡은 `Art/audio_src/bgm/*_loop.ogg`에 남음. Gemini 'Hydrogen' 모드는 음악을 안 만들고 글로 답함 → 자동 모드에서. 내장 브라우저 다운로드는 `~/Downloads`에 .tmp로 잠깐 생겼다 사라져서 바로 복사함. 반복 이음새(끝 → 처음)는 곡을 통째로 반복 — 이음새가 어색하면 끝부분 페이드 · 크로스페이드 필요.
- **건카타(불릿 파티) 소리**: 한 타마다 총소리(`BodyBlast`에서 `shot`), 도는 동안 반복 회전음 `Sfx.Loop("gunkata_spin")` → 끝에서 `StopLoop`(판이 도중에 끝나면 `OnDestroy`). 회전음 `gunkata_spin.ogg`(2.75초, 반복용으로 다듬음) 만듦. **메모리**: 공식 로더는 fp32 모델 + 체크포인트 전체를 한꺼번에 올려(18GB+) 페이징 파일 12GB에서 access violation → `gen_audio.py`의 `low_memory_loader()`가 모델을 먼저 fp16으로 줄이고 텐서 하나씩 채움(약 9GB, vendor 코드는 그대로). 그래도 가끔 첫 시도에 죽으면 한 번 더 돌릴 것. 가상 메모리 설정은 시스템 설정이라 Claude가 바꾸지 않음.
- **확인함 (Unity)**: 컴파일 에러 없음, 마을에서 새 `bgm_village` 재생 · 효과음 재생, 배경음악 스트리밍 가져오기.
- **확인 안 됨**: 판 안에서 직접 들어 보기. 소리는 길이 · 음량만 쟀고 들어 보지 않음 — 사용자가 듣고 바꿀 것 고르기(대시 0.19초로 짧음, 장전 소리 작음). 마을 시설 · UI 클릭 소리 없음.

## 2026-11-19 — 윈도우 빌드 · 빌드 표 복사 · 시설 화면 스크롤 · 깨어남 안개 (커밋 안 함)

- **윈도우 빌드**: `Game/Builds/Windows/1159.exe`(Git 제외). 제품 이름은 아직 "Game"(창 제목) — "11:59"로 바꾸면 저장 위치가 바뀌니 사용자 결정 필요.
- **빌드에서 표가 비던 문제**: `Tables.Read`가 `Application.dataPath/Data/Tables`에서 CSV를 읽음 → `Editor/TablesBuildCopy.cs`(빌드 후처리)가 CSV를 `<게임>_Data/Data/Tables`로 복사. 빌드에서 챕터 5 · 탄 47 · 총 8 읽힘 확인.
- **시설 화면 내용 = 스크롤** (사용자: 기억의 방 세 번째 줄이 잘림 → 카드 크기 그대로, 스크롤바로): `GameUI.ScrollArea`(가림 + ScrollRect, 휠 45) · `FitScroll`(가운데 기준 카드를 위쪽 정렬 + 넘친 만큼 내용 늘림), 다시 그려도 보던 자리 유지. 네 시설 공용.
- **스크롤바 그림** = Codex(`docs/images/ui_src/prompts/scrollbar.txt` → `scrollbar.png` → `python docs/tools/ui_scroll_cut.py docs/images/ui_src`) → `Resources/UI/scroll_track`(놋쇠 마개 홈) · `scroll_handle`(금빛 손잡이, 줄무늬 지움) · `scroll_grip`(줄무늬 조각, 손잡이 가운데에 얹음). 9분할 테두리는 `UIKit.Spr`의 이름 표(홈 위아래 26 · 손잡이 18), 반 크기로 그림(ppu 배율 2, 폭 24px), 손잡이는 마개를 안 덮게 위아래 14px 안쪽. 그림이 없으면 예전 단색. 넘칠 때만 보임(AutoHide).
- **깨어남 연기** = 적 사망 안개 `bk_mist`(회보라)로 (`VillageWake`).
- **확인함 (Unity 플레이)**: 컴파일 에러 없음, 기억의 방 열어 스크롤바 그림 · 맨 위 / 맨 아래(세 번째 줄 다 보임) 화면 확인. 직접 휠 · 끌기 손맛은 사용자 확인 필요.

## 남은 다음 작업 (2026-11-17 밤 정리 — 사용자: 브롤풍 기본 적용까지만 하고 나머지는 여기 남기고 대기)

### 그림체 마무리
1. **브롤풍 남은 손질**: 고양이(어두운 망토)가 적보다 덜 튐 → 고양이만 밝기 · 채도 더, 또는 림 강하게 / 이펙트 툰 불꽃 셰이더(`CatTang/ToonFire`) · FX 재질도 브롤 톤에 맞출지 / 에디터 씬 화면은 `_BrawlActor` 표시가 없어 캐릭터도 배경 값으로 보임(플레이 중엔 정상).
2. 확정된 값을 재질 · 빌더(`EnemyModelBuilder.ToonFor` · `EnvTone`)에 반영할지 검토 — 지금은 셰이더 전역 스위치 + 프로퍼티 블록이라 재질은 예전 값 그대로.

### 직접 조작 확인 (지금까지 화면 캡처로만 봄)
3. 반전 커서(움직일 때 · 중간 회색 바닥), 불릿 파티 바닥 탄자국, 특성 탄도 · 여러 특성 동시(실린더로 세트를 켜고 쏴서 `ShotTraits` 확인), 폭탄병이 모래 폭탄 모델을 던지는지, 특성 타격 4~9 · 몸 반응 연사 때 어지럽지 않은지.

### 게임 내용
4. ~~2장 나머지~~ (위 '2장 중간 보스 · 보스' 절에서 함). 남은 것: 직접 조작으로 역장 · 거인 손맛 · 모션 확인.
5. 1장 보스의 다른 패턴(휘두르기 · 태클 등)도 `PropHit.Area`로 폭발물에 반응하게.
6. 코스튬 4005 '거울 망토' 해금 = 누적 극한 회피 100회(`UnlockType.JustDodgeCount`) — 세는 코드 없음.
7. 2장 직접 조작으로 한 판(짐꾼 모션 · 공중 짐 · 레벨 업 빈도 · 경험치 곡선 12 + 6 × (Lv − 1)).

### 기획서
- **8~10번 반영함 (2026-11-17 밤)**: `docs/1159_2장_기획서_..._Ver02` · `공통_레벨디자인_..._Ver03` · `컨셉_..._Ver03` · `총 / 총탄 / 코스튬_..._Ver02`(완벽 장전 삭제 · 마지막 종소리 장전 끝 · 거울 망토 극한 회피 100회), 레벨 디자인 md · 컨셉 md. 생성 · 편집 스크립트 `docs/tools/build_ch2_ppt.js`(Ver01 손 수정 문구를 옮겨 둠) · `pptx_kit.py` · `edit_common_ver03.py` · `edit_concept_ver03.py` · `edit_items_ver02.py`.
  - 남은 점: 2장 '정지 구역 — 모든 투사체 정지'는 사용자 문구 그대로(구현은 적 탄만), 2장 '총탄 6발' 표기(그림 설명은 '표 탄'), 경험치 슬라이드 그림은 UI 아이콘뿐(3택 창 인게임 캡처 없음).
8. 경험치 · 레벨 업(수치 확정 뒤) · 하트 드랍 2~3% · 레벨 업 체력 회복 30%를 컨셉 · 공통 레벨 디자인 기획서에 반영.
9. PPT 기획서(.pptx)에 완벽 장전 삭제 · 마지막 종소리(장전 끝 발동) · 거울 망토 해금 조건 반영 (md는 고침).
10. 이번 이펙트 확정(특성 타격 4~9 · 고리 정리 · 특성 탄도 · 여러 특성 규칙 · 반전 커서 · 브롤풍)을 기획서에 반영할지.

### 아트 · 맵 (예전부터)
11. 1장 적 6종(기본 3 + 강화형 3)을 명암 없는 플랫 컬러로 다시 생성(GPT → 디노이즈 → Tripo → 리토포 → 리깅 → `tripo_import.py` → 빌더).
12. 공격 모션을 공격에 맞게 짧고 분명하게(Tripo 모션 1~2초, 동작별 프롬프트, 클립 이름은 `EnemyModelBuilder.Anims`).
13. 맵 굽기: 남은 소품 · 맵 타일 → `Resources/Kit/Ch1/*` → MCP로 방 프리팹(전투방 10 · 중간 보스 큰길 30×14m · 보스 원형 광장 지름 24m), 런타임은 방 프리팹을 골라 놓기만.
14. 자폭병 던지기 실제 AI 투척으로 다시 확인.

### 기타
- `Game/Assets/_Recovery/`(씬 복구 폴더) · `Art/tripo_input/ch2/.tripo/`는 커밋하지 않음 — 필요 없으면 지울 것.
- 적 FBX가 커서(1장 206MB) 푸시가 무거우면 Git LFS 고려.

## 확인 안 된 것

- 실제 판을 처음부터 끝까지(스테이지 1 → 중간 보스 → 스테이지 3 · 4 → 보스) 이어서 해 보지 않았음. 각 적 · 보스 · 패턴은 따로 소환해서 확인함.
- 폭탄병 도약 · 커지는 예고 · 3D 폭발은 화면으로 확인함. 화약통 연쇄 · 엄폐물 파괴 · 시간 지연 구역 · 되감기 폭발은 아직.
- 몬스터를 키운 뒤 보스 · 중간 보스 패턴 사거리(휘두르기 반경 · 손 판정 등)와 적 탄 발사 높이는 다시 맞추지 않음. 몸에 비해 작아 보이면 조정.
- 폭탄병 HP바는 도약 중 모델을 따라 올라가지 않음(바닥 높이에 남음).
- 2장 짐꾼 모션은 멈춘 화면으로만 봄. 직접 움직이는 모습은 사용자 확인 필요.
- 정지 구역은 적 탄만 멈춤(고양이 탄은 즉발이라 대상 아님, 기획서 '양쪽 모두' 중 고양이 쪽 미구현).
- 적 FBX가 하나에 약 25MB, `Game/Assets/Art/Characters/Ch1` 206MB. 푸시가 무거우면 Git LFS 고려.

## 주요 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Game/Enemy.cs` | 일반 적 3종 · 강화형 행동, 애니메이터 연결(`Anim`, `AnimBusy`) |
| `Scripts/Game/EnemyFactory.cs` | 적 생성(프리팹 있으면 모델, 없으면 도형), 강화형, 중간 보스 · 보스 |
| `Scripts/Game/EnemyCh2.cs` | 2장 차장 · 짐꾼 · 신호수 행동 (+ `SignalBlast.cs` 신호등 · `StopZone.cs` 정지 구역 · `PropHit.cs` 몬스터 공격 → 폭발물) |
| `Editor/PorterRig.cs` | 짐꾼 트렁크 분리 · 전용 클립 · 발 붙이기 |
| `Scripts/Game/BossBrain.cs` | 패턴 고르기 · 페이즈 · 패턴 실행 공통 |
| `Scripts/Game/Ch1Bosses.cs` | 중간 보스 · 보스 패턴, 웅덩이 |
| `Scripts/Game/Telegraph.cs` | 바닥 예고 |
| `Scripts/Game/EnemyProjectile.cs` | 적 탄 스프라이트 6종 |
| `Scripts/Game/Kit.cs` | 맵 키트 조립 · 화약통 · 엄폐물 · 솟아오르기 |
| `Editor/EnemyModelBuilder.cs` | Tripo 모델 → 툰 프리팹 + 애니메이터 |
| `Art/tools/` | `denoise.py` · `sheet_cut.py` · `tripo_import.py` |
| `Art/tripo_input/ch1/` | Tripo 입력 이미지(적 · 강화형 · 보스 · 소품 · 타일) |
