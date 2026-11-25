You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch3_quest.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_quest.py`
After writing it, RUN it once with that command and fix errors until it exports every piece and renders the preview.

## Base it on the chapter-2 story map script
Read `Art/tools/blender_kit_story_platform.py` FIRST and copy its whole framework into the new script (one self-contained script):
`materials()`, `Part`, `box` / `extrude` / `lathe` / `cylinder` / `tube` / `rod` / `beam` / `hoop` / `sector` / `lump` / `scroll` / `numeral` / `clock` helpers,
`SPECS` table, `assembled_mesh`, `triangles`, `bounds`, `finish()` (decimate to budget, recenter, drop unused materials, FBX export with EXACTLY the same export options), `preview()` and `main()`.
Changes from that script:
- `OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3Quest'`, files `Kit_Q_<Name>.fbx`.
- `PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_quest_preview.png'`, preview world colour a dawn starry violet (.12, .1, .22).
- No piece keeps the world origin: every piece is recentred exactly like the platform script (X/Y centred, Z from the lowest point), EXCEPT where a piece below says "origin at ..." (then put that point at the origin and do not recentre it).
- The ROLES / HARD / PALETTE lists below replace the platform ones. Materials are flat grey Principled BSDF named by role (Unity recolours by material NAME).
- Header comment in Korean: what it makes ("3장 클리어 퀘스트 '거울 마을' 키트 — 선 거울 · 광장 큰 거울 · 물웅덩이 · 노인 집 물건 4 · 노인 실루엣, 2026-11-25"), how to run.

## UVs (NEW, important)
The assembled mesh of every piece must carry ONE UV layer named `UVMap`. All faces get UV (0,0) EXCEPT faces whose role is `mirror_glass`:
those get a planar projection onto the glass plane, normalised to 0..1 over the glass rectangle's bounding box, with U increasing toward the viewer's RIGHT (+X) when looking at the glass from its front, and V increasing UP along the glass. Keep these UVs through decimation (do not decimate `mirror_glass` faces: build them as a simple flat fan / grid of <= 64 tris and exclude them from the decimate, e.g. by decimating with a vertex group or by building glass after decimation and joining). The FBX export must include the UVs. Unity will put a live render texture on the `mirror_glass` material slot, so the glass must be ONE flat planar region per piece, its front face pointing toward the viewer, lying 0.01 in front of a `mirror_back` backing panel.

## Setting
Chapter 3 of the game: the cat gunslinger wakes in his dream village and finds it MIRRORED left-right. Ornate standing mirrors appear on the islands; in them the old man's real-world belongings show up. Dawn starry-night village: violet sky, warm gold lamps, ornate western-dream style. Seen from a top-down camera ~55 deg above at small size, cel shader with black outlines.
Ornate rule (user requirement — never single-colour blocks): every piece uses at least 5 role materials (mirrors 6-8), no material over ~55% of the visible surface; layered moulded frames, gold filigree curls, gem studs, carved relief bands, finials. Chunky: nothing thinner than ~0.04 m. Seeded slight irregularity per piece. See also `Art/tools/specs/_ch3_detail_v2.md` for the detail style.

## Roles (ROLES list, all grey in Blender)
`wood`, `wood_dark`, `wood_light`, `gold`, `brass`, `bronze`, `iron`, `iron_dark`, `stone`, `stone_dark`, `moss`, `mirror_glass`, `mirror_back`, `gem_rose`, `gem_amber`, `ivory`, `velvet`, `fabric`, `fabric_dark`, `leather`, `poster`, `paper`, `clock_face`, `sand`, `glass`, `window_lit`, `curtain`, `thread_red`, `shadow`, `shadow_blur`, `water_edge`
HARD (bevelled) = everything except `mirror_glass`, `mirror_back`, `velvet`, `fabric`, `fabric_dark`, `poster`, `paper`, `sand`, `glass`, `window_lit`, `curtain`, `thread_red`, `shadow`, `shadow_blur`, `water_edge`, `moss`.
PALETTE (preview only): wood (.55,.36,.24), wood_dark (.3,.19,.14), wood_light (.75,.58,.4), gold (1,.8,.35), brass (.85,.64,.32), bronze (.6,.42,.26), iron (.25,.26,.34), iron_dark (.13,.13,.2), stone (.62,.6,.72), stone_dark (.36,.34,.48), moss (.35,.55,.42), mirror_glass (.75,.88,1), mirror_back (.2,.22,.35), gem_rose (1,.45,.7), gem_amber (1,.65,.2), ivory (.95,.92,.82), velvet (.5,.12,.3), fabric (.45,.38,.3), fabric_dark (.26,.22,.2), leather (.45,.28,.2), poster (.9,.72,.5), paper (.95,.9,.78), clock_face (1,.95,.82), sand (.95,.8,.5), glass (.6,.8,.95), window_lit (1,.82,.5), curtain (.7,.3,.35), thread_red (.9,.2,.25), shadow (.07,.05,.12), shadow_blur (.25,.2,.35), water_edge (.4,.55,.8).

