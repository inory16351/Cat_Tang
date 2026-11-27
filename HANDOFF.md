# 핸드오프 — 1장 적 · 보스 · Tripo 모델 · 리깅 · 적 탄 이펙트

마지막 갱신: 2026-11-24 (코스튬 — 위 "2026-11-24 (1)~(4)" 절). 그 전: 2026-11-22 (쌍둥이 연출 · 눈빛 꼬리 · 시침 걷기 — "2026-11-22 (5)" 절; 그 앞 연출 효과음 "(4)" 절, 위에서부터 2026-11-22 (1)~(4)). 그 전: 2026-11-21 (보스 등장 연출 · 보스 이름/칭호 — "2026-11-21" 절). 그 전: 2026-11-20 오후 (남은 보스 전부 — 4장 시침 · 분침 · 5장 거울 고양이 · 집행관 기믹 · 5장 천문대 소품 · 고래 효과음, "2026-11-20 (오후) — 남은 보스 전부" 절, 다음 = 그 절 "▶ 다음 작업"). 그 전: 2026-11-20 (5장 보스 시간의 주인 · 스타일 VFX(블렌더 영상 기법) · 바닥 가시성 · 보스 강화 부품 — "2026-11-20 — 5장 보스" 절). 그 전: 2026-11-19 밤 끝 (5장 구현: 적 6종 행동 · 전용 모션 · 방 기믹 허공의 균열 · 깨진 바닥 v2 · Codex + 블렌더 이펙트 · 효과음 16종 · Gemini 곡 3 — "2026-11-19 (밤 끝) — 5장" 절). 그 전: 2026-11-19 밤 늦게 (4장 적 행동 · 모든 적 스킬별 전용 모션 · 앞뒤 근육 방향 바로잡기 · 무기 자세 IK · 적 스킬 효과음 — "2026-11-19 (밤 늦게)" 절, 다음 = 그 절 끝 "▶ 다음 작업"). 그 전: 2026-11-19 밤 (4장 적 6종 모델 · 무기 따로 — "4장 적 모델" 절). 그 전: 2026-11-19 저녁 (4장 기획서 PPT · 4장 키트 · 느와르 · 레이저 이펙트 — "2026-11-19 (저녁)" 절). 그 전: 2026-11-19 오후 (4장 기획 확정 · 방 크기 장/스테이지별 · 스테이지별 문 · 복도 완료 — "2026-11-19 (오후)" 절). 그 전: 2026-11-19 (시설 화면 스크롤바 그림 연결 · 빌드 표 복사 — "2026-11-19 — 윈도우 빌드" 절). 그 전: 2026-11-18 밤 (마을 오브젝트 정리 뒤 씬 다시 지음 · 확인 — "(마지막, 다른 세션에서 이어서)" 항목). 그 전: 2026-11-18 밤 (새 마을: 다리로 이어진 섬 5개 · 장 해금 · NPC 5 — "2026-11-18 (밤 2) 새 마을" 절). 그 전: 2026-11-18 밤 늦게 (아웃게임 개편 · 메인 로비 · 마을 — "2026-11-18 (밤 늦게)" 절). 그 전: 2026-11-18 저녁 (3장 맵 키트 · 거울 반사 바닥 — "2026-11-18 (저녁)" 절). 그 전: 2026-11-18 오후 (상인 · 추억 · 3장 구현 시작). 그 전: 2026-11-18 (3장 레벨 디자인 확정 · 3장 기획서 Ver01 — 구현 전, 아래 "3장 구현 할 일" 참고). 이전 핸드오프(연출 · HUD · 판 구조)는 git 기록 `459df5c`의 HANDOFF.md 참고.


## 2026-11-27 (2) — 새 기획서 4종 완성 · 보스 패턴 연습 연결 · 말투 교정 (커밋 안 함) ← 여기서 이어서

- **보스 패턴 연습 연결**: `RunManager.EnterDoor` 스토리 맵 5개 분기 뒤에 `StoryPractice.Pending(chapter)` → `StoryPractice.Play(this, chapter, ...)` 분기 추가. 판마다 장별 1회(`practicedMask` 비트). **Unity가 꺼져 있어 컴파일 / 플레이 확인 안 함** — 켜서 콘솔 확인 + 설정 체크 후 보스 문 진입 시험 필요.
- **성장 시스템 Ver01 수정 끝**: 6 / 9 / 11 / 14쪽 겹침, 3 / 7쪽 빈 카드(요약 카드 추가), 5쪽 소제목 굵기(`b: true`), 문구 전체 한자어 교정. 파일명 그대로 Ver01(처음 완성본).
- **새 기획서 3종** (빌더 = `docs/tools/build_tutorial_ppt.js` / `build_ui_ppt.js` / `build_bossintro_ppt.js`, 실행 `NODE_PATH=./node_modules node <파일>`):
  - `1159_튜토리얼_기획서_20261127_정승운_Ver01.pptx` 17쪽 (판 1 / 기능 잠금 / 판 2 안내 / 연산 / 극한 회피 / 마을 안내 / 보스 패턴 연습 / 저장 키)
  - `1159_UI환경설정_기획서_20261127_정승운_Ver01.pptx` 17쪽 (화면 흐름 / 로비 / 마을 / HUD / 판 내 창 / 대화창 / 설정 4탭 / 조작 / 언어)
  - `1159_보스등장연출_기획서_20261127_정승운_Ver01.pptx` 14쪽 (의도 / 보스 목록 / 공통 흐름 / 전환 / 정지 카드 / 카드 10종 / 장별 2명씩 / 소리 / 설정 / 건너뛰기). 그림 = `docs/images/boss_intro/strip_<id>.jpg`(예전 GIF에서 4컷, `git show 9dfecfe4^:docs/images/boss_intro/boss_intro_<id>.gif`) · `card_<id>.jpg` · `freeze_row.jpg`. 크로노 & 템포 이름은 현재 이름 그대로 씀(사용자 확인 대기).
- **말투 교정**: 16종 전체 문단을 로컬 정규식으로 훑어 고유어 / AI 말투 35곳을 한자어로 교정(pptx 직접 + 빌더 JS 원문도 같이). 게임 속 대사 / UI 원문(따옴표 안, 대장장이 대사, 의상실 안내 문구, 스토리 주제문)은 그대로.
- **안티그라비티(agy) 검수는 못 함**: 기획서 텍스트를 agy로 보내는 명령이 Claude Code 자동 권한 검사에서 막힘(외부 전송). 도구는 만들어 둠: `docs/tools/deck_review_agy.py <work> extract|review|apply [덱,덱] [workers]` — 사용자가 권한을 허용하면 `review` → `apply`로 이어서 가능.
- 렌더 확인 스크립트: 스크래치패드 `render.ps1`(PowerPoint COM, 10줄) / `sheet.py`(접촉 시트). 재부팅 시 사라지므로 위 절 설명대로 새로 만들 것.
- **남은 것**: ① Unity 컴파일 + 보스 패턴 연습 / 첫 세션 코드 5개 플레이 확인 ② agy 검수(권한 허용 시) ③ HUD 도움말 / "E" 표시 키 바인딩 미반영 + "Esc 포기" 문구 ④ 판 1 Esc 포기 시 튜토리얼 전체 생략 ⑤ 로비 설정 창 제목 겹침.

## 2026-11-27 — 기획서 최신화 · 새 기획서 4종 (→ 위 (2)에서 이어서 끝냄)

### 사용자 요청 (원문 요약, 2026-11-27 오전)
- 게임과 기획서가 다른 부분 확인해서 최신화 + 새 기획서 제작. 말투는 사용자가 쓴 다른 기획서와 비슷하게, AI 말투 안 느껴지게 교정. **최종 검수는 안티그라비티 CLI(`agy`)에 맡겨 검수 + 텍스트 수정까지 명령**. 중간점(·) 금지 → `/`로. 레이아웃은 기획서마다 맞게 바꿔도 됨. 애매하면 질문.
- 필요한 이미지는 크롬(Claude in Chrome)으로 ChatGPT에서 생성. 코드 수정이 필요한 부분도 진행.
- 사용자가 고른 답: 새 기획서 4종 전부(튜토리얼 / 성장 시스템 / UI 환경설정 / 보스 등장 연출, 스토리 맵 1~5 / 엔딩은 스토리 기획서에) · 총탄 특성 단계 = **교체**(기획서 유지 → 코드를 고침) · 기본 총 8종 개별 스킬 = 개념만 설명 · 미구현 연출 4개 = 기획서에 남기고 이번에 구현함 · 평행세계 = 코드대로(전설 / 유도) · 증기 돌진 = 8m · 쓰러짐 대사 15줄 / 극한 회피 튜토리얼 문구 = 초안대로 확정.
- **추가 말투 규칙 (2026-11-27 사용자)**: 풀어쓴 고유어 대신 한자어 기획 용어. 예) 열림 → 활성화, 얻는 곳 → 획득처, 쓰는 곳 → 사용처, 사라짐 → 소멸 / 초기화, 고름 → 선택, 사기 / 팔기 → 구매 / 판매, 바로 → 즉시, 넘친 만큼 이어짐 → 초과분 이월, 빨려 옴 → 흡수, 바뀜 → 변경. 기존 규칙(명사로 끝내는 개조식 / 나열 " / " / 중간점 금지)과 함께 **모든 기획서(기존 최신화한 것 포함)와 agy 검수 지시에 적용**.

### 된 것 (첫 세션, 사용량 한도로 중단)
- **코드** (Unity 컴파일 오류 0, **플레이 확인 안 함**): 총탄 특성 단계 교체(가장 높은 단계만 적용, 툴팁도 그 단계만 밝게) · NPC 사물 감정(뻐꾸기 깃털 / 문지기 랜턴 / 재봉사 실, 재봉사 · 문지기 대사에 감정 태그) · 첫 전투 시작 총 돌려 뽑기 1초 · 극한 회피 튜토리얼(첫 주황 십자, 판 2 중간 보스부터 또는 판 3 이후) · 쓰러짐 반응 대사(마을 뻐꾸기, 15줄).
- **기존 기획서 새 버전 (파일명 20261127)**: 총탄 Ver05 / 총 Ver04 / 컨셉 Ver04 / 공통 레벨디자인 Ver04 / 1장 Ver03 / 2장 Ver05 / 3장 Ver02 / 4장 Ver03 / 5장 Ver02 / 코스튬 Ver05 / 마을 Ver02 / 스토리 Ver03(34쪽). 빌더 = `docs/tools/build_*_ppt.js`, 고친 스크립트 `edit_items_ver05.py` · `edit_concept_ver04.py` · `edit_levels_1127.py` · `deck_ops.py`.
- **인게임 캡처 27장**: `docs/images/ui_cap/` (lobby_ko · slots · village · gate · fac_* · hud3 · inventory · enhance · levelup · shop · map · pause · results · reward_doors · next_room-1 · settings_0~3 · tut_clock · tut_cond1 · tut_op3 · tut_dodge · fac_memory_tut).
- **성장 시스템 기획서 Ver01**: `docs/tools/build_growth_ppt.js` 실행해 `docs/1159_성장시스템_기획서_20261127_정승운_Ver01.pptx`(14쪽) 생성됨. **아직 고칠 것**:
  - 레이아웃 겹침: 6쪽 보상 문 표(7줄)가 아래 글머리와 겹침 → rowH 0.44로 / 9쪽 조각 표가 글머리와 겹침 → rowH 0.56 / 11쪽 노드 표 4층 줄이 각주와 겹침 → rowH 0.9, 각주 y 6.5 / 14쪽 합계 표가 아래 카드와 겹침 → rowH 0.55, 카드 y 6.1. (`D.table`은 줄마다 rowH + 0.05)
  - 3쪽 / 7쪽 카드 아래가 텅 빔 → 카드 높이 줄이고 요약 한 줄.
  - 5쪽 글머리 소제목이 안 굵어짐: 스크립트가 `{ t, head: true }`를 쓰는데 `D.bullets`는 `b: true`를 봄 → `b`로.
  - 문구 전체를 위 한자어 규칙으로 다시 씀(예: "1장 첫 클리어 때 열림" → "1장 최초 클리어 시 활성화", "판이 끝나면 사라짐" → "판 종료 시 소멸").

### 남은 것 (순서대로)
1. 성장 시스템 기획서 위 수정 → 다시 빌드 → 렌더로 확인.
2. **튜토리얼 / UI 환경설정 / 보스 등장 연출 기획서** 새로 만들기 (빌드 틀 = `build_growth_ppt.js`처럼 `build_level_ppts.js`의 `makeDeck`, `D.sep = " / "`). 내용은 코드에서 확인: 튜토리얼 = `StoryHints` · `OpHints` · `DodgeHints` · `InventoryScreen.TutAllow` · 기능 잠금 · `StoryPractice` / `GameSettings.BossPracticeCh` (위 2026-11-26 (4) 절에 정리 있음), UI 환경설정 = `SettingsUI` · `GameSettings` · `KeyBinds` · 로비 / HUD / 인벤토리 / 미니맵 / 결과 화면 (2026-11-26 (5)(6) 절), 보스 등장 연출 = `BossIntro.cs` · `BossIntroCards` · `docs/images/boss_intro` (2026-11-21 ~ 11-22 절). 필요한 그림은 캡처 우선, 없으면 크롬 ChatGPT로 생성.
3. AI 말투 / 중간점 / 한자어 교정: 첫 세션이 다 못 본 코스튬 · 마을 · 1~5장 · 공통 기획서 + 새 4종. 중간점은 빌드 때 `slash_dots.py`가 자동으로 바꿈.
4. 안티그라비티 검수: `agy` CLI(번역 검토에 쓴 `Art/tools/loc_agy_review.py` 참고)로 기획서 텍스트 추출본을 넘겨 검수 → 지적 반영 → 텍스트 수정까지.
5. 코드 변경 플레이 확인(특성 교체 툴팁 / 사물 감정 / 총 돌려 뽑기 / 극한 회피 튜토리얼 / 쓰러짐 대사).
6. 따로 발견한 것: 로비 설정 창 제목 "설정"이 탭과 겹침(안 고침).

### 참고
- **보스 등장 연출 기획서 자료는 조사 끝**: `docs/research_boss_intro_1127.md` (보스 10명 이름 / 칭호 / 카드 10종 / 샷 목록과 초 / 공통 흐름 / 건너뛰기 / 설정 연동). 크로노 / 템포 이름은 사용자 확인 전이니 기획서 만들 때 질문. `docs/images/boss_intro`는 비어 있음 → `git show 9dfecfe4^:docs/images/boss_intro/<파일>`로 GIF 복구하거나 카드 그림 `Resources/UI/BossIntro/*.png` 사용.
- **튜토리얼 / UI 환경설정 자료도 조사 끝**: `docs/research_tutorial_1127.md` · `docs/research_ui_settings_1127.md` (안내 문구 원문 / 키 / 해금 시점 / 설정 항목과 기본값 / 키 바인딩 / 화면 크기 / 맞는 캡처 그림).
- **조사 중 나온 코드 문제 (사용자에게 알릴 것)**: ① 보스 패턴 연습 설정이 연결 안 됨 — `StoryPractice.Pending()` / `Play()`를 아무도 호출하지 않음(`RunManager.EnterDoor`는 `StoryMap.Pending`만 봄) ② HUD 도움말 줄 · "E" 상호작용 표시가 키 바인딩을 무시하고 기본 키로 박혀 있음 + "Esc 포기" 문구 남음 ③ 언어 목록 8개 중 사전은 en / zh만 있음 ④ 판 1에서 Esc 포기하면 튜토리얼 전체를 건너뜀 ⑤ 시너지 안내 문구 "한 단계씩 강해져요"가 특성 '교체' 규칙과 맞는지 확인.
- 슬라이드 렌더(PowerPoint COM): `render.ps1 -pptx <파일> -out <폴더> [-slides "3,4"]` → `$p.Slides.Item(i).Export(..., "PNG", 1600, 900)`. 스크래치패드에 있던 스크립트라 새로 만들어야 함(10줄짜리).
- Unity가 DX12 드라이버 크래시로 한 번 꺼져 D3D11로 다시 켰음(재시작 전까지 D3D11). 게임 언어 설정은 한국어로 되돌려 둠. 시험 슬롯 3 삭제, 활성 슬롯 1.

## 2026-11-26 (6) — 환경설정 4탭 · 설정 연결 · 번역 검토(안티 그라비티) (커밋 안 함 · Unity 컴파일 OK · 플레이로 Esc 창 4탭 English 확인)

- **설정 그림**: `Resources/UI/Settings/*.png` 12장 .meta = Sprite · 밉맵 끔 · 9-slice 테두리(`Art/tools/specs/ui_settings_borders.json` 값). 코드는 `SettingsUI.Art(name)` = `Resources.Load<Sprite>("UI/Settings/"+name)`로 테두리 그대로 씀.
- **`SettingsUI` 4탭**(로비 설정 · Esc 창 같이): 소리(전체 · 음악 · 효과음 · 창 밖 소리 끄기) · 화면(창 모드 3종 · 해상도 ◀▶ · 수직 동기화 · 흔들림 · 번쩍임 줄이기 · UI 크기 80~130%) · 게임(언어 ◀▶ · 대사 자동 넘김 · 글자 속도 느림/보통/빠름/바로 · 조준선 모양 기본형/십자/점 · 조준선 크기 · 보스 패턴 연습 1~5장 체크) · 조작(`KeyBindsUI`, 줄 = row 그림 · 키캡 9-slice). 마지막 탭 기억. Esc 창은 1180 × 900, 설정 칸 1060 × 500으로 키움.
- **`GameSettings` 새 값**: `WindowMode`(0 전체 · 1 창 · 2 테두리 없음, 예전 fullscreen 이어받음, `Fullscreen`은 호환용) · `ShakeScale` · `ReduceFlash`/`FlashScale`(0.3) · `UiScale` · `DialogueAuto` · `TextSpeed`/`TextDelayScale` · `CursorStyle`/`CursorSize` · `MuteUnfocused` · `BossPracticeCh(ch)`/`SetBossPracticeCh`(예전 `BossPractice` 하나 → 장별, 처음엔 예전 값 이어받음) · `Changed` 이벤트.
- **게임 연결**(서브에이전트): 흔들림 = `CameraFollow.Shake` · `BossIntro.Shake` · `BossIntroCards.Quake` · `StoryMontage` · `StoryOverlay` 흔들림 · 로비 도장. 번쩍임 = `BossIntro.Flash` · `StoryMontage` 흰/반전 번쩍 · 글리치 빈도 · `ScreenFx` 피격 · 극한 회피 · `Health`(플레이어만) · `Afterimage.Spawn` · 4장 장식 글리치(**예고 잔상 `Glitch.Ghost`는 그대로** — 읽혀야 함). UI 크기 = 새 `UI/UiScaleFollow.cs`(게임 · 로비 · 지도 · 가장자리 표시 · 길 안내 · 연습 표시 캔버스, 연출 캔버스는 뺌). 대사 = `GameUI` 글자 속도 42자/초 ÷ 배율, 자동 넘김 1.2초 + 0.05초/자. 조준선 = `AimCursor`(1 = 가는 십자, 2 = 빨간 마름모만). 창 밖 소리 = 새 `Core/FocusMute.cs`. **안 함**: 대화 초상화 흔들림(`sl.shake`)은 흔들림 설정 미적용.
- **5장 스토리 맵 연습 모드**: `StoryWard.Play(rm, done, practice)` 추가(이전 세션부터 컴파일 오류였음) — 대사 · 침대/사신 연출 · 끝(벽시계 쏘기) 건너뛰고 ①②③ 기믹만 → 보스 방, 기록 안 함.
- **번역**: 표정 토큰 10개 `loc_extract.py` KEEP로 뺌. 새 설정 문구 27개 번역. **안티 그라비티(`agy` CLI, Gemini 4 Neon) 전체 검토** 1,707개 → 지적 76건, 72건 반영(4건은 용어 통일로 이미 같음, '한 번 더 = 지움'은 "Click again to delete"로 직접). 용어 통일: 열기 칸 = bars · Effect proc chance · 꿈 제목 '마지막 장' = The Final Chapter · 4장 '그 사람' = she(4장 기획 '그녀') → `docs/loc_glossary.md` 끝 표. `--check` = 오류 0 · 빠짐 0 · 남음 0.
  - 검토 스크립트: `Art/tools/loc_agy_review.py <round> <entries.json> [batch] [workers]` (entries = en.json의 entries 목록) → 스크립트 옆 `review/<round>/result_*.json` (지적 id · 종류 · 제안 영어). 결과는 스크립트 옆에 쌓이니 스크래치패드로 복사해 돌릴 것.

### ▶ 다음 작업
1. 플레이로 확인 못 한 것: UI 크기 바꿔 HUD · 대화창 · 지도, 조준선 모양 1 · 2 모양, 대사 자동 넘김 · 글자 속도, 창 모드 · 해상도(빌드에서만 의미), 번쩍임 줄이기 체감, 창 밖 소리, 장별 보스 연습(5장 병실 연습 포함).
2. English로 로비 · 마을 · 판 · 대화 화면 돌며 `CatTang/번역/못 찾은 문구 저장`으로 빠진 문구 모으기 · 긴 영어 넘침 확인.
3. 로비 설정 창(1060 × 480)에서 4탭 배치 눈으로 확인(Esc 창만 봄).

## 2026-11-26 (5) — 환경설정 · 영어 번역 (→ 위 (6)에서 이어서 끝냄)

### 지금 된 것
- **번역 틀** (이전 세션에서 만듦): `Scripts/Core/Loc.cs`(`Loc.T` — 통째로 → 줄마다 → ' · ' 조각 → {n} 틀 → 숫자 틀, 못 찾으면 `Loc.Misses`) · `Scripts/UI/LText.cs`(Text 대신 붙여 언어 바뀌면 다시 번역, `UIKit.Label` 등이 씀) · `Editor/LocMenu.cs` 메뉴 `CatTang/번역/못 찾은 문구 저장 · 언어: 한국어/English`. 추출기 `Art/tools/loc_extract.py` → `Art/tools/specs/loc_sources.json`(1,717개), 용어집 `docs/loc_glossary.md`.
- **영어 사전 새로 만듦**: `Game/Assets/Resources/Lang/en.json` 1,707개 — 서브에이전트 5개가 용어집 따라 나눠 번역 → 합침. `python Art/tools/loc_extract.py --check` = **오류 0 · 빠짐 10 · 남음 0**. 빠진 10개(기본 · 두통 · 결의 · 의문 · 씩 · 놀람 · 쓸쓸 · 다정 · 엄숙 · 의미심장)는 `GameUI.cs:484` `ExprId`의 표정 토큰(화면에 안 나옴)이라 일부러 뺌 → **loc_extract.py의 KEEP/제외에 넣어 빠짐 0으로 만들 것**.
- 조각별 원본 · 번역 스크립트는 스크래치패드(임시, 재부팅 시 사라짐): `.../scratchpad/loc/en0~4.json` · `chunk0~4.json`. 사전은 이미 프로젝트에 있으므로 필요 없음.
- **환경설정 그림**: Codex가 만든 원본 `Art/tools/specs/ui_settings_raw/*_raw.png` → 후처리 `Art/tools/ui_settings_post.py` → `Game/Assets/Resources/UI/Settings/` 12장(tab · tab_on · keycap · keycap_listen · row · settings_frame · icon_sound/display/game/keys · arrow_left/right). 9-slice 테두리 `Art/tools/specs/ui_settings_borders.json`(L,B,R,T: tab 64,36,64,64 · keycap 48 · row 48,47,48,47 · frame 96). **아직 안 함**: 최종 그림 눈으로 확인(접촉 시트 만들다 멈춤), Unity 임포트 설정(Sprite · 9-slice border 적용 — .meta 없음).
- **코드만 있고 창엔 없음**: `GameSettings`에 효과음 음량(`SfxVolume`) · 해상도(`Resolution`) · 수직 동기화(`VSync`) · 보스 패턴 연습(`BossPractice`, 전체 켬/끔 하나 — `StoryPractice.cs:31`이 씀). 단축키 `Core/KeyBinds.cs`(Act 10개, 마우스 버튼 지정, 겹치면 맞바꿈) · `UI/KeyBindsUI.cs`(키캡 그림 `UI/Settings/keycap(_listen)` 사용) — **어느 창에도 아직 안 붙음**. `SettingsUI.Build`는 아직 평평한 4줄(전체 · 음악 음량 · 전체 화면 · 언어), 로비(`LobbyScreen.cs:457`)와 Esc 창(`GameUI.cs:1032`)이 같이 씀.

### 사용자가 고른 추가 기능 (2026-11-26 확정)
- 소리 · 화면: **화면 흔들림 세기** · **번쩍임 · 화면 효과 줄이기**(피격 번쩍 · 글리치 · 잔상 약하게) · **창 모드 3종**(전체 화면 · 창 · 테두리 없는 창 — 지금 전체 화면 켬/끔 대체)
- 게임: **대사 자동 넘김 · 글자 속도** · **UI 크기**(HUD · 대화창 · 글자 배율) · **커서 · 조준선 모양/크기**
- 기타: **창 밖에서 소리 끄기** · **보스 패턴 튜토리얼 켜기를 장마다 체크**(스토리 퀘스트를 이미 봤어도 다음 도전에서 대화 없이 패턴 튜토리얼만 — 지금 `BossPractice` 하나를 1~5장 각각으로 나눔)
- **난이도 조절 · 쉬운 모드는 넣지 않음**(승천 모드로 대신). 피해 숫자 · 자동 조준 · 튜토리얼 다시 보기 · 프레임 제한은 고르지 않음.
- 탭 구성은 "알아서 적절하게" → 계획: **4탭** 소리(전체 · 음악 · 효과음 · 창 밖 소리 끄기) · 화면(창 모드 · 해상도(화살표 고르기) · 수직 동기화 · 흔들림 · 번쩍임 줄이기 · UI 크기) · 게임(언어 · 대사 자동 넘김/속도 · 커서/조준선 · 보스 패턴 연습 1~5장 체크) · 조작(KeyBindsUI). 탭 머리 = tab/tab_on + 아이콘, 줄 바탕 = row, 내용 테두리 = settings_frame, 값 고르기 = arrow_left/right.

## 2026-11-26 (4) — 내면 세계 맵 '멈춘 1초' · 작별은 실제 마을로 · 로비 로고 정렬 (커밋 안 함)

