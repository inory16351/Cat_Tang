// 5장 중간 보스 '거울 속의 고양이' 몸 (셰이더 CatTang/MirrorCat): 체력이 줄수록 금빛 균열이 번짐 (맞을수록 금이 감, 2026-11-19)
// 프리팹(Ch5_MidBoss)의 모델 뿌리에 붙음. 체력은 부모(적 뿌리)의 Health. 3페이즈 가짜 조각은 SetFake(true) → 그림자 끔 + 금 많이
using UnityEngine;

namespace CatTang
{
    public class MirrorCatSkin : MonoBehaviour
    {
        static readonly int CrackId = Shader.PropertyToID("_Crack");
        public float minCrack = 0.08f, maxCrack = 0.95f;

        Health health;
        Renderer[] rends;
        MaterialPropertyBlock mpb;
        float shown = -1f, flashUntil;
        bool fake;

        void Start()
        {
            health = GetComponentInParent<Health>();
            rends = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
        }

        /// <summary>가짜 조각(그림자 없음 · 한 발에 깨짐): 그림자를 끄고 금을 크게 보여 줌</summary>
        public void SetFake(bool on)
        {
            fake = on;
            if (rends == null) rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
                if (r) r.shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        /// <summary>맞은 순간 금이 잠깐 더 벌어져 보이게</summary>
        public void Flash(float dur = 0.12f) => flashUntil = Time.time + dur;

        void LateUpdate()
        {
            float hpFrac = health && health.maxHp > 0 ? Mathf.Clamp01(health.hp / health.maxHp) : 1f;
            float target = fake ? 0.7f : Mathf.Lerp(minCrack, maxCrack, 1f - hpFrac);
            if (Time.time < flashUntil) target = Mathf.Min(1f, target + 0.12f);
            if (Mathf.Abs(target - shown) < 0.002f) return;
            shown = Mathf.MoveTowards(shown < 0 ? target : shown, target, Time.deltaTime * 0.8f + 0.001f);
            foreach (var r in rends)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(CrackId, shown);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
