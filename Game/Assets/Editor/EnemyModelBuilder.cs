// Tripo 적 모델(Art/Characters/Ch1/*) → 게임용 프리팹(Resources/Enemies/*.prefab)
// - 원본 텍스처를 그대로 쓰는 툰 재질(CatTang/Toon)로 바꿈
// - 키를 맞추고(목표 높이) 발바닥을 y = 0에, 몸 가운데를 x · z = 0에 둠
// - 정면이 +Z가 되도록 yaw 보정 (Tripo 원본 방향이 다르면 Yaw 표를 고침)
// EnemyFactory가 Resources.Load("Enemies/이름")으로 불러 "Model" 자식으로 붙임.
// - 리깅된 모델(Tripo Auto Rig · Mixamo 뼈대 → Humanoid)은 애니메이터를 붙임:
//   Idle · Run = 주인공 고양이 클립을 휴머노이드로 돌려 씀, Attack · Special = Tripo AI 모션(클립 이름의 키워드로 찾음)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class EnemyModelBuilder
    {
        const string SrcDir = "Assets/Art/Characters/Ch1/";
        const string OutDir = "Assets/Resources/Enemies/";

        const string KitDir = "Assets/Resources/Kit/Ch1/";

        // 원본 이름 · 목표 크기(m) · 정면 보정 yaw(도) · 크기 기준(높이 h / 가로 w) · 출력 폴더 · 출력 이름
        // 적 키 (2026-11-16 측정, 사용자: 신체 비율에 맞게): 머리 크기를 고양이 머리(목~정수리 0.80m, 고양이 키 2.17m · 2.7등신)에 맞추고
        // 몸은 모델 원래 비율대로. 고양이 머리 대비 배율 × 모델 등신 = 키
        //   폭탄병 0.9 × 2.5등신 = 1.8 · 폭탄병+ 0.98 × 2.8 = 2.2 · 무법자 1.0 × 3.4 = 2.7 · 무법자+ 1.1 × 3.4 = 3.0
        //   깡패 1.25 × 2.8 = 2.8 (어깨 0.62m, 고양이 0.33m의 약 2배) · 깡패+ 는 머리가 작아(4.6등신) 어깨 기준: 어깨 0.92m → 3.2
        //   중간 보스 1.45 × 3.4 = 3.9 · 보스 2.1 × 3.8 = 6.5 (어깨 2m) · 정면 보정 yaw(도) · 크기 기준(높이 h / 가로 w) · 출력 폴더 · 출력 이름
        static readonly (string name, float size, float yaw, char fit, string outDir, string outName)[] Models =
        {
            ("Ch1_Outlaw", 2.7f, 0f, 'h', OutDir, "Ch1_Outlaw"),
            ("Ch1_Brute", 2.8f, 0f, 'h', OutDir, "Ch1_Brute"),
            ("Ch1_Bomber", 1.8f, 0f, 'h', OutDir, "Ch1_Bomber"),
            ("Ch1_Outlaw_Plus", 3.0f, 0f, 'h', OutDir, "Ch1_Outlaw_Plus"),
            ("Ch1_Brute_Plus", 3.2f, 0f, 'h', OutDir, "Ch1_Brute_Plus"),
            ("Ch1_Bomber_Plus", 2.2f, 0f, 'h', OutDir, "Ch1_Bomber_Plus"),
            ("Ch1_MidBoss", 3.9f, 0f, 'h', OutDir, "Ch1_MidBoss"),
            ("Ch1_Boss", 6.5f, 0f, 'h', OutDir, "Ch1_Boss"),
            // 맵 타일 · 소품 키트 (Kit.cs가 Resources/Kit/Ch1/이름으로 불러 조립)
            ("Kit_Wall", 4f, 0f, 'w', KitDir, "Wall"),
            ("Kit_Cliff", 4f, 0f, 'w', KitDir, "Cliff"),
            ("Kit_Floor", 4f, 0f, 'w', KitDir, "Floor"),
            ("Kit_ClockTower", 7f, 0f, 'h', KitDir, "ClockTower"),
            // 화약통 · 상자 · 울타리는 Codex 도형 모델(블렌더 +Y 앞 → yaw 180), Tripo 토큰 절약 (2026-11-16 사용자)
            ("Kit_Keg", 1.1f, 180f, 'h', KitDir, "Keg"),
            ("Kit_Wagon", 1.9f, 0f, 'h', KitDir, "Wagon"),
            ("Kit_Crates", 1.6f, 180f, 'h', KitDir, "Crates"),
            ("Kit_Fence", 1.2f, 180f, 'h', KitDir, "Fence"),
            ("Kit_MeltClock", 2.8f, 0f, 'h', KitDir, "MeltClock"),
            ("Kit_BarCounter", 1.3f, 0f, 'h', KitDir, "BarCounter"),
            // 저디테일 맵 조각 — Codex가 블렌더 스크립트로 도형 모델링 (Art/tools/blender_kit_simple.py)
            ("Kit_PlankWall", 4f, 180f, 'w', KitDir, "PlankWall"),
            ("Kit_Boardwalk", 4f, 180f, 'w', KitDir, "Boardwalk"),
            ("Kit_Sandbags", 2.2f, 180f, 'w', KitDir, "Sandbags"),
            ("Kit_BarrelStack", 1.6f, 180f, 'h', KitDir, "BarrelStack"),
            ("Kit_HitchPost", 2f, 180f, 'w', KitDir, "HitchPost"),
            ("Kit_Trough", 1.6f, 180f, 'w', KitDir, "Trough"),
            ("Kit_Signpost", 2.4f, 180f, 'h', KitDir, "Signpost"),
            ("Kit_LampPost", 3f, 180f, 'h', KitDir, "LampPost"),
            ("Kit_Rock_0", 1.4f, 0f, 'w', KitDir, "Rock_0"),
            ("Kit_Rock_1", 1.0f, 0f, 'w', KitDir, "Rock_1"),
            ("Kit_Rock_2", 1.8f, 0f, 'w', KitDir, "Rock_2"),
            ("Kit_Railing", 4f, 180f, 'w', KitDir, "Railing"),
            ("Kit_SaloonDoor", 2.4f, 180f, 'h', KitDir, "SaloonDoor"),
            // 바닥 조각 (흩뿌려 반복을 깸, Art/tools/blender_kit_ground.py)
            ("Kit_GroundPlanks", 2.2f, 0f, 'w', KitDir, "GroundPlanks"),
            ("Kit_Pebbles", 1.4f, 0f, 'w', KitDir, "Pebbles"),
            ("Kit_DryGrass", 1.0f, 0f, 'w', KitDir, "DryGrass"),
            ("Kit_SandDrift", 2.4f, 0f, 'w', KitDir, "SandDrift"),
            ("Kit_Skull", 0.8f, 0f, 'w', KitDir, "Skull"),
            // 절차적 건물 줄용 정면 8종 + 골목 울타리 (블렌더 + Codex, Art/tools/blender_kit_facades.py). 폭이 정확해야 줄이 맞음
            ("Kit_Facade_Store", 5f, 180f, 'w', KitDir, "Facade_Store"),
            ("Kit_Facade_Hotel", 6f, 180f, 'w', KitDir, "Facade_Hotel"),
            ("Kit_Facade_Narrow", 3f, 180f, 'w', KitDir, "Facade_Narrow"),
            ("Kit_Facade_Adobe", 4f, 180f, 'w', KitDir, "Facade_Adobe"),
            ("Kit_Facade_Shack", 3f, 180f, 'w', KitDir, "Facade_Shack"),
            ("Kit_Facade_Bank", 4f, 180f, 'w', KitDir, "Facade_Bank"),
            ("Kit_Facade_Barn", 5f, 180f, 'w', KitDir, "Facade_Barn"),
            ("Kit_Facade_Blue", 4f, 180f, 'w', KitDir, "Facade_Blue"),
            ("Kit_AlleyFence", 2f, 180f, 'w', KitDir, "AlleyFence"),
            // 폭탄병이 던지는 모래 폭탄 (블렌더 + Codex, Art/tools/blender_kit_sandbomb.py)
            ("Kit_SandBomb", 0.55f, 0f, 'h', KitDir, "SandBomb"),
            // 방 모양을 만드는 큰 지형지물 (블렌더 + Codex, Art/tools/blender_kit_landmarks.py). 크기 = 예전 막힌 덩어리만큼(4~6m) 키운 가로 (2026-11-17 사용자: 저 정도 크기로)
            ("Kit_LM_RockSpire", 5.8f, 180f, 'w', KitDir, "LM_RockSpire"),
            ("Kit_LM_BoulderPile", 5.0f, 180f, 'w', KitDir, "LM_BoulderPile"),
            ("Kit_LM_WagonWreck", 5.4f, 180f, 'w', KitDir, "LM_WagonWreck"),
            ("Kit_LM_MineCart", 5.2f, 180f, 'w', KitDir, "LM_MineCart"),
            ("Kit_LM_DeadTree", 4.2f, 180f, 'w', KitDir, "LM_DeadTree"),
            ("Kit_LM_WaterTower", 4.0f, 180f, 'w', KitDir, "LM_WaterTower"),
            ("Kit_LM_Stagecoach", 5.2f, 180f, 'w', KitDir, "LM_Stagecoach"),
            ("Kit_LM_Well", 3.6f, 180f, 'w', KitDir, "LM_Well"),
            ("Kit_LM_HayCart", 5.4f, 180f, 'w', KitDir, "LM_HayCart"),
            ("Kit_LM_MarketStall", 5.2f, 180f, 'w', KitDir, "LM_MarketStall"),
            ("Kit_LM_LumberPile", 5.2f, 180f, 'w', KitDir, "LM_LumberPile"),
        };

        // 애니메이터 구성: 공격 클립 키워드 · 공격 재생 시간(초) · 특수 클립 키워드 · 특수 시간 · 공격 반복 여부
        static readonly Dictionary<string, (string atk, float atkDur, string sp, float spDur, bool atkLoop)> Anims = new()
        {
            { "Ch1_Outlaw", ("revolver", 1.0f, null, 0, false) },
            { "Ch1_Outlaw_Plus", ("revolver", 0.9f, null, 0, false) },
            // 깡패 Attack = 돌진 예고 발 구르기 (MakeStompClip, Enemy.BruteWindup 0.6초와 맞춤). 돌진 자체는 Run(이동 속도)로
            { "Ch1_Brute", ("brutestomp", 0.6f, "swings", 0.8f, false) },
            { "Ch1_Brute_Plus", ("brutestomp", 0.6f, "swings", 0.7f, false) },
            // 폭탄병 Special = 점프(웅크림 0.35 → 체공 0.55 → 착지). 클립은 MakeJumpClip이 근육 커브로 만듦 (Enemy.BomberCrouch · BomberAir와 맞춤)
            { "Ch1_Bomber", ("shakes", 0.8f, "bomberjump", 1.0f, true) },
            { "Ch1_Bomber_Plus", ("shakes", 0.7f, "bomberjump", 1.0f, true) },
            { "Ch1_MidBoss", ("shotgun", 1.0f, "revolver", 1.2f, false) },
            { "Ch1_Boss", ("swings", 1.1f, "swings", 0.9f, false) },
        };

        [MenuItem("CatTang/1장 적 프리팹 만들기 (Tripo)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(KitDir);
            MakeJumpClip();
            MakeStompClip();
            MakeThrowClip();
            int made = 0;
            foreach (var m in Models)
                if (Build(m.name, m.size, m.yaw, m.fit, m.outDir, m.outName)) made++;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 같은 호출에서 막 만든 컨트롤러는 프리팹 저장 때 참조가 빠질 때가 있어, 저장된 컨트롤러를 다시 연결
            foreach (var m in Models)
            {
                string pp = m.outDir + m.outName + ".prefab";
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SrcDir + m.outName + ".controller");
                if (!ctrl || !Anims.ContainsKey(m.outName) || !AssetDatabase.LoadAssetAtPath<GameObject>(pp)) continue;
                var contents = PrefabUtility.LoadPrefabContents(pp);
                var an = contents.GetComponentInChildren<Animator>();
                if (an) { an.runtimeAnimatorController = ctrl; PrefabUtility.SaveAsPrefabAsset(contents, pp); }
                PrefabUtility.UnloadPrefabContents(contents);
            }
            AssetDatabase.SaveAssets();
            // 컨트롤러 · 프리팹을 강제로 다시 가져와야 참조가 풀림 (안 하면 실행 중에 컨트롤러가 비어 보임)
            foreach (var m in Models)
            {
                if (!Anims.ContainsKey(m.outName)) continue;
                AssetDatabase.ImportAsset(SrcDir + m.outName + ".controller", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(m.outDir + m.outName + ".prefab", ImportAssetOptions.ForceUpdate);
            }
            Debug.Log($"[EnemyModelBuilder] 프리팹 {made}/{Models.Length}개");
        }

        public static bool Build(string name, float height, float yaw) => Build(name, height, yaw, 'h', OutDir, name);

        static GameObject FindSource(string name)
        {
            foreach (var ext in new[] { ".fbx", ".glb", ".gltf", ".obj", ".prefab" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(SrcDir + name + ext);
                if (go) return go;
            }
            // 생성기가 하위 폴더에 넣었을 수 있음
            foreach (var guid in AssetDatabase.FindAssets(name + " t:GameObject", new[] { SrcDir.TrimEnd('/') }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go) return go;
            }
            return null;
        }

        public static bool Build(string name, float height, float yaw, char fit, string outDir, string outName)
        {
            var src = FindSource(name);
            if (!src) { Debug.Log($"[EnemyModelBuilder] {name} 원본 없음 ({SrcDir}) — 건너뜀"); return false; }

            var root = new GameObject(outName);
            var inst = (GameObject)Object.Instantiate(src);
            inst.name = "Mesh";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localRotation = Quaternion.Euler(0, yaw, 0) * inst.transform.localRotation;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var avatar = HumanAvatar(src);
            if (avatar && Anims.TryGetValue(outName, out var spec))
            {
                // ??는 유니티의 '가짜 null'(없는 컴포넌트)을 걸러내지 못하므로 명시적으로 확인
                var anim = inst.GetComponent<Animator>();
                if (!anim) anim = inst.AddComponent<Animator>();
                anim.avatar = avatar;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.runtimeAnimatorController = MakeController(outName, spec);
            }
            else foreach (var a in inst.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);

            // 재질: 원본 텍스처 → 툰
            var cache = new Dictionary<Material, Material>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (!cache.TryGetValue(m, out var toon)) cache[m] = toon = ToonFor(name, m, cache.Count);
                    mats[i] = toon;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 크기 · 위치 맞춤
            var b = Bounds(inst);
            float basis = fit == 'w' ? b.size.x : b.size.y;
            if (basis > 0.0001f)
            {
                float s = height / basis;
                inst.transform.localScale *= s;
                b = Bounds(inst);
                // 바닥 타일은 윗면을 y = 0.02에, 나머지는 발바닥을 y = 0에
                float y = outName == "Floor" ? b.max.y - 0.02f : b.min.y;
                inst.transform.localPosition -= new Vector3(b.center.x, y, b.center.z);
            }

            string path = outDir + outName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[EnemyModelBuilder] {path} (키 {height}m)");
            return true;
        }

        static Avatar HumanAvatar(GameObject src)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(src)))
                if (o is Avatar a && a.isValid && a.isHuman) return a;
            return null;
        }

        /// <summary>1장 폴더 · 주인공 FBX 안의 휴머노이드 클립 중 이름에 키워드가 든 것</summary>
        static AnimationClip FindClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var dirs = new[] { SrcDir.TrimEnd('/'), "Assets/Art/Characters/CatHero" };
            foreach (var guid in AssetDatabase.FindAssets("t:Model", dirs))
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview") && c.name.ToLower().Contains(key) && c.isHumanMotion) return c;
            // 에디터에서 만든 .anim (MakeJumpClip 등)
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { SrcDir.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.EndsWith(".anim")) continue;
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (c && c.name.ToLower().Contains(key)) return c;
            }
            return null;
        }

        // ---------- 점프 클립 (휴머노이드 근육 커브로 직접 만듦) ----------
        const string JumpPath = SrcDir + "Anim/Ch1_BomberJump.anim";

        // 시각(초) 별 자세. 0~0.35 웅크림 · 0.38 이륙(다리 쭉, 팔 위로) · 0.5~0.75 공중 웅크림(무릎 당김) · 0.9 착지 웅크림 · 1.0 유지
        // 값: 윗다리 앞뒤(+ = 앞으로 굽힘) · 아랫다리 펴기(+ = 쭉) · 발(+ = 발끝 듦) · 척추 앞뒤(+ = 숙임) · 팔 위아래(+ = 위) · 팔 앞뒤(+ = 앞) · 팔꿈치 펴기 · 몸 높이 변화(RootT.y)
        static readonly (float t, float uLeg, float lLeg, float foot, float spine, float armUp, float armFwd, float fore, float dy)[] JumpKeys =
        {
            (0.00f, 0.60f,  0.90f,  0.10f,  0.05f, -0.40f,  0.25f, 0.65f,  0.00f),
            (0.30f, 1.05f, -0.45f,  0.45f,  0.55f, -0.75f, -0.55f, 0.55f, -0.22f),   // 깊이 웅크리고 팔을 뒤로
            (0.35f, 1.05f, -0.45f,  0.45f,  0.55f, -0.75f, -0.60f, 0.55f, -0.22f),
            (0.42f, 0.10f,  1.10f, -0.70f, -0.15f,  0.80f,  0.60f, 0.85f,  0.08f),   // 이륙: 몸을 쭉 펴고 팔을 머리 위로
            (0.55f, 1.20f, -0.75f,  0.25f,  0.45f,  0.35f,  0.55f, 0.25f,  0.05f),   // 공중: 무릎을 가슴으로 당겨 몸을 공처럼
            (0.75f, 1.15f, -0.65f,  0.25f,  0.40f,  0.45f,  0.50f, 0.30f,  0.05f),
            (0.86f, 0.55f,  0.60f, -0.25f,  0.10f,  0.60f,  0.30f, 0.70f,  0.00f),   // 착지 직전: 다리를 뻗어 바닥을 찾음
            (0.92f, 1.05f, -0.50f,  0.45f,  0.60f, -0.30f,  0.40f, 0.50f, -0.24f),   // 착지: 쿵 웅크림
            (1.00f, 0.95f, -0.30f,  0.40f,  0.50f, -0.35f,  0.35f, 0.55f, -0.18f),
        };

        [MenuItem("CatTang/폭탄병 점프 클립 만들기")]
        public static AnimationClip MakeJumpClip()
        {
            // 바탕 자세 = 고양이 idle 첫 프레임 (손가락 · 머리 · 발 IK 등 나머지 커브를 그대로 채움)
            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") idle = c;
            if (!idle) { Debug.LogWarning("[EnemyModelBuilder] 고양이 idle 클립 없음 — 점프 클립 못 만듦"); return null; }
            Directory.CreateDirectory(SrcDir + "Anim");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(JumpPath);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, JumpPath); }
            clip.ClearCurves();
            clip.name = "Ch1_BomberJump";
            float end = JumpKeys[JumpKeys.Length - 1].t;
            foreach (var b in AnimationUtility.GetCurveBindings(idle))
            {
                float v0 = AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.3f);
                var curve = KeysFor(b.propertyName, v0);
                if (curve == null) curve = new AnimationCurve(new Keyframe(0, v0), new Keyframe(end, v0));
                AnimationUtility.SetEditorCurve(clip, b, curve);
            }
            var s = new AnimationClipSettings { loopTime = false, keepOriginalPositionY = true, keepOriginalOrientation = true, keepOriginalPositionXZ = true, stopTime = end };
            AnimationUtility.SetAnimationClipSettings(clip, s);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyModelBuilder] {JumpPath} ({end}초)");
            return clip;
        }

        // ---------- 발 구르기 클립 (깡패 돌진 예고, 2026-11-17 사용자: 발탄처럼 발 구르는 모션) ----------
        // 0~0.12 몸을 낮게 숙임 → 오른발로 땅을 뒤로 긁고 쿵 내딛기 두 번(0.2 · 0.3 · 0.4 · 0.5) → 0.6 숙인 채 돌진 준비
        const string StompPath = SrcDir + "Anim/Ch1_BruteStomp.anim";

        [MenuItem("CatTang/깡패 발 구르기 클립 만들기")]
        public static AnimationClip MakeStompClip()
        {
            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") idle = c;
            if (!idle) return null;
            Directory.CreateDirectory(SrcDir + "Anim");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StompPath);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, StompPath); }
            clip.ClearCurves();
            clip.name = "Ch1_BruteStomp";
            const float end = 0.6f;
            var keys = new Dictionary<string, float[][]>
            {
                { "Spine Front-Back", new[] { new[] { 0f, 0.05f }, new[] { 0.12f, 0.55f }, new[] { 0.6f, 0.55f } } },
                { "Chest Front-Back", new[] { new[] { 0f, 0f }, new[] { 0.12f, 0.35f }, new[] { 0.6f, 0.35f } } },
                { "Head Nod Down-Up", new[] { new[] { 0f, 0f }, new[] { 0.12f, -0.35f }, new[] { 0.6f, -0.35f } } },
                // 디딤발(왼발): 무릎 굽혀 버팀
                { "Left Upper Leg Front-Back", new[] { new[] { 0f, 0.6f }, new[] { 0.12f, 0.85f }, new[] { 0.6f, 0.85f } } },
                { "Left Lower Leg Stretch", new[] { new[] { 0f, 0.9f }, new[] { 0.12f, 0.4f }, new[] { 0.6f, 0.4f } } },
                // 구르는 발(오른발): 뒤로 긁고 쿵 내딛기 두 번
                { "Right Upper Leg Front-Back", new[] { new[] { 0f, 0.6f }, new[] { 0.12f, 0.75f }, new[] { 0.2f, 0.05f }, new[] { 0.3f, 0.9f }, new[] { 0.4f, 0.05f }, new[] { 0.5f, 0.9f }, new[] { 0.6f, 0.75f } } },
                { "Right Lower Leg Stretch", new[] { new[] { 0f, 0.9f }, new[] { 0.2f, 0.15f }, new[] { 0.3f, 0.75f }, new[] { 0.4f, 0.15f }, new[] { 0.5f, 0.75f }, new[] { 0.6f, 0.5f } } },
                { "Right Foot Up-Down", new[] { new[] { 0f, 0.1f }, new[] { 0.2f, -0.5f }, new[] { 0.3f, 0.3f }, new[] { 0.4f, -0.5f }, new[] { 0.5f, 0.3f }, new[] { 0.6f, 0.2f } } },
                // 팔: 아래 · 앞으로 내려 들이받을 자세
                { "Left Arm Down-Up", new[] { new[] { 0f, -0.4f }, new[] { 0.12f, -0.75f }, new[] { 0.6f, -0.75f } } },
                { "Right Arm Down-Up", new[] { new[] { 0f, -0.4f }, new[] { 0.12f, -0.75f }, new[] { 0.6f, -0.75f } } },
                { "Left Arm Front-Back", new[] { new[] { 0f, 0.25f }, new[] { 0.12f, 0.6f }, new[] { 0.6f, 0.6f } } },
                { "Right Arm Front-Back", new[] { new[] { 0f, 0.25f }, new[] { 0.12f, 0.6f }, new[] { 0.6f, 0.6f } } },
                { "Left Forearm Stretch", new[] { new[] { 0f, 0.65f }, new[] { 0.12f, 0.3f }, new[] { 0.6f, 0.3f } } },
                { "Right Forearm Stretch", new[] { new[] { 0f, 0.65f }, new[] { 0.12f, 0.3f }, new[] { 0.6f, 0.3f } } },
            };
            foreach (var b in AnimationUtility.GetCurveBindings(idle))
            {
                float v0 = AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.3f);
                AnimationCurve curve;
                if (keys.TryGetValue(b.propertyName, out var ks))
                {
                    var kf = new Keyframe[ks.Length];
                    for (int i = 0; i < ks.Length; i++) kf[i] = new Keyframe(ks[i][0], ks[i][1]);
                    curve = new AnimationCurve(kf);
                    for (int i = 0; i < kf.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); }
                }
                else if (b.propertyName == "RootT.y") curve = new AnimationCurve(new Keyframe(0, v0), new Keyframe(0.12f, v0 - 0.12f), new Keyframe(end, v0 - 0.12f));
                else curve = new AnimationCurve(new Keyframe(0, v0), new Keyframe(end, v0));
                AnimationUtility.SetEditorCurve(clip, b, curve);
            }
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = false, keepOriginalPositionY = true, keepOriginalOrientation = true, keepOriginalPositionXZ = true, stopTime = end });
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        // ---------- 던지기 클립 (폭탄병 모래 폭탄) ----------
        // 0~0.28 오른팔을 머리 뒤로 젖히고 몸을 뒤로 → 0.28~0.4 앞으로 휘둘러 놓음(0.34 = 손을 떠나는 순간, Enemy.ThrowRelease) → 0.6 따라 숙였다 복귀
        const string ThrowPath = SrcDir + "Anim/Ch1_BomberThrow.anim";
        public const float ThrowEnd = 0.6f;

        [MenuItem("CatTang/폭탄병 던지기 클립 만들기")]
        public static AnimationClip MakeThrowClip()
        {
            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") idle = c;
            if (!idle) return null;
            Directory.CreateDirectory(SrcDir + "Anim");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ThrowPath);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ThrowPath); }
            clip.ClearCurves();
            clip.name = "Ch1_BomberThrow";
            const float end = ThrowEnd;
            var keys = new Dictionary<string, float[][]>
            {
                // 몸통: 뒤로 젖힘 → 앞으로 숙이며 던짐 → 복귀
                { "Spine Front-Back", new[] { new[] { 0f, 0.05f }, new[] { 0.28f, -0.4f }, new[] { 0.38f, 0.55f }, new[] { 0.48f, 0.45f }, new[] { end, 0.05f } } },
                { "Chest Front-Back", new[] { new[] { 0f, 0f }, new[] { 0.28f, -0.35f }, new[] { 0.38f, 0.45f }, new[] { end, 0f } } },
                { "Spine Twist Left-Right", new[] { new[] { 0f, 0f }, new[] { 0.28f, -0.45f }, new[] { 0.38f, 0.4f }, new[] { end, 0f } } },
                // 던지는 팔(오른팔): 머리 위 뒤로 → 앞 아래로 휘두름
                { "Right Shoulder Down-Up", new[] { new[] { 0f, 0f }, new[] { 0.28f, 0.8f }, new[] { 0.38f, 0.3f }, new[] { end, 0f } } },
                { "Right Arm Down-Up", new[] { new[] { 0f, -0.4f }, new[] { 0.28f, 1f }, new[] { 0.34f, 0.7f }, new[] { 0.42f, -0.1f }, new[] { end, -0.4f } } },
                { "Right Arm Front-Back", new[] { new[] { 0f, 0.25f }, new[] { 0.28f, -0.7f }, new[] { 0.34f, 0.3f }, new[] { 0.42f, 0.9f }, new[] { end, 0.25f } } },
                { "Right Forearm Stretch", new[] { new[] { 0f, 0.65f }, new[] { 0.28f, -0.6f }, new[] { 0.34f, 0.6f }, new[] { 0.42f, 1f }, new[] { end, 0.65f } } },
                // 반대 팔: 앞으로 뻗어 겨눔
                { "Left Arm Down-Up", new[] { new[] { 0f, -0.4f }, new[] { 0.28f, 0.3f }, new[] { 0.4f, -0.3f }, new[] { end, -0.4f } } },
                { "Left Arm Front-Back", new[] { new[] { 0f, 0.25f }, new[] { 0.28f, 0.9f }, new[] { 0.4f, -0.2f }, new[] { end, 0.25f } } },
                // 다리: 앞발 내딛기
                { "Left Upper Leg Front-Back", new[] { new[] { 0f, 0.6f }, new[] { 0.28f, 0.75f }, new[] { 0.38f, 0.9f }, new[] { end, 0.6f } } },
                { "Right Upper Leg Front-Back", new[] { new[] { 0f, 0.6f }, new[] { 0.28f, 0.35f }, new[] { 0.38f, 0.45f }, new[] { end, 0.6f } } },
            };
            foreach (var b in AnimationUtility.GetCurveBindings(idle))
            {
                float v0 = AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.3f);
                AnimationCurve curve;
                if (keys.TryGetValue(b.propertyName, out var ks))
                {
                    var kf = new Keyframe[ks.Length];
                    for (int i = 0; i < ks.Length; i++) kf[i] = new Keyframe(ks[i][0], ks[i][1]);
                    curve = new AnimationCurve(kf);
                    for (int i = 0; i < kf.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); }
                }
                else curve = new AnimationCurve(new Keyframe(0, v0), new Keyframe(end, v0));
                AnimationUtility.SetEditorCurve(clip, b, curve);
            }
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = false, keepOriginalPositionY = true, keepOriginalOrientation = true, keepOriginalPositionXZ = true, stopTime = end });
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        /// <summary>근육 이름에 맞는 키 곡선 (해당 없으면 null → 바탕 자세 유지). 좌우는 같게, 팔 · 다리 비대칭은 조금만</summary>
        static AnimationCurve KeysFor(string prop, float baseV)
        {
            System.Func<(float t, float uLeg, float lLeg, float foot, float spine, float armUp, float armFwd, float fore, float dy), float> pick = null;
            bool left = prop.StartsWith("Left");
            if (prop.EndsWith("Upper Leg Front-Back")) pick = k => k.uLeg + (left ? 0.05f : -0.05f);
            else if (prop.EndsWith("Lower Leg Stretch")) pick = k => k.lLeg;
            else if (prop.EndsWith("Foot Up-Down")) pick = k => k.foot;
            else if (prop == "Spine Front-Back") pick = k => k.spine * 0.5f;
            else if (prop == "Chest Front-Back") pick = k => k.spine * 0.35f;
            else if (prop == "UpperChest Front-Back") pick = k => k.spine * 0.25f;
            else if (prop == "Head Nod Down-Up") pick = k => -k.spine * 0.4f;      // 숙여도 고개는 앞을 봄
            else if (prop.EndsWith("Arm Down-Up")) pick = k => k.armUp;
            else if (prop.EndsWith("Arm Front-Back")) pick = k => k.armFwd;
            else if (prop.EndsWith("Forearm Stretch")) pick = k => k.fore;
            else if (prop.EndsWith("Shoulder Down-Up")) pick = k => k.armUp > 0.3f ? 0.3f : -0.2f;
            else if (prop == "RootT.y") pick = k => baseV + k.dy;
            if (pick == null) return null;
            var keys = new Keyframe[JumpKeys.Length];
            for (int i = 0; i < JumpKeys.Length; i++) keys[i] = new Keyframe(JumpKeys[i].t, pick(JumpKeys[i]));
            var c = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            for (int i = 0; i < keys.Length; i++) AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            return c;
        }

        static AnimatorController MakeController(string outName, (string atk, float atkDur, string sp, float spDur, bool atkLoop) spec)
        {
            // 지웠다 새로 만들면 GUID가 바뀌어 프리팹 참조가 끊김 → 있으면 그 자산을 비우고 다시 채움
            string path = SrcDir + outName + ".controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!ctrl) ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var prm in ctrl.parameters) ctrl.RemoveParameter(prm);
            var old = ctrl.layers[0].stateMachine;
            foreach (var cs in old.states) old.RemoveState(cs.state);
            foreach (var at in old.anyStateTransitions) old.RemoveAnyStateTransition(at);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Special", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Busy", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle"); idle.motion = FindClip("idle");
            var run = sm.AddState("Run"); run.motion = FindClip("run");
            sm.defaultState = idle;
            var t = idle.AddTransition(run); t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.12f;
            t = run.AddTransition(idle); t.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.18f;
            AddAction(sm, idle, "Attack", FindClip(spec.atk), spec.atkDur, spec.atkLoop);
            AddAction(sm, idle, "Special", FindClip(spec.sp), spec.spDur, false);
            // 폭탄병 모래 폭탄 투척 (2026-11-17: 떨기 클립으로 던져서 모션 없이 터지는 것처럼 보였음)
            if (outName.Contains("Bomber")) AddAction(sm, idle, "Throw", FindClip("bomberthrow"), ThrowEnd, false);
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // AnyState → 동작 (트리거). 반복 동작(폭탄병 점멸)은 Busy가 꺼질 때까지, 아니면 다 재생 후 Idle로
        static void AddAction(AnimatorStateMachine sm, AnimatorState idle, string trigger, AnimationClip clip, float dur, bool loop)
        {
            if (!clip) return;
            var st = sm.AddState(trigger);
            st.motion = clip;
            st.speed = dur > 0 ? clip.length / dur : 1f;
            var t = sm.AddAnyStateTransition(st);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false; t.duration = 0.06f; t.canTransitionToSelf = false;
            var back = st.AddTransition(idle);
            if (loop) { back.AddCondition(AnimatorConditionMode.IfNot, 0, "Busy"); back.hasExitTime = false; }
            else { back.hasExitTime = true; back.exitTime = 0.95f; }
            back.duration = 0.12f;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Material ToonFor(string name, Material src, int index)
        {
            string p = SrcDir + name + "_Toon" + (index > 0 ? "_" + index : "") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(m, p); }
            // Tripo 내보내기: 원본 색 텍스처를 <이름>_basecolor.jpg/png로 옆에 둠 (FBX 안 재질은 연결이 안 될 때가 많음)
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SrcDir + name + "_basecolor.jpg") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SrcDir + name + "_basecolor.png");
            if (!tex)
            foreach (var prop in new[] { "_BaseMap", "_MainTex", "baseColorTexture", "_BaseColorMap" })
                if (src.HasProperty(prop) && src.GetTexture(prop)) { tex = src.GetTexture(prop); break; }
            if (!tex) tex = src.mainTexture;
            m.SetTexture("_BaseMap", tex);
            // 텍스처 없는 도형 모델(Codex 블렌더 스크립트, 재질 색만 있음)은 원본 재질 색을 그대로
            Color flat = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.color : Color.white;
            m.SetColor("_BaseColor", tex ? Color.white : flat);
            // 1장 팔레트: 황혼 보라 그림자 + 금빛 테두리
            m.SetColor("_ShadeColor", new Color(0.72f, 0.58f, 0.66f));
            m.SetColor("_SecondShadeColor", new Color(0.45f, 0.34f, 0.46f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 4f);   // 2026-11-16 사용자: 외곽선 두껍게
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
