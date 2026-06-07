using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 무기 슬롯 HUD. 무기 아이콘 + 레벨을 표시.
/// WeaponSystem.OnWeaponsChanged 이벤트 구독으로 갱신.
/// 아이콘은 WeaponDataSO.Icon(등급 무관 대표 아이콘) 사용. 등급별 아이콘은 레벨업 카드용.
/// </summary>
public class WeaponSlotsHUD : MonoBehaviour
{
    [System.Serializable]
    private struct SlotEntry
    {
        public GameObject Root;
        public Image Icon;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private SlotEntry[] _slots = new SlotEntry[3];

    private void OnEnable() => WeaponSystem.OnWeaponsChanged += Refresh;
    private void OnDisable() => WeaponSystem.OnWeaponsChanged -= Refresh;
    private void Start() => Refresh();

    private void Refresh()
    {
        var system = WeaponSystem.Instance;
        var equipped = system != null ? system.GetEquippedWeapons() : null;
        int equippedCount = equipped != null ? equipped.Count : 0;

        for (int i = 0; i < _slots.Length; i++)
        {
            var entry = _slots[i];
            bool active = i < equippedCount && equipped[i] != null && equipped[i].Data != null;

            if (entry.Root != null) entry.Root.SetActive(active);
            if (!active) continue;

            var slot = equipped[i];
            if (entry.Icon != null) entry.Icon.sprite = GetGradeIcon(slot);
            if (entry.Label != null) entry.Label.text = $"Lv {slot.Level}";
        }
    }

    /// <summary>무기 레벨 구간(마일스톤)에 해당하는 등급 아이콘. 없으면 기본 Icon으로 폴백.</summary>
    private static Sprite GetGradeIcon(WeaponSlot slot)
    {
        var icons = slot.Data.GradeIcons;
        if (icons != null && icons.Length > 0)
        {
            int idx = Mathf.Clamp(WeaponDataSO.GetMilestoneIndex(slot.Level), 0, icons.Length - 1);
            if (icons[idx] != null) return icons[idx];
        }
        return slot.Data.Icon;
    }
}
