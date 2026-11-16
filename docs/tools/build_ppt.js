// Cat_Tang 컨셉 기획서 PPT 생성 (확정 내용만)
// 실행: NODE_PATH=<pptxgenjs가 있는 node_modules> PPTX_SKILL=<pptx 스킬 폴더> node build_ppt.js
//       OUT_DIR을 주면 그 폴더에 쓴다 (기본: docs/)
const pptxgen = require("pptxgenjs");
const path = require("path");
const fs = require("fs");
const { applyTheme } = require(process.env.PPTX_SKILL + "/scripts/apply_theme.js");

const DOCS = path.resolve(__dirname, "..");
const IMG = path.join(DOCS, "images");
const OUT = path.join(process.env.OUT_DIR || DOCS, "Cat_Tang_컨셉_기획서.pptx");
const AUTHOR = "정승운";
const img = (n) => path.join(IMG, n);
const has = (n) => fs.existsSync(img(n));
const pngSize = (f) => { const b = fs.readFileSync(f); return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) }; };

const THEME = {
  name: "Cat_Tang Clock",
  headFontFace: "Noto Sans KR",
  bodyFontFace: "Noto Sans KR",
  colors: {
    dk1: "17131F", lt1: "F3EDE2", dk2: "241D30", lt2: "A99FB5",
    accent1: "E0A63A", accent2: "C2343B", accent3: "3B2D5C",
    accent4: "2C2440", accent5: "5FA8B8", accent6: "8C7AA8",
    hlink: "E0A63A", folHlink: "8C7AA8",
  },
};
const HEX = THEME.colors;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.333 x 7.5
pres.title = "11:59 컨셉 기획서";
pres.author = AUTHOR;
pres.company = AUTHOR;
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
const C = pres.SchemeColor;

const BG = C.text1;
const CARD = C.accent4;
const TXT = C.background1;
const MUTED = C.background2;
const GOLD = C.accent1;
const RED = C.accent2;
const TEAL = C.accent5;
const PURPLE = C.accent3;

// ---------- 레이아웃 ----------
pres.defineSlideMaster({
  title: "CONTENT",
  background: { color: BG },
  objects: [
    { text: { text: `11:59 · 컨셉 기획서 · ${AUTHOR}`, options: { x: 0.6, y: 7.0, w: 6, h: 0.3, fontSize: 10, color: MUTED, margin: 0 } } },
    {
      placeholder: {
        options: { name: "title", type: "title", x: 0.6, y: 0.45, w: 12.1, h: 0.8, fontSize: 30, bold: true, color: TXT, valign: "middle", align: "left", margin: 0 },
        text: "제목",
      },
    },
  ],
  slideNumber: { x: 12.2, y: 7.0, w: 0.5, h: 0.3, fontSize: 10, color: MUTED, align: "right" },
});
pres.defineSlideMaster({ title: "COVER", background: { color: BG }, objects: [] });

let shapeId = 0;
const nm = (s) => `${s}_${++shapeId}`;

function text(slide, t, o) {
  slide.addText(t, { isTextBox: true, fontFace: THEME.bodyFontFace, lang: "ko-KR", color: TXT, fontSize: 15, margin: 0, valign: "top", objectName: nm("text"), ...o });
}
function card(slide, x, y, w, h, fill = CARD) {
  slide.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.12, fill: { color: fill }, line: { color: fill }, objectName: nm("card") });
}
function bullets(slide, items, o) {
  const runs = items.map((it, i) => {
    const isObj = typeof it === "object";
    const t = isObj ? it.t : it;
    const head = isObj && it.b;
    return {
      text: t,
      options: {
        bullet: head ? false : { indent: 16 },
        breakLine: i < items.length - 1,
        bold: head,
        color: isObj && it.c ? it.c : TXT,
        paraSpaceBefore: head && i > 0 ? 10 : 0,
      },
    };
  });
  text(slide, runs, { fontSize: 15, paraSpaceAfter: 8, ...o });
}
// 원본 비율 유지. cover = 상자를 꽉 채우고 넘치는 쪽을 잘라 냄, contain = 잘리지 않게 상자 안에 맞춤
function picture(slide, file, x, y, w, h, label, fit = "cover") {
  if (!has(file)) {
    slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: CARD }, line: { color: CARD } });
    text(slide, label, { x, y: y + h / 2 - 0.2, w, h: 0.4, align: "center", color: MUTED });
    return;
  }
  const { w: iw, h: ih } = pngSize(img(file));
  if (fit === "contain") {
    const k = Math.min(w / iw, h / ih), dw = iw * k, dh = ih * k;
    slide.addImage({ path: img(file), x: x + (w - dw) / 2, y: y + (h - dh) / 2, w: dw, h: dh, objectName: nm("img"), altText: label });
  } else {
    const k = Math.max(w / iw, h / ih);
    slide.addImage({ path: img(file), x, y, w: iw * k, h: ih * k, sizing: { type: "cover", w, h }, objectName: nm("img"), altText: label });
  }
}
function caption(slide, t, x, y, w) {
  text(slide, t, { x, y, w, h: 0.3, fontSize: 10, color: MUTED });
}
function labelCard(slide, x, y, w, h, label, body, o = {}) {
  card(slide, x, y, w, h, o.fill);
  text(slide, label, { x: x + 0.25, y: y + 0.18, w: o.labelW || 1.6, h: 0.4, fontSize: o.labelSize || 15, bold: true, color: o.labelColor || GOLD });
  const by = o.bodyY || 0.18;
  text(slide, body, { x: x + (o.bodyX || 1.9), y: y + by, w: w - (o.bodyX || 1.9) - 0.2, h: h - by - 0.12, fontSize: o.bodySize || 14, lineSpacingMultiple: 1.15 });
}
function flow(s, x0, y, w, h, gap, steps, o = {}) {
  steps.forEach((st, i) => {
    const x = x0 + i * (w + gap);
    const last = i === steps.length - 1;
    card(s, x, y, w, h, last ? GOLD : o.fill || CARD);
    text(s, st[0], { x, y: y + 0.12, w, h: 0.3, fontSize: o.smallSize || 11, align: "center", color: last ? BG : MUTED });
    text(s, st[1], { x, y: y + h * 0.42, w, h: h * 0.48, fontSize: o.bigSize || 22, bold: true, align: "center", valign: "middle", color: last ? BG : TXT });
    if (!last) {
      s.addShape(pres.shapes.LINE, { x: x + w + 0.05, y: y + h / 2, w: gap - 0.1, h: 0, line: { color: MUTED, width: 2, endArrowType: "triangle" }, objectName: nm("arrow") });
    }
  });
}

