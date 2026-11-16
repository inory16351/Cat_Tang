# 핸드오프 — 불릿 파티(건카타 궁) 연출 작업

마지막 갱신: 2026-11-16

## 지금 상태

Q 불릿 파티는 아래처럼 동작하고, 플레이 모드에서 화면으로 확인했다(콘솔 에러 없음).

- **게임은 멈추지 않는다.** 타임스케일을 건드리지 않는다. 궁을 쓰는 동안 무적이며 WASD 이동(90% 속도)과 대시가 된다. 사격 · 총 스킬 · 상호작용은 막힌다.
- **판정:** 플레이어를 따라다니는 반지름 6.5m 원형 트리거 콜라이더(`BulletPartyZone`) 안의 적 전부가 2초 동안 12번 피해를 받는다. 한 타 피해는 그 칸 탄 피해 × 0.9이며 치명타와 탄 적중 효과가 적용된다. 궁 피해로는 열기가 차지 않는다.
- **연출:**
  - 몸 중심을 축으로 초당 900°로 계속 돌며 건카타 8자세를 홱홱 넘어간다.
  - 매 타마다 금빛 실루엣 분신 4개가 본체 사이사이 방향(1.6m)에 나타나고, 본체와 같은 순간에 사방으로 총구 폭발을 일으킨다.
  - 총구 폭발은 참고 영상(나무위키 mp4) 방식이다. 둥근 불덩이 뭉치가 터졌다가 그을린 갈색으로 식고, 흰 빛줄기와 붉은 흩뿌림이 뻗는다. 가운데에 회색 연기가 쌓이고, 어두운 소용돌이가 돌며, 파편 · 불티 · 탄피가 튄다.
  - 화염 방향은 항상 몸 바깥쪽이다.
  - 범위는 원 안에 떨어지는 총알 비(탄착 불꽃)로 보인다. 원 경계 이펙트 · 바닥 표시 · 금빛 고리는 없앴다(사용자 요청).
  - 마무리는 8방향 분신 일제 사격 뒤 연기와 소용돌이만 남아 흩어진다.

## 이번 작업에서 함께 바뀐 것 (커밋 f8e9303)

- **회전축:** CatHero 애니메이션의 루트 XZ 기준을 Original에서 Center of Mass로 바꿨다(`CatHero.fbx` 임포트 설정). 달리기 클립이 몸을 원점보다 0.73m 앞에 두어 회전축이 몸 밖에 있던 문제를 고쳤다.
- **대시 HUD:** 이미지 대신 `>>` 기호 3칸과 충전 바로 표시한다.
- **아이콘:** 총 · 탄 아이콘 55개의 남색 배경을 투명으로 바꿨다. 상점 · 보상 · 총 줍기 · 총포상 · 탄 강화 화면의 네모 틀을 뺐다.
- **아이콘 스크립트:** `slice_icons.py` · `icon_toon.py`가 투명 배경 RGBA로 저장한다. HUD 도구가 쓰는 `ui` 모드는 예전처럼 RGB다.

## 주요 파일

| 파일 | 역할 |
|---|---|
| `Game/Assets/Scripts/Game/PlayerController.cs` | `BulletParty()` 코루틴, `UltVolley` 한 타, `CloneVolley` 분신, `GunBlastAt` · `OutwardDir` 총구 폭발 방향, 궁 중 회전(`Update`) |
| `Game/Assets/Scripts/Game/BulletPartyZone.cs` | 원형 트리거 콜라이더 판정, 탄피 · 파편 파티클 |
| `Game/Assets/Scripts/Game/GunkataFx.cs` | 총구 폭발 연출: `Burst`(불덩이 뭉치 + 빛줄기 + 흩뿌림) · `Smoke` · `Swirl`, `FxSprite` · `FxLine` |
| `Game/Assets/Scripts/Game/Afterimage.cs` | 실루엣. `SpawnAt(model, pos, rot, …)`로 방향을 지정할 수 있다(분신용) |
| `Game/Assets/Resources/FX/` | `fire_puff_0~2`, `smoke_0~1`, `casing` 스프라이트 |
| `docs/tools/fx_sprites.py` | 위 FX 스프라이트 생성기. 실행하면 `Resources/FX`에 덮어쓴다 |

## 조절 값

- **`PlayerController`:**
  - `UltDuration` 지속 시간
  - `UltRadius` 범위
  - `UltTickMul` 한 타 피해
  - `UltMoveMul` 이동 속도
  - `UltSpinSpeed` 회전 속도
  - `UltClones` 분신 수
  - `UltCloneDist` 분신 거리
  - `UltRainPerTick` 총알 비 수
- **`GunkataFx.Burst`:**
  - 불덩이 크기 `1.7f - 0.3f * i`
  - 간격 `0.3f + 0.5f * i`
  - 빛줄기 길이 · 굵기 `3.0f` · `0.2f`
- **`FxLine.Arc`:** 소용돌이 굵기 `0.12~0.2`

## 도구

- **Python 3.12:** Pillow · numpy · scipy를 이 PC에 설치했다. `python` 명령은 아직 Microsoft Store 바로가기라서 전체 경로로 실행한다.
  ```bash
  C:\Users\user\AppData\Local\Programs\Python\Python312\python.exe docs/tools/fx_sprites.py
  ```
- 새 텍스처는 Unity에서 alphaIsTransparency, 밉맵 끔, Clamp, 무압축으로 설정해 두었다. 새로 추가한 텍스처에도 같은 설정이 필요하다.

## 남은 일 · 확인 안 된 것

- 상점 · 보상 · 총 줍기 화면에서 아이콘 틀을 뺀 결과는 직접 열어 보지 않았다.
- 금빛 분신 실루엣이 조금 옅다. 더 진하게 하려면 `CatTang/Silhouette` 셰이더 색이나 `PlayerController.UltGhost` 알파를 조정한다.
- 한 타에 이펙트 오브젝트가 많이 생긴다(본체 · 분신 10발 × 뭉치 · 선). 저사양에서 프레임을 확인해야 한다. 필요하면 풀링하거나 `UltClones`를 줄인다.
- 평소 사격(좌클릭)에는 새 총구 폭발을 넣지 않았다. 건카타 궁에만 들어 있다.
- 불릿 파티 밸런스(피해 0.9배 × 12타, 무적, 6.5m)는 플레이 감으로 조정이 필요하다.

## 커밋하지 않은 것 (의도적으로 제외)

사용자가 따로 작업 중인 파일이라 건드리지 않았다.

- `docs/*.pptx` · 기획 문서 `.md`
- `docs/tools/build_ppt.js` · `build_item_ppts.js`
- `docs/.obsidian/`
- `Game/ProjectSettings/EditorBuildSettings.asset` · `ShaderGraphSettings.asset`
