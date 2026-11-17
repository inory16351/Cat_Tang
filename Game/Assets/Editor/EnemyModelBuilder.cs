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
        const string Ch1Src = "Assets/Art/Characters/Ch1/";
        static string SrcDir = Ch1Src;                       // 지금 빌드하는 챕터의 원본 폴더 (2장 빌드 중엔 Ch2)
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
            // 방 사이 나무 문 — 미리 쪼갠 조각(Piece_XX) + 가시 조각(Splinter_X), 클리어하면 WoodGate가 조각을 날림 (blender_kit_woodgate.py)
            ("Kit_WoodGate", 4.6f, 180f, 'w', KitDir, "WoodGate"),
            // 체력 회복 하트 (적 처치 드롭 · 상인, blender_kit_heart.py) — HeartPickup이 띄워 돌림
            ("Kit_Heart", 0.6f, 0f, 'w', KitDir, "Heart"),
            ("Kit_XpGem", 0.35f, 0f, 'h', KitDir, "XpGem"),          // 경험치 보석 (blender_kit_xpgem.py, XpOrb가 씀)
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
            // 2장: 전용 클립 (Ch2Motion — 근육 커브로 만든 코드 클립, 짐꾼은 PorterRig)
            { "Ch2_Conductor", ("ch2m_conductorflick", 0.9f, null, 0, false) },
            { "Ch2_Conductor_Plus", ("ch2m_conductorflick", 0.8f, null, 0, false) },
            // 짐꾼: 전용 클립 (PorterRig — 팔은 트렁크 든 자세 고정). Attack = 방패 돌진 · Special = 내리찍기 · Throw = 트렁크 던지기
            { "Ch2_Porter", ("ch2_porterbash", PorterRig.BashEnd, "ch2_porterslam", PorterRig.SlamEnd, false) },
            { "Ch2_Porter_Plus", ("ch2_porter_plusbash", PorterRig.BashEnd, "ch2_porter_plusslam", PorterRig.SlamEnd, false) },
            { "Ch2_Signal", ("ch2m_signaltoss", 0.6f, null, 0, false) },
            { "Ch2_Signal_Plus", ("ch2m_signaltoss", 0.6f, null, 0, false) },
            { "Ch2_MidBoss", ("ch2m_masterpunch", 1.0f, "ch2m_masterbaton", 1.0f, false) },
            { "Ch2_Boss", ("ch2m_giantbrace", 1.1f, "ch2m_giantrods", 0.9f, false) },
        };

        // ---------- 2장 (멈춘 기차역, 2026-11-17) ----------
        // 원본: Art/Characters/Ch2 (Tripo 캐릭터 Ch2_* · 블렌더 + Codex 키트 Kit_*), 배색표 ModelPalette.Ch2 (맵 = 따뜻한 녹 · 놋쇠 · 검댕, 보라 X)
        // 적 키는 1장 같은 역할의 비율을 따름: 차장 ≈ 무법자 · 짐꾼 ≈ 깡패 · 신호수 ≈ 폭탄병
        const string Ch2Src = "Assets/Art/Characters/Ch2/";
        const string Kit2Dir = "Assets/Resources/Kit/Ch2/";

        static readonly (string name, float size, float yaw, char fit, string outDir, string outName)[] Models2 =
        {
            ("Ch2_Conductor", 2.7f, 0f, 'h', OutDir, "Ch2_Conductor"),
            ("Ch2_Porter", 2.9f, 0f, 'h', OutDir, "Ch2_Porter"),
            ("Ch2_Signal", 1.9f, 0f, 'h', OutDir, "Ch2_Signal"),
            ("Ch2_Conductor_Plus", 3.0f, 0f, 'h', OutDir, "Ch2_Conductor_Plus"),
            ("Ch2_Porter_Plus", 3.2f, 0f, 'h', OutDir, "Ch2_Porter_Plus"),
            ("Ch2_Signal_Plus", 2.2f, 0f, 'h', OutDir, "Ch2_Signal_Plus"),
            ("Ch2_MidBoss", 4.0f, 0f, 'h', OutDir, "Ch2_MidBoss"),
            ("Ch2_Boss", 6.5f, 0f, 'h', OutDir, "Ch2_Boss"),
            // 역 벽면 (blender_kit_ch2_station.py) — 폭이 정확해야 줄이 맞음
            ("Kit_Facade_StationHall", 8f, 180f, 'w', Kit2Dir, "Facade_StationHall"),
            ("Kit_Facade_Waiting", 6f, 180f, 'w', Kit2Dir, "Facade_Waiting"),
            ("Kit_Facade_Warehouse", 7f, 180f, 'w', Kit2Dir, "Facade_Warehouse"),
            ("Kit_Facade_EngineShed", 9f, 180f, 'w', Kit2Dir, "Facade_EngineShed"),
            ("Kit_Facade_Canopy", 8f, 180f, 'w', Kit2Dir, "Facade_Canopy"),
            ("Kit_Facade_Ticket", 5f, 180f, 'w', Kit2Dir, "Facade_Ticket"),
            ("Kit_Wall", 4f, 180f, 'w', Kit2Dir, "Wall"),
            ("Kit_Cliff_Embankment", 6f, 180f, 'w', Kit2Dir, "Cliff"),
            // 소품 · 폭발물 (blender_kit_ch2_props.py)
            ("Kit_SteamValve", 1.6f, 180f, 'h', Kit2Dir, "SteamValve"),
            ("Kit_FloatLuggage", 0.9f, 180f, 'w', Kit2Dir, "FloatLuggage"),
            ("Kit_MineCart", 1.4f, 180f, 'w', Kit2Dir, "MineCart"),
            ("Kit_RailTrack", 2.6f, 180f, 'w', Kit2Dir, "RailTrack"),
            ("Kit_Bench", 2.2f, 180f, 'w', Kit2Dir, "Bench"),
            ("Kit_LuggagePile", 2.0f, 180f, 'w', Kit2Dir, "LuggagePile"),
            ("Kit_Crates", 1.8f, 180f, 'w', Kit2Dir, "Crates"),
            ("Kit_TicketBooth", 2.4f, 180f, 'w', Kit2Dir, "TicketBooth"),
            ("Kit_SignalLamp", 2.4f, 180f, 'h', Kit2Dir, "SignalLamp"),
            ("Kit_StationClock", 3.0f, 180f, 'h', Kit2Dir, "StationClock"),
            // 큰 지형지물 · 보스 바닥 (blender_kit_ch2_landmarks.py)
            ("Kit_LM_Coach", 10f, 180f, 'w', Kit2Dir, "LM_Coach"),
            ("Kit_LM_Locomotive", 9f, 180f, 'w', Kit2Dir, "LM_Locomotive"),
            ("Kit_LM_WaterCrane", 3f, 180f, 'w', Kit2Dir, "LM_WaterCrane"),
            ("Kit_LM_CoalPile", 4.5f, 180f, 'w', Kit2Dir, "LM_CoalPile"),
            ("Kit_LM_LuggageCart", 4f, 180f, 'w', Kit2Dir, "LM_LuggageCart"),
            ("Kit_LM_SignalGantry", 5f, 180f, 'w', Kit2Dir, "LM_SignalGantry"),
            ("Kit_Turntable", 24f, 0f, 'w', Kit2Dir, "Turntable"),
            // 보스 객차 (크롬 ChatGPT 원화 → Tripo H3.1, 2026-11-17): 앞(+Z) = 빨간 그릴 끝, 런타임 Ch2Boss가 길이 4.2m로 다시 맞춤
            ("Ch2_BossCoach", 3.6f, 0f, 'h', Kit2Dir, "BossCoach"),
            // 지나가는 열차 (blender_kit_ch2_train.py): 앞 = +Z, 기관차 9m · 객차 8m (ToyTrain이 이어 붙임)
            ("Kit_TrainEngine", 3.64f, 0f, 'h', Kit2Dir, "TrainEngine"),
            ("Kit_TrainCar", 3.2f, 0f, 'h', Kit2Dir, "TrainCar"),
            ("Kit_PlatformEdge", 8f, 180f, 'w', Kit2Dir, "PlatformEdge"),
            // 굳은 승객 석상 10종 (blender_kit_ch2_passengers.py, FrozenPassenger) — 받침대 포함 키, 조각 · 금 선 · 돌 조각 틀
            ("Kit_Passenger_Luggage", 2.5f, 0f, 'h', Kit2Dir, "Passenger_Luggage"),
            ("Kit_Passenger_Hat", 2.5f, 0f, 'h', Kit2Dir, "Passenger_Hat"),
            ("Kit_Passenger_Child", 1.7f, 0f, 'h', Kit2Dir, "Passenger_Child"),
            ("Kit_Passenger_Watch", 2.6f, 0f, 'h', Kit2Dir, "Passenger_Watch"),
            ("Kit_Passenger_Newspaper", 2.5f, 0f, 'h', Kit2Dir, "Passenger_Newspaper"),
            ("Kit_Passenger_Wave", 2.45f, 0f, 'h', Kit2Dir, "Passenger_Wave"),
            ("Kit_Passenger_OldLady", 2.1f, 0f, 'h', Kit2Dir, "Passenger_OldLady"),
            ("Kit_Passenger_MotherChild", 2.45f, 0f, 'h', Kit2Dir, "Passenger_MotherChild"),
            ("Kit_Passenger_Sitter", 1.83f, 0f, 'h', Kit2Dir, "Passenger_Sitter"),
            ("Kit_Passenger_Runner", 2.6f, 0f, 'h', Kit2Dir, "Passenger_Runner"),
        };

        [MenuItem("CatTang/2장 굳은 승객만 다시 만들기")]
        public static void BuildCh2Passengers() => BuildCh2Only(CatTang.FrozenPassenger.Poses);

        /// <summary>굳은 승객 석상: 몸은 툰 돌(배색표 statue · statue_dark · statue_base), 금 선만 빛나는 흰색(CatTang/Silhouette 불투명, 그림자 없음)</summary>
        static void PassengerMats(GameObject inst)
        {
            string p = Ch2Src + "Passenger_Crack.mat";
            var crack = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!crack) { crack = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(crack, p); }
            crack.SetColor("_Color", new Color(0.92f, 0.97f, 1f, 1f));
            crack.SetFloat("_RimBoost", 0.3f);
            EditorUtility.SetDirty(crack);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Crack"))
                {
                    r.sharedMaterial = crack;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
        }

        [MenuItem("CatTang/2장 프리팹 만들기 (Tripo + 블렌더)")]
        public static void BuildCh2() => BuildCh2Only(null);

        [MenuItem("CatTang/2장 짐꾼만 다시 만들기")]
        public static void BuildCh2Porters() => BuildCh2Only("Ch2_Porter", "Ch2_Porter_Plus");

        /// <summary>2장 표 중 이름이 든 것만 (null이면 전부)</summary>
        public static void BuildCh2Only(params string[] only)
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(Kit2Dir);
            Ch2Motion.MakeAll();                                         // 전용 클립을 먼저 (컨트롤러가 찾음)
            SrcDir = Ch2Src;
            ModelPalette.Active = ModelPalette.Ch2;
            bool Want(string n) => only == null || System.Array.IndexOf(only, n) >= 0;
            try
            {
                int made = 0;
                foreach (var m in Models2)
                    if (Want(m.outName) && Build(m.name, m.size, m.yaw, m.fit, m.outDir, m.outName)) made++;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                foreach (var m in Models2)
                {
                    if (!Want(m.outName)) continue;
                    string pp = m.outDir + m.outName + ".prefab";
                    var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SrcDir + m.outName + ".controller");
                    if (!ctrl || !Anims.ContainsKey(m.outName) || !AssetDatabase.LoadAssetAtPath<GameObject>(pp)) continue;
                    var contents = PrefabUtility.LoadPrefabContents(pp);
                    var an = contents.GetComponentInChildren<Animator>();
                    if (an) { an.runtimeAnimatorController = ctrl; PrefabUtility.SaveAsPrefabAsset(contents, pp); }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                AssetDatabase.SaveAssets();
                // 1장 빌더와 같이: 컨트롤러 · 프리팹을 강제로 다시 가져와야 실행 중 컨트롤러가 안 비어 보임
                foreach (var m in Models2)
                {
                    if (!Want(m.outName) || !Anims.ContainsKey(m.outName)) continue;
                    AssetDatabase.ImportAsset(SrcDir + m.outName + ".controller", ImportAssetOptions.ForceUpdate);
                    AssetDatabase.ImportAsset(m.outDir + m.outName + ".prefab", ImportAssetOptions.ForceUpdate);
                }
                Debug.Log($"[EnemyModelBuilder] 2장 프리팹 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

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

        /// <summary>1장 표 중 이름(원본 이름)이 든 것만 빌드 (전체 다시 만들기 없이 키트 하나 추가할 때)</summary>
        public static int BuildCh1Only(params string[] names)
        {
            int made = 0;
            foreach (var m in Models)
                if (System.Array.IndexOf(names, m.name) >= 0 && Build(m.name, m.size, m.yaw, m.fit, m.outDir, m.outName)) made++;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return made;
        }

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
                // 짐꾼: 트렁크 든 팔 자세로 전용 클립 + 트렁크를 가슴 뼈에 고정 (PorterRig)
                if (PorterRig.IsPorter(outName)) { PorterRig.MakeClips(src, avatar, outName, SrcDir); PorterRig.SplitTrunk(inst, outName, SrcDir); }
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

            if (PorterRig.IsPorter(outName)) PorterRig.AddFootGround(inst);

            // 굳은 승객 석상: 빛나는 금 선 재질 + 깨어남 컴포넌트
            if (name.StartsWith("Kit_Passenger")) { PassengerMats(inst); root.AddComponent<CatTang.FrozenPassenger>(); }

            // 폭발물: 주황 ↔ 노랑 맥동 바깥 테두리 (2026-11-17 사용자: 몬스터처럼 테두리로 터진다는 걸 알려 줌)
            if (Explosives.Contains(name)) root.AddComponent<CatTang.DangerEdge>();

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
            var dirs = new[] { SrcDir.TrimEnd('/'), Ch1Src.TrimEnd('/'), "Assets/Art/Characters/CatHero" };
            foreach (var guid in AssetDatabase.FindAssets("t:Model", dirs))
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview") && c.name.ToLower().Contains(key) && c.isHumanMotion) return c;
            // 에디터에서 만든 .anim (MakeJumpClip 등)
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { SrcDir.TrimEnd('/'), Ch1Src.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.EndsWith(".anim")) continue;
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (c && c.name.ToLower().Contains(key)) return c;
            }
            return null;
        }

        // ---------- 점프 클립 (휴머노이드 근육 커브로 직접 만듦) ----------
        const string JumpPath = Ch1Src + "Anim/Ch1_BomberJump.anim";

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
            Directory.CreateDirectory(Ch1Src + "Anim");
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
        const string StompPath = Ch1Src + "Anim/Ch1_BruteStomp.anim";

        [MenuItem("CatTang/깡패 발 구르기 클립 만들기")]
        public static AnimationClip MakeStompClip()
        {
            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") idle = c;
            if (!idle) return null;
            Directory.CreateDirectory(Ch1Src + "Anim");
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
        const string ThrowPath = Ch1Src + "Anim/Ch1_BomberThrow.anim";
        public const float ThrowEnd = 0.6f;

        [MenuItem("CatTang/폭탄병 던지기 클립 만들기")]
        public static AnimationClip MakeThrowClip()
        {
            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                if (o is AnimationClip c && c.name == "idle") idle = c;
            if (!idle) return null;
            Directory.CreateDirectory(Ch1Src + "Anim");
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
            bool porter = PorterRig.IsPorter(outName);
            string pk = outName.ToLower();
            var idle = sm.AddState("Idle"); idle.motion = porter ? FindClip(pk + "idle") : FindClip("idle");
            var run = sm.AddState("Run"); run.motion = porter ? FindClip(pk + "walk") : FindClip(Ch2Motion.WalkFor(outName) ?? "run");
            sm.defaultState = idle;
            var t = idle.AddTransition(run); t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.12f;
            t = run.AddTransition(idle); t.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.18f;
            AddAction(sm, idle, "Attack", FindClip(spec.atk), spec.atkDur, spec.atkLoop);
            AddAction(sm, idle, "Special", FindClip(spec.sp), spec.spDur, false);
            // 폭탄병 모래 폭탄 투척 (2026-11-17: 떨기 클립으로 던져서 모션 없이 터지는 것처럼 보였음)
            if (outName.Contains("Bomber")) AddAction(sm, idle, "Throw", FindClip("bomberthrow"), ThrowEnd, false);
            if (porter) AddAction(sm, idle, "Throw", FindClip(pk + "throw"), PorterRig.ThrowEnd, false);
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

        // 적 테두리 (2026-11-17 사용자): 빨간 바탕선, 채도 높게, 단 형체 바깥에만 (안쪽 겹친 곳까지 빨개서 눈 아픔)
        // → 안쪽 선은 예전처럼 어두운 잉크, 빨강은 스텐실로 실루엣 바깥에만 (셰이더 OuterOutline 패스 + 렌더러 기능 CatTangOuterOutline)
        public static readonly Color EnemyOutline = new(0.78f, 0.07f, 0.12f);
        public const float EnemyOuterWidth = 3.5f;
        public const int EnemyStencil = 4;
        public static void EnemyEdge(Material m)
        {
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.08f, 0.05f, 0.08f));
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(1f, 0.78f, 0.35f));
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            if (m.HasProperty("_OuterOutlineColor"))
            {
                m.SetColor("_OuterOutlineColor", EnemyOutline);
                m.SetFloat("_OuterOutlineWidth", EnemyOuterWidth);
                m.SetInt("_OuterStencilRef", EnemyStencil);
            }
            EditorUtility.SetDirty(m);
        }

        [MenuItem("CatTang/적 테두리 색 적용 (살짝 붉은 외곽선)")]
        public static void ApplyEnemyEdge()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("Ch1_ t:Material", new[] { SrcDir.TrimEnd('/') }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m && m.name.StartsWith("Ch1_") && m.shader && m.shader.name == "CatTang/Toon") { EnemyEdge(m); n++; }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyModelBuilder] 적 테두리 {n}개 재질");
        }

        // 배경 톤을 낮추지 않는 것: 픽업 · 폭발물 (폭발물은 DangerEdge 주황 테두리가 붙음)
        static readonly HashSet<string> Pickups = new() { "Kit_Heart", "Kit_XpGem", "Kit_SandBomb", "Kit_Keg", "Kit_SteamValve", "Kit_FloatLuggage", "Kit_MineCart" };
        // 바닥에 깔리는 납작한 조각 (전차대 · 선로 · 승강장 가장자리)
        static readonly HashSet<string> FlatFloor = new() { "Kit_Turntable", "Kit_RailTrack", "Kit_PlatformEdge" };
        // 쏘면 터지는 물체 (1장 화약통 · 2장 증기 밸브 · 공중에 멈춘 짐 · 광차) → 프리팹에 DangerEdge
        static readonly HashSet<string> Explosives = new() { "Kit_Keg", "Kit_SteamValve", "Kit_FloatLuggage", "Kit_MineCart" };

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
            // 회색으로 뽑은 블렌더 모델: 재질 이름으로 배색표(ModelPalette) 색을 입힘 (Art/README.md)
            if (!tex && ModelPalette.TryResolve(src, flat, out var pal)) flat = pal;
            m.SetColor("_BaseColor", tex ? Color.white : flat);
            // 1장 팔레트: 황혼 보라 그림자 + 금빛 테두리
            m.SetColor("_ShadeColor", new Color(0.72f, 0.58f, 0.66f));
            m.SetColor("_SecondShadeColor", new Color(0.45f, 0.34f, 0.46f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", FlatFloor.Contains(name) ? 1f : 4f);   // 2026-11-16 사용자: 외곽선 두껍게 (납작한 바닥 조각은 판자 선이 지글거려 얇게)
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            // 배경 키트는 채도 · 밝기를 낮춤 (EnvTone), 캐릭터 · 하트 · 폭탄 · 화약통은 원래 색
            bool env = name.StartsWith("Kit_") && !Pickups.Contains(name);
            if (m.HasProperty("_Saturation")) { m.SetFloat("_Saturation", env ? EnvTone.Saturation : 1f); m.SetFloat("_Brightness", env ? EnvTone.Brightness : 1f); }
            // 적: 배경과 갈리게 살짝 진홍 외곽선 + 붉은 가장자리 빛 (2026-11-17 사용자: 적에게 살짝 배경선)
            if (name.StartsWith("Ch1_") || name.StartsWith("Ch2_")) { EnemyEdge(m); EnemyPalette.ApplyCurrent(m); }   // 적 배색 2안 (차가운 한 계열)
            if ((name == "Kit_Heart" || name == "Kit_XpGem") && m.HasProperty("_Saturation")) { m.SetFloat("_Saturation", 1.15f); m.SetFloat("_Brightness", 1.12f); }   // 하트 · 경험치 보석은 더 선명하게
            if (name == "Kit_XpGem" && m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 2.5f);   // 작은 물체라 외곽선을 얇게
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
