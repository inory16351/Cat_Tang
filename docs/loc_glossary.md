# 11:59 영어 번역 용어집

번역 사전 `Game/Assets/Resources/Lang/en.json` 의 고유명사 · 시스템 용어 · 문체 기준. 번역 전에 정하고 끝까지 지킴.
(추출: `python Art/tools/loc_extract.py` → `Art/tools/specs/loc_sources.json`, 검사: `--check`)

## 문체

| 대상 | 영어 문체 |
|---|---|
| UI · 안내(해요체) | 짧고 친근한 평서문. 2인칭 명령형("Drag the bullet...") 또는 평서("You can..."). 버튼 · 라벨은 원문 길이와 비슷하게, 첫 글자 대문자(Title Case는 고유명사 · 메뉴 이름만) |
| 보스 패턴 안내(…하라!) | 짧은 명령형 + 느낌표. "— " 앞뒤 구조 유지 |
| 고양이 | 무뚝뚝한 서부 총잡이. 짧은 문장, 줄임말("'s", "ain't" 정도), 욕은 "Damn"까지 |
| 거울 속 고양이 | 담담하고 다정. 낮은 목소리, 꾸밈 적게 |
| NPC(뻐꾸기 · 대장장이 · 재봉사 · 보안관 · 문지기 · 상인) | 시간에 잠식된 몽환적 존재. 반쯤 잊은 듯한 말, 부드럽고 느린 리듬 |
| 꿈 컷 설명(그림이 없을 때 자리 표시) | 짧은 명사구, 영화 콘티 말투("Wrinkled hands winding a spring (close-up)") |

## 기호 · 형식

- 대사 말줄임 `...` (온점 개수 원문 그대로: `......` → `......`). `…`(한 글자)는 원문에 있을 때만.
- 자리 표시 `{0}` `{1}` … 는 뜻 그대로 옮김(영어 어순에 맞게 자리만 바꿔도 됨). 리치 텍스트 태그 `<color=…>` `<size=…>` `<b>` `<i>` 는 그대로, 짝 유지.
- ` · ` (가운뎃점 구분) · ` — ` (줄표) · `×` `÷` `+` `−` · 앞뒤 공백 · 겹친 공백(키 안내의 두 칸)은 그대로.
- 숫자 단위: `초` → `s`(UI 숫자 뒤) / `seconds`(문장), `m` 그대로, `%` 그대로, `발` → `shots`, `칸` → `slots`, `개` → (수 뒤 명사 복수형).
- `{0}시` (시계 칸의 시각) → 짧은 라벨은 `{0}` 앞에 용어를 둠: `판정 {0}시` → `Judgment Hour {0}`, `{0}시 칸` → `Slot {0}`, 문장 속 `{0}시` → `{0} o'clock`.
- 키 이름: `좌클릭` Left Click · `우클릭` Right Click · `Space` · `Tab` · `Esc` · `E` · `Q` · `WASD` · `M` 그대로.

## 시스템 용어

