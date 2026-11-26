// 환경설정 UI 크기 (2026-11-26): 같은 오브젝트의 CanvasScaler 기준 해상도를 처음 값 / UiScale 로 맞춤.
// 설정이 바뀌면(GameSettings.Changed) 바로 다시 맞춤. 연출 컷 캔버스(보스 등장 · 꿈 · 엔딩 · 큰 글자)에는 붙이지 않음
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    [RequireComponent(typeof(CanvasScaler))]
    public class UiScaleFollow : MonoBehaviour
    {
        CanvasScaler scaler;
        Vector2 baseRes;

        /// <summary>기준 해상도를 다 정한 뒤에 부름 (그 값을 1배로 기억)</summary>
        public static UiScaleFollow Add(CanvasScaler sc)
        {
            if (!sc) return null;
            var f = sc.GetComponent<UiScaleFollow>();
            return f ? f : sc.gameObject.AddComponent<UiScaleFollow>();
        }

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            baseRes = scaler.referenceResolution;
        }

        void OnEnable() { GameSettings.Changed += Apply; Apply(); }
        void OnDisable() { GameSettings.Changed -= Apply; }

        void Apply()
        {
            if (!scaler) return;
            scaler.referenceResolution = baseRes / Mathf.Clamp(GameSettings.UiScale, 0.8f, 1.3f);
        }
    }
}
