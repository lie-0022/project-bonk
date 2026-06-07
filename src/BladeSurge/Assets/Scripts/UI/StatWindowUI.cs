using TMPro;
using UnityEngine;

/// <summary>
/// 레벨업/상자 선택 화면 우측의 상시표시 스탯 창. PlayerStats 최종값을 텍스트로 표시.
/// Tab 토글식 StatsPanelUI와 달리, 패널이 보이는 동안 항상 갱신된다.
/// </summary>
public class StatWindowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _statsText;

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_statsText == null) return;
        var s = PlayerStats.Instance;
        if (s == null) { _statsText.text = ""; return; }

        _statsText.text =
            $"이동 속도\t{s.MoveSpeed:F2}\n" +
            $"추가 점프\t{s.ExtraJumps}\n" +
            $"최대 HP\t{s.MaxHp:F0}\n" +
            $"HP 재생\t{s.HpRegen:F1} /s\n" +
            $"회피 확률\t{s.DodgeChance:P0}\n" +
            $"공격 속도\t{s.AttackSpeedMultiplier:F2}\n" +
            $"발사체 속도\t{s.ProjectileSpeed:F2}\n" +
            $"치명타 확률\t{s.CritChance:P0}\n" +
            $"치명타 배율\t{s.CritMultiplier:F2}\n" +
            $"생명 흡수\t{s.Lifesteal:P0}\n" +
            $"발사체 수\t{s.ProjectileCount}\n" +
            $"행운\t{s.LuckChance:P0}\n" +
            $"난이도(스폰)\t{s.DifficultySpawnMultiplier:F2}\n" +
            $"난이도(보상)\t{s.DifficultyRewardMultiplier:F2}";
    }
}
