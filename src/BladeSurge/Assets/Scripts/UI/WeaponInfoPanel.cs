using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 우측 무기 설명창. 선택된 캐릭터에 맞는 설명창 이미지를 통째로 표시한다.
/// 무기 아이콘·이름·설명이 이미 이미지에 그려져 있어 스프라이트 스왑만 한다.
/// CharacterSelectController.OnCharacterSelected 를 구독해 자동 갱신.
/// (확인 버튼 → GamePlay 진입은 맵 선택 완성 후 별도 연결 예정)
/// </summary>
public class WeaponInfoPanel : MonoBehaviour
{
    [Serializable]
    public class WeaponInfo
    {
        public CharacterType Character;
        public Sprite PanelSprite;
    }

    [Header("References")]
    [SerializeField] private CharacterSelectController _selectController;
    [SerializeField] private Image _panelImage;

    [Header("Character → 설명창 이미지")]
    [SerializeField] private WeaponInfo[] _infos = new WeaponInfo[3];

    private void OnEnable()
    {
        if (_selectController != null)
            _selectController.OnCharacterSelected += UpdatePanel;
    }

    private void OnDisable()
    {
        if (_selectController != null)
            _selectController.OnCharacterSelected -= UpdatePanel;
    }

    private void Start()
    {
        if (_selectController != null)
            UpdatePanel(_selectController.Selected);
    }

    /// <summary>지정 캐릭터에 맞는 설명창 이미지로 교체.</summary>
    public void UpdatePanel(CharacterType character)
    {
        if (_panelImage == null) return;

        foreach (WeaponInfo info in _infos)
        {
            if (info != null && info.Character == character && info.PanelSprite != null)
            {
                _panelImage.sprite = info.PanelSprite;
                return;
            }
        }
    }
}
