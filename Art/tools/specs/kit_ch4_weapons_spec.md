You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch4_weapons.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_weapons.py`
After writing it, RUN it once with that command and fix errors until it exports every piece. Then look at the preview PNG and fix anything that reads badly.
Read `Art/tools/specs/_ch4_common.md` first (material names, richness, export settings). Use `Art/tools/blender_kit_ch4_props.py` as the style reference.

## Goal
HAND-HELD WEAPONS for chapter-4 enemies (wind-up clockwork dolls, noir black/white with red accents). The characters are rigged separately WITHOUT weapons; the game parents each weapon to the enemy's hand bone, so:
- Each weapon is ONE joined mesh, exported to `Game/Assets/Art/Characters/Ch4/Kit_W_<Name>.fbx`.
- ORIGIN = the exact centre of the GRIP (where a closed fist holds it). The weapon's LONG AXIS points along +Z from the grip (blade / muzzle / head direction = +Z), the "edge" or "front" faces +Y. Anything behind the hand (stock, pommel) goes to -Z.
- Grip is a clear cylinder ~0.045 m radius, ~0.18 m long, centred on the origin along Z, in `wood_dark` or `iron` with a `red_lacquer` or `brass` band so the hand area reads.
- Chunky toy-like proportions (characters are 2-head-tall chibi dolls about 2.2 m tall), cel-shade friendly, no detail thinner than ~0.03 m, bevels on hard edges, at least 3 materials each. Clock / gear motifs.

## Pieces (overall length along Z, including the part behind the grip)
1. `Rifle` (1.35 m; muzzle at +Z 0.95, stock to -Z 0.4): toy-soldier musket: long `steel` barrel with `brass` bands, `wood_dark` stock and fore-end, a small gear-shaped hammer, `red_lacquer` sling ring, bayonet lug at the muzzle (no bayonet).
2. `Bell` (0.4 m; bell mouth at +Z): small hand bell: `wood_dark` handle (the grip), `copper` bell body opening toward +Z (mouth diameter 0.3), `brass` lip ring, tick-mark band in `dial_mark`, visible clapper.
3. `Hammer` (0.5 m; head at +Z 0.38): small mallet: `wood` handle, `iron` head (box 0.18 x 0.1 x 0.1) across X with `brass` caps, `red_lacquer` wrap below the head.
4. `NeedleDagger` (0.9 m; tip at +Z 0.75): a thin long clock-hand blade (second hand): `silver` blade with a hollow diamond near the tip and a sharp point, `red_lacquer` counterweight disc at the pommel end (-Z 0.12), `brass` small crossguard.
5. `PendulumScythe` (2.2 m; head at +Z 1.75, butt at -Z 0.45): long `iron` pole with `brass` rings; at the top a big pendulum disc (`silver`, diameter 0.7, with `dial_mark` clock ticks and an ornate cutout) from which a curved scythe blade (`steel`, edge `silver`, 0.9 long) sweeps to +X; small `red_lacquer` gem in the disc centre.
6. `Wrench` (1.2 m; jaws at +Z 0.95): oversized adjustable wrench: `steel` body, `iron_dark` adjuster screw, open jaw at the top, `red_lacquer` rubber grip sleeve around the origin, gear-tooth notch on the back.
7. `Greatsword` (2.8 m; tip at +Z 2.35, pommel -Z 0.3): the HOUR HAND boss's greatsword shaped like a giant ornate clock hour hand: broad `silver` blade with a hollow spade-shaped tip, `steel` fuller, `brass` crossguard shaped like a small clock dial (`dial_face` + `dial_mark`), `wood_dark` grip, `red_lacquer` pommel gem.
8. `Rapier` (1.7 m; tip at +Z 1.45): the MINUTE HAND boss's thin rapier shaped like a long minute hand: very thin `silver` blade with a small arrow tip, swept `iron_dark` cup hilt with a gear rim, `red_lacquer` ribbon ring at the pommel.

Keep each under ~3000 triangles. Print `EXPORTED <Name> <tris> <dims>` per piece and `DONE`.
Preview: all weapons standing side by side (grip at the same height), 1800x1000, Workbench flat material colours, camera from the front slightly above, to `Art/tools/specs/blender_kit_ch4_weapons_preview.png`; mark each origin with a small red cross ONLY in the preview (do not export it).
