using UnityEngine;

/// <summary>
/// 옵션값 영속화 래퍼. PlayerPrefs를 단일 진입점으로 추상화한다.
/// 카메라 감도, 오디오 음량 등 옵션 메뉴에서 변경하는 값은 모두 이 클래스를 거친다.
/// 키 충돌 방지를 위해 PrefsKey prefix를 사용한다.
/// </summary>
public static class SettingsService
{
    private const string PrefsKey = "BladeSurge.";

    // ── 키 정의 ─────────────────────────
    private const string KeyMouseSensitivity = PrefsKey + "MouseSensitivity";
    private const string KeyBgmVolume        = PrefsKey + "BgmVolume";
    private const string KeySfxVolume        = PrefsKey + "SfxVolume";
    private const string KeyUiVolume         = PrefsKey + "UiVolume";
    private const string KeyMasterVolume     = PrefsKey + "MasterVolume";

    // ── 기본값 ──────────────────────────
    public const float DefaultMouseSensitivity = 0.15f;
    public const float DefaultVolume           = 0.8f;

    // ── 카메라 감도 ─────────────────────
    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        set
        {
            PlayerPrefs.SetFloat(KeyMouseSensitivity, Mathf.Clamp(value, 0.01f, 1.0f));
            PlayerPrefs.Save();
        }
    }

    // ── 음량 (0~1, 선형) ────────────────
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyMasterVolume, DefaultVolume);
        set { PlayerPrefs.SetFloat(KeyMasterVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(KeyBgmVolume, DefaultVolume);
        set { PlayerPrefs.SetFloat(KeyBgmVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(KeySfxVolume, DefaultVolume);
        set { PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float UiVolume
    {
        get => PlayerPrefs.GetFloat(KeyUiVolume, DefaultVolume);
        set { PlayerPrefs.SetFloat(KeyUiVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    /// <summary>전체 옵션값 초기화 (디버그용).</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyMouseSensitivity);
        PlayerPrefs.DeleteKey(KeyMasterVolume);
        PlayerPrefs.DeleteKey(KeyBgmVolume);
        PlayerPrefs.DeleteKey(KeySfxVolume);
        PlayerPrefs.DeleteKey(KeyUiVolume);
        PlayerPrefs.Save();
    }
}
