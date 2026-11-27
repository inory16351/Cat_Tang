# -*- coding: utf-8 -*-
"""11:59 다국어(영어 밖) 사전 도구 (2026-11-27: 프랑스어 · 러시아어 · 일본어 · 중국어 간체 · 독일어 · 아랍어)

사전 = Game/Assets/Resources/Lang/<code>.json  {"entries":[{"ko":"…","en":"…","<code>":"…"}]}
  ko = 찾는 열쇠(원문), en = 참고(검수된 영어), <code> = 그 언어 번역. 항목 순서 = en.json 순서

사용:
  python Art/tools/loc_lang.py split <code> [크기=250]   en.json → Art/tools/loc_work/<code>/in_NN.json  ({id, ko, en, src})
  python Art/tools/loc_lang.py merge <code>              loc_work/<code>/out_NN.json ({id, <code>}) 모아 → Lang/<code>.json
  python Art/tools/loc_lang.py check <code>|all          자리 표시 · 태그 짝 · 줄바꿈 · 앞뒤 공백 · 한글 남음 · 말줄임 · 빠진 항목
  python Art/tools/loc_lang.py apply <code> <fix.json>   검수 반영: [{"id":n, "<code>":"…"}] (id = en.json 순서)
"""
import glob
import io
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LANG_DIR = os.path.join(ROOT, "Game", "Assets", "Resources", "Lang")
WORK = os.path.join(ROOT, "Art", "tools", "loc_work")
SOURCES = os.path.join(ROOT, "Art", "tools", "specs", "loc_sources.json")
CODES = ["fr", "ru", "ja", "zh", "de", "ar"]

HANGUL = re.compile(r"[가-힣]")
HOLE = re.compile(r"\{(\d+)\}")
TAG = re.compile(r"</?([a-zA-Z]+)(=[^>]*)?>")


def load_en():
    return json.load(io.open(os.path.join(LANG_DIR, "en.json"), encoding="utf-8"))["entries"]


def dump(path, obj):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=1)
        f.write("\n")


def split(code, size=250):
    en = load_en()
    src = json.load(io.open(SOURCES, encoding="utf-8"))
    d = os.path.join(WORK, code)
    for k, i in enumerate(range(0, len(en), size)):
        chunk = [{"id": i + j, "ko": e["ko"], "en": e["en"], "src": src.get(e["ko"], [])[:2]} for j, e in enumerate(en[i:i + size])]
        dump(os.path.join(d, "in_%02d.json" % k), chunk)
    print("%s: %d개 → %d 묶음 (%s)" % (code, len(en), (len(en) + size - 1) // size, os.path.relpath(d, ROOT)))


def merge(code):
    en = load_en()
    got = {}
    for p in sorted(glob.glob(os.path.join(WORK, code, "out_*.json"))):
        for e in json.load(io.open(p, encoding="utf-8")):
            got[int(e["id"])] = e[code]
    entries = [{"ko": e["ko"], "en": e["en"], code: got.get(i, "")} for i, e in enumerate(en)]
    dump(os.path.join(LANG_DIR, code + ".json"), {"entries": entries})
    print("%s: %d / %d 번역 → Lang/%s.json" % (code, sum(1 for x in entries if x[code]), len(en), code))


def tag_ok(s):
    stack = []
    for m in TAG.finditer(s):
        name = m.group(1).lower()
        if m.group(0).startswith("</"):
            if not stack or stack[-1] != name:
                return False
            stack.pop()
        else:
            stack.append(name)
    return not stack


def check(code):
    p = os.path.join(LANG_DIR, code + ".json")
    if not os.path.exists(p):
        print("%s: 사전 없음" % code); return 1
    en = load_en()
    entries = json.load(io.open(p, encoding="utf-8"))["entries"]
    errs = []
    if [e["ko"] for e in entries] != [e["ko"] for e in en]:
        errs.append("항목 목록 · 순서가 en.json 과 다름 (merge 다시)")
    for i, e in enumerate(entries):
        ko, t = e.get("ko", ""), e.get(code, "")
        if not t or not t.strip():
            errs.append("#%d 빈 칸: %r" % (i, ko)); continue
        if set(HOLE.findall(ko)) != set(HOLE.findall(t)):
            errs.append("#%d 자리 표시 다름: %r → %r" % (i, ko, t))
        if sorted(m.group(0) for m in TAG.finditer(ko)) != sorted(m.group(0) for m in TAG.finditer(t)):
            errs.append("#%d 태그 다름: %r → %r" % (i, ko, t))
        if not tag_ok(t):
            errs.append("#%d 태그 짝 안 맞음: %r" % (i, t))
        if t.count("\n") != ko.count("\n"):
            errs.append("#%d 줄 수 다름: %r → %r" % (i, ko, t))
        if t != t.strip():
            errs.append("#%d 앞뒤 공백: %r" % (i, t))
        if HANGUL.search(t):
            errs.append("#%d 한글 남음: %r" % (i, t))
        if "..." in ko and "..." not in t:
            errs.append("#%d 말줄임 빠짐: %r → %r" % (i, ko, t))
        if " · " in ko and " · " not in t and ko.count(" · ") > 0 and len(ko) < 40:
            errs.append("#%d ' · ' 구분 빠짐: %r → %r" % (i, ko, t))
    print("%s: 항목 %d · 오류 %d" % (code, len(entries), len(errs)))
    for x in errs[:80]:
        print("  ", x)
    return 1 if errs else 0


def apply(code, fix_path):
    p = os.path.join(LANG_DIR, code + ".json")
    data = json.load(io.open(p, encoding="utf-8"))
    fixes = json.load(io.open(fix_path, encoding="utf-8"))
    for f in fixes:
        data["entries"][int(f["id"])][code] = f[code]
    dump(p, data)
    print("%s: %d개 고침" % (code, len(fixes)))


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    a = sys.argv[1:]
    if not a:
        print(__doc__); return 1
    if a[0] == "split":
        split(a[1], int(a[2]) if len(a) > 2 else 250); return 0
    if a[0] == "merge":
        merge(a[1]); return 0
    if a[0] == "check":
        codes = CODES if a[1] == "all" else [a[1]]
        return max(check(c) for c in codes)
    if a[0] == "apply":
        apply(a[1], a[2]); return 0
    print(__doc__); return 1


if __name__ == "__main__":
    sys.exit(main())
