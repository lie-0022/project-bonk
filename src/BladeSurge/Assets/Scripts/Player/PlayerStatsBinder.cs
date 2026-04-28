using UnityEngine;

/// <summary>
/// Player GameObject에 부착. PlayerStats 패시브 값을 HealthComponent에 동기화하고,
/// HpRegen을 매 프레임 적용한다. Dodge는 HealthComponent.DodgeChance에 그대로 전달.
/// 출처: design/gdd/spec-system.md
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerStatsBinder : MonoBehaviour
{
    private HealthComponent _health;
    private PlayerStats _stats;
    private float _regenAccumulator;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _stats = PlayerStats.Instance;
        if (_stats == null)
        {
            Debug.LogWarning("[PlayerStatsBinder] PlayerStats.Instance is null. 동기화 비활성.");
            return;
        }

        ApplyStats(initialFullHeal: true);
    }

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += OnStatsChanged;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsChanged -= OnStatsChanged;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        // 스테이지 클리어 등 비-Playing 진입 시 누적기 리셋
        if (state != GameState.Playing) _regenAccumulator = 0f;
    }

    private void OnStatsChanged()
    {
        ApplyStats(initialFullHeal: false);
    }

    private void ApplyStats(bool initialFullHeal)
    {
        if (_stats == null || _health == null) return;
        _health.SetMaxHp(_stats.MaxHp, initialFullHeal);
        _health.DodgeChance = _stats.DodgeChance;
    }

    private void Update()
    {
        if (_stats == null || _health == null || !_health.IsAlive) return;

        float regen = _stats.HpRegen;
        if (regen <= 0f) return;

        _regenAccumulator += regen * Time.deltaTime;
        if (_regenAccumulator >= 1f)
        {
            float whole = Mathf.Floor(_regenAccumulator);
            _health.Heal(whole);
            _regenAccumulator -= whole;
        }
    }
}
