using UnityEngine;

/// <summary>
/// 스테이지별 적/오브젝트 스폰 허용 영역. WaveSpawner/JarSpawner/ChestSpawner가 활성 인스턴스를 찾아
/// 스폰 좌표를 이 영역 안으로 제한하고 바닥 높이에 맞춘다.
///
/// 모양은 원형(Circle) 또는 사각형(Rectangle)을 선택할 수 있다.
/// 맵 루트(Stage1/Stage2/Stage3 등) 하위에 배치하면 DebugStageSelector의 맵 활성화에 따라
/// 자동으로 해당 스테이지 영역만 활성화되어, 스포너가 올바른 영역을 집는다.
/// 영역이 없는 스테이지에서는 스포너가 기존(무제한) 스폰으로 동작한다.
/// </summary>
public class StageSpawnArea : MonoBehaviour
{
    public enum AreaShape { Circle, Rectangle }

    [Tooltip("영역 모양. Circle=원형(반경), Rectangle=사각형(가로/세로).")]
    [SerializeField] private AreaShape _shape = AreaShape.Circle;

    [Tooltip("원형일 때 스폰 허용 반경(m).")]
    [SerializeField] private float _radius = 28f;

    [Tooltip("사각형일 때 가로(X)/세로(Z) 전체 길이(m).")]
    [SerializeField] private Vector2 _rectSize = new Vector2(40f, 40f);

    [Tooltip("스폰 시 바닥(중심 y) 위로 띄울 높이(m). 살짝 띄워 바닥 끼임 방지.")]
    [SerializeField] private float _spawnHeightOffset = 0.2f;

    /// <summary>영역 중심(월드 좌표). y는 바닥 높이로 사용된다.</summary>
    public Vector3 Center => transform.position;

    /// <summary>원형 스폰 허용 반경(m).</summary>
    public float Radius => _radius;

    /// <summary>
    /// 월드 좌표를 이 영역 안으로 제한하고 y를 바닥 높이로 맞춘다.
    /// </summary>
    public Vector3 Clamp(Vector3 worldPos)
    {
        Vector3 center = transform.position;

        if (_shape == AreaShape.Rectangle)
        {
            float hx = _rectSize.x * 0.5f;
            float hz = _rectSize.y * 0.5f;
            worldPos.x = Mathf.Clamp(worldPos.x, center.x - hx, center.x + hx);
            worldPos.z = Mathf.Clamp(worldPos.z, center.z - hz, center.z + hz);
            worldPos.y = center.y + _spawnHeightOffset;
            return worldPos;
        }

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
        Vector3 center = transform.position;

        if (_shape == AreaShape.Rectangle)
        {
            float x = Random.Range(-_rectSize.x * 0.5f, _rectSize.x * 0.5f);
            float z = Random.Range(-_rectSize.y * 0.5f, _rectSize.y * 0.5f);
            return new Vector3(center.x + x, center.y + heightOffset, center.z + z);
        }

        Vector2 d = Random.insideUnitCircle * _radius;
        return new Vector3(center.x + d.x, center.y + heightOffset, center.z + d.y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 0.4f, 0.8f);

        if (_shape == AreaShape.Rectangle)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(_rectSize.x, 0.1f, _rectSize.y));
            return;
        }

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
