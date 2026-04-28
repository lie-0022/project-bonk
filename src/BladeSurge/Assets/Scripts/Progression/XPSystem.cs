using System;
using UnityEngine;

/// <summary>
/// 경험치 획득·레벨업을 관리하는 싱글턴 시스템.
/// 적 사망 시 XP 오브를 스폰하고, 플레이어가 오브를 수집하면 AddXP()를 통해 XP가 적용된다.
/// </summary>
public class XPSystem : MonoBehaviour, IInitializable
{
    public static XPSystem Instance { get; private set; }

    [Header("XP Settings")]
    [SerializeField] private float _baseXP = 100f;
    [SerializeField] private float _xpPerLevelIncrease = 50f;

    /// <summary>레벨업 시 발생. (currentLevel) — 무기 선택 UI 등이 구독한다.</summary>
    public static event Action<int> OnLevelUp;

    /// <summary>XP 변화 시 발생. (currentXP, xpToNextLevel, level) — HUD XP 게이지용.</summary>
    public static event Action<float, float, int> OnXPChanged;

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentXP { get; private set; }
    public float XPToNextLevel { get; private set; }

    /// <summary>현재 레벨 XP 진행률 [0~1]. HUD 게이지용.</summary>
    public float XPProgress => XPToNextLevel > 0f ? CurrentXP / XPToNextLevel : 0f;

    private Transform _playerTransform;
    private bool _isActive;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize()
    {
        CurrentLevel = 1;
        CurrentXP = 0f;
        XPToNextLevel = CalcXPToNextLevel(CurrentLevel);

        _playerTransform = GameObject.FindWithTag("Player")?.transform;

        EnemyBase.OnEnemyDied += OnEnemyDied;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        EnemyBase.OnEnemyDied -= OnEnemyDied;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _isActive = state == GameState.Playing;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        if (!_isActive || xpReward <= 0f) return;

        float rewardMult = PlayerStats.Instance != null
            ? PlayerStats.Instance.DifficultyRewardMultiplier
            : 1f;
        SpawnXPOrb(xpReward * rewardMult, position);
    }

    private void SpawnXPOrb(float xpReward, Vector3 position)
    {
        GameObject obj = PickupPool.Instance.GetXPOrb();
        if (obj == null) return;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
        obj.transform.position = position + new Vector3(offset.x, 0.3f, offset.y);
        obj.GetComponent<XPOrb>()?.Setup(xpReward, _playerTransform);
    }

    /// <summary>XPOrb가 수집될 때 호출한다.</summary>
    public void AddXP(float amount)
    {
        if (amount <= 0f) return;

        CurrentXP += amount;

        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = CalcXPToNextLevel(CurrentLevel);

            Debug.Log($"[XPSystem] 레벨업! Lv {CurrentLevel} (다음 레벨까지 {XPToNextLevel} XP)");
            OnLevelUp?.Invoke(CurrentLevel);
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel, CurrentLevel);
    }

    private float CalcXPToNextLevel(int level)
    {
        return _baseXP + (level - 1) * _xpPerLevelIncrease;
    }
}
