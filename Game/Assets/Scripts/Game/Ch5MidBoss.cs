// 5장 중간 보스 '거울 속의 고양이' (2026-11-20, 기획 docs/Cat_Tang_레벨_디자인.md '보스 상세 — 거울 속의 고양이')
// 모델 = Resources/Enemies/Ch5_MidBoss (주인공 고양이 + CatTang/MirrorCat 셰이더 · MirrorCatSkin: 맞을수록 금이 감)
// - 3페이즈: 거울(기본 사격) → 어긋남(66%: 잔상 돌진 · 시간 역행 추가, 패턴 간격 1.2배) → 조각(33%: 셋으로 쪼개짐, 그림자 있는 쪽만 진짜, 20초마다 다시 섞임)
// - 이동: 고양이와 같은 이속 4.5, 거리를 두고 사격 (1페이즈는 고양이 움직임을 거울처럼 따라 함)
// - 패턴: 쌍권총 연사 · 대시 베기 (가까움) / 조준 사격 · 반사 탄 (멂) / 건카타 궁 (거리 무관, 1 · 2페이즈)
// - 고유 기믹 3종 (쿨 20초, 직전 제외): 깨진 거울 벽 · 그림자 찾기 · 거울 너머 저격
// - 마지막 한 발 연출: minHp=1 → HP가 1이 되면 패턴 멈추고 슬로우 · 글리치 · 카메라 줌 → 플레이어가 쏘면 유리처럼 깨지며 사망
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Ch5MidBoss : BossBrain
    {
        public override float NearDist => 5f;
        public override float PatternGap => phase >= 2 ? 0.9f / 1.2f : 0.9f;
        public override string DisplayName => "미지의 존재";

        const float BurstTell = 0.5f, BurstGap = 0.15f, BurstDmg = 10f;
        const float SlashDash = 5f, SlashR = 2.5f, SlashHalf = 50f, SlashDmg = 28f;
        const float AimTrack = 0.8f, AimLock = 0.4f, AimSpeed = 14f, AimDmg = 25f;
        const float BounceSpeed = 8f, BounceDmg = 22f;
        const float KataTime = 2f, KataR = 8f, KataDmg = 8f;
        const float GhostDash = 5f, GhostDmg = 25f;
        const float GimmickFirst = 20f, GimmickEvery = 20f;
        public const float MirrorShardDmg = 25f, FakeShardDmg = 22f, SnipeDmg = 30f;

        int phase = 1;
        float nextGimmick, nextShuffle;
        string lastGimmick;
        bool inGimmick;
        bool lastShotActive;        // 마지막 한 발 연출 중
        readonly List<GameObject> spawned = new();
        readonly List<GameObject> fakes = new();
        readonly Queue<(float t, Vector3 p)> history = new();
        Transform model;

        protected override void Setup()
        {
            Add("쌍권총 연사", Reach.Near, Burst);
            Add("대시 베기", Reach.Near, DashSlash);
            Add("조준 사격", Reach.Far, AimShot);
            Add("반사 탄", Reach.Far, Bounce);
            Add("건카타 궁", Reach.Any, Kata, () => phase <= 2);
            Add("잔상 돌진", Reach.Any, GhostRush, () => phase >= 2);
            Add("시간 역행", Reach.Any, Rewind, () => phase >= 2);
            nextGimmick = Time.time + GimmickFirst;
            hp.minHp = 1;           // 마지막 한 발 연출용: HP 1에서 멈춤
        }

        void Start() { model = transform.Find("Model"); }

        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            if (lastShotActive) return;
            if (phase == 1 && hp.hp <= hp.maxHp * 0.66f) { phase = 2; BossFx.Toast("어긋남 — 거울 속 고양이가 어긋나기 시작한다"); }
            if (phase == 2 && hp.hp <= hp.maxHp * 0.33f && !busy) { phase = 3; StartCoroutine(Split()); }
            // 마지막 한 발: HP가 minHp(1)에 닿으면 패턴을 멈추고 연출 진입
            if (hp.hp <= hp.minHp && hp.minHp > 0 && !lastShotActive) { BeginLastShot(); return; }
            history.Enqueue((Time.time, transform.position));
            while (history.Count > 0 && Time.time - history.Peek().t > 3.2f) history.Dequeue();
            if (phase == 3 && Time.time >= nextShuffle && !busy) { nextShuffle = Time.time + 20f; Reshuffle(); }
        }

        protected override System.Func<IEnumerator> Forced()
        {
            if (inGimmick || Time.time < nextGimmick || phase == 3) return null;
            var all = new List<string> { "walls", "shadow", "snipe" };
            all.Remove(lastGimmick);
            lastGimmick = all[Random.Range(0, all.Count)];
            return () => Gimmick(lastGimmick);
        }

        // 1페이즈: 고양이 움직임을 거울처럼 (방 가운데 기준 좌우 반전 자리로) / 그 뒤: 6~9m 유지하며 옆으로
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (inGimmick || lastShotActive) return;
            Vector3 want;
            var c = RunManager.Instance ? RunManager.Instance.RoomCenter : Vector3.zero;
            if (phase == 1)
            {
                var pp = p.transform.position - c;
                want = c + new Vector3(-pp.x, 0, pp.z + (pp.z > 0 ? -7f : 7f));
            }
            else want = p.transform.position - dir * 7.5f + Vector3.Cross(Vector3.up, dir) * Mathf.Sin(Time.time * 0.8f) * 4f;
            var d = Effects.Flat(want - transform.position);
            if (d.magnitude > 0.3f) transform.position += d.normalized * Mathf.Min(spd, d.magnitude * 3f) * Time.deltaTime;
        }

        void Gun(Vector3 d, float speed, float dmg, float range = 16f) { Shoot(d, speed, dmg, 1.1f, range, EnemyProjectile.Style.Revolver); Sfx.Play("shot", 0.45f, 0.08f, 3); }

        // ---------- 패턴 ----------
        IEnumerator Burst()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 9f, 12f, T(BurstTell), Telegraph.Danger);
            yield return Wait(T(BurstTell));
            for (int i = 0; i < 6; i++)
            {
                d = ToPlayer();
                Face(d, 30f);
                Gun(Quaternion.Euler(0, Random.Range(-6f, 6f), 0) * d, 14f, BurstDmg);
                yield return Wait(T(BurstGap));
            }
            yield return Wait(T(0.3f));
        }

        IEnumerator DashSlash()
        {
            var d = ToPlayer();
            Telegraph.Lane(transform.position, d, SlashDash + SlashR, 1.4f, T(0.45f), Telegraph.Danger);
            yield return Wait(T(0.45f));
            Sfx.Play("dash", 0.7f, 0.05f, 2);
            Afterimage.Spawn(model, new Color(0.8f, 0.9f, 1f, 0.6f), 0.3f, false);
            for (float m = 0; m < SlashDash; m += 22f * Time.deltaTime) { transform.position += d * 22f * Time.deltaTime; yield return null; }
            Sfx.Play("e5_slash", 0.7f, 0.05f, 2);
            FxBurst.Spawn(transform.position + Vector3.up + d * 1.2f, 2f, FxBurst.Kind.Slash, new Color(0.85f, 0.9f, 1f));
            var p = P;
            if (p && p.health.Alive) { var to = Effects.Flat(p.transform.position - transform.position); if (to.magnitude <= SlashR && Vector3.Angle(d, to) <= SlashHalf) p.health.TakeDamage(SlashDmg); }
            yield return Wait(T(1f));                                           // 빈틈
        }

        IEnumerator AimShot()
        {
            var tele = Telegraph.Lane(transform.position, ToPlayer(), 16f, 0.25f, T(AimTrack + AimLock), Telegraph.Aim, transform);
            float end = Time.time + T(AimTrack);
            while (Time.time < end) { Face(ToPlayer(), 20f); yield return null; }
            var d = transform.forward;
            yield return Wait(T(AimLock));
            Gun(d, AimSpeed, AimDmg, 20f);
            yield return Wait(T(0.3f));
        }

        // 반사 탄: 벽에 한 번 튕기는 탄 2발 (방 경계에서 튕김)
        IEnumerator Bounce()
        {
            var d = ToPlayer();
            for (int i = -1; i <= 1; i += 2) StartCoroutine(BounceBullet(Quaternion.Euler(0, i * 25f, 0) * d));
            Sfx.Play("shot", 0.5f, 0.05f, 2);
            yield return Wait(T(0.6f));
        }

        IEnumerator BounceBullet(Vector3 d)
        {
            var go = Ch3Fx.Prim(PrimitiveType.Sphere, "BounceShot", null, Vector3.zero, Vector3.one * 0.45f, new Color(1f, 0.4f, 0.8f), 0f);
            spawned.Add(go);
            var pos = transform.position + Vector3.up * 1.1f + d;
            int bounces = 1;
            for (float t = 0; t < 4f && go; t += Time.deltaTime)
            {
                pos += d * BounceSpeed * Time.deltaTime;
                if (RunManager.Instance)
                {
                    var cl = RunManager.Instance.ClampToRoom(pos, 0.6f);
                    if ((Effects.Flat(cl - pos)).sqrMagnitude > 0.0001f)
                    {
                        if (bounces-- <= 0) break;
                        var n = Effects.Flat(cl - pos).normalized;
                        d = Vector3.Reflect(d, n); pos = new Vector3(cl.x, pos.y, cl.z);
                        FxBurst.Spawn(pos, 0.8f, FxBurst.Kind.Spark, new Color(1f, 0.5f, 0.85f));
                    }
                }
                go.transform.position = pos;
                var p = P;
                if (p && p.health.Alive && Effects.Flat(p.transform.position - pos).magnitude < 0.6f) { p.health.TakeDamage(BounceDmg); break; }
                yield return null;
            }
            if (go) Destroy(go);
        }

        IEnumerator Kata()
        {
            Telegraph.Ring(transform.position, KataR, T(0.6f), Telegraph.Danger, transform);
            yield return Wait(T(0.6f));
            for (int i = 0; i < 12; i++)
            {
                Gun(Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward, 11f, KataDmg, KataR);
                transform.Rotate(0, 60f, 0);
                yield return Wait(KataTime / 12f);
            }
        }

        IEnumerator GhostRush()
        {
            for (int i = 0; i < 3; i++)
            {
                var d = ToPlayer();
                Telegraph.Lane(transform.position, d, GhostDash, 1.2f, T(0.35f), Telegraph.Danger);
                yield return Wait(T(0.35f));
                Afterimage.Spawn(model, new Color(0.8f, 0.9f, 1f, 0.6f), 0.6f, false);
                bool hit = false;
                for (float m = 0; m < GhostDash; m += 20f * Time.deltaTime)
                {
                    var to = transform.position + d * 20f * Time.deltaTime;
                    if (RunManager.Instance) to = RunManager.Instance.ClampToRoom(to, 1f);
                    transform.position = to;
                    var p = P;
                    if (!hit && p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude < 1.2f) { p.health.TakeDamage(GhostDmg); hit = true; }
                    yield return null;
                }
            }
            yield return Wait(T(0.5f));
        }

        // 시간 역행: 3초 전 위치로 순간 이동 후 연사
        IEnumerator Rewind()
        {
            var back = history.Count > 0 ? history.Peek().p : transform.position;
            Afterimage.Spawn(model, new Color(0.7f, 0.85f, 1f, 0.6f), 0.5f, false);
            Sfx.Play("justdodge", 0.5f, 0f, 1);
            transform.position = back;
            yield return Wait(T(0.2f));
            yield return Burst();
        }

        // ---------- 3페이즈: 셋으로 쪼개짐 ----------
        IEnumerator Split()
        {
            busy = true;
            BossFx.Toast("조각 — 그림자가 있는 쪽만 진짜다!");
            Enemy.Fx3("GlassBurst", transform.position + Vector3.up * 1.2f, 1.4f);
            for (int i = 0; i < 2; i++) fakes.Add(MakeFake(transform.position + Quaternion.Euler(0, i * 120f + 60f, 0) * Vector3.forward * 4f));
            nextShuffle = Time.time + 20f;
            yield return Wait(0.6f);
            busy = false;
        }

        /// <summary>가짜 조각: 같은 모델 · 그림자 없음 · 한 발에 깨짐 (적 목록엔 안 넣음 → 방 클리어를 막지 않음)</summary>
        GameObject MakeFake(Vector3 at)
        {
            var go = new GameObject("MirrorCatFake");
            go.transform.position = at;
            go.transform.rotation = transform.rotation;
            var prefab = Resources.Load<GameObject>("Enemies/Ch5_MidBoss");
            if (prefab)
            {
                var m = Instantiate(prefab, go.transform, false);
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
                var skin = m.GetComponentInChildren<MirrorCatSkin>(); if (skin) skin.SetFake(true);
                if (model) m.transform.localScale = model.localScale;
            }
            var col = go.AddComponent<CapsuleCollider>(); col.radius = 0.6f; col.height = 2.2f; col.center = Vector3.up * 1.1f;
            go.AddComponent<PlayerOnlyTarget>();
            var h = go.AddComponent<Health>(); h.maxHp = 1; h.ResetHp(1);
            h.OnDeath += _ =>
            {
                var fm = go.GetComponentInChildren<MirrorCatSkin>();
                if (fm) GlassShatter.Play(fm.transform, 14, 0.7f);   // 가짜도 조각상처럼 깨짐 (작게)
                FakeShattered(go.transform.position); Destroy(go);
            };
            spawned.Add(go);
            return go;
        }

        void Reshuffle()
        {
            var pos = new List<Vector3> { transform.position };
            foreach (var f in fakes) if (f) pos.Add(f.transform.position);
            for (int i = pos.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pos[i], pos[j]) = (pos[j], pos[i]); }
            Enemy.Fx3("GlassBurst", transform.position + Vector3.up, 1f);
            transform.position = pos[0];
            int k = 1;
            foreach (var f in fakes) if (f && k < pos.Count) f.transform.position = pos[k++];
        }

        // ---------- 고유 기믹 ----------
        IEnumerator Gimmick(string id)
        {
            inGimmick = true;
            if (id == "walls") yield return MirrorWalls();
            else if (id == "shadow") yield return ShadowHunt();
            else yield return MirrorSnipe();
            foreach (var g in spawned) if (g && !fakes.Contains(g)) Destroy(g);
            Glitch.Show(model, true);
            hp.invulnerable = false;
            inGimmick = false;
            nextGimmick = Time.time + GimmickEvery;
        }

        Vector3 C => RunManager.Instance ? RunManager.Instance.RoomCenter : Vector3.zero;

        GameObject MirrorPanel(Vector3 at, Vector3 face, out Health h)
        {
            var go = new GameObject("MirrorPanel");
            go.transform.position = at;
            go.transform.rotation = Quaternion.LookRotation(face);
            spawned.Add(go);
            var vis = Kit.Place(RunManager.Instance ? RunManager.Instance.Chapter : null, "MirrorShard", go.transform, Vector3.zero, 0f, 1f, false);
            if (!vis) Ch3Fx.Prim(PrimitiveType.Cube, "Glass", go.transform, Vector3.up * 1.4f, new Vector3(2.2f, 2.8f, 0.15f), new Color(0.8f, 0.9f, 1f));
            h = Ch3Fx.Target(go, 1.1f, 2.8f);
            return go;
        }

        // ① 깨진 거울 벽: 거울 4장이 솟고 거울마다 보스 탄을 되쏴 줌(반사). 거울을 쏘면 깨지며 유리 조각이 보스 타격 25. 10초
        IEnumerator MirrorWalls()
        {
            BossFx.Toast("깨진 거울 벽 — 거울을 쏴서 깨라!");
            var mirrors = new List<(GameObject go, Health h)>();
            for (int i = 0; i < 4; i++)
            {
                var at = C + Quaternion.Euler(0, i * 90f + 45f, 0) * Vector3.forward * 7f;
                var go = MirrorPanel(at, Effects.Flat(C - at).normalized, out var h);
                bool broken = false;
                var hh = h;
                h.OnDamaged += _ => { if (broken) return; broken = true; Enemy.Fx3("GlassBurst", hh.transform.position + Vector3.up * 1.4f, 1.2f); hp.TakeDamage(MirrorShardDmg); hh.gameObject.SetActive(false); };
                mirrors.Add((go, h));
                Enemy.Fx3("MirrorFlash", at + Vector3.up * 1.4f, 1f);
            }
            float end = Time.time + 10f, nextShot = 0;
            while (Time.time < end && hp.Alive)
            {
                bool any = false;
                if (Time.time >= nextShot)
                {
                    nextShot = Time.time + 1.1f;
                    Gun(ToPlayer(), 12f, 18f);
                    foreach (var m in mirrors)
                    {
                        if (!m.go || !m.h.gameObject.activeSelf) continue;
                        any = true;
                        var p = P; if (!p) break;
                        var d = Effects.Flat(p.transform.position - m.go.transform.position).normalized;
                        EnemyProjectile.Spawn(m.go.transform.position + Vector3.up * 1.2f + d, d, 10f, 18f, 18f, EnemyProjectile.Style.Revolver, 0);
                    }
                }
                else foreach (var m in mirrors) if (m.go && m.h.gameObject.activeSelf) any = true;
                if (!any) break;
                yield return null;
            }
        }

        // ② 그림자 찾기: 어두워지고 거울 고양이 6마리(진짜 1 + 가짜 5)가 빙빙 돎. 그림자 있는 진짜를 8초 안에 맞히면 3초 기절, 가짜를 맞히면 깨지며 유리 조각 22
        IEnumerator ShadowHunt()
        {
            BossFx.Toast("그림자 찾기 — 그림자가 있는 진짜를 맞혀라!");
            var p = P;
            if (!p) yield break;
            var ring = new List<GameObject>();
            for (int i = 0; i < 5; i++) ring.Add(MakeFake(transform.position));
            float start = Time.time, end = start + 8f;
            float lastHit = hp.lastHitAt;
            bool found = false;
            float ang0 = Random.Range(0f, 360f);
            int realSlot = Random.Range(0, 6);
            while (Time.time < end && hp.Alive)
            {
                float a = ang0 + (Time.time - start) * 50f;
                var pc = Effects.Flat(p.transform.position);
                for (int i = 0, k = 0; i < 6; i++)
                {
                    var at = pc + Quaternion.Euler(0, a + i * 60f, 0) * Vector3.forward * 6f;
                    if (RunManager.Instance) at = RunManager.Instance.ClampToRoom(at, 1f);
                    if (i == realSlot) transform.position = new Vector3(at.x, transform.position.y, at.z);
                    else { if (k < ring.Count && ring[k]) ring[k].transform.position = new Vector3(at.x, ring[k].transform.position.y, at.z); k++; }
                }
                Face(ToPlayer(), 10f);
                if (hp.lastHitAt > lastHit) { found = true; break; }
                yield return null;
            }
            foreach (var f in ring) if (f) { Enemy.Fx3("GlassBurst", f.transform.position + Vector3.up, 0.8f); Destroy(f); }
            if (found) { BossFx.Toast("진짜를 찾았다 — 기절!"); hp.ApplyStun(3f); yield return Wait(0.3f); }
        }

        /// <summary>그림자 찾기의 가짜가 맞으면: 깨지며 유리 조각이 고양이 타격</summary>
        public static void FakeShattered(Vector3 at)
        {
            Enemy.Fx3("GlassBurst", at + Vector3.up, 1f);
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude < 3.5f) p.health.TakeDamage(FakeShardDmg);
        }

        // ③ 거울 너머 저격: 네 벽 거울을 오가며 저격 (조준선 1초, 피해 30). 그가 있는 거울을 맞히면 끌어내 2초 기절. 10초
        IEnumerator MirrorSnipe()
        {
            BossFx.Toast("거울 너머 저격 — 그가 비친 벽 거울을 쏴라!");
            var c = C;
            var size = RunManager.Instance && RunManager.Instance.CurrentRoom != null ? RunManager.Instance.CurrentRoom.size : new Vector2(28, 24);
            var spots = new[] { c + new Vector3(0, 0, size.y / 2 - 1.2f), c + new Vector3(0, 0, -size.y / 2 + 1.2f), c + new Vector3(size.x / 2 - 1.2f, 0, 0), c + new Vector3(-size.x / 2 + 1.2f, 0, 0) };
            var hs = new List<Health>();
            foreach (var s in spots) { MirrorPanel(s, Effects.Flat(c - s).normalized, out var h); hs.Add(h); }
            Glitch.Show(model, false);
            hp.invulnerable = true;
            float end = Time.time + 10f;
            bool pulled = false;
            int at = Random.Range(0, 4);
            while (Time.time < end && !pulled)
            {
                at = (at + Random.Range(1, 4)) % 4;
                var mirror = spots[at];
                transform.position = new Vector3(mirror.x, transform.position.y, mirror.z);
                Enemy.Fx3("MirrorFlash", mirror + Vector3.up * 1.4f, 1f);
                float hitBase = hs[at].lastHitAt;
                var p = P; if (!p) break;
                var d = Effects.Flat(p.transform.position - mirror).normalized;
                Telegraph.Lane(mirror, d, 26f, 0.3f, 1f, Telegraph.Aim);
                for (float t = 0; t < 1f; t += Time.deltaTime) { if (hs[at].lastHitAt > hitBase) { pulled = true; break; } yield return null; }
                if (pulled) break;
                EnemyProjectile.Spawn(mirror + Vector3.up * 1.2f + d, d, 22f, SnipeDmg, 28f, EnemyProjectile.Style.Sniper, 0);
                Sfx.Play("shot", 0.8f, 0f, 2);
                for (float t = 0; t < 0.6f; t += Time.deltaTime) { if (hs[at].lastHitAt > hitBase) { pulled = true; break; } yield return null; }
            }
            hp.invulnerable = false;
            Glitch.Show(model, true);
            var inward = Effects.Flat(c - transform.position).normalized;
            transform.position += inward * 2.5f;
            if (pulled) { BossFx.Toast("거울에서 끌어냈다 — 기절!"); Enemy.Fx3("GlassBurst", transform.position + Vector3.up, 1.2f); hp.ApplyStun(2f); yield return Wait(0.3f); }
        }

        // ---------- 마지막 한 발 연출 ----------
        // HP 1 → 가짜 전부 깨짐 → 글리치 떨며 무릎(기울어짐) → 슬로우 → 플레이어가 쏘면 유리처럼 깨지며 사망
        void BeginLastShot()
        {
            lastShotActive = true;
            busy = true;
            StopAllCoroutines();                    // 패턴 · 기믹 멈춤
            StartCoroutine(LastShotSceneInner());    // StopAllCoroutines 뒤 바로 새 코루틴 시작은 안전
        }

        IEnumerator LastShotSceneInner()
        {
            // 가짜 전부 깨뜨리기
            foreach (var f in fakes) if (f) { Enemy.Fx3("GlassBurst", f.transform.position + Vector3.up, 0.9f); Destroy(f); }
            fakes.Clear();
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear();

            // 글리치 떨림 + 모델을 약간 앞으로 기울여 비틀거림
            hp.invulnerable = true;
            if (model) StartCoroutine(Glitch.Shake(model, 0.6f, 0.35f));
            Sfx.Play("e_glass", 0.7f, 0f, 2);
            CameraFollow.Shake(0.15f);

            // 슬로우 (실제 시간 기준으로 연출)
            float savedTimeScale = Time.timeScale;
            Time.timeScale = 0.15f;
            CameraFollow.Zoom(0.82f);

            yield return new WaitForSecondsRealtime(0.4f);

            // 모델을 약간 기울임 (무릎 꿇는 대신 비틀거리는 느낌)
            if (model) model.localRotation = Quaternion.Euler(12f, 0, 5f);

            // 무적 풀고 minHp 없앰 → 플레이어가 쏘면 죽음
            hp.invulnerable = false;
            hp.minHp = 0;

            // 플레이어가 보스를 맞힐 때까지 대기 (최대 10초 실제 시간)
            float lastHit = hp.lastHitAt;
            float deadline = Time.unscaledTime + 10f;
            while (hp.Alive && Time.unscaledTime < deadline)
            {
                // 계속 글리치 · 떨림
                if (model && Random.value < 0.1f)
                {
                    var side = model.parent ? model.parent.right : Vector3.right;
                    Afterimage.SpawnAt(model, model.position + side * Random.Range(-0.3f, 0.3f), Glitch.Red, 0.15f);
                }
                yield return null;
            }

            // 타임아웃이면 직접 처리
            if (hp.Alive) hp.TakeDamage(hp.hp + 1);

            // 최종 사망 연출: 큰 유리 파열 + 히트스톱 + 흔들림
            Enemy.Fx3("GlassBurst", transform.position + Vector3.up * 1.3f, 2.5f);
            Enemy.Fx3("MirrorFlash", transform.position + Vector3.up * 1.5f, 2f);
            CameraFollow.Shake(0.25f);
            Sfx.Play("e3b_shards", 0.9f, 0f, 2);

            yield return new WaitForSecondsRealtime(0.3f);

            // 시간 · 카메라 복구
            Time.timeScale = 1f;
            CameraFollow.Zoom(1f);

            // 모델 기울기 되돌림 (Die에서 파괴되지만 혹시 남으면)
            if (model) model.localRotation = Quaternion.identity;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (lastShotActive) { Time.timeScale = 1f; CameraFollow.Zoom(1f); }
            foreach (var g in spawned) if (g) Destroy(g);
            foreach (var f in fakes) if (f) Destroy(f);
        }
    }

}
