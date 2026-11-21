using UnityEngine;

namespace CatTang
{
    // Owns only the brief stop it requested, so pausing or changing scenes stays safe.
    public sealed class CombatHitStop : MonoBehaviour
    {
        const float HoldScale = 0.065f;
        static CombatHitStop active;
        static float availableAt;
        float until, originalScale;

        public static void Play(float duration)
        {
            float now = Time.unscaledTime;
            if (active || duration <= 0f || now < availableAt || Time.timeScale < 0.99f) return;

            var stop = new GameObject("CombatHitStop").AddComponent<CombatHitStop>();
            active = stop;
            stop.originalScale = Time.timeScale;
            stop.until = now + Mathf.Clamp(duration, 0.02f, 0.08f);
            availableAt = stop.until + 0.06f;
            Time.timeScale = HoldScale;
        }

        void Update()
        {
            if (Time.unscaledTime < until && Mathf.Approximately(Time.timeScale, HoldScale)) return;
            Restore();
            Destroy(gameObject);
        }

        void OnDestroy() => Restore();

        void Restore()
        {
            if (active != this) return;
            active = null;
            if (Mathf.Approximately(Time.timeScale, HoldScale)) Time.timeScale = originalScale;
        }
    }
}

