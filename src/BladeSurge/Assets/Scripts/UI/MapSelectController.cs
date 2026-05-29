using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 맵 선택란. 3개 스테이지 썸네일.
/// 잠금 해제된 맵만 선택 가능하며, 선택 시 전체 화면 배경을 해당 맵 배경으로 교체한다.
/// 잠긴 맵(2·3)은 미개방 이미지로 표시되고 클릭되지 않는다.
/// 선택 시 OnMapSelected(index) 발행 → 확인 버튼이 진입할 맵을 결정.
/// </summary>
public class MapSelectController : MonoBehaviour
{
    [Serializable]
    public class MapEntry
    {
        public int Index;
        public bool Unlocked;
        public Button Button;
        public Image Thumbnail;
        public Sprite DefaultSprite;
        public Sprite SelectedSprite;
        public Sprite LockedSprite;
        [Tooltip("선택 시 전체 화면 배경으로 사용할 스프라이트")]
        public Sprite BackgroundSprite;
    }

    [Header("References")]
    [SerializeField] private Image _screenBackground;

    [Header("Maps (3개)")]
    [SerializeField] private MapEntry[] _maps = new MapEntry[3];

    [Header("Initial Selection")]
    [SerializeField] private int _defaultMapIndex;

    /// <summary>맵 선택이 바뀔 때 발행. 인자는 선택된 맵 인덱스.</summary>
    public event Action<int> OnMapSelected;

    /// <summary>현재 선택된 맵 인덱스.</summary>
    public int Selected { get; private set; } = -1;

    private void Start()
    {
        foreach (MapEntry map in _maps)
        {
            if (map == null) continue;

            // 잠긴 맵은 미개방 이미지 고정 + 클릭 불가
            if (!map.Unlocked)
            {
                if (map.Thumbnail != null && map.LockedSprite != null)
                    map.Thumbnail.sprite = map.LockedSprite;
                if (map.Button != null) map.Button.interactable = false;
                continue;
            }

            if (map.Button != null)
            {
                int captured = map.Index;
                map.Button.onClick.AddListener(() => Select(captured));
            }
        }

        Select(_defaultMapIndex);
    }

    /// <summary>지정 맵을 선택. 잠긴 맵이면 무시.</summary>
    public void Select(int index)
    {
        MapEntry chosen = FindMap(index);
        if (chosen == null || !chosen.Unlocked) return;

        Selected = index;

        foreach (MapEntry map in _maps)
        {
            if (map == null || !map.Unlocked || map.Thumbnail == null) continue;
            Sprite target = map.Index == index ? map.SelectedSprite : map.DefaultSprite;
            if (target != null) map.Thumbnail.sprite = target;
        }

        if (_screenBackground != null && chosen.BackgroundSprite != null)
            _screenBackground.sprite = chosen.BackgroundSprite;

        OnMapSelected?.Invoke(index);
    }

    private MapEntry FindMap(int index)
    {
        foreach (MapEntry map in _maps)
            if (map != null && map.Index == index) return map;
        return null;
    }
}
