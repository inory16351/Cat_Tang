You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch3_walls.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_walls.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch3_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
WALL modules that line the edges of square combat rooms (28 x 24 m) of the mirror world. The game tiles them side by side along a wall line, so each module has a mostly flat back, a clear front (+Y), and EXACTLY the width listed (X from -W/2 to +W/2; small rocks/curls may overhang <= 0.2 m). Depth (Y) about 1.2-2.2 m, with the main front face near Y = +1.0. They are seen from above-front, so the front face, top silhouette and top surfaces matter most. Height 3-5 m unless stated (the game lowers the south wall).

## Pieces
1. `Cliff_Shore` 6 m: mirror lake SHORE (stage 1): stepped rose `sandstone` boulders and slabs (3 height tiers 1.5 / 3 / 4.2 m, irregular, slightly different per seed), `sandstone_dark` lower faces, `sandstone_light` top facets, a thin vein of `gold` running diagonally across one big slab, a `sand` slope wedge at the foot (front), 3 small `crystal` clusters tucked at the foot and on a ledge.
2. `Wall_Colonnade` 8 m: RUINED colonnade (stages 2-3): a low `sandstone` back wall 1.4 m high along the back; in front of it 4 white `marble` columns (fluted, 0.6 diameter) on square `marble_dark` bases with `gold` capitals, spaced evenly; columns 1 and 3 full height 4.2 m carrying a broken `marble` architrave beam (with a `gold` trim band) that spans only part of the module, column 2 broken off at 2.2 m with a jagged top, column 4 broken at 3 m; `marble_dark` rubble at the foot.
3. `Wall_Arch` 6 m: ruined ARCH wall: a `sandstone` wall 4.5 m high with a big round-topped recessed arch (2.6 wide x 3.4 high, recessed 0.4) in the middle; inside the recess a `mirror` panel; `gold` keystone and a `gold` thin band outlining the arch; the top edge of the wall is broken/jagged on one side; `sandstone_light` cap stones on top.
4. `Wall_MirrorFrame` 4 m: a `sandstone` wall 3.6 m high with a large ornate `gold` frame (2.4 x 2.6, oval or arched) mounted on its front holding a `mirror` panel, a small `gold` sun-ray crest above it, a `marble` ledge shelf under it.
5. `Wall` 4 m x 3 m: generic `sandstone` block wall segment (staggered blocks, a few chipped corners) with a `marble` cap and a thin `gold` line under the cap - fallback filler.
6. `Cliff_Island` 6 m: edge of a FLOATING ISLAND (stage 3): a low irregular rim of `rock` / `sandstone` stones 0.8-1.6 m high with two small gaps, a few `crystal` shards, and below it, behind the rim (negative Y side), the island underside hanging down: an inverted jagged `rock_dark` cone mass going down to Z = -4 m (its top flush with Z 0, behind Y = 0.2). Front face of the rim at about Y = +0.6.
7. `Wall_Cloud` 6 m: INVERTED SKY (stage 4): a bank of big puffy `cloud` balls (3 rows, 1.2-2.4 m radius, flattened bottoms, `cloud_shade` on the lower half of each ball as separate flattened spheres) about 3.5 m high, and sticking UP out of the cloud tops, an upside-down small house: a `sandstone_light` box body (2 x 1.6 x 1.4) whose `roof_tile` gable roof points DOWN into the clouds, with one `window_lit` window and a `wood` door upside down, tilted ~10 deg.

Keep each under ~3500 triangles.
