# 핸드오프 — 연출 · HUD · 판 구조 · 컨셉 기획서 Ver02

마지막 갱신: 2026-11-16 (저녁)

이번 세션에서 바꾼 것을 주제별로 정리했다. 모두 플레이 모드에서 화면으로 확인했다(콘솔 에러 없음). 확인 못 한 것은 맨 아래 "남은 일"에 있다.

## 1. 판 구조 — 세피리아식 한 목숨 (사용자 결정)

- 한 판 = 한 목숨. 늘 **1장부터** 시작한다. 챕터를 고르는 화면은 없앴다.
- 보스를 넘으면 마을로 돌아가지 않고 **다음 장으로 이어진다** (체력 30% 회복, 골드 · 빌드 유지).
- 이번 판의 끝(`frontier`) = 지금까지 넘은 장 + 1. 새 장을 넘으면 판이 끝나고, 다음 판은 한 장 더 멀리 갈 수 있다. 5장을 다 넘은 뒤엔 매 판 1~5장.
- 시간의 문: 진행 목록(넘음 ✓ / 이번 목표 / 잠김) + "출발 — 1장부터" 버튼.
- 결과 화면: "N장에서 쓰러졌다" / "다음 판은 N장까지 갈 수 있다" / 시설 해금 안내.
- 코드: `RunManager.clearedAtStart · frontier · ChapterClear`, `GameUI.OpenChapterSelect · ShowResults`.

## 2. 불릿 파티(궁극기)

- **효과 적용:** 매 타 `FireEffects`(OnFire) + `ApplyHit`(OnHit · OnKill) → 활성화된 탄 · 특성 · 총 · 코스튬 효과 전부 적용 (기획서 문구 반영).
- **분신 (사미라 궁 · 불릿 파티 느낌):** `CloneStrikes` — 매 타 붉은 분신 4개가 **몸에서 대시해 나가며** 꼬리 잔상을 남기고(`Afterimage.DashIn`, 0.1초) 둘레 0.8~1.8m에 도착해 제각각 방향으로 쏘고 0.16초 뒤 사라짐. 첫 분신은 원 안의 적을 봄. 마무리는 8방향 1.8m로 뛰쳐나가며 일제 사격.
- **총구 폭발:** 3D 버전 (사용자 선택). 2D 가시 섬광 `FX/flash_0~2` + 코드로 구운 3D 가시 꽃잎 불꽃 원뿔 `FxMeshes.Flame` + 3D 불덩이 `FxMeshes.Ball`. 셰이더 `CatTang/ToonFire`(흰 → 노랑 → 주황 → 빨강 4단, 잉크 외곽선, `_Heat`로 식고 `_Dissolve`로 녹음). Codex 그림 프레임 버전도 남아 있다: `GunkataFx.UseDrawnBlast = true`.
- **파편 · 불티:** `BulletPartyZone` 파편 = Codex `FX/shards`(톱니 · 쇳조각 · 시곗바늘 · 시계 유리, 빙글빙글 돌며 떨어짐), 불티 = `FX/embers`(날아가는 방향으로 늘어나는 불꽃 줄기).
- **궁 준비 테두리 불길:** 열기 3칸이 차면 화면 가장자리에 Codex 손 그림 불길(`FX/edge/edge_0~2`)이 0.11초마다 번갈아 일렁임. 궁을 쓰면 꺼짐. `GameUI.UpdateHeatGlow`.

## 3. 사격 · 장전 · 대시

- **평소 사격:** 작은 총구 화염(Codex `FX/anim/flash_*` 4프레임 + 작은 3D 원뿔) `GunkataFx.MuzzleFlash`.
- **총알 궤적 (`Tracer`):** 빛 더하기 셰이더 `CatTang/AdditiveGlow`로 금빛 번짐 · 흰 심지 · 총구 → 목표로 날아가는 탄두 세 겹.
- **반동:** `KataPoser.Recoil(right)` — 쏜 팔이 20° 위로 튀었다 돌아옴(총은 팔과 거의 일직선). 평소 사격 · 백스텝 · 궁 매 타.
- **총 쥐는 위치:** `KataPoser.gripOffset` (0, -0.09, -0.02) — 손잡이가 앞발 안에 오게.
- **장전 (`ReloadMotion`, 플레이어에 자동 추가):** 총 세워 털기(빈 탄피 6개씩) → 허리 벨트 스피드로더에 찍어 넣기 → 빈 로더 양옆으로 던짐 → 철컥(섬광, 완벽 장전 구간 0.45~0.65) → 총 두 바퀴 돌려 쥠. 일찍 끝나면 철컥 + 한 바퀴.
- **닷지 액션 (대시로 공격을 직접 피함):** 시간 0.2배(실시간 0.3초 + 0.3초 회복) · 치명타 확률 **×1.5 (곱하기, 최대 100%)** 2.5초 · 푸른 화면 · "닷지! 치명타 ×1.5" 표시. `PlayerController.DodgeCritMul · DodgeCritTime · DodgeSlowScale`.
- **실루엣 색:** 붉은 계열 (`SilCrimson` 대시 · 분신 사격, `SilHot` 대시 끝 · 백스텝, `SilDodge` 닷지, `UltGhost` 궁 분신).

## 4. HUD

- **대시 칸 (세피리아식):** 체력 바 아래 Codex 대시 아이콘(`UI/dash_icon`) 3개. 찬 칸 = 원래 색, 빈 칸 = 어두운 회색, 차오르는 칸 = 아래에서 위로 색이 참, 다 차면 톡 튐. 50px · 그림자.
- **활성 특성 (`UI/TraitHud.cs`):** 특성마다 한 줄 = Codex 아이콘(`Icons/traits/trait_3001~3009`) · 이름 · 개수(특성 색) · 단계 칸 2/4/6(찬 단계 금색). 마우스를 올리면 툴팁(아이콘 · 단계 · 설명 · 단계별 효과 · 다음 단계까지 남은 개수). 단계는 1 · 2 · 3으로 표시.
- **보스 체력 흔적 바:** 같은 모양의 순백(`UIKit.WhiteSprite`).

