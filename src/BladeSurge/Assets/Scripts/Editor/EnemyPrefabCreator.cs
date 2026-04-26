using UnityEditor;
using UnityEngine;

/// <summary>
/// 임시 적 프리팹 생성 유틸리티.
/// 메뉴: TBD > Create Enemy Prefabs
/// 이미 존재하면 덮어쓴다.
/// </summary>
public static class EnemyPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/Enemy/";
    private const string MaterialPath = "Assets/Art/Materials/";
    private const int EnemyLayer = 9;

    [MenuItem("TBD/Create Enemy Prefabs")]
    public static void CreateEnemyPrefabs()
    {
        EnsureFolders();

        Material redMat = CreateRedMaterial();

        CreateChaserPrefab(redMat);
        CreateRusherPrefab(redMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EnemyPrefabCreator] Enemy_Chaser, Enemy_Rusher 프리팹 생성 완료");
        EditorUtility.DisplayDialog("완료", "Enemy_Chaser.prefab\nEnemy_Rusher.prefab\n생성 완료", "확인");
    }

    private static void CreateChaserPrefab(Material mat)
    {
        GameObject root = new GameObject("Enemy_Chaser");
        root.tag = "Enemy";
        root.layer = EnemyLayer;

        // 시각적 바디 (캡슐)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.layer = EnemyLayer;
        body.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * 1f;

        // 물리 콜라이더 (루트에)
        var col = root.AddComponent<CapsuleCollider>();
        col.center = Vector3.up * 1f;
        col.height = 2f;
        col.radius = 0.5f;

        // Rigidbody — 회전 고정, 중력 유지
        var rb = root.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // AI + 피드백 스크립트 (HealthComponent는 RequireComponent로 자동 추가됨)
        root.AddComponent<ChaserAI>();
        root.AddComponent<HitFlash>();

        // HealthComponent 수치 설정
        var health = root.GetComponent<HealthComponent>();
        var so = new SerializedObject(health);
        so.FindProperty("_maxHp").floatValue = 30f;
        so.FindProperty("_xpReward").floatValue = 10f;
        so.ApplyModifiedProperties();

        SavePrefab(root, PrefabPath + "Enemy_Chaser.prefab");
    }

    private static void CreateRusherPrefab(Material mat)
    {
        GameObject root = new GameObject("Enemy_Rusher");
        root.tag = "Enemy";
        root.layer = EnemyLayer;

        // 시각적 바디 (큐브)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.layer = EnemyLayer;
        body.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * 0.6f;
        body.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        // 물리 콜라이더 (루트에)
        var col = root.AddComponent<BoxCollider>();
        col.center = Vector3.up * 0.6f;
        col.size = new Vector3(1.2f, 1.2f, 1.2f);

        // Rigidbody — 회전 고정, 중력 유지
        var rb = root.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // AI + 피드백 스크립트 (HealthComponent는 RequireComponent로 자동 추가됨)
        root.AddComponent<RusherAI>();
        root.AddComponent<HitFlash>();

        // HealthComponent 수치 설정
        var health = root.GetComponent<HealthComponent>();
        var so = new SerializedObject(health);
        so.FindProperty("_maxHp").floatValue = 50f;
        so.FindProperty("_xpReward").floatValue = 20f;
        so.ApplyModifiedProperties();

        SavePrefab(root, PrefabPath + "Enemy_Rusher.prefab");
    }

    private static Material CreateRedMaterial()
    {
        string path = MaterialPath + "MAT_Enemy_Temp.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.85f, 0.1f, 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemy"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemy");
    }
}
