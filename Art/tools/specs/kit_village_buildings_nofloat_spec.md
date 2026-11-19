Edit ONE existing Blender script: `Art/tools/blender_kit_village_buildings.py` (do not edit any other file).
Read `Art/tools/specs/_village_common.md` for context.

Change requested by the user: the small DETACHED fragments floating ABOVE each object (little cubes / planks / shingles / gears / numerals / sand clumps / sparkles hovering over roofs, chimneys, signs, fence tops, tank tops, etc.) must be REMOVED from the models. The game now spawns them as slowly rising particles instead, so the static ones look wrong.

Rules:
- Remove every piece of geometry that is NOT physically connected to the object's body and sits above it (the rising "dissolve" fragments and their `sand_glow` / `stardust` trails above the object).
- KEEP the dissolving look on the object itself: the jagged / broken / crumbling top edge, missing shingles, gaps, cracks, sagging melted clocks (`gold_drip`), and glowing `sand_glow` grains that are attached to or lying on the object. It should read as "the top is crumbling away" without anything floating.
- Keep every piece name, width / footprint, origin, export path and export settings the same. Height will now be lower (the floating layers are gone) — that is expected.
- Keep at least 5 role materials per piece.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_buildings.py` and fix errors until every piece exports, and re-render the preview PNG.
Print `EXPORTED <Name> <tris> <dims>` per piece as before.
