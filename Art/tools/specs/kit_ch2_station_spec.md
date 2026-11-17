You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch2_station.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_station.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch2_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
WALL facade modules that line the edges of square combat rooms (28 x 24 m) of the stopped station. The game tiles them side by side along a wall line, so each module has a flat back, a clear front (+Y), and the width listed below; height 4.5-7 m. They are seen from above-front, so the front face, roofline and top silhouette matter most.

## Pieces
1. `Facade_StationHall` 8 m wide: brick station building front, two tall ARCHED windows (`window_lit` glass with `iron` mullions), `stone` sill band, `roof_tin` eave on top, a round `clock_face` clock with `brass` rim and stopped hands centered above the windows, small `soot` streaks under the eave.
2. `Facade_Waiting` 6 m: `wood` plank waiting-room wall, one wide window (`window`) with `paint_green` shutters, a `paint_cream` sign board (blank, no text), short `roof_tin` awning on two `iron` brackets.
3. `Facade_Warehouse` 7 m: freight warehouse, `brick_dark` lower half + `wood_dark` vertical boards upper half, a big sliding door (`wood` with `iron` X-bracing) slightly open showing a dark gap, a pulley beam sticking out at the top.
4. `Facade_EngineShed` 9 m: engine shed, tall `brick` wall with a huge ARCHED doorway (dark interior `soot` plane recessed 0.6 m), `stone` arch keystones, two small round windows, `rust` drip stains.
5. `Facade_Canopy` 8 m: platform canopy edge: row of 3 cast `iron` columns with decorative brackets holding a slanted `roof_tin` canopy (depth 2.5 m going back), a hanging `glass_lamp` lantern under it, low `stone` platform edge 0.4 m high along the bottom.
6. `Facade_Ticket` 5 m: small ticket office front, `paint_cream` wall, a barred ticket window (`iron` bars over `window_lit`), `wood_dark` counter shelf, `brass` bell on the counter, `roof_tin` little gable.
7. `Wall` 4 m wide x 3 m high generic `brick` wall segment with `stone` cap - fallback filler.
8. `Cliff_Embankment` 6 m: railway embankment / retaining wall for the outer freight yard: `stone_dark` big blocks stepped back, a `rail_steel` fence railing on top, a few `coal` lumps at the foot.

Keep each under ~3000 triangles.
