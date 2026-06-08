using UnityEngine;

/// <summary>
/// 스테이지(맵)별 웨이브 구성 데이터. DebugStageSelector가 선택한 맵에 맞는 config를
/// WaveSpawner에 주입(SetWaveConfig)한다. 스테이지마다 웨이브 수·길이·보스 등장 시점을
/// 독립적으로 설정해 페이싱을 차별화한다(예: 슬라임=짧고 쉬움 / 스켈레톤=길고 보스 늦게).
///
/// 미주입 시 WaveSpawner의 인스펙터 기본값이 그대로 쓰인다.
/// </summary>
[CreateAssetMenu(fileName = "StageWaveConfig", menuName = "BladeSurge/Stage Wave Config")]
public class StageWaveConfig : ScriptableObject
{
    [Tooltip("각 웨이브의 길이(초). 이 시간이 지나면 다음 웨이브로 전환된다.")]
    public float WaveDuration = 30f;

    [Tooltip("이 웨이브가 끝난 직후 보스를 스폰한다(1-based). 0 또는 음수면 보스 없음(웨이브 전부 클리어=승리).")]
    public int BossSpawnAfterWave = 5;

    [Tooltip("웨이브 간 전환 딜레이(초).")]
    public float WaveTransitionDelay = 1f;

    [Tooltip("웨이브별 스폰 간격·HP 배율·속도 배율. 배열 길이가 곧 웨이브 수다. StageDifficulty 배율은 이 위에 곱해진다.")]
    public WaveData[] Waves;
}
