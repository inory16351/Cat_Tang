Make ONE Blender low-poly floor piece for the Unity game Cat_Tang, chapter 2 boss arena, and export it to FBX.

Write the Blender Python script to `Art/tools/blender_kit_ch2_arena.py` (Korean header comment: what it makes, how to run), then run it headless:
`blender -b -P Art/tools/blender_kit_ch2_arena.py` (find blender.exe the same way the existing scripts / logs in Art/tools do).
Read `Art/tools/specs/_ch2_common.md` (materials, coordinates, export rules) and copy the setup / bevel / finish / export style of `Art/tools/blender_kit_ch2_landmarks.py` (its `turntable` piece is the OLD version of this floor: replace it with this richer one, but do NOT edit that script).

## Why
The old turntable deck read as a plain brown disc in game (one `wood_dark` colour, thin outline). The camera looks down ~55 deg from far away, so detail must come from LARGE colour regions made of separate material faces, not small bevels.

## Revision 3 (2026-11-18) - REPLACE revision 2 completely
Revisions 1 and 2 still clashed with the room's paving tiles (the room floor is a warm grey-beige stone-tile TEXTURE that we cannot reproduce with faces). New idea: a GIANT CLOCK laid on the floor, with NO full disc - the room's own tiles are the clock face. The model only contains the clock parts that sit ON the tiles. Rewrite `blender_kit_ch2_arena.py` for this; same piece name `Arena`, same FBX path and export settings.

## Piece: `Arena` -> `Game/Assets/Art/Characters/Ch2/Kit_Arena.fbx`
Diameter 32 m. ORIGIN at BOTTOM center (Z = 0 = the floor the tiles are on; everything is at Z >= 0). +Y = front = 12 o'clock.
1. Rim curb (the arena edge), radius 15.0 -> 16.0, height 0.25: `stone` coping in 24 blocks with tiny gaps, `stone_dark` thin lip on its inner vertical face.
2. Hour SLOTS in the rim (like the chambers of a revolver cylinder): at each of the 12 hours a ROUND socket cut down into the curb top: hole radius 0.42, `iron_dark` hole floor at Z 0.08, `brass` ring around it (0.12 wide, top Z 0.26). At 12 / 3 / 6 / 9 the slot is a DOUBLE (two sockets side by side, 0.95 m apart along the rim). Between hours, 4 small minute notches per gap: `iron_dark` rectangles 0.35 x 0.1 inset in the curb top.
3. Chapter ring: a thin `brass` ring at radius 14.4 -> 14.6, height 0.03, lying on the tiles.
4. Rails = the 12-6 and 3-9 lines: two `rail_steel` track pairs crossing in a + through the center (along X and Y), gauge 1.5 m, rail top 0.12 wide, height 0.05, running from radius 1.6 to 14.4, with `iron_dark` tie plates (2.2 x 0.22, height 0.02) every 1.2 m. These are boss charge paths: clearly visible.
5. Giant HANDS lying flat on the tiles (height 0.04, slightly above the tie plates): hour hand pointing at 10 o'clock, length 8.5 m, tapered 0.9 -> 0.3 m, `iron_dark` with a `brass` spade tip; minute hand pointing at 2 o'clock, length 13 m, tapered 0.6 -> 0.2 m, `iron_dark` with `brass` arrow tip; a short counterweight tail (1.8 m) behind each. Hands must not overlap the rails except at the hub.
6. Center hub: `brass` disc radius 1.5 (height 0.08) with 12 low gear teeth, `iron_dark` pin radius 0.5 on top (height 0.14).
NO filled face, NO wood, NO cream. Materials only: `stone`, `stone_dark`, `iron`, `iron_dark`, `rail_steel`, `brass`.
Budget <= 9000 triangles. One joined manifold mesh named `Arena`. Total bounds 32 x 32 x ~0.25 (assert X/Y = 32 from the rim). Print `EXPORTED Arena <tris> <dims>` and `DONE`.
Preview: render it ON TOP of a plain warm grey-beige plane (0.62, 0.56, 0.48) so it looks like it will on the tiles.

Also render a preview PNG `Art/tools/specs/kit_ch2_arena_preview.png` (top-down orthographic + one 55 deg view, temporary preview colours per role only in the render, not in the exported materials) and look at it; fix anything that reads wrong.

Do NOT edit any C# file, other scripts, or git.
