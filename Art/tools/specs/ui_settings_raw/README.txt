Settings sprite build
====================
9 untouched image_gen originals are retained as *_raw.png.
The built-in image_gen tool was used; the exact prompts are in prompts.json.
tab_on and keycap_listen are derived from the normal sprites in Python.
arrow_left is an exact horizontal mirror of arrow_right.

Run from C:/Project/Cat_Tang with any Python that has Pillow and numpy:
  python Art/tools/ui_settings_post.py
  python Art/tools/ui_settings_post.py --verify-only

Verified interpreter in this workspace (PowerShell):
  & 'C:/Program Files/Blender Foundation/Blender 5.3/5.3/python/bin/python.exe' 'Art/tools/ui_settings_post.py'
Blender's bundled Python has numpy. Pillow 13.0.0 for that Python 3.13 is
retained locally in _deps; the script uses it only if the regular import fails.

Outputs:
  Game/Assets/Resources/UI/Settings/*.png (12 RGBA sprites)
  Art/tools/specs/ui_settings_borders.json (Unity L,B,R,T pixels)
  validation.json (per-sprite checks)
  final_review.png / frame_review.png / stretch_review.png (checker previews)

The checkerboard and sprite labels occur only in review images, never in assets.
Normal assets use only #000000 #F2B23A #1E1018 #F3E2C0 RGB.
Selected assets use #000000 #FFD36A #3A1A1E #F3E2C0 plus #FFC24A glow.
Soft glow has 14px finite support and alpha <=114/255, within equal-size canvases.
Nine-slice contours are re-inked to exact stroke widths after generation;
isolated rivet/gear details are extracted from the original gold masks.
All stretch columns and rows are copies of a clean column and row.
row has only two centre rows and is intended primarily for horizontal stretching.
