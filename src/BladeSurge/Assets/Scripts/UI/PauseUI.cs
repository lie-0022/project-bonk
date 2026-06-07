using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 일시정지(Paused) 화면. GameManager.OnGameStateChanged를 구독해 Paused일 때만 패널을 표시.
/// 이어하기 → Playing 복귀(시간 재개), 메인메뉴 → 캐릭터/맵 선택 씬으로 이동.
/// 시간 정지는 GameManager.ChangeState가 처리(timeScale=0).
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Scene")]
    [SerializeField] private string _mainMenuScene = "CharacterSelect";

    private void Awake()
    {
        GameManager.OnGameStateChanged += OnStateChanged;
        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResume);
        if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (_panelRoot != null) _panelRoot.SetActive(state == GameState.Paused);
    }

    /// <summary>이어하기: Playing으로 복귀(시간 재개).</summary>
    private void OnResume()
    {
        GameManager.Instance?.ChangeState(GameState.Playing);
    }

    /// <summary>메인메뉴: 시간 복원 후 캐릭터/맵 선택 씬으로.</summary>
    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuScene);
    }
}
