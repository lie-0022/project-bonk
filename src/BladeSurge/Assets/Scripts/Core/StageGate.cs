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

    private bool _opened;

    private void OnEnable() { BossEnemy.OnBossDied += HandleBossDied; }
    private void OnDisable() { BossEnemy.OnBossDied -= HandleBossDied; }

    private void HandleBossDied(BossEnemy boss)
    {
        _opened = true;
        Debug.Log("[StageGate] 보스 처치 — 게이트 열림. 통과하면 다음 맵으로 이동한다.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_opened) return;
        if (!other.CompareTag("Player")) return;

        GameSession.SelectedMapIndex = _nextMapIndex;
        SceneManager.LoadScene(_gamePlaySceneName);
    }
}
