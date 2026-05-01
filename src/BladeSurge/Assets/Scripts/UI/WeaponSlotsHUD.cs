using TMPro;
using UnityEngine;

/// <summary>
/// 보유 무기 슬롯 HUD. 무기 이름 + 레벨을 텍스트로 표시.
/// WeaponSystem.OnWeaponsChanged 이벤트 구독으로 갱신.
/// MVP 최소 구현 — 아이콘/등급색/쿨다운 등은 추후 단계.
/// </summary>
public class WeaponSlotsHUD : MonoBehaviour
{
    [System.Serializable]
    private struct SlotEntry
    {
        public GameObject Root;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private SlotEntry[] _slots = new SlotEntry[3];

    private void OnEnable()
    {
        WeaponSystem.OnWeaponsChanged += Refresh;
    }

    private void OnDisable()
    {
        WeaponSystem.OnWeaponsChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        var system = WeaponSystem.Instance;
        var equipped = system != null ? system.GetEquippedWeapons() : null;
        int equippedCount = equipped != null ? equipped.Count : 0;

        for (int i = 0; i < _slots.Length; i++)
        {
            var entry = _slots[i];
            bool active = i < equippedCount && equipped[i] != null && equipped[i].Data != null;

            if (entry.Root != null)
                entry.Root.SetActive(active);

            if (active && entry.Label != null)
            {
                var slot = equipped[i];
                entry.Label.text = $"{slot.Data.WeaponName} Lv {slot.Level}";
            }
        }
    }
}
