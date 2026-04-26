using UnityEngine;

/// <summary>
/// 총 공격. 가장 가까운 적 방향으로 투사체를 발사한다.
/// 발사체 수만큼 동시에 약간 퍼져서 발사한다.
/// </summary>
public class GunAttack : MonoBehaviour
{
    private Transform _playerTransform;

    private void Awake()
    {
        _playerTransform = transform.root;
    }

    public void Execute(WeaponSlot slot, PlayerStats stats)
    {
        EnemyBase nearest = FindNearest(slot.Data.BaseRange);
        if (nearest == null) return;

        Vector3 dir = (nearest.transform.position - _playerTransform.position);
        dir.y = 0f;
        dir.Normalize();

        int count = stats.ProjectileCount;
        float spreadAngle = 10f; // 발사체 수 > 1일 때 퍼짐 각도

        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (count - 1)) : 0f;
            Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * dir;
            SpawnProjectile(slot, stats, spreadDir);
        }
    }

    private void SpawnProjectile(WeaponSlot slot, PlayerStats stats, Vector3 direction)
    {
        GameObject obj = WeaponSystem.Instance.GetProjectile(slot.Data.WeaponType);
        if (obj == null) return;

        obj.transform.position = _playerTransform.position + Vector3.up * 1f;
        obj.transform.rotation = Quaternion.LookRotation(direction);

        float speed = slot.Data.ProjectileSpeed * stats.ProjectileSpeed;
        obj.GetComponent<Projectile>()?.Setup(
            slot.Data.BaseDamage,
            speed,
            slot.Data.BaseRange,
            stats.SizeMultiplier,
            stats.CritChance,
            stats.CritMultiplier,
            stats.Lifesteal,
            _playerTransform);
    }

    private EnemyBase FindNearest(float range)
    {
        EnemyBase nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(_playerTransform.position, enemy.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
