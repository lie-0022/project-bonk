using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// XP 게이지 + 레벨 텍스트 UI. XPSystem 이벤트를 구독해 갱신한다.
/// </summary>
public class XpBarUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _levelText;

    private void OnEnable()
    {
        XPSystem.OnXPChanged += OnXPChanged;
        XPSystem.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        XPSystem.OnXPChanged -= OnXPChanged;
        XPSystem.OnLevelUp -= OnLevelUp;
    }

    private void Start()
    {
        Refresh(0f, 1f, 1);
    }

    private void OnXPChanged(float currentXP, float xpToNext, int level)
    {
        float ratio = xpToNext > 0f ? currentXP / xpToNext : 0f;
        Refresh(ratio, xpToNext, level);
    }

    private void OnLevelUp(int level)
    {
        if (_levelText != null)
            _levelText.text = $"Lv.{level}";
    }

    private void Refresh(float ratio, float xpToNext, int level)
    {
        if (_fillImage != null)
            _fillImage.fillAmount = Mathf.Clamp01(ratio);

        if (_levelText != null)
            _levelText.text = $"Lv.{level}";
    }
}
