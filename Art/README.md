# 모델 제작 규칙 (11:59 / Cat_Tang)

마지막 갱신: 2026-11-17

## 1. 어떤 도구로 만들까

| 대상 | 도구 | 예 |
|---|---|---|
| **간단하고 움직이지 않는 물체** — 벽, 바닥, 소품, 지형지물, 문, 픽업 | **블렌더 + Codex** (Codex가 블렌더 파이썬 스크립트를 짜서 도형 조합으로 모델링) | 건물 정면, 절벽 조각, 나무 문, 화약통, 상자, 하트, 모래 폭탄 |
| **복잡하고 리깅 · 모션이 필요한 것** — 캐릭터, 몬스터, 보스 | **Tripo** (그림 → 3D 생성 → 리토폴로지 → 오토 리그 → 모션) | 고양이, 태엽 무법자, 시계추 깡패, 모래시계 폭탄병, 보스 |

- 판단 기준: **뼈대(리깅)가 필요하거나, 도형 조합으로 만들기 힘든 유기적인 형태**면 Tripo. 그 외에는 전부 블렌더 + Codex.
- Tripo 크레딧은 캐릭터 · 몬스터에만 씀.
- 움직이는 소품(부서지는 문, 튀는 하트 등)도 뼈대가 필요 없으면 블렌더 + Codex. 움직임은 조각을 나눠 두고 코드로 처리 (예: 나무 문은 조각 32개로 미리 쪼개서 내보냄).

## 2. 블렌더 + Codex 작업 순서

