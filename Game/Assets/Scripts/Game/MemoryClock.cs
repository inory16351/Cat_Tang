// 추억 특성 표시 (2026-11-18 사용자: 꽃잎이 종말과 안 어울림 → 시계)
// - MemoryClock.Ring: 맞은 적 몸 위에 반투명 시계(Resources/UI/memory_clock, Codex 그림)가 툭 커지며 종처럼 좌우로 흔들려 '울리고',
//   같은 시계가 한 겹 더 퍼지며 옅어짐
// - MemoryBadge: 슬레이 더 스파이어 '파멸'처럼 적 머리 위 체력 바 왼쪽에 작은 시계 + 쌓인 표식 수 (Health.MarkStacks, 0이면 숨김)
using UnityEngine;

namespace CatTang
{
    public class MemoryClock : MonoBehaviour
    {
        const float Life = 0.5f;
        static Texture2D tex;
        static Material mat;
        Transform target, main, echo;
        Renderer mainR, echoR;
        MaterialPropertyBlock mpb;
        Vector3 offset;
        float born, size;

        public static Texture2D Tex => tex ? tex : (tex = Resources.Load<Texture2D>("UI/memory_clock"));

        static Material Mat()
        {
            if (mat) return mat;
            if (!Tex) return null;
            mat = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { mainTexture = Tex, renderQueue = 3400 };
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            return mat;
        }

        static Renderer MakeQuad(Transform parent, string name)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = Mat();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>맞은 적(target) 몸 위에서 시계가 울림. scale 1 = 대표 특성, 보조 특성일 땐 0.75</summary>
        public static bool Ring(Transform target, Vector3 hitPoint, float scale = 1f)
        {
            var cam = Camera.main;
            var at = target ? target.position + Vector3.up * 1.2f : hitPoint;
            if (cam) at += (cam.transform.position - at).normalized * 1.8f;   // 큰 몸(깡패 등)에 묻히지 않게 카메라 쪽으로
            // 블렌더로 구운 3D 회중시계 (2026-11-18 사용자: 심심함 → 3D로 화려하게): FX_Trait_MemoryClock = bk_memclock 플립북 + 반짝임
            var fx = FxEffect.Spawn("FX_Trait_MemoryClock", at, Quaternion.identity, scale);
            if (fx) { if (target) fx.Follow(target); return true; }
            if (!Mat()) return false;
            var go = new GameObject("MemoryClock");
            var m = go.AddComponent<MemoryClock>();
            m.target = target;
            m.offset = target ? at - target.position : Vector3.zero;
            go.transform.position = at;
            m.size = 1.1f * scale;
            m.mpb = new MaterialPropertyBlock();
            m.mainR = MakeQuad(go.transform, "Clock"); m.main = m.mainR.transform;
            m.echoR = MakeQuad(go.transform, "Echo"); m.echo = m.echoR.transform;
            m.born = Time.time;
            m.LateUpdate();
            return true;
        }

        /// <summary>표식 터뜨리기: 회중시계가 금 가며 깨짐 (FX_Trait_MemoryShatter = bk_memshatter 플립북)</summary>
        public static void Shatter(Transform target, Vector3 hitPoint)
        {
            var cam = Camera.main;
            var at = target ? target.position + Vector3.up * 1.2f : hitPoint;
            if (cam) at += (cam.transform.position - at).normalized * 1.8f;
            var fx = FxEffect.Spawn("FX_Trait_MemoryShatter", at, Quaternion.identity, 1.2f);
            if (fx && target) fx.Follow(target);
            CameraFollow.Shake(0.12f);
        }

