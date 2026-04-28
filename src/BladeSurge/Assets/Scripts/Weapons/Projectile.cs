using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 총·마법 공용 투사체. 발사 방향으로 직선 이동하다 적에 맞거나 최대 사거리 도달 시 소멸.
/// - 관통(pierce): -1 = 무한, 0 = 1명 명중 후 소멸, N = N명까지 추가 통과 (총 전용).
/// - 폭발(explosionRadius > 0): 첫 명중 시 반경 내 모든 적에 즉시 광역 피해 후 소멸 (마법 전용).
///   폭발이 켜져 있으면 관통은 무시된다.
/// </summary>
public class Projectile : MonoBehaviour, IPoolable
{
    private float _damage;
    private float _speed;
    private float _maxRange;
    private float _critChance;
    private float _critMultiplier;
    private float _lifesteal;
    private int _pierceRemaining;
    private float _explosionRadius;
    private Vector3 _startPosition;
    private Transform _playerTransform;
    private readonly HashSet<HealthComponent> _alreadyHit = new();
    private bool _isActive;

    public void Setup(float damage, float speed, float maxRange,
                      float critChance, float critMultiplier, float lifesteal,
                      Transform playerTransform,
                      int pierceCount = 0,
                      float explosionRadius = 0f)
    {
        _damage = damage;
        _speed = speed;
        _maxRange = maxRange;
        _critChance = critChance;
        _critMultiplier = critMultiplier;
        _lifesteal = lifesteal;
        _playerTransform = playerTransform;
        _pierceRemaining = pierceCount;
        _explosionRadius = explosionRadius;
        _alreadyHit.Clear();
        _startPosition = transform.position;
        _isActive = true;
    }

    public void OnSpawn() { _isActive = false; _alreadyHit.Clear(); }
    public void OnDespawn() { _isActive = false; _alreadyHit.Clear(); }

    private void Update()
    {
        if (!_isActive) return;

        transform.position += transform.forward * _speed * Time.deltaTime;

        if (Vector3.Distance(_startPosition, transform.position) >= _maxRange)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Enemy")) return;

        var health = other.GetComponent<HealthComponent>();
        if (health == null) return;

        // 폭발 모드: 첫 적과 충돌 시 반경 내 모두 광역 피해 후 소멸
        if (_explosionRadius > 0f)
        {
            DetonateExplosion(transform.position);
            ReturnToPool();
            return;
        }

        // 일반/관통 모드
        if (_alreadyHit.Contains(health)) return;
        _alreadyHit.Add(health);

        float finalDamage = ComputeDamage();
        DamageDealer.Deal(new DamageInfo(finalDamage, DamageSource.Player, gameObject), health);
        ApplyLifesteal(finalDamage);

        if (_pierceRemaining < 0) return;       // 무한 관통
        if (_pierceRemaining == 0) { ReturnToPool(); return; }
        _pierceRemaining--;
    }

    /// <summary>폭발 시 반경 내 모든 Enemy에게 광역 피해.</summary>
    private void DetonateExplosion(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(origin, _explosionRadius);
        int count = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var health = hit.GetComponent<HealthComponent>();
            if (health == null) continue;

            float finalDamage = ComputeDamage();
            DamageDealer.Deal(new DamageInfo(finalDamage, DamageSource.Player, gameObject), health);
            ApplyLifesteal(finalDamage);
            count++;
        }

        if (count > 0)
            Debug.Log($"[Projectile] Explosion radius={_explosionRadius:F1} hits={count}");
    }

    private float ComputeDamage() =>
        UnityEngine.Random.value < _critChance ? _damage * _critMultiplier : _damage;

    private void ApplyLifesteal(float damage)
    {
        if (_lifesteal <= 0f || _playerTransform == null) return;
        _playerTransform.GetComponent<HealthComponent>()?.Heal(damage * _lifesteal);
    }

    private void ReturnToPool()
    {
        _isActive = false;
        WeaponSystem.Instance.ReturnProjectile(gameObject);
    }
}
