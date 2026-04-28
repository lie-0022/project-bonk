using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 HP 바 UI. HealthComponent 이벤트를 구독해 갱신한다.
/// </summary>
public class HpBarUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _hpText;

    private HealthComponent _health;

    private void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[HpBarUI] 'Player' 태그 오브젝트를 찾을 수 없습니다.");
            return;
        }

        _health = player.GetComponent<HealthComponent>();
        if (_health == null)
        {
            Debug.LogWarning("[HpBarUI] HealthComponent를 찾을 수 없습니다.");
            return;
        }

        _health.OnHealthChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHealthChanged -= Refresh;
    }

    private void Refresh()
    {
        float ratio = _health.MaxHp > 0f ? _health.CurrentHp / _health.MaxHp : 0f;

        if (_fillImage != null)
            _fillImage.fillAmount = ratio;

        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(_health.CurrentHp)} / {Mathf.CeilToInt(_health.MaxHp)}";
    }
}
