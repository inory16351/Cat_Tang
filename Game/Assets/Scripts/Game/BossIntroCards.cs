// 보스 등장 연출 — 마지막 정지 카드 10종 (2026-11-21 사용자: 데이브 더 다이버랑 너무 비슷하지 않게 보스마다 변주)
// 현상수배(1장 중간) · 녹아내림(1장) · 발차판(2장 중간) · 기차 띠(2장) · 극장 커튼(3장 중간) · 거울 금(3장)
// 느와르 사선(4장 중간) · 흑백 반쪽(4장) · 글리치(5장 중간) · 시계판 11:59(5장)
// 모두 시간 정지(timeScale 0) 중 실시간으로 움직임. 각 카드는 card 아래에 직접 만들고 T초 동안 움직인 뒤 공통으로 사라짐.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public partial class BossIntro
    {
        RectTransform card;
        CanvasGroup cardGroup;
        Texture2D illTex;

        IEnumerator Freeze()
        {
            float T = def.main ? 1.8f : 1.6f;
            Time.timeScale = 0f;
            Music.Duck(0f, 0.05f);                                // 정지 순간 곡을 끊어 한 박자 침묵
            Snd(Sfx.Has("intro_impact") ? "intro_impact" : "e5b_stop", 0.85f);
            if (Sfx.Has("intro_stinger_" + def.Chapter)) Snd("intro_stinger_" + def.Chapter, 0.9f);
            Flash(Color.white, 1f);
            StartCoroutine(Letterbox(false));

            var go = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup));
            card = (RectTransform)go.transform; card.SetParent(freezeRoot, false); Full(card);
            cardGroup = go.GetComponent<CanvasGroup>();
            illTex = Resources.Load<Texture2D>("UI/BossIntro/" + def.id);

            IEnumerator body = def.card switch
            {
                IntroCard.Wanted => CardWanted(T),
                IntroCard.Melt => CardMelt(T),
                IntroCard.Board => CardBoard(T),
                IntroCard.Rail => CardRail(T),
                IntroCard.Curtain => CardCurtain(T),
                IntroCard.Shatter => CardShatter(T),
                IntroCard.NoirSlash => CardNoir(T),
                IntroCard.TwinSplit => CardTwin(T),
                IntroCard.Glitch => CardGlitch(T),
                _ => CardDial(T),
            };
            yield return body;

            // 공통 퇴장: 살짝 커지며 사라짐
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float u = EaseInOut(t / 0.22f);
                cardGroup.alpha = 1 - u;
                card.localScale = Vector3.one * (1 + 0.06f * u);
                yield return null;
            }
            Destroy(go);
        }

        // ---------- 카드 공통 도구 ----------
        static float Seg(float t, float start, float dur) => Mathf.Clamp01((t - start) / dur);

        Image Img(string name, Transform parent, Color c, Vector2 pos, Vector2 size, float rot = 0, Sprite spr = null)
        {
            var r = Rect(name, parent, c);
            r.sizeDelta = size; r.anchoredPosition = pos; r.localRotation = Quaternion.Euler(0, 0, rot);
            var im = r.GetComponent<Image>();
            if (spr) im.sprite = spr;
            return im;
        }

        Text Tx(Transform parent, string s, int size, Vector2 pos, Color c, TextAnchor a = TextAnchor.MiddleCenter, Font f = null, float w = 1400)
        {
            var t = new GameObject("T", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = f ? f : UIKit.Title;
            t.text = s; t.fontSize = size; t.alignment = a; t.color = c; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform; r.sizeDelta = new Vector2(w, size * 1.3f); r.anchoredPosition = pos;
            if (a == TextAnchor.MiddleLeft) r.pivot = new Vector2(0, 0.5f);
            else if (a == TextAnchor.MiddleRight) r.pivot = new Vector2(1, 0.5f);
            return t;
        }

        static void Fit(Text t, float maxW)
        {
            float pw = t.preferredWidth;
            if (pw > maxW) t.fontSize = Mathf.Max(36, Mathf.FloorToInt(t.fontSize * maxW / pw));
        }

        static T Fx<T>(Graphic g) where T : BaseMeshEffect => g.gameObject.AddComponent<T>();
        static void Shade(Graphic g, Color c, float d) { var s = g.gameObject.AddComponent<Shadow>(); s.effectColor = c; s.effectDistance = new Vector2(d, -d); }
        static void Line(Graphic g, Color c, float d) { var s = g.gameObject.AddComponent<Outline>(); s.effectColor = c; s.effectDistance = new Vector2(d, -d); }

        /// <summary>일러스트 (pivot 아래 가운데). keep = 위에서부터 보일 비율 (상반신 = 0.6)</summary>
        RawImage Ill(Transform parent, Vector2 pos, float height, float maxW, float keep)
        {
            if (!illTex) return null;
            var ri = new GameObject("Illust", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            ri.transform.SetParent(parent, false);
            ri.texture = illTex; ri.raycastTarget = false;
            ri.uvRect = new Rect(0, 1 - keep, 1, keep);
            float h = height, w = h * illTex.width / Mathf.Max(1f, illTex.height * keep);
            if (w > maxW) { h *= maxW / w; w = maxW; }
            var r = ri.rectTransform; r.sizeDelta = new Vector2(w, h); r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = pos;
            ri.color = new Color(1, 1, 1, 0);
            return ri;
        }
        float Keep => def.bust ? 0.6f : 1f;
        Vector2 IllPos => new(def.bust ? 520f : 470f, -550f);         // 카드 가운데 원점 기준 (화면 아래 = -540)

        string ChapterLabel => $"CHAPTER {def.Chapter}   ·   {(def.main ? "BOSS" : "MID BOSS")}";

        static Sprite circle, soft;
        static Sprite Circle() => circle ? circle : circle = MakeDisc(128, false);
        static Sprite Soft() => soft ? soft : soft = MakeDisc(256, true);
        static Sprite MakeDisc(int n, bool feather)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = feather ? Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d) : Mathf.Clamp01((1 - d) * n / 4f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
        static Sprite ringSpr;
        static Sprite RingSprite()
        {
            if (ringSpr) return ringSpr;
            const int n = 512;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float outer = Mathf.Clamp01((0.06f - Mathf.Abs(d - 0.93f)) * n / 2f);       // 굵은 바깥 테
                    float inner = Mathf.Clamp01((0.012f - Mathf.Abs(d - 0.8f)) * n / 2f);       // 가는 안 테
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Max(outer, inner) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return ringSpr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        /// <summary>왼쪽 글자 묶음 (챕터 · 칭호 · 이름 · 한글) — 여러 카드가 같이 씀</summary>
        class Block { public RectTransform root; public Text chap, title, name, ko; public Vector2 home; public bool punched; }

        Block MakeBlock(Transform parent, Vector2 pos, float rot, Color ink, Color shadow, float maxW = 780, bool skew = true)
        {
            var b = new Block();
            b.root = new GameObject("Block", typeof(RectTransform)).GetComponent<RectTransform>();
            b.root.SetParent(parent, false); b.root.anchoredPosition = pos; b.root.localRotation = Quaternion.Euler(0, 0, rot); b.home = pos;
            b.chap = Tx(b.root, ChapterLabel, 30, new Vector2(-575, 150), Alpha(ink, 0), TextAnchor.MiddleLeft, UIKit.BodyBold);
            b.title = Tx(b.root, def.titleEn, 52, new Vector2(-575, 100), Alpha(ink, 0), TextAnchor.MiddleLeft); Fit(b.title, maxW + 40);
            b.name = Tx(b.root, def.nameEn, 168, new Vector2(-575, -8), Alpha(ink, 0), TextAnchor.MiddleLeft); Fit(b.name, maxW);
            b.ko = Tx(b.root, def.KoLine, 44, new Vector2(-575, -128), Alpha(ink, 0), TextAnchor.MiddleLeft);
            Shade(b.name, Alpha(shadow, 0.95f), 9); Line(b.name, new Color(0, 0, 0, 0.85f), 3); Shade(b.ko, new Color(0, 0, 0, 0.6f), 3);
            if (skew) { Fx<SkewText>(b.name); Fx<SkewText>(b.title); }
            return b;
        }

        void AnimBlock(Block b, float t, float t0, float T)
        {
            float tu = EaseOut(Seg(t, t0, 0.22f));
            b.chap.color = Alpha(b.chap.color, tu); b.title.color = Alpha(b.title.color, tu);
            b.root.anchoredPosition = b.home + new Vector2(Mathf.Lerp(-260, 0, tu) + 14f * (t / T), 0);
            float nu = Seg(t, t0 + 0.08f, 0.14f);
            b.name.color = Alpha(b.name.color, nu);
            b.name.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, EaseOut(nu));
            if (!b.punched && t >= t0 + 0.08f) { b.punched = true; Snd("e_slam_1", 0.7f); }
            float ku = EaseOut(Seg(t, t0 + 0.22f, 0.22f));
            b.ko.color = Alpha(b.ko.color, ku);
        }

        void Quake(float t, float at, float power)
        {
            float k = Mathf.Clamp01(1 - (t - at) / 0.18f);
            freezeRoot.anchoredPosition = t >= at && k > 0 ? Random.insideUnitCircle * power * k : Vector2.zero;
        }

        Image Dim(Color c) { var d = Img("Dim", card, Alpha(c, 0), Vector2.zero, Vector2.zero); Full(d.rectTransform); return d; }

        // ============ 1장 중간 — 현상수배 포스터 ============
        IEnumerator CardWanted(float T)
        {
            var dim = Dim(new Color(0.32f, 0.2f, 0.08f));
            var ink = new Color(0.27f, 0.15f, 0.07f);
            var poster = Img("Poster", card, new Color(0.91f, 0.83f, 0.64f), Vector2.zero, new Vector2(700, 980));
            Line(poster, new Color(0.45f, 0.28f, 0.12f), 7);
            var pr = poster.rectTransform;
            var border = Img("Border", pr, Alpha(ink, 0.0f), Vector2.zero, new Vector2(640, 920)); Line(border, Alpha(ink, 0.8f), 3);
            var head = Tx(pr, def.titleEn, 46, new Vector2(0, 410), ink); Fit(head, 600);
            Img("Rule", pr, ink, new Vector2(0, 372), new Vector2(600, 5));
            var frame = Img("Frame", pr, new Color(0.18f, 0.12f, 0.07f), new Vector2(0, 90), new Vector2(600, 540));
            frame.gameObject.AddComponent<RectMask2D>();
            var ill = Ill(frame.rectTransform, new Vector2(0, -270), 760, 600, 0.62f);
            if (ill) { ill.color = new Color(1f, 0.9f, 0.74f, 1f); ill.rectTransform.anchoredPosition = new Vector2(0, 270 - ill.rectTransform.sizeDelta.y); }   // 위쪽(얼굴)부터 보이게
            var name = Tx(pr, def.nameEn, 120, new Vector2(0, -260), ink); Fit(name, 620);
            Tx(pr, def.KoLine, 40, new Vector2(0, -350), ink);
            Tx(pr, ChapterLabel, 26, new Vector2(0, -420), Alpha(ink, 0.8f), TextAnchor.MiddleCenter, UIKit.BodyBold);
            var stamp = Tx(card, "DEAD OR ALIVE", 70, new Vector2(150, -10), new Color(0.75f, 0.1f, 0.08f, 0));
            Line(stamp, new Color(0.75f, 0.1f, 0.08f, 0), 4);
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);
            bool slam = false, stamped = false;
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.55f * Seg(t, 0, 0.15f));
                float u = Seg(t, 0.02f, 0.3f), b = Back(u, 2.2f);
                pr.anchoredPosition = new Vector2(Mathf.Sin(t * 1.3f) * 4f, Mathf.LerpUnclamped(1400, 0, b));
                pr.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-28, -4, b));
                if (!slam && u >= 0.75f) { slam = true; Snd("e_slam_1"); }
                Quake(t, 0.26f, 14);
                float su = Seg(t, 0.5f, 0.12f);
                stamp.color = Alpha(stamp.color, su);
                stamp.GetComponent<Outline>().effectColor = Alpha(stamp.color, su);
                stamp.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, EaseOut(su));
                if (!stamped && t >= 0.56f) { stamped = true; Snd("e_slam_2", 0.8f); Snd("shot_1", 0.6f); }
                yield return null;
            }
        }

        // ============ 1장 — 녹아내림 (금물이 흘러내림) ============
        IEnumerator CardMelt(float T)
        {
            var dim = Dim(new Color(0.18f, 0.05f, 0.02f));
            var glow = Img("Glow", card, new Color(1f, 0.45f, 0.1f, 0), new Vector2(0, -560), new Vector2(2600, 900), 0, Soft());
            var ill = Ill(card, IllPos + new Vector2(0, -700), 1180, 1320, Keep);
            var drips = new List<(RectTransform bar, RectTransform drop, float len, float speed)>();
            for (int i = 0; i < 18; i++)
            {
                float w = Random.Range(16f, 48f);
                var c = Color.Lerp(new Color(1f, 0.72f, 0.15f), new Color(0.95f, 0.35f, 0.08f), Random.value);
                var bar = Img("Drip", card, c, new Vector2(Random.Range(-960f, 960f), 540), new Vector2(w, 0)).rectTransform;
                bar.pivot = new Vector2(0.5f, 1f);
                var drop = Img("Drop", card, c, Vector2.zero, Vector2.one * w * 1.5f, 0, Circle()).rectTransform;
                drips.Add((bar, drop, Random.Range(160f, 720f), Random.Range(0.6f, 1.4f)));
            }
            var blk = MakeBlock(card, new Vector2(-330, 30), -4, new Color(1f, 0.95f, 0.85f), new Color(0.55f, 0.15f, 0.03f));
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.62f * Seg(t, 0, 0.12f));
                glow.color = Alpha(glow.color, 0.55f * Seg(t, 0, 0.3f));
                foreach (var d in drips)
                {
                    float L = d.len * EaseOut(Mathf.Clamp01(t * d.speed / 0.9f));
                    d.bar.sizeDelta = new Vector2(d.bar.sizeDelta.x, L);
                    d.drop.anchoredPosition = d.bar.anchoredPosition + new Vector2(0, -L);
                }
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.08f, 0.38f));
                    ill.rectTransform.anchoredPosition = IllPos + new Vector2(-18f * t / T, Mathf.Lerp(-700, 0, iu));
                    ill.color = new Color(1, 1, 1, Seg(t, 0.08f, 0.15f));
                }
                AnimBlock(blk, t, 0.12f, T);
                float mu = Seg(t, 0.2f, 0.3f);                       // 이름이 늘어졌다 굳음
                blk.name.rectTransform.localScale = new Vector3(Mathf.Lerp(0.75f, 1f, Back(mu)), Mathf.Lerp(1.8f, 1f, Back(mu, 2.5f)), 1);
                Quake(t, 0.2f, 12);
                yield return null;
            }
        }

        // ============ 2장 중간 — 역 발차판 (글자가 한 자씩 넘어감) ============
        IEnumerator CardBoard(float T)
        {
            var dim = Dim(Color.black);
            var ill = Ill(card, IllPos, 1120, 1150, Keep);
            var board = Img("Board", card, new Color(0.07f, 0.07f, 0.08f), new Vector2(-330, 20), new Vector2(1150, 470));
            Line(board, new Color(0.85f, 0.65f, 0.25f), 5);
            var br = board.rectTransform;
            var amber = new Color(1f, 0.72f, 0.2f);
            Tx(br, $"PLATFORM {def.Chapter}   ·   {(def.main ? "BOSS" : "MID BOSS")}", 30, new Vector2(-540, 180), amber, TextAnchor.MiddleLeft, UIKit.BodyBold);
            var title = Tx(br, def.titleEn, 44, new Vector2(-540, 125), amber, TextAnchor.MiddleLeft); Fit(title, 900);
            var lampR = Img("LampR", br, new Color(1f, 0.15f, 0.1f), new Vector2(470, 180), Vector2.one * 34, 0, Circle());
            var lampG = Img("LampG", br, new Color(0.2f, 1f, 0.4f), new Vector2(515, 180), Vector2.one * 34, 0, Circle());
            string s = def.nameEn;
            int n = s.Length;
            float tw = Mathf.Min(86f, 1060f / n - 8f), th = tw * 1.45f, step = tw + 8f;
            var tiles = new List<(Text txt, RectTransform r, char want, float lockAt)>();
            float x0 = -step * (n - 1) / 2f;
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                if (s[i] == ' ') continue;
                var tile = Img("Tile", br, new Color(0.16f, 0.16f, 0.18f), new Vector2(x0 + i * step, 0), new Vector2(tw, th));
                Img("Mid", tile.rectTransform, Color.black, Vector2.zero, new Vector2(tw, 3));
                var tt = Tx(tile.rectTransform, " ", Mathf.RoundToInt(th * 0.72f), Vector2.zero, Color.white);
                tiles.Add((tt, tile.rectTransform, s[i], 0.3f + k * 0.07f)); k++;
            }
            var ko = Tx(br, def.KoLine, 40, new Vector2(0, -150), Color.white); Shade(ko, new Color(0, 0, 0, 0.6f), 3);
            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var locked = new bool[tiles.Count];
            float nextRoll = 0;
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.6f * Seg(t, 0, 0.12f));
                float bu = Seg(t, 0, 0.26f);
                br.anchoredPosition = new Vector2(-330, Mathf.LerpUnclamped(900, 20, Back(bu)));
                bool blink = Mathf.FloorToInt(t / 0.22f) % 2 == 0;
                lampR.color = Alpha(lampR.color, blink ? 1f : 0.2f); lampG.color = Alpha(lampG.color, blink ? 0.2f : 1f);
                bool roll = t >= nextRoll; if (roll) nextRoll = t + 0.04f;
                for (int i = 0; i < tiles.Count; i++)
                {
                    var tl = tiles[i];
                    if (t >= tl.lockAt)
                    {
                        if (!locked[i]) { locked[i] = true; tl.txt.text = tl.want.ToString(); if (!Sfx.Has("intro_flip")) Snd("ui_click_1", 0.5f); else if (i == 0) Snd("intro_flip", 0.8f); }
                        float bounce = Seg(t, tl.lockAt, 0.12f);
                        tl.r.localScale = new Vector3(1, Mathf.Lerp(0.6f, 1f, Back(bounce, 3f)), 1);
                    }
                    else if (t > 0.18f && roll) tl.txt.text = pool[Random.Range(0, pool.Length)].ToString();
                }
                ko.color = Alpha(ko.color, Seg(t, 0.3f + tiles.Count * 0.07f, 0.2f));
                if (ill)
                {
                    float iu = Seg(t, 0.1f, 0.34f);
                    ill.rectTransform.anchoredPosition = IllPos + new Vector2(Mathf.LerpUnclamped(950, 0, Back(iu)) - 20f * t / T, 0);
                    ill.color = new Color(1, 1, 1, Mathf.Clamp01(iu * 3));
                }
                Quake(t, 0.24f, 10);
                yield return null;
            }
        }

        // ============ 2장 — 기차 띠 (달려와 끼익 멈춤) ============
        IEnumerator CardRail(float T)
        {
            var dim = Dim(Color.black);
            var rail1 = Img("Rail", card, new Color(0.75f, 0.75f, 0.8f), new Vector2(0, 215), new Vector2(4000, 8));
            var rail2 = Img("Rail", card, new Color(0.75f, 0.75f, 0.8f), new Vector2(0, -195), new Vector2(4000, 8));
            var band = Img("Band", card, def.accent, new Vector2(3600, 10), new Vector2(4000, 380)).rectTransform;
            band.gameObject.AddComponent<RectMask2D>();
            var lines = new List<RectTransform>();
            for (int i = 0; i < 30; i++)
            {
                var l = Img("Line", band, new Color(1, 1, 1, Random.Range(0.1f, 0.3f)), new Vector2(Random.Range(-1900f, 1900f), Random.Range(-170f, 170f)), new Vector2(Random.Range(200, 900), Random.Range(3, 9))).rectTransform;
                lines.Add(l);
            }
            for (int i = 0; i < 6; i++) Img("Window", band, Alpha(def.accent2, 0.35f), new Vector2(-1500 + i * 600, 60), new Vector2(260, 120));
            var ill = Ill(card, IllPos, 1180, 1320, Keep);
            var blk = MakeBlock(card, new Vector2(-330, 30), 0, Color.white, def.accent2);
            var sparks = new List<(RectTransform r, Vector2 v)>();
            bool halted = false;
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                float dt = Time.unscaledDeltaTime;
                dim.color = Alpha(dim.color, 0.55f * Seg(t, 0, 0.12f));
                float bu = Seg(t, 0, 0.24f);
                band.anchoredPosition = new Vector2(Mathf.LerpUnclamped(3600, 0, Back(bu, 1.2f)), 10);
                if (!halted && bu >= 1)
                {
                    halted = true; Snd("e_steam"); Snd("e_slam_2", 0.7f);
                    for (int i = 0; i < 26; i++)
                        sparks.Add((Img("Spark", card, new Color(1f, Random.Range(0.6f, 0.95f), 0.2f), new Vector2(Random.Range(-700f, 700f), -195), new Vector2(Random.Range(8, 22), 5)).rectTransform,
                                    new Vector2(Random.Range(-500f, 500f), Random.Range(300f, 900f))));
                }
                float speed = halted ? Mathf.Lerp(6000f, 300f, Seg(t, 0.24f, 0.25f)) : 9000f;
                foreach (var l in lines) { var p = l.anchoredPosition; p.x -= speed * dt; if (p.x < -2000) p.x += 4000; l.anchoredPosition = p; }
                for (int i = 0; i < sparks.Count; i++)
                {
                    var sp = sparks[i]; sp.v += new Vector2(0, -2200f * dt);
                    sp.r.anchoredPosition += sp.v * dt; sp.r.localRotation = Quaternion.FromToRotation(Vector3.right, sp.v);
                    sparks[i] = sp;
                }
                if (ill)
                {
                    float iu = Seg(t, 0.04f, 0.3f);
                    ill.rectTransform.anchoredPosition = IllPos + new Vector2(Mathf.LerpUnclamped(1200, 0, Back(iu, 1.2f)) - 20f * t / T, 0);
                    ill.color = new Color(1, 1, 1, Mathf.Clamp01(iu * 3));
                }
                AnimBlock(blk, t, 0.14f, T);
                Quake(t, 0.24f, 16);
                yield return null;
            }
        }

        // ============ 3장 중간 — 극장 커튼 + 스포트라이트 + 간판 ============
        IEnumerator CardCurtain(float T)
        {
            // 2026-11-22 사용자: 멈춘 화면과 이어지게 — 커튼은 양옆 테두리로만 닫히고, 왼쪽에 보스 얼굴이 스포트라이트를 받은 채 보이고 일러스트는 오른쪽
            var face = new Vector2(-430, 120);
            var dim = Dim(Color.black);
            var spotFace = Img("SpotFace", card, new Color(1f, 0.97f, 0.85f, 0), face, new Vector2(900, 900), 0, Soft());
            var spotIll = Img("SpotIll", card, new Color(1f, 0.95f, 0.8f, 0), new Vector2(560, 40), new Vector2(1150, 1150), 0, Soft());
            var ill = Ill(card, new Vector2(560, -550), 1120, 1000, Keep);
            var sparkles = new List<Image>();
            for (int i = 0; i < 14; i++) sparkles.Add(Img("Sparkle", card, new Color(1, 1, 1, 0), new Vector2(560 + Random.Range(-420f, 420f), Random.Range(-380f, 420f)), Vector2.one * Random.Range(10, 26), 45));
            var red = new Color(0.55f, 0.04f, 0.08f);
            var gold = new Color(1f, 0.82f, 0.4f);
            RectTransform Curtain(float x)
            {
                var c = Img("Curtain", card, red, new Vector2(x, 0), new Vector2(1000, 1200)).rectTransform;
                for (int i = 0; i < 7; i++) Img("Fold", c, new Color(0.3f, 0.01f, 0.04f, 0.6f), new Vector2(-430 + i * 145, 0), new Vector2(34, 1200));
                return c;
            }
            var cl = Curtain(-1500); var cr = Curtain(1500);
            var valance = Img("Valance", card, red, new Vector2(0, 650), new Vector2(2000, 130)); Line(valance, gold, 5);
            var mq = Img("Marquee", card, new Color(0.22f, 0.04f, 0.06f), new Vector2(-400, -800), new Vector2(860, 290));
            Line(mq, gold, 6);
            var mr = mq.rectTransform;
            var bulbs = new List<Image>();
            for (int i = 0; i < 17; i++)
                for (int s2 = -1; s2 <= 1; s2 += 2) bulbs.Add(Img("Bulb", mr, new Color(1f, 0.85f, 0.4f), new Vector2(-400 + i * 50, s2 * 128), Vector2.one * 16, 0, Circle()));
            Tx(mr, ChapterLabel, 24, new Vector2(0, 92), gold, TextAnchor.MiddleCenter, UIKit.BodyBold);
            var title = Tx(mr, def.titleEn, 36, new Vector2(0, 52), gold); Fit(title, 780);
            var name = Tx(mr, def.nameEn, 112, new Vector2(0, -20), Color.white); Fit(name, 790); Shade(name, def.accent2, 7); Fx<SkewText>(name);
            var ko = Tx(mr, def.KoLine, 32, new Vector2(0, -96), new Color(1f, 0.93f, 0.8f));
            bool popped = false;
            Snd("e_conjure");
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.5f * Seg(t, 0, 0.12f));
                float cu = Back(Seg(t, 0f, 0.3f), 1.4f);                       // 커튼이 양옆에서 휙 닫혀 테두리가 됨
                cl.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-1500, -1250, cu), 0);
                cr.anchoredPosition = new Vector2(Mathf.LerpUnclamped(1500, 1250, cu), 0);
                valance.rectTransform.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(650, 490, Back(Seg(t, 0.02f, 0.28f), 1.6f)));
                spotFace.color = Alpha(spotFace.color, 0.2f * Seg(t, 0.06f, 0.15f));
                spotIll.color = Alpha(spotIll.color, 0.16f * Seg(t, 0.2f, 0.2f));
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.15f, 0.3f));
                    ill.color = new Color(1, 1, 1, iu);
                    ill.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(900, 560, iu) - 14f * t / T, -550);
                }
                mr.anchoredPosition = new Vector2(-400, Mathf.LerpUnclamped(-800, -330, Back(Seg(t, 0.2f, 0.25f), 1.3f)));
                bool on = Mathf.FloorToInt(t / 0.12f) % 2 == 0;
                for (int i = 0; i < bulbs.Count; i++) bulbs[i].color = Alpha(bulbs[i].color, (i % 2 == 0) == on ? 1f : 0.25f);
                for (int i = 0; i < sparkles.Count; i++) sparkles[i].color = Alpha(Color.white, Seg(t, 0.3f, 0.1f) * (0.5f + 0.5f * Mathf.Sin(t * 14f + i * 1.7f)));
                float nu = Seg(t, 0.36f, 0.14f);
                name.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, EaseOut(nu));
                name.color = Alpha(name.color, nu); ko.color = Alpha(ko.color, Seg(t, 0.48f, 0.2f));
                if (!popped && t >= 0.36f) { popped = true; Snd("e_slam_1", 0.7f); }
                Quake(t, 0.4f, 10);
                yield return null;
            }
        }

        // ============ 3장 — 거울 금 + 반사 잔상 ============
        IEnumerator CardShatter(float T)
        {
            var dim = Dim(new Color(0.55f, 0.75f, 0.95f));
            var center = new Vector2(-180, 60);
            var cracks = new List<(RectTransform r, float len)>();
            for (int i = 0; i < 18; i++)
            {
                float ang = i * 20f + Random.Range(-8f, 8f), len = Random.Range(380f, 1500f);
                var r = Img("Crack", card, new Color(1, 1, 1, 0.9f), center, new Vector2(0, Random.Range(2.5f, 6f)), ang).rectTransform;
                r.pivot = new Vector2(0, 0.5f); cracks.Add((r, len));
                if (i % 2 == 0)
                {
                    var mid = center + (Vector2)(Quaternion.Euler(0, 0, ang) * Vector3.right) * len * Random.Range(0.3f, 0.6f);
                    var br = Img("Branch", card, new Color(1, 1, 1, 0.75f), mid, new Vector2(0, 2.5f), ang + Random.Range(-40f, 40f)).rectTransform;
                    br.pivot = new Vector2(0, 0.5f); cracks.Add((br, len * 0.35f));
                }
            }
            bool wide = illTex && illTex.width > illTex.height;                       // 가로 그림(고래, 2026-11-22 사용자: 가로로 긴 그림으로)
            var P0 = wide ? new Vector2(360, -560) : IllPos;
            float ih = wide ? 930 : 1120, iw = wide ? 1440 : 1150;
            var ghost = Ill(card, P0 + new Vector2(-260, 0), ih, iw, wide ? 1f : Keep);
            if (ghost) ghost.rectTransform.localScale = new Vector3(-1, 1, 1);
            var ill = Ill(card, P0, ih, iw, wide ? 1f : Keep);
            var glint = Img("Glint", card, new Color(1, 1, 1, 0.5f), new Vector2(-900, 0), new Vector2(90, 2200), 28);
            var blk = MakeBlock(card, wide ? new Vector2(-330, 250) : new Vector2(-330, 30), -6, Color.white, new Color(0.15f, 0.35f, 0.6f));
            Snd("e5_phantom_break_1"); Snd("e_glass");
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.32f * Seg(t, 0, 0.1f));
                foreach (var c in cracks)
                {
                    c.r.sizeDelta = new Vector2(c.len * Expo(Seg(t, 0, 0.14f)), c.r.sizeDelta.y);
                    c.r.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.95f, 0.45f, Seg(t, 0.2f, 0.4f)));
                }
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.12f, 0.3f));
                    ill.rectTransform.anchoredPosition = P0 + new Vector2(Mathf.Lerp(260, 0, iu) - 16f * t / T, 0);
                    ill.color = new Color(1, 1, 1, iu);
                    ghost.rectTransform.anchoredPosition = P0 + new Vector2(Mathf.Lerp(0, -330, iu) + 16f * t / T, 0);
                    ghost.color = new Color(0.7f, 0.9f, 1f, 0.28f * iu);
                }
                glint.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-200, 1300, Seg(t, 0.45f, 0.4f)), 0);
                glint.color = new Color(1, 1, 1, 0.45f * (Seg(t, 0.45f, 0.05f) - Seg(t, 0.8f, 0.05f)));
                AnimBlock(blk, t, 0.14f, T);
                Quake(t, 0.02f, 16);
                yield return null;
            }
        }

        // ============ 4장 중간 — 느와르 붉은 사선 ============
        IEnumerator CardNoir(float T)
        {
            const float ang = -18f;
            var dim = Dim(Color.black);
            var normal = (Vector2)(Quaternion.Euler(0, 0, ang) * Vector3.down);
            var white = Img("White", card, new Color(0.93f, 0.93f, 0.93f), normal * 20, new Vector2(4200, 2200), ang).rectTransform;
            white.pivot = new Vector2(0.5f, 1f);
            var splats = new List<Image>();
            for (int i = 0; i < 14; i++)
            {
                var along = (Vector2)(Quaternion.Euler(0, 0, ang) * Vector3.right) * Random.Range(-200f, 1000f);
                splats.Add(Img("Splat", card, new Color(0.85f, 0.05f, 0.08f, 0), along + normal * Random.Range(-60f, 60f), Vector2.one * Random.Range(10, 46), 0, Circle()));
            }
            var ill = Ill(card, IllPos, 1120, 1150, Keep);
            var slash = Img("Slash", card, def.accent, Vector2.zero, new Vector2(0, 12), ang).rectTransform;
            var blk = MakeBlock(card, new Vector2(-330, 190), ang * 0.5f, Color.white, def.accent);
            blk.ko.color = Alpha(def.accent, 0); blk.title.color = Alpha(def.accent, 0);
            Snd("e4_scythe_1");
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.88f * Seg(t, 0, 0.06f));
                float su = Expo(Seg(t, 0.02f, 0.1f));
                slash.sizeDelta = new Vector2(4200 * su, Mathf.Lerp(26f, 12f, Seg(t, 0.12f, 0.3f)));
                float wu = EaseOut(Seg(t, 0.12f, 0.22f));
                white.anchoredPosition = normal * Mathf.Lerp(1400, 20, wu);
                foreach (var s in splats) { float k = Seg(t, 0.1f, 0.08f); s.color = Alpha(s.color, k); s.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k); }
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.16f, 0.2f));
                    ill.color = new Color(1, 1, 1, iu);
                    ill.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, iu);
                    ill.rectTransform.anchoredPosition = IllPos + new Vector2(-16f * t / T, 0);
                }
                AnimBlock(blk, t, 0.14f, T);
                Quake(t, 0.03f, 18);
                yield return null;
            }
        }

        // ============ 4장 — 흑백 반쪽 화면이 맞붙음 (이름도 양쪽) ============
        IEnumerator CardTwin(float T)
        {
            var left = Img("White", card, new Color(0.94f, 0.94f, 0.94f), Vector2.zero, Vector2.zero).rectTransform;
            left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0.5f, 1); left.offsetMin = left.offsetMax = Vector2.zero;
            var right = Img("Black", card, new Color(0.05f, 0.05f, 0.06f), Vector2.zero, Vector2.zero).rectTransform;
            right.anchorMin = new Vector2(0.5f, 0); right.anchorMax = new Vector2(1, 1); right.offsetMin = right.offsetMax = Vector2.zero;
            var seam = Img("Seam", card, def.accent2, Vector2.zero, new Vector2(0, 1200));
            var ill = Ill(card, new Vector2(0, -550), 1120, 1150, Keep);
            var parts = def.nameEn.Split('&');
            string a = parts[0].Trim(), b = parts.Length > 1 ? parts[1].Trim() : "";
            var koParts = def.nameKo.Split('&');
            var black = new Color(0.06f, 0.06f, 0.07f);
            var nA = Tx(card, a, 150, new Vector2(-560, -20), Alpha(black, 0)); Fit(nA, 640); Fx<SkewText>(nA); Shade(nA, Alpha(def.accent2, 0.9f), 8);
            var nB = Tx(card, b, 150, new Vector2(560, -20), Alpha(Color.white, 0)); Fit(nB, 640); Fx<SkewText>(nB); Shade(nB, Alpha(def.accent2, 0.9f), 8);
            var kA = Tx(card, koParts[0].Trim(), 44, new Vector2(-560, -130), Alpha(black, 0));
            var kB = Tx(card, koParts.Length > 1 ? koParts[1].Trim() : "", 44, new Vector2(560, -130), Alpha(Color.white, 0));
            var amp = Tx(card, "&", 130, new Vector2(0, 330), Alpha(def.accent2, 0)); Line(amp, Color.black, 4);
            var title = Tx(card, def.titleEn, 56, new Vector2(0, 450), Alpha(def.accent2, 0)); Line(title, Color.black, 3); Fx<SkewText>(title);
            var chap = Tx(card, ChapterLabel, 28, new Vector2(0, 395), new Color(0.6f, 0.6f, 0.62f, 0), TextAnchor.MiddleCenter, UIKit.BodyBold);
            var ko = Tx(card, def.titleKo, 44, new Vector2(0, -470), Alpha(def.accent2, 0)); Line(ko, Color.black, 3);
            bool met = false;
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                float u = Expo(Seg(t, 0, 0.22f));
                left.anchoredPosition = new Vector2(Mathf.Lerp(-960, 0, u), 0);
                right.anchoredPosition = new Vector2(Mathf.Lerp(960, 0, u), 0);
                if (!met && u >= 0.98f) { met = true; Snd("e4_bell_2"); Snd("e_slam_1", 0.7f); }
                seam.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0, 16, Seg(t, 0.22f, 0.08f)), 1200);
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.24f, 0.22f));
                    ill.color = new Color(1, 1, 1, iu);
                    ill.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, iu);
                }
                float na = Seg(t, 0.3f, 0.14f), nb = Seg(t, 0.38f, 0.14f);
                nA.color = Alpha(black, na); nA.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, EaseOut(na));
                nB.color = Alpha(Color.white, nb); nB.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, EaseOut(nb));
                kA.color = Alpha(black, Seg(t, 0.46f, 0.2f)); kB.color = Alpha(Color.white, Seg(t, 0.5f, 0.2f));
                float tu = Seg(t, 0.26f, 0.2f);
                amp.color = Alpha(def.accent2, Seg(t, 0.44f, 0.1f)); title.color = Alpha(def.accent2, tu); chap.color = Alpha(chap.color, tu);
                ko.color = Alpha(def.accent2, Seg(t, 0.55f, 0.2f));
                Quake(t, 0.22f, 18);
                yield return null;
            }
        }

        // ============ 5장 중간 — 글리치 (RGB 갈라짐 · 주사선 · 글자 뒤섞임) ============
        IEnumerator CardGlitch(float T)
        {
            var dim = Dim(new Color(0.06f, 0.03f, 0.12f));
            var illR = Ill(card, IllPos, 1120, 1150, Keep);
            var illC = Ill(card, IllPos, 1120, 1150, Keep);
            var ill = Ill(card, IllPos, 1120, 1150, Keep);
            var bars = new List<Image>();
            for (int i = 0; i < 18; i++) bars.Add(Img("Scan", card, new Color(1, 1, 1, 0), Vector2.zero, new Vector2(2200, 4)));
            var blocks = new List<Image>();
            for (int i = 0; i < 6; i++) blocks.Add(Img("Block", card, Alpha(def.accent, 0), Vector2.zero, new Vector2(300, 40)));
            var root2 = new GameObject("Name", typeof(RectTransform)).GetComponent<RectTransform>(); root2.SetParent(card, false); root2.anchoredPosition = new Vector2(-330, 30);
            Text N(Color c) { var t = Tx(root2, def.nameEn, 190, new Vector2(-575, 0), c, TextAnchor.MiddleLeft); Fit(t, 780); return t; }
            var nR = N(new Color(1f, 0.1f, 0.35f, 0)); var nC = N(new Color(0.1f, 1f, 1f, 0)); var nW = N(new Color(1, 1, 1, 0));
            var chap = Tx(root2, $"CHAPTER {def.Chapter}   ·   ???", 30, new Vector2(-575, 140), new Color(0.8f, 0.75f, 1f, 0), TextAnchor.MiddleLeft, UIKit.BodyBold);
            var ko = Tx(root2, def.KoLine, 46, new Vector2(-575, -125), new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#%&?!/<>01";
            string full = def.nameEn;
            float nextJump = 0;
            Snd(Sfx.Has("intro_glitch") ? "intro_glitch" : "e5_phantom_split");
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.72f * Seg(t, 0, 0.08f));
                bool jump = t >= nextJump; if (jump) nextJump = t + Random.Range(0.03f, 0.08f);
                if (jump)
                {
                    foreach (var b in bars)
                    {
                        b.rectTransform.anchoredPosition = new Vector2(0, Random.Range(-540f, 540f));
                        b.rectTransform.sizeDelta = new Vector2(2200, Random.Range(2f, 30f));
                        b.color = (Random.value < 0.5f ? new Color(1, 1, 1, 1) : new Color(0.4f, 1f, 1f, 1)) * new Color(1, 1, 1, Random.Range(0.04f, 0.2f));
                    }
                    foreach (var b in blocks)
                    {
                        b.rectTransform.anchoredPosition = new Vector2(Random.Range(-900f, 900f), Random.Range(-500f, 500f));
                        b.rectTransform.sizeDelta = new Vector2(Random.Range(80f, 500f), Random.Range(8f, 50f));
                        b.color = Alpha(Random.value < 0.5f ? def.accent : def.accent2, Random.value < 0.4f ? 0.7f : 0f);
                    }
                }
                float k = Seg(t, 0.06f, 0.2f);
                float j = Random.value < 0.25f ? 16f : 4f;
                if (ill)
                {
                    ill.color = new Color(1, 1, 1, Random.value < 0.06f ? 0.35f : k);
                    illR.color = new Color(1f, 0.15f, 0.4f, 0.45f * k); illC.color = new Color(0.1f, 1f, 1f, 0.45f * k);
                    illR.rectTransform.anchoredPosition = IllPos + new Vector2(-j, Random.Range(-3f, 3f));
                    illC.rectTransform.anchoredPosition = IllPos + new Vector2(j, Random.Range(-3f, 3f));
                }
                int solved = Mathf.FloorToInt(Seg(t, 0.25f, 0.6f) * full.Length);
                var sb = new System.Text.StringBuilder();
                for (int c = 0; c < full.Length; c++) sb.Append(c < solved || full[c] == ' ' ? full[c] : pool[Random.Range(0, pool.Length)]);
                if (Random.value < 0.06f) sb[Random.Range(0, sb.Length)] = pool[Random.Range(0, pool.Length)];
                string str = sb.ToString();
                float nu = Seg(t, 0.18f, 0.1f);
                nW.text = nR.text = nC.text = str;
                nW.color = Alpha(nW.color, nu); nR.color = Alpha(nR.color, 0.8f * nu); nC.color = Alpha(nC.color, 0.8f * nu);
                float off = Random.value < 0.2f ? Random.Range(8f, 22f) : 5f;
                nR.rectTransform.anchoredPosition = new Vector2(-575 - off, Random.Range(-3f, 3f));
                nC.rectTransform.anchoredPosition = new Vector2(-575 + off, Random.Range(-3f, 3f));
                chap.color = Alpha(chap.color, Seg(t, 0.12f, 0.2f)); ko.color = Alpha(ko.color, Seg(t, 0.5f, 0.2f));
                if (Random.value < 0.03f) Flash(Color.white, 0.15f);
                Quake(t, 0.18f, 10);
                yield return null;
            }
        }

        // ============ 5장 — 시계판이 11:59에 멈춤 ============
        IEnumerator CardDial(float T)
        {
            var dim = Dim(new Color(0.08f, 0.03f, 0.16f));
            var cen = new Vector2(430, 40);
            var halo = Img("Halo", card, Alpha(def.accent, 0), cen, Vector2.one * 1500, 0, Soft());
            var ring = Img("Ring", card, Alpha(def.accent2, 0), cen, Vector2.one * 1060, 0, RingSprite()).rectTransform;
            string[] roman = { "XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI" };
            var nums = new List<Text>();
            for (int i = 0; i < 12; i++)
            {
                var p = cen + (Vector2)(Quaternion.Euler(0, 0, -30f * i) * Vector3.up) * 400f;
                nums.Add(Tx(card, roman[i], 50, p, Alpha(def.accent2, 0)));
            }
            var ticks = new List<Image>();
            for (int i = 0; i < 60; i++)
            {
                var p = cen + (Vector2)(Quaternion.Euler(0, 0, -6f * i) * Vector3.up) * 455f;
                ticks.Add(Img("Tick", card, Alpha(def.accent2, 0), p, new Vector2(i % 5 == 0 ? 8 : 4, i % 5 == 0 ? 30 : 14), -6f * i));
            }
            var hourH = Img("Hour", card, Alpha(def.accent2, 0), cen, new Vector2(22, 260)).rectTransform; hourH.pivot = new Vector2(0.5f, 0.08f);
            var minH = Img("Minute", card, Alpha(def.accent2, 0), cen, new Vector2(12, 390)).rectTransform; minH.pivot = new Vector2(0.5f, 0.06f);
            var hub = Img("Hub", card, Alpha(def.accent2, 0), cen, Vector2.one * 46, 0, Circle());
            var ill = Ill(card, IllPos, 1180, 1320, Keep);
            var blk = MakeBlock(card, new Vector2(-360, 30), -5, Color.white, new Color(0.3f, 0.1f, 0.5f));
            bool ticked = false, belled = false;
            float hourEnd = -(11 * 30f + 59 * 0.5f), minEnd = -59 * 6f;
            for (float t = 0; t < T; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                dim.color = Alpha(dim.color, 0.7f * Seg(t, 0, 0.12f));
                float ru = EaseOut(Seg(t, 0, 0.3f));
                ring.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, ru);
                ring.localRotation = Quaternion.Euler(0, 0, -8f * t);
                ring.GetComponent<Image>().color = Alpha(def.accent2, ru);
                halo.color = Alpha(def.accent, 0.35f * ru);
                for (int i = 0; i < 12; i++) nums[i].color = Alpha(def.accent2, Seg(t, 0.05f + i * 0.02f, 0.1f));
                for (int i = 0; i < 60; i++) ticks[i].color = Alpha(def.accent2, 0.8f * Seg(t, 0.03f + i * 0.004f, 0.1f));
                float hu = EaseOut(Seg(t, 0.08f, 0.5f));
                hourH.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(hourEnd + 720, hourEnd, hu));
                float mStep = t < 0.58f ? Mathf.Lerp(minEnd + 1440, minEnd + 6, hu) : minEnd;    // 마지막 한 칸은 째깍
                minH.localRotation = Quaternion.Euler(0, 0, mStep);
                foreach (var g in new Graphic[] { hourH.GetComponent<Image>(), minH.GetComponent<Image>(), hub }) g.color = Alpha(def.accent2, Seg(t, 0.06f, 0.1f));
                if (!ticked && t >= 0.58f) { ticked = true; Snd("e4_tick_1"); Quake(t, t, 8); }
                if (!belled && t >= 0.62f) { belled = true; Snd("e5b_bell"); Flash(def.accent, 0.5f); }
                if (ill)
                {
                    float iu = EaseOut(Seg(t, 0.15f, 0.3f));
                    ill.color = new Color(1, 1, 1, iu);
                    ill.rectTransform.anchoredPosition = IllPos + new Vector2(-16f * t / T, Mathf.Lerp(-120, 0, iu));
                }
                AnimBlock(blk, t, 0.2f, T);
                Quake(t, 0.62f, 14);
                yield return null;
            }
        }
    }
}
