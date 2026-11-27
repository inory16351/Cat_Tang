// 얼굴 없는 NPC의 사물 감정 표현 (스토리 기획서 06 대화 연출, 2026-11-27 구현)
// 대화 일러스트 위에 그 NPC의 '몸이 된 물건'이 감정에 따라 움직임. 표정 토큰은 대사 머리 "이름#표정::대사"를 그대로 씀
//   태엽 풀린 뻐꾸기 = 깃털: 놀람 → 깃털이 확 흩날림 / 다정 → 볏에서 깃털이 살랑 떠오름 / 쓸쓸 · 엄숙 → 깃털이 천천히 떨어짐 / 의미심장 → 깃털 하나가 볏 옆에서 맴돎
//   괘종시계 문지기 = 진자 랜턴 밝기: 엄숙 → 밝고 고르게 / 놀람 → 확 타올랐다 가라앉음 / 쓸쓸 → 어둡게 / 다정 → 따뜻하게 천천히 맥동 / 의미심장 → 느린 깜빡임
//   실타래 재봉사 = 실 풀림: 쓸쓸 → 실이 길게 풀려 늘어짐 / 놀람 → 툭 풀려 흔들림 / 다정 → 실이 다시 감김 / 엄숙 → 팽팽하게 / 의미심장 → 실 끝이 천천히 흔들림
// 자리는 일러스트 그림 기준 비율 (Resources/UI/Portraits/*.png), 대화가 멈춘 동안에도 돌아가게 unscaled 시간
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class TalkEmote : MonoBehaviour
    {
        enum Kind { Feather, Lantern, Thread }

        Kind kind;
        RectTransform holder;
        Image portrait;
        RectTransform layer;

        // 그림 안 자리 (왼쪽 아래 = 0,0)
        static readonly Vector2 Crest = new(0.41f, 0.93f), Chest = new(0.5f, 0.42f);
        static readonly Vector2 LanternAt = new(0.50f, 0.15f);
        static readonly Vector2 SpoolAt = new(0.36f, 0.80f);

        /// <summary>사물 감정이 있는 NPC면 일러스트 자리에 붙임 (없으면 null)</summary>
        public static TalkEmote Attach(RectTransform holder, Image portrait, string who)
        {
            Kind k;
            switch (who)
            {
                case "태엽 풀린 뻐꾸기": k = Kind.Feather; break;
                case "괘종시계 문지기": k = Kind.Lantern; break;
                case "실타래 재봉사": k = Kind.Thread; break;
                default: return null;
            }
            var e = holder.gameObject.AddComponent<TalkEmote>();
            e.kind = k; e.holder = holder; e.portrait = portrait;
            e.layer = UIKit.MakeRect("Emote", holder, new Vector2(0.5f, 0), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0));
            e.Build();
            return e;
        }

        Vector2 At(Vector2 n)
        {
            var s = portrait ? portrait.rectTransform.sizeDelta : new Vector2(600, 820);
            return new Vector2((n.x - 0.5f) * s.x, n.y * s.y);
        }

        float Alpha => portrait ? portrait.color.a : 1f;
        bool Speaking => portrait && portrait.color.r > 0.8f;     // 듣는 쪽은 어둡게 칠해짐 (GameUI.TalkCo)

        // ---------- 뻐꾸기 깃털 ----------
        class Feather { public Image im; public Vector2 p, v; public float rot, spin, life, age, sway, size; }
        readonly List<Feather> feathers = new();

        void SpawnFeather(Vector2 from, Vector2 vel, float life, float size, float sway)
        {
            var rt = UIKit.MakeRect("Feather", layer, new Vector2(0.5f, 0), from, new Vector2(22, 64) * size);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = FeatherSpr(); im.raycastTarget = false;
            im.color = Random.value < 0.5f ? new Color(0.86f, 0.42f, 0.22f) : new Color(0.95f, 0.78f, 0.45f);
            feathers.Add(new Feather { im = im, p = from, v = vel, rot = Random.Range(-40f, 40f), spin = Random.Range(-160f, 160f), life = life, size = size, sway = sway });
        }

        // ---------- 문지기 랜턴 ----------
        Image glow, core;
        float lamp = 0.55f, lampTarget = 0.55f, flare, pulse, flicker;

        // ---------- 재봉사 실 ----------
        readonly List<Image> thread = new();
        float threadLen = 0.25f, threadTarget = 0.25f, threadSwing, threadTaut;

        void Build()
        {
            if (kind == Kind.Lantern)
            {
                glow = Img("Glow", GlowSpr(), new Vector2(260, 260), new Color(1f, 0.72f, 0.3f, 0));
                core = Img("Core", GlowSpr(), new Vector2(90, 90), new Color(1f, 0.95f, 0.75f, 0));
            }
            else if (kind == Kind.Thread)
            {
                for (int i = 0; i < 28; i++) thread.Add(Img("Thread", DotSpr(), new Vector2(9, 9), new Color(0.72f, 0.1f, 0.14f, 0)));
            }
        }

        Image Img(string n, Sprite sp, Vector2 size, Color c)
        {
            var rt = UIKit.MakeRect(n, layer, new Vector2(0.5f, 0), Vector2.zero, size);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = sp; im.raycastTarget = false; im.color = c;
            return im;
        }

        /// <summary>이 NPC가 이 표정으로 말하기 시작할 때 (표정 없음 = 기본)</summary>
        public void Cue(string expr)
        {
            switch (kind)
            {
                case Kind.Feather:
                    switch (expr)
                    {
                        case "놀람":
                            for (int i = 0; i < 9; i++) { float a = Random.Range(20f, 160f) * Mathf.Deg2Rad; SpawnFeather(At(Crest) + Random.insideUnitCircle * 30f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(260f, 420f), 1.6f, Random.Range(0.8f, 1.15f), 40f); }
                            break;
                        case "다정":
                            for (int i = 0; i < 3; i++) SpawnFeather(At(Crest) + new Vector2(Random.Range(-60f, 60f), 0), new Vector2(Random.Range(-30f, 30f), Random.Range(60f, 100f)), 2.4f, Random.Range(0.6f, 0.85f), 60f);
                            break;
                        case "쓸쓸":
                        case "엄숙":
                            for (int i = 0; i < 2; i++) SpawnFeather(At(Chest) + new Vector2(Random.Range(-140f, 140f), Random.Range(-40f, 60f)), new Vector2(0, -40f), 3.2f, Random.Range(0.75f, 0.95f), 90f);
                            break;
                        case "의미심장":
                            SpawnFeather(At(Crest) + new Vector2(110f, -30f), Vector2.zero, 3.0f, 0.9f, 0f);
                            feathers[feathers.Count - 1].spin = 70f;
                            break;
                    }
                    break;
                case Kind.Lantern:
                    pulse = 0; flicker = 0;
                    switch (expr)
                    {
                        case "엄숙": lampTarget = 1.0f; break;
                        case "놀람": lampTarget = 0.7f; flare = 1f * GameSettings.FlashScale; break;
                        case "쓸쓸": lampTarget = 0.22f; break;
                        case "다정": lampTarget = 0.78f; pulse = 1f; break;
                        case "의미심장": lampTarget = 0.6f; flicker = 1f; break;
                        default: lampTarget = 0.55f; break;
                    }
                    break;
                case Kind.Thread:
                    threadTaut = 0;
                    switch (expr)
                    {
                        case "쓸쓸": threadTarget = 1f; break;
                        case "놀람": threadTarget = Mathf.Max(threadLen, 0.7f); threadSwing = 1f; break;
                        case "다정": threadTarget = 0.18f; break;
                        case "엄숙": threadTarget = 0.4f; threadTaut = 1f; break;
                        case "의미심장": threadTarget = 0.55f; threadSwing = 0.5f; break;
                        default: threadTarget = Mathf.Min(threadLen, 0.35f); break;
                    }
                    break;
            }
        }

        void Update()
        {
            if (!portrait) return;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime, a = Alpha;
            switch (kind)
            {
                case Kind.Feather:
                    for (int i = feathers.Count - 1; i >= 0; i--)
                    {
                        var f = feathers[i];
                        f.age += dt;
                        if (f.age >= f.life || !f.im) { if (f.im) Destroy(f.im.gameObject); feathers.RemoveAt(i); continue; }
                        f.v *= Mathf.Exp(-2.2f * dt);
                        f.v.y -= 55f * dt;                                       // 가볍게 떨어짐
                        f.p += f.v * dt + new Vector2(Mathf.Sin(f.age * 3.1f + i) * f.sway * dt, 0);
                        f.rot += f.spin * dt; f.spin *= Mathf.Exp(-0.8f * dt);
                        var rt = f.im.rectTransform;
                        rt.anchoredPosition = f.p; rt.localEulerAngles = new Vector3(0, 0, f.rot + Mathf.Sin(f.age * 3.1f) * 25f);
                        float fa = Mathf.Clamp01(f.age / 0.12f) * Mathf.Clamp01((f.life - f.age) / 0.6f);
                        var c = f.im.color; c.a = fa * a; f.im.color = c;
                    }
                    break;
                case Kind.Lantern:
                {
                    if (!Speaking) lampTarget = Mathf.Min(lampTarget, 0.45f);
                    lamp = Mathf.Lerp(lamp, lampTarget, dt * 3f);
                    flare = Mathf.MoveTowards(flare, 0, dt * 1.6f);
                    float v = lamp + flare * 0.9f;
                    if (pulse > 0) v += Mathf.Sin(t * 2.4f) * 0.12f;
                    if (flicker > 0) v *= 0.75f + 0.25f * Mathf.PerlinNoise(t * 3.5f, 0.3f) * 2f * (Mathf.Sin(t * 1.3f) > 0.6f ? 0.4f : 1f);
                    v += (Mathf.PerlinNoise(t * 6f, 1.7f) - 0.5f) * 0.08f;          // 불꽃 일렁임
                    var p = At(LanternAt);
                    glow.rectTransform.anchoredPosition = p; core.rectTransform.anchoredPosition = p;
                    float s = 0.8f + v * 0.7f;
                    glow.rectTransform.localScale = new Vector3(s, s, 1);
                    glow.color = new Color(1f, 0.7f, 0.28f, Mathf.Clamp01(v * 0.55f) * a);
                    core.color = new Color(1f, 0.95f, 0.75f, Mathf.Clamp01(v * 0.65f) * a);
                    break;
                }
                case Kind.Thread:
                {
                    threadLen = Mathf.Lerp(threadLen, threadTarget, dt * 1.8f);
                    threadSwing = Mathf.MoveTowards(threadSwing, 0, dt * 0.5f);
                    var from = At(SpoolAt);
                    float L = 60f + threadLen * 420f;                                 // 풀린 길이 (px)
                    float sag = threadTaut > 0 ? 0.15f : 1f;
                    float sw = Mathf.Sin(t * 2.2f) * (12f + 50f * threadSwing);
                    int n = thread.Count;
                    for (int i = 0; i < n; i++)
                    {
                        float u = i / (float)(n - 1);
                        // 실패 왼쪽에서 풀려 나와 아래로 늘어짐: 처음엔 옆으로, 끝으로 갈수록 아래로 + 흔들림
                        var q = from + new Vector2(-u * L * 0.35f + Mathf.Sin(u * 3f + t * 1.1f) * 8f + sw * u * u, -u * L * sag);
                        var im = thread[i];
                        im.rectTransform.anchoredPosition = q;
                        var c = im.color; c.a = a * (u * L < 12f ? 0 : 0.95f); im.color = c;
                    }
                    break;
                }
            }
        }

        // ---------- 그림 (코드로 만든 작은 텍스처) ----------
        static Sprite glowSpr, featherSpr, dotSpr;

        static Sprite GlowSpr()
        {
            if (glowSpr) return glowSpr;
            const int S = 64;
            var tx = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                float v = Mathf.Clamp01(1 - d); v *= v;
                tx.SetPixel(x, y, new Color(1, 1, 1, v));
            }
            tx.Apply();
            return glowSpr = Sprite.Create(tx, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }

        static Sprite FeatherSpr()
        {
            if (featherSpr) return featherSpr;
            const int W = 24, H = 72;
            var tx = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float v = (y + 0.5f) / H;                                   // 0 = 깃대 끝, 1 = 깃털 끝
                float half = Mathf.Sin(Mathf.Clamp01(v * 1.05f) * Mathf.PI) * 0.5f * Mathf.Lerp(0.55f, 1f, v);
                float u = Mathf.Abs((x + 0.5f) / W - 0.5f);
                float inside = Mathf.Clamp01((half - u) * W * 0.8f);
                float spine = Mathf.Clamp01(1 - u * W * 0.9f) * 0.45f;
                float barb = 0.85f + 0.15f * Mathf.Sin((v * 26f + u * 9f));   // 깃가지 결
                var c = new Color(barb * (1 - spine), barb * (1 - spine), barb * (1 - spine), inside);
                if (v < 0.12f && u < 0.06f) c = new Color(0.4f, 0.3f, 0.2f, 1);   // 깃대
                tx.SetPixel(x, y, c);
            }
            tx.Apply();
            return featherSpr = Sprite.Create(tx, new Rect(0, 0, W, H), new Vector2(0.5f, 0.1f));
        }

        static Sprite DotSpr()
        {
            if (dotSpr) return dotSpr;
            const int S = 16;
            var tx = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                tx.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 2.5f)));
            }
            tx.Apply();
            return dotSpr = Sprite.Create(tx, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }
    }
}
