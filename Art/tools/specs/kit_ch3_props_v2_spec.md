Edit ONE existing Blender script: `Art/tools/blender_kit_ch3_props.py` (do not edit any other file). Keep every piece name, footprint, origin, function of the piece and the export paths the same; rebuild the geometry of EVERY piece much richer.
Read `Art/tools/specs/_ch3_common.md` and then `Art/tools/specs/_ch3_detail_v2.md` (the detail rules for this pass) first.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_props.py` and fix errors until it exports every piece and re-renders `Art/tools/specs/blender_kit_ch3_props_preview.png`.

Piece-specific ideas (on top of the general rules):
- `MemoryCrystal`: mixed `crystal` / `crystal_rose` prisms with `crystal_core` hearts, a carved octagonal `sandstone` pedestal with a `gold` rim and a ring of `glaze_teal` mosaic tiles, `bronze` collar around the crystal roots, floating `stardust` bits and a small floating `gold` ring orbiting at 1.5 m.
- `Fountain`: two-tier basin; outer basin `marble` with carved arches / scallops, `glaze_teal` tile band and `gold` rim; inner pedestal with fluting and `coral` + `ivory` mosaic diamonds; a small `gold` sun-disc ornament at the top, cartoon `water` jet arcs with `water_foam` splash crowns where they land.
- `StandMirror` / `MerchantMirror`: layered ornate `gold` frames (3 stepped profiles + `rose_gold` inner bead), filigree curls, `gem_rose` / `gem_amber` studs on the crest, carved `marble` / `ivory` plinth with moldings; MerchantMirror drapes `cloth_rose` with `gold` tassels and a `tile_cream` / `glaze_teal` checker floor step.
- `SandRock`: 3 boulders of different stone materials, carved half-buried ruin fragment (a piece of `marble` frieze with a sun relief) leaning on them, small crystal sprout.
- `FallenPillar`: fluted `marble` drums with `gold` bands and a carved `ivory` capital (volutes), a `glaze_teal` tile band, rubble of mixed stones.
- `MirrorShard`: keep it flat and thin (the game reflects bullets off its plane) but give it a richer broken `gold` frame with `rose_gold` beads and two `gem_rose` studs, and a few thin `crystal` edge chips.
- Ground bits: `Pebbles3` mixed stone colors + a tiny `glaze_teal` tile shard; `CrystalBits` mixed crystal / crystal_rose; `Ripple` unchanged.
