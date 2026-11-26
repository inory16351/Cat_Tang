// 엔딩 스토리 맵 '자정의 시계판' (2026-11-26 사용자: 엔딩도 스토리 맵에서 · 배치 = 시계판 원형 · 마을에서 깨어나 넘어감 · 새벽으로 밝아짐)
//   맵 = Resources/Story/StoryDial.prefab (원점 = 가운데 광장 섬 바닥). 2026-11-26 사용자: 작별은 실제 마을 섬에서 → 지금은 안 씀 (내면 맵 StoryInner로 바뀜)
//   마을 키트(Resources/Kit/Village)를 시계판 배치로 다시 조립 — 마을이 마지막으로 남긴 모습:
//     가운데 광장 섬(시계탑 11:59 · 거울 고양이 자리) + 시계 숫자 자리의 섬 다섯, 작별 순서대로 시계 방향
//     2시 보안관(총포상) → 4시 대장장이(탄약 공방) → 8시 재봉사(의상실) → 10시 뻐꾸기(여관) → 12시 문지기(시간의 문)
//     섬마다 가운데에서 다리 하나. 허공 아래엔 11:59를 가리키는 거대한 바늘 둘 · 둘레엔 떠 있는 숫자 돌 12개
//   이름 규칙 (Ch5Ending과 맞춤): DialIsle_{Gun,Ammo,Wardrobe,Cuckoo,Gate} · DialBridge_* · NpcSpot_* · Marks/Spawn · ReaperAt
//     MirrorCatSpot · PlazaClock/FaceAxis/{MinuteHand,HourHand} (축 로컬 z, 0° = 12시) · DialHands/{GiantMinute,GiantHour} (y축, 0° = 12시 = +Z)
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Isle = CatTang.EditorTools.VillageKitBuilder.Isle;

namespace CatTang.EditorTools
{
    public static class StoryDialBuilder
    {
        public const string MapPath = "Assets/Resources/Story/StoryDial.prefab";
        const float DialR = 31f;                                     // 가운데 → 섬 중심
        static readonly Isle Center = new("DialIsle_Center", Vector3.zero, 9.5f, 9f, 71);

        struct Stop { public string key, building, spot; public int hour; public float front; public Isle isle; }

        static Vector3 HourDir(float h) { float a = h * 30f * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); }

        static Stop S(string key, int hour, string building, string spot, float front, int seed) =>
            new() { key = key, hour = hour, building = building, spot = spot, front = front, isle = new Isle("DialIsle_" + key, HourDir(hour) * DialR, 7.5f, 7f, seed) };

        static readonly Stop[] Stops =
        {
            S("Gun", 2, "GunShop", "NpcSpot_GunShop", 4.1f, 83),
            S("Ammo", 4, "AmmoWorkshop", "NpcSpot_AmmoWorkshop", 4.1f, 89),
            S("Wardrobe", 8, "Wardrobe", "NpcSpot_Wardrobe", 3.6f, 97),
            S("Cuckoo", 10, "Inn", "NpcSpot_MemoryRoom", 4.6f, 101),
            S("Gate", 12, "TimeGate", "NpcSpot_Gatekeeper", 2.4f, 107),
        };

