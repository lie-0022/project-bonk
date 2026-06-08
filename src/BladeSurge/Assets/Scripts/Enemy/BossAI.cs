using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 AI. 기본은 단순 추적(걷기) + 접촉 데미지.
/// _jumpMovement=true면 걷지 않고 플레이어 쪽으로 반복 점프하며 착지마다 내려치기 AOE를 한다(슬라임 보스).
/// BossEnemy와 한 GameObject에 함께 부착되며, EnemyBase의 _isActive/_playerTransform 상태를 사용한다.
/// </summary>
[RequireComponent(typeof(BossEnemy))]
public class BossAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 2.0f;

    [Header("점프 이동 (슬라임 보스 — 걷지 않고 점프로만 이동)")]
    [Tooltip("켜면 걷기 대신 플레이어 쪽으로 반복 점프하며, 착지 시 내려치기 AOE를 한다.")]
    [SerializeField] private bool _jumpMovement = false;
    [Tooltip("점프 사이 쉬는 시간(초).")]
    [SerializeField] private float _jumpInterval = 1.5f;
    [Tooltip("점프 대기 중 플레이어를 향해 느리게 기어가는 속도(m/s). 0이면 제자리.")]
    [SerializeField] private float _jumpWalkSpeed = 1.2f;
    [Tooltip("한 번 점프로 이동하는 최대 거리(m). 플레이어가 멀면 이 거리만큼씩 접근.")]
    [SerializeField] private float _maxHopDistance = 10f;
    [Tooltip("점프 정점 높이(m).")]
    [SerializeField] private float _jumpHeight = 5f;
    [Tooltip("점프(상승+하강) 시간(초) = 착지 예고 시간.")]
    [SerializeField] private float _jumpDuration = 0.8f;
    [Tooltip("착지 내려치기 AOE 반경(m).")]
    [SerializeField] private float _slamRadius = 4f;
    [Tooltip("착지 내려치기 데미지.")]
    [SerializeField] private float _slamDamage = 30f;
    [SerializeField] private GameObject _slamTelegraphPrefab;
    [SerializeField] private GameObject _slamImpactPrefab;

    [Header("Contact Damage")]
    [SerializeField] private float _contactDamage = 25f;
    [SerializeField] private float _contactInterval = 0.8f;
    [SerializeField] private float _contactRadius = 2.5f;

    private BossEnemy _boss;
    private Rigidbody _rb;
    private EnemyAnimator _enemyAnimator;
    private Transform _player;
    private HealthComponent _playerHealth;
    private float _contactTimer;
    private float _jumpTimer;
    private bool _jumping;

    /// <summary>true면 BossAI가 이동/접촉을 멈춘다. 돌진 등 BossAttack이 직접 위치를 제어할 때 설정.</summary>
    public bool SuspendMovement { get; set; }

    private void Awake()
    {
        _boss = GetComponent<BossEnemy>();
        _rb = GetComponent<Rigidbody>();
        _enemyAnimator = GetComponent<EnemyAnimator>();
        _jumpTimer = _jumpInterval;
    }

    private void Update()
    {
        EnsurePlayerRef();
        if (_player == null) return;
        if (_jumping) return; // 점프 코루틴이 위치를 제어
        if (SuspendMovement) return; // 돌진 등 외부 공격이 위치를 제어

        if (!_boss.IsActive) { StopHorizontal(); return; }
        if (DropItemEffects.TimeStopActive) { StopHorizontal(); return; }
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) { StopHorizontal(); return; }
        if (_boss.Health == null || !_boss.Health.IsAlive) { StopHorizontal(); return; }

        if (_jumpMovement) HandleJumpMovement();
        else MoveTowardPlayer();

        HandleContactDamage();
    }

    private void StopHorizontal()
    {
        if (_rb != null && !_rb.isKinematic)
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
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

    // ── 점프 이동 + 착지 내려치기 ─────────────────────────────
    private void HandleJumpMovement()
    {
        SlowCrawlAndFace(); // 쉬는 동안 느리게 추적 + 플레이어 주시
        _jumpTimer -= Time.deltaTime;
        if (_jumpTimer > 0f) return;
        _jumpTimer = _jumpInterval;
        StartCoroutine(JumpHopRoutine());
    }

    // 점프 대기 중: 느린 속도로 플레이어를 향해 이동하며 계속 바라본다.
    private void SlowCrawlAndFace()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) { StopHorizontal(); return; }
        dir.Normalize();

        if (_rb != null && !_rb.isKinematic)
            _rb.linearVelocity = new Vector3(dir.x * _jumpWalkSpeed, _rb.linearVelocity.y, dir.z * _jumpWalkSpeed);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
    }

    private IEnumerator JumpHopRoutine()
    {
        _jumping = true;
        if (_rb != null) { _rb.linearVelocity = Vector3.zero; _rb.isKinematic = true; }

        Vector3 start = transform.position;
        Vector3 flat = _player.position - start; flat.y = 0f;
        float dist = flat.magnitude;
        Vector3 dir = dist > 0.01f ? flat / dist : transform.forward;
        Vector3 target = start + dir * Mathf.Min(dist, _maxHopDistance);
        target.y = start.y;

        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);

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

            if (_boss.Health == null || !_boss.Health.IsAlive) break;
            yield return null;
        }

        transform.position = target;

        if (_slamImpactPrefab != null)
        {
            var imp = Instantiate(_slamImpactPrefab);
            var burst = imp.GetComponent<ImpactBurst>();
            if (burst != null) burst.Setup(target, _slamRadius);
        }
        if (_enemyAnimator != null) _enemyAnimator.PlayAttack();

        if (_boss.Health != null && _boss.Health.IsAlive)
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
                if (_enemyAnimator != null) _enemyAnimator.PlayAttack();
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
