# Chapter 5 floor kit — shattered / warped ground pieces (Codex task)

Read `Art/tools/specs/_ch5_common.md` first and follow ALL its rules (materials by role name, coordinates, FBX export, preview PNG, Korean header comment).
Write a NEW script `Art/tools/blender_kit_ch5_floor.py` (Blender 5.3, run in background) that builds and exports the pieces below, then run it:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_floor.py`
Check the log ends with `DONE`, look at the preview PNG, and fix anything broken (overlapping garbage, missing faces, wrong origin, wrong size).

Why: the user said the chapter-5 floor looked like a recoloured reuse of chapter 4's neat marble checkerboard. The floor must feel MUCH more broken and warped, and fragments of the PREVIOUS chapters' floors must be mixed in here and there as cracked, distorted patches.
**SHAPE RULE (user, most important): where broken parts meet or are joined — outlines, gaps, seams, hole rims, crack paths — NEVER use uniform geometric shapes (no rectangles, squares, circles, regular polygons, straight even strips, evenly spaced zig-zags). Every outline and gap must be IRREGULAR and DISTORTED: jagged with uneven segment lengths, bulging and pinching widths, bent and wobbling edges (build outlines from seeded random radii + noise displacement, then bend/warp them), like torn and melted glass. A shape that looks like a primitive with noise on top is not enough — the overall silhouette itself must be lopsided.**
These pieces lie ON the floor (origin = bottom centre, bottom at Z 0, most pieces are low so the player walks over them; nothing taller than 0.6 m unless listed). Top faces must stay readable from a 55 deg top-down camera.

Extra material name allowed ONLY in this script: `patch_top` — the game replaces it at runtime with a previous chapter's real ground material (chapter 1 western dirt, chapter 2 station tiles, chapter 3 mirror lake, chapter 4 marble). Faces using `patch_top` MUST have a planar top-down UV in WORLD metres divided by 8 (u = x / 8, v = y / 8) so the ground texture keeps its real scale.

## Pieces (name, approx size X x Y x Z)
1. `FloorPatch_A` (3.6 x 2.8 x 0.14) — an irregular broken slab of an older floor: top face `patch_top` (one flat polygon outline with 9-12 jagged corners, no grid), thin side walls `void_stone_dark`, the whole outline rimmed with a raised (<= 0.03) glowing `crack_gold` seam, one or two cracks crossing the top (thin `crack_gold` inset lines). Very slightly tilted (2-4 deg) as if it sank.
2. `FloorPatch_B` (2.6 x 2.2 x 0.18) — same idea, broken into 3 pieces separated by 0.08-0.15 m void gaps (`night_sky` strips at the bottom of the gaps with 3-5 tiny `starlight` dots), each piece tilted differently (3-7 deg), one piece lifted 0.08 m.
3. `FloorPatch_C` (4.2 x 1.6 x 0.12) — a long bent strip of older floor that is WARPED: the strip curves along its length (bend ~25 deg in plan view) and its surface waves up and down (+-0.06 m, subdivided enough to look smooth), `patch_top` top, gold seam rim.
4. `FloorVoidPit` (2.4 x 2.0 x 0.05) — a jagged hole in the floor: a flat `night_sky` void face sunk to Z 0.005 with 6-10 `starlight` dots and a faint violet nebula band (`violet_dark`), surrounded by a ring of broken `void_stone` lip chunks (0.03-0.05 high) whose inner edges glow `crack_gold`; 2 small detached chunks floating 0.1-0.2 m above the hole.
5. `FloorSlabTilted` (2.2 x 1.8 x 0.6) — a big shattered slab of violet `marble` / `marble_dark` tipped up from the floor (one edge buried at Z 0, the other edge raised to ~0.55 m, ~15 deg), its exposed underside `void_stone_dark`, a spiderweb of `crack_gold` on top, 2-3 small `mirror` chips on it. (This one is an obstacle-sized landmark bit.)
6. `FloorShardSpike` (1.4 x 1.4 x 0.9) — a cluster of 4-6 jagged mirror-glass shards (`mirror` faces, `mirror_back` backs, `crystal` bases) jutting up out of a small cracked floor star (`crack_gold` lines on a `void_stone` disc 0.04 high).
7. `FloorWarpRidge` (3.0 x 0.9 x 0.25) — the floor itself bulging and buckling: a low curved ridge of `void_stone` / `void_stone_dark` slabs pushed up along a wavy line, cracked open along its crest with a `crack_gold` seam and a thin `night_sky` slit.

Budgets: up to ~3000 tris per piece (FloorSlabTilted / FloorShardSpike up to 4000). Same FBX export settings as the common rules, files `Game/Assets/Art/Characters/Ch5/Kit_<Name>.fbx`. Preview `Art/tools/specs/blender_kit_ch5_floor_preview.png`.
