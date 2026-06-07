using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 패시브 슬롯 HUD. 패시브 아이콘 + 레벨을 표시.
/// PlayerStats.OnStatsChanged 이벤트 구독으로 갱신.
/// 슬롯 3개 고정 (GDD: spec-system.md). 4종 이상은 앞 3개만 노출.
/// </summary>
public class PassiveSlotsHUD : MonoBehaviour
{
    [Serializable]
    private struct SlotEntry
    {
        public GameObject Root;
        public Image Icon;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private SlotEntry[] _slots = new SlotEntry[3];

    [Tooltip("PassiveType enum 순서대로 아이콘: MaxHp, HpRegen, Dodge, AttackSpeed, CritChance, CritDamage, Lifesteal, ProjectileCount, MoveSpeed, ExtraJump, Luck, Difficulty")]
    [SerializeField] private Sprite[] _passiveIcons;

    private static readonly PassiveType[] s_passiveOrder = (PassiveType[])Enum.GetValues(typeof(PassiveType));

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += Refresh;
        Refresh(); // 활성화 시(레벨업 창 열림 등)마다 현재 상태로 갱신
    }
    private void OnDisable() => PlayerStats.OnStatsChanged -= Refresh;

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
                if (entry.Icon != null) entry.Icon.sprite = GetIcon(type);
                if (entry.Label != null) entry.Label.text = $"Lv {level}";
                filled++;
            }
        }

        for (int i = filled; i < _slots.Length; i++)
            if (_slots[i].Root != null) _slots[i].Root.SetActive(false);
    }

    /// <summary>PassiveType enum 값(=배열 인덱스)에 해당하는 아이콘 반환.</summary>
    private Sprite GetIcon(PassiveType type)
    {
        int idx = (int)type;
        return (_passiveIcons != null && idx >= 0 && idx < _passiveIcons.Length) ? _passiveIcons[idx] : null;
    }
}
