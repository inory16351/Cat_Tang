// 건카타 총구 폭발 연출 (2D + 3D 섞음)
// - 2D 섬광(flash_0~2): 총구 앞에서 한 순간 번쩍이는 날카로운 가시 별 (잉크 외곽선, docs/tools/fx_flash.py)
// - 건카타 발사 섬광은 전부 3D 메시 (CatTang/ToonFire 셰이더: 툰 색 + 가는 잉크 외곽선, 노이즈 구멍으로 녹아 사라짐)
//   총구 섬광 덩어리(Core) · 총열 방향 긴 가시(Spike) · 양옆 십자 가시 · 날아가는 탄도 가시(Tracer) · 충격 고리(Torus)
// - 회색 연기(2D) · 몸 주위 소용돌이(선)
// - 3D 불꽃 원뿔(Flame) · 불덩이(Ball)는 평소 사격 · 장전 불꽃에서 씀
// 메시는 처음 쓸 때 코드로 한 번 구워서 공유함 (변형 3개씩)
using UnityEngine;

namespace CatTang
{
    public static class GunkataFx
    {
        static Material[] smokeMats, flashMats;
        static Material lineMat, fireMat, spikeMat, muzzleMat, sandMat, sootMat;
        internal static Mesh quad;
        static readonly Color SwirlCol = new Color(0.22f, 0.13f, 0.11f, 0.85f);

        static bool inited;
        internal static bool Init()
        {
            // 플레이 진입 시 도메인 리로드를 끈 프로젝트라(DisableDomainReload) 정적 필드가 판을 넘어 남음.
            // 런타임에 만든 재질 · 메시는 플레이 종료 때 파괴되므로, 하나라도 죽었으면 다시 만듦
            if (inited && fireMat && spikeMat && muzzleMat && sandMat && sootMat && lineMat && quad) return true;
            inited = true;
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");   // URP 태그가 있어야 Vivid(후처리 뒤)에서도 그려짐
            smokeMats = Load("FX/smoke_", 2, sh, 3000);
            flashMats = Load("FX/flash_", 3, sh, 3003);
            lineMat = new Material(sh) { renderQueue = 3001 };
            var fm = Resources.Load<Material>("FX/ToonFire");
            if (fm) fireMat = fm;
            else { var fire = Shader.Find("CatTang/ToonFire"); if (fire) fireMat = new Material(fire); }
            // 건카타 발사 섬광용: 외곽선을 가늘게 (가는 가시가 검은 막대처럼 보이지 않게) · 더 밝게
            // 색은 ToonFire 기본 (흰 심지 → 노랑 → 주황 → 빨강 테). 궁 분신 실루엣이 이 색에 맞춤 (PlayerController.UltGhost)
            if (fireMat)
            {
                spikeMat = new Material(fireMat) { name = "ToonFireSpike" };
                spikeMat.SetFloat("_OutlineWidth", 1.2f);
                spikeMat.SetFloat("_Intensity", 2.2f);
                // 일반 사격 총구 섬광용: 빨강 (2026-11-16 사용자). 작은 따뜻한 흰 심지 → 다홍 → 빨강 → 짙은 빨강 테, 분홍으로 날아가지 않게 밝기 낮게
                muzzleMat = new Material(fireMat) { name = "ToonFireMuzzleRed" };
                muzzleMat.SetFloat("_OutlineWidth", 1.0f);
                muzzleMat.SetFloat("_Intensity", 1.2f);
                muzzleMat.SetColor("_ColCore", new Color(1f, 0.86f, 0.72f));
                muzzleMat.SetColor("_ColHot", new Color(1f, 0.38f, 0.18f));
                muzzleMat.SetColor("_ColMid", new Color(0.95f, 0.14f, 0.1f));
                muzzleMat.SetColor("_ColEdge", new Color(0.6f, 0.04f, 0.05f));
                muzzleMat.SetColor("_ColAsh", new Color(0.26f, 0.05f, 0.05f));
                muzzleMat.SetColor("_OutlineColor", new Color(0.18f, 0.02f, 0.03f));
                // 모래시계 폭탄병 폭발: 모래빛 먼지 덩어리 · 파편 (뜨거울 땐 밝은 모래 → 식으면 흙갈색)
                sandMat = new Material(fireMat) { name = "ToonFireSand" };
                sandMat.SetFloat("_OutlineWidth", 2f);
                sandMat.SetFloat("_Intensity", 1.05f);
                sandMat.SetColor("_ColCore", new Color(1f, 0.93f, 0.74f));
                sandMat.SetColor("_ColHot", new Color(0.96f, 0.8f, 0.52f));
                sandMat.SetColor("_ColMid", new Color(0.82f, 0.62f, 0.38f));
                sandMat.SetColor("_ColEdge", new Color(0.6f, 0.42f, 0.27f));
                sandMat.SetColor("_ColAsh", new Color(0.42f, 0.31f, 0.24f));
                sandMat.SetColor("_OutlineColor", new Color(0.2f, 0.12f, 0.08f));
                // 그을음 연기: 짙은 회갈색 덩어리
                sootMat = new Material(fireMat) { name = "ToonFireSoot" };
                sootMat.SetFloat("_OutlineWidth", 2f);
                sootMat.SetFloat("_Intensity", 1f);
                sootMat.SetColor("_ColCore", new Color(0.62f, 0.55f, 0.52f));
                sootMat.SetColor("_ColHot", new Color(0.5f, 0.44f, 0.42f));
                sootMat.SetColor("_ColMid", new Color(0.38f, 0.33f, 0.32f));
                sootMat.SetColor("_ColEdge", new Color(0.27f, 0.23f, 0.23f));
                sootMat.SetColor("_ColAsh", new Color(0.2f, 0.17f, 0.17f));
                sootMat.SetColor("_OutlineColor", new Color(0.08f, 0.06f, 0.06f));
            }
            quad = new Mesh { name = "FxQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return fireMat != null;
        }

        static Material[] Load(string prefix, int n, Shader sh, int queue)
        {
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < n; i++)
            {
                var t = Resources.Load<Texture2D>(prefix + i);
                if (t) list.Add(new Material(sh) { mainTexture = t, renderQueue = queue });
            }
            return list.ToArray();
        }

        /// <summary>총구 한 발. p = 총구, d = 바깥 방향(수평), full = 본체(폭발 구름 + 붉은 흩뿌림) / 분신(폭발만)
        /// 주인공 = 손으로 그린 2D 프레임(Codex 시트 FX/anim/blast) · 3D 불꽃 원뿔은 아래에 깔아 입체감만 더함</summary>
        /// <summary>true = 건카타 폭발을 Codex 손그림 프레임으로, false = 3D 불꽃 (2026-11-16 사용자: 3D가 더 나음)</summary>
        public static bool UseDrawnBlast = false;

