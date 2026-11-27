# 기획서 문구 검수 (안티그라비티 agy CLI) — 2026-11-27
# 1) extract: pptx 문단 글을 뽑아 <work>/<deck>/entries.json
# 2) review : agy에 말투 규칙 + entries를 넘겨 findings(old → new) 받기 → <work>/<deck>/result.json
# 3) apply  : findings를 pptx run 단위로 반영 (+ 빌더 JS가 있으면 JS 원문도 같은 문자열로 고침)
# 사용: python deck_review_agy.py <work_dir> extract|review|apply [deck_filter] [workers]
import json, os, re, subprocess, sys, concurrent.futures as cf
from pptx import Presentation

DOCS = r"C:\Project\Cat_Tang\docs"
TOOLS = os.path.join(DOCS, "tools")
work = sys.argv[1]
mode = sys.argv[2]
flt = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] != "-" else ""
workers = int(sys.argv[4]) if len(sys.argv) > 4 else 4

DECKS = {
    "총탄": ("1159_총탄_기획서_20261127_정승운_Ver05.pptx", None),
    "총": ("1159_총_기획서_20261127_정승운_Ver04.pptx", None),
    "컨셉": ("1159_컨셉_기획서_20261127_정승운_Ver04.pptx", None),
    "공통": ("1159_공통_레벨디자인_기획서_20261127_정승운_Ver04.pptx", None),
    "1장": ("1159_1장_기획서_20261127_정승운_Ver03.pptx", None),
    "2장": ("1159_2장_기획서_20261127_정승운_Ver05.pptx", None),
    "3장": ("1159_3장_기획서_20261127_정승운_Ver02.pptx", None),
    "4장": ("1159_4장_기획서_20261127_정승운_Ver03.pptx", None),
    "5장": ("1159_5장_기획서_20261127_정승운_Ver02.pptx", None),
    "코스튬": ("1159_코스튬_기획서_20261127_정승운_Ver05.pptx", None),
    "마을": ("1159_마을_기획서_20261127_정승운_Ver02.pptx", None),
    "스토리": ("1159_스토리_기획서_20261127_정승운_Ver03.pptx", None),
    "성장": ("1159_성장시스템_기획서_20261127_정승운_Ver01.pptx", "build_growth_ppt.js"),
    "튜토리얼": ("1159_튜토리얼_기획서_20261127_정승운_Ver01.pptx", "build_tutorial_ppt.js"),
    "UI": ("1159_UI환경설정_기획서_20261127_정승운_Ver01.pptx", "build_ui_ppt.js"),
    "보스연출": ("1159_보스등장연출_기획서_20261127_정승운_Ver01.pptx", "build_bossintro_ppt.js"),
}

SCHEMA = {
    "type": "object",
    "properties": {"findings": {"type": "array", "items": {"type": "object", "properties": {
        "id": {"type": "integer"},
        "type": {"type": "string", "enum": ["ai_tone", "hanja_term", "ending", "dot", "awkward", "typo", "other"]},
        "old": {"type": "string"},
        "new": {"type": "string"},
        "why": {"type": "string"},
    }, "required": ["id", "type", "old", "new", "why"]}}},
    "required": ["findings"],
}

PROMPT = """당신은 게임 기획서 문구 교정 담당이다. 읽기 전용 작업: 파일을 만들거나 고치거나 지우지 말고, 셸 명령도 실행하지 말 것. 파일을 읽고 JSON으로만 답한다.

현재 폴더의 entries.json = 게임 '11:59' 기획서 PPT '{deck}'의 문단 목록 {{"id", "slide", "text"}}.

교정 규칙 (작성자 본인의 기획서 말투):
1. 개조식 + 명사로 끝맺음. 서술형 어미(~해요 / ~합니다 / ~한다 / ~된다 / ~이다 / ~함 남발)를 명사형으로. 예) "판이 끝나면 사라진다" → "판 종료 시 소멸", "바로 회복된다" → "즉시 회복".
2. 풀어쓴 고유어 대신 한자어 기획 용어. 예) 열림 → 활성화, 얻는 곳 → 획득처, 쓰는 곳 → 사용처, 사라짐 → 소멸 / 초기화, 고름 → 선택, 사기 / 팔기 → 구매 / 판매, 바로 → 즉시, 넘친 만큼 이어짐 → 초과분 이월, 빨려 옴 → 흡수, 바뀜 → 변경, 처음 → 최초, 끝 → 종료.
3. 나열 구분자는 " / ". 가운데 점(·)과 쉼표 나열 금지.
4. AI 말투 금지: "~를 통해", "다양한", "핵심적인", "효과적으로", "~하는 것이 중요", "단순히 ~가 아니라", "자연스럽게", "몰입감 있는", "~을 선사", 과장 수식, 줄표(—) 남발, 영어 번역투.
5. 오탈자 / 어색한 조사.

절대 바꾸지 말 것:
- 큰따옴표 "..." 또는 작은따옴표 '...' 안의 글 (게임 속 대사 / UI 문구 원문 / 이름), 영어 표기, 고유명사, 숫자 / 수식 / 단위, 코드 키 이름(영문 + 밑줄).
- 이미 규칙에 맞는 문단. 슬라이드 제목처럼 짧은 명사구는 그대로 둠.
- 뜻. 정보를 빼거나 더하지 말 것.
- 길이: 새 문구는 원래 길이의 110% 이하 (레이아웃 고정).

출력: 고칠 문단마다 {{"id", "type", "old", "new", "why"}}. old = 그 문단 text 안에 실제로 있는 연속된 부분 문자열(고칠 부분만, 짧을수록 좋음, 따옴표 안 글을 포함하지 말 것), new = 바꿀 문자열. 한 문단에 여러 곳이면 항목을 나눔. 고칠 게 없으면 {{"findings": []}}. JSON만 답할 것.
"""


