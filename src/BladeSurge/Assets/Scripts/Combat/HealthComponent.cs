using System;
using UnityEngine;

/// <summary>
/// 플레이어와 적 모두에게 공통으로 사용되는 체력 컴포넌트.
/// 피해 수신, 사망 판정, 무적 상태를 처리한다.
/// </summary>
public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _xpReward = 10f;

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsAlive { get; private set; } = true;
    public bool IsInvincible { get; set; }

    /// <summary>피격 시 발생. (피해량, 현재 HP)</summary>
    public event Action<float, float> OnDamaged;

    /// <summary>사망 시 발생. (xpReward) — 적 전용 값, 플레이어는 0</summary>
    public event Action<float> OnDeath;

    private void Awake()
    {
        CurrentHp = _maxHp;
    }

    /// <summary>
    /// 피해를 적용한다. 무적 상태이거나 이미 사망한 경우 무시한다.
    /// 음수 피해(힐)는 MVP에서 지원하지 않으므로 무시한다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || IsInvincible || amount <= 0f) return;

        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        Debug.Log($"[Health] {gameObject.name} HP: {CurrentHp:F1} / {_maxHp:F1} (피해: {amount})");
        OnDamaged?.Invoke(amount, CurrentHp);

        if (CurrentHp <= 0f)
            Die();
    }

    /// <summary>
    /// 풀에서 재사용할 때 호출. HP를 최대치로 되돌리고 사망 상태를 초기화한다.
    /// </summary>
    public void ResetHealth()
    {
        CurrentHp = _maxHp;
        IsAlive = true;
        IsInvincible = false;
    }

    /// <summary>
    /// 웨이브 배율을 적용해 최대 체력을 조정한다. OnSpawn 이후 WaveSpawner가 호출.
    /// </summary>
    public void ApplyHpMultiplier(float multiplier)
    {
        _maxHp = _maxHp * multiplier;
        CurrentHp = _maxHp;
    }

    /// <summary>HP를 회복한다. 최대 HP를 초과하지 않는다.</summary>
    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        CurrentHp = Mathf.Min(CurrentHp + amount, _maxHp);
    }

    private void Die()
    {
        IsAlive = false;
        OnDeath?.Invoke(_xpReward);
    }
}
