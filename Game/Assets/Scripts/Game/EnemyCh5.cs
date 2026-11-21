// 5장 '무너지는 시간의 끝' 일반 적 6종 (레벨 디자인 md '챕터 5' · '수치 기준 (챕터 5)', 2026-11-19)
// 체력 · 피해는 md 값 그대로 (챕터 배율 안 곱함). 강화형 체력 1.5배는 EnemyFactory.MakeElite.
// 색: 적 공격 궤적 = 자홍 · 마젠타 (Telegraph.Danger가 5장이면 자홍), 고양이 쪽 효과 = 흰색 · 금색
//
// - 파편 사수(ShardGunner): 8~11m 유지, 2.5초마다 유리탄 1발(예고 0.6 · 10m/초 · 19). 벽 · 거울 조각에 닿으면 3갈래(25°) 분열
//   강화형 관통 분열: 날아가다 0.45초 뒤 한 번 더 3갈래로 갈라짐 (엄폐물 뒤 · 옆으로 숨어도 위험)
// - 별자리 마녀(StarWitch): 9m 유지, 6초마다 고양이 둘레에 별 3개(1초에 걸쳐) → 별 사이 빛 선이 8m/초로 이어짐(선에 닿으면 19)
//   강화형 별자리 완성: 세 선이 다 이어지면 삼각형 가운데 반경 3m 폭발 22 (예고 0.5)
// - 붕괴 거인(RuinGiant): 이속 3.2, 내려찍기(예고 0.8 · 전방 90° 3m · 22) → 바닥 균열 장판(반경 2 · 4초 · 이속 30%↓).
//   4~8m면 돌진(예고 0.6 · 7m · 20). 강화형 균열 연쇄: 내려찍은 균열이 4m 안 다른 균열로 이어져 연쇄 폭발(반경 2 · 19)
// - 균열 자객(RiftAssassin): 균열(방 기믹 균열 · 깨진 흉터 · 거인 균열)에 들어가 1.5초 잠복(무적) → 고양이와 가장 가까운 균열이 0.6초 빛나며 예고
//   → 튀어나와 전방 100° 2.5m 베기 22 → 1초 빈틈. 균열이 없으면 걸어서 다가와 같은 베기. 강화형 이중 매복: 다른 균열에서 0.5초 뒤 한 번 더
// - 균열술사(RiftCaster): 10m 유지(도망), 5초마다 고양이 둘레에 균열 하나 열기(방 기믹과 같음), 3초마다 허공 탄 곡사(원 예고 0.8 · 반경 1.2 · 19).
//   죽을 때 주변에 균열 3개. 강화형 균열 즉발: 그 3개가 지연 없이 바로 터짐
// - 거울 허상(MirrorPhantom): 3장 분신술사 규칙(EnemyCh3 Trickster)을 그대로 씀 — 분신 2개 · 자리 섞기 · 3초마다 허공 탄.
//   그림자 있는 쪽만 진짜(분신은 그림자를 끔). 강화형 분신 3개 + 분신이 터진 자리에 균열 장판(반경 1.5 · 3초 · 30%↓)
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        // ---------- 수치 (md 챕터 5) ----------
        public const float GunnerNear = 8f, GunnerFar = 11f, GunnerInterval = 2.5f, GunnerAim = 0.6f, GunnerBullet = 10f, GunnerSplit = 25f, GunnerMidSplit = 0.45f;
        public const float WitchKeep = 9f, WitchInterval = 6f, WitchPlace = 1f, WitchStarR = 3.2f, WitchLineSpeed = 8f, WitchLineWidth = 0.35f, WitchBurstR = 3f, WitchBurstWarn = 0.5f;
        public const float GiantSlamWindup = 0.8f, GiantSlamRange = 3f, GiantSlamHalf = 45f, GiantCrackR = 2f, GiantCrackLife = 4f, GiantCrackSlow = 0.3f;
        public const float GiantDashWindup = 0.6f, GiantDashDist = 7f, GiantDashTime = 0.45f, GiantDashCd = 5f, GiantChainReach = 4f, GiantChainR = 2f;
        public const float RiftHide = 1.5f, RiftWarn = 0.6f, RiftWarn2 = 0.5f, RiftHalf = 50f, RiftRange = 2.5f, RiftOpen = 1f, RiftReach = 6f, RiftDiveCd = 5f, RiftWindup = 0.45f;
        public const float CasterKeep = 10f, CasterRiftEvery = 5f, CasterLobEvery = 3f, CasterLobFlight = 0.5f, CasterLobRelease = 0.3f, CasterLobR = 1.2f;
        public const float ShardDmg = 19f, StarDmg = 19f, StarBurstDmg = 22f, SlamDmg = 22f, GiantDashDmg = 20f, ChainDmg = 19f, RiftSlashDmg = 22f, VoidDmg = 19f;

        public static readonly Color Ch5Magenta = new(1f, 0.18f, 0.7f), Ch5Gold = new(1f, 0.78f, 0.3f), Ch5Violet = new(0.42f, 0.2f, 0.78f);
        static readonly Color Ch5Aim = new(1f, 0.5f, 0.86f);                 // 조준선 (자홍보다 옅게)

        // 5장 이펙트 = Codex 그림 시트가 기틀(cx_c5*, docs/images/fx_src/c5_*.png) + 블렌더로 구운 3D 플립북(bk_c5*, blender_bake_fx.py)을 겹침 (2026-11-19 사용자)
        static readonly Dictionary<string, string> Cx5 = new()
        {
            { "c5spark", "cx_c5shardhit" }, { "c5rift", "cx_c5riftshatter" }, { "c5starburst", "cx_c5starburst" },
            { "c5voidburst", "cx_c5voidburst" }, { "c5slash", "cx_c5voidslash" },
        };

        /// <summary>5장 이펙트: Codex 시트(바탕) + 블렌더 플립북(덧그림, 조금 작게)을 한 자리에 겹침. 둘 다 없으면 자홍 불똥</summary>
        public static void Fx5(string set, Vector3 at, float size = 1f, float life = 0.35f, Vector3 dir = default)
        {
            bool any = false;
            if (Cx5.TryGetValue(set, out var cx)) any |= FxFlipbook.Spawn(cx, at, dir, size, life, false);
            any |= FxFlipbook.Spawn("bk_" + set, at + Vector3.up * 0.02f, dir, size * (any ? 0.8f : 1f), life, false);
            if (!any) FxBurst.Spawn(at, 1.2f * size, FxBurst.Kind.Spark, Ch5Magenta);
        }

        float nextDash;

        /// <summary>죽을 때 5장 처리 (Die에서 부름)</summary>
        void OnDieCh5()
        {
            if (kind != Kind.RiftCaster) return;
            // 죽을 때 주변에 균열 3개 (강화형은 바로 터짐)
            for (int i = 0; i < 3; i++)
            {
                var at = transform.position + Quaternion.Euler(0, i * 120f + Random.Range(-20f, 20f), 0) * Vector3.forward * Random.Range(1.5f, 2.8f);
                var rm = RunManager.Instance; if (rm) at = rm.ClampToRoom(at, 1f);
                Rift.Spawn(at, elite ? 0.05f : Rift.Warn);
            }
        }

        // ================= 파편 사수 =================
        void GunnerUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, GunnerNear, GunnerFar);
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(GunnerShot(p));
        }

        IEnumerator GunnerShot(PlayerController p)
        {
            busy = true;
            Anim("Shot");
            BraceShot(GunnerAim / AtkMul, EnemyShotPose.Style.Rifle);
            var d = ToP(p);
            float end = Time.time + GunnerAim / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; break; }
                d = Vector3.Slerp(d, ToP(p), 4f * Time.deltaTime).normalized;
                if (tele) tele.Kill();
                tele = Telegraph.Lane(transform.position + d * 0.6f, d, 11f, 0.16f, 0.05f, Ch5Aim);
                Face(d);
                yield return null;
            }
            if (tele) tele.Kill();
            if (ok)
            {
                var muzzle = transform.position + Vector3.up * 1.0f + d * 0.9f;
                var plusRifle = WeaponT("ShardRiflePlus");
                var rifle = plusRifle ? plusRifle : WeaponT("ShardRifle");
                var visibleMuzzle = rifle ? rifle.TransformPoint(new Vector3(0f, plusRifle ? 1.05f : 0.90f, 0f)) : muzzle;
                ReleaseShot(d, EnemyShotPose.Style.Rifle, 1.05f);
                var pr = ShardBullet(muzzle, d, 24f);
                pr.LaunchVisualFrom(visibleMuzzle);
                StylizedFx.Spawn("spike_burst", visibleMuzzle, Quaternion.LookRotation(d), Vector3.one * 0.25f, Vector3.one * 0.8f, 0.16f, StylizedFx.Magenta.With(taperV: 0f));   // 총구 유리 가시
                Sfx.Play("e5_shard_shot", 0.5f, 0.06f, 3);
                Fx5("c5spark", visibleMuzzle, 0.6f, 0.2f);
                if (elite)
                    LaserFx.Later(GunnerMidSplit / AtkMul, () =>
                    {
                        if (!pr || !pr.enabled) return;
                        SplitShard(pr.transform.position, pr.Dir, 10f);
                        Fx5("c5spark", pr.transform.position, 0.7f, 0.22f);
                    });
            }
            nextAttack = Time.time + GunnerInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            yield return new WaitForSeconds(0.2f);
            busy = false;
        }

        /// <summary>유리탄: 벽 · 거울 조각에 한 번 닿으면 3갈래(가운데 = 튕긴 원래 탄)</summary>
        EnemyProjectile ShardBullet(Vector3 at, Vector3 d, float range)
        {
            var pr = EnemyProjectile.Spawn(at, d, GunnerBullet, projectileDamage, range, EnemyProjectile.Style.Needle);
            pr.bounces = 1;
            pr.OnBounced = (b, hitAt) =>
            {
                SplitShard(hitAt + Vector3.up * 1.0f, b.Dir, 12f, false);
                Sfx.Play("e5_shard_split", 0.45f, 0.05f, 3);
                Fx5("c5spark", hitAt + Vector3.up * 1.0f, 0.8f, 0.25f);
            };
            return pr;
        }

        void SplitShard(Vector3 at, Vector3 d, float range, bool withCenter = true)
        {
            foreach (float a in withCenter ? new[] { -GunnerSplit, 0f, GunnerSplit } : new[] { -GunnerSplit, GunnerSplit })
            {
                var s = EnemyProjectile.Spawn(at + d * 0.15f, Quaternion.Euler(0, a, 0) * d, GunnerBullet, projectileDamage, range, EnemyProjectile.Style.Needle);
                s.mirrored = true;                                                   // 갈라진 조각은 다시 갈라지지 않음
            }
        }

        // ================= 별자리 마녀 =================
        void WitchUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, WitchKeep - 1f, WitchKeep + 1f);
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(WitchConstellation(p));
        }

        /// <summary>지팡이를 들어 고양이 둘레에 별 3개를 하나씩 박음(1초, 별 사이에 옅은 예고 선) → 별에서 다음 별로 빛 선이 8m/초로 뻗음</summary>
        IEnumerator WitchConstellation(PlayerController p)
        {
            busy = true;
            Anim("Cast");
            var c = p.transform.position; c.y = 0;
            float a0 = Random.Range(0f, 360f);
            var stars = new List<Vector3>();
            var rm = RunManager.Instance;
            for (int i = 0; i < 3; i++)
            {
                var s = c + Quaternion.Euler(0, a0 + i * 120f + Random.Range(-15f, 15f), 0) * Vector3.forward * WitchStarR * Random.Range(0.85f, 1.15f);
                if (rm) s = rm.ClampToRoom(s, 1f);
                stars.Add(s);
            }
            var marks = new List<Telegraph>();
            for (int i = 0; i < 3; i++)
            {
                if (!health.Alive || health.Stunned) break;
                Face(ToP(p));
                marks.Add(Telegraph.Ring(stars[i], 0.45f, 4f, Ch5Gold).NoGlint());
                Fx5("c5starburst", stars[i] + Vector3.up * 1.0f, 0.55f, 0.3f);
                StylizedFx.Spawn("spiral_ball", stars[i] + Vector3.up * 1.0f, Quaternion.identity, Vector3.one * 0.2f, Vector3.one * 0.55f, 0.4f, StylizedFx.Glass.With(pattern: 2, scale: new Vector2(3, 1), scroll: new Vector2(-5f, 0), fresnel: 1f), 500f);   // 박힌 별이 빙글 반짝
                Sfx.Play("e5_star_place", 0.4f, 0.08f, 3);
                if (i > 0) Telegraph.Lane(stars[i - 1], Effects.Flat(stars[i] - stars[i - 1]).normalized, Vector3.Distance(stars[i - 1], stars[i]), WitchLineWidth, WitchPlace, Ch5Aim).NoGlint();
                yield return Hold(WitchPlace / 3f, _ => { });
            }
            bool ok = health.Alive && !health.Stunned && marks.Count == 3;
            if (ok)
            {
                Telegraph.Lane(stars[2], Effects.Flat(stars[0] - stars[2]).normalized, Vector3.Distance(stars[2], stars[0]), WitchLineWidth, 0.25f, Ch5Aim).NoGlint();
                Anim("Link");
                Sfx.Play("e5_star_line", 0.5f, 0.05f, 2);
                float longest = 0;
                for (int i = 0; i < 3; i++)
                {
                    var a = stars[i]; var b = stars[(i + 1) % 3];
                    StarLink.Make(a, b, WitchLineSpeed, WitchLineWidth, projectileDamage);
                    longest = Mathf.Max(longest, Vector3.Distance(a, b) / WitchLineSpeed);
                }
                if (elite)
                {
                    // 별자리 완성: 세 선이 다 이어지면 삼각형 가운데 폭발
                    var mid = (stars[0] + stars[1] + stars[2]) / 3f;
                    LaserFx.Later(longest, () => StarBurst(mid));
                }
            }
            foreach (var m in marks) if (m) m.Kill();
            nextAttack = Time.time + WitchInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            yield return new WaitForSeconds(0.3f);
            busy = false;
        }

        static void StarBurst(Vector3 mid)
        {
            Telegraph.Ring(mid, WitchBurstR, WitchBurstWarn, Telegraph.Danger).Grow();
            LaserFx.Later(WitchBurstWarn, () =>
            {
                var p = PlayerController.Instance;
                if (p && p.health.Alive && Effects.Flat(p.transform.position - mid).magnitude <= WitchBurstR) p.health.TakeDamage(StarBurstDmg);
                PropHit.Area(mid, WitchBurstR, StarBurstDmg);
                Fx5("c5starburst", mid + Vector3.up * 0.8f, 2.2f, 0.45f);
                StylizedFx.Shockwave(mid, WitchBurstR, StylizedFx.Magenta, 0.4f);
                Sfx.Play("e5_star_burst", 0.6f);
                CameraFollow.Shake(0.06f);
            });
        }

        // ================= 붕괴 거인 =================
        void GiantUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            Face(dir);
            if (dist > GiantSlamRange * 1.3f && dist < GiantDashDist + 1f && Time.time >= nextDash) { StartCoroutine(GiantDash(p)); return; }
            if (dist <= GiantSlamRange + 0.3f && Time.time >= nextAttack) { StartCoroutine(GiantSlam(p)); return; }
            if (dist > 1.8f) transform.position += dir * spd * Time.deltaTime;
        }

        /// <summary>망치를 머리 위로 들어 올림(예고 0.8) → 내리찍음 = 전방 90° 피해 + 바닥에 균열 장판(느려짐) — 자객이 숨을 수 있는 균열</summary>
        IEnumerator GiantSlam(PlayerController p)
        {
            busy = true;
            Anim("Slam");
            var d = ToP(p);
            transform.rotation = Quaternion.LookRotation(d);
            bool braced = BraceSwing(GiantSlamWindup, 0.52f, twist: 8f, overhead: true,
                light: () => Streak5("RuinMaul", 0.27f), amplitude: 1.26f);
            tele = Telegraph.Sector(transform.position, d, GiantSlamRange, GiantSlamHalf, GiantSlamWindup / AtkMul, Telegraph.Danger, transform);
            bool ok = false;
            yield return Hold(GiantSlamWindup, r => ok = r);
            if (ok)
            {
                if (!braced) Streak5("RuinMaul", 0.27f);
                var at = transform.position + d * GiantSlamRange * 0.6f;
                if (p && p.health.Alive)
                {
                    var to = Effects.Flat(p.transform.position - transform.position);
                    if (to.magnitude <= GiantSlamRange && Vector3.Angle(d, to) <= GiantSlamHalf) { p.health.TakeDamage(contactDamage, hitStop: 0.057f); p.health.Knockback(d * 4f); }
                }
                PropHit.Area(at, GiantSlamRange * 0.6f, contactDamage);
                RiftScarZone.Spawn(at, GiantCrackR, GiantCrackLife, GiantCrackSlow);   // 5장 전용 균열 장판 (깨진 금이 갈라지며 퍼짐)
                Fx5("c5voidburst", at + Vector3.up * 0.3f, 1.8f, 0.4f);
                MeleeReachFx.Impact(transform.position, d, GiantSlamRange, GiantSlamHalf, Ch5Magenta);
                Sfx.Play("e5_giant_slam", 0.7f, 0.05f, 2);
                CameraFollow.Shake(0.12f);
                if (elite) StartCoroutine(GiantChain(at));
                yield return new WaitForSeconds(0.6f);
            }
            nextAttack = Time.time + 2.2f / AtkMul;
            busy = false;
        }

        /// <summary>강화형 균열 연쇄: 새 균열에서 4m 안의 다른 균열(방 균열 · 흉터 · 앞선 거인 균열)로 차례로 이어져 반경 2m 폭발</summary>
        IEnumerator GiantChain(Vector3 from)
        {
            var visited = new List<Vector3> { from };
            var cur = from;
            for (int hop = 0; hop < 4; hop++)
            {
                if (!RiftSpots.Nearest(cur, GiantChainReach, visited, out var next)) yield break;
                Telegraph.Lane(cur, Effects.Flat(next - cur).normalized, Vector3.Distance(cur, next), 0.4f, 0.25f, Telegraph.Danger).NoGlint();
                Telegraph.Ring(next, GiantChainR, 0.3f, Telegraph.Danger);
                yield return new WaitForSeconds(0.3f);
                var p = PlayerController.Instance;
                if (p && p.health.Alive && Effects.Flat(p.transform.position - next).magnitude <= GiantChainR) p.health.TakeDamage(ChainDmg);
                PropHit.Area(next, GiantChainR, ChainDmg);
                Fx5("c5rift", next + Vector3.up * 1f, 1.6f, 0.35f);
                Sfx.Play("e5_rift_shatter", 0.5f, 0.06f, 3);
                visited.Add(next);
                cur = next;
            }
        }

        /// <summary>어깨를 낮춰 웅크림(예고 0.6) → 7m 일직선 돌진, 예고 선과 같은 판정</summary>
        IEnumerator GiantDash(PlayerController p)
        {
            busy = true;
            nextDash = Time.time + GiantDashCd;
            Anim("Dash");
            var d = ToP(p);
            var from = transform.position;
            tele = Telegraph.Lane(from, d, GiantDashDist + 0.8f, 1.8f, GiantDashWindup / AtkMul, Telegraph.Danger);
            bool ok = false;
            yield return Hold(GiantDashWindup, r => ok = r);
            if (ok)
            {
                Dashing = true;
                Sfx.Play("e5_giant_dash", 0.6f);
                var to = from + d * GiantDashDist;
                var rm = RunManager.Instance; if (rm) to = rm.ClampToRoom(to, 1.4f);
                bool hit = false;
                for (float t = 0; t < 1f; t += Time.deltaTime / GiantDashTime)
                {
                    transform.position = Vector3.Lerp(from, to, 1 - (1 - t) * (1 - t));
                    float mv = Effects.Flat(transform.position - from).magnitude;
                    if (!hit && p && p.health.Alive && Telegraph.InLane(p.transform.position, from, d, GiantDashDist + 0.8f, 1.8f, mv - 0.95f, mv + 0.95f))
                    { hit = true; p.health.TakeDamage(GiantDashDmg); p.health.Knockback(d * 6f); }
                    yield return null;
                }
                Dashing = false;
                PropHit.Area(transform.position + d, 1.4f, GiantDashDmg);
                Fx5("c5voidburst", transform.position + d * 0.8f + Vector3.up * 0.2f, 1.6f, 0.35f);
                yield return new WaitForSeconds(0.5f);
            }
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.8f);
            busy = false;
        }

        // ================= 균열 자객 =================
        void RiftAssassinUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (Time.time >= nextSpecial && dist > 3f && RiftSpots.Count > 0) { StartCoroutine(RiftDive(p)); return; }
            if (dist <= RiftRange + 0.3f && Time.time >= nextAttack) { StartCoroutine(RiftSlash(p, null, false)); return; }
            transform.position += dir * spd * Time.deltaTime;
            Face(dir);
        }

        IEnumerator RiftDive(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + RiftDiveCd * Random.Range(0.85f, 1.2f);
            Anim("Dive");
            Sfx.Play("e5_rift_dive", 0.55f);
            yield return Hold(0.35f, _ => { });
            var from = transform.position;
            Fx5("c5rift", from + Vector3.up, 1.1f, 0.3f);
            SetHidden(true);
            yield return Hold(RiftHide, _ => { });
            if (p && RiftSpots.Nearest(p.transform.position, RiftReach, null, out var spot))
            {
                yield return RiftEmerge(p, spot, RiftWarn);
                if (elite && health.Alive && p)
                {
                    var used = new List<Vector3> { spot };
                    if (RiftSpots.Nearest(p.transform.position, RiftReach + 2f, used, out var spot2))
                    {
                        Fx5("c5rift", transform.position + Vector3.up, 0.9f, 0.3f);
                        SetHidden(true);
                        yield return RiftEmerge(p, spot2, RiftWarn2);
                    }
                }
            }
            else
            {
                // 근처에 균열이 없으면 들어간 자리에서 다시 나와 걸어감
                SetHidden(false);
                Fx5("c5rift", from + Vector3.up, 0.9f, 0.3f);
            }
            busy = false;
        }

        /// <summary>균열이 warn초 빛난 뒤 거기서 튀어나와 벰</summary>
        IEnumerator RiftEmerge(PlayerController p, Vector3 spot, float warn)
        {
            var toP = p ? Effects.Flat(p.transform.position - spot) : Vector3.forward;
            var d = toP.sqrMagnitude > 1e-4f ? toP.normalized : Vector3.forward;
            var exit = spot + d * 0.5f;
            var rm = RunManager.Instance; if (rm) exit = rm.ClampToRoom(exit, 1f);
            Rift.Glow(spot, warn);
            var t = Telegraph.Sector(exit, d, RiftRange, RiftHalf, warn / AtkMul, Telegraph.Danger);
            yield return Hold(warn, _ => { });
            if (t) t.Kill();
            transform.position = new Vector3(exit.x, transform.position.y, exit.z);
            transform.rotation = Quaternion.LookRotation(d);
            SetHidden(false);
            Sfx.Play("e5_rift_emerge", 0.6f);
            Fx5("c5rift", exit + Vector3.up, 1.2f, 0.3f);
            yield return RiftSlash(p, d, true);
        }

        /// <summary>전방 100° · 2.5m 두 칼 베기 → 1초 빈틈. 균열에서 튀어나올 땐 예고가 이미 끝나 바로 벰</summary>
        IEnumerator RiftSlash(PlayerController p, Vector3? fixedDir, bool instant)
        {
            busy = true;
            Anim(instant ? "Emerge" : "Slash");
            var d = fixedDir ?? ToP(p);
            bool ownsBlades = WeaponT("RiftBlade") || WeaponT("RiftBladePlus");
            bool braced = BraceSwing(instant ? 0f : RiftWindup, 0.36f, twist: instant ? 12f : -21f,
                light: ownsBlades ? RiftBladeSwingGlow : null, amplitude: 1.23f);
            bool ok = true;
            if (!instant)
            {
                transform.rotation = Quaternion.LookRotation(d);
                tele = Telegraph.Sector(transform.position, d, RiftRange, RiftHalf, RiftWindup / AtkMul, Telegraph.Danger, transform).NoLaser();
                yield return Hold(RiftWindup, r => ok = r);
            }
            if (ok && health.Alive)
            {
                Sfx.Play("e5_slash", 0.55f, 0.06f, 3);
                if (ownsBlades && !braced) RiftBladeSwingGlow();
                MeleeReachFx.Sweep(transform.position, d, RiftRange, RiftHalf, Ch5Magenta, reverse: instant);
                if (p && p.health.Alive)
                {
                    var to = Effects.Flat(p.transform.position - transform.position);
                    if (to.magnitude <= RiftRange && Vector3.Angle(d, to) <= RiftHalf) p.health.TakeDamage(contactDamage, hitStop: 0.032f);
                }
                PropHit.Area(transform.position + d * 1.4f, RiftRange * 0.8f, contactDamage);
                const float follow = 0.19f;
                yield return Hold(follow, r => ok = r);
                if (ok)
                {
                    Anim("Breath");
                    yield return Hold(RiftOpen - follow, _ => { });
                }
            }
            nextAttack = Time.time + 1.2f / AtkMul;
            if (!fixedDir.HasValue) busy = false;
        }

        void RiftBladeSwingGlow()
        {
            if (!model) return;
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("Weapon_RiftBlade") || !BladePts5.TryGetValue(t.name.Substring(7), out var edge))
                    continue;
                BladeStreak.Begin(t, edge.tip, edge.grip, 0.24f, Ch5Magenta, 1.2f);
            }
        }

        // 칼빛 (BladeStreak, 5장 = 자홍). 무기 원점 = 손잡이, 긴 축 +Y
        static readonly Dictionary<string, (Vector3 tip, Vector3 grip)> BladePts5 = new()
        {
            { "RiftBlade", (new Vector3(0, 0.68f, 0), new Vector3(0, 0.08f, 0)) },
            { "RiftBladePlus", (new Vector3(0, 0.95f, 0), new Vector3(0, 0.1f, 0)) },
            { "RuinMaul", (new Vector3(0, 1.1f, 0), new Vector3(0, 0.4f, 0)) },
            { "RuinMaulPlus", (new Vector3(0, 1.6f, 0), new Vector3(0, 0.6f, 0)) },
        };
        void Streak5(string weapon, float swing)
        {
            var w = WeaponT(weapon + "Plus");
            if (w) weapon += "Plus"; else w = WeaponT(weapon);
            if (w && BladePts5.TryGetValue(weapon, out var bp)) BladeStreak.Begin(w, bp.tip, bp.grip, swing, Ch5Magenta);
        }

        // ================= 균열술사 =================
        void CasterUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, CasterKeep - 1f, CasterKeep + 2f);
            if (Time.time >= nextSpecial && dist < 18f) { StartCoroutine(CasterOpenRift(p)); return; }
            if (Time.time >= nextAttack && dist < 14f) StartCoroutine(CasterLob(p));
        }

        /// <summary>향로를 흔들어 고양이 둘레 허공에 균열을 엶 (방 기믹과 같은 균열: 1.5초 뒤 깨짐)</summary>
        IEnumerator CasterOpenRift(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + CasterRiftEvery * Random.Range(0.9f, 1.1f) / AtkMul;
            Anim("Open");
            Face(ToP(p));
            bool ok = false;
            yield return Hold(0.35f, r => ok = r);
            if (ok && p)
            {
                var at = p.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(0.6f, 2f);
                var rm = RunManager.Instance; if (rm) at = rm.ClampToRoom(at, 1f);
                Rift.Spawn(at, Rift.Warn);
            }
            yield return new WaitForSeconds(0.25f);
            busy = false;
        }

        /// <summary>허공 탄 곡사: 향로를 뒤로 젖혔다(0.3초) 던짐 → 0.5초 날아가 원 자리에 떨어짐 (원 예고 0.8초)</summary>
        IEnumerator CasterLob(PlayerController p)
        {
            busy = true;
            Anim("Lob");
            BraceShot(CasterLobRelease / AtkMul, EnemyShotPose.Style.Throw);
            var at = p.transform.position; at.y = transform.position.y;
            Telegraph.Ring(at, CasterLobR, (CasterLobRelease + CasterLobFlight) / AtkMul, Telegraph.Danger);
            for (float w = 0; w < CasterLobRelease / AtkMul; w += Time.deltaTime) { Face(Effects.Flat(at - transform.position)); yield return null; }
            if (health.Alive && !health.Stunned)
            {
                ReleaseShot(at - transform.position, EnemyShotPose.Style.Throw);
                VoidLob.Launch(transform.position + Vector3.up * 1.6f, at, CasterLobFlight / AtkMul, CasterLobR, projectileDamage);
                Sfx.Play("e5_void_lob", 0.5f);
            }
            yield return new WaitForSeconds(0.25f);
            nextAttack = Time.time + CasterLobEvery * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }
    }

    public partial class Enemy
    {
        // ================= 거울 허상 (5장 전용, 2026-11-19 사용자: 이전 모션 · 이펙트 재활용 말고 다른 느낌으로) =================
        // 9m 유지, 8초마다 분신 2개(강화 3) — 분신은 고양이를 축으로 본체를 비춘 자리(점대칭 · 좌우 대칭)에 거울 조각이 모여 생김.
        // 자리 바꾸기: 미끄러지지 않고 셋이 동시에 유리처럼 깨졌다가(0.25초) 서로 바뀐 자리에서 다시 맞춰짐 (0.35초). 본체 · 분신 3초마다 허공 탄
        public const float PhKeep = 9f, PhCloneEvery = 8f, PhShatter = 0.25f, PhReform = 0.35f, PhShotEvery = 3f, PhShotSpeed = 8f;
        float phBob, phChargeAt, phChargeUntil, phFiredAt = -1f;

        void PhantomUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (IsClone && (!cloneOwner || !cloneOwner.health.Alive)) { health.TakeDamage(health.maxHp * 10f); return; }
            KeepRange(p, dir, dist, spd, PhKeep - 1f, PhKeep + 1f);
            if (!IsClone && Time.time >= nextSpecial) { StartCoroutine(PhantomSplit(p)); return; }
            if (Time.time >= nextAttack && dist < 16f) StartCoroutine(PhantomShot(p));
        }

        IEnumerator PhantomShot(PlayerController p)
        {
            busy = true;
            var d = ToP(p);
            phChargeAt = Time.time;
            phChargeUntil = Time.time + 0.4f / AtkMul;
            tele = Telegraph.Lane(transform.position + d * 0.6f, d, 9f, 0.14f, 0.4f / AtkMul, Ch5Aim);
            bool ok = false;
            yield return Hold(0.4f, r => ok = r);
            phChargeUntil = 0f;
            if (ok)
            {
                phFiredAt = Time.time;
                EnemyProjectile.Spawn(transform.position + Vector3.up * 1.0f + d * 0.7f, ToP(p), PhShotSpeed, projectileDamage, 18f, EnemyProjectile.Style.Orb);
                Fx5("c5spark", transform.position + Vector3.up * 1.0f + d * 0.7f, 0.5f, 0.18f);
            }
            nextAttack = Time.time + PhShotEvery * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        /// <summary>고양이를 축으로 비친 자리: 점대칭 · 좌우 대칭 · 앞뒤 대칭 순 (방 안으로 붙잡음)</summary>
        Vector3 Reflected(PlayerController p, int i)
        {
            var c = p.transform.position; var r = transform.position - c; r.y = 0;
            var m = i == 0 ? -r : i == 1 ? new Vector3(-r.x, 0, r.z) : new Vector3(r.x, 0, -r.z);
            var at = c + m * Random.Range(0.85f, 1.1f);
            var rm = RunManager.Instance; if (rm) at = rm.ClampToRoom(at, 1.5f);
            return new Vector3(at.x, transform.position.y, at.z);
        }

        IEnumerator PhantomSplit(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + PhCloneEvery;
            Sfx.Play("e5_phantom_split", 0.6f);
            clones.RemoveAll(c => !c || !c.health.Alive);
            int max = elite ? 3 : 2, k = 0;
            while (clones.Count < max)
            {
                var at = Reflected(p, k++ % 3);
                Fx5("c5starburst", at + Vector3.up * 1.0f, 1.1f, 0.35f);
                StylizedFx.Spawn("spiral_ball", at + Vector3.up * 1.1f, Quaternion.identity, Vector3.one * 0.3f, Vector3.one * 1.1f, 0.45f, StylizedFx.Glass.With(pattern: 2, scale: new Vector2(3, 1), scroll: new Vector2(-6f, 0), fresnel: 1.2f), 600f);   // 거울 조각이 소용돌이치며 모여 분신이 됨
                var c = EnemyFactory.TricksterClone(at, chapter, this);
                clones.Add(c);
            }
            // 깨졌다 다시 맞춰지며 자리 바꾸기
            var group = new List<Enemy> { this };
            foreach (var c in clones) if (c) group.Add(c);
            var spots = new List<Vector3>();
            foreach (var g in group) spots.Add(g.transform.position);
            for (int i = spots.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            if (spots.Count > 1 && Effects.Flat(spots[0] - transform.position).sqrMagnitude < 0.01f) (spots[0], spots[1]) = (spots[1], spots[0]);
            foreach (var g in group) if (g) { g.busy = true; Fx5("c5spark", g.transform.position + Vector3.up * 1.0f, 1.3f, 0.3f); g.ShowBody(false); }
            yield return new WaitForSeconds(PhShatter);
            for (int i = 0; i < group.Count; i++)
                if (group[i]) { group[i].transform.position = spots[i]; Fx5("c5rift", spots[i] + Vector3.up * 1.0f, 1.0f, PhReform); }
            yield return new WaitForSeconds(PhReform);
            foreach (var g in group) if (g) { g.ShowBody(true); if (g != this) { g.busy = false; g.nextAttack = Time.time + Random.Range(0.5f, PhShotEvery); } }
            nextAttack = Time.time + Random.Range(0.6f, 1.4f);
            busy = false;
        }

        /// <summary>몸(모델)만 숨김/보임 (판정은 그대로 — 깨진 사이에도 맞을 수 있음)</summary>
        void ShowBody(bool on) { if (model) model.gameObject.SetActive(on); }

        /// <summary>뼈 없는 허상을 띄움: 바닥 위 0.25~0.4m에서 둥실, 움직이는 쪽으로 기울고, 가끔 한 프레임 어긋나 튐(글리치)</summary>
        void PhantomFloat(float moveSpeed)
        {
            phBob += Time.deltaTime * 2.2f;
            float k = Mathf.Clamp01(moveSpeed / Mathf.Max(0.5f, speed));
            float glitch = Random.value < 0.015f ? Random.Range(-0.12f, 0.12f) : 0f;
            float charge = Time.time < phChargeUntil ? WeaponSwingRig.Ease(Mathf.InverseLerp(phChargeAt, phChargeUntil, Time.time)) : 0f;
            float age = Time.time - phFiredAt;
            float thrust = (1f - WeaponSwingRig.Ease(age / 0.21f)) * (0.55f + 0.45f * Mathf.Clamp01(age / 0.035f));
            model.localPosition = new Vector3(glitch, 0.3f + Mathf.Sin(phBob) * 0.08f - charge * 0.09f, thrust * 0.13f);
            model.localRotation = Quaternion.Euler(14f * k - charge * 9f + thrust * 13f, 0, Mathf.Sin(phBob * 0.7f) * 6f + glitch * 60f) * modelBaseRot;
            float stretch = thrust * 0.11f - charge * 0.08f;
            model.localScale = Vector3.Scale(modelScale, new Vector3(1f - stretch * 0.42f, 1f - stretch * 0.42f, 1f + stretch));
        }
    }

    /// <summary>별자리 선: a에서 b로 speed m/초로 뻗는 빛 선. 뻗은 구간에 고양이가 닿으면 한 번 피해, 다 뻗으면 0.35초 머물다 사라짐</summary>
    public class StarLink : MonoBehaviour
    {
        Vector3 a, b; float speed, width, dmg, t0, len; bool hit; LineRenderer lr;

        public static StarLink Make(Vector3 a, Vector3 b, float speed, float width, float dmg)
        {
            var go = new GameObject("StarLink");
            var s = go.AddComponent<StarLink>();
            s.a = new Vector3(a.x, 1.0f, a.z); s.b = new Vector3(b.x, 1.0f, b.z);
            s.speed = speed; s.width = width; s.dmg = dmg; s.t0 = Time.time; s.len = Vector3.Distance(s.a, s.b);
            s.lr = go.AddComponent<LineRenderer>();
            s.lr.sharedMaterial = LaserFx.StripMat5();
            s.lr.textureMode = LineTextureMode.Stretch;
            s.lr.colorGradient = LaserFx.TailGradient(Enemy.Ch5Magenta);
            s.lr.widthMultiplier = width * 1.4f;
            s.lr.positionCount = 2;
            s.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.lr.receiveShadows = false;
            s.lr.SetPosition(0, s.a); s.lr.SetPosition(1, s.a);
            return s;
        }

        void Update()
        {
            float grown = Mathf.Min(len, (Time.time - t0) * speed);
            var head = Vector3.Lerp(a, b, len > 0.01f ? grown / len : 1f);
            lr.SetPosition(0, head);                                    // 머리(흰 심지) = 뻗어 가는 끝
            lr.SetPosition(1, a);
            var p = PlayerController.Instance;
            if (!hit && p && p.health.Alive)
            {
                var d = Effects.Flat(b - a); float L = d.magnitude;
                if (L > 0.01f)
                {
                    d /= L;
                    var rel = Effects.Flat(p.transform.position - a);
                    float along = Vector3.Dot(rel, d);
                    if (along >= 0 && along <= grown && (rel - d * along).magnitude <= width * 0.5f + 0.35f) { hit = true; p.health.TakeDamage(dmg); }
                }
            }
            float done = len / Mathf.Max(0.01f, speed);
            if (Time.time - t0 > done + 0.35f) Destroy(gameObject);
            else if (Time.time - t0 > done) lr.widthMultiplier = width * 1.4f * (1f - (Time.time - t0 - done) / 0.35f);
        }
    }

    /// <summary>허공 탄 곡사 (균열술사): 보라 구슬이 포물선으로 날아가 원 자리에 떨어짐 → 피해 + 허공 파열. 던진 적이 죽어도 끝까지 감</summary>
    public class VoidLob : MonoBehaviour
    {
        Vector3 from, to; float flight, radius, damage, t;
        Transform body, halo;
        TrailRenderer trail;

        public static void Launch(Vector3 from, Vector3 to, float flight, float radius, float damage)
        {
            var go = new GameObject("VoidLob");
            go.transform.position = from;
            var mesh = EnemyProjectile.BuildBody(go.transform, EnemyProjectile.Style.Orb);
            EnemyProjectile.TintBody(mesh, Enemy.Ch5Magenta, new MaterialPropertyBlock());
            var v = go.AddComponent<VoidLob>();
            v.from = from; v.to = to; v.flight = Mathf.Max(0.05f, flight); v.radius = radius; v.damage = damage;
            v.body = mesh.transform;
            v.halo = EnemyProjectile.BuildGlow(go.transform, new Color(1f, 0.18f, 0.7f, 0.76f));
            v.trail = EnemyProjectile.BuildTrail(go.transform, Enemy.Ch5Magenta, 0.34f, 0.19f);
            v.Pose();
            EffectShadowLight.Projectile(v.body, Enemy.Ch5Magenta, 0.23f);
        }

        void Pose()
        {
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.4f;
            var tangent = to - from + Vector3.up * (Mathf.Cos(t * Mathf.PI) * Mathf.PI * 2.4f);
            float push = Mathf.Lerp(-0.20f, 0.34f, WeaponSwingRig.Ease(t / 0.11f))
                * (1f - WeaponSwingRig.Ease((t - 0.11f) / 0.29f));
            float squash = WeaponSwingRig.Ease((t - 0.79f) / 0.21f) * 0.23f;
            float stretch = push - squash;
            if (tangent.sqrMagnitude > 0.0001f)
            {
                var up = Mathf.Abs(Vector3.Dot(tangent.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
                body.rotation = Quaternion.LookRotation(tangent, up) * Quaternion.AngleAxis(t * 190f, Vector3.forward);
            }
            body.localScale = new Vector3(1f - stretch * 0.45f, 1f - stretch * 0.45f, 1f + stretch) * 1.15f;
            var cam = Camera.main;
            if (cam) halo.rotation = cam.transform.rotation;
            halo.localScale = Vector3.one * 1.45f;
        }

        void Update()
        {
            t += Time.deltaTime / flight;
            if (t < 1f)
            {
                Pose();
                return;
            }
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - to).magnitude <= radius) p.health.TakeDamage(damage);
            Enemy.Fx5("c5voidburst", to + Vector3.up * 0.3f, 1.6f, 0.35f);
            StylizedFx.Shockwave(to, radius, StylizedFx.Void, 0.35f);
            Sfx.Play("e5_void_land", 0.55f);
            PropHit.Area(to, radius, damage);
            CameraFollow.Shake(0.04f);
            body.gameObject.SetActive(false);
            halo.gameObject.SetActive(false);
            trail.emitting = false;
            enabled = false;
            Destroy(gameObject, trail.time);
        }
    }
}
