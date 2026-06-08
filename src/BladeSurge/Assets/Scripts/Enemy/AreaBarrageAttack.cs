using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 원거리 범위 공격(다중 원 폭격). 플레이어 주변(또는 보스 주변)에 빨간 예고 원을 여러 개
/// 동시에 표시한 뒤, windup이 끝나면 각 원 범위를 동시에 타격한다.
/// GroundSlamAttack(단일 원, 플레이어 정조준)과 달리 여러 지점을 동시에 노려 회피를 강제한다.
/// BossAttackBase 상속 — 프리팹에 부착하면 BossAttackOrchestrator가 자동 등록.
/// </summary>
public class AreaBarrageAttack : BossAttackBase
{
    [Header("Barrage")]
    [Tooltip("동시에 떨어뜨릴 원의 개수.")]
    [SerializeField] private int _circleCount = 4;
    [Tooltip("원들이 분산되는 반경(중심으로부터). 클수록 넓게 흩뿌림.")]
    [SerializeField] private float _spread = 8f;
    [Tooltip("true=플레이어 주변에 분산, false=보스 주변에 분산.")]
    [SerializeField] private bool _aroundPlayer = true;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;

        // 중심은 플레이어(또는 보스) 위치. y는 유지 후 각 원을 실제 바닥으로 스냅(멀티레벨 대응).
        Vector3 center = (_aroundPlayer && player != null) ? player.position : transform.position;

        // 예고 원 동시 표시
        var positions = new List<Vector3>(_circleCount);
        for (int i = 0; i < _circleCount; i++)
        {
            Vector2 off = Random.insideUnitCircle * _spread;
            Vector3 p = SnapToGround(center + new Vector3(off.x, 0f, off.y)); // 바닥 높이로
            positions.Add(p);

            if (_telegraphPrefab != null)
            {
                var go = Instantiate(_telegraphPrefab);
                var ind = go.GetComponent<TelegraphIndicator>();
                if (ind != null) ind.Setup(p, _radius, _windupDuration);
            }
        }

        yield return new WaitForSeconds(_windupDuration);

        // 동시 타격
        bool bossAlive = boss != null && boss.Health != null && boss.Health.IsAlive;
        foreach (var p in positions)
        {
            if (_impactPrefab != null)
            {
                var imp = Instantiate(_impactPrefab);
                var burst = imp.GetComponent<ImpactBurst>();
                if (burst != null) burst.Setup(p, _radius);
            }

            if (!bossAlive) continue;
            var hits = Physics.OverlapSphere(p + Vector3.up * 1.5f, _radius); // 바닥에서 띄워 플레이어 포함
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].CompareTag("Player")) continue;
                var hp = hits[i].GetComponent<HealthComponent>();
                if (hp != null)
                    DamageDealer.Deal(new DamageInfo(_damage, DamageSource.Enemy, gameObject), hp);
                break;
            }
        }

        _firing = false;
    }
}