// ---------- 1. 표지 ----------
pres.addSection({ title: "표지" });
{
  const s = pres.addSlide({ masterName: "COVER", sectionTitle: "표지" });
  picture(s, "01_keyart.png", 0, 0, 13.333, 7.5, "키 아트");
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 7.75, y: 4.2, w: 5.1, h: 2.85, rectRadius: 0.12, fill: { color: BG, transparency: 20 }, line: { color: BG, transparency: 100 }, objectName: "cover_panel" });
  text(s, "11:59", { x: 8.1, y: 4.4, w: 4.6, h: 0.95, fontSize: 50, bold: true, color: GOLD, fontFace: THEME.headFontFace });
  text(s, "컨셉 기획서", { x: 8.1, y: 5.35, w: 4.6, h: 0.5, fontSize: 24, bold: true });
  text(s, "3D 탑뷰 빌드형 로그라이크 액션", { x: 8.1, y: 5.9, w: 4.6, h: 0.4, fontSize: 16, color: MUTED });
  text(s, `작성 ${AUTHOR} · 2026.11`, { x: 8.1, y: 6.35, w: 4.6, h: 0.3, fontSize: 13, color: TXT });
  text(s, "이미지는 모두 컨셉 시안", { x: 8.1, y: 6.68, w: 4.6, h: 0.3, fontSize: 11, color: MUTED });
  s.addNotes("제목 뜻: 자정 1분 전. 주인공의 회중시계가 멈춘 시각이자, 죽음을 앞둔 마지막 순간");
}

// ---------- 2. 기획 의도 ----------
pres.addSection({ title: "개요" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "개요" });
  s.addText("기획 의도", { placeholder: "title" });
  text(s, "차곡차곡 쌓은 빌드를 건카타 한 번에 터뜨리는 쾌감", { x: 0.6, y: 1.35, w: 12.1, h: 0.6, fontSize: 26, bold: true, color: GOLD });
  text(s, "플레이어에게 주고 싶은 두 가지", { x: 0.6, y: 1.98, w: 12.1, h: 0.35, fontSize: 14, color: MUTED });

  // 01 빌드 → 건카타
  card(s, 0.6, 2.55, 7.35, 4.2);
  text(s, "01", { x: 0.9, y: 2.8, w: 0.5, h: 0.45, fontSize: 16, bold: true, color: GOLD, valign: "middle" });
  text(s, "쌓는 재미, 터뜨리는 쾌감", { x: 1.4, y: 2.8, w: 6.2, h: 0.45, fontSize: 20, bold: true, valign: "middle" });
  text(s, "판을 돌며 탄과 연산을 모으고, 12칸 시계 위 어디에 놓을지 고민하는 재미. 조건이 맞아 탄이 하나씩 활성화될수록 점점 나만의 무기로 완성되는 총. 그렇게 채운 12발을 건카타 궁극기로 한꺼번에 쏟아붓는 순간, 그동안 쌓은 고민이 화면 가득 폭발.",
    { x: 0.9, y: 3.4, w: 6.75, h: 1.45, fontSize: 14, lineSpacingMultiple: 1.3 });
  flow(s, 0.9, 5.0, 1.5, 0.95, 0.25, [["모으기", "탄 · 연산"], ["놓기", "12칸 배치"], ["활성화", "조건 맞추기"], ["터뜨리기", "불릿 파티"]], { bigSize: 15, smallSize: 11 });
  text(s, "탄은 장전 순서대로 발사. 빌드마다 다른 궁극기 연출", { x: 0.9, y: 6.12, w: 6.75, h: 0.35, fontSize: 12, color: MUTED });

  // 02 이야기
  card(s, 8.2, 2.55, 4.53, 4.2, PURPLE);
  text(s, "02", { x: 8.5, y: 2.8, w: 0.5, h: 0.45, fontSize: 16, bold: true, color: GOLD, valign: "middle" });
  text(s, "궁금해지는 이야기", { x: 9.0, y: 2.8, w: 3.5, h: 0.45, fontSize: 20, bold: true, valign: "middle" });
  picture(s, "04_world.png", 8.5, 3.4, 3.93, 3.93 * 9 / 16, "세계관 시안");
  text(s, "자신이 누군가의 상상이라는 걸 모르는 고양이. 시간의 조각을 모을수록 드러나는 멈춘 시간 너머의 진실.",
    { x: 8.5, y: 5.72, w: 3.93, h: 0.9, fontSize: 13, lineSpacingMultiple: 1.2 });
  s.addNotes("핵심: 빌드를 쌓아 가는 재미 → 건카타로 한 번에 터뜨리는 액션의 쾌감. 그리고 다음 판을 하고 싶게 만드는 이야기.");
}

