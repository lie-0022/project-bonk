using UnityEngine;

/// <summary>
/// 모든 적 AI의 공통 기반 클래스.
/// 체력 초기화, 사망 처리, 풀 반환, 게임 상태 연동을 담당한다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HealthComponent))]
public abstract class EnemyBase : MonoBehaviour, IPoolable, IEnemyController
{
    /// <summary>현재 활성화된 모든 적 목록. BasicAttack 탐색에 사용.</summary>
    public static readonly System.Collections.Generic.List<EnemyBase> ActiveEnemies = new();

    /// <summary>적 사망 시 브로드캐스트. XPSystem, GoldSystem 등이 구독한다. (xpReward, position)</summary>
    public static event System.Action<float, Vector3> OnEnemyDied;

    [SerializeField] private float _spawnInvincibilityDuration = 0.5f;

    protected Rigidbody _rb;
    protected HealthComponent _health;
    protected Transform _playerTransform;
    protected HealthComponent _playerHealth;
    protected bool _isActive;
    protected float _speedMultiplier = 1f;

    private float _spawnInvincibilityTimer;

    public abstract EnemyType EnemyType { get; }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _health = GetComponent<HealthComponent>();
    }

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>풀에서 꺼낸 직후 호출. 체력 초기화 및 사망 이벤트 등록.</summary>
    public virtual void OnSpawn()
    {
        _health.ResetHealth();
        _health.OnDeath += HandleDeath;

        _spawnInvincibilityTimer = _spawnInvincibilityDuration;
        _health.IsInvincible = true;
    }

    /// <summary>풀에 반환하기 직전 호출. 이벤트 구독 해제 및 물리 정지.</summary>
    public virtual void OnDespawn()
    {
        _health.OnDeath -= HandleDeath;
        _isActive = false;
        _rb.linearVelocity = Vector3.zero;
    }

    // ── IEnemyController ─────────────────────────────────────

    /// <summary>
    /// 웨이브 배율을 적용한다. Activate 전에 호출해야 한다.
    /// </summary>
    public void ApplyWaveScale(float hpMultiplier, float speedMultiplier)
    {
        _health.ApplyHpMultiplier(hpMultiplier);
        _speedMultiplier = speedMultiplier;
    }

    /// <summary>OnSpawn 이후 WaveSpawner가 호출. 플레이어 참조를 받아 AI를 시작한다.</summary>
    public void Activate(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _playerHealth = playerTransform.GetComponent<HealthComponent>();
        _isActive = true;

        ActiveEnemies.Add(this);
        GameManager.OnGameStateChanged += OnGameStateChanged;
        OnActivate();
    }

    /// <summary>AI를 즉시 정지한다. 게임 상태 변화 또는 사망 시 호출.</summary>
    public void Deactivate()
    {
        _isActive = false;
        _rb.linearVelocity = Vector3.zero;
        ActiveEnemies.Remove(this);
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    // ── 서브클래스 훅 ─────────────────────────────────────────

    /// <summary>Activate 시 서브클래스별 초기화. 상태 리셋 등을 여기에 구현한다.</summary>
    protected abstract void OnActivate();

    // ── 내부 처리 ─────────────────────────────────────────────

    protected virtual void Update()
    {
        // 스폰 무적 타이머
        if (_spawnInvincibilityTimer > 0f)
        {
            _spawnInvincibilityTimer -= Time.deltaTime;
            if (_spawnInvincibilityTimer <= 0f)
                _health.IsInvincible = false;
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        _isActive = state == GameState.Playing;
        if (!_isActive)
            _rb.linearVelocity = Vector3.zero;
    }

    private void HandleDeath(float xpReward)
    {
        OnEnemyDied?.Invoke(xpReward, transform.position);
        Deactivate();
        ObjectPool.Instance.ReturnToPool(gameObject, EnemyType);
    }
}