1. 명세 쓰기: `Art/tools/specs/<이름>_spec.md` — 크기(m), 모양, 조각 구성, **재질 이름**, 내보내기 경로.
2. Codex 실행: `codex exec -s workspace-write - < Art/tools/specs/<이름>_spec.md`
   - Codex가 `Art/tools/blender_<이름>.py`를 쓰고 블렌더(백그라운드)로 돌려 `Game/Assets/Art/Characters/Ch1/Kit_<이름>.fbx`를 내보냄.
   - 실행: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_<이름>.py`
3. Unity 빌더 표에 한 줄 추가: `Game/Assets/Editor/EnemyModelBuilder.cs`의 `Models` (원본 이름 · 목표 크기 · 정면 보정 · 크기 기준 · 출력 이름).
4. 메뉴 `CatTang/1장 적 프리팹 만들기 (Tripo)` → `Resources/Kit/Ch1/<이름>.prefab` (툰 재질 자동 변환).
5. 씬 · 방에 놓는 건 Unity MCP로 프리팹을 직접 배치 (런타임에 도형을 조립하지 않음).

좌표 규칙 (블렌더): Z가 위, **+Y가 정면**, 바닥 가운데가 원점. FBX 내보내기 옵션은 기존 명세(`kit_sandbomb_spec.md` 등)와 같게.

## 3. 회색 모델 + 코드 배색 (2026-11-17 확인: 가능)

앞으로 블렌더 모델은 **회색으로 뽑고, 색은 코드의 배색표에서 입힘**.

### 블렌더 + Codex 모델 — 바로 가능 (확인함)

- 재질을 **역할 이름**으로 나누고 색은 전부 회색(예: 0.5, 0.5, 0.5)으로 둠.
  - 예: `wood`, `wood_dark`, `iron`, `brass`, `gold_drip`, `canvas`, `cloth_red`, `paint_teal`, `roof_tin`, `rock`, `stone`, `window` …
- 재질 이름은 FBX를 거쳐도 그대로 남음 (`Kit_WoodGate` → wood · wood_dark · wood_light · iron · gold_drip 확인).
- 빌더가 프리팹을 만들 때 **회색 재질이면 이름으로 배색표 색을 찾아 넣음**:
  - 배색표: `Game/Assets/Editor/ModelPalette.cs` (`ModelPalette.Ch1`, 이름 → 색)
  - 블렌더 중복 접미사(`wood.001`)는 떼고 찾음.
  - 표에 없는 이름이면 회색 그대로 두고 콘솔에 경고 → 표에 추가.
  - 이미 색이 칠해진 예전 모델은 건드리지 않음 (회색일 때만 표를 씀).
- 색을 바꾸고 싶으면 배색표 값만 고치고 빌더를 다시 돌리면 됨. 모델을 다시 뽑을 필요 없음.
- 테스트: 회색 `wood` · `iron` 두 조각 모델을 빌더에 넣어 각각 나무 갈색 (0.55, 0.33, 0.19) · 쇠 (0.24, 0.22, 0.25)로 칠해지는 것 확인.
- 명세 쓸 때: 색 대신 "재질 이름 목록"만 적고, 새 이름을 쓰면 `ModelPalette.Ch1`에 색을 추가.
- 챕터마다 다른 배색이 필요하면 `ModelPalette`에 `Ch2` 같은 표를 더 만들면 됨.

### Tripo 모델 — 그대로는 안 됨, 방법은 있음

- Tripo 모델은 **재질 하나 + 구워진 색 텍스처 한 장**으로 들어옴 (예: `Ch1_Outlaw` → 재질 `Lit` 하나). 부위(옷 · 피부 · 금속)를 나눈 정보가 없어서, 회색으로 뽑으면 코드로 부위별 색을 줄 수 없음.
- 그래서 Tripo는 **지금처럼 플랫 컬러(명암 없는 고유색) 그림으로 생성**하고, 배색 조정은 셰이더로:
  - 지금 쓰는 방식: 툰 셰이더 **색 범위 치환**(`_Recolor*`) — 특정 색상 범위(예: 적갈색 몸)만 다른 색으로 돌림. 금빛 · 진홍 포인트는 유지. 적 배색 2안이 이 방식 (`Editor/EnemyPalette.cs`).
  - 필요해지면: 텍스처를 색별로 나눈 부위 마스크를 따로 만들어(텍스처 색을 3~4묶음으로 자동 분류) 부위마다 배색표 색을 입히는 방식도 가능. 아직 안 만듦.

## 4. 공통 마무리

- 재질은 빌더가 툰 셰이더(`CatTang/Toon`)로 바꿈. 명암 · 외곽선은 셰이더가 처리하니 모델에는 명암을 칠하지 않음.
- 배경 재질은 셰이더 `_Saturation` · `_Brightness`로 톤 조절 가능 (`Editor/EnvTone.cs`, 지금은 원래 값 1).
- 적은 빨간 바깥 테두리가 자동으로 붙음 (`EnemyModelBuilder.EnemyEdge`).
- 관련 파일: `Art/tools/specs/` (명세) · `Art/tools/blender_*.py` (Codex가 쓴 스크립트) · `Art/tools/tripo_import.py` (Tripo 가져오기) · `Game/Assets/Editor/EnemyModelBuilder.cs` (프리팹 빌더) · `Game/Assets/Editor/ModelPalette.cs` (배색표).

## 5. Tripo CLI로 캐릭터 만들기 (2026-11-17, 2장부터)

웹 대신 CLI(`tripo`, API 크레딧)를 씀. **H3.1 모델이 P2보다 훨씬 쌈** (이미지 생성 40 + 리깅 25 = 마리당 약 65 크레딧, P2는 생성만 100~130).

1. 원화: ChatGPT 웹에서 정면 전신 · A자 자세 · 밝은 회색 배경 · 명암 없는 플랫 컬러 시트(3종 한 장) → 화면 확대 캡처로 한 명씩 자르기
   → `python Art/tools/keep_main.py <캡처.png> Art/tripo_input/ch2/<이름>.png` (옆 캐릭터 조각 지움) → `python Art/tools/denoise.py ...` → `<이름>_dn.png`
2. 생성 + 리깅 (백그라운드로 여러 개 동시에):
   `tripo make <이름>_dn.png --model tripo-v3.1 -p texture=true -p texture_quality=detailed -p face_limit=12000 --then "rig-check,rig:model=rig-v3.0,spec=mixamo,out_format=fbx" --name Ch2_X -o Art/models/ch2/Ch2_X --json --yes --no-open`
3. 들여오기: `python Art/tools/tripo_cli_import.py Ch2_X` → `Game/Assets/Art/Characters/Ch2/Ch2_X.fbx` + FBX에 박힌 텍스처를 꺼낸 `Ch2_X_basecolor.jpg`
4. Unity: FBX를 Humanoid로(아바타 생성, 재질 가져오기 끔) → 메뉴 `CatTang/2장 프리팹 만들기 (Tripo + 블렌더)` → `Resources/Enemies/Ch2_*.prefab` · `Resources/Kit/Ch2/*.prefab`
- 잔액: `tripo balance`. 크레딧이 모자라면 Chrome에서 Tripo 웹(studio.tripo3d.ai)으로.

## 6. 배색 · 테두리 규칙 (2026-11-17)

- **맵 = 따뜻하게, 몬스터 = 차갑게.** 몬스터는 모든 챕터에서 '시간의 하수인' 공통 색(차가운 보라 · 청회색 + 놋쇠 + 진홍 포인트). 맵 배색표(`ModelPalette.ChN`)에는 보라 · 파랑을 쓰지 않음.
- **바깥 테두리 = 위협 신호.** 몬스터 = 빨간 바깥 테두리(`EnemyEdge`, 스텐실 4). 쏘면 터지는 물체 = 주황 ↔ 노랑 맥동 바깥 테두리(`Scripts/Game/DangerEdge.cs`, 스텐실 5). 맵 · 일반 소품에는 바깥 테두리 없음.
- 폭발물 목록은 빌더 `EnemyModelBuilder.Explosives` (프리팹에 `DangerEdge`가 붙음). 1장 화약통은 `PowderKeg.Make`가 붙임.
