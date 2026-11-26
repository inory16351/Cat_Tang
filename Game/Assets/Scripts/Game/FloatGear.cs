// 공중에 뜬 톱니 과녁 (2026-11-26 사용자: 맞힐 물체 없이 배지만 떠서 이상함)
// - 블렌더 톱니(Resources/Kit/Ch4/FloatGear, Art/tools/blender_kit_ch4_floatgear.py)를 바닥 위에 띄워 천천히 오르내림 · 앞면이 늘 카메라 쪽
// - 멈춘 톱니라 돌지 않고 이따금 째깍 떨림. 위에 시각 배지 + 아래 화살표(BigLabel 테두리 배지가 자동으로 붙임)
// - 맞는 시각 탄에 맞으면 Solve(): 배지는 바로 사라지고, 톱니가 빠르게 돌며 살짝 떠올라 투명해지며 사라짐(0.8초)
// - 쓰는 곳: 4장 쌍둥이 보스 '멈춘 톱니 돌리기' · '순서대로 연속 명중' (Ch4Twins)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class FloatGear : MonoBehaviour
    {
        const float Hover = 0.9f, Bob = 0.15f, SolveTime = 0.8f;
        static GameObject prefab;
        Transform model;
        BigLabel badge;
        float seed, spin, nextTick;
        bool solving;

        public bool Solved => solving;

        /// <summary>at(바닥)에 뜬 톱니 + 시각 배지. 반환 = 과녁 Health (Ch3Fx.Target)</summary>
        public static FloatGear Make(Vector3 at, int hour, Color numColor, out Health target, Transform owner = null)
        {
            var go = new GameObject("FloatGear_" + hour);
            go.transform.position = at;
            if (owner) go.transform.SetParent(owner, true);
            var g = go.AddComponent<FloatGear>();
            g.seed = Random.value * 10f;
            if (!prefab) prefab = Resources.Load<GameObject>("Kit/Ch4/FloatGear");
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(0, Hover + 0.9f, 0);           // 톱니 가운데 높이
            if (prefab)
            {
                var m = Instantiate(prefab, pivot, false);
                m.transform.localPosition = new Vector3(0, -0.9f, 0);          // 프리팹 원점 = 바닥 가운데 → 가운데로 맞춤
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            else Ch3Fx.Prim(PrimitiveType.Cylinder, "Gear", pivot, Vector3.zero, new Vector3(1.8f, 0.18f, 1.8f), new Color(0.85f, 0.6f, 0.25f)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            g.model = pivot;
            target = Ch3Fx.Target(go, 1.0f, Hover + 1.8f);
            g.badge = BigLabel.Show(go.transform, new Vector3(0, Hover + 2.0f, 0), hour.ToString(), numColor, 1f, BigLabel.Frame.Gear);
            return g;
        }

        void Update()
        {
            if (!model) return;
            var cam = Camera.main;
            float y = Hover + 0.9f + Mathf.Sin((Time.time + seed) * 2.2f) * Bob;
            if (!solving) model.localPosition = new Vector3(0, y, 0);
            // 앞면(+Z 프리팹 앞)이 카메라 쪽 (수평으로만 돌림 — 위에서 내려다봐도 세운 톱니로 보이게)
            Quaternion face = model.rotation;
            if (cam)
            {
                // 탑뷰라 수평으로만 돌리면 납작해 보임 → 카메라 쪽으로 70%만 젖혀 앞면이 보이되 세운 느낌은 남김
                var to = cam.transform.position - model.position;
                var flat = new Vector3(to.x, 0, to.z);
                if (flat.sqrMagnitude < 0.01f) flat = -cam.transform.up;
                var dir = Vector3.Slerp(flat.normalized, to.normalized, 0.7f);
                face = Quaternion.LookRotation(dir, Vector3.up);
            }
            if (solving) spin += Mathf.Lerp(200f, 1200f, Mathf.Clamp01(spin / 900f)) * Time.deltaTime;
            else if (Time.time >= nextTick)
            {
                nextTick = Time.time + Random.Range(0.8f, 1.6f);
                spin += Random.Range(-6f, 6f);                                   // 멈춘 톱니가 째깍 떨림
            }
            model.rotation = face * Quaternion.Euler(0, 0, spin);
        }

        /// <summary>맞는 시각 탄: 돌며 투명해지며 사라짐</summary>
        public void Solve()
        {
            if (solving) return;
            solving = true;
            if (badge) Destroy(badge.gameObject);
            var col = GetComponent<Collider>(); if (col) col.enabled = false;
            StartCoroutine(SolveCo());
        }

        IEnumerator SolveCo()
        {
            Sfx.Play("e4_clank", 0.8f, 0.05f, 2);
            FxBurst.Spawn(model.position, 1.4f, FxBurst.Kind.Spark, new Color(1f, 0.8f, 0.4f));
            // 툰 재질은 투명이 안 돼서 같은 색 반투명 단색 재질로 바꿔 끼움
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");
            var mats = new List<Material>();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    var c = src[i] && src[i].HasProperty("_BaseColor") ? src[i].GetColor("_BaseColor") : Color.white;
                    dst[i] = new Material(sh) { color = c, renderQueue = 3000 };
                    mats.Add(dst[i]);
                }
                r.sharedMaterials = dst;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Vector3 p0 = model.localPosition, s0 = model.localScale;
            for (float t = 0; t < SolveTime; t += Time.deltaTime)
            {
                float k = t / SolveTime;
                model.localPosition = p0 + Vector3.up * (0.6f * k);
                model.localScale = s0 * (1f + 0.2f * k);
                foreach (var m in mats) { var c = m.color; c.a = 1f - k * k; m.color = c; }
                yield return null;
            }
            foreach (var m in mats) Destroy(m);
            Destroy(gameObject);
        }
    }
}
