// 5장 (무너지는 시간의 끝, 2026-11-19) — 맵 키트 · 무기 · 적 · 거울 허상 · 강화형 · 중간 보스(거울 고양이) · 보스(시간의 주인) 프리팹
// - 키트 · 무기 · 강화 파츠 · 거울 허상: 블렌더 + Codex (회색 + 역할 재질 → ModelPalette.Ch5). 명세 Art/tools/specs/kit_ch5_*_spec.md + _ch5_common.md
// - 일반 적 5종 + 보스: 크롬 ChatGPT 원화(빈손 · 정면 A자) → 크롬 Tripo H3.1 + 리토포 1만 + 자동 리깅(Mixamo) → Art/Characters/Ch5/Ch5_*.fbx
// - 강화형 (사용자: Tripo 크레딧 부족 → 4장 강화 기사처럼): 기본 FBX 복사(Ch5_*_Plus.fbx) + 보랏빛 · 금 균열 텍스처(Art/tools/ch5_textures.py plusfbx)
//   + 블렌더 강화 파츠 Kit_EP_*(뼈에 붙임) + 강화 무기 Kit_W_*Plus
// - 중간 보스 거울 속의 고양이: 주인공 고양이(CatHero) 프리팹을 좌우 반전 + 거울 텍스처(ch5_textures.py mirror) + 거울 리볼버
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        const string Ch5Src = "Assets/Art/Characters/Ch5/";
        const string Kit5Dir = "Assets/Resources/Kit/Ch5/";
        // AttachWeapons · AttachArmor가 무기 · 파츠 프리팹을 읽는 폴더 (4장 기본값)
        static string PartsDir = "Assets/Resources/Kit/Ch4/";
        static string PartPrefix = "KP_";

        static readonly (string name, float size, char fit, string outName)[] Kit5 =
        {
            // 벽면 (blender_kit_ch5_walls.py) — 폭이 정확해야 줄이 맞음
            ("Kit_Wall_Mirror", 6f, 'w', "Facade_Mirror"),               // S1 부서진 거울 문턱
            ("Kit_Wall_MemoryWest", 6f, 'w', "Facade_MemoryWest"),       // S2 뒤섞인 기억 회랑 (서부 + 기차역)
            ("Kit_Wall_MemoryClock", 6f, 'w', "Facade_MemoryClock"),     // S2 (사막 + 시계탑)
            ("Kit_Wall_DebrisEdge", 6f, 'w', "Facade_DebrisEdge"),       // S3 떠도는 잔해 섬 가장자리 (밑으로 늘어짐)
            ("Kit_Wall_ClockHeart", 8f, 'w', "Facade_ClockHeart"),       // S4 무너지는 시계 심장
            ("Kit_Wall_StarArch", 6f, 'w', "Facade_StarArch"),           // 어느 스테이지든 (아치 너머 별밤)
            ("Kit_Wall", 4f, 'w', "Wall"),
            // 방 기믹 · 오브젝트 · 엄폐물 (blender_kit_ch5_props.py) — 앞 장 오브젝트가 보라 · 금으로 일그러진 모습
            ("Kit_WarpedKeg", 1.2f, 'h', "WarpedKeg"),                   // 1장 화약통
            ("Kit_WarpedTrunk", 1.4f, 'w', "WarpedTrunk"),               // 2장 멈춘 짐
            ("Kit_MirrorShard", 2.5f, 'w', "MirrorShard"),               // 3장 거울 조각 (양쪽 반사)
            ("Kit_WarpedSpring", 1.5f, 'h', "WarpedSpring"),             // 4장 태엽 스프링
            ("Kit_CoverShardRock", 1.8f, 'w', "CoverShardRock"),
            ("Kit_CoverMemoryPile", 1.8f, 'w', "CoverMemoryPile"),
            ("Kit_CoverBrokenColumn", 2.4f, 'h', "CoverBrokenColumn"),
            ("Kit_RiftCrack", 2.0f, 'h', "RiftCrack"),                   // 방 기믹 '허공의 균열' 3D 모습
            ("Kit_ShardBits", 1.2f, 'w', "ShardBits"),
            ("Kit_CrackGlow", 3.0f, 'w', "CrackGlow"),
            ("Kit_StarDust", 1.0f, 'w', "StarDust"),
            // 큰 지형지물 (blender_kit_ch5_landmarks.py) — 앞 장 기억 조각
            ("Kit_LM_MirrorMonolith", 3.0f, 'w', "LM_MirrorMonolith"),
            ("Kit_LM_SaloonFragment", 4.6f, 'w', "LM_SaloonFragment"),
            ("Kit_LM_TrainFragment", 5.4f, 'w', "LM_TrainFragment"),
            ("Kit_LM_ClockHeart", 4.4f, 'w', "LM_ClockHeart"),
            ("Kit_LM_Hourglass", 2.8f, 'w', "LM_Hourglass"),
            ("Kit_LM_DesertShard", 4.0f, 'w', "LM_DesertShard"),
            // 깨지고 일그러진 바닥 (blender_kit_ch5_floor.py, 2026-11-19 사용자: 4장 바닥 재활용 말고 훨씬 깨지고 일그러지게 + 이전 장 바닥 조각이 섞이게).
            // FloorPatch_* 윗면(patch_top) = 게임이 1~4장 바닥 재질로 바꿔 끼움 (Ch5Kit.FloorBreakup)
            ("Kit_FloorPatch_A", 3.6f, 'w', "FloorPatch_A"),
            ("Kit_FloorPatch_B", 2.6f, 'w', "FloorPatch_B"),
            ("Kit_FloorPatch_C", 4.2f, 'w', "FloorPatch_C"),
            ("Kit_FloorVoidPit", 2.4f, 'w', "FloorVoidPit"),
            ("Kit_FloorSlabTilted", 2.2f, 'w', "FloorSlabTilted"),
            ("Kit_FloorShardSpike", 1.4f, 'w', "FloorShardSpike"),
            ("Kit_FloorWarpRidge", 3.0f, 'w', "FloorWarpRidge"),
            // 떠다니는 기억 잔해 · 보스 문자판 경기장 (blender_kit_ch5_debris.py, 2026-11-20 사용자: 맵이 밋밋함 · 초현실) — FD_ · 떠 있는 숫자는 원점 = 가운데
            ("Kit_FD_Saloon", 2.8f, 'w', "FD_Saloon"),
            ("Kit_FD_Rail", 3.0f, 'w', "FD_Rail"),
            ("Kit_FD_StationClock", 2.2f, 'w', "FD_StationClock"),
            ("Kit_FD_Gears", 2.4f, 'w', "FD_Gears"),
            ("Kit_FD_MirrorFrame", 2.2f, 'w', "FD_MirrorFrame"),
            ("Kit_FD_DesertPillar", 2.0f, 'h', "FD_DesertPillar"),
            ("Kit_FD_Bell", 1.8f, 'w', "FD_Bell"),
            ("Kit_FD_Island", 3.0f, 'w', "FD_Island"),
            ("Kit_BA_DialRing", 42f, 'w', "BA_DialRing"),
            ("Kit_BA_HandSpire", 9f, 'h', "BA_HandSpire"),
            ("Kit_BA_FloatingNumeral", 2.5f, 'w', "BA_FloatingNumeral"),
        };

        [MenuItem("CatTang/5장 키트 만들기 (블렌더)")]
        public static void BuildCh5Kit()
        {
            Directory.CreateDirectory(Kit5Dir);
            SrcDir = Ch5Src;
            ModelPalette.Active = ModelPalette.Ch5;
            KeepOrigin.Add("Kit_Wall_DebrisEdge");      // 섬 가장자리: 윗면 = 원점, 밑면은 바닥 아래로 늘어짐
            KeepOrigin.Add("Kit_Corridor_S3");
            foreach (var fd in new[] { "Kit_FD_Saloon", "Kit_FD_Rail", "Kit_FD_StationClock", "Kit_FD_Gears", "Kit_FD_MirrorFrame", "Kit_FD_DesertPillar", "Kit_FD_Bell", "Kit_FD_Island", "Kit_BA_FloatingNumeral" })
                KeepOrigin.Add(fd);                      // 떠 있는 잔해: 원점 = 가운데 (게임이 공중에서 돌림)          // 떠 있는 돌다리 복도: 밑면 바위가 아래로 늘어짐
            try
            {
                int made = 0;
                foreach (var k in Kit5)
                    if (File.Exists(Ch5Src + k.name + ".fbx") && Build(k.name, k.size, 180f, k.fit, Kit5Dir, k.outName)) made++;
                GroundMat("Ground", "Assets/Art/Env/Ch5/ground_ch5_a.png", Kit5Dir, mirror: false);
                // 바닥 셰이더 (CatTang/RiftFloor): 2026-11-20 사용자: 어지러운 패턴을 한 그림에 섞으니 가시성만 떨어짐 →
                // 패턴 하나씩 담은 바닥 6장(판석 · 체커 · 판자 · 역 타일 · 시계판 석판 · 자갈)을 불규칙한 다각형 조각으로 나눠 섞고, 같은 패턴 이웃은 합침
                var gm = AssetDatabase.LoadAssetAtPath<Material>(Kit5Dir + "Ground.mat");
                var rf = Shader.Find("CatTang/RiftFloor");
                if (gm && rf)
                {
                    var tex = gm.GetTexture("_BaseMap"); gm.shader = rf; gm.SetTexture("_BaseMap", tex);
                    gm.SetTextureScale("_BaseMap", Vector2.one); gm.SetTextureOffset("_BaseMap", Vector2.zero);
                    var pt = new[] { ("_Tex2", "b"), ("_Tex3", "c"), ("_Tex4", "d"), ("_Tex5", "e"), ("_Tex6", "f") };
                    int count = 1;
                    foreach (var (prop, key) in pt) { var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Env/Ch5/ground_ch5_{key}.png"); if (t2) { gm.SetTexture(prop, t2); count++; } }
                    gm.SetFloat("_ChunkCount", count); gm.SetFloat("_ChunkSize", 5f); gm.SetFloat("_ChunkSplit", 0.6f); gm.SetFloat("_ChunkTile", 6f);
                    gm.SetFloat("_SeamWidth", 0.035f); gm.SetFloat("_SeamGlow", 0.35f);
                    // 그림 자체가 조용한 단일 패턴이라 가시성 보정은 약하게, 일렁임도 약하게
                    gm.SetFloat("_Desat", 0.05f); gm.SetFloat("_Darken", 0.85f); gm.SetColor("_Tint", new Color(0.92f, 0.98f, 1.06f)); gm.SetFloat("_Contrast", 0.8f); gm.SetFloat("_Soft", 0.8f);
                    // 배색 (2026-11-20 사용자: 보라색이 별로 → 푸른 계열로 몽환적 · 신비롭게): 바닥 그림은 ground_texture_ch5.py가 푸른 계열로 돌림,
                    // 거울 = 푸른 하늘빛, 허공 = 짙은 남색, 이음매 빛 = 금 대신 옅은 청록 (별빛 같은 차가운 빛)
                    gm.SetColor("_MirrorDark", new Color(0.2f, 0.3f, 0.46f)); gm.SetColor("_MirrorLight", new Color(0.56f, 0.76f, 0.92f));
                    gm.SetColor("_VoidColor", new Color(0.04f, 0.09f, 0.24f)); gm.SetColor("_GlowColor", new Color(0.55f, 0.88f, 1f)); gm.SetFloat("_Glow", 1.3f);
                    // v6 시간 균열 (2026-11-20 사용자: 시간의 뒤틀림과 균열이 안 보임): 허공 위 깨진 판 · 넓은 틈으로 시차 허공 · 가장자리 빛 똑딱 · 시계 소용돌이 · 잔상
                    gm.SetFloat("_GapChance", 0.5f); gm.SetFloat("_GapMax", 1.0f); gm.SetFloat("_RimWidth", 0.07f);
                    gm.SetFloat("_WarpAmount", 0.004f);
                    EditorUtility.SetDirty(gm);
                }
                RiseFx5();
                // 균열이 깨질 때 튀는 유리 조각 (메시 파티클): FBX를 그대로 Resources로 + 빛 안 받는 단색 재질
                foreach (var fx in new[] { "FX_GlassShard", "FX_GlassShard2" })
                    if (File.Exists(Ch5Src + "Kit_" + fx + ".fbx")) { AssetDatabase.DeleteAsset(Kit5Dir + fx + ".fbx"); AssetDatabase.CopyAsset(Ch5Src + "Kit_" + fx + ".fbx", Kit5Dir + fx + ".fbx"); }
                FxMat5("FX_GlassShardMat", Color.white);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EnemyModelBuilder] 5장 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
            BuildGates();
        }

        static Material FxMat5(string name, Color c)
        {
            string p = Kit5Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetFloat("_Surface", 0f);
            m.SetColor("_BaseColor", c);
            m.renderQueue = -1;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>5장 키트 · 무기 · 파츠: 금 균열 · 별빛 · 거울 · 자홍 보석은 툰 대신 빛나는 단색(CatTang/Silhouette)</summary>
        static void Ch5GlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = Ch5Src + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var crack = Get("Glow_CrackGold", new Color(0.5f, 0.92f, 1f, 1f), 0.3f);          // v2 천문대: 청록 시간 균열 (재질 이름은 예전 그대로)
            var star = Get("Glow_Starlight", new Color(1f, 0.98f, 0.92f, 1f), 0.2f);
            var mirror = Get("Glow_Mirror5", new Color(0.82f, 0.88f, 1f, 1f), 0.6f);
            var magenta = Get("Glow_Magenta", new Color(1f, 0.3f, 0.78f, 1f), 0.3f);
            // 이전 장 바닥 조각 윗면: 자리 표시용 재질 (이름 Patch_Top5 → 게임이 1~4장 Ground 재질로 바꿈)
            string pp = Ch5Src + "Patch_Top5.mat";
            var patch = AssetDatabase.LoadAssetAtPath<Material>(pp);
            if (!patch) { patch = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(patch, pp); }
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = SrcMatName(r, i);
                    var pick = src == "crack_gold" ? crack : src == "starlight" ? star : src == "mirror" ? mirror : src == "magenta" ? magenta : src == "patch_top" ? patch : null;
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // ---------- 5장 적 ----------
        // 키: 4장과 같은 2등신 인형 비율 (고양이 머리 기준), 거인 · 보스는 크게. 강화형은 약 15% 크게 (1 · 2 · 4장 강화형과 같은 비율)
        static readonly (string name, float size)[] Models5 =
        {
            ("Ch5_Gunner", 2.5f), ("Ch5_Witch", 2.5f), ("Ch5_Giant", 3.4f), ("Ch5_Assassin", 2.6f), ("Ch5_Caster", 2.5f), ("Ch5_Boss", 5.6f), ("Ch5_LordOfTime", 6.2f),   // 2026-11-20 사용자: 진짜 시간의 주인 / 사신 느낌, 위압감 → Hyper3D 새 모델 (원화 Art/tripo_input/ch5_boss/lord_of_time.png)
            ("Ch5_Gunner_Plus", 2.9f), ("Ch5_Witch_Plus", 2.9f), ("Ch5_Giant_Plus", 3.9f), ("Ch5_Assassin_Plus", 3.0f), ("Ch5_Caster_Plus", 2.9f),
        };
        // 무기: 적 이름 → (무기, 손) 목록. 손 = true 오른손. 강화형은 큰 강화 무기
        static readonly Dictionary<string, (string weapon, bool right)[]> Weapons5 = new()
        {
            { "Ch5_Gunner", new[] { ("ShardRifle", true) } },
            { "Ch5_Witch", new[] { ("StarStaff", true) } },
            { "Ch5_Giant", new[] { ("RuinMaul", true) } },
            { "Ch5_Assassin", new[] { ("RiftBlade", true), ("RiftBlade", false) } },
            { "Ch5_Caster", new[] { ("RiftCenser", true) } },
            { "Ch5_Boss", new[] { ("TimeScythe", true) } },
            { "Ch5_LordOfTime", new[] { ("TimeScythe", true) } },
            { "Ch5_Gunner_Plus", new[] { ("ShardRiflePlus", true) } },
            { "Ch5_Witch_Plus", new[] { ("StarStaffPlus", true) } },
            { "Ch5_Giant_Plus", new[] { ("RuinMaulPlus", true) } },
            { "Ch5_Assassin_Plus", new[] { ("RiftBladePlus", true), ("RiftBladePlus", false) } },
            { "Ch5_Caster_Plus", new[] { ("RiftCenserPlus", true) } },
        };
        static readonly string[] WeaponNames5 = { "ShardRifle", "StarStaff", "RuinMaul", "RiftBlade", "RiftCenser", "TimeScythe", "MirrorPistol",
            "ShardRiflePlus", "StarStaffPlus", "RuinMaulPlus", "RiftBladePlus", "RiftCenserPlus" };
        // 강화 파츠 Kit_EP_* (원점 = 붙일 뼈 자리, +Y 앞 · +X 오른쪽, 2.2m 인형 기준 — kit_ch5_plus_spec.md). 보정 = 2.2m 기준 거리 (x 오른쪽 · y 위 · z 앞)
        // 적마다 몸 모양이 달라 고른 조합: 모자 큰 마녀 · 뾰족 후드 술사는 머리 파츠 없이
        static readonly string[] PartNames5 = { "ShoulderRift_R", "ShoulderRift_L", "BackRift", "HeadShards", "HipShards", "ArmShard_R", "ArmShard_L" };
        static readonly Dictionary<string, (string piece, HumanBodyBones bone, Vector3 offset)[]> Armor5 = new()
        {
            { "Ch5_Gunner_Plus", new[] { ("ShoulderRift_R", HumanBodyBones.RightUpperArm, Vector3.zero), ("ShoulderRift_L", HumanBodyBones.LeftUpperArm, Vector3.zero),
                ("BackRift", HumanBodyBones.UpperChest, Vector3.zero), ("HipShards", HumanBodyBones.Hips, Vector3.zero) } },
            { "Ch5_Witch_Plus", new[] { ("BackRift", HumanBodyBones.UpperChest, Vector3.zero), ("HipShards", HumanBodyBones.Hips, Vector3.zero),
                ("ArmShard_R", HumanBodyBones.RightLowerArm, Vector3.zero), ("ArmShard_L", HumanBodyBones.LeftLowerArm, Vector3.zero) } },
            { "Ch5_Giant_Plus", new[] { ("ShoulderRift_R", HumanBodyBones.RightUpperArm, Vector3.zero), ("ShoulderRift_L", HumanBodyBones.LeftUpperArm, Vector3.zero),
                ("BackRift", HumanBodyBones.UpperChest, Vector3.zero), ("HeadShards", HumanBodyBones.Head, Vector3.zero),
                ("ArmShard_R", HumanBodyBones.RightLowerArm, Vector3.zero), ("ArmShard_L", HumanBodyBones.LeftLowerArm, Vector3.zero) } },
            { "Ch5_Assassin_Plus", new[] { ("ShoulderRift_R", HumanBodyBones.RightUpperArm, Vector3.zero), ("ShoulderRift_L", HumanBodyBones.LeftUpperArm, Vector3.zero),
                ("HeadShards", HumanBodyBones.Head, Vector3.zero), ("ArmShard_R", HumanBodyBones.RightLowerArm, Vector3.zero), ("ArmShard_L", HumanBodyBones.LeftLowerArm, Vector3.zero) } },
            { "Ch5_Caster_Plus", new[] { ("ShoulderRift_R", HumanBodyBones.RightUpperArm, Vector3.zero), ("ShoulderRift_L", HumanBodyBones.LeftUpperArm, Vector3.zero),
                ("BackRift", HumanBodyBones.UpperChest, Vector3.zero), ("HipShards", HumanBodyBones.Hips, Vector3.zero) } },
        };

        // 보스 '시간의 주인' 부품: 깨진 문자판 후광 · 어깨 갑옷 · 왕관 · 망토 조각은 뼈에, 공전 모래시계 · 톱니는 게임(BossAura)이 띄움
        static readonly string[] BossParts5 = { "Halo", "Pauldron_L", "Pauldron_R", "Crown", "CapeShards", "Orbit_Hourglass", "Orbit_Gear" };
        static readonly (string piece, HumanBodyBones bone, Vector3 offset)[] BossArmor5 =
        {
            ("Halo", HumanBodyBones.Head, new Vector3(0, 0.12f, -0.34f)),          // 머리 뒤에 서 있는 후광 (2.2m 기준 값, 키 비례로 커짐)
            ("Crown", HumanBodyBones.Head, new Vector3(0, 0.36f, 0)),
            ("Pauldron_L", HumanBodyBones.LeftUpperArm, Vector3.zero),
            ("Pauldron_R", HumanBodyBones.RightUpperArm, Vector3.zero),
            ("CapeShards", HumanBodyBones.Hips, new Vector3(0, -0.92f, -0.08f)),   // 골반에서 바닥으로 (망토 끝이 허공으로 흩어짐)
        };

        /// <summary>
        /// 보스 다듬기 (2026-11-20 사용자: 낫이 잘려 보임): 낫 자루 가운데 큰 보석이 빛나는 단색(명암 · 외곽선 없음)이라 자홍 판처럼 자루를 가려 끊겨 보였음
        /// → 보스 몸의 자홍 빛나는 단색만 툰 재질(자홍)로 바꿈. 낫(키 비례 7.4m)은 0.72배, 후광은 0.7배 (화면을 덮지 않게)
        /// </summary>
        static void TuneBoss5(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                string mp = Ch5Src + "Boss_Magenta_Toon.mat";
                var toon = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (!toon) { toon = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(toon, mp); }
                toon.SetColor("_BaseColor", new Color(1f, 0.3f, 0.75f));
                if (toon.HasProperty("_OutlineWidth")) toon.SetFloat("_OutlineWidth", 3f);
                EditorUtility.SetDirty(toon);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials; bool ch = false;
                    for (int i = 0; i < mats.Length; i++) if (mats[i] && mats[i].name == "Glow_Magenta") { mats[i] = toon; ch = true; }
                    if (ch) r.sharedMaterials = mats;
                }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Weapon_TimeScythe") t.localScale *= 0.72f;
                    if (t.name == "Armor_Halo") t.localScale *= 0.7f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("CatTang/5장 무기 · 강화 파츠 만들기 (블렌더)")]
        public static void BuildCh5Weapons()
        {
            Directory.CreateDirectory(Kit5Dir);
            SrcDir = Ch5Src;
            ModelPalette.Active = ModelPalette.Ch5;
            try
            {
                int made = 0;
                var pieces = new List<string>();
                foreach (var w in WeaponNames5) pieces.Add("W_" + w);
                foreach (var a in PartNames5) pieces.Add("EP_" + a);
                foreach (var b in BossParts5) pieces.Add("BP_" + b);       // 보스 강화 부품 (blender_kit_ch5_bossparts.py, 2026-11-20 사용자: 보스가 더 강력한 느낌)
                foreach (var w in pieces)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>(Ch5Src + "Kit_" + w + ".fbx");
                    if (!src) continue;
                    var root = new GameObject(w);
                    var inst = (GameObject)Object.Instantiate(src);
                    inst.name = "Mesh";
                    inst.transform.SetParent(root.transform, false);           // 원점 = 손잡이 / 뼈 자리 그대로
                    var cache = new Dictionary<Material, Material>();
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i]) { if (!cache.TryGetValue(mats[i], out var t)) cache[mats[i]] = t = ToonFor("Kit_" + w, mats[i], cache.Count); mats[i] = t; }
                        r.sharedMaterials = mats;
                    }
                    Ch5GlowMats(inst);
                    foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    PrefabUtility.SaveAsPrefabAsset(root, Kit5Dir + w + ".prefab");
                    Object.DestroyImmediate(root);
                    made++;
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[EnemyModelBuilder] 5장 무기 · 강화 파츠 {made}개");
            }
            finally { SrcDir = Ch1Src; ModelPalette.Active = ModelPalette.Ch1; }
        }

        // ---------- 떠오르는 부스러기 (2026-11-20 사용자: 마을처럼 파티클로 분리되어 상승하는 부스러기, 벽과 중간중간 오브젝트에도) ----------
        // 마을 RiseSpot(VillageKitBuilder)과 같은 짜임: 돌 · 놋쇠 조각이 천천히 돌며 떠오르다 작아져 사라짐 + 청록 빛 모래.
        // 배색 = 천문대(남색 벽돌 · 흰 돌 · 놋쇠 · 청록). 프리팹 Resources/Kit/Ch5/FX_Rise → Ch5Rise.Scatter가 벽 꼭대기 · 지형지물 · 엄폐물에 군데군데 붙임
        static void RiseFx5()
        {
            Material Mat(string name, string tex, bool additive)
            {
                string p = Kit5Dir + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_BaseColor", Color.white);
                if (tex != null)
                {
                    m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Env/Village/" + tex));
                    m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)); m.SetFloat("_ZWrite", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.renderQueue = 3000;
                }
                EditorUtility.SetDirty(m);
                return m;
            }
            var root = new GameObject("FX_Rise");
            var go = new GameObject("Bits");
            go.transform.SetParent(root.transform, false);
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.16f, 0.4f);
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(0.2f, 0.27f, 0.45f), 0f), new GradientColorKey(new Color(0.86f, 0.9f, 0.96f), 0.3f), new GradientColorKey(new Color(0.36f, 0.46f, 0.62f), 0.55f),
                                 new GradientColorKey(new Color(0.92f, 0.74f, 0.4f), 0.8f), new GradientColorKey(new Color(0.5f, 0.92f, 1f), 1f) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.7f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = rot.y = rot.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.2f), new Keyframe(0.15f, 1), new Keyframe(0.75f, 0.8f), new Keyframe(1, 0)));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.3f; noise.scrollSpeed = 0.15f;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Mesh;
            pr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            pr.sharedMaterial = Mat("FX_RiseBitsMat", null, false);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.alignment = ParticleSystemRenderSpace.World;
            var g2 = new GameObject("Motes");
            g2.transform.SetParent(go.transform, false);
            var ps2 = g2.AddComponent<ParticleSystem>();
            var m2 = ps2.main;
            m2.loop = true; m2.prewarm = true;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            m2.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.92f, 1f), new Color(0.95f, 0.85f, 0.6f));
            m2.simulationSpace = ParticleSystemSimulationSpace.World;
            m2.maxParticles = 110;
            var e2 = ps2.emission; e2.rateOverTime = 14f;
            var s2 = ps2.shape; s2.shapeType = ParticleSystemShapeType.Cone; s2.angle = 10f; s2.radius = 0.6f;
            var c2 = ps2.colorOverLifetime; c2.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0, 1) });
            c2.color = gr;
            var pr2 = g2.GetComponent<ParticleSystemRenderer>();
            pr2.renderMode = ParticleSystemRenderMode.Billboard;
            pr2.sharedMaterial = Mat("FX_RiseMotesMat", "spark.png", true);
            pr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(root, Kit5Dir + "FX_Rise.prefab");
            Object.DestroyImmediate(root);
        }

        // 5장 무기 방향 (2026-11-19 사용자: 보스 · 마녀 손 위치 이상함). 무기 원점 = 손잡이, 긴 축 = 무기 +Y (Kit_W_* 측정).
        // 몸 기준(x 오른쪽 · y 위 · z 앞)으로 무기 +Y가 가리킬 방향과 무기 +X가 향할 방향. 왼손은 x를 뒤집음. idle 자세의 손바닥에서 이 방향으로 붙임
        static readonly Dictionary<string, (Vector3 up, Vector3 right)> Aim5Dirs = new()
        {
            { "StarStaff", (new Vector3(0.05f, 1f, 0.15f), Vector3.right) },        // 지팡이: 곧게 세우고 별 머리가 위
            { "ShardRifle", (new Vector3(0f, -0.12f, 1f), Vector3.right) },         // 소총: 총구 앞 · 살짝 아래 (허리 조준)
            { "RuinMaul", (new Vector3(0.25f, -0.85f, 0.45f), Vector3.right) },     // 망치: 세우면 머리가 얼굴을 가림 → 손에서 앞아래로 늘어뜨려 끌고 다님
            { "RiftCenser", (new Vector3(0.1f, -1f, 0.15f), Vector3.right) },       // 향로: 사슬 손잡이에서 아래로 늘어뜨림
            { "RiftBlade", (new Vector3(0f, -0.35f, 1f), Vector3.right) },          // 균열 칼: 앞으로 겨누고 끝이 살짝 아래
            { "TimeScythe", (new Vector3(0.08f, 1f, 0.1f), new Vector3(0f, 0f, -1f)) },   // 대낫: 자루를 세우고 날(무기 −X)이 앞을 봄
        };

        static bool Aim5(Transform body, string weapon, bool right, out Quaternion rot)
        {
            rot = Quaternion.identity;
            string key = weapon.EndsWith("Plus") ? weapon.Substring(0, weapon.Length - 4) : weapon;
            if (!Aim5Dirs.TryGetValue(key, out var d)) return false;
            var up = d.up; var rx = d.right;
            if (!right) { up.x = -up.x; rx.x = -rx.x; }
            var Y = body.TransformDirection(up).normalized;
            var X = body.TransformDirection(rx);
            X = (X - Vector3.Dot(X, Y) * Y).normalized;
            rot = Quaternion.LookRotation(Vector3.Cross(X, Y), Y);
            return true;
        }

        /// <summary>idle 클립을 0.3초 자리에 놓고, 원래 뼈 자세(되돌릴 값)를 돌려줌</summary>
        static List<(Transform t, Vector3 lp, Quaternion lr)> SampleIdle5(Animator anim)
        {
            var saved = new List<(Transform, Vector3, Quaternion)>();
            foreach (var t in anim.GetComponentsInChildren<Transform>(true)) saved.Add((t, t.localPosition, t.localRotation));
            AnimationClip idle = null;
            if (anim.runtimeAnimatorController)
                foreach (var c in anim.runtimeAnimatorController.animationClips)
                    if (c && c.name.ToLower().Contains("idle")) { idle = c; break; }
            if (idle) idle.SampleAnimation(anim.gameObject, 0.3f);
            return saved;
        }

        [MenuItem("CatTang/5장 적 프리팹 만들기 (Tripo + 블렌더)")]
        public static void BuildCh5()
        {
            Directory.CreateDirectory(OutDir);
            BuildCh5Weapons();
            SrcDir = Ch5Src;
            PartsDir = Kit5Dir; PartPrefix = "EP_";
            try
            {
                foreach (var (n, h) in Models5)
                {
                    var imp = AssetImporter.GetAtPath(Ch5Src + n + ".fbx") as ModelImporter;
                    if (!imp) { Debug.Log($"[EnemyModelBuilder] {n} 원본 없음 — 건너뜀"); continue; }
                    if (imp.animationType != ModelImporterAnimationType.Human || imp.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        imp.animationType = ModelImporterAnimationType.Human;
                        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        imp.materialImportMode = ModelImporterMaterialImportMode.None;
                        imp.SaveAndReimport();
                    }
                    if (!Build(n, h, 0f, 'h', OutDir, n)) continue;
                    if (Weapons5.TryGetValue(n, out var ws)) AttachWeapons(OutDir + n + ".prefab", ws, h);
                    if (Armor5.TryGetValue(n, out var ar)) AttachArmor(OutDir + n + ".prefab", ar, h);
                    if (n == "Ch5_Boss") { PartPrefix = "BP_"; AttachArmor(OutDir + n + ".prefab", BossArmor5, h); PartPrefix = "EP_"; TuneBoss5(OutDir + n + ".prefab"); }
                }
                BuildPhantom();
                BuildMirrorCat();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[EnemyModelBuilder] 5장 적 프리팹");
            }
            finally { SrcDir = Ch1Src; PartsDir = Kit4Dir; PartPrefix = "KP_"; ModelPalette.Active = ModelPalette.Ch1; }
        }

        /// <summary>거울 허상 (블렌더, 뼈대 없음 — 게임이 코드로 띄우고 · 기울이고 · 고리를 돌림). 강화형 = 15% 크게 + 머리 · 등 강화 파츠</summary>
        static void BuildPhantom()
        {
            ModelPalette.Active = ModelPalette.Ch5;
            if (!File.Exists(Ch5Src + "Kit_Ch5_MirrorPhantom.fbx")) return;
            KeepOrigin.Add("Kit_Ch5_MirrorPhantom");     // 원점 = 몸 아래 바닥 (꼬리 끝은 0.35m 떠 있음)
            if (!Build("Kit_Ch5_MirrorPhantom", 2.1f, 180f, 'h', OutDir, "Ch5_Phantom")) return;
            Build("Kit_Ch5_MirrorPhantom", 2.45f, 180f, 'h', OutDir, "Ch5_Phantom_Plus");
            // 강화형 파츠: 뼈가 없으니 몸 높이 비율 자리에 붙임 (2.2m 인형 기준 파츠 → 키 비례)
            var root = PrefabUtility.LoadPrefabContents(OutDir + "Ch5_Phantom_Plus.prefab");
            try
            {
                float s = 2.45f / WeaponDesignHeight;
                void Put(string piece, Vector3 at)
                {
                    var pp = AssetDatabase.LoadAssetAtPath<GameObject>(Kit5Dir + "EP_" + piece + ".prefab");
                    if (!pp) return;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pp, root.transform);
                    go.name = "Armor_" + piece;
                    go.transform.localPosition = at * 2.45f;
                    go.transform.localRotation = Quaternion.Euler(0, 180, 0);   // 블렌더 +Y 앞 → 유니티 +Z 앞
                    go.transform.localScale = Vector3.one * s;
                }
                Put("HeadShards", new Vector3(0, 0.62f, 0));      // 후드 꼭대기 쪽 (키 비율)
                Put("BackRift", new Vector3(0, 0.45f, -0.06f));   // 등
                PrefabUtility.SaveAsPrefabAsset(root, OutDir + "Ch5_Phantom_Plus.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>중간 보스 거울 속의 고양이: 주인공 고양이 프리팹을 좌우 반전한 거울 복제 (은청 거울 텍스처 + 금 균열 + 마젠타 눈, 거울 리볼버)</summary>
        static void BuildMirrorCat()
        {
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(CatHeroBuilder.PrefabPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Ch5Src + "Ch5_MidBoss_basecolor.png");
            if (!hero || !tex) { Debug.LogWarning("[EnemyModelBuilder] 거울 고양이: CatHero 프리팹 또는 거울 텍스처 없음"); return; }
            var root = (GameObject)PrefabUtility.InstantiatePrefab(hero);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Ch5_MidBoss";
            try
            {
                var body = root.transform.Find("Body");
                if (body) body.localScale = Vector3.Scale(body.localScale, new Vector3(-1f, 1f, 1f));   // 좌우 반전
                // 2026-11-19 사용자: 고양이 재활용 + 셰이더 씌워서 → 전용 셰이더 CatTang/MirrorCat (툰 명암 + 크롬 반사 + 은빛 테두리 + 흐르는 반짝임 + 체력 따라 번지는 금빛 균열)
                string mp = Ch5Src + "Ch5_MidBoss_Mirror.mat";
                var sh = Shader.Find("CatTang/MirrorCat") ?? Shader.Find("CatTang/Toon");
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, mp); }
                if (m.shader != sh) m.shader = sh;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 4f);
                if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.8f);
                EnemyEdge(m);
                if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(0.85f, 0.92f, 1f));   // 거울빛 테두리
                EditorUtility.SetDirty(m);
                foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.sharedMaterial = m;
                if (!root.GetComponent<MirrorCatSkin>()) root.AddComponent<MirrorCatSkin>();   // 체력이 줄수록 _Crack ↑
                // 리볼버 → 거울 리볼버 (블렌더 Kit_W_MirrorPistol, 원점 = 손잡이 · +Z 총구). 고양이 총 축(+z 총구 · +y 윗면)에 맞춰 붙이고 원래 메시는 끔
                var pistol = AssetDatabase.LoadAssetAtPath<GameObject>(Kit5Dir + "W_MirrorPistol.prefab");
                foreach (var gn in new[] { "Gun_R", "Gun_L" })
                {
                    var g = root.transform.Find(gn);
                    if (!g || !pistol) continue;
                    var old = g.Find("Mesh"); if (old) old.gameObject.SetActive(false);
                    var p = (GameObject)PrefabUtility.InstantiatePrefab(pistol, g);
                    p.name = "MirrorPistol";
                    p.transform.localPosition = Vector3.zero;
                    p.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f) * Quaternion.identity;   // 블렌더 +Z(총구) → 유니티 +Z 앞 (FBX 축 변환 보정)
                    p.transform.localScale = Vector3.one * 1.3f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, OutDir + "Ch5_MidBoss.prefab");
                Debug.Log("[EnemyModelBuilder] " + OutDir + "Ch5_MidBoss.prefab (거울 고양이)");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
