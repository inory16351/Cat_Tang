// 방을 도형으로 만듦 (모델링 전 임시): 바닥 · 벽 · 챕터 테마 소품 · 보상 문 · 상인
// 테마: Western(상자 · 통 · 선인장) · Ruins(기운 기둥 · 잔해) · Mirror(거울 판 · 수정) · Clockwork(톱니 · 뜬 상자) · Prison(시계판 · 사슬 · 수정)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class RoomBuilder
    {
        static readonly Dictionary<string, Material> mats = new();

        public static Material Mat(Color c, float outline = 4f)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + outline;
            if (mats.TryGetValue(key, out var m) && m) return m;
            var sh = Shader.Find("CatTang/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(sh);
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            mats[key] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static Transform Build(ChapterDef ch, Vector2 size, bool boss, int seed)
        {
            var root = new GameObject(boss ? "Room_Boss" : "Room").transform;
            float hw = size.x / 2, hd = size.y / 2;
            var floor = Mat(boss ? Color.Lerp(ch.Floor, new Color(0.3f, 0.25f, 0.45f), 0.45f) : ch.Floor, 0);
            var wall = Mat(ch.Wall, 4);
            Prim(PrimitiveType.Cube, "Floor", root, new Vector3(0, -0.1f, 0), new Vector3(size.x, 0.2f, size.y), floor);
            Prim(PrimitiveType.Cube, "Wall_W", root, new Vector3(-hw, 1.25f, 0), new Vector3(0.6f, 2.5f, size.y), wall);
            Prim(PrimitiveType.Cube, "Wall_E", root, new Vector3(hw, 1.25f, 0), new Vector3(0.6f, 2.5f, size.y), wall);
            Prim(PrimitiveType.Cube, "Wall_N", root, new Vector3(0, 1.25f, hd), new Vector3(size.x, 2.5f, 0.6f), wall);
            Prim(PrimitiveType.Cube, "Wall_S", root, new Vector3(0, 1.25f, -hd), new Vector3(size.x, 2.5f, 0.6f), wall);
            // 바닥 무늬 (시계판 원)
            var ring = Mat(Color.Lerp(ch.Floor, Color.white, 0.18f), 0);
            Prim(PrimitiveType.Cylinder, "FloorClock", root, new Vector3(0, 0.005f, 0), new Vector3(size.y * 0.55f, 0.01f, size.y * 0.55f), ring, false);
            Prim(PrimitiveType.Cylinder, "FloorClockIn", root, new Vector3(0, 0.012f, 0), new Vector3(size.y * 0.5f, 0.01f, size.y * 0.5f), floor, false);

            var rnd = new System.Random(seed);
            int count = boss ? 8 : 9;
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rnd.NextDouble() * 2 - 1) * (hw - 2.5f);
                float z = (float)(rnd.NextDouble() * 2 - 1) * (hd - 3.5f);
                if (Mathf.Abs(x) < 3.5f) x = Mathf.Sign(x == 0 ? 1 : x) * (3.5f + (float)rnd.NextDouble() * 2);
                if (z < -hd + 5) z = -hd + 5;                       // 시작 자리 비움
                Prop(ch.Prop, root, new Vector3(x, 0, z), rnd);
            }
            return root;
        }

        static void Prop(string theme, Transform root, Vector3 p, System.Random rnd)
        {
            float yaw = rnd.Next(0, 360);
            switch (theme)
            {
                case "Ruins":
                    if (rnd.NextDouble() < 0.6) Prim(PrimitiveType.Cylinder, "Pillar", root, p + Vector3.up * 1.1f, new Vector3(0.9f, 1.1f, 0.9f), Mat(new Color(0.62f, 0.52f, 0.46f)), true, new Vector3(rnd.Next(-14, 14), yaw, rnd.Next(-14, 14)));
                    else Prim(PrimitiveType.Cube, "Rubble", root, p + Vector3.up * 0.35f, new Vector3(1.4f, 0.7f, 1.0f), Mat(new Color(0.5f, 0.42f, 0.38f)), true, new Vector3(0, yaw, 8));
                    break;
                case "Mirror":
                    if (rnd.NextDouble() < 0.55) Prim(PrimitiveType.Cube, "Mirror", root, p + Vector3.up * 1.2f, new Vector3(1.6f, 2.4f, 0.15f), Mat(new Color(0.7f, 0.85f, 0.98f), 5), true, new Vector3(0, yaw, 0));
                    else Prim(PrimitiveType.Cube, "Crystal", root, p + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f), Mat(new Color(0.62f, 0.72f, 0.95f)), true, new Vector3(12, yaw, 18));
                    break;
                case "Clockwork":
                    if (rnd.NextDouble() < 0.6) Prim(PrimitiveType.Cylinder, "Gear", root, p + Vector3.up * 0.25f, new Vector3(1.8f, 0.25f, 1.8f), Mat(new Color(0.85f, 0.66f, 0.3f)), true);
                    else Prim(PrimitiveType.Cube, "Block", root, p + Vector3.up * 0.6f, new Vector3(1.1f, 1.2f, 1.1f), Mat(new Color(0.55f, 0.5f, 0.75f)), true, new Vector3(0, yaw, 0));
                    break;
                case "Prison":
                    if (rnd.NextDouble() < 0.5) Prim(PrimitiveType.Cylinder, "ClockSlab", root, p + Vector3.up * 1.1f, new Vector3(1.8f, 0.08f, 1.8f), Mat(new Color(0.9f, 0.86f, 0.74f), 5), true, new Vector3(80, yaw, 0));
                    else Prim(PrimitiveType.Cube, "Crystal", root, p + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f), Mat(new Color(0.62f, 0.45f, 0.9f)), true, new Vector3(12, yaw, 18));
                    break;
                default:   // Western
                    double r = rnd.NextDouble();
                    if (r < 0.35) Prim(PrimitiveType.Cube, "Crate", root, p + Vector3.up * 0.5f, Vector3.one, Mat(new Color(0.7f, 0.5f, 0.3f)), true, new Vector3(0, yaw, 0));
                    else if (r < 0.65) Prim(PrimitiveType.Cylinder, "Barrel", root, p + Vector3.up * 0.6f, new Vector3(0.8f, 0.6f, 0.8f), Mat(new Color(0.55f, 0.3f, 0.22f)), true);
                    else
                    {
                        var c = Prim(PrimitiveType.Capsule, "Cactus", root, p + Vector3.up * 1.0f, new Vector3(0.6f, 1.0f, 0.6f), Mat(new Color(0.45f, 0.62f, 0.38f)), true);
                        Prim(PrimitiveType.Capsule, "Arm", c.transform, new Vector3(0.8f, 0.2f, 0), new Vector3(0.6f, 0.4f, 0.6f), Mat(new Color(0.45f, 0.62f, 0.38f)), false);
                    }
                    break;
            }
        }

        // ---------- 스테이지: 방 4~5개가 복도로 이어진 덩어리 ----------
        public class RoomInfo
        {
            public Transform root;
            public Vector3 center;
            public Vector2 size;
            public readonly List<GameObject> gates = new();
            public Vector3 farSide = Vector3.forward;       // 다음 방(또는 출구 문)이 놓일 방향
            public bool entered, cleared;
            public bool midBoss;                            // 술집 홀 (중간 보스): 스테이지 2 마지막 방 뒤에 붙어 있음
            public readonly List<Rect> blocks = new();       // 방 모양으로 깎인 막힌 덩어리 (월드 XZ, Rect.x = 최소 x · y = 최소 z)
            public bool Blocked(Vector3 p, float pad = 0f)
            {
                foreach (var b in blocks) if (p.x > b.xMin - pad && p.x < b.xMax + pad && p.z > b.yMin - pad && p.z < b.yMax + pad) return true;
                return false;
            }
            public GameObject entranceGate, backGate;       // 술집 입구 · 뒷문 (평소 닫힘)
            public GameObject entryGate;                     // 술집 뒷문에서 이어 지은 첫 방의 남쪽 문: 지나온 술집을 지우므로 클리어해도 안 열림
            public bool Contains(Vector3 p, float inset = 1.6f)
            {
                var d = p - center;
                return Mathf.Abs(d.x) < size.x / 2 - inset && Mathf.Abs(d.z) < size.y / 2 - inset;
            }
        }

        public class StageLayout
        {
            public Transform root;
            public readonly List<RoomInfo> rooms = new();
        }

        static readonly Vector2Int[] Dirs = { new(0, 1), new(1, 0), new(-1, 0) };   // 북 · 동 · 서 (남쪽으로는 안 감)

        /// <param name="origin">스테이지를 지을 월드 위치 (첫 방 가운데)</param>
        /// <param name="entrySouth">첫 방 남쪽 벽을 열어 둠 (술집 뒷문에서 걸어 들어옴)</param>
        /// <param name="saloonAfter">마지막 방 뒤에 술집 홀(중간 보스)을 붙여 지음</param>
        public static StageLayout BuildStage(ChapterDef ch, int roomCount, int seed, bool boss, Vector2? sizeOverride = null,
            Vector3 origin = default, bool entrySouth = false, bool saloonAfter = false)
        {
            var layout = new StageLayout { root = new GameObject(boss ? "Stage_Boss" : "Stage").transform };
            layout.root.position = origin;
            var rnd = new System.Random(seed);
            var cells = new List<Vector2Int> { Vector2Int.zero };
            if (!boss)
                while (cells.Count < roomCount)
                {
                    var last = cells[cells.Count - 1];
                    var options = new List<Vector2Int>();
                    foreach (var d in Dirs) if (!cells.Contains(last + d)) options.Add(last + d);
                    if (options.Count == 0) break;
                    // 북쪽을 조금 더 자주
                    var pick = options[rnd.NextDouble() < 0.45 && options.Contains(last + Dirs[0]) ? options.IndexOf(last + Dirs[0]) : rnd.Next(options.Count)];
                    cells.Add(pick);
                }
            var cellSize = CellSize;
            var roomSize = sizeOverride ?? (boss ? new Vector2(34, 30) : RoomSize);
            var floor = Mat(boss ? Color.Lerp(ch.Floor, new Color(0.3f, 0.25f, 0.45f), 0.45f) : ch.Floor, 0);
            var wall = Mat(ch.Wall, 4);
            var gateM = Mat(new Color(0.85f, 0.62f, 0.25f), 4);
            var ring = Mat(Color.Lerp(ch.Floor, Color.white, 0.18f), 0);
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var local = new Vector3(c.x * cellSize.x, 0, c.y * cellSize.y);
                var info = new RoomInfo { center = origin + local, size = roomSize };
                info.root = new GameObject(boss ? "Room_Boss" : $"Room_{i + 1}").transform;
                info.root.SetParent(layout.root, false);
                info.root.localPosition = local;
                var prev = i > 0 ? cells[i - 1] - c : (entrySouth ? new Vector2Int(0, -1) : (Vector2Int?)null);
                var next = i < cells.Count - 1 ? cells[i + 1] - c : (Vector2Int?)null;
                if (saloonAfter && i == cells.Count - 1)
                {
                    saloonDir = Vector2Int.zero;
                    foreach (var d in Dirs) if (!cells.Contains(c + d) && !cells.Contains(c + d + Vector2Int.up) && (prev == null || prev.Value != d)) { saloonDir = d; break; }   // 술집 뒷문 너머 칸(스테이지 3 첫 방)도 비어 있어야
                    if (saloonDir == Vector2Int.zero)
                        foreach (var d in Dirs) if (!cells.Contains(c + d) && (prev == null || prev.Value != d)) { saloonDir = d; break; }
                    if (saloonDir != Vector2Int.zero) next = saloonDir;
                }
                info.farSide = next.HasValue ? new Vector3(next.Value.x, 0, next.Value.y) : (prev.HasValue ? new Vector3(-prev.Value.x, 0, -prev.Value.y) : Vector3.forward);
                float hw = roomSize.x / 2, hd = roomSize.y / 2;
                var floorGo = Prim(PrimitiveType.Cube, "Floor", info.root, new Vector3(0, -0.1f, 0), new Vector3(roomSize.x, 0.2f, roomSize.y), floor);
                if (!Kit.TileFloor(ch, info.root, roomSize, floorGo, seed + i))
                {
                    Prim(PrimitiveType.Cylinder, "FloorClock", info.root, new Vector3(0, 0.005f, 0), new Vector3(roomSize.y * 0.55f, 0.01f, roomSize.y * 0.55f), ring, false);
                    Prim(PrimitiveType.Cylinder, "FloorClockIn", info.root, new Vector3(0, 0.012f, 0), new Vector3(roomSize.y * 0.5f, 0.01f, roomSize.y * 0.5f), floor, false);
                }
                foreach (var side in new[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) })
                {
                    bool open = (prev.HasValue && prev.Value == side) || (next.HasValue && next.Value == side);
                    bool horizontal = side.y != 0;
                    float len = horizontal ? roomSize.x : roomSize.y;
                    var pos = new Vector3(side.x * hw, 1.25f, side.y * hd);
                    var inward = new Vector3(-side.x, 0, -side.y);
                    if (!open)
                    {
                        Kit.TileWall(ch, Prim(PrimitiveType.Cube, "Wall", info.root, pos, horizontal ? new Vector3(len, 2.5f, 0.6f) : new Vector3(0.6f, 2.5f, len), wall), inward);
                        continue;
                    }
                    float seg = len / 2 - 2.3f;
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var off = horizontal ? new Vector3(k * (2.3f + seg / 2), 0, 0) : new Vector3(0, 0, k * (2.3f + seg / 2));
                        Kit.TileWall(ch, Prim(PrimitiveType.Cube, "Wall", info.root, pos + off, horizontal ? new Vector3(seg, 2.5f, 0.6f) : new Vector3(0.6f, 2.5f, seg), wall), inward);
                    }
                    var gate = WoodGate.Make(ch, info.root, "Gate", new Vector3(pos.x, 0, pos.z), inward, gateM);   // 나무 문 (클리어하면 부서짐)
                    gate.SetActive(false);
                    info.gates.Add(gate);
                    if (i == 0 && entrySouth && prev.HasValue && prev.Value == side) info.entryGate = gate;
                    // 복도 (다음 방 쪽만)
                    if (next.HasValue && next.Value == side)
                    {
                        float gapLen = (horizontal ? cellSize.y - roomSize.y : cellSize.x - roomSize.x);
                        var mid = new Vector3(side.x * (hw + gapLen / 2), 0, side.y * (hd + gapLen / 2));
                        var cor = new GameObject("Corridor").transform;
                        cor.SetParent(info.root, false);
                        Prim(PrimitiveType.Cube, "Floor", cor, mid + Vector3.down * 0.1f, horizontal ? new Vector3(4.6f, 0.2f, gapLen + 0.4f) : new Vector3(gapLen + 0.4f, 0.2f, 4.6f), floor);
                        for (int k = -1; k <= 1; k += 2)
                        {
                            var o = horizontal ? new Vector3(k * 2.6f, 1.25f, 0) : new Vector3(0, 1.25f, k * 2.6f);
                            // 복도 벽도 건물 · 절벽으로 덮음 (맨 상자 X)
                            var cw = Prim(PrimitiveType.Cube, "Wall", cor, mid + o, horizontal ? new Vector3(0.6f, 2.5f, gapLen + 0.4f) : new Vector3(gapLen + 0.4f, 2.5f, 0.6f), wall);
                            Kit.TileWall(ch, cw, horizontal ? new Vector3(-k, 0, 0) : new Vector3(0, 0, -k));
                        }
                    }
                }
                if (!boss) AddShape(ch, info, rnd);
                if (KitProps(ch, info, hw, hd, rnd, boss)) { layout.rooms.Add(info); continue; }
                int props = boss ? 8 : 7;
                for (int k = 0; k < props; k++)
                {
                    float x = (float)(rnd.NextDouble() * 2 - 1) * (hw - 2.5f);
                    float z = (float)(rnd.NextDouble() * 2 - 1) * (hd - 2.5f);
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;                // 가운데 비움
                    if (Mathf.Abs(x) < 3f || Mathf.Abs(z) < 3f) continue;                    // 문 앞 통로 비움
                    if (info.Blocked(info.center + new Vector3(x, 0, z), 1f)) continue;
                    Prop(ch.Prop, info.root, new Vector3(x, 0, z), rnd);
                }
                layout.rooms.Add(info);
            }
            if (saloonAfter && cells.Count > 0 && saloonDir != Vector2Int.zero)
            {
                var lastCell = cells[cells.Count - 1];
                BuildSaloon(ch, layout, origin + new Vector3((lastCell.x + saloonDir.x) * cellSize.x, 0, (lastCell.y + saloonDir.y) * cellSize.y),
                    new Vector3(-saloonDir.x, 0, -saloonDir.y), cellSize, seed);
            }
            return layout;
        }

        static Vector2Int saloonDir;

        // ---------- 방 모양: 큰 지형지물 (2026-11-17 사용자) ----------
        // "완전 사각형 말고" → 처음엔 방 모서리를 덩어리로 깎았으나 "네모난 갈색 상자" · "이상함" →
        // "그 크기의, 그 장소에 있을 법한 오브젝트로 자연스럽게 배치". 블렌더 + Codex 지형지물(Art/tools/blender_kit_landmarks.py)을
        // 방마다 1~3개: 모서리 쪽(벽과 0.3~2.2m 틈 → 뒤로 돌아가는 좁은 길)이나 안쪽, 방향은 90° 단위 ± 15°.
        // 문 앞 통로(가운데 십자 ±2.6)와 방 가운데(반지름 4)는 비움, 서로 2m 이상 떨어짐. 발자국(축 정렬 사각형)은 RoomInfo.blocks →
        // 적 생성 · 이동 · 지도가 피함
        static readonly (string name, float w, float d)[] BadlandsLM =
            { ("LM_RockSpire", 5.8f, 4.18f), ("LM_BoulderPile", 5.0f, 3.78f), ("LM_WagonWreck", 5.4f, 4.35f), ("LM_MineCart", 5.2f, 2.49f), ("LM_DeadTree", 4.2f, 3.88f) };
        static readonly (string name, float w, float d)[] TownLM =
            { ("LM_WaterTower", 4.0f, 3.64f), ("LM_Stagecoach", 5.2f, 3.22f), ("LM_Well", 3.6f, 3.35f), ("LM_HayCart", 5.4f, 3.03f), ("LM_MarketStall", 5.2f, 3.06f), ("LM_LumberPile", 5.2f, 2.78f) };

        static void AddShape(ChapterDef ch, RoomInfo info, System.Random rnd)
        {
            var rm = RunManager.Instance;
            var pool = new List<(string name, float w, float d)>();
            if (Ch2Kit.Is(ch))
            {
                // 2장: 장소별 지형지물 (크기는 프리팹 경계로 잼). 객차 방은 객차를 나란히
                Ch2Kit.Coaches(ch, info, rnd);
                foreach (var n in Ch2Kit.Landmarks())
                    if (Kit.Has(ch, n)) { var lb = Kit.LocalBounds(Kit.Prefab(ch, n)); pool.Add((n, lb.size.x, lb.size.z)); }
            }
            else
            foreach (var lm in rm && rm.stageIndex == 0 ? BadlandsLM : TownLM) if (Kit.Has(ch, lm.name)) pool.Add(lm);
            if (pool.Count == 0) return;
            float hw = info.size.x / 2, hd = info.size.y / 2;
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            int S() => rnd.Next(2) == 0 ? -1 : 1;
            double roll = rnd.NextDouble();
            int want = roll < 0.12 ? 1 : roll < 0.6 ? 2 : 3;
            var local = new List<Rect>();
            for (int tries = 0; tries < 40 && local.Count < want && pool.Count > 0; tries++)
            {
                var lm = pool[rnd.Next(pool.Count)];
                float yaw = rnd.Next(4) * 90f + R(-15f, 15f);
                float c = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad)), sn = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad));
                float W = lm.w * c + lm.d * sn, D = lm.w * sn + lm.d * c;          // 돌린 뒤 축 정렬 발자국
                if (W > hw - 3.2f || D > hd - 3.2f) continue;
                Vector2 pos;
                if (rnd.NextDouble() < 0.55)
                {
                    float gx = R(0.3f, 2.2f), gz = R(0.3f, 2.2f);                    // 모서리 쪽, 벽과 틈
                    pos = new Vector2(S() * (hw - 0.6f - gx - W / 2), S() * (hd - 0.6f - gz - D / 2));
                }
                else
                    pos = new Vector2(S() * R(2.8f + W / 2, hw - 2.5f - W / 2), S() * R(2.8f + D / 2, hd - 2.5f - D / 2));
                var r = new Rect(pos.x - W / 2, pos.y - D / 2, W, D);
                // 문 앞 십자 통로 · 방 가운데 비움
                if (r.xMin < 2.6f && r.xMax > -2.6f) continue;
                if (r.yMin < 2.6f && r.yMax > -2.6f) continue;
                if (Vector2.Distance(Vector2.zero, ClosestPoint(r, Vector2.zero)) < 4f) continue;
                bool hit = false;
                foreach (var o in local) if (r.xMin < o.xMax + 2f && r.xMax > o.xMin - 2f && r.yMin < o.yMax + 2f && r.yMax > o.yMin - 2f) hit = true;
                if (hit) continue;
                local.Add(r);
                pool.Remove(lm);                                                     // 한 방에 같은 것 두 번 X
                Kit.Place(ch, lm.name, info.root, new Vector3(pos.x, 0, pos.y), yaw);
                info.blocks.Add(new Rect(info.center.x + r.x, info.center.z + r.y, W, D));
            }
        }

        static Vector2 ClosestPoint(Rect r, Vector2 p) => new(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));

        // ---------- 술집 홀 (중간 보스, 2026-11-17 사용자: 건물로 걸어 들어가 이어지게) ----------
        // 스테이지 2 마지막 방에서 복도를 지나 술집 여닫이문으로 들어감 → 홀 안에서 싸움 → 이기면 북쪽 뒷문이 열리고
        // 그 너머에 스테이지 3이 이어 지어짐 (RunManager). 실내: 나무 마루 · 판자벽, 바깥 입구 쪽은 건물 정면
        // 방 크기 (2026-11-17 사용자: 모양을 깎는다면 줄이지 말고 키울 것): 24×20 → 28×24. 깎이는 덩어리를 빼도 예전보다 넓음
        public static readonly Vector2 RoomSize = new(28, 24);
        public static readonly Vector2 CellSize = new(38, 34);       // 방 칸 간격 = 방 + 복도 10m
        public static readonly Vector2 SaloonSize = RoomSize;        // 일반 방과 같아야 들어오는 복도 길이가 맞음
        public const float CellDepth = 34f;               // 방 칸 간격 (스테이지 3을 뒷문 너머에 붙일 때) = CellSize.y

        static void BuildSaloon(ChapterDef ch, StageLayout layout, Vector3 center, Vector3 entrance, Vector2 cellSize, int seed)
        {
            var rnd = new System.Random(seed + 77);
            var size = SaloonSize;
            var info = new RoomInfo { center = center, size = size, midBoss = true, farSide = Vector3.forward };
            bool hall2 = Ch2Kit.Is(ch);                                      // 2장: 대합실 (태엽 역장)
            info.root = new GameObject(hall2 ? "WaitingHall" : "Saloon").transform;
            info.root.SetParent(layout.root, false);
            info.root.position = center;
            float hw = size.x / 2, hd = size.y / 2;
            var wall = Mat(new Color(0.42f, 0.26f, 0.16f), 4);
            var gateM = Mat(new Color(0.55f, 0.33f, 0.19f), 4);
            var floorM = Mat(new Color(0.5f, 0.32f, 0.2f), 0);
            var floorGo = Prim(PrimitiveType.Cube, "Floor", info.root, new Vector3(0, -0.1f, 0), new Vector3(size.x, 0.2f, size.y), floorM);
            Kit.TileFloor(ch, info.root, size, floorGo, seed, hall2 ? "Ground" : "SaloonFloor", false);
            string innerWall = hall2 ? "Wall" : "PlankWall";
            var ent = new Vector2Int(Mathf.RoundToInt(entrance.x), Mathf.RoundToInt(entrance.z));
            var back = new Vector2Int(0, 1);
            foreach (var side in new[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) })
            {
                bool open = side == ent || side == back;
                bool horizontal = side.y != 0;
                float len = horizontal ? size.x : size.y;
                var pos = new Vector3(side.x * hw, 1.25f, side.y * hd);
                var inward = new Vector3(-side.x, 0, -side.y);
                if (!open)
                {
                    Kit.TileWall(ch, Prim(PrimitiveType.Cube, "Wall", info.root, pos, horizontal ? new Vector3(len, 2.5f, 0.6f) : new Vector3(0.6f, 2.5f, len), wall), inward, innerWall);
                    continue;
                }
                float seg = len / 2 - 2.3f;
                for (int k = -1; k <= 1; k += 2)
                {
                    var off = horizontal ? new Vector3(k * (2.3f + seg / 2), 0, 0) : new Vector3(0, 0, k * (2.3f + seg / 2));
                    var cube = Prim(PrimitiveType.Cube, "Wall", info.root, pos + off, horizontal ? new Vector3(seg, 2.5f, 0.6f) : new Vector3(0.6f, 2.5f, seg), wall);
                    Kit.TileWall(ch, cube, inward, innerWall);
                    if (side == ent) Kit.TileWall(ch, cube, -inward, "Wall", false);     // 바깥에서 보면 술집 건물 정면 (2장 대합실도 낮은 벽 — 높은 역 건물은 카메라를 가림)
                }
                // 문: 여닫이 술집 문(장식) + 막는 문짝(평소 닫힘 → RunManager가 열어 줌)
                float doorYaw = Quaternion.LookRotation(inward).eulerAngles.y;
                if (side == ent && !hall2) Kit.Place(ch, "SaloonDoor", info.root, pos - new Vector3(0, 1.25f, 0) - inward * 0.1f, doorYaw, 1f, false);
                var gate = WoodGate.Make(ch, info.root, side == ent ? "SaloonGate" : "BackGate", new Vector3(pos.x, 0, pos.z), inward, gateM);
                info.gates.Add(gate);
                if (side == ent) info.entranceGate = gate; else info.backGate = gate;
                // 복도: 입구 쪽은 스테이지 2 마지막 방이 이미 지음. 뒷문 쪽은 스테이지 3 첫 방(깊이 20)의 남쪽 벽까지
                if (side == back)
                {
                    float gapLen = CellDepth - hd - RoomSize.y / 2;
                    var mid = new Vector3(0, 0, hd + gapLen / 2);
                    var cor = new GameObject("BackCorridor").transform;
                    cor.SetParent(info.root, false);
                    cor.localPosition = Vector3.zero;
                    var cf = Prim(PrimitiveType.Cube, "Floor", cor, mid + Vector3.down * 0.1f, new Vector3(4.6f, 0.2f, gapLen + 0.4f), floorM);
                    var cg = new GameObject("CorridorGround").transform; cg.SetParent(cor, false); cg.localPosition = mid;
                    Kit.TileFloor(ch, cg, new Vector2(4.6f, gapLen + 0.4f), cf, seed + 5, "Ground", false);
                    for (int k = -1; k <= 1; k += 2)
                        Prim(PrimitiveType.Cube, "Wall", cor, mid + new Vector3(k * 2.6f, 1.25f, 0), new Vector3(0.6f, 2.5f, gapLen + 0.4f), wall);
                }
            }
            if (hall2) { Ch2Kit.WaitingHall(ch, info, hw, hd, rnd); layout.rooms.Add(info); return; }
            // 실내 소품: 뒷벽 양쪽 카운터(보스가 그 사이에서 나옴), 모서리 술통 더미, 화약통 · 상자
            Kit.Place(ch, "BarCounter", info.root, new Vector3(-7f, 0, hd - 2.2f), 180f);
            Kit.Place(ch, "BarCounter", info.root, new Vector3(7f, 0, hd - 2.2f), 180f);
            Kit.Place(ch, "BarrelStack", info.root, new Vector3(-hw + 1.6f, 0, hd - 1.8f), 30f);
            Kit.Place(ch, "BarrelStack", info.root, new Vector3(hw - 1.6f, 0, -hd + 1.8f), 200f);
            Kit.Place(ch, "Crates", info.root, new Vector3(hw - 2f, 0, hd - 2.4f), 15f);
            var kegs = new List<Vector3> { new(-6f, 0, -2f), new(6f, 0, -1.5f), new(-9f, 0, 3f), new(9f, 0, 2.5f) };
            for (int i = 0; i < 2; i++)
            {
                var kp = kegs[rnd.Next(kegs.Count)]; kegs.Remove(kp);
                var kg = Kit.Place(ch, "Keg", info.root, kp, rnd.Next(360)); if (kg) PowderKeg.Make(kg);
            }
            layout.rooms.Add(info);
        }

        /// <summary>
        /// 키트 소품 배치 (1장 기획서): 화약통 방마다 2~4개 · 부서지는 엄폐물(마차 · 상자 · 울타리) · 녹은 시계 장식(벽 쪽).
        /// 후보 자리 중 일부에만 놓음 (가운데 · 문 앞 통로는 비움). 키트가 없으면 false
        /// </summary>
        static bool KitProps(ChapterDef ch, RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            if (Ch2Kit.Is(ch)) { Ch2Kit.Props(ch, info, hw, hd, rnd, boss); return true; }
            if (!Kit.Has(ch, "Keg")) return false;
            var spots = new List<Vector3>();
            for (float x = -hw + 3f; x <= hw - 3f; x += 3f)
                for (float z = -hd + 3f; z <= hd - 3f; z += 3f)
                {
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;            // 가운데 비움
                    if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;            // 문 앞 통로 비움
                    if (info.Blocked(info.center + new Vector3(x, 0, z), 1.2f)) continue;   // 방 모양으로 깎인 곳
                    spots.Add(new Vector3(x, 0, z));
                }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            int si = 0;
            Vector3 Next() => si < spots.Count ? spots[si++] + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) : Vector3.zero;

            int kegs = boss ? 0 : rnd.Next(2, 5);
            for (int k = 0; k < kegs; k++)
            {
                var go = Kit.Place(ch, "Keg", root, Next(), rnd.Next(360));
                if (go) PowderKeg.Make(go);
            }
            int cover = boss ? 2 : rnd.Next(3, 6);
            string[] kinds = { "Wagon", "Crates", "Fence" };
            for (int k = 0; k < cover; k++)
            {
                string kind = kinds[rnd.Next(kinds.Length)];
                var go = Kit.Place(ch, kind, root, Next(), rnd.Next(4) * 90 + rnd.Next(-10, 10));
                if (go) Breakable.Make(go, kind == "Wagon" ? 90 : kind == "Crates" ? 45 : 30);
            }
            // 보스 광장: 네 모서리에 녹아내린 시계탑
            if (boss && Kit.Has(ch, "ClockTower"))
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        Kit.Place(ch, "ClockTower", root, new Vector3(sx * (hw - 2.2f), 0, sz * (hd - 2.2f)), Quaternion.LookRotation(new Vector3(-sx, 0, -sz)).eulerAngles.y);
            // 녹은 시계 장식: 벽 가까이 1~2개 (장식 · 부서지지 않음)
            int clocks = rnd.Next(1, 3);
            for (int k = 0; k < clocks; k++)
            {
                bool ns = rnd.NextDouble() < 0.5;
                float sgn = rnd.NextDouble() < 0.5 ? -1 : 1;
                var p = ns ? new Vector3((float)(rnd.NextDouble() * 2 - 1) * (hw - 4), 0, sgn * (hd - 1.4f)) : new Vector3(sgn * (hw - 1.4f), 0, (float)(rnd.NextDouble() * 2 - 1) * (hd - 4));
                var inward = ns ? new Vector3(0, 0, -sgn) : new Vector3(-sgn, 0, 0);
                Kit.Place(ch, "MeltClock", root, p, Quaternion.LookRotation(inward).eulerAngles.y);
            }
            return true;
        }

        /// <summary>보상 문: 나무 문 그림 + 위에 보상 아이콘 + 이름</summary>
        public static RoomDoor Door(Transform root, Vector3 pos, RewardKind kind)
        {
            var go = new GameObject("Door_" + kind);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var frame = Mat(new Color(0.85f, 0.62f, 0.25f), 4);
            Prim(PrimitiveType.Cube, "Post_L", go.transform, new Vector3(-1.3f, 1.4f, 0), new Vector3(0.35f, 2.8f, 0.35f), frame, false);
            Prim(PrimitiveType.Cube, "Post_R", go.transform, new Vector3(1.3f, 1.4f, 0), new Vector3(0.35f, 2.8f, 0.35f), frame, false);
            Prim(PrimitiveType.Cube, "Lintel", go.transform, new Vector3(0, 2.9f, 0), new Vector3(3.0f, 0.35f, 0.4f), frame, false);
            var wood = Mat(kind == RewardKind.Boss ? new Color(0.55f, 0.18f, 0.2f) : new Color(0.55f, 0.36f, 0.22f), 3);
            Prim(PrimitiveType.Cube, "Leaf_L", go.transform, new Vector3(-0.6f, 1.2f, 0.05f), new Vector3(1.1f, 1.5f, 0.12f), wood, false, new Vector3(0, -12, 0));
            Prim(PrimitiveType.Cube, "Leaf_R", go.transform, new Vector3(0.6f, 1.2f, 0.05f), new Vector3(1.1f, 1.5f, 0.12f), wood, false, new Vector3(0, 12, 0));
            Interactable.WorldIcon(go.transform, RoomDoor.IconOf(kind), new Vector3(0, 4.1f, 0), 1.3f);
            Interactable.WorldLabel(go.transform, RoomDoor.NameOf(kind), new Vector3(0, 3.25f, -0.4f), 0.06f, UIKit.Gold);
            var d = go.AddComponent<RoomDoor>();
            d.reward = kind;
            d.radius = 2.4f;
            return d;
        }

        /// <summary>상인: 큰 모자 쓴 도형 사람 + 수레</summary>
        public static GameObject MerchantNpc(Transform root, Vector3 pos)
        {
            var go = new GameObject("Merchant");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            // 1장: 한쪽 바닥에서 술집 카운터가 솟아오르며 상인 등장 (키트가 있을 때)
            var rm = RunManager.Instance;
            // 2장: 바닥에서 매표 창구(매표소 직원)가 솟아오름
            string counter = rm && Kit.Has(rm.Chapter, "TicketBooth") ? "TicketBooth" : "BarCounter";
            if (rm && Kit.Has(rm.Chapter, counter))
            {
                var dir = Effects.Flat(-pos).normalized;
                Kit.Place(rm.Chapter, counter, go.transform, Vector3.zero, Quaternion.LookRotation(dir.sqrMagnitude > 0 ? dir : Vector3.forward).eulerAngles.y);
                go.AddComponent<RiseUp>();
                Interactable.WorldIcon(go.transform, "UI/coin", new Vector3(0, 3.3f, 0), 0.8f);
                return go;
            }
            Prim(PrimitiveType.Capsule, "Body", go.transform, new Vector3(0, 0.9f, 0), new Vector3(0.9f, 0.9f, 0.9f), Mat(new Color(0.4f, 0.55f, 0.45f), 5), true);
            Prim(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0, 2.0f, 0), Vector3.one * 0.7f, Mat(new Color(0.9f, 0.78f, 0.62f), 5), false);
            Prim(PrimitiveType.Cylinder, "Hat", go.transform, new Vector3(0, 2.35f, 0), new Vector3(1.4f, 0.04f, 1.4f), Mat(new Color(0.35f, 0.25f, 0.18f), 5), false);
            Prim(PrimitiveType.Cylinder, "HatTop", go.transform, new Vector3(0, 2.55f, 0), new Vector3(0.6f, 0.2f, 0.6f), Mat(new Color(0.35f, 0.25f, 0.18f), 5), false);
            Prim(PrimitiveType.Cube, "Cart", go.transform, new Vector3(-1.4f, 0.5f, 0), new Vector3(1.4f, 0.8f, 1.0f), Mat(new Color(0.7f, 0.5f, 0.3f)), true);
            Interactable.WorldIcon(go.transform, "UI/coin", new Vector3(0, 3.3f, 0), 0.8f);
            return go;
        }
    }
}
