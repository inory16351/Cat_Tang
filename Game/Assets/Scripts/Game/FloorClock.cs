// 1장 보스 '시각 패턴' 바닥 시계판 (2026-11-17 사용자: 기믹이 잘 안 보임 → 바닥에 큰 시계판, 그 시각 자리가 안전 지대)
// - 그림: Codex(docs/images/ui_src/prompts/boss_floor_clock.txt → docs/tools/ui_boss_clock_cut.py → Resources/FX/boss_floor_clock · boss_floor_hand)
// - 보스 방 바닥에 금빛 시계판이 퍼지며 나타나고(0.35초) → 큰 시곗바늘이 12시에서 목표 시각까지 째깍째깍 돌아가 멈춤 → 그 숫자 자리에 안전 지대(BossFx.Safe 원)
// - 끝나면 스르르 사라짐. 그림이 없으면 아무것도 안 그림(안전 지대 원만 남음)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class FloorClock : MonoBehaviour
    {
        public const float NumeralRing = 0.72f;          // 숫자 줄이 시계판 반지름의 몇 배 자리인지 (그림에서 잼: 아라비아 숫자판 2026-11-18, 12 · 3 · 6 · 9 가운데 평균 0.727)
        const float HubX = 0.145f, TipX = 0.96f;         // 바늘 그림: 축 고리 가운데 · 끝 (가로 비율)
        static Texture2D dialTex, handTex;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Renderer dial, hand;
        Transform pivot;
        MaterialPropertyBlock mpb;
        float radius;

        /// <summary>시계판 위 hour시 숫자 자리 (월드). 12시 = +Z(화면 위쪽)</summary>
        public static Vector3 HourPoint(Vector3 center, float radius, int hour)
        {
            float a = hour * 30f * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius * NumeralRing;
        }

        /// <summary>center에 반지름 radius 시계판, 바늘은 hour시로 돌아가 멈춤. dur 뒤 사라짐</summary>
        public static FloorClock Show(Vector3 center, float radius, int hour, float dur)
        {
            if (!dialTex) dialTex = Resources.Load<Texture2D>("FX/boss_floor_clock");
            if (!handTex) handTex = Resources.Load<Texture2D>("FX/boss_floor_hand");
            if (!dialTex) return null;
            var go = new GameObject("FloorClock");
            go.transform.position = new Vector3(center.x, center.y + 0.025f, center.z);
            var fc = go.AddComponent<FloorClock>();
            fc.radius = radius;
            fc.mpb = new MaterialPropertyBlock();
            // 시계판 그림: 자르는 스크립트가 바깥 고리에 딱 맞춰 자름 → 그림 폭 = 지름
            fc.dial = Flat(go.transform, "Dial", dialTex, Vector3.zero, new Vector2(radius * 2f, radius * 2f), 2980);
            if (handTex)
            {
                // 바늘 그림: 오른쪽(3시)을 가리킴 → 축을 시계판 가운데에, 끝은 숫자 줄 바로 안쪽까지(숫자를 안 가림)
                fc.pivot = new GameObject("HandPivot").transform;
                fc.pivot.SetParent(go.transform, false);
                fc.pivot.localPosition = new Vector3(0, 0.01f, 0);
                float len = radius * 0.56f, w = len / (TipX - HubX), h = w * handTex.height / handTex.width;
                fc.hand = Flat(fc.pivot, "Hand", handTex, new Vector3((0.5f - HubX) * w, 0, 0), new Vector2(w, h), 2981);
                fc.pivot.localRotation = Quaternion.Euler(0, -90f, 0);       // 12시에서 출발
            }
            fc.StartCoroutine(fc.Run(hour, dur));
            return fc;
        }

        static Renderer Flat(Transform parent, string name, Texture2D tex, Vector3 local, Vector2 size, int queue)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = local;
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);             // 바닥에 눕힘 (그림 위쪽 = +Z = 12시)
            q.transform.localScale = new Vector3(size.x, size.y, 1);
            var mat = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { renderQueue = queue, mainTexture = tex };
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void Tint(Renderer r, float a, float glow = 0f)
        {
            if (!r) return;
            var c = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.75f), glow);
            c.a = a;
            mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);
        }

        IEnumerator Run(int hour, float dur)
        {
            float t = 0;
            // 1) 퍼지며 나타남
            const float In = 0.35f;
            while (t < In)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / In), e = 1 - (1 - k) * (1 - k);
                transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, e);
                Tint(dial, 0.9f * k); Tint(hand, k);
                yield return null;
            }
            transform.localScale = Vector3.one;
            // 2) 바늘이 12시부터 한 칸씩 째깍째깍 (칸마다 0.06초, 마지막 칸은 툭 넘었다 돌아옴)
            if (pivot)
            {
                int steps = hour % 12 == 0 ? 12 : hour % 12;
                for (int i = 1; i <= steps; i++)
                {
                    float from = (i - 1) * 30f - 90f, to = i * 30f - 90f, s = 0;
                    while (s < 1)
                    {
                        s += Time.deltaTime / 0.06f;
                        float e = Mathf.Clamp01(s);
                        float over = i == steps ? Mathf.Sin(e * Mathf.PI) * 6f : 0f;
                        pivot.localRotation = Quaternion.Euler(0, Mathf.Lerp(from, to, e * e * (3 - 2 * e)) + over, 0);
                        yield return null;
                    }
                    t += 0.06f;
                }
            }
            // 3) 남은 시간: 은은하게 숨 쉼, 마지막 1초는 빠르게 깜빡여 곧 끝남을 알림
            float end = dur - 0.3f;
            while (t < end)
            {
                t += Time.deltaTime;
                float left = end - t;
                float pulse = left < 1f ? 0.5f + 0.5f * Mathf.Sin(t * 22f) : 0.5f + 0.5f * Mathf.Sin(t * 4f);
                Tint(dial, 0.75f + 0.2f * pulse, pulse * 0.6f);
                Tint(hand, 1f, pulse * 0.6f);
                yield return null;
            }
            // 4) 사라짐
            float o = 0;
            while (o < 0.3f)
            {
                o += Time.deltaTime;
                float k = 1 - Mathf.Clamp01(o / 0.3f);
                Tint(dial, 0.9f * k); Tint(hand, k);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r && r.sharedMaterial) Destroy(r.sharedMaterial);
        }
    }
}
