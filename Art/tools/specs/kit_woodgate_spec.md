You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_woodgate.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_woodgate.py`
After writing it, RUN it once with that command and fix errors until it exports.
Use `Art/tools/blender_kit_sandbomb.py` as a reference for the setup / materials / bevel / FBX-export style (same conventions).

## Goal
A PRE-FRACTURED heavy wooden barricade door that blocks the doorway between rooms in a top-down cel-shaded western roguelite
(chapter 1: dusty old-west town melting like a Dali clock; gold drips are the accent). It slams shut when a fight starts
and BURSTS INTO PIECES when the room is cleared. The game flings every piece separately, so the door must be built from
many SEPARATE mesh objects that together look like one intact door (no gaps visible when assembled).
Seen from a high camera → chunky, readable planks, no tiny detail. Slightly hand-made irregularity (seeded random, `random.Random(7731)`).
BEVEL modifier (width 0.012, 2 segments, angle limit, clamp overlap) on wood and iron parts.

## Dimensions (Blender Z-up, meters). Door plane = XZ plane, thickness along Y.
- Overall: width 4.6 (X from -2.3 to +2.3), height 2.6 (Z from 0 to 2.6), origin at bottom-center (0,0,0).
- Centered on Y=0; total thickness with rails and straps ≈ 0.32. Front = +Y.

## Construction (each listed piece = its own object)
1. **Vertical planks** (`wood` / `wood_light` alternating randomly, ~8 planks, widths 0.52–0.62 that sum exactly to 4.6, thickness 0.14 at Y∈[-0.07,0.07]).
   Plank tops are uneven: each plank height 2.45–2.6, top end cut either flat, slanted, or a shallow point.
   **Each plank is split into 2 or 3 chunks** along JAGGED SPLINTERED break lines (zig-zag cut across the plank with 3–5 teeth, teeth depth 0.06–0.14, break heights random between 0.6 and 2.0).
   The two chunks share exactly the same zig-zag edge so they fit together perfectly. Build each chunk as an extruded 2D polygon (outline in XZ, extruded in Y) — bmesh, not booleans.
   Keep planks clean (no grooves); give each chunk 0–2 small knot discs (`wood_dark`, flat cylinder r 0.04–0.06, 0.01 proud of the front face) parented by joining into that chunk.
2. **Horizontal rails** on the FRONT (+Y) face: two `wood_dark` rails (height 0.22, thickness 0.08, sitting on Y∈[0.07,0.15]) at Z≈0.55 and Z≈2.0, spanning the full width. Each rail is split into 3 pieces with jagged ends (same zig-zag-in-plan idea, 2–3 teeth).
3. **Diagonal Z brace** on the front between the two rails: one `wood_dark` board from lower-left to upper-right, split into 2 pieces at a jagged break.
4. **Iron straps** on the BACK (−Y) face: two `iron` bands (height 0.14, thickness 0.03) at Z≈0.8 and Z≈1.8, full width, each split into 3 segments (straight cuts are fine, iron bends not breaks). Big round `iron` rivet heads (flattened spheres r 0.035) every ~0.55 along each strap on the back AND matching rivets on the front rails, joined into the piece they sit on.
5. **Gold drip accent**: one `gold_drip` drip running down from the upper rail over a plank (elongated teardrop + blob), joined into that rail piece.
6. **Splinter templates** (for extra debris): 3 small loose objects named `Splinter_0`, `Splinter_1`, `Splinter_2` in `wood_light`/`wood` — thin jagged sticks 0.25–0.45 long, 0.04–0.07 wide, 0.03 thick (pointy on both ends). Put them INSIDE the door volume at the center (X≈0, Z≈1.3, Y=0) so they are hidden inside the planks.

## Naming
- Every door piece object: `Piece_00`, `Piece_01`, … (two-digit, any order). Splinters as above. No other objects.
- Each piece's ORIGIN must be at its own geometric center (origin_set ORIGIN_GEOMETRY / bounds center) so the game can spin it around its center.
- Do NOT join pieces together. Total triangles ≤ 6000.

## Colors (flat Principled BSDF base colors, NO textures), materials named exactly:
`wood` (0.55, 0.34, 0.19), `wood_light` (0.68, 0.45, 0.26), `wood_dark` (0.33, 0.19, 0.12), `iron` (0.24, 0.22, 0.25), `gold_drip` (1.0, 0.78, 0.22).

## Export
- Recalc normals outward on every piece. Export all objects (pieces + splinters, as separate meshes, keep hierarchy flat) to
  `Game/Assets/Art/Characters/Ch1/Kit_WoodGate.fbx` with
  `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
- No cameras/lights. Print `EXPORTED Kit_WoodGate <pieces> <tris> <dims>` and `DONE`.
- Short Korean header comment at the top of the script (what it makes, how to run).
