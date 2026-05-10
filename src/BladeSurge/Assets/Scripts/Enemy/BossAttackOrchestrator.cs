using UnityEngine;

/// <summary>
/// 보스에 부착된 BossAttackBase[] 들을 라운드로빈 + 쿨다운으로 발동.
/// 보스가 활성 상태일 때만 작동.
/// </summary>
[RequireComponent(typeof(BossEnemy))]
public class BossAttackOrchestrator : MonoBehaviour
{
    [Tooltip("첫 공격까지의 초기 지연(스폰 애니 후 잠깐 쉬는 시간).")]
    [SerializeField] private float _initialDelay = 3f;
    [Tooltip("공격 사이의 인터벌(초). 공격 자체의 _cooldown 과 합쳐짐.")]
    [SerializeField] private float _intervalBetweenAttacks = 1.0f;

    private BossEnemy _boss;
    private Transform _player;
    private BossAttackBase[] _attacks;
    private int _nextIdx;
    private float _timer;

    private void Awake()
    {
        _boss = GetComponent<BossEnemy>();
        _attacks = GetComponents<BossAttackBase>();
    }

    private void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
        _timer = _initialDelay;
    }

    private void Update()
    {
        if (_attacks == null || _attacks.Length == 0) return;
        if (_boss == null || !_boss.IsActive) return;
        if (_boss.Health == null || !_boss.Health.IsAlive) return;
        if (_player == null) return;

        // 공격 중이면 대기
        for (int i = 0; i < _attacks.Length; i++)
            if (_attacks[i] != null && _attacks[i].IsFiring) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        // 다음 공격 발동
        var atk = _attacks[_nextIdx];
        _nextIdx = (_nextIdx + 1) % _attacks.Length;
        if (atk != null && atk.TryFire(_boss, _player))
        {
            _timer = atk.Cooldown + _intervalBetweenAttacks;
        }
        else
        {
            _timer = _intervalBetweenAttacks;  // 발동 실패 시 짧게 재시도
        }
    }
}