- **사용자**: 죽음을 받아들이는 주인공의 감정이 잘 안 전해짐 → 내면 세계를 맵으로 만들어 받아들이는 과정을 보여 준 뒤 마을에서 작별 퀘스트. 맵 = **아무것도 없는 하얀 방**(죽음 직전 하얀 림보 분위기), 블렌더로. 대사 확정(두 줄은 사용자가 고침: "남아. 추억은 사라지지 않아." · "...그동안 계속 노력했어. 멈추게 하려고.").
- **흐름**: 꿈4가 11:59:59에서 하얗게 멈춤 → (`StoryRun`이 하얀 화면을 걷지 않고 `StoryInner.EnterWhite`, `VillageManager`는 깨어남 연출을 건너뜀) → 마을 씬 `VillageStory` "ch5" → `Ch5Ending.Run` 맨 앞 `StoryInner.Run` → 끝나면 마을 원래 자리에서 `VillageWake` → "...마을이... 녹고 있어." → **실제 마을 섬**(Isle_Gun · Ammo · Wardrobe · Center(뻐꾸기, 안 무너짐) · Gate)에서 작별, 첫 안내 "모두에게 작별인사를 전하세요." → 광장 거울 "이제 후회는 없는거지?" / 고양이 "...응." / 거울 "그럼, 이제 내려놔." → 총 내려놓기 이후 그대로(사신 · 시계탑 12:00 · 병실 · 크레딧). '자정의 시계판' 맵(StoryDial)은 이제 안 씀(프리팹 · 빌더는 남겨 둠), 거대 바늘 코드 삭제.
- **내면 맵** `Scripts/Game/StoryInner.cs` (키 `inner_done`, 맵 원점 (2600, 0, 2600)): ① 부정 — 앞으로 걸으면 예전 적들의 하얀 잔상이 다가옴(닿으면 흩어짐), 처음 한 번은 고양이가 반사적으로 쏨 → 방이 처음으로 되감김 → 거울 "또 되감았네." / 고양이 "...끝나지 않으면, 지지도 않아." / 거울 "이기지도 못하고." → 이후 쏘면 되감김, DenialEnd까지 쏘지 않고 걸어야 넘어감. ② 집착 — 그림자 고양이(고양이 모델 검은 실루엣)가 "아무도 못 데려가." · "거기까지 가게 두지 않아." · "난 아직 쏠 수 있어."를 외치며 그림자 탄(방사 · 3갈래), '버티기 20'은 쏘지 않을 때만 줄고 쏘면 +1.5초 · 그림자 커짐, 맞으면 +1초(대시 중 무시) → 0이면 그림자가 작아져 사라짐. ③ 기억 넷(Mem_Hand · Lantern · Watch · Shawl)에 다가가면 떠오르며 고양이 한마디 → 빛이 되어 거울로. ④ 거울 앞 최후 대화 8줄 → 하얗게. 하얀 방 동안 카메라 배경 · 안개 흰색, 마을 DawnVolume 무게 0, 전투 켬(쏠 수 있어야 '쏘지 않음'이 선택이 됨) · 무적, 끝나면 전부 되돌림.
- **맵**: 명세 `Art/tools/specs/kit_story_inner_spec.md` · Codex 프롬프트 `kit_story_inner_prompt.md` · 로그 `kit_story_inner.log` → `Art/tools/blender_kit_story_inner.py` → `Art/Characters/StoryInner/Kit_I_*.fbx` → `Editor/StoryInnerBuilder.cs` 메뉴 `CatTang/내면 맵 키트 만들기` → `… 내면 맵 조립` → `Resources/Story/StoryInner.prefab` (키트 `Resources/Kit/Story/I_*`).
- **판정 시각 · 연산 튜토리얼 고도화** (사용자 확정 초안): 한 쪽에 하나씩 + 제목에 'n/전체'. 판정 시각(첫 총탄, `StoryHints.CondPages`) 4쪽 = 탄환 시계 → 판정 시각(지금 그 탄으로 예: n시 칸 → 판정 m시 · 조건 → 맞아요/아직) → 직접 해 보기(조건에 맞는 칸마다 노란 테두리 + 아래 안내, 옮겨서 활성화되면 '활성화됐어요!', 전투 중 · 맞는 칸 없음이면 건너뜀) → 확인하는 곳. 연산(`OpHints.Pages`) 6쪽 = 연산 → 끼우는 곳 → 계산 순서 · 계산 규칙(본문이 0.6초마다 한 줄씩, `StoryOverlay.HintAtSteps`) → 미리 보기 → 직접 해 보기(가진 연산으로 맞출 수 있을 때만, `Reachable`). 새 도구 `StoryOverlay.TryAt`(멈추지 않고 조작하게 둔 채 여러 칸 테두리 맥동), `InventoryScreen.FeaturedBullet/Hour/Rect · SlotRect · EnhHour · EnhStoreOpRects`. '판정 +N' 이웃 효과는 연산 안내에서 빼고 그 탄을 처음 가질 때 아래 안내 줄 7초(`tut_judge_add`, **문구 초안 — 사용자 컨펌 필요**). 마을 플레이 모드에서 두 섹션 끝까지 확인. **직접 해 보기는 유도한 조작을 해야만 넘어감**(사용자): `InventoryScreen.TutAllow`(허용 끌어놓기 조건) — 판정 시각 = 그 탄을 노란 칸으로만, 연산 = 연산을 연산 칸에 끼우기 · 칸끼리 바꾸기 · 칸에서 보관함으로 떼기만. 그 밖의 끌어놓기 · 탄 클릭(강화 창) · Tab · Esc · 닫기 버튼은 거절 + 아래 줄 경고 "노란 테두리로 표시된 곳에서 해 주세요"(**문구 초안**). 두 안내 모두 전투 중엔 Tab 유도를 미룸(`InInventory(needEdit)`). 연산 안내는 직접 해 보기로 활성화시킬 수 있는 실린더 탄을 먼저 강화 창에 엶.
- **로비**: 로고 칸 폭을 로고 비율(578 × 475)로 — 칸이 더 넓어 preserveAspect가 그림을 왼쪽에 붙여 메뉴와 가운데가 64px 어긋났음. 로고 광택 = `UIFlatFx` 새 값 `_ShineDur`(한 번 훑는 시간, 0 = 예전처럼 주기 전체): 로고는 8초마다 4.5초에 걸쳐.

## 2026-11-26 (3) — 엔딩 스토리 맵 '자정의 시계판' · 병실 달빛 (커밋 안 함)

- **사용자 확정**: 엔딩도 스토리 맵에서. 배치 = 시계판 원형 · 시작 = 마을에서 깨어나 넘어감 · 분위기 = 새벽으로 밝아짐. 대사는 위 확정 그대로.
- **맵** `Editor/StoryDialBuilder.cs` 메뉴 `CatTang/엔딩 스토리 맵 시계판 조립` → `Resources/Story/StoryDial.prefab`. 마을 키트(`Resources/Kit/Village`)와 마을 섬 생성기(`VillageKitBuilder.BuildIsle` 등, internal로 엶)를 시계판 배치로 다시 씀: 가운데 광장 섬(Plaza · Centerpiece 시계탑, 문자판 바늘 11:59 = `PlazaClock/FaceAxis/{MinuteHand,HourHand}` — `Ch4QuestBuilder.FindFace · MakeHand` 재사용) + 반지름 31m에 2시 총포상(보안관) · 4시 탄약 공방(대장장이) · 8시 의상실(재봉사) · 10시 여관(뻐꾸기) · 12시 시간의 문(문지기), 섬마다 가운데에서 다리 하나. 허공 아래 y −6.5에 11:59를 가리키는 거대한 바늘 둘(`DialHands/GiantMinute · GiantHour`, ClockHand_Float), 둘레에 숫자 돌 12개(`Numerals/Numeral_h`), 새벽 별밤 허공 판 · 떠 있는 섬 · 부스러기. 건물은 녹아 오르는 조각 · 금빛 모래(RisingBits).
- **엔딩 흐름** `Scripts/Game/Ch5Ending.cs`: 마을에서 "...마을이... 녹고 있어." → 금빛 흩어짐 · 흔들림 → 하얗게 → 마을 씬 멀리(`DialOrigin` (−1600, 0, −2600))에 맵을 놓고 Spawn으로, 시계판 전체를 한 번 보여 줌 → 섬 순서 2 → 4 → 8 → 10 → 12시(시계 방향으로 12를 향해). 섬을 17m 떠나면 섬 · 다리 · 그 숫자 돌이 같이 무너짐(뻐꾸기 섬도 이제 무너짐 → 광장만 남음). NPC가 변한 물건은 섬 아래로 붙여 같이 떨어짐. 광장 거울 고양이(`MirrorCatSpot`) → 총 내려놓기 → 사신(`ReaperAt`) → 시계탑 바늘 + 거대한 바늘 12:00 → 종 → 병실 → 크레딧 → 마을 복귀. 맵 프리팹이 없으면 예전처럼 마을에서 진행. 엔딩 동안 `PlayerController.FallBack`(떨어지면 맵 Spawn으로) · `MiniMapView.Suppressed`, 끝에 풀림.
- 배치 수치: 시계판 반지름 31m · 가운데 섬 9.5 × 9 · 바깥 섬 7.5 × 7(첫 조립 27m · 9.5 × 9는 섬끼리 붙어 줄임) · 거대 바늘 시침 22m · 분침 36m. 미리보기 `Art/tools/specs/story_dial_top.png` · `_quarter.png` · `_plaza.png` · `_gun.png` · `_wardrobe.png`, 플레이 화면 `story_dial_play_1~4.png`.
- **시험**: 마을 씬 플레이 모드에서 자동 진행으로 엔딩 끝까지 확인 — 마을 첫 줄 → 시계판 Spawn → 섬 다섯 작별 · 물건 · 섬 · 다리 · 숫자 돌 붕괴 → 거울 고양이 · 총 내려놓기 · 사신 → 시계탑 · 거대 바늘 12:00 → 병실 → 크레딧 → 마을 복귀(맵 사라짐 · FallBack · 미니맵 숨김 풀림) → 문지기 승천 입구. 슬롯 1 값은 시험 전으로 되돌림. **안 본 것**: 사람 손으로 걸어 다니는 동선(시험은 순간이동), 곡 · 효과음 귀로, 크레딧 글자 화면.
- **다리 입구 정리** (2026-11-26 사용자: 진입로를 막거나 시각적으로 헷갈리게 하는 것 치움): `StoryDialBuilder.ClearMouths` — 다리 양 끝 입구 통로의 가장자리 바위(Edge_A/B/C) · 다리 밑받침 · 가로등 · 상자 · 걷는 줄의 가장자리 판정을 지우고 난간 끝에 짧은 벽(MouthWall)을 이음. 가운데 섬 가로등은 다리 줄기 사이(90 · 270 · 155 · 205도)로, 벤치 뺌, 거울 고양이 자리 (−1.2, −4.6). NPC는 다리 줄기에서 옆으로 4m 이상, 건물은 바깥쪽으로 더 밀어 둠.
- **섬 붕괴 시점 고침**: 다리 위에서 섬이 무너져 허공으로 떨어지던 것 → `Ch5Ending.CollapseWhenLeft`가 다리 가운데 쪽 끝(Deck 판정 길이)까지 돌아와야 무너지고, 막는 벽도 그 입구에.
- **녹아내리는 모래 아랫면** (2026-11-26 사용자: 아랫부분을 블렌더로 깎아 녹아내리는 느낌 → 물방울 말고 흘러내리는 모래 → 아래로 갈수록 디졸브 · 모래 질감): 블렌더 `Art/tools/blender_kit_story_dial_under.py`(Codex 3차, 명세 `specs/kit_story_dial_under_spec.md`, 섬 윤곽 `specs/story_dial_outlines.json`) → `Art/Characters/StoryDial/Kit_D_Under_*.fbx` + `_tips.json` → 메뉴 `CatTang/엔딩 시계판 아랫면 키트 만들기`(`Editor/StoryDialKit.cs`) → `Resources/Kit/StoryDial/Under_*`. 모래 사태 · 세로 홈 모래 기둥 · 부서져 흩어지는 알갱이 · 박힌 톱니(방울 · 녹은 금 없음). 재질 = 새 셰이더 `CatTang/ToonDissolve`(툰 복사본, `Art/tools/make_dissolve_shader.py`로 생성 — 툰을 고치면 다시 돌릴 것): 모래 알갱이 질감 + 높이 −2.5 → −11.5에서 모래 무늬로 디졸브 · 가장자리 모래빛. 바위 · 흙 띠 색도 모래 쪽으로 모음.
- **모래 파티클**: 모래 기둥 끝(섬마다 12곳, 가운데 16) 가늘게 쏟아지는 모래 줄기(`SandStream`) · 섬 윤곽을 따라 흘러넘치는 모래 커튼(`RimSand`) · 디졸브 구간 면에서 떨어져 나오는 모래(`DissolveDust_0~2`, 아래 띠일수록 많이). 방울 파티클은 뺌.
- **대화창 버그 고침**: `Scripts/UI/GameUI.cs:556` `GetComponent<CanvasGroup>() ?? AddComponent` — Unity 가짜 null이라 `??`가 안 먹어 대화마다 `MissingComponentException`(Banner) → 대화창 등장 · 화자 교대 애니메이션 코루틴이 첫 프레임에 죽던 것 → `TryGetComponent`로. 같은 실수 `Game/DangerEdge.cs:25` · `UI/CursorTip.cs:36`도 같이 고침. 플레이 모드 대화 시험에서 이름표 CanvasGroup 붙고 예외 0건 확인. **규칙: Unity 컴포넌트에 `??` · `?.` 쓰지 말 것.**
- **병실 달빛** (2026-11-26 사용자: 광원 밝기 전체적으로 줄여 달빛 느낌): 주변광 0.5대 → (0.27, 0.3, 0.42) 푸르게, 해 ×1.2 → ×0.6 · 색 (0.72, 0.8, 1), 배경 더 어둡게, 스탠드 2.2 → 1.0 · 모니터 1.2 → 0.55 · 창 달빛 스포트 7 → 6(상대적으로 달빛이 주도). 병실 맵 재조립함. 확인 그림 `Art/tools/specs/story_ward_moonlight_check.png`.

## 2026-11-26 (2) — 5장 스토리 · 엔딩 구현 (커밋 안 함 · 컴파일 OK · 플레이로 끝까지 돌림)

- **스토리 맵 5 '11:59의 병실'** `Scripts/Game/StoryWard.cs` (RunManager 5장 보스 문에 연결, 키 `storymap_ward`). 맵 = 블렌더 `Art/tools/blender_kit_story_ward.py`(Codex, 명세 `Art/tools/specs/kit_story_ward_spec.md`, 로그 `kit_story_ward.log`) → `Editor/StoryWardBuilder.cs` 메뉴 `CatTang/스토리 맵 5 병실 키트 만들기` → `… 맵 조립` → `Resources/Story/StoryWard.prefab` (키트 `Resources/Kit/Story/H_*` 21개, 가구 1.4배 · 벽시계 1.3배 · 사신 1.15배). 미리보기 `Art/tools/specs/story_ward_top.png` · `_quarter.png`. **배색 = 흰색 계열**(2026-11-26 사용자: 가시성 — 흰 타일 · 흰 벽 · 연한 청록 징두리 · 밝은 그늘, 실내 빛 밝게). **빛 = 북동쪽 창 달빛 한 방향**(2026-11-26 사용자: 전방향 빛이 어색, 실내니까 광원 맞춤) — 해 Euler(40, −146) · 부드러운 그림자 0.9 · 창 스포트 그림자, 방 가운데 채움 빛 뺌, 주변광 0.5대로 낮춤 → 그림자가 남서쪽으로 길게.
  ① 죽음의 시각 3시 → 8시(12초, 틀린 탄은 튕김) ② 링거 게이지(시작 12 · 최대 20 · 모래시계 +6, 5초마다 2개, 24초 버티기, 실패 = 다시) ③ 그림자 전투 4 · 5 + 숫자 인장(16초마다, 성공 = 그림자 체력 35% 깎임, 실패 = 인장당 4%) → 초침 52 → 57초 → 총성 → 사신 그림자 고개 돌림. 곡 `bgm_story_5`(Gemini 2:56).
- **보스 3페이즈**: 체력바 이름 `시간의 주인 · 11:59:SS` → `12:00` (`Ch5Bosses.DisplayName`). 토스트 "마지막 1분 — 버텨라!"는 원래 있던 것.
- **꿈4 '마지막 장'** `Scripts/Game/DreamCuts4.cs` (내부 dream6 = DreamOf(5), 29.3초 = 곡 `story_dream4`). 그림 15장 = 크롬 ChatGPT 유화 → `docs/tools/dream6_import.py` → `Resources/Story/Dreams/dream6/` (원본 `Art/story_src/dream6/`, `_j` = 붓질 떨림판). 곡 = 크롬 Gemini 62.6초를 `Art/tools/story_dream4_cut.py`로 세 토막 이음. `StoryRun.Dreams`에 6번째 항목. 기절 · 깨어남 대사 없음.
- **엔딩** `Scripts/Game/Ch5Ending.cs` + `VillageStory` 비트 `ch5` · `ascend`: 섬 순서 보안관 → 대장장이 → 재봉사 → 뻐꾸기 → 문지기(화살표 안내), NPC가 3장 거울 물건(`Kit/Ch3Quest/*` · 문지기 = `Kit/Story/W_GrandClock`)으로 → 그 섬을 17m 떠나면 섬 · 다리 붕괴(가운데 섬은 남김). 금빛 먼지 · 새벽빛 점점. 광장 거울 고양이 대화 → `E 총 내려놓기`(총 복제본을 바닥에 눕힘) → 거울 고양이 바스러짐 → 사신(`H_ReaperBody/Head`) 솟아나 고개 숙임 → 광장 시계 12:00(`ClockOverride`로 Ch4Quest 째깍 멈춤) → 종 → 하얗게 → 병실 몽타주(`DreamCuts.For(7)`, `dream7` = old_a · old_c 복사, 심전도 평탄) → '끝' → 크레딧(11:59 / 기획 · 개발 정승운 / 도구 Unity · Blender, 곡 `bgm_ending` Gemini 2:56, Esc로 넘김) → 마을 다시 불러옴(섬 복원) → 문지기 "...또 왔군." → `ascend_open` · 시간의 문 라벨 "승천 (준비 중)".
- 시험: 병실 맵 완주 + 엔딩 완주(크레딧 · 승천 입구까지)를 플레이 모드에서 자동 진행으로 확인. 슬롯 1 시험 값은 되돌림(장 4 · 판 29 · 스토리 키 삭제, 마지막 플레이 시각만 오늘로 남음). **안 본 것**: 귀로 곡 확인, 병실 끝 총성 · 사신 고개 연출 화면, 크레딧 글자 화면, 실제 손으로 하는 난이도(시험 캐릭터는 5장 적에게 금방 쓰러짐), 엔딩 새벽빛(낮춘 뒤 다시 안 봄).
- `GameUI.DevAutoAdvance`면 `E 총 내려놓기`도 2초 뒤 자동(녹화 · 시험용).

## 2026-11-26 — 5장 스토리 · 엔딩 기획 확정

흐름: 스토리 맵 5 '11:59의 병실' → 보스 시간의 주인 → 꿈4 '마지막 장' → 무너지는 마을 엔딩 → 크레딧 → 승천 모드. 거부 최고조(벽시계를 쏨) → 받아들임(총을 내려놓음). 대사 전부 사용자 컨펌. **기절 · 깨어남 대사는 사용자가 줄 예정(빈칸).**

- **스토리 맵 5 '11:59의 병실'** (보스 문 첫 진입, 틀 = StoryLake · StoryWorkshop, 맵 = 블렌더): 누운 노인 그림자 + 구석에서 지켜보기만 하는 사신 그림자.
  - 도착 고양이 "...여긴..." / "약 냄새... 낯익어." · 누운 그림자 고양이 "...누구지." · 사신 그림자 고양이 "...저건 처음부터 날 보고 있었어."
  - ① 죽음의 시각(벽시계가 가리킨 시각 탄으로 시계 맞히기, 2번): ???(노인) "...몇 시... 지..." → 성공 "...아직... 아니야." → 2번째 "...조금만... 더."
  - ② 시간 게이지(링거 방울마다 시간 줄어듦 · 모래시계 주워 버티기): ???(노인) "...시간이... 새고 있어." · 고양이 "(새는 걸 막으면 돼.)" · 버팀 고양이 "...버텼다."
  - ③ 그림자 전투 + 숫자 인장(느슨하게): 시작 고양이 "...그림자들이 침대로 몰려든다." · 끝 고양이 "아무도 못 데려가."
  - 끝: 초침이 12로 다가감 · 고양이 "...안 돼." / "거기까지 가게 두지 않아." → 총성 → 벽시계 멈춤 → 사신 그림자가 처음 고개를 돌림 → 하얗게 → 보스 방.
- **보스 3페이즈 60초 생존 = 마지막 1분**: 메커닉 그대로, 토스트 "마지막 1분" + HUD 시계 11:59:00 → 12:00.
- **꿈4 '마지막 장'** (유화 임파스토, 틀 = DreamCuts3): ① 노을빛 이불 속 어린 손이 그림책 마지막 장을 넘기려다 잠듦 ② 병상 협탁, 늙은 손이 같은 책을 마지막 장까지 넘김 ③ 결투 뒤 고양이 영웅이 총을 땅에 내려놓음 ④ 노을 길을 걸음, 발자국이 시계 눈금처럼 ⑤ 길 끝 그녀 그림자(4장 빨간 숄)가 손을 내밂 ⑥ 맞잡기 직전 11:59:59 초침 멈춤 → 하얗게. 그림 = 내장 브라우저 ChatGPT, 곡 = 크롬 Gemini.
- **엔딩 '무너지는 마을'** (직접 걸어감): 깨어남 뒤 고양이 "...마을이... 녹고 있어." → 섬이 다리 열린 반대 순서로 무너짐, 섬마다 NPC 한마디 → 원래 물건(3장 거울 물건)으로 → 섬 붕괴.
  - 보안관 "이제야 생각났어. 이 외투, 네 거였지." / "...잘 어울렸어." → 외투
  - 대장장이 "모래는 다 떨어졌군." / "이번엔 뒤집지 마라." → 모래시계
  - 재봉사 "마지막 단추까지 달았어." / "...이제 입고 가." → 재봉틀
  - 뻐꾸기 "노래, 기억났어." / "...한 번만 더 울어 줄게." (뻐꾹) → 뻐꾸기시계
  - 문지기 "문은 이제 하나뿐이야." / "그건 내가 지키는 문이 아니야." → 괘종시계
  - 광장(시계탑 11:59): 거울 "늦지 않았네." · 고양이 "...전부 사라졌어." · 거울 "원래 있던 자리로 돌아간 거야." · 고양이 "...난 아직 쏠 수 있어." · 거울 "알아." / "그래서 여기까지 온 거잖아."
  - **[E 총 내려놓기]** 상호작용 하나 → 거울(웃으며) "...이제 내 몫은 끝났어." / "잘 가." → 바스러져 먼저 사라짐 → 시간의 주인이 광장 가장자리에서 낫을 내림 · 고양이 "...기다려 준 건가." → 시계 얼굴 12:00 → 종 → 하얗게 → 병실(심전도 평탄 · 협탁 그림책 마지막 장 '끝') → 크레딧.
- **크레딧 뒤**: 복원된 마을에서 깨어남 · 문지기 "...또 왔군." / "한 번 더 들여다볼 텐가?" → 승천 모드 엶.

## 2026-11-25 (밤 3) — 4장 스토리 (커밋 안 함 · 컴파일 OK · 플레이 확인 일부)
명세 = `Art/tools/specs/ch4_story_spec.md` (대사 · 토스트 · 컷 전부 사용자 확정), 메모리 `story-ch4-flow.md`.
- **스토리 맵 4 '시계를 멈춘 공방'** `Scripts/Game/StoryWorkshop.cs` (RunManager 4장 보스 문에 연결): ① 벽시계 순서대로 멈추기 2판(2·5·9 → 3·8·6·11) ② 오르골 톱니 3개(2·9·5) → `q4_musicbox` ③ 그림자 전투 2번 + 문자판 칸 섞기(느슨) → 노인 그림자가 괘종시계 바늘을 붙잡아 11:59. 맵 = 블렌더 `Art/tools/blender_kit_story_workshop.py` → `Editor/StoryWorkshopBuilder.cs` → `Resources/Story/StoryWorkshop.prefab` (키트 `Resources/Kit/Story/W_*` 49개). 곡 `bgm_story_4`(끝 3초 크로스페이드 반복). 플레이로 처음부터 끝까지 확인(맞히기는 코드로).
- **꿈3 '11:59의 기원'** (내부 dream5) `DreamCuts3.cs`: 흑백 리노컷 + 빨강, 11컷 · 셀 32장(내장 브라우저 ChatGPT, 원본 `Art/story_src/dream5/`, 콘택트 시트 `docs/images/dream5/contact_sheet.png`), 곡 `story_dream3`(56.7초). 게임 안 녹화 확인 영상 `docs/images/dream5/dream5_preview.mp4`. `StoryOverlay`에 `DreamCuts.SongOf/Tune` 연결.
- **마을 퀘스트 '멈춘 마을'** `Ch4Quest.cs` · `Editor/Ch4QuestBuilder.cs`(Village 씬에 배치 저장함) · `VillageStory` ch4 Beat: 회색 정지 마을 → 섬 4곳 톱니(여관 4 · 공방 2·7 · 총포상 10·5 · 의상실 3·8·12) → NPC 대사 → 광장 11:59(쏘면 튕김) 마지막 대화. 개발 메뉴 `CatTang/4장 멈춘 마을/…`. 톱니 배지는 섬 가까이서만. 섬 완료는 메뉴로만 확인(직접 쏘기 판정 미확인).
- **대화창 명찰**: 화자가 바뀌면 좌우로 미끄러지지 않고 앞 자리에서 사라졌다 새 자리에서 톡 생김(`GameUI.TalkCo`, 사용자 요청). 플레이 화면 확인 못 함.
- **남은 것**: 4장 깨어남 혼잣말(사용자 작성, `StoryRun.WakeLines[3]`) · 곡 3개 귀로 확인 · 마을 톱니 직접 쏘기 판정 · 3장 노인 팔 피벗(+0.205가 왼어깨일 수 있음, 3장은 안 고침) · `Resources/Story/Dreams/dream5/f1~f4.png` 옛 자리 표시 그림 삭제 가능.

## 2026-11-25 (밤 2) — 인계 6가지 구현 (커밋 안 함 · 컴파일 OK · 플레이 일부 확인)
- **① 적 투사체**: 셰이더 `CatTangEnemyProjectile.shader`에 밝은 심(`_CoreWhite`) + 어두운 잉크 테두리 + 적과 같은 빨간 바깥 테두리(렌더러 기능 CatTangOuterOutline, 스텐실 6). **탄 색 = 장 바닥의 반대 계열**(사용자 요청): 1장 밝은 청록 · 2장 라임 · 3장 진한 주홍(흰 심 0.04 · 광륜 0.18) · 4장 밝은 빨강 · 5장 금빛 노랑 (`EnemyProjectile.ColorOf`, 5장 VoidLob도). 모바일 렌더러엔 빨간 테두리 기능 없음.
- **② 문 방향 화살표**: `GuideArrow.Marker/ClearGroup` — 보상 문(골드 문 · 총탄 문 · 최종 보스, 화면 밖이면 '보상 문 N개'로 묶음) + 중간 방을 비우면 다음 방 나무 문('다음 방'). 문 근처 2.6m면 숨김, 다음 방 들어가면 지움.
- **③ 미니맵** `UI/MiniMapView.cs`(HudView.Build 끝 한 줄): 방 · 적 점 · 문 · 고양이 + 조준 바늘. 방 판은 장별 색(`Resources/UI/map/map_room_ch2~5.png` = 원본 map_room을 장 바닥 색으로 다시 칠함). `MiniMapView.Suppressed`.
- **④ 길 막는 물체 부수기** `Game/DoorPathClear.cs`: 문 3m + 문↔방 가운데 통로 반폭 1.5m 안의 소품 · 엄폐물 · 떠다니는 잔해(바닥 · 벽 · 문 · 상인 · 적 · 픽업 제외). 덜컹 → 자기 메시 조각(GateDebris) + 먼지 · 유리는 유리 파열. 상인 자리는 그 길을 피함. 소품 전용 부서짐 이펙트(블렌더)는 아직 없음.
- **⑤ 튜토리얼 기능 잠금** `UI/TutorialGate.cs`: Tab · Q(불릿 파티) · 우클릭(총 스킬) · 판정 줄 · 특성 목록을 가르치는 순간 금빛 등장. 총 스킬 안내 새로 추가(판 2). 개발 메뉴 `CatTang/튜토리얼/…`.
- **⑥ 연산 첫 등장 안내** `StoryRun.cs` `OpHints`: 보상 문 한 줄 + 처음 얻으면 Tab → 연산 칸 → 탄 강화 창 3장. 레벨 업 연달아 열릴 때 안내가 카드 위에 겹치던 것 고침(0.4초 조용할 때까지 기다림). 3장 문구 "음수는 절대값으로 처리해요." 사용자 확정(`Enum.csv` 나누기 설명도).
- 플레이 확인(슬롯 3, 뒤에 지움 · 슬롯 1 그대로): 1장 투사체 · 미니맵 · '다음 방' 화살표 · 판 2 잠금(Q · 우클릭 · Tab · 열기 칸 숨김) · 연산 안내 1~3장 · 3장/5장 새 탄 색. **안 본 것**: 보상 문 화살표 묶음, 길 막는 물체 부서짐 연출, 총 스킬 안내, 2장 · 4장 탄 색.
- **문구 전부 사용자 확정**(총 스킬 안내 · 연산 문 한 줄 · 연산 1~4장 · '다음 방').
- **남은 것**: 판 2에선 기본 탄(연산 칸 0)으로 설명하게 되는 문제. 열기 게이지 빈 틀은 HUD 바탕 그림이라 잠금 중에도 보임. 판 1 Esc 포기 시 튜토리얼이 통째로 건너뛰어짐(기존 동작).

