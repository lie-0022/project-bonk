using UnityEngine;

/// <summary>
/// Stage3 2단 보스 진행 관리. BossEnemy.OnBossDied(정적 이벤트)를 카운트해서
/// 1번째 보스(중앙홀) 처치 → 상층 진입 봉인(_upperSeal)을 열고(비활성화),
/// 2번째(최종, 위층) 보스 처치 → 최종 게이트(_finalGate)를 연다.
///
/// 맵 루트(Stage3) 하위에 두면 해당 맵이 활성일 때만 카운트한다.
/// 보스 스폰은 단일 WaveSpawner 재무장 방식이며, 여기서는 사망 카운트만 담당한다.
/// </summary>
public class ArenaPhaseManager : MonoBehaviour
{
    [Tooltip("보스1(중앙홀) 처치 시 비활성화(열)할 상층 진입 봉인. 시작 시 활성(막힘) 상태여야 한다.")]
    [SerializeField] private GameObject _upperSeal;

    [Tooltip("보스2(최종, 위층) 처치 시 Open()을 호출할 최종 게이트. _autoOpenOnBossDied=false 로 둘 것.")]
    [SerializeField] private StageGate _finalGate;

    private int _bossDeaths;

    private void OnEnable() { BossEnemy.OnBossDied += HandleBossDied; }
    private void OnDisable() { BossEnemy.OnBossDied -= HandleBossDied; }

    private void HandleBossDied(BossEnemy boss)
    {
        _bossDeaths++;

        if (_bossDeaths == 1)
        {
            if (_upperSeal != null)
            {
                _upperSeal.SetActive(false);
                MinimapObjective.Set(_upperSeal.transform); // 상층 진입로로 유도
            }
            Debug.Log("[ArenaPhaseManager] 보스1 처치 — 상층 진입 봉인 열림");
        }
        else if (_bossDeaths >= 2)
        {
            if (_finalGate != null) _finalGate.Open();
            Debug.Log("[ArenaPhaseManager] 보스2(최종) 처치 — 최종 게이트 열림");
        }
    }
}
