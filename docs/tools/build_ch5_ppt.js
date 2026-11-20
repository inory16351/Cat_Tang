// 11:59 5장 기획서 (docs/Cat_Tang_레벨_디자인.md 챕터 5 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_ch5_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 나열 구분자는 " / "
// Ver01 (2026-11-19): 5장 무너지는 시간의 끝 확정. 색 = 보라 · 금 우주 (deep indigo-purple void + gold cracks + white starlight), 적 공격 궤적은 자홍 · 마젠타 계열
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_5장_기획서_20261119_정승운_${VER}.pptx`;
const VOID = "1A0A2E", CRACK_GOLD = "D4A843", STAR_WHITE = "E8E0F0", MAGENTA = "C83A7D", MIDNIGHT = "0D0620";

function build() {
  const SEC = ["5장 개요", "스테이지 구성", "일반 몬스터", "중간 보스", "보스", "수치 기획"];
  const D = makeDeck(`11:59 5장 기획서 ${VER}`, `11:59 / 5장 기획서: 무너지는 시간의 끝 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.19"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const pic = (n) => (has(n) ? n : null);
  const BGIMG = pic("ch5/ch5_bg.jpg") || "05_zone1_western.png";
  const infoRows = (s, rows, fs = 12) => rows.forEach((r, i) => {
    const y = 1.6 + i * 0.88;
    card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
    text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: fs, valign: "middle", lineSpacingMultiple: 1.05 });
  });
  // 균열 도식 (바닥에 금빛 갈라진 선)
  const crack = (s, cx, cy, r, fill = CRACK_GOLD, o = {}) => {
    D.circle(s, cx, cy, r, fill, { tr: o.tr || 55, line: fill, dash: "dash", lineW: 1.5 });
    // 갈라진 선
    D.line(s, cx - r * 0.6, cy - r * 0.3, r * 1.2, r * 0.6, fill, { width: 1.5 });
    D.line(s, cx - r * 0.2, cy - r * 0.7, r * 0.4, r * 1.4, fill, { width: 1 });
  };
  // 거울 조각 (반사하는 직사각형)
  const mirror = (s, cx, cy, w, h, angle = 0) => {
    s.addShape(pres.shapes.RECTANGLE, { x: cx - w / 2, y: cy - h / 2, w, h, rotate: angle, fill: { color: STAR_WHITE, transparency: 30 }, line: { color: CRACK_GOLD, width: 1.5 } });
  };

  const cv = D.cover(BGIMG, "5장 기획서", "무너지는 시간의 끝 / 중간 보스 / 보스 상세", "Ver 01");
  cv.addNotes("공통 규칙(챕터 구조 / 전투방 / 보스 세 박자 / 수치 기준)은 공통 레벨 디자인 기획서를 따름. 방 크기는 '방 크기 규칙' 표(5장 40×33 → 46×39), 웨이브 / 보상 / 방 사이 문 규칙은 1장과 같음");
  D.toc(pic("ch5/ch5_boss.jpg") || BGIMG);

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "무너지는 시간의 끝");
    picture(s, BGIMG, 0.6, 1.6, 7.3, 4.1, "5장 배경");
    caption(s, "5장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "마지막 챕터. 1장 '녹음' / 2장 '정지' / 3장 '반사' / 4장 '맞물림'에 이어 '붕괴'가 모티브. 이전 챕터들의 파편이 뒤섞인 보라 · 금 우주 공간", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["테마", "이전 챕터 잔해가 뒤섞인 보라 · 금 우주 허공 / 금빛 균열이 곳곳에"],
      ["색", "짙은 남보라 허공 + 금빛 균열 + 흰 별빛 / 적 공격은 자홍 · 마젠타"],
      ["상인", "합체 상인 (이전 4명이 하나로 합쳐진 금빛 균열 존재)"],
      ["일반 몬스터", "파편 사수 / 별자리 마녀 / 붕괴 거인 / 균열 자객 / 균열술사 / 거울 허상"],
      ["중간 보스", "거울 속의 고양이"],
      ["보스", "시간의 주인"],
    ], 11.5);
    s.addNotes("마지막 챕터. 서부 마을 / 기차역 / 거울 사막 / 시계탑의 파편이 보라 · 금으로 뒤틀려 흩어진 공간. 마을 느낌의 뒤틀린 오브젝트가 최종장 분위기를 줌");
  }

  // 02 스테이지 구성
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "파편이 흩어진 보라 허공을 가로질러");
    const flow = [
      ["스테이지 1", "서부 잔해", PURPLE], ["스테이지 2", "기차 · 사막 잔해", PURPLE], ["중간 보스", "거울 대결장", "5A1A20"],
      ["스테이지 3", "시계탑 잔해", PURPLE], ["스테이지 4", "시간의 감옥", PURPLE], ["보스", "시간의 감옥 중심", RED],
    ];
    flow.forEach((f, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.65, 1.76, 1.1, f[2]);
      text(s, f[0], { x, y: 1.72, w: 1.76, h: 0.4, fontSize: 12.5, bold: true, color: GOLD, align: "center" });
      text(s, f[1], { x, y: 2.1, w: 1.76, h: 0.5, fontSize: 13, bold: true, align: "center" });
      if (i < 5) arrow(s, x + 1.79, 2.2, 0.22, GOLD);
    });
    s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: 2.95, w: 12.1, h: 0.12, fill: { color: CRACK_GOLD, transparency: 40 }, line: { color: CRACK_GOLD, transparency: 100 } });
    s.addShape(pres.shapes.RECTANGLE, { x: 6.8, y: 2.95, w: 5.9, h: 0.12, fill: { color: MAGENTA, transparency: 10 }, line: { color: MAGENTA, transparency: 100 } });
    text(s, "뒤로 갈수록 균열이 많아지고 공간이 더 심하게 뒤틀림", { x: 0.6, y: 3.12, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED, align: "center" });

    picture(s, pic("ch5/ch5_room.jpg") || BGIMG, 0.6, 3.65, 5.6, 3.15, "전투방 컨셉");
    bullets(s, [
      { t: "장소", b: true, c: GOLD },
      "서부 잔해: 1장 건물 파편이 보라빛으로 뒤틀린 공간",
      "기차 · 사막 잔해: 2장 · 3장 오브젝트가 금빛 균열 사이에 떠 있음",
      "시계탑 잔해: 4장 톱니 · 종이 보라빛으로 녹아 붙은 조각",
      "시간의 감옥: 모든 챕터 잔해가 뒤섞인 최종 공간",
      { t: "전투방", b: true, c: GOLD },
      "방 크기 40×33 → 46×39m (방 크기 규칙 표) / 모든 방 평지",
      "웨이브 2~3번 / 다음 웨이브 1초 예고 / 방마다 6종 중 3~4종",
      "상인: 방을 비우면 15% 확률로 금빛 균열에서 합체 상인 등장",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 11.5, paraSpaceAfter: 2 });
  }
  {
    const s = content(2, "방 기믹: 균열");
    const bx = 0.6, by = 1.6, bw = 7.0, bh = 5.2;
    D.rect(s, bx, by, bw, bh, MIDNIGHT, { line: CRACK_GOLD });
    // 균열 여러 개
    crack(s, bx + 1.8, by + 1.5, 0.65);
    crack(s, bx + 4.2, by + 2.8, 0.55);
    crack(s, bx + 2.5, by + 3.8, 0.7);
    // 경고 중인 균열 (밝게)
    D.circle(s, bx + 5.2, by + 1.6, 0.6, CRACK_GOLD, { tr: 25, line: CRACK_GOLD, lineW: 2 });
    text(s, "경고 1.5초", { x: bx + 4.55, y: by + 0.85, w: 1.3, h: 0.28, fontSize: 10, bold: true, color: CRACK_GOLD, align: "center" });
    // 파쇄 중인 균열 (유리 파편)
    D.circle(s, bx + 1.8, by + 1.5, 0.65, MAGENTA, { tr: 40, line: MAGENTA, lineW: 2 });
    [[-0.3, -0.4], [0.35, -0.25], [-0.2, 0.35], [0.4, 0.3], [-0.5, 0.1]].forEach(([dx, dy]) =>
      D.rect(s, bx + 1.8 + dx - 0.06, by + 1.5 + dy - 0.06, 0.12, 0.12, STAR_WHITE, { tr: 20, line: CRACK_GOLD })
    );
    text(s, "파쇄 → 유리 파편", { x: bx + 0.95, y: by + 2.3, w: 1.7, h: 0.28, fontSize: 10, bold: true, color: MAGENTA, align: "center" });
    // 고양이와 적
    D.cat(s, bx + 3.5, by + 2.0, 0.14);
    D.mob(s, bx + 5.5, by + 3.5);
    D.mob(s, bx + 2.0, by + 4.3);
    caption(s, "방 탑뷰 / 8초마다 고양이 근처에 균열 등장", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 7.9, 1.6, 4.8, 5.2, PURPLE);
    text(s, "규칙", { x: 8.15, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "보스 / 중간 보스 방을 뺀 모든 방에 등장",
      "8초마다 고양이 근처 바닥에 금빛 균열이 생김",
      "경고 1.5초 뒤 파쇄 → 유리 파편이 반경 2.5m를 침",
      "피해: 적 30 / 고양이 15",
      "세피라 균열 스타일 (금빛 갈라짐)",
      "적도 균열에 맞음 → 역이용 가능",
    ], { x: 8.15, y: 2.2, w: 4.35, h: 2.6, fontSize: 12, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.15, y: 4.85, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "최종장에서 환경 자체가 무너지는 느낌",
      "적을 균열 근처로 유인해 추가 피해를 주는 전술",
    ], { x: 8.15, y: 5.3, w: 4.35, h: 1.4, fontSize: 12, paraSpaceAfter: 3 });
  }
  {
    const s = content(2, "이전 챕터의 잔해: 오브젝트");
    const W = 2.85, H = 3.15;
    const objs = [
      ["화약통 (1장)", "쏘면 폭발", "반경 3m\n적 40 / 고양이 15", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.42, MAGENTA, { tr: 65, line: MAGENTA, dash: "dash" });
        D.rect(s, cx - 0.15, cy - 0.2, 0.3, 0.4, "6B4A2E", { line: CRACK_GOLD });
        D.mob(s, cx + 0.5, cy - 0.3); D.mob(s, cx - 0.45, cy + 0.35);
      }],
      ["멈춘 짐 (2장)", "맞은 방향으로 날아감", "반경 1.5m\n적 40 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.cat(s, bx + 0.15, cy, 0.1);
        D.line(s, bx + 0.28, cy, 0.25, 0, GOLD, { width: 1.5, end: "triangle" });
        D.rect(s, bx + 0.6, cy - 0.12, 0.24, 0.24, VOID, { line: CRACK_GOLD });
        D.line(s, bx + 0.88, cy, 0.4, 0, GOLD, { width: 2, dash: "dash", end: "triangle" });
        D.mob(s, bx + 1.35, cy, 0.1);
      }],
      ["거울 조각 (3장)", "양쪽 탄 1회 반사 / 쏘면 15° 회전", "폭 2.5m", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        mirror(s, cx, cy, 0.08, sz * 0.65, 15);
        D.line(s, bx + 0.2, cy + 0.3, cx - bx - 0.25, -0.3, GOLD, { width: 1.5, end: "triangle" });
        D.line(s, cx + 0.05, cy, 0.5, -0.35, GOLD, { width: 1.5, dash: "dash", end: "triangle" });
      }],
      ["태엽 스프링 (4장)", "쏘면 튕기며 폭발", "반경 3m\n적 60 / 고양이 20", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.42, MAGENTA, { tr: 70, line: MAGENTA, dash: "dash" });
        [0.25, 0.18, 0.11].forEach((r) => D.circle(s, cx, cy, r, null, { line: CRACK_GOLD, lineW: 1.5 }));
        D.mob(s, cx + 0.5, cy - 0.3);
      }],
    ];
    objs.forEach((o, i) => {
      const x = 0.6 + i * 3.08, y = 1.6;
      card(s, x, y, W, H);
      const sz = 1.55;
      D.rect(s, x + 0.15, y + 0.15, W - 0.3, sz, MIDNIGHT, { line: MIDNIGHT });
      o[3](x + 0.15 + (W - 0.3 - sz) / 2, y + 0.15, sz);
      text(s, o[0], { x: x + 0.15, y: y + 1.78, w: W - 0.3, h: 0.36, fontSize: 13, bold: true, color: GOLD });
      text(s, o[1], { x: x + 0.15, y: y + 2.14, w: W - 0.3, h: 0.5, fontSize: 11 });
      text(s, o[2], { x: x + 0.15, y: y + 2.65, w: W - 0.3, h: 0.45, fontSize: 10, color: MUTED });
    });
    D.card(s, 0.6, 4.95, 12.1, 1.75, PURPLE);
    text(s, [{ text: "공통  ", options: { bold: true, color: GOLD } }, { text: "이전 챕터 오브젝트가 보라 · 금으로 뒤틀린 형태. 모두 몬스터 공격에도 발동 / 엄폐물(내구 60) = 뒤틀린 마차 · 녹슨 톱니 조각 · 깨진 시계판\n후보 자리 중 일부에만 무작위 배치 / 거울 조각은 양쪽 탄을 한 번씩 반사, 쏘면 15° 회전" }], { x: 0.85, y: 4.95, w: 11.7, h: 0.85, fontSize: 12, valign: "middle" });
    bullets(s, [
      "화약통(1장): 보라빛 화약통, 쏘면 반경 3m 폭발",
      "멈춘 짐(2장): 맞은 방향으로 날아가 적에게 충돌",
      "거울 조각(3장): 양쪽 탄 1회 반사, 쏘면 15° 회전",
      "태엽 스프링(4장): 쏘면 0.4초 떨다가 반경 3m 폭발",
    ], { x: 0.85, y: 5.85, w: 11.7, h: 0.8, fontSize: 11, paraSpaceAfter: 1 });
  }

  // 03 일반 몬스터
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시간의 하수인 6종");
    picture(s, pic("ch5/ch5_enemies.jpg") || BGIMG, 0.6, 1.6, 12.1, 2.45, "일반 몬스터 컨셉");
    const mobs = [
      ["파편 사수", "원거리", TEAL, "유리 총알이 벽 / 거울에 닿으면 3갈래 분열", ["8~11m / 2.5초마다", "예고 0.6초 / 10m/초", "체력 204 (3발)"]],
      ["별자리 마녀", "원거리", TEAL, "고양이 주변에 별 표식 3개를 찍고 연결", ["9m 유지 / 6초마다", "별 연결선 피해 19", "체력 204 (3발)"]],
      ["붕괴 거인", "근접", MAGENTA, "지면을 내려쳐 균열을 만들고 돌진", ["이속 3.2m/초", "내려찍기 + 돌진", "체력 544 (8발)"]],
      ["균열 자객", "근접", MAGENTA, "균열 속에 숨었다가 고양이 뒤에서 등장", ["점멸(균열 경유)", "등 뒤 베기 피해 22", "체력 272 (4발)"]],
      ["균열술사", "서포터", "8A2A30", "균열을 추가로 열고, 죽으면 3개 더 열림", ["이속 3.0 / 10m 도주", "허공 탄 피해 19", "체력 204 (3발)"]],
      ["거울 허상", "특수", "8A2A30", "분신 2개 생성, 그림자 있는 게 진짜", ["이속 3.0 / 9m 유지", "분신 사격 피해 19", "체력 204 (3발)"]],
    ];
    mobs.forEach((m, i) => {
      const x = 0.6 + i * 2.04, w = 1.94;
      card(s, x, 4.2, w, 2.65);
      text(s, m[0], { x: x + 0.12, y: 4.28, w: w - 0.2, h: 0.38, fontSize: 13, bold: true, color: GOLD });
      chip(s, m[1], x + 0.12, 4.68, 0.9, m[2], m[2] === TEAL ? BG : TXT, { size: 9, h: 0.26 });
      text(s, m[3], { x: x + 0.12, y: 5.0, w: w - 0.22, h: 0.8, fontSize: 10, lineSpacingMultiple: 1.05 });
      bullets(s, m[4], { x: x + 0.12, y: 5.82, w: w - 0.18, h: 1.0, fontSize: 9, paraSpaceAfter: 1, color: MUTED });
    });
    s.addNotes("적은 보라 · 마젠타 외곽선 + 금빛 균열이 몸에 새겨진 모습. 이전 챕터 잔해처럼 뒤틀린 형태. 적 공격(탄 / 별 / 균열 / 베기)은 전부 자홍 · 마젠타 계열");
  }
  {
    const s = content(3, "핵심 패턴 상세");
    const W = 3.95, H = 5.2;
    const cards = [
      ["파편 사수", "분열탄", ["8~11m 거리를 유지하며 한 발씩 사격", "예고 0.6초 → 유리 탄 발사 (10m/초 / 피해 19)", "벽 / 거울 조각에 닿으면 3갈래 분열 (25° 간격)", "분열탄 각각 피해 19"], (bx, by, sz) => {
        const cy = by + sz / 2;
        D.mob(s, bx + 0.2, cy, 0.12);
        D.line(s, bx + 0.35, cy, 0.6, 0, MAGENTA, { width: 2, end: "triangle" });
        mirror(s, bx + 1.05, cy, 0.06, 0.6, 10);
        [[-12, 0.5], [0, 0.45], [12, 0.5]].forEach(([a, r]) => {
          const rad = a * Math.PI / 180;
          D.line(s, bx + 1.1, cy, Math.cos(rad) * r, Math.sin(rad) * r, MAGENTA, { width: 1.5, dash: "dash", end: "triangle" });
        });
      }],
      ["균열 자객", "균열 잠입 · 등 뒤 베기", ["1.5초 동안 투명해지며 가장 가까운 균열로 진입", "고양이에게 가장 가까운 균열이 0.6초 동안 빛남 (경고)", "등장하며 전방 100° / 2.5m 베기 (피해 22)", "베기 후 1초 빈틈"], (bx, by, sz) => {
        const cy = by + sz / 2;
        crack(s, bx + 0.3, cy, 0.2);
        D.line(s, bx + 0.55, cy, 0.35, 0, CRACK_GOLD, { width: 1.5, dash: "sysDot" });
        crack(s, bx + 1.15, cy - 0.1, 0.22);
        D.mob(s, bx + 1.15, cy - 0.1, 0.1);
        D.pie(s, bx + 1.15, cy - 0.1, 0.5, 130, 230, MAGENTA, { tr: 50 });
        D.cat(s, bx + sz - 0.2, cy + 0.1, 0.1);
      }],
      ["거울 허상", "분신 + 그림자 판별", ["8초마다 분신 2개 생성 + 위치 셔플 (0.6초)", "분신도 사격 (8m/초 / 피해 19)", "한 발로 분신 파괴 / 진짜를 잡으면 전부 사라짐", "그림자가 보이는 쪽이 진짜 (3장과 반대)"], (bx, by, sz) => {
        const cy = by + sz / 2;
        [[-0.35, -0.3], [0.0, 0.3], [0.4, -0.15]].forEach(([dx, dy], k) => {
          D.mob(s, bx + sz / 2 + dx, cy + dy, 0.1);
          if (k === 1) {
            D.rect(s, bx + sz / 2 + dx - 0.08, cy + dy + 0.12, 0.16, 0.06, MIDNIGHT, { line: MIDNIGHT });
            text(s, "그림자", { x: bx + sz / 2 + dx - 0.3, y: cy + dy + 0.2, w: 0.6, h: 0.2, fontSize: 8, bold: true, color: CRACK_GOLD, align: "center" });
          }
        });
        D.cat(s, bx + sz - 0.15, cy, 0.1);
      }],
    ];
    cards.forEach((c, i) => {
      const x = 0.6 + i * 4.08, y = 1.6;
      card(s, x, y, W, H);
      const sz = 1.8;
      D.rect(s, x + 0.25, y + 0.25, W - 0.5, sz, MIDNIGHT, { line: MIDNIGHT });
      c[3](x + 0.25 + (W - 0.5 - sz) / 2, y + 0.25, sz);
      text(s, c[0], { x: x + 0.25, y: y + 2.2, w: W - 0.5, h: 0.32, fontSize: 12, bold: true, color: MUTED });
      text(s, c[1], { x: x + 0.25, y: y + 2.52, w: W - 0.5, h: 0.42, fontSize: 17, bold: true, color: GOLD });
      bullets(s, c[2], { x: x + 0.25, y: y + 3.0, w: W - 0.45, h: 2.1, fontSize: 11, paraSpaceAfter: 2 });
    });
  }
  {
    const s = content(3, "강화형: 스테이지 3부터 등장");
    text(s, "중간 보스 처치 후의 하수인은 전부 강화형. 금빛 균열이 더 많이 새겨진 모습 + 마젠타 눈빛 + 공격 하나 추가 + 체력 1.5배", { x: 0.6, y: 1.55, w: 12.1, h: 0.5, fontSize: 14, color: MUTED });
    const ups = [
      ["파편 사수+", "관통 분열", "탄이 첫 대상을 관통한 뒤 분열 (관통 + 3갈래)"],
      ["별자리 마녀+", "별자리 완성", "별 3개가 모두 연결되면 삼각형 영역 폭발 (반경 3m / 피해 22)"],
      ["붕괴 거인+", "균열 연쇄", "내려찍기 균열이 4m 안의 다른 균열로 연쇄 → 추가 균열 폭발"],
      ["균열 자객+", "이중 매복", "첫 베기 후 0.5초 뒤 다른 균열에서 한 번 더 등장"],
      ["균열술사+", "균열 폭발", "죽을 때 여는 3개 균열이 경고 없이 즉시 파쇄"],
      ["거울 허상+", "분신 3개", "분신 3개로 증가 + 죽은 분신이 균열 장판(반경 1.5m / 3초) 남김"],
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
    const s = content(4, "거울 속의 고양이");
    picture(s, pic("ch5/ch5_midboss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "중간 보스 컨셉");
    caption(s, "중간 보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "플레이어의 모든 무장을 복제한 거울 속 고양이. 쌍권총 / 12발 장전 / 대시 / 저스트 회피 / 건카타까지 쓰는 도플갱어", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["등장", "스테이지 2 마지막 방 뒤에 이어 붙은 거울 대결장"],
      ["보스맵", "28×24m / 흰색 반전 아레나 / 피해 누적 시 금빛 균열 증가"],
      ["이동", "고양이와 같은 속도 (이속 4.5m/초)"],
      ["체력", "10260 (목표 처치 시간 90초)"],
      ["페이즈", "3페이즈: 거울 → 왜곡 → 파편"],
      ["고유 기믹", "3종 쿨다운 스킬 무작위 (쿨 20초)"],
    ], 11.5);
  }
  {
    const s = content(4, "3페이즈: 거울 → 왜곡 → 파편");
    card(s, 0.6, 1.6, 3.95, 5.25);
    text(s, "1페이즈: 거울", { x: 0.85, y: 1.75, w: 3.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "기본 쌍권총 사격만 사용",
      "고양이처럼 움직이며 쏘기",
      "패턴 간격 보통",
    ], { x: 0.85, y: 2.2, w: 3.5, h: 1.4, fontSize: 12, paraSpaceAfter: 3 });
    const cx1 = 2.6, cy1 = 4.3;
    D.rect(s, 0.85, 3.65, 3.3, 1.75, MIDNIGHT, { line: CRACK_GOLD });
    D.cat(s, cx1 - 0.5, cy1, 0.12);
    D.boss(s, cx1 + 0.5, cy1, 0.12);
    D.line(s, cx1 - 0.35, cy1, 0.7, 0, MAGENTA, { width: 1.5, dash: "dash" });
    text(s, "서로 쏘기", { x: cx1 - 0.5, y: cy1 + 0.2, w: 1.0, h: 0.25, fontSize: 9, color: MUTED, align: "center" });

    card(s, 4.68, 1.6, 3.95, 5.25);
    text(s, "2페이즈: 왜곡", { x: 4.93, y: 1.75, w: 3.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, "HP 66% 이하", { x: 7.0, y: 1.78, w: 1.4, h: 0.35, fontSize: 10, bold: true, color: MAGENTA, align: "right" });
    bullets(s, [
      "잔상이 붙기 시작 (잔상 돌진 패턴 추가)",
      "대시 / 저스트 회피 / 건카타 궁 사용",
      "시간 역행: 3초 전 위치로 순간이동 후 연사",
    ], { x: 4.93, y: 2.2, w: 3.5, h: 1.6, fontSize: 12, paraSpaceAfter: 3 });
    const cx2 = 6.65, cy2 = 4.3;
    D.rect(s, 4.93, 3.85, 3.3, 1.55, MIDNIGHT, { line: CRACK_GOLD });
    D.boss(s, cx2, cy2, 0.12);
    [0.25, 0.5].forEach((dx) => D.circle(s, cx2 - dx, cy2 + dx * 0.3, 0.1, MAGENTA, { tr: 50, line: MAGENTA }));
    text(s, "잔상", { x: cx2 - 0.85, y: cy2 + 0.25, w: 0.6, h: 0.25, fontSize: 9, color: MUTED, align: "center" });

    card(s, 8.76, 1.6, 3.95, 5.25);
    text(s, "3페이즈: 파편", { x: 9.01, y: 1.75, w: 3.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, "HP 33% 이하", { x: 11.08, y: 1.78, w: 1.4, h: 0.35, fontSize: 10, bold: true, color: MAGENTA, align: "right" });
    bullets(s, [
      "3마리로 분열 + 위치 셔플",
      "그림자가 있는 쪽이 진짜",
      "가짜는 유리 (1발로 파괴)",
      "전멸 뒤 플레이어가 마지막 한 발로 마무리",
    ], { x: 9.01, y: 2.2, w: 3.5, h: 2.0, fontSize: 12, paraSpaceAfter: 3 });
    const cx3 = 10.73, cy3 = 4.5;
    D.rect(s, 9.01, 4.0, 3.3, 1.45, MIDNIGHT, { line: CRACK_GOLD });
    [[-0.4, -0.2], [0.0, 0.25], [0.45, -0.1]].forEach(([dx, dy], k) => {
      D.boss(s, cx3 + dx, cy3 + dy, 0.1);
      if (k === 1) D.rect(s, cx3 + dx - 0.07, cy3 + dy + 0.12, 0.14, 0.05, MIDNIGHT, { line: MIDNIGHT });
    });
    text(s, "그림자 = 진짜", { x: cx3 - 0.5, y: cy3 + 0.55, w: 1.0, h: 0.25, fontSize: 9, bold: true, color: CRACK_GOLD, align: "center" });
  }
  {
    const s = content(4, "고유 기믹 3종: 쿨 20초 무작위");
    const rules = [
      ["쿨 20초", "전투 20초 뒤 첫 발동 / 3종 중 무작위 (직전 기믹 제외)"],
      ["기믹 우선", "쿨이 다 돌면 진행 중인 패턴이 끝나는 대로 바로 기믹"],
      ["쿨 정지", "기믹 진행 동안은 쿨이 돌지 않음"],
      ["기믹 중", "거울 고양이 일반 패턴 없음"],
    ];
    rules.forEach((r, i) => {
      const x = 0.6 + i * 3.06;
      card(s, x, 1.6, 2.92, 1.25, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: x + 0.2, y: 1.68, w: 2.6, h: 0.38, fontSize: 15, bold: true, color: GOLD });
      text(s, r[1], { x: x + 0.2, y: 2.06, w: 2.6, h: 0.75, fontSize: 11.5 });
    });
    const g = [
      ["① 깨진 거울 벽", "거울 벽 4개가 솟아오름. 거울 고양이 탄이 거울에서 반사. 거울을 쏘면 파괴 → 반사 차단 + 파편이 보스에게도 피해", "경고 1초 / 파편 피해 25 / 성공: 반사 차단 + 보스 추가 피해", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        [[-0.5, 0], [0.5, 0], [0, -0.45], [0, 0.45]].forEach(([dx, dy]) => mirror(s, cx + dx, cy + dy, 0.06, 0.4));
        D.boss(s, cx, cy, 0.1);
        D.line(s, cx, cy, -0.4, -0.3, MAGENTA, { width: 1.5 });
        D.line(s, cx - 0.4, cy - 0.3, 0.3, -0.1, MAGENTA, { width: 1.5, dash: "dash", end: "triangle" });
      }],
      ["② 그림자 찾기", "아레나가 어두워지고 거울 고양이 6마리가 원을 그리며 돎. 8초 안에 그림자가 있는 진짜를 사격 → 3초 기절. 가짜를 맞추면 파쇄 + 유리 파편 피해", "가짜 피해 22 / 성공: 3초 기절", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.4, MIDNIGHT, { tr: 20, line: CRACK_GOLD, dash: "dash" });
        for (let k = 0; k < 6; k++) {
          const a = (k * 60 - 90) * Math.PI / 180;
          D.boss(s, cx + Math.cos(a) * sz * 0.35, cy + Math.sin(a) * sz * 0.35, 0.07);
        }
        text(s, "?", { x: cx - 0.15, y: cy - 0.15, w: 0.3, h: 0.3, fontSize: 14, bold: true, color: CRACK_GOLD, align: "center", valign: "middle" });
      }],
      ["③ 거울 너머 저격", "거울 고양이가 벽 거울 속으로 들어가 4벽면 사이를 이동하며 저격 (조준선 경고 1초 / 피해 30). 거울 고양이가 있는 벽 거울을 사격하면 끌어냄 + 2초 기절", "조준선 1초 / 피해 30 / 성공: 2초 기절", (bx, by, sz) => {
        D.rect(s, bx, by, sz, sz, MIDNIGHT, { line: CRACK_GOLD });
        mirror(s, bx + 0.08, by + sz / 2, 0.06, sz * 0.5);
        mirror(s, bx + sz - 0.08, by + sz / 2, 0.06, sz * 0.5);
        D.boss(s, bx + 0.08, by + sz / 2, 0.08);
        D.line(s, bx + 0.15, by + sz / 2, sz - 0.4, -0.2, MAGENTA, { width: 1.5, dash: "dash" });
        D.cat(s, bx + sz - 0.25, by + sz / 2 - 0.15, 0.08);
      }],
    ];
    g.forEach((it, i) => {
      const x = 0.6 + i * 4.08, y = 3.0;
      card(s, x, y, 3.95, 3.85);
      const sz = 1.55;
      D.rect(s, x + (3.95 - sz) / 2, y + 0.15, sz, sz, MIDNIGHT, { line: MIDNIGHT });
      it[3](x + (3.95 - sz) / 2, y + 0.15, sz);
      text(s, it[0], { x: x + 0.2, y: y + 1.78, w: 3.6, h: 0.36, fontSize: 14, bold: true, color: GOLD });
      text(s, it[1], { x: x + 0.2, y: y + 2.14, w: 3.6, h: 1.2, fontSize: 9.5, lineSpacingMultiple: 1.05 });
      text(s, it[2], { x: x + 0.2, y: y + 3.33, w: 3.6, h: 0.48, fontSize: 9, color: MUTED });
    });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "쌍권총 연사", "쌍권총으로 6발 연사", "예고 0.5초 / 0.15초 간격 / 6발\n발당 피해 10 (최대 60)", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.boss(s, bx + 0.25, cy, 0.12);
      [0, 1, 2].forEach((k) => D.line(s, bx + 0.4 + k * 0.08, cy - 0.06 + k * 0.06, 0.8, 0, MAGENTA, { width: 1.5 }));
      D.cat(s, bx + sz - 0.2, cy, 0.09);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "대시 베기", "5m 대시 + 도착점에서 2.5m 베기", "예고 0.6초 / 5m 대시 / 2.5m 베기\n피해 28", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.line(s, bx + 0.3, cy, 0.6, 0, CRACK_GOLD, { width: 2.5, dash: "dash", end: "triangle" });
      D.pie(s, bx + 1.05, cy, 0.45, 250, 110, MAGENTA, { tr: 45 });
      D.boss(s, bx + 0.25, cy, 0.12);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "조준 사격", "추적 조준 0.8초 → 고정 → 1발", "추적 0.8초 → 고정 → 14m/초\n피해 25", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.boss(s, bx + 0.2, cy, 0.12);
      D.line(s, bx + 0.35, cy, sz - 0.6, 0, MAGENTA, { width: 1.5, dash: "dash" });
      D.cat(s, bx + sz - 0.2, cy, 0.09);
      D.circle(s, bx + sz - 0.2, cy, 0.18, null, { line: MAGENTA, lineW: 1 });
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "반사 탄", "벽 / 거울에서 튕기는 탄 발사", "탄속 10m/초 / 벽 1회 반사\n피해 22", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.boss(s, bx + 0.2, cy, 0.12);
      D.line(s, bx + 0.35, cy, 0.5, -0.35, MAGENTA, { width: 1.5, end: "triangle" });
      D.line(s, bx + 0.85, cy - 0.35, 0.5, 0.4, MAGENTA, { width: 1.5, dash: "dash", end: "triangle" });
      D.rect(s, bx + 0.82, by + 0.05, 0.08, 0.3, STAR_WHITE, { line: CRACK_GOLD });
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "건카타 궁", "2초 동안 8m 안 무작위 12발 난사", "2초 / 12발 / 8m 무작위\n발당 피해 8 (최대 96)", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.4, MAGENTA, { tr: 60, line: MAGENTA, dash: "dash" });
      for (let k = 0; k < 8; k++) {
        const a = k * Math.PI / 4 + 0.3;
        D.circle(s, cx + Math.cos(a) * sz * 0.28, cy + Math.sin(a) * sz * 0.28, 0.035, MAGENTA);
      }
      D.boss(s, cx, cy, 0.12);
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 5m 안",
      "거리에 맞는 패턴 + 거리 무관 패턴을 같은 확률로 추첨",
      "직전 패턴은 제외",
      "2페이즈: 잔상 돌진 / 시간 역행 추가",
      "3페이즈: 3마리 분열 + 그림자 판별",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 11, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
    s.addNotes("거울 속의 고양이 = 플레이어 미러 매치. 자기 자신과 싸우는 느낌. 도플갱어가 자기 모든 기술을 쓰지만, 패턴을 읽으면 이길 수 있다는 게 핵심");
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "시간의 주인");
    picture(s, pic("ch5/ch5_boss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "시계 얼굴의 망토 쓴 사신. 런 내내 멀리서 지켜보던 존재. 대낫을 휘두르고 시간 자체를 무기로 씀", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["보스맵", "시간의 감옥 중심 / 원형 지름 42m / 깨진 시계판 파편이 떠 있음 / 밖은 보라 허공"],
      ["이동", "1페이즈 3.5m/초 / 2~3페이즈 4.5m/초"],
      ["체력", "15120 (1~2페이즈 합산, 목표 120초) / 3페이즈는 60초 생존"],
      ["페이즈", "3페이즈: 최종형 → 환영 소환 → 60초 생존"],
      ["상시 기믹", "남은 시간 게이지 (불-시간 메카닉 최종형)"],
      ["고유 기믹", "1페이즈 3종 + 3페이즈 패링 2종"],
    ], 11.5);
    s.addNotes("시간의 주인 = 모든 런의 끝에서 기다리는 존재. 1~4장 보스를 조종하던 본체. 시간 게이지가 0이 되면 즉사하는 불-시간 메카닉이 여기서 최종 완성");
  }
  {
    const s = content(5, "3페이즈 구조");
    const phases = [
      ["1페이즈: 최종형", "불-시간 메카닉 최종형 + 남은 시간 게이지\n낫 베기가 HP 대신 시간 게이지를 깎음\n시간 0 = 즉사\n모래시계 아이템(20초마다 2개 생성)으로 시간 회복", VOID, "HP 비례"],
      ["2페이즈: 환영 소환", "이전 보스 환영을 차례로 소환\n각 환영 HP 1500 (20초 안에 처치 or 소멸)\n환영은 해당 보스의 기믹 하나를 사용", PURPLE, "HP 50% 이하"],
      ["3페이즈: 60초 생존", "마지막 60초 카운트다운\n패링 패턴을 성공하면 초가 더 빨리 줄어듦\n아레나 가장자리가 15초마다 2m씩 좁아짐", RED, "남은 HP 없음"],
    ];
    phases.forEach((p, i) => {
      const x = 0.6 + i * 4.08;
      card(s, x, 1.6, 3.95, 5.25, p[2]);
      text(s, p[0], { x: x + 0.2, y: 1.75, w: 3.6, h: 0.4, fontSize: 17, bold: true, color: GOLD });
      chip(s, p[3], x + 2.5, 1.78, 1.25, CRACK_GOLD, MIDNIGHT, { size: 9, h: 0.3 });
      text(s, p[1], { x: x + 0.2, y: 2.3, w: 3.6, h: 2.6, fontSize: 12, lineSpacingMultiple: 1.15 });
      // 도식
      const bx = x + 0.3, by = 5.0, sz = 1.5;
      D.rect(s, bx, by, 3.4, sz, MIDNIGHT, { line: CRACK_GOLD });
      if (i === 0) {
        D.boss(s, bx + 1.7, by + sz / 2, 0.18);
        // 시간 게이지 바
        D.rect(s, bx + 0.3, by + 0.15, 2.8, 0.2, VOID, { line: CRACK_GOLD });
        D.rect(s, bx + 0.3, by + 0.15, 1.8, 0.2, CRACK_GOLD, { line: CRACK_GOLD });
        text(s, "시간 게이지", { x: bx + 0.3, y: by + 0.4, w: 2.8, h: 0.2, fontSize: 8, color: MUTED, align: "center" });
        // 모래시계
        D.rect(s, bx + 2.6, by + 0.9, 0.2, 0.3, CRACK_GOLD, { line: CRACK_GOLD });
        text(s, "⏳", { x: bx + 2.5, y: by + 1.2, w: 0.4, h: 0.2, fontSize: 8, color: CRACK_GOLD, align: "center" });
      } else if (i === 1) {
        D.boss(s, bx + 0.5, by + sz / 2, 0.12);
        // 환영 4개
        ["녹은 거인", "기관차", "거울 고래", "쌍둥이"].forEach((n, k) => {
          D.circle(s, bx + 1.2 + k * 0.5, by + sz / 2, 0.15, MAGENTA, { tr: 40, line: MAGENTA });
          text(s, n, { x: bx + 1.0 + k * 0.5, y: by + sz / 2 + 0.2, w: 0.5, h: 0.2, fontSize: 6, color: MUTED, align: "center" });
        });
      } else {
        D.boss(s, bx + 1.7, by + sz / 2, 0.18);
        D.cat(s, bx + 0.8, by + sz / 2 + 0.2, 0.1);
        // 좁아지는 가장자리
        D.rect(s, bx, by, 0.15, sz, MAGENTA, { tr: 50, line: MAGENTA });
        D.rect(s, bx + 3.25, by, 0.15, sz, MAGENTA, { tr: 50, line: MAGENTA });
        text(s, "60초", { x: bx + 1.2, by: by + 0.1, w: 1.0, h: 0.3, fontSize: 12, bold: true, color: CRACK_GOLD, align: "center" });
      }
    });
  }
  {
    const s = content(5, "1페이즈 고유 기믹 3종");
    const g = [
      ["① 죽음의 시각", "얼굴 시계가 특정 시각을 가리킴. 그 시각의 탄으로 시계 얼굴을 명중 → 시계 파쇄 + 4초 기절. 틀린 탄은 흡수 → 시간 게이지 감소", "쿨 25초 / 성공: 4초 기절\n틀린 탄 → 시간 게이지 감소", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.4, "2A2A30", { line: CRACK_GOLD, lineW: 2 });
        const a = (3 * 30 - 90) * Math.PI / 180;
        D.line(s, cx, cy, Math.cos(a) * sz * 0.3, Math.sin(a) * sz * 0.3, STAR_WHITE, { width: 3 });
        D.circle(s, cx, cy, 0.06, CRACK_GOLD);
        D.boss(s, cx, cy + sz * 0.25, 0.08);
      }],
      ["② 낫 베기 = 시간 깎기", "낫 공격이 HP 대신 시간 게이지를 깎음. 시간 0 = 즉사. 모래시계 픽업(20초마다 2개 생성)으로 시간 회복", "낫 피해 → 시간 -5초\n모래시계 = 시간 회복", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.boss(s, cx, cy, 0.1);
        D.line(s, cx - 0.4, cy - 0.1, 0.8, 0.2, MAGENTA, { width: 2.5 });
        D.rect(s, bx + 0.1, by + 0.1, sz - 0.2, 0.15, VOID, { line: CRACK_GOLD });
        D.rect(s, bx + 0.1, by + 0.1, (sz - 0.2) * 0.6, 0.15, CRACK_GOLD, { line: CRACK_GOLD });
        text(s, "-5초", { x: cx - 0.3, y: cy + 0.3, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: MAGENTA, align: "center" });
      }],
      ["③ 12칸 시계 숫자 인장", "바닥에 숫자 인장 3개 등장. 15초 안에 각 인장 위에 서서 맞는 시각 탄으로 사격 (시각 + 위치 + 순서). 성공: 4초 기절 / 실패: 남은 인장당 최대 HP 10%", "15초 / 3인장 순서대로\n성공: 4초 기절 / 실패: 인장당 10%", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        [[-0.35, -0.2, "3"], [0.3, -0.25, "7"], [0.0, 0.35, "11"]].forEach(([dx, dy, n]) => {
          D.rect(s, cx + dx - 0.15, cy + dy - 0.15, 0.3, 0.3, CRACK_GOLD, { tr: 30, line: CRACK_GOLD });
          text(s, n, { x: cx + dx - 0.15, y: cy + dy - 0.15, w: 0.3, h: 0.3, fontSize: 10, bold: true, color: MIDNIGHT, align: "center", valign: "middle" });
        });
        D.cat(s, cx - 0.35, cy - 0.2, 0.06);
      }],
    ];
    g.forEach((it, i) => {
      const x = 0.6 + i * 4.08, y = 1.6;
      card(s, x, y, 3.95, 5.25);
      const sz = 1.65;
      D.rect(s, x + (3.95 - sz) / 2, y + 0.15, sz, sz, MIDNIGHT, { line: MIDNIGHT });
      it[3](x + (3.95 - sz) / 2, y + 0.15, sz);
      text(s, it[0], { x: x + 0.2, y: y + 1.9, w: 3.6, h: 0.4, fontSize: 15, bold: true, color: GOLD });
      text(s, it[1], { x: x + 0.2, y: y + 2.35, w: 3.6, h: 1.8, fontSize: 11, lineSpacingMultiple: 1.1 });
      text(s, it[2], { x: x + 0.2, y: y + 4.65, w: 3.6, h: 0.55, fontSize: 10, color: MUTED });
    });
  }
  {
    const s = content(5, "2페이즈: 이전 보스 환영 소환");
    card(s, 0.6, 1.6, 12.1, 2.1, PURPLE);
    text(s, "환영 소환 규칙", { x: 0.85, y: 1.75, w: 6, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "HP 50% 이하 진입 / 시간의 주인은 가장자리로 물러나 감시 모드",
      "이전 4장 보스의 환영을 차례로 소환 / 각 환영 HP 1500 (20초 안에 처치하거나 자동 소멸)",
      "환영은 해당 보스의 기믹 하나를 무작위로 사용",
      "감시 모드: 3초마다 가장자리에서 빔 발사 (경고 0.8초 / 피해 28) + 시간 역류 (고양이의 3초 전 이동이 유령으로 재생 / 피해 25)",
    ], { x: 0.85, y: 2.2, w: 11.6, h: 1.4, fontSize: 12, paraSpaceAfter: 2 });
    const phantoms = [
      ["녹은 시계 거인 (1장)", "시각 패턴 or 초침 탄"],
      ["멈춘 기관차 거인 (2장)", "돌진 or 증기 폭발"],
      ["거울 고래 (3장)", "반사 탄막 or 분신"],
      ["시침 / 분침 쌍둥이 (4장)", "협공 or 별빛 낙하"],
    ];
    phantoms.forEach((p, i) => {
      const x = 0.6 + i * 3.05, y = 3.9;
      card(s, x, y, 2.9, 1.7);
      D.circle(s, x + 1.45, y + 0.55, 0.3, MAGENTA, { tr: 40, line: MAGENTA });
      text(s, String(i + 1) + "장", { x: x + 1.25, y: y + 0.35, w: 0.4, h: 0.4, fontSize: 11, bold: true, color: MIDNIGHT, align: "center", valign: "middle" });
      text(s, p[0], { x: x + 0.15, y: y + 0.95, w: 2.6, h: 0.35, fontSize: 11.5, bold: true, color: GOLD });
      text(s, p[1], { x: x + 0.15, y: y + 1.3, w: 2.6, h: 0.35, fontSize: 10.5, color: MUTED });
    });
    bullets(s, [
      "환영을 빠르게 처치할수록 감시 모드 지속 시간이 짧아짐",
      "모든 환영 처리 후 시간의 주인이 돌아와 3페이즈 진입",
    ], { x: 0.6, y: 5.8, w: 12.1, h: 1.0, fontSize: 12.5, paraSpaceAfter: 3 });
  }
  {
    const s = content(5, "보스 패턴: 1페이즈 (시간의 주인)");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "대낫 베기", "180° 6m 대낫 휘두르기. 시간 게이지도 깎임", "예고 0.9초 / 180° / 6m\n피해 38 + 시간 -5초", (bx, by, sz) => {
      const cx = bx + 0.35, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.7, 270, 90, MAGENTA, { tr: 45 });
      D.boss(s, cx, cy, 0.14);
      D.line(s, cx + 0.1, cy - 0.5, 0.6, 1.0, STAR_WHITE, { width: 2.5 });
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "시간 정지장", "반경 4m 원을 깔아 고양이 이속 -60%", "반경 4m / 3초 유지\n원 안 이속 -60%", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.4, VOID, { tr: 30, line: CRACK_GOLD, dash: "dash" });
      D.boss(s, cx, cy, 0.14);
      D.cat(s, cx + 0.3, cy + 0.2, 0.08);
      text(s, "-60%", { x: cx - 0.3, y: cy + sz * 0.3, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: CRACK_GOLD, align: "center" });
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "종말의 탄", "3개 대형 추적 구체 발사", "3구체 / 5m/초 / 호밍 60°/초\n피해 34 / 5초 수명", (bx, by, sz) => {
      const cx = bx + 0.3, cy = by + sz / 2;
      D.boss(s, cx, cy, 0.14);
      [[-0.1, -0.4, 0.15], [0.2, 0.0, 0.15], [-0.15, 0.4, 0.15]].forEach(([dx, dy, r]) => {
        D.circle(s, cx + 0.6 + dx, cy + dy, r, MAGENTA, { tr: 20, line: MAGENTA });
      });
      D.cat(s, bx + sz - 0.2, cy - 0.1, 0.08);
      D.line(s, cx + 0.75, cy - 0.35, 0.3, -0.1, MAGENTA, { width: 1, dash: "sysDot", end: "triangle" });
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "시계 바늘 창", "조준선 1초 → 20m/초 창", "조준선 1초 → 20m/초\n피해 36", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.boss(s, bx + 0.2, cy, 0.14);
      D.line(s, bx + 0.38, cy, sz - 0.6, 0, MAGENTA, { width: 2, dash: "dash" });
      D.rect(s, bx + sz - 0.3, cy - 0.04, 0.2, 0.08, STAR_WHITE, { line: CRACK_GOLD });
      D.cat(s, bx + sz - 0.15, cy + 0.3, 0.08);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "모래폭풍", "보스 주변 4m 반경 3초간 피해 + 밀어냄", "반경 4m / 3초\n10/초 피해 + 밀어냄", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.4, CRACK_GOLD, { tr: 50, line: CRACK_GOLD });
      for (let k = 0; k < 6; k++) { const a = k * 1.05; D.circle(s, cx + Math.cos(a) * sz * 0.25, cy + Math.sin(a) * sz * 0.25, 0.03, CRACK_GOLD); }
      D.boss(s, cx, cy, 0.14);
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 6m 안",
      "거리에 맞는 패턴 + 거리 무관을 같은 확률 추첨",
      "직전 패턴은 제외",
      "기믹 쿨 25초 / 기믹 우선 / 쿨 정지",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 11.5, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
  }
  {
    const s = content(5, "3페이즈: 60초 생존 + 패링 패턴");
    card(s, 0.6, 1.6, 6.3, 5.25, PURPLE);
    text(s, "60초 생존", { x: 0.85, y: 1.75, w: 5.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "1페이즈 모든 패턴을 1.3배 속도로 사용",
      "아레나 가장자리가 15초마다 2m씩 좁아짐 (균열 붕괴)",
      "최후의 째깍: 12방향 낫 파동 (0.5초 간격 / 피해 30 / 회피 타이밍 맞추면 시간 보너스)",
      "패링 패턴을 성공하면 카운트다운이 더 빨리 줄어듦",
    ], { x: 0.85, y: 2.25, w: 5.8, h: 2.4, fontSize: 12, paraSpaceAfter: 3 });
    // 아레나 도식
    const ax = 3.5, ay = 5.6, ar = 1.1;
    D.circle(s, ax, ay, ar, MIDNIGHT, { line: CRACK_GOLD, lineW: 1.5 });
    D.circle(s, ax, ay, ar - 0.2, null, { line: MAGENTA, lineW: 1, dash: "dash" });
    D.boss(s, ax, ay, 0.15);
    D.cat(s, ax - 0.4, ay + 0.3, 0.09);
    text(s, "좁아짐", { x: ax + 0.5, y: ay - 0.3, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: MAGENTA });

    card(s, 7.15, 1.6, 5.55, 2.45);
    text(s, "패링 1: 모래시계 뒤집기", { x: 7.4, y: 1.75, w: 5.0, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "15초마다 모래시계 3개 등장",
      "쏘면 뒤집힘 → 카운트다운 -5초",
      "놓치면 사신이 되감기 → +5초",
    ], { x: 7.4, y: 2.2, w: 5.0, h: 1.7, fontSize: 12, paraSpaceAfter: 3 });

    card(s, 7.15, 4.2, 5.55, 2.65);
    text(s, "패링 2: 종소리 12번", { x: 7.4, y: 4.35, w: 5.0, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "20초마다 종이 12번 울림 (0.5초 간격)",
      "매 종소리마다 탄 파동 발생",
      "종소리 타이밍에 저스트 회피 → 초가 더 빨리 줄어듦",
    ], { x: 7.4, y: 4.8, w: 5.0, h: 1.9, fontSize: 12, paraSpaceAfter: 3 });
  }

  // 06 수치
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "5장 수치 정리");
    D.table(s, [
      ["대상", "체력", "이속", "피해", "비고"],
      ["파편 사수", "204", "2.8m/초", "19", "8~11m / 2.5초 / 분열 3갈래"],
      ["별자리 마녀", "204", "2.4m/초", "19", "9m / 6초 / 별 3개 → 연결"],
      ["붕괴 거인", "544", "3.2m/초", "20~22", "내려찍기 + 돌진 / 균열 남김"],
      ["균열 자객", "272", "점멸", "22", "균열 경유 등 뒤 베기"],
      ["균열술사", "204", "3.0m/초", "19", "균열 추가 개방 / 사망 시 3개"],
      ["거울 허상", "204", "3.0m/초", "19", "분신 2개 / 그림자 = 진짜"],
      ["강화형", "기본 ×1.5", "-", "19~22", "공격 하나 추가"],
      ["거울 속의 고양이", "10260", "4.5m/초", "10~28", "목표 90초 / 거울 대결장 28×24m"],
      ["시간의 주인", "15120", "3.5~4.5", "30~46", "목표 120초 / 원형 지름 42m"],
    ], 0.6, 1.5, [2.1, 1.3, 1.25, 1.0, 3.75], { rowH: 0.34, size: 10 });
    const cards = [
      ["피해 기준", "4장보다 약 25% 상향\n일반 19~28 / 중간 보스 28~34\n보스 34~46 (기믹은 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 20 × 명중률 60%\n중간 보스 시점 ×9.5 / 90초\n보스 시점 ×10.5 / 120초"],
      ["전투방", "웨이브당 적: 스테이지 1은 8~10마리\n→ 스테이지 4는 11~14마리\n균열 8초마다 / 반경 2.5m"],
      ["오브젝트", "화약통 반경 3m / 적 40 · 고양이 15\n멈춘 짐 반경 1.5m / 적 40 · 고양이 15\n태엽 스프링 반경 3m / 적 60 · 고양이 20"],
    ];
    cards.forEach((c, i) => {
      const y = 1.55 + i * 1.32;
      card(s, 10.2, y, 2.5, 1.2, i % 2 ? CARD : PURPLE);
      text(s, c[0], { x: 10.35, y: y + 0.08, w: 2.3, h: 0.3, fontSize: 12.5, bold: true, color: GOLD });
      text(s, c[1], { x: 10.35, y: y + 0.38, w: 2.3, h: 0.8, fontSize: 9.5, lineSpacingMultiple: 1.1 });
    });
    text(s, "일반 몬스터 체력 = 5장 시작 화력(×8.5, 기본 탄 한 발 68) 기준으로 앞 챕터와 비슷한 발 수", { x: 0.6, y: 6.0, w: 9.3, h: 0.32, fontSize: 11.5, color: MUTED });
    text(s, "중간 보스 10260 = 20 × 9.5 × 90초 × 0.6  /  보스 15120 = 20 × 10.5 × 120초 × 0.6", { x: 0.6, y: 6.36, w: 9.3, h: 0.36, fontSize: 13, bold: true });
  }
  {
    const s = content(6, "방 크기 / 색 규칙");
    card(s, 0.6, 1.6, 7.3, 5.25, PURPLE);
    text(s, "방 크기 규칙 (5장)", { x: 0.85, y: 1.75, w: 6.8, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    D.table(s, [
      ["장", "스테이지 1", "스테이지 2", "스테이지 3", "스테이지 4"],
      ["1장", "28×24", "34×29", "36×30", "38×31"],
      ["2장", "34×29", "36×30", "38×32", "40×33"],
      ["3장", "36×30", "38×32", "40×33", "42×35"],
      ["4장", "38×32", "40×33", "42×35", "44×37"],
      ["5장", "40×33", "42×35", "44×37", "46×39"],
    ], 0.85, 2.25, [1.0, 1.45, 1.45, 1.45, 1.45], { rowH: 0.38, size: 11 });
    bullets(s, [
      "웨이브 적 수가 늘어나는 만큼 일반 전투방도 장 / 스테이지마다 커짐",
      "중간 보스 방 / 보스 방은 각 보스 경기장 값 그대로",
      "오브젝트 후보 자리 / 방 모양은 방 크기에 비례해 늘림",
    ], { x: 0.85, y: 5.1, w: 6.85, h: 1.65, fontSize: 12, paraSpaceAfter: 3 });

    card(s, 8.2, 1.6, 4.5, 5.25);
    text(s, "5장 색 규칙", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    D.rect(s, 8.45, 2.3, 1.3, 0.55, VOID, { line: CRACK_GOLD });
    D.rect(s, 9.8, 2.3, 1.3, 0.55, CRACK_GOLD, { line: CRACK_GOLD });
    D.rect(s, 11.15, 2.3, 1.3, 0.55, STAR_WHITE, { line: CRACK_GOLD });
    D.rect(s, 8.45, 2.95, 4.0, 0.3, MAGENTA, { line: MAGENTA });
    text(s, "적 공격 / 위험", { x: 8.45, y: 2.95, w: 4.0, h: 0.3, fontSize: 10, bold: true, color: STAR_WHITE, align: "center", valign: "middle" });
    bullets(s, [
      "보라 · 금 우주: 짙은 남보라 허공(VOID) + 금빛 균열(CRACK_GOLD) + 흰 별빛(STAR_WHITE)",
      "적 공격은 전부 자홍 · 마젠타 (탄 / 낫 / 균열 폭발 / 베기)",
      "이전 챕터 잔해가 보라 · 금으로 뒤틀린 형태로 등장",
      "균열은 금빛 갈라짐 (세피라 스타일)",
      "플레이어 공격은 흰색 · 금색",
      "그림체는 다른 장과 같음: 플랫 고유색 + 브롤풍 셰이더",
    ], { x: 8.45, y: 3.45, w: 4.05, h: 3.3, fontSize: 10.5, paraSpaceAfter: 3 });
  }

  return pres.writeFile({ fileName: path.join(OUT_DIR, FILE) });
}

(async () => {
  const o = await build();
  const py = process.env.PYTHON || "C:/Users/user/AppData/Local/Programs/Python/Python312/python.exe";
  try { require("child_process").execFileSync(py, [path.join(__dirname, "slash_dots.py"), o], { stdio: "inherit" }); }
  catch (e) { console.warn("slash_dots skipped:", e.message); }
  try { require("child_process").execFileSync(py, [path.join(__dirname, "pptx_compress.py"), o], { stdio: "inherit" }); }
  catch (e) { console.warn("compress skipped:", e.message); }
  console.log("saved", o);
})();
