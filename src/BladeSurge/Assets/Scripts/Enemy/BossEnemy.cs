using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 적. EnemyBase를 상속하지만 풀을 사용하지 않고, 사망 시 1초 연출 후 Win 상태로 전환한다.
///
/// MVP: 단순 추적 + 접촉 데미지만. 추후 AOE/페이즈 추가 예정.
/// </summary>
[RequireComponent(typeof(BossAI))]
public class BossEnemy : EnemyBase
{
    /// <summary>보스 등장 시 브로드캐스트. BossHpBarUI가 구독.</summary>
    public static event Action<BossEnemy> OnBossSpawned;

    /// <summary>보스 사망 시 브로드캐스트. (1초 연출 전) — 외부 시스템 알림용.</summary>
    public static event Action<BossEnemy> OnBossDied;

    [Header("Boss Identity")]
    [SerializeField] private string _bossName = "BOSS";
    [SerializeField] private Color _hpBarColor = new(0.85f, 0.2f, 0.2f, 1f);

    [Header("Boss Reward")]
    [SerializeField] private int _goldReward = 200;

    [Header("Death Sequence")]
    [SerializeField] private float _deathDelay = 1.0f;

    public string BossName => _bossName;
    public Color HpBarColor => _hpBarColor;
    public HealthComponent Health => _health;

    public override EnemyType EnemyType => EnemyType.Boss;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>Activate 시 서브클래스 초기화 — 보스는 BossAI가 별도 컴포넌트로 동작하므로 빈 구현.</summary>
    protected override void OnActivate()
    {
        // BossAI가 _isActive 상태와 player 참조를 BossEnemy 베이스로부터 읽음
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        OnBossSpawned?.Invoke(this);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
    }

    /// <summary>
    /// 사망 처리 오버라이드: 풀 반환 대신 1초 연출 후 Win.
    /// 골드 보상도 직접 지급 (xpReward는 0 — Win이라 의미 없음).
    /// </summary>
    protected override void HandleDeath(float xpReward)
    {
        Deactivate();
        OnBossDied?.Invoke(this);

        // 골드 보상 직접 지급 (오브가 아니라 즉시)
        if (GoldSystem.Instance != null)
            GoldSystem.Instance.AddGold(_goldReward);

        // 처치 수 통계에 보스도 포함 (RunStats가 OnEnemyDied 구독)
        FireEnemyDied(0f, transform.position);

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 사망 연출 시간 — VFX/사운드 후속 시 여기에 추가
        yield return new WaitForSecondsRealtime(_deathDelay);

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Win);

        // GameManager.LoadResultsAfterDelay 코루틴이 GameOver 씬 로드 → 그때 본 GameObject도 정리됨
        gameObject.SetActive(false);
    }
}
