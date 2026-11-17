## Common rules (chapter 2 "The Stopped Station")
Setting: a western-era steam railway station where time STOPPED a second before departure (top-down cel-shaded roguelite, camera ~55 deg looking down, objects read at small size).
Signature accent: things frozen mid-motion (a puff of steam hanging still, a suitcase stuck in mid-air) and small brass clock/gear details. Chunky silhouettes, no tiny detail, slightly hand-made irregularity (seeded random per piece). BEVEL modifier (0.02-0.04, 2 segments) on wood/metal parts.

### Materials - ALL GREY, named by ROLE
Every material is a flat Principled BSDF with base color (0.5, 0.5, 0.5), NO textures. The game recolors by material NAME, so use exactly these names (pick what you need, do not invent others):
`wood`, `wood_dark`, `wood_light`, `iron`, `iron_dark`, `rust`, `brass`, `rail_steel`, `brick`, `brick_dark`, `stone`, `stone_dark`, `roof_tin`, `soot`, `coal`, `window`, `window_lit`, `glass_lamp`, `canvas`, `leather`, `leather_dark`, `cloth_red`, `paint_cream`, `paint_green`, `clock_face`, `steam`, `signal_red`, `signal_amber`, `signal_green`.
`steam` = frozen white puffs; `window_lit` / `glass_lamp` = warm glowing glass.

### Coordinates / export
Blender Z-up, +Y is the FRONT (the face the player sees / the room-facing side), origin at the bottom center of each piece.
Each piece = ONE joined mesh object named exactly as listed, exported to its own FBX `Game/Assets/Art/Characters/Ch2/Kit_<Name>.fbx` with
`axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
Clear the scene between pieces (or export selected only). No cameras/lights. Print `EXPORTED <Name> <tris> <dims>` per piece and `DONE` at the end.
Use `Art/tools/blender_kit_landmarks.py` as a reference for setup / bevel / export style. Short Korean header comment at the top of the script (what it makes, how to run).
