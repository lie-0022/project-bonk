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
        BossEnemy.OnBossDied += OnBossDied;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyDied -= OnEnemyDied;
        BossEnemy.OnBossDied -= OnBossDied;
    }

    private void OnEnemyDied(float xpReward, Vector3 position)
    {
        if (Random.value > _dropChance) return;

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Instantiate(prefab, position + Vector3.up * _spawnYOffset, Quaternion.identity, transform);
    }

    /// <summary>
    /// 보스 처치 시 이속(Speed) 드롭을 보스 위치에 무조건 1개 스폰한다.
    /// 보스 처치 후 다음 목표(빛기둥/게이트)까지 거리가 있어 이동을 보조하기 위함.
    /// 일반 확률 드롭(OnEnemyDied)과 별개로 항상 확정 지급한다.
    /// </summary>
    private void OnBossDied(BossEnemy boss)
    {
        if (boss == null || _speedPrefab == null) return;
        Vector3 pos = boss.transform.position + Vector3.up * _spawnYOffset;
        Instantiate(_speedPrefab, pos, Quaternion.identity, transform);
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
