from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).parent / '_deps'))
from PIL import Image, ImageOps, ImageDraw
files = sorted(Path(__file__).parent.glob('*_raw.png'))
sheet = Image.new('RGB', (1000, 300 * len(files)), (80, 80, 80))
d = ImageDraw.Draw(sheet)
for i, p in enumerate(files):
    sheet.paste(ImageOps.contain(Image.open(p), (970, 260)), (15, i * 300 + 30))
    d.text((15, i * 300 + 6), p.stem, fill='white')
sheet.save(Path(__file__).parent / 'raw_review.png')
