// 적 스킬별 전용 모션 (2026-11-19 사용자: "모션도 계속 재사용 하지 말고 스킬들마다 좀 다르게, 액션감 있게" · "모든 적 모션 재활용한 거 검토하고 따로 만들어야 할 것은 다시 만들기, 너무 단조롭지 않게")
// Ch2Motion과 같은 방식: 크레딧 안 드는 휴머노이드 근육 커브 코드 클립, 기준 = 고양이 idle(0.3초) + 덧셈.
// 박자는 런타임 타이밍과 맞춤 = 예고(준비 자세) → 타격(0.05~0.1초로 빠르게) → 빈틈(따라 숙인 자세 유지).
//
// 자세는 "약어=값" 문자열 (값은 고양이 idle에서 더하는 근육값, 뒤에 같은 약어가 또 나오면 덮어씀):
//   RA/LA.FB 팔 앞(+)뒤 · .DU 팔 위(+)아래 · .TW 팔 비틀기 / RF/LF 팔꿈치 펴기(+)굽히기 · RF.TW 아래팔 비틀기 / RH/LH.DU 손목 · .IO 손목 옆
//   RS/LS.DU 어깨 으쓱 · .FB 어깨 앞뒤 / SP.FB 허리 숙임(+) · SP.LR 오른쪽으로 기욺(+) · SP.TW 허리를 오른쪽으로 돌림(+) / CH.* 가슴 같은 뜻 / HD.ND 고개 듦(+) · HD.TN 오른쪽 봄(+) · HD.TL 갸웃
//   RL/LL.FB 허벅지 앞(+) · .IO 벌림 / RK/LK 무릎 펴기(+)굽히기 / RFT/LFT 발목 (− = 발끝 세움) / ROOT 몸 높이(m, 덧셈)
//   ※ 2026-11-19 리그에서 잰 방향: 유니티 'Front-Back' 근육은 + = 뒤 → Ch2Motion.MirrorFrontBack이 뒤집어 주므로 이 표의 .FB는 + = 앞 그대로.
//     유니티 'Foot Up-Down'은 + = 발끝 내림 → 여기 FT는 − = 발끝 세움으로 적고 P()에서 부호를 바꿈. Twist · Left-Right · Nod는 유니티 그대로(+ = 오른쪽 · 위)
// 클립: Assets/Art/Characters/ChN/Anim/ChNM_*.anim → EnemyModelBuilder.Moves(스킬별 상태) · Anims(Attack/Special)
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class EnemyMotions
    {
        const string D1 = "Assets/Art/Characters/Ch1/Anim/", D2 = "Assets/Art/Characters/Ch2/Anim/", D3 = "Assets/Art/Characters/Ch3/Anim/", D4 = "Assets/Art/Characters/Ch4/Anim/", D5 = "Assets/Art/Characters/Ch5/Anim/";

        static readonly Dictionary<string, string> Abbr = new()
        {
            { "SP.FB", "Spine Front-Back" }, { "SP.LR", "Spine Left-Right" }, { "SP.TW", "Spine Twist Left-Right" },
            { "CH.FB", "Chest Front-Back" }, { "CH.LR", "Chest Left-Right" }, { "CH.TW", "Chest Twist Left-Right" },
            { "HD.ND", "Head Nod Down-Up" }, { "HD.TN", "Head Turn Left-Right" }, { "HD.TL", "Head Tilt Left-Right" },
        };

        static string Muscle(string a)
        {
            if (Abbr.TryGetValue(a, out var m)) return m;
            string side = a[0] == 'R' ? "Right " : "Left ";
            string rest = a.Substring(1);
            return rest switch
            {
                "A.FB" => side + "Arm Front-Back", "A.DU" => side + "Arm Down-Up", "A.TW" => side + "Arm Twist In-Out",
                "F" => side + "Forearm Stretch", "F.TW" => side + "Forearm Twist In-Out",
                "H.DU" => side + "Hand Down-Up", "H.IO" => side + "Hand In-Out",
                "S.DU" => side + "Shoulder Down-Up", "S.FB" => side + "Shoulder Front-Back",
                "L.FB" => side + "Upper Leg Front-Back", "L.IO" => side + "Upper Leg In-Out",
                "K" => side + "Lower Leg Stretch", "FT" => side + "Foot Up-Down",
                _ => null,
            };
        }

        static Dictionary<string, float> P(string s)
        {
            var d = new Dictionary<string, float>();
            foreach (var tok in s.Split(' '))
            {
                if (tok.Length == 0) continue;
                var kv = tok.Split('=');
                string key = kv[0] == "ROOT" ? "ROOT" : Muscle(kv[0]);
                if (key == null) { Debug.LogWarning("[EnemyMotions] 모르는 약어 " + kv[0]); continue; }
                float val = float.Parse(kv[1], CultureInfo.InvariantCulture);
                if (key.EndsWith("Foot Up-Down")) val = -val;                 // FT: − = 발끝 세움 (유니티는 + = 발끝 내림)
                d[key] = val;
            }
            return d;
        }

        /// <summary>시간표 (시각, 자세 문자열)로 클립: 어느 자세에든 나온 근육은 모든 키에 값(없으면 0 = idle)</summary>
        static void Clip(AnimationClip idle, string path, bool loop, params (float t, string pose)[] tl)
        {
            var poses = new List<(float t, Dictionary<string, float> p)>();
            var names = new HashSet<string>();
            foreach (var (t, s) in tl) { var p = P(s); poses.Add((t, p)); foreach (var k in p.Keys) names.Add(k); }
            var keys = new Dictionary<string, float[][]>();
            float[][] root = null;
            foreach (var n in names)
            {
                var kv = new float[poses.Count][];
                for (int i = 0; i < poses.Count; i++) kv[i] = new[] { poses[i].t, poses[i].p.TryGetValue(n, out var v) ? v : 0f };
                if (n == "ROOT") root = kv; else keys["+" + n] = kv;
            }
            var strike = IsSwing(path) ? SwingFeel(keys, ref root) : null;
            bool yaw = strike != null && BodyYaw(idle, keys);
            Ch2Motion.Make(idle, path, tl[tl.Length - 1].t, loop, keys, root);
            if (strike != null) StrikeTangents(path, strike);
            if (yaw) BakeOrientation(path);
        }

        // ================= 휘두르기 손맛 (2026-11-20 사용자: 참격 모션 전부 상체 회전력 · 가감속으로 사실적으로, 지금 너무 정적) =================
        // 모든 장의 휘두르기 클립(이름에 Swing · Slash · Whirl · Reap · Stab · Cut · Slam · Smash · Cleave · Chop · Guillotine)을 만든 뒤 한 번 더 다듬음:
        //  ① 박자 찾기: 몸통(허리 비틀기, 없으면 허리 숙임) 값이 가장 빠르게 바뀌는 키 = 치는 키, 그 앞 키 = 감은 꼭대기
        //  ② 상체 회전력: 감은 꼭대기 · 치는 키의 허리 값을 1.25배로, 가슴 비틀기는 허리를 따라(0.55배), 고개는 반대로 조금(눈은 목표에)
        //  ③ 운동 사슬: 다리 · 골반이 먼저(−0.02초) → 허리 → 가슴(+0.02) → 어깨(+0.03) → 위팔(+0.04) → 아래팔(+0.05) → 손(+0.06) 순서로 한 박자씩 늦게 도착 (채찍처럼)
        //  ④ 가감속: 감는 동안은 부드럽게(ClampedAuto), 치는 키로 들어갈 때는 곧은 선(가속), 친 뒤 몸통은 조금 넘쳤다(18%) 감속하며 돌아옴
        static readonly System.Text.RegularExpressions.Regex SwingName = new("(Swing|Slash|Whirl|Reap|Stab|Cut|Slam|Smash|Cleave|Chop|Guillotine)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        static bool IsSwing(string path) => SwingName.IsMatch(System.IO.Path.GetFileName(path)) && !path.Contains("Wind");   // 감기만 하는 클립(ReapWind)은 뺌

        static float Delay(string muscle)
        {
            if (muscle.Contains("Leg") || muscle.Contains("Foot") || muscle.Contains("Toes")) return -0.02f;
            if (muscle.Contains("Spine")) return 0f;
            if (muscle.Contains("Chest") || muscle.Contains("UpperChest")) return 0.02f;
            if (muscle.Contains("Neck") || muscle.Contains("Head")) return 0.025f;
            if (muscle.Contains("Shoulder")) return 0.03f;
            if (muscle.Contains("Forearm")) return 0.05f;
            if (muscle.Contains("Hand") || muscle.Contains("Finger")) return 0.06f;
            if (muscle.Contains("Arm")) return 0.04f;
            return 0f;
        }

        /// <summary>①~④ 중 키 값 · 시각을 바꾸는 부분. 돌려주는 값 = 근육별 '치는 키' 시각 (접선 다듬기용), 휘두르기로 못 읽으면 null</summary>
        static Dictionary<string, float> SwingFeel(Dictionary<string, float[][]> keys, ref float[][] root)
        {
            string drv = keys.ContainsKey("+Spine Twist Left-Right") ? "+Spine Twist Left-Right" : keys.ContainsKey("+Spine Front-Back") ? "+Spine Front-Back" : null;
            if (drv == null) return null;
            var d = keys[drv];
            int n = d.Length;
            if (n < 4) return null;
            // ① 치는 키 = 가장 빠른 변화 (첫 · 마지막 키는 쉬는 자세라 뺌)
            int s = -1; float best = 0;
            for (int i = 2; i < n - 1; i++)
            {
                float sp = Mathf.Abs(d[i][1] - d[i - 1][1]) / Mathf.Max(0.01f, d[i][0] - d[i - 1][0]);
                if (sp > best) { best = sp; s = i; }
            }
            if (s < 2 || Mathf.Abs(d[s][1] - d[s - 1][1]) < 0.12f) return null;
            int w = s - 1;
            float tW = d[w][0], tS = d[s][0];
            // ② 상체 회전력
            d[w][1] *= 1.25f; d[s][1] *= 1.25f;
            if (drv.Contains("Twist"))
            {
                if (!keys.ContainsKey("+Chest Twist Left-Right"))
                {
                    var c = new float[n][]; for (int i = 0; i < n; i++) c[i] = new[] { d[i][0], d[i][1] * 0.55f };
                    keys["+Chest Twist Left-Right"] = c;
                }
                if (!keys.ContainsKey("+Head Turn Left-Right"))
                {
                    var h = new float[n][]; for (int i = 0; i < n; i++) h[i] = new[] { d[i][0], -d[i][1] * 0.3f };
                    keys["+Head Turn Left-Right"] = h;
                }
            }
            // ③ 운동 사슬: 감은 꼭대기부터 뒤 키들을 근육 무리별로 늦춤 (마지막 키는 끝에 그대로, 순서는 안 뒤집힘)
            var strike = new Dictionary<string, float>();
            foreach (var kv in keys)
            {
                float dl = Delay(kv.Key);
                var a = kv.Value;
                for (int i = 1; i < a.Length - 1; i++)
                {
                    if (a[i][0] < tW - 1e-4f) continue;
                    float lo = a[i - 1][0] + 0.012f, hi = a[i + 1][0] - 0.012f;
                    a[i][0] = Mathf.Clamp(a[i][0] + dl, lo, Mathf.Max(lo, hi));
                }
                strike[kv.Key] = tS + dl;
            }
            if (root != null)
                for (int i = 1; i < root.Length - 1; i++)
                    if (root[i][0] >= tW - 1e-4f) root[i][0] = Mathf.Clamp(root[i][0] - 0.02f, root[i - 1][0] + 0.012f, Mathf.Max(root[i - 1][0] + 0.012f, root[i + 1][0] - 0.012f));
            // ④ 몸통 넘침: 친 뒤 0.06초에 감은 쪽 반대로 18% 더 → 그다음 키로 감속하며 돌아옴 (다음 키가 충분히 떨어져 있을 때만)
            foreach (var m in new[] { drv, "+Chest Twist Left-Right" })
            {
                if (!keys.TryGetValue(m, out var a)) continue;
                int k = -1; float ts = strike.TryGetValue(m, out var t0) ? t0 : tS;
                for (int i = 0; i < a.Length; i++) if (Mathf.Abs(a[i][0] - ts) < 0.02f) { k = i; break; }
                if (k < 1 || k >= a.Length - 1 || a[k + 1][0] - a[k][0] < 0.14f) continue;
                float over = a[k][1] + (a[k][1] - a[k - 1][1]) * 0.18f;
                var list = new List<float[]>(a);
                list.Insert(k + 1, new[] { a[k][0] + 0.06f, over });
                keys[m] = list.ToArray();
            }
            return strike;
        }

        // ⑤ 몸 전체 회전 (2026-11-20 사용자: "돌리는 각이 너무 작아" — 랫키호테 평타는 칼끝이 240 ~ 360° 돎): 허리 비틀기를 따라 몸통 전체(루트 방향)를 같은 쪽으로
        //    허리 1(≈40°)당 42° 더 돌림 → 감은 쪽 ~110° · 친 쪽 ~110°, 칼끝 호 약 200°. 루트 회전은 클립 '방향 굽기'(Bake Into Pose)로 몸에만 (적 오브젝트 방향은 그대로)
        const float YawPerTwist = 42f;
        static bool BodyYaw(AnimationClip idle, Dictionary<string, float[][]> keys)
        {
            if (!keys.TryGetValue("+Spine Twist Left-Right", out var tw)) return false;
            float Q(string n) { foreach (var b in AnimationUtility.GetCurveBindings(idle)) if (b.propertyName == n) return AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.3f); return n == "RootQ.w" ? 1 : 0; }
            var q0 = new Quaternion(Q("RootQ.x"), Q("RootQ.y"), Q("RootQ.z"), Q("RootQ.w"));
            var qs = new float[4][][];
            for (int c = 0; c < 4; c++) qs[c] = new float[tw.Length][];
            for (int i = 0; i < tw.Length; i++)
            {
                var q = Quaternion.Euler(0, YawPerTwist * tw[i][1], 0) * q0;
                qs[0][i] = new[] { tw[i][0], q.x }; qs[1][i] = new[] { tw[i][0], q.y }; qs[2][i] = new[] { tw[i][0], q.z }; qs[3][i] = new[] { tw[i][0], q.w };
            }
            keys["RootQ.x"] = qs[0]; keys["RootQ.y"] = qs[1]; keys["RootQ.z"] = qs[2]; keys["RootQ.w"] = qs[3];
            return true;
        }

        static void BakeOrientation(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.StartsWith("Assets/") ? path + ".anim" : path);
            if (!clip) return;
            var st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopBlendOrientation = true;     // Root Transform Rotation — Bake Into Pose
            st.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, st);
            EditorUtility.SetDirty(clip);
        }

        /// <summary>④ 가감속 접선 (랫키호테 attack act = ease-out): 감은 꼭대기를 떠나는 순간이 가장 빠르고(나가는 접선 2.4배) 치는 키에 거의 멈추듯 도착(들어가는 접선 0.15배)</summary>
        static void StrikeTangents(string path, Dictionary<string, float> strike)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path.StartsWith("Assets/") ? path + ".anim" : path);
            if (!clip) return;
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                if (!strike.TryGetValue("+" + b.propertyName, out float ts)) continue;
                var c = AnimationUtility.GetEditorCurve(clip, b);
                for (int i = 1; i < c.length; i++)
                {
                    if (Mathf.Abs(c.keys[i].time - ts) > 0.012f) continue;
                    var a = c.keys[i - 1]; var k = c.keys[i];
                    float slope = (k.value - a.value) / Mathf.Max(1e-3f, k.time - a.time);
                    AnimationUtility.SetKeyBroken(c, i - 1, true); AnimationUtility.SetKeyBroken(c, i, true);
                    AnimationUtility.SetKeyRightTangentMode(c, i - 1, AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Free);
                    a = c.keys[i - 1]; k = c.keys[i];
                    a.outTangent = slope * 2.4f; k.inTangent = slope * 0.15f;
                    c.MoveKey(i - 1, a); c.MoveKey(i, k);
                    break;
                }
                AnimationUtility.SetEditorCurve(clip, b, c);
            }
            EditorUtility.SetDirty(clip);
        }

        static AnimationClip CatClip(string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == name) return c;
            return null;
        }

        /// <summary>고양이 클립(src)을 그대로 쓰되, pose에 나온 근육만 '고양이 idle + 값'으로 고정 (무기 든 팔). 반복 · 길이는 src 그대로, 앞뒤 근육은 MirrorFrontBack</summary>
        /// <param name="swing">0 = 팔을 자세 그대로 고정. 0보다 크면 고정 자세 위에 원본 걸음의 팔 앞뒤 흔들림(다리와 엇갈림)을 이만큼 얹음
        /// (2026-11-20 사용자: 붕괴 거인 걸을 때 팔이 같이 안 움직임)</param>
        static void HoldArm(AnimationClip src, string path, string pose, float swing = 0f)
        {
            var idle = Ch2Motion.CatIdle();
            if (!src || !idle) return;
            var hold = P(pose);
            string full = path + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(full);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, full); }
            clip.ClearCurves();
            clip.name = System.IO.Path.GetFileNameWithoutExtension(full);
            float len = src.length;
            var held = new Dictionary<string, float>();
            foreach (var b in AnimationUtility.GetCurveBindings(src))
            {
                var c = AnimationUtility.GetEditorCurve(src, b);
                if (hold.TryGetValue(b.propertyName, out var off))
                {
                    var ic = AnimationUtility.GetEditorCurve(idle, b);
                    float v0 = ic != null ? ic.Evaluate(0.3f) : c.Evaluate(0);
                    if (swing > 0 && b.propertyName.EndsWith("Arm Front-Back") && c.length > 1)
                    {
                        // 원본 흔들림(평균을 뺀 값)을 얹음. 아래 MirrorHeldOnly가 앞뒤 근육 값을 뒤집으므로 흔들림은 미리 뒤집어 둠 → 뒤집힌 뒤 원래 방향(다리와 엇갈림)
                        float mean = 0; int n = 24;
                        for (int k = 0; k < n; k++) mean += c.Evaluate(len * k / n);
                        mean /= n;
                        var ks = new Keyframe[n + 1];
                        for (int k = 0; k <= n; k++) { float t = len * k / n; ks[k] = new Keyframe(t, v0 + off - (c.Evaluate(t) - mean) * swing); }
                        c = new AnimationCurve(ks);
                        for (int k = 0; k <= n; k++) c.SmoothTangents(k, 0);
                    }
                    else c = new AnimationCurve(new Keyframe(0, v0 + off), new Keyframe(len, v0 + off));
                    held[b.propertyName] = 1;
                }
                AnimationUtility.SetEditorCurve(clip, b, c);
            }
            foreach (var k in hold.Keys) if (!held.ContainsKey(k)) Debug.LogWarning($"[EnemyMotions] {clip.name}: '{k}' 커브가 원본에 없음");
            MirrorHeldOnly(clip, idle, held);                              // 고정한 팔의 앞뒤 근육만 뒤집기 (원본 커브는 이미 실제 방향)
            var st = AnimationUtility.GetAnimationClipSettings(src);
            AnimationUtility.SetAnimationClipSettings(clip, st);
            EditorUtility.SetDirty(clip);
        }

        static void MirrorHeldOnly(AnimationClip clip, AnimationClip idle, Dictionary<string, float> held)
        {
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                if (!held.ContainsKey(b.propertyName) || !b.propertyName.EndsWith("Front-Back")) continue;
                var ic = AnimationUtility.GetEditorCurve(idle, b);
                float p = ic != null ? ic.Evaluate(0.3f) : 0f;
                var c = AnimationUtility.GetEditorCurve(clip, b);
                var ks = c.keys;
                for (int i = 0; i < ks.Length; i++) { var k = ks[i]; k.value = 2f * p - k.value; ks[i] = k; }
                c.keys = ks;
                AnimationUtility.SetEditorCurve(clip, b, c);
            }
        }

        [MenuItem("CatTang/적 전용 모션 클립 만들기 (1 ~ 5장)")]
        public static void MakeAll()
        {
            var idle = Ch2Motion.CatIdle();
            if (!idle) { Debug.LogError("[EnemyMotions] 고양이 idle 클립이 없음"); return; }
            Ch1(idle); Ch2(idle); Ch3(idle); Ch4(idle); Ch5(idle);
            Intros(idle);
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyMotions] 적 전용 모션 클립 완료");
        }

        // ======================= 1장 =======================
        static void Ch1(AnimationClip idle)
        {
            // 무법자 3점사 = 허리춤 '부채질 사격': 오른손 리볼버를 허리에 고정, 왼손바닥으로 공이를 세 번 후려침 (발사 0.6 · 0.73 · 0.86)
            const string HIP = "RA.FB=.6 RA.DU=-.05 RF=-.1 LA.FB=.45 LA.DU=.05 LF=-.75 LH.DU=.3 SP.FB=.12 SP.TW=-.15 LL.FB=.25 LK=-.3 RK=-.2 ROOT=-.05";
            const string SLAP = HIP + " LH.DU=-.5 LF=-.55 RA.DU=.08 SP.FB=.05";
            Clip(idle, D1 + "Ch1M_OutlawFan", false, (0, ""), (.5f, HIP), (.6f, HIP), (.63f, SLAP), (.7f, HIP), (.76f, SLAP), (.83f, HIP), (.89f, SLAP), (1.05f, ""));
            // 무법자+ 옆 대시: 몸을 옆으로 눕히고 다리를 벌려 미끄러짐
            const string SIDE = "SP.LR=.45 CH.LR=.2 LL.IO=.45 RL.IO=-.1 LK=-.4 RK=-.2 LA.DU=.55 RA.DU=.25 RA.FB=.4 HD.TL=-.3 ROOT=-.08";
            Clip(idle, D1 + "Ch1M_OutlawSideDash", false, (0, ""), (.08f, SIDE), (.3f, SIDE), (.4f, ""));

            // 깡패 내리찍기: 두 팔을 머리 위로 크게 (0.6) → 몸째 숙이며 바닥을 쾅 → 숙인 채 버팀
            const string OVER = "LA.DU=1.1 RA.DU=1.1 LA.FB=.3 RA.FB=.3 LF=-.35 RF=-.35 SP.FB=-.3 CH.FB=-.2 HD.ND=.3 LFT=-.2 RFT=-.2 ROOT=.03";
            const string SMASH = "LA.DU=-.15 RA.DU=-.15 LA.FB=.9 RA.FB=.9 LF=.2 RF=.2 SP.FB=.6 CH.FB=.25 HD.ND=-.2 LL.FB=.45 RL.FB=.45 LK=-.6 RK=-.6 ROOT=-.15";
            Clip(idle, D1 + "Ch1M_BruteSlam", false, (0, ""), (.5f, OVER), (.6f, OVER + " LA.DU=1.2 RA.DU=1.2"), (.67f, SMASH), (.95f, SMASH), (1.15f, ""));
            // 깡패+ 회전 휘두르기: 허리를 감아 웅크림 (0.7) → 두 팔 펴며 풀어냄 (몸 회전은 코드)
            const string COIL = "SP.TW=-.75 CH.TW=-.35 LA.FB=-.4 RA.FB=.5 LA.DU=.3 RA.DU=.3 LL.FB=.35 RL.FB=.35 LK=-.5 RK=-.5 ROOT=-.1";
            const string FLING = "SP.TW=.4 LA.DU=.65 RA.DU=.65 LF=.4 RF=.4 LK=-.3 RK=-.3 ROOT=-.06";
            Clip(idle, D1 + "Ch1M_BruteSpin", false, (0, ""), (.6f, COIL), (.7f, COIL), (.78f, FLING), (1f, ""));

            // 현상금 사냥꾼 회전 난사: 웅크려 장전 (0.4) → 두 팔을 양옆으로 쭉 펴고 쏘며 돎 (몸 회전은 패턴 코드), 반동으로 가슴이 들썩
            const string ARMSOUT = "LA.DU=.95 RA.DU=.95 LA.FB=.1 RA.FB=.1 LF=.35 RF=.35 SP.FB=.05 LK=-.25 RK=-.25 ROOT=-.04";
            const string ARMSOUT2 = ARMSOUT + " CH.FB=-.12 LA.DU=1.02 RA.DU=.88 HD.ND=.1";
            Clip(idle, D1 + "Ch1M_HunterSpinFire", false, (0, ""), (.35f, "LA.FB=.6 RA.FB=.6 LF=-.6 RF=-.6 SP.FB=.25 LK=-.4 RK=-.4 ROOT=-.08"), (.45f, ARMSOUT),
                (.9f, ARMSOUT2), (1.35f, ARMSOUT), (1.8f, ARMSOUT2), (2.3f, ARMSOUT), (2.4f, ""));
            // 수배 저격: 한쪽 무릎 꿇고 두 손 조준 (1.6초) → 강한 반동
            const string KNEEL = "LL.FB=.6 LK=-.9 RL.FB=-.1 RK=-.95 ROOT=-.28 RA.FB=.8 RA.DU=.15 RF=.1 LA.FB=.85 LA.DU=.1 LF=-.1 SP.FB=.15 HD.ND=-.15 HD.TL=.15";
            Clip(idle, D1 + "Ch1M_HunterSnipe", false, (0, ""), (.35f, KNEEL), (1.6f, KNEEL), (1.64f, KNEEL + " RA.DU=.45 LA.DU=.4 SP.FB=-.15 HD.ND=.1"), (1.9f, KNEEL));
            // 돌진 태클: 어깨를 낮춰 땅을 박차는 준비 (0.6) → 어깨를 앞세워 달림
            const string SET = "SP.FB=.55 LS.FB=.5 LA.FB=.4 LF=-.7 RA.FB=-.4 RF=-.6 LL.FB=.4 LK=-.6 RL.FB=-.3 RK=-.4 HD.ND=.2 ROOT=-.14";
            const string RUNA = "SP.FB=.6 LS.FB=.6 LA.FB=.55 LF=-.6 RA.FB=-.55 LL.FB=.6 LK=-.2 RL.FB=-.35 RK=-.8 HD.ND=.25 ROOT=-.1";
            const string RUNB = "SP.FB=.6 LS.FB=.6 LA.FB=.55 LF=-.6 RA.FB=-.55 LL.FB=-.35 LK=-.8 RL.FB=.6 RK=-.2 HD.ND=.25 ROOT=-.1";
            Clip(idle, D1 + "Ch1M_HunterTackle", false, (0, ""), (.5f, SET), (.6f, SET), (.75f, RUNA), (.95f, RUNB), (1.15f, RUNA), (1.35f, RUNB), (1.6f, ""));
            // 하수인 호출: 손가락 휘파람 + 오른팔을 크게 흔들어 부름
            const string WHIS = "LA.FB=.55 LA.DU=.35 LF=-1 LH.DU=.2 HD.ND=.1 CH.FB=-.15";
            Clip(idle, D1 + "Ch1M_HunterWhistle", false, (0, ""), (.15f, WHIS), (.3f, WHIS + " RA.DU=1.1 RA.FB=.2 RF=-.2"), (.45f, WHIS + " RA.DU=.85 RA.FB=-.1 RF=-.5"),
                (.6f, WHIS + " RA.DU=1.1 RA.FB=.2 RF=-.2"), (.75f, WHIS + " RA.DU=.85 RA.FB=-.1 RF=-.5"), (.9f, ""));

            // 녹은 시계 거인 — 녹은 웅덩이: 두 팔을 들어 (0.8) 손바닥으로 바닥을 짓누름 (녹아 흘러내림)
            const string LIFT = "LA.DU=.8 RA.DU=.8 LA.FB=.55 RA.FB=.55 LF=-.4 RF=-.4 SP.FB=-.2 HD.ND=.2 ROOT=.02";
            const string PRESS = "LA.DU=-.1 RA.DU=-.1 LA.FB=.75 RA.FB=.75 LF=.3 RF=.3 LH.DU=.5 RH.DU=.5 SP.FB=.55 LK=-.5 RK=-.5 LL.FB=.35 RL.FB=.35 HD.ND=-.2 ROOT=-.13";
            Clip(idle, D1 + "Ch1M_GiantDrip", false, (0, ""), (.7f, LIFT), (.8f, LIFT), (.88f, PRESS), (1.2f, PRESS));
            // 12방향 숫자 탄: 팔짱 끼듯 모음 (0.5) → 두 팔 활짝, 숫자를 뿌리며 상체를 좌우로 돌림
            const string CROSSED = "LA.FB=.6 RA.FB=.6 LA.DU=.15 RA.DU=.15 LF=-.85 RF=-.85 SP.FB=.2 HD.ND=-.2 ROOT=-.04";
            const string OPEN = "LA.DU=.9 RA.DU=.9 LF=.3 RF=.3 CH.FB=-.3 HD.ND=.3";
            Clip(idle, D1 + "Ch1M_GiantCast", false, (0, ""), (.45f, CROSSED), (.55f, OPEN + " SP.TW=-.25"), (1.1f, OPEN + " SP.TW=.25"), (1.6f, OPEN + " SP.TW=-.25"), (1.7f, ""));
            // 되감기 폭발: 두 팔로 시곗바늘을 거꾸로 감듯 크게 두 바퀴 (앞 → 위 → 뒤 → 아래)
            const string C1 = "LA.DU=.3 RA.DU=.3 LA.FB=.7 RA.FB=.7 LF=.2 RF=.2", C2 = "LA.DU=1.1 RA.DU=1.1 LA.FB=.3 RA.FB=.3",
                         C3 = "LA.DU=.9 RA.DU=.9 LA.FB=-.5 RA.FB=-.5 CH.FB=-.2", C4 = "LA.DU=.1 RA.DU=.1 LA.FB=-.4 RA.FB=-.4 SP.FB=.15";
            Clip(idle, D1 + "Ch1M_GiantRewind", false, (0, ""), (.2f, C1), (.4f, C2), (.6f, C3), (.8f, C4), (1f, C1), (1.2f, C2), (1.4f, C3), (1.6f, C4), (1.8f, ""));
            // 시간 지연 구역: 오른손바닥을 앞으로 내밀어 공기를 짓누름
            const string PALM = "RA.FB=.75 RA.DU=.15 RF=.4 RH.DU=.6 SP.FB=.2 LA.FB=-.1 HD.ND=-.15 LL.FB=.3 LK=-.3 ROOT=-.05";
            Clip(idle, D1 + "Ch1M_GiantPress", false, (0, ""), (.3f, PALM), (.7f, PALM + " RH.DU=.8 SP.FB=.3 ROOT=-.08"), (.9f, ""));
        }

        // ======================= 2장 (보스 패턴 나눔 — 같은 클립을 여러 패턴이 나눠 쓰던 것) =======================
        static void Ch2(AnimationClip idle)
        {
            // 태엽 역장 — 시간표 탄막: 왼손에 시간표를 들고, 줄이 나갈 때마다(0.5 · 1.1 · 1.7 · 2.3) 오른손으로 전광판을 넘기듯 휙
            const string HOLD = "LA.FB=.6 LA.DU=.55 LF=-.7 LH.DU=.2 HD.ND=.05";
            const string SWA = HOLD + " RA.FB=.6 RA.DU=.45 RF=.2 RA.TW=-.3 SP.TW=.2", SWB = HOLD + " RA.FB=.5 RA.DU=.4 RF=.3 RA.TW=.3 SP.TW=-.25";
            Clip(idle, D2 + "Ch2M_MasterSchedule", false, (0, ""), (.45f, SWA), (.55f, SWB), (1f, SWA), (1.15f, SWB), (1.6f, SWA), (1.75f, SWB), (2.2f, SWA), (2.35f, SWB), (2.9f, ""));
            // 호루라기 열차: 호루라기를 물고 몸을 젖혀 불었다가 (0.6) 왼팔로 선로를 가리킴
            const string BLOW = "RA.FB=.55 RA.DU=.35 RF=-1 RH.DU=.2 SP.FB=-.25 CH.FB=-.25 HD.ND=.25 LA.DU=.2";
            const string POINT = "LA.FB=.85 LA.DU=.35 LF=.45 RA.FB=.5 RA.DU=.3 RF=-.95 SP.FB=.15 HD.ND=-.05 LL.FB=.25 LK=-.2";
            Clip(idle, D2 + "Ch2M_MasterWhistle", false, (0, ""), (.35f, BLOW), (.55f, BLOW), (.7f, POINT), (1.2f, POINT), (1.3f, ""));
            // 차장 호출: 두 손으로 이리 오라 손짓 두 번
            const string BA = "RA.FB=.6 RA.DU=.5 RF=.2 LA.FB=.6 LA.DU=.5 LF=.2 CH.FB=-.1", BB = "RA.FB=.5 RA.DU=.6 RF=-.8 LA.FB=.5 LA.DU=.6 LF=-.8 CH.FB=-.15 HD.ND=.1";
            Clip(idle, D2 + "Ch2M_MasterCall", false, (0, ""), (.2f, BA), (.38f, BB), (.55f, BA), (.72f, BB), (.9f, ""));

            // 기관차 거인 — 증기 분출: 팔을 웅크려 압력을 모으며 떨다가 (1초) 가슴을 펴고 두 팔을 활짝
            const string GATH = "LA.FB=.5 RA.FB=.5 LA.DU=.1 RA.DU=.1 LF=-.85 RF=-.85 SP.FB=.35 LK=-.4 RK=-.4 LL.FB=.3 RL.FB=.3 HD.ND=-.2 ROOT=-.1";
            const string BURST = "LA.DU=.85 RA.DU=.85 LA.FB=-.15 RA.FB=-.15 LF=.35 RF=.35 CH.FB=-.35 SP.FB=-.2 HD.ND=.35 ROOT=.03";
            Clip(idle, D2 + "Ch2M_GiantVent", false, (0, ""), (.3f, GATH), (.45f, GATH + " SP.LR=.06"), (.6f, GATH), (.75f, GATH + " SP.LR=-.06"), (.9f, GATH), (1f, BURST), (1.45f, BURST), (1.6f, ""));
            // 석탄 포격: 허리 숙여 두 팔로 퍼 담고 (1초) 머리 위로 던져 올림
            const string SCOOP = "LA.FB=.5 RA.FB=.5 LA.DU=-.15 RA.DU=-.15 LF=.1 RF=.1 SP.FB=.7 LK=-.55 RK=-.55 LL.FB=.4 RL.FB=.4 ROOT=-.15";
            const string HURL = "LA.DU=1.1 RA.DU=1.1 LA.FB=.45 RA.FB=.45 LF=.3 RF=.3 SP.FB=-.25 CH.FB=-.2 HD.ND=.35 ROOT=.02";
            Clip(idle, D2 + "Ch2M_GiantHeave", false, (0, ""), (.8f, SCOOP), (.95f, SCOOP), (1.05f, HURL), (1.35f, HURL), (1.5f, ""));
            // 객차 회전: 두 팔을 펴고 상체를 크게 돌려 매단 객차를 돌림
            const string SPREAD = "LA.DU=.8 RA.DU=.8 LF=.3 RF=.3 CH.FB=-.15";
            Clip(idle, D2 + "Ch2M_GiantCoachSpin", false, (0, ""), (.5f, SPREAD + " SP.TW=-.45 CH.TW=-.2 HD.TN=-.3"), (1f, SPREAD + " SP.TW=.45 CH.TW=.2 HD.TN=.3"), (1.4f, ""));
        }

        // ======================= 3장 =======================
        static void Ch3(AnimationClip idle)
        {
            // 거울 사수: 왼손 손거울을 얼굴 옆에 들고 오른손으로 겨눔 (0.6) → 빛을 튕기듯 손목을 젖힘
            const string MIR = "LA.FB=.65 LA.DU=.45 LF=-.9 LH.DU=.3 RA.FB=.85 RA.DU=.15 RF=.3 SP.TW=-.15 HD.ND=-.05 LL.FB=.15";
            Clip(idle, D3 + "Ch3M_ShooterMirror", false, (0, ""), (.2f, MIR), (.58f, MIR), (.63f, MIR + " RH.DU=.5 RA.DU=.3 CH.FB=-.15 HD.ND=.1"), (.9f, ""));
            // 빛가루 법사: 두 팔을 천천히 등 뒤 위로 펼쳐 구슬을 모음 (1.5) → 구슬마다 한 손씩 번갈아 내던짐
            const string UP = "LA.DU=1 RA.DU=1 LA.FB=-.25 RA.FB=-.25 LF=-.2 RF=-.2 CH.FB=-.25 HD.ND=.3 ROOT=.03 LFT=-.3 RFT=-.3";
            Clip(idle, D3 + "Ch3M_MageConjure", false, (0, ""), (.6f, "LA.DU=.7 RA.DU=.7 LA.FB=-.15 RA.FB=-.15 CH.FB=-.15 HD.ND=.15"), (1.5f, UP));
            Clip(idle, D3 + "Ch3M_MageFlingR", false, (0, UP), (.1f, UP + " RA.FB=-.4 RA.DU=1.1"), (.16f, UP + " RA.FB=.85 RA.DU=.35 RF=.4 RH.DU=-.3 SP.TW=-.25 CH.FB=.1"), (.4f, UP));
            Clip(idle, D3 + "Ch3M_MageFlingL", false, (0, UP), (.1f, UP + " LA.FB=-.4 LA.DU=1.1"), (.16f, UP + " LA.FB=.85 LA.DU=.35 LF=.4 LH.DU=-.3 SP.TW=.25 CH.FB=.1"), (.4f, UP));
            // 거울 너머 검객: 거울로 뛰어듦 / 칼을 반대 어깨로 감았다 (0.45) 가로로 벰 → 축 늘어진 빈틈 / 거울에서 솟구치며 올려 벰
            const string LOWC = "SP.FB=.5 LK=-.8 RK=-.8 LL.FB=.5 RL.FB=.5 RA.FB=-.3 RA.DU=-.1 RF=-.3 ROOT=-.25";
            Clip(idle, D3 + "Ch3M_BladeDive", false, (0, ""), (.18f, "SP.FB=.4 LK=-.6 RK=-.6 LL.FB=.4 RL.FB=.4 LA.FB=-.4 RA.FB=-.4 ROOT=-.12"),
                (.4f, "SP.FB=.8 LA.FB=.9 RA.FB=.9 LA.DU=.4 RA.DU=.4 LL.FB=-.3 RL.FB=-.3 LK=.2 RK=.2 HD.ND=-.3 ROOT=-.3"), (.45f, "SP.FB=.8 LA.FB=.9 RA.FB=.9 LA.DU=.4 RA.DU=.4 LL.FB=-.3 RL.FB=-.3 HD.ND=-.3 ROOT=-.3"));
            // 칼(오른손)을 왼어깨 쪽으로 감음 = 허리 왼쪽(−), 벨 때 오른쪽(+)으로 풀며 가로로, 고개는 반대로 돌려 표적을 봄
            const string CROSS = "RA.FB=.55 RA.DU=.35 RA.TW=-.4 RF=-.6 LA.FB=.2 SP.TW=-.45 CH.TW=-.25 SP.FB=.15 LL.FB=.3 LK=-.35 ROOT=-.06 HD.TN=.2";
            const string CUT = "RA.FB=.45 RA.DU=.55 RA.TW=.3 RF=.4 LA.FB=-.3 LA.DU=.2 SP.TW=.5 CH.TW=.3 SP.FB=.25 LL.FB=.45 LK=-.35 RL.FB=-.2 ROOT=-.08 HD.TN=-.2";
            const string SLUMP = "RA.FB=.15 RA.DU=-.05 RF=.1 SP.FB=.45 HD.ND=-.25 LK=-.3 RK=-.3 SP.TW=.2 ROOT=-.07";
            Clip(idle, D3 + "Ch3M_BladeSlash", false, (0, ""), (.4f, CROSS), (.45f, CROSS + " SP.TW=-.55"), (.53f, CUT), (.75f, SLUMP), (1.3f, SLUMP), (1.45f, ""));
            const string UPPER = "RA.FB=.6 RA.DU=1 RF=.35 SP.FB=-.25 CH.FB=-.2 LA.DU=.3 LA.FB=-.3 LL.FB=.1 LK=-.05 RL.FB=-.1 HD.ND=.3 ROOT=.02";
            Clip(idle, D3 + "Ch3M_BladeEmerge", false, (0, LOWC), (.12f, UPPER), (.35f, UPPER), (.6f, SLUMP), (1.25f, SLUMP), (1.4f, ""));
            // 유리 기사: 방패를 앞세워 버팀 (0.7) → 어깨로 밀고 들어감 / 거울이 깨질 때 뒤로 휘청
            const string BRACE = "LA.FB=.6 LA.DU=.2 LF=-.65 LS.FB=.3 RA.FB=.1 RF=-.3 SP.FB=-.05 SP.TW=.25 LL.FB=.35 LK=-.45 RL.FB=-.2 RK=-.25 HD.ND=-.1 ROOT=-.08";
            const string RAM = "LA.FB=.8 LA.DU=.25 LF=-.3 LS.FB=.6 SP.FB=.4 SP.TW=.35 LL.FB=.55 LK=-.3 RL.FB=-.45 RK=.1 HD.ND=-.2 ROOT=-.1";
            Clip(idle, D3 + "Ch3M_KnightBash", false, (0, ""), (.6f, BRACE), (.7f, BRACE), (.8f, RAM), (1f, RAM), (1.2f, ""));
            Clip(idle, D3 + "Ch3M_KnightStagger", false, (0, ""), (.1f, "SP.FB=-.45 CH.FB=-.3 LA.DU=.6 RA.DU=.6 LA.FB=-.3 RA.FB=-.2 HD.ND=.35 LL.FB=-.2 RL.FB=.3 RK=-.3 ROOT=-.04"),
                (.35f, "SP.FB=.2 HD.ND=-.1 LA.DU=.1"), (.7f, ""));
            // 신기루 분신술사: 카드를 손목으로 휙 날림 / 분신을 부를 땐 팔을 활짝 펼쳤다 극장식 인사
            const string FW = "RA.FB=.35 RA.DU=.1 RA.TW=-.5 RF=-.9 RH.DU=.4 SP.TW=-.35 HD.TN=.15";
            const string FS = "RA.FB=.65 RA.DU=.45 RA.TW=.3 RF=.45 RH.DU=-.4 SP.TW=.3 LA.DU=.3 LA.FB=-.2 HD.TN=-.1";
            Clip(idle, D3 + "Ch3M_TrickFlick", false, (0, ""), (.3f, FW), (.36f, FW), (.42f, FS), (.6f, ""));
            const string SPR = "LA.DU=.75 RA.DU=.75 LA.FB=-.1 RA.FB=-.1 LF=.2 RF=.2 CH.FB=-.25 HD.ND=.25 ROOT=.02";
            const string BOW = "SP.FB=.55 CH.FB=.2 RA.FB=.55 RA.DU=-.05 RF=-.8 LA.DU=.5 LA.FB=-.4 HD.ND=-.3 RL.FB=-.2 RK=-.15 LK=-.2";
            Clip(idle, D3 + "Ch3M_TrickBow", false, (0, ""), (.25f, SPR), (.45f, SPR), (.7f, BOW), (.9f, ""));
        }

        // ======================= 4장 =======================
        static void Ch4(AnimationClip idle)
        {
            // ---- 태엽 병정 ----
            // 2등신(팔 0.41m · 어깨 너비 0.47m)이라 소총(1.53m)을 어깨에 견착하거나 두 손으로 받칠 수 없음 (재 봄, 2026-11-19 사용자: 총 쏘는 자세 이상함) →
            // 오른 겨드랑이에 끼고 오른손으로 수평 조준 + 왼손은 배의 북 테두리에. 팔 = Ch4_Soldier 리그에서 두 뼈 IK로 손바닥을 목표에 놓고 읽은 값,
            // 손목(RF.TW · RH.DU · RH.IO) = 자세마다 총 방향이 목표(조준 수평 · 반동 +12° · 들고 다님 +40° · 착검 −4°)가 되게 풀어 낸 값. 총 손잡이 각도는 EnemyModelBuilder.WeaponGrip4
            const string RAIM = "RA.DU=.43 RA.FB=.36 RA.TW=-.23 RF=-.77 RF.TW=.24";
            const string LDRUM = "LA.DU=.42 LA.FB=.31 LA.TW=-.56 LF=-.38 LF.TW=.37";
            const string AIM = RAIM + " " + LDRUM + " SP.TW=-.12 HD.ND=-.08 LL.FB=.2 LK=-.15";
            const string KICK = AIM + " RA.FB=.2 RF=-.95 RF.TW=.07 RH.DU=.06 RH.IO=.14 SP.FB=-.15 HD.ND=.06 ROOT=-.01";   // 반동: 총구 +12°, 팔이 뒤로 밀림
            // 들고 다니는 자세 (받들어총): 팔꿈치를 굽혀 손을 허리에, 총구는 앞 위 40° — 전용 idle · 행군 걸음과 모든 동작의 시작 · 끝
            const string REST = "RA.DU=.42 RA.FB=.07 RA.TW=.1 RF=-.87 RF.TW=.02 RH.DU=.67 RH.IO=-.07";
            const string DUP = REST + " LA.DU=.69 LA.FB=.38 LA.TW=-.25 LF=-1.02 LF.TW=.05 SP.LR=-.06 HD.ND=-.1";   // 왼손을 들어 올림
            const string DHIT = REST + " LA.DU=.26 LA.FB=.45 LA.TW=-.6 LF=-.17 LF.TW=.5 SP.FB=.08 ROOT=-.02";      // 배의 북 테두리를 침
            // 단발: 받들어총 → 겨드랑이에 끼고 수평 조준 (0.5) → 총구가 튀는 반동
            Clip(idle, D4 + "Ch4M_SoldierShot", false, (0, REST), (.15f, AIM), (.5f, AIM), (.55f, KICK), (.8f, REST));
            // 일제 사격 (북 치는 병정): 왼손으로 북 두 번 (0.12 · 0.42) → 조준 → 3연발 반동 (0.8 · 0.95 · 1.1)
            Clip(idle, D4 + "Ch4M_SoldierDrum", false, (0, REST), (.06f, DUP), (.12f, DHIT), (.3f, DUP), (.42f, DHIT), (.62f, AIM), (.8f, AIM), (.83f, KICK), (.93f, AIM), (.98f, KICK), (1.08f, AIM), (1.13f, KICK), (1.25f, REST));
            // 일제 사격 (나머지): 처음부터 조준 → 3연발
            Clip(idle, D4 + "Ch4M_SoldierVolley", false, (0, REST), (.2f, AIM), (.8f, AIM), (.83f, KICK), (.93f, AIM), (.98f, KICK), (1.08f, AIM), (1.13f, KICK), (1.25f, REST));
            // 착검 돌격: 옆구리로 당겨 웅크림(오른손바닥 앞 0.30, 총 −4°) → 팔을 쭉 뻗어 찌르며 돌진(앞 0.50, 총 −3°, 왼팔은 뒤로 균형) → 찌른 채 빈틈
            const string BCR = "RA.DU=.44 RA.FB=.05 RA.TW=-.25 RF=-1.07 RF.TW=.12 RH.DU=.09 RH.IO=.14 " + LDRUM + " SP.FB=.3 LL.FB=.35 LK=-.5 RL.FB=-.15 RK=-.3 HD.ND=.12 ROOT=-.09";
            const string BTH = "RA.DU=.31 RA.FB=.65 RA.TW=-.35 RF=-.05 RF.TW=.15 RH.DU=.37 RH.IO=.02 LA.DU=.35 LA.FB=-.35 LF=-.2 SP.FB=.4 SP.TW=-.25 LL.FB=.55 LK=-.2 RL.FB=-.4 RK=.1 HD.ND=.15 ROOT=-.06";
            Clip(idle, D4 + "Ch4M_SoldierBayonet", false, (0, REST), (.6f, BCR), (.68f, BTH), (.94f, BTH), (1.45f, REST));
            // 전용 idle · 행군: 고양이 idle(숨쉬기) · run 그대로 + 오른팔만 받들어총으로 고정 (총이 바닥을 찌르지 않게)
            HoldArm(idle, D4 + "Ch4M_SoldierIdle", REST);
            HoldArm(CatClip("run"), D4 + "Ch4M_SoldierMarch", REST + " RF.TW=.13 RH.DU=.83 RH.IO=1.03");   // 달리기는 상체가 앞으로 숙어 손목을 더 젖힘 (총 32~42° 유지)

            // ---- 무기 든 팔 (2026-11-19, 소총과 같은 방법으로 리그에서 풂): 손바닥 자리(어깨 기준 · 팔 길이 배수)를 두 뼈 IK로 놓고,
            //      무기 손잡이 각도(EnemyModelBuilder.WeaponGrip4)는 그 적의 공격 자세 전체에 가장 맞게, 남는 차이는 손목(±0.9 안)으로. 아래 값은 그 결과 (.FB는 + = 앞 표기)
            const string RG_RAISE = "RS.DU=0 RS.FB=0 RA.DU=1.52 RA.FB=-0.26 RA.TW=0.86 RF=-0.34 RF.TW=-0.65 RH.DU=-0.01 RH.IO=-0.24";      // 망치를 머리 위 뒤로 젖힘
            const string RG_STRIKE = "RS.DU=0 RS.FB=0 RA.DU=0.34 RA.FB=0.7 RA.TW=-0.33 RF=-0.5 RF.TW=0.42 RH.DU=0.01 RH.IO=0.25";        // 앞 아래 종으로 내려침
            const string RGL_RAISE = "LS.DU=0 LS.FB=0 LA.DU=0.4 LA.FB=0.31 LA.TW=-0.56 LF=-0.49 LF.TW=0.24 LH.DU=-0.02 LH.IO=0.06";      // 종(입이 아래)을 가슴 앞에
            const string RGL_STRIKE = "LS.DU=0 LS.FB=0 LA.DU=0.45 LA.FB=0.53 LA.TW=-0.51 LF=-0.42 LF.TW=0.24 LH.DU=-0.02 LH.IO=-0.01";
            const string AS_PULL = "RS.DU=0 RS.FB=0 RA.DU=0.59 RA.FB=0.07 RA.TW=-1.65 RF=-0.85 RF.TW=-0.53 RH.DU=-0.9 RH.IO=0";          // 단검을 허리 뒤로 당김 (칼끝은 앞)
            const string AS_THR = "RS.DU=0 RS.FB=0 RA.DU=0.39 RA.FB=0.83 RA.TW=-0.34 RF=-0.11 RF.TW=0.41 RH.DU=0.53 RH.IO=-0.32";         // 쭉 뻗어 찌름 (칼끝 수평 앞)
            const string AS_REV = "RS.DU=0 RS.FB=0 RA.DU=0.24 RA.FB=0.27 RA.TW=-0.35 RF=-1 RF.TW=0.58 RH.DU=-0.52 RH.IO=-0.9";            // 역수: 칼끝 아래
            const string AS_WHIP = "RS.DU=0 RS.FB=0 RA.DU=0.09 RA.FB=0.73 RA.TW=-0.6 RF=-0.12 RF.TW=0.64 RH.DU=0.25 RH.IO=-0.5";         // 앞 아래로 내리꽂음
            const string KN_WR = "RS.DU=0 RS.FB=0 RA.DU=0.78 RA.FB=-0.03 RA.TW=1.32 RF=-0.68 RF.TW=-0.58 RH.DU=0 RH.IO=-0.5";             // 낫을 오른쪽 뒤 위로
            const string KN_SR = "RS.DU=0 RS.FB=0 RA.DU=0.06 RA.FB=0.84 RA.TW=-0.39 RF=-0.54 RF.TW=-0.08 RH.DU=0.01 RH.IO=0.02";        // 앞을 가로질러 왼쪽 앞으로
            const string KN_WL = "RS.DU=0 RS.FB=0 RA.DU=-0.14 RA.FB=0.95 RA.TW=-0.37 RF=-0.75 RF.TW=0.14 RH.DU=0.3 RH.IO=-0.83";         // 몸 앞 왼쪽으로 감음 (낫 머리 왼쪽 뒤)
            const string KN_SL = "RS.DU=0 RS.FB=0 RA.DU=0.58 RA.FB=0.56 RA.TW=-0.11 RF=-0.7 RF.TW=-0.21 RH.DU=-0.5 RH.IO=0.29";          // 오른쪽 앞으로 풀어냄
            const string KN_COIL = "RS.DU=0 RS.FB=0 RA.DU=0.51 RA.FB=-0.11 RA.TW=-1.54 RF=-0.68 RF.TW=0.59 RH.DU=-0.17 RH.IO=-0.2";      // 낫을 등 뒤 낮게
            const string KN_OUT = "RS.DU=0 RS.FB=0 RA.DU=0.73 RA.FB=0.16 RA.TW=0.52 RF=-0.27 RF.TW=-0.21 RH.DU=-0.1 RH.IO=0.19";         // 팔을 옆으로 쭉, 낫이 바깥을 향함
            const string RP_REACH = "RS.DU=0 RS.FB=0 RA.DU=0.27 RA.FB=0.76 RA.TW=-0.38 RF=-0.4 RF.TW=0.16 RH.DU=0.13 RH.IO=0.31";        // 렌치를 대상 쪽으로 뻗음
            const string RP_BACK = "RS.DU=0 RS.FB=0 RA.DU=1.27 RA.FB=-0.56 RA.TW=1.22 RF=-0.89 RF.TW=-0.19 RH.DU=-0.19 RH.IO=-0.43";     // 머리 뒤로 젖힘 (렌치 위 뒤)
            const string RP_THROW = "RS.DU=0 RS.FB=0 RA.DU=1.03 RA.FB=1.17 RA.TW=-0.11 RF=-0.53 RF.TW=-0.05 RH.DU=0.01 RH.IO=-0.31";     // 위 앞으로 던져 올림

            // 들고 다니는 자세 (idle · 걷기 · 동작 시작/끝): 손을 허리 앞에 IK로 놓고, 무기는 손잡이 각도가 자연스레 가리키는 쪽을 앞 ±50° · 아래로 안 처지게만 잡음 (손목 ±0.9 안)
            const string RG_REST = "RS.DU=0 RS.FB=0 RA.DU=0.47 RA.FB=0.15 RA.TW=-0.35 RF=-0.42 RF.TW=-0.64 RH.DU=-0.23 RH.IO=0.32 LS.DU=0 LS.FB=0 LA.DU=0.61 LA.FB=0.1 LA.TW=-0.47 LF=-0.42 LF.TW=0.31 LH.DU=-0.1 LH.IO=-0.61";
            const string AS_REST = "RS.DU=0 RS.FB=0 RA.DU=0.39 RA.FB=0.19 RA.TW=-0.51 RF=-0.3 RF.TW=0.15 RH.DU=0.23 RH.IO=0.1";
            const string KN_REST = "RS.DU=0 RS.FB=0 RA.DU=0.47 RA.FB=0.21 RA.TW=-0.31 RF=-0.58 RF.TW=0.38 RH.DU=0.9 RH.IO=0.89";
            const string RP_REST = "RS.DU=0 RS.FB=0 RA.DU=0.38 RA.FB=0.17 RA.TW=-0.48 RF=-0.58 RF.TW=-0.5 RH.DU=0.12 RH.IO=0.89";
            // 달리기(고양이 run은 상체가 숙음)에서 무기 방향을 맞춘 손목
            const string RG_RUNW = " RF.TW=-0.55 RH.DU=-0.75 RH.IO=0.32 LF.TW=0.31 LH.DU=-0.1 LH.IO=-1.51";
            const string AS_RUNW = " RF.TW=-0.1 RH.DU=-0.19 RH.IO=0.16";

            // ---- 종지기 ----
            const string RAISE = "SP.FB=-.25 CH.FB=-.15 HD.ND=.2 SP.TW=.15 " + RG_RAISE + " " + RGL_RAISE;
            const string RAISE2 = RAISE + " SP.FB=-.32 CH.FB=-.22";
            const string STRIKE = "SP.FB=.35 CH.FB=.15 HD.ND=-.25 SP.TW=-.1 ROOT=-.04 " + RG_STRIKE + " " + RGL_STRIKE;
            const string RING = STRIKE + " SP.FB=.18 HD.ND=-.12 ROOT=-.02";                     // 울림에 몸이 떨림
            // 망치를 머리 위로 크게 (0.6) → 왼손 종을 내려침 → 울림에 몸이 떨림
            Clip(idle, D4 + "Ch4M_RingerRing", false, (0, RG_REST), (.45f, RAISE), (.6f, RAISE2), (.67f, STRIKE), (.8f, RING), (1f, RG_REST));
            // 강화형 2겹: 내려친 뒤 바로 다시 젖혀 0.6초 뒤 한 번 더
            Clip(idle, D4 + "Ch4M_RingerDouble", false, (0, RG_REST), (.45f, RAISE), (.6f, RAISE2), (.67f, STRIKE), (.8f, RING), (1.05f, RAISE), (1.2f, RAISE2), (1.27f, STRIKE), (1.4f, RING), (1.6f, RG_REST));

            // ---- 오르골 무희 ----
            const string GATHER = "LA.FB=.5 RA.FB=.5 LA.DU=.25 RA.DU=.25 LF=-.55 RF=-.55 LFT=-.6 RFT=-.6 CH.FB=-.1 HD.ND=.1 ROOT=.04";
            const string SPA = "LA.DU=1 LA.FB=.15 LF=-.45 RA.DU=.45 RA.FB=.2 RF=-.15 RL.FB=.35 RK=-.85 RL.IO=.35 LFT=-.6 CH.FB=-.12 HD.TN=.35 ROOT=.05";
            const string SPB = "RA.DU=1 RA.FB=.15 RF=-.45 LA.DU=.45 LA.FB=.2 LF=-.15 RL.FB=.35 RK=-.85 RL.IO=.35 LFT=-.6 CH.FB=-.12 HD.TN=-.35 ROOT=.05";
            const string FIN = "LA.DU=1.15 RA.DU=1.15 LA.FB=-.05 RA.FB=-.05 LF=.2 RF=.2 CH.FB=-.3 HD.ND=.3 LFT=-.6 RFT=-.6 ROOT=.05";
            Clip(idle, D4 + "Ch4M_DancerRise", false, (0, ""), (.4f, GATHER));
            // 피루엣 반복: 한 다리 접고 발끝으로, 두 팔이 번갈아 머리 위로 둥글게 (몸 회전은 코드)
            Clip(idle, D4 + "Ch4M_DancerSpin", true, (0, SPA), (.3f, SPB), (.6f, SPA));
            Clip(idle, D4 + "Ch4M_DancerFinale", false, (0, GATHER), (.12f, FIN), (.55f, FIN), (.7f, ""));

            // ---- 초침 자객 ----
            const string CROUCH = "SP.FB=.45 LL.FB=.4 RL.FB=.4 LK=-.65 RK=-.65 LA.FB=-.3 RA.FB=-.35 RF=-.4 HD.ND=.25 ROOT=-.12";
            const string SPRING = "SP.FB=.55 LL.FB=.15 RL.FB=-.25 LK=-.1 RK=.1 LA.FB=-.6 RA.FB=-.6 HD.ND=.3 ROOT=-.03";
            const string LOW = "SP.FB=.25 LL.FB=.2 RL.FB=.2 LK=-.3 RK=-.3 RF=-.4 ROOT=-.05";
            // 째깍 점멸: 웅크렸다 (0.18 = 째깍) 튀어 나감
            Clip(idle, D4 + "Ch4M_AssassinBlink", false, (0, AS_REST), (.18f, CROUCH + " " + AS_REST), (.22f, SPRING), (.35f, LOW + " " + AS_REST));
            // 단검을 뒤로 당길 땐 오른어깨가 뒤로 = 허리 오른쪽(+), 찌를 땐 왼쪽(−)으로 돌며 오른어깨가 앞으로
            const string PULL = "LA.FB=.55 LF=-.4 SP.TW=.45 SP.FB=.2 LL.FB=.35 LK=-.4 RL.FB=-.2 RK=-.2 HD.TN=-.2 ROOT=-.07 " + AS_PULL;
            const string THR = "LA.FB=-.35 LF=-.2 SP.TW=-.35 SP.FB=.4 LL.FB=.55 LK=-.3 RL.FB=-.45 RK=.15 HD.TN=.1 ROOT=-.09 " + AS_THR;
            // 찌르기: 단검을 뒤로 당김 (0.5) → 쭉 뻗음
            Clip(idle, D4 + "Ch4M_AssassinStab", false, (0, AS_REST), (.45f, PULL), (.5f, PULL + " SP.TW=.5"), (.56f, THR), (.9f, THR));
            // 숨 고르기 빈틈: 숙인 채 어깨로 두 번 들썩
            const string BIN = "SP.FB=.35 CH.FB=.15 LS.DU=.2 RS.DU=.2 LA.FB=.15 RA.FB=.2 RF=-.3 HD.ND=-.2 LK=-.2 RK=-.2 ROOT=-.05";
            const string BOUT = "SP.FB=.42 CH.FB=.15 LA.FB=.15 RA.FB=.2 RF=-.3 HD.ND=-.3 LK=-.2 RK=-.2 ROOT=-.06";
            Clip(idle, D4 + "Ch4M_AssassinBreath", false, (0, BOUT + " " + AS_REST), (.25f, BIN + " " + AS_REST), (.5f, BOUT + " " + AS_REST), (.75f, BIN + " " + AS_REST), (1f, BOUT + " " + AS_REST));
            // 뒤 찌르기: 역수로 쥐고 등을 돌린 채 웅크림 (0.6) → 몸을 채찍처럼 돌리며 찌름 (반 바퀴는 코드)
            const string REV = "LA.FB=-.3 SP.TW=-.6 CH.TW=-.3 SP.FB=.3 LK=-.45 RK=-.45 LL.FB=.3 RL.FB=.3 HD.TN=-.4 ROOT=-.1 " + AS_REV;
            const string WHIP = "LA.FB=-.5 SP.TW=.55 CH.TW=.3 SP.FB=.45 LL.FB=.5 LK=-.3 RL.FB=-.3 HD.TN=.1 ROOT=-.08 " + AS_WHIP;
            Clip(idle, D4 + "Ch4M_AssassinBackStab", false, (0, AS_REST), (.55f, REV), (.6f, REV + " SP.TW=-.7"), (.72f, WHIP), (1f, WHIP));

            // ---- 진자 기사 (낫 = 오른손) ----
            // 오른쪽: 낫을 오른쪽 뒤로 젖힘 = 허리 오른쪽(+) → 앞을 가로질러 왼쪽(−)으로 / 왼쪽: 몸 앞으로 감음 = 허리 왼쪽(−) → 오른쪽(+)으로 풀며 역방향. 고개는 표적에 남김
            const string WR = "SP.TW=.7 CH.TW=.35 LA.FB=.5 LA.DU=.3 LF=-.6 LL.FB=.25 LK=-.3 RL.FB=-.1 HD.TN=-.25 ROOT=-.05 " + KN_WR;
            const string SR = "SP.TW=-.6 CH.TW=-.3 LA.FB=-.2 LA.DU=.15 LF=-.3 RL.FB=.25 RK=-.3 HD.TN=.2 SP.FB=.2 ROOT=-.07 " + KN_SR;
            const string WL = "SP.TW=-.7 CH.TW=-.35 LA.FB=.3 LF=-.5 RL.FB=.25 RK=-.3 HD.TN=.25 ROOT=-.05 " + KN_WL;
            const string SL = "SP.TW=.55 CH.TW=.3 LA.FB=-.2 LA.DU=.2 LL.FB=.25 LK=-.3 HD.TN=-.2 SP.FB=.2 ROOT=-.07 " + KN_SL;
            // 오른쪽: 낫을 오른쪽 뒤로 크게 젖힘 (0.5) → 앞을 가로질러 큰 호 → 다음 휘두름으로 이어지는 자세 / 왼쪽: 몸 앞으로 감아 역방향
            Clip(idle, D4 + "Ch4M_KnightSwingR", false, (0, KN_REST), (.42f, WR), (.5f, WR + " SP.TW=.8"), (.62f, SR), (.85f, SR + " SP.TW=-.4"), (1.2f, KN_REST));
            Clip(idle, D4 + "Ch4M_KnightSwingL", false, (0, KN_REST), (.42f, WL), (.5f, WL + " SP.TW=-.8"), (.62f, SL), (.85f, SL + " SP.TW=.35"), (1.2f, KN_REST));
            // 회전 베기: 낫을 등 뒤로 감고 깊이 웅크림 (0.8) → 팔을 펴 한 바퀴 (회전은 코드)
            const string KCOIL = "SP.TW=-.8 CH.TW=-.4 LA.FB=-.3 LA.DU=.3 LL.FB=.4 RL.FB=.4 LK=-.6 RK=-.6 SP.FB=.3 HD.TN=.3 ROOT=-.13 " + KN_COIL;
            const string KOUT = "SP.TW=.3 LA.DU=.55 LF=.3 LL.FB=.3 RL.FB=.1 LK=-.3 RK=-.3 SP.FB=.1 ROOT=-.08 " + KN_OUT;
            Clip(idle, D4 + "Ch4M_KnightWhirl", false, (0, KN_REST), (.7f, KCOIL), (.8f, KCOIL + " ROOT=-.16"), (.9f, KOUT), (1.12f, KOUT), (1.4f, KN_REST));
            Ch2Motion.Walk(idle, D4 + "Ch4M_BaseKnightStep", 1.1f, 0.3f, 0.14f, 0.12f, neutralLegs: true);   // 무거운 쇠 걸음 (바탕). 다리는 똑바로 선 값 기준 (대기 자세 기준이면 왼발이 뒤에 남아 안 걷는 것처럼 보임)
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D4 + "Ch4M_BaseKnightStep.anim"), D4 + "Ch4M_KnightWalk", KN_REST);   // + 낫 든 팔 고정
            HoldArm(idle, D4 + "Ch4M_KnightIdle", KN_REST);

            // ---- 시계 수리공 ----
            const string REA = "LA.FB=-.1 LA.DU=-.05 LF=-.8 LH.IO=.3 SP.FB=.15 HD.ND=-.05 " + RP_REACH + " RF.TW=.6";   // 손목을 감았다
            const string REB = REA + " RF.TW=-.3";                                                                          // 풀었다 (렌치가 돎)
            // 태엽 감기: 렌치를 대상 쪽으로 뻗어 손목을 빠르게 돌림
            Clip(idle, D4 + "Ch4M_RepairWind", false, (0, RP_REST), (.12f, REA), (.22f, REB), (.32f, REA), (.42f, REB), (.52f, REA), (.62f, REB), (.72f, REA), (.95f, RP_REST));
            // 렌치 곡사: 뒤로 젖혀 (0.3) 위로 던져 올림
            const string TB = "LA.FB=.4 LA.DU=.2 SP.FB=-.35 CH.FB=-.2 HD.ND=.3 LL.FB=.25 RL.FB=-.15 RK=-.2 " + RP_BACK;
            const string TT = "LA.FB=-.2 SP.FB=.25 CH.FB=.1 HD.ND=.2 LL.FB=.3 LK=-.25 ROOT=.02 " + RP_THROW;
            Clip(idle, D4 + "Ch4M_RepairToss", false, (0, RP_REST), (.26f, TB), (.3f, TB + " SP.FB=-.4"), (.38f, TT), (.65f, RP_REST));
            Ch2Motion.Walk(idle, D4 + "Ch4M_BaseRepairStep", 0.42f, 0.16f, 0.06f, 0.3f);   // 도망 종종걸음 (몸을 웅크리고 잔걸음)
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D4 + "Ch4M_BaseRepairStep.anim"), D4 + "Ch4M_RepairScurry", RP_REST);
            HoldArm(idle, D4 + "Ch4M_RepairIdle", RP_REST);
            // 종지기 · 자객 전용 idle · 달리기 (무기 든 팔 고정)
            HoldArm(idle, D4 + "Ch4M_RingerIdle", RG_REST);
            HoldArm(CatClip("run"), D4 + "Ch4M_RingerRun", RG_REST + RG_RUNW);
            HoldArm(idle, D4 + "Ch4M_AssassinIdle", AS_REST);
            HoldArm(CatClip("run"), D4 + "Ch4M_AssassinRun", AS_REST + AS_RUNW);
        }

        // ======================= 5장 (2026-11-19 사용자: 이전 모션 재활용 말고 다른 느낌으로) =======================
        // '붕괴' 테마: 동작 사이사이에 몸이 깨진 거울처럼 한 프레임씩 튀는 글리치(0.02~0.04초 어긋난 자세)와, 떠 있는 듯 기우뚱한 흔들림.
        // idle · 걸음도 고양이 클립을 빌리지 않고 이 장 전용으로 만듦 (걸음 = Ch2Motion.Walk 생성기 + 무기 든 팔 자세).
        // 중간 보스 거울 고양이만 예외: 거울에 비친 분신이라 고양이 기본 모션을 그대로 따라 함 (2026-11-19 사용자)
        static string G(string pose, string glitch) => pose + " " + glitch;      // 글리치 = 같은 자세에 어긋난 값 덧씀
        static void Ch5(AnimationClip idle)
        {
            System.IO.Directory.CreateDirectory(D5);
            // ---- 파편 사수 (유리 소총: 오른손, 총구 앞) ----
            const string GN_REST = "RA.DU=.35 RA.FB=.28 RA.TW=-.3 RF=-.55 RH.DU=.15 LA.DU=.2 LA.FB=.15 LF=-.3";
            Clip(idle, D5 + "Ch5M_GunnerIdle", true, (0, GN_REST + " HD.TL=.07 SP.LR=.04"), (.9f, GN_REST + " HD.TL=-.05 SP.LR=-.03 CH.FB=-.05"),
                 (.93f, G(GN_REST, "HD.TL=.16 HD.TN=.18 SP.LR=.08")), (.97f, GN_REST + " HD.TL=-.05"), (1.9f, GN_REST + " HD.TL=.07 SP.LR=.04"));
            const string GN_AIM = "RA.DU=.42 RA.FB=.62 RA.TW=-.25 RF=-.22 RH.DU=.05 LA.DU=.32 LA.FB=.5 LF=-.45 SP.TW=-.15 HD.ND=-.05 LL.FB=.25 LK=-.2 RL.FB=-.1";
            const string GN_KICK = GN_AIM + " RA.FB=.35 RF=-.6 SP.FB=-.22 HD.ND=.14 ROOT=-.02";
            // 단발: 겨눔(0.6 예고) → 반동 순간 몸이 두 번 어긋나 튐 → 제자리
            Clip(idle, D5 + "Ch5M_GunnerShot", false, (0, GN_REST), (.18f, GN_AIM), (.58f, GN_AIM), (.61f, G(GN_KICK, "SP.LR=.12 HD.TL=.15")), (.64f, G(GN_KICK, "SP.LR=-.1 HD.TL=-.12")), (.68f, GN_KICK), (.85f, GN_REST));
            Ch2Motion.Walk(idle, D5 + "Ch5M_BaseGunnerStep", 0.62f, 0.2f, 0.07f, 0.06f);
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D5 + "Ch5M_BaseGunnerStep.anim"), D5 + "Ch5M_GunnerWalk", GN_REST);

            // ---- 별자리 마녀 (별 지팡이: 오른손, 곧게 세움) ----
            const string WT_REST = "RA.DU=.3 RA.FB=.22 RF=-.9 RH.DU=.1 LA.DU=.25 LA.FB=.2 LF=-.6";
            // 떠 있는 듯 둥실 (몸 높이 +0.04, 좌우로 기우뚱)
            Clip(idle, D5 + "Ch5M_WitchIdle", true, (0, WT_REST + " SP.LR=.05 HD.TL=.06"), (1.2f, WT_REST + " SP.LR=-.05 HD.TL=-.06 ROOT=.04 CH.FB=-.06"), (2.4f, WT_REST + " SP.LR=.05 HD.TL=.06"));
            const string WT_UP = "RA.DU=1.05 RA.FB=.35 RF=-.2 LA.DU=.55 LA.FB=.45 LF=-.15 CH.FB=-.2 HD.ND=.25 ROOT=.04";
            const string WT_TAP = "RA.DU=.65 RA.FB=.75 RF=-.05 LA.DU=.4 LA.FB=.3 SP.FB=.15 HD.ND=-.1 ROOT=-.02";
            // 별 박기: 지팡이를 들었다 콕 (별 3개 = 0.33초 간격)
            Clip(idle, D5 + "Ch5M_WitchCast", false, (0, WT_REST), (.1f, WT_UP), (.2f, WT_TAP + " SP.TW=.25"), (.33f, WT_UP), (.43f, WT_TAP + " SP.TW=-.25"), (.56f, WT_UP), (.66f, WT_TAP), (.69f, G(WT_TAP, "HD.TL=.15 SP.LR=.1")), (.95f, WT_REST));
            const string WT_SPREAD = "RA.DU=1 RA.FB=-.1 LA.DU=1 LA.FB=-.1 RF=.2 LF=.2 CH.FB=-.3 HD.ND=.3 ROOT=.05";
            // 선 잇기: 두 팔을 활짝 펴 하늘을 가르듯
            Clip(idle, D5 + "Ch5M_WitchLink", false, (0, WT_REST), (.15f, WT_SPREAD), (.18f, G(WT_SPREAD, "SP.TW=.2 HD.TN=.2")), (.45f, WT_SPREAD + " SP.TW=-.15"), (.65f, WT_REST));
            Ch2Motion.Walk(idle, D5 + "Ch5M_BaseWitchStep", 0.7f, 0.14f, 0.1f, 0.02f);
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D5 + "Ch5M_BaseWitchStep.anim"), D5 + "Ch5M_WitchWalk", WT_REST);

            // ---- 붕괴 거인 (망치: 오른손, 앞아래로 늘어뜨려 끔) ----
            const string RG5_REST = "RA.DU=.15 RA.FB=.22 RF=-.2 LA.DU=.2 LA.FB=.1 LF=-.25 SP.FB=.14";
            Clip(idle, D5 + "Ch5M_GiantIdle", true, (0, RG5_REST + " SP.FB=.2 CH.FB=.08 HD.ND=-.1 LS.DU=.1 RS.DU=.1"), (1.4f, RG5_REST + " SP.FB=.1 HD.ND=.02"),
                 (2.0f, G(RG5_REST, "SP.LR=.1 HD.TL=.12 SP.FB=.12")), (2.05f, RG5_REST + " SP.FB=.12"), (2.8f, RG5_REST + " SP.FB=.2 CH.FB=.08 HD.ND=-.1 LS.DU=.1 RS.DU=.1"));
            const string RG5_LIFT = "RA.DU=1.5 RA.FB=-.2 RF=-.3 LA.DU=1.3 LA.FB=-.1 LF=-.2 SP.FB=-.35 CH.FB=-.25 HD.ND=.3 LL.FB=.2 LK=-.2 ROOT=.03";
            const string RG5_SMASH = "RA.DU=.1 RA.FB=.9 RF=.1 LA.DU=.2 LA.FB=.8 LF=.05 SP.FB=.55 CH.FB=.25 HD.ND=-.2 LL.FB=.5 LK=-.5 RL.FB=-.2 RK=-.2 ROOT=-.18";
            // 내리찍기: 머리 위로 들어 올림(0.8 예고) → 내리꽂는 순간 바닥이 깨지듯 몸이 두 번 떨림 → 박힌 채 버팀
            Clip(idle, D5 + "Ch5M_GiantSlam", false, (0, RG5_REST), (.5f, RG5_LIFT), (.78f, RG5_LIFT + " SP.FB=-.45"), (.84f, RG5_SMASH), (.87f, G(RG5_SMASH, "SP.LR=.12 ROOT=-.22")), (.9f, G(RG5_SMASH, "SP.LR=-.1")), (1.2f, RG5_SMASH), (1.45f, RG5_REST));
            const string RG5_COIL = "SP.FB=.5 RA.FB=-.4 LA.FB=-.4 LL.FB=.5 LK=-.6 RL.FB=-.1 RK=-.4 HD.ND=.3 ROOT=-.12";
            const string RG5_RAM = "SP.FB=.6 RA.FB=.5 RA.DU=.2 LA.FB=-.6 LL.FB=.7 LK=-.2 RL.FB=-.5 RK=.1 HD.ND=.2 ROOT=-.05";
            Clip(idle, D5 + "Ch5M_GiantDash", false, (0, RG5_REST), (.55f, RG5_COIL), (.62f, RG5_RAM), (1.05f, RG5_RAM), (1.5f, RG5_REST));
            Ch2Motion.Walk(idle, D5 + "Ch5M_BaseGiantStep", 1.2f, 0.26f, 0.16f, 0.16f);   // 무겁게 끄는 걸음
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D5 + "Ch5M_BaseGiantStep.anim"), D5 + "Ch5M_GiantWalk", RG5_REST, 1.3f);   // 망치 든 채로 팔도 크게 흔듦

            // ---- 균열 자객 (균열 칼 두 자루, 앞으로 겨눔) ----
            const string RA5_ARMS = "RA.DU=.35 RA.FB=.32 RF=-.5 LA.DU=.35 LA.FB=.32 LF=-.5";
            const string RA5_REST = RA5_ARMS + " SP.FB=.25 LK=-.25 RK=-.25 LL.FB=.15 RL.FB=.15 ROOT=-.05";
            // 경련하듯 고개를 홱홱 돌리며 웅크림
            Clip(idle, D5 + "Ch5M_AssassinIdle", true, (0, RA5_REST), (.5f, RA5_REST + " HD.TN=.3"), (.53f, G(RA5_REST, "HD.TN=-.35 HD.TL=.2")), (1.2f, RA5_REST + " SP.LR=.08"), (1.6f, RA5_REST));
            const string RA5_SINK = "SP.FB=.75 LL.FB=.65 RL.FB=.65 LK=-.95 RK=-.95 RA.FB=-.5 LA.FB=-.5 HD.ND=-.3 ROOT=-.32";
            Clip(idle, D5 + "Ch5M_AssassinDive", false, (0, RA5_REST), (.2f, RA5_SINK), (.23f, G(RA5_SINK, "SP.LR=.15")), (.45f, RA5_SINK + " ROOT=-.5"));
            const string RA5_BURST = "SP.FB=-.2 CH.FB=-.2 RA.DU=1.2 RA.FB=.6 LA.DU=1.2 LA.FB=.6 RF=.2 LF=.2 HD.ND=.2 ROOT=.06";
            const string RA5_CROSS = "SP.FB=.45 RA.DU=.2 RA.FB=.95 RA.TW=.4 LA.DU=.2 LA.FB=.95 LA.TW=.4 LL.FB=.4 LK=-.3 ROOT=-.08";
            // 튀어나오기: 균열 아래에서 솟구치며 X자로 올려 베고 → 내려 벰
            Clip(idle, D5 + "Ch5M_AssassinEmerge", false, (0, RA5_SINK), (.08f, RA5_BURST), (.1f, G(RA5_BURST, "SP.TW=.3")), (.18f, RA5_CROSS), (.5f, RA5_CROSS), (1.4f, RA5_REST));
            const string RA5_WIND = "RA.DU=.9 RA.FB=-.1 LA.DU=.9 LA.FB=-.1 SP.TW=.35 SP.FB=.1 LK=-.3 RK=-.3";
            const string RA5_CUT = "RA.DU=.3 RA.FB=.95 LA.DU=.3 LA.FB=.95 SP.TW=-.35 SP.FB=.4 LL.FB=.5 LK=-.3 RL.FB=-.2 ROOT=-.08";
            Clip(idle, D5 + "Ch5M_AssassinSlash", false, (0, RA5_REST), (.38f, RA5_WIND), (.45f, RA5_WIND + " SP.TW=.45"), (.5f, RA5_CUT), (.53f, G(RA5_CUT, "HD.TL=.2 SP.LR=.1")), (.9f, RA5_CUT));
            const string RA5_PANT = "SP.FB=.45 LS.DU=.25 RS.DU=.25 RA.FB=.2 LA.FB=.2 HD.ND=-.25 LK=-.3 RK=-.3 ROOT=-.07";
            Clip(idle, D5 + "Ch5M_AssassinBreath", false, (0, RA5_PANT), (.25f, RA5_PANT + " LS.DU=0 RS.DU=0 HD.ND=-.35"), (.5f, RA5_PANT), (.52f, G(RA5_PANT, "HD.TN=.3")), (.75f, RA5_PANT + " LS.DU=0 RS.DU=0"), (1f, RA5_PANT));
            Ch2Motion.Walk(idle, D5 + "Ch5M_BaseAssassinStep", 0.38f, 0.24f, 0.05f, 0.25f);   // 낮게 웅크린 빠른 잔걸음
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D5 + "Ch5M_BaseAssassinStep.anim"), D5 + "Ch5M_AssassinRun", RA5_ARMS);

            // ---- 균열술사 (균열 향로: 오른손에 늘어뜨림, 왼손은 가슴에) ----
            const string CS_REST = "RA.DU=.4 RA.FB=.35 RF=-.3 LA.DU=.3 LA.FB=.3 LF=-.85 CH.FB=.1 HD.ND=-.1";
            // 향로를 시계추처럼 흔듦
            Clip(idle, D5 + "Ch5M_CasterIdle", true, (0, CS_REST + " RA.FB=.22"), (.8f, CS_REST + " RA.FB=.5 SP.LR=.04"), (1.6f, CS_REST + " RA.FB=.22"));
            const string CS_RAISE = "RA.DU=.95 RA.FB=.8 RF=.1 LA.DU=.9 LA.FB=.6 LF=.1 CH.FB=-.2 HD.ND=.2 ROOT=.02";
            const string CS_TEAR = "RA.DU=.6 RA.FB=.9 LA.DU=.45 LA.FB=.9 LH.DU=-.5 SP.FB=.2 HD.ND=-.05";
            // 균열 열기: 두 손을 앞으로 뻗어 허공을 찢음
            Clip(idle, D5 + "Ch5M_CasterOpen", false, (0, CS_REST), (.2f, CS_RAISE), (.32f, CS_TEAR), (.35f, G(CS_TEAR, "SP.LR=.12 HD.TL=.15")), (.6f, CS_REST));
            const string CS_BACK = "RA.DU=.9 RA.FB=-.6 RF=-.1 SP.FB=-.3 SP.TW=.35 HD.ND=.2 LA.FB=.4";
            const string CS_FLING = "RA.DU=1.1 RA.FB=1 RF=.2 SP.FB=.25 SP.TW=-.3 LA.FB=-.2";
            Clip(idle, D5 + "Ch5M_CasterLob", false, (0, CS_REST), (.26f, CS_BACK), (.32f, CS_FLING), (.55f, CS_REST));
            Ch2Motion.Walk(idle, D5 + "Ch5M_BaseCasterStep", 0.45f, 0.15f, 0.08f, 0.22f);   // 도망치는 구부정한 걸음
            HoldArm(AssetDatabase.LoadAssetAtPath<AnimationClip>(D5 + "Ch5M_BaseCasterStep.anim"), D5 + "Ch5M_CasterWalk", CS_REST);
            // ---- 보스 '시간의 주인' (대낫: 오른손, 자루를 세워 듦) — 2026-11-20 ----
            // 유령 돌진 베기 (사용자: 낫을 크게 뒤로 들었다가 앞으로 베어 넘기며 유령처럼 돌진, 모션 가감속):
            //   키 간격으로 가감속 — 들어 올림은 키를 넓게(천천히 출발해 점점 빨리), 꼭대기에서 0.25초 멈칫(예비 동작), 베기는 0.07초(번개처럼),
            //   넘어간 뒤엔 과하게 넘어갔다(오버슈트) 미끄러지며 감속 → 천천히 제자리. 곡선 접선 = ClampedAuto라 키 사이가 부드럽게 가속 · 감속
            const string BS_REST = "RA.DU=.3 RA.FB=.25 RF=-.6 RH.DU=.1 LA.DU=.25 LA.FB=.1 LF=-.4 SP.FB=.05";
            // 떠다님 (2026-11-20 사용자: "막보 떠다니는 느낌을 줘, 왜 걸어가냐"): 발을 바닥에 딛지 않음 — 다리를 모아 발끝을 아래로 늘어뜨리고 무릎은 살짝 굽힘.
            // 몸이 떠오를 때 다리는 늦게 따라 뒤로 흩날림 (옷자락처럼). 위아래 둥실거림 · 이동 방향 기울기는 런타임 HoverFloat
            const string BS_LEGS = " LL.FB=.14 RL.FB=.02 LK=-.32 RK=-.14 LFT=-.65 RFT=-.65";
            const string BS_LEGS2 = " LL.FB=.04 RL.FB=-.08 LK=-.22 RK=-.3 LFT=-.6 RFT=-.7";
            Clip(idle, D5 + "Ch5M_BossIdle", true, (0, BS_REST + BS_LEGS + " SP.LR=.04 HD.ND=-.05"), (1.6f, BS_REST + BS_LEGS2 + " SP.LR=-.04 HD.ND=.02 CH.FB=-.05"), (3.2f, BS_REST + BS_LEGS + " SP.LR=.04 HD.ND=-.05"));
            // 2026-11-20 사용자: 베어 넘길 때 수평으로, 양손으로 휘두르며 → 낫을 오른쪽 뒤로 수평하게 감음(왼손이 몸을 가로질러 자루를 함께 잡음, 허리를 오른쪽으로 비틂)
            //   → 몸을 왼쪽으로 풀며 양손으로 앞을 수평하게 쓸어 넘김 → 왼쪽까지 넘어간 자세로 미끄러짐. 손목(RF.TW · RH.*)은 자루가 눕도록
            const string BS_WIND1 = "RA.DU=.6 RA.FB=-.05 RA.TW=.2 RF=-.35 RF.TW=-.4 RH.IO=.3 LA.DU=.5 LA.FB=.35 LA.TW=-.2 LF=-.35 SP.TW=.35 CH.TW=.15 HD.TN=-.1 ROOT=-.02";
            const string BS_WIND = "RA.DU=.78 RA.FB=-.38 RA.TW=.35 RF=-.25 RF.TW=-.85 RH.DU=-.2 RH.IO=.6 LA.DU=.68 LA.FB=.6 LA.TW=-.35 LF=-.3 LH.IO=-.4 SP.TW=.72 CH.TW=.38 SP.FB=-.05 HD.TN=-.25 LL.FB=.3 LK=-.3 RL.FB=-.15 RK=-.15 ROOT=-.06";
            const string BS_TOP = BS_WIND + " SP.TW=.88 CH.TW=.46 ROOT=-.09";
            Clip(idle, D5 + "Ch5M_BossReapWind", false, (0, BS_REST), (.12f, BS_REST + " SP.FB=.1 ROOT=-.03"), (.5f, BS_WIND1), (.85f, BS_WIND), (1.1f, BS_TOP));
            const string BS_CUT = "RA.DU=.72 RA.FB=.92 RA.TW=-.2 RF=-.05 RF.TW=-.85 RH.DU=-.2 RH.IO=.6 LA.DU=.62 LA.FB=.45 LA.TW=.2 LF=-.2 LH.IO=-.4 SP.TW=-.55 CH.TW=-.3 SP.FB=.25 HD.TN=.15 LL.FB=.55 LK=-.4 RL.FB=-.4 RK=-.1 ROOT=-.12";
            const string BS_OVER = BS_CUT + " SP.TW=-.82 CH.TW=-.46 RA.FB=.62 RA.DU=.8 LA.FB=-.05 LA.DU=.55 HD.TN=.3 ROOT=-.16";
            Clip(idle, D5 + "Ch5M_BossReapDash", false, (0, BS_TOP), (.07f, BS_CUT), (.09f, G(BS_CUT, "SP.LR=.1")), (.3f, BS_OVER), (.42f, BS_OVER + " ROOT=-.18"), (.8f, BS_OVER + " SP.TW=-.6 ROOT=-.12"), (1.05f, BS_REST));
            const string BS_SIDE = "RA.DU=.95 RA.FB=-.3 RA.TW=.3 RF=-.3 SP.TW=.65 CH.TW=.3 LA.FB=.45 LA.DU=.3 LF=-.3 SP.FB=-.1 LL.FB=.25 LK=-.25 ROOT=-.05";
            const string BS_SWEEP = "RA.DU=.6 RA.FB=.95 RA.TW=-.3 RF=.05 SP.TW=-.6 CH.TW=-.3 LA.FB=-.3 LA.DU=.35 SP.FB=.35 RL.FB=.3 RK=-.3 ROOT=-.1";
            Clip(idle, D5 + "Ch5M_BossReap", false, (0, BS_REST), (.6f, BS_SIDE), (.9f, BS_SIDE + " SP.TW=.75"), (.97f, BS_SWEEP), (1.15f, BS_SWEEP + " SP.TW=-.75"), (1.4f, BS_REST));
            const string BS_CAST = BS_REST + " LA.DU=1.2 LA.FB=.6 LF=.2 LH.DU=-.4 CH.FB=-.2 HD.ND=.2 ROOT=.05";
            Clip(idle, D5 + "Ch5M_BossCast", false, (0, BS_REST), (.35f, BS_CAST), (.38f, G(BS_CAST, "HD.TL=.12 SP.LR=.08")), (.8f, BS_CAST), (1.1f, BS_REST));
            const string BS_PULL = BS_REST + " LA.DU=.9 LA.FB=-.5 LF=-.3 SP.TW=.4 SP.FB=-.2 HD.ND=.1";
            const string BS_THROW = BS_REST + " LA.DU=.8 LA.FB=1.0 LF=.2 SP.TW=-.4 SP.FB=.3 LL.FB=.3 ROOT=-.05";
            Clip(idle, D5 + "Ch5M_BossSpear", false, (0, BS_REST), (.8f, BS_PULL), (1.0f, BS_PULL + " SP.TW=.5"), (1.06f, BS_THROW), (1.2f, BS_REST));
            // 이동도 걸음 없이 떠서 미끄러짐: 앞으로 기울고 다리는 뒤로 흩날림 (예전: 발을 조금 떼는 걸음 Ch5M_BaseBossGlide)
            const string BS_DRIFT = BS_REST + " SP.FB=.16 HD.ND=-.06 LL.FB=-.1 RL.FB=-.22 LK=-.35 RK=-.45 LFT=-.7 RFT=-.7";
            Clip(idle, D5 + "Ch5M_BossWalk", true, (0, BS_DRIFT), (.9f, BS_DRIFT + " LL.FB=-.2 RL.FB=-.12 LK=-.45 RK=-.32 SP.LR=.04 LA.FB=.05"), (1.8f, BS_DRIFT));
        }

        // ======================= 보스 등장 연출 전용 (2026-11-21 사용자: "새로운 동작을 만들어도 됨, 연출 전용으로") =======================
        // 박자는 BossIntro 컷 길이에 맞춤 (Intro 상태는 각 보스 첫 컷 또는 표시한 컷 시작에 재생). 컷 길이를 바꾸면 여기 시각도 같이.
        static void Intros(AnimationClip idle)
        {
            // 와인더 잭 (눈 컷 시작에 재생 → 단계 줌 첫 총성 0.84): 모자 숙인 채 → 고개 번쩍 → 허리춤에서 뽑아 겨눔 + 왼손 올가미를 머리 위로 돌림
            const string WJ_DOWN = "HD.ND=-.45 SP.FB=.12 RA.FB=.05 RA.DU=-.05 RF=-.2 LA.FB=.05 LF=-.2 LL.FB=.15 LK=-.15";
            const string WJ_UP = "HD.ND=.12 CH.FB=-.1 RA.FB=.2 RA.DU=.05 RF=-.6 RH.DU=.3 LA.FB=.1 LF=-.3 LL.FB=.2 LK=-.25 ROOT=-.03";
            const string WJ_AIM = "HD.ND=.05 HD.TL=-.12 RA.FB=.95 RA.DU=.2 RF=.4 SP.TW=-.25 CH.FB=-.05 LA.DU=1.15 LA.FB=.15 LF=-.35 LL.FB=.35 LK=-.35 RL.FB=-.15 ROOT=-.06";
            const string WJ_LASSO = WJ_AIM + " LA.FB=-.25 LA.DU=1.05 LF=-.15";
            Clip(idle, D1 + "Ch1M_HunterIntro", false, (0, WJ_DOWN), (.3f, WJ_DOWN), (.4f, WJ_UP), (.72f, WJ_UP), (.8f, WJ_AIM), (.84f, G(WJ_AIM, "RA.DU=.4 CH.FB=-.15")),
                (1.0f, WJ_LASSO), (1.2f, WJ_AIM), (1.4f, WJ_LASSO), (1.6f, WJ_AIM));

            // 달리오스 (첫 컷부터, 내려찍기 2.79): 웅크린 채 깨어남 → 일어나며 포효 → 두 팔을 머리 위로 → 바닥을 내려찍음
            const string DA_HUNCH = "SP.FB=.55 CH.FB=.2 HD.ND=-.4 LA.FB=.3 RA.FB=.3 LA.DU=-.1 RA.DU=-.1 LF=-.2 RF=-.2 LK=-.45 RK=-.45 LL.FB=.3 RL.FB=.3 ROOT=-.15";
            const string DA_ROAR = "SP.FB=-.25 CH.FB=-.35 HD.ND=.45 LA.DU=.75 RA.DU=.75 LA.FB=-.2 RA.FB=-.2 LF=.2 RF=.2 ROOT=.02";
            const string DA_OVER = "LA.DU=1.15 RA.DU=1.15 LA.FB=.3 RA.FB=.3 LF=-.3 RF=-.3 SP.FB=-.3 CH.FB=-.2 HD.ND=.3 ROOT=.03";
            const string DA_SLAM = "LA.DU=-.1 RA.DU=-.1 LA.FB=.85 RA.FB=.85 LF=.25 RF=.25 SP.FB=.65 CH.FB=.25 HD.ND=-.2 LL.FB=.45 RL.FB=.45 LK=-.6 RK=-.6 ROOT=-.16";
            Clip(idle, D1 + "Ch1M_GiantIntro", false, (0, DA_HUNCH), (.5f, DA_HUNCH), (1.05f, DA_ROAR), (1.45f, G(DA_ROAR, "LA.DU=.86 RA.DU=.66 SP.LR=.07")), (1.85f, DA_ROAR),
                (2.5f, DA_OVER), (2.64f, G(DA_OVER, "LA.DU=1.25 RA.DU=1.25")), (2.79f, DA_SLAM), (3.4f, DA_SLAM), (3.8f, ""));

            // 컨덕터 반 (첫 컷부터, 크래시 1.28): 몸을 젖혀 호루라기를 길게 → 신호봉(왼손)을 뒤로 감았다 앞으로 쭉 내밂
            const string CV_BLOW = "RA.FB=.55 RA.DU=.35 RF=-1 RH.DU=.2 SP.FB=-.25 CH.FB=-.25 HD.ND=.25 LA.DU=.2";
            const string CV_WIND = "LA.DU=.9 LA.FB=-.5 LF=-.4 SP.TW=.35 SP.FB=-.1 RA.FB=.3 RA.DU=.2 RF=-.6 HD.TN=-.15 LL.FB=.2";
            const string CV_THRUST = "LA.FB=1.0 LA.DU=.45 LF=.45 SP.TW=-.35 SP.FB=.25 RA.FB=.4 RA.DU=.3 RF=-.9 LL.FB=.45 LK=-.35 RL.FB=-.25 HD.ND=-.05 ROOT=-.08";
            Clip(idle, D2 + "Ch2M_MasterIntro", false, (0, ""), (.25f, CV_BLOW), (.55f, G(CV_BLOW, "SP.FB=-.32 HD.ND=.32 LA.DU=.35 LA.FB=-.2")), (.8f, CV_BLOW),
                (1.12f, CV_WIND), (1.28f, CV_THRUST), (1.32f, G(CV_THRUST, "SP.LR=.08")), (1.9f, CV_THRUST), (2.3f, ""));

            // 로코모스 (첫 컷부터, 돌진 1.0 ~ 2.2, 펀치 2.89): 압력을 모았다 터뜨림 → 팔을 휘저으며 달려옴 → 오른 주먹을 크게 감아 내지름
            const string LO_GATH = "LA.FB=.5 RA.FB=.5 LA.DU=.1 RA.DU=.1 LF=-.85 RF=-.85 SP.FB=.35 LK=-.4 RK=-.4 LL.FB=.3 RL.FB=.3 HD.ND=-.2 ROOT=-.1";
            const string LO_BURST = "LA.DU=.85 RA.DU=.85 LA.FB=-.15 RA.FB=-.15 LF=.35 RF=.35 CH.FB=-.35 SP.FB=-.2 HD.ND=.35 ROOT=.03";
            const string LO_RA = "SP.FB=.4 LA.FB=.6 RA.FB=-.5 LF=-.6 RF=-.6 LL.FB=-.3 RL.FB=.6 LK=-.7 RK=-.2 HD.ND=-.1 ROOT=-.08";
            const string LO_RB = "SP.FB=.4 LA.FB=-.5 RA.FB=.6 LF=-.6 RF=-.6 LL.FB=.6 RL.FB=-.3 LK=-.2 RK=-.7 HD.ND=-.1 ROOT=-.08";
            const string LO_WIND = "RA.FB=-.6 RA.DU=.4 RF=-.8 SP.TW=.5 CH.TW=.2 LA.FB=.5 LA.DU=.3 LF=-.5 LL.FB=.4 LK=-.4 ROOT=-.1";
            const string LO_PUNCH = "RA.FB=1.05 RA.DU=.3 RF=.5 SP.TW=-.45 CH.TW=-.2 SP.FB=.3 LA.FB=-.4 LA.DU=.2 LL.FB=.55 LK=-.45 RL.FB=-.35 ROOT=-.12";
            Clip(idle, D2 + "Ch2M_GiantIntro", false, (0, ""), (.25f, LO_GATH), (.4f, G(LO_GATH, "SP.LR=.06")), (.55f, LO_GATH), (.7f, LO_BURST),
                (1.0f, LO_RA), (1.25f, LO_RB), (1.5f, LO_RA), (1.75f, LO_RB), (2.0f, LO_RA), (2.25f, LO_RB), (2.7f, LO_WIND), (2.88f, LO_PUNCH), (3.5f, LO_PUNCH), (3.9f, ""));

            // 기로틴 (점멸 점프컷 4칸 0 · .25 · .5 · .75, 십자 베기 1.28): 칸마다 다른 자세로 뚝뚝 → 두 칼을 머리 위로 → X자로 내려 벰
            const string EX_P1 = "LA.FB=.55 RA.FB=.55 LA.DU=.2 RA.DU=.2 LF=-.7 RF=-.7 LA.TW=.3 RA.TW=.3 SP.FB=.1 HD.ND=-.2 ROOT=-.03";
            const string EX_P2 = "LA.DU=.95 RA.DU=.95 LF=.3 RF=.3 CH.FB=-.2 HD.ND=.2 LL.IO=.2 RL.IO=-.2";
            const string EX_P3 = "SP.FB=.5 LK=-.75 RK=-.75 LL.FB=.5 RL.FB=.5 LA.FB=-.3 RA.FB=-.3 LA.DU=.2 RA.DU=.2 HD.ND=.25 ROOT=-.25";
            const string EX_P4 = "RA.DU=1.2 RA.FB=.3 RF=-.1 LA.FB=.5 LA.DU=.1 LF=-.5 SP.TW=.25 HD.ND=.1 RL.FB=-.2 LL.FB=.3 LK=-.3";
            const string EX_WIND = "LA.DU=1.1 RA.DU=1.1 LA.FB=-.35 RA.FB=-.35 LF=-.2 RF=-.2 SP.FB=-.25 CH.FB=-.2 HD.ND=.2 ROOT=.02";
            const string EX_X = "LA.DU=.15 RA.DU=.15 LA.FB=.85 RA.FB=.85 LA.TW=.4 RA.TW=.4 LF=.3 RF=.3 SP.FB=.4 HD.ND=-.15 LL.FB=.45 LK=-.45 RL.FB=-.3 ROOT=-.12";
            Clip(idle, D4 + "Ch4M_ExecIntro", false, (0, EX_P1), (.24f, EX_P1), (.25f, EX_P2), (.49f, EX_P2), (.5f, EX_P3), (.74f, EX_P3), (.75f, EX_P4), (1.0f, EX_P4),
                (1.18f, EX_WIND), (1.3f, EX_X), (1.34f, G(EX_X, "SP.LR=-.08")), (1.9f, EX_X), (2.2f, ""));

            // 일루시오 (첫 컷부터, 카드 던지기 1.52): 모자 챙에 손을 얹고 깊이 인사 → 두 팔을 활짝 펼쳐 일어남 → 오른손으로 카드를 감아 카메라로 내던짐
            const string IL_BOW = "SP.FB=.45 CH.FB=.15 HD.ND=-.3 RA.FB=.45 RA.DU=.25 RF=-.85 RH.DU=.2 LA.DU=.7 LA.FB=-.3 LF=.2 LL.FB=-.1 RL.FB=.25 RK=-.25";
            const string IL_OPEN = "RS.DU=.5 LS.DU=.5 SP.FB=-.15 CH.FB=-.25 HD.ND=.2 RA.DU=1.35 RA.FB=.2 RF=.25 LA.DU=1.35 LA.FB=.2 LF=.25 ROOT=.02";   // 이 리그는 DU 1 ≈ 어깨 높이라 크게
            const string IL_WIND = "RA.FB=-.3 RA.DU=.5 RF=-.8 RA.TW=-.4 SP.TW=.35 CH.TW=.15 LA.DU=.6 LA.FB=.3 HD.TN=-.1";
            const string IL_FLING = "RA.FB=1.15 RA.DU=.85 RF=.6 RA.TW=.3 RH.DU=-.4 SP.TW=-.4 CH.TW=-.18 SP.FB=.18 LA.DU=.9 LA.FB=-.4 LL.FB=.35 LK=-.35 RL.FB=-.15 ROOT=-.06";
            // 2026-11-22 새 연출 박자: 연기 속 등장 0.25(인사 중) → 분신과 함께 팔 펼침 1.2 → 감기 1.62 → 카드 던지기 1.84 (컷 1.0 · 0.7 · 0.7)
            Clip(idle, D3 + "Ch3M_IllusioIntro", false, (0, IL_BOW), (.95f, G(IL_BOW, "SP.FB=.5 HD.ND=-.35")), (1.25f, IL_OPEN), (1.5f, G(IL_OPEN, "LA.DU=1 RA.DU=1")), (1.66f, IL_WIND), (1.84f, IL_FLING),
                (1.9f, G(IL_FLING, "SP.LR=.06")), (2.6f, IL_FLING), (3.0f, ""));

            // ---- 일루시오 전투 동작 (패턴 Cast 박자: 카드 부채 0.6 · 칼 상자 0.6/1.4 · 비둘기 0.5 · 거울 상자 0.8 · 분신 0.5) ----
            const string IL_REST = "RA.FB=.15 RF=-.3 LA.FB=.1 LF=-.25 HD.ND=.05";
            // 카드 부채: 두 손에 카드를 부채처럼 펼쳐 몸 옆으로 감았다(0.55) 앞으로 휙 흩뿌림
            const string CF_WIND = "RA.FB=-.35 RA.DU=.95 RF=-.55 LA.FB=.65 LA.DU=.75 LF=-.6 SP.TW=.5 CH.TW=.22 HD.TN=-.15 LL.FB=.2 LK=-.25";
            const string CF_THROW = "RA.FB=1.15 RA.DU=.9 RF=.6 RH.DU=-.3 LA.FB=-.3 LA.DU=1.0 LF=.4 SP.TW=-.5 CH.TW=-.22 SP.FB=.18 RL.FB=.35 RK=-.35 ROOT=-.05";
            Clip(idle, D3 + "Ch3M_IllusioCardFan", false, (0, IL_REST), (.45f, CF_WIND), (.55f, G(CF_WIND, "SP.TW=.55")), (.63f, CF_THROW), (.9f, CF_THROW), (1.2f, IL_REST));
            // 칼 꽂기 상자: 지휘봉처럼 오른팔을 들어(0.5) 칼이 꽂힐 자리를 내리꽂듯 가리킴, 두 번째 칼(1.4)도
            const string SB_UP = "RS.DU=.9 RA.DU=1.75 RA.FB=.25 RF=-.1 LA.DU=.7 LA.FB=-.25 SP.FB=-.18 HD.ND=.2";
            const string SB_POINT = "RA.DU=.75 RA.FB=1.2 RF=.6 RH.DU=-.3 LA.DU=.9 LA.FB=-.4 SP.FB=.28 HD.ND=-.15 LL.FB=.35 LK=-.35 ROOT=-.06";
            Clip(idle, D3 + "Ch3M_IllusioSwordBox", false, (0, IL_REST), (.45f, SB_UP), (.6f, SB_POINT), (1.1f, SB_POINT), (1.3f, SB_UP), (1.42f, SB_POINT), (1.8f, IL_REST));
            // 모자 속 비둘기: 오른손으로 모자를 머리 위로 번쩍 들어(0.45) 비둘기가 쏟아지는 동안 들고 있음, 왼팔은 펼침
            const string DV_HAT = "RS.DU=1 RA.TW=-.3 RA.DU=1.85 RA.FB=.25 RF=-.45 RH.DU=.2 LA.DU=1.2 LA.FB=.1 LF=.3 CH.FB=-.25 HD.ND=.2 ROOT=.03";
            Clip(idle, D3 + "Ch3M_IllusioDoves", false, (0, IL_REST), (.3f, "RA.DU=.9 RA.FB=.35 RF=-1 HD.ND=-.1"), (.45f, DV_HAT), (.55f, G(DV_HAT, "RA.DU=1.95")), (1.3f, DV_HAT), (1.65f, IL_REST));
            // 거울 상자: 두 팔을 활짝 펼쳐 무대를 소개하듯 (0.8), 몸을 뒤로 젖힘
            const string MB_OPEN = "RA.DU=1.4 LA.DU=1.4 RA.FB=.25 LA.FB=.25 RF=.35 LF=.35 RH.DU=.3 LH.DU=.3 CH.FB=-.32 SP.FB=-.15 HD.ND=.25 ROOT=.02";
            Clip(idle, D3 + "Ch3M_IllusioMirrorBox", false, (0, IL_REST), (.35f, "RA.FB=.6 LA.FB=.6 RF=-.8 LF=-.8 SP.FB=.25 HD.ND=-.2 ROOT=-.04"), (.8f, MB_OPEN), (1.4f, MB_OPEN), (1.75f, IL_REST));
            // 분신술사 호출: 오른손을 얼굴 옆으로 들어 손가락을 딱(0.5), 왼팔은 옆으로
            const string SM_UP = "RA.DU=1.2 RA.FB=.55 RF=-1 RH.DU=.3 LA.DU=1.0 LA.FB=-.2 HD.TN=-.1 SP.TW=.15";
            Clip(idle, D3 + "Ch3M_IllusioSummon", false, (0, IL_REST), (.4f, SM_UP), (.5f, G(SM_UP, "RH.DU=-.2 RF=-.9")), (.9f, SM_UP), (1.2f, IL_REST));
            // 진짜 찾기 · 거울 시계: 두 손을 가슴 앞에서 둥글게 돌리며 마법을 거는 자세 (1초)
            const string CJ_A = "RA.FB=.7 LA.FB=.7 RA.DU=.4 LA.DU=.2 RF=-.5 LF=-.7 SP.FB=.1 HD.ND=-.1", CJ_B = "RA.FB=.7 LA.FB=.7 RA.DU=.2 LA.DU=.4 RF=-.7 LF=-.5 SP.FB=.1 HD.ND=-.1";
            Clip(idle, D3 + "Ch3M_IllusioConjure", false, (0, IL_REST), (.25f, CJ_A), (.5f, CJ_B), (.75f, CJ_A), (1f, CJ_B), (1.3f, IL_REST));

            // 크로노(시침) · 템포(분침) (첫 컷부터, 교차 베기 2.93): 고개 숙여 서 있다가 → 고개 들고 자세 → 한 번에 내지름. 팔은 무기 리그가 잡으니 몸 위주

            const string HH_UP = "SP.FB=-.1 CH.FB=-.2 HD.ND=.2 LL.IO=.25 RL.IO=-.25 LK=-.2 RK=-.2 ROOT=-.05";
            const string MH_GUARD = "RA.FB=.7 RA.DU=.3 RF=.2 LA.DU=.6 LA.FB=-.4 LF=-.6 SP.TW=-.35 LL.FB=.35 LK=-.4 RL.FB=-.25 ROOT=-.1";
            // 2026-11-22: 착지(Intro, 첫 컷 0초부터: 공중에서 웅크림 → 0.7 착지 무릎 꿇듯 → 일어섬) + 마지막 컷 정면 결정 자세(IntroPose, 0.32에 내지름)
            const string TW_AIR = "SP.FB=-.1 HD.ND=-.15 LL.FB=.55 LK=-.9 RL.FB=.35 RK=-.85 ROOT=.05";
            const string TW_LAND = "SP.FB=.55 CH.FB=.15 HD.ND=.2 LL.FB=.75 LK=-1 RL.FB=-.25 RK=-.9 ROOT=-.4";
            Clip(idle, D4 + "Ch4M_HourIntro", false, (0, TW_AIR), (.55f, TW_AIR), (.7f, TW_LAND), (.88f, TW_LAND), (1.2f, HH_UP), (1.5f, ""));
            // 결정 자세는 상체를 세움 (숙이면 위에서 보는 정면 카메라에 투구 꼭대기만 보여 등을 보이는 것 같음)
            const string HH_STRIKE = "SP.FB=.12 CH.FB=-.15 HD.ND=-.05 SP.TW=-.3 LL.FB=.45 RL.FB=-.3 LK=-.45 RK=-.15 ROOT=-.12";
            Clip(idle, D4 + "Ch4M_HourPose", false, (0, HH_UP), (.2f, G(HH_UP, "SP.TW=.35")), (.32f, HH_STRIKE), (2.2f, HH_STRIKE), (2.6f, ""));
            Clip(idle, D4 + "Ch4M_MinuteIntro", false, (0, TW_AIR), (.55f, TW_AIR), (.7f, TW_LAND), (.88f, TW_LAND), (1.2f, MH_GUARD), (1.5f, ""));
            const string MH_THRUST = "RA.FB=1.0 RA.DU=.25 RF=.5 SP.FB=.1 SP.TW=-.45 CH.FB=-.1 LA.FB=-.5 LA.DU=.6 LL.FB=.65 LK=-.55 RL.FB=-.45 RK=.05 HD.ND=.05 ROOT=-.16";
            Clip(idle, D4 + "Ch4M_MinutePose", false, (0, MH_GUARD), (.2f, MH_GUARD), (.32f, MH_THRUST), (2.2f, MH_THRUST), (2.6f, ""));

            // 시간의 주인 (첫 컷부터, 째깍 컷 1.2 ~ 2.7을 일곱 칸, 대낫 3.23): 떠 있는 채 두 팔이 째깍째깍 한 칸씩 펼쳐 올라감 → 낫을 옆으로 감았다 수평으로 베어 넘김
            const string LT_REST = "RA.DU=.3 RA.FB=.25 RF=-.6 RH.DU=.1 LA.DU=.25 LA.FB=.1 LF=-.4 SP.FB=.05 LL.FB=.14 RL.FB=.02 LK=-.32 RK=-.14 LFT=-.65 RFT=-.65";
            const string LT_SIDE = LT_REST + " RA.DU=.95 RA.FB=-.3 RA.TW=.3 RF=-.3 SP.TW=.65 CH.TW=.3 LA.FB=.45 LA.DU=.3 LF=-.3 SP.FB=-.1";
            const string LT_SWEEP = LT_REST + " RA.DU=.6 RA.FB=.95 RA.TW=-.3 RF=.05 SP.TW=-.6 CH.TW=-.3 LA.FB=-.3 LA.DU=.35 SP.FB=.35";
            var keys = new List<(float, string)> { (0, LT_REST), (1.1f, LT_REST + " HD.ND=-.15") };
            var inv = CultureInfo.InvariantCulture;
            for (int k = 0; k < 7; k++)
            {
                float u = (k + 1) / 7f, t0 = 1.2f + k * (1.5f / 7f);
                string pose = LT_REST + " LA.DU=" + (0.25f + 0.95f * u).ToString("0.00", inv) + " RA.DU=" + (0.3f + 0.75f * u).ToString("0.00", inv)
                            + " LF=" + (-0.4f + 0.55f * u).ToString("0.00", inv) + " CH.FB=" + (-0.3f * u).ToString("0.00", inv)
                            + " HD.ND=" + (-0.15f + 0.5f * u).ToString("0.00", inv) + " ROOT=" + (0.08f * u).ToString("0.00", inv);
                keys.Add((t0 + 0.01f, pose)); keys.Add((t0 + 1.5f / 7f - 0.01f, pose));
            }
            keys.Add((3.05f, LT_SIDE)); keys.Add((3.13f, LT_SIDE + " SP.TW=.75")); keys.Add((3.23f, LT_SWEEP)); keys.Add((3.4f, LT_SWEEP + " SP.TW=-.75")); keys.Add((3.9f, LT_SWEEP)); keys.Add((4.3f, LT_REST));
            Clip(idle, D5 + "Ch5M_BossIntro", false, keys.ToArray());
        }
    }
}
