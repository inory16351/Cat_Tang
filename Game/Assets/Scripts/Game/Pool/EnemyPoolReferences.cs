namespace CatTang
{
    public partial class Enemy
    {
        // 풀링 (2026-11-20): 파괴될 때 무효가 되던 보관 참조를 반환 시점에 비운다.
        internal static void ForgetPooledTelegraph(Telegraph returned)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var enemy = All[i];
                if (enemy && enemy.tele == returned) enemy.tele = null;
            }
        }

        internal static void ForgetPooledProjectile(EnemyProjectile returned)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var enemy = All[i];
                if (!enemy) continue;
                for (int j = 0; j < enemy.heldOrbs.Count; j++)
                    if (enemy.heldOrbs[j] == returned) enemy.heldOrbs[j] = null;
            }
        }
    }
}
