# -*- coding: utf-8 -*-
"""11:59 번역 문구 추출기.

Game/Assets/Scripts/**/*.cs 와 Game/Assets/Data/Tables/*.csv 에서 화면에 나올 한국어 문구를 뽑는다.
  - .cs: 일반 "..." · 축자 @"..." · 보간 $"..." 문자열. 보간 구멍 {식[,정렬][:서식]} 은 나오는 순서대로 {0} {1} ...
         "a" + 식 + "b" 처럼 문자열과 식을 + 로 이은 사슬은 "a{0}b" 틀 하나로 (식 자리 = 구멍)
         받침 조사 함수(Eul) 구멍은 '을' · '를' 을 넣은 항목 둘로
  - 대사 접두어 '이름#표정::' 은 떼고, 이름은 따로 한 항목
  - 여러 줄 문구는 \\n 으로 나눠 한 줄 = 한 항목. 실행 중에 이어 붙는 글(구멍 · + 사슬)은 ' · ' 조각도 따로 한 항목
    (런타임 Loc.T 가 통째로 → 줄마다 → ' · ' 조각마다 → 자리 표시 틀 → 숫자 틀 순으로 찾음)
  - 빼는 것: 주석 · #지시문, Debug.Log*, [MenuItem] [Tooltip] [Header] 등 특성, Meta.StorySeen/MarkStory 키,
             PlayerPrefs 키, Resources.Load 경로, 파일 경로, new GameObject 이름, Regex, == / != / case 비교 값,
             조사 한 글자, Loc.cs(언어 이름은 제 언어로), KEEP(사람 이름 등 번역 안 함)
  - CSV: 한글이 든 칸. Enum.csv 의 Value · Value2 · CondValue · Duration 자리 표시는 {n} 으로 바꾼 항목도 함께

쓰기: Art/tools/specs/loc_sources.json  {"원문": ["파일:줄", ...], ...}  (번역 맥락용, 파일 · 줄 순서 그대로)
      --check  : Game/Assets/Resources/Lang/en.json 을 검사 (빈 칸 · 중복 · 자리 표시 집합 · 태그 짝 · 줄바꿈 ·
                 앞뒤 공백 · 한글 남음 · 말줄임) + 추출 결과와 비교해 빠진 항목 · 남는 항목
사용: python Art/tools/loc_extract.py [--check]
"""
import csv
import glob
import io
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ASSETS = os.path.join(ROOT, "Game", "Assets")
SCRIPTS = os.path.join(ASSETS, "Scripts")
TABLES = os.path.join(ASSETS, "Data", "Tables")
OUT_SOURCES = os.path.join(ROOT, "Art", "tools", "specs", "loc_sources.json")
LANG_EN = os.path.join(ASSETS, "Resources", "Lang", "en.json")

HANGUL = re.compile(r"[가-힣]")
HOLE = re.compile(r"\{(\d+)\}")

# 이 호출 · 특성 안의 문자열은 화면에 안 나옴 (전체 이름)
EXCLUDE_CALLS = {
    "Debug.Assert", "Debug.LogException",
    "CreateAssetMenu", "AddComponentMenu", "InspectorName", "ContextMenuItem", "Obsolete", "Conditional",
    "PlayerPrefs.GetString", "PlayerPrefs.SetString", "PlayerPrefs.GetInt", "PlayerPrefs.SetInt",
    "PlayerPrefs.GetFloat", "PlayerPrefs.SetFloat", "PlayerPrefs.HasKey", "PlayerPrefs.DeleteKey",
    "Resources.Load", "Resources.LoadAll", "AssetDatabase.LoadAssetAtPath", "Shader.Find",
    "Path.Combine", "File.WriteAllText", "File.ReadAllText", "File.Exists", "Directory.Exists",
    "Application.OpenURL", "EditorUtility.DisplayProgressBar",
    "GameObject", "Regex.Match", "Regex.Replace", "Regex.IsMatch", "Regex.Split",
}
# 마지막 이름만 같아도 뺌 (UnityEditor.MenuItem, UnityEngine.Debug.Log ...)
EXCLUDE_LAST = {"Log", "LogWarning", "LogError", "LogFormat", "LogWarningFormat", "LogErrorFormat",
                "MenuItem", "Tooltip", "Header", "ContextMenu", "StorySeen", "MarkStory", "StoryPrefKey", "Regex"}
