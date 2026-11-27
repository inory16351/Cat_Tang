// 11:59 코스튬 기획서 Ver05 (2026-11-27 게임과 맞춤: 거울 망토 자객 잔상 최대 3명 · 1장 클리어 = 꿈2 · 대사 말줄임 ...)
// Ver04 (2026-11-24 확정): 규칙 · 8벌 · 효과 연출 · 해금 흐름 · 의상실 대화와 안내
// 실행: NODE_PATH=docs/tools/node_modules node build_costume_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 나열 구분자는 " / "
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver05";
const FILE = `1159_코스튬_기획서_20261127_정승운_${VER}.pptx`;
const C = (n) => `costume/${n}`;

// 확정 목록 (Costume.csv · Effect.csv와 같음)
const COSTUMES = [
  { id: 4001, name: "보안관 코트", img: "cos_4001.png", cond: "처음부터", cost: 0, plus: ["기본 복장"], minus: [], line: "영웅의 기본 복장" },
  { id: 4002, name: "무법자 판초", img: "cos_4002.png", cond: "1장 첫 클리어", cost: 30, plus: ["연사 속도 +15%"], minus: ["받는 피해 +15%"], line: "빠르게 쏘고 더 아프게 맞음" },
  { id: 4003, name: "장의사 정장", img: "cos_4003.png", cond: "누적 처형 50회", cost: 40, plus: ["처형할 때마다 체력 2 회복", "처형 범위 +20%"], minus: ["활성화된 탄 공격력 -10%"], line: "마지막 숨을 거두어 제 숨으로" },
  { id: 4004, name: "시계공의 앞치마", img: "cos_4004.png", cond: "3장 첫 클리어", cost: 60, plus: ["판 시작 때 무작위 연산 1개", "연산 드랍 +30%"], minus: ["탄 장전 속도 -10%"], line: "연산을 들고 시작" },
  { id: 4005, name: "거울 망토", img: "cos_4005.png", cond: "누적 극한 회피 100회", cost: 50, plus: ["극한 회피 때 자객 잔상이 범위 안 무작위 적 최대 3명을 벰", "극한 회피 판정 범위 +50%"], minus: ["최대 체력 -20%"], line: "피한 자리에 남은 그림자가 대신 벰" },
  { id: 4006, name: "모래 순례자 로브", img: "cos_4006.png", cond: "0시 이하 탄 처치 100회", cost: 50, plus: ["판정 시각 0 이하 탄이 맞힌 자리에 모래 소용돌이 (둔화 / 지속 피해)", "판 시작 때 '개기일식' 지급"], minus: ["받는 피해 +10%"], line: "0시 이하의 시간을 걷는 자" },
  { id: 4007, name: "기억의 스카프", img: "cos_4007.png", cond: "6단계 특성 2개+ 보스 처치 10회", cost: 80, plus: ["특성 6단계가 5발로 켜짐"], minus: ["최대 체력 -15%"], line: "특성 6단계가 하나 빨리" },
  { id: 4008, name: "노인의 잠옷", img: "cos_4008.png", cond: "4장 첫 클리어", cost: 120, plus: ["체력 40% 미만이면 공격력 +30%"], minus: ["최대 체력 -30%"], line: "무너질수록 날카로워짐" },
];

