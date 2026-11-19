## Common rules (chapter 3 "Mirage Desert = Mirror World")
Setting: a dreamlike MIRROR WORLD beyond a desert mirage (top-down cel-shaded roguelite, camera ~55 deg looking down, objects read at small size).
The floor is a calm mirror lake that reflects a pink sunset sky; around it: rose sandstone rocks, ruined white marble colonnades with gold trim, gold-framed standing mirrors, glowing memory crystals, small fountains, floating mirror shards. Mood: serene, magical, sunset (peach / rose / cream / gold, teal only for water, white glow for crystals).
Signature accent: GOLD ornate trim and frames, broken marble, and pale glowing crystal. Chunky silhouettes, no tiny detail, slightly hand-made irregularity (seeded random per piece, `random.seed(<fixed>)`). BEVEL modifier (0.02-0.05, 2 segments) on stone / marble / gold parts.

### Materials - ALL GREY, named by ROLE
Every material is a flat Principled BSDF with base color (0.5, 0.5, 0.5), NO textures. The game recolors by material NAME, so use exactly these names (pick what you need, do not invent others):
`sandstone`, `sandstone_light`, `sandstone_dark`, `rock`, `rock_dark`, `sand`, `marble`, `marble_dark`, `gold`, `gold_dark`, `mirror`, `mirror_back`, `crystal`, `crystal_core`, `water`, `water_foam`, `cloud`, `cloud_shade`, `cloth_rose`, `stardust`, `wood`, `roof_tile`, `window_lit`.
`mirror` = the reflective glass face of every mirror. `mirror_back` = the dull back board of a mirror. `crystal` = pale crystal body, `crystal_core` = glowing inner crystal. `stardust` = tiny glowing floating bits. `water` = water surface, `water_foam` = ripples / splash.

### Coordinates / export
Blender Z-up, +Y is the FRONT (the face the player sees / the room-facing side), origin at the bottom center of each piece (unless the piece says otherwise).
Each piece = ONE joined mesh object named exactly as listed, exported to its own FBX `Game/Assets/Art/Characters/Ch3/Kit_<Name>.fbx` with
`axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
Clear the scene between pieces (or export selected only). No cameras/lights. Print `EXPORTED <Name> <tris> <dims>` per piece and `DONE` at the end.
Use `Art/tools/blender_kit_ch2_props.py` as a reference for setup / bevel / export style. Short Korean header comment at the top of the script (what it makes, how to run).
Finally render ONE preview PNG of all pieces laid out in a row (Workbench engine, flat MATERIAL color with a distinct viewport color per material name so pieces are readable, camera ~55 deg from above-front, 1600x900) to `Art/tools/specs/<script name without .py>_preview.png`. The preview is only for review; it must not change the exported FBX files.
