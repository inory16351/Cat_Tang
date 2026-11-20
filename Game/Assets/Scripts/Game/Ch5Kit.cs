// 5장 (무너지는 시간의 끝) 방 꾸미기 + 방 기믹 · 오브젝트 (docs/Cat_Tang_레벨_디자인.md 챕터 5 · 수치 기준 (챕터 5), 2026-11-19)
// - 장소(스테이지)별 벽면: 1 부서진 거울 문턱 → 2 뒤섞인 기억 회랑(서부 · 기차역 / 사막 · 시계탑) → 3 떠도는 잔해 섬 가장자리 → 4 무너지는 시계 심장
// - 방 기믹 '허공의 균열'(Rift): 전투 중 8초마다 고양이 근처 허공에 금 간 균열 → 예고 1.5초 → 깨지며 반경 2.5m 유리 조각 타격(적 30 · 고양이 15).
//   깨진 자리에 6초 동안 균열 흉터가 남음 = 균열 자객이 숨었다 튀어나오는 자리 (RiftSpots). 균열술사가 여는 균열도 같은 규격
// - 앞 장 대표 오브젝트가 보라 · 금으로 일그러져 섞여 나옴: 화약통(1장) 1~2 · 멈춘 짐(2장) 1~2 · 거울 조각(3장) 2 · 태엽 스프링(4장) 1~2
// - 엄폐물(파편 바위 · 기억 더미 · 부러진 기둥) 내구 60. 모든 오브젝트는 몬스터 공격에도 발동
// 키트(Resources/Kit/Ch5)는 블렌더 + Codex 모델 (Art/tools/blender_kit_ch5_*.py, 명세 Art/tools/specs/kit_ch5_*_spec.md)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Ch5Kit
    {
        public static bool Is(ChapterDef ch) => ch != null && ch.Id == 5 && Kit.Has(ch, "RiftCrack");
        public static readonly Vector2 BossRoomSize = new(50, 48);          // 시간 감옥 한가운데 원형 지름 42m
        public const float ArenaR = 21f;
        public const float CoverHp = 60f;

        static int Stage => RunManager.Instance ? RunManager.Instance.stageIndex : 0;
        static bool BossRoom => RunManager.Instance && RunManager.Instance.IsBossRoom;
        static bool MidBossRoom => RunManager.Instance && RunManager.Instance.IsMidBossRoom;

        // ---------- 바닥 조각 ("None" = 건너뜀 → 반쯤 비움) ----------
        public static readonly string[] GroundBits = { "ShardBits", "StarDust", "CrackGlow", "None" };
        public static float[] GroundWeights() => Stage >= 2 ? new[] { 0.5f, 0.35f, 0.25f, 1.4f } : new[] { 0.45f, 0.3f, 0.1f, 1.6f };

        // ---------- 벽면 (Kit.BuildingRow가 씀) ----------
        public static (string[] names, float[] widths) Facades()
        {
            if (BossRoom || MidBossRoom) return (new[] { "Facade_StarArch", "Facade_Mirror", "Facade_ClockHeart" }, new[] { 6f, 6f, 8f });
            switch (Stage)
            {
                case 0: return (new[] { "Facade_Mirror", "Facade_StarArch", "Facade_Mirror", "Wall" }, new[] { 6f, 6f, 6f, 4f });
                case 1: return (new[] { "Facade_MemoryWest", "Facade_MemoryClock", "Facade_StarArch", "Facade_MemoryWest" }, new[] { 6f, 6f, 6f, 6f });
                case 2: return (new[] { "Facade_DebrisEdge", "Facade_StarArch", "Facade_DebrisEdge" }, new[] { 6f, 6f, 6f });
                default: return (new[] { "Facade_ClockHeart", "Facade_StarArch", "Facade_Mirror" }, new[] { 8f, 6f, 6f });
            }
        }

        /// <summary>잔해 섬 가장자리(밑으로 늘어진 바위)는 낮아서 남쪽 벽을 덜 낮춤</summary>
        public static float SouthWallMul() => Stage == 2 ? 0.9f : 0.62f;

        /// <summary>큰 지형지물 = 앞 장 기억 조각 (스테이지 장소에 맞게)</summary>
        public static string[] Landmarks()
        {
            switch (Stage)
            {
                case 0: return new[] { "LM_MirrorMonolith", "LM_DesertShard", "LM_Hourglass" };
                case 1: return new[] { "LM_SaloonFragment", "LM_TrainFragment", "LM_MirrorMonolith", "LM_Hourglass" };
                case 2: return new[] { "LM_DesertShard", "LM_TrainFragment", "LM_SaloonFragment" };
                default: return new[] { "LM_ClockHeart", "LM_Hourglass", "LM_MirrorMonolith" };
            }
        }

        // ---------- 소품 · 오브젝트 (RoomBuilder.KitProps 대신) ----------
        public static void Props(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            if (boss)
            {
                // 시간 감옥 한가운데 (2026-11-20 사용자: 맵이 밋밋함): 사각 바닥을 숨기고 깨진 거대 문자판 원판 + 바늘 첨탑 + 떠 있는 숫자 조각 + 아래 허공
                Ch2Kit.ArenaFence(root, ArenaR);
                BossArena(ch, info, rnd);
                return;
            }
            if (!MidBossRoom) { RiftField.Attach(info); FloorBreakup(ch, info, hw, hd, rnd); Dress(ch, info, hw, hd, rnd); }   // 방 기믹: 8초마다 고양이 근처 허공의 균열 + 깨지고 일그러진 바닥

            var spots = new List<Vector3>();
            for (float x = -hw + 3f; x <= hw - 3f; x += 3f)
                for (float z = -hd + 3f; z <= hd - 3f; z += 3f)
                {
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;            // 가운데 비움
                    if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;            // 문 앞 통로 비움
                    if (info.Blocked(info.center + new Vector3(x, 0, z), 1.6f)) continue;
                    spots.Add(new Vector3(x, 0, z));
                }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            int si = 0;
            Vector3 Next() => si < spots.Count ? spots[si++] + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.8f : Vector3.zero;

            if (MidBossRoom) return;                                          // 거울 대전: 오브젝트 없이 하얀 대전 (기믹은 중간 보스가)

            int kegs = RoomBuilder.Scaled(rnd.Next(1, 3), info);
            for (int k = 0; k < kegs && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "WarpedKeg", root, Next(), rnd.Next(360));
                if (go) PowderKeg.Make(go);
            }
            int trunks = RoomBuilder.Scaled(rnd.Next(1, 3), info);
            for (int k = 0; k < trunks && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "WarpedTrunk", root, Next() + Vector3.up * FlyingLuggage.Height, rnd.Next(360), 1f, false);
                if (go) FlyingLuggage.Make(go);
            }
            int springs = RoomBuilder.Scaled(rnd.Next(1, 3), info);
            for (int k = 0; k < springs && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "WarpedSpring", root, Next(), rnd.Next(360));
                if (go) WindSpring.Make(go);
            }
            MirrorShard.SpawnSet(ch, info, rnd, 2);                           // 거울 조각(3장) 2개: 표류 · 양쪽 반사 · 쏘면 15°

            string[] kinds = { "CoverShardRock", "CoverMemoryPile", "CoverBrokenColumn" };
            int cover = RoomBuilder.Scaled(rnd.Next(3, 6), info);
            for (int k = 0; k < cover && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, kinds[rnd.Next(kinds.Length)], root, Next(), rnd.Next(4) * 90f + rnd.Next(-12, 12));
                if (go) Breakable.Make(go, CoverHp);
            }
        }

        // ---------- 초현실 방 꾸미기 (2026-11-20 사용자: 다른 맵에 비해 너무 밋밋함 · 마을 건물처럼 왜곡 · 초현실주의) ----------
        // ① 떠다니는 기억 잔해: 머리 위 2.8~5.5m에서 천천히 돌며 둥실 (전투 시야를 안 가림, 그림자 · 깊이감) — 스테이지마다 다른 조합
        // ② 스테이지별 바닥 구조물: 0 거울 비석 줄 · 1 술집 · 기차 조각 섬 · 2 큰 허공 구멍 · 3 시계 심장 · 모래시계 · 바늘 첨탑
        // ③ 금 · 보라 점광원 4개
        static readonly string[][] FloatSets =
        {
            new[] { "FD_MirrorFrame", "FD_DesertPillar", "FD_Island", "FD_MirrorFrame" },
            new[] { "FD_Saloon", "FD_Rail", "FD_StationClock", "FD_Bell", "FD_Island" },
            new[] { "FD_Island", "FD_Island", "FD_DesertPillar", "FD_Rail", "FD_Saloon" },
            new[] { "FD_Gears", "FD_StationClock", "FD_Bell", "FD_Gears", "FD_Island" },
        };

        static void Dress(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd)
        {
            var root = info.root;
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            bool Free(Vector3 lp, float rad)
            {
                if (Mathf.Abs(lp.x) < 3f + rad || Mathf.Abs(lp.z) < 3f + rad) return false;          // 문 앞 십자 통로
                if (Mathf.Abs(lp.x) > hw - 1.5f - rad || Mathf.Abs(lp.z) > hd - 1.5f - rad) return false;
                return !info.Blocked(info.center + lp, rad);
            }
            bool Put(string name, Vector3 lp, float yaw, float scale, float rad, bool block = true)
            {
                if (!Free(lp, rad) || !Kit.Has(ch, name)) return false;
                if (!Kit.Place(ch, name, root, lp, yaw, scale)) return false;
                if (block) info.blocks.Add(new Rect(info.center.x + lp.x - rad, info.center.z + lp.z - rad, rad * 2, rad * 2));
                return true;
            }
            // ① 떠다니는 잔해
            var set = FloatSets[Mathf.Clamp(Stage, 0, 3)];
            int nFloat = RoomBuilder.Scaled(rnd.Next(7, 11), info);
            for (int k = 0, tries = 0; k < nFloat && tries < 60; tries++)
            {
                var name = set[rnd.Next(set.Length)];
                if (!Kit.Has(ch, name)) continue;
                var lp = new Vector3(R(-hw + 2f, hw - 2f), R(2.8f, 5.5f), R(-hd + 2f, hd - 2f));
                var go = Kit.Place(ch, name, root, lp, R(0, 360), R(0.8f, 1.35f), false);
                if (!go) continue;
                go.transform.localRotation = Quaternion.Euler(R(-25f, 25f), R(0f, 360f), R(-25f, 25f));
                FloatingDebris.Make(go, rnd);
                k++;
            }
            // ② 스테이지별 바닥 구조물
            switch (Stage)
            {
                case 0:   // 부서진 거울 문턱: 깨진 거울 비석이 긴 변을 따라 두 줄 (대성당 기둥처럼, 하나하나 기울고 비틀림)
                {
                    bool alongX = hw >= hd;
                    float half = alongX ? hw : hd, side = (alongX ? hd : hw) * 0.55f;
                    for (float t = -half + 5f; t <= half - 4.9f; t += R(5.5f, 7.5f))
                        foreach (float sg in new[] { -1f, 1f })
                        {
                            if (rnd.NextDouble() < 0.25) continue;
                            var lp = alongX ? new Vector3(t, 0, sg * side) : new Vector3(sg * side, 0, t);
                            Put("LM_MirrorMonolith", lp, R(0, 360), R(0.45f, 0.65f), 0.8f);
                        }
                    break;
                }
                case 1:   // 뒤섞인 기억 회랑: 술집 · 기차 조각이 귀퉁이 섬처럼
                    foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                    {
                        if (rnd.NextDouble() < 0.35) continue;
                        var lp = new Vector3(sx * (hw - 6f), 0, sz * (hd - 6f));
                        Put(rnd.NextDouble() < 0.5 ? "LM_SaloonFragment" : "LM_TrainFragment", lp, R(0, 360), R(0.6f, 0.8f), 2f);
                    }
                    break;
                case 2:   // 떠도는 잔해 섬: 큰 허공 구멍 (밟아도 됨, 자객 자리) 3~4개
                {
                    int pits = rnd.Next(3, 5);
                    for (int k = 0, tries = 0; k < pits && tries < 40; tries++)
                    {
                        var lp = new Vector3(R(-hw + 5f, hw - 5f), 0, R(-hd + 5f, hd - 5f));
                        if (!Free(lp, 2.4f)) continue;
                        if (Kit.Place(ch, "FloorVoidPit", root, lp, R(0, 360), R(1.6f, 2.3f), false)) { RiftSpots.Add(info.center + lp, 9999f); k++; }
                    }
                    break;
                }
                default:  // 무너지는 시계 심장: 시계 심장 · 모래시계 · 땅에 꽂힌 바늘 첨탑
                    Put("LM_ClockHeart", new Vector3(rnd.NextDouble() < 0.5 ? -hw + 7f : hw - 7f, 0, R(-hd + 7f, hd - 7f)), R(0, 360), 0.75f, 2.2f);
                    for (int k = 0; k < 2; k++) Put("LM_Hourglass", new Vector3(R(-hw + 5f, hw - 5f), 0, R(-hd + 5f, hd - 5f)), R(0, 360), 0.7f, 1f);
                    Put("BA_HandSpire", new Vector3(rnd.NextDouble() < 0.5 ? -hw + 4f : hw - 4f, 0, rnd.NextDouble() < 0.5 ? -hd + 4f : hd - 4f), R(0, 360), 0.55f, 1f);
                    break;
            }
            // ③ 금 · 보라 점광원 (균열에서 새는 빛)
            for (int k = 0; k < 4; k++)
            {
                var lg = new GameObject("RiftLight");
                lg.transform.SetParent(root, false);
                lg.transform.localPosition = new Vector3(R(-hw + 4f, hw - 4f), 1.4f, R(-hd + 4f, hd - 4f));
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = R(7f, 10f); l.intensity = R(1.2f, 2f);
                l.color = k % 2 == 0 ? new Color(1f, 0.76f, 0.35f) : new Color(0.62f, 0.4f, 1f);
                l.shadows = LightShadows.None;
            }
        }

        /// <summary>보스 방: 사각 바닥 · 바닥 판을 숨기고 깨진 거대 문자판(반지름 21m) + 아래 허공 + 바늘 첨탑 3 + 떠 있는 숫자 조각 · 잔해 (경계 밖)</summary>
        static void BossArena(ChapterDef ch, RoomBuilder.RoomInfo info, System.Random rnd)
        {
            var root = info.root;
            if (!Kit.Has(ch, "BA_DialRing")) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Floor" || r.name == "Ground" || r.transform.parent && r.transform.parent.name == "GroundBits") r.enabled = false;
            Kit.Place(ch, "BA_DialRing", root, new Vector3(0, 0.02f, 0), 0f, 1f, false);
            // 아래 허공 (짙은 남보라 판 + 별가루)
            var voidPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.Destroy(voidPlane.GetComponent<Collider>());
            voidPlane.name = "Void";
            voidPlane.transform.SetParent(root, false);
            voidPlane.transform.localPosition = new Vector3(0, -2.5f, 0);
            voidPlane.transform.localScale = Vector3.one * 14f;
            var vm = new Material(Shader.Find("Universal Render Pipeline/Unlit")); vm.color = new Color(0.035f, 0.03f, 0.1f);
            voidPlane.GetComponent<Renderer>().sharedMaterial = vm;
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int k = 0; k < 40; k++)
            {
                float a = R(0, 360) * Mathf.Deg2Rad, d = R(22f, 34f);
                Kit.Place(ch, "StarDust", root, new Vector3(Mathf.Sin(a) * d, -2.4f, Mathf.Cos(a) * d), R(0, 360), R(0.8f, 1.6f), false);
            }
            for (int k = 0; k < 3; k++)
            {
                float a = (k * 120f + R(-20f, 20f)) * Mathf.Deg2Rad;
                Kit.Place(ch, "BA_HandSpire", root, new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 19.5f, R(0, 360), R(0.8f, 1f), false);
            }
            for (int k = 0; k < 8; k++)
            {
                float a = (k * 45f + R(-15f, 15f)) * Mathf.Deg2Rad;
                var name = k % 2 == 0 ? "BA_FloatingNumeral" : FloatSets[3][rnd.Next(FloatSets[3].Length)];
                var go = Kit.Place(ch, name, root, new Vector3(Mathf.Sin(a) * R(23f, 27f), R(1.5f, 5f), Mathf.Cos(a) * R(23f, 27f)), R(0, 360), R(1f, 1.5f), false);
                if (go) { go.transform.localRotation = Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f)); FloatingDebris.Make(go, rnd); }
            }
        }

        // ---------- 깨지고 일그러진 바닥 (2026-11-19 사용자: 4장 바닥 재활용 말고 훨씬 깨지고 일그러지게 + 이전 장 바닥이 일그러진 균열로 섞이게,
        //            깨진 부분 이음매는 사각형 · 원 같은 균일한 도형 말고 불규칙하고 일그러진 모양 → 블렌더 조각 자체가 불규칙 윤곽, 여기선 방향 · 크기 · 기울기도 제각각) ----------
        static readonly string[] OldGrounds = { "Kit/Ch1/Ground", "Kit/Ch2/Ground", "Kit/Ch3/Ground", "Kit/Ch4/Ground" };
        static readonly Dictionary<string, Material> oldGroundCache = new();

        static Material OldGround(System.Random rnd)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                var path = OldGrounds[rnd.Next(OldGrounds.Length)];
                if (!oldGroundCache.TryGetValue(path, out var m)) oldGroundCache[path] = m = Resources.Load<Material>(path);
                if (m) return m;
            }
            return null;
        }

        static void FloorBreakup(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd)
        {
            if (!Kit.Has(ch, "FloorPatch_A")) return;
            var root = info.root;
            bool Free(Vector3 lp, float rad)
            {
                if (Mathf.Abs(lp.x) < 2.6f + rad * 0.5f || Mathf.Abs(lp.z) < 2.6f + rad * 0.5f) return false;   // 문 앞 십자 통로
                if (Mathf.Abs(lp.x) > hw - 1.2f - rad || Mathf.Abs(lp.z) > hd - 1.2f - rad) return false;
                return !info.Blocked(info.center + lp, rad);
            }
            Vector3 Spot() => new(((float)rnd.NextDouble() * 2 - 1) * (hw - 2f), 0, ((float)rnd.NextDouble() * 2 - 1) * (hd - 2f));
            float Yaw() => (float)rnd.NextDouble() * 360f;
            float Sc(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            int Count(int a, int b) => RoomBuilder.Scaled(rnd.Next(a, b + 1), info);

            // 이전 장 바닥 조각 (걸어 다님, 충돌 없음): 윗면을 1~4장 바닥 재질로 — 크기 · 방향 · 좌우 뒤집기 제각각이라 같은 모양이 안 읽힘
            string[] patches = { "FloorPatch_A", "FloorPatch_B", "FloorPatch_C" };
            int nPatch = 0;   // 2026-11-20 천문대 재구성: 앞 장 바닥(체커 · 판자 · 역 타일)을 얹은 조각은 컨셉과 안 맞아 끔 (예전: Count(3, 6))
            for (int k = 0, tries = 0; k < nPatch && tries < 40; tries++)
            {
                var lp = Spot();
                if (!Free(lp, 1.4f)) continue;
                var go = Kit.Place(ch, patches[rnd.Next(patches.Length)], root, lp, Yaw(), Sc(0.8f, 1.35f), false);
                if (!go) continue;
                if (rnd.NextDouble() < 0.5) { var s = go.transform.localScale; go.transform.localScale = new Vector3(-s.x, s.y, s.z * Sc(0.75f, 1.25f)); }   // 뒤집고 한쪽으로 늘여 일그러짐
                var mat = OldGround(rnd);
                if (mat)
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++) if (mats[i] && mats[i].name.StartsWith("Patch_Top")) mats[i] = mat;
                        r.sharedMaterials = mats;
                    }
                k++;
            }
            // 허공 구멍 · 바닥 융기 (걸어 다님)
            int nPit = Count(1, 3);
            for (int k = 0, tries = 0; k < nPit && tries < 30; tries++)
            {
                var lp = Spot();
                if (!Free(lp, 1.2f)) continue;
                if (Kit.Place(ch, "FloorVoidPit", root, lp, Yaw(), Sc(0.8f, 1.4f), false)) { RiftSpots.Add(info.center + lp, 9999f); k++; }   // 허공 구멍 = 자객이 드나드는 자리
            }
            int nRidge = Count(1, 2);
            for (int k = 0, tries = 0; k < nRidge && tries < 30; tries++)
            {
                var lp = Spot();
                if (!Free(lp, 1.5f)) continue;
                if (Kit.Place(ch, "FloorWarpRidge", root, lp, Yaw(), Sc(0.85f, 1.4f), false)) k++;
            }
            // 기울어 솟은 판석 · 거울 파편 무더기 (막힘: 적 생성 · 소품이 피함)
            foreach (var (name, rad, n) in new[] { ("FloorSlabTilted", 1.2f, Count(0, 2)), ("FloorShardSpike", 0.8f, Count(1, 2)) })
                for (int k = 0, tries = 0; k < n && tries < 30; tries++)
                {
                    var lp = Spot();
                    if (!Free(lp, rad + 0.5f)) continue;
                    if (!Kit.Place(ch, name, root, lp, Yaw(), Sc(0.85f, 1.3f))) continue;
                    info.blocks.Add(new Rect(info.center.x + lp.x - rad, info.center.z + lp.z - rad, rad * 2, rad * 2));
                    k++;
                }
        }
    }

    /// <summary>균열 자객이 숨었다 나올 수 있는 자리 (방 균열 · 깨진 흉터 · 거인 내려찍기 균열). 자리마다 끝나는 시각</summary>
    public static class RiftSpots
    {
        static readonly List<(Vector3 p, float until)> spots = new();

        public static void Add(Vector3 at, float life) { at.y = 0; spots.Add((at, Time.time + life)); }

        static void Prune() => spots.RemoveAll(s => s.until < Time.time);

        public static int Count { get { Prune(); return spots.Count; } }

        /// <summary>to에서 within 안에 있는 가장 가까운 자리 (except와 1m 안인 자리는 뺌)</summary>
        public static bool Nearest(Vector3 to, float within, List<Vector3> except, out Vector3 best)
        {
            Prune();
            best = Vector3.zero; float bd = within; bool found = false;
            foreach (var (p, _) in spots)
            {
                bool skip = false;
                if (except != null) foreach (var e in except) if (Effects.Flat(e - p).magnitude < 1f) { skip = true; break; }
                if (skip) continue;
                float d = Effects.Flat(p - to).magnitude;
                if (d < bd) { bd = d; best = p; found = true; }
            }
            return found;
        }
    }

    /// <summary>
    /// 허공의 균열 (5장 방 기믹 · 균열술사): 허공에 금 간 균열이 커지며 떨리다(예고 1.5초) 깨짐 → 반경 2.5m 유리 조각 타격 (적 30 · 고양이 15).
    /// 깨진 자리에 6초 동안 흉터(CrackGlow)가 남음. 겉모습 = Kit/Ch5/RiftCrack (블렌더), 깨짐 = 구운 플립북 bk_c5rift → Codex 시트 cx_c5riftshatter
    /// </summary>
    public class Rift : MonoBehaviour
    {
        public const float Warn = 1.5f, Radius = 2.5f, EnemyDmg = 30f, PlayerDmg = 15f, ScarLife = 6f;
        float warn, born; Vector3 at, baseScale; bool done;

        public static Rift Spawn(Vector3 at, float warn)
        {
            at.y = 0;
            var ch = RunManager.Instance ? RunManager.Instance.Chapter : null;
            var pf = ch != null ? Kit.Prefab(ch, "RiftCrack") : null;
            var go = pf ? Instantiate(pf) : new GameObject("Rift");
            go.name = "Rift";
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            var r = go.AddComponent<Rift>();
            r.warn = Mathf.Max(0.05f, warn); r.born = Time.time; r.at = at; r.baseScale = go.transform.localScale;
            go.transform.localScale = r.baseScale * 0.25f;
            RiftSpots.Add(at, r.warn + ScarLife);
            if (warn > 0.1f)
            {
                Telegraph.Ring(at, Radius, r.warn, Telegraph.Danger).Grow();
                Sfx.Play("e5_rift_open", 0.4f, 0.06f, 3);
            }
            return r;
        }

        /// <summary>균열 자리가 warn초 빛남 (자객이 튀어나올 예고)</summary>
        public static void Glow(Vector3 at, float warn)
        {
            at.y = 0;
            Telegraph.Ring(at, 0.9f, warn, Telegraph.Danger).NoGlint();
            Enemy.Fx5("c5spark", at + Vector3.up * 0.4f, 0.8f, Mathf.Min(0.35f, warn));
            Scar(at, warn + 0.2f);
        }

        /// <summary>깨진 자리에 남는 균열 흉터 (바닥에 붙은 금빛 금, life초 뒤 사라짐)</summary>
        public static void Scar(Vector3 at, float life)
        {
            at.y = 0.02f;
            var ch = RunManager.Instance ? RunManager.Instance.Chapter : null;
            var pf = ch != null ? Kit.Prefab(ch, "CrackGlow") : null;
            if (!pf) return;
            var go = Instantiate(pf, at, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.transform.localScale *= 0.8f;
            Destroy(go, life);
        }

        void Update()
        {
            if (done) return;
            float t = (Time.time - born) / warn;
            float k = Mathf.Clamp01(t);
            // 금이 커지며 떨림 (끝으로 갈수록 빠르게)
            transform.localScale = baseScale * Mathf.Lerp(0.25f, 1f, 1 - (1 - k) * (1 - k));
            transform.position = at + Random.insideUnitSphere * 0.04f * k;
            if (t < 1f) return;
            done = true;
            Shatter();
        }

        void Shatter()
        {
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= Radius) p.health.TakeDamage(PlayerDmg);
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && en.kind != Enemy.Kind.Boss && Effects.Flat(en.transform.position - at).magnitude < Radius) en.health.TakeDamage(EnemyDmg);
            PropHit.Area(at, Radius, EnemyDmg);
            Enemy.Fx5("c5rift", at + Vector3.up * 1.1f, 3.2f, 0.45f);             // Codex 균열 깨짐 + 블렌더 균열
            Enemy.Fx5("c5spark", at + Vector3.up * 0.9f, 2.2f, 0.35f);
            StylizedFx.Shockwave(at, Radius, StylizedFx.Glass, 0.4f);              // 스타일 VFX: 유리 금 그물 충격파 + 자홍 가시 폭발
            StylizedFx.Burst(at + Vector3.up * 1.1f, 2.4f, StylizedFx.Magenta);
            Sfx.Play("e5_rift_shatter", 0.6f, 0.06f, 3);
            CameraFollow.Shake(0.07f);
            Scar(at, ScarLife);
            Destroy(gameObject);
        }
    }

    /// <summary>방 기믹 타이머: 이 방에서 전투 중이면 8초마다(첫 번째는 4초 뒤) 고양이 근처 1.5~3.5m 허공에 균열</summary>
    public class RiftField : MonoBehaviour
    {
        public const float Every = 8f, First = 4f;
        RoomBuilder.RoomInfo room;
        float next = -1f;

        public static void Attach(RoomBuilder.RoomInfo info)
        {
            var f = info.root.gameObject.AddComponent<RiftField>();
            f.room = info;
        }

        void Update()
        {
            var rm = RunManager.Instance;
            bool live = rm && rm.CurrentRoom == room && rm.inCombat && !rm.runOver;
            if (!live) { next = -1f; return; }
            if (next < 0) next = Time.time + First;
            if (Time.time < next) return;
            next = Time.time + Every;
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive) return;
            var at = p.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(1.5f, 3.5f);
            at = rm.ClampToRoom(at, 1.2f);
            Rift.Spawn(at, Rift.Warn);
        }
    }
}

