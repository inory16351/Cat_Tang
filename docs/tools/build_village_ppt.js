// 11:59 마을 기획서 Ver02 (2026-11-27 게임과 맞춤): 의상실 1장 · 탄약 공방 / 총포상 = 2장 퀘스트 · 시작 총 고르기 삭제 · 기억의 방 갈래 트리 · 장별 마을 퀘스트
// Ver01 (2026-11-18): 메인 마을 구조 · 진행 흐름 · 섬 해금 · NPC · 연출
// 실행: NODE_PATH=<pptxgenjs@3.12 · image-size@1 있는 node_modules> node build_village_ppt.js
// 틀 · 색 · 도식 도우미는 build_level_ppts.js(makeDeck)와 같음. 나열 구분자는 " / "
// 그림: docs/images/village/ (Unity 캡처 · NPC 원화 · 키트 미리보기)
const path = require("path");
const L = require("./build_level_ppts.js");
const { makeDeck, AUTHOR, BG, CARD, TXT, MUTED, GOLD, RED, TEAL, PURPLE, LAV, FLOOR, has } = L;

const OUT_DIR = process.env.OUT_DIR || path.resolve(__dirname, "..");
const VER = "Ver02";
const FILE = `1159_마을_기획서_20261127_정승운_${VER}.pptx`;
const V = (n) => `village/${n}`;
const DAWN = "F2A877", NIGHT = "2A2458";

