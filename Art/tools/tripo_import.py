# Tripo 내보내기 zip(FBX + .fbm 텍스처) → Unity 1장 폴더로 들여오기
# 사용: python Art/tools/tripo_import.py <zip> <Unity 이름(예: Ch1_Outlaw)> [Unity 폴더]
# - 원본은 Art/models/ch1/<이름>/ 에 풀어 둠 (git 제외)
# - FBX → <폴더>/<이름>.fbx, 원본 색 텍스처 → <이름>_basecolor.jpg (2048, 품질 92)
import glob
import os
import shutil
import sys
import zipfile
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))


def main():
    src_zip, name = sys.argv[1], sys.argv[2]
    dst = sys.argv[3] if len(sys.argv) > 3 else os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Characters', 'Ch1')
    raw = os.path.join(ROOT, 'Art', 'models', 'ch1', name)
    shutil.rmtree(raw, ignore_errors=True)
    os.makedirs(raw)
    with zipfile.ZipFile(src_zip) as z:
        for m in z.namelist():                      # zip slip 방지: 폴더 밖으로 나가는 이름은 건너뜀
            p = os.path.normpath(os.path.join(raw, m))
            if not p.startswith(raw):
                continue
            z.extract(m, raw)
    fbx = glob.glob(os.path.join(raw, '*.fbx'))[0]
    os.makedirs(dst, exist_ok=True)
    shutil.copy(fbx, os.path.join(dst, name + '.fbx'))
    tex = [t for t in glob.glob(os.path.join(raw, '*.fbm', '*')) if 'basecolor' in t.lower() or 'tripo_image' in t.lower()]
    if tex:
        im = Image.open(tex[0]).convert('RGB')
        if max(im.size) > 2048:
            im = im.resize((2048, 2048))
        im.save(os.path.join(dst, name + '_basecolor.jpg'), quality=92)
    print(name, os.path.getsize(fbx) // 1024, 'KB fbx,', 'tex' if tex else 'NO TEX', os.path.basename(tex[0]) if tex else '')


if __name__ == '__main__':
    main()
