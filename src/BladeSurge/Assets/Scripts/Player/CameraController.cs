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
    [SerializeField] private float _sensitivity = 0.15f;

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
        _yaw   += mouseX * _sensitivity;
        _pitch -= mouseY * _sensitivity;
        _pitch  = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.rotation = rotation;
        transform.position = _target.position + rotation * new Vector3(0f, 0f, -_distance);
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
