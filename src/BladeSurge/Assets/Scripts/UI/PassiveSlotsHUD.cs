using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 보유 패시브 슬롯 HUD. 패시브명(약어) + 레벨을 텍스트로 표시.
/// PlayerStats.OnStatsChanged 이벤트 구독으로 갱신.
/// MVP 최소 구현 — 슬롯 3개 고정 (GDD: spec-system.md 슬롯 제한). 4종 이상은 앞 3개만 노출.
/// </summary>
public class PassiveSlotsHUD : MonoBehaviour
{
    [Serializable]
    private struct SlotEntry
    {
        public GameObject Root;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private SlotEntry[] _slots = new SlotEntry[3];

    private static readonly PassiveType[] s_passiveOrder = (PassiveType[])Enum.GetValues(typeof(PassiveType));

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += Refresh;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        var stats = PlayerStats.Instance;
        int filled = 0;

        if (stats != null)
        {
            for (int i = 0; i < s_passiveOrder.Length && filled < _slots.Length; i++)
            {
                var type = s_passiveOrder[i];
                int level = stats.GetPassiveLevel(type);
                if (level <= 0) continue;

                var entry = _slots[filled];
                if (entry.Root != null) entry.Root.SetActive(true);
                if (entry.Label != null) entry.Label.text = $"{GetShortName(type)} Lv {level}";
                filled++;
            }
        }

        for (int i = filled; i < _slots.Length; i++)
        {
            if (_slots[i].Root != null) _slots[i].Root.SetActive(false);
        }
    }

    private static string GetShortName(PassiveType type) => type switch
    {
        PassiveType.MaxHp => "체력",
        PassiveType.HpRegen => "재생",
        PassiveType.Dodge => "회피",
        PassiveType.AttackSpeed => "공속",
        PassiveType.CritChance => "치확",
        PassiveType.CritDamage => "치명",
        PassiveType.Lifesteal => "흡혈",
        PassiveType.ProjectileCount => "발사체",
        PassiveType.MoveSpeed => "이속",
        PassiveType.ExtraJump => "점프",
        PassiveType.Luck => "행운",
        PassiveType.Difficulty => "난이도",
        _ => type.ToString(),
    };
}
