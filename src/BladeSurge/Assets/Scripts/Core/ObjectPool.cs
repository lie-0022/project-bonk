using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 오브젝트를 타입별로 관리하는 오브젝트 풀.
/// WaveSpawner가 GetFromPool로 꺼내고, 적 AI가 ReturnToPool로 반환한다.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject _chaserPrefab;
    [SerializeField] private GameObject _rusherPrefab;

    [Header("Pool Sizes")]
    [SerializeField] private int _chaserInitialSize = 50;
    [SerializeField] private int _rusherInitialSize = 20;

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
        _prefabs[EnemyType.Chaser] = _chaserPrefab;
        _prefabs[EnemyType.Rusher] = _rusherPrefab;

        Prewarm(EnemyType.Chaser, _chaserInitialSize);
        Prewarm(EnemyType.Rusher, _rusherInitialSize);
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
