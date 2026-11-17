// 몬스터 공격이 엄폐물 · 폭발물에도 닿게 (2026-11-17 사용자: 공중 짐 등 폭발물이 몬스터 공격에도 반응하도록)
// 적 탄은 원래부터 엄폐물 · 폭발물을 맞혔음(EnemyProjectile). 여기는 근접 · 범위 공격(내리찍기 · 돌진 · 자폭 · 신호등 폭발 등)용.
// 대상 = Health가 있되 고양이도 적도 아닌 것 (Kit.Make · Ch2Kit.MakeHp로 만든 엄폐물 · 화약통 · 증기 밸브 · 공중 짐 · 광차).
// 맞은 방향(Health.HitFrom)을 남겨서 공중 짐이 그 방향으로 날아감.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class PropHit
    {
        static readonly Collider[] buf = new Collider[64];
        static readonly HashSet<Health> seen = new();

        static bool IsProp(Health h) => h && h.Alive && !h.isPlayer && !h.GetComponent<Enemy>();

        /// <summary>원형 범위 (center = 바닥 자리). once가 있으면 이미 맞은 것은 건너뜀 (돌진처럼 매 프레임 부를 때)</summary>
        public static void Area(Vector3 center, float radius, float dmg, HashSet<Health> once = null)
        {
            seen.Clear();
            var flat = new Vector3(center.x, 0, center.z);
            int n = Physics.OverlapCapsuleNonAlloc(flat + Vector3.up * 0.2f, flat + Vector3.up * 2.2f, radius, buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = buf[i].GetComponentInParent<Health>();
                if (!IsProp(h) || !seen.Add(h) || (once != null && !once.Add(h))) continue;
                var d = Effects.Flat(h.transform.position - flat);
                h.HitFrom(d.sqrMagnitude > 1e-4f ? d : Vector3.forward);
                h.TakeDamage(dmg);
            }
        }
    }
}
