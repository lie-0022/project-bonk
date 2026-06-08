using UnityEngine;

/// <summary>
/// 보스 발 밑 (또는 플레이어 위치)에 큰 단일 원 AOE.
/// MVP: 플레이어 현재 위치 노리기. 회피 가능.
/// </summary>
public class GroundSlamAttack : BossAttackBase
{
    [Tooltip("true=플레이어 위치 노림(난이도↑), false=보스 자기 위치(접근 막기)")]
    [SerializeField] private bool _targetPlayer = true;

    protected override System.Collections.IEnumerator FireRoutine(BossEnemy boss, Transform player)
    {
        // y는 그대로 두고 TelegraphAndImpact 내부 SnapToGround가 바닥 높이로 보정한다(멀티레벨 대응).
        Vector3 origin = _targetPlayer && player != null ? player.position : transform.position;
        yield return TelegraphAndImpact(origin, _radius, boss);
    }
}
