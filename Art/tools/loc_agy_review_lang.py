# -*- coding: utf-8 -*-
"""Antigravity(agy CLI · Gemini) 검수 — 영어 밖 언어 사전 (2026-11-27 프랑스어 · 러시아어 · 일본어 · 중국어 · 독일어 · 아랍어)

읽기 전용 프롬프트. 묶음마다 작업 폴더(loc_work/<code>/review/ws_NN)에 용어집 둘 + 묶음 JSON을 복사해 agy 에 넘김.
결과: Art/tools/loc_work/<code>/review/result_NN.json  {"findings":[{id, type, problem, suggested}]}  (id = en.json 순서)
      전부 모은 것: Art/tools/loc_work/<code>/review/findings.json
사용: python Art/tools/loc_agy_review_lang.py <code> [묶음 크기=200] [동시=3] [모델=gemini-4-neon-high]
"""
import concurrent.futures as cf
import io
import json
import os
import shutil
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LANG_DIR = os.path.join(ROOT, "Game", "Assets", "Resources", "Lang")
SOURCES = os.path.join(ROOT, "Art", "tools", "specs", "loc_sources.json")

NAMES = {"fr": "French", "ru": "Russian", "ja": "Japanese", "zh": "Simplified Chinese (mainland)", "de": "German", "ar": "Arabic (Modern Standard)"}

EXTRA = {
    "ru": "Numbers fill {{0}} at runtime, so constructions must work for any number (no noun declension that depends on the count).",
    "ar": "The text is stored in logical order; the engine shapes letters and reorders right-to-left itself. Bidi control characters, presentation forms, tatweel and diacritics are NOT allowed. Western digits are used.",
    "ja": "Dialogue ellipses must stay as ASCII '...' (same number of dots), not '……'. Full-width ！？ are fine.",
    "zh": "Dialogue ellipses must stay as ASCII '...' (same number of dots), not '……'. Full-width Chinese punctuation is fine.",
    "fr": "UI uses vous/imperative; the Cat says tu to others.",
    "de": "UI uses du-form.",
}

PROMPT = """You are a senior Korean→{lang} game localization reviewer. READ-ONLY task: do NOT create, edit, move or delete any file and do NOT run shell commands; only read files and answer.

Read these files in the current directory:
- loc_glossary.md : the project's base terminology and style guide (Korean→English; binding for meaning, voices and symbols).
- loc_glossary_{code}.md : the {lang} terminology and style decisions (binding for {lang} terms).
- {batch} : a JSON list of entries {{"id", "ko", "en", "{code}", "src"}} from the dictionary of the game "11:59" (a dual-revolver cat hero who is the unconscious of an old clockmaker refusing to die; Western + clocks; frozen at 11:59). ko = Korean source, en = reviewed English (reference only), {code} = the {lang} translation to review. src = where the Korean appears in the code/tables (file:line), for context.

How the dictionary is used: at runtime a displayed Korean string is replaced by the {code} text. {{0}},{{1}} are placeholders filled at runtime (may be reordered but the same set must stay). Rich-text tags (<color=..>, <size=..>, <b>) must be kept and paired. Dialogue ellipses '...' must be kept. Some ko entries are fragments joined at runtime with ' · ' or inserted into other entries' {{0}}. Entries from Enum.csv may contain the literal designer tokens Value / Value2 / CondValue / Duration which must stay as-is. {extra}

For EACH entry check the {code} text against the Korean (use English only as a hint): mistranslation (meaning differs), omission (content missing or added), placeholder_or_tag (placeholder/tag/ellipsis broken), glossary (violates either glossary's term or voice rules, or is inconsistent with how the same term is translated elsewhere in this batch), grammar (wrong grammar, agreement, spelling, script), awkward (unnatural {lang}), too_long (UI button/label much longer than needed), tone (character voice wrong: Cat = terse Western gunslinger; Mirror Cat = calm, gentle; NPCs = dreamlike, half-remembering). Report ONLY real problems worth fixing — do not report entries that are fine, and do not nitpick style preferences. For each problem give the entry id, the type, a short explanation in English, and a full corrected {lang} string in suggested (keeping placeholders/tags/line breaks).

Answer with JSON only, matching the given schema: {{"findings": [...]}}. If everything is fine return {{"findings": []}}.
"""

