using UnityEngine;

/// <summary>
/// 스테이지별 적 스폰 허용 영역(원형). WaveSpawner가 활성 인스턴스를 찾아
/// 스폰 좌표를 이 영역 안으로 제한하고 바닥 높이에 맞춘다.
///
/// 맵 루트(Stage1/Stage3 등) 하위에 배치하면 DebugStageSelector의 맵 활성화에 따라
/// 자동으로 해당 스테이지 영역만 활성화되어, WaveSpawner가 올바른 영역을 집는다.
/// 영역이 없는 스테이지에서는 WaveSpawner가 기존(무제한) 스폰으로 동작한다.
/// </summary>
public class StageSpawnArea : MonoBehaviour
{
    [Tooltip("이 GameObject 위치를 중심으로 한 스폰 허용 반경(m).")]
    [SerializeField] private float _radius = 28f;

    [Tooltip("스폰 시 바닥(중심 y) 위로 띄울 높이(m). 살짝 띄워 바닥 끼임 방지.")]
    [SerializeField] private float _spawnHeightOffset = 0.2f;

    /// <summary>영역 중심(월드 좌표). y는 바닥 높이로 사용된다.</summary>
    public Vector3 Center => transform.position;

    /// <summary>스폰 허용 반경(m).</summary>
    public float Radius => _radius;

    /// <summary>
    /// 월드 좌표를 이 원형 영역 안으로 제한하고 y를 바닥 높이로 맞춘다.
    /// 수평 거리가 반경을 넘으면 경계 위로 끌어당긴다.
    /// </summary>
    public Vector3 Clamp(Vector3 worldPos)
    {
        Vector3 center = transform.position;
        Vector3 flat = worldPos - center;
        flat.y = 0f;

        if (flat.sqrMagnitude > _radius * _radius)
            worldPos = center + flat.normalized * _radius;

        worldPos.y = center.y + _spawnHeightOffset;
        return worldPos;
    }

    /// <summary>
    /// 영역 안의 균일 랜덤 지점을 반환한다. y는 바닥 높이 + heightOffset.
    /// 항아리/상자 등 배치형 스포너가 맵 안쪽·바닥 위에 놓을 때 사용한다.
    /// </summary>
    public Vector3 RandomPoint(float heightOffset = 0f)
    {
        Vector2 d = Random.insideUnitCircle * _radius;
        Vector3 center = transform.position;
        return new Vector3(center.x + d.x, center.y + heightOffset, center.z + d.y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 0.4f, 0.8f);
        const int segments = 48;
        Vector3 prev = transform.position + new Vector3(_radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