function build() {
  const SEC = ["마을 개요", "진행 흐름", "섬 구조", "해금", "NPC", "연출"];
  const D = makeDeck(`11:59 마을 기획서 ${VER}`, `11:59 / 마을 기획서: 새벽의 섬 마을 ${VER} / ${AUTHOR}`, SEC);
  D.date = "2026.11.27"; D.sep = " / ";
  const { pres, text, card, bullets, picture, caption, chip, arrow, content } = D;

  D.cover(V("map_all.png"), "마을 기획서", "새벽의 섬 마을 / 진행 흐름 / 섬 해금 / 마을 퀘스트", "Ver 02");
  D.toc(V("center_isle.png"));

  // 01 마을 개요 ------------------------------------------------------------
  pres.addSection({ title: SEC[0] });
  {
    const s = content(1, "새벽에 사라져 가는 서부 마을");
    picture(s, V("map_all.png"), 0.6, 1.6, 7.3, 4.11, "마을 전경", "cover");
    caption(s, "마을 전경 (모든 섬이 열린 상태)", 0.6, 5.8, 7.3);
    card(s, 8.2, 1.6, 4.5, 5.25);
    text(s, "컨셉", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "시계공의 꿈 끝자락, 녹아 사라지다 남은 서부 마을",
      "판이 끝나면 돌아와 다시 눈을 뜨는 곳",
      "새벽 직전의 허공에 뜬 섬 여러 개 + 섬을 잇는 다리",
      "하늘 = 별밤과 은하수, 지평선만 금빛으로 밝아 옴",
      "건물 위쪽부터 시계 / 모래로 녹아 흩어지고, 조각이 천천히 떠오름",
      "물체는 따뜻한 색(바랜 나무 / 놋쇠 / 금 / 크림), 차가운 색은 하늘 / 허공에만",
    ], { x: 8.45, y: 2.25, w: 4.05, h: 4.4, fontSize: 13.5, paraSpaceAfter: 6 });
    s.addNotes("모델: 1장 전투 키트를 쓰지 않는 마을 전용 키트 (블렌더 + Codex). 건물 5 / 섬 가장자리 · 떠도는 섬 · 광장 · 길 9 / 다리 2 / 소품 15. NPC 5는 Tripo.");
  }
  {
    const s = content(1, "마을 구성");
    D.table(s, [
      ["섬", "시설", "NPC", "열리는 때"],
      ["가운데 섬", "시계 광장 / 여관 2층 기억의 방", "태엽 풀린 뻐꾸기", "처음부터 (기억의 방은 1장 클리어)"],
      ["문 섬 (남)", "시간의 문", "괘종시계 문지기", "처음부터"],
      ["북동 섬", "의상실", "실타래 재봉사", "1장 클리어"],
      ["동쪽 섬", "탄약 공방", "모래시계 대장장이", "2장 클리어 + 흐르는 모래 퀘스트"],
      ["서쪽 섬", "총포상", "빈 외투 보안관", "2장 클리어 + 결투 퀘스트"],
    ], 0.6, 1.7, [2.2, 3.6, 2.9, 3.4], { rowH: 0.62, size: 14 });
    text(s, "가운데 섬에서 다리 4개가 문 섬 / 북동 섬 / 동쪽 섬 / 서쪽 섬으로 이어짐. 잠긴 섬의 다리는 끊긴 채 허공에 떠 있음 / 광장 위엔 멈춘 시곗바늘", { x: 0.6, y: 6.05, w: 12.1, h: 0.6, fontSize: 14, color: MUTED });
  }

  // 02 진행 흐름 ------------------------------------------------------------
  pres.addSection({ title: SEC[1] });
  {
    const s = content(2, "한 판의 흐름");
    // 위: 처음 시작
    const steps = [["새로 시작", "저장 슬롯 선택"], ["1장 전투", "마을 없이 바로"], ["판 끝", "쓰러짐 / 1장 클리어"], ["마을에서 깨어남", "연기 속에서 기상"], ["뻐꾸기 대화", "마을 / 시간의 문 안내"]];
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.5;
      card(s, x, 1.65, 2.15, 1.15, i === 1 ? PURPLE : CARD);
      text(s, st[0], { x: x + 0.15, y: 1.75, w: 1.9, h: 0.45, fontSize: 15, bold: true, color: i === 1 ? GOLD : TXT, align: "center" });
      text(s, st[1], { x: x + 0.15, y: 2.2, w: 1.9, h: 0.45, fontSize: 11.5, color: MUTED, align: "center" });
      if (i < steps.length - 1) arrow(s, x + 2.18, 2.22, 0.3, GOLD);
    });
    // 아래: 판마다 늘어나는 장
    text(s, "이후의 판 (세피리아처럼 판은 상시 1장부터)", { x: 0.6, y: 3.15, w: 8, h: 0.4, fontSize: 16, bold: true, color: GOLD });
    const runs = [["2번째 판", ["1장"], "1장 클리어 → 마을 귀환"], ["3번째 판", ["1장", "2장"], "1 / 2장 연이어 클리어 → 마을 귀환"], ["4번째 판", ["1장", "2장", "3장"], "1 / 2 / 3장 연이어 클리어 → 마을 귀환"]];
    runs.forEach((r, i) => {
      const y = 3.7 + i * 0.95;
      text(s, r[0], { x: 0.6, y, w: 1.4, h: 0.7, fontSize: 14, bold: true, valign: "middle" });
      r[1].forEach((c, k) => {
        chip(s, c, 2.1 + k * 1.35, y + 0.15, 1.15, k === r[1].length - 1 ? GOLD : LAV, BG, { size: 13 });
        if (k < r[1].length - 1) arrow(s, 2.1 + k * 1.35 + 1.17, y + 0.35, 0.16, MUTED);
      });
      text(s, r[2], { x: 6.3, y, w: 3.6, h: 0.7, fontSize: 13, color: MUTED, valign: "middle" });
    });
    card(s, 10.0, 3.2, 2.7, 3.4);
    bullets(s, [
      "이번 판 목표 = 넘은 장 + 1",
      "목표 장 보스를 넘거나 쓰러지면 마을로",
      "시간의 조각은 남음",
      "골드는 소멸",
    ], { x: 10.15, y: 3.35, w: 2.45, h: 3.1, fontSize: 12.5, paraSpaceAfter: 6 });
  }
  {
    const s = content(2, "첫 판과 첫 귀환");
    picture(s, V("talk_cuckoo.png"), 0.6, 1.6, 7.3, 4.1, "뻐꾸기 첫 대화", "cover");
    caption(s, "첫 귀환: 깨어난 뒤 태엽 풀린 뻐꾸기의 첫 대화", 0.6, 5.8, 7.3);
    card(s, 8.2, 1.6, 4.5, 5.25);
    text(s, "처음 시작할 때", { x: 8.45, y: 1.75, w: 4, h: 0.4, fontSize: 17, bold: true, color: GOLD });
    bullets(s, [
      "새 슬롯은 마을을 거치지 않고 1장 전투부터",
      "판이 끝나면 마을 가운데 섬, 여관 앞에서 깨어남",
      "뻐꾸기가 마을 / 반복되는 11:59 / 시간의 문을 안내",
      "첫 귀환 때 열린 곳: 가운데 섬 / 문 섬",
      "기억의 방은 1장을 넘기 전까지 잠김",
      "판 도중 쓰러지면 뻐꾸기가 쓰러진 곳에 반응하는 한 줄",
      "마을에서 ESC: 잠시 멈춤 (계속하기 / 환경설정 / 메인 로비로) / M: 마을 지도",
    ], { x: 8.45, y: 2.25, w: 4.05, h: 4.4, fontSize: 13.5, paraSpaceAfter: 6 });
  }

  // 03 섬 구조 ------------------------------------------------------------
  pres.addSection({ title: SEC[2] });
  {
    const s = content(3, "섬 지도");
    picture(s, V("map_locked.png"), 0.6, 1.55, 8.6, 4.84, "섬 지도", "cover");
    caption(s, "처음 귀환 때의 마을: 문 섬 다리만 이어져 있고 나머지 다리는 끊김", 0.6, 6.45, 8.6);
    const tags = [["가운데 섬", "광장 / 여관 / 뻐꾸기", GOLD], ["문 섬 (남)", "시간의 문 / 문지기", GOLD], ["북동 섬", "의상실 / 1장", LAV], ["동쪽 섬", "탄약 공방 / 2장 퀘스트", LAV], ["서쪽 섬", "총포상 / 2장 퀘스트", RED]];
    tags.forEach((t, i) => {
      const y = 1.6 + i * 0.98;
      card(s, 9.5, y, 3.2, 0.85);
      D.circle(s, 9.78, y + 0.42, 0.11, t[2]);
      text(s, t[0], { x: 10.0, y: y + 0.06, w: 2.6, h: 0.38, fontSize: 14, bold: true });
      text(s, t[1], { x: 10.0, y: y + 0.42, w: 2.6, h: 0.36, fontSize: 11.5, color: MUTED });
    });
  }
  {
    const s = content(3, "섬별 구성");
    const cols = [
      ["가운데 섬", "타원 34 x 30m", ["시계 광장 (바닥 시계판 + 녹는 시계 기둥 + 떠서 도는 시곗바늘)", "여관 2층 기억의 방", "우물 / 마차 / 벤치 / 가로등 / 선인장", "판이 끝나면 여관 앞에서 깨어남"]],
      ["문 섬", "타원 20 x 16m", ["시간의 문 (괘종시계 기둥 두 개 + 11:59 시계)", "문 너머 = 섬 끝의 허공", "괘종시계 문지기"]],
      ["동쪽 / 북동 / 서쪽 섬", "각 타원 약 20 x 18m", ["시설 건물 1채 + 그 시설 NPC", "풍차 / 물탱크 / 울타리 / 상자", "다리가 끊긴 동안은 건너갈 수 없음"]],
    ];
    cols.forEach((c, i) => {
      const x = 0.6 + i * 4.1;
      card(s, x, 1.6, 3.85, 4.9);
      text(s, c[0], { x: x + 0.25, y: 1.75, w: 3.4, h: 0.42, fontSize: 17, bold: true, color: GOLD });
      text(s, c[1], { x: x + 0.25, y: 2.17, w: 3.4, h: 0.32, fontSize: 12, color: MUTED });
      bullets(s, c[2], { x: x + 0.25, y: 2.65, w: 3.4, h: 3.6, fontSize: 13, paraSpaceAfter: 6 });
    });
    text(s, "섬 가장자리 = 아래로 매달린 흙 / 바위층과 뿌리, 가장자리부터 바스러져 허공으로 떨어지는 조각. 섬 둘레엔 작은 조각 섬과 부스러기가 떠 있음", { x: 0.6, y: 6.6, w: 12.1, h: 0.4, fontSize: 12.5, color: MUTED });
  }

  // 04 해금 ------------------------------------------------------------
  pres.addSection({ title: SEC[3] });
  {
    const s = content(4, "장 클리어 해금");
    const steps = [
      ["처음", ["가운데 섬 / 문 섬", "뻐꾸기 / 문지기", "시간의 문"]],
      ["1장 클리어", ["기억의 방", "북동 섬 다리 / 의상실", "재봉사 등장", "시곗바늘 수리 퀘스트"]],
      ["2장 클리어", ["동쪽 섬 다리 / 대장장이", "흐르는 모래 → 탄약 공방", "서쪽 섬 다리 / 보안관", "결투 → 총포상"]],
      ["3장 클리어", ["거울 마을 퀘스트", "광장에 거울 속 고양이", "해금 없음"]],
      ["4장 클리어", ["멈춘 마을 퀘스트", "섬마다 시계 감기", "해금 없음"]],
      ["5장 클리어", ["작별 / 엔딩", "크레딧 뒤 복원된 마을"]],
    ];
    D.line(s, 0.9, 2.45, 11.5, 0, MUTED, { width: 2 });
    steps.forEach((st, i) => {
      const x = 0.6 + i * 2.05;
      D.circle(s, x + 0.3, 2.45, 0.16, i === 0 ? LAV : GOLD, { line: TXT });
      text(s, st[0], { x: x + 0.05, y: 1.65, w: 1.9, h: 0.45, fontSize: 15, bold: true, color: i === 0 ? TXT : GOLD });
      card(s, x, 2.85, 1.9, 2.75);
      bullets(s, st[1], { x: x + 0.12, y: 3.0, w: 1.7, h: 2.5, fontSize: 11.5, paraSpaceAfter: 5 });
    });
    text(s, "장을 넘고 처음 마을에 돌아오면 끊긴 다리가 이어지는 연출 → 건너편 NPC의 첫 대화 / 탄약 공방 · 총포상은 장 돌파만으로는 미활성화 / 퀘스트 완료 시 활성화", { x: 0.6, y: 5.85, w: 12.1, h: 0.7, fontSize: 13.5, color: MUTED });
  }
  {
    const s = content(4, "시설별 해금 방식");
    D.table(s, [
      ["시설", "성장 방식", "대상"],
      ["기억의 방", "시간의 조각으로 능력치 노드 성장", "화력 / 생존 / 손재주 3갈래 × 4층, 장 n개 돌파 시 n층 활성화"],
      ["탄약 공방", "해금 묶음 구매 (여정 60 / 마무리 80 조각)", "2장 묶음 탄 6발, 장마다 묶음 추가"],
      ["총포상", "해금 묶음 구매 (가스등 랜턴 70 / 기관차 80 조각)", "2장 묶음 총 2자루, 장마다 묶음 추가"],
      ["의상실", "업적 조건을 채운 코스튬 구매", "코스튬"],
    ], 0.6, 1.7, [2.2, 4.6, 5.3], { rowH: 0.62, size: 14 });
    card(s, 0.6, 5.0, 12.1, 1.45, PURPLE);
    text(s, "기본 총탄 / 총은 처음부터 해금 / 묶음을 산 뒤부터 그 총탄 / 총이 판의 드랍 / 상점에 섞여 나옴 / 첫 묶음 무료 없음 / 판 시작은 상시 양손 기본 총", { x: 0.85, y: 5.1, w: 11.6, h: 1.25, fontSize: 15, bold: true, valign: "middle" });
  }

  {
    const s = content(4, "장별 마을 퀘스트");
    D.table(s, [
      ["때", "퀘스트", "방식", "결과"],
      ["1장 클리어", "시곗바늘 수리", "반딧불 길을 따라 숨은 물건 3개(돋보기 안경 / 팔찌 / 쪽지) → 재봉사", "광장 바늘이 다시 돎"],
      ["2장 클리어", "흐르는 모래 / 결투", "첫 중간 보스의 흐르는 모래 → 대장장이 / 종이 울리면 뽑는 결투 → 보안관", "탄약 공방 / 총포상"],
      ["3장 클리어", "거울 마을", "좌우가 뒤집힌 마을, 거울 속 물건이 원래 있어야 할 자리를 쏴서 거울 깨기", "마을 복귀 / 거울 속 고양이"],
      ["4장 클리어", "멈춘 마을", "섬마다 멈춘 톱니를 정해진 시각 탄으로 쏴서 시간 되돌리기", "마을 시계가 다시 흐름"],
      ["5장 클리어", "작별", "무너지는 섬마다 NPC 작별 → 광장에서 총 내려놓기", "엔딩"],
    ], 0.6, 1.7, [1.7, 2.3, 5.9, 2.2], { rowH: 0.72, size: 12.5 });
    text(s, "장마다 다른 손맛 (모으기 / 판 안 드랍 / 틀린 곳 찾기 / 시각 맞히기) / 대사와 연출은 스토리 기획서", { x: 0.6, y: 6.55, w: 12.1, h: 0.4, fontSize: 13, color: MUTED });
  }

  // 05 NPC ------------------------------------------------------------
  pres.addSection({ title: SEC[4] });
  {
    const s = content(5, "시계공이 잊어버린 존재 5");
    const npcs = [
      ["cuckoo.png", "태엽 풀린 뻐꾸기", "기억의 방", "노래를 잊은 첫 뻐꾸기시계의 새"],
      ["gatekeeper.png", "괘종시계 문지기", "시간의 문", "안개 낀 문자판, 진자가 랜턴"],
      ["hourglass.png", "모래시계 대장장이", "탄약 공방", "깨진 모래시계 몸, 새는 모래로 탄을 부음"],
      ["seamstress.png", "실타래 재봉사", "의상실", "얼굴 대신 실패, 팔은 시곗바늘"],
      ["sheriff.png", "빈 외투 보안관", "총포상", "바랜 필름 같은 빈 외투, 녹는 회중시계 머리"],
    ];
    npcs.forEach((n, i) => {
      const x = 0.6 + i * 2.48;
      card(s, x, 1.55, 2.3, 5.25);
      picture(s, V(n[0]), x + 0.1, 1.65, 2.1, 2.9, n[1], "contain");
      text(s, n[1], { x: x + 0.12, y: 4.65, w: 2.06, h: 0.4, fontSize: 14, bold: true, color: GOLD, align: "center" });
      chip(s, n[2], x + 0.45, 5.1, 1.4, PURPLE, TXT, { size: 11 });
      text(s, n[3], { x: x + 0.15, y: 5.55, w: 2.0, h: 1.1, fontSize: 11.5, color: MUTED, align: "center" });
    });
  }
  {
    const s = content(5, "NPC 공통");
    bullets(s, [
      "사람이 아닌, 시간에 잠식된 몽환적 존재 (사람 얼굴 / 피부 없음)",
      "몬스터(차가운 보라 + 빨간 테두리)와 구분되는 따뜻한 색",
      "자기 섬의 시설 앞에 서 있음, 다리가 이어지기 전엔 보이지 않음",
      "처음 만날 때 한 번 대화, 이후엔 시설 입구로 바로 이용",
      "얼굴 대신 몸이 된 물건으로 감정 표현: 뻐꾸기 깃털 / 문지기 랜턴 밝기 / 재봉사 실 풀림",
      "모습은 플랫 컬러 원화 → 3D 모델 (인게임 툰 셰이더)",
    ], { x: 0.6, y: 1.7, w: 7.0, h: 4.5, fontSize: 16, paraSpaceAfter: 10 });
    picture(s, V("talk_smith.png"), 7.9, 1.65, 4.8, 2.7, "대장장이 첫 대화", "cover");
    caption(s, "2장 클리어 뒤 모래시계 대장장이의 첫 대화", 7.9, 4.45, 4.8);
  }

  // 06 연출 ------------------------------------------------------------
  pres.addSection({ title: SEC[5] });
  {
    const s = content(6, "귀환 / 해금 연출");
    const shots = [["wake.png", "1. 깨어남", "라벤더 안개가 걷히며 연기 속에서 일어남"], ["bridge_reconnect.png", "2. 다리 이어짐", "끊긴 다리 조각이 사라지고 성한 다리가 끝에서부터 이어짐, 카메라가 다리를 비춤"], ["talk_smith.png", "3. 첫 대화", "건너편 NPC가 나타나고 첫 대화"]];
    shots.forEach((sh, i) => {
      const x = 0.6 + i * 4.1;
      picture(s, V(sh[0]), x, 1.6, 3.85, 2.17, sh[1], "cover");
      text(s, sh[1], { x, y: 3.9, w: 3.85, h: 0.4, fontSize: 16, bold: true, color: GOLD });
      text(s, sh[2], { x, y: 4.35, w: 3.85, h: 1.2, fontSize: 13, color: TXT });
    });
    text(s, "장을 여러 개 한 번에 넘으면 열린 다리가 차례로 이어짐", { x: 0.6, y: 6.1, w: 12.1, h: 0.4, fontSize: 13, color: MUTED });
  }
  {
    const s = content(6, "사라져 가는 마을");
    picture(s, V("rising_bits.png"), 0.6, 1.6, 6.6, 3.71, "건물 위 조각", "cover");
    caption(s, "건물 위로 천천히 떠오르는 벽돌 / 판자 조각", 0.6, 5.4, 6.6);
    picture(s, V("kit_buildings.png"), 7.5, 1.6, 5.2, 2.93, "시설 건물", "cover");
    caption(s, "시설 건물 5 (시간의 문 / 의상실 / 탄약 공방 / 총포상 / 여관)", 7.5, 4.6, 5.2);
    bullets(s, [
      "건물 위쪽 조각이 천천히 돌며 떠오르다 작아지며 소멸 (계속 반복)",
      "빛나는 모래 알갱이가 함께 피어오름",
      "섬 가장자리와 다리 끝도 바스러져 허공으로",
    ], { x: 7.5, y: 5.0, w: 5.2, h: 1.8, fontSize: 13, paraSpaceAfter: 5 });
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
