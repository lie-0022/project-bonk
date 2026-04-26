using UnityEngine;

/// <summary>
/// 스킬 정의 데이터 계약 (ScriptableObject가 구현).
/// 수치 데이터만 포함. 발동 로직은 SkillSystem에서 처리한다. (ADR-0007)
/// </summary>
public interface ISkillData
{
    /// <summary>스킬 고유 식별자. 슬롯 배정 및 중복 체크용.</summary>
    SkillType SkillType { get; }

    /// <summary>기본 쿨타임(초).</summary>
    float BaseCooldown { get; }

    /// <summary>기본 피해량.</summary>
    float BaseDamage { get; }

    /// <summary>스킬 아이콘. UI 표시용.</summary>
    Sprite Icon { get; }
}
