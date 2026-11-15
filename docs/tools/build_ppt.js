// Cat_Tang 컨셉 기획서 PPT 생성 (확정 내용만)
// 실행: NODE_PATH=<pptxgenjs가 있는 node_modules> PPTX_SKILL=<pptx 스킬 폴더> node build_ppt.js
const pptxgen = require("pptxgenjs");
const path = require("path");
const fs = require("fs");
const { applyTheme } = require(process.env.PPTX_SKILL + "/scripts/apply_theme.js");

const IMG = "D:/pro/Cat_Tang/docs/images";
const OUT = "D:/pro/Cat_Tang/docs/Cat_Tang_컨셉_기획서.pptx";
const AUTHOR = "정승운";
const img = (n) => path.join(IMG, n);
const has = (n) => fs.existsSync(img(n));

const THEME = {
  name: "Cat_Tang Clock",
  headFontFace: "Malgun Gothic",
  bodyFontFace: "Malgun Gothic",
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
pres.title = "Cat_Tang 컨셉 기획서";
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
    { text: { text: `CAT_TANG · 컨셉 기획서 · ${AUTHOR}`, options: { x: 0.6, y: 7.0, w: 6, h: 0.3, fontSize: 10, color: MUTED, margin: 0 } } },
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
  slide.addText(t, { isTextBox: true, fontFace: THEME.bodyFontFace, color: TXT, fontSize: 15, margin: 0, valign: "top", objectName: nm("text"), ...o });
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
function picture(slide, file, x, y, w, h, label) {
  if (has(file)) {
    slide.addImage({ path: img(file), x, y, w, h, sizing: { type: "cover", w, h }, objectName: nm("img"), altText: label });
  } else {
    slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: CARD }, line: { color: CARD } });
    text(slide, label, { x, y: y + h / 2 - 0.2, w, h: 0.4, align: "center", color: MUTED });
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

// ---------- 1. 표지 ----------
pres.addSection({ title: "표지" });
{
  const s = pres.addSlide({ masterName: "COVER", sectionTitle: "표지" });
  picture(s, "01_keyart.png", 0, 0, 13.333, 7.5, "키 아트");
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 7.75, y: 4.2, w: 5.1, h: 2.85, rectRadius: 0.12, fill: { color: BG, transparency: 20 }, line: { color: BG, transparency: 100 }, objectName: "cover_panel" });
  text(s, "CAT_TANG", { x: 8.1, y: 4.4, w: 4.6, h: 0.95, fontSize: 50, bold: true, color: GOLD, fontFace: THEME.headFontFace });
  text(s, "캣탕 · 컨셉 기획서", { x: 8.1, y: 5.35, w: 4.6, h: 0.5, fontSize: 24, bold: true });
  text(s, "3D 탑뷰 빌드형 로그라이크 액션", { x: 8.1, y: 5.9, w: 4.6, h: 0.4, fontSize: 16, color: MUTED });
  text(s, `작성 ${AUTHOR} · 2026.11`, { x: 8.1, y: 6.35, w: 4.6, h: 0.3, fontSize: 13, color: TXT });
  text(s, "확정안 기준 · 이미지는 컨셉 시안", { x: 8.1, y: 6.68, w: 4.6, h: 0.3, fontSize: 11, color: MUTED });
  s.addNotes("제목 뜻: 고양이(Cat) + 총소리 '탕'. 이 문서는 확정된 내용만 담음.");
}

