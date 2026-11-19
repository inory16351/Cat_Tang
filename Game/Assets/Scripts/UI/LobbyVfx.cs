// 로비 VFX (2026-11-18 사용자: 로비 화면에 VFX 넣어서 생동감) — 그림(키 일러스트) 위에 코드로 얹는 움직임. 그림과 같이 움직이게 art 아래에 붙음
//  - 해 · 회중시계 뒤: 천천히 도는 햇살 + 숨쉬듯 부푸는 빛무리
//  - 녹은 회중시계 아래 가장자리: 녹은 금 방울이 맺혔다 늘어나 떨어지며 사라짐
//  - 두 자루 권총 총구: 가끔 번쩍이는 네 갈래 반짝임
//  - 아래 왼쪽 모래 바람: 큰 불똥이 바람 따라 휘날림 (작은 불티는 LobbyScreen)
//  - 제목이 찍힐 때: 불꽃이 사방으로 튐 / 제목 녹은 방울도 가끔 떨어짐
// 위치는 그림 비율 좌표(u, v: 왼쪽 위 0,0 → 오른쪽 아래 1,1, key.png 1920x1080 기준)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class LobbyVfx : MonoBehaviour
    {
        // 그림 속 위치 (key.png 기준)
        static readonly Vector2 Sun = new(0.443f, 0.21f);
        static readonly Vector2 DripL = new(0.36f, 0.43f), DripR = new(0.53f, 0.43f);   // 녹는 시계 아래 가장자리
        static readonly Vector2 GunUp = new(0.143f, 0.115f), GunDown = new(0.428f, 0.79f);

        RectTransform art, title;
        RectTransform rays, rays2, halo;
        Image haloImg;
        Sprite dot, star, ray;
        readonly List<Drop> drops = new();
        readonly List<Spark> sparks = new();
        float nextDrip, nextGlint, nextGust, nextTitleDrip, on;
        bool active;

        class Drop { public RectTransform rt; public Image img; public float t, life, vy, stretch; public bool falling; public Vector2 pos; }
        class Spark { public RectTransform rt; public Image img; public Vector2 v; public float t, life, size, spin; public Color col; public bool gravity; }

        public static LobbyVfx Create(RectTransform art, RectTransform title)
        {
            var go = new GameObject("LobbyVfx", typeof(RectTransform));
            go.transform.SetParent(art, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var v = go.AddComponent<LobbyVfx>();
            v.art = art; v.title = title;
            v.Build();
            return v;
        }

        static Vector2 P(Vector2 uv) => new((uv.x - 0.5f) * 1920f, (0.5f - uv.y) * 1080f);

        void Build()
        {
            dot = MakeDot(); star = MakeStar(); ray = MakeRays();
            halo = Img("Halo", dot, P(Sun), new Vector2(760, 760), new Color(1f, 0.55f, 0.15f, 0)).rectTransform;
            haloImg = halo.GetComponent<Image>();
            rays = Img("Rays", ray, P(Sun), new Vector2(1500, 1500), new Color(1f, 0.7f, 0.3f, 0)).rectTransform;
            rays2 = Img("Rays2", ray, P(Sun), new Vector2(1150, 1150), new Color(1f, 0.45f, 0.15f, 0)).rectTransform;
            rays.SetAsFirstSibling(); rays2.SetSiblingIndex(1); halo.SetSiblingIndex(2);
        }

        Image Img(string n, Sprite s, Vector2 pos, Vector2 size, Color c)
        {
            var rt = UIKit.MakeRect(n, transform, new Vector2(0.5f, 0.5f), pos, size);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = false;
            return im;
        }

        /// <summary>그림이 다 그려진 뒤 켬</summary>
        public void Begin() { active = true; nextDrip = nextGlint = Time.unscaledTime + 0.4f; nextGust = Time.unscaledTime + 1f; nextTitleDrip = Time.unscaledTime + 2.5f; }

        /// <summary>제목이 쾅 찍힐 때: 불꽃이 사방으로</summary>
        public void TitleBurst(Vector2 screenLocal)
        {
            for (int i = 0; i < 46; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2);
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * Random.Range(250f, 900f);
                AddSpark(screenLocal, v, Random.Range(0.4f, 0.9f), Random.Range(6f, 16f), Random.value < 0.5f ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.12f), true, Random.value < 0.4f);
            }
        }

        void Update()
        {
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            on = Mathf.MoveTowards(on, active ? 1 : 0, dt * 0.8f);
            // 햇살 · 빛무리
            if (rays)
            {
                rays.localRotation = Quaternion.Euler(0, 0, now * 3f);
                rays2.localRotation = Quaternion.Euler(0, 0, -now * 5f + 20f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(now * 1.3f);
                rays.GetComponent<Image>().color = new Color(1f, 0.72f, 0.3f, (0.16f + pulse * 0.08f) * on);
                rays2.GetComponent<Image>().color = new Color(1f, 0.45f, 0.15f, (0.12f + (1 - pulse) * 0.06f) * on);
                haloImg.color = new Color(1f, 0.6f, 0.2f, (0.22f + pulse * 0.14f) * on);
                halo.localScale = Vector3.one * (1f + pulse * 0.08f);
            }
            if (!active) { StepParticles(dt); return; }

            // 녹은 금 방울
            if (now >= nextDrip)
            {
                nextDrip = now + Random.Range(0.35f, 0.9f);
                var uv = Vector2.Lerp(DripL, DripR, Random.value);
                uv.y += Mathf.Sin((uv.x - DripL.x) / (DripR.x - DripL.x) * Mathf.PI) * 0.04f;   // 가장자리가 아래로 둥글게
                SpawnDrop(P(uv), Random.Range(9f, 15f));
            }
            // 총구 반짝임
            if (now >= nextGlint)
            {
                nextGlint = now + Random.Range(1.6f, 3.2f);
                var at = P(Random.value < 0.5f ? GunUp : GunDown);
                AddSpark(at, Vector2.zero, 0.45f, 120f, new Color(1f, 0.95f, 0.7f), false, false, star, 200f);
                AddSpark(at, Vector2.zero, 0.3f, 60f, new Color(1f, 0.7f, 0.3f), false, false, dot, 0f);
            }
            // 모래 바람에 휘날리는 큰 불똥 (아래 왼쪽 → 오른쪽 위)
            if (now >= nextGust)
            {
                nextGust = now + Random.Range(0.12f, 0.3f);
                var from = P(new Vector2(Random.Range(0f, 0.45f), Random.Range(0.8f, 1.02f)));
                var v = new Vector2(Random.Range(120f, 320f), Random.Range(60f, 200f));
                AddSpark(from, v, Random.Range(1.6f, 3.2f), Random.Range(5f, 11f), Random.value < 0.6f ? new Color(1f, 0.55f, 0.15f) : new Color(1f, 0.85f, 0.45f), false, false);
            }
            // 제목의 녹은 방울
            if (title && now >= nextTitleDrip)
            {
                nextTitleDrip = now + Random.Range(1.8f, 3.5f);
                var local = (Vector2)transform.InverseTransformPoint(title.TransformPoint(title.rect.center + new Vector2(Random.Range(-0.38f, 0.4f) * title.rect.width, -title.rect.height * 0.3f)));
                SpawnDrop(local, Random.Range(8f, 12f));
            }
            StepParticles(dt);
        }

        void SpawnDrop(Vector2 at, float size)
        {
            var im = Img("Drop", dot, at, new Vector2(size, size), new Color(1f, 0.72f, 0.2f, 0));
            drops.Add(new Drop { rt = im.rectTransform, img = im, life = Random.Range(0.6f, 1.2f), pos = at, stretch = 1 });
        }

        void AddSpark(Vector2 at, Vector2 v, float life, float size, Color c, bool gravity, bool streak, Sprite s = null, float spin = 0)
        {
            var im = Img("Spark", s ? s : dot, at, new Vector2(streak ? size * 3f : size, size), c);
            sparks.Add(new Spark { rt = im.rectTransform, img = im, v = v, life = life, size = size, col = c, gravity = gravity, spin = spin });
        }

        void StepParticles(float dt)
        {
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.t += dt;
                if (!d.falling)
                {
                    // 맺힘: 커지며 아래로 늘어남
                    float k = d.t / d.life;
                    d.img.color = new Color(1f, 0.72f + k * 0.1f, 0.2f, Mathf.Clamp01(k * 3f));
                    d.stretch = 1f + k * 0.9f;
                    d.rt.anchoredPosition = d.pos + new Vector2(0, -k * 8f);
                    if (k >= 1) { d.falling = true; d.t = 0; d.pos = d.rt.anchoredPosition; }
                }
                else
                {
                    d.vy += 1400f * dt;
                    d.pos.y -= d.vy * dt;
                    d.stretch = 1.4f + Mathf.Min(1.8f, d.vy / 400f);
                    d.rt.anchoredPosition = d.pos;
                    float fade = Mathf.Clamp01(1f - d.t / 0.55f);
                    d.img.color = new Color(1f, 0.8f, 0.3f, fade);
                    if (fade <= 0) { Destroy(d.rt.gameObject); drops.RemoveAt(i); continue; }
                }
                d.rt.localScale = new Vector3(1f / Mathf.Sqrt(d.stretch), d.stretch, 1);
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                s.t += dt;
                float k = s.t / s.life;
                if (k >= 1) { Destroy(s.rt.gameObject); sparks.RemoveAt(i); continue; }
                if (s.gravity) s.v += new Vector2(0, -900f) * dt; else s.v += new Vector2(Mathf.Sin(s.t * 4f + i) * 60f, 20f) * dt;
                s.v *= 1f - dt * (s.gravity ? 1.2f : 0.3f);
                s.rt.anchoredPosition += s.v * dt;
                if (s.v.sqrMagnitude > 1f && s.spin == 0) s.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(s.v.y, s.v.x) * Mathf.Rad2Deg);
                else s.rt.localRotation = Quaternion.Euler(0, 0, s.t * s.spin);
                float a = s.spin > 0 ? Mathf.Sin(k * Mathf.PI) : (1 - k) * (0.7f + 0.3f * Mathf.Sin(s.t * 30f));
                s.img.color = new Color(s.col.r, s.col.g, s.col.b, a);
                if (s.spin > 0) s.rt.localScale = Vector3.one * Mathf.Sin(k * Mathf.PI);
            }
        }

        // ---------- 절차 텍스처 ----------
        static Sprite MakeDot()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float a = Mathf.Clamp01(1 - d); a = a * a;
                    float core = Mathf.Clamp01(1 - d * 2.2f);
                    px[y * N + x] = new Color32(255, (byte)(200 + 55 * core), (byte)(150 + 105 * core), (byte)(Mathf.Clamp01(a + core) * 255));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeStar()
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1;
                    float arm = Mathf.Max(Mathf.Clamp01(1 - Mathf.Abs(u) * 18f) * (1 - Mathf.Abs(v)), Mathf.Clamp01(1 - Mathf.Abs(v) * 18f) * (1 - Mathf.Abs(u)));
                    float glow = Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v) * 2.2f);
                    float a = Mathf.Clamp01(arm * 1.4f + glow * glow);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeRays()
        {
            const int N = 256;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            var rnd = new System.Random(7);
            var w = new float[24];
            for (int i = 0; i < w.Length; i++) w[i] = 0.4f + (float)rnd.NextDouble() * 0.6f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float ang = (Mathf.Atan2(v, u) / (Mathf.PI * 2) + 0.5f) * w.Length;
                    int k = Mathf.FloorToInt(ang) % w.Length;
                    float f = ang - Mathf.Floor(ang);
                    float beam = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(f - 0.5f) * 2f / (0.25f + w[k] * 0.35f)), 2f) * w[k];
                    float a = beam * Mathf.Clamp01(1 - r) * Mathf.Clamp01(r * 4f - 0.6f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }
    }
}
