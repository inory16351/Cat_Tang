# Tripo CLI 결과(tripo make ... -o Art/models/ch2/<이름>) → Unity 챕터 폴더로 들여오기
# 사용: python Art/tools/tripo_cli_import.py <이름(예: Ch2_Conductor)> [챕터 폴더 이름(기본 Ch2)]
# - Art/models/ch2/<이름>/tripo-out/*-rig-*/model.fbx (없으면 아무 model.fbx) → Game/Assets/Art/Characters/<챕터>/<이름>.fbx
# - FBX 안에 박힌 색 텍스처(JPEG/PNG)를 꺼내 <이름>_basecolor.jpg (2048, 품질 92) — 빌더(EnemyModelBuilder.ToonFor)가 이 파일을 씀
import glob
import io
import os
import shutil
import sys
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))


def embedded_images(data: bytes):
    out = []
    i = 0
    while True:                                     # JPEG: FF D8 FF … FF D9
        s = data.find(b'\xff\xd8\xff', i)
        if s < 0:
            break
        e = data.find(b'\xff\xd9', s)
        while e >= 0:
            try:
                im = Image.open(io.BytesIO(data[s:e + 2]))
                im.load()
                out.append(im)
                break
            except Exception:
                e = data.find(b'\xff\xd9', e + 2)
        i = s + 3
    i = 0
    while True:                                     # PNG: 시그니처 … IEND
        s = data.find(b'\x89PNG\r\n\x1a\n', i)
        if s < 0:
            break
        e = data.find(b'IEND', s)
        if e < 0:
            break
        try:
            im = Image.open(io.BytesIO(data[s:e + 8]))
            im.load()
            out.append(im)
        except Exception:
            pass
        i = s + 8
    return out


def main():
    name = sys.argv[1]
    chapter = sys.argv[2] if len(sys.argv) > 2 else 'Ch2'
    base = os.path.join(ROOT, 'Art', 'models', chapter.lower(), name)
    cands = glob.glob(os.path.join(base, '**', '*-rig-*', 'model.fbx'), recursive=True) or glob.glob(os.path.join(base, '**', 'model.fbx'), recursive=True)
    if not cands:
        raise SystemExit(f'no model.fbx under {base}')
    fbx = max(cands, key=os.path.getmtime)
    dst = os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Characters', chapter)
    os.makedirs(dst, exist_ok=True)
    shutil.copy(fbx, os.path.join(dst, name + '.fbx'))
    ims = embedded_images(open(fbx, 'rb').read())
    if ims:
        im = max(ims, key=lambda m: m.size[0] * m.size[1]).convert('RGB')
        if max(im.size) > 2048:
            im = im.resize((2048, 2048))
        im.save(os.path.join(dst, name + '_basecolor.jpg'), quality=92)
    print(name, os.path.getsize(fbx) // 1024, 'KB fbx,', f'tex {ims[0].size}' if ims else 'NO TEX')


if __name__ == '__main__':
    main()
