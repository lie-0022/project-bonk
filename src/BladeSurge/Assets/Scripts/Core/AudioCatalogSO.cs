using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SFX/BGM 클립을 이벤트로 매핑하는 카탈로그. AudioManager가 이 SO를 참조해 재생.
/// Assets/Data/Audio/AudioCatalog.asset 에 저장.
///
/// 운영:
/// - 신규 SFX 추가 시 AudioEvents.cs의 enum에 항목 추가 → 이 SO 인스펙터에서 Entry 추가 후 클립 드롭
/// - 동일 이벤트에 클립 여러 개 등록 시 재생 시 랜덤 선택 (반복 회피)
/// </summary>
[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Game/Audio Catalog")]
public class AudioCatalogSO : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public SfxEvent Event;
        [Tooltip("같은 이벤트에 여러 클립 등록 시 재생 때마다 랜덤 선택")]
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float VolumeScale = 1f;
        [Tooltip("0이면 효과 없음. 양수면 ±랜덤 피치(반음 단위 비례)")]
        [Range(0f, 0.3f)] public float PitchVariance = 0f;
    }

    [Serializable]
    public class BgmEntry
    {
        public BgmTrack Track;
        public AudioClip Clip;
        [Range(0f, 1f)] public float VolumeScale = 1f;
        public bool Loop = true;
    }

    [SerializeField] private SfxEntry[] _sfxEntries;
    [SerializeField] private BgmEntry[] _bgmEntries;

    private Dictionary<SfxEvent, SfxEntry> _sfxLookup;
    private Dictionary<BgmTrack, BgmEntry> _bgmLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    /// <summary>SFX 이벤트에 매핑된 엔트리 조회. 없으면 null.</summary>
    public SfxEntry GetSfx(SfxEvent evt)
    {
        if (_sfxLookup == null) BuildLookup();
        _sfxLookup.TryGetValue(evt, out var e);
        return e;
    }

    /// <summary>BGM 트랙에 매핑된 엔트리 조회. 없으면 null.</summary>
    public BgmEntry GetBgm(BgmTrack track)
    {
        if (_bgmLookup == null) BuildLookup();
        _bgmLookup.TryGetValue(track, out var e);
        return e;
    }

    private void BuildLookup()
    {
        _sfxLookup = new Dictionary<SfxEvent, SfxEntry>();
        if (_sfxEntries != null)
        {
            foreach (var e in _sfxEntries)
            {
                if (e == null || e.Event == SfxEvent.None) continue;
                _sfxLookup[e.Event] = e;
            }
        }

        _bgmLookup = new Dictionary<BgmTrack, BgmEntry>();
        if (_bgmEntries != null)
        {
            foreach (var e in _bgmEntries)
            {
                if (e == null || e.Track == BgmTrack.None) continue;
                _bgmLookup[e.Track] = e;
            }
        }
    }
}
