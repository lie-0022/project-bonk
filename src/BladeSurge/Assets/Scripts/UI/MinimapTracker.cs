using UnityEngine;

/// <summary>
/// 미니맵에 표시될 오브젝트가 부착하는 컴포넌트.
/// OnEnable/OnDisable 라이프사이클로 MinimapManager에 자동 등록/해제.
/// </summary>
public class MinimapTracker : MonoBehaviour
{
    public enum MarkerType
    {
        Player,
        Enemy,
        Boss,
        Chest,
        Coin,
    }

    [SerializeField] private MarkerType _type = MarkerType.Enemy;
    public MarkerType Type => _type;

    private void OnEnable() { MinimapManager.Register(this); }
    private void OnDisable() { MinimapManager.Unregister(this); }
}
