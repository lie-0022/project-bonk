using System;
using UnityEngine;

/// <summary>
/// 한 판(런) 통계 추적기. 처치 수, 생존 시간을 실시간 집계하고,
/// 게임 종료(GameOver/Win) 시 정적 스냅샷에 동결해 결과 화면에서 읽도록 한다.
///
/// 씬에 1개 인스턴스 배치(Managers 하위 권장). DontDestroyOnLoad 사용 안 함 —
/// 결과 화면은 LastRun 정적 스냅샷에서 값을 가져온다.
/// </summary>
public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    /// <summary>마지막 런 결과 스냅샷. GameOver/GameClear 씬에서 참조.</summary>
    public static Snapshot LastRun = new();

    public int KillCount { get; private set; }
    public float SurvivalTime { get; private set; }

    private bool _running;

    [Serializable]
    public struct Snapshot
    {
        public int KillCount;
        public float SurvivalTime;
        public int FinalGold;
        public int FinalLevel;
        public bool IsCleared;     // true=Win, false=GameOver

        /// <summary>"3:42" 형식의 분:초 문자열.</summary>
        public string FormattedTime
        {
            get
            {
                int total = Mathf.FloorToInt(SurvivalTime);
                return $"{total / 60:00}:{total % 60:00}";
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        EnemyBase.OnEnemyDied += OnEnemyDied;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyDied -= OnEnemyDied;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (_running) SurvivalTime += Time.deltaTime;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        KillCount++;
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                _running = true;
                break;
            case GameState.Paused:
                _running = false;
                break;
            case GameState.GameOver:
            case GameState.Win:
                _running = false;
                FreezeSnapshot(newState == GameState.Win);
                break;
        }
    }

    private void FreezeSnapshot(bool cleared)
    {
        LastRun = new Snapshot
        {
            KillCount = KillCount,
            SurvivalTime = SurvivalTime,
            FinalGold = GoldSystem.Instance != null ? GoldSystem.Instance.CurrentGold : 0,
            FinalLevel = XPSystem.Instance != null ? XPSystem.Instance.CurrentLevel : 1,
            IsCleared = cleared,
        };
    }
}
