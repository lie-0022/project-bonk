using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Play 검증용 임시 디버그 컴포넌트.
/// WASD(이동) / Space(점프) / Shift(대시) 등과 충돌하지 않는 키만 사용.
///
/// === 키 매핑 ===
/// 1/2/3       → 현재 등급 모드로 Sword/Gun/Magic 추가/강화
/// 4           → 등급 모드 = AutoRoll (행운 반영 자동 추첨, 기본값)
/// 5           → 등급 모드 = Common 강제
/// 6           → 등급 모드 = Epic 강제
/// 7           → 등급 모드 = Unique 강제
/// 8           → 등급 모드 = Legend 강제
/// 9           → 현재 등급 모드 콘솔에 출력 (확인용)
/// 0           → 행운 패시브 +1 레벨 (등급 가중치 확인용)
///
/// 등급 모드는 sticky: 한 번 누르면 다시 바꿀 때까지 유지.
/// LevelupSelection UI 구현 후 본 컴포넌트와 파일을 통째로 삭제할 것.
/// </summary>
public class WeaponDebugger : MonoBehaviour
{
    /// <summary>등급 강제 모드. null = 자동 추첨.</summary>
    private CardGrade? _forcedGrade;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 무기 선택 UI가 열려 있으면 1/2/3 키는 UI가 소비 — 디버그 입력 무시
        if (LevelupWeaponSelection.Instance != null && LevelupWeaponSelection.Instance.IsSelecting)
            return;

        if (Keyboard.current.digit4Key.wasPressedThisFrame) SetMode(null);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SetMode(CardGrade.Common);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SetMode(CardGrade.Epic);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) SetMode(CardGrade.Unique);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) SetMode(CardGrade.Legend);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) PrintCurrentMode();

        if (Keyboard.current.digit1Key.wasPressedThisFrame) TryAdd(WeaponType.Sword);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TryAdd(WeaponType.Gun);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TryAdd(WeaponType.Magic);

        if (Keyboard.current.digit0Key.wasPressedThisFrame) BumpLuck();
    }

    private void PrintCurrentMode()
    {
        string label = _forcedGrade.HasValue ? _forcedGrade.Value.ToString() : "AutoRoll";
        Debug.Log($"[WeaponDebugger] Current grade mode = {label}");
    }

    private void SetMode(CardGrade? grade)
    {
        _forcedGrade = grade;
        string label = grade.HasValue ? grade.Value.ToString() : "AutoRoll";
        Debug.Log($"[WeaponDebugger] Grade mode = {label}");
    }

    private void TryAdd(WeaponType type)
    {
        var ws = WeaponSystem.Instance;
        if (ws == null)
        {
            Debug.LogWarning("[WeaponDebugger] WeaponSystem.Instance is null.");
            return;
        }

        CardGrade grade = _forcedGrade ?? CardGradeRoller.Roll();
        bool added = ws.AddWeapon(type, grade);
        int level = ws.GetWeaponLevel(type);
        string mode = _forcedGrade.HasValue ? "forced" : "rolled";
        Debug.Log($"[WeaponDebugger] {type} grade={grade} ({mode}) → added/upgraded={added}, level={level}");
    }

    private void BumpLuck()
    {
        var stats = PlayerStats.Instance;
        if (stats == null)
        {
            Debug.LogWarning("[WeaponDebugger] PlayerStats.Instance is null.");
            return;
        }

        bool added = stats.IncrementPassive(PassiveType.Luck);
        int lv = stats.GetPassiveLevel(PassiveType.Luck);
        Debug.Log($"[WeaponDebugger] Luck +1 → lv={lv} chance={stats.LuckChance:P1} | {CardGradeRoller.DebugProbabilities(stats.LuckChance)}");
    }
}
