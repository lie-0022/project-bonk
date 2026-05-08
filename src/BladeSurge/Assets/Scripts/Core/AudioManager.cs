using UnityEngine;

/// <summary>
/// BGM/SFX/UI 오디오 재생을 중앙화한 싱글턴.
/// AudioSource 3채널 (BGM 루프, SFX 일회성, UI 일회성)을 보유하며,
/// SettingsService의 음량 설정을 자동 적용한다.
///
/// 실제 AudioClip 자산은 외부 도착 후 BGM/SFX 카탈로그(ScriptableObject 또는 Addressables)
/// 로 연결할 예정. 현재는 골격만.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (자식에 자동 생성)")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _uiSource;

    [Header("기본 볼륨 (SettingsService 값과 곱해짐)")]
    [SerializeField, Range(0f, 1f)] private float _bgmBaseVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _sfxBaseVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _uiBaseVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureSources();
        ApplyVolumes();
    }

    /// <summary>SettingsService 변경 시 호출해 즉시 음량 반영.</summary>
    public void ApplyVolumes()
    {
        float master = SettingsService.MasterVolume;
        if (_bgmSource != null) _bgmSource.volume = master * SettingsService.BgmVolume * _bgmBaseVolume;
        if (_sfxSource != null) _sfxSource.volume = master * SettingsService.SfxVolume * _sfxBaseVolume;
        if (_uiSource != null)  _uiSource.volume  = master * SettingsService.UiVolume  * _uiBaseVolume;
    }

    /// <summary>BGM을 루프 재생한다. 동일 클립이면 무시.</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBgm()
    {
        if (_bgmSource != null) _bgmSource.Stop();
    }

    /// <summary>일회성 SFX (월드 효과음 — 공격, 피격, 사망 등).</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>UI 효과음 (버튼 hover/click, 카드 등장 등).</summary>
    public void PlayUi(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _uiSource == null) return;
        _uiSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void EnsureSources()
    {
        if (_bgmSource == null) _bgmSource = CreateChildSource("BGM_Source", true);
        if (_sfxSource == null) _sfxSource = CreateChildSource("SFX_Source", false);
        if (_uiSource == null)  _uiSource  = CreateChildSource("UI_Source", false);
    }

    private AudioSource CreateChildSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f; // 2D
        return src;
    }
}
