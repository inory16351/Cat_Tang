You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_simple.py` (create the folder file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_simple.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.

## Goal
Low-detail, primitive-based ("shape-first") modular map pieces for a top-down cel-shaded western game (chapter 1: a dusty old-west town that is slowly MELTING like a Dali clock — a few drips of molten gold are the signature accent). Pieces are seen from a high top-down camera at small size, so silhouettes must be chunky and readable; no tiny detail. Build everything from bevelled boxes, cylinders, cones, low-poly spheres (bmesh or bpy.ops primitives), slightly irregular (rotate/scale each plank/stone a little with a seeded random so it is hand-made, not CAD-perfect). Add a BEVEL modifier (width 0.015–0.03, 2 segments) on wooden parts so the toon shader catches edges.

## Colors (flat material base colors, NO textures, NO image nodes)
Use a Principled BSDF per color with `Base Color` set; name the materials exactly:
- `wood_dark` (0.33, 0.19, 0.12), `wood` (0.55, 0.33, 0.19), `wood_light` (0.72, 0.48, 0.28)
- `iron` (0.24, 0.22, 0.25), `brass` (0.85, 0.62, 0.25), `gold_drip` (1.0, 0.78, 0.22)
- `sand` (0.82, 0.62, 0.4), `rock` (0.62, 0.32, 0.22), `rock_dark` (0.45, 0.22, 0.16)
- `cloth_red` (0.7, 0.15, 0.13), `canvas` (0.85, 0.76, 0.6), `water` (0.3, 0.55, 0.65)
Each mesh object may use several of these materials (assign per face/part, then join parts into ONE object per piece).

## Pieces (one FBX each; sizes in meters; Blender Z-up, +Y = the piece's FRONT, origin at the center of the footprint on the ground z=0)
1. `Kit_PlankWall` — interior wooden plank wall segment, 4.0 wide × 0.3 deep × 2.6 tall: vertical planks (wood/wood_light alternating, slightly uneven tops), 2 horizontal dark beams, 3 iron nail plates, one small gold drip running down from the top beam.
2. `Kit_Boardwalk` — raised wooden sidewalk tile 4.0 × 2.0, 0.25 tall: planks across the short side with small gaps, 4 short dark posts underneath at the front edge.
3. `Kit_Sandbags` — low cover: a curved row of 7 pillow-shaped sandbags (flattened rounded boxes, canvas/sand) two layers, 2.2 wide × 0.8 deep × 0.9 tall.
4. `Kit_BarrelStack` — 3 wooden barrels (cylinders with 2 iron hoops each), two standing + one lying on top, footprint ~1.6 × 1.0, 1.6 tall.
5. `Kit_HitchPost` — horse hitching rail: two posts + one horizontal rail, 2.0 wide, 1.1 tall, wood_dark, an iron ring on the rail.
6. `Kit_Trough` — water trough: wooden box 1.6 × 0.6 × 0.6 on 4 small legs, water surface inset 0.08 below the rim.
7. `Kit_Signpost` — signpost 2.4 tall with two arrow-shaped boards pointing different ways (wood_light, dark edge), a little gold drip from one board.
8. `Kit_LampPost` — street lamp: iron post 3.0 tall, brass lantern box on top with a cone cap.
9. `Kit_Rock_0`, `Kit_Rock_1`, `Kit_Rock_2` — desert boulders 1.0–1.8 wide: low-poly displaced spheres/boxes (rock + rock_dark facets), flat bottoms, different shapes.
10. `Kit_Railing` — arena fence/railing segment 4.0 wide × 1.0 tall: 5 posts + 2 rails, brass caps on the posts.
11. `Kit_SaloonDoor` — saloon swing doors in a frame: frame 1.6 wide × 2.4 tall, two half-height swing doors (wood_light) in the middle.

## Export
- For each piece: select only it, apply modifiers on export, export FBX to `Game/Assets/Art/Characters/Ch1/<Name>.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- Keep each piece under ~3000 triangles. Recalculate normals outward. No cameras/lights in the export.
- Print `EXPORTED <name> <tris>` for each and `DONE` at the end.
- Put a short Korean header comment at the top of the script describing what it makes and how to run it (the project writes comments in Korean).