// ---------- 2. 한눈에 보기 ----------
pres.addSection({ title: "개요" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "개요" });
  s.addText("한눈에 보기", { placeholder: "title" });
  text(s, "노인 시계공이 어릴 적 동경한 서부극 영웅, 쌍권총 고양이.\n멈춘 시간에서 탈출하려 싸우다\n끝내 노인의 죽음을 받아들이고 떠나는 이야기.", { x: 0.6, y: 1.45, w: 6.0, h: 1.1, fontSize: 16, lineSpacingMultiple: 1.2 });
  const rows = [
    ["장르", "빌드형 로그라이크 액션 (세피리아식)"],
    ["시점", "3D 탑뷰"],
    ["핵심", "총 액션 + 건카타 궁극기"],
    ["아트", "하데스풍 카툰 3D"],
    ["엔진", "Unity (URP)"],
  ];
  rows.forEach((r, i) => {
    const y = 2.75 + i * 0.8;
    card(s, 0.6, y, 6.0, 0.66);
    text(s, r[0], { x: 0.85, y: y + 0.13, w: 1.0, h: 0.4, fontSize: 14, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 1.95, y: y + 0.13, w: 4.5, h: 0.4, fontSize: 15, valign: "middle" });
  });
  picture(s, "02_ingame.png", 7.0, 1.45, 5.73, 5.2, "인게임 목업");
  caption(s, "인게임 화면 시안", 7.0, 6.7, 5.7);
}

// ---------- 3. 주인공 ----------
pres.addSection({ title: "캐릭터 · 아트" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "캐릭터 · 아트" });
  s.addText("주인공: 쌍권총 고양이", { placeholder: "title" });
  card(s, 0.6, 1.45, 7.6, 4.3, "F3EDE2");
  picture(s, "03_turnaround.png", 0.7, 1.55, 7.4, 4.1, "턴어라운드");
  caption(s, "3D 모델링용 턴어라운드 시안 (앞 · 옆 · 뒤)", 0.6, 5.85, 7.6);
  bullets(s, [
    { t: "출처", b: true, c: GOLD },
    "랫키호테 경찰서 보스 '쌍총 사냥꾼'",
    "쌍권총 고양이 설정 그대로",
    { t: "디자인 변경", b: true, c: GOLD },
    "경찰 배지 → 11시 59분에 멈춘 회중시계",
    "실린더가 시계판인 금장 리볼버",
    "모자 띠 총알 5발 → 6발",
    { t: "필수", b: true, c: GOLD },
    "건카타 궁극기",
  ], { x: 8.6, y: 1.5, w: 4.2, h: 5.0, fontSize: 14, paraSpaceAfter: 6 });
}

// ---------- 4. 아트 방향 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "캐릭터 · 아트" });
  s.addText("아트 방향", { placeholder: "title" });
  picture(s, "01_keyart.png", 0.6, 1.45, 6.4, 5.2, "키 아트");
  caption(s, "키 아트 시안", 0.6, 6.7, 6.4);
  const items = [
    ["그림체", "하데스풍 카툰 느낌의 3D"],
    ["비율", "랫키호테 쌍총 사냥꾼 그대로\n약 3등신 · 큰 머리 · 넓은 모자 챙\n큰 총 · 큰 부츠 · 굵은 줄무늬 꼬리"],
    ["공간", "서부 마을에서 시작, 처음부터 달리풍\n초현실 기운. 점점 일그러져\n거울 같은 몽환적 시간 감옥으로"],
  ];
  let y = 1.45;
  const hs = [1.0, 1.85, 1.85];
  items.forEach((it, i) => {
    labelCard(s, 7.4, y, 5.33, hs[i], it[0], it[1], { labelW: 1.1, bodyX: 1.4 });
    y += hs[i] + 0.25;
  });
}

// ---------- 5. 이야기 ----------
pres.addSection({ title: "세계관" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "세계관" });
  s.addText("이야기: 노인과 영웅", { placeholder: "title" });
  picture(s, "04_world.png", 6.3, 1.45, 6.43, 5.2, "세계관 컨셉");
  caption(s, "세계관 시안: 거울에 비친 모습은 고양이가 아닌 노인", 6.3, 6.7, 6.4);
  bullets(s, [
    { t: "구조", b: true, c: GOLD },
    "현실 → 허상 이중 구조 (랫키호테 방식)",
    { t: "두 사람", b: true, c: GOLD },
    "소망의 주인: 병상의 노인 시계공",
    "주인공: 노인이 어릴 적 동경한 서부극 영웅",
    { t: "주인공의 시점", b: true, c: GOLD },
    "현실을 모름. 소중한 것을 지켜야 한다는 감각만 있음",
    "잊히지 않으려고, 시간을 되돌리려고 탈출을 시도",
    { t: "결말 · 교훈", b: true, c: GOLD },
    "무너지는 공간을 떠나며 노인의 죽음을 받아들임",
    "시간이 다한 소중한 이는 보내 줘야 함",
  ], { x: 0.6, y: 1.45, w: 5.4, h: 5.3, fontSize: 14, paraSpaceAfter: 5 });
  s.addNotes("주제 참고: 장화 신은 고양이: 끝내주는 모험. 죽음을 상징하는 존재는 독자 디자인.");
}

