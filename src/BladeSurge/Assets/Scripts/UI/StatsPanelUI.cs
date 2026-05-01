using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tab 키 토글 스탯창. PlayerStats 최종값을 한 화면에 표시.
/// 열려있는 동안 Time.timeScale=0 (레벨업 UI와 동일 패턴).
/// 레벨업 선택 중에는 Tab 입력 무시 (timeScale 충돌 방지).
/// MVP 최소 구현 — 디자인 디테일은 추후.
/// </summary>
public class StatsPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _statsText;

    private bool _isOpen;
    private float _previousTimeScale = 1f;

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += RefreshIfOpen;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsChanged -= RefreshIfOpen;
        if (_isOpen) Close();
    }

    private void Start()
    {
        SetVisible(false);
        _isOpen = false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb.tabKey.wasPressedThisFrame) return;

        var levelup = LevelupWeaponSelection.Instance;
        if (levelup != null && levelup.IsSelecting) return;

        if (_isOpen) Close();
        else Open();
    }

    private void Open()
    {
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetVisible(true);
        _isOpen = true;
        Refresh();
    }

    private void Close()
    {
        Time.timeScale = _previousTimeScale;
        SetVisible(false);
        _isOpen = false;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _canvasGroup.interactable = visible;
    }

    private void RefreshIfOpen()
    {
        if (_isOpen) Refresh();
    }

    private void Refresh()
    {
        if (_statsText == null) return;
        var s = PlayerStats.Instance;
        if (s == null) { _statsText.text = "PlayerStats 없음"; return; }

        _statsText.text =
            $"이동 속도\t{s.MoveSpeed:F2}\n" +
            $"추가 점프\t{s.ExtraJumps}\n" +
            $"최대 HP\t{s.MaxHp:F0}\n" +
            $"HP 재생\t{s.HpRegen:F1} /s\n" +
            $"회피 확률\t{s.DodgeChance:P0}\n" +
            $"공격 속도\t{s.AttackSpeedMultiplier:F2}\n" +
            $"발사체 속도\t{s.ProjectileSpeed:F2}\n" +
            $"치명타 확률\t{s.CritChance:P0}\n" +
            $"치명타 배율\t{s.CritMultiplier:F2}\n" +
            $"생명 흡수\t{s.Lifesteal:P0}\n" +
            $"발사체 수\t{s.ProjectileCount}\n" +
            $"행운\t{s.LuckChance:P0}\n" +
            $"난이도(스폰)\t{s.DifficultySpawnMultiplier:F2}\n" +
            $"난이도(보상)\t{s.DifficultyRewardMultiplier:F2}";
    }
}
