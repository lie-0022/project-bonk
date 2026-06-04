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

    [Header("Slash VFX")]
    [Tooltip("휘두를 때 플레이어 전방에 스폰할 슬래시 이펙트. facing 방향으로 회전.")]
    [SerializeField] private GameObject _slashVfxPrefab;
    [Tooltip("슬래시 VFX 스폰 위치 오프셋(플레이어 전방/높이).")]
    [SerializeField] private Vector3 _slashVfxOffset = new Vector3(0f, 1f, 1.5f);
    [Tooltip("슬래시 VFX 자동 소멸 시간(초).")]
    [SerializeField] private float _slashVfxLifetime = 1.5f;
    [Tooltip("슬래시 VFX 회전 오프셋(Euler). facing 기준 추가 회전. Y+ = 오른쪽으로 틀기.")]
    [SerializeField] private Vector3 _slashVfxRotationOffset = Vector3.zero;
    [Tooltip("슬래시 1개가 덮는다고 보는 각도(도). 타격 각도가 이보다 넓으면 여러 개로 나눠 부채꼴 배치. 작을수록 더 많이 생성.")]
    [SerializeField] private float _slashVfxCoverageAngle = 180f;
    [Tooltip("슬래시 VFX 크기 배율 (Lv1 기본 범위 기준). 레벨업으로 범위가 커지면 자동으로 같은 비율로 따라감.")]
    [SerializeField] private Vector3 _slashVfxScale = Vector3.one;

    [Header("Debug Gizmo")]
    [Tooltip("Scene 뷰에 검 공격 사거리(reach)를 빨간 원으로 표시. VFX 크기 맞춤용.")]
    [SerializeField] private bool _drawRangeGizmo = true;
    [Tooltip("Play 중 검 휘두를 때 실제 타격 콜라이더(빨간 막대)를 표시. 범위 체크용.")]
    [SerializeField] private bool _showSweepCollider = false;

    private MeshRenderer _sweepRenderer; // 디버그 시각화용 (있을 때만)
    private bool _isSweeping;
    private float _rangeScaleRatio = 1f; // 이펙트 범위 비례 추종 계수 (Lv1 BaseRange 대비 현재 FinalRange 비율)
    private float _lastSweepAngle = 180f; // Gizmo 부채꼴 각도 (마지막 공격의 FinalSweepAngle, 비Play 기본 180°)

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
        Debug.Log($"[SwordAttack] Execute mode={_hitMode} lvl={slot.Level} swingCount={stats.ProjectileCount} sweepAngle={slot.FinalSweepAngle} dmg={slot.FinalDamage:F1} dur={slot.Data.SweepDuration / stats.ProjectileSpeed:F2}s");
        AudioManager.Instance?.Play(SfxEvent.PlayerAttackSword);
        StartCoroutine(SwingSequence(slot, stats));
    }

    private IEnumerator SwingSequence(WeaponSlot slot, PlayerStats stats)
    {
        _isSweeping = true;
        int swingCount = stats.ProjectileCount;
        float sweepDuration = slot.Data.SweepDuration / stats.ProjectileSpeed;
        // 검 각도·데미지·사거리는 무기 레벨 + 등급 누적 결과 (slot에서 캐싱).
        float sweepAngle = slot.FinalSweepAngle;
        float damage = slot.FinalDamage;
        // 사거리를 데이터 기반으로 콜라이더에 반영 (검 FinalRange = 최대 도달 반경).
        ApplySweepRange(slot.FinalRange);
        // 이펙트 크기를 범위에 비례 추종시키기 위한 계수 (Lv1 BaseRange 대비 현재 범위).
        _rangeScaleRatio = slot.Data.BaseRange > 0f ? slot.FinalRange / slot.Data.BaseRange : 1f;
        _lastSweepAngle = sweepAngle; // Gizmo 부채꼴 각도 갱신 (레벨업 시 넓어짐)

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

        // 슬래시 VFX 스폰. 타격 각도(sweepAngle)가 넓으면 여러 개를 부채꼴로 분산 배치.
        if (_slashVfxPrefab != null)
        {
            float coverage = _slashVfxCoverageAngle > 1f ? _slashVfxCoverageAngle : 180f;
            int count = Mathf.Max(1, Mathf.CeilToInt(sweepAngle / coverage));
            for (int i = 0; i < count; i++)
            {
                // 각 슬래시 yaw: sweepAngle 범위를 count등분한 구간 중앙 (count=1이면 facing 중앙).
                float slashYaw = count == 1
                    ? facing
                    : facing - sweepAngle * 0.5f + sweepAngle * (i + 0.5f) / count;
                SpawnSlashVfx(slashYaw);
            }
        }

        _sweepCollider.enabled = true;
        // 디버그: 빨간 막대(콜라이더 메시) 표시 토글 — 실제 타격 범위 확인용
        if (_showSweepCollider && _sweepRenderer != null) _sweepRenderer.enabled = true;

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

    /// <summary>슬래시 VFX 1개를 지정 yaw 방향으로 스폰. 위치·회전 오프셋·범위 비례 크기 적용 후 자동 재생.</summary>
    private void SpawnSlashVfx(float yaw)
    {
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 pos = _playerTransform.position + yawRot * _slashVfxOffset;
        Quaternion rot = yawRot * Quaternion.Euler(_slashVfxRotationOffset);
        GameObject vfx = Instantiate(_slashVfxPrefab, pos, rot);
        // 실제 공격 범위에 맞춰 크기 조정 (_slashVfxScale × 범위 비례 계수).
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, _slashVfxScale * _rangeScaleRatio);
        // 프리팹 파티클이 Play On Awake=false라 명시적으로 재생해야 화면에 보인다.
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>())
            ps.Play();
        Destroy(vfx, _slashVfxLifetime);
    }

    /// <summary>
    /// 검 사거리(FinalRange = 최대 도달 반경)를 sweep 콜라이더에 반영한다.
    /// 콜라이더 박스의 월드 z 길이(bounds.size.z)가 finalRange가 되도록 localScale.z를 조정.
    /// (sweep 로직상 콜라이더 중심거리 reach = z길이의 절반, 박스 절반도 reach → 최대 도달 = z길이)
    /// </summary>
    private void ApplySweepRange(float finalRange)
    {
        if (_sweepCollider == null || finalRange <= 0f) return;
        Transform t = _sweepCollider.transform;
        var box = _sweepCollider as BoxCollider;
        float boxSizeZ = box != null ? box.size.z : 1f;
        float parentScaleZ = t.parent != null ? t.parent.lossyScale.z : 1f;
        if (boxSizeZ <= 0f || parentScaleZ <= 0f) return;

        Vector3 ls = t.localScale;
        ls.z = finalRange / (boxSizeZ * parentScaleZ);
        t.localScale = ls;
    }

    /// <summary>
    /// Scene 뷰에 실제 타격 영역(전방 부채꼴)을 표시. BurstAreaOnce 판정 범위와 동일.
    /// 반경 = 콜라이더 z 길이(=FinalRange), 각도 = 마지막 공격의 FinalSweepAngle(비Play 기본 180°).
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!_drawRangeGizmo || _sweepCollider == null) return;

        Transform center = _pivotPoint != null ? _pivotPoint
            : (_playerTransform != null ? _playerTransform : transform);

        // 타격 반경 = 콜라이더 박스의 월드 z 전체 길이(bounds.size.z = BurstArea range).
        float reach;
        var box = _sweepCollider as BoxCollider;
        if (box != null)
            reach = box.size.z * _sweepCollider.transform.lossyScale.z;
        else
            reach = _sweepCollider.bounds.size.z;

        Vector3 c = center.position;
        float facing = _playerTransform != null ? _playerTransform.eulerAngles.y : 0f;
        float half = _lastSweepAngle * 0.5f;

        // 전방 부채꼴: 좌측 변 → 호 → 우측 변
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        const int seg = 40;
        Vector3 prevPoint = c + (Quaternion.Euler(0f, facing - half, 0f) * Vector3.forward) * reach;
        Gizmos.DrawLine(c, prevPoint); // 좌측 변
        for (int i = 1; i <= seg; i++)
        {
            float a = facing - half + _lastSweepAngle * (i / (float)seg);
            Vector3 cur = c + (Quaternion.Euler(0f, a, 0f) * Vector3.forward) * reach;
            Gizmos.DrawLine(prevPoint, cur);
            prevPoint = cur;
        }
        Gizmos.DrawLine(prevPoint, c); // 우측 변
    }
}
