# Chapter 5 — floating memory debris + boss dial arena (Codex task)

Read `Art/tools/specs/_ch5_common.md` first and follow ALL its rules (role material names, coordinates, FBX export, preview PNG, Korean header comment, richness rules).
Write a NEW script `Art/tools/blender_kit_ch5_debris.py` and run it:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_debris.py`
Check the log ends with `DONE`, look at the preview and fix problems.

Why: the user says chapter-5 rooms look far too plain compared to the other chapters. These pieces FLOAT in the air above the arena (2.5-5 m up, slowly rotating) and are placed as interior set dressing, so the map feels like a collapsing space full of drifting memories of the previous chapters. They are seen from a 55-degree top-down camera, so make the TOP and SIDES rich and readable; bottoms can be broken rock with `void_stone_dark` and hanging `crack_gold` drips. Every piece is BROKEN: jagged irregular fracture edges (never straight or uniform — user rule), a chunk of floor/rock still attached underneath (an "island" root), `crack_gold` seams, 1-3 small detached chunks floating 0.1-0.3 m off it (joined into the same mesh).
Origin = centre of the piece's mass (not the bottom) for all FLOAT pieces, because the game rotates them in the air.

## Floating debris (each ~1.5-3 m, 2500-5000 tris)
1. `FD_Saloon` — a broken piece of a Wild-West saloon facade: swinging doors + part of the porch planks (`wood`, `wood_dark`), a crooked "SALOON"-shaped sign board (shapes only, no letters: raised blocky relief), on a rock island root.
2. `FD_Rail` — a twisted segment of railway: 3 sleepers (`wood_dark`), two bent rails (`steel`, `iron_dark`) curling upward at one end, a station lamp post snapped in half.
3. `FD_StationClock` — a big round station clock (`dial_face`, `dial_mark`, `brass`) cracked in half, hands frozen, hanging from a broken iron bracket.
4. `FD_Gears` — a cluster of 3 interlocked clock gears (`brass`, `gold_dark`) of different sizes, one gear chipped, a pendulum bob dangling.
5. `FD_MirrorFrame` — an ornate gold mirror frame (`gold`) with the glass shattered (`mirror` shards still in the frame, some floating out), desert sand spilling from the bottom (`sand`).
6. `FD_DesertPillar` — a broken sandstone pillar drum (`sand`, `void_stone`) with a crystal growth (`crystal`), tilted.
7. `FD_Bell` — a bronze bell (`brass`) cracked, with its broken wooden yoke (`wood_dark`).
8. `FD_Island` — a plain floating rock island (3 m wide) of broken violet floor tiles (`void_stone`, `marble`) with `crack_gold` seams and a little `starlight` dust — the filler piece.

## Boss arena pieces (ground, origin = bottom centre)
9. `BA_DialRing` — the floor of the boss arena: a huge broken CLOCK DIAL disc, radius 21 m, flat (top at Z 0.0-0.08), made of 12 wedge-shaped dial segments (irregular jagged gaps of 0.3-1.2 m between them showing nothing — the game puts void below), each segment with `marble` face, `dial_mark` roman-numeral relief blocks near the rim (shapes only), `gold` rim trim, `crack_gold` cracks; 3 segments are tilted/sunk a little (2-5 deg). Keep tris under 12000.
10. `BA_HandSpire` — a giant broken clock hand (`gold_dark`, `gold`) stabbed diagonally into the ground like a spire, 9 m long, with chains (`iron_dark`). Origin = where it enters the ground.
11. `BA_FloatingNumeral` — a floating chunk of the dial with one big roman numeral relief (shape blocks), `marble` + `gold`, 2.5 m, rock root underneath (FLOAT origin = centre).

Preview: all pieces in a grid with a 2.2 m grey capsule for scale, to `Art/tools/specs/blender_kit_ch5_debris_preview.png`.
