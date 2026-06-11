using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 점멸 강타. 플레이어의 현재 위치에 원형 예고를 표시하고, windup이 끝나면
/// 보스가 그 지점으로 순간이동하며 범위 슬램을 터뜨린다. 거리 유지(카이팅) 플레이를 응징한다.
/// 예고는 시전 시점 위치에 고정되므로 windup 동안 벗어나면 회피할 수 있다.
/// BossAttackBase 상속 — _radius(슬램 반경)/_damage/_windupDuration/_telegraphPrefab/_impactPrefab 사용.
/// </summary>
public class TeleportStrikeAttack : BossAttackBase
{
    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;
        var ai = GetComponent<BossAI>();
        var rb = GetComponent<Rigidbody>();

        // 시전 동안 제자리 고정 (마법 캐스팅 느낌 + 예고 지점과의 정합)
        if (ai != null) ai.SuspendMovement = true;
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // 목적지: 시전 시점의 플레이어 위치 (XZ만 — 같은 층 가정, 보스 y 유지)
        Vector3 destXZ = player != null ? player.position : transform.position;
        Vector3 telegraphPos = SnapToGround(destXZ);

        if (_telegraphPrefab != null)
        {
            var go = Instantiate(_telegraphPrefab);
            var ind = go.GetComponent<TelegraphIndicator>();
            if (ind != null) ind.Setup(telegraphPos, _radius, _windupDuration);
        }

        yield return new WaitForSeconds(_windupDuration);

        bool bossAlive = boss != null && boss.Health != null && boss.Health.IsAlive;
        if (bossAlive)
        {
            // 순간이동 (XZ 이동, y는 현재 층 유지). Rigidbody 위치도 동기화해 물리 워프 잔상 방지.
            Vector3 warp = new Vector3(destXZ.x, transform.position.y, destXZ.z);
            transform.position = warp;
            if (rb != null) rb.position = warp;

            if (_impactPrefab != null)
            {
                var imp = Instantiate(_impactPrefab);
                var burst = imp.GetComponent<ImpactBurst>();
                if (burst != null) burst.Setup(telegraphPos, _radius);
            }

            var hits = Physics.OverlapSphere(telegraphPos + Vector3.up * 1.5f, _radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].CompareTag("Player")) continue;
                var hp = hits[i].GetComponent<HealthComponent>();
                if (hp != null)
                    DamageDealer.Deal(new DamageInfo(_damage, DamageSource.Enemy, gameObject), hp);
                break;
            }
        }

        if (ai != null) ai.SuspendMovement = false;
        _firing = false;
    }
}
