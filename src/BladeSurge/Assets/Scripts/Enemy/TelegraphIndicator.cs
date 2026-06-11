using UnityEngine;

/// <summary>
/// 보스 광역 공격 예고 표시. 바닥에 빨간 원을 펼치고 펄스 애니로 경고.
/// Quad 메시(수평) + URP/Unlit Transparent 머티리얼 사용.
/// 외부에서 Setup(position, radius, duration) 호출 후 자동 페이드아웃.
/// </summary>
public class TelegraphIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private float _pulseFreq = 6f;
    [SerializeField] private float _alphaMin = 0.4f;
    [SerializeField] private float _alphaMax = 0.85f;
    [Tooltip("표시 색 틴트. 원형(빨간색이 텍스처에 포함)은 흰색 유지. 단색 쿼드(사각 경로 예고 등)는 여기서 색 지정.")]
    [SerializeField] private Color _tint = Color.white;

    private float _duration;
    private float _elapsed;
    private MaterialPropertyBlock _mpb;
    private static readonly int Prop_BaseColor = Shader.PropertyToID("_BaseColor");

    /// <summary>radius(월드 m), duration(초) 로 표시 시작.</summary>
    public void Setup(Vector3 worldPos, float radius, float duration)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y + 0.02f, worldPos.z); // 바닥(worldPos.y는 SnapToGround로 보정됨) 살짝 위. 멀티레벨 상층 대응
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);              // Quad가 위로 보도록
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        _duration = duration;
        _elapsed = 0f;
        gameObject.SetActive(true);
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    /// <summary>
    /// 직사각형 경로 예고. origin에서 dir(수평) 방향으로 length만큼 뻗는 폭 width의 사각형을 깐다.
    /// 돌진(ChargeAttack) 등 직선 공격의 경로 표시용. duration 후 자동 소멸.
    /// </summary>
    public void SetupRect(Vector3 origin, Vector3 dir, float width, float length, float duration)
    {
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

        Vector3 center = origin + dir * (length * 0.5f);
        transform.position = new Vector3(center.x, origin.y + 0.02f, center.z); // 바닥 살짝 위
        // Quad를 눕히고(X+90) 진행 방향으로 yaw 회전 — local Y축이 경로 방향(길이)이 된다.
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        transform.localScale = new Vector3(width, length, 1f);

        _duration = duration;
        _elapsed = 0f;
        gameObject.SetActive(true);
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (_renderer == null) return;

        _elapsed += Time.deltaTime;
        float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        // 펄스: 사인파 alpha + 끝나갈수록 빨라짐
        float pulse = (Mathf.Sin(_elapsed * _pulseFreq * (1f + t)) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(_alphaMin, _alphaMax, pulse);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(Prop_BaseColor, new Color(_tint.r, _tint.g, _tint.b, alpha * _tint.a));
        _renderer.SetPropertyBlock(_mpb);

        if (_elapsed >= _duration)
        {
            gameObject.SetActive(false);
            Destroy(gameObject); // 풀 사용은 Phase C 이상에서. 일단 단순 destroy.
        }
    }
}
