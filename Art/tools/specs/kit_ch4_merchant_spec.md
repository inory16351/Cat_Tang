You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch4_merchant.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_merchant.py`
After writing it, RUN it and fix errors until it exports. Then LOOK at the preview PNG next to the attached concept image and iterate until the model clearly matches the concept (silhouette, proportions, every listed part). Spend effort here: this is a hero character, make it HIGH QUALITY and detailed.
Read `Art/tools/specs/_ch4_common.md` (material NAMES, bevel and export settings) and use `Art/tools/blender_kit_ch4_props.py` as a helper / export reference.

## What
The chapter-4 MERCHANT: a clockwork MUSIC-BOX DOLL (attached image = the concept art, match it). It is NOT an enemy: warm colours, calm, dreamlike, a being eroded by time. In the game it stands on top of a music box and slowly turns, so it must look good from every side and from a ~55 deg top-down camera. Static mesh (no rig), standing pose exactly like the concept: arms slightly out and down (A-pose, fists), legs apart, feet flat.

## Proportions (total height 2.6 m including the crown; chibi ~2 heads tall)
- Head: big round porcelain head ~0.75 m tall, `porcelain` with thin dark crack grooves (inset `dial_mark` lines) across the face; closed smiling eyes = two curved `gold` arcs (eyelids with lashes), a small smile groove; big curled `porcelain` hair rolls (spiral volutes) on both sides like the concept, framing the face.
- Crown / hat: on top of the head a MUSIC-BOX PIN CYLINDER (horizontal drum ~0.55 wide, `porcelain`/`dial_face` drum covered in small raised `wood_dark` pins, `red_dark` end caps with `gold` rims, a `gold` crank handle sticking out to the right) sitting in an ornate `gold` tiara (scroll shapes, a pointed front piece with a `red_dark` diamond inset, small spikes on top).
- Neck: thin `porcelain` neck with a frilled `porcelain` collar, a `velvet` bow tie with a round `copper`/amber `gem_red`-free AMBER jewel (use `lamp_warm` for the amber gem).
- Torso: a GLASS DOME chest (`glass` hemisphere front) showing `gold` and `brass` interlocking gears inside, framed by a `gold` heart-shaped bezel; a `velvet` (deep wine red) cape/shoulder mantle with `gold` embroidered swirl trim and `gold` scalloped edge, short fringe tassels in `gold` at the cape hem.
- Belt: `wood_dark` belt with a big ornate `gold` diamond buckle; hanging from the left hip a `wood_dark` COIN POUCH (tied with `gold` cord, a couple of `gold` coins showing), from the right hip a small `gold` BALANCE SCALE (two pans on chains).
- Skirt: knee-length pleated `porcelain` skirt with a `velvet` overskirt opening at the front, `gold` hem band.
- Arms: ball-jointed doll arms (`porcelain` segments with `gold` ball joints at shoulder / elbow / wrist), `gold` cuffs, white `porcelain` fists.
- Legs: ball-jointed `porcelain` legs, `gold` knee joints, `velvet` + `gold` ankle boots with a small `wood_dark` diamond on the toe.
- Back: a BIG `gold` WIND-UP KEY (figure-8 bow, thick) sticking out of the back, clearly visible from the front past the right shoulder as in the concept.
Colours come from material NAMES (the game recolours by name); use only names from `_ch4_common.md` (`porcelain`, `gold`, `brass`, `velvet`, `red_dark`, `wood_dark`, `glass`, `dial_face`, `dial_mark`, `lamp_warm`, `copper`, `silver` ...). Warm palette: ivory porcelain, gold, wine velvet, dark wood; NO noir black/white look, NO red glowing eyes.
Quality: smooth subdivided organic shapes for head / hair / limbs (use subdivision or high segment counts, shade flat is fine), bevels on hard parts, no detail thinner than ~0.02 m, up to ~20000 triangles.

## Export
ONE joined mesh named `Ch4_Merchant`, origin at the bottom centre between the feet, +Y = the doll's FRONT (face direction), Z up, exported to `Game/Assets/Art/Characters/Ch4/Kit_Ch4_Merchant.fbx` with the export settings from `_ch4_common.md`.
Print `EXPORTED Ch4_Merchant <tris> <dims>` and `DONE`.
Preview: `Art/tools/specs/blender_kit_ch4_merchant_preview.png`, 1800x1000, Workbench flat MATERIAL colours with a WARM viewport colour per material (porcelain ivory, gold yellow, velvet wine red, wood dark brown, glass pale blue), three views side by side: front, 3/4 front-right, back.
