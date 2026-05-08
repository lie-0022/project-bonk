using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 HP 바. BossEnemy.OnBossSpawned 이벤트로 자동 활성화되고,
/// 보스 HealthComponent.OnHealthChanged 를 구독해 Fill을 갱신한다.
/// 보스 사망 또는 OnBossDied 발생 시 자동 숨김.
/// 씬에 1개 인스턴스 배치 — 게임 시작 시 비활성 상태.
/// </summary>
public class BossHpBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _root;       // 활성/비활성 토글 대상
    [SerializeField] private Image _fillImage;       // Filled 타입 Image
    [SerializeField] private TMP_Text _nameText;     // 보스 이름
    [SerializeField] private TMP_Text _hpText;       // "HP / MaxHP" 표시

    private BossEnemy _boss;
    private HealthComponent _health;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        BossEnemy.OnBossSpawned += OnBossSpawned;
        BossEnemy.OnBossDied += OnBossDied;
    }

    private void OnDisable()
    {
        BossEnemy.OnBossSpawned -= OnBossSpawned;
        BossEnemy.OnBossDied -= OnBossDied;
        if (_health != null) _health.OnHealthChanged -= UpdateHp;
    }

    private void OnBossSpawned(BossEnemy boss)
    {
        _boss = boss;
        _health = boss.Health;
        if (_health == null) return;

        _health.OnHealthChanged += UpdateHp;

        if (_nameText != null) _nameText.text = boss.BossName;
        if (_fillImage != null) _fillImage.color = boss.HpBarColor;
        UpdateHp();
        SetVisible(true);
    }

    private void OnBossDied(BossEnemy boss)
    {
        SetVisible(false);
        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHp;
            _health = null;
        }
        _boss = null;
    }

    private void UpdateHp()
    {
        if (_health == null) return;
        if (_fillImage != null)
        {
            float ratio = _health.MaxHp > 0f ? _health.CurrentHp / _health.MaxHp : 0f;
            _fillImage.fillAmount = Mathf.Clamp01(ratio);
        }
        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(_health.CurrentHp)} / {Mathf.CeilToInt(_health.MaxHp)}";
    }

    private void SetVisible(bool visible)
    {
        if (_root != null && _root != gameObject)
        {
            _root.SetActive(visible);
            return;
        }
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(visible);
        var selfImage = GetComponent<Image>();
        if (selfImage != null) selfImage.enabled = visible;
    }
}