## 2026-11-25 (밤) — ▶ 다음 작업 인계 (사용자 요청, 구현 안 함 · 다른 계정에서 진행)
3장 퀘스트 · 스토리 맵 3 · 스토리 곡은 바로 아래 절 (커밋 안 함). 호숫가 안내 토스트는 **사용자 확정**(지금 코드 문구 그대로), 3장 대사 전부 확정.
사용자 요청 6가지 (2026-11-25 원문 요지 · 참고 코드 위치):
1. **적 투사체 가시성**: 배경과 색이 비슷해 안 보이는 투사체(장마다 배경색 대비 확인 — 3장 거울빛 · 5장 보라 등)를 더 또렷하게. 그리고 **적에게 두른 빨간 배경 선(바깥 테두리)을 투사체에도** 넣기. 참고: `Scripts/Game/EnemyProjectile.cs`(`Style` Orb · Revolver · Pellet · Sniper · Needle · Numeral · Ticket, `Spawn`), 적 테두리 = `Editor/EnemyModelBuilder.cs` `EnemyEdge(Material)` · `Scripts/Game/DangerEdge.cs`(폭발물 맥동 테두리), 탄은 풀링(`Scripts/Game/Pool/`) 쓰니 재사용 때 테두리 상태 초기화 주의.
2. **전투 끝나면 화살표로 문 방향 안내**: `RunManager.RoomCleared()` → `SpawnDoors()`(RunManager.cs 318 · 554) 뒤에 `GuideArrow.Show(door, …)`(Scripts/UI/GuideArrow.cs, 마을에서 쓰는 것) 또는 화면 가장자리 방향 화살표. 문에 들어가면 지움.
3. **오른쪽 아래 현재 방 미니맵(롤처럼)**: M 키 전체 지도 `Scripts/UI/MapView.cs`(방 타일 · 문 · 고양이 머리 아이콘 그림 재사용 가능)를 축소해 HUD(`Scripts/UI/HudView.cs`) 오른쪽 아래에 상시. 지금 방 + 적 점 · 문 · 고양이 위치 정도.
4. **문이 생길 때 길 막는 오브젝트 부수기**: `SpawnDoors()`에서 문 위치 둘레 일정 거리 안의 오브젝트 중 바닥 · 벽(방 가장자리)을 뺀 것(소품 · 엄폐물 · 떠다니는 잔해 등)을 파괴(부서지는 연출 + 충돌체 제거). 문과 방 가운데를 잇는 통로 폭 안도 같이 보면 좋음. 방 꾸미기 코드: `RoomBuilder` · `Kit` · `Ch3Kit` · `Ch5Kit.Dress` 등.
5. **튜토리얼 동안 아직 안 배운 기능은 꺼 두기**: 예) 첫 판에 궁극기(불릿 파티 `Q`, `PlayerController` 271행 · `BulletParty()` 1103행, 열기 `rev.heat`)가 열려 있음 · 우클릭 스킬(270행 `UseSkill`) 등. 튜토리얼 흐름 = `Scripts/Game/StoryRun.cs`(`StoryRun1` 판 1 · `StoryHints` 판 2 · 키 tut1 / tut2 / tut_synergy). 안내 전엔 입력 무시 + HUD 표시도 숨김, 안내할 때 켜짐.
6. **연산 첫 등장 즉시 튜토리얼**: 연산(Op, `Tables.Ops` · `OpDef`, 인벤토리 '탄 강화' 창 `Scripts/UI/InventoryScreen.cs`)을 처음 얻거나 보상 문에 처음 보일 때 바로 안내. **판정 시각과 쏘는 시각에 정확히 어떻게 관여하는지** 알려 주기 — 규칙은 `Scripts/Game/Revolver.cs` 머리말: 판정 시각 = 탄이 놓인 시각에 연산을 위 슬롯부터 적용(+ 이웃 탄의 판정 +1 효과), 나누어떨어지지 않으면 반올림(.5는 0에서 멀어지게), 활성화(자리 조건 충족)된 탄만 특성 · 고유 효과. 발사 순서(1 → 12, 되감기면 12 → 1)는 연산과 무관하다는 점도 구분해 설명. 기존 튜토리얼 방식([[tutorial-ui-prefs]] 메모: 해당 화면을 열게 유도 → 비추며 설명 · 노란 선 · 문구는 사용자 컨펌) 따름.
- 위 1~6의 안내 문구 · 연출은 초안으로 만들고 사용자 컨펌 받기.

## 2026-11-25 (저녁) — 3장 클리어 퀘스트 '거울 마을' · 스토리 맵 3 '거울 같은 호숫가' · 스토리 맵 전용 곡 3 (커밋 안 함)
- **3장 클리어 = 블랙아웃**(꿈4 안 나옴, `StoryRun.Blackout` 2 · 3장) → 깨어남 첫 줄 "...뭔가 이상해." → `VillageStory` "ch3" → `Ch3Quest.Intro`.
- **거울 마을** (`Scripts/Game/Ch3Quest.cs`, 배치 `Editor/Ch3QuestBuilder.cs` 메뉴 `CatTang/3장 거울 마을 키트 만들기` · `배치 (Village 씬)`): 들어올 때 `Ch3Quest.Setup()`이 정함 — 3장 클리어 + 2장 퀘스트(q2_gun) 끝 + 미완료면 `Village` 뿌리 x 배율 −1(섬 Top 정적 배치 끔). 이름표는 `Billboard`가 거꾸로 안 보이게 맞춤.
  - 섬 거울 4(`MirrorView`: 좌우 반전 거울 카메라, ReflectOnly 레이어 = 거울에만 · MainOnly = 거울엔 안 비침, 레이어 9 · 10 추가). NPC는 거울 옆으로 나오고 원래 자리에 노인 집 물건(뻐꾸기시계 · 금 간 모래시계 · 외투+포스터+영사기 · 재봉틀)이 거울에만 비침 → 그 빈 자리를 쏘면(마우스 지점, 거울 17m 안에서만) 깨짐 · 비친 자리(카메라 중심선 반대)는 튕김. 거울 앞 E = 들여다보기(`MirrorPeek`).
  - 넷 다 깨면 광장 큰 거울(정면에서 마주 봄 `faceFromGlass`): 고양이 대신 노인 실루엣(`OldManShadow` + 팔 `ArmPivot`) → 손 들기 → 쏘면 깨짐 · 삐 · 마을 복귀. 고양이 모델은 PlayerController가 계속 Vivid 레이어로 되돌려서 거울 카메라에서 Vivid를 뺌(`excludeMask`).
  - 거울 속 고양이 = **광장에 나와 선 NPC**(`MirrorCatNpc`, 3장 상인 모습 · 발밑 원형 오라 없이 둘레 거울 조각, 사용자 지시로 물웅덩이/손거울 안 씀). E: 퀘스트 중 힌트, 끝난 뒤 판마다 다른 한 마디.
  - 효과음 Stable Audio: q3_cuckoo · q3_projector · q3_sewing · q3_crack (모래는 e5b_sand).
- **스토리 맵 3** (`Scripts/Game/StoryLake.cs`, `Editor/StoryLakeBuilder.cs` 메뉴 키트 · 맵 조립, 프리팹 `Resources/Story/StoryLake.prefab`, 블렌더 `Art/tools/blender_kit_story_lake.py`): ① 물에 비친 시각(실제 3시 → 5시) ② 물수제비(3 → 7번, 부표) ③ 그림자 전투 2 + 숫자 연잎 3(15초) → 잔교 서쪽에 앉은 두 그림자 → 수면 → 청년만. `RunManager` 3장 보스 문에 연결.
  - 물 = 새 셰이더 `CatTang/LakeWater`(사용자: "물 느낌 확실하게" → 뒤에 "각진 흰 반짝임은 조금만" · 물결 조금 더: `_GlintAmount` · `_CrestAmount` · `_WaveStrength 0.42`). 기둥 시계 햇빛 번짐 = 빌더가 만든 둥근 그림 `Art/Characters/StoryLake/SunGlare.png`(예전엔 그림이 없어 네모로 보였음): 굴절 · 깊이 색 · 물가 거품 · 드문 반짝임 · 반사(MirrorReflection, PlaneY = 물 높이) + 화면 안 물결 링. PC 렌더 설정(깊이 · 불투명 텍스처)에 기댐 — 모바일 설정은 꺼져 있어 깊은 물 색 + 반사만.
- **스토리 맵 전용 곡** (크롬 Gemini): `bgm_story_1`(시계탑 광장) · `_2`(비 오는 승강장) · `_3`(호숫가) — `StoryBgm`, 맵 내내 · 전투에서도. 원본 `Art/audio_src/gemini/`. **귀로는 안 들어 봄.**
- 플레이 확인: 마을 뒤집기 · 첫 대화 · 섬 거울 깨기 · 광장 큰 거울 · 마을 복귀, 호숫가 ①~③ · 끝 — 대사는 자동 넘김, 쏘기 판정은 코드로 맞힘 처리(실제 클릭 조준은 안 해 봄). 시험용 저장은 되돌림.
- 남은 것: 토스트(호숫가 안내 문구)는 초안 · 컨펌 전. 마을 뒤집힘 동안 BoxCollider 음수 배율 경고(동작은 정상). 깨진 거울 유리는 단색. Unity가 뒤에 있으면 컴파일 뒤 MCP 브리지(6400)가 안 열림 → 창을 앞으로.

## 2026-11-25 (진행 중) — 2장 해금 묶음 구현 · 인계
명세: `Art/tools/specs/ch2_unlock_spec.md` (탄 6 · 총 2 · 묶음 4 · 미련 개편 · 이펙트 9 이름표). 메모리 `story-ch2-flow.md`에 확정 내역.
- **커밋함 (81edb786):** 2장 클리어 퀘스트 (블랙아웃 · 대장장이 · 흐르는 모래 · 결투 · 탄약 공방 · 총포상). 대사 확정.
- **안 커밋 (이 세션 작업, 컴파일 OK · 플레이 미확인):**
  - 표: `Data/Tables` Bullet(1047~1052) · Gun(2009 · 2010) · Skill(6009 · 6010) · Effect(9181~9184 미련 개편, 9263~9271) · TraitTier(31036 · 31037) · Trait(미련 설명) · Unlock(8001 · 8002 · 8101 · 8102). CSV를 직접 고침 (`docs/tools/build_tables.py`는 코스튬 등이 CSV와 이미 어긋나 있어 돌리면 덮어씀 — 돌리지 말 것).
  - 코드: `Game/PlayerSkills.cs`(새 partial: 총 스킬 랜턴 던지기 · 증기 돌진, 공명 강화 `Resonance` · 원혼, 기차 탄 · 환승 · 레일 · 연기, `LanternField`, 개발용 `DevFire` · `DevSkill`) · `PlayerController.cs`(partial로 바꿈, 우클릭 → `UseSkill`, 사격에 기차 · 공명 · OnCrit · LockTransfer · SmokeRing · OnKill 다음 탄) · `Revolver.DevAllAwake` · `Tables.Skills` · 미련 색(Effects · GunkataFx) · `GameUI.GroupContent` 묶음 카드 2열 레이아웃 · `UIKit.ItemTip` 총 스킬 줄 · `Dev/Ch2UnlockDemo.cs`(시연 녹화 8장면).
  - 총 스킬은 원래 모든 총이 백스텝 샷 하나였음 → 새 총 두 자루만 표 스킬, 나머지 그대로.
- **다른 세션 결과 (안 커밋):** 아이콘 8 `Resources/Icons/bullets/bullet_1047~1052.png` · `guns/gun_2009 · 2010.png` (미리보기 `Art/ui_src/ch2_icons_preview.png`) · 기획서 `docs/1159_총탄_기획서_20261125_정승운_Ver04.pptx` · `총_..._Ver03` · `2장_..._Ver04`. 이펙트 9종(ch2-vfx 세션)은 블렌더 메시 · 빌더 `Editor/Ch2UnlockFxBuilder.cs` 준비, Unity 프리팹 만드는 중.
- **Unity MCP 주의:** Claude의 unityMCP 도구가 "No Unity Editor instances"로 실패하면 8080에 직접 JSON-RPC (scratchpad `u.sh <도구> <인자.json>` + `ex.py`: initialize → mcp-session-id → tools/call). 서버 · Unity는 정상.
- **시연 완료 (2026-11-25 오후):** 이펙트 프리팹 9개 다 들어옴 → Run 씬에서 `Ch2UnlockDemo.All` 녹화, GIF 8개 `Art/fx/ch2_unlock/demo/<장면>.gif` + 구매 화면 `demo/shop.gif` (테스트 슬롯 3에서 촬영 후 `DeleteSlot(3)` · `ActiveSlot = 1` 복구). 콘솔 에러 없음.
- **피드백 반영 (2026-11-25 오후):** ① 망각 6단계 얼음 꽃: 바닥 별 그림 · 꽃잎 16장 → 블렌더로 구운 별 모양 얼음 수정 무더기 `Resources/FX/IceBloom/fx_icebloom_0~2` (`Art/tools/blender_fx_icebloom.py`, 땅 솟음 Upheaval처럼 솟음) · 반투명(몸 알파 0.42 · 끝 0.62 · 서리판 0.3), 깨짐 그림은 그대로 (`OblivionFx.IceBloom`). ② 증기 돌진 6 → 8m (Skill.csv 6010 · 속도 그대로). ③ 미련 원혼: Hyper3D Rodin 유령(`Art/models/fx_wraith_h3d/wraith_h3d.glb` → `Art/tools/blender_fx_ghost.py` → `fx_c2_ghost`) + 오라 껍데기 2겹(새 셰이더 `CatTang/GhostAura`, 재질 FxC2WraithAuraIn/Out) + 오라 알갱이. GIF 다시 뽑음.
- **피드백 2 (2026-11-25 오후):** 얼음 꽃 = 수정 조각별 오브젝트(이름 c_<지연ms>, frost)로 다시 구움 → `IceBloom`이 서리판부터 안쪽 · 중간 · 바깥 수정 차례로 밑동에서 자라게 (Open 0.58초). 원혼 몸 = 새 셰이더 `CatTang/GhostBody`(ToonFire 반투명판: 깊이 먼저 패스 · 가운데 알파 0.2 · 가장자리 0.7 · 노이즈 줄임, 재질 FxC2GhostBody), 크기 1.7~2.0 · 카메라 쪽으로 20~30° 젖힘, 오라 세기 낮춤. 시연에 `bloom` 장면 추가(녹화 중 가둠) · 적을 녹화 전 제자리로 되돌림.
- **피드백 3:** 얼음 꽃 가운데 심(굵은 기둥 1 + 곧은 수정 6~7)으로 꽉 채움 · 깨질 때 그림판(cx_ice_bloom) 제거 → 3D 조각 · 섬광만.
- **▶ 다음:** ① 사용자 GIF 피드백 반영. ② 수치(가격 여정 60 · 마무리 80 · 랜턴 70 · 기관차 80, 총 스탯) 컨펌. ③ 커밋 (Moon.mat은 이 작업과 무관 — 빼기).

## 2026-11-24 (4) — 거울 망토 자객 v7 · 코스튬 효과 전부 인게임 확인 (커밋함)
- **거울 망토 `ShadowStrike`** (사용자 여러 번 다듬음): 돌진 0.16초 · **이동 전체 잔상** `ShadowGhost`(0.36m 간격, 프레임 사이 메움, AssassinGhost 셰이더 ZWrite Off 복사본, 알파 · 디졸브로 걷힘) — 예전엔 `Afterimage.SpawnAt`에 몸 위치를 넘겨 뿌리 위치가 두 번 더해져 엉뚱한 데 찍혔음.
  **칼끝 리본 `DashSlash`**: 칼끝 길(`TipAt`)을 월드에 고정 — 칼 쥔 손 쪽 옆을 따라 궤적 시작점부터 가늘고 **긴 꼬리** → 대상 근처부터 멈춘 몸 앞을 돌아 굵은 초승달 → 반대쪽 **짧고 뾰족한 끝**(`TipWidth`). 단색 보라(`SlashColor`) + 더하는 빛(번짐 + 바깥 날 심지) · 톱니 없이 불규칙 찢김(여러 겹 노이즈 + 무작위 V자 + 떨어진 조각) · 찢긴 자리에서 뒤로 뻗는 **속도선** · 사라짐 = 긴 꼬리부터 먹힘. 타격 = `HitArcs`(빛 호 4~6 · 섬광 · 돌진 방향 섬광 가시 · 빛 · 0.04초 멈칫) + 단색으로 다시 칠한 `bk_cos_mirror_slash` 플립북.
  녹화 도구 `Dev/CostumeFxDemo.Mirror|Others(folder)` (captureFramerate 고정) → `docs/tools/fx_frames_to_gif.py` → `docs/images/costume_fx/mirror_ingame(_slow).gif`.
- **코스튬 효과 18종 연결 확인**: 빠져 있던 9406 `OpDropRateAdd`(시계공 앞치마 연산 +30%)를 방 보상 · 레벨업 보상 가중치에 붙임. 코스튬 프리팹 7벌(4001은 기본 모델) 다 있음. 장의사 · 모래 · 잠옷 이펙트 인게임에서 뜨는 것 확인(잠옷 펄스는 전투 중엔 잘 안 보임).
- Unity MCP: 상태 파일 포트(6401)와 실제 포트(6400)가 달라 못 붙던 것 → `~/.unity-mcp/unity-mcp-port*.json` 포트를 6400으로 고침. 컴파일 뒤엔 Unity 창을 앞으로 가져와야 리로드가 끝남.
- `Art/fx/`(블렌더 미리보기 · 굽기 원본, 82MB)는 .gitignore — 게임 사본은 `Resources/FX/anim`.

## 2026-11-24 (3) — 코스튬 이펙트 다시 깎음 · 자객 고양이 잔상 (커밋 안 함, Unity MCP 연결 끊김 → 플레이 확인 안 됨, dotnet 컴파일 오류 0)
- **거울 망토**(사용자: 날아가지 말고 대상에게 자객 잔상이 떠서 벤다, 세피리아 혼령봉인연구회 제2형): 원화 = **내장 브라우저 ChatGPT** 채팅 "Create Cat Assassin Art"(`chatgpt.com/c/6b04f2e5-ecd8-83e8-9bf2-00a09d696604`) → `Art/tripo_input/assassin/assassin_windup|slash.jpg` → Hyper3D **새 계정** Rodin Quad 12k(47027f4c… · c39e755f…) → `Art/tools/blender_assassin_pose.py`(같은 배율, 움츠린 자세 높이 0.8, 앞 = 블렌더 -Y) → `Resources/Costumes/assassin_windup|slash.fbx`. **v4(사용자 추가)**: 극한 회피 한 번 = 범위 9 안 무작위 적 **최대 3명, 한 대상엔 자객 하나씩**(엇박 0.07초) · 원형 고리 · 돌진 선 없이 잔상으로 · 스쿼시 앤 스트레치 가감속(피어남 납작 → 움츠림 → 폭발 가속 앞뒤 늘어남 → 도착 찌그러짐 · 반동) · 돌진 잔상 6(같은 시간 간격 = 가속 구간 벌어짐) · 리본 꼬리 초승달 X 베기 + 섬광 가시 + 꼬리 불똥. `ShadowStrike`(본체 = Silhouette 셰이더 검은 몸, 잔상 = Afterimage), 미리보기 질감 = GLB 어둡게 + 보라 테. Effect 9407 설명 바꿈. 크기 1.8×1.35. **Unity에서 FBX 앞 방향(+Z인지) 확인 필요** — 반대면 `face`에 180° 곱하기.
- **모래 소용돌이**(사용자: 그림 느낌 줄이고 단색 광 · 선 굵게 · 일어났다 없어지는 한 벌): 블렌더 `glow()` 재질(UV 가운데 하얀 심지 → 가장자리 사라짐) + `GlowStreak`(심지 + 번짐 두 겹). 호박색 하나, 잉크 없음. `sand_full` 40장 한 벌을 수명 동안 한 번(`SandVortex.Run`), 옛 `sand_loop/grain` 삭제. 플립북 최대 장수 32 → 64(`FxFlipbook.Load`).
- **노인의 잠옷**: 오라 = 처음 불꽃 혀 그대로(사용자: 아까가 나음). 켜질 때 펄스 = 번개 · 가시 빼고 **정신수양처럼 온화하게** — 들숨(빛 점이 모여듦) → 일렁이는 기 물결 고리 3겹 + 감겨 오르는 기 줄기 + 몸 뒤 후광, `pajama_pulse` 24장 1.1초.
- GIF `docs/images/costume_fx/mirror|sand|pajama.gif`. 빌더 `CostumeModelBuilder` 오류(지운 model 읽음) 고침 — 다시 돌리진 못함(앞치마 프리팹만 있음).
- **▶ 다음**: Unity MCP 다시 연결 → 컴파일 확인 → `Cat_Tang/코스튬 모델 프리팹 만들기` → 새 플립북 텍스처 `alphaIsTransparency` 켜기(옛 것과 같게) → 거울 · 모래 · 잠옷 인게임 확인(자객 앞 방향 · 크기) → 앞 절 남은 일 → 커밋.

## 2026-11-24 (2) — 코스튬 모델 원본 비율 · 키 맞춤 · 잠옷 리깅 · 의상실 카드 초상화 (커밋 안 함, Unity 꺼진 채 → 플레이 확인 안 됨)
- **측정**: `Art/tools/blender_measure_body.py`(뼈 높이 · 뼈별 메쉬 폭) + `blender_proportion_sheet.py`(목 높이 맞춘 정면 직교 렌더, `|목 배율` 붙이면 실제 키 비교). 목 높이 기준으로 보면 세로 비율(가랑이 · 무릎)은 비슷하고, **머리가 원본보다 작음**(눈 사이 / 귀 끝 높이: 원본 0.177, 코스튬 0.13~0.14) · 팔 짧음.
- **변형**: `Art/tools/blender_costume_proportion.py` — 뼈 배율(Head 균일, 팔 · 아래팔 길이만 ×1.2, 손은 물려받지 않음) → 메쉬에 굽고 쉬는 자세로 → 귀 끝 높이가 원본과 같게 목 높이 `_neck` 맞춰 원본과 같은 블렌더 단위(키 약 0.98)로 저장. 머리 배율 4002 1.32 · 4003 1.47 · 4004 1.48 · 4005 1.45 · 4006 1.38 · 4007/4008 1.3, 기록 `Art/models/incoming/hyper3d_costume/proportion_fit.json`, 결과 `<Id>/base_fit.fbx` → `Assets/Art/Characters/CatHero/Costumes/<ModelKey>/<ModelKey>.fbx`(덮어씀). 비교 그림 `docs/images/costume/model_compare_fit.png`(실제 키, 빨간 선 = 원본 귀 끝 · 바닥). 자세 테스트 `Art/tools/blender_pose_test.py`(팔 내림 · 고개 돌림) 8벌 다 정상.
- **4008 잠옷 리깅**: Hyper3D 크레딧 부족(업로드만 됨, `rodin_generate_rigging` = 잔액 부족) → `Art/tools/blender_rig_transfer.py`로 4007(스카프) 뼈 · 가중치를 가장 가까운 면에서 옮김(같은 파이프라인이라 몸 위치 거의 같음) → `costume_pajama/` 새로 만듦(+ `_basecolor.png`).
- **빌더**: `CostumeModelBuilder` 키 맞춤을 머리 뼈 → **주인공과 같은 `CatHeroBuilder.ModelScale`**(FBX에 이미 구워짐)로. 머리 높이가 주인공의 0.5~2배를 벗어나면 경고(단위 어긋남 확인용). 아직 안 돌림.
- **의상실 카드 초상화**(사용자: "코스튬 UI도 크롬 지피티로"): 크롬 ChatGPT 채팅 "Create wardrobe grid portraits"(`chatgpt.com/c/6b04ed60-a930-83ee-949c-9ffbfbc63c15`, lineup_a/b 첨부 → 4×2 허리 위 초상화, 마젠타 바탕) → 원본 `Art/ui_src/costume/cos_wardrobe_grid.png` → `docs/tools/ui_costume_cut.py` → `Resources/UI/Costumes/cos_4001~4008.png`. `GameUI.WardrobeContent` 카드 300×440, 위에 초상화(안 연 옷 = 그림자색, 조건 채움 = 흐리게), 줄 간격 455(스크롤).
- **▶ 다음**: Unity 열기 → `Cat_Tang/코스튬 모델 프리팹 만들기` → 콘솔 경고 · 휴머노이드 아바타 확인(4008은 옮긴 가중치라 소매 · 모자 방울 움직임 확인) → 코스튬 입고 판 플레이(크기 · 총 쥔 손) → 의상실 카드 모양 확인 → 기획서 3D 모델 장 그림을 `model_compare_fit.png`로 교체 → 커밋.

## 2026-11-24 — 코스튬 확정 · 의상실 1장 해금 · 코스튬 효과 · Hyper3D 코스튬 모델 (커밋 안 함, Unity 꺼진 채 작업 → 플레이 확인 전부 안 됨)
- **확정(사용자 질문으로)**: 의상실 = 1장 클리어(기억의 방과 동시). 코스튬 8벌 / 해금 = 조건 + 조각 구매 유지. 표는 `Data/Tables/Costume.csv` · `Effect.csv`(9403~9418) · `Enum.csv` 직접 수정 (`docs/tools/build_tables.py`는 Memory 열 등이 빠진 옛 상태라 돌리면 안 됨).
  판초 그대로 · 장의사 = 처형 시 체력 2 회복 + 처형 범위 +20% / 활성 탄 -10% · 시계공 = 기존 + 장전 -10% · 거울 = 극한 회피 시 그림자 잔상이 무작위 적 공격(세피리아 혼령봉인연구회 제2형) + 극한 회피 판정 +50% / 체력 -20% · 순례자 = 판정 0 이하 탄 적중 자리 모래 소용돌이(둔화 · 지속 피해) + 개기일식 / 받는 피해 +10%, 조건 '0 이하 탄 처치 100' · 스카프 조건 '6단계 특성 2개+ 상태 보스 처치 10' · 잠옷 = 4장 클리어, 체력 40% 미만 공격 +30% / 체력 -30%.
- **코드**: `Game/CostumeFx.cs`(ExecuteHeal · ShadowStrike · SandVortex · LowHpAura, 플립북 `Resources/FX/anim/bk_cos_*` 108장 = 포크가 `Art/tools/blender_fx_costume.py`로 구움, GIF `docs/images/costume_fx/`) · PlayerController(LowHpDamageBonus · ReloadTimeAdd · ShadowStrike · Heal · SandVortex · 처치 카운터 · `WearCostumeModel`) · Effects.CondOk(처형 범위) · Enemy.Die(스카프 카운터) · Meta(FacilityUnlockChapter 의상실 1, 카운터 2종) · Tables(CostumeDef.ModelKey).
- **스토리**: `StoryRun.FaintLines/WakeLines` 장별(1장만 대사, 2~5장 빈칸 = 사용자가 줄 예정). VillageStory ch1 = 기억의 방 해금 → 뻐꾸기 "아 그러고보니 손님이…" → 의상실 다리(`Bridge_Wardrobe` reqChapter 2→1, 씬 직접 수정) · 해금 → 화살표 둘(`GuideArrow` 여러 개 지원, `keep`). ch2_tailor 대화 삭제. 의상실 문 첫 사용 = `VillageStory.TailorMeet`(사용자 대사 4줄) → 의상실. 시설 첫 진입 안내 `GameUI.FacilityTutorial`(기억의 방 · 의상실 각 1회).
- 컴파일: Unity 없이 dotnet으로 확인(오류 0). 새 파일이 Unity csproj에 아직 없어서 임시 csproj로 검사(스크래치 `mkcheck.py`) — Unity 열면 자동 해결.
- **기획서**: `docs/1159_코스튬_기획서_20261124_정승운_Ver04.pptx` (생성기 `docs/tools/build_costume_ppt.js`, 섹션 5개 11장). 시안 `docs/images/costume/lineup_a/b.png` · `cos_400X.png`(크롬 ChatGPT, 받기 = 페이지에 원본 크기 오버레이 → 줌 캡처 조각 → `docs/tools/stitch_tiles.py`).
- **Hyper3D 코스튬 모델**: A자세 원화 `Art/tripo_input/costume/cos_400X_apose.png` → Rodin Gen-2.5 Medium 3만 → rigging human FBX → `Art/models/incoming/hyper3d_costume/<Id>/` (+ `preview.png`, 비교 시트 `docs/images/costume/model_compare.png`). 4002~4007 리깅 완료, 4008(잠옷)은 형태만(`base_unrigged.fbx`) — 이전 계정 크레딧 소진.
  Unity용 복사: `Assets/Art/Characters/CatHero/Costumes/<ModelKey>/` (4002~4007) + 빌더 메뉴 `Cat_Tang/코스튬 모델 프리팹 만들기`(`Editor/CostumeModelBuilder.cs` → `Resources/Costumes/<ModelKey>.prefab`, 머리 뼈 높이 맞춤, 아직 안 돌림).
