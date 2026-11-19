// 메인 마을 · 메인 로비 씬 (2026-11-18 사용자: 녹아내린 서부 마을 유지 · 메인 마을 맵 · 메인 로비 + 저장 슬롯)
//  Cat_Tang/Build Village (Kit)  — 마을 전용 키트(Resources/Kit/Village)로 지은 새벽 허공의 섬 5개(다리로 이음, 장을 넘을 때마다 섬이 열림). 시설 4곳 + 시간의 문, NPC(NpcSpot_*)
//  Cat_Tang/Build Lobby Scene    — 메인 로비 (저장 슬롯 3개, 화면은 전부 Codex 그림 · LobbyScreen 연출)
//  빌드 목록: Lobby 0 · Village 1 · Run 2
// 프리팹은 PrefabUtility로 씬에 연결된 채 놓임 (키트를 고치면 마을에도 반영)
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang.EditorTools
{
    public static class VillageKitBuilder
    {
        const string VillagePath = "Assets/Scenes/Village.unity";
        const string LobbyPath = "Assets/Scenes/Lobby.unity";
        const string RunPath = "Assets/Scenes/Run.unity";
        const string KitDir = "Assets/Resources/Kit/Ch1/";

        [MenuItem("Cat_Tang/Build Village (Kit)")]
        public static void BuildVillageMenu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildVillage(); }

        [MenuItem("Cat_Tang/Build Lobby Scene")]
        public static void BuildLobbyMenu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { BuildLobby(); SetBuildList(); } }

        public static void SetBuildList()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LobbyPath, true), new EditorBuildSettingsScene(VillagePath, true), new EditorBuildSettingsScene(RunPath, true)
            };
            AssetDatabase.SaveAssets();
        }

        // ---------- 공통 ----------
        static GameObject K(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool collider = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KitDir + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[Village] 키트 없음 " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (collider && !go.GetComponentInChildren<Collider>())
            {
                var rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = go.transform.InverseTransformPoint(b.center);
                    var s = go.transform.InverseTransformVector(b.size);
                    bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
            }
            return go;
        }

        // ---------- 마을 NPC (시계공이 잊어버린 존재 5, EnemyModelBuilder.BuildNpcs → Resources/Npcs) ----------
        static string NpcFor(string spot) => spot switch
        {
            "NpcSpot_MemoryRoom" => "Npc_Cuckoo",
            "NpcSpot_GunShop" => "Npc_Sheriff",
            "NpcSpot_AmmoWorkshop" => "Npc_Hourglass",
            "NpcSpot_Wardrobe" => "Npc_Seamstress",
            "NpcSpot_Gatekeeper" => "Npc_Gatekeeper",
            _ => null,
        };

        /// <summary>NPC 자리에 프리팹을 세움 (프리팹이 아직 없으면 빈 자리로 둠). 이미 있던 모델은 지우고 다시</summary>
        static void PlaceNpc(Transform spot)
        {
            string n = NpcFor(spot.name);
            if (n == null) return;
            for (int i = spot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(spot.GetChild(i).gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyModelBuilder.NpcOut + n + ".prefab");
            if (!prefab) { Debug.Log("[Village] NPC 프리팹 없음 " + n + " — 빈 자리"); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, spot);
            go.name = n;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var col = spot.GetComponent<CapsuleCollider>();                        // 고양이가 뚫고 지나가지 않게
            if (!col) col = spot.gameObject.AddComponent<CapsuleCollider>();
            col.radius = 0.7f; col.height = 2.4f; col.center = new Vector3(0, 1.2f, 0);
        }

        /// <summary>지금 열린 마을 씬의 NpcSpot_*에 NPC를 다시 세움 (마을 전체를 다시 짓지 않고)</summary>
        [MenuItem("Cat_Tang/Place Village NPCs")]
        public static void PlaceNpcsInOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != VillagePath) scene = EditorSceneManager.OpenScene(VillagePath);
            int c = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.StartsWith("NpcSpot_")) { PlaceNpc(t); c++; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Village] NPC 자리 {c}곳 갱신");
        }

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            if (parent) t.SetParent(parent, false);
            return t;
        }

        // ---------- 마을 (2026-11-18 사용자: 1장 키트 재활용 X, '소멸의 여로' 느낌 · 다리로 이어진 섬 여러 개 · 장을 넘을 때마다 섬이 열림) ----------
        // 새벽 별밤 허공에 뜬 섬들, 위쪽부터 녹아 흩어지는 서부 건물. 마을 전용 키트 Resources/Kit/Village (EnemyModelBuilder.BuildVillageKit)
        //  가운데 섬(처음부터): 시계 광장 · 여관(기억의 방, 1장 클리어로 열림) · 뻐꾸기
        //  남쪽 문 섬(처음부터): 시간의 문 · 괘종시계 문지기
        //  동쪽 섬(2장): 탄약 공방 · 모래시계 대장장이 / 북동 섬(2장): 의상실 · 실타래 재봉사 / 서쪽 섬(3장): 총포상 · 빈 외투 보안관
        // 화면 위쪽(북) = 새벽이 밝아 오는 쪽
        const string VKit = EnemyModelBuilder.VillageKitDir;

        // 섬 윤곽 = 타원 × 비대칭 물결(2 · 3 · 5겹) + 불룩한 곶 두 군데 (2026-11-18 사용자: 완전한 원형 말고 비대칭 도형으로 몽환적인 느낌). seed마다 모양이 다름
        struct Isle { public string name; public Vector3 c; public float rx, rz; public int seed; public Isle(string n, Vector3 c, float rx, float rz, int seed) { name = n; this.c = c; this.rx = rx; this.rz = rz; this.seed = seed; } }
        static readonly Isle Center = new("Isle_Center", new Vector3(0, 0, 0), 17f, 15f, 11);
        static readonly Isle GateIsle = new("Isle_Gate", new Vector3(0, 0, -31), 10f, 8f, 23);
        static readonly Isle AmmoIsle = new("Isle_Ammo", new Vector3(34, 0, -3), 10f, 9f, 37);
        static readonly Isle WardIsle = new("Isle_Wardrobe", new Vector3(25, 0, 27), 9.5f, 8.5f, 41);
        static readonly Isle GunIsle = new("Isle_Gun", new Vector3(-34, 0, 2), 10f, 9f, 53);

        static float Lobe(float a, float c, float w) { float d = Mathf.DeltaAngle(a * Mathf.Rad2Deg, c * Mathf.Rad2Deg) * Mathf.Deg2Rad; return Mathf.Exp(-d * d / (w * w)); }

        /// <summary>각도 a(0 = 북, 시계 방향)에서 타원 반지름에 곱하는 배율. 시설 자리가 깎이지 않게 최소 0.88</summary>
        static float Shape(Isle s, float a)
        {
            // 2026-11-18 사용자: 섬 모양을 불규칙한 도형으로 — 큰 물결 4겹 + 불룩한 곶 3 + 안으로 파인 만 1 (시설 자리가 깎이지 않게 최소 0.78)
            var r = new System.Random(s.seed);
            float P() => (float)(r.NextDouble() * Mathf.PI * 2);
            float p1 = P(), p2 = P(), p3 = P(), p4 = P();
            float l1 = P(), l2 = l1 + Mathf.PI * (0.55f + (float)r.NextDouble() * 0.3f), l3 = l1 + Mathf.PI * (1.2f + (float)r.NextDouble() * 0.4f);
            float bay = l1 + Mathf.PI * (0.25f + (float)r.NextDouble() * 0.2f);
            float f = 1f + 0.15f * Mathf.Sin(2 * a + p1) + 0.11f * Mathf.Sin(3 * a + p2) + 0.06f * Mathf.Sin(5 * a + p3) + 0.035f * Mathf.Sin(8 * a + p4);
            f += 0.42f * Lobe(a, l1, 0.32f) + 0.3f * Lobe(a, l2, 0.26f) + 0.22f * Lobe(a, l3, 0.2f);
            f -= 0.26f * Lobe(a, bay, 0.22f);
            return Mathf.Max(0.78f, f);
        }

        /// <summary>윤곽 위 점 (섬 중심 기준, pad만큼 바깥으로)</summary>
        static Vector3 ShapePoint(Isle s, float a, float pad = 0f)
        {
            var p = new Vector3(Mathf.Sin(a) * s.rx, 0, Mathf.Cos(a) * s.rz) * Shape(s, a);
            return p + p.normalized * pad;
        }

        static GameObject V(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool collider = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VKit + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[Village] 마을 키트 없음 " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (collider && !go.GetComponentInChildren<Collider>())
            {
                // 아래 2.5m 안의 몸통만 판정 (위로 흩어져 떠 있는 조각은 막지 않음)
                var rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var bnd = rs[0].bounds;
                    foreach (var r in rs) bnd.Encapsulate(r.bounds);
                    var bc = go.AddComponent<BoxCollider>();
                    var size = go.transform.InverseTransformVector(new Vector3(bnd.size.x * 0.86f, 2.5f, bnd.size.z * 0.86f));
                    bc.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                    bc.center = go.transform.InverseTransformPoint(new Vector3(bnd.center.x, go.transform.position.y + 1.25f, bnd.center.z));
                }
            }
            return go;
        }

        /// <summary>섬 윤곽 점(섬 중심 기준, inset만큼 안쪽)과 바깥쪽 방향 yaw (프리팹 +Z가 바깥). 방향은 윤곽 접선에 수직</summary>
        static Vector3 Rim(Isle s, float ang, float inset, out float yaw)
        {
            var p = ShapePoint(s, ang, -inset);
            var t = ShapePoint(s, ang + 0.01f) - ShapePoint(s, ang - 0.01f);
            var nrm = new Vector3(t.z, 0, -t.x).normalized;
            if (Vector3.Dot(nrm, p) < 0) nrm = -nrm;
            yaw = Mathf.Atan2(nrm.x, nrm.z) * Mathf.Rad2Deg;
            return p;
        }

        /// <summary>섬 중심에서 dir 쪽 윤곽까지 거리 (타원 매개변수 각을 구해 같은 방향의 윤곽 점)</summary>
        static float RimDist(Isle s, Vector3 dir) => ShapePoint(s, Mathf.Atan2(dir.x / s.rx, dir.z / s.rz)).magnitude;

        /// <summary>타원 섬 윗면 (가운데 + 동심 고리 4겹), 흙 텍스처 8m 반복. 씬에 저장하려면 메시를 에셋으로</summary>
        /// <summary>섬 윗면 메시의 바깥 고리(마지막 seg개 정점)를 섬 로컬 (x, z)로 IsleOutline에 저장</summary>
        static void StoreOutline(Transform isle, Transform top, Mesh m)
        {
            var vs = m.vertices;
            int seg = (vs.Length - 1) / 4;
            if (seg < 3) return;
            var pts = new Vector2[seg];
            for (int i = 0; i < seg; i++)
            {
                var w = isle.InverseTransformPoint(top.TransformPoint(vs[1 + 3 * seg + i]));
                pts[i] = new Vector2(w.x, w.z);
            }
            var o = isle.GetComponent<IsleOutline>();
            if (!o) o = isle.gameObject.AddComponent<IsleOutline>();
            o.points = pts;
        }

        /// <summary>지금 열린 마을 씬의 섬에 지도용 둘레만 채움 (마을을 다시 짓지 않고)</summary>
        [MenuItem("Cat_Tang/Village Map Outlines")]
        public static void FillOutlines()
        {
            int c = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mf.name == "Top" && mf.transform.parent && mf.transform.parent.name.StartsWith("Isle_") && mf.sharedMesh)
                {
                    StoreOutline(mf.transform.parent, mf.transform, mf.sharedMesh);
                    EditorUtility.SetDirty(mf.transform.parent.gameObject);
                    c++;
                }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Village] 지도용 섬 둘레 {c}개");
        }

        static Mesh IslandMesh(string name, Isle s, float pad, int seg)
        {
            var vs = new List<Vector3> { Vector3.zero };
            var tri = new List<int>();
            for (int ring = 1; ring <= 4; ring++)
            {
                float k = ring / 4f;
                for (int i = 0; i < seg; i++)
                {
                    float a = i * Mathf.PI * 2 / seg;
                    vs.Add(ShapePoint(s, a, pad) * k);
                }
            }
            for (int i = 0; i < seg; i++) tri.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % seg });
            for (int ring = 1; ring < 4; ring++)
            {
                int o0 = 1 + (ring - 1) * seg, o1 = 1 + ring * seg;
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    tri.AddRange(new[] { o0 + i, o1 + i, o1 + j, o0 + i, o1 + j, o0 + j });
                }
            }
            var uv = new Vector2[vs.Count];
            for (int i = 0; i < vs.Count; i++) uv[i] = new Vector2(vs[i].x / 8f, vs[i].z / 8f);
            var m = new Mesh { name = name };
            m.SetVertices(vs);
            m.SetTriangles(tri, 0);
            m.uv = uv;
            m.RecalculateNormals();
            m.RecalculateBounds();
            string mp = "Assets/Art/Env/Village/" + name + ".asset";
            AssetDatabase.DeleteAsset(mp);
            AssetDatabase.CreateAsset(m, mp);
            return m;
        }

        static Material EnvMat(string name, string tex, bool unlit)
        {
            const string dir = "Assets/Art/Env/Village/";
            string p = dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + tex);
            if (!m)
            {
                var src = AssetDatabase.LoadAssetAtPath<Material>(KitDir + "Ground.mat");    // 같은 바닥 셰이더 설정만 빌림 (텍스처는 마을 것)
                m = unlit ? new Material(Shader.Find("Universal Render Pipeline/Unlit")) : src ? new Material(src) : new Material(Shader.Find("CatTang/Toon"));
                AssetDatabase.CreateAsset(m, p);
            }
            foreach (var prop in new[] { "_BaseMap", "_MainTex" }) if (m.HasProperty(prop)) m.SetTexture(prop, t);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- 건물 위로 천천히 떠오르는 조각 (2026-11-18 사용자: 건물 위 벽돌 같은 조각은 파티클로 계속 올라가게, 천천히) ----------
        // 모델에 굳어 있던 떠 있는 조각은 지우고(blender_kit_village_buildings.py), 지붕 위 상자 영역에서 벽돌 · 판자 조각이 생겨
        // 천천히 돌며 떠오르다 작아져 사라짐 + 빛나는 모래 알갱이. 색 = 마을 배색(나무 · 놋쇠 · 금 · 크림 · 장밋빛)
        static Material BitsMat(string name, Color c, string tex = null, bool additive = true)
        {
            string p = "Assets/Art/Env/Village/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (tex != null)
            {
                // 반짝이 그림(spark.png)을 쓰는 빛 입자: 투명 · 가산
                m.SetTexture("_BaseMap", tex.Length > 0 ? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Env/Village/" + tex) : null);   // "" = 그림 없이 단색 (투명만)
                // additive = false: 밝은 흙바닥 위에서도 보이게 일반 반투명 (바늘 끝 금빛 모래선)
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha)); m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>건물 가장 높은 곳들(꼬인 굴뚝 꼭대기 · 지붕 끝 · 부서진 곳)에서 조각 · 빛 모래가 기둥처럼 하늘로 솟음 (2026-11-18 사용자: 몇 군데에서 파티클로 하늘로)
        /// 메시 정점 중 위쪽 30%에서 서로 가장 멀리 떨어진 count곳을 골라 좁은 원뿔 방출</summary>
        static void RiseSpots(GameObject building, int count = 3)
        {
            if (!building) return;
            var t = building.transform;
            var pts = new List<Vector3>();
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var mf in building.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var vs = mf.sharedMesh.vertices;
                for (int i = 0; i < vs.Length; i += 3)
                {
                    var w = mf.transform.TransformPoint(vs[i]);
                    pts.Add(w); minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y);
                }
            }
            if (pts.Count == 0) return;
            float cut = minY + (maxY - minY) * 0.7f;
            var high = pts.FindAll(v => v.y >= cut);
            if (high.Count == 0) return;
            var chosen = new List<Vector3> { high[0] };
            foreach (var v in high) if (v.y > chosen[0].y) chosen[0] = v;               // 가장 높은 곳부터
            while (chosen.Count < count)
            {
                Vector3 best = high[0]; float bd = -1;
                foreach (var v in high)
                {
                    float d = float.MaxValue;
                    foreach (var c in chosen) d = Mathf.Min(d, Vector2.Distance(new Vector2(v.x, v.z), new Vector2(c.x, c.z)));
                    d += (v.y - cut) * 0.5f;                                                 // 높은 곳일수록 조금 더
                    if (d > bd) { bd = d; best = v; }
                }
                if (bd < 1.5f) break;
                chosen.Add(best);
            }
            for (int k = 0; k < chosen.Count; k++)
            {
                var go = new GameObject("RiseSpot");
                go.transform.SetParent(t, false);
                go.transform.position = chosen[k];
                go.transform.rotation = Quaternion.Euler(-90, 0, 0);                         // 원뿔이 위를 향함
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true; main.prewarm = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                main.startRotation3D = true;
                main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
                var grad = new Gradient();
                grad.SetKeys(new[] { new GradientColorKey(new Color(0.66f, 0.42f, 0.26f), 0f), new GradientColorKey(new Color(0.86f, 0.64f, 0.42f), 0.3f), new GradientColorKey(new Color(0.72f, 0.74f, 0.88f), 0.55f),
                                     new GradientColorKey(new Color(1f, 0.8f, 0.32f), 0.8f), new GradientColorKey(new Color(0.8f, 0.92f, 1f), 1f) },
                             new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 90;
                var em = ps.emission; em.rateOverTime = 6f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.6f;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
                rot.x = rot.y = rot.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
                var sol = ps.sizeOverLifetime; sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.2f), new Keyframe(0.15f, 1), new Keyframe(0.75f, 0.8f), new Keyframe(1, 0)));
                var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.3f; noise.scrollSpeed = 0.15f;
                var pr = go.GetComponent<ParticleSystemRenderer>();
                pr.renderMode = ParticleSystemRenderMode.Mesh;
                pr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                pr.sharedMaterial = BitsMat("RisingBits", Color.white);
                pr.shadowCastingMode = ShadowCastingMode.Off;
                pr.alignment = ParticleSystemRenderSpace.World;
                // 같은 자리에서 빛 모래 기둥
                var g2 = new GameObject("RiseSpotGlow");
                g2.transform.SetParent(go.transform, false);
                var ps2 = g2.AddComponent<ParticleSystem>();
                var m2 = ps2.main;
                m2.loop = true; m2.prewarm = true;
                m2.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6.5f);
                m2.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                m2.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
                m2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.88f, 0.56f), new Color(0.75f, 0.92f, 1f));
                m2.simulationSpace = ParticleSystemSimulationSpace.World;
                m2.maxParticles = 160;
                var e2 = ps2.emission; e2.rateOverTime = 18f;
                var s2 = ps2.shape; s2.shapeType = ParticleSystemShapeType.Cone; s2.angle = 8f; s2.radius = 0.5f;
                var c2 = ps2.colorOverLifetime; c2.enabled = true;
                var gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0, 1) });
                c2.color = gr;
                var pr2 = g2.GetComponent<ParticleSystemRenderer>();
                pr2.renderMode = ParticleSystemRenderMode.Billboard;
                pr2.sharedMaterial = BitsMat("RisingSand", Color.white, "spark.png");
                pr2.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void RisingBits(GameObject building, float rate = 7f)
        {
            if (!building) return;
            var rs = building.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var t = building.transform;
            float top = b.max.y - t.position.y;
            var local = t.InverseTransformVector(b.size);
            float w = Mathf.Abs(local.x), d = Mathf.Abs(local.z);

            // 1) 벽돌 · 판자 조각
            var go = new GameObject("RisingBits");
            go.transform.SetParent(t, false);
            go.transform.localPosition = new Vector3(0, top * 0.62f, 0);                // 위쪽 절반 전체 (부서진 지붕 · 모서리)
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.24f, 0.55f);
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(0.66f, 0.42f, 0.26f), 0f), new GradientColorKey(new Color(0.86f, 0.64f, 0.42f), 0.25f), new GradientColorKey(new Color(0.88f, 0.5f, 0.5f), 0.45f),
                                 new GradientColorKey(new Color(0.86f, 0.64f, 0.3f), 0.65f), new GradientColorKey(new Color(1f, 0.94f, 0.82f), 0.82f), new GradientColorKey(new Color(1f, 0.8f, 0.32f), 1f) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 140;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(w * 0.95f, top * 0.55f, d * 0.85f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            vel.y = new ParticleSystem.MinMaxCurve(0.22f, 0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true; rot.separateAxes = true;
            rot.x = rot.y = rot.z = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.12f, 1), new Keyframe(0.7f, 0.9f), new Keyframe(1, 0)));
            var noise = ps.noise;
            noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.25f; noise.scrollSpeed = 0.1f;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Mesh;
            pr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            pr.sharedMaterial = BitsMat("RisingBits", Color.white);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.alignment = ParticleSystemRenderSpace.World;

            // 2) 빛나는 모래 알갱이 (작은 점, 더 빨리 · 더 많이)
            var g2 = new GameObject("RisingSand");
            g2.transform.SetParent(t, false);
            g2.transform.localPosition = new Vector3(0, top * 0.55f, 0);
            var ps2 = g2.AddComponent<ParticleSystem>();
            var m2 = ps2.main;
            m2.loop = true; m2.prewarm = true;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            m2.startSpeed = 0f;
            m2.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            m2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.88f, 0.56f), new Color(1f, 0.96f, 0.82f));
            m2.simulationSpace = ParticleSystemSimulationSpace.World;
            m2.scalingMode = ParticleSystemScalingMode.Hierarchy;
            m2.maxParticles = 200;
            var em2 = ps2.emission; em2.rateOverTime = rate * 4f;
            var sh2 = ps2.shape; sh2.shapeType = ParticleSystemShapeType.Box; sh2.scale = new Vector3(w * 1.0f, top * 0.6f, d * 0.9f);
            var v2 = ps2.velocityOverLifetime;
            v2.enabled = true; v2.space = ParticleSystemSimulationSpace.World;
            v2.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            v2.y = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            v2.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var sol2 = ps2.sizeOverLifetime;
            sol2.enabled = true;
            sol2.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0)));
            var pr2 = g2.GetComponent<ParticleSystemRenderer>();
            pr2.renderMode = ParticleSystemRenderMode.Billboard;
            pr2.sharedMaterial = BitsMat("RisingSand", Color.white, "spark.png");
            pr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>섬 하나: 윗면 · 걷는 판정 · 바스러지는 가장자리 조각 · 가장자리 안쪽 보이지 않는 벽(다리 입구 gaps는 비움)</summary>
        static Transform BuildIsle(Transform parent, Isle s, List<Vector3> gaps, System.Random rnd)
        {
            var root = Group(s.name, parent);
            root.localPosition = s.c;
            var top = new GameObject("Top");
            top.transform.SetParent(root, false);
            top.transform.localPosition = new Vector3(0, 0.01f, 0);
            var topMesh = IslandMesh("Island_" + s.name, s, 0.6f, 72);
            top.AddComponent<MeshFilter>().sharedMesh = topMesh;
            StoreOutline(root, top.transform, topMesh);                                // 마을 지도용 둘레 (플레이 중엔 static batching으로 메시를 못 읽음)
            var mr = top.AddComponent<MeshRenderer>();
            mr.sharedMaterial = EnvMat("Ground_Village", "ground_village.png", false);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            top.isStatic = true;
            var gc = new GameObject("GroundCollider").AddComponent<BoxCollider>();
            gc.transform.SetParent(root, false);
            gc.center = new Vector3(0, -0.5f, 0);
            gc.size = new Vector3(s.rx * 2.8f + 2, 1f, s.rz * 2.8f + 2);                 // 불룩한 곶까지 덮게

            var edge = Group("Edge", root);
            string[] kinds = { "Edge_A", "Edge_B", "Edge_C", "Edge_A", "Edge_B" };
            float perim = 0;
            for (int k = 0; k < 120; k++) perim += (ShapePoint(s, (k + 1) * Mathf.PI * 2 / 120) - ShapePoint(s, k * Mathf.PI * 2 / 120)).magnitude;
            int n = Mathf.Max(10, Mathf.RoundToInt(perim / 4.2f));
            // 둘레 길이 기준으로 고르게 (곶 쪽이 듬성해지지 않게)
            var cum = new float[361];
            for (int k = 1; k <= 360; k++) cum[k] = cum[k - 1] + (ShapePoint(s, k * Mathf.PI * 2 / 360) - ShapePoint(s, (k - 1) * Mathf.PI * 2 / 360)).magnitude;
            float AngleAt(float len) { int k = 1; while (k < 360 && cum[k] < len) k++; float t = Mathf.InverseLerp(cum[k - 1], cum[k], len); return (k - 1 + t) * Mathf.PI * 2 / 360; }
            for (int i = 0; i < n; i++)
            {
                float a = AngleAt((i + (float)(rnd.NextDouble() - 0.5) * 0.15f) * cum[360] / n);
                var p = Rim(s, a, 0f, out float yaw);
                bool atGap = gaps.Exists(g => (g - (s.c + p)).magnitude < 3.2f);
                if (!atGap) V(kinds[rnd.Next(kinds.Length)], edge, p, yaw + (float)(rnd.NextDouble() - 0.5) * 8f, 0.92f + (float)rnd.NextDouble() * 0.15f, false);
                else V("Edge_C", edge, p + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 0.6f, yaw, 0.7f, false);   // 다리 밑받침 (작게)
                var w = Rim(s, a, 1.4f, out _);
                if (gaps.Exists(g => (g - (s.c + w)).magnitude < 2.6f)) continue;            // 다리 입구는 열어 둠
                var bc = new GameObject("Bound").AddComponent<BoxCollider>();
                bc.transform.SetParent(edge, false);
                bc.transform.localPosition = w + Vector3.up * 2f;
                bc.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                bc.size = new Vector3(perim / n + 0.8f, 4f, 1f);
            }
            return root;
        }

        /// <summary>두 섬 사이 다리 끝점 (섬 가장자리 조금 안쪽)</summary>
        static (Vector3 a, Vector3 b) BridgeEnds(Isle s1, Isle s2)
        {
            var d = (s2.c - s1.c); d.y = 0; d.Normalize();
            var a = s1.c + d * (RimDist(s1, d) - 1.2f);
            var b = s2.c - d * (RimDist(s2, -d) - 1.2f);
            return (a, b);
        }

        /// <summary>다리 (성한 · 끊긴 두 벌) + 바닥 · 난간 판정 + 잠겼을 때 양 끝 막음. reqChapter 장을 넘으면 열림 (IslandBridge)</summary>
        static IslandBridge Bridge(Transform parent, string name, Isle s1, Isle s2, int reqChapter, params GameObject[] reveal)
        {
            var (a, b) = BridgeEnds(s1, s2);
            var root = Group(name, parent);
            root.localPosition = (a + b) * 0.5f;
            var dir = (b - a); float len = dir.magnitude; dir /= len;
            root.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            var whole = V("Bridge", root, Vector3.zero, 0, 1f, false);
            var broken = V("BridgeBroken", root, Vector3.zero, 0, 1f, false);
            foreach (var g in new[] { whole, broken }) if (g) { var sc = g.transform.localScale; g.transform.localScale = new Vector3(sc.x, sc.y, sc.z * len / 10f); }
            var deck = new GameObject("Deck").AddComponent<BoxCollider>();
            deck.transform.SetParent(root, false);
            deck.center = new Vector3(0, -0.5f, 0);
            deck.size = new Vector3(2.8f, 1f, len);
            foreach (float sx in new[] { -1.5f, 1.5f })
            {
                var rail = new GameObject("Rail").AddComponent<BoxCollider>();
                rail.transform.SetParent(root, false);
                rail.center = new Vector3(sx, 1.5f, 0);
                rail.size = new Vector3(0.3f, 3f, len - 1.6f);
            }
            var blocks = new List<Collider>();
            if (reqChapter > 0)
                foreach (float sz in new[] { -1f, 1f })
                {
                    var bl = new GameObject("Blocker").AddComponent<BoxCollider>();
                    bl.transform.SetParent(root, false);
                    bl.center = new Vector3(0, 1.5f, sz * (len * 0.5f - 0.8f));
                    bl.size = new Vector3(3.4f, 3f, 0.6f);
                    blocks.Add(bl);
                }
            var ib = root.gameObject.AddComponent<IslandBridge>();
            ib.reqChapter = reqChapter;
            ib.whole = whole;
            ib.broken = broken;
            ib.blockers = blocks.ToArray();
            ib.revealOnOpen = reveal;
            if (reqChapter <= 0 && broken) Object.DestroyImmediate(broken);
            return ib;
        }

        // ---------- 새벽 조명 (2026-11-18 사용자: 조명에 파란빛을 줘서 새벽 느낌) ----------
        // 툰 셰이더(브롤 모드)는 해 색을 곱하고 환경광(SH)을 더함 → 해 = 푸른 새벽빛, 환경광 = 남색. 따뜻함은 창 · 등불(발광) + 가로등 점광원
        // 화면 전체는 전역 볼륨(Art/Env/Village/DawnVolume.asset): 그늘 파랗게 · 밝은 곳 복숭아빛(스플릿 토닝) · 살짝 차가운 화이트 밸런스 · 블룸 · 남색 비네트
        static readonly Color DawnSunColor = new(0.8f, 0.85f, 1f);   // 2026-11-18 사용자가 고른 화면 기준 (서부 흙 + 푸른 새벽빛)
        static readonly Color AmbSky = new(0.36f, 0.44f, 0.8f), AmbEquator = new(0.42f, 0.42f, 0.68f), AmbGround = new(0.18f, 0.18f, 0.36f);

        static void DawnLighting()
        {
            var sun = new GameObject("DawnSun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = DawnSunColor;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.shadowBias = 0.08f;
            sun.shadowNormalBias = 0.8f;
            sun.transform.rotation = Quaternion.Euler(42f, 200f, 0f);               // 먼 쪽(북)에서 낮게 — 해 뜨기 직전 하늘빛
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbSky;
            RenderSettings.ambientEquatorColor = AmbEquator;
            RenderSettings.ambientGroundColor = AmbGround;
            // 툰 셰이더는 SampleSH로 환경광을 읽음 → 3색을 SH에 바로 구워 넣음 (자동 환경광 굽기를 기다리지 않게)
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(AmbEquator);
            sh.AddDirectionalLight(Vector3.up, (AmbSky - AmbEquator) * 0.9f, 1f);
            sh.AddDirectionalLight(Vector3.down, (AmbGround - AmbEquator) * 0.9f, 1f);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = false;

            var vol = new GameObject("DawnVolume").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0;
            vol.sharedProfile = DawnProfile();
        }

        static VolumeProfile DawnProfile()
        {
            const string p = "Assets/Art/Env/Village/DawnVolume.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
            if (prof) return prof;                                                     // 있으면 에디터에서 고친 값을 그대로 씀
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(prof, p);
            T Add<T>() where T : VolumeComponent { var c = prof.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, prof); return c; }
            var wb = Add<WhiteBalance>();
            wb.temperature.Override(-8f);
            wb.tint.Override(3f);
            // 그늘 · 어두운 곳은 파랗게, 밝은 곳은 살짝 따뜻하게 (스플릿 토닝은 화면 전체가 보랏빛 회색으로 죽어서 안 씀)
            var smh = Add<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(0.7f, 0.83f, 1.26f, -0.02f));
            smh.midtones.Override(new Vector4(0.95f, 0.97f, 1.04f, 0f));
            smh.highlights.Override(new Vector4(1.08f, 1f, 0.92f, 0.02f));
            var ca = Add<ColorAdjustments>();
            ca.saturation.Override(16f);                                          // 푸른 보정에도 따뜻한 색이 죽지 않게                                          // 푸른 보정에도 따뜻한 색이 죽지 않게
            ca.contrast.Override(10f);
            ca.postExposure.Override(0.1f);
            ca.colorFilter.Override(new Color(0.9f, 0.94f, 1f));
            var bl = Add<Bloom>();
            bl.threshold.Override(0.88f);
            bl.intensity.Override(0.85f);
            bl.scatter.Override(0.6f);
            bl.tint.Override(new Color(1f, 0.82f, 0.62f));
            var vg = Add<Vignette>();
            vg.color.Override(new Color(0.05f, 0.06f, 0.18f));
            vg.intensity.Override(0.28f);
            vg.smoothness.Override(0.5f);
            EditorUtility.SetDirty(prof);
            AssetDatabase.SaveAssets();
            return prof;
        }

        /// <summary>가로등 밑 따뜻한 빛 웅덩이 (푸른 새벽 바탕 위 주황 불빛). 툰 셰이더는 픽셀 추가 조명만 읽고 URP는 정점 조명이라 점광원 대신
        /// 바닥에 3단으로 끊은 가산 원판(Art/Env/Village/lamp_pool.png)을 깔아 표현</summary>
        static void LampLights(Transform town)
        {
            const string mp = "Assets/Art/Env/Village/LampPool.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Env/Village/lamp_pool.png"));
            m.SetColor("_BaseColor", new Color(1f, 0.8f, 0.55f, 0.55f));
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);                     // 투명 · 가산
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One); m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            var lamps = new List<Transform>();
            foreach (var t in town.GetComponentsInChildren<Transform>(true)) if (t.name == "Lamp") lamps.Add(t);
            foreach (var t in lamps)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "LampPool";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(t, false);
                q.transform.localPosition = new Vector3(0, 0.06f, 0);
                q.transform.localRotation = Quaternion.Euler(90, 0, 0);
                q.transform.localScale = new Vector3(6.5f, 6.5f, 1f) / Mathf.Max(0.01f, t.lossyScale.x);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ---------- 몽환 · 부서진 시간 (2026-11-18 사용자: 바닥 파랗게 · 새벽 더 세게 · 곳곳에 몽환적 · 부서진 시간 오브젝트 · 바깥은 뒤집힌 밤하늘) ----------
        static void Float(GameObject g, float bob, float period, float spin, float tilt = 2f)
        {
            if (!g) return;
            var f = g.GetComponent<DreamFloat>();
            if (!f) f = g.AddComponent<DreamFloat>();
            f.bob = bob; f.period = period; f.spin = spin; f.tilt = tilt;
        }

        // ---------- 시계탑 둘레를 도는 시곗바늘 (2026-11-18 사용자: 바늘 아래 돌판 없애고, 시계탑을 중심으로 공중에 떠서 천천히 돌게 + 바늘 끝 궤적에 황금빛 모래선) ----------
        const float HandHeight = 4.6f, HandRadius = 5.4f, HandSpin = 4f;   // 바늘 가운데 반경 (끝 ≈ 8.2m) · 초당 4도 = 한 바퀴 90초
        static void OrbitHand(Transform parent, Vector3 tower)
        {
            var pivot = new GameObject("ClockHand_Orbit").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = tower + Vector3.up * HandHeight;
            Float(pivot.gameObject, 0.25f, 9f, HandSpin, 1.2f);                 // 돌기 + 아주 느린 오르내림 · 기우뚱
            var hand = V("ClockHand_Float", pivot, Vector3.zero, 0, 1f, false);
            if (!hand) return;
            // 바늘 끝 = 길이 방향(로컬 z) 두 끝 중 폭이 좁은 쪽 → 바깥(+x)을 가리키게
            var mfs = hand.GetComponentsInChildren<MeshFilter>();
            float zMin = float.MaxValue, zMax = float.MinValue;
            var pts = new List<Vector3>();
            foreach (var mf in mfs) foreach (var v in mf.sharedMesh.vertices) { var p = hand.transform.InverseTransformPoint(mf.transform.TransformPoint(v)); pts.Add(p); zMin = Mathf.Min(zMin, p.z); zMax = Mathf.Max(zMax, p.z); }
            float WidthNear(float z) { float w = 0; foreach (var p in pts) if (Mathf.Abs(p.z - z) < 0.6f) w = Mathf.Max(w, Mathf.Abs(p.x)); return w; }
            bool tipAtMax = WidthNear(zMax) < WidthNear(zMin);
            float tipZ = tipAtMax ? zMax : zMin, mid = (zMin + zMax) * 0.5f;
            hand.transform.localRotation = Quaternion.Euler(0, tipAtMax ? 90 : -90, 0);   // 로컬 +z(또는 −z) 끝 → 바깥 +x
            hand.transform.localPosition = new Vector3(HandRadius, 0, 0) - hand.transform.localRotation * Vector3.Scale(hand.transform.localScale, new Vector3(0, 0, mid));   // 바늘 가운데가 반경 HandRadius에
            var tip = new GameObject("TipSand").transform;
            tip.SetParent(hand.transform, false);
            tip.localPosition = new Vector3(0, 0, tipZ + (tipAtMax ? -0.15f : 0.15f));

            // 황금빛 모래선: 부드러운 금빛 심지 + 모래알처럼 끊어진 반짝이 줄 (월드 공간이라 지나간 자리에 남아 천천히 따라옴)
            var core = tip.gameObject.AddComponent<TrailRenderer>();
            core.time = 13f; core.minVertexDistance = 0.12f; core.numCapVertices = 2;
            core.widthCurve = new AnimationCurve(new Keyframe(0, 0.38f), new Keyframe(0.35f, 0.26f), new Keyframe(1, 0.02f));
            core.colorGradient = Grad(new Color(1f, 0.8f, 0.28f), new Color(0.95f, 0.62f, 0.2f), 0.95f);
            core.sharedMaterial = BitsMat("HandSandCore", Color.white, "", false);    // 그림 없는 반투명 금빛 (가산이면 밝은 흙바닥 위에서 안 보임)
            core.shadowCastingMode = ShadowCastingMode.Off; core.receiveShadows = false;

            // 끝에서 흩날려 천천히 가라앉는 빛 모래알
            var ps = tip.gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.55f), new Color(1f, 0.72f, 0.3f));
            main.gravityModifier = 0.015f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = 16f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Grad(Color.white, Color.white, 1f));
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.2f)));
            var pr = tip.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = BitsMat("HandSandGrains", Color.white, "spark.png", false);   // 반투명 반짝이 = 선을 따라 남는 모래알
            pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false;
        }

        /// <summary>앞(진한 금) → 꼬리(옅어지며 사라짐) 그라데이션</summary>
        static Gradient Grad(Color head, Color tail, float alpha)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                      new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.6f, 0.45f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>섬마다 부서진 시간 오브젝트 (땅에 박힌 거대 시곗바늘 · 깨져 멈춘 시계판 · 위로 흐르는 모래시계 · 시간의 균열 · 시간 수정 · 숫자 돌 …) + 허공에 뜬 톱니 · 계단</summary>
        static void DreamDecor(Transform c, Transform g, Transform a, Transform w, Transform s, Transform floats)
        {
            var d = Group("DreamDecor", c);
            OrbitHand(d, new Vector3(0, 0, -4.5f));
            V("MeltWatch_Rock", d, new Vector3(-5.5f, 0, -12.2f), 30);
            Float(V("ClockShards", d, new Vector3(-12.5f, 3.4f, 1.5f), 70, 1f, false), 0.35f, 8f, 3f, 4f);
            d = Group("DreamDecor", g);
            V("TimeRift", d, new Vector3(-6.4f, 0, -2.6f), 30);
            d = Group("DreamDecor", a);
            V("Hourglass_Broken", d, new Vector3(-7f, 0, -2.4f), 0);
            d = Group("DreamDecor", w);
            V("Pendulum_Frozen", d, new Vector3(1f, 0, -6.2f), 160);
            d = Group("DreamDecor", s);
            V("TimeRift", d, new Vector3(7.4f, 0, -5.4f), -20, 0.9f);
            // 허공: 섬 사이에 멈춰 떠 있는 조각 몇 개만
            Float(V("StairsFloat", floats, new Vector3(-22, 1.5f, -16), 30, 1f, false), 0.5f, 9f, 2f, 5f);
            Float(V("GearStack_Float", floats, new Vector3(25, 3, -19), 0, 1.2f, false), 0.4f, 8f, -9f);
            Float(V("ClockShards", floats, new Vector3(-25, 4, 13), 20, 1.3f, false), 0.5f, 9f, 2f, 5f);
        }

        /// <summary>뒤집힌 밤하늘 사이 구름층(반투명, 천천히 흐름) 두 겹 + 섬 아래에서 반짝이는 별빛</summary>
        static void SkyLayers(Transform town)
        {
            const string mp = "Assets/Art/Env/Village/CloudLayer.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Env/Village/cloud_village.png"));
            m.SetTextureScale("_BaseMap", new Vector2(1.3f, 1.3f));                 // 크게 · 옅게 (반복 무늬가 얼룩처럼 보이지 않게)
            m.SetColor("_BaseColor", new Color(0.85f, 0.88f, 1f, 0.42f));
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 2990;
            EditorUtility.SetDirty(m);
            var layers = new[] { (y: -17f, sc: 300f, spd: new Vector2(0.004f, 0.0015f)), (y: -28f, sc: 340f, spd: new Vector2(-0.0025f, 0.002f)) };
            foreach (var l in layers)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "CloudLayer";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(town, false);
                q.transform.localPosition = new Vector3(0, l.y, 20);
                q.transform.localRotation = Quaternion.Euler(90, 0, 0);
                q.transform.localScale = new Vector3(l.sc, l.sc, 1);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                q.AddComponent<CloudScroll>().speed = l.spd;
            }
            // 별빛 반짝임: 섬 아래 넓은 상자에서 생겼다 사라지는 반짝이
            var go = new GameObject("StarTwinkle");
            go.transform.SetParent(town, false);
            go.transform.localPosition = new Vector3(0, -24, 15);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.9f, 1f), new Color(1f, 0.92f, 0.78f));
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 90f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(170, 18, 140);
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.sharedMaterial = BitsMat("StarSpark", Color.white, "spark.png");
        }

        /// <summary>섬 위를 떠도는 몽환적인 빛가루 (푸른 · 금빛 반짝이가 천천히 떠오르다 사라짐)</summary>
        static void IslandMotes(Transform isle, Isle s)
        {
            var go = new GameObject("DreamMotes");
            go.transform.SetParent(isle, false);
            go.transform.localPosition = new Vector3(0, 1.8f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.9f, 1f), new Color(1f, 0.86f, 0.6f));
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = s.rx * s.rz * 0.025f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(s.rx * 1.7f, 3f, s.rz * 1.7f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f); vel.y = new ParticleSystem.MinMaxCurve(0.06f, 0.22f); vel.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.2f; noise.scrollSpeed = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.sharedMaterial = BitsMat("DreamMote", Color.white, "spark.png");
        }

        // ---------- 신비한 나무 · 오브젝트 (2026-11-18 사용자: 몽환적인 신비한 나무 · 오브젝트를 더해 '여로' 느낌) ----------
        /// <summary>섬 안 빈자리 찾기: 윤곽 안쪽 · 기존 물체 / 입구 / NPC / 다리 입구와 겹치지 않는 곳 (못 찾으면 null)</summary>
        static Vector3? FreeSpot(Transform isle, Isle s, float clear, List<Vector3> gaps, System.Random rnd)
        {
            var obstacles = new List<(Vector3 c, float r)>();
            foreach (var r in isle.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                string n = r.transform.root == isle ? "" : r.gameObject.name;
                if (r.transform.IsChildOf(isle.Find("Edge")) || r.name == "Top" || r.name == "LampPool") continue;
                var b = r.bounds;
                obstacles.Add((new Vector3(b.center.x, 0, b.center.z), Mathf.Max(b.extents.x, b.extents.z) * 0.8f));
            }
            foreach (var fd in isle.GetComponentsInChildren<FacilityDoor>(true)) obstacles.Add((fd.transform.position, 4f));
            foreach (var t in isle.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("NpcSpot_") || t.name == "GateEntrance") obstacles.Add((t.position, 3f));
            foreach (var g in gaps) obstacles.Add((g, 4f));
            for (int tries = 0; tries < 60; tries++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), k = 0.35f + (float)rnd.NextDouble() * 0.45f;
                var local = ShapePoint(s, a) * k;
                var world = isle.position + local;
                bool ok = true;
                foreach (var o in obstacles) { var dd = world - o.c; dd.y = 0; if (dd.magnitude < o.r + clear) { ok = false; break; } }
                if (ok) return local;
            }
            return null;
        }

        /// <summary>나무 둘레로 빛 꽃잎 · 빛가루가 천천히 떠오름</summary>
        static void PetalRise(GameObject tree, float h)
        {
            if (!tree) return;
            var go = new GameObject("PetalRise");
            go.transform.SetParent(tree.transform, false);
            go.transform.localPosition = new Vector3(0, h * 0.6f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.95f), new Color(0.72f, 0.92f, 1f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = h * 0.35f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.4f); vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.sharedMaterial = BitsMat("DreamMote", Color.white, "spark.png");
        }

        static void MysticDecor((Transform t, Isle s)[] isles, Transform floats, List<Vector3> gaps, System.Random rnd)
        {
            // (이름, 개수, 비울 반경, 키) — 가운데 섬이 가장 풍성하게
            var plan = new Dictionary<string, (string n, int k, float clear, float h)[]>
            {
                ["Isle_Center"] = new[] { ("DreamTree_Crystal", 1, 3.5f, 7f), ("SpiritLanterns", 1, 1.6f, 4f), ("BellFlowers", 1, 1.2f, 1f), ("GlowMushrooms", 1, 1f, 1.2f) },
                ["Isle_Gate"] = new[] { ("RuneArch", 1, 2.4f, 4.5f) },
                ["Isle_Ammo"] = new[] { ("DreamTree_Willow", 1, 3f, 6.5f) },
                ["Isle_Wardrobe"] = new[] { ("DreamTree_Small", 1, 1.4f, 3f), ("BellFlowers", 1, 1.2f, 1f) },
                ["Isle_Gun"] = new[] { ("DreamTree_Small", 1, 1.4f, 3f) },
            };
            foreach (var (t, s) in isles)
            {
                if (!plan.TryGetValue(s.name, out var items)) continue;
                var grp = Group("Mystic", t);
                foreach (var (n, k, clear, h) in items)
                    for (int i = 0; i < k; i++)
                    {
                        var spot = FreeSpot(t, s, clear, gaps, rnd);
                        if (spot == null) { Debug.Log($"[Village] {s.name} {n} 자리 없음"); continue; }
                        var go = V(n, grp, spot.Value, (float)rnd.NextDouble() * 360f, 0.9f + (float)rnd.NextDouble() * 0.2f, n != "BellFlowers" && n != "GlowMushrooms");
                        if (n.StartsWith("DreamTree")) PetalRise(go, h);
                    }
            }
            // 허공: 수정이 매달린 뜬 바위 (천천히 오르내림)
            foreach (var pos in new[] { new Vector3(-20, 0.5f, -24), new Vector3(30, 0, -32), new Vector3(8, -1, 44) })
                Float(V("FloatRock_Crystal", floats, pos, (float)rnd.NextDouble() * 360f, 1f + (float)rnd.NextDouble() * 0.5f, false), 0.6f, 9f, 2f, 3f);
        }

        /// <summary>보이는 초승달: 섬 사이 틈(가운데 섬 남동쪽 · 문 섬 옆) 아래에 카메라 쪽을 향한 큰 달 (moon.png, 마젠타 배경을 지운 Codex 그림)</summary>
        static void CrescentMoon(Transform town)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Env/Village/moon.png");
            if (!tex) { Debug.Log("[Village] moon.png 없음 — 초승달 생략"); return; }
            const string mp = "Assets/Art/Env/Village/Moon.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 2995;
            EditorUtility.SetDirty(m);
            foreach (var (pos, size) in new[] { (new Vector3(17f, -10f, -21f), 16f), (new Vector3(-26f, -13f, -14f), 11f) })
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "CrescentMoon";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(town, false);
                q.transform.localPosition = pos;
                q.transform.localRotation = Quaternion.LookRotation(new Vector3(0, -17f, 11f).normalized, Vector3.forward);   // 게임 카메라 쪽을 봄
                q.transform.localScale = new Vector3(size, size, 1);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                Float(q, 0.6f, 14f, 0f, 1.5f);
            }
        }

        public static void BuildVillage()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 새벽 빛: 먼 쪽(북) 낮은 복숭아빛 해 + 남색 하늘 · 장밋빛 지평 · 어두운 허공 3색 환경광
            DawnLighting();

            var town = Group("Village");
            var rnd = new System.Random(1159);

            // 섬 아래 허공: 새벽 별밤 판 (위쪽 = 북 = 지평선 금빛), 그림자 없음
            var voidGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            voidGo.name = "DawnVoid";
            Object.DestroyImmediate(voidGo.GetComponent<Collider>());
            voidGo.transform.SetParent(town, false);
            voidGo.transform.localPosition = new Vector3(0, -40f, 30f);
            voidGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
            voidGo.transform.localScale = new Vector3(380, 380, 1);                 // 뒤집힌 밤하늘 (달 · 은하수 · 별자리)
            var vmr = voidGo.GetComponent<MeshRenderer>();
            vmr.sharedMaterial = EnvMat("Void_Village", "void_village.png", true);
            if (AssetImporter.GetAtPath("Assets/Art/Env/Village/void_village.png") is TextureImporter vti && vti.wrapMode != TextureWrapMode.Mirror) { vti.wrapMode = TextureWrapMode.Mirror; vti.SaveAndReimport(); }
            vmr.sharedMaterial.SetTextureScale("_BaseMap", new Vector2(4, 4));      // Codex 밤하늘 그림을 거울 반복 (별이 뭉개지지 않게)
            vmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vmr.receiveShadows = false;

            // 다리 끝 자리 (섬 가장자리를 비워 둠)
            var links = new[] { (Center, GateIsle), (Center, AmmoIsle), (Center, WardIsle), (Center, GunIsle) };
            var gaps = new List<Vector3>();
            foreach (var (s1, s2) in links) { var (a, b) = BridgeEnds(s1, s2); gaps.Add(a); gaps.Add(b); }
            var isles = Group("Islands", town);
            var cI = BuildIsle(isles, Center, gaps, rnd);
            var gI = BuildIsle(isles, GateIsle, gaps, rnd);
            var aI = BuildIsle(isles, AmmoIsle, gaps, rnd);
            var wI = BuildIsle(isles, WardIsle, gaps, rnd);
            var sI = BuildIsle(isles, GunIsle, gaps, rnd);

            // ----- 가운데 섬: 시계 광장 · 여관(기억의 방) · 뻐꾸기 -----
            V("Plaza", cI, new Vector3(0, 0, -4.5f), 0, 1f, false);
            V("Centerpiece", cI, new Vector3(0, 0, -4.5f), 20, 1f);
            var inn = Facility(cI, "Inn_MemoryRoom", "Inn", new Vector3(0, 0, 9f), 180, 4.6f, Meta.Facility.MemoryRoom, "기억의 방 (여관 2층)");
            inn.Find("NpcSpot_MemoryRoom").localPosition = new Vector3(5.2f, 0, 5.4f);     // 뻐꾸기: 문 옆 가로등(-3, 3.4)에 붙어 있어 바깥으로 → 월드 (-5.2, 3.6) (2026-11-18 사용자)
            V("Well", cI, new Vector3(-10, 0, -1), 15);
            V("Bench", cI, new Vector3(8, 0, 2.5f), 230);
            V("Signpost", cI, new Vector3(4.6f, 0, -12), 200);
            V("Wagon", cI, new Vector3(-11.5f, 0, 6.5f), 75, 0.9f);
            V("Barrels", cI, new Vector3(8.2f, 0, 8.8f), 20);
            foreach (var pos in new[] { new Vector3(-3, 0, 3.4f), new Vector3(3, 0, 3.4f), new Vector3(-8.5f, 0, -4.5f), new Vector3(8.5f, 0, -6.5f), new Vector3(-2.4f, 0, -13) })
                V("Lamp", cI, pos, 0);
            V("Cactus", cI, new Vector3(13, 0, 6), 40, 0.9f);

            // ----- 남쪽 문 섬: 시간의 문 · 괘종시계 문지기 (처음부터) -----
            var gate = Group("TimeGate", gI);
            gate.localPosition = new Vector3(0, 0, -3f);
            var tg = V("TimeGate", gate, Vector3.zero, 0, 1f, false);
            RisingBits(tg, 2f);
            RiseSpots(tg, 2);
            foreach (float gx in new[] { -3.6f, 3.6f })              // 문기둥만 막음 (가운데는 열려 있지만 뒤가 섬 끝)
            {
                var pc = new GameObject("Pillar").AddComponent<BoxCollider>();
                pc.transform.SetParent(gate, false);
                pc.transform.localPosition = new Vector3(gx, 2f, 0);
                pc.size = new Vector3(1.6f, 4f, 1.6f);
            }
            var entry = new GameObject("GateEntrance");
            entry.transform.SetParent(gate, false);
            entry.transform.localPosition = new Vector3(0, 0, 2.4f);
            var cg = entry.AddComponent<ChapterGate>();
            cg.prompt = "시간의 문";
            cg.radius = 3f;
            var npcGate = new GameObject("NpcSpot_Gatekeeper").transform;     // 괘종시계 문지기 자리
            npcGate.SetParent(gate, false);
            npcGate.localPosition = new Vector3(-5.4f, 0, 2.6f);
            npcGate.localRotation = Quaternion.Euler(0, 20, 0);
            PlaceNpc(npcGate);
            V("Lamp", gI, new Vector3(5.4f, 0, 0.4f), 0);
            V("Lamp", gI, new Vector3(-2.6f, 0, 5.2f), 0);
            V("Fence", gI, new Vector3(6.5f, 0, -3.5f), -70);

            // ----- 동쪽 섬: 탄약 공방 · 모래시계 대장장이 (2장) -----
            var ammo = Facility(aI, "AmmoWorkshop", "AmmoWorkshop", new Vector3(2.5f, 0, 1f), -90, 4.1f, Meta.Facility.AmmoWorkshop, "탄약 공방");
            V("Windpump", aI, new Vector3(4.5f, 0, -6f), 30);
            V("Barrels", aI, new Vector3(-4.6f, 0, 6.4f), -60);     // 대장장이(NpcSpot)에 붙어 있어 옆으로 (2026-11-18 사용자)
            V("Crates", aI, new Vector3(-4.5f, 0, -5.4f), 10);
            V("Lamp", aI, new Vector3(-6.5f, 0, 3.2f), 0);
            V("Cactus", aI, new Vector3(7.5f, 0, 5f), 110, 0.9f);

            // ----- 북동 섬: 의상실 · 실타래 재봉사 (2장) -----
            var ward = Facility(wI, "Wardrobe", "Wardrobe", new Vector3(1.5f, 0, 2.5f), 225, 3.6f, Meta.Facility.Wardrobe, "의상실");
            V("Bench", wI, new Vector3(-5, 0, -2), 135);
            V("Lamp", wI, new Vector3(-3.5f, 0, -5), 0);
            V("Fence", wI, new Vector3(5.5f, 0, -4.5f), 20);
            V("Cactus", wI, new Vector3(6.5f, 0, 4.5f), 70, 0.8f);

            // ----- 서쪽 섬: 총포상 · 빈 외투 보안관 (3장) -----
            var gun = Facility(sI, "GunShop", "GunShop", new Vector3(-2.5f, 0, 1f), 90, 4.1f, Meta.Facility.GunShop, "총포상");
            V("WaterTower", sI, new Vector3(-4.5f, 0, -6f), -25);
            V("HitchPost", sI, new Vector3(4.2f, 0, -4.6f), 90);
            V("Crates", sI, new Vector3(4.6f, 0, 5.6f), 75);
            V("Lamp", sI, new Vector3(6.5f, 0, -1.6f), 0);
            V("Fence", sI, new Vector3(-7.5f, 0, 4.5f), 60);

            // 다리: 문 섬은 처음부터, 동 · 북동은 2장, 서는 3장 (건너편 NPC는 열릴 때 나타남)
            var bridges = Group("Bridges", town);
            Bridge(bridges, "Bridge_Gate", Center, GateIsle, 0);
            Bridge(bridges, "Bridge_Ammo", Center, AmmoIsle, Meta.FacilityUnlockChapter[(int)Meta.Facility.AmmoWorkshop], ammo.Find("NpcSpot_AmmoWorkshop").gameObject);
            Bridge(bridges, "Bridge_Wardrobe", Center, WardIsle, Meta.FacilityUnlockChapter[(int)Meta.Facility.Wardrobe], ward.Find("NpcSpot_Wardrobe").gameObject);
            Bridge(bridges, "Bridge_Gun", Center, GunIsle, Meta.FacilityUnlockChapter[(int)Meta.Facility.GunShop], gun.Find("NpcSpot_GunShop").gameObject);

            // 섬 둘레 허공에 뜬 조각 섬 · 부스러기 (깊이를 달리해 아래로 이어지는 허공 느낌)
            var floats = Group("FloatingIsles", town);
            V("FloatIsle_L", floats, new Vector3(-24, -9, 28), 30, 1f, false);
            V("FloatIsle_L", floats, new Vector3(52, -12, 18), 200, 0.9f, false);
            V("FloatIsle_L", floats, new Vector3(-52, -16, -20), 120, 1.1f, false);
            V("FloatIsle_M", floats, new Vector3(-18, -5, -30), 120, 1f, false);
            V("FloatIsle_M", floats, new Vector3(20, -6, -28), 300, 1.1f, false);
            V("FloatIsle_M", floats, new Vector3(4, -10, 42), 80, 1f, false);
            V("FloatIsle_M", floats, new Vector3(50, -7, -20), 60, 1f, false);
            V("FloatIsle_S", floats, new Vector3(-10, -3, 24), 10, 1f, false);
            V("FloatIsle_S", floats, new Vector3(42, -2.5f, 10), 150, 1.2f, false);
            V("FloatIsle_S", floats, new Vector3(-48, -2, 14), 240, 1f, false);
            V("FloatIsle_S", floats, new Vector3(-44, -4, -10), 60, 0.9f, false);
            V("FloatIsle_S", floats, new Vector3(12, -4, -42), 90, 1.1f, false);
            V("FloatIsle_S", floats, new Vector3(-12, -6, -44), 300, 1f, false);
            V("FloatIsle_S", floats, new Vector3(38, -5, 38), 30, 1f, false);
            foreach (var d in new[] { new Vector3(-20, 1.5f, 12), new Vector3(18, 1, -14), new Vector3(-10, 1, -22), new Vector3(12, 1.5f, -22), new Vector3(10, 2, 22), new Vector3(-24, 1, -10), new Vector3(46, 1, -12), new Vector3(36, 2, 14) })
                V("Debris", floats, d, (float)rnd.NextDouble() * 360, 1f + (float)rnd.NextDouble() * 0.4f, false);
            foreach (Transform f in floats)                                           // 떠 있는 섬 · 부스러기: 아주 천천히 오르내림
                Float(f.gameObject, f.name == "Debris" ? 0.45f : 0.25f, f.name == "Debris" ? 6f : 11f, f.name == "Debris" ? 4f : 0f, f.name == "Debris" ? 4f : 0.8f);
            DreamDecor(cI, gI, aI, wI, sI, floats);
            MysticDecor(new[] { (cI, Center), (gI, GateIsle), (aI, AmmoIsle), (wI, WardIsle), (sI, GunIsle) }, floats, gaps, rnd);
            SkyLayers(town);
            CrescentMoon(town);
            foreach (var isl in new[] { (cI, Center), (gI, GateIsle), (aI, AmmoIsle), (wI, WardIsle), (sI, GunIsle) }) IslandMotes(isl.Item1, isl.Item2);

            // 흩뿌림 (판정 없음): 섬마다 마른 풀 · 굴러다니는 풀
            foreach (var s in new[] { Center, GateIsle, AmmoIsle, WardIsle, GunIsle })
            {
                var sc = Group("Scatter", town.Find("Islands/" + s.name));
                int count = Mathf.RoundToInt(s.rx * s.rz * 0.03f);
                for (int i = 0; i < count; i++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = 0.25f + Mathf.Sqrt((float)rnd.NextDouble()) * 0.65f;
                    var p = new Vector3(Mathf.Sin(a) * s.rx * r, 0, Mathf.Cos(a) * s.rz * r);
                    if (s.name == Center.name && (new Vector2(p.x, p.z + 4.5f).magnitude < 7.5f || (Mathf.Abs(p.x) < 6 && p.z > 2))) continue;
                    V(rnd.NextDouble() < 0.15 ? "Tumbleweed" : "GrassTuft", sc, p, rnd.Next(360), (float)(0.8 + rnd.NextDouble() * 0.6), false);
                }
            }

            var player = SceneBuilders.MakePlayer(new Vector3(0, 0.1f, 1.5f));   // 광장 북쪽, 여관 앞에서 깨어남
            player.combatEnabled = false;
            var cam = SceneBuilders.MakeCamera(player.transform.position).GetComponent<Camera>();
            cam.backgroundColor = new Color(0.09f, 0.08f, 0.2f);               // 허공 판 밖 = 깊은 남색
            cam.farClipPlane = 260f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;    // 새벽 색보정 볼륨 (DawnVolume)
            LampLights(town);
            new GameObject("VillageManager").AddComponent<VillageManager>();
            var ui = new GameObject("GameUI").AddComponent<GameUI>();
            ui.village = true;
            EditorSceneManager.SaveScene(scene, VillagePath);
            Debug.Log("[CatTang] 새벽 허공 섬 마을 씬 저장 " + VillagePath);
        }

        /// <summary>시설 건물(마을 키트) + 입구(FacilityDoor) + 잠김 울타리 + 문 앞 NPC 자리. front = 건물 중심에서 문 앞까지 거리</summary>
        static Transform Facility(Transform parent, string name, string building, Vector3 pos, float yaw, float front, Meta.Facility f, string label)
        {
            var root = Group(name, parent);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            var bgo = V(building, root, Vector3.zero, 0, 1f);
            RisingBits(bgo, 4f);
            RiseSpots(bgo, 3);
            var locked = V("Fence", root, new Vector3(0, 0, front + 0.6f), 0, 0.9f, false);
            if (locked) locked.name = "Locked";
            var entry = new GameObject("Entrance");
            entry.transform.SetParent(root, false);
            entry.transform.localPosition = new Vector3(0, 0, front + 1.6f);
            var fd = entry.AddComponent<FacilityDoor>();
            fd.facility = f;
            fd.prompt = label;
            fd.radius = 2.8f;
            fd.lockedVisual = locked;
            var npc = new GameObject("NpcSpot_" + f).transform;                    // 잊혀진 존재 NPC 자리
            npc.SetParent(root, false);
            npc.localPosition = new Vector3(3.4f, 0, front + 1.2f);
            npc.localRotation = Quaternion.Euler(0, 200, 0);
            PlaceNpc(npc);
            return root;
        }

        // ---------- 메인 로비 ----------
        // 화면은 전부 그림 (LobbyScreen이 Resources/UI/Lobby 그림으로 배경 · 제목 · 슬롯 카드 · 버튼을 깔고 연출). 씬엔 카메라와 로비만
        public static void BuildLobby()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.14f, 0.26f);
            camGo.AddComponent<AudioListener>();
            new GameObject("Lobby").AddComponent<LobbyScreen>();
            EditorSceneManager.SaveScene(scene, LobbyPath);
            Debug.Log("[CatTang] 메인 로비 씬 저장 " + LobbyPath);
        }
    }
}
