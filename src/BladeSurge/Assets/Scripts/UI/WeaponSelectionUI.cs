using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 레벨업 선택 UI. 패널 + 3개 카드 버튼.
/// LevelupWeaponSelection.OnSelectionRequired 수신 → 카드 그리기 → 클릭 또는 1/2/3 키로 선택.
/// 무기와 패시브 카드를 동일한 슬롯에 표시한다.
/// 출처: design/gdd/weapon-selection-ui.md, design/gdd/spec-system.md
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

    private readonly List<LevelupChoice> _currentChoices = new();
    private bool _isOpen;

    // 커서 상태 백업 (Show 시 잠금 해제, Hide 시 복원)
    private CursorLockMode _previousLockState;
    private bool _previousCursorVisible;

    private void Awake()
    {
        // 구독은 Awake에서 (GameObject 비활성화 후에도 이벤트 받을 수 있도록)
        LevelupWeaponSelection.OnSelectionRequired += Show;
        Debug.Log($"[WeaponSelectionUI] Awake 완료. 구독 등록. panelRoot={(_panelRoot != null ? _panelRoot.name : "NULL")}");
        Hide();
    }

    private void OnDestroy()
    {
        LevelupWeaponSelection.OnSelectionRequired -= Show;
    }

    private void Update()
    {
        if (!_isOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) TrySelect(0);
        else if (kb.digit2Key.wasPressedThisFrame) TrySelect(1);
        else if (kb.digit3Key.wasPressedThisFrame) TrySelect(2);
    }

    private void Show(IReadOnlyList<LevelupChoice> choices)
    {
        Debug.Log($"[WeaponSelectionUI] Show 호출. choices={choices.Count}");
        _currentChoices.Clear();
        _currentChoices.AddRange(choices);

        if (_cards != null)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var view = _cards[i];
                if (view == null) continue;

                bool shouldShow = i < choices.Count;

                if (shouldShow)
                {
                    BindCard(view, choices[i], i);
                }

                // Unity-safe null check (destroyed object 대응)
                if (view.Root != null)
                {
                    view.Root.SetActive(shouldShow);
                }
            }
        }

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WeaponSelectionUI] _panelRoot 가 매핑 안됨. Inspector 확인 필요.");
        }

        // 커서 잠금 해제 (현재 상태 백업 후 마우스 활성화)
        _previousLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _isOpen = true;
    }

    private void Hide()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);

        // 커서 상태 복원 (Show 전에 호출되면 백업값이 디폴트라 게임 시작 시 무해)
        if (_isOpen)
        {
            Cursor.lockState = _previousLockState;
            Cursor.visible = _previousCursorVisible;
        }

        _isOpen = false;
    }

    private void BindCard(CardView view, LevelupChoice choice, int index)
    {
        Color gradeColor = GetGradeColor(choice.Grade);

        if (view.GradeFrame != null) view.GradeFrame.color = gradeColor;
        if (view.WeaponNameText != null) view.WeaponNameText.text = GetCardName(choice);
        if (view.DescriptionText != null) view.DescriptionText.text = GetCardDescription(choice);
        if (view.GradeText != null)
        {
            view.GradeText.text = $"[{GetGradeLabel(choice.Grade)}]";
            view.GradeText.color = gradeColor;
        }
        if (view.StateText != null)
        {
            view.StateText.text = choice.IsNew
                ? "신규"
                : $"Lv {choice.CurrentLevel} → Lv {choice.CurrentLevel + 1}";
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

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i]?.Button != null) _cards[i].Button.interactable = false;
        }

        var chosen = _currentChoices[index];
        Hide();
        LevelupWeaponSelection.Instance?.Choose(chosen);
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

    private static string GetCardName(LevelupChoice choice) => choice.Kind switch
    {
        LevelupChoiceKind.Weapon  => GetWeaponName(choice.WeaponType),
        LevelupChoiceKind.Passive => GetPassiveName(choice.PassiveType),
        _                         => "?",
    };

    private static string GetCardDescription(LevelupChoice choice) => choice.Kind switch
    {
        LevelupChoiceKind.Weapon  => GetWeaponDescription(choice.WeaponType),
        LevelupChoiceKind.Passive => GetPassiveDescription(choice.PassiveType),
        _                         => string.Empty,
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

    private static string GetPassiveName(PassiveType type) => type switch
    {
        PassiveType.MaxHp           => "최대 체력",
        PassiveType.HpRegen         => "체력 재생",
        PassiveType.Dodge           => "회피",
        PassiveType.AttackSpeed     => "공격 속도",
        PassiveType.CritChance      => "치명타 확률",
        PassiveType.CritDamage      => "치명타 피해",
        PassiveType.Lifesteal       => "생명 흡수",
        PassiveType.ProjectileCount => "발사체 수",
        PassiveType.MoveSpeed       => "이동 속도",
        PassiveType.ExtraJump       => "추가 점프",
        PassiveType.Luck            => "행운",
        PassiveType.Difficulty      => "난이도",
        _                           => "?",
    };

    private static string GetPassiveDescription(PassiveType type) => type switch
    {
        PassiveType.MaxHp           => "최대 HP +15/Lv",
        PassiveType.HpRegen         => "HP 회복 +0.5/s/Lv",
        PassiveType.Dodge           => "피해 회피 (cap 60%)",
        PassiveType.AttackSpeed     => "공속/발사체 속도 ↑ (floor 0.30)",
        PassiveType.CritChance      => "치명타 (cap 75%)",
        PassiveType.CritDamage      => "치명타 피해 +15%/Lv",
        PassiveType.Lifesteal       => "데미지의 +2%/Lv 흡혈 (cap 25%)",
        PassiveType.ProjectileCount => "발사체 수 +1 (3Lv마다)",
        PassiveType.MoveSpeed       => "이동 속도 +1.0/Lv",
        PassiveType.ExtraJump       => "추가 점프 +1/Lv",
        PassiveType.Luck            => "고등급 카드 확률 (cap 80%)",
        PassiveType.Difficulty      => "스폰+15%/Lv, 보상+20%/Lv",
        _                           => string.Empty,
    };
}