// ---------- 6. 구역 흐름 ----------
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
  picture(s, "05_zone1_western.png", 0.6, 2.65, 4.4, 2.48, "구역 1 시안");
  caption(s, "구역 1 시안: 녹아내리는 시계, 갈라진 하늘", 0.6, 5.18, 4.4);
  picture(s, "04_world.png", 5.15, 2.65, 4.4, 2.48, "구역 5 시안");
  caption(s, "구역 5 시안: 시간 감옥과 시간의 주인", 5.15, 5.18, 4.4);
  labelCard(s, 9.75, 2.65, 2.98, 1.15, "구역 보스", "출구를 지키는 시간의 존재", { labelW: 2.5, bodyX: 0.25, bodyY: 0.6, bodySize: 13 });
  labelCard(s, 9.75, 3.98, 2.98, 1.15, "최종 상대", "시간의 존재들의 주인", { labelW: 2.5, bodyX: 0.25, bodyY: 0.6, bodySize: 13 });
  card(s, 0.6, 5.65, 12.13, 1.1);
  bullets(s, [
    "보스를 이기면 '소중한 것'의 시간의 조각 획득. 모일수록 현실(노인)이 조금씩 보임",
    "시간의 주인은 런 도중 가끔 나타나 지켜보기만 함. 마지막 구역에서 맞섬",
  ], { x: 0.9, y: 5.8, w: 11.6, h: 0.9, fontSize: 14, paraSpaceAfter: 4 });
}

