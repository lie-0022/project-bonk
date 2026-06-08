using UnityEngine;

/// <summary>
/// 임팩트 순간 시각 효과 — 짧은 폭발 같은 구체.
/// Setup(worldPos, radius)로 호출하면 0→2*radius로 빠르게 커지면서 페이드 아웃.
/// </summary>
public class ImpactBurst : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private float _duration = 0.45f;
    [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _alphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));
    [SerializeField] private Color _baseColor = new Color(1f, 0.45f, 0.1f, 1f);

    private float _elapsed;
    private float _maxScale;
    private MaterialPropertyBlock _mpb;
    private static readonly int Prop_BaseColor = Shader.PropertyToID("_BaseColor");

    public void Setup(Vector3 worldPos, float radius)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y + 0.5f, worldPos.z); // worldPos.y(SnapToGround 보정) 기준으로 띄움. 멀티레벨 상층 대응
        transform.rotation = Quaternion.identity;
        _maxScale = radius * 2f;
        transform.localScale = Vector3.zero;
        _elapsed = 0f;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_renderer == null) return;
        _elapsed += Time.deltaTime;
        float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        float s = _scaleCurve.Evaluate(t) * _maxScale;
        transform.localScale = new Vector3(s, s, s);

        float a = _alphaCurve.Evaluate(t);
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(Prop_BaseColor, new Color(_baseColor.r, _baseColor.g, _baseColor.b, a));
        _renderer.SetPropertyBlock(_mpb);

        if (_elapsed >= _duration) Destroy(gameObject);
    }
}