// ---------- 3. 원페이지 기획서 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "개요" });
  s.addText("원페이지 기획서", { placeholder: "title" });
  text(s, [
    { text: "병상의 노인 시계공이 어릴 적 동경한 서부극 영웅, " },
    { text: "쌍권총 고양이", options: { bold: true, color: GOLD } },
    { text: ". 멈춘 시간에서 벗어나려 싸우다 끝내 노인의 죽음을 받아들이는 " },
    { text: "3D 탑뷰 빌드형 로그라이크 액션", options: { bold: true, color: GOLD } },
    { text: "." },
  ], { x: 0.6, y: 1.35, w: 12.1, h: 0.8, fontSize: 17, lineSpacingMultiple: 1.2 });

  picture(s, "02_ingame.png", 0.6, 2.35, 5.6, 5.6 * 9 / 16, "인게임 목업");
  card(s, 0.6, 5.65, 5.6, 1.1);
  const info = [["장르", "빌드형 로그라이크"], ["시점", "3D 탑뷰"], ["아트", "하데스풍 카툰 3D"], ["엔진", "Unity 6 (URP)"], ["레퍼런스", "하데스 · 세피리아 · 스컬"]];
  info.forEach((r, i) => {
    const x = 0.85 + (i % 2) * 2.75, y = 5.74 + Math.floor(i / 2) * 0.32;
    text(s, r[0], { x, y, w: 0.85, h: 0.3, fontSize: 12, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: x + 0.9, y, w: i === 4 ? 4.2 : 1.8, h: 0.3, fontSize: 12, valign: "middle" });
  });

  const cells = [
    ["핵심 재미", "탄을 모아 빌드를 쌓고, 건카타 궁극기로 한 번에 폭발. 빌드마다 다른 궁극기 연출."],
    ["탄환 시계", "양손 리볼버의 12칸이 곧 시계이자 인벤토리. 놓인 시각과 연산으로 탄 활성화."],
    ["이야기", "노인의 상상 속 영웅인 고양이. 시간의 조각으로 드러나는 노인의 삶과, 끝내 그 죽음을 받아들이는 결말."],
    ["레벨 디자인", "챕터 5개, 챕터마다 스테이지 4개와 보스. 판이 끝나면 '기억'으로 영구 강화."],
  ];
  const cw = 2.99, ch = 2.08;
  cells.forEach((c, i) => {
    const x = 6.5 + (i % 2) * (cw + 0.25), y = 2.35 + Math.floor(i / 2) * (ch + 0.24);
    card(s, x, y, cw, ch, i === 0 ? PURPLE : CARD);
    text(s, c[0], { x: x + 0.25, y: y + 0.22, w: cw - 0.5, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    text(s, c[1], { x: x + 0.25, y: y + 0.72, w: cw - 0.5, h: ch - 0.85, fontSize: 13, lineSpacingMultiple: 1.25 });
  });
}

// ---------- 4. 주인공 ----------
pres.addSection({ title: "캐릭터 · 아트" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "캐릭터 · 아트" });
  s.addText("주인공: 쌍권총 고양이", { placeholder: "title" });
  card(s, 0.6, 1.45, 7.6, 4.36, "F3EDE2");
  picture(s, "03_turnaround.png", 0.7, 1.55, 7.4, 4.16, "턴어라운드", "contain");
  caption(s, "3D 모델링용 턴어라운드 시안 (앞 · 옆 · 뒤)", 0.6, 5.92, 7.6);
  bullets(s, [
    { t: "캐릭터", b: true, c: GOLD },
    "쌍권총을 든 서부극 영웅 고양이",
    "챙 넓은 모자 · 빨간 스카프 · 긴 코트",
    { t: "디자인 포인트", b: true, c: GOLD },
    "11시 59분에 멈춘 회중시계",
    "실린더를 시계판처럼 꾸민 금장 리볼버",
    "모자 띠 총알 6발 (실린더 탄 수와 동일)",
    { t: "꼭 지킬 것", b: true, c: GOLD },
    "건카타 궁극기 '불릿 파티'",
  ], { x: 8.6, y: 1.5, w: 4.13, h: 5.2, fontSize: 14, paraSpaceAfter: 6 });
}

// ---------- 5. 아트 방향 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "캐릭터 · 아트" });
  s.addText("아트 방향", { placeholder: "title" });
  picture(s, "01_keyart.png", 0.6, 1.45, 6.4, 3.6, "키 아트");
  caption(s, "키 아트 시안", 0.6, 5.1, 6.4);
  labelCard(s, 0.6, 5.5, 6.4, 1.25, "구현 방법", "플랫한 그림체로 AI 생성 느낌을 최대한 절제\n툰 셰이더로 하데스풍의 개성 있는 그림체 구현", { labelW: 1.3, bodyX: 1.6, bodySize: 14 });
  const items = [
    ["그림체", "하데스풍 카툰 3D"],
    ["비율", "약 3등신 데포르메\n큰 머리와 넓은 모자 챙\n큰 총과 부츠, 굵은 줄무늬 꼬리"],
    ["공간", "평범한 서부 마을 같지만\n처음부터 달리 그림 같은 초현실 기운\n구역을 지날수록 점점 일그러져\n끝내 거울 같은 시간 감옥으로"],
  ];
  let y = 1.45;
  const hs = [0.95, 1.8, 2.05];
  items.forEach((it, i) => {
    labelCard(s, 7.4, y, 5.33, hs[i], it[0], it[1], { labelW: 1.1, bodyX: 1.4 });
    y += hs[i] + 0.25;
  });
}

