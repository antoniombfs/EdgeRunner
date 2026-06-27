using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_ObjectAwareScene
{
    private const string TraversalScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_TraversalBase.unity";
    private const string LowCoinRunScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_LowCoinRun.unity";
    private const string HighCoinJumpScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_HighCoinJump.unity";
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

        if (!CanReplaceOpenScenes())
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

        SaveAndKeepOpen(scene, TraversalScenePath, player, "TraversalBase");
    }

    public static void BuildTraversalBaseBatch()
    {
        BuildTraversalBase();
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA LowCoinRun")]
    public static void BuildLowCoinRun()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_LowCoinRun");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "LowCoinRun_Platform", 7f, 0f, 18f, sprite);

        ScoreAttackManager manager = CreateCoinManager(root.transform);
        GameObject goal = CreateLockedGoal("Goal_ScoreMaxOA_LowCoinRun", new Vector3(11.5f, 1.2f, 0f), manager);
        GameObject player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureCoinPhasePlayer(
            player,
            manager,
            EdgeRunnerObjectAwarePhase.LowCoinRun,
            true);
        CreateCoin(root.transform, "LowCoinRun_Coin_01", new Vector3(3.5f, 1.35f, 0f), sprite, manager);
        CreateCoin(root.transform, "LowCoinRun_Coin_02", new Vector3(6f, 1.35f, 0f), sprite, manager);
        CreateDeathZone(7f, 30f, "DeathZone_ScoreMaxOA_LowCoinRun");
        CreateCamera(player.transform);
        ValidateCoinPhase(scene, player, EdgeRunnerObjectAwarePhase.LowCoinRun, 2);
        SaveAndKeepOpen(scene, LowCoinRunScenePath, player, "LowCoinRun");
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA HighCoinJump")]
    public static void BuildHighCoinJump()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_HighCoinJump");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "HighCoinJump_Platform", 7f, 0f, 18f, sprite);

        ScoreAttackManager manager = CreateCoinManager(root.transform);
        GameObject goal = CreateLockedGoal("Goal_ScoreMaxOA_HighCoinJump", new Vector3(11.5f, 1.2f, 0f), manager);
        GameObject player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureCoinPhasePlayer(
            player,
            manager,
            EdgeRunnerObjectAwarePhase.HighCoinJump,
            false);
        CreateCoin(root.transform, "HighCoinJump_Coin_01", new Vector3(3.5f, 2.55f, 0f), sprite, manager);
        CreateCoin(root.transform, "HighCoinJump_Coin_02", new Vector3(6.5f, 2.55f, 0f), sprite, manager);
        CreateDeathZone(7f, 30f, "DeathZone_ScoreMaxOA_HighCoinJump");
        CreateCamera(player.transform);
        ValidateCoinPhase(scene, player, EdgeRunnerObjectAwarePhase.HighCoinJump, 2);
        SaveAndKeepOpen(scene, HighCoinJumpScenePath, player, "HighCoinJump");
    }

    public static void BuildCoinPhasesBatch()
    {
        BuildLowCoinRun();
        BuildHighCoinJump();
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
        ValidateObjectAwarePlayer(player, "TraversalBase");

        if (SceneContainsComponent<ScoreAttackManager>(scene) ||
            SceneContainsComponent<ScoreAttackCoin>(scene) ||
            SceneContainsComponent<ScoreAttackAndroid>(scene) ||
            SceneContainsComponent<ScoreAttackGoalLock>(scene))
        {
            throw new System.InvalidOperationException(
                "TraversalBase must not contain ScoreAttack managers, objectives, or a locked Goal.");
        }

        ValidateCommonSceneObjects(scene, "TraversalBase");
    }

    private static void ValidateCoinPhase(
        Scene scene,
        GameObject player,
        EdgeRunnerObjectAwarePhase expectedPhase,
        int expectedCoins)
    {
        string phaseName = expectedPhase.ToString();
        ValidateObjectAwarePlayer(player, phaseName);
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");

        if (phase == null || phase.enumValueIndex != (int)expectedPhase)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} has the wrong ObjectAware curriculum phase.");
        }

        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            CountSceneComponents<ScoreAttackCoin>(scene) != expectedCoins ||
            CountSceneComponents<ScoreAttackGoalLock>(scene) != 1 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must contain one manager, {expectedCoins} coins, one locked Goal, " +
                "and no Androids.");
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateObjectAwarePlayer(GameObject player, string phaseName)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        if (player.name != "Player_V5_ScoreMaxObjectAware" ||
            objectAwareAgent == null ||
            !objectAwareAgent.enabled)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} requires Player_V5_ScoreMaxObjectAware with its agent enabled.");
        }

        if (player.GetComponent<EdgeRunnerAgentV5ScoreMax>() != null)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must not contain the legacy 83-observation ScoreMax agent.");
        }

        EdgeRunnerAgentV5[] agents = player.GetComponents<EdgeRunnerAgentV5>();
        if (agents.Length != 1 || agents[0] != objectAwareAgent)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must contain exactly one ObjectAware Agent component.");
        }

        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            throw new System.InvalidOperationException($"{phaseName} is missing BehaviorParameters.");
        }

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
        bool validBehavior =
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
                $"{phaseName} must use ObjectAware, 111 observations, branches [3,2,2], " +
                "Default, and Model None.");
        }
    }

    private static void ValidateCommonSceneObjects(Scene scene, string phaseName)
    {
        if (CountSceneComponents<DeathZone>(scene) != 1 ||
            CountSceneComponents<Camera>(scene) != 1)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} must contain exactly one DeathZone and one Camera.");
        }
    }

    private static bool SceneContainsComponent<T>(Scene scene) where T : Component
    {
        return CountSceneComponents<T>(scene) > 0;
    }

    private static int CountSceneComponents<T>(Scene scene) where T : Component
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            count += roots[i].GetComponentsInChildren<T>(true).Length;
        }

        return count;
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

    private static bool CanReplaceOpenScenes()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes/Training");
        return Application.isBatchMode ||
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    private static void SaveAndKeepOpen(
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
                $"ObjectAware {phaseName} could not be saved at {scenePath}.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SceneAsset savedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (savedScene != null)
        {
            EditorGUIUtility.PingObject(savedScene);
        }

        Debug.Log(
            $"[OBJECT AWARE BUILDER] Created and opened {scenePath} with " +
            $"{EdgeRunnerAgentV5ScoreMaxObjectAware.DefaultExpectedObservationSize} observations.");
    }

    private static ScoreAttackManager CreateCoinManager(Transform parent)
    {
        GameObject managerObject = new GameObject("ScoreMaxOA_CoinManager");
        managerObject.transform.SetParent(parent, false);
        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetInt(manager, "minActiveCoins", 2);
        SetInt(manager, "maxActiveCoins", 2);
        SetInt(manager, "minActiveEnemies", 0);
        SetInt(manager, "maxActiveEnemies", 0);
        SetFloat(manager, "coinReward", 2f);
        SetFloat(manager, "finalCompletionReward", 10f);
        SetFloat(manager, "prematureGoalPenalty", -2f);
        SetBool(manager, "endEpisodeOnPrematureGoal", false);
        SetBool(manager, "debugLogs", false);
        return manager;
    }

    private static void ConfigureCoinPhasePlayer(
        GameObject player,
        ScoreAttackManager manager,
        EdgeRunnerObjectAwarePhase phase,
        bool useContextualJumpMask)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetObjectReference(manager, "agent", agent);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "maskUselessJumps", false);
        SetInt(agent, "objectAwarePhase", (int)phase);
        SetBool(agent, "enableObjectAwareRewardShaping", true);
        SetBool(agent, "enableMissedCoinEpisodeEnd", true);
        SetBool(agent, "enableContextualJumpMask", useContextualJumpMask);
        SetFloat(agent, "missedCoinPenalty", -2f);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "lowCoinHeightThreshold", 0.45f);
        SetFloat(agent, "lowCoinRunWindowX", 3f);
        SetFloat(agent, "highCoinJumpWindowX", 2.25f);
        SetFloat(agent, "lowCoinGroundApproachReward", 0.01f);
        SetFloat(agent, "lowCoinGroundedAlignmentReward", 0.005f);
        SetFloat(agent, "lowCoinUnnecessaryJumpPenalty", -0.02f);
        SetFloat(agent, "highCoinApproachReward", 0.01f);
        SetFloat(agent, "highCoinJumpCueReward", 0.04f);
        SetFloat(agent, "earlyJumpPenalty", -0.01f);
        SetFloat(agent, "jumpSpamPenalty", -0.01f);

        // Coin phases learn from nextObjective rather than the locked Goal.
        SetFloat(agent, "goalReward", 0f);
        SetFloat(agent, "progressRewardScale", 0f);
        SetFloat(agent, "maxProgressRewardPerStep", 0f);
        SetFloat(agent, "milestoneReward", 0f);
        SetFloat(agent, "backtrackPenalty", 0f);
        SetFloat(agent, "jumpPenalty", 0f);
        SetFloat(agent, "idleJumpPenalty", 0f);
        SetFloat(agent, "flatGroundJumpPenalty", 0f);
        SetFloat(agent, "earlyGapJumpPenalty", 0f);
        SetFloat(agent, "uselessJumpPenalty", 0f);
        SetFloat(agent, "gapJumpReward", 0f);
        SetFloat(agent, "gapLandingReward", 0f);
        SetFloat(agent, "lowMomentumJumpPenalty", 0f);
        SetFloat(agent, "forwardActionReward", 0f);
        SetFloat(agent, "forwardVelocityReward", 0f);
        SetFloat(agent, "wrongDirectionActionPenalty", 0f);
        SetFloat(agent, "distanceProgressRewardScale", 0f);
        SetFloat(agent, "maxDistanceProgressReward", 0f);
        SetFloat(agent, "distanceRegressionPenaltyScale", 0f);
        SetFloat(agent, "maxDistanceRegressionPenalty", 0f);
        SetFloat(agent, "stepPenalty", -0.001f);
        SetFloat(agent, "idlePenalty", -0.001f);
        SetFloat(agent, "noProgressTimeLimit", 15f);
        SetFloat(agent, "stuckTimeLimit", 15f);
        SetFloat(agent, "maxEpisodeTime", 60f);

        SetBool(agent, "debugObjectAwareObservationCount", false);
        SetBool(agent, "debugObjectAwareNextObjective", false);
        SetBool(agent, "debugObjectAwareJumpContext", false);
        SetBool(agent, "debugObjectAwareGizmos", false);
    }

    private static GameObject CreateLockedGoal(
        string name,
        Vector3 position,
        ScoreAttackManager manager)
    {
        GameObject goal = CreateGoal(position);
        goal.name = name;
        goal.tag = "Goal";
        goal.transform.localScale = new Vector3(1.2f, 2.4f, 1f);
        ScoreAttackGoalLock goalLock = goal.GetComponent<ScoreAttackGoalLock>();
        if (goalLock == null)
        {
            goalLock = goal.AddComponent<ScoreAttackGoalLock>();
        }

        goalLock.SetManager(manager);
        SetObjectReference(manager, "goal", goal.transform);
        return goal;
    }

    private static void CreateCoin(
        Transform parent,
        string name,
        Vector3 position,
        Sprite sprite,
        ScoreAttackManager manager)
    {
        GameObject coin = new GameObject(name);
        coin.transform.SetParent(parent, false);
        coin.transform.position = position;
        coin.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer renderer = coin.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.85f, 0.15f, 1f);
        renderer.sortingOrder = 8;

        CircleCollider2D collider = coin.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        ScoreAttackCoin coinScript = coin.AddComponent<ScoreAttackCoin>();
        coinScript.SetManager(manager);
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

    private static void CreateDeathZone(
        float centerX,
        float width,
        string name = "DeathZone_ScoreMaxOA_TraversalBase")
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

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on {target.name}.");
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on {target.name}.");
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

}
