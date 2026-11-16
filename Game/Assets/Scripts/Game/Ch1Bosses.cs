// 1장 보스 두 마리 (1장 기획서 Ver01)
// - 중간 보스 '태엽 현상금 사냥꾼': 산탄 부채꼴 · 회전 난사(가까움 5m) / 수배 저격 · 돌진 태클(멂) / 하수인 호출(거리 무관, 필드 2명 이하일 때만)
//   기믹 '현상금 표식': 20초마다 숫자(2~4) → 5초 뒤 발사 시각이 그 숫자의 배수면 최대 체력 20% 피해
// - 보스 '녹은 시계 거인': 휘두르기 · 웅덩이(가까움 6m) / 12방향 숫자 탄 · 되감기 폭발(멂) / 시간 지연 구역(거리 무관)
//   상시 '초침 탄' 매 초 1발, 기믹 '시각 패턴'은 체력 75 · 50 · 25%에 확정
// 패턴 고르기는 BossBrain(PatternPicker): 거리에 맞는 패턴 + 거리 무관 패턴 동일 확률, 직전 패턴 제외
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    static class BossFx
    {
        public static readonly Color Safe = new(0.45f, 1f, 0.6f);

        public static void Toast(string msg) { var ui = Object.FindFirstObjectByType<GameUI>(); if (ui) ui.Toast(msg); }

        /// <summary>머리 위 숫자 표시 (TextMesh, 카메라를 봄)</summary>
        public static TextMesh Label(Transform parent, Vector3 local, string text, Color c, float size = 0.06f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = c;
            tm.text = text;
            go.AddComponent<Billboard>();
            return tm;
        }

        public static IEnumerator Blast(Vector3 at, float radius, float delay, float dmg)
        {
            Telegraph.Ring(at, radius, delay, Telegraph.Danger);
            yield return new WaitForSeconds(delay);
            FxBurst.Spawn(at + Vector3.up * 0.3f, radius * 1.2f, FxBurst.Kind.Explosion);
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= radius) p.health.TakeDamage(dmg);
        }
    }

    /// <summary>녹은 웅덩이: 6초 남아 밟으면 이속 30% 감소</summary>
    public class MeltPuddle : MonoBehaviour
    {
        public float radius = 1.5f, slow = 0.3f, until;

        public static void Spawn(Vector3 at, float radius, float life, float slow)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "MeltPuddle";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = at + Vector3.up * 0.02f;
            go.transform.localScale = new Vector3(radius * 2, 0.01f, radius * 2);
            go.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(new Color(0.95f, 0.62f, 0.2f), 0);
            var m = go.AddComponent<MeltPuddle>();
            m.radius = radius; m.slow = slow; m.until = Time.time + life;
        }

        void Update()
        {
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude <= radius) p.health.ApplySlow(slow, 0.15f);
            float left = until - Time.time;
            if (left < 0.6f) transform.localScale = new Vector3(radius * 2, 0.01f, radius * 2) * Mathf.Max(0.01f, left / 0.6f);
            if (left <= 0) Destroy(gameObject);
        }
    }

    // =====================================================================
    // 중간 보스: 태엽 현상금 사냥꾼
    // =====================================================================
    public class Ch1MidBoss : BossBrain
    {
        public override float NearDist => 5f;
        public override float PatternGap => 1.1f;
        public override string DisplayName => "태엽 현상금 사냥꾼";

        public const float BountyEvery = 20f, BountyCount = 5f, BountyDmg = 0.2f;
        float nextBounty;
        bool bountyOn;

        protected override void Setup()
        {
            Add("산탄 부채꼴", Reach.Near, Shotgun);
            Add("회전 난사", Reach.Near, SpinFire);
            Add("수배 저격", Reach.Far, Snipe);
            Add("돌진 태클", Reach.Far, Tackle);
            Add("하수인 호출", Reach.Any, Summon, () => Enemy.All.Count - 1 <= 2);
            nextBounty = Time.time + 8f;
        }

        // 산탄 사거리를 잡으려고 계속 다가옴 (이속 4)
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist > 3.5f) transform.position += dir * spd * Time.deltaTime;
        }

        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            if (!bountyOn && Time.time >= nextBounty) StartCoroutine(Bounty());
        }

        // 현상금 표식: 고양이 머리 위 숫자, 5초 동안 쏘거나 장전해 바늘을 옮김
        IEnumerator Bounty()
        {
            bountyOn = true;
            var p = P;
            int mark = Random.Range(2, 5);
            var tm = BossFx.Label(p.transform, new Vector3(0, 2.6f, 0), mark.ToString(), new Color(1f, 0.85f, 0.3f), 0.06f);
            BossFx.Toast($"현상금 표식 {mark} — 5초 뒤 발사 시각이 {mark}의 배수면 피해");
            float end = Time.time + BountyCount;
            while (Time.time < end && p && p.health.Alive && hp.Alive)
            {
                int left = Mathf.CeilToInt(end - Time.time);
                bool danger = p.rev.next % mark == 0;
                tm.text = $"{mark}\n<size=40>{left}</size>";
                tm.color = danger ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.85f, 0.3f);
                yield return null;
            }
            if (tm) Destroy(tm.gameObject);
            if (p && p.health.Alive && hp.Alive)
            {
                if (p.rev.next % mark == 0)
                {
                    p.health.TakeDamage(p.health.maxHp * BountyDmg);
                    FxBurst.Spawn(p.transform.position + Vector3.up, 1.6f, FxBurst.Kind.Explosion);
                    BossFx.Toast($"현상금 집행 — {p.rev.next}시는 {mark}의 배수");
                }
                else BossFx.Toast("현상금 표식 회피");
            }
            nextBounty = Time.time + BountyEvery - BountyCount;
            bountyOn = false;
        }

        // 산탄 부채꼴: 5발 · 60° · 사거리 7m · 발당 5
        IEnumerator Shotgun()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 7f, 30f, T(0.45f), Telegraph.Danger);
            float end = Time.time + T(0.45f);
            while (Time.time < end) { Face(d, 14f); yield return null; }
            for (int i = 0; i < 5; i++) Shoot(Quaternion.Euler(0, -30 + 15 * i, 0) * d, 14f, 5f * self.chapterDmg, 1.2f, 7f);
            GunkataFx.MuzzleFlash(transform.position + Vector3.up * 1.2f + d * 1.4f, d, 1.2f);
            yield return Wait(T(0.35f));
        }

        // 회전 난사: 2초 동안 한 바퀴 반, 0.1초마다 양쪽으로 · 발당 8
        IEnumerator SpinFire()
        {
            Telegraph.Ring(transform.position, 2.2f, T(0.4f), Telegraph.Danger, transform);
            yield return Wait(T(0.4f));
            float dur = 2f, step = T(0.1f), a0 = transform.eulerAngles.y;
            int shots = Mathf.RoundToInt(dur / 0.1f);
            for (int i = 0; i < shots; i++)
            {
                float a = a0 + 540f * i / shots;
                transform.rotation = Quaternion.Euler(0, a, 0);
                var f = transform.forward;
                Shoot(f, 9f, 8f * self.chapterDmg, 1.2f);
                Shoot(-f, 9f, 8f * self.chapterDmg, 1.2f);
                yield return Wait(step);
            }
        }

        // 수배 저격: 조준선이 1.2초 쫓아오다 0.4초 고정 → 관통탄 한 발 · 피해 15
        IEnumerator Snipe()
        {
            var d = ToPlayer();
            var t = Telegraph.Lane(transform.position, d, 30f, 0.18f, T(1.6f), Telegraph.Aim, transform);
            float end = Time.time + T(1.2f);
            while (Time.time < end) { d = ToPlayer(); t.SetDir(d); Face(d, 14f); yield return null; }
            t.SetColor(Telegraph.Danger);
            yield return Wait(T(0.4f));
            Shoot(d, 28f, 15f * self.chapterDmg, 1.3f);
            GunkataFx.MuzzleFlash(transform.position + Vector3.up * 1.3f + d * 1.4f, d, 1f);
            yield return Wait(T(0.3f));
        }

        // 돌진 태클: 예고 0.6초 · 14m/초 · 피해 15, 벽에 부딪히면 2초 기절
        IEnumerator Tackle()
        {
            var d = ToPlayer();
            Telegraph.Lane(transform.position, d, 22f, 2f, T(0.6f), Telegraph.Danger);
            float end = Time.time + T(0.6f);
            while (Time.time < end) { Face(d, 14f); yield return null; }
            bool hit = false;
            float run = 0;
            var rm = RunManager.Instance;
            while (run < 22f)
            {
                float step = 14f * hp.SlowMul * Time.deltaTime;
                var before = transform.position;
                transform.position += d * step;
                if (rm) transform.position = rm.ClampToRoom(transform.position, 3f);
                float moved = (transform.position - before).magnitude;
                run += step;
                if (!hit && P && Effects.Flat(P.transform.position - transform.position).magnitude < 1.8f)
                {
                    hit = true;
                    P.health.TakeDamage(15f * self.chapterDmg);
                    P.health.Knockback(d * 8f);
                }
                if (moved < step * 0.3f)
                {
                    // 벽 · 건물에 부딪힘 → 기절 (큰 빈틈)
                    FxBurst.Spawn(transform.position + Vector3.up * 1.5f + d, 2f, FxBurst.Kind.Explosion);
                    BossFx.Toast("벽에 부딪혀 기절!");
                    hp.ApplyStun(2f);
                    yield return Wait(0.1f);
                    break;
                }
                yield return null;
            }
            yield return Wait(T(0.4f));
        }

        // 하수인 호출: 태엽 무법자 2명 (필드 최대 4명)
        IEnumerator Summon()
        {
            yield return Wait(T(0.5f));
            int room = 4 - (Enemy.All.Count - 1);
            for (int i = 0; i < Mathf.Min(2, room); i++)
            {
                var pos = transform.position + transform.right * (i == 0 ? -3f : 3f);
                if (RunManager.Instance) pos = RunManager.Instance.ClampToRoom(pos, 1.5f);
                Telegraph.Ring(pos, 0.7f, 0.6f, Telegraph.Aim);
                var e = EnemyFactory.Outlaw(pos, self.chapter ?? Tables.Chapter(1));
                RunManager.Instance?.AddEnemy(e);
            }
            yield return Wait(T(0.4f));
        }
    }

    // =====================================================================
    // 보스: 녹은 시계 거인
    // =====================================================================
    public class Ch1Boss : BossBrain
    {
        public override float NearDist => 6f;
        public override float PatternGap => 1.3f;
        public override string DisplayName => "녹은 시계 거인";

        float secondAngle, nextSecond;
        readonly Queue<(float t, Vector3 p)> trail = new();
        float nextSample;
        readonly HashSet<int> trialsDone = new();
        Transform hand, hand2;
        TextMesh face;

        protected override void Setup()
        {
            Add("시계 바늘 휘두르기", Reach.Near, Sweep);
            Add("녹은 웅덩이", Reach.Near, Puddles);
            Add("12방향 숫자 탄", Reach.Far, Numbers);
            Add("되감기 폭발", Reach.Far, Rewind);
            Add("시간 지연 구역", Reach.Any, SlowZone);
            hand = transform.Find("Hand");
            hand2 = transform.Find("Hand2");
            nextSecond = Time.time + 1f;
        }

        void Start()
        {
            face = BossFx.Label(transform, new Vector3(0, 5.2f, 0), "", new Color(1f, 0.9f, 0.55f), 0.1f);
        }

        // 느리게 걸어서 따라옴 (이속 3)
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist > 3f) transform.position += dir * spd * Time.deltaTime;
        }

        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            // 초침 탄: 초침이 돌며 매 초 그 방향으로 바늘 탄 1발 (탄속 9 · 피해 10)
            if (Time.time >= nextSecond)
            {
                secondAngle += 30f;
                Shoot(Quaternion.Euler(0, secondAngle, 0) * Vector3.forward, 9f, 10f * self.chapterDmg, 2.2f);
                nextSecond = Time.time + T(1f);
            }
            if (hand && !busy) hand.localRotation = Quaternion.Slerp(hand.localRotation, Quaternion.Euler(0, secondAngle, 0), 8f * Time.deltaTime);
            // 되감기용 고양이 발자취 (0.1초마다, 3.5초치)
            if (Time.time >= nextSample)
            {
                trail.Enqueue((Time.time, p.transform.position));
                while (trail.Count > 0 && trail.Peek().t < Time.time - 3.5f) trail.Dequeue();
                nextSample = Time.time + 0.1f;
            }
        }

        // 체력 75 · 50 · 25%에 한 번씩 확정 등장
        protected override System.Func<IEnumerator> Forced()
        {
            foreach (int th in new[] { 75, 50, 25 })
                if (!trialsDone.Contains(th) && hp.hp <= hp.maxHp * th / 100f) { trialsDone.Add(th); return ClockTrial; }
            return null;
        }

        // 시각 패턴: 얼굴 시계에 시각, 안전 지대(반경 2m). 5초 뒤 안전 지대 안 + 발사 시각 일치면 파훼, 아니면 최대 체력 40%
        IEnumerator ClockTrial()
        {
            var p = P;
            int hour = Random.Range(1, 13);
            if (hour == p.rev.next) hour = hour % 12 + 1;          // 처음부터 맞춰져 있지 않게
            var rm = RunManager.Instance;
            Vector3 c = rm ? rm.RoomCenter : transform.position;
            Vector2 half = rm ? rm.RoomSize / 2 - new Vector2(3.5f, 3.5f) : new Vector2(8, 8);
            Vector3 zone;
            int tries = 0;
            do zone = c + new Vector3(Random.Range(-half.x, half.x), 0, Random.Range(-half.y, half.y));
            while (Effects.Flat(zone - transform.position).magnitude < 4f && ++tries < 12);
            Telegraph.Ring(zone, 2f, 5f, BossFx.Safe);
            var zoneMark = BossFx.Label(null, zone + Vector3.up * 0.6f, hour.ToString(), BossFx.Safe, 0.08f);
            BossFx.Toast($"시각 패턴 — 안전 지대에서 발사 시각을 {hour}시로 맞춰라");
            float end = Time.time + 5f;
            while (Time.time < end)
            {
                if (face) face.text = $"{hour}시\n<size=40>{Mathf.CeilToInt(end - Time.time)}</size>";
                if (hand) hand.localRotation = Quaternion.Euler(0, hour * 30f, 0);
                yield return null;
            }
            if (face) face.text = "";
            if (zoneMark) Destroy(zoneMark.gameObject);
            if (!p || !p.health.Alive) yield break;
            bool inZone = Effects.Flat(p.transform.position - zone).magnitude <= 2f;
            bool timeOk = p.rev.next == hour;
            if (inZone && timeOk)
            {
                BossFx.Toast("시각 패턴 파훼!");
                FxBurst.Spawn(zone + Vector3.up, 2.5f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.35f));
            }
            else
            {
                p.health.TakeDamage(p.health.maxHp * 0.4f);
                FxBurst.Spawn(p.transform.position + Vector3.up, 3f, FxBurst.Kind.Explosion);
                BossFx.Toast(!inZone ? "안전 지대 밖 — 시각 패턴 실패" : $"발사 시각 {p.rev.next}시 ≠ {hour}시 — 시각 패턴 실패");
            }
            yield return Wait(T(0.6f));
        }

        // 시계 바늘 휘두르기: 반지름 8m, 안전 부채꼴 90°만 남김 · 예고 0.8초 · 피해 20
        IEnumerator Sweep()
        {
            var safe = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
            Telegraph.Ring(transform.position, 8f, T(0.8f), Telegraph.Danger);
            Telegraph.Sector(transform.position, safe, 8f, 45f, T(0.8f), BossFx.Safe);
            yield return Wait(T(0.8f));
            float a0 = hand ? hand.localEulerAngles.y : 0;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                if (hand) hand.localRotation = Quaternion.Euler(0, a0 + 720f * t / 0.3f, 0);
                if (hand2) hand2.localRotation = Quaternion.Euler(0, -a0 - 720f * t / 0.3f, 0);
                yield return null;
            }
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= 8f && Vector3.Angle(to, safe) > 45f) p.health.TakeDamage(20f * self.chapterDmg);
            }
            GunkataFx.Swirl(transform.position + Vector3.up, 8f, 720f);
            yield return Wait(T(0.4f));
        }

        // 녹은 웅덩이: 고양이 주변 원 4개 (반경 1.5) · 예고 0.8초 · 피해 15 → 6초 남아 이속 30% 감소
        IEnumerator Puddles()
        {
            var at = P.transform.position;
            var spots = new List<Vector3> { at };
            for (int i = 0; i < 3; i++)
                spots.Add(at + Quaternion.Euler(0, i * 120 + Random.Range(-25f, 25f), 0) * Vector3.forward * Random.Range(2.2f, 3.6f));
            foreach (var s in spots) Telegraph.Ring(s, 1.5f, T(0.8f), Telegraph.Danger);
            yield return Wait(T(0.8f));
            bool hurt = false;
            foreach (var s in spots)
            {
                if (!hurt) hurt = HitCircle(s, 1.5f, 15f * self.chapterDmg);
                FxBurst.Spawn(s + Vector3.up * 0.2f, 1.6f, FxBurst.Kind.Gold, new Color(1f, 0.6f, 0.2f));
                MeltPuddle.Spawn(s, 1.5f, 6f, 0.3f);
            }
            yield return Wait(T(0.4f));
        }

        // 12방향 숫자 탄: 1시부터 12시까지 0.1초 간격 · 탄속 8 · 피해 15
        IEnumerator Numbers()
        {
            Telegraph.Ring(transform.position, 2.5f, T(0.5f), Telegraph.Aim, transform);
            yield return Wait(T(0.5f));
            for (int h = 1; h <= 12; h++)
            {
                if (face) face.text = h.ToString();
                if (hand) hand.localRotation = Quaternion.Euler(0, h * 30f, 0);
                Shoot(Quaternion.Euler(0, h * 30f, 0) * Vector3.forward, 8f, 15f * self.chapterDmg, 1.6f);
                yield return Wait(T(0.1f));
            }
            if (face) face.text = "";
        }

        // 되감기 폭발: 3초 전부터 고양이가 지나온 길 6곳이 0.25초 간격으로 폭발 (반경 1.5 · 피해 15)
        IEnumerator Rewind()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                float when = Time.time - 3f + i * 0.5f;
                Vector3 best = P.transform.position; float bestDt = 99;
                foreach (var s in trail) { float dt = Mathf.Abs(s.t - when); if (dt < bestDt) { bestDt = dt; best = s.p; } }
                pts.Add(best);
            }
            for (int i = 0; i < pts.Count; i++) StartCoroutine(BossFx.Blast(pts[i], 1.5f, T(0.6f + 0.25f * i), 15f * self.chapterDmg));
            yield return Wait(T(0.6f + 0.25f * pts.Count));
        }

        // 시간 지연 구역: 반경 4m · 4초, 원 안에서 이속 · 공속 40% 감소, 그 안으로 탄이 쏟아짐
        IEnumerator SlowZone()
        {
            StartCoroutine(SlowZoneRun(P.transform.position));
            yield return Wait(T(0.6f));
        }

        IEnumerator SlowZoneRun(Vector3 at)
        {
            Telegraph.Ring(at, 4f, 4f, new Color(0.6f, 0.45f, 1f));
            float end = Time.time + 4f, nextDrop = Time.time + 0.3f;
            while (Time.time < end && hp.Alive)
            {
                var p = P;
                if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= 4f)
                {
                    p.health.ApplySlow(0.4f, 0.15f);
                    p.health.ApplyAttackSpeedDown(0.4f, 0.15f);
                }
                if (Time.time >= nextDrop)
                {
                    var drop = at + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(0f, 3.4f);
                    StartCoroutine(BossFx.Blast(drop, 0.8f, 0.5f, 10f * self.chapterDmg));
                    nextDrop = Time.time + 0.3f;
                }
                yield return null;
            }
        }
    }
}
