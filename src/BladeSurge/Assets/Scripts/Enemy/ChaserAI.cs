using UnityEngine;

/// <summary>
/// 추적형 적 AI. 매 프레임 플레이어 방향으로 직선 이동하며, 접촉 시 지속 피해를 준다.
/// </summary>
public class ChaserAI : EnemyBase
{
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _contactDamage = 8f;
    [SerializeField] private float _contactInterval = 0.5f;
    [SerializeField] private float _contactRadius = 1.2f;

    private float _contactTimer;

    public override EnemyType EnemyType => EnemyType.Chaser;

    protected override void OnActivate()
    {
        _contactTimer = 0f;
    }

    protected override void Update()
    {
        base.Update(); // 스폰 무적 타이머

        if (!_isActive || _playerTransform == null) return;

        MoveTowardPlayer();
        HandleContactDamage();
    }

    private void MoveTowardPlayer()
    {
        Vector3 dir = _playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        float speed = _moveSpeed * _speedMultiplier;
        _rb.linearVelocity = new Vector3(dir.x * speed, _rb.linearVelocity.y, dir.z * speed);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), 720f * Time.deltaTime);
    }

    private void HandleContactDamage()
    {
        float dx = transform.position.x - _playerTransform.position.x;
        float dz = transform.position.z - _playerTransform.position.z;
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
            _contactTimer = 0f; // 멀어지면 타이머 초기화 (다시 접촉 시 즉시 피해)
        }
    }
}
