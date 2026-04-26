using UnityEngine;

/// <summary>플레이어 공격인지 적 공격인지 구분. 아군 피해 방지에 사용.</summary>
public enum DamageSource { Player, Enemy }

/// <summary>피해 정보를 담는 구조체. 모든 피해는 이 구조체로 표현된다.</summary>
public struct DamageInfo
{
    public float Amount;
    public DamageSource Source;
    public GameObject Instigator;

    public DamageInfo(float amount, DamageSource source, GameObject instigator)
    {
        Amount = Mathf.Min(amount, 9999f);
        Source = source;
        Instigator = instigator;
    }
}

/// <summary>
/// 피해를 대상에게 적용하는 stateless 유틸리티.
/// 스킬, 기본 공격, 적 AI 모두 Deal()을 호출한다.
/// </summary>
public static class DamageDealer
{
    /// <summary>
    /// DamageInfo를 target에 적용한다.
    /// DamageSource와 타겟 태그가 맞지 않으면 아군 피해로 간주해 무시한다.
    /// </summary>
    public static void Deal(DamageInfo info, HealthComponent target)
    {
        if (target == null || info.Amount <= 0f) return;

        // 팀 구분 — 플레이어 공격은 Enemy에게만, 적 공격은 Player에게만
        if (info.Source == DamageSource.Player && !target.CompareTag("Enemy")) return;
        if (info.Source == DamageSource.Enemy  && !target.CompareTag("Player")) return;

        target.TakeDamage(info.Amount);
    }
}
