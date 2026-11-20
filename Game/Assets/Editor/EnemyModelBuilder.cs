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
    public static partial class EnemyModelBuilder
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
            // 스테이지별 문 · 복도 · 문틀 + 보상 문 (2026-11-19, blender_kit_ch1_gates.py)
            ("Kit_Gate_S1", 4.6f, 180f, 'w', KitDir, "Gate_S1"),
            ("Kit_Corridor_S1", 10.4f, 180f, 'd', KitDir, "Corridor_S1"),
            ("Kit_Portal_S1", 6.8f, 180f, 'w', KitDir, "Portal_S1"),
            ("Kit_Gate_S2", 4.6f, 180f, 'w', KitDir, "Gate_S2"),
            ("Kit_Corridor_S2", 10.4f, 180f, 'd', KitDir, "Corridor_S2"),
            ("Kit_Portal_S2", 6.8f, 180f, 'w', KitDir, "Portal_S2"),
            ("Kit_Gate_S3", 4.6f, 180f, 'w', KitDir, "Gate_S3"),
            ("Kit_Corridor_S3", 10.4f, 180f, 'd', KitDir, "Corridor_S3"),
            ("Kit_Portal_S3", 6.8f, 180f, 'w', KitDir, "Portal_S3"),
            ("Kit_Gate_S4", 4.6f, 180f, 'w', KitDir, "Gate_S4"),
            ("Kit_Corridor_S4", 10.4f, 180f, 'd', KitDir, "Corridor_S4"),
            ("Kit_Portal_S4", 6.8f, 180f, 'w', KitDir, "Portal_S4"),
            ("Kit_RewardDoor", 3.0f, 180f, 'w', KitDir, "RewardDoor"),
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
            // Special = 내리찍기 전용 클립 (EnemyMotions — 예전엔 Tripo '휘두르기'를 돌려 씀), 회전 휘두르기는 Moves의 Spin
            { "Ch1_Brute", ("brutestomp", 0.6f, "ch1m_bruteslam", 1.15f, false) },
            { "Ch1_Brute_Plus", ("brutestomp", 0.6f, "ch1m_bruteslam", 1.15f, false) },
            // 폭탄병 Special = 점프(웅크림 0.35 → 체공 0.55 → 착지). 클립은 MakeJumpClip이 근육 커브로 만듦 (Enemy.BomberCrouch · BomberAir와 맞춤)
            { "Ch1_Bomber", ("shakes", 0.8f, "bomberjump", 1.0f, true) },
            { "Ch1_Bomber_Plus", ("shakes", 0.7f, "bomberjump", 1.0f, true) },
            { "Ch1_MidBoss", ("shotgun", 1.0f, "revolver", 1.2f, false) },
            { "Ch1_Boss", ("swings", 1.1f, "swings", 0.9f, false) },
            // 3장 · 4장 (2026-11-19): 전용 모션 (EnemyMotions — 1장 Tripo 클립 돌려쓰기를 바꿈). 스킬이 여럿이면 아래 Moves에 상태를 더 둠
            { "Ch3_Shooter", ("ch3m_shootermirror", 0.9f, null, 0, false) },
            { "Ch3_Mage", ("ch3m_mageconjure", 1.5f, null, 0, false) },
            { "Ch3_Blade", ("ch3m_bladeslash", 1.45f, null, 0, false) },
            { "Ch3_Knight", ("ch3m_knightbash", 1.2f, null, 0, false) },
            { "Ch3_Trickster", ("ch3m_trickflick", 0.6f, null, 0, false) },
            { "Ch4_Soldier", ("ch4m_soldiershot", 0.8f, null, 0, false) },
            { "Ch4_Ringer", ("ch4m_ringerring", 1.0f, null, 0, false) },
            { "Ch4_Dancer", ("ch4m_dancerspin", 0.6f, null, 0, true) },
            { "Ch4_Assassin", ("ch4m_assassinstab", 0.9f, null, 0, false) },
            { "Ch4_Knight", ("ch4m_knightswingr", 1.2f, null, 0, false) },
            // 4장 보스 (Hyper3D, 2026-11-20): 집행관 · 시침 = 진자 기사 모션, 분침 = 초침 자객 모션을 빌려 씀
            { "Ch4_Executioner", ("ch4m_knightswingr", 1.2f, null, 0, false) },
            { "Ch4_HourHand", ("ch4m_knightswingr", 1.2f, null, 0, false) },
            { "Ch4_MinuteHand", ("ch4m_assassinstab", 0.9f, null, 0, false) },
            { "Ch4_Repairman", ("ch4m_repairtoss", 0.65f, null, 0, false) },
            // 5장 (2026-11-19 사용자: 앞 장 모션 재활용 말고 다른 느낌으로) → 전용 클립 (EnemyMotions.Ch5, 글리치 · 둥실). 보스는 아직 행동이 없어 기본 공격만
            { "Ch5_Gunner", ("ch5m_gunnershot", 0.85f, null, 0, false) },
            { "Ch5_Witch", ("ch5m_witchcast", 0.95f, "ch5m_witchlink", 0.65f, false) },
            { "Ch5_Giant", ("ch5m_giantslam", 1.45f, "ch5m_giantdash", 1.5f, false) },
            { "Ch5_Assassin", ("ch5m_assassinslash", 0.9f, null, 0, false) },
            { "Ch5_Caster", ("ch5m_casterlob", 0.55f, "ch5m_casteropen", 0.6f, false) },
            { "Ch5_Boss", ("ch5m_bossreap", 1.4f, null, 0, false) },
            { "Ch5_LordOfTime", ("ch5m_bossreap", 1.4f, null, 0, false) },
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

        // 스킬별 전용 상태 (트리거 없이 Enemy.Anim("상태 이름") → Animator.CrossFade): (상태, 클립 키워드, 재생 시간, 반복)
        // 반복 상태는 Busy가 꺼질 때 Idle로, 아니면 다 재생 후 Idle로. 클립 = EnemyMotions · Ch2Motion (2026-11-19 사용자: 스킬마다 다른 모션, 단조롭지 않게)
        static readonly Dictionary<string, (string state, string clip, float dur, bool loop)[]> Moves = new()
        {
            { "Ch1_Outlaw", new[] { ("Burst", "ch1m_outlawfan", 1.05f, false), ("Dash", "ch1m_outlawsidedash", 0.4f, false) } },
            { "Ch1_Outlaw_Plus", new[] { ("Burst", "ch1m_outlawfan", 1.05f, false), ("Dash", "ch1m_outlawsidedash", 0.4f, false) } },
            { "Ch1_Brute", new[] { ("Spin", "ch1m_brutespin", 1.0f, false) } },
            { "Ch1_Brute_Plus", new[] { ("Spin", "ch1m_brutespin", 1.0f, false) } },
            { "Ch1_MidBoss", new[] { ("SpinFire", "ch1m_hunterspinfire", 2.4f, false), ("Snipe", "ch1m_huntersnipe", 1.9f, false), ("Tackle", "ch1m_huntertackle", 1.6f, false), ("Summon", "ch1m_hunterwhistle", 0.9f, false) } },
            { "Ch1_Boss", new[] { ("Puddles", "ch1m_giantdrip", 1.2f, false), ("Numbers", "ch1m_giantcast", 1.7f, false), ("Rewind", "ch1m_giantrewind", 1.8f, false), ("SlowZone", "ch1m_giantpress", 0.9f, false) } },
            { "Ch2_MidBoss", new[] { ("Schedule", "ch2m_masterschedule", 2.9f, false), ("Whistle", "ch2m_masterwhistle", 1.3f, false), ("Call", "ch2m_mastercall", 0.9f, false) } },
            { "Ch2_Boss", new[] { ("Vent", "ch2m_giantvent", 1.6f, false), ("Heave", "ch2m_giantheave", 1.5f, false), ("CoachSpin", "ch2m_giantcoachspin", 1.4f, false) } },
            { "Ch3_Mage", new[] { ("FlingR", "ch3m_mageflingr", 0.4f, false), ("FlingL", "ch3m_mageflingl", 0.4f, false) } },
            { "Ch3_Blade", new[] { ("Dive", "ch3m_bladedive", 0.45f, false), ("Emerge", "ch3m_bladeemerge", 1.4f, false) } },
            { "Ch3_Knight", new[] { ("Stagger", "ch3m_knightstagger", 0.7f, false) } },
            { "Ch3_Trickster", new[] { ("Bow", "ch3m_trickbow", 0.9f, false) } },
            { "Ch4_Soldier", new[] { ("Shot", "ch4m_soldiershot", 0.8f, false), ("Drum", "ch4m_soldierdrum", 1.25f, false), ("Volley", "ch4m_soldiervolley", 1.25f, false), ("Bayonet", "ch4m_soldierbayonet", 1.45f, false) } },
            { "Ch4_Ringer", new[] { ("Ring", "ch4m_ringerring", 1.0f, false), ("RingDouble", "ch4m_ringerdouble", 1.6f, false) } },
            { "Ch4_Dancer", new[] { ("Rise", "ch4m_dancerrise", 0.4f, false), ("Spin", "ch4m_dancerspin", 0.6f, true), ("Finale", "ch4m_dancerfinale", 0.7f, false) } },
            { "Ch4_Assassin", new[] { ("Blink", "ch4m_assassinblink", 0.35f, false), ("Stab", "ch4m_assassinstab", 0.9f, false), ("Breath", "ch4m_assassinbreath", 1.0f, false), ("BackStab", "ch4m_assassinbackstab", 1.0f, false) } },
            { "Ch4_Knight", new[] { ("SwingL", "ch4m_knightswingl", 1.2f, false), ("SwingR", "ch4m_knightswingr", 1.2f, false), ("Whirl", "ch4m_knightwhirl", 1.4f, false) } },
            // 4장 보스 (Hyper3D, 2026-11-20): 집행관 · 시침 = 진자 기사 모션, 분침 = 초침 자객 모션을 빌려 씀
            { "Ch4_Executioner", new[] { ("SwingL", "ch4m_knightswingl", 1.2f, false), ("SwingR", "ch4m_knightswingr", 1.2f, false), ("Whirl", "ch4m_knightwhirl", 1.4f, false) } },
            { "Ch4_HourHand", new[] { ("SwingL", "ch4m_knightswingl", 1.2f, false), ("SwingR", "ch4m_knightswingr", 1.2f, false), ("Whirl", "ch4m_knightwhirl", 1.4f, false) } },
            { "Ch4_MinuteHand", new[] { ("Blink", "ch4m_assassinblink", 0.35f, false), ("Stab", "ch4m_assassinstab", 0.9f, false), ("Breath", "ch4m_assassinbreath", 1.0f, false), ("BackStab", "ch4m_assassinbackstab", 1.0f, false) } },
            { "Ch4_Repairman", new[] { ("Wind", "ch4m_repairwind", 0.95f, false), ("Toss", "ch4m_repairtoss", 0.65f, false) } },
            { "Ch5_Gunner", new[] { ("Shot", "ch5m_gunnershot", 0.85f, false) } },
            { "Ch5_Witch", new[] { ("Cast", "ch5m_witchcast", 0.95f, false), ("Link", "ch5m_witchlink", 0.65f, false) } },
            { "Ch5_Giant", new[] { ("Slam", "ch5m_giantslam", 1.45f, false), ("Dash", "ch5m_giantdash", 1.5f, false) } },
            { "Ch5_Assassin", new[] { ("Dive", "ch5m_assassindive", 0.45f, false), ("Emerge", "ch5m_assassinemerge", 1.4f, false), ("Slash", "ch5m_assassinslash", 0.9f, false), ("Breath", "ch5m_assassinbreath", 1.0f, false) } },
            { "Ch5_Caster", new[] { ("Open", "ch5m_casteropen", 0.6f, false), ("Lob", "ch5m_casterlob", 0.55f, false) } },
            { "Ch5_Boss", new[] { ("ReapWind", "ch5m_bossreapwind", 1.1f, false), ("ReapDash", "ch5m_bossreapdash", 1.05f, false), ("Reap", "ch5m_bossreap", 1.4f, false), ("Cast", "ch5m_bosscast", 1.1f, false), ("Spear", "ch5m_bossspear", 1.2f, false) } },
            { "Ch5_LordOfTime", new[] { ("ReapWind", "ch5m_bossreapwind", 1.1f, false), ("ReapDash", "ch5m_bossreapdash", 1.05f, false), ("Reap", "ch5m_bossreap", 1.4f, false), ("Cast", "ch5m_bosscast", 1.1f, false), ("Spear", "ch5m_bossspear", 1.2f, false) } },
        };
        // 달리기 대신 쓸 전용 걷기 (없으면 고양이 run)
        static readonly Dictionary<string, string> Walks = new()
        {
            { "Ch4_Soldier", "ch4m_soldiermarch" },      // 받들어총 행군 (고양이 run + 오른팔 고정)
            { "Ch4_Ringer", "ch4m_ringerrun" },
            { "Ch4_Assassin", "ch4m_assassinrun" },
            { "Ch4_Knight", "ch4m_knightwalk" },
            // 4장 보스 (Hyper3D, 2026-11-20): 집행관 · 시침 = 진자 기사 모션, 분침 = 초침 자객 모션을 빌려 씀
            { "Ch4_Executioner", "ch4m_knightwalk" },
            { "Ch4_HourHand", "ch4m_knightwalk" },
            { "Ch4_MinuteHand", "ch4m_assassinrun" },
            { "Ch4_Repairman", "ch4m_repairscurry" },
            { "Ch5_Gunner", "ch5m_gunnerwalk" },
            { "Ch5_Witch", "ch5m_witchwalk" },
            { "Ch5_Giant", "ch5m_giantwalk" },
            { "Ch5_Assassin", "ch5m_assassinrun" },
            { "Ch5_Caster", "ch5m_casterwalk" },
            { "Ch5_Boss", "ch5m_bosswalk" },
            { "Ch5_LordOfTime", "ch5m_bosswalk" },
        };

        // 고양이 idle 대신 쓸 전용 idle (무기 든 팔을 고정)
        static readonly Dictionary<string, string> Idles = new()
        {
            { "Ch4_Soldier", "ch4m_soldieridle" },
            { "Ch4_Ringer", "ch4m_ringeridle" },
            { "Ch4_Assassin", "ch4m_assassinidle" },
            { "Ch4_Knight", "ch4m_knightidle" },
            // 4장 보스 (Hyper3D, 2026-11-20): 집행관 · 시침 = 진자 기사 모션, 분침 = 초침 자객 모션을 빌려 씀
            { "Ch4_Executioner", "ch4m_knightidle" },
            { "Ch4_HourHand", "ch4m_knightidle" },
            { "Ch4_MinuteHand", "ch4m_assassinidle" },
            { "Ch4_Repairman", "ch4m_repairidle" },
            { "Ch5_Gunner", "ch5m_gunneridle" },
            { "Ch5_Witch", "ch5m_witchidle" },
            { "Ch5_Giant", "ch5m_giantidle" },
            { "Ch5_Assassin", "ch5m_assassinidle" },
            { "Ch5_Caster", "ch5m_casteridle" },
            { "Ch5_Boss", "ch5m_bossidle" },
            { "Ch5_LordOfTime", "ch5m_bossidle" },
        };

        /// <summary>4장 강화형(Ch4_*_Plus)은 기본 적의 무기 · 손잡이 각도 · 스킬 모션 · idle · 걸음을 그대로 씀</summary>
        static string Base4(string n) => n != null && (n.StartsWith("Ch4_") || n.StartsWith("Ch5_")) && n.EndsWith("_Plus") ? n.Substring(0, n.Length - 5) : n;   // 5장 강화형도 같은 규칙 (2026-11-19)

        [MenuItem("CatTang/적 전용 모션 다시 만들기 (클립 + 컨트롤러)")]
        public static void RebuildMotions()
        {
            Ch2Motion.MakeAll();
            EnemyMotions.MakeAll();
            MakeJumpClip(); MakeStompClip(); MakeThrowClip();                 // 1장 코드 클립도 앞뒤 방향 바로잡아 다시
            AssetDatabase.Refresh();
            var touched = new List<string>();
            try
            {
                foreach (var kv in Anims)
                {
                    string n = kv.Key;
                    if (PorterRig.IsPorter(n)) continue;                       // 짐꾼은 PorterRig 전용 (원본 FBX에서 클립을 굽기 때문에 따로)
                    SrcDir = n.StartsWith("Ch2_") ? Ch2Src : n.StartsWith("Ch3_") ? Ch3Src : n.StartsWith("Ch4_") ? Ch4Src : n.StartsWith("Ch5_") ? Ch5Src : Ch1Src;
                    string cp = SrcDir + n + ".controller";
                    if (!AssetDatabase.LoadAssetAtPath<AnimatorController>(cp)) continue;   // 프리팹이 아직 없는 적
                    MakeController(n, kv.Value);                                // 같은 자산을 비우고 다시 채움 → 프리팹 참조 그대로
                    touched.Add(cp);
                    // 4장 강화형 컨트롤러 (기본과 같은 상태)
                    if ((n.StartsWith("Ch4_") || n.StartsWith("Ch5_")) && AssetDatabase.LoadAssetAtPath<AnimatorController>(SrcDir + n + "_Plus.controller")) { MakeController(n + "_Plus", kv.Value); touched.Add(SrcDir + n + "_Plus.controller"); }
                }
            }
            finally { SrcDir = Ch1Src; }
            AssetDatabase.SaveAssets();
            foreach (var cp in touched) AssetDatabase.ImportAsset(cp, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[EnemyModelBuilder] 적 컨트롤러 {touched.Count}개 다시 만듦");
        }

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
            // 스테이지별 문 · 복도 · 문틀 + 보상 문 (2026-11-19, blender_kit_ch2_gates.py)
            ("Kit_Gate_S1", 4.6f, 180f, 'w', Kit2Dir, "Gate_S1"),
            ("Kit_Corridor_S1", 10.4f, 180f, 'd', Kit2Dir, "Corridor_S1"),
            ("Kit_Portal_S1", 6.8f, 180f, 'w', Kit2Dir, "Portal_S1"),
            ("Kit_Gate_S2", 4.6f, 180f, 'w', Kit2Dir, "Gate_S2"),
            ("Kit_Corridor_S2", 10.4f, 180f, 'd', Kit2Dir, "Corridor_S2"),
            ("Kit_Portal_S2", 6.8f, 180f, 'w', Kit2Dir, "Portal_S2"),
            ("Kit_Gate_S3", 4.6f, 180f, 'w', Kit2Dir, "Gate_S3"),
            ("Kit_Corridor_S3", 10.4f, 180f, 'd', Kit2Dir, "Corridor_S3"),
            ("Kit_Portal_S3", 6.8f, 180f, 'w', Kit2Dir, "Portal_S3"),
            ("Kit_Gate_S4", 4.6f, 180f, 'w', Kit2Dir, "Gate_S4"),
            ("Kit_Corridor_S4", 10.4f, 180f, 'd', Kit2Dir, "Corridor_S4"),
            ("Kit_Portal_S4", 6.8f, 180f, 'w', Kit2Dir, "Portal_S4"),
            ("Kit_RewardDoor", 3.0f, 180f, 'w', Kit2Dir, "RewardDoor"),
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
            ("Kit_Arena", 32f, 180f, 'w', Kit2Dir, "Arena"),               // 2026-11-18: 보스 경기장 거대한 시계 지름 32m (blender_kit_ch2_arena.py), 12시가 먼 쪽(북)
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

        // ---------- 3장 (신기루 사막 = 거울 세계, 2026-11-18) ----------
        // 원본: Art/Characters/Ch3 (ChatGPT 원화 → Tripo H3.1 + 리깅), 키는 1 · 2장 같은 역할 비율 (사수 ≈ 무법자, 기사 ≈ 짐꾼)
        const string Ch3Src = "Assets/Art/Characters/Ch3/";
        static readonly (string name, float size)[] Models3 =
        {
            ("Ch3_Shooter", 2.6f), ("Ch3_Mage", 2.5f), ("Ch3_Blade", 2.7f), ("Ch3_Knight", 2.95f), ("Ch3_Trickster", 2.5f),
        };

        [MenuItem("CatTang/3장 적 프리팹 만들기 (Tripo)")]
        public static void BuildCh3()
        {
            Directory.CreateDirectory(OutDir);
            SrcDir = Ch3Src;
            try
            {
                foreach (var (n, h) in Models3)
                {
                    var imp = AssetImporter.GetAtPath(Ch3Src + n + ".fbx") as ModelImporter;
                    if (!imp) { Debug.Log($"[EnemyModelBuilder] {n} 원본 없음 — 건너뜀"); continue; }
                    if (imp.animationType != ModelImporterAnimationType.Human || imp.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        imp.animationType = ModelImporterAnimationType.Human;
                        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        imp.materialImportMode = ModelImporterMaterialImportMode.None;
                        imp.SaveAndReimport();
                    }
                    // 자동 매핑이 깨지면(손가락 뼈 겹침) 같은 mixamo 뼈대인 1장 상인 매핑을 빌림
                    var srcGo = FindSource(n);
                    if (srcGo && !HumanAvatar(srcGo) && AssetImporter.GetAtPath(MerchantSrc + "Ch1_Merchant.fbx") is ModelImporter refImp)
                    {
                        var names = new HashSet<string>();
                        foreach (var t in srcGo.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
                        var hd = imp.humanDescription;
                        hd.human = System.Array.FindAll(refImp.humanDescription.human, hb => names.Contains(hb.boneName));
                        imp.humanDescription = hd;
                        imp.SaveAndReimport();
                    }
                    Build(n, h, 0f, 'h', OutDir, n);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                foreach (var (n, _) in Models3)
                {
                    AssetDatabase.ImportAsset(Ch3Src + n + ".controller", ImportAssetOptions.ForceUpdate);
                    AssetDatabase.ImportAsset(OutDir + n + ".prefab", ImportAssetOptions.ForceUpdate);
                }
                Debug.Log("[EnemyModelBuilder] 3장 적 프리팹");
            }
            finally { SrcDir = Ch1Src; }
        }

        // 3장 맵 키트 (블렌더 + Codex, 회색 + 역할 재질 → ModelPalette.Ch3). 명세 Art/tools/specs/kit_ch3_{walls,props,landmarks}_spec.md + _ch3_common.md
        const string Kit3Dir = "Assets/Resources/Kit/Ch3/";
        static readonly (string name, float size, char fit, string outName)[] Kit3 =
        {
            // 벽면 (blender_kit_ch3_walls.py) — 폭이 정확해야 줄이 맞음
            ("Kit_Cliff_Shore", 6f, 'w', "Cliff"),
            ("Kit_Wall_Colonnade", 8f, 'w', "Facade_Colonnade"),
            ("Kit_Wall_Arch", 6f, 'w', "Facade_Arch"),
            ("Kit_Wall_MirrorFrame", 4f, 'w', "Facade_MirrorFrame"),
            ("Kit_Wall", 4f, 'w', "Wall"),
            // 스테이지별 문 · 복도 · 문틀 + 보상 문 (2026-11-19, blender_kit_ch3_gates.py)
            ("Kit_Gate_S1", 4.6f, 'w', "Gate_S1"),
            ("Kit_Corridor_S1", 10.4f, 'd', "Corridor_S1"),
            ("Kit_Portal_S1", 6.8f, 'w', "Portal_S1"),
            ("Kit_Gate_S2", 4.6f, 'w', "Gate_S2"),
            ("Kit_Corridor_S2", 10.4f, 'd', "Corridor_S2"),
            ("Kit_Portal_S2", 6.8f, 'w', "Portal_S2"),
            ("Kit_Gate_S3", 4.6f, 'w', "Gate_S3"),
            ("Kit_Corridor_S3", 10.4f, 'd', "Corridor_S3"),
            ("Kit_Portal_S3", 6.8f, 'w', "Portal_S3"),
            ("Kit_Gate_S4", 4.6f, 'w', "Gate_S4"),
            ("Kit_Corridor_S4", 10.4f, 'd', "Corridor_S4"),
            ("Kit_Portal_S4", 6.8f, 'w', "Portal_S4"),
            ("Kit_RewardDoor", 3.0f, 'w', "RewardDoor"),
            ("Kit_Cliff_Island", 6f, 'w', "Facade_Island"),
            ("Kit_Wall_Cloud", 6f, 'w', "Facade_Cloud"),
            // 소품 · 폭발물 (blender_kit_ch3_props.py)
            ("Kit_MemoryCrystal", 1.7f, 'h', "MemoryCrystal"),
            ("Kit_Fountain", 2.2f, 'w', "Fountain"),
            ("Kit_StandMirror", 2.6f, 'h', "StandMirror"),
            ("Kit_MerchantMirror", 3.6f, 'h', "MerchantMirror"),
            ("Kit_SandRock", 1.8f, 'w', "SandRock"),
            ("Kit_FallenPillar", 2.6f, 'w', "FallenPillar"),
            ("Kit_MirrorShard", 2.5f, 'w', "MirrorShard"),
            ("Kit_Pebbles3", 1.2f, 'w', "Pebbles3"),
            ("Kit_CrystalBits", 1.0f, 'w', "CrystalBits"),
            ("Kit_Ripple", 1.6f, 'w', "Ripple"),
            // 큰 지형지물 (blender_kit_ch3_landmarks.py)
            ("Kit_LM_ArchRuin", 5f, 'w', "LM_ArchRuin"),
            ("Kit_LM_CrystalSpire", 3.6f, 'w', "LM_CrystalSpire"),
            ("Kit_LM_Hourglass", 3.6f, 'w', "LM_Hourglass"),
            ("Kit_LM_FloatRock", 4.0f, 'w', "LM_FloatRock"),
            ("Kit_LM_MirrorGate", 5f, 'w', "LM_MirrorGate"),
            ("Kit_LM_Obelisk", 2.4f, 'w', "LM_Obelisk"),
            ("Kit_LM_Dune", 5.6f, 'w', "LM_Dune"),
            // 보스 소품 (blender_kit_ch3_bossprops.py): 상인 가면 · 마술사 소품 · 거울 극장 벽 · 거울 제단
            ("Kit_MerchantMask", 0.62f, 'w', "MerchantMask"),
            ("Kit_SwordBox", 1.6f, 'w', "SwordBox"),
            ("Kit_MagicSword", 1.8f, 'd', "MagicSword"),
            ("Kit_MirrorPanel", 6f, 'w', "MirrorPanel"),
            ("Kit_Dove", 0.9f, 'w', "Dove"),
            ("Kit_NumberCard", 1.3f, 'h', "NumberCard"),
            ("Kit_MemoryShard", 2.2f, 'h', "MemoryShard"),
            ("Kit_Facade_Curtain", 8f, 'w', "Facade_Curtain"),
            ("Kit_Facade_MirrorHall", 6f, 'w', "Facade_MirrorHall"),
            ("Kit_Altar", 40f, 'w', "Altar"),
            // 보스 (blender_kit_ch3_bosses.py, 리깅 없음 — 이름 붙은 조각을 코드가 돌림): 신기루 마술사 4m · 거울 고래 길이 14m
            ("Kit_Ch3_Magician", 4.0f, 'h', "Ch3_Magician"),
            ("Kit_Ch3_Whale", 14f, 'd', "Ch3_Whale"),
        };

        /// <summary>스테이지별 문 · 복도 · 문틀 + 보상 문만 다시 빌드 (1 · 2 · 3장, blender_kit_ch{1,2,3}_gates.py)</summary>
        [MenuItem("CatTang/스테이지 문 · 복도 만들기 (블렌더)")]
        public static void BuildGates()
        {
            var sets = new (string src, string dir, Dictionary<string, Color> pal)[]
            {
                (Ch1Src, KitDir, ModelPalette.Ch1), (Ch2Src, Kit2Dir, ModelPalette.Ch2), (Ch3Src, Kit3Dir, ModelPalette.Ch3),
                (Ch4Src, Kit4Dir, ModelPalette.Ch4), (Ch5Src, Kit5Dir, ModelPalette.Ch5),
            };
            int made = 0;
            try
            {
                foreach (var (src, dir, pal) in sets)
                {
                    if (!File.Exists(src + "Kit_Gate_S1.fbx")) continue;
                    Directory.CreateDirectory(dir);
                    SrcDir = src;
                    ModelPalette.Active = pal;
                    for (int n = 1; n <= 5; n++)
                        foreach (var (b, size, fit) in n <= 4
                            ? new[] { ($"Gate_S{n}", 4.6f, 'w'), ($"Corridor_S{n}", 10.4f, 'd'), ($"Portal_S{n}", 6.8f, 'w') }
                            : new[] { ("RewardDoor", 3.0f, 'w') })
                            if (File.Exists(src + "Kit_" + b + ".fbx") && Build("Kit_" + b, size, 180f, fit, dir, b)) made++;
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EnemyModelBuilder] 스테이지 문 · 복도 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        [MenuItem("CatTang/3장 키트 만들기 (블렌더)")]
        public static void BuildCh3Kit()
        {
            Directory.CreateDirectory(Kit3Dir);
            SrcDir = Ch3Src;
            ModelPalette.Active = ModelPalette.Ch3;
            try
            {
                int made = 0;
                foreach (var k in Kit3)
                    if (Build(k.name, k.size, 180f, k.fit, Kit3Dir, k.outName)) made++;
                GroundMat("Ground", "Assets/Art/Env/Ch3/ground_ch3.png");
                GroundMat("GroundCloud", "Assets/Art/Env/Ch3/ground_ch3_cloud.png");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EnemyModelBuilder] 3장 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        // 4장 맵 키트 (2026-11-19, 블렌더 + Codex, 회색 + 역할 재질 → ModelPalette.Ch4 느와르). 명세 Art/tools/specs/kit_ch4_{walls,props,gates}_spec.md + _ch4_common.md
        const string Ch4Src = "Assets/Art/Characters/Ch4/";
        const string Kit4Dir = "Assets/Resources/Kit/Ch4/";
        static readonly (string name, float size, char fit, string outName)[] Kit4 =
        {
            // 벽면 (blender_kit_ch4_walls.py) — 폭이 정확해야 줄이 맞음
            ("Kit_Wall_Storehouse", 6f, 'w', "Facade_Storehouse"),
            ("Kit_Wall_Gears", 6f, 'w', "Facade_Gears"),
            ("Kit_Wall_BellArch", 6f, 'w', "Facade_BellArch"),
            ("Kit_Wall_DialBack", 8f, 'w', "Facade_DialBack"),
            ("Kit_Wall_Pendulum", 6f, 'w', "Facade_Pendulum"),
            ("Kit_Wall_Girder", 6f, 'w', "Facade_Girder"),
            ("Kit_Wall", 4f, 'w', "Wall"),
            // 방 기믹 · 오브젝트 · 엄폐물 · 상인 (blender_kit_ch4_props.py)
            ("Kit_GearFloor", 8f, 'w', "GearFloor"),
            ("Kit_WindSpring", 1.5f, 'h', "WindSpring"),
            ("Kit_GearWheel", 1.6f, 'h', "GearWheel"),
            ("Kit_OldBell", 2.4f, 'h', "OldBell"),
            ("Kit_GearCrate", 1.6f, 'w', "GearCrate"),
            ("Kit_BrokenClockPillar", 2.6f, 'h', "BrokenClockPillar"),
            ("Kit_SpringBarrel", 1.3f, 'h', "SpringBarrel"),
            ("Kit_MusicBox", 2.6f, 'w', "MusicBox"),
            ("Kit_CogBits", 1.2f, 'w', "CogBits"),
            ("Kit_ScrewBits", 1.0f, 'w', "ScrewBits"),
            ("Kit_FloorGrate", 2.0f, 'w', "FloorGrate"),
            // 큰 지형지물 (blender_kit_ch4_landmarks.py, 명세 kit_ch4_landmarks_spec.md)
            ("Kit_LM_Clockwork", 5.0f, 'w', "LM_Clockwork"),
            ("Kit_LM_PendulumCase", 3.6f, 'w', "LM_PendulumCase"),
            ("Kit_LM_GearStack", 4.4f, 'w', "LM_GearStack"),
            ("Kit_LM_BellFrame", 4.2f, 'w', "LM_BellFrame"),
            ("Kit_LM_DialFragment", 5.2f, 'w', "LM_DialFragment"),
            // 신전 느낌 (2026-11-19 사용자: 공장 느낌 → 신전, blender_kit_ch4_temple.py, 명세 kit_ch4_temple_spec.md)
            ("Kit_Wall_Colonnade", 6f, 'w', "Facade_Colonnade"),
            ("Kit_Wall_RoseDial", 6f, 'w', "Facade_RoseDial"),
            ("Kit_LM_ClockAltar", 4.4f, 'w', "LM_ClockAltar"),
            ("Kit_LM_ArchRuin", 5.0f, 'w', "LM_ArchRuin"),
            ("Kit_Deco_BannerPillar", 4.2f, 'h', "Deco_BannerPillar"),
            ("Kit_Deco_Candles", 1.0f, 'w', "Deco_Candles"),
            // 넓은 방 채우기 (2026-11-19 사용자, blender_kit_ch4_temple2.py, 명세 kit_ch4_temple2_spec.md)
            ("Kit_Deco_NaveColumn", 6.5f, 'h', "Deco_NaveColumn"),
            ("Kit_Deco_AngelStatue", 3.0f, 'h', "Deco_AngelStatue"),
            ("Kit_Deco_LanternStand", 3.2f, 'h', "Deco_LanternStand"),
            ("Kit_Deco_Pew", 3.0f, 'w', "Deco_Pew"),
            ("Kit_Deco_Reliquary", 2.0f, 'h', "Deco_Reliquary"),
            ("Kit_Deco_FloorDial", 6.0f, 'w', "Deco_FloorDial"),
        };

        [MenuItem("CatTang/4장 키트 만들기 (블렌더)")]
        public static void BuildCh4Kit()
        {
            Directory.CreateDirectory(Kit4Dir);
            SrcDir = Ch4Src;
            ModelPalette.Active = ModelPalette.Ch4;
            try
            {
                int made = 0;
                foreach (var k in Kit4)
                    if (File.Exists(Ch4Src + k.name + ".fbx") && Build(k.name, k.size, 180f, k.fit, Kit4Dir, k.outName)) made++;
                GroundMat("Ground", "Assets/Art/Env/Ch4/ground_ch4.png", Kit4Dir, mirror: false);
                // 톱니 맞물림 3D 불티 · 고철 재질 (메시 파티클, 색 = 파티클 색): 빛이 안 드는 단색(URP Particles/Unlit, 불투명) — GearPinch가 Resources에서 읽음
                FxMat("FX_SparkMat", new Color(1f, 1f, 1f));
                FxMat("FX_ScrapMat", new Color(1f, 1f, 1f));
                // 종지기 3D 음파 고리 (SoundRing): Codex 음파 띠 그림(마젠타 빼고 투명) → 반투명 단색 재질
                var wave = AssetDatabase.LoadAssetAtPath<Texture2D>(Kit4Dir + "FX_SoundWave.png");
                if (wave)
                {
                    var wm = FxMat("FX_SoundWaveMat", Color.white);
                    wm.SetTexture("_BaseMap", wave);
                    wm.SetFloat("_Surface", 1f); wm.SetFloat("_Blend", 0f);           // 반투명 (알파 섞기)
                    wm.SetOverrideTag("RenderType", "Transparent");
                    wm.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    wm.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    wm.SetInt("_ZWrite", 0);
                    wm.SetFloat("_Cull", 0f);                                          // 양면
                    wm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    wm.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    EditorUtility.SetDirty(wm);
                }
                // 상인 오르골 인형 (블렌더 + Codex, 사용자: Tripo 말고 블렌더로) → Resources/Merchants/Ch4_Merchant (RoomBuilder.MerchantFigure가 씀, 거울 고양이 대신)
                if (File.Exists(Ch4Src + "Kit_Ch4_Merchant.fbx"))
                {
                    ModelPalette.Active = ModelPalette.Ch4Merchant;
                    Directory.CreateDirectory(MerchantOut);
                    if (Build("Kit_Ch4_Merchant", 2.6f, 180f, 'h', MerchantOut, "Ch4_Merchant")) made++;
                    ModelPalette.Active = ModelPalette.Ch4;
                }   // 4장 리벳 철판 바닥 (옆 창 ChatGPT 그림 → Art/tools/ground_texture_ch4.py로 이음매 맞춤)
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EnemyModelBuilder] 4장 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
            BuildGates();
        }

        // ---------- 4장 적 (2026-11-19, 느와르): 크롬 ChatGPT 원화(빈손 · 정면 A자) → 크롬 Tripo H3.1 + 리토포 1만 + 자동 리깅(Mixamo) → Art/Characters/Ch4/Ch4_*.fbx ----------
        // 무기는 따로 (사용자: 무기를 같이 모델링하면 모션이 부자연스러움): 블렌더 + Codex Kit_W_*.fbx (원점 = 손잡이, 긴 축 = +Y) → Resources/Kit/Ch4/W_*
        // → 적 프리팹의 손 뼈(손바닥 자리) 아래에 붙임. 4장 적은 느와르라 적 배색 치환(EnemyPalette)은 안 하고 빨간 바깥 테두리만
        static readonly (string name, float size)[] Models4 =
        {
            ("Ch4_Soldier", 2.5f), ("Ch4_Ringer", 2.4f), ("Ch4_Dancer", 2.6f), ("Ch4_Assassin", 2.6f), ("Ch4_Knight", 3.0f), ("Ch4_Repairman", 2.3f),
            // 강화형 (2026-11-19 사용자: 색만 바꾸지 말고 이전 장처럼 새로 뽑기 — 같은 배색, 더 강력한 모습): 기본보다 약 15% 큼 (1 · 2장 강화형과 같은 비율)
            ("Ch4_Soldier_Plus", 2.9f), ("Ch4_Ringer_Plus", 2.75f), ("Ch4_Dancer_Plus", 3.0f), ("Ch4_Assassin_Plus", 3.0f), ("Ch4_Knight_Plus", 3.45f), ("Ch4_Repairman_Plus", 2.65f),
            // 보스 (2026-11-20 사용자: Hyper3D로 다시, 컨셉 아트 느낌 살려서 · 집행관 · 시침은 육중하게): 원화 Art/tripo_input/ch4_boss/*_concept.png → Hyper3D 생성 + 리깅
            ("Ch4_Executioner", 4.2f), ("Ch4_HourHand", 5.0f), ("Ch4_MinuteHand", 4.0f),
        };
        // 무기: 적 이름 → (무기, 손) 목록. 손 = true 오른손
        static readonly Dictionary<string, (string weapon, bool right)[]> Weapons4 = new()
        {
            { "Ch4_Soldier", new[] { ("Rifle", true) } },   // 손잡이 각도 = WeaponGrip4 (조준 자세 기준)
            { "Ch4_Ringer", new[] { ("Hammer", true), ("Bell", false) } },
            { "Ch4_Assassin", new[] { ("NeedleDagger", true) } },
            { "Ch4_Knight", new[] { ("PendulumScythe", true) } },
            { "Ch4_Repairman", new[] { ("Wrench", true) } },
            // 강화 기사 (2026-11-19 사용자: Tripo가 계속 오류 → 블렌더로 추가 장비 + 주 무기): 기본 기사 몸(Ch4_Knight_Plus.fbx = 기본 FBX 복사 + 녹슨 텍스처) + 큰 양날 진자 낫
            { "Ch4_Knight_Plus", new[] { ("PendulumScythePlus", true) } },
            // 쌍둥이 보스: 시침 = 문자판 대검, 분침 = 레이피어 (집행관은 팔 자체가 칼날이라 없음)
            { "Ch4_HourHand", new[] { ("Greatsword", true) } },
            { "Ch4_MinuteHand", new[] { ("Rapier", true) } },
        };
        // 강화형 추가 장비: 블렌더 + Codex Kit_KP_*.fbx (원점 = 붙일 뼈 자리, +Y 앞 · +X 오른쪽, 2.2m 기사 기준) → Resources/Kit/Ch4/KP_*
        // → 뼈 아래에 몸 방향 그대로(쉬는 자세 기준) 붙임. 명세 Art/tools/specs/kit_ch4_knight_plus_spec.md
        // 보정 = 뼈 자리에서 더 옮길 거리 (2.2m 기준, 몸 방향: x 오른쪽 · y 위 · z 앞) — 화면으로 보고 맞춘 값
        static readonly Dictionary<string, (string piece, HumanBodyBones bone, Vector3 offset)[]> Armor4 = new()
        {
            { "Ch4_Knight_Plus", new[] {
                ("Pauldron_R", HumanBodyBones.RightUpperArm, new Vector3(-0.08f, 0.12f, 0)), ("Pauldron_L", HumanBodyBones.LeftUpperArm, new Vector3(0.08f, 0.12f, 0)),
                // 가슴 진자(KP_ChestPendulum)는 뺌 (2026-11-19 사용자: 2등신이라 가슴 높이 판이 얼굴을 가림, 기본 몸에 가슴 진자가 이미 있음)
                ("BackPendulum", HumanBodyBones.UpperChest, Vector3.zero),
                ("Tasset", HumanBodyBones.Hips, Vector3.zero), ("HelmCrest", HumanBodyBones.Head, new Vector3(0, -0.12f, 0)) } },
        };
        static readonly string[] ArmorNames4 = { "Pauldron_R", "Pauldron_L", "BackPendulum", "ChestPendulum", "Tasset", "HelmCrest" };
        // 무기 손잡이 각도 (손 뼈 기준 localEuler) — 쉬는 자세 기준(위로 세우고 앞 25°) 대신 공격 자세에서 맞춘 것.
        // 소총 (2026-11-19 사용자: 총 쏘는 자세가 이상함 → 측정): 팔 0.41m · 어깨 너비 0.47m인 2등신이라 견착 · 두 손 받치기가 닿지 않음
        // → 오른 겨드랑이에 끼고 한 손으로 수평 조준(왼손은 북 위). 조준 자세(Ch4M_SoldierShot 0.15~0.5초)에서 IK로 손을 놓고, 그때 총 +Y(총구) = 몸 앞 · 총 +Z = 위가 되는 손 기준 각도
        static readonly Dictionary<string, Vector3> WeaponGrip4 = new()
        {
            { "Ch4_Soldier/Rifle", new Vector3(57.7f, 142.7f, 352.8f) },
            // 나머지 무기: 그 적의 공격 자세 전체(손을 IK로 놓은 뒤)에서 무기가 가리켜야 할 방향들의 평균에 맞춘 손잡이 각도 (EnemyMotions의 *_RAISE · KN_* 등과 한 쌍)
            { "Ch4_Ringer/Hammer", new Vector3(10.2f, 172.4f, 11.6f) },
            { "Ch4_Ringer/Bell", new Vector3(302.5f, 346.5f, 235.8f) },
            { "Ch4_Assassin/NeedleDagger", new Vector3(50.9f, 141.2f, 21.7f) },
            { "Ch4_Knight/PendulumScythe", new Vector3(32.7f, 113.7f, 302.6f) },
            { "Ch4_Knight/PendulumScythePlus", new Vector3(32.7f, 113.7f, 302.6f) },   // 같은 손잡이 규칙(원점 = 손잡이, 긴 축 +Z)이라 같은 각도
            { "Ch4_Repairman/Wrench", new Vector3(37.7f, 142.9f, 344.6f) },
            // 5장: 4장 각도를 빌렸더니 보스 낫이 가로로 몸을 관통 · 마녀 지팡이가 거꾸로 매달림 (2026-11-19 사용자 지적) → EnemyModelBuilderCh5.Aim5가 idle 자세에서 방향을 잡음
        };
        static readonly string[] WeaponNames4 = { "Rifle", "Bell", "Hammer", "NeedleDagger", "PendulumScythe", "Wrench", "Greatsword", "Rapier", "PendulumScythePlus" };
        const float WeaponDesignHeight = 2.2f;   // 무기 크기를 정한 기준 키 (명세: 2등신 인형 약 2.2m)

        [MenuItem("CatTang/4장 무기 만들기 (블렌더)")]
        public static void BuildCh4Weapons()
        {
            Directory.CreateDirectory(Kit4Dir);
            SrcDir = Ch4Src;
            ModelPalette.Active = ModelPalette.Ch4;
            try
            {
                int made = 0;
                var pieces = new List<string>();
                foreach (var w in WeaponNames4) pieces.Add("W_" + w);
                foreach (var a in ArmorNames4) pieces.Add("KP_" + a);
                foreach (var w in pieces)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>(Ch4Src + "Kit_" + w + ".fbx");
                    if (!src) continue;
                    var root = new GameObject(w);
                    var inst = (GameObject)Object.Instantiate(src);
                    inst.name = "Mesh";
                    inst.transform.SetParent(root.transform, false);           // 원점 = 손잡이 그대로 (옮기지 않음)
                    var cache = new Dictionary<Material, Material>();
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i]) { if (!cache.TryGetValue(mats[i], out var t)) cache[mats[i]] = t = ToonFor("Kit_" + w, mats[i], cache.Count); mats[i] = t; }
                        r.sharedMaterials = mats;
                    }
                    foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    PrefabUtility.SaveAsPrefabAsset(root, Kit4Dir + w + ".prefab");
                    Object.DestroyImmediate(root);
                    made++;
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[EnemyModelBuilder] 4장 무기 {made}개");
            }
            finally { SrcDir = Ch1Src; ModelPalette.Active = ModelPalette.Ch1; }
        }

        [MenuItem("CatTang/4장 적 프리팹 만들기 (Tripo)")]
        public static void BuildCh4()
        {
            Directory.CreateDirectory(OutDir);
            BuildCh4Weapons();
            SrcDir = Ch4Src;
            try
            {
                foreach (var (n, h) in Models4)
                {
                    var imp = AssetImporter.GetAtPath(Ch4Src + n + ".fbx") as ModelImporter;
                    if (!imp) { Debug.Log($"[EnemyModelBuilder] {n} 원본 없음 — 건너뜀"); continue; }
                    if (imp.animationType != ModelImporterAnimationType.Human || imp.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        imp.animationType = ModelImporterAnimationType.Human;
                        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        imp.materialImportMode = ModelImporterMaterialImportMode.None;
                        imp.SaveAndReimport();
                    }
                    var srcGo = FindSource(n);
                    if (srcGo && !HumanAvatar(srcGo) && AssetImporter.GetAtPath(MerchantSrc + "Ch1_Merchant.fbx") is ModelImporter refImp)
                    {
                        var names = new HashSet<string>();
                        foreach (var t in srcGo.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
                        var hd = imp.humanDescription;
                        hd.human = System.Array.FindAll(refImp.humanDescription.human, hb => names.Contains(hb.boneName));
                        imp.humanDescription = hd;
                        imp.SaveAndReimport();
                    }
                    if (!Build(n, h, 0f, 'h', OutDir, n)) continue;
                    if (Weapons4.TryGetValue(n, out var ws) || Weapons4.TryGetValue(Base4(n), out ws)) AttachWeapons(OutDir + n + ".prefab", ws, h);
                    if (Armor4.TryGetValue(n, out var ar)) AttachArmor(OutDir + n + ".prefab", ar, h);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[EnemyModelBuilder] 4장 적 프리팹");
            }
            finally { SrcDir = Ch1Src; }
        }

        /// <summary>적 프리팹의 손 뼈에 무기를 붙임: 손바닥 가운데(손 뼈 → 가운뎃손가락 뿌리 사이)에 손잡이, 무기 긴 축 = 몸 위쪽에서 앞으로 25° 기운 방향(쉬는 자세 기준), 크기 = 키 비례</summary>
        static void AttachWeapons(string prefabPath, (string weapon, bool right)[] ws, float height)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var anim = root.GetComponentInChildren<Animator>();
                if (!anim || !anim.avatar || !anim.avatar.isHuman) { Debug.LogWarning($"[EnemyModelBuilder] {prefabPath} 휴머노이드 아님 — 무기 안 붙임"); return; }
                string who = Base4(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
                var restore = who.StartsWith("Ch5_") ? SampleIdle5(anim) : null;      // 5장: idle 자세의 손에서 방향을 잡음 (끝나면 뼈 원래대로)
                foreach (var (w, right) in ws)
                {
                    var wp = AssetDatabase.LoadAssetAtPath<GameObject>(PartsDir + "W_" + w + ".prefab");
                    var hand = anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                    if (!wp || !hand) continue;
                    var mid = anim.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
                    var grip = mid ? Vector3.Lerp(hand.position, mid.position, 0.6f) : hand.position;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(wp);
                    go.name = "Weapon_" + w;
                    var fwd = root.transform.forward;
                    var up = Quaternion.AngleAxis(25f, root.transform.right) * root.transform.up;     // 위로 세우되 앞으로 살짝
                    go.transform.SetPositionAndRotation(grip, Quaternion.LookRotation((fwd - Vector3.Dot(fwd, up) * up).normalized, up));
                    go.transform.localScale = Vector3.one * (height / WeaponDesignHeight);
                    if (restore != null && Aim5(root.transform, w, right, out var aim)) go.transform.rotation = aim;
                    go.transform.SetParent(hand, true);
                    if (restore == null && WeaponGrip4.TryGetValue(who + "/" + w, out var ge)) go.transform.localRotation = Quaternion.Euler(ge);
                }
                if (restore != null) foreach (var (t, lp, lr) in restore) if (t) { t.localPosition = lp; t.localRotation = lr; }   // 뼈를 묶음 자세로 (무기는 손 자식이라 함께 돌아감)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>파티클 메시용 단색 재질 (URP Particles/Unlit, 불투명, 정점 색 = 파티클 색 곱함)</summary>
        static Material FxMat(string name, Color c)
        {
            string p = Kit4Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetFloat("_Surface", 0f);                     // 불투명
            m.SetColor("_BaseColor", c);
            m.renderQueue = -1;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>강화형 추가 장비를 뼈에 붙임: 원점 = 뼈 위치, 방향 = 몸 방향(쉬는 자세), 크기 = 키 / 2.2m</summary>
        static void AttachArmor(string prefabPath, (string piece, HumanBodyBones bone, Vector3 offset)[] ps, float height)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var anim = root.GetComponentInChildren<Animator>();
                if (!anim || !anim.avatar || !anim.avatar.isHuman) return;
                foreach (var (p, b, off) in ps)
                {
                    var pp = AssetDatabase.LoadAssetAtPath<GameObject>(PartsDir + PartPrefix + p + ".prefab");
                    var bone = anim.GetBoneTransform(b);
                    if (!pp || !bone) { Debug.LogWarning($"[EnemyModelBuilder] 장비 {p} 없음 — 건너뜀"); continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pp);
                    go.name = "Armor_" + p;
                    // 블렌더 +Y(앞)는 FBX로 들어오면 유니티 -Z → 몸 앞(+Z)에 맞게 반 바퀴 돌림 (2026-11-19 화면 확인)
                    go.transform.SetPositionAndRotation(bone.position + root.transform.rotation * off * (height / WeaponDesignHeight), root.transform.rotation * Quaternion.Euler(0, 180, 0));
                    go.transform.localScale = Vector3.one * (height / WeaponDesignHeight);
                    // 어깨 갑옷: 자리는 위팔 관절이지만 쇄골 뼈에 붙임 (팔을 내리고 휘둘러도 갑옷이 어깨 위에 남게)
                    var parent = b == HumanBodyBones.RightUpperArm ? anim.GetBoneTransform(HumanBodyBones.RightShoulder)
                               : b == HumanBodyBones.LeftUpperArm ? anim.GetBoneTransform(HumanBodyBones.LeftShoulder) : null;
                    go.transform.SetParent(parent ? parent : bone, true);
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ---------- 메인 마을 키트 (2026-11-18 사용자: 1장 키트 재활용 X, '소멸의 여로' 느낌 — 새벽에 사라져 가는 서부 마을, 허공의 큰 섬) ----------
        // 블렌더 + Codex, 회색 + 역할 재질 → ModelPalette.Village. 명세 Art/tools/specs/kit_village_{buildings,island,props}_spec.md + _village_common.md
        public const string VillageSrc = "Assets/Art/Characters/Village/";
        public const string VillageKitDir = "Assets/Resources/Kit/Village/";
        static readonly (string name, float size, char fit, string outName)[] KitVillage =
        {
            // 시설 건물 (blender_kit_village_buildings.py) — 위쪽이 조각으로 흩어져 떠오름
            ("Kit_V_Inn", 11f, 'w', "Inn"),
            ("Kit_V_GunShop", 9f, 'w', "GunShop"),
            ("Kit_V_AmmoWorkshop", 9f, 'w', "AmmoWorkshop"),
            ("Kit_V_Wardrobe", 8f, 'w', "Wardrobe"),
            ("Kit_V_TimeGate", 9f, 'w', "TimeGate"),
            // 섬 가장자리 · 떠 있는 조각 · 광장 · 길 (blender_kit_village_island.py)
            ("Kit_V_Edge_A", 6f, 'w', "Edge_A"),
            ("Kit_V_Edge_B", 6f, 'w', "Edge_B"),
            ("Kit_V_Edge_C", 6f, 'w', "Edge_C"),
            ("Kit_V_FloatIsle_S", 2.5f, 'w', "FloatIsle_S"),
            ("Kit_V_FloatIsle_M", 5f, 'w', "FloatIsle_M"),
            ("Kit_V_FloatIsle_L", 8f, 'w', "FloatIsle_L"),
            ("Kit_V_Debris", 3f, 'w', "Debris"),
            ("Kit_V_Plaza", 14.155f, 'w', "Plaza"),
            ("Kit_V_Path", 2.4f, 'w', "Path"),
            // 섬 사이 다리 (blender_kit_village_bridge.py) — 성한 다리 · 끊긴 다리 (잠긴 섬)
            ("Kit_V_Bridge", 3.2f, 'w', "Bridge"),
            ("Kit_V_BridgeBroken", 3.2f, 'w', "BridgeBroken"),
            // 소품 · 지형지물 (blender_kit_village_props.py)
            ("Kit_V_Centerpiece", 7f, 'h', "Centerpiece"),
            ("Kit_V_Windpump", 9f, 'h', "Windpump"),
            ("Kit_V_WaterTower", 9f, 'h', "WaterTower"),
            ("Kit_V_Wagon", 5f, 'w', "Wagon"),
            ("Kit_V_Well", 4f, 'h', "Well"),
            ("Kit_V_Lamp", 3.4f, 'h', "Lamp"),
            ("Kit_V_Cactus", 3.2f, 'h', "Cactus"),
            ("Kit_V_Fence", 4f, 'w', "Fence"),
            ("Kit_V_Barrels", 2.2f, 'h', "Barrels"),
            ("Kit_V_Crates", 2.2f, 'h', "Crates"),
            ("Kit_V_Bench", 2.2f, 'w', "Bench"),
            ("Kit_V_Signpost", 3.4f, 'h', "Signpost"),
            ("Kit_V_HitchPost", 2.4f, 'w', "HitchPost"),
            ("Kit_V_Tumbleweed", 1f, 'w', "Tumbleweed"),
            ("Kit_V_GrassTuft", 0.8f, 'w', "GrassTuft"),
            // 부서진 시간 · 몽환 오브젝트 (blender_kit_village_dream.py, 2026-11-18 사용자: 곳곳에 몽환적 · 부서진 시간 느낌 오브젝트)
            ("Kit_V_ClockHand_Giant", 7f, 'd', "ClockHand_Giant"),
            ("Kit_V_ClockHand_Float", 5.6f, 'd', "ClockHand_Float"),    // 받침 없이 시계탑 둘레를 떠서 도는 시곗바늘 (가운데 원점)
            ("Kit_V_Gear_Half", 4f, 'w', "Gear_Half"),
            ("Kit_V_GearStack_Float", 2.4f, 'w', "GearStack_Float"),
            ("Kit_V_ClockShards", 3f, 'w', "ClockShards"),
            ("Kit_V_Hourglass_Broken", 4f, 'h', "Hourglass_Broken"),
            ("Kit_V_TimeRift", 3.2f, 'h', "TimeRift"),
            ("Kit_V_Pendulum_Frozen", 4.5f, 'h', "Pendulum_Frozen"),
            ("Kit_V_MeltWatch_Rock", 1.8f, 'w', "MeltWatch_Rock"),
            ("Kit_V_StairsFloat", 3f, 'w', "StairsFloat"),
            ("Kit_V_TimeCrystal", 2.2f, 'h', "TimeCrystal"),
            ("Kit_V_NumeralStones", 2.5f, 'w', "NumeralStones"),
            ("Kit_V_MoonDial", 3.6f, 'h', "MoonDial"),
            // 신비한 나무 · 오브젝트 (blender_kit_village_mystic.py, 2026-11-18 사용자: 몽환적인 신비한 나무 · 오브젝트, 여로 느낌)
            ("Kit_V_DreamTree_Crystal", 7f, 'h', "DreamTree_Crystal"),
            ("Kit_V_DreamTree_Willow", 6.5f, 'h', "DreamTree_Willow"),
            ("Kit_V_DreamTree_Small", 3f, 'h', "DreamTree_Small"),
            ("Kit_V_SpiritLanterns", 4f, 'h', "SpiritLanterns"),
            ("Kit_V_BellFlowers", 2.4f, 'w', "BellFlowers"),
            ("Kit_V_RuneArch", 4.5f, 'h', "RuneArch"),
            ("Kit_V_FloatRock_Crystal", 3.5f, 'h', "FloatRock_Crystal"),
            ("Kit_V_GlowMushrooms", 1.6f, 'w', "GlowMushrooms"),
        };

        [MenuItem("CatTang/마을 키트 만들기 (블렌더)")]
        public static void BuildVillageKit()
        {
            Directory.CreateDirectory(VillageKitDir);
            SrcDir = VillageSrc;
            ModelPalette.Active = ModelPalette.Village;
            try
            {
                int made = 0;
                foreach (var k in KitVillage)
                    if (Build(k.name, k.size, 180f, k.fit, VillageKitDir, k.outName)) made++;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EnemyModelBuilder] 마을 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        /// <summary>마을 키트의 불 켜진 창 · 등불 · 흩어지는 모래 빛 · 빛가루: 툰 대신 빛나는 단색 (새벽 어둠 속에서 따뜻하게 빛남)</summary>
        static void VillageGlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = VillageSrc + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var win = Get("Glow_Window", new Color(1f, 0.8f, 0.42f, 1f), 0.3f);
            var lantern = Get("Glow_Lantern", new Color(1f, 0.78f, 0.36f, 1f), 0.5f);
            var sand = Get("Glow_Sand", new Color(1f, 0.88f, 0.56f, 1f), 0.4f);
            var dust = Get("Glow_Stardust", new Color(1f, 0.96f, 0.82f, 1f), 0.3f);
            var core = Get("Glow_CrystalCore", new Color(0.82f, 0.95f, 1f, 1f), 0.5f);      // 시간 수정 속 · 시간의 균열 (차가운 빛)
            var rift = Get("Glow_Rift", new Color(0.86f, 0.98f, 1f, 1f), 0.7f);
            var petal = Get("Glow_Petal", new Color(1f, 0.82f, 0.95f, 1f), 0.5f);         // 신비한 나무 꽃 · 빛 구슬
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var pick = SrcMatName(r, i) switch { "window_lit" => win, "lantern_glow" => lantern, "sand_glow" => sand, "stardust" => dust, "crystal_core" => core, "rift_glow" => rift, "petal_glow" => petal, _ => null };
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>3장 바닥 재질: 2장 Ground.mat(같은 셰이더 · 설정)을 복사해 텍스처만 바꿈 (Art/tools/ground_texture_ch3.py)</summary>
        static void GroundMat(string matName, string texPath, string dir = null, bool mirror = true)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (!tex) { Debug.LogWarning($"[EnemyModelBuilder] 바닥 텍스처 없음 {texPath}"); return; }
            string p = (dir ?? Kit3Dir) + matName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m)
            {
                var src = AssetDatabase.LoadAssetAtPath<Material>(Kit2Dir + "Ground.mat");
                m = src ? new Material(src) : new Material(Shader.Find("CatTang/Toon"));
                AssetDatabase.CreateAsset(m, p);
            }
            // 거울 호수(Ground)는 실시간 반사를 섞는 거울 바닥 셰이더, 구름 바닥은 그대로
            if (mirror && matName == "Ground") { var sh = Shader.Find("CatTang/MirrorFloor"); if (sh) { m.shader = sh; m.SetFloat("_ReflStrength", 0.85f); m.SetColor("_ReflTint", new Color(1f, 1f, 1.05f, 1f)); } }
            m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(m);
        }

        /// <summary>3장 거울면 · 빛나는 수정 · 빛가루: 툰 대신 빛나는 단색(CatTang/Silhouette 불투명, 그림자 없음) — 거울은 옅은 하늘빛, 수정 속은 분홍 흰빛</summary>
        static void GlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = Ch3Src + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var mirror = Get("Glow_Mirror", new Color(0.86f, 0.95f, 1f, 1f), 0.6f);
            var core = Get("Glow_CrystalCore", new Color(1f, 0.82f, 0.94f, 1f), 0.4f);
            var dust = Get("Glow_Stardust", new Color(1f, 0.93f, 0.66f, 1f), 0.3f);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = SrcMatName(r, i);
                    var pick = src == "mirror" ? mirror : src == "crystal_core" ? core : src == "stardust" ? dust : null;
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>FBX 원본 렌더러의 i번째 재질 이름 (툰으로 바꾸기 전 이름, 블렌더 .001 접미사 뗌)</summary>
        static string SrcMatName(Renderer r, int i)
        {
            var mf = r.GetComponent<MeshFilter>();
            var mesh = mf ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh;
            if (!mesh) return "";
            var path = AssetDatabase.GetAssetPath(mesh);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!src) return "";
            foreach (var sr in src.GetComponentsInChildren<Renderer>(true))
            {
                var sm = sr.GetComponent<MeshFilter>();
                if (sm && sm.sharedMesh == mesh && i < sr.sharedMaterials.Length && sr.sharedMaterials[i])
                    return System.Text.RegularExpressions.Regex.Replace(sr.sharedMaterials[i].name.ToLowerInvariant(), @"\.\d+$", "");
            }
            return "";
        }

        // ---------- 상인 (2026-11-18 사용자: 사람 말고 시간에 잠식된 몽환적 존재) ----------
        // 원본: Art/Characters/Merchant (ChatGPT 원화 → Tripo H3.1 + 리깅), 1장 = 녹아내리는 회중시계 머리 바텐더 · 2장 = 얼어붙은 불꽃 랜턴 머리 매표원
        // → Resources/Merchants/Ch{장}_Merchant.prefab (RoomBuilder.MerchantFigure가 씀). 움직임 = 고양이 idle 클립을 휴머노이드로 돌려 씀
        const string MerchantSrc = "Assets/Art/Characters/Merchant/";
        const string MerchantOut = "Assets/Resources/Merchants/";
        static readonly (string name, float height)[] Merchants = { ("Ch1_Merchant", 2.9f), ("Ch2_Merchant", 3.0f) };   // 고양이 2.27m의 약 1.3배: 카운터(1.3m) 너머로 상체가 보이게

        [MenuItem("CatTang/상인 프리팹 만들기 (Tripo)")]
        public static void BuildMerchants()
        {
            foreach (var (n, h) in Merchants) BuildIdleFigure(MerchantSrc, MerchantOut, n, h);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyModelBuilder] 상인 프리팹");
        }

        // ---------- 마을 NPC 5 = 시계공이 잊어버린 존재 (2026-11-18, 컨셉 기획서 5.2) ----------
        // 원본: Art/Characters/Npc (ChatGPT 원화 Art/tripo_input/npc → Tripo H3.1 + 리깅) → Resources/Npcs/Npc_*.prefab
        // VillageKitBuilder가 마을 NpcSpot_*에 프리팹으로 세움. 움직임 = 상인과 같이 고양이 idle
        public const string NpcSrc = "Assets/Art/Characters/Npc/";
        public const string NpcOut = "Assets/Resources/Npcs/";
        public static readonly (string name, float height)[] Npcs =
        {
            ("Npc_Cuckoo", 2.1f),       // 태엽 풀린 뻐꾸기 — 기억의 방 (고양이 2.27m보다 조금 작게)
            ("Npc_Hourglass", 2.7f),    // 모래시계 대장장이 — 탄약 공방
            ("Npc_Sheriff", 2.9f),      // 빈 외투 보안관 — 총포상
            ("Npc_Seamstress", 2.7f),   // 실타래 재봉사 — 의상실
            ("Npc_Gatekeeper", 3.8f),   // 괘종시계 문지기 — 시간의 문 (가장 큼)
        };

        [MenuItem("CatTang/마을 NPC 프리팹 만들기 (Tripo)")]
        public static void BuildNpcs()
        {
            foreach (var (n, h) in Npcs) BuildIdleFigure(NpcSrc, NpcOut, n, h);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyModelBuilder] 마을 NPC 프리팹");
        }

        /// <summary>Tripo 리깅 FBX → 툰 프리팹 + 고양이 idle 애니메이터 (상인 · 마을 NPC 공용)</summary>
        static void BuildIdleFigure(string srcDir, string outDir, string n, float h)
        {
            Directory.CreateDirectory(outDir);
            SrcDir = srcDir;
            try
            {
                // FBX를 휴머노이드로 (아바타 생성, 재질 가져오기 끔)
                var imp = AssetImporter.GetAtPath(srcDir + n + ".fbx") as ModelImporter;
                if (!imp) { Debug.Log($"[EnemyModelBuilder] {n} 원본 없음 — 건너뜀"); return; }
                if (imp.animationType != ModelImporterAnimationType.Human || imp.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    imp.animationType = ModelImporterAnimationType.Human;
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.SaveAndReimport();
                }
                // 자동 매핑이 손가락 뼈를 겹쳐 잡아 아바타가 깨지면(2장 매표원) 같은 mixamo 뼈대인 1장 바텐더의 매핑을 빌려 씀
                var srcGo = FindSource(n);
                if (srcGo && !HumanAvatar(srcGo) && n != "Ch1_Merchant" && AssetImporter.GetAtPath(MerchantSrc + "Ch1_Merchant.fbx") is ModelImporter refImp)
                {
                    var names = new HashSet<string>();
                    foreach (var t in srcGo.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
                    var hd = imp.humanDescription;
                    hd.human = System.Array.FindAll(refImp.humanDescription.human, hb => names.Contains(hb.boneName));
                    imp.humanDescription = hd;
                    imp.SaveAndReimport();
                }
                if (!Build(n, h, 0f, 'h', outDir, n)) return;
                // 대기 동작: 고양이 idle
                var src = FindSource(n);
                var avatar = src ? HumanAvatar(src) : null;
                AnimationClip idle = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CatHero/CatHero.fbx"))
                    if (o is AnimationClip c && c.name == "idle") idle = c;
                string pp = outDir + n + ".prefab";
                var contents = PrefabUtility.LoadPrefabContents(pp);
                var mesh = contents.transform.Find("Mesh");
                if (avatar && idle && mesh)
                {
                    string cp = srcDir + n + ".controller";
                    AssetDatabase.DeleteAsset(cp);
                    var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(cp, idle);
                    var an = mesh.GetComponent<Animator>();
                    if (!an) an = mesh.gameObject.AddComponent<Animator>();
                    an.avatar = avatar;
                    an.applyRootMotion = false;
                    an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    an.runtimeAnimatorController = ctrl;
                }
                else Debug.LogWarning($"[EnemyModelBuilder] {n}: 아바타 {(bool)avatar} · idle {(bool)idle} — 움직임 없이 저장");
                PrefabUtility.SaveAsPrefabAsset(contents, pp);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            finally { SrcDir = Ch1Src; }
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
            if (avatar && (Anims.TryGetValue(outName, out var spec) || Anims.TryGetValue(Base4(outName), out spec)))
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
            float basis = fit == 'w' ? b.size.x : fit == 'd' ? b.size.z : b.size.y;
            if (basis > 0.0001f)
            {
                float s = height / basis;
                inst.transform.localScale *= s;
                b = Bounds(inst);
                // 바닥 타일은 윗면을 y = 0.02에, 나머지는 발바닥을 y = 0에
                // 3장 섬 가장자리는 밑면이 바닥 아래로 늘어져 있어 원래 원점(테두리 바닥 = 0) 그대로
                float y = outName == "Floor" ? b.max.y - 0.02f : KeepOrigin.Contains(name) ? inst.transform.position.y : b.min.y;
                inst.transform.localPosition -= new Vector3(b.center.x, y, b.center.z);
                // 마을 신비 소품: 흙 받침 · 둘레 돌이 땅과 따로 놀아서(2026-11-18 사용자: 균일화) 받침 윗면까지 땅에 묻음 → 식물이 땅에서 바로 자람
                if (SinkBed.Contains(name)) { float top = SubmeshTop(inst, "earth"); if (top > 0f) inst.transform.localPosition -= new Vector3(0f, top, 0f); }
            }

            if (PorterRig.IsPorter(outName)) PorterRig.AddFootGround(inst);

            // 굳은 승객 석상: 빛나는 금 선 재질 + 깨어남 컴포넌트
            if (name.StartsWith("Kit_Passenger")) { PassengerMats(inst); root.AddComponent<CatTang.FrozenPassenger>(); }
            // 3장 키트: 거울면 · 수정 속 · 빛가루는 빛나는 단색
            if (name.StartsWith("Kit_") && SrcDir == Ch3Src) GlowMats(inst);
            if (name.StartsWith("Kit_V_") && SrcDir == VillageSrc) VillageGlowMats(inst);
            if (name.StartsWith("Kit_") && SrcDir == Ch5Src) Ch5GlowMats(inst);   // 5장: 금 균열 · 별빛 · 거울 · 자홍은 빛나는 단색

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
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", SrcDir == Ch5Src ? new[] { SrcDir.TrimEnd('/'), Ch1Src.TrimEnd('/'), Ch3Src.TrimEnd('/'), Ch4Src.TrimEnd('/') } : new[] { SrcDir.TrimEnd('/'), Ch1Src.TrimEnd('/') }))
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
            Ch2Motion.MirrorFrontBack(clip, idle);                         // 앞뒤 근육 방향 바로잡기 ('Front-Back' + = 뒤, Ch2Motion 참고)
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
            Ch2Motion.MirrorFrontBack(clip, idle);                         // 앞뒤 근육 방향 바로잡기 ('Front-Back' + = 뒤, Ch2Motion 참고)
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
            Ch2Motion.MirrorFrontBack(clip, idle);                         // 앞뒤 근육 방향 바로잡기 ('Front-Back' + = 뒤, Ch2Motion 참고)
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
            var idle = sm.AddState("Idle"); idle.motion = porter ? FindClip(pk + "idle") : FindClip(Idles.TryGetValue(Base4(outName), out var idk) ? idk : "idle");
            var run = sm.AddState("Run"); run.motion = porter ? FindClip(pk + "walk") : FindClip(Walks.TryGetValue(Base4(outName), out var wk) ? wk : Ch2Motion.WalkFor(outName) ?? "run");
            sm.defaultState = idle;
            var t = idle.AddTransition(run); t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.12f;
            t = run.AddTransition(idle); t.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed"); t.hasExitTime = false; t.duration = 0.18f;
            AddAction(sm, idle, "Attack", FindClip(spec.atk), spec.atkDur, spec.atkLoop);
            AddAction(sm, idle, "Special", FindClip(spec.sp), spec.spDur, false);
            // 폭탄병 모래 폭탄 투척 (2026-11-17: 떨기 클립으로 던져서 모션 없이 터지는 것처럼 보였음)
            if (outName.Contains("Bomber")) AddAction(sm, idle, "Throw", FindClip("bomberthrow"), ThrowEnd, false);
            if (porter) AddAction(sm, idle, "Throw", FindClip(pk + "throw"), PorterRig.ThrowEnd, false);
            if (Moves.TryGetValue(Base4(outName), out var mv))
                foreach (var (state, key, dur, loop) in mv)
                {
                    var clip = FindClip(key);
                    if (!clip) { Debug.LogWarning($"[EnemyModelBuilder] {outName} 상태 {state}: 클립 '{key}' 없음"); continue; }
                    AddMove(sm, idle, state, clip, dur, loop);
                }
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

        /// <summary>스킬별 전용 상태: 트리거 전이 없이 코드가 이름으로 바로 재생 (Animator.CrossFade). 끝나면 Idle로 (반복이면 Busy가 꺼질 때)</summary>
        static void AddMove(AnimatorStateMachine sm, AnimatorState idle, string name, AnimationClip clip, float dur, bool loop)
        {
            var st = sm.AddState(name);
            st.motion = clip;
            st.speed = dur > 0 ? clip.length / dur : 1f;
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
        static readonly HashSet<string> Pickups = new() { "Kit_Heart", "Kit_XpGem", "Kit_SandBomb", "Kit_Keg", "Kit_SteamValve", "Kit_FloatLuggage", "Kit_MineCart", "Kit_MemoryCrystal", "Kit_WarpedKeg", "Kit_WarpedTrunk" };
        // 바닥에 깔리는 납작한 조각 (전차대 · 선로 · 승강장 가장자리)
        // 원점 높이를 그대로 두는 조각 (밑이 바닥 아래로 내려가는 것): 섬 가장자리 밑면 · 제단 테두리
        static readonly HashSet<string> KeepOrigin = new() { "Kit_Cliff_Island", "Kit_Altar",
            "Kit_V_Edge_A", "Kit_V_Edge_B", "Kit_V_Edge_C", "Kit_V_FloatIsle_S", "Kit_V_FloatIsle_M", "Kit_V_FloatIsle_L", "Kit_V_Debris", "Kit_V_Bridge", "Kit_V_BridgeBroken", "Kit_V_Plaza",
            "Kit_V_GearStack_Float", "Kit_V_ClockShards", "Kit_V_StairsFloat", "Kit_V_FloatRock_Crystal", "Kit_V_ClockHand_Float" };   // 마을: 윗면 · 다리 바닥 = 원점(밑으로 늘어짐), 부스러기 = 가운데
        // 흙 받침(moss_rock, 재질 earth)을 땅에 묻는 소품 (blender_kit_village_mystic.py)
        static readonly HashSet<string> SinkBed = new() { "Kit_V_BellFlowers", "Kit_V_GlowMushrooms", "Kit_V_SpiritLanterns", "Kit_V_DreamTree_Crystal" };

        /// <summary>원본 재질 이름이 role인 서브메시 정점 중 가장 높은 월드 y (없으면 0)</summary>
        static float SubmeshTop(GameObject inst, string role)
        {
            float top = 0f;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                var mesh = mf ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh;
                if (!mesh) continue;
                var verts = mesh.vertices;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (SrcMatName(r, i) != role) continue;
                    foreach (int vi in mesh.GetTriangles(i)) top = Mathf.Max(top, r.transform.TransformPoint(verts[vi]).y);
                }
            }
            return top;
        }

        static readonly HashSet<string> FlatFloor = new() { "Kit_Turntable", "Kit_Arena", "Kit_RailTrack", "Kit_PlatformEdge", "Kit_Ripple", "Kit_Pebbles3", "Kit_Altar" };
        // 쏘면 터지는 물체 (1장 화약통 · 2장 증기 밸브 · 공중에 멈춘 짐 · 광차) → 프리팹에 DangerEdge
        static readonly HashSet<string> Explosives = new() { "Kit_Keg", "Kit_SteamValve", "Kit_FloatLuggage", "Kit_MineCart", "Kit_MemoryCrystal", "Kit_WarpedKeg", "Kit_WarpedTrunk" };

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
            // 1장 팔레트: 황혼 보라 그림자 + 금빛 테두리. 4장(느와르)은 색 없는 회색 그림자 (2026-11-19 사용자: 흑백 대비)
            bool noir = name.StartsWith("Ch4_") || (name.StartsWith("Kit_W_") && SrcDir == Ch4Src) || ModelPalette.Active == ModelPalette.Ch4;
            bool cosmos = name.StartsWith("Ch5_") || SrcDir == Ch5Src;   // 5장: 보랏빛 그림자 (보라 · 금 우주)
            m.SetColor("_ShadeColor", noir ? new Color(0.6f, 0.6f, 0.63f) : cosmos ? new Color(0.7f, 0.6f, 0.86f) : new Color(0.72f, 0.58f, 0.66f));
            m.SetColor("_SecondShadeColor", noir ? new Color(0.3f, 0.3f, 0.33f) : cosmos ? new Color(0.4f, 0.3f, 0.58f) : new Color(0.45f, 0.34f, 0.46f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", FlatFloor.Contains(name) ? 1f : 4f);   // 2026-11-16 사용자: 외곽선 두껍게 (납작한 바닥 조각은 판자 선이 지글거려 얇게)
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            // 배경 키트는 채도 · 밝기를 낮춤 (EnvTone), 캐릭터 · 하트 · 폭탄 · 화약통은 원래 색
            bool env = name.StartsWith("Kit_") && !Pickups.Contains(name) && !(SrcDir == Ch5Src && (name.StartsWith("Kit_W_") || name.StartsWith("Kit_EP_") || name == "Kit_Ch5_MirrorPhantom"));
            if (m.HasProperty("_Saturation")) { m.SetFloat("_Saturation", env ? EnvTone.Saturation : 1f); m.SetFloat("_Brightness", env ? EnvTone.Brightness : 1f); }
            // 적: 배경과 갈리게 살짝 진홍 외곽선 + 붉은 가장자리 빛 (2026-11-17 사용자: 적에게 살짝 배경선)
            if ((name.StartsWith("Ch1_") || name.StartsWith("Ch2_") || name.StartsWith("Ch3_")) && !name.EndsWith("_Merchant")) { EnemyEdge(m); EnemyPalette.ApplyCurrent(m); }
            if ((name.StartsWith("Ch4_") || name.StartsWith("Ch5_")) && !name.EndsWith("_Merchant")) EnemyEdge(m);   // 5장 적도 원래 색 그대로 + 빨간 바깥 테두리
            if (name == "Kit_Ch5_MirrorPhantom") EnemyEdge(m);   // 4장 적: 느와르 원래 색 그대로 + 빨간 바깥 테두리만   // 적 배색 2안 (차가운 한 계열). 상인은 적이 아님
            // 3장 블렌더 보스(마술사 · 고래): 적이라 빨간 바깥 테두리 (색은 이미 차가운 몬스터 색으로 뽑아 배색 치환은 안 함)
            if (name == "Kit_Ch3_Magician" || name == "Kit_Ch3_Whale") EnemyEdge(m);
            if ((name == "Kit_Heart" || name == "Kit_XpGem") && m.HasProperty("_Saturation")) { m.SetFloat("_Saturation", 1.15f); m.SetFloat("_Brightness", 1.12f); }   // 하트 · 경험치 보석은 더 선명하게
            if (name == "Kit_XpGem" && m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 2.5f);   // 작은 물체라 외곽선을 얇게
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
