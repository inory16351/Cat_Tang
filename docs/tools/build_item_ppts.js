// 총탄 · 총 · 코스튬 기획서 PPT (docs/data/items_for_ppt.json 기반)
// 실행: NODE_PATH=<pptxgenjs node_modules> PPTX_SKILL=<pptx 스킬 폴더> node build_item_ppts.js
const pptxgen = require("pptxgenjs");
const fs = require("fs");
const path = require("path");
const { applyTheme } = require(process.env.PPTX_SKILL + "/scripts/apply_theme.js");

const DOCS = path.resolve(__dirname, "..");
const OUT_DIR = process.env.OUT_DIR || DOCS;
const DATA = JSON.parse(fs.readFileSync(path.join(DOCS, "data/items_for_ppt.json"), "utf8"));
const ICON = (p) => path.join(DOCS, "images/icons", p);
const hasIcon = (p) => fs.existsSync(ICON(p));
const pngSize = (f) => { const b = fs.readFileSync(f); return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) }; };

const THEME = {
  name: "Cat_Tang Clock", headFontFace: "Noto Sans KR", bodyFontFace: "Noto Sans KR",
  colors: { dk1: "17131F", lt1: "F3EDE2", dk2: "241D30", lt2: "A99FB5", accent1: "E0A63A", accent2: "C2343B",
            accent3: "3B2D5C", accent4: "2C2440", accent5: "5FA8B8", accent6: "8C7AA8", hlink: "E0A63A", folHlink: "8C7AA8" },
};
const GRADE_HEX = Object.fromEntries(DATA.grades.map((g) => [g.key, g.color]));
const GRADE_KO = Object.fromEntries(DATA.grades.map((g) => [g.key, g.name]));

function deck(title, sub) {
  const pres = new pptxgen();
  pres.layout = "LAYOUT_WIDE";
  pres.title = title; pres.author = DATA.author; pres.company = DATA.author;
  pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
  const C = pres.SchemeColor;
  const K = { BG: C.text1, CARD: C.accent4, TXT: C.background1, MUTED: C.background2, GOLD: C.accent1, RED: C.accent2, PURPLE: C.accent3, TEAL: C.accent5 };
  pres.defineSlideMaster({
    title: "CONTENT", background: { color: K.BG },
    objects: [
      { text: { text: `11:59 · ${title} · ${DATA.author}`, options: { x: 0.6, y: 7.0, w: 7, h: 0.3, fontSize: 10, color: K.MUTED, margin: 0 } } },
      { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.45, w: 12.1, h: 0.8, fontSize: 30, bold: true, color: K.TXT, valign: "middle", align: "left", margin: 0 }, text: "제목" } },
    ],
    slideNumber: { x: 12.2, y: 7.0, w: 0.5, h: 0.3, fontSize: 10, color: K.MUTED, align: "right" },
  });
  pres.defineSlideMaster({ title: "COVER", background: { color: K.BG }, objects: [] });
  let n = 0;
  const nm = (s) => `${s}_${++n}`;
  const text = (s, t, o) => s.addText(t, { isTextBox: true, fontFace: THEME.bodyFontFace, lang: "ko-KR", color: K.TXT, fontSize: 15, margin: 0, valign: "top", objectName: nm("text"), ...o });
  const card = (s, x, y, w, h, fill = K.CARD, line) => s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.12, fill: { color: fill }, line: line ? { color: line, width: 1.5 } : { color: fill }, objectName: nm("card") });
  const bullets = (s, items, o) => text(s, items.map((t, i) => {
    const head = typeof t === "object";
    return { text: head ? t.t : t, options: { bullet: head ? false : { indent: 16 }, bold: head, color: head ? K.GOLD : K.TXT, breakLine: i < items.length - 1, paraSpaceBefore: head && i ? 10 : 0 } };
  }), { fontSize: 15, paraSpaceAfter: 8, ...o });
  const slide = (t, section) => { const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: section }); s.addText(t, { placeholder: "title" }); return s; };
  const cover = (heroImg) => {
    pres.addSection({ title: "표지" });
    const s = pres.addSlide({ masterName: "COVER", sectionTitle: "표지" });
    if (heroImg && hasIcon(heroImg)) {
      // 원본 비율 유지: 슬라이드를 꽉 채우고 넘치는 쪽만 잘라 냄
      const { w: iw, h: ih } = pngSize(ICON(heroImg));
      const k = Math.max(13.333 / iw, 7.5 / ih);
      s.addImage({ path: ICON(heroImg), x: 0, y: 0, w: iw * k, h: ih * k, sizing: { type: "cover", w: 13.333, h: 7.5 }, objectName: "cover_img" });
    }
    s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: K.BG, transparency: heroImg ? 35 : 0 }, line: { color: K.BG, transparency: 100 }, objectName: "cover_dim" });
    text(s, "11:59", { x: 0.8, y: 2.3, w: 11, h: 0.9, fontSize: 44, bold: true, color: K.GOLD });
    text(s, title, { x: 0.8, y: 3.25, w: 11, h: 0.8, fontSize: 36, bold: true });
    text(s, sub, { x: 0.8, y: 4.1, w: 11, h: 0.5, fontSize: 18, color: K.MUTED });
    text(s, `작성 ${DATA.author} · 2026.11 · 수치는 1차안`, { x: 0.8, y: 6.5, w: 11, h: 0.35, fontSize: 13, color: K.MUTED });
  };
  return { pres, K, text, card, bullets, slide, cover, nm };
}

