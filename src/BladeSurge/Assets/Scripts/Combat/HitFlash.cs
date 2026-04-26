using System.Collections;
using UnityEngine;

/// <summary>
/// 피격 시 흰색 플래시를 재생하는 컴포넌트.
/// HealthComponent의 OnDamaged/OnDeath를 구독해 자동으로 재생한다.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class HitFlash : MonoBehaviour, IHitFeedback
{
    [SerializeField] private float _flashDuration = 0.1f;

    private HealthComponent _health;
    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    private void OnEnable()
    {
        _health.OnDamaged += OnDamaged;
        _health.OnDeath   += OnDeath;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= OnDamaged;
        _health.OnDeath   -= OnDeath;
    }

    private void OnDamaged(float amount, float currentHp) => PlayHitFeedback(amount);
    private void OnDeath(float _) => PlayDeathFeedback();

    public void PlayHitFeedback(float amount)
    {
        if (_renderer == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(Color.white));
    }

    public void PlayDeathFeedback()
    {
        if (_renderer == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(Color.gray));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        _renderer.material.color = flashColor;
        yield return new WaitForSeconds(_flashDuration);
        // 원래 색으로 복구 (오브젝트가 아직 활성화 상태일 때만)
        if (_renderer != null)
            _renderer.material.color = _originalColor;
        _flashCoroutine = null;
    }
}
