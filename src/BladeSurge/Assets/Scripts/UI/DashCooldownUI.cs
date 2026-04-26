using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대시 쿨타임 아이콘 UI. PlayerController.DashCooldownNormalized를 매 프레임 폴링한다.
/// </summary>
public class DashCooldownUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    private PlayerController _player;

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null)
            _player = go.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (_fillImage == null || _player == null) return;
        // 1=방금 사용(어두움), 0=준비됨(밝음)
        _fillImage.fillAmount = _player.DashCooldownNormalized;
    }
}
