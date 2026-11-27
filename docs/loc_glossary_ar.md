# 11:59 아랍어(ar) 번역 용어집

기준: `docs/loc_glossary.md` (한국어 → 영어). 이 문서는 아랍어 열과 아랍어 문체 결정. 사전 = `Game/Assets/Resources/Lang/ar.json`.
현대 표준 아랍어(MSA), PC 게임 현지화 관용 표현 기준. 일관성 > 다양성.

## 문체 · 형식 결정

| 항목 | 결정 |
|---|---|
| UI · 안내 | 2인칭 남성 단수 명령형 ("اسحب الطلقة..."), 중립적이고 명확하게. 라벨은 명사(마스다르) 위주로 짧게: "إعادة التعبئة" |
| 보스 패턴 안내 | 짧은 명령형 + "!" · ` — ` 앞뒤 구조 유지 ("قف داخل المنطقة الآمنة!") |
| 고양이 | 짧고 건조한 문장, 남성. 욕은 "تبا" 까지 |
| 거울 속 고양이 | 담담 · 다정, 남성. 짧고 낮은 말투 |
| NPC | 몽환적, 반쯤 잊은 듯, 부드럽고 느린 리듬. 말줄임 `...` 많이 유지 |
| 성별 | 플레이어 · 고양이 · 거울 고양이 · 대장장이 · 보안관 · 뻐꾸기 · 문지기 · 상인 = 남성. 재봉사 = 여성(시계공 아내의 재봉틀). "그녀(빨간 숄)" = 여성 |
| 숫자 | 서양 숫자 0-9. `{0}` 앞에 명사를 두는 라벨형("شظايا الزمن +{0}", "المرات: {0}")으로 수량에 따른 명사 변화 회피. 불가피하면 `{0} مرة` · `{0} خانة` 처럼 단수형 고정(UI 관용) |
| 단위 | `초` → `ث` (UI 숫자 뒤, "{0}ث") / "ثانية" (문장). `m` → "م". `%` 그대로. `발` → "طلقة". `칸` → "خانة" |
| 시각 | `{0}시`(문장) → "الساعة {0}" · `판정 {0}시` → "ساعة الحكم {0}" · `{0}시 칸` → "الخانة {0}" · `발사 시각 {0}시` → "ساعة الإطلاق {0}" |
| 기호 | `...` 원문 개수 그대로(ASCII 온점). ` · ` ` — ` `×` `÷` `+` `−` 그대로. 아랍어 쉼표 `،` · 물음표 `؟` 사용. 앞뒤 공백 · 겹친 공백 원문 그대로 |
| 금지 | 양방향 제어 문자(RLM/LRM 등) · 표시형 글자 · 타트윌(ـ) · 하라카트(타슈킬). 탄윈 없는 형태로("مواز", "ثان") |
| 키 | Space · Tab · Esc · E · Q · WASD · M 라틴 그대로. 좌클릭 = النقر الأيسر · 우클릭 = النقر الأيمن · 휠 클릭 = النقر الأوسط |
| 디자이너 토큰 | `Value` `Value2` `CondValue` `Duration` 라틴 그대로 |
| 게임 이름 | 11:59 그대로 |

## 시스템 용어

