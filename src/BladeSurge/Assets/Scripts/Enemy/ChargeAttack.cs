using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 직선 돌진 공격. windup(조준 예고) 후 플레이어 방향으로 빠르게 직진하며,
/// 돌진 경로에서 플레이어에 닿으면 데미지를 준다(돌진 1회당 1타). 벽에 막히면 멈춘다(물리 속도 이동).
/// 돌진 동안 BossAI.SuspendMovement로 보스의 일반 이동을 정지시킨다.
/// BossAttackBase 상속 — 프리팹에 부착하면 BossAttackOrchestrator가 자동 등록.
/// </summary>
[RequireComponent(typeof(BossAI))]
public class ChargeAttack : BossAttackBase
{
    [Header("Charge")]
    [Tooltip("돌진 속도(m/s).")]
    [SerializeField] private float _chargeSpeed = 18f;
    [Tooltip("돌진 지속 시간(초).")]
    [SerializeField] private float _chargeDuration = 0.7f;
    [Tooltip("돌진 중 플레이어 타격 판정 반경(m).")]
    [SerializeField] private float _hitRadius = 2f;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;
        var ai = GetComponent<BossAI>();
        var rb = GetComponent<Rigidbody>();

        // 조준 + 윈드업(예고)
        Vector3 dir = player != null ? (player.position - transform.position) : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) { dir.Normalize(); transform.rotation = Quaternion.LookRotation(dir); }

        if (_telegraphPrefab != null)
        {
            var go = Instantiate(_telegraphPrefab);
            var ind = go.GetComponent<TelegraphIndicator>();
            if (ind != null) ind.Setup(transform.position, _radius, _windupDuration);
        }
        yield return new WaitForSeconds(_windupDuration);

        // 방향 고정 후 돌진
        if (ai != null) ai.SuspendMovement = true;
        bool hit = false;
        float t = 0f;
        while (t < _chargeDuration)
        {
            bool paused = DropItemEffects.TimeStopActive ||
                          (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing);
            if (!paused)
            {
                t += Time.deltaTime;
                if (rb != null)
                    rb.linearVelocity = new Vector3(dir.x * _chargeSpeed, rb.linearVelocity.y, dir.z * _chargeSpeed);

                if (!hit && player != null)
                {
                    float dx = transform.position.x - player.position.x;
                    float dz = transform.position.z - player.position.z;
                    if (dx * dx + dz * dz < _hitRadius * _hitRadius)
                    {
                        var hp = player.GetComponent<HealthComponent>();
                        if (hp != null)
                            DamageDealer.Deal(new DamageInfo(_damage, DamageSource.Enemy, gameObject), hp);
                        hit = true;
                    }
                }
            }
            else if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            if (boss == null || boss.Health == null || !boss.Health.IsAlive) break;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (ai != null) ai.SuspendMovement = false;
        _firing = false;
    }
}
