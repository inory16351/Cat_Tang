// 브롤 스타즈풍 그림체 시험 스위치 (2026-11-17 사용자: "하데스 느낌 말고 브롤 스타즈 느낌, 플랫하면서 가시성 높은 그림체". 지금 셰이더는 그대로 두고 시험)
// - F8: 툰 셰이더 전역 _CatTangBrawl 켜고 끔 (기본 꺼짐 = 지금 모습 그대로). 재질 · 빌더는 안 건드림
// - 켜져 있는 동안 고양이 · 적(Health가 있는 것) 렌더러에 _BrawlActor = 1을 표시 → 셰이더가 캐릭터는 선명하게, 배경은 한 톤 낮고 단순하게
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    public class BrawlStyle : MonoBehaviour
    {
        static readonly int GlobalId = Shader.PropertyToID("_CatTangBrawl");
        static readonly int ActorId = Shader.PropertyToID("_BrawlActor");
        public static bool On { get; private set; }
        float nextTag;
        MaterialPropertyBlock mpb;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Set(false);                                   // 도메인 재로드가 꺼져 있어 전역 값이 남을 수 있음 → 시작은 항상 원래 모습
            var go = new GameObject("BrawlStyle");
            DontDestroyOnLoad(go);
            go.AddComponent<BrawlStyle>();
        }

        public static void Set(bool on)
        {
            On = on;
            Shader.SetGlobalFloat(GlobalId, on ? 1f : 0f);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame)
            {
                Set(!On);
                Debug.Log("[BrawlStyle] 브롤 스타즈풍 시험 모드 " + (On ? "켬" : "끔"));
                nextTag = 0;
            }
            if (On && Time.unscaledTime >= nextTag) { nextTag = Time.unscaledTime + 0.5f; TagActors(); }
        }

        /// <summary>고양이 · 적 렌더러에 캐릭터 표시 (새로 생긴 적도 0.5초마다)</summary>
        public void TagActors()
        {
            mpb ??= new MaterialPropertyBlock();
            foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                if (!h.isPlayer && !h.GetComponent<Enemy>() && !h.name.StartsWith("Enemy")) continue;   // 화약통 · 상자 등 부서지는 소품은 배경 취급
                foreach (var r in h.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                    r.GetPropertyBlock(mpb);
                    if (mpb.GetFloat(ActorId) > 0.5f) continue;
                    mpb.SetFloat(ActorId, 1f);
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        void OnDisable() => Set(false);
    }
}
