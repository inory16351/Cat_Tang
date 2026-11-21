using UnityEngine;

namespace CatTang
{
    // Adds weapon-specific grips and contact arcs to the existing humanoid stance.
    internal sealed class HeldStrikePose
    {
        internal enum Style { None, Pendulum, Flail, Trunk, TrunkBash, Thrust, BackThrust }

        internal static float BackThrustTurn(float phase)
            => WeaponSwingRig.Heavy(Mathf.InverseLerp(0.46f, 0.74f, phase));

        struct Arm
        {
            public Transform upper, fore, hand;
            public float length;
            public bool Valid => upper && fore && hand && length > 0.01f;

            public Arm(Animator animator, bool right)
            {
                upper = animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
                fore = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
                hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                length = upper && fore && hand ? Vector3.Distance(upper.position, fore.position)
                                                  + Vector3.Distance(fore.position, hand.position) : 0f;
            }

            public void Reach(Vector3 at, Vector3 outward, float weight)
            {
                if (!Valid) return;
                var pole = upper.position + (outward - Vector3.up * 0.42f) * length;
                WeaponSwingRig.TwoBone(upper, fore, hand, Vector3.Lerp(hand.position, at, weight), pole);
            }

            public void Aim(Vector3 localAxis, Vector3 worldAxis, float weight)
            {
                if (!Valid) return;
                var correction = Quaternion.FromToRotation(hand.TransformDirection(localAxis), worldAxis);
                hand.rotation = Quaternion.Slerp(Quaternion.identity, correction, weight) * hand.rotation;
            }
        }

        Style style;
        Arm left, right;
        Transform prop;
        Vector3 trunkRestPosition, leftGrip, rightGrip, handAxis, maceGrip;
        Quaternion trunkRestRotation, leftGripRotation, rightGripRotation;
        readonly Vector3[] trunkCorners = new Vector3[8];

