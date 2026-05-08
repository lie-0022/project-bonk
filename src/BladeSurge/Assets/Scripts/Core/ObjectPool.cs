using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 적 오브젝트를 타입별로 관리하는 오브젝트 풀.
/// MVP: 단일 mob 종족만 풀링 — 스테이지별 종족 교체는 인스펙터에서 _mobPrefab 갱신.
/// 보스는 풀 사용하지 않음 (BossEnemy가 직접 Instantiate/Destroy).
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("현재 스테이지의 잡몹 프리팹 (Slime/Skeleton/Goblin 등 — EnemyType.Mob).")]
    [FormerlySerializedAs("_chaserPrefab")]
    [SerializeField] private GameObject _mobPrefab;

    [Header("Pool Sizes")]
    [FormerlySerializedAs("_chaserInitialSize")]
    [SerializeField] private int _mobInitialSize = 60;

    private readonly Dictionary<EnemyType, Queue<GameObject>> _pools = new();
    private readonly Dictionary<EnemyType, GameObject> _prefabs = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// GameManager.Start()에서 호출. 프리팹을 등록하고 풀을 사전 생성한다.
    /// </summary>
    public void Initialize()
    {
        _prefabs[EnemyType.Mob] = _mobPrefab;
        Prewarm(EnemyType.Mob, _mobInitialSize);
    }

    /// <summary>Initialize 이전(Awake)에 호출해 mob 프리팹을 외부에서 교체. DebugStageSelector 용.</summary>
    public void SetMobPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        _mobPrefab = prefab;
    }

    private void Prewarm(EnemyType type, int count)
    {
        _pools[type] = new Queue<GameObject>();
        for (int i = 0; i < count; i++)
            _pools[type].Enqueue(CreateNew(type));
    }

    /// <summary>
    /// 풀에서 오브젝트를 꺼내 활성화한다. 풀이 비어있으면 자동 확장.
    /// </summary>
    public GameObject GetFromPool(EnemyType type)
    {
        if (!_pools.TryGetValue(type, out var pool))
        {
            Debug.LogError($"[ObjectPool] 등록되지 않은 타입: {type}");
            return null;
        }

        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNew(type);

        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀에 반환한다. 이미 비활성화된 오브젝트는 무시한다.
    /// </summary>
    public void ReturnToPool(GameObject obj, EnemyType type)
    {
        if (obj == null || !obj.activeSelf) return;

        obj.GetComponent<IPoolable>()?.OnDespawn();
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (_pools.TryGetValue(type, out var pool))
            pool.Enqueue(obj);
    }

    private GameObject CreateNew(EnemyType type)
    {
        Debug.Log($"[ObjectPool] 풀 확장: {type}");
        var obj = Instantiate(_prefabs[type], transform);
        obj.SetActive(false);
        return obj;
    }
}
