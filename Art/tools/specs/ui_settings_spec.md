# 환경설정 전용 UI 그림 (11:59) — Codex 명세

Use your image generation tool to create the settings-window UI sprites below, then post-process them with a Python script
(PIL/numpy) so they are clean flat sprites with real transparency. Work inside `C:\Project\Cat_Tang`.
Do NOT edit any `.cs` file, any `.meta` file, or anything outside the two folders named here. Do not run git.

- Raw generations → `Art/tools/specs/ui_settings_raw/<name>_raw.png` (keep them).
- Post-process script → `Art/tools/ui_settings_post.py` (re-runnable: raw → final).
- Final sprites → `Game/Assets/Resources/UI/Settings/<name>.png` (create the folder; do NOT create .meta files).

## Style — match the attached lobby UI images EXACTLY
The attached images are the game's existing lobby UI (`Resources/UI/Lobby/panel.png`, `menu_button.png`, `check_on.png`,
`slider_knob.png`, `icon_back.png`). New sprites must look like they belong to the same set:
- **Flat vector look, 4 flat colours only, no gradients, no texture, no painted shading, no metal sheen, no drop shadow:**
  - ink outline: pure black `#000000`
  - gold trim / gold shapes: `#F2B23A`
  - dark fill (panel inside): plum-black `#1E1018`
  - cream accents (clock faces, small highlights only): `#F3E2C0`
  - (only for the "on / listen" glow states: an extra lighter gold `#FFD36A` for an inner highlight line + soft glow, see below)
- Construction like the lobby panel / menu button: **outer black ink line → solid gold band → (thin black line) → flat plum-black inside.**
  Small decorations are simple flat shapes: round rivet dots, little clipped/chamfered corners, a tiny pocket-watch or
  gear, a pointed diamond tip — same simplicity as the lobby panel (chamfered corners + rivet dots) and menu_button (pointed ends + 4 dots).
- Theme: Western × clockwork (brass, pocket watch, gears, rivets). Hand-inked feel but crisp, thick, even line weight.
- No text, no letters, no numbers anywhere.

### Line weights at FINAL size (measured from the lobby set — keep them)
| sprite (final size) | outer black ink | gold band | inner black line |
|---|---|---|---|
| tab 512×168, keycap 384×160 | 7 px | 8 px | 2 px (optional) |
| row 1024×96 | 4 px | 5 px | 1 px |
| settings_frame 1600×1000 | 10 px | 22 px | 9 px |
| icons 256², arrows 128² | ~8 px / ~5 px outline around gold silhouettes | — | — |

## Generation tips
- Generate each sprite on a perfectly flat pure-green `#00FF00` background (no shadow, no floor, no vignette) so it can be keyed
  out; object centred with margin. Generate wide sprites at a wide aspect (e.g. 1536×1024 or wider) and crop.
- Pairs must be the SAME drawing in two states: generate the normal one first, then make the second by editing it
  (or recolour it in post) so silhouettes line up pixel-for-pixel.
- `arrow_left` = horizontal mirror of `arrow_right` (do it in post).

## Post-process (in `ui_settings_post.py`)
1. Key out the green background → alpha (remove green fringe / spill; no green pixels may remain).
2. Snap every opaque pixel to the nearest palette colour (black, gold, plum-black, cream; + light gold only in on/listen) at full
   resolution, so the result is truly flat like the lobby set.
3. Crop to the content, pad to the target aspect, resize with LANCZOS to the final size (anti-aliased edges are fine).
4. 9-slice sprites (`tab*`, `keycap*`, `row`, `settings_frame`): make the stretchable middle perfectly uniform — replace the central
   stretch columns (and rows) with copies of one clean column/row, so stretching shows no seams. All corner/end decoration must sit
   inside the border zones. Write the border you used (left, bottom, right, top in px, Unity order L,B,R,T) to
   `Art/tools/specs/ui_settings_borders.json`.
5. Glow states (`tab_on`, `keycap_listen`): same shape as the normal one, but the gold band uses the lighter gold `#FFD36A` with a thin
   cream inner highlight line, the inside fill slightly warmer (`#3A1A1E`), plus a soft outer glow (gold `#FFC24A`, alpha ≤ 0.45, 10–14 px)
   drawn behind the black outline inside the canvas (leave enough transparent margin for it; the normal state gets the SAME canvas
   size and margin so both line up).
6. Verify each final: size, RGBA, has transparent corners, no green pixels. Print a table.

## Sprites

### `tab.png` / `tab_on.png` — 512×168 (tab button, horizontal ~4:1.3)
A tab header button like a small version of the lobby menu button: chamfered (clipped) top corners, flat bottom edge (it sits on
the content frame), black ink → gold band → plum-black inside, a small gold rivet dot near each top corner. Left/right 64 px end zones
hold the decoration; middle is plain. `tab` = normal (as described). `tab_on` = selected glow state (see post-process 5).

### `icon_sound.png` · `icon_display.png` · `icon_game.png` · `icon_keys.png` — 256×256 each
Tab icons: solid gold `#F2B23A` silhouette with thick black ink outline, tiny cream accent allowed, no fill background, like
`icon_back.png` (gold arrow + black outline). Same scale/visual weight across all four, centred, ~12 px margin.
- sound: an old gramophone horn / bugle horn with 2 short sound-wave arcs
- display: an ornate picture frame / old monitor (rectangle frame with corner notches) — inside cream
- game: a pocket watch with a small gear behind it (cream dial, two gold hands, no numerals)
- keys: an antique skeleton key crossing a single square keycap

### `keycap.png` / `keycap_listen.png` — 384×160 (key-binding button, ~2.4:1)
A brass-rimmed typewriter-style keycap seen from the front: rounded-rect, black ink → gold rim band → plum-black face; a slightly
thicker gold lip at the bottom (gives a pressable depth), 4 tiny rivet dots at the corners. Plain centre for the key label.
`keycap_listen` = waiting-for-input glow state (post-process 5).

### `arrow_right.png` (+ mirrored `arrow_left.png`) — 128×128
Value-picker arrow: a solid gold triangle pointing right with black ink outline, a small gold round knob/rivet at its base and a tiny
curl flourish — same style as lobby `icon_back.png`'s arrowhead. Centred, ~10 px margin.

### `row.png` — 1024×96 (one settings line background, thin long strip)
A long thin plaque: plum-black body, thin gold top & bottom line with black ink, ends finished with a small pointed (diamond)
tip like menu_button's ends plus one rivet dot each end. Ends occupy ≤ 48 px each; the middle is uniform.

### `settings_frame.png` — 1600×1000 (content area frame under the tabs, ~1.6:1)
A frame like the lobby `panel.png` (chamfered corners, black → gold → black band) but **the inside is fully transparent** (only the
border is drawn). Corners: small chamfer + one rivet dot + a tiny gear/flourish, all within 96 px of each corner. Straight edges plain.
No pocket watch on top, no drips.
