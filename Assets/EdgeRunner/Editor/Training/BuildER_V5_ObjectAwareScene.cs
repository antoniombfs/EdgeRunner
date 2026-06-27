using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_ObjectAwareScene
{
    private const string ScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_TraversalBase.unity";
    private const string PlayerPrefabPath =
        "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA TraversalBase")]
    public static void BuildTraversalBase()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes/Training");

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_TraversalBase");
        Sprite platformSprite = GetSharedSprite();

        CreatePlatform(root.transform, "Traversal_Start", 3f, 0f, 8f, platformSprite);
        CreatePlatform(root.transform, "Traversal_Landing_A", 12.2f, 0f, 6.4f, platformSprite);
        CreatePlatform(root.transform, "Traversal_Landing_B", 21.5f, 0.35f, 7f, platformSprite);
        CreatePlatform(root.transform, "Traversal_Final", 32.5f, 0.1f, 10f, platformSprite);

        GameObject goal = CreateGoal(new Vector3(35f, 1.2f, 0f));
        GameObject player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        CreateDeathZone(17.5f, 48f);
        CreateCamera(player.transform);
        ValidateTraversalBase(scene, player);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new System.InvalidOperationException(
                $"ObjectAware TraversalBase could not be saved at {ScenePath}.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SceneAsset savedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (savedScene != null)
        {
            EditorGUIUtility.PingObject(savedScene);
        }

        Debug.Log(
            $"[OBJECT AWARE BUILDER] Created and opened {ScenePath} with " +
            $"{EdgeRunnerAgentV5ScoreMaxObjectAware.DefaultExpectedObservationSize} observations.");
    }

    public static void BuildTraversalBaseBatch()
    {
        BuildTraversalBase();
    }

    private static GameObject CreateObjectAwarePlayer(Vector3 position, Transform goal)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException("Player_V5 prefab was not found.", PlayerPrefabPath);
        }

        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (player == null)
        {
            throw new System.InvalidOperationException("Player_V5 prefab could not be instantiated.");
        }

        player.name = "Player_V5_ScoreMaxObjectAware";
        player.transform.position = position;

        EdgeRunnerAgentV5 baseAgent = player.GetComponent<EdgeRunnerAgentV5>();
        string serializedBaseAgent = baseAgent != null
            ? JsonUtility.ToJson(baseAgent)
            : string.Empty;
        Transform inheritedGroundCheck = null;
        if (baseAgent != null)
        {
            SerializedObject serializedAgent = new SerializedObject(baseAgent);
            inheritedGroundCheck = serializedAgent.FindProperty("groundCheck")?.objectReferenceValue as Transform;
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.AddComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        if (!string.IsNullOrEmpty(serializedBaseAgent))
        {
            JsonUtility.FromJsonOverwrite(serializedBaseAgent, agent);
        }

        if (baseAgent != null)
        {
            Object.DestroyImmediate(baseAgent);
        }

        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "groundCheck", inheritedGroundCheck);
        SetObjectReference(agent, "objectAwareGoal", goal);
        SetObjectReference(agent, "objectAwareRigidbody", player.GetComponent<Rigidbody2D>());
        SetBool(agent, "debugObjectAwareObservationCount", false);
        SetBool(agent, "debugObjectAwareNextObjective", false);
        SetBool(agent, "debugObjectAwareJumpContext", false);
        SetBool(agent, "debugObjectAwareGizmos", false);

        ConfigureBehavior(player);
        DecisionRequester requester = player.GetComponent<DecisionRequester>();
        if (requester == null)
        {
            requester = player.AddComponent<DecisionRequester>();
        }

        requester.enabled = true;

        EdgeRunnerAgentV5[] agents = player.GetComponents<EdgeRunnerAgentV5>();
        if (agents.Length != 1 || agents[0] != agent)
        {
            throw new System.InvalidOperationException(
                "ObjectAware player must contain exactly one active Agent component.");
        }

        return player;
    }

    private static void ValidateTraversalBase(Scene scene, GameObject player)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        if (objectAwareAgent == null || !objectAwareAgent.enabled)
        {
            throw new System.InvalidOperationException(
                "TraversalBase requires an enabled EdgeRunnerAgentV5ScoreMaxObjectAware.");
        }

        if (player.GetComponent<EdgeRunnerAgentV5ScoreMax>() != null)
        {
            throw new System.InvalidOperationException(
                "TraversalBase must not contain the legacy 83-observation ScoreMax agent.");
        }

        EdgeRunnerAgentV5[] agents = player.GetComponents<EdgeRunnerAgentV5>();
        if (agents.Length != 1 || agents[0] != objectAwareAgent)
        {
            throw new System.InvalidOperationException(
                "TraversalBase must contain exactly one ObjectAware Agent component.");
        }

        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
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
        bool validBehavior = behavior != null &&
            behavior.BehaviorName == "EdgeRunnerV5ScoreMaxObjectAware" &&
            behavior.BehaviorType == BehaviorType.Default &&
            observationSize != null &&
            observationSize.intValue ==
                EdgeRunnerAgentV5ScoreMaxObjectAware.DefaultExpectedObservationSize &&
            continuousActions != null &&
            continuousActions.intValue == 0 &&
            validBranches &&
            model != null &&
            model.objectReferenceValue == null;

        if (!validBehavior)
        {
            throw new System.InvalidOperationException(
                "TraversalBase BehaviorParameters must be ObjectAware, 111 observations, " +
                "branches [3,2,2], Default, and Model None.");
        }

        if (SceneContainsComponent<ScoreAttackManager>(scene) ||
            SceneContainsComponent<ScoreAttackCoin>(scene) ||
            SceneContainsComponent<ScoreAttackAndroid>(scene) ||
            SceneContainsComponent<ScoreAttackGoalLock>(scene))
        {
            throw new System.InvalidOperationException(
                "TraversalBase must not contain ScoreAttack managers, objectives, or a locked Goal.");
        }
    }

    private static bool SceneContainsComponent<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren<T>(true) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureBehavior(GameObject player)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            behavior = player.AddComponent<BehaviorParameters>();
        }

        behavior.BehaviorName = "EdgeRunnerV5ScoreMaxObjectAware";
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
                EdgeRunnerAgentV5ScoreMaxObjectAware.DefaultExpectedObservationSize;
        }

        if (continuousActions != null)
        {
            continuousActions.intValue = 0;
        }

        if (branchSizes != null)
        {
            int[] branches = { 3, 2, 2 };
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

    private static void CreatePlatform(
        Transform parent,
        string name,
        float centerX,
        float topY,
        float width,
        Sprite sprite)
    {
        GameObject platform = new GameObject(name);
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, topY - 0.2f, 0f);
        platform.transform.localScale = new Vector3(width, 0.4f, 1f);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            throw new System.InvalidOperationException("The Ground layer is missing.");
        }

        platform.layer = groundLayer;
        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.18f, 0.32f, 0.34f, 1f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;
    }

    private static GameObject CreateGoal(Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : new GameObject("Goal_ScoreMaxOA_TraversalBase");

        if (goal == null)
        {
            throw new System.InvalidOperationException("Goal could not be created.");
        }

        goal.name = "Goal_ScoreMaxOA_TraversalBase";
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

    private static void CreateDeathZone(float centerX, float width)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefabPath);
        GameObject deathZone = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : new GameObject("DeathZone_ScoreMaxOA_TraversalBase");

        if (deathZone == null)
        {
            throw new System.InvalidOperationException("DeathZone could not be created.");
        }

        deathZone.name = "DeathZone_ScoreMaxOA_TraversalBase";
        deathZone.transform.position = new Vector3(centerX, -7f, 0f);
        deathZone.transform.localScale = new Vector3(width, 1f, 1f);

        Collider2D collider = deathZone.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = deathZone.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
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

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on {target.name}.");
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
                $"Serialized property '{propertyName}' was not found on {target.name}.");
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

}
