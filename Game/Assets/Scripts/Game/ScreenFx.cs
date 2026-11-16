// 화면 연출 (URP 전역 볼륨을 런타임에 만들어 씀)
// - PerfectDodge: 극한 회피(닷지 액션) — 젠레스 존 제로식. 순간 파란 섬광 → 화면이 회색으로 빠지고(약간 차갑게) 가장자리 색수차 · 비네트,
//   슬로우가 풀리며 색이 돌아옴
// - PlayerHit: 고양이 피격 — 순간 하얗게 번쩍 → 붉은 비네트 · 색수차 · 렌즈가 툭 튐, 0.4초에 걸쳐 빠짐 (+ 아주 짧은 히트스톱)
// 시간은 실제 시간(unscaled)으로 돌림 (슬로우 · 히트스톱 중에도 일정)
// 플레이 진입 시 도메인 리로드를 끈 프로젝트라 정적 참조가 판을 넘어 남음 → 파괴됐으면 다시 만듦
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    public class ScreenFx : MonoBehaviour
    {
        static ScreenFx inst;

        class Layer
        {
            public Volume vol;
            public ColorAdjustments color;
            public ChromaticAberration chroma;
            public Vignette vig;
            public LensDistortion lens;
            public float start = -99;
        }

        Layer dodge, hit;
        float dodgeHold, dodgeRecover, hitPower;

        static ScreenFx Get()
        {
            if (inst) return inst;
            var go = new GameObject("ScreenFx");
            inst = go.AddComponent<ScreenFx>();
            inst.dodge = inst.Make(100, new Color(0.05f, 0.08f, 0.2f));
            inst.hit = inst.Make(101, new Color(0.45f, 0.02f, 0.03f));
            return inst;
        }

        Layer Make(int priority, Color vignette)
        {
            var l = new Layer();
            l.vol = gameObject.AddComponent<Volume>();
            l.vol.isGlobal = true;
            l.vol.priority = priority;
            l.vol.weight = 0;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            l.color = prof.Add<ColorAdjustments>(true);
            l.chroma = prof.Add<ChromaticAberration>(true);
            l.vig = prof.Add<Vignette>(true);
            l.lens = prof.Add<LensDistortion>(true);
            l.vig.color.Override(vignette);
            l.vig.smoothness.Override(0.45f);
            l.vol.profile = prof;
            return l;
        }

        /// <summary>카메라 후처리를 켬 (Run 씬 카메라는 꺼져 있음). 볼륨이 비어 있으면 화면은 그대로</summary>
        static void EnablePost()
        {
            var cam = Camera.main;
            if (!cam) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data && !data.renderPostProcessing) data.renderPostProcessing = true;
        }

        /// <summary>극한 회피 화면: hold = 회색 유지(실제 초), recover = 색이 돌아오는 시간</summary>
        public static void PerfectDodge(float hold, float recover)
        {
            EnablePost();
            var s = Get();
            s.dodge.start = Time.unscaledTime; s.dodgeHold = hold; s.dodgeRecover = recover;
            s.enabled = true;
        }

        /// <summary>고양이 피격 화면: power 0~1 (피해가 클수록 세게)</summary>
        public static void PlayerHit(float power)
        {
            EnablePost();
            var s = Get();
            s.hit.start = Time.unscaledTime; s.hitPower = Mathf.Clamp(power, 0.4f, 1f);
            s.enabled = true;
        }

        void Update()
        {
            bool any = false;
            // 극한 회피
            {
                float t = Time.unscaledTime - dodge.start, total = dodgeHold + dodgeRecover;
                if (t <= total + 0.05f)
                {
                    any = true;
                    float w = t < 0.04f ? t / 0.04f : t < dodgeHold ? 1f : 1f - Mathf.SmoothStep(0, 1, (t - dodgeHold) / Mathf.Max(0.01f, dodgeRecover));
                    dodge.vol.weight = Mathf.Clamp01(w);
                    float flash = Mathf.Clamp01(1f - t / 0.12f);
                    dodge.color.postExposure.Override(Mathf.Lerp(-0.15f, 1.3f, flash));
                    dodge.color.saturation.Override(Mathf.Lerp(-82f, -20f, flash));
                    dodge.color.contrast.Override(18f);
                    dodge.color.colorFilter.Override(Color.Lerp(new Color(0.86f, 0.92f, 1f), new Color(0.7f, 0.85f, 1f), flash));
                    dodge.chroma.intensity.Override(Mathf.Lerp(0.35f, 1f, flash));
                    dodge.vig.intensity.Override(0.38f);
                    dodge.lens.intensity.Override(Mathf.Lerp(-0.08f, -0.28f, flash));
                }
                else dodge.vol.weight = 0;
            }
            // 피격
            {
                const float Life = 0.42f;
                float t = Time.unscaledTime - hit.start;
                if (t <= Life)
                {
                    any = true;
                    float k = 1f - t / Life;                       // 1 → 0
                    float pop = Mathf.Clamp01(1f - t / 0.06f);     // 첫 0.06초 번쩍
                    hit.vol.weight = Mathf.Clamp01(k * 1.4f) * hitPower;
                    hit.color.postExposure.Override(Mathf.Lerp(0f, 0.9f, pop));
                    hit.color.saturation.Override(-25f * k);
                    hit.color.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, 0.82f, 0.82f), k));
                    hit.chroma.intensity.Override(Mathf.Lerp(0.2f, 1f, k));
                    hit.vig.intensity.Override(Mathf.Lerp(0.15f, 0.55f, k));
                    hit.lens.intensity.Override(0.22f * k * Mathf.Sin(t * 60f));   // 렌즈가 툭 튀었다 흔들리며 돌아옴
                }
                else hit.vol.weight = 0;
            }
            if (!any) enabled = false;
        }

        void OnDestroy() { if (inst == this) inst = null; }
    }
}
