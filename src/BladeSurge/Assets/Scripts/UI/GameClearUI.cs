using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 클리어 화면. 최종 스테이지 클리어(GameState.Win) 시 GameManager가 이 씬을 로드한다.
/// 3개 스테이지 누적 점수(RunTotals)를 표시하고 최고 점수를 갱신/표시한다.
/// '메인 화면' 버튼 → 타이틀(MainMenu) 씬으로 이동. 진입 시 커서 잠금 해제.
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [SerializeField] private Button _mainScreenButton;
    [SerializeField] private string _mainMenuScene = "MainMenu";

    [Header("Score (TMP_Text — 선택)")]
    [Tooltip("최종 총점.")]
    [SerializeField] private TMP_Text _scoreText;
    [Tooltip("점수 내역(스테이지/처치/골드/레벨/시간).")]
    [SerializeField] private TMP_Text _breakdownText;
    [Tooltip("최고 점수 / 신기록 표시.")]
    [SerializeField] private TMP_Text _bestText;

    private void Awake()
    {
        if (_mainScreenButton != null) _mainScreenButton.onClick.AddListener(OnMainScreen);

        // 클리어 화면은 마우스 조작 화면 — 커서 잠금 해제.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        PopulateScore();
    }

    private void PopulateScore()
    {
        bool isNewBest = RunTotals.TrySaveBest();

        if (_scoreText != null)
            _scoreText.text = RunTotals.Score.ToString("N0");

        if (_breakdownText != null)
            _breakdownText.text =
                $"스테이지 클리어   {RunTotals.StagesCleared}\n" +
                $"처치 수   {RunTotals.TotalKills}\n" +
                $"획득 골드   {RunTotals.TotalGold}\n" +
                $"최고 레벨   {RunTotals.HighestLevel}\n" +
                $"총 시간   {RunTotals.FormattedTotalTime}";

        if (_bestText != null)
            _bestText.text = isNewBest
                ? $"신기록!  BEST {RunTotals.BestScore:N0}"
                : $"BEST {RunTotals.BestScore:N0}";
    }

    private void OnMainScreen()
    {
        SceneManager.LoadScene(_mainMenuScene);
    }
}
