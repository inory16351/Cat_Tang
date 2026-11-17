You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_landmarks.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_landmarks.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.

## Goal
LARGE obstacle "landmark" props for a top-down cel-shaded western roguelite (chapter 1: a dusty old-west frontier slowly MELTING like a Dali clock — one or two small drips of molten gold per piece are the signature accent).
They are placed inside square combat rooms (28 × 24 m) to break up the rectangle and give cover, so each one must read instantly as a real, plausible object of that place, with a strong chunky silhouette from a high camera (~55° looking down). No tiny detail. Solid (players walk around, not through).
Build from bevelled boxes, cylinders, cones, low-poly displaced spheres (bmesh or bpy.ops). Hand-made irregularity with a seeded random per piece (planks slightly rotated/uneven, rocks faceted). BEVEL modifier (0.02–0.04, 2 segments) on wood/metal parts.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`wood_dark` (0.33, 0.19, 0.12), `wood` (0.55, 0.33, 0.19), `wood_light` (0.72, 0.48, 0.28), `iron` (0.24, 0.22, 0.25), `brass` (0.85, 0.62, 0.25), `gold_drip` (1.0, 0.78, 0.22),
`rock` (0.66, 0.34, 0.22), `rock_dark` (0.46, 0.22, 0.15), `rock_light` (0.8, 0.52, 0.34), `sand` (0.84, 0.64, 0.4), `hay` (0.9, 0.74, 0.35), `canvas` (0.85, 0.76, 0.6), `cloth_red` (0.7, 0.15, 0.13), `cloth_teal` (0.25, 0.5, 0.5), `bone` (0.92, 0.88, 0.78), `water` (0.3, 0.55, 0.65), `ore` (0.45, 0.4, 0.42)
Each piece = ONE mesh object (join its parts), several materials per part.

## Common layout
Blender Z-up, +Y = front, origin at the CENTER of the footprint on the ground (z = 0). Nothing may stick out of the listed footprint (X width × Y depth). Bottom flat on z = 0. Keep each piece under ~6000 triangles.

## Pieces (one FBX each) — footprint W × D, height H
Badlands canyon road (stage 1):
1. `Kit_LM_RockSpire` — 5.0 × 4.0, H 3.6: a cluster of 3 tall eroded sandstone spires/hoodoos of different heights (stacked, slightly offset faceted chunks, horizontal strata bands alternating `rock`/`rock_light`/`rock_dark`), a few loose boulders at the base, one gold drip down the tallest spire.
2. `Kit_LM_BoulderPile` — 4.0 × 3.5, H 2.2: 4–5 big rounded faceted boulders piled together (`rock`/`rock_dark`), a dry bush (thin `wood_dark` twigs) poking out, a cow skull (`bone`) leaning on it.
3. `Kit_LM_WagonWreck` — 5.0 × 3.5, H 2.2: a covered wagon tipped on its side: wooden bed, one broken wheel lying flat, one wheel still attached up in the air, torn `canvas` cover with exposed hoop ribs, 2 spilled barrels and a crate beside it.
4. `Kit_LM_MineCart` — 4.5 × 3.0, H 2.0: short section of mine rails on sleepers with an `iron`/`wood` mine cart full of `ore` lumps, a stack of timber props and a pickaxe leaning on the cart, an `iron` lantern on a post.
5. `Kit_LM_DeadTree` — 3.5 × 3.5, H 4.0: gnarled leafless dead tree (`wood_dark`, thick twisted trunk splitting into 4–5 crooked branches), a ring of rocks around the base, a frayed rope hanging from one branch, a gold drip from a branch tip.
Frontier town (stages 2–4):
6. `Kit_LM_WaterTower` — 4.0 × 4.0, H 4.6: wooden water tank (vertical staves `wood`, 3 `iron` hoops, conical shingle roof `wood_dark`) on 4 splayed legs with X cross-braces, a ladder, a spout pipe; a small puddle (`water`, flat) under the spout inside the footprint.
7. `Kit_LM_Stagecoach` — 5.0 × 2.6, H 2.6: a stagecoach body (`cloth_red` painted panels with `brass` trim, `wood_dark` frame), 4 big spoked wheels (`wood_light` rims, `iron` tires), luggage trunks and a `canvas` bundle tied on the roof, driver's bench at the front. No horses.
8. `Kit_LM_Well` — 3.2 × 3.2, H 2.9: round stone well (`rock_light` stones, `rock_dark` rim) with two wooden posts, a small gabled shingle roof, a crank with rope and a bucket; a wooden trough beside it.
9. `Kit_LM_HayCart` — 5.0 × 3.4, H 2.4: a flatbed farm cart heaped with `hay` (lumpy mound), a pitchfork stuck in it, plus 3 square hay bales stacked beside the cart (two on the ground, one on top).
10. `Kit_LM_MarketStall` — 4.5 × 3.0, H 2.7: a market stall: wooden counter with crates of produce (small `cloth_red`/`hay`/`cloth_teal` lumps), striped canopy (`canvas`/`cloth_teal` alternating) on 4 posts, a hanging sign board (`wood_light`).
11. `Kit_LM_LumberPile` — 4.5 × 2.6, H 1.7: neat stack of sawn planks and logs (log ends visible, `wood_light` cut faces), a sawhorse with a saw, a few loose planks leaning against the stack.

## Export
- Each piece: select only it, export FBX to `Game/Assets/Art/Characters/Ch1/<Name>.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- Recalc normals outward. No cameras/lights. Print `EXPORTED <name> <tris> <dims>` per piece and verify X/Y dims ≤ the footprint (+0.02); `DONE` at the end.
- Short Korean header comment at the top of the script (what it makes, how to run).
