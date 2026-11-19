You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch3_bosses.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_bosses.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch3_common.md` and `Art/tools/specs/_ch3_detail_v2.md` first (materials, coordinates, export, detail rules: rich, many colors, ornate).

## IMPORTANT difference from the other kit scripts
These two bosses are NOT rigged. The game animates them by rotating named PARTS. So each boss is exported as ONE FBX that contains SEVERAL separate mesh objects (do NOT join them), each named exactly as listed, each with its object ORIGIN placed at its pivot / joint (listed), all top level (no parenting), positioned in their rest pose. Use the same export settings (object_types MESH etc.) and export all parts of one boss together into `Game/Assets/Art/Characters/Ch3/Kit_<BossName>.fbx`. Front = +Y. Print one EXPORTED line per boss listing its part names.
Monsters use the cool "monster" colour family, so ON THESE TWO BOSSES ONLY you may also use these material names: `boss_lavender`, `boss_slate`, `boss_crimson`, `boss_cream` (in addition to the common and detail lists).

## 1. `Ch3_Magician` (Mirage Magician, clockwork stage magician, total height 4.0 m, mid boss)
A tall clockwork puppet magician with no human face: a smooth porcelain `boss_cream` mask face with a painted `gold` crescent smile and two glowing `crystal_core` eye slits. Parts:
- `Body` (pivot at floor center 0,0,0): legs in `boss_slate` trousers with `gold` side stripes and pointed shoes, long tailcoat `boss_lavender` with `gold` buttons and filigree lapels, a big flowing cape (`boss_crimson` outside, `boss_cream` lining visible at the edges, stiff cartoon folds) hanging from the shoulders to near the floor, a wind-up key (`gold` and `bronze`) sticking out of the back between the shoulders, a `gem_rose` brooch at the collar.
- `Head` (pivot at the top of the neck, about Z 3.05): the mask face plus a TALL top hat (`boss_slate` with a `boss_crimson` band, `gold` buckle, 3 playing cards tucked in the band), small `gold` gear ear plates.
- `ArmL` (pivot at the left shoulder, about X -0.55, Z 2.85): sleeve `boss_lavender` with `boss_cream` cuff, white glove hand `ivory`, holding a short `boss_slate` wand with a `gold` tip. Rest pose: arm hanging down slightly outward.
- `ArmR` (pivot at the right shoulder, about X +0.55, Z 2.85): same sleeve, the hand holds a FAN of 7 playing cards (`ivory` faces with `gold` clock numerals, `boss_crimson` backs) spread 70 degrees. Rest pose: forearm raised forward.
The silhouette must read from above: hat, cape, cards. Total under about 12000 triangles.

## 2. `Ch3_Whale` (Mirror Whale, final boss, length 14 m along Y with the head at +Y, height about 5 m, width about 6 m with fins)
A huge stylized whale whose skin is a MIRROR: body mostly `mirror` plates separated by thin `gold` seams (like a stained glass grid of big mirror plates), belly `boss_cream` with `boss_lavender` groove lines, a huge clock dial on the back (`ivory` dial ring with `gold` Arabic numeral (1–12) ticks and two `gold` hands) centered on top, `crystal` and `crystal_rose` shards growing along the spine ridge, glowing `crystal_core` eyes, a few floating `stardust` bits. Chunky, cute but majestic cartoon shape (rounded head, tapering body).
Parts (origins at the joints):
- `Body` (pivot at body center at floor level: X 0, Y 0, Z 0; the belly bottom is at about Z 0.3): head and main body back to the tail root at about Y -4.5.
- `Tail` (pivot at the tail root, about Y -4.5, Z 1.8): tail stock plus a big fluke (span about 6 m, `mirror` with `gold` rim and `gem_rose` studs) ending at about Y -7.
- `FinL` and `FinR` (pivots at the fin roots on the body sides, about X -2.3 / +2.3, Y 1.5, Z 1.4): pectoral fins 3 m long angled down and out, `mirror` with `gold` rim.
- `Jaw` (pivot at the jaw hinge, about Y 4.2, Z 1.3): lower jaw `boss_cream` with a `gold` lip line, so the game can open the mouth.
Total under about 16000 triangles.

Render ONE preview PNG of both bosses (Workbench, a distinct colour per material, camera about 45 degrees above front, 1600x900) to `Art/tools/specs/blender_kit_ch3_bosses_preview.png`.
