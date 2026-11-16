// 화면 연출 (URP 전역 볼륨을 런타임에 만들어 씀)
// - PerfectDodge: 극한 회피(닷지 액션) — 젠레스 존 제로식. 순간 파란 섬광 → 화면이 회색으로 빠지고(약간 차갑게) 가장자리 색수차 · 비네트,
//   슬로우가 풀리며 색이 돌아옴. 시간은 실제 시간(unscaled)으로 돌림 (슬로우 중에도 일정)
// 플레이 진입 시 도메인 리로드를 끈 프로젝트라 정적 참조가 판을 넘어 남음 → 볼륨이 파괴됐으면 다시 만듦
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    public class ScreenFx : MonoBehaviour
    {
        static ScreenFx inst;
        Volume vol;
        ColorAdjustments color;
        ChromaticAberration chroma;
        Vignette vig;
        LensDistortion lens;

        float start = -99, hold, recover;

        static ScreenFx Get()
        {
            if (inst) return inst;
            var go = new GameObject("ScreenFx");
            inst = go.AddComponent<ScreenFx>();
            inst.Build();
            return inst;
        }

        void Build()
        {
            vol = gameObject.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100;
            vol.weight = 0;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            color = prof.Add<ColorAdjustments>(true);
            chroma = prof.Add<ChromaticAberration>(true);
            vig = prof.Add<Vignette>(true);
            lens = prof.Add<LensDistortion>(true);
            vig.color.Override(new Color(0.05f, 0.08f, 0.2f));
            vig.smoothness.Override(0.45f);
            vol.profile = prof;
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
            s.start = Time.unscaledTime; s.hold = hold; s.recover = recover;
            s.enabled = true;
        }

        void Update()
        {
            float t = Time.unscaledTime - start;
            float total = hold + recover;
            if (t > total + 0.05f) { vol.weight = 0; enabled = false; return; }
            // 무게: 0.04초에 확 들어오고 hold 동안 유지, recover 동안 부드럽게 빠짐
            float w = t < 0.04f ? t / 0.04f : t < hold ? 1f : 1f - Mathf.SmoothStep(0, 1, (t - hold) / Mathf.Max(0.01f, recover));
            vol.weight = Mathf.Clamp01(w);
            // 첫 0.12초: 파랗게 번쩍 (노출 ↑ · 푸른 필터) → 회색으로 가라앉음
            float flash = Mathf.Clamp01(1f - t / 0.12f);
            color.postExposure.Override(Mathf.Lerp(-0.15f, 1.3f, flash));
            color.saturation.Override(Mathf.Lerp(-82f, -20f, flash));
            color.contrast.Override(18f);
            color.colorFilter.Override(Color.Lerp(new Color(0.86f, 0.92f, 1f), new Color(0.7f, 0.85f, 1f), flash));
            // 가장자리: 색수차는 번쩍일 때 세게, 비네트는 유지, 렌즈는 살짝 빨려 들어갔다 풀림
            chroma.intensity.Override(Mathf.Lerp(0.35f, 1f, flash));
            vig.intensity.Override(0.38f);
            lens.intensity.Override(Mathf.Lerp(-0.08f, -0.28f, flash));
        }

        void OnDestroy() { if (inst == this) inst = null; }
    }
}
