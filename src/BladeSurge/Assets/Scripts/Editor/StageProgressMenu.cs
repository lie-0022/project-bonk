#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지 해금 진행도 디버그 메뉴. 잠금/해금 상태를 테스트할 때 사용.
/// BladeSurge ▸ Stage Progress ▸ ...
/// </summary>
public static class StageProgressMenu
{
    [MenuItem("BladeSurge/Stage Progress/Reset (첫 스테이지만)")]
    private static void Reset()
    {
        StageProgress.ResetProgress();
        Debug.Log("[StageProgress] 초기화 — 첫 스테이지만 해금됨");
    }

    [MenuItem("BladeSurge/Stage Progress/Unlock All (전체 해금)")]
    private static void UnlockAll()
    {
        StageProgress.UnlockAll(2); // 맵 0/1/2
        Debug.Log("[StageProgress] 전체 해금됨 (0/1/2)");
    }

    [MenuItem("BladeSurge/Stage Progress/Log Current")]
    private static void Log()
    {
        Debug.Log($"[StageProgress] HighestUnlocked = {StageProgress.HighestUnlocked} (이 인덱스까지 해금)");
    }
}
#endif