// ---------- 6. 이야기 ----------
pres.addSection({ title: "세계관" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "세계관" });
  s.addText("이야기: 노인과 영웅", { placeholder: "title" });
  bullets(s, [
    { t: "두 겹의 세계", b: true, c: GOLD },
    "현실과 허상이 겹친 이중 구조",
    { t: "두 사람", b: true, c: GOLD },
    "현실: 병상에 누운 노인 시계공",
    "허상: 그가 어릴 적 동경한 서부극 영웅, 쌍권총 고양이",
    { t: "고양이가 아는 것", b: true, c: GOLD },
    "현실을 모르는 고양이",
    "소중한 무언가를 지켜야 한다는 감각 하나로, 잊히지 않으려고, 멈춘 시간을 되돌리려고 벌이는 싸움",
    { t: "결말", b: true, c: GOLD },
    "무너지는 공간을 떠나며 노인의 죽음을 받아들이는 결말",
  ], { x: 0.6, y: 1.45, w: 5.4, h: 5.3, fontSize: 14, paraSpaceAfter: 5 });
  picture(s, "04_world.png", 6.3, 1.45, 6.43, 6.43 * 9 / 16, "세계관 컨셉");
  caption(s, "세계관 시안: 거울에 비친 모습은 고양이가 아닌 노인", 6.3, 5.12, 6.43);
  card(s, 6.3, 5.55, 6.43, 1.2, PURPLE);
  text(s, "시간이 다한 소중한 사람은, 보내 줘야 한다", { x: 6.6, y: 5.7, w: 5.9, h: 0.5, fontSize: 19, bold: true, color: GOLD, valign: "middle" });
  text(s, "이 게임이 끝에 건네고 싶은 이야기", { x: 6.6, y: 6.2, w: 5.9, h: 0.35, fontSize: 12, color: MUTED });
  s.addNotes("주제 참고: 장화 신은 고양이: 끝내주는 모험. 죽음을 상징하는 존재는 이 게임만의 디자인.");
}

// ---------- 7. 구역 흐름 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "세계관" });
  s.addText("5구역: 서부에서 시간 감옥으로", { placeholder: "title" });
  const zones = ["달리풍 서부 마을", "무너지는 마을", "거울 황야", "시계 장치 하늘", "시간 감옥 중심"];
  const zw = 2.27, zg = 0.2;
  zones.forEach((z, i) => {
    const x = 0.6 + i * (zw + zg);
    const fill = i === 0 ? RED : i === 4 ? PURPLE : CARD;
    card(s, x, 1.45, zw, 0.95, fill);
    text(s, `구역 ${i + 1}`, { x: x + 0.2, y: 1.55, w: zw - 0.4, h: 0.3, fontSize: 11, color: i === 0 ? TXT : MUTED });
    text(s, z, { x: x + 0.2, y: 1.85, w: zw - 0.4, h: 0.4, fontSize: 15, bold: true });
    if (i < zones.length - 1) {
      s.addShape(pres.shapes.LINE, { x: x + zw + 0.02, y: 1.92, w: zg - 0.04, h: 0, line: { color: MUTED, width: 1.5, endArrowType: "triangle" }, objectName: nm("zarrow") });
    }
  });
  picture(s, "05_zone1_western.png", 0.6, 2.65, 4.4, 4.4 * 9 / 16, "구역 1 시안");
  caption(s, "구역 1 시안: 녹아내리는 시계와 갈라진 하늘", 0.6, 5.18, 4.4);
  picture(s, "04_world.png", 5.15, 2.65, 4.4, 4.4 * 9 / 16, "구역 5 시안");
  caption(s, "구역 5 시안: 시간 감옥과 시간의 주인", 5.15, 5.18, 4.4);
  labelCard(s, 9.75, 2.65, 2.98, 1.15, "구역 보스", "출구를 지키는 시간의 존재", { labelW: 2.5, bodyX: 0.25, bodyY: 0.6, bodySize: 13 });
  labelCard(s, 9.75, 3.98, 2.98, 1.15, "최종 상대", "시간의 존재들의 주인", { labelW: 2.5, bodyX: 0.25, bodyY: 0.6, bodySize: 13 });
  card(s, 0.6, 5.65, 12.13, 1.1);
  bullets(s, [
    "구역 보스를 쓰러뜨리면 '소중한 것'의 시간의 조각 획득. 조각이 모일수록 조금씩 드러나는 현실 속 노인의 모습",
    "시간의 주인은 런 도중 가끔 나타나 지켜보기만 하다가, 마지막 구역에서 최종 대결",
  ], { x: 0.9, y: 5.8, w: 11.6, h: 0.9, fontSize: 14, paraSpaceAfter: 4 });
}

