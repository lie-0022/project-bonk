using TMPro;
using UnityEngine;

/// <summary>
/// 상단 바 UI. 흐른 시간, 소지 골드, 킬 수를 표시한다.
/// </summary>
public class TopBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _killText;

    private float _survivalTime;
    private int _killCount;
    private int _gold;
    private bool _isPlaying;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
        EnemyBase.OnEnemyDied += OnEnemyDied;
        GoldSystem.OnGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        EnemyBase.OnEnemyDied -= OnEnemyDied;
        GoldSystem.OnGoldChanged -= OnGoldChanged;
    }

    private void Start()
    {
        _survivalTime = 0f;
        _killCount = 0;
        _gold = 0;
        RefreshAll();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _survivalTime += Time.deltaTime;
        RefreshTime();
    }

    private void OnGameStateChanged(GameState state)
    {
        _isPlaying = state == GameState.Playing;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        _killCount++;
        RefreshKill();
    }

    private void OnGoldChanged(int gold)
    {
        _gold = gold;
        RefreshGold();
    }

    private void RefreshAll()
    {
        RefreshTime();
        RefreshGold();
        RefreshKill();
    }

    private void RefreshTime()
    {
        if (_timeText == null) return;
        int minutes = Mathf.FloorToInt(_survivalTime / 60f);
        int seconds = Mathf.FloorToInt(_survivalTime % 60f);
        _timeText.text = $"{minutes}:{seconds:00}";
    }

    private void RefreshGold()
    {
        if (_goldText == null) return;
        _goldText.text = $"{_gold} G";
    }

    private void RefreshKill()
    {
        if (_killText == null) return;
        _killText.text = $"{_killCount} Kill";
    }

    /// <summary>GameOver/Win 화면용 최종 수치를 반환한다.</summary>
    public (float time, int kills) GetFinalStats() => (_survivalTime, _killCount);
}
