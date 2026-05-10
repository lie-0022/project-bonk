using UnityEngine;

/// <summary>
/// 플레이어 Animator 파라미터(IsMoving)를 매 프레임 갱신.
/// CharacterController.velocity 의 수평 성분으로 이동 여부 판정.
///
/// 무기 변경 시 PlayerWeaponVisual에서 자식 Visual GameObject가 swap되고
/// 그쪽 Animator는 무기별 Override Controller를 사용한다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private float _movingThreshold = 0.1f;

    private CharacterController _cc;
    private static readonly int Param_IsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        Vector3 v = _cc.velocity;
        v.y = 0f;
        bool moving = v.sqrMagnitude > _movingThreshold * _movingThreshold;

        // 자식의 모든 Animator(현재 활성 무기 Visual)에게 전달
        var animators = GetComponentsInChildren<Animator>(false);
        for (int i = 0; i < animators.Length; i++)
            animators[i].SetBool(Param_IsMoving, moving);
    }
}
