using UnityEngine;

/// <summary>
/// 미니맵 유도 목표 지점 홀더. "보스 처치 후 이동할 곳"을 가리킨다.
/// StageGate.Open()(게이트 열림) / ArenaPhaseManager(상층 봉인 열림) 등이 Set,
/// 새 인카운터(전투) 시작 시 Clear. MinimapManager가 Target을 읽어 마커+화살표를 표시한다.
/// 정적이지만 Target은 씬 오브젝트 Transform이라 씬 리로드 시 자동으로 무효(파괴)된다.
/// </summary>
public static class MinimapObjective
{
    /// <summary>유도 목표. null이면 표시 안 함.</summary>
    public static Transform Target;

    public static void Set(Transform t) { Target = t; }
    public static void Clear() { Target = null; }
}
