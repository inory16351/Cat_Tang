// Heavy attacks drive the existing humanoid bones after the Animator evaluates.
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
            public float reachDrive;        // Blend toward full arm extension during contact.
            public bool accelerateToImpact; // Vertical strikes carry speed into the ground.
            public bool pivotTurn;         // the attack also spins the actor or model root
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

        // A planted foot keeps its world pose while the hips and shoulders lead the blade.
        internal struct Leg
        {
            public Transform thigh, knee, foot;
            public Vector3 plant, pole;
            public Quaternion rotation;
            public float length;
            public bool Valid => thigh && knee && foot;

            public Leg(Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones ankle)
            {
                thigh = animator.GetBoneTransform(upper);
                knee = animator.GetBoneTransform(lower);
                foot = animator.GetBoneTransform(ankle);
                length = thigh && knee && foot
                    ? Vector3.Distance(thigh.position, knee.position) + Vector3.Distance(knee.position, foot.position)
                    : 0f;
                plant = pole = Vector3.zero;
                rotation = Quaternion.identity;
            }

            public void Plant(Vector3 forward)
            {
                plant = foot.position;
                rotation = foot.rotation;
                pole = knee.position + forward * length;
            }

            public void Pose(Vector3 step, float lift, float pivot, float weight)
            {
                Solve(plant + step + Vector3.up * lift, pole + step, pivot, weight);
            }

            public void Turn(Vector3 start, Vector3 center, float degrees, bool lead, float weight)
            {
                // Alternate quick steps, holding the other foot's position until it lifts.
                const float stride = 90f;
                float offset = lead ? 0f : 0.5f;
                float phase = Mathf.Abs(degrees) / stride + offset;
                float step = Ease((Mathf.Repeat(phase, 1f) - 0.5f) * 2f);
                float turn = Mathf.Max(0f, Mathf.Floor(phase) + step - offset) * stride * Mathf.Sign(degrees);
                var plantedYaw = Quaternion.AngleAxis(turn, Vector3.up);
                var currentYaw = Quaternion.AngleAxis(degrees, Vector3.up);
                float lift = Mathf.Sin(step * Mathf.PI) * length * 0.10f;
                var target = center + plantedYaw * (plant - start) + Vector3.up * lift;
                Solve(target, center + currentYaw * (pole - start), turn, weight);
            }

            void Solve(Vector3 target, Vector3 guide, float yaw, float weight)
            {
                var ankle = Quaternion.Slerp(foot.rotation, Quaternion.AngleAxis(yaw, Vector3.up) * rotation, weight);
                TwoBone(thigh, knee, foot, Vector3.Lerp(foot.position, target, weight), guide);
                foot.rotation = ankle;
            }
        }

        Animator an;
        Transform body, uArm, lArm, hand, spine, chest, uArm2, lArm2, hand2;
        Quaternion bodyRest;
        Transform hips;
        Leg leftLeg, rightLeg;
        Vector3 hipBase, hipApplied, plantForward, plantRight, plantOrigin;
        bool hipOffsetApplied, plantPending, leadRight, pivotTurn;
        float turnDegrees, lastHeading;
        float legWeight, stepLength, leadStep, rearStep, weightShift, compression, rearPivot, brace;
        Vector3 longL, flatL;
        Vector3? glowTip, glowGrip;
        float lenL, armLen;
        Vector3 dir, flat;
        float bodyYaw, lean, activeReach;
        Coroutine cur;
        System.Action hitAfterPose;
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
            if (!GetComponent<HoverFloat>())
            {
                hips = an.GetBoneTransform(HumanBodyBones.Hips);
                leftLeg = new Leg(an, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
                rightLeg = new Leg(an, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            }
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
            // Curved scythes have a separate light span along the cutting edge.
            // Trail points continue to define the established swing radius.
            if (weapon.name.Contains("Scythe"))
            {
                int sideAxis = 3 - li - fi;
                float outer = Mathf.Abs(b.max[sideAxis]) >= Mathf.Abs(b.min[sideAxis]) ? b.max[sideAxis] : b.min[sideAxis];
                var bladeEdge = Vector3.zero;
                bladeEdge[sideAxis] = outer;
                glowGrip = longL * (lenL * 0.91f) + bladeEdge * 0.19f;
                glowTip = longL * (lenL * 0.49f) + bladeEdge * 0.94f;
            }
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

        void Update() => RestoreHipOffset();

        void RestoreHipOffset()
        {
            if (!hipOffsetApplied || !hips) return;
            if ((hips.localPosition - hipApplied).sqrMagnitude < 1e-8f) hips.localPosition = hipBase;
            hipOffsetApplied = false;
        }

        void PoseLegs()
        {
            if (!hips || !leftLeg.Valid || !rightLeg.Valid || legWeight <= 0f) return;
            float legLength = Mathf.Min(leftLeg.length, rightLeg.length);
            float side = leadRight ? 1f : -1f;
            hipBase = hips.localPosition;
            hips.position += legWeight * (plantRight * (weightShift * side * legLength * 0.12f)
                + plantForward * (weightShift * legLength * 0.07f) - Vector3.up * (compression * legLength * 0.14f));
            hipApplied = hips.localPosition;
            hipOffsetApplied = true;

            var front = leadRight ? rightLeg : leftLeg;
            var back = leadRight ? leftLeg : rightLeg;
            if (pivotTurn)
            {
                float heading = (body.parent ? body.parent : transform).eulerAngles.y;
                turnDegrees += Mathf.DeltaAngle(lastHeading, heading);
                lastHeading = heading;
                front.Turn(plantOrigin, transform.position, turnDegrees, true, legWeight);
                back.Turn(plantOrigin, transform.position, turnDegrees, false, legWeight);
                return;
            }

            Vector3 step = plantForward * stepLength + plantRight * (brace * side * legLength * 0.10f);
            float leadLift = Mathf.Sin(leadStep * Mathf.PI) * legLength * 0.12f;
            float rearLift = Mathf.Sin(rearStep * Mathf.PI) * legLength * 0.08f;
            front.Pose(step * leadStep, leadLift, leadStep * side * 12f, legWeight);
            back.Pose(plantForward * (stepLength * rearStep * 0.65f), rearLift, rearPivot * side * 28f, legWeight);
        }

        void LateUpdate()
        {
            if (!ready || !weapon || !body) return;
            // Capture untouched ankle poses before turning the model.
            if (plantPending && hips && leftLeg.Valid && rightLeg.Valid)
            {
                plantForward = Effects.Flat(transform.forward).normalized;
                plantRight = Vector3.Cross(Vector3.up, plantForward);
                leftLeg.Plant(plantForward);
                rightLeg.Plant(plantForward);
                plantOrigin = transform.position;
                lastHeading = (body.parent ? body.parent : transform).eulerAngles.y;
                turnDegrees = 0f;
                plantPending = false;
            }
            body.localRotation = bodyRest * Quaternion.Euler(lean * HipShare, bodyYaw * HipShare, 0);
            PoseLegs();

            float upYaw = bodyYaw * (1 - HipShare), upLean = lean * (1 - HipShare);
            int bones = spine && chest && spine != chest ? 2 : 1;
            Quaternion twist = Quaternion.AngleAxis(upYaw / bones, body.up)
                             * Quaternion.AngleAxis(upLean / bones, body.right);
            if (spine) spine.rotation = twist * spine.rotation;
            if (chest && chest != spine) chest.rotation = twist * chest.rotation;
            if (!bodyOnly) PoseWeapon();
            var hit = hitAfterPose;
            hitAfterPose = null;
            hit?.Invoke();
        }

        void PoseWeapon()
        {
            float sd = rightHand ? 1 : -1;
            var wd = body.TransformDirection(dir).normalized;
            var wf = Vector3.ProjectOnPlane(body.TransformDirection(flat), wd);
            if (wf.sqrMagnitude < 1e-4f) wf = Vector3.ProjectOnPlane(body.right, wd);
            wf.Normalize();
            // 손잡이 자리: 팔이 아래로 늘어진 방향과 칼 방향 사이, 조금 앞 · 위로
            var gd = new Vector3(0.25f * sd, -1f, 0.35f) * 0.35f + dir.normalized * 0.8f + new Vector3(0, 0.15f, 0.25f);
            bool two = twoHand && uArm2 && lArm2 && hand2;
            var from = two ? (uArm.position + uArm2.position) * 0.5f + body.right * sd * 0.2f : uArm.position;
            var grip = from + body.TransformDirection(gd.normalized) * armLen * Mathf.Lerp(reach, 0.985f, activeReach);
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

        internal static void TwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 pole)
        {
            float la = (b.position - a.position).magnitude, lb = (c.position - b.position).magnitude;
            var at = target - a.position;
            float d = Mathf.Clamp(at.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
            var dirT = at.sqrMagnitude > 1e-8f ? at.normalized : (c.position - a.position).normalized;
            var bend = Vector3.ProjectOnPlane(pole - a.position, dirT);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.down, dirT);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.right, dirT);
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
            hitAfterPose = null;
            if (cur != null) StopCoroutine(cur);
            cur = StartCoroutine(Run(s));
            return cur;
        }

        /// <summary>칼 방향만 천천히 옮겨 들고 있기 (예: 하늘로 치켜듦)</summary>
        public Coroutine Raise(Vector3 to, Vector3 flatTo, float time)
        {
            if (!ready) return null;
            hitAfterPose = null;
            if (cur != null) StopCoroutine(cur);
            legWeight = 0f;
            activeReach = 0f;
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
        public void Release(float time = 0.4f)
        {
            if (!ready) return;
            hitAfterPose = null;
            if (cur != null) StopCoroutine(cur);
            cur = StartCoroutine(Back(time));
        }

        IEnumerator Run(Swing s)
        {
            var ax = s.axis.normalized;
            var start = s.from.normalized;
            plantPending = hips && rightLeg.Valid && leftLeg.Valid;
            pivotTurn = s.pivotTurn;
            leadRight = Mathf.Abs(ax.y) < 0.6f || ax.y * s.sweep > 0f;
            stepLength = Mathf.Max(0, s.lunge);
            brace = Mathf.Abs(ax.y) > 0.6f ? 1f : 0.42f;
            legWeight = leadStep = rearStep = weightShift = compression = rearPivot = activeReach = 0f;
            var swingFlat = ax;                                    // 칼날(얇은 면의 옆선)이 앞장서게: 칼 면 법선 = 도는 축
            bool horizontal = Mathf.Abs(ax.y) > 0.6f;
            float Yaw(float th) => horizontal ? s.bodyFollow * (th - s.sweep * 0.5f) * Mathf.Sign(ax.y) : 0f;
            Vector3 d0 = dir, f0 = flat; float y0 = bodyYaw, l0 = lean;
            float windStart = Time.time;
            float holdStart = windStart + s.wind;
            float strikeStart = holdStart + s.windHold;
            float contactAt = strikeStart + s.act;
            // ① 감기: 사인 가감속 (무거운 무기를 천천히 들어 올려 천천히 꼭대기에 멈춤), 몸은 반대로 15% 더 감음
            while (Time.time < holdStart)
            {
                float k = Ease((Time.time - windStart) / s.wind);
                legWeight = k;
                weightShift = -k;
                compression = 0.55f * k;
                dir = Vector3.Slerp(d0, start, k); flat = Vector3.Slerp(f0, swingFlat, k);
                bodyYaw = Mathf.LerpAngle(y0, Yaw(0) * 1.15f, k); lean = Mathf.Lerp(l0, -s.leanBack, k);
                yield return null;
            }
            // ② 꼭대기 멈칫: 살짝 더 당김 (힘을 모음)
            while (Time.time < strikeStart)
            {
                float k = Mathf.Sin((Time.time - holdStart) / s.windHold * Mathf.PI * 0.5f);
                dir = Quaternion.AngleAxis(-Mathf.Sign(s.sweep) * 6f * k, ax) * start; flat = swingFlat;
                bodyYaw = Yaw(0) * (1.15f + 0.08f * k); lean = -s.leanBack * (1 + 0.15f * k);
                yield return null;
            }
            legWeight = 1f;
            float startYaw = bodyYaw;
            // A sweep eases out. A falling blade keeps gaining speed until contact.
            if (s.streak > 0 && weapon)
            {
                var tip = streakTip ?? longL * lenL;
                var gp = streakGrip ?? longL * lenL * 0.22f;
                BladeStreak.Begin(weapon, tip, gp, s.act + 0.05f, streakColor, streakExtend * s.streak,
                    glowTip: glowTip, glowGrip: glowGrip);
            }
            Vector3 fwd = Effects.Flat(transform.forward);
            float lastE = 0;
            float pullBack = -Mathf.Sign(s.sweep) * 6f;
            while (Time.time < contactAt)
            {
                float u = Mathf.Clamp01((Time.time - strikeStart) / s.act);
                float e = s.accelerateToImpact ? u * u * (0.35f + 0.65f * u) : Heavy(u);
                float th = Mathf.Lerp(pullBack, s.sweep, e);
                dir = Quaternion.AngleAxis(th, ax) * start;
                // Hips initiate the turn and the leading foot catches the transfer.
                float drive = Heavy(u + 0.14f * Mathf.Sin(u * Mathf.PI));
                activeReach = s.reachDrive * Heavy(u / 0.56f);
                leadStep = Ease(u / 0.52f);
                rearPivot = Ease(u / 0.82f);
                weightShift = Mathf.Lerp(-1f, 1f, drive);
                float loading = s.accelerateToImpact ? Ease((u - 0.74f) / 0.26f) : Ease((u - 0.56f) / 0.44f);
                compression = Mathf.Lerp(0.55f, 0.22f, drive) + 0.66f * loading;
                bodyYaw = Mathf.LerpAngle(startYaw, Yaw(s.sweep) * 1.12f, drive);
                lean = Mathf.Lerp(-s.leanBack, s.leanFwd, drive);
                if (s.lunge > 0) { transform.position += fwd * s.lunge * (e - lastE); lastE = e; }
                yield return null;
            }
            dir = Quaternion.AngleAxis(s.sweep, ax) * start;
            bodyYaw = Yaw(s.sweep) * 1.12f;
            lean = s.leanFwd;
            leadStep = rearPivot = weightShift = 1f;
            activeReach = s.reachDrive;
            compression = 0.88f;
            if (s.lunge > 0) transform.position += fwd * s.lunge * (1 - lastE);
            hitAfterPose = s.onHit;
            // ④ 끝 자세 유지 (박힘 · 빈틈) — 몸 넘침은 이때 천천히 제자리로
            float yHit = bodyYaw;
            for (float t = 0; t < s.hold; t += Time.deltaTime)
            {
                float k = Ease(t / s.hold);
                bodyYaw = Mathf.LerpAngle(yHit, Yaw(s.sweep), k);
                float recoil = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.16f));
                lean = s.leanFwd + s.leanFwd * 0.18f * recoil;
                compression = Mathf.Lerp(0.88f, 0.56f, k);
                yield return null;
            }
            // ⑤ 돌아오기: 처음 45%는 그대로, 나머지 부드럽게
            yield return Back(s.rec, 0.45f);
        }

        /// <summary>무거운 베기 곡선: 0 → 1, 시작은 가속(정지에서 출발), 가장 빠른 곳은 앞쪽(u ≈ 0.4), 끝은 길게 감속</summary>
        public static float Heavy(float u) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Pow(Mathf.Clamp01(u), 0.7f));

        internal static float Ease(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));

        IEnumerator Back(float time, float holdFrac = 0f)
        {
            Vector3 d0 = dir, f0 = flat; float y0 = bodyYaw, l0 = lean;
            float plantWeight = legWeight, settle = compression, shift = weightShift, heldReach = activeReach;
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01((t / time - holdFrac) / (1 - holdFrac));
                rearStep = Ease(k / 0.73f);
                legWeight = plantWeight * (1 - Ease((k - 0.40f) / 0.60f));
                k = Ease(k);
                weightShift = Mathf.Lerp(shift, 0f, k);
                activeReach = Mathf.Lerp(heldReach, 0f, k);
                compression = Mathf.Lerp(settle, 0f, k);
                dir = Vector3.Slerp(d0, restDir, k); flat = Vector3.Slerp(f0, restFlat, k);
                bodyYaw = Mathf.LerpAngle(y0, 0, k); lean = Mathf.Lerp(l0, 0, k);
                yield return null;
            }
            dir = restDir; flat = restFlat; bodyYaw = 0; lean = 0;
            legWeight = activeReach = 0f;
            cur = null;
        }

        // ================= 맞는 순간 (랫키호테: 히트스톱 동안 칼빛은 멈추고 충격은 계속) =================
        /// <summary>무거운 일격: 히트스톱 · 화면 흔들림 · 지면 폭발(StylizedFx.GroundBlast, ring = 폭발 색). power 1 = 보통 큰 일격
        /// (2026-11-20 사용자: 폭발은 연기보다 레퍼런스 지면 폭발 느낌 → 회색 흙먼지 덩이 빼고 지면 폭발로)</summary>
        public void Impact(Vector3 at, float power, StylizedFx.Look? ring)
        {
            at.y = transform.position.y;
            CameraFollow.Shake(0.22f + 0.18f * power);
            CombatHitStop.Play(0.035f + 0.023f * power);
            StylizedFx.GroundBlast(at, 1.6f + 1.2f * power, ring ?? StylizedFx.BlastRed);
        }

        void OnDisable()
        {
            RestoreHipOffset();
            legWeight = 0f;
            plantPending = false;
            hitAfterPose = null;
        }

    }
}
