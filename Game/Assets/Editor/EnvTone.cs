// 배경 톤 낮추기 (2026-11-17 사용자: 맵 · 몬스터 · 고양이 배색이 비슷해 잘 안 보임 → 배경 톤 낮추기 선택)
// 맵 키트(Resources/Kit/Ch{장}/*.prefab) · 바닥 재질의 툰 재질만 채도 · 밝기를 낮춰 배경을 뒤로 물림.
// 고양이 · 적 · 하트 · 모래 폭탄 · 화약통(쏘는 대상)은 그대로라 상대적으로 튀어 보임.
// 툰 셰이더의 _Saturation · _Brightness를 절대값으로 넣으므로 여러 번 돌려도 같음. 빌더(EnemyModelBuilder)도 키트를 만들 때 이것을 부름.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class EnvTone
    {
        public const float Saturation = 1f, Brightness = 1f;   // 2026-11-17 사용자: 적을 2안으로 바꾸고 맵은 원래 채도로 되돌림 (예전 값 0.55)
        const string KitRoot = "Assets/Resources/Kit";
        // 배경이 아닌 키트 (눈에 띄어야 하는 것)
        static readonly HashSet<string> Keep = new() { "Heart", "SandBomb", "Keg" };

        [MenuItem("CatTang/배경 톤 낮추기 (맵 키트 채도 · 밝기)")]
        public static void Apply()
        {
            var done = new HashSet<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { KitRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!go || Keep.Contains(go.name)) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials) Tone(m, done);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { KitRoot }))
                Tone(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)), done);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnvTone] 배경 재질 {done.Count}개 — 채도 {Saturation} · 밝기 {Brightness}");
        }

        public static void Tone(Material m, HashSet<Material> done = null)
        {
            if (!m || !m.HasProperty("_Saturation") || (done != null && !done.Add(m))) return;
            m.SetFloat("_Saturation", Saturation);
            m.SetFloat("_Brightness", Brightness);
            EditorUtility.SetDirty(m);
        }
    }
}