- **▶ 다음 작업 (사용자 마지막 요청)**: "비율이 원본이랑 너무 다름 → 원본 비율에 맞춰 새로 넣은 아이디(Hyper3D 새 계정)로 다시 만들기". 측정 도구 `Art/tools/blender_measure_rig.py`(원본 `CatHero.fbx` vs `4002/base.fbx` 뼈 높이 출력) 만들어 둠, 결과 아직 안 봄. 원본은 머리 · 모자 크고 다리 짧은 땅딸한 체형, 새 모델은 몸이 길고 머리가 작음.
  방법 후보: (1) 측정한 비율(머리 크기 · 다리 길이 · 몸통 폭)을 GPT A자세 원화 프롬프트에 반영해 다시 그리고 Rodin 재생성 (2) 지금 모델을 블렌더에서 뼈 비율로 변형 → `rodin_create_uploads kind=model` → 새 계정으로 리깅. 다시 만든 뒤 원본과 비교 시트 다시 보여 주기 → Unity 복사 · 빌더 실행 → 기획서 3D 모델 장 그림 교체.
  그 밖: Unity 열어 실제 1장 클리어 흐름 · 재봉사 대화 · 시설 안내 · 코스튬 효과 플레이 확인, 커밋.

## 2026-11-23 (3) — 첫 대화 · 꿈1 뒤 대사 · 길 안내 화살표 · 기억의 방 갈래 트리 (커밋 안 함)
- **첫 패배 뒤 뻐꾸기 대화**(intro) 끝을 사용자 대사로: 고양이 "난… 이 마을을 벗어나야 해…" → 뻐꾸기 "…어째서? 같은 짓을 반복하는 거지?" → "정 그렇다면 저쪽으로 나가 봐. …" → 카메라가 시간의 문 비춤 → **화살표**(`UI/GuideArrow.cs`: 대상 위에서 통통 튐, 화면 밖이면 가장자리에서 가리킴, 문을 쓰면(`Reached`) 또는 멀어졌다 다가가면 사라짐).
- **꿈1 뒤**(ch1): 혼잣말 "젠장… 또 이상한 꿈을 꿨군…"이 대화 첫 줄로 붙음 → 뻐꾸기 "저런… 악몽을 꾼 모양이네… 방을 하나 마련해 두었으니 올라가 봐." → 고양이 "저 시곗바늘은 대체 무슨 의미가 있는 거지?" → 뻐꾸기(**의미심장** 표정, 새 그림 `Portraits/cuckoo_knowing.png`, GPT · 원본 `Art/portraits_src/cuckoo_knowing_src.png`) "…때가 되면 다 알게 될 거야." → 기억의 방 문 비춤 · "기억의 방 해금" → 안내 창 "시간의 조각으로 능력 강화 / 더 강해진 기억으로 재도전" → 화살표. 그 뒤 장의 튜토리얼 · 해금 연출은 사용자 요청대로 아직 안 만듦(2 · 3장은 예전 대사 + 다리만).
  개발용 `VillageStory.Preview("intro" | "ch1", wake)` = 기록 없이 재생. 확인 프레임 `docs/images/check/village_intro/`.
- **기억의 방 갈래 트리** (사용자 확정: 갈래 트리 · 다 찍으면 ~+35% · 기술 노드 = 생존 보험 + 보상 선택폭 · 장 클리어마다 층): `Memory.csv`에 Branch · Floor · Req 열. 화력 · 생존 · 손재주 × 4층, 층 n = n장 클리어, 위층은 Req 노드(없으면 같은 갈래 아래층 하나) Lv 1 이상. 예전 노드 Id 그대로 이어 받고 최대 단계가 줄면 자름(`Meta.Level`).
  기술 노드 4: 넓어진 선택(레벨 업 선택지 +1) · 한 번 더(1회에 한해 쓰러지면 체력 30% 부활 + 1.5초 무적, 판마다 다시 채워짐) · 다시 뽑기(1회에 한해 보상 다시 뽑기, 판마다 다시 채워짐) · 보스의 선물(보스 보상 +1). 전부 찍으면 공격 +15% · 치명 +6% · 치명 피해 +30% · 체력 +60 · 방어 +6 · 회피 -15% 등, 총 비용 ≈ 1,900 조각.
  화면: 톱니 소켓(잠김 · 열림 · 금빛) + 노드 아이콘 17종(GPT 시트 → `Resources/UI/Memory/`, 원본 `Art/ui_src/`) + 이음선(아래 노드를 찍으면 금빛) + 오른쪽 설명 판(현재 Lv · 다음 Lv · 강화 버튼). 문구는 사용자 요청대로 짧은 명사형.
  **판 안 확인 (임시 레벨로 판 진입)**: 체력 100→140 · 공격 ×1.13 · 방어 4 · 발동 ×1.1 · 이동 ×1.03 · 열기 +8% · 회피 재충전 −15% 전부 반영, 레벨 업 선택지 4개, 치명타 맞으면 체력 30%(42/140) · 무적으로 버팀(1회), 다시 뽑기 버튼 → 새 4장 · 버튼 사라짐, 보스 보상 고른 뒤 남은 카드로 "보스의 선물 · 추가 선택". 확인 캡처 `docs/images/check/memory_tree5.png`. 저장 슬롯은 테스트 뒤 원래대로(0장 · 0조각).

## 2026-11-23 (2) — 꿈1 배색 변주 · 맥 빌드 (커밋 안 함)
- 사용자: "꿈1(수채화) 컷이 배색 · 질감이 다 같아서 글리치를 넣어도 긴박하고 몽환적인 느낌이 안 남 → 세피아 · 색반전, 다른 배색 컷 끼워 넣기, 디졸브에도 섞기 · 추가 컷은 GPT로" · "맥북 빌드 뽑게 셋팅".
- **배색 `MGrade`** (`StoryMontage.cs`, 셰이더 `UIBlur`에 `_Sepia · _Invert · _Duo(_DuoA/_DuoB) · _Hue · _Contrast`, 감마 공간 계산): 프리셋 Sepia · Negative · OldNegative · Cyanotype · Blood · Verdigris · Ink · Shift(도).
  쓰는 자리: `MShot.grade`(컷 전체 · 바탕 종이색도 CPU로 같이) · `MLayer.grade`(겹쳐 비치는 잔상만) · `MShot.tinGrade`(빠져나가는 앞 장면이 그 배색으로 물들며 디졸브 · 물감 덮기 · 줌 · 휙) · `MShot.flips`(컷 안 잠깐 뒤집힘) · 큐 flash 음수 = 화면 음화 번쩍(`CatTang/UIInvert`, Blend OneMinusDstColor).
  몽타주 `gradeChance`(배색 안 정한 전환이 무작위 배색) · `gradeGlitch`(글리치 띠 일부가 음화 · 세피아 · 청사진) — 꿈1은 0.35 / 0.3.
- **꿈1 컷 배치** (`DreamCuts.cs` 도우미 G · T · F · LG · Neg · Alien): 노인 손 = 세피아 · 먹 · 음화 / 아이 손 = 원색, 총성마다 음화, 박동마다 음화, 끼어들기 컷 배색 뒤섞음.
- **새 다른 배색 컷 (크롬 ChatGPT)** → `Resources/Story/Dreams/dream1/`: night_ws(남색 밤 작업대) · ink_wind(먹 + 주홍 초침) · teal_corridor(녹청 복도) · gold_child(주황 · 코발트 거대 문자판 앞 아이) · chalk_watch(검은 종이 분필 회중시계) · cyan_heart(청사진 심장 시계). 원본 `Art/story_src/dream1_*.png`.
  사용자 지적("움직이는 건 연속 동작 이미지 or 오브젝트 따로 뽑아 애니메이션, 컷 많이")대로 **움직이는 건 셀 · 부품으로**: ink_wind → _b → _c(꼭지 · 손가락 · 초침 한 단계씩, 도우미 `Ink`) · cyan_heart ↔ _b(이완 ↔ 수축, `Heart`) · gold_child → _b → _c(바늘 12시 → 빛 폭발, `Child`) · chalk_bg + chalk_watch_obj(분필 시계만 밝기 → 알파로 오려 냄, 사슬 꼭대기 축 진자, `Chalk`). 셀은 원본을 첨부해 "같은 구도, 동작만 한 단계" 편집으로 뽑음. 꿈1 = 90컷(전 64).
  확인: 플레이 녹화 `docs/images/check/dream1_grade/`(372장) — 오류 없음, 새 배색 컷 · 음화 · 세피아 · 배색 글리치 조각 보임. 분필 컷 바탕이 잔상(echo) 때문에 살짝 회갈색으로 보임 — 거슬리면 그 컷만 echo 끄기. **귀로 · 실제 꿈 흐름(장 클리어 → 기절)으로는 안 봄.**
- **스토리 연출 전용 곡** (사용자: "두 영상 연출 다 효과음 빼고 전용 노래(제미나이), 재생 중 인게임 노래 X"): `Music.Story(이름)`(인게임 곡 일시정지 · 전용 곡 처음부터) · `Music.StoryEnd(페이드)`(전용 곡 줄여 끄고 인게임 곡 멈춘 자리에서 + 음량 되돌림 — 꿈 → 마을 곡이 0으로 남던 것도 고침). `StoryMontage.sfx = false`로 컷 · 박자 · 전환 효과음 끔.
  곡: Chrome Gemini → `Art/audio_src/gemini/story_opening.mp4`(62.6초) · `story_dream1.mp4`(59.4초) → −16 LUFS 48kHz OGG `Resources/Audio/story_opening` · `story_dream1`. 오프닝 재생은 `StoryRun1.PlayOpening(ov)`로 뺌(녹화 도구 `IntroDemo.RecordOpening` · `RecordDream`).
  고친 버그: 몽타주 그림 캐시가 파괴된 스프라이트를 붙잡아 두 번째 재생부터 그림 대신 자리 표시 글자만 나옴 → 파괴됐으면 다시 만듦.
  확인 영상(곡 입힘): `docs/videos/opening_v2_music.mp4`(28.8초) · `dream1_watercolor_v3_music.mp4`(41.4초). 영상의 소리는 곡 파일을 녹화 시각에 맞춰 입힌 것 — 게임 안 실제 재생 소리는 귀로 확인 안 함.
- **동화책 구간 배색 · 전환 섞기 · 글리치 줄임** (사용자: "동화 부분 배색 너무 비슷, 글리치랑 물감 트랜지션 적절히 — 2번째 스토리 전체" → "글리치 너무 많음, 적당히"):
  동화책 파노라마 · 대결 컷을 쪼개 구간마다 배색(원색 → 청록 돌림 → 달밤 `MGrade.Moon` / 달밤 → 황토 판화 `MGrade.Ochre`), 사이 전환 = 물감 덮기 ↔ 조각 섞기.
  꿈 전체: `StoryMontage.mixTrans`(0.6) — 0.3초 이상 컷의 끊기 · 디졸브 · 줌을 물감 덮기 ↔ 조각 섞기로 번갈아 바꿈(씨앗 고정, 짧은 셀 컷은 끊기 그대로). 글리치: 무작위 빈도 1.5 → 0.6, 박자 글리치는 흔들림 ≥ 18(`glitchCueMin`)만, 전환 글리치는 조각 섞기에만 0.15초.
  확인 영상 `docs/videos/dream1_watercolor_v4_music.mp4`(41.9초).
- **꿈 → 깨어남 → 해금 연결** (사용자: "멈춘 손 없애고, 젠장…또 이상한 꿈을 꿨군… 으로 이어주고, 해금 구조 스토리랑 이어서"): 꿈 제목 카드 뺌. 흐름 = 장 보스 첫 클리어(`ChapterClear`, 이번 판 끝 장 · 그 꿈 안 봄) → 기절 → 꿈 몽타주(전용 곡) → 하얗게 → 마을 깨어남(`VillageWake`) → 조각 토스트 → 혼잣말 `StoryRun.WakeLine` → NPC 대화(`VillageStory`, ch1 첫 줄 "……또 꿈이다" 빼고 이어 붙임) → 그 NPC 시설이 이번 장에 열렸으면 문을 비추며 "마을에 '…'이(가) 열렸다"(`RevealFacility`: 뻐꾸기 → 기억의 방, 대장장이 → 탄약 공방, 재봉사 → 의상실, 보안관 → 총포상). 다리 이음(2 · 3장)은 그대로. **플레이로 흐름 전체는 안 돌려 봄**(실제 저장에서 대화 기록이 남아서). 꿈 2 ~ 5는 그림이 없어 자리 표시(물감 번짐 + 조각 설명).
- 빌드 정리: `python docs/tools/package_builds.py [windows|mac|all]` → `Game/Builds/dist/1159_<날짜>_<OS>.zip` (맥 zip은 실행 비트 넣음).
- **맥 빌드**: 에디터에 Mac Build Support (Mono) 설치함(Unity Hub CLI). 메뉴 `Cat_Tang/빌드/맥 (Intel + Apple Silicon)` · `Cat_Tang/빌드/윈도우` (`Editor/BuildMenu.cs`) → `Game/Builds/macOS/1159.app` + `처음 실행.command`(xattr · chmod · ad-hoc 서명) + `읽어 주세요.txt`. 윈도우에선 Mono만, 서명 없음 → 맥에서 처음 한 번 그 스크립트. 배치 모드 `-executeMethod CatTang.EditorTools.BuildMenu.BuildMac`.
  번들 ID가 템플릿 값이면 `com.cattang.game1159`로 바꿈. 표 CSV 복사(`TablesBuildCopy`)는 맥이면 `.app/Contents/Data/Tables`.
  모듈을 에디터가 켜진 채 설치하면 빌드가 "성공"으로 찍혀도 `Build target 'StandaloneOSX' not supported`로 .app이 안 생김 → 에디터 재시작 필요. 재시작 뒤 배치 모드로 빌드함.
  **빌드 완료 (2026-11-23)**: 윈도우 1206MB(빌드 점검 `-smoke` 오류 0) · 맥 1223MB(Intel + Apple Silicon, 표 13개 복사) → 배포 zip `Game/Builds/dist/1159_20261123_Windows.zip`(564MB) · `1159_20261123_macOS.zip`(572MB). **맥에서 실제로 실행해 보진 않음.**

## 2026-11-22 (5) — 쌍둥이 연출 액션 · 미지의 존재 구도 · 눈빛 꼬리 눈에 붙이기 · 시침 걷기 (커밋 안 함)
- 사용자: "크로노 & 템포 너무 가만히 서 있음, 컷 전환 더" · "미지의 존재 얼굴을 일러가 안 가리게, 중간에 한 번 사라지는 문제" · "눈에 붙는 레이저 궤적 — 단색 + 광효과, 눈 위치 정확히" · "시침 걷는 모션이 없는 것 같음".
- **쌍둥이 연출** 컷 5개: 시계바늘 부감 착지(1.0) → 크로노 대검 근접(Duel 0.55) → 템포 찌르기 근접(Duel 0.5) → 옆에서 둘이 자리 바꾸며 교차 베기 + 느린 화면(Profile 0.8) → 낮게 다가가는 정면 결정 자세(Push 1.35).
  새 컷 `Duel`(한 녀석만 바깥 3/4에서), 도구 `Focus` · `Twin(hour)` · `Swap`(스쳐 지나가 어깨 맞댄 자리 ±2.3m에서 멈춤) · `FaceFront`. 새 클립 `Ch4M_HourIntro/MinuteIntro`(공중 웅크림 → 착지) · `Ch4M_HourPose/MinutePose`(상체 세운 결정 자세, 상태 "IntroPose").
  주의(고친 것): 연출 시작 때 모델 Mesh 위치를 저장했다 되돌리면 2m 뜸 (생성 프레임엔 발 맞춤 전 값) → 방향만 되돌림. 테스트 방(1장)은 좁아서 마지막 컷 카메라가 바위벽에 묻힘 → 실제 4장 보스방(`IntroDemo.RunReal`)에서 확인함.
- **미지의 존재**: 마지막 컷 0.4초 깜빡임(Flicker) + 얼굴 앞 유리 폭발이 "사라짐"으로 보였음 → 빼고 등 뒤에서 깨지게. `faceLeft`(마지막 컷에서 보스를 화면 왼쪽 1/3) + 글리치 카드 일러를 오른쪽 끝 · 이름을 얼굴 아래 · 어둡게 덜.
- **눈빛 꼬리 `EyeTrail` (LaserFx.cs)**: 단색 심지(알파) + 3.6배 넓은 빛 번짐(`CatTang/AdditiveGlow`) 두 겹, 그라디언트 대신 한 색(4장 빨강 · 5장 자홍).
  눈 자리 = 머리 뼈에 붙임 + **텍스처에 칠한 눈 색으로 찾기**: 스킨 메시를 구워(생성 2프레임 뒤) 머리 둘레 정점마다 텍스처(GPU로 읽음) 색을 보고, 눈 색 점들을 덩어리로 묶어 같은 높이 · 가운데를 사이에 둔 작은 덩어리 한 쌍(없으면 가운데 외눈). 모델마다 한 번 계산해 머리 뼈 로컬로 저장.
  확인: 4 · 5장 모델 26개 정면 · 옆 그림 `docs/images/check/eyes_found_all.jpg` — 대부분 정확, 분침(얼굴 전체가 빨간 가면) · 5장 캐스터(칠한 눈 없음)는 머리 뼈 높이 얼굴 앞면으로. 수리공+는 고글 아래쪽에 조금 걸침. 시간의 주인 · 2등신 고양이처럼 y/fwd/gap을 직접 준 몸은 예전 방식 그대로.
  주의: 구운 메시 정점은 `BakeMesh(m, true)` + `transform.localToWorldMatrix`여야 함 (이 모델들은 Mesh에 ×188 배율).
- **시침 걷기**: `Ch4M_KnightWalk`가 대기 자세 기준으로 앞뒤를 뒤집어 왼발이 내내 뒤에 남는 절뚝 걸음이었음 → `Ch2Motion.Walk(..., neutralLegs: true)`로 똑바로 선 값(0.62) 기준 + 보폭 0.3. 4장 기사 · 집행관도 같은 클립.
- 확인용 도구: `IntroDemo.RunReal(폴더, 장)` 실제 보스방 입장 흐름 촬영 · `RunMoves`에 "Run"(걷기 유지) · "Spin"(눈빛 꼬리 켠 채 회전) · `EyeCalibSpawn` + `EyeCalib` 눈 위치 그림.
- GIF: `docs/images/boss_intro/boss_intro_ch4_boss.gif`(실제 4장 보스방) · `boss_intro_ch5_mid.gif` · `eyetrail_chrono.gif` · `chrono_walk.gif`.

## 2026-11-22 (4) — 연출 효과음 생성 · 커밋
- audio-studio 작은 효과음 모델(`stable-audio-3-small-sfx`, 3.3GB) 다운로드 끝 → `gen_audio.py sfx intro_riser intro_whoosh intro_impact intro_stinger_1~5 intro_flip intro_glitch` 로 11개 생성(`Resources/Audio/Sfx/intro_*.ogg`, whoosh는 _1/_2 두 가지). 코드가 `Sfx.Has`로 파일을 찾아 자동으로 예전 소리 대신 씀 — 코드 수정 없음.
- 생성 명령 (Git Bash): ffmpeg는 winget으로 깔려 있지만 PATH에 안 잡혀 있음 → 직접 지정해야 함.
  `F=$LOCALAPPDATA/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.1-full_build/bin; PATH="$F:$PATH" AUDIO_STUDIO_FFMPEG="$F/ffmpeg.exe" AUDIO_STUDIO_SA3_MODEL=sm-sfx C:/Project/audio-studio-mcp/.venv/Scripts/python.exe Art/tools/gen_audio.py sfx <이름…>` (GTX 1060에서 하나 1~2초).
- 확인: Unity 임포트 · 길이 정상, 음량(최대 -0.1 ~ -1.5 dB, 평균 -8 ~ -20 dB) 정상. **귀로 안 들어 봄** — 소리 느낌 · 브금 연계(줄임 → 정지 침묵 → 곡 처음부터)는 플레이해서 들어 볼 것. 마음에 안 들면 `gen_audio.py` SFX 표 문구나 SEED를 바꿔 다시 생성.
- audio-studio MCP 서버는 세션 시작 때 연결 시간 초과(30초) — 생성은 `gen_audio.py` 직접 실행(프로세스 안)으로 하면 됨.
- 아직 확인 안 됨: 장별 실제 보스방에서 연출(촬영은 전부 1장 테스트 방), 일루시오 실제 전투, Unity 재시작 뒤 첫 플레이 맵 안 지어지는 문제(원인 모름).

## 2026-11-22 (3) — 모든 보스 '등장 장면' · 카메라 각도 다양화 · 일루시오 끝 구도
- 사용자: "등장하는 장면에서부터 시작해서 보스가 나타난다는 걸 명확하게(모비 글래스처럼), 모든 보스 — 소환 · 땅에서 · 점프 등 고유 이미지와 맵 컨셉에 맞게" · "카메라 무빙이 너무 유사, 다각도로" · "일루시오 극장 카드: 멈추는 타이밍과 연결 — 얼굴이 옆에 확실히 보이고 일러는 옆에".
- **등장**: `BossIntroDef.entryUp`(+ = 하늘에서, 여러 마리면 뒤 녀석 3m씩 더) · `entryBack`(멀리서) · `emerge`(땅 아래) → 비트 `Enter(dur, drop|descend|charge|rise)`, 착지 이펙트는 `Later(초, …)`, 예고 원 `MarkLandings`, 숨김 등장 `Hide`. 카메라 기준점이 보스를 수평으로 따라감(등장 돌진).
  와인더 잭 = 착지 표시 → 떨어져 쿵 / 달리오스 = 녹은 땅이 터지며 솟음 / 컨덕터 반 = 증기 뿜으며 달려 들어옴 / 로코모스 = 멀리서 돌진 / 일루시오 = 연기 속 / 모비 글래스 = 수면 위로 / 기로틴 = 붉은 글리치 순간이동 / 쌍둥이 = 착지 원 두 개 → 시곗바늘처럼 내리꽂힘 / 미지의 존재 = 거울 깨지며 / 시간의 주인 = 소환 원 → 천천히 내려옴 → 공허 돔.
- **새 각도**: BehindBoss(보스 등 뒤 어깨 너머) · Profile(완전 옆 망원) · WormEye(발밑 수직 올려다봄) · HighOrbit(높은 새의 눈) + 보스별 다시 배정 (마지막 컷은 정면 유지, 컷 길이는 그대로라 연출 클립 박자 유지). 컨덕터 반 끝 = QuickDraw.
- **일루시오 끝**: 새 컷 Portrait(얼굴을 화면 왼쪽 1/3에 두고 파고듦) + 극장 카드 다시 — 커튼은 양옆 테두리로만 닫힘, 얼굴에 스포트라이트, 일러스트 오른쪽, 간판은 얼굴 아래.

## 2026-11-22 (2) — 일루시오 새 모델 동작 · 연출 · 연출 소리 자리 · 브금 연계 · audio-studio
- **일루시오 전투 동작**: 패턴마다 `Cast(시간, 상태)` → 새 클립 `Ch3M_Illusio{CardFan, SwordBox, Doves, MirrorBox, Summon, Conjure}` (거울 상자 연사 = Attack 카드 튕기기). 패턴 시작 Attack 트리거는 끔(anim ""). 예전 키트 모델이면 PuppetAnim 그대로.
  이 리그는 Arm Down-Up 1 ≈ 어깨 높이라 팔 값을 크게(1.2 ~ 1.95) + 머리 위 자세엔 어깨 들기(RS.DU). 모자 들기는 그래도 어깨 조금 위까지만 올라감.
- **일루시오 연출 새로**: 컷 LowRise 1.0 → HeroOrbit 0.7 → Spiral 0.7. 보스 숨김(`Hide`) → 0.25초에 섬광 · 연기 속에서 인사하는 채로 등장 → 양옆 거울 분신(`Doubles`: 모델 복제가 같은 동작을 따라 하며 깜빡이다 유리처럼 깨짐) → 카드를 카메라로 내던지는 정면 자세에서 정지. 클립 `Ch3M_IllusioIntro` 박자 다시 맞춤. HeroOrbit 거리 · 화각을 조금 물림(모자 잘림).
- **브금 연계** `Music.Duck/Restart`: 연출 시작 0.3까지 줄임 → 정지 순간 0(한 박자 침묵) → 카메라가 돌아올 때 보스 곡을 처음부터 0.9초에 걸쳐 키움 (스킵하면 0.4초). **귀로 안 들어 봄.**
- **연출 효과음 자리**: `intro_riser`(시작) · `intro_whoosh`(휙 전환) · `intro_impact`(정지) · `intro_stinger_1~5`(장별 카드) · `intro_flip`(발차판) · `intro_glitch`(미지의 존재). 파일이 있으면 쓰고 없으면 예전 소리(`Sfx.Has`). 생성 표는 `Art/tools/gen_audio.py` SFX에 추가해 둠 → `gen_audio.py sfx intro_riser intro_whoosh …`.
- **audio-studio**: 사용자가 설치를 돌려 `.venv` · PyTorch 2.7.1+cu126 · CUDA 됨. 이 PC GPU = **GTX 1060 6GB** (예전 메모의 RTX 5060 아님 → cu128 바꿀 필요 없음). Hugging Face 로그인 안 됨 → 가중치 없음, ffmpeg 없음. 프로젝트 `.mcp.json`에 `audio-studio`(stdio, `AUDIO_STUDIO_SA3_MODEL=sm-sfx`) 등록 — 지금은 가중치가 없어 연결 시간 초과.
- 확인용: `IntroDemo.RunMoves(folder, 장, 중간?, "상태,…")` (두뇌 끄고 상태를 하나씩 재생하며 찍음). GIF `docs/images/boss_intro/boss_intro_ch3_mid.gif` · `illusio_moves.gif`.
- 주의: 이번 Unity 재시작 뒤 첫 플레이에서 RunManager가 꺼진 채 시작해 맵이 안 지어진 적 있음(원인 모름, 씬 파일은 그대로) — 그땐 RunManager.Start를 직접 불러 해결.

## 2026-11-22 — 등장 연출 3차 (코드만, **컴파일 · 촬영 확인 안 됨** — Unity 도메인 리로드가 13분 넘게 멈춰서 중단)
- 사용자: 모비 글래스 앞모습으로 끝 + 일러 가로로 · 일루시오 Hyper3D로 다시 · 미지의 존재 연출 다듬기(맥크리 '최고의 플레이' 참고) · 줌 · 달리 · 극단적 화각 · 로우 앵글 · 가감속 전체 강화 · 스페이스 건너뛰기를 게임식(아이콘)으로 오른쪽 위.
- `BossIntro.cs`: 새 컷 Vertigo(돌리 줌) · HeroLow(낮은 3/4 정면) · HeroOrbit(허리 높이 근접 궤도) · QuickDraw(정면 낮은 앵글 확 조임). 모든 컷 화각 폭 확대(광각 ~82 ↔ 망원 ~17), 궤도에 달리인, 움직이는 컷은 `SpeedRamp`(빠름 → 느림 → 빠름).
  고래 끝 = HeroLow (정면 그대로는 고래가 납작한 날개처럼만 보임), 시간의 주인 끝 = Vertigo, 미지의 존재 = OverShoulder → HeroOrbit(모자 챙 · 리볼버 3바퀴) → QuickDraw(두 자루 뽑아 겨눔 · 총구 섬광 · 느린 화면).
  건너뛰기 = 오른쪽 위 키캡(SPACE) + "건너뛰기 ▶▶", 숨 쉬듯 깜빡, 누르면 키캡이 눌림.
