using UnityEditor;
using UnityEngine;

/// <summary>
/// 픽업 프리팹 생성 유틸리티.
/// 메뉴: TBD > Create Pickup Prefabs
/// </summary>
public static class PickupPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/Pickup/";
    private const string MaterialPath = "Assets/Art/Materials/";
    private const int PickupLayer = 12;

    [MenuItem("TBD/Create Pickup Prefabs")]
    public static void CreatePickupPrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Pickup"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Pickup");

        CreateXPOrbPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", "Pickup_XPOrb.prefab 생성 완료", "확인");
    }

    private static void CreateXPOrbPrefab()
    {
        GameObject root = new GameObject("Pickup_XPOrb");
        root.tag = "Pickup";
        root.layer = PickupLayer;

        // 파란 구체
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.layer = PickupLayer;
        body.transform.SetParent(root.transform);
        body.transform.localScale = Vector3.one * 0.3f;
        body.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(body.GetComponent<SphereCollider>());

        // 파란 머티리얼
        body.GetComponent<Renderer>().material = GetOrCreateBlueMaterial();

        // XPOrb 스크립트
        root.AddComponent<XPOrb>();

        string path = PrefabPath + "Pickup_XPOrb.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log("[PickupPrefabCreator] Pickup_XPOrb.prefab 생성 완료");
    }

    private static Material GetOrCreateBlueMaterial()
    {
        string path = MaterialPath + "MAT_XPOrb.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.2f, 0.5f, 1.0f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
