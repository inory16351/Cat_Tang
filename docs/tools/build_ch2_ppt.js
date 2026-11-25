// 11:59 2장 기획서 (docs/Cat_Tang_레벨_디자인.md 챕터 2 기준)
// 실행: NODE_PATH=<pptxgenjs · image-size가 있는 node_modules> node build_ch2_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 1장 Ver02 규칙대로 나열 구분자는 " / "
// Ver02 (2026-11-17): Ver01 pptx에서 손으로 고친 문구를 이 생성기에 옮기고, 구현으로 확정된 값(빨간불 · 출발 시각 · 레일 돌진 ·
//   객차 회전 · 지나가는 열차 · 전용 모션)을 반영
// Ver03 (2026-11-18): 패턴 고르기를 모든 보스 공통 '거리에 맞는 패턴 + 거리 무관 패턴 동일 확률'로, 보스 경기장 지름 24m → 32m (원형 경계 · 새 바닥)
// Ver04 (2026-11-25): Ver03 pptx에서 손으로 고친 문구를 옮기고, 07 '클리어 퀘스트' 섹션 추가 (블랙아웃 → 대장장이 · 흐르는 모래 → 탄약 공방 →
//   보안관 결투 → 총포상 · 해금 묶음). 근거: Game/Assets/Scripts/Game/Ch2Quest.cs · VillageStory.cs "ch2" · StoryRun.WakeLines (대사 확정본),
//   Art/tools/specs/ch2_unlock_spec.md (묶음 값은 초안). 묶음 아이콘은 Game/Assets/Resources/Icons에서 읽기만 (없으면 자리 표시)
const path = require("path");
const fs = require("fs");
const sizeOf = require("image-size");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver04";
const FILE = `1159_2장_기획서_20261125_정승운_${VER}.pptx`;
const ICONS = path.resolve(__dirname, "../../Game/Assets/Resources/Icons");
const STEAM = "BFC9D1", COAL = "3A3440", AMBER = "E8B23A", GREEN = "4FA36B";

