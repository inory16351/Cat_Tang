// 2장 해금 묶음 (2026-11-25 사용자 확정, 명세 Art/tools/specs/ch2_unlock_spec.md)
//   총 스킬: 가스등 랜턴 '랜턴 던지기'(빛 장판) · 기관차 '증기 돌진' — 나머지 총은 지금처럼 백스텝 샷
//   공명 사격 강화: 미련 특성(피해 · 반경 · 6단계 푸른 원혼) · 연착(둔화된 적에게 +) · 끝나지 않는 여정(공명에 둔화 · 반경 +1) · 랜턴 장판 안 반경 +1
//   새 탄 효과: 마지막 기차(직선 돌진 관통 · 끝 공명 폭발) · 환승역(가둠 옮겨 붙음) · 레일 위의 불꽃(치명타 폭발) · 연기 고리(처치 폭발)
// 이펙트 프리팹(FX_*)은 Resources/FX/Prefabs — 아직 없으면 기존 이펙트로 대신 그림
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTang
{
    public partial class PlayerController
    {
        // ---------- 총 스킬 ----------
        static bool NewSkill(SkillDef sk) => sk != null && (sk.Action == "LanternField" || sk.Action == "SteamDash");

        /// <summary>우클릭 스킬: 양손 총 중 새 스킬을 가진 총(오른손 먼저)의 스킬, 없으면 null = 백스텝 샷</summary>
        public SkillDef CurrentSkill
        {
            get
            {
                foreach (var g in new[] { rev.rightGun, rev.leftGun })
                    if (g != null && Tables.Skills.TryGetValue(g.SkillId, out var sk) && NewSkill(sk)) return sk;
                return null;
            }
        }

        IEnumerator UseSkill()
        {
            var sk = CurrentSkill;
            if (sk == null) { yield return BackstepShot(); yield break; }
            nextSkill = Time.time + sk.Cooldown;
            foreach (var e in Effects.Global(this, "OnSkill")) if (e.Type == "HeatGain") rev.AddHeat(e.Value);
            if (sk.Action == "LanternField") yield return LanternThrow(sk);
            else yield return SteamDash(sk);
        }

        static readonly Color LanternAmber = new(1f, 0.72f, 0.35f);
        static readonly Color SteamWhite = new(0.92f, 0.94f, 1f);

        /// <summary>랜턴 던지기: 조준한 곳(최대 9m)으로 포물선 → 떨어진 자리에 빛 장판</summary>
        IEnumerator LanternThrow(SkillDef sk)
        {
            var from = transform.position + Vector3.up * 1.2f;
            var to = transform.position + aimDir * Mathf.Clamp(aimDist, 2.5f, 9f);
            if (Physics.Raycast(to + Vector3.up * 5f, Vector3.down, out var gh, 12f, ~0, QueryTriggerInteraction.Ignore)) to.y = gh.point.y;
            if (poser) poser.Recoil(true, 0.6f);
            Sfx.Play("dash", 0.5f, 0.05f);
            var pts = new Vector3[12];
            for (int k = 0; k < pts.Length; k++)
            {
                float u = k / (pts.Length - 1f);
                pts[k] = Vector3.Lerp(from, to + Vector3.up * 0.3f, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 2.2f;
            }
            Tracer.SpawnPath(pts, LanternAmber, 0.07f, false, false);
            yield return new WaitForSeconds(0.28f);
            LanternField.Spawn(to, sk.Value2 > 0 ? sk.Value2 : 4f, sk.Duration > 0 ? sk.Duration : 4f, sk.Value);
        }

        /// <summary>증기 돌진: 조준 방향으로 돌진(무적)하며 지나간 적에게 관통 사격 → 끝에서 증기 폭발</summary>
        IEnumerator SteamDash(SkillDef sk)
        {
            int s = Mathf.Clamp(rev.next, 1, 12);
            float dist = sk.Value2 > 0 ? sk.Value2 : 8f, dur = 0.22f * dist / 6f;   // 2026-11-25 사용자: 돌진 6 → 8m (속도는 그대로, 시간이 늘어남)
            var dir = aimDir;
            var start = transform.position;
            health.invulnerable = true;
            Sfx.Play(Sfx.Has("e_steam") ? "e_steam" : "dash", 0.8f, 0.05f);
            if (Sfx.Has("e_whistle")) Sfx.Play("e_whistle", 0.45f, 0.05f);
            Afterimage.Spawn(model, new Color(0.85f, 0.9f, 1f, 0.6f));
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            if (!FxEffect.Spawn("FX_SteamDash", start + Vector3.up * 0.6f, rot, dist)) FxBurst.Spawn(start + Vector3.up * 0.6f, 1.6f, FxBurst.Kind.Dust, SteamWhite);
            var hitSet = new HashSet<Health>();
            float baseDmg = ShotDamage(s);
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                cc.Move(dir * (dist / dur) * Time.deltaTime);
                // 경로 적: 몸 주변 1.4m에 닿으면 관통 사격 한 발씩
                foreach (var en in Enemy.All.ToArray())
                {
                    if (!en || !en.health.Alive || hitSet.Contains(en.health)) continue;
                    if (Effects.Flat(en.transform.position - transform.position).magnitude > 1.4f) continue;
                    hitSet.Add(en.health);
                    var at = en.transform.position + Vector3.up * 0.9f;
                    Tracer.Spawn(transform.position + Vector3.up * 0.9f, at, SteamWhite, 0.12f, true);
                    bool crit = CritChanceFor(s) > Random.value;
                    en.health.HitFrom(dir);
                    ApplyHit(s, en.health, baseDmg * (crit ? CritDamage : 1f), at, 1f, dir, SteamWhite, crit);
                }
                yield return null;
            }
            health.invulnerable = false;
            var end = transform.position + dir * 0.6f;
            float r = 3f;
            if (!FxEffect.Spawn("FX_SteamBlast", end, rot, r / 3f)) GunkataFx.Explosion(end, r, true, SteamWhite);
            Sfx.Play(Sfx.Has("e_steam") ? "e_steam" : "shot", 0.9f, 0.05f);
            CameraFollow.Shake(0.28f);
            Effects.AreaDamage(end, r, baseDmg * (sk.Value > 0 ? sk.Value : 1.5f));
            if (poser) { poser.Recoil(true, 1.3f); poser.Recoil(false, 1.3f); }
        }

        // ---------- 개발용 (시연 녹화 Dev/Ch2UnlockDemo) ----------
        /// <summary>그 방향으로 지금 칸 한 발 (critNext면 확정 치명타)</summary>
        public void DevFire(Vector3 dir, bool critNext = false)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 1e-4f) aimDir = dir.normalized;
            if (critNext) critShots += 1;
            if (rev.reloading) FinishReload();
            FireNext();
        }

        /// <summary>그 자리를 조준해 우클릭 스킬</summary>
        public void DevSkill(Vector3 at)
        {
            var d = at - transform.position; d.y = 0;
            if (d.sqrMagnitude > 1e-4f) { aimDir = d.normalized; aimDist = d.magnitude; }
            nextSkill = 0;
            StartCoroutine(UseSkill());
        }

        // ---------- 공명 사격 강화 ----------
        /// <summary>공명 수치 합: 이 칸 사격의 패시브(이 탄 · 특성 · 총 · 코스튬) + 다른 활성화된 탄의 '모든 탄' 패시브 (끝나지 않는 여정)</summary>
        float ResMod(int s, string type) => Effects.Passive(this, s, type) + AllBulletsPassive(s, type, out _);

        float AllBulletsPassive(int skip, string type, out float dur)
        {
            float v = 0; dur = 0;
            for (int i = 1; i <= 12; i++)
            {
                if (i == skip || !rev.Awakened(i)) continue;
                var b = rev.slots[i].bullet;
                if (b == null) continue;
                foreach (var id in new[] { b.Effect1, b.Effect2 })
                    if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Trigger == "Passive" && e.Target == "AllBullets" && e.Type == type)
                    { v += e.Value; dur = Mathf.Max(dur, e.Duration); }
            }
            return v;
        }

        float ResSlowDuration(int s)
        {
            AllBulletsPassive(s, "ResonanceSlow", out float d);
            foreach (var e in Effects.For(this, s, "Passive")) if (e.Type == "ResonanceSlow") d = Mathf.Max(d, e.Duration);
            return d;
        }

        /// <summary>공명 범위 피해 (예전 ResonanceBlast(end, 2, dmg×30%)): 반경 · 피해는 미련 · 탄 · 랜턴 장판으로 늘고, 둔화 · 원혼이 붙음.
        /// baseDmg = 그 사격 피해(치명타 포함). forceRadius &gt; 0이면 그 반경 (마지막 기차 끝 폭발)</summary>
        void Resonance(int s, Vector3 at, float baseDmg, Color col, bool show, float forceRadius = 0f)
        {
            float mul = 0.3f + ResMod(s, "ResonanceDamageAdd");
            float radius = forceRadius > 0 ? forceRadius : 2f + ResMod(s, "ResonanceRadiusAdd") + (LanternField.Boosts(at) ? 1f : 0f);
            float dmg = baseDmg * mul * (1 + Effects.GlobalPassive(this, "AreaDamageAdd"));
            float vsSlow = Effects.Passive(this, s, "ResonanceVsSlowedAdd");
            float slow = ResMod(s, "ResonanceSlow"), slowDur = ResSlowDuration(s);
            if (show) GunkataFx.SmallFlash(at, 1.2f * Mathf.Max(1f, radius / 2f), col);
            if (slow > 0 && show) FxBurst.Spawn(at, radius * 1.3f, FxBurst.Kind.Converge, new Color(0.55f, 0.6f, 1f), 0.35f);
            foreach (var en in Enemy.All.ToArray())
            {
                if (!en || !en.health.Alive || Effects.Flat(en.transform.position - at).magnitude >= radius) continue;
                en.health.TakeDamage(dmg * (en.health.Slowed ? 1 + vsSlow : 1f));
                if (slow > 0) en.health.ApplySlow(Mathf.Min(0.8f, slow), Mathf.Max(0.5f, slowDur) * ControlDurationMul);
            }
            // 미련 6단계: 0.6초 뒤 그 자리에 푸른 원혼이 피어나 울림
            foreach (var e in Effects.Global(this, "Passive"))
                if (e.Type == "ResonanceWraith") { StartCoroutine(Wraith(at, e.Value2 > 0 ? e.Value2 : 3f, dmg * e.Value, Mathf.Max(0.1f, e.Duration))); break; }
        }

        public static readonly Color WraithBlue = new(0.44f, 0.49f, 1f);

        IEnumerator Wraith(Vector3 at, float radius, float dmg, float delay)
        {
            at.y = transform.position.y;
            if (!FxEffect.Spawn("FX_LingerWraith", at, Quaternion.identity, radius / 3f, WraithBlue))
                FxBurst.Spawn(at + Vector3.up * 0.4f, radius * 1.2f, FxBurst.Kind.Converge, WraithBlue, delay);
            yield return new WaitForSeconds(delay);
            if (!FxEffect.Has("FX_LingerWraith")) GunkataFx.Explosion(at, radius, false, WraithBlue);
            if (Sfx.Has("e4_bell_2")) Sfx.Play("e4_bell_2", 0.35f, 0.15f);
            Effects.AreaDamage(at, radius, dmg);
        }

        // ---------- 새 탄 효과 ----------
        /// <summary>마지막 기차: 이 칸이 '기차 탄'이면 그 효과 (관통 성장 = Value, 끝 폭발 반경 = Value2)</summary>
        EffectDef TrainOf(int s)
        {
            foreach (var e in Effects.For(this, s, "Passive")) if (e.Type == "TrainShot") return e;
            return null;
        }

        /// <summary>기차 탄두가 총구에서 끝까지 달려감 (판정은 이미 끝남, 그림만)</summary>
        IEnumerator TrainRun(Vector3 from, Vector3 to)
        {
            var d = to - from; d.y = 0;
            if (d.sqrMagnitude < 0.01f) yield break;
            var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            var fx = FxEffect.Spawn("FX_TrainShot", from, rot, 1.6f);
            if (Sfx.Has("e_whistle")) Sfx.Play("e_whistle", 0.6f, 0.05f);
            if (!fx) yield break;
            float len = d.magnitude, dur = Mathf.Clamp(len / 60f, 0.12f, 0.35f);
            for (float t = 0; t < dur && fx; t += Time.deltaTime)
            {
                fx.transform.position = Vector3.Lerp(from, to, t / dur);
                yield return null;
            }
            if (fx) fx.transform.position = to;
        }

        /// <summary>환승역: 갇힌 적을 맞히면 반경 안 가장 가까운 '안 갇힌' 다른 적에게 가둠이 옮겨 붙음</summary>
        void LockTransfer(Health from, float radius, float dur)
        {
            Enemy best = null; float bd = radius;
            foreach (var en in Enemy.All)
            {
                if (!en || !en.health.Alive || en.health == from || en.health.TimeLocked) continue;
                float d = Effects.Flat(en.transform.position - from.transform.position).magnitude;
                if (d < bd) { bd = d; best = en; }
            }
            if (!best) return;
            specialCount++;
            best.health.ApplyTimeLock(dur);
            best.WatchTimeLock(this);
            OblivionFx.Lock(best.health.transform);
            var a = from.transform.position + Vector3.up * 0.9f;
            var b = best.transform.position + Vector3.up * 0.9f;
            Tracer.Spawn(a, b, new Color(0.6f, 0.85f, 1f), 0.1f, true);
            var look = Effects.Flat(b - a);
            var rot = look.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(look.normalized, Vector3.up) : Quaternion.identity;
            if (!FxEffect.Spawn("FX_LockTransfer", a, rot, 1f)) FxBurst.Spawn(a, 1f, FxBurst.Kind.Spark, new Color(0.6f, 0.85f, 1f));
            FxEffect.Spawn("FX_LockTransfer", b, rot, 1f);
        }

        /// <summary>레일 위의 불꽃: 치명타 자리에서 한쪽으로 긁히듯 튀는 불꽃 폭발</summary>
        void RailSpark(Vector3 at, Vector3 dir, float radius, float dmg)
        {
            specialCount++;
            var f = Effects.Flat(dir);
            var rot = f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f.normalized, Vector3.up) : Quaternion.identity;
            var col = new Color(1f, 0.62f, 0.25f);
            if (!FxEffect.Spawn("FX_RailSpark", at, rot, radius / 2f, col)) GunkataFx.Explosion(at, radius, false, col);
            Effects.AreaDamage(at, radius, dmg);
        }

        /// <summary>연기 고리: 처치한 자리에서 수평으로 퍼지는 연기 고리 폭발</summary>
        void SmokeRing(Vector3 at, float radius, float dmg)
        {
            specialCount++;
            var col = new Color(0.8f, 0.8f, 0.85f);
            if (!FxEffect.Spawn("FX_SmokeRing", at, Quaternion.identity, radius / 3f, col)) GunkataFx.Explosion(at, radius, true, col);
            CameraFollow.Shake(0.12f);
            Effects.AreaDamage(at, radius, dmg);
        }
    }

    /// <summary>가스등 랜턴 '랜턴 던지기' 빛 장판: 안의 적 둔화 · 안에서 터지는 공명 반경 +1</summary>
    public class LanternField : MonoBehaviour
    {
        public static readonly List<LanternField> All = new();
        float radius, until, slow, tick;

        public static LanternField Spawn(Vector3 at, float radius, float dur, float slow)
        {
            var go = new GameObject("LanternField");
            var root = RunManager.Instance ? RunManager.Instance.StageRoot : null;
            if (root) go.transform.SetParent(root, true);
            go.transform.position = at;
            var f = go.AddComponent<LanternField>();
            f.radius = radius; f.until = Time.time + dur; f.slow = slow;
            if (!FxEffect.Spawn("FX_LanternField", at, Quaternion.identity, radius / 4f))
            {
                Telegraph.Ring(at, radius, dur, new Color(1f, 0.72f, 0.35f, 0.8f));
                var l = new GameObject("Glow").AddComponent<Light>();
                l.transform.SetParent(go.transform, false); l.transform.localPosition = Vector3.up * 1.2f;
                l.type = LightType.Point; l.color = new Color(1f, 0.72f, 0.35f); l.range = radius * 1.6f; l.intensity = 2.5f;
            }
            if (Sfx.Has("e4_bell_1")) Sfx.Play("e4_bell_1", 0.3f, 0.1f);
            return f;
        }

        public static bool Boosts(Vector3 p)
        {
            foreach (var f in All) if (f && Effects.Flat(p - f.transform.position).magnitude < f.radius) return true;
            return false;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (Time.time >= until) { Destroy(gameObject); return; }
            if ((tick -= Time.deltaTime) > 0) return;
            tick = 0.25f;
            foreach (var en in Enemy.All)
                if (en && en.health.Alive && Effects.Flat(en.transform.position - transform.position).magnitude < radius)
                    en.health.ApplySlow(slow, 0.4f);
        }
    }
}