// ---------- 7. 메인 UI ----------
pres.addSection({ title: "탄환 시계" });
function cylinder(s, cx, cy, r, labels, color, title) {
  s.addShape(pres.shapes.OVAL, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fill: { color: CARD }, line: { color, width: 2.5 }, objectName: nm("cyl") });
  const cr = r * 0.27;
  labels.forEach((lb, k) => {
    const a = (k / 6) * Math.PI * 2;
    const x = cx + Math.sin(a) * r * 0.58;
    const y = cy - Math.cos(a) * r * 0.58;
    const special = lb === 12;
    s.addShape(pres.shapes.OVAL, { x: x - cr, y: y - cr, w: 2 * cr, h: 2 * cr, fill: { color: special ? GOLD : color }, line: { color: special ? "F3EDE2" : color, width: special ? 2.5 : 1 }, objectName: nm("chamber") });
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
  text(s, "열기 게이지 (3칸 차면 궁극기)", { x: gx, y: gy + 0.38, w: gw, h: 0.3, fontSize: 12, color: MUTED, align: "center" });
  card(s, 0.6, 5.55, 12.13, 1.2);
  bullets(s, [
    "화면 하단 가운데, 높이 약 25%. 가운데 시계 바늘 = 지금 쏠 차례. 쏠 때마다 칸이 비고 바늘이 한 칸 이동",
    "실린더 하나에 6발, 양쪽 합쳐 12칸. 12시 칸만 따로 강해지는 규칙은 없음",
  ], { x: 0.9, y: 5.72, w: 11.6, h: 0.95, fontSize: 14, paraSpaceAfter: 6 });
}

// ---------- 8. 발사 순서 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "탄환 시계" });
  s.addText("발사 순서 · 장전", { placeholder: "title" });
  const w = 0.82, gap = 0.17, x0 = 0.6, y = 1.9;
  for (let i = 1; i <= 12; i++) {
    const odd = i % 2 === 1;
    const x = x0 + (i - 1) * (w + gap);
    const col = odd ? GOLD : RED;
    s.addShape(pres.shapes.OVAL, { x, y, w, h: w, fill: { color: col }, line: { color: i === 12 ? "F3EDE2" : col, width: i === 12 ? 3 : 1 }, objectName: nm("shot") });
    text(s, String(i), { x, y, w, h: w, fontSize: 20, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, odd ? "오른" : "왼", { x: x - 0.1, y: y + w + 0.1, w: w + 0.2, h: 0.3, fontSize: 12, color: col, align: "center" });
  }
  s.addShape(pres.shapes.LINE, { x: x0 + w / 2, y: 3.45, w: 11 * (w + gap), h: 0, line: { color: MUTED, width: 2, dashType: "dash", beginArrowType: "triangle" }, objectName: "loop_arrow" });
  text(s, "12 발사 후 바늘이 실린더 장전처럼 회전 → 초기화 → 1부터 다시", { x: 0.6, y: 3.6, w: 12.1, h: 0.4, fontSize: 14, color: MUTED, align: "center" });
  const cards = [
    ["순서 고정", "실제 발사 순서는 늘 1 → 12\n홀수 칸 오른총, 짝수 칸 왼총\n자동으로 번갈아 발사"],
    ["효과 범위", "깨어난 탄 효과는\n모든 사격에 적용\n12시 칸 강화 없음"],
    ["완벽 장전", "바늘이 12를 지날 때 맞춰 누름\n장전이 빨라지고\n다음 탄 강화"],
  ];
  cards.forEach((c, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 4.35, 3.87, 2.3);
    text(s, c[0], { x: x + 0.3, y: 4.6, w: 3.3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, c[1], { x: x + 0.3, y: 5.15, w: 3.3, h: 1.4, fontSize: 14, lineSpacingMultiple: 1.2 });
  });
}

// ---------- 9. 탄환 ----------
pres.addSection({ title: "빌드" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("탄환 = 아이템, 12칸 = 인벤토리", { placeholder: "title" });
  text(s, "탄을 모아 칸에 넣음. 놓는 자리가 곧 빌드.", { x: 0.6, y: 1.35, w: 12, h: 0.4, fontSize: 16, color: MUTED });
  const parts = [
    ["자리 조건", "탄에만 붙음. 판정 시각이 맞아야 깨어남\n예: 7시 · 3의 배수 · 4의 배수 · 소수 · 음수"],
    ["특성", "탄 하나에 2개. 겹칠수록 강해짐 (스컬 각인식)\n같은 특성 2 · 4 · 6개에 단계 상승"],
    ["고유 효과", "탄마다 다른 효과. 깨어나야 발동"],
    ["공격력", "깨어나든 아니든 항상 적용"],
    ["연산 슬롯", "탄마다 0~2칸. 등급과 무관\n효과가 강한 탄은 슬롯이 적을 수 있음"],
    ["등급 · 이름", "4등급. '~탄'으로 통일하지 않음\n등급이 높을수록 고유한 이름"],
  ];
  parts.forEach((p, i) => {
    const x = 0.6 + (i % 2) * 6.13;
    const y = 1.9 + Math.floor(i / 2) * 1.32;
    labelCard(s, x, y, 5.95, 1.17, p[0], p[1], { labelW: 1.6, bodyX: 1.85, bodySize: 13 });
  });
  card(s, 0.6, 5.95, 12.13, 0.8, PURPLE);
  text(s, [
    { text: "깨어난 탄  ", options: { bold: true, color: GOLD } },
    { text: "특성 + 고유 효과 + 공격력      " },
    { text: "못 깨어난 탄  ", options: { bold: true, color: RED } },
    { text: "공격력만" },
  ], { x: 0.9, y: 6.1, w: 11.6, h: 0.5, fontSize: 16, valign: "middle" });
}

// ---------- 10. 탄 연산 ----------
function flow(s, y, steps) {
  const w = 2.1, gap = 0.55;
  steps.forEach((st, i) => {
    const x = 0.6 + i * (w + gap);
    const last = i === steps.length - 1;
    card(s, x, y, w, 1.1, last ? GOLD : CARD);
    text(s, st[0], { x, y: y + 0.12, w, h: 0.3, fontSize: 11, align: "center", color: last ? BG : MUTED });
    text(s, st[1], { x, y: y + 0.42, w, h: 0.55, fontSize: 22, bold: true, align: "center", color: last ? BG : TXT });
    if (!last) {
      s.addShape(pres.shapes.LINE, { x: x + w + 0.08, y: y + 0.55, w: gap - 0.16, h: 0, line: { color: MUTED, width: 2, endArrowType: "triangle" }, objectName: nm("arrow") });
    }
  });
}
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("탄 연산: 판정 시각", { placeholder: "title" });
  text(s, "탄의 연산 슬롯에 사칙연산을 끼움. 그 탄은 놓인 시각에 연산을 적용한 값으로 판정.", { x: 0.6, y: 1.4, w: 12.1, h: 0.4, fontSize: 16, color: MUTED });
  flow(s, 2.1, [["놓인 시각", "3시"], ["위 슬롯", "+1"], ["아래 슬롯", "×2"], ["판정 시각", "8시"]]);
  flow(s, 3.55, [["놓인 시각", "3시"], ["위 슬롯", "×2"], ["아래 슬롯", "+1"], ["판정 시각", "7시"]]);
  card(s, 11.0, 2.1, 1.73, 2.55, PURPLE);
  text(s, "같은 연산도\n끼운 순서에 따라\n결과가 달라짐", { x: 11.15, y: 2.3, w: 1.5, h: 2.2, fontSize: 13, valign: "middle" });
  card(s, 0.6, 5.0, 12.13, 1.6);
  bullets(s, [
    "연산은 따로 드랍되는 아이템. 탄의 슬롯(위 · 아래)에 끼우고 위 슬롯 먼저 계산",
    "바뀌는 것은 판정 시각뿐. 실제 발사 순서는 그대로",
    "연산 · 총 교체는 전투 밖에서만",
  ], { x: 0.9, y: 5.2, w: 11.6, h: 1.3, fontSize: 15, paraSpaceAfter: 6 });
}

// ---------- 11. 판정 규칙 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "빌드" });
  s.addText("판정 규칙", { placeholder: "title" });
  const head = { bold: true, color: BG, fill: { color: GOLD }, fontSize: 14, valign: "middle" };
  const cell = (t, o = {}) => ({ text: t, options: { color: TXT, fill: { color: CARD }, fontSize: 13, valign: "middle", ...o } });
  const rows = [
    [{ text: "항목", options: head }, { text: "규칙", options: head }, { text: "예시", options: head }],
    [cell("연산 종류", { bold: true }), cell("+1~+3 · -1~-3 · ×2 · ×3 · ÷2 · ÷3"), cell("×0 · ÷0 없음")],
    [cell("범위", { bold: true }), cell("12 초과 · 0 이하도 값 유지. 한 바퀴 돌리지 않음"), cell("14시 · -2시 가능, 음수 조건 탄 존재")],
    [cell("나눗셈", { bold: true }), cell("나누어떨어지지 않으면 반올림. .5는 0에서 먼 쪽"), cell("4.5 → 5, -4.5 → -5")],
    [cell("배수", { bold: true }), cell("0과 음수도 배수 조건 충족"), cell("-6시 = 3의 배수 + 음수")],
    [cell("0시", { bold: true }), cell("모든 배수 조건 충족. 빌드 조정 도구로 일부러 둠"), cell("-3을 끼운 탄을 3시에 → 0시")],
    [cell("예시", { bold: true }), cell("×2를 끼운 탄을 7시에"), cell("판정 14시 → '12 초과' 조건 충족")],
  ];
  s.addTable(rows, { x: 0.6, y: 1.5, w: 12.13, colW: [1.6, 5.4, 5.13], rowH: 0.6, border: { type: "solid", color: HEX.dk1, pt: 2 }, fontFace: THEME.bodyFontFace, margin: [0, 0.15, 0, 0.15], objectName: "rule_table" });
  text(s, "구현 메모: Unity Mathf.Round는 4.5 → 4로 반올림하므로 별도 반올림 함수 사용", { x: 0.6, y: 6.05, w: 12.1, h: 0.35, fontSize: 12, color: MUTED });
}