        public static void Burst(Vector3 p, Vector3 d, float scale, bool full)
        {
            if (!Init()) return;
            d = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
            var rot = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            bool drawn = UseDrawnBlast && FxFlipbook.Spawn("blast", p, d, 2.7f * scale * Random.Range(0.92f, 1.08f), 0.4f, true, d * 0.6f * scale);
            if (drawn)
            {
                // 3D 원뿔: 짧고 작게 (그림 밑에서 부피감)
                FxMesh.Spawn(FxMeshes.Flame(Random.Range(0, 3)), fireMat, p, rot, new Vector3(0.38f, 0.38f, 1.1f) * scale, 0.12f, 0, d * 0.8f * scale, FxMesh.Kind.Flame);
                if (full)
                {
                    var side = Vector3.Cross(Vector3.up, d).normalized;
                    FxFlipbook.Spawn("cloud", p + d * 1.5f * scale + side * Random.Range(-0.25f, 0.25f) * scale + Vector3.up * 0.15f, Vector3.zero, 1.7f * scale, 0.55f, false,
                        d * 1.2f * scale + Vector3.up * 0.4f, Random.Range(0f, 360f));
                }
            }
            else
            {
                // 3D 방식 (2026-11-16 개편: 촛불 같은 꽃잎 원뿔 → 날카로운 발사 섬광, 전부 3D 메시)
                // 대칭 없이 변칙적으로: 매 발 섬광 덩어리 찌그러짐 · 주 가시 길이와 빗나감 · 곁가시 수와 각도 · 길이 · 지연이 제각각
                var squash = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, p + d * Random.Range(0.08f, 0.22f) * scale, squash,
                    new Vector3(Random.Range(0.5f, 0.75f), Random.Range(0.42f, 0.68f), Random.Range(0.32f, 0.5f)) * scale, Random.Range(0.15f, 0.21f), 0, d * 1.2f * scale, FxMesh.Kind.Core);
                // 주 가시: 총열에서 살짝 빗나가고 길이 · 굵기 제각각
                var md = Jitter(d, 7f, -0.05f, 0.08f);
                float mw = Random.Range(0.34f, 0.5f);
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p, Quaternion.LookRotation(md, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                    new Vector3(mw, mw, Random.Range(1.7f, 2.9f)) * scale, Random.Range(0.2f, 0.28f), 0, md * 2.5f * scale, FxMesh.Kind.Spike);
                // 곁가시: 1~4개, 아무 쪽으로나 (앞쪽일수록 길게) · 조금씩 늦게
                int nSide = full ? Random.Range(2, 5) : Random.Range(1, 3);
                for (int k = 0; k < nSide; k++)
                {
                    float a = Random.Range(18f, 115f) * (Random.value < 0.5f ? -1f : 1f);
                    var sd = Jitter(Quaternion.AngleAxis(a, Vector3.up) * d, 0f, -0.15f, 0.25f);
                    float sw = Random.Range(0.13f, 0.27f);
                    float sl = Random.Range(0.45f, 1.5f) * (1.2f - Mathf.Abs(a) / 150f);
                    FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p + d * Random.Range(0.05f, 0.25f) * scale,
                        Quaternion.LookRotation(sd, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                        new Vector3(sw, sw, sl) * scale, Random.Range(0.13f, 0.22f), Random.Range(0f, 0.035f), sd * Random.Range(0.4f, 1.6f) * scale, FxMesh.Kind.Spike);
                }
            }
            // 탄도: 가는 3D 가시가 살짝 빗나가며 쏜살같이 날아감
            var td = Jitter(d, 4f, -0.03f, 0.03f);
            FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p + d * 0.3f * scale, Quaternion.LookRotation(td, Vector3.up),
                new Vector3(0.11f, 0.11f, Random.Range(1.4f, 2.1f)) * scale, Random.Range(0.17f, 0.23f), 0, td * Random.Range(17f, 24f) * scale, FxMesh.Kind.Tracer);
            if (!full) return;
            // 불똥 탄도: 2~5개, 한쪽으로 쏠리기도 하는 작은 3D 가시
            int nSpark = Random.Range(2, 6);
            float bias = Random.Range(-15f, 15f);
            for (int k = 0; k < nSpark; k++)
            {
                var dd = Jitter(Quaternion.AngleAxis(bias + Random.Range(-28f, 28f), Vector3.up) * d, 0f, -0.06f, 0.12f);
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p + dd * 0.3f * scale, Quaternion.LookRotation(dd, Vector3.up),
                    new Vector3(0.06f, 0.06f, Random.Range(0.5f, 1.1f)) * scale, Random.Range(0.18f, 0.3f), Random.Range(0f, 0.05f), dd * Random.Range(11f, 20f) * scale, FxMesh.Kind.Tracer);
            }
        }

        /// <summary>3D 폭발 (모래시계 폭탄병 자폭 등). p = 바닥 중심, radius = 피해 반경(m), sand = 모래 먼지 · 파편 섞음.
        /// 흰 섬광 덩어리 → 불덩이 무리 → 그을음 · 모래 덩어리가 솟으며 식고 → 바닥 충격 고리 · 튀는 파편. 전부 툰 불꽃 메시</summary>
        public static void Explosion(Vector3 p, float radius, bool sand = true) => Explosion(p, radius, sand, null);

        /// <summary>tint가 있으면 불덩이 · 섬광을 그 색으로 (탄 폭발 효과: 탄 색 폭발)</summary>
        public static void Explosion(Vector3 p, float radius, bool sand, Color? tint)
        {
            Sfx.Play("explosion", Mathf.Clamp(0.55f + radius * 0.1f, 0.6f, 1f), 0.08f, 2);
            if (!Init()) return;
            System.Func<FxMesh, FxMesh> T = f => f && tint.HasValue ? f.Tint(tint.Value) : f;
            // 블렌더로 구운 불꽃 · 연기 · 불똥 플립북 프리팹 (반경 2m 기준으로 맞춤)
            FxEffect.Spawn("FX_Explosion", p, Quaternion.identity, radius / 2f, tint);
            float r = Mathf.Max(0.5f, radius);
            var up = Vector3.up;
            EffectShadowLight.Flash(p + up * Mathf.Clamp(r * 0.48f, 0.7f, 1.7f), tint ?? new Color(1f, 0.64f, 0.29f),
                Mathf.Clamp(r * 2.75f, 3.4f, 8.2f), Mathf.Clamp(r * 4.1f, 5.0f, 13.5f), 0.29f);
            // 1) 섬광 덩어리: 한가운데에서 하얗게 펑
            T(FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, p + up * r * 0.35f, Random.rotation,
                Vector3.one * r * 1.25f, 0.32f, 0, up * r * 0.6f, FxMesh.Kind.Core));
            // 2) 불덩이 무리: 반구 위에 흩어져 바깥 · 위로 부풀어 나감 (크기 · 시차 제각각)
            int nFire = 7 + Random.Range(0, 3);
            for (int k = 0; k < nFire; k++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.8f + 0.15f; d.Normalize();
                float s = r * Random.Range(0.45f, 0.75f);
                T(FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), fireMat, p + d * r * Random.Range(0.15f, 0.45f) + up * 0.2f, Random.rotation,
                    new Vector3(s, s * Random.Range(0.8f, 1.1f), s), Random.Range(0.45f, 0.7f), Random.Range(0f, 0.08f), d * r * Random.Range(2.2f, 3.6f), FxMesh.Kind.Ball));
            }
            // 3) 그을음 · 모래 덩어리: 조금 늦게 피어올라 천천히 솟으며 식음
            int nSmoke = 6 + Random.Range(0, 3);
            for (int k = 0; k < nSmoke; k++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.5f + 0.35f; d.Normalize();
                float s = r * Random.Range(0.5f, 0.85f);
                var m = sand && k % 2 == 0 ? sandMat : sootMat;
                FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), m, p + d * r * Random.Range(0.3f, 0.7f) + up * 0.3f, Random.rotation,
                    Vector3.one * s, Random.Range(0.8f, 1.15f), Random.Range(0.06f, 0.18f), (d + up * 0.6f) * r * Random.Range(1.0f, 1.8f), FxMesh.Kind.Ball);
            }
            // 4) 바닥 충격 고리 (고리 메시 지름 1 → 피해 반경까지 퍼짐) + 한 박자 늦은 모래 고리
            var flat = Quaternion.LookRotation(up, Vector3.forward);
            T(FxMesh.Spawn(FxMeshes.Torus(), spikeMat, p + up * 0.08f, flat, new Vector3(r * 2f, r * 2f, 2.5f), 0.3f, 0, Vector3.zero, FxMesh.Kind.Ring));
            if (sand) FxMesh.Spawn(FxMeshes.Torus(), sandMat, p + up * 0.05f, flat * Quaternion.Euler(0, 0, 40f), new Vector3(r * 2.4f, r * 2.4f, 4f), 0.5f, 0.06f, Vector3.zero, FxMesh.Kind.Ring);
            // 5) 파편: 바깥 · 위로 튀는 짧은 가시 (모래 · 불똥)
            int nDebris = 10 + Random.Range(0, 5);
            for (int k = 0; k < nDebris; k++)
            {
                var d = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
                d.y = Random.Range(0.15f, 0.9f); d.Normalize();
                var m = sand && k % 3 != 0 ? sandMat : spikeMat;
                float w = Random.Range(0.08f, 0.16f) * r;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), m, p + up * 0.4f + d * r * 0.2f, Quaternion.LookRotation(d, up),
                    new Vector3(w, w, Random.Range(0.35f, 0.7f) * r), Random.Range(0.3f, 0.45f), Random.Range(0f, 0.05f), d * r * Random.Range(5f, 9f), FxMesh.Kind.Tracer);
            }
            CameraFollow.Shake(Mathf.Clamp(0.12f * r, 0.15f, 0.5f));
        }

        // ---------- 타격 · 닷지 · 공명 (2026-11-16 사용자: 훨씬 화려하고 건카타 · 폭발과 어울리게 → 전부 3D 툰 불꽃 메시 + 탄 색) ----------

        static Vector3 Perp(Vector3 d) { var p = Vector3.Cross(d, Vector3.up); return p.sqrMagnitude > 1e-4f ? p.normalized : Vector3.right; }

        /// <summary>메시 · 재질 준비됨 (밖에서 FxMesh를 쓸 때)</summary>
        public static bool Ready => Init();

        /// <summary>한 방향으로 쏜살같이 날아가는 가는 빛 가시 하나 (바람 · 빛살)</summary>
        public static void Streak(Vector3 at, Vector3 d, float len, float w, Color col, float speed)
        {
            if (!Init()) return;
            FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, at, Quaternion.LookRotation(d, Vector3.up),
                new Vector3(w, w, len), Random.Range(0.16f, 0.24f), 0, d * speed, FxMesh.Kind.Tracer)?.Tint(col);
        }

        public const float HitScale = 1.3f;                                  // 고양이 탄 일반 타격 크기 (2026-11-17 사용자: 조금 크게)
        static readonly Color CritRed = new(1f, 0.16f, 0.14f), CritHot = new(1f, 0.82f, 0.62f);

        /// <summary>치명타 (2026-11-17 사용자: 일반 타격 모양을 빨간 계열로, 더 크고 화려하게):
        /// 진홍 일반 타격 1.8배 + 45° 비튼 뜨거운 흰빛 일반 타격 1.1배를 겹치고, 탄도에 수직인 면으로 진홍 빛살 8개가 사방으로 날아감</summary>
        public static void CritImpact(Vector3 p, Vector3 dir, float scale = 1f)
        {
            if (!Init()) return;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            var look = Quaternion.LookRotation(dir, Vector3.up);
            bool prefab = FxEffect.Spawn("FX_Hit", p, look, 1.8f * scale, CritRed);
            if (prefab) FxEffect.Spawn("FX_Hit", p, look * Quaternion.Euler(0, 0, 45f), 1.1f * scale, CritHot);
            else HitImpact(p, dir, CritRed, false, 1.8f * scale);
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, p, Random.rotation, Vector3.one * 0.9f * scale, 0.12f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(CritHot);
            var up = Perp(dir);
            float roll = Random.Range(0f, 45f);
            for (int k = 0; k < 8; k++)
            {
                var d = (Quaternion.AngleAxis(roll + k * 45f + Random.Range(-8f, 8f), dir) * up - dir * 0.15f).normalized;
                float w = (k % 2 == 0 ? 0.2f : 0.13f) * scale;
                FxMesh.Spawn(FxMeshes.Spike(k % 3), spikeMat, p + d * 0.15f, Quaternion.LookRotation(d, dir),
                    new Vector3(w, w, (k % 2 == 0 ? 1.5f : 0.9f) * scale), Random.Range(0.2f, 0.28f), Random.Range(0f, 0.03f), d * Random.Range(5f, 8f) * scale, FxMesh.Kind.Tracer)?.Tint(k % 2 == 0 ? CritRed : CritHot);
            }
        }

        /// <summary>총알 타격: 탄 색 섬광 덩어리 + 맞은 면에서 튀어나오는 가시(주로 쏜 쪽 · 옆, 몇 개는 관통 방향) + 탄도에 수직인 충격 고리.
        /// crit = 금빛 네 갈래 별 가시 + 큰 고리 겹침. small = 관통 · 튕김용 작은 불꽃(고리 없음)</summary>
        public static void HitImpact(Vector3 p, Vector3 dir, Color col, bool crit, float scale = 1f, bool small = false)
        {
            if (!Init()) return;
            // 에디터에서 만든 프리팹(블렌더 메시: 만화 충격 별 · 튀는 왕관 · 파편 · 관통 가시 · 반짝임)이 있으면 그걸 꺼냄
            var look = Quaternion.LookRotation(dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward, Vector3.up);
            if (FxEffect.Spawn(crit ? "FX_HitCrit" : "FX_Hit", p, look, scale * (small ? 0.55f : 1f), col)) return;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            float s = scale * (small ? 0.6f : 1f) * (crit ? 1.35f : 1f);
            var back = -dir;
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, p, Random.rotation, Vector3.one * 0.5f * s, 0.13f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(col);
            int n = small ? Random.Range(2, 4) : crit ? Random.Range(7, 10) : Random.Range(4, 7);
            for (int k = 0; k < n; k++)
            {
                // 70%는 쏜 쪽 반구(맞은 면에서 튀는 파편), 30%는 관통 방향
                var basis = Random.value < 0.7f ? back : dir;
                var d = (basis + Random.insideUnitSphere * 1.1f); d.y = Mathf.Abs(d.y) * 0.6f + 0.05f;
                d = d.sqrMagnitude > 1e-4f ? d.normalized : basis;
                float w = Random.Range(0.12f, 0.24f) * s;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p + d * 0.08f, Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                    new Vector3(w, w, Random.Range(0.45f, 1.05f) * s), Random.Range(0.14f, 0.22f), Random.Range(0f, 0.03f), d * Random.Range(1f, 3f) * s, FxMesh.Kind.Spike)?.Tint(col);
            }
            if (!crit) return;
            // 치명타: 금빛 네 갈래 별 (쏜 방향을 축으로 십자, 살짝 비틀어) + 큰 흰 고리
            var side = Perp(dir);
            float roll = Random.Range(-25f, 25f);
            for (int k = 0; k < 4; k++)
            {
                var d = (Quaternion.AngleAxis(roll + k * 90f, dir) * Vector3.up + back * 0.25f).normalized;
                float len = (k % 2 == 0 ? 1.9f : 1.3f) * s;
                FxMesh.Spawn(FxMeshes.Spike(k % 3), spikeMat, p, Quaternion.LookRotation(d, side), new Vector3(0.22f * s, 0.22f * s, len), 0.26f, 0.02f, Vector3.zero, FxMesh.Kind.Spike);
            }
        }

        /// <summary>닷지 원: 바닥에 탄 색 충격 고리 두 겹이 퍼지고, 몸을 감싸는 자이로 고리 두 개가 엇갈려 돌며,
        /// 몸에서 바깥으로 짧은 빛살 가시가 사방으로 튐. big = 닷지 액션(저스트), 아니면 확률 회피(작게)</summary>
        public static void DodgeBurst(Vector3 feet, Color col, bool big)
        {
            if (!Init()) return;
            if (FxEffect.Spawn("FX_DodgePerfect", feet, Quaternion.identity, big ? 1f : 0.65f, col)) return;   // 에디터 프리팹 (블렌더 메시)
            float s = big ? 1f : 0.65f;
            var up = Vector3.up;
            var body = feet + up * 0.9f;
            var flat = Quaternion.LookRotation(up, Vector3.forward);
            // 몸을 감싸는 고리 두 개: 서로 다른 기울기로
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, body, Random.rotation, Vector3.one * 0.9f * s, 0.16f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(col);
            // 바깥으로 튀는 빛살: 고르지 않은 간격 · 길이
            int n = big ? 12 : 7;
            float a0 = Random.Range(0f, 360f);
            for (int k = 0; k < n; k++)
            {
                var d = Quaternion.Euler(0, a0 + k * 360f / n + Random.Range(-12f, 12f), 0) * Vector3.forward;
                d.y = Random.Range(-0.05f, 0.35f); d.Normalize();
                float w = Random.Range(0.1f, 0.18f) * s;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, body + d * 0.4f * s, Quaternion.LookRotation(d, up),
                    new Vector3(w, w, Random.Range(0.8f, 1.6f) * s), Random.Range(0.22f, 0.32f), Random.Range(0f, 0.05f), d * Random.Range(7f, 11f) * s, FxMesh.Kind.Tracer)?.Tint(col);
            }
        }

        /// <summary>둥근 빛 폭발 (공명 사격 섬광 · 범위 기절 · 보상 등 FxBurst.Gold 자리): 탄 색 섬광 덩어리 + 방사 가시 (ring = 범위 효과만 고리, 2026-11-17 사용자: 다 고리 같다)</summary>
        public static void Flare(Vector3 at, float size, Color col, bool ring = false)
        {
            if (!Init()) return;
            float s = Mathf.Max(0.4f, size * 0.5f);
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, at, Random.rotation, Vector3.one * 0.9f * s, 0.2f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(col);
            if (ring) FxMesh.Spawn(FxMeshes.Torus(), spikeMat, at, Random.rotation, Vector3.one * 2.2f * s, 0.28f, 0.02f, Vector3.zero, FxMesh.Kind.Ring)?.Tint(col);
            int n = 6 + Random.Range(0, 4);
            for (int k = 0; k < n; k++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.7f; d.Normalize();
                float w = Random.Range(0.12f, 0.22f) * s;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, at + d * 0.2f * s, Quaternion.LookRotation(d, Vector3.up),
                    new Vector3(w, w, Random.Range(0.7f, 1.4f) * s), Random.Range(0.18f, 0.28f), Random.Range(0f, 0.04f), d * Random.Range(2f, 4f) * s, FxMesh.Kind.Spike)?.Tint(col);
            }
        }

        /// <summary>작고 짧은 섬광 (공명 범위 피해, 2026-11-18 사용자 확정): 다른 특수 이펙트를 안 가리게 작은 탄 색 덩어리 + 짧은 가시 몇 개, 연기 · 고리 없음</summary>
        public static void SmallFlash(Vector3 at, float size, Color col)
        {
            if (!Init()) return;
            float s = size * 0.5f;
            var c = Color.Lerp(col, Color.white, 0.35f);
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, at, Random.rotation, Vector3.one * 0.7f * s, 0.12f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(c);
            int n = 4 + Random.Range(0, 2);
            for (int k = 0; k < n; k++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.6f; d.Normalize();
                float w = Random.Range(0.08f, 0.14f) * s;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, at + d * 0.15f * s, Quaternion.LookRotation(d, Vector3.up),
                    new Vector3(w, w, Random.Range(0.5f, 0.9f) * s), Random.Range(0.1f, 0.16f), 0f, d * Random.Range(2f, 3.5f) * s, FxMesh.Kind.Spike)?.Tint(c);
            }
        }

        /// <summary>탄 특성별 타격 (FX_Trait_*, 2026-11-17 사용자 확정 — 충격 고리 없이 모양 · 움직임으로 개성):
        /// 회상 = 첫 타 + 메아리 두 번 · 추억 = 꽃잎 8장 · 망각 = 얼음 가시 박혔다 깨짐 ·
        /// 되감기 = 맞은 적의 보라 잔상이 뒤에서 미끄러져 되돌아오고 시곗바늘이 거꾸로 휙 (타임 락 느낌) + 박혔다 깨지는 보라 결정 ·
        /// 종말 = 진홍 불꽃에서 해골이 솟아 타 사라짐 · 지연 = 충전 폭탄이 적에 붙어 깜빡이다 0.45초 뒤 날카로운 호박색 폭발 ·
        /// 예지 = 금빛 눈 문양이 번쩍 뜨고 탄도 쪽으로 금빛 선 · 영원 = 별도 이펙트 없음(청록 일반 타격, 정지 추가 피해는 초록 숫자) ·
        /// 미련 = 잿빛 주황 불씨가 적에 붙어 0.8초 타다 재로 흩날림. 맞은 적 몸도 특성 색으로 물들고(Health.TraitTint). 종말은 맞을 땐 진홍 일반 타격만 (해골 · 불길은 치명타 CritSkull, 처형은 ExecutionFx.Rend). 활성화된 탄은 크게. target = 맞은 적(따라붙는 조각 · 잔상). 그렸으면 true</summary>
        /// <summary>여러 특성 동시 (2026-11-17 사용자: 맞은 자리엔 켜진 특성 전부): traits[0] = 대표(가장 많이 모은 특성) 타격 그대로 + 몸 반응,
        /// 나머지는 0.75배로 조금씩 비껴 같이 터짐 (몸 반응 · 따라붙기는 대표만). 하나라도 그렸으면 true</summary>
        public static bool TraitHitAll(System.Collections.Generic.IList<int> traits, Vector3 p, Vector3 dir, Color col, bool awake, Transform target)
        {
            if (traits == null || traits.Count == 0) return false;
            bool any = TraitHit(traits[0], p, dir, col, awake, target);
            var fwd = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            var cam = Camera.main;
            var q = cam ? p + (cam.transform.position - p).normalized * 0.75f : p;
            var side = cam ? cam.transform.right : Vector3.right;
            for (int i = 1; i < traits.Count; i++)
            {
                if (traits[i] == 3002) continue;   // 추억: 표식이 다 찼을 때만 시계 (PlayerController)
                string n = TraitPrefab(traits[i]);
                if (n == null) continue;                                       // 영원은 따로 그림 없음 (정지 추가 피해 초록 숫자)
                float a = (i - 0.5f) * 2.4f;                                    // 대표 둘레로 고르게 비껴 놓음
                var off = (side * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * 0.35f;
                if (traits[i] == 3005)                                          // 종말: 맞을 땐 불길 없이 진홍 일반 타격만 (불 · 해골은 처형 때만, 2026-11-18 사용자)
                {
                    FxEffect.Spawn("FX_Hit", q + off, Quaternion.LookRotation(fwd, Vector3.up), HitScale * 0.75f, TraitColor(3005, col));
                    any = true;
                    continue;
                }
                var sub = FxEffect.Spawn("FX_Trait_" + n, q + off, Quaternion.LookRotation(fwd, Vector3.up), (awake ? 1.1f : 0.8f) * 0.75f, TraitColor(traits[i], col));
                if (sub && traits[i] == 3003) NoSpark(sub);
                any = true;
            }
            return any;
        }

        /// <summary>치명타 (2026-11-18 사용자: 종말 처형 때 나오던 해골 · 불꽃을 이제 치명타 때): 진홍 불길에서 해골이 크게 솟아 타 사라짐 + 몸에 불길.
        /// 종말 처형 · 보스 추가 피해는 ExecutionFx.Rend (X자 상처 + 블랙아웃)</summary>
        public static void CritSkull(Transform target, Vector3 hit, float scale = 1f)
        {
            var cam = Camera.main;
            var p = target ? target.position + Vector3.up * 1.1f : hit;
            if (cam) p += (cam.transform.position - p).normalized * 0.8f;
            var tc = TraitColor(3005, Color.white);
            if (!FxEffect.Spawn("FX_Trait_Doom", p, Quaternion.identity, 1.6f * scale, tc)) { CritImpact(hit, Vector3.forward, scale); return; }
            if (!target) return;
            var burn = FxEffect.Spawn("FX_Trait_DoomBurn", target.position + (cam ? (cam.transform.position - target.position).normalized * 0.5f : Vector3.zero), Quaternion.identity, 1.2f * scale, tc);
            if (burn) burn.Follow(target);
        }

        /// <summary>망각 타격: 파란 불똥(구운 불똥 플립북) 끔 — 얼음 가시만 (2026-11-18 사용자: 불똥 빼고 얼음 느낌만)</summary>
        static void NoSpark(FxEffect fx)
        {
            var sp = fx.transform.Find("BakedSpark");
            if (sp) sp.gameObject.SetActive(false);
        }

        static string TraitPrefab(int trait) => trait switch
        {
            3001 => "Recall", 3003 => "Oblivion", 3004 => "Rewind", 3005 => "Doom",
            3006 => "Delay", 3007 => "Foresight", _ => null   // 미련(3009)은 타격 이펙트 없음, 추억(3002)은 MemoryClock (2026-11-18 사용자)
        };

        public static bool TraitHit(int trait, Vector3 p, Vector3 dir, Color col, bool awake, Transform target = null)
        {
            string n = trait switch
            {
                3001 => "Recall", 3002 => "Memory", 3003 => "Oblivion", 3004 => "Rewind", 3005 => "Doom",
                3006 => "Delay", 3007 => "Foresight", 3008 => "Hit", _ => null   // 미련(3009)은 따로 그림 없음 → 일반 타격 (2026-11-18 사용자)
            };
            if (n == null) return false;
            var fwd = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            var look = Quaternion.LookRotation(fwd, Vector3.up);
            var tc = TraitColor(trait, col);
            float sc = awake ? 1.1f : 0.8f;
            // 2026-11-17 사용자: 특성마다 고유 색 (예전엔 탄 색이라 9종이 다 주황으로 비슷해 보임). 탄 색은 궤적 선에만 남음
            // 맞은 자리에서 카메라 쪽으로 당겨 그림: 큰 몬스터 몸 안에 묻혀 가려지지 않게 (붙는 폭탄 · 불씨 · 눈 문양이 몸 앞에 보이게)
            if (trait == 3002) return false;   // 추억: 맞을 때마다는 그림 없음 → 표식이 다 찼을 때 시계 (PlayerController MemoryMark, 2026-11-18 사용자)
            var cam = Camera.main;
            if (cam) p += (cam.transform.position - p).normalized * 0.7f;
            if (trait == 3008) return FxEffect.Spawn("FX_Hit", p, look, HitScale, tc);
            // 종말 (2026-11-18 사용자: 맞을 때 불타는 효과 빼고 처형 때만): 맞으면 진홍 일반 타격 + 몸이 잠깐 진홍빛 (지직거림 · 불길 없음).
            // 진홍 불길 · 해골 · 몸 불길(FX_Trait_Doom · FX_Trait_DoomBurn)은 치명타(CritSkull), 처형은 X자 상처(ExecutionFx.Rend)
            if (trait == 3005)
            {
                var dhp = target ? target.GetComponent<Health>() : null;
                if (dhp) dhp.TraitTint(tc, 0.45f, 0.5f);
                return FxEffect.Spawn("FX_Hit", p, look, HitScale, tc);
            }
            var fx = FxEffect.Spawn("FX_Trait_" + n, p, look, sc, tc);
            if (!fx) return false;
            if (trait == 3003) NoSpark(fx);
            // 맞은 적 몸 자체의 반응 (2026-11-17 사용자): 특성 색으로 물들고, 종말은 몸에 진홍 불길이 붙어 타오름
            var hp = target ? target.GetComponent<Health>() : null;
            if (hp)
            {
                switch (trait)
                {
                    case 3001: hp.TraitTint(tc, 0.3f, 0.55f); break;                              // 회상: 하늘빛으로 잠깐
                    case 3002: hp.TraitTint(tc, 0.5f, 0.5f); break;                               // 추억: 분홍빛
                    case 3003: hp.TraitTint(tc, 0.6f, 0.7f); break;                               // 망각: 얼어붙은 청백
                    case 3004: hp.TraitTint(tc, 0.4f, 0.75f, 9f); break;                          // 되감기: 보라로 지직거림
                    case 3006: hp.TraitTint(tc, 0.45f, 0.6f, 5f); hp.FlashLater(0.45f); break;   // 지연: 호박빛 깜빡이다 폭발 순간 하얗게
                    case 3007: hp.TraitTint(tc, 0.45f, 0.65f); break;                             // 예지: 금빛
                }
            }
            // 지연: 적에 붙어 따라다님
            if (target && trait == 3006) fx.Follow(target);
            // 되감기: 맞은 적의 보라 잔상이 탄도 뒤쪽(0.2초 전 자리)에서 미끄러져 제자리로 되돌아옴
            if (trait == 3004 && target)
            {
                var body = target.Find("Model");
                if (body)
                {
                    var flat = new Vector3(fwd.x, 0, fwd.z).normalized;
                    var c = new Color(tc.r, tc.g, tc.b, 0.75f);
                    Afterimage.DashIn(body, body.position - flat * 1.1f * sc, body.position, body.rotation, c, 0.16f, 0.22f);
                }
            }
            return true;
        }

        /// <summary>특성 탄 탄도 (2026-11-17 사용자: 얼음이면 궤적도 파랗고 얼음 덩어리가 날아가듯): 특성 모양 덩어리가 a → b로 날아가며 부스러기를 흘림.
        /// 회상 별 · 추억 꽃 · 망각 얼음 덩어리(+작은 조각 둘, 서리 부스러기가 떨어짐) · 되감기 시곗바늘(돌며) · 종말 진홍 불덩이(불꽃 흘림) ·
        /// 지연 충전 폭탄(돌며 → 맞은 자리에 붙음) · 예지 금빛 바늘 · 영원 없음(궤적 색만) · 미련 불씨(연기 흘림)</summary>
        /// trait2 = 보조 특성: 덩어리 모양은 주 특성, 흘리는 부스러기는 보조 특성 (예: 망각 + 종말 = 진홍 불꽃을 흘리는 얼음 덩어리)
        public static void TraitBolt(int trait, Vector3 a, Vector3 b, Color tc, float scale = 1f, int trait2 = 0)
        {
            if (!Init()) return;
            if (trait == 3008) { if (trait2 == 0 || trait2 == 3008) return; trait = trait2; tc = TraitColor(trait2, tc); trait2 = 0; }   // 영원은 덩어리 없음 → 보조 특성 모양으로
            var d = b - a; float len = d.magnitude;
            if (len < 0.3f) return;
            var dir = d / len;
            float T = Mathf.Clamp(len / 90f, 0.04f, 0.1f);
            var look = Quaternion.LookRotation(dir, Vector3.up);
            string mesh = trait switch
            {
                3001 => "fx_star", 3002 => "fx_flower", 3003 => "fx_shard_0", 3004 => "fx_hand", 3005 => "fx_ball_0",
                3006 => "fx_charge", 3007 => "fx_spike_1", _ => "fx_ball_1"
            };
            Vector3 size = trait switch
            {
                3003 => new Vector3(0.38f, 0.38f, 0.75f), 3007 => new Vector3(0.1f, 0.1f, 1.4f), 3005 => Vector3.one * 0.5f,
                3004 => Vector3.one * 0.9f, 3006 => Vector3.one * 0.6f, _ => Vector3.one * 0.55f
            } * scale;
            var rot = trait == 3003 || trait == 3007 ? look : trait == 3004 || trait == 3006 || trait == 3001 || trait == 3002 ? look * Quaternion.Euler(90, 0, 0) : Random.rotation;
            int crumb = trait2 != 0 && trait2 != 3008 ? trait2 : trait;
            Bolt(FxMeshes.Lib(mesh), a, b, T, rot, size, tc, crumb, true, crumb == trait ? tc : TraitColor(crumb, tc));
            if (trait == 3003)
            {
                // 얼음: 작은 조각 둘이 살짝 비껴 같이 날아감
                for (int k = 0; k < 2; k++)
                {
                    var off = Quaternion.AngleAxis(Random.Range(0f, 360f), dir) * Perp(dir) * 0.22f * scale;
                    Bolt(FxMeshes.Lib(k == 0 ? "fx_shard_1" : "fx_shard_2"), a + off - dir * 0.3f, b + off * 0.4f, T * 1.08f, look, new Vector3(0.2f, 0.2f, 0.4f) * scale, tc, trait, false, tc);
                }
            }
        }

        static void Bolt(Mesh m, Vector3 a, Vector3 b, float T, Quaternion rot, Vector3 size, Color tc, int crumbTrait, bool crumbs, Color crumbCol)
        {
            if (!m) return;
            var kind = rot == Quaternion.LookRotation((b - a).normalized, Vector3.up) ? FxMesh.Kind.Spike : FxMesh.Kind.Ball;
            var f = FxMesh.Spawn(m, spikeMat, a, rot, size, T * 2.6f, 0, Vector3.zero, kind)?.Tint(tc);
            if (!f) return;
            var bolt = f.gameObject.AddComponent<TraitBoltMove>();
            bolt.a = a; bolt.b = b; bolt.T = T; bolt.trait = crumbTrait; bolt.col = crumbCol; bolt.crumbs = crumbs; bolt.size = size.x;
            string mn = m.name;
            bolt.spin = mn == "fx_hand" ? -1500f : mn == "fx_charge" ? 900f : mn == "fx_star" || mn == "fx_flower" ? 700f : 0f;
        }

        /// <summary>탄도 덩어리가 지나가며 흘리는 부스러기: 얼음은 서리 조각이 떨어지고, 종말은 불꽃이 솟고, 미련은 연기, 나머지는 작은 불똥</summary>
        public static void BoltCrumb(int trait, Vector3 p, Vector3 dir, Color tc, float s)
        {
            if (!Init()) return;
            var up = Vector3.up;
            switch (trait)
            {
                case 3003:
                    FxMesh.Spawn(FxMeshes.Lib("fx_shard_" + Random.Range(0, 3)), spikeMat, p + Random.insideUnitSphere * 0.12f, Random.rotation, Vector3.one * Random.Range(0.08f, 0.15f) * s,
                        Random.Range(0.25f, 0.4f), 0, (-up * Random.Range(1.5f, 3f) + Random.insideUnitSphere) , FxMesh.Kind.Ball)?.Tint(Color.Lerp(tc, Color.white, 0.3f));
                    break;
                case 3005:
                    FxMesh.Spawn(FxMeshes.Flame(Random.Range(0, 3)), fireMat, p, Quaternion.LookRotation(up + Random.insideUnitSphere * 0.4f, dir), new Vector3(0.18f, 0.18f, 0.45f) * s,
                        Random.Range(0.2f, 0.3f), 0, up * 1.5f, FxMesh.Kind.Flame)?.Tint(tc);
                    break;
                case 3009:
                    FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), sootMat, p, Random.rotation, Vector3.one * Random.Range(0.15f, 0.25f) * s,
                        Random.Range(0.35f, 0.5f), 0, up * 1.2f, FxMesh.Kind.Ball);
                    break;
                default:
                    FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, p, Quaternion.LookRotation(-dir + Random.insideUnitSphere * 0.5f, up), new Vector3(0.05f, 0.05f, 0.3f) * s,
                        Random.Range(0.12f, 0.2f), 0, -dir * 1.5f, FxMesh.Kind.Spike)?.Tint(tc);
                    break;
            }
        }

        /// <summary>특성 고유 색: 회상 하늘 · 추억 분홍 · 망각 청백 · 되감기 보라 · 종말 진홍 · 지연 호박 · 예지 금 · 영원 청록 · 미련 잿빛 주황</summary>
        public static Color TraitColor(int trait, Color fallback) => trait switch
        {
            3001 => new Color(0.5f, 0.85f, 1f),
            3002 => new Color(1f, 0.55f, 0.75f),
            3003 => new Color(0.42f, 0.75f, 1f),   // 망각: 얼음 파랑 (2026-11-17 사용자: 궤적도 파랗게 — 예전 청백은 거의 흰색)
            3004 => new Color(0.72f, 0.48f, 1f),
            3005 => new Color(0.92f, 0.12f, 0.16f),
            3006 => new Color(1f, 0.68f, 0.22f),
            3007 => new Color(1f, 0.88f, 0.38f),
            3008 => new Color(0.3f, 0.95f, 0.85f),
            3009 => new Color(0.95f, 0.5f, 0.32f),
            _ => fallback,
        };

        /// <summary>적 공격 예고 섬광 (젠레스 존 제로식 주황 십자 반짝임): 맞기 직전 이 순간에 회피하라는 신호</summary>
        public static void WarnGlint(Vector3 p, float scale = 1f) => FxEffect.Spawn("FX_WarnGlint", p, Quaternion.identity, scale);

        /// <summary>수평 방향 d를 좌우 ±yaw° · 위아래 y 성분(lo~hi)만큼 무작위로 흔든 단위 벡터</summary>
        static Vector3 Jitter(Vector3 d, float yaw, float lo, float hi)
        {
            var v = Quaternion.AngleAxis(Random.Range(-yaw, yaw), Vector3.up) * d;
            v.y += Random.Range(lo, hi);
            return v.normalized;
        }

        /// <summary>일반 사격 총구 섬광 (리볼버, 2026-11-16 개편): 전부 가는 빨간 3D (muzzleMat).
        /// - 총구: 작은 섬광 덩어리 + 총열 방향 길고 가는 바늘 + 앞쪽으로 비스듬한 가는 곁가시 2개(좌우 각도 다름) + 얇은 고리 + 아주 가는 탄도
        /// - 리볼버 강조 (gun을 주면): 실린더와 총열 틈에서 양옆으로 칙 새는 가스 가시(리볼버만의 특징) · 공이 자리 불티 (고리는 2026-11-17 뺌)
        /// - 총구에 남는 가는 연기 한 줄기</summary>
        public static void MuzzleFlash(Vector3 p, Vector3 d, float scale, Transform gun = null)
        {
            if (!Init()) return;
            var mat = muzzleMat ? muzzleMat : fireMat;
            d = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
            // 총구 섬광 덩어리 (작게)
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), mat, p + d * 0.06f * scale, Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                new Vector3(0.2f, 0.2f, 0.16f) * scale * Random.Range(0.9f, 1.1f), 0.09f, 0, d * 0.3f * scale, FxMesh.Kind.Core);
            // 주 바늘: 가늘고 길게
            var md = Jitter(d, 3f, -0.02f, 0.03f);
            FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), mat, p, Quaternion.LookRotation(md, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                new Vector3(0.085f, 0.085f, Random.Range(1.0f, 1.35f)) * scale, Random.Range(0.1f, 0.13f), 0, md * 1.6f * scale, FxMesh.Kind.Spike);
            // 곁가시 2개: 앞쪽으로 비스듬히, 좌우 각도 · 길이 다르게
            for (int s = -1; s <= 1; s += 2)
            {
                var sd = Jitter(Quaternion.AngleAxis(s * Random.Range(22f, 42f), Vector3.up) * d, 0f, -0.05f, 0.12f);
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), mat, p + d * 0.05f * scale, Quaternion.LookRotation(sd, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                    new Vector3(0.04f, 0.04f, Random.Range(0.3f, 0.5f)) * scale, Random.Range(0.08f, 0.11f), Random.Range(0f, 0.015f), sd * 0.6f * scale, FxMesh.Kind.Spike);
            }
            // 탄도: 아주 가는 바늘이 쏜살같이
            FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), mat, p + d * 0.2f * scale, Quaternion.LookRotation(d, Vector3.up),
                new Vector3(0.035f, 0.035f, Random.Range(0.9f, 1.3f)) * scale, 0.12f, 0.01f, d * 26f * scale, FxMesh.Kind.Tracer);

            if (gun)
            {
                // 총 기준 좌표 (원점 = 손잡이, +z = 총구 쪽, +y = 윗면): 총구까지 거리로 실린더 · 공이 자리를 짐작
                var mz = gun.InverseTransformPoint(p);
                var up = gun.up; var side = gun.right; var fwd = gun.forward;
                // 실린더-총열 틈 가스: 틈에서 작게 번쩍 + 양옆으로 칙 새는 가시 2~3개씩 (리볼버만의 모습이라 잘 보이게)
                var gap = gun.TransformPoint(new Vector3(0, mz.y * 0.85f, mz.z * 0.42f));
                FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), mat, gap, Random.rotation, new Vector3(0.12f, 0.09f, 0.12f) * scale, 0.07f, 0, up * 0.2f * scale, FxMesh.Kind.Core);
                for (int s = -1; s <= 1; s += 2)
                    for (int j = 0, nj = Random.Range(2, 4); j < nj; j++)
                    {
                        var gd = (side * s + up * Random.Range(0.05f, 0.55f) + fwd * Random.Range(-0.2f, 0.3f)).normalized;
                        FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), mat, gap, Quaternion.LookRotation(gd, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                            new Vector3(0.065f, 0.065f, Random.Range(0.3f, 0.5f)) * scale, Random.Range(0.09f, 0.13f), Random.Range(0f, 0.015f), gd * 0.7f * scale, FxMesh.Kind.Spike);
                    }
                // 공이 자리 불티 (실린더 뒤 위)
                var hammer = gun.TransformPoint(new Vector3(0, mz.y * 0.95f, mz.z * 0.08f));
                FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), mat, hammer, Random.rotation, Vector3.one * 0.07f * scale, 0.07f, 0, up * 0.4f * scale, FxMesh.Kind.Core);
            }
            // 총구에 남는 가는 연기 한 줄기
            Smoke(p + d * 0.25f * scale + Vector3.up * 0.05f, 0.45f * scale, 0.55f);
        }

        public const float ImpactLife = 0.4f;           // = Editor/FxImpactBaker.Duration
        public const float ImpactWorldSize = 2.4f;      // = Editor/FxImpactBaker.WorldSize

        /// <summary>바닥 탄착 한 번: 3D로 조립해 구운 플립북(FX/anim/hit0~2, Cat_Tang/Bake Impact FX)을 재생. p = 바닥 점.
        /// 구운 그림은 게임 카메라 각도에서 본 모습이라 카메라 정면 판에 그대로 붙임. 그림이 없으면 false</summary>
        public static bool Impact(Vector3 p, float scale)
        {
            string set = "hit" + Random.Range(0, 3);
            if (FxFlipbook.Count(set) == 0) return false;
            float size = ImpactWorldSize * scale;
            var pos = p;
            var cam = Camera.main;
            if (cam)
            {
                // 판 아래쪽이 바닥에 묻혀 잘리지 않게 카메라 쪽으로 당기고, 가까워진 만큼 작게 해 화면 크기는 그대로
                const float pull = 1.2f;
                float dist = Vector3.Distance(cam.transform.position, p);
                pos = p - cam.transform.forward * pull;
                if (dist > pull * 2) size *= (dist - pull) / dist;
            }
            return FxFlipbook.Spawn(set, pos, Vector3.zero, size, ImpactLife, false);
        }

        /// <summary>회색 연기 뭉치</summary>
        public static void Smoke(Vector3 at, float size, float life = 0.9f)
        {
            if (!Init()) return;
            // 블렌더로 구운 툰 연기 플립북(bk_smoke)이 있으면 그걸로 (뭉게구름이 피어올라 녹아 흩어짐)
            if (FxFlipbook.Spawn("bk_smoke", at, Vector3.zero, size * 1.1f, life * 1.1f, false, Vector3.up * 0.6f + Random.insideUnitSphere * 0.3f, Random.Range(0f, 360f))) return;
            if (smokeMats.Length == 0) return;
            FxSprite.Spawn(smokeMats[Random.Range(0, smokeMats.Length)], at, size, life, 0, Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f, FxSprite.Style.Smoke);
        }

        /// <summary>작은 불꽃 한 점 (장전 철컥 등): 섬광 없이 작은 불덩이만</summary>
        public static void Spark(Vector3 at, float size)
        {
            if (!Init()) return;
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), fireMat, at, Random.rotation, Vector3.one * size, 0.22f, 0, Vector3.up * 0.3f, FxMesh.Kind.Ball);
        }

        /// <summary>섬광 한 장만 (장전 철컥 반짝 등)</summary>
        public static void Flash(Vector3 at, Vector3 dir, float size, float life = 0.08f)
        {
            if (!Init() || flashMats.Length == 0) return;
            FxSprite.Spawn(flashMats[Random.Range(0, flashMats.Length)], at, size, life, 0, Vector3.zero, FxSprite.Style.Flash, dir);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 어두운 곡선 (spin = 도/초)</summary>
        public static void Swirl(Vector3 center, float radius, float spin, float life = 0.35f)
        {
            if (!Init()) return;
            FxLine.Arc(center, radius, spin, SwirlCol, life);
        }

        internal static Material LineMat { get { Init(); return lineMat; } }
        internal static Mesh Quad { get { Init(); return quad; } }
    }

    /// <summary>2D 프레임 애니메이션 (Codex 시트를 잘라 만든 FX/anim/이름_0~N). 카메라를 보고, 그림의 +x(오른쪽)를 화면에 비친 방향 d에 맞춤.
    /// pivotLeft = 그림 왼쪽 가운데가 원점(총구)</summary>
    public class FxFlipbook : MonoBehaviour
    {
        static readonly System.Collections.Generic.Dictionary<string, Material[]> sets = new();
        static readonly int ColorId = Shader.PropertyToID("_Color");
        Material[] frames;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, size;
        Vector3 origin, dir, drift;
        bool pivotLeft;
        float roll;

        public static int Count(string set) => Load(set).Length;
        /// <summary>프레임 재질들 (FX/anim/set_0~N, 블렌더로 구운 bk_* 포함)</summary>
        public static Material[] Frames(string set) => Load(set);

        static Material[] Load(string set)
        {
            if (sets.TryGetValue(set, out var m) && (m.Length == 0 || m[0])) return m;   // 지난 판에 파괴된 재질이면 다시 만듦
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");   // URP 태그가 있어야 Vivid(후처리 뒤)에서도 그려짐
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < 64; i++)   // 코스튬 모래바람 sand_full = 40장
            {
                var t = Resources.Load<Texture2D>("FX/anim/" + set + "_" + i);
                if (!t) break;
                list.Add(new Material(sh) { mainTexture = t, renderQueue = 3004 });
            }
            return sets[set] = list.ToArray();
        }

        // 풀링 (2026-11-20): 프레임과 자세만 새로 지정하고 쿼드와 재질 블록은 재사용한다.
        static readonly System.Func<GameObject> factory = Create;
        static GameObject Create()
        {
            var go = new GameObject("Flipbook"); VividLayer.Set(go);
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.Quad;
            var f = go.AddComponent<FxFlipbook>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            return go;
        }

        public static bool Spawn(string set, Vector3 pos, Vector3 dir, float size, float life, bool pivotLeft, Vector3 drift = default, float roll = 0)
        {
            var fr = Load(set);
            if (fr.Length == 0) return false;
            var go = GameObjectPool.Get("FxFlipbook", factory);
            var f = go.GetComponent<FxFlipbook>();
            f.mpb.Clear();
            f.frames = fr; f.origin = pos; f.dir = dir; f.size = size; f.life = life; f.pivotLeft = pivotLeft; f.drift = drift; f.roll = roll;
            f.born = Time.time;
            f.Pose(0);
            return true;
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { GameObjectPool.Release(gameObject); return; }
            origin += drift * Time.deltaTime;
            Pose(t);
        }

        void Pose(float t)
        {
            int i = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
            mr.sharedMaterial = frames[i];
            var cam = Camera.main;
            float ang = roll;
            Vector3 right = Vector3.right;
            if (cam)
            {
                if (dir.sqrMagnitude > 1e-4f)
                {
                    var v = cam.transform.InverseTransformDirection(dir);
                    ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                }
                var rot = cam.transform.rotation * Quaternion.Euler(0, 0, ang);
                transform.rotation = rot;
                right = rot * Vector3.right;
            }
            transform.localScale = Vector3.one * size;
            transform.position = pivotLeft ? origin + right * size * 0.5f : origin;
            // 마지막 20%는 살짝 옅어짐
            var c = Color.white; c.a = t < 0.8f ? 1 : 1 - (t - 0.8f) / 0.2f;
            mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>3D 이펙트 메시. 블렌더로 만든 메시(Resources/FX/Meshes/fx_*, Art/tools/blender_fx.py)를 먼저 쓰고,
    /// 없으면 코드로 구운 메시 (처음 쓸 때 한 번 만들어 공유)</summary>
    public static class FxMeshes
    {
        static Mesh[] flames, balls, spikes;
        static Mesh torus;
        static readonly System.Collections.Generic.Dictionary<string, Mesh> lib = new();

        /// <summary>블렌더 메시 (Resources/FX/Meshes/name). 없으면 null</summary>
        public static Mesh Lib(string name)
        {
            if (lib.TryGetValue(name, out var m) && m) return m;
            m = Resources.Load<Mesh>("FX/Meshes/" + name);
            lib[name] = m;
            return m;
        }

        /// <summary>충격 고리: XY 평면의 도넛 (축 = +z), 반지름 0.5 · 굵기 0.06. 둘레가 조금씩 울퉁불퉁</summary>
        public static Mesh Torus()
        {
            var bl = Lib("fx_ring"); if (bl) return bl;
            if (torus) return torus;
            const int A = 40, B = 10;
            const float R0 = 0.5f, r0 = 0.06f;
            var verts = new Vector3[A * B];
            for (int a = 0; a < A; a++)
            {
                float u = a * Mathf.PI * 2 / A;
                float rr = r0 * (1 + 0.35f * Mathf.Sin(u * 5) * Mathf.Sin(u * 3 + 1.3f));
                var c = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0);
                for (int b = 0; b < B; b++)
                {
                    float v = b * Mathf.PI * 2 / B;
                    verts[a * B + b] = c * (R0 + Mathf.Cos(v) * rr) + Vector3.forward * Mathf.Sin(v) * rr;
                }
            }
            var tris = new System.Collections.Generic.List<int>();
            for (int a = 0; a < A; a++)
                for (int b = 0; b < B; b++)
                {
                    int i0 = a * B + b, i1 = ((a + 1) % A) * B + b, i2 = a * B + (b + 1) % B, i3 = ((a + 1) % A) * B + (b + 1) % B;
                    // 바깥을 보게 감음 (거꾸로면 면이 안쪽을 봐서 툰 불꽃이 검은 테로만 보임)
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            torus = new Mesh { name = "FxTorus", vertices = verts, triangles = tris.ToArray() };
            torus.RecalculateNormals();
            torus.RecalculateBounds();
            return torus;
        }

        /// <summary>바늘 가시: 원점(총구)에서 +z로 뻗음. 뿌리 바로 앞이 가장 굵고(반지름 약 0.5) 끝(z=1)은 바늘처럼 뾰족.
        /// 단면은 4~5갈래 별 모양 → 어느 방향에서 봐도 날이 선 실루엣</summary>
        public static Mesh Spike(int v)
        {
            var bl = Lib("fx_spike_" + Mathf.Abs(v) % 3); if (bl) return bl;
            if (spikes == null || !spikes[0]) { spikes = new Mesh[3]; for (int i = 0; i < 3; i++) spikes[i] = BuildSpike(53 + i * 11); }
            return spikes[Mathf.Abs(v) % 3];
        }

        public static Mesh BuildSpike(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            const int U = 16, K = 40;
            int ridges = 4 + rng.Next(2);
            float phase = R(0, Mathf.PI * 2);
            var verts = new Vector3[(U + 1) * K + 1];
            for (int k = 0; k < K; k++)
            {
                float th = k * Mathf.PI * 2 / K;
                float pa = (th + phase) / (Mathf.PI * 2) * ridges;
                float peak = 1 - Mathf.Abs(pa - Mathf.Floor(pa) - 0.5f) * 2;   // 0(골) ~ 1(날)
                float star = Mathf.Lerp(0.72f, 1f, Mathf.Pow(peak, 1.6f));   // 둥근 몸통에 날이 살짝 선 단면
                for (int u = 0; u <= U; u++)
                {
                    float t = u / (float)U;
                    // 뿌리에서 0.18까지 부드럽게 굵어졌다가 끝으로 곧게 가늘어짐 (부피 있는 창끝)
                    float prof = t < 0.18f ? Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0, 1, t / 0.18f)) : Mathf.Pow(1 - (t - 0.18f) / 0.82f, 1.15f);
                    float r = 0.5f * prof * star * (1 + 0.06f * Mathf.Sin(th * 3 + t * 7 + phase));   // 살짝 일렁이는 표면
                    verts[u * K + k] = new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, t);
                }
            }
            int baseIdx = (U + 1) * K;
            verts[baseIdx] = new Vector3(0, 0, -0.02f);
            var tris = new System.Collections.Generic.List<int>();
            for (int u = 0; u < U; u++)
                for (int k = 0; k < K; k++)
                {
                    int a = u * K + k, b = u * K + (k + 1) % K, c = (u + 1) * K + k, d = (u + 1) * K + (k + 1) % K;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            for (int k = 0; k < K; k++) { tris.Add(baseIdx); tris.Add((k + 1) % K); tris.Add(k); }
            var m = new Mesh { name = "FxSpike" + seed, vertices = verts, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>불꽃 원뿔: 원점(총구)에서 +z로 뻗음. 좁은 뿌리 → 넓게 퍼지다 끝이 가시 꽃잎으로 갈라짐. 길이 1, 반지름 약 0.5</summary>
        public static Mesh Flame(int v)
        {
            var bl = Lib("fx_flame_" + Mathf.Abs(v) % 3); if (bl) return bl;
            if (flames == null || !flames[0]) { flames = new Mesh[3]; for (int i = 0; i < 3; i++) flames[i] = BuildFlame(101 + i * 17); }
            return flames[Mathf.Abs(v) % 3];
        }

        /// <summary>울퉁불퉁한 불덩이: 지름 약 1</summary>
        public static Mesh Ball(int v)
        {
            var bl = Lib("fx_ball_" + Mathf.Abs(v) % 3); if (bl) return bl;
            if (balls == null || !balls[0]) { balls = new Mesh[3]; for (int i = 0; i < 3; i++) balls[i] = BuildBall(7 + i * 13); }
            return balls[Mathf.Abs(v) % 3];
        }

        public static Mesh BuildFlame(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            const int U = 18, K = 64;
            int petals = 6 + rng.Next(3);
            float phase = R(0, Mathf.PI * 2);
            var petalLen = new float[petals];
            for (int i = 0; i < petals; i++) petalLen[i] = R(0.55f, 1f);
            var verts = new Vector3[(U + 1) * K + 1];
            for (int k = 0; k < K; k++)
            {
                float th = k * Mathf.PI * 2 / K;
                // 꽃잎: 각도마다 가장 가까운 꽃잎 중심까지의 거리 → 뾰족한 봉우리
                float pa = (th + phase) / (Mathf.PI * 2) * petals;
                int pi = Mathf.FloorToInt(pa) % petals;
                float fr = pa - Mathf.Floor(pa);
                float peak = 1 - Mathf.Abs(fr - 0.5f) * 2;                 // 0(골) ~ 1(봉우리)
                float sp = Mathf.Pow(peak, 2.2f);
                float zEnd = 0.62f + 0.38f * sp * petalLen[pi];             // 봉우리는 길게 뻗음
                float wid = 0.7f + 0.45f * sp;
                for (int u = 0; u <= U; u++)
                {
                    float t = u / (float)U;
                    float prof = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.62f));  // 0 → 1 → 0
                    float r = 0.5f * prof * wid * (1 + 0.08f * Mathf.Sin(th * 3 + t * 9 + phase));
                    r += 0.12f * sp * t * t * prof;                          // 꽃잎 끝이 바깥으로 살짝 벌어짐
                    verts[u * K + k] = new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, t * zEnd);
                }
            }
            int baseIdx = (U + 1) * K;
            verts[baseIdx] = new Vector3(0, 0, -0.02f);
            var tris = new System.Collections.Generic.List<int>();
            for (int u = 0; u < U; u++)
                for (int k = 0; k < K; k++)
                {
                    int a = u * K + k, b = u * K + (k + 1) % K, c = (u + 1) * K + k, d = (u + 1) * K + (k + 1) % K;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            for (int k = 0; k < K; k++) { tris.Add(baseIdx); tris.Add((k + 1) % K); tris.Add(k); }
            var m = new Mesh { name = "FxFlame" + seed, vertices = verts, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Mesh BuildBall(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            // 혹 8~10개: 방향 · 크기
            int n = 8 + rng.Next(3);
            var dirs = new Vector3[n]; var amp = new float[n];
            for (int i = 0; i < n; i++) { dirs[i] = new Vector3(R(-1, 1), R(-1, 1), R(-1, 1)).normalized; amp[i] = R(0.18f, 0.38f); }
            const int LAT = 20, LON = 32;
            var verts = new Vector3[(LAT + 1) * LON];
            for (int a = 0; a <= LAT; a++)
                for (int b = 0; b < LON; b++)
                {
                    float th = Mathf.PI * a / LAT, ph = Mathf.PI * 2 * b / LON;
                    var nrm = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    float r = 0.36f;
                    for (int i = 0; i < n; i++) r += amp[i] * Mathf.Pow(Mathf.Max(0, Vector3.Dot(nrm, dirs[i])), 5f);
                    verts[a * LON + b] = nrm * r;
                }
            var tris = new System.Collections.Generic.List<int>();
            for (int a = 0; a < LAT; a++)
                for (int b = 0; b < LON; b++)
                {
                    int i0 = a * LON + b, i1 = a * LON + (b + 1) % LON, i2 = (a + 1) * LON + b, i3 = (a + 1) * LON + (b + 1) % LON;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            var m = new Mesh { name = "FxBall" + seed, vertices = verts, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>3D 불꽃 메시 하나: 확 뻗거나 부풀고 → 식으며 → 노이즈 구멍으로 녹아 사라짐</summary>
    public class FxMesh : MonoBehaviour
    {
        public enum Kind { Flame, Ball, Spike, Core, Tracer, Ring }
        public static readonly int HeatId = Shader.PropertyToID("_Heat"), DissolveId = Shader.PropertyToID("_Dissolve"), SeedId = Shader.PropertyToID("_Seed");
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, delay, spin;
        Vector3 size, drift, spinAxis;
        Kind kind;

        static readonly int ColCoreId = Shader.PropertyToID("_ColCore"), ColHotId = Shader.PropertyToID("_ColHot"), ColMidId = Shader.PropertyToID("_ColMid"),
            ColEdgeId = Shader.PropertyToID("_ColEdge"), ColAshId = Shader.PropertyToID("_ColAsh"), OutlineColId = Shader.PropertyToID("_OutlineColor"),
            IntensityId = Shader.PropertyToID("_Intensity");

        /// <summary>탄 색으로 물들임: 흰 심지는 남기고 뜨거운 층 → 탄 색 → 짙은 탄 색 테 → 그을음, 외곽선도 탄 색의 짙은 잉크.
        /// 어두운 탄 색도 불꽃처럼 보이게 밝기를 끌어올림</summary>
        public FxMesh Tint(Color c)
        {
            FxTint.Apply(mpb, c);
            mr.SetPropertyBlock(mpb);
            return this;
        }

        // 풀링 (2026-11-20): 메시와 재질은 호출 값으로 교체하고 렌더러와 재질 블록은 보관한다.
        MeshFilter mf;
        static readonly System.Func<GameObject> factory = Create;
        static GameObject Create()
        {
            var go = new GameObject("FxMesh"); VividLayer.Set(go);
            var f = go.AddComponent<FxMesh>();
            f.mf = go.AddComponent<MeshFilter>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            return go;
        }

        public static FxMesh Spawn(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 size, float life, float delay, Vector3 drift, Kind kind)
        {
            if (!mesh || !mat) return null;
            var go = GameObjectPool.Get("FxMesh", factory);
            go.transform.SetPositionAndRotation(pos, rot);
            var f = go.GetComponent<FxMesh>();
            f.mf.sharedMesh = mesh;
            f.mr.sharedMaterial = mat;
            f.mpb.Clear();
            f.mpb.SetFloat(SeedId, Random.Range(0f, 100f));
            f.born = Time.time; f.life = life; f.delay = delay; f.size = size; f.drift = drift; f.kind = kind;
            // 바늘 가시는 돌지 않음 (일렁임 없이 곧게 찌름)
            f.spin = kind == Kind.Flame ? Random.Range(-240f, 240f) : kind == Kind.Ball ? Random.Range(-120f, 120f) : 0f;
            f.spinAxis = kind == Kind.Ball ? Random.onUnitSphere : Vector3.forward;
            f.Pose(-1);
            return f;
        }

        void LateUpdate()
        {
            float t = (Time.time - born - delay) / life;
            if (t >= 1) { GameObjectPool.Release(gameObject); return; }
            if (t >= 0)
            {
                transform.position += drift * Time.deltaTime;
                if (kind != Kind.Tracer) drift *= Mathf.Exp(-6f * Time.deltaTime);   // 탄도는 감속 없이 쭉
                transform.rotation = transform.rotation * Quaternion.AngleAxis(spin * Time.deltaTime, spinAxis);
            }
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) { transform.localScale = Vector3.zero; return; }
            Evaluate(kind, size, t, out var sc, out float heat, out float dis);
            transform.localScale = sc;
            mpb.SetFloat(HeatId, heat);
            mpb.SetFloat(DissolveId, dis);
            mr.SetPropertyBlock(mpb);
        }

        /// <summary>수명 비율 t(0~1)에서의 크기 · 열(_Heat) · 녹음(_Dissolve). 스프라이트로 구울 때도 같은 곡선을 씀 (Editor/FxImpactBaker)</summary>
        public static void Evaluate(Kind kind, Vector3 size, float t, out Vector3 sc, out float heat, out float dis)
        {
            if (kind == Kind.Core)
            {
                // 총구 섬광 덩어리: 0.3 동안 펑 부풀고(0.25 → 1.1) 하얗게 타다 오그라들며 천천히 녹음
                float e = Mathf.Clamp01(t / 0.3f);
                float pop = t < 0.3f ? Mathf.Lerp(0.25f, 1.1f, Mathf.Sin(e * Mathf.PI * 0.5f)) : Mathf.Lerp(1.1f, 0.65f, (t - 0.3f) / 0.7f);
                sc = size * pop;
                heat = 1 - Mathf.Clamp01((t - 0.35f) / 0.65f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.4f) / 0.6f));
            }
            else if (kind == Kind.Tracer)
            {
                // 탄도: 앞 15% 동안 쭉 늘어나며 나와서 날아가며 점점 가늘어지고 뒤 절반에 녹음
                float e = Mathf.Clamp01(t / 0.15f); e = 1 - (1 - e) * (1 - e);
                sc = new Vector3(size.x * (1 - 0.5f * t), size.y * (1 - 0.5f * t), size.z * (Mathf.Lerp(0.3f, 0.9f, e) + 0.3f * t));
                heat = 1 - Mathf.Clamp01((t - 0.45f) / 0.55f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.5f) / 0.5f));
            }
            else if (kind == Kind.Ring)
            {
                // 충격 고리: 퍼지고(0.2 → 1) 굵기(z)는 얇아지며 뒤 절반에 녹음
                float e = 1 - Mathf.Pow(1 - t, 3);
                float r = Mathf.Lerp(0.2f, 1f, e);
                sc = new Vector3(size.x * r, size.y * r, size.z * Mathf.Lerp(1f, 0.3f, t));
                heat = 1 - Mathf.Clamp01((t - 0.35f) / 0.65f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.45f) / 0.55f));
            }
            else if (kind == Kind.Spike)
            {
                // 앞 20% 동안 총구에서 쭉 뻗어 나오고(0.3 → 1) 잠깐 하얗게 버틴 뒤,
                // 굵기는 계속 줄며 더 날카로워지고 뒤 절반에 천천히 식으며 녹음
                float e = Mathf.Clamp01(t / 0.2f); e = 1 - (1 - e) * (1 - e) * (1 - e);
                float len = Mathf.Lerp(0.3f, 1f, e) + 0.12f * t;
                float wid = Mathf.Lerp(0.6f, 1f, e) * Mathf.Lerp(1f, 0.35f, t);
                sc = new Vector3(size.x * wid, size.y * wid, size.z * len);
                heat = 1 - Mathf.Clamp01((t - 0.4f) / 0.6f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.48f) / 0.52f));
            }
            else if (kind == Kind.Flame)
            {
                // 0.25 동안 확 뻗고(길이 0.25 → 1.1, 굵기 0.5 → 1) 조금 더 늘어나며 식고 녹음
                float e = Mathf.Clamp01(t / 0.25f); e = 1 - (1 - e) * (1 - e) * (1 - e);
                float len = Mathf.Lerp(0.25f, 1.1f, e) + 0.15f * t;
                float wid = Mathf.Lerp(0.5f, 1f, e) * (1 - 0.25f * t);
                sc = new Vector3(size.x * wid, size.y * wid, size.z * len);
                heat = 1 - Mathf.Clamp01((t - 0.3f) / 0.7f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.3f) / 0.7f));
            }
            else
            {
                // 0.15 동안 튀어나오듯 부풀고(살짝 넘쳤다 돌아옴) 천천히 커지며 그을린 색으로 식고 녹음
                float e = Mathf.Clamp01(t / 0.15f);
                float pop = e < 1 ? Mathf.Lerp(0.3f, 1.12f, Mathf.Sin(e * Mathf.PI * 0.5f)) : 1.12f - 0.12f * Mathf.Clamp01((t - 0.15f) / 0.15f);
                sc = size * pop * (1 + 0.3f * t);
                heat = 1 - Mathf.Clamp01((t - 0.25f) / 0.6f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.35f) / 0.65f));
            }
        }
    }

    /// <summary>카메라를 보는 스프라이트 한 장 (불덩이 / 연기)</summary>
    public class FxSprite : MonoBehaviour
    {
        public enum Style { Fire, Smoke, Flash }
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, delay, size, roll, rollSpeed;
        Vector3 drift;
        Style style;
        Vector3 dir;

        public static void Spawn(Material mat, Vector3 pos, float size, float life, float delay, Vector3 drift, Style style, Vector3 dir = default)
        {
            var go = new GameObject(style == Style.Smoke ? "Smoke" : style == Style.Flash ? "Flash" : "FirePuff"); VividLayer.Set(go);
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.quad;
            var s = go.AddComponent<FxSprite>();
            s.mr = go.AddComponent<MeshRenderer>();
            s.mr.sharedMaterial = mat;
            s.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.mr.receiveShadows = false;
            s.mpb = new MaterialPropertyBlock();
            s.born = Time.time; s.life = life; s.delay = delay; s.size = size; s.drift = drift; s.style = style;
            s.dir = dir;
            s.roll = style == Style.Flash ? 0 : Random.Range(0f, 360f);
            s.rollSpeed = style == Style.Flash ? 0 : Random.Range(-90f, 90f);
            s.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born - delay) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            transform.position += drift * Time.deltaTime;
            drift *= Mathf.Exp(-4f * Time.deltaTime);
            roll += rollSpeed * Time.deltaTime;
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) return;
            var cam = Camera.main;
            if (cam && style == Style.Flash)
            {
                // 섬광 그림의 +x(긴 가시) = 화면에 비친 총구 방향
                var v = cam.transform.InverseTransformDirection(dir);
                roll = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            }
            if (cam) transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, roll);
            float sc; Color c;
            if (style == Style.Flash)
            {
                // 크게 번쩍 → 살짝 커지며 끝에서 사라짐
                sc = Mathf.Lerp(0.75f, 1.15f, t);
                c = Color.white;
                c.a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            }
            else if (style == Style.Fire)
            {
                // 0.06초 만에 확 부풀고(0.35→1) 천천히 더 커지며 → 하얗게 뜨겁다가 그을린 갈색으로 식으며 사라짐
                float pop = t < 0.2f ? 1 - (1 - t / 0.2f) * (1 - t / 0.2f) : 1;
                sc = Mathf.Lerp(0.35f, 1f, pop) * (1 + 0.15f * t);
                float cool = Mathf.Clamp01((t - 0.3f) / 0.5f);
                c = Color.Lerp(Color.white, new Color(0.55f, 0.38f, 0.33f), cool);
                c.a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            }
            else
            {
                // 연기: 부풀며 진해졌다가 옅어짐
                sc = Mathf.Lerp(0.7f, 1.5f, 1 - (1 - t) * (1 - t));
                c = Color.white;
                c.a = t < 0.15f ? t / 0.15f * 0.8f : 0.8f * (1 - (t - 0.15f) / 0.85f);
            }
            transform.localScale = Vector3.one * size * sc;
            mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>선 이펙트: 빛줄기(Ray) · 소용돌이 곡선(Arc)</summary>
    public class FxLine : MonoBehaviour
    {
        LineRenderer lr;
        float born, life, len, width, radius, spin, startAng;
        Vector3 origin, dir;
        Quaternion tilt;
        Color col;
        bool arc;
        const int ArcPts = 18;

        static FxLine Make(Color c, float life, int points)
        {
            var go = new GameObject("FxLine"); VividLayer.Set(go);
            var f = go.AddComponent<FxLine>();
            f.lr = go.AddComponent<LineRenderer>();
            f.lr.sharedMaterial = GunkataFx.LineMat;
            f.lr.positionCount = points;
            f.lr.numCapVertices = 2;
            f.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.lr.receiveShadows = false;
            f.col = c; f.life = life; f.born = Time.time;
            return f;
        }

        /// <summary>총구에서 바깥으로 뻗는 가는 빛줄기 (뿌리 굵게 → 끝 뾰족)</summary>
        public static void Ray(Vector3 from, Vector3 dir, float len, float width, Color c, float life)
        {
            var f = Make(c, life, 2);
            f.origin = from; f.dir = dir.normalized; f.len = len; f.width = width;
            f.Pose(0);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 곡선 (150° 호, 꼬리로 갈수록 가늘게)</summary>
        public static void Arc(Vector3 center, float radius, float spin, Color c, float life)
        {
            var f = Make(c, life, ArcPts);
            f.arc = true;
            f.origin = center; f.radius = radius; f.spin = spin;
            f.startAng = Random.Range(0f, 360f);
            f.tilt = Quaternion.Euler(Random.Range(-28f, 28f), Random.Range(0f, 360f), Random.Range(-28f, 28f));
            f.width = Random.Range(0.12f, 0.2f);
            f.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            Pose(t);
        }

        void Pose(float t)
        {
            var c = col;
            c.a *= t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f;
            if (!arc)
            {
                float grow = 1 - (1 - Mathf.Clamp01(t / 0.35f)) * (1 - Mathf.Clamp01(t / 0.35f));
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir * len * Mathf.Lerp(0.4f, 1f, grow));
                lr.startWidth = width * (1 - t * 0.7f);
                lr.endWidth = 0;
                lr.startColor = c;
                var e = c; e.a *= 0.3f; lr.endColor = e;
                return;
            }
            // 소용돌이: 반지름이 조금 커지며 빠르게 돎
            float r = radius * (1 + 0.3f * t);
            float a0 = startAng + spin * (Time.time - born);
            for (int i = 0; i < ArcPts; i++)
            {
                float a = (a0 - Mathf.Sign(spin) * i * 150f / (ArcPts - 1)) * Mathf.Deg2Rad;   // 꼬리는 도는 방향 뒤쪽
                lr.SetPosition(i, origin + tilt * new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            lr.startWidth = width;
            lr.endWidth = 0;
            lr.startColor = c;
            var tail = c; tail.a = 0; lr.endColor = tail;
        }
    }
    /// <summary>특성 탄도 덩어리 이동 (GunkataFx.TraitBolt): T초 동안 a → b, 지나가며 부스러기, 도착하면 사라짐</summary>
    public class TraitBoltMove : MonoBehaviour
    {
        public Vector3 a, b; public float T, spin, size; public int trait; public Color col; public bool crumbs;
        float born = -1, nextCrumb;
        void LateUpdate()
        {
            if (born < 0) born = Time.time;
            float u = (Time.time - born) / T;
            if (u >= 1) { Destroy(gameObject); return; }
            transform.position = Vector3.Lerp(a, b, u);
            if (spin != 0) transform.rotation = Quaternion.AngleAxis(spin * Time.deltaTime, (b - a).normalized) * transform.rotation;
            if (crumbs && Time.time >= nextCrumb)
            {
                nextCrumb = Time.time + 0.008f;
                GunkataFx.BoltCrumb(trait, transform.position, (b - a).normalized, col, Mathf.Max(0.6f, size * 1.6f));
            }
        }
    }
}