| 한국어 | English | العربية |
|---|---|---|
| 탄환 시계 · 총알 시계 | Bullet Clock | ساعة الطلقات |
| 칸 (실린더 · 시계) | slot | خانة · `{0}시 칸` → الخانة {0} |
| 실린더 | cylinder | الأسطوانة |
| 탄 · 총탄 | bullet | طلقة / طلقات · `새 총탄` → طلقات جديدة |
| 탄환 (기본 탄) | Plain Round | طلقة عادية |
| 총 | gun | مسدس · 오른손(홀수 칸) اليد اليمنى (الخانات الفردية) · 왼손(짝수 칸) اليد اليسرى (الخانات الزوجية) |
| 판정 시각 | Judgment Hour | ساعة الحكم |
| 발사 시각 | Firing Hour | ساعة الإطلاق |
| 시각 조건 · 활성화 조건 · 조건 | hour condition · activation condition · condition | شرط الساعة · شرط التفعيل · شرط |
| 활성화 / 비활성화 | Active / Inactive | مفعل / غير مفعل · `활성화됐어요` → تم التفعيل! |
| 연산 | Operator | عملية · `연산 칸 · 연산 슬롯` → خانة العملية |
| 연산 문 | Operator Door | باب العمليات |
| 특성 | Trait | سمة |
| 시너지 | Synergy | تآزر |
| 단계 | Tier | مرتبة · `특성 6단계` → المرتبة 6 للسمة |
| 고유 효과 | Unique Effect | تأثير فريد |
| 불릿 파티 | Bullet Party | حفلة الرصاص |
| 열기 | Heat | الحرارة |
| 열기 칸 | bar(s) | شريط · `열기 +0.5칸` → الحرارة +0.5 شريط |
| 총 스킬 | Gun Skill | مهارة المسدس |
| 대시 | Dash | اندفاع |
| 극한 회피 · 저스트 회피 | Perfect Dodge | مراوغة مثالية |
| 회피 | dodge | مراوغة · `닷지!` → مراوغة! |
| 처형 | Execute | إجهاز |
| 치명타 | Crit | ضربة حرجة · فرصة الضربة الحرجة · ضرر الضربة الحرجة |
| 공격력 | ATK | الهجوم |
| 체력 | HP | الصحة |
| 방어력 | DEF | الدفاع |
| 연사 속도 | Fire Rate | معدل الإطلاق |
| 장전 | Reload | إعادة التعبئة |
| 사거리 | Range | المدى |
| 재사용 대기 | Cooldown | فترة التهدئة |
| 둔화 | Slow | إبطاء |
| 경직 | Stagger | ترنح |
| 기절 | Stunned | مذهول · `기절!` → مذهول! |
| 관통 · 튕김 · 갈라짐 | Pierce · Ricochet · Split | اختراق · ارتداد · انشطار |
| 가둠 · 시간에 가둠 | Time Lock | حبس الزمن |
| 정지 · 시간 정지 | Stop · Time Stop | إيقاف · إيقاف الزمن |
| 추억 표식 · 표식 | Nostalgia Mark · Mark | علامة الحنين · علامة |
| 공명 · 공명 사격 | Resonance · Resonance Shot | رنين · طلقة الرنين |
| 실루엣 · 고양이 실루엣 | Silhouette · Cat Silhouette | طيف · طيف القط |
| 원혼 | Restless Spirit | روح هائمة |
| 시간의 조각 | Time Shards | شظايا الزمن |
| 골드 | Gold | ذهب |
| 보관함 | Storage | المستودع |
| 인벤토리 | Inventory | الحقيبة |
| 보상 · 다시 뽑기 · 새로고침 | Reward · Reroll · Refresh | مكافأة · إعادة السحب · تحديث |
| 하트 | Heart | قلب |
| 판 · 런 | run | جولة |
| 장 | Chapter | الفصل · `{0}장` → الفصل {0} |
| 스테이지 · 방 · 중간 보스 · 보스 | Stage · Room · Mid-Boss · Boss | مرحلة · غرفة · زعيم فرعي · زعيم |
| 문 | door | باب |
| 시간의 문 | Time Gate | بوابة الزمن |
| 승천 모드 | Ascension Mode | وضع الارتقاء |
| 부활 | Revive | إحياء |
| 해금 | Unlock(ed) | فتح · `{0} 해금` → تم فتح {0} |
| 묶음 | bundle | حزمة |
| 코스튬 · 의상 | Costume | زي |
| 꿈 | Dream | حلم |
| 기억 | memory | ذكرى |
| 레벨 업 | Level Up | رفع المستوى |
| 튜토리얼 · 안내 | Tutorial · Guide | الدرس التعليمي · إرشاد |
| 효과 발동 확률 | Effect Proc Chance | فرصة تفعيل التأثير |
| 저장 슬롯 | Memory {0} · Save {0} | الذكرى {0} · الحفظ {0} |
| 환경설정 탭 | Sound · Display · Game · Controls | الصوت · العرض · اللعبة · التحكم |

## 특성 (Trait.csv)

| 한국어 | English | العربية |
|---|---|---|
| 회상 | Recall | استذكار |
| 추억 | Nostalgia | حنين |
| 망각 | Oblivion | نسيان |
| 되감기 | Rewind | ترجيع |
| 종말 | Doom | هلاك |
| 지연 | Delay | تأخير |
| 예지 | Foresight | بصيرة |
| 영원 | Eternity | خلود |
| 미련 | Lingering | تعلق |

## 등급

일반 Common عادي · 희귀 Rare نادر · 영웅 Epic ملحمي · 전설 Legendary أسطوري

## 장소 · 시설

