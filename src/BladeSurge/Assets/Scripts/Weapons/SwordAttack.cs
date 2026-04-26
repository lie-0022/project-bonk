using System.Collections;
using UnityEngine;

/// <summary>
/// 검 공격. 콜라이더를 플레이어 전방 기준으로 호 형태로 회전시켜 적을 타격한다.
/// 발사체 수(연속 횟수)만큼 0.3초 간격으로 반복 휘두른다.
/// </summary>
public class SwordAttack : MonoBehaviour
{
    [SerializeField] private Transform _pivotPoint;     // 회전 중심 (플레이어)
    [SerializeField] private Collider _sweepCollider;   // 부채꼴 콜라이더

    private Transform _playerTransform;
    private bool _isSweeping;

    private void Awake()
    {
        _playerTransform = transform.root;
        if (_sweepCollider != null)
            _sweepCollider.enabled = false;
    }

    public void Execute(WeaponSlot slot, PlayerStats stats)
    {
        if (_isSweeping) return;
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
        var hitTargets = new System.Collections.Generic.HashSet<Collider>();

        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sweepDuration);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // 콜라이더 위치 회전
            float rad = currentAngle * Mathf.Deg2Rad;
            float reach = _sweepCollider.bounds.extents.z;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * reach;
            _sweepCollider.transform.position = _playerTransform.position + offset;
            _sweepCollider.transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);

            // 콜라이더 겹침 체크
            Collider[] hits = Physics.OverlapBox(
                _sweepCollider.bounds.center,
                _sweepCollider.bounds.extents,
                _sweepCollider.transform.rotation);

            foreach (var hit in hits)
            {
                if (hitTargets.Contains(hit)) continue;
                var health = hit.GetComponent<HealthComponent>();
                if (health == null || !hit.CompareTag("Enemy")) continue;

                hitTargets.Add(hit);
                float finalDamage = ApplyCrit(damage, stats);
                DamageDealer.Deal(new DamageInfo(finalDamage, DamageSource.Player, gameObject), health);
                ApplyLifesteal(finalDamage, stats);
            }

            yield return null;
        }

        _sweepCollider.enabled = false;
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