# 통째로 빼는 파일 (Loc.cs = 언어 고르기의 언어 이름은 제 언어로 보여야 함)
EXCLUDE_FILES = {"Scripts/Core/Loc.cs"}
# 번역하지 않고 원문 그대로 두는 글 (사람 이름) — en.json 에 넣지 않음
KEEP = {"정승운"}
# 화면에 안 나오는 표정 토큰 (대사 '이름#표정::' 의 표정, GameUI.ExprId) — 번역 · 추출 안 함
KEEP |= {"기본", "두통", "결의", "의문", "씩", "놀람", "쓸쓸", "다정", "엄숙", "의미심장"}
# 받침에 따라 붙는 조사 함수: 구멍 대신 조사 둘을 넣은 항목을 따로 뽑음 (ClockQuest.Eul)
PARTICLE_FUNCS = {"Eul": ("을", "를")}
PARTICLES = {"을", "를", "이", "가", "은", "는", "와", "과", "으로", "로", "이나", "나"}
TAG = re.compile(r"</?([a-zA-Z]+)(=[^>]*)?>")
FILE_PATH = re.compile(r"^[^\s]*[/\\][^\s]*$|^[^\s]+\.(png|jpg|wav|ogg|mp3|json|csv|asset|prefab|mat|fbx|unity|txt|md)$", re.I)


# ---------------------------------------------------------------- C# 토큰
class Tok:
    __slots__ = ("kind", "text", "line", "value", "holes", "hsrc", "calls")

    def __init__(self, kind, text, line, value=None, holes=0, hsrc=()):
        self.kind = kind      # 'str' | 'id' | 'op' | 'num' | 'char'
        self.text = text
        self.line = line
        self.value = value    # str: 풀린 값 (보간 구멍은 \x00k\x00 표시)
        self.holes = holes    # str: 구멍 개수
        self.hsrc = hsrc      # str: 구멍 식 원문
        self.calls = ()       # str: 둘러싼 호출 이름들 (안쪽부터)


ESC = {"n": "\n", "t": "\t", "r": "\r", "\\": "\\", '"': '"', "'": "'", "0": "\0", "a": "\a", "b": "\b", "f": "\f", "v": "\v"}


class Lexer:
    def __init__(self, src):
        self.s = src
        self.i = 0
        self.line = 1
        self.inner = []  # 보간 구멍 안의 토큰 목록들 (구멍마다 하나 — 그 안의 + 사슬도 틀로)

    def peek(self, k=0):
        j = self.i + k
        return self.s[j] if j < len(self.s) else ""

    def adv(self, n=1):
        for _ in range(n):
            if self.i < len(self.s):
                if self.s[self.i] == "\n":
                    self.line += 1
                self.i += 1

    def at_line_start(self):
        return self.s[self.s.rfind("\n", 0, self.i) + 1:self.i].strip() == ""

    def run(self, in_hole=False):
        """in_hole: 보간 구멍 안 — 짝 없는 '}' 나 맨 바깥 ',' ':'(정렬 · 서식)에서 멈춤"""
        depth = 0
        toks = []
        while self.i < len(self.s):
            c = self.peek()
            if c in " \t\r\n\f":
                self.adv(); continue
            if c == "/" and self.peek(1) == "/":
                while self.i < len(self.s) and self.peek() != "\n":
                    self.adv()
                continue
            if c == "/" and self.peek(1) == "*":
                self.adv(2)
                while self.i < len(self.s) and not (self.peek() == "*" and self.peek(1) == "/"):
                    self.adv()
                self.adv(2); continue
            if c == "#" and not in_hole and self.at_line_start():
                while self.i < len(self.s) and self.peek() != "\n":
                    self.adv()
                continue
            if c in "$@\"":
                j = self.i
                pre = ""
                while j < len(self.s) and self.s[j] in "$@" and len(pre) < 2:
                    pre += self.s[j]; j += 1
                if j < len(self.s) and self.s[j] == '"':
                    line = self.line
                    self.adv(len(pre))
                    toks.append(self.string("$" in pre, "@" in pre, line))
                    continue
            if c == "'":
                line = self.line
                self.adv()
                while self.i < len(self.s) and self.peek() != "'":
                    if self.peek() == "\\":
                        self.adv()
                    self.adv()
                self.adv()
                toks.append(Tok("char", "'", line)); continue
            if c.isalpha() or c == "_":
                j = self.i
                while j < len(self.s) and (self.s[j].isalnum() or self.s[j] == "_"):
                    j += 1
                toks.append(Tok("id", self.s[self.i:j], self.line)); self.adv(j - self.i); continue
            if c.isdigit():
                j = self.i
                while j < len(self.s) and (self.s[j].isalnum() or self.s[j] in "._"):
                    j += 1
                toks.append(Tok("num", self.s[self.i:j], self.line)); self.adv(j - self.i); continue
            two = self.s[self.i:self.i + 2]
            if two in ("==", "!=", "=>", "+=", "-=", "&&", "||", "??", "<=", ">=", "::", "?.", "++", "--", "?["):
                if in_hole and depth == 0 and two == "::":
                    pass
                toks.append(Tok("op", two, self.line)); self.adv(2); continue
            if in_hole:
                if c in "([{":
                    depth += 1
                elif c in ")]}":
                    if depth == 0 and c == "}":
                        return toks
                    depth -= 1
                elif c in ",:" and depth == 0:
                    while self.i < len(self.s) and self.peek() != "}":
                        self.adv()
                    return toks
            toks.append(Tok("op", c, self.line)); self.adv()
        return toks

    def string(self, interp, verbatim, line):
        self.adv()  # 여는 "
        parts = []
        holes = 0
        hsrc = []
        while self.i < len(self.s):
            c = self.peek()
            if verbatim and c == '"':
                if self.peek(1) == '"':
                    parts.append('"'); self.adv(2); continue
                self.adv(); break
            if not verbatim and c == "\\":
                n = self.peek(1)
                if n == "u":
                    parts.append(chr(int(self.s[self.i + 2:self.i + 6], 16))); self.adv(6); continue
                parts.append(ESC.get(n, n)); self.adv(2); continue
            if not verbatim and c == '"':
                self.adv(); break
            if interp and c == "{":
                if self.peek(1) == "{":
                    parts.append("{"); self.adv(2); continue
                self.adv()
                h0 = self.i
                sub = self.run(in_hole=True)
                hsrc.append(self.s[h0:self.i])
                self.adv()  # 닫는 }
                if any(t.kind == "str" for t in sub):
                    self.inner.append(sub)
                parts.append("\x00%d\x00" % holes)
                holes += 1
                continue
            if interp and c == "}" and self.peek(1) == "}":
                parts.append("}"); self.adv(2); continue
            parts.append(c); self.adv()
        return Tok("str", '"', line, "".join(parts), holes, tuple(hsrc))