- `IntroPoser.cs` (새): 클립을 따로 못 만드는 모델(거울 고양이 = 주인공 컨트롤러)에 근육 자세를 런타임으로 덧씀 (EnemyMotions 표기 그대로, 가감속) + 물체 돌리기(Spin, 리볼버).
- `BossIntroCards.cs`: 거울 금 카드가 가로 그림이면 가로 배치. `Resources/UI/BossIntro/ch3_boss.png` = 가로 고래(ChatGPT, 원본 `Art/illust_incoming/ch3_boss_wide.png`, 예전 세로는 `ch3_boss_portrait_feathered.png`).
- **일루시오 Hyper3D 완료**: 정면 A자세 원화(`Art/illust_incoming/ch3_mid_front.png`, ChatGPT) → Hyper3D MCP `rodin_generate`(Gen-2.5 Medium, 3만 폴리, generation 5f7131d1…) → `rodin_generate_rigging` bone_template=human(FBX, 548ab791…; human_Unity + unity 내보내기는 "표준 휴머노이드 아님"으로 실패) → `Art/Characters/Ch3/Ch3_MidBoss.fbx` + `_basecolor.png`, 원본 `Art/models/incoming/hyper3d_illusio/`.
  빌더 `Models3`에 ("Ch3_MidBoss", 3.6), 동작 = 분신술사 카드 튕기기 · 인사 + 연출 전용 `Ch3M_IllusioIntro`(인사 → 팔 펼침 → 카드 던지기 1.52). `EnemyFactory.MidBoss3` = AttachModel("Ch3_MidBoss"), 없으면 예전 키트. 휴머노이드 아바타 정상, 모델 사방 렌더 `docs/images/check/illusio_model.jpg`.
  남은 확인: 실제 전투에서 패턴 동작(Ch3MidBoss.Cast는 PuppetAnim 전용이라 이제 아무것도 안 함 — 필요하면 Anim("Bow") 등으로), 분신(거울 상자 · 분신술사 호출)이 새 모델과 어울리는지.
- Hyper3D MCP: 프로젝트 `.mcp.json`에 `hyper3d` (http, https://api.hyper3d.com/api/mcp, OAuth) 추가 — 세션을 다시 열고 승인 · 브라우저 인증해야 도구가 보임.
- audio-studio: 이 PC엔 없었음 → `C:/Project/audio-studio-mcp`에 클론만 함(설치 스크립트 실행은 권한 확인에서 막힘, 사용자가 직접 실행 필요).

## 2026-11-21 (2) — 등장 연출 보스별 차별화 · 카드 10종 · 연출 전용 동작 (커밋 안 함)
- 사용자: "보스마다 다른 카메라 워킹 + 액션 · 이펙트로 차별점, 데더다랑 너무 비슷하니 변주" · "새로운 동작 만들어도 됨, 연출 전용으로".
- `BossIntro.cs` 다시 짬: 보스 표에 컷 순서 + **비트**(`At(컷, 진행률, 람다)` — 보스 동작 · 이펙트 · 소리 · 흔들림 · 느린 화면 · 실제 돌진 `Lunge` · 바닥에서 솟구침 `Emerge`). 새 컷 13종(Feet · Showdown · Chase · JumpCut · Stutter · ClockSweep · OverShoulder · Rise · Snap · Pullback · Spiral · LookUp · Push).
- `BossIntroCards.cs` (새): 정지 카드 10종 — 현상수배 · 녹아내림 · 발차판(글자 플립) · 기차 띠 · 극장 커튼 · 거울 금 · 느와르 사선 · 흑백 반쪽 · 글리치 · 시계판 11:59. 보스별 배정은 `Def`의 `card`.
- 연출 전용 동작: `EnemyMotions.Intros` → `Ch1M_HunterIntro` · `Ch1M_GiantIntro` · `Ch2M_MasterIntro` · `Ch2M_GiantIntro` · `Ch4M_ExecIntro` · `Ch4M_HourIntro` · `Ch4M_MinuteIntro` · `Ch5M_BossIntro`, `EnemyModelBuilder.Moves`에 "Intro" 상태. **시각은 BossIntro 컷 길이에 맞춘 값** — 컷 길이를 바꾸면 클립 시각도 같이 고칠 것. 3장 둘(절차 애니) · 5장 중간(고양이 모델)은 클립 없이 Attack.
- 메뉴 `CatTang/적 전용 모션 다시 만들기 (클립 + 컨트롤러)`로 다시 만듦.
- 사용자: "마지막은 앞모습으로 멋있는 액션 자세에서 일러" → 마지막 컷은 전부 정면으로 끝남(Snap · Pullback · Spiral · Crash 옆 어긋남 제거, 쌍둥이 마지막 = Crash), 마지막 컷 동안 고양이를 숨김. 연출 클립은 정지 순간(느린 화면 감안)에 타격 자세를 붙잡도록 맞춤.
- 키 재기: 휴머노이드는 머리 뼈 높이(캡슐의 65 ~ 130%), 떠 있는 사신(HoverFloat)은 캡슐. 쌍둥이 R은 벌어진 거리의 55%만.
- 확인용 GIF 대신 프레임: `docs/images/check/intro10~13/`. **아직 안 본 것**: 장별 실제 보스 방(벽 · 경기장)에서의 각도, 중간 보스 술집 홀(1장)처럼 방 안에서 시작하는 경우.

## 2026-11-21 — 보스 등장 연출 · 보스 이름 · 칭호 (커밋 안 함)
- 사용자: 메인 · 중간 보스 전부 고유 등장 연출 — 원신 · 명조식 역동적 카메라 + 전환, 마지막은 데이브 더 다이버식 일러스트 + 정지. 메인 6초 · 중간 4초, 첫 만남 스킵 불가 · 이후 스페이스 스킵. 거대한 보스는 일러스트 상반신만.
- `BossIntro.cs` (새): 보스 표(`Def(chapter, mid)` — 영어/한글 이름 · 칭호 · 색 · 컷 순서 · 크래시 줌 소리), 컷 6종(Wide · LowRise · Orbit · Crash · Dutch · Top) + 전환 4종(Hard · Flash · Whip · Wipe), 정지 화면(timeScale 0, 색 띠 · 일러스트 · 영어 타이포 · 한글 줄), 가림막 자동 숨김, 하늘 안 보이게 최소 10° 내려다봄.
  크기 = 보스 루트 CapsuleCollider (스킨 메시 bounds · BakeMesh는 배율이 들쭉날쭉해서 버림). 4장 쌍둥이는 Enemy.All의 보스 둘을 같이 잡음.
- 연결: `RunManager.SpawnWave`(보스 · 중간 보스 만든 직후 yield), `Enemy.Update`/`TwinDirector.Update`는 `BossIntro.Playing` 동안 멈춤, `CameraFollow.Hold` · `GetFollowPose`, `BossBrain.HoldPatterns`, `Meta.IntroSeen/MarkIntroSeen`(슬롯별).
- 일러스트: ChatGPT(Chrome)로 10장 생성 · 투명 PNG 그대로 → `Resources/UI/BossIntro/<id>.png` (가장자리 페더 처리). 원본 `Art/illust_incoming/`. 3장 둘은 원화가 없어 게임 화면을 참고로 줌. 5장 보스는 컨셉 원화(망토 사신) 기준이라 게임 모델(갑옷)과 조금 다름.
- 이름 변경: DisplayName · Chapter.csv BossName · 토스트 · 5장 환영 이름. 4장 체력바 크로노 = 시침 · 템포 = 분침은 내가 정한 것 (사용자 확인 전).
- 확인용: `Dev/IntroDemo.cs` — 플레이 중 `IntroDemo.Run(folder, "1m,1b,...")`, 화면(UI 포함)을 프레임으로 저장. 결과 `docs/images/check/intro6~7/`.
- **아직 안 본 것**: 실제 런에서 각 장 보스 방(원형 경기장 · 벽 · 소품)에서의 카메라 각, 스페이스 스킵 직후 대시가 안 나가는지, 영어 글꼴(BlackHanSans 기울임 — 더 맞는 영문 글꼴 받을지 사용자 결정).

## 2026-11-20 (밤 늦게 5) — 땅 파동 · 땅 솟음 · 낫 눕히기 · 휘두르기 느리게 (커밋 안 함)
- `GroundWave.cs` (새): 내려찍기 파동 전용 — 돌진 꼬리처럼 땅에 붙은 띠(StylizedVfx, 꼬리부터 흩어짐) + 머리가 지나가는 자리마다 `Upheaval`(블렌더 바위 무더기, `Art/tools/blender_fx_upheaval.py` → `Resources/FX/Upheaval/fx_upheaval_0~2.fbx`, 솟음 0.11초 → 0.75 ~ 0.9초 남음 → 가라앉음, 틈의 빨간빛이 식음). 칼끝 박힌 자리에도 Upheaval.
- 낫(진자 기사 · 막보) 눕혀 수평으로, 막보 Reap 250°. 휘두르기 베기 시간 ×1.35 (기사 0.23 · 시침 0.32 / 회전 0.65 · 막보 0.3).

## 2026-11-20 (밤 늦게 4) — 범위 통일 · 지면 폭발 · 내려찍기 파동 · 막보 양손 낫 (커밋 안 함)
- 범위 통일 (사용자: 데미지 · 이펙트 · 텔레그래프 하나로, 이펙트 범위 = 데미지): `WeaponSwingRig.EffectRadius` · `Sector(swing, fwd)` · `InSector` — 진자 기사 휘두르기 · 막보 Reap180 부채꼴 / 유령 돌진 길 폭(= 칼빛 반지름 × 2) / 시침 대검 회전 원(칼빛 반지름 7.0m) / 내려찍기 파동 폭 = SlamW.
  → 범위가 예전보다 넓어짐(기사 200°, 막보 240° 등): 플레이로 난이도 확인 필요.
- 지면 폭발 `StylizedFx.GroundBlast` + `BlastRed` (레퍼런스 모양 · 빨강): 섬광 반구 · 불꽃 왕관 2 · 가시 꽃잎 · 충격 고리 2 · 불티. `WeaponSwingRig.Impact`가 회색 연기 덩이 대신 이것.
- 내려찍기 파동 `Ch4Twins.SlamWave`: 바닥 칼빛 + 서 있는 칼빛 벽(2.6m)을 길 따라 날림, 지나간 길에 작은 지면 폭발.
- 막보 낫: 55% 크기 · 양손(`twoHand`, 왼손 IK) · 쉬는 자세 = 몸 앞 대각선.
- WalkDemo.RunHeavy: 촬영 동안 대상 외 적은 계속 지움(KeepClear).

## 2026-11-20 (밤 늦게 3) — 깨진 메타 66개 복구 · 칼빛 v3 · 무거운 가감속 · 막보 떠다니기 (커밋 안 함)
- **5장 에셋 메타 66개가 0으로 채워진 채 커밋돼 있었음**(7b57e4c부터) → Unity가 그 FBX · 텍스처를 못 불러옴 (막보 낫이 안 보인 원인, 5장 키트 · 무기 · 강화 부품 다수).
  프리팹 · 재질이 참조하던 GUID를 이름으로 짝지어(`X.prefab` ↔ `Kit_X.fbx`, `Facade_X` ↔ `Kit_Wall_X`, `Ch5_X_Toon.mat` ↔ `Ch5_X_basecolor`) 멀쩡한 메타를 틀로 다시 씀. FX_GlassShard 4개만 참조가 없어 새 GUID.
  임포트 설정은 기본값(Kit_BA_DialRing 틀) — 원래 설정이 달랐던 모델이 있으면 빌더 다시 돌릴 것.
- 칼빛 v3: `Art/tools/blender_slash_tex.py` → `Resources/FX/Tex/slash_flow.png` (결 · 노이즈 · 덩어리), `CatTang/SlashRibbon` 툰 단계 + 결대로 찢겨 사라짐, 휘두르는 동안 칼날 위 레이저 띠. 두께는 그대로.
- `WeaponSwingRig`: 감기 = 사인 가감속(천천히 들어 올림), 베기 = Heavy 곡선(가속 → 빠름 → 길게 감속), 몸 회전 · 숙임을 골반 35% · 허리 · 가슴 65%로 나눔.
- 막보: `HoverFloat.cs`(둥실 · 이동 방향 기울기), `Ch5M_BossIdle` · `Ch5M_BossWalk`를 발 안 딛는 떠 있는 자세로.

## 2026-11-20 (밤 늦게 2) — 큰 무기 휘두르기 · 랫키호테식 칼빛 (커밋 안 함)
- 사용자: "휘두르기 · 돌진 베기 다시 — 랫키호테 평타 이펙트 · 모션 분석해서 3D로, 돌리는 각이 너무 작아" · "시침 대검이 몸에 박힘" · "대검은 낮게 들고 크게, 압도적으로" · "낫 휘두르는 애들도 · 가감속".
- 랫키호테 평타 분석: 3타 호 360° · 240° · 350°, 베기 0.05 ~ 0.24초 ease-out, 칼빛 = 1/240초 칼끝 · 밑동 기록 · 칼끝 ×1.5 · 흰색 → 4겹 띠 → 식은 색, 투명이 아니라 가늘어지며 사라짐.
- `BladeStreak.cs` v2 + 셰이더 `CatTang/SlashRibbon` (정점 색 띠 겹): 위 방식 그대로. 모든 4 · 5장 칼빛이 바뀜.
- `WeaponSwingRig.cs` (새): 무기를 코드로 쥠 (메시 경계로 긴 축 · 얇은 축 잼, 오른팔 두 뼈 IK, 몸 전체 yaw · 숙임 · 한 걸음, 감기 → 멈칫 → ease-out 베기 → 유지 → 복귀, Impact = 히트스톱 · 흔들림 · 흙먼지 · 충격 고리).
  무기가 몸 메시에 붙은 모델(시간의 주인 Ch5_LordOfTime)은 BodyOnly — 몸 전체만 크게 돌림.
  붙은 곳: 시침(EnemyFactory.Boss4, 쉬는 자세 = 낮은 하단 자세) · `Ch4Twins` Slam(168° 수직 내려찍기 + 길 따라 바닥 터짐) · Spin(1.5바퀴) / 진자 기사 `EnemyCh4` 좌우 220° · 회전 / `Ch5Bosses` Reap180 240° · GhostReap 280°.
- `EnemyMotions`: 휘두르기 클립에 몸 전체 회전(허리 비틀기 1당 42°, Bake Into Pose) + ease-out 접선. 클립 다시 만듦.
- GIF: `docs/images/check/heavy_*.gif`. **못 한 것**: 시침은 팔이 0.9m라 양손 쥐기 불가 → 한 손 하단 자세. 실제 전투 루트에서 판정 박자 확인, 칼빛 색 · 굵기 손질.

## 2026-11-20 (밤) — 인계: 집행관 · 거울 고양이 사망 크기 · 돌진 조각 · 풀링(Codex 진행 중) (커밋 안 함, **컴파일 · 플레이 확인 안 함**)

### 이번 요청 (사용자 원문 요약)
1. 돌진 이펙트 튀는 오브젝트 중 잘 안 보이는 것만 조금 수정
2. 거울 고양이 죽을 때 갑자기 작아지는 것 수정
3. 진자 집행관: 멀면 전광석화로 이동 · 가까우면 그냥 걸어서 / 휘두를 때 4장 적처럼 레이저 리본 궤적 / 전광석화 사이 간격 조금 더 늘리기
   + **휘두르는 참격 모션 전부(모든 장 몬스터) 상체 회전력 써서 사실적으로 (지금 너무 정적)** + 4장 몬스터 이동할 때 눈 레이저 궤적 남는지 확인
4. 이해 안 되면 질문 · 5. 오브젝트 풀링은 섹션 나눠 최적 방향으로, **Codex에 맡김** · 휴지통 이동 허용 · 토큰 다 쓰기 전에 핸드오프

### 한 것 (코드만 — Codex가 같은 때 스크립트를 고치는 중이라 컴파일을 미룸)
- **거울 고양이 사망 크기**: `GlassShatter.cs` — `BakeMesh`는 부모 배율이 빠진 크기로 나옴(Afterimage.cs 주석과 같은 문제) → `TRS(…, smr.transform.lossyScale)`. 같은 실수가 `HeavyStep.cs`(발 점 구름) · `GroundFeet.cs`(바닥 틈)에도 있어 같이 고침. **HeavyStep은 이 버그 때문에 발 점이 몸 가운데로 쪼그라들어 있었을 수 있음 → 발자국 윤곽이 실제 발 크기로 나오는지 다시 볼 것.** GroundFeet는 발이 메시 원점 높이라 결과가 거의 같았을 것(시침은 바닥에 붙는 것 확인했었음) — 다시 확인.
- **돌진 조각 크기**: `DashTrail.cs` 스타일 표에서 작은 것만 키움 (모래알 0.12~0.2 · 증기 불티 0.09~0.15 · 석탄 0.18~0.28 + 밝게 · 레이저 바늘 0.09~0.14 · 은 조각 0.2~0.3 · 균열 유리 0.18~0.3 · 금 불티 0.08~0.12).
- **진자 집행관** `Ch4Bosses.cs` `Ch4MidBoss`: `BlinkFar = 7` — 고양이가 7m보다 멀 때만 전광석화(Forced의 BlinkChain 조건) · 안이면 지직거림 · 옆 튐 없이 보통 걸음(Move). 간격 `BlinkGap 0.16 → 0.32` · `BlinkGlitch 0.15 → 0.2` · `ChainEvery 2.2~3.6 → 3.0~4.5`.
  베기 이펙트 `FxBurst.Slash`(판 그림) → `BladeRibbon()` = `BladeStreak.Begin(오른손 뼈, 칼끝, 손잡이, 0.35, Ch4Base)`. 이 모델은 **무기가 몸 메시 한 덩어리**(무기 뼈 · Weapon_ 없음, 렌더러 "model" 하나)라 칼끝을 "아래팔 → 손 방향 1.5m"로 어림함 → **플레이로 리본이 실제 칼날을 따라가는지 확인하고 tipLocal 방향을 맞출 것** (칼날이 손에서 다른 축으로 뻗어 있으면 hand 로컬 축으로 다시 잡기).
- **휴지통**: 안 쓰는 예전 돌진 판(`FX_Dash_*.prefab` 6 + `bk_dash_*` 96장) 휴지통으로 옮김.

### Codex — 오브젝트 풀링 (백그라운드로 실행 중이었음)
- 명세 `Art/tools/specs/pooling_spec.md`, 로그 `Art/tools/specs/pooling.log`. 섹션: 0 공통 풀(`Scripts/Game/Pool/`) · 1 FxEffect/FxPart · 2 FxBurst · FxMesh · FxFlipbook · Tracer · StylizedFx · 3 EnemyProjectile · DamageNumber · Telegraph · 4 방 지을 때 미리 만들기 · 5 큰 연출은 그대로 · 6 FxPoolBench에 D) 실제 풀 측정.
- Claude가 고치는 파일은 금지 목록으로 줌(DashTrail · DashFx · HeavyStep · GlassShatter · GroundFeet · Ch4Bosses · Ch4Twins · EnemyCh4 · BladeStreak · LaserFx · Ch5MidBoss · MirrorCatSkin · Enemy · EnemyFactory · WalkDemo · Editor/ · 에셋 파일).
- **다음 세션 할 일**: ① 로그 끝의 보고(바뀐 파일 · 되돌림 목록 · "Claude가 고칠 것" · 위험한 곳) 읽기 ② `git diff`로 Codex 변경 검토 (특히 FxPart 원래 크기 · 회전 저장/복원, EnemyProjectile 필드 되돌림, Telegraph.Kill) ③ 컴파일 ④ FxPoolBench 돌려 A~D 비교 ⑤ 전투 한 판 돌려 이펙트 겉모습이 예전과 같은지 · 콘솔 에러.
  (Codex가 아직 안 끝났으면 `tasklist | grep codex`로 확인, 끝날 때까지 스크립트 컴파일 금지)

### 사용자 답 (이후)
- 4장 눈 레이저 궤적 = **빠르게 움직일 때만** 남는 게 맞음 (지금 설계 그대로 — 바꿀 것 없음).
- 휘두르기 모션에 **가감속도** 넣어 사실적으로.

### 휘두르기 손맛 — 코드 씀, **클립은 아직 안 만듦** (`Editor/EnemyMotions.cs` `Clip()` → `SwingFeel` · `StrikeTangents`)
- 이름에 Swing · Slash · Whirl · Reap · Stab · Cut · Slam · Smash · Cleave · Chop · Guillotine 이 들어간 모든 장 클립(감기만 하는 *Wind 제외)을 만들 때 자동으로:
  ① 몸통(허리 비틀기, 없으면 허리 숙임)이 가장 빠르게 바뀌는 키 = 치는 키, 그 앞 = 감은 꼭대기 ② 그 두 키 허리 값 ×1.25, 가슴 비틀기가 없으면 허리 ×0.55로 만들고 고개는 반대로 ×0.3
  ③ 운동 사슬 — 감은 꼭대기 뒤 키들을 다리 −0.02 · 허리 0 · 가슴 +0.02 · 목 +0.025 · 어깨 +0.03 · 위팔 +0.04 · 아래팔 +0.05 · 손 +0.06초 늦춤 ④ 몸통은 친 뒤 0.06초에 18% 넘쳤다 돌아옴, 치는 키 들어가는 접선 = 곧은 선(가속), 나머지 ClampedAuto(감속).
- **할 일**: Codex 끝난 뒤 컴파일 → `CatTang.EditorTools.EnemyMotions.MakeAll()` (메뉴 `CatTang/적 전용 모션 클립 만들기 (1 ~ 5장)`) → 콘솔에 `[EnemyMotions] 모르는 약어` · `[Ch2Motion] … 못 찾음` 경고 없는지 → 휘두르는 적(4장 진자 기사 · 초침 자객 · 5장 균열 자객 · 3장 거울 검객 · 보스 낫 등)을 WalkDemo 식으로 꺼내 `Anim("SwingR")` 등 재생해 GIF로 확인. 어색하면 Delay 값 · 1.25 · 0.18을 조정.
- 주의: ③으로 키 시각이 바뀌어 **판정 타이밍과 칼빛(BladeStreak) 시작 박자가 0.02~0.06초 어긋날 수 있음** — 플레이로 보고 필요하면 각 적의 칼빛 시작을 조금 늦출 것.

### ✅ 이후 진행 (같은 날 밤 늦게) — Codex 풀링 끝 · 전부 컴파일 성공
- **Codex 풀링 완료** (16개 파일, 보고 = `Art/tools/specs/pooling.log` 끝): `Scripts/Game/Pool/`(GameObjectPool · PoolMember · IPooled · PoolStats), FxEffect · FxPart · FxBurst · GunkataFx(FxMesh · FxFlipbook) · Tracer · StylizedFx(FlashRibbon 포함) · EnemyProjectile · DamageNumber · Telegraph · RunManager(방 지을 때 프레임당 4개씩 예열) · FxPoolBench(D 항목).
  Codex가 짚은 충돌 1건 고침: `Ch2Bosses.cs` 연기 고리 `puffs`가 수명 끝나 풀로 돌아간 FX_SmokePuff를 계속 움직이고 Destroy하던 것 → `GameObjectPool.Version` 저장 + `IsCurrent`일 때만 움직이고 `Release`.
- **컴파일 에러 0** (경고 4는 예전부터 있던 안 쓰는 필드). 플레이 모드 측정 (이펙트 300개, `bench2.json` 결과):
  지금 방식 Instantiate 8.6ms · 2.3MB / 실제 풀 첫 판(새로 만듦) 13.1ms · 1.7MB → **두 번째 판(전부 재사용 300/300) 3.4ms · 35KB** / 오브젝트 없는 DashFx 0.09ms · 0. FxPart MPB 재사용으로 B 풀 할당 125KB → 24KB. 콘솔 에러 없음.
  **아직 안 본 것**: 실제 전투에서 풀링된 이펙트 · 탄 · 예고 · 피해 숫자의 겉모습이 예전과 같은지 (특히 장별 탄 꼬리, 예고 플래그 섞임, 씬 다시 불러오기).
- **휘두르기 손맛 클립 생성 완료**: `EnemyMotions.MakeAll()` 경고 없음. 휘두르기 이름 클립 13개 중 11개에 적용(치는 키 곧은 선 접선 확인). `Ch2_PorterSlam` · `Ch2_Porter_PlusSlam`은 다른 빌더(짐꾼 리그)가 만들어 안 들어감. **겉모습은 플레이로 아직 안 봄.**
- **ReaperFx 붙임**: `Ch5Bosses.Reap180`(대낫 베기) → `ReaperFx.BigSlash`, `GhostReap` 끝 → `ReaperFx.DashEnd` (예전 Void 충격 고리 · 흔들림 대체). **플레이로 아직 안 봄.**
- 다음: ① 4장 진자 집행관(걷기/점멸 간격/레이저 리본 칼끝 방향) ② 거울 고양이 사망 크기 ③ 휘두르는 적들 모션 ④ 최종 보스 대낫 연출 ⑤ 발자국 크기(HeavyStep 배율 수정 뒤) — 전부 WalkDemo 식으로 GIF 확인, ⑥ 전투 한 판으로 풀링 겉모습 확인.

### 최종 보스 대낫 포스 — `ReaperFx.cs` (→ 위 ✅에서 붙임)
- 사용자 참고: 거대한 소용돌이 베기 그림 + Steam 영상(낫 든 사신이 붉은 베기 줄기 · 검은 잉크 연기 · 검은 깃털 · 불티를 흩뿌리고 연기로 사라짐). "마지막 보스는 이 정도 포스와 압도적인 감각, 특히 …"(문장이 끊김 — 낫 베기로 해석, 사용자에게 확인 필요).
- `ReaperFx.BigSlash(at, dir, radius, cw)`: 흰 심지 호 → 섬광 결 호 → 자홍 호 → 가장 크고 오래가는 검은 잉크(Void) 호 + 호 접선으로 날리는 불티 26 + 깃털 · 보라 조각 14(돌며 떨어짐) + 바닥 충격 + 흔들림.
  `ReaperFx.DashEnd(at, dir)`: 유령 돌진 끝 잉크 폭발. 5장 색 규칙 때문에 주황 불 대신 흰빛 → 자홍 → 보라.
- **붙일 곳** (`Ch5Bosses.cs`, Codex 끝난 뒤): `Reap180()`의 `ScytheFlash(0.22f)` 옆에 `ReaperFx.BigSlash(transform.position, d, ReapRange, true)`, `GhostReap()` 끝 `StylizedFx.Shockwave(to, …)` 자리에 `ReaperFx.DashEnd(to, d)`, 잔상 `Echo` 베기 끝에 작게. 3페이즈 진입 연출에도 BigSlash 360(좌우 두 번).
- 한계: DashFx 색은 FxTint가 밝기를 0.85 이상으로 올려서 '검은' 깃털은 짙은 보라로 나옴 → 진짜 검정이 필요하면 DashFx에 '색 그대로' 플래그(FxTint 대신 _ColCore 등 직접) 추가.
- 확인: 보스방에서 `Reap180` · `GhostReap`을 리플렉션으로 바로 불러 GIF.

### 남은 것 (안 함)
1. ~~모든 장 휘두르기 모션에 상체 회전력~~ → 위 '휘두르기 손맛'으로 코드 씀 (클립 생성 · 확인만 남음). 원래 계획 메모 (`Editor/EnemyMotions.cs`): 휘두르는 클립(이름에 Swing · Slash · Whirl · Reap · Cleave · Cross · Slam 류, 장마다 Clip(...) 키 자세 문자열)마다
   예비 동작 키에 허리 · 가슴을 반대로 감고(`SP.TW` · `CH.TW` 있으면 같이, 골반 `HP`/ROOT 회전 키가 있으면 조금), 치는 키에서 반대쪽으로 크게 풀고(오버슈트 → 되돌림), 머리는 목표를 보게(`HD.TN` 반대로 조금) — 손만 휘두르지 말고 허리 → 어깨 → 팔 순서로 한 박자씩 늦게(키를 0.03~0.05초씩 어긋나게).
   먼저 `P()` 자세 문자열 해석기에서 쓸 수 있는 키(TW = 비틀기) 목록을 확인. 다 만든 뒤 메뉴 `CatTang/적 전용 모션 클립 만들기 (1 ~ 5장)` (= `EnemyMotions.MakeAll()`), 프리팹 무기 방향은 idle만 써서 다시 안 만들어도 됨.
