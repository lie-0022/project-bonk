/// <summary>
/// 피해를 받을 수 있는 모든 오브젝트의 계약.
/// DamageDealer가 HealthComponent 구체 타입 대신 이 인터페이스에 의존한다.
/// </summary>
public interface IDamageable
{
    /// <summary>현재 체력. 읽기 전용.</summary>
    float CurrentHp { get; }

    /// <summary>최대 체력. 읽기 전용.</summary>
    float MaxHp { get; }

    /// <summary>생존 여부. false이면 TakeDamage 호출을 무시한다.</summary>
    bool IsAlive { get; }

    /// <summary>
    /// 무적 상태. true이면 TakeDamage 호출을 무시.
    /// 플레이어 대시 무적 프레임에서 외부에서 설정한다.
    /// </summary>
    bool IsInvincible { get; set; }

    /// <summary>
    /// 피해를 적용한다. amount <= 0이면 무시.
    /// IsAlive == false 또는 IsInvincible == true이면 무시.
    /// </summary>
    void TakeDamage(float amount);
}
