// 방 사이 나무 문 (2026-11-17 사용자: 노란 상자 문 → 나무 문 모델 + 부서지는 연출)
// - 모델: 블렌더 + Codex(Art/tools/blender_kit_woodgate.py) — 미리 쪼개 둔 판자 · 가로대 · 쇠띠 조각(Piece_XX) + 작은 가시 조각(Splinter_X)
//   → 빌더가 Resources/Kit/Ch1/WoodGate.prefab으로 만듦. 키트가 없으면 예전 색 상자를 그림으로 씀.
// - 닫힘(Close): 위에서 쾅 떨어져 박히고 바닥 흙먼지 · 살짝 눌렸다 복원.
// - 부서짐(Break): 0.28초 덜컹거리다(점점 세게) 조각마다 날아감 — 고양이 반대쪽(복도 쪽)으로 튀고, 돌며 바닥에 튕기다 멈춘 뒤 가라앉아 사라짐.
//   가시 조각 · 흙먼지 · 나무 부스러기 · 화면 흔들림.
// RunManager는 SetActive 대신 WoodGate.Close / WoodGate.Break를 부름.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class WoodGate : MonoBehaviour
    {
        public const float Width = 4.6f, Height = 2.5f, Thick = 0.5f;
        const float RattleTime = 0.28f;
        static readonly Color DustCol = FxBurst.MistDust;
        static readonly Color WoodCol = new(0.62f, 0.4f, 0.22f);

        Transform visual;
        BoxCollider box;
        Coroutine anim;

        /// <summary>문 만들기. floorPos = 문 가운데 바닥(로컬), inward = 방 안쪽 방향(문 정면이 이쪽을 봄)</summary>
        public static GameObject Make(ChapterDef ch, Transform parent, string name, Vector3 floorPos, Vector3 inward, Material fallback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = floorPos;
            go.transform.localRotation = Quaternion.LookRotation(inward);
            var g = go.AddComponent<WoodGate>();
            g.box = go.AddComponent<BoxCollider>();
            g.box.center = new Vector3(0, Height / 2, 0);
            g.box.size = new Vector3(Width, Height, Thick);
            // 스테이지별 문(블렌더 + Codex, Art/tools/blender_kit_ch{1,2,3}_gates.py) → 장 기본 나무 문 → 1장 나무 문
            var prefab = Kit.Prefab(ch, Kit.Stage("Gate")) ?? Kit.Prefab(ch, "WoodGate") ?? Kit.Prefab(ch, "@1/WoodGate");
            if (prefab)
            {
                var v = Object.Instantiate(prefab, go.transform, false);
                v.name = "Visual";
                foreach (var c in v.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                // 가시 조각 틀은 숨김 (부서질 때 복제해서 씀)
                foreach (var r in v.GetComponentsInChildren<Renderer>(true))
                    if (r.name.StartsWith("Splinter")) r.enabled = false;
                g.visual = v.transform;
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(cube.GetComponent<Collider>());
                cube.name = "Visual";
                cube.transform.SetParent(go.transform, false);
                cube.transform.localPosition = new Vector3(0, Height / 2, 0);
                cube.transform.localScale = new Vector3(Width, Height, Thick);
                cube.GetComponent<Renderer>().sharedMaterial = fallback;
                g.visual = cube.transform;
            }
            g.home = g.visual.localPosition;
            return go;
        }

        Vector3 home;

        /// <summary>문 닫기 (animate = 위에서 쾅)</summary>
        public static void Close(GameObject g, bool animate = true)
        {
            if (!g) return;
            var wg = g.GetComponent<WoodGate>();
            if (g.activeSelf && (!wg || wg.box.enabled)) return;
            g.SetActive(true);
            if (!wg) return;
            wg.ResetPose();
            if (animate) wg.anim = wg.StartCoroutine(wg.SlamIn());
        }

        /// <summary>문 부수기 (animate = 덜컹 → 조각 날림, 아니면 그냥 사라짐)</summary>
        public static void Break(GameObject g, bool animate = true)
        {
            if (!g || !g.activeSelf) return;
            var wg = g.GetComponent<WoodGate>();
            if (!wg || !animate) { g.SetActive(false); return; }
            if (!wg.box.enabled) return;       // 이미 부서지는 중
            if (wg.anim != null) wg.StopCoroutine(wg.anim);
            wg.ResetPose();
            wg.box.enabled = false;            // 길은 바로 열림 (ResetPose가 다시 켜므로 그 뒤에)
            wg.anim = wg.StartCoroutine(wg.BreakRoutine());
        }

        void ResetPose()
        {
            if (anim != null) { StopCoroutine(anim); anim = null; }
            box.enabled = true;
            visual.gameObject.SetActive(true);
            visual.localPosition = home;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
        }

        void OnDisable() { anim = null; }

        // ---------- 닫힘: 위에서 떨어져 박힘 ----------
        IEnumerator SlamIn()
        {
            const float fall = 0.2f, drop = 3.4f;
            float t = 0;
            while (t < fall)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fall);
                visual.localPosition = home + Vector3.up * drop * (1 - k * k);
                yield return null;
            }
            visual.localPosition = home;
            for (int i = -1; i <= 1; i++)
                FxBurst.Spawn(transform.position + transform.right * (i * 1.6f) + transform.forward * 0.4f + Vector3.up * 0.1f, 1.5f, FxBurst.Kind.Dust, DustCol);
            CameraFollow.Shake(0.07f);
            // 쿵: 살짝 눌렸다 튀어 오름
            t = 0;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.16f);
                float s = 1 - 0.08f * Mathf.Sin(k * Mathf.PI) * (1 - k);
                visual.localScale = new Vector3(1 + (1 - s) * 0.5f, s, 1);
                yield return null;
            }
            visual.localScale = Vector3.one;
            anim = null;
        }

        // ---------- 부서짐 ----------
        IEnumerator BreakRoutine()
        {
            // 1) 덜컹: 문 전체가 앞뒤로 흔들리고 조각들이 조금씩 들썩임 (점점 세게)
            var pieces = new List<Transform>();
            var piecePos = new List<Vector3>();
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                if (!r.name.StartsWith("Splinter") && r.transform != visual) { pieces.Add(r.transform); piecePos.Add(r.transform.localPosition); }
            float t = 0;
            int bumps = 0;
            while (t < RattleTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / RattleTime);
                float amp = Mathf.Lerp(0.02f, 0.09f, k);
                visual.localPosition = home + new Vector3(Mathf.Sin(t * 70f) * amp * 0.6f, 0, Mathf.Sin(t * 53f + 1f) * amp);
                visual.localRotation = Quaternion.Euler(Mathf.Sin(t * 61f) * amp * 25f, 0, Mathf.Sin(t * 47f) * amp * 12f);
                for (int i = 0; i < pieces.Count; i++)
                    pieces[i].localPosition = piecePos[i] + Random.insideUnitSphere * amp * 0.25f;
                if (k > bumps / 3f) { bumps++; FxBurst.Spawn(transform.position + transform.right * Random.Range(-2f, 2f) + Vector3.up * Random.Range(0.5f, 2.2f), 0.9f, FxBurst.Kind.Dust, DustCol); }
                yield return null;
            }
            for (int i = 0; i < pieces.Count; i++) pieces[i].localPosition = piecePos[i];
            visual.localPosition = home;
            visual.localRotation = Quaternion.identity;

            // 2) 펑: 조각마다 복제해 날림 (문은 숨김)
            Burst();
            visual.gameObject.SetActive(false);
            yield return null;
            anim = null;
            ResetPose();                   // 다음에 닫힐 때를 위해 원위치한 뒤 끔
            gameObject.SetActive(false);
        }

        void Burst()
        {
            var parent = transform.parent;
            var center = transform.position + Vector3.up * Height / 2;
            // 고양이 반대쪽(보통 복도 쪽)으로 튐
            var normal = transform.forward;
            var player = PlayerController.Instance;
            float side = player ? Mathf.Sign(Vector3.Dot(transform.position - player.transform.position, normal)) : -1f;
            if (side == 0) side = -1f;
            var away = normal * side;
            float groundY = transform.position.y;
            Mesh fallbackMesh = null;

            var srcs = new List<Renderer>();
            var spl = new List<Renderer>();
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                (r.name.StartsWith("Splinter") ? spl : srcs).Add(r);
            var splinters = spl.ToArray();

            foreach (var r in srcs)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var rt = r.transform;
                // 판자 높이 · 가로 위치에 따라: 위쪽 조각일수록 높이, 가장자리 조각일수록 옆으로
                var local = transform.InverseTransformPoint(r.bounds.center);
                float hx = local.x / (Width / 2), hy = Mathf.Clamp01(local.y / Height);
                var v = away * Random.Range(3.5f, 7.5f)
                      + transform.right * (hx * Random.Range(1.5f, 3.5f) + Random.Range(-0.8f, 0.8f))
                      + Vector3.up * Random.Range(2.5f, 5.5f) * (0.6f + hy * 0.7f);
                SpawnDebris(parent, mf.sharedMesh, r.sharedMaterials, rt.position, rt.rotation, rt.lossyScale, v, groundY);
            }
            // 가시 조각 (틀이 없으면 판자 색 작은 상자)
            int n = Random.Range(12, 17);
            for (int i = 0; i < n; i++)
            {
                Mesh mesh; Material[] mats; Vector3 scale;
                if (splinters.Length > 0)
                {
                    var s = splinters[Random.Range(0, splinters.Length)];
                    mesh = s.GetComponent<MeshFilter>().sharedMesh;
                    mats = s.sharedMaterials;
                    scale = s.transform.lossyScale * Random.Range(0.8f, 1.3f);
                }
                else
                {
                    if (!fallbackMesh) { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); fallbackMesh = c.GetComponent<MeshFilter>().sharedMesh; Destroy(c); }
                    mesh = fallbackMesh;
                    mats = new[] { RoomBuilder.Mat(WoodCol) };
                    scale = new Vector3(0.06f, 0.06f, Random.Range(0.25f, 0.45f));
                }
                var at = center + transform.right * Random.Range(-Width / 2, Width / 2) + Vector3.up * Random.Range(-1f, 1f);
                var v = away * Random.Range(4f, 10f) + transform.right * Random.Range(-3f, 3f) + Vector3.up * Random.Range(3f, 8f);
                SpawnDebris(parent, mesh, mats, at, Random.rotation, scale, v, groundY, 0.6f);
            }
            // 흙먼지 · 나무 부스러기 · 흔들림
            for (int i = -2; i <= 2; i++)
                FxBurst.Spawn(transform.position + transform.right * (i * 1.1f) + away * 0.3f + Vector3.up * 0.2f, 2.2f, FxBurst.Kind.Dust, DustCol, 0.6f);
            FxBurst.Spawn(center, 3.2f, FxBurst.Kind.Dust, DustCol, 0.7f);
            FxBurst.Spawn(center + away * 0.4f, 2.4f, FxBurst.Kind.Spark, WoodCol);
            CameraFollow.Shake(0.14f);
        }

        static void SpawnDebris(Transform parent, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 vel, float groundY, float lifeMul = 1f)
        {
            var go = new GameObject("GateDebris");
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one;
            go.transform.localScale = new Vector3(scale.x / go.transform.lossyScale.x, scale.y / go.transform.lossyScale.y, scale.z / go.transform.lossyScale.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            var d = go.AddComponent<GateDebris>();
            d.vel = vel;
            d.spin = Random.onUnitSphere * Random.Range(250f, 800f);
            d.groundY = groundY;
            d.life = Random.Range(2.2f, 3.2f) * lifeMul;
            d.rend = mr;
        }
    }

    /// <summary>문 조각 하나: 중력 · 회전 · 바닥 튕김 → 멈춤 → 가라앉아 사라짐 (물리 엔진 안 씀)</summary>
    public class GateDebris : MonoBehaviour
    {
        public Vector3 vel, spin;
        public float groundY, life;
        public Renderer rend;
        const float Gravity = 26f, Sink = 0.5f;
        float t;
        bool resting;

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            if (t > life)
            {
                // 가라앉으며 작아짐
                float k = Mathf.Clamp01((t - life) / Sink);
                transform.position += Vector3.down * dt * 0.6f;
                transform.localScale *= 1 - dt * 3f;
                if (k >= 1) Destroy(gameObject);
                return;
            }
            if (resting) return;
            vel.y -= Gravity * dt;
            transform.position += vel * dt;
            transform.Rotate(spin * dt, Space.World);
            float bottom = rend.bounds.min.y;
            if (bottom < groundY)
            {
                transform.position += Vector3.up * (groundY - bottom);
                if (vel.y < 0)
                {
                    vel.y = -vel.y * 0.3f;
                    vel.x *= 0.55f; vel.z *= 0.55f;
                    spin *= 0.5f;
                    if (vel.y < 1.2f && new Vector2(vel.x, vel.z).magnitude < 1.2f) resting = true;
                }
            }
        }
    }
}
