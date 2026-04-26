// PROTOTYPE - NOT FOR PRODUCTION
// Question: 아이소메트릭 카메라 Lerp 추적이 뱀서라이크 가독성을 유지하면서 자연스럽게 동작하는가?
// Date: 2026-03-26

using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // 플레이어 Transform 드래그

    [Header("Isometric Settings")]
    public Vector3 offset = new Vector3(0f, 10f, -10f); // Y+10, Z-10 (45도 앵글)
    public float smoothSpeed = 5f; // 1=느림, 15=즉시

    private void Start()
    {
        // 시작 시 즉시 스냅 (Lerp 없이)
        if (target != null)
            transform.position = target.position + offset;

        // 카메라 앵글 강제 설정: 아이소메트릭 45도
        transform.rotation = Quaternion.Euler(45f, 45f, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // 앵글 고정 (회전 방지)
        transform.rotation = Quaternion.Euler(45f, 45f, 0f);
    }

    // 재시작 시 외부에서 호출
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
