// 피해 숫자: 숫자마다 따로 그린 이미지(Resources/UI/digits/d0~d9)를 이어 붙여 띄움. 튀어 오르며 커졌다 작아지고 사라짐.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class DamageNumber : MonoBehaviour
    {
        static readonly Material[] mats = new Material[10];
        static readonly float[] aspect = new float[10];
        static Mesh quad;
        readonly List<MeshRenderer> parts = new();
        float born, scale;
        Vector3 vel;
        MaterialPropertyBlock mpb;

        static bool Load()
        {
            if (mats[0]) return true;
            var sh = Shader.Find("Sprites/Default");
            for (int i = 0; i < 10; i++)
            {
                var tex = Resources.Load<Texture2D>("UI/digits/d" + i);
                if (!tex) return false;
                mats[i] = new Material(sh) { mainTexture = tex, renderQueue = 3100 };
                aspect[i] = (float)tex.width / tex.height;
            }
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return true;
        }

        public static void Spawn(Vector3 pos, float dmg)
        {
            if (!Load()) return;
            var go = new GameObject("Dmg");
            go.transform.position = pos + new Vector3(Random.Range(-0.35f, 0.35f), 0, 0);
            var d = go.AddComponent<DamageNumber>();
            d.scale = dmg >= 30 ? 0.62f : dmg >= 15 ? 0.5f : 0.4f;
            d.vel = new Vector3(Random.Range(-0.6f, 0.6f), 3.2f, 0);
            d.born = Time.time;
            d.mpb = new MaterialPropertyBlock();
            string s = Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString();
            float total = 0;
            foreach (var c in s) total += aspect[c - '0'] * 0.82f;
            float x = -total / 2;
            foreach (var c in s)
            {
                int n = c - '0';
                var p = new GameObject("d" + n);
                p.transform.SetParent(go.transform, false);
                float w = aspect[n] * 0.82f;
                p.transform.localPosition = new Vector3(x + w / 2, 0, 0);
                p.transform.localScale = new Vector3(aspect[n], 1, 1);
                p.AddComponent<MeshFilter>().sharedMesh = quad;
                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mats[n];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d.parts.Add(mr);
                x += w;
            }
            go.transform.localScale = Vector3.one * d.scale;
        }

        void Update()
        {
            float t = (Time.time - born) / 0.75f;
            if (t >= 1) { Destroy(gameObject); return; }
            vel.y -= 9f * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 0.9f, Mathf.Clamp01((t - 0.15f) / 0.3f));
            transform.localScale = Vector3.one * scale * pop;
            float a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            mpb.SetColor("_Color", new Color(1, 1, 1, a));
            foreach (var r in parts) r.SetPropertyBlock(mpb);
        }
    }
}
