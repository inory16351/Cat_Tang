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
        public static Enemy RandomMinion(Vector3 pos, ChapterDef ch)
        {
            float r = Random.value, outlaw = Mathf.Clamp01(ch.ShooterRatio + 0.1f);
            if (r < outlaw) return Outlaw(pos, ch);
            if (r < outlaw + 0.25f) return Bomber(pos, ch);
            return Brute(pos, ch);
        }

        // 태엽 무법자: 원거리 · 이속 2.6 · 2초마다 1발(예고 0.5초) · 피해 8 · 내구 24
        public static Enemy Outlaw(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Enemy_Outlaw", pos, 24 * ch.HpMul, Enemy.Kind.Outlaw, 0.45f, 1.7f);
            if (!AttachModel(e, "Ch1_Outlaw"))
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
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 시계추 깡패: 돌진 근접 · 이속 3.4 · 돌진 예고 0.6초 · 피해 12 · 내구 48
        public static Enemy Brute(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Enemy_Brute", pos, 48 * ch.HpMul, Enemy.Kind.Brute, 0.75f, 2.1f);
            if (!AttachModel(e, "Ch1_Brute"))
            {
                var m = ProtoModel(e);
                Part(PrimitiveType.Cube, m, new Vector3(0, 0.95f, 0), new Vector3(1.3f, 1.4f, 0.9f), M(Tint(new Color(0.36f, 0.28f, 0.3f), ch)));
                Part(PrimitiveType.Cylinder, m, new Vector3(0, 1.35f, 0.47f), new Vector3(0.8f, 0.05f, 0.8f), M(new Color(0.95f, 0.9f, 0.75f), 2f), new Vector3(90, 0, 0));
                Part(PrimitiveType.Sphere, m, new Vector3(0.8f, 0.45f, 0.6f), Vector3.one * 0.6f, M(new Color(0.95f, 0.65f, 0.25f), 2f));
            }
            e.speed = 3.4f;
            e.contactDamage = 12 * ch.DmgMul;
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 모래시계 폭탄병: 자폭 · 이속 4.5 · 붙으면 0.8초 점멸 후 반경 2m 폭발 · 피해 12 · 내구 16
        public static Enemy Bomber(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Enemy_Bomber", pos, 16 * ch.HpMul, Enemy.Kind.Bomber, 0.4f, 1.3f);
            if (!AttachModel(e, "Ch1_Bomber"))
            {
                var m = ProtoModel(e);
                Part(PrimitiveType.Capsule, m, new Vector3(0, 0.55f, 0), new Vector3(0.6f, 0.45f, 0.5f), M(Tint(new Color(0.4f, 0.3f, 0.28f), ch)));
                Part(PrimitiveType.Sphere, m, new Vector3(0, 1.1f, 0), new Vector3(0.45f, 0.55f, 0.45f), M(new Color(1f, 0.75f, 0.3f), 2f));
                Part(PrimitiveType.Sphere, m, new Vector3(0.4f, 0.7f, 0.25f), Vector3.one * 0.32f, M(new Color(0.15f, 0.12f, 0.12f)));
            }
            e.speed = 4.5f;
            e.contactDamage = 12 * ch.DmgMul;
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        // 1장 중간 보스: 태엽 현상금 사냥꾼 · 체력 1080 · 이속 4
        public static Enemy MidBoss(Vector3 pos, ChapterDef ch)
        {
            var e = Root("MidBoss_BountyHunter", pos, 1080 * ch.HpMul, Enemy.Kind.Boss, 0.9f, 2.8f);
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
            var e = Root("Boss_" + ch.BossPattern, pos, ch.BossHp, Enemy.Kind.Boss, 1.5f, 4f);
            Part(PrimitiveType.Cylinder, e.transform, new Vector3(0, 1.6f, 0), new Vector3(2.6f, 1.6f, 2.6f), body);
            Part(PrimitiveType.Cylinder, e.transform, new Vector3(0, 3.3f, 0), new Vector3(2.9f, 0.12f, 2.9f), gold);
            Part(PrimitiveType.Sphere, e.transform, new Vector3(0, 3.9f, 0), Vector3.one * 1.3f, body);
            Part(PrimitiveType.Sphere, e.transform, new Vector3(0.3f, 4.0f, 0.55f), Vector3.one * 0.25f, M(new Color(1f, 0.85f, 0.3f), 2f));
            Part(PrimitiveType.Sphere, e.transform, new Vector3(-0.3f, 4.0f, 0.55f), Vector3.one * 0.25f, M(new Color(1f, 0.85f, 0.3f), 2f));
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
                }
                e.brain = e.gameObject.AddComponent<Ch1Boss>();
            }
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }
    }
}
