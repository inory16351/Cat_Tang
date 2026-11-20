// 2장 보스 두 마리 (docs/Cat_Tang_레벨_디자인.md '태엽 역장' · '멈춘 기관차 거인')
// 수치는 기획 값 그대로(2장 값은 이미 1장보다 25% 높게 잡혀 있어 챕터 피해 배율을 곱하지 않음)
// 패턴 고르기 = 모든 보스 공통 규칙: 거리에 맞는 패턴 + 거리 무관 패턴 동일 확률, 직전 패턴 제외 (BossBrain · PatternPicker)
//
// - 중간 보스 '태엽 역장' (대합실 28×24m, 체력 2520 · 이속 3.5 · 가까움 5m)
//   가까우면 신호봉 휘두르기 · 개찰 펀치 / 멀면 시간표 탄막 · 호루라기 열차 / 거리 무관 차장 호출(필드 2명 이하)
//   기믹 '빨간불': 25초마다 호루라기 + 노란불 1초 → 빨간불 3초(움직이면 최대 체력 20%, 한 번) + 느린 표 탄 6발(쏘면 부서짐) → 초록불
// - 보스 '멈춘 기관차 거인' (전차대 지름 24m, 체력 4860 · 이속 2.5 · 가까움 6m, 2페이즈)
//   가까우면 연결봉 휘두르기 · 증기 분출 / 멀면 레일 돌진 · 석탄 포격 / 거리 무관 객차 회전(1페이즈만)
//   상시 '연기 링' 3초마다(빈틈 45°), 기믹 '출발 시각'은 체력 75 · 50 · 25%에 확정
//   체력 50%: 객차를 떼어 냄 → 경기장 둘레를 상시 회전, 레일 돌진 2연속 (이속 · 패턴 간격 1.2배는 BossBrain.PhaseMul)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    static class Ch2Fx
    {
        public static readonly Color Yellow = new(1f, 0.82f, 0.2f);
        public static readonly Color Red = new(1f, 0.18f, 0.2f);
        public static readonly Color Green = new(0.35f, 1f, 0.5f);
        public static readonly Color Smoke = new(0.78f, 0.8f, 0.86f);

        static Material lineMat;
        public static Material LineMat()
        {
            if (!lineMat) lineMat = new Material(Shader.Find("Sprites/Default"));
            return lineMat;
        }

        /// <summary>도형 대체 몸 (모델이 없을 때)</summary>
        public static GameObject Box(string name, Transform parent, Vector3 local, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(c, 3f);
            return go;
        }

        /// <summary>2장 키트 모델(Resources/Kit/Ch2/이름) 또는 적 모델(Resources/Enemies/이름) 하나를 띄움. 없으면 null</summary>
        public static GameObject Model(string name, Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Kit/Ch2/" + name) ?? Resources.Load<GameObject>("Enemies/" + name);
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return go;
        }

        /// <summary>플레이어가 선분(from → from + dir·len) 폭 안이면 true</summary>
        public static bool PlayerInLane(Vector3 from, Vector3 dir, float len, float width)
        {
            var p = PlayerController.Instance;
            return p && p.health.Alive && Ch2Kit.InLane(p.transform.position, from, dir, len, width);
        }
    }

    // =====================================================================
    // 중간 보스: 태엽 역장
    // =====================================================================
    public class Ch2MidBoss : BossBrain
    {
        public override float NearDist => 5f;
        public override float PatternGap => 1.1f;
        public override string DisplayName => "태엽 역장";

        public const float RedEvery = 25f, YellowTime = 1f, RedTime = 3f, RedDmg = 0.2f;
        float nextRed;
        BigLabel lamp;                                   // 머리 위 신호등 (노랑 · 빨강 · 초록)

        protected override void Setup()
        {
            Add("신호봉 휘두르기", Reach.Near, Baton, null, "Special");
            Add("개찰 펀치", Reach.Near, GatePunch);
            Add("시간표 탄막", Reach.Far, Timetable, null, "Schedule");
            Add("호루라기 열차", Reach.Far, WhistleTrain, null, "Whistle");
            Add("차장 호출", Reach.Any, Summon, () => Enemy.All.Count - 1 <= 2, "Call");
            nextRed = Time.time + 12f;
        }

        // 신호봉을 들고 고양이를 따라오며 거리를 좁힘
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist > 3f) transform.position += dir * spd * Time.deltaTime;
        }

        // 빨간불은 패턴 사이에 확정으로 끼어듦 (공격과 겹치지 않게)
        protected override System.Func<IEnumerator> Forced() => Time.time >= nextRed ? RedLight : null;

        IEnumerator RedLight()
        {
            var p = P;
            if (!lamp) lamp = Label(transform, new Vector3(0, 4.9f, 0), "", Ch2Fx.Yellow, 1.5f);
            // 호루라기 + 노란불 1초
            if (self) self.Anim("Special");
            BossFx.Toast("호루라기 — 노란불! 곧 빨간불, 멈춰서 표 탄을 쏴 떨어뜨려라");
            lamp.text = "●"; lamp.color = Ch2Fx.Yellow;
            Telegraph.Ring(p.transform.position, 1.2f, YellowTime, Ch2Fx.Yellow, p.transform);
            yield return Wait(YellowTime);
            // 빨간불 3초: 움직이면(대시 포함) 최대 체력 20% 한 번, 느린 표 탄 6발이 둘레에서 고양이를 향해
            lamp.color = Ch2Fx.Red;
            BossFx.Toast("빨간불 — 움직이지 마!");
            Telegraph.Ring(p.transform.position, 1.2f, RedTime, Ch2Fx.Red, p.transform);
            Vector3 anchor = p.transform.position;
            bool punished = false;
            float end = Time.time + RedTime;
            int fired = 0;
            float a0 = Random.Range(0f, 360f);
            while (Time.time < end && p && p.health.Alive && hp.Alive)
            {
                // 표 탄: 0.2초 간격으로 6발, 고양이 둘레 8m(벽 안쪽)에서 → 4m/초면 2초 남짓에 도착
                if (fired < 6 && Time.time >= end - RedTime + fired * 0.2f)
                {
                    var from = anchor + Quaternion.Euler(0, a0 + fired * 60f, 0) * Vector3.forward * 8f;
                    var rm = RunManager.Instance;
                    if (rm) from = rm.ClampToRoom(from, 1f);
                    var b = EnemyProjectile.Spawn(from + Vector3.up * 1.1f, anchor - from, 4f, 15f, 0, EnemyProjectile.Style.Ticket);
                    b.shootable = true;
                    fired++;
                }
                bool moved = Effects.Flat(p.transform.position - anchor).magnitude > 0.25f || p.health.dashing;
                if (moved && !punished)
                {
                    punished = true;
                    p.health.TakeDamage(p.health.maxHp * RedDmg);
                    FxBurst.Spawn(p.transform.position + Vector3.up, 2f, FxBurst.Kind.Explosion, Ch2Fx.Red);
                    BossFx.Toast("빨간불에 움직였다!");
                }
                if (moved) anchor = p.transform.position;          // 한 번만 벌함 (계속 움직여도 더는 안 깎음)
                yield return null;
            }
            // 초록불
            if (lamp) { lamp.color = Ch2Fx.Green; }
            BossFx.Toast("초록불");
            yield return Wait(0.6f);
            if (lamp) lamp.text = "";
            nextRed = Time.time + T(RedEvery - YellowTime - RedTime);
        }

        // 신호봉 휘두르기: 예고 0.6초, 전방 반원 반경 3.5m, 피해 18
        IEnumerator Baton()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 3.5f, 90f, T(0.6f), Telegraph.Danger);
            float end = Time.time + T(0.6f);
            while (Time.time < end) { Face(d, 14f); yield return null; }
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= 3.5f && Vector3.Dot(to, d) >= 0) p.health.TakeDamage(18f);
            }
            PropHit.Area(transform.position + d * 1.75f, 2f, 18f);
            GunkataFx.Swirl(transform.position + Vector3.up + d * 0.8f, 3.5f, 200f);
            yield return Wait(T(0.45f));
        }

        // 개찰 펀치: 짧은 돌진 3연타, 각 3m, 0.4초 간격, 피해 15
        IEnumerator GatePunch()
        {
            var rm = RunManager.Instance;
            bool hit = false;
            for (int k = 0; k < 3; k++)
            {
                var d = ToPlayer();
                float tele = k == 0 ? T(0.5f) : T(0.25f);
                var laneFrom = transform.position;
                Telegraph.Lane(laneFrom, d, 3.6f, 1.8f, tele, Telegraph.Danger);
                float end = Time.time + tele;
                while (Time.time < end) { Face(d, 18f); yield return null; }
                for (float t = 0; t < 0.15f; t += Time.deltaTime)
                {
                    transform.position += d * (3f / 0.15f) * Time.deltaTime;
                    if (rm) transform.position = rm.ClampToRoom(transform.position, 1.5f);
                    float mv = Effects.Flat(transform.position - laneFrom).magnitude;
                    if (!hit && P && Telegraph.InLane(P.transform.position, laneFrom, d, 3.6f, 1.8f, mv - 1.3f, mv + 1.3f))   // 예고 선과 같게
                    {
                        hit = true;                                  // 3연타 중 한 번만 (몸통 접촉 피해 없음 규칙)
                        P.health.TakeDamage(15f);
                        P.health.Knockback(d * 5f);
                    }
                    yield return null;
                }
                PropHit.Area(transform.position + d, 1.5f, 15f);
                FxBurst.Spawn(transform.position + Vector3.up * 1.2f + d, 1.2f, FxBurst.Kind.Spark, Ch2Fx.Yellow);
                yield return Wait(Mathf.Max(0, T(0.4f) - 0.15f));
            }
            yield return Wait(T(0.35f));
        }

        // 시간표 탄막: 전광판처럼 뒤집히는 가로 줄 탄 4줄, 줄마다 빈칸 1곳, 0.6초 간격, 6m/초, 피해 15
        IEnumerator Timetable()
        {
            var d = ToPlayer();
            var side = Vector3.Cross(Vector3.up, d);
            Telegraph.Lane(transform.position, d, 6f, 14f, T(0.5f), Telegraph.Aim, transform);
            float end = Time.time + T(0.5f);
            while (Time.time < end) { Face(d, 14f); yield return null; }
            int lastGap = 99;
            for (int row = 0; row < 4; row++)
            {
                // 빈칸 자리(폭 2.2m)는 줄마다 바뀜 — 고양이 쪽 ±4m 안에서
                var toP = P ? Effects.Flat(P.transform.position - transform.position) : d * 8f;
                float center = Vector3.Dot(toP, side);
                int gap;
                do gap = Random.Range(-3, 4); while (gap == lastGap);
                lastGap = gap;
                float gapAt = center + gap * 1.3f;
                var origin = transform.position + d * 1.2f;
                for (float x = -7f; x <= 7f; x += 1f)
                {
                    float lat = center + x;
                    if (Mathf.Abs(lat - gapAt) < 1.1f) continue;
                    EnemyProjectile.Spawn(origin + side * lat + Vector3.up * 1.2f, d, 6f, 15f, 18f, EnemyProjectile.Style.Ticket);
                }
                FxBurst.Spawn(origin + Vector3.up * 1.5f, 1.2f, FxBurst.Kind.Spark, Ch2Fx.Yellow);
                yield return Wait(T(0.6f));
            }
            yield return Wait(T(0.3f));
        }

        // 호루라기 열차: 바닥 선 3줄(폭 2m) 예고 1초 → 증기 열차 모형이 20m/초로 통과, 피해 18
        IEnumerator WhistleTrain()
        {
            var rm = RunManager.Instance;
            Vector3 c = rm ? rm.RoomCenter : transform.position;
            Vector2 size = rm ? rm.RoomSize : new Vector2(28, 24);
            var p = P;
            // 줄 방향: 동서(가로) 또는 남북 — 고양이가 있는 줄 + 양옆
            bool alongX = Random.value < 0.5f;
            var dir = alongX ? Vector3.right : Vector3.forward;
            if (Random.value < 0.5f) dir = -dir;
            var across = alongX ? Vector3.forward : Vector3.right;
            float pc = p ? Vector3.Dot(p.transform.position - c, across) : 0;
            float half = (alongX ? size.y : size.x) / 2 - 1.5f;
            float spacing = Random.Range(3.2f, 4.2f);
            float len = (alongX ? size.x : size.y);
            var lanes = new List<Vector3>();
            for (int k = -1; k <= 1; k++)
            {
                float off = Mathf.Clamp(pc + k * spacing + Random.Range(-0.6f, 0.6f), -half, half);
                lanes.Add(c + across * off - dir * (len / 2));
            }
            BossFx.Toast("호루라기 — 열차가 지나간다");
            foreach (var l in lanes) Telegraph.Lane(l, dir, len, 2f, T(1f), Telegraph.Danger);
            Sfx.Play("e_whistle", 0.85f);                                           // 호루라기를 부는 박자
            yield return Wait(T(1f));
            foreach (var l in lanes) ToyTrain.Run(l, dir, len, 20f, 2f, 18f, 0.45f);
            yield return Wait(T(0.6f));
        }

        // 차장 호출: 태엽 차장 2명 (필드 최대 4명)
        IEnumerator Summon()
        {
            Sfx.Play("e_whistle", 0.8f);
            yield return Wait(T(0.5f));
            int room = 4 - (Enemy.All.Count - 1);
            for (int i = 0; i < Mathf.Min(2, room); i++)
            {
                var pos = transform.position + transform.right * (i == 0 ? -3f : 3f);
                if (RunManager.Instance) pos = RunManager.Instance.ClampToRoom(pos, 1.5f);
                Telegraph.Ring(pos, 0.7f, 0.6f, Telegraph.Aim);
                var e = EnemyFactory.Conductor(pos, self.chapter ?? Tables.Chapter(2));
                RunManager.Instance?.AddEnemy(e);
            }
            yield return Wait(T(0.4f));
        }
    }

    /// <summary>호루라기 열차 · 지나가는 열차: 선로를 한 번 훑고 사라지는 열차. 몸이 닿은 것은 한 번씩 피해</summary>
    public class ToyTrain : MonoBehaviour
    {
        Vector3 dir, start;
        float len, speed, width, playerDmg, enemyDmg, bodyLen, run;
        readonly HashSet<Health> done = new();

        /// <param name="scale">열차 모델 배율 (호루라기 열차 = 모형이라 작게)</param>
        public static ToyTrain Run(Vector3 from, Vector3 dir, float len, float speed, float width, float playerDmg, float scale, float enemyDmg = 0, int cars = 2)
        {
            var go = new GameObject(enemyDmg > 0 ? "PassingTrain" : "WhistleTrain");
            go.transform.position = from - dir * 2f;
            var t = go.AddComponent<ToyTrain>();
            t.dir = dir; t.start = from; t.len = len; t.speed = speed; t.width = width;
            t.playerDmg = playerDmg; t.enemyDmg = enemyDmg;
            t.bodyLen = t.Build(scale, cars);                       // 돌리기 전에 조립 (LocalBounds가 월드 축으로 잼)
            go.transform.rotation = Quaternion.LookRotation(dir);
            CameraFollow.Shake(0.08f);
            return t;
        }

        // 기관차 + 객차 (블렌더 모델 Kit/Ch2/TrainEngine · TrainCar 가 있으면, 없으면 상자)
        float Build(float scale, int cars)
        {
            float z = 0;
            var eng = Ch2Fx.Model("TrainEngine", transform);
            float engLen = 9f;
            if (eng) { eng.transform.localScale *= scale; engLen = Kit.LocalBounds(eng).size.z; eng.transform.localPosition = new Vector3(0, 0, -engLen / 2); }   // 모델 피벗 = 가운데 → 앞끝을 머리 자리에
            else
            {
                var b = new GameObject("Engine").transform; b.SetParent(transform, false);
                Ch2Fx.Box("Body", b, new Vector3(0, 1.4f, -4.5f), new Vector3(2.4f, 2.8f, 9f), new Color(0.3f, 0.28f, 0.34f));
                Ch2Fx.Box("Light", b, new Vector3(0, 2.2f, 0.05f), new Vector3(0.9f, 0.9f, 0.2f), new Color(1f, 0.9f, 0.5f));
                b.localScale = Vector3.one * scale;
                engLen = 9f * scale;
            }
            z = -engLen;
            for (int i = 0; i < cars; i++)
            {
                var car = Ch2Fx.Model("TrainCar", transform);
                float carLen = 8f * scale;
                if (car) { car.transform.localScale *= scale; float cl = Kit.LocalBounds(car).size.z; carLen = cl + 0.2f * scale; car.transform.localPosition = new Vector3(0, 0, z - 0.2f * scale - cl / 2); }
                else
                {
                    var b = new GameObject("Car").transform; b.SetParent(transform, false);
                    Ch2Fx.Box("Body", b, new Vector3(0, 1.3f, -4f), new Vector3(2.3f, 2.6f, 7.8f), new Color(0.45f, 0.3f, 0.26f));
                    b.localScale = Vector3.one * scale;
                    b.localPosition = new Vector3(0, 0, z);
                }
                z -= carLen;
            }
            return -z;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += dir * step;
            run += step;
            // 몸 전체(머리 ~ 꼬리)가 지나간 칸만 판정: 머리 위치 기준 뒤로 bodyLen
            var head = transform.position;
            var tail = head - dir * bodyLen;
            var segFrom = Vector3.Dot(tail - start, dir) < 0 ? start : tail;
            float segLen = Mathf.Max(0, Vector3.Dot(head - segFrom, dir));
            if (segLen > 0 && Vector3.Dot(head - start, dir) > 0)
            {
                var p = PlayerController.Instance;
                if (p && p.health.Alive && !done.Contains(p.health) && Ch2Kit.InLane(p.transform.position, segFrom, dir, segLen, width + 0.3f))
                {
                    done.Add(p.health);
                    p.health.TakeDamage(playerDmg);
                    p.health.Knockback(Vector3.Cross(Vector3.up, dir) * (Vector3.Dot(p.transform.position - head, Vector3.Cross(Vector3.up, dir)) >= 0 ? 6f : -6f));
                }
                if (enemyDmg > 0)
                    foreach (var en in Enemy.All.ToArray())
                        if (en && en.health.Alive && !done.Contains(en.health) && Ch2Kit.InLane(en.transform.position, segFrom, dir, segLen, width + 0.5f)) { done.Add(en.health); en.health.TakeDamage(enemyDmg); }
                PropHit.Area(head, width / 2 + 0.3f, enemyDmg > 0 ? enemyDmg : playerDmg, done);
            }
            if (run > len + bodyLen + 4f) Destroy(gameObject);
        }
    }

    /// <summary>석탄 불씨 장판: 밟으면 초당 피해</summary>
    public class EmberPatch : MonoBehaviour
    {
        float radius, dps, until;

        public static void Spawn(Vector3 at, float radius, float life, float dps)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "EmberPatch";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = at + Vector3.up * 0.02f;
            go.transform.localScale = new Vector3(radius * 2, 0.01f, radius * 2);
            go.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(new Color(1f, 0.38f, 0.12f), 0);
            var e = go.AddComponent<EmberPatch>();
            e.radius = radius; e.dps = dps; e.until = Time.time + life;
        }

        void Update()
        {
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude <= radius) p.health.TakeDamage(dps * Time.deltaTime);
            float left = until - Time.time;
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 9f);
            transform.localScale = new Vector3(radius * 2 * pulse, 0.01f, radius * 2 * pulse) * Mathf.Clamp01(left / 0.5f);
            if (left <= 0) Destroy(gameObject);
        }
    }

    /// <summary>
    /// 연기 링: 굴뚝에서 퍼지는 고리 (빈틈 1곳), 고양이가 고리 앞 선에 닿으면 한 번 피해.
    /// 겉모습 = 블렌더로 구운 연기 플립북(FX_SmokePuff, MCP로 만든 프리팹)이 고리 둘레에 늘어서 함께 퍼지며 부풂 + 앞 선을 알리는 옅은 줄
    /// </summary>
    public class SmokeRing : MonoBehaviour
    {
        float r, speed, dmg, maxR, gapHalf, startAt;
        Vector3 gapDir;
        bool hit, begun;
        LineRenderer lr;
        readonly List<(Transform t, Vector3 dir, int ver)> puffs = new();   // ver = 풀 사용 번호 (풀링 2026-11-20: 수명이 끝나 풀로 돌아가 다른 데 쓰인 뒤엔 건드리지 않음)
        const int Seg = 72, Puffs = 30;

        /// <param name="delay">이만큼 기다렸다가 굴뚝에서 나옴 (3겹 연달아)</param>
        public static void Spawn(Vector3 at, float speed, float dmg, float maxR, Vector3 gapDir, float gapDeg = 45f, float delay = 0f)
        {
            var go = new GameObject("SmokeRing");
            go.transform.position = new Vector3(at.x, 0.7f, at.z);
            var s = go.AddComponent<SmokeRing>();
            s.speed = speed; s.dmg = dmg; s.maxR = maxR; s.gapDir = Effects.Flat(gapDir).normalized; s.gapHalf = gapDeg / 2; s.r = 1.2f;
            s.startAt = Time.time + delay;
        }

        void Begin()
        {
            begun = true;
            lr = gameObject.AddComponent<LineRenderer>();
            lr.sharedMaterial = Ch2Fx.LineMat();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.22f;
            lr.numCapVertices = 3;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 연기 뭉치: 빈틈을 뺀 호에 고르게 (수명 = 링이 끝까지 퍼지는 시간)
            float g0 = Mathf.Atan2(gapDir.x, gapDir.z) * Mathf.Rad2Deg;
            for (int i = 0; i < Puffs; i++)
            {
                float a = (g0 + gapHalf + 6f + (360f - gapHalf * 2 - 12f) * (i + 0.5f) / Puffs) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var fx = FxEffect.Spawn("FX_SmokePuff", transform.position + dir * r, Quaternion.identity, Random.Range(1.5f, 1.9f));
                if (fx) puffs.Add((fx.transform, dir, GameObjectPool.Version(fx.gameObject)));
            }
            FxBurst.Spawn(transform.position + Vector3.up * 5.5f, 1.8f, FxBurst.Kind.Dust, Ch2Fx.Smoke);
        }

        void Update()
        {
            if (Time.time < startAt) return;
            if (!begun) Begin();
            r += speed * Time.deltaTime;
            if (r > maxR) { Destroy(gameObject); return; }
            foreach (var (t, dir, ver) in puffs) if (t && GameObjectPool.IsCurrent(t.gameObject, ver)) t.position = new Vector3(transform.position.x, t.position.y, transform.position.z) + dir * r;
            // 앞 선: 빈틈을 뺀 호
            float g0 = Mathf.Atan2(gapDir.x, gapDir.z) * Mathf.Rad2Deg;
            lr.positionCount = Seg + 1;
            for (int i = 0; i <= Seg; i++)
            {
                float a = (g0 + gapHalf + (360f - gapHalf * 2) * i / Seg) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Sin(a) * r, -0.6f, Mathf.Cos(a) * r));          // 바닥 위 0.1m
            }
            var c = Ch2Fx.Smoke; c.a = Mathf.Clamp01((maxR - r) / 3f) * 0.55f;
            lr.startColor = lr.endColor = c;
            if (hit) return;
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive) return;
            var to = Effects.Flat(p.transform.position - transform.position);
            if (Mathf.Abs(to.magnitude - r) > 0.45f) return;
            if (Vector3.Angle(to, gapDir) <= gapHalf) return;           // 빈틈으로 빠짐
            hit = true;
            p.health.TakeDamage(dmg);
        }

        void OnDestroy()
        {
            foreach (var (t, _, ver) in puffs) if (t && GameObjectPool.IsCurrent(t.gameObject, ver)) GameObjectPool.Release(t.gameObject);
        }
    }

    // =====================================================================
    // 보스: 멈춘 기관차 거인
    // =====================================================================
    public class Ch2Boss : BossBrain
    {
        public override float NearDist => 6f;
        public override float PatternGap => 1.3f;
        public override string DisplayName => "멈춘 기관차 거인";

        const float RingEvery = 5f, RingSpeed = 4.5f, RingDmg = 12f, RingLayerGap = 0.6f;   // 2026-11-18 사용자: 너무 잦고 빠름 → 5초마다 · 4.5m/초 (겹 사이 0.6초 = 약 2.7m 유지)
        const int RingLayers = 3;
        const float DepartTime = 10f, DepartStun = 3f, DepartFailDmg = 0.4f;
        const int Coaches = 4;
        const float CoachLap = 6f, CoachDmg = 20f, CoachBand = 2f;

        float nextRing;
        readonly HashSet<int> departDone = new();
        bool phase2;
        BigLabel clock;                                  // 헤드라이트 시계 (출발 시각)

        // 객차: 1페이즈엔 보스 뒤에 줄줄이 매달림, 객차 회전 패턴 · 2페이즈엔 경기장 둘레를 돎
        readonly List<Transform> coaches = new();
        readonly List<Vector3> trail = new();            // 보스가 지나온 자리 (객차가 따라옴)
        bool coachesOrbit;
        float orbitAngle, orbitSpeed;
        readonly Dictionary<int, float> coachHitAt = new();

        // 출발 시각 판정 (OnFired)
        int departHour;
        bool departOn, departHit;

        protected override void Setup()
        {
            Add("연결봉 휘두르기", Reach.Near, Rods, null, "Special");
            Add("증기 분출", Reach.Near, SteamBurst, null, "Vent");
            Add("레일 돌진", Reach.Far, RailCharge);
            Add("석탄 포격", Reach.Far, CoalBarrage, null, "Heave");
            Add("객차 회전", Reach.Any, CoachSpin, () => !phase2 && !coachesOrbit, "CoachSpin");
            nextRing = Time.time + 2f;
        }

        void Start()
        {
            clock = Label(transform, new Vector3(0, 7.2f, 0), "", new Color(1f, 0.92f, 0.6f), 1.5f);
            BuildCoaches();
            if (P) P.OnFired += OnPlayerFired;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (P) P.OnFired -= OnPlayerFired;
            foreach (var c in coaches) if (c) Destroy(c.gameObject);
            coaches.Clear();
        }

        void BuildCoaches()
        {
            for (int i = 0; i < Coaches; i++)
            {
                var root = new GameObject("BossCoach_" + i).transform;
                root.position = transform.position - transform.forward * (4f + i * 4.6f);
                var m = Ch2Fx.Model("Ch2_BossCoach", root) ?? Ch2Fx.Model("BossCoach", root);
                if (m)
                {
                    // 길이 4.2m로 맞춤 (모델 앞 = +Z)
                    var b = Kit.LocalBounds(m);
                    float s = 4.2f / Mathf.Max(0.5f, b.size.z);
                    m.transform.localScale *= s;
                }
                else
                {
                    Ch2Fx.Box("Body", root, new Vector3(0, 1.1f, 0), new Vector3(1.9f, 2.0f, 4.2f), new Color(0.36f, 0.27f, 0.5f));
                    Ch2Fx.Box("Roof", root, new Vector3(0, 2.2f, 0), new Vector3(2.1f, 0.25f, 4.4f), new Color(0.85f, 0.65f, 0.25f));
                }
                coaches.Add(root);
            }
        }

        // 느리게 걸어서 따라옴
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist > 3.5f) transform.position += dir * spd * Time.deltaTime;
        }

        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            // 상시 연기 링 3겹 (2026-11-18 사용자: 거리로 컨트롤하는 감각): 5초마다 0.6초 간격으로 3개(4.5m/초, 사이 약 2.7m).
            // 빈틈(45°)은 겹마다 같은 쪽으로 40~70° 밀림 → 거리를 조절하며 옆으로 비켜 빈틈 셋을 차례로 통과
            if (Time.time >= nextRing)
            {
                float a = Random.Range(0f, 360f), turn = (Random.value < 0.5f ? -1f : 1f);
                for (int k = 0; k < RingLayers; k++)
                {
                    SmokeRing.Spawn(transform.position, RingSpeed, RingDmg, 18f, Quaternion.Euler(0, a, 0) * Vector3.forward, 45f, k * RingLayerGap);
                    a += turn * Random.Range(40f, 70f);
                }
                nextRing = Time.time + T(RingEvery);
            }
            // 2페이즈 진입: 객차를 떼어 냄 → 둘레 상시 회전
            if (!phase2 && hp.hp <= hp.maxHp * 0.5f)
            {
                phase2 = true;
                BossFx.Toast("객차를 떼어 냈다 — 경기장 둘레를 돈다!");
                CameraFollow.Shake(0.2f);
                StartOrbit(true);
            }
        }

        // ---------- 객차 ----------
        float ArenaR()
        {
            var rm = RunManager.Instance;
            var s = rm ? rm.RoomSize : new Vector2(24, 24);
            return Mathf.Min(Ch2Kit.BossArenaDiameter / 2f, Mathf.Min(s.x, s.y) / 2f) - CoachBand / 2f - 0.2f;   // 전차대 가장자리 (지름 32m → 약 14.8m)
        }

        Vector3 ArenaC() => RunManager.Instance ? RunManager.Instance.RoomCenter : transform.position;

        void StartOrbit(bool forever)
        {
            coachesOrbit = true;
            orbitSpeed = 360f / CoachLap;
            orbitAngle = Random.Range(0f, 360f);
            var c = ArenaC(); float r = ArenaR();
            foreach (var co in coaches) if (co) FxBurst.Spawn(co.position + Vector3.up, 1.5f, FxBurst.Kind.Dust, Ch2Fx.Smoke);
            if (!forever) return;
            Telegraph.Ring(c, r + CoachBand / 2, 1.2f, Telegraph.Danger);
        }

        void LateUpdate()
        {
            if (!hp || !hp.Alive) return;
            if (coachesOrbit) OrbitCoaches();
            else FollowCoaches();
        }

        void FollowCoaches()
        {
            // 보스 뒤로 지나온 자리를 따라 줄줄이 (간격 4.6m)
            var head = transform.position - transform.forward * 2.2f;
            if (trail.Count == 0 || (trail[0] - head).sqrMagnitude > 0.04f) trail.Insert(0, head);
            if (trail.Count > 400) trail.RemoveAt(trail.Count - 1);
            for (int i = 0; i < coaches.Count; i++)
            {
                var co = coaches[i];
                if (!co) continue;
                float want = 2.2f + i * 4.6f;
                Vector3 pos = PointAlongTrail(want, out Vector3 fwd);
                co.position = Vector3.Lerp(co.position, pos, 12f * Time.deltaTime);
                if (fwd.sqrMagnitude > 0.001f) co.rotation = Quaternion.Slerp(co.rotation, Quaternion.LookRotation(fwd), 10f * Time.deltaTime);
            }
        }

        Vector3 PointAlongTrail(float dist, out Vector3 fwd)
        {
            float acc = 0;
            var prev = transform.position - transform.forward * 2.2f;
            fwd = transform.forward;
            foreach (var t in trail)
            {
                float seg = (t - prev).magnitude;
                if (acc + seg >= dist && seg > 0.0001f)
                {
                    fwd = (prev - t).normalized;
                    return Vector3.Lerp(prev, t, (dist - acc) / seg);
                }
                acc += seg; prev = t;
            }
            // 기록이 짧으면 등 뒤 일직선 — 경기장 밖으로 나가는 만큼은 둘레를 따라 감음 (벽을 뚫고 나가 보이지 않게)
            var c = ArenaC(); float r = ArenaR();
            var straight = transform.position - transform.forward * (2.2f + dist - acc);
            var rel = Effects.Flat(straight - c);
            if (rel.magnitude <= r) return straight;
            float a0 = Mathf.Atan2(rel.x, rel.z);
            float extra = rel.magnitude - r;
            float a = a0 + extra / r;
            fwd = -new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
            return c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r;
        }

        void OrbitCoaches()
        {
            var c = ArenaC(); float r = ArenaR();
            orbitAngle += orbitSpeed * Time.deltaTime;
            float gapDeg = 4.6f / r * Mathf.Rad2Deg;
            var p = P;
            for (int i = 0; i < coaches.Count; i++)
            {
                var co = coaches[i];
                if (!co) continue;
                float a = (orbitAngle - i * gapDeg) * Mathf.Deg2Rad;
                var pos = c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r;
                var tangent = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
                co.position = Vector3.Lerp(co.position, pos, 8f * Time.deltaTime);
                co.rotation = Quaternion.Slerp(co.rotation, Quaternion.LookRotation(tangent), 10f * Time.deltaTime);
                // 닿으면 피해 20 (객차마다 1초에 한 번)
                if (p && p.health.Alive && Effects.Flat(p.transform.position - co.position).magnitude < 2.4f
                    && (!coachHitAt.TryGetValue(i, out float last) || Time.time - last > 1f))
                {
                    coachHitAt[i] = Time.time;
                    p.health.TakeDamage(CoachDmg);
                    p.health.Knockback(Effects.Flat(c - p.transform.position).normalized * 6f);
                }
            }
        }

        // 객차 회전 (1페이즈): 매달린 객차 4량이 경기장 둘레 폭 2m를 6초간 한 바퀴, 피해 20
        IEnumerator CoachSpin()
        {
            var c = ArenaC(); float r = ArenaR();
            Telegraph.Ring(c, r + CoachBand / 2, T(1f), Telegraph.Danger);
            BossFx.Toast("객차가 경기장 둘레를 돈다 — 안쪽으로!");
            yield return Wait(T(1f));
            StartOrbit(false);
            moveWhileBusy = true;
            yield return Wait(CoachLap);
            if (!phase2) { coachesOrbit = false; trail.Clear(); }
            yield return Wait(T(0.3f));
        }

        // ---------- 기믹: 출발 시각 ----------
        protected override System.Func<IEnumerator> Forced()
        {
            foreach (int th in new[] { 75, 50, 25 })
                if (!departDone.Contains(th) && hp.hp <= hp.maxHp * th / 100f) { departDone.Add(th); return Departure; }
            return null;
        }

        void OnPlayerFired(int slot, bool awake)
        {
            if (!departOn || departHit || slot != departHour || !hp) return;
            // 이 탄이 보스를 맞혔고(같은 프레임) 정면(헤드라이트 쪽 ±70°)에서 왔으면 출발 저지
            if (Time.time - hp.lastHitAt > 0.001f) return;
            if (Vector3.Angle(-hp.lastHitDir, transform.forward) > 70f) return;
            departHit = true;
        }

        IEnumerator Departure()
        {
            var p = P;
            int hour = Random.Range(4, 10);
            if (p && hour == p.rev.next) hour = hour == 9 ? 4 : hour + 1;
            departHour = hour; departHit = false; departOn = true;
            BossFx.Toast($"출발 준비 — 헤드라이트 시계 {hour}시! 바늘이 {hour}시일 때 쏜 탄을 헤드라이트에 맞혀라");
            if (self) self.Anim("Special");
            float end = Time.time + DepartTime;
            while (Time.time < end && hp.Alive && !departHit)
            {
                if (p) Face(ToPlayer(), 3f);
                int left = Mathf.CeilToInt(end - Time.time);
                bool ready = p && p.rev.next == hour;
                if (clock)
                {
                    clock.text = $"{hour}시\n<size=46>{left}초</size>";
                    clock.color = ready ? Ch2Fx.Green : new Color(1f, 0.92f, 0.6f);
                }
                yield return null;
            }
            departOn = false;
            if (clock) clock.text = "";
            if (!hp.Alive) yield break;
            if (departHit)
            {
                BossFx.Toast("출발 저지! 기관차 거인 기절");
                FxBurst.Spawn(transform.position + Vector3.up * 4f + transform.forward * 1.2f, 3f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.35f));
                CameraFollow.Shake(0.18f);
                hp.ApplyStun(DepartStun);
                yield return Wait(0.2f);
                yield break;
            }
            // 시간 초과: 경기장을 가로지르는 전속 돌진 (예고 선 1초, 최대 체력 40%)
            BossFx.Toast("출발 — 전속 돌진!");
            var d = ToPlayer();
            var c = ArenaC();
            float r = ArenaR() + 1f;
            Telegraph.Lane(transform.position, d, r * 2.2f, 3f, 1f, Telegraph.Danger);
            float t0 = Time.time + 1f;
            while (Time.time < t0) { Face(d, 14f); yield return null; }
            yield return Charge(d, r * 2.2f, 26f, () => P.health.maxHp * DepartFailDmg, 3f);
            yield return Wait(T(0.8f));
        }

        // ---------- 패턴 ----------
        // 연결봉 휘두르기: 양옆 부채꼴 100° · 6m, 앞뒤만 안전, 예고 0.8초, 피해 22
        IEnumerator Rods()
        {
            var f = ToPlayer();
            Face(f, 30f);
            var right = Vector3.Cross(Vector3.up, f);
            Telegraph.Sector(transform.position, right, 6f, 50f, T(0.8f), Telegraph.Danger, transform);
            Telegraph.Sector(transform.position, -right, 6f, 50f, T(0.8f), Telegraph.Danger, transform);
            yield return Wait(T(0.8f));
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= 6f && (Vector3.Angle(to, right) <= 50f || Vector3.Angle(to, -right) <= 50f)) p.health.TakeDamage(22f);
            }
            PropHit.Area(transform.position + right * 3f, 3f, 22f);
            PropHit.Area(transform.position - right * 3f, 3f, 22f);
            GunkataFx.Swirl(transform.position + Vector3.up * 1.4f, 6f, 360f);
            CameraFollow.Shake(0.12f);
            yield return Wait(T(0.5f));
        }

        // 증기 분출: 몸 주변 반경 7m, 반경 2~3.5m 링만 안전, 예고 1초, 피해 20
        IEnumerator SteamBurst()
        {
            Telegraph.Ring(transform.position, 7f, T(1f), Telegraph.Danger);
            Telegraph.Ring(transform.position, 3.5f, T(1f), BossFx.Safe);
            Telegraph.Ring(transform.position, 2f, T(1f), Telegraph.Danger);
            yield return Wait(T(1f));
            Sfx.Play("e_steam", 0.9f);
            var p = P;
            if (p && p.health.Alive)
            {
                float d = Effects.Flat(p.transform.position - transform.position).magnitude;
                if (d <= 7f && (d < 2f || d > 3.5f)) p.health.TakeDamage(20f);
            }
            PropHit.Area(transform.position, 7f, 20f);
            for (int i = 0; i < 10; i++)
            {
                var at = transform.position + Quaternion.Euler(0, i * 36f, 0) * Vector3.forward * (i % 2 == 0 ? 5.2f : 1.2f);
                FxBurst.Spawn(at + Vector3.up * 0.8f, 2.4f, FxBurst.Kind.Dust, Ch2Fx.Smoke);
            }
            yield return Wait(T(0.6f));
        }

        // 레일 돌진: 고양이와 가까운 선로가 빛나며 예고 1초 → 16m/초 일직선, 피해 25, 끝에서 1.5초 멈춤 (2페이즈 2연속)
        IEnumerator RailCharge()
        {
            int times = phase2 ? 2 : 1;
            for (int k = 0; k < times; k++)
            {
                var c = ArenaC();
                float r = ArenaR();
                var p = P;
                var rel = p ? Effects.Flat(p.transform.position - c) : Vector3.forward;
                // +자 선로 2줄 (동서 · 남북) 중 고양이와 가까운 줄
                bool alongX = Mathf.Abs(rel.z) < Mathf.Abs(rel.x);
                var axis = alongX ? Vector3.right : Vector3.forward;
                // 보스와 가까운 끝에서 출발해 반대쪽 끝까지
                var relB = Effects.Flat(transform.position - c);
                float sgn = Vector3.Dot(relB, axis) >= 0 ? 1f : -1f;
                var startPos = c + axis * sgn * r;
                var d = -axis * sgn;
                // 선로 끝으로 미끄러져 감 (0.5초)
                var from = transform.position;
                for (float t = 0; t < 0.5f; t += Time.deltaTime) { transform.position = Vector3.Lerp(from, startPos, t / 0.5f); Face(d, 10f); yield return null; }
                Telegraph.Lane(startPos, d, r * 2f, 3f, T(1f), Telegraph.Danger);
                yield return Wait(T(1f));
                yield return Charge(d, r * 2f, 16f, () => 25f, 3f);
                yield return Wait(T(1.5f));
            }
        }

        /// <summary>일직선 돌진: 지나간 몸이 고양이에 닿으면 한 번 피해 + 밀림, 엄폐물 · 폭발물도 맞힘</summary>
        IEnumerator Charge(Vector3 d, float len, float speed, System.Func<float> dmg, float width)
        {
            float run = 0;
            bool hit = false;
            var once = new HashSet<Health>();
            var rm = RunManager.Instance;
            var laneFrom = transform.position;                                      // 예고 선 시작 (돌진 패턴은 모두 지금 자리에서 예고)
            var cc = GetComponent<CapsuleCollider>(); float bodyR = cc ? cc.radius : 2f;
            while (run < len && hp.Alive)
            {
                float step = speed * hp.SlowMul * Time.deltaTime;
                transform.position += d * step;
                if (rm) transform.position = rm.ClampToRoom(transform.position, 2.5f);
                var fromC = Effects.Flat(transform.position - ArenaC());                   // 원형 경기장 안으로
                if (fromC.magnitude > ArenaR()) transform.position = ArenaC() + fromC.normalized * ArenaR();
                run += step;
                if (!hit && P && Telegraph.InLane(P.transform.position, laneFrom, d, len, width, run - bodyR, run + bodyR))   // 예고 선(폭 width)과 같게
                {
                    hit = true;
                    P.health.TakeDamage(dmg());
                    P.health.Knockback(Vector3.Cross(Vector3.up, d) * (Vector3.Dot(P.transform.position - transform.position, Vector3.Cross(Vector3.up, d)) >= 0 ? 9f : -9f));
                }
                PropHit.Area(transform.position, width / 2 + 0.5f, 40f, once);
                yield return null;
            }
            FxBurst.Spawn(transform.position + Vector3.up * 1.5f + d * 1.5f, 2.5f, FxBurst.Kind.Dust, Ch2Fx.Smoke);
            CameraFollow.Shake(0.15f);
        }

        // 석탄 포격: 고양이 주변 곡사 5발, 원 예고 1초, 반경 1.5m, 피해 18, 착지 자리에 불씨 장판 4초(초당 5)
        IEnumerator CoalBarrage()
        {
            var at = P.transform.position;
            var spots = new List<Vector3> { at };
            for (int i = 0; i < 4; i++)
                spots.Add(at + Quaternion.Euler(0, i * 90 + Random.Range(-30f, 30f), 0) * Vector3.forward * Random.Range(2.4f, 4.2f));
            var rm = RunManager.Instance;
            for (int i = 0; i < spots.Count; i++) if (rm) spots[i] = rm.ClampToRoom(spots[i], 1.5f);
            foreach (var s in spots) { Telegraph.Ring(s, 1.5f, T(1f), Telegraph.Danger); StartCoroutine(Lob(s, T(1f))); }
            yield return Wait(T(1f));
            bool hurt = false;
            foreach (var s in spots)
            {
                if (!hurt) hurt = HitCircle(s, 1.5f, 18f);
                PropHit.Area(s, 1.5f, 18f);
                FxBurst.Spawn(s + Vector3.up * 0.3f, 1.8f, FxBurst.Kind.Explosion);
                EmberPatch.Spawn(s, 1.3f, 4f, 5f);
            }
            yield return Wait(T(0.5f));
        }

        // 굴뚝에서 포물선으로 날아가는 석탄 덩이
        IEnumerator Lob(Vector3 to, float dur)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.6f;
            go.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(new Color(0.16f, 0.14f, 0.16f), 3f);
            var from = transform.position + Vector3.up * 6.4f;
            for (float t = 0; t < dur && go; t += Time.deltaTime)
            {
                float k = t / dur;
                go.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 5f;
                go.transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0);
                yield return null;
            }
            if (go) Destroy(go);
        }
    }
}