function build() {
  const SEC = ["2장 개요", "스테이지 구성", "일반 몬스터", "중간 보스", "보스", "수치 기획", "클리어 퀘스트"];
  const D = makeDeck(`11:59 2장 기획서 ${VER}`, `11:59 / 2장 기획서: 멈춘 기차역 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.25"; D.sep = " / ";
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

  const cv = D.cover(BGIMG, "2장 기획서", "정지된 기차역 / 중간 보스 / 보스 상세", `Ver ${VER.slice(3)}`);
  cv.addNotes("공통 규칙(챕터 구조 / 전투방 / 보스 세 박자 / 수치 기준)은 공통 레벨 디자인 기획서를 따름. 방 크기 / 웨이브 / 보상 / 방 사이 문 규칙은 1장과 같음");
  D.toc(has("ch2/ch2_boss.jpg") ? "ch2/ch2_boss.jpg" : BGIMG);

  // 01 개요
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "정지된 기차역");
    picture(s, BGIMG, 0.6, 1.6, 7.3, 4.1, "2장 배경");
    caption(s, "2장 배경 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "출발 직전 시간이 멈춘 역. 1장의 ‘지연’에 이어 ‘정지’가 모티브인 두 번째 챕터", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["테마", "공중에 멈춘 증기 / 떨어지다 멈춘 짐 / 돌로 굳은 승객 석상"],
      ["오브젝트", "멈춘 증기 밸브 / 공중에 멈춘 짐 / 선로 위 광차 / 부서지는 엄폐물 / 지나가는 열차"],
      ["상인", "매표소 직원"],
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
      "굳은 승객 석상 6~8개(보스 방 제외): 탄을 막고, 웨이브 예고 때 깨지며 하수인 등장",
    ], { x: 6.5, y: 3.6, w: 6.2, h: 3.25, fontSize: 12, paraSpaceAfter: 3 });
  }
  {
    const s = content(2, "쏘면 다시 흐르는 시간: 오브젝트");
    const W = 2.92, H = 3.15;
    const objs = [
      ["멈춘 증기 밸브", "피격 시 멈춘 시간이 풀려 정면으로 증기 분출", "승강장 / 기관고 · 방마다 2~3개\n0.5초 뒤 6m × 폭 1.5m · 1.5초\n피해 적 40 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.circle(s, bx + 0.3, cy, 0.17, "6B6F78", { line: STEAM });
        D.rect(s, bx + 0.48, cy - 0.17, sz - 0.62, 0.34, STEAM, { tr: 40, line: STEAM });
        for (let k = 0; k < 4; k++) D.circle(s, bx + 0.75 + k * 0.28, cy + (k % 2 ? 0.05 : -0.05), 0.09, "FFFFFF", { tr: 55, line: STEAM });
        D.mob(s, bx + sz - 0.35, cy);
      }],
      // 2026-11-17: 위에서 떨어지는 방식 → 맞은 방향과 반대로 날아가 폭발 (고양이 탄 → 짐 → 적 쪽 범위 폭발)
      ["공중에 멈춘 짐", "피격 시 피격 방향과 반대로 날아가 폭발", "야적장 / 승강장 / 객차 · 2~4개\n범위형 피해 · 반경 1.5m\n피해 적 40 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.cat(s, bx + 0.12, cy, 0.09);
        D.line(s, bx + 0.24, cy, 0.3, 0, GOLD, { end: "triangle", width: 1 });
        D.rect(s, bx + 0.58, cy - 0.15, 0.44, 0.3, "8A5A3A", { line: TXT });
        D.line(s, bx + 1.06, cy, 0.38, 0, GOLD, { end: "triangle", dash: "dash" });
        D.circle(s, bx + sz - 0.32, cy, 0.42, RED, { tr: 60, line: RED, dash: "dash" });
        D.mob(s, bx + sz - 0.32, cy);
      }],
      ["선로 위 광차", "피격 시 선로를 따라 질주하며 경로의 적을 들이받음", "야적장 / 기관고 · 1~2개\n12m/초 질주 → 끝에서 부서짐\n피해 적 50 / 고양이 15", (bx, by, sz) => {
        const cy = by + sz / 2;
        rail(s, bx + 0.1, cy, sz - 0.2);
        D.rect(s, bx + 0.2, cy - 0.16, 0.36, 0.32, "8A6440", { line: TXT });
        D.arrow(s, bx + 0.62, cy, sz - 1.05, GOLD, { width: 2 });
        D.mob(s, bx + sz - 0.3, cy);
      }],
      ["지나가는 열차", "피격 시 기관차 + 객차 3량이 28m/초로 선로를 훑고 지나감.", "야적장 / 승강장 / 기관고 · 선로 폭 3m\n입장 6초 뒤 첫 열차 · 이후 12초마다\n예고 1.5초 · 피해 적 60 / 고양이 20", (bx, by, sz) => {
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
    text(s, [{ text: "공통  ", options: { bold: true, color: GOLD } }, { text: "적과 고양이 모두 피해 / 증기 밸브 · 짐 · 광차는 몬스터 공격에도 발동\n발동 순간 예고 표시(증기 방향 / 짐 비행 경로 / 광차 경로 / 선로 노란 빛) / 후보 자리 중 일부에만 배치" }], { x: 0.85, y: 5.85, w: 11.7, h: 0.95, fontSize: 13, valign: "middle" });
    D.legend(s, 8.0, 1.2);
  }

  // 03 일반 몬스터
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "시간의 하수인 3종");
    picture(s, has("ch2/ch2_enemies.jpg") ? "ch2/ch2_enemies.jpg" : BGIMG, 0.6, 1.6, 12.1, 2.7, "일반 몬스터 컨셉");
    const mobs = [
      ["태엽 차장", "원거리", "7~10m 유지 / 구멍 난 승차권 3갈래 투척", ["이속 2.8m/초", "3갈래(30°) 2.5초마다 / 예고 0.6초", "탄속 9m/초 / 발당 피해 10 / 체력 60 (3발)", "전용 모션: 승차권 튕기기"]],
      ["트렁크 짐꾼", "방패 근접", "트렁크로 정면 탄 차단 / 접근 후 내리찍기 공격", ["이속 3.2m/초 / 정면 120° 탄 막음", "내리찍기 예고 0.7초 / 전방 90° 2.5m / 피해 15", "내리찍은 뒤 1.2초 빈틈 / 체력 160 (8발)", "전용 모션: 트렁크 든 걷기 / 내리찍기 / 던지기"]],
      ["신호수", "설치형", "고양이 주변에 신호등 원 설치 → 노랑에서 빨강으로 바뀌면 폭발", ["이속 3.0m/초 / 8m 유지", "4초마다 2개 / 노랑 1.5초 → 폭발", "반경 2m / 피해 12 / 체력 60 (3발)", "전용 모션: 신호등 던지기"]],
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
      ["태엽 차장+", "검표 펀치", "피격 시 대시 충전 정지", "6초 쿨 / 예고 0.6초 / 피해 10 + 2초 대시 충전 정지", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.mob(s, bx + 0.3, cy, 0.14);
        D.line(s, bx + 0.45, cy, sz - 0.95, 0, RED, { dash: "dash", end: "triangle" });
        D.cat(s, bx + sz - 0.35, cy);
        [0, 1, 2].forEach((k) => D.circle(s, bx + sz - 0.65 + k * 0.3, cy + 0.45, 0.09, k === 2 ? CARD : GOLD, { line: GOLD }));
        text(s, "대시 정지", { x: bx + sz - 0.95, y: cy + 0.6, w: 1.0, h: 0.25, fontSize: 9, bold: true, color: GOLD, align: "center" });
      }],
      ["트렁크 짐꾼+", "트렁크 던지기", "방패인 트렁크를 일직선으로 던짐. 던진 뒤엔 일정 시간 전면 공격 차단 불가", "8초 쿨 / 예고 0.8초 / 직선 10m / 피해 15 / 3초 전면 공격 차단 불가", (bx, by, sz) => {
        const cy = by + sz / 2;
        D.rect(s, bx + 0.15, cy - 0.15, sz - 0.3, 0.3, RED, { tr: 65, line: RED });
        D.rect(s, bx + 0.95, cy - 0.13, 0.32, 0.26, "8A5A3A", { line: TXT });
        D.arrow(s, bx + 1.3, cy, 0.55, GOLD, { width: 2 });
        D.mob(s, bx + 0.32, cy, 0.14);
        text(s, "무방비", { x: bx + 0.05, y: cy + 0.25, w: 0.6, h: 0.25, fontSize: 9, bold: true, color: TEAL });
      }],
      ["신호수+", "정지 구역", "신호등이 터진 자리에 시간이 정지된 구역. 공간 안에 들어온 모든 투사체가 정지 후 종료 시 다시 이동", "반경 2m / 3초 / 모든 투사체 정지", (bx, by, sz) => {
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
    text(s, "멈춘 역의 질서를 지키는 역장. 맵의 메인 테마인 ‘정지’를 주요 기믹으로 사용", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["등장", "스테이지 2 마지막 방 뒤에 이어 붙은 대합실 / 승리 시 뒷문 너머로 스테이지 3 연결"],
      ["보스맵", "대합실 28×24m / 가운데 통로(폭 8m) 양옆에 벤치 줄 3줄씩(엄폐물, 내구 30) / 증기 밸브 2 / 역 시계"],
      ["이동", "신호봉을 들고 고양이를 따라오며 거리를 좁힘 (이속 3.5m/초)"],
      ["체력", "2520 (목표 처치 시간 60초)"],
      ["페이즈", "1페이즈 / 체력 50% 이하부터 공격 속도와 패턴 간격만 1.2배"],
      ["고유 기믹", "빨간불"],
    ], 11.5);
    s.addNotes("전용 모션: 펀치 / 신호봉 내리치기 / 걷기. 1장 술집과 같은 흐름으로 스테이지 2 마지막 방 뒤에 대합실을 붙여 지음");
  }
  {
    const s = content(4, "고유 기믹: 빨간불");
    const steps = [
      ["① 노란불", "전투 12초 뒤 첫 발동\n이후 25초마다 호루라기\n노란불 1초 예고", AMBER],
      ["② 빨간불 3초", "움직이면(대시 포함) 피해\n천천히 이동하는 총탄 6발 발사", RED],
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
      "빨간불이 시작된 자리에서 0.25m 넘게 벗어나거나 대시하면 최대 체력 20% 피해 (한 번)",
      "조준 / 사격 / 장전은 자유",
      "총탄: 6발 / 고양이 둘레 8m에서 0.2초 간격 / 4m/초 / 쏘면 부서짐 / 맞으면 피해 15",
    ], { x: 8.45, y: 2.3, w: 4.0, h: 2.2, fontSize: 12.5 });
    text(s, "기획의도", { x: 8.45, y: 4.6, w: 4, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    bullets(s, [
      "이동 / 대시로 피하던 1장과 반대로 정지를 메인 기믹으로 채용",
    ], { x: 8.45, y: 5.05, w: 4.0, h: 1.6, fontSize: 13, lineSpacingMultiple: 1.15 });
  }
  {
    const s = content(4, "중간 보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "신호봉 휘두르기", "신호봉을 크게 휘두르는 범위 공격", "예고 0.6초 / 전방 반원 3.5m\n피해 18", (bx, by, sz) => {
      const cx = bx + sz * 0.25, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.55, 270, 90, RED, { tr: 60 });
      D.boss(s, cx, cy, 0.15);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "개찰 펀치", "짧게 세 번 돌진하며 공격. 매 타격시 경로 전환", "3연타 / 각 3m / 0.4초 간격\n피해 15 (3연타 중 한 번만)", (bx, by, sz) => {
      const pts = [[0.2, 0.75], [0.45, 0.45], [0.65, 0.65], [0.85, 0.3]];
      for (let k = 0; k < 3; k++) D.line(s, bx + pts[k][0] * sz, by + pts[k][1] * sz, (pts[k + 1][0] - pts[k][0]) * sz, (pts[k + 1][1] - pts[k][1]) * sz, GOLD, { end: "triangle", width: 2 });
      pts.slice(1).forEach(([u, v]) => D.circle(s, bx + u * sz, by + v * sz, 0.15, RED, { tr: 55, line: RED }));
      D.boss(s, bx + pts[0][0] * sz, by + pts[0][1] * sz, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "시간표 탄막", "전광판 글자판이 뒤집히듯 탄막을 발사. 빈 공간을 통해 회피", "4줄 / 빈칸 폭 약 2.2m / 0.6초 간격\n6m/초 / 피해 15", (bx, by, sz) => {
      const gaps = [1, 4, 2, 5];
      gaps.forEach((g, r) => { for (let c = 0; c < 7; c++) if (c !== g) D.rect(s, bx + 0.12 + c * 0.2, by + 0.15 + r * 0.28, 0.15, 0.1, r === 0 ? GOLD : "FF8A80", { line: r === 0 ? GOLD : "FF8A80" }); });
      D.cat(s, bx + 0.12 + 5 * 0.2 + 0.07, by + sz - 0.18, 0.09);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "호루라기 열차", "고양이가 있는 줄과 양옆 줄(3.2~4.2m 간격)을 예고한 뒤 증기 열차 모형이 빠르게 통과", "선 3줄(폭 2m) / 예고 1초\n20m/초 / 피해 18", (bx, by, sz) => {
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
      "거리에 맞는 패턴 + 거리 무관 패턴을 같은 확률로 추첨",
      "직전 패턴은 제외",
      "빨간불은 추첨과 별개로 25초마다",
    ], { x: X[2] + 0.25, y: Y[1] + 0.6, w: W - 0.4, h: 1.8, fontSize: 11.5, paraSpaceAfter: 2 });
    D.legend(s, 0.6, 6.74);
  }

  // 05 보스
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "기관차 거인");
    picture(s, has("ch2/ch2_boss.jpg") ? "ch2/ch2_boss.jpg" : BGIMG, 0.6, 1.6, 7.3, 4.1, "보스 컨셉");
    caption(s, "보스 컨셉 시안", 0.6, 5.78, 7.3);
    text(s, "2장의 출구를 지키는 시간의 존재. 시계 문자판 헤드라이트를 달고 몸을 일으킨 증기 기관차", { x: 0.6, y: 6.15, w: 7.3, h: 0.65, fontSize: 14 });
    infoRows(s, [
      ["보스맵", "거대한 시계 바닥 / 지름 32m / 원형 경계 / 갈색 테두리 시각 눈금 / +자 선로 2줄"],
      ["이동", "느린 속도로 고양이 추적 (이속 2.5m/초) / 보라 / 금색 객차 4량이 뒤를 따라옴"],
      ["체력", "4860 (목표 처치 시간 90초)"],
      ["페이즈", "2페이즈 / 체력 50%에 객차를 떼어 내고 가속"],
      ["상시 탄막", "연기 링"],
      ["고유 기믹", "출발 시각"],
    ]);
    s.addNotes("전용 모션: 버팀 / 연결봉 휘두르기 / 무거운 걸음. 객차 4량은 보스 원화와 같은 보라 / 금색 전용 객차 모델");
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
    caption(s, "전차대 탑뷰 / 헤드라이트 시계에 '7시'와 남은 초", 0.65, 6.72, 4.8, { align: "center" });

    const steps = [
      ["① 출발 준비", "헤드라이트 시계에 출발 시각(4~9시)과 남은 초가 뜨고 보스는 제자리에서 출발 준비. 연기 링 패턴만 사용"],
      ["② 출발 시각 탄", "쏘거나 장전해 바늘을 출발 시각에 맞추면 시계 글자가 초록으로 변화. 그때 쏜 총탄을 보스 정면(±70°)에 명중"],
      ["③ 저지", "10초 안에 맞히면 출발 저지 → 보스 3초 기절. 빗나가면 장전해 다시 시도"],
    ];
    steps.forEach((st, i) => {
      const y = 1.6 + i * 1.12;
      card(s, 5.8, y, 6.9, 1.0, i === 2 ? "1F4450" : CARD);
      text(s, st[0], { x: 6.05, y, w: 1.75, h: 1.0, fontSize: 14.5, bold: true, color: GOLD, valign: "middle" });
      text(s, st[1], { x: 7.8, y, w: 4.75, h: 1.0, fontSize: 12.5, valign: "middle", lineSpacingMultiple: 1.1 });
    });
    bullets(s, [
      "시간 초과: 예고 선 1초 → 경기장을 가로지르는 26m/초 전속 돌진 / 최대 체력 40% 피해",
      "체력 75 / 50 / 25%에 한 번씩 확정 등장",
      "발사 시각: 쏘면 한 칸 이동 / 장전하면 첫 칸으로 복귀",
      "1장 시각 패턴(바늘 맞추기)의 발전형: 사격을 통한 시간 제어 + 해당 총탄을 명중시키는 패턴",
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
      "5초마다 굴뚝에서 연기 링 3겹 (0.6초 간격)",
      "겹마다 빈틈 한 곳(45°) / 다음 겹 빈틈은 같은 쪽으로 40~70° 밀림",
      "4.5m/초 / 겹마다 피해 12 / 거리를 조절하며 빈틈을 차례로 통과하여 회피",
    ], { x: 0.85, y: 5.3, w: 5.0, h: 1.45, fontSize: 13.5, paraSpaceAfter: 3 });

    text(s, "2페이즈 (체력 50%)", { x: 6.3, y: 1.65, w: 5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    // 도식: 둘레를 도는 객차
    const ox = 7.5, oy = 3.25, R = 0.9;
    D.circle(s, ox, oy, R + 0.2, FLOOR, { line: LAV });
    D.circle(s, ox, oy, R - 0.2, null, { line: LAV, dash: "dash" });
    for (let k = 0; k < 4; k++) { const a = (k * 32 - 60) * Math.PI / 180; D.rect(s, ox + Math.cos(a) * R - 0.12, oy + Math.sin(a) * R - 0.12, 0.24, 0.24, "8A5A3A", { line: TXT }); }
    s.addShape(pres.shapes.ARC, { x: ox - R - 0.35, y: oy - R - 0.35, w: 2 * R + 0.7, h: 2 * R + 0.7, angleRange: [70, 140], line: { color: GOLD, width: 2, endArrowType: "triangle" } });
    D.boss(s, ox, oy, 0.2);
    bullets(s, [
      "객차를 떼어 냄 → 이속 / 패턴 간격 1.2배",
      "레일 돌진 2연속",
      "떼어 낸 객차가 경기장 둘레(반경 약 15m)를 6초에 한 바퀴씩 상시 회전",
      "→ 객차 회전 패턴은 상시화, 후보에서 제외",
    ], { x: 9.05, y: 2.15, w: 3.7, h: 2.4, fontSize: 12.5, paraSpaceAfter: 3 });

    text(s, "패턴 고르기", { x: 6.3, y: 4.75, w: 2.0, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    text(s, "거리에 맞는 패턴 + 거리 무관 패턴 같은 확률 / 직전 패턴 제외", { x: 8.4, y: 4.79, w: 4.3, h: 0.32, fontSize: 10.5, color: MUTED, valign: "middle" });
    const groups = [["가까울 때 (6m 안)", ["연결봉 휘두르기", "증기 분출"], RED], ["멀 때", ["레일 돌진", "석탄 포격"], PURPLE], ["거리 무관", ["객차 회전 (1페이즈)"], TEAL]];
    groups.forEach((g, i) => {
      const y = 5.2 + i * 0.55;
      text(s, g[0], { x: 6.3, y, w: 2.0, h: 0.45, fontSize: 12, bold: true, valign: "middle" });
      g[1].forEach((p, k) => chip(s, p, 8.4 + k * 2.15, y + 0.04, 2.05, g[2], g[2] === TEAL ? BG : TXT, { size: 11, h: 0.37 }));
    });
    s.addNotes("거리에 맞는 패턴 + 거리 무관 패턴을 같은 확률로 추첨, 직전 패턴 제외 (모든 보스 공통). 출발 시각은 추첨과 별개로 체력 75 / 50 / 25%에 확정");
  }
  {
    const s = content(5, "보스 패턴");
    const W = 3.95, H = 2.45, X = [0.6, 4.68, 8.76], Y = [1.6, 4.24];
    D.patternCard(s, X[0], Y[0], W, H, "연결봉 휘두르기", "바퀴 연결봉을 양쪽으로 휘둘러 양옆을 범위 공격.", "양옆 부채꼴 100° / 6m\n예고 0.8초 / 피해 22", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.pie(s, cx, cy, sz * 0.46, 310, 50, RED, { tr: 55 });
      D.pie(s, cx, cy, sz * 0.46, 130, 230, RED, { tr: 55 });
      D.cat(s, cx, cy - sz * 0.33, 0.09);
      D.boss(s, cx, cy, 0.14);
    }, "가까울 때");
    D.patternCard(s, X[1], Y[0], W, H, "증기 분출", "넓은 원 범위에 증기 분출. 보스 가까이의 링 모양 구역만 안전", "반경 7m / 안전 링 2~3.5m\n예고 1초 / 피해 20", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      D.circle(s, cx, cy, sz * 0.48, RED, { tr: 60, line: RED });
      D.circle(s, cx, cy, sz * 0.25, TEAL, { tr: 35, line: TEAL });
      D.circle(s, cx, cy, sz * 0.15, RED, { tr: 30, line: RED });
      D.cat(s, cx + sz * 0.2, cy - 0.05, 0.08);
      D.boss(s, cx, cy, 0.12);
    }, "가까울 때");
    D.patternCard(s, X[2], Y[0], W, H, "레일 돌진", "+자 선로 중 고양이와 가까운 줄 끝으로 미끄러져 이동 후, 선로가 빛나고 일직선 돌진", "예고 1초 / 16m/초 / 피해 25\n끝에서 1.5초 멈춤 (2페이즈 2연속)", (bx, by, sz) => {
      const cy = by + sz / 2;
      D.rect(s, bx + 0.05, cy - 0.2, sz - 0.1, 0.4, AMBER, { tr: 65, line: AMBER });
      rail(s, bx + 0.05, cy, sz - 0.1);
      D.arrow(s, bx + 0.45, cy, sz - 0.7, GOLD, { width: 2.5 });
      D.boss(s, bx + 0.28, cy, 0.15);
    }, "멀 때");
    D.patternCard(s, X[0], Y[1], W, H, "석탄 포격", "고양이 주변으로 석탄 덩이 5발을 곡사. 떨어진 자리에 불씨 소환. 지속 피해", "원 예고 1초 / 반경 1.5m / 피해 18\n불씨 장판 4초 / 초당 피해 5", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2;
      [[0, 0], [-0.45, -0.35], [0.45, -0.3], [-0.4, 0.4], [0.42, 0.38]].forEach(([dx, dy]) => D.circle(s, cx + dx, cy + dy, 0.2, "B5622E", { tr: 40, line: AMBER }));
      D.cat(s, cx + 0.05, cy + 0.02, 0.08);
    }, "멀 때");
    D.patternCard(s, X[1], Y[1], W, H, "객차 회전", "뒤따르던 객차 4량이 경기장 외곽을 돌며 원형 범위 피해. 가장자리에서 안쪽으로 이동 유도 및 제한", "반경 약 15m / 한 바퀴 6초 / 피해 20\n(객차마다 1초에 한 번) / 1페이즈만", (bx, by, sz) => {
      const cx = bx + sz / 2, cy = by + sz / 2, R = sz * 0.4;
      D.circle(s, cx, cy, R + 0.12, RED, { tr: 70, line: RED });
      D.circle(s, cx, cy, R - 0.12, FLOOR, { line: FLOOR });
      for (let k = 0; k < 4; k++) { const a = (k * 35 - 40) * Math.PI / 180; D.rect(s, cx + Math.cos(a) * R - 0.1, cy + Math.sin(a) * R - 0.1, 0.2, 0.2, "8A5A3A", { line: TXT }); }
      D.boss(s, cx, cy, 0.14);
    }, "거리 무관");
    D.card(s, X[2], Y[1], W, H, PURPLE);
    text(s, "패턴 파훼 유도", { x: X[2] + 0.25, y: Y[1] + 0.15, w: 3, h: 0.38, fontSize: 15, bold: true, color: GOLD });
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
      ["멈춘 기관차 거인", "4860", "2.5m/초", "12~25", "목표 90초 / 전차대 지름 32m"],
    ], 0.6, 1.6, [2.3, 1.45, 1.25, 1.0, 3.4], { rowH: 0.48, size: 12 });
    const cards = [
      ["피해 기준", "1장보다 약 25% 상향\n일반 10~15 / 중간 보스 15~18\n보스 18~25 (기믹은 최대 체력 비례)"],
      ["체력 역산", "시작 화력 초당 20 × 명중률 60%\n중간 보스 시점 ×3.5 / 60초\n보스 시점 ×4.5 / 90초"],
      ["전투방", "웨이브당 적: 스테이지 1은 5~7마리\n→ 스테이지 4는 7~10마리\n열차 입장 6초 뒤 / 12초마다 / 28m/초"],
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

  // 07 클리어 퀘스트 (2026-11-25 확정 · 구현: Ch2Quest.cs · 대사는 코드의 확정본 그대로)
  pres.addSection({ title: SEC[6] });
  const SMITH = "모래시계 대장장이", SHERIFF = "빈 외투 보안관", CUCKOO = "태엽 풀린 뻐꾸기";
  const WHO = { "고양이": GOLD, [CUCKOO]: TEAL, [SMITH]: AMBER, [SHERIFF]: "C9A27A" };
  const SHORT = { [CUCKOO]: "뻐꾸기", [SMITH]: "대장장이", [SHERIFF]: "보안관" };
  // 대사: [화자, 글] 목록. 화자는 색 · 굵게, 글은 본문 색
  const say = (s, lines, o) => text(s, lines.map(([who, t], i) => [
    { text: (SHORT[who] || who) + "  ", options: { bold: true, color: WHO[who] || MUTED, fontSize: (o.fontSize || 12) - 1 } },
    { text: t, options: { color: TXT, breakLine: i < lines.length - 1 } },
  ]).flat(), { fontSize: 12, paraSpaceAfter: 4, lineSpacingMultiple: 1.05, ...o });
  const portrait = (s, file, x, y, w, h, label) => { card(s, x, y, w, h, "C9C6CF"); picture(s, file, x + 0.08, y + 0.08, w - 0.16, h - 0.16, label, "contain"); };
  const step = (s, x, y, w, h, n, title, desc, fill, tag) => {
    card(s, x, y, w, h, fill);
    D.circle(s, x + 0.32, y + 0.33, 0.19, GOLD, { line: GOLD });
    text(s, String(n), { x: x + 0.13, y: y + 0.14, w: 0.38, h: 0.38, fontSize: 13, bold: true, color: BG, align: "center", valign: "middle" });
    text(s, title, { x: x + 0.62, y: y + 0.12, w: w - 0.7, h: 0.42, fontSize: 14.5, bold: true, color: GOLD, valign: "middle" });
    text(s, desc, { x: x + 0.2, y: y + 0.62, w: w - 0.35, h: h - 1.0, fontSize: 11.5, lineSpacingMultiple: 1.1 });
    if (tag) D.chip(s, tag, x + w - 0.95, y + h - 0.4, 0.8, PURPLE, TXT, { size: 9, h: 0.26 });
  };
  {
    const s = content(7, "블랙아웃에서 다시 도는 바늘까지");
    const steps = [
      ["블랙아웃", "2장 보스 처치 뒤 꿈 없이 기절 → 암전", "판 안"],
      ["깨어남", "마을에서 깨어나 뻐꾸기와 대화", "마을"],
      ["바늘이 다시 멈춤", "광장 시곗바늘 정지 / 바늘 둘레에 조각 2개", "마을"],
      ["대장장이의 부탁", "대장장이 섬 다리 연결 / 흐르는 모래를 구해 와 달라는 부탁", "마을"],
      ["흐르는 모래", "다음 판 첫 중간 보스가 확정으로 떨어뜨림 / 주우면 죽어도 유지", "판 안"],
      ["탄약 공방 해금", "모래 전달 → 조각 하나가 바늘로 / 탄약 공방 열림", "마을"],
      ["보안관 결투", "보안관 섬 다리 연결 / 종이 울리면 먼저 뽑기", "마을"],
      ["총포상 해금", "마지막 조각 → 바늘이 다시 돎 / 총포상 열림", "마을"],
    ];
    const W = 2.88, H = 1.6, GX = 0.19;
    steps.forEach((st, i) => {
      const r = Math.floor(i / 4), c = i % 4, x = 0.6 + c * (W + GX), y = 1.6 + r * (H + 0.55);
      step(s, x, y, W, H, i + 1, st[0], st[1], st[2] === "판 안" ? "3A2436" : CARD, st[2]);
      if (c < 3) arrow(s, x + W + 0.02, y + H / 2, GX - 0.04, GOLD);
    });
    // 1줄 끝 → 2줄 처음
    const x4 = 0.6 + 3 * (W + GX) + W / 2, x5 = 0.6 + W / 2, yA = 1.6 + H;
    D.line(s, x4, yA, 0, 0.27, GOLD, { width: 2 });
    D.line(s, x4, yA + 0.27, x5 - x4, 0, GOLD, { width: 2 });
    D.line(s, x5, yA + 0.27, 0, 0.28, GOLD, { width: 2, end: "triangle" });
    card(s, 0.6, 5.95, 12.1, 0.88, PURPLE);
    text(s, [
      { text: "규칙  ", options: { bold: true, color: GOLD } },
      { text: "2장 클리어 = 꿈 연출 없음 / 퀘스트 순서 = 탄 → 총 / 해금 때는 묶음을 살 수 있다는 안내만 (첫 묶음 무료 없음 / 조각 지급 없음)", options: { breakLine: true } },
      { text: "광장 시계는 대장장이 / 보안관 퀘스트로 나눠 고치고, 마지막에 바늘이 다시 돎", options: { color: MUTED, fontSize: 12 } },
    ], { x: 0.85, y: 5.95, w: 11.7, h: 0.88, fontSize: 13, valign: "middle", paraSpaceAfter: 2 });
    s.addNotes("판 안 = 전투 판에서 일어나는 단계, 마을 = 마을 섬에서 일어나는 단계. 해금 기록 키: 탄약 공방 q2_ammo / 총포상 q2_gun. 2장 보스 문 앞 스토리 맵 2(배웅하던 승강장)는 이 퀘스트와 별개");
  }
  {
    const s = content(7, "블랙아웃과 깨어남");
    portrait(s, "village/cuckoo.png", 0.6, 1.6, 3.2, 3.45, CUCKOO);
    caption(s, CUCKOO, 0.6, 5.12, 3.2, { align: "center" });
    bullets(s, [
      { t: "연출 순서", b: true, c: GOLD },
      "2장 보스 처치 → 기절 → 암전 (기절 대사 없음)",
      "마을에서 깨어남 → 멈춘 광장 시곗바늘을 먼저 비춤",
      "뻐꾸기 대화 → 대장장이 섬 다리 연결 → 대장장이에게 화살표",
    ], { x: 0.6, y: 5.45, w: 3.3, h: 1.45, fontSize: 11, paraSpaceAfter: 2 });
    card(s, 4.1, 1.6, 8.6, 5.25);
    text(s, "깨어남 대화", { x: 4.35, y: 1.72, w: 4, h: 0.4, fontSize: 15, bold: true, color: GOLD });
    say(s, [
      ["고양이", "윽...머리가...! 어떤 일이 일어난거지..."],
      ["고양이", "난 분명... 그 기차역에 있었는데..."],
      [CUCKOO, "일어났구나...멀리까지 간 모양이던데..."],
      [CUCKOO, "그나저나 또 새로운 손님들이 찾아왔어."],
      [CUCKOO, "시계를 고칠 수 있을지도 몰라."],
      ["고양이", "(또 고장이 난건가? 무슨일인지 영문을 모르겠군)"],
      [CUCKOO, "그렇게 의심스런 눈초리로 쳐다보진마"],
      [CUCKOO, "결국 이 시계가 멈추는 이유는..."],
      [CUCKOO, "아니야.. 차차 알게 되겠지"],
      ["고양이", "...."],
    ], { x: 4.35, y: 2.25, w: 8.1, h: 4.5, fontSize: 15, paraSpaceAfter: 10 });
  }
  {
    const s = content(7, "대장장이: 흐르는 모래와 탄약 공방");
    portrait(s, "village/hourglass.png", 0.6, 1.6, 2.7, 2.91, SMITH);
    caption(s, SMITH, 0.6, 4.58, 2.7, { align: "center" });
    card(s, 0.6, 4.95, 2.7, 1.9, "3A2436");
    text(s, "흐르는 모래", { x: 0.78, y: 5.03, w: 2.4, h: 0.35, fontSize: 13, bold: true, color: AMBER });
    bullets(s, [
      "부탁을 들은 뒤 판의 첫 중간 보스가 확정 드랍",
      "부탁 전에는 드랍 없음",
      "주우면 바로 기록 / 판에서 죽어도 유지",
    ], { x: 0.78, y: 5.4, w: 2.42, h: 1.4, fontSize: 10.5, paraSpaceAfter: 2 });
    // 부탁
    card(s, 3.5, 1.6, 4.5, 5.25);
    text(s, "① 첫 방문: 부탁", { x: 3.72, y: 1.72, w: 4, h: 0.38, fontSize: 14.5, bold: true, color: GOLD });
    say(s, [
      [SMITH, "...모래가 다 새기 전에 왔군."],
      ["고양이", "광장 시계가 또 멈췄어."],
      [SMITH, "알고 있다. 시계 속 모래가 말라 버렸어."],
      [SMITH, "흐르는 모래가 있어야 바늘이 다시 간다."],
      [SMITH, "시간의 문 너머, 처음 길을 막아서는 큰 놈이 그걸 품고 있지."],
      [SMITH, "가져오면 바늘을 채워 주마. 남는 모래로는 탄도 부어 주고."],
    ], { x: 3.72, y: 2.2, w: 4.1, h: 3.1, fontSize: 12.5, paraSpaceAfter: 6 });
    text(s, [
      { text: "안내  ", options: { bold: true, color: GOLD } }, { text: "다음 판 첫 중간 보스에게서 흐르는 모래를 얻어 오자", options: { breakLine: true } },
      { text: "다시 말 걸면  ", options: { bold: true, color: MUTED } }, { text: "\"흐르는 모래는?\" / \"시간의 문 너머, 처음 길을 막아서는 큰 놈이다.\"", options: { color: MUTED } },
    ], { x: 3.72, y: 5.5, w: 4.1, h: 1.25, fontSize: 11, paraSpaceAfter: 4, valign: "bottom" });
    // 전달
    card(s, 8.2, 1.6, 4.5, 5.25, PURPLE);
    text(s, "② 모래를 가져옴: 탄약 공방", { x: 8.42, y: 1.72, w: 4.1, h: 0.38, fontSize: 14.5, bold: true, color: GOLD });
    say(s, [
      [SMITH, "가져왔군."],
      [SMITH, "...그래, 이 모래다."],
      [SMITH, "아직 따뜻하군. 누군가 오래 쥐고 있던 모래야."],
    ], { x: 8.42, y: 2.2, w: 4.1, h: 1.1, fontSize: 12.5, paraSpaceAfter: 6 });
    D.chip(s, "조각 하나 → 바늘 / 탄약 공방 해금", 8.42, 3.4, 4.05, AMBER, BG, { size: 11, h: 0.34 });
    say(s, [
      [SMITH, "바늘 한쪽은 채웠다. 나머지는 내 손으론 안 돼."],
      [SMITH, "서쪽 섬의 보안관을 찾아가."],
      [SMITH, "그 친구는 말보다 총으로 이야기하지."],
      [SMITH, "공방도 다시 열었다. 새 탄을 부으려면 조각을 가져와."],
    ], { x: 8.42, y: 3.92, w: 4.1, h: 2.2, fontSize: 12.5, paraSpaceAfter: 6 });
    text(s, "→ 보안관 섬 다리 연결 / 보안관에게 화살표", { x: 8.42, y: 6.3, w: 4.1, h: 0.4, fontSize: 11, color: MUTED });
    s.addNotes("해금 알림: '탄약 공방 해금 — 시간의 조각으로 새 총탄 묶음을 살 수 있다'. 모래 모델 = 작은 모래시계 + 호박색 빛, 다가가면(4m) 빨려 와 주워짐");
  }
  {
    const s = content(7, "보안관 결투와 총포상");
    portrait(s, "village/sheriff.png", 0.6, 1.6, 2.7, 2.91, SHERIFF);
    caption(s, SHERIFF, 0.6, 4.58, 2.7, { align: "center" });
    // 결투 전
    card(s, 3.5, 1.6, 4.5, 2.5);
    text(s, "① 결투 신청", { x: 3.72, y: 1.72, w: 4, h: 0.38, fontSize: 14.5, bold: true, color: GOLD });
    say(s, [
      [SHERIFF, "대장장이가 보냈나."],
      [SHERIFF, "내 얼굴은 필름과 함께 바랬어. 하지만 총은 아직 기억하지."],
      [SHERIFF, "바늘을 움직이고 싶다면, 한 번 겨뤄 보세."],
      [SHERIFF, "종이 울리면 뽑게. 먼저 뽑으면 처음부터야."],
    ], { x: 3.72, y: 2.2, w: 4.1, h: 1.85, fontSize: 12, paraSpaceAfter: 5 });
    // 결투 규칙
    card(s, 3.5, 4.3, 4.5, 2.55, "3A2436");
    text(s, "② 결투 (연출 위주)", { x: 3.72, y: 4.4, w: 4, h: 0.38, fontSize: 14.5, bold: true, color: AMBER });
    bullets(s, [
      "7m 떨어져 마주 섬 / 째깍 소리 2~4.4초 뒤 종 + 섬광 + '지금!'",
      "종 뒤 1.3초 안에 좌클릭 = 승리 (슬로 / 보안관 외투가 휘청)",
      "일찍 쏘면 \"...성급하군. 다시.\" / 늦으면 보안관이 먼저 쏨 \"느려. 한 번 더.\"",
      "패널티 없음 / 이길 때까지 다시",
    ], { x: 3.72, y: 4.85, w: 4.15, h: 1.95, fontSize: 11, paraSpaceAfter: 3 });
    // 이긴 뒤
    card(s, 8.2, 1.6, 4.5, 5.25, PURPLE);
    text(s, "③ 이긴 뒤: 총포상", { x: 8.42, y: 1.72, w: 4.1, h: 0.38, fontSize: 14.5, bold: true, color: GOLD });
    say(s, [
      [SHERIFF, "......좋은 손이야."],
      [SHERIFF, "떠나는 기차를 붙잡으려던 그 손이군."],
      ["고양이", "......어떻게 그걸."],
      [SHERIFF, "총포상을 다시 열지. 바늘도 이제 돌 거야."],
    ], { x: 8.42, y: 2.2, w: 4.1, h: 1.9, fontSize: 12.5, paraSpaceAfter: 6 });
    D.chip(s, "마지막 조각 → 바늘이 다시 돎", 8.42, 4.25, 4.05, GOLD, BG, { size: 11, h: 0.34 });
    D.chip(s, "총포상 해금", 8.42, 4.72, 4.05, AMBER, BG, { size: 11, h: 0.34 });
    text(s, "해금 알림: '총포상 해금 — 시간의 조각으로 새 총 묶음을 살 수 있다'", { x: 8.42, y: 5.25, w: 4.1, h: 0.6, fontSize: 11, color: MUTED });
  }
  {
    const s = content(7, "해금 묶음: 탄약 공방 / 총포상");
    text(s, [
      { text: "묶음을 시간의 조각으로 사면 그 묶음의 탄 / 총이 판의 드랍 / 상점에 등장 (세피리아식)", options: { breakLine: true } },
      { text: "기존 탄 / 총은 처음부터 해금 / 시작 총 고르기 없음 (판 시작 = 양손 기본 총)" },
    ], { x: 0.6, y: 1.42, w: 12.1, h: 0.55, fontSize: 12, color: MUTED, paraSpaceAfter: 1 });
    const GC = { 희귀: "4F8CE0", 영웅: "9B5DE5", 전설: "E0A63A" };
    const icon = (x, y, sz, kind, id, col) => {
      const f = path.join(ICONS, kind, `${kind.slice(0, -1)}_${id}.png`);
      if (fs.existsSync(f)) {
        const d = sizeOf(fs.readFileSync(f)), k = Math.min(sz / d.width, sz / d.height);
        s.addImage({ path: f, x: x + (sz - d.width * k) / 2, y: y + (sz - d.height * k) / 2, w: d.width * k, h: d.height * k });
      } else {
        D.rect(s, x, y, sz, sz, FLOOR, { line: col, dash: "dash" });
        text(s, "아이콘\n제작 중", { x, y, w: sz, h: sz, fontSize: 8, color: MUTED, align: "center", valign: "middle" });
        console.warn("아이콘 없음:", f);
      }
    };
    const bundle = (x, y, w, h, fac, name, kind, cost, items, kindIcon) => {
      card(s, x, y, w, h);
      text(s, fac, { x: x + 0.25, y: y + 0.12, w: 2.5, h: 0.3, fontSize: 11, color: MUTED });
      text(s, [{ text: name, options: { bold: true, color: GOLD } }, { text: `  ${kind}`, options: { color: TXT, fontSize: 12 } }], { x: x + 0.25, y: y + 0.4, w: w - 1.9, h: 0.42, fontSize: 17 });
      text(s, [{ text: `${cost} 조각`, options: { bold: true } }, { text: "  초안", options: { color: MUTED, fontSize: 9.5 } }], { x: x + w - 1.75, y: y + 0.4, w: 1.5, h: 0.42, fontSize: 15, align: "right" });
      const iw = (w - 0.5) / items.length;
      items.forEach(([id, nm, gr], k) => {
        const ix = x + 0.25 + k * iw, sz = 0.92;
        icon(ix + (iw - sz) / 2, y + 0.85, sz, kindIcon, id, GC[gr]);
        text(s, [{ text: nm, options: { bold: true, color: GC[gr] } }, { text: `  ${gr}`, options: { color: MUTED, fontSize: 9.5 } }], { x: ix, y: y + 0.85 + sz + 0.06, w: iw, h: 0.3, fontSize: 11.5, align: "center" });
      });
    };
    const BW = 5.95, BH = 2.2;
    text(s, "탄약 공방", { x: 0.6, y: 2.02, w: 3, h: 0.3, fontSize: 13, bold: true, color: GOLD });
    text(s, "총포상", { x: 6.75, y: 2.02, w: 3, h: 0.3, fontSize: 13, bold: true, color: GOLD });
    bundle(0.6, 2.33, BW, BH, "묶음 1 / 탄 3발", "여정", "제어: 둔화 / 가둠", 60, [[1047, "연착", "희귀"], [1048, "환승역", "영웅"], [1049, "끝나지 않는 여정", "전설"]], "bullets");
    bundle(0.6, 4.65, BW, BH, "묶음 2 / 탄 3발", "마무리", "화력: 관통 / 폭발", 80, [[1050, "레일 위의 불꽃", "희귀"], [1051, "연기 고리", "영웅"], [1052, "마지막 기차", "전설"]], "bullets");
    bundle(6.75, 2.33, BW, BH, "묶음 3 / 총 1자루", "가스등 랜턴", "스킬 랜턴 던지기", 70, [[2009, "가스등 랜턴", "영웅"]], "guns");
    bundle(6.75, 4.65, BW, BH, "묶음 4 / 총 1자루", "기관차", "스킬 증기 돌진", 80, [[2010, "기관차", "전설"]], "guns");
    s.addNotes("묶음 값 60~80 조각은 초안. 이름 / 생김새만 2장 기억(비 오는 승강장 / 기차) 결, 기능은 스토리와 무관. 등급은 희귀~전설. 탄 / 총 상세는 총탄 기획서 Ver04 / 총 기획서 Ver03. 앞으로 장마다 묶음 확장");
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
