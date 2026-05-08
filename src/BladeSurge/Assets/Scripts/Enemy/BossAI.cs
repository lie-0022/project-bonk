using UnityEngine;

/// <summary>
/// 보스 AI. MVP: 단순 추적 + 접촉 데미지.
/// BossEnemy와 한 GameObject에 함께 부착되며, EnemyBase 의 _isActive/_playerTransform 등 상태를 사용한다.
/// 추후 AOE/페이즈 추가 시 본 컴포넌트를 확장하거나 별도 행동 트리로 교체.
/// </summary>
[RequireComponent(typeof(BossEnemy))]
public class BossAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 2.0f;

    [Header("Contact Damage")]
    [SerializeField] private float _contactDamage = 25f;
    [SerializeField] private float _contactInterval = 0.8f;
    [SerializeField] private float _contactRadius = 2.5f;

    private BossEnemy _boss;
    private Rigidbody _rb;
    private Transform _player;
    private HealthComponent _playerHealth;
    private float _contactTimer;

    private void Awake()
    {
        _boss = GetComponent<BossEnemy>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 활성 상태 / 플레이어 참조는 BossEnemy(EnemyBase) 가 관리.
        // 보스도 EnemyBase.ActiveEnemies 에 등록되어 있으므로 player를 매 프레임 다시 찾기보다
        // BossEnemy의 활성 상태를 그대로 따른다.
        EnsurePlayerRef();
        if (_player == null) return;

        // 일시정지/사망/타임스톱 중에는 정지
        if (DropItemEffects.TimeStopActive)
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            return;
        }
        if (_boss.Health == null || !_boss.Health.IsAlive)
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            return;
        }

        MoveTowardPlayer();
        HandleContactDamage();
    }

    private void EnsurePlayerRef()
    {
        if (_player != null) return;
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo == null) return;
        _player = playerGo.transform;
        _playerHealth = playerGo.GetComponent<HealthComponent>();
    }

    private void MoveTowardPlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        if (_rb != null)
            _rb.linearVelocity = new Vector3(dir.x * _moveSpeed, _rb.linearVelocity.y, dir.z * _moveSpeed);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
    }

    private void HandleContactDamage()
    {
        if (_playerHealth == null) return;

        float dx = transform.position.x - _player.position.x;
        float dz = transform.position.z - _player.position.z;
        float sqrDist = dx * dx + dz * dz;

        if (sqrDist < _contactRadius * _contactRadius)
        {
            _contactTimer -= Time.deltaTime;
            if (_contactTimer <= 0f)
            {
                DamageDealer.Deal(
                    new DamageInfo(_contactDamage, DamageSource.Enemy, gameObject),
                    _playerHealth);
                _contactTimer = _contactInterval;
            }
        }
        else
        {
            _contactTimer = 0f;
        }
    }
}
