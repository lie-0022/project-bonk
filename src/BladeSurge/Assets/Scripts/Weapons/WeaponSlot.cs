using UnityEngine;

/// <summary>
/// 개별 무기 슬롯. 타이머·레벨·등급 누적 멀티플라이어를 관리한다.
/// Damage·Interval은 매 레벨 등급별 곱셈 누적, 범위·각도·관통·폭발반경은 마일스톤 테이블 기반.
/// 출처: design/gdd/weapon-system.md
/// </summary>
public class WeaponSlot
{
    public WeaponDataSO Data { get; private set; }
    public int Level { get; private set; } = 1;

    // 누적 멀티플라이어 (등급 적용 결과)
    private float _damageMultiplier = 1f;
    private float _intervalMultiplier = 1f;

    // 최종 캐싱 (RecalculateFinalStats에서 갱신)
    public float FinalDamage { get; private set; }
    public float FinalInterval { get; private set; }
    public float FinalRange { get; private set; }
    public float FinalSweepAngle { get; private set; }
    public int FinalPierce { get; private set; }
    public float FinalExplosionRadius { get; private set; }

    private float _cooldownRemaining;

    public float CooldownNormalized =>
        FinalInterval > 0f ? Mathf.Clamp01(_cooldownRemaining / FinalInterval) : 0f;

    public WeaponSlot(WeaponDataSO data, float attackSpeedMultiplier)
    {
        Data = data;
        RecalculateFinalStats(attackSpeedMultiplier);
        _cooldownRemaining = 0f;
    }

    /// <summary>레벨업. 등급별 데미지·공속 보너스 곱셈 누적 + 마일스톤 효과 적용.</summary>
    public void Upgrade(CardGrade grade, float attackSpeedMultiplier)
    {
        if (Level >= PlayerStats.MaxPassiveLevel) return; // 동일한 max 사용 (15)
        Level++;
        var bonus = GradeBonus.Get(grade);
        _damageMultiplier *= 1f + bonus.DamageBonus;
        _intervalMultiplier *= 1f - bonus.IntervalReduction;
        RecalculateFinalStats(attackSpeedMultiplier);
    }

    /// <summary>패시브 공격 속도 변경 등 외부 요인으로 interval만 갱신해야 할 때 호출.</summary>
    public void UpdateInterval(float attackSpeedMultiplier)
    {
        RecalculateFinalStats(attackSpeedMultiplier);
    }

    private void RecalculateFinalStats(float playerAttackSpeedMultiplier)
    {
        FinalDamage = Data.BaseDamage * _damageMultiplier;
        FinalInterval = Data.BaseAttackInterval * _intervalMultiplier * playerAttackSpeedMultiplier;
        FinalRange = Data.GetRangeAt(Level);
        FinalSweepAngle = Data.GetSweepAngleAt(Level);
        FinalPierce = Data.GetPierceAt(Level);
        FinalExplosionRadius = Data.GetExplosionRadiusAt(Level);
    }

    public void Tick(float deltaTime, WeaponSystem system)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
            return;
        }

        if (EnemyBase.ActiveEnemies.Count == 0) return;

        system.ExecuteWeapon(this);
        _cooldownRemaining = FinalInterval;
    }
}
