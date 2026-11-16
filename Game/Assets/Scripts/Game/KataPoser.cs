// 고양이 모델 자세 덧입히기 (Animator 결과 위에 LateUpdate로).
// 자세 = 팔 · 다리가 가리킬 방향(캐릭터 기준) + 허리 비틀기 · 기울기 + 골반 높이. 뼈 축과 무관하게 '방향을 맞추는' 식이라 리그가 바뀌어도 동작.
// - 평소 전투: 두 팔을 조준 방향으로 (Aim)
// - 불릿 파티: 랫키호테 쌍총 사냥꾼 건카타 8자세 (Kata 0~7) 사이를 홱홱 끊어 넘어감
// 총은 손에 붙어 있고 매 프레임 팔 방향(아래팔 → 손)을 따라 총구가 향함.
using UnityEngine;

namespace CatTang
{
    [System.Serializable]
    public struct KataPose
    {
        public Vector3 rArm, lArm;          // 팔 방향 (캐릭터 기준, z = 앞)
        public Vector3 rThigh, rShin, lThigh, lShin;   // 다리 방향 (0 = 애니메이션 그대로)
        public float twist, lean, side, hips, headYaw;  // 허리 비틀기(도) · 앞 기울기(도) · 옆 기울기(도) · 골반 높이(m, 모델 크기 기준) · 고개
        public bool legs;

        public static KataPose Lerp(KataPose a, KataPose b, float t) => new KataPose
        {
            rArm = Vector3.Slerp(a.rArm, b.rArm, t), lArm = Vector3.Slerp(a.lArm, b.lArm, t),
            rThigh = Vector3.Slerp(a.rThigh, b.rThigh, t), rShin = Vector3.Slerp(a.rShin, b.rShin, t),
            lThigh = Vector3.Slerp(a.lThigh, b.lThigh, t), lShin = Vector3.Slerp(a.lShin, b.lShin, t),
            twist = Mathf.Lerp(a.twist, b.twist, t), lean = Mathf.Lerp(a.lean, b.lean, t), side = Mathf.Lerp(a.side, b.side, t),
            hips = Mathf.Lerp(a.hips, b.hips, t), headYaw = Mathf.Lerp(a.headYaw, b.headYaw, t),
            legs = t < 0.5f ? a.legs : b.legs
        };
    }

    public class KataPoser : MonoBehaviour
    {
        public Animator anim;
        public Transform gunR, gunL, muzzleR, muzzleL;
        [Range(0, 1)] public float aimWeight = 1f;
        /// <summary>총 원점(손잡이)을 주먹 기준으로 얼마나 옮길지 (총 기준 좌표, m). 음수 y = 총을 아래로 → 손잡이가 주먹 안으로</summary>
        // 총 원점(손잡이 가운데)을 주먹 중심에서 총 기준으로 옮기는 양 (m, 모델 크기 비례). y = 아래로, z = 총구 쪽으로.
        // 2026-11-16: z가 -0.02라 주먹이 실린더 위에 얹히고 손잡이가 주먹 뒤로 삐져나왔음 → 총을 앞으로 당겨 주먹이 손잡이 윗부분을 감싸게
        public Vector3 gripOffset = new Vector3(0, -0.09f, 0.09f);
        public float snapTime = 0.05f;                  // 자세 전환 시간 (절도 있게 짧게)

