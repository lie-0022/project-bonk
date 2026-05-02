using UnityEngine;

public enum DropItemType { Magnet, Speed, TimeStop }

/// <summary>
/// 바닥 드롭 아이템 효과 매니저. 자석/이속/타임스톱의 글로벌 상태와 잔여 시간 관리.
/// 중첩 시 지속 시간 연장. 출처: design/gdd/drop-items.md
/// </summary>
public class DropItemEffects : MonoBehaviour
{
    public static DropItemEffects Instance { get; private set; }

    public static bool MagnetActive { get; private set; }
    public static bool TimeStopActive { get; private set; }
    public static float MoveSpeedMultiplier { get; private set; } = 1f;

    [Header("Durations (s)")]
    [SerializeField] private float _magnetDuration = 5f;
    [SerializeField] private float _speedDuration = 5f;
    [SerializeField] private float _timeStopDuration = 3f;

    [Header("Speed Buff")]
    [SerializeField] private float _speedMultiplier = 1.5f;

    private float _magnetTimer;
    private float _speedTimer;
    private float _timeStopTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResetAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ResetAll();
            Instance = null;
        }
    }

    private void Update()
    {
        // TimeStop은 Time.timeScale을 건드리지 않으므로 deltaTime 사용 OK
        Tick(ref _magnetTimer, () => MagnetActive = false);
        Tick(ref _speedTimer, () => MoveSpeedMultiplier = 1f);
        Tick(ref _timeStopTimer, () => TimeStopActive = false);
    }

    private static void Tick(ref float timer, System.Action onExpire)
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) onExpire?.Invoke();
    }

    /// <summary>드롭 아이템 효과 적용. 중첩 시 잔여 시간에 더함.</summary>
    public void Apply(DropItemType type)
    {
        switch (type)
        {
            case DropItemType.Magnet:
                MagnetActive = true;
                _magnetTimer = Mathf.Max(0f, _magnetTimer) + _magnetDuration;
                break;
            case DropItemType.Speed:
                MoveSpeedMultiplier = _speedMultiplier;
                _speedTimer = Mathf.Max(0f, _speedTimer) + _speedDuration;
                break;
            case DropItemType.TimeStop:
                TimeStopActive = true;
                _timeStopTimer = Mathf.Max(0f, _timeStopTimer) + _timeStopDuration;
                break;
        }
    }

    private static void ResetAll()
    {
        MagnetActive = false;
        TimeStopActive = false;
        MoveSpeedMultiplier = 1f;
    }
}
