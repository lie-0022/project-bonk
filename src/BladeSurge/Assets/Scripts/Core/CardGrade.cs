/// <summary>
/// 레벨업·상자 카드 등장 시 결정되는 등급.
/// 등급에 따라 무기 데미지·공속 증가 폭이 차등 적용된다.
/// 출처: design/gdd/weapon-system.md, design/gdd/levelup-selection.md
/// </summary>
public enum CardGrade
{
    Common = 0,
    Epic,
    Unique,
    Legend,
}

/// <summary>등급별 무기 강화 보너스 정의. GDD 표와 일치.</summary>
public static class GradeBonus
{
    public readonly struct Entry
    {
        public readonly float DamageBonus;       // 데미지 +bonus (+0.12 = +12%)
        public readonly float IntervalReduction; // 공격 간격 -reduction (-0.06 = 간격 6% 감소)
        public Entry(float damage, float intervalReduction)
        {
            DamageBonus = damage;
            IntervalReduction = intervalReduction;
        }
    }

    public static Entry Get(CardGrade grade) => grade switch
    {
        CardGrade.Common => new Entry(0.12f, 0.06f),
        CardGrade.Epic   => new Entry(0.20f, 0.10f),
        CardGrade.Unique => new Entry(0.32f, 0.15f),
        CardGrade.Legend => new Entry(0.50f, 0.22f),
        _                => new Entry(0f, 0f),
    };
}
