using UnityEngine;

/// <summary>
/// 3개 스테이지를 거치는 한 런(run)의 누적 통계 + 최종 점수. "스테이지별 리셋 + 합산" 모델.
/// 정적이라 GamePlay 씬 리로드(스테이지 전환)에도 값이 유지된다. 새 런 시작 시 Reset().
/// 각 스테이지 클리어 시 AddStage()로 그 스테이지의 RunStats 스냅샷을 누적한다.
/// 최종 스테이지 클리어 후 GameClear에서 Score를 표시한다.
/// </summary>
public static class RunTotals
{
    // 점수 가중치 (밸런싱 단계에서 조정 가능)
    public const int GoldWeight = 1;
    public const int KillWeight = 10;
    public const int LevelWeight = 100;
    public const int StageWeight = 1000;
    public const int TimeWeight = 1; // 생존 초당. 밸런싱 시 방향(빠른 클리어 보너스 등) 재검토 가능

    public static int TotalKills;
    public static int TotalGold;
    public static float TotalTime;
    public static int HighestLevel;
    public static int StagesCleared;

    /// <summary>최종 총점.</summary>
    public static int Score =>
        TotalGold * GoldWeight
        + TotalKills * KillWeight
        + HighestLevel * LevelWeight
        + StagesCleared * StageWeight
        + Mathf.RoundToInt(TotalTime) * TimeWeight;

    private const string BestScoreKey = "BladeSurge.BestScore";

    /// <summary>최고 점수(PlayerPrefs 영속).</summary>
    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

    /// <summary>"M:SS" 누적 시간 문자열.</summary>
    public static string FormattedTotalTime
    {
        get { int t = Mathf.FloorToInt(TotalTime); return $"{t / 60:00}:{t % 60:00}"; }
    }

    /// <summary>새 런 시작 시 누적 초기화. 메뉴→게임 진입 / 재시작 시 호출.</summary>
    public static void Reset()
    {
        TotalKills = 0;
        TotalGold = 0;
        TotalTime = 0f;
        HighestLevel = 0;
        StagesCleared = 0;
    }

    /// <summary>스테이지 클리어 스냅샷을 누적한다.</summary>
    public static void AddStage(RunStats.Snapshot s)
    {
        TotalKills += s.KillCount;
        TotalGold += s.FinalGold;
        TotalTime += s.SurvivalTime;
        if (s.FinalLevel > HighestLevel) HighestLevel = s.FinalLevel;
        StagesCleared++;
    }

    /// <summary>현재 점수가 최고 점수를 넘으면 저장. 갱신 시 true.</summary>
    public static bool TrySaveBest()
    {
        int s = Score;
        if (s <= BestScore) return false;
        PlayerPrefs.SetInt(BestScoreKey, s);
        PlayerPrefs.Save();
        return true;
    }
}
