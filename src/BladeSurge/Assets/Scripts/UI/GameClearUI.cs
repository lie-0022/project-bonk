using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 클리어 화면. 최종 스테이지 클리어(GameState.Win) 시 GameManager가 이 씬을 로드한다.
/// '메인 화면' 버튼 → 타이틀(MainMenu) 씬으로 이동. 진입 시 커서 잠금 해제.
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [SerializeField] private Button _mainScreenButton;
    [SerializeField] private string _mainMenuScene = "MainMenu";

    private void Awake()
    {
        if (_mainScreenButton != null) _mainScreenButton.onClick.AddListener(OnMainScreen);

        // 클리어 화면은 마우스 조작 화면 — 커서 잠금 해제.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    private void OnMainScreen()
    {
        SceneManager.LoadScene(_mainMenuScene);
    }
}