def deck_entries(path):
    prs = Presentation(path)
    out = []
    def walk(shapes, si):
        for sh in shapes:
            if sh.shape_type == 6:
                walk(sh.shapes, si); continue
            if not sh.has_text_frame: continue
            for p in sh.text_frame.paragraphs:
                t = "".join(r.text for r in p.runs).strip()
                if len(t) < 6 or re.fullmatch(r"[\d\s.,/%:+×→\-()초배m]+", t): continue
                out.append({"id": len(out), "slide": si, "text": t})
    for i, s in enumerate(prs.slides, 1):
        walk(s.shapes, i)
    return out


def sel():
    return {k: v for k, v in DECKS.items() if not flt or k in flt.split(",")}


if mode == "extract":
    for k, (f, _) in sel().items():
        d = os.path.join(work, k); os.makedirs(d, exist_ok=True)
        e = deck_entries(os.path.join(DOCS, f))
        json.dump(e, open(os.path.join(d, "entries.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        print(k, len(e))

elif mode == "review":
    def run(k):
        d = os.path.join(work, k)
        res = os.path.join(d, "result.json")
        if os.path.exists(res): return k, "cached"
        sp = os.path.join(d, "schema.json"); json.dump(SCHEMA, open(sp, "w", encoding="utf-8"))
        for attempt in range(3):
            try:
                p = subprocess.run(["agy", "-p", PROMPT.format(deck=k), "--model", "gemini-4-neon-high", "--output-format", "json", "--json-schema", sp],
                                   cwd=d, capture_output=True, text=True, encoding="utf-8", timeout=1800)
                outer = json.loads(p.stdout.strip().splitlines()[-1])
                resp = outer.get("response", "").strip()
                if resp.startswith("```"): resp = resp.strip("`").split("\n", 1)[1]
                inner = json.loads(resp)
                json.dump(inner, open(res, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
                return k, "ok %d" % len(inner["findings"])
            except Exception as ex:
                open(os.path.join(d, "error_%d.txt" % attempt), "w", encoding="utf-8").write((p.stdout if 'p' in dir() else "") + "\n---\n" + (p.stderr if 'p' in dir() else "") + "\n---\n" + repr(ex))
        return k, "failed"
    with cf.ThreadPoolExecutor(workers) as ex:
        for k, st in ex.map(run, list(sel().keys())):
            print(k, st, flush=True)

elif mode == "apply":
    QUOTE = re.compile(r"\"[^\"]*\"|'[^']*'|“[^”]*”|‘[^’]*’")
    for k, (f, js) in sel().items():
        d = os.path.join(work, k)
        res = os.path.join(d, "result.json")
        if not os.path.exists(res): print(k, "no result"); continue
        ents = {e["id"]: e for e in json.load(open(os.path.join(d, "entries.json"), encoding="utf-8"))}
        finds = json.load(open(res, encoding="utf-8"))["findings"]
        prs = Presentation(os.path.join(DOCS, f))
        slides = list(prs.slides)
        jsrc = open(os.path.join(TOOLS, js), encoding="utf-8").read() if js else None
        ok, skip, log = 0, 0, []
        for fd in finds:
            e = ents.get(fd["id"]); old, new = fd["old"], fd["new"]
            if not e or not old or old == new or old not in e["text"]:
                skip += 1; log.append(("미일치", fd)); continue
            # 따옴표 안 글은 건드리지 않음
            i0 = e["text"].index(old); i1 = i0 + len(old)
            if any(m.start() < i1 and i0 < m.end() for m in QUOTE.finditer(e["text"])):
                skip += 1; log.append(("따옴표", fd)); continue
            if len(new) > len(old) * 1.25 + 4:
                skip += 1; log.append(("길이", fd)); continue
            def walk(shapes):
                for sh in shapes:
                    if sh.shape_type == 6:
                        if walk(sh.shapes): return True
                        continue
                    if not sh.has_text_frame: continue
                    for p in sh.text_frame.paragraphs:
                        if "".join(r.text for r in p.runs).strip() != e["text"]: continue
                        for r in p.runs:
                            if old in r.text:
                                r.text = r.text.replace(old, new, 1); return True
                return False
            if walk(slides[e["slide"] - 1].shapes):
                ok += 1
                if jsrc is not None and jsrc.count(old) == 1: jsrc = jsrc.replace(old, new)
            else:
                skip += 1; log.append(("run 경계", fd))
        prs.save(os.path.join(DOCS, f))
        if jsrc is not None: open(os.path.join(TOOLS, js), "w", encoding="utf-8").write(jsrc)
        json.dump([{"why": w, **fd} for w, fd in log], open(os.path.join(d, "skipped.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        print(k, "applied", ok, "skipped", skip)
