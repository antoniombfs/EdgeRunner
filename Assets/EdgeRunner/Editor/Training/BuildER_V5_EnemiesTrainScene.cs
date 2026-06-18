using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_EnemiesTrainScene
{
    private const string ScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_Train.unity";
    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5_Enemies.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidEnemyPrefabPath = "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";

    [MenuItem("EdgeRunner/Training/Build ER_V5_Enemies_Train")]
    public static void BuildFromMenu()
    {
        BuildScene();
    }

    public static void BuildSceneFromCommandLine()
    {
        BuildScene();
    }

    private static void BuildScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_Train_EnemyIntro01");
        GameObject player = CreatePlayer();
        GameObject goal = CreateGoal();

        ConfigurePlayer(player, goal.transform);
        CreateCamera(player.transform);
        CreateLevel(root.transform, sharedSprite);
        CreateEnemyIntro01(root.transform, sharedSprite);
        CreateDeathZone();

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware training scene: {ScenePath}");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes", "Training");
        EnsureFolder("Assets/EdgeRunner", "Editor");
        EnsureFolder("Assets/EdgeRunner/Editor", "Training");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Sprite GetSharedSprite()
    {
        GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);

        if (groundPrefab != null)
        {
            SpriteRenderer renderer = groundPrefab.GetComponent<SpriteRenderer>();

            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static GameObject CreatePlayer()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

        if (playerPrefab == null)
        {
            throw new System.InvalidOperationException($"Missing Player_V5_Enemies prefab at {PlayerPrefabPath}");
        }

        GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;

        if (player == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate Player_V5_Enemies prefab.");
        }

        player.name = "Player_V5_Enemies";
        player.transform.position = new Vector3(-10f, 1.15f, 0f);
        return player;
    }

    private static void ConfigurePlayer(GameObject player, Transform goal)
    {
        EdgeRunnerAgentV5EnemyAware agent = player.GetComponent<EdgeRunnerAgentV5EnemyAware>();

        if (agent == null)
        {
            throw new System.InvalidOperationException("Player_V5_Enemies prefab does not contain EdgeRunnerAgentV5EnemyAware.");
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SetObjectReference(agent, "rb", rb);
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetObjectReference(agent, "mixedLevelGenerator", null);
        SetObjectReference(agent, "evaluationManager", null);

        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();

        if (behavior != null)
        {
            behavior.BehaviorName = "EdgeRunnerV5Enemies";
            behavior.BehaviorType = BehaviorType.Default;
            ConfigureBehaviorBrain(behavior);
        }
    }

    private static void ConfigureBehaviorBrain(BehaviorParameters behavior)
    {
        SerializedObject serializedObject = new SerializedObject(behavior);
        SerializedProperty vectorObservationSize = serializedObject.FindProperty("m_BrainParameters.VectorObservationSize");

        if (vectorObservationSize != null)
        {
            vectorObservationSize.intValue = EdgeRunnerAgentV5EnemyAware.DefaultExpectedObservationSize;
        }

        SerializedProperty branchSizes = serializedObject.FindProperty("m_BrainParameters.m_ActionSpec.BranchSizes");

        if (branchSizes != null)
        {
            int[] expectedBranches = { 3, 2, 2 };
            branchSizes.arraySize = expectedBranches.Length;

            for (int i = 0; i < expectedBranches.Length; i++)
            {
                branchSizes.GetArrayElementAtIndex(i).intValue = expectedBranches[i];
            }
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(18f, 5.4f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.0f;
        camera.backgroundColor = new Color(0.055f, 0.08f, 0.13f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.AddComponent<AudioListener>();

        DemoCameraFollow2D follow = cameraObject.AddComponent<DemoCameraFollow2D>();
        follow.SetTarget(target);
    }

    private static void CreateLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("EnemyIntro01_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StartPlatform_Wide", -4f, 0f, new Vector2(16f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "Landing_01_Wide", 15.5f, 0f, new Vector2(16f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "EnemyIntro_Platform_Main", 38f, 0f, new Vector2(22f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "GoalApproach_Wide", 64.5f, 0f, new Vector2(24f, 0.4f), sprite);
    }

    private static GameObject CreatePlatformWithTop(
        Transform parent,
        string name,
        float centerX,
        float topY,
        Vector2 size,
        Sprite sprite)
    {
        float centerY = topY - size.y * 0.5f;

        GameObject platform = new GameObject(name);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, centerY, 0f);
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.18f, 0.27f, 0.32f, 1f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        return platform;
    }

    private static void CreateEnemyIntro01(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("EnemyIntro01_RealAndroid");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        GameObject enemy = enemyPrefab != null
            ? PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject
            : CreateFallbackEnemy(fallbackSprite);

        if (enemy == null)
        {
            return;
        }

        enemy.name = "Android_EnemyAware_Intro01";
        enemy.transform.SetParent(enemyRoot.transform, false);
        enemy.transform.position = new Vector3(38f, 1.02f, 0f);

        Collider2D collider = enemy.GetComponent<Collider2D>();

        if (collider != null)
        {
            collider.isTrigger = true;
        }

        DemoAndroidPatrol patrol = enemy.GetComponent<DemoAndroidPatrol>();

        if (patrol != null)
        {
            patrol.Configure(0.8f, 2.2f);
        }

        DemoEnemyHazard demoHazard = enemy.GetComponent<DemoEnemyHazard>();

        if (demoHazard != null)
        {
            demoHazard.SetAffectsAgent(true);
        }

        EdgeRunnerEnemyMarker marker = enemy.GetComponent<EdgeRunnerEnemyMarker>();

        if (marker == null)
        {
            marker = enemy.AddComponent<EdgeRunnerEnemyMarker>();
        }

        marker.SetAffectsAgent(true);
    }

    private static GameObject CreateFallbackEnemy(Sprite sprite)
    {
        GameObject enemy = new GameObject("Android_EnemyAware_Fallback");
        enemy.transform.localScale = new Vector3(0.9f, 1.25f, 1f);

        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.55f, 0.62f, 0.68f, 1f);
        renderer.sortingOrder = 6;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        enemy.AddComponent<DemoAndroidPatrol>();
        enemy.AddComponent<EdgeRunnerEnemyMarker>();
        return enemy;
    }

    private static GameObject CreateGoal()
    {
        GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal = goalPrefab != null
            ? PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject
            : new GameObject("Goal");

        if (goal == null)
        {
            throw new System.InvalidOperationException("Failed to create Goal.");
        }

        goal.name = "Goal";
        goal.transform.position = new Vector3(72f, 1.1f, 0f);
        goal.transform.localScale = new Vector3(1.2f, 2.4f, 1f);

        BoxCollider2D collider = goal.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider = goal.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;

        if (goal.GetComponent<EnemyAwareGoalTrigger>() == null)
        {
            goal.AddComponent<EnemyAwareGoalTrigger>();
        }

        return goal;
    }

    private static void CreateDeathZone()
    {
        GameObject deathZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefabPath);
        GameObject deathZone = deathZonePrefab != null
            ? PrefabUtility.InstantiatePrefab(deathZonePrefab) as GameObject
            : new GameObject("DeathZone");

        if (deathZone == null)
        {
            return;
        }

        deathZone.name = "DeathZone_EnemyAware";
        deathZone.transform.position = new Vector3(32f, -7f, 0f);
        deathZone.transform.localScale = new Vector3(110f, 1f, 1f);

        BoxCollider2D collider = deathZone.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider = deathZone.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;

        DeathZone gameplayDeathZone = deathZone.GetComponent<DeathZone>();

        if (gameplayDeathZone != null)
        {
            gameplayDeathZone.enabled = false;
        }

        if (deathZone.GetComponent<EnemyAwareDeathZone>() == null)
        {
            deathZone.AddComponent<EnemyAwareDeathZone>();
        }
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