| 한국어 | English | 메모 |
|---|---|---|
| 탄환 시계 · 총알 시계 | Bullet Clock | 실린더 12칸 = 시계 12시각 |
| 칸 (실린더 · 시계) | slot | `{0}시 칸` → `Slot {0}` |
| 탄 · 총탄 | bullet | 아이템. 묶음 상품 `새 총탄` → `new bullets` |
| 탄환 (기본 탄 이름) | Plain Round | Bullet.csv 1000 |
| 총 | gun | 오른손(홀수 칸) · 왼손(짝수 칸) = right hand (odd slots) · left hand (even slots) |
| 판정 시각 | Judgment Hour | 칸의 시각 + 연산 결과. `판정 {0}시` → `Judgment Hour {0}` |
| 발사 시각 | Firing Hour | 지금 쏘는 칸의 시각 |
| 시각 조건 · 활성화 조건 · 조건 | hour condition · activation condition · condition | |
| 활성화 / 비활성화 | Active / Inactive (동사: activate) | `활성화됐어요` → `Activated!` |
| 연산 | Operator | +1 · ×2 · ÷3 … 탄에 끼움. `연산 칸 · 연산 슬롯` → `Operator Slot` |
| 연산 문 | Operator Door | |
| 특성 | Trait | |
| 시너지 | Synergy | |
| 단계 (특성 · 강화) | Tier | `특성 6단계` → `Trait Tier 6` |
| 고유 효과 | Unique Effect | |
| 불릿 파티 | Bullet Party | Q 궁극기 |
| 열기 | Heat | 불릿 파티 게이지 |
| 총 스킬 | Gun Skill | 우클릭 |
| 대시 | Dash | |
| 극한 회피 · 저스트 회피 | Perfect Dodge | |
| 회피 | dodge | `닷지!` → `Dodge!` |
| 처형 | Execute | |
| 치명타 · 치명 | Crit | 치명타 확률 = Crit Chance, 치명타 피해 = Crit Damage |
| 공격 · 공격력 | ATK · Attack | 숫자 라벨은 `ATK` |
| 체력 | HP | |
| 방어 · 방어력 | DEF · Defense | |
| 연사 · 연사 속도 | Fire Rate | |
| 장전 | Reload | |
| 사거리 | Range | |
| 재사용 · 재사용 대기 | Cooldown | |
| 둔화 | Slow | |
| 경직 | Stagger | |
| 기절 | Stunned | 보스 안내 `기절!` → `stunned!` |
| 관통 · 튕김 · 갈라짐 | Pierce · Ricochet · Split | |
| 가둠 · 시간에 가둠 | Time Lock | |
| 정지 · 시간 정지 | Stop · Time Stop | |
| 추억 표식 · 표식 | Nostalgia Mark · Mark | |
| 공명 · 공명 사격 | Resonance · Resonance Shot | |
| 실루엣 · 고양이 실루엣 | Silhouette · Cat Silhouette | |
| 원혼 | Restless Spirit | |
| 시간의 조각 | Time Shards | 아웃게임 재화. `시간의 조각 +{0}` → `Time Shards +{0}` |
| 골드 | Gold | 인게임 재화 |
| 보관함 | Storage | |
| 인벤토리 | Inventory | |
| 보상 · 다시 뽑기 · 새로고침 | Reward · Reroll · Refresh | |
| 하트 | Heart | 회복 아이템 |
| 판 · 런 | run | |
| 장 | Chapter | `{0}장` → `Chapter {0}` / 짧은 라벨 `Ch. {0}` |
| 스테이지 · 방 · 중간 보스 · 보스 | Stage · Room · Mid-Boss · Boss | |
| 문 | door | 방 사이 |
| 시간의 문 | Time Gate | 마을 → 출격 |
| 승천 모드 | Ascension Mode | |
| 부활 | Revive | |
| 해금 | Unlock(ed) | |
| 묶음 | bundle | 상점 묶음 구매 |
| 코스튬 · 의상 | Costume | |
| 꿈 | Dream | |
| 기억 | memory | |
| 레벨 업 | Level Up | |
| 튜토리얼 · 안내 | Tutorial · Guide | |

## 특성 (Trait.csv)

| 한국어 | English |
|---|---|
| 회상 | Recall |
| 추억 | Nostalgia |
| 망각 | Oblivion |
| 되감기 | Rewind |
| 종말 | Doom |
| 지연 | Delay |
| 예지 | Foresight |
| 영원 | Eternity |
| 미련 | Lingering |

## 장소 · 시설

| 한국어 | English |
|---|---|
| 마을 · 광장 · 섬 · 다리 · 여관 | Village · Plaza · Island · Bridge · Inn |
| 기억의 방 | Memory Room |
| 총포상 | Gunsmith |
| 탄약 공방 | Ammo Workshop |
| 의상실 | Wardrobe |
| 시계탑 · 광장 시계 | Clock Tower · Plaza Clock |
| 1장 달리풍으로 녹아내리는 서부 마을 | The Melting Western Town |
| 2장 멈춘 기차역 | The Stopped Station |
| 3장 신기루 사막 | The Mirage Desert |
| 4장 시계 장치 속 | Inside the Clockwork |
| 5장 무너지는 시간의 끝 | The Crumbling End of Time |

