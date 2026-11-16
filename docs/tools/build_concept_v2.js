// 11:59 컨셉 기획서 Ver02 (docs/11_59_컨셉 기획_정승운_20261116.md 기준, 이미지 위주로 새로 구성)
// 실행: NODE_PATH=<pptxgenjs가 있는 node_modules> node build_concept_v2.js
//       테마 색은 PPTX_SKILL(apply_theme.js 위치)이 있으면 적용. OUT_DIR로 출력 폴더 지정 (기본 docs/)
const pptxgen = require("pptxgenjs");
const sizeOf = require("image-size");
const path = require("path");
const fs = require("fs");

const DOCS = path.resolve(__dirname, "..");
const IMG = path.join(DOCS, "images");
const OUT = path.join(process.env.OUT_DIR || DOCS, "1159_컨셉_기획서_20261116_정승운_Ver02.pptx");
const AUTHOR = "정승운";
const img = (n) => path.join(IMG, n);
const has = (n) => fs.existsSync(img(n));
const dims = (n) => { const d = sizeOf(fs.readFileSync(img(n))); return { w: d.width, h: d.height }; };

const THEME = {
  name: "1159 Clock",
  headFontFace: "Noto Sans KR",
  bodyFontFace: "Noto Sans KR",
  colors: {
    dk1: "17131F", lt1: "F3EDE2", dk2: "241D30", lt2: "A99FB5",
    accent1: "E0A63A", accent2: "C2343B", accent3: "3B2D5C",
    accent4: "2C2440", accent5: "5FA8B8", accent6: "8C7AA8",
    hlink: "E0A63A", folHlink: "8C7AA8",
  },
};
const H = THEME.colors;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.333 x 7.5
pres.title = "11:59 컨셉 기획서 Ver02";
pres.author = AUTHOR;
pres.company = AUTHOR;
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
const C = pres.SchemeColor;
const BG = C.text1, CARD = C.accent4, TXT = C.background1, MUTED = C.background2;
const GOLD = C.accent1, RED = C.accent2, TEAL = C.accent5, PURPLE = C.accent3, LAV = C.accent6;

// ---------- 레이아웃 ----------
pres.defineSlideMaster({
  title: "CONTENT",
  background: { color: BG },
  objects: [
    { text: { text: `11:59 · 컨셉 기획서 Ver02 · ${AUTHOR}`, options: { x: 0.6, y: 7.05, w: 6, h: 0.3, fontSize: 10, color: MUTED, margin: 0 } } },
    {
      placeholder: {
        options: { name: "title", type: "title", x: 0.6, y: 0.62, w: 12.1, h: 0.75, fontSize: 30, bold: true, color: TXT, valign: "middle", align: "left", margin: 0 },
        text: "제목",
      },
    },
  ],
  slideNumber: { x: 12.2, y: 7.05, w: 0.5, h: 0.3, fontSize: 10, color: MUTED, align: "right" },
});
pres.defineSlideMaster({ title: "COVER", background: { color: BG }, objects: [] });

let sid = 0;
const nm = (s) => `${s}_${++sid}`;
function text(slide, t, o) {
  slide.addText(t, { isTextBox: true, fontFace: THEME.bodyFontFace, lang: "ko-KR", color: TXT, fontSize: 15, margin: 0, valign: "top", objectName: nm("text"), ...o });
}
function card(slide, x, y, w, h, fill = CARD, o = {}) {
  slide.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.12, fill: { color: fill, transparency: o.tr || 0 }, line: { color: o.line || fill, width: o.lineW || 1, transparency: o.line ? 0 : (o.tr || 0) }, objectName: nm("card") });
}
function bullets(slide, items, o) {
  const runs = items.map((it, i) => {
    const obj = typeof it === "object";
    const head = obj && it.b;
    return {
      text: obj ? it.t : it,
      options: { bullet: head ? false : { indent: 15 }, breakLine: i < items.length - 1, bold: head, color: obj && it.c ? it.c : TXT, paraSpaceBefore: head && i > 0 ? 8 : 0 },
    };
  });
  text(slide, runs, { fontSize: 15, paraSpaceAfter: 7, lineSpacingMultiple: 1.1, ...o });
}
// 원본 비율 유지. cover = 상자를 채우고 넘치는 쪽을 잘라 냄 / contain = 잘리지 않게 상자 안에 맞춤
function picture(slide, file, x, y, w, h, label, fit = "cover", o = {}) {
  if (!has(file)) {
    slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: CARD }, line: { color: CARD } });
    text(slide, label, { x, y: y + h / 2 - 0.2, w, h: 0.4, align: "center", color: MUTED });
    return { x, y, w, h };
  }
  const { w: iw, h: ih } = dims(file);
  if (fit === "contain") {
    const k = Math.min(w / iw, h / ih), dw = iw * k, dh = ih * k;
    const dx = o.alignLeft ? x : x + (w - dw) / 2, dy = o.alignTop ? y : y + (h - dh) / 2;
    slide.addImage({ path: img(file), x: dx, y: dy, w: dw, h: dh, objectName: nm("img"), altText: label, rounding: false, transparency: o.tr || 0 });
    return { x: dx, y: dy, w: dw, h: dh };
  }
  const k = Math.max(w / iw, h / ih);
  slide.addImage({ path: img(file), x, y, w: iw * k, h: ih * k, sizing: { type: "cover", w, h }, objectName: nm("img"), altText: label, transparency: o.tr || 0 });
  return { x, y, w, h };
}
function caption(slide, t, x, y, w, o = {}) {
  text(slide, t, { x, y, w, h: 0.28, fontSize: 10, color: MUTED, ...o });
}
// 섹션 번호 (제목 위 작은 금색 글자)
function kicker(slide, n, t) {
  text(slide, `${String(n).padStart(2, "0")}  ${t}`, { x: 0.6, y: 0.3, w: 8, h: 0.3, fontSize: 12, bold: true, color: GOLD, charSpacing: 1 });
}
function chip(slide, t, x, y, w, fill = GOLD, color = BG, o = {}) {
  slide.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h: o.h || 0.36, rectRadius: 0.18, fill: { color: fill }, line: { color: fill }, objectName: nm("chip") });
  text(slide, t, { x, y, w, h: o.h || 0.36, fontSize: o.size || 12, bold: true, color, align: "center", valign: "middle" });
}
function arrow(slide, x, y, w, color = MUTED) {
  slide.addShape(pres.shapes.LINE, { x, y, w, h: 0, line: { color, width: 2, endArrowType: "triangle" }, objectName: nm("arrow") });
}
function dimBox(slide, x, y, w, h, tr = 25) {
  slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: BG, transparency: tr }, line: { color: BG, transparency: 100 }, objectName: nm("shade") });
}

