You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch2_landmarks.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_landmarks.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch2_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
LARGE obstacle landmarks placed inside combat rooms (28 x 24 m) of the stopped station to break up the rectangle and give cover (players walk around them, solid), plus the boss arena floor. Each must read instantly from a high camera.

## Pieces (footprint W x D x H in m)
1. `LM_Coach` (10 x 3.2 x 3.6): a stopped passenger CARRIAGE whose long front side (+Y) is OPEN / cut away like a dollhouse so the interior is visible: `wood` / `paint_green` body, `roof_tin` curved roof, `iron_dark` bogies with wheels, `window_lit` windows on the closed back side, 3 rows of `leather` seats inside, `brass` handrails, carriage floor 0.5 m high, two little `iron` step boxes in front.
2. `LM_Locomotive` (9 x 3.4 x 4.2): front part of a stopped black steam locomotive: `iron_dark` boiler, big `brass` round headlight with a `clock_face` lens, `soot` smokestack with a frozen `steam` puff above it, `cloth_red` painted cowcatcher, `iron` wheels with connecting rods.
3. `LM_WaterCrane` (3 x 3 x 6): railway water tower: `wood` tank on `iron` legs, swinging spout arm, `rust` streaks, a frozen `steam` water-droplet blob under the spout.
4. `LM_CoalPile` (4.5 x 3.5 x 1.8): heap of `coal` lumps with a `wood_dark` retaining board and an `iron` shovel stuck in it.
5. `LM_LuggageCart` (4 x 1.8 x 2.0): long flat baggage cart (`wood` bed, `iron` wheels and handle) loaded with a leaning stack of `leather` / `canvas` suitcases and trunks, top two cases frozen mid-fall above the stack.
6. `LM_SignalGantry` (5 x 1.2 x 4.5): `iron` lattice signal gantry with two signal heads (`signal_red`, `signal_amber`, `signal_green` lenses); legs are the solid part.
7. `Turntable` (diameter 24 m, height 0.3): the BOSS ARENA floor: a round `stone` pit edge ring 0.6 m wide, inner deck of `wood_dark` planks, two `rail_steel` track pairs crossing in a + through the center (along X and Y), a `brass` center hub with gear teeth, `iron` edge rail ring. Origin at center, top surface at Z = 0 (the deck sinks below 0).
8. `PlatformEdge` (8 x 1.2 x 0.5): raised stone platform edge segment (`stone` slab, `paint_cream` safety line along the top front, `stone_dark` face).

Keep each under ~4000 triangles (Turntable ~6000).
