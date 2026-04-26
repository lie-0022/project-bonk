using System;
using UnityEngine;

/// <summary>
/// 플레이어 중앙 스탯 컨테이너. 패시브 효과가 이 클래스를 통해 적용된다.
/// WeaponSystem, PlayerController, HealthComponent가 이 값을 참조한다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    /// <summary>스탯 변경 시 브로드캐스트. WeaponSystem 등이 구독해 수치를 갱신한다.</summary>
    public static event Action OnStatsChanged;

    [Header("Base Movement")]
    [SerializeField] private float _baseMoveSpeed = 6f;
    [SerializeField] private int _baseExtraJumps = 0;

    [Header("Base Combat")]
    [SerializeField] private float _baseMaxHp = 100f;
    [SerializeField] private float _baseHpRegen = 0f;
    [SerializeField] private float _baseArmor = 0f;
    [SerializeField] private float _baseDodgeChance = 0f;

    [Header("Base Weapon")]
    [SerializeField] private float _baseAttackSpeedMultiplier = 1f;
    [SerializeField] private float _baseCritChance = 0f;
    [SerializeField] private float _baseCritMultiplier = 1.5f;
    [SerializeField] private float _baseLifesteal = 0f;
    [SerializeField] private int _baseProjectileCount = 1;
    [SerializeField] private float _baseProjectileSpeed = 1f;
    [SerializeField] private float _baseSizeMultiplier = 1f;

    // ── 최종 스탯 (기본값 + 패시브 보너스) ─────────────────

    // 이동
    public float MoveSpeed { get; private set; }
    public int ExtraJumps { get; private set; }

    // 생존
    public float MaxHp { get; private set; }
    public float HpRegen { get; private set; }
    public float Armor { get; private set; }
    public float DodgeChance { get; private set; }

    // 무기
    public float AttackSpeedMultiplier { get; private set; }
    public float CritChance { get; private set; }
    public float CritMultiplier { get; private set; }
    public float Lifesteal { get; private set; }
    public int ProjectileCount { get; private set; }
    public float ProjectileSpeed { get; private set; }
    public float SizeMultiplier { get; private set; }

    // ── 패시브 보너스 누적값 ─────────────────────────────────
    private float _bonusMoveSpeed;
    private int _bonusExtraJumps;
    private float _bonusMaxHp;
    private float _bonusHpRegen;
    private float _armorReduction;        // 0~1 (피해 감소율)
    private float _dodgeChance;           // 0~1
    private float _attackSpeedReduction;  // 0~1 (발동 간격 감소율)
    private float _critChance;            // 0~1
    private float _critMultiplierBonus;
    private float _lifesteal;             // 0~1
    private int _bonusProjectileCount;
    private float _projectileSpeedBonus;  // 배율
    private float _sizeBonus;             // 배율

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RecalculateStats();
    }

    /// <summary>패시브 적용 후 최종 스탯을 재계산한다.</summary>
    public void RecalculateStats()
    {
        MoveSpeed = _baseMoveSpeed + _bonusMoveSpeed;
        ExtraJumps = _baseExtraJumps + _bonusExtraJumps;

        MaxHp = _baseMaxHp + _bonusMaxHp;
        HpRegen = _baseHpRegen + _bonusHpRegen;
        Armor = Mathf.Clamp01(_baseArmor + _armorReduction);
        DodgeChance = Mathf.Clamp01(_baseDodgeChance + _dodgeChance);

        AttackSpeedMultiplier = Mathf.Max(0.1f, _baseAttackSpeedMultiplier - _attackSpeedReduction);
        CritChance = Mathf.Clamp01(_baseCritChance + _critChance);
        CritMultiplier = _baseCritMultiplier + _critMultiplierBonus;
        Lifesteal = Mathf.Clamp01(_baseLifesteal + _lifesteal);
        ProjectileCount = Mathf.Max(1, _baseProjectileCount + _bonusProjectileCount);
        ProjectileSpeed = _baseProjectileSpeed + _projectileSpeedBonus;
        SizeMultiplier = _baseSizeMultiplier + _sizeBonus;

        OnStatsChanged?.Invoke();
    }

    /// <summary>모든 패시브 보너스를 초기화한다. 스테이지 클리어 시 호출.</summary>
    public void ResetPassives()
    {
        _bonusMoveSpeed = 0f;
        _bonusExtraJumps = 0;
        _bonusMaxHp = 0f;
        _bonusHpRegen = 0f;
        _armorReduction = 0f;
        _dodgeChance = 0f;
        _attackSpeedReduction = 0f;
        _critChance = 0f;
        _critMultiplierBonus = 0f;
        _lifesteal = 0f;
        _bonusProjectileCount = 0;
        _projectileSpeedBonus = 0f;
        _sizeBonus = 0f;
        RecalculateStats();
    }

    // ── 패시브 적용 메서드 (레벨 1씩 올라갈 때 호출) ──────────

    public void AddMoveSpeed(float amount)           { _bonusMoveSpeed += amount; RecalculateStats(); }
    public void AddExtraJump(int amount)             { _bonusExtraJumps += amount; RecalculateStats(); }
    public void AddMaxHp(float amount)               { _bonusMaxHp += amount; RecalculateStats(); }
    public void AddHpRegen(float amount)             { _bonusHpRegen += amount; RecalculateStats(); }
    public void AddArmor(float amount)               { _armorReduction += amount; RecalculateStats(); }
    public void AddDodgeChance(float amount)         { _dodgeChance += amount; RecalculateStats(); }
    public void AddAttackSpeed(float reductionRate)  { _attackSpeedReduction += reductionRate; RecalculateStats(); }
    public void AddCritChance(float amount)          { _critChance += amount; RecalculateStats(); }
    public void AddCritMultiplier(float amount)      { _critMultiplierBonus += amount; RecalculateStats(); }
    public void AddLifesteal(float amount)           { _lifesteal += amount; RecalculateStats(); }
    public void AddProjectileCount(int amount)       { _bonusProjectileCount += amount; RecalculateStats(); }
    public void AddProjectileSpeed(float amount)     { _projectileSpeedBonus += amount; RecalculateStats(); }
    public void AddSize(float amount)                { _sizeBonus += amount; RecalculateStats(); }
}