| 한국어 | English | العربية |
|---|---|---|
| 마을 · 광장 · 섬 · 다리 · 여관 | Village · Plaza · Island · Bridge · Inn | القرية · الساحة · الجزيرة · الجسر · النزل |
| 기억의 방 | Memory Room | غرفة الذكريات |
| 총포상 | Gunsmith | محل الأسلحة |
| 탄약 공방 | Ammo Workshop | ورشة الذخيرة |
| 의상실 | Wardrobe | غرفة الأزياء |
| 시계탑 · 광장 시계 | Clock Tower · Plaza Clock | برج الساعة · ساعة الساحة |
| 1장 | The Melting Western Town | البلدة الغربية الذائبة |
| 2장 | The Stopped Station | المحطة المتوقفة |
| 3장 | The Mirage Desert | صحراء السراب |
| 4장 | Inside the Clockwork | داخل آلية الساعة |
| 5장 | The Crumbling End of Time | نهاية الزمن المنهارة |

## 인물 · NPC

| 한국어 | English | العربية |
|---|---|---|
| 고양이 | Cat | القط |
| 거울 속 고양이 | Mirror Cat | قط المرآة |
| 태엽 풀린 뻐꾸기 | Unwound Cuckoo | وقواق بلا زنبرك |
| 모래시계 대장장이 · 대장장이 | Hourglass Smith · Smith | حداد الساعة الرملية · الحداد |
| 실타래 재봉사 · 재봉사 | Skein Tailor · Tailor | خياطة البكرات · الخياطة (여성) |
| 빈 외투 보안관 · 보안관 | Empty-Coat Sheriff · Sheriff | شريف المعطف الفارغ · الشريف |
| 괘종시계 문지기 · 문지기 | Grandfather Clock Gatekeeper · Gatekeeper | حارس ساعة الجد · الحارس |
| 떠돌이 상인 | Wandering Merchant | التاجر المتجول |
| 노인 · 시계공 · 아이 · 그녀(빨간 숄) | the old man · the clockmaker · the child · she (red shawl) | العجوز · صانع الساعات · الطفل · هي (ذات الشال الأحمر) |

## 보스

| 칭호 · 이름 | English | العربية |
|---|---|---|
| 황야의 무법자 · 와인더 잭 | Outlaw of the Wasteland · Winder Jack | الخارج عن القانون في البرية · وايندر جاك |
| 불타는 시계탑 · 달리오스 | The Burning Clock Tower · Dalios | برج الساعة المشتعل · داليوس |
| 승강장의 지배자 · 컨덕터 반 | Ruler of the Platform · Conductor Van | سيد الرصيف · كونداكتور فان |
| 탈선한 종착역 · 로코모스 | The Derailed Terminus · Locomos | المحطة الأخيرة الخارجة عن القضبان · لوكوموس |
| 반사의 연출가 · 일루시오 | Director of Reflections · Illusio | مخرج الانعكاسات · إيلوسيو |
| 호수의 기억 · 모비 글래스 | Memory of the Lake · Moby Glass | ذكرى البحيرة · موبي غلاس |
| 시계탑의 단두대 · 기로틴 | Guillotine of the Clock Tower · Guillotin | مقصلة برج الساعة · غيلوتان |
| 시간의 수문장 · 크로노 & 템포 | Wardens of Time · Chrono & Tempo | حراس الزمن · كرونو وتيمبو |
| 영원의 지배자 · 시간의 주인 | Ruler of Eternity · Master of Time | حاكم الخلود · سيد الزمن |
| 미지의 존재 | Unknown Being | الكيان المجهول |

## 총 · 총 스킬 · 코스튬 · 기억의 방

