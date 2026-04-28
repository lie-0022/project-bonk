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
    /// <summary>발사체 이동 속도 / 검 휘두르기 속도 배율. AttackSpeed 패시브와 연동된 파생 값.</summary>
    public float ProjectileSpeed { get; private set; }    // 1 / AttackSpeedMultiplier

    // 메타
    public float LuckChance { get; private set; }         // 0~0.80 (cap), 등급 가중치 보정용
    public float DifficultySpawnMultiplier { get; private set; } // 1.0 + 0.15*lvl
    public float DifficultyRewardMultiplier { get; private set; } // 1.0 + 0.20*lvl

    // ── 패시브 레벨 저장 ─────────────────────────────────
    // _levels: 선택 횟수 (Lv 15 cap 판정용)
    // _effectiveLevels: 등급 가중치 누적 (수치 산출용). 커먼 1회=+1.0, 레전드 1회=+4.0.
    private readonly Dictionary<PassiveType, int> _levels = new();
    private readonly Dictionary<PassiveType, float> _effectiveLevels = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResetPassives();
    }

    /// <summary>현재 패시브 선택 횟수 조회 (Lv 15 cap 판정용). 미보유 시 0.</summary>
    public int GetPassiveLevel(PassiveType type) =>
        _levels.TryGetValue(type, out int lv) ? lv : 0;

    /// <summary>등급 가중치 적용된 effective level 조회 (수치 환산용).</summary>
    public float GetEffectiveLevel(PassiveType type) =>
        _effectiveLevels.TryGetValue(type, out float lv) ? lv : 0f;

    /// <summary>
    /// 패시브 레벨을 1 증가시킨다. 등급 가중치는 effective level에 누적.
    /// 최대 레벨(15회 선택) 도달 시 무시.
    /// </summary>
    /// <returns>실제로 증가했으면 true.</returns>
    public bool IncrementPassive(PassiveType type, CardGrade grade = CardGrade.Common)
    {
        int current = GetPassiveLevel(type);
        if (current >= MaxPassiveLevel) return false;

        _levels[type] = current + 1;
        _effectiveLevels[type] = GetEffectiveLevel(type) + GradeBonus.PassiveWeight(grade);
        RecalculateStats();
        return true;
    }

    /// <summary>모든 패시브 레벨을 0으로 초기화한다. 스테이지 클리어 시 호출.</summary>
    public void ResetPassives()
    {
        _levels.Clear();
        _effectiveLevels.Clear();
        RecalculateStats();
    }

    /// <summary>패시브 effective level을 기반으로 최종 스탯을 재계산한다.</summary>
    public void RecalculateStats()
    {
        // 가산식 (선형 누적, effective level 사용)
        MoveSpeed = _baseMoveSpeed + 1.0f * GetEffectiveLevel(PassiveType.MoveSpeed);
        ExtraJumps = _baseExtraJumps + Mathf.FloorToInt(GetEffectiveLevel(PassiveType.ExtraJump));
        MaxHp = _baseMaxHp + 15f * GetEffectiveLevel(PassiveType.MaxHp);
        HpRegen = 0.5f * GetEffectiveLevel(PassiveType.HpRegen) + _baseHpRegen;
        CritMultiplier = _baseCritMultiplier + 0.15f * GetEffectiveLevel(PassiveType.CritDamage);
        ProjectileCount = _baseProjectileCount + Mathf.FloorToInt(GetEffectiveLevel(PassiveType.ProjectileCount) / 3f);

        // DR (Diminishing Returns) — final = cap × (1 - (1-perLevel)^effLv)
        DodgeChance = DiminishingReturns(GetEffectiveLevel(PassiveType.Dodge), 0.08f, 0.60f);
        CritChance = DiminishingReturns(GetEffectiveLevel(PassiveType.CritChance), 0.08f, 0.75f);
        LuckChance = DiminishingReturns(GetEffectiveLevel(PassiveType.Luck), 0.10f, 0.80f);

        // Cap 가산식
        Lifesteal = Mathf.Min(0.02f * GetEffectiveLevel(PassiveType.Lifesteal), 0.25f);

        // 곱셈 + floor (공격 속도 = 발사체 속도/검 모션 속도와 연동)
        float atkMult = Mathf.Pow(0.95f, GetEffectiveLevel(PassiveType.AttackSpeed));
        AttackSpeedMultiplier = Mathf.Max(atkMult, 0.30f);
        ProjectileSpeed = 1f / AttackSpeedMultiplier;

        // 난이도
        float diffLv = GetEffectiveLevel(PassiveType.Difficulty);
        DifficultySpawnMultiplier = 1f + 0.15f * diffLv;
        DifficultyRewardMultiplier = 1f + 0.20f * diffLv;

        OnStatsChanged?.Invoke();
    }

    /// <summary>점감 공식: final = cap × (1 - (1-perLevel)^effLv)</summary>
    private static float DiminishingReturns(float effLevel, float perLevel, float cap)
    {
        if (effLevel <= 0f) return 0f;
        return cap * (1f - Mathf.Pow(1f - perLevel, effLevel));
    }
}
