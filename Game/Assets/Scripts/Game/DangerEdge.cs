// 폭발하는 물체 표시 (2026-11-17 사용자: 몬스터처럼 바깥 테두리로 "터진다"를 알려 줄 것)
// 적의 빨간 바깥 테두리와 같은 셰이더 패스(CatTangOuterOutline)를 쓰되 색은 주황 ↔ 노랑으로 천천히 맥동.
// 스텐실은 적(4)과 다른 값(5)이라 적과 겹쳐도 각자 테두리가 따로 그려짐.
// 재질은 인스턴스로 복제해서 바꿈(스텐실 값은 재질 렌더 상태라 프로퍼티 블록으로는 안 바뀜). 툰 재질이 아닌 렌더러는 건너뜀
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class DangerEdge : MonoBehaviour
    {
        public static readonly Color Orange = new(1f, 0.42f, 0.05f);
        public static readonly Color Yellow = new(1f, 0.86f, 0.18f);
        public const float Width = 5f, Period = 1.1f;       // 적(3.5)보다 조금 굵게: 작은 물체라 게임 카메라에서 얇아 보임
        public const int Stencil = 5;

        static readonly int ColorId = Shader.PropertyToID("_OuterOutlineColor");
        static readonly int WidthId = Shader.PropertyToID("_OuterOutlineWidth");
        static readonly int StencilId = Shader.PropertyToID("_OuterStencilRef");

        readonly List<Material> mats = new();
        float phase;

        /// <summary>폭발물에 붙임 (이미 있으면 그대로)</summary>
        public static DangerEdge Add(GameObject go) => go.TryGetComponent<DangerEdge>(out var e) ? e : go.AddComponent<DangerEdge>();

        void Start()
        {
            phase = Random.value * Period;          // 여러 개가 한꺼번에 깜빡이지 않게
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                foreach (var m in r.materials)      // 인스턴스
                {
                    if (!m || !m.HasProperty(StencilId) || !m.HasProperty(WidthId)) continue;
                    m.SetInt(StencilId, Stencil);
                    m.SetFloat(WidthId, Width);
                    mats.Add(m);
                }
            }
        }

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / Period);
            var c = Color.Lerp(Orange, Yellow, k);
            foreach (var m in mats) if (m) m.SetColor(ColorId, c);
        }

        /// <summary>이미 터진 물체(증기 밸브처럼 남는 것): 테두리 끔</summary>
        public void Off()
        {
            foreach (var m in mats) if (m) m.SetFloat(WidthId, 0f);
            enabled = false;
        }

        void OnDestroy()
        {
            foreach (var m in mats) if (m) Destroy(m);
        }
    }
}