| English | العربية |
|---|---|
| Sheriff's Revolver · Rusty Colt · Twin Derringers · Silver Echo · Back to the Future | مسدس الشريف · كولت الصدئ · ديرينجر التوأم · الصدى الفضي · العودة إلى المستقبل |
| Horizon · Longing · The Last Bell · Gaslight Lantern · Locomotive | الأفق · الشوق · الجرس الأخير · فانوس الغاز · القاطرة |
| Backstep Shot · Four-Way Turn · Vault Shot · Parry Shot · Sand Field | طلقة التراجع · الطلقات الأربع · طلقة القفز · طلقة الصد · بساط الرمل |
| Aimed Shot · Silhouette of Recall · Time Stop · Lantern Toss · Steam Rush | الطلقة المصوبة · طيف الاستذكار · إيقاف الزمن · رمي الفانوس · اندفاع البخار |
| Sheriff's Coat · Outlaw Poncho · Undertaker's Suit · Clockmaker's Apron | معطف الشريف · بونشو الخارج عن القانون · بدلة الحانوتي · مريلة صانع الساعات |
| Mirror Cloak · Sand Pilgrim's Robe · Scarf of Memory · Old Man's Pajamas | عباءة المرآة · رداء حاج الرمال · وشاح الذكرى · بيجامة العجوز |
| Sharp Eye · Hawk Eye · Burning Heart · Honed Memory · Deep Wound | العين الحادة · عين الصقر · القلب المتقد · الذكرى المشحوذة · الجرح العميق |
| Sturdy Body · Tenacious Breath · Light Feet · Callused Skin · Boots of the Wind | الجسد الصلب · النفس الطويل · الخطى الخفيفة · الجلد المتصلب · حذاء الريح |
| Wider Choices · One More Time · Collecting Memories · Quick Hands · Lucky Coin · Reroll · Boss's Gift | خيارات أوسع · مرة أخرى · جمع الذكريات · الأيدي السريعة · عملة الحظ · إعادة السحب · هدية الزعيم |

## 탄 (Bullet.csv) 이름

| English | العربية |
|---|---|
| Plain Round · Tea Time · Twin Second Hands · Sandwind · Deepened Shadow | طلقة عادية · وقت الشاي · عقربا الثواني التوأمان · ريح الرمال · الظل الداكن |
| Rose-Tinted Memory · Broken Vow · Fading Lamp · DEAR[ ] · Nostalgia | ذكرى وردية · العهد المكسور · المصباح الخافت · DEAR[ ] · نوستالجيا |
| Rusty Mainspring · Dried Flower · Hourglass · Clockwork Music Box | الزنبرك الصدئ · الزهرة المجففة · الساعة الرملية · صندوق الموسيقى الزنبركي |
| Frozen Time · Parallel World · Echo of the Bell Tower · Daybreak | الزمن المتجمد · عالم مواز · صدى برج الجرس · بزوغ الفجر |
| Total Eclipse · Faded Photograph · Solitude · Heat Haze · Twilight | الكسوف الكلي · الصورة الباهتة · العزلة · تموج الحر · الغسق |
| Astronomical Twilight · Sunshower · Time Loop Ending · Passing Years | الشفق الفلكي · مطر مشمس · نهاية الحلقة الزمنية · مرور السنين |
| Eye of Odin · Forgetfulness · Night Train · Time Paradox | عين أودين · شرود الذهن · قطار الليل · مفارقة الزمن |
| Eleven Fifty-Nine · That Summer · Lingering Summer · The Last Page | الحادية عشرة وتسع وخمسون · ذلك الصيف · الصيف الممتد · الصفحة الأخيرة |
| Bullet of Upheaval · Waters of Lethe · Pocket Watch Tick Marks | طلقة الثورة · مياه ليثي · تدريجات ساعة الجيب |
| Worn Gray Coat · Four-Leaf Clover Bookmark · High Noon Duel | المعطف الرمادي البالي · فاصل البرسيم الرباعي · مبارزة الظهيرة |
| Coin on the Rails · Pinwheel · Final Firework · Stopped Pulse | عملة على القضبان · دوارة الهواء · آخر الألعاب النارية · النبض المتوقف |
| Fateful Coin Toss · Rustless Armor · Delayed Arrival · Transfer Station | رمية القدر · الدرع الذي لا يصدأ · الوصول المتأخر · محطة التبديل |
| Endless Journey · Sparks on the Rails · Smoke Ring · The Last Train | الرحلة التي لا تنتهي · شرر على القضبان · حلقة الدخان · القطار الأخير |

## 상점 묶음 · 기타

| 한국어 | English | العربية |
|---|---|---|
| 여정 · 마무리 | Journey · Finale | الرحلة · الختام |
| 흐르는 모래 | Flowing Sand | الرمل المتدفق |
| 매듭 | Knot | العقدة |
| 돋보기 안경 · 이름 지워진 팔찌 · 삐뚤빼뚤한 쪽지 | Magnifying Spectacles · Nameless Bracelet · Crooked Note | النظارة المكبرة · السوار الممحو الاسم · القصاصة المعوجة |
| 자정 · 12:00 | Midnight · 12:00 | منتصف الليل · 12:00 |
| 꿈 제목 '마지막 장' | The Final Chapter | الفصل الأخير |
| 4장 '그 사람' | she / her | هي (مؤنث) |
| 11:59 | 11:59 | 11:59 |
