using UnityEngine;

public enum WeaponType { None = 0, Sword, Gun, Magic }

/// <summary>
/// 무기 기본 수치를 담는 ScriptableObject. Assets/Data/Weapons/ 에 저장.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Identity")]
    public WeaponType WeaponType;
    public string WeaponName;

    [Header("Base Stats")]
    public float BaseDamage;
    public float BaseAttackInterval;
    public float BaseRange;

    [Header("Sword Only")]
    public float SweepAngle = 60f;
    public float SweepDuration = 0.2f;
    public float SweepSpeed = 360f;
    public float SwingInterval = 0.3f;

    [Header("Projectile Only (Gun / Magic)")]
    public float ProjectileSpeed = 15f;
    public GameObject ProjectilePrefab;
}
