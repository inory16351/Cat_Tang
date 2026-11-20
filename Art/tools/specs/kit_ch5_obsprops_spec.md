You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch5_obsprops.py` (create this file; do not edit any other file except its FBX outputs and preview PNG).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_obsprops.py`
After writing it, RUN it once and fix errors until it exports every piece. Then look at the preview PNG and fix anything that reads badly.
Read FIRST `Art/tools/specs/_ch5_observatory_common.md` (setting = Greenwich Observatory of Time, colour meanings), then `Art/tools/specs/_ch5_common.md` (richness, material-name mechanism, coordinates, FBX export settings — ignore its old violet/gold setting text).

## Goal
REPLACE the look of chapter-5 props and floating debris (they still show Wild-West / train / desert remnants that do not fit the new observatory concept) with observatory / time-instrument versions under the SAME names, the SAME size, origin and front conventions, so the game keeps placing them and keeps their gameplay. Measure each old piece first: the old scripts are `Art/tools/blender_kit_ch5_props.py` and `Art/tools/blender_kit_ch5_debris.py` (and the old FBX files in `Art/models/ch5_unwarped/Kit_<Name>.fbx` — import them to read the exact bounding box and origin). Keep each new piece's footprint and height within ±10 % of the old one, origin at the same place (props: bottom centre; FD_* floating debris: origin = CENTRE of the piece, because the game spins them in the air).

## Pieces
Props (gameplay kept by the game):
1. `WarpedKeg` (explodes when shot): an unstable brass CHRONOMETER CANISTER — a squat cylinder of `brass` bands and `indigo` panels with a cracked `dial_face` gauge on top, cyan `crack_gold` cracks glowing, a small `red_lacquer` warning ring, a couple of `steel` valves.
2. `WarpedTrunk` (floats and drifts): a floating astronomer's INSTRUMENT CASE / big closed star atlas — `wood_dark` case with `brass` corners and clasp, a `night_sky` star-chart panel with `starlight` dots on the lid, a few loose chart pages hovering (joined).
3. `WarpedSpring` (wind-up spring that launches): keep a coiled clock spring but on an observatory base: `steel` spring, `brass` ratchet wheel, `marble` base with `void_stone` panel.
4. `CoverShardRock` (breakable cover): broken observatory MASONRY — chunks of `marble` cornice and `void_stone` navy brick with a piece of `brass` meridian strip, cyan cracks.
5. `CoverMemoryPile` (breakable cover): a pile of fallen INSTRUMENTS and BOOKS — stacked `wood`/`cloth` books, a toppled `brass` small telescope, a cracked `dial_face` clock, a rolled star chart (`night_sky` + `starlight`).
6. `CoverBrokenColumn` (breakable cover): a broken fluted `marble` column drum with a `gold` capital fragment and a `brass` band, navy `void_stone` plinth, cyan cracks.
Floating debris (no collision, spin in the air, origin = centre):
7. `FD_Saloon`: a curved OBSERVATORY DOME SHELL fragment (`void_stone` plates, `brass` ribs, edge of the slit).
8. `FD_Rail`: a broken TELESCOPE TUBE section (`wood_dark` tube, `brass` rings, `mirror` lens at one end).
9. `FD_StationClock`: a floating fragment of the 24-HOUR GATE CLOCK dial (`dial_face` wedge, `dial_mark` ticks, `gold` bezel arc, one `brass` hand).
10. `FD_Bell`: a broken ARMILLARY RING cluster (2-3 crossed `brass` ring arcs with a small `crystal` sphere).
11. `FD_DesertPillar`: a floating piece of a `marble` pilaster with a `brass` PRIME MERIDIAN strip and a `void_stone` brick corner.
12. `FD_MirrorFrame`: a floating STAR-CHART panel (`night_sky` slab with raised `starlight` stars and thin `steel` constellation lines, `gold` frame corner).
Keep `FD_Gears` and `FD_Island` as they are (do NOT export them).

## Output
Export each to `Game/Assets/Art/Characters/Ch5/Kit_<Name>.fbx` (overwrite). Budgets: props up to ~5000 tris, debris up to ~3000. 5+ role materials each.
Preview: ONE PNG of all 12 pieces in a grid to `Art/tools/specs/blender_kit_ch5_obsprops_preview.png` (Workbench, flat colours close to the new palette).
