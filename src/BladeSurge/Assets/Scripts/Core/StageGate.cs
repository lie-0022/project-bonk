using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 클리어 게이트. 보스 사망(BossEnemy.OnBossDied) 후 "열림" 상태가 되고,
/// 플레이어가 트리거를 통과하면 지정한 다음 맵으로 전환한다(GamePlay 씬 리로드).
///
/// 맵 루트(Stage2 등) 하위에 두면 해당 맵이 활성일 때만 동작한다.
/// 게이트가 없는 맵에서는 BossEnemy가 기존대로 즉시 Win 처리한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StageGate : MonoBehaviour
{
    [Tooltip("통과 시 진입할 다음 맵 인덱스(GameSession.SelectedMapIndex). 0=슬라임, 1=고블린, 2=스켈레톤.")]
    [SerializeField] private int _nextMapIndex = 2;

    [Tooltip("전환할 게임플레이 씬 이름.")]
    [SerializeField] private string _gamePlaySceneName = "GamePlay";

    [Tooltip("true면 통과 시 다음 맵 대신 게임 클리어(Win)로 처리한다. 최종 스테이지 게이트용.")]
    [SerializeField] private bool _isFinalStage = false;

    [Tooltip("보스 처치(게이트 열림) 시 비활성화할 오브젝트들. 게이트 앞 해골 더미 등 장애물.")]
    [SerializeField] private GameObject[] _obstaclesOnClear;

    [Tooltip("보스 처치(게이트 열림) 시 켤 빛기둥(클리어 마커). 평소엔 비활성, 보스 사망 후 등장.")]
    [SerializeField] private GameObject _pillarOnClear;

    [Tooltip("true면 보스 사망(BossEnemy.OnBossDied) 시 자동으로 열린다. 2단 보스처럼 외부(ArenaPhaseManager)가 Open()을 호출하는 경우 false로 둔다.")]
    [SerializeField] private bool _autoOpenOnBossDied = true;

    private bool _opened;
    private bool _passed;

    private void OnEnable() { if (_autoOpenOnBossDied) BossEnemy.OnBossDied += HandleBossDied; }
    private void OnDisable() { BossEnemy.OnBossDied -= HandleBossDied; }

    private void HandleBossDied(BossEnemy boss) => Open();

    /// <summary>
    /// 게이트를 연다. 자동(보스 사망 구독) 또는 외부(ArenaPhaseManager 등)에서 호출한다. 중복 호출은 무시.
    /// </summary>
    public void Open()
    {
        if (_opened) return;
        _opened = true;

        // 게이트 앞 장애물(해골 더미 등) 제거
        if (_obstaclesOnClear != null)
        {
            foreach (var obstacle in _obstaclesOnClear)
                if (obstacle != null) obstacle.SetActive(false);
        }

        // 빛기둥(클리어 마커) 등장 — 보스 사망 후에만 켜진다.
        if (_pillarOnClear != null) _pillarOnClear.SetActive(true);

        // 미니맵 유도: 이 게이트로 안내
        MinimapObjective.Set(transform);

        Debug.Log(_isFinalStage
            ? "[StageGate] 게이트 열림 — 통과하면 게임 클리어."
            : "[StageGate] 게이트 열림 — 통과하면 다음 맵으로 이동한다.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_opened || _passed) return;
        if (!other.CompareTag("Player")) return;
        _passed = true;

        // 이 스테이지 클리어 통계를 누적(랭킹). 다음 맵 로드/Win 전환 전에 캡처한다.
        if (RunStats.Instance != null)
            RunTotals.AddStage(RunStats.Instance.CaptureCurrent(true));

        // 스테이지 클리어 → 다음 스테이지 해금(로컬 진행도 저장).
        StageProgress.MarkCleared(GameSession.SelectedMapIndex);

        // 최종 스테이지면 다음 맵 대신 게임 클리어(Win) 처리.
        if (_isFinalStage)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Win);
            return;
        }

        GameSession.SelectedMapIndex = _nextMapIndex;
        SceneManager.LoadScene(_gamePlaySceneName);
    }
}