        void LateUpdate()
        {
            float t = Time.time - born;
            if (t > Life) { Destroy(gameObject); return; }
            if (target) transform.position = target.position + offset;
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            float u = t / Life;
            // 본 시계: 0.07초에 툭 커졌다 자리 잡고, 종처럼 좌우로 흔들리다 잦아듦
            float pop = t < 0.07f ? Mathf.Lerp(0.5f, 1.15f, t / 0.07f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.07f) / 0.08f));
            float swing = Mathf.Sin(t * 42f) * 18f * (1 - u);
            main.localScale = Vector3.one * size * pop;
            main.localRotation = Quaternion.Euler(0, 0, swing);
            float a = 0.62f * (u < 0.6f ? 1f : 1 - (u - 0.6f) / 0.4f);
            mpb.SetColor("_Color", new Color(1, 1, 1, a));
            mainR.SetPropertyBlock(mpb);
            // 울림 겹: 같은 시계가 퍼지며 옅어짐
            echo.localScale = Vector3.one * size * Mathf.Lerp(1f, 1.9f, u);
            echo.localPosition = new Vector3(0, 0, 0.01f);
            mpb.SetColor("_Color", new Color(1, 1, 1, 0.35f * (1 - u)));
            echoR.SetPropertyBlock(mpb);
        }
    }

    /// <summary>적 머리 위 체력 바 왼쪽: 작은 시계 + 쌓인 추억 표식 수 (EnemyHpBar가 붙임)</summary>
    public class MemoryBadge : MonoBehaviour
    {
        Health h;
        Renderer icon;
        Renderer[] digits = new Renderer[2];
        static Material[] digitMats;
        static float[] digitAspect;
        MaterialPropertyBlock mpb;
        int shown = -1;
        float bump;
        // 크기 배율 (2026-11-18 사용자: 머리 위 표식이 작아 잘 안 보임 → 키움)
        const float S = 1.8f;

        public static void Attach(Health h, Transform barRoot, float barWidth)
        {
            if (!h || !barRoot || !MemoryClock.Tex) return;
            var go = new GameObject("MemoryBadge");
            go.transform.SetParent(barRoot, false);
            go.transform.localPosition = new Vector3(-barWidth / 2 + 0.17f * S, 0.12f + 0.18f * S, -0.003f);   // 체력 바 위 왼쪽
            var b = go.AddComponent<MemoryBadge>();
            b.h = h;
            b.Build();
        }

        void Build()
        {
            mpb = new MaterialPropertyBlock();
            var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = MemoryClock.Tex, renderQueue = 3105 };
            icon = Quad("Icon", mat, new Vector3(0, 0, 0), 0.34f * S, 0.34f * S);
            if (digitMats == null)
            {
                digitMats = new Material[10]; digitAspect = new float[10];
                for (int i = 0; i < 10; i++)
                {
                    var tx = Resources.Load<Texture2D>("UI/digits/d" + i);
                    if (!tx) { digitMats = null; break; }
                    digitMats[i] = new Material(Shader.Find("Sprites/Default")) { mainTexture = tx, renderQueue = 3106 };
                    digitAspect[i] = (float)tx.width / tx.height;
                }
            }
            for (int i = 0; i < 2; i++) digits[i] = Quad("Digit" + i, null, Vector3.zero, 0.2f * S, 0.26f * S);
            gameObject.SetActive(true);
        }

        Renderer Quad(string name, Material m, Vector3 pos, float w, float hgt)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(transform, false);
            q.transform.localPosition = pos;
            q.transform.localScale = new Vector3(w, hgt, 1);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = 110;
            return r;
        }

        void LateUpdate()
        {
            if (!h) return;
            int n = h.Alive ? h.MarkStacks : 0;
            bool on = n > 0;
            if (icon.enabled != on) { icon.enabled = on; foreach (var d in digits) d.enabled = false; }
            if (!on) { shown = 0; return; }
            if (n != shown) { if (n > shown) bump = 0.15f; shown = n; Layout(n); }
            // 표식이 늘면 시계가 살짝 튀며 흔들림
            if (bump > 0) bump -= Time.deltaTime;
            float k = Mathf.Clamp01(bump / 0.15f);
            icon.transform.localScale = Vector3.one * 0.34f * S * (1 + 0.35f * k);
            icon.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 50f) * 20f * k);
        }

        void Layout(int n)
        {
            if (digitMats == null) return;
            string s = n.ToString();
            float h0 = 0.24f * S, x = 0.2f * S;
            for (int i = 0; i < digits.Length; i++)
            {
                var d = digits[i];
                if (i >= s.Length) { d.enabled = false; continue; }
                int v = s[i] - '0';
                d.enabled = true;
                d.sharedMaterial = digitMats[v];
                float w = h0 * digitAspect[v];
                d.transform.localScale = new Vector3(w, h0, 1);
                d.transform.localPosition = new Vector3(x + w / 2, -0.06f * S, -0.001f);
                x += w * 0.85f;
            }
        }
    }
}
