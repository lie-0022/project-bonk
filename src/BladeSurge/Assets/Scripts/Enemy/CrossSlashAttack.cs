using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 십자(방사형 직선) 충격파 공격. 보스를 중심으로 여러 방향의 직선 경로에
/// 직사각형 예고(TelegraphIndicator.SetupRect)를 깔고, windup 후 라인 위를 동시에 타격한다.
/// 발동마다 기준 각도를 랜덤 회전시켜 같은 자리에 서 있으면 맞도록 한다(가로 회피 강제).
/// BossAttackBase 상속 — 프리팹에 부착하면 BossAttackOrchestrator가 자동 등록.
/// </summary>
public class CrossSlashAttack : BossAttackBase
{
    [Header("Cross Slash")]
    [Tooltip("직선 개수. 4=십자.")]
    [SerializeField] private int _lineCount = 4;
    [Tooltip("보스 중심에서 한 방향으로 뻗는 길이(m).")]
    [SerializeField] private float _lineLength = 18f;
    [Tooltip("직선 폭(m).")]
    [SerializeField] private float _lineWidth = 4f;
    [Tooltip("직사각형 예고 프리팹(TelegraphIndicator). Telegraph_Rect.")]
    [SerializeField] private GameObject _rectTelegraphPrefab;
    [Tooltip("발동마다 기준 각도를 랜덤화한다(패턴 암기 방지).")]
    [SerializeField] private bool _randomizeAngle = true;

    protected override IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        _firing = true;

        Vector3 origin = SnapToGround(transform.position);
        int count = Mathf.Max(1, _lineCount);
        float baseAngle = _randomizeAngle ? Random.Range(0f, 360f / count) : 0f;

        // 방향 확정 + 예고 표시 (예고와 타격 라인이 정확히 일치하도록 방향을 저장)
        var dirs = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float a = (baseAngle + 360f / count * i) * Mathf.Deg2Rad;
            dirs[i] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));

            if (_rectTelegraphPrefab != null)
            {
                var go = Instantiate(_rectTelegraphPrefab);
                var ind = go.GetComponent<TelegraphIndicator>();
                if (ind != null) ind.SetupRect(origin, dirs[i], _lineWidth, _lineLength, _windupDuration);
            }
        }

        yield return new WaitForSeconds(_windupDuration);

        // windup 중 보스 사망 시 타격 생략 (시각 임팩트도 생략 — 예고는 자연 페이드)
        bool bossAlive = boss != null && boss.Health != null && boss.Health.IsAlive;
        if (bossAlive)
        {
            // 임팩트 시각: 각 라인을 따라 일정 간격으로 폭발 표시
            if (_impactPrefab != null)
            {
                for (int i = 0; i < count; i++)
                {
                    for (float d = _lineWidth * 0.5f; d < _lineLength; d += 3f)
                    {
                        var imp = Instantiate(_impactPrefab);
                        var burst = imp.GetComponent<ImpactBurst>();
                        if (burst != null) burst.Setup(origin + dirs[i] * d, _lineWidth * 0.6f);
                    }
                }
            }

            // 데미지: 플레이어가 어느 한 라인 안에 있으면 1회 타격
            if (player != null)
            {
                Vector3 toP = player.position - origin;
                toP.y = 0f;
                float halfWidth = _lineWidth * 0.5f + 0.5f; // 캡슐 반경 여유
                for (int i = 0; i < count; i++)
                {
                    float along = Vector3.Dot(toP, dirs[i]);
                    if (along < -1f || along > _lineLength) continue;
                    float perp = (toP - dirs[i] * along).magnitude;
                    if (perp > halfWidth) continue;

                    var hp = player.GetComponent<HealthComponent>();
                    if (hp != null)
                        DamageDealer.Deal(new DamageInfo(_damage, DamageSource.Enemy, gameObject), hp);
                    break;
                }
            }
        }

        _firing = false;
    }
}
