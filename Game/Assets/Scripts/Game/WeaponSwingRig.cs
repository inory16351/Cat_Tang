// 큰 무기 휘두르기 (2026-11-20 사용자: "휘두르기랑 돌진 베기 다시 — 랫키호테 평타 모션 분석해서 3D로, 돌리는 각이 너무 작아",
//   "대검은 양손으로 낮게 들고 크게 베기", "거대한 무기를 휘두르는 압도적인 느낌", "낫 휘두르는 애들도", "모션 가감속도")
// 근육 클립(작은 몸통 비틀기)으로는 칼이 60°쯤밖에 안 돌아서, 무기 방향을 코드로 직접 쥠:
//  - 무기는 적 루트 아래로 옮기고, 매 프레임(애니메이터 다음) 몸 기준 '칼 방향'과 '칼 면'으로 놓음. 무기의 긴 축 · 얇은 축은 메시 경계에서 스스로 잼 (원점 = 손잡이)
//  - 손잡이 자리 = 어깨에서 칼 방향 쪽으로 팔 길이만큼 → 오른팔(또는 왼팔)을 두 뼈 IK로 거기 놓음 (팔꿈치는 바깥 · 아래 · 뒤)
//  - 휘두르기 = 축 둘레로 칼 방향을 sweep°만큼 돌림 (랫키호테 1타 360° · 2타 240° · 3타 350°처럼 크게).
//    박자 = 랫키호테 attack: 감기(ease-out 세제곱 · 몸을 반대로 더 감음) → 꼭대기 멈칫(살짝 더 당김) → 베기(1 − (1 − u)^pow, 처음이 가장 빠르고 끝에서 감속)
//          → 끝 자세 유지(박힘 · 빈틈) → 처음 45%는 그대로, 나머지는 부드럽게(smoothstep) 쉬는 자세로
//    몸 전체도 칼을 따라 돎(bodyFollow) · 감을 때 뒤로 젖혔다 벨 때 0.35 안에 앞으로 숙임 · 앞으로 한 걸음(lunge)
//  - 무거운 가감속 (2026-11-20 사용자: "천천히 시작점으로 들어 올린 뒤 빠르게 돌리며 가감속 — 무거운 무기로 강하게"):
//    감기 = 사인 가감속(천천히 떠서 천천히 멈춤), 베기 = (1 − cos(π · u^0.7)) / 2 (빠르게 붙는 가속 → 끝에서 길게 감속)
//  - 하체 (사용자: "하체 움직임도 자연스럽게"): 몸 회전 · 숙임을 골반(몸 전체) 35%, 허리 · 가슴 뼈 65%로 나눠 발이 통째로 돌지 않게
//  - 칼빛 = BladeStreak (랫키호테 칼빛 붙이는 방식: 실제 칼끝 · 밑동 기록, 칼끝 extend배), 맞는 순간 Impact: 히트스톱 · 흔들림 · 흙먼지 · 충격 고리
using System.Collections;
using UnityEngine;

namespace CatTang
{
    [DefaultExecutionOrder(-50)]   // BladeStreak(0)이 이 프레임의 무기 자리를 읽기 전에 놓음
    public class WeaponSwingRig : MonoBehaviour
    {
        public struct Swing
        {
            public Vector3 axis, from;      // 몸 기준 (x 오른쪽 · y 위 · z 앞): 도는 축 · 시작 칼 방향
            public float sweep;             // 도는 각 (+ = 축 기준 오른나사)
            public float wind, windHold, act, hold, rec;
            public float actPow;            // 베기 감속 지수 (랫키호테 1타 2.2)
            public float bodyFollow;        // 몸이 칼 회전을 따라가는 비율 (축이 위일 때)
            public float leanBack, leanFwd; // 감을 때 젖힘 · 벨 때 숙임 (도)
            public float lunge;             // 베는 동안 앞으로 (m)
            public float streak;            // 칼빛 길이 배율 (0 = 없음)
            public System.Action onHit;     // 베기가 끝나는 순간 (칼이 목표에 닿음)
        }