// ---------- 8. 메인 UI ----------
pres.addSection({ title: "탄환 시계" });
function cylinder(s, cx, cy, r, labels, color, title) {
  s.addShape(pres.shapes.OVAL, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fill: { color: CARD }, line: { color, width: 2.5 }, objectName: nm("cyl") });
  const cr = r * 0.27;
  labels.forEach((lb, k) => {
    const a = (k / 6) * Math.PI * 2;
    const x = cx + Math.sin(a) * r * 0.58;
    const y = cy - Math.cos(a) * r * 0.58;
    s.addShape(pres.shapes.OVAL, { x: x - cr, y: y - cr, w: 2 * cr, h: 2 * cr, fill: { color }, line: { color, width: 1 }, objectName: nm("chamber") });
    text(s, String(lb), { x: x - cr, y: y - cr, w: 2 * cr, h: 2 * cr, fontSize: 14, bold: true, color: BG, align: "center", valign: "middle" });
  });
  text(s, title, { x: cx - 1.4, y: cy + r + 0.1, w: 2.8, h: 0.35, fontSize: 13, bold: true, align: "center", color });
}
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "탄환 시계" });
  s.addText("메인 UI: 탄환 시계", { placeholder: "title" });
  cylinder(s, 2.3, 2.95, 1.15, [2, 4, 6, 8, 10, 12], RED, "왼총 · 짝수 칸");
  cylinder(s, 11.03, 2.95, 1.15, [1, 3, 5, 7, 9, 11], GOLD, "오른총 · 홀수 칸");
  const cx = 6.67, cy = 2.95, r = 1.4;
  s.addShape(pres.shapes.OVAL, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fill: { color: "F3EDE2" }, line: { color: GOLD, width: 4 }, objectName: "clock_face" });
  for (let h = 1; h <= 12; h++) {
    const a = (h / 12) * Math.PI * 2;
    const x = cx + Math.sin(a) * r * 0.78;
    const y = cy - Math.cos(a) * r * 0.78;
    text(s, String(h), { x: x - 0.25, y: y - 0.2, w: 0.5, h: 0.4, fontSize: 14, bold: true, color: BG, align: "center", valign: "middle" });
  }
  s.addShape(pres.shapes.LINE, { x: cx, y: cy - r * 0.62, w: r * 0.31, h: r * 0.62, line: { color: RED, width: 4 }, flipV: true, objectName: "clock_hand" });
  s.addShape(pres.shapes.OVAL, { x: cx - 0.09, y: cy - 0.09, w: 0.18, h: 0.18, fill: { color: RED }, line: { color: RED }, objectName: "clock_pin" });
  // 열기 게이지
  const gx = 4.67, gy = 4.75, gw = 4.0, gh = 0.3;
  for (let k = 0; k < 3; k++) {
    const segW = (gw - 0.2) / 3;
    const fill = k < 2 ? RED : CARD;
    s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: gx + k * (segW + 0.1), y: gy, w: segW, h: gh, rectRadius: 0.08, fill: { color: fill }, line: { color: RED, width: 1 }, objectName: nm("heat") });
  }
  text(s, "열기 게이지: 3칸이 차면 궁극기", { x: gx, y: gy + 0.38, w: gw, h: 0.3, fontSize: 12, color: MUTED, align: "center" });
  card(s, 0.6, 5.55, 12.13, 1.05);
  bullets(s, [
    "화면 하단 가운데, 높이의 약 25%. 시계 바늘이 지금 쏠 차례를 가리키고, 한 발 쏠 때마다 칸이 비며 바늘이 한 칸 이동",
    "실린더 하나에 6발씩, 양쪽 합쳐 12칸. 시계의 12시간과 같은 구조",
  ], { x: 0.9, y: 5.72, w: 11.6, h: 0.95, fontSize: 14, paraSpaceAfter: 6 });
}

// ---------- 9. 발사 순서 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "탄환 시계" });
  s.addText("발사 순서 · 장전", { placeholder: "title" });
  const w = 0.82, gap = 0.17, x0 = 0.6, y = 1.9;
  for (let i = 1; i <= 12; i++) {
    const odd = i % 2 === 1;
    const x = x0 + (i - 1) * (w + gap);
    const col = odd ? GOLD : RED;
    s.addShape(pres.shapes.OVAL, { x, y, w, h: w, fill: { color: col }, line: { color: col, width: 1 }, objectName: nm("shot") });
    text(s, String(i), { x, y, w, h: w, fontSize: 20, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, odd ? "오른" : "왼", { x: x - 0.1, y: y + w + 0.1, w: w + 0.2, h: 0.3, fontSize: 12, color: col, align: "center" });
  }
  s.addShape(pres.shapes.LINE, { x: x0 + w / 2, y: 3.45, w: 11 * (w + gap), h: 0, line: { color: MUTED, width: 2, dashType: "dash", beginArrowType: "triangle" }, objectName: "loop_arrow" });
  text(s, "12발을 다 쏘면 바늘이 실린더처럼 한 바퀴 돌며 장전, 다시 1부터 시작", { x: 0.6, y: 3.6, w: 12.1, h: 0.4, fontSize: 14, color: MUTED, align: "center" });
  const cards = [
    ["순서 고정", "실제 발사 순서는 상시 1 → 12\n홀수 칸은 오른총, 짝수 칸은 왼총\n번갈아 자동 발사"],
    ["효과는 모든 사격에", "활성화 탄의 효과는\n모든 사격에 함께 적용\n그 탄을 직접 쏠 때는 더 강하게 발동"],
    ["완벽 장전", "바늘이 12를 지나는 순간 R 입력 시\n장전 속도 증가\n다음 탄 강화"],
  ];
  cards.forEach((c, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 4.35, 3.87, 2.3);
    text(s, c[0], { x: x + 0.3, y: 4.6, w: 3.3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, c[1], { x: x + 0.3, y: 5.15, w: 3.3, h: 1.4, fontSize: 14, lineSpacingMultiple: 1.2 });
  });
}

