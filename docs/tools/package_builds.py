# 빌드 정리 (2026-11-23): Game/Builds/Windows · macOS → Game/Builds/dist/1159_<날짜>_Windows.zip · 1159_<날짜>_macOS.zip
#   맥 zip은 실행 비트(0755)를 넣어 둠: .app/Contents/MacOS/* · '처음 실행.command' → 맥에서 풀면 바로 실행 권한 유지
#   python docs/tools/package_builds.py [windows|mac|all]
import datetime, os, sys, zipfile

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..', 'Game', 'Builds'))
DIST = os.path.join(ROOT, 'dist')
STAMP = datetime.date.today().strftime('%Y%m%d')


def pack(src, out, exec_rule):
    n = 0
    with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        base = os.path.dirname(src)
        for dp, dns, fns in os.walk(src):
            for f in fns:
                p = os.path.join(dp, f)
                arc = os.path.relpath(p, base).replace('\\', '/')
                if '_BurstDebugInformation_DoNotShip' in arc or '_BackUpThisFolder_ButDontShipItWithYourGame' in arc:
                    continue
                zi = zipfile.ZipInfo.from_file(p, arc)
                zi.compress_type = zipfile.ZIP_DEFLATED
                mode = 0o755 if exec_rule(arc) else 0o644
                zi.external_attr = (0o100000 | mode) << 16
                zi.create_system = 3
                with open(p, 'rb') as fh:
                    z.writestr(zi, fh.read(), compresslevel=6)
                n += 1
    print(f'{out}  ({n} files, {os.path.getsize(out) / 1048576:.0f} MB)')


def main(which):
    os.makedirs(DIST, exist_ok=True)
    if which in ('windows', 'all'):
        src = os.path.join(ROOT, 'Windows')
        if os.path.exists(os.path.join(src, '1159.exe')):
            pack(src, os.path.join(DIST, f'1159_{STAMP}_Windows.zip'), lambda a: False)
        else:
            print('Windows 빌드 없음')
    if which in ('mac', 'all'):
        src = os.path.join(ROOT, 'macOS')
        if os.path.isdir(os.path.join(src, '1159.app')):
            pack(src, os.path.join(DIST, f'1159_{STAMP}_macOS.zip'), lambda a: '/Contents/MacOS/' in a or a.endswith('.command'))
        else:
            print('macOS 빌드 없음 (1159.app)')


main(sys.argv[1] if len(sys.argv) > 1 else 'all')
