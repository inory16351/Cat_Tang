// 종말 처형 · 보스 추가 피해 연출 (2026-11-18 사용자: 다키스트 던전 '상처 찢기'처럼 빨간 X자 상처로 처형 느낌)
//  - 검은 블랙아웃은 뺌 (사용자: 처형은 바로 죽으니 죽을 때 나오는 검은 안개(DeathMist)랑 합쳐지면 충분)
//  1) 히트스톱: 실제 시간 0.1초 (보스 0.06초) 거의 멈춤
//  2) X자 상처 (몸통 안에서 안 벗어나는 크기: 몸 폭 · 높이 중 작은 쪽 기준) = 그림 + 3D (2026-11-18 사용자: 해골처럼 그림이랑 블렌더 3D를 합쳐 자연스럽게):
//     그림 = Codex 시트 cx_rend 8장 (docs/images/fx_src/prompts/execute_rend.txt → fx_sheet_cut.py --strict): 흰 베기 → 찢어진 진홍 상처 → 두 번째 베기 → 붉은 X · 피 튐 → 식어 흘러내림 → 부서짐
//     3D = 블렌더 베인 자국 메시(fx_slash) 두 획이 그림과 같은 박자로 그어지는 흰 심지 빛줄기(그림 아래 깔려 입체감) + 핏방울 가시
//  3) 카메라 흔들림
// 시간은 자체 시계(실제 시간 = deltaTime / timeScale)라 히트스톱 중에도 그어지고, 녹화(FxRecorder 고정 간격) 때도 같은 빠르기
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class ExecutionFx : MonoBehaviour
    {
        static Material slashMat;
        static Material[] rendFrames;
        const float ImageLife = 0.62f;                                      // 그림 8장 재생 시간
        static readonly int HeatId = Shader.PropertyToID("_Heat"), DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly Color Blood = new(0.82f, 0.04f, 0.07f);

        class Slash { public Transform pivot; public MeshRenderer r; public MaterialPropertyBlock b; public float start; public Vector3 from, dir; public float len; public bool sprayed; }

        readonly List<Slash> slashes = new();
        Renderer image;
        float imageSize, clock;
        Vector3 center;

        /// <summary>target = 처형 · 추가 피해 받은 적. boss = 보스 추가 피해 (멈춤 · 흔들림 약하게)</summary>
        public static void Rend(Transform target, bool boss)
        {
            if (!target || !Init()) return;
            var cam = Camera.main;
            if (!cam) return;
            // 몸 크기: 모델 렌더러만 (체력 바 · 표식 빼고). X는 몸통 안에서 안 벗어나게 (2026-11-18 사용자: 너무 큼)
            //   → 몸의 좁은 쪽(폭 · 높이 중 작은 값) 기준, 그림 X 끝이 그 안에 들어오게
            var b = OblivionFx.BodyBounds(target);
            float size = Mathf.Clamp(Mathf.Min(Mathf.Max(b.size.x, b.size.z), b.size.y), 0.8f, 4f);
            var go = new GameObject("ExecutionRend");
            var fx = go.AddComponent<ExecutionFx>();
            var camT = cam.transform;
            fx.center = b.center + (camT.position - b.center).normalized * Mathf.Max(1.2f, b.extents.z + 0.6f);   // 몸 앞(카메라 쪽 시선 위)에 그음 → 화면에선 몸 한가운데
            float L = size * 0.8f;
            var right = camT.right; var up = camT.up;
            fx.AddSlash(camT, (right - up).normalized, L, 0f);                  // 좌상 → 우하 (그림 1장째)
            fx.AddSlash(camT, (-right - up).normalized, L, ImageLife * 2f / 8f); // 우상 → 좌하 (그림 3장째)
            fx.imageSize = size * 1.05f;                                        // 그림 X 내용은 판의 약 85% → 몸 폭의 약 0.9
            fx.MakeImage(camT);
            fx.StartCoroutine(fx.Stop(boss ? 0.06f : 0.1f));
            CameraFollow.Shake(boss ? 0.12f : 0.2f);
            var hp = target.GetComponent<Health>();
            if (hp) hp.TraitTint(new Color(0.15f, 0f, 0.02f), 0.85f, 0.35f);
        }

        static bool Init()
        {
            if (slashMat) return true;
            var mesh = FxMeshes.Lib("fx_slash");
            var fm = Resources.Load<Material>("FX/ToonFire");
            if (!mesh || !fm) return false;
            rendFrames = FxFlipbook.Frames("cx_rend");
            slashMat = new Material(fm) { name = "ToonFireRend" };
            slashMat.SetFloat("_OutlineWidth", 1.6f);
            slashMat.SetFloat("_Intensity", 1.25f);
            slashMat.SetColor("_ColCore", new Color(1f, 0.62f, 0.56f));
            slashMat.SetColor("_ColHot", new Color(0.98f, 0.16f, 0.14f));
            slashMat.SetColor("_ColMid", new Color(0.78f, 0.03f, 0.06f));
            slashMat.SetColor("_ColEdge", new Color(0.38f, 0f, 0.03f));
            slashMat.SetColor("_ColAsh", new Color(0.14f, 0f, 0.02f));
            slashMat.SetColor("_OutlineColor", new Color(0.05f, 0f, 0.01f));
            return true;
        }

        void MakeImage(Transform camT)
        {
            if (rendFrames == null || rendFrames.Length == 0) return;
            var q = new GameObject("RendImage");
            q.transform.SetParent(transform, false);
            q.transform.SetPositionAndRotation(center - camT.forward * 0.05f, camT.rotation);
            q.transform.localScale = Vector3.one * imageSize;
            q.AddComponent<MeshFilter>().sharedMesh = GunkataFx.Quad;
            var r = q.AddComponent<MeshRenderer>();
            r.sharedMaterial = rendFrames[0];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            VividLayer.Set(q);
            image = r;
        }

        void AddSlash(Transform camT, Vector3 dir, float len, float delay)
        {
            var f = -camT.forward;
            var pivot = new GameObject("Slash").transform;
            pivot.SetParent(transform, false);
            var from = center - dir * len * 0.5f;
            pivot.SetPositionAndRotation(from, Quaternion.LookRotation(f, Vector3.Cross(f, dir)));   // 메시 X = 베는 방향, +Z = 카메라 쪽
            pivot.localScale = new Vector3(0.001f, len * 0.35f, len);             // 아주 가는 빛줄기 (그림의 베기 선과 겹침)
            var body = new GameObject("Mesh");
            body.transform.SetParent(pivot, false);
            body.transform.localPosition = new Vector3(0.5f, 0, 0);               // 메시 시작(-0.5)을 회전축에 붙임 → X 배율로 그어짐
            body.AddComponent<MeshFilter>().sharedMesh = FxMeshes.Lib("fx_slash");
            var r = body.AddComponent<MeshRenderer>();
            r.sharedMaterial = slashMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            VividLayer.Set(body);
            var b = new MaterialPropertyBlock();
            b.SetFloat(HeatId, 1f); b.SetFloat(DissolveId, 0f);
            r.SetPropertyBlock(b);
            r.enabled = delay <= 0f;
            slashes.Add(new Slash { pivot = pivot, r = r, b = b, start = delay, from = from, dir = dir, len = len });
        }

        System.Collections.IEnumerator Stop(float real)
        {
            if (Time.timeScale < 0.99f) yield break;                            // 다른 멈춤 · 슬로우 중이면 건드리지 않음
            Time.timeScale = 0.04f;
            yield return new WaitForSecondsRealtime(real);
            if (Time.timeScale > 0f && Time.timeScale < 0.05f) Time.timeScale = 1f;
        }

        void Update()
        {
            clock += Time.timeScale > 1e-4f ? Time.deltaTime / Time.timeScale : Time.unscaledDeltaTime;
            float t = clock;
            // 그림 (자체 시계로 넘김: 히트스톱 중에도 3D 획과 같은 박자)
            if (image)
            {
                int fi = Mathf.FloorToInt(t / ImageLife * rendFrames.Length);
                image.enabled = fi < rendFrames.Length;
                if (image.enabled) image.sharedMaterial = rendFrames[Mathf.Clamp(fi, 0, rendFrames.Length - 1)];
                var cam = Camera.main;
                if (cam) image.transform.rotation = cam.transform.rotation;
            }
            // 3D 획: 그림 밑에 깔린 흰 심지 빛줄기 — 그어지고 곧 식어 사라짐 (그림이 주인공)
            foreach (var s in slashes)
            {
                float k = t - s.start;
                if (k < 0f) continue;
                s.r.enabled = true;
                float draw = Mathf.Clamp01(k / 0.07f);
                float ease = 1f - (1f - draw) * (1f - draw);
                var sc = s.pivot.localScale;
                s.pivot.localScale = new Vector3(Mathf.Max(0.001f, ease) * s.len, sc.y, sc.z);
                s.b.SetFloat(HeatId, Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((k - 0.06f) / 0.25f)));       // 흰 심지 → 진홍으로 식음
                s.b.SetFloat(DissolveId, Mathf.Clamp01((k - 0.22f) / 0.2f));                          // 그림이 X를 이어받으면 녹아 사라짐
                s.r.SetPropertyBlock(s.b);
                if (!s.sprayed && draw >= 1f) { s.sprayed = true; Spray(s); }
            }
            if (t > 1.1f) Destroy(gameObject);
        }

        /// <summary>핏방울: 획을 따라 베는 방향으로 튀는 가는 가시</summary>
        void Spray(Slash s)
        {
            if (!GunkataFx.Init()) return;
            for (int k = 0; k < 7; k++)
            {
                var at = s.from + s.dir * s.len * Random.Range(0.25f, 0.95f);
                var d = (s.dir + Random.insideUnitSphere * 0.45f).normalized;
                float w = Random.Range(0.08f, 0.15f) * s.len * 0.4f;
                FxMesh.Spawn(FxMeshes.Spike(k % 3), slashMat, at, Quaternion.LookRotation(d, Vector3.up), new Vector3(w, w, Random.Range(0.6f, 1.1f) * s.len * 0.35f),
                    Random.Range(0.22f, 0.32f), 0f, d * Random.Range(2.5f, 4.5f) * Mathf.Clamp(s.len, 0.6f, 2f), FxMesh.Kind.Tracer)?.Tint(Blood);
            }
        }
    }
}
