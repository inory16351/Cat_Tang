You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_heart.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_heart.py`
After writing it, RUN it once with that command and fix errors until it exports.
Use `Art/tools/blender_kit_sandbomb.py` as a reference for the setup / materials / bevel / FBX-export style (same conventions).

## Goal
A HEALTH PICKUP heart that sometimes drops from killed enemies (and is sold by the merchant) in a top-down cel-shaded western roguelite
(chapter 1: dusty old-west town melting like a Dali clock; gold drips are the accent; the hero is a gunslinger cat).
It floats and spins slowly above the ground, seen from a high camera at small size → a fat, instantly readable heart silhouette, chunky, toy-like, no tiny detail.

## Design: `Kit_Heart` (one mesh object, about 0.6 m wide, origin at the CENTER of the heart, Blender Z-up, heart stands upright in the XZ plane, thickness along Y)
- Body: a PUFFY rounded heart (like an inflated cushion / candy): two lobes + pointed bottom, thickness ~0.28 at the center tapering to soft rounded edges. Build it smoothly (e.g. heart outline extruded then subdivided/smoothed, or two spheres + a cone blended with a remesh/smooth). Smooth shading on the body. Material `heart_red`.
- A glossy highlight: a small flattened `heart_light` blob on the upper-left lobe, on BOTH faces (it spins, so both sides must read).
- A thin `gold` rim band around the heart's outline (a slightly bigger, very thin ring following the silhouette at the middle of the thickness), like a brass setting.
- One `gold` drip running down from the rim at the right lobe (elongated teardrop + blob), the chapter's melting-gold accent.
- A small `cream` plus sign (+) embossed on the center of BOTH faces (two crossing bevelled bars, 0.1 × 0.03, protruding 0.015).
- Keep under ~2500 triangles.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`heart_red` (0.92, 0.18, 0.22), `heart_light` (1.0, 0.62, 0.62), `gold` (1.0, 0.78, 0.22), `cream` (1.0, 0.95, 0.85).
For `heart_red` also set Emission Color = same and Emission Strength 0.4 (it should glow a little).

## Export
- Join into ONE mesh object named `Kit_Heart`, recalc normals outward, export FBX to `Game/Assets/Art/Characters/Ch1/Kit_Heart.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- No cameras/lights. Print `EXPORTED Kit_Heart <tris> <dims>` and `DONE`.
- Short Korean header comment at the top of the script (what it makes, how to run).
