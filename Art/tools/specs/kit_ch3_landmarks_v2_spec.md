Edit ONE existing Blender script: `Art/tools/blender_kit_ch3_landmarks.py` (do not edit any other file). Keep every piece name, footprint, origin and export path the same; rebuild the geometry of EVERY piece much richer.
Read `Art/tools/specs/_ch3_common.md` and then `Art/tools/specs/_ch3_detail_v2.md` (the detail rules for this pass) first.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_landmarks.py` and fix errors until it exports every piece and re-renders `Art/tools/specs/blender_kit_ch3_landmarks_preview.png`.

Piece-specific ideas (on top of the general rules):
- `LM_ArchRuin`: arches made of alternating `sandstone` / `coral` voussoir blocks with a `gold` keystone holding a `gem_amber`; fluted `marble` columns with carved `ivory` capitals and `gold` astragal rings; a relief frieze band of sun discs above; `glaze_teal` mosaic on the column bases.
- `LM_CrystalSpire`: mixed `crystal` / `crystal_rose` prisms, `crystal_core` hearts, a carved stone ring (`sandstone` with `gold` rim and `glaze_teal` tile inlays) half buried around the base.
- `LM_Hourglass`: ornate `gold` discs with stepped moldings and sun-ray edges, twisted `bronze` / `rose_gold` posts, `gem_rose` studs, the cracked crystal bulb with `crystal_core` sand glow, a `sand` mound with a carved `marble` plaque.
- `LM_FloatRock`: island top with a mosaic `tile_cream` / `glaze_teal` floor patch, tiny ruined columns with `gold` capitals, mixed crystals; layered rock strata (`rock`, `sandstone_dark`, `rock_dark`) on the hanging cone.
- `LM_MirrorGate`: gold ring with layered profile, sun-ray spikes alternating `gold` / `rose_gold`, `gem_rose` studs, the plinth carved with steps of `marble`, `ivory` molding and a `glaze_teal` tile riser band, gold lantern finials with `window_lit` glow.
- `LM_Obelisk`: shaft with carved relief bands (crescent moons, stars) in `sandstone_light`, a `gold` clock face with `ivory` dial and `gem_amber` center, `glaze_teal` inlaid hieroglyph-like tiles, stepped `marble` base with `gold` lines.
- `LM_Dune`: rename nothing, but make it less "plain sand": add a half-buried carved column capital (`ivory` + `gold`), a broken `glaze_teal` tiled floor slab sticking out, the gold mirror frame with gems, crystal sprouts; sand stays only on the mound itself.
