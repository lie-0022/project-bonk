using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameOver/GameClear 씬의 결과 표시 컨트롤러.
/// RunStats.LastRun 스냅샷을 읽어 처치 수/생존 시간/골드/레벨을 표시하고,
/// 재시작 / 메인 메뉴 버튼을 제공한다.
///
/// 씬 단독으로 동작 — RunStats가 없는 직접 진입(개발 중)에도 기본값 표시.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private string _gameOverTitle = "GAME OVER";
    [SerializeField] private string _winTitle = "STAGE CLEAR";
    [SerializeField] private Color _gameOverColor = new(0.85f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color _winColor      = new(0.95f, 0.78f, 0.2f, 1f);

    [Header("Stats Labels (TMP_Text)")]
    [SerializeField] private TMP_Text _killCountText;
    [SerializeField] private TMP_Text _survivalTimeText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _levelText;

    [Header("Buttons")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string _gameplaySceneName = "GamePlay";
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ApplyResult();
        WireButtons();
    }

    private void ApplyResult()
    {
        var s = RunStats.LastRun;
        if (_titleText != null)
        {
            _titleText.text = s.IsCleared ? _winTitle : _gameOverTitle;
            _titleText.color = s.IsCleared ? _winColor : _gameOverColor;
        }
        if (_killCountText != null)    _killCountText.text    = s.KillCount.ToString();
        if (_survivalTimeText != null) _survivalTimeText.text = s.FormattedTime;
        if (_goldText != null)         _goldText.text         = s.FinalGold.ToString();
        if (_levelText != null)        _levelText.text        = s.FinalLevel.ToString();
    }

    private void WireButtons()
    {
        if (_restartButton != null)
            _restartButton.onClick.AddListener(() =>
            {
                RunTotals.Reset(); // 재시작 = 새 런
                SceneManager.LoadScene(_gameplaySceneName);
            });
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(_mainMenuSceneName));
    }
}
