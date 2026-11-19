// 2장 (멈춘 기차역) 방 꾸미기 + 폭발물 3종 (docs/Cat_Tang_레벨_디자인.md 챕터 2)
// - 장소(스테이지)별 벽면 · 지형지물 · 소품: 1 화물 야적장 → 2 승강장 → 3 멈춘 객차 안 → 4 기관차고 → 보스 전차대
// - 폭발물(쏘면 시간이 풀림, 주황 ↔ 노랑 테두리 DangerEdge): 증기 밸브(정면 6m 증기) · 공중에 멈춘 짐(떨어짐) · 광차(선로 따라 질주)
// 키트(Resources/Kit/Ch2)는 블렌더 + Codex 모델 (Art/tools/blender_kit_ch2_*.py)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Ch2Kit
    {
        public static bool Is(ChapterDef ch) => ch != null && ch.Id == 2 && Kit.Has(ch, "SteamValve");

        // 보스 경기장 (2026-11-18 사용자: 패턴 설계상 더 크게): 전차대 지름 24m → 32m, 방 40×38m.
        // 객차 둘레 회전(폭 2m)이 가장자리를 돌아도 안쪽 반경 약 14m가 남아 증기 분출(7m) · 레일 돌진을 피할 자리가 생김
        public const float BossArenaDiameter = 32f;
        public static readonly Vector2 BossRoomSize = new(40, 38);

        static int Stage => RunManager.Instance ? RunManager.Instance.stageIndex : 0;
        static bool BossRoom => RunManager.Instance && RunManager.Instance.IsBossRoom;

        // ---------- 벽면 (Kit.BuildingRow가 씀). 스테이지 1(야적장)은 둑(Cliff)이라 여기 안 옴 ----------
        public static (string[] names, float[] widths) Facades()
        {
            if (BossRoom || Stage >= 3) return (new[] { "Facade_EngineShed", "Facade_Warehouse", "Wall" }, new[] { 9f, 7f, 4f });
            if (Stage == 2) return (new[] { "Facade_Canopy", "Facade_Waiting", "Facade_Warehouse" }, new[] { 8f, 6f, 7f });
            return (new[] { "Facade_Canopy", "Facade_Waiting", "Facade_Ticket", "Facade_StationHall" }, new[] { 8f, 6f, 5f, 8f });
        }

        // ---------- 큰 지형지물 (RoomBuilder.AddShape가 씀) ----------
        public static string[] Landmarks()
        {
            switch (Stage)
            {
                case 0: return new[] { "LM_CoalPile", "LM_LuggageCart", "LM_WaterCrane", "LM_SignalGantry" };
                case 1: return new[] { "LM_LuggageCart", "LM_WaterCrane", "LM_SignalGantry" };
                case 2: return new string[0];                                  // 객차 방은 Coaches가 따로
                default: return new[] { "LM_Locomotive", "LM_CoalPile", "LM_WaterCrane", "LM_SignalGantry" };
            }
        }

        /// <summary>스테이지 3(멈춘 객차 안): 옆면이 뚫린 객차 2~3량이 나란히. 열린 면은 방 가운데 줄을 봄, 등판 · 양 끝만 막힘</summary>
        public static void Coaches(ChapterDef ch, RoomBuilder.RoomInfo info, System.Random rnd)
        {
            if (Stage != 2 || BossRoom || !Kit.Has(ch, "LM_Coach")) return;
            var slots = new List<Vector2> { new(-8.2f, 5.6f), new(8.2f, 5.6f), new(-8.2f, -5.6f), new(8.2f, -5.6f) };
            for (int i = slots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (slots[i], slots[j]) = (slots[j], slots[i]); }
            int n = rnd.NextDouble() < 0.5 ? 2 : 3;
            for (int k = 0; k < n; k++)
            {
                var s = new Vector2(slots[k].x * info.size.x / 28f, slots[k].y * info.size.y / 24f);   // 방 크기에 비례
                float yaw = s.y > 0 ? 180f : 0f;                               // 열린 면(+Z)이 방 가운데를 보게
                var go = Kit.Place(ch, "LM_Coach", info.root, new Vector3(s.x, 0, s.y), yaw, 1f, false);
                if (!go) continue;
                var b = Kit.LocalBounds(Kit.Prefab(ch, "LM_Coach"));
                float h = b.size.y;
                // 등판 (닫힌 면) + 양 끝 벽만 충돌 → 안쪽 좌석 사이로 들어갈 수 있음
                AddBox(go, new Vector3(b.center.x, h / 2, b.min.z + 0.35f), new Vector3(b.size.x, h, 0.7f));
                AddBox(go, new Vector3(b.min.x + 0.3f, h / 2, b.center.z), new Vector3(0.6f, h, b.size.z));
                AddBox(go, new Vector3(b.max.x - 0.3f, h / 2, b.center.z), new Vector3(0.6f, h, b.size.z));
                // 적 생성 · 이동이 피할 발자국 (등판 쪽 띠)
                float backZ = info.center.z + s.y + (s.y > 0 ? 1f : -1f);
                info.blocks.Add(new Rect(info.center.x + s.x - b.size.x / 2, backZ - 0.6f, b.size.x, 1.2f));
            }
        }

        static void AddBox(GameObject go, Vector3 center, Vector3 size)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = center;
            bc.size = size;
        }

        // ---------- 소품 · 폭발물 (RoomBuilder.KitProps 대신) ----------
        /// <summary>장소별: 야적장 = 광차 · 멈춘 짐 / 승강장 = 증기 밸브 · 멈춘 짐 / 객차 = 멈춘 짐 · 엄폐물 / 기관고 = 증기 밸브 · 광차. 엄폐물은 전부</summary>
        public static void Props(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            int stage = Stage;
            if (boss)
            {
                // 경기장 바닥: 새 'Arena'(지름 32m, 방사형 판자 · 시계 숫자 띠 · +자 선로 · 놋쇠 축)가 있으면 그것, 없으면 예전 전차대(24m)를 늘려 씀
                // 예전 전차대는 두께 0.3m 원판이라 윗면이 땅(0.01)보다 0.12 위에 오게 내려 놓음
                if (Kit.Has(ch, "Arena")) Kit.Place(ch, "Arena", root, Vector3.zero, 0f, 1f, false);              // 거대한 시계: 원판 없이 테두리 · 슬롯 · 선로 · 바늘만 타일 위에 (밑면 = 바닥)
                else if (Kit.Has(ch, "Turntable")) Kit.Place(ch, "Turntable", root, new Vector3(0, -0.18f, 0), 0f, BossArenaDiameter / 24f, false);
                ArenaFence(root, BossArenaDiameter / 2f);
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        Kit.Place(ch, "SignalLamp", root, new Vector3(sx * (hw - 1.6f), 0, sz * (hd - 1.6f)), Quaternion.LookRotation(new Vector3(-sx, 0, -sz)).eulerAngles.y);
                return;
            }

            // 선로 한 줄 (야적장 · 승강장 · 기관고): 동서로 방을 가로지름, 광차 길
            float railZ = float.NaN;
            if (stage != 2 && Kit.Has(ch, "RailTrack"))
            {
                railZ = (rnd.NextDouble() < 0.5 ? -1 : 1) * (4.5f + (float)rnd.NextDouble() * 3.5f);
                var rail = new GameObject("Rail").transform;
                rail.SetParent(root, false);
                for (float x = -hw + 2f; x <= hw - 2f; x += 4f)
                    Kit.Place(ch, "RailTrack", rail, new Vector3(x, 0.01f, railZ), 90f, 1f, false);
            }
            // 지나가는 열차 (선로가 있는 방): 12초마다 기적 · 선로 빛 1.5초 → 열차가 선로를 훑음
            if (!float.IsNaN(railZ)) PassingTrain.Attach(info, railZ, hw);

            // 후보 자리 (가운데 · 문 앞 통로 · 막힌 곳 · 선로 띠 비움)
            var spots = new List<Vector3>();
            for (float x = -hw + 3f; x <= hw - 3f; x += 3f)
                for (float z = -hd + 3f; z <= hd - 3f; z += 3f)
                {
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;
                    if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;
                    if (!float.IsNaN(railZ) && Mathf.Abs(z - railZ) < 1.8f) continue;
                    if (info.Blocked(info.center + new Vector3(x, 0, z), 1.4f)) continue;
                    spots.Add(new Vector3(x, 0, z));
                }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            int si = 0;
            Vector3 Next() => si < spots.Count ? spots[si++] + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.8f : Vector3.zero;
            float Yaw90() => rnd.Next(4) * 90f;

            // 광차 1~2 (야적장 · 기관고): 선로 위, 먼 쪽 끝을 향함
            if ((stage == 0 || stage >= 3) && !float.IsNaN(railZ))
            {
                int carts = Mathf.Min(2, RoomBuilder.Scaled(rnd.Next(1, 3), info));
                for (int k = 0; k < carts; k++)
                {
                    float x = (k == 0 ? -1 : 1) * (4f + (float)rnd.NextDouble() * 5f);
                    float dir = x < 0 ? 1f : -1f;                          // 방 반대편으로 달림
                    var go = Kit.Place(ch, "MineCart", root, new Vector3(x, 0, railZ), dir > 0 ? 90f : 270f);
                    if (go) RunawayCart.Make(go, Mathf.Abs((dir > 0 ? hw - 1.5f : -hw + 1.5f) - x));
                }
            }
            // 증기 밸브 2~3 (승강장 · 기관고)
            if (stage == 1 || stage >= 3)
            {
                int n = RoomBuilder.Scaled(rnd.Next(2, 4), info);
                for (int k = 0; k < n; k++)
                {
                    var p = Next();
                    // 노즐은 방 가운데 쪽(가로 · 세로 중 먼 축)을 봄
                    var toC = Mathf.Abs(p.x) > Mathf.Abs(p.z) ? new Vector3(-Mathf.Sign(p.x), 0, 0) : new Vector3(0, 0, -Mathf.Sign(p.z));
                    var go = Kit.Place(ch, "SteamValve", root, p, Quaternion.LookRotation(toC).eulerAngles.y);
                    if (go) SteamVent.Make(go);
                }
            }
            // 공중에 멈춘 짐 2~4 (야적장 · 승강장 · 객차)
            if (stage <= 2)
            {
                int n = RoomBuilder.Scaled(rnd.Next(2, 5), info);
                for (int k = 0; k < n; k++)
                {
                    var go = Kit.Place(ch, "FloatLuggage", root, Next() + Vector3.up * FlyingLuggage.Height, rnd.Next(360), 1.1f, false);
                    if (go) FlyingLuggage.Make(go);
                }
            }
            // 부서지는 엄폐물 3~5 (짐 더미 · 벤치 · 상자)
            string[] kinds = { "LuggagePile", "Bench", "Crates" };
            int cover = RoomBuilder.Scaled(rnd.Next(3, 6), info);
            for (int k = 0; k < cover; k++)
            {
                string kind = kinds[rnd.Next(kinds.Length)];
                var go = Kit.Place(ch, kind, root, Next(), Yaw90() + rnd.Next(-10, 10));
                if (go) Breakable.Make(go, kind == "LuggagePile" ? 60 : kind == "Crates" ? 45 : 30);
            }
            // 굳은 승객 석상 6~8 (FrozenPassenger): 받침대 위 멈춘 자세, 웨이브 예고 때 깨지며 하수인이 나옴. 돌이라 고양이 · 탄을 막음
            // 한 방 안에선 포즈가 겹치지 않게 섞어 차례로, 크기 ±8%
            var poses = new List<string>();
            foreach (var pz in FrozenPassenger.Poses) if (Kit.Has(ch, pz)) poses.Add(pz);
            for (int i = poses.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (poses[i], poses[j]) = (poses[j], poses[i]); }
            if (poses.Count > 0)
            {
                int people = RoomBuilder.Scaled(rnd.Next(6, 9), info);
                for (int k = 0; k < people && si < spots.Count; k++)
                {
                    string pose = poses[k % poses.Count];
                    var go = Kit.Place(ch, pose, root, Next(), rnd.Next(360), 0.92f + (float)rnd.NextDouble() * 0.16f, false);
                    if (go && !go.GetComponent<FrozenPassenger>()) go.AddComponent<FrozenPassenger>();
                }
            }
            // 장식: 벽 가까이 역 시계 · 신호등 1~2
            int decor = rnd.Next(1, 3);
            for (int k = 0; k < decor; k++)
            {
                bool ns = rnd.NextDouble() < 0.5;
                float sgn = rnd.NextDouble() < 0.5 ? -1 : 1;
                var p = ns ? new Vector3((float)(rnd.NextDouble() * 2 - 1) * (hw - 4), 0, sgn * (hd - 1.4f)) : new Vector3(sgn * (hw - 1.4f), 0, (float)(rnd.NextDouble() * 2 - 1) * (hd - 4));
                if (Mathf.Abs(p.x) < 3f && Mathf.Abs(p.z) > hd - 2f) p.x = 4f * sgn;   // 문 앞 피함
                if (Mathf.Abs(p.z) < 3f && Mathf.Abs(p.x) > hw - 2f) p.z = 4f * sgn;
                var inward = ns ? new Vector3(0, 0, -sgn) : new Vector3(-sgn, 0, 0);
                Kit.Place(ch, rnd.NextDouble() < 0.5 ? "StationClock" : "SignalLamp", root, p, Quaternion.LookRotation(inward).eulerAngles.y);
            }
        }

        /// <summary>
        /// 중간 보스 대합실 (28×24m): 긴 벤치 줄이 엄폐물(보스 탄에 부서짐). 가운데 세로 통로는 비움(보스가 뒷벽 쪽에서 나옴).
        /// 뒷벽에 역 시계, 모서리 짐 더미 · 매표 부스 장식, 증기 밸브 2개
        /// </summary>
        public static void WaitingHall(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd)
        {
            var root = info.root;
            // 벤치 줄: 좌우 두 블록 × 3줄, 줄마다 벤치 2~3개 이어 붙임 (하나씩 부서짐)
            float benchW = Kit.Has(ch, "Bench") ? Mathf.Max(1.2f, Kit.LocalBounds(Kit.Prefab(ch, "Bench")).size.x) : 2f;
            foreach (int sx in new[] { -1, 1 })
                foreach (float z in new[] { -5.5f, -0.5f, 4.5f })
                {
                    int n = rnd.Next(2, 4);
                    float x0 = sx * 4.2f;                                       // 가운데 통로(폭 8m) 바깥부터
                    for (int k = 0; k < n; k++)
                    {
                        var go = Kit.Place(ch, "Bench", root, new Vector3(x0 + sx * (benchW * (k + 0.5f)), 0, z + (float)(rnd.NextDouble() - 0.5) * 0.4f), z > 0 ? 180f : 0f);
                        if (go) Breakable.Make(go, 30);
                    }
                }
            Kit.Place(ch, "StationClock", root, new Vector3(0, 0, hd - 1.2f), 180f);
            Kit.Place(ch, "LuggagePile", root, new Vector3(-hw + 1.8f, 0, hd - 1.8f), 30f);
            Kit.Place(ch, "LuggagePile", root, new Vector3(hw - 1.8f, 0, -hd + 1.8f), 210f);
            Kit.Place(ch, "SignalLamp", root, new Vector3(hw - 1.4f, 0, hd - 1.4f), 225f);
            Kit.Place(ch, "SignalLamp", root, new Vector3(-hw + 1.4f, 0, -hd + 1.4f), 45f);
            foreach (int sx in new[] { -1, 1 })
            {
                var go = Kit.Place(ch, "SteamValve", root, new Vector3(sx * (hw - 1.6f), 0, 1.5f), sx > 0 ? 270f : 90f);
                if (go) SteamVent.Make(go);
            }
        }

        /// <summary>원형 경기장 경계: 전차대 가장자리 바깥에 보이지 않는 벽 조각을 둘러 고양이가 모서리(돌바닥)로 빠져 객차 회전 · 증기를 피하지 못하게</summary>
        public static void ArenaFence(Transform root, float r)
        {
            var fence = new GameObject("ArenaFence").transform;
            fence.SetParent(root, false);
            const int n = 48;
            float seg = 2f * Mathf.PI * (r + 0.4f) / n + 0.3f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var go = new GameObject("Fence_" + i);
                go.transform.SetParent(fence, false);
                go.transform.localPosition = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (r + 0.4f) + Vector3.up * 1.5f;
                go.transform.localRotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                go.AddComponent<BoxCollider>().size = new Vector3(seg, 3f, 0.8f);
            }
        }

        // ---------- 공통: 선 · 원 범위 피해 ----------
        public static void HitLane(Vector3 from, Vector3 dir, float len, float width, float enemyDmg, float playerDmg, HashSet<Health> done)
        {
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && !done.Contains(en.health) && InLane(en.transform.position, from, dir, len, width + 0.5f)) { done.Add(en.health); en.health.TakeDamage(enemyDmg); }
            var p = PlayerController.Instance;
            if (p && p.health.Alive && !done.Contains(p.health) && InLane(p.transform.position, from, dir, len, width + 0.3f)) { done.Add(p.health); p.health.TakeDamage(playerDmg); }
        }

        public static bool InLane(Vector3 pos, Vector3 from, Vector3 dir, float len, float width)
        {
            var rel = Effects.Flat(pos - from);
            float along = Vector3.Dot(rel, dir);
            return along >= -0.3f && along <= len && (rel - dir * along).magnitude <= width / 2;
        }

        public static Health MakeHp(GameObject go)
        {
            var h = go.AddComponent<Health>();
            h.maxHp = 1;
            h.ResetHp(1);
            return h;
        }
    }

    /// <summary>증기 밸브 (2장 기획서): 쏘면 0.5초 뒤 정면 6m × 폭 1.5m로 증기 1.5초 · 적 40 · 고양이 15. 쏜 뒤엔 그냥 파이프</summary>
    public class SteamVent : MonoBehaviour
    {
        public const float Delay = 0.5f, Len = 6f, Width = 1.5f, Dur = 1.5f, EnemyDmg = 40f, PlayerDmg = 15f;
        bool done;

        public static void Make(GameObject go)
        {
            Ch2Kit.MakeHp(go);
            go.AddComponent<SteamVent>();
            DangerEdge.Add(go);
        }

        void Awake() => GetComponent<Health>().OnDeath += _ => { if (!done) { done = true; StartCoroutine(Blow()); } };

        IEnumerator Blow()
        {
            var dir = Effects.Flat(transform.forward).normalized;
            var from = transform.position + dir * 0.5f;
            Telegraph.Lane(from, dir, Len, Width, Delay, Telegraph.Danger);
            GunkataFx.Smoke(from + Vector3.up * 1f, 0.8f, 0.5f);
            yield return new WaitForSeconds(Delay);
            GetComponent<DangerEdge>()?.Off();
            CameraFollow.Shake(0.08f);
            var hit = new HashSet<Health>();
            float end = Time.time + Dur, nextPuff = 0;
            while (Time.time < end)
            {
                Ch2Kit.HitLane(from, dir, Len, Width, EnemyDmg, PlayerDmg, hit);
                if (Time.time >= nextPuff)
                {
                    for (int i = 0; i < 3; i++)
                        GunkataFx.Smoke(from + dir * Random.Range(0.3f, Len) + Vector3.up * Random.Range(0.6f, 1.4f) + Vector3.Cross(dir, Vector3.up) * Random.Range(-0.5f, 0.5f), Random.Range(1f, 1.6f), 0.7f);
                    nextPuff = Time.time + 0.08f;
                }
                yield return null;
            }
        }
    }

    /// <summary>공중에 멈춘 짐: 쏘면 탄에 밀려 쏜 방향으로 날아가다 적 · 벽에 부딪히거나 12m 끝에서 폭발 · 반경 1.5m · 적 40 · 고양이 15
    /// (2026-11-17 사용자: 위에서 떨어지는 건 별로 → 총으로 쏘면 날아가서 폭발). 날아갈 길은 0.12초 동안 직선 예고</summary>
    public class FlyingLuggage : MonoBehaviour
    {
        public const float Height = 1.7f, Kick = 0.12f, Speed = 15f, Range = 12f, Radius = 1.5f, EnemyDmg = 40f, PlayerDmg = 15f;
        bool done;

        public static void Make(GameObject go)
        {
            Ch2Kit.MakeHp(go);
            go.AddComponent<FlyingLuggage>();
            // 쏘는 높이에서 맞도록 바닥까지 닿는 맞음 상자 (자식, Health는 부모에서 찾음)
            var hitBox = new GameObject("Hit");
            hitBox.transform.SetParent(go.transform, false);
            hitBox.transform.position = go.transform.position - Vector3.up * Height * 0.5f;
            var bc = hitBox.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.9f, Height + 0.6f, 0.9f) / Mathf.Max(0.01f, go.transform.lossyScale.x);
            DangerEdge.Add(go);
        }

        void Awake() => GetComponent<Health>().OnDeath += _ => { if (!done) { done = true; StartCoroutine(Fly()); } };

        IEnumerator Fly()
        {
            // 날아갈 방향 = 맞은 방향 (고양이 탄 · 적 탄 · 몬스터 근접 · 폭발, Health.HitFrom). 기록이 없으면 고양이 → 짐
            var h = GetComponent<Health>();
            var p = PlayerController.Instance;
            Vector3 dir;
            if (h && Time.time - h.lastHitAt < 0.3f) dir = h.lastHitDir;
            else dir = p ? Effects.Flat(transform.position - p.transform.position) : transform.forward;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            var ground = new Vector3(transform.position.x, 0.02f, transform.position.z);
            Telegraph.Lane(ground, dir, Range, 1.2f, Kick, Telegraph.Danger).NoGlint();
            // 맞은 순간: 움찔 (뒤로 살짝 밀렸다 튐) + 불꽃
            GunkataFx.Spark(transform.position, 0.9f);
            var start = transform.position;
            for (float t = 0; t < Kick; t += Time.deltaTime)
            {
                transform.position = start + dir * Mathf.Sin(t / Kick * Mathf.PI) * 0.15f + Random.insideUnitSphere * 0.04f;
                yield return null;
            }
            // 날아감: 높이를 몸통 높이(1.0m)로 낮추며 굴러가듯 회전, 연기 꼬리
            var rollAxis = Vector3.Cross(Vector3.up, dir);
            float moved = 0, nextPuff = 0;
            var pos = start;
            while (moved < Range)
            {
                float step = Speed * Time.deltaTime;
                if (Physics.Raycast(pos, dir, out var wh, step + 0.5f, ~0, QueryTriggerInteraction.Ignore) && !wh.collider.GetComponentInParent<Enemy>() && !wh.collider.GetComponentInParent<PlayerController>())
                {
                    pos = wh.point - dir * 0.4f;              // 벽 · 엄폐물에 쾅
                    break;
                }
                pos += dir * step;
                moved += step;
                pos.y = Mathf.Lerp(start.y, 1.0f, Mathf.Clamp01(moved / 3f));
                transform.position = pos;
                transform.Rotate(rollAxis, 600f * Time.deltaTime, Space.World);
                if (Time.time >= nextPuff) { GunkataFx.Smoke(pos - dir * 0.4f, 0.7f, 0.4f); nextPuff = Time.time + 0.05f; }
                bool hitEnemy = false;
                foreach (var en in Enemy.All)
                    if (en && en.health.Alive && Effects.Flat(en.transform.position - pos).magnitude < 1.1f) { hitEnemy = true; break; }
                if (hitEnemy) break;
                yield return null;
            }
            Explode(new Vector3(pos.x, 0.02f, pos.z));
        }

        void Explode(Vector3 ground)
        {
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && Effects.Flat(en.transform.position - ground).magnitude <= Radius + 0.4f) en.health.TakeDamage(EnemyDmg);
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - ground).magnitude <= Radius + 0.3f) p.health.TakeDamage(PlayerDmg);
            GunkataFx.Explosion(ground, Radius, false, new Color(1f, 0.55f, 0.2f));
            FxBurst.Spawn(ground + Vector3.up * 0.5f, 1.6f, FxBurst.Kind.Spark, new Color(0.75f, 0.5f, 0.3f));
            CameraFollow.Shake(0.14f);
            PropHit.Area(ground, Radius, EnemyDmg);           // 근처 폭발물 · 엄폐물도 연쇄
            Destroy(gameObject);
        }
    }

    /// <summary>광차 (2장 기획서): 쏘면 선로 따라 12m/초로 질주, 끝에서 부서짐 · 적 50 · 고양이 15</summary>
    public class RunawayCart : MonoBehaviour
    {
        public const float Speed = 12f, Windup = 0.3f, Width = 1.4f, EnemyDmg = 50f, PlayerDmg = 15f;
        public float runLen = 10f;
        bool done;

        public static void Make(GameObject go, float runLen)
        {
            Ch2Kit.MakeHp(go);
            go.AddComponent<RunawayCart>().runLen = Mathf.Max(2f, runLen);
            DangerEdge.Add(go);
        }

        void Awake() => GetComponent<Health>().OnDeath += _ => { if (!done) { done = true; StartCoroutine(Run()); } };

        IEnumerator Run()
        {
            var dir = Effects.Flat(transform.forward).normalized;
            Telegraph.Lane(transform.position, dir, runLen, Width, Windup, Telegraph.Danger);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            GunkataFx.Spark(transform.position + Vector3.up * 0.4f, 1f);
            yield return new WaitForSeconds(Windup);
            var hit = new HashSet<Health>();
            float moved = 0;
            while (moved < runLen)
            {
                float step = Speed * Time.deltaTime;
                var from = transform.position;
                transform.position += dir * step;
                moved += step;
                Ch2Kit.HitLane(from - dir * 0.7f, dir, step + 1.4f, Width, EnemyDmg, PlayerDmg, hit);
                if (Random.value < 0.5f) GunkataFx.Spark(from + Vector3.up * 0.15f, 0.5f);
                yield return null;
            }
            FxBurst.Spawn(transform.position + Vector3.up * 0.6f, 2.2f, FxBurst.Kind.Explosion, new Color(1f, 0.6f, 0.25f));
            CameraFollow.Shake(0.12f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지나가는 열차 (레벨 디자인 md 2장): 선로 폭 3m, 12초마다, 예고 1.5초(기적 + 선로 빛), 피해 적 60 · 고양이 20.
    /// 그 방이 전투 중일 때만 돎. 열차 몸은 블렌더 모델 Kit/Ch2/TrainEngine · TrainCar (ToyTrain)
    /// </summary>
    public class PassingTrain : MonoBehaviour
    {
        public const float Every = 12f, Warn = 1.5f, Width = 3f, Speed = 28f, PlayerDmg = 20f, EnemyDmg = 60f;
        RoomBuilder.RoomInfo room;
        float railZ, hw, nextAt = -1;

        public static void Attach(RoomBuilder.RoomInfo info, float railZ, float hw)
        {
            var t = info.root.gameObject.AddComponent<PassingTrain>();
            t.room = info; t.railZ = railZ; t.hw = hw;
        }

        void Update()
        {
            var rm = RunManager.Instance;
            bool live = rm && rm.CurrentRoom == room && rm.inCombat && !rm.runOver;
            if (!live) { nextAt = -1; return; }
            if (nextAt < 0) nextAt = Time.time + 6f;                 // 방에 들어서고 6초 뒤 첫 열차
            if (Time.time < nextAt) return;
            nextAt = Time.time + Every;
            StartCoroutine(Pass());
        }

        IEnumerator Pass()
        {
            var dir = Random.value < 0.5f ? Vector3.right : Vector3.left;
            var from = room.center + new Vector3(-dir.x * (hw + 1f), 0, railZ);
            float len = hw * 2 + 2f;
            var ui = Object.FindFirstObjectByType<GameUI>();
            if (ui) ui.Toast("뿌우 — 열차가 지나간다!");
            Telegraph.Lane(from, dir, len, Width, Warn, Telegraph.Danger);
            // 선로 빛: 노랑 줄이 빠르게 깜빡임
            var glow = Telegraph.Lane(from, dir, len, 0.5f, Warn, Ch2Fx.Yellow);
            CameraFollow.Shake(0.05f);
            yield return new WaitForSeconds(Warn);
            if (glow) glow.Kill();
            ToyTrain.Run(from, dir, len, Speed, Width, PlayerDmg, 1f, EnemyDmg, 3);
        }
    }
}
