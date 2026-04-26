/// <summary>
/// 스킬 슬롯의 런타임 상태 계약.
/// HUD가 SkillSystem 내부 구조 없이 쿨타임·아이콘을 읽기 위해 사용.
/// </summary>
public interface ISkillSlot
{
    /// <summary>장착된 스킬 데이터. null이면 빈 슬롯.</summary>
    ISkillData SkillData { get; }

    /// <summary>
    /// 쿨타임 진행률 (0 = 사용 가능, 1 = 방금 사용함).
    /// HUD 쿨타임 오버레이 fill 값으로 직접 사용.
    /// </summary>
    float CooldownNormalized { get; }

    /// <summary>발동 가능 여부 (스킬 존재 + 쿨타임 0 + 게임 상태 Playing).</summary>
    bool IsReady { get; }
}
