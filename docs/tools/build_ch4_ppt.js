// 11:59 4장 기획서 (docs/Cat_Tang_레벨_디자인.md 챕터 4 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_ch4_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 나열 구분자는 " / "
// Ver01 (2026-11-19): 4장 시계 장치 속 확정 (질문으로 확정한 내용만). 색 = 느와르 흑백 대비 + 붉은 포인트, 적 공격 궤적은 빨간 계열
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_4장_기획서_20261119_정승운_${VER}.pptx`;
const WHITE = "FFFFFF", SILVER = "D9D9DC", STEEL = "8C8C92", INK = "16161A", BLOOD = "E0323A", PINKRED = "FF8A80";

function build() {
  const SEC = ["4장 개요", "스테이지 구성", "일반 몬스터", "중간 보스", "보스", "수치 기획"];
  const D = makeDeck(`11:59 4장 기획서 ${VER}`, `11:59 / 4장 기획서: 시계 장치 속 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.19"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const pic = (n) => (has(`ch4/${n}`) ? `ch4/${n}` : null);
  const BGIMG = pic("ch4_bg.jpg") || "05_zone1_western.png";
  const infoRows = (s, rows, fs = 12) => rows.forEach((r, i) => {
    const y = 1.6 + i * 0.88;
    card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
    text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: fs, valign: "middle", lineSpacingMultiple: 1.05 });
  });
  // 톱니 (원 + 이빨)
  const gear = (s, cx, cy, r, fill = SILVER, teeth = 12, o = {}) => {
    for (let k = 0; k < teeth; k++) {
      const a = k * 360 / teeth, rad = a * Math.PI / 180, tw = Math.max(0.06, r * 0.28), th = r * 0.22;
      const tx = cx + Math.cos(rad) * (r + th * 0.35), ty = cy + Math.sin(rad) * (r + th * 0.35);
      s.addShape(pres.shapes.RECTANGLE, { x: tx - th / 2, y: ty - tw / 2, w: th, h: tw, rotate: a, fill: { color: fill, transparency: o.tr || 0 }, line: { color: INK, width: 0.75 } });
    }
    D.circle(s, cx, cy, r, fill, { line: INK, lineW: 1, tr: o.tr || 0 });
    D.circle(s, cx, cy, r * 0.25, o.hub || STEEL, { line: INK });
  };
  // 회전 화살표
  const spin = (s, cx, cy, r, cw = true, color = GOLD) => s.addShape(pres.shapes.ARC, {
    x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, angleRange: cw ? [200, 330] : [330, 200],
    line: { color, width: 1.75, endArrowType: cw ? "triangle" : undefined, beginArrowType: cw ? undefined : "triangle" },
  });
  // 시계 문자판
  const clock = (s, cx, cy, r, label, o = {}) => {
    D.circle(s, cx, cy, r, o.fill || "F3F3F0", { line: o.line || INK, lineW: 1.5, tr: o.tr || 0 });
    if (label !== undefined) text(s, String(label), { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fontSize: o.size || 12, bold: true, color: o.color || INK, align: "center", valign: "middle" });
  };

  const cv = D.cover(BGIMG, "4장 기획서", "시계 장치 속 / 중간 보스 / 보스 상세", "Ver 01");
  cv.addNotes("공통 규칙(챕터 구조 / 전투방 / 보스 세 박자 / 수치 기준)은 공통 레벨 디자인 기획서를 따름. 방 크기는 '방 크기 규칙' 표(4장 38×32 → 44×37), 웨이브 / 보상 / 방 사이 문 규칙은 1장과 같음");
  D.toc(pic("ch4_boss.jpg") || BGIMG);

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "맞물려 돌아가는 시계탑 속");
    picture(s, BGIMG, 0.6, 1.6, 7.3, 4.1, "4장 배경");
    caption(s, "4장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "시계공의 거대한 시계탑 내부. 1장 ‘녹음’ / 2장 ‘정지’ / 3장 ‘반사’에 이어 ‘맞물림’이 모티브. 아래에서 꼭대기 문자판까지 올라가는 챕터", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["테마", "돌아가는 톱니 바닥 / 흔들리는 진자 / 태엽 계단 / 기계 틈으로 보이는 별밤"],
      ["색", "느와르 흑백 대비 / 붉은 포인트만 / 적 공격 궤적은 빨간 계열"],
      ["상인", "태엽 오르골 인형 (오르골 상자 위에서 돌며 장사)"],
      ["일반 몬스터", "태엽 병정 / 종지기 / 오르골 무희 / 초침 자객 / 진자 기사 / 시계 수리공"],
      ["중간 보스", "진자 집행관"],
      ["보스", "시침 / 분침 쌍둥이"],
    ], 11.5);
    s.addNotes("분위기 참고: 메이플스토리 파풀라투스 시계탑 (분위기만, 디자인은 독자적으로). 색은 새까만 그림자와 흰 달빛 하이라이트, 은빛 톱니. 붉은색은 리본 / 램프 / 적 눈빛 / 적 공격에만 써서 흑백 배경 위에서 위험이 바로 읽히게 함. 현실 흔적(시계공 이야기)은 나중에 스토리로");
  }

  // 02 스테이지 구성
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "시계탑 밑바닥에서 꼭대기 문자판으로");
    const flow = [
      ["스테이지 1", "태엽 창고", PURPLE], ["스테이지 2", "톱니 기관실", PURPLE], ["중간 보스", "진자 홀", "5A1A20"],
      ["스테이지 3", "종루", PURPLE], ["스테이지 4", "문자판 뒤 회랑", PURPLE], ["보스", "꼭대기 문자판", RED],
    ];
    flow.forEach((f, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.65, 1.76, 1.1, f[2]);
      text(s, f[0], { x, y: 1.72, w: 1.76, h: 0.4, fontSize: 12.5, bold: true, color: GOLD, align: "center" });
      text(s, f[1], { x, y: 2.1, w: 1.76, h: 0.5, fontSize: 13, bold: true, align: "center" });
      if (i < 5) arrow(s, x + 1.79, 2.2, 0.22, GOLD);
    });
    s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: 2.95, w: 12.1, h: 0.12, fill: { color: STEEL, transparency: 40 }, line: { color: STEEL, transparency: 100 } });
    s.addShape(pres.shapes.RECTANGLE, { x: 6.8, y: 2.95, w: 5.9, h: 0.12, fill: { color: WHITE, transparency: 10 }, line: { color: WHITE, transparency: 100 } });
    text(s, "위로 오를수록 벽이 트이고 별밤이 더 많이 보임", { x: 0.6, y: 3.12, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED, align: "center" });

    picture(s, pic("ch4_room.jpg") || BGIMG, 0.6, 3.65, 5.6, 3.15, "전투방 컨셉");
    bullets(s, [
      { t: "장소", b: true, c: GOLD },
      "태엽 창고: 태엽 / 상자가 쌓인 닫힌 밑바닥",
      "톱니 기관실: 벽마다 맞물린 거대한 톱니",
      "종루: 트인 아치 너머 별밤, 낡은 종",
      "문자판 뒤 회랑: 반투명 문자판 뒷면과 철골 통로",
      { t: "전투방", b: true, c: GOLD },
      "방 크기 38×32 → 44×37m (방 크기 규칙 표) / 모든 방 평지",
      "웨이브 2~3번 / 다음 웨이브 1초 예고 / 방마다 6종 중 3~4종",
      "상인: 방을 비우면 15% 확률로 오르골 상자가 솟음",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 11.5, paraSpaceAfter: 2 });
  }
  {
    const s = content(2, "방 기믹: 돌아가는 톱니 바닥");
    const bx = 0.6, by = 1.6, bw = 7.0, bh = 5.2;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: STEEL });
    const g1 = [bx + 2.3, by + 2.4, 1.35], g2 = [bx + 4.75, by + 2.85, 1.05];
    gear(s, g1[0], g1[1], g1[2], SILVER, 16);
    gear(s, g2[0], g2[1], g2[2], "B5B5BA", 12);
    spin(s, g1[0], g1[1], g1[2] * 0.7, true);
    spin(s, g2[0], g2[1], g2[2] * 0.7, false);
    // 맞물린 이빨 자리 (위험)
    const mx = (g1[0] + g1[2] + g2[0] - g2[2]) / 2, my = (g1[1] + g2[1]) / 2;
    D.circle(s, mx, my, 0.32, BLOOD, { tr: 35, line: BLOOD });
    text(s, "맞물림 = 피해 + 밀려남", { x: mx - 1.0, y: my + 0.4, w: 2.0, h: 0.28, fontSize: 10, bold: true, color: PINKRED, align: "center" });
    // 실려 도는 고양이
    D.cat(s, g1[0] - 0.75, g1[1] - 0.55, 0.14);
    D.line(s, g1[0] - 0.6, g1[1] - 0.75, 0.45, -0.15, GOLD, { width: 1.5, dash: "dash", end: "triangle" });
    // 축 쏘기
    D.cat(s, bx + 5.9, by + 4.6, 0.14);
    D.line(s, bx + 5.75, by + 4.45, g2[0] - (bx + 5.75) + 0.2, g2[1] - (by + 4.45) + 0.2, GOLD, { width: 2, end: "triangle" });
    text(s, "축을 쏘면 반대로", { x: bx + 5.1, y: by + 4.85, w: 1.8, h: 0.28, fontSize: 10, bold: true, color: GOLD, align: "center" });
    caption(s, "방 탑뷰 / 이웃 톱니는 반대 방향으로 돎", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 7.9, 1.6, 4.8, 5.2, PURPLE);
    text(s, "규칙", { x: 8.15, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "보스 / 중간 보스 방을 뺀 모든 방에 2~3개",
      "지름 6~9m / 초당 20° / 이웃 톱니는 반대 방향",
      "올라선 고양이 / 적이 함께 실려 돎",
      "맞물린 이빨 자리(폭 1.5m): 0.5초마다 피해 10 + 바깥으로 밀려남 (적도 같음)",
      "가운데 축(반경 0.8m)을 쏘면 회전 방향이 바뀜 / 다시 쓰려면 3초",
    ], { x: 8.15, y: 2.2, w: 4.35, h: 2.6, fontSize: 12, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.15, y: 4.85, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "가만히 서 있어도 위치가 바뀌는 방 → 조준과 이동을 같이 생각",
      "적을 맞물림 자리로 실어 보내는 역이용",
    ], { x: 8.15, y: 5.3, w: 4.35, h: 1.4, fontSize: 12, paraSpaceAfter: 3 });
  }
  {
    const s = content(2, "쏘면 움직이는 장치: 오브젝트");
    const W = 3.95, H = 3.15;
    const objs = [
      ["감긴 태엽 스프링", "쏘면 0.4초 떨다가 튕기며 폭발", "방마다 2~4개\n반경 3m 폭발\n피해 적 60 / 고양이 20", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.42, BLOOD, { tr: 75, line: BLOOD, dash: "dash" });
        [0.28, 0.2, 0.12].forEach((r) => D.circle(s, cx, cy, r, null, { line: SILVER, lineW: 2 }));
        D.mob(s, cx + 0.55, cy - 0.35); D.mob(s, cx - 0.5, cy + 0.4);
      }],
      ["굴러가는 톱니바퀴", "쏘면 맞은 방향으로 굴러가 적을 침, 벽에 한 번 튕긴 뒤 부서짐", "방마다 1~2개\n14m/초 / 피해 60", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.cat(s, bx + 0.2, cy, 0.1);
        D.line(s, bx + 0.32, cy, 0.25, 0, GOLD, { width: 1.5, end: "triangle" });
        gear(s, bx + 0.85, cy, 0.18, SILVER, 10);
        D.line(s, bx + 1.08, cy, 0.5, 0, GOLD, { width: 2, dash: "dash", end: "triangle" });
        D.mob(s, bx + 1.62, cy, 0.11);
      }],
      ["낡은 종", "쏘면 음파 링이 퍼져 적을 치고 링 안의 적 탄을 지움", "방마다 1~2개\n반경 5m / 적 피해 30\n다시 쓰려면 8초", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        [0.3, 0.55, 0.8].forEach((r) => D.circle(s, cx, cy, r, null, { line: WHITE, lineW: 1.5, dash: "dash" }));
        s.addShape(pres.shapes.TRAPEZOID, { x: cx - 0.17, y: cy - 0.2, w: 0.34, h: 0.36, fill: { color: STEEL }, line: { color: INK } });
        D.circle(s, cx + 0.6, cy - 0.35, 0.05, BLOOD); D.circle(s, cx - 0.55, cy + 0.45, 0.05, BLOOD);
        text(s, "적 탄 지움", { x: cx - 0.6, y: by + 0.05, w: 1.2, h: 0.25, fontSize: 9, bold: true, color: TEAL, align: "center" });
      }],
    ];
    objs.forEach((o, i) => {
      const x = 0.6 + i * 4.08, y = 1.6;
      card(s, x, y, W, H);
      const sz = 1.75;
      D.rect(s, x + 0.2, y + 0.2, W - 0.4, sz, FLOOR, { line: FLOOR });
      o[3](x + 0.2 + (W - 0.4 - sz) / 2, y + 0.2, sz);
      text(s, o[0], { x: x + 0.2, y: y + 2.05, w: W - 0.4, h: 0.36, fontSize: 15, bold: true, color: GOLD });
      text(s, o[1], { x: x + 0.2, y: y + 2.42, w: W - 0.4, h: 0.7, fontSize: 11.5 });
      text(s, o[2], { x: x + 0.2, y: 4.85, w: W - 0.4, h: 0.85, fontSize: 10.5, color: MUTED });
    });
    D.card(s, 0.6, 5.85, 12.1, 0.95, PURPLE);
    text(s, [{ text: "공통  ", options: { bold: true, color: GOLD } }, { text: "스프링 / 톱니바퀴 / 종은 몬스터 공격(탄 / 근접 / 폭발)에도 발동 / 엄폐물(톱니 상자 / 부서진 시계 기둥 / 태엽 통) 내구 50\n후보 자리 중 일부에만 무작위 배치, 톱니 바닥 위는 피함" }], { x: 0.85, y: 5.85, w: 11.7, h: 0.95, fontSize: 12.5, valign: "middle" });
  }

  // 03 일반 몬스터
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시계탑의 하수인 6종");
    picture(s, pic("ch4_enemies.jpg") || BGIMG, 0.6, 1.6, 12.1, 2.45, "일반 몬스터 컨셉");
    const mobs = [
      ["태엽 병정", "원거리", TEAL, "소총으로 한 발씩. 둘 이상이면 북 소리 뒤 일제 사격", ["8~10m / 3초마다", "일제 사격 3연발", "체력 156 (3발)"]],
      ["종지기", "원거리", TEAL, "종을 쳐서 음파 링을 퍼뜨림. 대시로 넘음", ["9m 유지 / 5초마다", "링 반경 10m / 피해 16", "체력 156 (3발)"]],
      ["오르골 무희", "원거리", TEAL, "제자리에서 돌며 두 갈래 나선 탄막", ["7초마다 3초 회전", "6m/초 / 피해 15", "체력 208 (4발)"]],
      ["초침 자객", "근접", RED, "매 초 ‘째깍’에 맞춰 점멸하며 다가와 찌름", ["1초마다 2.5m 점멸", "찌르기 피해 20", "체력 156 (3발)"]],
      ["진자 기사", "근접", "6A6A70", "진자 낫을 좌우로 번갈아 휘두르며 전진", ["1.2초마다 한쪽 120°", "피해 18", "체력 416 (8발)"]],
      ["시계 수리공", "서포터", "8A2A30", "도망 다니며 근처 적의 태엽을 감아 줌", ["4초마다 태엽 감기", "렌치 곡사 피해 15", "체력 156 / 방마다 1"]],
    ];
    mobs.forEach((m, i) => {
      const x = 0.6 + i * 2.04, w = 1.94;
      card(s, x, 4.2, w, 2.65);
      text(s, m[0], { x: x + 0.12, y: 4.28, w: w - 0.2, h: 0.38, fontSize: 13, bold: true, color: GOLD });
      chip(s, m[1], x + 0.12, 4.68, 0.9, m[2], m[2] === TEAL ? BG : TXT, { size: 9, h: 0.26 });
      text(s, m[3], { x: x + 0.12, y: 5.0, w: w - 0.22, h: 0.8, fontSize: 10, lineSpacingMultiple: 1.05 });
      bullets(s, m[4], { x: x + 0.12, y: 5.82, w: w - 0.18, h: 1.0, fontSize: 9, paraSpaceAfter: 1, color: MUTED });
    });
    s.addNotes("적은 몸 테두리 붉은 외곽선 + 붉은 눈빛, 등에 태엽 열쇠. 흑백 배경에서 적 공격(탄 / 링 / 예고 범위 / 휘두름)은 전부 빨간 계열");
  }
  {
    const s = content(3, "박자와 협동을 쓰는 하수인");
    const W = 3.95, H = 5.2;
    const cards = [
      ["태엽 병정", "일제 사격", ["방에 병정이 둘 이상이면 8초마다", "북 소리 예고 0.8초 → 전부 고양이 쪽으로 3연발 (0.15초 간격)", "사격 방향이 겹치지 않는 틈으로 빠짐", "혼자면 3초마다 한 발 (예고 0.5초 / 10m/초 / 피해 15)"], (bx, by, sz) => {
        const cy = by + sz / 2;
        D.cat(s, bx + sz - 0.3, cy, 0.11);
        [[0.25, 0.3], [0.2, cy - by], [0.3, sz - 0.3]].forEach(([u, v]) => {
          D.mob(s, bx + u, by + v, 0.1);
          D.line(s, bx + u + 0.12, by + v, sz - 0.6 - u, cy - by - v, BLOOD, { width: 1.5, dash: "dash", end: "triangle" });
        });
        text(s, "둥!", { x: bx + 0.05, y: by - 0.02, w: 0.5, h: 0.25, fontSize: 10, bold: true, color: PINKRED });
      }],
      ["초침 자객", "째깍 박자 점멸", ["1초마다 ‘째깍’에 맞춰 고양이 쪽으로 2.5m 점멸", "2.5m 안이면 예고 0.5초 → 전방 2.5m / 폭 1m 찌르기, 피해 20", "찌른 뒤 1초 빈틈", "박자를 읽으면 점멸 직후가 가장 안전"], (bx, by, sz) => {
        const cy = by + sz / 2;
        [0.25, 0.65, 1.05].forEach((u, k) => D.circle(s, bx + u, cy, 0.1, k === 2 ? BLOOD : STEEL, { tr: k === 2 ? 0 : 55, line: k === 2 ? INK : STEEL, dash: k === 2 ? undefined : "dash" }));
        D.rect(s, bx + 1.18, cy - 0.1, 0.4, 0.2, BLOOD, { tr: 45, line: BLOOD });
        D.cat(s, bx + sz - 0.12, cy, 0.1);
        text(s, "째깍 째깍", { x: bx + 0.1, y: by + 0.12, w: 1.3, h: 0.25, fontSize: 9, bold: true, color: MUTED });
      }],
      ["시계 수리공", "태엽 감기 → 먼저 잡기", ["10m 거리를 두고 도망 다님 (이속 3.0)", "4초마다 8m 안 적 하나의 태엽을 감음 (연결 선이 보임)", "감긴 적: 3초 동안 이속 / 공속 1.3배 + 최대 체력 15% 회복", "3초마다 렌치 곡사 (원 예고 0.8초 / 반경 1.2m / 피해 15)"], (bx, by, sz) => {
        const cy = by + sz / 2;
        D.mob(s, bx + 0.3, cy + 0.3, 0.12);
        D.mob(s, bx + 1.2, cy - 0.35, 0.12);
        D.line(s, bx + 0.4, cy + 0.22, 0.7, -0.5, WHITE, { width: 1.5, dash: "sysDot" });
        text(s, "+15%", { x: bx + 1.05, y: cy - 0.75, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: TEAL });
        D.circle(s, bx + sz - 0.35, cy + 0.4, 0.22, BLOOD, { tr: 55, line: BLOOD, dash: "dash" });
      }],
    ];
    cards.forEach((c, i) => {
      const x = 0.6 + i * 4.08, y = 1.6;
      card(s, x, y, W, H);
      const sz = 1.8;
      D.rect(s, x + 0.25, y + 0.25, W - 0.5, sz, FLOOR, { line: FLOOR });
      c[3](x + 0.25 + (W - 0.5 - sz) / 2, y + 0.25, sz);
      text(s, c[0], { x: x + 0.25, y: y + 2.2, w: W - 0.5, h: 0.32, fontSize: 12, bold: true, color: MUTED });
      text(s, c[1], { x: x + 0.25, y: y + 2.52, w: W - 0.5, h: 0.42, fontSize: 17, bold: true, color: GOLD });
      bullets(s, c[2], { x: x + 0.25, y: y + 3.0, w: W - 0.45, h: 2.1, fontSize: 11, paraSpaceAfter: 2 });
    });
  }
  {
    const s = content(3, "강화형: 스테이지 3부터 등장");
    text(s, "중간 보스 처치 후의 하수인은 전부 강화형. 더 녹슬고 태엽 / 톱니가 튀어나온 모습 + 붉은 눈빛 + 공격 하나 추가 + 체력 1.5배", { x: 0.6, y: 1.55, w: 12.1, h: 0.5, fontSize: 14, color: MUTED });
    const ups = [
      ["태엽 병정+", "착검 돌격", "6초 쿨 / 예고 0.6초 / 6m 돌진하며 찌름 / 피해 18"],
      ["종지기+", "음파 링 2겹", "링 2개를 0.6초 간격으로 / 두 번 넘어야 함"],
      ["오르골 무희+", "마무리 12방향", "회전이 끝날 때 12방향 탄 한 번 (6m/초 / 피해 15)"],
      ["초침 자객+", "뒤 찌르기", "첫 찌르기 뒤 고양이 뒤 1.5m로 점멸, 예고 0.6초 → 한 번 더"],
      ["진자 기사+", "회전 베기", "8초 쿨 / 예고 0.8초 / 반경 3m 한 바퀴 / 피해 20"],
      ["시계 수리공+", "2마리 감기", "한 번에 2마리 감기 / 죽을 때 8m 안 적 전부 감기"],
    ];
    ups.forEach((u, i) => {
      const y = 2.15 + i * 0.79;
      card(s, 0.6, y, 12.1, 0.7, i % 2 ? CARD : PURPLE);
      text(s, u[0], { x: 0.85, y, w: 2.6, h: 0.7, fontSize: 13, bold: true, color: MUTED, valign: "middle" });
      text(s, u[1], { x: 3.45, y, w: 2.4, h: 0.7, fontSize: 16, bold: true, color: GOLD, valign: "middle" });
      text(s, u[2], { x: 5.9, y, w: 6.6, h: 0.7, fontSize: 12.5, valign: "middle" });
    });
  }

  // 04 중간 보스
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "진자 집행관");
    picture(s, pic("ch4_midboss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "중간 보스 컨셉");
    caption(s, "중간 보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "두 팔이 거대한 진자 칼날인 태엽 기사. 가슴에 흔들리는 추, 얼굴은 은빛 문자판 가면에 붉은 눈빛. 박자와 시각으로 판결을 내림", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["등장", "스테이지 2 마지막 방 뒤에 이어 붙은 진자 홀 / 승리 시 뒷문 너머로 스테이지 3 연결"],
      ["보스맵", "28×24m / 천장에 거대한 진자 / 양옆 벽에 멈춘 톱니 / 장애물 없음"],
      ["이동", "진자처럼 좌우로 흔들리듯 걸으며 다가옴 (이속 3.5m/초)"],
      ["체력", "5400 (목표 처치 시간 60초)"],
      ["페이즈", "1페이즈 / 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["고유 기믹", "틱-톡 박자 / 판결의 수 (쿨 스킬) / 끊어진 추 재기 (66 / 33%)"],
    ], 11.5);
  }
  {
    const s = content(4, "고유 기믹 1: 틱-톡 박자");
    const bx = 0.6, by = 1.6, bw = 7.3, bh = 3.6;
    D.rect(s, bx, by, bw / 2, bh, BLOOD, { tr: 60, line: BLOOD });
    D.rect(s, bx + bw / 2, by, bw / 2, bh, FLOOR, { line: STEEL });
    text(s, "틱", { x: bx + 0.2, y: by + 0.15, w: 1.0, h: 0.5, fontSize: 24, bold: true, color: WHITE });
    text(s, "톡", { x: bx + bw - 1.2, y: by + 0.15, w: 1.0, h: 0.5, fontSize: 24, bold: true, color: MUTED, align: "right" });
    D.line(s, bx + bw / 2, by, 0, 0.9, SILVER, { width: 2 });
    D.circle(s, bx + bw / 2 - 0.9, by + 1.25, 0.32, SILVER, { line: INK });
    D.line(s, bx + bw / 2, by + 0.1, -0.85, 1.0, SILVER, { width: 3 });
    D.boss(s, bx + bw / 2, by + bh / 2 + 0.6, 0.2);
    D.cat(s, bx + bw * 0.78, by + bh - 0.6, 0.15);
    caption(s, "방 탑뷰 / 진자가 쓸 쪽 바닥이 0.6초 붉게 예고", bx, by + bh - 0.32, bw, { align: "center" });
    // 박자표
    const beats = ["L", "R", "L", "R", "L", "R", "R", "L"], gaps = [1.2, 1.1, 1.0, 0.9, 0.85, 0.8, 0.7];
    beats.forEach((b, i) => {
      const x = bx + 0.1 + i * 0.9;
      chip(s, b === "L" ? "틱" : "톡", x, 5.4, 0.75, i === 6 ? BLOOD : b === "L" ? STEEL : CARD, TXT, { size: 11, h: 0.36 });
      if (i < 7) text(s, gaps[i] + "", { x: x + 0.55, y: 5.82, w: 0.5, h: 0.25, fontSize: 9, color: MUTED, align: "center" });
    });
    text(s, "8번 흔들림 / 간격 1.2초 → 0.7초 / 7번째 엇박 (같은 쪽 두 번)", { x: bx, y: 6.15, w: bw, h: 0.3, fontSize: 11.5, color: MUTED });

    card(s, 8.2, 1.6, 4.5, 5.25, PURPLE);
    text(s, "규칙", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "전투 12초 뒤 첫 발동 / 이후 25초마다",
      "천장 진자가 내려와 방을 왼쪽(틱) / 오른쪽(톡)으로 번갈아 쓸어 냄",
      "쓸 쪽 바닥 0.6초 붉게 예고 → 방 절반 피해 22",
      "진행 동안 집행관은 제자리에서 칼날로 박자를 맞춤 (공격 가능)",
      "다른 기믹 진행 중엔 쿨이 멈춤",
    ], { x: 8.45, y: 2.2, w: 4.0, h: 3.0, fontSize: 12, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.45, y: 5.25, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "리듬을 타며 좌우로 옮겨 다니면서도 딜을 넣는 구간",
      "엇박 한 번으로 습관적인 이동을 끊음",
    ], { x: 8.45, y: 5.7, w: 4.0, h: 1.1, fontSize: 12, paraSpaceAfter: 2 });
  }
  {
    const s = content(4, "고유 기믹 2: 판결의 수");
    const steps = [
      ["① 판결", "고양이 머리 위에\n판결 숫자 n (2~11)", STEEL],
      ["② 카운트다운 5초", "진자가 1초마다 똑딱\n5 → 0", GOLD],
      ["③ 집행", "0일 때 발사 시각 = n이면 무죄\n아니면 유죄", BLOOD],
    ];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.52;
      card(s, x, 1.65, 2.3, 1.85, i === 1 ? "3A3A40" : CARD);
      text(s, st[0], { x: x + 0.2, y: 1.78, w: 2.0, h: 0.4, fontSize: 15, bold: true, color: GOLD });
      text(s, st[1], { x: x + 0.2, y: 2.25, w: 2.0, h: 1.15, fontSize: 11.5, lineSpacingMultiple: 1.15 });
      if (i < 2) arrow(s, x + 2.32, 2.57, 0.18, GOLD);
    });
    const bx = 0.6, by = 3.75, bw = 7.3, bh = 3.05;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: FLOOR });
    D.cat(s, bx + 2.0, by + 1.9, 0.18);
    D.rect(s, bx + 1.75, by + 0.85, 0.5, 0.5, "F3F3F0", { line: INK });
    text(s, "7", { x: bx + 1.75, y: by + 0.85, w: 0.5, h: 0.5, fontSize: 16, bold: true, color: INK, align: "center", valign: "middle" });
    clock(s, bx + 3.4, by + 1.9, 0.35, 7, { size: 14 });
    text(s, "내 발사 시각 7 → 무죄", { x: bx + 2.75, y: by + 2.35, w: 1.4, h: 0.45, fontSize: 9.5, color: TEAL, align: "center" });
    D.boss(s, bx + 5.9, by + 1.5, 0.2);
    D.rect(s, bx + 5.55, by + 0.25, 0.7, 0.18, SILVER, { line: INK });
    D.line(s, bx + 5.9, by + 0.45, 0, 0.75, SILVER, { width: 2, end: "triangle" });
    text(s, "단두대가 집행관 쪽으로 → 2초 기절", { x: bx + 4.6, y: by + 2.0, w: 2.6, h: 0.3, fontSize: 10, bold: true, color: GOLD, align: "center" });
    caption(s, "숫자 = 맞춰야 하는 시각 (1장 현상금 표식의 반대 발전형)", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 8.2, 1.65, 4.5, 5.2, PURPLE);
    text(s, "판정", { x: 8.45, y: 1.8, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "전투 25초 뒤 첫 발동 / 이후 25초마다",
      "틱-톡 박자와 최소 8초 떨어뜨림",
      "무죄: 단두대가 집행관 쪽으로 떨어져 2초 기절",
      "유죄: 고양이 자리에 단두대 진자, 최대 체력 25% 피해",
      "발사 시각 = 다음에 쏠 실린더 칸 번호 / 쏘면 한 칸 / 장전하면 첫 칸",
    ], { x: 8.45, y: 2.3, w: 4.0, h: 3.0, fontSize: 12, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.45, y: 5.3, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "5초 동안 몇 발 쏘고 언제 장전할지 계산하는 짧은 퍼즐",
    ], { x: 8.45, y: 5.75, w: 4.0, h: 1.0, fontSize: 12, paraSpaceAfter: 2 });
  }
  {
    const s = content(4, "고유 기믹 3: 끊어진 추 재기");
    const bx = 0.6, by = 1.6, bw = 7.3, bh = 3.3;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: STEEL });
    [[1.6, 1.4, 4], [3.8, 2.2, 9], [5.8, 1.2, 2]].forEach(([u, v, n], k) => {
      const cx = bx + u, cy = by + v;
      D.circle(s, cx, cy, 0.75 + k * 0.1, BLOOD, { tr: 75, line: BLOOD, dash: "dash" });
      D.line(s, cx, by + 0.05, 0, v - 0.3, STEEL, { width: 1.5, dash: "sysDot" });
      clock(s, cx, cy, 0.25, n, { size: 11, fill: SILVER });
    });
    D.cat(s, bx + 2.8, by + bh - 0.55, 0.14);
    caption(s, "추 3개 / 흔들림 반경 2m → 3초마다 +1m (최대 6m)", bx, by + bh - 0.32, bw, { align: "center" });
    bullets(s, [
      "체력 66 / 33%에 한 번씩 확정 (진행 중인 패턴이 끝나면 바로 발동)",
      "추마다 서로 다른 시각 1~12 / 추는 자기 시각 탄에만 멈춤, 다른 탄은 튕김",
      "추에 닿으면 피해 22",
      "12초 뒤 남은 추가 끝까지 흔들려 홀을 쓸음: 남은 추 하나당 최대 체력 10%",
      "다 멈추면 집행관 3초 기절 / 진행 동안 집행관은 멈추고 무적",
    ], { x: 0.6, y: 5.05, w: 7.3, h: 1.8, fontSize: 12, paraSpaceAfter: 2 });

    card(s, 8.2, 1.6, 4.5, 5.25, PURPLE);
    text(s, "기획의도", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "체력 구간마다 홀 전체를 쓰는 큰 기믹",
      "점점 커지는 흔들림 사이를 피하며 순서를 정해 시각을 맞춤",
      "쏘는 순서를 바늘 순서대로 고르면 장전 없이 끝남 → 판단 보상",
    ], { x: 8.45, y: 2.2, w: 4.0, h: 2.4, fontSize: 12.5, paraSpaceAfter: 4 });
    text(s, "기믹 세 가지 정리", { x: 8.45, y: 4.65, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "틱-톡: 위치 (어느 쪽에 설지)",
      "판결의 수: 시각 하나 맞추기",
      "끊어진 추: 시각 여러 개 + 회피",
    ], { x: 8.45, y: 5.1, w: 4.0, h: 1.6, fontSize: 12.5, paraSpaceAfter: 3 });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "진자 십자 베기", "두 칼날을 X자로 교차해 전방 부채꼴을 벰", "예고 0.7초 / 100° / 4m / 피해 24", (bx, by, sz) => {
      const cx = bx + 0.35, cy = by + sz / 2;
      D.pie(s, cx, cy, sz - 0.5, 310, 50, BLOOD, { tr: 50 });
      D.line(s, cx + 0.3, cy - 0.35, 0.7, 0.7, WHITE, { width: 2 });
      D.line(s, cx + 0.3, cy + 0.35, 0.7, -0.7, WHITE, { width: 2 });
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "단두대 내려찍기", "고양이 자리로 뛰어올라 내려찍고 앞뒤로 충격파", "원 예고 0.8초 / 반경 1.5m / 피해 26\n앞뒤 직선 폭 1.5m / 8m / 피해 22", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.rect(s, cx - 0.15, by + 0.08, 0.3, sz - 0.16, BLOOD, { tr: 65, line: BLOOD });
      D.circle(s, cx, cy, 0.35, BLOOD, { tr: 40, line: BLOOD });
      D.cat(s, cx + 0.5, cy + 0.2, 0.09);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "사슬 추 끌어오기", "사슬 추를 직선으로 던져 맞으면 앞으로 끌어옴", "조준선 0.8초 / 14m / 16m/초 / 피해 22\n빗나가면 추를 감는 1초 빈틈", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.boss(s, bx + 0.25, cy, 0.14);
      D.line(s, bx + 0.4, cy, sz - 0.7, 0, BLOOD, { width: 2, dash: "dash" });
      D.circle(s, bx + sz - 0.25, cy, 0.12, SILVER, { line: INK });
      D.line(s, bx + sz - 0.45, cy + 0.25, -0.6, 0, GOLD, { width: 1.5, end: "triangle" });
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "톱니 원반 던지기", "톱니 원반 3개를 부채꼴로 던짐, 벽에 한 번 튕김", "40° 3개 / 9m/초 / 피해 22", (bx, by, sz) => {
      const cx = bx + 0.3, cy = by + sz / 2;
      [-20, 0, 20].forEach((a) => { const r = a * Math.PI / 180; D.line(s, cx + 0.15, cy, Math.cos(r) * (sz - 0.6), Math.sin(r) * (sz - 0.6), BLOOD, { width: 1.5, dash: "dash", end: "triangle" }); gear(s, cx + 0.15 + Math.cos(r) * 0.75, cy + Math.sin(r) * 0.75, 0.08, SILVER, 8); });
      D.boss(s, cx, cy, 0.14);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "초침 자객 호출", "초침 자객을 불러냄", "2명 소환 / 필드 최대 4명\n필드 몬스터 2명 이하일 때만", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.45, -0.4], [0.45, -0.35]].forEach(([dx, dy]) => { D.circle(s, cx + dx, cy + dy, 0.17, BLOOD, { tr: 70, line: BLOOD, dash: "dash" }); D.mob(s, cx + dx, cy + dy); });
      D.boss(s, cx, cy + 0.25, 0.15);
      text(s, "+2", { x: cx - 0.25, y: by + 0.08, w: 0.5, h: 0.3, fontSize: 12, bold: true, color: GOLD, align: "center" });
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 5m 안",
      "거리에 맞는 패턴 + 거리 무관 패턴을 같은 확률로 추첨",
      "직전 패턴은 제외",
      "틱-톡 / 판결의 수 25초 쿨 / 끊어진 추는 체력 66 / 33%",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 11.5, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
    s.addNotes("전용 모션: 걷기(흔들림) / 십자 베기 / 내려찍기 / 사슬 던지기");
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "시침 / 분침 쌍둥이");
    picture(s, pic("ch4_boss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "문자판에서 깨어난 두 바늘 기사. 시침 = 크고 느린 은빛 대검 기사 / 분침 = 가늘고 빠른 붉은 세검 기사", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["보스맵", "시계탑 꼭대기 문자판 / 원형 지름 40m / 밖은 별밤 허공 / 바닥 = 12칸 문자판"],
      ["이동", "시침은 느리게 따라옴 (2.4m/초) / 분침은 둘레를 빠르게 돎 (4.2m/초)"],
      ["체력", "따로: 시침 5000 / 분침 4180 (합 9180, 목표 90초)"],
      ["페이즈", "한쪽이 쓰러지면 남은 쪽 분노 (2페이즈)"],
      ["상시 탄막", "별빛 낙하 (2초마다 / 2페이즈 1.5초마다)"],
      ["고유 기믹", "3종 무작위 반복 (쿨 20초)"],
    ], 11.5);
    s.addNotes("전용 모션(예정): 시침 걷기 / 내려찍기 / 회전, 분침 질주 / 찌르기 연타 / 돌진");
  }
  {
    const s = content(5, "두 바늘, 따로 쓰러뜨리기");
    card(s, 0.6, 1.6, 6.0, 5.25, PURPLE);
    text(s, "2페이즈: 남은 바늘의 분노", { x: 0.85, y: 1.75, w: 5.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const ox = 1.9, oy = 3.45;
    clock(s, ox, oy, 0.85, undefined, { fill: "2A2A30", line: SILVER });
    D.line(s, ox, oy, 0, -0.6, SILVER, { width: 6 });
    D.line(s, ox, oy, 0.62, 0.3, BLOOD, { width: 3 });
    text(s, "시침", { x: ox - 0.6, y: oy - 1.18, w: 1.2, h: 0.3, fontSize: 11, bold: true, color: SILVER, align: "center" });
    text(s, "분침", { x: ox + 0.5, y: oy + 0.3, w: 0.9, h: 0.3, fontSize: 11, bold: true, color: PINKRED });
    bullets(s, [
      "체력은 따로, 한쪽이 쓰러지면 남은 쪽이 쓰러진 바늘을 흡수",
      "이속 / 패턴 간격 1.2배",
      "쓰러진 쪽 패턴 하나를 물려받음: 남은 쪽이 시침이면 ‘분침 돌진’(2번), 분침이면 ‘시각 낙인’",
    ], { x: 3.0, y: 2.25, w: 3.45, h: 2.3, fontSize: 11.5, paraSpaceAfter: 3 });
    bullets(s, [
      { t: "먼저 잡을 쪽 선택이 전략", b: true, c: GOLD },
      "시침 먼저 → 빠른 분침이 강해짐",
      "분침 먼저 → 느리지만 한 방이 큰 시침이 강해짐",
    ], { x: 0.85, y: 4.75, w: 5.5, h: 1.9, fontSize: 12.5, paraSpaceAfter: 3 });

    card(s, 6.85, 1.6, 5.85, 5.25);
    text(s, "패턴 박자", { x: 7.1, y: 1.75, w: 5.3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const tl = [["시침", 0.0, 1.6, SILVER], ["분침", 1.75, 1.1, BLOOD], ["시침", 3.0, 1.5, SILVER], ["분침", 4.6, 0.9, BLOOD]];
    tl.forEach(([n, x0, w, c]) => { D.rect(s, 7.1 + x0, 2.4, w, 0.42, c, { tr: 25, line: INK }); text(s, n, { x: 7.1 + x0, y: 2.4, w, h: 0.42, fontSize: 10.5, bold: true, color: INK, align: "center", valign: "middle" }); });
    D.rect(s, 7.1 + 3.0, 2.95, 0.9, 0.32, BLOOD, { tr: 55, line: BLOOD, dash: "dash" });
    text(s, "약한 패턴 겹침 30%", { x: 7.1 + 3.95, y: 2.95, w: 1.9, h: 0.32, fontSize: 9.5, color: PINKRED, valign: "middle" });
    bullets(s, [
      "기본은 번갈아: 한쪽 패턴이 끝나야 다른 쪽 예고 시작 (기본 조작만으로 노히트 가능)",
      "협공 패턴(열두 시 협공)은 하나로 취급",
      "가끔 동시에: 둘 다 살아 있고 한쪽 체력 50% 이하면, 상대 예고가 시작될 때 30% 확률로 약한 패턴(시각 낙인 / 바늘 부채)을 겹침",
      "겹칠 때 두 예고 범위는 서로 겹치지 않게",
    ], { x: 7.1, y: 3.45, w: 5.4, h: 3.3, fontSize: 12, paraSpaceAfter: 4 });
  }
  {
    const s = content(5, "고유 기믹 3종: 쿨 20초 무작위");
    const rules = [
      ["쿨 20초", "전투 20초 뒤 첫 발동 / 3종 중 무작위 (직전 기믹 제외)"],
      ["기믹 우선", "쿨이 다 돌면 진행 중인 패턴이 끝나는 대로 바로 기믹"],
      ["쿨 정지", "기믹 진행 동안은 쿨이 돌지 않음"],
      ["기믹 중", "쌍둥이 일반 패턴 없음 / 별빛 낙하는 계속"],
    ];
    rules.forEach((r, i) => {
      const x = 0.6 + i * 3.06;
      card(s, x, 1.6, 2.92, 1.25, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: x + 0.2, y: 1.68, w: 2.6, h: 0.38, fontSize: 15, bold: true, color: GOLD });
      text(s, r[1], { x: x + 0.2, y: 2.06, w: 2.6, h: 0.75, fontSize: 11.5 });
    });
    const g = [
      ["① 순서대로 연속 명중", "쌍둥이가 가장자리로 물러나 멈추고 문자판 둘레 숫자 3개가 차례로 빛남. 12초 안에 빛난 순서대로 각 숫자 시각 탄으로 표적 명중 (다음 표적만 열림, 틀린 탄은 튕김). 내림차순이 섞이면 중간에 장전 필요", "2페이즈 숫자 4개 / 성공: 4초 기절 / 실패: 남은 표적마다 바늘 낙하 최대 체력 15%", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        clock(s, cx, cy, 0.62, undefined, { fill: "2A2A30", line: SILVER });
        [[3, 0], [7, 1], [11, 2]].forEach(([n, k]) => { const a = (n * 30 - 90) * Math.PI / 180; D.circle(s, cx + Math.cos(a) * 0.62, cy + Math.sin(a) * 0.62, 0.15, k === 0 ? GOLD : SILVER, { line: INK }); text(s, String(k + 1), { x: cx + Math.cos(a) * 0.62 - 0.15, y: cy + Math.sin(a) * 0.62 - 0.15, w: 0.3, h: 0.3, fontSize: 9, bold: true, color: INK, align: "center", valign: "middle" }); });
      }],
      ["② 뒤섞인 문자판", "바닥 문자판 숫자 12칸이 2초 동안 뒤섞여 자리가 바뀜. 6초 뒤 그 순간 내 발사 시각 숫자가 적힌 칸 위에 서 있어야 안전 (시각 + 위치를 함께 읽기). 쌍둥이는 둘레를 돌며 느린 바늘 탄", "바늘 탄 6m/초 / 피해 15 / 실패: 최대 체력 35%", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        [5, 11, 2, 8, 12, 3].forEach((n, k) => D.pie(s, cx, cy, 0.68, k * 60, k * 60 + 60, k % 2 ? "3A3A40" : "F3F3F0", { line: INK }));
        D.cat(s, cx + 0.3, cy - 0.15, 0.08);
      }],
      ["③ 멈춘 톱니 돌리기", "경기장 둘레(고양이에서 6~14m)에 톱니 3개가 내려옴, 톱니마다 서로 다른 시각 1~12. 톱니는 자기 시각 탄에만 돌아감 (다른 탄은 튕김)", "10초 / 다 돌리면 4초 기절 / 남은 톱니 하나당 최대 체력 12% (최대 36%)", (bx, by, sz) => {
        [[0.4, 0.45, 2], [1.15, 0.4, 9], [0.8, 1.15, 5]].forEach(([u, v, n]) => { gear(s, bx + u, by + v, 0.22, SILVER, 10); text(s, String(n), { x: bx + u - 0.2, y: by + v - 0.2, w: 0.4, h: 0.4, fontSize: 9, bold: true, color: INK, align: "center", valign: "middle" }); });
      }],
    ];
    g.forEach((it, i) => {
      const x = 0.6 + i * 4.08, y = 3.0;
      card(s, x, y, 3.95, 3.85);
      const sz = 1.55;
      D.rect(s, x + (3.95 - sz) / 2, y + 0.15, sz, sz, FLOOR, { line: FLOOR });
      it[3](x + (3.95 - sz) / 2, y + 0.15, sz);
      text(s, it[0], { x: x + 0.2, y: y + 1.78, w: 3.6, h: 0.36, fontSize: 14, bold: true, color: GOLD });
      text(s, it[1], { x: x + 0.2, y: y + 2.14, w: 3.6, h: 1.2, fontSize: 9.5, lineSpacingMultiple: 1.05 });
      text(s, it[2], { x: x + 0.2, y: y + 3.33, w: 3.6, h: 0.48, fontSize: 9, color: MUTED });
    });
    s.addNotes("발사 시각 = 바늘 위치(다음에 쏠 실린더 칸 번호), 쏘면 한 칸 이동 / 장전하면 첫 칸으로 복귀. 1장(시각 맞추기) → 2장(+약점 명중) → 3장(+반전 읽기) → 4장(+순서 / 장전 판단)의 발전형");
  }
  {
    const s = content(5, "보스 패턴: 시침 (느림 / 큰 범위)");
    const W = 5.98, H = 2.45, X = [0.6, 6.72], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "시침 내려찍기", "대검을 앞으로 내려찍어 긴 직선을 가름. 끝에 대검이 박혀 빈틈", "예고 0.9초 / 직선 12m / 폭 2.5m / 피해 32\n끝에서 1.2초 빈틈", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + 0.35, cy - 0.22, sz - 0.4, 0.44, BLOOD, { tr: 50, line: BLOOD });
      D.boss(s, bx + 0.2, cy, 0.15);
      D.cat(s, bx + sz * 0.7, cy - 0.45, 0.08);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "대검 회전", "대검을 들고 한 바퀴 돔", "예고 0.8초 / 반경 4.5m / 피해 30", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.42, BLOOD, { tr: 55, line: BLOOD });
      spin(s, cx, cy, sz * 0.3, true, WHITE);
      D.boss(s, cx, cy, 0.14);
      D.cat(s, cx + sz * 0.47, cy, 0.08);
    }, "가까울 때");
    D.patternCard(s, X[0], Y[1], W, H, "시각 낙인", "고양이 둘레에 원 3개가 차례로 예고된 뒤 폭발", "원 3개 반경 2m / 0.4초 간격 / 예고 1초\n피해 28", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.4, -0.3], [0.35, -0.2], [0, 0.4]].forEach(([dx, dy], k) => { D.circle(s, cx + dx, cy + dy, 0.3, BLOOD, { tr: 40 + k * 15, line: BLOOD }); text(s, String(k + 1), { x: cx + dx - 0.15, y: cy + dy - 0.15, w: 0.3, h: 0.3, fontSize: 10, bold: true, color: WHITE, align: "center", valign: "middle" }); });
      D.cat(s, cx + 0.05, cy - 0.05, 0.08);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "열두 시 협공", "쌍둥이가 12시 / 6시 자리로 순간 이동한 뒤 고양이를 지나는 X자로 동시에 돌진", "X자 두 줄 예고 1.2초 / 피해 34\n둘 다 살아 있을 때만", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      s.addShape(pres.shapes.RECTANGLE, { x: bx + 0.1, y: cy - 0.15, w: sz - 0.2, h: 0.3, rotate: 40, fill: { color: BLOOD, transparency: 55 }, line: { color: BLOOD } });
      s.addShape(pres.shapes.RECTANGLE, { x: bx + 0.1, y: cy - 0.15, w: sz - 0.2, h: 0.3, rotate: -40, fill: { color: BLOOD, transparency: 55 }, line: { color: BLOOD } });
      D.boss(s, cx, by + 0.15, 0.12); D.boss(s, cx, by + sz - 0.15, 0.12);
      D.cat(s, cx, cy, 0.08);
    }, "거리 무관");
    D.legend(s, 0.6, 6.74);
    s.addNotes("시침 '가까움' = 6m 안 / 체력 5000 / 이속 2.4m/초");
  }
  {
    const s = content(5, "보스 패턴: 분침 (빠름 / 연타) + 별빛 낙하");
    const W = 5.98, H = 2.45, X = [0.6, 6.72], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "60초 찌르기", "세검으로 앞을 빠르게 연타", "예고 0.5초 / 전방 3m / 폭 1.2m\n1.2초 동안 0.2초마다 피해 8 (최대 6번)", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + 0.35, cy - 0.15, 1.0, 0.3, BLOOD, { tr: 45, line: BLOOD });
      [0, 1, 2].forEach((k) => D.line(s, bx + 0.4 + k * 0.1, cy - 0.08 + k * 0.08, 0.9, 0, WHITE, { width: 1 }));
      D.boss(s, bx + 0.2, cy, 0.12);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "분침 돌진", "고양이 쪽 직선 예고 뒤 돌진, 돌진마다 다시 조준해 3번", "예고 0.6초 / 20m/초 / 3번 / 피해 27", (bx, by, sz) => {
      const pts = [[0.2, 1.3], [0.9, 0.4], [1.4, 1.2], [sz - 0.15, 0.5]];
      for (let k = 0; k < 3; k++) D.line(s, bx + pts[k][0], by + pts[k][1], pts[k + 1][0] - pts[k][0], pts[k + 1][1] - pts[k][1], BLOOD, { width: 2.5, end: "triangle" });
      D.boss(s, bx + 0.2, by + 1.3, 0.11);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "바늘 부채", "바늘 탄 7발을 부채꼴로 던짐", "50° / 7발 / 12m/초 / 발당 피해 18", (bx, by, sz) => {
      const cx = bx + 0.3, cy = by + sz / 2;
      for (let k = -3; k <= 3; k++) { const a = k * 8.3 * Math.PI / 180; D.line(s, cx + 0.15, cy, Math.cos(a) * (sz - 0.6), Math.sin(a) * (sz - 0.6), BLOOD, { width: 1, end: "triangle" }); }
      D.boss(s, cx, cy, 0.13);
    }, "멀 때");
    card(s, X[1], Y[1], W, H, PURPLE);
    text(s, "상시 탄막: 별빛 낙하", { x: X[1] + 0.25, y: Y[1] + 0.15, w: 5, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    {
      const cx = X[1] + 1.2, cy = Y[1] + 1.4;
      D.circle(s, cx, cy, 0.8, null, { line: STEEL, dash: "dash" });
      [[-0.3, -0.35], [0.4, 0.1], [-0.2, 0.45]].forEach(([dx, dy]) => D.circle(s, cx + dx, cy + dy, 0.2, BLOOD, { tr: 45, line: BLOOD }));
      D.cat(s, cx, cy, 0.08);
    }
    bullets(s, [
      "2초마다 별 하나가 고양이 주변 6m 안 무작위 자리에 떨어짐",
      "원 예고 1초 / 반경 1.2m / 피해 15",
      "2페이즈 1.5초마다",
    ], { x: X[1] + 2.3, y: Y[1] + 0.6, w: 3.5, h: 1.7, fontSize: 11.5, paraSpaceAfter: 3 });
    D.legend(s, 0.6, 6.74);
    s.addNotes("분침 '가까움' = 5m 안 / 체력 4180 / 이속 4.2m/초");
  }

  // 06 수치
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "4장 수치 정리");
    D.table(s, [
      ["대상", "체력", "이속", "피해", "비고"],
      ["태엽 병정", "156", "2.6m/초", "15", "8~10m / 둘 이상이면 일제 사격"],
      ["종지기", "156", "2.4m/초", "16", "음파 링 5초마다 / 반경 10m"],
      ["오르골 무희", "208", "2.2m/초", "15", "7초마다 3초 회전 나선 탄"],
      ["초침 자객", "156", "점멸", "20", "1초마다 2.5m 점멸"],
      ["진자 기사", "416", "2.8m/초", "18", "1.2초마다 한쪽 120°"],
      ["시계 수리공", "156", "3.0m/초", "15", "태엽 감기 4초마다 / 방마다 1"],
      ["강화형", "기본 ×1.5", "-", "15~20", "공격 하나 추가"],
      ["진자 집행관", "5400", "3.5m/초", "22~26", "목표 60초 / 진자 홀 28×24m"],
      ["시침 / 분침", "5000 / 4180", "2.4 / 4.2", "18~34", "목표 90초 / 원형 지름 40m"],
    ], 0.6, 1.55, [2.1, 1.5, 1.25, 1.0, 3.55], { rowH: 0.36, size: 10.5 });
    const cards = [
      ["피해 기준", "3장보다 약 25% 상향\n일반 15~22 / 중간 보스 22~27\n보스 27~37 (기믹은 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 20 × 명중률 60%\n중간 보스 시점 ×7.5 / 60초\n보스 시점 ×8.5 / 90초"],
      ["전투방", "웨이브당 적: 스테이지 1은 7~9마리\n→ 스테이지 4는 9~12마리\n톱니 바닥 2~3개 / 초당 20°"],
      ["오브젝트", "태엽 스프링 반경 3m / 적 60 · 고양이 20\n톱니바퀴 14m/초 / 피해 60\n종 반경 5m / 적 30 / 쿨 8초"],
    ];
    cards.forEach((c, i) => {
      const y = 1.6 + i * 1.32;
      card(s, 10.2, y, 2.5, 1.2, i % 2 ? CARD : PURPLE);
      text(s, c[0], { x: 10.35, y: y + 0.08, w: 2.3, h: 0.3, fontSize: 12.5, bold: true, color: GOLD });
      text(s, c[1], { x: 10.35, y: y + 0.38, w: 2.3, h: 0.8, fontSize: 9.5, lineSpacingMultiple: 1.1 });
    });
    text(s, "일반 몬스터 체력 = 4장 시작 화력(×6.5, 기본 탄 한 발 52) 기준으로 앞 챕터와 비슷한 발 수", { x: 0.6, y: 6.12, w: 9.3, h: 0.32, fontSize: 11.5, color: MUTED });
    text(s, "중간 보스 5400 = 20 × 7.5 × 60초 × 0.6  /  보스 9180 = 20 × 8.5 × 90초 × 0.6", { x: 0.6, y: 6.48, w: 9.3, h: 0.36, fontSize: 13, bold: true });
  }
  {
    const s = content(6, "공통 변경: 방 크기 / 색 규칙");
    card(s, 0.6, 1.6, 7.3, 5.25, PURPLE);
    text(s, "방 크기 규칙 (모든 챕터)", { x: 0.85, y: 1.75, w: 6.8, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    D.table(s, [
      ["장", "스테이지 1", "스테이지 2", "스테이지 3", "스테이지 4"],
      ["1장", "28×24", "34×29", "36×30", "38×31"],
      ["2장", "34×29", "36×30", "38×32", "40×33"],
      ["3장", "36×30", "38×32", "40×33", "42×35"],
      ["4장", "38×32", "40×33", "42×35", "44×37"],
    ], 0.85, 2.25, [1.0, 1.45, 1.45, 1.45, 1.45], { rowH: 0.4, size: 12 });
    bullets(s, [
      "웨이브 적 수가 늘어나는 만큼 일반 전투방도 장 / 스테이지마다 커짐 (예전: 전부 28×24m)",
      "중간 보스 방 / 보스 방은 각 보스 경기장 값 그대로",
      "오브젝트 후보 자리 / 방 모양은 방 크기에 비례해 늘림",
    ], { x: 0.85, y: 5.05, w: 6.85, h: 1.7, fontSize: 12, paraSpaceAfter: 3 });

    card(s, 8.2, 1.6, 4.5, 5.25);
    text(s, "4장 색 규칙", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    D.rect(s, 8.45, 2.3, 1.3, 0.55, "0A0A0C", { line: STEEL });
    D.rect(s, 9.8, 2.3, 1.3, 0.55, "8C8C92", { line: STEEL });
    D.rect(s, 11.15, 2.3, 1.3, 0.55, "F5F5F5", { line: STEEL });
    D.rect(s, 8.45, 2.95, 4.0, 0.3, BLOOD, { line: BLOOD });
    text(s, "적 공격 / 포인트", { x: 8.45, y: 2.95, w: 4.0, h: 0.3, fontSize: 10, bold: true, color: WHITE, align: "center", valign: "middle" });
    bullets(s, [
      "느와르: 새까만 그림자 / 흰 달빛 하이라이트 / 은빛 톱니, 중간 회색은 적게",
      "붉은색은 리본 / 램프 / 적 눈빛 / 적 공격에만",
      "적 공격 = 레이저 느낌: 흰 심지 + 빨간 띠 + 잉크선 (탄 / 섬광 / 베기 / 돌진 자국)",
      "빠른 이동(점멸 / 돌진) 때 눈빛이 빨간 레이저 꼬리를 남김",
      "칼끝 / 눈 이동 궤적은 꼬리가 머리 쪽으로 따라 들어가며 리본 꼬리로 끝남",
      "그림체는 다른 장과 같음: 플랫 고유색 + 브롤풍 셰이더, 이펙트는 툰 띠 + 잉크선",
    ], { x: 8.45, y: 3.45, w: 4.05, h: 3.3, fontSize: 10.5, paraSpaceAfter: 3 });
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
