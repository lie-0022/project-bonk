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

    private float _duration;
    private float _elapsed;
    private MaterialPropertyBlock _mpb;
    private static readonly int Prop_BaseColor = Shader.PropertyToID("_BaseColor");

    /// <summary>radius(월드 m), duration(초) 로 표시 시작.</summary>
    public void Setup(Vector3 worldPos, float radius, float duration)
    {
        transform.position = new Vector3(worldPos.x, 0.02f, worldPos.z); // 바닥 살짝 위
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);              // Quad가 위로 보도록
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
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
        _mpb.SetColor(Prop_BaseColor, new Color(1f, 1f, 1f, alpha));
        _renderer.SetPropertyBlock(_mpb);

        if (_elapsed >= _duration)
        {
            gameObject.SetActive(false);
            Destroy(gameObject); // 풀 사용은 Phase C 이상에서. 일단 단순 destroy.
        }
    }
}