const SEC = ["핵심 재미 요소", "컨셉과 세계관", "원페이지 기획서", "비주얼 가이드", "레벨 디자인", "핵심 시스템", "빌드 시스템", "아웃게임 성장 시스템"];
function content(sec, title) {
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: SEC[sec - 1] });
  kicker(s, sec, SEC[sec - 1]);
  s.addText(title, { placeholder: "title" });
  return s;
}

// =============== 표지 ===============
pres.addSection({ title: "표지" });
{
  const s = pres.addSlide({ masterName: "COVER", sectionTitle: "표지" });
  picture(s, "01_keyart.png", 0, 0, 13.333, 7.5, "키 아트");
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 7.75, y: 4.25, w: 5.1, h: 2.8, rectRadius: 0.12, fill: { color: BG, transparency: 18 }, line: { color: BG, transparency: 100 }, objectName: "cover_panel" });
  text(s, "11:59", { x: 8.1, y: 4.42, w: 4.6, h: 0.95, fontSize: 50, bold: true, color: GOLD });
  text(s, "컨셉 기획서", { x: 8.1, y: 5.37, w: 2.6, h: 0.5, fontSize: 24, bold: true });
  chip(s, "Ver 02", 10.5, 5.44, 1.0, RED, TXT, { size: 12 });
  text(s, "3D 탑뷰 빌드형 로그라이크 액션", { x: 8.1, y: 5.92, w: 4.6, h: 0.4, fontSize: 16, color: MUTED });
  text(s, `작성 ${AUTHOR} · 2026.11.16`, { x: 8.1, y: 6.38, w: 4.6, h: 0.3, fontSize: 13 });
  text(s, "이미지는 모두 컨셉 시안", { x: 8.1, y: 6.7, w: 4.6, h: 0.3, fontSize: 11, color: MUTED });
  s.addNotes("제목 뜻: 자정 1분 전. 시계공이 죽음을 앞둔 마지막 1분이자, 주인공의 회중시계가 멈춘 시각");
}

// =============== 목차 ===============
pres.addSection({ title: "목차" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "목차" });
  s.addText("목차", { placeholder: "title" });
  picture(s, "v2/1159v2_02_outer_story.png", 7.2, 0, 6.133, 7.5, "시계공의 마지막 1분");
  // 시계 숫자처럼 8칸
  SEC.forEach((t, i) => {
    const col = i < 4 ? 0 : 1, row = i % 4;
    const x = 0.6 + col * 3.2, y = 1.75 + row * 1.25;
    card(s, x, y, 2.95, 1.0);
    text(s, String(i + 1).padStart(2, "0"), { x: x + 0.22, y: y + 0.15, w: 0.8, h: 0.7, fontSize: 28, bold: true, color: GOLD, valign: "middle" });
    text(s, t, { x: x + 1.0, y: y + 0.15, w: 1.85, h: 0.7, fontSize: 15, bold: true, valign: "middle" });
  });
}

// =============== 01 핵심 재미 요소 ===============
pres.addSection({ title: SEC[0] });
{
  const s = content(1, "빌드를 짜는 재미 + 터뜨리는 쾌감");
  const cols = [
    { img: "icons/sheet_bullets_legendary.png", head: "전략적 빌드 구성", body: "다양한 상황과 드랍된 아이템에 맞추어\n빌드를 전략적으로 구성", tag: "총탄 · 총 · 연산" },
    { img: "v2/1159v2_04_ult.png", head: "화려한 액션 피드백", body: "화려한 액션과 이펙트로 완성된 빌드에 대한\n피드백을 강하게 전달 → 유저에게 성취감 전달", tag: "건카타 궁극기" },
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 6.2;
    picture(s, c.img, x, 1.7, 5.9, 3.32, c.head);
    card(s, x, 5.15, 5.9, 1.7);
    text(s, c.head, { x: x + 0.3, y: 5.3, w: 3.6, h: 0.45, fontSize: 20, bold: true, color: GOLD });
    chip(s, c.tag, x + 5.9 - 0.3 - 1.9, 5.34, 1.9, i ? RED : PURPLE, TXT);
    text(s, c.body, { x: x + 0.3, y: 5.85, w: 5.3, h: 0.9, fontSize: 15, lineSpacingMultiple: 1.15 });
  });
  text(s, "+", { x: 6.28, y: 3.0, w: 0.5, h: 0.6, fontSize: 30, bold: true, color: GOLD, align: "center" });
}

