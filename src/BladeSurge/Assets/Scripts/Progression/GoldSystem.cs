using System;
using UnityEngine;

/// <summary>
/// 골드 획득·관리 싱글턴. 적 사망 시 타입별 골드를 지급하고 OnGoldChanged를 브로드캐스트한다.
/// </summary>
public class GoldSystem : MonoBehaviour, IInitializable
{
    public static GoldSystem Instance { get; private set; }

    [Header("Gold Rewards")]
    [SerializeField] private int _chaserGold = 5;
    [SerializeField] private int _rusherGold = 10;

    /// <summary>골드 변경 시 브로드캐스트. (currentGold) — TopBarUI가 구독한다.</summary>
    public static event Action<int> OnGoldChanged;

    public int CurrentGold { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private Transform _playerTransform;

    public void Initialize()
    {
        CurrentGold = 0;
        _playerTransform = GameObject.FindWithTag("Player")?.transform;
        EnemyBase.OnEnemyDied += OnEnemyDied;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    private void OnDestroy()
    {
        EnemyBase.OnEnemyDied -= OnEnemyDied;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        int baseReward = xpReward >= 20f ? _rusherGold : _chaserGold;
        float rewardMult = PlayerStats.Instance != null
            ? PlayerStats.Instance.DifficultyRewardMultiplier
            : 1f;
        int reward = Mathf.RoundToInt(baseReward * rewardMult);
        SpawnGoldOrb(reward, position);
    }

    private void SpawnGoldOrb(int goldAmount, Vector3 position)
    {
        GameObject obj = PickupPool.Instance.GetGoldOrb();
        if (obj == null) return;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
        obj.transform.position = position + new Vector3(offset.x, 0.3f, offset.y);
        obj.GetComponent<GoldOrb>()?.Setup(goldAmount, _playerTransform);
    }

    /// <summary>골드를 추가한다. 외부 시스템(상자, 이벤트)에서도 호출 가능.</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    /// <summary>골드를 소비한다. 잔액 부족 시 false 반환.</summary>
    public bool SpendGold(int amount)
    {
        if (amount > CurrentGold) return false;
        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }
}