## Coordinates
Blender X = Unity X, Blender Y = Unity Z (north, away from the camera), Z up. The camera sits SOUTH (-Y) looking north and down ~55 deg. "Front" = the side facing -Y (toward the camera) unless stated.

## Pieces (SPECS order) — name, size, budget
1. `StandMirror` (budget 7000): ornate cheval (standing) mirror, overall ~2.4 m wide, ~3.4 m tall.
   - Base: a heavy `stone` / `stone_dark` stepped plinth 2.4 x 1.2 x 0.35 with a `gold` moulding band and four `bronze` lion-paw-ish feet (lumps) at the corners.
   - Two `wood_dark` uprights with `gold` collar rings and `gold` urn finials rise from the plinth ends; the mirror frame hangs between them on two `brass` pivot knobs.
   - Mirror frame: TILTED BACK 30 deg (its top leans toward +Y) so it faces the high camera. Frame = layered moulding: outer `gold`, middle `wood` with carved relief band (repeating small sun discs in `gold`), inner `bronze` lip; arched top. Crest on top: a `gold` sunburst with a `gem_rose` cabochon in the middle, two `gem_amber` studs on the sides; small `gold` filigree curls at the four corners.
   - GLASS: an arched-top rectangle exactly 1.6 m wide x 2.2 m tall (the arch is a half-ellipse of rise 0.35 inside that 2.2), role `mirror_glass`, planar UV as described above, 0.01 in front of a `mirror_back` panel. Its centre (before tilt) is 1.85 m above the plinth bottom.
2. `PlazaMirror` (budget 9500): the big plaza mirror, same build language but grander: overall ~3.6 m wide, ~5.4 m tall.
   - Round stepped pedestal (three steps, radius 2.2 / 1.9 / 1.6, `stone`, `stone_dark`, `ivory` with `gold` edge bands, a mosaic ring of small `gem_rose` / `gem_amber` diamond inlays on the top step).
   - Two fluted `ivory` columns with `gold` capitals hold the frame; frame tilted back 25 deg.
   - GLASS: arched-top rectangle exactly 2.6 m wide x 3.6 m tall (arch rise 0.6), `mirror_glass` with planar UV, `mirror_back` behind. Centre (before tilt) 2.7 m above the pedestal bottom.
   - Frame: outer `gold`, `velvet` inset band with `gold` studs, inner `bronze`; crest = a round clock dial (`clock_face`, radius 0.45, `iron_dark` ticks, hands fixed at 11:59) inside a `gold` sun-ray halo with three `gem_rose` gems.
3. `MirrorPuddle` (budget 2500): flat irregular rain puddle on the ground, outer radius ~2.0 (10-12 jagged points, seeded), total height <= 0.15.
   - Water surface: ONE flat region of role `mirror_glass` at Z 0.02, planar UV over its bounding square (U = +X, V = +Y, i.e. looking straight down with north up).
   - A thin `water_edge` ring band (0.12 wide) around the water at Z 0.015, 8 small flat rim stones (`stone` / `stone_dark` lumps, 0.25-0.45) and 3 `moss` tufts around it. Origin at the centre of the water, Z 0 = ground (do not recentre).
