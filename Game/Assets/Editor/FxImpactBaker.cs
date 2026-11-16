// 바닥 탄착(불릿 파티 총알 비) 이펙트를 3D로 조립해 게임 카메라 각도에서 프레임마다 렌더 → 투명 PNG 플립북으로 구움
// - 탄착 섬광 덩어리(바닥에 납작) · 위로 튀며 떨어지는 불똥 가시 4~6개 · 바닥에 눕는 충격 고리 · 먼지 뭉치 2~3개
// - 모양 · 움직임은 런타임 3D 이펙트와 같은 메시(FxMeshes)와 같은 곡선(FxMesh.Evaluate), 같은 ToonFire 셰이더
// - 결과: Assets/Resources/FX/anim/hit{변형}_{프레임}.png → GunkataFx.Impact가 FxFlipbook으로 재생
// 메뉴: Cat_Tang/Bake Impact FX
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class FxImpactBaker
    {
        const string OutDir = "Assets/Resources/FX/anim/";
        public const string SetPrefix = "hit";
        public const int Variants = 3, Frames = 12, Res = 256;
        public const float Duration = 0.4f;             // 구운 길이 (초) = GunkataFx.ImpactLife
        public const float WorldSize = 2.4f;            // 한 장이 덮는 월드 크기 (m) = GunkataFx.ImpactWorldSize
        static readonly Vector3 Origin = new Vector3(0, -500, 0);     // 씬과 겹치지 않는 곳
        const int Layer = 31;
        static readonly Quaternion CamRot = Quaternion.Euler(56.31f, 0, 0);   // 게임 카메라 각도

        // 트리포로 만든 탄착 왕관 모델 (가시 왕관 + 먼지 뭉치, GPT 참고 이미지 docs/images/fx_src/impact_ref.png → Tripo)
        const string CrownDir = "Assets/Art/FX/ImpactCrown/";
        const float CrownWidth = 1.1f, CrownHeight = 0.95f;   // 월드 크기 (m)

        class Part
        {
            public Mesh mesh; public Material mat; public FxMesh.Kind kind;
            public Vector3 pos, size, drift; public Quaternion rot;
            public float life, delay, gravity, seed;
            public bool crown;                          // 트리포 왕관: 바닥에서 솟구쳤다 식으며 재처럼 녹음
            public GameObject go; public MaterialPropertyBlock mpb;
        }

        const string CrownAsset = CrownDir + "ImpactCrown.asset";
        const int CrownGrid = 110;                      // 줄이기 격자 (단위 크기 기준 칸 수)

        /// <summary>왕관 메시 사본. 줄여서 저장해 둔 ImpactCrown.asset이 있으면 그것, 없으면 CrownDir 안의 원본 모델(트리포 FBX)에서
        /// 바닥 가운데 = 원점, 가로 반지름 0.5 · 높이 1로 맞추고 격자로 줄여 ImpactCrown.asset으로 저장 (원본 FBX는 그 뒤 지워도 됨). 둘 다 없으면 null</summary>
        static Mesh LoadCrown()
        {
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(CrownAsset);
            if (saved) return Object.Instantiate(saved);
            var built = BuildCrownFromModel();
            if (!built) return null;
            AssetDatabase.CreateAsset(Object.Instantiate(built), CrownAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FxImpactBaker] 왕관 메시 줄여서 저장: {CrownAsset} (삼각형 {built.triangles.Length / 3})");
            return built;
        }

        /// <summary>격자 칸마다 꼭짓점을 하나로 합쳐 면 수를 줄임 (뭉개진 삼각형 · 겹친 삼각형은 버림)</summary>
        static void Decimate(List<Vector3> verts, List<int> tris, int grid, out List<Vector3> outV, out List<int> outT)
        {
            var map = new Dictionary<Vector3Int, int>();
            var sum = new List<Vector3>(); var cnt = new List<int>();
            var remap = new int[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                var c = new Vector3Int(Mathf.FloorToInt((v.x + 0.5f) * grid), Mathf.FloorToInt(v.y * grid), Mathf.FloorToInt((v.z + 0.5f) * grid));
                if (!map.TryGetValue(c, out int id)) { id = sum.Count; map[c] = id; sum.Add(Vector3.zero); cnt.Add(0); }
                sum[id] += v; cnt[id]++; remap[i] = id;
            }
            outV = new List<Vector3>(sum.Count);
            for (int i = 0; i < sum.Count; i++) outV.Add(sum[i] / cnt[i]);
            outT = new List<int>();
            var seen = new HashSet<(int, int, int)>();
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                int a = remap[tris[i]], b = remap[tris[i + 1]], c = remap[tris[i + 2]];
                if (a == b || b == c || a == c) continue;
                int lo = Mathf.Min(a, Mathf.Min(b, c)), hi = Mathf.Max(a, Mathf.Max(b, c)), mid = a + b + c - lo - hi;
                if (!seen.Add((lo, mid, hi))) continue;
                outT.Add(a); outT.Add(b); outT.Add(c);
            }
        }

        static Mesh BuildCrownFromModel()
        {
            if (!AssetDatabase.IsValidFolder(CrownDir.TrimEnd('/'))) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { CrownDir.TrimEnd('/') }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (!go) continue;
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var m = mf.sharedMesh; if (!m) continue;
                    var tr = mf.transform.localToWorldMatrix;   // 루트 회전 포함 (Blender FBX는 루트에 Z-up → Y-up 회전이 붙음)
                    int b = verts.Count;
                    foreach (var v in m.vertices) verts.Add(tr.MultiplyPoint3x4(v));
                    foreach (var n in m.normals) norms.Add(tr.MultiplyVector(n).normalized);
                    for (int s = 0; s < m.subMeshCount; s++) foreach (var i in m.GetTriangles(s)) tris.Add(b + i);
                }
                if (verts.Count == 0) continue;
                var bounds = new Bounds(verts[0], Vector3.zero);
                foreach (var v in verts) bounds.Encapsulate(v);
                var pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                float rad = Mathf.Max(bounds.extents.x, bounds.extents.z), h = bounds.size.y;
                for (int i = 0; i < verts.Count; i++)
                {
                    var v = verts[i] - pivot;
                    verts[i] = new Vector3(v.x * 0.5f / rad, v.y / h, v.z * 0.5f / rad);
                }
                Decimate(verts, tris, CrownGrid, out var dv, out var dt);
                var mesh = new Mesh { name = "ImpactCrown", indexFormat = dv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                mesh.SetVertices(dv);
                mesh.SetTriangles(dt, 0);
                mesh.RecalculateNormals();          // 가로 · 세로를 따로 늘였으니 법선 다시
                mesh.RecalculateBounds();
                return mesh;
            }
            return null;
        }

        /// <summary>왕관: 0.22 동안 바닥에서 솟구쳐(높이 0.15 → 살짝 넘침 → 1) 퍼지고, 하얗게 탔다가 식어 재 · 먼지색으로 녹음</summary>
        static void CrownPose(Vector3 size, float t, out Vector3 sc, out float heat, out float dis)
        {
            float e = Mathf.Clamp01(t / 0.22f), x = e - 1;
            float back = 1 + 2.70158f * x * x * x + 1.70158f * x * x;
            float hgt = Mathf.LerpUnclamped(0.15f, 1f, back) * (1 - 0.15f * Mathf.Clamp01((t - 0.5f) / 0.5f));
            float wid = Mathf.Lerp(0.55f, 1f, 1 - (1 - e) * (1 - e)) * (1 + 0.12f * t);
            sc = new Vector3(size.x * wid, size.y * hgt, size.z * wid);
            heat = 1 - Mathf.Clamp01((t - 0.28f) / 0.55f);
            dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.45f) / 0.55f));
        }

        [MenuItem("Cat_Tang/Bake Impact FX")]
        public static void Bake()
        {
            var src = Resources.Load<Material>("FX/ToonFire");
            if (!src) { Debug.LogError("[FxImpactBaker] Resources/FX/ToonFire 재질 없음"); return; }
            var fire = new Material(src);
            fire.SetFloat("_OutlineWidth", 2.4f);       // 256px에서 2.4px → 게임 화면(약 절반 크기)에서 1px 남짓
            fire.SetFloat("_Intensity", 2.0f);
            var dust = new Material(src);
            dust.SetFloat("_OutlineWidth", 2.4f);
            dust.SetFloat("_Intensity", 1.0f);
            dust.SetColor("_ColCore", new Color(0.97f, 0.93f, 0.86f));
            dust.SetColor("_ColHot", new Color(0.88f, 0.81f, 0.74f));
            dust.SetColor("_ColMid", new Color(0.74f, 0.68f, 0.68f));
            dust.SetColor("_ColEdge", new Color(0.58f, 0.52f, 0.56f));
            dust.SetColor("_ColAsh", new Color(0.48f, 0.44f, 0.48f));
            dust.SetColor("_OutlineColor", new Color(0.27f, 0.19f, 0.19f));

            var camGo = new GameObject("FxBakeCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = WorldSize * 0.5f;
            cam.transform.SetPositionAndRotation(Origin - CamRot * Vector3.forward * 20f, CamRot);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.allowHDR = false;
            var rt = new RenderTexture(Res, Res, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            cam.targetTexture = rt;
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            var written = new List<string>();
            var crown = LoadCrown();
            try
            {
                Directory.CreateDirectory(OutDir);
                for (int v = 0; v < Variants; v++)
                {
                    var parts = crown ? BuildCrown(new System.Random(2000 + v * 53), v, crown, fire, dust)
                                      : Build(new System.Random(1000 + v * 77), fire, dust);
                    foreach (var p in parts)
                    {
                        p.go = new GameObject("BakePart") { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
                        p.go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                        var mr = p.go.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = p.mat;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mr.receiveShadows = false;
                        p.mpb = new MaterialPropertyBlock();
                    }
                    for (int f = 0; f < Frames; f++)
                    {
                        float time = (f + 0.6f) * Duration / Frames;
                        foreach (var p in parts) Pose(p, time);
                        cam.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, Res, Res), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        string path = OutDir + SetPrefix + v + "_" + f + ".png";
                        File.WriteAllBytes(path, tex.EncodeToPNG());
                        written.Add(path);
                    }
                    foreach (var p in parts) Object.DestroyImmediate(p.go);
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(camGo);
                rt.Release(); Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(fire); Object.DestroyImmediate(dust);
                if (crown) Object.DestroyImmediate(crown);
            }

            AssetDatabase.Refresh();
            foreach (var path in written)
            {
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            Debug.Log($"[FxImpactBaker] 탄착 플립북 {Variants}종 × {Frames}프레임 구움 ({(crown ? "트리포 왕관" : "절차적 형태")}) → {OutDir}{SetPrefix}*");
        }

        static void Pose(Part p, float time)
        {
            float tau = time - p.delay;
            float tl = tau / p.life;
            if (tl < 0 || tl >= 1) { p.go.SetActive(false); return; }
            p.go.SetActive(true);
            // 감속하며 밀려나고(런타임 FxMesh와 같은 e^-6t) 불똥은 중력으로 떨어짐
            var pos = p.pos + p.drift * ((1 - Mathf.Exp(-6f * tau)) / 6f) + Vector3.down * (0.5f * p.gravity * tau * tau);
            Vector3 sc; float heat, dis;
            if (p.crown) CrownPose(p.size, tl, out sc, out heat, out dis);
            else FxMesh.Evaluate(p.kind, p.size, tl, out sc, out heat, out dis);
            p.go.transform.SetPositionAndRotation(pos, p.rot);
            p.go.transform.localScale = sc;
            p.mpb.SetFloat(FxMesh.SeedId, p.seed);
            p.mpb.SetFloat(FxMesh.HeatId, heat);
            p.mpb.SetFloat(FxMesh.DissolveId, dis);
            p.go.GetComponent<MeshRenderer>().SetPropertyBlock(p.mpb);
        }

        /// <summary>트리포 왕관이 주인공: 왕관(변형마다 방향 · 좌우 뒤집기 · 크기 다름) + 작은 섬광 · 불똥 2~4개 · 고리 · 먼지 0~1개</summary>
        static List<Part> BuildCrown(System.Random rng, int v, Mesh crown, Material fire, Material dust)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var parts = new List<Part>();
            float flip = v % 2 == 1 ? -1f : 1f;         // 홀수 변형은 좌우 뒤집어 다른 모양처럼
            parts.Add(new Part
            {
                mesh = crown, mat = fire, crown = true,
                pos = Origin, rot = Quaternion.Euler(0, v * 125f + R(-20, 20), 0),
                size = new Vector3(CrownWidth * flip, CrownHeight, CrownWidth) * R(0.9f, 1.08f),
                life = R(0.32f, 0.38f), delay = 0, seed = R(0, 100)
            });
            parts.Add(new Part
            {
                mesh = FxMeshes.Ball(rng.Next(3)), mat = fire, kind = FxMesh.Kind.Core,
                pos = Origin + Vector3.up * 0.1f, rot = Quaternion.Euler(0, R(0, 360), 0),
                size = new Vector3(R(0.3f, 0.38f), R(0.18f, 0.24f), R(0.3f, 0.38f)),
                life = 0.12f, delay = 0, drift = Vector3.up * 0.5f, seed = R(0, 100)
            });
            int n = 2 + rng.Next(3);
            for (int k = 0; k < n; k++)
            {
                var dir = Quaternion.Euler(-R(30, 75), R(0, 360), 0) * Vector3.forward;
                float w = R(0.05f, 0.09f);
                parts.Add(new Part
                {
                    mesh = FxMeshes.Spike(rng.Next(3)), mat = fire, kind = FxMesh.Kind.Spike,
                    pos = Origin + Vector3.up * 0.1f, rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, R(0, 360)),
                    size = new Vector3(w, w, R(0.3f, 0.6f)), life = R(0.16f, 0.24f), delay = R(0.02f, 0.06f),
                    drift = dir * R(2.5f, 4.5f), gravity = 9f, seed = R(0, 100)
                });
            }
            if (rng.NextDouble() < 0.7)
                parts.Add(new Part
                {
                    mesh = FxMeshes.Torus(), mat = fire, kind = FxMesh.Kind.Ring,
                    pos = Origin + Vector3.up * 0.03f, rot = Quaternion.LookRotation(Vector3.up, Vector3.forward),
                    size = Vector3.one * R(1.1f, 1.4f), life = R(0.2f, 0.25f), delay = 0, seed = R(0, 100)
                });
            if (rng.NextDouble() < 0.6)
            {
                var off = Quaternion.Euler(0, R(0, 360), 0) * Vector3.forward * R(0.15f, 0.35f);
                parts.Add(new Part
                {
                    mesh = FxMeshes.Ball(rng.Next(3)), mat = dust, kind = FxMesh.Kind.Ball,
                    pos = Origin + off + Vector3.up * 0.12f, rot = Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)),
                    size = Vector3.one * R(0.22f, 0.32f), life = R(0.3f, 0.36f), delay = R(0.06f, 0.1f),
                    drift = Vector3.up * R(0.5f, 0.9f) + off.normalized * R(0.3f, 0.6f), seed = R(0, 100)
                });
            }
            return parts;
        }

        static List<Part> Build(System.Random rng, Material fire, Material dust)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var parts = new List<Part>();
            // 1) 탄착 섬광 덩어리: 바닥에 납작하게 펑
            parts.Add(new Part
            {
                mesh = FxMeshes.Ball(rng.Next(3)), mat = fire, kind = FxMesh.Kind.Core,
                pos = Origin + Vector3.up * 0.08f, rot = Quaternion.Euler(0, R(0, 360), 0),
                size = new Vector3(R(0.44f, 0.56f), R(0.24f, 0.32f), R(0.44f, 0.56f)),
                life = 0.16f, delay = 0, drift = Vector3.up * 0.6f, seed = R(0, 100)
            });
            // 2) 튀는 불똥 가시 4~6개: 위쪽으로 고르지 않게 퍼졌다가 떨어짐
            int n = 4 + rng.Next(3);
            float yaw0 = R(0, 360);
            for (int k = 0; k < n; k++)
            {
                float yaw = yaw0 + k * 360f / n + R(-35, 35);
                var dir = Quaternion.Euler(-R(20, 70), yaw, 0) * Vector3.forward;
                float w = R(0.07f, 0.12f);
                parts.Add(new Part
                {
                    mesh = FxMeshes.Spike(rng.Next(3)), mat = fire, kind = FxMesh.Kind.Spike,
                    pos = Origin + Vector3.up * 0.05f, rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, R(0, 360)),
                    size = new Vector3(w, w, R(0.35f, 0.8f)), life = R(0.14f, 0.22f), delay = R(0, 0.03f),
                    drift = dir * R(2f, 4.5f), gravity = 9f, seed = R(0, 100)
                });
            }
            // 3) 바닥 충격 고리 (가끔 빠짐)
            if (rng.NextDouble() < 0.8)
                parts.Add(new Part
                {
                    mesh = FxMeshes.Torus(), mat = fire, kind = FxMesh.Kind.Ring,
                    pos = Origin + Vector3.up * 0.03f, rot = Quaternion.LookRotation(Vector3.up, Vector3.forward),
                    size = Vector3.one * R(0.9f, 1.25f), life = R(0.2f, 0.25f), delay = 0, seed = R(0, 100)
                });
            // 4) 먼지 뭉치 2~3개: 조금 늦게 바닥에서 뭉게뭉게
            int m = 2 + rng.Next(2);
            for (int k = 0; k < m; k++)
            {
                var off = Quaternion.Euler(0, R(0, 360), 0) * Vector3.forward * R(0.05f, 0.3f);
                parts.Add(new Part
                {
                    mesh = FxMeshes.Ball(rng.Next(3)), mat = dust, kind = FxMesh.Kind.Ball,
                    pos = Origin + off + Vector3.up * R(0.1f, 0.25f), rot = Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)),
                    size = Vector3.one * R(0.35f, 0.55f), life = R(0.3f, 0.38f), delay = R(0.02f, 0.06f),
                    drift = Vector3.up * R(0.6f, 1.2f) + off.normalized * R(0.3f, 0.8f), seed = R(0, 100)
                });
            }
            return parts;
        }
    }
}
