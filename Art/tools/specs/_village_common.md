## Common rules (main VILLAGE hub — "a western town vanishing at dawn")
Setting: the hub village of a top-down cel-shaded roguelite (camera ~55 deg looking down, objects read at small size, black outlines from the shader).
The village is ONE big floating island hanging in an endless void at the last moment before dawn: a starry night sky with a faint milky way above and below, and a warm peach-gold glow rising at the horizon.
The town is an old WESTERN frontier town (wooden false-front buildings, boardwalks, water tower, windpump, wagons, cactus) that is slowly VANISHING into time:
- the UPPER parts of buildings and objects dissolve upward: roof edges, chimneys, signs and fence tops break into many small detached fragments (little cubes, planks, shingles, gears, clock hands, sand clumps) that float frozen ABOVE the object, getting smaller and sparser the higher they go (3-5 rising "layers" of fragments).
- melting pocket-watch / clock motifs: clock faces sag and drip down walls in molten gold (`gold_drip`), cracked dials, frozen clock hands.
- glowing grains of sand (`sand_glow`) trail from the dissolving parts.
Mood: quiet, dreamlike, melancholic, beautiful. The objects themselves are WARM (weathered wood, brass, gold, cream, rust, rose-dawn paint) — no purple/blue in object materials (the cold colors belong to the sky / void only).

Signature accents: GOLD trim and filigree on signs and frames, brass clock parts, glowing lanterns and windows (`window_lit`, `lantern_glow`), floating fragments.
Chunky silhouettes, slightly hand-made irregularity (seeded random per piece, `random.seed(<fixed>)`). BEVEL modifier (0.02-0.05, 2 segments) on wood / stone / metal parts. No detail thinner than ~0.04 m.

### Richness rules (the user dislikes single-color blocky objects — "brown boxes")
- Every piece uses AT LEAST 5 different role materials (buildings and landmarks 6-8), and no single material may cover more than ~55% of the visible surface.
- Break big plain surfaces into: board-and-batten planks with alternating `wood` / `wood_dark` / `wood_light` / `wood_weathered` boards, painted trim bands, corner posts, window frames, shutters, cornices with brackets and dentils, false-front crests with `gold` scroll ornaments, rivets/studs, `iron` hinges and bands, hanging signs with carved frames.
- Clock motifs: brass bezels, `clock_face` dials with `iron` hands and `gold` numerals as small blocks, gears in `brass`.
- Triangle budget: props up to ~6000 tris, buildings / landmarks up to ~12000 tris, island edge modules up to ~8000.

### Materials - ALL GREY, named by ROLE
Every material is a flat Principled BSDF with base color (0.5, 0.5, 0.5), NO textures. The game recolors by material NAME, so use exactly these names (pick what you need, do not invent others):
`wood`, `wood_dark`, `wood_light`, `wood_weathered`, `plank`, `roof_shingle`, `roof_tin`, `adobe`, `adobe_light`, `stone`, `stone_dark`, `rock`, `rock_dark`, `earth`, `root`, `sand`, `sand_glow`, `brass`, `gold`, `gold_drip`, `iron`, `rust`, `canvas`, `cloth_red`, `cloth_cream`, `paint_rose`, `paint_sage`, `glass`, `window_lit`, `lantern_glow`, `clock_face`, `cactus`, `cactus_dark`, `stardust`.
`sand_glow` = glowing drifting sand grains / dissolving bits. `stardust` = tiny bright floating sparkles. `window_lit` = warm lit window glass. `lantern_glow` = lantern flame / bulb. `gold_drip` = molten melting gold of clocks. `earth` / `root` = soil strata and dangling roots under the island.

### Coordinates / export
Blender Z-up, +Y is the FRONT (the side facing the player / the plaza), origin at the bottom center of each piece (unless the piece says otherwise).
Each piece = ONE joined mesh object named exactly as listed, exported to its own FBX `Game/Assets/Art/Characters/Village/Kit_V_<Name>.fbx` with
`axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
Create the folder if missing. Clear the scene between pieces (or export selected only). No cameras/lights in the FBX. Print `EXPORTED <Name> <tris> <dims>` per piece and `DONE` at the end.
Use `Art/tools/blender_kit_ch3_landmarks.py` as a reference for setup / bevel / export style. Short Korean header comment at the top of the script (what it makes, how to run).
Run with `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P <script>` and fix errors until every piece exports.
Finally render ONE preview PNG of all pieces laid out in a row (Workbench engine, flat MATERIAL color with a distinct viewport color per material name so pieces are readable, camera ~55 deg from above-front, 1600x900) to `Art/tools/specs/<script name without .py>_preview.png`. The preview is only for review; it must not change the exported FBX files.
