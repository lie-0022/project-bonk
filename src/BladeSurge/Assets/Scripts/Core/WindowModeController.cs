using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 창/전체화면 모드를 결정적으로 관리한다. 창모드(1280x720, 타이틀바 O)로 시작하고,
/// Alt+Enter로 테두리 없는 전체화면(FullScreenWindow — 작업표시줄을 완전히 덮음)과 토글한다.
///
/// Unity 내장 Alt+Enter(allowFullscreenSwitch)는 모드가 불확실하므로 끄고(Player Settings),
/// 여기서 명시적으로 FullScreenWindow를 지정해 "전체화면 시 작업표시줄이 보이는" 문제를 막는다.
/// MainMenu(엔트리 씬)에 두면 DontDestroyOnLoad로 모든 씬에 유지된다.
/// </summary>
public class WindowModeController : MonoBehaviour
{
    [SerializeField] private int _windowedWidth = 1280;
    [SerializeField] private int _windowedHeight = 720;

    private static WindowModeController _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 창모드로 시작 (타이틀바 있음, 리사이즈 가능)
        Screen.SetResolution(_windowedWidth, _windowedHeight, FullScreenMode.Windowed);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
        if (alt && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            Toggle();
    }

    /// <summary>창모드 ↔ 테두리 없는 전체화면(작업표시줄 덮음) 토글.</summary>
    public void Toggle()
    {
        if (Screen.fullScreen)
        {
            Screen.SetResolution(_windowedWidth, _windowedHeight, FullScreenMode.Windowed);
        }
        else
        {
            Resolution r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
    }
}