// =============== 02 컨셉과 세계관 ===============
pres.addSection({ title: SEC[1] });
{
  const s = content(2, "안과 밖, 두 개의 이야기");
  const half = [
    { img: "v2/1159v2_01_inner_world.png", tag: "내부 스토리 · 인게임", body: "정지된 시간의 공간에 갇힌 고양이 사수가\n탈출하기 위해 사투를 벌임", c: PURPLE },
    { img: "v2/1159v2_02_outer_story.png", tag: "외부 스토리 · 아웃게임", body: "동화 속 주인공인 고양이 사수가 늙은 시계공의\n죽음을 앞둔 마지막 1분을 반복", c: RED },
  ];
  half.forEach((h, i) => {
    const x = 0.6 + i * 6.2;
    picture(s, h.img, x, 1.7, 5.9, 3.32, h.tag);
    chip(s, h.tag, x + 0.2, 1.88, 2.5, h.c, TXT);
    text(s, h.body, { x, y: 5.2, w: 5.9, h: 0.8, fontSize: 16, lineSpacingMultiple: 1.2 });
  });
  card(s, 0.6, 6.12, 12.1, 0.72, CARD);
  text(s, [
    { text: "결말  ", options: { bold: true, color: GOLD } },
    { text: "죽음을 받아들이고 떠남 · 정해진 운명은 받아들여야 한다는 감동적 스토리 연출" },
  ], { x: 0.9, y: 6.12, w: 11.6, h: 0.72, fontSize: 15, valign: "middle" });
}

// =============== 03 원페이지 기획서 ===============
pres.addSection({ title: SEC[2] });
{
  const s = content(3, "원페이지 기획서");
  picture(s, "v2/1159v2_04_ult.png", 0.6, 1.65, 4.6, 2.59, "궁극기");
  picture(s, "v2/1159v2_03_chapters.png", 0.6, 4.36, 4.6, 2.59, "챕터 흐름");
  const rows = [
    ["게임명", "11:59"],
    ["장르", "3D 탑뷰 빌드형 로그라이크 액션"],
    ["주인공", "정지된 시간에 갇힌 쌍권총 고양이 사수"],
    ["핵심 재미", "드랍 아이템으로 빌드를 짜고, 화려한 액션으로 터뜨림"],
    ["핵심 시스템", "시계 연산 · 궁극기(건카타) · 대쉬 회피"],
    ["빌드", "총탄 · 총 · 연산"],
    ["레벨", "5개 챕터 × (스테이지 4 + 보스 1)"],
    ["성장", "시간의 조각으로 스탯 노드 강화 · 코스튬 · 새 아이템 해금"],
  ];
  rows.forEach((r, i) => {
    const y = 1.65 + i * 0.47;
    card(s, 5.5, y, 7.2, 0.4, i % 2 ? CARD : PURPLE);
    text(s, r[0], { x: 5.7, y, w: 1.6, h: 0.4, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 7.3, y, w: 5.3, h: 0.4, fontSize: 14, valign: "middle" });
  });
  text(s, "레퍼런스 게임", { x: 5.5, y: 5.5, w: 3, h: 0.3, fontSize: 13, bold: true, color: GOLD });
  [["v2/ref/hades_2.jpg", "하데스"], ["v2/ref/skul_2.jpg", "스컬"], ["v2/ref/sephiria_3.jpg", "세피리아"]].forEach((r, i) => {
    const x = 5.5 + i * 2.43;
    picture(s, r[0], x, 5.85, 2.3, 0.8, r[1]);
    caption(s, r[1], x, 6.68, 2.3);
  });
}

// =============== 04 비주얼 가이드 ===============
pres.addSection({ title: SEC[3] });
{
  const s = content(4, "레퍼런스: 하데스");
  picture(s, "v2/ref/hades_5.jpg", 0.6, 1.65, 7.4, 4.16, "하데스 전투");
  picture(s, "v2/ref/hades_2.jpg", 8.2, 1.65, 4.5, 2.53, "하데스 전투 2");
  caption(s, "Hades © Supergiant Games (Steam 스크린샷)", 0.6, 5.88, 7.4);
  card(s, 8.2, 4.33, 4.5, 2.52);
  bullets(s, [
    { t: "내부 비주얼 방향", b: true, c: GOLD },
    "해칭과 명암 대비를 강하게",
    "강렬한 잉크 표현",
    "플랫한 채색 위에 굵은 외곽선",
  ], { x: 8.45, y: 4.5, w: 4.1, h: 2.2, fontSize: 15 });
  text(s, "인 게임 화면은 하데스처럼 굵은 잉크 선과 강한 명암으로 손 그림 느낌을 살림", { x: 0.6, y: 6.25, w: 7.4, h: 0.6, fontSize: 14, color: MUTED });
}
{
  const s = content(4, "구현 방안");
  const steps = [
    ["03_turnaround.png", "① 플랫 시안", "디테일을 뺀 플랫한 원화"],
    ["v2/1159v2_01_inner_world.png", "② 이미지 생성", "시안 기반으로 컨셉 이미지 생성"],
    ["v2/ingame_ult_close.png", "③ 툰 셰이더", "손 그림 효과 · 잉크 외곽선"],
  ];
  steps.forEach((st, i) => {
    const x = 0.6 + i * 4.15;
    picture(s, st[0], x, 1.65, 3.8, 2.14, st[1]);
    text(s, st[1], { x, y: 3.88, w: 3.8, h: 0.38, fontSize: 17, bold: true, color: GOLD });
    text(s, st[2], { x, y: 4.26, w: 3.8, h: 0.35, fontSize: 14 });
    if (i < 2) arrow(s, x + 3.85, 2.72, 0.25, GOLD);
  });
  // 2D / 3D 나눔
  const split = [
    ["2D", "이펙트 · 아이콘", TEAL],
    ["3D 모델링", "오브젝트 · 맵 디자인 · 캐릭터 · 몬스터", PURPLE],
    ["셰이더", "2D / 3D 통일 적용", RED],
  ];
  split.forEach((sp, i) => {
    const x = 0.6 + i * 4.15;
    card(s, x, 4.9, 3.8, 1.25, sp[2]);
    text(s, sp[0], { x: x + 0.25, y: 5.02, w: 3.3, h: 0.4, fontSize: 17, bold: true });
    text(s, sp[1], { x: x + 0.25, y: 5.45, w: 3.4, h: 0.6, fontSize: 14 });
  });
  text(s, "같은 셰이더로 2D 아이콘과 3D 모델의 그림체를 맞춤", { x: 0.6, y: 6.35, w: 12, h: 0.4, fontSize: 14, color: MUTED });
  s.addNotes("③ 이미지는 현재 프로토타입 인게임 화면 (툰 셰이더 + 건카타 총구 폭발 이펙트)");
}

