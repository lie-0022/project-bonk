using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 온라인 리더보드 패널. 닉네임 입력 → 현재 런 점수(RunTotals.Score) 등록 → 상위 순위 표시.
/// GameClear 화면 등에 배치. OnlineLeaderboard(네트워크)를 사용.
/// </summary>
public class LeaderboardPanelUI : MonoBehaviour
{
    [SerializeField] private OnlineLeaderboard _service;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Button _submitButton;
    [SerializeField] private TMP_Text _statusText;
    [Tooltip("순위 목록 (여러 줄 텍스트).")]
    [SerializeField] private TMP_Text _listText;
    [SerializeField] private int _topCount = 10;

    private const string NickKey = "BladeSurge.Nickname";
    private bool _submitted;

    private void Awake()
    {
        if (_service == null) _service = OnlineLeaderboard.Instance;
        if (_submitButton != null) _submitButton.onClick.AddListener(OnSubmit);
        if (_nicknameInput != null) _nicknameInput.text = PlayerPrefs.GetString(NickKey, "");
    }

    private void OnEnable()
    {
        if (_service == null) _service = OnlineLeaderboard.Instance;
        if (_service == null || !_service.IsConfigured)
        {
            SetStatus("온라인 랭킹 미설정");
            SetList("(DB 미연결)");
            return;
        }
        Refresh();
    }

    private void OnSubmit()
    {
        if (_submitted) { SetStatus("이미 등록됨"); return; }
        if (_service == null || !_service.IsConfigured) { SetStatus("온라인 랭킹 미설정"); return; }

        string nick = _nicknameInput != null ? _nicknameInput.text : "익명";
        PlayerPrefs.SetString(NickKey, nick);
        PlayerPrefs.Save();

        SetStatus("등록 중...");
        if (_submitButton != null) _submitButton.interactable = false;

        _service.SubmitScore(nick, RunTotals.Score, ok =>
        {
            _submitted = ok;
            SetStatus(ok ? "등록 완료!" : "등록 실패 (네트워크 확인)");
            if (!ok && _submitButton != null) _submitButton.interactable = true;
            if (ok) Refresh();
        });
    }

    private void Refresh()
    {
        if (_service == null || !_service.IsConfigured) return;
        SetList("불러오는 중...");
        _service.FetchTop(_topCount, list =>
        {
            if (list == null) { SetList("불러오기 실패"); return; }
            if (list.Count == 0) { SetList("아직 기록 없음 — 첫 주자가 되어보세요!"); return; }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
                sb.AppendLine($"{i + 1}.   {list[i].nickname}      {list[i].score:N0}");
            SetList(sb.ToString());
        });
    }

    private void SetStatus(string s) { if (_statusText != null) _statusText.text = s; }
    private void SetList(string s) { if (_listText != null) _listText.text = s; }
}