namespace CatTang
{
    /// <summary>
    /// 균열 장판 (5장 전용, 붕괴 거인 내려찍기 · 거울 허상 강화형): 1장 녹은 웅덩이를 빌리지 않고 깨진 바닥 금(Kit/Ch5/CrackGlow, 블렌더)이 갈라지며 퍼짐.
    /// 안에 들어오면 느려지고, 테두리에서 자홍 불씨가 튐. 이음매는 불규칙: 금 조각 2~3장을 크기 · 방향 · 늘임을 달리해 겹침. 균열 자객이 드나드는 자리(RiftSpots)
    /// </summary>
    public class RiftScarZone : MonoBehaviour
    {
        float radius, slow, until, born, nextSpark;
        readonly List<(Transform t, Vector3 full)> pieces = new();

        public static void Spawn(Vector3 at, float radius, float life, float slow)
        {
            at.y = 0.02f;
            var go = new GameObject("RiftScarZone");
            go.transform.position = at;
            var z = go.AddComponent<RiftScarZone>();
            z.radius = radius; z.slow = slow; z.born = Time.time; z.until = Time.time + life;
            var ch = RunManager.Instance ? RunManager.Instance.Chapter : null;
            var pf = ch != null ? Kit.Prefab(ch, "CrackGlow") : null;
            int n = pf ? Random.Range(2, 4) : 0;
            for (int i = 0; i < n; i++)
            {
                var c = Instantiate(pf, go.transform);
                foreach (var col in c.GetComponentsInChildren<Collider>()) Destroy(col);
                c.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), i * 0.004f, Random.Range(-0.4f, 0.4f)) * radius;
                c.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                float s = radius / 1.5f * Random.Range(0.7f, 1.15f);
                var full = new Vector3(s * Random.Range(0.75f, 1.35f), 1f, s * Random.Range(0.6f, 1.2f));   // 한쪽으로 늘여 일그러짐
                c.transform.localScale = Vector3.zero;
                z.pieces.Add((c.transform, full));
            }
            RiftSpots.Add(at, life);
            // 장판 범위가 읽히게: 테두리만 남는 보랏빛 연기 고리 (스타일 VFX, 나이 문턱을 높게 = 성긴 결)
            StylizedFx.Spawn("ring", at + Vector3.up * 0.04f, Quaternion.identity, Vector3.one * radius * 0.5f, Vector3.one * radius, 0.5f, StylizedFx.Void, 25f, Mathf.Max(0f, life - 0.5f), 0.42f);
        }

        void Update()
        {
            float age = Time.time - born, left = until - Time.time;
            // 갈라지며 퍼짐 (0.25초) → 끝 0.5초에 오그라들며 사라짐
            float k = Mathf.Clamp01(age / 0.25f) * Mathf.Clamp01(left / 0.5f);
            foreach (var (t, full) in pieces) if (t) t.localScale = full * (1 - (1 - k) * (1 - k));
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude <= radius) p.health.ApplySlow(slow, 0.15f);
            if (Time.time >= nextSpark)
            {
                nextSpark = Time.time + Random.Range(0.25f, 0.5f);
                var at = transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * radius * Random.Range(0.5f, 1f);
                FxBurst.Spawn(at + Vector3.up * 0.15f, 0.5f, FxBurst.Kind.Spark, Enemy.Ch5Magenta);
            }
            if (left <= 0) Destroy(gameObject);
        }
    }
}

