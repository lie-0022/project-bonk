using UnityEngine;

/// <summary>
/// 스테이지 해금 진행도(로컬 PlayerPrefs 저장). 처음엔 첫 스테이지(인덱스 0)만 해금.
/// 어떤 스테이지를 클리어하면 다음 스테이지가 해금된다(MarkCleared). 맵 선택 화면이 IsUnlocked로 잠금 표시.
/// </summary>
public static class StageProgress
{
    private const string Key = "BladeSurge.HighestUnlockedStage";

    /// <summary>해금된 최고 스테이지 인덱스. 0 = 첫 스테이지만 해금(기본).</summary>
    public static int HighestUnlocked
    {
        get => PlayerPrefs.GetInt(Key, 0);
        private set { PlayerPrefs.SetInt(Key, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    /// <summary>해당 인덱스 스테이지가 해금되어 선택 가능한지.</summary>
    public static bool IsUnlocked(int index) => index <= HighestUnlocked;

    /// <summary>clearedIndex 스테이지를 클리어했음을 기록 → 다음 스테이지 해금.</summary>
    public static void MarkCleared(int clearedIndex)
    {
        if (clearedIndex + 1 > HighestUnlocked)
            HighestUnlocked = clearedIndex + 1;
    }

    /// <summary>진행도 초기화(첫 스테이지만 해금). 디버그/뉴게임용.</summary>
    public static void ResetProgress() => HighestUnlocked = 0;

    /// <summary>모든 스테이지 해금(디버그용).</summary>
    public static void UnlockAll(int maxIndex) => HighestUnlocked = maxIndex;
}
