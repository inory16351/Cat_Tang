namespace CatTang
{
    // 풀링 (2026-11-20): 로그 없이 키별 생성과 재사용 횟수를 조회한다.
    public readonly struct PoolStats
    {
        public readonly int Created, Reused, Idle;
        public PoolStats(int created, int reused, int idle)
        {
            Created = created; Reused = reused; Idle = idle;
        }
    }
}
