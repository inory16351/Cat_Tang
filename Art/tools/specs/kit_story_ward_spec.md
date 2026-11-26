# 스토리 맵 5 '11:59의 병실' 키트 명세 (2026-11-26)

블렌더 스크립트 `Art/tools/blender_kit_story_ward.py` (헤드리스 실행:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_ward.py`)
→ `Game/Assets/Art/Characters/StoryWard/Kit_H_<이름>.fbx` → 유니티 메뉴 `CatTang/스토리 맵 5 병실 키트 만들기`
→ `Resources/Kit/Story/H_<이름>.prefab` → 메뉴 `CatTang/스토리 맵 5 병실 맵 조립` → `Resources/Story/StoryWard.prefab`.
틀은 스토리 맵 4 공방(`blender_kit_story_workshop.py` · `StoryWorkshopBuilder.cs`)과 같음.

## 장면
지금 이 순간, 시계공 노인이 누운 밤의 병실. 마지막 1분(11:59). 침대에 노인 그림자(시트 아래 누운 실루엣), 구석에 지켜보기만 하는 사신 그림자(두건 망토 · 얼굴 자리에 시계 · 낫).
머리맡 벽시계 · 링거 · 심전도 모니터 · 협탁 위 낡은 그림책(고양이 영웅) · 방문객 의자에 4장의 빨간 숄.
색 = **밤 병실이지만 차갑기만 하지 않게**: 옅은 민트 · 청자색 벽, 크림 · 연회색 타일 바닥(청크마다 다른 패턴), 하얀 시트, 은빛 금속, 따뜻한 나무 협탁 · 의자, 벽시계는 호두 + 놋쇠(공방에서 온 시계),
창 밖 남색 밤 + 달, 모니터 초록빛, 협탁 스탠드 호박빛. **빨강 = 숄 하나**. 그림자(노인 · 사신) = `shadow` + 가는 `shadow_rim` 띠.
카메라 = 남쪽 위 쿼터뷰(약 57°). 벽 위쪽은 어둡게 열림. 장식 규칙: 조각마다 역할 재질 5~8(작은 움직이는 조각만 2~4), 한 재질이 겉면 55% 넘지 않게, 몰딩 · 상감 · 금 장식. 0.04m보다 가는 것 없음.

## 좌표 · 크기 기준
- 블렌더 (X, Y, Z) = 유니티 (X, Z, Y). +Y = 북쪽. 조각 정면 = 블렌더 −Y(카메라 쪽).
- 고양이 키 2.17m, 노인(사람) 치수 × 1.1 가독성. 방 안쪽 24 × 20m: x −12 ~ 12, y −10 ~ 10. 벽 높이 4.8, 두께 0.5. 남쪽 벽 = 낮은 징두리 1.1m + 가운데 입구(x ±1.8).
- 맵 배치(유니티 빌더): 침대 (0, 0, 5.2) 머리 = 북쪽 · 협탁 (2.9, 0, 7.0) · 링거대 (−2.3, 0, 6.6) · 모니터 (−3.1, 0, 7.8) · 벽시계 = 북벽 x −6, 중심 높이 3.0 · 창 북벽 x 6.5 하나(크게) ·
  방문객 의자 (3.6, 0, 3.4) 침대를 봄 · 사신 그림자 (9.0, 0, 7.6) 침대를 봄 · 서쪽 벽 수납장 (−11.2, 0, 1) · 가림 커튼 레일 (동쪽 x 6 ~ 11, y 2) ·
  전투 원 = 남쪽 가운데 (0, 0, −3.5) 반지름 6 (가구 없음) · 도착 (0, 0, −8.6).

## 재질 역할 (회색 Principled, 이름 = 역할 → 유니티 `StoryWardPalette`)
| 역할 | 색(미리보기) | 쓰는 곳 |
|---|---|---|
| `tile` / `tile_light` / `tile_dark` / `tile_seam` | 크림 / 연회색 / 회청 / 짙은 이음 | 바닥 타일 청크 |
| `wall` / `wall_pat` / `wainscot` / `molding` / `plaster_dark` / `void` | 옅은 민트 / 청자 무늬 / 청록 징두리 / 크림 몰딩 / 짙은 남청 / 검정 | 벽 |
| `wood` / `wood_dark` / `wood_light` / `walnut` | 참나무 / 호두 / 메이플 / 짙은 호두 | 협탁 · 의자 · 벽시계 |
| `steel` / `steel_dark` / `chrome` / `brass` / `gold` | 은 / 짙은 철 / 밝은 크롬 / 놋쇠 / 금 | 침대 틀 · 링거대 · 시계 테 |
| `sheet` / `sheet_shade` / `pillow` / `blanket` / `blanket_pat` | 흰 시트 / 그늘 / 베개 / 연청 담요 / 담요 무늬 | 침대 |
| `curtain` / `curtain_dark` | 연한 베이지 · 청회 | 가림 커튼 · 창 커튼 |
| `dial_face` / `dial_mark` | 상아 / 세피아 | 벽시계 문자판 |
| `iv_bag` / `iv_fluid` / `tube` | 반투명 흰 / 옅은 노랑 / 투명 흰 | 링거 |
| `screen` / `screen_glow` / `plastic` / `plastic_dark` | 검정 화면 / 초록 선 / 연회 / 회색 | 모니터 · 기계 |
| `book_cover` / `book_page` / `book_gold` | 적갈 표지 / 크림 종이 / 금박 | 그림책 |
| `shawl_red` / `shawl_red_dark` | 진한 빨강(숄만) | 숄 |
| `glass` / `night` / `moon_glow` / `lamp_glow` | 밤 유리 / 남색 밤 / 달빛 / 호박빛 | 창 · 스탠드 |
| `flower` / `leaf` / `vase` | 흰 꽃 / 초록 / 하늘빛 자기 | 협탁 꽃병 |
| `shadow` / `shadow_rim` | 검보라 / 금빛 테 | 노인 · 사신 그림자 |

## 원점 규칙 (KEEP = 블렌더 좌표 그대로, 유니티가 피벗으로 씀)
`Ground` `Room`(맵 원점) · 시계 바늘 `HandHour` `HandMinute` `HandSecond`(축, 12시 = +Z 위, 바늘은 문자판 앞 −Y로 조금 뜸) ·
`WallClock`(뒷면 가운데 = 벽에 붙는 점, 문자판 가운데가 원점에서 Z +0 높이) · `ReaperHead`(목 = 회전 축) · `IVDrop`(물방울 가운데).
나머지 = 가로 가운데 · 바닥 Z 0.

## 조각 (이름 · 크기 · 삼각형 예산)
1. `Ground` (45000) — 바닥: 보로노이 불규칙 다각형 청크(약 3.2m) 하나마다 타일 한 패턴(정사각 0.6 / 마름모 / 헤링본 중 하나, `tile` `tile_light` `tile_dark` 중 하나 + 이음 `tile_seam`, 대비 낮게). 전투 원(0, −3.5, 반지름 6)엔 얇은 `tile_seam` 원 테두리 + 12방향 작은 금 눈금(`brass`). 밑판 · 바깥 `void`.
2. `Room` (28000) — 북 · 서 · 동 벽 높이 4.8: 징두리 1.1m(`wainscot` + `molding` 띠) · 벽면 `wall` + 은은한 세로 줄무늬(`wall_pat`) · 처마 몰딩 · 기둥 4개. 북벽 창 구멍 (x 6.5, 폭 3.4, 높이 1.0 ~ 3.8). 남벽 = 낮은 징두리 + 가운데 입구 양옆 문틀(`wood_dark` + `brass` 손잡이 판). 벽 콘센트 · 산소 공급 레일(침대 머리 위 북벽, `chrome` + `plastic`).
3. `Window` (6000) — 큰 창: 창틀(`molding`) · 십자 창살 · 밤 하늘 판(`night`) + 달(`moon_glow`) + 별 점 · 양옆 커튼(`curtain` 주름) · 창턱에 작은 화분. 원점 = 바닥 가운데(창 아래 벽 앞).
4. `Bed` (10000) — 병원 침대 2.4 × 1.25(긴 쪽 = Y), 매트리스 윗면 0.75: `steel` · `chrome` 틀 + 머리판 · 발판(`wood_light` 판넬), 접이식 난간 양쪽, 바퀴 4, 머리 쪽이 30° 들림. 시트(`sheet` + `sheet_shade` 주름) · 베개 · 접힌 담요(`blanket` + `blanket_pat` 체크) 발치. **노인은 따로**(OldManLying) — 침대엔 사람 자리만 비움(시트가 살짝 패인 정도).
5. `OldManLying` (4000) — 침대에 누운 노인 실루엣(얼굴 없음): 들린 머리판에 기대 누운 자세, 시트를 가슴까지 덮음(시트 위로 드러난 머리 · 어깨 · 한 팔이 시트 위에 놓임, 손은 침대 가장자리 쪽 +X로). 키 1.72m. 전부 `shadow`, 시트 경계 · 머리 윤곽에 가는 `shadow_rim`. 원점 = 침대 매트리스 가운데 윗면(Bed와 같은 자리에 놓으면 맞게).
6. `Nightstand` (6000) — 협탁 0.7 × 0.55, 높이 0.72: `walnut` · `wood` + `brass` 손잡이 서랍 2, 위에 스탠드(`lamp_glow` 갓, 높이 0.55) · 물컵 · 돋보기 안경 · 꽃병(`vase` + `flower` 3송이 + `leaf`). 그림책은 따로.
7. `PictureBook` (1500) — 펼친 낡은 그림책 0.5 × 0.34: `book_cover` 표지 · `book_page` 두 면(가운데 고양이 실루엣 `shadow` + 노을 띠 `book_gold`) · 귀퉁이 닳음. 원점 = 바닥 가운데(협탁 위 0.72에 놓임).
8. `WallClock` (6000) — 공방에서 온 둥근 벽시계(문자판 반지름 0.95): `walnut` 테 + `brass` 안 테 + `gold` 꼭지 장식 · 로마 숫자 12 · 분 눈금 60(12시만 `shawl_red`) · 아래 짧은 진자 상자. 원점 = 문자판 가운데 높이의 뒷면 가운데(벽에 붙는 점). 바늘은 따로.
9. `HandHour` (500) 길이 0.5 · `HandMinute` (500) 0.78 · `HandSecond` (300) 0.85 가는 바늘(`iron`/`steel_dark` + 끝 `shawl_red` 평형추, 초침만 `shawl_red`). 원점 = 축, +Z 방향 = 12시.
10. `IVStand` (3000) — 링거대 2.1m: `chrome` 기둥 · 바퀴 5 · 걸이 고리 · 수액 주머니(`iv_bag` + `iv_fluid` 차오름 선) · 점적통(`tube` 원통, 원점 기준 높이 1.45 ~ 1.62) · 늘어진 줄(`tube`)이 침대 쪽 −X → +X로. **점적통 가운데 높이 = 1.52** 를 머리말 주석에 적기.
11. `IVDrop` (120) — 물방울 0.06 (`iv_fluid`). 원점 = 가운데.
12. `Monitor` (4000) — 심전도 모니터 받침대 1.4m + 화면 0.55 × 0.4(`screen` 바탕 + 초록 심전도 선 `screen_glow` + 숫자 판) · `plastic` · `plastic_dark` 몸 · 케이블. 화면 정면 = −Y로 살짝 동쪽(침대 쪽)을 봄.
13. `VisitorChair` (4000) — 나무 팔걸이 의자(`wood` · `walnut` 방석 `blanket`), 등받이에 **빨간 숄**(`shawl_red` · `shawl_red_dark` 주름 + 술)이 걸쳐짐.
14. `Cabinet` (4000) — 서쪽 벽 수납장 1.8 × 0.6 × 2.0: `wood_light` + `plastic` 문 · `brass` 손잡이 · 위에 약병 · 수건 더미.
15. `CurtainRail` (5000) — 가림 커튼: 천장 레일(`chrome`, 높이 3.4, 길이 5) + 반쯤 걷힌 주름 커튼(`curtain` · `curtain_dark`, 바닥 0.3 위까지).
16. `ReaperBody` (5000) — 사신 그림자 몸 2.7m: 두건 망토(바닥까지 끌리는 넓은 자락, 찢긴 끝), 두 손으로 세워 든 큰 낫(자루 3.0 · 날 1.4), 목 위는 비움. 전부 `shadow`, 자락 끝 · 낫 날 테두리에 `shadow_rim`. 정면 = −Y. 원점 = 바닥 가운데. **목 위치(머리 피벗) 좌표를 머리말 주석에 적기.**
17. `ReaperHead` (2500) — 두건 + 얼굴 자리에 둥근 시계(문자판 `dial_face`, 바늘 11:59 고정 `shadow`, 테 `shadow_rim`), 두건은 `shadow`. 원점 = 목(회전 축), 정면 = −Y.
18. `Door` (3000) — 남쪽 입구 문틀에 반쯤 열린 병실 문(`wood_light` + 작은 유리창 `glass` + `chrome` 손잡이). 원점 = 바닥 가운데, 문틀 폭 3.6.
19. `Hourglass` (1500) — 바닥에 서 있는 모래시계 0.7m (`brass` 틀 · `glass` · 모래 `iv_fluid`). 연습 ② 픽업용.

미리보기: `Art/tools/specs/blender_kit_story_ward_preview.png` — Ground 빼고 모든 조각을 두세 줄로.
