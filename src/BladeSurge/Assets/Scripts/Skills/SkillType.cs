/// <summary>
/// 스킬 고유 식별자. 슬롯 배정 및 중복 체크에 사용.
/// </summary>
public enum SkillType
{
    None = 0,

    // 검사 스킬
    DashSlash,
    Whirlwind,
    BladeBeam,
    Execute,

    // 스탯 강화 (레벨업 선택지에서 스킬과 함께 등장)
    StatAttackSpeed,
    StatMoveSpeed,
    StatMaxHp,
    StatDamage,
}
