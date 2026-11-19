## Common rules (chapter 4 "Inside the Clockwork" = the clockmaker's giant clock tower)
Setting: a dreamlike CLOCK TOWER INTERIOR (top-down cel-shaded roguelite, camera ~55 deg looking down, objects read at small size).
Huge interlocking brass gears, rotating gear discs set flush in the floor, swinging pendulums, coiled mainsprings, old bronze bells, iron girders and riveted plates, clock faces, and through gaps in the machinery a starry NIGHT SKY. The player climbs from the bottom (spring storehouse) to the top (dial face). Motif: "interlocking / meshing". Mood: nocturnal, mechanical, quiet and grand.
Colours (the game recolors by material name): NOIR - hard black-and-white contrast (inky black, steel grey, bright silver / white highlights), almost monochrome; the only colour is a small RED accent (red lacquer, red gem eyes, red ribbon). Warm only for the merchant. Shape contrast matters: give every piece clearly separated dark and light parts.
Signature accent: GEAR TEETH, rivets, clock dials with Roman-numeral ticks (as raised bars, no text), spiral springs, pendulum discs. Chunky silhouettes, slightly hand-made irregularity (seeded random per piece, `random.seed(<fixed>)`). BEVEL modifier (0.02-0.05, 2 segments, angle limit, clamp overlap) on hard parts.

### Richness (the user wants high-end, not flat single-color objects)
- Every piece uses AT LEAST 4 role materials (walls / landmarks 6-8); no single material covers more than ~55% of the visible surface. Break big surfaces into bands, panels, insets and trims.
- Carve: stacked moldings, recessed riveted panels, gear-tooth rims, spoked wheels, relief clock dials, spiral grooves, chamfered edges everywhere. Cracks / seams as inset grooves in a darker material.
- No detail thinner than ~0.04 m, no micro noise. Budgets: props up to ~5000 tris, walls / landmarks / merchant up to ~8000.

### Materials - ALL GREY, named by ROLE
Every material is a flat Principled BSDF with base color (0.5, 0.5, 0.5), NO textures. The game recolors by material NAME, so use exactly these names (pick what you need, do not invent others):
`steel`, `steel_dark`, `iron`, `iron_dark`, `silver`, `brass`, `brass_dark`, `gold`, `copper`, `navy_paint`, `navy_dark`, `red_lacquer`, `red_dark`, `gem_red`, `wood`, `wood_dark`, `stone`, `stone_dark`, `dial_face` (pale ivory clock face), `dial_mark` (dark hour ticks / hands), `glass`, `starlight` (small glowing star bits), `night_sky` (deep starry backdrop panels), `porcelain`, `ribbon_red`, `velvet`, `lamp_warm`.

### Coordinates / export
Blender Z-up, +Y is the FRONT (the face the player sees / the room-facing side), origin at the bottom center of each piece (unless the piece says otherwise).
Each piece = ONE joined mesh object named exactly as listed (unless the piece says it has several objects), exported to its own FBX `Game/Assets/Art/Characters/Ch4/Kit_<Name>.fbx` with
`axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
Recalc normals outward. Clear the scene between pieces (or export selected only). No cameras/lights. Print `EXPORTED <Name> <tris> <dims>` per piece and `DONE` at the end. Create the folder `Game/Assets/Art/Characters/Ch4/` if missing.
Use `Art/tools/blender_kit_ch3_props.py` as a reference for setup / bevel / export style. Short Korean header comment at the top of the script (what it makes, how to run).
Finally render ONE preview PNG of all pieces laid out in a grid (Workbench engine, flat MATERIAL color with a distinct viewport color per material name so pieces are readable, camera ~55 deg from above-front, 1800x1000) to `Art/tools/specs/<script name without .py>_preview.png`. The preview is only for review; it must not change the exported FBX files.
