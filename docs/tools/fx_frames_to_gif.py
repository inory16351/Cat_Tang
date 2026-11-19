# 이펙트 미리보기 프레임(FxRecorder: f_000.png ...) → GIF
# 실행: python docs/tools/fx_frames_to_gif.py <프레임 폴더> <출력.gif> <가운데 x> <가운데 y(화면 아래 기준, Unity)> [자를 폭] [자를 높이] [fps] [출력 폭]
import glob, os, sys
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
cx, cy = float(sys.argv[3]), float(sys.argv[4])
cw = int(sys.argv[5]) if len(sys.argv) > 5 else 760
ch = int(sys.argv[6]) if len(sys.argv) > 6 else 560
fps = float(sys.argv[7]) if len(sys.argv) > 7 else 30
ow = int(sys.argv[8]) if len(sys.argv) > 8 else 560
files = sorted(glob.glob(os.path.join(src, 'f_*.png')))
frames = []
for f in files:
    im = Image.open(f).convert('RGB')
    W, H = im.size
    y = H - cy                                     # Unity 화면 좌표는 아래가 0
    l = int(min(max(0, cx - cw / 2), W - cw)); t = int(min(max(0, y - ch / 2), H - ch))
    c = im.crop((l, t, l + cw, t + ch)).resize((ow, int(ow * ch / cw)), Image.LANCZOS)
    frames.append(c.quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, optimize=True, disposal=2)
print('gif', out, len(frames), 'frames')
