using UnityEngine;

/// <summary>
/// 보스 공격 1개의 추상 베이스. Wind-up → Impact → (cleanup) 라이프사이클.
/// BossAttackOrchestrator 가 _cooldown 마다 TryFire() 호출.
/// </summary>
public abstract class BossAttackBase : MonoBehaviour
{
    [Header("Common")]
    [Tooltip("이 공격 발동 간 쿨다운(초). Orchestrator가 이 값으로 라운드로빈/랜덤 발동.")]
    [SerializeField] protected float _cooldown = 5f;
    [Tooltip("Wind-up 시간(초). 텔레그래프 표시 후 데미지 판정까지의 대기.")]
    [SerializeField] protected float _windupDuration = 1.5f;
    [Tooltip("공격 데미지.")]
    [SerializeField] protected float _damage = 50f;
    [Tooltip("AOE 반경(미터).")]
    [SerializeField] protected float _radius = 4f;

    [Header("Visuals")]
    [SerializeField] protected GameObject _telegraphPrefab;
    [Tooltip("임팩트 순간 표시될 폭발 시각 prefab (ImpactBurst).")]
    [SerializeField] protected GameObject _impactPrefab;

    public float Cooldown => _cooldown;

    private bool _firing;
    public bool IsFiring => _firing;

    /// <summary>Orchestrator에서 호출. 이미 발동 중이면 false.</summary>
    public bool TryFire(BossEnemy boss, Transform player)
    {
        if (_firing) return false;
        if (boss == null || !boss.IsActive || boss.Health == null || !boss.Health.IsAlive) return false;
        StartCoroutine(FireRoutine(boss, player));
        return true;
    }

    protected abstract System.Collections.IEnumerator FireRoutine(BossEnemy boss, Transform player);

    /// <summary>
    /// 텔레그래프 표시 + windup 대기 후 worldPos 위치에 OverlapSphere로 플레이어 데미지.
    /// 서브클래스 공통 헬퍼.
    /// </summary>
    protected System.Collections.IEnumerator TelegraphAndImpact(Vector3 worldPos, float radius, BossEnemy boss)
    {
        _firing = true;
        // 텔레그래프 표시
        TelegraphIndicator indicator = null;
        if (_telegraphPrefab != null)
        {
            var go = Instantiate(_telegraphPrefab);
            indicator = go.GetComponent<TelegraphIndicator>();
            if (indicator != null) indicator.Setup(worldPos, radius, _windupDuration);
        }

        yield return new WaitForSeconds(_windupDuration);

        // 임팩트 시각
        if (_impactPrefab != null)
        {
            var impactGo = Instantiate(_impactPrefab);
            var burst = impactGo.GetComponent<ImpactBurst>();
            if (burst != null) burst.Setup(worldPos, radius);
        }

        // 임팩트: OverlapSphere로 플레이어 검출
        var hits = Physics.OverlapSphere(worldPos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Player")) continue;
            var hp = hits[i].GetComponent<HealthComponent>();
            if (hp == null) continue;
            DamageDealer.Deal(new DamageInfo(_damage, DamageSource.Enemy, gameObject), hp);
            break; // 플레이어 1명 가정
        }

        _firing = false;
    }
}