## 인물 · NPC

| 한국어 | English | 말투 |
|---|---|---|
| 고양이 | Cat | 무뚝뚝한 총잡이 |
| 거울 속 고양이 | Mirror Cat | 담담 · 다정 |
| 태엽 풀린 뻐꾸기 | Unwound Cuckoo | 몽환 · 노래를 잊은 새 |
| 모래시계 대장장이 · 대장장이 | Hourglass Smith · Smith | 몽환 · 무겁고 짧게 |
| 실타래 재봉사 · 재봉사 | Skein Tailor · Tailor | 몽환 · 부드럽게 |
| 빈 외투 보안관 · 보안관 | Empty-Coat Sheriff · Sheriff | 몽환 · 옛 서부 영웅 |
| 괘종시계 문지기 · 문지기 | Grandfather Clock Gatekeeper · Gatekeeper | 몽환 · 엄숙 |
| 떠돌이 상인 | Wandering Merchant | |
| 노인 · 시계공 · 아이 · 그녀(빨간 숄) | the old man · the clockmaker · the child · she (in the red shawl) | 현실 쪽 인물 |

## 보스

| 칭호 · 이름 | English |
|---|---|
| 황야의 무법자 · 와인더 잭 | Outlaw of the Wasteland · Winder Jack |
| 불타는 시계탑 · 달리오스 | The Burning Clock Tower · Dalios |
| 승강장의 지배자 · 컨덕터 반 | Ruler of the Platform · Conductor Van |
| 탈선한 종착역 · 로코모스 | The Derailed Terminus · Locomos |
| 반사의 연출가 · 일루시오 | Director of Reflections · Illusio |
| 호수의 기억 · 모비 글래스 | Memory of the Lake · Moby Glass |
| 시계탑의 단두대 · 기로틴 | Guillotine of the Clock Tower · Guillotin |
| 시간의 수문장 · 크로노 & 템포 | Wardens of Time · Chrono & Tempo |
| 영원의 지배자 · 시간의 주인 | Ruler of Eternity · Master of Time |
| 미지의 존재 | Unknown Being |

## 등급 (Grade.csv)

일반 Common · 희귀 Rare · 영웅 Epic · 전설 Legendary

## 총 · 총 스킬 · 코스튬 · 기억의 방

| 한국어 | English |
|---|---|
| 보안관의 리볼버 · 녹슨 콜트 · 쌍둥이 데린저 · 은빛 메아리 · 백투더 퓨처 | Sheriff's Revolver · Rusty Colt · Twin Derringers · Silver Echo · Back to the Future |
| 지평선 · 동경 · 마지막 종소리 · 가스등 랜턴 · 기관차 | Horizon · Longing · The Last Bell · Gaslight Lantern · Locomotive |
| 백스텝 샷 · 앞뒤 네 발 · 넘어가며 쏘기 · 반사탄 패링 · 모래 장판 | Backstep Shot · Four-Way Turn · Vault Shot · Parry Shot · Sand Field |
| 조준 사격 · 회상의 실루엣 · 시간 정지 · 랜턴 던지기 · 증기 돌진 | Aimed Shot · Silhouette of Recall · Time Stop · Lantern Toss · Steam Rush |
| 보안관 코트 · 무법자 판초 · 장의사 정장 · 시계공의 앞치마 | Sheriff's Coat · Outlaw Poncho · Undertaker's Suit · Clockmaker's Apron |
| 거울 망토 · 모래 순례자 로브 · 기억의 스카프 · 노인의 잠옷 | Mirror Cloak · Sand Pilgrim's Robe · Scarf of Memory · Old Man's Pajamas |
| 기억의 방: 날카로운 눈 · 매의 눈 · 뜨거운 심장 · 날 선 기억 · 깊은 상처 | Sharp Eye · Hawk Eye · Burning Heart · Honed Memory · Deep Wound |
| 단단한 몸 · 질긴 숨 · 가벼운 발 · 굳은 살 · 바람의 장화 | Sturdy Body · Tenacious Breath · Light Feet · Callused Skin · Boots of the Wind |
| 넓어진 선택 · 한 번 더 · 추억 수집 · 빠른 손 · 행운의 동전 · 다시 뽑기 · 보스의 선물 | Wider Choices · One More Time · Collecting Memories · Quick Hands · Lucky Coin · Reroll · Boss's Gift |

