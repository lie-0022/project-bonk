using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 슬롯 관리, 타이머, 발동을 총괄하는 싱글턴.
/// 최대 3개 무기를 독립 타이머로 자동 발동한다.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    public static WeaponSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int _maxWeaponSlots = 3;
    [SerializeField] private int _maxWeaponLevel = 15;

    [Header("Weapon Data")]
    [SerializeField] private WeaponDataSO _swordData;
    [SerializeField] private WeaponDataSO _gunData;
    [SerializeField] private WeaponDataSO _magicData;

    [Header("Attack Components")]
    [SerializeField] private SwordAttack _swordAttack;
    [SerializeField] private GunAttack _gunAttack;
    [SerializeField] private MagicAttack _magicAttack;

    [Header("Projectile Pool")]
    [SerializeField] private GameObject _gunProjectilePrefab;
    [SerializeField] private GameObject _magicProjectilePrefab;
    [SerializeField] private int _projectileInitialSize = 30;

    private readonly Queue<GameObject> _gunProjectilePool = new();
    private readonly Queue<GameObject> _magicProjectilePool = new();

    /// <summary>무기 추가/강화 시 브로드캐스트. HUD 슬롯 갱신용.</summary>
    public static event Action OnWeaponsChanged;

    private readonly List<WeaponSlot> _slots = new();
    private bool _isActive;
    private PlayerStats _stats;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _stats = PlayerStats.Instance;
        GameManager.OnGameStateChanged += OnGameStateChanged;
        PlayerStats.OnStatsChanged += OnStatsChanged;
        InitializeProjectilePool();

        // 이미 Playing 상태일 수 있으므로 즉시 동기화 (Start 호출 순서 race 방지)
        if (GameManager.Instance != null)
            OnGameStateChanged(GameManager.Instance.CurrentState);
    }

    private void InitializeProjectilePool()
    {
        for (int i = 0; i < _projectileInitialSize; i++)
        {
            _gunProjectilePool.Enqueue(CreateProjectile(_gunProjectilePrefab));
            _magicProjectilePool.Enqueue(CreateProjectile(_magicProjectilePrefab));
        }
    }

    public GameObject GetProjectile(WeaponType weaponType)
    {
        var pool = weaponType == WeaponType.Magic ? _magicProjectilePool : _gunProjectilePool;
        var prefab = weaponType == WeaponType.Magic ? _magicProjectilePrefab : _gunProjectilePrefab;

        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateProjectile(prefab);
        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        return obj;
    }

    public void ReturnProjectile(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;
        obj.GetComponent<IPoolable>()?.OnDespawn();
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (obj.CompareTag("MagicProjectile"))
            _magicProjectilePool.Enqueue(obj);
        else
            _gunProjectilePool.Enqueue(obj);
    }

    private GameObject CreateProjectile(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        PlayerStats.OnStatsChanged -= OnStatsChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        _isActive = state == GameState.Playing;
    }

    private void OnStatsChanged()
    {
        // 공격 속도 패시브 변경 시 각 슬롯 interval 갱신
        foreach (var slot in _slots)
            slot.UpdateInterval(_stats.AttackSpeedMultiplier);
    }

    private void Update()
    {
        if (!_isActive) return;

        foreach (var slot in _slots)
            slot.Tick(Time.deltaTime, this);
    }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>새 무기를 추가한다. 슬롯이 가득 차면 무시. 보유 중이면 등급 적용해 강화.</summary>
    public bool AddWeapon(WeaponType type, CardGrade grade = CardGrade.Common)
    {
        if (HasWeapon(type)) return UpgradeWeapon(type, grade);
        if (_slots.Count >= _maxWeaponSlots) return false;

        var data = GetData(type);
        if (data == null) return false;

        _slots.Add(new WeaponSlot(data, _stats.AttackSpeedMultiplier));
        OnWeaponsChanged?.Invoke();
        return true;
    }

    /// <summary>기존 무기를 등급에 따라 강화한다. 최대 레벨이면 무시.</summary>
    public bool UpgradeWeapon(WeaponType type, CardGrade grade = CardGrade.Common)
    {
        var slot = GetSlot(type);
        if (slot == null || slot.Level >= _maxWeaponLevel) return false;

        slot.Upgrade(grade, _stats.AttackSpeedMultiplier);
        OnWeaponsChanged?.Invoke();
        return true;
    }

    public bool HasWeapon(WeaponType type) => GetSlot(type) != null;
    public int GetWeaponLevel(WeaponType type) => GetSlot(type)?.Level ?? 0;
    public IReadOnlyList<WeaponSlot> GetEquippedWeapons() => _slots;

    public float GetCooldownNormalized(WeaponType type)
    {
        var slot = GetSlot(type);
        return slot?.CooldownNormalized ?? 0f;
    }

    // ── 발동 ─────────────────────────────────────────────────

    /// <summary>WeaponSlot 타이머가 만료되면 호출한다.</summary>
    public void ExecuteWeapon(WeaponSlot slot)
    {
        switch (slot.Data.WeaponType)
        {
            case WeaponType.Sword:
                _swordAttack?.Execute(slot, _stats);
                break;
            case WeaponType.Gun:
                _gunAttack?.Execute(slot, _stats);
                break;
            case WeaponType.Magic:
                _magicAttack?.Execute(slot, _stats);
                break;
        }
    }

    // ── 내부 유틸 ────────────────────────────────────────────

    private WeaponSlot GetSlot(WeaponType type) =>
        _slots.Find(s => s.Data.WeaponType == type);

    private WeaponDataSO GetData(WeaponType type) => type switch
    {
        WeaponType.Sword => _swordData,
        WeaponType.Gun   => _gunData,
        WeaponType.Magic => _magicData,
        _                => null
    };
}
