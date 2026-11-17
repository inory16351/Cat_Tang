You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ground.py` (create only this file; do not edit any other file).
Run it headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ground.py` and fix errors until every piece exports.
Use `Art/tools/blender_kit_simple.py` (already in the repo) as the reference for style, materials, export settings and helper structure — read it first and match it.

## Goal
Flat GROUND DETAIL pieces scattered on a seamless dusty orange dirt floor (chapter 1 of a top-down cel-shaded western game that is slowly melting) to break up visual repetition. Seen from a high top-down camera: chunky readable shapes, very low height (they lie on the ground), no tiny detail, slightly irregular with a seeded random.

## Colors (flat material base colors, no textures; same names/values as blender_kit_simple.py where they overlap)
`wood_dark` (0.33,0.19,0.12), `wood` (0.55,0.33,0.19), `wood_light` (0.72,0.48,0.28), `rock` (0.62,0.32,0.22), `rock_dark` (0.45,0.22,0.16), `sand_light` (0.93,0.7,0.45), `grass_dry` (0.85,0.68,0.32), `grass_dark` (0.62,0.46,0.2), `iron` (0.24,0.22,0.25).

## Pieces (Blender Z-up, origin = footprint center at z=0, everything within 0..0.35 height unless noted)
1. `Kit_GroundPlanks` — 3 weathered boards lying on the ground, overlapping at different angles, one broken shorter; 2.2 × 1.2 footprint, 0.08 thick, a couple of iron nails.
2. `Kit_Pebbles` — cluster of 8–12 flattened low-poly stones of varied size (rock / rock_dark), ~1.4 footprint, max height 0.25.
3. `Kit_DryGrass` — 3–4 tufts of dry grass blades (thin flattened cones fanning out), grass_dry/grass_dark, ~1.0 footprint, 0.45 tall.
4. `Kit_SandDrift` — a soft low sand mound / ripple patch (displaced flattened disc, wavy outline), sand_light, ~2.4 × 1.6, max height 0.12.
5. `Kit_Skull` — a small cartoon cattle skull with two curved horns lying on the ground (bone white (0.92,0.86,0.74) and wood_dark eye holes), ~0.8 wide, 0.3 tall.

## Export
Same as blender_kit_simple.py: one FBX each to `Game/Assets/Art/Characters/Ch1/<Name>.fbx`, `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`. Under ~1500 triangles each, normals outward. Print `EXPORTED <name> <tris>` and `DONE`. Korean header comment.
