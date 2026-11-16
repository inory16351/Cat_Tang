# Tripo 입력 전 GPT 이미지 디노이즈: 미디언(가장자리 보존) 두 번 → 아주 약한 블러로 결 정리
# 사용: python Art/tools/denoise.py <입력.png> [...]  →  같은 폴더에 <이름>_dn.png
import sys
from pathlib import Path
from PIL import Image, ImageFilter


def denoise(src: Path) -> Path:
    im = Image.open(src).convert("RGB")
    im = im.filter(ImageFilter.MedianFilter(3)).filter(ImageFilter.MedianFilter(3))
    im = im.filter(ImageFilter.GaussianBlur(0.4))
    out = src.with_name(src.stem + "_dn.png")
    im.save(out)
    return out


if __name__ == "__main__":
    for a in sys.argv[1:]:
        print(denoise(Path(a)))