2. **4장 몬스터 눈 레이저 궤적 확인**: 사용자 답 = 빠를 때만이 맞음. 빠른 이동 때 실제로 남는지만 GIF로 확인 (`WalkDemo.RunDash`에 4장 적 몇 개 넣기).
3. 위 변경 전부 플레이 확인 + GIF (거울 고양이 산산조각 크기 · 집행관 걷기/점멸/리본 · 돌진 조각).
4. MCP 팁: Unity 창이 뒤에 있으면 컴파일 후 리로드를 안 해 브리지가 안 붙음 → PowerShell `SetForegroundWindow(Unity 창)`로 깨움 (앞 절 참고). stdio(`UnityMCP`) 쪽이 붙음.

## 2026-11-20 (저녁) — 육중한 발걸음 흙먼지 · 돌진 꼬리 v2 · 발 바닥 맞추기 · 거울 고양이 (커밋 안 함)

- **발걸음 흙먼지** `HeavyStep.cs`: 시작할 때 스킨 메시를 구워 발 근처 정점 = 발 콜라이더 → 가장 낮은 점이 바닥에 닿는 프레임에 실제로 닿은 점들의 볼록 껍질(발자국 윤곽) 둘레에서 `FX_DustClump`(bk_dustclump)가 바깥으로 굴러 퍼지고 가운데 `FX_FootDust`(bk_dustpuff) 솟음. 붙은 곳: 1장 거인 · 2장 기관차 거인 · 4장 시침 · 5장 붕괴 거인 · 5장 거울 고양이 (`EnemyFactory`). 개발용 `HeavyStep.ShowFootprint` = 윤곽 초록 선.
- **발 바닥 맞추기** `GroundFeet.cs`: 휴머노이드는 발을 애니메이터 오브젝트 높이에 놓는데 프리팹 Mesh가 올라가 있어 떠 있었음(시침 2.6 · 분침 2.1 · 집행관 2.4 · 기관차 거인 2.8m, 일반 적 0.2~0.4m) → 꺼낼 때 한 번 재서 내림. 시간의 주인은 원래 떠 있어 뺌.
- **돌진 꼬리 v2** `DashTrail.cs` + `DashFx.cs`: 모든 적에 붙어 돌진을 스스로 알아챔(평소 이속 1.9배↑ · 바라보는 쪽 · 순간 이동 뺌). ① 경로를 따라 이어진 바닥 띠 ② 카메라 쪽으로 솟는 벽 띠 (둘 다 `CatTang/StylizedVfx`, 점마다 지난 시간을 U에 넣어 꼬리부터 흩어짐) ③ 3D 입자(블렌더 이펙트 메시 + ToonFire)를 **게임 오브젝트 없는 입자 풀**(DashFx, 600칸, Graphics.DrawMesh)로. 스타일 표 = sand · steam · water · laser · rift · mirror. 톱니 메시 `Resources/FX/Meshes/fx_gear.asset` 새로.
  - 이전 판(구운 그림 도장)의 `FX_Dash_*.prefab` · `FX/anim/bk_dash_*` 96장은 **안 씀 — 지워도 됨** (자동 모드가 휴지통 이동을 막아 남겨 둠). Codex 참고 그림은 `docs/images/fx_src/dash_*.png`.
- **거울 고양이**: 좌우 반전(스케일 −1) + KataPoser가 겹쳐 몸이 구겨지던 것 → 반전 뺌(빌더 · 프리팹). 사망 = `GlassShatter.cs`(그 자세로 굳어 금 번짐 → 유리 덩어리로 산산조각, 가짜도 작게).
- **붕괴 거인 걷기**: `EnemyMotions.HoldArm(…, swing)` — 망치 든 자세 위에 다리와 엇갈린 팔 흔들기. 클립 다시 만듦.
- **확인용** `Scripts/Dev/WalkDemo.cs`(Run 걷기 · RunDash 돌진 · RunBlink 4장 전광석화 · RunShatter 거울 고양이 사망), `FxPoolBench.cs`(이펙트 생성 방식 비교). GIF: `docs/images/{heavy_step_dust, dash_trails, mirrorcat_shatter, ch4_midboss_blink}.gif`.
- **MCP 메모**: Unity가 창이 뒤에 있으면 컴파일 뒤 스크립트 리로드를 안 함 → 브리지가 안 붙음. 창을 앞으로 가져오면(SetForegroundWindow) 리로드 · 브리지 시작됨.
- **못 한 것**: 실제 전투 루트로 돌진 꼬리 · 발 흙먼지 확인(테스트는 행동 끄고 걷게/돌진시킴), 오브젝트 풀링 적용(비교만 함 — 아래 결정 대기).

## 2026-11-20 (오후) — 남은 보스 전부 · 5장 천문대 소품 · 고래 효과음 (커밋 99cdd07 이후, **커밋 안 함**)

### 이번에 한 것
- **4장 최종 보스 시침 · 분침** `Scripts/Game/Ch4Twins.cs` (새 파일) + `EnemyFactory.Boss4`: 시침(5000 · 이속 2.4 · `Ch4HourBrain`) · 분침(4180 · 4.2 · `Ch4MinuteBrain`) 따로 체력, 분침은 `RunManager.AddEnemy`로 같이 넣음. `TwinDirector`(보스방에 하나)가 번갈아 · 겹침(한쪽 50% 이하 30%로 약한 패턴) · 상시 별빛 낙하(2초 / 2페이즈 1.5초) · 기믹 3종(20초 쿨: 순서대로 연속 명중 · 뒤섞인 문자판 · 멈춘 톱니) 담당. 패턴 = 기획서 그대로(내려찍기 · 대검 회전 · 시각 낙인 · 60초 찌르기 · 분침 돌진 · 바늘 부채 · 열두 시 협공), 2페이즈 물려받기. Awake에서 Setup이 바로 불려서 시침/분침을 자식 클래스로 나눔(필드 X).
  - 플레이로 본 것: 보스방 진입 → 둘 다 나옴 · 시침 체력바 · 멈춘 톱니 기믹 · 별빛 예고. **못 본 것**: 패턴 하나하나 · 협공 · 2페이즈 · 다른 기믹 2종.
  - 시침 대검 각도 고침: `WeaponGrip4["Ch4_HourHand/Greatsword"] = (311.6, 271.4, 165.9)` (칼끝 앞 아래). 프리팹에도 저장됨.
- **5장 중간 보스 거울 속의 고양이** `Scripts/Game/Ch5MidBoss.cs` (새 파일) + `EnemyFactory.MidBoss5`(10260 · 4.5) · `RunManager.HasMidBoss` 5장까지 · `MidBossName` · `RoomBuilder` 거울 대전(28×24, 벽 Facade_Mirror, 장애물 없음).
  - 3페이즈(66% 어긋남: 잔상 돌진 · 시간 역행 추가 / 33% 조각: 가짜 2(그림자 없음 · 한 발에 깨짐 · 깨지면 고양이 근처 22) · 20초마다 섞임), 패턴 5 + 2, 기믹 3종(깨진 거울 벽 = 거울 4장이 탄을 되쏨, 쏘면 깨지며 보스 25 / 그림자 찾기 = 가짜 5와 빙빙, 진짜 맞히면 3초 기절 / 거울 너머 저격 = 네 벽 거울을 오가며 저격, 그 거울 맞히면 2초 기절).
  - 플레이로 본 것: 체력바 · 패턴(건카타 궁) · 깨진 거울 벽 · 3페이즈 분열(가짜 2). **못 본 것**: 실제 거울 대전 방 안에서(테스트는 일반 방에서 소환 — 1페이즈 거울 이동이 방 가운데 기준이라 일반 방에선 엉뚱한 데로 감, 대전에선 정상일 것), 그림자 찾기 · 저격. **마지막 한 발 연출은 아직 없음.**
- **진자 집행관 고유 기믹 3종** (`Ch4Bosses.cs`): 진자 운동(12초 뒤 · 25초마다, 왼쪽 / 오른쪽 8번, 7번째 엇박, 방 절반 22) · 판결의 수(25초 뒤 · 25초마다, 진자와 8초 이상 떨어짐, 숫자 n · 5초 → `rev.next == n` 무죄 2초 기절 / 유죄 최대 체력 25%) · 끊어진 추(66 · 33% 확정, 추 3개 자기 시각 탄에만 멈춤, 반경 2 → 3초마다 +1m, 12초, 남은 추당 10%, 다 멈추면 3초 기절, 그동안 보스 무적). 기믹 중엔 다른 쿨 멈춤. **플레이로 아직 안 봄** (컴파일만).
- **5장 천문대 소품 12종** 블렌더 + Codex (`kit_ch5_obsprops_spec.md` → `blender_kit_ch5_obsprops.py`): 같은 이름 · 크기로 모양만 교체 (화약통 → 크로노미터 통 · 짐 → 천문 도구 상자 · 스프링 · 엄폐물 3 · 떠다니는 잔해 6 = 돔 껍데기 · 망원경 통 · 게이트 시계 조각 · 혼천의 고리 · 자오선 벽기둥 · 별자리판). 뒤틀림 다시 적용, 5장 키트 다시 빌드. 예전 원본은 `Art/models/ch5_unwarped_violet/`(25개). 플레이로 확인함.
  - `Ch5Kit.FloorBreakup`의 앞 장 바닥 조각(`nPatch`) 끔 (코드만 고침, 플레이 재확인 안 함).
- **거울 고래 효과음 5종** audio-studio (`e3b_song / slam / dive / beam / shards`) → `Resources/Audio/Sfx/`, `Ch3Bosses.cs` 패턴 10곳에 연결. 귀로 안 들어 봄.
- **5장 보스 시간의 주인 새 모델** `Ch5_LordOfTime` 프리팹(이전 턴): 크기(낫 포함 경계 약 10×9m)와 모션이 맞는지 **플레이로 아직 안 봄**. `EnemyFactory.Boss5`가 이걸 먼저 씀(없으면 예전 Ch5_Boss).

### ▶ 다음 작업 (남은 것)
1. 플레이 확인: 집행관 기믹 3종 · 쌍둥이 패턴 전부 · 협공 · 2페이즈 · 거울 고양이(실제 거울 대전에서 · 그림자 찾기 · 저격) · 시간의 주인 크기/모션 · 5장 방에서 앞 장 바닥 조각이 사라졌는지.
   - 테스트 요령: 플레이 → `RunManager`의 `chapter` 필드 + `Chapter` 속성 바꾸고 `BuildStage(index, RewardKind)` 리플렉션 호출. 보스방 = index 4. **주의**: 시작 방의 웨이브 예약(0.3초)이 남아 있으면 새로 지은 방에서 보스가 한 번 더 나옴(테스트에서만 생기는 일).
2. 거울 고양이 마지막 한 발 연출.
3. 4장 보스 경기장 바닥이 일반 4장 바닥(문자판 아님) — 기획은 은빛 거대 문자판. 뒤섞인 문자판 기믹은 숫자 라벨 + 칸 선으로 대신 보여 줌.
4. 새 효과음 · 4장 곡 귀로 확인, 음량 손질.
5. 메모리 규칙상 브랜치는 main 하나인데 지금 `feat/ch5`에서 작업 중 — 합칠지 사용자에게 물어볼 것.



## 2026-11-20 — 5장 보스 '시간의 주인' · 스타일 VFX · 바닥 가시성 (커밋 안 함)

### 사용자 결정
- 색 가시성: 보라 · 금 적이 보라 바닥에 묻힘 → **바닥은 채도 빼고 어두운 청회색 + 잔무늬 흐리게 · 명암 압축, 금 균열만 원래 색** (셰이더 `CatTang/RiftFloor` `_Desat · _Darken · _Tint · _Contrast · _Soft · _Mid`). **벽 · 소품 배색 `ModelPalette.Ch5` 돌 · 대리석을 차가운 슬레이트로**, 보라는 포인트만.
- 보스는 더 강력한 느낌 → 블렌더 부품 + 아우라. 베기 궤적은 **리본 형태 · 섬광에 가깝게** (단색 초승달은 "그림 같음"). 유령 돌진 베기는 **수평으로, 양손으로** 베어 넘김 + 모션 가감속.
- 블렌더 VFX 영상(인스타 goodgood3d "같은 노드 5개") 기법은 **모든 이펙트가 아니라 어울리는 큰 동작에만** 써서 화려하게.
- 두 번째 영상(블렌더 2.5D 붓 · 색연필 화풍)은 **적용하지 말고 셰이더로 가능한지만 검토** (아래 '검토').

### 스타일 VFX (영상 기법 → 유니티)
- 셰이더 `Art/Shaders/CatTangStylizedVfx.shader`: 절차 노이즈 / 보로노이(세포 경계선 = 금 간 그물) / 줄무늬, UV 오프셋 흐름(매핑 Location), 계단 3색 + 잉크 띠, 나이 문턱(구멍 뚫리며 사라짐), V · U 끝 깎기, 프레넬, **미리 곱한 알파 `_Opacity`(0 = 더해지는 빛)**, **`_Sweep` · `_TailFade`(머리부터 그어지고 꼬리부터 흩어짐)**.
- 메시(Codex + 블렌더 `Art/tools/blender_fx5_shapes.py`, 명세 `fx5_shapes_spec.md`): `Resources/FX/Meshes5/fx5_{dome, ring, ring_tall, swirl, slash_arc, spiral_ball, spike_burst}` (U = 흐름, V = 가로). 초승달은 가운데가 −X로 들어옴.
- `Scripts/Game/StylizedFx.cs`: `Spawn`(한 겹) + 프리셋 `Shockwave · Slash · Dome · Tornado · Burst`, 겉모습 `Magenta · Gold · Void(덮는 연기) · Glass · SlashStreak · SlashCore`. **`FlashRibbon`**: 무기 안쪽 · 끝 점을 매 프레임 기록한 띠 메시 섬광 (보스 낫 · 균열 자객 두 칼).
- 5장 적 적용 (큰 순간만): 균열 깨짐(유리 충격파 + 가시 폭발), 균열 장판 테두리, 거인 내려찍기 충격파, 별 박기 나선 구 · 별자리 폭발, 자객 튀어나오기 섬광 리본, 허상 분열(유리 나선 구), 허공 탄 착지, 사수 총구 가시. 작은 탄 · 불똥은 기존 플립북 그대로.

### 보스 `Scripts/Game/Ch5Bosses.cs` (md '보스 상세 — 시간의 주인')
- `Health.minHp`(새 필드) = 1 → 체력 0에서 안 죽고 3페이즈로. 남은 시간은 **보스 이름 줄에 표시**(`DisplayName`, GameUI 보스 바가 매 프레임 읽음).
- 1페이즈: 남은 시간 240초(매초 줄어듦, 0 = 즉사), 모래시계 픽업 20초마다 2개(+20초). 패턴 대낫 베기(180° 6m, 맞으면 −10초) · **유령 돌진 베기**(예고 1.1초 · 방향은 앞 75%만 따라감 → 11m 반투명 유령 돌진 smootherstep 가감속 · 잔상 · 낫 섬광 리본, 맞으면 −15초) · 시간 정지장(금빛 돔) · 종말의 탄 3 · 시계 바늘 창 · 모래폭풍(회오리). 기믹 25초: 죽음의 시각(얼굴 시계 시각 탄 → 4초 기절, 다른 탄 −3초) ↔ 숫자 인장 3개(시각 탄 쏜 뒤 1.5초 안 그 인장 위, 순서대로).
- 2페이즈(50%): 가장자리로, 감시 빔 · 시간 역류(지난 3초 경로). 환영 `BossGhost` 4종 차례로(녹은 시계 거인 웅덩이 비 · 기관차 거인 돌진 · 거울 고래 물결 · 쌍둥이 십자 바늘), 반투명 `CatTang/MirrorGhost`, 체력 1500, 20초.
- 3페이즈(체력 0): 무적, 60초 버티기(이름 줄 '마지막 n초'), 경계 원 15초마다 −2m(밖이면 피해 + 밀어 넣음), 모래시계 뒤집기(−5 / 놓치면 +5), 종소리 12번(울림 ±0.25초 저스트 회피 −1초), 최후의 째깍. 0초 → 처치 → 장 클리어.
- 강력한 느낌: 블렌더 부품(Codex `blender_kit_ch5_bossparts.py`, 명세 `kit_ch5_bossparts_spec.md`, 미리보기 `blender_kit_ch5_bossparts_preview.png`) `BP_Halo`(11:59 깨진 문자판 후광, 0.7배) · `BP_Crown` · `BP_Pauldron_L/R` · `BP_CapeShards`를 뼈에(`BossArmor5`), `BossAura` = 발밑 연기 · 금 고리 + 3초마다 회오리 + 공전 모래시계 3 · 톱니 2. 낫 0.72배, 낫 보석은 빛나는 단색 → 툰(자루를 가려 '잘려 보였음').
- 모션(`EnemyMotions.Ch5` 보스 부분): Idle(둥실) · Walk(미끄러짐) · ReapWind(오른쪽 뒤로 수평하게 감음, 왼손이 자루를 함께) · ReapDash(0.07초 수평 베기 → 오버슈트 → 감속) · Reap · Cast · Spear. 키 간격 + ClampedAuto 접선으로 가감속. `EnemyFactory.Boss5`(체력 15120, 이속 3.5, 자홍 눈빛 꼬리).
- 효과음 13종 `e5b_*` (gen_audio.py).
- **잔상 (맵 패턴, 2026-11-20 사용자: 발탄처럼 · 끊는 기믹 아님 · 한 마리씩 · 지속 등장 · 텔레그래프 없이 유령을 보고 피함)**: `Ch5Boss.Echo()` — 보스 패턴과 따로 `Always`에서 시작, 1페 6~7초 · 2페 9초 · 3페 4.5초마다 한 마리. 고양이 둘레 무작위 방향(보스 쪽 ±45° 제외) 6~9m 허공 균열에서 보라 유령(보스 모델 복제, 장식 부품 빼고 0.8배, `BossGhost.MakeGhostly`)이 나와 낫을 수평으로 감은 채 1.1초 몸을 돌림 → 마지막 0.25초 방향 고정 → 10m 유령 돌진 베기(가감속 · 잔상 · 섬광 리본), 맞으면 −8초. **바닥 예고 없음.**
- **색 규칙 (사용자: 금빛 고리가 뭘 뜻하는지 헷갈림)**: 금 = 고양이에게 이로움(모래시계 · 숫자 인장 · 보스 기절 · 죽음의 시각 과녁 · 승리), 자홍 · 보라 = 해로움. 시간 정지장 · 모래폭풍 · 종소리 · 시간 역류 · 아우라 고리 · 3페 진입 · 쌍둥이 환영을 자홍/보라로 바꿈. 에너지 계열 스타일 VFX(자홍 · 금 · 유리)는 기본 '더해지는 빛'(불투명도 0) + 흐름 방향 결(U 성기게 · V 촘촘히) — 덮는 색 덩어리는 "그림 같음"

### 맵 개편 — 초현실주의 (2026-11-20 사용자: 다른 맵에 비해 너무 밋밋함 · 바닥 이미지 · 재질 다시, 여러 패턴을 불규칙하게 섞어서 · 벽이랑 오브젝트는 마을 건물처럼 왜곡 · 뒤틀린 초현실주의 느낌)
- **바닥 v3**: Codex 그림 `Art/env_src/ch5/ground_ch5_surreal_codex.png`(프롬프트 `Art/tools/specs/ground_ch5_v3_prompt.txt`): 녹아내린 체커 · 휜 판자 · 역 타일 · 자갈 · 늘어진 시계판 · 모래 물결 · 별 허공이 금 이음매로 뒤섞인 패치워크(이음매 없음). `ground_texture_ch5.py` 기본 원본을 이것으로. 셰이더 `CatTang/RiftFloor`에 **패치워크 층** 추가: 4장 대리석 · 1장 술집 판자 · 2장 역 타일 · 3장 바닥을 도메인 왜곡 노이즈 경계(녹아내린 모양)로 군데군데 섞고 경계는 금 이음매 (`_Tex2~5 · _PatchAmount 0.35 · _PatchScale · _PatchWarp · _SeamWidth`), 가시성 값은 덜 누름(`_Desat 0.5 · _Darken 0.78 · _Contrast 0.6`). 빌더가 재질에 텍스처 · 값을 넣음.
- **벽 · 오브젝트 왜곡**: Codex `Art/tools/blender_ch5_surreal_warp.py`(명세 `ch5_surreal_warp_spec.md`) — 마을 건물 `dream_volume.warp`처럼 휘기 · 비틀기 · 배불림 · 윗면이 녹아 처짐(달리) · 흔들림을 버텍스에 구움. 원본은 `Art/models/ch5_unwarped/`(무시 폴더)에 백업하고 늘 백업에서 읽어 다시 돌려도 두 번 안 휨. 벽 · 문은 바닥 발자국 · 통로 유지.
- **방 꾸미기 `Ch5Kit.Dress`**: 떠다니는 기억 잔해(Codex `blender_kit_ch5_debris.py`: FD_Saloon · Rail · StationClock · Gears · MirrorFrame · DesertPillar · Bell · Island, `FloatingDebris` 둥실 · 회전) 방마다 7~10개 2.8~5.5m, 스테이지별 바닥 구조물(0 거울 비석 줄 · 1 술집 · 기차 조각 섬 · 2 큰 허공 구멍 · 3 시계 심장 · 모래시계 · 바늘 첨탑), 금 · 보라 점광원 4.
- **보스 방 `Ch5Kit.BossArena`**: 사각 바닥 숨김 → 깨진 거대 문자판 `BA_DialRing`(반지름 21m) + 아래 허공 판 · 별가루 + 바늘 첨탑 3 + 떠 있는 숫자 조각 · 잔해.
- **물결 고리 → `VoidRipple`**(5장 전용, 블렌더 벽 고리 메시가 퍼지는 자홍 빛 벽 + 보라 연기 띠). **시계 숫자 탄** → Codex `docs/images/fx_src/c5_numeral_bullet.png`(깨진 시계판 수정 + 유리 꼬리) → `Resources/FX/bullets/dial5`, 숫자는 흰색 (5장만).

### 검토만 (적용 안 함) — 블렌더 2.5D 붓 · 색연필 화풍 영상
- 셰이더로 대부분 가능: ① 명암을 붓질 / 해칭 텍스처로 계단화(NdotL 문턱 × 붓 마스크, 화면 · 삼면 투영) ② 노이즈로 외곽선 굵기 흔들기 + 실루엣 가장자리 알파 깎기(손그림 테두리) ③ 저주파 노이즈 색 얼룩(수채 안료) + 프레넬 가장자리 진하게(번진 테두리) ④ 종이 결은 URP 렌더러 기능 후처리. 불꽃은 1번 영상 방식(노이즈 띠 + 계단 램프)과 같음.
- 한계 · 위험: 지금의 브롤풍 단색 툰과 톤이 다름(섞으면 이질감), 블렌더 합성(compositor) 효과는 후처리로 따로 만들어야 함, 움직이는 화면에서 화면 고정 붓 텍스처가 미끄러져 보임(물체 공간 투영 필요). 쓴다면 일부(추억 · 엔딩 연출, 보스 아우라)에 한정하는 게 맞음.

### 확인 · 안 한 것
- 컴파일 에러 없음. 플레이 모드에서 보스 방 생성 · 1→2→3페이즈 전환(환영 소환 · 경계 원 · 카운트다운) · 유령 돌진 베기(느린 시간, 예고 → 유령 돌진 → 수평 베어 넘김) 화면 확인, 콘솔 에러 없음. 새 바닥 · 벽 색 화면 확인.
- **안 함**: 보스 전 패턴 손으로 한 판(난이도 · 수치), 숫자 인장 · 죽음의 시각 실제 성공/실패 플레이, 환영 4종 모두 보기, 효과음 귀로, 5장 중간 보스(거울 고양이) 행동, 테스트 때 1장 웨이브가 남아 보스가 둘 생긴 건 디버그 진입 탓(정상 진행에선 아님 — 그래도 한 번 정상 루트로 확인 필요).

## 2026-11-19 (밤 끝) — 5장 '무너지는 시간의 끝' 구현 (커밋 안 함)

### 사용자 결정 (이번 세션)
- **앞 장 이펙트 · 모션 그냥 재활용 금지 — 다른 느낌으로 재구성** (1장 녹은 웅덩이 · 3장 분신술사 빛가루/미끄러지는 자리 섞기 · 4장 칼빛 · 고양이 idle/run 빌려 쓰기 모두 뺌). **예외: 중간 보스 거울 고양이는 거울에 비친 분신이라 고양이 기본 모션을 따라 해도 됨.**
- 이펙트 = **Codex 그림 시트로 기본 기틀 + 블렌더 3D 굽기를 더함** (둘을 한 자리에 겹침: `Enemy.Fx5`).
- **바닥: 4장 체커 재활용 말고 훨씬 깨지고 일그러지게 + 중간중간 이전 장 바닥이 일그러진 균열로 섞이게. 깨진 부분 이음매는 사각형 · 원 같은 균일한 도형 금지, 불규칙하고 일그러진 모양.**
- 중간 보스 = 주인공 고양이 재활용 + 셰이더 씌우기, 눈빛은 4장 적처럼 (자홍).
- 리깅 파츠 손 위치 (보스 · 마녀 지적) → 5장 무기 방향 다시 잡음 (아래).

### 모델 · 프리팹
- Tripo 6종 다 들어옴(사용자가 직접 받음): 사수 · 마녀 · 거인(골렘) · 자객(전사) · 균열술사(마법사) · 보스(후드 시계). `tripo_import.py` → `Art/Characters/Ch5/Ch5_*.fbx`, 강화형 = `ch5_textures.py plusfbx <이름>`(기본 FBX 복사 + 보라 · 금 균열 텍스처). 보스는 zip 2개 중 나중 것(21:20) 사용.
- **강화형 무기 · 파츠 FBX 12개가 0바이트로 깨져 있었음** (지난 세션 저장 끊김) → `blender_kit_ch5_plus.py` 다시 돌려 복구.
- **5장 무기 방향 `EnemyModelBuilderCh5.Aim5Dirs`**: 4장 손잡이 각도를 빌려 보스 낫이 가로로 몸 관통 · 마녀 지팡이가 거꾸로 매달렸던 것 → 프리팹 idle 자세(0.3초)를 놓고 손바닥에서 몸 기준 방향으로 붙임(지팡이 · 낫 세움, 소총 앞, 향로 아래, 망치 앞아래로 끔, 칼 앞). `AttachWeapons`가 5장이면 `SampleIdle5` → 붙인 뒤 뼈 원래대로. `WeaponGrip4`의 5장 줄은 지움.
- 메뉴: `CatTang/5장 키트 만들기 (블렌더)` → `CatTang/적 전용 모션 다시 만들기 (클립 + 컨트롤러)` → `CatTang/5장 적 프리팹 만들기 (Tripo + 블렌더)` 순서 (프리팹이 새 idle로 무기 방향을 잼).

### 적 행동 `Scripts/Game/EnemyCh5.cs` (md '수치 기준 (챕터 5)' 그대로)
- 파편 사수(`ShardGunner`): 유리탄, 벽 · 거울에 닿으면 3갈래(25°) · 강화 0.45초 뒤 공중 분열 / 별자리 마녀(`StarWitch`): 고양이 둘레 별 3개 → 별 사이 빛 선 `StarLink`(8m/초, 닿으면 19) · 강화 삼각형 가운데 폭발 22 / 붕괴 거인(`RuinGiant`): 내려찍기 + 균열 장판 `RiftScarZone` · 4~8m 돌진 · 강화 균열 연쇄(4m 안 균열로 이어 터짐) / 균열 자객(`RiftAssassin`): `RiftSpots`(방 균열 · 흉터 · 거인 균열 · 허공 구멍)에 잠복 → 가장 가까운 균열에서 튀어나와 벰 · 강화 이중 매복 / 균열술사(`RiftCaster`): 도망 + 5초마다 균열 열기 + 허공 탄 곡사 `VoidLob` · 죽을 때 균열 3(강화 즉발) / 거울 허상(`MirrorPhantom`, 5장 전용 로직): 분신이 **고양이를 축으로 비친 자리**에 생기고, 자리 바꾸기는 셋이 유리처럼 깨졌다 바뀐 자리에서 다시 맞춰짐 · 분신은 그림자 없음 · 뼈 없는 몸은 `PhantomFloat`로 둥실 + 글리치.
- `EnemyFactory.RandomCh5`: 방마다 3~4종, 균열술사 최대 1 · 거울 허상 최대 2 (안 막으면 허상 10 + 분신 20이 나왔음). 체력: 사수 · 마녀 · 술사 · 허상 204 · 거인 544 · 자객 272. 모든 5장 적에 자홍 눈빛 꼬리(`EyeTrail.Attach(e, 자홍)` + 블렌더 `bk_c5strip`).
- `Telegraph.Danger` = 5장이면 자홍. `EnemyProjectile` 5장: 구슬 → `bk_c5orb`, 그 밖 → `bk_c5shard`, 맞음 → `Fx5("c5spark")`.

