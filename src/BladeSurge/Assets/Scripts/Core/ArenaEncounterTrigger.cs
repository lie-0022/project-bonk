using UnityEngine;

/// <summary>
/// 투기장 봉인 트리거. 플레이어가 통로를 올라와 홀에 진입하면 1회 발동:
/// 1) 봉인 벽(_barrier)을 활성화해 통로로 되돌아가지 못하게 막고,
/// 2) WaveSpawner.BeginEncounter()로 몬스터 스폰을 시작한다.
///
/// 진입 판정은 물리 트리거 대신 위치 폴링으로 한다(CharacterController와 트리거
/// 콜라이더 간 이벤트가 환경에 따라 불안정하므로 결정적 방식 채택).
///
/// 발동 조건: 플레이어 y가 _enterHeight 이상(홀 바닥 높이 도달) AND 봉인 벽 평면을
/// 기준으로 "홀 쪽"으로 완전히 넘어옴(벽 두께 바깥). 봉인 벽의 위치/방향은 Start에서
/// BoxCollider로부터 자동 해석하므로, 벽을 옮기거나 X/Z 어느 방향으로 두어도 대응된다.
/// (벽 회전은 없다고 가정 — 축 정렬 박스)
/// </summary>
public class ArenaEncounterTrigger : MonoBehaviour
{
    [Tooltip("발동 시 활성화할 봉인 벽(BoxCollider 보유, 시작 시 비활성). 이 벽 평면이 봉인 경계가 된다.")]
    [SerializeField] private GameObject _barrier;

    [Tooltip("발동 시 인카운터를 시작할 WaveSpawner.")]
    [SerializeField] private WaveSpawner _waveSpawner;

    [Tooltip("진입 판정 대상 태그.")]
    [SerializeField] private string _playerTag = "Player";

    [Tooltip("이 높이(y) 이상으로 올라오면 '홀 바닥 도달'로 판정. 홀 바닥 높이 부근.")]
    [SerializeField] private float _enterHeight = 13f;

    private Transform _player;
    private bool _fired;

    // Start에서 봉인 벽으로부터 해석한 경계 정보
    private int _axis;          // 봉인 벽의 막는 축 (0=X, 2=Z)
    private bool _hallIsNegative; // 홀이 벽 기준 음(-)의 방향에 있는가
    private float _threshold;   // 이 값을 넘으면 홀 쪽 (벽 두께 바깥 면)

    private void Start()
    {
        var p = GameObject.FindWithTag(_playerTag);
        if (p != null) _player = p.transform;
        else Debug.LogWarning("[ArenaEncounterTrigger] '" + _playerTag + "' 태그 오브젝트 없음");

        if (!ResolveBarrierPlane())
            Debug.LogWarning("[ArenaEncounterTrigger] 봉인 벽 평면 해석 실패 — 발동 불가");
    }

    /// <summary>봉인 벽 BoxCollider(비활성 상태에서도)로부터 막는 축·홀 방향·경계값을 계산.</summary>
    private bool ResolveBarrierPlane()
    {
        if (_barrier == null) return false;
        var box = _barrier.GetComponent<BoxCollider>();
        if (box == null) return false;

        Vector3 center = _barrier.transform.TransformPoint(box.center);
        Vector3 ls = _barrier.transform.lossyScale;
        float extX = Mathf.Abs(box.size.x * ls.x) * 0.5f;
        float extZ = Mathf.Abs(box.size.z * ls.z) * 0.5f;

        // 더 얇은 수평 축 = 벽이 막는 방향(법선)
        _axis = extX <= extZ ? 0 : 2;
        float ext = _axis == 0 ? extX : extZ;

        // 홀 중심 기준점: StageSpawnArea가 있으면 그 중심, 없으면 벽에서 살짝 안쪽 추정 불가 → false
        var area = FindFirstObjectByType<StageSpawnArea>(FindObjectsInactive.Include);
        if (area == null) return false;
        Vector3 hall = area.Center;

        _hallIsNegative = hall[_axis] < center[_axis];
        _threshold = _hallIsNegative ? center[_axis] - ext : center[_axis] + ext;
        return true;
    }

    private void Update()
    {
        if (_fired || _player == null) return;

        Vector3 pos = _player.position;
        if (pos.y < _enterHeight) return;

        float v = pos[_axis];
        bool crossedToHall = _hallIsNegative ? (v <= _threshold) : (v >= _threshold);
        if (!crossedToHall) return;

        Fire();
    }

    private void Fire()
    {
        _fired = true;

        if (_barrier != null)
            _barrier.SetActive(true);

        if (_waveSpawner != null)
            _waveSpawner.BeginEncounter();
        else
            Debug.LogWarning("[ArenaEncounterTrigger] WaveSpawner 미할당 — 인카운터 시작 생략");

        Debug.Log("[ArenaEncounterTrigger] 홀 진입 — 봉인 + 인카운터 시작");
    }
}
