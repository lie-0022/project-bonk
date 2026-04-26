using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// XP 오브·골드 등 픽업 오브젝트를 관리하는 풀.
/// </summary>
public class PickupPool : MonoBehaviour, IInitializable
{
    public static PickupPool Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject _xpOrbPrefab;
    [SerializeField] private GameObject _goldOrbPrefab;

    [Header("Pool Sizes")]
    [SerializeField] private int _xpOrbInitialSize = 100;
    [SerializeField] private int _goldOrbInitialSize = 100;

    private readonly Queue<GameObject> _xpOrbPool = new();
    private readonly Queue<GameObject> _goldOrbPool = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize()
    {
        for (int i = 0; i < _xpOrbInitialSize; i++)
            _xpOrbPool.Enqueue(CreateNew(_xpOrbPrefab));

        for (int i = 0; i < _goldOrbInitialSize; i++)
            _goldOrbPool.Enqueue(CreateNew(_goldOrbPrefab));
    }

    public GameObject GetXPOrb()
    {
        GameObject obj = _xpOrbPool.Count > 0
            ? _xpOrbPool.Dequeue()
            : CreateNew(_xpOrbPrefab);

        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        return obj;
    }

    public void ReturnXPOrb(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;
        obj.GetComponent<IPoolable>()?.OnDespawn();
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _xpOrbPool.Enqueue(obj);
    }

    public GameObject GetGoldOrb()
    {
        GameObject obj = _goldOrbPool.Count > 0
            ? _goldOrbPool.Dequeue()
            : CreateNew(_goldOrbPrefab);

        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        return obj;
    }

    public void ReturnGoldOrb(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;
        obj.GetComponent<IPoolable>()?.OnDespawn();
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _goldOrbPool.Enqueue(obj);
    }

    private GameObject CreateNew(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }
}
