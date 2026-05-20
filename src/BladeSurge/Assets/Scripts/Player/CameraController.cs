using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 오빗 카메라. 마우스 X로 수평 회전, 위치 스냅(Lerp 없음).
/// GameManager.OnGameStateChanged를 구독해 커서 잠금을 자동 관리한다.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private float _distance = 15f;
    [SerializeField] private float _pitchMin = -20f;
    [SerializeField] private float _pitchMax = 70f;

    [Header("Collision (벽 통과 방지)")]
    [Tooltip("카메라가 충돌 판정할 레이어(맵 벽/바닥 = Environment). 플레이어/적은 제외.")]
    [SerializeField] private LayerMask _collisionMask = 0;
    [Tooltip("충돌 판정 구체 반경(m). 카메라가 벽에 살짝 못 미치게 한다.")]
    [SerializeField] private float _collisionRadius = 0.3f;
    [Tooltip("벽에 부딪혔을 때 유지할 최소 거리(m).")]
    [SerializeField] private float _minDistance = 1.5f;
    [Tooltip("충돌 지점에서 추가로 당길 여유(m).")]
    [SerializeField] private float _collisionBuffer = 0.2f;
    [Tooltip("플레이어 발 기준 시점 피벗 높이(m). 바닥 자가충돌 방지 + 자연스러운 프레이밍.")]
    [SerializeField] private float _pivotHeight = 1.2f;

    /// <summary>현재 적용된 마우스 감도. SettingsService(PlayerPrefs)에서 동적으로 읽는다.</summary>
    private float Sensitivity => SettingsService.MouseSensitivity;

    /// <summary>카메라 수평 Forward. 플레이어 이동 기준축.</summary>
    public Vector3 HorizontalForward =>
        Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

    /// <summary>카메라 수평 Right. 플레이어 이동 기준축.</summary>
    public Vector3 HorizontalRight =>
        Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

    private Transform _target;
    private float _yaw;
    private float _pitch = 30f;

    private void Awake()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) _target = player.transform;
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start() => LockCursor();

    private void OnDestroy() => UnlockCursor();

    // 윈도우 포커스 복귀 시 Playing 상태면 자동 재잠금
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
            LockCursor();
    }

    private void Update()
    {
        // 일시정지 중 좌클릭으로 재개
        if (GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameState.Paused
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        // timeScale=0 (레벨업 선택 등) 동안 카메라 회전 입력 차단
        if (Time.timeScale == 0f) return;

        float mouseX = Mouse.current.delta.x.ReadValue();
        float mouseY = Mouse.current.delta.y.ReadValue();
        _yaw   += mouseX * Sensitivity;
        _pitch -= mouseY * Sensitivity;
        _pitch  = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.rotation = rotation;

        // 시점 피벗(머리 높이 부근)에서 원하는 카메라 위치 방향으로 SphereCast.
        // 벽(Environment)에 막히면 충돌 지점 앞으로 당겨 카메라가 맵을 통과하지 않게 한다.
        Vector3 pivot = _target.position + Vector3.up * _pivotHeight;
        Vector3 dir = rotation * Vector3.back; // 피벗 -> 카메라 방향
        float distance = _distance;

        if (_collisionMask != 0 && Physics.SphereCast(
                pivot, _collisionRadius, dir, out RaycastHit hit,
                _distance, _collisionMask, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(_minDistance, hit.distance - _collisionBuffer);
        }

        transform.position = pivot + dir * distance;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
            LockCursor();
        else
            UnlockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
