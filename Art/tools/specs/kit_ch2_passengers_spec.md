You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch2_passengers.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_passengers.py`
After writing it, RUN it once with that command and fix errors until all four FBX files export.
Use `Art/tools/blender_kit_woodgate.py` as a reference for the setup / materials / separate-piece export style (same conventions: pieces are NOT joined, each piece's origin at its own geometric center).

## Goal
Chapter 2 "The Stopped Station": time stopped a second before the train left. Frozen PASSENGERS stand around the rooms as translucent blue-grey glassy statues, frozen mid-motion.
When the next enemy wave comes, white cracks spread over some of them and they SHATTER, and an enemy steps out.
The game renders them with a translucent rim shader, so only the SILHOUETTE / POSE reads — make the poses big, clear and dynamic, readable from a high top-down camera (~55 deg). No faces, no fingers, no small detail.
Style: chunky stylized mannequin people (soft rounded primitives: UV spheres, capsules, tapered cylinders, rounded boxes), slightly cartoon proportions — head about 1/4.5 of total height, big hands/feet as simple blobs, long coats as tapered cones.

## Four passengers — each one its own FBX, origin at bottom center between the feet, facing +Y (front)
1. `Kit_Passenger_Luggage` (height 1.75): a man in a long coat and bowler hat, mid-stride hurrying forward (front leg forward, back heel lifted), torso leaning forward ~10 deg, RIGHT hand carrying a big SUITCASE low beside the leg (box 0.55 x 0.18 x 0.4 with a handle), left arm swinging forward.
2. `Kit_Passenger_Hat` (height 1.75): a woman in a long bell-shaped skirt and short cape, LEFT hand holding a wide-brimmed hat down on her head (elbow high out to the side), right arm thrown back for balance, head turned slightly to her right, skirt flaring backwards as if wind / sudden stop (skirt hem tilted back).
3. `Kit_Passenger_Child` (height 1.15): a small child running, frozen mid-jump: one foot lifted far back, the other on tip-toe, both arms out (one forward, one back), big round head with a newsboy cap, short coat, a little scarf end flying backwards stiffly.
4. `Kit_Passenger_Watch` (height 1.8): a tall gentleman in a top hat and tailcoat, standing still but leaning back slightly, LEFT hand holding an open POCKET WATCH up in front of his face (disc r 0.07 on a short chain), right hand holding a cane planted diagonally on the ground in front-right.

## Pieces (for shattering)
Every passenger is built from SEPARATE objects named `Piece_00`, `Piece_01`, … (two-digit), about 10–16 per passenger:
head (+ hat as its own piece), upper torso, lower torso / skirt / coat tail (split coats/skirts into 2–3 vertical chunks), upper arm, forearm+hand per arm, thigh, shin+foot per leg, and props (suitcase, hat, watch, cane, scarf) as their own pieces.
Pieces should overlap slightly at the joints so the statue looks solid (no gaps).
Body pieces use material `ghost`; hats, suitcase, cane, cap, scarf use `ghost_dark`.
Each piece's ORIGIN at its own geometric center (origin_set ORIGIN_GEOMETRY / bounds center). Flat shading is fine; smooth spheres OK.

## Crack lines
On each passenger add 6–9 thin CRACK objects named `Crack_00`, `Crack_01`, … in material `crack`:
each is a jagged zig-zag strip (4–7 segments, width 0.012–0.02, thickness 0.006) lying ON the outer surface of the body (front, sides and back, mostly torso, head and legs), branching like cracks in glass (some cracks fork into 2 short branches — branches can be their own Crack object).
Place them just proud of the surface (0.004 outward along the surface normal) so they sit on top. They will start hidden and appear one by one in the game.
Origin of each crack: its own center.

## Shard templates
3 small loose objects `Shard_0`, `Shard_1`, `Shard_2` in `ghost`: sharp irregular glass shards (thin triangular / quad wedges) 0.08–0.16 long, 0.01–0.02 thick. Put them INSIDE the upper torso volume (hidden) so they cannot be seen.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`ghost` (0.62, 0.7, 0.8), `ghost_dark` (0.45, 0.52, 0.62), `crack` (1.0, 1.0, 1.0).

## Export
- Clear the scene between passengers (or export only that passenger's objects). Recalc normals outward on every object.
- Export each passenger's objects (pieces + cracks + shards, separate meshes, flat hierarchy, no parent empty) to
  `Game/Assets/Art/Characters/Ch2/<Name>.fbx` (Name = `Kit_Passenger_Luggage` etc.) with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- Keep each passenger ≤ 5000 triangles. No cameras/lights.
- Print `EXPORTED <Name> <pieces> <cracks> <tris> <dims>` per passenger and `DONE` at the end.
- Also render a quick preview PNG of all four side by side (workbench engine, front 3/4 view from above, solid color by material) to `Art/tools/specs/kit_ch2_passengers_preview.png` so a human can check the poses.
- Short Korean header comment at the top of the script (what it makes, how to run).
