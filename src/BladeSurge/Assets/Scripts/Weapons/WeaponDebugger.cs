using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Play 검증용 임시 디버그 컴포넌트.
/// 1/2/3 키로 Sword/Gun/Magic 무기를 추가·강화한다.
/// LevelupSelection UI 구현 후 본 컴포넌트와 파일을 통째로 삭제할 것.
/// </summary>
public class WeaponDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            TryAdd(WeaponType.Sword);
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            TryAdd(WeaponType.Gun);
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            TryAdd(WeaponType.Magic);
    }

    private void TryAdd(WeaponType type)
    {
        var ws = WeaponSystem.Instance;
        if (ws == null)
        {
            Debug.LogWarning("[WeaponDebugger] WeaponSystem.Instance is null.");
            return;
        }

        bool added = ws.AddWeapon(type);
        int level = ws.GetWeaponLevel(type);
        Debug.Log($"[WeaponDebugger] {type} → added/upgraded={added}, level={level}");
    }
}