### 전용 모션 (`Editor/EnemyMotions.cs` `Ch5()`, `Art/Characters/Ch5/Anim/Ch5M_*`)
- '붕괴' 느낌: 동작 사이 한 프레임씩 어긋나 튀는 **글리치 키(0.02~0.04초)** + 둥실 · 기우뚱. idle · 걸음도 전용(`Ch2Motion.Walk` 생성기 + 무기 든 팔). 상태: 사수 Shot / 마녀 Cast · Link / 거인 Slam · Dash / 자객 Dive · Emerge · Slash · Breath / 술사 Open · Lob. 보스는 아직 행동이 없어 거인 클립을 임시로 붙임.
- 아직 리그에서 IK로 손 자리를 잰 값이 아님 (4장처럼) — 모션마다 무기가 어색하면 4장 방식으로 손바닥 목표를 풀어 넣을 것.

### 맵 · 방 기믹 (`Scripts/Game/Ch5Kit.cs`)
- 벽면 스테이지별(거울 문턱 → 기억 회랑 → 잔해 섬 가장자리 → 시계 심장), 지형지물 = 앞 장 기억 조각, 보스 방 원형 경계 21m.
- **허공의 균열 `Rift` + `RiftField`**: 전투 중 8초마다(첫 4초) 고양이 근처 1.5~3.5m → 1.5초 커지며 떨림 → 깨짐 반경 2.5(적 30 · 고양이 15) → 흉터(CrackGlow) 6초. 오브젝트: 일그러진 화약통 · 멈춘 짐 · 태엽 스프링 1~2, 거울 조각 2(`MirrorShard.SpawnSet(…, count)`), 엄폐물 내구 60.
- **바닥 v2**: Codex 그림 `Art/env_src/ch5/ground_ch5_broken_codex.png`(프롬프트 `Art/tools/specs/ground_ch5_v2_prompt.txt`, 상하좌우 이어짐 확인) → `ground_texture_ch5.py` → 셰이더 **`CatTang/RiftFloor`**(UV 두 겹 물결 + 월드 노이즈 도메인 왜곡으로 일렁임, 금 균열 맥동, 허공 틈 별 반짝임). 예전 체커 원본은 `ground_ch5_gpt.png`에 남김.
- **깨진 바닥 조각** (Codex + 블렌더 `blender_kit_ch5_floor.py`, 명세 `kit_ch5_floor_spec.md`, 미리보기 `blender_kit_ch5_floor_preview.png`): `FloorPatch_A/B/C` 윗면 재질 `Patch_Top5` → 게임이 **1~4장 Ground 재질로 바꿔 끼움**(`Ch5Kit.FloorBreakup`, 방마다 3~6, 크기 · 방향 · 뒤집기 · 늘임 제각각) / `FloorVoidPit`(자객 자리) / `FloorWarpRidge` / `FloorSlabTilted` · `FloorShardSpike`(막힘).

