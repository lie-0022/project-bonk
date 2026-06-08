using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 확인 버튼. 현재 선택된 캐릭터/맵을 GameSession에 저장한 뒤 GamePlay 씬으로 진입한다.
/// CharacterSelectController / MapSelectController 의 현재 선택값을 읽어 넘긴다.
/// </summary>
public class StartGameButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSelectController _characterSelect;
    [SerializeField] private MapSelectController _mapSelect;
    [SerializeField] private Button _button;

    [Header("Scene")]
    [SerializeField] private string _gamePlaySceneName = "GamePlay";

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
    }

    private void Start()
    {
        if (_button != null)
            _button.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        if (_characterSelect != null)
            GameSession.SelectedCharacter = _characterSelect.Selected;
        if (_mapSelect != null)
            GameSession.SelectedMapIndex = _mapSelect.Selected;

        // 새 런 시작 — 누적 랭킹 통계 초기화.
        RunTotals.Reset();

        SceneManager.LoadScene(_gamePlaySceneName);
    }
}
