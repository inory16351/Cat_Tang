// 11:59 UI / 환경설정 기획서 Ver01 (2026-11-27): 화면 흐름 / 로비 / 마을 / 판 HUD / 판 내 창 / 환경설정 4탭 / 조작 / 언어
// 실행: NODE_PATH=./node_modules node build_ui_ppt.js
// 내용 = 코드 기준 (LobbyScreen / GameUI / HudView / InventoryScreen / SettingsUI / GameSettings / KeyBinds / KeyBindsUI / Loc), 조사 docs/research_ui_settings_1127.md
// 틀 = build_level_ppts.js(makeDeck), 나열 구분자 " / ", 말투 = 명사로 끝내는 개조식 + 한자어 기획 용어
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, FLOOR } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver01";
const FILE = `1159_UI환경설정_기획서_20261127_정승운_${VER}.pptx`;
const U = (n) => `ui_cap/jpg/${n.replace(/.png$/, ".jpg")}`;   // 캡처 JPG 사본 (용량)

function build() {
  const SEC = ["UI 설계 원칙", "로비 / 마을", "판 HUD", "판 내 창", "환경설정", "조작", "언어"];
  const D = makeDeck(`11:59 UI 환경설정 기획서 ${VER}`, `11:59 / UI 환경설정 기획서 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.27"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;

  D.cover("02_ingame.png", "UI 환경설정 기획서", "화면 구성 / HUD / 환경설정 4탭 / 조작", `Ver ${VER.slice(3)}`);
  D.toc(U("settings_1.png"));

  // 01 UI 설계 원칙 ------------------------------------------------------------
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "탄환 시계가 중심인 UI");
    const goals = [
      ["시선 중심 = 탄환 시계", "화면 하단 중앙 HUD에 실린더 / 시곗바늘 / 열기 / 경험치 집중\n다음 발사 시각과 판정 결과를 한 줄로 즉시 확인", GOLD],
      ["태엽 서부 양피지 톤", "나무 / 황동 테두리 + 양피지 창 / 빨간 리본 배너\n로비와 마을, 판 내 창 모두 같은 프레임 사용", TEAL],
      ["필요한 정보만 노출", "튜토리얼 중 미해금 HUD 숨김 / 대화 중 HUD 숨김\n도움말 줄은 해금된 키만 표시", RED],
    ];
    goals.forEach((g, i) => {
      const y = 1.6 + i * 1.45;
      card(s, 0.6, y, 6.2, 1.3);
      s.addShape(pres.shapes.RECTANGLE, { x: 0.6, y: y + 0.2, w: 0.07, h: 0.9, fill: { color: g[2] }, line: { color: g[2] } });
      text(s, g[0], { x: 0.85, y: y + 0.12, w: 5.8, h: 0.38, fontSize: 16, bold: true, color: g[2] });
      text(s, g[1], { x: 0.85, y: y + 0.52, w: 5.8, h: 0.72, fontSize: 12.5, lineSpacingMultiple: 1.1 });
    });
    picture(s, U("hud3.png"), 7.1, 1.6, 5.6, 3.15, "판 HUD", "cover");
    caption(s, "판 HUD (하단 중앙 탄환 시계)", 7.1, 4.83, 5.6);
    card(s, 0.6, 6.05, 12.1, 0.8, PURPLE);
    text(s, [{ text: "기준 해상도  ", options: { bold: true, color: GOLD } }, { text: "1920 × 1080 / 세로 기준 스케일 / 환경설정 UI 크기 80~130% 배율 추가 적용" }], { x: 0.9, y: 6.05, w: 11.5, h: 0.8, fontSize: 15, valign: "middle", align: "center" });
  }
  {
    const s = content(1, "화면 흐름");
    const st = [["로비", "인트로 8.2초\n메뉴 4개"], ["저장 슬롯", "기억 1~3\n시작 / 덮어쓰기"], ["마을", "섬 / 시설 / NPC\n시간의 문"], ["판", "전투방 → 보상 문\n→ 보스"], ["결과", "돌파 / 사망 / 포기\n조각 정산"]];
    const n = st.length, gap = 0.3, bw = (12.1 - gap * (n - 1)) / n;
    st.forEach((t, i) => {
      const x = 0.6 + i * (bw + gap);
      card(s, x, 1.7, bw, 1.6, i === 3 ? PURPLE : CARD);
      text(s, t[0], { x: x + 0.1, y: 1.82, w: bw - 0.2, h: 0.42, fontSize: 17, bold: true, color: GOLD, align: "center" });
      text(s, t[1], { x: x + 0.1, y: 2.3, w: bw - 0.2, h: 0.9, fontSize: 12.5, align: "center", lineSpacingMultiple: 1.15 });
      if (i < n - 1) arrow(s, x + bw + 0.03, 2.5, gap - 0.06, GOLD);
    });
    D.table(s, [
      ["화면", "열기", "닫기", "게임 정지"],
      ["인벤토리", "Tab", "Tab / Esc / 닫기 버튼", "정지 없음 (전투 중 보기 전용)"],
      ["탄 강화", "인벤토리에서 탄 클릭", "Esc / 닫기 버튼", "정지 없음"],
      ["레벨 업 3택", "레벨 상승 시 자동", "선택 시", "정지"],
      ["상인", "상인 상호작용 (E)", "Esc / 닫기 버튼", "정지"],
      ["지도", "M", "M / Esc", "정지 없음"],
      ["일시 정지", "Esc", "Esc / 계속하기", "정지"],
    ], 0.6, 3.6, [2.4, 3.3, 3.2, 3.2], { rowH: 0.42, size: 12 });
  }

  // 02 로비 / 마을 ------------------------------------------------------------
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "로비와 저장 슬롯");
    picture(s, U("lobby_ko.png"), 0.6, 1.6, 5.9, 3.32, "로비", "cover");
    caption(s, "로비 (새로 시작 / 이어하기 / 설정 / 게임 종료)", 0.6, 4.98, 5.9);
    picture(s, U("slots.png"), 6.8, 1.6, 5.9, 3.32, "저장 슬롯", "cover");
    caption(s, "이어하기 저장 슬롯 3개", 6.8, 4.98, 5.9);
    bullets(s, [
      "인트로 8.2초: 고양이 얼굴 근접 → 잉크 선 → 줌아웃 → 색 번짐 → 타이틀 도장 + 흔들림 → 메뉴 등장 / 클릭 또는 키 입력 시 생략",
      "이어하기: 사용 중인 슬롯 없으면 비활성 (회색) / 게임 종료: \"정말 종료하시겠습니까?\" 확인",
      "슬롯 카드: '기억 n' / 진행도 ('n장까지 넘음' / '아직 1장 앞' / '11:59를 넘음') / 시간의 조각 / 판 횟수 / 마지막 플레이 시각",
      "덮어쓰기 / 지우기 = 2회 클릭 확인 (\"정말 덮어쓸까?\" / \"한 번 더 = 지움\") / 슬롯 선택 시 라벤더 안개 전환 → 마을",
    ], { x: 0.6, y: 5.35, w: 12.1, h: 1.6, fontSize: 11.5, paraSpaceAfter: 3 });
  }
  {
    const s = content(2, "마을과 시간의 문");
    picture(s, U("village.png"), 0.6, 1.6, 5.9, 3.32, "마을", "cover");
    caption(s, "마을 (시설 입구 'E 들어가기' / 길 안내 화살표)", 0.6, 4.98, 5.9);
    picture(s, U("gate.png"), 6.8, 1.6, 5.9, 3.32, "시간의 문", "cover");
    caption(s, "시간의 문 (장 5줄 / 출발 버튼)", 6.8, 4.98, 5.9);
    bullets(s, [
      "마을 HUD: 방 이름 줄 '여관 앞 마을' + 도움말 줄만 표시 / M = 해금한 섬 / 다리 / 시설만 그린 마을 지도",
      "시간의 문: 장 5줄 상태 '넘음 ✓' / '이번 목표' / '잠김' + 버튼 '출발: 1장부터' (항상 1장부터 / 목숨 1개)",
      "길 안내 화살표: 노란 화살표 상하 반동 + 대상 이름 / 대상이 화면 밖이면 화면 가장자리에 고정",
    ], { x: 0.6, y: 5.35, w: 12.1, h: 1.6, fontSize: 12, paraSpaceAfter: 4 });
  }

  // 03 판 HUD ------------------------------------------------------------
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "판 HUD 배치");
    picture(s, U("hud3.png"), 0.6, 1.6, 6.4, 3.6, "판 HUD", "cover");
    caption(s, "판 HUD 전체", 0.6, 5.28, 6.4);
    D.table(s, [
      ["위치", "요소"],
      ["왼쪽 위", "체력 바 + HP 숫자 / 대시 아이콘 3개 (아래부터 충전)"],
      ["왼쪽", "활성 특성 목록 (단계 표시 2 / 4 / 6)"],
      ["가운데 위", "진행 줄 'n장 / 스테이지 s/4 / 방 r/n' / 보스 체력 바"],
      ["오른쪽 위", "골드 / 시간의 조각 'N (+이번 판)'"],
      ["가운데 아래", "탄환 시계 HUD (화면 높이 25%) / 상호작용 'E 대상'"],
      ["왼쪽 아래", "도움말 줄 (해금된 키만)"],
      ["오른쪽 아래", "미니맵 250 (좁은 화면 170)"],
    ], 7.3, 1.6, [1.5, 3.9], { rowH: 0.56, size: 11 });
    bullets(s, [
      "고양이 머리 위 '<<<' 대시 잔여 표시 / 불릿 파티 준비 시 화면 가장자리 불꽃",
      "조준 커서 84px, 배경 색 반전 / 전투 종료 시 진행 줄에 '다음 방 / 문으로' 추가",
    ], { x: 0.6, y: 5.75, w: 6.5, h: 1.2, fontSize: 11.5, paraSpaceAfter: 4 });
  }
  {
    const s = content(3, "탄환 시계 HUD와 미니맵");
    card(s, 0.6, 1.6, 5.95, 4.2, PURPLE);
    text(s, "탄환 시계 HUD", { x: 0.85, y: 1.75, w: 5.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "구성: 열기 3칸 / 실린더 2개 × 6발 (왼손 짝수 / 오른손 홀수) / 시곗바늘",
      "하단: 경험치 바 + 레벨 배지",
      "글줄 1: '다음 s시 / 탄 이름 / 판정 j시 / 활성화' 또는 '장전 중…'",
      "글줄 2: '열기 x/3' → 최대 시 'Q 불릿 파티!'",
      "글줄 3: '총 스킬 n초' / '준비'",
      "판정 줄은 판정 시각 튜토리얼 이후 표시",
    ], { x: 0.85, y: 2.25, w: 5.5, h: 3.5, fontSize: 12.5, paraSpaceAfter: 5 });
    card(s, 6.75, 1.6, 5.95, 4.2);
    text(s, "미니맵 / 보스 체력 바", { x: 7.0, y: 1.75, w: 5.5, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "미니맵: 적 점 / 문 / 고양이 + 조준 방향 바늘 / 전투 중 반투명",
      "판 1 / 엔딩 연출 중 미니맵 숨김",
      "보스 체력 바 980 × 86 / 피격 시 흰 잔상 감소",
      "보스 체력 바 이름 = 칭호 + 이름 (5장 3페이즈는 '11:59:SS' 시계 표기)",
      "중간 보스 방 진행 줄 = '중간 보스' / 보스 방 = 보스 이름",
    ], { x: 7.0, y: 2.25, w: 5.5, h: 3.5, fontSize: 12.5, paraSpaceAfter: 5 });
    card(s, 0.6, 6.0, 12.1, 0.85, FLOOR);
    text(s, [{ text: "의도  ", options: { bold: true, color: GOLD } }, { text: "전투 중 시선 이동 최소화 / 빌드 판단에 필요한 정보는 하단 중앙에 모음" }], { x: 0.9, y: 6.0, w: 11.5, h: 0.85, fontSize: 14, valign: "middle", align: "center" });
  }

  // 04 판 내 창 ------------------------------------------------------------
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "인벤토리와 탄 강화");
    picture(s, U("inventory.png"), 0.6, 1.6, 5.9, 3.32, "인벤토리", "cover");
    caption(s, "인벤토리 1760 × 940", 0.6, 4.98, 5.9);
    picture(s, U("enhance.png"), 6.8, 1.6, 5.9, 3.32, "탄 강화", "cover");
    caption(s, "탄 강화 창 1240 × 760", 6.8, 4.98, 5.9);
    bullets(s, [
      "인벤토리: 왼쪽 탄환 시계 12칸 ('왼손 / 짝수' / '오른손 / 홀수') / 가운데 보관함 4 × 3 + '끌어서 팔기' / 오른쪽 '설명' / '능력치' 탭",
      "전투 중에는 보기 전용 (\"전투 중에는 교체할 수 없습니다\")",
      "탄 강화: 연산 칸 '위 (먼저 계산)' / '아래' + '보관함의 연산' + 미리 보기 줄 '이 탄이 활성화되는 칸 (지금 연산 기준)'",
    ], { x: 0.6, y: 5.35, w: 12.1, h: 1.6, fontSize: 12, paraSpaceAfter: 4 });
  }
  {
    const s = content(4, "레벨 업 / 상인 / 보상 문");
    picture(s, U("levelup.png"), 0.6, 1.6, 3.9, 2.2, "레벨 업", "cover");
    caption(s, "레벨 업 3택", 0.6, 3.86, 3.9);
    picture(s, U("shop.png"), 4.7, 1.6, 3.9, 2.2, "상인", "cover");
    caption(s, "떠돌이 상인", 4.7, 3.86, 3.9);
    picture(s, U("reward_doors.png"), 8.8, 1.6, 3.9, 2.2, "보상 문", "cover");
    caption(s, "보상 문", 8.8, 3.86, 3.9);
    const cols = [
      ["레벨 업", ["배너 '레벨 업! Lv N: 하나 고르기' / 게임 정지", "카드 430 × 520 / 간격 470 / 버튼 '고르기'", "'다시 뽑기 (1회)' (기억의 방 기술)", "총 선택 시 교체 창: 왼손 / 오른손 / 버리기"]],
      ["상인", ["창 1400 × 820 / 카드 3열 400 × 240", "버튼 '{가격} 골드' / '판매됨'", "'하트 / 체력 N%' / '새로고침 (N 골드)'", "화면 밖 상인 = 동전 아이콘 가장자리 화살표"]],
      ["보상 문", ["나무 문 위 보상 아이콘", "전투 종료 시 문마다 작은 화살표", "전부 화면 밖이면 '보상 문 N개' 하나로 통합", "스테이지 4 = '최종 보스' 표시"]],
    ];
    cols.forEach((c, i) => {
      const x = 0.6 + i * 4.1;
      card(s, x, 4.25, 3.9, 2.25, i === 0 ? PURPLE : CARD);
      text(s, c[0], { x: x + 0.2, y: 4.35, w: 3.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
      bullets(s, c[1], { x: x + 0.2, y: 4.78, w: 3.55, h: 2.0, fontSize: 11, paraSpaceAfter: 3 });
    });
  }
  {
    const s = content(4, "지도 / 일시 정지 / 결과 화면");
    picture(s, U("map.png"), 0.6, 1.6, 3.9, 2.2, "지도", "cover");
    caption(s, "지도 (M)", 0.6, 3.86, 3.9);
    picture(s, U("pause.png"), 4.7, 1.6, 3.9, 2.2, "일시 정지", "cover");
    caption(s, "일시 정지 (Esc)", 4.7, 3.86, 3.9);
    picture(s, U("results.png"), 8.8, 1.6, 3.9, 2.2, "결과 화면", "cover");
    caption(s, "결과 화면", 8.8, 3.86, 3.9);
    const cols = [
      ["지도", ["양피지 판에 방 타일 / 복도 / 문", "중간 보스 / 보스 아이콘 + 고양이 머리", "게임 정지 없음 / M 또는 Esc로 닫기"]],
      ["일시 정지", ["배너 '잠시 멈춤' + 환경설정 4탭 (1180 × 900)", "판: '계속하기' / '전투 포기' (포기 확인 창에 반입 조각 수 표시)", "마을: '계속하기' / '메인 로비로' + 슬롯 정보 줄"]],
      ["결과 화면", ["배너 'n장 돌파' / '11:59를 넘었다' / '시간에 삼켜졌다'", "시간의 조각 +N (보유 N) / 골드 소멸 안내 / 시설 해금 / 다음 판 목표", "버튼 '마을로' / 장 최초 돌파 시 결과 화면 대신 기절 → 꿈"]],
    ];
    cols.forEach((c, i) => {
      const x = 0.6 + i * 4.1;
      card(s, x, 4.25, 3.9, 2.25, i === 1 ? PURPLE : CARD);
      text(s, c[0], { x: x + 0.2, y: 4.35, w: 3.5, h: 0.36, fontSize: 15, bold: true, color: GOLD });
      bullets(s, c[1], { x: x + 0.2, y: 4.78, w: 3.55, h: 2.0, fontSize: 11, paraSpaceAfter: 3 });
    });
  }
  {
    const s = content(4, "대화창");
    card(s, 0.6, 1.6, 12.1, 1.9, FLOOR);
    // 대화창 구성 도식: 왼쪽 고양이 초상화 / 가운데 대사 / 오른쪽 상대 초상화
    D.rect(s, 0.9, 1.8, 1.6, 1.5, CARD, { line: GOLD });
    text(s, "고양이\n초상화", { x: 0.9, y: 1.8, w: 1.6, h: 1.5, fontSize: 12, color: MUTED, align: "center", valign: "middle" });
    D.rect(s, 10.8, 1.8, 1.6, 1.5, CARD, { line: GOLD });
    text(s, "상대\n초상화", { x: 10.8, y: 1.8, w: 1.6, h: 1.5, fontSize: 12, color: MUTED, align: "center", valign: "middle" });
    D.rect(s, 2.75, 2.0, 7.8, 1.1, CARD, { line: TXT, lineW: 0.75 });
    text(s, "\"윽...... 꿈인가.......\"", { x: 2.95, y: 2.0, w: 7.4, h: 1.1, fontSize: 15, valign: "middle" });
    chip(s, "이름표 (말하는 쪽 아래)", 0.9, 3.12, 2.4, GOLD, "17131F", { size: 10, h: 0.3 });
    bullets(s, [
      "대사 데이터 형식 '이름#표정::대사' / 표정에 따라 초상화 연출 (예: 두통 = 흔들림 + 빨간 번쩍임)",
      "넘기기: E / Space / Enter / 클릭 / 글자 출력 중 입력 시 즉시 전체 표시",
      "화자 변경 시 이름표가 이전 자리에서 사라진 뒤 새 자리에 등장 (좌우 미끄러짐 없음)",
      "대화 중 HUD 숨김 / 글자 속도와 자동 넘김은 환경설정 게임 탭 값 적용",
    ], { x: 0.6, y: 3.85, w: 12.1, h: 2.9, fontSize: 13, paraSpaceAfter: 6 });
  }

  // 05 환경설정 ------------------------------------------------------------
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "환경설정 구조 / 소리 탭");
    picture(s, U("settings_0.png"), 0.6, 1.6, 5.9, 3.32, "소리 탭", "cover");
    caption(s, "소리 탭 (로비 설정 / Esc 창 공용)", 0.6, 4.98, 5.9);
    bullets(s, [
      "4탭 '소리' / '화면' / '게임' / '조작' / 마지막으로 연 탭 기억",
      "로비 설정 창과 판 / 마을 Esc 창에서 같은 구성 사용",
      "저장: 모든 슬롯 공통 (PlayerPrefs cattang.opt.*) / 변경 즉시 적용",
      "줄 높이 52 / 간격 58 / 조작 칸 폭 430 / 9-slice 프레임 그림",
    ], { x: 6.8, y: 1.6, w: 5.9, h: 2.4, fontSize: 12.5, paraSpaceAfter: 5 });
    D.table(s, [
      ["소리 항목", "방식", "기본값"],
      ["전체 음량", "슬라이더", "1.0"],
      ["음악 음량", "슬라이더", "0.55"],
      ["효과음 음량", "슬라이더", "0.8"],
      ["창 밖에서 소리 끄기", "켬 / 끔", "켬"],
    ], 6.8, 4.15, [2.7, 1.8, 1.4], { rowH: 0.45, size: 12 });
  }
  {
    const s = content(5, "화면 탭");
    picture(s, U("settings_1.png"), 0.6, 1.6, 5.9, 3.32, "화면 탭", "cover");
    caption(s, "화면 탭", 0.6, 4.98, 5.9);
    D.table(s, [
      ["항목", "방식 / 기본값", "적용 범위"],
      ["창 모드", "전체 화면 / 창 / 테두리 없는 창", "예전 전체 화면 값 이어받음"],
      ["해상도", "◀▶ '모니터 그대로' + 폭 1024 이상", "빌드 전용"],
      ["수직 동기화", "켬 / 끔 (켬)", ""],
      ["화면 흔들림", "0~1 (1)", "카메라 / 보스 등장 / 스토리 연출 / 로비 도장"],
      ["번쩍임 / 화면 효과 줄이기", "켬 / 끔 (끔) / 켬 = 배율 0.3", "피격 / 글리치 / 잔상 / 보스 연출 / 두통"],
      ["UI 크기", "80~130%, 10% 단위 (100%)", "HUD / 로비 / 지도 / 화살표 / 연습 표시"],
    ], 6.8, 1.6, [1.75, 2.15, 2.0], { rowH: 0.62, size: 10.5 });
    bullets(s, [
      "번쩍임 줄이기 예외: 4장 예고 잔상은 공격 판독 정보라 그대로 유지",
      "UI 크기 예외: 꿈 / 보스 등장 등 연출 캔버스는 배율 미적용 (화면 구도 유지)",
    ], { x: 0.6, y: 5.4, w: 5.9, h: 1.5, fontSize: 12, paraSpaceAfter: 5 });
  }
  {
    const s = content(5, "게임 탭");
    picture(s, U("settings_2.png"), 0.6, 1.6, 5.9, 3.32, "게임 탭", "cover");
    caption(s, "게임 탭", 0.6, 4.98, 5.9);
    D.table(s, [
      ["항목", "방식 / 기본값", "효과"],
      ["언어", "◀▶ 언어 이름 (한국어)", "즉시 재번역"],
      ["대사 자동 넘김", "켬 / 끔 (끔)", "1.2초 + 글자당 0.05초 후 다음 줄"],
      ["글자 속도", "느림 / 보통 / 빠름 / 바로 (보통)", "초당 42자 ÷ 배율 1.6 / 1 / 0.5 / 0"],
      ["조준선 모양", "기본형 / 십자 / 점 (기본형)", "쐐기 + 마름모 / 가는 선 4개 / 빨간 마름모"],
      ["조준선 크기", "0.6~1.6배 (1.0)", ""],
      ["보스 패턴 연습 (장별)", "1~5장 체크 (끔)", "스토리 맵을 연습 모드로 재진행 (튜토리얼 기획서)"],
    ], 6.8, 1.6, [1.75, 2.15, 2.0], { rowH: 0.62, size: 10.5 });
    bullets(s, [
      "난이도 조절 / 쉬운 모드는 미제공 (승천 모드로 대체) / 피해 숫자 / 자동 조준 / 프레임 제한 제외",
    ], { x: 0.6, y: 5.4, w: 5.9, h: 1.2, fontSize: 12, paraSpaceAfter: 5 });
  }

  // 06 조작 ------------------------------------------------------------
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "조작 탭 / 키 바인딩");
    picture(s, U("settings_3.png"), 0.6, 1.6, 5.9, 3.32, "조작 탭", "cover");
    caption(s, "조작 탭 (2열 × 5줄 키캡)", 0.6, 4.98, 5.9);
    D.table(s, [
      ["행동", "기본 키", "행동", "기본 키"],
      ["위로 이동", "W", "상호작용", "E"],
      ["아래로 이동", "S", "인벤토리", "Tab"],
      ["왼쪽으로 이동", "A", "지도", "M"],
      ["오른쪽으로 이동", "D", "불릿 파티", "Q"],
      ["대시", "Space", "총 스킬", "우클릭"],
    ], 6.8, 1.6, [1.75, 1.2, 1.75, 1.2], { rowH: 0.48, size: 12 });
    bullets(s, [
      "고정 키: Esc / 좌클릭 사격 / 대사 넘김",
      "지정 가능 마우스 버튼: 우클릭 / 휠 클릭 / 마우스 4 / 마우스 5",
      "키캡 클릭 → '키를 누르세요' 대기 / Esc 또는 다른 곳 클릭 시 취소 / 변경된 키는 연한 금색",
      "중복 키 선택 시 두 행동의 키 교체 / 저장값 중복 시 뒤쪽 행동을 기본값으로 복구",
      "안내 문구 + '기본값으로' 버튼 / 안내 창 키 표시 (극한 회피 등)에 바인딩 반영",
    ], { x: 0.6, y: 5.35, w: 12.1, h: 1.6, fontSize: 11.5, paraSpaceAfter: 3 });
  }

  // 07 언어 ------------------------------------------------------------
  pres.addSection({ title: SEC[6] });
  {
    const s = content(7, "언어와 번역 구조");
    D.table(s, [
      ["언어", "표기", "비고"],
      ["한국어", "한국어", "원문 (코드 내 한국어 문자열)"],
      ["영어", "English", "1,707개 / 용어집 기준 번역 + 안티그라비티 검토"],
      ["중국어 간체", "简体中文", "사전 적용"],
      ["일본어 / 프랑스어 / 독일어 / 러시아어", "日本語 / Français / Deutsch / Русский", "용어집 작성 / 사전 제작 중"],
      ["아랍어", "Arabic (아랍 문자)", "오른쪽 → 왼쪽 표시 / 전용 글꼴"],
    ], 0.6, 1.6, [3.6, 3.9, 4.6], { rowH: 0.55, size: 12.5 });
    bullets(s, [
      "번역 방식: 한국어 원문을 키로 사전 조회 → 전체 문장 → 줄 단위 → {0} 틀 → 숫자 틀 순서",
      "UI 글자 컴포넌트가 언어 변경 시 즉시 재번역 / 미번역 문구는 수집 후 사전에 추가",
      "언어별 글꼴 교체 / 표정 토큰 등 화면에 나오지 않는 문자열은 번역 대상 제외",
      "사전이 없는 언어 선택 시 한국어 원문 표시",
    ], { x: 0.6, y: 5.4, w: 12.1, h: 1.55, fontSize: 12.5, paraSpaceAfter: 4 });
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
