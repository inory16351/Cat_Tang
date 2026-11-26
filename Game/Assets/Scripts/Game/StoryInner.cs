// 내면 세계 맵 '멈춘 1초' (2026-11-26 사용자 확정 · 대사 확정, HANDOFF.md "2026-11-26 (4)")
//   사용자: 죽음을 받아들이는 주인공의 감정이 잘 안 전해짐 → 내면 세계를 맵으로 만들어 받아들이는 과정을 보여 준 뒤 마을 작별 퀘스트.
//   맵 컨셉 = 아무것도 없는 하얀 방 (죽음 직전의 하얀 림보). 꿈4가 11:59:59에서 멈추면 마을 대신 여기서 눈뜸 (Ch5Ending.Run 맨 앞).
//   ① 부정: 쏘면 방이 처음으로 되감김 · 쏘지 않고 걸어가야 나아감 (처음 한 번은 반사적으로 쏨 → 거울 속 고양이 대화)
//   ② 집착: 고양이의 그림자가 예전 대사를 외치며 공격 · 쏠수록 커지고, 쏘지 않고 피하며 버티면 작아져 사라짐
//   ③ 기억: 바닥에 떨어진 기억 넷(시계탑 바늘 · 승강장 등불 · 공방 회중시계 · 빨간 숄)에 다가가면 고양이 한마디
//   ④ 받아들임: 북쪽 끝 거울 속 고양이와 최후의 대화 → 하얗게 → 마을에서 깨어남
// 맵 = Resources/Story/StoryInner.prefab (블렌더 키트 Art/tools/blender_kit_story_inner.py, Editor/StoryInnerBuilder.cs)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryInner : MonoBehaviour
    {
        public const string Key = "inner_done";
        /// <summary>꿈4를 보고 마을로 넘어올 때 하얀 화면을 걷지 않고 여기로 (StoryRun이 켬, VillageManager가 깨어남 연출을 건너뜀)</summary>
        public static bool EnterWhite, FromDream;
        public static bool Pending => Ch5Ending.Pending && !Meta.StorySeen(Key);

        const string Cat = "고양이", MC = Ch3Quest.MirrorCat;
        static readonly Vector3 Origin = new(2600, 0, 2600);
        static readonly Color White = new(0.97f, 0.97f, 0.98f), Gold = new(1f, 0.85f, 0.4f), Ink = new(0.12f, 0.1f, 0.16f);
        const float DenialEndZ = -8f, ArenaR = 7f, HoldTime = 20f, HoldMax = 26f, HitPenalty = 1f, OrbSpeed = 5.5f, OrbHitR = 0.75f;

        // 그림자가 외치는 예전 대사 (스토리 맵 5 · 엔딩에서 고양이가 했던 말)
        static readonly string[] ShadowBarks = { "아무도 못 데려가.", "거기까지 가게 두지 않아.", "난 아직 쏠 수 있어." };
        // 기억 넷 (빌더 Marks/Mem_*)
        static readonly (string mark, string line)[] Memories =
        {
            ("Mem_Hand", Cat + "#의문::...저 바늘, 내가 처음 고친 거였지."),
            ("Mem_Lantern", Cat + "#의문::...배웅은, 늘 내 몫이었어."),
            ("Mem_Watch", Cat + "#의문::...멈춘 시계도, 다 고쳐 줬는데."),
            ("Mem_Shawl", Cat + "#의문::...오래 기다리게 했네."),
        };

        public static IEnumerator Run(GameUI ui)
        {
            var go = new GameObject("StoryInner");
            var si = go.AddComponent<StoryInner>();
            si.ui = ui;
            si.StartCoroutine(si.Play());
            while (si && !si.done) yield return null;
        }

        GameUI ui;
        PlayerController p;
        Transform map, mirror, mirrorFig;
        bool done, fired, finished;
        float firedAt = -9f;
        Vector3 villagePos; Quaternion villageRot;
        Camera cam; CameraClearFlags oldFlags; Color oldBg, oldAmbient, oldFogColor; bool oldFog; FogMode oldFogMode; float oldFogStart, oldFogEnd;
        Light sun; Color sunColor; float sunIntensity; Quaternion sunRot;
        bool oldCombat;
        readonly List<(UnityEngine.Rendering.Volume v, float w)> volumes = new();
        AudioSource tick;
        readonly List<GameObject> temp = new();

        Transform F(string name)
        {
            if (!map) return null;
            foreach (var t in map.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        Vector3 At(string name, Vector3 local) { var t = F(name); return t ? t.position : Origin + local; }

        IEnumerator Play()
        {
            p = PlayerController.Instance;
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            if (ov && !FromDream) yield return ov.FadeWhite(0, 1, 1.4f);        // 꿈에서 바로 온 게 아니면 (이어하기 등) 하얗게 덮으며 들어감
            FromDream = false;
            if (ov) { ov.SetBlack(0); ov.SetWhite(1); }
            villagePos = p.transform.position; villageRot = p.model ? p.model.rotation : Quaternion.identity;

            var prefab = Resources.Load<GameObject>("Story/StoryInner");
            if (!prefab) Debug.LogWarning("[StoryInner] Story/StoryInner 프리팹 없음 — 빈 하얀 바닥으로");
            map = prefab ? Instantiate(prefab, Origin, Quaternion.identity).transform : BareFloor();
            WhiteOn();
            oldCombat = p.combatEnabled;
            p.combatEnabled = true;                                             // 쏠 수 있어야 '쏘지 않음'이 선택이 됨
            p.health.invulnerable = true;
            p.OnFired += OnFired;
            GuideArrow.Clear();
            MiniMapView.Suppressed = true;
            GameUI.RoomOverride = "";
            var spawn = At("Spawn", new Vector3(0, 0, -32));
            p.Teleport(spawn + Vector3.up * 0.3f);
            if (p.model) p.model.rotation = Quaternion.identity;
            PlayerController.FallBack = spawn + Vector3.up * 0.3f;
            CameraFollow.SnapZoom(1f);
            mirror = F("Mirror");
            mirrorFig = MakeMirrorCat();
            Music.Duck(1f, 1.5f);                                               // 하얀 방 = 거의 무음, 째깍 소리만 멀리
            if (Sfx.Has("e4_tick_1")) tick = Sfx.Loop("e4_tick_1", 0.12f);
            if (ov) yield return ov.FadeWhite(1, 0, 2.6f);
            yield return new WaitForSeconds(0.6f);
            p.InputLocked = false;

            yield return Denial(spawn);
            yield return Clinging();
            yield return Remember();
            yield return Accept();

            // 하얗게 → 마을에서 깨어남
            Snd("e5b_bell", 0.5f);
            if (ov) yield return ov.FadeWhite(0, 1, 2.4f);
            Meta.MarkStory(Key);
            Cleanup();
            p.Teleport(villagePos);
            if (p.model) p.model.rotation = villageRot;
            CameraFollow.SnapZoom(1f);
            VillageWake.Play(p);                                                // 누운 채 연기 속에서 깨어남
            Music.Duck(0f, 1f);
            if (ov) yield return ov.FadeWhite(1, 0, 1.6f);
            while (FindFirstObjectByType<VillageWake>()) yield return null;
            done = true;
            Destroy(gameObject);
        }

        // ================= ① 부정: 쏘면 되감김 =================
        IEnumerator Denial(Vector3 spawn)
        {
            var end = At("DenialEnd", new Vector3(0, 0, DenialEndZ));
            var endMark = new GameObject("DenialEndMark"); endMark.transform.position = end; endMark.transform.SetParent(map, true); temp.Add(endMark);
            GuideArrow.Show(endMark.transform, "앞으로", keep: true);
            var ghosts = new List<Ghost>();
            float[] rise = { -26f, -20f, -14f };
            bool first = true;
            while (true)
            {
                int next = 0;
                fired = false;
                bool rewound = false;
                while (p.transform.position.z < end.z)
                {
                    float z = p.transform.position.z - Origin.z;
                    if (next < rise.Length && z > rise[next] - 4f)
                    {
                        ghosts.AddRange(RiseGhosts(Origin + new Vector3(0, 0, rise[next] + 3.5f), first && next == 0 ? 1 : 2 + next));
                        next++;
                        if (first)
                        {
                            // 처음 한 번은 반사적으로 쏨 (사용자 확정 흐름: 부정 = 지금까지처럼 쏴서 멈추려 함)
                            yield return new WaitForSeconds(0.5f);
                            p.InputLocked = true;
                            var g = ghosts.Count > 0 ? ghosts[0] : null;
                            if (g != null && g.go && p.model) p.model.rotation = Quaternion.LookRotation(Effects.Flat(g.go.transform.position - p.transform.position).normalized);
                            yield return new WaitForSeconds(0.35f);
                            Sfx.Play("shot", 0.9f, 0f);
                            if (p.model) FxBurst.Spawn(p.transform.position + Vector3.up * 1.2f + p.model.forward * 0.8f, 1.1f, FxBurst.Kind.Muzzle, Gold);
                            yield return new WaitForSeconds(0.25f);
                            fired = true;
                        }
                    }
                    StepGhosts(ghosts);
                    if (fired) { rewound = true; break; }
                    yield return null;
                }
                if (!rewound) break;
                yield return Rewind(spawn, ghosts);
                if (first)
                {
                    first = false;
                    p.InputLocked = true;
                    if (mirror) CameraFollow.FocusOn(mirror.position, 0.9f);
                    yield return Talk(MC + "::또 되감았네.", Cat + "#의문::...끝나지 않으면, 지지도 않아.", MC + "::이기지도 못하고.");
                    CameraFollow.FocusOn(null, 0f);
                    ui?.Toast("쏘면 처음으로 되감겨요 — 쏘지 말고 걸어가세요");
                }
                else ui?.Toast("되감겼어요 — 쏘지 말고 걸어가세요");
                p.InputLocked = false;
            }
            GuideArrow.Clear();
            foreach (var g in ghosts) if (g.go) StartCoroutine(Fade(g, 0.8f));
            ghosts.Clear();
            Snd("e4_tick_2", 0.5f);
            FxBurst.Spawn(end + Vector3.up * 0.4f, 2.2f, FxBurst.Kind.Gold, Color.white);
        }

        IEnumerator Rewind(Vector3 spawn, List<Ghost> ghosts)
        {
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            Snd("e5_rift_shatter", 0.35f);
            if (ov) yield return ov.FadeWhite(0, 1, 0.35f);
            foreach (var g in ghosts) if (g.go) Destroy(g.go);
            ghosts.Clear();
            p.Teleport(spawn + Vector3.up * 0.3f);
            if (p.model) p.model.rotation = Quaternion.identity;
            CameraFollow.SnapZoom(1f);
            fired = false;
            yield return new WaitForSeconds(0.3f);
            if (ov) yield return ov.FadeWhite(1, 0, 0.7f);
        }

        class Ghost { public GameObject go; public float speed; public float born; }

        /// <summary>예전에 쓰러뜨린 적들의 하얀 잔상 — 다가오지만 닿으면 그냥 흩어짐 (해치지 않음)</summary>
        List<Ghost> RiseGhosts(Vector3 center, int n)
        {
            string[] pool = { "Ch1_Outlaw", "Ch1_Brute", "Ch2_Porter", "Ch3_Knight", "Ch4_Dancer", "Ch4_Executioner" };
            var list = new List<Ghost>();
            for (int i = 0; i < n; i++)
            {
                var pf = Resources.Load<GameObject>("Enemies/" + pool[Random.Range(0, pool.Length)]);
                if (!pf) continue;
                var at = center + new Vector3(n == 1 ? 0 : Mathf.Lerp(-4f, 4f, i / (float)(n - 1)), 0, Random.Range(-1f, 1.5f));
                var go = Instantiate(pf, at, Quaternion.Euler(0, 180f, 0), map);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
                Silhouette(go, new Color(0.55f, 0.55f, 0.62f, 0.55f));
                FxBurst.Spawn(at + Vector3.up * 0.6f, 1.2f, FxBurst.Kind.Dust, new Color(0.8f, 0.8f, 0.86f));
                temp.Add(go);
                list.Add(new Ghost { go = go, speed = Random.Range(1.2f, 1.8f), born = Time.time });
            }
            Snd("e_glass", 0.25f);
            return list;
        }

        void StepGhosts(List<Ghost> ghosts)
        {
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i];
                if (!g.go) { ghosts.RemoveAt(i); continue; }
                var d = Effects.Flat(p.transform.position - g.go.transform.position);
                if (d.magnitude < 1.1f) { StartCoroutine(Fade(g, 0.5f)); ghosts.RemoveAt(i); continue; }   // 닿으면 흩어짐
                g.go.transform.position += d.normalized * g.speed * Time.deltaTime;
                g.go.transform.rotation = Quaternion.Slerp(g.go.transform.rotation, Quaternion.LookRotation(d.normalized), 4f * Time.deltaTime);
            }
        }

        IEnumerator Fade(Ghost g, float dur)
        {
            if (!g.go) yield break;
            var t0 = g.go.transform.localScale;
            FxBurst.Spawn(g.go.transform.position + Vector3.up, 1.3f, FxBurst.Kind.Dust, Color.white);
            for (float t = 0; t < 1f && g.go; t += Time.deltaTime / dur) { g.go.transform.localScale = t0 * (1f - t); yield return null; }
            if (g.go) Destroy(g.go);
        }

        // ================= ② 집착: 그림자 고양이 =================
        IEnumerator Clinging()
        {
            var center = At("ArenaCenter", new Vector3(0, 0, 3));
            ui?.Toast("앞으로");
            while (Effects.Flat(p.transform.position - center).magnitude > ArenaR - 1.5f) yield return null;
            p.InputLocked = true;
            var shadow = MakeShadowCat(center + new Vector3(0, 0, 3.5f));
            CameraFollow.FocusOn(Vector3.Lerp(p.transform.position, shadow.position, 0.5f), 0.85f);
            Snd("e5_rift_shatter", 0.4f);
            float baseScale = shadow.localScale.y;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f) { shadow.localScale = Vector3.one * baseScale * Mathf.Lerp(0.2f, 1.3f, Mathf.SmoothStep(0, 1, t)); yield return null; }
            var bark = BigLabel.Show(shadow, new Vector3(0, 4.2f, 0), ShadowBarks[2], new Color(0.86f, 0.8f, 0.98f), 0.8f, BigLabel.Frame.Plain);   // 옅은 보라 글자 + 검은 테 (하얀 바닥에서 읽힘)
            yield return new WaitForSeconds(1.6f);
            CameraFollow.FocusOn(null, 0f);
            ui?.Toast("쏘지 말고 피하며 버티세요 — 쏠수록 그림자가 커져요");
            p.InputLocked = false;

            var holdLb = BigLabel.Show(p.transform, new Vector3(0, 3.1f, 0), "", Gold, 0.9f, BigLabel.Frame.Timer);
            float hold = HoldTime, size = 1.3f, nextAttack = Time.time + 1.2f, nextBark = Time.time + 5f;
            int barkI = 0, pattern = 0;
            var orbs = new List<GameObject>();
            fired = false;
            while (hold > 0f)
            {
                float dt = Time.deltaTime;
                bool shooting = Time.time - firedAt < 1.2f;
                if (fired)
                {
                    fired = false;
                    size = Mathf.Min(2.4f, size + 0.18f);
                    hold = Mathf.Min(HoldMax, hold + 1.5f);
                    if (bark) bark.text = ShadowBarks[barkI++ % ShadowBarks.Length];
                    nextBark = Time.time + 3f;
                    FxBurst.Spawn(shadow.position + Vector3.up * 1.5f, 1.6f, FxBurst.Kind.Spark, Ink);
                    CameraFollow.Shake(0.08f);
                }
                if (!shooting) { hold -= dt; size = Mathf.MoveTowards(size, Mathf.Lerp(0.25f, 1.3f, hold / HoldTime), 0.25f * dt); }
                shadow.localScale = Vector3.Lerp(shadow.localScale, Vector3.one * baseScale * size, 5f * dt);
                var look = Effects.Flat(p.transform.position - shadow.position);
                if (look.sqrMagnitude > 0.01f) shadow.rotation = Quaternion.Slerp(shadow.rotation, Quaternion.LookRotation(look.normalized), 6f * dt);
                if (Time.time >= nextBark && bark) { bark.text = ShadowBarks[barkI++ % ShadowBarks.Length]; nextBark = Time.time + 5f; }
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + Mathf.Lerp(1.3f, 2.1f, 1f - size / 2.4f);
                    var from = shadow.position + Vector3.up * 1.1f;
                    if (pattern++ % 2 == 0)
                    {
                        int n = Mathf.RoundToInt(Mathf.Lerp(8, 14, size / 2.4f));
                        float off = Random.Range(0f, 360f);
                        for (int i = 0; i < n; i++) orbs.Add(Orb(from, Quaternion.Euler(0, off + i * 360f / n, 0) * Vector3.forward));
                    }
                    else
                    {
                        var aim = look.sqrMagnitude > 0.01f ? look.normalized : Vector3.back;
                        for (int i = -1; i <= 1; i++) orbs.Add(Orb(from, Quaternion.Euler(0, i * 14f, 0) * aim));
                    }
                    Snd("e_glass", 0.2f);
                }
                for (int i = orbs.Count - 1; i >= 0; i--)
                {
                    var o = orbs[i];
                    if (!o) { orbs.RemoveAt(i); continue; }
                    var dir = o.transform.forward;
                    o.transform.position += dir * OrbSpeed * dt;
                    var d = Effects.Flat(p.transform.position - o.transform.position).magnitude;
                    if (d < OrbHitR && !p.health.dashing)
                    {
                        hold = Mathf.Min(HoldMax, hold + HitPenalty);
                        FxBurst.Spawn(o.transform.position, 1f, FxBurst.Kind.Hit, Ink);
                        CameraFollow.Shake(0.12f);
                        Snd("e_glass", 0.4f);
                        Destroy(o); orbs.RemoveAt(i);
                    }
                    else if (Effects.Flat(o.transform.position - center).magnitude > 18f) { Destroy(o); orbs.RemoveAt(i); }
                }
                if (holdLb) holdLb.text = $"버티기 {Mathf.CeilToInt(Mathf.Max(0, hold))}";
                yield return null;
            }
            foreach (var o in orbs) if (o) Destroy(o);
            if (holdLb) Destroy(holdLb.gameObject);
            if (bark) Destroy(bark.gameObject);
            // 그림자가 작아져 사라짐
            p.InputLocked = true;
            CameraFollow.FocusOn(shadow.position, 0.85f);
            var s0 = shadow.localScale;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f)
            {
                shadow.localScale = s0 * (1f - t);
                if (Random.value < 0.35f) FxBurst.Spawn(shadow.position + Vector3.up * Random.Range(0.2f, 1.6f), 0.7f, FxBurst.Kind.Dust, new Color(0.3f, 0.28f, 0.36f));
                yield return null;
            }
            Destroy(shadow.gameObject);
            FxBurst.Spawn(center + Vector3.up * 0.4f, 2.4f, FxBurst.Kind.Gold, Color.white);
            Snd("e4_tick_2", 0.5f);
            yield return new WaitForSeconds(0.8f);
            CameraFollow.FocusOn(null, 0f);
            p.InputLocked = false;
        }

        GameObject Orb(Vector3 from, Vector3 dir)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // 그림자 덩어리 탄 (작고 단순한 것이라 도형으로)
            Destroy(o.GetComponent<Collider>());
            o.name = "ShadowOrb";
            o.transform.SetParent(map, true);
            o.transform.position = from;
            o.transform.rotation = Quaternion.LookRotation(dir);
            o.transform.localScale = Vector3.one * 0.55f;
            Silhouette(o, new Color(Ink.r, Ink.g, Ink.b, 1f));
            temp.Add(o);
            return o;
        }

        Transform MakeShadowCat(Vector3 at)
        {
            var root = new GameObject("ShadowCat").transform;
            root.SetParent(map, true);
            root.position = at;
            if (p.model)
            {
                var fig = Instantiate(p.model.gameObject, root);
                foreach (var mb in fig.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
                foreach (var c in fig.GetComponentsInChildren<Collider>(true)) Destroy(c);
                fig.transform.localPosition = Vector3.zero;
                fig.transform.localRotation = Quaternion.identity;
                Silhouette(fig, new Color(Ink.r, Ink.g, Ink.b, 1f));
            }
            temp.Add(root.gameObject);
            return root;
        }

        // ================= ③ 기억 넷 =================
        IEnumerator Remember()
        {
            var marks = new List<(Transform t, string line)>();
            foreach (var (mark, line) in Memories) { var t = F(mark); if (t) { t.gameObject.SetActive(true); marks.Add((t, line)); } }
            foreach (var (t, _) in marks) FxBurst.Spawn(t.position + Vector3.up * 0.5f, 1.6f, FxBurst.Kind.Gold, Gold);
            Snd("e4_bell", 0.35f);
            ui?.Toast("떨어진 기억에 다가가세요");
            int left = marks.Count;
            var seen = new bool[marks.Count];
            while (left > 0)
            {
                for (int i = 0; i < marks.Count; i++)
                {
                    if (seen[i]) continue;
                    var t = marks[i].t;
                    if (Effects.Flat(p.transform.position - t.position).magnitude > 2.2f) continue;
                    seen[i] = true; left--;
                    p.InputLocked = true;
                    CameraFollow.FocusOn(t.position, 0.75f);
                    Snd("e4_tick_2", 0.4f);
                    var item = t.Find("Item");
                    var y0 = item ? item.localPosition : Vector3.zero;
                    for (float k = 0; k < 1f && item; k += Time.deltaTime / 0.8f) { item.localPosition = y0 + Vector3.up * Mathf.SmoothStep(0, 0.9f, k); yield return null; }
                    yield return Talk(marks[i].line);
                    // 빛이 되어 거울 쪽으로
                    FxBurst.Spawn(t.position + Vector3.up, 1.8f, FxBurst.Kind.Gold, Gold);
                    if (mirror) FxBurst.Spawn(mirror.position + Vector3.up * 1.8f, 1.2f, FxBurst.Kind.Converge, Gold);
                    t.gameObject.SetActive(false);
                    CameraFollow.FocusOn(null, 0f);
                    p.InputLocked = false;
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }

        // ================= ④ 거울 속 고양이와 최후의 대화 =================
        IEnumerator Accept()
        {
            var stand = At("MirrorStand", new Vector3(0, 0, 27.5f));
            var mark = new GameObject("MirrorStandMark"); mark.transform.position = stand; mark.transform.SetParent(map, true); temp.Add(mark);
            GuideArrow.Show(mark.transform, "거울로", keep: true);
            if (tick) { Sfx.StopLoop(tick, 2f); tick = null; }
            while (Effects.Flat(p.transform.position - stand).magnitude > 2.2f) yield return null;
            GuideArrow.Clear();
            p.InputLocked = true;
            if (p.model && mirror) p.model.rotation = Quaternion.LookRotation(Effects.Flat(mirror.position - p.transform.position).normalized);
            CameraFollow.FocusOn(Vector3.Lerp(stand, mirror ? mirror.position : stand, 0.55f), 0.7f);
            yield return new WaitForSeconds(0.8f);
            yield return Talk(Cat + "#의문::...무서워.", MC + "::응.",
                              Cat + "#의문::끝나면... 아무것도 안 남을까 봐.", MC + "::남아. 추억은 사라지지 않아.",
                              Cat + "#의문::...그동안 계속 노력했어. 멈추게 하려고.", MC + "::알아. 그게 너였으니까.",
                              Cat + "#결의::...인사는 하고 가야겠지.", MC + "::다들 기다리고 있어.");
            if (mirror) FxBurst.Spawn(mirror.position + Vector3.up * 1.8f, 3f, FxBurst.Kind.Gold, Color.white);
            yield return new WaitForSeconds(0.6f);
        }

        Transform MakeMirrorCat()
        {
            if (!mirror || !p.model) return null;
            var pane = F("MirrorPane");
            var at = pane ? pane.position : mirror.position + new Vector3(0, 0, 0.35f);
            var fig = Instantiate(p.model.gameObject, map);
            fig.name = "MirrorCatFigure";
            foreach (var mb in fig.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            foreach (var c in fig.GetComponentsInChildren<Collider>(true)) Destroy(c);
            at.y = Origin.y;
            fig.transform.position = at + Vector3.back * 0.3f;             // 거울 면 바로 앞에 비친 듯 (남쪽을 봄, MirrorGhost로 반투명)
            fig.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var s = fig.transform.localScale; fig.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
            MirrorGhost.Apply(fig, rings: false);
            return fig.transform;
        }

        // ================= 하얀 방 빛 · 정리 =================
        void WhiteOn()
        {
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = White; }
            oldAmbient = RenderSettings.ambientLight;
            oldFog = RenderSettings.fog; oldFogColor = RenderSettings.fogColor; oldFogMode = RenderSettings.fogMode; oldFogStart = RenderSettings.fogStartDistance; oldFogEnd = RenderSettings.fogEndDistance;
            RenderSettings.ambientLight = new Color(0.95f, 0.95f, 0.97f);
            // 마을 새벽 보정(DawnVolume: 푸른 화이트밸런스 · 비네트)을 끔 — 하얀 방은 하얗게
            foreach (var v in FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)) { volumes.Add((v, v.weight)); v.weight = 0f; }
            RenderSettings.fog = true; RenderSettings.fogColor = White; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 34f; RenderSettings.fogEndDistance = 70f;   // 하얀 바닥 끝이 안개로 사라짐
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sunRot = sun.transform.rotation; sun.color = new Color(1f, 0.98f, 0.95f); sun.intensity = Mathf.Max(1.1f, sunIntensity); sun.transform.rotation = Quaternion.Euler(62f, -150f, 0f); }
        }

        void WhiteOff()
        {
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RenderSettings.ambientLight = oldAmbient;
            foreach (var (v, w) in volumes) if (v) v.weight = w;
            volumes.Clear();
            RenderSettings.fog = oldFog; RenderSettings.fogColor = oldFogColor; RenderSettings.fogMode = oldFogMode; RenderSettings.fogStartDistance = oldFogStart; RenderSettings.fogEndDistance = oldFogEnd;
            if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRot; }
        }

        void Cleanup()
        {
            if (finished) return;
            finished = true;
            if (p) { p.OnFired -= OnFired; p.combatEnabled = oldCombat; p.health.invulnerable = false; p.InputLocked = false; }
            foreach (var g in temp) if (g) Destroy(g);
            temp.Clear();
            if (tick) { Sfx.StopLoop(tick, 0.3f); tick = null; }
            if (map) Destroy(map.gameObject);
            WhiteOff();
            GuideArrow.Clear();
            CameraFollow.FocusOn(null, 0f);
            PlayerController.FallBack = null;
            MiniMapView.Suppressed = false;
            GameUI.RoomOverride = null;
        }

        void OnDestroy() { Cleanup(); }

        void OnFired(int slot, bool awake) { fired = true; firedAt = Time.time; }

        Transform BareFloor()
        {
            var root = new GameObject("StoryInner_Bare").transform;
            root.position = Origin;
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.transform.SetParent(root, false);
            go.transform.localScale = new Vector3(4.4f, 1, 8.4f);
            Silhouette(go, White);
            return root;
        }

        static void Silhouette(GameObject go, Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.color = c;
            if (c.a < 0.99f && m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        IEnumerator Talk(params string[] lines)
        {
            bool d = false;
            string speaker = lines.Length > 0 && lines[0].Contains("::") ? lines[0].Substring(0, lines[0].IndexOf("::")).Split('#')[0] : Cat;
            ui.Talk(speaker, lines, () => d = true);
            while (!d) yield return null;
        }

        static void Snd(string key, float vol) { if (Sfx.Has(key)) Sfx.Play(key, vol, 0f); }

        /// <summary>개발용: 마을 씬에서 바로 내면 맵만 (끝나면 마을로)</summary>
        public static void DevPreview()
        {
            var ui = FindFirstObjectByType<GameUI>();
            var go = new GameObject("StoryInnerDev").AddComponent<DevRunner>();
            go.StartCoroutine(Run(ui));
        }
        class DevRunner : MonoBehaviour { }
    }
}
