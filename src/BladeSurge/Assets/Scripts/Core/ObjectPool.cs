using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 적 오브젝트 풀. 한 스테이지 내 여러 종류의 잡몹(_mobPrefabs[])을 섞어 풀링한다.
/// 보스는 풀 사용하지 않음 (BossEnemy가 직접 Instantiate/Destroy).
///
/// MVP: 단일 스테이지에서 종족 일관성. 스테이지 전환 시 SetMobPrefabs 로 교체.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("현재 스테이지의 잡몹 프리팹 목록. 스폰 시 균등 랜덤 선택.")]
    [SerializeField] private GameObject[] _mobPrefabs;

    [Header("Pool Sizes")]
    [Tooltip("프리팹별 사전 생성 인스턴스 수 (총합 = _mobPrefabs.Length × 이 값).")]
    [FormerlySerializedAs("_mobInitialSize")]
    [SerializeField] private int _mobInitialSizePerPrefab = 20;

    private readonly List<GameObject> _mobPool = new();
    /// <summary>인스턴스 → 원본 prefab 매핑 (반환 시 어느 prefab 출신인지 추적).</summary>
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize()
    {
        if (_mobPrefabs == null || _mobPrefabs.Length == 0)
        {
            Debug.LogWarning("[ObjectPool] _mobPrefabs 비어있음 — 스폰 불가");
            return;
        }
        foreach (var prefab in _mobPrefabs)
        {
            if (prefab == null) continue;
            for (int i = 0; i < _mobInitialSizePerPrefab; i++)
                _mobPool.Add(CreateNew(prefab));
        }
    }

    /// <summary>Initialize 이전(Awake)에 호출해 mob 프리팹 목록을 외부에서 교체. DebugStageSelector 용.</summary>
    public void SetMobPrefabs(GameObject[] prefabs)
    {
        if (prefabs == null) return;
        _mobPrefabs = prefabs;
    }

    /// <summary>
    /// 풀에서 임의 mob 인스턴스 1개를 꺼낸다. 풀이 비면 임의 prefab으로 새로 생성.
    /// </summary>
    public GameObject GetFromPool(EnemyType type)
    {
        if (type != EnemyType.Mob)
        {
            Debug.LogError($"[ObjectPool] 지원하지 않는 타입: {type}");
            return null;
        }

        GameObject obj;
        if (_mobPool.Count > 0)
        {
            int idx = Random.Range(0, _mobPool.Count);
            obj = _mobPool[idx];
            _mobPool.RemoveAt(idx);
        }
        else
        {
            // 풀 고갈 — 임의 prefab으로 확장
            var prefab = _mobPrefabs[Random.Range(0, _mobPrefabs.Length)];
            obj = CreateNew(prefab);
        }

        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        return obj;
    }

    /// <summary>오브젝트를 풀에 반환. 이미 비활성이면 무시.</summary>
    public void ReturnToPool(GameObject obj, EnemyType type)
    {
        if (obj == null || !obj.activeSelf) return;
        if (type != EnemyType.Mob) return;

        obj.GetComponent<IPoolable>()?.OnDespawn();
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _mobPool.Add(obj);
    }

    private GameObject CreateNew(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        _instanceToPrefab[obj] = prefab;
        return obj;
    }
}
