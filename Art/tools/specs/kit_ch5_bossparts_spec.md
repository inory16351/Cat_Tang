# Chapter 5 final boss "Lord of Time" — extra parts to make him look POWERFUL (Codex task)

Read `Art/tools/specs/_ch5_common.md` first and follow ALL its rules (role material names, coordinates, FBX export, preview PNG, Korean header comment, richness rules).
Write a NEW script `Art/tools/blender_kit_ch5_bossparts.py` and run it:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_bossparts.py`
Check the log ends with `DONE`, look at the preview PNG and fix problems.

Context: the boss (Tripo model `Game/Assets/Art/Characters/Ch5/Ch5_Boss.fbx`, see concept `docs/images/ch5/ch5_boss.jpg`) is a hooded reaper whose face is a cracked clock frozen at 11:59, holding a giant scythe. In game he is 5.6 m tall. The user says he does not feel powerful enough. These parts are attached to his bones / float around him in game to add menace and scale. Design them at the size for a 2.2 m reference body (the game scales by height / 2.2, so a 5.6 m boss multiplies by ~2.5). Each part = ONE joined mesh object, origin as stated, exported to `Game/Assets/Art/Characters/Ch5/Kit_BP_<Name>.fbx`. Avoid uniform/regular shapes where things are broken: broken edges must be irregular and jagged (user rule).

## Parts
1. `Halo` — a huge broken CLOCK-DIAL HALO that floats behind the head: a ring of radius 0.95 (in the XZ plane, facing +Y = front) made of 12 separate heavy gold/brass dial segments with roman-numeral blocks (`dial_face` plates with `dial_mark` relief strokes, not text), several segments cracked off and hovering slightly out of place, `crack_gold` seams, 4 short `gold` spikes radiating outward at 12/3/6/9, and two long clock hands (`gold_dark`) crossing inside pointing to 11:59. Origin = ring centre. ~6000 tris.
2. `Pauldron_L` / `Pauldron_R` — massive shoulder plates for a cloaked reaper: layered `void_stone_dark` / `iron_dark` plates with `gold` trims, a jutting broken GEAR half-embedded on top (`brass`), 3 spikes of violet `crystal` growing out of a crack, a small hanging chain with a tiny hourglass (`gold`, `sand`, `porcelain` glass). Mirror pair. Origin = where it sits on the shoulder joint, +Y forward, extends up/outward ~0.35 m. ~3500 tris each.
3. `Crown` — a jagged crown of 7 uneven shard-spikes (`gold` and `crystal`, different heights 0.15-0.45) rising from a cracked band, sits on top of the hood. Origin = band centre bottom. ~2500 tris.
4. `CapeShards` — the cape hem dissolving into the void: a semicircle (behind the body, radius 0.55 at Z 0) of 14 drifting `night_sky` / `violet_dark` cloth-like torn shards and `mirror` glass fragments floating 0.0-0.5 m above ground at irregular heights and angles, a few `starlight` specks. Origin = centre on the ground. ~3000 tris.
5. `Orbit_Hourglass` — a single floating hourglass (0.35 tall) with broken `gold` frame, `porcelain` glass bulbs and `sand` inside, cracked. Origin = centre. ~1200 tris. (The game makes 3 of them orbit the boss.)
6. `Orbit_Gear` — a floating broken gear chunk (0.4 wide, missing a third of its teeth, jagged break), `brass` / `gold_dark` with `crack_gold` seam. Origin = centre. ~1200 tris.

Preview: all parts laid out next to a 2.2 m grey capsule for scale, to `Art/tools/specs/blender_kit_ch5_bossparts_preview.png`.
