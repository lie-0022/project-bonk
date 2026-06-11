using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 직선 돌진 공격. windup 동안 돌진 경로에 직사각형 예고를 깔고, 플레이어 방향으로 빠르게 직진한다.
/// 돌진 경로에서 플레이어에 닿으면 데미지(돌진 1회당 1타), 벽에 막히면 멈춘다(물리 속도 이동).
/// 착지 시 주위 360도 슬램(원형 예고 후 범위 데미지)을 하고, HP가 일정 비율 이하면 대쉬를 2연속으로 한다.
/// windup~돌진 동안 BossAI.SuspendMovement로 보스의 일반 이동을 정지시킨다(예고 경로와 실제 출발점 일치).
/// BossAttackBase 상속 — 프리팹에 부착하면 BossAttackOrchestrator가 자동 등록.
/// </summary>
[RequireComponent(typeof(BossAI))]
public class ChargeAttack : BossAttackBase
{
    [Header("Charge")]
    [Tooltip("돌진 속도(m/s).")]
    [SerializeField] private float _chargeSpeed = 26f;
    [Tooltip("돌진 지속 시간(초).")]
    [SerializeField] private float _chargeDuration = 0.55f;
    [Tooltip("돌진 중 플레이어 타격 판정 반경(m).")]
    [SerializeField] private float _hitRadius = 2.2f;

    [Header("Path Telegraph (직선 경로 예고)")]
    [Tooltip("돌진 경로에 깔 직사각형 예고 프리팹(TelegraphIndicator). 미설정 시 경로 예고 생략.")]
    [SerializeField] private GameObject _pathTelegraphPrefab;
    [Tooltip("경로 예고 폭(m). 히트 판정(_hitRadius×2)과 비슷하게 맞춘다.")]
    [SerializeField] private float _pathWidth = 4f;

    [Header("Landing Slam (착지 360도 범위 공격)")]
    [Tooltip("착지 슬램 예고(원형) 프리팹. 미설정 시 슬램 생략.")]
    [SerializeField] private GameObject _slamTelegraphPrefab;
    [Tooltip("슬램 폭발 시각 프리팹(ImpactBurst).")]
    [SerializeField] private GameObject _slamImpactPrefab;
    [Tooltip("슬램 반경(m).")]
    [SerializeField] private float _slamRadius = 3.5f;
    [Tooltip("슬램 데미지.")]
    [SerializeField] private float _slamDamage = 25f;
    [Tooltip("착지 후 슬램 폭발까지의 예고 시간(초). 짧게 — 회피 마지막 기회.")]
    [SerializeField] private float _slamDelay = 0.45f;

    [Header("Phase 2 (저체력 2연속 대쉬)")]
    [Tooltip("보스 HP가 이 비율 이하면 대쉬를 2연속으로 한다. 0이면 항상 1회.")]
    [Range(0f, 1f)]
    [SerializeField] private float _doubleDashHpRatio = 0.5f;
    [Tooltip("2번째 대쉬 전 재조준 예고 시간(초). 첫 windup보다 짧게 해 압박을 유지.")]
    [SerializeField] private float _secondWindup = 0.45f;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;
        var ai = GetComponent<BossAI>();
        var rb = GetComponent<Rigidbody>();

        // windup 동안에도 일반 이동을 멈춰 예고한 경로와 실제 출발점이 어긋나지 않게 한다.
        if (ai != null) ai.SuspendMovement = true;
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // 저체력 페이즈: 대쉬 2연속
        bool phase2 = _doubleDashHpRatio > 0f && boss != null && boss.Health != null
            && boss.Health.CurrentHp <= boss.Health.MaxHp * _doubleDashHpRatio;
        int dashCount = phase2 ? 2 : 1;

        for (int dash = 0; dash < dashCount; dash++)
        {
            // 조준 (대쉬마다 현재 플레이어 위치로 재조준)
            Vector3 dir = player != null ? (player.position - transform.position) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) { dir.Normalize(); transform.rotation = Quaternion.LookRotation(dir); }
            else dir = transform.forward;

            // 직선 경로 예고 — 길이는 실제 도달 거리(속도×시간)와 일치시킨다.
            float windup = dash == 0 ? _windupDuration : _secondWindup;
            if (_pathTelegraphPrefab != null)
            {
                float pathLength = _chargeSpeed * _chargeDuration + _hitRadius;
                var go = Instantiate(_pathTelegraphPrefab);
                var ind = go.GetComponent<TelegraphIndicator>();
                if (ind != null) ind.SetupRect(SnapToGround(transform.position), dir, _pathWidth, pathLength, windup);
            }
            yield return new WaitForSeconds(windup);
            if (boss == null || boss.Health == null || !boss.Health.IsAlive) break;

            // 방향 고정 후 돌진
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
            if (boss == null || boss.Health == null || !boss.Health.IsAlive) break;

            // 착지 360도 슬램: 원형 예고 → (보스 생존 시) 폭발 + 범위 데미지
            if (_slamTelegraphPrefab != null)
            {
                Vector3 slamPos = SnapToGround(transform.position);
                var go = Instantiate(_slamTelegraphPrefab);
                var ind = go.GetComponent<TelegraphIndicator>();
                if (ind != null) ind.Setup(slamPos, _slamRadius, _slamDelay);

                yield return new WaitForSeconds(_slamDelay);
                if (boss == null || boss.Health == null || !boss.Health.IsAlive) break;

                if (_slamImpactPrefab != null)
                {
                    var imp = Instantiate(_slamImpactPrefab);
                    var burst = imp.GetComponent<ImpactBurst>();
                    if (burst != null) burst.Setup(slamPos, _slamRadius);
                }

                var hits = Physics.OverlapSphere(slamPos + Vector3.up * 1.5f, _slamRadius);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (!hits[i].CompareTag("Player")) continue;
                    var hp = hits[i].GetComponent<HealthComponent>();
                    if (hp != null)
                        DamageDealer.Deal(new DamageInfo(_slamDamage, DamageSource.Enemy, gameObject), hp);
                    break;
                }
            }
        }

        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (ai != null) ai.SuspendMovement = false;
        _firing = false;
    }
}
