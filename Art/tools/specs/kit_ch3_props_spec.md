You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch3_props.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_props.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch3_common.md` first: it has the material names, coordinate and export rules you must follow.

## Goal
Gameplay props, cover and interactive objects for combat rooms (28 x 24 m) of the mirror world. The interactive ones are shot by the player, so they need a big, simple, instantly readable silhouette from a high camera.

## Pieces (footprint W x D x H in m)
1. `MemoryCrystal` (1.1 x 1.1 x 1.7) EXPLOSIVE: a cluster of 5 hexagonal crystal prisms with pointed tips (one tall central 1.6 m, four shorter ones leaning outward 20-35 deg) growing out of a small lumpy `rock` base (0.3 m high); prisms are `crystal`, and a thinner prism inside the tallest one pokes out slightly as `crystal_core`; 4 tiny `stardust` octahedra float around it at 1.2-1.8 m (separate floating islands of the same mesh, not touching).
2. `Fountain` (2.2 x 2.2 x 1.9): small round fountain: `marble` basin (outer diameter 2.2, rim 0.55 high, rim 0.2 thick) with a `gold` band around the outside of the rim, `water` surface disc inside at 0.45 m, two `water_foam` ripple rings on the water, a central `marble_dark` pedestal with a small bowl at 1.2 m, and a stylized frozen-looking `water` jet: a fat column rising from the bowl to 1.9 m, curling over into 4 drooping arcs that fall back into the basin (chunky tubes, cartoon water).
3. `StandMirror` (1.5 x 0.6 x 2.6): free-standing tall ARCHED mirror (cover; a monster also jumps out of it): ornate `gold` frame 0.12 thick with a round medallion crest on top and small scroll curls at the shoulders, `mirror` glass panel facing +Y set 0.03 behind the frame front, `mirror_back` board on the back (-Y), standing on a low `sandstone` plinth 1.5 x 0.6 x 0.25 with two `gold_dark` claw feet.
4. `MerchantMirror` (2.8 x 1.2 x 3.6): the merchant's grand standing mirror that rises from the floor: tall arched `mirror` panel 1.9 wide x 2.8 high facing +Y in a thick ornate `gold` frame, a `marble` column on each side with `gold` capitals, `cloth_rose` drapes hanging in swags across the top and down both sides, a round `gold` crest at the top with a small sun-ray fan, a 0.35 m `marble` step platform in front (depth 1.2) so a figure can stand on it in front of the glass.
5. `SandRock` (1.8 x 1.4 x 1.3) cover: 2-3 chunky faceted rose `sandstone` boulders leaning together, `sandstone_dark` on the lower faces, `sandstone_light` top facets, a `sand` drift wedge piled at the base.
6. `FallenPillar` (2.6 x 1.0 x 1.0) cover: a toppled white `marble` column broken into 2 drums lying on their side with a gap and slight angle, fluting grooves (6-8 flat facets), a `gold` band ring on each drum, the square capital block lying at one end, 3-4 `marble_dark` rubble chunks.
7. `MirrorShard` (2.5 x 0.15 x 3.05, origin at FLOOR center, the panel FLOATS): a thin irregular mirror panel occupying X -1.25..1.25 and Z 0.6..3.0 (nothing below Z 0.6): its outline is a slightly irregular 6-8 sided polygon (mostly a tall rectangle with one corner broken off diagonally and a jagged top edge), glass 0.05 thick with `mirror` on BOTH faces (+Y and -Y), and a broken ornate `gold` frame strip (0.08 x 0.1 profile) running only along the bottom edge and up one side, ending in a little curl. Keep it flat and thin; the game spins it around Z and reflects bullets off its plane.
8. `Pebbles3` (1.2 x 1.2 x 0.15) ground bit, no collision: 6-9 small flat rose `sandstone` / `rock` pebbles scattered.
9. `CrystalBits` (1.0 x 1.0 x 0.35) ground bit: 4-6 small `crystal` shards sticking out of the floor at angles, one tiny `crystal_core`.
10. `Ripple` (1.6 x 1.6 x 0.02) ground bit: 2-3 concentric thin flat `water_foam` rings (width 0.04) lying at Z 0.005-0.02, like a ripple on the mirror lake.

Keep each under ~2500 triangles (MerchantMirror ~4000).
