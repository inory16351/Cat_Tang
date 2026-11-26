// 모델 배색표 (2026-11-17 사용자: 앞으로 블렌더 모델은 회색으로 뽑고 코드로 배색)
// 블렌더 + Codex 모델은 재질 이름이 역할 이름(wood · iron · gold_drip …)으로 FBX에 그대로 들어옴.
// 회색으로 뽑은 재질(채도 거의 0)은 빌더(EnemyModelBuilder.ToonFor)가 이 표에서 이름으로 색을 찾아 입힘.
// 색이 들어 있는 예전 모델은 그대로 둠 (회색일 때만 표를 씀). 표에 없는 이름이면 회색 그대로 + 경고.
// 색을 바꾸려면 여기 값을 고치고 메뉴 `CatTang/1장 적 프리팹 만들기 (Tripo)`로 다시 빌드. 규칙은 Art/README.md
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class ModelPalette
    {
        // 1장 (서부 마을) — 지금 Codex 명세(Art/tools/specs/*_spec.md)에서 쓰던 색을 그대로 옮김
        public static readonly Dictionary<string, Color> Ch1 = new()
        {
            // 나무 · 금속
            { "wood", new Color(0.55f, 0.33f, 0.19f) },
            { "wood_light", new Color(0.72f, 0.48f, 0.28f) },
            { "wood_dark", new Color(0.33f, 0.19f, 0.12f) },
            { "iron", new Color(0.24f, 0.22f, 0.25f) },
            { "brass", new Color(0.85f, 0.62f, 0.25f) },
            { "gold", new Color(1f, 0.78f, 0.22f) },
            { "gold_drip", new Color(1f, 0.78f, 0.22f) },
            // 천 · 칠
            { "canvas", new Color(0.85f, 0.76f, 0.6f) },
            { "cloth_red", new Color(0.7f, 0.15f, 0.13f) },
            { "cloth_teal", new Color(0.25f, 0.5f, 0.5f) },
            { "paint_teal", new Color(0.24f, 0.48f, 0.47f) },
            { "paint_red", new Color(0.62f, 0.2f, 0.15f) },
            { "paint_mustard", new Color(0.82f, 0.6f, 0.2f) },
            { "paint_cream", new Color(0.88f, 0.8f, 0.62f) },
            { "paint_blue", new Color(0.3f, 0.38f, 0.55f) },
            // 지붕 · 벽 · 돌
            { "roof_tin", new Color(0.46f, 0.48f, 0.52f) },
            { "roof_shingle", new Color(0.36f, 0.25f, 0.2f) },
            { "roof_red", new Color(0.5f, 0.2f, 0.14f) },
            { "brick", new Color(0.58f, 0.28f, 0.2f) },
            { "stone", new Color(0.55f, 0.5f, 0.46f) },
            { "rock", new Color(0.64f, 0.33f, 0.22f) },
            { "rock_light", new Color(0.8f, 0.52f, 0.34f) },
            { "rock_dark", new Color(0.45f, 0.22f, 0.15f) },
            { "ore", new Color(0.45f, 0.4f, 0.42f) },
            { "sand", new Color(0.86f, 0.66f, 0.42f) },
            { "hay", new Color(0.9f, 0.74f, 0.35f) },
            { "bone", new Color(0.92f, 0.88f, 0.78f) },
            // 창 · 물 · 빛
            { "window", new Color(0.12f, 0.13f, 0.2f) },
            { "window_lit", new Color(0.98f, 0.78f, 0.4f) },
            { "water", new Color(0.3f, 0.55f, 0.65f) },
            { "spark", new Color(1f, 0.55f, 0.15f) },
            // 하트
            { "heart_red", new Color(0.92f, 0.18f, 0.22f) },
            { "heart_light", new Color(1f, 0.62f, 0.62f) },
            { "cream", new Color(1f, 0.95f, 0.85f) },
        };

        // 2장 (멈춘 기차역, 2026-11-17 사용자: 맵 = 따뜻하게 · 몬스터 = 차갑게). 몬스터가 차가운 보라 · 청회색이므로 맵엔 보라 · 파랑을 쓰지 않음
        public static readonly Dictionary<string, Color> Ch2 = new()
        {
            { "wood", new Color(0.55f, 0.36f, 0.22f) },
            { "wood_dark", new Color(0.32f, 0.2f, 0.13f) },
            { "wood_light", new Color(0.74f, 0.52f, 0.32f) },
            { "iron", new Color(0.32f, 0.28f, 0.26f) },
            { "iron_dark", new Color(0.18f, 0.16f, 0.15f) },
            { "rust", new Color(0.64f, 0.31f, 0.15f) },
            { "brass", new Color(0.88f, 0.65f, 0.28f) },
            { "rail_steel", new Color(0.52f, 0.48f, 0.44f) },
            { "brick", new Color(0.62f, 0.31f, 0.2f) },
            { "brick_dark", new Color(0.43f, 0.21f, 0.14f) },
            { "stone", new Color(0.66f, 0.59f, 0.5f) },
            { "stone_dark", new Color(0.45f, 0.39f, 0.33f) },
            { "roof_tin", new Color(0.52f, 0.46f, 0.4f) },
            { "soot", new Color(0.15f, 0.13f, 0.12f) },
            { "coal", new Color(0.11f, 0.1f, 0.1f) },
            { "window", new Color(0.18f, 0.14f, 0.12f) },
            { "window_lit", new Color(1f, 0.74f, 0.36f) },
            { "train_glow", new Color(1f, 0.55f, 0.12f) },          // 지나가는 열차 헤드라이트 · 배장기 (위협 신호 주황)
            { "glass_lamp", new Color(1f, 0.83f, 0.46f) },
            { "canvas", new Color(0.84f, 0.74f, 0.56f) },
            { "leather", new Color(0.6f, 0.33f, 0.18f) },
            { "leather_dark", new Color(0.37f, 0.2f, 0.12f) },
            { "cloth_red", new Color(0.74f, 0.19f, 0.14f) },
            { "paint_cream", new Color(0.92f, 0.84f, 0.66f) },
            { "paint_green", new Color(0.36f, 0.46f, 0.28f) },
            { "clock_face", new Color(0.97f, 0.91f, 0.77f) },
            { "steam", new Color(0.96f, 0.94f, 0.9f) },
            { "signal_red", new Color(0.96f, 0.2f, 0.14f) },
            { "signal_amber", new Color(1f, 0.7f, 0.15f) },
            { "signal_green", new Color(0.32f, 0.85f, 0.4f) },
            // 굳은 승객 석상 (금 선 crack은 빌더가 빛나는 흰 재질로 바꿔 끼움)
            { "statue", new Color(0.74f, 0.72f, 0.68f) },
            { "statue_dark", new Color(0.55f, 0.53f, 0.5f) },
            { "statue_base", new Color(0.47f, 0.45f, 0.42f) },
            { "crack", Color.white },
        };

        // 3장 (신기루 사막 = 거울 세계, 2026-11-18). 기획 색은 청록 · 연보라 · 분홍 노을 · 흰 빛 + 금이지만
        // 몬스터가 차가운 보라 · 청회색이라 맵 고체(돌 · 대리석 · 금)는 장미 사암 · 크림 · 금으로 따뜻하게, 청록은 물에만, 흰 빛은 수정 · 거울에만
        public static readonly Dictionary<string, Color> Ch3 = new()
        {
            { "sandstone", new Color(0.97f, 0.76f, 0.76f) },
            { "sandstone_light", new Color(1f, 0.88f, 0.86f) },
            { "sandstone_dark", new Color(0.86f, 0.6f, 0.64f) },
            { "rock", new Color(0.92f, 0.7f, 0.72f) },
            { "rock_dark", new Color(0.72f, 0.5f, 0.56f) },
            { "sand", new Color(1f, 0.88f, 0.78f) },
            { "marble", new Color(0.97f, 0.93f, 0.88f) },
            { "marble_dark", new Color(0.8f, 0.73f, 0.68f) },
            { "gold", new Color(1f, 0.8f, 0.32f) },
            { "gold_dark", new Color(0.78f, 0.55f, 0.22f) },
            { "mirror", new Color(0.88f, 0.96f, 0.97f) },         // 거울면: 빌더가 반짝이는 거울 재질로 바꿔 끼움 (MirrorMats)
            { "mirror_back", new Color(0.78f, 0.56f, 0.5f) },
            { "crystal", new Color(0.93f, 0.96f, 1f) },
            { "crystal_core", new Color(1f, 0.86f, 0.95f) },       // 빛가루 분홍 (Enemy.Fx3 빛가루와 같은 계열)
            { "stardust", new Color(1f, 0.93f, 0.7f) },
            { "water", new Color(0.42f, 0.8f, 0.78f) },
            { "water_foam", new Color(0.92f, 1f, 0.98f) },
            { "cloud", new Color(1f, 0.95f, 0.92f) },
            { "cloud_shade", new Color(0.94f, 0.8f, 0.8f) },
            { "cloth_rose", new Color(0.82f, 0.36f, 0.42f) },
            { "wood", new Color(0.55f, 0.36f, 0.22f) },
            { "roof_tile", new Color(0.72f, 0.36f, 0.3f) },
            { "window_lit", new Color(1f, 0.78f, 0.42f) },
            // 2차 '화려하게' (2026-11-18 사용자: 단색 객체가 너무 많음) — 상감 · 보석 · 장식
            { "glaze_teal", new Color(0.2f, 0.66f, 0.64f) },
            { "coral", new Color(0.94f, 0.47f, 0.4f) },
            { "ivory", new Color(1f, 0.96f, 0.86f) },
            { "bronze", new Color(0.66f, 0.42f, 0.22f) },
            { "rose_gold", new Color(0.96f, 0.66f, 0.5f) },
            { "gem_rose", new Color(1f, 0.35f, 0.55f) },
            { "gem_amber", new Color(1f, 0.62f, 0.12f) },
            { "crystal_rose", new Color(1f, 0.78f, 0.86f) },
            { "tile_cream", new Color(0.98f, 0.9f, 0.76f) },
            // 3장 블렌더 보스 전용 (몬스터 공통 차가운 색 + 진홍 포인트)
            { "boss_lavender", new Color(0.6f, 0.56f, 0.82f) },
            { "boss_slate", new Color(0.3f, 0.32f, 0.46f) },
            { "boss_crimson", new Color(0.8f, 0.12f, 0.22f) },
            { "boss_cream", new Color(0.95f, 0.92f, 0.9f) },
        };

        // 4장 (시계 장치 속, 2026-11-19 사용자: 느와르 — 강한 흑백 대비, 색은 붉은 포인트만, 적 공격 궤적은 빨간 계열).
        // 금속 · 돌 · 나무 전부 검정 ~ 흰색 사이 무채색으로 명암을 크게 벌리고, 놋쇠 · 금도 은빛 흰색으로. 붉은색은 칠 · 보석 · 리본만.
        // 상인(오르골)만 따뜻한 색 조금 (velvet · lamp_warm)
        public static readonly Dictionary<string, Color> Ch4 = new()
        {
            { "steel", new Color(0.78f, 0.78f, 0.8f) },
            { "steel_dark", new Color(0.32f, 0.32f, 0.34f) },
            { "iron", new Color(0.2f, 0.2f, 0.21f) },
            { "iron_dark", new Color(0.08f, 0.08f, 0.09f) },
            { "silver", new Color(0.95f, 0.95f, 0.96f) },
            { "brass", new Color(0.9f, 0.89f, 0.86f) },
            { "brass_dark", new Color(0.42f, 0.41f, 0.4f) },
            { "gold", new Color(1f, 0.99f, 0.95f) },
            { "copper", new Color(0.6f, 0.59f, 0.58f) },
            { "navy_paint", new Color(0.13f, 0.13f, 0.15f) },
            { "navy_dark", new Color(0.05f, 0.05f, 0.06f) },
            { "red_lacquer", new Color(0.78f, 0.06f, 0.08f) },
            { "red_dark", new Color(0.42f, 0.03f, 0.05f) },
            { "gem_red", new Color(1f, 0.1f, 0.12f) },
            { "wood", new Color(0.5f, 0.49f, 0.48f) },
            { "wood_dark", new Color(0.17f, 0.17f, 0.17f) },
            { "stone", new Color(0.66f, 0.66f, 0.66f) },
            { "stone_dark", new Color(0.26f, 0.26f, 0.27f) },
            { "dial_face", new Color(0.97f, 0.97f, 0.95f) },
            { "dial_mark", new Color(0.04f, 0.04f, 0.05f) },
            // 보스 경기장 문자판 (2026-11-26 사용자: 바닥 시계 디테일 살리기 — 전엔 배색표에 없어 선 · 숫자가 연회색 0.735로 흐렸음)
            { "dial_line", new Color(0.22f, 0.22f, 0.24f) },
            { "dial_number", new Color(0.1f, 0.1f, 0.11f) },
            { "dial_black", new Color(0.05f, 0.05f, 0.06f) },
            { "dial_silver", new Color(0.8f, 0.8f, 0.82f) },
            { "glass", new Color(0.86f, 0.88f, 0.9f) },
            { "starlight", Color.white },
            { "night_sky", new Color(0.02f, 0.02f, 0.03f) },
            { "porcelain", new Color(0.98f, 0.97f, 0.96f) },
            { "ribbon_red", new Color(0.86f, 0.08f, 0.1f) },
            { "velvet", new Color(0.55f, 0.06f, 0.1f) },
            { "lamp_warm", new Color(1f, 0.84f, 0.6f) },
        };

        // 5장 (무너지는 시간의 끝) v2 = 그리니치 천문대 (2026-11-20 사용자: 바닥 · 벽 재구성, 천문대 컨셉, 시간은 유지 — 표준시): 남색 벽돌 · 흰 돌 · 놋쇠 계기 · 청록 시간 균열 · 붉은 타임볼.
        // 재질 이름은 그대로 두고 뜻만 바꿈 (crack_gold = 청록 균열 빛). 명세 Art/tools/specs/_ch5_observatory_common.md. 예전 보라 · 금 값은 git 기록
        public static readonly Dictionary<string, Color> Ch5 = new()
        {
            { "void_stone", new Color(0.2f, 0.27f, 0.45f) },            // 남색 벽돌
            { "void_stone_dark", new Color(0.11f, 0.15f, 0.28f) },
            { "marble", new Color(0.86f, 0.9f, 0.96f) },                // 흰 돌 (포틀랜드석)
            { "marble_dark", new Color(0.56f, 0.64f, 0.76f) },
            { "violet", new Color(0.36f, 0.46f, 0.62f) },               // 슬레이트 블루 판
            { "violet_dark", new Color(0.2f, 0.26f, 0.4f) },
            { "indigo", new Color(0.1f, 0.14f, 0.3f) },
            { "night_sky", new Color(0.03f, 0.05f, 0.13f) },
            { "starlight", Color.white },
            { "crack_gold", new Color(0.45f, 0.92f, 1f) },             // 청록 시간 균열 (빌더가 빛나는 단색으로 바꿈, Ch5GlowMats)
            { "gold", new Color(0.95f, 0.76f, 0.38f) },
            { "gold_dark", new Color(0.7f, 0.52f, 0.26f) },
            { "mirror", new Color(0.78f, 0.9f, 1f) },                  // 빛나는 단색 (유리 · 렌즈)
            { "mirror_back", new Color(0.4f, 0.5f, 0.66f) },
            { "crystal", new Color(0.6f, 0.82f, 1f) },
            { "magenta", new Color(1f, 0.22f, 0.72f) },                // 빛나는 단색 (적 포인트)
            { "wood", new Color(0.32f, 0.25f, 0.22f) },                 // 짙게 물들인 나무 (망원경 통 · 시계 장)
            { "wood_dark", new Color(0.2f, 0.15f, 0.14f) },
            { "iron", new Color(0.3f, 0.36f, 0.48f) },                  // 청강
            { "iron_dark", new Color(0.15f, 0.18f, 0.26f) },
            { "steel", new Color(0.66f, 0.72f, 0.82f) },
            { "brass", new Color(0.9f, 0.72f, 0.4f) },
            { "dial_face", new Color(0.95f, 0.93f, 0.86f) },
            { "dial_mark", new Color(0.12f, 0.16f, 0.3f) },
            { "sand", new Color(0.8f, 0.78f, 0.72f) },
            { "red_lacquer", new Color(0.85f, 0.16f, 0.14f) },          // 붉은 타임볼
            { "cloth", new Color(0.25f, 0.35f, 0.6f) },
            { "lamp_warm", new Color(1f, 0.8f, 0.5f) },
            { "porcelain", new Color(0.95f, 0.96f, 1f) },
            { "velvet", new Color(0.2f, 0.22f, 0.45f) },
        };

        // 4장 상인 오르골 인형 (블렌더 + Codex, 원화 Art/tripo_input/merchant/ch4_musicdoll.png): 느와르 장에서 혼자 따뜻한 색 — 상아 자기 · 금 · 포도주 벨벳 · 짙은 나무
        public static readonly Dictionary<string, Color> Ch4Merchant = new()
        {
            { "porcelain", new Color(0.97f, 0.91f, 0.8f) },
            { "dial_face", new Color(0.96f, 0.9f, 0.78f) },
            { "dial_mark", new Color(0.3f, 0.18f, 0.12f) },
            { "gold", new Color(0.95f, 0.74f, 0.3f) },
            { "brass", new Color(0.86f, 0.64f, 0.3f) },
            { "brass_dark", new Color(0.55f, 0.38f, 0.18f) },
            { "copper", new Color(0.78f, 0.45f, 0.26f) },
            { "silver", new Color(0.88f, 0.86f, 0.8f) },
            { "velvet", new Color(0.45f, 0.1f, 0.18f) },
            { "red_dark", new Color(0.4f, 0.08f, 0.14f) },
            { "red_lacquer", new Color(0.62f, 0.12f, 0.16f) },
            { "ribbon_red", new Color(0.55f, 0.12f, 0.2f) },
            { "wood", new Color(0.55f, 0.36f, 0.22f) },
            { "wood_dark", new Color(0.36f, 0.22f, 0.14f) },
            { "glass", new Color(0.86f, 0.9f, 0.92f) },
            { "lamp_warm", new Color(1f, 0.62f, 0.2f) },
        };

        // 메인 마을 (2026-11-18 사용자: 1장 키트 재활용 X, '소멸의 여로' 느낌 — 새벽에 사라져 가는 서부 마을, 허공에 뜬 큰 섬)
        // 물체는 따뜻하게(바랜 나무 · 놋쇠 · 금 · 크림 · 녹 · 새벽 장밋빛 칠), 차가운 색은 하늘 · 허공에만. 새벽 남색 조명 아래서도 칙칙하지 않게 밝게
        // 스토리 맵 '어린 날 시계탑 광장' (blender_kit_story_plaza.py, 2026-11-25): 노을 — 따뜻한 살구 · 벽돌 · 금, 그림자는 짙은 보라
        public static readonly Dictionary<string, Color> StoryPlaza = new()
        {
            { "wood", new Color(0.62f, 0.36f, 0.2f) },
            { "wood_dark", new Color(0.34f, 0.18f, 0.12f) },
            { "wood_light", new Color(0.9f, 0.66f, 0.4f) },
            { "plank", new Color(0.76f, 0.5f, 0.3f) },
            { "stone", new Color(0.96f, 0.76f, 0.6f) },
            { "stone_dark", new Color(0.62f, 0.44f, 0.42f) },
            { "cobble", new Color(0.9f, 0.64f, 0.5f) },
            { "cobble_dark", new Color(0.7f, 0.44f, 0.4f) },
            { "brick", new Color(0.82f, 0.4f, 0.3f) },
            { "adobe", new Color(1f, 0.74f, 0.54f) },
            { "adobe_light", new Color(1f, 0.88f, 0.72f) },
            { "roof_shingle", new Color(0.56f, 0.26f, 0.34f) },
            { "brass", new Color(0.9f, 0.62f, 0.28f) },
            { "gold", new Color(1f, 0.8f, 0.3f) },
            { "iron", new Color(0.3f, 0.22f, 0.28f) },
            { "clock_face", new Color(1f, 0.95f, 0.82f) },
            { "window_lit", new Color(1f, 0.8f, 0.42f) },
            { "lantern_glow", new Color(1f, 0.74f, 0.32f) },
            { "cloth_red", new Color(0.88f, 0.32f, 0.28f) },
            { "cloth_cream", new Color(1f, 0.94f, 0.8f) },
            { "paint_teal", new Color(0.3f, 0.62f, 0.6f) },
            { "shadow", new Color(0.16f, 0.08f, 0.2f) },
            { "shadow_rim", new Color(1f, 0.6f, 0.4f) },
            { "hub", new Color(1f, 0.8f, 0.3f) },
            { "glass", new Color(0.8f, 0.9f, 0.92f) },
            { "rope", new Color(0.7f, 0.52f, 0.36f) },
            { "rock", new Color(0.56f, 0.4f, 0.42f) },
            { "earth", new Color(0.78f, 0.52f, 0.4f) },
            { "grass_dry", new Color(0.88f, 0.72f, 0.46f) },
        };

        // 스토리 맵 2 '배웅하던 승강장' (2026-11-25): 비 오는 밤 — 푸른 돌 · 벽돌 + 호박색 가스등 · 금 장식. 밤이라도 바닥 · 벽은 밝게(가독성)
        public static readonly Dictionary<string, Color> StoryPlatform = new()
        {
            { "stone", new Color(0.7f, 0.7f, 0.76f) },
            { "stone_dark", new Color(0.46f, 0.46f, 0.56f) },
            { "tile_light", new Color(0.8f, 0.79f, 0.82f) },
            { "tile_dark", new Color(0.58f, 0.58f, 0.68f) },
            { "cobble", new Color(0.64f, 0.63f, 0.7f) },
            { "cobble_dark", new Color(0.48f, 0.47f, 0.56f) },
            { "brick", new Color(0.74f, 0.44f, 0.44f) },
            { "brick_dark", new Color(0.5f, 0.3f, 0.36f) },
            { "wood", new Color(0.62f, 0.42f, 0.32f) },
            { "wood_dark", new Color(0.36f, 0.24f, 0.22f) },
            { "iron", new Color(0.3f, 0.3f, 0.4f) },
            { "iron_dark", new Color(0.18f, 0.19f, 0.28f) },
            { "brass", new Color(0.92f, 0.68f, 0.34f) },
            { "gold", new Color(1f, 0.8f, 0.34f) },
            { "rail_steel", new Color(0.74f, 0.76f, 0.82f) },
            { "ballast", new Color(0.4f, 0.39f, 0.44f) },
            { "clock_face", new Color(1f, 0.95f, 0.82f) },
            { "window_lit", new Color(1f, 0.8f, 0.45f) },
            { "lantern_glow", new Color(1f, 0.74f, 0.36f) },
            { "glass", new Color(0.5f, 0.58f, 0.7f) },
            { "puddle", new Color(0.5f, 0.56f, 0.7f) },
            { "paint_cream", new Color(0.96f, 0.9f, 0.76f) },
            { "paint_green", new Color(0.3f, 0.54f, 0.5f) },
            { "poster", new Color(0.92f, 0.76f, 0.54f) },
            { "scarf", new Color(0.9f, 0.28f, 0.28f) },
            { "leather", new Color(0.55f, 0.34f, 0.24f) },
            { "shadow", new Color(0.08f, 0.05f, 0.12f) },
            { "shadow_rim", new Color(1f, 0.72f, 0.42f) },
        };

        public static readonly Dictionary<string, Color> Village = new()
        {
            { "wood", new Color(0.66f, 0.42f, 0.26f) },
            { "wood_dark", new Color(0.4f, 0.24f, 0.16f) },
            { "wood_light", new Color(0.86f, 0.64f, 0.42f) },
            { "wood_weathered", new Color(0.72f, 0.6f, 0.5f) },
            { "plank", new Color(0.78f, 0.55f, 0.34f) },
            { "roof_shingle", new Color(0.66f, 0.34f, 0.3f) },
            { "roof_tin", new Color(0.7f, 0.62f, 0.56f) },
            { "adobe", new Color(0.93f, 0.7f, 0.54f) },
            { "adobe_light", new Color(1f, 0.86f, 0.72f) },
            // 땅 · 바위 · 돌 = 서부 흙빛 (2026-11-18 푸른 바닥을 써 봤다가 서부 느낌이 빠져서 되돌림)
            { "stone", new Color(0.8f, 0.7f, 0.62f) },
            { "stone_dark", new Color(0.58f, 0.48f, 0.44f) },
            { "rock", new Color(0.7f, 0.56f, 0.48f) },
            { "rock_dark", new Color(0.46f, 0.34f, 0.32f) },
            { "earth", new Color(0.56f, 0.38f, 0.3f) },
            { "root", new Color(0.44f, 0.3f, 0.22f) },
            { "sand", new Color(0.95f, 0.8f, 0.6f) },
            { "sand_glow", new Color(1f, 0.86f, 0.52f) },          // 흩어지는 모래 빛 (빌더가 발광으로)
            { "brass", new Color(0.86f, 0.64f, 0.3f) },
            { "gold", new Color(1f, 0.8f, 0.32f) },
            { "gold_drip", new Color(1f, 0.7f, 0.22f) },
            { "iron", new Color(0.32f, 0.29f, 0.3f) },
            { "rust", new Color(0.7f, 0.36f, 0.2f) },
            { "canvas", new Color(0.94f, 0.88f, 0.74f) },
            { "cloth_red", new Color(0.78f, 0.24f, 0.22f) },
            { "cloth_cream", new Color(1f, 0.94f, 0.82f) },
            { "paint_rose", new Color(0.88f, 0.5f, 0.5f) },
            { "paint_sage", new Color(0.62f, 0.7f, 0.5f) },
            { "glass", new Color(0.86f, 0.9f, 0.84f) },
            { "window_lit", new Color(1f, 0.8f, 0.42f) },
            { "lantern_glow", new Color(1f, 0.74f, 0.32f) },
            { "clock_face", new Color(1f, 0.95f, 0.84f) },
            { "cactus", new Color(0.46f, 0.62f, 0.38f) },
            { "cactus_dark", new Color(0.32f, 0.46f, 0.28f) },
            { "stardust", new Color(1f, 0.95f, 0.78f) },
            // 부서진 시간 · 몽환 오브젝트 (blender_kit_village_dream.py)
            { "crystal_blue", new Color(0.62f, 0.8f, 1f) },
            { "crystal_core", new Color(0.86f, 0.96f, 1f) },
            { "rift_glow", new Color(0.8f, 0.96f, 1f) },
            { "moon", new Color(0.94f, 0.94f, 1f) },
            { "glass_blue", new Color(0.6f, 0.74f, 0.96f) },
            // 신비한 나무 · 오브젝트 (blender_kit_village_mystic.py)
            { "bark_pale", new Color(0.82f, 0.82f, 0.9f) },
            { "bark_dark", new Color(0.4f, 0.36f, 0.5f) },
            { "leaf_crystal", new Color(0.6f, 0.82f, 0.96f) },
            { "petal_glow", new Color(1f, 0.8f, 0.94f) },
        };

        /// <summary>지금 빌드하는 챕터의 배색표 (빌더가 챕터마다 바꿔 끼움)</summary>
        public static Dictionary<string, Color> Active = Ch1;

        /// <summary>회색 재질이면 이름으로 배색표 색을 돌려줌 (블렌더 중복 접미사 .001 등은 떼고 찾음)</summary>
        public static bool TryResolve(Material src, Color flat, out Color color)
        {
            color = flat;
            if (!src) return false;
            Color.RGBToHSV(flat, out _, out float s, out _);
            if (s > 0.06f) return false;                                   // 색이 이미 칠해진 모델 → 그대로
            string key = Regex.Replace(src.name.ToLowerInvariant(), @"(\.\d+|\s*\(instance\))$", "");
            if (Active.TryGetValue(key, out color)) return true;
            Debug.LogWarning($"[ModelPalette] 배색표에 없는 재질 이름: {src.name} — 회색 그대로 (ModelPalette 배색표에 추가)");
            color = flat;
            return false;
        }
    }
}
