You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_sandbomb.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_sandbomb.py`
After writing it, RUN it once with that command and fix errors until it exports.

## Goal
The thrown "sand bomb" of the HOURGLASS BOMBER enemy in a top-down cel-shaded western roguelite (chapter 1: dusty old-west town melting like a Dali clock; gold drips are the accent).
The bomb is lobbed in a high arc and spins in the air, seen from a high camera at small size → chunky, readable silhouette, no tiny detail.
Build from bevelled primitives (bmesh or bpy.ops). Slightly hand-made irregularity (seeded random). BEVEL modifier (0.01–0.02, 2 segments) on hard parts.

## Design: `Kit_SandBomb` (one mesh object, ~0.5 m tall overall, origin at the CENTER of the bomb body, Blender Z-up)
- Body: a round bomb (sphere r ≈ 0.18) wrapped in a lumpy `canvas` sand sack: the sack is a slightly squashed sphere (r ≈ 0.2) with 4–5 soft bulges and a gathered neck at the top, tied with a `wood_dark` rope band (torus) around the neck, plus one crossing rope band around the belly.
- On top of the neck: a small `brass` HOURGLASS cap (two short cones tip-to-tip, total ~0.12 tall, with thin brass discs top and bottom) — the bomber's signature. Inside the hourglass bulbs a tiny `sand` fill visible (inner smaller cones in `sand`).
- From the top of the hourglass: a short curly `iron` fuse (curved cylinder, ~0.12 long) ending in a SPARK: a small star of 5–6 sharp `spark` spikes (cones) around a tiny sphere.
- A single `gold_drip` drip running down one side of the sack from the rope band (elongated teardrop + blob).
- Two `sand` dribbles leaking from a small tear at the bottom of the sack (little cone/cylinder trickles).
- Keep under ~3000 triangles.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`canvas` (0.85, 0.76, 0.6), `sand` (0.9, 0.72, 0.45), `wood_dark` (0.33, 0.19, 0.12), `brass` (0.85, 0.62, 0.25), `iron` (0.24, 0.22, 0.25), `gold_drip` (1.0, 0.78, 0.22), `spark` (1.0, 0.55, 0.15) — for `spark` also set Emission Color = same and Emission Strength 3.

## Export
- Join into ONE mesh object named `Kit_SandBomb`, recalc normals outward, export FBX to `Game/Assets/Art/Characters/Ch1/Kit_SandBomb.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- No cameras/lights. Print `EXPORTED Kit_SandBomb <tris> <dims>` and `DONE`.
- Short Korean header comment at the top of the script (what it makes, how to run).
