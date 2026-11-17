You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_xpgem.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_xpgem.py`
After writing it, RUN it once with that command and fix errors until it exports.
Use `Art/tools/blender_kit_heart.py` as a reference for the setup / materials / bevel / FBX-export style (same conventions).

## Goal
An EXPERIENCE GEM that pops out of every killed enemy in a top-down cel-shaded roguelite (time-themed: clocks, brass, gears; the hero is a gunslinger cat).
Many of them fly around at once, spinning, seen from a high camera at very small size (about 0.35 m tall in game) → a crisp, instantly readable crystal silhouette, chunky facets, no tiny detail.

## Design: `Kit_XpGem` (one mesh object, about 0.35 m tall, origin at the CENTER of the gem, Blender Z-up, long axis along Z)
- Body: an elongated faceted crystal shard — a hexagonal bipyramid (6 sides), top point taller than bottom point (about 60/40), with a slightly wider middle belt (a short hexagonal prism band between the two pyramids). FLAT shading (hard facets) so the cel shader shows clear light/dark faces. Material `gem_teal`.
- Inner glow core: a smaller copy of the bipyramid (about 55% size) inside, material `gem_core` — it shows through nothing (the outer body is opaque), so instead make the outer body's belt band a separate ring of faces with material `gem_light` (a lighter teal stripe around the middle) so it reads as a gem with a bright band.
- A tiny `gold` clock-hand mark on BOTH broad faces of the belt: a short thin bar (minute hand) and a shorter bar (hour hand) meeting at a small round dot, protruding ~0.004, like an engraved clock hand. Keep it very simple (3 small parts per side).
- Two small `gold` caps: a thin hexagonal ring at the top and bottom of the belt band (brass setting), very thin.
- Keep under ~800 triangles.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`gem_teal` (0.25, 0.85, 0.95), `gem_light` (0.7, 1.0, 1.0), `gem_core` (0.9, 1.0, 1.0), `gold` (1.0, 0.78, 0.22).
For `gem_teal` and `gem_light` also set Emission Color = same and Emission Strength 0.5 (it should glow a little).

## Export
- Join into ONE mesh object named `Kit_XpGem`, recalc normals outward, export FBX to `Game/Assets/Art/Characters/Ch1/Kit_XpGem.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- No cameras/lights. Print `EXPORTED Kit_XpGem <tris> <dims>` and `DONE`.
- Short Korean header comment at the top of the script (what it makes, how to run).
