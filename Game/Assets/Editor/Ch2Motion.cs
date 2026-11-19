// 2장 전용 모션 (짐꾼 PorterRig처럼 근육 커브로 직접 만든 휴머노이드 클립)
// 사용자 2026-11-17: 차장 · 신호수 · 태엽 역장 · 기관차 거인도 1장 클립 돌려쓰기 대신 전용 모션 → 크레딧 안 드는 코드 클립으로.
// 기준 자세 = 고양이 idle(0.3초)의 근육값. 표의 키 이름 앞에 '+'가 붙으면 그 기준값에 더함(덧셈), 없으면 절대값.
// 시간은 런타임 패턴 타이밍과 맞춤 (EnemyModelBuilder.Anims의 재생 시간으로 늘이고 줄임).
//   차장  Attack = 승차권 튕기기   (오른팔 뒤로 → 0.55초 앞으로 휙)
//   신호수 Attack = 신호등 던지기  (두 팔 위로 → 0.34초 앞으로 내리꽂음 = SignalUpdate의 던지기 시점)
//   역장  Attack = 개찰 펀치 · 탄막 (몸을 낮추고 오른 주먹 뻗기) / Special = 신호봉 내려치기 · 호루라기 (두 팔 높이 → 내려침)
//   거인  Attack = 상체 숙여 버팀 (돌진 · 포격) / Special = 양팔 크게 휘두르기 (연결봉) / Run = 무거운 걸음
// 클립: Assets/Art/Characters/Ch2/Anim/Ch2M_*.anim (FindClip 키워드 = 이름 소문자)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class Ch2Motion
    {
        const string Dir = "Assets/Art/Characters/Ch2/Anim/";

        /// <summary>달리기 대신 쓸 전용 걷기 클립 키워드 (없으면 null)</summary>
        public static string WalkFor(string outName) => outName == "Ch2_Boss" ? "ch2m_giantwalk" : outName == "Ch2_MidBoss" ? "ch2m_masterwalk" : null;

        [MenuItem("CatTang/2장 전용 모션 클립 만들기")]
        public static void MakeAll()
        {
            Directory.CreateDirectory(Dir);
            var idle = CatIdle();
            if (!idle) { Debug.LogError("[Ch2Motion] 고양이 idle 클립이 없음"); return; }

            // ---- 차장: 승차권 튕기기 0.9초 ----
            Make(idle, "Ch2M_ConductorFlick", 0.9f, false, new()
            {
                { "+Right Arm Front-Back", K(0, 0, 0.42f, -0.45f, 0.55f, 0.75f, 0.68f, 0.6f, 0.9f, 0) },
                { "+Right Arm Down-Up", K(0, 0, 0.42f, 0.35f, 0.55f, 0.45f, 0.9f, 0) },
                { "+Right Forearm Stretch", K(0, 0, 0.42f, -0.5f, 0.55f, 0.6f, 0.9f, 0) },
                { "+Right Hand Down-Up", K(0, 0, 0.42f, 0.5f, 0.55f, -0.6f, 0.9f, 0) },
                { "+Left Arm Front-Back", K(0, 0, 0.42f, 0.25f, 0.55f, -0.2f, 0.9f, 0) },
                { "+Spine Twist Left-Right", K(0, 0, 0.42f, -0.35f, 0.55f, 0.3f, 0.9f, 0) },
                { "+Chest Front-Back", K(0, 0, 0.42f, -0.15f, 0.55f, 0.2f, 0.9f, 0) },
            }, null);

            // ---- 신호수: 신호등 던지기 0.6초 (0.34초에 손을 놓음) ----
            Make(idle, "Ch2M_SignalToss", 0.6f, false, new()
            {
                { "+Left Arm Down-Up", K(0, 0, 0.25f, 1.1f, 0.36f, 0.2f, 0.6f, 0) },
                { "+Right Arm Down-Up", K(0, 0, 0.25f, 1.1f, 0.36f, 0.2f, 0.6f, 0) },
                { "+Left Arm Front-Back", K(0, 0, 0.25f, -0.2f, 0.36f, 0.8f, 0.6f, 0) },
                { "+Right Arm Front-Back", K(0, 0, 0.25f, -0.2f, 0.36f, 0.8f, 0.6f, 0) },
                { "+Spine Front-Back", K(0, 0, 0.25f, -0.35f, 0.36f, 0.45f, 0.6f, 0) },
                { "+Head Nod Down-Up", K(0, 0, 0.25f, 0.3f, 0.36f, -0.25f, 0.6f, 0) },
            }, K(0, 0, 0.25f, 0.05f, 0.36f, -0.06f, 0.6f, 0));

            // ---- 태엽 역장: 펀치 1.0초 / 신호봉 내려치기 1.0초 / 걷기 0.8초 ----
            Make(idle, "Ch2M_MasterPunch", 1.0f, false, new()
            {
                { "+Right Arm Front-Back", K(0, 0, 0.3f, -0.4f, 0.42f, 0.9f, 0.7f, 0.85f, 1.0f, 0) },
                { "+Right Arm Down-Up", K(0, 0, 0.3f, -0.1f, 0.42f, 0.4f, 1.0f, 0) },
                { "+Right Forearm Stretch", K(0, 0, 0.3f, -0.7f, 0.42f, 0.7f, 0.7f, 0.6f, 1.0f, 0) },
                { "+Spine Twist Left-Right", K(0, 0, 0.3f, -0.4f, 0.42f, 0.35f, 1.0f, 0) },
                { "+Spine Front-Back", K(0, 0, 0.3f, 0.15f, 0.42f, 0.3f, 1.0f, 0) },
                { "+Left Upper Leg Front-Back", K(0, 0, 0.42f, 0.3f, 1.0f, 0) },
                { "+Left Lower Leg Stretch", K(0, 0, 0.42f, -0.35f, 1.0f, 0) },
            }, K(0, 0, 0.42f, -0.08f, 1.0f, 0));
            Make(idle, "Ch2M_MasterBaton", 1.0f, false, new()
            {
                { "+Right Arm Down-Up", K(0, 0, 0.5f, 1.3f, 0.62f, -0.1f, 1.0f, 0) },
                { "+Right Arm Front-Back", K(0, 0, 0.5f, 0.1f, 0.62f, 0.8f, 1.0f, 0) },
                { "+Left Arm Down-Up", K(0, 0, 0.5f, 0.5f, 0.62f, 0f, 1.0f, 0) },
                { "+Spine Front-Back", K(0, 0, 0.5f, -0.4f, 0.62f, 0.5f, 1.0f, 0) },
                { "+Chest Front-Back", K(0, 0, 0.5f, -0.3f, 0.62f, 0.35f, 1.0f, 0) },
                { "+Head Nod Down-Up", K(0, 0, 0.5f, 0.35f, 0.62f, -0.3f, 1.0f, 0) },
            }, K(0, 0, 0.62f, -0.07f, 1.0f, 0));
            Walk(idle, "Ch2M_MasterWalk", 0.8f, 0.3f, 0.25f);

            // ---- 기관차 거인: 버팀 1.1초 / 연결봉 휘두르기 0.9초 / 무거운 걸음 1.4초 ----
            Make(idle, "Ch2M_GiantBrace", 1.1f, false, new()
            {
                { "+Spine Front-Back", K(0, 0, 0.35f, 0.5f, 1.1f, 0.45f) },
                { "+Chest Front-Back", K(0, 0, 0.35f, 0.3f, 1.1f, 0.25f) },
                { "+Left Arm Front-Back", K(0, 0, 0.35f, 0.5f, 1.1f, 0.4f) },
                { "+Right Arm Front-Back", K(0, 0, 0.35f, 0.5f, 1.1f, 0.4f) },
                { "+Left Upper Leg Front-Back", K(0, 0, 0.35f, 0.35f, 1.1f, 0.3f) },
                { "+Right Upper Leg Front-Back", K(0, 0, 0.35f, -0.2f, 1.1f, -0.2f) },
                { "+Left Lower Leg Stretch", K(0, 0, 0.35f, -0.45f, 1.1f, -0.4f) },
            }, K(0, 0, 0.35f, -0.12f, 1.1f, -0.1f));
            Make(idle, "Ch2M_GiantRods", 0.9f, false, new()
            {
                { "+Left Arm Down-Up", K(0, 0, 0.75f, 0.9f, 0.82f, 0.6f, 0.9f, 0.3f) },
                { "+Right Arm Down-Up", K(0, 0, 0.75f, 0.9f, 0.82f, 0.6f, 0.9f, 0.3f) },
                { "+Left Arm Front-Back", K(0, 0, 0.75f, -0.6f, 0.82f, 0.7f, 0.9f, 0.4f) },
                { "+Right Arm Front-Back", K(0, 0, 0.75f, 0.7f, 0.82f, -0.6f, 0.9f, -0.3f) },
                { "+Spine Twist Left-Right", K(0, 0, 0.75f, -0.5f, 0.82f, 0.5f, 0.9f, 0.2f) },
                { "+Upper Leg In-Out", K(0, 0, 0.75f, 0.25f, 0.9f, 0.2f) },
            }, K(0, 0, 0.75f, -0.1f, 0.82f, -0.14f, 0.9f, -0.05f));
            Walk(idle, "Ch2M_GiantWalk", 1.4f, 0.22f, 0.12f);

            AssetDatabase.SaveAssets();
            Debug.Log("[Ch2Motion] 2장 전용 클립 8개 → " + Dir);
        }

        /// <summary>걷기: stride = 다리 앞뒤 폭, sway = 상체 좌우 기우뚱 (무거울수록 보폭 작고 몸이 크게 내려앉음)</summary>
        internal static void Walk(AnimationClip idle, string name, float W, float stride, float sway, float lean = 0.1f)
        {
            float q = W / 4, a = 0.62f + stride, b = 0.62f - stride;
            Make(idle, name, W, true, new()
            {
                { "Left Upper Leg Front-Back", K(0, a, q, 0.62f, 2 * q, b, 3 * q, 0.6f, W, a) },
                { "Right Upper Leg Front-Back", K(0, b, q, 0.6f, 2 * q, a, 3 * q, 0.62f, W, b) },
                { "Left Lower Leg Stretch", K(0, 0.9f, q, 0.85f, 2 * q, 0.75f, 3 * q, 0.3f, W, 0.9f) },
                { "Right Lower Leg Stretch", K(0, 0.75f, q, 0.3f, 2 * q, 0.9f, 3 * q, 0.85f, W, 0.75f) },
                { "Left Upper Leg In-Out", K(0, 0.15f, W, 0.15f) }, { "Right Upper Leg In-Out", K(0, 0.15f, W, 0.15f) },
                { "+Left Arm Front-Back", K(0, -stride, 2 * q, stride, W, -stride) },
                { "+Right Arm Front-Back", K(0, stride, 2 * q, -stride, W, stride) },
                { "Spine Left-Right", K(0, sway, 2 * q, -sway, W, sway) },
                { "+Spine Front-Back", K(0, lean, W, lean) },
            }, K(0, -0.05f, q, 0.02f, 2 * q, -0.05f, 3 * q, 0.02f, W, -0.05f));
        }

        internal static AnimationClip CatIdle()
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") return c;
            return null;
        }

        /// <param name="name">클립 이름 (2장 폴더) 또는 "Assets/..."로 시작하는 확장자 뺀 전체 경로 (다른 장 전용 모션 EnemyMotions)</param>
        internal static void Make(AnimationClip idle, string name, float end, bool loop, Dictionary<string, float[][]> keys, float[][] rootDy)
        {
            string path = name.StartsWith("Assets/") ? name + ".anim" : Dir + name + ".anim";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            clip.name = Path.GetFileNameWithoutExtension(path);
            var used = new HashSet<string>();
            foreach (var bnd in AnimationUtility.GetCurveBindings(idle))
            {
                float v0 = AnimationUtility.GetEditorCurve(idle, bnd).Evaluate(0.3f);
                string pn = bnd.propertyName;
                AnimationCurve curve = null;
                if (keys.TryGetValue(pn, out var abs)) { curve = Curve(abs, 0); used.Add(pn); }
                else if (keys.TryGetValue("+" + pn, out var add)) { curve = Curve(add, v0); used.Add("+" + pn); }
                else
                {
                    // "+Upper Leg In-Out"처럼 좌우를 뺀 이름이면 양쪽에 같이
                    foreach (var lr in new[] { "Left ", "Right " })
                        if (pn.StartsWith(lr) && keys.TryGetValue("+" + pn.Substring(lr.Length), out var both)) { curve = Curve(both, v0); used.Add("+" + pn.Substring(lr.Length)); }
                }
                if (curve == null && pn == "RootT.y" && rootDy != null) curve = Curve(rootDy, v0);
                curve ??= new AnimationCurve(new Keyframe(0, v0), new Keyframe(end, v0));
                AnimationUtility.SetEditorCurve(clip, bnd, curve);
            }
            foreach (var k in keys.Keys) if (!used.Contains(k)) Debug.LogWarning($"[Ch2Motion] {clip.name}: 근육 이름 '{k}'을 고양이 idle 클립에서 못 찾음 — 무시됨");
            MirrorFrontBack(clip, idle);
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = loop, keepOriginalPositionY = true, keepOriginalOrientation = true, keepOriginalPositionXZ = true, stopTime = end });
            EditorUtility.SetDirty(clip);
        }

        static AnimationCurve Curve(float[][] ks, float offset)
        {
            var kf = new Keyframe[ks.Length];
            for (int i = 0; i < ks.Length; i++) kf[i] = new Keyframe(ks[i][0], ks[i][1] + offset);
            var c = new AnimationCurve(kf);
            for (int i = 0; i < kf.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto); }
            return c;
        }

        /// <summary>
        /// 앞뒤 근육 방향 바로잡기 (2026-11-19 측정): 유니티 근육 이름은 '음수-양수' 순서라 "Front-Back"은 + = 뒤쪽.
        /// (적 리그 12종 + 고양이 모두 같음: Arm Front-Back +0.5 → 손이 뒤로 0.3m, Chest/Spine +0.5 → 머리가 엉덩이 뒤로, Upper Leg +0.5 → 발이 뒤로)
        /// 코드 클립 표는 모두 '+ = 앞'으로 적어 왔으므로, 다 만든 클립의 Front-Back 커브를 기준값(고양이 idle 또는 pivot) 둘레로 뒤집음 → 표는 그대로 '+ = 앞'.
        /// 덧셈 키(기준 + off → 기준 − off) · 절대 키(v → 2·기준 − v) 모두 같은 식. 키가 없는(기준 그대로) 커브는 안 바뀜.
        /// </summary>
        internal static void MirrorFrontBack(AnimationClip clip, AnimationClip idle, Dictionary<string, float> pivot = null)
        {
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                if (!b.propertyName.EndsWith("Front-Back")) continue;
                float p;
                if (pivot == null || !pivot.TryGetValue(b.propertyName, out p))
                {
                    var ic = idle ? AnimationUtility.GetEditorCurve(idle, b) : null;
                    p = ic != null ? ic.Evaluate(0.3f) : 0f;
                }
                var c = AnimationUtility.GetEditorCurve(clip, b);
                var ks = c.keys;
                for (int i = 0; i < ks.Length; i++) { var k = ks[i]; k.value = 2f * p - k.value; k.inTangent = -k.inTangent; k.outTangent = -k.outTangent; ks[i] = k; }
                c.keys = ks;
                AnimationUtility.SetEditorCurve(clip, b, c);
            }
        }

        internal static float[][] K(params float[] tv) { var r = new float[tv.Length / 2][]; for (int i = 0; i < r.Length; i++) r[i] = new[] { tv[i * 2], tv[i * 2 + 1] }; return r; }
    }
}
