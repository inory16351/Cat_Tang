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
        const string D1 = "Assets/Art/Characters/Ch1/Anim/", D2 = "Assets/Art/Characters/Ch2/Anim/", D3 = "Assets/Art/Characters/Ch3/Anim/", D4 = "Assets/Art/Characters/Ch4/Anim/";

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
            Ch2Motion.Make(idle, path, tl[tl.Length - 1].t, loop, keys, root);
        }

        static AnimationClip CatClip(string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == name) return c;
            return null;
        }

        /// <summary>고양이 클립(src)을 그대로 쓰되, pose에 나온 근육만 '고양이 idle + 값'으로 고정 (무기 든 팔). 반복 · 길이는 src 그대로, 앞뒤 근육은 MirrorFrontBack</summary>
        static void HoldArm(AnimationClip src, string path, string pose)
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
                    c = new AnimationCurve(new Keyframe(0, v0 + off), new Keyframe(len, v0 + off));
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

        [MenuItem("CatTang/적 전용 모션 클립 만들기 (1 · 2 · 3 · 4장)")]
        public static void MakeAll()
        {
            var idle = Ch2Motion.CatIdle();
            if (!idle) { Debug.LogError("[EnemyMotions] 고양이 idle 클립이 없음"); return; }
            Ch1(idle); Ch2(idle); Ch3(idle); Ch4(idle);
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
            Ch2Motion.Walk(idle, D4 + "Ch4M_BaseKnightStep", 1.1f, 0.24f, 0.14f, 0.12f);   // 무거운 쇠 걸음 (바탕)
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
    }
}
