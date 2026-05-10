using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 맵에 항아리를 랜덤 배치한다.
/// 사각 영역 내부에서 _minDistance 간격을 유지해 스폰 (ChestSpawner 패턴).
/// </summary>
public class JarSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject _jarPrefab;

    [Header("Spawn Area (XZ)")]
    [SerializeField] private Vector2 _areaCenter = Vector2.zero;
    [SerializeField] private Vector2 _areaSize = new Vector2(40f, 40f);
    [SerializeField] private float _spawnY = 0.0f;

    [Header("Layout")]
    [SerializeField] private int _jarCount = 20;
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private int _maxAttemptsPerJar = 30;

    private void Start()
    {
        if (_jarPrefab == null)
        {
            Debug.LogWarning("[JarSpawner] Jar prefab 미할당");
            return;
        }
        SpawnJars();
    }

    private void SpawnJars()
    {
        var placed = new List<Vector3>(_jarCount);
        for (int i = 0; i < _jarCount; i++)
        {
            if (TryFindPosition(placed, out Vector3 pos))
            {
                Instantiate(_jarPrefab, pos, Quaternion.identity, transform);
                placed.Add(pos);
            }
        }
    }

    private bool TryFindPosition(List<Vector3> placed, out Vector3 pos)
    {
        for (int attempt = 0; attempt < _maxAttemptsPerJar; attempt++)
        {
            float x = _areaCenter.x + Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f);
            float z = _areaCenter.y + Random.Range(-_areaSize.y * 0.5f, _areaSize.y * 0.5f);
            var candidate = new Vector3(x, _spawnY, z);

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
        Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.3f);
        Gizmos.DrawCube(new Vector3(_areaCenter.x, _spawnY, _areaCenter.y), new Vector3(_areaSize.x, 0.1f, _areaSize.y));
    }
}
