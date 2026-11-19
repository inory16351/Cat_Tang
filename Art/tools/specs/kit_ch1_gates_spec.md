You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_ch1_gates.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch1_gates.py`
After writing it, RUN it once with that command and fix errors until everything exports. Then look at the preview PNG and fix anything that reads badly.
Read and follow `Art/tools/specs/_gates_common.md` (piece types, sizes, naming, export). Use `Art/tools/blender_kit_woodgate.py` as the reference for pre-fractured pieces and setup / bevel / export style.
Export folder: `Game/Assets/Art/Characters/Ch1/`.

## Chapter 1 setting
A dusty old-west town melting like a Dali clock (melted clocks, gold drips are the signature accent), warm sunny desert. Stages go from outside to the center: badlands cliff path → town outskirts → streets / building interiors → near the round plaza; things get MORE melted each stage.
Allowed material names (grey, by role): `wood`, `wood_light`, `wood_dark`, `iron`, `brass`, `gold`, `gold_drip`, `canvas`, `cloth_red`, `cloth_teal`, `paint_teal`, `paint_red`, `paint_mustard`, `paint_cream`, `paint_blue`, `roof_tin`, `roof_shingle`, `roof_red`, `brick`, `stone`, `rock`, `rock_light`, `rock_dark`, `sand`, `hay`, `bone`, `window`, `window_lit`, `cream`.

## Stage looks
- S1 badlands cliff path — Gate: ranch gate of rough horizontal logs between two thick posts, rope lashings, a cow skull (`bone`) on top. Corridor: rocky canyon pass — floor of packed sand with scattered flat stones, side walls of stacked rocks / boulders (`rock`, `rock_dark`) with an occasional leaning wooden post. Portal: two chunky rock pillars with a crooked log lintel, a hanging rope and a small skull.
- S2 town outskirts — Gate: heavy plank barricade with its own design (vertical planks, crossed X boards, iron straps, a hand-painted warning board `paint_red`). Corridor: wooden boardwalk floor (planks across), porch railings and hitching posts on the sides, a couple of barrels. Portal: wooden frame with a blank town sign board hanging from the lintel, a gold drip running down one post.
- S3 streets / interiors — Gate: sheriff jail door — iron bar grid in a riveted iron frame with a brass lock plate (bars split into pieces, some bent). Corridor: narrow alley between low brick walls (`brick`, `stone` caps), stacked crates, a drain gutter in a stone-slab floor. Portal: brick arch with a small melted clock as the keystone (clock face `cream`, drooping, gold drip).
- S4 near the plaza (most melted) — Gate: ornate wrought-iron double gate with scrolls and a sun / clock medallion, half melting (gold drips hanging from the top bar). Corridor: paved stone path (`stone` tiles) lined with melting clock posts / lamp posts and low melting walls, gold drips pooling. Portal: melting clock-tower arch — tall pillars, a big drooping clock face on top sliding over the lintel.
- RewardDoor (chapter 1): saloon style — carved wooden frame with brass corner plates and a small gold drip, two swinging saloon leaves (`wood_light` with a `paint_teal` panel).