SCHEMA = {
    "type": "object",
    "properties": {
        "findings": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "id": {"type": "integer"},
                    "type": {"type": "string", "enum": ["mistranslation", "omission", "placeholder_or_tag", "glossary", "grammar", "awkward", "too_long", "tone", "other"]},
                    "problem": {"type": "string"},
                    "suggested": {"type": "string"},
                },
                "required": ["id", "type", "problem", "suggested"],
            },
        }
    },
    "required": ["findings"],
}


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    code = sys.argv[1]
    size = int(sys.argv[2]) if len(sys.argv) > 2 else 200
    workers = int(sys.argv[3]) if len(sys.argv) > 3 else 3
    model = sys.argv[4] if len(sys.argv) > 4 else "gemini-4-neon-high"
    entries = json.load(io.open(os.path.join(LANG_DIR, code + ".json"), encoding="utf-8"))["entries"]
    sources = json.load(io.open(SOURCES, encoding="utf-8"))
    out_dir = os.path.join(ROOT, "Art", "tools", "loc_work", code, "review")
    os.makedirs(out_dir, exist_ok=True)
    schema_path = os.path.join(out_dir, "schema.json")
    json.dump(SCHEMA, io.open(schema_path, "w", encoding="utf-8"), indent=1)
    prompt_t = PROMPT.replace("{extra}", EXTRA.get(code, ""))

    def run(k, batch):
        res_path = os.path.join(out_dir, "result_%02d.json" % k)
        if os.path.exists(res_path):
            return k, "cached"
        work = os.path.join(out_dir, "ws_%02d" % k)
        os.makedirs(work, exist_ok=True)
        shutil.copy(os.path.join(ROOT, "docs", "loc_glossary.md"), os.path.join(work, "loc_glossary.md"))
        shutil.copy(os.path.join(ROOT, "docs", "loc_glossary_%s.md" % code), os.path.join(work, "loc_glossary_%s.md" % code))
        name = "batch_%02d.json" % k
        json.dump(batch, io.open(os.path.join(work, name), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        prompt = prompt_t.format(lang=NAMES[code], code=code, batch=name)
        for attempt in range(3):
            p = subprocess.run(["agy", "-p", prompt, "--model", model, "--output-format", "json", "--json-schema", schema_path],
                               cwd=work, capture_output=True, text=True, encoding="utf-8", timeout=2400)
            try:
                outer = json.loads(p.stdout.strip().splitlines()[-1] if p.stdout.strip() else "{}")
                resp = outer.get("response", "").strip()
                if resp.startswith("```"):
                    resp = resp.strip("`").split("\n", 1)[1]
                inner = json.loads(resp)
                json.dump({"batch": name, "findings": inner["findings"]}, io.open(res_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
                return k, "ok %d findings" % len(inner["findings"])
            except Exception as e:
                io.open(os.path.join(out_dir, "error_%02d_%d.txt" % (k, attempt)), "w", encoding="utf-8").write(p.stdout + "\n---\n" + p.stderr + "\n---\n" + repr(e))
        return k, "failed"

    batches = []
    for i in range(0, len(entries), size):
        batches.append([{"id": i + j, "ko": e["ko"], "en": e["en"], code: e.get(code, ""), "src": sources.get(e["ko"], [])[:2]}
                        for j, e in enumerate(entries[i:i + size])])
    print("%s: batches %d" % (code, len(batches)), flush=True)
    with cf.ThreadPoolExecutor(workers) as ex:
        for k, status in ex.map(lambda a: run(*a), list(enumerate(batches))):
            print(code, k, status, flush=True)

    allf = []
    for k in range(len(batches)):
        p = os.path.join(out_dir, "result_%02d.json" % k)
        if os.path.exists(p):
            for f in json.load(io.open(p, encoding="utf-8"))["findings"]:
                f[code + "_now"] = entries[f["id"]].get(code, "") if 0 <= f["id"] < len(entries) else ""
                f["ko"] = entries[f["id"]]["ko"] if 0 <= f["id"] < len(entries) else ""
                allf.append(f)
    json.dump(allf, io.open(os.path.join(out_dir, "findings.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("%s: findings %d → %s" % (code, len(allf), os.path.relpath(os.path.join(out_dir, "findings.json"), ROOT)))


if __name__ == "__main__":
    main()
