// 2장 해금 탄 · 총 이펙트 프리팹 9종 조립 + 편집 모드 확인 촬영 (2026-11-25)
// - 메시: Art/tools/blender_fx_ch2unlock.py → Resources/FX/Meshes/fx_c2_* (CatTang/FX 메시 가져오기)
// - 플립북: bk_c2steam (Art/tools/blender_bake_ch2unlock.py) · bk_c2lanternpool (Art/tools/fx_ch2_lanternpool.py)
// - 결과: Resources/FX/Prefabs/FX_LingerWraith … FX_SteamBlast (FxEffect 루트 + FxPart 조각). 게임은 FxEffect.Spawn(name, pos, rot, scale, tint)
//   스케일 1 = 명세(Art/tools/specs/ch2_unlock_spec.md '이펙트')의 반경 · 길이, +Z = 진행 방향
// - 촬영: Capture(...) — 플레이 모드 없이 FxPart와 같은 곡선으로 시간을 돌려 게임 카메라 각도에서 프레임 PNG로 (FxImpactBaker와 같은 방식: 숨긴 오브젝트 · 레이어 31)
// 메뉴: CatTang/FX 2장 해금 프리팹 만들기
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = CatTang.FxMesh.Kind;

namespace CatTang.EditorTools
{
    public static class Ch2UnlockFxBuilder
    {
        const string PrefabDir = "Assets/Resources/FX/Prefabs/";
        const string MatDir = "Assets/Resources/FX/Mats/";
        public static readonly string[] Names = { "FX_LingerWraith", "FX_LockTransfer", "FX_TrainShot", "FX_TrainEnd", "FX_SmokeRing", "FX_RailSpark", "FX_LanternField", "FX_SteamDash", "FX_SteamBlast" };

