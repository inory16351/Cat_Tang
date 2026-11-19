Edit ONE existing Blender script: `Art/tools/blender_kit_village_buildings.py` (do not edit any other file).
Read `Art/tools/specs/_village_common.md` for context.

Change requested by the user: the five buildings should look much more BROKEN / DESTROYED by time — as if pieces of them have been torn away into the dawn sky (the game fills the broken areas with slowly RISING particles, so the gaps are where things "float away").

For every building (Inn, GunShop, AmmoWorkshop, Wardrobe, TimeGate):
- Make 2-4 clear destruction areas, mostly on the UPPER half and the roof: big ragged HOLES in the roof showing rafters and a dark interior (`wood_dark`), a COLLAPSED CORNER where the wall boards end in a jagged broken line, a missing chunk of the false-front crest, a broken-off balcony / awning end, exposed beams and snapped planks sticking out at angles, a few cracks (inset grooves in `wood_dark` / `stone_dark`).
- Broken edges should step down irregularly (staircase-like jagged silhouettes), chunky, readable from a ~55 deg top-down camera.
- Some crumbling fragments may still be ATTACHED (hanging by a nail, resting on the roof), but NOTHING detached floating above (the game makes those as particles).
- Keep each building's FRONT DOOR, porch, steps and the front boardwalk intact and walkable; keep the signature identity items readable (Inn cuckoo-clock crest, GunShop revolver sign, AmmoWorkshop hourglass sign, Wardrobe spool sign, TimeGate clock dial and grandfather-clock pillars). The TimeGate keeps its 4.5 m wide empty passage.
- Keep piece names, footprint / width, origin, export path and export settings the same; keep >= 6 role materials per building; triangle budget up to ~14000.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_buildings.py` and fix errors until every piece exports, and re-render the preview PNG. Print `EXPORTED <Name> <tris> <dims>` per piece.
