// 11:59 2장 기획서 (docs/Cat_Tang_레벨_디자인.md 챕터 2 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_ch2_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 1장 Ver02 규칙대로 나열 구분자는 " / "
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_2장_기획서_20261117_정승운_${VER}.pptx`;
const STEAM = "BFC9D1", COAL = "3A3440", AMBER = "E8B23A", GREEN = "4FA36B";

function build() {
  const SEC = ["2장 개요", "스테이지 구성", "일반 몬스터", "중간 보스", "보스", "수치 기획"];
  const D = makeDeck(`11:59 2장 기획서 ${VER}`, `11:59 / 2장 기획서: 멈춘 기차역 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.17"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;
  const BGIMG = has("ch2/ch2_bg.jpg") ? "ch2/ch2_bg.jpg" : "05_zone1_western.png";
  const infoRows = (s, rows, fs = 12) => rows.forEach((r, i) => {
    const y = 1.6 + i * 0.88;
    card(s, 8.15, y, 4.55, 0.78, i % 2 ? CARD : PURPLE);
    text(s, r[0], { x: 8.35, y, w: 1.1, h: 0.78, fontSize: 13, bold: true, color: GOLD, valign: "middle" });
    text(s, r[1], { x: 9.45, y, w: 3.15, h: 0.78, fontSize: fs, valign: "middle", lineSpacingMultiple: 1.05 });
  });
  // 선로 한 줄 (가로)
  const rail = (s, x, y, w, color = "8A7A66") => {
    D.line(s, x, y - 0.07, w, 0, color, { width: 1.25 }); D.line(s, x, y + 0.07, w, 0, color, { width: 1.25 });
    for (let k = 0; k <= Math.floor(w / 0.15); k++) D.line(s, x + k * 0.15, y - 0.11, 0, 0.22, "5C4E40", { width: 1 });
  };

  const cv = D.cover(BGIMG, "2장 기획서", "멈춘 기차역 / 중간 보스 / 보스 상세", "Ver 01");
  cv.addNotes("공통 규칙(챕터 구조 / 전투방 / 보스 세 박자 / 수치 기준)은 공통 레벨 디자인 기획서를 따름. 방 크기 / 웨이브 / 보상 / 방 사이 문 규칙은 1장과 같음");
  D.toc(has("ch2/ch2_boss.jpg") ? "ch2/ch2_boss.jpg" : BGIMG);

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "멈춘 기차역");
    picture(s, BGIMG, 0.6, 1.6, 7.3, 4.1, "2장 배경");
    caption(s, "2장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "출발 직전 시간이 멈춘 역. 1장의 '녹음'에 이어 '정지'가 모티브인 두 번째 챕터", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["테마", "공중에 멈춘 증기 / 떨어지다 멈춘 짐 / 굳은 승객 실루엣"],
      ["오브젝트", "멈춘 증기 밸브 / 공중에 멈춘 짐 / 선로 위 광차 / 부서지는 엄폐물 / 지나가는 열차"],
      ["상인", "매표소 직원 (바닥에서 매표 창구가 솟아오름)"],
      ["일반 몬스터", "태엽 차장 / 트렁크 짐꾼 / 신호수"],
      ["중간 보스", "태엽 역장"],
      ["보스", "멈춘 기관차 거인"],
    ], 12);
  }

  // 02 스테이지 구성
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "역 바깥 야적장에서 전차대로");
    const flow = [
      ["스테이지 1", "화물 야적장", PURPLE], ["스테이지 2", "승강장", PURPLE], ["중간 보스", "대합실", "8C3A50"],
      ["스테이지 3", "멈춘 객차 안", PURPLE], ["스테이지 4", "기관차고", PURPLE], ["보스", "전차대", RED],
    ];
    flow.forEach((f, i) => {
      const x = 0.6 + i * 2.06;
      card(s, x, 1.65, 1.76, 1.1, f[2]);
      text(s, f[0], { x, y: 1.72, w: 1.76, h: 0.4, fontSize: 12.5, bold: true, color: GOLD, align: "center" });
      text(s, f[1], { x, y: 2.1, w: 1.76, h: 0.5, fontSize: 14, bold: true, align: "center" });
      if (i < 5) arrow(s, x + 1.79, 2.2, 0.22, GOLD);
    });
    s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: 2.95, w: 12.1, h: 0.12, fill: { color: LAV, transparency: 40 }, line: { color: LAV, transparency: 100 } });
    s.addShape(pres.shapes.RECTANGLE, { x: 6.8, y: 2.95, w: 5.9, h: 0.12, fill: { color: STEAM, transparency: 10 }, line: { color: STEAM, transparency: 100 } });
    text(s, "역 중심에 가까워질수록 더 많은 것이 공중에 굳어 있는 모습", { x: 0.6, y: 3.12, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED, align: "center" });

    picture(s, has("ch2/ch2_room.jpg") ? "ch2/ch2_room.jpg" : BGIMG, 0.6, 3.65, 5.6, 3.15, "전투방 컨셉");
    bullets(s, [
      { t: "전투방", b: true, c: GOLD },
      "방 크기 28×24m / 웨이브 2~3번 / 다음 웨이브 1초 예고 (1장과 같은 규칙)",
      "객차 방: 옆면이 뚫린 객차 2~3량이 나란히 서 있고 그 사이와 안을 오가며 전투",
      "전투 종료 시 15% 확률로 바닥에서 매표 창구가 솟아오르며 상인 등장",
      { t: "장소별 오브젝트", b: true, c: GOLD },
      "야적장: 광차 / 멈춘 짐      승강장: 증기 밸브 / 멈춘 짐",
      "객차: 멈춘 짐 / 엄폐물      기관고: 증기 밸브 / 광차",
      "엄폐물(짐가방 더미 / 벤치 / 화물 상자)은 모든 방",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 12.5, paraSpaceAfter: 4 });
  }
  {
    const s = content(2, "쏘면 다시 흐르는 시간: 오브젝트");
    const W = 2.92, H = 3.15;
    const objs = [
      ["멈춘 증기 밸브", "쏘면 멈춘 시간이 풀려 정면으로 증기 분출", "승강장 / 기관고 · 방마다 2~3개\n0.5초 뒤 6m × 폭 1.5m · 1.5초\n피해 적 40 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.circle(s, bx + 0.3, cy, 0.17, "6B6F78", { line: STEAM });
        D.rect(s, bx + 0.48, cy - 0.17, sz - 0.62, 0.34, STEAM, { tr: 40, line: STEAM });
        for (let k = 0; k < 4; k++) D.circle(s, bx + 0.75 + k * 0.28, cy + (k % 2 ? 0.05 : -0.05), 0.09, "FFFFFF", { tr: 55, line: STEAM });
        D.mob(s, bx + sz - 0.35, cy);
      }],
      ["공중에 멈춘 짐", "쏘면 그림자가 커지며 아래로 낙하", "야적장 / 승강장 / 객차 · 2~4개\n0.6초 뒤 낙하 · 반경 1.5m\n피해 적 40 / 고양이 15", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy + 0.3, 0.42, RED, { tr: 60, line: RED, dash: "dash" });
        D.rect(s, cx - 0.22, cy - 0.55, 0.44, 0.3, "8A5A3A", { line: TXT });
        D.line(s, cx, cy - 0.2, 0, 0.35, GOLD, { end: "triangle", dash: "dash" });
        D.mob(s, cx + 0.2, cy + 0.38);
      }],
      ["선로 위 광차", "쏘면 선로를 따라 질주하며 경로의 적을 들이받음", "야적장 / 기관고 · 1~2개\n12m/초 질주 → 끝에서 부서짐\n피해 적 50 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        rail(s, bx + 0.1, cy, sz - 0.2);
        D.rect(s, bx + 0.2, cy - 0.16, 0.36, 0.32, "8A6440", { line: TXT });
        D.arrow(s, bx + 0.62, cy, sz - 1.05, GOLD, { width: 2 });
        D.mob(s, bx + sz - 0.3, cy);
      }],
      ["지나가는 열차", "멈춘 열차가 주기적으로 잠깐 풀려 선로를 훑고 지나감. 적도 피해", "야적장 / 승강장 / 기관고\n선로 폭 3m · 12초마다 · 예고 1.5초\n피해 적 60 / 고양이 20", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.rect(s, bx + 0.05, cy - 0.3, sz - 0.1, 0.6, AMBER, { tr: 70, line: AMBER });
        rail(s, bx + 0.05, cy, sz - 0.1);
        D.rect(s, bx + 0.12, cy - 0.22, 0.55, 0.44, COAL, { line: TXT });
        D.arrow(s, bx + 0.72, cy, sz - 0.95, GOLD, { width: 2.5 });
        text(s, "기적!", { x: bx + 0.1, y: by + 0.12, w: 0.8, h: 0.25, fontSize: 9, bold: true, color: AMBER });
      }],
    ];
    objs.forEach((o, i) => {
      const x = 0.6 + i * 3.06, y = 1.6;
      card(s, x, y, W, H);
      const sz = 1.75;
      D.rect(s, x + 0.2, y + 0.2, W - 0.4, sz, FLOOR, { line: FLOOR });
      o[3](x + 0.2 + (W - 0.4 - sz) / 2, y + 0.2, sz);
      text(s, o[0], { x: x + 0.2, y: y + 2.05, w: W - 0.4, h: 0.36, fontSize: 15, bold: true, color: GOLD });
      text(s, o[1], { x: x + 0.2, y: y + 2.42, w: W - 0.4, h: 0.7, fontSize: 11.5 });
      text(s, o[2], { x: 0.6 + i * 3.06 + 0.2, y: 4.85, w: W - 0.4, h: 0.85, fontSize: 10.5, color: MUTED });
    });
    D.card(s, 0.6, 5.85, 12.1, 0.95, PURPLE);
    text(s, [{ text: "공통  ", options: { bold: true, color: GOLD } }, { text: "적과 고양이 모두 피해 / 쏘는 순간 예고 표시(증기 방향 / 낙하 그림자 / 광차 경로 / 선로 빛) / 후보 자리 중 일부에만 배치" }], { x: 0.85, y: 5.85, w: 11.7, h: 0.95, fontSize: 13, valign: "middle" });
    D.legend(s, 8.0, 1.2);
  }

  // 03 일반 몬스터
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시간의 하수인 3종");
    picture(s, has("ch2/ch2_enemies.jpg") ? "ch2/ch2_enemies.jpg" : BGIMG, 0.6, 1.6, 12.1, 2.7, "일반 몬스터 컨셉");
    const mobs = [
      ["태엽 차장", "원거리", "7~10m 유지 / 구멍 난 승차권 3갈래 투척", ["이속 2.8m/초", "3갈래(30°) 2.5초마다 / 예고 0.6초", "탄속 9m/초 / 발당 피해 10 / 체력 60 (3발)"]],
      ["트렁크 짐꾼", "방패 근접", "트렁크로 정면 탄 차단 / 다가와 내리찍기", ["이속 3.2m/초 / 정면 120° 탄 막음", "내리찍기 예고 0.7초 / 전방 90° 2.5m / 피해 15", "내리찍은 뒤 1.2초 빈틈 / 체력 160 (8발)"]],
      ["신호수", "설치형", "고양이 주변에 신호등 원 설치 → 노랑에서 빨강으로 바뀌면 폭발", ["이속 3.0m/초 / 8m 유지", "4초마다 2개 / 노랑 1.5초 → 폭발", "반경 2m / 피해 12 / 체력 60 (3발)"]],
    ];
    mobs.forEach((m, i) => {
      const x = 0.6 + i * 4.1;
      card(s, x, 4.45, 3.9, 2.4);
      text(s, m[0], { x: x + 0.25, y: 4.58, w: 2.6, h: 0.4, fontSize: 17, bold: true, color: GOLD });
      chip(s, m[1], x + 2.65, 4.62, 1.05, [TEAL, "7A6A55", RED][i], i === 0 ? BG : TXT, { size: 11, h: 0.32 });
      text(s, m[2], { x: x + 0.25, y: 5.02, w: 3.5, h: 0.42, fontSize: 12.5 });
      bullets(s, m[3], { x: x + 0.25, y: 5.5, w: 3.5, h: 1.3, fontSize: 11.5, paraSpaceAfter: 3, color: MUTED });
    });
  }
  {
    const s = content(3, "강화형: 스테이지 3부터 등장");
    text(s, "중간 보스 처치 후의 하수인은 전부 강화형. 더 굳고 일그러진 모습 + 공격 하나 추가 + 체력 1.5배", { x: 0.6, y: 1.55, w: 12.1, h: 0.5, fontSize: 14, color: MUTED });
    const W = 3.95, H = 4.6;
    const ups = [
      ["태엽 차장+", "검표 펀치", "구멍 뚫는 펀치 탄 한 발. 맞으면 잠깐 대시 충전 정지", "6초 쿨 / 예고 0.6초 / 피해 10 + 2초 대시 충전 정지", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.mob(s, bx + 0.3, cy, 0.14);
        D.line(s, bx + 0.45, cy, sz - 0.95, 0, RED, { dash: "dash", end: "triangle" });
        D.cat(s, bx + sz - 0.35, cy);
        [0, 1, 2].forEach((k) => D.circle(s, bx + sz - 0.65 + k * 0.3, cy + 0.45, 0.09, k === 2 ? CARD : GOLD, { line: GOLD }));
        text(s, "대시 정지", { x: bx + sz - 0.95, y: cy + 0.6, w: 1.0, h: 0.25, fontSize: 9, bold: true, color: GOLD, align: "center" });
      }],
      ["트렁크 짐꾼+", "트렁크 던지기", "방패인 트렁크를 일직선으로 던짐. 던진 뒤엔 무방비", "8초 쿨 / 예고 0.8초 / 직선 10m / 피해 15 / 3초 방패 없음", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.rect(s, bx + 0.15, cy - 0.15, sz - 0.3, 0.3, RED, { tr: 65, line: RED });
        D.rect(s, bx + 0.95, cy - 0.13, 0.32, 0.26, "8A5A3A", { line: TXT });
        D.arrow(s, bx + 1.3, cy, 0.55, GOLD, { width: 2 });
        D.mob(s, bx + 0.32, cy, 0.14);
        text(s, "무방비", { x: bx + 0.05, y: cy + 0.25, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: TEAL });
      }],
      ["신호수+", "정지 구역", "신호등이 터진 자리에 시간이 멈춘 구역. 들어온 탄이 멈췄다가 끝날 때 다시 날아감", "반경 2m / 3초 / 적 탄 · 내 탄 모두 정지", (bx, by, sz) => {
        const cx = bx + sz / 2, cy = by + sz / 2;
        D.circle(s, cx, cy, sz * 0.38, TEAL, { tr: 60, line: TEAL, dash: "dash" });
        [[-0.3, -0.15], [0.05, 0.2], [0.3, -0.25], [-0.1, -0.4]].forEach(([dx, dy], k) => {
          D.circle(s, cx + dx, cy + dy, 0.05, k % 2 ? GOLD : "FF8A80");
          D.line(s, cx + dx - 0.2, cy + dy, 0.12, 0, k % 2 ? GOLD : "FF8A80", { width: 1, dash: "sysDot" });
        });
        text(s, "정지", { x: cx - 0.3, y: by + 0.08, w: 0.6, h: 0.25, fontSize: 10, bold: true, color: TEAL, align: "center" });
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
      text(s, u[2], { x: x + 0.25, y: y + 3.25, w: W - 0.5, h: 0.75, fontSize: 13 });
      text(s, u[3], { x: x + 0.25, y: y + 4.02, w: W - 0.5, h: 0.5, fontSize: 11, color: MUTED });
    });
  }

  // 04 중간 보스
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "태엽 역장");
    picture(s, has("ch2/ch2_midboss.jpg") ? "ch2/ch2_midboss.jpg" : BGIMG, 0.6, 1.6, 7.3, 4.1, "중간 보스 컨셉");
    caption(s, "중간 보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "멈춘 역의 질서를 지키는 역장. 호루라기 한 번으로 모든 움직임을 멈춰 세우는 근거리 압박형", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["등장", "스테이지 2 마지막 방 뒤 대합실 / 승리 시 승강장 쪽 문으로 스테이지 3 연결"],
      ["경기장", "대합실 28×24m / 긴 벤치 줄 = 엄폐물 (보스 탄에 부서짐)"],
      ["이동", "신호봉을 들고 고양이를 따라오며 거리를 좁힘 (이속 3.5m/초)"],
      ["체력", "2520 (목표 처치 시간 60초)"],
      ["페이즈", "1페이즈 / 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["고유 기믹", "빨간불"],
    ]);
  }
  {
    const s = content(4, "고유 기믹: 빨간불");
    const steps = [
      ["① 노란불", "25초마다 호루라기\n노란불 1초 예고", AMBER],
      ["② 빨간불 3초", "움직이면(대시 포함) 피해\n느린 표 탄 6발이 날아옴", RED],
      ["③ 초록불", "다시 자유롭게 이동", GREEN],
    ];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.52;
      card(s, x, 1.65, 2.3, 1.85, i === 1 ? "5A2430" : CARD);
      D.circle(s, x + 2.0, 1.95, 0.14, st[2], { line: TXT });
      text(s, st[0], { x: x + 0.2, y: 1.78, w: 1.7, h: 0.4, fontSize: 15, bold: true, color: GOLD });
      text(s, st[1], { x: x + 0.2, y: 2.25, w: 2.0, h: 1.15, fontSize: 12, lineSpacingMultiple: 1.15 });
      if (i < 2) arrow(s, x + 2.32, 2.57, 0.18, GOLD);
    });
    // 도식: 제자리에서 표 탄 격추
    const bx = 0.6, by = 3.75, bw = 7.3, bh = 3.05;
    D.rect(s, bx, by, bw, bh, FLOOR, { line: FLOOR });
    const cx = bx + 1.4, cy = by + bh / 2;
    D.cat(s, cx, cy, 0.17);
    D.circle(s, cx, cy, 0.38, null, { line: RED, lineW: 1.5, dash: "dash" });
    text(s, "움직이면 피해", { x: cx - 0.8, y: cy + 0.45, w: 1.6, h: 0.28, fontSize: 10, bold: true, color: RED, align: "center" });
    D.boss(s, bx + bw - 0.8, cy, 0.25);
    D.rect(s, bx + bw - 1.0, by + 0.2, 0.4, 0.75, COAL, { line: TXT });
    D.circle(s, bx + bw - 0.8, by + 0.4, 0.12, RED, { line: RED });
    D.circle(s, bx + bw - 0.8, by + 0.75, 0.12, "3A2A2A", { line: "5C4E40" });
    const tick = [[-0.9, -0.35], [-0.6, 0.6], [-1.2, 0.95], [-0.2, -0.95], [0.5, 0.2], [0.9, -0.6]];
    tick.forEach(([dx, dy], k) => {
      const tx = cx + 2.6 + dx * 1.1, ty = cy + dy * 0.9;
      D.rect(s, tx - 0.11, ty - 0.07, 0.22, 0.14, "E8DCC0", { line: GOLD });
      if (k < 3) D.line(s, cx + 0.2, cy, tx - 0.15 - cx - 0.2, ty - cy, GOLD, { dash: "sysDot", width: 1 });
    });
    caption(s, "빨간불: 제자리에서 날아오는 표 탄을 쏴서 떨어뜨림", bx, by + bh - 0.32, bw, { align: "center" });

    card(s, 8.2, 1.65, 4.5, 5.2, PURPLE);
    text(s, "판정", { x: 8.45, y: 1.8, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "빨간불 중 이동 / 대시 시 최대 체력 20% 피해 (한 번)",
      "조준 / 사격 / 장전은 자유",
      "표 탄: 6발 / 4m/초 / 쏘면 부서짐 / 맞으면 피해 15",
    ], { x: 8.45, y: 2.3, w: 4.0, h: 1.9, fontSize: 13 });
    text(s, "노리는 것", { x: 8.45, y: 4.3, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "이동 / 대시로 피하던 1장과 반대로 제자리 조준 실력을 시험",
      "1장 표식(바늘 피하기) → 2장 빨간불(움직임 멈추기)",
    ], { x: 8.45, y: 4.75, w: 4.0, h: 2.0, fontSize: 13, lineSpacingMultiple: 1.15 });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "신호봉 휘두르기", "신호봉을 크게 휘둘러 앞쪽 반원을 쓸어 냄", "예고 0.6초 / 전방 반원 3.5m\n피해 18", (bx, by, sz) => {
      const cx = bx + sz * 0.25, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.55, 270, 90, RED, { tr: 60 });
      D.boss(s, cx, cy, 0.15);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "개찰 펀치", "짧게 세 번 돌진하며 펀치. 매번 방향을 다시 잡음", "3연타 / 각 3m / 0.4초 간격\n피해 15", (bx, by, sz) => {
      const pts = [[0.2, 0.75], [0.45, 0.45], [0.65, 0.65], [0.85, 0.3]];
      for (let k = 0; k < 3; k++) D.line(s, bx + pts[k][0] * sz, by + pts[k][1] * sz, (pts[k + 1][0] - pts[k][0]) * sz, (pts[k + 1][1] - pts[k][1]) * sz, GOLD, { end: "triangle", width: 2 });
      pts.slice(1).forEach(([u, v]) => D.circle(s, bx + u * sz, by + v * sz, 0.15, RED, { tr: 55, line: RED }));
      D.boss(s, bx + pts[0][0] * sz, by + pts[0][1] * sz, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "시간표 탄막", "전광판 글자판이 뒤집히듯 가로 줄 탄이 차례로 날아옴. 줄마다 빈칸 한 곳", "4줄 / 0.6초 간격 / 6m/초\n피해 15", (bx, by, sz) => {
      const gaps = [1, 4, 2, 5];
      gaps.forEach((g, r) => { for (let c = 0; c < 7; c++) if (c !== g) D.rect(s, bx + 0.12 + c * 0.2, by + 0.15 + r * 0.28, 0.15, 0.1, r === 0 ? GOLD : "FF8A80", { line: r === 0 ? GOLD : "FF8A80" }); });
      D.cat(s, bx + 0.12 + 5 * 0.2 + 0.07, by + sz - 0.18, 0.09);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "호루라기 열차", "바닥 선 3줄을 예고한 뒤 증기 열차 모형이 빠르게 통과", "선 3줄(폭 2m) / 예고 1초\n20m/초 / 피해 18", (bx, by, sz) => {
      [0.3, 0.75, 1.2].forEach((v) => { D.rect(s, bx + 0.05, by + v - 0.11, sz - 0.1, 0.22, RED, { tr: 65, line: RED }); });
      D.rect(s, bx + 0.12, by + 0.75 - 0.1, 0.3, 0.2, COAL, { line: TXT });
      D.arrow(s, bx + 0.45, by + 0.75, sz - 0.65, GOLD, { width: 2 });
      D.cat(s, bx + sz * 0.6, by + 0.52, 0.09);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "차장 호출", "호루라기로 태엽 차장을 불러냄", "2명 소환 / 필드 최대 4명\n필드 몬스터 2명 이하일 때만", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[-0.45, -0.4], [0.45, -0.35]].forEach(([dx, dy]) => { D.circle(s, cx + dx, cy + dy, 0.17, LAV, { tr: 70, line: LAV, dash: "dash" }); D.mob(s, cx + dx, cy + dy); });
      D.boss(s, cx, cy + 0.25, 0.15);
      text(s, "+2", { x: cx - 0.25, y: by + 0.08, w: 0.5, h: 0.3, fontSize: 12, bold: true, color: GOLD, align: "center" });
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 고르기", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "'가까움' 기준 = 5m 안",
      "거리에 맞는 후보 70% / 나머지 30%",
      "차장 호출은 거리 무관 동일 가중치",
      "직전 패턴은 제외",
      "빨간불은 추첨과 별개로 25초마다",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 12, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "멈춘 기관차 거인");
    picture(s, has("ch2/ch2_boss.jpg") ? "ch2/ch2_boss.jpg" : BGIMG, 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "2장의 출구를 지키는 시간의 존재. 시계 문자판 헤드라이트를 달고 몸을 일으킨 증기 기관차, 뒤에 객차 4량", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["경기장", "전차대 / 지름 24m / 바닥에 +자 선로 2줄"],
      ["이동", "느린 걸음으로 고양이 추적 (이속 2.5m/초)"],
      ["체력", "4860 (목표 처치 시간 90초)"],
      ["페이즈", "2페이즈 / 체력 50%에 객차를 떼어 내고 가속"],
      ["상시 탄막", "연기 링"],
      ["고유 기믹", "출발 시각"],
    ]);
  }
  {
    const s = content(5, "고유 기믹: 출발 시각");
    // 경기장 도식
    const ax = 3.0, ay = 4.3, AR = 2.35;
    D.circle(s, ax, ay, AR, FLOOR, { line: LAV, lineW: 1.5 });
    D.rect(s, ax - AR + 0.1, ay - 0.1, 2 * AR - 0.2, 0.2, "5C4E40", { line: "8A7A66" });
    D.rect(s, ax - 0.1, ay - AR + 0.1, 0.2, 2 * AR - 0.2, "5C4E40", { line: "8A7A66" });
    D.boss(s, ax + 0.6, ay - 0.6, 0.36);
    s.addShape(pres.shapes.OVAL, { x: ax + 0.6 - 0.2, y: ay - 0.6 - 0.2, w: 0.4, h: 0.4, fill: { color: "F3EDE2" }, line: { color: GOLD, width: 1.5 } });
    text(s, "7", { x: ax + 0.4, y: ay - 0.8, w: 0.4, h: 0.4, fontSize: 12, bold: true, color: BG, align: "center", valign: "middle" });
    D.cat(s, ax - 1.3, ay + 1.15);
    D.line(s, ax - 1.15, ay + 1.0, 1.55, -1.4, GOLD, { width: 2, end: "triangle" });
    text(s, "7시 탄", { x: ax - 1.45, y: ay + 0.45, w: 0.8, h: 0.28, fontSize: 10, bold: true, color: GOLD });
    caption(s, "전차대 탑뷰 / 헤드라이트 시계에 '7시'", 0.65, 6.72, 4.8, { align: "center" });

    const steps = [
      ["① 출발 준비", "헤드라이트 시계에 출발 시각(4~9시)이 뜨고 보스는 제자리에서 출발 준비. 연기 링만 계속"],
      ["② 출발 시각 탄", "쏘거나 장전해 바늘을 출발 시각 바로 앞까지 옮긴 뒤, 그 시각의 한 발을 헤드라이트에 명중"],
      ["③ 저지", "10초 안에 맞히면 출발 저지 → 보스 3초 기절. 빗나가면 장전해 다시 시도"],
    ];
    steps.forEach((st, i) => {
      const y = 1.6 + i * 1.12;
      card(s, 5.8, y, 6.9, 1.0, i === 2 ? "1F4450" : CARD);
      text(s, st[0], { x: 6.05, y, w: 1.75, h: 1.0, fontSize: 14.5, bold: true, color: GOLD, valign: "middle" });
      text(s, st[1], { x: 7.8, y, w: 4.75, h: 1.0, fontSize: 12.5, valign: "middle", lineSpacingMultiple: 1.1 });
    });
    bullets(s, [
      "시간 초과: 경기장을 가로지르는 전속 돌진 (예고 선 1초 / 최대 체력 40% 피해)",
      "체력 75 / 50 / 25%에 한 번씩 확정 등장",
      "발사 시각: 쏘면 한 칸 이동 / 장전하면 첫 칸으로 복귀",
      "1장 시각 패턴(바늘 맞추기)의 발전형: 바늘을 맞추고 + 그 한 발을 약점에 명중",
    ], { x: 5.8, y: 5.05, w: 6.9, h: 1.85, fontSize: 12.5, paraSpaceAfter: 4 });
  }
  {
    const s = content(5, "상시 탄막과 2페이즈");
    card(s, 0.6, 1.6, 5.4, 5.25);
    text(s, "연기 링", { x: 0.85, y: 1.75, w: 3, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const cx = 3.3, cy = 3.65;
    [0.7, 1.1, 1.5].forEach((r, k) => s.addShape(pres.shapes.ARC, { x: cx - r, y: cy - r, w: 2 * r, h: 2 * r, angleRange: [330 - k * 25, 285 - k * 25], line: { color: STEAM, width: 3, transparency: 20 + k * 20 } }));
    D.boss(s, cx, cy, 0.2);
    D.cat(s, cx + Math.cos(-0.8) * 1.3 + 0.1, cy + Math.sin(-0.8) * 1.3, 0.1);
    text(s, "빈틈", { x: cx + 0.8, y: cy - 1.75, w: 0.7, h: 0.25, fontSize: 10, bold: true, color: TEAL });
    bullets(s, [
      "3초마다 굴뚝에서 퍼지는 연기 링",
      "빈틈 한 곳(45°)으로 통과",
      "6m/초 / 피해 12 / 패턴과 별개로 전투 내내 지속",
    ], { x: 0.85, y: 5.3, w: 5.0, h: 1.45, fontSize: 13.5, paraSpaceAfter: 3 });

    text(s, "2페이즈 (체력 50%)", { x: 6.3, y: 1.65, w: 5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    // 도식: 둘레를 도는 객차
    const ox = 7.5, oy = 3.05, R = 1.0;
    D.circle(s, ox, oy, R + 0.2, FLOOR, { line: LAV });
    D.circle(s, ox, oy, R - 0.2, null, { line: LAV, dash: "dash" });
    for (let k = 0; k < 4; k++) { const a = (k * 32 - 60) * Math.PI / 180; D.rect(s, ox + Math.cos(a) * R - 0.12, oy + Math.sin(a) * R - 0.12, 0.24, 0.24, "8A5A3A", { line: TXT }); }
    s.addShape(pres.shapes.ARC, { x: ox - R - 0.35, y: oy - R - 0.35, w: 2 * R + 0.7, h: 2 * R + 0.7, angleRange: [70, 140], line: { color: GOLD, width: 2, endArrowType: "triangle" } });
    D.boss(s, ox, oy, 0.2);
    bullets(s, [
      "객차를 떼어 냄 → 이속 / 패턴 간격 1.2배",
      "레일 돌진 2연속",
      "떼어 낸 객차가 경기장 둘레(폭 2m)를 상시 회전",
      "→ 객차 회전 패턴은 상시화, 후보에서 제외",
    ], { x: 9.05, y: 2.15, w: 3.7, h: 2.4, fontSize: 12.5, paraSpaceAfter: 3 });

    text(s, "패턴 고르기", { x: 6.3, y: 4.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    const groups = [["가까울 때 (6m 안)", ["연결봉 휘두르기", "증기 분출"], RED], ["멀 때", ["레일 돌진", "석탄 포격"], PURPLE], ["거리 무관", ["객차 회전 (1페이즈)"], TEAL]];
    groups.forEach((g, i) => {
      const y = 5.2 + i * 0.55;
      text(s, g[0], { x: 6.3, y, w: 2.0, h: 0.45, fontSize: 12, bold: true, valign: "middle" });
      g[1].forEach((p, k) => chip(s, p, 8.4 + k * 2.15, y + 0.04, 2.05, g[2], g[2] === TEAL ? BG : TXT, { size: 11, h: 0.37 }));
    });
    s.addNotes("거리에 맞는 후보 70% / 나머지 30%, 직전 패턴 제외. 출발 시각은 추첨과 별개로 체력 75 / 50 / 25%에 확정");
  }
  {
    const s = content(5, "보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "연결봉 휘두르기", "바퀴 연결봉을 양쪽으로 휘둘러 양옆을 쓸어 냄. 앞뒤만 안전", "양옆 부채꼴 100° / 6m\n예고 0.8초 / 피해 22", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.46, 310, 50, RED, { tr: 55 });
      D.pie(s, cx, cy, sz * 0.46, 130, 230, RED, { tr: 55 });
      D.cat(s, cx, cy - sz * 0.33, 0.09);
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "증기 분출", "몸 주변 넓은 원에 증기. 보스 가까이의 링 모양 구역만 안전", "반경 7m / 안전 링 2~3.5m\n예고 1초 / 피해 20", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.48, RED, { tr: 60, line: RED });
      D.circle(s, cx, cy, sz * 0.25, TEAL, { tr: 35, line: TEAL });
      D.circle(s, cx, cy, sz * 0.15, RED, { tr: 30, line: RED });
      D.cat(s, cx + sz * 0.2, cy - 0.05, 0.08);
      D.boss(s, cx, cy, 0.12);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "레일 돌진", "고양이와 가까운 선로가 빛난 뒤 그 선로를 따라 일직선 돌진", "예고 1초 / 16m/초 / 피해 25\n끝에서 1.5초 멈춤 (2페이즈 2연속)", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + 0.05, cy - 0.2, sz - 0.1, 0.4, AMBER, { tr: 65, line: AMBER });
      rail(s, bx + 0.05, cy, sz - 0.1);
      D.arrow(s, bx + 0.45, cy, sz - 0.7, GOLD, { width: 2.5 });
      D.boss(s, bx + 0.28, cy, 0.15);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "석탄 포격", "고양이 주변으로 석탄 덩이 5발을 곡사. 떨어진 자리에 불씨", "원 예고 1초 / 반경 1.5m / 피해 18\n불씨 장판 4초 / 밟으면 초당 5", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[0, 0], [-0.45, -0.35], [0.45, -0.3], [-0.4, 0.4], [0.42, 0.38]].forEach(([dx, dy]) => D.circle(s, cx + dx, cy + dy, 0.2, "B5622E", { tr: 40, line: AMBER }));
      D.cat(s, cx + 0.05, cy + 0.02, 0.08);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "객차 회전", "매달린 객차 4량이 경기장 둘레를 한 바퀴. 가장자리에서 안쪽으로 몰아냄", "둘레 폭 2m / 6초 / 피해 20\n1페이즈만 (2페이즈는 상시)", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2, R = sz * 0.4;
      D.circle(s, cx, cy, R + 0.12, RED, { tr: 70, line: RED });
      D.circle(s, cx, cy, R - 0.12, FLOOR, { line: FLOOR });
      for (let k = 0; k < 4; k++) { const a = (k * 35 - 40) * Math.PI / 180; D.rect(s, cx + Math.cos(a) * R - 0.1, cy + Math.sin(a) * R - 0.1, 0.2, 0.2, "8A5A3A", { line: TXT }); }
      D.boss(s, cx, cy, 0.14);
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "피하는 법", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "연결봉: 보스 앞뒤로 이동",
      "증기: 원 밖이 아니라 보스 쪽 안전 링으로",
      "레일 돌진: 빛나는 선로 밖으로 / 멈춘 1.5초가 반격 시간",
      "석탄: 원 밖으로 대시 / 남은 불씨 회피",
      "객차: 경기장 안쪽으로",
    ], { x: X[2] + 0.25, y: Y[1] + 0.58, w: W - 0.4, h: 1.85, fontSize: 11, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
  }

  // 06 수치
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "2장 수치 정리");
    D.table(s, [
      ["대상", "체력", "이속", "피해", "비고"],
      ["태엽 차장", "60", "2.8m/초", "10", "3갈래 2.5초마다 / 예고 0.6초"],
      ["트렁크 짐꾼", "160", "3.2m/초", "15", "정면 120° 막음 / 빈틈 1.2초"],
      ["신호수", "60", "3.0m/초", "12", "신호등 2개 4초마다 / 반경 2m"],
      ["강화형", "기본 ×1.5", "-", "10~15", "공격 하나 추가"],
      ["태엽 역장", "2520", "3.5m/초", "15~18", "목표 60초 / 대합실 28×24m"],
      ["멈춘 기관차 거인", "4860", "2.5m/초", "12~25", "목표 90초 / 전차대 지름 24m"],
    ], 0.6, 1.6, [2.3, 1.45, 1.25, 1.0, 3.4], { rowH: 0.48, size: 12 });
    const cards = [
      ["피해 기준", "1장보다 약 25% 상향\n일반 10~15 / 중간 보스 15~18\n보스 18~25 (기믹은 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 20 × 명중률 60%\n중간 보스 시점 ×3.5 / 60초\n보스 시점 ×4.5 / 90초"],
      ["전투방", "웨이브당 적: 스테이지 1은 5~7마리\n→ 스테이지 4는 7~10마리\n지나가는 열차 12초마다 / 고양이 20"],
      ["오브젝트 / 상인", "증기 / 짐 피해 적 40 · 광차 50\n고양이 피해 15\n상인 15% / 챕터당 최소 1번"],
    ];
    cards.forEach((c, i) => {
      const y = 1.6 + i * 1.32;
      card(s, 10.2, y, 2.5, 1.2, i % 2 ? CARD : PURPLE);
      text(s, c[0], { x: 10.35, y: y + 0.08, w: 2.3, h: 0.3, fontSize: 12.5, bold: true, color: GOLD });
      text(s, c[1], { x: 10.35, y: y + 0.38, w: 2.3, h: 0.8, fontSize: 9.5, lineSpacingMultiple: 1.1 });
    });
    text(s, "일반 몬스터 체력 = 2장 시작 화력(×2.5) 기준으로 1장과 비슷한 발 수 (차장 3발 / 짐꾼 8발 / 신호수 3발)", { x: 0.6, y: 5.55, w: 9.3, h: 0.4, fontSize: 12.5, color: MUTED });
    text(s, "보스 체력은 '목표 처치 시간 × 그 시점 예상 화력 × 명중률'로 역산", { x: 0.6, y: 5.95, w: 9.3, h: 0.4, fontSize: 12.5, color: MUTED });
    text(s, "중간 보스 2520 = 20 × 3.5 × 60초 × 0.6  /  보스 4860 = 20 × 4.5 × 90초 × 0.6", { x: 0.6, y: 6.4, w: 9.3, h: 0.4, fontSize: 13, bold: true });
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
