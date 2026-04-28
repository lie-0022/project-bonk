using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패시브 종류 식별자. spec-system.md 정의와 일치한다.
/// </summary>
public enum PassiveType
{
    MaxHp,
    HpRegen,
    Dodge,
    AttackSpeed,
    CritChance,
    CritDamage,
    Lifesteal,
    ProjectileCount,
    ProjectileSpeed,
    MoveSpeed,
    ExtraJump,
    Luck,
    Difficulty,
}

/// <summary>
/// 플레이어 중앙 스탯 컨테이너. 패시브 레벨을 저장하고 GDD 공식에 따라 최종 스탯을 산출한다.
/// WeaponSystem, PlayerController, HealthComponent, LevelupSelection이 이 값을 참조한다.
/// 공식 출처: design/gdd/spec-system.md (max level 15).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    /// <summary>스탯 변경 시 브로드캐스트. WeaponSystem 등이 구독해 수치를 갱신한다.</summary>
    public static event Action OnStatsChanged;

    public const int MaxPassiveLevel = 15;

    [Header("Base Movement")]
    [SerializeField] private float _baseMoveSpeed = 6f;
    [SerializeField] private int _baseExtraJumps = 0;

    [Header("Base Combat")]
    [SerializeField] private float _baseMaxHp = 100f;
    [SerializeField] private float _baseHpRegen = 0f;

    [Header("Base Weapon")]
    [SerializeField] private float _baseCritMultiplier = 1.5f;
    [SerializeField] private int _baseProjectileCount = 1;

    // ── 최종 스탯 (기본값 + 패시브 보너스) ─────────────────

    // 이동
    public float MoveSpeed { get; private set; }
    public int ExtraJumps { get; private set; }

    // 생존
    public float MaxHp { get; private set; }
    public float HpRegen { get; private set; }
    public float DodgeChance { get; private set; }       // 0~0.60 (cap)

    // 무기
    public float AttackSpeedMultiplier { get; private set; } // 0.30~1.0 (floor 적용)
    public float CritChance { get; private set; }         // 0~0.75 (cap)
    public float CritMultiplier { get; private set; }     // base + bonus
    public float Lifesteal { get; private set; }          // 0~0.25 (cap)
    public int ProjectileCount { get; private set; }
    public float ProjectileSpeed { get; private set; }    // 1.0 + bonus

    // 메타
    public float LuckChance { get; private set; }         // 0~0.80 (cap), 등급 가중치 보정용
    public float DifficultySpawnMultiplier { get; private set; } // 1.0 + 0.15*lvl
    public float DifficultyRewardMultiplier { get; private set; } // 1.0 + 0.20*lvl

    // ── 패시브 레벨 저장 ─────────────────────────────────
    private readonly Dictionary<PassiveType, int> _levels = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResetPassives();
    }

    /// <summary>현재 패시브 레벨 조회. 미보유 시 0 반환.</summary>
    public int GetPassiveLevel(PassiveType type) =>
        _levels.TryGetValue(type, out int lv) ? lv : 0;

    /// <summary>패시브 레벨을 1 증가시킨다. 최대 레벨 도달 시 무시.</summary>
    /// <returns>실제로 증가했으면 true.</returns>
    public bool IncrementPassive(PassiveType type)
    {
        int current = GetPassiveLevel(type);
        if (current >= MaxPassiveLevel) return false;
        _levels[type] = current + 1;
        RecalculateStats();
        return true;
    }

    /// <summary>모든 패시브 레벨을 0으로 초기화한다. 스테이지 클리어 시 호출.</summary>
    public void ResetPassives()
    {
        _levels.Clear();
        RecalculateStats();
    }

    /// <summary>패시브 레벨을 기반으로 최종 스탯을 재계산한다.</summary>
    public void RecalculateStats()
    {
        // 가산식 (선형 누적)
        MoveSpeed = _baseMoveSpeed + 0.2f * GetPassiveLevel(PassiveType.MoveSpeed);
        ExtraJumps = _baseExtraJumps + GetPassiveLevel(PassiveType.ExtraJump);
        MaxHp = _baseMaxHp + 15f * GetPassiveLevel(PassiveType.MaxHp);
        HpRegen = 0.5f * GetPassiveLevel(PassiveType.HpRegen) + _baseHpRegen;
        CritMultiplier = _baseCritMultiplier + 0.15f * GetPassiveLevel(PassiveType.CritDamage);
        ProjectileCount = _baseProjectileCount + GetPassiveLevel(PassiveType.ProjectileCount) / 3;
        ProjectileSpeed = 1f + 0.1f * GetPassiveLevel(PassiveType.ProjectileSpeed);

        // DR (Diminishing Returns) — final = cap × (1 - (1-perLevel)^level)
        DodgeChance = DiminishingReturns(GetPassiveLevel(PassiveType.Dodge), 0.08f, 0.60f);
        CritChance = DiminishingReturns(GetPassiveLevel(PassiveType.CritChance), 0.08f, 0.75f);
        LuckChance = DiminishingReturns(GetPassiveLevel(PassiveType.Luck), 0.10f, 0.80f);

        // Cap 가산식
        Lifesteal = Mathf.Min(0.02f * GetPassiveLevel(PassiveType.Lifesteal), 0.25f);

        // 곱셈 + floor
        float atkMult = Mathf.Pow(0.95f, GetPassiveLevel(PassiveType.AttackSpeed));
        AttackSpeedMultiplier = Mathf.Max(atkMult, 0.30f);

        // 난이도
        int diffLv = GetPassiveLevel(PassiveType.Difficulty);
        DifficultySpawnMultiplier = 1f + 0.15f * diffLv;
        DifficultyRewardMultiplier = 1f + 0.20f * diffLv;

        OnStatsChanged?.Invoke();
    }

    /// <summary>점감 공식: final = cap × (1 - (1-perLevel)^level)</summary>
    private static float DiminishingReturns(int level, float perLevel, float cap)
    {
        if (level <= 0) return 0f;
        return cap * (1f - Mathf.Pow(1f - perLevel, level));
    }
}
