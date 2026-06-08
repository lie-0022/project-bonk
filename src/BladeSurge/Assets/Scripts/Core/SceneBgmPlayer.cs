using UnityEngine;

/// <summary>
/// 씬 진입 시 지정한 BGM 트랙을 재생한다. 게임플레이 상태머신(GameManager) 밖에서
/// BGM을 시작해야 하는 메뉴 씬(MainMenu/CharacterSelect/GameOver/GameClear 등)에 부착한다.
///
/// AudioManager(DontDestroyOnLoad 싱글턴)가 같은 씬 또는 이전 씬에 존재해야 한다.
/// Start에서 호출하므로 AudioManager.Awake(Instance 설정)보다 늦게 실행돼 안전하다.
/// </summary>
public class SceneBgmPlayer : MonoBehaviour
{
    [Tooltip("이 씬에서 재생할 BGM 트랙. None이면 아무것도 하지 않는다.")]
    [SerializeField] private BgmTrack _track = BgmTrack.MainMenu;

    private void Start()
    {
        if (_track == BgmTrack.None) return;
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneBgmPlayer] AudioManager.Instance 없음 — BGM 재생 생략. 씬에 AudioManager가 있는지 확인.");
            return;
        }
        AudioManager.Instance.PlayBgm(_track);
    }
}
