# 11:59 — glossaire français (fr)

Complète `docs/loc_glossary.md` (coréen → anglais). Le coréen reste la source ; l'anglais sert de référence pour l'intention et la terminologie. Dictionnaire : `Game/Assets/Resources/Lang/fr.json` (outil : `python Art/tools/loc_lang.py merge|check fr`).

## Style

| Cible | Choix français |
|---|---|
| UI · tutoriel | Clair et amical, **vous**. Consignes à l'impératif vous (« Faites glisser la balle… »). Boutons/onglets = infinitif ou nom court (« Recommencer », « Quitter », « Réserve »). Majuscule seulement au premier mot (sauf noms propres). |
| Indices de boss (…하라 !) | Impératif court + « ! ». Garder la structure « X — Y ». |
| Chat | Pistolero laconique de l'Ouest. Phrases courtes, tutoie tout le monde. Familier léger (« Bon sang », « j'sais pas » évité ; tournures orales : « C'est pas moi. »). |
| Chat du miroir | Calme, doux, voix basse. Tutoie le Chat. |
| PNJ (Coucou, Forgeron, Couturière, Shérif, Gardien, Marchand) | Êtres rêveurs rongés par le temps : phrases lentes, à moitié oubliées. Tutoient le Chat. Le Shérif garde un ton de vieux héros de western. |
| Plans de rêve (descriptions sans image) | Groupes nominaux courts, ton de découpage (« Mains ridées remontant un ressort (gros plan) »). |

## Typographie · formats

