// 4장 (시계 장치 속) 방 꾸미기 + 방 기믹 · 오브젝트 (docs/Cat_Tang_레벨_디자인.md 챕터 4 · 수치 기준 (챕터 4))
// - 장소(스테이지)별 벽면: 1 태엽 창고 → 2 톱니 기관실 → 3 종루 → 4 문자판 뒤 회랑 (아래 → 위로 오를수록 별밤이 더 보임)
// - 돌아가는 톱니 바닥(방 기믹): 보스 · 중간 보스 방 빼고 방마다 2~3개, 지름 6~9m, 초당 20°(이웃 톱니는 반대 방향),
//   올라선 고양이 · 적이 함께 실려 돎, 맞물린 이빨 자리(폭 1.5m)에 있으면 0.5초마다 피해 10 + 바깥으로 밀려남,
//   가운데 축(반경 0.8m)을 쏘면 회전 방향이 바뀜(다시 쓰려면 3초)
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
        public static readonly string[] GroundBits = { "CogBits", "ScrewBits", "FloorGrate" };
        public static float[] GroundWeights() => Stage >= 3 ? new[] { 0.6f, 0.6f, 1.4f } : new[] { 1.2f, 1f, 0.6f };

        // ---------- 벽면 (Kit.BuildingRow가 씀) ----------
        public static (string[] names, float[] widths) Facades()
        {
            if (BossRoom || MidBossRoom) return (new[] { "Facade_Pendulum", "Facade_Gears", "Facade_DialBack" }, new[] { 6f, 6f, 8f });
            switch (Stage)
            {
                case 0: return (new[] { "Facade_Storehouse", "Wall" }, new[] { 6f, 4f });
                case 1: return (new[] { "Facade_Gears", "Facade_Pendulum", "Wall" }, new[] { 6f, 6f, 4f });
                case 2: return (new[] { "Facade_BellArch", "Facade_Pendulum", "Wall" }, new[] { 6f, 6f, 4f });
                default: return (new[] { "Facade_DialBack", "Facade_Girder" }, new[] { 8f, 6f });
            }
        }

        /// <summary>문자판 뒤 회랑(철골 난간)은 키가 낮아 남쪽 벽을 덜 낮춤</summary>
        public static float SouthWallMul() => Stage >= 3 ? 0.85f : 0.6f;

        /// <summary>큰 지형지물: 아직 4장 지형지물 모델이 없음 (있으면 RoomBuilder.AddShape가 프리팹 경계로 잼)</summary>
        public static string[] Landmarks() => new[] { "LM_Clockwork", "LM_PendulumCase", "LM_GearStack" };

        // ---------- 소품 · 오브젝트 (RoomBuilder.KitProps 대신) ----------
        public static void Props(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            if (boss)
            {
                // 꼭대기 문자판: 원형 경계(밖은 별밤 허공)만. 문자판 바닥 · 쌍둥이 기믹은 보스(Ch4Bosses)가 맡음
                Ch2Kit.ArenaFence(root, ArenaR);
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
            string[] kinds = { "GearCrate", "BrokenClockPillar", "SpringBarrel" };
            int cover = RoomBuilder.Scaled(rnd.Next(3, 6), info);
            for (int k = 0; k < cover && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, kinds[rnd.Next(kinds.Length)], root, Next(), rnd.Next(4) * 90f + rnd.Next(-12, 12));
                if (go) Breakable.Make(go, CoverHp);
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
    /// 이웃 톱니와 맞물린 이빨 자리(폭 1.5m)에 있으면 0.5초마다 피해 10 + 바깥으로 밀려남(적도 같음).
    /// 가운데 축(반경 0.8m)을 쏘면 회전 방향이 바뀜(다시 쓰려면 3초)
    /// </summary>
    public class GearFloor : MonoBehaviour
    {
        public const float DegPerSec = 20f, MeshWidth = 1.5f, MeshDmg = 10f, MeshTick = 0.5f, MeshPush = 7f, HubR = 0.8f, ReverseCd = 3f;
        public static readonly List<GearFloor> All = new();

        public float radius = 4f;
        public int dir = 1;                     // +1 = 위에서 볼 때 시계 방향
        float reverseReady, nextMeshTick;
        CharacterController playerCc;

        public static void Make(GameObject go, float radius, int dir)
        {
            var g = go.AddComponent<GearFloor>();
            g.radius = radius; g.dir = dir;
            // 축: 쏠 수 있는 작은 원기둥 충돌체 (톱니 메시는 밟고 지나가므로 충돌 없음)
            var hub = new GameObject("Hub");
            hub.transform.SetParent(go.transform, false);
            hub.transform.localPosition = Vector3.up * 0.6f / Mathf.Max(0.01f, go.transform.localScale.y);
            var col = hub.AddComponent<CapsuleCollider>();
            col.radius = HubR / Mathf.Max(0.01f, go.transform.localScale.x);
            col.height = 1.4f / Mathf.Max(0.01f, go.transform.localScale.y);
            var h = Ch4Kit.MakeHp(hub, 1e6f);
            h.OnDamaged += _ => { h.ResetHp(h.maxHp); g.Reverse(); };
            DangerEdge.Add(hub);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Reverse()
        {
            if (Time.time < reverseReady) return;
            reverseReady = Time.time + ReverseCd;
            dir = -dir;
            // 맞물린 이웃도 같이 뒤집혀야 이빨이 맞음
            foreach (var o in All)
                if (o != this && Touching(this, o)) { o.dir = -dir; o.reverseReady = reverseReady; }
            var c = transform.position;
            FxBurst.Spawn(c + Vector3.up * 0.6f, 1.4f, FxBurst.Kind.Spark, Ch4Kit.Friendly);
            CameraFollow.Shake(0.05f);
        }

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

            // 맞물린 이빨 자리 (한 쌍당 한 번: 인덱스가 작은 쪽이 처리)
            if (Time.time < nextMeshTick) return;
            nextMeshTick = Time.time + MeshTick;
            int me = All.IndexOf(this);
            foreach (var o in All)
            {
                if (o == this || All.IndexOf(o) < me || !Touching(this, o)) continue;
                var oc = o.transform.position; oc.y = 0;
                var axis = (oc - c).normalized;
                var mesh = c + axis * radius;
                var side = Vector3.Cross(Vector3.up, axis);
                if (p && p.health.Alive)
                {
                    var d = Effects.Flat(p.transform.position - mesh);
                    if (d.magnitude < MeshWidth)
                    {
                        p.health.TakeDamage(MeshDmg);
                        p.health.Knockback((Vector3.Dot(d, side) >= 0 ? side : -side) * MeshPush);
                    }
                }
                foreach (var e in Enemy.All.ToArray())
                {
                    if (!e || !e.health.Alive || e.health.IsBoss) continue;
                    var d = Effects.Flat(e.transform.position - mesh);
                    if (d.magnitude >= MeshWidth) continue;
                    e.health.TakeDamage(MeshDmg);
                    e.health.Knockback((Vector3.Dot(d, side) >= 0 ? side : -side) * MeshPush);
                }
                FxBurst.Spawn(mesh + Vector3.up * 0.2f, 0.8f, FxBurst.Kind.Spark, Ch4Kit.Red);
            }
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
