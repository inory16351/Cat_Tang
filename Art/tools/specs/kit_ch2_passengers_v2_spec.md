You are EDITING the existing Blender 5.x script `Art/tools/blender_kit_ch2_passengers.py` (made from `Art/tools/specs/kit_ch2_passengers_spec.md` — read both first). Edit only that script.
Run it headless after editing: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch2_passengers.py` and fix errors until every FBX exports.

## Change of direction (from the art director)
The frozen passengers are no longer translucent ghosts. They are STONE STATUES: people turned to grey stone the moment time stopped, standing on the station floor like a sculpture garden.
The game will render them OPAQUE with the cel/toon shader, so now surface form matters a bit more: soft chiselled stone look, slightly chunky, hand-carved irregularity (seeded random per piece: tiny dents/bevels, nothing noisy). Keep the same stylized chunky proportions (head ~1/4.5 of height) and the same piece / crack / shard system.

## 1. Materials (rename, for ALL passengers)
- `ghost` → `statue` (0.72, 0.70, 0.66), `ghost_dark` → `statue_dark` (0.52, 0.50, 0.47) (hats, luggage, props, hair, shoes), `crack` stays (1, 1, 1).
- Add `statue_base` (0.45, 0.43, 0.40) for the base.

## 2. Stone base under every statue
Each statue stands on a low rough stone plinth: an irregular 8–10 sided slab, radius about 0.42 × height-of-figure / 1.75 (child smaller), 0.10 tall, top slightly uneven, chipped edges (a couple of corners cut off), material `statue_base`. The figure's feet sit ON top of it (shift the figure up by the base height). Origin of the whole statue (for placement) is still the bottom center of the base at Z=0.
The base is split into 3 wedge pieces with jagged edges (pie slices) and those are ordinary `Piece_XX` objects. Add 1–2 crack lines on the base top too.

## 3. Keep the 4 existing poses, add 6 NEW poses (10 total, each its own FBX)
Existing: `Kit_Passenger_Luggage`, `Kit_Passenger_Hat`, `Kit_Passenger_Child`, `Kit_Passenger_Watch`.
New (all frozen mid-motion, big readable silhouettes from a high top-down camera, clearly different from each other in outline):
5. `Kit_Passenger_Newspaper` (1.75): a man in a flat cap and waistcoat, standing, holding a big open NEWSPAPER spread wide in front of him with both hands (thin wide slab 0.6 × 0.4, slightly bent in the middle), one knee slightly bent.
6. `Kit_Passenger_Wave` (1.7): a woman in a long dress and bonnet waving goodbye: right arm raised high holding a fluttering HANDKERCHIEF (stiff wavy little flag), left hand on her chest, body twisted toward the right.
7. `Kit_Passenger_OldLady` (1.45): a hunched old woman with a shawl and a big round BUNDLE (cloth sack) on her back, leaning on a short walking stick, small steps.
8. `Kit_Passenger_MotherChild` (1.7 mother, child 1.0): a mother pulling a small child by the hand, the child leaning back resisting with one arm reaching back toward something; two figures on ONE shared base (oval base about 1.0 × 0.6).
9. `Kit_Passenger_Sitter` (1.25 seated): a stout man sitting on a big TRUNK, stretching both arms up in a frozen yawn, legs apart, a bowler hat tilted back.
10. `Kit_Passenger_Runner` (1.8): a lanky man in a long open coat sprinting for the train, one leg far forward, coat tails flying straight back, one arm thrust forward holding up a TICKET (small slab), hat flying off just behind his head (hat is a separate floating piece, connected to the head by a thin stone wisp so it stays attached).

Each new pose follows all the existing rules: 12–18 `Piece_XX` (body chunks + props + 3 base wedges), 6–9 `Crack_XX` on the surface (+1–2 on the base), 3 `Shard_X` hidden inside the torso, origins at piece centers, ≤ 5500 triangles, materials as above.

## Export / preview
- Export to `Game/Assets/Art/Characters/Ch2/<Name>.fbx` with the same FBX options as before. Print `EXPORTED <Name> <pieces> <cracks> <tris> <dims>` per statue and `DONE`.
- Overwrite the preview `Art/tools/specs/kit_ch2_passengers_preview.png` with all 10 statues in two rows of 5 (workbench, 3/4 top-down view, solid color by material) so a human can check the poses.
- Update the Korean header comment (now: 굳은 승객 석상 10종).
