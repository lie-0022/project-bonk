using UnityEngine;

/// <summary>
/// 마법 공격. 가장 가까운 적 방향으로 느린 투사체 발사 → 적중 시 반경 폭발 (광역 피해).
/// 검(근접)/총(직선 고속)과 차별화된 광역 정체성. 출처: design/gdd/weapon-system.md
/// </summary>
public class MagicAttack : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;

    public void Execute(WeaponSlot slot, PlayerStats stats)
    {
        EnemyBase nearest = FindNearest(slot.FinalRange);
        if (nearest == null) return;

        Vector3 targetPoint = nearest.TryGetComponent<Collider>(out var col)
            ? col.bounds.center
            : nearest.transform.position;
        Vector3 dir = (targetPoint - _playerTransform.position).normalized;

        int count = stats.ProjectileCount;
        float spreadAngle = 8f;

        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (count - 1)) : 0f;
            Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * dir;
            SpawnProjectile(slot, stats, spreadDir);
        }

        Debug.Log($"[MagicAttack] lvl={slot.Level} dmg={slot.FinalDamage:F1} explosionR={slot.FinalExplosionRadius:F1} range={slot.FinalRange:F1}");
    }

    private void SpawnProjectile(WeaponSlot slot, PlayerStats stats, Vector3 direction)
    {
        GameObject obj = WeaponSystem.Instance.GetProjectile(slot.Data.WeaponType);
        if (obj == null) return;

        obj.transform.position = _playerTransform.position;
        obj.transform.rotation = Quaternion.LookRotation(direction);

        float speed = slot.Data.ProjectileSpeed * stats.ProjectileSpeed;
        obj.GetComponent<Projectile>()?.Setup(
            slot.FinalDamage,
            speed,
            slot.FinalRange,
            stats.CritChance,
            stats.CritMultiplier,
            stats.Lifesteal,
            _playerTransform,
            pierceCount: 0,
            explosionRadius: slot.FinalExplosionRadius);
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
