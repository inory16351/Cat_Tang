You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_props.py` (create only this file; do not edit any other file — another job is writing `Art/tools/blender_kit_simple.py` at the same time, do not touch it).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_props.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.

## Goal
Low-detail, primitive-based ("shape-first") gameplay props for a top-down cel-shaded western game (chapter 1: a dusty old-west town slowly MELTING like a Dali clock; molten-gold drips are the signature accent). Seen from a high top-down camera at small size: chunky readable silhouettes, no tiny detail. Build from bevelled boxes, cylinders, cones, low-poly spheres; slightly irregular per part with a seeded random (hand-made feel). BEVEL modifier (width 0.015–0.03, 2 segments) on wooden parts.

## Colors (flat material base colors, NO textures)
Principled BSDF per color, exact names:
`wood_dark` (0.33,0.19,0.12), `wood` (0.55,0.33,0.19), `wood_light` (0.72,0.48,0.28), `iron` (0.24,0.22,0.25), `brass` (0.85,0.62,0.25), `gold_drip` (1.0,0.78,0.22), `barrel_red` (0.72,0.14,0.12), `fuse` (0.85,0.75,0.55), `spark` (1.0,0.9,0.35).
Join each piece into ONE mesh object (multiple materials allowed).

## Pieces (Blender Z-up, +Y = FRONT, origin at footprint center on ground z=0)
1. `Kit_Keg` — explosive powder keg 0.75 diameter × 1.0 tall: bulging barrel (lathe/cylinder with mid bulge) in barrel_red, 3 iron hoops, a big yellow lightning-bolt emblem (extruded flat zig-zag, brass/gold) on the front (+Y), a curved fuse (thin bent cylinder, fuse color) coming out of the top lid with a small star-shaped spark (spark) at its tip, one gold drip running down from the lid.
2. `Kit_Crates` — two stacked wooden crates: bottom 1.1 cube-ish (1.1×1.0×0.9), top 0.8 rotated ~12°, plank faces with an X brace on the front/side faces (wood/wood_light planks, wood_dark frame edges), small gold drip from the top crate.
3. `Kit_Fence` — wooden fence segment 2.4 wide × 1.0 tall: 3 thick posts with slightly broken/uneven tops, 2 horizontal rails (one a bit tilted), iron straps where rails meet posts.

## Export
- Each piece: select only it, export FBX to `Game/Assets/Art/Characters/Ch1/<Name>.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- Under ~3000 triangles each, normals outward. Print `EXPORTED <name> <tris>` per piece and `DONE` at the end.
- Short Korean header comment at the top (project comments are Korean).
