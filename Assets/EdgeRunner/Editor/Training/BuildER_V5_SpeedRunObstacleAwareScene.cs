using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_SpeedRunObstacleAwareScene
{
    private const string PlayerPrefabPath =
        "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidPrefabPath =
        "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";

    private const string TraversalBaseScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunOA_TraversalBase.unity";
    private const string StaticAndroidScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunOA_StaticAndroid.unity";

    private const string BehaviorName =
        EdgeRunnerAgentV5SpeedRunObstacleAware.ExpectedBehaviorName;

    [MenuItem("EdgeRunner/Training/SpeedRunObstacleAware/Build TraversalBase")]
    public static void BuildTraversalBaseFromMenu()
    {
        if (CanReplaceOpenScenes())
        {
            BuildTraversalBaseScene();
        }
    }

    [MenuItem("EdgeRunner/Training/SpeedRunObstacleAware/Build StaticAndroid")]
    public static void BuildStaticAndroidFromMenu()
    {
        if (CanReplaceOpenScenes())
        {
            BuildStaticAndroidScene();
        }
    }

    [MenuItem("EdgeRunner/Training/SpeedRunObstacleAware/Build Both")]
    public static void BuildBothFromMenu()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        BuildTraversalBaseScene();
        BuildStaticAndroidScene();
    }

    public static void BuildTraversalBaseFromCommandLine()
    {
        BuildTraversalBaseScene();
    }

    public static void BuildStaticAndroidFromCommandLine()
    {
        BuildStaticAndroidScene();
    }

    public static void BuildBothFromCommandLine()
    {
        BuildTraversalBaseScene();
        BuildStaticAndroidScene();
    }

    private static void BuildTraversalBaseScene()
    {
        EnsureTrainingSceneFolder();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite sprite = GetSharedSprite();
        GameObject root = new GameObject("ER_V5_SpeedRunOA_TraversalBase");

        CreatePlatform(root.transform, "Traversal_Start", 4f, 0f, 14f, sprite);
        CreatePlatform(root.transform, "Traversal_Middle", 17.6f, 0.35f, 10f, sprite);
        CreatePlatform(root.transform, "Traversal_GoalPlatform", 32.6f, 0f, 16f, sprite);

        GameObject goal = CreateGoal(new Vector3(38f, 1.1f, 0f));
        GameObject player = CreatePlayer(
            new Vector3(0f, 1.15f, 0f),
            goal.transform,
            allowFlatObstacleJumps: false,
            maxEpisodeSeconds: 60f);

        CreateDeathZone(19f, 55f, "DeathZone_SpeedRunOA_TraversalBase");
        CreateCamera(player.transform);
        ValidateScene(scene, player, goal, null, "TraversalBase");
        SaveScene(scene, TraversalBaseScenePath, player, "TraversalBase");
    }

    private static void BuildStaticAndroidScene()
    {
        EnsureTrainingSceneFolder();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite sprite = GetSharedSprite();
        GameObject root = new GameObject("ER_V5_SpeedRunOA_StaticAndroid");

        // One uninterrupted wide platform keeps the Android lesson separate from gap timing.
        CreatePlatform(root.transform, "StaticAndroid_WidePlatform", 17f, 0f, 40f, sprite);

        GameObject goal = CreateGoal(new Vector3(33f, 1.1f, 0f));
        GameObject android = CreateStaticAndroid(
            root.transform,
            "SpeedRunOA_StaticAndroid_01",
            new Vector3(12f, 1.02f, 0f),
            sprite);
        GameObject player = CreatePlayer(
            new Vector3(0f, 1.15f, 0f),
            goal.transform,
            allowFlatObstacleJumps: true,
            maxEpisodeSeconds: 40f);

        CreateDeathZone(17f, 50f, "DeathZone_SpeedRunOA_StaticAndroid");
        CreateCamera(player.transform);
        ValidateScene(scene, player, goal, android, "StaticAndroid");
        SaveScene(scene, StaticAndroidScenePath, player, "StaticAndroid");
    }

    private static GameObject CreatePlayer(
        Vector3 position,
        Transform goal,
        bool allowFlatObstacleJumps,
        float maxEpisodeSeconds)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException("Player_V5 prefab was not found.", PlayerPrefabPath);
        }

        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (player == null)
        {
            throw new System.InvalidOperationException("Player_V5 could not be instantiated.");
        }

        player.name = "Player_V5_SpeedRunObstacleAware";
        player.transform.position = position;

        EdgeRunnerAgentV5 baseAgent = player.GetComponent<EdgeRunnerAgentV5>();
        if (baseAgent == null)
        {
            throw new System.InvalidOperationException("Player_V5 has no EdgeRunnerAgentV5 component.");
        }

        string serializedBaseAgent = JsonUtility.ToJson(baseAgent);
        SerializedObject serializedAgent = new SerializedObject(baseAgent);
        Transform inheritedGroundCheck =
            serializedAgent.FindProperty("groundCheck")?.objectReferenceValue as Transform;

        EdgeRunnerAgentV5SpeedRunObstacleAware agent =
            player.AddComponent<EdgeRunnerAgentV5SpeedRunObstacleAware>();
        JsonUtility.FromJsonOverwrite(serializedBaseAgent, agent);
        Object.DestroyImmediate(baseAgent);

        SetObjectReference(agent, "rb", player.GetComponent<Rigidbody2D>());
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "groundCheck", inheritedGroundCheck);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetObjectReference(agent, "mixedLevelGenerator", null);
        SetObjectReference(agent, "evaluationManager", null);
        SetObjectReference(agent, "obstacleAwareGoal", goal);
        SetBool(agent, "enableSpeedRunnerMode", true);
        SetBool(agent, "forceSprintInSpeedRunner", false);
        SetFloat(agent, "speedRunnerSprintReward", 0f);
        SetBool(agent, "maskUselessJumps", !allowFlatObstacleJumps);
        SetFloat(agent, "noProgressTimeLimit", 12f);
        SetFloat(agent, "stuckTimeLimit", 12f);
        SetFloat(agent, "maxEpisodeTime", maxEpisodeSeconds);
        SetFloat(agent, "obstacleCollisionPenalty", -6f);
        SetFloat(agent, "passedAndroidReward", 0.5f);
        SetBool(agent, "debugObstacleAwareEvents", false);

        ConfigureBehavior(player);

        DecisionRequester requester = player.GetComponent<DecisionRequester>();
        if (requester == null)
        {
            requester = player.AddComponent<DecisionRequester>();
        }

        requester.enabled = true;
        return player;
    }

    private static void ConfigureBehavior(GameObject player)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            behavior = player.AddComponent<BehaviorParameters>();
        }

        behavior.BehaviorName = BehaviorName;
        behavior.BehaviorType = BehaviorType.Default;

        SerializedObject serializedBehavior = new SerializedObject(behavior);
        SerializedProperty observationSize = serializedBehavior.FindProperty(
            "m_BrainParameters.VectorObservationSize");
        SerializedProperty continuousActions = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.m_NumContinuousActions");
        SerializedProperty branchSizes = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.BranchSizes");
        SerializedProperty model = serializedBehavior.FindProperty("m_Model");

        if (observationSize != null)
        {
            observationSize.intValue =
                EdgeRunnerAgentV5SpeedRunObstacleAware.DefaultExpectedObservationSize;
        }

        if (continuousActions != null)
        {
            continuousActions.intValue = 0;
        }

        if (branchSizes != null)
        {
            int[] branches =
            {
                EdgeRunnerAgentV5SpeedRunObstacleAware.MovementActionBranchSize,
                EdgeRunnerAgentV5SpeedRunObstacleAware.JumpActionBranchSize,
                EdgeRunnerAgentV5SpeedRunObstacleAware.SprintActionBranchSize
            };
            branchSizes.arraySize = branches.Length;

            for (int i = 0; i < branches.Length; i++)
            {
                branchSizes.GetArrayElementAtIndex(i).intValue = branches[i];
            }
        }

        if (model != null)
        {
            model.objectReferenceValue = null;
        }

        serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateStaticAndroid(
        Transform parent,
        string name,
        Vector3 position,
        Sprite fallbackSprite)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidPrefabPath);
        GameObject android = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : CreateFallbackAndroid(fallbackSprite);

        if (android == null)
        {
            throw new System.InvalidOperationException("Static Android could not be created.");
        }

        android.name = name;
        android.transform.SetParent(parent, false);
        android.transform.position = position;
        android.transform.localScale = new Vector3(0.95f, 1.2f, 1f);

        RemoveConflictingAndroidComponents(android);

        Collider2D collider = android.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = android.AddComponent<BoxCollider2D>();
        }

        collider.enabled = true;
        collider.isTrigger = true;

        Rigidbody2D body = android.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = android.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearVelocity = Vector2.zero;

        DemoAndroidPatrol patrol = android.GetComponent<DemoAndroidPatrol>();
        if (patrol == null)
        {
            patrol = android.AddComponent<DemoAndroidPatrol>();
        }

        patrol.Configure(0f, 0.1f);
        patrol.enabled = false;

        EdgeRunnerEnemyMarker marker = android.GetComponent<EdgeRunnerEnemyMarker>();
        if (marker == null)
        {
            marker = android.AddComponent<EdgeRunnerEnemyMarker>();
        }

        marker.SetAffectsAgent(true);
        marker.SetAlive(true);
        SetBool(marker, "isActiveEnemy", true);
        SetBool(marker, "isDangerous", true);
        SetObjectReference(marker, "visualRoot", android.transform);
        SetObjectReference(marker, "enemyCollider", collider);

        SpeedRunObstacleHazard hazard = android.GetComponent<SpeedRunObstacleHazard>();
        if (hazard == null)
        {
            hazard = android.AddComponent<SpeedRunObstacleHazard>();
        }

        hazard.SetAffectsAgent(true);
        return android;
    }

    private static void RemoveConflictingAndroidComponents(GameObject android)
    {
        DestroyComponents<DemoEnemyHazard>(android);
        DestroyComponents<ScoreAttackAndroid>(android);
        DestroyComponents<StompableAndroidEnemy>(android);
        DestroyComponents<StompableAndroidStompZone>(android);
        DestroyComponents<StompableAndroidSideHazard>(android);
    }

    private static void DestroyComponents<T>(GameObject root) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                Object.DestroyImmediate(components[i]);
            }
        }
    }

    private static GameObject CreateFallbackAndroid(Sprite sprite)
    {
        GameObject android = new GameObject("SpeedRunOA_StaticAndroid_Fallback");
        SpriteRenderer renderer = android.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.58f, 0.68f, 0.75f, 1f);
        renderer.sortingOrder = 7;
        android.AddComponent<BoxCollider2D>();
        android.AddComponent<Rigidbody2D>();
        return android;
    }

    private static Transform CreatePlatform(
        Transform parent,
        string name,
        float centerX,
        float topY,
        float width,
        Sprite sprite)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            throw new System.InvalidOperationException("The Ground layer is missing.");
        }

        GameObject platform = new GameObject(name);
        platform.layer = groundLayer;
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, topY - 0.2f, 0f);
        platform.transform.localScale = new Vector3(width, 0.4f, 1f);

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.18f, 0.32f, 0.36f, 1f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;
        return platform.transform;
    }

    private static GameObject CreateGoal(Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : new GameObject("Goal");

        if (goal == null)
        {
            throw new System.InvalidOperationException("Goal could not be created.");
        }

        goal.name = "Goal";
        goal.transform.position = position;

        Collider2D collider = goal.GetComponent<Collider2D>();
        if (collider == null)
        {
            BoxCollider2D box = goal.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2.5f, 5f);
            collider = box;
        }

        collider.isTrigger = true;
        return goal;
    }

    private static void CreateDeathZone(float centerX, float width, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefabPath);
        GameObject deathZone = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : new GameObject(name);

        if (deathZone == null)
        {
            throw new System.InvalidOperationException("DeathZone could not be created.");
        }

        deathZone.name = name;
        deathZone.transform.position = new Vector3(centerX, -7f, 0f);
        deathZone.transform.localScale = new Vector3(width, 1f, 1f);

        Collider2D collider = deathZone.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = deathZone.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;

        if (deathZone.GetComponent<DeathZone>() == null)
        {
            deathZone.AddComponent<DeathZone>();
        }
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(8f, 5.5f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(0.04f, 0.07f, 0.11f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();

        DemoCameraFollow2D follow = cameraObject.AddComponent<DemoCameraFollow2D>();
        follow.SetTarget(target);
    }

    private static void ValidateScene(
        Scene scene,
        GameObject player,
        GameObject goal,
        GameObject android,
        string phaseName)
    {
        EdgeRunnerAgentV5SpeedRunObstacleAware agent =
            player != null ? player.GetComponent<EdgeRunnerAgentV5SpeedRunObstacleAware>() : null;
        BehaviorParameters behavior = player != null
            ? player.GetComponent<BehaviorParameters>()
            : null;

        if (agent == null || behavior == null || goal == null)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} is missing its agent, BehaviorParameters, or Goal.");
        }

        ValidateBehavior(behavior, phaseName);
        ValidateAgentGoalReferences(agent, goal.transform, phaseName);

        if (CountSceneComponents<EdgeRunnerAgentV5SpeedRunObstacleAware>(scene) != 1 ||
            CountSceneComponents<DeathZone>(scene) != 1 ||
            CountSceneComponents<Camera>(scene) != 1 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 0 ||
            CountSceneComponents<ScoreAttackGoalLock>(scene) != 0 ||
            CountSceneComponents<StompableAndroidEnemy>(scene) != 0 ||
            CountSceneComponents<StompableAndroidStompZone>(scene) != 0 ||
            CountSceneComponents<StompableAndroidSideHazard>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} has an invalid common scene composition.");
        }

        int markerCount = CountSceneComponents<EdgeRunnerEnemyMarker>(scene);
        int hazardCount = CountSceneComponents<SpeedRunObstacleHazard>(scene);

        if (android == null)
        {
            if (markerCount != 0 || hazardCount != 0)
            {
                throw new System.InvalidOperationException(
                    "TraversalBase must contain no Android markers or obstacle hazards.");
            }
        }
        else
        {
            DemoAndroidPatrol patrol = android.GetComponent<DemoAndroidPatrol>();
            Collider2D collider = android.GetComponent<Collider2D>();
            SpeedRunObstacleHazard hazard = android.GetComponent<SpeedRunObstacleHazard>();
            EdgeRunnerEnemyMarker marker = android.GetComponent<EdgeRunnerEnemyMarker>();

            if (markerCount != 1 || hazardCount != 1 || marker == null || hazard == null ||
                !hazard.AffectsAgent || patrol == null || patrol.enabled ||
                collider == null || !collider.enabled || !collider.isTrigger)
            {
                throw new System.InvalidOperationException(
                    "StaticAndroid must contain one static marked trigger hazard.");
            }

            SerializedObject serializedAgent = new SerializedObject(agent);
            SerializedProperty maskUselessJumps =
                serializedAgent.FindProperty("maskUselessJumps");
            if (maskUselessJumps == null || maskUselessJumps.boolValue)
            {
                throw new System.InvalidOperationException(
                    "StaticAndroid must permit flat-ground obstacle jumps.");
            }
        }
    }

    private static void ValidateBehavior(BehaviorParameters behavior, string phaseName)
    {
        SerializedObject serializedBehavior = new SerializedObject(behavior);
        SerializedProperty observationSize = serializedBehavior.FindProperty(
            "m_BrainParameters.VectorObservationSize");
        SerializedProperty continuousActions = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.m_NumContinuousActions");
        SerializedProperty branchSizes = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.BranchSizes");
        SerializedProperty model = serializedBehavior.FindProperty("m_Model");

        bool validBranches = branchSizes != null &&
            branchSizes.arraySize == 3 &&
            branchSizes.GetArrayElementAtIndex(0).intValue == 3 &&
            branchSizes.GetArrayElementAtIndex(1).intValue == 2 &&
            branchSizes.GetArrayElementAtIndex(2).intValue == 2;
        bool valid =
            behavior.BehaviorName == BehaviorName &&
            behavior.BehaviorType == BehaviorType.Default &&
            observationSize != null &&
            observationSize.intValue ==
                EdgeRunnerAgentV5SpeedRunObstacleAware.DefaultExpectedObservationSize &&
            continuousActions != null && continuousActions.intValue == 0 &&
            validBranches && model != null && model.objectReferenceValue == null;

        if (!valid)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must use {BehaviorName}, 67 observations, " +
                "branches [3,2,2], BehaviorType Default, and Model None.");
        }
    }

    private static void ValidateAgentGoalReferences(
        EdgeRunnerAgentV5SpeedRunObstacleAware agent,
        Transform goal,
        string phaseName)
    {
        SerializedObject serializedAgent = new SerializedObject(agent);
        Transform baseGoal =
            serializedAgent.FindProperty("goal")?.objectReferenceValue as Transform;
        Transform obstacleGoal =
            serializedAgent.FindProperty("obstacleAwareGoal")?.objectReferenceValue as Transform;

        if (baseGoal != goal || obstacleGoal != goal)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must assign the same Goal to the base and ObstacleAware fields.");
        }
    }

    private static int CountSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> components = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            components.AddRange(roots[i].GetComponentsInChildren<T>(true));
        }

        return components.Count;
    }

    private static void SaveScene(
        Scene scene,
        string scenePath,
        GameObject player,
        string phaseName)
    {
        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scenePath))
        {
            throw new System.InvalidOperationException(
                $"SpeedRunObstacleAware {phaseName} could not be saved at {scenePath}.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[SPEEDRUN OA BUILDER] Created {scenePath}; behavior={BehaviorName}; " +
            "observations=67; actions=[3,2,2].");
    }

    private static Sprite GetSharedSprite()
    {
        GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);
        SpriteRenderer renderer = groundPrefab != null
            ? groundPrefab.GetComponent<SpriteRenderer>()
            : null;

        return renderer != null && renderer.sprite != null
            ? renderer.sprite
            : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static bool CanReplaceOpenScenes()
    {
        EnsureTrainingSceneFolder();
        return Application.isBatchMode ||
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    private static void EnsureTrainingSceneFolder()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes/Training");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Missing serialized property '{propertyName}' on {target.GetType().Name}.");
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
            throw new System.InvalidOperationException(
                $"Missing serialized property '{propertyName}' on {target.GetType().Name}.");
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Missing serialized property '{propertyName}' on {target.GetType().Name}.");
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
