// 장전 동작 (스피드로더). 장전 진행도(0~1)에 맞춰 팔 자세 · 총 회전 · 탄피 · 스피드로더를 연출.
// 흐름: 총 세워 털기(빈 탄피 6개씩 쏟아짐) → 허리 벨트의 스피드로더에 총을 찍어 넣음 → 빈 로더를 양옆으로 털어 버림
//      → 철컥 닫기(반짝, 완벽 장전 구간 0.45~0.65와 맞춤) → 총 두 바퀴 돌려 쥐고 조준으로.
// 완벽 장전 · 즉시 장전으로 일찍 끝나면 철컥 + 한 바퀴 돌리기만 빠르게.
// 불릿 파티 중엔 손대지 않음 (궁이 끝나면 지금 단계 자세부터 다시).
using UnityEngine;

namespace CatTang
{
    public class ReloadMotion : MonoBehaviour
    {
        PlayerController pc;
        KataPoser poser;
        int phase = -1, applied = -1;
        bool ejected, loadersOut, tossed, snapped;
        Transform loaderR, loaderL;
        float finaleStart = -1, finaleSpin;

        static Material brass, steel, copper;
        static Mesh cyl, sph;

        // ---------- 자세 (캐릭터 기준 방향, z = 앞, x = 오른쪽) ----------
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z).normalized;
        // 털기: 두 총을 얼굴 옆으로 세움 (총구 하늘)
        static readonly KataPose Flick = new KataPose { rArm = V(0.45f, 0.85f, 0.35f), lArm = V(-0.45f, 0.85f, 0.35f), lean = -6 };
        // 찍기: 허리 옆 벨트로 총구를 내림, 몸을 살짝 숙임
        static readonly KataPose Dip = new KataPose { rArm = V(0.32f, -1f, 0.12f), lArm = V(-0.32f, -1f, 0.12f), lean = 14, side = 0 };
        // 털어 버리기: 두 팔을 양옆으로 휙
        static readonly KataPose Toss = new KataPose { rArm = V(1f, 0.15f, 0.25f), lArm = V(-1f, 0.15f, 0.25f), lean = -4 };
        // 철컥: 가슴 앞에서 두 총을 모아 위로 튕김
        static readonly KataPose Snap = new KataPose { rArm = V(-0.15f, 0.45f, 1f), lArm = V(0.15f, 0.45f, 1f), lean = -3 };
        // 돌려 쥐기: 낮게 앞으로
        static readonly KataPose Twirl = new KataPose { rArm = V(0.3f, -0.35f, 1f), lArm = V(-0.3f, -0.35f, 1f) };

        void Awake()
        {
            pc = GetComponent<PlayerController>();
        }

        void Start()
        {
            poser = pc ? pc.poser : null;
            if (pc) { pc.OnReloadStart += Begin; pc.OnReloadEnd += End; pc.OnFired += Fired; }
        }

        void OnDestroy()
        {
            if (pc) { pc.OnReloadStart -= Begin; pc.OnReloadEnd -= End; pc.OnFired -= Fired; }
            DropLoaders(false);
        }

        void Begin(float dur)
        {
            phase = applied = -1;
            ejected = loadersOut = tossed = snapped = false;
            finaleStart = -1;
        }

        void End(bool perfect)
        {
            if (!poser || pc.UltimateActive) { Clear(); return; }
            if (phase < 0 && !pc.rev.reloading) { return; }          // 장전 없이 실린더만 초기화된 경우
            if (loadersOut && !tossed) DropLoaders(true);
            // 아직 철컥 전에 끝남(완벽 · 즉시 장전) → 철컥 + 빠른 한 바퀴
            if (!snapped) { snapped = true; Clack(); }
            if (phase < 4) { finaleStart = Time.time; finaleSpin = 360f; poser.Pose(Twirl, 0.06f); }
            else { poser.Release(0.1f); ResetGuns(); }
            phase = -1;
        }

        void Fired(int slot, bool ok)
        {
            if (finaleStart < 0) return;
            finaleStart = -1;
            ResetGuns();
            poser.Release(0.05f);
        }