- Espace normale avant `! ? : ;` (pas d'espace insécable requise). Guillemets « » seulement si le coréen a des guillemets.
- Points de suspension : `...` ASCII, même nombre de points que la source. `…` seulement s'il est dans la source.
- `{0}` `{1}` conservés (ordre libre). Balises `<color>` `<size>` `<b>` `<i>` intactes et appariées. Même nombre de `\n`.
- ` · ` ` — ` `×` `÷` `+` `−` et doubles espaces conservés à l'identique. Jetons `Value` `Value2` `CondValue` `Duration` gardés tels quels.
- Unités : `초` → `s` après un nombre en UI (`{0} s`) / « secondes » en phrase ; `발` → « tirs » ; `칸` → « cases » ; `%` collé au nombre comme la source.
- Heures : `{0}시` en phrase → `{0} h` ; étiquette `판정 {0}시` → `Heure de jugement {0}` ; `{0}시 칸` → `Case {0}`.
- Touches : Space · Tab · Esc · E · Q · WASD · M inchangées. `좌클릭` Clic gauche · `우클릭` Clic droit · `휠 클릭` Clic molette.
- Le titre « 11:59 » ne change jamais.

## Termes système

| Coréen | English | Français |
|---|---|---|
| 탄환 시계 · 총알 시계 | Bullet Clock | Horloge-barillet |
| 칸 (실린더 · 시계) | slot | case |
| 탄 · 총탄 | bullet | balle (`새 총탄` → nouvelles balles) |
| 탄환 (balle de base) | Plain Round | Balle ordinaire |
| 총 | gun | arme (main droite = cases impaires · main gauche = cases paires) |
| 판정 시각 | Judgment Hour | Heure de jugement |
| 발사 시각 | Firing Hour | Heure de tir |
| 시각 조건 · 활성화 조건 · 조건 | hour condition · activation condition · condition | condition d'heure · condition d'activation · condition |
| 활성화 / 비활성화 | Active / Inactive | Actif / Inactif (`활성화됐어요` → Activé !) |
| 연산 | Operator | Opérateur (`연산 칸` → Case d'opérateur) |
| 연산 문 | Operator Door | Porte d'opérateur |
| 특성 | Trait | Trait |
| 시너지 | Synergy | Synergie |
| 단계 | Tier | Palier (`특성 6단계` → trait au palier 6) |
| 고유 효과 | Unique Effect | Effet unique |
| 불릿 파티 | Bullet Party | Bullet Party (nom propre de l'ultime, gardé) |
| 열기 | Heat | Chaleur |
| 열기 칸 | bar(s) | barre(s) (`열기 +0.5칸` → Chaleur +0.5 barre) |
| 총 스킬 | Gun Skill | Compétence d'arme |
| 대시 | Dash | Ruée |
| 극한 회피 · 저스트 회피 | Perfect Dodge | Esquive parfaite |
| 회피 | dodge | esquive (`닷지!` → Esquive !) |
| 처형 | Execute | Exécution |
| 치명타 | Crit | Critique (Chances de critique · Dégâts critiques) |
| 공격 · 공격력 | ATK · Attack | ATQ · Attaque |
| 체력 | HP | PV |
| 방어 · 방어력 | DEF · Defense | DÉF · Défense |
| 연사 속도 | Fire Rate | Cadence de tir |
| 장전 | Reload | Rechargement |
| 사거리 | Range | Portée |
| 재사용 대기 | Cooldown | Temps de recharge |
| 둔화 | Slow | Ralentissement |
| 경직 | Stagger | Déséquilibre |
| 기절 | Stunned | Étourdi (`기절!` → étourdi !) |
| 관통 · 튕김 · 갈라짐 | Pierce · Ricochet · Split | Perforation · Ricochet · Fragmentation |
| 가둠 · 시간에 가둠 | Time Lock | Verrou · Verrou temporel |
| 정지 · 시간 정지 | Stop · Time Stop | Arrêt · Arrêt du temps |
| 추억 표식 · 표식 | Nostalgia Mark · Mark | Marque de nostalgie · Marque |
| 공명 · 공명 사격 | Resonance · Resonance Shot | Résonance · Tir de résonance |
| 실루엣 · 고양이 실루엣 | Silhouette · Cat Silhouette | Silhouette · Silhouette du Chat |
| 원혼 | Restless Spirit | Âme errante |
| 시간의 조각 | Time Shards | Éclats de temps |
| 골드 | Gold | Or |
| 보관함 | Storage | Réserve |
| 인벤토리 | Inventory | Inventaire |
| 보상 · 다시 뽑기 · 새로고침 | Reward · Reroll · Refresh | Récompense · Relancer · Actualiser |
| 하트 | Heart | Cœur |
| 판 · 런 | run | partie |
| 장 | Chapter | Chapitre (`{0}장` → Chapitre {0} / court : Ch. {0}) |
| 스테이지 · 방 · 중간 보스 · 보스 | Stage · Room · Mid-Boss · Boss | Étape · Salle · Mini-boss · Boss |
| 문 | door | porte |
| 시간의 문 | Time Gate | Porte du temps |
| 승천 모드 | Ascension Mode | Mode Ascension |
| 부활 | Revive | Résurrection |
| 해금 | Unlock(ed) | Débloquer · débloqué |
| 묶음 | bundle | lot |
| 코스튬 · 의상 | Costume | Costume |
| 꿈 | Dream | Rêve |
| 기억 | memory | souvenir |
| 레벨 업 | Level Up | Niveau supérieur |
| 튜토리얼 · 안내 | Tutorial · Guide | Tutoriel · Guide |
| 효과 발동 확률 | Effect proc chance | Chance de déclenchement |
| 저장 슬롯 (기억 {0} · 슬롯 {0}) | Memory {0} · Save {0} | Souvenir {0} · Sauvegarde {0} |
| 환경설정 탭 | Sound · Display · Game · Controls | Son · Affichage · Jeu · Commandes |

## Traits (Trait.csv)

| Coréen | English | Français |
|---|---|---|
| 회상 | Recall | Réminiscence |
| 추억 | Nostalgia | Nostalgie |
| 망각 | Oblivion | Oubli |
| 되감기 | Rewind | Rembobinage |
| 종말 | Doom | Apocalypse |
| 지연 | Delay | Retard |
| 예지 | Foresight | Prescience |
| 영원 | Eternity | Éternité |
| 미련 | Lingering | Regret |

## Lieux · installations

| Coréen | English | Français |
|---|---|---|
| 마을 · 광장 · 섬 · 다리 · 여관 | Village · Plaza · Island · Bridge · Inn | Village · Place · Île · Pont · Auberge |
| 기억의 방 | Memory Room | Salle des souvenirs |
| 총포상 | Gunsmith | Armurerie |
| 탄약 공방 | Ammo Workshop | Atelier de munitions |
| 의상실 | Wardrobe | Garde-robe |
| 시계탑 · 광장 시계 | Clock Tower · Plaza Clock | Tour de l'horloge · Horloge de la place |
| 1장 | The Melting Western Town | La Ville de l'Ouest qui fond |
| 2장 | The Stopped Station | La Gare figée |
| 3장 | The Mirage Desert | Le Désert des mirages |
| 4장 | Inside the Clockwork | Au cœur du mécanisme |
| 5장 | The Crumbling End of Time | La Fin du temps qui s'effondre |

## Personnages · PNJ

| Coréen | English | Français |
|---|---|---|
| 고양이 | Cat | Chat (« le Chat ») |
| 거울 속 고양이 | Mirror Cat | Chat du miroir |
| 태엽 풀린 뻐꾸기 | Unwound Cuckoo | Coucou détraqué |
| 모래시계 대장장이 · 대장장이 | Hourglass Smith · Smith | Forgeron du sablier · Forgeron |
| 실타래 재봉사 · 재봉사 | Skein Tailor · Tailor | Couturière à l'écheveau · Couturière (machine à coudre de la femme de l'horloger → féminin) |
| 빈 외투 보안관 · 보안관 | Empty-Coat Sheriff · Sheriff | Shérif au manteau vide · Shérif |
| 괘종시계 문지기 · 문지기 | Grandfather Clock Gatekeeper · Gatekeeper | Gardien de l'horloge comtoise · Gardien |
| 떠돌이 상인 | Wandering Merchant | Marchand errant |
| 노인 · 시계공 · 아이 · 그녀(빨간 숄) | the old man · the clockmaker · the child · she (red shawl) | le vieil homme · l'horloger · l'enfant · elle (au châle rouge) |

## Boss

| Coréen | English | Français |
|---|---|---|
| 황야의 무법자 · 와인더 잭 | Outlaw of the Wasteland · Winder Jack | Le Hors-la-loi des terres sauvages · Winder Jack |
| 불타는 시계탑 · 달리오스 | The Burning Clock Tower · Dalios | La Tour de l'horloge en flammes · Dalios |
| 승강장의 지배자 · 컨덕터 반 | Ruler of the Platform · Conductor Van | Le Souverain du quai · Conductor Van |
| 탈선한 종착역 · 로코모스 | The Derailed Terminus · Locomos | Le Terminus déraillé · Locomos |
| 반사의 연출가 · 일루시오 | Director of Reflections · Illusio | Le Metteur en scène des reflets · Illusio |
| 호수의 기억 · 모비 글래스 | Memory of the Lake · Moby Glass | Le Souvenir du lac · Moby Glass |
| 시계탑의 단두대 · 기로틴 | Guillotine of the Clock Tower · Guillotin | La Guillotine de la tour de l'horloge · Guillotin |
| 시간의 수문장 · 크로노 & 템포 | Wardens of Time · Chrono & Tempo | Les Sentinelles du temps · Chrono & Tempo |
| 영원의 지배자 · 시간의 주인 | Ruler of Eternity · Master of Time | Le Souverain de l'éternité · Le Maître du temps |
| 미지의 존재 | Unknown Being | Être inconnu |

`지배자` = Souverain partout ; `주인` = Maître ; `수문장` = Sentinelles (pour ne pas confondre avec le Gardien, PNJ).

## Raretés (Grade.csv)

일반 Commun · 희귀 Rare · 영웅 Épique · 전설 Légendaire

## Armes · compétences · costumes · Salle des souvenirs

| English | Français |
|---|---|
| Sheriff's Revolver · Rusty Colt · Twin Derringers · Silver Echo · Back to the Future | Revolver du shérif · Colt rouillé · Derringers jumeaux · Écho d'argent · Retour vers le futur |
| Horizon · Longing · The Last Bell · Gaslight Lantern · Locomotive | Horizon · Langueur · La Dernière Cloche · Lanterne à gaz · Locomotive |
| Backstep Shot · Four-Way Turn · Vault Shot · Parry Shot · Sand Field | Tir en recul · Tir aux quatre vents · Tir en saut · Parade-riposte · Champ de sable |
| Aimed Shot · Silhouette of Recall · Time Stop · Lantern Toss · Steam Rush | Tir visé · Silhouette de réminiscence · Arrêt du temps · Lancer de lanterne · Charge de vapeur |
| Sheriff's Coat · Outlaw Poncho · Undertaker's Suit · Clockmaker's Apron | Manteau de shérif · Poncho de hors-la-loi · Costume de croque-mort · Tablier d'horloger |
| Mirror Cloak · Sand Pilgrim's Robe · Scarf of Memory · Old Man's Pajamas | Cape miroir · Robe de pèlerin des sables · Écharpe du souvenir · Pyjama du vieil homme |
| Sharp Eye · Hawk Eye · Burning Heart · Honed Memory · Deep Wound | Œil vif · Œil de faucon · Cœur ardent · Souvenir aiguisé · Blessure profonde |
| Sturdy Body · Tenacious Breath · Light Feet · Callused Skin · Boots of the Wind | Corps robuste · Souffle tenace · Pieds légers · Peau calleuse · Bottes du vent |
| Wider Choices · One More Time · Collecting Memories · Quick Hands · Lucky Coin · Reroll · Boss's Gift | Choix élargis · Encore une fois · Collection de souvenirs · Mains rapides · Pièce porte-bonheur · Relance · Cadeau du boss |

## Balles (Bullet.csv)

Balle ordinaire · L'Heure du thé · Trotteuses jumelles · Vent de sable · Ombre épaissie · Souvenir embelli · Serment brisé · Lampe qui s'éteint · DEAR[ ] · Nostalgia ·
Ressort rouillé · Fleur séchée · Sablier · Boîte à musique mécanique · Temps figé · Monde parallèle · Écho du clocher · Point du jour ·
Éclipse totale · Photo jaunie · Solitude · Brume de chaleur · Crépuscule · Crépuscule astronomique · Averse ensoleillée · Fin en boucle temporelle · Les Années qui passent ·
Œil d'Odin · Trou de mémoire · Train de nuit · Paradoxe temporel · Onze heures cinquante-neuf · Cet été-là · L'Été qui s'attarde · La Dernière Page ·
Balle du bouleversement · Flots du Léthé · Graduations de la montre à gousset · Vieux manteau gris · Marque-page au trèfle à quatre feuilles · Duel à midi ·
Pièce sur les rails · Moulinet · Dernier feu d'artifice · Pouls arrêté · Pile ou face du destin · Armure sans rouille · Arrivée retardée · Gare de correspondance ·
Voyage sans fin · Étincelles sur les rails · Rond de fumée · Le Dernier Train

(ordre identique à la liste anglaise de `loc_glossary.md`)

## Lots (Unlock.csv)

Voyage · Dénouement

## Autres expressions

| Coréen | English | Français |
|---|---|---|
| 흐르는 모래 | Flowing Sand | Sable qui s'écoule |
| 매듭 | Knot | Nœud |
| 돋보기 안경 · 이름 지워진 팔찌 · 삐뚤빼뚤한 쪽지 | Magnifying Spectacles · Nameless Bracelet · Crooked Note | Lunettes-loupes · Bracelet sans nom · Mot griffonné |
| 11:59 | 11:59 | 11:59 |
| 자정 · 12:00 | Midnight · 12:00 | Minuit · 12:00 |
| 꿈 제목 '마지막 장' | The Final Chapter | Le Dernier Chapitre |
| 4장 '그 사람' | she / her | elle |
