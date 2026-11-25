# 2장 해금 이펙트 확인 GIF: Unity 편집 모드 촬영 프레임(Ch2UnlockFxBuilder.Capture) → Art/fx/ch2_unlock/<이름>.gif
#   python Art/tools/fx_ch2_gif.py [이름...]
# 프레임: Art/fx/ch2_unlock/frames/<이름>/f_000.png … (30fps). 끝에 0.4초 멈춤을 붙여 반복이 구분되게
import os, sys, glob
from PIL import Image, ImageDraw

ROOT = r"C:/Project/Cat_Tang/Art/fx/ch2_unlock"
FPS = 30
names = sys.argv[1:] or sorted(os.path.basename(p) for p in glob.glob(os.path.join(ROOT, "frames", "*")))
for n in names:
    fs = sorted(glob.glob(os.path.join(ROOT, "frames", n, "f_*.png")))
    if not fs:
        print("no frames", n); continue
    ims = []
    for i, f in enumerate(fs):
        im = Image.open(f).convert("RGB")
        d = ImageDraw.Draw(im)
        d.text((8, 6), f"{n}  {i / FPS:0.2f}s", fill=(200, 205, 215))
        ims.append(im.convert("P", palette=Image.ADAPTIVE, colors=200))
    durs = [round(1000 / FPS)] * len(ims)
    durs[-1] += 400
    out = os.path.join(ROOT, n + ".gif")
    ims[0].save(out, save_all=True, append_images=ims[1:], duration=durs, loop=0, disposal=2)
    print("GIF", out, len(ims))
