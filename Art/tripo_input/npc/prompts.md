# 마을 NPC 5 — ChatGPT 원화 프롬프트 (Tripo 입력용)

시계공이 잊어버린 존재 (컨셉 기획서 5.2 · 시설 표). 사람 얼굴 · 피부 없음, 적(차가운 보라 + 빨간 테두리)과 구분되게 따뜻한 색.
상인 원화(`../merchant/ch1_bartender_main_dn.png`)와 같은 결: 플랫 컬러 · 굵은 검은 외곽선 · 명암 거의 없음 · 정면 · 팔 살짝 벌린 A자 · 연회색 단색 배경 · 한 장에 한 캐릭터.
흐름: ChatGPT 원화 → `Art/tools/keep_main.py` · `denoise.py` → Tripo H3.1 + 리깅 → `Game/Assets/Art/Characters/Npc/` → 빌더 → 마을 `NpcSpot_*`.

## 공통 꼬리말 (모든 프롬프트 끝에 붙임)

Single character, full body, front view, standing straight in a relaxed A-pose with arms slightly away from the body, symmetrical, feet visible. Flat cel-shaded cartoon style for a 3D game model reference: solid flat colors, thick clean black outlines, almost no shading, no gradients, no texture noise. Plain light gray background (#C8C8CC), no ground shadow, no text, no frame. NOT a human: no human face, no human skin. Warm palette (brass, gold, amber, cream, warm brown, muted red). Whimsical, dreamlike, slightly melancholic — a forgotten being slowly eroded by time.

## 1. 태엽 풀린 뻐꾸기 — 기억의 방 (`cuckoo`)
A bipedal cuckoo-clock bird keeper, the very first cuckoo a clockmaker ever built. Stands upright like a small innkeeper, about 1.5 head-heights tall chibi proportions. Wooden carved bird head with a tiny brass beak slightly open (it forgot its song), round painted eyes. Body is a little carved wooden cuckoo-clock house worn like a vest, with a small closed shutter on the chest. Wings act as arms with rusted copper feather tips; a few rusty feathers are flaking off and falling. A big unwound winding key sticks out of its back, drooping. Two pine-cone chain weights hang from its waist like a belt.

## 2. 모래시계 대장장이 — 탄약 공방 (`hourglass`)
A sturdy blacksmith whose torso IS a large cracked hourglass in a brass frame, sand slowly leaking from the crack and pouring down. No head — where the head would be, the top bulb of the hourglass glows faintly with amber sand. Thick brass-and-leather arms with heavy gauntlet hands, one hand holding a small blacksmith hammer, a leather apron hanging from the lower frame with a few brass bullet casings in its pockets. Short sturdy legs ending in iron boots, a little pile of golden sand at its feet.

## 3. 빈 외투 보안관 — 총포상 (`sheriff`)
An empty sheriff from an old western film the clockmaker saw as a child. A long faded duster coat standing upright with no body inside — the coat is sepia and slightly washed-out like an old film still, with faint film-scratch lines. Where the head should be floats a melting brass pocket watch (hands stuck near 11:59), with a wide-brimmed cowboy hat hovering above it. Empty gloves float at the end of the sleeves, a tarnished sheriff star badge on the chest, a gun belt with an empty holster around the waist, empty boots below.

## 4. 실타래 재봉사 — 의상실 (`seamstress`)
The clockmaker's wife's sewing machine come alive as a gentle seamstress figure. Instead of a face, a large wooden thread spool with colorful thread (muted red, cream, gold) wound around it. Slim body shaped like an old cast-iron sewing machine dressed in a long cream skirt with a patchwork apron. Her arms are long brass clock hands ending in delicate pincushion hands with needles. A faded yellow tape measure unwinds from her shoulders and flutters in loops around her. Thimbles as buttons.

## 5. 괘종시계 문지기 — 시간의 문 (`gatekeeper`)
A tall, solemn gatekeeper whose body is an antique grandfather (longcase) clock in dark polished walnut with gold inlay, standing on two short carved wooden legs, with long thin wooden arms. The clock face at the top has no numbers — it is filled with swirling lavender-gray fog (no eyes, no mouth). Through the glass door of its body, the swinging pendulum is a glowing brass lantern with a warm amber flame. A ring of old keys hangs from one hand. Taller and thinner than the others, about 2 times the height of the cuckoo.
