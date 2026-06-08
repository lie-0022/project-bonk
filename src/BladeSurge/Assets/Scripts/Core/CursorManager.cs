using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 커스텀 마우스 커서. 기본 커서 + 좌클릭 시 커서 이미지를 교체한다(2상태).
/// 커서가 보이는 화면(메뉴/캐릭터선택/일시정지/결과)에서 동작 — 게임플레이는 커서가 잠겨 숨겨진다.
/// 첫 씬(MainMenu)에 1개 배치하면 DontDestroyOnLoad로 전 씬에서 유지된다.
/// 커서별 핫스팟을 따로 지정한다(.cur의 hotspot 값). CursorMode.ForceSoftware로 크기 제약 없이 안전.
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Default")]
    [SerializeField] private Texture2D _defaultCursor;
    [SerializeField] private Vector2 _defaultHotspot = Vector2.zero;

    [Header("Click (좌클릭 중)")]
    [SerializeField] private Texture2D _clickCursor;
    [SerializeField] private Vector2 _clickHotspot = Vector2.zero;

    [Tooltip("ForceSoftware=크기 제약 없음(권장), Auto=하드웨어 커서(빠르나 32x32 등 제약).")]
    [SerializeField] private CursorMode _cursorMode = CursorMode.ForceSoftware;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyDefault();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) ApplyClick();
        else if (mouse.leftButton.wasReleasedThisFrame) ApplyDefault();
    }

    private void ApplyDefault()
    {
        if (_defaultCursor != null) Cursor.SetCursor(_defaultCursor, _defaultHotspot, _cursorMode);
    }

    private void ApplyClick()
    {
        if (_clickCursor != null) Cursor.SetCursor(_clickCursor, _clickHotspot, _cursorMode);
        else if (_defaultCursor != null) Cursor.SetCursor(_defaultCursor, _defaultHotspot, _cursorMode);
    }
}