        void Clear()
        {
            phase = -1; finaleStart = -1;
            DropLoaders(true);
            ResetGuns();
        }

        void ResetGuns()
        {
            if (!poser) return;
            poser.gunExtraR = poser.gunExtraL = Quaternion.identity;
        }

        void Update()
        {
            if (!poser || !pc) return;
            if (pc.UltimateActive) { applied = -1; ResetGuns(); return; }

            // 끝난 뒤 돌려 쥐기
            if (finaleStart >= 0)
            {
                float f = (Time.time - finaleStart) / 0.24f;
                if (f >= 1) { finaleStart = -1; ResetGuns(); poser.Release(0.1f); return; }
                var q = Quaternion.Euler(-finaleSpin * EaseOut(f), 0, 0);
                poser.gunExtraR = poser.gunExtraL = q;
                return;
            }
            if (!pc.rev.reloading) return;

            float p = pc.ReloadProgress;
            phase = p < 0.2f ? 0 : p < 0.42f ? 1 : p < 0.5f ? 2 : p < 0.6f ? 3 : 4;
            if (phase != applied)
            {
                applied = phase;
                switch (phase)
                {
                    case 0: poser.Pose(Flick, 0.07f); break;
                    case 1: poser.Pose(Dip, 0.08f); break;
                    case 2: poser.Pose(Toss, 0.05f); break;
                    case 3: poser.Pose(Snap, 0.05f); break;
                    case 4: poser.Pose(Twirl, 0.08f); break;
                }
            }

            switch (phase)
            {
                case 0:
                {
                    // 총을 세운 채 손목으로 탈탈 (총 기준 위아래로 까딱)
                    float k = Mathf.Clamp01(p / 0.2f);
                    float shake = Mathf.Sin(k * Mathf.PI * 5f) * 28f * (1 - k * 0.6f);
                    poser.gunExtraR = Quaternion.Euler(shake, 0, 0);
                    poser.gunExtraL = Quaternion.Euler(-shake, 0, 0);
                    if (!ejected && p > 0.05f) { ejected = true; Eject(poser.gunR, poser.muzzleR); Eject(poser.gunL, poser.muzzleL); }
                    break;
                }
                case 1:
                {
                    ResetGuns();
                    // 벨트에서 로더가 올라와 실린더 뒤에 꽂힘 (0.22 → 0.34)
                    if (!loadersOut && p > 0.22f) { loadersOut = true; loaderR = MakeLoader(); loaderL = MakeLoader(); }
                    float k = Mathf.InverseLerp(0.22f, 0.34f, p);
                    bool justSeated = k >= 1 && loaderR && loaderR.name != "Seated";
                    FollowGun(loaderR, poser.gunR, poser.muzzleR, true, k);
                    FollowGun(loaderL, poser.gunL, poser.muzzleL, false, k);
                    if (justSeated)
                    {
                        loaderR.name = "Seated";
                        GunkataFx.Spark(Cylinder(poser.gunR, poser.muzzleR), 0.18f);
                        GunkataFx.Spark(Cylinder(poser.gunL, poser.muzzleL), 0.18f);
                    }
                    break;
                }
                case 2:
                {
                    FollowGun(loaderR, poser.gunR, poser.muzzleR, true, 1);
                    FollowGun(loaderL, poser.gunL, poser.muzzleL, false, 1);
                    if (!tossed && p > 0.44f) DropLoaders(true);
                    break;
                }
                case 3:
                {
                    if (!tossed) DropLoaders(true);
                    // 철컥: 위로 튕겼다 돌아옴
                    float k = Mathf.InverseLerp(0.5f, 0.6f, p);
                    float flick = Mathf.Sin(Mathf.Clamp01(k * 1.6f) * Mathf.PI) * -35f;
                    poser.gunExtraR = poser.gunExtraL = Quaternion.Euler(flick, 0, 0);
                    if (!snapped && p > 0.53f) { snapped = true; Clack(); }
                    break;
                }
                case 4:
                {
                    // 두 바퀴 돌려 쥠
                    float k = Mathf.InverseLerp(0.6f, 0.97f, p);
                    var q = Quaternion.Euler(-720f * EaseInOut(k), 0, 0);
                    poser.gunExtraR = poser.gunExtraL = q;
                    break;
                }
            }
        }

        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1 - (1 - x) * (1 - x) * (1 - x); }
        static float EaseInOut(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2; }

        // ---------- 위치 ----------
        static float GunLen(Transform gun, Transform muzzle) => gun && muzzle ? Mathf.Max(0.15f, (muzzle.position - gun.position).magnitude) : 0.3f;

        /// <summary>실린더 가운데: 손잡이에서 총구 쪽 1/4, 윗면으로 조금</summary>
        static Vector3 Cylinder(Transform gun, Transform muzzle)
        {
            if (!gun) return Vector3.zero;
            if (!muzzle) return gun.position + gun.forward * 0.08f;
            return Vector3.Lerp(gun.position, muzzle.position, 0.22f) + gun.up * GunLen(gun, muzzle) * 0.06f;
        }

        // ---------- 탄피 ----------
        void Eject(Transform gun, Transform muzzle)
        {
            if (!gun) return;
            EnsureMats();
            var c = Cylinder(gun, muzzle);
            float k = GunLen(gun, muzzle) / 0.3f;
            float ground = transform.position.y;
            for (int i = 0; i < 6; i++)
            {
                var a = i * 60f * Mathf.Deg2Rad;
                var ring = (gun.right * Mathf.Cos(a) + gun.up * Mathf.Sin(a)) * 0.03f * k;
                var go = new GameObject("SpentCasing");
                go.transform.position = c + ring;
                go.transform.rotation = gun.rotation * Quaternion.Euler(90, 0, 0);
                var body = Part(go.transform, cyl, brass, Vector3.zero, new Vector3(0.022f, 0.026f, 0.022f) * k);
                body.localRotation = Quaternion.identity;
                // 총구가 하늘 → 실린더 뒤(-forward)는 아래: 쏟아지듯 떨어지고 옆으로 흩어짐
                var v = -gun.forward * Random.Range(1.5f, 3f) + (ring.normalized + Random.insideUnitSphere * 0.6f) * Random.Range(1.2f, 2.6f);
                Tumble.Add(go, v, Random.insideUnitSphere * 900f, ground + 0.02f, Random.Range(0.9f, 1.3f), 0.35f, 0.55f);
            }
            GunkataFx.Smoke(c, 0.6f * k, 0.5f);
        }

        // ---------- 스피드로더 ----------
        Transform MakeLoader()
        {
            EnsureMats();
            float k = GunLen(poser.gunR, poser.muzzleR) / 0.3f * 1.25f;     // 위에서 내려다봐도 보이게 조금 크게
            var root = new GameObject("Speedloader").transform;
            var model = Resources.Load<GameObject>("Props/Speedloader");
            if (model)
            {
                var m = Instantiate(model, root);
                m.transform.localScale *= k;
            }
            else
            {
                // 남색 쇠 원판 + 손잡이 꼭지 + 아래로 6발 (탄두가 -y)
                Part(root, cyl, steel, new Vector3(0, 0.012f, 0) * k, new Vector3(0.1f, 0.012f, 0.1f) * k);
                Part(root, cyl, steel, new Vector3(0, 0.035f, 0) * k, new Vector3(0.04f, 0.018f, 0.04f) * k);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad;
                    var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.032f * k;
                    Part(root, cyl, brass, o + new Vector3(0, -0.03f, 0) * k, new Vector3(0.026f, 0.03f, 0.026f) * k).name = "Round";
                    Part(root, sph, copper, o + new Vector3(0, -0.062f, 0) * k, new Vector3(0.024f, 0.03f, 0.024f) * k).name = "Round";
                }
            }
            root.position = transform.position + Vector3.up * 0.8f;
            return root;
        }

        /// <summary>로더를 허리 벨트(시작) → 실린더 뒤(k=1)로. 탄두(-y)가 총구 방향</summary>
        void FollowGun(Transform loader, Transform gun, Transform muzzle, bool right, float k)
        {
            if (!loader || !gun) return;
            var model = pc.model ? pc.model : transform;
            var belt = model.position + model.up * 0.78f + model.right * (right ? 0.26f : -0.26f) + model.forward * 0.08f;
            var seat = Cylinder(gun, muzzle) - gun.forward * GunLen(gun, muzzle) * 0.2f;
            float e = EaseOut(k);
            loader.position = Vector3.Lerp(belt, seat, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.12f;
            var seatRot = Quaternion.FromToRotation(Vector3.down, gun.forward) ;
            var beltRot = Quaternion.Euler(0, model.eulerAngles.y, 0);
            loader.rotation = Quaternion.Slerp(beltRot, seatRot, e);
        }

        void DropLoaders(bool fling)
        {
            tossed = true;
            foreach (var l in new[] { loaderR, loaderL })
            {
                if (!l) continue;
                if (!fling) { Destroy(l.gameObject); continue; }
                // 탄은 실린더에 남기고 빈 로더만 던짐
                foreach (Transform c in l) if (c.name == "Round") Destroy(c.gameObject);
                var model = pc && pc.model ? pc.model : transform;
                var side = l == loaderR ? model.right : -model.right;
                var v = side * Random.Range(3.5f, 5f) + Vector3.up * Random.Range(2.5f, 3.5f) - model.forward * 0.8f;
                Tumble.Add(l.gameObject, v, Random.insideUnitSphere * 720f, transform.position.y + 0.03f, Random.Range(1.0f, 1.4f), 0.3f, 0.4f);
            }
            loaderR = loaderL = null;
        }

        void Clack()
        {
            foreach (var (g, m) in new[] { (poser.gunR, poser.muzzleR), (poser.gunL, poser.muzzleL) })
            {
                if (!g) continue;
                var c = Cylinder(g, m);
                GunkataFx.Flash(c + g.up * 0.05f, g.forward, 0.55f, 0.09f);
            }
            CameraFollow.Shake(0.03f);
        }

        // ---------- 재질 · 조각 ----------
        static void EnsureMats()
        {
            if (brass) return;
            var sh = Shader.Find("CatTang/Toon");
            Material M(Color c, Color shade)
            {
                var m = new Material(sh ? sh : Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", c);
                if (m.HasProperty("_ShadeColor")) m.SetColor("_ShadeColor", shade);
                if (m.HasProperty("_SecondShadeColor")) m.SetColor("_SecondShadeColor", shade * 0.7f);
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 1.6f);
                if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.9f);
                return m;
            }
            brass = M(new Color(1f, 0.78f, 0.32f), new Color(0.78f, 0.55f, 0.35f));
            copper = M(new Color(0.85f, 0.45f, 0.25f), new Color(0.7f, 0.5f, 0.45f));
            steel = M(new Color(0.24f, 0.22f, 0.32f), new Color(0.6f, 0.58f, 0.75f));
            cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            sph = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        }

        static Transform Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }

    /// <summary>던져진 작은 물건: 중력 · 바닥에서 통통 튐 · 돌기 · 끝에 작아지며 사라짐</summary>
    public class Tumble : MonoBehaviour
    {
        Vector3 v, spin;
        float ground, life, bounce, born, fric;

        public static void Add(GameObject go, Vector3 v, Vector3 spin, float ground, float life, float bounce, float friction)
        {
            var t = go.AddComponent<Tumble>();
            t.v = v; t.spin = spin; t.ground = ground; t.life = life; t.bounce = bounce; t.fric = friction; t.born = Time.time;
        }

        void Update()
        {
            float dt = Time.deltaTime, age = Time.time - born;
            if (age > life) { Destroy(gameObject); return; }
            v += Vector3.down * 22f * dt;
            var p = transform.position + v * dt;
            if (p.y < ground)
            {
                p.y = ground;
                if (v.y < -1.5f) { v.y = -v.y * bounce; v.x *= fric; v.z *= fric; spin *= 0.6f; }
                else { v.y = 0; v.x *= 0.9f; v.z *= 0.9f; spin *= 0.9f; }
            }
            transform.position = p;
            transform.rotation = Quaternion.Euler(spin * dt) * transform.rotation;
            float fade = Mathf.Clamp01((life - age) / 0.25f);
            if (fade < 1) transform.localScale = Vector3.one * fade;
        }
    }
}
