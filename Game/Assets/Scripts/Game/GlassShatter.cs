// 거울 고양이 사망 (2026-11-20 사용자: 유리 깨지듯 · 조각상처럼 산산조각)
// 적 모델을 떼어 그 자세 그대로 굳힘(스킨 메시를 정지 메시로 구움) → 0.35초 동안 금이 온몸으로 번지며 떨림 · 하얗게 달아오름 →
// 몸을 덩어리(세포 나누기: 무작위 씨앗점에 가장 가까운 삼각형끼리)로 쪼개 바깥 · 위로 튀김 → 돌며 떨어져 바닥에 튕기고 → 오그라들며 사라짐.
// 터지는 순간 블렌더 유리 파열(FX_Ch3_GlassBurst · MirrorFlash) + 5장 유리 불똥(bk_c5spark)을 겹침.
// 조각 메시는 죽는 순간의 자세로 만들어야 해서 런타임에 굽는 수밖에 없음 (도형 조립 아님)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class GlassShatter : MonoBehaviour
    {
        static readonly int CrackId = Shader.PropertyToID("_Crack");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        class Chunk { public Transform t; public Vector3 vel, spinAxis; public float spin, scale; public bool rested; }

        readonly List<Chunk> chunks = new();
        readonly List<Renderer> statue = new();
        MaterialPropertyBlock mpb;
        Vector3 center, basePos;
        float born, groundY, size;
        bool broken;
        const float CrackTime = 0.35f, Life = 2.2f;

        /// <summary>model = 적의 "Model" 자식. pieces = 조각 수, size = 튀는 세기 배율</summary>
        public static void Play(Transform model, int pieces = 26, float size = 1f)
        {
            if (!model) return;
            float ground = model.parent ? model.parent.position.y : model.position.y;
            model.SetParent(null, true);
            foreach (var a in model.GetComponentsInChildren<Animator>()) a.enabled = false;
            foreach (var b in model.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;   // KataPoser · MirrorCatSkin 멈춤 (자세 · 금을 여기서 맡음)
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            var g = new GameObject("GlassShatter").AddComponent<GlassShatter>();
            g.Build(model, pieces, ground, size);
        }

        void Build(Transform model, int pieces, float ground, float sz)
        {
            mpb = new MaterialPropertyBlock();
            born = Time.time;
            groundY = ground;
            size = sz;
            var b = new Bounds(model.position, Vector3.zero);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy) b.Encapsulate(r.bounds);
            center = b.center;
            basePos = model.position;

            // 씨앗점: 몸 경계 안 무작위 (조각 크기가 고르지 않게 몇 개는 몰아 둠)
            var seeds = new Vector3[pieces];
            for (int i = 0; i < pieces; i++)
                seeds[i] = b.center + Vector3.Scale(b.extents, new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));

            // 렌더러마다 지금 자세의 메시를 월드 좌표로 굳힘
            var tris = new List<(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ua, Vector2 ub, Vector2 uc, Material m)>();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                Mesh mesh; Matrix4x4 mtx;
                if (r is SkinnedMeshRenderer smr)
                {
                    mesh = new Mesh();
                    smr.BakeMesh(mesh, true);
                    mtx = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, smr.transform.lossyScale);   // 구운 메시는 부모 배율이 빠져 나옴 → 다시 곱함 (안 곱하면 고양이 Body 1.8배만큼 작아졌음)
                }
                else if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh && mf.sharedMesh.isReadable)
                {
                    mesh = mf.sharedMesh; mtx = r.transform.localToWorldMatrix;
                }
                else continue;
                var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv;
                bool hasN = n.Length == v.Length, hasUv = uv.Length == v.Length;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var mat = r.sharedMaterials.Length > s ? r.sharedMaterials[s] : r.sharedMaterial;
                    var idx = mesh.GetTriangles(s);
                    for (int k = 0; k < idx.Length; k += 3)
                    {
                        int i0 = idx[k], i1 = idx[k + 1], i2 = idx[k + 2];
                        tris.Add((mtx.MultiplyPoint3x4(v[i0]), mtx.MultiplyPoint3x4(v[i1]), mtx.MultiplyPoint3x4(v[i2]),
                            hasN ? mtx.MultiplyVector(n[i0]).normalized : Vector3.up, hasN ? mtx.MultiplyVector(n[i1]).normalized : Vector3.up, hasN ? mtx.MultiplyVector(n[i2]).normalized : Vector3.up,
                            hasUv ? uv[i0] : Vector2.zero, hasUv ? uv[i1] : Vector2.zero, hasUv ? uv[i2] : Vector2.zero, mat));
                    }
                }
            }
            Destroy(model.gameObject);
            if (tris.Count == 0) { Destroy(gameObject, 0.1f); return; }

            // 삼각형을 가장 가까운 씨앗점의 조각으로 (조각 × 재질별 묶음)
            var groups = new Dictionary<(int, Material), List<int>>();
            for (int i = 0; i < tris.Count; i++)
            {
                var c = (tris[i].a + tris[i].b + tris[i].c) / 3f;
                int best = 0; float bd = float.MaxValue;
                for (int s = 0; s < pieces; s++) { float d = (c - seeds[s]).sqrMagnitude; if (d < bd) { bd = d; best = s; } }
                var key = (best, tris[i].m);
                if (!groups.TryGetValue(key, out var l)) groups[key] = l = new List<int>();
                l.Add(i);
            }
            var pieceRoots = new Dictionary<int, Chunk>();
            foreach (var kv in groups)
            {
                int pid = kv.Key.Item1;
                if (!pieceRoots.TryGetValue(pid, out var ch))
                {
                    var pr = new GameObject("Shard" + pid).transform;
                    pr.SetParent(transform, false);
                    pr.position = seeds[pid];
                    var outDir = (seeds[pid] - center); outDir.y = Mathf.Abs(outDir.y) * 0.5f;
                    outDir = outDir.sqrMagnitude > 1e-4f ? outDir.normalized : Random.onUnitSphere;
                    ch = new Chunk
                    {
                        t = pr,
                        vel = (outDir * Random.Range(3.5f, 7f) + Vector3.up * Random.Range(2.5f, 5.5f)) * size,
                        spinAxis = Random.onUnitSphere, spin = Random.Range(240f, 720f), scale = 1f
                    };
                    pieceRoots[pid] = ch;
                    chunks.Add(ch);
                }
                // 조각 메시 (조각 원점 기준). 안쪽에서도 보이게 뒤집은 면을 함께 넣음 (속이 빈 껍데기라 깨진 단면이 비어 보이지 않게)
                var list = kv.Value;
                var verts = new Vector3[list.Count * 6]; var norms = new Vector3[verts.Length]; var uvs = new Vector2[verts.Length]; var ind = new int[verts.Length];
                var o = ch.t.position;
                for (int j = 0; j < list.Count; j++)
                {
                    var tr = tris[list[j]]; int q = j * 6;
                    verts[q] = tr.a - o; verts[q + 1] = tr.b - o; verts[q + 2] = tr.c - o;
                    norms[q] = tr.na; norms[q + 1] = tr.nb; norms[q + 2] = tr.nc;
                    uvs[q] = tr.ua; uvs[q + 1] = tr.ub; uvs[q + 2] = tr.uc;
                    verts[q + 3] = tr.a - o; verts[q + 4] = tr.c - o; verts[q + 5] = tr.b - o;
                    norms[q + 3] = -tr.na; norms[q + 4] = -tr.nc; norms[q + 5] = -tr.nb;
                    uvs[q + 3] = tr.ua; uvs[q + 4] = tr.uc; uvs[q + 5] = tr.ub;
                    for (int k = 0; k < 6; k++) ind[q + k] = q + k;
                }
                var m = new Mesh { name = "shard" };
                if (verts.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.vertices = verts; m.normals = norms; m.uv = uvs; m.triangles = ind;
                m.RecalculateBounds();
                var go = new GameObject("part");
                go.transform.SetParent(ch.t, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = kv.Key.Item2;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                statue.Add(mr);
            }
            VividLayer.SetAll(gameObject);
            Sfx.Play("e_glass", 0.8f, 0f, 2);
        }

        void Update()
        {
            float t = Time.time - born;
            if (!broken)
            {
                // 굳은 조각상: 금이 온몸으로 번지고 하얗게 달아오르며 바르르 떪 (조각은 아직 제자리)
                float k = Mathf.Clamp01(t / CrackTime);
                float shake = 0.04f * k * size;
                transform.position = new Vector3(Random.Range(-shake, shake), 0, Random.Range(-shake, shake));
                foreach (var r in statue)
                {
                    if (!r) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetFloat(CrackId, Mathf.Lerp(0.6f, 1.6f, k));
                    mpb.SetColor(BaseColorId, Color.Lerp(Color.white, new Color(1.6f, 1.6f, 1.75f), k * k));
                    r.SetPropertyBlock(mpb);
                }
                if (t >= CrackTime) Break();
                return;
            }
            float dt = Time.deltaTime;
            float fade = Mathf.Clamp01((t - (Life - 0.6f)) / 0.6f);
            foreach (var c in chunks)
            {
                if (!c.t) continue;
                if (!c.rested)
                {
                    c.vel += Vector3.down * 22f * dt;
                    c.t.position += c.vel * dt;
                    c.t.rotation = Quaternion.AngleAxis(c.spin * dt, c.spinAxis) * c.t.rotation;
                    if (c.t.position.y < groundY + 0.08f && c.vel.y < 0)
                    {
                        // 바닥에 튕김: 느려지다 멈춰 누움
                        c.t.position = new Vector3(c.t.position.x, groundY + 0.08f, c.t.position.z);
                        c.vel = new Vector3(c.vel.x * 0.45f, -c.vel.y * 0.3f, c.vel.z * 0.45f);
                        c.spin *= 0.4f;
                        if (c.vel.y < 1f) { c.rested = true; }
                    }
                }
                c.t.localScale = Vector3.one * Mathf.Max(0.001f, 1 - fade);
            }
            if (t > Life) Destroy(gameObject);
        }

        void Break()
        {
            broken = true;
            transform.position = Vector3.zero;
            foreach (var r in statue)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(CrackId, 1.2f);
                mpb.SetColor(BaseColorId, Color.white);
                r.SetPropertyBlock(mpb);
            }
            Enemy.Fx3("GlassBurst", center, 2.2f * size);
            Enemy.Fx3("MirrorFlash", center + Vector3.up * 0.2f, 1.8f * size);
            for (int i = 0; i < 4; i++) Enemy.Fx5("c5spark", center + Random.insideUnitSphere * 0.6f, 0.9f * size);
            Sfx.Play("e3b_shards", 1f, 0f, 2);
            CameraFollow.Shake(0.3f);
        }
    }
}
