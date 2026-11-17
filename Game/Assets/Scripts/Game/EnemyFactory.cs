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
                foreach (var m in mats) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * new Color(1.15f, 0.82f, 0.68f));
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

        // 1장 중간 보스: 태엽 현상금 사냥꾼 · 체력 1080 · 이속 4
        public static Enemy MidBoss(Vector3 pos, ChapterDef ch)
        {
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

        public static Enemy Boss(Vector3 pos, ChapterDef ch)
        {
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
