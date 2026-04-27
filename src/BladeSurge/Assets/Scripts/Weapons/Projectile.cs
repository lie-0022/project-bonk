using UnityEngine;

/// <summary>
/// 총·마법 공용 투사체. 발사 방향으로 직선 이동하다 적에 맞거나 최대 사거리 도달 시 소멸.
/// </summary>
public class Projectile : MonoBehaviour, IPoolable
{
    private float _damage;
    private float _speed;
    private float _maxRange;
    private float _size;
    private float _critChance;
    private float _critMultiplier;
    private float _lifesteal;
    private Vector3 _startPosition;
    private Vector3 _baseScale;            // 프리팹의 원본 스케일 (한 번만 캐싱)
    private bool _baseScaleCached;
    private Transform _playerTransform;
    private bool _isActive;

    public void Setup(float damage, float speed, float maxRange, float size,
                      float critChance, float critMultiplier, float lifesteal,
                      Transform playerTransform)
    {
        if (!_baseScaleCached)
        {
            _baseScale = transform.localScale;
            _baseScaleCached = true;
        }

        _damage = damage;
        _speed = speed;
        _maxRange = maxRange;
        _size = size;
        _critChance = critChance;
        _critMultiplier = critMultiplier;
        _lifesteal = lifesteal;
        _playerTransform = playerTransform;
        _startPosition = transform.position;
        transform.localScale = _baseScale * size;
        _isActive = true;
    }

    public void OnSpawn() { _isActive = false; }
    public void OnDespawn() { _isActive = false; }

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

        float finalDamage = UnityEngine.Random.value < _critChance
            ? _damage * _critMultiplier
            : _damage;

        DamageDealer.Deal(new DamageInfo(finalDamage, DamageSource.Player, gameObject), health);

        if (_lifesteal > 0f && _playerTransform != null)
            _playerTransform.GetComponent<HealthComponent>()?.Heal(finalDamage * _lifesteal);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _isActive = false;
        WeaponSystem.Instance.ReturnProjectile(gameObject);
    }
}
