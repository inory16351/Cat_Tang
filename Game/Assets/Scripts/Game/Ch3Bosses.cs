// 3장 보스 두 마리 (docs/Cat_Tang_레벨_디자인.md '신기루 마술사' · '거울 고래', 2026-11-18)
// 수치는 기획 값 그대로 (3장 값은 이미 높여 잡아 챕터 피해 배율을 곱하지 않음)
// 패턴 고르기 = 모든 보스 공통: 거리에 맞는 패턴 + 거리 무관 패턴 동일 확률, 직전 제외 (BossBrain · PatternPicker)
// 모델은 블렌더 + Codex (Art/tools/blender_kit_ch3_bosses.py, 리깅 없음) — 이름 붙은 조각(Head · ArmL · ArmR / Body · Tail · FinL · FinR · Jaw)을
// PuppetAnim · WhaleAnim이 돌려 움직임. 모델이 없으면 도형.
//
// - 중간 보스 '신기루 마술사' (거울 극장 36×32m, 체력 3960 · 이속 3.5 · 가까움 5m)
//   가까우면 카드 부채 · 칼 꽂기 상자 / 멀면 모자 속 비둘기 · 거울 상자 / 거리 무관 분신술사 호출(필드 2명 이하)
//   기믹 '진짜 찾기'(15초 뒤 첫 발동, 25초마다, 거울 시계 중엔 멈춤) · '거울 시계'(체력 66 · 33% 확정)
// - 보스 '거울 고래' (거울 제단 지름 40m, 체력 7020 · 이속 3 / 3.6 · 가까움 7m, 2페이즈)
//   상시: 반사 빛줄기(내내) + 물결 링(1페이즈) / 기억 비(2페이즈)
//   기믹 4종 쿨 20초 무작위(직전 제외, 쿨이 차면 기믹 우선, 기믹 중 쿨 정지): 거울에 비친 시각 · 고래의 노래 · 흩어진 기억 조각 · 기억 삼키기
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    static class Ch3Fx
    {
        public static readonly Color Pink = new(1f, 0.6f, 0.85f);
        public static readonly Color Light = new(0.8f, 0.95f, 1f);
        public static readonly Color Gold = new(1f, 0.85f, 0.4f);
        public static readonly Color Teal = new(0.35f, 0.95f, 0.9f);

        /// <summary>3장 키트 모델(Resources/Kit/Ch3/이름) 하나를 띄움 (충돌체 뺌). 없으면 null</summary>
        public static GameObject Model(string name, Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Kit/Ch3/" + name);
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return go;
        }

        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 local, Vector3 scale, Color c, float outline = 2f)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = RoomBuilder.Mat(c, outline);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>쏘면 맞는 과녁 (Health + 충돌체). 체력은 줄지 않고(맞을 때마다 다시 채움) OnDamaged로 알림</summary>
        public static Health Target(GameObject go, float radius, float height)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = radius; col.height = height; col.center = Vector3.up * height / 2;
            go.AddComponent<PlayerOnlyTarget>();                                   // 적 탄은 그냥 지나감 (고양이가 쏜 것만 셈)
            var h = go.AddComponent<Health>();
            h.maxHp = 1e6f; h.ResetHp(1e6f);
            h.OnDamaged += _ => h.ResetHp(h.maxHp);
            return h;
        }

        public static Vector3 ArenaC() => RunManager.Instance ? RunManager.Instance.RoomCenter : Vector3.zero;

        public static void PullPlayer(Vector3 to, float speed)
        {
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive || p.health.dashing) return;                // 대시로 빠져나옴
            var d = Effects.Flat(to - p.transform.position);
            if (d.magnitude < 0.3f) return;
            var cc = p.GetComponent<CharacterController>();
            var step = d.normalized * speed * Time.deltaTime;
            if (cc && cc.enabled) cc.Move(step); else p.transform.position += step;
        }

        /// <summary>바닥 시계 (거울 시계 · 거울에 비친 시각): 금 테 + 크림 판 + 12시 표시 + 바늘. 바늘 Transform을 돌려줌</summary>
        public static Transform FloorClock(Vector3 at, float radius, out GameObject root, bool numbers12 = true)
        {
            root = new GameObject("FloorClock");
            root.transform.position = new Vector3(at.x, 0.03f, at.z);
            Prim(PrimitiveType.Cylinder, "Rim", root.transform, Vector3.zero, new Vector3(radius * 2f, 0.02f, radius * 2f), Gold, 1f);
            Prim(PrimitiveType.Cylinder, "Face", root.transform, Vector3.up * 0.015f, new Vector3(radius * 1.82f, 0.02f, radius * 1.82f), new Color(1f, 0.96f, 0.88f), 1f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                bool big = i % 3 == 0;
                var tick = Prim(PrimitiveType.Cube, "Tick" + i, root.transform, new Vector3(Mathf.Sin(a), 0.03f, Mathf.Cos(a)) * radius * 0.78f + Vector3.up * 0.03f,
                    new Vector3(big ? 0.16f : 0.08f, 0.03f, big ? 0.4f : 0.22f), i == 0 && numbers12 ? new Color(0.85f, 0.15f, 0.25f) : new Color(0.35f, 0.3f, 0.45f), 1f);
                tick.transform.localRotation = Quaternion.Euler(0, i * 30f, 0);
            }
            var hand = new GameObject("Hand").transform;
            hand.SetParent(root.transform, false);
            hand.localPosition = Vector3.up * 0.06f;
            Prim(PrimitiveType.Cube, "Blade", hand, new Vector3(0, 0, radius * 0.38f), new Vector3(0.14f, 0.04f, radius * 0.76f), new Color(0.25f, 0.2f, 0.35f), 1f);
            Prim(PrimitiveType.Cylinder, "Hub", root.transform, Vector3.up * 0.07f, new Vector3(0.3f, 0.03f, 0.3f), Gold, 1f);
            return hand;
        }
    }

    /// <summary>고양이 탄만 맞는 과녁 표시 (진짜 찾기 분신 · 고래 기믹 표적): 적 탄은 통과</summary>
    public class PlayerOnlyTarget : MonoBehaviour { }

    /// <summary>블렌더 보스 조각 움직임: 마술사(Head · ArmL · ArmR) — 숨쉬기 · 머리 갸웃 · 손짓(Cast)</summary>
    public class PuppetAnim : MonoBehaviour
    {
        Transform head, armL, armR, body;
        Quaternion headR, armLR, armRR;
        float castUntil, castLen = 0.5f, phase;
        public float bob = 1f;

        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Head")) head = t;
                else if (t.name.StartsWith("ArmL")) armL = t;
                else if (t.name.StartsWith("ArmR")) armR = t;
                else if (t.name.StartsWith("Body")) body = t;
            }
            if (head) headR = head.localRotation;
            if (armL) armLR = armL.localRotation;
            if (armR) armRR = armR.localRotation;
            phase = Random.value * 10f;
        }

        public void Cast(float dur = 0.5f) { castLen = Mathf.Max(0.1f, dur); castUntil = Time.time + castLen; }

        void LateUpdate()
        {
            float t = Time.time + phase;
            float c = Time.time < castUntil ? Mathf.Sin((1f - (castUntil - Time.time) / castLen) * Mathf.PI) : 0f;
            // 조각 축은 FBX에 따라 다를 수 있어 로컬 X(앞뒤로 들기) · Z(좌우) 둘을 작게 섞음
            if (head) head.localRotation = headR * Quaternion.Euler(Mathf.Sin(t * 1.3f) * 4f, 0, Mathf.Sin(t * 0.9f) * 6f * bob);
            if (armL) armL.localRotation = armLR * Quaternion.Euler(-70f * c + Mathf.Sin(t * 1.7f) * 5f, 0, -25f * c);
            if (armR) armR.localRotation = armRR * Quaternion.Euler(-85f * c + Mathf.Sin(t * 1.5f + 1f) * 6f, 0, 20f * c);
            if (body) body.localPosition = new Vector3(body.localPosition.x, Mathf.Abs(Mathf.Sin(t * 2.2f)) * 0.04f * bob, body.localPosition.z);
        }
    }

    /// <summary>거울 고래 조각 움직임: 꼬리 흔들기 · 지느러미 · 입 벌리기 · 높이(잠수 · 비행) · 기울기</summary>
    public class WhaleAnim : MonoBehaviour
    {
        Transform tail, finL, finR, jaw;
        Quaternion tailR, finLR, finRR, jawR;
        public float height, heightTarget, jawOpen, jawTarget, swim = 1f, roll, rollTarget;
        float t;

        void Start()
        {
            foreach (var x in GetComponentsInChildren<Transform>(true))
            {
                if (x.name.StartsWith("Tail")) tail = x;
                else if (x.name.StartsWith("FinL")) finL = x;
                else if (x.name.StartsWith("FinR")) finR = x;
                else if (x.name.StartsWith("Jaw")) jaw = x;
            }
            if (tail) tailR = tail.localRotation;
            if (finL) finLR = finL.localRotation;
            if (finR) finRR = finR.localRotation;
            if (jaw) jawR = jaw.localRotation;
        }

        void LateUpdate()
        {
            t += Time.deltaTime * swim;
            height = Mathf.MoveTowards(height, heightTarget, 9f * Time.deltaTime);
            jawOpen = Mathf.MoveTowards(jawOpen, jawTarget, 3f * Time.deltaTime);
            roll = Mathf.MoveTowards(roll, rollTarget, 60f * Time.deltaTime);
            transform.localPosition = new Vector3(0, height + Mathf.Sin(t * 1.4f) * 0.25f, 0);
            transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.4f + 0.6f) * 3f, 0, roll);
            if (tail) tail.localRotation = tailR * Quaternion.Euler(Mathf.Sin(t * 2.6f) * 16f, 0, 0);
            if (finL) finL.localRotation = finLR * Quaternion.Euler(0, 0, Mathf.Sin(t * 2.2f) * 14f);
            if (finR) finR.localRotation = finRR * Quaternion.Euler(0, 0, -Mathf.Sin(t * 2.2f) * 14f);
            if (jaw) jaw.localRotation = jawR * Quaternion.Euler(jawOpen * 28f, 0, 0);
        }
    }

    /// <summary>퍼지는 링 (물결 링 · 충격파): 앞 선에 닿으면 한 번 피해, 대시 중이면 넘음</summary>
    public class RippleRing : MonoBehaviour
    {
        float r, speed, dmg, maxR, width;
        bool hit;
        LineRenderer lr;
        Color col;
        const int Seg = 64;

        public static void Spawn(Vector3 at, float speed, float dmg, float maxR, float width, Color c, float startR = 0.8f)
        {
            var go = new GameObject("RippleRing");
            go.transform.position = new Vector3(at.x, 0.08f, at.z);
            var s = go.AddComponent<RippleRing>();
            s.speed = speed; s.dmg = dmg; s.maxR = maxR; s.width = width; s.r = startR; s.col = c;
            s.lr = go.AddComponent<LineRenderer>();
            s.lr.sharedMaterial = Ch2Fx.LineMat();
            s.lr.useWorldSpace = false;
            s.lr.loop = true;
            s.lr.widthMultiplier = width;
            s.lr.positionCount = Seg;
            s.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            r += speed * Time.deltaTime;
            if (r > maxR) { Destroy(gameObject); return; }
            for (int i = 0; i < Seg; i++)
            {
                float a = i * Mathf.PI * 2 / Seg;
                lr.SetPosition(i, new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r));
            }
            var c = col; c.a = Mathf.Clamp01((maxR - r) / 3f) * 0.9f;
            lr.startColor = lr.endColor = c;
            if (hit || dmg <= 0) return;                                           // 피해 0 = 겉모습만 (물결 연출)
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive || p.health.dashing) return;
            float d = Effects.Flat(p.transform.position - transform.position).magnitude;
            if (Mathf.Abs(d - r) > width * 0.5f) return;                          // 그린 링 폭 그대로 (예전 +0.3m 여유 없앰)
            hit = true;
            p.health.TakeDamage(dmg);
        }
    }

    /// <summary>모자 속 비둘기: 유도 탄을 따라다니며 날갯짓하는 빛 비둘기 모델 (탄 그림은 숨김)</summary>
    public class DoveFollow : MonoBehaviour
    {
        public Transform target;
        Vector3 last;

        public static void Attach(EnemyProjectile p)
        {
            var dove = Ch3Fx.Model("Dove", null);
            if (!dove) return;
            foreach (var r in p.GetComponentsInChildren<Renderer>()) if (!(r is TrailRenderer)) r.enabled = false;
            dove.transform.position = p.transform.position;
            dove.transform.localScale *= 1.3f;
            var f = dove.AddComponent<DoveFollow>();
            f.target = p.transform;
            f.last = p.transform.position;
        }

        void LateUpdate()
        {
            if (!target) { Enemy.Fx3("Stardust", transform.position, 0.4f); Destroy(gameObject); return; }
            var d = Effects.Flat(target.position - last);
            last = target.position;
            transform.position = target.position;
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 18f) * 12f);
        }
    }

    /// <summary>거울 바닥 속에 비친 시계 (진짜 찾기): 숫자 고리(1~12, 아라비아 숫자)를 좌우 반전 · 거울빛 · 일렁임으로 바닥에 띄움.
    /// 가운데에서 번지며 떠오르고(0.5초) dur 뒤 가라앉아 사라짐. 12시 = 북, 반전이라 3시 = 서쪽</summary>
    public class ReflectedClock : MonoBehaviour
    {
        public const float NumeralRing = FloorClock.NumeralRing;
        static readonly int FadeId = Shader.PropertyToID("_Fade"), RiseId = Shader.PropertyToID("_Rise");
        Material mat;
        float born, dur, radius, sinkAt = -1f;

        /// <summary>반전된 판 위 hour시 숫자 방향 (단위 벡터)</summary>
        public static Vector3 HourDir(int hour)
        {
            float a = hour * 30f * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
        }

        public static ReflectedClock Show(Vector3 center, float radius, float dur)
        {
            var tex = Resources.Load<Texture2D>("FX/boss_floor_clock");
            var sh = Shader.Find("CatTang/MirrorClock");
            if (!tex || !sh) return null;
            var go = new GameObject("ReflectedClock");
            go.transform.position = new Vector3(center.x, center.y + 0.03f, center.z);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Dial";
            q.transform.SetParent(go.transform, false);
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);                 // 바닥에 눕힘 (그림 위쪽 = +Z = 12시)
            q.transform.localScale = new Vector3(-radius * 2f, radius * 2f, 1f);      // x 음수 = 좌우 반전 (거울 글자)
            var rc = go.AddComponent<ReflectedClock>();
            rc.mat = new Material(sh) { mainTexture = tex, renderQueue = 2980 };
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = rc.mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            rc.born = Time.time; rc.dur = dur; rc.radius = radius;
            rc.Apply(0f, 0f);
            // 떠오를 때 숫자 자리마다 거울 반짝임
            for (int h = 3; h <= 12; h += 3) Enemy.Fx3("MirrorFlash", center + HourDir(h) * radius * NumeralRing + Vector3.up * 0.3f, 0.6f);
            return rc;
        }

        /// <summary>지금부터 0.5초 동안 가라앉아 사라짐</summary>
        public void Sink() { if (sinkAt < 0) sinkAt = Time.time; }

        void Apply(float fade, float rise) { if (!mat) return; mat.SetFloat(FadeId, fade); mat.SetFloat(RiseId, rise); }

        void Update()
        {
            float age = Time.time - born;
            if (sinkAt < 0 && age >= dur) Sink();
            float k = Mathf.Clamp01(age / 0.5f);
            float rise = 1f - (1f - k) * (1f - k);
            float fade = rise;
            if (sinkAt >= 0)
            {
                float s = Mathf.Clamp01((Time.time - sinkAt) / 0.5f);
                fade *= 1f - s;
                if (s >= 1f) { Destroy(gameObject); return; }
            }
            Apply(fade, rise);
            // 물 위에 비친 것처럼 아주 살짝 숨 쉼
            float b = 1f + Mathf.Sin(Time.time * 1.6f) * 0.006f;
            transform.localScale = new Vector3(b, 1f, b);
        }

        void OnDestroy() { if (mat) Destroy(mat); }
    }

    // =====================================================================
    // 중간 보스: 신기루 마술사
    // =====================================================================
    public class Ch3MidBoss : BossBrain
    {
        public override float NearDist => 5f;
        public override float PatternGap => 1.0f;
        public override string DisplayName => "신기루 마술사";

        public const float FindFirst = 15f, FindEvery = 25f, FindSeal = 1f, FindWindow = 6f, FindStun = 3f;
        public const float CounterSpeed = 8f, CounterDmg = 18f;
        public const float ClockTime = 6f, ClockRadius = 2f, ClockFailDmg = 0.3f;

        float nextFind;
        readonly HashSet<int> clockDone = new();
        PuppetAnim puppet;

        protected override void Setup()
        {
            Add("카드 부채", Reach.Near, CardFan);
            Add("칼 꽂기 상자", Reach.Near, SwordBox);
            Add("모자 속 비둘기", Reach.Far, Doves);
            Add("거울 상자", Reach.Far, MirrorBox);
            Add("분신술사 호출", Reach.Any, Summon, () => Enemy.All.Count - 1 <= 2);
            nextFind = Time.time + FindFirst;
        }

        void Start() { puppet = GetComponentInChildren<PuppetAnim>(); }

        void Cast(float d = 0.5f) { if (puppet) puppet.Cast(d); }

        // 6~9m 거리 유지
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist < 6f) transform.position -= dir * spd * Time.deltaTime;
            else if (dist > 9f) transform.position += dir * spd * Time.deltaTime;
            else transform.position += Vector3.Cross(Vector3.up, dir) * spd * 0.35f * Time.deltaTime;   // 무대 위를 옆으로 거닐기
        }

        // 거울 시계(체력 구간 확정) > 진짜 찾기(쿨) — 패턴 사이에 끼어듦. 가끔 빛가루로 흩어져 다른 자리에 나타남
        protected override System.Func<IEnumerator> Forced()
        {
            foreach (int th in new[] { 66, 33 })
                if (!clockDone.Contains(th) && hp.hp <= hp.maxHp * th / 100f) { clockDone.Add(th); return MirrorClock; }
            if (Time.time >= nextFind) return FindTheReal;
            if (Random.value < 0.18f) return Blink;
            return null;
        }

        IEnumerator Blink()
        {
            var p = P;
            if (!p) yield break;
            Enemy.Fx3("Stardust", transform.position + Vector3.up * 2f, 1.4f);
            var to = p.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(6.5f, 8.5f);
            if (RunManager.Instance) to = RunManager.Instance.ClampToRoom(to, 2f);
            transform.position = new Vector3(to.x, transform.position.y, to.z);
            Enemy.Fx3("Stardust", transform.position + Vector3.up * 2f, 1.4f);
            yield return Wait(T(0.35f));
        }

        // ---------- 패턴 ----------
        // 카드 부채: 예고 0.6초, 전방 60° 부채꼴로 카드 7장, 8m/초, 피해 18
        IEnumerator CardFan()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 8f, 30f, T(0.6f), Telegraph.Danger);
            Cast(T(0.8f));
            float end = Time.time + T(0.6f);
            while (Time.time < end) { Face(d, 14f); yield return null; }
            for (int i = 0; i < 7; i++)
                Shoot(Quaternion.Euler(0, -30f + i * 10f, 0) * d, 8f, 18f, 1.6f, 14f, EnemyProjectile.Style.Ticket);
            Enemy.Fx3("MirrorFlash", transform.position + Vector3.up * 2.2f + d, 0.8f);
            yield return Wait(T(0.4f));
        }

        // 칼 꽂기 상자: 고양이 자리 원 0.8초 → 상자 낙하(18) → 0.5초 뒤 상하좌우 칼 6m(20), 대각선만 안전
        IEnumerator SwordBox()
        {
            var p = P;
            if (!p) yield break;
            var at = Effects.Flat(p.transform.position);
            Cast(T(0.6f));
            Telegraph.Ring(at, 1.2f, T(0.8f), Telegraph.Danger);
            yield return Wait(T(0.8f));
            var box = Ch3Fx.Model("SwordBox", null) ?? Ch3Fx.Prim(PrimitiveType.Cube, "SwordBox", null, Vector3.zero, Vector3.one * 1.5f, new Color(0.94f, 0.47f, 0.4f));
            box.transform.position = at + Vector3.up * 6f;
            for (float t = 0; t < 0.15f; t += Time.deltaTime) { box.transform.position = at + Vector3.up * Mathf.Lerp(6f, 0f, t / 0.15f); yield return null; }
            box.transform.position = at;
            CameraFollow.Shake(0.1f);
            FxBurst.Spawn(at + Vector3.up * 0.4f, 1.6f, FxBurst.Kind.Dust, new Color(1f, 0.85f, 0.85f));
            HitCircle(at, 1.2f, 18f);
            var dirs = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            foreach (var d in dirs) Telegraph.Lane(at, d, 6f, 0.9f, T(0.5f), Telegraph.Danger).NoGlint();
            yield return Wait(T(0.5f));
            var swords = new List<GameObject>();
            foreach (var d in dirs)
            {
                var s = Ch3Fx.Model("MagicSword", null) ?? Ch3Fx.Prim(PrimitiveType.Cube, "Sword", null, Vector3.zero, new Vector3(0.15f, 0.06f, 1.8f), new Color(1f, 0.96f, 0.86f));
                s.transform.rotation = Quaternion.LookRotation(d);
                swords.Add(s);
            }
            var hit = false;
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                for (int i = 0; i < 4; i++) swords[i].transform.position = at + Vector3.up * 0.7f + dirs[i] * Mathf.Lerp(0.3f, 4.6f, t / 0.15f);
                if (!hit) foreach (var d in dirs) if (Ch2Fx.PlayerInLane(at, d, 6f, 0.9f)) { hit = true; p.health.TakeDamage(20f); break; }
                yield return null;
            }
            yield return Wait(T(0.5f));
            foreach (var s in swords) if (s) Destroy(s);
            Enemy.Fx3("Stardust", at + Vector3.up, 1.2f);
            Destroy(box);
        }

        // 모자 속 비둘기: 4마리, 6m/초 · 초당 120° 유도 · 4초 뒤 소멸, 피해 18
        IEnumerator Doves()
        {
            Cast(T(0.7f));
            Telegraph.Ring(transform.position, 1.6f, T(0.5f), Telegraph.Aim, transform);
            yield return Wait(T(0.5f));
            var d = ToPlayer();
            for (int i = 0; i < 4; i++)
            {
                var dir = Quaternion.Euler(0, -60f + i * 40f, 0) * d;
                var b = EnemyProjectile.Spawn(transform.position + Vector3.up * 3.6f + dir, dir, 6f, 18f, 24f, EnemyProjectile.Style.Orb);
                b.homing = 120f;
                DoveFollow.Attach(b);
                Enemy.Fx3("Stardust", transform.position + Vector3.up * 3.8f, 0.5f);
                yield return Wait(T(0.12f));
            }
            yield return Wait(T(0.4f));
        }

        // 거울 상자: 고양이 둘레 6×6m 거울벽 4면 예고 0.8초 → 4초(한 면만 열림, 마술사 쪽), 탄 6발이 상자 안에서 튕김
        IEnumerator MirrorBox()
        {
            var p = P;
            if (!p) yield break;
            var c = Effects.Flat(p.transform.position);
            if (RunManager.Instance) c = RunManager.Instance.ClampToRoom(c, 3.6f);
            var toBoss = Effects.Flat(transform.position - c);
            int open = Mathf.Abs(toBoss.x) > Mathf.Abs(toBoss.z) ? (toBoss.x > 0 ? 2 : 3) : (toBoss.z > 0 ? 0 : 1);   // 0 북 1 남 2 동 3 서
            var sides = new[] { (Vector3.forward, 0f), (Vector3.back, 180f), (Vector3.right, 90f), (Vector3.left, 270f) };
            for (int i = 0; i < 4; i++)
            {
                if (i == open) continue;
                var (n, _) = sides[i];
                var side = Vector3.Cross(Vector3.up, n);
                Telegraph.Lane(c + n * 3f - side * 3f, side, 6f, 0.5f, T(0.8f), Telegraph.Aim).NoGlint();
            }
            Cast(T(1f));
            BossFx.Toast("거울 상자 — 열린 쪽으로!");
            yield return Wait(T(0.8f));
            var walls = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                if (i == open) continue;
                var (n, yaw) = sides[i];
                var w = Ch3Fx.Model("MirrorPanel", null) ?? Ch3Fx.Prim(PrimitiveType.Cube, "MirrorPanel", null, Vector3.zero, new Vector3(6f, 2.6f, 0.2f), Ch3Fx.Light);
                w.transform.position = c + n * 3f;
                w.transform.rotation = Quaternion.Euler(0, yaw, 0);
                var bc = w.AddComponent<BoxCollider>();
                bc.center = new Vector3(0, 1.3f, 0) / Mathf.Max(0.01f, w.transform.lossyScale.y);
                bc.size = new Vector3(6f, 2.6f, 0.4f) / Mathf.Max(0.01f, w.transform.lossyScale.x);
                w.AddComponent<RiseUp>().depth = 2.6f;
                walls.Add(w);
            }
            yield return Wait(0.5f);
            // 탄 6발: 열린 면으로 상자 안에 쏘아 벽에 튕김 (적 탄 bounces)
            for (int k = 0; k < 6; k++)
            {
                var aim = Effects.Flat(c + Random.insideUnitSphere * 2f - transform.position).normalized;
                var b = EnemyProjectile.Spawn(transform.position + Vector3.up * 1.4f + aim * 1.2f, aim, 8f, 18f, 30f, EnemyProjectile.Style.Needle);
                b.bounces = 3;
                Cast(0.25f);
                yield return Wait(T(0.35f));
            }
            yield return Wait(Mathf.Max(0, 4f - 0.5f - 6 * T(0.35f)));
            foreach (var w in walls) if (w) { Enemy.Fx3("GlassBurst", w.transform.position + Vector3.up * 1.3f, 0.9f); Destroy(w); }
        }

        IEnumerator Summon()
        {
            Cast(T(0.6f));
            yield return Wait(T(0.5f));
            int room = 4 - (Enemy.All.Count - 1);
            for (int i = 0; i < Mathf.Min(2, room); i++)
            {
                var pos = transform.position + transform.right * (i == 0 ? -3f : 3f);
                if (RunManager.Instance) pos = RunManager.Instance.ClampToRoom(pos, 1.5f);
                Telegraph.Ring(pos, 0.7f, 0.6f, Telegraph.Aim);
                Enemy.Fx3("Stardust", pos + Vector3.up, 1f);
                var e = EnemyFactory.Trickster(pos, self.chapter ?? Tables.Chapter(3));
                RunManager.Instance?.AddEnemy(e);
            }
            yield return Wait(T(0.4f));
        }

        // ---------- 기믹 1: 진짜 찾기 (2026-11-18 사용자: 숫자 카드 X → 바닥 시계 자리) ----------
        // 1초 사격 봉인 동안 방 가운데 거울 바닥 속에 큰 숫자 시계가 비쳐 떠오르고(ReflectedClock, 반경 7m, 12시 = 북, **좌우 반전** — 3시가 서쪽 · 9시가 동쪽,
        // 2026-11-18 사용자), 마술사 + 분신 4가 시계의 시각 자리 5곳(12칸 중)으로 흩어져 그 숫자 바로 바깥에 둥글게 섬 (숫자를 가리지 않아 시계 전체가 화면에 없어도 읽힘).
        // 진짜 = 반전된 판에서 발동 순간 내 발사 시각 숫자 자리에 선 것. 6초 안에 진짜를 맞히면 3초 기절 + 분신 소멸 / 가짜를 맞히면 그 분신이 터지며 3갈래 반격 /
        // 시간 초과면 남은 분신 전부 3갈래 후 소멸.
        public const float FindClockR = 7f, FindStandOut = 1.3f;
        IEnumerator FindTheReal()
        {
            var p = P;
            if (!p) { nextFind = Time.time + 5f; yield break; }
            int real = Mathf.Clamp(p.rev.next, 1, 12);
            p.fireLockUntil = Time.time + FindSeal;
            BossFx.Toast("진짜 찾기 — 거울에 비친 시계에서 지금 발사 시각 자리에 선 마술사를 쏴라!");
            Cast(FindSeal);
            // 시각 5개: 진짜 + 다른 시각 4 (섞음)
            var hours = new List<int>();
            for (int i = 1; i <= 12; i++) if (i != real) hours.Add(i);
            for (int i = hours.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (hours[i], hours[j]) = (hours[j], hours[i]); }
            hours.RemoveRange(4, hours.Count - 4);
            hours.Insert(0, real);                                               // 0번 = 마술사 본인
            var c = Ch3Fx.ArenaC();
            var dial = ReflectedClock.Show(c, FindClockR, FindSeal + FindWindow + 0.6f);
            if (dial) Own(dial.gameObject);
            RippleRing.Spawn(c, 18f, 0f, FindClockR + FindStandOut, 0.3f, Ch3Fx.Gold);
            Vector3 HourAt(int h) => c + ReflectedClock.HourDir(h) * (FindClockR + FindStandOut);
            // 분신 4 (진짜와 같은 모습, 쏘면 맞는 과녁)
            var clones = new List<(GameObject go, Health h)>();
            var start = transform.position;
            for (int i = 0; i < 4; i++)
            {
                var go = Own(new GameObject("MagicianClone"));
                go.transform.position = start;
                var m = Ch3Fx.Model("Ch3_Magician", go.transform);
                if (m) m.AddComponent<PuppetAnim>();
                else Ch3Fx.Prim(PrimitiveType.Capsule, "Body", go.transform, Vector3.up * 1.9f, new Vector3(1.2f, 1.9f, 1.2f), new Color(0.6f, 0.56f, 0.82f));
                clones.Add((go, Ch3Fx.Target(go, 1.1f, 4f)));
                Enemy.Fx3("Stardust", start + Vector3.up * 2f, 1f);
            }
            Transform Who(int i) => i == 0 ? transform : clones[i - 1].go.transform;
            // 흩어지기 1초: 시계 둘레를 돌며 각자 시각 자리로 미끄러져 감
            var from = new Vector3[5];
            for (int i = 0; i < 5; i++) from[i] = Who(i).position;
            for (float t = 0; t < FindSeal; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / FindSeal);
                for (int i = 0; i < 5; i++)
                {
                    var to = HourAt(hours[i]);
                    var pos = Vector3.Lerp(from[i], to, k) + Quaternion.Euler(0, 90, 0) * Effects.Flat(to - c).normalized * Mathf.Sin(k * Mathf.PI) * 3f;
                    var w = Who(i);
                    w.position = new Vector3(pos.x, w.position.y, pos.z);
                }
                yield return null;
            }
            for (int i = 0; i < 5; i++)
            {
                var to = HourAt(hours[i]); var w = Who(i);
                w.position = new Vector3(to.x, w.position.y, to.z);
                Enemy.Fx3("Stardust", to + Vector3.up * 2f, 0.9f);
            }
            // 6초 판정
            bool realHit = false;
            System.Action<float> onReal = _ => realHit = true;
            hp.OnDamaged += onReal;
            var alive = new List<(GameObject go, Health h)>(clones);
            var hitClone = new HashSet<Health>();
            foreach (var cl in clones) { var hh = cl.h; hh.OnDamaged += _ => hitClone.Add(hh); }
            float end = Time.time + FindWindow;
            BigLabel timer = Label(p.transform, new Vector3(0, 3.4f, 0), "", Ch3Fx.Pink, 1f);
            while (Time.time < end && hp.Alive && !realHit)
            {
                if (timer) timer.text = Mathf.CeilToInt(end - Time.time).ToString();
                var pp = Effects.Flat(p.transform.position);
                transform.rotation = Quaternion.LookRotation(Effects.Flat(pp - transform.position).normalized + Vector3.forward * 1e-4f);
                for (int i = alive.Count - 1; i >= 0; i--)
                {
                    var go = alive[i].go;
                    go.transform.rotation = Quaternion.LookRotation(Effects.Flat(pp - go.transform.position).normalized + Vector3.forward * 1e-4f);
                    if (!hitClone.Contains(alive[i].h)) continue;
                    Enemy.Fx3("Stardust", go.transform.position + Vector3.up * 2f, 1.3f);
                    CounterShot(go.transform.position);
                    Destroy(go);
                    alive.RemoveAt(i);
                }
                yield return null;
            }
            hp.OnDamaged -= onReal;
            if (timer) Destroy(timer.gameObject);
            if (realHit)
            {
                BossFx.Toast($"{real}시 자리 — 진짜를 찾았다, 마술사 기절!");
                hp.ApplyStun(FindStun);
                foreach (var cl in alive) if (cl.go) { Enemy.Fx3("Stardust", cl.go.transform.position + Vector3.up * 2f, 1.2f); Destroy(cl.go); }
            }
            else
            {
                BossFx.Toast($"시간 초과 — 진짜는 {real}시 자리, 분신들의 반격!");
                foreach (var cl in alive) if (cl.go) { CounterShot(cl.go.transform.position); Enemy.Fx3("Stardust", cl.go.transform.position + Vector3.up * 2f, 1.2f); Destroy(cl.go); }
            }
            Enemy.Fx3("MirrorFlash", c + Vector3.up * 0.4f, 1.2f);
            if (dial) dial.Sink();
            nextFind = Time.time + T(FindEvery);
            yield return Wait(realHit ? FindStun : T(0.6f));
        }

        /// <summary>분신 반격: 고양이 쪽 3갈래 (30°, 8m/초, 피해 18)</summary>
        void CounterShot(Vector3 from)
        {
            var p = P;
            if (!p) return;
            var d = Effects.Flat(p.transform.position - from).normalized;
            for (int k = -1; k <= 1; k++)
                EnemyProjectile.Spawn(from + Vector3.up * 1.6f + d * 1f, Quaternion.Euler(0, k * 15f, 0) * d, CounterSpeed, CounterDmg, 14f, EnemyProjectile.Style.Ticket);
        }

        // ---------- 기믹 2: 거울 시계 ----------
        // 바닥이 거울로 덮이고 시계 1~6개(반경 2m, 서로 1m 이상, 고양이에서 3~10m). 홀수 = 시계 위에 서기 · 짝수 = 모든 시계 밖, 6초 뒤 한 번 판정. 실패 = 최대 체력 30%
        IEnumerator MirrorClock()
        {
            var p = P;
            if (!p) yield break;
            hp.invulnerable = true;
            float pausedFrom = Time.time;
            Cast(1f);
            BossFx.Toast("거울 시계 — 시계가 홀수면 시계 위에, 짝수면 모든 시계 밖에 서라!");
            // 1초 연출: 바닥 전체에 거울 물결 + 반짝임
            var c = Ch3Fx.ArenaC();
            RippleRing.Spawn(c, 22f, 0f, 22f, 0.35f, Ch3Fx.Light);
            for (int i = 0; i < 10; i++) Enemy.Fx3("MirrorFlash", c + new Vector3(Random.Range(-12f, 12f), 0.3f, Random.Range(-10f, 10f)), 0.8f);
            yield return Wait(1f);
            int n = Random.Range(1, 7);
            var spots = new List<Vector3>();
            for (int tries = 0; tries < 300 && spots.Count < n; tries++)
            {
                var s = Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(3f, 10f);
                if (RunManager.Instance) s = RunManager.Instance.ClampToRoom(s, ClockRadius + 0.3f);
                if (Effects.Flat(s - p.transform.position).magnitude < 3f) continue;
                bool ok = true;
                foreach (var o in spots) if (Effects.Flat(o - s).magnitude < ClockRadius * 2 + 1f) { ok = false; break; }
                if (ok) spots.Add(s);
            }
            n = spots.Count;
            var clocks = new List<GameObject>();
            var hands = new List<Transform>();
            foreach (var s in spots)
            {
                hands.Add(Ch3Fx.FloorClock(s, ClockRadius, out var root));
                clocks.Add(root);
                Enemy.Fx3("MirrorFlash", s + Vector3.up * 0.5f, 0.7f);
            }
            float end = Time.time + ClockTime;
            while (Time.time < end && hp.Alive)
            {
                float k = 1f - (end - Time.time) / ClockTime;
                foreach (var h in hands) if (h) h.localRotation = Quaternion.Euler(0, k * 360f, 0);
                yield return null;
            }
            bool onClock = false;
            foreach (var s in spots) if (Effects.Flat(p.transform.position - s).magnitude <= ClockRadius) onClock = true;
            bool pass = (n % 2 == 1) ? onClock : !onClock;
            if (pass) { BossFx.Toast($"시계 {n}개 — 통과!"); foreach (var s in spots) Enemy.Fx3("Stardust", s + Vector3.up * 0.5f, 1f); }
            else
            {
                BossFx.Toast($"시계 {n}개 — 실패!");
                p.health.TakeDamage(p.health.maxHp * ClockFailDmg);
                CameraFollow.Shake(0.2f);
                foreach (var s in spots) Enemy.Fx3("GlassBurst", s + Vector3.up * 0.5f, 1.2f);
            }
            foreach (var g in clocks) if (g) Destroy(g);
            hp.invulnerable = false;
            nextFind += Time.time - pausedFrom;            // 진행 중엔 진짜 찾기 쿨이 멈춤
            yield return Wait(T(0.5f));
        }

    }

    // =====================================================================
    // 보스: 거울 고래
    // =====================================================================
    public class Ch3Boss : BossBrain
    {
        public override float NearDist => 7f;
        public override float PatternGap => 1.2f;
        public override string DisplayName => "거울 고래";

        public const float ArenaR = 20f;
        const float BeamLap = 20f, BeamWidth = 0.6f, BeamDmg = 15f, BeamTick = 0.5f;
        const float RippleEvery = 6f, RippleSpeed = 5f, RippleWidth = 0.5f, RippleDmg = 15f;
        const float RainRate = 2f, RainRadius = 1f, RainDmg = 15f;
        const float GimmickFirst = 20f, GimmickEvery = 20f, GimmickStun = 3f, GimmickFail = 0.4f;

        bool phase2;
        float nextRipple, nextRain, nextGimmick, beamAngle, beamHitAt = -9f;
        bool inGimmick;
        string lastGimmick;
        WhaleAnim anim;
        LineRenderer beam;

        // 발사 시각 판정 (OnFired 같은 프레임 명중)
        int needHour;
        Health needTarget;                    // 이 과녁(또는 고래 머리)에 맞혀야 함
        bool needHead, gimmickHit;
        readonly Dictionary<Health, int> shardNum = new();
        readonly HashSet<Health> shardBroken = new();
        readonly List<BigLabel> shardLabels = new();

        protected override void Setup()
        {
            Add("꼬리 내려치기", Reach.Near, TailSlam, () => !phase2);
            Add("소용돌이", Reach.Near, Vortex, () => !phase2);
            Add("잠수 돌진", Reach.Far, DiveCharge, () => !phase2);
            Add("거울 파편 뿌리기", Reach.Far, ScatterShards, () => !phase2);
            Add("지느러미 휩쓸기", Reach.Near, FinSweep, () => phase2);
            Add("빛 브레스", Reach.Near, LightBreath, () => phase2);
            Add("급강하", Reach.Far, Plunge, () => phase2);
            Add("하늘 가르기", Reach.Far, SkySlice, () => phase2);
            nextGimmick = Time.time + GimmickFirst;
            nextRipple = Time.time + 3f;
        }

        void Start()
        {
            anim = GetComponentInChildren<WhaleAnim>();
            if (P) P.OnFired += OnPlayerFired;
            var go = new GameObject("ReflectBeam");
            beam = go.AddComponent<LineRenderer>();
            beam.sharedMaterial = Ch2Fx.LineMat();
            beam.widthMultiplier = BeamWidth;
            beam.numCapVertices = 4;
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.startColor = beam.endColor = new Color(1f, 0.78f, 0.95f, 0.9f);
            beamAngle = Random.Range(0f, 360f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (P) P.OnFired -= OnPlayerFired;
            if (beam) Destroy(beam.gameObject);
            ClearShards();
            var p = P;
            if (p) for (int i = 1; i <= 12; i++) p.rev.lost[i] = false;
        }

        float Spd => phase2 ? 3.6f : 3f;
        void Height(float h) { if (anim) anim.heightTarget = h; }
        float FlyH => phase2 ? 3.2f : 0f;

        // 고양이 둘레를 크게 돌며 헤엄 (반경 10m 안팎)
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            var side = Vector3.Cross(Vector3.up, dir);
            var v = side * Spd;
            if (dist < 9f) v -= dir * Spd * 0.6f; else if (dist > 12f) v += dir * Spd * 0.8f;
            transform.position += v * Time.deltaTime;
            KeepInArena();
        }

        void KeepInArena()
        {
            var c = Ch3Fx.ArenaC();
            var rel = Effects.Flat(transform.position - c);
            if (rel.magnitude > ArenaR - 3f) transform.position = c + rel.normalized * (ArenaR - 3f) + Vector3.up * transform.position.y;
        }

        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            UpdateBeam(p);
            if (!phase2 && hp.hp <= hp.maxHp * 0.5f) EnterPhase2();
            if (!phase2 && Time.time >= nextRipple) { Ripple(transform.position); }
            if (phase2 && p && Time.time >= nextRain)
            {
                nextRain = Time.time + 1f / RainRate;
                var at = Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(0f, 8f);
                StartCoroutine(RainDrop(at));
            }
        }

        void Ripple(Vector3 at)
        {
            RippleRing.Spawn(at, RippleSpeed, RippleDmg, ArenaR * 1.6f, RippleWidth, Ch3Fx.Teal);
            nextRipple = Time.time + T(RippleEvery);
        }

        IEnumerator RainDrop(Vector3 at)
        {
            Telegraph.Ring(at, RainRadius, 0.8f, Telegraph.Aim).NoGlint();
            yield return new WaitForSeconds(0.8f);
            HitCircle(at, RainRadius, RainDmg);
            Enemy.Fx3("Stardust", at + Vector3.up * 0.3f, 0.45f);
        }

        // ---------- 상시: 반사 빛줄기 ----------
        // 둘레 거울 조각 8개 사이를 튕기는 빛줄기, 시작 자리가 20초에 한 바퀴 돎. 거울 조각을 쏘면(15°) 경로가 바뀜
        void UpdateBeam(PlayerController p)
        {
            if (!beam) return;
            beamAngle += 360f / BeamLap * Time.deltaTime;
            var c = Ch3Fx.ArenaC();
            var dirOut = Quaternion.Euler(0, beamAngle, 0) * Vector3.forward;
            var from = c + dirOut * (ArenaR - 0.5f) + Vector3.up * 1.0f;
            var dir = Quaternion.Euler(0, 28f, 0) * -dirOut;
            var pts = new List<Vector3> { from };
            MirrorShard last = null;
            for (int bounce = 0; bounce < 6; bounce++)
            {
                float exit = RayExitCircle(from, dir, c, ArenaR);
                var m = MirrorShard.Raycast(from, dir, exit, last, out float t, out var hit);
                if (!m) { pts.Add(from + dir * exit); break; }
                hit.y = from.y;
                pts.Add(hit);
                from = hit; dir = m.Reflect(dir); last = m;
            }
            beam.positionCount = pts.Count;
            beam.SetPositions(pts.ToArray());
            if (!p || !p.health.Alive || Time.time - beamHitAt < BeamTick) return;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var seg = Effects.Flat(b - a);
                if (Ch2Kit.InLane(p.transform.position, Effects.Flat(a), seg.normalized, seg.magnitude, BeamWidth + 0.5f))
                {
                    beamHitAt = Time.time;
                    p.health.TakeDamage(BeamDmg);
                    break;
                }
            }
        }

        static float RayExitCircle(Vector3 from, Vector3 dir, Vector3 c, float r)
        {
            var o = Effects.Flat(from - c);
            var d = Effects.Flat(dir).normalized;
            float b = Vector3.Dot(o, d), cc = o.sqrMagnitude - r * r;
            float disc = b * b - cc;
            if (disc < 0) return 2f * r;
            return Mathf.Max(0.5f, -b + Mathf.Sqrt(disc));
        }

        // ---------- 2페이즈 ----------
        void EnterPhase2()
        {
            phase2 = true;
            BossFx.Toast("거울이 깨졌다 — 고래가 하늘을 난다!");
            CameraFollow.Shake(0.3f);
            var c = Ch3Fx.ArenaC();
            for (int i = 0; i < 18; i++) Enemy.Fx3("GlassBurst", c + Quaternion.Euler(0, i * 20f, 0) * Vector3.forward * Random.Range(3f, 17f) + Vector3.up * 0.3f, 1.2f);
            // 바닥: 거울 대신 그 아래 하늘(구름) — 방 바닥 판에 재질 인스턴스
            var rm = RunManager.Instance;
            var room = rm ? rm.CurrentRoom : null;
            var ground = room != null && room.root ? room.root.Find("Ground") : null;
            var sky = Resources.Load<Material>("Kit/Ch3/GroundCloud");
            if (ground && sky) ground.GetComponent<Renderer>().sharedMaterial = new Material(sky);
            MirrorReflection.Ensure(false);
            Height(FlyH);
        }

        // ---------- 기믹 스케줄: 쿨 20초, 기믹 우선, 기믹 중 쿨 정지 ----------
        protected override System.Func<IEnumerator> Forced()
        {
            if (inGimmick || Time.time < nextGimmick) return null;
            var all = new List<(string id, System.Func<IEnumerator> run)> { ("mirror", MirroredTime), ("song", WhaleSong), ("shards", ScatteredMemories), ("swallow", SwallowMemory) };
            all.RemoveAll(g => g.id == lastGimmick);
            var pick = all[Random.Range(0, all.Count)];
            lastGimmick = pick.id;
            return () => Gimmick(pick.run());
        }

        IEnumerator Gimmick(IEnumerator inner)
        {
            inGimmick = true;
            yield return inner;
            inGimmick = false;
            nextGimmick = Time.time + GimmickEvery;
        }

        void OnPlayerFired(int slot, bool awake)
        {
            if (slot != needHour && shardNum.Count == 0) return;
            // 흩어진 기억 조각: 자기 숫자 탄에만 깨짐
            foreach (var kv in shardNum)
            {
                var h = kv.Key;
                if (!h || shardBroken.Contains(h) || Time.time - h.lastHitAt > 0.001f) continue;
                if (slot == kv.Value) { shardBroken.Add(h); Enemy.Fx3("GlassBurst", h.transform.position + Vector3.up * 1.2f, 1f); h.gameObject.SetActive(false); HideShardLabel(h); }
                else FxBurst.Spawn(h.transform.position + Vector3.up * 1.2f, 0.8f, FxBurst.Kind.Spark, Ch3Fx.Light);   // 튕김
            }
            if (gimmickHit || slot != needHour) return;
            if (needTarget && Time.time - needTarget.lastHitAt <= 0.001f) gimmickHit = true;
            if (needHead && Time.time - hp.lastHitAt <= 0.001f && Vector3.Angle(-hp.lastHitDir, transform.forward) <= 60f) gimmickHit = true;
        }

        void HideShardLabel(Health h)
        {
            foreach (var lb in shardLabels) if (lb && lb.target == h.transform) lb.gameObject.SetActive(false);
        }

        void ClearShards()
        {
            foreach (var kv in shardNum) if (kv.Key) Destroy(kv.Key.gameObject);
            foreach (var lb in shardLabels) if (lb) Destroy(lb.gameObject);
            shardNum.Clear(); shardBroken.Clear(); shardLabels.Clear();
        }

        void Stun(float dur)
        {
            hp.ApplyStun(dur);
            FxBurst.Spawn(transform.position + Vector3.up * 3f, 2.5f, FxBurst.Kind.Gold, Ch3Fx.Gold);
        }

        // ① 거울에 비친 시각: 잠수(2페이즈는 하늘 위로) → 바닥에 좌우 반전된 시계(보이는 n시 = 실제 12 - n시). 8초 안에 실제 시각 탄을 예고 원 가운데에 맞히면 저지
        IEnumerator MirroredTime()
        {
            var p = P;
            if (!p) yield break;
            int real;
            do real = Random.Range(1, 13); while (real == p.rev.next);
            int shown = real == 12 || real == 6 ? real : 12 - real;
            hp.invulnerable = true;
            Height(phase2 ? 14f : -7f);
            Ripple(transform.position);
            if (anim) anim.swim = 2f;
            var c = Ch3Fx.ArenaC();
            var at = Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(6f, 9f);
            var rel = Effects.Flat(at - c); if (rel.magnitude > ArenaR - 4f) at = c + rel.normalized * (ArenaR - 4f);
            // 바닥 시계: 숫자 없이 12시 표시만, 바늘은 '비친' 시각을 가리킴
            var hand = Ch3Fx.FloorClock(c, 3.2f, out var clock);
            clock.transform.localScale = new Vector3(-1f, 1f, 1f);                    // 좌우 반전 (12시 표시는 그대로 위)
            hand.localRotation = Quaternion.Euler(0, -shown * 30f, 0);
            BossFx.Toast("거울에 비친 시각 — 바닥 시계는 좌우가 뒤집혀 있다. 실제 시각 탄을 원 가운데에!");
            var marker = new GameObject("MirrorTarget");
            marker.transform.position = at;
            needTarget = Ch3Fx.Target(marker, 0.9f, 2.2f);
            Ch3Fx.Prim(PrimitiveType.Cylinder, "Core", marker.transform, Vector3.up * 0.05f, new Vector3(1.6f, 0.03f, 1.6f), Ch3Fx.Gold, 1f);
            needHour = real; needHead = false; gimmickHit = false;
            Telegraph.Ring(at, 3f, 8f, Telegraph.Danger).NoGlint();
            var timer = Label(marker.transform, new Vector3(0, 3f, 0), "8", Ch3Fx.Pink, 1.2f);
            float end = Time.time + 8f;
            while (Time.time < end && !gimmickHit && hp.Alive) { if (timer) timer.text = Mathf.CeilToInt(end - Time.time).ToString(); yield return null; }
            needHour = 0; needTarget = null;
            Destroy(clock);
            transform.position = new Vector3(at.x, transform.position.y, at.z);
            Height(FlyH);
            if (anim) anim.swim = 1f;
            hp.invulnerable = false;
            Destroy(marker);
            if (gimmickHit) { BossFx.Toast("저지 — 고래가 걸렸다!"); Stun(GimmickStun); Ripple(at); yield return Wait(GimmickStun); }
            else
            {
                BossFx.Toast("시간 초과 — 파도!");
                CameraFollow.Shake(0.3f);
                RippleRing.Spawn(at, 18f, 0f, 9f, 0.6f, Ch3Fx.Teal);
                if (Effects.Flat(p.transform.position - at).magnitude <= 9f) p.health.TakeDamage(p.health.maxHp * GimmickFail);
                Ripple(at);
                yield return Wait(T(0.8f));
            }
        }

        // ② 고래의 노래: 1~12번 울음(0.5초 간격, 숫자 표시 없음, 울 때마다 둘레 물결) → 머리가 빛나는 4초 안에 울음 수 시각 탄을 머리(정면 ±60°)에
        IEnumerator WhaleSong()
        {
            Sfx.Play("e3b_song", 0.9f, 0f, 1);
            var p = P;
            if (!p) yield break;
            int n = Random.Range(1, 13);
            BossFx.Toast("고래의 노래 — 울음 수를 세라");
            if (anim) anim.jawTarget = 0.6f;
            var c = Ch3Fx.ArenaC();
            for (int i = 0; i < n; i++)
            {
                if (p) Face(ToPlayer(), 20f);
                RippleRing.Spawn(c, 0f, 0f, 1f, 0.25f, Ch3Fx.Pink, ArenaR - 0.6f);   // 둘레에 물결 한 줄 (피해 없음, 금방 사라짐)
                FxBurst.Spawn(transform.position + transform.forward * 5f + Vector3.up * 2.5f, 1.4f, FxBurst.Kind.Spark, Ch3Fx.Pink);
                CameraFollow.Shake(0.04f);
                yield return Wait(0.5f);
            }
            if (anim) anim.jawTarget = 0f;
            var glow = Ch3Fx.Prim(PrimitiveType.Sphere, "HeadGlow", transform, transform.InverseTransformPoint(transform.position + transform.forward * 5f + Vector3.up * 2.2f), Vector3.one * 2.2f, Ch3Fx.Gold, 0f);
            needHour = n; needHead = true; needTarget = null; gimmickHit = false;
            BossFx.Toast("머리가 빛난다 — 울음 수 시각 탄을 머리에!");
            float end = Time.time + 4f;
            while (Time.time < end && !gimmickHit && hp.Alive)
            {
                if (p) Face(ToPlayer(), 3f);
                glow.transform.localScale = Vector3.one * (2.2f + Mathf.Sin(Time.time * 10f) * 0.25f) / Mathf.Max(0.01f, transform.lossyScale.x);
                yield return null;
            }
            needHour = 0; needHead = false;
            Destroy(glow);
            if (gimmickHit) { BossFx.Toast($"{n}번 — 맞췄다! 고래 기절"); Stun(GimmickStun); yield return Wait(GimmickStun); }
            else
            {
                BossFx.Toast($"{n}번이었다 — 노래 충격파!");
                RippleRing.Spawn(transform.position, 30f, 0f, ArenaR * 1.5f, 0.8f, Ch3Fx.Pink);
                CameraFollow.Shake(0.3f);
                if (p) p.health.TakeDamage(p.health.maxHp * GimmickFail);
                yield return Wait(T(0.8f));
            }
        }

        // ③ 흩어진 기억 조각: 숫자 조각 6개(서로 다른 1~12, 고양이에서 4~14m), 자기 숫자 시각 탄에만 깨짐.
        // 10초 뒤 고래가 남은 조각을 삼키며 가로질러 돌진(예고 선 1초): 조각당 최대 체력 7%, 다 깨면 고래 3초 기절
        IEnumerator ScatteredMemories()
        {
            var p = P;
            if (!p) yield break;
            BossFx.Toast("흩어진 기억 조각 — 조각 숫자와 같은 시각 탄으로만 깨진다!");
            var nums = new List<int>();
            for (int i = 1; i <= 12; i++) nums.Add(i);
            for (int i = nums.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (nums[i], nums[j]) = (nums[j], nums[i]); }
            var c = Ch3Fx.ArenaC();
            shardNum.Clear(); shardBroken.Clear();
            var placed = new List<Vector3>();
            for (int k = 0, tries = 0; k < 6 && tries < 200; tries++)
            {
                var at = Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(4f, 14f);
                if (Effects.Flat(at - c).magnitude > ArenaR - 2f) continue;
                bool ok = true; foreach (var o in placed) if ((o - at).magnitude < 3f) ok = false;
                if (!ok) continue;
                placed.Add(at);
                var go = new GameObject("MemoryShard_" + nums[k]);
                go.transform.position = at;
                if (!Ch3Fx.Model("MemoryShard", go.transform)) Ch3Fx.Prim(PrimitiveType.Capsule, "Shard", go.transform, Vector3.up * 1.2f, new Vector3(0.8f, 1f, 0.8f), new Color(1f, 0.78f, 0.86f));
                var h = Ch3Fx.Target(go, 0.6f, 2.4f);
                shardNum[h] = nums[k];
                shardLabels.Add(Label(go.transform, new Vector3(0, 3.0f, 0), nums[k].ToString(), Ch3Fx.Gold, 1.1f));
                Enemy.Fx3("Stardust", at + Vector3.up, 0.8f);
                k++;
            }
            float end = Time.time + 10f;
            while (Time.time < end && hp.Alive && shardBroken.Count < shardNum.Count) yield return null;
            int left = shardNum.Count - shardBroken.Count;
            if (left == 0)
            {
                BossFx.Toast("기억 조각을 다 깼다 — 고래 기절!");
                ClearShards();
                Stun(GimmickStun);
                yield return Wait(GimmickStun);
                yield break;
            }
            // 남은 조각을 삼키며 돌진: 고래 → 고양이 쪽 가로지르기
            var dir = Effects.Flat(p.transform.position - transform.position).normalized;
            float len = ArenaR * 1.6f;
            Telegraph.Lane(transform.position, dir, len, 5f, 1f, Telegraph.Danger);
            BossFx.Toast($"남은 조각 {left}개 — 고래가 삼키며 돌진!");
            yield return Wait(1f);
            for (float moved = 0; moved < len; )
            {
                float step = 26f * Time.deltaTime;
                transform.position += dir * step; moved += step;
                foreach (var kv in shardNum) if (kv.Key && kv.Key.gameObject.activeSelf && Effects.Flat(kv.Key.transform.position - transform.position).magnitude < 3f) { Enemy.Fx3("Stardust", kv.Key.transform.position + Vector3.up, 1f); kv.Key.gameObject.SetActive(false); HideShardLabel(kv.Key); }
                KeepInArena();
                yield return null;
            }
            p.health.TakeDamage(p.health.maxHp * 0.07f * left);
            CameraFollow.Shake(0.25f);
            ClearShards();
            yield return Wait(T(0.6f));
        }

        // ④ 기억 삼키기: 예고 1초 → 2초 동안 입 앞 60° · 12m에서 빨아들임(2m/초, 대시로 빠짐) → 입 앞 3m 안이면 삼켜짐: 피해 25 + 실린더 3칸 사라짐.
        // 되찾기: 옆구리 빛나는 부위 3곳(각 2발) → 1칸씩, 12초 뒤 자동 복귀
        IEnumerator SwallowMemory()
        {
            var p = P;
            if (!p) yield break;
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 12f, 30f, 1f, Telegraph.Aim);
            BossFx.Toast("기억 삼키기 — 대시로 빠져나와라!");
            float tEnd = Time.time + 1f;
            while (Time.time < tEnd) { Face(d, 14f); yield return null; }
            if (anim) anim.jawTarget = 1f;
            var mouth = transform.position + transform.forward * 6.5f;
            float end = Time.time + 2f;
            while (Time.time < end && hp.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= 12f && Vector3.Angle(to, transform.forward) <= 30f) Ch3Fx.PullPlayer(mouth, 2f);
                if (Random.value < 0.4f) Enemy.Fx3("Stardust", transform.position + transform.forward * Random.Range(7f, 14f) + Vector3.up * 1f, 0.4f);
                yield return null;
            }
            if (anim) anim.jawTarget = 0f;
            if (Effects.Flat(p.transform.position - mouth).magnitude > 3f) { yield return Wait(T(0.5f)); yield break; }
            p.health.TakeDamage(25f);
            var pool = new List<int>();
            for (int i = 1; i <= 12; i++) if (!p.rev.lost[i]) pool.Add(i);
            var lostNow = new List<int>();
            for (int k = 0; k < 3 && pool.Count > 0; k++) { int i = Random.Range(0, pool.Count); p.rev.lost[pool[i]] = true; lostNow.Add(pool[i]); pool.RemoveAt(i); }
            BossFx.Toast($"삼켜졌다 — 실린더 {string.Join(", ", lostNow)}칸이 사라졌다! 옆구리 빛나는 곳을 2발씩 쏴서 되찾아라");
            StartCoroutine(Recover(lostNow));
            yield return Wait(T(0.6f));
        }

        IEnumerator Recover(List<int> lostNow)
        {
            var p = P;
            var spots = new List<(GameObject go, int hits)>();
            var offs = new[] { new Vector3(2.6f, 2f, 1.5f), new Vector3(-2.6f, 2f, 0.5f), new Vector3(2.6f, 2f, -2f) };
            var hitCount = new Dictionary<Health, int>();
            foreach (var o in offs)
            {
                var go = new GameObject("MemoryWeakPoint");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(o.x, 0, o.z) / Mathf.Max(0.01f, transform.lossyScale.x);
                Ch3Fx.Prim(PrimitiveType.Sphere, "Glow", go.transform, Vector3.up * o.y, Vector3.one * 0.9f, Ch3Fx.Pink, 0f);
                var h = Ch3Fx.Target(go, 0.7f, 3f);
                hitCount[h] = 0;
                h.OnDamaged += _ => hitCount[h]++;
                spots.Add((go, 0));
            }
            float end = Time.time + 12f;
            while (Time.time < end && lostNow.Count > 0 && hp)
            {
                foreach (var kv in new List<KeyValuePair<Health, int>>(hitCount))
                {
                    if (!kv.Key || kv.Value < 2 || !kv.Key.gameObject.activeSelf) continue;
                    kv.Key.gameObject.SetActive(false);
                    if (p && lostNow.Count > 0) { p.rev.lost[lostNow[0]] = false; BossFx.Toast($"{lostNow[0]}칸을 되찾았다"); lostNow.RemoveAt(0); }
                    Enemy.Fx3("Stardust", kv.Key.transform.position + Vector3.up * 2f, 0.9f);
                }
                yield return null;
            }
            if (p) foreach (int i in lostNow) p.rev.lost[i] = false;
            if (lostNow.Count > 0) BossFx.Toast("삼켜진 칸이 돌아왔다");
            foreach (var s in spots) if (s.go) Destroy(s.go);
        }

        // ---------- 1페이즈 패턴 ----------
        // 꼬리 내려치기: 뒤쪽 120° · 7m, 예고 0.8초, 피해 26 + 물보라 탄 8발(5m/초, 15)
        IEnumerator TailSlam()
        {
            var back = -transform.forward;
            Telegraph.Sector(transform.position, back, 7f, 60f, T(0.8f), Telegraph.Danger, transform);
            yield return Wait(T(0.8f));
            Sfx.Play("e3b_slam", 0.9f, 0.05f, 2);
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= 7f && Vector3.Angle(to, back) <= 60f) p.health.TakeDamage(26f);
            }
            CameraFollow.Shake(0.15f);
            var tailAt = transform.position + back * 5f;
            for (int i = 0; i < 8; i++)
                EnemyProjectile.Spawn(tailAt + Vector3.up * 1f, Quaternion.Euler(0, i * 45f, 0) * Vector3.forward, 5f, 15f, 14f, EnemyProjectile.Style.Orb);
            Enemy.Fx3("GlassBurst", tailAt + Vector3.up, 1.2f);
            Ripple(tailAt);
            yield return Wait(T(0.5f));
        }

        // 소용돌이: 예고 0.8초 → 반경 8m에서 1.5초 끌어당김(2.5m/초) → 반경 4m 폭발 26
        IEnumerator Vortex()
        {
            var c = transform.position;
            Telegraph.Ring(c, 8f, T(0.8f), Telegraph.Aim, transform).NoGlint();
            yield return Wait(T(0.8f));
            Sfx.Play("e3b_dive", 0.7f, 0.1f, 2);
            Telegraph.Ring(c, 4f, 1.5f, Telegraph.Danger, transform);
            float end = Time.time + 1.5f;
            while (Time.time < end)
            {
                var p = P;
                if (p && Effects.Flat(p.transform.position - transform.position).magnitude <= 8f) Ch3Fx.PullPlayer(transform.position, 2.5f);
                if (Random.value < 0.3f) Enemy.Fx3("Stardust", transform.position + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * 7f + Vector3.up * 0.5f, 0.4f);
                yield return null;
            }
            HitCircle(transform.position, 4f, 26f);
            CameraFollow.Shake(0.2f);
            Enemy.Fx3("GlassBurst", transform.position + Vector3.up * 1.5f, 1.6f);
            Ripple(transform.position);
            yield return Wait(T(0.5f));
        }

        // 잠수 돌진: 잠수 → 고양이 쪽 직선(폭 4m) 예고 1초 → 18m/초 돌진 28, 끝에서 튀어오름 반경 3m 22
        IEnumerator DiveCharge()
        {
            Sfx.Play("e3b_song", 0.6f, 0.05f, 1);
            hp.invulnerable = true;
            Height(-6f);
            Ripple(transform.position);
            yield return Wait(0.5f);
            var p = P;
            var dir = p ? Effects.Flat(p.transform.position - transform.position).normalized : transform.forward;
            float len = Mathf.Min(RayExitCircle(transform.position, dir, Ch3Fx.ArenaC(), ArenaR - 3f), 24f);
            var laneFrom = transform.position;
            Telegraph.Lane(laneFrom, dir, len, 4f, 1f, Telegraph.Danger);
            transform.rotation = Quaternion.LookRotation(dir);
            yield return Wait(1f);
            Sfx.Play("e3b_dive", 0.9f, 0.05f, 2);
            bool hit = false;
            for (float moved = 0; moved < len;)
            {
                float step = 18f * Time.deltaTime;
                transform.position += dir * step; moved += step;
                if (!hit && p && Telegraph.InLane(p.transform.position, laneFrom, dir, len, 4f, moved - 3f, moved + 2f)) { hit = true; p.health.TakeDamage(28f); }
                yield return null;
            }
            Height(1.5f);
            hp.invulnerable = false;
            yield return Wait(0.25f);
            Height(FlyH);
            HitCircle(transform.position, 3f, 22f);
            CameraFollow.Shake(0.2f);
            Ripple(transform.position);
            yield return Wait(T(0.6f));
        }

        // 거울 파편 뿌리기: 떠다니는 거울 조각 4개를 10초 동안 더 + 물방울 탄 12방향(6m/초, 15)
        IEnumerator ScatterShards()
        {
            Sfx.Play("e3b_shards", 0.8f, 0.08f, 2);
            if (anim) anim.jawTarget = 0.5f;
            yield return Wait(T(0.5f));
            for (int i = 0; i < 12; i++)
                EnemyProjectile.Spawn(transform.position + Vector3.up * 1.6f, Quaternion.Euler(0, i * 30f, 0) * Vector3.forward, 6f, 15f, 16f, EnemyProjectile.Style.Orb);
            var rm = RunManager.Instance;
            var room = rm ? rm.CurrentRoom : null;
            var ch = self.chapter ?? Tables.Chapter(3);
            if (room != null)
            {
                int before = MirrorShard.All.Count;
                MirrorShard.SpawnSet(ch, room, new System.Random(Random.Range(0, 99999)));
                MirrorShard.SpawnSet(ch, room, new System.Random(Random.Range(0, 99999)));
                var extra = MirrorShard.All.GetRange(before, MirrorShard.All.Count - before);
                while (extra.Count > 4) { Destroy(extra[extra.Count - 1].gameObject); extra.RemoveAt(extra.Count - 1); }
                StartCoroutine(Despawn(extra, 10f));
            }
            if (anim) anim.jawTarget = 0f;
            yield return Wait(T(0.5f));
        }

        IEnumerator Despawn(List<MirrorShard> list, float after)
        {
            yield return new WaitForSeconds(after);
            foreach (var m in list) if (m) { Enemy.Fx3("GlassBurst", m.transform.position + Vector3.up * 1.5f, 0.8f); Destroy(m.gameObject); }
        }

        // ---------- 2페이즈 패턴 ----------
        // 지느러미 휩쓸기: 낮게 원형 선회, 반경 2.5~8m 링 피해 26, 예고 0.8초 (안쪽 원 · 바깥만 안전)
        IEnumerator FinSweep()
        {
            var c = transform.position;
            Telegraph.Ring(c, 8f, T(0.8f), Telegraph.Danger);
            Telegraph.Ring(c, 2.5f, T(0.8f), Ch3Fx.Teal).NoGlint();
            Height(1f);
            yield return Wait(T(0.8f));
            Sfx.Play("e3b_slam", 0.8f, 0.1f, 2);
            if (anim) anim.rollTarget = 30f;
            var p = P;
            if (p && p.health.Alive)
            {
                float d = Effects.Flat(p.transform.position - c).magnitude;
                if (d >= 2.5f && d <= 8f) p.health.TakeDamage(26f);
            }
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { transform.Rotate(0, 600f * Time.deltaTime, 0); yield return null; }
            if (anim) anim.rollTarget = 0f;
            Height(FlyH);
            Enemy.Fx3("MirrorFlash", c + Vector3.up * 1.5f, 1.4f);
            yield return Wait(T(0.5f));
        }

        // 빛 브레스: 예고 1초 → 광선(폭 1.5 · 16m)이 2초 동안 90°를 쓸고 지나감, 0.25초마다 12
        IEnumerator LightBreath()
        {
            Sfx.Play("e3b_beam", 0.9f, 0.03f, 1);
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 16f, 45f, 1f, Telegraph.Danger);
            float tEnd = Time.time + 1f;
            while (Time.time < tEnd) { Face(d, 14f); yield return null; }
            if (anim) anim.jawTarget = 1f;
            var lr = new GameObject("LightBreath").AddComponent<LineRenderer>();
            lr.sharedMaterial = Ch2Fx.LineMat();
            lr.widthMultiplier = 1.5f;
            lr.positionCount = 2;
            lr.startColor = new Color(1f, 0.95f, 0.8f, 0.95f); lr.endColor = new Color(1f, 0.7f, 0.9f, 0.5f);
            float sign = Random.value < 0.5f ? -1 : 1, nextTick = 0;
            for (float t = 0; t < 2f && hp.Alive; t += Time.deltaTime)
            {
                var dir = Quaternion.Euler(0, sign * Mathf.Lerp(-45f, 45f, t / 2f), 0) * d;
                var from = transform.position + dir * 5f + Vector3.up * 1.6f;
                lr.SetPosition(0, from); lr.SetPosition(1, from + dir * 16f);
                if (Time.time >= nextTick && Ch2Fx.PlayerInLane(Effects.Flat(from), dir, 16f, 1.5f)) { P.health.TakeDamage(12f); nextTick = Time.time + 0.25f; }
                yield return null;
            }
            Destroy(lr.gameObject);
            if (anim) anim.jawTarget = 0f;
            yield return Wait(T(0.5f));
        }

        // 급강하: 그림자 원(반경 3.5) 예고 1.2초 → 내리꽂힘 30 + 충격파 링(6m/초, 18, 대시로 넘음)
        IEnumerator Plunge()
        {
            var p = P;
            if (!p) yield break;
            var at = Effects.Flat(p.transform.position);
            Height(12f);
            Telegraph.Ring(at, 3.5f, 1.2f, Telegraph.Danger);
            yield return Wait(1.2f);
            Sfx.Play("e3b_slam", 1f, 0f, 2);
            transform.position = new Vector3(at.x, transform.position.y, at.z);
            Height(0f);
            yield return Wait(0.15f);
            HitCircle(at, 3.5f, 30f);
            CameraFollow.Shake(0.3f);
            RippleRing.Spawn(at, 6f, 18f, 18f, 0.6f, Ch3Fx.Pink, 3.5f);
            Enemy.Fx3("GlassBurst", at + Vector3.up, 1.6f);
            yield return Wait(T(0.4f));
            Height(FlyH);
            yield return Wait(T(0.4f));
        }

        // 하늘 가르기: 고양이를 지나는 폭 5m 직선 예고 1.5초 → 30m/초로 낮게 통과, 28
        IEnumerator SkySlice()
        {
            var p = P;
            if (!p) yield break;
            var c = Ch3Fx.ArenaC();
            var dir = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
            var through = Effects.Flat(p.transform.position);
            var from = through - dir * ArenaR * 1.2f;
            float len = ArenaR * 2.4f;
            Telegraph.Lane(from, dir, len, 5f, 1.5f, Telegraph.Danger);
            hp.invulnerable = true;
            Height(14f);
            yield return Wait(1.5f);
            Sfx.Play("e3b_beam", 0.8f, 0.1f, 1);
            transform.position = new Vector3(from.x, transform.position.y, from.z);
            transform.rotation = Quaternion.LookRotation(dir);
            Height(1.2f);
            bool hit = false;
            for (float moved = 0; moved < len;)
            {
                float step = 30f * Time.deltaTime;
                transform.position += dir * step; moved += step;
                if (!hit && p && Telegraph.InLane(p.transform.position, from, dir, len, 5f, moved - 4f, moved + 4f)) { hit = true; p.health.TakeDamage(28f); }
                yield return null;
            }
            var rel = Effects.Flat(transform.position - c);
            if (rel.magnitude > ArenaR - 3f) transform.position = c + rel.normalized * (ArenaR - 4f);
            hp.invulnerable = false;
            Height(FlyH);
            yield return Wait(T(0.6f));
        }
    }
}
