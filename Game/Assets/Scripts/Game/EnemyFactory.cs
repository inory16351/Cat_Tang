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

        public static Enemy Chaser(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Enemy_Chaser", pos, 30 * ch.HpMul, Enemy.Kind.Chaser, 0.5f, 1.2f);
            Part(PrimitiveType.Cube, e.transform, new Vector3(0, 0.6f, 0), Vector3.one * 0.9f, M(Tint(new Color(0.58f, 0.36f, 0.78f), ch)), new Vector3(0, 45, 0));
            Part(PrimitiveType.Sphere, e.transform, new Vector3(0, 0.75f, 0.45f), Vector3.one * 0.25f, M(new Color(1f, 0.85f, 0.3f), 2f));
            e.speed = 3.4f + 0.15f * ch.Id;
            e.contactDamage = 8 * ch.DmgMul;
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }

        public static Enemy Shooter(Vector3 pos, ChapterDef ch)
        {
            var e = Root("Enemy_Shooter", pos, 22 * ch.HpMul, Enemy.Kind.Shooter, 0.5f, 1.4f);
            Part(PrimitiveType.Sphere, e.transform, new Vector3(0, 0.8f, 0), Vector3.one, M(Tint(new Color(0.3f, 0.58f, 0.78f), ch)));
            Part(PrimitiveType.Cylinder, e.transform, new Vector3(0, 0.85f, 0.6f), new Vector3(0.22f, 0.3f, 0.22f), M(new Color(1f, 0.85f, 0.3f), 2f), new Vector3(90, 0, 0));
            e.speed = 2.6f;
            e.projectileDamage = 7 * ch.DmgMul;
            e.shootInterval = Mathf.Max(1.1f, 2.1f - 0.15f * ch.Id);
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
            e.GetComponent<Health>().RefreshRenderers();
            return e;
        }
    }
}