        public void Begin(Animator animator, Style style, Transform held, SkinnedMeshRenderer skin)
        {
            Restore();
            this.style = style;
            if (style == Style.None) return;

            left = new Arm(animator, false);
            right = new Arm(animator, true);
            if (style == Style.Trunk || style == Style.TrunkBash)
            {
                if (!held || !held.parent || !skin || !skin.sharedMesh || !left.Valid || !right.Valid) { this.style = Style.None; return; }
                var mesh = skin.sharedMesh;
                int bone = System.Array.IndexOf(skin.bones, held);
                var binds = mesh.bindposes;
                if (bone < 0 || bone >= binds.Length) { this.style = Style.None; return; }
                var bounds = mesh.GetSubMesh(0).bounds;
                for (int i = 0; i < trunkCorners.Length; i++)
                {
                    var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                    trunkCorners[i] = binds[bone].MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, sign));
                }
                prop = held;
                trunkRestPosition = held.localPosition;
                trunkRestRotation = held.localRotation;
                leftGrip = held.InverseTransformPoint(left.hand.position);
                rightGrip = held.InverseTransformPoint(right.hand.position);
                leftGripRotation = Quaternion.Inverse(held.rotation) * left.hand.rotation;
                rightGripRotation = Quaternion.Inverse(held.rotation) * right.hand.rotation;
            }
            else if ((style == Style.Thrust || style == Style.BackThrust) && right.Valid)
            {
                handAxis = right.hand.InverseTransformDirection(held ? held.up : right.hand.position - right.fore.position);
            }
            else if (left.Valid)
            {
                var along = left.hand.position - left.fore.position;
                var thumb = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                var side = thumb ? Vector3.ProjectOnPlane(thumb.position - left.hand.position, along).normalized : animator.transform.forward;
                var bend = Vector3.Cross(along.normalized, side);
                var head = style == Style.Flail ? new Vector3(2.7f, 1.4f, 0.72f) : new Vector3(2.25f, 0.56f, 1.50f);
                var handle = along * 0.27f;
                var tip = along * head.x + along.magnitude * (side * head.y + bend * head.z);
                maceGrip = left.hand.InverseTransformVector(handle);
                handAxis = left.hand.InverseTransformVector(tip - handle);
            }
        }

        internal bool PendulumWeapon(out Transform hand, out Vector3 grip, out Vector3 end)
        {
            hand = left.hand;
            grip = maceGrip;
            end = grip + handAxis;
            return (style == Style.Pendulum || style == Style.Flail) && left.Valid;
        }

        public void Restore()
        {
            if (prop)
            {
                prop.localPosition = trunkRestPosition;
                prop.localRotation = trunkRestRotation;
            }
            prop = null;
            style = Style.None;
        }

        public void Apply(float elapsed, float wind, float recovery, Vector3 origin, Vector3 forward, Vector3 side)
        {
            if (style == Style.None) return;
            float u = Mathf.Clamp01(elapsed / Mathf.Max(wind, 0.045f));
            float finish = WeaponSwingRig.Ease((elapsed - wind - recovery * 0.20f) / (recovery * 0.80f));
            float weight = WeaponSwingRig.Ease(elapsed / 0.10f) * (1f - finish);
            float lift = WeaponSwingRig.Ease(u / 0.67f);
            float release = style == Style.Flail || style == Style.BackThrust ? 0.77f : 0.70f;
            float fall = Mathf.InverseLerp(release, 1f, u);
            float travel = fall * fall * (0.42f + 0.58f * fall);
            if (style == Style.Trunk || style == Style.TrunkBash)
                PoseTrunk(origin.y, forward, side, lift, travel, weight);
            else if (style == Style.Thrust || style == Style.BackThrust)
                PoseThrust(forward, side, u, lift, travel, weight);
            else if (left.Valid)
                PosePendulum(forward, side, lift, travel, weight);
        }

        void PoseTrunk(float floor, Vector3 forward, Vector3 side, float lift, float travel, float weight)
        {
            if (!prop || !prop.parent) return;
            var home = prop.parent.TransformPoint(trunkRestPosition);
            var homeRotation = prop.parent.rotation * trunkRestRotation;
            float reach = Mathf.Min(left.length, right.length);
            bool slam = style == Style.Trunk;
            var raised = home + (slam ? Vector3.up * 0.82f + forward * 0.12f
                                      : Vector3.up * 0.12f - forward * 0.19f) * (reach * lift);
            var hit = home + forward * reach * (slam ? 0.32f : 0.42f);
            var hitRotation = Quaternion.AngleAxis(slam ? 28f : 7f, side) * homeRotation;
            if (slam)
            {
                float bottom = float.MaxValue;
                var scale = prop.lossyScale;
                foreach (var corner in trunkCorners)
                    bottom = Mathf.Min(bottom, (hitRotation * Vector3.Scale(corner, scale)).y);
                hit.y = floor + 0.035f - bottom;
            }
            var liftRotation = Quaternion.AngleAxis((slam ? -21f : -8f) * lift, side) * homeRotation;
            var pose = Vector3.Lerp(home, Vector3.Lerp(raised, hit, travel), weight);
            var rotation = Quaternion.Slerp(homeRotation, Quaternion.Slerp(liftRotation, hitRotation, travel), weight);
            prop.SetPositionAndRotation(pose, rotation);
            // Both wrists stay on the handles. The existing idle grip takes over during recovery.
            float gripWeight = Mathf.Clamp01(weight * 2f);
            left.Reach(prop.TransformPoint(leftGrip), -side, gripWeight);
            right.Reach(prop.TransformPoint(rightGrip), side, gripWeight);
            left.hand.rotation = Quaternion.Slerp(left.hand.rotation, prop.rotation * leftGripRotation, weight);
            right.hand.rotation = Quaternion.Slerp(right.hand.rotation, prop.rotation * rightGripRotation, weight);
        }

        void PosePendulum(Vector3 forward, Vector3 side, float lift, float travel, float weight)
        {
            var raised = (Vector3.up * 0.78f - forward * 0.24f - side * 0.61f).normalized;
            var planted = (Vector3.down * 0.74f + forward * 0.61f - side * 0.27f).normalized;
            var direction = Vector3.Slerp(raised, planted, travel);
            left.Reach(left.upper.position + direction * left.length * Mathf.Lerp(0.74f, 0.97f, travel), -side, weight * lift);
            var head = Vector3.Slerp((Vector3.up * 0.83f - forward * 0.29f - side * 0.43f).normalized,
                (Vector3.down * 0.84f + forward * 0.54f).normalized, travel);
            left.Aim(handAxis, head, weight * lift * 0.94f);
            if (right.Valid)
                right.Reach(right.upper.position + (side * 0.64f + Vector3.down * 0.68f - forward * 0.22f) * right.length,
                    side, weight * lift * 0.80f);
        }

        void PoseThrust(Vector3 forward, Vector3 side, float phase, float lift, float travel, float weight)
        {
            if (!right.Valid) return;
            if (style == Style.BackThrust)
            {
                var turn = Quaternion.AngleAxis(Mathf.Lerp(-180f, 0f, BackThrustTurn(phase)), Vector3.up);
                forward = turn * forward;
                side = turn * side;
            }
            var handTarget = forward * Mathf.Lerp(-0.49f, 0.95f, travel) - side * 0.14f - Vector3.up * 0.20f;
            right.Reach(right.upper.position + handTarget * right.length, side * 0.55f, weight * lift);
            right.Aim(handAxis, forward, weight * lift);
            if (left.Valid)
            {
                var counter = (-forward * Mathf.Lerp(0.14f, 0.62f, travel) - side * 0.30f - Vector3.up * 0.42f) * left.length;
                left.Reach(left.upper.position + counter, -side, weight * lift);
            }
        }
    }
}

