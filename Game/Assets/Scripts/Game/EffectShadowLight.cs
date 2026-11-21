using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    [DefaultExecutionOrder(60)]
    internal sealed class EffectShadowLight : MonoBehaviour
    {
        // Point-light shadows use six faces, so reserve them for the strongest active flashes.
        static readonly EffectShadowLight[] slots = new EffectShadowLight[2];
        static GameObject lightPrefab;
        Light source;
        Transform anchor;
        Vector3 localPoint;
        float born, duration, peak;

        public static void Flash(Vector3 position, Color color, float range, float intensity, float duration)
            => Play(position, color, range, intensity, duration, null, Vector3.zero);

        public static void Blade(Transform weapon, Vector3 grip, Vector3 tip, Color color, float duration)
        {
            if (!weapon) return;
            float length = weapon.TransformVector(tip - grip).magnitude;
            var point = Vector3.Lerp(grip, tip, 0.62f);
            Play(weapon.TransformPoint(point) + Vector3.up * 0.27f, color,
                Mathf.Clamp(length * 1.3f + 2.2f, 3f, 5.5f), 4.6f, duration, weapon, point);
        }

        public static void Projectile(Transform body, Color color, float duration = 0.19f)
        {
            if (!body) return;
            Play(body.position + Vector3.up * 0.27f, color, 2.9f, 3.1f, duration, body, Vector3.zero);
        }

        static void Play(Vector3 position, Color color, float range, float intensity, float duration, Transform anchor, Vector3 localPoint)
        {
            if (!Camera.main || duration <= 0f) return;
            int index = 0;
            float weakest = float.PositiveInfinity;
            for (int i = 0; i < slots.Length; i++)
            {
                var candidate = slots[i];
                float strength = candidate && candidate.source.enabled ? candidate.source.intensity * candidate.source.range : 0f;
                if (strength >= weakest) continue;
                weakest = strength;
                index = i;
            }
            if (weakest > intensity * range) return;

            var effect = slots[index];
            if (!effect)
            {
                if (!lightPrefab) lightPrefab = Resources.Load<GameObject>("FX/EffectShadowPoint");
                if (!lightPrefab) return;
                var go = Instantiate(lightPrefab);
                go.name = "Effect Shadow Light";
                effect = slots[index] = go.AddComponent<EffectShadowLight>();
                effect.source = go.GetComponent<Light>();
            }

            effect.transform.position = position;
            effect.anchor = anchor;
            effect.localPoint = localPoint;
            effect.peak = intensity;
            effect.born = Time.time;
            effect.duration = duration;
            color /= Mathf.Max(1f, color.maxColorComponent);
            color.a = 1f;
            effect.source.color = color;
            effect.source.range = range;
            effect.source.shadows = UniversalRenderPipeline.asset && UniversalRenderPipeline.asset.supportsAdditionalLightShadows
                ? LightShadows.Soft : LightShadows.None;
            effect.source.intensity = intensity * 0.68f;
            effect.source.enabled = true;
        }

        void LateUpdate()
        {
            if (!source || !source.enabled) return;
            float age = (Time.time - born) / duration;
            if (age >= 1f)
            {
                source.enabled = false;
                anchor = null;
                return;
            }
            if (anchor && !anchor.gameObject.activeInHierarchy)
            {
                source.enabled = false;
                anchor = null;
                return;
            }
            if (anchor) transform.position = anchor.TransformPoint(localPoint) + Vector3.up * 0.27f;
            float pulse = Mathf.Lerp(0.68f, 1f, Mathf.Clamp01(age / 0.14f));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, age));
            source.intensity = peak * pulse * fade;
        }
    }
}
