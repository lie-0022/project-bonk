using System;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 캐릭터 선택 화면 중앙의 직업별 2D Spine 프리뷰. CharacterSelectController.OnCharacterSelected를
/// 구독해, 선택된 직업의 SkeletonGraphic만 활성화한다(나머지 숨김). 자산이 아직 없는 직업은 비워두면 됨.
/// </summary>
public class CharacterSelectPreview : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public CharacterType Type;
        public SkeletonGraphic Spine;
    }

    [SerializeField] private CharacterSelectController _controller;
    [SerializeField] private Entry[] _entries;

    private void OnEnable()
    {
        if (_controller != null) _controller.OnCharacterSelected += Show;
    }

    private void OnDisable()
    {
        if (_controller != null) _controller.OnCharacterSelected -= Show;
    }

    private void Start()
    {
        if (_controller != null) Show(_controller.Selected);
    }

    private void Show(CharacterType type)
    {
        if (_entries == null) return;
        foreach (var e in _entries)
        {
            if (e == null || e.Spine == null) continue;
            e.Spine.gameObject.SetActive(e.Type == type);
        }
    }
}
