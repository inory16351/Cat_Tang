Edit ONE existing Blender script: `Art/tools/blender_kit_ch3_walls.py`. Change ONLY the piece `Cliff_Shore` (its builder function); leave every other piece exactly as it is.
Then RUN `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_walls.py` and fix errors until it exports every piece and re-renders the preview.
Read `Art/tools/specs/_ch3_common.md` for material names / coordinates / export rules.

## Problem
In the game, `Cliff_Shore` modules are tiled side by side along the room edge (6 m wide each, randomly scaled in height 0.8-1.35x and width 1-1.18x). The current version is made of axis-aligned rectangular slabs, so a whole wall reads as a row of brown BOXES. The user strongly dislikes the "row of square brown boxes" look.

## New `Cliff_Shore` (6 m wide, X -3..3, depth Y about -0.6..1.4, height up to ~3.6 m)
A natural mirror-lake SHORE made of ROUNDED, organic rose sandstone boulders, not boxes:
- 7-10 boulders of varied sizes (radius 0.5-1.6 m). Build each from a subdivided icosphere / cube with random vertex displacement (seeded), then flatten slightly (scale Z 0.6-0.9) and cut flat facets so it looks hand-carved, cel-shaded and chunky (faceted, ~40-120 faces each, no smooth blobs). No two boulders the same shape or size, random rotations.
- Arrange them as an irregular PILE: big boulders at the back (Y < 0.6) reaching 2.6-3.6 m, medium ones in front at 1-1.8 m, small ones at the very front around Y 1.0-1.4. The TOP SILHOUETTE must be bumpy and irregular, never a flat or stepped line. Boulders overlap / interpenetrate so there are no gaps through the pile at ground level from X -3 to 3.
- Both ends (X = +-3) should be lower and taper (so tiling neighbours blend), the tallest point somewhere in the middle third, offset from center.
- Materials: `sandstone` main, `sandstone_light` on upward-facing facets (assign per face by normal Z > 0.55), `sandstone_dark` on downward / steep lower facets near the ground.
- Keep the diagonal `gold` vein, but as a thin raised strip following the surface of one big boulder.
- `sand` drift: a low rounded wedge at the front foot (height <= 0.35) along the whole width.
- 3 small `crystal` clusters (hexagonal prisms with pointed tips) tucked between boulders, one up on a ledge.
Under ~3000 triangles. Keep origin at bottom center and the same export settings / FBX path (`Kit_Cliff_Shore.fbx`).