async function save(pres, file) {
  const out = path.join(OUT_DIR, file);
  await pres.writeFile({ fileName: out });
  await applyTheme(out, THEME);
  console.log("written", out);
}

// ---------------------------------------------------------------- 총탄
async function bulletDeck() {
  const d = deck("총탄 기획서", "탄 · 특성 · 고유 효과 · 연산");
  const { pres, K, text, card, bullets, slide, cover } = d;
  cover("sheet_bullets_common.png");
  pres.addSection({ title: "규칙" });
  {
    const s = slide("총탄 = 아이템, 12칸 = 인벤토리", "규칙");
    const parts = [
      ["자리 조건", "1개. 판정 시각이 맞으면 활성화"], ["특성", "2개. 같은 특성 2 · 4 · 6개에 단계 상승"],
      ["고유 효과", "일반 · 희귀 1개, 영웅 · 전설 최대 2개"], ["공격력", "활성화 여부와 상관없이 상시"], ["연산 슬롯", "0~2칸. 등급과 무관"],
      ["이름", "시간의 몽환적 · 시적 표현. 높은 등급일수록 고유한 이름"],
    ];
    parts.forEach((p, i) => {
      const x = 0.6 + (i % 2) * 6.13, y = 1.5 + Math.floor(i / 2) * 1.25;
      card(s, x, y, 5.95, 1.08);
      text(s, p[0], { x: x + 0.3, y: y + 0.2, w: 1.6, h: 0.4, fontSize: 16, bold: true, color: K.GOLD });
      text(s, p[1], { x: x + 2.0, y: y + 0.2, w: 3.8, h: 0.7, fontSize: 14 });
    });
    card(s, 0.6, 5.35, 12.13, 0.9, K.PURPLE);
    text(s, [{ text: "활성화 탄  ", options: { bold: true, color: K.GOLD } }, { text: "특성 + 고유 효과 + 공격력      " },
             { text: "비활성화 탄  ", options: { bold: true, color: K.RED } }, { text: "공격력만" }], { x: 0.9, y: 5.55, w: 11.6, h: 0.5, fontSize: 16, valign: "middle" });
  }
  {
    const s = slide("효과 적용 범위", "규칙");
    const cols = [
      ["탄 고유 효과", ["활성화돼 있으면 모든 사격에 적용 (같은 효과는 탄 수만큼 겹침)", "그 탄을 직접 쏠 때는 2배 + 공명 사격", "공명 사격: 굵은 궤적 · 금빛 섬광 · 탄 색 폭발 30% · 화면 흔들림"]],
      ["특성", ["단계는 활성화 탄의 특성으로 셈", "켜진 효과는 모든 탄에 적용", "비활성화 탄도 포함"]],
      ["총 고유 효과", ["그 총이 담당하는 칸의 탄에만", "오른손 = 홀수 칸, 왼손 = 짝수 칸", "스킬 · 완벽 장전 효과만 전역"]],
    ];
    cols.forEach((c, i) => {
      const x = 0.6 + i * 4.13;
      card(s, x, 1.5, 3.87, 4.6);
      text(s, c[0], { x: x + 0.3, y: 1.8, w: 3.3, h: 0.5, fontSize: 20, bold: true, color: K.GOLD });
      bullets(s, c[1], { x: x + 0.3, y: 2.55, w: 3.35, h: 3.3, fontSize: 15, paraSpaceAfter: 10 });
    });
  }
  {
    const s = slide("등급", "규칙");
    DATA.grades.forEach((g, i) => {
      const x = 0.6 + i * 3.1;
      card(s, x, 1.5, 2.9, 3.0, K.CARD, g.color);
      text(s, g.name, { x: x + 0.3, y: 1.75, w: 2.3, h: 0.6, fontSize: 26, bold: true, color: g.color });
      text(s, `${g.count}종`, { x: x + 0.3, y: 2.45, w: 2.3, h: 0.5, fontSize: 20 });
      text(s, `드랍 가중치 ${g.weight}`, { x: x + 0.3, y: 3.05, w: 2.3, h: 0.4, fontSize: 13, color: K.MUTED });
      text(s, i < 2 ? "고유 효과 1개" : "고유 효과 최대 2개", { x: x + 0.3, y: 3.5, w: 2.3, h: 0.4, fontSize: 13 });
    });
    bullets(s, ["일반: 단순한 한 줄 효과", "희귀: 조건이 붙은 효과", "영웅: 다른 칸 · 다른 시스템과 엮이는 효과", "전설: 규칙을 바꾸는 효과"],
            { x: 0.6, y: 4.8, w: 12.1, h: 1.9, fontSize: 15, paraSpaceAfter: 4 });
  }
  pres.addSection({ title: "특성" });
  for (let page = 0; page < 2; page++) {
    const s = slide(`특성 9종 (${page + 1}/2)`, "특성");
    const head = { bold: true, color: THEME.colors.dk1, fill: { color: THEME.colors.accent1 }, fontSize: 13, valign: "middle" };
    const cell = (t, o = {}) => ({ text: t, options: { lang: "ko-KR", color: THEME.colors.lt1, fill: { color: THEME.colors.accent4 }, fontSize: 11, valign: "middle", ...o } });
    const rows = [[{ text: "특성", options: head }, { text: "2개", options: head }, { text: "4개", options: head }, { text: "6개 (새 효과)", options: head }]];
    DATA.traits.slice(page * 5, page * 5 + 5).forEach((t) => rows.push([cell(`${t.name}\n${t.desc}`, { bold: true }), cell(t.t2), cell(t.t4), cell(t.t6)]));
    s.addTable(rows, { x: 0.6, y: 1.45, w: 12.13, colW: [2.6, 3.4, 2.4, 3.73], rowH: [0.45, 0.95, 0.95, 0.95, 0.95, 0.95].slice(0, rows.length),
                       border: { type: "solid", color: THEME.colors.dk1, pt: 2 }, fontFace: THEME.bodyFontFace, margin: [0.04, 0.12, 0.04, 0.12], objectName: d.nm("trait_table") });
  }
  pres.addSection({ title: "탄 목록" });
  const sheetOf = { Common: "sheet_bullets_common.png", Rare: "sheet_bullets_rare.png", Epic: "sheet_bullets_epic.png", Legendary: "sheet_bullets_legendary.png" };
  for (const g of DATA.grades) {
    const list = DATA.bullets.filter((b) => b.grade === g.key);
    for (let p = 0; p < list.length; p += 6) {
      const s = slide(`${g.name} 탄 (${list.length}종)${list.length > 6 ? ` ${p / 6 + 1}/${Math.ceil(list.length / 6)}` : ""}`, "탄 목록");
      list.slice(p, p + 6).forEach((b, i) => {
        const x = 0.6 + (i % 3) * 4.1, y = 1.45 + Math.floor(i / 3) * 2.75;
        card(s, x, y, 3.93, 2.6, K.CARD, g.color);
        const icon = `bullets_toon/bullet_${b.id}.png`;
        if (hasIcon(icon)) s.addImage({ path: ICON(icon), x: x + 0.12, y: y + 0.12, w: 1.2, h: 1.2, objectName: d.nm("icon"), altText: b.name });
        else { s.addShape(pres.shapes.OVAL, { x: x + 0.2, y: y + 0.2, w: 0.95, h: 0.95, fill: { color: g.color }, line: { color: g.color }, objectName: d.nm("ph") }); }
        text(s, b.name, { x: x + 1.45, y: y + 0.18, w: 2.38, h: 0.45, fontSize: 16, bold: true, color: g.color });
        text(s, `${b.cond} · ${b.traits}\n공격력 ${b.dmg} · 슬롯 ${b.slots}`, { x: x + 1.45, y: y + 0.66, w: 2.38, h: 0.6, fontSize: 11, color: K.MUTED });
        text(s, b.effects.join("\n"), { x: x + 0.2, y: y + 1.42, w: 3.55, h: 1.1, fontSize: 12, lineSpacingMultiple: 1.05 });
      });
    }
  }
  pres.addSection({ title: "연산 · 데이터" });
  {
    const s = slide("연산 10종", "연산 · 데이터");
    DATA.ops.forEach((o, i) => {
      const x = 0.6 + (i % 5) * 2.45, y = 1.6 + Math.floor(i / 5) * 1.9;
      card(s, x, y, 2.25, 1.6);
      text(s, o.name, { x, y: y + 0.25, w: 2.25, h: 0.7, fontSize: 30, bold: true, align: "center", color: K.GOLD });
      text(s, `드랍 가중치 ${o.weight}`, { x, y: y + 1.05, w: 2.25, h: 0.35, fontSize: 12, align: "center", color: K.MUTED });
    });
    text(s, "×0 · ÷0 없음. 탄의 연산 슬롯(위 · 아래)에 끼우고 위 슬롯부터 계산. 교체는 전투 밖에서만.", { x: 0.6, y: 5.6, w: 12.1, h: 0.4, fontSize: 14, color: K.MUTED });
  }
  {
    const s = slide("데이터 테이블 구조", "연산 · 데이터");
    const tables = [
      ["Bullet", "Id · Name · Grade · Damage · CondType · CondValue · Trait1 · Trait2 · OpSlots · Effect1 · Effect2 · IconKey · Desc"],
      ["Trait / TraitTier", "특성 이름 · 설명 / 단계(2 · 4 · 6)별 EffectId"],
      ["Effect", "Id · Trigger · Cond · CondValue · Chance · Target · Type · Value · Value2 · Duration · Desc"],
      ["Op", "Id · Name · OpType · Operand · DropWeight"],
      ["Enum", "모든 enum의 키와 뜻"],
    ];
    tables.forEach((t, i) => {
      const y = 1.5 + i * 0.98;
      card(s, 0.6, y, 12.13, 0.82);
      text(s, t[0], { x: 0.9, y: y + 0.2, w: 2.4, h: 0.45, fontSize: 15, bold: true, color: K.GOLD });
      text(s, t[1], { x: 3.3, y: y + 0.2, w: 9.2, h: 0.5, fontSize: 13 });
    });
    text(s, "타입은 string · enum · int · float만. 관리 docs/data/Cat_Tang_Data.xlsx · 게임 Game/Assets/Data/Tables/*.csv", { x: 0.6, y: 6.5, w: 12.1, h: 0.35, fontSize: 12, color: K.MUTED });
  }
  await save(pres, "Cat_Tang_총탄_기획서.pptx");
}

