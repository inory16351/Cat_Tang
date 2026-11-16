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
        static bool Init()
        {
            // 플레이 진입 시 도메인 리로드를 끈 프로젝트라(DisableDomainReload) 정적 필드가 판을 넘어 남음.
            // 런타임에 만든 재질 · 메시는 플레이 종료 때 파괴되므로, 하나라도 죽었으면 다시 만듦
            if (inited && fireMat && spikeMat && muzzleMat && sandMat && sootMat && lineMat && quad) return true;
            inited = true;
            var sh = Shader.Find("Sprites/Default");
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
            // 충격 고리: 가끔 빠지고, 기울기 · 크기 제각각
            if (Random.value < 0.7f)
                FxMesh.Spawn(FxMeshes.Torus(), spikeMat, p + d * Random.Range(0.35f, 0.7f) * scale,
                    Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(-25f, 25f), 0),
                    Vector3.one * (full ? 1.25f : 0.85f) * Random.Range(0.7f, 1.25f) * scale, Random.Range(0.2f, 0.3f), Random.Range(0f, 0.02f), d * 2f * scale, FxMesh.Kind.Ring);
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
            if (!Init()) return;
            System.Func<FxMesh, FxMesh> T = f => f && tint.HasValue ? f.Tint(tint.Value) : f;
            float r = Mathf.Max(0.5f, radius);
            var up = Vector3.up;
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

        /// <summary>총알 타격: 탄 색 섬광 덩어리 + 맞은 면에서 튀어나오는 가시(주로 쏜 쪽 · 옆, 몇 개는 관통 방향) + 탄도에 수직인 충격 고리.
        /// crit = 금빛 네 갈래 별 가시 + 큰 고리 겹침. small = 관통 · 튕김용 작은 불꽃(고리 없음)</summary>
        public static void HitImpact(Vector3 p, Vector3 dir, Color col, bool crit, float scale = 1f, bool small = false)
        {
            if (!Init()) return;
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
            if (!small)
                FxMesh.Spawn(FxMeshes.Torus(), spikeMat, p, Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0),
                    Vector3.one * 1.0f * s, 0.2f, 0, back * 0.6f, FxMesh.Kind.Ring)?.Tint(col);
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
            FxMesh.Spawn(FxMeshes.Torus(), spikeMat, p, Quaternion.LookRotation(dir, Vector3.up), Vector3.one * 2.1f * s, 0.26f, 0.03f, Vector3.zero, FxMesh.Kind.Ring);
        }

        /// <summary>닷지 원: 바닥에 탄 색 충격 고리 두 겹이 퍼지고, 몸을 감싸는 자이로 고리 두 개가 엇갈려 돌며,
        /// 몸에서 바깥으로 짧은 빛살 가시가 사방으로 튐. big = 닷지 액션(저스트), 아니면 확률 회피(작게)</summary>
        public static void DodgeBurst(Vector3 feet, Color col, bool big)
        {
            if (!Init()) return;
            float s = big ? 1f : 0.65f;
            var up = Vector3.up;
            var body = feet + up * 0.9f;
            var flat = Quaternion.LookRotation(up, Vector3.forward);
            FxMesh.Spawn(FxMeshes.Torus(), spikeMat, feet + up * 0.06f, flat, new Vector3(5.2f * s, 5.2f * s, 3f), 0.38f, 0, Vector3.zero, FxMesh.Kind.Ring)?.Tint(col);
            FxMesh.Spawn(FxMeshes.Torus(), spikeMat, feet + up * 0.04f, flat * Quaternion.Euler(0, 0, 30f), new Vector3(3.4f * s, 3.4f * s, 4f), 0.32f, 0.07f, Vector3.zero, FxMesh.Kind.Ring)?.Tint(col);
            // 몸을 감싸는 고리 두 개: 서로 다른 기울기로
            for (int k = 0; k < 2; k++)
            {
                var rot = Quaternion.Euler(Random.Range(55f, 80f) * (k == 0 ? 1 : -1), Random.Range(0f, 360f), 0);
                FxMesh.Spawn(FxMeshes.Torus(), spikeMat, body, rot, new Vector3(2.4f * s, 2.4f * s, 2f), 0.3f, k * 0.04f, Vector3.zero, FxMesh.Kind.Ring)?.Tint(Color.Lerp(col, Color.white, 0.35f));
            }
            // 가운데 섬광
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

        /// <summary>둥근 빛 폭발 (공명 사격 섬광 · 범위 기절 · 보상 등 FxBurst.Gold 자리): 탄 색 섬광 덩어리 + 고리 + 방사 가시</summary>
        public static void Flare(Vector3 at, float size, Color col)
        {
            if (!Init()) return;
            float s = Mathf.Max(0.4f, size * 0.5f);
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), spikeMat, at, Random.rotation, Vector3.one * 0.9f * s, 0.2f, 0, Vector3.zero, FxMesh.Kind.Core)?.Tint(col);
            FxMesh.Spawn(FxMeshes.Torus(), spikeMat, at, Random.rotation, Vector3.one * 2.2f * s, 0.28f, 0.02f, Vector3.zero, FxMesh.Kind.Ring)?.Tint(col);
            int n = 6 + Random.Range(0, 4);
            for (int k = 0; k < n; k++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.7f; d.Normalize();
                float w = Random.Range(0.12f, 0.22f) * s;
                FxMesh.Spawn(FxMeshes.Spike(Random.Range(0, 3)), spikeMat, at + d * 0.2f * s, Quaternion.LookRotation(d, Vector3.up),
                    new Vector3(w, w, Random.Range(0.7f, 1.4f) * s), Random.Range(0.18f, 0.28f), Random.Range(0f, 0.04f), d * Random.Range(2f, 4f) * s, FxMesh.Kind.Spike)?.Tint(col);
            }
        }

        /// <summary>수평 방향 d를 좌우 ±yaw° · 위아래 y 성분(lo~hi)만큼 무작위로 흔든 단위 벡터</summary>
        static Vector3 Jitter(Vector3 d, float yaw, float lo, float hi)
        {
            var v = Quaternion.AngleAxis(Random.Range(-yaw, yaw), Vector3.up) * d;
            v.y += Random.Range(lo, hi);
            return v.normalized;
        }

        /// <summary>일반 사격 총구 섬광 (리볼버, 2026-11-16 개편): 전부 가는 빨간 3D (muzzleMat).
        /// - 총구: 작은 섬광 덩어리 + 총열 방향 길고 가는 바늘 + 앞쪽으로 비스듬한 가는 곁가시 2개(좌우 각도 다름) + 얇은 고리 + 아주 가는 탄도
        /// - 리볼버 강조 (gun을 주면): 실린더와 총열 틈에서 양옆으로 칙 새는 가스 가시(리볼버만의 특징) · 실린더 둘레 작은 고리 · 공이 자리 불티
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
            // 얇은 고리
            FxMesh.Spawn(FxMeshes.Torus(), mat, p + d * 0.22f * scale, Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-12f, 12f), 0),
                new Vector3(0.38f, 0.38f, 0.35f) * scale, 0.13f, 0, d * 1.2f * scale, FxMesh.Kind.Ring);
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
                // 실린더 둘레 작은 고리 (철컥 돌아간 느낌)
                var cyl = gun.TransformPoint(new Vector3(0, mz.y * 0.7f, mz.z * 0.3f));
                FxMesh.Spawn(FxMeshes.Torus(), mat, cyl, Quaternion.LookRotation(fwd, up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
                    new Vector3(0.2f, 0.2f, 0.45f) * scale, 0.12f, 0.02f, Vector3.zero, FxMesh.Kind.Ring);
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
            if (!Init() || smokeMats.Length == 0) return;
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

        static Material[] Load(string set)
        {
            if (sets.TryGetValue(set, out var m) && (m.Length == 0 || m[0])) return m;   // 지난 판에 파괴된 재질이면 다시 만듦
            var sh = Shader.Find("Sprites/Default");
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < 32; i++)
            {
                var t = Resources.Load<Texture2D>("FX/anim/" + set + "_" + i);
                if (!t) break;
                list.Add(new Material(sh) { mainTexture = t, renderQueue = 3004 });
            }
            return sets[set] = list.ToArray();
        }

        public static bool Spawn(string set, Vector3 pos, Vector3 dir, float size, float life, bool pivotLeft, Vector3 drift = default, float roll = 0)
        {
            var fr = Load(set);
            if (fr.Length == 0) return false;
            var go = new GameObject("Flip_" + set);
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.Quad;
            var f = go.AddComponent<FxFlipbook>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            f.frames = fr; f.origin = pos; f.dir = dir; f.size = size; f.life = life; f.pivotLeft = pivotLeft; f.drift = drift; f.roll = roll;
            f.born = Time.time;
            f.Pose(0);
            return true;
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
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

    /// <summary>코드로 구운 3D 이펙트 메시 (처음 쓸 때 한 번 만들어 공유)</summary>
    public static class FxMeshes
    {
        static Mesh[] flames, balls, spikes;
        static Mesh torus;

        /// <summary>충격 고리: XY 평면의 도넛 (축 = +z), 반지름 0.5 · 굵기 0.06. 둘레가 조금씩 울퉁불퉁</summary>
        public static Mesh Torus()
        {
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
            if (flames == null || !flames[0]) { flames = new Mesh[3]; for (int i = 0; i < 3; i++) flames[i] = BuildFlame(101 + i * 17); }
            return flames[Mathf.Abs(v) % 3];
        }

        /// <summary>울퉁불퉁한 불덩이: 지름 약 1</summary>
        public static Mesh Ball(int v)
        {
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
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.85f);
            mpb.SetColor(ColCoreId, Color.HSVToRGB(h, s * 0.3f, 1f));
            mpb.SetColor(ColHotId, Color.HSVToRGB(h, s * 0.7f, 1f));
            mpb.SetFloat(IntensityId, 1.45f);                 // 흰빛으로 날아가지 않게 (기본 가시 재질 2.2)
            mpb.SetColor(ColMidId, Color.HSVToRGB(h, s, v));
            mpb.SetColor(ColEdgeId, Color.HSVToRGB(h, Mathf.Min(1f, s * 1.1f), v * 0.62f));
            mpb.SetColor(ColAshId, Color.HSVToRGB(h, s * 0.6f, v * 0.28f));
            mpb.SetColor(OutlineColId, Color.HSVToRGB(h, s * 0.7f, 0.13f));
            mr.SetPropertyBlock(mpb);
            return this;
        }

        public static FxMesh Spawn(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 size, float life, float delay, Vector3 drift, Kind kind)
        {
            if (!mesh || !mat) return null;
            var go = new GameObject("Fx" + kind);
            go.transform.SetPositionAndRotation(pos, rot);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var f = go.AddComponent<FxMesh>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.sharedMaterial = mat;
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
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
            if (t >= 1) { Destroy(gameObject); return; }
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
            var go = new GameObject(style == Style.Smoke ? "Smoke" : style == Style.Flash ? "Flash" : "FirePuff");
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
            var go = new GameObject("FxLine");
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
}
