using UnityEngine;

/// <summary>
/// EnemyAI 구현체들의 공통 계약.
/// WaveSpawner와 GameManager가 구체 AI 타입 없이 적을 제어하기 위해 사용.
/// FindObjectsByType 없이 List&lt;IEnemyController&gt; 캐싱으로 전체 AI 일괄 제어 가능.
/// </summary>
public interface IEnemyController
{
    /// <summary>
    /// AI 활성화. OnSpawn 이후 호출. 플레이어 Transform을 주입받아 추적 시작.
    /// </summary>
    void Activate(Transform playerTransform);

    /// <summary>
    /// AI 정지. 게임 상태가 Playing이 아닐 때 GameManager가 호출.
    /// </summary>
    void Deactivate();

    /// <summary>적 종류. 스폰 통계 및 디버그용.</summary>
    EnemyType EnemyType { get; }
}