// ---------------------------------------------------------------- 총
async function gunDeck() {
  const d = deck("총 기획서", "기본 수치 · 고유 효과 · 스킬");
  const { pres, K, text, card, bullets, slide, cover } = d;
  cover("sheet_guns.png");
  pres.addSection({ title: "규칙" });
  {
    const s = slide("총 규칙", "규칙");
    bullets(s, [
      { t: "장착" }, "드랍 아이템. 아무 손에나 장착 (오른손 = 홀수 칸, 왼손 = 짝수 칸)", "양손 2자루만. 주우면 교체 창: 왼손 · 오른손 · 버리기 (빠진 총은 바닥에)", "시작 = 양손 기본 총 (효과 없음)",
      { t: "총마다 다른 것" }, "기본 수치: 공격력 배율 · 발사 간격 · 장전 시간 · 사거리", "손별 효과: 오른손 효과 ≠ 왼손 효과 (오른손 = 공격, 왼손 = 보조)", "스킬 1개 (스컬식). 양손 합쳐 2개, 쿨타임 방식",
      { t: "효과 적용 범위" }, "그 손이 담당하는 칸의 탄에만. 스킬 · 완벽 장전 효과만 전역",
      { t: "없는 것" }, "연산 슬롯 (연산은 탄에)",
    ], { x: 0.6, y: 1.45, w: 12.1, h: 5.3 });
  }
  {
    const s = slide("스킬 8종 (쿨타임)", "규칙");
    DATA.skills.forEach((k, i) => {
      const x = 0.6 + (i % 2) * 6.13, y = 1.45 + Math.floor(i / 2) * 1.32;
      card(s, x, y, 5.95, 1.17);
      text(s, k.name, { x: x + 0.3, y: y + 0.18, w: 3.5, h: 0.4, fontSize: 16, bold: true, color: K.GOLD });
      text(s, `${k.cd}초`, { x: x + 4.4, y: y + 0.18, w: 1.3, h: 0.4, fontSize: 14, align: "right", color: K.MUTED });
      text(s, k.desc, { x: x + 0.3, y: y + 0.62, w: 5.4, h: 0.45, fontSize: 13 });
    });
  }
  pres.addSection({ title: "총 목록" });
  for (let p = 0; p < DATA.guns.length; p += 4) {
    const s = slide(`총 8종 (${p / 4 + 1}/2)`, "총 목록");
    DATA.guns.slice(p, p + 4).forEach((g, i) => {
      const x = 0.6 + (i % 2) * 6.13, y = 1.45 + Math.floor(i / 2) * 2.7;
      const col = GRADE_HEX[g.grade];
      card(s, x, y, 5.95, 2.55, K.CARD, col);
      const icon = `guns_toon/gun_${g.id}.png`;
      if (hasIcon(icon)) s.addImage({ path: ICON(icon), x: x + 0.15, y: y + 0.2, w: 1.3, h: 1.3, objectName: d.nm("icon"), altText: g.name });
      text(s, g.name, { x: x + 1.6, y: y + 0.18, w: 3.0, h: 0.45, fontSize: 17, bold: true, color: col });
      text(s, GRADE_KO[g.grade], { x: x + 4.6, y: y + 0.2, w: 1.15, h: 0.4, fontSize: 12, align: "right", color: col });
      text(s, `공격력 ×${g.mul} · 발사 ${g.interval}초 · 장전 ${g.reload}초 · ${g.range}m`, { x: x + 1.6, y: y + 0.65, w: 4.2, h: 0.35, fontSize: 11, color: K.MUTED });
      text(s, `스킬: ${g.skill}`, { x: x + 1.6, y: y + 1.0, w: 4.2, h: 0.35, fontSize: 12, bold: true });
      text(s, (g.effects.length ? g.effects : ["양손 모두 효과 없음 (기본 총)"]).join("\n"), { x: x + 0.25, y: y + 1.55, w: 5.5, h: 0.9, fontSize: 11 });
    });
  }
  await save(pres, "Cat_Tang_총_기획서.pptx");
}

