using UnityEngine;

/// <summary>
/// 카드 등장 시 등급을 추첨하는 정적 헬퍼.
/// 기본 가중치 + 행운 패시브 보정을 적용한다.
/// 출처: design/gdd/levelup-selection.md
/// </summary>
public static class CardGradeRoller
{
    // 기본 등장 확률 (LuckChance = 0 기준). 합 = 1.0
    private const float BaseCommon = 0.60f;
    private const float BaseEpic   = 0.25f;
    private const float BaseUnique = 0.12f;
    private const float BaseLegend = 0.03f;

    /// <summary>
    /// 행운(0~0.80)을 반영해 등급을 추첨.
    /// 행운만큼 Common 비중이 줄어들고, 줄어든 비중은 상위 등급(Epic/Unique/Legend)에
    /// 기본 비율대로 분배된다. 행운 0.5 = Common 절반이 상위 등급으로 이동.
    /// </summary>
    public static CardGrade Roll(float luckChance)
    {
        luckChance = Mathf.Clamp01(luckChance);

        float commonW = BaseCommon * (1f - luckChance);
        float shifted = BaseCommon * luckChance;

        // 상위 등급 기본 비율로 shifted 분배
        float upperTotal = BaseEpic + BaseUnique + BaseLegend; // 0.40
        float epicW   = BaseEpic   + shifted * (BaseEpic   / upperTotal);
        float uniqueW = BaseUnique + shifted * (BaseUnique / upperTotal);
        float legendW = BaseLegend + shifted * (BaseLegend / upperTotal);

        return SampleByWeight(commonW, epicW, uniqueW, legendW);
    }

    /// <summary>현재 PlayerStats의 행운 값으로 즉시 추첨.</summary>
    public static CardGrade Roll() =>
        Roll(PlayerStats.Instance != null ? PlayerStats.Instance.LuckChance : 0f);

    private static CardGrade SampleByWeight(float c, float e, float u, float l)
    {
        float total = c + e + u + l;
        float roll = Random.value * total;

        if (roll < c) return CardGrade.Common;
        roll -= c;
        if (roll < e) return CardGrade.Epic;
        roll -= e;
        if (roll < u) return CardGrade.Unique;
        return CardGrade.Legend;
    }

    /// <summary>디버그용: 현재 행운 기준 각 등급의 확률을 문자열로 반환.</summary>
    public static string DebugProbabilities(float luckChance)
    {
        luckChance = Mathf.Clamp01(luckChance);
        float commonW = BaseCommon * (1f - luckChance);
        float shifted = BaseCommon * luckChance;
        float upperTotal = BaseEpic + BaseUnique + BaseLegend;
        float epicW   = BaseEpic   + shifted * (BaseEpic   / upperTotal);
        float uniqueW = BaseUnique + shifted * (BaseUnique / upperTotal);
        float legendW = BaseLegend + shifted * (BaseLegend / upperTotal);
        return $"Luck={luckChance:P0} → C={commonW:P1} E={epicW:P1} U={uniqueW:P1} L={legendW:P1}";
    }
}