## 탄 (Bullet.csv) 이름

탄환 Plain Round · 티 타임 Tea Time · 쌍둥이 초침 Twin Second Hands · 모래바람 Sandwind · 짙어진 그림자 Deepened Shadow ·
추억미화 Rose-Tinted Memory · 부서진 맹세 Broken Vow · 꺼져 가는 등불 Fading Lamp · DEAR[ ] DEAR[ ] · 노스탤지어 Nostalgia ·
녹슨 태엽 Rusty Mainspring · 드라이드 플라워 Dried Flower · 모래시계 Hourglass · 시계태엽 오르골 Clockwork Music Box ·
얼어붙은 시간 Frozen Time · 평행세계 Parallel World · 종탑의 메아리 Echo of the Bell Tower · 새벽여명 Daybreak ·
개기일식 Total Eclipse · 빛바랜 사진 Faded Photograph · 고독 Solitude · 아지랑이 Heat Haze · 황혼 Twilight ·
천문박명 Astronomical Twilight · 여우비 Sunshower · 타임 루프 엔딩 Time Loop Ending · 세월 Passing Years ·
오딘의 눈동자 Eye of Odin · 건망증 Forgetfulness · 야행열차 Night Train · 타임 패러독스 Time Paradox ·
열한 시 오십구 분 Eleven Fifty-Nine · 그해 여름 That Summer · 여름의 지속 Lingering Summer · 마지막 페이지 The Last Page ·
변혁의 총탄 Bullet of Upheaval · 레테의 물결 Waters of Lethe · 회중시계의 눈금 Pocket Watch Tick Marks ·
낡은 회색 코트 Worn Gray Coat · 네잎클로버 책갈피 Four-Leaf Clover Bookmark · 정오의 결투 High Noon Duel ·
철길 위의 동전 Coin on the Rails · 바람개비 Pinwheel · 불꽃놀이 마지막 발 Final Firework · 멈춘 맥박 Stopped Pulse ·
운명의 동전 던지기 Fateful Coin Toss · 녹슬지 않는 갑주 Rustless Armor · 연착 Delayed Arrival · 환승역 Transfer Station ·
끝나지 않는 여정 Endless Journey · 레일 위의 불꽃 Sparks on the Rails · 연기 고리 Smoke Ring · 마지막 기차 The Last Train

## 상점 묶음 (Unlock.csv)

여정 Journey · 마무리 Finale

## 기타 고유 표현

| 한국어 | English |
|---|---|
| 흐르는 모래 | Flowing Sand |
| 매듭 | Knot |
| 돋보기 안경 · 이름 지워진 팔찌 · 삐뚤빼뚤한 쪽지 | Magnifying Spectacles · Nameless Bracelet · Crooked Note |
| 11:59 | 11:59 (그대로) |
| 자정 · 12:00 | Midnight · 12:00 |

## 용어 통일 (2026-11-26)

| 한국어 | English | 메모 |
|---|---|---|
| 열기 칸 (불릿 파티 게이지) | bar(s) | `열기 +0.5칸` → `Heat +0.5 bars` (시계 칸 slot과 구분) |
| 효과 발동 확률 | Effect proc chance | 라벨은 `Effect Proc Chance` |
| 저장 슬롯 (로비 '기억 {0}' · Esc 창 '슬롯 {0}') | Memory {0} · Save {0} | 원문 단어가 달라 그대로 둠 |
| 꿈 제목 '마지막 장' | The Final Chapter | 탄 '마지막 페이지' = The Last Page 와 구분 |
| 4장 '그 사람' | she / her | 노인이 떠나보낸 그녀 (4장 기획 확정) |
| 환경설정 탭 | Sound · Display · Game · Controls | |
