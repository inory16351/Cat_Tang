Write ONE new Blender script `Art/tools/blender_kit_village_mystic.py` (do not edit any other file).
Read `Art/tools/specs/_village_common.md` first and follow it (materials, richness rules, coordinates, export path `Game/Assets/Art/Characters/Village/Kit_V_<Name>.fbx`, preview).

The hub village floats in a dreamlike dawn void (dusty blue ground, upside-down night sky below). The user wants MYSTERIOUS TREES and objects scattered around the islands to strengthen a quiet, vanishing, otherworldly mood: things that glow softly, look ancient and half-dissolved, and feel like a journey through a fading dream. Cel-shaded, chunky, big readable silhouettes from a ~55 deg top-down camera. No people.

Extra role materials allowed for this kit (grey like the others, named exactly):
`bark_pale` (pale silvery bark), `bark_dark` (dark violet-grey bark), `leaf_crystal` (translucent-looking crystal leaves, body color), `crystal_blue`, `crystal_core` (bright glowing), `rift_glow` (glowing white-cyan), `moon` (pale silver-white), `glass_blue`, `petal_glow` (glowing pale pink-violet petals).
Glowing parts must use `crystal_core`, `rift_glow`, `petal_glow`, `lantern_glow`, `sand_glow` or `stardust` (the game makes them emit light).
No tiny detached dust above the objects (the game adds particles). Designed detached parts (floating stones, lanterns, fruit) are fine.

Pieces (W x D x H overall, origin at bottom center):
1. `DreamTree_Crystal` ~ 5 x 5 x 7 m — an ancient tree with a thick twisting `bark_pale` trunk spiraling upward, roots partly lifted off the ground with small rocks floating among them, wide flat canopy made of faceted `leaf_crystal` leaf clusters with `crystal_core` glowing hearts, a few small clock-dial "fruits" (`clock_face` + `brass` rim) hanging on thin chains, `stardust` motes caught in the branches (attached).
2. `DreamTree_Willow` ~ 5 x 5 x 6.5 m — a weeping willow with a `bark_dark` gnarled trunk, long hanging strands (thin chunky ribbons) made of `rift_glow` and `petal_glow` beads drooping to the ground, an old `brass` lantern hanging on one branch (`lantern_glow`), a few roots sinking into a small pool of glowing `glass_blue` "water" with `moon` stepping stones.
3. `DreamTree_Small` ~ 2 x 2 x 3 m — a young slender tree of `bark_pale` with round glowing bulbs (`crystal_core` / `petal_glow`) instead of leaves, on a mossy rock (`rock`, `cactus_dark`).
4. `SpiritLanterns` ~ 3 x 3 x 4 m, origin at bottom center — a tall thin `iron` post with a crooked arm, from which 5-7 round paper-like lanterns (`canvas` shells with `lantern_glow` cores and `brass` caps) hang at different heights; plus 2-3 lanterns detached floating around the top (part of the design).
5. `BellFlowers` ~ 2.4 x 2.4 x 1 m — a clump of tall drooping bell-shaped flowers with `petal_glow` / `crystal_core` glowing bells, `cactus_dark` stems and leaves, on a low mossy mound (`earth`, `cactus`).
6. `RuneArch` ~ 4 x 1.2 x 4.5 m — a broken ancient stone arch (`stone`, `stone_dark`, `moon` keystone) with carved clock numerals and glowing runes (`rift_glow` inlay lines), the top half crumbling with several blocks missing (jagged), ivy (`cactus_dark`) creeping up one side, a cracked `glass_blue` disc set in the remaining side.
7. `FloatRock_Crystal` ~ 3 x 3 x 3.5 m, origin at CENTER — a floating inverted-cone rock (`rock`, `rock_dark`, `earth`) with a flat mossy top carrying a small `DreamTree_Small`-like sapling, and `crystal_blue` / `crystal_core` crystals hanging from its underside like stalactites.
8. `GlowMushrooms` ~ 1.6 x 1.6 x 1.2 m — a cluster of 6-9 tall mushrooms with `moon` stems and caps in `crystal_blue` with `crystal_core` glowing spots underneath, on `earth` with small pebbles.