        Transform hips, spine, head, rUp, rLo, rHand, lUp, lLo, lHand, rTh, rSh, rFoot, lTh, lSh, lFoot;
        Transform rIndex, rThumb, lIndex, lThumb;      // 주먹 중심 · 엄지 방향 (총 쥐는 자세)
        // 손 · 손가락 뼈의 기본(모델링) 자세 회전. 자세를 덧입힐 땐 애니메이션의 손목 꺾임 · 손가락 움직임을 지우고 이 자세로 되돌림.
        // 걷기 애니메이션이 손을 돌리면 엄지 위치가 바뀌어 Roll이 아래팔을 다른 각으로 비틀고, 아래팔에 붙은 주먹 메시가 축 둘레로 돌아
        // 늘 같은 자리에 놓이는 총과 따로 놀았음 (2026-11-16 걸을 때 총이 손에서 떨어지는 문제)
        Transform[] handBonesR, handBonesL;
        Quaternion[] handRestR, handRestL;
        // 상체(허리 · 가슴 · 쇄골)의 기본(모델링, 좌우 대칭) 자세 회전. 자세를 덧입힐 땐 이 자세로 되돌린 뒤 비틀기 · 기울기를 얹음.
        // 기본 애니메이션의 상체 비틀림이 남으면 왼어깨가 오른어깨보다 8cm 바깥 · 4cm 아래에 놓여, 같은 방향으로 뻗은 두 팔과 총이
        // 서로 다른 자리에서 나와 어색했음 (2026-11-16)
        Transform[] torsoBones;
        Quaternion[] torsoRest;
        KataPose from, to, cur;
        float t = 1, weight, weightTarget;
        float scale = 1;
        /// <summary>골반 뼈 (몸 중심 = 회전축)</summary>
        public Transform Hips => hips;

        // ---------- 자세 표 ----------
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z).normalized;
        public static readonly KataPose Aim = new KataPose { rArm = V(0.14f, -0.22f, 1), lArm = V(-0.14f, -0.22f, 1) };

        /// <summary>건카타 8자세 (랫키호테 쌍총 사냥꾼 kata_0~7). backFacing = 몸이 대상 반대쪽을 봄</summary>
        public static readonly KataPose[] Kata =
        {
            // 0 앞뒤 동시: 오른팔 앞, 왼팔 뒤, 다리 넓게
            new KataPose { rArm = V(0.12f, -0.22f, 1), lArm = V(-0.12f, -0.22f, -1), twist = -15, legs = true,
                rThigh = V(0.45f, -1, 0.25f), rShin = V(0.25f, -1, -0.1f), lThigh = V(-0.45f, -1, -0.25f), lShin = V(-0.25f, -1, 0.1f), hips = -0.06f },
            // 1 위아래: 오른팔 하늘, 왼팔 아래 앞
            new KataPose { rArm = V(0.15f, 1, 0.25f), lArm = V(-0.35f, -0.7f, 0.6f), side = 6, legs = true,
                rThigh = V(0.3f, -1, 0.3f), rShin = V(0.2f, -1, -0.2f), lThigh = V(-0.3f, -1, -0.2f), lShin = V(-0.2f, -1, 0), hips = -0.05f },
            // 2 뒤돌아 겨드랑이 밑: 몸은 등지고 오른팔이 왼 겨드랑이 밑으로 뒤를 쏨 (등진 상태에서 -z가 대상)
            new KataPose { rArm = V(-0.35f, -0.25f, -1), lArm = V(0.3f, 0.1f, 0.7f), twist = 25, lean = 10, headYaw = 60, legs = true,
                rThigh = V(0.3f, -1, 0.1f), rShin = V(0.1f, -1, -0.15f), lThigh = V(-0.3f, -1, -0.1f), lShin = V(-0.1f, -1, 0.1f), hips = -0.04f },
            // 3 X자 양옆: 팔을 교차해 양옆으로
            new KataPose { rArm = V(-1, -0.32f, 0.4f), lArm = V(1, -0.32f, 0.4f), legs = true,
                rThigh = V(0.35f, -1, 0), rShin = V(0.25f, -1, 0), lThigh = V(-0.35f, -1, 0), lShin = V(-0.25f, -1, 0), hips = -0.08f },
            // 4 공중 V: 뛰어올라 두 팔 V, 다리 접음
            new KataPose { rArm = V(0.7f, 1, 0.25f), lArm = V(-0.7f, 1, 0.25f), lean = -10, legs = true, hips = 0.22f,
                rThigh = V(0.2f, -0.35f, 1), rShin = V(0.1f, -1, -0.3f), lThigh = V(-0.2f, -0.6f, 0.8f), lShin = V(-0.1f, -1, -0.6f) },
            // 5 무릎 미끄러지며: 낮게, 두 팔 앞으로 겹쳐
            new KataPose { rArm = V(0.06f, -0.2f, 1), lArm = V(-0.06f, -0.08f, 1), lean = 16, legs = true, hips = -0.15f,
                rThigh = V(0.15f, -0.55f, -0.85f), rShin = V(0.05f, -0.2f, -1), lThigh = V(-0.2f, -0.1f, 1), lShin = V(-0.1f, -1, 0.05f) },
            // 6 어깨 너머: 등진 채 두 팔을 어깨 너머 뒤로 (등진 상태에서 -z가 대상)
            new KataPose { rArm = V(0.35f, 0.35f, -1), lArm = V(0.05f, 0.25f, -1), twist = -20, headYaw = -70, legs = true,
                rThigh = V(0.25f, -1, 0.2f), rShin = V(0.15f, -1, 0), lThigh = V(-0.25f, -1, -0.15f), lShin = V(-0.1f, -1, 0), hips = -0.03f },
            // 7 회전: 두 팔 수평 양옆 (회전은 코드가 몸을 돌림)
            new KataPose { rArm = V(1, -0.12f, 0.1f), lArm = V(-1, -0.12f, 0.1f), side = -8, legs = true,
                rThigh = V(0.3f, -1, 0), rShin = V(0.2f, -1, 0), lThigh = V(-0.3f, -1, 0), lShin = V(-0.2f, -1, 0), hips = 0.05f },
        };
        public static readonly bool[] KataBackFacing = { false, false, true, false, false, false, true, false };