// ---------------------------------------------------------------- 코스튬
async function costumeDeck() {
  const d = deck("코스튬 기획서", "해금 · 효과 · 외형");
  const { pres, K, text, card, bullets, slide, cover } = d;
  cover(null);
  pres.addSection({ title: "규칙" });
  {
    const s = slide("코스튬 규칙", "규칙");
    bullets(s, [
      { t: "선택" }, "거점(서부 여관 2층 방)에서 한 벌 골라 런 시작", "첫 버전 8벌",
      { t: "효과" }, "능력치 보정 · 시작 아이템 · 규칙 변경. 장점과 단점을 함께",
      { t: "해금" }, "업적 조건 달성 → 시간의 조각으로 구매",
      { t: "외형" }, "몸은 같고 옷 · 모자 메시를 교체 (실루엣이 달라짐)",
    ], { x: 0.6, y: 1.45, w: 12.1, h: 5.3 });
  }
  pres.addSection({ title: "목록" });
  for (let p = 0; p < DATA.costumes.length; p += 4) {
    const s = slide(`코스튬 8벌 (${p / 4 + 1}/2)`, "목록");
    DATA.costumes.slice(p, p + 4).forEach((c, i) => {
      const x = 0.6 + (i % 2) * 6.13, y = 1.45 + Math.floor(i / 2) * 2.7;
      card(s, x, y, 5.95, 2.55);
      text(s, c.name, { x: x + 0.3, y: y + 0.2, w: 3.6, h: 0.45, fontSize: 18, bold: true, color: K.GOLD });
      text(s, c.cost ? `${c.cost} 조각` : "기본", { x: x + 3.9, y: y + 0.22, w: 1.8, h: 0.4, fontSize: 13, align: "right", color: K.MUTED });
      text(s, `해금: ${c.unlock}`, { x: x + 0.3, y: y + 0.7, w: 5.4, h: 0.35, fontSize: 12, color: K.MUTED });
      text(s, (c.effects.length ? c.effects : ["효과 없음"]).join("\n"), { x: x + 0.3, y: y + 1.1, w: 5.4, h: 0.95, fontSize: 13 });
      text(s, c.desc, { x: x + 0.3, y: y + 2.05, w: 5.4, h: 0.35, fontSize: 11, color: K.MUTED });
    });
  }
  await save(pres, "Cat_Tang_코스튬_기획서.pptx");
}

(async () => {
  await bulletDeck();
  await gunDeck();
  await costumeDeck();
})();
