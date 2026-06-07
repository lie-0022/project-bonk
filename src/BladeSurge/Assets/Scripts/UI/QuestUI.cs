using TMPro;
using UnityEngine;

/// <summary>
/// 퀘스트(목표) 안내 텍스트. 보스 처치 전/후로 문구를 바꾼다.
/// - 보스 처치 전: "보스가 나올 때까지 살아남으세요!"
/// - 보스 처치 후: "이동하세요!"
/// BossEnemy.OnBossDied 이벤트를 구독.
/// </summary>
public class QuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;
    [Tooltip("보스 처치 전 안내")]
    [SerializeField] private string _beforeBoss = "보스가 나올 때까지 살아남으세요!";
    [Tooltip("보스 처치 후 안내")]
    [SerializeField] private string _afterBoss = "이동하세요!";

    private void OnEnable()
    {
        BossEnemy.OnBossDied += OnBossDied;
        SetText(_beforeBoss); // 시작/재활성 시 기본 안내
    }

    private void OnDisable()
    {
        BossEnemy.OnBossDied -= OnBossDied;
    }

    private void OnBossDied(BossEnemy boss)
    {
        SetText(_afterBoss);
    }

    private void SetText(string s)
    {
        if (_label != null) _label.text = s;
    }
}
