#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 어떤 씬에 있어도 Play ▶ 누르면 MainMenu 씬(처음)에서 시작하도록 강제.
/// Edit > Project Settings > Editor > Play Mode Start Scene 을 코드로 설정.
///
/// 끄려면: 메뉴 BladeSurge > Play Mode Start Scene > Disable
/// 다시 켜려면: BladeSurge > Play Mode Start Scene > Enable (MainMenu)
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartScene
{
    private const string StartScenePath = "Assets/Scenes/MainMenu.unity";
    private const string MenuEnable  = "BladeSurge/Play Mode Start Scene/Enable (MainMenu)";
    private const string MenuDisable = "BladeSurge/Play Mode Start Scene/Disable";
    private const string PrefKey = "BladeSurge.PlayModeStartScene.Enabled";

    static PlayModeStartScene()
    {
        // 도메인 리로드 시 마지막 설정 복원
        if (EditorPrefs.GetBool(PrefKey, true))
            ApplyStartScene();
    }

    [MenuItem(MenuEnable)]
    private static void Enable()
    {
        EditorPrefs.SetBool(PrefKey, true);
        ApplyStartScene();
        Debug.Log($"[PlayModeStartScene] 활성화 — Play 누르면 항상 {StartScenePath}에서 시작");
    }

    [MenuItem(MenuDisable)]
    private static void Disable()
    {
        EditorPrefs.SetBool(PrefKey, false);
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[PlayModeStartScene] 비활성화 — 현재 열린 씬에서 시작");
    }

    [MenuItem(MenuEnable, true)]
    private static bool ValidateEnable() =>
        !EditorPrefs.GetBool(PrefKey, true);

    [MenuItem(MenuDisable, true)]
    private static bool ValidateDisable() =>
        EditorPrefs.GetBool(PrefKey, true);

    private static void ApplyStartScene()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        if (sceneAsset == null)
        {
            Debug.LogWarning($"[PlayModeStartScene] {StartScenePath} 씬을 찾을 수 없음");
            return;
        }
        EditorSceneManager.playModeStartScene = sceneAsset;
    }
}
#endif
