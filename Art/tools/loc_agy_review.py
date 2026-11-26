# Gemini (agy CLI) review of en.json in batches. Read-only prompts; writes results to review/<round>/
# usage: python agy_review.py <round> <entries.json> [batch_size] [workers]
import json, os, subprocess, sys, shutil, concurrent.futures as cf

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = r"C:\Project\Cat_Tang"
rnd = sys.argv[1]
entries = json.load(open(sys.argv[2], encoding="utf-8"))
size = int(sys.argv[3]) if len(sys.argv) > 3 else 200
workers = int(sys.argv[4]) if len(sys.argv) > 4 else 3
sources = json.load(open(os.path.join(REPO, "Art", "tools", "specs", "loc_sources.json"), encoding="utf-8"))

out_dir = os.path.join(HERE, "review", rnd)
os.makedirs(out_dir, exist_ok=True)

SCHEMA = {
    "type": "object",
    "properties": {
        "findings": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "id": {"type": "integer"},
                    "type": {"type": "string", "enum": ["mistranslation", "omission", "placeholder_or_tag", "glossary", "awkward", "too_long", "tone", "other"]},
                    "problem": {"type": "string"},
                    "suggested_en": {"type": "string"},
                },
                "required": ["id", "type", "problem", "suggested_en"],
            },
        }
    },
    "required": ["findings"],
}
schema_path = os.path.join(out_dir, "schema.json")
json.dump(SCHEMA, open(schema_path, "w", encoding="utf-8"), indent=1)

PROMPT = """You are a senior Korean→English game localization reviewer. READ-ONLY task: do NOT create, edit, move or delete any file and do NOT run shell commands; only read files and answer.

Read these files in the current directory:
- loc_glossary.md : the project's terminology and style guide (binding).
- {batch} : a JSON list of entries {{"id", "ko", "en", "src"}} from the dictionary of the game "11:59" (a dual-revolver cat hero who is the unconscious of an old clockmaker refusing to die; Western + clocks; frozen at 11:59). src = where the Korean text appears in the code/tables (file:line), for context.

How the dictionary is used: at runtime a displayed Korean string is replaced by en. {{0}},{{1}} are placeholders filled at runtime (en may reorder them but must keep the same set and meaning). Rich-text tags (<color=..>, <size=..>, <b>) must be kept and paired. Dialogue ellipses '...' must be kept. Some ko entries are fragments joined at runtime with ' · '. Entries from Enum.csv may contain the literal designer tokens Value / Value2 / CondValue / Duration which must stay as-is.

For EACH entry check: mistranslation (meaning differs), omission (content missing or added), placeholder_or_tag (placeholder/tag/ellipsis broken), glossary (violates loc_glossary.md term or voice rules), awkward (unnatural English), too_long (UI button/label much longer than needed), tone (character voice wrong: Cat = terse Western gunslinger; Mirror Cat = calm, gentle; NPCs = dreamlike, half-remembering). Report ONLY real problems worth fixing — do not report entries that are fine, and do not nitpick style preferences. For each problem give the entry id, the type, a short explanation, and a full corrected English string in suggested_en (keeping placeholders/tags).

Answer with JSON only, matching the given schema: {{"findings": [...]}}. If everything is fine return {{"findings": []}}.
"""


def run(k, batch):
    work = os.path.join(out_dir, "ws_%02d" % k)
    os.makedirs(work, exist_ok=True)
    shutil.copy(os.path.join(REPO, "docs", "loc_glossary.md"), os.path.join(work, "loc_glossary.md"))
    name = "batch_%02d.json" % k
    json.dump(batch, open(os.path.join(work, name), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    res_path = os.path.join(out_dir, "result_%02d.json" % k)
    if os.path.exists(res_path):
        return k, "cached"
    for attempt in range(3):
        p = subprocess.run(["agy", "-p", PROMPT.format(batch=name), "--model", "gemini-4-neon-high",
                            "--output-format", "json", "--json-schema", schema_path],
                           cwd=work, capture_output=True, text=True, encoding="utf-8", timeout=1800)
        try:
            outer = json.loads(p.stdout.strip().splitlines()[-1] if p.stdout.strip() else "{}")
            resp = outer.get("response", "")
            resp = resp.strip()
            if resp.startswith("```"):
                resp = resp.strip("`").split("\n", 1)[1]
            inner = json.loads(resp)
            json.dump({"batch": name, "outer": {k2: v for k2, v in outer.items() if k2 != "response"}, "findings": inner["findings"]},
                      open(res_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
            return k, "ok %d findings" % len(inner["findings"])
        except Exception as e:
            open(os.path.join(out_dir, "error_%02d_%d.txt" % (k, attempt)), "w", encoding="utf-8").write(p.stdout + "\n---\n" + p.stderr + "\n---\n" + repr(e))
    return k, "failed"


batches = []
for i in range(0, len(entries), size):
    chunk = []
    for j, e in enumerate(entries[i:i + size]):
        chunk.append({"id": i + j, "ko": e["ko"], "en": e["en"], "src": sources.get(e["ko"], [])[:2]})
    batches.append(chunk)
print("batches", len(batches), flush=True)
with cf.ThreadPoolExecutor(workers) as ex:
    for k, status in ex.map(lambda a: run(*a), list(enumerate(batches))):
        print(k, status, flush=True)
