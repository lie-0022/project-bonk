using UnityEngine;

/// <summary>
/// 플레이어 자동 공격. 매 attackInterval마다 범위 내 가장 가까운 적에게 피해를 준다.
/// 입력 없이 자동 발동되며 이동·스킬과 완전히 독립적으로 동작한다.
/// </summary>
public class BasicAttack : MonoBehaviour
{
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackInterval = 1.0f;
    [SerializeField] private float _attackRange = 5.0f;

    /// <summary>공격 속도 버프 스킬이 이 값을 줄인다.</summary>
    public float AttackInterval
    {
        get => _attackInterval;
        set => _attackInterval = Mathf.Max(0.1f, value);
    }

    private float _attackTimer;
    private bool _isActive;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _isActive = state == GameState.Playing;
    }

    private void Update()
    {
        if (!_isActive) return;

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            TryAttack();
            _attackTimer = _attackInterval;
        }
    }

    private void TryAttack()
    {
        EnemyBase target = FindNearestEnemy();
        if (target == null) return;

        DamageDealer.Deal(
            new DamageInfo(_attackDamage, DamageSource.Player, gameObject),
            target.GetComponent<HealthComponent>());
    }

    private EnemyBase FindNearestEnemy()
    {
        EnemyBase nearest = null;
        float nearestSqrDist = _attackRange * _attackRange;

        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) continue;

            float sqrDist = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
