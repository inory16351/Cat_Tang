# 5장 3D 이펙트 굽기 (블렌더) — Codex 작업 명세

Edit ONLY `Art/tools/blender_bake_fx.py` (add new functions and register them in `SETS`). Do not change existing bake functions. Then run the bake and check the results.

## Context
- `Art/tools/blender_bake_fx.py` bakes 3D toon effects into transparent flipbook PNGs: `Game/Assets/Resources/FX/anim/bk_<set>_<frame>.png` (256 px square, 16 frames), using the existing helpers (`reset`, `toon_mat`, `ink4`/`ink`, `gem_obj`, `shard_obj`, `star_obj`, `laser_rod`, `ribbon_obj`, `ribbon_layers`, `flat_mat`, `setv`, `ease_out`, `render`). Read the 3rd-chapter (`bake_c3orb`, `bake_c3shard`, `bake_c3spark`) and 4th-chapter (`bake_c4orb`, `bake_c4needle`, `bake_c4spark`, `bake_c4strip`) sets first and follow the same camera, ink outline, toon band and dissolve conventions.
- Chapter 5 look: cosmic purple-and-gold void, broken mirror glass, gold cracks. ENEMY attack colour = hot magenta (#FF2FB4) / magenta-pink (#FF6FD0) with white-hot cores, deep violet (#3A1A6A) shade, small gold (#FFC64A) accents, black ink outline.

## New sets (each a `bake_c5*` function, registered in SETS)
1. `c5orb` — enemy void orb bullet, loops (frame 15 → 0 seamless): a faceted dark-violet gem core with a magenta toon rim, a thin gold ring orbiting it (tilted ~30°), 3 tiny white glass chips orbiting. Gentle pulse of the rim heat.
2. `c5shard` — enemy crystal-shard bullet, loops: an elongated sharp crystal splinter (use `shard_obj`, pale lavender → magenta toon bands, white hot edge) pointing RIGHT (+X in camera view, the game rotates it to the flight direction), spinning around its long axis, with 2 tiny chips trailing behind on the left.
3. `c5spark` — hit / bounce / split burst, one-shot: white-magenta 4-point flash star (frames 0-2), then 6-8 triangular glass shards burst outward and spin, a thin magenta shock ring expands, 6 gold spark rods fly out; everything dissolves by frame 15.
4. `c5rift` — the room hazard "crack in the air", one-shot 16 frames, vertical in screen: frames 0-7 a jagged lightning-like crack line GROWS from the centre up and down with side branches (white-hot core rods inside a magenta glow band, black ink), frame 8-9 it bulges into a lens of deep violet with tiny white stars; frames 10-15 it shatters: 10-14 glass shards fly outward with a magenta ring, crack lines dissolve.
5. `c5strip` — ribbon tail texture for eye trails / blade streaks (same layout as `c4strip`: long horizontal ribbon, head on the LEFT white-hot, body magenta-pink, tail on the RIGHT fading to violet with a swallow-tail notch). Same frame count and file naming as `c4strip`.

## Run & check
- Bake only the new sets:
  `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_bake_fx.py -- c5orb c5shard c5spark c5rift c5strip`
- Confirm every `bk_c5*_N.png` exists with alpha (transparent background) and the effect is not clipped at the frame edge. Make a contact sheet PNG of all five sets at `Art/tools/specs/fx_ch5_bake_preview.png` (PIL is fine) and look at it; fix and re-bake anything clipped, invisible or wrongly coloured.
- Print `DONE` at the end with the list of baked sets.