// ---------- 12. 총 · 궁극기 ----------
pres.addSection({ title: "전투" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "전투" });
  s.addText("총 · 스킬 · 궁극기", { placeholder: "title" });
  card(s, 0.6, 1.45, 5.95, 5.3);
  text(s, "총 (스컬식)", { x: 0.95, y: 1.7, w: 5.3, h: 0.5, fontSize: 22, bold: true, color: GOLD });
  bullets(s, [
    "드랍 아이템. 양손 2자루만 (시작 = 기본 총 2자루)",
    "오른손 = 홀수 칸, 왼손 = 짝수 칸 담당",
    "주우면 교체 창: 왼손 · 오른손 · 버리기",
    "손별 효과: 같은 총도 오른손 ≠ 왼손",
    "스킬 1개. 양손 합쳐 스킬 2개",
    "연산 슬롯은 없음 (연산은 탄에)",
  ], { x: 0.95, y: 2.4, w: 5.3, h: 4.2, fontSize: 15, paraSpaceAfter: 10 });
  card(s, 6.78, 1.45, 5.95, 5.3, PURPLE);
  text(s, "궁극기: 불릿 파티", { x: 7.13, y: 1.7, w: 5.3, h: 0.5, fontSize: 22, bold: true, color: GOLD });
  const steps = [
    "열기 게이지 3칸에서 Q. 탄을 맞힐 때만 참",
    "연기로 사라졌다가 대상 적 등 뒤에서 등장",
    "12발 동안 건카타 8자세를 홱홱 끊어 넘기며 좌우로 튐 (사미라 궁 느낌)",
    "방금 자세의 금빛 실루엣도 함께 사격, 카메라가 확 당겨짐",
  ];
  steps.forEach((t, i) => {
    const y = 2.45 + i * 0.95;
    s.addShape(pres.shapes.OVAL, { x: 7.13, y, w: 0.55, h: 0.55, fill: { color: GOLD }, line: { color: GOLD }, objectName: nm("step") });
    text(s, String(i + 1), { x: 7.13, y, w: 0.55, h: 0.55, fontSize: 16, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, t, { x: 7.85, y: y - 0.02, w: 4.7, h: 0.75, fontSize: 14, valign: "middle" });
  });
  text(s, "탄이 장전 순서대로 나가므로 빌드마다 궁극기 모습이 달라짐", { x: 7.13, y: 6.25, w: 5.3, h: 0.35, fontSize: 12, color: MUTED });
}