        public Transform weapon;
        public Color streakColor = BladeStreak.Ch4Base;
        public float streakExtend = 1.25f;
        public Vector3 restDir = new(0.3f, -0.37f, 0.88f), restFlat = Vector3.right;
        public bool rightHand = true;
        public float reach = 0.85f;
        public Vector3? streakTip, streakGrip;   // 무기 기준 칼빛 자리 (없으면 긴 축 끝 · 1/4 지점)
        // 양손 쥐기 (2026-11-20 사용자: 막보 낫을 사신처럼 두 손으로): 오른손 = 손잡이(아래), 왼손 = 자루를 따라 handGap(m) 위. 쥐는 자리는 두 어깨 가운데 앞
        public bool twoHand;
        public float handGap = 1f;

        Animator an;
        Transform body, uArm, lArm, hand, spine, chest, uArm2, lArm2, hand2;
        Quaternion bodyRest;
        Vector3 longL, flatL;
        float lenL, armLen;
        Vector3 dir, flat;
        float bodyYaw, lean;
        Coroutine cur;
        bool ready, bodyOnly;
        /// <summary>무기가 몸 메시에 붙어 있음 (Weapon_ 자리에 메시 없음) → 무기는 손을 따라가고, 몸 전체 회전 · 숙임만 코드로</summary>
        public bool BodyOnly => bodyOnly;
        public bool Swinging => cur != null;
        public float Length => lenL * (weapon ? weapon.lossyScale.x : 1f);

        /// <summary>적 모델의 Weapon_이름 무기를 잡는 조종기 (이미 있으면 그것). keepPose = 지금 쥔 방향을 쉬는 자세로</summary>
        const float HipShare = 0.35f;

        // ================= 범위 통일 (2026-11-20 사용자: "데미지 범위 · 이펙트 · 텔레그래프 범위 통일, 지금 이펙트 범위가 데미지 콜라이더") =================
        /// <summary>칼빛이 닿는 가장 먼 거리 (적 중심 → 늘린 칼끝, 바닥 기준): 어깨 옆 거리 + 팔 + 칼빛 칼끝 (BladeStreak와 같은 늘림)</summary>
        public float EffectRadius(float streak = 1f)
        {
            if (!ready || bodyOnly || !weapon) return 0f;
            var tipL = streakTip ?? longL * lenL;
            var gL = streakGrip ?? longL * lenL * 0.22f;
            float tip = weapon.TransformVector(gL).magnitude + weapon.TransformVector(tipL - gL).magnitude * streakExtend * Mathf.Max(0.01f, streak);
            return Effects.Flat(uArm.position - transform.position).magnitude + armLen * reach * 0.8f + tip;
        }

        /// <summary>수평 휘두르기가 쓸고 가는 부채꼴 (fwd = 적이 바라보는 쪽): 칼 방향 회전 + 골반 회전까지. 예고 · 판정이 모두 이걸 씀</summary>
        public (Vector3 center, float half, float radius) Sector(Swing s, Vector3 fwd)
        {
            fwd = Effects.Flat(fwd).normalized;
            float sg = Mathf.Sign(s.axis.y);
            float y0 = Mathf.Atan2(s.from.x, s.from.z) * Mathf.Rad2Deg;
            float a0 = y0 - HipShare * s.bodyFollow * s.sweep * 0.5f * sg * 1.2f;
            float a1 = y0 + s.sweep * sg + HipShare * s.bodyFollow * s.sweep * 0.5f * sg * 1.12f;
            float c = (a0 + a1) * 0.5f, half = Mathf.Min(180f, Mathf.Abs(a1 - a0) * 0.5f + 6f);
            return (Quaternion.Euler(0, c, 0) * fwd, half, EffectRadius(s.streak));
        }

        public static bool InSector(Vector3 pt, Vector3 at, Vector3 center, float half, float radius)
        {
            var to = Effects.Flat(pt - at);
            return to.magnitude <= radius && (half >= 180f || Vector3.Angle(center, to) <= half);
        }

        public static WeaponSwingRig Get(Enemy e, string weaponName, Color col, bool keepPose = true)
        {
            if (!e) return null;
            var r = e.GetComponent<WeaponSwingRig>();
            if (r && r.ready) return r;
            Transform w = null;
            foreach (var t in e.GetComponentsInChildren<Transform>(true)) if (t.name == "Weapon_" + weaponName) { w = t; break; }
            if (!w) return null;
            if (!r) r = e.gameObject.AddComponent<WeaponSwingRig>();
            r.weapon = w; r.streakColor = col;
            r.Init(keepPose);
            return r.ready ? r : null;
        }

