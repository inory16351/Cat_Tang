Edit ONE existing Blender script: `Art/tools/blender_kit_village_buildings.py` (rewrite the building geometry; do not edit any other file).
Read `Art/tools/specs/_village_common.md` for context (materials, coordinates, export).

The user wants the five buildings to have IRREGULAR, DREAMLIKE SHAPES instead of straight boxes. References they gave (only for mood, do not copy any real building):
- a "crooked house": walls and roof lines that bulge, sag and lean like melted wax; window and door frames warped into soft wobbly shapes; the whole facade bending as if seen in a dream.
- a twisted brick chimney that curls and spirals upward like a corkscrew.
- a cluster of offset stacked boxes (rooms piled at different angles and offsets, cantilevered, some jutting out, terraces on top of others).
Combine these with the existing look: a western frontier town (wood boards, false fronts, porches) that is crumbling away into time at the top (keep a few broken / torn areas at the upper parts and roof — the game emits rising particles from the highest points).

For every building (Inn, GunShop, AmmoWorkshop, Wardrobe, TimeGate):
- Build the body from 2-4 VOLUMES stacked and offset (e.g. a ground floor, a second box shifted sideways and rotated 8-15 deg, a small third room cantilevered on top), each volume's walls BENT: use curved / lattice / simple-deform (bend, taper, twist) so walls lean and bulge, roof edges wave like melting wax, eaves droop.
- Windows and doors: rounded wobbly frames (not rectangles), different sizes, some tilted.
- At least ONE twisted / curling chimney or tower per building (spiral bend, brick bands in `stone` / `stone_dark` / `rust`), rising high — it should be the tallest point of the building.
- Keep each building's identity readable: Inn = 2-3 stacked tilted floors with balconies and the cuckoo-clock crest, GunShop = revolver sign + striped awning, AmmoWorkshop = big barn doors + hourglass sign + twisted forge chimney, Wardrobe = spool sign + bay window with dress forms, TimeGate = two grandfather-clock pillars (they may lean and bend toward each other) and the cracked 11:59 dial on top, keeping the 4.5 m wide empty passage.
- Keep the FRONT DOOR, steps and the front boardwalk at ground level straight, walkable and centered on +Y like before; keep the footprint width / depth and origin the same (overall size may grow upward a little, max ~11 m).
- Keep >= 6 role materials per building (same material names as before), chunky cel-shaded detail (no detail thinner than 0.04 m), up to ~16000 triangles each.
- Nothing detached floating above the buildings (the game adds particles).
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_buildings.py` and fix errors until every piece exports, and re-render the preview PNG. Print `EXPORTED <Name> <tris> <dims>` per piece.
