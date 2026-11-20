// 3장 방 기믹: 떠다니는 거울 조각 (레벨 디자인 md '챕터 3' · '수치 기준 (챕터 3)', 2026-11-18)
// - 폭 2.5m 거울판이 0.8m/초로 표류하며 초당 10° 회전, 방 안에서 튕김
// - 고양이 탄 · 적 탄 모두 한 번 튕겨 냄, 몸은 통과 (콜라이더 없음 → 길막 아님. 판정은 이 스크립트가 선분 교차로 직접 함)
// - 쏘면 조각이 15° 돌아감 → 각도를 노려 적을 맞히거나, 적 탄이 뒤에서 날아옴
// 겉모습: Resources/Kit/Ch3/MirrorShard 프리팹이 있으면 그걸(블렌더 + Codex), 없으면 임시 판.
// 판의 앞면 = transform.forward (반사면 법선), 폭 방향 = transform.right. 양면 모두 반사.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class MirrorShard : MonoBehaviour
    {
        public static readonly List<MirrorShard> All = new();
        public const float Width = 2.5f, Height = 2.4f, Drift = 0.8f, Spin = 10f, ShotTurn = 15f, PerRoom = 3;

        Vector3 vel;
        bool anchored;                               // 3장 보스 제단 둘레: 제자리 고정, 저절로 안 돎 (쏘면 15°만)
        float spinSign = 1f, pendingTurn, flash;
        Rect area;                                   // 떠다니는 범위 (월드 XZ)
        RoomBuilder.RoomInfo room;
        Renderer[] glass;
        MaterialPropertyBlock mpb;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>제자리에 고정된 거울 조각 하나 (거울 고래 제단 둘레 8개)</summary>
        public static MirrorShard SpawnAnchored(ChapterDef ch, RoomBuilder.RoomInfo info, Vector3 worldPos, float yaw)
        {
            var area = new Rect(worldPos.x - 0.1f, worldPos.z - 0.1f, 0.2f, 0.2f);
            var m = Make(ch, info, worldPos, yaw, area, new System.Random(1));
            m.anchored = true;
            m.vel = Vector3.zero;
            return m;
        }

        /// <summary>방 하나에 거울 조각 3개 (보스 · 중간 보스 방 제외는 부르는 쪽이 판단)</summary>
        public static void SpawnSet(ChapterDef ch, RoomBuilder.RoomInfo info, System.Random rnd, int count = (int)PerRoom)   // 5장은 방마다 2개
        {
            float hw = info.size.x / 2 - 3f, hd = info.size.y / 2 - 3f;
            var area = new Rect(info.center.x - hw, info.center.z - hd, hw * 2, hd * 2);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = info.center;
                for (int tries = 0; tries < 20; tries++)
                {
                    p = info.center + new Vector3((float)(rnd.NextDouble() * 2 - 1) * hw * 0.8f, 0, (float)(rnd.NextDouble() * 2 - 1) * hd * 0.8f);
                    if (Mathf.Abs(p.x - info.center.x) < 3f && Mathf.Abs(p.z - info.center.z) < 3f) continue;   // 방 가운데(시작 자리 근처) 비움
                    if (!info.Blocked(p, 1.5f)) break;
                }
                Make(ch, info, p, (float)rnd.NextDouble() * 360f, area, rnd);
            }
        }

        static MirrorShard Make(ChapterDef ch, RoomBuilder.RoomInfo info, Vector3 worldPos, float yaw, Rect area, System.Random rnd)
        {
            GameObject go;
            var prefab = Kit.Prefab(ch, "MirrorShard");
            if (prefab)
            {
                go = Instantiate(prefab, info.root, false);
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                // 빌더가 아랫변을 바닥(y 0)에 붙여 놓으므로 임시 판과 같은 높이(아랫변 0.6m)로 띄움
                if (go.transform.childCount > 0)
                {
                    var b = Kit.LocalBounds(go);
                    go.transform.GetChild(0).localPosition += Vector3.up * (0.6f - b.min.y);
                }
            }
            else
            {
                // 임시: 은빛 테두리 + 하늘빛 거울면 (블렌더 키트가 들어오기 전까지)
                go = new GameObject("MirrorShard");
                go.transform.SetParent(info.root, false);
                RoomBuilder.Prim(PrimitiveType.Cube, "Glass", go.transform, new Vector3(0, Height / 2 + 0.6f, 0), new Vector3(Width, Height, 0.06f), RoomBuilder.Mat(new Color(0.78f, 0.92f, 1f), 1), false);
                var frame = RoomBuilder.Mat(new Color(0.95f, 0.85f, 0.55f), 4);
                RoomBuilder.Prim(PrimitiveType.Cube, "FrameTop", go.transform, new Vector3(0, Height + 0.62f, 0), new Vector3(Width + 0.1f, 0.08f, 0.1f), frame, false);
                RoomBuilder.Prim(PrimitiveType.Cube, "FrameBot", go.transform, new Vector3(0, 0.58f, 0), new Vector3(Width + 0.1f, 0.08f, 0.1f), frame, false);
            }
            go.name = "MirrorShard";
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var m = go.AddComponent<MirrorShard>();
            m.area = area;
            m.room = info;
            float a = (float)rnd.NextDouble() * Mathf.PI * 2;
            m.vel = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Drift;
            m.spinSign = rnd.NextDouble() < 0.5 ? -1f : 1f;
            m.glass = go.GetComponentsInChildren<Renderer>();
            m.mpb = new MaterialPropertyBlock();
            go.AddComponent<MirrorBob>();
            return m;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var p = transform.position + vel * dt;
            if (p.x < area.xMin || p.x > area.xMax) { vel.x = -vel.x; p.x = Mathf.Clamp(p.x, area.xMin, area.xMax); }
            if (p.z < area.yMin || p.z > area.yMax) { vel.z = -vel.z; p.z = Mathf.Clamp(p.z, area.yMin, area.yMax); }
            // 막힌 덩어리(지형지물)에 들어가면 되돌아 나옴
            if (room != null && room.Blocked(p, 0.8f)) { vel = -vel; p = transform.position; }
            transform.position = p;
            float turn = anchored ? 0f : Spin * spinSign * dt;
            if (Mathf.Abs(pendingTurn) > 0.01f)
            {
                float t = Mathf.MoveTowards(0, pendingTurn, 120f * dt);           // 맞으면 15°를 빠르게 (0.125초)
                pendingTurn -= t;
                turn += t;
            }
            transform.Rotate(0, turn, 0, Space.World);
            if (flash > 0)
            {
                flash -= dt;
                float k = Mathf.Clamp01(flash / 0.25f);
                mpb.SetColor("_EmissionColor", Color.white * k * 1.5f);
                mpb.SetColor("_FlashColor", new Color(1, 1, 1, k));
                foreach (var r in glass) if (r) r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>판 법선 (수평)</summary>
        public Vector3 Normal => Effects.Flat(transform.forward).normalized;

        /// <summary>수평 선분 from + dir * t (0 &lt; t ≤ max)이 이 판을 지나는지. dir은 수평 단위 벡터</summary>
        public bool Intersect(Vector3 from, Vector3 dir, float max, out float t, out Vector3 point)
        {
            t = 0; point = default;
            var n = Normal;
            var d = Effects.Flat(dir);
            float denom = Vector3.Dot(d, n);
            if (Mathf.Abs(denom) < 1e-4f) return false;
            var c = transform.position;
            t = Vector3.Dot(Effects.Flat(c - from), n) / denom;
            if (t < 0.05f || t > max) return false;
            point = from + dir * t;
            float lateral = Vector3.Dot(Effects.Flat(point - c), Effects.Flat(transform.right).normalized);
            return Mathf.Abs(lateral) <= Width / 2;
        }

        /// <summary>가장 가까이 걸리는 거울 조각 (exclude 제외)</summary>
        public static MirrorShard Raycast(Vector3 from, Vector3 dir, float max, MirrorShard exclude, out float t, out Vector3 point)
        {
            MirrorShard best = null; t = max; point = default;
            foreach (var m in All)
            {
                if (!m || m == exclude || !m.isActiveAndEnabled) continue;
                if (m.Intersect(from, dir, t, out float tt, out var pp) && tt < t) { best = m; t = tt; point = pp; }
            }
            return best;
        }

        /// <summary>판에 튕긴 방향 (수평 유지)</summary>
        public Vector3 Reflect(Vector3 dir)
        {
            var r = Vector3.Reflect(Effects.Flat(dir), Normal);
            return r.sqrMagnitude > 1e-6f ? r.normalized : -dir;
        }

        /// <summary>t초 동안 빛남 (거울 너머 검객이 튀어나오기 전 예고)</summary>
        public void Glow(float t) => flash = Mathf.Max(flash, t);

        /// <summary>탄이 튕긴 순간: 반짝임 + 불똥. byPlayer = 고양이 탄이면 15° 돌아감</summary>
        public void OnBounce(Vector3 at, Color col, bool byPlayer)
        {
            flash = 0.25f;
            if (byPlayer) pendingTurn += ShotTurn * spinSign;
            Enemy.Fx3("MirrorFlash", at, 0.8f);                                  // 거울 반사 섬광 (블렌더 구운 플립북)
        }
    }

    /// <summary>거울 조각이 공중에서 천천히 오르내림 (자식만 움직여 판정 위치는 그대로)</summary>
    public class MirrorBob : MonoBehaviour
    {
        float phase;
        Transform[] kids;
        Vector3[] baseY;

        void Start()
        {
            phase = Random.value * 10f;
            kids = new Transform[transform.childCount];
            baseY = new Vector3[kids.Length];
            for (int i = 0; i < kids.Length; i++) { kids[i] = transform.GetChild(i); baseY[i] = kids[i].localPosition; }
        }

        void Update()
        {
            float y = Mathf.Sin(Time.time * 1.3f + phase) * 0.15f;
            for (int i = 0; i < kids.Length; i++) if (kids[i]) kids[i].localPosition = baseY[i] + Vector3.up * y;
        }
    }
}
