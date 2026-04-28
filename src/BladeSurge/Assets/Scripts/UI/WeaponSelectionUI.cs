using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 레벨업 무기 선택 UI. 패널 + 3개 카드 버튼.
/// LevelupWeaponSelection.OnSelectionRequired 수신 → 카드 그리기 → 클릭 또는 1/2/3 키로 선택.
/// 출처: design/gdd/weapon-selection-ui.md
/// </summary>
public class WeaponSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class CardView
    {
        public GameObject Root;
        public Button Button;
        public Image GradeFrame;
        public TextMeshProUGUI WeaponNameText;
        public TextMeshProUGUI DescriptionText;
        public TextMeshProUGUI GradeText;
        public TextMeshProUGUI StateText; // 신규 / 강화 Lv X → Lv X+1
    }

    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Cards (max 3)")]
    [SerializeField] private CardView[] _cards = new CardView[3];

    [Header("Grade Colors")]
    [SerializeField] private Color _commonColor = new(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color _epicColor   = new(0.6f, 0.3f, 0.9f);
    [SerializeField] private Color _uniqueColor = new(1.0f, 0.85f, 0.2f);
    [SerializeField] private Color _legendColor = new(0.95f, 0.25f, 0.25f);

    private readonly List<LevelupWeaponSelection.WeaponChoice> _currentChoices = new();
    private bool _isOpen;

    private void Awake()
    {
        Hide();
    }

    private void OnEnable()
    {
        LevelupWeaponSelection.OnSelectionRequired += Show;
    }

    private void OnDisable()
    {
        LevelupWeaponSelection.OnSelectionRequired -= Show;
    }

    private void Update()
    {
        if (!_isOpen) return;

        // 1/2/3 단축키 (unscaled — Time.timeScale = 0 상황 대응)
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) TrySelect(0);
        else if (kb.digit2Key.wasPressedThisFrame) TrySelect(1);
        else if (kb.digit3Key.wasPressedThisFrame) TrySelect(2);
    }

    private void Show(IReadOnlyList<LevelupWeaponSelection.WeaponChoice> choices)
    {
        _currentChoices.Clear();
        _currentChoices.AddRange(choices);

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null) continue;

            if (i < choices.Count)
            {
                BindCard(_cards[i], choices[i], i);
                _cards[i].Root?.SetActive(true);
            }
            else
            {
                _cards[i].Root?.SetActive(false);
            }
        }

        if (_panelRoot != null) _panelRoot.SetActive(true);
        _isOpen = true;
    }

    private void Hide()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
        _isOpen = false;
    }

    private void BindCard(CardView view, LevelupWeaponSelection.WeaponChoice choice, int index)
    {
        Color gradeColor = GetGradeColor(choice.Grade);

        if (view.GradeFrame != null) view.GradeFrame.color = gradeColor;
        if (view.WeaponNameText != null) view.WeaponNameText.text = GetWeaponName(choice.WeaponType);
        if (view.DescriptionText != null) view.DescriptionText.text = GetWeaponDescription(choice.WeaponType);
        if (view.GradeText != null)
        {
            view.GradeText.text = $"[{GetGradeLabel(choice.Grade)}]";
            view.GradeText.color = gradeColor;
        }
        if (view.StateText != null)
        {
            view.StateText.text = choice.IsNew
                ? "신규"
                : $"강화 Lv {choice.CurrentLevel} → Lv {choice.CurrentLevel + 1}";
        }

        if (view.Button != null)
        {
            view.Button.interactable = true;
            view.Button.onClick.RemoveAllListeners();
            int captured = index;
            view.Button.onClick.AddListener(() => TrySelect(captured));
        }
    }

    private void TrySelect(int index)
    {
        if (!_isOpen) return;
        if (index < 0 || index >= _currentChoices.Count) return;

        // 중복 선택 방지: 모든 카드 비활성화
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i]?.Button != null) _cards[i].Button.interactable = false;
        }

        var chosen = _currentChoices[index];
        Hide();
        LevelupWeaponSelection.Instance?.ChooseWeapon(chosen);
    }

    private Color GetGradeColor(CardGrade grade) => grade switch
    {
        CardGrade.Common => _commonColor,
        CardGrade.Epic   => _epicColor,
        CardGrade.Unique => _uniqueColor,
        CardGrade.Legend => _legendColor,
        _                => _commonColor,
    };

    private static string GetGradeLabel(CardGrade grade) => grade switch
    {
        CardGrade.Common => "커먼",
        CardGrade.Epic   => "에픽",
        CardGrade.Unique => "유니크",
        CardGrade.Legend => "레전드",
        _                => "?",
    };

    private static string GetWeaponName(WeaponType type) => type switch
    {
        WeaponType.Sword => "검",
        WeaponType.Gun   => "총",
        WeaponType.Magic => "마법",
        _                => "?",
    };

    private static string GetWeaponDescription(WeaponType type) => type switch
    {
        WeaponType.Sword => "주변 부채꼴 베기",
        WeaponType.Gun   => "고속 관통 투사체",
        WeaponType.Magic => "느린 폭발 투사체",
        _                => string.Empty,
    };
}