// ---------- 10. 탄환 ----------
pres.addSection({ title: "빌드" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("탄환 = 아이템, 12칸 = 인벤토리", { placeholder: "title" });
  text(s, "탄을 모아 칸에 장착. 어디에 놓느냐가 곧 빌드.", { x: 0.6, y: 1.35, w: 12, h: 0.4, fontSize: 16, color: MUTED });
  const parts = [
    ["자리 조건", "탄마다 활성화 조건 존재\n7시 · 3의 배수 · 소수 · 음수 등, 판정 시각이 맞으면 활성화"],
    ["특성", "탄 하나에 특성 2개 (스컬 각인식)\n같은 특성이 2 · 4 · 6개 모이면 단계 상승"],
    ["고유 효과", "탄마다 다른 효과\n활성화되면 모든 사격에 함께 적용"],
    ["공격력", "활성화 여부와 상관없이\n그 탄을 쏠 때 상시 적용"],
    ["연산 슬롯", "탄마다 0~2칸, 등급과 무관\n강한 탄은 슬롯이 적은 경우도 존재"],
    ["등급 · 이름", "일반 · 희귀 · 영웅 · 전설 4등급\n'~탄'으로 맞추지 않고, 높은 등급일수록 저만의 이름"],
  ];
  parts.forEach((p, i) => {
    const x = 0.6 + (i % 2) * 6.13;
    const y = 1.9 + Math.floor(i / 2) * 1.32;
    labelCard(s, x, y, 5.95, 1.17, p[0], p[1], { labelW: 1.6, bodyX: 1.85, bodySize: 13 });
  });
  card(s, 0.6, 5.95, 12.13, 0.8, PURPLE);
  text(s, [
    { text: "활성화 탄  ", options: { bold: true, color: GOLD } },
    { text: "특성 + 고유 효과 + 공격력      " },
    { text: "비활성화 탄  ", options: { bold: true, color: RED } },
    { text: "공격력만" },
  ], { x: 0.9, y: 6.1, w: 11.6, h: 0.5, fontSize: 16, valign: "middle" });
}

// ---------- 11. 탄 연산 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("탄 연산: 판정 시각", { placeholder: "title" });
  text(s, "탄에 사칙연산을 끼우면, 놓인 시각이 아닌 계산한 값으로 판정.", { x: 0.6, y: 1.4, w: 12.1, h: 0.4, fontSize: 16, color: MUTED });
  flow(s, 0.6, 2.1, 2.1, 1.1, 0.55, [["놓인 시각", "3시"], ["위 슬롯", "+1"], ["아래 슬롯", "×2"], ["판정 시각", "8시"]]);
  flow(s, 0.6, 3.55, 2.1, 1.1, 0.55, [["놓인 시각", "3시"], ["위 슬롯", "×2"], ["아래 슬롯", "+1"], ["판정 시각", "7시"]]);
  card(s, 11.0, 2.1, 1.73, 2.55, PURPLE);
  text(s, "같은 연산이라도\n끼우는 순서에 따라\n다른 결과", { x: 11.12, y: 2.3, w: 1.5, h: 2.2, fontSize: 13, valign: "middle" });
  card(s, 0.6, 5.0, 12.13, 1.45);
  bullets(s, [
    "연산은 따로 드랍되는 아이템. 탄의 위 · 아래 슬롯에 끼우고, 위 슬롯부터 계산",
    "바뀌는 건 판정 시각뿐. 실제 발사 순서는 그대로 유지",
    "연산과 총 교체는 전투가 없을 때만 가능",
  ], { x: 0.9, y: 5.2, w: 11.6, h: 1.3, fontSize: 15, paraSpaceAfter: 6 });
}

// ---------- 12. 판정 규칙 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("판정 규칙", { placeholder: "title" });
  const head = { lang: "ko-KR", bold: true, color: BG, fill: { color: GOLD }, fontSize: 14, valign: "middle" };
  const cell = (t, o = {}) => ({ text: t, options: { lang: "ko-KR", color: TXT, fill: { color: CARD }, fontSize: 13, valign: "middle", ...o } });
  const rows = [
    [{ text: "항목", options: head }, { text: "규칙", options: head }, { text: "비고", options: head }],
    [cell("연산 종류", { bold: true }), cell("+1~+3 · -1~-3 · ×2 · ×3 · ÷2 · ÷3"), cell("×0 · ÷0 없음")],
    [cell("범위", { bold: true }), cell("12 초과 · 0 이하도 값 그대로 유지"), cell("14시 · -2시도 가능. 음수 조건 탄도 존재")],
    [cell("나눗셈", { bold: true }), cell("나누어떨어지지 않으면 반올림. .5는 0에서 먼 쪽으로"), cell("4.5 → 5, -4.5 → -5")],
    [cell("배수", { bold: true }), cell("0과 음수도 배수로 인정"), cell("-6시는 3의 배수이자 음수")],
    [cell("0시", { bold: true }), cell("모든 배수 조건을 채우는 칸. 빌드 조정용으로 일부러 둔 칸"), cell("-3을 끼운 탄을 3시에 → 0시")],
    [cell("예시", { bold: true }), cell("×2를 끼운 탄을 7시에 놓으면"), cell("14시로 판정 → '12 초과' 조건 충족")],
  ];
  s.addTable(rows, { x: 0.6, y: 1.5, w: 12.13, colW: [1.6, 5.6, 4.93], rowH: 0.66, border: { type: "solid", color: HEX.dk1, pt: 2 }, fontFace: THEME.bodyFontFace, margin: [0, 0.15, 0, 0.15], objectName: "rule_table" });
  s.addNotes("구현 메모: Unity Mathf.Round는 4.5를 4로 반올림하므로 반올림 함수 별도 구현.");
}