// =============== 05 레벨 디자인 ===============
pres.addSection({ title: SEC[4] });
{
  const s = content(5, "이야기 루프 흐름: 5개 챕터");
  const r = picture(s, "v2/1159v2_03_chapters.png", 0.6, 1.6, 12.1, 4.0, "챕터 흐름", "cover");
  const names = ["1장", "2장", "3장", "4장", "5장"];
  arrow(s, 1.3, 1.96, 9.6, TXT);
  names.forEach((n, i) => chip(s, n, 0.9 + i * 2.42, 1.78, 0.9, i === 0 ? GOLD : i === 4 ? RED : LAV, i === 0 ? BG : TXT));
  card(s, 0.6, 5.78, 5.95, 1.07);
  text(s, [{ text: "시작  ", options: { bold: true, color: GOLD } }, { text: "고양이 사수의 컨셉을 이어 일그러진 서부 마을에서 시작" }], { x: 0.85, y: 5.78, w: 5.5, h: 1.07, fontSize: 14, valign: "middle" });
  card(s, 6.75, 5.78, 5.95, 1.07);
  text(s, [{ text: "진행  ", options: { bold: true, color: GOLD } }, { text: "스토리가 진행될수록 초현실적 · 몽환적 느낌을 강조 → 강해진 적을 시각적으로 전달" }], { x: 7.0, y: 5.78, w: 5.5, h: 1.07, fontSize: 14, valign: "middle" });
}
{
  const s = content(5, "챕터 · 스테이지 구성과 보상");
  // 구조도
  text(s, "1개 챕터", { x: 0.6, y: 1.65, w: 3, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  const seq = ["스테이지", "스테이지", "스테이지", "스테이지", "보스"];
  seq.forEach((t, i) => {
    const x = 0.6 + i * 1.2;
    card(s, x, 2.05, 1.05, 0.75, i === 4 ? RED : PURPLE);
    text(s, t, { x, y: 2.05, w: 1.05, h: 0.75, fontSize: 13, bold: true, align: "center", valign: "middle" });
  });
  text(s, "1개 스테이지", { x: 0.6, y: 3.1, w: 3, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  ["전투방", "전투방", "전투방", "(전투방)"].forEach((t, i) => {
    const x = 0.6 + i * 1.0;
    card(s, x, 3.5, 0.85, 0.65, CARD, { line: i === 3 ? LAV : undefined });
    text(s, t, { x, y: 3.5, w: 0.85, h: 0.65, fontSize: 11, align: "center", valign: "middle", color: i === 3 ? MUTED : TXT });
  });
  arrow(s, 4.55, 3.83, 0.5, GOLD);
  card(s, 5.15, 3.5, 1.4, 0.65, GOLD);
  text(s, "클리어 · 보상", { x: 5.15, y: 3.5, w: 1.4, h: 0.65, fontSize: 12, bold: true, color: BG, align: "center", valign: "middle" });
  bullets(s, [
    "각 챕터는 4개의 스테이지와 1개의 보스 스테이지로 구성",
    "각 스테이지는 3~4개의 전투방",
    "모든 적을 처치하면 스테이지 클리어 후 보상 지급",
  ], { x: 0.6, y: 4.5, w: 6.0, h: 1.6, fontSize: 14 });
  // 보상
  const rew = [
    ["선택형 보상", "3개 중 1개 선택", ["v2/icon_bullet.png", "v2/icon_gun.png", null], ["총탄", "총", "연산"], GOLD],
    ["확정형 보상", "클리어 시 항상 지급", null, ["방어구 강화", "골드", "시간의 조각"], TEAL],
  ];
  rew.forEach((r, i) => {
    const y = 1.65 + i * 2.0;
    card(s, 7.0, y, 5.7, 1.8);
    text(s, r[0], { x: 7.25, y: y + 0.15, w: 2.5, h: 0.4, fontSize: 17, bold: true, color: r[4] });
    text(s, r[1], { x: 9.6, y: y + 0.18, w: 2.9, h: 0.35, fontSize: 12, color: MUTED, align: "right" });
    r[3].forEach((t, k) => {
      const x = 7.25 + k * 1.78;
      if (r[2] && r[2][k] && has(r[2][k])) {
        picture(s, r[2][k], x, y + 0.65, 0.75, 0.75, t, "contain");
        text(s, t, { x: x + 0.8, y: y + 0.65, w: 0.95, h: 0.75, fontSize: 14, bold: true, valign: "middle" });
      } else {
        chip(s, r[2] && k === 2 ? "+ − × ÷" : t, x, y + 0.8, r[2] ? 0.75 : 1.6, r[4], BG, { size: r[2] ? 10 : 13, h: 0.45 });
        if (r[2]) text(s, t, { x: x + 0.8, y: y + 0.65, w: 0.95, h: 0.75, fontSize: 14, bold: true, valign: "middle" });
      }
    });
  });
  card(s, 7.0, 5.65, 5.7, 1.2, CARD, { line: RED, lineW: 1.5 });
  text(s, [{ text: "상인", options: { bold: true, color: RED, breakLine: true } }, { text: "스테이지마다 일정 확률로 등장 · 골드로 총탄 / 총 / 연산 판매" }], { x: 7.25, y: 5.65, w: 5.3, h: 1.2, fontSize: 14, valign: "middle" });
}

// =============== 06 핵심 시스템 ===============
pres.addSection({ title: SEC[5] });
{
  const s = content(6, "시계 연산");
  // 시계 판: 12칸 실린더
  const cx = 3.2, cy = 4.0, R = 1.72;
  s.addShape(pres.shapes.OVAL, { x: cx - 2.2, y: cy - 2.2, w: 4.4, h: 4.4, fill: { color: CARD }, line: { color: GOLD, width: 3 }, objectName: "clock_face" });
  const on = { 2: true, 3: true, 6: true, 9: true, 12: true };
  for (let k = 1; k <= 12; k++) {
    const a = (k * 30 - 90) * Math.PI / 180;
    const x = cx + Math.cos(a) * R - 0.29, y = cy + Math.sin(a) * R - 0.29;
    s.addShape(pres.shapes.OVAL, { x, y, w: 0.58, h: 0.58, fill: { color: on[k] ? GOLD : PURPLE }, line: { color: on[k] ? GOLD : LAV, width: 1 }, objectName: nm("chamber") });
    text(s, String(k), { x, y, w: 0.58, h: 0.58, fontSize: 13, bold: true, align: "center", valign: "middle", color: on[k] ? BG : TXT });
  }
  s.addShape(pres.shapes.LINE, { x: cx, y: cy - 1.1, w: 0, h: 1.1, line: { color: TXT, width: 4 }, objectName: "hand_min" });
  s.addShape(pres.shapes.LINE, { x: cx - 0.7, y: cy - 0.4, w: 0.7, h: 0.4, line: { color: TXT, width: 5 }, objectName: "hand_hr" });
  s.addShape(pres.shapes.OVAL, { x: cx - 0.12, y: cy - 0.12, w: 0.24, h: 0.24, fill: { color: GOLD }, line: { color: GOLD }, objectName: "hub" });
  chip(s, "활성화 탄", 1.65, 6.45, 1.35, GOLD, BG, { size: 11, h: 0.32 });
  chip(s, "비활성화 탄", 3.1, 6.45, 1.45, PURPLE, TXT, { size: 11, h: 0.32 });
  picture(s, "ui/hud_unified_toon_rgba.png", 6.55, 1.6, 3.2, 1.62, "탄환 시계 HUD", "contain");
  caption(s, "인게임 HUD: 양손 실린더 + 시계", 9.85, 2.3, 2.9);
  bullets(s, [
    { t: "각 총탄은 시계의 숫자를 의미하는 실린더에 장착", b: false },
    "장착된 총탄은 총탄 고유의 요구조건을 충족하는 시각의 실린더에 놓여야만 고유 효과와 특성 활성화",
    "연산을 통해 각 총탄의 판정 시각을 조정 가능",
  ], { x: 6.55, y: 3.5, w: 6.15, h: 2.3, fontSize: 15 });
  card(s, 6.55, 5.9, 6.15, 0.95);
  text(s, [{ text: "예  ", options: { bold: true, color: GOLD } }, { text: "‘3의 배수 시각’ 조건 탄 → 3 · 6 · 9 · 12시에 놓이면 활성화" }], { x: 6.8, y: 5.9, w: 5.8, h: 0.95, fontSize: 14, valign: "middle" });
}
{
  const s = content(6, "궁극기: 불릿 파티");
  picture(s, "v2/1159v2_04_ult.png", 0.6, 1.6, 7.2, 4.05, "궁극기 컨셉");
  card(s, 0.6, 5.85, 7.2, 1.0, PURPLE);
  text(s, "화려한 액션으로 시각적 도파민 제공 · 사용 타이밍 조절 → 전략적 재미 전달", { x: 0.85, y: 5.85, w: 6.8, h: 1.0, fontSize: 14, bold: true, valign: "middle" });
  // 열기 게이지
  text(s, "열기 게이지", { x: 8.1, y: 1.65, w: 3, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  for (let i = 0; i < 3; i++) {
    s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 8.1 + i * 1.12, y: 2.08, w: 1.0, h: 0.42, rectRadius: 0.08, fill: { color: i < 3 ? RED : CARD }, line: { color: GOLD, width: 1.5 }, objectName: nm("heat") });
  }
  chip(s, "Q", 11.55, 2.03, 0.55, GOLD, BG, { size: 16, h: 0.52 });
  text(s, "발동", { x: 12.15, y: 2.03, w: 0.6, h: 0.52, fontSize: 13, bold: true, valign: "middle" });
  bullets(s, [
    "각 공격을 명중시키면 열기 게이지 획득",
    "열기 게이지 3칸 이상이면 Q 키로 발동 가능",
    "넓은 범위의 적을 모든 총탄을 소비해 동시에 공격",
    "궁극기 사용 시 무적 판정",
    "활성화된 총탄 · 특성 효과 모두 적용",
  ], { x: 8.1, y: 2.7, w: 4.6, h: 2.0, fontSize: 14, paraSpaceAfter: 4 });
  picture(s, "v2/ingame_ult.png", 8.1, 4.75, 4.6, 2.1, "프로토타입 인게임");
  chip(s, "프로토타입 인게임", 8.25, 4.88, 1.9, BG, GOLD, { size: 11, h: 0.32 });
}
{
  const s = content(6, "대쉬 회피 · 닷지 액션");
  picture(s, "v2/1159v2_05_dodge.png", 0.6, 1.6, 7.2, 4.05, "닷지 컨셉");
  picture(s, "v2/ref/zzz_dodge.jpg", 0.6, 5.8, 1.85, 1.04, "젠레스 존 제로");
  text(s, "레퍼런스: 젠레스 존 제로 닷지 액션\n(출처: ONE Esports)", { x: 2.6, y: 5.8, w: 5.2, h: 1.04, fontSize: 12, color: MUTED, valign: "middle" });
  text(s, "대쉬 횟수", { x: 8.1, y: 1.65, w: 3, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  for (let i = 0; i < 3; i++) text(s, "»", { x: 8.1 + i * 0.9, y: 1.95, w: 0.8, h: 0.7, fontSize: 36, bold: true, color: TEAL });
  text(s, "최대 3회 저장", { x: 10.9, y: 2.1, w: 1.8, h: 0.4, fontSize: 13, color: MUTED, valign: "middle" });
  bullets(s, [
    "대쉬 횟수는 시간에 따라 자동 충전",
    "대쉬 사용 시 무적 판정",
    "적의 공격을 대쉬로 직접 회피하면 닷지 액션 발동",
  ], { x: 8.1, y: 2.9, w: 4.6, h: 1.7, fontSize: 15 });
  card(s, 8.1, 4.7, 4.6, 2.15, PURPLE);
  text(s, "닷지 액션", { x: 8.35, y: 4.85, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
  bullets(s, ["짧은 시간 치명타 보정 획득", "주변 시간이 느려지는 연출로 피드백 전달"], { x: 8.35, y: 5.35, w: 4.2, h: 1.4, fontSize: 14 });
}

// =============== 07 빌드 시스템 ===============
pres.addSection({ title: SEC[6] });
{
  const s = content(7, "캐릭터 속성과 보상 분배");
  const stats = ["방어력", "체력", "치명타 확률", "치명타 피해", "데미지", "이동속도", "열기"];
  text(s, "캐릭터가 가지는 속성", { x: 0.6, y: 1.65, w: 5, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  stats.forEach((t, i) => chip(s, t, 0.6 + i * 1.74, 2.1, 1.6, CARD, TXT, { size: 13, h: 0.45 }));
  text(s, "열기 = 궁극기 게이지", { x: 0.6, y: 2.65, w: 5, h: 0.3, fontSize: 11, color: MUTED });
  const cols = [
    ["총탄", "데미지 · 다양한 고유 효과", "icons/sheet_bullets_legendary.png", GOLD],
    ["방어구 강화", "방어력 · 체력", null, TEAL],
    ["총", "치명타 확률 · 치명타 피해", "icons/sheet_guns.png", RED],
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 4.15;
    card(s, x, 3.2, 3.8, 3.65);
    if (c[2]) picture(s, c[2], x + 0.2, 3.35, 3.4, 1.9, c[0]);
    else {
      s.addShape(pres.shapes.OVAL, { x: x + 1.15, y: 3.45, w: 1.5, h: 1.5, fill: { color: TEAL }, line: { color: TEAL }, objectName: nm("armor") });
      text(s, "+", { x: x + 1.15, y: 3.45, w: 1.5, h: 1.5, fontSize: 54, bold: true, color: BG, align: "center", valign: "middle" });
    }
    text(s, c[0], { x: x + 0.25, y: 5.45, w: 3.3, h: 0.45, fontSize: 19, bold: true, color: c[3] });
    text(s, c[1], { x: x + 0.25, y: 5.95, w: 3.4, h: 0.7, fontSize: 15 });
  });
}
{
  const s = content(7, "총탄");
  picture(s, "icons/sheet_bullets_legendary.png", 0.6, 1.6, 6.6, 3.71, "총탄 아이콘");
  caption(s, "총탄 아이콘 시안 (전설 등급)", 0.6, 5.38, 6.6);
  bullets(s, [
    "활성화 조건을 충족하는 시각 실린더에 장착되어야만 고유 효과와 특성 활성화",
    "등급에 따라 강력한 고유 효과 존재",
    "0~2개의 연산 슬롯으로 판정 시각 조정 가능",
    "2개의 특성 존재",
  ], { x: 7.5, y: 1.7, w: 5.2, h: 3.3, fontSize: 15 });
  card(s, 7.5, 5.15, 5.2, 1.7, PURPLE);
  text(s, [{ text: "핵심 목표\n", options: { bold: true, color: GOLD, breakLine: true } }, { text: "특성과 판정 시각의 전략적 운용으로 빌드 성장을 자극" }], { x: 7.75, y: 5.15, w: 4.8, h: 1.7, fontSize: 15, valign: "middle", lineSpacingMultiple: 1.2 });
  ["일반", "희귀", "영웅", "전설"].forEach((g, i) => chip(s, g, 0.6 + i * 1.2, 5.85, 1.05, [LAV, TEAL, RED, GOLD][i], i === 3 ? BG : TXT, { size: 12 }));
  text(s, "등급", { x: 5.5, y: 5.85, w: 1.7, h: 0.36, fontSize: 12, color: MUTED, valign: "middle" });
}
{
  const s = content(7, "특성");
  // 규칙
  bullets(s, [
    "총탄마다 특성 2개 존재",
    "활성화 된 총탄의 특성만 반영",
    "동일한 특성이 2, 4, 6개 모이면 단계별 효과 발동",
    "켜진 특성 효과는 모든 사격에 적용",
  ], { x: 0.6, y: 1.65, w: 5.6, h: 1.9, fontSize: 15 });
  // 단계
  [["2개", "1단계"], ["4개", "2단계"], ["6개", "3단계"]].forEach((t, i) => {
    const x = 0.6 + i * 1.95;
    card(s, x, 3.65, 1.75, 0.95, i === 2 ? GOLD : PURPLE);
    text(s, t[0], { x, y: 3.7, w: 1.75, h: 0.45, fontSize: 20, bold: true, align: "center", color: i === 2 ? BG : TXT });
    text(s, t[1], { x, y: 4.13, w: 1.75, h: 0.35, fontSize: 13, align: "center", color: i === 2 ? BG : MUTED });
    if (i < 2) arrow(s, x + 1.77, 4.12, 0.16, GOLD);
  });
  // 인게임 HUD
  picture(s, "v2/ingame_traits.png", 0.6, 4.8, 3.29, 2.05, "활성 특성 HUD", "contain", { alignLeft: true });
  text(s, "인게임: 활성 특성 목록\n마우스를 올리면 단계별 효과", { x: 4.0, y: 5.6, w: 2.3, h: 0.9, fontSize: 12, color: MUTED });
  // 특성 9종
  const traits = [
    ["회상", "BF8CFF", "고양이 실루엣이 나타나 함께 사격"],
    ["추억", "FF8CCC", "같은 적을 맞힐수록 표식 · 피해 증가"],
    ["망각", "8CD9FF", "적을 시간 속에 가둬 정지 · 받는 피해 증가"],
    ["되감기", "66F2D9", "피격 직전으로 되감아 회피"],
    ["종말", "FF4D4D", "빈사 적 처형 · 보스 추가 피해"],
    ["지연", "FF9940", "일정 시간 후 추가 데미지"],
    ["예지", "A680FF", "대쉬 회피 판정 증가"],
    ["영원", "FFD95A", "정지 · 둔화 지속 증가"],
    ["미련", "A6B8E6", "비활성화 탄에 보너스 효과 추가"],
  ];
  traits.forEach((t, i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = 6.6 + col * 2.05, y = 1.65 + row * 1.75;
    card(s, x, y, 1.9, 1.6);
    const iconFile = `v2/traits/trait_${3001 + i}.png`;
    if (has(iconFile)) picture(s, iconFile, x + 0.14, y + 0.12, 0.62, 0.62, t[0], "contain");
    else s.addShape(pres.shapes.DIAMOND, { x: x + 0.2, y: y + 0.22, w: 0.26, h: 0.26, fill: { color: t[1] }, line: { color: t[1] }, objectName: nm("traitdot") });
    text(s, t[0], { x: x + 0.84, y: y + 0.2, w: 1.0, h: 0.45, fontSize: 17, bold: true, color: t[1] });
    text(s, t[2], { x: x + 0.16, y: y + 0.84, w: 1.62, h: 0.7, fontSize: 12, color: TXT, lineSpacingMultiple: 1.1 });
  });
  s.addNotes("특성 예: 회상 2개 = 쏠 때 10% 확률로 고양이 실루엣이 같은 방향으로 1발 · 4개 = 확률 20% · 6개 = 실루엣이 3초 동안 남아 사격을 따라 함");
}
{
  const s = content(7, "총");
  picture(s, "icons/sheet_guns.png", 0.6, 1.6, 6.6, 3.71, "총 아이콘");
  caption(s, "총 아이콘 시안", 0.6, 5.38, 6.6);
  // 양손
  card(s, 0.6, 5.75, 3.2, 1.1, RED);
  text(s, [{ text: "오른손\n", options: { bold: true, breakLine: true } }, { text: "홀수 칸 담당" }], { x: 0.85, y: 5.75, w: 2.8, h: 1.1, fontSize: 14, valign: "middle" });
  card(s, 4.0, 5.75, 3.2, 1.1, PURPLE);
  text(s, [{ text: "왼손\n", options: { bold: true, breakLine: true } }, { text: "짝수 칸 담당" }], { x: 4.25, y: 5.75, w: 2.8, h: 1.1, fontSize: 14, valign: "middle" });
  bullets(s, [
    "왼손과 오른손에 장착 가능",
    "2개 넘게 소지 불가 (예비 인벤토리 없음) → 새 총을 얻으면 기존 총을 버려야 장착 가능",
    "왼손 / 오른손 장착에 따라 효과가 달라짐 → 부가적인 전략 요소",
    "총마다 고유 효과와 등급 존재",
  ], { x: 7.5, y: 1.7, w: 5.2, h: 3.5, fontSize: 15 });
  card(s, 7.5, 5.3, 5.2, 1.55, CARD, { line: GOLD, lineW: 1.5 });
  text(s, [{ text: "액티브 스킬\n", options: { bold: true, color: GOLD, breakLine: true } }, { text: "총이 액티브 스킬을 활성화 → 캐릭터는 최대 2개의 추가 기술 보유" }], { x: 7.75, y: 5.3, w: 4.8, h: 1.55, fontSize: 15, valign: "middle", lineSpacingMultiple: 1.15 });
}
{
  const s = content(7, "연산");
  bullets(s, [
    "총탄의 활성화 조건 조정을 더 유연하게, 부가적인 전략 요소로 작동",
    "총탄의 연산 슬롯에 장착해 판정 시각 조정 (발사 순서엔 영향 없음)",
    "1~3의 숫자와 사칙연산 → 계산 체감이 너무 복잡하지 않게",
  ], { x: 0.6, y: 1.7, w: 6.0, h: 2.2, fontSize: 15 });
  // 연산 종류
  text(s, "연산 종류", { x: 0.6, y: 3.65, w: 3, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  ["+1~3", "−1~3", "×2", "×3", "÷2", "÷3"].forEach((t, i) => chip(s, t, 0.6 + i * 1.0, 4.07, 0.88, TEAL, BG, { size: 14, h: 0.5 }));
  bullets(s, [
    "×0 · ÷0 없음",
    "따로 드랍되는 아이템, 교체는 전투가 없을 때만",
    "판정 시각은 12 초과 · 0 이하도 그대로 유지",
  ], { x: 0.6, y: 4.85, w: 6.0, h: 1.7, fontSize: 14 });
  // 예시: 순서가 결과를 바꿈
  card(s, 7.0, 1.65, 5.7, 5.2);
  text(s, "세부 연산 규칙", { x: 7.3, y: 1.8, w: 5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
  text(s, "사칙연산의 기본 우선순위를 따르지 않고, 슬롯 순서대로 적용", { x: 7.3, y: 2.25, w: 5.2, h: 0.6, fontSize: 14 });
  const ex = [
    ["4시", ["+2", "×2"], "12시", GOLD],
    ["4시", ["×2", "+2"], "10시", LAV],
  ];
  ex.forEach((e, i) => {
    const y = 3.1 + i * 1.65;
    text(s, i ? "순서를 바꾸면" : "놓인 시각 4시", { x: 7.3, y, w: 5, h: 0.3, fontSize: 12, color: MUTED });
    chip(s, e[0], 7.3, y + 0.4, 0.95, CARD, TXT, { h: 0.6, size: 15 });
    e[1].forEach((op, k) => {
      arrow(s, 8.3 + k * 1.3, y + 0.7, 0.25);
      chip(s, op, 8.6 + k * 1.3, y + 0.4, 0.95, TEAL, BG, { h: 0.6, size: 15 });
    });
    arrow(s, 10.9, y + 0.7, 0.25, GOLD);
    chip(s, e[2], 11.2, y + 0.4, 1.2, e[3], e[3] === GOLD ? BG : TXT, { h: 0.6, size: 16 });
  });
  text(s, "같은 연산이라도 끼우는 순서에 따라 다른 판정 시각", { x: 7.3, y: 6.3, w: 5.2, h: 0.4, fontSize: 13, color: MUTED });
}

// =============== 08 아웃게임 성장 ===============
pres.addSection({ title: SEC[7] });
{
  const s = content(8, "메인 로비: 기억의 지속 속 서부 마을");
  const lobby = has("v2/1159v2_06_lobby.png") ? "v2/1159v2_06_lobby.png" : "05_zone1_western.png";
  picture(s, lobby, 0.6, 1.6, 8.0, 4.5, "메인 로비 컨셉");
  picture(s, "v2/ref/dali_persistence.jpg", 8.9, 1.6, 3.8, 2.75, "기억의 지속", "contain", { alignLeft: true, alignTop: true });
  caption(s, "모티브: 살바도르 달리 「기억의 지속」(1931)", 8.9, 4.4, 3.8);
  text(s, "메인 로비를 달리의 「기억의 지속」을 모티브로 한 서부 마을로 구성", { x: 8.9, y: 4.85, w: 3.8, h: 1.2, fontSize: 15, lineSpacingMultiple: 1.15 });
  text(s, "게임을 진행하며 스테이지를 클리어할 때마다 성장 요소가 조금씩 해금", { x: 0.6, y: 6.3, w: 12, h: 0.45, fontSize: 15, bold: true, color: GOLD });
}
{
  const s = content(8, "해금되는 성장 요소");
  const items = [
    ["캐릭터 스탯 성장", "스탯 성장 · 고유 효과\n시간의 조각으로 성장 노드 강화", GOLD],
    ["새로운 아이템", "새 총탄 · 새 총 해금", TEAL],
    ["코스튬", "캐릭터처럼 새로운 궁극기나\n기술 사용", RED],
  ];
  items.forEach((it, i) => {
    const y = 1.65 + i * 1.3;
    card(s, 0.6, y, 6.6, 1.12);
    s.addShape(pres.shapes.OVAL, { x: 0.82, y: y + 0.21, w: 0.7, h: 0.7, fill: { color: it[2] }, line: { color: it[2] }, objectName: nm("dot") });
    text(s, String(i + 1), { x: 0.82, y: y + 0.21, w: 0.7, h: 0.7, fontSize: 18, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, it[0], { x: 1.75, y, w: 2.2, h: 1.12, fontSize: 16, bold: true, valign: "middle" });
    text(s, it[1], { x: 3.95, y, w: 3.15, h: 1.12, fontSize: 13, valign: "middle", lineSpacingMultiple: 1.1 });
  });
  card(s, 0.6, 5.62, 6.6, 1.23, PURPLE);
  text(s, [{ text: "시간의 조각  ", options: { bold: true, color: GOLD } }, { text: "인 게임에서 획득 → 스탯 성장 노드 강화" }], { x: 0.85, y: 5.62, w: 6.2, h: 1.23, fontSize: 14, valign: "middle" });
  picture(s, "v2/ref/sephiria_costume.png", 7.6, 1.65, 2.5, 3.85, "세피리아 코스튬", "contain", { alignTop: true, alignLeft: true });
  text(s, "코스튬 레퍼런스", { x: 10.35, y: 1.7, w: 2.4, h: 0.35, fontSize: 15, bold: true, color: GOLD });
  text(s, "세피리아 코스튬 목록", { x: 10.35, y: 2.1, w: 2.4, h: 0.35, fontSize: 14 });
  caption(s, "출처: 커뮤니티 캡처", 10.35, 2.45, 2.4);
  text(s, "코스튬마다 외형과 함께 새로운 궁극기 · 기술 사용", { x: 7.6, y: 5.62, w: 5.1, h: 1.23, fontSize: 14, valign: "middle" });
}

(async () => {
  await pres.writeFile({ fileName: OUT });
  if (process.env.PPTX_SKILL) {
    const { applyTheme } = require(process.env.PPTX_SKILL + "/scripts/apply_theme.js");
    await applyTheme(OUT, THEME);
  }
  console.log("saved", OUT);
})();
