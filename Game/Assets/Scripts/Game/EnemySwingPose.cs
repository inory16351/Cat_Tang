using System;
using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        EnemySwingPose swingPose;

        bool BraceSwing(float wind, float recovery, float twist = 12f, bool overhead = false,
                        bool pivot = false, Action light = null, float amplitude = 1f,
                        HeldStrikePose.Style heldStyle = HeldStrikePose.Style.None, Transform held = null, SkinnedMeshRenderer heldSkin = null)
        {
            if (!anim || !anim.isHuman) return false;
            if (!swingPose) swingPose = gameObject.AddComponent<EnemySwingPose>();
            return swingPose.Begin(anim, health, wind / AtkMul, recovery, twist, overhead, pivot, light, amplitude, heldStyle, held, heldSkin);
        }
    }

    // Layers stance and weight transfer onto each existing humanoid attack clip.
    [DefaultExecutionOrder(25)]
    public sealed class EnemySwingPose : MonoBehaviour
    {
        Animator animator;
        Health health;
        Transform hips, spine, chest, rigRoot;
        WeaponSwingRig.Leg lead, rear;
        Vector3 hipBase, hipApplied, origin, forward, right;
        bool offsetApplied, pending, overhead, pivot, thrust, thrustPlantPending;
        Vector3 thrustPlantOrigin;
        float start, wind, recovery, twist, amplitude, lastHeading, turn;
        Action strikeGlow;
        readonly HeldStrikePose heldPose = new();

        internal bool PendulumWeapon(out Transform hand, out Vector3 grip, out Vector3 end)
            => heldPose.PendulumWeapon(out hand, out grip, out end);

        internal bool Begin(Animator an, Health owner, float wind, float recovery, float twist,
                            bool overhead, bool pivot, Action light, float amplitude,
                            HeldStrikePose.Style heldStyle, Transform held, SkinnedMeshRenderer heldSkin)
        {
            RestoreHip();
            if (animator != an)
            {
                animator = an;
                hips = an.GetBoneTransform(HumanBodyBones.Hips);
                spine = an.GetBoneTransform(HumanBodyBones.Spine);
                chest = an.GetBoneTransform(HumanBodyBones.Chest) ?? an.GetBoneTransform(HumanBodyBones.UpperChest);
                rigRoot = an.transform.parent ? an.transform.parent : an.transform;
            }
            if (!hips) return false;

            bool rightFoot = twist >= 0f;
            var left = new WeaponSwingRig.Leg(an, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            var rightLeg = new WeaponSwingRig.Leg(an, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            lead = rightFoot ? rightLeg : left;
            rear = rightFoot ? left : rightLeg;
            this.health = owner;
            this.wind = Mathf.Max(0f, wind);
            this.recovery = Mathf.Max(0.12f, recovery);
            this.twist = twist;
            this.amplitude = amplitude;
            this.overhead = overhead;
            this.pivot = pivot;
            thrust = heldStyle == HeldStrikePose.Style.Thrust || heldStyle == HeldStrikePose.Style.BackThrust;
            thrustPlantPending = thrust;
            heldPose.Begin(an, heldStyle, held, heldSkin);
            strikeGlow = light;
            start = Time.time;
            pending = true;
            enabled = true;
            return true;
        }

        void Update() => RestoreHip();
        void OnDisable() { RestoreHip(); heldPose.Restore(); strikeGlow = null; }

        void RestoreHip()
        {
            if (!offsetApplied || !hips) return;
            if ((hips.localPosition - hipApplied).sqrMagnitude < 1e-8f) hips.localPosition = hipBase;
            offsetApplied = false;
        }

        void LateUpdate()
        {
            float elapsed = Time.time - start;
            if (!hips || !health || !health.Alive || health.Stunned || elapsed >= wind + recovery)
            {
                enabled = false;
                return;
            }

            bool hasLegs = lead.Valid && rear.Valid;
            if (pending)
            {
                origin = transform.position;
                forward = Effects.Flat(transform.forward).normalized;
                right = Vector3.Cross(Vector3.up, forward);
                if (hasLegs) { lead.Plant(forward); rear.Plant(forward); }
                lastHeading = rigRoot.eulerAngles.y;
                turn = 0f;
                pending = false;
            }

            float back = WeaponSwingRig.Ease(elapsed / Mathf.Max(0.08f, wind * 0.75f));
            float pushWindow = Mathf.Clamp(wind * 0.28f, 0.08f, 0.18f);
            float push = WeaponSwingRig.Heavy((elapsed - wind + pushWindow) / (pushWindow + 0.055f));
            float finish = WeaponSwingRig.Ease((elapsed - wind) / recovery);
            float strength = WeaponSwingRig.Ease(elapsed / 0.085f) * (1 - finish);
            float transfer = Mathf.Lerp(-0.60f * back, 1f, push);
            float side = Mathf.Sign(twist);
            float length = hasLegs ? Mathf.Min(lead.length, rear.length) : 1f;
            hipBase = hips.localPosition;
            hips.position += strength * length * amplitude * (forward * transfer * 0.10f
                + right * transfer * side * (overhead ? 0.03f : 0.085f)
                - Vector3.up * (0.05f * back + (overhead ? 0.09f : 0.04f) * push));
            hipApplied = hips.localPosition;
            offsetApplied = true;

            float yaw = strength * transfer * twist;
            float pitch = strength * amplitude * Mathf.Lerp(overhead ? -7f : -3f, thrust ? 16f : overhead ? 11f : 6f, push);
            if (spine) Twist(spine, yaw * 0.55f, pitch * 0.55f);
            if (chest && chest != spine) Twist(chest, yaw * 0.45f, pitch * 0.45f);

            // An attack can be interrupted by a knockback before the next animator state.
            if (hasLegs && thrust && elapsed >= wind)
            {
                if (thrustPlantPending)
                {
                    lead.Plant(forward);
                    rear.Plant(forward);
                    thrustPlantOrigin = transform.position;
                    thrustPlantPending = false;
                }
                var step = Effects.Flat(transform.position - thrustPlantOrigin);
                float stride = WeaponSwingRig.Ease((elapsed - wind) / 0.12f);
                lead.Pose(step + forward * length * 0.28f * stride, Mathf.Sin(stride * Mathf.PI) * length * 0.12f, 0f, strength);
                rear.Pose(step - forward * length * 0.34f * stride, 0f, side * 12f * stride, strength);
            }
            else if (hasLegs && Effects.Flat(transform.position - origin).sqrMagnitude < length * length * 0.36f)
            {
                float heading = rigRoot.eulerAngles.y;
                turn += Mathf.DeltaAngle(lastHeading, heading);
                lastHeading = heading;
                if (pivot)
                {
                    lead.Turn(origin, transform.position, turn, true, strength);
                    rear.Turn(origin, transform.position, turn, false, strength);
                }
                else
                {
                    Vector3 step = (forward * 0.20f + right * side * 0.075f) * (length * amplitude);
                    lead.Pose(step * push, Mathf.Sin(push * Mathf.PI) * length * 0.105f, side * 10f * push, strength);
                    rear.Pose(Vector3.zero, 0f, side * 16f * push, strength);
                }
            }

            heldPose.Apply(elapsed, wind, recovery, origin, forward, right);
            if (strikeGlow != null && elapsed >= wind - pushWindow)
            {
                var begin = strikeGlow;
                strikeGlow = null;
                begin();
            }
        }

        void Twist(Transform bone, float yaw, float pitch)
        {
            bone.rotation = Quaternion.AngleAxis(yaw, Vector3.up)
                          * Quaternion.AngleAxis(pitch, right) * bone.rotation;
        }
    }
}

