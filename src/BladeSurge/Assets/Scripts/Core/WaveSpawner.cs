using UnityEngine;

/// <summary>웨이브 한 개의 설정값.</summary>
[System.Serializable]
public struct WaveData
{
    public float SpawnInterval;
    public float ChaserRatio;    // 0~1, 나머지는 Rusher
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

    [Header("Wave Data")]
    [SerializeField] private WaveData[] _waves = new WaveData[]
    {
        new WaveData { SpawnInterval = 2.0f, ChaserRatio = 1.00f, HpMultiplier = 1.0f, SpeedMultiplier = 1.0f },
        new WaveData { SpawnInterval = 1.5f, ChaserRatio = 0.80f, HpMultiplier = 1.2f, SpeedMultiplier = 1.1f },
        new WaveData { SpawnInterval = 1.2f, ChaserRatio = 0.70f, HpMultiplier = 1.5f, SpeedMultiplier = 1.2f },
        new WaveData { SpawnInterval = 1.0f, ChaserRatio = 0.60f, HpMultiplier = 1.8f, SpeedMultiplier = 1.3f },
        new WaveData { SpawnInterval = 0.8f, ChaserRatio = 0.50f, HpMultiplier = 2.2f, SpeedMultiplier = 1.5f },
    };

    /// <summary>현재 웨이브 번호 (1-based). HUD 표시용.</summary>
    public int CurrentWave { get; private set; } = 1;

    /// <summary>현재 웨이브 남은 시간(초). HUD 표시용.</summary>
    public float WaveTimeRemaining { get; private set; }

    private Transform _playerTransform;
    private bool _isSpawning;
    private float _spawnTimer;
    private float _waveTimer;
    private bool _inTransition;
    private float _transitionTimer;

    private void Start()
    {
        _playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (_playerTransform == null)
        {
            Debug.LogError("[WaveSpawner] Player 태그 오브젝트를 찾을 수 없음");
            return;
        }

        GameManager.OnGameStateChanged += OnGameStateChanged;
        StartWave(0);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

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

        WaveData wave = _waves[CurrentWave - 1];
        EnemyType type = Random.value < wave.ChaserRatio ? EnemyType.Chaser : EnemyType.Rusher;

        GameObject obj = ObjectPool.Instance.GetFromPool(type);
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
        if (CurrentWave >= _waves.Length)
        {
            // 모든 웨이브 완료 → Win
            _isSpawning = false;
            GameManager.Instance.ChangeState(GameState.Win);
            return;
        }

        CurrentWave++;
        _inTransition = true;
        _transitionTimer = _waveTransitionDelay;
        _isSpawning = false;

        Debug.Log($"[WaveSpawner] Wave {CurrentWave} 전환 중...");
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
        _isSpawning = state == GameState.Playing && !_inTransition;
    }
}
