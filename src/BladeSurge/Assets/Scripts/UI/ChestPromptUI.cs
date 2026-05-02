using TMPro;
using UnityEngine;

/// <summary>
/// 가장 가까운 상자에 대한 "E로 열기 (XXG)" 프롬프트.
/// Chest.OnNearestChanged 이벤트 구독으로 갱신.
/// </summary>
public class ChestPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _label;

    private void OnEnable()
    {
        Chest.OnNearestChanged += OnNearestChanged;
        ChestSystem.OnPurchaseCountChanged += OnCostChanged;
    }

    private void OnDisable()
    {
        Chest.OnNearestChanged -= OnNearestChanged;
        ChestSystem.OnPurchaseCountChanged -= OnCostChanged;
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void OnNearestChanged(Chest chest, int cost)
    {
        if (chest == null) { SetVisible(false); return; }
        UpdateLabel(cost);
        SetVisible(true);
    }

    private void OnCostChanged(int purchases, int nextCost)
    {
        // 가까운 상자가 있는 동안만 갱신
        if (_canvasGroup != null && _canvasGroup.alpha > 0f)
            UpdateLabel(nextCost);
    }

    private void UpdateLabel(int cost)
    {
        if (_label == null) return;
        _label.text = $"E 로 열기 ({cost} G)";
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }
}
