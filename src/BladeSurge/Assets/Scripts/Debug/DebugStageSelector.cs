using UnityEngine;

/// <summary>
/// 디버그용 스테이지(종족) 선택기. 인스펙터에서 _race를 골라두면 게임 시작 시
/// ObjectPool의 mob 프리팹과 WaveSpawner의 boss 프리팹을 해당 종족 쌍으로 교체한다.
///
/// 정식 스테이지 진행 시스템이 도입되기 전까지의 임시 도구. Awake에서 동작하므로
/// ObjectPool.Initialize / WaveSpawner.Start 보다 먼저 적용된다 (스크립트 실행 순서 무관).
/// </summary>
public class DebugStageSelector : MonoBehaviour
{
    [Header("선택")]
    [SerializeField] private EnemyRace _race = EnemyRace.Slime;

    [Header("Race 프리팹 매핑")]
    [SerializeField] private GameObject _slimeMob;
    [SerializeField] private GameObject _slimeBoss;
    [SerializeField] private GameObject _skeletonMob;
    [SerializeField] private GameObject _skeletonBoss;
    [SerializeField] private GameObject _goblinMob;
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

        GameObject mob;
        GameObject boss;
        switch (_race)
        {
            case EnemyRace.Skeleton: mob = _skeletonMob; boss = _skeletonBoss; break;
            case EnemyRace.Goblin:   mob = _goblinMob;   boss = _goblinBoss;   break;
            default:                 mob = _slimeMob;    boss = _slimeBoss;    break;
        }

        if (mob == null || boss == null)
        {
            Debug.LogWarning($"[DebugStageSelector] {_race} 프리팹 미설정 — 적용 생략");
            return;
        }

        _objectPool.SetMobPrefab(mob);
        _waveSpawner.SetBossPrefab(boss);
        Debug.Log($"[DebugStageSelector] Race={_race} mob={mob.name} boss={boss.name}");
    }
}
