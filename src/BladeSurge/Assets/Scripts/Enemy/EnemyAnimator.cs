using UnityEngine;

/// <summary>
/// 적의 Animator 파라미터를 자동 갱신한다.
/// - IsMoving: Rigidbody 속도 기반 (Idle ↔ Walking 전환용)
/// - Hit: HealthComponent.OnDamaged 시 트리거
/// - Death: HealthComponent.OnDeath 시 트리거
///
/// Animator에 동일 파라미터(IsMoving:bool, Hit:trigger, Death:trigger)가 정의돼 있어야 한다.
/// 캡슐 placeholder처럼 Animator 가 없는 적에는 부착하지 않으면 된다.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private float _movingThreshold = 0.1f;
    [Tooltip("Hit 애니 최소 재생 간격(초). 0 이면 매번 재생. 보스/탱키 적은 1.0+ 권장.")]
    [SerializeField] private float _hitAnimCooldown = 0f;

    private Animator _animator;
    private Rigidbody _rb;
    private HealthComponent _health;
    private float _lastHitTime = -999f;

    private static readonly int Param_IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Param_Hit = Animator.StringToHash("Hit");
    private static readonly int Param_Death = Animator.StringToHash("Death");
    private static readonly int Param_Spawn = Animator.StringToHash("Spawn");
    private static readonly int Param_Attack = Animator.StringToHash("Attack");
    private static readonly int Param_IdleVariant = Animator.StringToHash("IdleVariant");
    private static readonly int Param_WalkVariant = Animator.StringToHash("WalkVariant");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _rb = GetComponent<Rigidbody>();
        _health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        _health.OnDamaged += OnDamaged;
        _health.OnDeath   += OnDeath;
        if (_animator != null)
        {
            _animator.SetTrigger(Param_Spawn);
            _animator.SetFloat(Param_IdleVariant, Random.value);
            _animator.SetFloat(Param_WalkVariant, Random.value);
        }
    }

    private void OnDisable()
    {
        _health.OnDamaged -= OnDamaged;
        _health.OnDeath   -= OnDeath;
    }

    private void Update()
    {
        if (_animator == null || _rb == null) return;
        bool moving = _rb.linearVelocity.sqrMagnitude > _movingThreshold * _movingThreshold;
        _animator.SetBool(Param_IsMoving, moving);
    }

    private void OnDamaged(float amount, float currentHp)
    {
        if (_animator == null) return;
        if (Time.time - _lastHitTime < _hitAnimCooldown) return;
        _animator.SetTrigger(Param_Hit);
        _lastHitTime = Time.time;
    }

    private void OnDeath(float xpReward)
    {
        if (_animator != null) _animator.SetTrigger(Param_Death);
    }

    /// <summary>외부(AI)에서 호출 — 공격 트리거.</summary>
    public void PlayAttack()
    {
        if (_animator != null) _animator.SetTrigger(Param_Attack);
    }
}