// ---------- 13. 드랍 ----------
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "전투" });
  s.addText("드랍 요소", { placeholder: "title" });
  const items = [
    ["탄환", GOLD, ["12칸에 배치", "자리 조건 + 특성 2개", "고유 효과 · 공격력", "연산 슬롯 0~2칸"]],
    ["연산", TEAL, ["탄의 연산 슬롯에 장착", "위 슬롯부터 계산", "±1~3 · ×2 · ×3 · ÷2 · ÷3"]],
    ["총", RED, ["양손 2자루, 주우면 교체", "공격력 · 연사 · 손별 효과", "스킬 1개"]],
  ];
  card(s, 0.6, 5.75, 12.13, 1.0, PURPLE);
  text(s, [
    { text: "런 안 빌드  ", options: { bold: true, color: GOLD } },
    { text: "탄환 배치 + 연산 장착 + 총 선택. 교체는 전투 밖에서만" },
  ], { x: 0.9, y: 5.95, w: 11.6, h: 0.6, fontSize: 16, valign: "middle" });
  items.forEach((it, i) => {
    const x = 0.6 + i * 4.13;
    card(s, x, 1.5, 3.87, 4.0);
    s.addShape(pres.shapes.OVAL, { x: x + 0.35, y: 1.85, w: 0.9, h: 0.9, fill: { color: it[1] }, line: { color: it[1] }, objectName: nm("dot") });
    text(s, String(i + 1), { x: x + 0.35, y: 1.85, w: 0.9, h: 0.9, fontSize: 24, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, it[0], { x: x + 1.45, y: 2.0, w: 2.2, h: 0.6, fontSize: 24, bold: true, color: it[1], valign: "middle" });
    bullets(s, it[2], { x: x + 0.35, y: 3.1, w: 3.25, h: 2.3, fontSize: 15, paraSpaceAfter: 10 });
  });
}

