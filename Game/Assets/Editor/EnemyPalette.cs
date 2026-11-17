// 적 배색 비교 (2026-11-17 사용자: 적 · 고양이 · 맵이 같은 갈색 계열 → 1안 · 2안 입혀 보고 비교)
// 툰 셰이더 색 범위 치환(_Recolor*)으로 적 몸의 적갈색 · 갈색만 돌림. 금빛 금속 · 선명한 진홍 포인트는 유지.
//   1안 종류별: 무법자 청록 · 깡패 진보라 · 폭탄병 겨자 · 강화형 = 같은 색을 더 진하게 · 중간 보스 검은 코트 · 보스 남색
//   2안 한 계열: 모든 적을 차가운 보라 · 청회색 (강화형 · 보스는 조금 더 진하게)
//   원래: 치환 끔
// 확정(2026-11-17 사용자): 2안. 빌더가 적 재질을 만들 때 Current를 넣음
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class EnemyPalette
    {
        const string Dir = "Assets/Art/Characters/Ch1/";

        // 재질 · 목표 색상(도) · 채도 × · 명도 ×
        static readonly (string mat, float hue, float sat, float val)[] Plan1 =
        {
            ("Ch1_Outlaw_Toon", 185, 0.85f, 1.15f),
            ("Ch1_Outlaw_Plus_Toon", 190, 1.1f, 1.0f),
            ("Ch1_Brute_Toon", 285, 0.85f, 1.15f),
            ("Ch1_Brute_Plus_Toon", 290, 1.15f, 1.0f),
            ("Ch1_Bomber_Toon", 45, 1.2f, 1.35f),
            ("Ch1_Bomber_Plus_Toon", 38, 1.35f, 1.2f),
            ("Ch1_MidBoss_Toon", 230, 0.3f, 0.75f),
            ("Ch1_Boss_Toon", 245, 0.9f, 1.05f),
        };

        static readonly (string mat, float hue, float sat, float val)[] Plan2 =
        {
            ("Ch1_Outlaw_Toon", 255, 0.6f, 1.15f),
            ("Ch1_Outlaw_Plus_Toon", 265, 0.8f, 1.05f),
            ("Ch1_Brute_Toon", 255, 0.6f, 1.15f),
            ("Ch1_Brute_Plus_Toon", 265, 0.8f, 1.05f),
            ("Ch1_Bomber_Toon", 255, 0.6f, 1.15f),
            ("Ch1_Bomber_Plus_Toon", 265, 0.8f, 1.05f),
            ("Ch1_MidBoss_Toon", 250, 0.45f, 0.85f),
            ("Ch1_Boss_Toon", 270, 0.85f, 1.0f),
        };

        [MenuItem("CatTang/적 배색/1안 종류별 색")] public static void ApplyPlan1() => Apply(Plan1, 1f);
        [MenuItem("CatTang/적 배색/2안 차가운 한 계열")] public static void ApplyPlan2() => Apply(Plan2, 1f);
        [MenuItem("CatTang/적 배색/원래 색")] public static void ApplyOriginal() => Apply(Plan1, 0f);

        /// <summary>빌더용: 확정된 안(2안)을 재질 하나에</summary>
        public static void ApplyCurrent(Material m)
        {
            foreach (var e in Plan2) if (m && m.name == e.mat) Set(m, e, 1f);
        }

        static void Set(Material m, (string mat, float hue, float sat, float val) e, float amount)
        {
            if (!m.HasProperty("_RecolorAmount")) return;
            m.SetFloat("_RecolorAmount", amount);
            bool boss = e.mat.StartsWith("Ch1_Boss");                       // 보스 텍스처는 주황 갈색이 많아 범위를 조금 옮김
            m.SetFloat("_RecolorFrom", boss ? 0.01f : 0.985f);
            m.SetFloat("_RecolorWidth", 0.075f);
            m.SetFloat("_RecolorHue", e.hue / 360f);
            m.SetFloat("_RecolorSat", e.sat);
            m.SetFloat("_RecolorVal", e.val);
            EditorUtility.SetDirty(m);
        }

        static void Apply((string mat, float hue, float sat, float val)[] plan, float amount)
        {
            foreach (var e in plan)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Dir + e.mat + ".mat");
                if (m) Set(m, e, amount);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
