using System.Collections;
using UnityEngine;

/// <summary>검 공격의 데미지 판정 방식.</summary>
public enum SwordHitMode
{
    /// <summary>박스가 호를 그리며 매 프레임 닿은 적만 처리. 박스가 쓸고 지나간 적만 맞음.</summary>
    Sweep,
    /// <summary>시각은 sweep과 동일하나 시작 시점에 부채꼴 영역 내 적 모두에게 즉시 일괄 데미지.</summary>
    BurstAreaOnce
}

/// <summary>
/// 검 공격. 콜라이더를 플레이어 전방 기준으로 호 형태로 회전시켜 적을 타격한다.
/// 발사체 수(연속 횟수)만큼 0.3초 간격으로 반복 휘두른다.
/// _hitMode로 데미지 판정 방식 토글 가능.
/// </summary>
public class SwordAttack : MonoBehaviour
{
    [Header("Hit Mode")]
    [SerializeField] private SwordHitMode _hitMode = SwordHitMode.Sweep;

    [Header("References")]
    [SerializeField] private Transform _playerTransform; // 플레이어 루트 (Inspector 연결)
    [SerializeField] private Transform _pivotPoint;      // 회전 중심 (플레이어)
    [SerializeField] private Collider _sweepCollider;    // 부채꼴 콜라이더

    private MeshRenderer _sweepRenderer; // 디버그 시각화용 (있을 때만)
    private bool _isSweeping;

    private void Awake()
    {
        if (_sweepCollider != null)
        {
            _sweepCollider.enabled = false;
            _sweepRenderer = _sweepCollider.GetComponent<MeshRenderer>();
            if (_sweepRenderer != null) _sweepRenderer.enabled = false;
        }
    }

    public void Execute(WeaponSlot slot, PlayerStats stats)
    {
        if (_isSweeping) return;
        Debug.Log($"[SwordAttack] Execute mode={_hitMode} swingCount={stats.ProjectileCount} sweepAngle={slot.Data.SweepAngle * stats.SizeMultiplier} dur={slot.Data.SweepDuration / stats.ProjectileSpeed:F2}s");
        StartCoroutine(SwingSequence(slot, stats));
    }

    private IEnumerator SwingSequence(WeaponSlot slot, PlayerStats stats)
    {
        _isSweeping = true;
        int swingCount = stats.ProjectileCount;
        float sweepDuration = slot.Data.SweepDuration / stats.ProjectileSpeed;
        float sweepAngle = slot.Data.SweepAngle * stats.SizeMultiplier;
        float damage = slot.Data.BaseDamage;

        for (int i = 0; i < swingCount; i++)
        {
            yield return StartCoroutine(SingleSwing(sweepAngle, sweepDuration, damage, stats));

            if (i < swingCount - 1)
                yield return new WaitForSeconds(slot.Data.SwingInterval);
        }

        _isSweeping = false;
    }

    private IEnumerator SingleSwing(float sweepAngle, float sweepDuration, float damage, PlayerStats stats)
    {
        // 플레이어 facing 방향 기준 시작/끝 각도
        float facing = _playerTransform.eulerAngles.y;
        float startAngle = facing - sweepAngle * 0.5f;
        float endAngle = facing + sweepAngle * 0.5f;

        _sweepCollider.enabled = true;
        if (_sweepRenderer != null) _sweepRenderer.enabled = true;

        // BurstAreaOnce: 시각 sweep 시작 시점에 한 번에 부채꼴 영역 데미지
        if (_hitMode == SwordHitMode.BurstAreaOnce)
            ApplyBurstAreaDamage(facing, sweepAngle, damage, stats);

        var hitTargets = new System.Collections.Generic.HashSet<Collider>();

        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sweepDuration);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // 시각 sweep (두 모드 공통) — 콜라이더 위치/회전 갱신
            float rad = currentAngle * Mathf.Deg2Rad;
            float reach = _sweepCollider.bounds.extents.z;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * reach;
            _sweepCollider.transform.position = _playerTransform.position + offset;
            _sweepCollider.transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);

            // Sweep 모드: 매 프레임 OverlapBox로 박스 안 적 검사
            if (_hitMode == SwordHitMode.Sweep)
            {
                Collider[] hits = Physics.OverlapBox(
                    _sweepCollider.bounds.center,
                    _sweepCollider.bounds.extents,
                    _sweepCollider.transform.rotation);

                if (hits.Length > 0)
                    Debug.Log($"[SwordAttack] OverlapBox hits={hits.Length} at {_sweepCollider.bounds.center}");

                foreach (var hit in hits)
                {
                    if (hitTargets.Contains(hit)) continue;
                    var health = hit.GetComponent<HealthComponent>();
                    if (health == null || !hit.CompareTag("Enemy")) continue;

                    hitTargets.Add(hit);
                    ApplyHit(health, damage, stats);
                }
            }

            yield return null;
        }

        _sweepCollider.enabled = false;
        if (_sweepRenderer != null) _sweepRenderer.enabled = false;
    }

    /// <summary>BurstAreaOnce 모드: 부채꼴 영역 안 적 모두 즉시 일괄 데미지.</summary>
    private void ApplyBurstAreaDamage(float facing, float sweepAngle, float damage, PlayerStats stats)
    {
        float range = _sweepCollider.bounds.size.z; // sweep 모드와 동일 사거리(박스 z 길이)
        Vector3 origin = _playerTransform.position;
        Vector3 forward = Quaternion.Euler(0f, facing, 0f) * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
        float halfAngle = sweepAngle * 0.5f;

        Collider[] hits = Physics.OverlapSphere(origin, range);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var health = hit.GetComponent<HealthComponent>();
            if (health == null) continue;

            Vector3 toEnemy = hit.transform.position - origin;
            toEnemy.y = 0f;
            // 플레이어와 동일 위치에 있는 적은 무조건 적중
            if (toEnemy.sqrMagnitude < 0.0001f)
            {
                ApplyHit(health, damage, stats);
                hitCount++;
                continue;
            }
            toEnemy.Normalize();

            float angleDiff = Vector3.Angle(forward, toEnemy);
            if (angleDiff <= halfAngle)
            {
                ApplyHit(health, damage, stats);
                hitCount++;
            }
        }

        if (hitCount > 0)
            Debug.Log($"[SwordAttack] Burst area hits={hitCount} range={range:F2} angle={sweepAngle:F0}");
    }

    private void ApplyHit(HealthComponent health, float damage, PlayerStats stats)
    {
        float finalDamage = ApplyCrit(damage, stats);
        DamageDealer.Deal(new DamageInfo(finalDamage, DamageSource.Player, gameObject), health);
        ApplyLifesteal(finalDamage, stats);
    }

    private float ApplyCrit(float damage, PlayerStats stats)
    {
        if (UnityEngine.Random.value < stats.CritChance)
            return damage * stats.CritMultiplier;
        return damage;
    }

    private void ApplyLifesteal(float damage, PlayerStats stats)
    {
        if (stats.Lifesteal <= 0f) return;
        var playerHealth = _playerTransform.GetComponent<HealthComponent>();
        playerHealth?.Heal(damage * stats.Lifesteal);
    }
}
