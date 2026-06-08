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

    [Header("점프 내려치기 (옵션 — 슬라임 등)")]
    [Tooltip("켜면 주기적으로 플레이어 위치로 크게 점프해 내려치기 AOE를 한다. 슬라임 프리팹만 켤 것.")]
    [SerializeField] private bool _canJumpSlam = false;
    [Tooltip("점프 쿨다운(초).")]
    [SerializeField] private float _jumpInterval = 5f;
    [Tooltip("이 거리(m) 안에 플레이어가 있을 때만 점프 시작.")]
    [SerializeField] private float _jumpRange = 9f;
    [Tooltip("점프 정점 높이(m).")]
    [SerializeField] private float _jumpHeight = 4f;
    [Tooltip("점프(상승+하강) 총 시간(초) = 텔레그래프 시간.")]
    [SerializeField] private float _jumpDuration = 0.9f;
    [Tooltip("착지 내려치기 AOE 반경(m).")]
    [SerializeField] private float _slamRadius = 3f;
    [Tooltip("내려치기 데미지.")]
    [SerializeField] private float _slamDamage = 20f;
    [SerializeField] private GameObject _slamTelegraphPrefab;
    [SerializeField] private GameObject _slamImpactPrefab;

    private float _contactTimer;
    private float _jumpTimer;
    private bool _jumping;
    private EnemyAnimator _enemyAnimator;

    public override EnemyType EnemyType => EnemyType.Mob;

    protected override void OnActivate()
    {
        _contactTimer = 0f;
        _jumpTimer = _jumpInterval;
        _jumping = false;
        if (_rb != null) _rb.isKinematic = false;
        if (_enemyAnimator == null) _enemyAnimator = GetComponent<EnemyAnimator>();
    }

    protected override void Update()
    {
        base.Update(); // 스폰 무적 타이머

        if (_jumping) return; // 점프 중엔 코루틴이 위치를 제어
        if (!_isActive || _playerTransform == null) return;
        if (DropItemEffects.TimeStopActive)
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            return;
        }

        MoveTowardPlayer();
        HandleContactDamage();
        HandleJumpSlam();
    }

    // ── 점프 내려치기 ─────────────────────────────────────────
    private void HandleJumpSlam()
    {
        if (!_canJumpSlam) return;
        _jumpTimer -= Time.deltaTime;
        if (_jumpTimer > 0f) return;

        float dx = transform.position.x - _playerTransform.position.x;
        float dz = transform.position.z - _playerTransform.position.z;
        if (dx * dx + dz * dz > _jumpRange * _jumpRange) return; // 멀면 대기(쿨다운 유지)

        _jumpTimer = _jumpInterval;
        StartCoroutine(JumpSlamRoutine());
    }

    private System.Collections.IEnumerator JumpSlamRoutine()
    {
        _jumping = true;
        if (_rb != null) { _rb.linearVelocity = Vector3.zero; _rb.isKinematic = true; }

        Vector3 start = transform.position;
        Vector3 target = _playerTransform.position; // 점프 시작 시점의 플레이어 위치(회피 가능)
        target.y = start.y;

        if (_slamTelegraphPrefab != null)
        {
            var go = Instantiate(_slamTelegraphPrefab);
            var ind = go.GetComponent<TelegraphIndicator>();
            if (ind != null) ind.Setup(target, _slamRadius, _jumpDuration);
        }

        float t = 0f;
        while (t < _jumpDuration)
        {
            bool paused = DropItemEffects.TimeStopActive ||
                          (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing);
            if (!paused) t += Time.deltaTime;

            float u = Mathf.Clamp01(t / _jumpDuration);
            Vector3 pos = Vector3.Lerp(start, target, u);
            pos.y = start.y + _jumpHeight * 4f * u * (1f - u); // 포물선(0→정점→0)
            transform.position = pos;

            if (_health == null || !_health.IsAlive) break; // 사망 시 중단
            yield return null;
        }

        transform.position = target;

        if (_slamImpactPrefab != null)
        {
            var imp = Instantiate(_slamImpactPrefab);
            var burst = imp.GetComponent<ImpactBurst>();
            if (burst != null) burst.Setup(target, _slamRadius);
        }

        if (_health != null && _health.IsAlive)
        {
            var hits = Physics.OverlapSphere(target, _slamRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].CompareTag("Player")) continue;
                var hp = hits[i].GetComponent<HealthComponent>();
                if (hp != null)
                    DamageDealer.Deal(new DamageInfo(_slamDamage, DamageSource.Enemy, gameObject), hp);
                break;
            }
        }

        if (_rb != null) _rb.isKinematic = false;
        _jumping = false;
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
                if (_enemyAnimator != null) _enemyAnimator.PlayAttack();
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
