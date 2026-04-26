using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동, 점프, 대시를 처리합니다.
/// 캐릭터는 이동 방향을 바라봅니다. 카메라는 독립적으로 회전합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _rotationSpeed = 720f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _gravity = -20f;

    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 20f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooldown = 1.5f;

    /// <summary>대시 쿨타임 진행률 [0~1]. HUD 표시용.</summary>
    public float DashCooldownNormalized =>
        _dashCooldownTimer > 0f ? Mathf.Clamp01(_dashCooldownTimer / _dashCooldown) : 0f;

    private CharacterController _cc;
    private CameraController _cameraController;
    private HealthComponent _health;
    private InputSystem_Actions _input;

    private Vector3 _velocity;       // 수직 속도 (중력·점프)
    private Vector3 _moveDirection;  // 마지막 이동 방향 (대시 방향 결정용)

    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDir;

    private bool _inputEnabled = true;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _health = GetComponent<HealthComponent>();
        _cameraController = FindFirstObjectByType<CameraController>();
        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (_input == null) _input = new InputSystem_Actions();
        _input.Player.Enable();
        GameManager.OnGameStateChanged += OnGameStateChanged;
        if (_health != null) _health.OnDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        _input?.Player.Disable();
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        if (_health != null) _health.OnDeath -= OnPlayerDeath;
    }

    private void OnGameStateChanged(GameState state)
    {
        _inputEnabled = state == GameState.Playing;
    }

    private void OnPlayerDeath(float _)
    {
        GameManager.Instance.ChangeState(GameState.GameOver);
    }

    private void Update()
    {
        if (!_inputEnabled) return;

        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= Time.deltaTime;

        // isGrounded는 Move() 호출 전에 한 번만 캐시 (Move를 여러 번 호출하면 상태가 바뀜)
        bool grounded = _cc.isGrounded;

        // 수평 이동 벡터 계산
        Vector3 horizontalMove;
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            horizontalMove = _dashDir * _dashSpeed;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                if (_health != null) _health.IsInvincible = false;
            }
        }
        else
        {
            horizontalMove = CalcHorizontalMove();
            TryDash();
        }

        // 수직 속도 (중력·점프)
        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f;

        if (_input.Player.Jump.WasPressedThisFrame() && grounded)
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

        _velocity.y += _gravity * Time.deltaTime;

        // Move는 프레임당 딱 한 번만 호출
        _cc.Move((horizontalMove + Vector3.up * _velocity.y) * Time.deltaTime);

        HandleRotation();
    }

    private Vector3 CalcHorizontalMove()
    {
        Vector2 input = _input.Player.Move.ReadValue<Vector2>();

        Vector3 forward = _cameraController != null
            ? _cameraController.HorizontalForward
            : Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;

        Vector3 right = _cameraController != null
            ? _cameraController.HorizontalRight
            : Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;

        _moveDirection = forward * input.y + right * input.x;
        if (_moveDirection.sqrMagnitude > 1f) _moveDirection.Normalize();

        return _moveDirection * _moveSpeed;
    }

    private void TryDash()
    {
        if (_dashCooldownTimer > 0f) return;
        if (!_input.Player.Sprint.WasPressedThisFrame()) return;

        _isDashing = true;
        _dashTimer = _dashDuration;
        _dashDir = _moveDirection.sqrMagnitude > 0.01f ? _moveDirection : transform.forward;
        _dashCooldownTimer = _dashCooldown;

        if (_health != null) _health.IsInvincible = true;
    }

    private void HandleRotation()
    {
        if (_moveDirection.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(_moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
    }
}