### 이펙트
- Codex 시트(초록 #00FF00 바탕 — 자홍 이펙트라 마젠타 키 불가 → `fx_sheet_cut.py --green` 추가): `docs/images/fx_src/c5_{riftshatter,shardhit,starburst,voidburst,voidslash}.png` → `cx_c5*` (프롬프트 `prompts/c5_fx_sheets.txt`).
- 블렌더 굽기(Codex가 `blender_bake_fx.py`에 추가, 명세 `Art/tools/specs/fx_ch5_bake_spec.md`, 미리보기 `fx_ch5_bake_preview.png`): `bk_c5orb · c5shard · c5spark · c5rift · c5strip`.
- `Enemy.Fx5(set)`: Codex 시트(바탕) + 블렌더(덧그림 0.8배)를 겹침. 대응표 `Cx5`.
- 중간 보스 셰이더 **`CatTang/MirrorCat`**(툰 + 크롬 반사 + 은빛 테두리 + 흐르는 반짝임 + `_Crack` 금빛 균열) + `MirrorCatSkin`(체력이 줄수록 균열 번짐, 가짜 조각 `SetFake`). `Ch5_MidBoss` 프리팹에 붙음.

### 소리
- 효과음 16종 `e5_*`(audio-studio, `gen_audio.py` SFX 표) → `Resources/Audio/Sfx/`.
- **배경음악 Gemini 3곡** `bgm_stage_5`(2:38) · `bgm_midboss_5`(2:43) · `bgm_boss_5`(2:42): 원본 `Art/audio_src/gemini/*.mp3` → **`python Art/tools/gemini_bgm.py <이름>`**(끝 무음 · 페이드 · −16 LUFS · 48kHz OGG). 받는 법: 내장 브라우저 Gemini '음악 만들기' → 트랙 다운로드 → 오디오만 → `~/Downloads/<guid>.tmp`를 복사. 중간 보스 곡은 처음에 Gemini 오류 → '다시 시도'로 됨. **귀로는 안 들어 봄.**
- 표 `Chapter.csv` 5장 = 무너지는 시간의 끝 · 바닥 2C2248 / 벽 1B1530 · 보스 체력 15120. `RoomBuilder.StageRooms` 5장 40×33 → 46×39.

### 확인한 것 / 안 한 것
- 컴파일 에러 없음. 플레이 모드에서 5장 스테이지 1 방 짓기 · 적 스폰(종류 섞임, 허상 상한) · 균열 · 흉터 · 새 바닥 · 지형지물 화면 확인, 콘솔 에러 없음.
- **안 됨**: 5장 중간 보스(거울 고양이) · 보스(시간의 주인) 행동(`HasMidBoss`는 아직 3장까지, 4장 보스도 아직), 5장 상인(앞 장 상인 합체), 전용 모션 무기 IK 정밀 조정, 적 스킬 손맛 직접 플레이, 음악 귀로 확인.

## 2026-11-19 (저녁) — 4장 기획서 PPT · 4장 키트 · 느와르 색 · 레이저 이펙트

### 사용자 결정
- **4장 색 = 느와르**: 강한 흑백 대비(새까만 그림자 · 흰 달빛 · 은빛 톱니, 중간 회색 적게), 색은 붉은 포인트만. **적 공격 궤적(탄 · 예고 범위 · 휘두름)은 빨간 계열**. 레벨 디자인 md 챕터 4 '색' 줄에 반영.
- **적 공격 = 레이저 느낌**: 흰 심지 + 빨간 띠 + 잉크선. 빠른 이동(점멸 · 돌진) 때 **눈빛이 빨간 레이저 꼬리**를 남김.
- **칼끝 · 눈 이동 궤적의 끝 = 리본 꼬리(제비꼬리)**, 꼬리가 머리 쪽으로 따라 들어가며 사라짐(자연스러운 종결).
- **그림체는 다른 장과 같은 방식**: 모델 = 플랫 고유색(회색 + 역할 재질 → 배색표) + `CatTang/Toon` + 브롤풍, 이펙트 = 블렌더 툰 띠 + 잉크선 플립북. 이질적이지 않게.
- 이미지는 크롬 ChatGPT, 복잡한 모델링은 크롬 Tripo, 간단한 오브젝트 · 맵은 Codex + 블렌더.

### 만든 것
- **4장 기획서** `docs/1159_4장_기획서_20261119_정승운_Ver01.pptx` (21장, 생성기 `docs/tools/build_ch4_ppt.js`, 실행 `NODE_PATH=<pptxgenjs@3.12 · image-size@1> node build_ch4_ppt.js`). 컨셉 시안 5장 `docs/images/ch4/` (크롬 ChatGPT, 원본 `src/`): 배경 · 전투방 · 일반 적 6종 · 진자 집행관 · 쌍둥이. 같은 채팅에서 이어 요청하면 느와르 화풍이 유지됨.
- **크롬 ChatGPT 그림 받기**: 페이지에서 `fetch(img.src)` → blob → `<a download>` 클릭 → `~/Downloads`에 바로 저장됨(이번엔 막히지 않음).
- **4장 키트 (Codex + 블렌더)**: 명세 `Art/tools/specs/_ch4_common.md` · `kit_ch4_{props,walls,gates}_spec.md` → `Art/tools/blender_kit_ch4_*.py` → `Game/Assets/Art/Characters/Ch4/Kit_*.fbx` 31개 → 메뉴 **`CatTang/4장 키트 만들기 (블렌더)`**(문 · 복도도 같이) → `Resources/Kit/Ch4/` (벽 7 · 소품 11 · 문/복도 13). 배색 `ModelPalette.Ch4`(느와르).
- **`Ch4Kit.cs`**: 스테이지별 벽면(창고 → 기관실 → 종루 → 문자판 뒤), 바닥 조각, **돌아가는 톱니 바닥 `GearFloor`**(맞물려 반대 방향 · 실어 나름 · 맞물림 피해 10 + 밀침 · 축 쏘면 반대로 · 이웃도 같이), **태엽 스프링 `WindSpring`**(0.4초 뒤 3m 폭발 60/20), **굴러가는 톱니바퀴 `GearWheel`**(맞은 방향 14m/초, 한 번 튕김), **낡은 종 `OldBell`**(5m 링 · 적 30 · 적 탄 지움 · 쿨 8), 엄폐물 3종 내구 50, 보스 방 원형 경계 20m. `Kit.cs` · `RoomBuilder.cs` · `RunManager.cs`에 연결. `Chapter.csv` 4장 = 시계 장치 속 · 바닥 34343A / 벽 18181C · 보스 체력 9180.
- **레이저 이펙트 `LaserFx.cs`** (4장에서만):
  - 탄: `EnemyProjectile` 4장이면 구슬 → `bk_c4orb`(레이저 구슬), 그 밖 → `bk_c4needle`(레이저 볼트 + 속도선), 맞음 · 튕김 → `bk_c4spark`(레이저 가시 섬광), 빛 번짐 빨강.
  - `Telegraph.Danger` = 4장이면 새빨강(속성으로 바뀜). 빨간 예고가 끝나는 순간 부채꼴 → `LaserFx.Slash`(블렌더 `bk_c4slash` 칼끝 리본 + GPT 시트 `cx_c4slash`), 직선 → `LaserFx.Streak`(돌진 자국, 꼬리가 도착점으로 빨려 들어감).
  - **눈빛 꼬리 `EyeTrail`**: 4장 적(지금은 3장 적을 빌림)에 붙음. `emitting`을 끄지 않고(끄면 Unity가 폭을 뚝 0으로 만듦) `time`을 줄여 꼬리가 눈으로 들어가며 끝남, 끝날 때 꼬리 끝 작은 불똥, 점멸이면 옛 자리 → 새 자리 직선. 텍스처 `bk_c4strip`(리본 꼬리, 길이 방향 머리 흰빛 → 꼬리 옅은 빨강).
  - 굽기: `Art/tools/blender_bake_fx.py -- c4orb c4needle c4spark c4slash c4strip`. GPT 시트 원본 `docs/images/fx_src/c4_laserslash.png` → `python docs/tools/fx_sheet_cut.py ... cx_c4slash 8 --strict`.
- 확인: 플레이 모드에서 4장 스테이지 1~4 방 짓기(벽 · 톱니 바닥 3개 · 소품) 화면, 레이저 탄 · 섬광 · 베기 · 돌진 자국 · 눈빛 꼬리(점멸 → 멈춤 → 꼬리 들어감) 느린 시간 화면 확인, 콘솔 에러 없음.

### 4장 적 모델 (같은 날 이어서)
- **원칙 (사용자)**: 무기는 몸과 따로 모델링해 손 뼈에 붙임 (같이 모델링하면 리깅 · 모션이 부자연스러움). 원화는 빈손 주먹 · 정면 A자 · 두 다리 따로(무희도 발끝 서기 X).
- 원화: 크롬 ChatGPT 한 채팅에서 3장 유리 기사 원화(화풍) + 4장 적 라인업(디자인) 첨부 → 한 마리씩 → `Art/tripo_input/ch4/<이름>.png` → `keep_main.py` · `denoise.py` → `_main_dn.png`. (`soldier_withgun.png` · `dancer_pointe.png` = 버린 첫 시안)
- Tripo 웹(크롬): H3.1 생성 55 → 리토포 삼각형 1만 35 → Auto Rig Humanoid · Mixamo 20 → Export FBX · Mixamo · 2K · 뼈대 포함 → `~/Downloads/tripo_convert_*.zip` → `python Art/tools/tripo_import.py <zip> Ch4_<이름> Game/Assets/Art/Characters/Ch4`. 크레딧 1375 → 약 695.
  - 작업 ID (studio.tripo3d.ai/workspace/rigging/<id>): 병정 db62245b · 종지기 860f1d6f · 무희 eecfb08b · 자객 b155c04f · 기사 7c0500d9 · 수리공 5860d84c.
- 무기 8종 (블렌더 + Codex, 명세 `Art/tools/specs/kit_ch4_weapons_spec.md`, 원점 = 손잡이 · 긴 축 +Z): 소총 · 종 · 망치 · 바늘 단검 · 진자 낫 · 렌치 + 보스용 대검(시침) · 세검(분침) → `Kit_W_*.fbx` → `Resources/Kit/Ch4/W_*`.
- 메뉴 **`CatTang/4장 적 프리팹 만들기 (Tripo)`**(`BuildCh4`, 무기도 같이): Humanoid · 툰(회색 그림자, 적 배색 치환 없이 빨간 테두리만) → `Resources/Enemies/Ch4_*`, `AttachWeapons`가 손 뼈(손바닥 = 손 → 가운뎃손가락 뿌리 60%)에 무기를 위로 세워 앞으로 25° 기울여 붙임. 병정 소총 · 종지기 망치(오른손) + 종(왼손) · 자객 단검 · 기사 낫 · 수리공 렌치, 무희 맨손. 크기 = 키 / 2.2.
- 게임: 4장 전용 행동이 아직 없어 `EnemyFactory.Make3`가 4장이면 3장 행동에 4장 모델을 끼움(사수 → 병정 · 법사 → 종지기/무희 · 검객 → 자객 · 기사 → 기사 · 분신술사 → 수리공, 체력 ×1.44) + 눈빛 꼬리.
- 공격 클립은 1장 · 고양이 것 임시(병정 revolver · 무희 shakes · 나머지 swings). 무기 각도는 쉬는 자세 기준 — 공격 모션에서 어색하면 `AttachWeapons` 각도 손질.

### 2026-11-19 (밤 늦게) — 4장 적 행동 · 모든 적 스킬별 전용 모션 · 앞뒤 근육 방향 바로잡기 · 적 스킬 효과음
- **사용자**: "모션 재사용 말고 스킬마다 다르게, 액션감 있게" → "모든 적 모션 재활용한 거 검토하고 따로 만들어야 할 것은 다시, 단조롭지 않게" → "총 쏘는 자세 이상함, 제대로 파악해서" → "새 모션에 어울리는 효과음 오디오 스튜디오로, 노래는 이전처럼 제미나이로".
- **4장 적 행동 `Scripts/Game/EnemyCh4.cs`** (md '수치 기준 (챕터 4)' 그대로): 태엽 병정(단발 · 2마리 이상이면 8초마다 북 → 전원 3연발 일제 사격, 마지막 0.3초 조준 고정 · 강화 착검 돌격) / 종지기(음파 링 `RippleRing` 빨강, 대시로 넘음 · 강화 2겹) / 오르골 무희(0.4초 발끝 → 3초 피루엣 두 갈래 나선 · 강화 12방향) / 초침 자객(모든 자객이 같은 정수 초 박자로 2.5m 점멸, 찌르기 → 1초 숨 고르기 · 강화 고양이 뒤로 점멸해 등 돌린 채 웅크렸다 반 바퀴 돌며 역수 찌르기) / 진자 기사(1.2초마다 좌우 번갈아 120° 낫, 휘두르며 전진 · 강화 회전 베기) / 시계 수리공(도망 · 태엽 감기 = 연결 선 `WindLink` + 3초 1.3배 + 15% 회복 · 렌치 곡사 `ThrownWrench` · 강화 2마리 · 죽을 때 주변 전부). `EnemyFactory.RandomCh4`(방마다 3~4종, 수리공 최대 1, 체력 · 피해 md 값), 3장 행동 빌려 쓰던 임시 분기(`Ch4Model` · 체력 ×1.44) 지움. 강화형 색 = 녹슨 붉은 기.
- **전 적 모션 검토 결과 → 전용 모션**: Tripo 일반 클립(revolver · swings · shakes · shotgun)을 돌려 쓰던 3장 5종 · 4장 6종 · 1장 깡패 내리찍기/회전 · 사냥꾼 회전 난사/태클/호출, 패턴 여럿이 클립 하나를 나눠 쓰던 1장 거인 · 2장 역장 · 2장 거인을 모두 스킬별 클립으로. 그대로 둔 것: 1장 폭탄병 · 2장 일반 적 · 짐꾼(이미 전용), 3장 보스(리깅 없음, 코드 움직임).
  - 생성기 `Editor/EnemyMotions.cs`(자세 = "약어=값" 문자열 시간표, 고양이 idle + 덧셈, 메뉴 `CatTang/적 전용 모션 클립 만들기`) → `Art/Characters/ChN/Anim/ChNM_*.anim`.
  - 컨트롤러: `EnemyModelBuilder.Moves`(스킬별 상태, 트리거 없이) · `Idles` · `Walks`(전용 idle · 걸음). 코드는 `Enemy.Anim("상태")` — 트리거가 있으면 트리거, 없으면 그 상태를 CrossFade. 보스는 `BossBrain.Add(..., anim: "상태")`. 무희 · 회전 베기 · 역수 찌르기 몸 회전은 `Enemy.SpinBody` / `HoldYaw`.
  - 메뉴 **`CatTang/적 전용 모션 다시 만들기 (클립 + 컨트롤러)`**: 2장 · 1~4장 클립 + 1장 점프/발 구르기/던지기 → 컨트롤러 25개를 같은 자산에 다시 채움(프리팹 참조 그대로). 짐꾼은 `2장 짐꾼만 다시 만들기`.
- **앞뒤 근육 방향 바로잡기 (중요)**: 리그 12종 + 고양이에서 재 보니 유니티 근육 이름은 '음수-양수' 순서 → **"Front-Back" + = 뒤** (Arm +0.5 → 손 0.3m 뒤, Chest/Spine + → 머리가 엉덩이 뒤, Upper Leg + → 발 뒤), "Foot Up-Down" + = 발끝 내림. Head Nod · Twist(+ = 오른쪽) · Down-Up · Stretch는 이름 그대로. 그동안 모든 코드 클립(2장 · 짐꾼 · 1장 던지기 등)이 + = 앞으로 적혀 앞뒤가 뒤집혀 있었음(짐꾼 내리찍기는 맞는 순간 뒤로 젖혀 있었음). `Ch2Motion.MirrorFrontBack`이 다 만든 클립의 Front-Back 커브를 기준값(고양이 idle, 짐꾼 팔은 트렁크 든 자세) 둘레로 뒤집음 → 표는 계속 '+ = 앞'으로 적으면 됨. 확인: 짐꾼 내리찍기 예고 머리 −0.07m(뒤) → 맞을 때 +0.06m(앞).
- **무기 든 자세 = 리그에서 풂 (총 자세 지적 후)**: 병정은 2등신(팔 0.41m · 어깨 너비 0.47m, 북이 배 앞 0.6~1.05m)이라 1.53m 소총을 견착 · 두 손 받치기 불가 → **오른 겨드랑이에 끼고 한 손 수평 조준, 왼손은 북 테두리**. 방법: 두 뼈 IK로 손바닥을 어깨 기준 목표에 → 손목(아래팔 비틀기 · 손목 위아래 · 옆)으로 무기 방향 → `GetHumanPose`로 근육 읽어 `EnemyMotions`에 붙임. 무기 손잡이 각도 `EnemyModelBuilder.WeaponGrip4`(그 적 공격 자세 전체의 평균 방향). 들고 다니는 자세(*_REST)로 전용 idle · 걸음(`HoldArm` = 고양이 idle/run + 무기 든 팔 고정)과 동작 시작/끝을 만들어 무기가 바닥을 찌르지 않음. 확인 수치: 조준 yaw −1° pitch 0° · 반동 +12° · 들고 다님 +40° · 착검 −4°/−3°, 망치 젖힘 27° · 내려침 −51°, 자객 찌르기 0°/−3°, 수리공 던지기 53°. 역수 찌르기 · 당김은 정수/역수 손잡이가 달라 23~31° 남음.
  - 주의: `SetHumanPose`는 몸 높이를 0.5m 바꿔 놓으므로 재는 기준은 `clip.SampleAnimation`(게임과 같은 자세). 근육값이 ±1 밖(고양이 idle 어깨 −1.96)이라 ±1로 자르면 안 됨.
- **적 스킬 효과음 22종 (audio-studio, `Art/tools/gen_audio.py` SFX 표 e4_* · e_*)**: 병정 사격 · 북 · 착검, 종, 무희 회전 · 마무리, 째깍 점멸 · 찌르기, 낫 · 회전 베기, 태엽 감기 · 렌치 던지기 · 쨍, 1~3장 부채질 3연발 · 내리찍기(깡패 · 녹은 웅덩이) · 회전 · 호루라기(사냥꾼 · 역장 호출 · 호루라기 열차) · 거울 깨짐 · 거울 다이브 · 빛 모으기 · 마술사 인사 · 증기 분출. 공격이 나가는 박자에 `Sfx.Play`. 첫 실행은 페이징 파일 부족(os error 1455)으로 죽어서 다시 돌림(핸드오프 메모대로).
- 확인: 컴파일 에러 없음 · 클립 근육 이름 경고 없음, 플레이 모드에서 4장으로 바꿔 6종(일반 · 강화) 스폰 → 웨이브도 4장 섞임으로 나옴, 상태(Shot · Blink · SwingL · Run 등) · 예고 · 레이저 탄 · 음파 링 확인, 콘솔 에러 없음. **소리는 플레이에서 귀로는 안 들어 봄**.

- **칼빛 줄기 `Scripts/Game/BladeStreak.cs` + 셰이더 `Shaders/CatTangBladeTrail.shader`** (사용자: 궤적이 칼날에 붙게 · 4장은 섬광처럼 · 랫키호테 평타 이펙트 참고 · 3D니까 사실적인 빨간 궤적): 랫키호테 `RatQuixoteUnity/Assets/Scripts/Game/FoeRigLive.cs` LabSlash/DrawFoeStreaks · `BossLive/LiveMirror.cs` BladeFx 방식 그대로 = 휘두르는 동안 매 프레임 실제 무기의 칼끝 · 밑동을 기록(크게 돌면 밑동 둘레 각도로 채움). 겉모습은 3D: 칼이 쓸고 간 면(밑동 15% ~ 칼끝) 띠 메시, 셰이더 = 미리 곱한 알파(붉은 몸이 밝은 바닥도 덮음) + HDR 흰 심지(블룸), 칼날 끝선 · 머리가 가장 밝고 꼬리 · 안쪽으로 붉게 옅어짐, 시작 0.06초 섬광. 4장 적: 기사 낫(좌우 · 회전 베기) · 자객 단검 · 종지기 망치 · 착검에 붙음(`EnemyCh4.Streak`, 칼끝 좌표 `BladePts`). 이 공격들은 몸 앞에 따로 뜨던 레이저 호/줄을 끔(`Telegraph.NoLaser()`). 확인: 4장 방(어두운 바닥)에서 낫 궤적이 머리 경로를 따라 붙어 사라짐 (프레임 캡처).
- **타격 범위 = 예고 (사용자: 실제 타격 범위랑 텔레그래프 동일하게)**: `Telegraph.InLane(점, 시작, 방향, 길이, 폭, lo, hi)` = 그린 직선과 똑같은 판정, 돌진은 지금 몸이 덮은 구간(나아간 거리 ± 몸 반지름)만. 적용: 1장 깡패 돌진 · 사냥꾼 태클, 2장 짐꾼 돌진 · 트렁크 · 역장 개찰 펀치 · 거인 돌진(Charge), 3장 유리 기사 밀치기 · 고래 잠수 돌진 2종, 4장 착검 · 자객 찌르기. `Ch2Kit.InLane` 뒤 0.3m 여유 없앰, `RippleRing`(종지기 음파 · 3장 물결) 판정 폭 = 그린 폭(+0.3 없앰). 부채꼴 · 원은 원래 같았음. 컴파일만 확인 (돌진 맞는지 플레이로는 안 봄).

### ▶ 다음 작업 (여기서 이어서)
0. **(2026-11-19 밤 끝) 4장 강화형 6종 완료**: 병정 · 종지기 · 무희 · 자객 · 수리공 = Tripo zip(사용자가 직접 받음) → `tripo_import.py` → `Ch4_*_Plus` 프리팹, 무기 든 자세 기본형과 같게 맞음(화면 확인). **기사는 Tripo 오류가 계속 나서 블렌더로**: `Ch4_Knight_Plus.fbx` = 기본 기사 FBX 복사 + 더 낡은 텍스처(조금 어둡게 + 옅은 녹 얼룩), 그 위에 블렌더 + Codex 추가 장비 5개(`Kit_KP_*`: 어깨 갑옷 좌우 · 등 톱니 고리 + 진자 · 허리 갑판 · 투구 볏 — 가슴 진자 `KP_ChestPendulum`은 만들었지만 2등신이라 얼굴을 가려서 안 붙임 — 명세 `kit_ch4_knight_plus_spec.md`) + 주 무기 양날 진자 낫 `W_PendulumScythePlus`. 빌더 `Armor4`(뼈 · 보정 거리, 블렌더 +Y 앞 → 유니티에서 반 바퀴), `AttachArmor`, 어깨 갑옷은 쇄골 뼈에 붙임. 칼빛은 `EnemyCh4.Streak`이 `Weapon_*Plus`를 먼저 찾음. Tripo CLI 잔액 25 (웹 크레딧은 별도). 플레이 모드로는 아직 안 봄.
1. ~~4장 강화형 모델 6종~~ (위 0번으로 끝, 아래는 기록)
   - 원화 6장 완료: 크롬 ChatGPT 채팅 "강화형 원화 제작"(기본 원화 6장 첨부 → 한 마리씩) → `Art/tripo_input/ch4/<이름>_plus.png` → `_plus_main_dn.png`(keep_main · denoise). 모두 같은 느와르 배색, 더 크고 녹슬고 태엽 · 톱니 돌출, 붉은 눈, 빈손 주먹, 정면 A자.
   - Tripo(크롬, studio.tripo3d.ai) 작업 ID와 상태 (크레딧 715 → 약 130 남음):
     - 병정 `4dc0c746-71c2-41ad-b308-f4941621f691`: 생성 ✓ · 리토포(삼각형 1만) ✓ · 리깅 ✓ · **Export 눌렀음(FBX · Mixamo · 2K · 뼈대) — zip이 아직 ~/Downloads에 안 내려옴 → 다시 Export**
     - 종지기 `4c733bd3-5aa0-4722-96ca-1091e8258fed`: 생성 ✓ · 리토포 ✓ · 리깅 시작함 → Export
     - 무희 `29bd9856-a1a3-492d-bac2-5aed47264a3f`: 생성 ✓ · 리토포 ✓ · 리깅 시작함 → Export
     - 자객 `60abd5a4-3fc1-469e-ba71-e8b4f644661d`: 생성 ✓ · 리토포 ✓ · 리깅 시작함 → Export
     - 기사 `7eacf62d-99dd-4d05-9a89-6efe12124f3e`: 생성 ✓(목록 썸네일 있음) 그러나 열면 "Failed to load resource" (새로고침 3번) → 다시 열어 보고, 안 되면 Tripo의 Free Retry 또는 재생성. **리토포(35) · 리깅(20) 아직**
     - 수리공 `829ed399-560a-46de-9919-f6e0e03f80b3`: 생성 ✓ · 리토포 시작함 → 리깅 → Export
     - 팁: 오른쪽 목록 클릭이 가끔 안 먹음 → 주소 `studio.tripo3d.ai/workspace/{retopology|rigging}/<아무 글자>-<작업 ID>`로 바로 열면 됨. 리토포 패널은 기본이 Quad(40) — **Triangle(35) · 10000**으로 바꿀 것. 리깅 = Humanoid · Mixamo.
   - 가져오기: `python Art/tools/tripo_import.py <zip> Ch4_<이름>_Plus Game/Assets/Art/Characters/Ch4` (이름 = Soldier · Ringer · Dancer · Assassin · Knight · Repairman) → Unity 메뉴 **`CatTang/4장 적 프리팹 만들기 (Tripo)`** (빌더에 `Ch4_*_Plus` 6종 · 키 약 15% 크게 추가해 둠, 무기 · 손잡이 각도 · 스킬 모션 · idle · 걸음은 `Base4()`로 기본 적 것을 그대로 씀) → `EnemyFactory.AttachModelFor`가 `_Plus` 프리팹을 자동으로 씀(없으면 지금처럼 기본 모델 + 녹슨 붉은 색).
   - 그 뒤 확인: 강화형은 체형이 달라 무기 든 자세(IK로 기본 적 리그에서 푼 값)가 어긋날 수 있음 → 화면으로 보고, 어긋나면 이번 IK 풀이(아래 '무기 든 자세' 방법)를 `_Plus` 리그로 다시.
2. **4장 배경음악 (Gemini, 이전처럼)**: 내장 브라우저 Gemini가 로그아웃 상태 → 사용자 로그인 필요. bgm_stage_4 · bgm_midboss_4 · bgm_boss_4 (느와르 시계탑 · 진자 홀 · 별밤 문자판 위 쌍둥이) → 원본 `Art/audio_src/gemini/` → −16 LUFS 48kHz OGG → `Resources/Audio/`. `Music.PlayFor`는 이름만 맞으면 바로 씀(지금은 1장 곡으로 대신).
3. 새 효과음 귀로 확인 · 음량 손질, 칼빛 · 돌진 판정 플레이로 확인.
4. 중간 보스 진자 집행관 · 보스 쌍둥이 모델(같은 흐름) · `Ch4Bosses.cs` · `RunManager.HasMidBoss`에 4장. 보스 무기 자세도 IK 방법으로.
- **커밋 안 함** (이번 작업 전부 작업 트리에 있음).

### 남은 것 (4장)
- **(2026-11-19 밤 끝, 같은 세션) 맵 마무리 진행**: ① 바닥 = 옆 창 ChatGPT 4×4 리벳 철판 그림(원본 `Art/env_src/ch4/ground_ch4_gpt.png`, 사용자가 붙여 줌 — 옆 창은 파일 저장이 안 되고 localhost 전송도 막힘) → `Art/tools/ground_texture_ch4.py` → `Art/Env/Ch4/ground_ch4.png` → `Kit/Ch4/Ground.mat`(4장 키트 메뉴가 만듦, 거울 셰이더 아님). ② 지형지물 5종 블렌더 + Codex(`kit_ch4_landmarks_spec.md` → `blender_kit_ch4_landmarks.py`): LM_Clockwork · LM_PendulumCase · LM_GearStack · LM_BellFrame · LM_DialFragment, `Ch4Kit.Landmarks()` 스테이지별(벽면과 같은 장소). 괘종시계 장 FBX에 꼬인 면 2개 경고(눈에 안 띔). ③ 상인 = `MusicBox` 오르골 상자 위 돌림판(1.1m)에 섬, 알림 "오르골이 울리며…". **인형 모델 없음 → 지금은 3장 거울 고양이가 대신 섬.** 플레이 모드로 스테이지 1 · 4 방 화면 확인, 콘솔 에러 없음. 남은 것: 상인 오르골 인형 모델, 직접 한 판.
- **신전 느낌으로 (2026-11-19 사용자: "다른 스테이지에 비해 너무 공장 느낌, 기획서 보고 신전 같게")**: 기획서 컨셉(`docs/images/ch4/ch4_room.jpg` · `ch4_bg.jpg` 아치 회랑 · 기둥 · 붉은 깃발 · 랜턴 · 별밤)대로. ① 바닥 = 흑백 대리석 체커 + 은빛 문자판 · 별 상감(옆 창 ChatGPT, 사용자가 붙여 줌, 철판 원본은 `ground_ch4_ironplate_gpt.png`). ② 블렌더 + Codex 신전 키트(`kit_ch4_temple_spec.md` → `blender_kit_ch4_temple.py`): Facade_Colonnade(아치 너머 별밤 · 깃발 · 랜턴) · Facade_RoseDial(장미창 문자판 + 제단 벽감) · LM_ClockAltar · LM_ArchRuin · Deco_BannerPillar · Deco_Candles. ③ `Ch4Kit.Facades()` 회랑 · 장미창 위주(창고 선반 · 철골 뺌, 기계 벽은 기관실 · 진자만), `Landmarks()` 제단 · 아치 위주, `GroundBits`에 "None" 넣어 톱니 · 나사 · 철창 잔해 약 70% 줄임, `Ch4Kit.Temple()` 문 양옆 · 벽 끝 깃발 기둥 + 벽가 촛대. 플레이 모드로 스테이지 1 방 전체 화면 확인(회랑 48 · 장미창 21 · 깃발 기둥 27), 콘솔 에러 없음.
- **넓은 방 채우기 (사용자: 맵이 넓으니 오브젝트 더)**: 블렌더 + Codex `kit_ch4_temple2_spec.md` → `blender_kit_ch4_temple2.py`: Deco_NaveColumn(회중석 기둥, 방의 60%에 긴 변 따라 두 줄 6.5m 간격) · Deco_AngelStatue(네 귀퉁이 0~3) · Deco_Reliquary(0~1) · Deco_LanternStand(2~3) · Deco_Pew(엄폐물에 섞임, 부서짐) · Deco_FloorDial(만들었지만 **사용자: 바닥 시계 이상함 → 배치 안 함**). `Ch4Kit.TempleInterior()`, 큰 것은 `info.blocks`에 등록.
- **톱니 바닥 규칙 변경 (사용자: 방향 바꾸기 이득 없음)**: 축 쏴서 방향 바꾸기 없앰(축 충돌체 · `Reverse` 지움). 맞물린 자리 = **블렌더로 구운 고철 불꽃**(사용자: 레이저 불똥 섞은 게 이상함, 제철소 불꽃 사진 참고, 너무 크지 않게): `blender_bake_fx.py -- c4grind c4scrap` → `bk_c4grind`(반복, 불티 60 포물선 분수 + 쇳조각 + 달군 바닥 빛, 0.22초마다 겹쳐 띄움, 크기 3.4 = 불꽃 폭 약 1.4m) · `bk_c4scrap`(끼었을 때 한 번: 섬광 → 불티 70 사방 + 톱니 · 볼트 조각, 크기 3.2) + 0.5초마다 판정 폭 빨간 고리 · 화면 흔들림 · e4_clank. 색은 흰 → 노랑 → 주황(느와르 장이지만 쇳물 불꽃이라 따뜻하게). **최종 (사용자: 정확히 맞물리는 자리에 타격 콜라이더 + 거기에 불씨, 불꽃은 그림 말고 3D로)**: `GearPinch` = 두 톱니 중심선 위 이빨 끝 원이 겹친 구간 한가운데(모델 이빨 끝 4.0 · 뿌리 3.52 · 윗면 0.56 잼, 놓을 때 겹침 약 0.15m)에 박스 트리거(맞물림 선 2m × 0.9m × 높이 2.2, Ignore Raycast 층) → 0.5초마다 `Physics.OverlapBox`로 고양이 · 적 피해 10 + 옆으로 밀기, 같은 크기 `Telegraph.Lane` 예고(NoGlint). 이펙트 = 블렌더 3D 메시(`blender_fx_scrap3d.py` → `Resources/Kit/Ch4/FX_Spark.fbx` 뾰족 결정 · `FX_Scrap.fbx` 톱니 · 너트 · 볼트 · 꺾인 판)를 메시 파티클로: 불티 초당 90(속도 방향으로 눕힘, 흰 → 주황, 굵기 x·y 0.9~1.3 · 길이 0.35~0.6) · 고철 초당 5(돌며 떨어지고 주황 → 검은 쇠), 끼면 불티 120 + 고철 14. 재질 `FX_SparkMat` · `FX_ScrapMat`(URP Particles/Unlit, 4장 키트 메뉴가 만듦). 플립북 `bk_c4grind` · `bk_c4scrap`은 이제 안 씀. 기획서 md 챕터 4 · 수치 기준 같이 고침. 느린 시간으로 낀 장면 화면 확인.
- **종지기 음파 고리 3D (사용자: 파동 날리는 원도 이미지랑 블렌더로 제대로)**: 그림 = Codex imagegen(`docs/images/fx_src/prompts/c4_soundwave.txt`, 참고 `ch4_room.jpg` → `c4_soundwave.png` 2048×512 마젠타 바탕, 빨간 물마루 + 흰 심지 + 별 + 바깥 잔물결 · 점선 + 안쪽 시계 눈금, 물결 3개) → `Art/tools/fx_soundwave_key.py`(마젠타 → 투명, 위아래 여백 자름) → `Resources/Kit/Ch4/FX_SoundWave.png`. 3D = `Art/tools/blender_fx_soundwave3d.py` → `FX_SoundSeg.fbx`(1×1 조각, 바깥으로 치우친 물마루 높이 0.22, UV U 0~1/3 = 물결 하나). `SoundRing`(Ch4Kit.cs) = 조각 36개를 둘레에 놓고 반지름만 키움(띠 두께 = 판정 폭 그대로, RippleRing과 같은 판정), 재질 `FX_SoundWaveMat`(반투명, 4장 키트 메뉴). 종지기 `RingerBell`이 씀(자산 없으면 예전 선 고리). 화면 확인: 빨간 물결 띠 + 흰 심지로 보임. 판정 폭 `RingWidth` 0.5라 띠가 가는 편 — 굵게 하려면 판정 폭도 같이 키워야 함.
- **상인 오르골 인형 = 블렌더로 완료 (사용자: Tripo 말고 블렌더로)**: 원화 `Art/tripo_input/merchant/ch4_musicdoll.png`를 `codex exec -i`로 넘겨 `kit_ch4_merchant_spec.md` → `blender_kit_ch4_merchant.py` → `Kit_Ch4_Merchant.fbx`(통짜, 뼈대 없음) → 4장 키트 메뉴가 상인 전용 따뜻한 배색표 `ModelPalette.Ch4Merchant`로 `Resources/Merchants/Ch4_Merchant.prefab`(키 2.6m) 생성 → `MerchantFigure`가 거울 고양이 대신 씀 + `MusicDollTurn`(초당 24° 돎, 고양이 4m 안이면 돌아봄). 플레이 모드로 오르골 위 화면 확인. (아래 Tripo 메모는 지난 계획, 안 써도 됨)
- ~~상인 오르골 인형 — 원화만, Tripo는 나중에 (2026-11-19 사용자 지시)~~: 옆 창 ChatGPT 대화 "캐릭터 원화 작성"(`chatgpt.com/c/6afe91e2-40cc-83ec-839e-2c65550670e7`) 마지막에 요청(상아 자기 · 금 · 포도주 벨벳 · 호박색 눈 · 오르골 핀 실린더 왕관 · 등 태엽 열쇠, 빈손 A자). 원본을 `Art/tripo_input/merchant/ch4_musicdoll.png`로 → keep_main · denoise → **Tripo 생성 + 리깅은 나중에** (CLI 잔액 25라 웹 크레딧) → `Art/Characters/Merchant/Ch4_Merchant.fbx` → 빌더 `Merchants` 표에 `("Ch4_Merchant", 3.0f)` 추가 → `Resources/Merchants/Ch4_Merchant` 이 생기면 `RoomBuilder.MerchantFigure`가 거울 고양이 대신 씀. 기획서: 오르골 상자 위에서 천천히 돌며 장사(돌기는 아직 안 넣음).
1. ~~바닥 텍스처~~ (위에서 끝)
2. **진자 집행관 · 쌍둥이(시침 · 분침) · 오르골 인형 모델**: 일반 적 6종과 같은 흐름(빈손 원화 → Tripo → 무기 따로, 쌍둥이 무기 W_Greatsword · W_Rapier 있음). 집행관은 팔이 칼날이라 몸과 같이.
3. `EnemyCh4.cs` · `Ch4Bosses.cs` (3장 구조 그대로), `RunManager.HasMidBoss`(지금 `chapter <= 3`) · `MidBossName`에 4장 추가, `EnemyFactory` 4장 분기.
4. 상인 오르골 상자(`MusicBox` 프리팹 있음) 연결, 4장 지형지물(LM_*) 모델.
5. 한 판 이어서 직접 해 보기(톱니 바닥 이동감 · 맞물림 피해 · 굴러가는 톱니바퀴 튕김).

## 2026-11-19 (오후) — 4장 기획 확정 · 방 크기 장/스테이지별 · 스테이지별 문 · 복도

### 4장 기획 (질문으로 전부 확정, 기획서 PPT는 아직 — 다른 세션에서)
- 전부 `docs/Cat_Tang_레벨_디자인.md`에 씀: '챕터 4' · '수치 기준 (챕터 4)' · '방 크기 규칙' · 보스 상세 '진자 집행관' · '시침 · 분침 쌍둥이'. 말투는 기존 기획서처럼 명사형 개조식.
- 요약: 테마 **시계 장치 속**(모티브 '맞물림', 파풀라투스 시계탑 분위기, 느와르 흑백 대비 · 붉은 포인트(적 공격 궤적은 빨간 계열), 현실 흔적은 나중에 스토리로 — 지금은 몽환적 시계 공간에 집중) · 스테이지 아래 → 위(태엽 창고 → 톱니 기관실 → 진자 홀(중간 보스) → 종루 → 문자판 뒤 회랑 → 꼭대기 문자판(보스, 원형 40m)) · 방 기믹 **돌아가는 톱니 바닥** · 오브젝트 태엽 스프링 · 톱니바퀴 · 낡은 종 · 상인 **태엽 오르골 인형** · 일반 적 6종(태엽 병정 · 종지기 · 오르골 무희 · 초침 자객 · 진자 기사 · 시계 수리공) · 중간 보스 **진자 집행관**(기믹 틱-톡 박자 · 판결의 수 · 끊어진 추 재기) · 보스 **시침 · 분침 쌍둥이**(체력 따로, 남은 쪽 분노, 상시 별빛 낙하, 기믹 3종 쿨 무작위: 순서대로 연속 명중 · 뒤섞인 문자판 · 멈춘 톱니 돌리기, 패턴 기본 번갈아 + 한쪽 50% 이하면 약한 패턴 가끔 겹침).
- **4장 기획서 PPT 만들 때**: 3장 생성기 `docs/tools/build_ch3_ppt.js`를 본떠 `build_ch4_ppt.js` → `docs/1159_4장_기획서_20261119_정승운_Ver01.pptx`. 규칙은 메모리 deck-style-prefs(확정만 · Noto Sans KR · 비율 유지 · 명사형 개조식 · 나열은 " / " → `slash_dots.py`). 컨셉 시안은 3장처럼 Codex 이미지 생성(`docs/images/ch4/`). 공통 레벨 디자인 기획서에도 '방 크기 규칙' 표 반영 필요.
- **4장 구현 할 일(제안 순서)**: ① `build_tables.py` 챕터 · 적 · 보스 표에 4장(체력 중간 보스 5400 · 보스 시침 5000 / 분침 4180) → CSV · xlsx ② 4장 키트(톱니 바닥 · 태엽 스프링 · 톱니바퀴 · 종 · 엄폐물 · 벽면 · 오르골 상자) 블렌더 + Codex ③ 적 6종 · 집행관 · 쌍둥이 · 오르골 인형 원화(ChatGPT) → Tripo(크레딧 확인, 3장 때 잔액 25) ④ `EnemyCh4.cs` · `Ch4Bosses.cs` · `Ch4Kit.cs`(3장 파일 구조 그대로) ⑤ `RunManager.HasMidBoss`(지금 `chapter <= 3`) · `MidBossName`에 4장 추가.

### 방 크기 = 장 · 스테이지별 (사용자: 몬스터 수에 비해 방이 작음, 스테이지 2부터)
- `RoomBuilder.RoomSizeFor(장, 스테이지)` 표(md '방 크기 규칙'과 같음): 1장 28×24 · 34×29 · 36×30 · 38×31 / 2장 34×29 → 40×33 / 3장 36×30 → 42×35 / 4장 38×32 → 44×37. 5장 이후는 4장 값. 중간 보스 홀 · 보스 방은 그대로.
- 방 칸 간격 = 방 + 복도 `Corridor`(10m), 스테이지마다 계산(예전 고정 `CellSize` 38×34는 보스 방만). `CellDepth` 지움.
- 중간 보스 홀: 들어오는 벽이 스테이지 2 마지막 방에서 10m 거리에 오게 홀 가운데를 밀어 맞춤(예전엔 3장 극장만), 뒷문 복도 10m, 스테이지 3 첫 방은 `RoomBuilder.BackAttachDistance`(홀 깊이/2 + 10 + 스테이지 3 방 깊이/2)에 지음(`RunManager`).
- 오브젝트 수: `RoomBuilder.Density`(기준 방 대비 넓이 배율 1~1.8) · `Scaled(n, info)`로 1장 화약통 · 엄폐물, 2장 광차(최대 2) · 증기 밸브 · 공중 짐 · 엄폐물 · 굳은 승객, 3장 기억 수정 · 분수 샘 · 엄폐물 수를 늘림. 지형지물은 배율 1.5 이상이면 하나 더. 2장 객차 자리도 방 크기에 비례.
- 확인: 플레이 모드에서 1 · 2 · 3장 스테이지 2(+ 중간 보스 홀)를 지어 방 사이 벽 틈이 전부 10m, 홀 → 스테이지 3 틈 10m인 것 숫자로 확인. 2장 스테이지 4 방(40×33)을 위에서 화면으로 봄. 콘솔 에러 없음(GameUI · HudView 널 에러는 Run 씬을 바로 틀어서 생긴 것, 기존).
- 확인 안 됨: 처음부터 이어서 한 판(실제 웨이브 · 카메라 · M 지도), 큰 방에서 적 생성 위치 · 상인 자리.
- 테스트 팁: Run 씬을 바로 플레이하면 `RunManager.Instance` · `Chapter`가 비어 있을 수 있음 → 리플렉션으로 `Instance` 넣고 `Tables.Load()` 뒤 `chapter` · `Chapter` 바꿔 `BuildStage(int, RewardKind, Vector3?)` 호출.

### 스테이지별 문 · 복도 · 문틀 · 보상 문 (완료)
- 사용자: 1장만 나무 문 모델이 있고 2 · 3장은 회색 상자 → **스테이지마다 다른 문 + 복도 디자인을 블렌더 + Codex로 만들어 연결**.
- 명세: 공통 `Art/tools/specs/_gates_common.md`(조각 종류 · 크기 · 이름 · 내보내기) + 장별 `kit_ch{1,2,3}_gates_spec.md`(스테이지별 모습). Codex 실행: `codex exec -s workspace-write - < Art/tools/specs/kit_chN_gates_spec.md > Art/tools/specs/kit_chN_gates.log` → `Art/tools/blender_kit_chN_gates.py` → `Game/Assets/Art/Characters/ChN/Kit_*.fbx` + 미리보기 `Art/tools/specs/blender_kit_chN_gates_preview.png`.
- 조각 (장마다 스테이지 n = 1~4):
  - `Gate_Sn`: 미리 쪼갠 막는 문(`Piece_XX` + `Splinter_0~2`, 4.6 × 2.4~2.7m) — 기존 `WoodGate`가 조각을 날림.
  - `Corridor_Sn`: 복도 겉모습 한 덩어리(바닥 4.6m + 양옆 벽, 길이 10.4m, 벽 높이 2.5 이하).
  - `Portal_Sn`: 방 벽 구멍 둘레 문틀(통로 폭 4.6m 비움, 폭 6.8m).
  - `RewardDoor`(장마다 하나): `Frame` · `Leaf_L` · `Leaf_R`.
- 스테이지 모습(요약): 1장 S1 목장 통나무 문 · 바위 협곡 / S2 판자 바리케이드 · 판자 보도 / S3 보안관 감옥 창살 · 벽돌 골목 / S4 녹아내리는 철문 · 녹는 시계 기둥 길. 2장 S1 화물 상자 벽 · 선로 / S2 개찰 차단기 · 캐노피 통로 / S3 객차 연결문 · 객차 통로 / S4 철 셔터 · 벽돌 터널. 3장 S1 사암 + 수정 벽 · 물 위 모래길 / S2 거울 판자 바리케이드 · 거울 판자 보도 / S3 대리석 + 수정 결계 · 대리석 다리 / S4 구름 벽 · 구름길.
- **Codex 완료 (이 세션 끝에 확인)**: 3장 모두 FBX 13개(문 · 복도 · 문틀 S1~4 + 보상 문) · 로그 `DONE` · 미리보기 PNG 3장 나옴. 미리보기 검토 · 프리팹 빌드부터 하면 됨. (그 전 상태: Codex 3개가 실행 중, 스크립트 자체 검사 실패를 고치는 중이었음 — 1장 `Corridor_S4` 삼각형 8008 > 7000, 2장 `Portal_S2` 통로 막힘, 3장 `Corridor_S3` 길이 10.54m. 그때까지 나온 FBX: 1장 문 S1~4 · 복도 S1~3 · 문틀 S1~3, 2장 문 S1~2 · 복도 S1~2 · 문틀 S1, 3장 문 S1~3 · 복도 S1~2 · 문틀 S1~2. 보상 문은 아직 없음. 로그 끝(`DONE`)과 FBX 13개 × 3장이 다 있는지 먼저 확인하고, 멈췄으면 같은 명령으로 다시 돌릴 것(스크립트가 있으니 Codex가 이어서 고침).)
- 코드(컴파일 확인함):
  - `Kit.Stage("Gate")` → `Gate_S{stageIndex+1}`(보스 방은 S4로 고정).
  - `WoodGate.Make`: 스테이지 문 → 그 장 `WoodGate` → `@1/WoodGate` 순서로 찾음.
  - `RoomBuilder.DressCorridor`: `Corridor_Sn`을 복도 가운데에 놓고(길이 비례), `Portal_Sn`을 이쪽 · 저쪽 방 벽에 놓음. 모델이 있으면 맨 상자 바닥 · 벽은 렌더러만 끄고 충돌은 그대로, 없으면 예전처럼 맨 상자 + 벽 모듈. 스테이지 방 사이 복도와 중간 보스 홀 뒷문 복도 둘 다 적용(뒷문 복도는 지을 때 스테이지가 S2라 S2 모양).
  - `RoomBuilder.Door`: 그 장에 `RewardDoor`가 있으면 그걸 씀(yaw 180, 정면이 고양이 쪽), 없으면 예전 맨 상자 문.
- 빌더: `EnemyModelBuilder` 장별 표(1장 `Models` · `Models2` · `Kit3`)에 13줄씩 추가, 새 메뉴 **`CatTang/스테이지 문 · 복도 만들기 (블렌더)`**(`BuildGates`) = 이 조각들만 1 · 2 · 3장 팔레트로 빌드(FBX가 있는 것만) → `Resources/Kit/ChN/{Gate_Sn, Corridor_Sn, Portal_Sn, RewardDoor}`.
- **완료 (2026-11-19 오후 늦게, 다른 세션에서 이어서)**: 미리보기 3장 검토(명세대로) → 메뉴로 프리팹 39개 빌드(`Resources/Kit/Ch1~3/`) → 플레이 모드에서 1장 S1~4 · 2장 S1~4 · 3장 S1~4를 지어 위에서 찍어 봄: 복도 · 문틀이 벽 구멍과 맞고 방향 맞음, 문이 문틀 안에 닫힘(`WoodGate.Close`), `Break` 뒤 꺼짐, 보상 문 = Frame · Leaf_L · Leaf_R, 정면 맞음. 콘솔 에러 없음.
  - 확인 안 됨: 실제 한 판 이어서(문 부서지는 연출 눈으로), 1장 기존 `WoodGate`는 이제 안 쓰임(지우지 않음). 기획서(공통 레벨 디자인)에 문 · 복도 반영할지는 사용자에게 물을 것.

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
- **빌드 버그 (사용자: 빌드에서 커서 안 나옴 · 레벨 업 선택지 안 나옴)**: 원인 = 코드에서 `Shader.Find("CatTang/...")`로만 찾는 셰이더가 빌드에서 빠짐 → `HudView.Build`의 `new Material(null)` 예외 → `GameUI.Awake`가 중간에 멈춰 그 뒤(조준 커서 · 특성 HUD · 창 층 `screens` · 토스트)가 안 만들어짐. 에디터에선 안 보임.
  - `Editor/ShaderBuildInclude.cs`(빌드 전처리 + 메뉴 `Cat_Tang/Include Code Shaders In Build`): CatTang 셰이더 전부(Toon · MirrorFloor는 변형이 많아 제외 — 재질이 써서 원래 들어감) + Sprites/Default를 Graphics Settings '항상 포함'에 넣음. **새 셰이더를 `Shader.Find`로만 쓰면 이게 챙겨 줌.**
  - `HudView`: FX 셰이더가 없으면 이펙트만 건너뜀(예외 안 냄).
  - **빌드 점검** `Scripts/Dev/BuildSmoke.cs`: `1159.exe -smoke -logFile smoke.log` → 판으로 바로 들어가 커서 · HUD · 셰이더 · 레벨 업 창 확인 후 종료, 로그의 `[Smoke]` 줄. 인자 없으면 아무것도 안 함, 저장 안 건드림. 결과: GameUI · AimCursor · HudView · 셰이더 · 레벨업창 모두 True, 오류 0.
- **마을 지도 (M)** `UI/VillageMap.cs` (사용자: 마을에서도 맵): 판 지도와 같은 양피지 판 · 고양이 머리. **열린 곳만**(사용자): 다리가 이어진 섬(둘레 모양 그대로, 짙은 테) · 열린 다리 · 해금된 시설 · 시간의 문(마름모 + 이름)만 그림, 잠긴 섬 · 다리 · 시설은 안 그림(닫힌 섬 위 표식도 뺌). 범위도 열린 섬 기준(최소 60m)이라 열린 곳이 적으면 크게 보임. 다른 창 · 대화 · 깨어나는 중엔 안 열림, Esc는 지도만 닫음(`VillageMap.EscUsed` → 마을 메뉴 안 열림). 안내 줄에 'M 지도'.
  - 섬 모양은 `IsleOutline`(섬 둘레 72점, 섬 로컬)에서 읽음 — 섬 윗면이 정적이라 플레이 중엔 static batching으로 메시가 합쳐져 못 읽기 때문. `VillageKitBuilder.BuildIsle`가 붙이고, 지금 씬은 메뉴 `Cat_Tang/Village Map Outlines`로 채워 저장함.
  - 확인함(에디터 플레이): 다 열린 상태(섬 5 · 다리 4 · 시설 4 · 문 · 고양이 위치), 총포상 · 의상실 다리를 잠근 상태(플레이 중에만 해금 장을 임시로 올림 → 섬 3개만, 확대됨). Esc · M 키로 직접 열고 닫기는 사용자 확인 필요.

- **로비 게임 종료** (사용자): 메인 메뉴 4번째 '게임 종료' → 확인 창 '정말 종료하시겠습니까?'(설정 창 판 60%, 예 · 아니오, Esc · 바깥 클릭 = 닫기) → 예 = `Application.Quit`. 설정 창의 '게임 끝내기'는 뺌(사용자). `LobbyScreen.OpenQuitConfirm` · `QuitGame`. 에디터 화면 확인함.
- **판 안 ESC = 전투 포기 확인** (사용자): 창이 없을 때 ESC → '전투를 포기하고 마을로 이동하시겠습니까?' + '이번 판에서 얻은 시간의 조각 N개만 가져갑니다'(게임 멈춤) → 예 = `RunManager.GiveUp`(판 끝 처리 `Meta.RunEnded` + 얻은 조각만 더함, 결과 창 없이 마을로) / 아니오 · X = 이어서. 판 지도가 열려 있으면 ESC는 지도만 닫음(`MapView.EscUsed`). 조각 계산은 `RunManager.ShardGainPreview`(기억 · 코스튬 보너스 포함, 죽었을 때와 같음). 안내 줄에 'Esc 포기'. 확인함: 창 · 멈춤 · 아니오로 이어짐. '예'는 에디터 저장을 바꿔서 직접 안 눌러 봄.

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
