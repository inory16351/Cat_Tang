// 3장 '신기루 사막 = 거울 세계' 일반 적 5종 (레벨 디자인 md '챕터 3' · '수치 기준 (챕터 3)', 2026-11-18)
// 체력 · 피해는 md 값 그대로 (챕터 배율 안 곱함). 강화형 체력 1.5배는 EnemyFactory.MakeElite.
//
// - 거울 사수(MirrorShooter): 8~11m 유지, 2.5초마다 반사탄 1발(예고 0.6초, 탄속 10, 피해 12). 벽 · 거울 조각에 1번 튕김 → 엄폐물 뒤도 안전하지 않음
//   강화형 산탄 반사: 튕길 때 3갈래(25°)로 갈라짐
// - 빛가루 법사(LightMage): 9m 유지, 6초마다 등 뒤에 빛 구슬 5개(1.5초에 걸쳐 생김) → 0.4초 간격으로 하나씩, 5m/초 · 초당 90° 유도 · 4초 뒤 소멸, 발당 12
//   강화형 구슬 폭발: 죽을 때 남은 구슬 자리마다 작은 탄 8방향(5m/초, 12)
// - 거울 너머 검객(MirrorBlade): 이속 3.6, 거울에 들어가 2초 잠복(무적) → 고양이와 가장 가까운 거울(4m 안)이 0.7초 빛나며 예고 → 튀어나와
//   전방 100° · 2.5m 베기(16) → 1초 빈틈. 4m 안에 거울이 없으면 걸어서 다가와 같은 베기
//   강화형 2연타: 첫 베기 뒤 다른 거울이 0.5초 빛나고 한 번 더 튀어나와 벰
// - 유리 기사(GlassKnight): 이속 2.6, 정면 120° 탄을 고양이 쪽으로 되튕김(되튕긴 탄 8, 기사는 피해 없음). 정면에 4발 맞으면 거울이 깨져 5초 반사 없음
//   방패 밀치기: 예고 0.7초 → 전방 3m 돌진, 16. 강화형 조각 폭발: 거울이 깨질 때 0.5초 금 → 반경 2.5m 폭발 15
// - 신기루 분신술사(Trickster): 이속 3.0, 9m 유지, 8초마다 분신 2개(필드에 최대 2) + 셋이 0.6초 동안 자리를 섞음.
//   본체 · 분신 모두 3초마다 빛 탄 1발(8m/초, 12). 분신은 한 발에 소멸, 본체를 잡으면 분신도 사라짐
//   강화형: 분신 3개, 분신이 터진 자리에 빛가루 장판 반경 1.5m · 3초 · 이속 30% 감소
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        // ---------- 수치 ----------
        public const float ShooterNear = 8f, ShooterFar = 11f, ShooterInterval = 2.5f, ShooterAim = 0.6f, ShooterSpeed = 10f, ShooterSplit = 25f;
        public const float MageKeep = 9f, MageInterval = 6f, MageConjure = 1.5f, MageGap = 0.4f, MageOrbSpeed = 5f, MageHoming = 90f, MageOrbLife = 4f;
        public const int MageOrbs = 5;
        public const float BladeHide = 2f, BladeWarn = 0.7f, BladeHalf = 50f, BladeRange = 2.5f, BladeOpen = 1f, BladeMirrorReach = 4f, BladeDiveCooldown = 5f, BladeWindup = 0.45f, BladeWarn2 = 0.5f;
        public const float KnightShieldHalf = 60f, KnightTurn = 150f, KnightBreakHits = 4, KnightBroken = 5f, KnightBashWindup = 0.7f, KnightBashDist = 3f, KnightBashSpeed = 11f, KnightBashCooldown = 3f;
        public const float KnightReflectDamage = 8f, KnightReflectSpeed = 14f, KnightBurstRadius = 2.5f, KnightBurstDamage = 15f, KnightBurstFuse = 0.5f;
        public const float TrickKeep = 9f, TrickCloneInterval = 8f, TrickShuffle = 0.6f, TrickShotInterval = 3f, TrickShotSpeed = 8f;
        public const float TrickPuddleRadius = 1.5f, TrickPuddleLife = 3f, TrickPuddleSlow = 0.3f;
        static readonly Color Ch3Light = new Color(0.75f, 0.95f, 1f), Ch3Pink = new Color(1f, 0.7f, 0.9f);

        /// <summary>3장 이펙트 프리팹 (Resources/FX/Prefabs/FX_Ch3_*: 블렌더 3D 메시 조각 — 유리 파편 · 반사 빛살 · 고리 · 반짝임, 2026-11-18 사용자: 그림 말고 블렌더).
        /// 툰 불꽃 재질에 색을 입힘: 거울 · 유리 = 옅은 하늘빛, 빛가루 = 분홍 라벤더. 없으면 불똥으로 대신</summary>
        public static void Fx3(string name, Vector3 at, float scale = 1f)
        {
            var tint = name == "Stardust" ? new Color(1f, 0.72f, 0.95f) : new Color(0.62f, 0.92f, 1f);
            if (!FxEffect.Spawn("FX_Ch3_" + name, at, Quaternion.identity, scale, tint)) FxBurst.Spawn(at, 1.4f * scale, FxBurst.Kind.Spark, Ch3Light);
        }

        float nextSpecial, mirrorBrokenUntil;
        int mirrorHits;
        readonly List<EnemyProjectile> heldOrbs = new();
        public Enemy cloneOwner;                                   // 분신이면 본체
        readonly List<Enemy> clones = new();
        bool IsClone => cloneOwner != null;

        /// <summary>죽을 때 3장 처리 (Die에서 부름)</summary>
        void OnDieCh3()
        {
            switch (kind)
            {
                case Kind.LightMage:
                    // 남은(아직 안 쏜) 구슬: 강화형이면 자리마다 작은 탄 8방향, 아니면 그냥 흩어짐
                    foreach (var o in heldOrbs)
                    {
                        if (!o || !o.enabled) continue;
                        if (elite)
                            for (int k = 0; k < 8; k++)
                            {
                                var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward;
                                EnemyProjectile.Spawn(o.transform.position, d, MageOrbSpeed, projectileDamage, 10f, EnemyProjectile.Style.Pellet);
                            }
                        Fx3(elite ? "GlassBurst" : "Stardust", o.transform.position, elite ? 0.6f : 0.5f);
                        o.Vanish();
                    }
                    heldOrbs.Clear();
                    break;
                case Kind.Trickster:
                    if (IsClone)
                    {
                        Fx3("Stardust", transform.position + Vector3.up, 1f);
                        if (cloneOwner && cloneOwner.elite) MeltPuddle.Spawn(transform.position, TrickPuddleRadius, TrickPuddleLife, TrickPuddleSlow, new Color(0.95f, 0.8f, 1f));
                        if (cloneOwner) cloneOwner.clones.Remove(this);
                    }
                    else
                        foreach (var c in clones.ToArray()) if (c && c.health.Alive) c.health.TakeDamage(c.health.maxHp * 10f);   // 본체를 잡으면 분신도 흩어짐
                    break;
            }
        }

        // ================= 거울 사수 =================
        void ShooterUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, ShooterNear, ShooterFar);
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(ShooterShot(p));
        }

        IEnumerator ShooterShot(PlayerController p)
        {
            busy = true;
            Anim("Attack");
            var d = ToP(p);
            // 손거울로 빛을 모음: 따라오는 조준선
            tele = Telegraph.Lane(transform.position + d * 0.6f, d, 11f, 0.16f, ShooterAim / AtkMul, Telegraph.Aim);
            float end = Time.time + ShooterAim / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                d = Vector3.Slerp(d, ToP(p), 4f * Time.deltaTime).normalized;
                if (tele) tele.Kill();
                tele = Telegraph.Lane(transform.position + d * 0.6f, d, 11f, 0.16f, 0.05f, Telegraph.Aim);
                Face(d);
                yield return null;
            }
            if (ok)
            {
                var pr = EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + d * 0.7f, d, ShooterSpeed, projectileDamage, 24f, EnemyProjectile.Style.Orb);
                pr.bounces = 1;
                if (elite) pr.OnBounced = (b, at) =>
                {
                    // 산탄 반사: 튕긴 자리에서 3갈래 (가운데는 원래 탄)
                    foreach (float a in new[] { -ShooterSplit, ShooterSplit })
                        EnemyProjectile.Spawn(at + b.Dir * 0.2f + Vector3.up * 0.9f * 0, Quaternion.Euler(0, a, 0) * b.Dir, ShooterSpeed, projectileDamage, 14f, EnemyProjectile.Style.Orb).mirrored = true;
                };
                GunkataFx.Flare(transform.position + Vector3.up * 0.9f + d * 0.7f, 0.9f, Ch3Light);
            }
            nextAttack = Time.time + ShooterInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        // ================= 빛가루 법사 =================
        void MageUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, MageKeep - 1f, MageKeep + 1f);
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(MageOrbsRun(p));
        }

        IEnumerator MageOrbsRun(PlayerController p)
        {
            busy = true;
            Anim("Attack");
            heldOrbs.Clear();
            var back = -Effects.Flat(transform.forward).normalized;
            var side = Vector3.Cross(Vector3.up, back);
            // 등 뒤 부채꼴로 하나씩 생김 (1.5초)
            for (int i = 0; i < MageOrbs; i++)
            {
                if (!health.Alive) yield break;
                float u = (i - (MageOrbs - 1) / 2f) / ((MageOrbs - 1) / 2f);
                var at = transform.position + back * 0.9f + side * u * 1.4f + Vector3.up * (1.8f + 0.35f * (1 - Mathf.Abs(u)));
                var o = EnemyProjectile.Spawn(at, ToP(p), MageOrbSpeed, projectileDamage, MageOrbSpeed * MageOrbLife, EnemyProjectile.Style.Orb);
                o.frozenUntil = float.MaxValue;
                heldOrbs.Add(o);
                Fx3("Stardust", at, 0.45f);
                yield return Hold(MageConjure / MageOrbs, _ => { });
                Face(ToP(p));
            }
            // 하나씩 느리게 유도해 날림
            for (int i = 0; i < heldOrbs.Count; i++)
            {
                if (!health.Alive) yield break;
                var o = heldOrbs[i];
                if (o && o.enabled)
                {
                    o.frozenUntil = 0;
                    o.Retarget(p.transform.position - o.transform.position);
                    o.homing = MageHoming;
                }
                heldOrbs[i] = null;
                yield return Hold(MageGap, _ => { });
            }
            heldOrbs.Clear();
            nextAttack = Time.time + MageInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        // ================= 거울 너머 검객 =================
        void BladeUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (Time.time >= nextSpecial && MirrorShard.All.Count > 0 && dist > 3f)
            {
                StartCoroutine(BladeDive(p));
                return;
            }
            if (dist <= BladeRange + 0.3f && Time.time >= nextAttack) { StartCoroutine(BladeSlash(p, null)); return; }
            transform.position += dir * spd * Time.deltaTime;
            Face(dir);
        }

        static MirrorShard NearestShard(Vector3 to, float within, MirrorShard except)
        {
            MirrorShard best = null; float bd = within;
            foreach (var m in MirrorShard.All)
            {
                if (!m || m == except) continue;
                float d = Vector3.Distance(Effects.Flat(m.transform.position), Effects.Flat(to));
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }

        void SetHidden(bool hide)
        {
            health.invulnerable = hide;
            if (model) model.gameObject.SetActive(!hide);
            var col = GetComponent<Collider>(); if (col) col.enabled = !hide;
            var bar = transform.Find("HpBar"); if (bar) bar.gameObject.SetActive(!hide);
        }

        IEnumerator BladeDive(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + BladeDiveCooldown * Random.Range(0.85f, 1.2f);
            // 빛가루로 흩어지며 거울 속으로 (2초 잠복, 무적)
            var from = transform.position;
            Fx3("Stardust", from + Vector3.up, 1.1f);
            SetHidden(true);
            yield return Hold(BladeHide, _ => { });
            var m = p ? NearestShard(p.transform.position, BladeMirrorReach, null) : null;
            if (m)
            {
                yield return BladeEmerge(p, m, BladeWarn);
                if (elite && health.Alive)
                {
                    var m2 = p ? NearestShard(p.transform.position, BladeMirrorReach + 2f, m) : null;
                    if (m2)
                    {
                        Fx3("Stardust", transform.position + Vector3.up, 0.9f);
                        SetHidden(true);
                        yield return BladeEmerge(p, m2, BladeWarn2);
                    }
                }
            }
            else
            {
                // 4m 안에 거울이 없으면 들어간 자리에서 다시 나와 걸어감
                SetHidden(false);
                Fx3("Stardust", from + Vector3.up, 0.9f);
            }
            busy = false;
        }

        /// <summary>거울이 warn초 빛난 뒤 그 앞으로 튀어나와 벰</summary>
        IEnumerator BladeEmerge(PlayerController p, MirrorShard m, float warn)
        {
            var mpos = Effects.Flat(m.transform.position);
            var toP = p ? Effects.Flat(p.transform.position) - mpos : Vector3.forward;
            var d = toP.sqrMagnitude > 1e-4f ? toP.normalized : Vector3.forward;
            var exit = mpos + d * 0.6f;
            var rm = RunManager.Instance; if (rm) exit = rm.ClampToRoom(exit, 1f);
            m.Glow(warn);
            var t = Telegraph.Sector(exit, d, BladeRange, BladeHalf, warn / AtkMul, Telegraph.Danger);
            yield return Hold(warn, _ => { });
            if (t) t.Kill();
            transform.position = new Vector3(exit.x, transform.position.y, exit.z);
            transform.rotation = Quaternion.LookRotation(d);
            SetHidden(false);
            Fx3("GlassBurst", exit + Vector3.up, 0.9f);
            yield return BladeSlash(p, d, true);
        }

        /// <summary>전방 100° · 2.5m 베기 → 1초 빈틈. 거울에서 튀어나올 땐 예고가 이미 끝나 바로 벰</summary>
        IEnumerator BladeSlash(PlayerController p, Vector3? fixedDir, bool instant = false)
        {
            busy = true;
            Anim("Attack");
            var d = fixedDir ?? ToP(p);
            bool ok = true;
            if (!instant)
            {
                tele = Telegraph.Sector(transform.position, d, BladeRange, BladeHalf, BladeWindup / AtkMul, Telegraph.Danger, transform);
                yield return Hold(BladeWindup, r => ok = r);
            }
            if (ok && health.Alive)
            {
                if (p && p.health.Alive)
                {
                    var to = Effects.Flat(p.transform.position - transform.position);
                    if (to.magnitude <= BladeRange && Vector3.Angle(d, to) <= BladeHalf) p.health.TakeDamage(contactDamage);
                }
                GunkataFx.HitImpact(transform.position + d * 1.4f + Vector3.up, d, Ch3Light, false, 1.1f);
                PropHit.Area(transform.position + d * 1.4f, BladeRange * 0.8f, contactDamage);
                yield return Hold(BladeOpen, _ => { });                            // 벤 뒤 빈틈
            }
            nextAttack = Time.time + 1.2f / AtkMul;
            if (!fixedDir.HasValue) busy = false;
        }

        // ================= 유리 기사 =================
        public bool MirrorUp => kind == Kind.GlassKnight && Time.time >= mirrorBrokenUntil;

        /// <summary>Health.ReflectFrom: 정면 ±60°에서 온 탄을 되튕김. 4발째에 거울이 깨짐 (그 뒤 5초는 정상 피해)</summary>
        public bool KnightReflects(Vector3 d)
        {
            if (!MirrorUp || !health.Alive) return false;
            var from = -Effects.Flat(d);
            if (from.sqrMagnitude < 1e-4f || Vector3.Angle(Effects.Flat(transform.forward), from) > KnightShieldHalf) return false;
            mirrorHits++;
            if (mirrorHits >= KnightBreakHits) StartCoroutine(KnightMirrorBreak());
            return true;
        }

        IEnumerator KnightMirrorBreak()
        {
            mirrorHits = 0;
            mirrorBrokenUntil = Time.time + KnightBroken;
            var shield = model ? model.Find("Mirror") : null;
            var at = transform.position + Effects.Flat(transform.forward) * 0.8f;
            Fx3("GlassBurst", at + Vector3.up * 1.1f, 1.1f);
            if (shield) shield.gameObject.SetActive(false);
            if (elite)
            {
                // 조각 폭발: 0.5초 금이 번진 뒤 반경 2.5m
                var t = Telegraph.Ring(at, KnightBurstRadius, KnightBurstFuse, Telegraph.Danger);
                yield return new WaitForSeconds(KnightBurstFuse);
                var p = PlayerController.Instance;
                if (p && p.health.Alive && Vector3.Distance(Effects.Flat(p.transform.position), Effects.Flat(at)) <= KnightBurstRadius) p.health.TakeDamage(KnightBurstDamage);
                GunkataFx.Explosion(at, KnightBurstRadius, false);
                PropHit.Area(at, KnightBurstRadius, KnightBurstDamage);
            }
            while (Time.time < mirrorBrokenUntil) yield return null;
            if (shield && health.Alive) { shield.gameObject.SetActive(true); Fx3("MirrorFlash", transform.position + Effects.Flat(transform.forward) * 0.8f + Vector3.up * 1.1f, 0.8f); }
        }

        void KnightUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            // 몸 돌리는 속도가 느림 (등 · 옆을 노릴 틈)
            var want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, KnightTurn * Time.deltaTime);
            if (dist < KnightBashDist + 1.2f && Time.time >= nextAttack) { StartCoroutine(KnightBash(p)); return; }
            float facing = Vector3.Angle(Effects.Flat(transform.forward), dir);
            if (dist > 1.8f && facing < 70f) transform.position += Effects.Flat(transform.forward).normalized * spd * Time.deltaTime;
        }

        IEnumerator KnightBash(PlayerController p)
        {
            busy = true;
            Anim("Attack");
            var d = Effects.Flat(transform.forward).normalized;
            tele = Telegraph.Lane(transform.position, d, KnightBashDist + 0.8f, 1.8f, KnightBashWindup / AtkMul, Telegraph.Danger);
            bool ok = false;
            yield return Hold(KnightBashWindup, r => ok = r);
            if (ok)
            {
                float moved = 0; bool hit = false;
                while (moved < KnightBashDist && health.Alive)
                {
                    float step = KnightBashSpeed * Time.deltaTime;
                    transform.position += d * step; moved += step;
                    if (!hit && p && p.health.Alive && Vector3.Distance(Effects.Flat(p.transform.position), Effects.Flat(transform.position + d * 0.8f)) < 1.3f)
                    {
                        hit = true;
                        p.health.TakeDamage(contactDamage);
                        p.health.Knockback(d * 6f);
                    }
                    yield return null;
                }
                FxBurst.Spawn(transform.position + d + Vector3.up * 0.2f, 1.6f, FxBurst.Kind.Dust, new Color(0.85f, 0.9f, 1f));
                yield return Hold(0.5f, _ => { });
            }
            nextAttack = Time.time + KnightBashCooldown * Random.Range(0.85f, 1.2f) / AtkMul;
            busy = false;
        }

        // ================= 신기루 분신술사 =================
        void TricksterUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (IsClone && (!cloneOwner || !cloneOwner.health.Alive)) { health.TakeDamage(health.maxHp * 10f); return; }
            KeepRange(p, dir, dist, spd, TrickKeep - 1f, TrickKeep + 1f);
            if (!IsClone && Time.time >= nextSpecial) { StartCoroutine(TrickClones(p)); return; }
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(TrickShot(p));
        }

        IEnumerator TrickShot(PlayerController p)
        {
            busy = true;
            Anim("Attack");
            var d = ToP(p);
            tele = Telegraph.Lane(transform.position + d * 0.6f, d, 9f, 0.14f, 0.4f / AtkMul, Telegraph.Aim);
            bool ok = false;
            yield return Hold(0.4f, r => ok = r);
            if (ok) EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + d * 0.7f, ToP(p), TrickShotSpeed, projectileDamage, 18f, EnemyProjectile.Style.Orb);
            nextAttack = Time.time + TrickShotInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        IEnumerator TrickClones(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + TrickCloneInterval;
            clones.RemoveAll(c => !c || !c.health.Alive);
            int max = elite ? 3 : 2;
            var rm = RunManager.Instance;
            while (clones.Count < max)
            {
                var at = transform.position + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * Random.Range(2.5f, 4f);
                if (rm) at = rm.ClampToRoom(at, 1.5f);
                var c = EnemyFactory.TricksterClone(at, chapter, this);
                clones.Add(c);
                Fx3("Stardust", at + Vector3.up, 1f);
            }
            // 셋이 0.6초 동안 자리를 섞음
            var group = new List<Enemy> { this };
            foreach (var c in clones) if (c) group.Add(c);
            var starts = new List<Vector3>();
            foreach (var g in group) starts.Add(g.transform.position);
            var ends = new List<Vector3>(starts);
            for (int i = ends.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (ends[i], ends[j]) = (ends[j], ends[i]); }
            if (ends.Count > 1 && ends[0] == starts[0]) (ends[0], ends[1]) = (ends[1], ends[0]);
            foreach (var g in group) if (g != this) g.busy = true;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / TrickShuffle;
                float k = Mathf.SmoothStep(0, 1, t);
                for (int i = 0; i < group.Count; i++)
                    if (group[i]) group[i].transform.position = Vector3.Lerp(starts[i], ends[i], k) + Vector3.up * 0f;
                yield return null;
            }
            foreach (var g in group) if (g && g != this) { g.busy = false; g.nextAttack = Time.time + Random.Range(0.5f, TrickShotInterval); }
            nextAttack = Time.time + Random.Range(0.6f, 1.4f);
            busy = false;
        }
    }
}
