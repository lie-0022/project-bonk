using UnityEngine;

/// <summary>
/// 적 사망 시 _dropChance 확률로 3종 드롭 아이템 중 1개를 위치에 스폰.
/// EnemyBase.OnEnemyDied 이벤트 구독.
/// </summary>
public class DropItemSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _magnetPrefab;
    [SerializeField] private GameObject _speedPrefab;
    [SerializeField] private GameObject _timeStopPrefab;

    [Header("Drop")]
    [Range(0f, 1f)]
    [SerializeField] private float _dropChance = 0.05f;
    [SerializeField] private float _spawnYOffset = 0.5f;

    private void OnEnable()
    {
        EnemyBase.OnEnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyDied -= OnEnemyDied;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        if (Random.value > _dropChance) return;

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Instantiate(prefab, position + Vector3.up * _spawnYOffset, Quaternion.identity, transform);
    }

    private GameObject PickPrefab()
    {
        int roll = Random.Range(0, 3);
        return roll switch
        {
            0 => _magnetPrefab,
            1 => _speedPrefab,
            _ => _timeStopPrefab,
        };
    }
}
