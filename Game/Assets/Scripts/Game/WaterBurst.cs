// 3장 분수 샘 물 폭발 — 진짜 3D 물 (2026-11-18 사용자: 그림 · 플립북이 물 같지 않음 → 블렌더 3D로, 확실히 물처럼)
// 메시: Art/tools/blender_fx.py의 fx_water_column(물기둥) · fx_water_crown(버섯 물 왕관) · fx_water_drop(물방울) · fx_water_splash(밑동 물보라 왕관)
//       → 메뉴 CatTang/FX 메시 가져오기 → Resources/FX/Meshes. 셰이더 CatTang/Water (반투명 · 프레넬 · 흐르는 물결 · 반짝 · 외곽선)
// 움직임 (약 1.2초): 물기둥이 0.18초에 솟구침 → 버티며 출렁 → 무너져 가라앉음 / 꼭대기에서 버섯 왕관이 피어 퍼지며 내려앉음 /
//       물방울 18개가 포물선으로 튀어 꼬리를 끌고 떨어지다 바닥에 닿으면 작은 물보라 / 밑동 물보라 왕관이 옆으로 퍼짐
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class WaterBurst : MonoBehaviour
    {
        static Material mat;
        static readonly int FadeId = Shader.PropertyToID("_Fade"), FlowId = Shader.PropertyToID("_Flow"), FoamId = Shader.PropertyToID("_FoamY");

        class Part { public Transform t; public MeshRenderer r; public MaterialPropertyBlock b; }
        class Drop { public Part p; public Vector3 pos, vel; public float size; }
        class Mini { public Part p; public float born; public float size; }

        Part column, crown, splash;
        readonly List<Drop> drops = new();
        readonly List<Mini> minis = new();
        Vector3 ground;
        float born, H, W, scale;
        bool dropsOut;

        public const float Life = 1.3f;

        /// <summary>ground = 바닥 위 분수 자리. scale 1 = 물기둥 높이 3.6m · 지름 1.5m. 메시나 셰이더가 없으면 false</summary>
        public static bool Spawn(Vector3 ground, float scale = 1f)
        {
            if (!FxMeshes.Lib("fx_water_column") || !Mat()) return false;
            var go = new GameObject("WaterBurst");
            go.transform.position = ground;
            var w = go.AddComponent<WaterBurst>();
            w.ground = ground; w.scale = scale; w.born = Time.time;
            w.H = 3.6f * scale; w.W = 1.5f * scale;
            w.column = w.Make("fx_water_column", 2.6f, 99f);
            w.crown = w.Make("fx_water_crown", 1.2f, 0.09f);
            w.splash = w.Make("fx_water_splash", 1.2f, 0.3f);
            w.crown.t.gameObject.SetActive(false);
            w.Pose(0f);
            return true;
        }

        static Material Mat()
        {
            if (mat) return mat;
            var sh = Shader.Find("CatTang/Water");
            if (!sh) return null;
            mat = new Material(sh);
            return mat;
        }

        Part Make(string mesh, float flow, float foamY)
        {
            var go = new GameObject(mesh);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = FxMeshes.Lib(mesh);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var p = new Part { t = go.transform, r = r, b = new MaterialPropertyBlock() };
            p.b.SetFloat(FlowId, flow);
            p.b.SetFloat(FoamId, foamY);
            Fade(p, 1f);
            return p;
        }

        static void Fade(Part p, float a)
        {
            p.b.SetFloat(FadeId, Mathf.Clamp01(a));
            p.r.SetPropertyBlock(p.b);
        }

        static float Out(float x, float pow = 3f) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), pow);

        void Update()
        {
            float t = Time.time - born;
            Pose(t);
            if (!dropsOut && t >= 0.16f) { dropsOut = true; Throw(); }
            float dt = Time.deltaTime, g = 22f * scale;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.vel.y -= g * dt;
                d.pos += d.vel * dt;
                if (d.pos.y <= ground.y + 0.05f)
                {
                    AddMini(new Vector3(d.pos.x, ground.y + 0.02f, d.pos.z), d.size);
                    Destroy(d.p.t.gameObject);
                    drops.RemoveAt(i);
                    continue;
                }
                d.p.t.position = d.pos;
                d.p.t.rotation = Quaternion.FromToRotation(Vector3.up, -d.vel.normalized);   // 꼬리를 날아온 쪽으로
                float stretch = 1f + Mathf.Min(1.2f, d.vel.magnitude * 0.07f);
                d.p.t.localScale = new Vector3(d.size, d.size * stretch, d.size);
            }
            for (int i = minis.Count - 1; i >= 0; i--)
            {
                var m = minis[i];
                float k = (Time.time - m.born) / 0.3f;
                if (k >= 1f) { Destroy(m.p.t.gameObject); minis.RemoveAt(i); continue; }
                float s = m.size * (0.8f + 1.6f * Out(k));
                m.p.t.localScale = new Vector3(s, m.size * 1.6f * (1f - k), s);
                Fade(m.p, 1f - k * k);
            }
            if (t > Life && drops.Count == 0 && minis.Count == 0) Destroy(gameObject);
        }

        void Pose(float t)
        {
            // 물기둥
            float rise = Out(t / 0.18f);
            float c = Mathf.Clamp01((t - 0.42f) / 0.4f);                    // 무너짐
            float wob = 1f + 0.05f * Mathf.Sin(t * 34f) * (1f - c);
            float colH = H * Mathf.Max(0.02f, rise * (1f - 0.95f * Mathf.Pow(c, 1.4f)));
            float colW = W * (0.7f + 0.3f * rise) * (1f + 0.25f * c) * wob;
            column.t.localPosition = Vector3.zero;
            column.t.localScale = new Vector3(colW, colH, colW);
            column.t.localRotation = Quaternion.Euler(0, t * 120f, 0);
            column.t.gameObject.SetActive(c < 1f);
            Fade(column, 1f - c * c);
            // 버섯 물 왕관: 꼭대기에서 피어 넓게 퍼지며 내려앉음
            if (t >= 0.1f)
            {
                crown.t.gameObject.SetActive(true);
                float b = Out((t - 0.1f) / 0.32f);
                float spread = W * (0.75f + 0.95f * b + 0.4f * c);
                float tall = W * (0.5f + 0.4f * b) * (1f - 0.6f * c);
                crown.t.localPosition = new Vector3(0, colH * 1.04f, 0);
                crown.t.localScale = new Vector3(spread, tall, spread);
                crown.t.localRotation = Quaternion.Euler(0, -t * 90f, 0);
                Fade(crown, 1f - Mathf.Clamp01((t - 0.55f) / 0.4f));
                if (t > 0.95f) crown.t.gameObject.SetActive(false);
            }
            // 밑동 물보라 왕관
            float k = Mathf.Clamp01(t / 0.55f);
            float sw = W * (0.9f + 1.2f * Out(k, 2f));
            splash.t.localScale = new Vector3(sw, W * 1.0f * (1f - 0.65f * k), sw);
            splash.t.gameObject.SetActive(k < 1f);
            Fade(splash, 1f - k * k);
        }

        void Throw()
        {
            for (int i = 0; i < 18; i++)
            {
                float a = (i / 18f) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = Make("fx_water_drop", 0f, 99f);
                var d = new Drop
                {
                    p = p,
                    pos = ground + dir * W * 0.75f + Vector3.up * H * Random.Range(0.85f, 1.0f),
                    vel = (dir * Random.Range(3.5f, 6.5f) + Vector3.up * Random.Range(2.5f, 5f)) * scale,
                    size = Random.Range(0.45f, 0.75f) * scale,
                };
                p.t.position = d.pos;
                drops.Add(d);
            }
        }

        void AddMini(Vector3 at, float size)
        {
            var p = Make("fx_water_splash", 1.2f, 0.3f);
            p.t.position = at;
            minis.Add(new Mini { p = p, born = Time.time, size = size * 1.4f });
        }
    }
}
