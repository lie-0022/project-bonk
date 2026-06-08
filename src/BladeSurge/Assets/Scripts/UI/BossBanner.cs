using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 보스 등장 시 화면 중앙에 보스 이름 배너를 잠깐 표시하고 페이드아웃한다.
/// BossEnemy.OnBossSpawned를 구독해 보스 이름(BossName)을 자동 표시 — 맵별로 다른 보스 이름이 나온다.
/// 2단 보스(보스1·보스2)도 각 스폰마다 표시된다.
/// </summary>
public class BossBanner : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _label;
    [Tooltip("보스 이름 뒤에 붙는 문구(등장 시).")]
    [SerializeField] private string _suffix = " 등장!";
    [Tooltip("보스 처치 시 표시 문구.")]
    [SerializeField] private string _clearText = "보스 처치!";
    [Tooltip("등장 배너 색.")]
    [SerializeField] private Color _spawnColor = new Color(0.92f, 0.16f, 0.16f, 1f);
    [Tooltip("처치 배너 색.")]
    [SerializeField] private Color _clearColor = new Color(0.96f, 0.82f, 0.22f, 1f);
    [Tooltip("완전 표시 유지 시간(초).")]
    [SerializeField] private float _holdTime = 1.6f;
    [Tooltip("페이드 인/아웃 시간(초).")]
    [SerializeField] private float _fadeTime = 0.5f;

    private Coroutine _routine;

    private void Awake() { if (_group != null) _group.alpha = 0f; }

    private void OnEnable()
    {
        BossEnemy.OnBossSpawned += OnBossSpawned;
        BossEnemy.OnBossDied += OnBossDied;
    }

    private void OnDisable()
    {
        BossEnemy.OnBossSpawned -= OnBossSpawned;
        BossEnemy.OnBossDied -= OnBossDied;
    }

    private void OnBossSpawned(BossEnemy boss)
    {
        ShowBanner((boss != null ? boss.BossName : "BOSS") + _suffix, _spawnColor);
    }

    private void OnBossDied(BossEnemy boss)
    {
        ShowBanner(_clearText, _clearColor);
    }

    private void ShowBanner(string message, Color color)
    {
        if (_label != null) { _label.text = message; _label.color = color; }
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Show());
    }

    private IEnumerator Show()
    {
        if (_group == null) yield break;

        // 페이드 인
        float e = 0f;
        while (e < _fadeTime)
        {
            e += Time.deltaTime;
            _group.alpha = Mathf.Lerp(0f, 1f, _fadeTime > 0f ? e / _fadeTime : 1f);
            yield return null;
        }
        _group.alpha = 1f;

        yield return new WaitForSeconds(_holdTime);

        // 페이드 아웃
        e = 0f;
        while (e < _fadeTime)
        {
            e += Time.deltaTime;
            _group.alpha = Mathf.Lerp(1f, 0f, _fadeTime > 0f ? e / _fadeTime : 1f);
            yield return null;
        }
        _group.alpha = 0f;
    }
}
