using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 컨트롤러. 시작/옵션/크레딧/종료 버튼 처리.
/// 옵션·크레딧은 별도 씬 또는 같은 캔버스 내 패널 토글.
/// MVP 단계에서는 씬 전환 방식.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _quitButton;

    [Header("Scene Names")]
    [SerializeField] private string _nextSceneName = "CharacterSelect";
    [SerializeField] private string _optionsSceneName = "Options";
    [SerializeField] private string _creditsSceneName = "Credits";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        WireButtons();
    }

    private void WireButtons()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartClicked);
        if (_optionsButton != null)
            _optionsButton.onClick.AddListener(OnOptionsClicked);
        if (_creditsButton != null)
            _creditsButton.onClick.AddListener(OnCreditsClicked);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(QuitApplication);
    }

    private void OnStartClicked()
    {
        if (SceneExistsInBuild(_nextSceneName))
            SceneManager.LoadScene(_nextSceneName);
        else
            Debug.Log($"[MainMenu] '{_nextSceneName}' 씬이 아직 Build Settings에 없음. 다음 화면 구성 후 연결 예정.");
    }

    private void OnOptionsClicked()
    {
        if (SceneExistsInBuild(_optionsSceneName))
            SceneManager.LoadScene(_optionsSceneName);
        else
            Debug.Log($"[MainMenu] '{_optionsSceneName}' 씬이 Build Settings에 없음. 추후 추가 예정.");
    }

    private void OnCreditsClicked()
    {
        if (SceneExistsInBuild(_creditsSceneName))
            SceneManager.LoadScene(_creditsSceneName);
        else
            Debug.Log($"[MainMenu] '{_creditsSceneName}' 씬이 Build Settings에 없음. 추후 추가 예정.");
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>씬이 Build Settings에 등록되어 있는지 확인.</summary>
    private static bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
}
