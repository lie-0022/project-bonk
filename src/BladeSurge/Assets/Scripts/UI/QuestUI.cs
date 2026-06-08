using TMPro;
using UnityEngine;

/// <summary>
/// 퀘스트(목표) 안내 텍스트. 인카운터/보스 진행에 따라 문구를 바꾼다.
/// - 인카운터(웨이브) 시작 시: "보스가 나올 때까지 살아남으세요!" (2단 보스의 각 페이즈마다 리셋)
/// - 보스 처치 후: "이동하세요!"
/// WaveSpawner.OnEncounterStarted / BossEnemy.OnBossDied 이벤트를 구독.
/// </summary>
public class QuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;
    [Tooltip("인카운터 시작/보스 처치 전 안내 (맵별 안내가 없을 때의 기본값)")]
    [SerializeField] private string _beforeBoss = "보스가 나올 때까지 살아남으세요!";
    [Tooltip("보스 처치 후 안내")]
    [SerializeField] private string _afterBoss = "이동하세요!";

    [Tooltip("맵별 '보스 전' 안내. 인덱스 = GameSession.SelectedMapIndex(0 슬라임/1 고블린/2 스켈레톤). 비거나 공백이면 기본값 사용.")]
    [SerializeField] private string[] _beforeBossByMap =
    {
        "슬라임 무리를 헤치고 보스가 나올 때까지 살아남으세요!",
        "고블린 소굴 — 보스가 나올 때까지 버티세요!",
        "해골 군단을 처치하고 본 로드를 쓰러뜨리세요!",
    };

    /// <summary>현재 맵에 맞는 '보스 전' 안내 문구. 맵별 설정이 있으면 그것을, 없으면 기본값.</summary>
    private string BeforeText
    {
        get
        {
            int idx = GameSession.SelectedMapIndex;
            if (_beforeBossByMap != null && idx >= 0 && idx < _beforeBossByMap.Length
                && !string.IsNullOrWhiteSpace(_beforeBossByMap[idx]))
                return _beforeBossByMap[idx];
            return _beforeBoss;
        }
    }

    private void OnEnable()
    {
        BossEnemy.OnBossDied += OnBossDied;
        WaveSpawner.OnEncounterStarted += OnEncounterStarted;
        SetText(BeforeText); // 시작/재활성 시 맵별 안내
    }

    private void OnDisable()
    {
        BossEnemy.OnBossDied -= OnBossDied;
        WaveSpawner.OnEncounterStarted -= OnEncounterStarted;
    }

    // 새 인카운터(다음 홀/페이즈)가 시작되면 다시 맵별 '보스 전' 안내로 되돌린다.
    private void OnEncounterStarted() => SetText(BeforeText);

    private void OnBossDied(BossEnemy boss)
    {
        SetText(_afterBoss);
    }

    private void SetText(string s)
    {
        if (_label != null) _label.text = s;
    }
}
