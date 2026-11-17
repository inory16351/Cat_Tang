You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch2_train.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_train.py`
After writing it, RUN it once with that command and fix errors until it exports every piece.
Read `Art/tools/specs/_ch2_common.md` first: it has the material names, coordinate and export rules you must follow.
Use `Art/tools/blender_kit_ch2_landmarks.py` (its `LM_Locomotive` / `LM_Coach`) as the reference for code style, materials, bevel, join and export. Short Korean header comment at the top.

## Goal
A MOVING THREAT: a ghost steam train that sweeps along a 3 m wide railway track lane through a chapter-2 room. Game code moves it; the model is static (no rig, no animation). The train is long and narrow and is seen mostly from ABOVE (camera ~55 deg down), so the TOP silhouette must read clearly: roof ribs/vents, smokestack, steam dome, cab roof, coupling between cars.

## Orientation (important, differs from the landmark locomotive)
- The train TRAVELS toward +Y (Blender) = the front. Length runs along Y, width along X.
- Origin at the bottom center of the footprint (wheels touch Z = 0, X centered, Y centered on the body length incl. coupler/cowcatcher).
- Width (X) must stay <= 2.6 m including every protrusion (handrails, steps, cylinders, rods) so it fits inside the 3 m track lane.

## Pieces (W x L x H in m)
1. `TrainEngine` (2.6 x ~9.0 x ~3.6): steam locomotive.
   - `iron_dark` long cylindrical boiler lying along Y (radius ~0.85, 12-16 sides), its front end capped by a round `iron` smokebox door.
   - BIG round headlight on top of the front of the boiler (lamp housing `brass`, lens disc ~0.9 m diameter facing +Y) - lens uses material `train_glow`.
   - `soot` smokestack (flared top, ~1.0 m tall above boiler) near the front; a `brass` steam dome and a smaller sand dome on top of the boiler behind it.
   - Front: wedge-shaped COWCATCHER (pilot) at the very front, low, angled bars/slats leaning forward, spanning nearly full width - material `train_glow` (so it can glow as a danger sign). Front buffer beam `cloth_red` behind it.
   - Rear: boxy `wood_dark`/`paint_green` driver CAB (about 2.6 m wide, 2.6 m long) with `roof_tin` roof overhanging slightly, `window_lit` side and front windows; the cab roof is the highest flat area besides the stack (~3.6 m total height).
   - Running gear: 3 pairs of big `iron` driving wheels (radius ~0.7) + small leading wheels, `brass` connecting rod on each side, `iron_dark` frame/footplate along the full length, `iron` cylinders at the front sides. Keep all within the 2.6 m width.
   - A `brass` handrail line along each side of the boiler; a small `brass` bell or whistle.
   - Rear coupler (`iron` hook block) at -Y end.
2. `TrainCar` (2.6 x ~8.0 x ~3.2): one passenger coach, repeated by code behind the engine (cars placed end to end, so front and rear coupler ends must look fine when butted together at ~0.2 m gap).
   - Body box `paint_green` with `wood` lower panel band, `paint_cream` thin stripe under the windows.
   - A row of 6-7 `window_lit` windows on BOTH long sides (+X and -X), slightly inset frames `wood_dark`.
   - `roof_tin` gently curved roof (arched along X) with a raised clerestory strip running along the length (`roof_tin`) and 3-4 small `iron_dark` roof vents on top - top view should show a clear ribbed roof (add 6-8 thin `iron` cross ribs).
   - End platforms at both ends (`iron_dark` floor, `brass` railings, small `wood_dark` end door), `iron` coupler block at each end (front +Y and rear -Y).
   - Two `iron_dark` bogies each with 2 axles of `iron` wheels (radius ~0.45), body floor at ~1.0 m.

## Materials
Use the role names from `_ch2_common.md` (all grey (0.5,0.5,0.5)), PLUS exactly ONE extra role material named `train_glow` (also grey, same flat Principled BSDF) used ONLY for the headlight lens and the cowcatcher - the game makes it glow as a threat signal. Add `train_glow` to the allowed role set in your script.

## Budget / style
Low-poly primitive composition, chunky, a little seeded irregularity, BEVEL 0.02-0.04 / 2 segments on wood/metal. Each piece <= ~3000 triangles AFTER modifiers - check and print it; lower cylinder sides or drop bevel on small parts if over.

## Export
Each piece = ONE joined mesh object named `TrainEngine` / `TrainCar`, exported to `Game/Assets/Art/Characters/Ch2/Kit_TrainEngine.fbx` and `Kit_TrainCar.fbx` with the export options from `_ch2_common.md`. Print `EXPORTED <Name> <tris> <dims>` per piece and assert width (X dim) <= 2.6 + 1e-3. Print `DONE` at the end.
