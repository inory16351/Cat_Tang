# 11:59 독일어 번역 용어집 (Deutsch)

`docs/loc_glossary.md` (한국어 → 영어) 를 기준으로 한 독일어 열. 사전: `Game/Assets/Resources/Lang/de.json` (작업: `Art/tools/loc_work/de/out_NN.json`).
번역은 한국어 뜻에서, 용어는 아래 표를 끝까지 그대로 씀 (다양성보다 일관성).

## Stil (문체)

| 대상 | Deutsch |
|---|---|
| UI · 안내 | Freundliches **du**, Imperativ („Zieh die Kugel …“, „Drück Q.“). Buttons/Labels kurz, Nomen oder Infinitiv („Weiter“, „Zurück“, „Freischalten“). Keine überlangen Komposita in Labels. |
| Boss-Hinweise (…하라!) | Kurzer du-Imperativ + „!“, Struktur mit „ — “ bleibt („Stell dich auf …!“). |
| Kater (고양이) | Wortkarger Western-Revolverheld. Kurze Sätze, umgangssprachliche Verkürzungen sparsam („nich'“, „is'“, „'n“), Fluch höchstens „Verdammt“. |
| Spiegelkater | Ruhig, sanft, schlicht. |
| NPCs (Kuckuck · Schmied · Schneiderin · Sheriff · Torwächter · Händler) | Traumhaft, halb vergessen, weich und langsam. Sie duzen den Kater. |
| Traumbilder (Platzhalter für Schnitte) | Kurze Nominalphrasen im Storyboard-Ton („Faltige Hände ziehen eine Feder auf (Nahaufnahme)“). |

## Zeichen · Format

- Dialog-Auslassung: ASCII-Punkte, Anzahl wie im Original (`......` bleibt `......`). `…` nur wo das Original es hat.
- `{0}` `{1}` … alle behalten (Reihenfolge frei). Tags `<color=…>` `<size=…>` `<b>` `<i>` unverändert und gepaart. `\n` = gleiche Zeilenzahl.
- ` · ` ` — ` `×` `÷` `+` `−` und doppelte Leerzeichen genau wie im Original.
- Anführungszeichen „…“ nur, wo das Koreanische '…' setzt.
- Einheiten: `초` → `s` mit Leerzeichen hinter Zahlen in UI (`{0} s`), im Satz `Sekunden`. `m` → ` m`. `%` direkt an der Zahl (`30%`). `발` → `Schuss`, `칸` (Kugeluhr) → `Slot(s)`, `칸` (Hitze) → `Balken`.
- Dezimalzahlen: Punkt wie im Original beibehalten (`0.5`), damit fester Text und zur Laufzeit eingesetzte Zahlen gleich aussehen.
- Stunden: kurzes Label → Begriff vor `{0}`: `판정 {0}시` → `Urteilsstunde {0}`, `{0}시 칸` → `Slot {0}`; im Satz `{0}시` → `{0} Uhr`.
- Tasten: `Space` `Tab` `Esc` `E` `Q` `WASD` `M` `Enter` bleiben. `좌클릭` Linksklick · `우클릭` Rechtsklick · `휠 클릭` Mittelklick.
- Designer-Tokens `Value` `Value2` `CondValue` `Duration` bleiben wörtlich.
- „11:59“ bleibt immer „11:59“.

## Systembegriffe

| 한국어 | English | Deutsch |
|---|---|---|
| 탄환 시계 · 총알 시계 | Bullet Clock | Kugeluhr |
| 칸 (실린더 · 시계) | slot | Slot (der, Pl. Slots) · `{0}시 칸` → `Slot {0}` |
| 탄 · 총탄 | bullet | Kugel (Pl. Kugeln) · `새 총탄` → `neuen Kugeln` |
| 탄환 (기본 탄) | Plain Round | Einfache Kugel |
| 총 | gun | Waffe · rechte Hand (ungerade Slots) · linke Hand (gerade Slots) |
| 판정 시각 | Judgment Hour | Urteilsstunde |
| 발사 시각 | Firing Hour | Feuerstunde |
| 시각 조건 · 활성화 조건 · 조건 | hour condition · activation condition · condition | Stundenbedingung · Aktivierungsbedingung · Bedingung |
| 활성화 / 비활성화 | Active / Inactive | Aktiv / Inaktiv (aktivieren) · `활성화됐어요` → `Aktiviert!` |
| 연산 | Operator | Operator (Pl. Operatoren) · Operator-Slot |
| 연산 문 | Operator Door | Operator-Tür |
| 특성 | Trait | Merkmal (Pl. Merkmale) |
| 시너지 | Synergy | Synergie |
| 단계 | Tier | Stufe · `특성 6단계` → `Merkmalstufe 6` |
| 고유 효과 | Unique Effect | Sondereffekt |
| 불릿 파티 | Bullet Party | Kugelparty |
| 열기 | Heat | Hitze · `열기 칸` → Balken |
| 총 스킬 | Gun Skill | Waffenskill |
| 대시 | Dash | Dash |
| 극한 회피 · 저스트 회피 | Perfect Dodge | Perfektes Ausweichen |
| 회피 | dodge | Ausweichen · `닷지!` → `Ausgewichen!` |
| 처형 | Execute | Hinrichtung (hinrichten) |
| 치명타 · 치명 | Crit | Krit · Krit-Chance · Krit-Schaden (Kurzlabel `Krit-SCH`) |
| 공격 · 공격력 | ATK · Attack | ANG · Angriff |
| 체력 | HP | LP |
| 방어 · 방어력 | DEF · Defense | VER · Verteidigung |
| 연사 · 연사 속도 | Fire Rate | Feuerrate |
| 장전 | Reload | Nachladen |
| 사거리 | Range | Reichweite |
| 재사용 대기 | Cooldown | Abklingzeit |
| 둔화 | Slow | Verlangsamung (verlangsamt) |
| 경직 | Stagger | Taumeln |
| 기절 | Stunned | betäubt · Boss-Hinweis `기절!` → `betäubt!` |
| 관통 · 튕김 · 갈라짐 | Pierce · Ricochet · Split | Durchschlag · Abpraller · Spaltung |
| 가둠 · 시간에 가둠 | Time Lock | Zeitbann (gebannt, in der Zeit bannen) |
| 정지 · 시간 정지 | Stop · Time Stop | Stopp · Zeitstopp (gestoppt) |
| 추억 표식 · 표식 | Nostalgia Mark · Mark | Nostalgiezeichen · Zeichen |
| 공명 · 공명 사격 | Resonance · Resonance Shot | Resonanz · Resonanzschuss |
| 실루엣 · 고양이 실루엣 | Silhouette · Cat Silhouette | Silhouette · Katersilhouette |
| 원혼 | Restless Spirit | Ruheloser Geist |
| 시간의 조각 | Time Shards | Zeitsplitter (kurz: Splitter) |
| 골드 | Gold | Gold |
| 보관함 | Storage | Lager |
| 인벤토리 | Inventory | Inventar |
| 보상 · 다시 뽑기 · 새로고침 | Reward · Reroll · Refresh | Belohnung · Neu würfeln · Auffrischen |
| 하트 | Heart | Herz |
| 판 · 런 | run | Run (der, Pl. Runs) |
| 장 | Chapter | Kapitel · Kurzlabel `Kap. {0}` |
| 스테이지 · 방 · 중간 보스 · 보스 · 최종 보스 | Stage · Room · Mid-Boss · Boss · Final Boss | Etappe · Raum · Zwischenboss · Boss · Endboss |
| 문 | door | Tür |
| 시간의 문 | Time Gate | Zeittor |
| 승천 모드 | Ascension Mode | Aufstiegsmodus |
| 부활 | Revive | Wiederbelebung |
| 해금 | Unlock(ed) | Freischalten / freigeschaltet |
| 묶음 | bundle | Paket |
| 코스튬 · 의상 | Costume | Kostüm |
| 꿈 | Dream | Traum |
| 기억 | memory | Erinnerung |
| 레벨 업 | Level Up | Level-Up |
| 튜토리얼 · 안내 | Tutorial · Guide | Tutorial · Hinweis |
| 효과 발동 확률 | Effect proc chance | Effekt-Auslösechance |
| 저장 슬롯 | Memory {0} · Save {0} | Erinnerung {0} (Lobby) · Spielstand {0} (Esc) |
| 환경설정 탭 | Sound · Display · Game · Controls | Ton · Anzeige · Spiel · Steuerung |

## Merkmale (Trait.csv)

| 한국어 | English | Deutsch |
|---|---|---|
| 회상 | Recall | Rückblick |
| 추억 | Nostalgia | Nostalgie |
| 망각 | Oblivion | Vergessen |
| 되감기 | Rewind | Zurückspulen |
| 종말 | Doom | Untergang |
| 지연 | Delay | Verzögerung |
| 예지 | Foresight | Voraussicht |
| 영원 | Eternity | Ewigkeit |
| 미련 | Lingering | Nachhall |

## Orte · Einrichtungen

| 한국어 | English | Deutsch |
|---|---|---|
| 마을 · 광장 · 섬 · 다리 · 여관 | Village · Plaza · Island · Bridge · Inn | Dorf · Platz · Insel · Brücke · Gasthaus |
| 기억의 방 | Memory Room | Erinnerungsraum |
| 총포상 | Gunsmith | Waffenladen |
| 탄약 공방 | Ammo Workshop | Munitionswerkstatt |
| 의상실 | Wardrobe | Garderobe |
| 시계탑 · 광장 시계 | Clock Tower · Plaza Clock | Uhrturm · Platzuhr |
| 1장 | The Melting Western Town | Die schmelzende Westernstadt |
| 2장 | The Stopped Station | Der stillstehende Bahnhof |
| 3장 | The Mirage Desert | Die Fata-Morgana-Wüste |
| 4장 | Inside the Clockwork | Im Inneren des Uhrwerks |
| 5장 | The Crumbling End of Time | Das zerfallende Ende der Zeit |

## Figuren · NPCs

| 한국어 | English | Deutsch |
|---|---|---|
| 고양이 | Cat | Kater |
| 거울 속 고양이 | Mirror Cat | Spiegelkater |
| 태엽 풀린 뻐꾸기 | Unwound Cuckoo | Abgelaufener Kuckuck |
| 모래시계 대장장이 · 대장장이 | Hourglass Smith · Smith | Sanduhrschmied · Schmied |
| 실타래 재봉사 · 재봉사 | Skein Tailor · Tailor | Knäuelschneiderin · Schneiderin |
| 빈 외투 보안관 · 보안관 | Empty-Coat Sheriff · Sheriff | Sheriff im leeren Mantel · Sheriff |
| 괘종시계 문지기 · 문지기 | Grandfather Clock Gatekeeper · Gatekeeper | Standuhr-Torwächter · Torwächter |
| 떠돌이 상인 · 상인 | Wandering Merchant · Merchant | Fahrender Händler · Händler |
| 노인 · 시계공 · 아이 · 그녀(빨간 숄) | the old man · the clockmaker · the child · she (red shawl) | der alte Mann · der Uhrmacher · das Kind · sie (im roten Schal) |

## Bosse

| 한국어 | English | Deutsch |
|---|---|---|
| 황야의 무법자 · 와인더 잭 | Outlaw of the Wasteland · Winder Jack | Gesetzloser der Ödnis · Winder Jack |
| 불타는 시계탑 · 달리오스 | The Burning Clock Tower · Dalios | Der brennende Uhrturm · Dalios |
| 승강장의 지배자 · 컨덕터 반 | Ruler of the Platform · Conductor Van | Herrscher des Bahnsteigs · Schaffner Van |
| 탈선한 종착역 · 로코모스 | The Derailed Terminus · Locomos | Der entgleiste Endbahnhof · Locomos |
| 반사의 연출가 · 일루시오 | Director of Reflections · Illusio | Regisseur der Spiegelungen · Illusio |
| 호수의 기억 · 모비 글래스 | Memory of the Lake · Moby Glass | Erinnerung des Sees · Moby Glass |
| 시계탑의 단두대 · 기로틴 | Guillotine of the Clock Tower · Guillotin | Guillotine des Uhrturms · Guillotin |
| 시간의 수문장 · 크로노 & 템포 | Wardens of Time · Chrono & Tempo | Wächter der Zeit · Chrono & Tempo |
| 영원의 지배자 · 시간의 주인 | Ruler of Eternity · Master of Time | Herrscher der Ewigkeit · Herr der Zeit |
| 미지의 존재 | Unknown Being | Unbekanntes Wesen |

## Seltenheit (Grade.csv)

일반 Gewöhnlich · 희귀 Selten · 영웅 Episch · 전설 Legendär

## Waffen · Waffenskills · Kostüme · Erinnerungsraum

| 한국어 | Deutsch |
|---|---|
| 보안관의 리볼버 · 녹슨 콜트 · 쌍둥이 데린저 · 은빛 메아리 · 백투더 퓨처 | Revolver des Sheriffs · Rostiger Colt · Zwillingsderringer · Silberecho · Zurück in die Zukunft |
| 지평선 · 동경 · 마지막 종소리 · 가스등 랜턴 · 기관차 | Horizont · Sehnsucht · Die letzte Glocke · Gaslaterne · Lokomotive |
| 백스텝 샷 · 앞뒤 네 발 · 넘어가며 쏘기 · 반사탄 패링 · 모래 장판 | Rückschritt-Schuss · Kehrtsalve · Sprungschuss · Parierschuss · Sandfeld |
| 조준 사격 · 회상의 실루엣 · 시간 정지 · 랜턴 던지기 · 증기 돌진 | Gezielter Schuss · Silhouette des Rückblicks · Zeitstopp · Laternenwurf · Dampfsturm |
| 보안관 코트 · 무법자 판초 · 장의사 정장 · 시계공의 앞치마 | Sheriffmantel · Gesetzlosen-Poncho · Bestatteranzug · Uhrmacherschürze |
| 거울 망토 · 모래 순례자 로브 · 기억의 스카프 · 노인의 잠옷 | Spiegelumhang · Robe des Sandpilgers · Schal der Erinnerung · Pyjama des alten Mannes |
| 날카로운 눈 · 매의 눈 · 뜨거운 심장 · 날 선 기억 · 깊은 상처 | Scharfes Auge · Falkenauge · Brennendes Herz · Geschärfte Erinnerung · Tiefe Wunde |
| 단단한 몸 · 질긴 숨 · 가벼운 발 · 굳은 살 · 바람의 장화 | Fester Körper · Zäher Atem · Leichte Füße · Hornhaut · Stiefel des Windes |
| 넓어진 선택 · 한 번 더 · 추억 수집 · 빠른 손 · 행운의 동전 · 다시 뽑기 · 보스의 선물 | Mehr Auswahl · Noch einmal · Erinnerungen sammeln · Flinke Hände · Glücksmünze · Neu würfeln · Geschenk des Bosses |
| 기억의 방 가지: 화력 · 생존 · 손재주 · 기술 · {0}층 | Feuerkraft · Überleben · Geschick · Technik · Ebene {0} |

## Kugeln (Bullet.csv)

Einfache Kugel (탄환) · Teestunde · Zwillingszeiger · Sandwind · Tiefer Schatten · Verklärte Erinnerung (추억미화) · Gebrochener Schwur · Verlöschende Lampe · DEAR[ ] · Heimweh (노스탤지어, ≠ Merkmal Nostalgie) ·
Rostige Zugfeder · Getrocknete Blume · Sanduhr · Uhrwerk-Spieldose · Gefrorene Zeit · Parallelwelt · Echo des Glockenturms · Morgengrauen ·
Totale Finsternis · Verblichenes Foto · Einsamkeit · Hitzeflimmern · Abenddämmerung · Astronomische Dämmerung · Sonnenregen · Zeitschleifen-Ende · Verstrichene Jahre ·
Odins Auge · Vergesslichkeit · Nachtzug · Zeitparadoxon · Elf Uhr neunundfünfzig · Jener Sommer · Währender Sommer · Die letzte Seite ·
Kugel des Umbruchs · Wasser der Lethe · Striche der Taschenuhr · Abgetragener grauer Mantel · Kleeblatt-Lesezeichen · Mittagsduell ·
Münze auf den Gleisen · Windrad · Letzte Rakete · Stiller Puls · Schicksalhafter Münzwurf · Rostfreie Rüstung · Verspätung · Umsteigebahnhof ·
Endlose Reise · Funken auf den Schienen · Rauchring · Der letzte Zug

## Pakete (Unlock.csv)

여정 Reise · 마무리 Finale

## Sonstige Eigennamen

| 한국어 | Deutsch |
|---|---|
| 흐르는 모래 | Fließender Sand |
| 매듭 | Knoten |
| 돋보기 안경 · 이름 지워진 팔찌 · 삐뚤빼뚤한 쪽지 | Lupenbrille · Namenloses Armband · Krakeliger Zettel |
| 자정 · 12:00 | Mitternacht · 12:00 |
| 꿈 제목 '마지막 장' | Das letzte Kapitel (≠ Kugel „Die letzte Seite“) |
| 4장 '그 사람' | sie / ihr |
| 보스 패턴 연습 | Boss-Muster-Training |