def annotate_calls(toks):
    """각 문자열 토큰에 둘러싼 호출 · 특성 이름 기록"""
    stack = []
    for k, t in enumerate(toks):
        if t.kind == "op" and t.text in ("(", "[", "?["):
            name = ""
            if t.text == "(":
                j = k - 1
                parts = []
                while j >= 0 and toks[j].kind == "id":
                    parts.insert(0, toks[j].text)
                    if j - 1 >= 0 and toks[j - 1].kind == "op" and toks[j - 1].text in (".", "?."):
                        j -= 2; continue
                    break
                name = ".".join(parts)
            stack.append(name)
        elif t.kind == "op" and t.text in ")]":
            if stack:
                stack.pop()
        elif t.kind == "str":
            t.calls = tuple(n for n in reversed(stack) if n)


STOP = {",", ";", ")", "]", "}", "?", ":", "==", "!=", "=>", "=", "&&", "||", "??", "{", "+=", "-=", "<=", ">=", "<", ">", "+", "-"}
STOP_ID = {"return", "case", "yield", "else", "throw", "await"}


def operand_end(toks, k):
    depth = 0
    j = k
    while j < len(toks):
        t = toks[j]
        if t.kind == "op":
            if t.text in ("(", "[", "?["):
                depth += 1
            elif t.text in ")]":
                if depth == 0:
                    return j
                depth -= 1
            elif depth == 0 and t.text in STOP and j > k:
                return j
        j += 1
    return j


def operand_start(toks, k):
    depth = 0
    j = k
    while j >= 0:
        t = toks[j]
        if t.kind == "op":
            if t.text in ")]":
                depth += 1
            elif t.text in ("(", "[", "?["):
                if depth == 0:
                    return j + 1
                depth -= 1
            elif depth == 0 and t.text in STOP and j < k:
                return j + 1
        elif t.kind == "id" and depth == 0 and t.text in STOP_ID and j < k:
            return j + 1
        j -= 1
    return 0


