using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 종류. 무기와 1:1 대응 (전사=Sword, 마법사=Magic, 거너=Gun).
/// </summary>
public enum CharacterType
{
    Warrior,
    Mage,
    Gunner
}

/// <summary>
/// 캐릭터 선택창(좌측 패널) 컨트롤러.
/// 3행(전사/마법사/거너)을 클릭하면 해당 캐릭터를 선택하고,
/// 아이콘 스프라이트를 선택본으로 스왑 + 이름 색을 하이라이트한다.
/// 항상 1개만 선택되며, 선택 시 OnCharacterSelected 이벤트를 발행한다.
/// 무기 설명창(Step 3)·모델 프리뷰(Step 5)는 이 이벤트를 구독해 갱신할 예정.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    [Serializable]
    public class CharacterRow
    {
        public CharacterType Type;
        public Button Button;
        public Image Icon;
        public TextMeshProUGUI NameLabel;
        public Sprite IconDefault;
        public Sprite IconSelected;
    }

    [Header("Rows (전사 / 마법사 / 거너)")]
    [SerializeField] private CharacterRow[] _rows = new CharacterRow[3];

    [Header("Name Colors")]
    [SerializeField] private Color _selectedColor = new(1f, 1f, 1f);
    [SerializeField] private Color _unselectedColor = new(0.7f, 0.72f, 0.74f);

    [Header("Initial Selection")]
    [SerializeField] private CharacterType _defaultSelection = CharacterType.Warrior;

    /// <summary>선택이 바뀔 때 발행. 인자는 새로 선택된 캐릭터.</summary>
    public event Action<CharacterType> OnCharacterSelected;

    /// <summary>현재 선택된 캐릭터.</summary>
    public CharacterType Selected { get; private set; }

    private void Start()
    {
        foreach (CharacterRow row in _rows)
        {
            if (row?.Button == null) continue;
            CharacterType captured = row.Type;
            row.Button.onClick.AddListener(() => Select(captured));
        }

        Select(_defaultSelection);
    }

    /// <summary>지정 캐릭터를 선택하고 모든 행의 시각 상태를 갱신한다.</summary>
    public void Select(CharacterType type)
    {
        Selected = type;

        foreach (CharacterRow row in _rows)
        {
            if (row == null) continue;
            bool isSelected = row.Type == type;

            if (row.Icon != null)
            {
                Sprite target = isSelected ? row.IconSelected : row.IconDefault;
                if (target != null) row.Icon.sprite = target;
            }

            if (row.NameLabel != null)
                row.NameLabel.color = isSelected ? _selectedColor : _unselectedColor;
        }

        OnCharacterSelected?.Invoke(type);
    }
}
