using System;
using UnityEngine;

/// <summary>
/// 상자 구매 카운트·가격 계산·구매 처리 싱글턴.
/// 가격: floor(BaseCost * Multiplier^purchases). 출처: design/gdd/chest-system.md
/// </summary>
public class ChestSystem : MonoBehaviour
{
    public static ChestSystem Instance { get; private set; }

    [Header("Pricing")]
    [SerializeField] private int _baseCost = 50;
    [SerializeField] private float _costMultiplier = 1.5f;

    public int Purchases { get; private set; }

    /// <summary>구매 카운트 변경 시 발동. (purchases, nextCost)</summary>
    public static event Action<int, int> OnPurchaseCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Purchases = 0;
        OnPurchaseCountChanged?.Invoke(Purchases, GetNextCost());
    }

    /// <summary>다음 상자 구매 비용.</summary>
    public int GetNextCost()
    {
        return Mathf.FloorToInt(_baseCost * Mathf.Pow(_costMultiplier, Purchases));
    }

    /// <summary>구매 시도. 골드 차감 + 선택 UI 트리거. 성공 시 true.</summary>
    public bool TryPurchase()
    {
        var gold = GoldSystem.Instance;
        var levelup = LevelupWeaponSelection.Instance;
        if (gold == null || levelup == null) return false;
        if (levelup.IsSelecting) return false;

        int cost = GetNextCost();
        if (!gold.SpendGold(cost)) return false;

        Purchases++;
        OnPurchaseCountChanged?.Invoke(Purchases, GetNextCost());
        levelup.RequestSelection();
        return true;
    }
}
