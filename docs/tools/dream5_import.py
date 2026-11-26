# 꿈3 '11:59의 기원' (내부 키 dream5) 그림 정리: 받은 d5_*.png → 원본 Art/story_src/dream5/ · 게임용 Resources/Story/Dreams/dream5/ (접두사 d5_ 뺌)
#   그림 = Claude 내장 브라우저 ChatGPT (흑백 리노컷 + 빨강 한 점, 1672x941). 받기 = 페이지에서 생성 이미지를 fetch →
#   multipart form(target _self)으로 127.0.0.1 수신 서버에 보내고 303으로 대화로 되돌아옴 (ChatGPT CSP가 fetch · iframe · 클립보드를 막아서, form-action만 report-only)
#   배경 · 셀 = 가로 1450 JPG (꿈2와 같은 크기 · 저장소 크기), 필름 결(grain_a~c) = 이 스크립트가 만든 투명 PNG (먼지 · 긁힘 · 판 번짐)
#   .meta = 꿈2 bell_a.jpg.meta를 복사해 guid만 새로 (같은 임포트 설정)
# 실행: python docs/tools/dream5_import.py <받은 폴더>   (그 다음 docs/tools/dream5_sheet.py 로 콘택트 시트)
import glob, os, shutil, sys, uuid, random
from PIL import Image, ImageDraw, ImageFilter

ROOT = r"C:/Project/Cat_Tang"
IN = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser("~/Downloads")
SRC = os.path.join(ROOT, "Art/story_src/dream5")
DST = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream5")
META = os.path.join(ROOT, "Game/Assets/Resources/Story/Dreams/dream2/bell_a.jpg.meta")
# 좌우 반전해서 씀: ChatGPT가 빨간 분침을 12시 오른쪽(= 12:01)에 그린 컷 → 뒤집어 12시 왼쪽(= 11:59)으로 (글자 없는 그림이라 반전 티 안 남)
FLIP = {"minute_a", "minute_b", "minute_c"}
os.makedirs(SRC, exist_ok=True)
os.makedirs(DST, exist_ok=True)
meta_text = open(META, encoding="utf-8").read()


def write_meta(path):
    m = path + ".meta"
    if os.path.exists(m):
        return                                            # 이미 있으면 guid 유지 (다시 돌려도 참조가 안 깨짐)
    lines = meta_text.splitlines(True)
    lines = ["guid: " + uuid.uuid4().hex + "\n" if l.startswith("guid:") else l for l in lines]
    open(m, "w", encoding="utf-8", newline="\n").write("".join(lines))


n = 0
for f in sorted(glob.glob(os.path.join(IN, "d5_*.png"))):
    name = os.path.basename(f)[3:-4]
    shutil.copy2(f, os.path.join(SRC, name + ".png"))
    im = Image.open(f).convert("RGB")
    if name in FLIP:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    if im.width < im.height * 1.3:
        print("세로 비율 — 다시 뽑을 것:", name, im.size)
    w = 1450
    out = os.path.join(DST, name + ".jpg")
    im.resize((w, round(im.height * w / im.width)), Image.LANCZOS).save(out, quality=92)
    write_meta(out)
    n += 1


def grain(seed, path):
    """필름 결 한 장: 투명 바탕에 잉크 먼지 · 가는 흰 긁힘 · 판 가장자리 번짐 (리노컷 인쇄 결)"""
    W, H = 960, 540
    rnd = random.Random(seed)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for _ in range(260):                                   # 잉크 먼지 (작은 점 · 덩어리)
        x, y, r = rnd.uniform(0, W), rnd.uniform(0, H), rnd.choice([0.6, 0.8, 1, 1.2, 1.6, 2.4])
        a = rnd.randint(90, 200)
        d.ellipse((x - r, y - r * rnd.uniform(0.6, 1.2), x + r, y + r), fill=(10, 8, 8, a))
    for _ in range(rnd.randint(2, 4)):                     # 세로 긁힘 (흰 선, 살짝 휨)
        x = rnd.uniform(0.05, 0.95) * W
        y0, y1 = rnd.uniform(0, H * 0.4), rnd.uniform(H * 0.6, H)
        pts, y = [], y0
        while y < y1:
            pts.append((x, y)); x += rnd.uniform(-1.2, 1.2); y += rnd.uniform(8, 20)
        d.line(pts, fill=(245, 240, 228, rnd.randint(60, 130)), width=1)
    for _ in range(rnd.randint(3, 6)):                     # 머리카락 같은 짧은 검은 실
        x, y = rnd.uniform(0, W), rnd.uniform(0, H)
        pts = [(x, y)]
        for _ in range(rnd.randint(4, 9)):
            x += rnd.uniform(-9, 9); y += rnd.uniform(-9, 9); pts.append((x, y))
        d.line(pts, fill=(12, 10, 10, rnd.randint(70, 140)), width=1)
    im = im.filter(ImageFilter.GaussianBlur(0.35))
    im.save(path)
    write_meta(path)


for i, k in enumerate("abc"):
    grain(1159 + i * 31, os.path.join(DST, f"grain_{k}.png"))
print("imported", n, "+ grain 3")