        static float YawTo(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;          // +Z가 d를 보게
        static float FaceYaw(Vector3 d) => Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;      // 정면(−Z) 모델이 d를 보게

        static (Vector3 a, Vector3 b) Ends(Isle s1, Isle s2)
        {
            var d = s2.c - s1.c; d.y = 0; d.Normalize();
            return (s1.c + d * (VillageKitBuilder.RimDist(s1, d) - 1.2f), s2.c - d * (VillageKitBuilder.RimDist(s2, -d) - 1.2f));
        }

        [MenuItem("CatTang/엔딩 스토리 맵 시계판 조립")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryDial").transform;
            try
            {
                var rnd = new System.Random(1200);
                var gaps = new List<Vector3>();
                foreach (var s in Stops) { var (a, b) = Ends(Center, s.isle); gaps.Add(a); gaps.Add(b); }
                var isles = VillageKitBuilder.Group("Islands", root);
                var marks = VillageKitBuilder.Group("Marks", root);

                // ----- 가운데 광장: 시계탑 11:59 · 거울 고양이 자리 -----
                var cI = VillageKitBuilder.BuildIsle(isles, Center, gaps, rnd);
                VillageKitBuilder.V("Plaza", cI, Vector3.zero, 0, 1f, false);
                var tower = VillageKitBuilder.V("Centerpiece", cI, new Vector3(0, 0, 1.5f), 0, 1f);
                // 가로등 · 거울 고양이는 다리 줄기(0 · 60 · 120 · 240 · 300도) 사이에만 (2026-11-26 사용자: 다리 입구를 막거나 헷갈리게 하는 것 치움)
                foreach (float deg in new[] { 90f, 270f, 155f, 205f })
                    VillageKitBuilder.V("Lamp", cI, new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad), 0, Mathf.Cos(deg * Mathf.Deg2Rad)) * 6.5f, 0);
                Mark(marks, "Spawn", new Vector3(0, 0, -7f), 0);
                Mark(marks, "ReaperAt", new Vector3(5.2f, 0, -5.8f), 0);
                Mark(marks, "MirrorCatSpot", new Vector3(-1.2f, 0, -4.6f), 0);
                PlazaClock(root, tower ? tower.transform : null);

                // ----- 시계 숫자 자리의 섬 다섯 -----
                var bridges = VillageKitBuilder.Group("Bridges", root);
                foreach (var s in Stops)
                {
                    var iT = VillageKitBuilder.BuildIsle(isles, s.isle, gaps, rnd);
                    var toCenter = (-s.isle.c).normalized;
                    Vector3 bpos, front;
                    if (s.key == "Gate") { bpos = new Vector3(0, 0, 2.6f); front = Vector3.back; }
                    else
                    {
                        // 건물은 바깥 옆(동 · 서)에 두고 정면이 가운데 쪽 가로를 봄 → 카메라(남)에선 옆모습, 고양이를 가리지 않음
                        float side = Mathf.Sign(s.isle.c.x);
                        bpos = new Vector3(2.8f * side, 0, Mathf.Sign(s.isle.c.z) * 1.6f);   // 다리 입구(가운데 쪽)에서 멀게
                        front = new Vector3(-side, 0, 0);
                    }
                    var fac = VillageKitBuilder.Group("Facility_" + s.key, iT);
                    fac.localPosition = bpos;
                    fac.localRotation = Quaternion.Euler(0, YawTo(front), 0);
                    var bgo = VillageKitBuilder.V(s.building, fac, Vector3.zero, 0, 1f);
                    VillageKitBuilder.RisingBits(bgo, 5f);                       // 녹아 오르는 조각 · 금빛 모래
                    VillageKitBuilder.RiseSpots(bgo, 3);
                    if (s.key == "Gate")
                        foreach (float gx in new[] { -3.6f, 3.6f })
                        {
                            var pc = new GameObject("Pillar").AddComponent<BoxCollider>();
                            pc.transform.SetParent(fac, false);
                            pc.transform.localPosition = new Vector3(gx, 2f, 0);
                            pc.size = new Vector3(1.6f, 4f, 1.6f);
                        }
                    var spot = new GameObject(s.spot).transform;
                    spot.SetParent(iT, false);
                    var sp = s.key == "Gate" ? new Vector3(-4.4f, 0, 0.2f) : bpos + front * (s.front + 1.4f) + Vector3.forward * (s.isle.c.z > 0 ? -1.2f : 1.2f);
                    // NPC가 다리 입구 정면에 서지 않게: 다리 줄기(섬 중심 → 가운데 방향)에서 옆으로 4m 이상
                    float along = Vector3.Dot(sp, toCenter); var perp = sp - toCenter * along;
                    if (along > -1f && perp.magnitude < 4f)
                    {
                        var sideDir = perp.magnitude > 0.1f ? perp.normalized : -Vector3.Cross(Vector3.up, toCenter) * Mathf.Sign(Vector3.Dot(bpos, Vector3.Cross(Vector3.up, toCenter)) + 0.001f);
                        sp = toCenter * Mathf.Min(along, 2.5f) + sideDir * 4f;
                    }
                    spot.localPosition = sp;
                    spot.localRotation = Quaternion.Euler(0, FaceYaw(toCenter), 0);
                    VillageKitBuilder.PlaceNpc(spot);
                    VillageKitBuilder.V("Lamp", iT, spot.localPosition + Quaternion.Euler(0, YawTo(toCenter), 0) * new Vector3(-2.6f, 0, 1.4f), 0);
                    VillageKitBuilder.V(rnd.NextDouble() < 0.5 ? "Crates" : "Barrels", iT, -toCenter * 6.2f + Vector3.Cross(Vector3.up, toCenter) * 2.5f, rnd.Next(360));

                    var (a, b) = Ends(Center, s.isle);
                    Bridge(bridges, "DialBridge_" + s.key, a, b);
                }

                ClearMouths(isles, bridges);
                MeltUndersides(isles);
                DialHands(root);
                Numerals(root);
                Sky(root, rnd);

                PrefabUtility.SaveAsPrefabAsset(root.gameObject, MapPath);
                Debug.Log("[StoryDial] 맵 " + MapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        // ---------- 녹아내리는 아랫면 + 파티클 (2026-11-26 사용자: 파티클 더하고 맵 아랫부분을 블렌더로 깎아 녹아내리는 느낌) ----------

        /// <summary>섬마다 블렌더 아랫면(Resources/Kit/StoryDial/Under_*) + 흘러내리는 모래 파티클
        /// (2026-11-26 사용자: 물방울 처리 말고 모래가 흐르는 느낌으로) — 모래 기둥 끝에서 가늘게 쏟아지는 모래 줄기 · 섬 가장자리를 타고 흘러넘치는 모래 커튼</summary>
        static void MeltUndersides(Transform isles)
        {
            int under = 0, streams = 0;
            foreach (Transform isle in isles)
            {
                if (!isle.name.StartsWith("DialIsle_")) continue;
                string key = isle.name.Substring(9);
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyModelBuilder.StoryDialKitDir + "Under_" + key + ".prefab");
                if (pf)
                {
                    var u = (GameObject)PrefabUtility.InstantiatePrefab(pf, isle);
                    u.name = "MeltUnder";
                    u.transform.localPosition = Vector3.zero; u.transform.localRotation = Quaternion.identity;
                    foreach (var c in u.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    under++;
                }
                else Debug.LogWarning("[StoryDial] 아랫면 키트 없음 Under_" + key + " — 메뉴 '엔딩 시계판 아랫면 키트 만들기' 먼저");
                // 모래 기둥이 흩어지기 시작하는 곳 (tips json: 유니티 섬 로컬 x, y, z) — 고르게 골라 섬마다 12곳(가운데 16)
                var tipsPath = EnemyModelBuilder.StoryDialSrc + "Kit_D_Under_" + key + "_tips.json";
                var tips = new List<Vector3>();
                if (System.IO.File.Exists(tipsPath))
                {
                    var txt = System.IO.File.ReadAllText(tipsPath);
                    var nums = System.Text.RegularExpressions.Regex.Matches(txt, @"-?\d+(\.\d+)?([eE]-?\d+)?");
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    for (int i = 0; i + 2 < nums.Count; i += 3)
                        tips.Add(new Vector3(float.Parse(nums[i].Value, inv), float.Parse(nums[i + 1].Value, inv), float.Parse(nums[i + 2].Value, inv)));
                }
                var fx = VillageKitBuilder.Group("SandFx", isle);
                DissolveDust(fx, isle.Find("MeltUnder"), key);
                foreach (var t in Spread(tips, key == "Center" ? 16 : 12)) { SandStream(fx, t); streams++; }
                RimSandCurtain(fx, isle, key);
            }
            Debug.Log($"[StoryDial] 아랫면 {under}개 · 모래 줄기 {streams}곳");
        }

        /// <summary>디졸브 구간(아랫면 높이 DissolveTop ~ DissolveBottom)의 면에서만 모래 알갱이가 떨어져 나와 흩날림 — 도형이 그대로 모래가 되어 흘러내리는 것처럼
        /// (2026-11-26 사용자: 도형과 파티클이 자연스럽게 이어지게, 아래로 내려가면서 점점 파티클과 디졸브). 아래쪽일수록 많이(높이 띠 셋)</summary>
        static void DissolveDust(Transform parent, Transform under, string key)
        {
            if (!under) return;
            var mf = under.GetComponentInChildren<MeshFilter>();
            if (!mf || !mf.sharedMesh) return;
            var src = mf.sharedMesh;
            var vs = src.vertices; var tris = src.triangles;
            var toIsle = parent.parent.worldToLocalMatrix * mf.transform.localToWorldMatrix;    // 메시 → 섬 로컬
            float[] bandTop = { EnemyModelBuilder.DialDissolveTop, -5.5f, -8.5f }, bandBot = { -5.5f, -8.5f, EnemyModelBuilder.DialDissolveBottom };
            float[] density = { 0.35f, 0.9f, 1.6f };                         // 1m²당 초당 알갱이
            for (int band = 0; band < 3; band++)
            {
                var bv = new List<Vector3>(); var bt = new List<int>(); float area = 0;
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    var a = vs[tris[t]]; var b = vs[tris[t + 1]]; var c = vs[tris[t + 2]];
                    float cy = (a.y + b.y + c.y) / 3f;                          // 메시 오브젝트 공간 높이 = 셰이더 디졸브 기준과 같음
                    if (cy > bandTop[band] || cy <= bandBot[band]) continue;
                    int i0 = bv.Count;
                    bv.Add(toIsle.MultiplyPoint3x4(a)); bv.Add(toIsle.MultiplyPoint3x4(b)); bv.Add(toIsle.MultiplyPoint3x4(c));
                    bt.Add(i0); bt.Add(i0 + 1); bt.Add(i0 + 2);
                    area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                }
                if (bt.Count == 0) continue;
                var m = new Mesh { name = $"DialDust_{key}_{band}", indexFormat = bv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(bv); m.SetTriangles(bt, 0); m.RecalculateNormals(); m.RecalculateBounds();
                string mp = $"Assets/Art/Env/Village/DialDust_{key}_{band}.asset";
                AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(m, mp);

                var go = new GameObject("DissolveDust_" + band);
                go.transform.SetParent(parent, false);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true; main.prewarm = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);      // 겉에서 살짝 떨어져 나옴
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f + band * 0.01f, 0.09f + band * 0.02f);
                main.startColor = new ParticleSystem.MinMaxGradient(SandA, SandB);
                main.gravityModifier = 0.32f;
                main.maxParticles = 1500;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = Mathf.Min(area * density[band], 420f);
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Mesh; sh.meshShapeType = ParticleSystemMeshShapeType.Triangle; sh.mesh = m;
                var noise = ps.noise; noise.enabled = true; noise.strength = 0.18f; noise.frequency = 0.5f; noise.scrollSpeed = 0.25f;
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.4f)));
                var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeInOut(0.06f, 0.55f);
                var pr = go.GetComponent<ParticleSystemRenderer>();
                pr.renderMode = ParticleSystemRenderMode.Stretch;
                pr.velocityScale = 0.05f; pr.lengthScale = 1.1f;
                pr.sharedMaterial = VillageKitBuilder.BitsMat("DialSandGrain", Color.white, "", false);
                pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>점들 중 서로 가장 멀리 떨어진 n개 (가장 먼 점 고르기)</summary>
        static List<Vector3> Spread(List<Vector3> pts, int n)
        {
            var res = new List<Vector3>();
            if (pts.Count <= n) { res.AddRange(pts); return res; }
            res.Add(pts[0]);
            while (res.Count < n)
            {
                Vector3 best = pts[0]; float bd = -1f;
                foreach (var p in pts) { float d = float.MaxValue; foreach (var q in res) d = Mathf.Min(d, (p - q).sqrMagnitude); if (d > bd) { bd = d; best = p; } }
                res.Add(best);
            }
            return res;
        }

        static readonly Color SandA = new(0.93f, 0.74f, 0.47f), SandB = new(1f, 0.87f, 0.62f);

        static Gradient FadeInOut(float inAt, float outAt)
        {
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, inAt), new GradientAlphaKey(1, outAt), new GradientAlphaKey(0, 1) });
            return gr;
        }

        /// <summary>모래 기둥 끝: 모래시계처럼 가늘게 쏟아지는 모래 알갱이 줄기 (아래로 갈수록 퍼지며 옅어짐) + 가끔 빛나는 알갱이</summary>
        static void SandStream(Transform parent, Vector3 at)
        {
            var go = new GameObject("SandStream");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);             // 원뿔 축 = 아래
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(SandA, SandB);
            main.gravityModifier = 0.45f;
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = new ParticleSystem.MinMaxCurve(34f, 46f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 3f; sh.radius = 0.09f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.6f; noise.scrollSpeed = 0.3f;   // 아래로 갈수록 살짝 흩어짐
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.55f)));
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeInOut(0.05f, 0.6f);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;                     // 떨어지는 속도만큼 늘어나 가는 모래 줄로 읽힘
            pr.velocityScale = 0.06f; pr.lengthScale = 1.2f;
            pr.sharedMaterial = VillageKitBuilder.BitsMat("DialSandGrain", Color.white, "", false);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 빛나는 시간 모래 몇 알
            var gl = new GameObject("SandGlint");
            gl.transform.SetParent(go.transform, false);
            var ps2 = gl.AddComponent<ParticleSystem>();
            var m2 = ps2.main;
            m2.loop = true; m2.prewarm = true;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.2f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            m2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.5f), new Color(1f, 0.95f, 0.75f));
            m2.gravityModifier = 0.45f;
            m2.maxParticles = 20;
            m2.simulationSpace = ParticleSystemSimulationSpace.World;
            var em2 = ps2.emission; em2.rateOverTime = new ParticleSystem.MinMaxCurve(2f, 4f);
            var sh2 = ps2.shape; sh2.shapeType = ParticleSystemShapeType.Cone; sh2.angle = 4f; sh2.radius = 0.1f;
            var c2 = ps2.colorOverLifetime; c2.enabled = true; c2.color = FadeInOut(0.1f, 0.5f);
            var pr2 = gl.GetComponent<ParticleSystemRenderer>();
            pr2.renderMode = ParticleSystemRenderMode.Billboard;
            pr2.sharedMaterial = VillageKitBuilder.BitsMat("DreamMote", Color.white, "spark.png");
        }

        /// <summary>섬 가장자리 둘레(실제 윤곽)를 따라 모래가 흘러넘쳐 아래로 쏟아지는 커튼 — 윤곽선 고리 메시의 모서리에서 내보냄</summary>
        static void RimSandCurtain(Transform parent, Transform isle, string key)
        {
            var outline = isle.GetComponent<CatTang.IsleOutline>();
            if (!outline || outline.points == null || outline.points.Length < 3) return;
            var pts = outline.points;
            // 윤곽을 2% 바깥으로 · 높이 −0.35 얇은 띠 메시 (방출용, 보이지 않음)
            var vs = new List<Vector3>(); var tri = new List<int>();
            foreach (var p in pts) { var v = new Vector3(p.x, 0, p.y) * 1.02f; vs.Add(v + Vector3.down * 0.35f); vs.Add(v + Vector3.down * 0.4f); }
            for (int i = 0; i < pts.Length; i++) { int j = (i + 1) % pts.Length; tri.AddRange(new[] { 2 * i, 2 * j, 2 * i + 1, 2 * j, 2 * j + 1, 2 * i + 1 }); }
            var m = new Mesh { name = "DialRim_" + key };
            m.SetVertices(vs); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
            string mp = "Assets/Art/Env/Village/DialRim_" + key + ".asset";
            AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(m, mp);
            float perim = 0; for (int i = 0; i < pts.Length; i++) perim += (pts[(i + 1) % pts.Length] - pts[i]).magnitude;

            var go = new GameObject("RimSand");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(SandA, SandB);
            main.gravityModifier = 0.4f;
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = perim * 3.2f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Mesh; sh.meshShapeType = ParticleSystemMeshShapeType.Edge; sh.mesh = m;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.radial = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);              // 가장자리에서 살짝 바깥으로 넘쳐 떨어짐
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(-0.15f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.1f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeInOut(0.1f, 0.55f);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.06f; pr.lengthScale = 1.2f;
            pr.sharedMaterial = VillageKitBuilder.BitsMat("DialSandGrain", Color.white, "", false);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static readonly HashSet<string> MouthJunk = new() { "Edge_A", "Edge_B", "Edge_C", "Lamp", "Crates", "Barrels", "Bench", "GrassTuft", "Tumbleweed" };

        /// <summary>다리 양 끝 입구(허공 쪽 1.75m ~ 섬 안 4.25m, 폭 ±2.9m)에 걸친 장식 · 가장자리 바위 · 다리 밑받침 · 가장자리 판정을 지움
        /// (2026-11-26 사용자: 다리 진입로를 막거나 시각적으로 헷갈리게 하는 것 치움). 지운 자리 옆은 난간 끝에서 섬 안쪽으로 짧은 벽을 이어 떨어지지 않게</summary>
        static void ClearMouths(Transform isles, Transform bridges)
        {
            Physics.SyncTransforms();
            int removed = 0;
            foreach (Transform br in bridges)
            {
                var deck = br.Find("Deck") ? br.Find("Deck").GetComponent<BoxCollider>() : null;
                if (!deck) continue;
                float half = deck.size.z * 0.5f;
                var ends = new[] { (end: br.position - br.forward * half, inward: -br.forward), (end: br.position + br.forward * half, inward: br.forward) };
                var inv = Quaternion.Inverse(br.rotation);
                foreach (var (end, inward) in ends)
                {
                    var c = end + inward * 1.25f;
                    var kill = new List<GameObject>();
                    foreach (var t in isles.GetComponentsInChildren<Transform>(true))
                    {
                        if (!MouthJunk.Contains(t.name)) continue;
                        // 장식은 렌더러 하나라도 입구 통로(±3.4 × 깊이 3.5)에 걸치면 지움
                        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                        {
                            var lp = inv * (r.bounds.center - c); var e = r.bounds.extents;
                            if (Mathf.Abs(lp.x) < 3.4f + e.x && Mathf.Abs(lp.z) < 3.5f + e.z) { kill.Add(t.gameObject); break; }
                        }
                    }
                    // 가장자리 판정(Bound)은 걸어가는 줄(폭 ±1.4, 허공 0.5 ~ 섬 안 4m)에 닿는 것만 지움 — 옆은 남겨 떨어지지 않게
                    foreach (var col in Physics.OverlapBox(end + inward * 1.75f + Vector3.up * 1.2f, new Vector3(1.4f, 1f, 2.25f), br.rotation))
                        if (col.name == "Bound" && col.transform.IsChildOf(isles)) kill.Add(col.gameObject);
                    foreach (var g in kill) if (g) { Object.DestroyImmediate(g); removed++; }
                    foreach (float sx in new[] { -1.75f, 1.75f })                       // 난간 끝 → 섬 안쪽으로 짧은 벽 (입구 모서리로 떨어지지 않게)
                    {
                        var w = new GameObject("MouthWall").AddComponent<BoxCollider>();
                        w.transform.SetParent(br, false);
                        w.transform.position = end + inward * 0.4f + br.right * sx + Vector3.up * 1.5f;
                        w.transform.rotation = br.rotation;
                        w.size = new Vector3(0.5f, 3f, 3.4f);
                    }
                }
            }
            Debug.Log($"[StoryDial] 다리 입구 정리 {removed}개");
        }

        static Transform Mark(Transform parent, string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            return t;
        }

        /// <summary>마을 다리 키트 + 바닥 · 난간 판정 (잠금 없음)</summary>
        static void Bridge(Transform parent, string name, Vector3 a, Vector3 b)
        {
            var root = VillageKitBuilder.Group(name, parent);
            root.localPosition = (a + b) * 0.5f;
            var dir = b - a; float len = dir.magnitude; dir /= len;
            root.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            var whole = VillageKitBuilder.V("Bridge", root, Vector3.zero, 0, 1f, false);
            if (whole) { var sc = whole.transform.localScale; whole.transform.localScale = new Vector3(sc.x, sc.y, sc.z * len / 10f); }
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
        }

        /// <summary>시계탑 카메라 쪽 문자판에 11:59 바늘. FaceAxis(앞 = 문자판 바깥) 아래 로컬 z 회전, 0° = 12시</summary>
        static void PlazaClock(Transform root, Transform tower)
        {
            var plaza = new GameObject("PlazaClock").transform;
            plaza.SetParent(root, false);
            plaza.localPosition = tower ? Effects.Flat(tower.localPosition) : Vector3.zero;
            bool ok = Ch4QuestBuilder.FindFace(tower, out var facePos, out var faceNormal, out float faceR);
            if (!ok) { facePos = plaza.position + new Vector3(0, 4.4f, -1.0f); faceNormal = Vector3.back; faceR = 0.6f; Debug.LogWarning("[StoryDial] 시계탑 문자판 못 찾음 — 기본 자리"); }
            var axis = new GameObject("FaceAxis").transform;
            axis.SetParent(plaza, true);
            axis.position = facePos;
            axis.rotation = Quaternion.LookRotation(faceNormal, Vector3.up);
            var hand = AssetDatabase.LoadAssetAtPath<GameObject>(Ch4QuestBuilder.HandPrefab);
            if (!hand) { Debug.LogWarning("[StoryDial] 바늘 키트 없음"); return; }
            var mh = Ch4QuestBuilder.MakeHand(hand, axis, "MinuteHand", facePos + faceNormal * 0.12f, faceNormal, faceR * 0.86f, 0.9f);
            var hh = Ch4QuestBuilder.MakeHand(hand, axis, "HourHand", facePos + faceNormal * 0.07f, faceNormal, faceR * 0.55f, 1.25f);
            mh.localRotation = Quaternion.Euler(0, 0, -59 * 6f);
            hh.localRotation = Quaternion.Euler(0, 0, -(11 + 59 / 60f) * 30f);
        }

        /// <summary>허공 아래 거대한 바늘 둘 (위에서 본 시계판 = 섬 배치), 11:59 → 엔딩 끝에 12:00</summary>
        static void DialHands(Transform root)
        {
            var g = VillageKitBuilder.Group("DialHands", root);
            g.localPosition = new Vector3(0, -6.5f, 0);
            void Hand(string name, float clockDeg, float length, float width)
            {
                var pivot = VillageKitBuilder.Group(name, g);
                pivot.localRotation = Quaternion.Euler(0, clockDeg, 0);
                var h = VillageKitBuilder.V("ClockHand_Float", pivot, Vector3.zero, 0, 1f, false);
                if (!h) return;
                float k = length / 5.6f;
                h.transform.localScale = new Vector3(h.transform.localScale.x * k * width, h.transform.localScale.y * k, h.transform.localScale.z * k);
                h.transform.localPosition = new Vector3(0, 0, length * 0.5f - 1.2f * k);
                foreach (var r in h.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Hand("GiantHour", (11 + 59 / 60f) * 30f, 22f, 1.25f);
            Hand("GiantMinute", 59 * 6f, 36f, 0.9f);
        }

        /// <summary>시계판 둘레에 떠 있는 숫자 돌 12개 (섬 자리 숫자는 Numeral_h — 섬이 무너질 때 같이 떨어짐)</summary>
        static void Numerals(Transform root)
        {
            var g = VillageKitBuilder.Group("Numerals", root);
            for (int h = 1; h <= 12; h++)
            {
                var d = HourDir(h);
                var n = VillageKitBuilder.V("NumeralStones", g, d * (DialR + 13.5f) + Vector3.down * 3.5f, YawTo(d) + 180f, 2.2f, false);
                if (!n) continue;
                n.name = "Numeral_" + h;
                VillageKitBuilder.Float(n, 0.35f, 9f + h * 0.4f, 0f, 1.5f);
            }
        }

        /// <summary>섬 아래 새벽 별밤 허공 + 멀리 떠 있는 조각 섬 · 부스러기</summary>
        static void Sky(Transform root, System.Random rnd)
        {
            var voidGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            voidGo.name = "DawnVoid";
            Object.DestroyImmediate(voidGo.GetComponent<Collider>());
            voidGo.transform.SetParent(root, false);
            voidGo.transform.localPosition = new Vector3(0, -40f, 25f);
            voidGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
            voidGo.transform.localScale = new Vector3(320, 320, 1);
            var mr = voidGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Env/Village/Void_Village.mat");
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var floats = VillageKitBuilder.Group("FloatingIsles", root);
            for (int i = 0; i < 14; i++)
            {
                float a = (i + (float)rnd.NextDouble() * 0.6f) / 14f * Mathf.PI * 2f, r = 52f + (float)rnd.NextDouble() * 16f;
                string kind = i % 3 == 0 ? "FloatIsle_M" : "FloatIsle_S";
                VillageKitBuilder.V(kind, floats, new Vector3(Mathf.Sin(a) * r, -4f - (float)rnd.NextDouble() * 10f, Mathf.Cos(a) * r), rnd.Next(360), 0.9f + (float)rnd.NextDouble() * 0.4f, false);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 16f + (float)rnd.NextDouble() * 30f;
                var p = new Vector3(Mathf.Sin(a) * r, 1f + (float)rnd.NextDouble() * 2f, Mathf.Cos(a) * r);
                bool onIsle = Effects.Flat(p).magnitude < 13f;
                foreach (var s in Stops)
                {
                    var d = s.isle.c.normalized; float along = Vector3.Dot(Effects.Flat(p), d);
                    if (Effects.Flat(p - s.isle.c).magnitude < 12f || (along > 0 && along < DialR && (Effects.Flat(p) - d * along).magnitude < 5f)) onIsle = true;   // 섬 · 다리 위는 비움
                }
                if (onIsle) continue;
                VillageKitBuilder.V("Debris", floats, p, rnd.Next(360), 1f + (float)rnd.NextDouble() * 0.4f, false);
            }
            foreach (Transform f in floats)
                VillageKitBuilder.Float(f.gameObject, f.name == "Debris" ? 0.45f : 0.25f, f.name == "Debris" ? 6f : 11f, f.name == "Debris" ? 4f : 0f, f.name == "Debris" ? 4f : 0.8f);
        }
    }
}
