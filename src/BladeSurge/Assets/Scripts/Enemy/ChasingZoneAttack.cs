using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 추적 장판 공격. 플레이어 발밑에 원형 예고를 띄우고 일정 시간 플레이어를 따라다니다가,
/// 마지막에 위치를 잠그고 짧은 딜레이 후 폭발한다. 제자리에 서서 딜만 하는 플레이를 응징한다.
/// 장판 이동 속도는 플레이어 기본 이속보다 약간 느려 계속 움직이면 떨칠 수 있다.
/// BossAttackBase 상속 — _radius/_damage/_telegraphPrefab/_impactPrefab 사용.
/// </summary>
public class ChasingZoneAttack : BossAttackBase
{
    [Header("Chasing Zone")]
    [Tooltip("플레이어를 추적하는 시간(초).")]
    [SerializeField] private float _chaseDuration = 1.4f;
    [Tooltip("장판 추적 속도(m/s). 플레이어 기본 이속(6)보다 낮게 — 움직이면 떨칠 수 있다.")]
    [SerializeField] private float _followSpeed = 4.5f;
    [Tooltip("추적 종료 후 폭발까지의 잠금 시간(초) — 마지막 회피 기회.")]
    [SerializeField] private float _lockDelay = 0.35f;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;

        // 시작 위치: 플레이어 발밑
        Vector3 zonePos = SnapToGround(player != null ? player.position : transform.position);
        float groundY = zonePos.y;

        GameObject zoneGo = null;
        if (_telegraphPrefab != null)
        {
            zoneGo = Instantiate(_telegraphPrefab);
            var ind = zoneGo.GetComponent<TelegraphIndicator>();
            if (ind != null) ind.Setup(zonePos, _radius, _chaseDuration + _lockDelay);
        }

        // 추적: 장판이 플레이어 XZ를 향해 일정 속도로 이동 (Paused 시 timeScale=0 → deltaTime 0으로 자동 정지)
        float t = 0f;
        while (t < _chaseDuration)
        {
            t += Time.deltaTime;

            if (player != null)
            {
                Vector3 target = new Vector3(player.position.x, groundY, player.position.z);
                zonePos = Vector3.MoveTowards(zonePos, target, _followSpeed * Time.deltaTime);
            }
            if (zoneGo != null)
                zoneGo.transform.position = new Vector3(zonePos.x, groundY + 0.02f, zonePos.z);

            if (boss == null || boss.Health == null || !boss.Health.IsAlive) break;
            yield return null;
        }

        // 잠금 — 위치 고정, 마지막 회피 기회
        yield return new WaitForSeconds(_lockDelay);

        bool bossAlive = boss != null && boss.Health != null && boss.Health.IsAlive;
        if (bossAlive)
        {
            if (_impactPrefab != null)
            {
                var imp = Instantiate(_impactPrefab);
                var burst = imp.GetComponent<ImpactBurst>();
                if (burst != null) burst.Setup(zonePos, _radius);
            }

            var hits = Physics.OverlapSphere(zonePos + Vector3.up * 1.5f, _radius);
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