// ---------- 13. 총 · 궁극기 ----------
pres.addSection({ title: "전투" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "전투" });
  s.addText("총 · 스킬 · 궁극기", { placeholder: "title" });
  card(s, 0.6, 1.45, 5.95, 5.3);
  text(s, "총 (스컬식)", { x: 0.95, y: 1.7, w: 5.3, h: 0.5, fontSize: 22, bold: true, color: GOLD });
  bullets(s, [
    "총도 드랍 아이템. 양손에 한 자루씩, 딱 두 자루만 장착",
    "오른손은 홀수 칸, 왼손은 짝수 칸 담당",
    "새 총을 주우면 왼손 · 오른손 · 버리기 중 선택",
    "같은 총이라도 손마다 다른 효과",
    "스킬은 총에 귀속. 총마다 하나씩, 양손 합쳐 두 개",
    { t: "사격 감각", b: true, c: GOLD },
    "리볼버답게 거의 즉발 판정, 궤적은 레이저처럼 빠른 선",
    "탄마다 다른 궤적 색과 피격 이펙트",
  ], { x: 0.95, y: 2.4, w: 5.3, h: 4.2, fontSize: 15, paraSpaceAfter: 8 });
  card(s, 6.78, 1.45, 5.95, 5.3, PURPLE);
  text(s, "궁극기: 불릿 파티", { x: 7.13, y: 1.7, w: 5.3, h: 0.5, fontSize: 22, bold: true, color: GOLD });
  const steps = [
    "열기 게이지 3칸이 차면 Q. 열기는 탄 명중 시에만 충전",
    "연기처럼 사라졌다가 대상 적의 등 뒤에서 등장",
    "12발을 쏘는 동안 건카타 8자세를 홱홱 끊어 넘기며 좌우로 이동",
    "방금 섰던 자리에 금빛 실루엣이 남아 함께 사격. 카메라 줌 인",
  ];
  steps.forEach((t, i) => {
    const y = 2.45 + i * 0.95;
    s.addShape(pres.shapes.OVAL, { x: 7.13, y, w: 0.55, h: 0.55, fill: { color: GOLD }, line: { color: GOLD }, objectName: nm("step") });
    text(s, String(i + 1), { x: 7.13, y, w: 0.55, h: 0.55, fontSize: 16, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, t, { x: 7.85, y: y - 0.1, w: 4.7, h: 0.75, fontSize: 14, valign: "middle" });
  });
  text(s, "장전한 탄이 순서대로 나가므로, 빌드마다 다른 궁극기 연출", { x: 7.13, y: 6.25, w: 5.3, h: 0.35, fontSize: 12, color: MUTED });
}

// ---------- 14. 레벨 디자인 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "전투" });
  s.addText("레벨 디자인", { placeholder: "title" });
  flow(s, 0.6, 1.5, 1.75, 1.0, 0.326, [["거점", "마을"], ["스테이지", "1"], ["스테이지", "2"], ["스테이지", "3"], ["스테이지", "4"], ["챕터 끝", "보스"]], { bigSize: 18 });
  text(s, "챕터 5개 = 구역 5개. 스테이지 하나는 전투 방 4~5개가 문으로 이어진 덩어리. 방을 모두 비우면 보상 획득 후 문 2~3개 중 하나를 골라 진행하며, 문 위에 다음 보상 표시.",
    { x: 0.6, y: 2.75, w: 12.1, h: 0.8, fontSize: 14, lineSpacingMultiple: 1.25, color: MUTED });
  const cols = [
    ["선택형 보상", GOLD, ["총탄 · 총탄 강화(연산) · 총", "셋 중 하나 선택"]],
    ["확정형 보상", TEAL, ["골드 · 시간의 조각: 진행할수록 많이", "방어구 강화: 체력 · 방어력 상승"]],
    ["상인", RED, ["전투를 끝낸 방에 확률로 등장 (챕터마다 최소 한 번)", "골드로 탄 · 연산 · 총 · 회복 구매"]],
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 3.75, 3.87, 2.05);
    text(s, c[0], { x: x + 0.3, y: 3.95, w: 3.3, h: 0.4, fontSize: 17, bold: true, color: c[1] });
    bullets(s, c[2], { x: x + 0.3, y: 4.5, w: 3.35, h: 1.25, fontSize: 13, paraSpaceAfter: 6 });
  });
  card(s, 0.6, 6.0, 12.13, 0.75, PURPLE);
  text(s, [
    { text: "판의 시작  ", options: { bold: true, color: GOLD } },
    { text: "12칸 모두 효과 없는 기본 '탄환'으로 출발. 골드는 그 판에서만 쓰고, 판이 끝나면 소멸" },
  ], { x: 0.9, y: 6.1, w: 11.6, h: 0.55, fontSize: 14, valign: "middle" });
}

