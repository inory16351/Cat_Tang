# 꿈4 '마지막 장' (내부 키 dream6 = DreamOf(5)) 그림 정리: ~/Downloads/dream6_*.png → 원본 Art/story_src/dream6/ · 게임용 Resources/Story/Dreams/dream6/*.jpg (가로 1450)
#   그림 = 크롬 ChatGPT 유화 임파스토 1672x941 (2026-11-26). _j = 선 떨림용으로 살짝 일그러뜨린 판 (같은 그림 두 장 교대)
#   .meta = 꿈2 bell_a.jpg.meta 복사 + guid 새로 · grain_a~c = 꿈3(dream5) 것을 복사
# 실행: python docs/tools/dream6_import.py
import glob, os, shutil, uuid
from PIL import Image, ImageFilter

ROOT = r"C:/Project/Cat_Tang"
IN = os.path.expanduser("~/Downloads")
SRC = os.path.join(ROOT, "Art/story_src/dream6")
DST = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream6")
META = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream2/bell_a.jpg.meta")
JITTER = {"walk_a", "her_a", "lay_a", "clock_a", "child_b"}
os.makedirs(SRC, exist_ok=True); os.makedirs(DST, exist_ok=True)
meta_text = open(META, encoding="utf-8").read()

def write_meta(path):
    m = path + ".meta"
    if os.path.exists(m): return
    lines = ["guid: " + uuid.uuid4().hex + "\n" if l.startswith("guid:") else l for l in meta_text.splitlines(True)]
    open(m, "w", encoding="utf-8", newline="\n").write("".join(lines))

def jitter(im):
    # 붓질이 살아 움직이게: 아주 작은 물결 일그러뜨림 + 미세 회전
    w, h = im.size
    im2 = im.rotate(0.35, resample=Image.BICUBIC, expand=False)
    return im2.transform((w, h), Image.QUAD, (3, 2, 1, h - 3, w - 2, h - 1, w - 4, 3), resample=Image.BICUBIC)

for p in sorted(glob.glob(os.path.join(IN, "dream6_*.png"))):
    name = os.path.basename(p)[len("dream6_"):-4]
    shutil.copy2(p, os.path.join(SRC, name + ".png"))
    im = Image.open(p).convert("RGB")
    im = im.resize((1450, round(1450 * im.height / im.width)), Image.LANCZOS)
    out = os.path.join(DST, name + ".jpg"); im.save(out, quality=90); write_meta(out)
    if name in JITTER:
        o2 = os.path.join(DST, name + "_j.jpg"); jitter(im).save(o2, quality=90); write_meta(o2)
    print("ok", name)
for g in ("grain_a", "grain_b", "grain_c"):
    for ext in (".png",):
        s = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream5", g + ext)
        if os.path.exists(s):
            d = os.path.join(DST, g + ext); shutil.copy2(s, d); write_meta(d)