        void Awake()
        {
            if (!anim) anim = GetComponentInChildren<Animator>();
            if (anim && anim.isHuman)
            {
                hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                spine = anim.GetBoneTransform(HumanBodyBones.Spine);
                head = anim.GetBoneTransform(HumanBodyBones.Head);
                rUp = anim.GetBoneTransform(HumanBodyBones.RightUpperArm); rLo = anim.GetBoneTransform(HumanBodyBones.RightLowerArm); rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                lUp = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm); lLo = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm); lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                rTh = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg); rSh = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg); rFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                rIndex = anim.GetBoneTransform(HumanBodyBones.RightIndexProximal); rThumb = anim.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                lIndex = anim.GetBoneTransform(HumanBodyBones.LeftIndexProximal); lThumb = anim.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                lTh = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); lSh = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg); lFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                // 아바타에 손가락이 안 잡혀 있으면 (CatHero는 오른 엄지가 빠져 있음) 이름으로 찾음.
                // 엄지가 없으면 Roll이 아예 안 돌아 오른 아래팔 비틀림이 애니메이션 그대로 → 주먹과 총이 어긋났음
                if (!rThumb) rThumb = FindFinger(rHand, "Thumb");
                if (!lThumb) lThumb = FindFinger(lHand, "Thumb");
                if (!rIndex) rIndex = FindFinger(rHand, "Index");
                if (!lIndex) lIndex = FindFinger(lHand, "Index");
                scale = Mathf.Max(0.1f, (head.position - hips.position).magnitude / 0.36f);
                // Awake 시점 = 애니메이터가 돌기 전이라 뼈가 모델링 자세 그대로
                CaptureRest(rHand, out handBonesR, out handRestR);
                CaptureRest(lHand, out handBonesL, out handRestL);
                var torso = new System.Collections.Generic.List<Transform>();
                foreach (var b in new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder })
                {
                    var t = anim.GetBoneTransform(b);
                    if (t) torso.Add(t);
                }
                torsoBones = torso.ToArray();
                torsoRest = new Quaternion[torsoBones.Length];
                for (int i = 0; i < torsoBones.Length; i++) torsoRest[i] = torsoBones[i].localRotation;
            }
            from = to = cur = Aim;
            weight = weightTarget = aimWeight;
        }

        /// <summary>건카타 자세로 홱 (snap 초 동안)</summary>
        public void Snap(int kata, float snap = -1)
        {
            from = cur; to = Kata[Mathf.Clamp(kata, 0, Kata.Length - 1)]; t = 0;
            if (snap > 0) snapTime = snap;
            weightTarget = 1;
        }

        /// <summary>임의 자세로 (장전 동작 등). snap 초 동안</summary>
        public void Pose(KataPose p, float snap)
        {
            from = cur; to = p; t = 0; snapTime = snap;
            weightTarget = 1;
        }

        /// <summary>총을 손 안에서 더 돌림 (총 기준, 손잡이 원점). 장전 털기 · 돌려 쥐기용. identity = 그대로</summary>
        [System.NonSerialized] public Quaternion gunExtraR = Quaternion.identity, gunExtraL = Quaternion.identity;

        /// <summary>평소 조준 자세로 돌아옴</summary>
        public void Release(float snap = 0.12f)
        {
            from = cur; to = Aim; t = 0; snapTime = snap;
            weightTarget = aimWeight;
        }

        // ---------- 사격 반동 ----------
        const float RecoilArm = 20f, RecoilGun = 6f, RecoilDecay = 7f;     // 총은 팔과 거의 일직선으로 같이 올라감     // 팔이 위로 튀는 각 · 총구가 젖혀지는 각 · 돌아오는 속도(1/초)
        float recoilR, recoilL;

        /// <summary>쏠 때 호출: 그 손 팔이 위로 튀고 총구가 젖혀졌다가 0.15초쯤에 돌아옴</summary>
        public void Recoil(bool right, float power = 1f)
        {
            if (right) recoilR = Mathf.Max(recoilR, power); else recoilL = Mathf.Max(recoilL, power);
        }

        // 반동 곡선: 바로 튀고(위쪽) 빠르게 감쇠
        static float Curve(float r) => r * r * (3 - 2 * r);

        Vector3 Kick(Vector3 dir, float r)
        {
            if (r <= 0.001f) return dir;
            var axis = Vector3.Cross(dir, transform.up);
            if (axis.sqrMagnitude < 1e-4f) return dir;
            return Quaternion.AngleAxis(RecoilArm * Curve(r), axis.normalized) * dir;
        }

        // ---------- 몸 반동 ----------
        const float KickLean = 14f, KickHips = 0.035f, KickDecay = 6f;   // 상체가 뒤로 젖혀지는 각 · 골반이 꺼지는 높이 · 돌아오는 속도(1/초)
        float bodyKick;

        /// <summary>쏠 때 호출: 상체가 뒤로 확 젖혀지고 골반이 살짝 꺼졌다가 돌아옴</summary>
        public void BodyKick(float power = 1f) => bodyKick = Mathf.Max(bodyKick, Mathf.Clamp01(power));

        public void SetAimWeight(float w) { aimWeight = w; if (to.Equals(Aim) || IsAim) weightTarget = w; }
        bool IsAim => to.rArm == Aim.rArm && to.lArm == Aim.lArm;

        void LateUpdate()
        {
            if (!hips) return;
            float dt = Time.unscaledDeltaTime;
            if (t < 1) t = Mathf.Min(1, t + dt / Mathf.Max(0.01f, snapTime));
            float e = 1 - (1 - t) * (1 - t) * (1 - t);                  // 끝에서 딱 멈추는 감속
            cur = KataPose.Lerp(from, to, e);
            weight = Mathf.MoveTowards(weight, weightTarget, dt * 8f);
            if (weight <= 0.001f) { AlignGuns(); return; }

            var root = transform;
            Vector3 W(Vector3 local) => root.TransformDirection(local).normalized;

            // 몸 반동 (사격)
            bodyKick = Mathf.MoveTowards(bodyKick, 0, Time.deltaTime * KickDecay);
            float kick = Curve(bodyKick);
            // 상체는 좌우 대칭인 기본 자세로 (애니메이션 비틀림 지움) → 그 위에 자세의 비틀기 · 기울기
            RestoreRest(torsoBones, torsoRest, weight);
            // 기본 자세 자체도 살짝 기울어 있어(왼어깨가 7cm 높음) 어깨선을 몸 기준 수평 · 정면에 직각으로 맞춤 → 두 팔이 거울처럼 같은 자리에서 나옴
            if (spine && rUp && lUp)
            {
                var sv = lUp.position - rUp.position;
                if (sv.sqrMagnitude > 1e-6f)
                    spine.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(sv.normalized, -root.right), weight) * spine.rotation;
                // 어깨 가운데가 몸 가운데(루트)에서 옆으로 치우친 만큼 골반을 옮겨 가운데 맞춤 (애니메이션 골반이 3.5cm쯤 비껴 있음)
                float side = root.InverseTransformPoint((lUp.position + rUp.position) * 0.5f).x;
                hips.position -= root.right * side * weight;
            }
            // 골반 높이
            hips.position += root.up * (cur.hips - KickHips * kick) * scale * weight;
            // 허리 비틀기 · 기울기 (+ 반동으로 뒤로 젖힘)
            if (spine)
            {
                var q = Quaternion.AngleAxis(cur.twist * weight, root.up) * Quaternion.AngleAxis((cur.lean - KickLean * kick) * weight, root.right) * Quaternion.AngleAxis(-cur.side * weight, root.forward);
                spine.rotation = q * spine.rotation;
            }
            if (head && Mathf.Abs(cur.headYaw) > 0.1f) head.rotation = Quaternion.AngleAxis(cur.headYaw * 0.6f * weight, root.up) * head.rotation;

            // 다리
            if (cur.legs && to.legs)
            {
                Point(rTh, rSh, W(cur.rThigh), weight); Point(rSh, rFoot, W(cur.rShin), weight);
                Point(lTh, lSh, W(cur.lThigh), weight); Point(lSh, lFoot, W(cur.lShin), weight);
            }
            // 팔 (위팔 → 아래팔 → 손을 같은 방향으로 쭉)
            // 반동: 쏜 팔이 위로 튀었다 돌아옴
            float dt2 = Time.deltaTime;
            recoilR = Mathf.MoveTowards(recoilR, 0, dt2 * RecoilDecay); recoilL = Mathf.MoveTowards(recoilL, 0, dt2 * RecoilDecay);
            var ra = Kick(W(cur.rArm), recoilR); var la = Kick(W(cur.lArm), recoilL);
            Point(rUp, rLo, ra, weight); Point(rLo, rHand, ra, weight);
            Point(lUp, lLo, la, weight); Point(lLo, lHand, la, weight);
            // 손목 · 손가락은 모델링 자세로 (애니메이션마다 달라지면 아래 Roll 각과 주먹 자리가 흔들림)
            RestoreRest(handBonesR, handRestR, weight);
            RestoreRest(handBonesL, handRestL, weight);
            // 아래팔을 팔 축으로 비틀어 엄지(= 총 윗면)가 위를 향하게
            Roll(rLo, rHand, rIndex, rThumb, weight);
            Roll(lLo, lHand, lIndex, lThumb, weight);
            AlignGuns();
        }

        /// <summary>손 아래에서 이름에 key(Thumb · Index)가 든 첫 마디 뼈 (손 바로 아래 자식 우선)</summary>
        static Transform FindFinger(Transform hand, string key)
        {
            if (!hand) return null;
            foreach (Transform c in hand) if (c.name.Contains(key)) return c;
            foreach (var c in hand.GetComponentsInChildren<Transform>(true)) if (c != hand && c.name.Contains(key)) return c;
            return null;
        }

        static void CaptureRest(Transform hand, out Transform[] bones, out Quaternion[] rest)
        {
            bones = hand ? hand.GetComponentsInChildren<Transform>(true) : new Transform[0];
            rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) rest[i] = bones[i].localRotation;
        }

        static void RestoreRest(Transform[] bones, Quaternion[] rest, float w)
        {
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i]) bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, rest[i], w);
        }

        static void Point(Transform bone, Transform child, Vector3 dir, float w)
        {
            if (!bone || !child || dir.sqrMagnitude < 0.001f) return;
            var curDir = child.position - bone.position;
            if (curDir.sqrMagnitude < 1e-6f) return;
            var q = Quaternion.FromToRotation(curDir.normalized, dir);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, q, w) * bone.rotation;
        }

        static Vector3 Fist(Transform hand, Transform index) => index ? hand.position + (index.position - hand.position) * 0.55f : hand.position;

        /// <summary>팔 방향에 맞는 '총 윗면' 방향: 캐릭터 위쪽 (팔이 수직이면 앞 · 뒤)</summary>
        Vector3 DesiredUp(Vector3 d)
        {
            var up = transform.up - d * Vector3.Dot(transform.up, d);
            if (up.sqrMagnitude < 0.05f) up = d.y > 0 ? -transform.forward : transform.forward;
            return up.normalized;
        }

        void Roll(Transform lo, Transform hand, Transform index, Transform thumb, float w)
        {
            if (!lo || !hand || !thumb) return;
            var d = (hand.position - lo.position).normalized;
            var fist = Fist(hand, index);
            var cur = thumb.position - fist;
            cur -= d * Vector3.Dot(cur, d);
            if (cur.sqrMagnitude < 1e-8f) return;
            float ang = Vector3.SignedAngle(cur, DesiredUp(d), d);
            lo.rotation = Quaternion.AngleAxis(ang * w, d) * lo.rotation;
        }

        void AlignGuns()
        {
            Align(gunR, rLo, rHand, rIndex, rThumb);
            Align(gunL, lLo, lHand, lIndex, lThumb);
            if (gunR) gunR.rotation *= gunExtraR * Quaternion.Euler(-RecoilGun * Curve(recoilR), 0, 0);
            if (gunL) gunL.rotation *= gunExtraL * Quaternion.Euler(-RecoilGun * Curve(recoilL), 0, 0);
        }

        // 총 원점 = 손잡이 (주먹 안), +z = 총구, +y = 윗면(엄지 쪽)
        void Align(Transform gun, Transform lo, Transform hand, Transform index, Transform thumb)
        {
            if (!gun || !lo || !hand) return;
            var d = hand.position - lo.position;
            if (d.sqrMagnitude < 1e-6f) return;
            d.Normalize();
            var fist = Fist(hand, index);
            Vector3 up = thumb ? thumb.position - fist : transform.up;
            up -= d * Vector3.Dot(up, d);
            if (up.sqrMagnitude < 1e-6f) up = DesiredUp(d);
            var rot = Quaternion.LookRotation(d, up.normalized);
            // 손잡이가 주먹 안에 오도록 총을 총 기준으로 옮김 (위 · 아래 = y, 앞뒤 = z, 모델 크기 비례)
            gun.SetPositionAndRotation(fist + rot * gripOffset * scale, rot);
        }

        /// <summary>그 총의 총구가 가리키는 수평 방향</summary>
        public Vector3 BarrelDir(bool right)
        {
            var g = right ? gunR : gunL;
            if (!g) return transform.forward;
            var d = g.forward; d.y = 0;
            return d.sqrMagnitude > 0.001f ? d.normalized : transform.forward;
        }
    }
}