        void Init(bool keepPose)
        {
            an = GetComponentInChildren<Animator>();
            if (!an || !an.isHuman || !weapon) return;
            body = an.transform;
            bodyRest = body.localRotation;
            var side = rightHand ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm;
            uArm = an.GetBoneTransform(side);
            lArm = an.GetBoneTransform(rightHand ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            hand = an.GetBoneTransform(rightHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (!uArm || !lArm || !hand) return;
            uArm2 = an.GetBoneTransform(rightHand ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            lArm2 = an.GetBoneTransform(rightHand ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            hand2 = an.GetBoneTransform(rightHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            spine = an.GetBoneTransform(HumanBodyBones.Spine);
            chest = an.GetBoneTransform(HumanBodyBones.Chest); if (!chest) chest = an.GetBoneTransform(HumanBodyBones.UpperChest);
            armLen = (lArm.position - uArm.position).magnitude + (hand.position - lArm.position).magnitude;
            // 무기 축: 무기 기준 경계 상자 → 가장 긴 축(원점에서 먼 쪽) = 칼 방향, 가장 얇은 축 = 칼 면
            var b = new Bounds(); bool any = false;
            foreach (var mf in weapon.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = weapon.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (!any) { bodyOnly = true; ready = true; return; }
            var s = b.size;
            int li = s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2;
            int fi = s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2;
            if (fi == li) fi = (li + 1) % 3;
            float sign = b.center[li] >= 0 ? 1 : -1;
            longL = Vector3.zero; longL[li] = sign;
            flatL = Vector3.zero; flatL[fi] = 1;
            lenL = sign > 0 ? b.max[li] : -b.min[li];
            if (keepPose)
            {
                restDir = body.InverseTransformDirection(weapon.TransformDirection(longL)).normalized;
                restFlat = body.InverseTransformDirection(weapon.TransformDirection(flatL)).normalized;
            }
            weapon.SetParent(body.parent ? body.parent : transform, true);   // 모델 루트 아래 (죽을 때 모델과 같이 사라지게, 손 뼈는 아님)
            dir = restDir.normalized; flat = restFlat.normalized;
            ready = true;
        }

        /// <summary>무기 크기 배율 (예: 막보 낫이 몸보다 훨씬 커서 줄임)</summary>
        public void ScaleWeapon(float k) { if (weapon) weapon.localScale *= k; }

        public void SetRest(Vector3 d, Vector3 f) { restDir = d.normalized; restFlat = f.normalized; if (cur == null) { dir = restDir; flat = restFlat; } }

        public Vector3 TipWorld => weapon ? weapon.position + weapon.TransformDirection(longL) * Length : transform.position;

        void LateUpdate()
        {
            if (!ready || !weapon || !body) return;
            // 골반(몸 전체) 35% · 허리 · 가슴 65% (애니메이터가 이 프레임 뼈를 놓은 뒤라 덧돌림)
            body.localRotation = bodyRest * Quaternion.Euler(lean * HipShare, bodyYaw * HipShare, 0);
            float upYaw = bodyYaw * (1 - HipShare), upLean = lean * (1 - HipShare);
            var bones = chest && spine ? 2 : 1;
            foreach (var b in new[] { spine, chest })
            {
                if (!b) continue;
                b.rotation = Quaternion.AngleAxis(upYaw / bones, body.up) * Quaternion.AngleAxis(upLean / bones, body.right) * b.rotation;
            }
            if (bodyOnly) return;
            float sd = rightHand ? 1 : -1;
            var wd = body.TransformDirection(dir).normalized;
            var wf = Vector3.ProjectOnPlane(body.TransformDirection(flat), wd);
            if (wf.sqrMagnitude < 1e-4f) wf = Vector3.ProjectOnPlane(body.right, wd);
            wf.Normalize();
            // 손잡이 자리: 팔이 아래로 늘어진 방향과 칼 방향 사이, 조금 앞 · 위로
            var gd = new Vector3(0.25f * sd, -1f, 0.35f) * 0.35f + dir.normalized * 0.8f + new Vector3(0, 0.15f, 0.25f);
            bool two = twoHand && uArm2 && lArm2 && hand2;
            var from = two ? (uArm.position + uArm2.position) * 0.5f + body.right * sd * 0.2f : uArm.position;
            var grip = from + body.TransformDirection(gd.normalized) * armLen * reach;
            if (two) grip -= wd * handGap * 0.45f;                                         // 두 손 사이가 어깨 앞에 오게 손잡이를 조금 내림
            var pole = uArm.position + (body.right * sd - body.up * 0.5f - body.forward * 0.5f) * armLen;
            TwoBone(uArm, lArm, hand, grip, pole);
            weapon.SetPositionAndRotation(hand.position, Quaternion.LookRotation(wf, wd) * Quaternion.Inverse(Quaternion.LookRotation(flatL, longL)));
            if (two)
            {
                var pole2 = uArm2.position + (-body.right * sd - body.up * 0.5f - body.forward * 0.3f) * armLen;
                TwoBone(uArm2, lArm2, hand2, hand.position + wd * handGap, pole2);
            }
        }

        static void TwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 pole)
        {
            float la = (b.position - a.position).magnitude, lb = (c.position - b.position).magnitude;
            var at = target - a.position;
            float d = Mathf.Clamp(at.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
            var dirT = at.normalized;
            var bend = Vector3.ProjectOnPlane(pole - a.position, dirT);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.down, dirT);
            bend.Normalize();
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2 * la * d), -1, 1), sinA = Mathf.Sqrt(1 - cosA * cosA);
            var elbow = a.position + dirT * la * cosA + bend * la * sinA;
            a.rotation = Quaternion.FromToRotation(b.position - a.position, elbow - a.position) * a.rotation;
            var tgt = a.position + dirT * d;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, tgt - b.position) * b.rotation;
        }

        public Coroutine Play(Swing s)
        {
            if (!ready) return null;
            if (cur != null) StopCoroutine(cur);
            cur = StartCoroutine(Run(s));
            return cur;
        }

        /// <summary>칼 방향만 천천히 옮겨 들고 있기 (예: 하늘로 치켜듦)</summary>
        public Coroutine Raise(Vector3 to, Vector3 flatTo, float time)
        {
            if (!ready) return null;
            if (cur != null) StopCoroutine(cur);
            cur = StartCoroutine(RaiseRun(to.normalized, flatTo.normalized, time));
            return cur;
        }
        IEnumerator RaiseRun(Vector3 to, Vector3 f1, float time)
        {
            Vector3 d0 = dir, f0 = flat;
            for (float t = 0; t < time; t += Time.deltaTime) { float k = 1 - Mathf.Pow(1 - t / time, 3); dir = Vector3.Slerp(d0, to, k); flat = Vector3.Slerp(f0, f1, k); yield return null; }
            dir = to; flat = f1;
            cur = null;
        }
        public void Release(float time = 0.4f) { if (!ready) return; if (cur != null) StopCoroutine(cur); cur = StartCoroutine(Back(time)); }

        IEnumerator Run(Swing s)
        {
            var ax = s.axis.normalized;
            var start = s.from.normalized;
            var swingFlat = ax;                                    // 칼날(얇은 면의 옆선)이 앞장서게: 칼 면 법선 = 도는 축
            bool horizontal = Mathf.Abs(ax.y) > 0.6f;
            float Yaw(float th) => horizontal ? s.bodyFollow * (th - s.sweep * 0.5f) * Mathf.Sign(ax.y) : 0f;
            Vector3 d0 = dir, f0 = flat; float y0 = bodyYaw, l0 = lean;
            // ① 감기: 사인 가감속 (무거운 무기를 천천히 들어 올려 천천히 꼭대기에 멈춤), 몸은 반대로 15% 더 감음
            for (float t = 0; t < s.wind; t += Time.deltaTime)
            {
                float k = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t / s.wind);
                dir = Vector3.Slerp(d0, start, k); flat = Vector3.Slerp(f0, swingFlat, k);
                bodyYaw = Mathf.LerpAngle(y0, Yaw(0) * 1.15f, k); lean = Mathf.Lerp(l0, -s.leanBack, k);
                yield return null;
            }
            // ② 꼭대기 멈칫: 살짝 더 당김 (힘을 모음)
            for (float t = 0; t < s.windHold; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / s.windHold * Mathf.PI * 0.5f);
                dir = Quaternion.AngleAxis(-Mathf.Sign(s.sweep) * 6f * k, ax) * start; flat = swingFlat;
                bodyYaw = Yaw(0) * (1.15f + 0.08f * k); lean = -s.leanBack * (1 + 0.15f * k);
                yield return null;
            }
            var startHeld = dir;
            float startYaw = bodyYaw;
            // ③ 베기: 무거운 가속 → 가장 빠름(전체의 40% 지점) → 길게 감속
            if (s.streak > 0 && weapon)
            {
                var tip = streakTip ?? longL * lenL;
                var gp = streakGrip ?? longL * lenL * 0.22f;
                BladeStreak.Begin(weapon, tip, gp, s.act + 0.05f, streakColor, streakExtend * s.streak);
            }
            Vector3 p0 = transform.position, fwd = Effects.Flat(transform.forward);
            float lastE = 0;
            float pullBack = -Mathf.Sign(s.sweep) * 6f;
            for (float t = 0; t < s.act; t += Time.deltaTime)
            {
                float u = t / s.act, e = Heavy(u);
                float th = Mathf.Lerp(pullBack, s.sweep, e);
                dir = Quaternion.AngleAxis(th, ax) * start;
                bodyYaw = Mathf.LerpAngle(startYaw, Yaw(s.sweep) * 1.12f, e);     // 몸은 12% 넘쳐 돎
                lean = Mathf.Lerp(-s.leanBack, s.leanFwd, Mathf.Pow(Mathf.Min(1, u / 0.35f), 0.6f));
                if (s.lunge > 0) { transform.position += fwd * s.lunge * (e - lastE); lastE = e; }
                yield return null;
            }
            dir = Quaternion.AngleAxis(s.sweep, ax) * start;
            if (s.lunge > 0) transform.position += fwd * s.lunge * (1 - lastE);
            s.onHit?.Invoke();
            // ④ 끝 자세 유지 (박힘 · 빈틈) — 몸 넘침은 이때 천천히 제자리로
            float yHit = bodyYaw;
            for (float t = 0; t < s.hold; t += Time.deltaTime) { bodyYaw = Mathf.LerpAngle(yHit, Yaw(s.sweep), t / s.hold); yield return null; }
            // ⑤ 돌아오기: 처음 45%는 그대로, 나머지 부드럽게
            yield return Back(s.rec, 0.45f);
        }

