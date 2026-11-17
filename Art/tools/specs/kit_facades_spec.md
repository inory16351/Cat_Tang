You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_facades.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_facades.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.

## Goal
Procedural-wall building blocks for a top-down cel-shaded western roguelite (chapter 1: a dusty old-west town slowly MELTING like a Dali clock — a few drips of molten gold are the signature accent).
The game assembles room walls from these facades at random (different widths, heights, setbacks, alleys between them) so a row of buildings must NOT look like one repeated tile. Each facade must therefore have a clearly different SILHOUETTE (roof line / parapet shape / height), a different PAINT colour and a different ROOF colour.
The camera is high and looks down at ~55°, so the ROOF / TOP and the upper half of the front are what is seen most: make roofs readable (planks, tin ridges, parapet caps, chimneys, water barrel, etc.) and keep everything chunky — no tiny detail.
Build from bevelled boxes, cylinders, cones (bmesh or bpy.ops primitives). Slightly irregular: rotate/scale each plank/board a little with a seeded random (seed per piece) so it looks hand-made, not CAD-perfect; a few boards may be a bit crooked. Add a BEVEL modifier (width 0.02–0.03, 2 segments) on wooden/painted parts so the toon shader catches edges.

## Colors (flat material base colors, NO textures, NO image nodes)
One Principled BSDF per colour with `Base Color` set, materials named exactly:
- `wood_dark` (0.33, 0.19, 0.12), `wood` (0.55, 0.33, 0.19), `wood_light` (0.72, 0.48, 0.28)
- `paint_teal` (0.24, 0.48, 0.47), `paint_cream` (0.88, 0.8, 0.62), `paint_mustard` (0.82, 0.6, 0.2), `paint_red` (0.62, 0.2, 0.15), `paint_blue` (0.3, 0.38, 0.55)
- `adobe` (0.82, 0.6, 0.42), `brick` (0.58, 0.28, 0.2), `stone` (0.55, 0.5, 0.46)
- `roof_tin` (0.46, 0.48, 0.52), `roof_red` (0.5, 0.2, 0.14), `roof_shingle` (0.36, 0.25, 0.2)
- `window` (0.12, 0.13, 0.2), `window_lit` (0.98, 0.78, 0.4), `iron` (0.24, 0.22, 0.25), `brass` (0.85, 0.62, 0.25), `gold_drip` (1.0, 0.78, 0.22)
- `cloth_red` (0.7, 0.15, 0.13), `canvas` (0.85, 0.76, 0.6)
Each piece = ONE mesh object (join its parts), several materials assigned per part.

## Common layout (every facade)
- Blender Z-up, **+Y = FRONT** (the side facing the street/room), origin at the center of the footprint on the ground (z = 0).
- Footprint: exact WIDTH along X (listed per piece; nothing may stick out beyond ±width/2), DEPTH 3.0 along Y (y from −1.5 back to +1.5 front). The main building body spans y −1.5 … +1.1; porches/awnings/steps may come forward up to y = +1.5.
- The back and sides are plain (seen rarely) but must be closed (no holes). The roof must cover the whole body.
- Doors are closed (no interior). Windows are dark `window` panes inset 0.05 with a wooden frame; 1–2 panes per piece may be `window_lit`.
- Add exactly ONE small `gold_drip` accent per piece (a drip running down from a parapet edge, sign or roof corner: elongated teardrop/cylinder + blob).

## Pieces (one FBX each)
1. `Kit_Facade_Store` — width 5.0, height 3.4: one-storey general store with a tall STEPPED false-front parapet (three steps, highest in the middle), `paint_teal` boards, big blank sign board (`wood_light` with `wood_dark` border) across the parapet, wide shop window, door, a striped awning (`canvas`/`cloth_red` alternate stripes) sloping forward to y = +1.5 at height 2.2. Roof behind the parapet: `roof_tin` with ridges.
2. `Kit_Facade_Hotel` — width 6.0, height 4.4: two-storey hotel, `paint_cream`, flat parapet with a small triangular pediment in the middle, 4 upper windows, door + 2 windows below, a front porch roof on 4 thin `wood_dark` posts (posts at y ≈ +1.4) that doubles as an upper balcony with a low railing. Roof `roof_shingle`, one brick chimney.
3. `Kit_Facade_Narrow` — width 3.0, height 4.0: narrow two-storey house, `paint_mustard`, a pointed GABLE front (triangle top), one door, one upper window, a small hanging sign on an iron bracket. Roof `roof_red` pitched (ridge front-to-back).
4. `Kit_Facade_Adobe` — width 4.0, height 2.7: adobe house, `adobe` walls with softened/rounded edges (bigger bevel 0.08), flat roof with a low parapet, 4 protruding log beam ends (vigas, `wood_dark` cylinders) poking out of the front just under the roof, small deep window, wooden door, a `cloth_red` rug hanging from the parapet. Roof top `adobe` slightly darker? → use `stone` for the roof surface.
5. `Kit_Facade_Shack` — width 3.0, height 2.5: weathered plank shack (`wood`/`wood_dark` planks, crooked), single-slope lean-to roof (high at back, low at front), `roof_tin` with 2 patches in `iron`, boarded-up window (two crossed planks), a barrel beside the door (inside the footprint).
6. `Kit_Facade_Bank` — width 4.0, height 3.3: solid `brick` bank with `stone` corner pillars, a flat cornice band, an ARCHED door with `brass` frame, two barred windows (`iron` bars), a blank `brass` name band above the door. Roof `roof_tin` flat with a low stone parapet cap.
7. `Kit_Facade_Barn` — width 5.0, height 3.8: `paint_red` barn front with a GAMBREL roof outline (two slopes each side), white-ish (`paint_cream`) trim and an X brace on the big double door, a hay loft door up top with a small pulley beam sticking out. Roof `roof_shingle`.
8. `Kit_Facade_Blue` — width 4.0, height 3.0: one-storey barber/telegraph office, `paint_blue` boards, CURVED (arched) false-front parapet top, a small porch roof (`roof_tin`) on 2 posts, one window + door, a striped pole (cylinder `cloth_red`/`paint_cream` bands) at one front corner inside the footprint.
9. `Kit_AlleyFence` — width 2.0, height 2.0, depth 0.4 (y −0.2…+0.2): tall crooked plank fence to close a narrow alley between buildings (`wood`/`wood_dark`), with one board missing.

## Export
- For each piece: select only it, export FBX to `Game/Assets/Art/Characters/Ch1/<Name>.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- Keep each piece under ~5000 triangles. Recalculate normals outward. No cameras/lights in the export.
- Print `EXPORTED <name> <tris> <dims x y z>` for each and `DONE` at the end; verify each piece's X dimension equals its listed width within 0.02 m and its Y dimension ≤ 3.0 (fence 0.4).
- Put a short Korean header comment at the top of the script describing what it makes and how to run it (the project writes comments in Korean).
