// 11:59 3장 기획서 (docs/Cat_Tang_레벨_디자인.md 챕터 3 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_ch3_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 나열 구분자는 " / "
// Ver01 (2026-11-18): 3장 신기루 사막 = 거울 세계 확정 (질문으로 확정한 내용만), 상인 공통 규칙 · 2장 짐꾼 정면 33% 변경 포함
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_3장_기획서_20261118_정승운_${VER}.pptx`;
const MIRROR = "BFE6F0", SKY = "7FC8D8", PINK = "E8A0C8", WHITE = "FFFFFF", NAVY = "1F3550";

function build() {
  const SEC = ["3장 개요", "스테이지 구성", "일반 몬스터", "중간 보스", "보스", "수치 기획"];
  const D = makeDeck(`11:59 3장 기획서 ${VER}`, `11:59 / 3장 기획서: 신기루 사막 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.18"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const pic = (n) => (has(`ch3/${n}`) ? `ch3/${n}` : null);
  const BGIMG = pic("ch3_bg.jpg") || "05_zone1_western.png";
  const infoRows = (s, rows, fs = 12) => rows.forEach((r, i) => {
    const y = 1.6 + i * 0.88;
    card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
    text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: fs, valign: "middle", lineSpacingMultiple: 1.05 });
  });
  // 떠다니는 거울판 (기울어진 얇은 판)
  const mirror = (s, cx, cy, w, rot = 0) => s.addShape(pres.shapes.RECTANGLE, { x: cx - w / 2, y: cy - 0.05, w, h: 0.1, rotate: rot, fill: { color: MIRROR }, line: { color: WHITE, width: 1 } });
  // 시계 문자판
  const clock = (s, cx, cy, r, label, o = {}) => {
    D.circle(s, cx, cy, r, o.fill || "F3EDE2", { line: o.line || GOLD, lineW: 1.5, tr: o.tr || 0 });
    if (label !== undefined) text(s, String(label), { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, fontSize: o.size || 12, bold: true, color: o.color || BG, align: "center", valign: "middle" });
  };

  const cv = D.cover(BGIMG, "3장 기획서", "신기루 사막 = 거울 세계 / 중간 보스 / 보스 상세", "Ver 01");
  cv.addNotes("공통 규칙(챕터 구조 / 전투방 / 보스 세 박자 / 수치 기준)은 공통 레벨 디자인 기획서를 따름. 방 크기 / 웨이브 / 보상 / 방 사이 문 규칙은 1장과 같음");
  D.toc(pic("ch3_boss.jpg") || BGIMG);

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "신기루 너머의 거울 세계");
    picture(s, BGIMG, 0.6, 1.6, 7.3, 4.1, "3장 배경");
    caption(s, "3장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "사막 신기루 너머로 들어간 거울 세계. 1장 ‘녹음’ / 2장 ‘정지’에 이어 ‘반사’가 모티브로, 본격적으로 몽환적인 챕터", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["테마", "하늘을 비추는 거울 호수 / 허공에 뜬 섬 / 떠다니는 거울 조각 / 빛가루로 흩어지는 기억"],
      ["오브젝트", "떠다니는 거울 조각 / 기억 수정 / 분수 샘 / 신기루 엄폐물"],
      ["상인", "거울 속 고양이 (좌우 반전된 또 다른 고양이 총잡이)"],
      ["일반 몬스터", "거울 사수 / 빛가루 법사 / 거울 너머 검객 / 유리 기사 / 신기루 분신술사"],
      ["중간 보스", "신기루 마술사"],
      ["보스", "거울 고래"],
    ], 11.5);
    s.addNotes("분위기 참고: 메이플스토리 소멸의 여로(빛가루로 흩어지는 기억 / 떠 있는 섬) / 에스페라(하늘을 비추는 거울 바다). 색: 청록 / 연보라 / 분홍 노을 / 흰 빛, 금색 포인트. 원래 4장 테마였던 '거울 미궁'은 빠지고 4장 테마는 미정");
  }

  // 02 스테이지 구성
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "호수 기슭에서 거울 제단으로");
    const flow = [
      ["스테이지 1", "거울 호수 기슭", PURPLE], ["스테이지 2", "거울 속 서부 마을", PURPLE], ["중간 보스", "거울 극장", "8C3A50"],
      ["스테이지 3", "떠도는 섬", PURPLE], ["스테이지 4", "뒤집힌 하늘", PURPLE], ["보스", "거울 제단", RED],
    ];
    flow.forEach((f, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.65, 1.76, 1.1, f[2]);
      text(s, f[0], { x, y: 1.72, w: 1.76, h: 0.4, fontSize: 12.5, bold: true, color: GOLD, align: "center" });
      text(s, f[1], { x, y: 2.1, w: 1.76, h: 0.5, fontSize: 13, bold: true, align: "center" });
      if (i < 5) arrow(s, x + 1.79, 2.2, 0.22, GOLD);
    });
    s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: 2.95, w: 12.1, h: 0.12, fill: { color: SKY, transparency: 55 }, line: { color: SKY, transparency: 100 } });
    s.addShape(pres.shapes.RECTANGLE, { x: 6.8, y: 2.95, w: 5.9, h: 0.12, fill: { color: PINK, transparency: 15 }, line: { color: PINK, transparency: 100 } });
    text(s, "중심에 가까워질수록 땅이 사라지고 하늘과 거울만 남는 모습", { x: 0.6, y: 3.12, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED, align: "center" });

    picture(s, pic("ch3_room.jpg") || BGIMG, 0.6, 3.65, 5.6, 3.15, "전투방 컨셉");
    bullets(s, [
      { t: "장소", b: true, c: GOLD },
      "호수 기슭: 사막 모래가 거울 호수로 이어지는 곳",
      "거울 속 서부 마을: 1 / 2장 장소가 좌우 반전된 채 빛가루로 바스러짐",
      "떠도는 섬: 허공에 뜬 기억 조각 섬, 가장자리는 보이지 않는 벽",
      "뒤집힌 하늘: 구름이 바닥, 거꾸로 매달린 건물",
      { t: "전투방", b: true, c: GOLD },
      "방 크기 28×24m / 웨이브 2~3번 / 다음 웨이브 1초 예고 (1장과 같은 규칙)",
      "다섯 종류 중 방마다 3~4종을 섞어 등장, 스테이지가 오를수록 수만 늘어남",
      "상인: 방을 비우면 15% 확률로 선 거울이 솟고 거울 속 고양이가 장사",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 11.5, paraSpaceAfter: 2 });
  }
  {
    const s = content(2, "방 기믹: 떠다니는 거울 조각");
    // 도식: 방 탑뷰, 거울 조각에 튕기는 탄
    const bx = 0.6, by = 1.6, bw = 7.0, bh = 5.2;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: LAV });
    mirror(s, bx + 2.3, by + 1.3, 1.3, 30);
    mirror(s, bx + 4.9, by + 3.6, 1.3, -40);
    mirror(s, bx + 5.4, by + 1.2, 1.3, 75);
    [[2.3, 1.3], [4.9, 3.6], [5.4, 1.2]].forEach(([u, v]) => D.arrow(s, bx + u + 0.5, by + v + 0.45, 0.4, MUTED, { dash: "sysDot", width: 1 }));
    // 고양이 탄 → 거울 → 적
    const cx = bx + 1.0, cy = by + 4.2;
    D.cat(s, cx, cy, 0.16);
    D.line(s, cx + 0.1, cy - 0.12, bx + 4.75 - cx - 0.1, by + 3.6 - cy + 0.12, GOLD, { width: 2 });
    D.line(s, bx + 4.85, by + 3.55, 1.25, -2.1, GOLD, { width: 2, end: "triangle" });
    D.mob(s, bx + 6.25, by + 1.3, 0.14);
    text(s, "튕겨서 적 명중", { x: bx + 5.72, y: by + 0.82, w: 1.25, h: 0.28, fontSize: 10, bold: true, color: GOLD, align: "center" });
    // 적 탄 → 거울 → 고양이 뒤
    D.mob(s, bx + 0.9, by + 0.8, 0.14);
    D.line(s, bx + 1.0, by + 0.85, 1.2, 0.4, "FF8A80", { width: 1.5, dash: "dash" });
    D.line(s, bx + 2.25, by + 1.3, -1.1, 2.7, "FF8A80", { width: 1.5, dash: "dash", end: "triangle" });
    text(s, "적 탄도 튕김", { x: bx + 1.6, y: by + 2.5, w: 1.4, h: 0.28, fontSize: 10, bold: true, color: "FF8A80" });
    caption(s, "방 탑뷰 / 거울판 3개가 표류하며 천천히 회전", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 7.9, 1.6, 4.8, 5.2, PURPLE);
    text(s, "규칙", { x: 8.15, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "보스 / 중간 보스 방을 뺀 모든 방에 3개",
      "폭 2.5m 거울판 / 0.8m/초로 표류 / 초당 10° 회전",
      "고양이 탄 / 적 탄 모두 한 번 튕겨 냄",
      "몸은 통과 (길을 막지 않음)",
      "쏘면 조각이 15° 돌아감",
    ], { x: 8.15, y: 2.2, w: 4.35, h: 2.4, fontSize: 12.5, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.15, y: 4.65, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "각도를 노려 엄폐물 뒤 적을 맞히는 재미",
      "적 탄이 뒤에서 날아올 수 있어 위치 선정이 중요",
      "거울 특성 / 거울 은총 빌드가 빛나는 챕터",
    ], { x: 8.15, y: 5.1, w: 4.35, h: 1.6, fontSize: 12.5, paraSpaceAfter: 3 });
  }
  {
    const s = content(2, "쏘면 깨지는 기억: 오브젝트");
    const W = 3.95, H = 3.15;
    const objs = [
      ["기억 수정", "쏘면 깨지며 빛가루 폭발", "방마다 2~4개\n반경 3m 폭발\n피해 적 50 / 고양이 18", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.42, PINK, { tr: 65, line: PINK, dash: "dash" });
        s.addShape(pres.shapes.DIAMOND, { x: cx - 0.17, y: cy - 0.28, w: 0.34, h: 0.56, fill: { color: MIRROR }, line: { color: WHITE, width: 1 } });
        D.mob(s, cx + 0.55, cy - 0.35); D.mob(s, cx - 0.5, cy + 0.4);
      }],
      ["분수 샘", "쏘면 둥근 물기둥 벽이 솟아 양쪽 탄을 막음", "방마다 1~2개\n반경 1.5m / 3초 유지\n다시 쓰려면 8초", (bx, by, sz) => {
        const cx = bx + sz * 0.6, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.3, SKY, { tr: 45, line: WHITE, lineW: 2 });
        D.cat(s, cx + 0.1, cy, 0.1);
        D.mob(s, bx + 0.2, cy);
        D.line(s, bx + 0.3, cy, cx - sz * 0.3 - bx - 0.35, 0, "FF8A80", { dash: "dash", width: 1.5, end: "triangle" });
        text(s, "막음", { x: cx - sz * 0.3 - 0.45, y: cy - 0.35, w: 0.5, h: 0.25, fontSize: 9, bold: true, color: TEAL });
      }],
      ["신기루 엄폐물", "모래 바위 / 무너진 기둥 / 선 거울. 몇 발 맞으면 부서짐", "모든 방\n내구 40\n후보 자리 중 일부만 배치", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.rect(s, bx + 0.25, cy - 0.25, 0.4, 0.5, "B89A6A", { line: TXT });
        D.circle(s, bx + 0.95, cy, 0.22, "8A7A66", { line: TXT });
        D.rect(s, bx + 1.3, cy - 0.3, 0.12, 0.6, MIRROR, { line: WHITE });
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
    text(s, [{ text: "공통  ", options: { bold: true, color: GOLD } }, { text: "기억 수정 / 분수 샘은 몬스터 공격(탄 / 근접 / 폭발)에도 발동\n후보 자리 중 일부에만 무작위 배치 / 원래 1장 화약통, 2장 증기 밸브 자리" }], { x: 0.85, y: 5.85, w: 11.7, h: 0.95, fontSize: 13, valign: "middle" });
  }

  // 03 일반 몬스터
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시간의 하수인 5종");
    picture(s, pic("ch3_enemies.jpg") || BGIMG, 0.6, 1.6, 12.1, 2.55, "일반 몬스터 컨셉");
    const mobs = [
      ["거울 사수", "원거리", TEAL, "손거울로 빛을 모아 쏨. 거울 조각 / 벽에 한 번 튕겨 꺾여 옴", ["8~11m 유지 / 2.5초마다", "탄속 10m/초 / 피해 12", "체력 108 (3발)"]],
      ["빛가루 법사", "원거리", TEAL, "빛 구슬 5개를 띄우고 하나씩 느리게 유도해 날림", ["6초마다 구슬 5개", "5m/초 / 초당 90° 유도", "피해 12 / 체력 108"]],
      ["거울 너머 검객", "근접", RED, "거울 속에 숨었다가 가장 가까운 거울에서 튀어나와 벰", ["잠복 2초 / 거울 예고 0.7초", "전방 100° 2.5m / 피해 16", "체력 144 (4발)"]],
      ["유리 기사", "방패 근접", "7A6A55", "정면 탄을 고양이 쪽으로 되튕김. 4발 맞으면 거울이 깨짐", ["정면 120° 반사 (피해 8)", "깨지면 5초 반사 없음", "밀치기 3m / 피해 16 / 체력 288"]],
      ["신기루 분신술사", "특수", PINK, "가짜 분신 둘을 만들고 셋이 자리를 섞음", ["8초마다 분신 2개", "분신은 한 발에 소멸", "본체 처치 시 분신도 소멸 / 체력 108"]],
    ];
    mobs.forEach((m, i) => {
      const x = 0.6 + i * 2.44, w = 2.32;
      card(s, x, 4.3, w, 2.55);
      text(s, m[0], { x: x + 0.15, y: 4.38, w: w - 0.3, h: 0.38, fontSize: 14, bold: true, color: GOLD });
      chip(s, m[1], x + 0.15, 4.78, 1.0, m[2], m[2] === TEAL || m[2] === PINK ? BG : TXT, { size: 9.5, h: 0.27 });
      text(s, m[3], { x: x + 0.15, y: 5.1, w: w - 0.3, h: 0.72, fontSize: 10.5, lineSpacingMultiple: 1.05 });
      bullets(s, m[4], { x: x + 0.15, y: 5.82, w: w - 0.25, h: 1.0, fontSize: 9.5, paraSpaceAfter: 1, color: MUTED });
    });
  }
  {
    const s = content(3, "거울을 쓰는 하수인");
    const W = 3.95, H = 5.2;
    const cards = [
      ["거울 너머 검객", "거울에서 튀어나오는 베기", ["떠다니는 거울 조각 / 선 거울 속으로 들어가 2초 잠복 (무적)", "고양이와 가장 가까운 거울(4m 안)이 0.7초 빛나며 예고", "튀어나와 전방 100° / 2.5m 베기, 벤 뒤 1초 빈틈", "4m 안에 거울이 없으면 걸어서 다가와 같은 베기"], (bx, by, sz) => {
        const cy = by + sz / 2;
        mirror(s, bx + 0.5, cy - 0.3, 0.7, 70);
        D.circle(s, bx + 0.5, cy - 0.3, 0.35, WHITE, { tr: 75, line: WHITE, dash: "dash" });
        D.pie(s, bx + 0.8, cy - 0.1, 0.6, 330, 70, RED, { tr: 55 });
        D.mob(s, bx + 0.8, cy - 0.1, 0.13);
        D.cat(s, bx + 1.3, cy + 0.15);
      }],
      ["유리 기사", "거울 방패 반사 → 깨기", ["앞에 큰 거울 방패: 정면 120° 탄을 고양이 쪽으로 되튕김 (되튕긴 탄 피해 8, 기사는 피해 없음)", "정면에 4발 맞으면 거울이 산산조각 → 5초 동안 반사 없음, 정면도 정상 피해", "5초 뒤 거울이 다시 돋아남", "공격: 방패 밀치기 예고 0.7초 / 전방 3m 돌진 / 피해 16"], (bx, by, sz) => {
        const cy = by + sz / 2;
        D.mob(s, bx + sz - 0.45, cy, 0.15);
        s.addShape(pres.shapes.ARC, { x: bx + sz - 0.9, y: cy - 0.45, w: 0.9, h: 0.9, angleRange: [120, 240], line: { color: MIRROR, width: 4 } });
        D.cat(s, bx + 0.3, cy);
        D.line(s, bx + 0.45, cy - 0.1, sz - 1.4, 0, GOLD, { width: 1.5, end: "triangle" });
        D.line(s, bx + sz - 0.95, cy + 0.1, -(sz - 1.4), 0, GOLD, { width: 1.5, dash: "dash", end: "triangle" });
        text(s, "4발 → 깨짐", { x: bx + 0.35, y: by + 0.12, w: 1.1, h: 0.25, fontSize: 9, bold: true, color: TEAL, align: "center" });
      }],
      ["신기루 분신술사", "진짜를 찾아 쏘기", ["8초마다 분신 2개 (필드에 최대 2) + 셋이 0.6초 동안 자리를 섞음", "본체 / 분신 모두 3초마다 빛 탄 1발 (8m/초, 피해 12)", "분신은 한 발에 빛가루로 흩어짐", "본체를 잡으면 분신도 같이 사라짐"], (bx, by, sz) => {
        const cy = by + sz / 2;
        [-0.5, 0, 0.5].forEach((dx, k) => D.circle(s, bx + sz / 2 + dx, cy, 0.15, k === 1 ? LAV : PINK, { tr: k === 1 ? 0 : 50, line: k === 1 ? TXT : PINK, dash: k === 1 ? undefined : "dash" }));
        s.addShape(pres.shapes.ARC, { x: bx + sz / 2 - 0.55, y: cy - 0.55, w: 1.1, h: 1.1, angleRange: [200, 340], line: { color: GOLD, width: 1.5, endArrowType: "triangle" } });
        text(s, "?", { x: bx + sz / 2 - 0.2, y: cy + 0.2, w: 0.4, h: 0.3, fontSize: 12, bold: true, color: GOLD, align: "center" });
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
    s.addNotes("2장 트렁크 짐꾼도 같이 변경: 정면 탄 완전 차단 → 피해 33%만 받음 (완전 차단은 불쾌함)");
  }
  {
    const s = content(3, "강화형: 스테이지 3부터 등장");
    text(s, "중간 보스 처치 후의 하수인은 전부 강화형. 더 투명하고 일렁이는 모습 + 공격 하나 추가 + 체력 1.5배", { x: 0.6, y: 1.55, w: 12.1, h: 0.5, fontSize: 14, color: MUTED });
    const ups = [
      ["거울 사수+", "산탄 반사", "튕길 때 3갈래(25°)로 갈라짐"],
      ["빛가루 법사+", "구슬 폭발", "죽을 때 남은 구슬 자리마다 작은 탄 8방향 (5m/초, 피해 12)"],
      ["거울 너머 검객+", "2연타", "첫 베기 뒤 다른 거울이 0.5초 빛나고 한 번 더 튀어나와 벰"],
      ["유리 기사+", "조각 폭발", "거울이 깨질 때 0.5초 금이 번진 뒤 반경 2.5m 폭발, 피해 15"],
      ["신기루 분신술사+", "분신 3개", "분신이 터진 자리에 빛가루 장판 (반경 1.5m / 3초 / 이속 30% 감소)"],
    ];
    ups.forEach((u, i) => {
      const y = 2.2 + i * 0.93;
      card(s, 0.6, y, 12.1, 0.82, i % 2 ? CARD : PURPLE);
      text(s, u[0], { x: 0.85, y, w: 2.6, h: 0.82, fontSize: 13, bold: true, color: MUTED, valign: "middle" });
      text(s, u[1], { x: 3.45, y, w: 2.2, h: 0.82, fontSize: 17, bold: true, color: GOLD, valign: "middle" });
      text(s, u[2], { x: 5.7, y, w: 6.8, h: 0.82, fontSize: 13, valign: "middle" });
    });
  }

  // 04 중간 보스
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "신기루 마술사");
    picture(s, pic("ch3_midboss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "중간 보스 컨셉");
    caption(s, "중간 보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "거울 극장의 태엽 마술사. 분신 속 진짜 찾기와 거울 아래 시계로 판단을 시험함", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["등장", "스테이지 2 마지막 방 뒤에 이어 붙은 거울 극장 / 승리 시 뒷문 너머로 스테이지 3 연결"],
      ["보스맵", "28×24m / 뒷벽 무대 커튼 / 양옆 벽 전체가 거울 / 장애물 없음"],
      ["이동", "6~9m 거리 유지, 패턴 사이 가끔 빛가루로 흩어져 자리 이동 (이속 3.5m/초)"],
      ["체력", "3960 (목표 처치 시간 60초)"],
      ["페이즈", "1페이즈 / 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["고유 기믹", "진짜 찾기 (쿨 스킬) / 거울 시계 (체력 66 / 33%)"],
    ], 11.5);
  }
  {
    const s = content(4, "고유 기믹 1: 진짜 찾기");
    const steps = [
      ["① 섞기", "1초 동안 사격 봉인\n마술사 + 분신 4명이\n숫자 카드를 들고 섞임", PINK],
      ["② 찾기 6초", "발동 순간 내 발사 시각과\n같은 숫자를 든 게 진짜", GOLD],
      ["③ 기절", "진짜 명중: 3초 기절\n+ 분신 전부 소멸", TEAL],
    ];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.52;
      card(s, x, 1.65, 2.3, 1.85, i === 1 ? "4A3A20" : CARD);
      text(s, st[0], { x: x + 0.2, y: 1.78, w: 2.0, h: 0.4, fontSize: 15, bold: true, color: GOLD });
      text(s, st[1], { x: x + 0.2, y: 2.25, w: 2.0, h: 1.15, fontSize: 11.5, lineSpacingMultiple: 1.15 });
      if (i < 2) arrow(s, x + 2.32, 2.57, 0.18, GOLD);
    });
    const bx = 0.6, by = 3.75, bw = 7.3, bh = 3.05;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: FLOOR });
    const ccx = bx + bw / 2 + 0.4, ccy = by + bh / 2 - 0.1, R = 1.1;
    const nums = [3, 7, 10, 5, 1];
    nums.forEach((n, k) => {
      const a = (-90 + k * 72) * Math.PI / 180, px = ccx + Math.cos(a) * R, py = ccy + Math.sin(a) * R;
      D.circle(s, px, py, 0.17, k === 1 ? RED : PINK, { tr: k === 1 ? 0 : 45, line: k === 1 ? TXT : PINK, dash: k === 1 ? undefined : "dash" });
      D.rect(s, px + 0.17, py - 0.22, 0.26, 0.34, "F3EDE2", { line: GOLD });
      text(s, String(n), { x: px + 0.17, y: py - 0.22, w: 0.26, h: 0.34, fontSize: 10, bold: true, color: BG, align: "center", valign: "middle" });
    });
    const p7 = { x: ccx + Math.cos(-18 * Math.PI / 180) * R, y: ccy + Math.sin(-18 * Math.PI / 180) * R };
    D.cat(s, bx + 1.0, by + bh - 0.75, 0.16);
    clock(s, bx + 1.0, by + 0.65, 0.3, 7, { size: 13 });
    text(s, "내 발사 시각", { x: bx + 0.35, y: by + 1.0, w: 1.3, h: 0.25, fontSize: 9.5, color: MUTED, align: "center" });
    D.line(s, bx + 1.15, by + bh - 0.85, p7.x - bx - 1.35, p7.y - (by + bh - 0.85) + 0.1, GOLD, { width: 2, end: "triangle" });
    caption(s, "7시일 때 발동 → 7 카드를 든 마술사가 진짜", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 8.2, 1.65, 4.5, 5.2, PURPLE);
    text(s, "판정", { x: 8.45, y: 1.8, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "전투 15초 뒤 첫 발동 / 이후 25초마다",
      "카드 숫자는 서로 다른 1~12 / 반경 6m 원으로 늘어섬",
      "가짜 명중: 그 분신이 빛가루로 터지며 반격 탄 3갈래 (30° / 8m/초 / 피해 18)",
      "6초 안에 진짜를 못 맞히면 남은 분신이 전부 동시에 3갈래 사격 후 소멸",
      "거울 시계 진행 중엔 쿨이 멈춤",
    ], { x: 8.45, y: 2.3, w: 4.0, h: 2.9, fontSize: 12, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.45, y: 5.2, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "섞는 동안 사격을 막아 바늘이 바뀌는 예외를 없앰",
      "첫 발로 맞혀야 바늘이 밀리지 않음",
    ], { x: 8.45, y: 5.65, w: 4.0, h: 1.1, fontSize: 12, paraSpaceAfter: 2 });
  }
  {
    const s = content(4, "고유 기믹 2: 거울 시계");
    const ex = [["홀수: 시계 위에 서기", [[1.0, 0.7], [2.3, 1.5], [1.2, 2.1]], true], ["짝수: 모든 시계 피하기", [[1.0, 0.8], [2.4, 0.9], [1.3, 2.0], [2.5, 2.1]], false]];
    ex.forEach((e, i) => {
      const bx = 0.6 + i * 3.75, by = 1.6, bw = 3.55, bh = 3.0;
      D.rect(s, bx, by, bw, bh, NAVY, { line: MIRROR });
      e[1].forEach(([u, v]) => { D.circle(s, bx + u, by + v, 0.42, MIRROR, { tr: 70, line: WHITE }); clock(s, bx + u, by + v, 0.2, undefined, { tr: 30 }); });
      if (e[2]) { D.cat(s, bx + 2.3, by + 1.5, 0.12); D.circle(s, bx + 2.3, by + 1.5, 0.42, null, { line: TEAL, lineW: 2 }); }
      else { D.cat(s, bx + 1.9, by + 1.45, 0.12); }
      text(s, e[0], { x: bx, y: by + bh + 0.08, w: bw, h: 0.32, fontSize: 13, bold: true, color: GOLD, align: "center" });
      text(s, `시계 ${e[1].length}개`, { x: bx + 0.1, y: by + 0.08, w: 1.2, h: 0.28, fontSize: 10, bold: true, color: WHITE });
    });
    bullets(s, [
      "체력 66 / 33%에 한 번씩 확정 (진행 중인 패턴이 끝나면 바로 발동)",
      "1초 연출로 바닥 전체가 거울로 덮이고 거울 아래에 시계 1~6개(무작위)가 나타나 바늘이 돌기 시작",
      "바늘이 한 바퀴 다 도는 6초 안에 결정 / 판정은 6초 뒤 한 번",
      "실패: 최대 체력 30% 피해",
    ], { x: 0.6, y: 5.1, w: 7.3, h: 1.75, fontSize: 12.5, paraSpaceAfter: 3 });

    card(s, 8.2, 1.6, 4.5, 5.25, PURPLE);
    text(s, "시계 배치", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "반경 2m",
      "시계끼리 1m 이상 떨어져 겹치지 않음",
      "고양이에서 3~10m 거리",
      "진행 동안 마술사는 멈추고 무적, 다른 패턴 없음",
    ], { x: 8.45, y: 2.2, w: 4.0, h: 2.2, fontSize: 12.5, paraSpaceAfter: 3 });
    text(s, "기획의도", { x: 8.45, y: 4.5, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "체력 구간마다 맵 전체를 덮는 큰 기믹 (윌 거미줄 패턴 참고)",
      "세고 → 판단하고 → 이동하는 짧은 퍼즐",
    ], { x: 8.45, y: 4.95, w: 4.0, h: 1.8, fontSize: 12.5, paraSpaceAfter: 3 });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "카드 부채", "카드를 펼쳐 전방 부채꼴로 날림", "예고 0.6초 / 60° 부채꼴 7장\n8m/초 / 발당 피해 18", (bx, by, sz) => {
      const cx = bx + 0.3, cy = by + sz / 2;
      for (let k = -3; k <= 3; k++) { const a = k * 10 * Math.PI / 180; D.line(s, cx + 0.15, cy, Math.cos(a) * (sz - 0.6), Math.sin(a) * (sz - 0.6), "F3EDE2", { width: 1, end: "triangle" }); }
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "칼 꽂기 상자", "고양이 자리에 상자가 떨어지고, 상자에서 칼이 상하좌우로 박힘. 대각선만 안전", "원 예고 0.8초 / 반경 1.2m / 피해 18\n0.5초 뒤 칼 4방향 6m / 피해 20", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.rect(s, cx - 0.12, by + 0.08, 0.24, sz - 0.16, RED, { tr: 60, line: RED });
      D.rect(s, bx + 0.08, cy - 0.12, sz - 0.16, 0.24, RED, { tr: 60, line: RED });
      D.rect(s, cx - 0.2, cy - 0.2, 0.4, 0.4, "8A5A3A", { line: GOLD });
      D.cat(s, cx + 0.45, cy - 0.45, 0.09);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "모자 속 비둘기", "모자에서 빛 비둘기를 꺼내 고양이를 쫓게 함", "4마리 / 6m/초 / 초당 120° 유도\n4초 뒤 소멸 / 피해 18", (bx, by, sz) => {
      D.boss(s, bx + 0.3, by + sz / 2, 0.14);
      [[0.8, 0.4], [1.0, 0.75], [0.9, 1.1], [1.15, 1.3]].forEach(([u, v]) => { D.circle(s, bx + u, by + v, 0.07, WHITE); D.line(s, bx + u + 0.08, by + v, 0.18, 0, WHITE, { dash: "sysDot", width: 1, end: "triangle" }); });
      D.cat(s, bx + sz - 0.2, by + sz / 2, 0.09);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "거울 상자", "고양이 둘레에 거울벽 4면이 솟고 한 면만 열림. 마술사가 안으로 탄을 쏘아 벽에 튕김", "6×6m / 예고 0.8초 / 4초 유지\n탄 6발 / 8m/초 / 피해 18", (bx, by, sz) => {
      const m = 0.25;
      D.line(s, bx + m, by + m, sz - 2 * m, 0, MIRROR, { width: 3 });
      D.line(s, bx + m, by + m, 0, sz - 2 * m, MIRROR, { width: 3 });
      D.line(s, bx + m, by + sz - m, sz - 2 * m, 0, MIRROR, { width: 3 });
      D.cat(s, bx + sz / 2, by + sz / 2, 0.1);
      D.line(s, bx + sz - 0.1, by + 0.55, -0.9, 0.35, "FF8A80", { dash: "dash", width: 1.2 });
      D.line(s, bx + sz - 1.0, by + 0.9, 0.5, 0.5, "FF8A80", { dash: "dash", width: 1.2, end: "triangle" });
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "분신술사 호출", "신기루 분신술사를 불러냄", "2명 소환 / 필드 최대 4명\n필드 몬스터 2명 이하일 때만", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.45, -0.4], [0.45, -0.35]].forEach(([dx, dy]) => { D.circle(s, cx + dx, cy + dy, 0.17, PINK, { tr: 70, line: PINK, dash: "dash" }); D.mob(s, cx + dx, cy + dy); });
      D.boss(s, cx, cy + 0.25, 0.15);
      text(s, "+2", { x: cx - 0.25, y: by + 0.08, w: 0.5, h: 0.3, fontSize: 12, bold: true, color: GOLD, align: "center" });
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 5m 안",
      "거리에 맞는 패턴 + 거리 무관 패턴을 같은 확률로 추첨",
      "직전 패턴은 제외",
      "진짜 찾기는 25초 쿨 / 거울 시계는 체력 66 / 33%",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 11.5, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "거울 고래");
    picture(s, pic("ch3_boss.jpg") || BGIMG, 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "3장의 출구를 지키는 시간의 존재. 몸이 거울처럼 하늘을 비추고 등에 시계 문자판 무늬를 새긴 거대한 고래", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["보스맵", "호수 한가운데 둥근 거울 제단 / 지름 40m / 원형 경계(밖은 허공) / 둘레에 거울 조각 8개"],
      ["이동", "고양이 둘레를 크게 돌며 헤엄침 (이속 3m/초)"],
      ["체력", "7020 (목표 처치 시간 90초)"],
      ["페이즈", "2페이즈 / 체력 50%에 바닥 거울이 깨지고 공중을 날아다님"],
      ["상시 탄막", "반사 빛줄기 + 물결 링(1페이즈) / 기억 비(2페이즈)"],
      ["고유 기믹", "4종 무작위 반복 (쿨 20초)"],
    ], 11.5);
    s.addNotes("전용 모션(예정): 헤엄 / 잠수 / 꼬리 내려치기 / 울음 / 빨아들이기 / 비행 / 급강하");
  }
  {
    const s = content(5, "고유 기믹 4종: 쿨 20초 무작위");
    const rules = [
      ["쿨 20초", "전투 20초 뒤 첫 발동 / 4종 중 무작위 (직전 기믹 제외)"],
      ["기믹 우선", "쿨이 다 돌면 진행 중인 패턴이 끝나는 대로 바로 기믹"],
      ["쿨 정지", "기믹 진행 동안은 쿨이 돌지 않음 / 끝난 뒤부터 다시 20초"],
      ["기믹 중", "일반 패턴 없음 / 상시 탄막은 계속"],
    ];
    rules.forEach((r, i) => {
      const x = 0.6 + i * 3.06;
      card(s, x, 1.6, 2.92, 1.25, i % 2 ? CARD : PURPLE);
      text(s, r[0], { x: x + 0.2, y: 1.68, w: 2.6, h: 0.38, fontSize: 15, bold: true, color: GOLD });
      text(s, r[1], { x: x + 0.2, y: 2.06, w: 2.6, h: 0.75, fontSize: 11.5 });
    });
    const g = [
      ["① 거울에 비친 시각", "고래가 잠수하면 바닥에 좌우 반전된 시계가 비침. 보이는 n시 = 실제 12 − n시(6 / 12시는 그대로). 떠오를 자리 예고 원(반경 3m)에 실제 시각 탄을 맞히면 저지", "8초 / 성공: 3초 기절 / 실패: 반경 9m 파도, 최대 체력 40%", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        clock(s, cx - 0.35, cy, 0.3, 3, { fill: MIRROR, size: 13 });
        text(s, "→", { x: cx - 0.05, y: cy - 0.2, w: 0.3, h: 0.4, fontSize: 14, bold: true, color: GOLD, align: "center" });
        clock(s, cx + 0.5, cy, 0.3, 9, { size: 13 });
        text(s, "보임", { x: cx - 0.65, y: cy + 0.32, w: 0.6, h: 0.25, fontSize: 9, color: MUTED, align: "center" });
        text(s, "실제", { x: cx + 0.2, y: cy + 0.32, w: 0.6, h: 0.25, fontSize: 9, color: GOLD, align: "center" });
      }],
      ["② 고래의 노래", "고래가 멈추고 1~12번 울음(0.5초 간격, 울 때마다 경기장 둘레에 물결 한 줄, 숫자 표시 없음). 마지막 울음 뒤 머리가 빛나는 동안 울음 수 시각 탄을 머리(정면 ±60°)에 명중", "4초 / 성공: 3초 기절 / 실패: 경기장 전체 충격파, 최대 체력 40%", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        [0.3, 0.5, 0.7].forEach((r) => s.addShape(pres.shapes.ARC, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, angleRange: [300, 60], line: { color: SKY, width: 2 } }));
        D.boss(s, cx - 0.2, cy, 0.17);
        text(s, "×5", { x: cx + 0.25, y: by + 0.1, w: 0.5, h: 0.3, fontSize: 11, bold: true, color: GOLD });
      }],
      ["③ 흩어진 기억 조각", "경기장에 숫자 조각 6개(서로 다른 1~12, 고양이에서 4~14m). 조각은 자기 숫자 시각 탄에만 깨지고 다른 탄은 튕김. 10초 뒤 고래가 남은 조각을 삼키며 돌진(예고 선 1초)", "남은 조각 하나당 최대 체력 7% (최대 42%) / 다 깨면 3초 기절", (bx, by, sz) => {
        [[0.35, 0.4, 2], [0.95, 0.3, 11], [1.3, 0.8, 5], [0.4, 1.1, 8], [0.95, 1.25, 1]].forEach(([u, v, n]) => {
          s.addShape(pres.shapes.DIAMOND, { x: bx + u - 0.15, y: by + v - 0.2, w: 0.3, h: 0.4, fill: { color: MIRROR }, line: { color: WHITE, width: 1 } });
          text(s, String(n), { x: bx + u - 0.15, y: by + v - 0.2, w: 0.3, h: 0.4, fontSize: 9, bold: true, color: BG, align: "center", valign: "middle" });
        });
      }],
      ["④ 기억 삼키기", "예고 1초 → 2초 동안 입 앞 부채꼴에서 빨아들임(고양이를 2m/초로 끌어당김, 대시로 빠져나옴). 끝날 때 입 앞 3m 안이면 삼켜져 실린더 3칸이 사라짐(쏠 때 헛방)", "60° / 12m / 피해 25 / 옆구리 빛나는 부위 3곳에 2발씩 → 1칸씩 복귀, 12초 뒤 자동 복귀", (bx, by, sz) => {
        const cx = bx + 0.35, cy = by + sz / 2;
        D.pie(s, cx, cy, sz - 0.5, 330, 30, SKY, { tr: 65 });
        D.boss(s, cx, cy, 0.17);
        D.cat(s, cx + 0.95, cy, 0.09);
        D.line(s, cx + 0.85, cy, -0.4, 0, GOLD, { width: 1.5, end: "triangle" });
      }],
    ];
    g.forEach((it, i) => {
      const x = 0.6 + (i % 2) * 6.1, y = 3.0 + Math.floor(i / 2) * 1.95;
      card(s, x, y, 6.0, 1.85);
      const sz = 1.55;
      D.rect(s, x + 0.15, y + 0.15, sz, sz, FLOOR, { line: FLOOR });
      it[3](x + 0.15, y + 0.15, sz);
      text(s, it[0], { x: x + 1.85, y: y + 0.08, w: 4.0, h: 0.36, fontSize: 14, bold: true, color: GOLD });
      text(s, it[1], { x: x + 1.85, y: y + 0.44, w: 4.05, h: 0.95, fontSize: 9.5, lineSpacingMultiple: 1.05 });
      text(s, it[2], { x: x + 1.85, y: y + 1.38, w: 4.05, h: 0.42, fontSize: 9, color: MUTED });
    });
    s.addNotes("발사 시각 = 바늘 위치(다음에 쏠 실린더 칸 번호), 쏘면 한 칸 이동 / 장전하면 첫 칸으로 복귀. 1장(시각 맞추기) → 2장(+약점 명중) → 3장(+반전 읽기 / 세기 / 여러 개 처리)의 발전형. 2페이즈의 거울에 비친 시각은 고래가 하늘 위로 솟았다가 예고 원에 내리꽂힘");
  }
  {
    const s = content(5, "상시 탄막과 2페이즈");
    card(s, 0.6, 1.6, 5.4, 5.25);
    text(s, "상시 탄막 (두 개까지만 겹침)", { x: 0.85, y: 1.75, w: 5, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    const cx = 2.0, cy = 3.25, R = 1.0;
    D.circle(s, cx, cy, R + 0.15, NAVY, { line: MIRROR });
    const pts = [];
    for (let k = 0; k < 8; k++) { const a = (k * 45 + 10) * Math.PI / 180; pts.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R]); mirror(s, pts[k][0], pts[k][1], 0.25, k * 45 + 100); }
    [[0, 3], [3, 6], [6, 1]].forEach(([a, b]) => D.line(s, pts[a][0], pts[a][1], pts[b][0] - pts[a][0], pts[b][1] - pts[a][1], "FFF3B0", { width: 2 }));
    bullets(s, [
      { t: "반사 빛줄기 (내내)", b: true, c: GOLD },
      "둘레 거울 8개 사이를 튕기는 선 / 20초에 한 바퀴",
      "폭 0.6m / 0.5초마다 피해 15 / 거울을 쏘면 경로 변경",
    ], { x: 3.25, y: 2.2, w: 2.65, h: 2.0, fontSize: 10.5, paraSpaceAfter: 2 });
    bullets(s, [
      { t: "물결 링 (1페이즈)", b: true, c: GOLD },
      "수면을 칠 때마다 + 최소 6초마다 / 5m/초 / 폭 0.5m / 피해 15 / 대시로 넘음",
      { t: "기억 비 (2페이즈, 물결 링 대신)", b: true, c: GOLD },
      "초당 2방울 / 고양이 주변 8m / 원 예고 0.8초 / 반경 1m / 피해 15",
    ], { x: 0.85, y: 4.45, w: 5.0, h: 2.3, fontSize: 11.5, paraSpaceAfter: 2 });

    text(s, "2페이즈 (체력 50%)", { x: 6.3, y: 1.65, w: 5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const ox = 7.4, oy = 3.1;
    D.circle(s, ox, oy, 1.0, "7FB8E0", { tr: 30, line: MIRROR });
    [[-0.5, -0.3, 0.5, 0.6], [0.1, -0.7, 0.4, 0.9], [-0.2, 0.2, 0.9, -0.1]].forEach(([x1, y1, dx, dy]) => D.line(s, ox + x1, oy + y1, dx, dy, WHITE, { width: 1.5 }));
    D.boss(s, ox + 0.2, oy - 0.2, 0.2);
    text(s, "거울이 깨진 하늘", { x: ox - 1.0, y: oy + 1.08, w: 2.0, h: 0.28, fontSize: 10, color: MUTED, align: "center" });
    bullets(s, [
      "바닥 거울이 깨져 아래 하늘이 보임 (고양이는 그대로 설 수 있음)",
      "고래가 잠수 대신 공중을 날아다님",
      "이속 / 패턴 간격 1.2배",
      "일반 패턴 묶음이 공중형으로 바뀜",
      "물결 링 → 기억 비",
    ], { x: 8.65, y: 2.1, w: 4.1, h: 2.5, fontSize: 12, paraSpaceAfter: 3 });

    text(s, "패턴 고르기", { x: 6.3, y: 4.75, w: 2.0, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, "거리에 맞는 패턴 같은 확률 / 직전 패턴 제외 / 거리 무관 자리는 기믹 4종", { x: 8.3, y: 4.79, w: 4.4, h: 0.32, fontSize: 10, color: MUTED, valign: "middle" });
    const groups = [["1페이즈 가까울 때", ["꼬리 내려치기", "소용돌이"], RED], ["1페이즈 멀 때", ["잠수 돌진", "거울 파편 뿌리기"], PURPLE], ["2페이즈 가까울 때", ["지느러미 휩쓸기", "빛 브레스"], RED], ["2페이즈 멀 때", ["급강하", "하늘 가르기"], PURPLE]];
    groups.forEach((gr, i) => {
      const y = 5.2 + i * 0.41;
      text(s, gr[0], { x: 6.3, y, w: 2.1, h: 0.36, fontSize: 11, bold: true, valign: "middle" });
      gr[1].forEach((p, k) => chip(s, p, 8.45 + k * 2.15, y + 0.03, 2.05, gr[2], TXT, { size: 10, h: 0.3 }));
    });
    s.addNotes("'가까움' = 7m 안. 2페이즈 이속 3.6m/초");
  }
  {
    const s = content(5, "보스 패턴: 1페이즈 수면형");
    const W = 5.98, H = 2.45, X = [0.6, 6.72], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "꼬리 내려치기", "뒤쪽을 꼬리로 내려치고 물보라 탄을 흩뿌림", "뒤쪽 부채꼴 120° / 7m / 예고 0.8초 / 피해 26\n물보라 탄 8발 / 5m/초 / 피해 15", (bx, by, sz) => {
      const cx = bx + sz * 0.7, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.6, 120, 240, RED, { tr: 55 });
      D.boss(s, cx, cy, 0.15);
      D.cat(s, cx + 0.3, cy - 0.4, 0.09);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "소용돌이", "몸 주변으로 끌어당긴 뒤 가까운 원을 터뜨림. 끌려가지 않게 바깥으로 이동", "예고 0.8초 / 반경 8m에서 1.5초 끌어당김(2.5m/초)\n→ 반경 4m 폭발 / 피해 26", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.48, SKY, { tr: 80, line: SKY, dash: "dash" });
      D.circle(s, cx, cy, sz * 0.25, RED, { tr: 55, line: RED });
      D.boss(s, cx, cy, 0.13);
      D.cat(s, cx + sz * 0.38, cy, 0.08);
      D.line(s, cx + sz * 0.33, cy, -0.25, 0, GOLD, { width: 1.5, end: "triangle" });
    }, "가까울 때");
    D.patternCard(s, X[0], Y[1], W, H, "잠수 돌진", "거울 아래로 잠수해 그림자가 고양이 쪽 직선을 예고한 뒤 돌진, 끝에서 튀어오름", "직선 폭 4m / 예고 1초 / 18m/초 / 피해 28\n끝에서 튀어오름 반경 3m / 피해 22", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + 0.05, cy - 0.2, sz - 0.1, 0.4, RED, { tr: 65, line: RED });
      D.circle(s, bx + 0.3, cy, 0.15, "334455", { tr: 30, line: SKY });
      D.arrow(s, bx + 0.5, cy, sz - 0.95, GOLD, { width: 2.5 });
      D.circle(s, bx + sz - 0.3, cy, 0.25, RED, { tr: 55, line: RED });
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "거울 파편 뿌리기", "떠다니는 거울 조각을 더 불러내 빛줄기와 탄이 더 많이 튕기게 함. 불러낼 때 물방울 탄을 사방으로", "거울 조각 4개 추가 / 10초 유지\n물방울 탄 12방향 / 6m/초 / 피해 15", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      for (let k = 0; k < 12; k++) { const a = k * 30 * Math.PI / 180; D.circle(s, cx + Math.cos(a) * 0.55, cy + Math.sin(a) * 0.55, 0.04, SKY); }
      [[-0.45, -0.5, 30], [0.5, -0.4, -30], [-0.4, 0.5, 70], [0.45, 0.5, 10]].forEach(([dx, dy, r]) => mirror(s, cx + dx, cy + dy, 0.35, r));
      D.boss(s, cx, cy, 0.14);
    }, "멀 때");
    D.legend(s, 0.6, 6.74);
  }
  {
    const s = content(5, "보스 패턴: 2페이즈 공중형");
    const W = 5.98, H = 2.45, X = [0.6, 6.72], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "지느러미 휩쓸기", "낮게 원을 그리며 선회해 도넛 모양 범위를 쓸어 냄. 안쪽 원과 바깥만 안전", "반경 2.5~8m 링 / 예고 0.8초 / 피해 26", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.46, RED, { tr: 55, line: RED });
      D.circle(s, cx, cy, sz * 0.16, TEAL, { tr: 35, line: TEAL });
      D.boss(s, cx, cy, 0.1);
      D.cat(s, cx + 0.1, cy + 0.08, 0.07);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "빛 브레스", "입에서 광선을 뿜어 부채꼴로 쓸고 지나감. 광선 반대쪽으로 돌아 피함", "예고 1초 / 폭 1.5m / 길이 16m\n2초 동안 90° / 0.25초마다 피해 12", (bx, by, sz) => {
      const cx = bx + 0.3, cy = by + sz / 2;
      D.pie(s, cx, cy, sz - 0.4, 315, 45, "FFF3B0", { tr: 70 });
      D.line(s, cx, cy, sz - 0.6, -0.4, "FFF3B0", { width: 4 });
      s.addShape(pres.shapes.ARC, { x: cx - 0.7, y: cy - 0.7, w: 1.4, h: 1.4, angleRange: [330, 30], line: { color: GOLD, width: 1.5, endArrowType: "triangle" } });
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[0], Y[1], W, H, "급강하", "그림자 원을 예고한 뒤 내리꽂히고 충격파 링이 퍼짐", "그림자 원 반경 3.5m / 예고 1.2초 / 피해 30\n충격파 링 6m/초 / 피해 18 / 대시로 넘음", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.25, RED, { tr: 50, line: RED });
      D.circle(s, cx, cy, sz * 0.45, null, { line: "FF8A80", lineW: 2, dash: "dash" });
      D.boss(s, cx, cy, 0.12);
      D.cat(s, cx + sz * 0.38, cy - 0.2, 0.08);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "하늘 가르기", "고양이 위치를 지나는 직선을 예고한 뒤 경기장을 낮게 가로질러 통과", "폭 5m 직선 / 예고 1.5초\n30m/초 / 피해 28", (bx, by, sz) => {
      const cy = by + sz / 2;
      s.addShape(pres.shapes.RECTANGLE, { x: bx + 0.05, y: cy - 0.28, w: sz - 0.1, h: 0.56, rotate: -20, fill: { color: RED, transparency: 65 }, line: { color: RED } });
      D.cat(s, bx + sz / 2, cy, 0.09);
      D.boss(s, bx + 0.2, cy + 0.35, 0.13);
    }, "멀 때");
    D.legend(s, 0.6, 6.74);
  }

  // 06 수치
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "3장 수치 정리");
    D.table(s, [
      ["대상", "체력", "이속", "피해", "비고"],
      ["거울 사수", "108", "2.8m/초", "12", "8~11m 유지 / 반사탄 2.5초마다"],
      ["빛가루 법사", "108", "2.6m/초", "12", "구슬 5개 6초마다 / 유도"],
      ["거울 너머 검객", "144", "3.6m/초", "16", "거울 잠복 2초 / 예고 0.7초"],
      ["유리 기사", "288", "2.6m/초", "16", "정면 반사 / 4발에 깨짐 / 5초"],
      ["신기루 분신술사", "108", "3.0m/초", "12", "분신 2개 8초마다"],
      ["강화형", "기본 ×1.5", "-", "12~16", "공격 하나 추가"],
      ["신기루 마술사", "3960", "3.5m/초", "18~22", "목표 60초 / 거울 극장 28×24m"],
      ["거울 고래", "7020", "3m/초", "15~30", "목표 90초 / 원형 지름 40m"],
    ], 0.6, 1.6, [2.3, 1.45, 1.25, 1.0, 3.4], { rowH: 0.43, size: 11.5 });
    const cards = [
      ["피해 기준", "2장보다 약 25% 상향\n일반 12~18 / 중간 보스 18~22\n보스 22~30 (기믹은 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 20 × 명중률 60%\n중간 보스 시점 ×5.5 / 60초\n보스 시점 ×6.5 / 90초"],
      ["전투방", "웨이브당 적: 스테이지 1은 6~8마리\n→ 스테이지 4는 8~11마리\n거울 조각 3개 / 0.8m/초"],
      ["오브젝트", "기억 수정 반경 3m / 적 50 · 고양이 18\n분수 샘 반경 1.5m / 3초 / 쿨 8초\n엄폐물 내구 40"],
    ];
    cards.forEach((c, i) => {
      const y = 1.6 + i * 1.32;
      card(s, 10.2, y, 2.5, 1.2, i % 2 ? CARD : PURPLE);
      text(s, c[0], { x: 10.35, y: y + 0.08, w: 2.3, h: 0.3, fontSize: 12.5, bold: true, color: GOLD });
      text(s, c[1], { x: 10.35, y: y + 0.38, w: 2.3, h: 0.8, fontSize: 9.5, lineSpacingMultiple: 1.1 });
    });
    text(s, "일반 몬스터 체력 = 3장 시작 화력(×4.5, 기본 탄 한 발 36) 기준으로 앞 챕터와 비슷한 발 수", { x: 0.6, y: 6.02, w: 9.3, h: 0.32, fontSize: 12, color: MUTED });
    text(s, "중간 보스 3960 = 20 × 5.5 × 60초 × 0.6  /  보스 7020 = 20 × 6.5 × 90초 × 0.6", { x: 0.6, y: 6.42, w: 9.3, h: 0.4, fontSize: 13, bold: true });
  }
  {
    const s = content(6, "공통 변경: 상인 / 2장 짐꾼");
    card(s, 0.6, 1.6, 7.3, 5.25, PURPLE);
    text(s, "상인 공통 규칙 (모든 챕터)", { x: 0.85, y: 1.75, w: 6.8, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "방을 비울 때마다 15% (스테이지 끝 방만이 아님)",
      "보장: 스테이지 1 / 2에서 한 번도 못 만났으면 스테이지 2 마지막 방(중간 보스 직전)에 확정",
      "고양이 / 카메라에서 보이는 쪽에 솟고, 화면 밖이면 화면 가장자리 화살표 + 토스트로 위치 표시",
      "상인 모습: 1장 술집 바텐더 / 2장 매표소 직원 / 3장 거울 속 고양이",
      "판매: 총탄 / 총 / 연산 / 하트(25골드 / 3개 / 최대 체력 20%)",
    ], { x: 0.85, y: 2.25, w: 6.85, h: 3.4, fontSize: 13.5, paraSpaceAfter: 5 });
    text(s, "기존 구현은 스테이지 보상 뒤에만 15%, 보장은 스테이지 3 끝이라 상인을 거의 못 만나던 문제를 고침", { x: 0.85, y: 5.85, w: 6.85, h: 0.8, fontSize: 11.5, color: MUTED });

    card(s, 8.2, 1.6, 4.5, 5.25);
    text(s, "2장 트렁크 짐꾼", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "정면 120° 탄: 완전 차단 → 피해 33%만 받음",
      "옆 / 뒤, 내리찍은 뒤 1.2초 빈틈은 그대로 정상 피해",
      "3장 유리 기사(정면 반사 → 4발에 깨짐)와 역할이 겹치지 않게, 짐꾼은 '덜 들어가는 방패'",
    ], { x: 8.45, y: 2.25, w: 4.05, h: 3.2, fontSize: 13, paraSpaceAfter: 5 });
    text(s, "완전 차단은 플레이가 불쾌하다는 피드백 반영", { x: 8.45, y: 5.85, w: 4.05, h: 0.8, fontSize: 11.5, color: MUTED });
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
