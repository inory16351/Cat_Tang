Edit ONE existing Blender script: `Art/tools/blender_kit_ch3_walls.py` (do not edit any other file). Keep every piece name, EXACT width, origin, export path the same; rebuild the geometry of EVERY piece much richer.
Read `Art/tools/specs/_ch3_common.md` and then `Art/tools/specs/_ch3_detail_v2.md` (the detail rules for this pass) first.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_walls.py` and fix errors until it exports every piece and re-renders `Art/tools/specs/blender_kit_ch3_walls_preview.png`.

These modules are tiled side by side along room edges, so: no rows of identical plain boxes, irregular top silhouettes, and the ends (X = +-W/2) must still meet neighbours cleanly.

Piece-specific ideas (on top of the general rules):
- `Cliff_Shore`: KEEP the new rounded organic boulder pile (it was just remodelled from boxes on purpose; do not go back to slabs). Add richness: boulders in 4 different stone materials, a half-buried carved `marble` column drum with a `gold` band, a broken `glaze_teal` tiled step, gold vein, mixed `crystal` / `crystal_rose` clusters with `bronze` collars.
- `Wall_Colonnade`: back wall built of offset blocks in mixed stones with a `coral` / `ivory` mosaic frieze band and a `gold` line; fluted `marble` columns with carved `ivory` volute capitals, `gold` astragal rings and `glaze_teal` tiled square bases; the architrave carved with a relief of sun discs and a dentil row.
- `Wall_Arch`: arch made of alternating `sandstone` / `coral` voussoirs with a `gold` keystone holding a `gem_amber`, the recessed mirror framed by a stepped `gold` / `rose_gold` molding, `glaze_teal` tile spandrels, a relief frieze across the top, broken top edge in mixed stones.
- `Wall_MirrorFrame`: layered ornate oval `gold` frame (3 stepped profiles, `rose_gold` bead, filigree curls, `gem_rose` studs, sun-ray crest with `gem_amber`), wall of mixed stone blocks with an `ivory` molding and a `glaze_teal` tile dado band, `marble` ledge with carved brackets.
- `Wall`: offset blocks of 3 stone materials, chipped corners as grooves, a `glaze_teal` diamond tile band, `marble` cap with `gold` line and dentils.
- `Cliff_Island`: rim stones in mixed materials with small `gold` studs, mosaic `tile_cream` / `glaze_teal` floor fragments on top, crystal sprouts; the hanging underside in layered strata (`rock`, `sandstone_dark`, `rock_dark`) with dangling crystals.
- `Wall_Cloud`: clouds stay soft white / `cloud_shade`; make the upside-down house ornate: `coral` walls with `ivory` corner quoins, `roof_tile` + `gold` ridge, `window_lit` windows with `glaze_teal` shutters, a tiny upside-down `gold` weathervane; add 2 small upside-down lanterns.
