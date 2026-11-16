// 11:59 공통 레벨 디자인 기획서 · 1장 기획서 (docs/Cat_Tang_레벨_디자인.md 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_level_ppts.js
//       OUT_DIR로 출력 폴더 지정 (기본 docs/). 그림체 · 색은 컨셉 기획서 Ver02(build_concept_v2.js)와 같음
const pptxgen = require("pptxgenjs");
const sizeOf = require("image-size");
const path = require("path");
const fs = require("fs");

const DOCS = path.resolve(__dirname, "..");
const IMG = path.join(DOCS, "images");
const OUT_DIR = process.env.OUT_DIR || DOCS;
const AUTHOR = "정승운";
const DATE = "2026.11.16";
const img = (n) => path.join(IMG, n);
const has = (n) => fs.existsSync(img(n));
const dims = (n) => { const d = sizeOf(fs.readFileSync(img(n))); return { w: d.width, h: d.height }; };

const FONT = "Noto Sans KR";
// 컨셉 기획서 Ver02와 같은 팔레트
const BG = "17131F", CARD = "2C2440", TXT = "F3EDE2", MUTED = "A99FB5";
const GOLD = "E0A63A", RED = "C2343B", TEAL = "5FA8B8", PURPLE = "3B2D5C", LAV = "8C7AA8", FLOOR = "221B2E";

function makeDeck(docTitle, footer, sections) {
  const pres = new pptxgen();
  pres.layout = "LAYOUT_WIDE"; // 13.333 x 7.5
  pres.title = docTitle;
  pres.author = AUTHOR;
  pres.company = AUTHOR;
  pres.theme = { headFontFace: FONT, bodyFontFace: FONT };
  pres.defineSlideMaster({
    title: "CONTENT",
    background: { color: BG },
    objects: [
      { text: { text: footer, options: { x: 0.6, y: 7.05, w: 7, h: 0.3, fontSize: 10, color: MUTED, margin: 0, fontFace: FONT } } },
      { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.62, w: 12.1, h: 0.75, fontSize: 30, bold: true, color: TXT, valign: "middle", align: "left", margin: 0, fontFace: FONT }, text: "제목" } },
    ],
    slideNumber: { x: 12.2, y: 7.05, w: 0.5, h: 0.3, fontSize: 10, color: MUTED, align: "right" },
  });
  pres.defineSlideMaster({ title: "COVER", background: { color: BG }, objects: [] });

  let sid = 0;
  const nm = (s) => `${s}_${++sid}`;
  const D = { pres, sections };
  const text = D.text = (slide, t, o) => slide.addText(t, { isTextBox: true, fontFace: FONT, lang: "ko-KR", color: TXT, fontSize: 15, margin: 0, valign: "top", objectName: nm("text"), ...o });
  D.card = (slide, x, y, w, h, fill = CARD, o = {}) => slide.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.12, fill: { color: fill, transparency: o.tr || 0 }, line: { color: o.line || fill, width: o.lineW || 1, transparency: o.line ? 0 : (o.tr || 0) }, objectName: nm("card") });
  D.bullets = (slide, items, o) => {
    const runs = items.map((it, i) => {
      const obj = typeof it === "object";
      const head = obj && it.b;
      const sub = obj && it.sub;
      return { text: obj ? it.t : it, options: { bullet: head ? false : { indent: sub ? 12 : 15 }, indentLevel: sub ? 1 : 0, breakLine: i < items.length - 1, bold: head, color: obj && it.c ? it.c : (sub ? MUTED : TXT), fontSize: sub ? (o.fontSize || 15) - 2 : undefined, paraSpaceBefore: head && i > 0 ? 8 : 0 } };
    });
    text(slide, runs, { fontSize: 15, paraSpaceAfter: 6, lineSpacingMultiple: 1.1, ...o });
  };
  // 원본 비율 유지. cover = 상자를 채우고 넘치는 쪽을 잘라 냄 / contain = 잘리지 않게 상자 안에 맞춤
  D.picture = (slide, file, x, y, w, h, label, fit = "cover", o = {}) => {
    if (!has(file)) {
      slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: CARD }, line: { color: CARD } });
      text(slide, label, { x, y: y + h / 2 - 0.2, w, h: 0.4, align: "center", color: MUTED });
      return;
    }
    const { w: iw, h: ih } = dims(file);
    if (fit === "contain") {
      const k = Math.min(w / iw, h / ih), dw = iw * k, dh = ih * k;
      slide.addImage({ path: img(file), x: x + (w - dw) / 2, y: y + (h - dh) / 2, w: dw, h: dh, objectName: nm("img"), altText: label });
      return;
    }
    const k = Math.max(w / iw, h / ih);
    slide.addImage({ path: img(file), x, y, w: iw * k, h: ih * k, sizing: { type: "cover", w, h }, objectName: nm("img"), altText: label });
  };
  D.caption = (slide, t, x, y, w, o = {}) => text(slide, t, { x, y, w, h: 0.28, fontSize: 10, color: MUTED, ...o });
  D.chip = (slide, t, x, y, w, fill = GOLD, color = BG, o = {}) => {
    slide.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h: o.h || 0.36, rectRadius: 0.18, fill: { color: fill }, line: { color: fill }, objectName: nm("chip") });
    text(slide, t, { x, y, w, h: o.h || 0.36, fontSize: o.size || 12, bold: true, color, align: "center", valign: "middle" });
  };
  D.arrow = (slide, x, y, w, color = MUTED, o = {}) => slide.addShape(pres.shapes.LINE, { x, y, w, h: o.h || 0, flipV: o.flipV, line: { color, width: o.width || 2, endArrowType: "triangle", dashType: o.dash }, objectName: nm("arrow") });
  // (x, y)에서 (x + dx, y + dy)까지. 음수 크기는 PPT가 못 여니 뒤집기로 바꿔 저장
  D.line = (slide, x, y, dx, dy, color, o = {}) => slide.addShape(pres.shapes.LINE, {
    x: Math.min(x, x + dx), y: Math.min(y, y + dy), w: Math.abs(dx), h: Math.abs(dy), flipH: dx < 0, flipV: dy < 0,
    line: { color, width: o.width || 1.5, dashType: o.dash, endArrowType: o.end }, objectName: nm("line"),
  });
  D.circle = (slide, cx, cy, r, fill, o = {}) => slide.addShape(pres.shapes.OVAL, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fill: fill ? { color: fill, transparency: o.tr || 0 } : { type: "none" }, line: { color: o.line || fill || TXT, width: o.lineW || 1, dashType: o.dash, transparency: o.lineTr || 0 }, objectName: nm("dot") });
  // 부채꼴: 0도 = 3시 방향, 시계 방향
  D.pie = (slide, cx, cy, r, a0, a1, fill, o = {}) => slide.addShape(pres.shapes.PIE, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, angleRange: [a0, a1], fill: { color: fill, transparency: o.tr || 0 }, line: { color: o.line || fill, width: o.lineW || 0.75, transparency: o.lineTr || 0 }, objectName: nm("pie") });
  D.rect = (slide, x, y, w, h, fill, o = {}) => slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: fill ? { color: fill, transparency: o.tr || 0 } : { type: "none" }, line: { color: o.line || fill || TXT, width: o.lineW || 1, dashType: o.dash, transparency: o.lineTr || 0 }, objectName: nm("rect") });
  D.kicker = (slide, n, t) => text(slide, `${String(n).padStart(2, "0")}  ${t}`, { x: 0.6, y: 0.3, w: 8, h: 0.3, fontSize: 12, bold: true, color: GOLD, charSpacing: 1 });
  D.content = (sec, title) => {
    const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: sections[sec - 1] });
    D.kicker(s, sec, sections[sec - 1]);
    s.addText(title, { placeholder: "title" });
    return s;
  };
  // 표: rows[0] = 머리글
  D.table = (slide, rows, x, y, colW, o = {}) => {
    const rh = o.rowH || 0.4;
    rows.forEach((r, i) => {
      const yy = y + i * (rh + 0.05);
      D.card(slide, x, yy, colW.reduce((a, b) => a + b, 0), rh, i === 0 ? PURPLE : (i % 2 ? CARD : FLOOR));
      let xx = x;
      r.forEach((c, k) => {
        text(slide, String(c), { x: xx + 0.15, y: yy, w: colW[k] - 0.2, h: rh, fontSize: o.size || 12, bold: i === 0 || k === 0, color: i === 0 ? GOLD : (k === 0 ? TXT : TXT), valign: "middle", align: k === 0 ? "left" : (o.align || "left") });
        xx += colW[k];
      });
    });
  };
  D.cover = (bgFile, title, sub, ver) => {
    pres.addSection({ title: "표지" });
    const s = pres.addSlide({ masterName: "COVER", sectionTitle: "표지" });
    D.picture(s, bgFile, 0, 0, 13.333, 7.5, "표지");
    s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 7.35, y: 4.15, w: 5.5, h: 2.9, rectRadius: 0.12, fill: { color: BG, transparency: 15 }, line: { color: BG, transparency: 100 }, objectName: "cover_panel" });
    text(s, "11:59", { x: 7.7, y: 4.3, w: 4.6, h: 0.95, fontSize: 50, bold: true, color: GOLD });
    text(s, title, { x: 7.7, y: 5.25, w: 4.0, h: 0.5, fontSize: 24, bold: true, fit: "shrink" });
    D.chip(s, ver, 11.65, 5.32, 1.0, RED, TXT, { size: 12 });
    text(s, sub, { x: 7.7, y: 5.82, w: 5.0, h: 0.4, fontSize: 15, color: MUTED });
    text(s, `작성 ${AUTHOR} · ${DATE}`, { x: 7.7, y: 6.3, w: 4.6, h: 0.3, fontSize: 13 });
    text(s, "이미지는 모두 컨셉 시안", { x: 7.7, y: 6.62, w: 4.6, h: 0.3, fontSize: 11, color: MUTED });
    return s;
  };
  D.toc = (sideImg) => {
    pres.addSection({ title: "목차" });
    const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "목차" });
    s.addText("목차", { placeholder: "title" });
    D.picture(s, sideImg, 7.2, 0, 6.133, 7.5, "목차 이미지");
    const half = Math.ceil(sections.length / 2);
    sections.forEach((t, i) => {
      const col = i < half ? 0 : 1, row = i % half;
      const x = 0.6 + col * 3.2, y = 1.75 + row * (half > 4 ? 1.0 : 1.25);
      D.card(s, x, y, 2.95, half > 4 ? 0.85 : 1.0);
      text(s, String(i + 1).padStart(2, "0"), { x: x + 0.22, y: y + 0.08, w: 0.8, h: half > 4 ? 0.7 : 0.85, fontSize: 26, bold: true, color: GOLD, valign: "middle" });
      text(s, t, { x: x + 1.0, y: y + 0.08, w: 1.85, h: half > 4 ? 0.7 : 0.85, fontSize: 15, bold: true, valign: "middle" });
    });
  };
  // 작은 도식용 기호
  D.cat = (slide, cx, cy, r = 0.13) => { D.circle(slide, cx, cy, r, GOLD, { line: TXT, lineW: 1 }); };
  D.boss = (slide, cx, cy, r = 0.2) => { D.circle(slide, cx, cy, r, RED, { line: TXT, lineW: 1 }); };
  D.mob = (slide, cx, cy, r = 0.1) => { D.circle(slide, cx, cy, r, LAV, { line: TXT, lineW: 0.75 }); };
  D.legend = (slide, x, y) => {
    D.cat(slide, x + 0.12, y + 0.14, 0.09); text(slide, "고양이", { x: x + 0.27, y, w: 0.8, h: 0.28, fontSize: 10, color: MUTED, valign: "middle" });
    D.circle(slide, x + 1.12, y + 0.14, 0.1, RED, { line: TXT }); text(slide, "보스", { x: x + 1.28, y, w: 0.6, h: 0.28, fontSize: 10, color: MUTED, valign: "middle" });
    D.circle(slide, x + 1.95, y + 0.14, 0.09, RED, { tr: 55, line: RED }); text(slide, "피해 범위", { x: x + 2.1, y, w: 0.9, h: 0.28, fontSize: 10, color: MUTED, valign: "middle" });
    D.circle(slide, x + 3.08, y + 0.14, 0.09, TEAL, { tr: 35, line: TEAL }); text(slide, "안전 · 효과 범위", { x: x + 3.23, y, w: 1.4, h: 0.28, fontSize: 10, color: MUTED, valign: "middle" });
  };
  // 패턴 카드: 왼쪽 도식, 오른쪽 이름 · 설명 · 수치
  D.patternCard = (slide, x, y, w, h, name, desc, nums, draw, tag) => {
    D.card(slide, x, y, w, h);
    const s = Math.min(h - 0.3, 1.55);
    D.rect(slide, x + 0.15, y + 0.15, s, s, FLOOR, { line: FLOOR });
    draw(x + 0.15, y + 0.15, s);
    const tx = x + s + 0.32, tw = w - s - 0.45;
    text(slide, name, { x: tx, y: y + 0.14, w: tw, h: 0.36, fontSize: 15, bold: true, color: GOLD });
    if (tag) D.chip(slide, tag, x + w - 1.05, y + h - 0.42, 0.9, PURPLE, TXT, { size: 9, h: 0.27 });
    text(slide, desc, { x: tx, y: y + 0.52, w: tw, h: h - 0.62 - 0.4, fontSize: 11.5, lineSpacingMultiple: 1.1 });
    text(slide, nums, { x: x + 0.15, y: y + s + 0.22, w: w - 0.3 - (tag ? 1.0 : 0), h: h - s - 0.3, fontSize: 10.5, color: MUTED, valign: "top" });
  };
  return D;
}