## 5. 데이터 · 기획서

- **특성 문구 (`docs/tools/build_tables.py` → CSV · xlsx):** 지연 = 일정 시간 후 추가 피해, 예지 = 대시 회피 판정 증가, 미련 = 비활성화 탄에 보너스 효과. 수치는 그대로.
- **컨셉 기획서 Ver02 (19장):** `docs/1159_컨셉_기획서_20261116_정승운_Ver02.pptx`, 생성기 `docs/tools/build_concept_v2.js` (사용자 MD `docs/11_59_컨셉 기획_정승운_20261116.md` 기준). 이미지 `docs/images/v2/` — ChatGPT 생성 6장, 레퍼런스 `ref/`(Steam 스크린샷 · 젠존제 · 달리 · 세피리아 코스튬), 인게임 캡처, 특성 아이콘.
  ```bash
  NODE_PATH=<pptxgenjs가 있는 node_modules> PPTX_SKILL=<pptx 스킬 폴더> node docs/tools/build_concept_v2.js
  ```

## 그림 만드는 법 (Codex)

- 지시문 · 원본: `docs/images/fx_src/prompts/*.txt`, `docs/images/ui_src/prompts/*.txt`. 마젠타 바탕으로 그리게 해서 투명 처리한다.
  ```bash
  codex exec "$(cat prompts/<이름>.txt)" -s workspace-write -c approval_policy=never --skip-git-repo-check -C <폴더> -i prompts/ref_style.png
  ```
- 자르기: `fx_sheet_cut.py`(프레임 시트 → `FX/anim`), `fx_strip_key.py`(시트 통째 → 파티클 텍스처), `fx_edge_cut.py`(테두리 불길), `ui_dash_cut.py`(대시 아이콘). 특성 아이콘은 3x3 시트를 잘라 `Icons/traits`.
- 새 텍스처는 Unity에서 alphaIsTransparency · 밉맵 끔 · Clamp · 무압축(또는 HQ)으로 설정.
- Python: `C:\Users\user\AppData\Local\Programs\Python\Python312\python.exe` (Pillow · numpy · openpyxl · python-pptx).

## 주요 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Game/PlayerController.cs` | 궁(`BulletParty` · `UltVolley` · `CloneStrikes` · `CloneFire`), `FireEffects`, 닷지(`JustDodge` · `DodgeSlow`), 실루엣 색 |
| `Scripts/Game/RunManager.cs` | 판 구조 (1장부터 · frontier · 장 이어가기) |
| `Scripts/Game/GunkataFx.cs` | 총구 폭발 · 평소 총구 화염 · `FxFlipbook` · `FxMeshes` · `FxMesh` |
| `Scripts/Game/Tracer.cs` | 총알 궤적 세 겹 |
| `Scripts/Game/KataPoser.cs` | 자세 · 반동 · 쥐는 위치 · `Pose` · `gunExtraR/L` |
| `Scripts/Game/ReloadMotion.cs` | 스피드로더 장전 · `Tumble`(던져진 물건) |
| `Scripts/Game/Afterimage.cs` | 실루엣 · `DashIn`(움직이는 분신 + 꼬리) |
| `Scripts/Game/BulletPartyZone.cs` | 궁 판정 원 · 탄피 · 파편 · 불티 |
| `Scripts/UI/GameUI.cs` | 대시 칸 · 닷지 표시 · 궁 준비 불길 · 보스 바 · 시간의 문 · 결과 |
| `Scripts/UI/TraitHud.cs` | 활성 특성 목록 · 툴팁 |

## 남은 일 · 확인 안 된 것

- **새 시간의 문 화면 · 쓰러졌을 때 결과 화면은 직접 띄워 보지 않았다.** (장 이어가기는 확인함)
- 궁 불티(embers)가 날아가는 방향으로 제대로 향하는지 확대해서 보지 않았다.
- 총 모델은 `gun_2001` 하나뿐 — 총을 바꿔도 모델이 안 바뀐다. 사용자 결정: 적 모델을 Tripo로 다 뽑은 뒤 토큰이 남으면 총마다 모델 생성. (Tripo 크레딧이 지금 없음)
- 한 타에 이펙트 오브젝트가 많다(본체 + 분신 4 × 원뿔 · 불덩이 · 선 · 꼬리 잔상). 저사양 프레임 확인 필요. 필요하면 풀링하거나 `UltClones`를 줄인다.
- 밸런스: 닷지 ×1.5 · 2.5초, 불릿 파티 0.9배 × 12타 · 무적 · 6.5m, 장 사이 회복 30%는 플레이 감으로 조정 필요.
- 기획서 레벨 디자인 슬라이드에 새 판 구조(한 목숨 · 점점 멀리)는 아직 안 넣었다.

## 커밋하지 않은 것 (의도적으로 제외)

사용자가 따로 작업 중인 파일이라 건드리지 않았다.

- 기존 기획서 `docs/Cat_Tang_*.pptx` · `docs/Cat_Tang_목록_초안.md`, Ver01 `docs/1159_..._Ver01.pptx`, 사용자 MD `docs/11_59_컨셉 기획_..._20261116.md`
- `docs/tools/build_ppt.js` · `build_item_ppts.js`
- `docs/.obsidian/`
- `Game/ProjectSettings/EditorBuildSettings.asset` · `ShaderGraphSettings.asset` · `Game/Assets/Settings/Mobile_RPAsset.asset`
