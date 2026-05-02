using UnityEngine;

/// <summary>돌진형 상태 머신.</summary>
public enum RusherState { Approaching, WindingUp, Charging, Recovering }

/// <summary>
/// 돌진형 적 AI. 평소에는 천천히 접근하다가 예고 후 고속 직선 돌진한다.
/// WindingUp 시 노란색으로 변색해 플레이어에게 경고를 준다.
/// </summary>
public class RusherAI : EnemyBase
{
    [SerializeField] private float _approachSpeed = 2.0f;
    [SerializeField] private float _chargeWindupTime = 2.5f;
    [SerializeField] private float _windupWarnDuration = 0.5f;
    [SerializeField] private float _chargeSpeed = 15.0f;
    [SerializeField] private float _maxChargeDistance = 8.0f;
    [SerializeField] private float _rushCooldown = 3.0f;
    [SerializeField] private float _chargeDamage = 20f;
    [SerializeField] private float _contactRadius = 1.5f;

    private RusherState _state;
    private float _stateTimer;
    private Vector3 _chargeDir;
    private float _chargeDistanceTraveled;
    private bool _chargeDamageDealt;

    private Renderer _renderer;
    private Color _originalColor;

    public override EnemyType EnemyType => EnemyType.Rusher;

    protected override void Awake()
    {
        base.Awake();
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    protected override void OnActivate()
    {
        _state = RusherState.Approaching;
        _stateTimer = 0f;
        _chargeDistanceTraveled = 0f;
        _chargeDamageDealt = false;
        SetColor(_originalColor);
    }

    protected override void Update()
    {
        base.Update(); // 스폰 무적 타이머

        if (!_isActive || _playerTransform == null) return;
        if (DropItemEffects.TimeStopActive)
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            return;
        }

        switch (_state)
        {
            case RusherState.Approaching: UpdateApproaching(); break;
            case RusherState.WindingUp:   UpdateWindingUp();   break;
            case RusherState.Charging:    UpdateCharging();    break;
            case RusherState.Recovering:  UpdateRecovering();  break;
        }
    }

    private void UpdateApproaching()
    {
        MoveTowardPlayer(_approachSpeed);

        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _chargeWindupTime)
            EnterWindingUp();
    }

    private void UpdateWindingUp()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _windupWarnDuration)
            EnterCharging();
    }

    private void UpdateCharging()
    {
        float step = _chargeSpeed * Time.deltaTime;
        _rb.linearVelocity = new Vector3(
            _chargeDir.x * _chargeSpeed,
            _rb.linearVelocity.y,
            _chargeDir.z * _chargeSpeed);
        _chargeDistanceTraveled += step;

        // 돌진 중 플레이어 충돌 검사 (1회만)
        if (!_chargeDamageDealt)
        {
            float dx = transform.position.x - _playerTransform.position.x;
            float dz = transform.position.z - _playerTransform.position.z;
            if (dx * dx + dz * dz < _contactRadius * _contactRadius)
            {
                DamageDealer.Deal(
                    new DamageInfo(_chargeDamage, DamageSource.Enemy, gameObject),
                    _playerHealth);
                _chargeDamageDealt = true;
            }
        }

        if (_chargeDistanceTraveled >= _maxChargeDistance)
            EnterRecovering();
    }

    private void UpdateRecovering()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _rushCooldown)
        {
            _state = RusherState.Approaching;
            _stateTimer = 0f;
        }
    }

    // ── 상태 전환 ─────────────────────────────────────────────

    private void EnterWindingUp()
    {
        _state = RusherState.WindingUp;
        _stateTimer = 0f;
        _rb.linearVelocity = Vector3.zero;
        SetColor(Color.yellow);
    }

    private void EnterCharging()
    {
        _chargeDir = _playerTransform.position - transform.position;
        _chargeDir.y = 0f;
        _chargeDir.Normalize();
        _chargeDistanceTraveled = 0f;
        _chargeDamageDealt = false;
        _state = RusherState.Charging;
        _stateTimer = 0f;
        SetColor(Color.red);
    }

    private void EnterRecovering()
    {
        _rb.linearVelocity = Vector3.zero;
        _state = RusherState.Recovering;
        _stateTimer = 0f;
        SetColor(_originalColor);
    }

    // ── 공통 헬퍼 ────────────────────────────────────────────

    private void MoveTowardPlayer(float speed)
    {
        Vector3 dir = _playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        float scaledSpeed = speed * _speedMultiplier;
        _rb.linearVelocity = new Vector3(dir.x * scaledSpeed, _rb.linearVelocity.y, dir.z * scaledSpeed);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), 720f * Time.deltaTime);
    }

    private void SetColor(Color color)
    {
        if (_renderer != null) _renderer.material.color = color;
    }
}
