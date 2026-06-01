using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 맵에 상자를 랜덤 배치한다.
/// 사각 영역(_areaCenter, _areaSize) 내부에서 _minDistance 간격을 유지해 스폰.
/// </summary>
public class ChestSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject _chestPrefab;

    [Header("Spawn Area (XZ)")]
    [SerializeField] private Vector2 _areaCenter = Vector2.zero;
    [SerializeField] private Vector2 _areaSize = new Vector2(40f, 40f);
    [SerializeField] private float _spawnY = 0.5f;

    [Header("Layout")]
    [SerializeField] private int _chestCount = 12;
    [SerializeField] private float _minDistance = 5f;
    [SerializeField] private int _maxAttemptsPerChest = 30;

    private StageSpawnArea _spawnArea;
    private Terrain _terrain;

    private void Start()
    {
        if (_chestPrefab == null)
        {
            Debug.LogWarning("[ChestSpawner] Chest prefab 미할당");
            return;
        }
        // 활성 스테이지에 스폰 영역이 있으면 그 안쪽·바닥 위로 배치(맵 밖/바닥 아래 방지).
        // 없으면 직렬화된 사각 영역(_areaCenter/_areaSize/_spawnY) 폴백.
        _spawnArea = FindFirstObjectByType<StageSpawnArea>();
        // 사각 영역 폴백을 맵 전체로 넓혀도 지형 높이에 맞춰 배치되도록 활성 Terrain을 캐싱.
        _terrain = FindFirstObjectByType<Terrain>();
        SpawnChests();
    }

    private void SpawnChests()
    {
        var placed = new List<Vector3>(_chestCount);
        for (int i = 0; i < _chestCount; i++)
        {
            if (TryFindPosition(placed, out Vector3 pos))
            {
                Instantiate(_chestPrefab, pos, Quaternion.identity, transform);
                placed.Add(pos);
            }
            else
            {
                Debug.LogWarning($"[ChestSpawner] {i + 1}번째 상자 자리 못 찾음 (영역 좁거나 minDistance 큼)");
            }
        }
    }

    private bool TryFindPosition(List<Vector3> placed, out Vector3 pos)
    {
        for (int attempt = 0; attempt < _maxAttemptsPerChest; attempt++)
        {
            Vector3 candidate;
            if (_spawnArea != null)
            {
                candidate = _spawnArea.RandomPoint(_spawnY);
            }
            else
            {
                float x = _areaCenter.x + Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f);
                float z = _areaCenter.y + Random.Range(-_areaSize.y * 0.5f, _areaSize.y * 0.5f);
                // Terrain이 있으면 해당 지점의 지형 높이에 _spawnY를 더해 바닥 위에 배치(공중/지하 방지).
                float y = _terrain != null
                    ? _terrain.SampleHeight(new Vector3(x, 0f, z)) + _terrain.transform.position.y + _spawnY
                    : _spawnY;
                candidate = new Vector3(x, y, z);
            }

            bool ok = true;
            foreach (var existing in placed)
            {
                if (Vector3.Distance(existing, candidate) < _minDistance) { ok = false; break; }
            }
            if (ok) { pos = candidate; return true; }
        }
        pos = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawCube(new Vector3(_areaCenter.x, _spawnY, _areaCenter.y), new Vector3(_areaSize.x, 0.1f, _areaSize.y));
    }
}
