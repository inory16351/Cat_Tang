// 엔딩 스토리 맵 '자정의 시계판' — 녹아내리는 섬 아랫면 키트 (2026-11-26 사용자: 맵 아랫부분도 블렌더로 따로 깎아서 녹아내리는 느낌)
//   원본 Art/tools/blender_kit_story_dial_under.py (Codex, 명세 Art/tools/specs/kit_story_dial_under_spec.md · 섬 윤곽 story_dial_outlines.json)
//   → Assets/Art/Characters/StoryDial/Kit_D_Under_<Key>.fbx (+ _tips.json = 매달린 방울 끝, 유니티 섬 로컬 좌표)
//   → 메뉴 CatTang/엔딩 시계판 아랫면 키트 만들기 → Resources/Kit/StoryDial/Under_<Key>.prefab (원점 = 섬 중심 윗면 그대로, 마을 배색)
//   시계판 조립(StoryDialBuilder)이 섬마다 붙이고 방울 끝마다 떨어지는 파티클을 냄
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryDialSrc = "Assets/Art/Characters/StoryDial/";
        public const string StoryDialKitDir = "Assets/Resources/Kit/StoryDial/";
        public static readonly string[] DialUnderKeys = { "Center", "Gun", "Ammo", "Wardrobe", "Cuckoo", "Gate" };

        [MenuItem("CatTang/엔딩 시계판 아랫면 키트 만들기")]
        public static void BuildStoryDialUnderKit()
        {
            Directory.CreateDirectory(StoryDialKitDir);
            SrcDir = StoryDialSrc;
            ModelPalette.Active = ModelPalette.Village;
            foreach (var k in DialUnderKeys) KeepOrigin.Add("Kit_D_Under_" + k);
            try
            {
                int made = 0;
                foreach (var k in DialUnderKeys)
                {
                    string n = "Kit_D_Under_" + k;
                    if (!Build(n, NativeWidth(StoryDialSrc + n + ".fbx"), 180f, 'w', StoryDialKitDir, "Under_" + k)) continue;
                    var path = StoryDialKitDir + "Under_" + k + ".prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var mesh = root.transform.Find("Mesh");
                        if (mesh) mesh.localPosition = Vector3.zero;                                   // 원점 = 섬 중심 윗면
                        DissolveMats(root);                                                                // 아래로 갈수록 모래 무늬로 디졸브 (빛나는 모래 재질도 같이 사라지게 툰 그대로)
                        foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    made++;
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryDial] 아랫면 키트 {made}/{DialUnderKeys.Length}개");
            }
            finally
            {
                foreach (var k in DialUnderKeys) KeepOrigin.Remove("Kit_D_Under_" + k);
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        // 디졸브 높이 (아랫면 메시 오브젝트 공간 Y, 위 -2.5 → 아래 -11.5). StoryDialBuilder.DissolveDust 파티클 띠도 같은 값
        public const float DialDissolveTop = -2.5f, DialDissolveBottom = -11.5f;

        /// <summary>아랫면 툰 재질을 CatTang/ToonDissolve로 (같은 속성 이름이라 색 · 명암은 그대로)</summary>
        static void DissolveMats(GameObject root)
        {
            var sh = Shader.Find("CatTang/ToonDissolve");
            if (!sh) { Debug.LogWarning("[StoryDial] CatTang/ToonDissolve 셰이더 없음"); return; }
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || (m.shader != sh && m.shader.name != "CatTang/Toon")) continue;
                    m.shader = sh;
                    m.SetFloat("_DissolveTop", DialDissolveTop); m.SetFloat("_DissolveBottom", DialDissolveBottom);
                    m.SetFloat("_DissolveScale", 1.3f); m.SetFloat("_DissolveEdge", 0.12f);
                    m.SetColor("_DissolveEdgeColor", new Color(1f, 0.82f, 0.5f, 1f));
                    m.SetFloat("_SandGrain", 0.22f); m.SetFloat("_SandScale", 26f);
                    // 바위 · 흙 띠도 모래 계열로 모음 (2026-11-26 사용자: 모래 질감이 아니라 이질적) — 밝기 차이는 남기고 색만 모래 쪽으로
                    if (m.HasProperty("_BaseColor"))
                    {
                        var c = m.GetColor("_BaseColor");
                        float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                        var sand = new Color(0.92f, 0.72f, 0.48f) * Mathf.Lerp(0.55f, 1.08f, Mathf.InverseLerp(0.15f, 0.8f, lum));
                        bool metal = c.r > 0.8f && c.g > 0.55f && c.b < 0.4f && lum > 0.6f;   // 박힌 시계 금 · 놋쇠는 그대로
                        if (!metal) m.SetColor("_BaseColor", Color.Lerp(c, new Color(sand.r, sand.g, sand.b, c.a), 0.6f));
                    }
                    EditorUtility.SetDirty(m);
                }
        }

        static float NativeWidth(string fbx)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (!src) return 1f;
            var inst = (GameObject)Object.Instantiate(src);
            try
            {
                inst.transform.rotation = Quaternion.Euler(0, 180f, 0) * inst.transform.rotation;
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) return 1f;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                return b.size.x;
            }
            finally { Object.DestroyImmediate(inst); }
        }
    }
}
