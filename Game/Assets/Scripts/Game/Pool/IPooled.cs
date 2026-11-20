namespace CatTang
{
    // 풀링 (2026-11-20): 컴포넌트의 사용별 상태는 각 컴포넌트가 되돌린다.
    public interface IPooled
    {
        void OnSpawned();
        void OnDespawned();
    }
}
