// 적 탄: 눈에 보이게 느린 구체. 벽에 닿거나 시간이 지나면 사라짐.
using UnityEngine;

namespace CatTang
{
    public class EnemyProjectile : MonoBehaviour
    {
        Vector3 dir;
        float speed, damage, dieAt, rangeLeft;
        static Material mat;

        /// <summary>range > 0이면 그 거리만큼 날아가면 사라짐 (산탄 사거리)</summary>
        public static EnemyProjectile Spawn(Vector3 pos, Vector3 dir, float speed, float damage, float range = 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyBullet";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.42f;
            if (mat == null)
            {
                var sh = Shader.Find("CatTang/Toon") ?? Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(sh);
                mat.SetColor("_BaseColor", new Color(0.75f, 0.3f, 0.95f));
                if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 3f);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var p = go.AddComponent<EnemyProjectile>();
            p.dir = dir.normalized;
            p.speed = speed;
            p.damage = damage;
            p.dieAt = Time.time + 6f;
            p.rangeLeft = range > 0 ? range : float.MaxValue;
            return p;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            if (Physics.Raycast(transform.position, dir, out var hit, step + 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var hp = hit.collider.GetComponentInParent<Health>();
                if (hp == null) { Destroy(gameObject); return; }
                // 엄폐물 · 화약통은 적 탄을 막음 (맞으면 깎임)
                if (!hp.isPlayer && !hp.GetComponent<Enemy>()) { hp.TakeDamage(damage); Destroy(gameObject); return; }            // 벽
            }
            transform.position += dir * step;
            rangeLeft -= step;
            if (rangeLeft <= 0) { Destroy(gameObject); return; }
            var pl = PlayerController.Instance;
            if (pl && pl.health.Alive)
            {
                var d = pl.transform.position + Vector3.up * 0.9f - transform.position;
                if (d.sqrMagnitude < 0.55f * 0.55f)
                {
                    pl.health.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }
            if (Time.time > dieAt) Destroy(gameObject);
        }
    }
}