// ---------- 15. 드랍 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "전투" });
  s.addText("드랍 요소", { placeholder: "title" });
  const items = [
    ["탄환", GOLD, ["12칸에 장착", "자리 조건과 특성 2개", "고유 효과 · 공격력", "연산 슬롯 0~2칸"]],
    ["연산", TEAL, ["탄의 연산 슬롯에 장착", "위 슬롯부터 계산", "±1~3 · ×2 · ×3 · ÷2 · ÷3"]],
    ["총", RED, ["양손에 두 자루, 주우면 교체", "공격력 · 연사 · 손별 효과", "스킬 1개"]],
  ];
  // 빌드 재료
  items.forEach((it, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 1.45, 3.87, 2.6);
    s.addShape(pres.shapes.OVAL, { x: x + 0.3, y: 1.7, w: 0.7, h: 0.7, fill: { color: it[1] }, line: { color: it[1] }, objectName: nm("dot") });
    text(s, String(i + 1), { x: x + 0.3, y: 1.7, w: 0.7, h: 0.7, fontSize: 20, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, it[0], { x: x + 1.2, y: 1.75, w: 2.4, h: 0.6, fontSize: 22, bold: true, color: it[1], valign: "middle" });
    bullets(s, it[2], { x: x + 0.3, y: 2.65, w: 3.35, h: 1.6, fontSize: 14, paraSpaceAfter: 4 });
  });
  // 재화
  const money = [
    ["골드", GOLD, "그 판에서만 쓰는 재화. 상인에게서 탄 · 연산 · 총 · 회복 구매\n판이 끝나면 소멸"],
    ["시간의 조각", C.accent6, "판이 끝나도 남는 영구 재화. 기억 강화 · 시설 레벨 · 복장 해금에 사용\n조각마다 노인의 삶을 담은 짧은 글"],
  ];
  money.forEach((m, i) => {
    const x = 0.6 + i * 6.19;
    card(s, x, 4.3, 5.94, 1.45);
    s.addShape(pres.shapes.OVAL, { x: x + 0.3, y: 4.52, w: 0.5, h: 0.5, fill: { color: m[1] }, line: { color: m[1] }, objectName: nm("dot") });
    text(s, String(i + 4), { x: x + 0.3, y: 4.52, w: 0.5, h: 0.5, fontSize: 15, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, m[0], { x: x + 0.95, y: 4.52, w: 4.7, h: 0.5, fontSize: 18, bold: true, color: m[1], valign: "middle" });
    text(s, m[2], { x: x + 0.3, y: 5.15, w: 5.4, h: 0.5, fontSize: 12, lineSpacingMultiple: 1.15 });
  });
  card(s, 0.6, 6.0, 12.13, 0.75, PURPLE);
  text(s, [
    { text: "런 안의 빌드  ", options: { bold: true, color: GOLD } },
    { text: "탄 배치 + 연산 장착 + 총 선택. 교체는 전투가 없을 때만" },
  ], { x: 0.9, y: 6.08, w: 11.6, h: 0.6, fontSize: 15, valign: "middle" });
}

// ---------- 16. 성장 · 해금 ----------
pres.addSection({ title: "성장" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "성장" });
  s.addText("성장 · 해금", { placeholder: "title" });
  const cols = [
    ["기억", "영구 강화", ["판이 끝난 뒤 기억의 방에서 강화", "한 번 찍은 기억은 영구 유지", "재화는 시간의 조각. 조각마다 노인의 삶을 담은 짧은 글"]],
    ["복장", "해금 요소", ["해금한 복장 중 한 벌을 골라 판 시작", "복장마다 다른 효과, 다른 출발점"]],
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 1.5, 3.87, 3.9);
    text(s, c[0], { x: x + 0.3, y: 1.75, w: 3.3, h: 0.55, fontSize: 22, bold: true, color: GOLD });
    text(s, c[1], { x: x + 0.3, y: 2.3, w: 3.3, h: 0.35, fontSize: 13, color: MUTED });
    bullets(s, c[2], { x: x + 0.3, y: 2.85, w: 3.35, h: 2.45, fontSize: 14, paraSpaceAfter: 8 });
  });
  // 마을 시설
  const x = 0.6 + 2 * 4.13;
  card(s, x, 1.5, 3.87, 3.9);
  text(s, "마을", { x: x + 0.3, y: 1.75, w: 3.3, h: 0.55, fontSize: 22, bold: true, color: GOLD });
  text(s, "챕터 보스를 넘길 때마다 시설 개방", { x: x + 0.3, y: 2.3, w: 3.3, h: 0.35, fontSize: 13, color: MUTED });
  [["기억의 방", "처음부터"], ["총포상", "챕터 1 보스 후"], ["탄약 공방", "챕터 2 보스 후"], ["의상실", "챕터 3 보스 후"]].forEach((f, k) => {
    const y = 2.85 + k * 0.6;
    text(s, f[0], { x: x + 0.3, y, w: 1.5, h: 0.45, fontSize: 14, bold: true, valign: "middle" });
    text(s, f[1], { x: x + 1.8, y, w: 1.8, h: 0.45, fontSize: 13, color: MUTED, align: "right", valign: "middle" });
  });
  card(s, 0.6, 5.65, 12.13, 1.1, PURPLE);
  text(s, [
    { text: "거점  ", options: { bold: true, color: GOLD } },
    { text: "서부 여관 2층 방. 현실의 병실이 숨어 있어, 기억을 되찾을수록 조금씩 병실을 닮아 가는 방" },
  ], { x: 0.9, y: 5.85, w: 11.6, h: 0.7, fontSize: 15, valign: "middle" });
  s.addNotes("마을은 전투와 같은 3D 탑뷰로 이동, 시설 앞에서 E로 입장. 시설 레벨은 시간의 조각으로 강화.");
}

(async () => {
  await pres.writeFile({ fileName: OUT });
  await applyTheme(OUT, THEME);
  console.log("written", OUT);
})();
