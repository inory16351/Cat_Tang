# 코스튬 효과 미리보기 프레임(Art/fx/costume/preview/<효과>/f_*.png, blender_fx_costume.py) → docs/images/costume_fx/<효과>.gif
# 실행: python docs/tools/costume_fx_gif.py [효과...]   (기본: 전부, 30fps, 끝에 0.4초 멈춤)
import glob, os, sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
SRC = os.path.join(ROOT, "Art", "fx", "costume", "preview")
OUT = os.path.join(ROOT, "docs", "images", "costume_fx")
os.makedirs(OUT, exist_ok=True)
names = sys.argv[1:] or sorted(os.listdir(SRC))
for n in names:
    fs = sorted(glob.glob(os.path.join(SRC, n, "f_*.png")))
    frames = [Image.open(f).convert("RGB").quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE) for f in fs]
    dur = [33] * len(frames); dur[-1] = 400
    out = os.path.join(OUT, f"{n}.gif")
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=dur, loop=0, optimize=True, disposal=2)
    print("gif", out, len(frames), f"{os.path.getsize(out) // 1024}KB")