// =====================================================================
// 1) 공통 레벨 디자인 기획서
// =====================================================================
function buildCommon() {
  const SEC = ["레벨 디자인 의도", "챕터 구조", "스테이지와 전투방", "전투방 제작 규칙", "보상과 상인", "적 설계 규칙", "보스 설계 규칙", "수치 기준"];
  const D = makeDeck("11:59 공통 레벨 디자인 기획서 Ver01", `11:59 · 공통 레벨 디자인 기획서 Ver01 · ${AUTHOR}`, SEC);
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const ROOM = has("ch1/ch1_room.jpg") ? "ch1/ch1_room.jpg" : "02_ingame.png";

  const cv = D.cover("v2/1159v2_03_chapters.png", "공통 레벨 디자인 기획서", "챕터 · 스테이지 · 전투방 · 보스 공통 규칙", "Ver 01");
  cv.addNotes("모든 챕터에 똑같이 적용하는 규칙만 정리. 챕터별 내용은 챕터 기획서(1장 기획서부터)에서 따로 다룸");
  D.toc("v2/1159v2_03_chapters.png");

  // 01 의도
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "판마다 바뀌는 방, 눈으로 읽히는 공격");
    picture(s, ROOM, 0.6, 1.6, 7.3, 4.1, "전투방 컨셉");
    caption(s, "1장 전투방 컨셉 시안 (서부 거리)", 0.6, 5.78, 7.3);
    const goals = [
      ["판마다 새로운 전투", "직접 짠 전투방을 판마다 무작위로 뽑고,\n화약통 · 엄폐물 자리도 매번 달라짐", GOLD],
      ["읽고 피하는 전투", "모든 공격에 예고 존재 · 몸통 접촉 피해 없음\n→ 맞더라도 내 실수라고 납득할 수 있게", TEAL],
      ["갈수록 녹아내리는 세계", "챕터가 진행될수록 초현실 · 몽환 느낌을 강조\n→ 강해진 적을 눈으로 먼저 전달", RED],
    ];
    goals.forEach((g, i) => {
      const y = 1.6 + i * 1.4;
      card(s, 8.15, y, 4.55, 1.25);
      s.addShape(pres.shapes.RECTANGLE, { x: 8.15, y: y + 0.18, w: 0.07, h: 0.9, fill: { color: g[2] }, line: { color: g[2] } });
      text(s, g[0], { x: 8.4, y: y + 0.12, w: 4.2, h: 0.38, fontSize: 16, bold: true, color: g[2] });
      text(s, g[1], { x: 8.4, y: y + 0.5, w: 4.2, h: 0.7, fontSize: 12.5, lineSpacingMultiple: 1.1 });
    });
    card(s, 8.15, 5.85, 4.55, 1.0, PURPLE);
    text(s, [{ text: "목표  ", options: { bold: true, color: GOLD } }, { text: "판마다 다른 상황에 맞춰 빌드를 짜고, 그 빌드를 시험해 보게 만드는 것" }], { x: 8.4, y: 5.85, w: 4.15, h: 1.0, fontSize: 13, valign: "middle" });
  }

  // 02 챕터 구조
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "5개 챕터: 서부 마을에서 시간의 끝까지");
    picture(s, "v2/1159v2_03_chapters.png", 0.6, 1.6, 12.1, 3.0, "챕터 흐름");
    const ch = [["1장", "일그러진 서부 마을"], ["2장", "멈춘 기차역"], ["3장", "모래시계 사막"], ["4장", "거울 미궁"], ["5장", "무너지는 시간의 끝"]];
    ch.forEach((c, i) => {
      const x = 0.6 + i * 2.45;
      card(s, x, 4.8, 2.25, 1.0, i === 0 ? GOLD : i === 4 ? RED : CARD);
      text(s, c[0], { x: x + 0.2, y: 4.86, w: 1.8, h: 0.35, fontSize: 13, bold: true, color: i === 0 ? BG : GOLD });
      text(s, c[1], { x: x + 0.2, y: 5.2, w: 2.0, h: 0.5, fontSize: 14, bold: true, color: i === 0 ? BG : TXT });
      if (i < 4) arrow(s, x + 2.27, 5.3, 0.16, GOLD);
    });
    bullets(s, [
      "고양이 사수의 컨셉을 살려 서부극 배경에서 출발",
      "뒤 챕터로 갈수록 초현실 · 몽환 느낌을 강하게 → 강해진 적을 시각적으로 전달",
    ], { x: 0.6, y: 6.0, w: 12.1, h: 0.85, fontSize: 14 });
  }
  {
    const s = content(2, "1개 챕터 구성");
    const seq = [["스테이지 1", PURPLE], ["스테이지 2", PURPLE], ["중간 보스", "8C3A50"], ["스테이지 3", PURPLE], ["스테이지 4", PURPLE], ["보스", RED]];
    seq.forEach((q, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.75, 1.72, 0.95, q[1]);
      text(s, q[0], { x, y: 1.75, w: 1.72, h: 0.95, fontSize: 16, bold: true, align: "center", valign: "middle" });
      if (i < 5) arrow(s, x + 1.75, 2.22, 0.27, GOLD);
    });
    // 구간 표시
    s.addShape(pres.shapes.LINE, { x: 0.6, y: 2.9, w: 3.78, h: 0, line: { color: LAV, width: 1.5 } });
    text(s, "기본 하수인", { x: 0.6, y: 2.98, w: 3.78, h: 0.3, fontSize: 12, color: MUTED, align: "center" });
    s.addShape(pres.shapes.LINE, { x: 6.78, y: 2.9, w: 3.78, h: 0, line: { color: GOLD, width: 1.5 } });
    text(s, "강화형 하수인", { x: 6.78, y: 2.98, w: 3.78, h: 0.3, fontSize: 12, color: GOLD, align: "center" });
    const parts = [
      ["스테이지", "전투방 3~4개로 구성\n모든 적을 처치하면 클리어 후 보상", PURPLE],
      ["중간 보스", "챕터마다 하나\n스테이지 2와 3 사이의 별도 방에서 등장", "8C3A50"],
      ["보스", "챕터 출구를 지키는 시간의 존재\n쓰러뜨리면 다음 챕터로 이동", RED],
      ["일반 적", "시간의 하수인\n챕터 테마에 맞춰 모습과 형태가 바뀜", LAV],
    ];
    parts.forEach((p, i) => {
      const x = 0.6 + (i % 2) * 6.1, y = 3.6 + Math.floor(i / 2) * 1.62;
      card(s, x, y, 5.95, 1.45);
      s.addShape(pres.shapes.RECTANGLE, { x, y: y + 0.2, w: 0.07, h: 1.05, fill: { color: p[2] }, line: { color: p[2] } });
      text(s, p[0], { x: x + 0.3, y: y + 0.15, w: 2, h: 0.4, fontSize: 17, bold: true, color: GOLD });
      text(s, p[1], { x: x + 0.3, y: y + 0.58, w: 5.5, h: 0.8, fontSize: 13.5, lineSpacingMultiple: 1.15 });
    });
  }

  // 03 스테이지와 전투방
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "스테이지: 일직선으로 이어진 전투방");
    text(s, "1개 스테이지", { x: 0.6, y: 1.6, w: 4, h: 0.35, fontSize: 15, bold: true, color: GOLD });
    ["전투방", "전투방", "전투방", "(전투방)"].forEach((t, i) => {
      const x = 0.6 + i * 1.75;
      card(s, x, 2.05, 1.45, 0.85, CARD, { line: i === 3 ? LAV : undefined });
      text(s, t, { x, y: 2.05, w: 1.45, h: 0.85, fontSize: 14, bold: true, align: "center", valign: "middle", color: i === 3 ? MUTED : TXT });
      arrow(s, x + 1.48, 2.47, 0.24, GOLD);
    });
    card(s, 7.6, 2.05, 1.75, 0.85, GOLD);
    text(s, "클리어 · 보상", { x: 7.6, y: 2.05, w: 1.75, h: 0.85, fontSize: 14, bold: true, color: BG, align: "center", valign: "middle" });
    caption(s, "갈림길 없이 한 줄로 연결 · 보상은 스테이지 끝에서만 지급", 0.6, 3.0, 9);

    text(s, "1개 전투방", { x: 0.6, y: 3.55, w: 4, h: 0.35, fontSize: 15, bold: true, color: GOLD });
    const waves = [["웨이브 1", PURPLE, TXT], ["예고 1초", FLOOR, MUTED], ["웨이브 2", PURPLE, TXT], ["예고 1초", FLOOR, MUTED], ["(웨이브 3)", CARD, MUTED], ["방 클리어", TEAL, BG]];
    waves.forEach((w, i) => {
      const x = 0.6 + i * 1.47;
      card(s, x, 4.0, 1.25, 0.7, w[1], { line: i === 4 ? LAV : undefined });
      text(s, w[0], { x, y: 4.0, w: 1.25, h: 0.7, fontSize: 12.5, bold: true, align: "center", valign: "middle", color: w[2] });
      if (i < 5) arrow(s, x + 1.27, 4.35, 0.18, GOLD);
    });
    bullets(s, [
      "방마다 웨이브 2~3번 · 한 무리를 다 잡으면 다음 무리가 예고 후 등장",
      "웨이브당 적 수는 스테이지가 뒤로 갈수록 증가",
      "방 크기는 1.5~2 화면, 카메라가 고양이를 따라 이동",
    ], { x: 0.6, y: 5.0, w: 8.8, h: 1.8, fontSize: 14 });
    picture(s, ROOM, 9.7, 1.6, 3.0, 5.25, "전투방 컨셉");
    caption(s, "전투방 컨셉 시안", 9.7, 6.88, 3.0);
  }

  // 04 전투방 제작 규칙
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "직접 짠 방 + 무작위 배치");
    // 탑뷰 방 도식
    const rx = 0.6, ry = 1.65, rw = 6.6, rh = 4.0;
    D.rect(s, rx, ry, rw, rh, FLOOR, { line: LAV, lineW: 1.5 });
    D.rect(s, rx + 0.02, ry + 0.02, rw - 0.04, 0.32, "3A2F4F", { line: "3A2F4F" });
    D.rect(s, rx + 0.02, ry + rh - 0.34, rw - 0.04, 0.32, "3A2F4F", { line: "3A2F4F" });
    text(s, "건물 벽 · 절벽 (보이지 않는 벽)", { x: rx + 0.2, y: ry + 0.02, w: 4, h: 0.32, fontSize: 9.5, color: MUTED, valign: "middle" });
    // 입구 / 출구
    D.rect(s, rx - 0.08, ry + 1.6, 0.16, 0.8, GOLD, { line: GOLD });
    D.rect(s, rx + rw - 0.08, ry + 1.6, 0.16, 0.8, TEAL, { line: TEAL });
    text(s, "입구", { x: rx + 0.15, y: ry + 1.85, w: 0.6, h: 0.3, fontSize: 10, color: GOLD });
    text(s, "다음 방", { x: rx + rw - 0.85, y: ry + 1.85, w: 0.7, h: 0.3, fontSize: 10, color: TEAL, align: "right" });
    // 후보 자리 (점선) 와 채워진 자리
    const spots = [[1.4, 0.9, 1], [2.3, 2.9, 0], [3.1, 1.3, 1], [3.9, 2.6, 1], [4.8, 0.95, 0], [5.4, 2.95, 1], [2.0, 1.9, 0], [4.5, 1.9, 0]];
    spots.forEach(([dx, dy, on]) => {
      if (on) D.rect(s, rx + dx - 0.22, ry + dy - 0.16, 0.44, 0.32, "6B4A2E", { line: "8A6440" });
      else D.rect(s, rx + dx - 0.22, ry + dy - 0.16, 0.44, 0.32, null, { line: LAV, dash: "dash" });
    });
    [[1.9, 3.0], [3.6, 0.85], [5.0, 2.0]].forEach(([dx, dy]) => D.circle(s, rx + dx, ry + dy, 0.13, RED, { line: "FF7A6E" }));
    D.circle(s, rx + 2.75, ry + 2.15, 0.13, null, { line: RED, dash: "dash" });
    D.cat(s, rx + 0.9, ry + 2.0);
    [[4.2, 1.5], [5.6, 1.7], [4.9, 3.2], [3.3, 3.25]].forEach(([dx, dy]) => D.mob(s, rx + dx, ry + dy));
    // 범례
    const ly = ry + rh + 0.15;
    D.rect(s, 0.6, ly + 0.05, 0.3, 0.22, "6B4A2E", { line: "8A6440" }); text(s, "엄폐물", { x: 0.98, y: ly, w: 0.8, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    D.rect(s, 1.8, ly + 0.05, 0.3, 0.22, null, { line: LAV, dash: "dash" }); text(s, "비어 있는 후보 자리", { x: 2.18, y: ly, w: 1.6, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    D.circle(s, 3.95, ly + 0.16, 0.11, RED, { line: "FF7A6E" }); text(s, "화약통", { x: 4.13, y: ly, w: 0.8, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    D.cat(s, 5.05, ly + 0.16, 0.1); text(s, "고양이", { x: 5.22, y: ly, w: 0.7, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    D.mob(s, 6.05, ly + 0.16); text(s, "적", { x: 6.22, y: ly, w: 0.5, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    caption(s, "전투방 배치 예시 (탑뷰)", 0.6, 6.3, 6.6);

    bullets(s, [
      { t: "방 만들기", b: true, c: GOLD },
      "챕터마다 전투방을 손으로 직접 짜 두고, 판마다 무작위로 뽑아 씀 (하데스 · 스컬 방식)",
      "스테이지 장소에 어울리는 방 중에서만 뽑음",
      { t: "지형", b: true, c: GOLD },
      "모든 방은 평지, 높낮이 없이 장애물로만 구성",
      "절벽 · 낭떠러지는 장식 → 보이지 않는 벽으로 막아 떨어지지 않음",
      { t: "오브젝트", b: true, c: GOLD },
      "화약통 · 엄폐물은 방마다 후보 자리를 정해 두고 판마다 일부만 채움",
      "엄폐물은 총에 맞으면 부서짐 · 화약통은 터지면 적과 고양이 모두에게 피해",
    ], { x: 7.5, y: 1.6, w: 5.2, h: 5.2, fontSize: 13.5 });
  }

  // 05 보상과 상인
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "스테이지 보상과 상인");
    const rew = [
      ["선택형 보상", "3개 중 1개 선택", [["v2/icon_bullet.png", "총탄"], ["v2/icon_gun.png", "총"], [null, "연산"]], GOLD],
      ["확정형 보상", "클리어 시 항상 지급", [[null, "방어구 강화"], [null, "골드"], [null, "시간의 조각"]], TEAL],
    ];
    rew.forEach((r, i) => {
      const y = 1.65 + i * 1.95;
      card(s, 0.6, y, 6.3, 1.78);
      text(s, r[0], { x: 0.85, y: y + 0.15, w: 2.5, h: 0.4, fontSize: 17, bold: true, color: r[3] });
      text(s, r[1], { x: 3.9, y: y + 0.18, w: 2.8, h: 0.35, fontSize: 12, color: MUTED, align: "right" });
      r[2].forEach((it, k) => {
        const x = 0.85 + k * 1.98;
        if (it[0] && has(it[0])) {
          picture(s, it[0], x, y + 0.7, 0.75, 0.75, it[1], "contain");
          text(s, it[1], { x: x + 0.82, y: y + 0.7, w: 1.1, h: 0.75, fontSize: 14, bold: true, valign: "middle" });
        } else if (i === 0) {
          chip(s, "+ − × ÷", x, y + 0.85, 0.75, r[3], BG, { size: 10, h: 0.45 });
          text(s, it[1], { x: x + 0.82, y: y + 0.7, w: 1.1, h: 0.75, fontSize: 14, bold: true, valign: "middle" });
        } else chip(s, it[1], x, y + 0.85, 1.75, r[3], BG, { size: 13, h: 0.45 });
      });
    });
    bullets(s, [
      "보상은 스테이지의 마지막 전투방을 비웠을 때 지급",
    ], { x: 0.6, y: 5.65, w: 6.3, h: 1.2, fontSize: 14 });
    // 상인
    card(s, 7.2, 1.65, 5.5, 5.2, CARD, { line: RED, lineW: 1.5 });
    text(s, "상인", { x: 7.45, y: 1.8, w: 3, h: 0.4, fontSize: 18, bold: true, color: RED });
    picture(s, has("ch1/ch1_merchant.jpg") ? "ch1/ch1_merchant.jpg" : ROOM, 7.45, 2.3, 5.0, 2.2, "상인 등장 컨셉");
    caption(s, "1장 상인: 바닥에서 솟아오른 술집 카운터", 7.45, 4.52, 5.0);
    bullets(s, [
      "골드로 총탄 / 총 / 연산 판매",
      "전투방을 비울 때마다 15% 확률로 등장",
      "챕터당 최소 1번은 등장 보장",
      "챕터마다 다른 모습 (1장: 술집 바텐더)",
    ], { x: 7.45, y: 4.95, w: 5.0, h: 1.8, fontSize: 14 });
  }

  // 06 적 설계 규칙
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "몸통 접촉 피해 없이, 공격 모션에만 피해");
    // 타임라인
    text(s, "일반 적 공격 한 번의 흐름", { x: 0.6, y: 1.6, w: 6, h: 0.35, fontSize: 15, bold: true, color: GOLD });
    const tl = [["이동 · 접근", 3.0, CARD, "닿아도 피해 없음"], ["예고 모션", 3.0, PURPLE, "피해 없음 · 피할 시간"], ["공격", 2.3, RED, "이 순간에만 피해"]];
    let x = 0.6;
    tl.forEach((t) => {
      card(s, x, 2.05, t[1] - 0.08, 0.75, t[2]);
      text(s, t[0], { x, y: 2.05, w: t[1] - 0.08, h: 0.75, fontSize: 14, bold: true, align: "center", valign: "middle" });
      text(s, t[3], { x, y: 2.88, w: t[1] - 0.08, h: 0.3, fontSize: 11, align: "center", color: t[2] === RED ? "FF8A80" : MUTED });
      x += t[1];
    });
    bullets(s, [
      { t: "공통 규칙", b: true, c: GOLD },
      "근접 몬스터도 공격 모션이 있고, 모션이 터지는 순간에만 피해",
      "몸통 접촉 피해 없음 → 적 사이를 지나다녀도 안전",
      "모든 공격 앞에 예고 모션 존재",
      { t: "구성", b: true, c: GOLD },
      "챕터마다 하수인 3종 → 처음부터 섞여 나오고, 뒤로 갈수록 수만 늘어남",
      "스테이지 3부터(중간 보스 이후)는 전부 강화형으로 교체",
    ], { x: 0.6, y: 3.45, w: 8.0, h: 3.4, fontSize: 14 });
    card(s, 8.9, 1.6, 3.8, 5.25, PURPLE);
    text(s, "강화형 하수인", { x: 9.15, y: 1.75, w: 3.3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    picture(s, has("ch1/ch1_enemies.jpg") ? "ch1/ch1_enemies.jpg" : "05_zone1_western.png", 9.15, 2.25, 3.3, 1.86, "하수인 컨셉");
    bullets(s, [
      "더 녹아내린 모습",
      "기존 공격에 공격 하나 추가",
      "내구 1.5배",
    ], { x: 9.15, y: 4.35, w: 3.3, h: 1.5, fontSize: 14 });
    text(s, "겉모습만 봐도 강해졌다는 걸 바로 알 수 있게", { x: 9.15, y: 5.75, w: 3.3, h: 0.6, fontSize: 12, color: MUTED });
  }

  // 07 보스 설계 규칙
  pres.addSection({ title: SEC[6] });
  {
    const s = content(7, "보스 패턴은 세 박자: 예고 → 공격 → 빈틈");
    const beats = [["예고", "자세 · 소리 · 경고 표시", "기본 0.6초", PURPLE], ["공격", "예고한 그대로 발동", "패턴마다 다름", RED], ["빈틈", "확정된 반격 기회", "기본 0.9초", TEAL]];
    beats.forEach((b, i) => {
      const x = 0.6 + i * 4.15;
      card(s, x, 1.65, 3.8, 1.55, b[3]);
      text(s, b[0], { x: x + 0.3, y: 1.78, w: 2, h: 0.5, fontSize: 24, bold: true });
      text(s, b[2], { x: x + 1.9, y: 1.86, w: 1.7, h: 0.35, fontSize: 12, bold: true, color: i === 2 ? BG : GOLD, align: "right" });
      text(s, b[1], { x: x + 0.3, y: 2.45, w: 3.3, h: 0.5, fontSize: 14 });
      if (i < 2) arrow(s, x + 3.82, 2.42, 0.3, GOLD);
    });
    caption(s, "패턴별로 따로 정한 시간이 있으면 그 값을 우선", 0.6, 3.28, 8);
    bullets(s, [
      { t: "공정함", b: true, c: GOLD },
      "예고가 시작되면 조준 고정 (예고 뒤에 방향을 틀지 않음)",
      "몸통 접촉 피해 없음 · 패턴 사이 기습 없음",
      "빈틈 동안엔 보스가 반격하지 않음",
      "기본 조작(이동 · 대쉬)만으로 노히트 가능해야 함",
    ], { x: 0.6, y: 3.75, w: 6.0, h: 3.1, fontSize: 14 });
    bullets(s, [
      { t: "패턴 고르기", b: true, c: GOLD },
      "고양이와의 거리 · 조건으로 후보를 추린 뒤 가중치 추첨",
      "직전에 쓴 패턴은 후보에서 제외 → 같은 패턴 연속 금지",
    ], { x: 6.9, y: 3.75, w: 5.8, h: 1.6, fontSize: 14 });
    // 거리 후보 도식
    const cx = 8.35, cy = 6.0;
    D.circle(s, cx, cy, 0.8, PURPLE, { tr: 30, line: LAV, dash: "dash" });
    D.boss(s, cx, cy, 0.17);
    D.cat(s, cx + 0.45, cy - 0.3, 0.09);
    D.cat(s, cx + 1.15, cy + 0.4, 0.09);
    text(s, "가까움", { x: cx - 0.45, y: cy + 0.32, w: 0.9, h: 0.3, fontSize: 10, color: MUTED, align: "center" });
    text(s, [
      { text: "가까우면  ", options: { color: GOLD, bold: true } }, { text: "근접 패턴 후보", options: { breakLine: true } },
      { text: "멀면  ", options: { color: GOLD, bold: true } }, { text: "원거리 패턴 후보", options: { breakLine: true } },
      { text: "거리 무관  ", options: { color: GOLD, bold: true } }, { text: "언제나 후보" },
    ], { x: 9.75, y: 5.3, w: 3.0, h: 1.45, fontSize: 12.5, lineSpacingMultiple: 1.3, valign: "middle" });
  }

  // 08 수치 기준
  pres.addSection({ title: SEC[7] });
  {
    const s = content(8, "고양이 기준값과 보스 체력 역산");
    card(s, 0.6, 1.65, 4.0, 5.2);
    text(s, "고양이 기준값", { x: 0.85, y: 1.8, w: 3.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const cat = [["체력", "100"], ["이동속도", "6.5m/초"], ["대쉬 거리", "5m"], ["대쉬 시간", "0.18초"], ["대쉬 횟수", "최대 3회"], ["대쉬 충전", "2초마다 1회"]];
    cat.forEach((c, i) => {
      const y = 2.35 + i * 0.6;
      text(s, c[0], { x: 0.85, y, w: 1.6, h: 0.45, fontSize: 14, color: MUTED, valign: "middle" });
      text(s, c[1], { x: 2.4, y, w: 2.0, h: 0.45, fontSize: 16, bold: true, align: "right", valign: "middle" });
    });
    text(s, "패턴의 예고 시간 · 탄속 · 범위는 이 값으로 노히트가 가능한지 확인해서 정함", { x: 0.85, y: 6.0, w: 3.55, h: 0.75, fontSize: 11.5, color: MUTED });

    card(s, 4.85, 1.65, 7.85, 2.95);
    text(s, "보스 체력 역산", { x: 5.1, y: 1.8, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const f = [["목표 처치 시간", CARD], ["×", null], ["그 시점 예상 화력", CARD], ["×", null], ["명중률 60%", CARD], ["=", null], ["보스 체력", RED]];
    let fx = 5.1;
    f.forEach((t) => {
      if (!t[1]) { text(s, t[0], { x: fx, y: 2.45, w: 0.32, h: 0.6, fontSize: 18, bold: true, color: GOLD, align: "center", valign: "middle" }); fx += 0.32; return; }
      const w = t[0].length > 7 ? 1.8 : 1.3;
      card(s, fx, 2.45, w, 0.6, t[1]);
      text(s, t[0], { x: fx, y: 2.45, w, h: 0.6, fontSize: 12.5, bold: true, align: "center", valign: "middle" });
      fx += w;
    });
    bullets(s, [
      "시작 화력: 초당 약 20 (기본 탄 8 · 0.3초 간격 · 12발 쏘면 장전 1.2초)",
      "화력은 빌드가 쌓인 만큼 커진다고 가정해, 챕터 기획서에서 보스별 배율을 정함",
    ], { x: 5.1, y: 3.3, w: 7.4, h: 1.2, fontSize: 13 });

    card(s, 4.85, 4.8, 7.85, 2.05);
    text(s, "보스 패턴 템포 기본값", { x: 5.1, y: 4.95, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    [["예고", "0.6초", PURPLE], ["빈틈", "0.9초", TEAL]].forEach((t, i) => {
      const x = 5.1 + i * 2.2;
      card(s, x, 5.5, 2.0, 1.05, t[2]);
      text(s, t[0], { x, y: 5.55, w: 2.0, h: 0.4, fontSize: 13, align: "center" });
      text(s, t[1], { x, y: 5.92, w: 2.0, h: 0.5, fontSize: 20, bold: true, align: "center" });
    });
    text(s, "패턴마다 따로 정한 값이 있으면 그 값을 우선 적용", { x: 9.6, y: 5.5, w: 2.95, h: 1.0, fontSize: 12, color: MUTED, valign: "middle" });
  }

  return D.pres.writeFile({ fileName: path.join(OUT_DIR, "1159_공통_레벨디자인_기획서_20261116_정승운_Ver01.pptx") });
}

// =====================================================================
// 2) 1장 기획서: 일그러진 서부 마을 (보스 상세 포함)
// =====================================================================
function buildCh1() {
  const SEC = ["1장 개요", "스테이지 구성", "일반 적", "중간 보스", "보스", "1장 수치"];
  const D = makeDeck("11:59 1장 기획서 Ver01", `11:59 · 1장 기획서: 일그러진 서부 마을 Ver01 · ${AUTHOR}`, SEC);
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const ROOM = has("ch1/ch1_room.jpg") ? "ch1/ch1_room.jpg" : "02_ingame.png";

  const cv = D.cover("05_zone1_western.png", "1장 기획서", "일그러진 서부 마을 · 중간 보스 · 보스 상세", "Ver 01");
  cv.addNotes("공통 규칙(챕터 구조 · 전투방 · 보스 세 박자 · 수치 기준)은 공통 레벨 디자인 기획서를 따름");
  D.toc("ch1/ch1_boss.jpg");

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "일그러진 서부 마을");
    picture(s, "05_zone1_western.png", 0.6, 1.6, 7.3, 4.1, "1장 배경");
    caption(s, "1장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "고양이 사수의 여정이 시작되는 첫 챕터. 서부극 배경 위로 녹아내린 시계가 하나둘 보이기 시작함", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14, color: TXT });
    const rows = [
      ["테마", "녹아내린 시계가 걸린 황무지 서부 마을"],
      ["오브젝트", "녹아내린 시계(장식) · 화약통 · 부서지는 엄폐물(마차 · 상자 · 울타리)"],
      ["상인", "술집 바텐더"],
      ["일반 적", "태엽 무법자 · 시계추 깡패 · 모래시계 폭탄병"],
      ["중간 보스", "태엽 현상금 사냥꾼"],
      ["보스", "녹은 시계 거인"],
    ];
    rows.forEach((r, i) => {
      const y = 1.6 + i * 0.88;
      card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
      text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: 12.5, valign: "middle", lineSpacingMultiple: 1.05 });
    });
  }

  // 02 스테이지 구성
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "마을 바깥에서 중심의 광장으로");
    const flow = [
      ["스테이지 1", "황무지 절벽길", PURPLE], ["스테이지 2", "마을 어귀", PURPLE], ["중간 보스", "술집 앞 큰길", "8C3A50"],
      ["스테이지 3", "거리 · 건물 내부", PURPLE], ["스테이지 4", "광장 근처", PURPLE], ["보스", "원형 광장", RED],
    ];
    flow.forEach((f, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.65, 1.76, 1.1, f[2]);
      text(s, f[0], { x, y: 1.72, w: 1.76, h: 0.4, fontSize: 12.5, bold: true, color: GOLD, align: "center" });
      text(s, f[1], { x, y: 2.1, w: 1.76, h: 0.5, fontSize: 14, bold: true, align: "center" });
      if (i < 5) arrow(s, x + 1.79, 2.2, 0.22, GOLD);
    });
    // 녹는 정도 막대
    s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: 2.95, w: 12.1, h: 0.12, fill: { type: "solid", color: LAV, transparency: 40 }, line: { color: LAV, transparency: 100 } });
    s.addShape(pres.shapes.RECTANGLE, { x: 6.8, y: 2.95, w: 5.9, h: 0.12, fill: { color: GOLD, transparency: 10 }, line: { color: GOLD, transparency: 100 } });
    text(s, "마을 중심에 가까워질수록 건물과 시계가 더 녹아내린 모습", { x: 0.6, y: 3.12, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED, align: "center" });

    picture(s, ROOM, 0.6, 3.65, 5.6, 3.15, "전투방 컨셉");
    bullets(s, [
      { t: "전투방 종류", b: true, c: GOLD },
      "서부 거리 · 마을 광장 · 황무지 절벽길 · 건물 내부(술집 · 보안관 사무소)",
      "직접 짠 전투방 10개 → 스테이지 장소에 맞는 방 중에서 무작위로 뽑음",
      { t: "배치", b: true, c: GOLD },
      "방 크기 1.5~2 화면 · 방마다 웨이브 2~3번 · 다음 웨이브는 1초 예고 후 등장",
      "화약통은 방마다 2~4개, 엄폐물과 함께 후보 자리 중 일부에만 놓임",
      "방을 비우면 15% 확률로 한쪽 바닥에서 술집 카운터가 솟아오르며 상인 등장",
      "황무지 절벽 낭떠러지는 장식 · 보이지 않는 벽으로 막혀 있음",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 12.5, paraSpaceAfter: 4 });
  }

  // 03 일반 적
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시간의 하수인 3종");
    picture(s, "ch1/ch1_enemies.jpg", 0.6, 1.6, 12.1, 2.7, "일반 적 컨셉", "cover");
    const mobs = [
      ["태엽 무법자", "원거리", "거리를 두고 리볼버로 한 발씩 견제", ["이속 2.6m/초", "2초마다 1발 (예고 0.5초)", "피해 8 · 내구 24 (기본 탄 3발)"]],
      ["시계추 깡패", "돌진 근접", "시계추를 들고 일직선으로 돌진", ["이속 3.4m/초", "돌진 예고 0.6초", "피해 12 · 내구 48 (기본 탄 6발)"]],
      ["모래시계 폭탄병", "자폭", "빠르게 달려와 붙은 뒤 폭발", ["이속 4.5m/초", "붙으면 0.8초 점멸 후 반경 2m 폭발", "피해 12 · 내구 16 (기본 탄 2발)"]],
    ];
    mobs.forEach((m, i) => {
      const x = 0.6 + i * 4.1;
      card(s, x, 4.45, 3.9, 2.4);
      text(s, m[0], { x: x + 0.25, y: 4.58, w: 2.6, h: 0.4, fontSize: 17, bold: true, color: GOLD });
      chip(s, m[1], x + 2.75, 4.62, 0.95, [TEAL, RED, "B5622E"][i], i === 0 ? BG : TXT, { size: 11, h: 0.32 });
      text(s, m[2], { x: x + 0.25, y: 5.02, w: 3.5, h: 0.35, fontSize: 13 });
      bullets(s, m[3], { x: x + 0.25, y: 5.45, w: 3.5, h: 1.3, fontSize: 12, paraSpaceAfter: 3, color: MUTED });
    });
  }
  {
    const s = content(3, "강화형: 스테이지 3부터 등장");
    text(s, "중간 보스를 쓰러뜨린 뒤의 하수인은 전부 강화형. 더 녹아내린 모습에 공격이 하나씩 추가되고 내구는 1.5배", { x: 0.6, y: 1.55, w: 12.1, h: 0.5, fontSize: 14, color: MUTED });
    const W = 3.95, H = 4.6;
    const ups = [
      ["태엽 무법자+", "굴러 피하기 사격", "고양이가 조준하면 옆으로 구르며 한 발", "4초 쿨 · 3m 구르기 · 1발", (bx, by, sz) => {
        const cx = bx + sz * 0.62, cy = by + sz * 0.5;
        D.mob(s, cx, cy - 0.35, 0.14); D.circle(s, cx, cy + 0.4, 0.14, LAV, { tr: 60, line: LAV, dash: "dash" });
        D.line(s, cx, cy - 0.2, 0, 0.47, GOLD, { end: "triangle", dash: "dash" });
        D.cat(s, bx + 0.35, cy); D.line(s, bx + 0.5, cy, cx - bx - 0.65, -0.3, GOLD, { dash: "sysDot" });
        D.line(s, cx - 0.15, cy + 0.38, bx + 0.52 - (cx - 0.15), cy + 0.05 - (cy + 0.38), RED, { end: "triangle" });
      }],
      ["시계추 깡패+", "회전 휘두르기", "시계추를 들고 한 바퀴 돌며 주변을 원형으로 타격", "예고 0.7초 · 반경 2.5m · 피해 12", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.4, RED, { tr: 60, line: RED });
        D.mob(s, cx, cy, 0.15);
        s.addShape(pres.shapes.ARC, { x: cx - sz * 0.3, y: cy - sz * 0.3, w: sz * 0.6, h: sz * 0.6, angleRange: [200, 140], line: { color: GOLD, width: 2, endArrowType: "triangle" } });
      }],
      ["모래시계 폭탄병+", "모래 장판", "터진 자리에 이속을 늦추는 모래 장판이 잠깐 남음", "반경 2m · 3초 · 이속 30% 감소", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.38, "C9A35A", { tr: 45, line: GOLD, dash: "dash" });
        for (let k = 0; k < 7; k++) { const a = k * 0.9; D.circle(s, cx + Math.cos(a) * sz * 0.22, cy + Math.sin(a) * sz * 0.2, 0.025, GOLD); }
        D.cat(s, cx + sz * 0.12, cy + sz * 0.05, 0.11);
      }],
    ];
    ups.forEach((u, i) => {
      const x = 0.6 + i * 4.08, y = 2.2;
      card(s, x, y, W, H);
      const sz = 2.0;
      D.rect(s, x + 0.25, y + 0.25, W - 0.5, sz, FLOOR, { line: FLOOR });
      u[4](x + 0.25 + (W - 0.5 - sz) / 2, y + 0.25, sz);
      text(s, u[0], { x: x + 0.25, y: y + 2.45, w: W - 0.5, h: 0.32, fontSize: 12, bold: true, color: MUTED });
      text(s, u[1], { x: x + 0.25, y: y + 2.78, w: W - 0.5, h: 0.42, fontSize: 18, bold: true, color: GOLD });
      text(s, u[2], { x: x + 0.25, y: y + 3.25, w: W - 0.5, h: 0.7, fontSize: 13.5 });
      text(s, u[3], { x: x + 0.25, y: y + 4.0, w: W - 0.5, h: 0.4, fontSize: 12, color: MUTED });
    });
  }

  // 04 중간 보스
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "태엽 현상금 사냥꾼");
    picture(s, "ch1/ch1_midboss.jpg", 0.6, 1.6, 7.3, 4.1, "중간 보스 컨셉");
    caption(s, "중간 보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "태엽 무법자들의 대장. 산탄총을 쏘며 끈질기게 다가오고, 휘파람으로 부하를 불러냄", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    const rows = [
      ["등장", "스테이지 2와 3 사이의 별도 방"],
      ["경기장", "술집 앞 큰길 30×14m · 양옆 건물 = 돌진 태클이 부딪히는 벽"],
      ["이동", "산탄 사거리를 잡으려고 계속 다가오며 압박 (이속 4m/초)"],
      ["체력", "1080 (목표 처치 시간 60초)"],
      ["페이즈", "1페이즈 · 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["고유 기믹", "현상금 표식"],
    ];
    rows.forEach((r, i) => {
      const y = 1.6 + i * 0.88;
      card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
      text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: 12, valign: "middle", lineSpacingMultiple: 1.05 });
    });
  }
  {
    const s = content(4, "고유 기믹: 현상금 표식");
    // 도식: 표식 → 5초 → 판정
    const steps = [
      ["① 표식", "20초마다 고양이 머리 위에\n숫자 표식(2~4)이 붙음"],
      ["② 5초 카운트", "그동안 쏘거나 장전해서\n바늘 위치를 옮김"],
      ["③ 판정", "바늘이 표식 숫자의 배수 칸에 있으면 최대 체력 20% 피해"],
    ];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.52;
      card(s, x, 1.65, 2.3, 1.85, i === 2 ? "5A2430" : CARD);
      text(s, st[0], { x: x + 0.2, y: 1.78, w: 2.0, h: 0.4, fontSize: 15, bold: true, color: GOLD });
      text(s, st[1], { x: x + 0.2, y: 2.25, w: 2.0, h: 1.15, fontSize: 12, lineSpacingMultiple: 1.15 });
      if (i < 2) arrow(s, x + 2.32, 2.57, 0.18, GOLD);
    });
    // 예시 시계 (표식 3)
    const cx = 2.5, cy = 5.1, R = 1.05;
    s.addShape(pres.shapes.OVAL, { x: cx - 1.45, y: cy - 1.45, w: 2.9, h: 2.9, fill: { color: CARD }, line: { color: GOLD, width: 2 }, objectName: "mark_clock" });
    for (let k = 1; k <= 12; k++) {
      const a = (k * 30 - 90) * Math.PI / 180, on = k % 3 === 0;
      const x = cx + Math.cos(a) * R, y = cy + Math.sin(a) * R;
      D.circle(s, x, y, 0.19, on ? RED : PURPLE, { line: on ? "FF8A80" : LAV });
      text(s, String(k), { x: x - 0.19, y: y - 0.19, w: 0.38, h: 0.38, fontSize: 10, bold: true, align: "center", valign: "middle" });
    }
    D.line(s, cx, cy, Math.cos((5 * 30 - 90) * Math.PI / 180) * 0.7, Math.sin((5 * 30 - 90) * Math.PI / 180) * 0.7, TXT, { width: 3 });
    D.circle(s, cx, cy, 0.08, GOLD);
    chip(s, "표식 3", cx - 0.5, cy - 0.62, 1.0, RED, TXT, { size: 11, h: 0.3 });
    bullets(s, [
      { t: "예: 표식 3", b: true, c: GOLD },
      "3 · 6 · 9 · 12시에 바늘이 있으면 피해",
      "지금 바늘은 5시 → 한 발 쏘면 6시가 되어 위험",
      "그대로 두거나 두 발 쏴서 7시로 넘기면 안전",
    ], { x: 4.25, y: 4.0, w: 3.6, h: 2.6, fontSize: 12.5, paraSpaceAfter: 4 });
    card(s, 8.2, 1.65, 4.5, 5.2, PURPLE);
    text(s, "발사 시각이란", { x: 8.45, y: 1.8, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "바늘이 가리키는 칸 = 다음에 쏠 실린더 칸 번호",
      "쏘면 바늘이 한 칸 넘어감",
      "장전하면 첫 칸으로 돌아감",
    ], { x: 8.45, y: 2.3, w: 4.0, h: 1.6, fontSize: 13.5 });
    text(s, "보스와 반대 구조", { x: 8.45, y: 4.05, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "녹은 시계 거인: 바늘을 얼굴 시각에 맞춰야 피해 없음",
      "현상금 사냥꾼: 바늘이 표식 숫자의 배수를 피해야 피해 없음",
    ], { x: 8.45, y: 4.5, w: 4.0, h: 2.2, fontSize: 13.5, lineSpacingMultiple: 1.15 });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    const bx0 = (bx, sz) => bx + sz * 0.22;
    D.patternCard(s, X[0], Y[0], W, H, "산탄 부채꼴", "앞으로 산탄을 부채꼴로 퍼뜨림. 가까울수록 많이 맞음", "5발 · 60° · 사거리 7m\n발당 피해 5 (다 맞으면 25)", (bx, by, sz) => {
      const cx = bx0(bx, sz), cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.72, -30, 30, RED, { tr: 60 });
      for (let k = 0; k < 5; k++) { const a = (-30 + k * 15) * Math.PI / 180; D.circle(s, cx + Math.cos(a) * sz * 0.6, cy + Math.sin(a) * sz * 0.6, 0.04, GOLD); }
      D.boss(s, cx, cy, 0.15);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "회전 난사", "한 바퀴 반을 돌며 양쪽으로 쉬지 않고 쏨", "2초 · 0.1초마다 두 방향 발사\n발당 피해 8", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      for (let k = 0; k < 8; k++) { const a = k * Math.PI / 4 + 0.2; const r = sz * (0.22 + 0.025 * k); D.circle(s, cx + Math.cos(a) * r, cy + Math.sin(a) * r, 0.04, GOLD); D.circle(s, cx - Math.cos(a) * r, cy - Math.sin(a) * r, 0.04, GOLD); }
      s.addShape(pres.shapes.ARC, { x: cx - sz * 0.42, y: cy - sz * 0.42, w: sz * 0.84, h: sz * 0.84, angleRange: [300, 220], line: { color: LAV, width: 1.5, endArrowType: "triangle" } });
      D.boss(s, cx, cy, 0.15);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "수배 저격", "조준선으로 고양이를 쫓다가 멈춘 방향으로 관통탄 한 발", "추적 1.2초 → 고정 0.4초 → 발사\n피해 15", (bx, by, sz) => {
      const cy = by + sz / 2, cx = bx + 0.25;
      D.line(s, cx, cy, sz - 0.35, 0, RED, { dash: "dash", width: 1.25 });
      D.cat(s, bx + sz - 0.35, cy, 0.11);
      D.circle(s, bx + sz - 0.35, cy, 0.2, null, { line: RED, lineW: 1 });
      D.boss(s, cx, cy, 0.15);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "돌진 태클", "일직선으로 돌진. 벽에 부딪히면 잠깐 기절해 큰 빈틈이 생김", "예고 0.6초 · 14m/초 · 피해 15\n벽에 부딪히면 2초 기절", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + sz - 0.2, by + 0.1, 0.12, sz - 0.2, "6B4A2E", { line: "8A6440" });
      D.rect(s, bx + 0.25, cy - 0.15, sz - 0.6, 0.3, RED, { tr: 60, line: RED });
      D.arrow(s, bx + 0.45, cy, sz - 0.75, GOLD, { width: 2 });
      D.boss(s, bx + 0.28, cy, 0.15);
      text(s, "기절", { x: bx + sz - 0.75, y: by + 0.15, w: 0.5, h: 0.25, fontSize: 9, bold: true, color: GOLD });
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "하수인 호출", "휘파람을 불어 태엽 무법자를 불러냄", "2명 소환 · 필드 최대 4명", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.45, -0.4], [0.45, -0.35]].forEach(([dx, dy]) => { D.circle(s, cx + dx, cy + dy, 0.17, LAV, { tr: 70, line: LAV, dash: "dash" }); D.mob(s, cx + dx, cy + dy); });
      D.boss(s, cx, cy + 0.25, 0.15);
      text(s, "+2", { x: cx - 0.25, y: by + 0.08, w: 0.5, h: 0.3, fontSize: 12, bold: true, color: GOLD, align: "center" });
    }, "거리 무관");
    // 선택 규칙
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 5m 안",
      "거리에 맞는 후보 70% · 나머지 30%",
      "하수인 호출은 거리와 상관없이 후보",
      "직전 패턴은 제외",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 12.5, paraSpaceAfter: 3 });
    D.legend(s, 0.6, 6.74);
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "녹은 시계 거인");
    picture(s, "ch1/ch1_boss.jpg", 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "1장의 출구를 지키는 시간의 존재. 녹아내린 회중시계 얼굴에 시각을 띄우고, 거대한 시침과 분침을 휘두름", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    const rows = [
      ["경기장", "원형 마을 광장 · 지름 24m · 장애물 없음 · 가장자리에 녹아내린 시계탑과 건물"],
      ["이동", "느리게 걸어서 고양이를 따라옴 (이속 3m/초)"],
      ["체력", "2700 (목표 처치 시간 90초)"],
      ["페이즈", "1페이즈 · 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["상시 탄막", "초침 탄"],
      ["고유 기믹", "시각 패턴"],
    ];
    rows.forEach((r, i) => {
      const y = 1.6 + i * 0.88;
      card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
      text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: 12, valign: "middle", lineSpacingMultiple: 1.05 });
    });
  }
  {
    const s = content(5, "고유 기믹: 시각 패턴");
    // 경기장 도식
    const ax = 3.0, ay = 4.3, AR = 2.35;
    D.circle(s, ax, ay, AR, FLOOR, { line: LAV, lineW: 1.5 });
    D.circle(s, ax + 1.0, ay - 0.9, 0.42, TEAL, { tr: 40, line: TEAL, lineW: 1.5 });
    text(s, "안전 지대", { x: ax + 0.5, y: ay - 1.65, w: 1.0, h: 0.3, fontSize: 10, bold: true, color: TEAL, align: "center" });
    D.boss(s, ax - 0.35, ay + 0.3, 0.32);
    // 얼굴 시계
    s.addShape(pres.shapes.OVAL, { x: ax - 0.35 - 0.2, y: ay + 0.3 - 0.2, w: 0.4, h: 0.4, fill: { color: "F3EDE2" }, line: { color: GOLD, width: 1 } });
    text(s, "4", { x: ax - 0.55, y: ay + 0.1, w: 0.4, h: 0.4, fontSize: 12, bold: true, color: BG, align: "center", valign: "middle" });
    D.cat(s, ax - 1.3, ay + 1.15);
    D.line(s, ax - 1.15, ay + 1.0, 1.9, -1.65, GOLD, { dash: "dash", end: "triangle" });
    // 초침 탄
    for (let k = 0; k < 4; k++) D.circle(s, ax - 0.35 - 0.55 - k * 0.38, ay + 0.3 - 0.35 - k * 0.22, 0.04, "FF8A80");
    caption(s, "광장 탑뷰 · 얼굴 시계에 '4시'", 0.65, 6.72, 4.8, { align: "center" });

    const steps = [
      ["① 예고 5초", "얼굴 시계에 시각이 뜨고, 광장 무작위 위치에 안전 지대(반경 2m) 1곳 표시"],
      ["② 맞추기", "안전 지대로 가면서 쏘거나 장전해 내 발사 시각을 얼굴 시각에 맞춤"],
      ["③ 판정", "발동하는 순간에 한 번만 판정. 안전 지대 안에 있고 시각까지 맞으면 피해 없이 파훼"],
    ];
    steps.forEach((st, i) => {
      const y = 1.6 + i * 1.12;
      card(s, 5.8, y, 6.9, 1.0, i === 2 ? "1F4450" : CARD);
      text(s, st[0], { x: 6.05, y, w: 1.5, h: 1.0, fontSize: 15, bold: true, color: GOLD, valign: "middle" });
      text(s, st[1], { x: 7.55, y, w: 5.0, h: 1.0, fontSize: 13, valign: "middle", lineSpacingMultiple: 1.1 });
    });
    bullets(s, [
      "실패하면 최대 체력 40% 피해",
      "체력 75 · 50 · 25%에 한 번씩 확정 등장",
      "예고 동안 보스는 멈추고 초침 탄만 계속 → 바늘을 맞추려고 쏜 탄도 보스에게 들어감",
      "발사 시각: 쏘면 한 칸 넘어가고, 장전하면 첫 칸으로 돌아감",
    ], { x: 5.8, y: 5.05, w: 6.9, h: 1.85, fontSize: 13, paraSpaceAfter: 4 });
  }
  {
    const s = content(5, "상시 탄막과 패턴 고르기");
    card(s, 0.6, 1.6, 5.4, 5.25);
    text(s, "초침 탄", { x: 0.85, y: 1.75, w: 3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const cx = 3.3, cy = 3.65, R = 1.35;
    D.circle(s, cx, cy, R, FLOOR, { line: LAV });
    for (let k = 0; k < 12; k++) { const a = (k * 30 - 90) * Math.PI / 180; D.line(s, cx + Math.cos(a) * (R - 0.12), cy + Math.sin(a) * (R - 0.12), Math.cos(a) * 0.1, Math.sin(a) * 0.1, LAV, { width: 1 }); }
    const a = (40 - 90) * Math.PI / 180;
    D.line(s, cx, cy, Math.cos(a) * 1.0, Math.sin(a) * 1.0, RED, { width: 2.5 });
    [1.5, 1.85, 2.2].forEach((r) => D.circle(s, cx + Math.cos(a) * r, cy + Math.sin(a) * r, 0.05, "FF8A80"));
    D.boss(s, cx, cy, 0.17);
    bullets(s, [
      "초침이 돌며 매 초 그 방향으로 바늘 탄 1발",
      "패턴과 별개로 전투 내내 계속",
      "탄속 9m/초 · 피해 10",
    ], { x: 0.85, y: 5.3, w: 5.0, h: 1.45, fontSize: 13.5, paraSpaceAfter: 3 });

    text(s, "패턴 고르기", { x: 6.3, y: 1.65, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const groups = [
      ["가까울 때 (6m 안)", ["시계 바늘 휘두르기", "녹은 웅덩이 솟기"], RED],
      ["멀 때", ["12방향 숫자 탄", "되감기 폭발"], PURPLE],
      ["거리 무관", ["시간 지연 구역"], TEAL],
    ];
    groups.forEach((g, i) => {
      const y = 2.15 + i * 1.2;
      card(s, 6.3, y, 6.4, 1.05);
      s.addShape(pres.shapes.RECTANGLE, { x: 6.3, y: y + 0.15, w: 0.07, h: 0.75, fill: { color: g[2] }, line: { color: g[2] } });
      text(s, g[0], { x: 6.55, y, w: 2.0, h: 1.05, fontSize: 13.5, bold: true, valign: "middle" });
      g[1].forEach((p, k) => chip(s, p, 8.6 + k * 2.0, y + 0.33, 1.9, g[2], g[2] === TEAL ? BG : TXT, { size: 11.5, h: 0.4 }));
    });
    bullets(s, [
      "거리에 맞는 후보 70% · 나머지 30%",
      "직전 패턴은 제외",
      "시각 패턴은 추첨과 별개로 체력 75 · 50 · 25%에 확정",
    ], { x: 6.3, y: 5.8, w: 6.4, h: 1.1, fontSize: 13, paraSpaceAfter: 3 });
  }
  {
    const s = content(5, "보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "시계 바늘 휘두르기", "거대한 시침 · 분침을 원으로 휘둘러 부채꼴 안전 구역만 남김", "바늘 길이 8m · 안전 부채꼴 90°\n예고 0.8초 · 피해 20", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.46, RED, { tr: 60, line: RED });
      D.pie(s, cx, cy, sz * 0.46, 200, 290, TEAL, { tr: 25 });
      D.cat(s, cx - sz * 0.12, cy - sz * 0.25, 0.09);
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "녹은 웅덩이 솟기", "고양이 주변에 원 4개를 예고한 뒤 녹은 웅덩이가 솟아오름", "반경 1.5m × 4 · 예고 0.8초 · 피해 15\n남은 웅덩이 6초 · 밟으면 이속 30% 감소", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.35, -0.3], [0.35, -0.32], [-0.32, 0.35], [0.38, 0.3]].forEach(([dx, dy]) => D.circle(s, cx + dx, cy + dy, 0.24, "C9A35A", { tr: 40, line: GOLD }));
      D.cat(s, cx, cy, 0.1);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "12방향 숫자 탄", "1시부터 12시까지 차례로 쏴서 나선처럼 퍼져 나감", "0.1초 간격 · 탄속 8m/초 · 피해 15", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      for (let k = 1; k <= 12; k++) { const a = (k * 30 - 90) * Math.PI / 180, r = sz * (0.16 + k * 0.025); D.circle(s, cx + Math.cos(a) * r, cy + Math.sin(a) * r, 0.05, k === 1 ? GOLD : "FF8A80"); }
      D.boss(s, cx, cy, 0.14);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "되감기 폭발", "3초 전부터 고양이가 지나온 길 6곳이 차례로 폭발", "0.25초 간격 · 반경 1.5m · 피해 15", (bx, by, sz) => {
      const pts = [[0.2, 0.8], [0.32, 0.6], [0.45, 0.48], [0.58, 0.42], [0.7, 0.33], [0.8, 0.22]];
      pts.forEach(([u, v], k) => D.circle(s, bx + u * sz, by + v * sz, 0.13, RED, { tr: 35 + k * 8, line: RED }));
      D.cat(s, bx + 0.9 * sz, by + 0.14 * sz, 0.09);
      text(s, "1", { x: bx + 0.2 * sz - 0.13, y: by + 0.8 * sz - 0.13, w: 0.26, h: 0.26, fontSize: 9, bold: true, align: "center", valign: "middle" });
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "시간 지연 구역", "원 안의 시간이 느려지고 그 안으로 탄이 쏟아짐", "반경 4m · 4초 유지\n원 안에서 이속 · 공속 40% 감소", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.42, TEAL, { tr: 55, line: TEAL, dash: "dash" });
      [[-0.3, -0.2], [0.2, -0.35], [0.3, 0.15], [-0.15, 0.3], [0.05, -0.05]].forEach(([dx, dy]) => D.circle(s, cx + dx, cy + dy, 0.045, "FF8A80"));
      D.cat(s, cx + 0.1, cy + 0.12, 0.09);
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "피하는 법", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "휘두르기: 예고된 안전 부채꼴로 이동",
      "웅덩이: 원 밖으로 대쉬, 남은 웅덩이는 피해 다님",
      "숫자 탄: 나선 사이 빈틈으로 이동",
      "되감기: 멈추지 말고 계속 이동",
      "지연 구역: 원 밖으로 빠져나감",
    ], { x: X[2] + 0.25, y: Y[1] + 0.58, w: W - 0.4, h: 1.85, fontSize: 11.5, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
    s.addNotes("시계 바늘 휘두르기 레퍼런스: 메이플스토리 파풀라투스 (안전 구역만 남기고 전 범위 공격)");
  }

  // 06 1장 수치
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "1장 수치 정리");
    D.table(s, [
      ["대상", "체력 · 내구", "이속", "피해", "비고"],
      ["태엽 무법자", "24", "2.6m/초", "8", "2초마다 1발 · 예고 0.5초"],
      ["시계추 깡패", "48", "3.4m/초", "12", "돌진 예고 0.6초"],
      ["모래시계 폭탄병", "16", "4.5m/초", "12", "0.8초 점멸 후 반경 2m 폭발"],
      ["강화형", "기본 ×1.5", "-", "-", "공격 하나 추가"],
      ["태엽 현상금 사냥꾼", "1080", "4m/초", "5~15", "목표 60초 · 경기장 30×14m"],
      ["녹은 시계 거인", "2700", "3m/초", "10~20", "목표 90초 · 경기장 지름 24m"],
    ], 0.6, 1.6, [2.3, 1.45, 1.25, 1.0, 3.4], { rowH: 0.48, size: 12 });
    const cards = [
      ["피해 기준", "일반 적 8~12\n중간 보스 12~15 · 보스 15~20\n(기믹 실패는 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 약 20 × 명중률 60%\n중간 보스 시점 화력 ×1.5 · 60초\n보스 시점 화력 ×2.5 · 90초"],
      ["전투방", "웨이브당 적: 스테이지 1은 4~6마리\n→ 스테이지 4는 6~9마리\n다음 웨이브 예고 1초"],
      ["오브젝트 · 상인", "화약통 방마다 2~4개 · 반경 3m\n피해: 적 40 · 고양이 15\n상인 15% · 챕터당 최소 1번"],
    ];
    cards.forEach((c, i) => {
      const y = 1.6 + i * 1.32;
      card(s, 10.2 - 0.0, y, 2.5, 1.2, i % 2 ? CARD : PURPLE);
      text(s, c[0], { x: 10.35, y: y + 0.08, w: 2.3, h: 0.3, fontSize: 12.5, bold: true, color: GOLD });
      text(s, c[1], { x: 10.35, y: y + 0.38, w: 2.3, h: 0.8, fontSize: 9.5, lineSpacingMultiple: 1.1 });
    });
    text(s, "보스 체력은 '목표 처치 시간 × 그 시점 예상 화력 × 명중률'로 역산 (공통 레벨 디자인 기획서 08 참고)", { x: 0.6, y: 5.55, w: 9.3, h: 0.4, fontSize: 12.5, color: MUTED });
    text(s, "중간 보스 1080 = 20 × 1.5 × 60초 × 0.6  ·  보스 2700 = 20 × 2.5 × 90초 × 0.6", { x: 0.6, y: 6.0, w: 9.3, h: 0.4, fontSize: 13, bold: true, color: TXT });
  }

  return D.pres.writeFile({ fileName: path.join(OUT_DIR, "1159_1장_기획서_20261116_정승운_Ver01.pptx") });
}

(async () => {
  const outs = [await buildCommon(), await buildCh1()];
  const py = process.env.PYTHON || "C:/Users/user/AppData/Local/Programs/Python/Python312/python.exe";
  for (const o of outs) {
    try { require("child_process").execFileSync(py, [path.join(__dirname, "pptx_compress.py"), o], { stdio: "inherit" }); }
    catch (e) { console.warn("compress skipped:", e.message); }
    console.log("saved", o);
  }
})();
