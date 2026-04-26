// PROTOTYPE - NOT FOR PRODUCTION
// Question: CharacterController + 마우스 방향 대시 + 무적 프레임이 자연스럽게 느껴지는가?
// Date: 2026-03-26

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.5f;

    // 외부 시스템에서 읽는 값
    public bool IsInvincible => _isDashing;
    public Vector3 AimDirection { get; private set; } = Vector3.forward;
    public float DashCooldownNormalized => _dashCooldown > 0 ? Mathf.Clamp01(_dashCooldownTimer / dashCooldown) : 0f;

    private CharacterController _cc;
    private Camera _cam;

    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDir;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = Camera.main;
    }

    private void Update()
    {
        UpdateAimDirection();

        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            _cc.Move(_dashDir * dashSpeed * Time.deltaTime);
            if (_dashTimer <= 0f)
                _isDashing = false;
        }
        else
        {
            HandleMovement();
            TryDash();
        }

        HandleRotation();
        ApplyGravity();
    }

    // 마우스 커서의 월드 위치를 계산해 AimDirection 갱신
    private void UpdateAimDirection()
    {
        // 플레이어 Y 높이의 수평 평면에 레이 교차
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 aimPoint = ray.GetPoint(dist);
            Vector3 dir = aimPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                AimDirection = dir.normalized;
        }
    }

    // 카메라 기준 WASD 이동 (아이소메트릭 보정)
    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(_cam.transform.right,   Vector3.up).normalized;

        Vector3 moveDir = camForward * v + camRight * h;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        _cc.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    // Space 키로 마우스 방향 대시
    private void TryDash()
    {
        if (_dashCooldownTimer > 0f) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        _isDashing = true;
        _dashTimer = dashDuration;
        _dashDir = AimDirection;
        _dashCooldownTimer = dashCooldown;

        Debug.Log($"[Prototype] Dash! dir={_dashDir}, invincible={IsInvincible}");
    }

    // 항상 마우스 방향을 바라봄
    private void HandleRotation()
    {
        if (AimDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(AimDirection);
    }

    // 중력 (CharacterController는 자동 중력 없음)
    private void ApplyGravity()
    {
        if (!_cc.isGrounded)
            _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }
}
