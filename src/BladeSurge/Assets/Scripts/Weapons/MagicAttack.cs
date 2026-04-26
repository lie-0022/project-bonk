using UnityEngine;

/// <summary>
/// 마법 공격. GunAttack과 동일한 로직이며 이펙트만 다르다.
/// </summary>
public class MagicAttack : MonoBehaviour
{
    private GunAttack _gunAttack;

    private void Awake()
    {
        _gunAttack = GetComponent<GunAttack>();
        if (_gunAttack == null)
            _gunAttack = gameObject.AddComponent<GunAttack>();
    }

    public void Execute(WeaponSlot slot, PlayerStats stats)
    {
        _gunAttack.Execute(slot, stats);
    }
}
