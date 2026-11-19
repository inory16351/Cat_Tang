You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch3_landmarks.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_landmarks.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch3_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
LARGE obstacle landmarks placed inside combat rooms (28 x 24 m) of the mirror world to break up the rectangle and give cover (players walk around them; they are solid, so the footprint must be compact and mostly filled at ground level). Each must read instantly from a high camera at ~55 deg.

## Pieces (footprint W x D x H in m)
1. `LM_ArchRuin` (5 x 1.8 x 4.6): free-standing ruined double arcade: 3 white `marble` columns on `marble_dark` square bases with `gold` capitals, two round `sandstone` arches spanning them (the right arch broken half-way with a jagged end), `gold` keystone on the intact arch, rubble at the foot.
2. `LM_CrystalSpire` (3.6 x 3.4 x 4.6): a giant crystal formation: 7-9 big hexagonal `crystal` prisms with pointed tips fanning out of a lumpy `rock` / `sandstone` mound (1 m high), the tallest 4.6 m, a glowing `crystal_core` prism inside the two biggest, 6 small `stardust` octahedra floating around the top (separate islands).
3. `LM_Hourglass` (3.6 x 3.0 x 4.2): a giant broken HOURGLASS monument half sunk and tilted ~20 deg in a `sand` mound: `gold` top and bottom discs with 4 twisted `gold_dark` posts, the upper `crystal` bulb cracked open with a jagged hole, a `sand` stream frozen mid-air pouring from the hole down to the mound (chunky curved tube), the mound wider than the hourglass.
4. `LM_FloatRock` (4.0 x 3.4 x 4.2): a FLOATING rock island chunk: on the floor a low `sand` / `rock` mound (0.6 m) with a few crystals; hovering above it from Z 1.4 to 4.2 an inverted jagged cone of `rock_dark` (point down) with a flat `sandstone` top surface carrying two tiny broken `marble` columns and a `crystal` cluster; 3 small loose rocks floating between mound and island (separate islands).
5. `LM_MirrorGate` (5.0 x 1.4 x 5.2): a big standing ring PORTAL: a circular ornate `gold` ring (outer diameter 4.2, profile 0.35) with sun-ray spikes around its outside, holding a round `mirror` disc (double sided), standing on a stepped `sandstone` / `marble` plinth (2 steps, 5 x 1.4 x 0.7), two small `gold` lantern finials on the plinth corners.
6. `LM_Obelisk` (2.4 x 2.4 x 6.0): a rose `sandstone` obelisk on a square stepped `marble` base (2.4 x 2.4 x 0.8), tapering shaft with a pyramid `gold` tip, a round `gold` clock face inset on the front (+Y) near the top (with two hands), a diagonal crack line, and 2 `crystal` shards growing from the base corner.
7. `LM_Dune` (5.6 x 4.0 x 1.7): a smooth wind-shaped `sand` dune mound (soft, low-poly but smooth top), a half-buried tilted ornate `gold` mirror frame (with `mirror` glass) sticking out of its crest, 2-3 `sandstone` rocks at its foot.

Keep each under ~4500 triangles.
