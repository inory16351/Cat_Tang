# PPTX 용량 줄이기 (슬라이드 · 글은 그대로, 그림만 다시 저장)
# - 투명하지 않은 PNG → JPEG(품질 Q)로 바꾸고 관계(rels) · 콘텐츠 형식을 고침
# - 긴 변이 MAX보다 큰 그림은 줄임 (16:9 슬라이드 전체 폭이 1920px이면 충분)
# - 투명한 PNG(아이콘 · HUD)는 PNG로 두고 크기만 줄이고 압축 최적화
# 실행: python docs/tools/pptx_compress.py <입력.pptx> [출력.pptx]   (출력을 안 주면 제자리에 덮어씀)
import io, os, re, sys, zipfile
from PIL import Image

MAX = 1920
Q = 86


def has_alpha(im):
    if im.mode in ('RGBA', 'LA') or (im.mode == 'P' and 'transparency' in im.info):
        a = im.convert('RGBA').getchannel('A')
        return a.getextrema()[0] < 250
    return False


def main():
    src = sys.argv[1]
    dst = sys.argv[2] if len(sys.argv) > 2 else src
    zin = zipfile.ZipFile(src)
    files = {n: zin.read(n) for n in zin.namelist()}
    order = zin.namelist()
    zin.close()
    rename = {}
    before = sum(len(v) for v in files.values())
    for name in list(files):
        if not name.startswith('ppt/media/') or not re.search(r'\.(png|jpe?g)$', name, re.I):
            continue
        data = files[name]
        try:
            im = Image.open(io.BytesIO(data)); im.load()
        except Exception:
            continue
        if max(im.size) > MAX:
            k = MAX / max(im.size)
            im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
        buf = io.BytesIO()
        if name.lower().endswith('.png') and has_alpha(im):
            im.save(buf, 'PNG', optimize=True)
            new_name = name
        else:
            im.convert('RGB').save(buf, 'JPEG', quality=Q, optimize=True, progressive=True)
            new_name = re.sub(r'\.(png|jpeg)$', '.jpg', name, flags=re.I)
        out = buf.getvalue()
        if len(out) >= len(data) and new_name == name:
            continue                                  # 줄지 않으면 원본 유지
        del files[name]
        files[new_name] = out
        if new_name != name:
            rename[name] = new_name
    # 같은 그림이 여러 장 들어 있으면 하나만 남김 (pptxgenjs는 쓸 때마다 따로 넣음)
    import hashlib
    seen = {}
    for n in sorted(files):
        if not n.startswith('ppt/media/'):
            continue
        h = hashlib.sha1(files[n]).hexdigest()
        if h in seen:
            keep = seen[h]
            del files[n]
            orig = next((o for o, v in rename.items() if v == n), n)
            rename[orig] = keep
            if orig != n:
                pass
            for o, v in list(rename.items()):
                if v == n: rename[o] = keep
            rename.setdefault(n, keep)
        else:
            seen[h] = n
    # 관계 파일의 경로 고치기
    for n in list(files):
        if n.endswith('.rels') and rename:
            t = files[n].decode('utf-8')
            for old, new in rename.items():
                t = t.replace('media/' + os.path.basename(old), 'media/' + os.path.basename(new))
            files[n] = t.encode('utf-8')
    # 콘텐츠 형식: jpg 기본 등록
    ct = files['[Content_Types].xml'].decode('utf-8')
    if 'Extension="jpg"' not in ct:
        ct = ct.replace('<Default ', '<Default Extension="jpg" ContentType="image/jpeg"/><Default ', 1)
    files['[Content_Types].xml'] = ct.encode('utf-8')
    names = [rename.get(n, n) for n in order]
    names += [n for n in files if n not in names]
    tmp = dst + '.tmp'
    written = set()
    with zipfile.ZipFile(tmp, 'w', zipfile.ZIP_DEFLATED) as z:
        for n in names:
            if n in files and n not in written:
                written.add(n)
                z.writestr(n, files[n], zipfile.ZIP_STORED if n.endswith(('.jpg', '.jpeg', '.png')) else zipfile.ZIP_DEFLATED)
    os.replace(tmp, dst)
    after = os.path.getsize(dst)
    print(f'{before / 1e6:.1f}MB (풀린 크기) → 파일 {after / 1e6:.1f}MB · 그림 {len(rename)}개 JPEG로')


if __name__ == '__main__':
    main()