namespace CatTang
{
    /// <summary>
    /// 허공 파동 (5장 전용 물결 고리, 2026-11-20 사용자: 물결 고리 아쉬움 — 3장 선 고리 대신): 블렌더 벽 고리 메시(fx5_ring_tall)가
    /// 일정한 속도로 퍼지는 자홍 빛의 벽 + 안쪽에 옅은 보라 연기 띠. 판정은 벽이 지나가는 순간 그 띠(폭 width) 안이면 한 번 (대시로 넘음)
    /// </summary>
    public class VoidRipple : MonoBehaviour
    {
        float r, speed, maxR, width, dmg; bool hit; Transform wall, smoke; Renderer wallR, smokeR; MaterialPropertyBlock mpb;

        public static void Spawn(Vector3 at, float speed, float dmg, float maxR, float width, float startR = 1f)
        {
            at.y = 0.02f;
            var go = new GameObject("VoidRipple");
            go.transform.position = at;
            var v = go.AddComponent<VoidRipple>();
            v.r = startR; v.speed = speed; v.maxR = maxR; v.width = width; v.dmg = dmg;
            var w = StylizedFx.Spawn("ring_tall", at, Quaternion.identity, Vector3.one, Vector3.one, 999f, StylizedFx.Magenta.With(scale: new Vector2(4f, 3f), scroll: new Vector2(-1.5f, 0.4f), taperV: 0.3f), 0f, 999f, 0.12f);
            var s = StylizedFx.Spawn("ring", at + Vector3.up * 0.03f, Quaternion.identity, Vector3.one, Vector3.one, 999f, StylizedFx.Void.With(scale: new Vector2(6f, 1.5f), scroll: new Vector2(1f, 0)), 0f, 999f, 0.35f);
            if (w) { v.wall = w.transform; w.enabled = false; v.wallR = w.GetComponent<Renderer>(); }
            if (s) { v.smoke = s.transform; s.enabled = false; v.smokeR = s.GetComponent<Renderer>(); }
            v.mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            r += speed * Time.deltaTime;
            float k = Mathf.Clamp01(r / maxR);
            if (wall) { wall.localScale = new Vector3(r, 1.1f * (1 - k * 0.5f), r); wall.Rotate(0, 40f * Time.deltaTime, 0); }
            if (smoke) smoke.localScale = Vector3.one * r;
            // 끝으로 갈수록 구멍이 뚫리며 흩어짐
            foreach (var rr in new[] { wallR, smokeR })
            {
                if (!rr) continue;
                rr.GetPropertyBlock(mpb);
                mpb.SetFloat("_Age", Mathf.Lerp(0.12f, 0.95f, k * k));
                rr.SetPropertyBlock(mpb);
            }
            var p = PlayerController.Instance;
            if (!hit && dmg > 0 && p && p.health.Alive)
            {
                float d = Effects.Flat(p.transform.position - transform.position).magnitude;
                if (Mathf.Abs(d - r) <= width * 0.5f) { hit = true; p.health.TakeDamage(dmg); }
            }
            if (r >= maxR) { if (wall) Destroy(wall.gameObject); if (smoke) Destroy(smoke.gameObject); Destroy(gameObject); }
        }
    }
}

