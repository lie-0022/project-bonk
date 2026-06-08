using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 첫 스테이지(맵0) 시작 전 검은 화면 타자기 인트로 컷신.
/// timeScale=0으로 게임을 멈춘 채 unscaled 시간으로 컷(문단)을 한 글자씩 출력한다.
/// - 타이핑 중 클릭 → 그 컷을 즉시 전부 표시(완성)
/// - 타이핑 완료 후 클릭 → 다음 컷으로 진행 (마지막 컷이면 게임 시작)
/// - ESC → 전체 스킵
/// 지정 맵(_onlyOnMapIndex)이 아니면 즉시 비활성화한다.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Tooltip("검은 화면+텍스트 루트의 CanvasGroup (페이드용).")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _label;

    [Tooltip("컷(문단) 목록. 한 컷씩 타이핑 → 클릭으로 다음 컷. PM이 자유롭게 편집.")]
    [TextArea(3, 8)]
    [SerializeField] private string[] _lines =
    {
        "검(劍)이 녹슬어가던 시대,",
        "이름 없는 검사가 일어섰다.",
        "—— 검나쎄짐 ——",
    };

    [Tooltip("이 맵 인덱스에서만 컷신 재생(0=첫 스테이지).")]
    [SerializeField] private int _onlyOnMapIndex = 0;
    [Tooltip("글자 출력 간격(초, unscaled). 작을수록 빠른 '타다닥'.")]
    [SerializeField] private float _charInterval = 0.04f;
    [Tooltip("타이핑 시작 전 대기(초). 씬 로드 직후 프레임 튐을 흘려보내 초반 글자 폭주를 막는다.")]
    [SerializeField] private float _startDelay = 0.35f;
    [Tooltip("종료 페이드아웃 시간(초).")]
    [SerializeField] private float _fadeOut = 0.4f;

    private void Start()
    {
        if (GameSession.SelectedMapIndex != _onlyOnMapIndex)
        {
            ReleaseImmediate();
            return;
        }
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        Time.timeScale = 0f;
        if (_group != null) { _group.alpha = 1f; _group.blocksRaycasts = true; }

        // 로드 직후 프레임 튐(큰 deltaTime)을 흘려보낸 뒤 타이핑 시작 — 최소 몇 프레임 + _startDelay 경과.
        int warmFrames = 0;
        float warm = 0f;
        while (warmFrames < 3 || warm < _startDelay)
        {
            warmFrames++;
            warm += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (var full in _lines)
        {
            if (_label != null) _label.text = "";

            // ── 타이핑 단계 (매 프레임 입력 체크) ──
            int shown = 0;
            float charTimer = 0f;
            while (shown < full.Length)
            {
                if (SkipAllPressed()) { yield return Release(); yield break; }
                if (AdvancePressed()) { shown = full.Length; break; } // 즉시 완성

                // 프레임당 delta 상한 — 한 프레임이 길어도 글자가 폭주하지 않게.
                charTimer += Mathf.Min(Time.unscaledDeltaTime, _charInterval * 2f);
                if (charTimer >= _charInterval)
                {
                    charTimer -= _charInterval;
                    shown++;
                    if (_label != null) _label.text = full.Substring(0, Mathf.Min(shown, full.Length));
                }
                yield return null;
            }
            if (_label != null) _label.text = full;
            yield return null; // 완성시킨 클릭 프레임 소비

            // ── 다음 컷 대기 (클릭으로 진행) ──
            while (true)
            {
                if (SkipAllPressed()) { yield return Release(); yield break; }
                if (AdvancePressed()) break;
                yield return null;
            }
            yield return null; // 진행 클릭 프레임 소비
        }

        yield return Release();
    }

    // 좌클릭 / 스페이스 / 엔터 = 진행(완성·다음)
    private bool AdvancePressed()
    {
        var m = Mouse.current;
        var k = Keyboard.current;
        return (m != null && m.leftButton.wasPressedThisFrame)
            || (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame));
    }

    // ESC = 전체 스킵
    private bool SkipAllPressed()
    {
        var k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
    }

    private IEnumerator Release()
    {
        if (_group != null)
        {
            float e = 0f;
            while (e < _fadeOut)
            {
                e += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(1f, 0f, _fadeOut > 0f ? e / _fadeOut : 1f);
                yield return null;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }
        Time.timeScale = 1f;
        if (_group != null) _group.gameObject.SetActive(false);
    }

    private void ReleaseImmediate()
    {
        if (_group != null) { _group.alpha = 0f; _group.blocksRaycasts = false; _group.gameObject.SetActive(false); }
    }
}
