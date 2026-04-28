using UnityEngine;

public enum WeaponType { None = 0, Sword, Gun, Magic }

/// <summary>
/// 무기 기본 수치 + 마일스톤 테이블. Assets/Data/Weapons/ 에 저장.
/// 마일스톤 인덱스: [0]=Lv1, [1]=Lv5, [2]=Lv10, [3]=Lv15
/// 사이 레벨(2~4, 6~9, 11~14)은 직전 마일스톤 값 사용.
/// 출처: design/gdd/weapon-system.md
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Identity")]
    public WeaponType WeaponType;
    public string WeaponName;

    [Header("Base Stats (Lv1)")]
    public float BaseDamage;
    public float BaseAttackInterval;
    public float BaseRange;

    [Header("Sword Only")]
    public float SweepAngle = 180f;
    public float SweepDuration = 0.2f;
    public float SweepSpeed = 360f;
    public float SwingInterval = 0.3f;
    [Tooltip("검 각도 마일스톤 [Lv1, Lv5, Lv10, Lv15]")]
    public float[] MilestoneSweepAngles = { 180f, 240f, 300f, 360f };

    [Header("Projectile Only (Gun / Magic)")]
    public float ProjectileSpeed = 15f;
    public GameObject ProjectilePrefab;

    [Header("Range Milestones [Lv1, Lv5, Lv10, Lv15]")]
    [Tooltip("기본 사거리 대비 배율. 검=반경, 총/마법=사거리")]
    public float[] MilestoneRangeMultipliers = { 1.0f, 1.4f, 1.8f, 2.5f };

    [Header("Gun Only — Pierce Milestones")]
    [Tooltip("관통 횟수. -1 = 무한 관통")]
    public int[] MilestonePierceCounts = { 0, 1, 2, -1 };

    [Header("Magic Only — Explosion Radius Milestones")]
    [Tooltip("폭발 반경 (월드 유닛)")]
    public float[] MilestoneExplosionRadii = { 2.0f, 3.0f, 4.5f, 7.0f };

    /// <summary>레벨에 해당하는 마일스톤 인덱스를 반환. (1~4 → 0, 5~9 → 1, 10~14 → 2, 15+ → 3)</summary>
    public static int GetMilestoneIndex(int level)
    {
        if (level >= 15) return 3;
        if (level >= 10) return 2;
        if (level >= 5) return 1;
        return 0;
    }

    public float GetSweepAngleAt(int level) =>
        SafeGet(MilestoneSweepAngles, GetMilestoneIndex(level), SweepAngle);

    public float GetRangeAt(int level) =>
        BaseRange * SafeGet(MilestoneRangeMultipliers, GetMilestoneIndex(level), 1f);

    public int GetPierceAt(int level) =>
        (int)SafeGet(System.Array.ConvertAll(MilestonePierceCounts, x => (float)x),
                     GetMilestoneIndex(level), 0f);

    public float GetExplosionRadiusAt(int level) =>
        SafeGet(MilestoneExplosionRadii, GetMilestoneIndex(level), 2f);

    private static float SafeGet(float[] array, int index, float fallback)
    {
        if (array == null || array.Length == 0) return fallback;
        if (index < 0) return array[0];
        if (index >= array.Length) return array[array.Length - 1];
        return array[index];
    }
}
