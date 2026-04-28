using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 패시브 시스템 즉시 테스트용 임시 디버거.
/// 한 번에 한 패시브씩 검증하기 위한 단순 키 매핑.
///
/// === 키 매핑 (전체 12종, 모두 커먼 등급) ===
/// M = MoveSpeed   J = ExtraJump   H = MaxHp
/// V = Dodge       Y = HpRegen     K = AttackSpeed (발사체 속도/검 모션 포함)
/// Q = CritChance  E = CritDamage  T = Lifesteal
/// U = ProjectileCount
/// O = Luck        P = Difficulty
///
/// 회피용 키: A/W/S/D는 이동, Space=점프, Shift=대시, R=재시작, 1~9·0=WeaponDebugger
///
/// LevelupSelection UI 구축 완료 후 본 컴포넌트는 삭제할 것.
/// </summary>
public class PassiveDebugger : MonoBehaviour
{
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.mKey.wasPressedThisFrame) Bump(PassiveType.MoveSpeed);
        if (kb.jKey.wasPressedThisFrame) Bump(PassiveType.ExtraJump);
        if (kb.hKey.wasPressedThisFrame) Bump(PassiveType.MaxHp);
        if (kb.vKey.wasPressedThisFrame) Bump(PassiveType.Dodge);
        if (kb.yKey.wasPressedThisFrame) Bump(PassiveType.HpRegen);
        if (kb.kKey.wasPressedThisFrame) Bump(PassiveType.AttackSpeed);
        if (kb.qKey.wasPressedThisFrame) Bump(PassiveType.CritChance);
        if (kb.eKey.wasPressedThisFrame) Bump(PassiveType.CritDamage);
        if (kb.tKey.wasPressedThisFrame) Bump(PassiveType.Lifesteal);
        if (kb.uKey.wasPressedThisFrame) Bump(PassiveType.ProjectileCount);
        if (kb.oKey.wasPressedThisFrame) Bump(PassiveType.Luck);
        if (kb.pKey.wasPressedThisFrame) Bump(PassiveType.Difficulty);
    }

    private void Bump(PassiveType type)
    {
        var stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogWarning("[PassiveDebugger] PlayerStats null"); return; }

        bool ok = stats.IncrementPassive(type, CardGrade.Common);
        int picks = stats.GetPassiveLevel(type);
        float effLv = stats.GetEffectiveLevel(type);

        if (!ok)
        {
            Debug.Log($"[PassiveDebugger] {type} 최대 (15회) 도달");
            return;
        }

        Debug.Log($"[PassiveDebugger] {type} +1 | picks={picks} effLv={effLv:F2} | {Snapshot(type, stats)}");
    }

    private static string Snapshot(PassiveType type, PlayerStats s) => type switch
    {
        PassiveType.MoveSpeed   => $"MoveSpeed={s.MoveSpeed:F2}",
        PassiveType.ExtraJump   => $"ExtraJumps={s.ExtraJumps}",
        PassiveType.MaxHp       => $"MaxHp={s.MaxHp:F1}",
        PassiveType.Dodge       => $"Dodge={s.DodgeChance:P1}",
        PassiveType.HpRegen     => $"HpRegen={s.HpRegen:F2}/s",
        PassiveType.AttackSpeed     => $"AtkSpdMult={s.AttackSpeedMultiplier:F3} ProjSpd×{s.ProjectileSpeed:F2}",
        PassiveType.CritChance      => $"Crit={s.CritChance:P1}",
        PassiveType.CritDamage      => $"CritMult×{s.CritMultiplier:F2}",
        PassiveType.Lifesteal       => $"Lifesteal={s.Lifesteal:P1}",
        PassiveType.ProjectileCount => $"ProjCount={s.ProjectileCount}",
        PassiveType.Luck            => $"Luck={s.LuckChance:P1}",
        PassiveType.Difficulty      => $"DiffSpawn×{s.DifficultySpawnMultiplier:F2} Reward×{s.DifficultyRewardMultiplier:F2}",
        _                           => "?",
    };
}
