// 1장 보스 달리오스 '시간 지연 구역' 돔 (2026-11-23 사용자: 시간 지연인 걸 알 수 있게 돔을 씌우고 그 안에서 폭발 — 히오스 제라툴 궁처럼.
//   "돔이 생기는 것도 아크와 스쿼시 앤 스트레치를 넣어 액션감 살리고, 유저가 피할 수 있는 타이밍을 줘야")
// 프리팹: Resources/FX/Prefabs/FX_TimeDome (MCP로 만든 오브젝트, 메시 = Art/tools/blender_fx_timedome.py)
//   OrbRoot(Orb · OrbBand) = 보스가 던지는 시계 구슬 / DomeRoot(Shell · Cage · Dial · Hands) = 펼쳐지는 돔 (반지름 1 → radius배)
// 흐름: 던짐(포물선, 늘어났다 줄었다 + 회전) → 착지(납작 눌림) → 돔이 위로 길게 솟았다 눌렸다 자리 잡음(스쿼시 · 스트레치)
//      → 유지(뼈대가 거꾸로 느리게 돎 · 꼭대기 시계판 바늘이 남은 시간을 가리킴, 폭발마다 번쩍 · 살짝 부풂) → 닫힘(살짝 부풀었다 납작하게 꺼지며 사라짐)
// 런타임은 프리팹을 꺼내 움직이기만 함 (도형은 블렌더 메시 · 프리팹)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class TimeDome : MonoBehaviour
    {
        public Transform orbRoot, domeRoot, cage, hands;
        public float landTime = 0.38f;                                  // 착지 → 돔이 자리 잡을 때까지

        Renderer[] rends;
        MaterialPropertyBlock mpb;
        static readonly int FadeId = Shader.PropertyToID("_Fade"), FlashId = Shader.PropertyToID("_Flash"), RemainId = Shader.PropertyToID("_Remain");
        float flash, pulse, remain = 1f;                 // remain: 꼭대기 시계판의 남은 시간 (1 → 0)

        /// <summary>from에서 at으로 구슬을 던져 반지름 radius 돔을 펼침. flight초 뒤 착지, 그 뒤 landTime초에 돔 완성, duration초 유지</summary>
        public static TimeDome Cast(Vector3 from, Vector3 at, float radius, float flight, float duration)
        {
            var pf = Resources.Load<GameObject>("FX/Prefabs/FX_TimeDome");
            if (!pf) return null;
            var d = Instantiate(pf, at, Quaternion.identity).GetComponent<TimeDome>();
            VividLayer.SetAll(d.gameObject);
            d.StartCoroutine(d.Run(from, at, radius, flight, duration));
            return d;
        }

        /// <summary>안에서 폭발할 때: 번쩍 + 살짝 부풂</summary>
        public void Pulse() { flash = Mathf.Max(flash, 0.55f); pulse = 1f; }

        void Awake()
        {
            rends = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
        }

        void Apply(float fade)
        {
            foreach (var r in rends)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(FadeId, fade);
                mpb.SetFloat(FlashId, flash);
                mpb.SetFloat(RemainId, remain);
                r.SetPropertyBlock(mpb);
            }
        }

        static float EaseOut(float t) => 1 - (1 - t) * (1 - t);
        static float Back(float t) { const float c = 2.2f; t -= 1; return 1 + (c + 1) * t * t * t + c * t * t; }

        IEnumerator Run(Vector3 from, Vector3 at, float R, float flight, float duration)
        {
            domeRoot.gameObject.SetActive(false);
            orbRoot.gameObject.SetActive(true);
            float orbR = 0.6f;
            float h = 2.5f + Effects.Flat(at - from).magnitude * 0.25f;   // 포물선 꼭대기 높이 (출발점보다 위)
            Sfx.Play("e_conjure", 0.7f);
            // 1. 던짐: 위로 솟았다 떨어지는 포물선. 빠를수록 진행 방향으로 늘어나고(스트레치) 꼭대기에선 둥글게
            Vector3 prev = from;
            for (float t = 0; t < flight; t += Time.deltaTime)
            {
                float k = t / flight;
                var p = Vector3.Lerp(from, at, k) + Vector3.up * (4 * h * k * (1 - k));
                var vel = (p - prev) / Mathf.Max(Time.deltaTime, 1e-4f);
                prev = p;
                orbRoot.position = p + Vector3.up * orbR;
                float speed = vel.magnitude;
                float stretch = 1f + Mathf.Clamp(speed * 0.035f, 0f, 0.7f);
                if (speed > 0.01f) orbRoot.rotation = Quaternion.LookRotation(vel.normalized) * Quaternion.Euler(90, 0, 0);
                orbRoot.localScale = new Vector3(orbR / Mathf.Sqrt(stretch), orbR * stretch, orbR / Mathf.Sqrt(stretch));
                flash = Mathf.Max(0, flash - Time.deltaTime * 3);
                flash = Mathf.Max(flash, k > 0.75f ? (k - 0.75f) * 1.6f : 0);   // 떨어지기 직전 점점 밝아짐
                Apply(1);
                yield return null;
            }
            // 2. 착지: 구슬이 납작하게 눌림 → 돔이 솟음
            orbRoot.rotation = Quaternion.identity;
            orbRoot.position = at + Vector3.up * orbR * 0.35f;
            orbRoot.localScale = new Vector3(orbR * 1.7f, orbR * 0.35f, orbR * 1.7f);
            flash = 1f; Apply(1);
            Sfx.Play("e_slam_1", 0.8f); Sfx.Play("e_glass", 0.5f);
            CameraFollow.Shake(0.25f);
            yield return null; yield return null;
            orbRoot.gameObject.SetActive(false);
            domeRoot.gameObject.SetActive(true);
            // 스쿼시 · 스트레치: (가늘고 높게) → (넓고 낮게) → 제자리. 키 = (시간 비율, 가로, 세로)
            var keys = new[] { (0f, 0.15f, 0.2f), (0.3f, 0.62f, 1.55f), (0.62f, 1.14f, 0.72f), (0.84f, 0.97f, 1.06f), (1f, 1f, 1f) };
            for (float t = 0; t < landTime; t += Time.deltaTime)
            {
                float k = t / landTime;
                int i = 0; while (i < keys.Length - 2 && k > keys[i + 1].Item1) i++;
                var (k0, x0, y0) = keys[i]; var (k1, x1, y1) = keys[i + 1];
                float u = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(k0, k1, k));
                domeRoot.localScale = new Vector3(Mathf.Lerp(x0, x1, u) * R, Mathf.Lerp(y0, y1, u) * R, Mathf.Lerp(x0, x1, u) * R);
                flash = Mathf.Max(0, 1 - k * 1.4f);
                Apply(1);
                yield return null;
            }
            domeRoot.localScale = Vector3.one * R;
            // 3. 유지: 뼈대는 거꾸로 느리게. 꼭대기 시계판 = 지속 시간 (2026-11-23 사용자): 바늘이 12시부터 시계 방향으로 돌고,
            //    바늘 뒤(지난 시간)는 짙어지고 앞(남은 시간)의 밝은 부채꼴이 줄어듦. 바늘은 째깍째깍 끊어 감 (초 단위 8칸)
            float start = Time.time, end = start + duration;
            if (hands) hands.localRotation = Quaternion.identity;
            while (Time.time < end)
            {
                if (cage) cage.Rotate(0, -14f * Time.deltaTime, 0, Space.Self);
                float el = (Time.time - start) / duration;
                float stepped = Mathf.Floor(el * 48f) / 48f;                    // 째깍: 48칸
                float sub = Mathf.Clamp01((el * 48f - Mathf.Floor(el * 48f)) * 6f);
                float shown = stepped + Back(sub) / 48f;                        // 칸마다 살짝 넘쳤다 돌아옴
                remain = 1f - Mathf.Clamp01(shown);
                if (hands) hands.localRotation = Quaternion.Euler(0, 360f * Mathf.Clamp01(shown), 0);
                flash = Mathf.Max(0, flash - Time.deltaTime * 4);
                pulse = Mathf.Max(0, pulse - Time.deltaTime * 6);
                domeRoot.localScale = new Vector3(R * (1 + 0.035f * pulse), R * (1 - 0.03f * pulse), R * (1 + 0.035f * pulse));
                Apply(1);
                yield return null;
            }
            remain = 0;
            // 4. 닫힘: 살짝 부풀었다(예비 동작) 납작하게 꺼지며 사라짐
            const float close = 0.32f;
            for (float t = 0; t < close; t += Time.deltaTime)
            {
                float k = t / close;
                float sx = k < 0.35f ? Mathf.Lerp(1f, 1.08f, EaseOut(k / 0.35f)) : Mathf.Lerp(1.08f, 1.2f, (k - 0.35f) / 0.65f);
                float sy = k < 0.35f ? Mathf.Lerp(1f, 1.1f, EaseOut(k / 0.35f)) : Mathf.Lerp(1.1f, 0.02f, Mathf.SmoothStep(0, 1, (k - 0.35f) / 0.65f));
                domeRoot.localScale = new Vector3(sx * R, sy * R, sx * R);
                flash = k < 0.35f ? 0.4f : 0;
                Apply(1 - Mathf.Clamp01((k - 0.5f) / 0.5f));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