        // ---------- 재질 (CatTang/ToonFire: 정면 심지 → 뜨거운 층 → 가운데 → 테 → 식은 색, 잉크 외곽선) ----------
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static Material ToonMat(string name, string core, string hot, string mid, string edge, string ash, string ink, float intensity, float outline)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("CatTang/ToonFire")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("CatTang/ToonFire");
            m.SetColor("_ColCore", H(core)); m.SetColor("_ColHot", H(hot)); m.SetColor("_ColMid", H(mid));
            m.SetColor("_ColEdge", H(edge)); m.SetColor("_ColAsh", H(ash)); m.SetColor("_OutlineColor", H(ink));
            m.SetFloat("_Intensity", intensity); m.SetFloat("_OutlineWidth", outline);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material AuraMat(string name, string col, string core, float intensity, float inflate, float rimPow, float wobble)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("CatTang/GhostAura")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("CatTang/GhostAura");
            m.SetColor("_Color", H(col)); m.SetColor("_CoreColor", H(core));
            m.SetFloat("_Intensity", intensity); m.SetFloat("_Inflate", inflate); m.SetFloat("_RimPower", rimPow); m.SetFloat("_Wobble", wobble);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material fire, sprite, wraith, steam, soot, brass, iron, lantern, lockm, auraIn, auraOut, ghostBody;
        static void Mats()
        {
            fire = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "FxFire.mat");
            sprite = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "FxSprite.mat");
            // 미련 원혼: 남보라 쪽 푸른빛 (#6F7CFF) — 망각 청백 · 영원 청록과 구분
            wraith = ToonMat("FxC2Wraith", "#EEF0FF", "#B4BCFF", "#6F7CFF", "#3E46C4", "#1E1C52", "#0D0B2A", 1.7f, 1.6f);
            // 기관차 증기: 흰 → 푸른 회색 (식으면 회청)
            steam = ToonMat("FxC2Steam", "#FFFFFF", "#F1F4F7", "#CBD4DC", "#8D9AA7", "#5B6672", "#27303A", 1.0f, 1.6f);
            // 굴뚝 연기: 따뜻한 회색 → 그을음
            soot = ToonMat("FxC2Soot", "#DCD5CF", "#B8B0AA", "#8F8783", "#635C58", "#3A3533", "#161212", 1.0f, 1.8f);
            // 놋쇠 (랜턴 틀 · 시곗바늘)
            brass = ToonMat("FxC2Brass", "#FFF3C8", "#EDC870", "#BA8A33", "#70491B", "#3C2610", "#1C1006", 1.15f, 1.4f);
            // 사슬 쇠
            iron = ToonMat("FxC2Iron", "#EEF1F6", "#BCC2CE", "#848B9A", "#4C5260", "#2A2D35", "#0E0F13", 1.05f, 1.4f);
            // 가스등 불빛: 크림 → 호박 → 짙은 주황
            lantern = ToonMat("FxC2Lantern", "#FFF9E2", "#FFD884", "#FFA941", "#D9691E", "#6A3010", "#2A1006", 1.9f, 1.4f);
            // 가둠 섬광: 차가운 청백 (탄 색이 오면 덮어씀)
            // 원혼 오라 (2026-11-25 사용자: "주변에 오라처럼 광 효과"): 몸 껍데기 프레넬 빛 — 안쪽 좁고 밝게 · 바깥 넓고 흐리게
            auraIn = AuraMat("FxC2WraithAuraIn", "#6C7CFF", "#B9C4FF", 1.0f, 0.035f, 2.6f, 0.008f);
            // 원혼 몸 (2026-11-25 사용자: "유령도 좀 반투명하게"): 원혼 색 그대로, 가운데 비치고 가장자리 진하게
            ghostBody = ToonMat("FxC2GhostBody", "#D6DCFF", "#A3AEFF", "#6F7CFF", "#3E46C4", "#1E1C52", "#0D0B2A", 1.15f, 1.6f);
            ghostBody.shader = Shader.Find("CatTang/GhostBody");
            ghostBody.SetFloat("_AlphaCore", 0.2f); ghostBody.SetFloat("_AlphaEdge", 0.7f); ghostBody.SetFloat("_OutlineAlpha", 0.55f);
            EditorUtility.SetDirty(ghostBody);
            auraOut = AuraMat("FxC2WraithAuraOut", "#4A52E0", "#8796FF", 0.45f, 0.1f, 1.8f, 0.012f);
            lockm = ToonMat("FxC2Lock", "#FFFFFF", "#DDF2FF", "#8FCBFF", "#3F7FD0", "#1C2F5A", "#0A1426", 1.8f, 1.2f);
            AssetDatabase.SaveAssets();
        }

        static Mesh M(string n)
        {
            var m = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/FX/Meshes/" + n + ".asset");
            if (!m) Debug.LogWarning("[Ch2UnlockFx] 메시 없음: " + n + " — CatTang/FX 메시 가져오기 먼저");
            return m;
        }

        // ---------- 조립 도구 ----------
        static GameObject root;
        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static Quaternion RQ() => Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360));

        static FxPart P(string name, Mesh mesh, Material mat, K kind, float life, float delay, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 drift, bool tint = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var p = go.AddComponent<FxPart>();
            p.kind = kind; p.life = life; p.delay = delay; p.drift = drift; p.useTint = tint; p.jitter = 0.12f;
            return p;
        }
        static FxPart P(string name, Mesh mesh, Material mat, K kind, float life, float delay, Vector3 pos, Vector3 euler, Vector3 scale, Vector3 drift, bool tint = false)
            => P(name, mesh, mat, kind, life, delay, pos, Quaternion.Euler(euler), scale, drift, tint);

        static FxPart Flip(string name, string set, float life, float delay, Vector3 pos, float size, bool flat = false)
        {
            var p = P(name, null, sprite, K.Core, life, delay, pos, Quaternion.identity, Vector3.one * size, Vector3.zero);
            p.flipSet = set; p.flat = flat; p.billboard = !flat;
            return p;
        }

        static Vector3 S(float s) => Vector3.one * s;
        static Quaternion Look(Vector3 d) => Quaternion.LookRotation(d.normalized, Mathf.Abs(d.normalized.y) > 0.95f ? Vector3.forward : Vector3.up);
        static readonly Vector3 Flat = new Vector3(90, 0, 0);
        static Mesh Ball(int i) => M("fx_ball_" + (i % 3));
        static Mesh Spike(int i) => M("fx_spike_" + (i % 3));

        [MenuItem("CatTang/FX 2장 해금 프리팹 만들기")]
        public static string BuildAll()
        {
            if (EditorApplication.isPlaying) return "플레이 중에는 만들지 않음";
            Mats();
            var scene = EditorSceneManager.NewPreviewScene();
            var log = new List<string>();
            try
            {
                foreach (var n in Names)
                {
                    rnd = new System.Random(n.GetHashCode() & 0xffff);
                    root = new GameObject(n);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                    var fx = root.AddComponent<FxEffect>();
                    fx.tintable = false;
                    switch (n)
                    {
                        case "FX_LingerWraith": LingerWraith(); break;
                        case "FX_LockTransfer": LockTransfer(); fx.tintable = true; break;
                        case "FX_TrainShot": TrainShot(); fx.tintable = true; break;
                        case "FX_TrainEnd": TrainEnd(); fx.tintable = true; break;
                        case "FX_SmokeRing": SmokeRing(); fx.tintable = true; break;
                        case "FX_RailSpark": RailSpark(); fx.tintable = true; break;
                        case "FX_LanternField": LanternField(); break;
                        case "FX_SteamDash": SteamDash(); break;
                        case "FX_SteamBlast": SteamBlast(); break;
                    }
                    float end = 0; int cnt = 0;
                    foreach (var p in root.GetComponentsInChildren<FxPart>()) { end = Mathf.Max(end, p.EndTime); cnt++; }
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + n + ".prefab");
                    Object.DestroyImmediate(root);
                    log.Add($"{n}: 조각 {cnt} · 끝 {end:0.00}s");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.SaveAssets();
            var s = string.Join("\n", log);
            Debug.Log("[Ch2UnlockFx]\n" + s);
            return s;
        }

        // ---------- 1. 미련 6단계 원혼 (반경 3): 땅에서 원혼 4이 차례로 피어오름(0~0.6) → 머리에서 소리 물결 · 바닥 파동 3겹(0.55~1.2) → 꼬리 불씨로 흩어짐(1.0~1.5) ----------
        static void LingerWraith()
        {
            var wr = M("fx_c2_ghost"); var ring = M("fx_ring"); var wisp = M("fx_wisp"); var wave = M("fx_c2_wave");
            var seep = P("Seep", ring, wraith, K.Ring, 0.5f, 0f, new Vector3(0, 0.05f, 0), Flat, new Vector3(2.4f, 2.4f, 3f), Vector3.zero);
            seep.jitter = 0;
            for (int i = 0; i < 4; i++)
            {
                float a = (25 + i * 90 + R(-22, 22)) * Mathf.Deg2Rad, rr = R(1.0f, 1.65f), h = R(1.7f, 2.0f);   // 2026-11-25: 1.25~1.5 → 1.7~2.0 (게임 화면에서 유령 모양이 읽히게)
                var at = new Vector3(Mathf.Sin(a) * rr, 0, Mathf.Cos(a) * rr);
                float d0 = i * 0.09f;
                // 원혼 몸: 원점(발치)에서 0.25 → 1.1배로 솟고 하얗게 타다 식으며 남색으로 녹음
                // 2026-11-25 사용자: 유령은 Hyper3D 모델(fx_c2_ghost, 두건 · 찢긴 자락 · 연기 꼬리)로 + 주변에 오라 광
                //   오라 껍데기 2겹은 몸과 같은 수명 · 회전 · 흐름(jitter 0)이라 몸에 딱 붙어 따라감 — 셰이더가 법선 방향으로 부풀림
                var euler = new Vector3(R(20, 30), 180 + R(-35, 35), R(-8, 8));   // 카메라 쪽으로 뒤로 젖힘 — 위에서 내려다봐도 두건 · 몸 · 손이 보이게
                float spin = R(-40, 40);
                var w = P("Wraith" + i, wr, ghostBody, K.Core, 1.6f, d0, at, euler, S(h), new Vector3(0, 1.8f, 0));
                w.spin = spin; w.spinAxis = Vector3.up; w.jitter = 0f;
                foreach (var (nm, am) in new[] { ("AuraIn", auraIn), ("AuraOut", auraOut) })
                {
                    var au = P(nm + i, wr, am, K.Core, 1.6f, d0, at, euler, S(h), new Vector3(0, 1.8f, 0));
                    au.spin = spin; au.spinAxis = Vector3.up; au.jitter = 0f;
                }
                // 오라 알갱이: 몸 둘레에서 빛 알갱이가 천천히 피어올라 위로 흩어짐
                for (int k = 0; k < 5; k++)
                {
                    float b = R(0, Mathf.PI * 2);
                    var off = new Vector3(Mathf.Cos(b) * 0.38f, R(0.2f, 0.9f), Mathf.Sin(b) * 0.38f) * h;
                    var mo = P($"AuraMote{i}_{k}", Ball(k), wraith, K.Ball, R(0.55f, 0.8f), d0 + 0.12f + k * 0.12f, at + off, RQ(), S(R(0.06f, 0.1f)), new Vector3(Mathf.Cos(b) * 0.4f, R(1.6f, 2.4f), Mathf.Sin(b) * 0.4f));
                    mo.jitter = 0.2f;
                }
                // 발치 고리: 솟는 순간 바닥이 출렁
                var foot = P("Foot" + i, ring, wraith, K.Ring, 0.4f, d0, at + Vector3.up * 0.04f, Flat, new Vector3(1.3f, 1.3f, 2.5f), Vector3.zero);
                foot.jitter = 0.1f;
                // 울림: 머리 높이에서 카메라를 보는 소리 물결 2겹
                for (int k = 0; k < 2; k++)
                {
                    var e = P($"Echo{i}_{k}", ring, wraith, K.Ring, 0.45f, 0.55f + d0 + k * 0.17f, at + Vector3.up * h * 0.78f, Quaternion.identity, new Vector3(1.2f + k * 0.7f, 1.2f + k * 0.7f, 2.2f), Vector3.zero);
                    e.billboard = true;
                }
                // 흩어짐: 꼬리 긴 불씨 3가닥이 바깥 · 위로 날아감 (굵은 머리가 앞, 꼬리는 뒤)
                for (int k = 0; k < 3; k++)
                {
                    var outv = new Vector3(at.x, 0, at.z).normalized;
                    var d = (outv * R(0.5f, 1f) + Vector3.up * R(0.7f, 1.2f) + new Vector3(R(-0.5f, 0.5f), 0, R(-0.5f, 0.5f))).normalized;
                    P($"Scatter{i}_{k}", wisp, wraith, K.Tracer, R(0.36f, 0.44f), 1.0f + d0 * 0.5f + k * 0.04f, at + Vector3.up * h * R(0.45f, 0.8f), Look(-d), new Vector3(1.1f, 1.1f, R(0.7f, 1f)), d * R(3.2f, 4.5f));
                }
            }
            // 바닥 파동 3겹: 반경 3까지 (지름 6)
            float[] wd = { 0.6f, 0.78f, 0.96f };
            for (int k = 0; k < 3; k++)
            {
                var g = P("Wave" + k, wave, wraith, K.Ring, 0.55f + k * 0.04f, wd[k], new Vector3(0, 0.06f, 0), Flat * 1 + new Vector3(0, 0, k * 37), new Vector3(6f - k * 0.25f, 6f - k * 0.25f, 7f - k * 1.5f), Vector3.zero);
                g.jitter = 0.03f;
            }
            // 흩어지는 빛 알갱이
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45 * Mathf.Deg2Rad + R(-0.3f, 0.3f);
                var d = new Vector3(Mathf.Sin(a), R(0.6f, 1.2f), Mathf.Cos(a));
                P("Mote" + k, Ball(k), wraith, K.Ball, 0.45f, 1.05f + R(0, 0.1f), new Vector3(Mathf.Sin(a), R(0.4f, 1.1f), Mathf.Cos(a)) * 1.2f, RQ(), S(R(0.12f, 0.2f)), d * 3f);
            }
        }

        // ---------- 2. 환승역 가둠 옮겨 붙음 (0.4초): 맞은 자리에서 시곗바늘 3개가 프로펠러처럼 돌며 튀고 끊어진 사슬 조각이 흩어짐. 앞(+Z) = 옮겨 붙는 쪽 ----------
        static void LockTransfer()
        {
            var hand = M("fx_c2_clockhand"); var link = M("fx_c2_chainlink"); var broken = M("fx_c2_chainbroken"); var ring = M("fx_ring");
            P("Flash", Ball(0), lockm, K.Core, 0.16f, 0, Vector3.zero, Quaternion.identity, S(0.65f), Vector3.zero, true);
            var snap = P("Snap", ring, lockm, K.Ring, 0.22f, 0, Vector3.zero, Quaternion.identity, new Vector3(1.7f, 1.7f, 3f), Vector3.zero, true);
            snap.billboard = true;
            float[] yaw = { R(-55, -25), R(-10, 15), R(30, 60) };
            for (int i = 0; i < 3; i++)
            {
                float a = yaw[i] * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), R(0.25f, 0.5f), Mathf.Cos(a));
                var h = P("Hand" + i, hand, brass, K.Ball, R(0.34f, 0.4f), R(0, 0.03f), d * 0.1f, Look(d), S(R(0.5f, 0.62f)), d * R(6f, 8f));
                h.spin = R(700, 1000) * (i % 2 == 0 ? 1 : -1); h.spinAxis = Vector3.up;
            }
            for (int i = 0; i < 5; i++)
            {
                var d = new Vector3(R(-1, 1), R(0.2f, 1f), R(-0.6f, 1f)).normalized;
                var c = P("Chain" + i, i % 2 == 0 ? broken : link, iron, K.Ball, R(0.3f, 0.4f), R(0, 0.04f), d * 0.12f, RQ(), S(R(0.45f, 0.6f)), d * R(5f, 7f));
                c.spin = R(500, 900); c.spinAxis = new Vector3(R(-1, 1), R(-1, 1), R(-1, 1)).normalized;
            }
            for (int i = 0; i < 6; i++)
            {
                var d = new Vector3(R(-1, 1), R(-0.2f, 0.8f), R(-0.4f, 1f)).normalized;
                P("Spark" + i, Spike(i), lockm, K.Tracer, R(0.16f, 0.22f), 0, d * 0.1f, Look(d), new Vector3(0.07f, 0.07f, R(0.4f, 0.6f)), d * R(8f, 11f), true);
            }
        }

        // ---------- 3. 마지막 기차 탄 (탄을 따라다님 · 2초 버팀): 굵은 증기 탄두(홈이 돌며 감김) + 보일러 불씨 + 0.25초마다 '칙' 증기 고리 + 0.1초마다 연기 뭉게 꼬리 ----------
        static void TrainShot()
        {
            var head = P("Head", M("fx_c2_trainhead"), steam, K.Core, 2.0f, 0, new Vector3(0, 0, 0.15f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.58f), Vector3.zero);
            head.hold = true; head.holdHeat = 0.85f; head.spin = 260; head.spinAxis = Vector3.forward; head.jitter = 0;
            var core = P("Boiler", Ball(1), fire, K.Core, 2.0f, 0, new Vector3(0, 0, 0.38f), Quaternion.identity, S(0.24f), Vector3.zero, true);
            core.hold = true; core.holdHeat = 1f; core.blink = 5f; core.jitter = 0;
            var bow = P("Bow", M("fx_c2_steamcone"), steam, K.Core, 2.0f, 0, new Vector3(0, 0, 0.5f), Quaternion.identity, new Vector3(0.62f, 0.62f, 0.32f), Vector3.zero);
            bow.hold = true; bow.holdHeat = 0.7f; bow.spin = -320; bow.spinAxis = Vector3.forward; bow.jitter = 0;
            for (int k = 0; k < 8; k++)
            {
                var c = P("Chuff" + k, M("fx_c2_smokering"), steam, K.Ring, 0.45f, k * 0.25f, new Vector3(0, 0.02f, -0.08f), new Vector3(0, 0, R(0, 360)), new Vector3(0.85f, 0.85f, 0.9f), new Vector3(0, 0.4f, -3.6f));
                c.jitter = 0.08f;
            }
            for (int k = 0; k < 28; k++)
            {
                bool dark = k % 3 != 1;
                P("Smoke" + k, Ball(k), dark ? soot : steam, K.Ball, R(0.5f, 0.62f), k * 0.07f, new Vector3(R(-0.06f, 0.06f), R(0.05f, 0.15f), -0.2f), RQ(),
                    S(R(0.3f, 0.42f)), new Vector3(R(-0.9f, 0.9f), R(1.4f, 2.2f), -R(4f, 5f)));
            }
        }

        // ---------- 4. 마지막 기차 끝 공명 폭발 (반경 4): 섬광 → 기적처럼 '뿌-뿌---' 퍼지는 바닥 고리 3겹 + 머리 높이 소리 물결 + 증기 기둥 · 바닥 증기 · 증기 분사 ----------
        static void TrainEnd()
        {
            var ring = M("fx_ring");
            P("Flash", Ball(0), fire, K.Core, 0.3f, 0, new Vector3(0, 0.6f, 0), Quaternion.identity, S(2.6f), Vector3.up * 1f, true);
            // 기적 '뿌-뿌---': 짧은 두 겹이 바짝 붙어 퍼지고, 한 박자 쉬고 길게 한 겹 (가는 물결 고리 · 큰 반경에서도 띠가 얇음)
            var wave = M("fx_c2_wave");
            P("Whistle0", wave, fire, K.Ring, 0.42f, 0f, new Vector3(0, 0.08f, 0), Flat, new Vector3(8f, 8f, 9f), Vector3.zero, true).jitter = 0.02f;
            P("Whistle0b", wave, fire, K.Ring, 0.42f, 0.05f, new Vector3(0, 0.08f, 0), Flat + new Vector3(0, 0, 15), new Vector3(7.4f, 7.4f, 7f), Vector3.zero, true).jitter = 0.02f;
            P("Whistle1", wave, steam, K.Ring, 0.45f, 0.16f, new Vector3(0, 0.1f, 0), Flat + new Vector3(0, 0, 30), new Vector3(8.2f, 8.2f, 8f), Vector3.zero).jitter = 0.02f;
            P("Whistle1b", wave, steam, K.Ring, 0.45f, 0.21f, new Vector3(0, 0.1f, 0), Flat + new Vector3(0, 0, 45), new Vector3(7.6f, 7.6f, 6f), Vector3.zero).jitter = 0.02f;
            P("Whistle2", wave, fire, K.Ring, 0.65f, 0.42f, new Vector3(0, 0.08f, 0), Flat + new Vector3(0, 0, 60), new Vector3(8f, 8f, 10f), Vector3.zero, true).jitter = 0.02f;
            P("Tread", ring, steam, K.Ring, 0.35f, 0f, new Vector3(0, 0.06f, 0), Flat, new Vector3(3.2f, 3.2f, 3f), Vector3.zero);
            for (int k = 0; k < 3; k++)
            {
                var e = P("Sound" + k, ring, steam, K.Ring, 0.36f, 0.04f + k * 0.11f, new Vector3(0, 1.3f, 0), Quaternion.identity, S(2.4f + k * 1.2f), Vector3.zero);
                e.billboard = true; e.transform.localScale = new Vector3(2.4f + k * 1.2f, 2.4f + k * 1.2f, 2f);
            }
            for (int k = 0; k < 3; k++)
            {
                var f = Flip("Column" + k, "bk_c2steam", 0.9f, 0.04f + k * 0.05f, k == 0 ? new Vector3(0, 0.9f, 0) : new Vector3(k == 1 ? -0.9f : 0.9f, 0.5f, 0.3f), k == 0 ? 3.4f : 2.4f);
                f.pivotY = 0.35f; f.flipGrow = 1.35f;
            }
            for (int k = 0; k < 9; k++)
            {
                float a = (k * 40 + R(-12, 12)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                P("Ground" + k, Ball(k), steam, K.Ball, R(0.6f, 0.8f), R(0.02f, 0.08f), d * 0.4f + Vector3.up * 0.3f, RQ(), S(R(0.9f, 1.25f)), d * R(15f, 18f) + Vector3.up * 1.5f);
            }
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60 + 30 + R(-10, 10)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), R(0.08f, 0.3f), Mathf.Cos(a));
                P("Jet" + k, Spike(k), steam, K.Spike, 0.4f, 0.02f, Vector3.up * 0.4f, Look(d), new Vector3(0.45f, 0.45f, R(2.4f, 2.9f)), d * 3f);
            }
        }

        // ---------- 5. 연기 고리 처치 폭발 (반경 3, 0.7초): 짧은 섬광 → 굴뚝 연기 도넛이 수평으로 굴러 퍼지고 안쪽 증기 도넛이 반대로 돎 + 고리를 따라 밀려 나가는 뭉게 ----------
        static void SmokeRing()
        {
            var sr = M("fx_c2_smokering");
            P("Flash", Ball(2), fire, K.Core, 0.2f, 0, new Vector3(0, 0.5f, 0), Quaternion.identity, S(1.3f), Vector3.zero, true);
            var a = P("Ring", sr, soot, K.Ring, 0.7f, 0, new Vector3(0, 0.45f, 0), Flat, new Vector3(6f, 6f, 5f), Vector3.zero);
            a.spin = 90; a.spinAxis = Vector3.forward; a.jitter = 0.03f;
            var b = P("InnerRing", sr, steam, K.Ring, 0.6f, 0.05f, new Vector3(0, 0.6f, 0), Flat + new Vector3(0, 0, 40), new Vector3(4.2f, 4.2f, 3.6f), Vector3.zero);
            b.spin = -130; b.spinAxis = Vector3.forward; b.jitter = 0.03f;
            for (int k = 0; k < 10; k++)
            {
                float t = (k * 36 + R(-10, 10)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t));
                P("Roll" + k, Ball(k), soot, K.Ball, R(0.5f, 0.62f), R(0, 0.05f), d * 0.4f + Vector3.up * 0.45f, RQ(), S(R(0.45f, 0.65f)), d * R(14f, 16f));
            }
            for (int k = 0; k < 3; k++)
                P("Chimney" + k, Ball(k), k == 1 ? steam : soot, K.Ball, 0.65f, 0.04f * k, new Vector3(R(-0.2f, 0.2f), 0.6f, R(-0.2f, 0.2f)), RQ(), S(R(0.7f, 0.9f)), new Vector3(0, R(5f, 7f), 0));
        }

        // ---------- 6. 레일 위의 불꽃 치명 폭발 (반경 2, 0.4초): 바닥에 레일 두 줄 긁힌 자국이 앞(+Z)으로 쭉 그어지며 불똥이 그쪽 부채꼴로만 튐 ----------
        static void RailSpark()
        {
            var scrape = M("fx_c2_scrape");
            P("Flash", Ball(0), fire, K.Core, 0.14f, 0, new Vector3(0, 0.3f, 0), Quaternion.identity, S(0.85f), Vector3.zero, true);
            for (int s = -1; s <= 1; s += 2)
            {
                var g = P("Scrape" + (s < 0 ? "L" : "R"), scrape, fire, K.Spike, 0.4f, s < 0 ? 0f : 0.02f, new Vector3(s * 0.16f, 0.03f, -0.35f), new Vector3(0, s * 3, 0), new Vector3(1.5f, 1f, 2.2f), Vector3.zero);
                g.jitter = 0.05f;
            }
            for (int k = 0; k < 18; k++)
            {
                float yaw = R(-35, 35) * Mathf.Deg2Rad, el = R(5, 40) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(yaw) * Mathf.Cos(el));
                P("Spark" + k, Spike(k), fire, K.Tracer, R(0.22f, 0.32f), R(0, 0.06f), new Vector3(R(-0.15f, 0.15f), 0.15f, R(-0.1f, 0.2f)), Look(d), new Vector3(0.05f, 0.05f, R(0.5f, 1f)), d * R(6f, 9f));
            }
            for (int k = 0; k < 6; k++)
            {
                var d = new Vector3(R(-0.5f, 0.5f), R(0.3f, 0.8f), 1f).normalized;
                P("Drop" + k, Ball(k), fire, K.Ball, 0.35f, R(0, 0.05f), new Vector3(0, 0.2f, 0), RQ(), S(R(0.08f, 0.12f)), d * R(4f, 6f));
            }
        }

        // ---------- 7. 랜턴 장판 (반경 4, 4초): 떨어진 랜턴에서 호박색 빛 웅덩이가 확 켜짐(빗방울 물결 일렁임) · 불티가 피어오름 → 끝에 지직거리다 꺼지며 연기 한 줌 ----------
        static void LanternField()
        {
            var pool = Flip("Pool", "bk_c2lanternpool", 4f, 0, new Vector3(0, 0.03f, 0), 8f, true);
            pool.flipGrow = 1f; pool.jitter = 0; pool.opacity = 0.9f;
            var tilt = Quaternion.Euler(0, 30, 22);
            var fr = P("LanternFrame", M("fx_c2_lantern_frame"), brass, K.Core, 4f, 0, Vector3.zero, tilt, S(1.3f), Vector3.zero);
            fr.hold = true; fr.holdHeat = 0.75f; fr.jitter = 0;
            var gl = P("LanternGlass", M("fx_c2_lantern_glass"), lantern, K.Core, 4f, 0, Vector3.zero, tilt, S(1.3f), Vector3.zero);
            gl.hold = true; gl.holdHeat = 1f; gl.jitter = 0;
            P("Land", M("fx_ring"), lantern, K.Ring, 0.4f, 0, new Vector3(0, 0.06f, 0), Flat, new Vector3(8f, 8f, 3f), Vector3.zero).jitter = 0.02f;
            P("Burst", Ball(0), lantern, K.Core, 0.22f, 0, new Vector3(0, 0.4f, 0), Quaternion.identity, S(1.1f), Vector3.zero);
            for (int k = 0; k < 8; k++)
            {
                float a = (k * 45 + R(-15, 15)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), R(0.3f, 0.8f), Mathf.Cos(a));
                P("Glass" + k, Spike(k), lantern, K.Tracer, R(0.2f, 0.28f), 0, new Vector3(0, 0.35f, 0), Look(d), new Vector3(0.06f, 0.06f, R(0.35f, 0.55f)), d * R(5f, 8f));
            }
            for (int k = 0; k < 12; k++)
            {
                float a = R(0, Mathf.PI * 2), r = Mathf.Sqrt(R(0.05f, 1f)) * 3.4f;
                var e = P("Ember" + k, Ball(k), lantern, K.Tracer, R(1.0f, 1.3f), 0.4f + k * 0.22f + R(0, 0.1f), new Vector3(Mathf.Sin(a) * r, 0.1f, Mathf.Cos(a) * r), Quaternion.Euler(-90, 0, 0), new Vector3(0.09f, 0.09f, 0.2f), new Vector3(R(-0.1f, 0.1f), R(0.8f, 1.1f), 0));
                e.jitter = 0.2f;
            }
            for (int k = 0; k < 2; k++)
                P("OutPuff" + k, Ball(k), soot, K.Ball, 0.6f, 3.5f + k * 0.12f, tilt * new Vector3(0, 0.85f, 0) * 1.3f, RQ(), S(0.32f + k * 0.08f), new Vector3(R(-0.3f, 0.3f), 2.4f, 0));
        }

        // ---------- 8. 증기 돌진 꼬리 (길이 1 = 돌진 거리, 원점 = 출발점, +Z = 돌진 방향): 증기 줄기가 앞으로 쭉 그어지고 길 따라 뭉게가 시차를 두고 옆으로 밀려 나감 ----------
        static void SteamDash()
        {
            var st = P("Streak", Spike(0), steam, K.Spike, 0.35f, 0, new Vector3(0, 0.45f, 0), Quaternion.identity, new Vector3(0.14f, 0.1f, 1f), Vector3.zero);
            st.jitter = 0;
            for (int k = 0; k < 12; k++)
            {
                float z = k / 11f;
                float side = k % 2 == 0 ? 1 : -1;
                P("Puff" + k, Ball(k), steam, K.Ball, R(0.5f, 0.6f), z * 0.15f, new Vector3(R(-0.03f, 0.03f), R(0.3f, 0.42f), z), RQ(), S(R(0.16f, 0.24f)), new Vector3(side * R(0.6f, 1.1f), R(0.3f, 0.7f), -0.3f));
            }
            for (int k = 0; k < 4; k++)
            {
                float z = 0.15f + k * 0.25f;
                var f = Flip("Wisp" + k, "bk_c2steam", 0.6f, z * 0.15f + 0.03f, new Vector3(0, 0.35f, z), 0.45f);
                f.pivotY = 0.3f;
            }
            for (int k = 0; k < 3; k++)
            {
                float z = 0.25f + k * 0.27f;
                P("Line" + k, Spike(k), steam, K.Tracer, 0.25f, z * 0.15f, new Vector3(k == 1 ? 0.1f : -0.1f, 0.3f + k * 0.12f, z), Quaternion.Euler(0, 180, 0), new Vector3(0.03f, 0.03f, 0.35f), new Vector3(0, 0, -1.4f));
            }
        }

        // ---------- 9. 증기 돌진 끝 폭발 (반경 3, 0.6초): 보일러 불씨가 펑 → 흰 증기 덩어리가 터지며 바닥 증기 고리 · 방사 뭉게 · 증기 분사 ----------
        static void SteamBlast()
        {
            P("Boiler", Ball(1), fire, K.Core, 0.22f, 0, new Vector3(0, 0.5f, 0), Quaternion.identity, S(1.4f), Vector3.zero);
            P("Burst", Ball(0), steam, K.Core, 0.4f, 0.02f, new Vector3(0, 0.5f, 0), Quaternion.identity, S(2.3f), Vector3.up * 1.2f);
            var g = P("GroundRing", M("fx_c2_smokering"), steam, K.Ring, 0.5f, 0, new Vector3(0, 0.2f, 0), Flat, new Vector3(6f, 6f, 4f), Vector3.zero);
            g.spin = 120; g.spinAxis = Vector3.forward; g.jitter = 0.03f;
            for (int k = 0; k < 10; k++)
            {
                float a = (k * 36 + R(-12, 12)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                P("Puff" + k, Ball(k), steam, K.Ball, R(0.48f, 0.56f), R(0, 0.05f), d * 0.3f + Vector3.up * 0.35f, RQ(), S(R(0.8f, 1.1f)), d * R(13f, 15f) + Vector3.up * 1f);
            }
            for (int k = 0; k < 8; k++)
            {
                float a = (k * 45 + 22 + R(-10, 10)) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Sin(a), R(0.08f, 0.45f), Mathf.Cos(a));
                P("Jet" + k, Spike(k), steam, K.Spike, 0.35f, 0.01f, Vector3.up * 0.4f, Look(d), new Vector3(0.4f, 0.4f, R(2.1f, 2.6f)), d * 2f);
            }
            for (int k = 0; k < 2; k++)
            {
                var f = Flip("Cloud" + k, "bk_c2steam", 0.6f, 0.04f, new Vector3(k == 0 ? -0.5f : 0.5f, 0.6f, 0), 2.6f);
                f.pivotY = 0.35f; f.flipGrow = 1.3f;
            }
        }

        // =====================================================================================
        // 편집 모드 확인 촬영: FxPart.Pose와 같은 규칙으로 시간을 돌려 게임 카메라 각도(피치 56.3° · 시야 40°)에서 PNG 프레임
        // =====================================================================================
        static readonly Vector3 Origin = new Vector3(0, -800, 0);
        const int Layer = 31;

        class Sim
        {
            public FxPart p; public MeshRenderer mr; public MeshFilter mf; public MaterialPropertyBlock mpb = new();
            public Vector3 baseLocal, baseScale, drift, acc; public Quaternion baseRot; public Material[] frames; public float roll, seed;
        }

        static Mesh quad;
        static Mesh Quad()
        {
            if (quad) return quad;
            quad = new Mesh { name = "CapQuad", hideFlags = HideFlags.HideAndDontSave };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return quad;
        }

        static GameObject Hidden(string n, Transform parent = null)
        {
            var g = new GameObject(n) { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
            if (parent) g.transform.SetParent(parent, false);
            return g;
        }

        /// <summary>name 프리팹을 dur초 동안 fps로 찍어 outDir/f_000.png …. dist = 게임 카메라 거리 배율(1 = 게임 기본 20m) · focus = 카메라가 보는 점(루트 기준) · guide = 바닥 안내 원 반경(m) · yaw = 루트 방향 · rootY = 루트 높이(맞은 자리 · 날아가는 탄)</summary>
        public static string Capture(string name, float dur, int fps, string outDir, float dist, Vector3 focus, float guide, float yaw = 0f, float scale = 1f, float rootY = 0f, int w = 480, int h = 360)
        {
            if (EditorApplication.isPlaying) return "플레이 중에는 찍지 않음";
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab");
            if (!pf) return "프리팹 없음 " + name;
            Directory.CreateDirectory(outDir);
            foreach (var old in Directory.GetFiles(outDir, "f_*.png")) File.Delete(old);
            var made = new List<Object>();
            var stage = Hidden("CapStage");
            stage.transform.position = Origin;
            // 바닥 · 안내 원
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var gm = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave }; gm.SetColor("_BaseColor", new Color(0.2f, 0.21f, 0.24f)); made.Add(gm);
            var ground = Hidden("Ground", stage.transform);
            ground.AddComponent<MeshFilter>().sharedMesh = Quad(); ground.AddComponent<MeshRenderer>().sharedMaterial = gm;
            ground.transform.localRotation = Quaternion.Euler(90, 0, 0); ground.transform.localScale = Vector3.one * 60;
            if (guide > 0)
            {
                var rm = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave }; rm.SetColor("_BaseColor", new Color(0.36f, 0.38f, 0.44f)); made.Add(rm);
                var gr = Hidden("Guide", stage.transform);
                gr.AddComponent<MeshFilter>().sharedMesh = GuideRing(guide);
                gr.AddComponent<MeshRenderer>().sharedMaterial = rm;
                gr.transform.localPosition = Vector3.up * 0.005f;
            }
            // 이펙트 (프리팹 사본, 숨김)
            var fxGo = (GameObject)Object.Instantiate(pf);
            foreach (var t in fxGo.GetComponentsInChildren<Transform>(true)) { t.gameObject.hideFlags = HideFlags.HideAndDontSave; t.gameObject.layer = Layer; }
            fxGo.transform.SetParent(stage.transform, false);
            fxGo.transform.localPosition = Vector3.up * rootY; fxGo.transform.localRotation = Quaternion.Euler(0, yaw, 0); fxGo.transform.localScale = Vector3.one * scale;
            // 카메라
            var camGo = Hidden("CapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 40; cam.nearClipPlane = 0.3f; cam.farClipPlane = 200f;
            cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.21f, 0.24f);
            var look = Origin + focus;
            cam.transform.position = look + new Vector3(0, 17f, -11f) * dist;
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var sims = new List<Sim>();
            int si = 0;
            foreach (var p in fxGo.GetComponentsInChildren<FxPart>(true))
            {
                p.enabled = false;
                var s = new Sim { p = p, mr = p.GetComponent<MeshRenderer>(), mf = p.GetComponent<MeshFilter>() };
                s.baseLocal = p.transform.localPosition; s.baseScale = p.transform.localScale; s.baseRot = p.transform.localRotation;
                s.drift = fxGo.transform.TransformDirection(p.drift) * fxGo.transform.lossyScale.x;
                s.roll = p.randomRoll ? (si * 137.5f) % 360f : 0f; s.seed = si * 7.31f; si++;
                if (!string.IsNullOrEmpty(p.flipSet)) { s.frames = FxFlipbook.Frames(p.flipSet); if (s.mf) s.mf.sharedMesh = Quad(); }
                sims.Add(s);
            }
            int n = Mathf.CeilToInt(dur * fps);
            const int sub = 4;
            float dt = 1f / fps / sub, time = 0;
            try
            {
                cam.Render();
                for (int f = 0; f < n; f++)
                {
                    foreach (var s in sims) Pose(s, time, cam, fxGo.transform);
                    cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    File.WriteAllBytes(Path.Combine(outDir, $"f_{f:000}.png"), tex.EncodeToPNG());
                    for (int k = 0; k < sub; k++) { time += dt; foreach (var s in sims) Step(s, time, dt); }
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(camGo); Object.DestroyImmediate(fxGo); Object.DestroyImmediate(stage);
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
                foreach (var o in made) Object.DestroyImmediate(o);
            }
            return $"{name}: {n} 프레임 → {outDir}";
        }

        /// <summary>바닥 안내 원: 반경 r · 선 굵기 4cm (게임에는 안 씀, 촬영 전용)</summary>
        static Mesh GuideRing(float r)
        {
            const int N = 96;
            var v = new Vector3[N * 2]; var tri = new int[N * 6];
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2 / N;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * (r - 0.02f); v[i * 2 + 1] = d * (r + 0.02f);
                int j = (i + 1) % N;
                tri[i * 6] = i * 2; tri[i * 6 + 1] = j * 2; tri[i * 6 + 2] = i * 2 + 1;
                tri[i * 6 + 3] = i * 2 + 1; tri[i * 6 + 4] = j * 2; tri[i * 6 + 5] = j * 2 + 1;
            }
            var m = new Mesh { name = "Guide", hideFlags = HideFlags.HideAndDontSave, vertices = v, triangles = tri };
            m.RecalculateBounds();
            return m;
        }

        static void Step(Sim s, float time, float dt)
        {
            float t = (time - s.p.delay) / Mathf.Max(0.01f, s.p.life);
            if (t < 0 || t >= 1) return;
            s.acc += s.drift * dt;
            if (s.p.kind != K.Tracer) s.drift *= Mathf.Exp(-6f * dt);
            if (s.p.spin != 0) s.baseRot = s.baseRot * Quaternion.AngleAxis(s.p.spin * dt, s.p.spinAxis);
        }

        static void Pose(Sim s, float time, Camera cam, Transform root)
        {
            var p = s.p; var tr = p.transform;
            float t = (time - p.delay) / Mathf.Max(0.01f, p.life);
            tr.localPosition = s.baseLocal; tr.position += s.acc;
            bool on = t >= 0 && t < 1;
            s.mr.enabled = on;
            if (!on) return;
            if (s.frames != null && s.frames.Length > 0)
            {
                s.mr.sharedMaterial = s.frames[Mathf.Min(s.frames.Length - 1, (int)(t * s.frames.Length))];
                tr.localScale = s.baseScale * Mathf.Lerp(1f, p.flipGrow, t);
                if (p.flat) tr.rotation = Quaternion.Euler(0, root.eulerAngles.y + s.roll, 0) * Quaternion.Euler(90, 0, 0);
                else
                {
                    tr.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up) * Quaternion.Euler(0, 0, s.roll);
                    if (p.pivotX != 0.5f || p.pivotY != 0.5f || p.liftToCamera != 0)
                    {
                        float k = root.lossyScale.x, sz = s.baseScale.y * k * Mathf.Lerp(1f, p.flipGrow, t);
                        var p0 = tr.position;
                        tr.position = p0 + tr.right * (0.5f - p.pivotX) * sz + tr.up * (0.5f - p.pivotY) * sz + (cam.transform.position - p0).normalized * p.liftToCamera * k;
                    }
                }
                var fc = Color.white; fc.a *= p.opacity;
                s.mpb.SetColor("_Color", fc);
                s.mr.SetPropertyBlock(s.mpb);
                return;
            }
            FxMesh.Evaluate(p.kind, s.baseScale, t, out var sc, out float heat, out float dis);
            if (p.hold)
            {
                float pop = t < 0.12f ? Mathf.Lerp(0.3f, 1.15f, Mathf.Sin(t / 0.12f * Mathf.PI * 0.5f)) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.12f) / 0.1f));
                float end = t > 0.85f ? 1 - (t - 0.85f) / 0.15f : 1;
                sc = s.baseScale * pop * end; heat = p.holdHeat; dis = 0;
            }
            if (p.blink > 0)
            {
                float ph = p.blink * p.life * (t + t * t);
                float b = Mathf.Pow(Mathf.Abs(Mathf.Sin(ph * Mathf.PI)), 6f);
                sc *= 1 + 0.35f * b; heat = Mathf.Max(heat, b);
            }
            tr.localScale = sc;
            if (p.billboard) tr.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up) * Quaternion.Euler(0, 0, s.roll) * s.baseRot;
            else tr.localRotation = s.baseRot;
            s.mpb.SetFloat(FxMesh.HeatId, heat); s.mpb.SetFloat(FxMesh.DissolveId, dis); s.mpb.SetFloat(FxMesh.SeedId, s.seed);
            s.mr.SetPropertyBlock(s.mpb);
        }
    }
}
