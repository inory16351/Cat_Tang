// 적 사망 연출 (2026-11-16 사용자: 이미지로 터지며 사라지지 말고 검은 안개로 변해 사라지게)
// 적 모델을 떼어 내 그 자세로 굳히고(애니메이터 정지) → 0.12초 만에 검게 물듦 → 검은 안개(블렌더로 구운 bk_mist)가 소용돌이치며 솟는 사이
// 몸이 가라앉고 옆으로 오그라들며 사라짐. 적 본체(판정 · 체력)는 바로 지워지고 이 껍데기만 남아 연출함
using UnityEngine;

namespace CatTang
{
    public class DeathMist : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly Color Ink = new Color(0.05f, 0.03f, 0.07f);

        Renderer[] rends;
        MaterialPropertyBlock mpb;
        Vector3 basePos, baseScale;
        float born, height, life, nextPuff;
        int puffs;

        /// <summary>model = 적의 "Model" 자식. size = 연출 크기 배율 (보스는 크게)</summary>
        public static void Play(Transform model, float size = 1f)
        {
            if (!model) return;
            model.SetParent(null, true);
            foreach (var a in model.GetComponentsInChildren<Animator>()) a.enabled = false;        // 그 자세로 굳음
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            var d = model.gameObject.AddComponent<DeathMist>();
            d.rends = model.GetComponentsInChildren<Renderer>();
            d.mpb = new MaterialPropertyBlock();
            d.basePos = model.position;
            d.baseScale = model.localScale;
            d.born = Time.time;
            d.life = 0.55f + 0.15f * size;
            var b = new Bounds(model.position, Vector3.zero);
            foreach (var r in d.rends) b.Encapsulate(r.bounds);
            d.height = Mathf.Max(1f, b.size.y);
            // 첫 안개: 몸 전체를 덮는 큰 뭉치
            d.Puff(0.5f, 1.25f * size);
        }

        void Puff(float h01, float scale)
        {
            var at = basePos + Vector3.up * height * h01 + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f)) * height * 0.3f;
            FxFlipbook.Spawn("bk_mist", at, Vector3.zero, height * scale, Random.Range(0.6f, 0.8f), false,
                Vector3.up * height * 0.5f + Random.insideUnitSphere * 0.3f, Random.Range(0f, 360f));
            puffs++;
        }

        void Update()
        {
            float t = (Time.time - born) / life;
            if (t >= 1f) { Destroy(gameObject); return; }
            // 검게 물듦 (0.12초) — 툰 재질의 기본 색을 잉크색으로
            float ink = Mathf.Clamp01((Time.time - born) / 0.12f);
            foreach (var r in rends)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Color.Lerp(Color.white, Ink, ink));
                mpb.SetColor(OutlineColorId, new Color(0.25f, 0.1f, 0.35f));
                r.SetPropertyBlock(mpb);
            }
            // 가라앉으며 옆으로 오그라들고 살짝 위로 늘어남 (안개로 빨려 올라가는 느낌)
            float e = t * t;
            transform.position = basePos + Vector3.down * height * 0.35f * e;
            transform.localScale = Vector3.Scale(baseScale, new Vector3(1f - 0.85f * e, 1f + 0.25f * t - 0.6f * e, 1f - 0.85f * e));
            // 안개 뭉치를 몇 번 더 (위로 갈수록 작게)
            if (puffs < 4 && Time.time >= nextPuff)
            {
                nextPuff = Time.time + life * 0.18f;
                Puff(Random.Range(0.3f, 0.9f), Random.Range(0.7f, 1f));
            }
        }
    }
}
