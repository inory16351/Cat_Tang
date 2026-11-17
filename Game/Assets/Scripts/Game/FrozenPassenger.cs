// 2장 굳은 승객 석상 (2026-11-17 사용자 확정: 몽환감 보강 — '굳은 승객이 하수인으로 깨어남' / 같은 날: 반투명 X → 석상 느낌, 모습 다양하게)
// - 방마다 시간이 멈춘 순간 돌로 굳은 승객 석상(10종, 받침대 위)이 서 있음. 단단한 돌이라 고양이 · 탄을 막음(엄폐물처럼, 부서지지는 않음).
// - 다음 웨이브 예고 동안(RunManager.SpawnWave) 몇 개에 빛나는 금이 위에서부터 번지고 → 떨리며 돌가루 → 깨져 조각이 흩날리고 그 자리에서 차장 · 짐꾼 · 신호수가 나옴.
// 모델: 블렌더 + Codex(Art/tools/blender_kit_ch2_passengers.py) — 미리 쪼갠 몸 · 받침 조각 Piece_XX · 금 선 Crack_XX · 돌 조각 틀 Shard_X
//   → 빌더(EnemyModelBuilder, Kit_Passenger_*)가 툰 돌 재질(statue · statue_dark · statue_base) + 금 선은 빛나는 흰색으로 Resources/Kit/Ch2/Passenger_*.prefab.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class FrozenPassenger : MonoBehaviour
    {
        public static readonly string[] Poses =
        {
            "Passenger_Luggage", "Passenger_Hat", "Passenger_Child", "Passenger_Watch", "Passenger_Newspaper",
            "Passenger_Wave", "Passenger_OldLady", "Passenger_MotherChild", "Passenger_Sitter", "Passenger_Runner",
        };
        public static readonly List<FrozenPassenger> All = new();

        static readonly Color DustCol = new(0.78f, 0.76f, 0.72f);
        static readonly Color GlowCol = new(0.85f, 0.95f, 1f);

        readonly List<Renderer> body = new();
        readonly List<Renderer> cracks = new();
        readonly List<Renderer> shards = new();
        Transform visual;
        Vector3 home;
        bool waking;

        public bool Free => !waking && isActiveAndEnabled;

        void Awake()
        {
            visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            home = visual.localPosition;
            Bounds b = default;
            bool first = true;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Crack")) { cracks.Add(r); r.enabled = false; continue; }
                if (r.name.StartsWith("Shard")) { shards.Add(r); r.enabled = false; continue; }
                body.Add(r);
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            // 금은 머리 → 발 순으로 번지게 (위에서부터 갈라짐)
            cracks.Sort((x, y) => y.bounds.center.y.CompareTo(x.bounds.center.y));
            // 돌이라 막힘: 팔 · 소품은 빼고 몸통 굵기만 (받침대 발자국의 60%)
            if (!first && !GetComponent<Collider>())
            {
                var c = gameObject.AddComponent<BoxCollider>();
                var lc = transform.InverseTransformPoint(b.center);
                var ls = transform.lossyScale;
                c.center = new Vector3(lc.x, b.size.y / 2 / ls.y, lc.z);
                c.size = new Vector3(Mathf.Clamp(b.size.x * 0.6f, 0.6f, 1.4f) / ls.x, b.size.y / ls.y, Mathf.Clamp(b.size.z * 0.6f, 0.6f, 1.4f) / ls.z);
            }
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>지금 방 안의 깨울 수 있는 석상 (고양이에게서 minDist 이상 떨어진 것, 섞어서)</summary>
        public static List<FrozenPassenger> Pick(RoomBuilder.RoomInfo room, Vector3 player, float minDist)
        {
            var list = new List<FrozenPassenger>();
            if (room == null) return list;
            foreach (var p in All)
                if (p && p.Free && room.Contains(p.transform.position) && Effects.Flat(p.transform.position - player).magnitude >= minDist) list.Add(p);
            for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
            return list;
        }

        /// <summary>duration 동안 금이 번지고 떨리다 끝에 깨짐. 하수인은 RunManager가 같은 자리에 만듦</summary>
        public void Awaken(float duration)
        {
            if (waking) return;
            waking = true;
            StartCoroutine(AwakenRoutine(duration));
        }

        IEnumerator AwakenRoutine(float duration)
        {
            // 1) 빛나는 금이 위에서부터 하나씩 (앞 75% 동안), 나올 때 톡 튀어나오는 크기 + 작은 섬광
            var crackScale = new List<Vector3>();
            foreach (var c in cracks) crackScale.Add(c.transform.localScale);
            float crackSpan = duration * 0.75f;
            int shown = 0, puffs = 0;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                while (shown < cracks.Count && t >= crackSpan * shown / Mathf.Max(1, cracks.Count))
                {
                    var c = cracks[shown];
                    c.enabled = true;
                    if (shown % 2 == 0) FxBurst.Spawn(c.bounds.center, 0.5f, FxBurst.Kind.Spark, GlowCol, 0.18f);
                    shown++;
                }
                for (int i = 0; i < shown; i++)
                {
                    float age = t - crackSpan * i / Mathf.Max(1, cracks.Count);
                    float pop = 1f + 0.6f * Mathf.Clamp01(1f - age / 0.12f);
                    cracks[i].transform.localScale = crackScale[i] * pop;
                }
                // 2) 뒤 40%는 떨림 + 받침 둘레 돌가루
                float shake = Mathf.Clamp01((k - 0.6f) / 0.4f);
                visual.localPosition = home + new Vector3(Mathf.Sin(t * 73f), 0, Mathf.Sin(t * 59f + 1.3f)) * (0.05f * shake);
                if (shake > puffs / 3f && puffs < 3)
                {
                    puffs++;
                    FxBurst.Spawn(transform.position + Effects.Flat(Random.onUnitSphere).normalized * 0.5f + Vector3.up * 0.15f, 1f, FxBurst.Kind.Dust, DustCol, 0.4f);
                }
                yield return null;
            }
            visual.localPosition = home;
            Shatter();
        }

        // ---------- 깨짐: 몸 조각 · 돌 조각이 사방으로 (문 조각 GateDebris를 같이 씀) ----------
        void Shatter()
        {
            var center = transform.position + Vector3.up * 1f;
            float groundY = transform.position.y;
            var parent = transform.parent;
            foreach (var r in body)
            {
                if (!r || !r.enabled) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var rt = r.transform;
                var out_ = Effects.Flat(r.bounds.center - transform.position);
                out_ = out_.sqrMagnitude > 0.001f ? out_.normalized : Effects.Flat(Random.onUnitSphere).normalized;
                float up = Mathf.Clamp01((r.bounds.center.y - groundY) / 2f);     // 받침 조각은 낮게, 머리는 높이
                var v = out_ * Random.Range(2.5f, 5f) + Random.insideUnitSphere * 1.2f + Vector3.up * Random.Range(1.5f, 4f) * (0.4f + up);
                Debris(parent, mf.sharedMesh, r.sharedMaterials, rt.position, rt.rotation, rt.lossyScale, v, groundY, Random.Range(1.2f, 1.8f));
            }
            int n = shards.Count > 0 ? Random.Range(12, 17) : 0;
            for (int i = 0; i < n; i++)
            {
                var s = shards[Random.Range(0, shards.Count)];
                var mf = s.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var at = center + Random.insideUnitSphere * 0.5f;
                var v = Effects.Flat(Random.onUnitSphere).normalized * Random.Range(3f, 7f) + Vector3.up * Random.Range(2.5f, 6f);
                Debris(parent, mf.sharedMesh, s.sharedMaterials, at, Random.rotation, s.transform.lossyScale * Random.Range(0.8f, 1.6f), v, groundY, 0.7f);
            }
            FxBurst.Spawn(center, 2.4f, FxBurst.Kind.Spark, GlowCol);
            FxBurst.Spawn(center, 2.6f, FxBurst.Kind.Dust, DustCol, 0.6f);
            FxBurst.Spawn(transform.position + Vector3.up * 0.15f, 2.8f, FxBurst.Kind.Dust, DustCol, 0.65f);
            CameraFollow.Shake(0.06f);
            Destroy(gameObject);
        }

        static void Debris(Transform parent, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 vel, float groundY, float life)
        {
            var go = new GameObject("StatueDebris");
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot);
            var ls = go.transform.lossyScale;
            go.transform.localScale = new Vector3(scale.x / ls.x, scale.y / ls.y, scale.z / ls.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            var d = go.AddComponent<GateDebris>();
            d.vel = vel;
            d.spin = Random.onUnitSphere * Random.Range(250f, 700f);
            d.groundY = groundY;
            d.life = life;
            d.rend = mr;
        }
    }
}
