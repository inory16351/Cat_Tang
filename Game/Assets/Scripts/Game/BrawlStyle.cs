// 브롤 스타즈풍 그림체 (2026-11-17 사용자: "하데스 느낌 말고 브롤 스타즈 느낌" → 시험 후 **기본으로 확정**)
// - 툰 셰이더 전역 _CatTangBrawl = 1이 기본 (예전 하데스풍 툰은 셰이더에 그대로 남아 있음 — 플레이 중 F8로 비교용 전환). 재질 · 빌더는 안 건드림
// - 에디터 씬 화면도 같은 그림체로 보이게 Editor/BrawlStyleEditor가 켬
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
            Set(true);                                    // 기본 = 브롤풍 (도메인 재로드가 꺼져 있어 전역 값이 남을 수 있음 → 시작할 때 항상 다시 넣음)
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
                Debug.Log("[BrawlStyle] 그림체: " + (On ? "브롤 스타즈풍 (기본)" : "예전 하데스풍 툰 (비교용)"));
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

        void OnDisable() => Set(true);                    // 플레이를 끝내도 에디터 화면은 기본 그림체로
    }
}
