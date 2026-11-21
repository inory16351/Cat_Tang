using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        EnemyShotPose shotPose;

        EnemyShotPose ShotPose()
        {
            if (!anim || !anim.isHuman) return null;
            if (!shotPose) shotPose = gameObject.AddComponent<EnemyShotPose>();
            shotPose.Bind(anim, health);
            return shotPose;
        }

        void BraceShot(float duration, EnemyShotPose.Style style) => ShotPose()?.Aim(duration, style);

        void ReleaseShot(Vector3 direction, EnemyShotPose.Style style, float strength = 1f, bool leftHand = false)
            => ShotPose()?.Release(direction, style, strength, leftHand);
    }

    // Layers the force of the shot on top of the weapon-specific animation at the actual release frame.
    [DefaultExecutionOrder(30)]
    internal sealed class EnemyShotPose : MonoBehaviour
    {
        internal enum Style { Sidearm, Rifle, Cast, Throw }

        struct Bone
        {
            readonly Transform bone;
            Quaternion basis, applied;
            bool changed;

            public Bone(Transform bone) { this.bone = bone; basis = applied = Quaternion.identity; changed = false; }

            public void Pitch(Vector3 axis, float degrees)
            {
                if (!bone || Mathf.Abs(degrees) < 0.01f) return;
                basis = bone.localRotation;
                bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
                applied = bone.localRotation;
                changed = true;
            }

            public void Restore()
            {
                if (changed && bone && Mathf.Abs(Quaternion.Dot(bone.localRotation, applied)) > 0.999999f)
                    bone.localRotation = basis;
                changed = false;
            }
        }

        Animator animator;
        Health health;
        Bone hips, chest, rightArm, rightFore, leftArm, leftFore;
        float aimAt, aimFor, firedAt = -1f, strength = 1f;
        Vector3 shotDirection;
        bool leftHand;
        Style style;

        internal void Bind(Animator source, Health owner)
        {
            if (animator == source) return;
            Restore();
            animator = source;
            health = owner;
            hips = Get(HumanBodyBones.Hips);
            var torso = source.GetBoneTransform(HumanBodyBones.Chest);
            chest = new Bone(torso ? torso : source.GetBoneTransform(HumanBodyBones.Spine));
            rightArm = Get(HumanBodyBones.RightUpperArm);
            rightFore = Get(HumanBodyBones.RightLowerArm);
            leftArm = Get(HumanBodyBones.LeftUpperArm);
            leftFore = Get(HumanBodyBones.LeftLowerArm);

            Bone Get(HumanBodyBones id) => new(source.GetBoneTransform(id));
        }

        internal void Aim(float duration, Style style)
        {
            aimAt = Time.time;
            aimFor = Mathf.Max(0f, duration);
            firedAt = -1f;
            this.style = style;
            enabled = true;
        }

        internal void Release(Vector3 direction, Style style, float strength, bool leftHand)
        {
            aimFor = 0f;
            firedAt = Time.time;
            shotDirection = Effects.Flat(direction).normalized;
            this.style = style;
            this.strength = strength;
            this.leftHand = leftHand;
            enabled = true;
        }

        void Update() => Restore();

        void Restore()
        {
            hips.Restore(); chest.Restore();
            rightArm.Restore(); rightFore.Restore();
            leftArm.Restore(); leftFore.Restore();
        }

        void OnDisable() => Restore();

        void LateUpdate()
        {
            float now = Time.time;
            float sinceFire = now - firedAt;
            bool aiming = aimFor > 0f && now < aimAt + aimFor + 0.04f;
            bool fired = firedAt >= 0f && sinceFire < 0.28f;
            if (!health || !health.Alive || health.Stunned || !animator || (!aiming && !fired))
            {
                enabled = false;
                return;
            }

            var direction = fired && shotDirection.sqrMagnitude > 0.001f ? shotDirection : transform.forward;
            var side = Vector3.Cross(Vector3.up, direction);
            float tension = aiming ? WeaponSwingRig.Ease((now - aimAt) / Mathf.Max(0.08f, aimFor * 0.72f)) : 0f;
            float kick = fired ? Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(sinceFire / 0.024f))
                * (1f - WeaponSwingRig.Ease((sinceFire - 0.028f) / 0.115f)) : 0f;
            float settle = fired ? Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0.09f, 0.26f, sinceFire)) : 0f;
            float impulse = (kick - settle * 0.23f) * strength;
            bool cast = style == Style.Cast || style == Style.Throw;
            float torso = (cast ? -3.8f : 5.4f) * tension + (cast ? 7f : -7.5f) * impulse;
            float shoulder = (cast ? 9.2f : style == Style.Rifle ? -8.5f : -13.5f) * impulse;
            float forearm = (cast ? 5f : -6.0f) * impulse;
            hips.Pitch(side, torso * 0.26f);
            chest.Pitch(side, torso * 0.74f);
            if (fired && leftHand)
            {
                leftArm.Pitch(side, shoulder);
                leftFore.Pitch(side, forearm);
            }
            else
            {
                rightArm.Pitch(side, shoulder);
                rightFore.Pitch(side, forearm);
            }
            if (style == Style.Rifle && !leftHand) leftArm.Pitch(side, shoulder * 0.52f);
        }
    }
}
