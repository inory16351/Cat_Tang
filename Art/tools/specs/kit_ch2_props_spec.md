You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch2_props.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_props.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch2_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
Gameplay props and cover for combat rooms of the stopped station. Several are EXPLOSIVE / INTERACTIVE objects the player shoots - they must have a big, simple, instantly readable silhouette.

## Pieces (footprint W x D x H in m)
1. `SteamValve` (1.0 x 0.8 x 1.6): an upright cast `iron` steam pipe stub coming out of the floor with a big `brass` round valve WHEEL on top and a pressure gauge (`clock_face` dial, `brass` rim) on the side, a short horizontal NOZZLE pointing to +Y (front) where steam will shoot out, `rust` bands, a small frozen `steam` puff (lumpy blob of 3 spheres) hanging in the air 0.3 m in front of the nozzle. Front = +Y = steam direction.
2. `FloatLuggage` (0.9 x 0.5 x 0.7, origin at the bottom of the case; the game lifts it in the air): a fat old travel TRUNK tilted ~15 deg as if frozen while falling: `leather` body, `leather_dark` straps, `brass` corners and lock, a `paint_cream` luggage tag on a string sticking out STIFFLY sideways (frozen).
3. `MineCart` (1.4 x 1.0 x 1.0): a small rail ore cart: `iron` tub with riveted `rust` bands, 4 `iron_dark` wheels, filled with `coal` lumps, no rails (rails are separate). Front = +Y = direction it rolls.
4. `RailTrack` (length 4 m along Y, gauge 1.5 m, width 2.6 m): two `rail_steel` rails on 5 `wood_dark` sleepers + `stone` ballast strip (low, height <= 0.2). Tileable end to end along Y.
5. `Bench` (2.2 x 0.7 x 1.0): station bench, `wood` slats, cast `iron` curly legs and armrests.
6. `LuggagePile` (2.0 x 1.4 x 1.4): cover: stacked suitcases and hatboxes (`leather`, `leather_dark`, `canvas`, `brass` corners), one suitcase frozen half-sliding off the top.
7. `Crates` (1.8 x 1.2 x 1.2): two `wood_light` freight crates with `iron` corner bands + a `canvas` sack.
8. `TicketBooth` (2.4 x 1.4 x 2.6): merchant booth that rises from the floor: `wood_dark` booth with `paint_cream` panels, arched barred window (`iron` bars, `window_lit`), a counter, a `brass` bell, a `roof_tin` little roof with a small round `clock_face` on top. Front = +Y.
9. `SignalLamp` (0.5 x 0.5 x 2.4): railway signal post: `iron` pole, a lamp head with three round lenses `signal_red` (top), `signal_amber`, `signal_green` (bottom) facing +Y, `iron_dark` hoods over each lens.
10. `StationClock` (0.8 x 0.4 x 3.0): free-standing platform clock on a cast `iron` post, double-sided round `clock_face` with `brass` rims, hands frozen at 11:59.

Keep each under ~2500 triangles.
