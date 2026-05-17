using UnityEngine;

/// <summary>
/// BGM/SFX/UI 오디오 재생을 중앙화한 싱글턴.
/// AudioSource 3채널 (BGM 루프, SFX 일회성, UI 일회성)을 보유하며,
/// SettingsService의 음량 설정을 자동 적용한다.
///
/// 사용:
/// - SFX: AudioManager.Instance.Play(SfxEvent.PlayerHit)
/// - BGM: AudioManager.Instance.PlayBgm(BgmTrack.GameplayStage1)
/// - AudioCatalogSO 인스펙터에 클립 매핑 필요.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private AudioCatalogSO _catalog;

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
        DontDestroyOnLoad(gameObject);

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

    // ---------- SFX ----------

    /// <summary>SFX 이벤트 재생. 카탈로그에 매핑 없으면 무시.</summary>
    public void Play(SfxEvent evt)
    {
        if (_catalog == null || _sfxSource == null) return;
        var entry = _catalog.GetSfx(evt);
        if (entry == null) return;
        var clip = PickClip(entry.Clips);
        if (clip == null) return;

        if (entry.PitchVariance > 0f)
        {
            float prev = _sfxSource.pitch;
            _sfxSource.pitch = 1f + Random.Range(-entry.PitchVariance, entry.PitchVariance);
            _sfxSource.PlayOneShot(clip, entry.VolumeScale);
            _sfxSource.pitch = prev;
        }
        else
        {
            _sfxSource.PlayOneShot(clip, entry.VolumeScale);
        }
    }

    /// <summary>UI 효과음 (버튼 hover/click, 카드 등장 등). SFX와 동일 카탈로그 사용.</summary>
    public void PlayUi(SfxEvent evt)
    {
        if (_catalog == null || _uiSource == null) return;
        var entry = _catalog.GetSfx(evt);
        if (entry == null) return;
        var clip = PickClip(entry.Clips);
        if (clip == null) return;
        _uiSource.PlayOneShot(clip, entry.VolumeScale);
    }

    /// <summary>직접 AudioClip 재생 (카탈로그 우회). 외부 시스템 호환용.</summary>
    public void PlaySfxDirect(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    // ---------- BGM ----------

    /// <summary>BGM 단일 트랙 재생. 동일 트랙이면 무시. 플레이리스트 모드 해제.</summary>
    public void PlayBgm(BgmTrack track)
    {
        if (_catalog == null || _bgmSource == null) return;
        var entry = _catalog.GetBgm(track);
        if (entry == null || entry.Clip == null) return;
        if (_bgmSource.clip == entry.Clip && _bgmSource.isPlaying && _playlistTracks == null) return;

        _playlistTracks = null;
        _playlistIndex = 0;
        _bgmSource.clip = entry.Clip;
        _bgmSource.loop = entry.Loop;
        _bgmSource.volume = SettingsService.MasterVolume * SettingsService.BgmVolume * _bgmBaseVolume * entry.VolumeScale;
        _bgmSource.Play();
    }

    public void StopBgm()
    {
        if (_bgmSource != null) _bgmSource.Stop();
        _playlistTracks = null;
        _playlistIndex = 0;
    }

    /// <summary>BGM 트랙의 AudioClip을 미리 메모리로 로드해 첫 재생 시 hitch를 방지.</summary>
    public void PreloadBgm(BgmTrack track)
    {
        if (_catalog == null) return;
        var entry = _catalog.GetBgm(track);
        if (entry == null || entry.Clip == null) return;
        if (entry.Clip.loadState != AudioDataLoadState.Loaded)
            entry.Clip.LoadAudioData();
    }

    private BgmTrack[] _playlistTracks;
    private int _playlistIndex;

    /// <summary>여러 트랙을 순차 재생하고 끝까지 가면 처음 트랙으로 루프한다.</summary>
    public void PlayBgmPlaylist(params BgmTrack[] tracks)
    {
        if (tracks == null || tracks.Length == 0) return;
        _playlistTracks = tracks;
        _playlistIndex = 0;
        PlayPlaylistEntry();
    }

    private void PlayPlaylistEntry()
    {
        if (_playlistTracks == null || _playlistTracks.Length == 0) return;
        if (_catalog == null || _bgmSource == null) return;

        var entry = _catalog.GetBgm(_playlistTracks[_playlistIndex]);
        if (entry == null || entry.Clip == null) return;

        _bgmSource.clip = entry.Clip;
        _bgmSource.loop = false; // 플레이리스트는 개별 클립 loop 끔
        _bgmSource.volume = SettingsService.MasterVolume * SettingsService.BgmVolume * _bgmBaseVolume * entry.VolumeScale;
        _bgmSource.Play();
    }

    private void Update()
    {
        if (_playlistTracks == null || _bgmSource == null) return;
        if (_bgmSource.clip == null || _bgmSource.isPlaying) return;

        _playlistIndex = (_playlistIndex + 1) % _playlistTracks.Length;
        PlayPlaylistEntry();
    }

    // ---------- Internal ----------

    private static AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        return clips[Random.Range(0, clips.Length)];
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
