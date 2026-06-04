using UnityEngine;

/// <summary>
/// 선택한 무기(GameSession.SelectedWeapon)에 따라 플레이어 캐릭터 외형 모델을 전환한다.
/// Visual_Root 아래 무기별 Visual GameObject(Sword/Gun/Magic) 중 하나만 활성화한다.
/// 캐릭터 선택은 시작 시 1회 고정(전사→Sword, 마법사→Magic, 거너→Gun).
/// </summary>
public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("무기별 캐릭터 모델 (Visual_Root 자식)")]
    [SerializeField] private GameObject _visualSword;
    [SerializeField] private GameObject _visualGun;
    [SerializeField] private GameObject _visualMagic;

    private void Start()
    {
        Apply(GameSession.SelectedWeapon);
    }

    /// <summary>무기 타입에 맞는 캐릭터 모델만 활성화하고 나머지는 끈다.</summary>
    public void Apply(WeaponType weapon)
    {
        if (_visualSword != null) _visualSword.SetActive(weapon == WeaponType.Sword);
        if (_visualGun != null) _visualGun.SetActive(weapon == WeaponType.Gun);
        if (_visualMagic != null) _visualMagic.SetActive(weapon == WeaponType.Magic);
    }
}
