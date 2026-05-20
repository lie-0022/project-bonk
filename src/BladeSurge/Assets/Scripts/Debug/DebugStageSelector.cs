using UnityEngine;

/// <summary>
/// 디버그용 스테이지(종족) 선택기. 인스펙터에서 _race를 골라두면 게임 시작 시
/// ObjectPool의 mob 프리팹 배열과 WaveSpawner의 boss 프리팹을 해당 종족 쌍으로 교체한다.
///
/// 종족별로 잡몹은 여러 종류를 등록할 수 있다 (예: Skeleton 스테이지엔 Warrior/Minion/Rogue/Mage 4종 혼합).
/// 정식 스테이지 진행 시스템이 도입되기 전까지의 임시 도구. Awake에서 동작하므로
/// ObjectPool.Initialize / WaveSpawner.Start 보다 먼저 적용된다.
/// </summary>
public class DebugStageSelector : MonoBehaviour
{
    [Header("선택")]
    [SerializeField] private EnemyRace _race = EnemyRace.Slime;

    [Header("Race 프리팹 매핑")]
    [SerializeField] private GameObject[] _slimeMobs;
    [SerializeField] private GameObject _slimeBoss;
    [SerializeField] private GameObject[] _skeletonMobs;
    [SerializeField] private GameObject _skeletonBoss;
    [SerializeField] private GameObject[] _goblinMobs;
    [SerializeField] private GameObject _goblinBoss;

    [Header("Race 맵 매핑")]
    [Tooltip("선택한 종족에 해당하는 맵 루트만 활성화되고 나머지는 비활성화된다. 미설정 시 맵 전환 생략.")]
    [SerializeField] private GameObject _slimeMap;
    [SerializeField] private GameObject _skeletonMap;
    [SerializeField] private GameObject _goblinMap;

    [Header("타깃 컴포넌트")]
    [SerializeField] private ObjectPool _objectPool;
    [SerializeField] private WaveSpawner _waveSpawner;

    private void Awake()
    {
        ApplyMap();

        if (_objectPool == null || _waveSpawner == null)
        {
            Debug.LogWarning("[DebugStageSelector] ObjectPool 또는 WaveSpawner 참조 누락 — 적용 생략");
            return;
        }

        GameObject[] mobs;
        GameObject boss;
        switch (_race)
        {
            case EnemyRace.Skeleton: mobs = _skeletonMobs; boss = _skeletonBoss; break;
            case EnemyRace.Goblin:   mobs = _goblinMobs;   boss = _goblinBoss;   break;
            default:                 mobs = _slimeMobs;    boss = _slimeBoss;    break;
        }

        if (mobs == null || mobs.Length == 0 || boss == null)
        {
            Debug.LogWarning($"[DebugStageSelector] {_race} 프리팹 미설정 — 적용 생략");
            return;
        }

        _objectPool.SetMobPrefabs(mobs);
        _waveSpawner.SetBossPrefab(boss);
        Debug.Log($"[DebugStageSelector] Race={_race} mobs={mobs.Length}종 boss={boss.name}");
    }

    /// <summary>
    /// 선택한 종족에 해당하는 맵만 활성화하고 나머지 맵 루트는 비활성화한다.
    /// 맵 루트가 하나도 설정되지 않았으면 아무것도 하지 않는다(기존 씬 구성 유지).
    /// </summary>
    private void ApplyMap()
    {
        if (_slimeMap == null && _skeletonMap == null && _goblinMap == null)
        {
            return;
        }

        GameObject selected;
        switch (_race)
        {
            case EnemyRace.Skeleton: selected = _skeletonMap; break;
            case EnemyRace.Goblin:   selected = _goblinMap;   break;
            default:                 selected = _slimeMap;    break;
        }

        SetMapActive(_slimeMap, ReferenceEquals(_slimeMap, selected));
        SetMapActive(_skeletonMap, ReferenceEquals(_skeletonMap, selected));
        SetMapActive(_goblinMap, ReferenceEquals(_goblinMap, selected));

        if (selected == null)
        {
            Debug.LogWarning($"[DebugStageSelector] {_race} 맵 미설정 — 맵 비활성 상태일 수 있음");
        }
        else
        {
            Debug.Log($"[DebugStageSelector] 맵 활성화: {selected.name}");
        }
    }

    private static void SetMapActive(GameObject map, bool active)
    {
        if (map != null && map.activeSelf != active)
        {
            map.SetActive(active);
        }
    }
}