4. `CuckooBench` (budget 6000): the old man's workbench with the FIRST cuckoo clock he made.
   - `wood` / `wood_dark` workbench 1.6 x 0.8, 0.95 tall: thick top with `wood_light` edge strip, turned legs, a lower shelf with a `leather` tool roll and a `brass` oil can, a `iron_dark` bench vice on the right end.
   - On the bench, standing upright and facing -Y: a hand-carved wooden cuckoo clock ~0.75 m tall: house shape with steep `wood_dark` leaf-carved roof, `wood_light` facade with carved leaves, `clock_face` dial (radius 0.13, `iron_dark` hands at 11:59), a little door open with a small `ivory` / `gem_amber` bird popping out, two pinecone weights (`bronze`) on chains hanging over the bench front edge, a `gold` small finial bird on the roof.
   - Scattered on the bench: a few `brass` gears, `iron` tweezers, a magnifier on a `brass` arm.
5. `HourglassBench` (budget 6000): a second workbench (same footprint, slightly different details: `wood_dark` top, `iron` braces) with a LARGE hourglass ~0.75 m tall standing on it: `brass` top and bottom discs with `gold` turned pillars (3), `glass` bulbs with `sand` inside (more sand in the bottom bulb, a thin sand stream in the neck), and a CRACK: a jagged inset groove line in `iron_dark` running across the upper bulb. Around it: scattered `sand` spill heap, `brass` gears, a small `ivory` notebook.
6. `CoatPoster` (budget 6000): a group:
   - A standing `wood_dark` coat rack 1.9 m (turned post, `brass` hooks, tripod feet) with a long worn duster COAT hanging from it (`fabric` with `fabric_dark` patches, folds as vertical ridges, `leather` belt) and a cowboy hat (`leather`, `gold` band) on the top hook.
   - Beside it (+X 1.0 m) a `wood` easel holding a framed western movie POSTER (0.9 x 1.3): `wood_dark` frame with `gold` corners, the poster itself in `poster` with RAISED relief shapes (a gunslinger silhouette in `shadow`, a big setting sun disc in `gem_amber`, mesa outlines in `bronze`) — NO letters.
   - In front of the easel, a small film PROJECTOR on a `wood` crate: `iron_dark` body, two `iron` film reels on top, `glass` lens, `brass` crank.
7. `SewingWindow` (budget 6000): a group:
   - A free-standing window frame 1.6 wide x 2.0 tall (`wood_light` frame with `ivory` mullions, four `window_lit` panes) with `curtain` drapes tied at both sides and a `wood` sill with a little `paper` note.
   - In front of it: an antique TREADLE sewing machine: `iron_dark` filigree cast-iron legs (scroll shapes) with a treadle plate, `wood` table top 1.0 x 0.5, machine head in `iron_dark` with `gold` decal bands, `brass` hand wheel, a spool of `thread_red` on top with a thread running to the needle, a folded piece of `fabric` on the table.
8. `OldManShadow` (budget 3000): faceless SILHOUETTE of an old man 1.72 m, slightly hunched, thin, in a long worn overcoat to the knees (hem frayed into 4-5 uneven tatters), collar up, no hat, sparse hair tuft. Body all `shadow`; the head front has a soft round `shadow_blur` patch where the face would be (the face is "smudged"). Thin `shadow_blur` band at the coat hem. LEFT arm hangs at his side. NO RIGHT ARM (it is a separate file). Mark the right shoulder joint: the origin of this piece is at the bottom centre between the feet (do not recentre), and print `SHOULDER_R <x y z>` = the right shoulder pivot point (on his right = viewer's LEFT when he faces -Y, so negative X... careful: he faces -Y toward the camera, so his right side is at -X).
9. `OldManArm` (budget 600): his right arm in the same style (`shadow` sleeve with a `shadow_blur` cuff band, a thin bony hand), 0.68 m long, hanging straight DOWN along -Z from the origin. Origin = the shoulder pivot (top of the arm), do not recentre.

## Output
Print `EXPORTED <Name> <tris> <dims>` per piece, the `SHOULDER_R` line, and `DONE` at the end (same as the platform script). Render the preview PNG with all pieces in two rows (mirrors in the top row) and the arm attached to the old man at the shoulder.