function build() {
  const SEC = ["코스튬 규칙", "코스튬 8벌", "효과 연출", "해금 흐름", "의상실"];
  const D = makeDeck(`11:59 코스튬 기획서 ${VER}`, `11:59 / 코스튬 기획서 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.27"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;

  D.cover("01_keyart.png", "코스튬 기획서", "해금 / 효과 / 의상실", "Ver 05");
  D.toc(has(C("lineup_a.png")) ? C("lineup_a.png") : "village/seamstress.png");

  // 01 코스튬 규칙 ------------------------------------------------------------
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "판의 출발점을 바꾸는 옷");
    picture(s, C("lineup_a.png"), 0.6, 1.6, 6.4, 2.75, "코스튬 시안", "contain");
    picture(s, C("lineup_b.png"), 0.6, 4.4, 6.4, 2.75, "코스튬 시안", "contain");
    const rules = [
      ["선택", "판 시작 전 의상실에서 한 벌 착용 / 판 도중 교체 불가"],
      ["구성", "기본 포함 8벌"],
      ["효과", "장점과 단점을 함께 / 능력치 보정 / 시작 아이템 / 규칙 변경"],
      ["해금", "업적 조건 달성 → 시간의 조각으로 구매"],
      ["의상실", "1장 첫 클리어 때 기억의 방과 함께 해금"],
    ];
    rules.forEach((r, i) => {
      const y = 1.65 + i * 1.02;
      card(s, 7.3, y, 5.4, 0.9);
      chip(s, r[0], 7.45, y + 0.27, 1.05, i === 4 ? GOLD : LAV, BG, { size: 12 });
      text(s, r[1], { x: 8.65, y: y + 0.08, w: 3.95, h: 0.75, fontSize: 13, valign: "middle" });
    });
  }

  // 02 코스튬 8벌 ------------------------------------------------------------
  pres.addSection({ title: SEC[1] });
  for (let p = 0; p < 8; p += 4) {
    const s = content(2, `코스튬 8벌 (${p / 4 + 1}/2)`);
    COSTUMES.slice(p, p + 4).forEach((c, i) => {
      const x = 0.6 + i * 3.07, y = 1.55, w = 2.9, h = 5.35;
      card(s, x, y, w, h);
      picture(s, C(c.img), x + 0.15, y + 0.15, w - 0.3, 2.3, c.name, "contain");
      text(s, c.name, { x: x + 0.15, y: y + 2.5, w: w - 0.3, h: 0.4, fontSize: 16, bold: true, color: GOLD });
      text(s, c.line, { x: x + 0.15, y: y + 2.88, w: w - 0.3, h: 0.32, fontSize: 10.5, color: MUTED });
      const runs = [];
      c.plus.forEach((t) => runs.push({ t: "+ " + t, c: TXT }));
      c.minus.forEach((t) => runs.push({ t: "- " + t, c: "E58A8A" }));
      text(s, runs.map((r, k) => ({ text: r.t, options: { color: r.c, breakLine: k < runs.length - 1 } })), { x: x + 0.15, y: y + 3.25, w: w - 0.3, h: 1.35, fontSize: 11.5, lineSpacingMultiple: 1.1 });
      D.rect(s, x + 0.15, y + 4.62, w - 0.3, 0.01, MUTED, { line: MUTED, lineW: 0.5 });
      text(s, `해금  ${c.cond}`, { x: x + 0.15, y: y + 4.68, w: w - 0.3, h: 0.36, fontSize: 10.5, color: TXT });
      text(s, c.cost > 0 ? `시간의 조각 ${c.cost}` : "무료", { x: x + 0.15, y: y + 4.98, w: w - 0.3, h: 0.3, fontSize: 10.5, color: GOLD, bold: true });
    });
  }
  {
    const s = content(2, "해금 조건");
    D.table(s, [
      ["코스튬", "조건", "세는 시점", "조각"],
      ["무법자 판초", "1장 첫 클리어", "장 보스 처치", "30"],
      ["장의사 정장", "누적 처형 50회", "보스가 아닌 적을 처형", "40"],
      ["시계공의 앞치마", "3장 첫 클리어", "장 보스 처치", "60"],
      ["거울 망토", "누적 극한 회피 100회", "극한 회피 성공", "50"],
      ["모래 순례자 로브", "판정 시각 0 이하 탄으로 처치 100회", "그 탄의 타격으로 적이 쓰러짐", "50"],
      ["기억의 스카프", "특성 6단계 2개 이상으로 보스 처치 10회", "보스가 쓰러질 때 6단계 특성 2개 이상", "80"],
      ["노인의 잠옷", "4장 첫 클리어", "장 보스 처치", "120"],
    ], 0.6, 1.7, [2.6, 4.2, 4.1, 1.2], { rowH: 0.56, size: 13 });
    text(s, "조건 달성 전에도 의상실 카드에 진행도 표시 (예: 23 / 50)", { x: 0.6, y: 6.55, w: 12.1, h: 0.35, fontSize: 12.5, color: MUTED });
  }

  {
    const s = content(2, "3D 모델");
    picture(s, C("model_compare.png"), 0.6, 1.55, 12.1, 4.47, "코스튬 3D 모델", "contain");
    text(s, "원화 A자세 → 3D 모델 → 휴머노이드 리깅 / 주인공과 같은 동작 / 양손 리볼버 / 툰 셰이더 / 머리 높이를 주인공과 맞춤", { x: 0.6, y: 6.2, w: 12.1, h: 0.4, fontSize: 13, color: MUTED });
  }

  // 03 효과 연출 ------------------------------------------------------------
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "발동할 때만 보이는 타격형 연출");
    const fx = [
      ["장의사 정장", "undertaker.gif", "처형 순간 쓰러진 적에게서 혼 줄기가 고양이에게 흡수 / 짧게 회복 번쩍임"],
      ["거울 망토", "mirror.gif", "극한 회피 순간 범위 안 무작위 적 최대 3명 곁에 자객 잔상이 하나씩 나타나 베고 흩어짐"],
      ["모래 순례자 로브", "sand.gif", "맞힌 자리에 모래 소용돌이 2초 / 안의 적 둔화 + 지속 피해"],
      ["노인의 잠옷", "pajama.gif", "체력 40% 아래로 떨어질 때 한 번 펄스 / 그 뒤 몸 둘레 희미한 푸른 잔불"],
    ];
    fx.forEach((f, i) => {
      const x = 0.6 + (i % 2) * 6.1, y = 1.6 + Math.floor(i / 2) * 2.7;
      card(s, x, y, 5.95, 2.5);
      picture(s, `costume_fx/${f[1]}`, x + 0.15, y + 0.15, 2.2, 2.2, f[0], "contain");
      text(s, f[0], { x: x + 2.55, y: y + 0.2, w: 3.25, h: 0.4, fontSize: 16, bold: true, color: GOLD });
      text(s, f[2], { x: x + 2.55, y: y + 0.7, w: 3.25, h: 1.6, fontSize: 12.5 });
    });
    text(s, "능력치만 바뀌는 옷(판초 / 앞치마 / 스카프)은 별도 연출 없음", { x: 0.6, y: 7.0 - 0.3, w: 8, h: 0.3, fontSize: 11, color: MUTED });
  }

  // 04 해금 흐름 ------------------------------------------------------------
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "1장 첫 클리어 → 의상실");
    const steps = [
      ["기절", "\"......또, 이 소리......\""],
      ["꿈 2", "불 꺼진 시계탑"],
      ["깨어남", "\"젠장... 또 이상한 꿈을 꿨군...\""],
      ["뻐꾸기 대화", "기억의 방 해금 / 시곗바늘 수리 시작"],
      ["뻐꾸기", "\"아 그러고보니 손님이 온 모양이던데.....한번 찾아가봐.\""],
      ["의상실 해금", "다리 이음 / 문 비춤 / 화살표 둘"],
    ];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.08;
      card(s, x, 1.7, 1.85, 1.9, i === 5 ? PURPLE : CARD);
      text(s, st[0], { x: x + 0.08, y: 1.8, w: 1.7, h: 0.42, fontSize: 14.5, bold: true, color: GOLD, align: "center" });
      text(s, st[1], { x: x + 0.08, y: 2.28, w: 1.7, h: 1.2, fontSize: 11, color: TXT, align: "center" });
      if (i < steps.length - 1) arrow(s, x + 1.87, 2.6, 0.2, MUTED);
    });
    bullets(s, [
      "기억의 방 / 의상실에 길 안내 화살표 동시 표시, 방문한 곳은 화살표 소멸",
      "시설 설명은 둘 중 먼저 들어간 곳에서",
      "의상실 첫 방문 = 재봉사 첫 대화 → 의상실 화면 → 코스튬 안내",
      "기절 / 깨어남 대사는 장마다 다름",
    ], { x: 0.6, y: 4.0, w: 12.1, h: 2.6, fontSize: 15, paraSpaceAfter: 8 });
  }

  // 05 의상실 ------------------------------------------------------------
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "재봉사 첫 대화");
    picture(s, C("tailor.png"), 0.6, 1.6, 4.4, 4.9, "실타래 재봉사", "contain");
    caption(s, "실타래 재봉사", 0.6, 6.55, 4.4, { align: "center" });
    const lines = [
      ["재봉사", "아 어서와. 이번이 몇번째 만남인지는 모르겠지만...."],
      ["고양이", "....난 그런 기억이 없는데"],
      ["재봉사", "아 그런 모양이군. 하지만 그런건 이 곳에선 중요하지 않지."],
      ["재봉사", "아무튼 골라봐. 무법자에게도 패션은 언제나 중요한 법이지."],
    ];
    lines.forEach((l, i) => {
      const y = 1.7 + i * 1.15;
      card(s, 5.3, y, 7.4, 1.0, l[0] === "고양이" ? PURPLE : CARD);
      chip(s, l[0], 5.45, y + 0.32, 1.0, l[0] === "고양이" ? GOLD : LAV, BG, { size: 12 });
      text(s, l[1], { x: 6.6, y: y + 0.1, w: 5.95, h: 0.8, fontSize: 14, valign: "middle" });
    });
    text(s, "의상실 문을 처음 쓸 때 / 대화가 끝나면 바로 의상실 화면", { x: 5.3, y: 6.4, w: 7.4, h: 0.35, fontSize: 12.5, color: MUTED });
  }
  {
    const s = content(5, "첫 방문 안내");
    card(s, 0.6, 1.7, 5.95, 3.4, PURPLE);
    text(s, "의상실", { x: 0.85, y: 1.85, w: 5.5, h: 0.45, fontSize: 18, bold: true, color: GOLD });
    text(s, "코스튬은 캐릭터에게 특별한 능력을 더해줍니다.\n새로운 의상을 통해 당신의 매력을 뽐내세요.", { x: 0.85, y: 2.45, w: 5.5, h: 1.2, fontSize: 15 });
    text(s, "코스튬 카드 첫 줄을 비추며 표시", { x: 0.85, y: 4.4, w: 5.5, h: 0.4, fontSize: 12.5, color: MUTED });
    card(s, 6.75, 1.7, 5.95, 3.4);
    text(s, "기억의 방", { x: 7.0, y: 1.85, w: 5.5, h: 0.45, fontSize: 18, bold: true, color: GOLD });
    text(s, "시간의 조각으로 능력 강화\n더 강해진 기억으로 재도전", { x: 7.0, y: 2.45, w: 5.5, h: 1.2, fontSize: 15 });
    text(s, "노드 트리 화면을 비추며 표시", { x: 7.0, y: 4.4, w: 5.5, h: 0.4, fontSize: 12.5, color: MUTED });
    text(s, "두 시설 중 먼저 들어간 곳에서 그 시설 안내 / 시설마다 한 번", { x: 0.6, y: 5.5, w: 12.1, h: 0.4, fontSize: 15, align: "center" });
  }

  return pres.writeFile({ fileName: path.join(OUT_DIR, FILE) });
}

(async () => {
  const o = await build();
  const py = process.env.PYTHON || "C:/Users/user/AppData/Local/Programs/Python/Python312/python.exe";
  require("child_process").execFileSync(py, [path.join(__dirname, "slash_dots.py"), o], { stdio: "inherit" });
  try { require("child_process").execFileSync(py, [path.join(__dirname, "pptx_compress.py"), o], { stdio: "inherit" }); }
  catch (e) { console.warn("compress skipped:", e.message); }
  console.log("saved", o);
})();