namespace CatTang
{
    /// <summary>떠다니는 잔해: 제자리에서 천천히 둥실 (위아래 0.3m) · 천천히 돎 (초현실 방 꾸미기)</summary>
    public class FloatingDebris : MonoBehaviour
    {
        Vector3 basePos; Quaternion baseRot; float phase, spin, bob, tilt;
        public static void Make(GameObject go, System.Random rnd)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var f = go.AddComponent<FloatingDebris>();
            f.basePos = go.transform.localPosition; f.baseRot = go.transform.localRotation;
            f.phase = (float)rnd.NextDouble() * 10f; f.spin = (float)(rnd.NextDouble() * 2 - 1) * 9f; f.bob = 0.2f + (float)rnd.NextDouble() * 0.2f; f.tilt = 3f + (float)rnd.NextDouble() * 4f;
        }
        void Update()
        {
            float t = Time.time + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * 0.7f) * bob;
            transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.5f) * tilt, spin * t, Mathf.Cos(t * 0.43f) * tilt) * baseRot;
        }
    }

    /// <summary>떠오르는 부스러기 (2026-11-20 사용자: 마을처럼 파티클로 분리되어 상승하는 부스러기 — 벽과 중간중간 오브젝트에도):
    /// 방을 다 지은 뒤 벽(약 30%) · 지형지물(70%) · 엄폐물 · 잔해(35%) 꼭대기에 FX_Rise를 붙임. 방마다 최대 12곳</summary>
    public static class Ch5Rise
    {
        static GameObject fx;
        public static void Scatter(RoomBuilder.RoomInfo info, System.Random rnd)
        {
            if (!fx) fx = Resources.Load<GameObject>("Kit/Ch5/FX_Rise");
            if (!fx || !info.root) return;
            int made = 0;
            foreach (var t in info.root.GetComponentsInChildren<Transform>())
            {
                if (made >= 12) break;
                string n = t.name;
                double chance = n.StartsWith("Facade_") || n == "Wall" ? 0.3 : n.StartsWith("LM_") ? 0.7 : n.StartsWith("Cover") || n.StartsWith("Warped") || n.StartsWith("FD_") ? 0.35 : 0;
                if (chance <= 0 || rnd.NextDouble() > chance) continue;
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var at = new Vector3(Mathf.Lerp(b.min.x, b.max.x, 0.25f + (float)rnd.NextDouble() * 0.5f), b.max.y - 0.15f, Mathf.Lerp(b.min.z, b.max.z, 0.25f + (float)rnd.NextDouble() * 0.5f));
                var go = Object.Instantiate(fx, t, true);
                go.transform.position = at;
                go.transform.rotation = Quaternion.identity;
                made++;
            }
        }
    }
}
