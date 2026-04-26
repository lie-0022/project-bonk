/// <summary>
/// 개별 무기 슬롯. 타이머와 레벨을 관리한다.
/// </summary>
public class WeaponSlot
{
    public WeaponDataSO Data { get; private set; }
    public int Level { get; private set; } = 1;

    private float _currentInterval;
    private float _cooldownRemaining;

    public float CooldownNormalized =>
        _currentInterval > 0f ? UnityEngine.Mathf.Clamp01(_cooldownRemaining / _currentInterval) : 0f;

    public WeaponSlot(WeaponDataSO data, float attackSpeedMultiplier)
    {
        Data = data;
        UpdateInterval(attackSpeedMultiplier);
        _cooldownRemaining = 0f; // 획득 즉시 발동 가능
    }

    public void UpdateInterval(float attackSpeedMultiplier)
    {
        _currentInterval = Data.BaseAttackInterval * attackSpeedMultiplier;
    }

    public void Upgrade(float attackSpeedMultiplier)
    {
        Level++;
        UpdateInterval(attackSpeedMultiplier);
    }

    public void Tick(float deltaTime, WeaponSystem system)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
            return;
        }

        // 범위 내 적이 있을 때만 발동
        if (EnemyBase.ActiveEnemies.Count == 0) return;

        system.ExecuteWeapon(this);
        _cooldownRemaining = _currentInterval;
    }
}
