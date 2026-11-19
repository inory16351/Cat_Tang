Write ONE new Blender script `Art/tools/blender_kit_village_dream.py` (do not edit any other file).
Read `Art/tools/specs/_village_common.md` first and follow it (materials, richness rules, coordinates, export path `Game/Assets/Art/Characters/Village/Kit_V_<Name>.fbx`, preview).

The user wants MORE scattered objects across the floating islands that show a DREAMLIKE mood and "BROKEN TIME". These are set pieces placed between buildings, along paths and near island edges. Big readable silhouettes from a ~55 deg top-down camera, cel-shaded, chunky.
The village ground is now DUSTY BLUE / periwinkle at dawn, so these pieces may use cool glowing accents (new materials below) mixed with warm brass / gold / wood.

Extra role materials allowed for this kit (grey like the others, named exactly):
`crystal_blue` (pale blue time crystal body), `crystal_core` (bright glowing inner crystal), `rift_glow` (glowing white-cyan light of a tear in time), `moon` (pale silver-white), `glass_blue` (blue-tinted glass).
Glowing parts must use `rift_glow`, `crystal_core`, `sand_glow` or `stardust` (the game makes them emit light).
DO NOT add tiny detached floating dust above objects; detached floating PARTS that are part of the design (shards, gears, steps) are fine — the game animates whole pieces.

Pieces (W x D x H overall, origin at bottom center unless noted):
1. `ClockHand_Giant` ~ 2 x 7 x 4 m — a gigantic clock MINUTE HAND (ornate `iron` blade with `gold` filigree and a `brass` counterweight ring) stabbed diagonally into the ground about 35 deg, cracked earth slabs (`earth`, `rock`) heaved up around the impact, a few chips of rock frozen just above the cracks.
2. `Gear_Half` ~ 4 x 1.2 x 3 m — a huge `brass` gear standing upright, half buried, several teeth broken off, `rust` patches, `iron` hub with `gold` spokes, a smaller `brass` gear leaning against it.
3. `GearStack_Float` ~ 2.4 x 2.4 x 1.4 m, origin at CENTER — three gears of different sizes stacked loosely on a shared `iron` axle (`brass`, `gold`, `rust`), meant to float and slowly rotate (the game spins it around Y).
4. `ClockShards` ~ 3 x 1.5 x 3 m, origin at CENTER — a large clock face (`clock_face` with `brass` rim and `gold` numerals) SHATTERED into 7-9 jagged pieces frozen while bursting outward, the pieces separated by small gaps, the two hands broken apart, `rift_glow` light leaking from the cracks (thin glowing slivers along the break edges).
5. `Hourglass_Broken` ~ 2 x 2 x 4 m — a tall hourglass on a carved `stone` pedestal with a `brass` / `gold` frame, the upper glass bulb (`glass_blue`) cracked open, the `sand_glow` sand flowing UPWARD from the crack in a curved ribbon that ends in a floating clump above.
6. `TimeRift` ~ 1.6 x 0.6 x 3.2 m — a vertical jagged TEAR IN THE AIR: a lightning-like zig-zag slit of `rift_glow` slabs, framed by sharp `crystal_blue` shards growing from its edges, with a few clock pieces (gears, numerals, a hand) being sucked into it; the bottom 0.4 m starts slightly above the ground on a cracked `stone_dark` base.
7. `Pendulum_Frozen` ~ 2 x 1 x 4.5 m — a broken grandfather-clock frame (`wood_dark`, `gold` trim) with no case front, its pendulum (`brass` rod, big `gold` bob) frozen mid-swing at a steep 40 deg angle as if time stopped, a cracked `glass_blue` panel on the ground.
8. `MeltWatch_Rock` ~ 1.8 x 1.4 x 1.4 m — a rounded `rock` boulder with a large pocket watch (`brass`, `clock_face`, `iron` hands) melting and draping over its edge in `gold_drip`, the chain trailing on the ground.
9. `StairsFloat` ~ 3 x 1.6 x 2.4 m, origin at CENTER — a broken fragment of a staircase (4 steps of `plank` and `stone`, `wood_dark` stringers, a piece of `iron` railing) torn from somewhere, with `rock` / `earth` clinging to its broken end; meant to float and bob.
10. `TimeCrystal` ~ 1.8 x 1.6 x 2.2 m — a cluster of tall faceted `crystal_blue` prisms with `crystal_core` glowing centers, growing from a cracked `stone_dark` / `rock` base, one prism has a tiny `clock_face` dial trapped inside (visible as an inset).
11. `NumeralStones` ~ 2.5 x 1.5 x 1.3 m — three chunky carved stone blocks shaped like clock numerals (XII, IX, III as simple bold bar shapes) in `stone`, `stone_dark`, `moon`, half sunk at angles, with `gold` inlay lines and moss of `cactus_dark`.
12. `MoonDial` ~ 1.8 x 1.8 x 3.6 m — a slender `iron` / `brass` pole sculpture holding a crescent `moon` disc with a small `clock_face` sundial plate, hanging tiny `brass` bells; a little `crystal_core` star at the top.