        /// <summary>무거운 베기 곡선: 0 → 1, 시작은 가속(정지에서 출발), 가장 빠른 곳은 앞쪽(u ≈ 0.4), 끝은 길게 감속</summary>
        public static float Heavy(float u) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Pow(Mathf.Clamp01(u), 0.7f));

        IEnumerator Back(float time, float holdFrac = 0f)
        {
            Vector3 d0 = dir, f0 = flat; float y0 = bodyYaw, l0 = lean;
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01((t / time - holdFrac) / (1 - holdFrac));
                k = k * k * (3 - 2 * k);
                dir = Vector3.Slerp(d0, restDir, k); flat = Vector3.Slerp(f0, restFlat, k);
                bodyYaw = Mathf.LerpAngle(y0, 0, k); lean = Mathf.Lerp(l0, 0, k);
                yield return null;
            }
            dir = restDir; flat = restFlat; bodyYaw = 0; lean = 0;
            cur = null;
        }

        // ================= 맞는 순간 (랫키호테: 히트스톱 동안 칼빛은 멈추고 충격은 계속) =================
        static bool stopping;
        /// <summary>무거운 일격: 히트스톱 · 화면 흔들림 · 지면 폭발(StylizedFx.GroundBlast, ring = 폭발 색). power 1 = 보통 큰 일격
        /// (2026-11-20 사용자: 폭발은 연기보다 레퍼런스 지면 폭발 느낌 → 회색 흙먼지 덩이 빼고 지면 폭발로)</summary>
        public void Impact(Vector3 at, float power, StylizedFx.Look? ring)
        {
            at.y = transform.position.y;
            CameraFollow.Shake(0.22f + 0.18f * power);
            if (!stopping && Time.timeScale > 0.99f) StartCoroutine(HitStop(0.05f + 0.045f * power));
            StylizedFx.GroundBlast(at, 1.6f + 1.2f * power, ring ?? StylizedFx.BlastRed);
        }

        void OnDisable() { if (stopping) { Time.timeScale = 1f; stopping = false; } }   // 히트스톱 중에 사라지면 시간을 되돌림

        static IEnumerator HitStop(float real)
        {
            stopping = true;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(real);
            if (Time.timeScale < 0.1f) Time.timeScale = 1f;
            stopping = false;
        }
    }
}
