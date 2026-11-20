// 4장 (시계 장치 속) 방 꾸미기 + 방 기믹 · 오브젝트 (docs/Cat_Tang_레벨_디자인.md 챕터 4 · 수치 기준 (챕터 4))
// - 장소(스테이지)별 벽면: 1 태엽 창고 → 2 톱니 기관실 → 3 종루 → 4 문자판 뒤 회랑 (아래 → 위로 오를수록 별밤이 더 보임)
// - 돌아가는 톱니 바닥(방 기믹): 보스 · 중간 보스 방 빼고 방마다 2~3개, 지름 6~9m, 초당 20°(이웃 톱니는 반대 방향),
//   올라선 고양이 · 적이 함께 실려 돎, 맞물린 이빨 자리(폭 1.5m)에 있으면 0.5초마다 피해 10 + 바깥으로 밀려남,
//   맞물린 자리는 늘 불똥이 튀고 끼면 큰 불똥 폭발 (축을 쏴서 방향 바꾸기는 2026-11-19 뺌)
// - 감긴 태엽 스프링: 방마다 2~4, 쏘면 0.4초 떨다가 반경 3m 폭발 · 적 60 · 고양이 20
// - 굴러가는 톱니바퀴: 방마다 1~2, 쏘면 맞은 방향으로 14m/초 굴러가 적을 치고(60) 벽에 한 번 튕긴 뒤 부서짐
// - 낡은 종: 방마다 1~2, 쏘면 음파 링 반경 5m — 적 30 + 범위 안 적 탄을 지움, 다시 쓰려면 8초
// - 엄폐물(톱니 상자 · 부서진 시계 기둥 · 태엽 통): 내구 50. 스프링 · 톱니바퀴 · 종은 몬스터 공격에도 발동(PropHit · 적 탄)
// 색 (2026-11-19 사용자): 느와르 흑백 대비, 적 공격 궤적은 빨간 계열 → 고양이 쪽 효과(종 링 등)는 흰색
// 키트(Resources/Kit/Ch4)는 블렌더 + Codex 모델 (Art/tools/blender_kit_ch4_*.py, 명세 Art/tools/specs/kit_ch4_*_spec.md)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Ch4Kit
    {
        public static bool Is(ChapterDef ch) => ch != null && ch.Id == 4 && Kit.Has(ch, "GearFloor");
        public static readonly Vector2 BossRoomSize = new(48, 46);          // 꼭대기 문자판 원형 지름 40m
        public const float ArenaR = 20f;
        public const float CoverHp = 50f;

        static int Stage => RunManager.Instance ? RunManager.Instance.stageIndex : 0;
        static bool BossRoom => RunManager.Instance && RunManager.Instance.IsBossRoom;
        static bool MidBossRoom => RunManager.Instance && RunManager.Instance.IsMidBossRoom;

        // ---------- 바닥 조각 ----------
        // 2026-11-19 사용자: 공장 느낌 → 신전 느낌. 톱니 · 나사 · 철창 잔해를 크게 줄임 ("None" = 없는 프리팹이라 그 차례는 건너뜀 → 약 70%는 비움)
        public static readonly string[] GroundBits = { "CogBits", "ScrewBits", "FloorGrate", "None" };
        public static float[] GroundWeights() => Stage >= 3 ? new[] { 0.35f, 0.15f, 0.4f, 2.2f } : new[] { 0.5f, 0.25f, 0.15f, 2.2f };

        // ---------- 벽면 (Kit.BuildingRow가 씀) ----------
        public static (string[] names, float[] widths) Facades()
        {
            // 2026-11-19 사용자: 공장 느낌 → 기획서 컨셉(아치 회랑 · 기둥 · 붉은 깃발 · 별밤)처럼 신전 느낌: 아치 회랑 · 장미창 문자판 벽이 주, 기계 벽은 가끔
            if (BossRoom || MidBossRoom) return (new[] { "Facade_Colonnade", "Facade_Pendulum", "Facade_RoseDial", "Facade_DialBack" }, new[] { 6f, 6f, 6f, 8f });
            switch (Stage)
            {
                case 0: return (new[] { "Facade_Colonnade", "Facade_RoseDial", "Facade_Colonnade", "Wall" }, new[] { 6f, 6f, 6f, 4f });
                case 1: return (new[] { "Facade_Colonnade", "Facade_Gears", "Facade_RoseDial", "Facade_Pendulum" }, new[] { 6f, 6f, 6f, 6f });
                case 2: return (new[] { "Facade_BellArch", "Facade_Colonnade", "Facade_RoseDial", "Facade_Colonnade" }, new[] { 6f, 6f, 6f, 6f });
                default: return (new[] { "Facade_DialBack", "Facade_Colonnade", "Facade_Colonnade" }, new[] { 8f, 6f, 6f });
            }
        }

        /// <summary>문자판 뒤 회랑(철골 난간)은 키가 낮아 남쪽 벽을 덜 낮춤</summary>
        public static float SouthWallMul() => Stage >= 3 ? 0.85f : 0.6f;

        /// <summary>큰 지형지물 (블렌더 + Codex, RoomBuilder.AddShape가 프리팹 경계로 잼): 스테이지 장소에 맞게
        /// 스테이지 벽면(Facades)과 같은 장소: 0 태엽 창고 · 1 톱니 기관실/진자 · 2 종루 · 3 문자판 뒤 회랑</summary>
        public static string[] Landmarks()
        {
            switch (Stage)
            {
                // 신전 느낌: 시계 제단 · 무너진 아치가 주, 기계 덩어리(시계 기계 · 톱니 더미)는 기관실에만
                case 0: return new[] { "LM_ClockAltar", "LM_ArchRuin", "LM_PendulumCase" };
                case 1: return new[] { "LM_ArchRuin", "LM_ClockAltar", "LM_GearStack", "LM_Clockwork" };
                case 2: return new[] { "LM_BellFrame", "LM_ArchRuin", "LM_ClockAltar" };
                default: return new[] { "LM_DialFragment", "LM_ArchRuin", "LM_ClockAltar" };
            }
        }

        // ---------- 소품 · 오브젝트 (RoomBuilder.KitProps 대신) ----------
        public static void Props(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            if (boss)
            {
                // 꼭대기 문자판: 원형 경계(밖은 별밤 허공)만. 문자판 바닥 · 쌍둥이 기믹은 보스(Ch4Bosses)가 맡음
                Ch2Kit.ArenaFence(root, ArenaR);
                // 은빛 거대 문자판 바닥 (2026-11-20)
                Kit.Place(ch, "BossFloor", root, new Vector3(0, 0.02f, 0), 0f, 1f, false);
                return;
            }

            // 돌아가는 톱니 바닥 2~3개: 첫 톱니에 이어 붙여 서로 맞물리게 (이웃 = 반대 방향)
            var gears = new List<(Vector3 p, float r)>();
            if (!MidBossRoom && Kit.Has(ch, "GearFloor"))
            {
                int want = RoomBuilder.Density(info) >= 1.5f ? 3 : rnd.Next(2, 4);
                for (int tries = 0; tries < 60 && gears.Count < want; tries++)
                {
                    float r = 3f + (float)rnd.NextDouble() * 1.5f;                  // 지름 6~9m
                    Vector3 p;
                    if (gears.Count == 0) p = new Vector3(((float)rnd.NextDouble() - 0.5f) * (hw - r - 3f), 0, ((float)rnd.NextDouble() - 0.5f) * (hd - r - 3f));
                    else
                    {
                        var prev = gears[rnd.Next(gears.Count)];
                        float a = (float)rnd.NextDouble() * Mathf.PI * 2;
                        p = prev.p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (prev.r + r - 0.15f);   // 이빨이 살짝 겹치게
                    }
                    if (Mathf.Abs(p.x) > hw - r - 2f || Mathf.Abs(p.z) > hd - r - 2f) continue;
                    bool overlap = false;
                    foreach (var g in gears) if (Effects.Flat(g.p - p).magnitude < g.r + r - 0.4f) overlap = true;
                    if (overlap || info.Blocked(info.center + p, r)) continue;
                    gears.Add((p, r));
                }
                for (int i = 0; i < gears.Count; i++)
                {
                    var go = Kit.Place(ch, "GearFloor", root, gears[i].p, rnd.Next(360), gears[i].r / 4f, false);
                    if (go) GearFloor.Make(go, gears[i].r, i % 2 == 0 ? 1 : -1);
                }
            }

            TempleInterior(ch, info, hw, hd, rnd, gears);   // 넓은 방 가운데를 채우는 신전 가구 (기둥 줄 · 석상 · 성물함 · 랜턴 · 바닥 문자판)

            // 후보 자리 (가운데 · 문 앞 통로 · 막힌 곳 · 톱니 바닥 위는 비움)
            var spots = new List<Vector3>();
            for (float x = -hw + 3f; x <= hw - 3f; x += 3f)
                for (float z = -hd + 3f; z <= hd - 3f; z += 3f)
                {
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;
                    if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;
                    var lp = new Vector3(x, 0, z);
                    bool onGear = false;
                    foreach (var g in gears) if (Effects.Flat(g.p - lp).magnitude < g.r + 1.2f) onGear = true;
                    if (onGear || info.Blocked(info.center + lp, 1.6f)) continue;
                    spots.Add(lp);
                }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            int si = 0;
            Vector3 Next() => si < spots.Count ? spots[si++] + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.8f : Vector3.zero;

            int springs = RoomBuilder.Scaled(rnd.Next(2, 5), info);
            for (int k = 0; k < springs && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "WindSpring", root, Next(), rnd.Next(360));
                if (go) WindSpring.Make(go);
            }
            int wheels = RoomBuilder.Scaled(rnd.Next(1, 3), info);
            for (int k = 0; k < wheels && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "GearWheel", root, Next(), rnd.Next(360));
                if (go) GearWheel.Make(go);
            }
            int bells = RoomBuilder.Scaled(rnd.Next(1, 3), info);
            for (int k = 0; k < bells && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "OldBell", root, Next(), rnd.Next(4) * 90f);
                if (go) OldBell.Make(go);
            }
            // 엄폐물: 신전 의자(Deco_Pew)를 섞어 공장 상자만 나오지 않게
            string[] kinds = Kit.Has(ch, "Deco_Pew") ? new[] { "Deco_Pew", "Deco_Pew", "BrokenClockPillar", "GearCrate", "SpringBarrel" } : new[] { "GearCrate", "BrokenClockPillar", "SpringBarrel" };
            int cover = RoomBuilder.Scaled(rnd.Next(3, 6), info);
            for (int k = 0; k < cover && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, kinds[rnd.Next(kinds.Length)], root, Next(), rnd.Next(4) * 90f + rnd.Next(-12, 12));
                if (go) Breakable.Make(go, CoverHp);
            }
            Temple(ch, info, hw, hd, rnd, gears);
        }

        /// <summary>방 안 신전 가구 (2026-11-19 사용자: 맵이 넓으니 오브젝트를 더 넣어 느낌 살리기). 큰 것(기둥 · 석상 · 성물함)은 막힌 곳으로 등록해
        /// 적 생성 · 이동 · 다른 소품이 피함. 문 앞 통로(가운데 십자 ±3m) · 톱니 바닥은 비움</summary>
        static void TempleInterior(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, List<(Vector3 p, float r)> gears)
        {
            if (!Kit.Has(ch, "Deco_NaveColumn")) return;
            var root = info.root;
            bool Free(Vector3 lp, float rad)
            {
                if (Mathf.Abs(lp.x) < 3f + rad || Mathf.Abs(lp.z) < 3f + rad) return false;          // 문 앞 십자 통로
                if (Mathf.Abs(lp.x) > hw - 1.5f - rad || Mathf.Abs(lp.z) > hd - 1.5f - rad) return false;
                foreach (var g in gears) if (Effects.Flat(g.p - lp).magnitude < g.r + rad + 0.8f) return false;
                return !info.Blocked(info.center + lp, rad);
            }
            void Block(Vector3 lp, float w, float d) => info.blocks.Add(new Rect(info.center.x + lp.x - w / 2, info.center.z + lp.z - d / 2, w, d));
            bool Put(string name, Vector3 lp, float yaw, float rad, bool block = true)
            {
                if (!Free(lp, rad)) return false;
                if (!Kit.Place(ch, name, root, lp, yaw)) return false;
                if (block) Block(lp, rad * 2, rad * 2);
                return true;
            }

            // 바닥 대형 문자판 상감(Deco_FloorDial)은 뺌 (2026-11-19 사용자: 바닥면 시계가 이상함) — 프리팹은 남아 있음

            // 회중석 기둥 줄 (방의 60%): 긴 변을 따라 양쪽에 한 줄씩, 6.5m 간격 — 대성당 안 느낌. 높아서 남쪽(카메라 쪽) 줄은 반쯤 투명해지지 않게 조금 안쪽
            if (rnd.NextDouble() < 0.6)
            {
                bool alongX = hw >= hd;
                float half = alongX ? hw : hd, side = (alongX ? hd : hw) * 0.55f;
                for (float t = -half + 5f; t <= half - 4.9f; t += 6.5f)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var lp = alongX ? new Vector3(t, 0, s * side) : new Vector3(s * side, 0, t);
                        Put("Deco_NaveColumn", lp, alongX ? (s > 0 ? 180f : 0f) : (s > 0 ? 270f : 90f), 0.65f);
                    }
            }
            // 기도하는 태엽 천사 석상: 네 귀퉁이 안쪽에 0~3개 (방 가운데를 봄)
            foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            {
                if (rnd.NextDouble() < 0.45) continue;
                var lp = new Vector3(sx * (hw - 5.5f), 0, sz * (hd - 5.5f));
                Put("Deco_AngelStatue", lp, Quaternion.LookRotation(new Vector3(-sx, 0, -sz)).eulerAngles.y, 0.85f);
            }
            // 모래시계 성물함 0~1, 서 있는 랜턴 3~5 (랜턴은 가늘어서 막힌 곳으로 안 잡음)
            for (int tries = 0; tries < 10; tries++)
            {
                if (rnd.NextDouble() < 0.3) break;
                var lp = new Vector3(((float)rnd.NextDouble() * 2 - 1) * (hw - 5f), 0, ((float)rnd.NextDouble() * 2 - 1) * (hd - 5f));
                if (Put("Deco_Reliquary", lp, rnd.Next(4) * 90f, 0.8f)) break;
            }
            int lanterns = RoomBuilder.Scaled(rnd.Next(2, 4), info);   // 키 큰 기둥이라 화면 가장자리에서 원근으로 기울어 보임 → 많으면 산만
            for (int k = 0, tries = 0; k < lanterns && tries < 40; tries++)
            {
                var lp = new Vector3(((float)rnd.NextDouble() * 2 - 1) * (hw - 3f), 0, ((float)rnd.NextDouble() * 2 - 1) * (hd - 3f));
                if (Put("Deco_LanternStand", lp, rnd.Next(360), 0.4f, false)) k++;
            }
        }

        /// <summary>신전 장식 (2026-11-19 사용자: 공장 말고 신전 느낌): 북 · 동 · 서 벽 안쪽을 따라 깃발 기둥(랜턴 + 붉은 깃발)을 짝지어 세우고,
        /// 그 사이 벽가에 촛대. 문 앞 통로(벽 가운데 ±3m) · 톱니 바닥 · 막힌 곳은 피함</summary>
        static void Temple(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, List<(Vector3 p, float r)> gears)
        {
            if (!Kit.Has(ch, "Deco_BannerPillar")) return;
            var root = info.root;
            bool Free(Vector3 lp, float rad)
            {
                foreach (var g in gears) if (Effects.Flat(g.p - lp).magnitude < g.r + rad + 0.5f) return false;
                return !info.Blocked(info.center + lp, rad);
            }
            // (벽 안쪽 선 위의 점, 방 안쪽을 보는 방향)
            var sides = new List<(System.Func<float, Vector3> at, float len, float yaw)>
            {
                (t => new Vector3(t, 0, hd - 1.3f), hw, 180f),     // 북쪽 벽 (남쪽을 봄)
                (t => new Vector3(-hw + 1.3f, 0, t), hd, 90f),     // 서쪽 벽
                (t => new Vector3(hw - 1.3f, 0, t), hd, 270f),     // 동쪽 벽
            };
            foreach (var (at, len, yaw) in sides)
            {
                // 깃발 기둥: 문 양옆에 한 쌍(신전 입구처럼) + 벽 양 끝 쪽에 하나씩
                foreach (float t in new[] { -3.6f, 3.6f, -(len - 3f), len - 3f })
                {
                    var lp = at(t);
                    if (rnd.NextDouble() < 0.25 || !Free(lp, 0.8f)) continue;
                    Kit.Place(ch, "Deco_BannerPillar", root, lp, yaw);
                }
                // 촛대: 벽가 빈자리에 0~2개
                int candles = rnd.Next(0, 3);
                for (int k = 0; k < candles; k++)
                {
                    float t = (float)(rnd.NextDouble() * 2 - 1) * (len - 4f);
                    if (Mathf.Abs(t) < 4.5f) continue;
                    var lp = at(t);
                    if (Free(lp, 0.8f)) Kit.Place(ch, "Deco_Candles", root, lp, yaw + rnd.Next(-10, 10));
                }
            }
        }

        public static Health MakeHp(GameObject go, float hp = 1f)
        {
            var h = go.AddComponent<Health>();
            h.maxHp = hp;
            h.ResetHp(hp);
            return h;
        }

        /// <summary>고양이 쪽 효과 색 (느와르: 흰색). 적 공격은 Telegraph.Danger 계열 빨강</summary>
        public static readonly Color Friendly = new(1f, 1f, 1f);
        public static readonly Color Red = new(0.9f, 0.08f, 0.1f);
    }

    /// <summary>
    /// 돌아가는 톱니 바닥 (4장 방 기믹): 바닥에 박힌 톱니 원판이 천천히 돌고 올라선 고양이 · 적이 함께 실려 돎.
    /// 이웃 톱니와 맞물린 자리 = 두 톱니의 이빨 끝 원이 겹친 구간의 한가운데(GearPinch). 거기에 타격 박스 콜라이더(맞물림 선을 따라 2m × 0.9m)를 두고,
    /// 0.5초마다 박스 안의 고양이 · 적에게 피해 10 + 바깥으로 밀어냄. 박스와 같은 크기의 빨간 예고(Lane) · 3D 불티(블렌더 FX_Spark) · 고철 조각(FX_Scrap)도 그 자리에서 나옴.
    /// (2026-11-19 사용자: 축 쏴서 방향 바꾸기 없앰 / 정확히 맞물리는 자리를 찾아 타격 콜라이더 + 거기에 불씨 / 불꽃은 그림 말고 3D로)
    /// </summary>
    public class GearFloor : MonoBehaviour
    {
        public const float DegPerSec = 20f, MeshDmg = 10f, MeshTick = 0.5f, MeshPush = 7f;
        public const float ModelTipR = 4.0f, ModelTop = 0.56f;   // Kit_GearFloor 모델(배율 1): 이빨 끝 반지름 4.0 · 뿌리 3.52 · 윗면 높이 0.56 (2026-11-19 잼)
        public static readonly List<GearFloor> All = new();

        public float radius = 4f;               // = 이빨 끝 반지름 (모델 배율 radius / 4)
        public int dir = 1;                     // +1 = 위에서 볼 때 시계 방향
        float nextMeshTick, nextPinchScan;
        CharacterController playerCc;
        readonly Dictionary<GearFloor, GearPinch> pinches = new();

        public static void Make(GameObject go, float radius, int dir)
        {
            var g = go.AddComponent<GearFloor>();
            g.radius = radius; g.dir = dir;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public float TopY => ModelTop * transform.lossyScale.y;
        static bool Touching(GearFloor a, GearFloor b) => Effects.Flat(a.transform.position - b.transform.position).magnitude < a.radius + b.radius + 0.3f;

        void Update()
        {
            float step = DegPerSec * dir * Time.deltaTime;
            transform.Rotate(0, step, 0, Space.World);
            var c = transform.position; c.y = 0;
            var rot = Quaternion.Euler(0, step, 0);

            // 실어 나르기: 고양이 (CharacterController.Move) · 적 (위치 직접)
            var p = PlayerController.Instance;
            if (p && p.health.Alive)
            {
                var off = Effects.Flat(p.transform.position - c);
                if (off.magnitude < radius)
                {
                    if (!playerCc) playerCc = p.GetComponent<CharacterController>();
                    if (playerCc && playerCc.enabled) playerCc.Move(rot * off - off);
                }
            }
            foreach (var e in Enemy.All)
            {
                if (!e || !e.health.Alive || e.health.IsBoss) continue;
                var off = Effects.Flat(e.transform.position - c);
                if (off.magnitude < radius) e.transform.position += rot * off - off;
            }

            // 맞물린 자리 만들기 (한 쌍당 하나: 인덱스가 작은 쪽이 맡음, 톱니가 다 놓인 뒤 한 번)
            if (Time.time >= nextPinchScan)
            {
                nextPinchScan = Time.time + 0.5f;
                int me = All.IndexOf(this);
                foreach (var o in All)
                    if (o != this && All.IndexOf(o) > me && Touching(this, o) && !pinches.ContainsKey(o))
                        pinches[o] = GearPinch.Make(this, o);
            }
            if (Time.time < nextMeshTick) return;
            nextMeshTick = Time.time + MeshTick;
            foreach (var kv in pinches) if (kv.Value) kv.Value.Tick();
        }
    }

    /// <summary>
    /// 톱니 두 개가 맞물린 자리: 정확한 위치 = 두 중심을 잇는 선 위, 이빨 끝 원이 겹친 구간의 한가운데 (A에서 ra − 겹침/2).
    /// 타격 = 이 자리에 맞물림 선(옆 방향)을 따라 길쭉한 박스 콜라이더(트리거, 총알이 안 걸리게 Ignore Raycast 층) → 0.5초마다 OverlapBox로 안에 든 고양이 · 적을 찾음.
    /// 이펙트 = 같은 자리 톱니 윗면 높이에서 3D 불티(메시 파티클, 아래 흰색 → 올라가며 주황) + 이따금 고철 조각, 누가 끼면 불티 · 고철을 한꺼번에 터뜨림
    /// </summary>
    public class GearPinch : MonoBehaviour
    {
        public const float Len = 2.0f, Depth = 0.9f, Height = 2.2f;
        BoxCollider box;
        Vector3 side;
        ParticleSystem sparks, scrap, sparkBurst, scrapBurst;
        static Material sparkMat, scrapMat;
        static Mesh sparkMesh;
        static Mesh[] scrapMeshes;
        static Transform floorPlane;

        public static GearPinch Make(GearFloor a, GearFloor b)
        {
            var ca = a.transform.position; ca.y = 0;
            var cb = b.transform.position; cb.y = 0;
            float d = (cb - ca).magnitude;
            var axis = (cb - ca) / Mathf.Max(0.001f, d);
            float overlap = a.radius + b.radius - d;                 // 이빨 끝 원이 겹친 길이 (놓을 때 약 0.15m)
            var at = ca + axis * (a.radius - overlap * 0.5f);
            var go = new GameObject("GearPinch");
            go.layer = 2;                                            // Ignore Raycast: 총알 · 레이가 이 트리거에 걸리지 않게
            go.transform.SetParent(a.transform.parent, true);
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(axis));   // z = 두 톱니를 잇는 축, x = 맞물림 선(옆)
            var g = go.AddComponent<GearPinch>();
            g.side = Vector3.Cross(Vector3.up, axis);
            g.box = go.AddComponent<BoxCollider>();
            g.box.isTrigger = true;
            g.box.size = new Vector3(Len, Height, Depth);
            g.box.center = new Vector3(0, Height / 2, 0);
            float top = Mathf.Max(a.TopY, b.TopY);
            g.sparks = g.MakeSparks(top, false);
            g.sparkBurst = g.MakeSparks(top, true);
            g.scrap = g.MakeScrap(top, false);
            g.scrapBurst = g.MakeScrap(top, true);
            return g;
        }

        /// <summary>0.5초마다: 박스와 같은 크기의 빨간 예고 + 박스 안 고양이 · 적에게 피해 · 밀어냄</summary>
        public void Tick()
        {
            var at = transform.position;
            Telegraph.Lane(at - side * (Len / 2), side, Len, Depth, GearFloor.MeshTick * 0.9f, Ch4Kit.Red).NoGlint();   // 타격 범위 = 예고 (노란 십자 반짝임은 끔)
            var hits = Physics.OverlapBox(box.transform.TransformPoint(box.center), box.size * 0.5f, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            bool caught = false;
            var done = new HashSet<Health>();
            foreach (var h in hits)
            {
                var hp = h.GetComponentInParent<Health>();
                if (!hp || !hp.Alive || hp.IsBoss || !done.Add(hp)) continue;
                bool isPlayer = h.GetComponentInParent<PlayerController>();
                bool isEnemy = h.GetComponentInParent<Enemy>();
                if (!isPlayer && !isEnemy) continue;              // 소품 · 엄폐물은 안 갈림
                var dv = Effects.Flat(hp.transform.position - at);
                hp.TakeDamage(GearFloor.MeshDmg);
                hp.Knockback((Vector3.Dot(dv, side) >= 0 ? side : -side) * GearFloor.MeshPush);
                caught = true;
                if (isPlayer) CameraFollow.Shake(0.09f);
            }
            if (caught)
            {
                sparkBurst.Emit(120);
                scrapBurst.Emit(14);
                Sfx.Play("e4_clank", 0.7f, 0.08f);
            }
        }

        // ---------- 3D 불티 · 고철 (블렌더 Art/tools/blender_fx_scrap3d.py → Resources/Kit/Ch4/FX_Spark · FX_Scrap, 재질 FX_SparkMat · FX_ScrapMat = 4장 키트 메뉴가 만듦) ----------
        static void Load()
        {
            if (sparkMesh) return;
            var sm = Resources.LoadAll<Mesh>("Kit/Ch4/FX_Spark");
            if (sm.Length > 0) sparkMesh = sm[0];
            scrapMeshes = Resources.LoadAll<Mesh>("Kit/Ch4/FX_Scrap");
            sparkMat = Resources.Load<Material>("Kit/Ch4/FX_SparkMat");
            scrapMat = Resources.Load<Material>("Kit/Ch4/FX_ScrapMat");
            if (!floorPlane) { floorPlane = new GameObject("SparkFloorPlane").transform; floorPlane.position = new Vector3(0, 0.02f, 0); }
        }

        ParticleSystem NewPs(string name, float top)
        {
            Load();
            if (!floorPlane) { floorPlane = new GameObject("SparkFloorPlane").transform; floorPlane.position = new Vector3(0, 0.02f, 0); }
            var g = new GameObject(name); VividLayer.Set(g);
            g.transform.SetParent(transform, false);
            g.transform.position = transform.position + Vector3.up * top;
            g.transform.rotation = Quaternion.LookRotation(Vector3.up, transform.forward);   // 원뿔(로컬 z)이 위를 보고, 로컬 x = 맞물림 선
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var co = ps.collision; co.enabled = true; co.type = ParticleSystemCollisionType.Planes; co.SetPlane(0, floorPlane);
            co.bounce = new ParticleSystem.MinMaxCurve(0.25f, 0.45f); co.dampen = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            var pr = g.GetComponent<ParticleSystemRenderer>();
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
            return ps;
        }

        /// <summary>3D 불티: 뾰족한 결정 메시가 날아가는 방향으로 누워 날아감. 분출점 = 흰색 → 날아오르며 노랑 · 주황 (사용자: 아래 흰색 → 올라가며 주황)</summary>
        ParticleSystem MakeSparks(float top, bool burst)
        {
            var ps = NewPs(burst ? "SparkBurst3D" : "Sparks3D", top);
            var main = ps.main;
            main.loop = !burst; main.duration = 1f;
            main.startLifetime = burst ? new ParticleSystem.MinMaxCurve(0.45f, 0.9f) : new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = burst ? new ParticleSystem.MinMaxCurve(7f, 13f) : new ParticleSystem.MinMaxCurve(4.5f, 9f);
            // 굵기(x · y)와 길이(z)를 따로: 메시가 길이 1 · 굵기 0.14라 같은 배율이면 5cm로 가늘어서 안 보임 (1차 화면) → 굵기 약 3배
            main.startSize3D = true;
            main.startSizeX = burst ? new ParticleSystem.MinMaxCurve(1.1f, 1.6f) : new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSizeY = main.startSizeX;
            main.startSizeZ = burst ? new ParticleSystem.MinMaxCurve(0.45f, 0.75f) : new ParticleSystem.MinMaxCurve(0.35f, 0.6f);   // 불티 길이 0.35~0.6m
            main.gravityModifier = 1.6f;
            main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = burst ? 0f : 90f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = burst ? 80f : 60f; sh.radius = burst ? 0.4f : 0.25f;
            sh.scale = new Vector3(Len * 0.6f, 0.4f, 1f);          // 맞물림 선(로컬 x)을 따라 길게 퍼진 분출구
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0.18f), new GradientColorKey(new Color(1f, 0.6f, 0.14f), 0.45f), new GradientColorKey(new Color(1f, 0.42f, 0.05f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            col.color = grad;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.15f));   // 식으며 가늘어져 사라짐
            var co = ps.collision; co.lifetimeLoss = 0.4f;
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = sparkMesh; pr.sharedMaterial = sparkMat;
            pr.alignment = ParticleSystemRenderSpace.Velocity;      // 날아가는 방향으로 눕힘
            if (!burst) ps.Play();
            return ps;
        }

        /// <summary>고철 조각 (톱니 · 너트 · 볼트 · 꺾인 판): 달군 주황으로 튀어 올라 돌며 떨어지고 검은 쇠로 식음, 바닥에서 튐</summary>
        ParticleSystem MakeScrap(float top, bool burst)
        {
            var ps = NewPs(burst ? "ScrapBurst3D" : "Scrap3D", top);
            var main = ps.main;
            main.loop = !burst; main.duration = 1f;
            main.startLifetime = burst ? new ParticleSystem.MinMaxCurve(0.9f, 1.4f) : new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed = burst ? new ParticleSystem.MinMaxCurve(5f, 9f) : new ParticleSystem.MinMaxCurve(3.5f, 6f);
            main.startSize = burst ? new ParticleSystem.MinMaxCurve(0.16f, 0.3f) : new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.gravityModifier = 2.2f;
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = burst ? 0f : 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = burst ? 70f : 45f; sh.radius = 0.25f;
            var rl = ps.rotationOverLifetime; rl.enabled = true; rl.separateAxes = true;
            rl.x = new ParticleSystem.MinMaxCurve(-12f, 12f); rl.y = new ParticleSystem.MinMaxCurve(-12f, 12f); rl.z = new ParticleSystem.MinMaxCurve(-12f, 12f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.62f, 0.18f), 0f), new GradientColorKey(new Color(0.75f, 0.3f, 0.12f), 0.35f), new GradientColorKey(new Color(0.22f, 0.21f, 0.22f), 0.7f), new GradientColorKey(new Color(0.16f, 0.16f, 0.17f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            col.color = grad;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.0f));
            var co = ps.collision; co.lifetimeLoss = 0.15f; co.bounce = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Mesh;
            if (scrapMeshes != null && scrapMeshes.Length > 0) pr.SetMeshes(scrapMeshes);
            pr.sharedMaterial = scrapMat;
            if (!burst) ps.Play();
            return ps;
        }
    }

    /// <summary>
    /// 종지기 음파 고리 (2026-11-19 사용자: 파동 날리는 원도 이미지랑 블렌더로 제대로): RippleRing과 같은 판정(앞 선에 닿으면 한 번, 대시면 넘음, 판정 폭 = 그린 띠 폭)에
    /// 겉모습만 3D로 — 블렌더로 깎은 음파 조각(Resources/Kit/Ch4/FX_SoundSeg, 가운데보다 바깥이 솟은 물마루) 36개를 둘레에 늘어놓고,
    /// Codex로 그린 음파 띠 그림(FX_SoundWave: 빨간 물마루 + 흰 심지 + 바깥 잔물결 + 안쪽 시계 눈금)을 입힘.
    /// 반지름이 커지면 조각을 바깥으로 밀고 둘레 방향으로만 늘림 → 띠 두께(= 판정 폭)는 그대로. 끝 3m에서 옅어짐, 솟은 높이도 줄어듦
    /// </summary>
    public class SoundRing : MonoBehaviour
    {
        const int Seg = 36;
        float r, speed, dmg, maxR, width;
        bool hit;
        readonly Transform[] segs = new Transform[Seg];
        readonly Renderer[] rends = new Renderer[Seg];
        MaterialPropertyBlock mpb;
        static Mesh segMesh;
        static Material mat;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        public static bool Spawn(Vector3 at, float speed, float dmg, float maxR, float width, float startR = 0.8f)
        {
            if (!segMesh) { var ms = Resources.LoadAll<Mesh>("Kit/Ch4/FX_SoundSeg"); if (ms.Length > 0) segMesh = ms[0]; }
            if (!mat) mat = Resources.Load<Material>("Kit/Ch4/FX_SoundWaveMat");
            if (!segMesh || !mat) return false;                   // 자산이 없으면 예전 선 고리로 (EnemyCh4가 대신 부름)
            var go = new GameObject("SoundRing"); VividLayer.Set(go);
            go.transform.position = new Vector3(at.x, 0.03f, at.z);
            var s = go.AddComponent<SoundRing>();
            s.speed = speed; s.dmg = dmg; s.maxR = maxR; s.width = width; s.r = startR;
            s.mpb = new MaterialPropertyBlock();
            for (int i = 0; i < Seg; i++)
            {
                var g = new GameObject("Seg"); VividLayer.Set(g);
                g.transform.SetParent(go.transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = segMesh;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                s.segs[i] = g.transform; s.rends[i] = mr;
            }
            s.Layout();
            return true;
        }

        void Layout()
        {
            float arc = Mathf.PI * 2 * r / Seg * 1.04f;              // 이음매가 안 보이게 살짝 겹침
            float fade = Mathf.Clamp01((maxR - r) / 3f);
            float rise = 0.6f + 0.6f * fade;                          // 퍼지며 물마루가 낮아짐
            for (int i = 0; i < Seg; i++)
            {
                float a = i * Mathf.PI * 2 / Seg;
                var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                segs[i].localPosition = outward * r;
                segs[i].localRotation = Quaternion.LookRotation(-outward);   // 조각의 바깥(블렌더 +Y) = FBX로 들어오면 -Z
                segs[i].localScale = new Vector3(arc, width * 2.2f * rise, width);
            }
            mpb.SetColor(ColorId, new Color(1f, 1f, 1f, 0.95f * fade));
            foreach (var mr in rends) mr.SetPropertyBlock(mpb);
        }

        void Update()
        {
            r += speed * Time.deltaTime;
            if (r > maxR) { Destroy(gameObject); return; }
            Layout();
            if (hit || dmg <= 0) return;
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive || p.health.dashing) return;
            float d = Effects.Flat(p.transform.position - transform.position).magnitude;
            if (Mathf.Abs(d - r) > width * 0.5f) return;            // 그린 띠 폭 그대로
            hit = true;
            p.health.TakeDamage(dmg);
        }
    }

    /// <summary>4장 상인 오르골 인형 (기획서: 오르골 상자 위에서 천천히 돌며 장사): 평소엔 천천히 돌고, 고양이가 4m 안에 오면 부드럽게 돌아서 고양이를 봄</summary>
    public class MusicDollTurn : MonoBehaviour
    {
        public const float DegPerSec = 24f, FaceRange = 4f;
        void Update()
        {
            var p = PlayerController.Instance;
            var to = p ? Effects.Flat(p.transform.position - transform.position) : Vector3.zero;
            if (p && to.magnitude < FaceRange && to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 180f * Time.deltaTime);
            else
                transform.Rotate(0, DegPerSec * Time.deltaTime, 0, Space.World);
        }
    }

    /// <summary>감긴 태엽 스프링 (4장): 쏘면(몬스터 공격 포함) 0.4초 떨다가 튕기며 반경 3m 폭발 · 적 60 · 고양이 20, 근처 폭발물 연쇄</summary>
    public class WindSpring : MonoBehaviour
    {
        public const float Radius = 3f, EnemyDmg = 60f, PlayerDmg = 20f, Fuse = 0.4f;
        bool done;

        public static void Make(GameObject go)
        {
            Ch4Kit.MakeHp(go);
            go.AddComponent<WindSpring>();
            DangerEdge.Add(go);
        }

        void Awake() => GetComponent<Health>().OnDeath += _ => { if (!done) { done = true; StartCoroutine(Snap()); } };

        IEnumerator Snap()
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            var at = transform.position;
            var ground = new Vector3(at.x, 0.03f, at.z);
            Telegraph.Ring(ground, Radius, Fuse, Telegraph.Danger);
            var baseScale = transform.localScale;
            for (float t = 0; t < Fuse; t += Time.deltaTime)
            {
                transform.position = at + Random.insideUnitSphere * 0.06f;
                transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f - 0.25f * t / Fuse), baseScale.z);   // 눌렸다가
                yield return null;
            }
            Effects.Explosion(ground, Radius, EnemyDmg, new Color(0.95f, 0.95f, 0.95f));
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - ground).magnitude <= Radius + 0.3f) p.health.TakeDamage(PlayerDmg);
            FxBurst.Spawn(ground + Vector3.up * 0.8f, Radius * 0.9f, FxBurst.Kind.Spark, Color.white);
            CameraFollow.Shake(0.12f);
            PropHit.Area(ground, Radius, EnemyDmg);
            Destroy(gameObject);
        }
    }

    /// <summary>굴러가는 톱니바퀴 (4장): 쏘면 맞은 방향으로 14m/초 굴러가 적을 치고(60) 벽에 한 번 튕긴 뒤 부서짐</summary>
    public class GearWheel : MonoBehaviour
    {
        public const float Speed = 14f, EnemyDmg = 60f, Width = 1.6f, MaxTime = 3.5f;
        bool rolling;

        public static void Make(GameObject go)
        {
            var h = Ch4Kit.MakeHp(go, 1e6f);
            go.AddComponent<GearWheel>();
            DangerEdge.Add(go);
        }

        void Awake()
        {
            var h = GetComponent<Health>();
            h.OnDamaged += _ =>
            {
                h.ResetHp(h.maxHp);
                if (rolling) return;
                var d = Time.time - h.lastHitAt < 0.2f ? h.lastHitDir : Effects.Flat(transform.position - (PlayerController.Instance ? PlayerController.Instance.transform.position : transform.position - transform.forward));
                if (d.sqrMagnitude < 1e-4f) d = transform.forward;
                StartCoroutine(Roll(d.normalized));
            };
        }

        IEnumerator Roll(Vector3 dir)
        {
            rolling = true;
            var mine = GetComponentsInChildren<Collider>();
            foreach (var c in mine) c.enabled = false;
            var hit = new HashSet<Health>();
            int bounces = 0;
            float t = 0;
            transform.rotation = Quaternion.LookRotation(dir);
            var spinAxis = Vector3.Cross(Vector3.up, dir);
            while (t < MaxTime)
            {
                float step = Speed * Time.deltaTime;
                var from = transform.position;
                // 벽(정적 충돌체)에 닿으면 한 번 튕기고, 두 번째는 부서짐
                if (Physics.SphereCast(from + Vector3.up * 0.8f, 0.6f, dir, out var rh, step + 0.3f, ~0, QueryTriggerInteraction.Ignore)
                    && !rh.collider.GetComponentInParent<Enemy>() && !rh.collider.GetComponentInParent<PlayerController>())
                {
                    if (++bounces > 1) break;
                    var n = Effects.Flat(rh.normal).normalized;
                    if (n.sqrMagnitude < 0.5f) n = -dir;
                    dir = Vector3.Reflect(dir, n).normalized;
                    spinAxis = Vector3.Cross(Vector3.up, dir);
                    FxBurst.Spawn(rh.point, 1.2f, FxBurst.Kind.Spark, Color.white);
                    CameraFollow.Shake(0.06f);
                }
                transform.position += dir * step;
                transform.Rotate(spinAxis, step / 0.8f * Mathf.Rad2Deg, Space.World);
                foreach (var e in Enemy.All.ToArray())
                    if (e && e.health.Alive && Effects.Flat(e.transform.position - transform.position).magnitude < Width && hit.Add(e.health))
                    { e.health.TakeDamage(EnemyDmg); e.health.Knockback(dir * 6f); }
                PropHit.Area(transform.position, Width * 0.6f, EnemyDmg, hit);
                t += Time.deltaTime;
                yield return null;
            }
            FxBurst.Spawn(transform.position + Vector3.up * 0.7f, 2f, FxBurst.Kind.Explosion, new Color(0.85f, 0.85f, 0.85f));
            CameraFollow.Shake(0.1f);
            Destroy(gameObject);
        }
    }

    /// <summary>낡은 종 (4장): 쏘면 음파 링 반경 5m — 적 피해 30 + 범위 안 적 탄을 지움. 다시 쓰려면 8초 (부서지지 않음)</summary>
    public class OldBell : MonoBehaviour
    {
        public const float Radius = 5f, EnemyDmg = 30f, Cooldown = 8f;
        float readyAt;
        Health hp;

        public static void Make(GameObject go)
        {
            var h = Ch4Kit.MakeHp(go, 1e6f);
            go.AddComponent<OldBell>().hp = h;
        }

        void Awake()
        {
            if (!hp) hp = GetComponent<Health>();
            hp.OnDamaged += _ => { hp.ResetHp(hp.maxHp); if (Time.time >= readyAt) Ring(); };
        }

        void Ring()
        {
            readyAt = Time.time + Cooldown;
            var ground = new Vector3(transform.position.x, 0.03f, transform.position.z);
            RippleRing.Spawn(ground, 14f, 0f, Radius, 0.35f, Ch4Kit.Friendly);
            RippleRing.Spawn(ground, 10f, 0f, Radius * 0.8f, 0.2f, Ch4Kit.Friendly, 0.4f);
            foreach (var e in Enemy.All.ToArray())
                if (e && e.health.Alive && Effects.Flat(e.transform.position - ground).magnitude <= Radius + 0.4f) e.health.TakeDamage(EnemyDmg);
            EnemyProjectile.WashAway(ground, Radius);
            CameraFollow.Shake(0.07f);
            StartCoroutine(Swing());
        }

        IEnumerator Swing()
        {
            var baseRot = transform.localRotation;
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 14f) * 10f * (1f - t / 1.2f), 0, 0);
                yield return null;
            }
            transform.localRotation = baseRot;
        }
    }
}
