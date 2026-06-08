using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 졸개 소환 공격. windup 후 보스 주변에 잡몹(ObjectPool의 Mob)을 여러 마리 소환한다.
/// 소환되는 잡몹 종류는 DebugStageSelector가 스테이지 종족에 맞춰 ObjectPool에 설정한 것을 따른다.
/// BossAttackBase를 상속하므로 보스 프리팹에 부착하면 BossAttackOrchestrator가 자동 등록한다.
/// </summary>
public class SummonAttack : BossAttackBase
{
    [Header("Summon")]
    [Tooltip("한 번에 소환할 잡몹 수.")]
    [SerializeField] private int _summonCount = 4;
    [Tooltip("보스로부터 소환 위치 반경(m).")]
    [SerializeField] private float _summonRadius = 4f;
    [Tooltip("소환 잡몹 체력 배율.")]
    [SerializeField] private float _summonHpScale = 1f;
    [Tooltip("소환 잡몹 속도 배율.")]
    [SerializeField] private float _summonSpeedScale = 1f;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;

        if (_telegraphPrefab != null)
        {
            var go = Instantiate(_telegraphPrefab);
            var indicator = go.GetComponent<TelegraphIndicator>();
            if (indicator != null) indicator.Setup(transform.position, _summonRadius, _windupDuration);
        }
        yield return new WaitForSeconds(_windupDuration);

        if (boss != null && boss.Health != null && boss.Health.IsAlive && ObjectPool.Instance != null)
        {
            for (int i = 0; i < _summonCount; i++)
            {
                float angle = (360f / Mathf.Max(1, _summonCount)) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _summonRadius;
                Vector3 pos = transform.position + offset;

                GameObject obj = ObjectPool.Instance.GetFromPool(EnemyType.Mob);
                if (obj == null) continue;

                obj.transform.SetPositionAndRotation(pos, Quaternion.identity);
                var enemy = obj.GetComponent<EnemyBase>();
                if (enemy == null) continue;
                enemy.ApplyWaveScale(_summonHpScale, _summonSpeedScale);
                enemy.Activate(player);
            }
        }

        _firing = false;
    }
}
