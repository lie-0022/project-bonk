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

    [Header("타깃 컴포넌트")]
    [SerializeField] private ObjectPool _objectPool;
    [SerializeField] private WaveSpawner _waveSpawner;

    private void Awake()
    {
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
}
