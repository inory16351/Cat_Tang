You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch5_phantom.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_phantom.py`
After writing it, RUN it once with that command and fix errors until it exports every piece. Then look at the preview PNG and fix anything that reads badly.
Read `Art/tools/specs/_ch5_common.md` first (material names, richness, coordinates, export settings). Use `Art/tools/blender_kit_ch4_merchant.py` as the reference for building a character-like figure from primitives (it is also a no-skeleton figure).

## Goal
The chapter-5 enemy "MIRROR PHANTOM" (거울 허상), built in Blender because it needs NO skeleton: it FLOATS and the game animates it in code (bobbing, leaning, spinning the shard ring, splitting into clones; only the real one casts a shadow). It must sit in the same family as the other chapter-5 enemies: chunky 2-head-tall chibi proportion, ~2.1 m tall, toy-like, cel-shade friendly, readable from a high top-down camera.

Look: a hooded spectre made of MIRROR. A big rounded hood/head (≈ 45% of the height) whose face opening is a dark `indigo` void with a smooth `mirror` mask plate (oval, slightly cracked with raised `crack_gold` lines) and two glowing `magenta` diamond eyes; a short faceted cape/body of angular `mirror` panels (front faces `mirror`, edges `mirror_back`) over a `violet_dark` inner robe, the cape hem breaking into a TAIL of 4-6 jagged shards that taper to a point at the bottom (no legs — it hovers); `gold` collar clasp with a `magenta` gem; `crack_gold` crack seams across the panels; two chunky floating HANDS (mitten-like, `mirror` with `gold` cuffs) detached from the body at shoulder height (≈0.15 m gap, no arms), slightly forward, palms in.

Objects (export together in ONE FBX `Game/Assets/Art/Characters/Ch5/Kit_Ch5_MirrorPhantom.fbx`, flat hierarchy):
- `Body` — hood + mask + cape + shard tail, ONE joined mesh. Origin at (0,0,0) = the point directly under the body where the floor would be; the lowest tail tip is at Z 0.35 (it hovers), top of hood at ~Z 2.1. Faces +Y.
- `Hand_R` — right floating hand (+X side, the phantom's right), origin at its own centre, placed at about (+0.62, +0.12, 1.15).
- `Hand_L` — mirror of Hand_R at (-0.62, +0.12, 1.15).
- `Orbit` — a ring of 6 small angular mirror shards (`mirror`, `mirror_back`, `crack_gold` edge), each ~0.22 long, evenly spaced on a circle of radius 0.85 at Z 1.0, tilted outward; origin at (0, 0, 1.0) so the game can spin it around Z. ONE joined mesh.
Under ~6000 triangles total. At least 6 materials. Print `EXPORTED <Name> <objects> <tris> <dims>` and `DONE`.

Preview: render to `Art/tools/specs/blender_kit_ch5_phantom_preview.png` (1800x1000, Workbench flat MATERIAL colours with a distinct viewport colour per material name close to the game palette: mirror = pale silver-blue, violet_dark = deep purple, crack_gold = bright gold, magenta = magenta): front view, 3/4 view, and a top-down ~55 deg view side by side (duplicates only in the preview).
