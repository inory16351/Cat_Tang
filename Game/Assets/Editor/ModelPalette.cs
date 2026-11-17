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
