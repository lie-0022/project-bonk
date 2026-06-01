using UnityEngine;

/// <summary>웨이브 한 개의 설정값.</summary>
[System.Serializable]
public struct WaveData
{
    public float SpawnInterval;
    public float HpMultiplier;
    public float SpeedMultiplier;
}

/// <summary>
/// 시간 기반 웨이브 스폰 시스템.
/// 플레이어 주변 spawnRadius 거리에서 적을 스폰하고 5개 웨이브를 진행한다.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float _spawnRadius = 15f;
    [SerializeField] private float _waveDuration = 60f;
    [SerializeField] private float _waveTransitionDelay = 1f;

    [Header("Encounter Gating")]
    [Tooltip("true면 게임 시작 시 즉시 웨이브 시작(기존 동작). false면 BeginEncounter() 호출 전까지 스폰 안 함 (투기장 봉인 트리거 등).")]
    [SerializeField] private bool _autoStartOnPlay = true;

    [Header("Boss")]
    [Tooltip("이 웨이브가 종료된 직후 보스를 스폰한다. 1-based. 0 또는 음수면 비활성.")]
    [SerializeField] private int _bossSpawnAfterWave = 1;
    [Tooltip("보스 프리팹 (BossEnemy 컴포넌트 부착 필요).")]
    [SerializeField] private GameObject _bossPrefab;
    [Tooltip("보스 스폰 거리 (플레이어로부터).")]
    [SerializeField] private float _bossSpawnRadius = 15f;

    [Header("Wave Data")]
    [SerializeField] private WaveData[] _waves = new WaveData[]
    {
        new WaveData { SpawnInterval = 2.0f, HpMultiplier = 1.0f, SpeedMultiplier = 1.0f },
        new WaveData { SpawnInterval = 1.5f, HpMultiplier = 1.2f, SpeedMultiplier = 1.1f },
        new WaveData { SpawnInterval = 1.2f, HpMultiplier = 1.5f, SpeedMultiplier = 1.2f },
        new WaveData { SpawnInterval = 1.0f, HpMultiplier = 1.8f, SpeedMultiplier = 1.3f },
        new WaveData { SpawnInterval = 0.8f, HpMultiplier = 2.2f, SpeedMultiplier = 1.5f },
    };

    /// <summary>현재 웨이브 번호 (1-based). HUD 표시용.</summary>
    public int CurrentWave { get; private set; } = 1;

    /// <summary>현재 웨이브 남은 시간(초). HUD 표시용.</summary>
    public float WaveTimeRemaining { get; private set; }

    private Transform _playerTransform;
    private StageSpawnArea _spawnArea;
    private bool _encounterStarted;
    private bool _isSpawning;
    private float _spawnTimer;
    private float _waveTimer;
    private bool _inTransition;
    private float _transitionTimer;
    private bool _bossSpawned;
    private GameObject _bossInstance;
    private static readonly Vector3 PrewarmPosition = new Vector3(-9999f, -9999f, -9999f);

    /// <summary>Start 이전(Awake)에 호출해 보스 프리팹을 외부에서 교체. DebugStageSelector 용.</summary>
    public void SetBossPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        _bossPrefab = prefab;
    }

    /// <summary>
    /// Start 이전(Awake)에 호출해 자동 시작 여부를 외부에서 설정. DebugStageSelector 용.
    /// 봉인 트리거(ArenaEncounterTrigger)가 있는 스테이지는 false, 없는 스테이지는 true로 지정한다.
    /// </summary>
    public void SetAutoStart(bool autoStart)
    {
        _autoStartOnPlay = autoStart;
    }

    private void Start()
    {
        _playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (_playerTransform == null)
        {
            Debug.LogError("[WaveSpawner] Player 태그 오브젝트를 찾을 수 없음");
            return;
        }

        // 활성 스테이지의 스폰 영역을 집는다. 비활성 맵의 영역은 제외된다.
        _spawnArea = FindFirstObjectByType<StageSpawnArea>();

        PrewarmBoss();

        GameManager.OnGameStateChanged += OnGameStateChanged;

        if (_autoStartOnPlay)
            BeginEncounter();
    }

    /// <summary>
    /// 인카운터(웨이브 스폰)를 시작한다. 봉인 트리거(ArenaEncounterTrigger) 등이 호출.
    /// _autoStartOnPlay=false일 때 이 호출 전까지는 몬스터가 스폰되지 않는다. 중복 호출은 무시.
    /// </summary>
    public void BeginEncounter()
    {
        if (_encounterStarted) return;
        _encounterStarted = true;
        StartWave(0);
        Debug.Log("[WaveSpawner] 인카운터 시작");
    }

    // 보스 등장 시 hitch 방지: 프리팹을 화면 밖에 비활성 상태로 미리 Instantiate하고
    // BGM AudioClip도 사전 로드해 메시/머티리얼/애니메이터/오디오를 GPU·메모리에 워밍.
    private void PrewarmBoss()
    {
        if (_bossPrefab == null) return;

        _bossInstance = Instantiate(_bossPrefab, PrewarmPosition, Quaternion.identity);
        _bossInstance.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PreloadBgm(BgmTrack.BossBattle);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (_playerTransform == null) return;
        if (!_encounterStarted) return;

        if (_inTransition)
        {
            _transitionTimer -= Time.deltaTime;
            if (_transitionTimer <= 0f)
                StartWave(CurrentWave); // CurrentWave는 이미 다음 인덱스로 증가된 상태
            return;
        }

        if (!_isSpawning) return;

        // 스폰 타이머
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnEnemy();
            float baseInterval = _waves[CurrentWave - 1].SpawnInterval;
            float spawnMult = PlayerStats.Instance != null
                ? PlayerStats.Instance.DifficultySpawnMultiplier
                : 1f;
            _spawnTimer = baseInterval / Mathf.Max(spawnMult, 0.01f);
        }

        // 웨이브 타이머
        _waveTimer -= Time.deltaTime;
        WaveTimeRemaining = Mathf.Max(0f, _waveTimer);

        if (_waveTimer <= 0f)
            AdvanceWave();
    }

    private void SpawnEnemy()
    {
        // 플레이어 주변 랜덤 방향에서 스폰
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnRadius;
        Vector3 spawnPos = _playerTransform.position + offset;

        // 스폰 영역이 있으면 맵 안쪽으로 제한 + 바닥 높이 스냅 (맵 밖 스폰 방지)
        if (_spawnArea != null)
            spawnPos = _spawnArea.Clamp(spawnPos);

        WaveData wave = _waves[CurrentWave - 1];

        GameObject obj = ObjectPool.Instance.GetFromPool(EnemyType.Mob);
        if (obj == null) return;

        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;

        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.ApplyWaveScale(wave.HpMultiplier, wave.SpeedMultiplier);
        enemy.Activate(_playerTransform);
    }

    private void AdvanceWave()
    {
        // 지정된 웨이브 종료 후 보스 스폰 (한 번만)
        if (!_bossSpawned && _bossSpawnAfterWave > 0 && CurrentWave == _bossSpawnAfterWave)
            SpawnBoss();

        if (CurrentWave >= _waves.Length)
        {
            // 모든 웨이브 완료. 보스 모드일 때는 보스 사망이 Win을 트리거하므로 여기선 스폰만 정지.
            _isSpawning = false;
            if (_bossSpawnAfterWave <= 0 && !_bossSpawned)
            {
                // 보스 모드 비활성 — 기존 동작(웨이브 클리어 = Win)
                GameManager.Instance.ChangeState(GameState.Win);
            }
            else
            {
                Debug.Log("[WaveSpawner] 모든 웨이브 종료 — 보스 사망 대기 중");
            }
            return;
        }

        CurrentWave++;
        _inTransition = true;
        _transitionTimer = _waveTransitionDelay;
        _isSpawning = false;

        Debug.Log($"[WaveSpawner] Wave {CurrentWave} 전환 중...");
    }

    private void SpawnBoss()
    {
        if (_bossPrefab == null)
        {
            Debug.LogWarning("[WaveSpawner] Boss 프리팹 미설정 — 보스 스폰 생략");
            _bossSpawned = true;
            return;
        }
        if (_playerTransform == null) return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _bossSpawnRadius;
        Vector3 spawnPos = _playerTransform.position + offset;

        // 스폰 영역이 있으면 맵 안쪽으로 제한 + 바닥 높이 스냅, 없으면 기존 폴백(최소 y=1)
        if (_spawnArea != null)
            spawnPos = _spawnArea.Clamp(spawnPos);
        else
            spawnPos.y = Mathf.Max(spawnPos.y, 1f);

        // Prewarmed 인스턴스 사용. 누락 시(SetBossPrefab이 Start 이후 호출된 경우 등) 즉시 Instantiate 폴백.
        GameObject obj = _bossInstance;
        if (obj == null)
        {
            obj = Instantiate(_bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            obj.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            obj.SetActive(true);
            _bossInstance = null;
        }

        var boss = obj.GetComponent<BossEnemy>();
        if (boss == null)
        {
            Debug.LogError("[WaveSpawner] Boss 프리팹에 BossEnemy 컴포넌트 없음");
            Destroy(obj);
            return;
        }

        boss.OnSpawn();
        boss.Activate(_playerTransform);
        _bossSpawned = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm(BgmTrack.BossBattle);

        Debug.Log($"[WaveSpawner] Boss 스폰 (Wave {CurrentWave} 종료 후)");
    }

    private void StartWave(int waveIndex)
    {
        CurrentWave = waveIndex + 1;
        _waveTimer = _waveDuration;
        WaveTimeRemaining = _waveDuration;
        _spawnTimer = 0f;
        _inTransition = false;
        _isSpawning = true;

        Debug.Log($"[WaveSpawner] Wave {CurrentWave} 시작");
    }

    private void OnGameStateChanged(GameState state)
    {
        _isSpawning = _encounterStarted && state == GameState.Playing && !_inTransition;
    }
}
