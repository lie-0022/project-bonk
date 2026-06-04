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
    [Header("Impact VFX")]
    [Tooltip("명중/폭발 시 스폰할 임팩트 이펙트. 프리팹별로 다르게 설정 가능. URP 호환 에셋만.")]
    [SerializeField] private GameObject _impactVfxPrefab;
    [Tooltip("임팩트 이펙트 크기 배율.")]
    [SerializeField] private float _impactVfxScale = 1f;

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
    private TrailRenderer[] _trails; // 트레일 (풀 재사용 시 Clear로 잔상 제거)

    private void Awake()
    {
        _trails = GetComponentsInChildren<TrailRenderer>(true);
    }

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
        PlayTrails(); // 발사 위치 확정 후 트레일 시작 (이전 잔상 제거)
    }

    public void OnSpawn() { _isActive = false; _alreadyHit.Clear(); }
    public void OnDespawn() { _isActive = false; _alreadyHit.Clear(); StopTrails(); }

    /// <summary>트레일 재시작. Clear로 풀 재사용 시 이전 위치→새 위치 잔상선을 제거한다.</summary>
    private void PlayTrails()
    {
        if (_trails == null) return;
        foreach (var t in _trails)
        {
            if (t == null) continue;
            t.Clear();
            t.emitting = true;
        }
    }

    /// <summary>트레일 방출 중단 (풀 반환 시).</summary>
    private void StopTrails()
    {
        if (_trails == null) return;
        foreach (var t in _trails)
            if (t != null) t.emitting = false;
    }

    /// <summary>명중/폭발 지점에 임팩트 이펙트 스폰. 파티클 강제 재생 후 자동 소멸.</summary>
    private void SpawnImpact(Vector3 pos)
    {
        if (_impactVfxPrefab == null) return;
        GameObject fx = Instantiate(_impactVfxPrefab, pos, Quaternion.identity);
        if (_impactVfxScale > 0f) fx.transform.localScale *= _impactVfxScale;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
            ps.Play();
        Destroy(fx, 2f);
    }

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

    /// <summary>폭발 시 반경 내 모든 Enemy에게 광역 피해 + 폭발 이펙트.</summary>
    private void DetonateExplosion(Vector3 origin)
    {
        SpawnImpact(origin);

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
