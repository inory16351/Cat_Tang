// 마을 시계바늘 수리 퀘스트 배치 (2026-11-25): Village 씬에 숨은 물건 3 · 반딧불 길 3 · 재봉사 건네기를 직접 저장
//   물건 모델 = Resources/Kit/Story/Memento_* (blender_kit_story_plaza.py). 자리는 1장 클리어 때 열린 섬(가운데 · 시간의 문 · 의상실)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class ClockQuestBuilder
    {
        static readonly (string id, string kit, Vector3 pos, Vector3[] path)[] Spots =
        {
            ("glasses", "Memento_Glasses", new Vector3(-12.5f, 0, 1.8f), new[] { new Vector3(0, 0, -10), new Vector3(-5, 0, -8), new Vector3(-9.5f, 0, -4.5f), new Vector3(-12.5f, 0, 1.8f) }),
            ("band", "Memento_Band", new Vector3(-6f, 0, -37f), new[] { new Vector3(0, 0, -10), new Vector3(0, 0, -16), new Vector3(0, 0, -22), new Vector3(-1, 0, -28), new Vector3(-3, 0, -33), new Vector3(-6, 0, -37) }),
            ("note", "Memento_Note", new Vector3(33f, 0, 26f), new[] { new Vector3(0, 0, -10), new Vector3(6, 0, 3), new Vector3(12, 0, 12), new Vector3(15, 0, 16), new Vector3(20, 0, 20), new Vector3(27, 0, 23.5f), new Vector3(33, 0, 26) }),
        };

        [MenuItem("CatTang/마을 시계바늘 퀘스트 배치")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Village.unity");
            var old = GameObject.Find("ClockQuest"); if (old) Object.DestroyImmediate(old);
            var root = new GameObject("ClockQuest");
            root.AddComponent<ClockQuest>();
            foreach (var s in Spots)
            {
                var m = new GameObject("Memento_" + s.id);
                m.transform.SetParent(root.transform, false);
                m.transform.position = s.pos;
                var pick = m.AddComponent<MementoPickup>(); pick.id = s.id;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyModelBuilder.StoryKitDir + s.kit + ".prefab");
                if (prefab)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, m.transform);
                    model.name = "Model";
                    model.transform.localPosition = new Vector3(0, 0.9f, 0);
                    model.transform.localScale *= 1.25f;
                }
                // 바닥의 따뜻한 빛 고리 (반딧불이 모이는 자리)
                var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(glow.GetComponent<Collider>());
                glow.name = "Glow";
                glow.transform.SetParent(m.transform, false);
                glow.transform.localPosition = new Vector3(0, 0.04f, 0);
                glow.transform.localRotation = Quaternion.Euler(90, 0, 0);
                glow.transform.localScale = Vector3.one * 2.4f;
                var gm = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Characters/StoryPlaza/Memento_Glow.mat");
                if (!gm)
                {
                    gm = new Material(Shader.Find("Sprites/Default"));
                    var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                        float a = Mathf.Clamp01(1 - d); tex.SetPixel(x, y, new Color(1, 1, 1, a * a * 0.8f));
                    }
                    tex.Apply();
                    AssetDatabase.CreateAsset(tex, "Assets/Art/Characters/StoryPlaza/Memento_Glow.asset");
                    gm.mainTexture = tex;
                    gm.color = new Color(1f, 0.85f, 0.45f, 0.85f);
                    AssetDatabase.CreateAsset(gm, "Assets/Art/Characters/StoryPlaza/Memento_Glow.mat");
                }
                glow.GetComponent<MeshRenderer>().sharedMaterial = gm;
                glow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var f = new GameObject("Fireflies_" + s.id);
                f.transform.SetParent(root.transform, false);
                f.AddComponent<FireflyTrail>().id = s.id;
                for (int i = 0; i < s.path.Length; i++)
                {
                    var w = new GameObject("Waypoint_" + i).transform;
                    w.SetParent(f.transform, false);
                    w.position = s.path[i];
                }
            }
            var spot = GameObject.Find("NpcSpot_Wardrobe");
            if (spot)
            {
                var oldD = spot.GetComponentInChildren<TailorDelivery>(); if (oldD) Object.DestroyImmediate(oldD.gameObject);
                var d = new GameObject("TailorDelivery");
                d.transform.SetParent(spot.transform, false);
                d.AddComponent<TailorDelivery>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ClockQuest] 마을 퀘스트 배치 저장");
        }
    }
}
