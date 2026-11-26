# 스토리 맵 4 '시계를 멈춘 공방' 키트 명세 (2026-11-25)

블렌더 스크립트 `Art/tools/blender_kit_story_workshop.py` (헤드리스 실행:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_workshop.py`)
→ `Game/Assets/Art/Characters/StoryWorkshop/Kit_W_<이름>.fbx` → 유니티 메뉴 `CatTang/스토리 맵 4 공방 키트 만들기`
→ `Resources/Kit/Story/W_<이름>.prefab` → 메뉴 `CatTang/스토리 맵 4 공방 맵 조립` → `Resources/Story/StoryWorkshop.prefab`.
틀은 스토리 맵 3 호숫가(`blender_kit_story_lake.py` · `StoryLakeBuilder.cs`)와 같음.

## 장면
노년의 시계공이 아내를 떠나보낸 비 오는 밤의 공방. 옛 풍습대로 공방의 시계를 하나씩 멈춘다.
색 (2026-11-26 사용자 확정 — 처음엔 4장 느와르였으나 4장 전투 맵과 같은 공간처럼 보여 바꿈) = **기억 속 집: 따뜻한 나무 위주**.
호두(짙은 갈색) · 참나무(꿀색) 바닥 · 벽 판재 · 몰딩, 크림 회벽, 놋쇠 · 금빛 시계 테 · 진자, 초록 펠트 작업대, 붉은 갈색 가죽 의자, 따뜻한 상아 문자판, 짙은 포도주 커튼,
문자판 바닥 = 메이플 · 참나무 · 호두 상감 + 놋쇠 숫자. **그녀의 숄만 진한 빨강**(화면이 흑백으로 빠질 때 이것만 남김 — 연출은 StoryWorkshop.cs).
빛 = 따뜻한 점광원 `WarmLight_1~10`(시계를 멈출 때마다 하나씩 꺼짐) + 창 달빛(차가운 푸른빛 `MoonLight_L/R`).
사람 = 그림자 실루엣(얼굴 없음) — 노인 그림자는 3장 퀘스트 `Kit/Ch3Quest/OldManShadow` + `OldManArm`을 그대로 씀(팔 = `ArmPivot`).
카메라 = 남쪽 위 쿼터뷰(CameraFollow offset (0, 17, −11), 약 57°). 벽 위쪽은 어둡게 열림.

## 좌표 · 크기 기준
- 블렌더 (X, Y, Z) = 유니티 (X, Z, Y). +Y = 북쪽. 조각 정면 = 블렌더 −Y(유니티 −Z, 카메라 쪽).
- 고양이(주인공) 키 2.17m (CatHero 프리팹 뼈 비율: 골반 0.68 · 어깨 1.20 · 손 0.97 · 목 1.24 · 머리 0.93m). 노인 그림자 1.72m.
  가구는 노인(사람) 치수에 가독성 1.1배: 작업대 윗면 1.0(고양이 손 높이) · 의자 앉는 면 0.52 · 등받이 1.15.
  상호작용 물체는 고양이보다 크게 읽히게: 톱니 지름 2.1(고양이 키 ≈ 1배, 축 높이 1.25) · 벽시계 문자판 지름 1.0~1.44(고양이 머리 0.93보다 큼, 중심 높이 2.55~3.15 = 고양이 머리 위) ·
  괘종시계 높이 3.74(고양이 1.7배) · 문자판 지름 1.64 · 중심 높이 2.1(노인 그림자가 디딤대 위에서 팔 70°로 바늘 평형추에 닿는 높이) · 벽 높이 5.2(2.4배) · 문자판 바닥 반지름 5.6(고양이 키 2.6배) · 숫자 메달 반지름 1.0 · 바닥 숫자 높이 1.0~1.12.
- 방 안쪽 26 × 22m: x −13 ~ 13, z −11 ~ 11. 남쪽 벽은 낮은 징두리(1.15m)에 가운데 입구(폭 3.6m).

## 재질 (회색 Principled, 이름 = 역할 → 유니티 `StoryWorkshopPalette`가 색을 입힘, 그늘 = 따뜻한 갈색 _ShadeColor (0.8, 0.66, 0.56))
| 역할 | 색 | 쓰는 곳 |
|---|---|---|
| `plank` / `plank_dark` / `plank_light` / `plank_seam` | 꿀색 참나무 / 호두 / 밝은 참나무 / 짙은 이음 | 바닥 판자 청크 |
| `wallpaper` / `wallpaper_pat` | 크림 회벽 / 금빛 스텐실 | 벽면 · 다마스크 마름모 · 진열판 무늬 판 |
| `wainscot` / `molding` / `plaster_dark` | 참나무 징두리 / 꿀색 몰딩 / 짙은 호두 | 벽 판재 · 액자 몰딩 · 벽 위 덮개 |
| `wood` / `wood_dark` / `wood_light` / `walnut` / `maple` | 참나무 / 호두 / 메이플 밝은 / 짙은 호두 / 메이플 | 시계 상자 · 가구 · 문자판 상감 |
| `brass` / `brass_dark` / `gold` / `silver` / `steel` / `steel_dark` | 놋쇠 / 짙은 놋쇠 / 금 / 상아빛 은 / 따뜻한 강철 | 시계 테 · 진자 · 장식 · 공구 |
| `iron` / `iron_dark` | 짙은 청동 / 흑갈색 | 바늘 · 받침 기둥 · 바이스 |
| `dial_face` / `dial_mark` / `dial_inlay` | 따뜻한 상아 / 세피아 / 밝은 참나무 | 문자판 · 눈금 · 상감 쐐기 |
| `felt_green` | 초록 펠트 | 작업대 상판 |
| `leather` | 붉은 갈색 가죽 | 의자 방석 · 디딤대 |
| `curtain` / `curtain_dark` | 짙은 포도주 | 커튼 · 진자 칸 안쪽 |
| `shawl_red` / `shawl_red_dark` | **진한 빨강**(숄만) | 빈 의자 숄 |
| `red_lacquer` / `red_dark` / `gem_red` / `velvet_red` | 벽돌빛 · 짙은 적갈 | 바늘 일부 · 12 테 · 보석 · 오르골 안감 |
| `rug` / `rug_dark` / `sheet` / `sheet_shade` / `paper` / `porcelain` | 녹슨 붉은 깔개 / 크림 천 / 종이 · 자기 | 깔개 · 거울 덮개 · 종이 · 인형 |
| `glass` / `rain` | 밤 유리(푸른빛) / 비 | 창 |
| `lantern_glow` / `red_glow` / `moon_glow` | 빛나는 단색 | 램프 · 촛불 / 붉은 램프 / 창의 달 |

바닥 `plank` `plank_dark` `plank_light` `plank_seam` · 벽 `wallpaper` `wallpaper_pat` `wainscot` `molding` `plaster_dark` `void` ·
나무 `wood` `wood_dark` `wood_light` · 금속 `iron` `iron_dark` `steel` `steel_dark` `silver` `brass` `brass_dark` `gold`(크림 금) ·
문자판 `dial_face` `dial_mark` `dial_inlay` · 빨강 `red_lacquer` `red_dark` `gem_red` `shawl_red` `shawl_red_dark` `velvet_red` ·
천 `curtain` `curtain_dark` `sheet` `sheet_shade` `rug` `rug_dark` `leather` `paper` · 유리 `glass` `rain` `porcelain` `stone_dark` ·
빛(유니티가 빛나는 단색으로 바꿈) `lantern_glow`(따뜻함) `red_glow`(붉음) `moon_glow`(달빛).
규칙: 조각마다 역할 재질 5~8(바늘 · 숫자 · 진자 · 실린더 같은 작은 움직이는 조각만 2~4), 한 재질이 겉면 55%를 넘지 않게, 몰딩 · 상감 · 금 장식. 0.04m보다 가는 것 없음.

## 원점 규칙
원점 고정(`KEEP`) = 블렌더 좌표 그대로 내보냄 → 유니티 빌더가 Mesh 위치를 0으로 되돌림(피벗 = 원점):
`Ground` `Room`(맵 원점) · `DialRim`(문자판 가운데) · `DialTile`(메달 가운데) · 바늘 `GrandHour` `GrandMinute` `HandHour` `HandMinute` `HandHourRed` `HandMinuteRed`(축, 12시 = +Z 위) ·
진자 `GrandPendulum` `PendulumS`(매단 점, 아래로) · `Gear`(축 가운데, 면 = −Y) · `MusicCylinder`(축 가운데, 축 = X) ·
`GrandClock` `WallClock1~4` `MusicBox` `GearStand` `Window`(바닥 가운데, 기준점 표 아래) · 벽걸이 `DecoRound` `DecoSun`(뒷면 가운데) `ClockShelf`(뒷면 아래).
나머지 = 가로 가운데 · 바닥 Z 0.

## 조각 (이름 · 크기 · 삼각형 예산)
1. `Ground` (45000) — 방 바닥 판자: 보로노이 불규칙 다각형 청크(약 3.4m) 하나마다 판자 한 패턴(방향 0° 또는 90°, `plank` / `plank_dark` / `plank_light` 중 하나 + 이음 `plank_seam`), 대비 낮게. 문자판 자리(반지름 5.52) 비움. 밑판 · 바깥 `void` 판.
2. `Room` (28000) — 벽: 북 · 서 · 동 높이 5.2 · 두께 0.6, 징두리 판넬(`wainscot` · `wood_dark` · `molding`) 1.1m · 의자 높이 띠 + 금 구슬 · 다마스크 마름모(`wallpaper_pat`) · 기둥 장식(금 주두) · 처마 몰딩 + 금 치아 장식.
   북벽 창 구멍 2개(x ±6, 폭 2.2, 1.1~4.0), 괘종시계 뒤 금 후광 고리. 남벽 = 낮은 징두리 + 가운데 입구(x ±1.8) 양옆 기둥(놋쇠 공).
3. `Window` (7000) — 창틀 · 창살 · 비 줄기(`rain`) · 창에 비친 달(`moon_glow`) · 양옆 커튼(`curtain` 주름 + 금 끈 · 술) · 커튼봉 · 창턱.
4. `DialRim` (12000) — 문자판 바닥 고정부(반지름 5.6): 회색 면 · 바깥 테(분 눈금 60, 12시만 빨강) · 메달 자리 12(어두운 홈 + 금 테, 중심 반지름 4.1) · 반시 금 마름모 · 가운데 장미 무늬 + 빈 축.
5. `DialTile` (2000) — 둥근 숫자 메달 반지름 1.0(원점 = 메달 가운데): 크림 면 · 금 테 · 은 리벳. **StoryWorkshop.cs가 칸 위치만 바꾸므로(회전 없음) 부채꼴 대신 둥근 메달** — 어느 자리로 가도 모양이 맞음.
6. `Num_1` ~ `Num_12` (각 3000) — 바닥 숫자(Bookman Old Style Bold 아라비아 숫자, 높이 1.12 / 두 자리 1.0, 검정 + 크림 금 테, 12만 빨강). 남쪽에서 바로 읽힘. 메달의 자식 `Num`.
7. `GrandClock` (16000) — 괘종시계 2.34 × 1.2 × 3.74: 받침 · 진자 칸(검은 벨벳 안쪽, 앞 열림) · 후드 · 문자판 r 0.82(로마 숫자 · 분 눈금) · 기둥 · 모서리 금 소용돌이 · 백조 목 박공 · 금 항아리 꼭지 + 빨간 보석 · 빨간 칠 띠.
   기준점(유니티 로컬): 바늘 축 (0, 2.1, −0.65) · 진자 축 (0, 1.2, −0.05).
8. `GrandHour` · `GrandMinute` (각 1500) — 스페이드 · 달 고리 시침 0.5 / 분침 0.72(빨간 평형추).
9. `GrandPendulum` (2500) — 리라 진자, 추 가운데 −0.8 · 추 r 0.2(햇살 은 + 빨간 보석).
10. `WallClock1` 둥근 큰 벽시계(문자판 r 0.72 + 아래 진자 상자) · `WallClock2` 레귤레이터(r 0.5 + 진자 칸) · `WallClock3` 뻐꾸기시계(r 0.5, 지붕 · 뻐꾸기 문 · 솔방울 추) · `WallClock4` 팔각 학교 시계(r 0.6 + 진자 상자) (각 9000).
    모두 세워 둔 진열판(1.9 × 3.95, 액자 몰딩 · 무늬 판 · 금 장식)에 달림. 바늘 · 진자는 따로.
    기준점(유니티 로컬, 바늘 축 · 바늘 배율 / 진자 축 · 진자 배율): 1 = (0, 2.75, −0.37) · 1.44 / (0, 1.92, −0.26) · 0.6 · 2 = (0, 3.15, −0.5) · 1.0 / (0, 2.55, −0.27) · 1.25 ·
    3 = (0, 2.62, −0.55) · 1.0 / (0, 2.0, −0.33) · 0.85 · 4 = (0, 2.85, −0.36) · 1.2 / (0, 2.1, −0.25) · 0.5.
11. `HandHour` · `HandMinute` (+ `Red`) (각 600) — 일반 바늘(문자판 r 0.5 기준, 시침 0.3 · 분침 0.43). `PendulumS` (900) — 길이 0.8 막대 + 추 r 0.15.
12. 벽 장식 `DecoRound`(r 0.3) · `DecoSun`(햇살 r 0.5) · `ClockShelf`(벽 선반 2.4 + 탁상시계 3: 나폴레옹 모자 · 캐리지 · 유리 돔).
13. `Workbench` (9000) — 3.3 × 1.0, 윗면 1.0, 뒤 공구판(렌치 · 망치 · 드라이버 · 캘리퍼스 · 액자 사진), 서랍 4, 바이스, 돋보기 · 핀셋 · 작은 톱니 · 열린 회중시계 · 종이.
14. `MusicBox` (7000) — 그녀의 오르골 0.95 × 0.62 × 0.42, 뚜껑 열림(105°, 안쪽 은 거울 + 빨간 장미), 상감 · 금 발 · 빨간 벨벳 안감 · 빗 · 조속기 톱니 · 태엽 열쇠 · 무희 인형. 실린더 자리 (−0.17, 0.24, 0.02)(유니티 로컬).
15. `MusicCylinder` (2000) — 핀 박힌 실린더 r 0.095 × 0.44(축 X) + 끝 톱니.
16. `Gear` (5000) — 지름 2.1 톱니 18개 · 바큇살 6 · 테 금 구슬 · 가운데 숫자판 자리(크림 원판 r 0.3) · 빨간 표지 이 하나. `GearStand` (3000) — 뒤 기둥 + 베어링 + 받침판(축 높이 1.25).
17. `Chair` · `EmptyChair`(빨간 숄이 걸쳐짐, 술 장식) (각 4000), `Stool`(디딤대, 윗면 0.55), `SideTable`, `Lamp`(따뜻한 빛 유리 굴뚝, 불빛 높이 0.42), `RedLamp`(붉은 유리, 불빛 높이 0.3).
18. `Sconce` — 벽 촛대(놋쇠 판 · 금 소용돌이 · 촛불 2, 뒷면 = 벽, 원점 = 초 받침 높이). `ToolCabinet`(작은 서랍장 1.4 × 0.55 × 2.15), `PartsCrates`(상자 + 부품 더미), `CoveredMirror`(천을 덮은 거울 1.3 × 2.4, 주름 명암), `UmbrellaStand`(젖은 우산 + 물웅덩이), `Rug`(타원 3.4 × 2.3, 빨간 가는 테).

## 맵 배치 (StoryWorkshopBuilder)
문자판 바닥 중심 (0, 0, 0.5) · 메달 Tile_n = 중심에서 n시 방향 4.1m · 괘종시계 (0, 0, 10.5) · 디딤대 (0, 0, 9.1) · 노인 그림자 (−0.2, 0.625, 9.25) 시계를 봄 ·
벽시계 서쪽 진열판 x −12.1, z 7.2 / 2.6 / −2.0 / −6.6(정면이 동남동, yaw −60, 표적 Target = 진열판 앞 2m 바닥) ·
작업대 동쪽 (12.3, 0, 1.6) 정면 서쪽(오르골 · 램프는 작업대 자식) · 톱니 받침 (9.6, 0, 6.6) · (7.9, 0, −1.9) · (10.2, 0, −5.6), 톱니 = 그 위 1.25m · 창 (±6, 0, 11) · 입구 = 남쪽 가운데, 도착 (0, 0, −9.2).
노인 팔: ArmPivot = 오른 어깨(키트 로컬 (−0.205, 1.365, 0.015) + Mesh 오프셋), 피벗 y 180° · 팔 자식 y 180° → 로컬 x −70°면 앞으로 듦(손 높이 약 1.76, 바늘 축 앞 z 9.86).
미리보기: 메뉴 `CatTang/스토리 맵 4 공방 미리보기 찍기` → `Art/tools/specs/story_workshop_top.png` · `story_workshop_quarter.png`.