def chains(toks):
    """+ 로 이은 사슬 → [(항목(문자열 토큰 또는 None = 식), 첫 문자열 토큰, 사슬 앞 토큰)]"""
    used = set()
    out = []
    for k, t in enumerate(toks):
        if t.kind != "str" or k in used:
            continue
        start = k
        while start - 1 >= 0 and toks[start - 1].kind == "op" and toks[start - 1].text == "+":
            s = operand_start(toks, start - 2)
            if s > start - 2:
                break
            start = s
        items = []
        j = start
        while True:
            e = operand_end(toks, j)
            if e - j == 1 and toks[j].kind == "str":
                items.append(toks[j]); used.add(j)
            else:
                items.append(None)
            if e < len(toks) and toks[e].kind == "op" and toks[e].text == "+":
                j = e + 1
                continue
            break
        if not any(x is t for x in items):  # 사슬 해석이 틀렸으면 단독으로
            items = [t]; used.add(k); start = k
        out.append((items, t, toks[start - 1] if start > 0 else None))
    return out


def lit_value(tok, base):
    """문자열 토큰 값의 \\x00k\\x00 구멍 → {base+k}. 조사 함수 구멍은 \\x01함수\\x01 표시"""
    def f(m):
        k = int(m.group(1))
        src = tok.hsrc[k].strip() if k < len(tok.hsrc) else ""
        fn = src.split("(")[0].strip()
        if fn in PARTICLE_FUNCS:
            return "\x01%s\x01" % fn
        return "{%d}" % (base + k)
    return re.sub(r"\x00(\d+)\x00", f, tok.value), base + tok.holes


# ---------------------------------------------------------------- 정리 · 항목
SPEAKER = re.compile(r"^([^\n]*?)::(.*)$", re.S)
PART_MARK = re.compile(r"\x01(\w+)\x01")


def renumber(s):
    seen = {}

    def f(m):
        k = m.group(1)
        if k not in seen:
            seen[k] = len(seen)
        return "{%d}" % seen[k]
    return HOLE.sub(f, s)


def clean_piece(s):
    s = s.strip()
    s = re.sub(r"^(·\s*)+", "", s)
    s = re.sub(r"(\s*·)+$", "", s)
    return s.strip()


def tags_paired(s):
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


def balanced(s):
    """' · ' 로 자른 조각이 괄호 · 리치 텍스트 태그를 반만 품지 않았는지"""
    return s.count("(") == s.count(")") and s.count("[") == s.count("]") and tags_paired(s)


def emit(text, src, add, speakers=True, split=False):
    m = PART_MARK.search(text)
    if m:
        for p in PARTICLE_FUNCS[m.group(1)]:
            emit(text[:m.start()] + p + text[m.end():], src, add, speakers, split)
        return
    for line in text.split("\n"):
        sm = SPEAKER.match(line)
        if sm:
            name = sm.group(1).split("#")[0].strip()
            if speakers and HANGUL.search(name) and not HOLE.search(name) and name not in KEEP:
                add(name, src)
            line = sm.group(2)
        piece = clean_piece(line)
        if not HANGUL.search(piece) or piece in KEEP or piece in PARTICLES:
            continue
        piece = renumber(piece)
        add(piece, src)
        if split and " · " in piece:
            ps = [clean_piece(p) for p in piece.split(" · ")]
            if all(balanced(p) for p in ps):
                for p in ps:
                    if HANGUL.search(p) and p not in KEEP and p not in PARTICLES:
                        add(renumber(p), src)


def extract_cs(path, rel, add):
    src = io.open(path, encoding="utf-8-sig").read()
    lx = Lexer(src)
    toks = lx.run()
    # 맨 바깥 + 보간 구멍 안(구멍 값은 런타임이 다시 번역 → 단독 항목 · 틀)
    for lst in [toks] + lx.inner:
        extract_toks(lst, rel, add)


def extract_toks(toks, rel, add):
    annotate_calls(toks)
    for items, first, before in chains(toks):
        strs = [x for x in items if x is not None]
        if not any(HANGUL.search(x.value or "") for x in strs):
            continue
        if any(c in EXCLUDE_CALLS or c.split(".")[-1] in EXCLUDE_LAST for x in strs for c in x.calls):
            continue
        if before is not None and ((before.kind == "op" and before.text in ("==", "!=")) or (before.kind == "id" and before.text == "case")):
            continue
        parts = []
        base = 0
        for x in items:
            if x is None:
                parts.append("{%d}" % base); base += 1
            else:
                v, base = lit_value(x, base)
                parts.append(v)
        text = "".join(parts)
        if FILE_PATH.match(text.strip()):
            continue
        emit(text, "%s:%d" % (rel, first.line), add, split=base > 0)


