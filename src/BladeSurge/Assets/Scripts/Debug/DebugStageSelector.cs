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

    [Header("Race 웨이브 구성 (스테이지별 페이싱)")]
    [Tooltip("선택한 종족(맵)에 맞는 웨이브 구성을 WaveSpawner에 주입한다. 미설정 시 WaveSpawner 기본값 유지.")]
    [SerializeField] private StageWaveConfig _slimeWaveConfig;
    [SerializeField] private StageWaveConfig _goblinWaveConfig;
    [SerializeField] private StageWaveConfig _skeletonWaveConfig;

    [Header("타깃 컴포넌트")]
    [SerializeField] private ObjectPool _objectPool;
    [SerializeField] private WaveSpawner _waveSpawner;
    [SerializeField] private JarSpawner _jarSpawner;
    [SerializeField] private ChestSpawner _chestSpawner;

    [Header("맵별 항아리 수 (맵 크기/밀도 고려 — 과밀 방지)")]
    [Tooltip("선택한 종족(맵)에 따라 JarSpawner의 항아리 수를 덮어쓴다.")]
    [SerializeField] private int _slimeJarCount = 150;
    [SerializeField] private int _goblinJarCount = 120;
    [SerializeField] private int _skeletonJarCount = 120;

    [Header("맵별 상자 수 (과밀 방지)")]
    [Tooltip("선택한 종족(맵)에 따라 ChestSpawner의 상자 수를 덮어쓴다.")]
    [SerializeField] private int _slimeChestCount = 12;
    [SerializeField] private int _goblinChestCount = 10;
    [SerializeField] private int _skeletonChestCount = 10;

    [Header("맵별 난이도 배율 (1=기본 — PM 플레이테스트로 튜닝)")]
    [Tooltip("잡몹·보스 체력 배율.")]
    [SerializeField] private float _slimeHpMult = 1f;
    [SerializeField] private float _goblinHpMult = 1f;
    [SerializeField] private float _skeletonHpMult = 1f;
    [Tooltip("적이 주는 데미지 배율.")]
    [SerializeField] private float _slimeDamageMult = 1f;
    [SerializeField] private float _goblinDamageMult = 1f;
    [SerializeField] private float _skeletonDamageMult = 1f;
    [Tooltip("코인·경험치 보상 배율.")]
    [SerializeField] private float _slimeRewardMult = 1f;
    [SerializeField] private float _goblinRewardMult = 1f;
    [SerializeField] private float _skeletonRewardMult = 1f;

    private void Awake()
    {
        // CharacterSelect 화면의 맵 선택을 반영 (단독 실행 시 GameSession 기본값 → Slime)
        _race = GameSession.SelectedRace;

        ApplyMap();
        ApplyJarCount();
        ApplyChestCount();
        ApplyDifficulty();
        ApplyWaveConfig();

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

    private void Update()
    {
#if UNITY_EDITOR
        // [디버그] F9: 웨이브를 기다리지 않고 즉시 보스 스폰 (보스 테스트 단축용). 빌드에서는 제외.
        if (_waveSpawner != null
            && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
        {
            _waveSpawner.DebugSpawnBossNow();
        }
#endif
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

        // 선택된 맵에 봉인 트리거(ArenaEncounterTrigger)가 있으면 그 트리거가 인카운터를 시작하므로
        // WaveSpawner 자동 시작을 끄고, 없으면(마을 등 일반 스테이지) 진입 즉시 자동 시작하게 한다.
        if (_waveSpawner != null && selected != null)
        {
            bool hasSealTrigger = selected.GetComponentInChildren<ArenaEncounterTrigger>(true) != null;
            _waveSpawner.SetAutoStart(!hasSealTrigger);
        }

        MovePlayerToSpawn(selected);

        if (selected == null)
        {
            Debug.LogWarning($"[DebugStageSelector] {_race} 맵 미설정 — 맵 비활성 상태일 수 있음");
        }
        else
        {
            Debug.Log($"[DebugStageSelector] 맵 활성화: {selected.name}");
        }
    }

    /// <summary>
    /// 선택한 종족(맵)에 맞는 항아리 수를 JarSpawner에 적용한다. JarSpawner.Start 이전(Awake)에 호출.
    /// </summary>
    private void ApplyJarCount()
    {
        if (_jarSpawner == null) return;
        int count = _race switch
        {
            EnemyRace.Goblin => _goblinJarCount,
            EnemyRace.Skeleton => _skeletonJarCount,
            _ => _slimeJarCount
        };
        _jarSpawner.SetJarCount(count);
    }

    /// <summary>선택한 종족(맵)에 맞는 상자 수를 ChestSpawner에 적용한다. ChestSpawner.Start 이전(Awake)에 호출.</summary>
    private void ApplyChestCount()
    {
        if (_chestSpawner == null) return;
        int count = _race switch
        {
            EnemyRace.Goblin => _goblinChestCount,
            EnemyRace.Skeleton => _skeletonChestCount,
            _ => _slimeChestCount
        };
        _chestSpawner.SetChestCount(count);
    }

    /// <summary>선택한 종족(맵)에 맞는 웨이브 구성을 WaveSpawner에 주입한다. WaveSpawner.Start 이전(Awake)에 호출.</summary>
    private void ApplyWaveConfig()
    {
        if (_waveSpawner == null) return;
        StageWaveConfig config = _race switch
        {
            EnemyRace.Goblin => _goblinWaveConfig,
            EnemyRace.Skeleton => _skeletonWaveConfig,
            _ => _slimeWaveConfig
        };
        if (config != null) _waveSpawner.SetWaveConfig(config);
    }

    /// <summary>선택한 종족(맵)에 맞는 난이도 배율(체력/보상)을 StageDifficulty에 적용한다. WaveSpawner/Gold/XP가 읽기 전(Awake)에 설정.</summary>
    private void ApplyDifficulty()
    {
        StageDifficulty.EnemyHpMultiplier = _race switch
        {
            EnemyRace.Goblin => _goblinHpMult,
            EnemyRace.Skeleton => _skeletonHpMult,
            _ => _slimeHpMult
        };
        StageDifficulty.EnemyDamageMultiplier = _race switch
        {
            EnemyRace.Goblin => _goblinDamageMult,
            EnemyRace.Skeleton => _skeletonDamageMult,
            _ => _slimeDamageMult
        };
        StageDifficulty.RewardMultiplier = _race switch
        {
            EnemyRace.Goblin => _goblinRewardMult,
            EnemyRace.Skeleton => _skeletonRewardMult,
            _ => _slimeRewardMult
        };
    }

    /// <summary>
    /// 선택한 맵의 "PlayerSpawn" 자식 위치로 플레이어를 이동한다(맵마다 시작 지점이 다르므로).
    /// PlayerSpawn이 없으면 이동하지 않는다(기존 위치 유지). CharacterController는 이동 전 비활성화.
    /// </summary>
    private void MovePlayerToSpawn(GameObject selected)
    {
        if (selected == null) return;
        Transform spawn = selected.transform.Find("PlayerSpawn");
        if (spawn == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (cc != null) cc.enabled = true;
    }

    private static void SetMapActive(GameObject map, bool active)
    {
        if (map != null && map.activeSelf != active)
        {
            map.SetActive(active);
        }
    }
}
