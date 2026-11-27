// 11:59 보스 등장 연출 기획서 Ver01 (2026-11-27): 연출 의도 / 공통 흐름 / 카메라 / 정지 카드 / 보스별 연출 / 소리 / 설정 / 건너뛰기
// 실행: NODE_PATH=./node_modules node build_bossintro_ppt.js
// 내용 = 코드 기준 (BossIntro.cs / BossIntroCards.cs), 조사 docs/research_boss_intro_1127.md
// 그림 = docs/images/boss_intro/strip_<id>.jpg (예전 GIF 4컷, git 9dfecfe4^) / card_<id>.jpg (Resources/UI/BossIntro)
// 틀 = build_level_ppts.js(makeDeck), 나열 구분자 " / ", 말투 = 명사로 끝내는 개조식 + 한자어 기획 용어
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, FLOOR } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_보스등장연출_기획서_20261127_정승운_${VER}.pptx`;
const B = (n) => `boss_intro/${n}`;

const BOSSES = [
  { ch: 1, mid: true, id: "ch1_mid", title: "황야의 무법자", name: "와인더 잭", en: "WINDER JACK", card: "Wanted", shots: "Feet 0.8 → Profile 0.7 → ShotDown 1.9", sum: "3.4초",
    act: ["착지 링 예고 → 9m 높이에서 낙하 착지 → 재장전", "샷 진행 36% 지점 카메라 저격 → 카메라가 회전하며 바닥으로 낙하, 바닥에서 올려다보는 구도"],
    cardDesc: "현상수배 포스터 낙하 → 0.56초 빨간 'DEAD OR ALIVE' 도장" },
  { ch: 1, id: "ch1_boss", title: "불타는 시계탑", name: "달리오스", en: "DALIOS", card: "Melt", shots: "LowRise 1.4 → BehindBoss 1.2 (Flash) → Pullback 1.6", sum: "4.2초",
    act: ["녹은 바닥에서 상승 등장 / 상체 대형 구도 (bust 60%)", "금빛 베기 → 0.4배속 내려찍기 폭발"],
    cardDesc: "녹은 금 방울 18개 / 이름이 늘어졌다 굳는 연출" },
  { ch: 2, mid: true, id: "ch2_mid", title: "승강장의 지배자", name: "컨덕터 반", en: "CONDUCTOR VAN", card: "Board", shots: "HighOrbit 1.0 → QuickDraw 1.4 (Wipe)", sum: "2.4초",
    act: ["9m 뒤에서 증기와 함께 돌진", "호루라기 + 빨강 / 초록 신호 번쩍임 → 0.45배속"],
    cardDesc: "반전식 출발 안내판 / 글자 0.07초 간격 회전 후 정지 / 'PLATFORM n'" },
  { ch: 2, id: "ch2_boss", title: "탈선한 종착역", name: "로코모스", en: "LOCOMOS", card: "Rail", shots: "Wide 1.0 → Chase 1.6 (Whip) → Crash 1.6 (Flash)", sum: "4.2초",
    act: ["14m 거리에서 돌진 → 3.2m 지점 들이받기", "충돌 순간 슬로 + 석탄 폭발"],
    cardDesc: "기차 띠 진입 → 불꽃을 튀기며 급정거" },
  { ch: 3, mid: true, id: "ch3_mid", title: "반사의 연출가", name: "일루시오", en: "ILLUSIO", card: "Curtain", shots: "LowRise 1.0 → HeroOrbit 0.7 → Portrait 0.7", sum: "2.4초",
    act: ["25% 지점 번쩍임 + 연기 속 인사하며 등장", "거울 분신 2체가 동작 모방 → 0.75초 후 유리처럼 파괴 → 카메라 쪽으로 카드 투척"],
    cardDesc: "커튼 테두리 / 스포트라이트 / 전구 점멸 간판" },
  { ch: 3, id: "ch3_boss", title: "호수의 기억", name: "모비 글래스", en: "MOBY GLASS", card: "Shatter", shots: "Top 1.3 → Rise 1.5 (Whip) → HeroLow 1.4 (Flash)", sum: "4.2초",
    act: ["부감 물결 → 수면을 뚫고 솟구침", "카드 그림만 가로형 (고래 실루엣)"],
    cardDesc: "방사형 균열 18개 / 거울 잔상 / 광택 통과" },
  { ch: 4, mid: true, id: "ch4_mid", title: "시계탑의 단두대", name: "기로틴", en: "GUILLOTINE", card: "NoirSlash", shots: "JumpCut 1.0 → Crash 1.4 (Flash)", sum: "2.4초",
    act: ["느와르 연출: 각도 4개 하드컷 연결", "빨간 글리치 순간이동 → 빨간 X 베기"],
    cardDesc: "검은 화면 / -18도 빨간 사선 / 흰 반쪽 진입 / 핏자국" },
  { ch: 4, id: "ch4_boss", title: "시간의 수문장", name: "크로노 & 템포", en: "CHRONO & TEMPO", card: "TwinSplit", shots: "ClockSweep 1.0 → Duel 0.55 → Duel 0.5 → Profile 0.8 (Flash) → Push 1.35 (Whip)", sum: "4.2초",
    act: ["시곗바늘처럼 회전 하강 착지 → 대검 / 레이피어 찌르기 클로즈업", "엇갈려 베며 통과 → 어깨 나란히 (±2.3m) 0.3배속 → 정면 결의 포즈"],
    cardDesc: "흰 반쪽 / 검은 반쪽 충돌 / 가운데 큰 '&'를 두고 이름 좌우 분할" },
  { ch: 5, mid: true, id: "ch5_mid", title: "(칭호 없음)", name: "미지의 존재", en: "UNKNOWN", card: "Glitch", shots: "OverShoulder 0.75 → HeroOrbit 0.95 (Flash) → QuickDraw 1.0", sum: "2.7초",
    act: ["고양이 모델에 실시간 자세 적용: 모자챙 → 리볼버 3바퀴 회전", "카메라를 향한 쌍권총 속사 → 뒤쪽 유리 파괴"],
    cardDesc: "RGB 분리 / 주사선 / 뒤섞인 글자가 이름으로 정리 / 'CHAPTER 5 / ???'" },
  { ch: 5, id: "ch5_boss", title: "영원의 지배자", name: "시간의 주인", en: "LORD OF TIME", card: "Dial", shots: "WormEye 1.2 → Stutter 1.5 → Vertigo 1.5 (Whip)", sum: "4.2초",
    act: ["소환진 위 9m 높이에서 천천히 하강 / 허공 돔", "Stutter = 째깍거리며 7번 끊어 도는 궤도 / Vertigo = 돌리 줌 + 0.4배속 대낫 베기"],
    cardDesc: "시계판 바늘 회전 → 11:59 정지 → 0.58초 마지막 1분 째깍 → 종소리" },
];

function build() {
  const SEC = ["연출 의도", "공통 흐름", "카메라 / 전환", "정지 카드", "보스별 연출", "소리 / 설정"];
  const D = makeDeck(`11:59 보스 등장 연출 기획서 ${VER}`, `11:59 / 보스 등장 연출 기획서 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.27"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;

  D.cover(B("card_ch5_boss.jpg"), "보스 등장 연출 기획서", "보스 10명 / 컷 구성 / 정지 카드 / 건너뛰기", `Ver ${VER.slice(3)}`);
  D.toc(B("card_ch4_boss.jpg"));

  // 01 연출 의도 ------------------------------------------------------------
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "보스마다 한 장면을 각인시키는 등장");
    picture(s, B("freeze_row.jpg"), 0.6, 1.6, 7.0, 1.31, "정지 카드 모음", "cover");
    caption(s, "정지 카드 (달리오스 / 로코모스 / 컨덕터 반)", 0.6, 2.98, 7.0);
    picture(s, B("strip_ch4_boss.jpg"), 0.6, 3.3, 7.0, 0.975, "크로노 & 템포 컷", "cover");
    caption(s, "크로노 & 템포 컷 흐름 (ClockSweep → Duel → Profile → 카드)", 0.6, 4.33, 7.0);
    const goals = [
      ["캐릭터 각인", "보스 고유 동작 1개 + 이름 카드로 첫 만남을 기억에 남김", GOLD],
      ["패턴 예고", "등장 동작에 그 보스의 핵심 패턴 (돌진 / 낙하 / 분신)을 미리 노출", TEAL],
      ["전투 리듬 보호", "첫 만남만 강제 / 재도전부터 Space 즉시 건너뛰기", RED],
    ];
    goals.forEach((g, i) => {
      const y = 1.6 + i * 0.95;
      card(s, 7.85, y, 4.85, 0.82);
      s.addShape(pres.shapes.RECTANGLE, { x: 7.85, y: y + 0.14, w: 0.07, h: 0.54, fill: { color: g[2] }, line: { color: g[2] } });
      text(s, g[0], { x: 8.1, y: y + 0.06, w: 4.5, h: 0.32, fontSize: 14, bold: true, color: g[2] });
      text(s, g[1], { x: 8.1, y: y + 0.38, w: 4.5, h: 0.4, fontSize: 11 });
    });
    bullets(s, [
      "대상: 장마다 중간 보스 1 + 보스 1 = 총 10명 / 보스 생성 직후 자동 발동",
      "구성: 실시간 카메라 컷 (보스 4.2초 / 중간 보스 2.4~3.4초) → 정지 카드 (보스 1.8초 / 중간 보스 1.6초)",
      "연출 화면 글자는 영어 (칭호 / 이름) + 한국어 보조 줄 / 체력 바에는 한국어 이름",
      "대형 보스 4명 (달리오스 / 로코모스 / 크로노 & 템포 / 시간의 주인)은 상체 대형 구도 (bust 60%)",
    ], { x: 0.6, y: 4.75, w: 12.1, h: 2.1, fontSize: 13, paraSpaceAfter: 5 });
  }
  {
    const s = content(1, "보스 목록");
    D.table(s, [
      ["장", "구분", "칭호 / 이름", "영어 표기", "카드"],
      ...BOSSES.map((b) => [`${b.ch}장`, b.mid ? "중간 보스" : "보스", `${b.title} / ${b.name}`, b.en, b.card]),
    ], 0.6, 1.6, [0.9, 1.5, 4.4, 3.4, 1.9], { rowH: 0.38, size: 12 });
    text(s, "칭호 / 이름 2026.11.21 확정 / 5장 중간 보스는 칭호 없이 '???' 표기", { x: 0.6, y: 6.5, w: 12.1, h: 0.3, fontSize: 11.5, color: MUTED });
  }

  // 02 공통 흐름 ------------------------------------------------------------
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "등장 연출 공통 흐름");
    const st = [
      ["발동", "보스 생성 직후\n자동 재생"],
      ["준비", "적 / 미니맵 정지\n입력 잠금 / HUD 숨김\n음악 0.3으로 감소"],
      ["컷", "샷 목록 실시간 재생\n레터박스 118px\n(0.22초에 닫힘)"],
      ["정지 카드", "게임 시간 정지\n음악 0.05초에 무음\n충격음 + 장별 스팅어"],
      ["종료", "보스 곡 처음부터\n카메라 0.4초 복귀\n패턴 1.0초 보류"],
    ];
    const n = st.length, gap = 0.28, bw = (12.1 - gap * (n - 1)) / n;
    st.forEach((t, i) => {
      const x = 0.6 + i * (bw + gap);
      card(s, x, 1.7, bw, 2.0, i === 3 ? PURPLE : CARD);
      text(s, t[0], { x: x + 0.1, y: 1.82, w: bw - 0.2, h: 0.42, fontSize: 17, bold: true, color: GOLD, align: "center" });
      text(s, t[1], { x: x + 0.1, y: 2.3, w: bw - 0.2, h: 1.3, fontSize: 12, align: "center", lineSpacingMultiple: 1.15 });
      if (i < n - 1) arrow(s, x + bw + 0.03, 2.7, gap - 0.06, GOLD);
    });
    D.table(s, [
      ["구분", "컷 합계", "정지 카드", "전체"],
      ["보스 (1~5장)", "4.2초", "1.8초", "약 6.6초"],
      ["1장 중간 보스", "3.4초", "1.6초", "약 5.6초"],
      ["2~4장 중간 보스", "2.4초", "1.6초", "약 4.6초"],
      ["5장 중간 보스", "2.7초", "1.6초", "약 4.9초"],
    ], 0.6, 3.95, [3.5, 2.8, 2.8, 3.0], { rowH: 0.38, size: 12.5 });
    bullets(s, [
      "등장 위치: 위 / 뒤 / 땅속 (높이 × 1.3 아래)에서 진입 / 보스는 고양이 방향으로 회전",
      "종료 시 입력 잠금 1프레임 뒤 해제 (건너뛰기 Space가 대시로 입력되는 문제 방지) / 본 기록 저장",
    ], { x: 0.6, y: 6.2, w: 12.1, h: 0.7, fontSize: 11.5, paraSpaceAfter: 2 });
  }

  // 03 카메라 / 전환 ------------------------------------------------------------
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "카메라 규칙과 컷 전환");
    D.table(s, [
      ["전환", "연출", "사용 예"],
      ["Hard", "즉시 컷", "기본"],
      ["Flash", "흰 번쩍임 (느와르 보스는 강조색 번쩍임)", "달리오스 / 로코모스 / 기로틴"],
      ["Whip", "55도 회전 + 속도선 0.13초 + 휙 소리", "로코모스 / 모비 글래스 / 시간의 주인"],
      ["Wipe", "-16도 사선 띠로 화면 덮기", "컨덕터 반"],
    ], 0.6, 1.6, [1.6, 5.6, 4.9], { rowH: 0.5, size: 12.5 });
    card(s, 0.6, 4.4, 5.95, 2.45, PURPLE);
    text(s, "카메라 규칙", { x: 0.85, y: 4.52, w: 5.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "로우 앵글 샷 외에는 최소 10도 부감 유지 (바닥 문자판 가독성)",
      "이동 샷에 SpeedRamp 1.55 → 0.45 → 1.55배속 적용",
      "마지막 샷은 항상 정면 / 카드로 이어지는 구도",
    ], { x: 0.85, y: 4.95, w: 5.5, h: 1.85, fontSize: 12, paraSpaceAfter: 4 });
    card(s, 6.75, 4.4, 5.95, 2.45);
    text(s, "화면 정리", { x: 7.0, y: 4.52, w: 5.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "시야를 가리는 소품 일시 숨김",
      "마지막 샷에서 고양이 숨김 (보스 단독 구도)",
      "샷 목록은 게임 시간이 아닌 실제 시간 기준 재생 (슬로 연출과 무관하게 길이 고정)",
    ], { x: 7.0, y: 4.95, w: 5.5, h: 1.85, fontSize: 12, paraSpaceAfter: 4 });
  }

  // 04 정지 카드 ------------------------------------------------------------
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "정지 카드와 이름 글자");
    picture(s, B("card_ch2_boss.jpg"), 0.6, 1.6, 2.7, 4.05, "로코모스 카드", "cover");
    caption(s, "카드 그림 (로코모스)", 0.6, 5.72, 2.7);
    // 타이밍 도식: 0 → 0.22 → 0.30 → 0.44 → 1.8
    const X0 = 3.7, W = 9.0, T = 1.8, y = 1.75;
    const tx = (t) => X0 + ([0, 0.22, 0.3, 0.44, 1.8].indexOf(t) / 4) * W;   // 간격 = 순서 (시간 비례 아님)
    D.rect(s, X0, y + 0.45, W, 0.06, MUTED, { line: MUTED });
    [[0, "카드 진입 / 흰 번쩍임 / 레터박스 열림"], [0.22, "CHAPTER 줄 + 칭호 진입 완료"], [0.3, "영어 이름 찍힘 (1.9 → 1배, 0.14초)"], [0.44, "한국어 줄 등장"], [1.8, "퇴장 (0.22초 페이드 + 6% 확대)"]].forEach((m, i) => {
      D.circle(s, tx(m[0]), y + 0.48, 0.09, i === 2 ? RED : GOLD, { line: TXT });
      text(s, `${m[0]}초`, { x: tx(m[0]) - 0.45, y: y, w: 0.9, h: 0.3, fontSize: 10.5, bold: true, color: GOLD, align: "center" });
      text(s, m[1], { x: i === 0 ? X0 - 0.3 : i === 4 ? tx(m[0]) - 1.4 : tx(m[0]) - 0.85, y: y + 0.68, w: i === 4 ? 1.4 : 1.7, h: 0.5, fontSize: 10, color: TXT, align: i === 0 ? "left" : i === 4 ? "right" : "center" });
    });
    D.table(s, [
      ["요소", "규칙"],
      ["시간", "게임 시간 정지 / 보스 1.8초 / 중간 보스 1.6초"],
      ["소리", "음악 0.05초에 무음 (한 박자 침묵) → 충격음 + 장별 스팅어 / 이름 찍힘 효과음"],
      ["글자", "'CHAPTER n / BOSS' 줄 + 영어 칭호 -260px 위치에서 0.22초 진입 / 영어 이름 168pt 기울임"],
      ["움직임", "카드 표시 중 전체 14px 흐름 (정지 화면이 멈춰 보이지 않게)"],
      ["그림", "보스별 카드 그림 / 그림 미존재 시 글자만 표시"],
    ], 3.7, 3.25, [1.4, 7.6], { rowH: 0.5, size: 11.5 });
  }
  {
    const s = content(4, "카드 10종");
    const cols = 5, cw = 2.3, gap = 0.15;
    BOSSES.forEach((b, i) => {
      const r = Math.floor(i / cols), c = i % cols;
      const x = 0.6 + c * (cw + gap), y = 1.55 + r * 2.7;
      picture(s, B(`card_${b.id}.jpg`), x, y, 0.95, 1.42, b.name, b.id === "ch3_boss" ? "contain" : "cover");
      card(s, x + 1.0, y, cw - 1.0, 1.42, b.mid ? CARD : PURPLE);
      text(s, b.card, { x: x + 1.08, y: y + 0.06, w: cw - 1.15, h: 0.3, fontSize: 12, bold: true, color: GOLD });
      text(s, b.name, { x: x + 1.08, y: y + 0.36, w: cw - 1.15, h: 0.3, fontSize: 10.5, bold: true });
      text(s, `${b.ch}장 ${b.mid ? "중간 보스" : "보스"}`, { x: x + 1.08, y: y + 0.66, w: cw - 1.15, h: 0.28, fontSize: 9.5, color: MUTED });
      text(s, b.cardDesc, { x: x, y: y + 1.5, w: cw, h: 1.1, fontSize: 9.5, lineSpacingMultiple: 1.05 });
    });
  }

  // 05 보스별 연출 ------------------------------------------------------------
  pres.addSection({ title: SEC[4] });
  for (let ch = 1; ch <= 5; ch++) {
    const pair = BOSSES.filter((b) => b.ch === ch);
    const s = content(5, `${ch}장: ${pair.map((b) => b.name).join(" / ")}`);
    pair.forEach((b, i) => {
      const y0 = 1.55 + i * 2.72;
      chip(s, b.mid ? "중간 보스" : "보스", 0.6, y0, 1.1, b.mid ? CARD : RED, TXT, { size: 11 });
      text(s, `${b.title} / ${b.name}  (${b.en})`, { x: 1.85, y: y0, w: 6.4, h: 0.36, fontSize: 15, bold: true, color: GOLD, valign: "middle" });
      picture(s, B(`strip_${b.id}.jpg`), 0.6, y0 + 0.48, 7.6, 1.06, `${b.name} 연출`, "contain");
      text(s, [{ text: "샷  ", options: { bold: true, color: GOLD } }, { text: `${b.shots}  (합계 ${b.sum})` }], { x: 0.6, y: y0 + 1.62, w: 7.6, h: 0.5, fontSize: 11, valign: "top" });
      card(s, 8.45, y0 + 0.48, 4.25, 1.95, i === 0 ? FLOOR : CARD);
      bullets(s, [
        { t: "등장 동작", b: true, c: GOLD },
        ...b.act,
        { t: `카드 ${b.card}`, b: true, c: GOLD },
        b.cardDesc,
      ], { x: 8.6, y: y0 + 0.55, w: 4.0, h: 1.85, fontSize: 10, paraSpaceAfter: 1, lineSpacingMultiple: 1.0 });
    });
  }

  // 06 소리 / 설정 ------------------------------------------------------------
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "소리 / 설정 연동 / 건너뛰기");
    D.table(s, [
      ["효과음", "시점"],
      ["intro_riser", "연출 시작 (음악 0.4초 동안 0.3으로 감소)"],
      ["intro_whoosh_1 / 2", "Whip 전환"],
      ["intro_impact + intro_stinger_1~5", "정지 카드 진입 (장별 스팅어)"],
      ["intro_flip / intro_glitch", "Board 카드 글자 회전 / Glitch 카드"],
      ["보스 곡", "종료 시 처음부터 재생 / 0.9초 동안 음량 복귀 (건너뛰기 시 0.4초)"],
    ], 0.6, 1.6, [3.4, 2.65], { rowH: 0.58, size: 11 });
    card(s, 6.75, 1.6, 5.95, 2.2, PURPLE);
    text(s, "건너뛰기", { x: 7.0, y: 1.72, w: 5.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "첫 만남 건너뛰기 불가 / 2회차부터 Space로 컷과 카드 전부 생략",
      "건너뛰기 가능 시에만 오른쪽 위 SPACE 키캡 + '건너뛰기 ▶▶' 맥동 표시",
      "본 기록은 저장 슬롯별 / 다시 보기 기능 미제공",
    ], { x: 7.0, y: 2.15, w: 5.5, h: 1.6, fontSize: 12, paraSpaceAfter: 4 });
    card(s, 6.75, 4.0, 5.95, 1.65);
    text(s, "환경설정 연동", { x: 7.0, y: 4.12, w: 5.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
    bullets(s, [
      "카메라 흔들림 × 화면 흔들림 설정값",
      "번쩍임 / 글리치 × 번쩍임 배율 ('번쩍임 줄이기' 켬 = 0.3)",
    ], { x: 7.0, y: 4.55, w: 5.5, h: 1.05, fontSize: 12, paraSpaceAfter: 4 });
    text(s, "효과음은 파일 미존재 시 기존 효과음으로 대체 / UI 크기 설정은 연출 캔버스에 미적용 (구도 유지)", { x: 0.6, y: 5.85, w: 12.1, h: 0.4, fontSize: 12, color: MUTED });
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