// ---------- 14. 성장 · 해금 ----------
pres.addSection({ title: "성장" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "성장" });
  s.addText("성장 · 해금", { placeholder: "title" });
  const cols = [
    ["복장", "해금 요소", ["거점에서 한 벌 골라 런 시작", "복장마다 추가 효과", "런의 출발점이 달라짐"]],
    ["기억", "개인 강화 · 세피리아식", ["판이 끝나야 찍을 수 있음, 계속 유지", "재화: 시간의 조각 (구역 보스에게서 획득)", "조각마다 노인의 삶이 조금씩 드러남"]],
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 6.13;
    card(s, x, 1.5, 5.95, 3.9);
    text(s, c[0], { x: x + 0.4, y: 1.8, w: 5.2, h: 0.55, fontSize: 22, bold: true, color: GOLD });
    text(s, c[1], { x: x + 0.4, y: 2.4, w: 5.2, h: 0.35, fontSize: 13, color: MUTED });
    bullets(s, c[2], { x: x + 0.4, y: 3.0, w: 5.3, h: 2.3, fontSize: 15, paraSpaceAfter: 10 });
  });
  card(s, 0.6, 5.65, 12.13, 1.1, PURPLE);
  text(s, [
    { text: "거점  ", options: { bold: true, color: GOLD } },
    { text: "서부 여관 2층 방. 현실의 병실이 숨어 있고, 기억을 되찾을수록 병실처럼 변함" },
  ], { x: 0.9, y: 5.85, w: 11.6, h: 0.7, fontSize: 15, valign: "middle" });
}

// ---------- 15. 다음 결정 ----------
pres.addSection({ title: "다음 결정" });
{
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: "다음 결정" });
  s.addText("다음 결정 사항", { placeholder: "title" });
  const left = [
    "구역 보스 5종 (시간의 존재) 디자인 · 패턴",
    "시간의 주인 디자인",
    "노인이 동경한 서부극 이야기 내용",
    "시간의 조각 글 구성 (노인의 삶)",
    "현실 물건 → 허상 대응",
  ];
  const right = [
    "탄환 · 총 · 복장 · 기억 목록 (1차 초안 검토)",
    "탄 특성 종류",
    "탄 등급 4단계 이름",
    "총 스킬 목록",
    "기억(개인 강화)에 스킬 포함 여부",
  ];
  [["세계관 · 연출", left], ["시스템", right]].forEach((c, i) => {
    const x = 0.6 + i * 6.13;
    card(s, x, 1.5, 5.95, 4.3);
    text(s, c[0], { x: x + 0.4, y: 1.8, w: 5.2, h: 0.5, fontSize: 20, bold: true, color: GOLD });
    bullets(s, c[1], { x: x + 0.4, y: 2.6, w: 5.3, h: 3.0, fontSize: 16, paraSpaceAfter: 14 });
  });
  text(s, "목록 초안: docs/Cat_Tang_목록_초안.md", { x: 0.6, y: 6.05, w: 12.1, h: 0.35, fontSize: 12, color: MUTED });
}

(async () => {
  await pres.writeFile({ fileName: OUT });
  await applyTheme(OUT, THEME);
  console.log("written", OUT);
})();
