// 2장 트렁크 짐꾼 리그 다듬기 (2026-11-17 사용자: 짐꾼 모션이 어색함)
// 원인: Tripo 자동 리깅이 몸 앞 트렁크를 팔(특히 오른팔뚝)에 붙여 스키닝 → 고양이 대기 · 달리기 클립(팔 위치가 다름)을 돌려 쓰면
//       트렁크가 접히거나 몸속으로 들어가 버림.
// 1) SplitTrunk: 트렁크 정점(원본 비율 기준 상자: 앞 0.085~0.3 · 높이 0.17~0.70 · 좌우 ±0.27)을 새 뼈 'Trunk'(가슴 Spine2 자식)에 100% 붙인
//    메시 사본을 만들어 프리팹이 씀 → 트렁크는 가슴에 단단히 붙어 움직이고, 런타임이 이 뼈를 줄여 숨길 수 있음(트렁크 던지기)
// 2) 전용 클립: 팔 근육은 모델 기본 자세(= 트렁크를 두 손으로 든 자세)값으로 고정, 다리 · 허리만 움직임
//    대기(숨쉬기) · 걷기(무거운 발걸음) · 내리찍기(젖혔다 → 허리를 숙여 트렁크로 바닥을 찍고 → 빈틈 동안 숙인 채 → 일어남)
//    · 던지기(뒤로 젖혀 → 앞으로 내던짐 → 빈손을 뻗음) · 방패 돌진(트렁크 앞세워 낮게 버팀)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class PorterRig
    {
        // 원본(블렌더에서 측정, 키 1.0 기준) 트렁크 상자. 강화형은 트렁크가 조금 넓음
        const float BoxFwdMin = 0.085f, BoxFwdMax = 0.32f, BoxUpMin = 0.17f, BoxUpMax = 0.70f, Margin = 0.02f;
        static float BoxSide(string outName) => outName.Contains("Plus") ? 0.29f : 0.27f;

        // 클립 시간 (Enemy 상수와 맞춤: SlamWindup 0.7 · SlamOpen 1.2 / TrunkAim 0.8 / 돌진 예고 0.5 + 돌진 0.4)
        public const float SlamHit = 0.7f, SlamEnd = 2.4f, ThrowRelease = 0.8f, ThrowEnd = 1.3f, BashEnd = 1.0f, WalkCycle = 0.9f, IdleCycle = 1.6f;

        public static bool IsPorter(string outName) => outName.StartsWith("Ch2_Porter");

        // ---------------------------------------------------------------- 트렁크 분리
        public static void SplitTrunk(GameObject inst, string outName, string dir)
        {
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!smr || !smr.sharedMesh) { Debug.LogWarning($"[PorterRig] {outName}: 스킨 메시 없음"); return; }
            var bones = smr.bones;
            Transform chest = null;
            foreach (var b in bones) if (b && b.name.EndsWith("Spine2")) chest = b;
            if (!chest) { Debug.LogWarning($"[PorterRig] {outName}: Spine2 없음"); return; }

            var src = smr.sharedMesh;
            var verts = src.vertices;
            var bw = src.boneWeights;
            var bp = src.bindposes;
            // 기본 자세의 월드 위치 (갓 꺼낸 FBX = 바인드 자세)
            var world = new Vector3[verts.Length];
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                int k = bw[i].boneIndex0;
                world[i] = (bones[k].localToWorldMatrix * bp[k]).MultiplyPoint3x4(verts[i]);
                min = Vector3.Min(min, world[i]); max = Vector3.Max(max, world[i]);
            }
            float H = max.y - min.y;
            Vector3 c = (min + max) / 2;
            float side = BoxSide(outName);
            // 상자 안 = 1, 상자 밖 Margin까지 서서히 0 (원본 비율: 앞 = +Z, 위 = +Y, 옆 = X)
            var t = new float[verts.Length];
            Vector3 sum = Vector3.zero; int n = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                float f = (world[i].z - c.z) / H, u = (world[i].y - min.y) / H, s = Mathf.Abs(world[i].x - c.x) / H;
                float o = Mathf.Max(BoxFwdMin - f, f - BoxFwdMax, BoxUpMin - u, u - BoxUpMax, s - side, 0f);
                t[i] = o <= 0 ? 1f : Mathf.Clamp01(1f - o / Margin);
                if (t[i] >= 1f) { sum += world[i]; n++; }
            }
            if (n < 100) { Debug.LogWarning($"[PorterRig] {outName}: 트렁크 정점 {n}개뿐 — 방향이 다른 듯, 건너뜀"); return; }

            // 트렁크 뼈: 가슴 자식, 트렁크 가운데, 몸 방향
            var old = chest.Find("Trunk");
            if (old) Object.DestroyImmediate(old.gameObject);
            var trunk = new GameObject("Trunk").transform;
            trunk.SetPositionAndRotation(sum / n, inst.transform.parent ? inst.transform.parent.rotation : Quaternion.identity);
            trunk.SetParent(chest, true);
            trunk.localScale = Vector3.one;

            // 메시 둘로 나눔: 몸(트렁크 삼각형을 뺌, 원래 가중치) + 트렁크(트렁크 삼각형만, 전부 Trunk 뼈)
            // → 트렁크는 가슴에 단단히 붙고, 던질 땐 트렁크 렌더러만 끄면 됨 (뼈를 줄이면 경계가 늘어나 부채꼴 자국이 남았음)
            int ti = bones.Length;
            var newBones = new List<Transform>(bones) { trunk }.ToArray();
            var newBp = new List<Matrix4x4>(bp) { trunk.worldToLocalMatrix * smr.transform.localToWorldMatrix }.ToArray();
            var body = Object.Instantiate(src);
            var box = Object.Instantiate(src);
            int bodyTris = 0, boxTris = 0;
            for (int sm = 0; sm < src.subMeshCount; sm++)
            {
                var tris = src.GetTriangles(sm);
                var bt = new List<int>(tris.Length); var kt = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    bool inTrunk = t[tris[i]] >= 0.5f && t[tris[i + 1]] >= 0.5f && t[tris[i + 2]] >= 0.5f;
                    var dst = inTrunk ? kt : bt;
                    dst.Add(tris[i]); dst.Add(tris[i + 1]); dst.Add(tris[i + 2]);
                }
                body.SetTriangles(bt, sm); box.SetTriangles(kt, sm);
                bodyTris += bt.Count / 3; boxTris += kt.Count / 3;
            }
            body.bindposes = newBp;
            var kw = new BoneWeight[bw.Length];
            for (int i = 0; i < kw.Length; i++) kw[i] = new BoneWeight { boneIndex0 = ti, weight0 = 1f };
            box.boneWeights = kw;
            box.bindposes = newBp;
            body = SaveMesh(body, dir + outName + "_BodyMesh.asset");
            box = SaveMesh(box, dir + outName + "_TrunkMesh.asset");
            smr.bones = newBones;
            smr.sharedMesh = body;
            // 트렁크 렌더러: 몸 렌더러와 같은 자리 · 뼈 · 재질 (재질은 뒤에서 빌더가 툰으로 바꿈)
            var go = new GameObject("TrunkSkin");
            go.transform.SetParent(smr.transform.parent, false);
            go.transform.localPosition = smr.transform.localPosition;
            go.transform.localRotation = smr.transform.localRotation;
            go.transform.localScale = smr.transform.localScale;
            var ts = go.AddComponent<SkinnedMeshRenderer>();
            ts.bones = newBones;
            ts.rootBone = smr.rootBone;
            ts.sharedMesh = box;
            ts.sharedMaterials = smr.sharedMaterials;
            ts.localBounds = smr.localBounds;
            ts.updateWhenOffscreen = smr.updateWhenOffscreen;
            Debug.Log($"[PorterRig] {outName}: 트렁크 정점 {n}개 · 삼각형 {boxTris} (몸 {bodyTris}) → TrunkSkin + Trunk 뼈");
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            mesh.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>발 붙이기 (FootGround): 크기 · 위치를 맞춘 뒤 불러야 기본 발 높이가 맞음</summary>
        public static void AddFootGround(GameObject inst)
        {
            var feet = new List<Transform>();
            foreach (var t in inst.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith("LeftFoot") || t.name.EndsWith("RightFoot") || t.name.EndsWith("LeftToeBase") || t.name.EndsWith("RightToeBase")) feet.Add(t);
            if (feet.Count == 0) return;
            var fg = inst.GetComponent<CatTang.FootGround>();
            if (!fg) fg = inst.AddComponent<CatTang.FootGround>();
            fg.feet = feet.ToArray();
            // 기준 = 발 뼈가 발바닥보다 얼마나 위에 있는지 (메시를 실제로 구워서 잼 — 렌더러 경계는 부정확해서 몸이 떠 보였음)
            var root = inst.transform.parent;
            float low = float.MaxValue;
            foreach (var f in feet) low = Mathf.Min(low, root.InverseTransformPoint(f.position).y);
            float sole = float.MaxValue;
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr)
            {
                var baked = new Mesh();
                smr.BakeMesh(baked, true);                     // (실측: true + TransformPoint가 화면과 맞음)
                foreach (var v in baked.vertices) sole = Mathf.Min(sole, root.InverseTransformPoint(smr.transform.TransformPoint(v)).y);
                Object.DestroyImmediate(baked);
            }
            fg.restY = sole < float.MaxValue ? low - sole : low;
        }

        // ---------------------------------------------------------------- 전용 클립
        static readonly string[] ArmMuscles =
        {
            "Shoulder Down-Up", "Shoulder Front-Back", "Arm Down-Up", "Arm Front-Back", "Arm Twist In-Out",
            "Forearm Stretch", "Forearm Twist In-Out", "Hand Down-Up", "Hand In-Out",
        };

        /// <summary>모델 기본 자세의 근육값 (트렁크를 든 팔)</summary>
        static Dictionary<string, float> RestMuscles(GameObject fbx, Avatar avatar)
        {
            var inst = (GameObject)Object.Instantiate(fbx);
            var d = new Dictionary<string, float>();
            try
            {
                var h = new HumanPoseHandler(avatar, inst.transform);
                var pose = new HumanPose();
                h.GetHumanPose(ref pose);
                for (int i = 0; i < pose.muscles.Length; i++) d[HumanTrait.MuscleName[i]] = pose.muscles[i];
                h.Dispose();
            }
            finally { Object.DestroyImmediate(inst); }
            return d;
        }

        static AnimationClip CatIdle()
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") return c;
            return null;
        }

        /// <summary>키 표로 휴머노이드 클립 만들기. 표에 없는 근육은 팔 = 기본 자세, 나머지 = 고양이 idle 값</summary>
        static AnimationClip MakeClip(string path, string name, float end, bool loop, Dictionary<string, float[][]> keys, Dictionary<string, float> rest, float[][] rootDy)
        {
            var idle = CatIdle();
            if (!idle) return null;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            clip.name = name;
            foreach (var b in AnimationUtility.GetCurveBindings(idle))
            {
                float v0 = AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.3f);
                AnimationCurve curve = null;
                if (keys.TryGetValue(b.propertyName, out var ks)) curve = Curve(ks, 0);
                else if (b.propertyName == "RootT.y" && rootDy != null) curve = Curve(rootDy, v0);
                else
                {
                    // 팔: 기본 자세(트렁크 든 자세) + 표의 덧셈 키("+Left Arm Down-Up" 식)
                    foreach (var m in ArmMuscles)
                        foreach (var lr in new[] { "Left ", "Right " })
                            if (b.propertyName == lr + m && rest.TryGetValue(lr + m, out var rv))
                                curve = keys.TryGetValue("+" + lr + m, out var add) ? Curve(add, rv) : Curve(new[] { new[] { 0f, 0f }, new[] { end, 0f } }, rv);
                }
                curve ??= new AnimationCurve(new Keyframe(0, v0), new Keyframe(end, v0));
                AnimationUtility.SetEditorCurve(clip, b, curve);
            }
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = loop, keepOriginalPositionY = true, keepOriginalOrientation = true, keepOriginalPositionXZ = true, stopTime = end });
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimationCurve Curve(float[][] ks, float offset)
        {
            var kf = new Keyframe[ks.Length];
            for (int i = 0; i < ks.Length; i++) kf[i] = new Keyframe(ks[i][0], ks[i][1] + offset);
            var c = new AnimationCurve(kf);
            for (int i = 0; i < kf.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto); }
            return c;
        }

        static float[][] K(params float[] tv) { var r = new float[tv.Length / 2][]; for (int i = 0; i < r.Length; i++) r[i] = new[] { tv[i * 2], tv[i * 2 + 1] }; return r; }

        /// <summary>짐꾼 전용 클립 5개를 만듦 (키워드: 이름 소문자 + idle · walk · slam · throw · bash)</summary>
        public static void MakeClips(GameObject fbx, Avatar avatar, string outName, string dir)
        {
            var rest = RestMuscles(fbx, avatar);
            Directory.CreateDirectory(dir + "Anim");
            string P(string s) => dir + "Anim/" + outName + s + ".anim";
            // 다리 기본: 윗다리 0.6 · 아랫다리 펴기 0.9 (1장 클립들과 같은 서 있는 자세), 몸은 트렁크 무게로 살짝 앞으로
            float W = WalkCycle, q = W / 4;

            // 대기: 숨쉬기 (허리 · 가슴이 살짝 오르내림)
            MakeClip(P("Idle"), outName + "Idle", IdleCycle, true, new Dictionary<string, float[][]>
            {
                { "Spine Front-Back", K(0, 0.12f, IdleCycle / 2, 0.06f, IdleCycle, 0.12f) },
                { "Chest Front-Back", K(0, 0.05f, IdleCycle / 2, -0.02f, IdleCycle, 0.05f) },
                { "Left Upper Leg Front-Back", K(0, 0.62f, IdleCycle, 0.62f) }, { "Right Upper Leg Front-Back", K(0, 0.62f, IdleCycle, 0.62f) },
                { "Left Lower Leg Stretch", K(0, 0.8f, IdleCycle / 2, 0.75f, IdleCycle, 0.8f) }, { "Right Lower Leg Stretch", K(0, 0.8f, IdleCycle / 2, 0.75f, IdleCycle, 0.8f) },
                { "Left Upper Leg In-Out", K(0, 0.15f, IdleCycle, 0.15f) }, { "Right Upper Leg In-Out", K(0, 0.15f, IdleCycle, 0.15f) },
            }, rest, K(0, 0f, IdleCycle / 2, -0.015f, IdleCycle, 0f));

            // 걷기: 무거운 발걸음 (발 디딜 때 몸이 내려앉고 좌우로 기우뚱, 팔 · 트렁크는 고정)
            MakeClip(P("Walk"), outName + "Walk", W, true, new Dictionary<string, float[][]>
            {
                { "Left Upper Leg Front-Back", K(0, 0.9f, q, 0.62f, 2 * q, 0.32f, 3 * q, 0.6f, W, 0.9f) },
                { "Right Upper Leg Front-Back", K(0, 0.32f, q, 0.6f, 2 * q, 0.9f, 3 * q, 0.62f, W, 0.32f) },
                { "Left Lower Leg Stretch", K(0, 0.9f, q, 0.85f, 2 * q, 0.75f, 3 * q, 0.25f, W, 0.9f) },
                { "Right Lower Leg Stretch", K(0, 0.75f, q, 0.25f, 2 * q, 0.9f, 3 * q, 0.85f, W, 0.75f) },
                { "Left Foot Up-Down", K(0, 0.2f, 2 * q, -0.1f, 3 * q, 0.3f, W, 0.2f) },
                { "Right Foot Up-Down", K(0, -0.1f, q, 0.3f, 2 * q, 0.2f, W, -0.1f) },
                { "Left Upper Leg In-Out", K(0, 0.15f, W, 0.15f) }, { "Right Upper Leg In-Out", K(0, 0.15f, W, 0.15f) },
                { "Spine Front-Back", K(0, 0.22f, q, 0.16f, 2 * q, 0.22f, 3 * q, 0.16f, W, 0.22f) },
                { "Spine Left-Right", K(0, 0.08f, 2 * q, -0.08f, W, 0.08f) },                 // 디딤발 쪽으로 기우뚱
                { "Spine Twist Left-Right", K(0, -0.06f, 2 * q, 0.06f, W, -0.06f) },
                { "Head Nod Down-Up", K(0, -0.05f, W, -0.05f) },
            }, rest, K(0, -0.05f, q, 0.02f, 2 * q, -0.05f, 3 * q, 0.02f, W, -0.05f));

            // 내리찍기: 0~0.6 뒤로 젖혀 트렁크를 들어 올림 → 0.7 허리 · 무릎을 굽혀 트렁크로 바닥을 쾅 → 0.82~1.95 숙인 채(빈틈) → 2.4 일어남
            float E = SlamEnd, h = SlamHit;
            MakeClip(P("Slam"), outName + "Slam", E, false, new Dictionary<string, float[][]>
            {
                { "Spine Front-Back", K(0, 0.12f, 0.55f, -0.75f, h - 0.04f, -0.6f, h + 0.08f, 0.9f, h + 0.2f, 0.8f, 1.95f, 0.78f, E, 0.12f) },
                { "Chest Front-Back", K(0, 0.05f, 0.55f, -0.6f, h + 0.08f, 0.72f, 1.95f, 0.62f, E, 0.05f) },
                { "UpperChest Front-Back", K(0, 0f, 0.55f, -0.4f, h + 0.08f, 0.5f, 1.95f, 0.45f, E, 0f) },
                { "Head Nod Down-Up", K(0, 0f, 0.55f, 0.3f, h + 0.08f, -0.5f, 1.95f, -0.45f, E, 0f) },
                { "Left Upper Leg Front-Back", K(0, 0.62f, 0.55f, 0.5f, h + 0.08f, 0.92f, 1.95f, 0.9f, E, 0.62f) },
                { "Right Upper Leg Front-Back", K(0, 0.62f, 0.55f, 0.5f, h + 0.08f, 0.92f, 1.95f, 0.9f, E, 0.62f) },
                { "Left Lower Leg Stretch", K(0, 0.8f, 0.55f, 0.9f, h + 0.08f, 0.25f, 1.95f, 0.3f, E, 0.8f) },
                { "Right Lower Leg Stretch", K(0, 0.8f, 0.55f, 0.9f, h + 0.08f, 0.25f, 1.95f, 0.3f, E, 0.8f) },
                { "Left Upper Leg In-Out", K(0, 0.15f, h + 0.08f, 0.35f, 1.95f, 0.35f, E, 0.15f) }, { "Right Upper Leg In-Out", K(0, 0.15f, h + 0.08f, 0.35f, 1.95f, 0.35f, E, 0.15f) },
                // 팔: 들어 올릴 때 살짝 위로, 찍을 때 앞으로 쭉 (트렁크를 내리꽂는 느낌)
                { "+Left Arm Down-Up", K(0, 0f, 0.55f, 0.25f, h + 0.08f, -0.1f, 1.95f, -0.1f, E, 0f) }, { "+Right Arm Down-Up", K(0, 0f, 0.55f, 0.25f, h + 0.08f, -0.1f, 1.95f, -0.1f, E, 0f) },
                { "+Left Arm Front-Back", K(0, 0f, 0.55f, 0.1f, h + 0.08f, 0.25f, 1.95f, 0.2f, E, 0f) }, { "+Right Arm Front-Back", K(0, 0f, 0.55f, 0.1f, h + 0.08f, 0.25f, 1.95f, 0.2f, E, 0f) },
            }, rest, K(0, 0f, 0.55f, 0.03f, h + 0.08f, -0.32f, h + 0.16f, -0.28f, 1.95f, -0.3f, E, 0f));

            // 던지기: 0~0.75 뒤로 젖히며 비틂 → 0.8 앞으로 내던짐(트렁크는 이때 런타임이 숨기고 날림) → 빈손을 앞으로 뻗었다 복귀
            float R = ThrowRelease, T = ThrowEnd;
            MakeClip(P("Throw"), outName + "Throw", T, false, new Dictionary<string, float[][]>
            {
                { "Spine Front-Back", K(0, 0.12f, R - 0.05f, -0.8f, R + 0.08f, 0.75f, R + 0.25f, 0.55f, T, 0.12f) },
                { "Chest Front-Back", K(0, 0.05f, R - 0.05f, -0.6f, R + 0.08f, 0.6f, T, 0.05f) },
                { "Spine Twist Left-Right", K(0, 0f, R - 0.05f, -0.35f, R + 0.08f, 0.3f, T, 0f) },
                { "Head Nod Down-Up", K(0, 0f, R - 0.05f, 0.3f, R + 0.08f, -0.3f, T, 0f) },
                { "Left Upper Leg Front-Back", K(0, 0.62f, R - 0.05f, 0.45f, R + 0.08f, 0.95f, T, 0.62f) },
                { "Right Upper Leg Front-Back", K(0, 0.62f, R - 0.05f, 0.8f, R + 0.08f, 0.35f, T, 0.62f) },
                { "Left Lower Leg Stretch", K(0, 0.8f, R - 0.05f, 0.9f, R + 0.08f, 0.55f, T, 0.8f) },
                { "Right Lower Leg Stretch", K(0, 0.8f, R - 0.05f, 0.5f, R + 0.08f, 0.9f, T, 0.8f) },
                { "+Left Arm Down-Up", K(0, 0f, R - 0.05f, 0.35f, R + 0.08f, 0.15f, R + 0.25f, 0.1f, T, 0f) }, { "+Right Arm Down-Up", K(0, 0f, R - 0.05f, 0.35f, R + 0.08f, 0.15f, R + 0.25f, 0.1f, T, 0f) },
                { "+Left Arm Front-Back", K(0, 0f, R - 0.05f, -0.1f, R + 0.08f, 0.6f, R + 0.25f, 0.5f, T, 0f) }, { "+Right Arm Front-Back", K(0, 0f, R - 0.05f, -0.1f, R + 0.08f, 0.6f, R + 0.25f, 0.5f, T, 0f) },
                { "+Left Forearm Stretch", K(0, 0f, R + 0.08f, 0.5f, R + 0.25f, 0.4f, T, 0f) }, { "+Right Forearm Stretch", K(0, 0f, R + 0.08f, 0.5f, R + 0.25f, 0.4f, T, 0f) },
            }, rest, K(0, 0f, R - 0.05f, -0.06f, R + 0.08f, -0.1f, T, 0f));

            // 방패 돌진: 0~0.25 낮게 웅크려 트렁크를 앞세움 → 1.0까지 버틴 자세 (돌진은 코드가 밀어 줌)
            MakeClip(P("Bash"), outName + "Bash", BashEnd, false, new Dictionary<string, float[][]>
            {
                { "Spine Front-Back", K(0, 0.12f, 0.25f, 0.6f, BashEnd, 0.55f) },
                { "Chest Front-Back", K(0, 0.05f, 0.25f, 0.3f, BashEnd, 0.3f) },
                { "Head Nod Down-Up", K(0, 0f, 0.25f, -0.3f, BashEnd, -0.3f) },
                { "Left Upper Leg Front-Back", K(0, 0.62f, 0.25f, 0.8f, BashEnd, 0.78f) },
                { "Right Upper Leg Front-Back", K(0, 0.62f, 0.25f, 0.35f, BashEnd, 0.38f) },
                { "Left Lower Leg Stretch", K(0, 0.8f, 0.25f, 0.55f, BashEnd, 0.6f) },
                { "Right Lower Leg Stretch", K(0, 0.8f, 0.25f, 0.85f, BashEnd, 0.85f) },
            }, rest, K(0, 0f, 0.25f, -0.16f, BashEnd, -0.14f));

            AssetDatabase.SaveAssets();
            Debug.Log($"[PorterRig] {outName} 전용 클립 5개 ({dir}Anim/)");
        }
    }
}