# ---------------------------------------------------------------- CSV
ENUM_TOKENS = re.compile(r"\b(CondValue|Value2|Value|Duration)\b")


def extract_csv(path, rel, add):
    with io.open(path, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    if not rows:
        return
    head = [h.split(":")[0] for h in rows[0]]
    for r_i, row in enumerate(rows[1:], start=2):
        for c_i, cell in enumerate(row):
            if not HANGUL.search(cell):
                continue
            src = "%s:%d (%s)" % (rel, r_i, head[c_i] if c_i < len(head) else "?")
            emit(cell, src, add, speakers=False)
            if os.path.basename(path) == "Enum.csv" and ENUM_TOKENS.search(cell):
                idx = {}

                def f(m):
                    if m.group(1) not in idx:
                        idx[m.group(1)] = len(idx)
                    return "{%d}" % idx[m.group(1)]
                emit(ENUM_TOKENS.sub(f, cell), src + " 자리 표시", add, speakers=False)


# ---------------------------------------------------------------- 실행
def extract():
    found = {}

    def add(text, src):
        lst = found.setdefault(text, [])
        if src not in lst:
            lst.append(src)

    files = sorted(glob.glob(os.path.join(SCRIPTS, "**", "*.cs"), recursive=True), key=lambda p: p.replace("\\", "/").lower())
    for p in files:
        rel = os.path.relpath(p, ASSETS).replace("\\", "/")
        if "/Editor/" in "/" + rel or rel in EXCLUDE_FILES:
            continue
        extract_cs(p, rel, add)
    for p in sorted(glob.glob(os.path.join(TABLES, "*.csv")), key=lambda p: p.lower()):
        extract_csv(p, os.path.relpath(p, ASSETS).replace("\\", "/"), add)
    return found



def check_entries(entries, keys):
    errs = []
    seen = set()
    for i, e in enumerate(entries):
        ko, en = e.get("ko"), e.get("en")
        if not ko or not en or not en.strip():
            errs.append("#%d 빈 칸: %r" % (i, ko)); continue
        if ko in seen:
            errs.append("#%d 중복: %r" % (i, ko))
        seen.add(ko)
        if set(HOLE.findall(ko)) != set(HOLE.findall(en)):
            errs.append("#%d 자리 표시 다름: %r → %r" % (i, ko, en))
        if sorted(m.group(0) for m in TAG.finditer(ko)) != sorted(m.group(0) for m in TAG.finditer(en)):
            errs.append("#%d 태그 다름: %r → %r" % (i, ko, en))
        stack = []
        bad = False
        for m in TAG.finditer(en):
            name = m.group(1).lower()
            if m.group(0).startswith("</"):
                if not stack or stack[-1] != name:
                    bad = True; break
                stack.pop()
            else:
                stack.append(name)
        if bad or stack:
            errs.append("#%d 태그 짝 안 맞음: %r" % (i, en))
        if "\n" in en and "\n" not in ko:
            errs.append("#%d 줄바꿈 생김: %r" % (i, en))
        if en != en.strip():
            errs.append("#%d 앞뒤 공백: %r" % (i, en))
        if HANGUL.search(en):
            errs.append("#%d 영어에 한글 남음: %r" % (i, en))
        if "..." in ko and "..." not in en:
            errs.append("#%d 말줄임 빠짐: %r → %r" % (i, ko, en))
    have = [e.get("ko") for e in entries]
    hs = set(have)
    ks = set(keys)
    missing = [k for k in keys if k not in hs]
    extra = [k for k in have if k not in ks]
    return errs, missing, extra


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    found = extract()
    os.makedirs(os.path.dirname(OUT_SOURCES), exist_ok=True)
    with io.open(OUT_SOURCES, "w", encoding="utf-8", newline="\n") as f:
        json.dump(found, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print("항목 %d → %s" % (len(found), os.path.relpath(OUT_SOURCES, ROOT)))
    if "--check" in sys.argv:
        if not os.path.exists(LANG_EN):
            print("en.json 없음")
            return 1
        data = json.load(io.open(LANG_EN, encoding="utf-8"))
        errs, missing, extra = check_entries(data["entries"], list(found))
        print("en.json 항목 %d · 오류 %d · 빠짐 %d · 남음 %d" % (len(data["entries"]), len(errs), len(missing), len(extra)))
        for e in errs[:100]:
            print("  오류", e)
        for k in missing[:100]:
            print("  빠짐", repr(k))
        for k in extra[:50]:
            print("  남음", repr(k))
        return 1 if errs or missing else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
