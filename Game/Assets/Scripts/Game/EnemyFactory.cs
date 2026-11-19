// 적을 도형으로 만듦 (모델링 전 임시). 챕터가 오를수록 체력 · 피해 배율 증가, 색이 챕터 테마를 따름.
// 보스 = 챕터마다 다른 패턴의 '시간의 존재' (큰 원기둥 몸통 + 시계판 + 바늘)
using UnityEngine;

namespace CatTang
{
    public static class EnemyFactory
    {
        static Material M(Color c, float o = 4f) => RoomBuilder.Mat(c, o);

        static GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null) =>
            RoomBuilder.Prim(t, t.ToString(), parent, pos, scale, m, false, euler);

        static Enemy Root(string name, Vector3 pos, float hp, Enemy.Kind kind, float radius, float height)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = height;
            col.center = new Vector3(0, height / 2, 0);
            var h = go.AddComponent<Health>();
            h.maxHp = hp;
            var e = go.AddComponent<Enemy>();
            e.kind = kind;
            if (kind != Enemy.Kind.Boss) EnemyHpBar.Attach(h, height + 0.45f);
            StatusFx.Attach(h, height);
            return e;
        }

        static Color Tint(Color baseC, ChapterDef ch) => Color.Lerp(baseC, ch.Wall, 0.25f);

        /// <summary>Tripo 모델 프리팹(Resources/Enemies/이름)이 있으면 "Model" 자식으로 붙임. 없으면 false → 도형으로 대신</summary>
        static bool AttachModel(Enemy e, string res)
        {
            var prefab = Resources.Load<GameObject>("Enemies/" + res);
            if (!prefab) return false;
            var m = Object.Instantiate(prefab, e.transform, false);
            m.name = "Model";
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            e.SetModel(m.transform);
            return true;
        }

        /// <summary>도형 몸통을 "Model" 아래에 만들어 흔들림 연출이 똑같이 붙게 함</summary>
        static Transform ProtoModel(Enemy e)
        {
            var m = new GameObject("Model").transform;
            m.SetParent(e.transform, false);
            e.SetModel(m);
            return m;
        }

        /// <summary>일반 적 웨이브 구성: 무법자 = 챕터 사격형 비율 + 10%, 폭탄병 25%, 나머지 깡패</summary>
        public static Enemy RandomMinion(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            if (ch != null && ch.Id >= 3) return RandomCh3(pos, ch, elite);
            if (ch != null && ch.Id == 2)
            {
                // 2장: 차장 40% · 짐꾼 30% · 신호수 30% (세 종류가 처음부터 섞여 나옴)
                float r2 = Random.value;
                if (r2 < 0.4f) return Conductor(pos, ch, elite);
                if (r2 < 0.7f) return Porter(pos, ch, elite);
                return Signal(pos, ch, elite);
            }
            float r = Random.value, outlaw = Mathf.Clamp01(ch.ShooterRatio + 0.1f);
            if (r < outlaw) return Outlaw(pos, ch, elite);
            if (r < outlaw + 0.25f) return Bomber(pos, ch, elite);
            return Brute(pos, ch, elite);
        }

        /// <summary>강화형 모델(Resources/Enemies/이름_Plus)이 있으면 그걸, 없으면 기본 모델</summary>
        static bool AttachModelFor(Enemy e, string res, bool elite, out bool plusModel)
        {
            plusModel = elite && AttachModel(e, res + "_Plus");
            return plusModel || AttachModel(e, res);
        }

        /// <summary>강화형: 체력 1.5배. 전용 모델이 없으면 기본 모델을 조금 크게 · 더 달군 색으로</summary>
        static void MakeElite(Enemy e, bool plusModel)
        {
            e.elite = true;
            e.name += "_Plus";
            e.GetComponent<Health>().maxHp *= 1.5f;
            var model = e.transform.Find("Model");
            // 강화형 모델은 기본보다 10~20% 큼 (EnemyModelBuilder 키 표) → 몸 판정도 같이
            var col = e.GetComponent<CapsuleCollider>();
            if (col) { col.radius *= 1.15f; col.height *= 1.15f; col.center = new Vector3(0, col.height / 2, 0); }
            if (plusModel || !model) return;
            model.localScale *= 1.12f;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                // 3장 강화형: 더 밝고 차가운 빛 (md: 더 투명하고 일렁이는 모습), 1 · 2장: 더 달군 색
                var mul = e.chapter != null && e.chapter.Id >= 3 ? new Color(0.9f, 1.15f, 1.35f) : new Color(1.15f, 0.82f, 0.68f);
                foreach (var m in mats) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * mul);
                r.materials = mats;
            }
            e.SetModel(model);
        }

        // 태엽 무법자: 원거리 · 이속 2.6 · 2초마다 1발(예고 0.5초) · 피해 8 · 내구 24
        public static Enemy Outlaw(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Outlaw", pos, 24 * ch.HpMul, Enemy.Kind.Outlaw, 0.55f, 2.6f);
            if (!AttachModelFor(e, "Ch1_Outlaw", elite, out bool plus))
            {
                var m = ProtoModel(e);
                Part(PrimitiveType.Capsule, m, new Vector3(0, 0.75f, 0), new Vector3(0.6f, 0.6f, 0.5f), M(Tint(new Color(0.42f, 0.3f, 0.32f), ch)));
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 1.45f, 0.05f), new Vector3(0.5f, 0.06f, 0.5f), M(new Color(0.95f, 0.9f, 0.75f), 2f), new Vector3(90, 0, 0));
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 1.72f, 0), new Vector3(0.75f, 0.03f, 0.75f), M(new Color(0.3f, 0.2f, 0.18f)));
                Part(PrimitiveType.Cylinder, m, new Vector3(0.3f, 0.95f, 0.45f), new Vector3(0.12f, 0.25f, 0.12f), M(new Color(0.85f, 0.65f, 0.3f), 2f), new Vector3(90, 0, 0));
            }
            e.chapter = ch;                     // 장 정보 (폭탄병 모래 폭탄 모델 찾기 등 — 없으면 회색 기본 구를 던졌음, 2026-11-17)
            e.speed = 2.6f;
            e.projectileDamage = 8 * ch.DmgMul;
            e.shootInterval = 2f;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 시계추 깡패: 돌진 근접 · 이속 3.4 · 돌진 예고 0.6초 · 피해 12 · 내구 48
        public static Enemy Brute(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Brute", pos, 48 * ch.HpMul, Enemy.Kind.Brute, 0.95f, 2.7f);
            if (!AttachModelFor(e, "Ch1_Brute", elite, out bool plus))
            {
                var m = ProtoModel(e);
                Part(PrimitiveType.Cube, m, new Vector3(0, 0.95f, 0), new Vector3(1.3f, 1.4f, 0.9f), M(Tint(new Color(0.36f, 0.28f, 0.3f), ch)));
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 1.35f, 0.47f), new Vector3(0.8f, 0.05f, 0.8f), M(new Color(0.95f, 0.9f, 0.75f), 2f), new Vector3(90, 0, 0));
                Part(PrimitiveType.Sphere, m, new Vector3(0.8f, 0.45f, 0.6f), Vector3.one * 0.6f, M(new Color(0.95f, 0.65f, 0.25f), 2f));
            }
            e.chapter = ch;                     // 장 정보 (폭탄병 모래 폭탄 모델 찾기 등 — 없으면 회색 기본 구를 던졌음, 2026-11-17)
            e.speed = 3.4f;
            e.contactDamage = 12 * ch.DmgMul;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 모래시계 폭탄병: 자폭 · 이속 4.5 · 붙으면 0.8초 점멸 후 반경 2m 폭발 · 피해 12 · 내구 16
        public static Enemy Bomber(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Bomber", pos, 16 * ch.HpMul, Enemy.Kind.Bomber, 0.6f, 1.75f);
            if (!AttachModelFor(e, "Ch1_Bomber", elite, out bool plus))
            {
                var m = ProtoModel(e);
                Part(PrimitiveType.Capsule, m, new Vector3(0, 0.55f, 0), new Vector3(0.6f, 0.45f, 0.5f), M(Tint(new Color(0.4f, 0.3f, 0.28f), ch)));
                Part(PrimitiveType.Sphere, m, new Vector3(0, 1.1f, 0), new Vector3(0.45f, 0.55f, 0.45f), M(new Color(1f, 0.75f, 0.3f), 2f));
                Part(PrimitiveType.Sphere, m, new Vector3(0.4f, 0.7f, 0.25f), Vector3.one * 0.32f, M(new Color(0.15f, 0.12f, 0.12f)));
            }
            e.chapter = ch;                     // 장 정보 (폭탄병 모래 폭탄 모델 찾기 등 — 없으면 회색 기본 구를 던졌음, 2026-11-17)
            e.speed = 4.5f;
            e.contactDamage = 12 * ch.DmgMul;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // ---------- 2장 (멈춘 기차역) — 체력 · 피해는 레벨 디자인 md 2장 값 그대로 (챕터 배율 안 곱함). 강화형 1.5배는 MakeElite ----------
        // 몸 판정은 1장 같은 역할 적과 같은 비율 (모델 키: 차장 2.7 · 짐꾼 2.9 · 신호수 1.9, EnemyModelBuilder.Models2)

        /// <summary>도형 대신: 모델이 없을 때 쓰는 단순 몸통</summary>
        static void Proto2(Enemy e, ChapterDef ch, Color body, float h, float w)
        {
            var m = ProtoModel(e);
            Part(PrimitiveType.Capsule, m, new Vector3(0, h * 0.4f, 0), new Vector3(w, h * 0.4f, w * 0.85f), M(body));
            Part(PrimitiveType.Cylinder, m, new Vector3(0, h * 0.86f, 0), new Vector3(w * 0.9f, 0.05f, w * 0.9f), M(new Color(0.2f, 0.18f, 0.25f)));
        }

        // 태엽 차장: 원거리 · 이속 2.8 · 7~10m 유지 · 승차권 3갈래 2.5초마다 · 발당 피해 10 · 내구 60
        public static Enemy Conductor(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Conductor", pos, 60, Enemy.Kind.Conductor, 0.55f, 2.6f);
            if (!AttachModelFor(e, "Ch2_Conductor", elite, out bool plus)) Proto2(e, ch, new Color(0.35f, 0.32f, 0.45f), 2.2f, 0.6f);
            e.chapter = ch;
            e.speed = 2.8f;
            e.projectileDamage = 10;
            e.shootInterval = Enemy.TicketInterval;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 트렁크 짐꾼: 방패 근접 · 이속 3.2 · 정면 120° 탄 막음 · 내리찍기 피해 15 · 내구 160
        public static Enemy Porter(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Porter", pos, 160, Enemy.Kind.Porter, 0.95f, 2.8f);
            if (!AttachModelFor(e, "Ch2_Porter", elite, out bool plus))
            {
                Proto2(e, ch, new Color(0.32f, 0.3f, 0.4f), 2.4f, 1.1f);
                Part(PrimitiveType.Cube, e.transform.Find("Model"), new Vector3(0, 1.1f, 0.75f), new Vector3(1.2f, 0.9f, 0.35f), M(new Color(0.55f, 0.35f, 0.2f)));   // 트렁크 방패
            }
            e.chapter = ch;
            e.speed = 3.2f;
            e.contactDamage = 15;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().BlockFrom = e.PorterBlocks;
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 신호수: 설치형 · 이속 3.0 · 8m 유지 · 4초마다 신호등 2개 · 폭발 피해 12 · 내구 60
        public static Enemy Signal(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Root("Enemy_Signal", pos, 60, Enemy.Kind.Signal, 0.6f, 1.9f);
            if (!AttachModelFor(e, "Ch2_Signal", elite, out bool plus)) Proto2(e, ch, new Color(0.38f, 0.33f, 0.42f), 1.6f, 0.6f);
            e.chapter = ch;
            e.speed = 3f;
            e.projectileDamage = 12;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // ---------- 3장 (신기루 사막 = 거울 세계, 2026-11-18) — 체력 · 피해는 md 3장 값 그대로. 모델(Resources/Enemies/Ch3_*)이 없으면 도형 ----------
        // 적 구성: 다섯 종류가 처음부터 섞여 나오고, 방마다 3~4종을 뽑아 섞음
        static object ch3MixRoom;
        static readonly System.Collections.Generic.List<int> ch3Mix = new();

        static Enemy RandomCh3(Vector3 pos, ChapterDef ch, bool elite)
        {
            var room = RunManager.Instance ? (object)RunManager.Instance.CurrentRoom : null;
            if (room == null || room != ch3MixRoom || ch3Mix.Count == 0)
            {
                ch3MixRoom = room;
                ch3Mix.Clear();
                var all = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4 };
                int n = Random.value < 0.5f ? 3 : 4;
                while (ch3Mix.Count < n) { int i = Random.Range(0, all.Count); ch3Mix.Add(all[i]); all.RemoveAt(i); }
            }
            return ch3Mix[Random.Range(0, ch3Mix.Count)] switch
            {
                0 => MirrorShooter(pos, ch, elite),
                1 => LightMage(pos, ch, elite),
                2 => MirrorBlade(pos, ch, elite),
                3 => GlassKnight(pos, ch, elite),
                _ => Trickster(pos, ch, elite),
            };
        }

        static readonly Color Ch3Body = new Color(0.62f, 0.6f, 0.82f), Ch3Glass = new Color(0.78f, 0.93f, 1f);

        static Enemy Make3(string name, string model, Vector3 pos, ChapterDef ch, bool elite, float hp, Enemy.Kind kind, float r, float h, float speed, Color tint, System.Action<Transform> extra = null)
        {
            var e = Root(name, pos, hp, kind, r, h);
            if (!AttachModelFor(e, model, elite, out bool plus))
            {
                Proto2(e, ch, Color.Lerp(Ch3Body, tint, 0.4f), h * 0.85f, r * 1.1f);
                extra?.Invoke(e.transform.Find("Model"));
            }
            e.chapter = ch;
            e.speed = speed;
            if (elite) MakeElite(e, plus);
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 거울 사수: 내구 108 · 이속 2.8 · 반사탄 12
        public static Enemy MirrorShooter(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Make3("Enemy_MirrorShooter", "Ch3_Shooter", pos, ch, elite, 108, Enemy.Kind.MirrorShooter, 0.55f, 2.4f, 2.8f, new Color(0.7f, 0.9f, 1f),
                m => Part(PrimitiveType.Cylinder, m, new Vector3(0.35f, 1.2f, 0.45f), new Vector3(0.45f, 0.03f, 0.45f), M(Ch3Glass, 1f), new Vector3(90, 0, 0)));   // 손거울
            e.projectileDamage = 12;
            e.shootInterval = Enemy.ShooterInterval;
            return e;
        }

        // 빛가루 법사: 내구 108 · 이속 2.6 · 유도 구슬 12
        public static Enemy LightMage(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Make3("Enemy_LightMage", "Ch3_Mage", pos, ch, elite, 108, Enemy.Kind.LightMage, 0.55f, 2.4f, 2.6f, new Color(1f, 0.85f, 0.95f),
                m => Part(PrimitiveType.Sphere, m, new Vector3(0, 2.3f, -0.3f), Vector3.one * 0.45f, M(new Color(1f, 0.95f, 0.75f), 1f)));                         // 머리 위 빛
            e.projectileDamage = 12;
            return e;
        }

        // 거울 너머 검객: 내구 144 · 이속 3.6 · 베기 16
        public static Enemy MirrorBlade(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Make3("Enemy_MirrorBlade", "Ch3_Blade", pos, ch, elite, 144, Enemy.Kind.MirrorBlade, 0.6f, 2.5f, 3.6f, new Color(0.55f, 0.5f, 0.9f),
                m => Part(PrimitiveType.Cube, m, new Vector3(0.45f, 1.1f, 0.6f), new Vector3(0.08f, 0.08f, 1.3f), M(Ch3Glass, 1f)));                                 // 유리 칼
            e.contactDamage = 16;
            return e;
        }

        // 유리 기사: 내구 288 · 이속 2.6 · 정면 반사 · 방패 밀치기 16
        public static Enemy GlassKnight(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Make3("Enemy_GlassKnight", "Ch3_Knight", pos, ch, elite, 288, Enemy.Kind.GlassKnight, 0.95f, 2.8f, 2.6f, new Color(0.6f, 0.75f, 0.95f),
                m => { var mir = Part(PrimitiveType.Cube, m, new Vector3(0, 1.2f, 0.8f), new Vector3(1.4f, 1.6f, 0.12f), M(Ch3Glass, 1f)); mir.name = "Mirror"; });   // 거울 방패
            e.contactDamage = 16;
            e.GetComponent<Health>().ReflectFrom = e.KnightReflects;
            return e;
        }

        // 신기루 분신술사: 내구 108 · 이속 3.0 · 빛 탄 12
        public static Enemy Trickster(Vector3 pos, ChapterDef ch, bool elite = false)
        {
            var e = Make3("Enemy_Trickster", "Ch3_Trickster", pos, ch, elite, 108, Enemy.Kind.Trickster, 0.55f, 2.3f, 3f, new Color(1f, 0.7f, 0.85f),
                m => Part(PrimitiveType.Cylinder, m, new Vector3(0, 2.05f, 0), new Vector3(0.9f, 0.06f, 0.9f), M(new Color(0.35f, 0.25f, 0.45f))));                  // 넓은 모자
            e.projectileDamage = 12;
            return e;
        }

        /// <summary>신기루 분신: 본체와 같은 모습 · 한 발에 소멸 · 방 정리 · 보상에 안 셈</summary>
        public static Enemy TricksterClone(Vector3 pos, ChapterDef ch, Enemy owner)
        {
            var e = Trickster(pos, ch, false);
            e.name = "Enemy_TricksterClone";
            e.cloneOwner = owner;
            e.noReward = true;
            e.projectileDamage = owner.projectileDamage;
            var h = e.GetComponent<Health>();
            h.maxHp = 1; h.ResetHp(1);
            if (owner.elite) { var m = e.transform.Find("Model"); var om = owner.transform.Find("Model"); if (m && om) m.localScale = om.localScale; }
            return e;
        }

        // 1장 중간 보스: 태엽 현상금 사냥꾼 · 체력 1080 · 이속 4
        public static Enemy MidBoss(Vector3 pos, ChapterDef ch)
        {
            if (ch.Id == 2) return MidBoss2(pos, ch);
            if (ch.Id == 3) return MidBoss3(pos, ch);
            var e = Root("MidBoss_BountyHunter", pos, 1080 * ch.HpMul, Enemy.Kind.Boss, 1.3f, 3.8f);
            if (!AttachModel(e, "Ch1_MidBoss"))
            {
                var m = ProtoModel(e);
                var coat = M(Tint(new Color(0.38f, 0.26f, 0.26f), ch));
                Part(PrimitiveType.Capsule, m, new Vector3(0, 1.2f, 0), new Vector3(1.1f, 1.2f, 0.9f), coat);
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 2.25f, 0.1f), new Vector3(0.7f, 0.06f, 0.7f), M(new Color(0.95f, 0.9f, 0.75f), 2f), new Vector3(90, 0, 0));
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 2.6f, 0), new Vector3(1.3f, 0.04f, 1.3f), M(new Color(0.28f, 0.18f, 0.16f)));
                Part(PrimitiveType.Cylinder, m, new Vector3(0.5f, 1.3f, 0.8f), new Vector3(0.22f, 0.6f, 0.22f), M(new Color(0.55f, 0.45f, 0.4f), 2f), new Vector3(90, 0, 0));
                Part(PrimitiveType.Cube, m, new Vector3(-0.45f, 2.3f, -0.35f), new Vector3(0.12f, 0.5f, 0.35f), M(new Color(0.9f, 0.7f, 0.3f), 2f));
            }
            e.speed = 4f;
            e.chapter = ch;
            e.chapterDmg = ch.DmgMul;
            e.GetComponent<Health>().IsBoss = true;
            e.brain = e.gameObject.AddComponent<Ch1MidBoss>();
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 2장 중간 보스: 태엽 역장 · 체력 2520 · 이속 3.5 (2장 값은 이미 높여 잡아 챕터 배율 안 곱함)
        static Enemy MidBoss2(Vector3 pos, ChapterDef ch)
        {
            var e = Root("MidBoss_StationMaster", pos, 2520, Enemy.Kind.Boss, 1.3f, 4f);
            if (!AttachModel(e, "Ch2_MidBoss")) Proto2(e, ch, new Color(0.3f, 0.3f, 0.45f), 3.6f, 1.1f);
            e.speed = 3.5f;
            e.chapter = ch;
            e.chapterDmg = 1f;
            e.GetComponent<Health>().IsBoss = true;
            e.brain = e.gameObject.AddComponent<Ch2MidBoss>();
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 3장 블렌더 보스(리깅 없음): Resources/Kit/Ch3/이름 프리팹을 "Model"로 붙이고, 조각 움직임 컴포넌트는 그 아래 메시(Enemy가 흔드는 Model과 따로)에
        static T AttachKit3<T>(Enemy e, string res) where T : Component
        {
            var prefab = Resources.Load<GameObject>("Kit/Ch3/" + res);
            if (!prefab) return null;
            var m = Object.Instantiate(prefab, e.transform, false);
            m.name = "Model";
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            e.SetModel(m.transform);
            var mesh = m.transform.childCount > 0 ? m.transform.GetChild(0) : m.transform;
            return mesh.gameObject.AddComponent<T>();
        }

        // 3장 중간 보스: 신기루 마술사 · 체력 3960 · 이속 3.5 (블렌더 + Codex 4m 태엽 꼭두각시)
        static Enemy MidBoss3(Vector3 pos, ChapterDef ch)
        {
            var e = Root("MidBoss_MirageMagician", pos, 3960, Enemy.Kind.Boss, 1.2f, 4f);
            if (!AttachKit3<PuppetAnim>(e, "Ch3_Magician")) Proto2(e, ch, new Color(0.45f, 0.4f, 0.65f), 3.8f, 1.1f);
            e.speed = 3.5f;
            e.chapter = ch;
            e.chapterDmg = 1f;
            e.GetComponent<Health>().IsBoss = true;
            e.brain = e.gameObject.AddComponent<Ch3MidBoss>();
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 3장 보스: 거울 고래 · 체력 7020(표) · 이속 3 (블렌더 + Codex 길이 14m, 조각 움직임 WhaleAnim)
        static Enemy Boss3(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Boss_MirrorWhale", pos, ch.BossHp, Enemy.Kind.Boss, 3.2f, 5f);
            if (!AttachKit3<WhaleAnim>(e, "Ch3_Whale")) Proto2(e, ch, new Color(0.7f, 0.85f, 0.95f), 4.5f, 5f);
            e.speed = 3f;
            e.chapter = ch;
            e.chapterDmg = 1f;
            e.GetComponent<Health>().IsBoss = true;
            e.brain = e.gameObject.AddComponent<Ch3Boss>();
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 2장 보스: 멈춘 기관차 거인 · 체력 4860(표) · 이속 2.5
        static Enemy Boss2(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Boss_LocoGiant", pos, ch.BossHp, Enemy.Kind.Boss, 2.4f, 6.5f);
            if (!AttachModel(e, "Ch2_Boss")) Proto2(e, ch, new Color(0.34f, 0.26f, 0.48f), 6f, 2.4f);
            e.speed = 2.5f;
            e.chapter = ch;
            e.chapterDmg = 1f;
            e.GetComponent<Health>().IsBoss = true;
            e.brain = e.gameObject.AddComponent<Ch2Boss>();
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        public static Enemy Boss(Vector3 pos, ChapterDef ch)
        {
            if (ch.Id == 2) return Boss2(pos, ch);
            if (ch.Id == 3) return Boss3(pos, ch);
            var body = M(Color.Lerp(new Color(0.88f, 0.78f, 0.58f), ch.Wall, 0.35f), 6f);
            var gold = M(new Color(0.95f, 0.7f, 0.25f), 5f);
            var e = Root("Boss_" + ch.BossPattern, pos, ch.BossHp, Enemy.Kind.Boss, 2.4f, 6.2f);
            // 1장 보스는 Tripo 모델(녹은 시계 거인)이 있으면 그걸로. 바늘(Hand)은 판정 · 방향용으로 남기되 안 보이게
            bool modeled = ch.Id == 1 && AttachModel(e, "Ch1_Boss");
            if (!modeled)
            {
                Part(PrimitiveType.Cylinder, e.transform, new Vector3(0, 1.6f, 0), new Vector3(2.6f, 1.6f, 2.6f), body);
                Part(PrimitiveType.Cylinder, e.transform, new Vector3(0, 3.3f, 0), new Vector3(2.9f, 0.12f, 2.9f), gold);
                Part(PrimitiveType.Sphere, e.transform, new Vector3(0, 3.9f, 0), Vector3.one * 1.3f, body);
                Part(PrimitiveType.Sphere, e.transform, new Vector3(0.3f, 4.0f, 0.55f), Vector3.one * 0.25f, M(new Color(1f, 0.85f, 0.3f), 2f));
                Part(PrimitiveType.Sphere, e.transform, new Vector3(-0.3f, 4.0f, 0.55f), Vector3.one * 0.25f, M(new Color(1f, 0.85f, 0.3f), 2f));
            }
            int hands = ch.BossPattern == "Gear" || ch.BossPattern == "Master" ? 2 : 1;
            for (int i = 0; i < hands; i++)
            {
                var hand = new GameObject(i == 0 ? "Hand" : "Hand2").transform;
                hand.SetParent(e.transform, false);
                hand.localPosition = new Vector3(0, 1.0f, 0);
                hand.localRotation = Quaternion.Euler(0, i * 180, 0);
                Part(PrimitiveType.Cube, hand, new Vector3(0, 0, 2.6f), new Vector3(0.35f, 0.25f, 4.2f), gold);
            }
            e.speed = 2.2f;
            e.contactDamage = 18 * ch.DmgMul;
            e.projectileDamage = 10 * ch.DmgMul;
            e.shootInterval = 2.4f;
            e.bossPattern = ch.BossPattern;
            e.GetComponent<Health>().IsBoss = true;
            e.chapter = ch;
            e.chapterDmg = ch.DmgMul;
            if (ch.Id == 1)
            {
                // 녹은 시계 거인: 이속 3 · 시계 바늘 8m
                e.speed = 3f;
                foreach (var hn in new[] { "Hand", "Hand2" })
                {
                    var h = e.transform.Find(hn);
                    if (!h) continue;
                    var bar = h.GetChild(0);
                    bar.localPosition = new Vector3(0, 0, 4f);
                    bar.localScale = new Vector3(0.35f, 0.25f, 8f);
                    bar.GetComponent<Renderer>().enabled = !modeled;   // 모델이 칼을 들고 있으면 막대는 숨김
                }
                e.brain = e.gameObject.AddComponent<Ch1Boss>();
            }
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }
    }
}
