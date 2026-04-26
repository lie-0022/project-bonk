/// <summary>
/// ObjectPool에서 관리되는 모든 오브젝트의 생명주기 계약.
/// 풀에서 꺼낼 때(OnSpawn)와 반환할 때(OnDespawn) 상태를 초기화한다.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 풀에서 꺼낸 직후 호출. 위치·상태·타이머 등 모든 로컬 상태 초기화.
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 풀에 반환하기 직전 호출. 이벤트 구독 해제, 파티클 정지 등 정리 작업.
    /// </summary>
    void OnDespawn();
}
