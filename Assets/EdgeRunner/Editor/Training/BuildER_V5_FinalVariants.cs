using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_FinalVariants
{
    private const string GoalRunnerScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_GoalRunnerRandom.unity";
    private const string SpeedRunnerScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerRandom.unity";
    private const string ScoreAttackIntroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackIntro.unity";
    private const string ScoreAttackEasyScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackEasy.unity";
    private const string ScoreAttackRandomControlledScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreAttackRandomControlled.unity";

    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidEnemyPrefabPath = "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";
    private const string NoFrictionMaterialPath = "Assets/EdgeRunner/Physics/NoFriction2D.physicsMaterial2D";

    [MenuItem("EdgeRunner/Training/V5/Build GoalRunnerRandom")]
    public static void BuildGoalRunnerRandomFromMenu()
    {
        BuildGoalRunnerRandomScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build SpeedRunnerRandom")]
    public static void BuildSpeedRunnerRandomFromMenu()
    {
        BuildSpeedRunnerRandomScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreAttackIntro")]
    public static void BuildScoreAttackIntroFromMenu()
    {
        BuildScoreAttackIntroScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreAttackEasy")]
    public static void BuildScoreAttackEasyFromMenu()
    {
        BuildScoreAttackEasyScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreAttackRandomControlled")]
    public static void BuildScoreAttackRandomControlledFromMenu()
    {
        BuildScoreAttackRandomControlledScene();
    }

    public static void BuildGoalRunnerRandomScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PhysicsMaterial2D noFrictionMaterial = CreateOrUpdateNoFrictionMaterial();
        GameObject root = new GameObject("ER_V5_GoalRunnerRandom");
        GameObject player = CreatePlayer(new Vector3(1f, 1.5f, 0f));
        GameObject generator = CreateMixedLevelGenerator(root.transform, "GoalRunnerRandomGenerator", false, noFrictionMaterial);

        ConfigureGoalRunnerPlayer(player, generator.GetComponent<MixedLevelGenerator>(), speedRunner: false, noFrictionMaterial);
        CreateCamera(player.transform);
        CreateDeathZone("DeathZone_GoalRunnerRandom", 28f, 120f);
        CreateEvaluationManager("GoalRunnerRandom_Evaluation", player, "GoalRunnerRandom", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, GoalRunnerScenePath, "GoalRunnerRandom");
    }

    public static void BuildSpeedRunnerRandomScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PhysicsMaterial2D noFrictionMaterial = CreateOrUpdateNoFrictionMaterial();
        GameObject root = new GameObject("ER_V5_SpeedRunnerRandom");
        GameObject player = CreatePlayer(new Vector3(1f, 1.5f, 0f));
        GameObject generator = CreateMixedLevelGenerator(root.transform, "SpeedRunnerRandomGenerator", true, noFrictionMaterial);

        ConfigureGoalRunnerPlayer(player, generator.GetComponent<MixedLevelGenerator>(), speedRunner: true, noFrictionMaterial);
        CreateCamera(player.transform);
        CreateDeathZone("DeathZone_SpeedRunnerRandom", 28f, 120f);
        CreateEvaluationManager("SpeedRunnerRandom_Evaluation", player, "SpeedRunnerRandom", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, SpeedRunnerScenePath, "SpeedRunnerRandom");
    }

    public static void BuildScoreAttackIntroScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreAttackIntro");
        ScoreAttackManager manager = CreateScoreAttackManager(root.transform, false, 1, 1);
        GameObject player = CreatePlayer(new Vector3(0f, 1.15f, 0f));
        GameObject goal = CreateScoreAttackGoal(new Vector3(10f, 1.1f, 0f), manager);

        ConfigureScoreAttackPlayer(player, goal.transform, manager);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreAttackIntro_Platform", 5f, 0f, new Vector2(24f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_01", new Vector3(3f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackAndroid(root.transform, "ScoreAttackAndroid_01", new Vector3(6f, 1.02f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreAttackIntro", 5f, 34f);
        CreateEvaluationManager("ScoreAttackIntro_Evaluation", player, "ScoreAttackIntro", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreAttackIntroScenePath, "ScoreAttackIntro");
    }

    public static void BuildScoreAttackEasyScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreAttackEasy");
        ScoreAttackManager manager = CreateScoreAttackManager(root.transform, false, 2, 1);
        GameObject player = CreatePlayer(new Vector3(0f, 1.15f, 0f));
        GameObject goal = CreateScoreAttackGoal(new Vector3(13f, 1.1f, 0f), manager);

        ConfigureScoreAttackPlayer(player, goal.transform, manager);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreAttackEasy_Platform", 6.5f, 0f, new Vector2(30f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_01", new Vector3(3f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_02", new Vector3(8.5f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackAndroid(root.transform, "ScoreAttackAndroid_01", new Vector3(6f, 1.02f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreAttackEasy", 6.5f, 38f);
        CreateEvaluationManager("ScoreAttackEasy_Evaluation", player, "ScoreAttackEasy", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreAttackEasyScenePath, "ScoreAttackEasy");
    }

    public static void BuildScoreAttackRandomControlledScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreAttackRandomControlled");
        ScoreAttackManager manager = CreateScoreAttackManager(root.transform, true, 3, 2);
        GameObject player = CreatePlayer(new Vector3(0f, 1.15f, 0f));
        GameObject goal = CreateScoreAttackGoal(new Vector3(15f, 1.1f, 0f), manager);
        ConfigureScoreAttackRandomCoinPlacement(manager, goal.transform);

        ConfigureScoreAttackPlayer(player, goal.transform, manager);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreAttackRandomControlled_Platform", 7.5f, 0f, new Vector2(34f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_01", new Vector3(3f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_02", new Vector3(6f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackCoin(root.transform, "ScoreAttackCoin_03", new Vector3(9f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackAndroid(root.transform, "ScoreAttackAndroid_01", new Vector3(6f, 1.02f, 0f), sprite, manager);
        CreateScoreAttackAndroid(root.transform, "ScoreAttackAndroid_02", new Vector3(10f, 1.02f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreAttackRandomControlled", 7.5f, 42f);
        CreateEvaluationManager("ScoreAttackRandomControlled_Evaluation", player, "ScoreAttackRandomControlled", "Eval100");

        Selection.activeObject = player;
        SaveScene(scene, ScoreAttackRandomControlledScenePath, "ScoreAttackRandomControlled");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes", "Training");
        EnsureFolder("Assets/EdgeRunner", "Editor");
        EnsureFolder("Assets/EdgeRunner/Editor", "Training");
        EnsureFolder("Assets/EdgeRunner/ML/Config", "V5");
        EnsureFolder("Assets/EdgeRunner", "Docs");
        EnsureFolder("Assets/EdgeRunner", "Physics");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static GameObject CreatePlayer(Vector3 position)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

        if (playerPrefab == null)
        {
            throw new System.InvalidOperationException($"Missing Player_V5 prefab at {PlayerPrefabPath}");
        }

        GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;

        if (player == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate Player_V5 prefab.");
        }

        player.name = "Player_V5";
        player.transform.position = position;
        return player;
    }

    private static void ConfigureGoalRunnerPlayer(
        GameObject player,
        MixedLevelGenerator generator,
        bool speedRunner,
        PhysicsMaterial2D noFrictionMaterial)
    {
        EdgeRunnerAgentV5 agent = RequireAgent(player);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        SetObjectReference(agent, "rb", rb);
        SetObjectReference(agent, "goal", null);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", true);
        SetObjectReference(agent, "mixedLevelGenerator", generator);
        SetObjectReference(agent, "evaluationManager", null);
        SetObjectReference(agent, "scoreAttackManager", null);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "disableTrainingEpisodeEndsInDemo", false);
        SetBool(agent, "disableAgentMovementInDemo", false);
        SetBool(agent, "enableLedgeUnstuck", true);
        SetBool(agent, "debugLedgeUnstuck", false);
        SetFloat(agent, "ledgeStuckMinTime", 0.15f);
        SetFloat(agent, "ledgeFrontCheckDistance", 0.25f);
        SetFloat(agent, "ledgeFootClearCheckDistance", 0.20f);
        SetFloat(agent, "ledgeUnstuckHorizontalNudge", 0.20f);
        SetFloat(agent, "ledgeUnstuckVerticalNudge", 0.12f);
        SetFloat(agent, "ledgeUnstuckCooldown", 0.25f);
        SetInt(agent, "ledgeMaxUnstucksPerEpisode", 5);
        ApplyPhysicsMaterialToColliders(player, noFrictionMaterial);

        if (speedRunner)
        {
            SetFloat(agent, "goalReward", 12.0f);
            SetFloat(agent, "stepPenalty", -0.0015f);
            SetFloat(agent, "distanceProgressRewardScale", 0.12f);
            SetFloat(agent, "maxDistanceProgressReward", 0.12f);
            SetFloat(agent, "progressRewardScale", 0.10f);
            SetFloat(agent, "maxProgressRewardPerStep", 0.10f);
            SetFloat(agent, "forwardVelocityReward", 0.004f);
            SetFloat(agent, "idlePenalty", -0.004f);
            SetFloat(agent, "noProgressTimeLimit", 6.0f);
            SetFloat(agent, "stuckTimeLimit", 6.0f);
            SetFloat(agent, "maxEpisodeTime", 35.0f);
        }
        else
        {
            SetFloat(agent, "goalReward", 10.0f);
            SetFloat(agent, "stepPenalty", -0.0003f);
            SetFloat(agent, "distanceProgressRewardScale", 0.08f);
            SetFloat(agent, "maxDistanceProgressReward", 0.08f);
            SetFloat(agent, "progressRewardScale", 0.05f);
            SetFloat(agent, "maxProgressRewardPerStep", 0.05f);
            SetFloat(agent, "noProgressTimeLimit", 8.0f);
            SetFloat(agent, "stuckTimeLimit", 8.0f);
            SetFloat(agent, "maxEpisodeTime", 45.0f);
        }

        ConfigureBehavior(player, BehaviorType.Default);
        EnsureDecisionRequester(player, true);
    }

    private static void ConfigureScoreAttackPlayer(GameObject player, Transform goal, ScoreAttackManager manager)
    {
        EdgeRunnerAgentV5 agent = RequireAgent(player);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        SetObjectReference(agent, "rb", rb);
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetObjectReference(agent, "mixedLevelGenerator", null);
        SetObjectReference(agent, "evaluationManager", null);
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetFloat(agent, "goalReward", 5.0f);
        SetFloat(agent, "stepPenalty", -0.0005f);
        SetFloat(agent, "distanceProgressRewardScale", 0.06f);
        SetFloat(agent, "maxDistanceProgressReward", 0.06f);
        SetFloat(agent, "noProgressTimeLimit", 10.0f);
        SetFloat(agent, "stuckTimeLimit", 10.0f);
        SetFloat(agent, "maxEpisodeTime", 50.0f);
        SetBool(agent, "disableTrainingEpisodeEndsInDemo", false);
        SetBool(agent, "disableAgentMovementInDemo", false);
        SetBool(agent, "enableLedgeUnstuck", false);
        SetBool(agent, "debugLedgeUnstuck", false);
        SetObjectReference(manager, "agent", agent);

        ConfigureBehavior(player, BehaviorType.Default);
        EnsureDecisionRequester(player, true);
    }

    private static EdgeRunnerAgentV5 RequireAgent(GameObject player)
    {
        EdgeRunnerAgentV5 agent = player.GetComponent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            throw new System.InvalidOperationException("Player_V5 prefab does not contain EdgeRunnerAgentV5.");
        }

        return agent;
    }

    private static void ConfigureBehavior(GameObject player, BehaviorType behaviorType)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();

        if (behavior == null)
        {
            behavior = player.AddComponent<BehaviorParameters>();
        }

        behavior.BehaviorName = "EdgeRunnerV5";
        behavior.BehaviorType = behaviorType;

        SerializedObject serializedObject = new SerializedObject(behavior);
        SerializedProperty vectorObservationSize = serializedObject.FindProperty("m_BrainParameters.VectorObservationSize");

        if (vectorObservationSize != null)
        {
            vectorObservationSize.intValue = EdgeRunnerAgentV5.DefaultExpectedObservationSize;
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

    private static void EnsureDecisionRequester(GameObject player, bool enabled)
    {
        DecisionRequester decisionRequester = player.GetComponent<DecisionRequester>();

        if (decisionRequester == null)
        {
            decisionRequester = player.AddComponent<DecisionRequester>();
        }

        decisionRequester.DecisionPeriod = 1;
        decisionRequester.TakeActionsBetweenDecisions = true;
        decisionRequester.enabled = enabled;
    }

    private static GameObject CreateMixedLevelGenerator(
        Transform parent,
        string name,
        bool speedRunner,
        PhysicsMaterial2D noFrictionMaterial)
    {
        GameObject generatorObject = new GameObject(name);
        generatorObject.transform.SetParent(parent, false);

        GameObject levelRoot = new GameObject(name + "_LevelRoot");
        levelRoot.transform.SetParent(generatorObject.transform, false);

        MixedLevelGenerator generator = generatorObject.AddComponent<MixedLevelGenerator>();
        SetObjectReference(generator, "levelRoot", levelRoot.transform);
        SetObjectReference(generator, "platformPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath));
        SetObjectReference(generator, "goalPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath));
        SetObjectReference(generator, "platformPhysicsMaterial", noFrictionMaterial);
        SetBool(generator, "generateOnStart", false);
        SetInt(generator, "minSegments", speedRunner ? 5 : 6);
        SetInt(generator, "maxSegments", speedRunner ? 8 : 9);
        SetFloat(generator, "minPlatformWidth", 4.8f);
        SetFloat(generator, "minLandingPlatformWidth", 5.0f);
        SetFloat(generator, "minRecoveryPlatformWidth", 5.0f);
        SetFloat(generator, "maxPlatformWidth", 7.5f);
        SetFloat(generator, "minGapWidth", 2.2f);
        SetFloat(generator, "maxGapWidth", speedRunner ? 3.0f : 3.2f);
        SetFloat(generator, "minDistanceBetweenGaps", 5.0f);
        SetFloat(generator, "minRunupBeforeGap", 2.5f);
        SetFloat(generator, "minLandingAfterGap", 5.0f);
        SetFloat(generator, "minHeightDelta", 0.25f);
        SetFloat(generator, "maxHeightDelta", 0.9f);
        SetBool(generator, "useV5GenerationRules", true);
        SetBool(generator, "forceRecoveryPlatformAfterHardSegment", true);
        SetBool(generator, "avoidRepeatedGaps", true);
        SetBool(generator, "avoidHardGapIntoStepUp", true);
        SetBool(generator, "enablePlatformChainSegment", !speedRunner);
        SetFloat(generator, "minPlatformChainWidth", 4.5f);
        SetFloat(generator, "maxPlatformChainWidth", 5.5f);
        SetFloat(generator, "minPlatformChainGap", 2.2f);
        SetFloat(generator, "maxPlatformChainGap", 2.8f);
        SetInt(generator, "maxPlatformChainsPerEpisode", speedRunner ? 0 : 1);
        SetFloat(generator, "safeEdgeMargin", 1.0f);
        SetFloat(generator, "finalGoalPlatformWidth", 10.0f);
        SetFloat(generator, "finalGoalSafeRunup", 5.0f);
        SetFloat(generator, "goalEdgeMargin", 2.0f);
        SetBool(generator, "useWideGoalTrigger", true);
        SetVector2(generator, "wideGoalTriggerSize", new Vector2(2.5f, 7.0f));
        SetBool(generator, "debugGenerationValues", false);
        SetFloat(generator, "startX", 0f);
        SetFloat(generator, "startY", 0f);
        SetFloat(generator, "goalXOffsetFromEnd", 2f);
        SetFloat(generator, "goalYOffset", 1.1f);

        return generatorObject;
    }

    private static PhysicsMaterial2D CreateOrUpdateNoFrictionMaterial()
    {
        EnsureFolder("Assets/EdgeRunner", "Physics");

        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionMaterialPath);

        if (material == null)
        {
            material = new PhysicsMaterial2D("NoFriction2D");
            AssetDatabase.CreateAsset(material, NoFrictionMaterialPath);
        }

        material.friction = 0f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void ApplyPhysicsMaterialToColliders(GameObject root, PhysicsMaterial2D material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                colliders[i].sharedMaterial = material;
            }
        }
    }

    private static ScoreAttackManager CreateScoreAttackManager(Transform parent, bool randomize, int maxCoins, int maxEnemies)
    {
        GameObject managerObject = new GameObject("ScoreAttackManager");
        managerObject.transform.SetParent(parent, false);

        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", randomize);
        SetInt(manager, "minActiveCoins", randomize ? 2 : maxCoins);
        SetInt(manager, "maxActiveCoins", maxCoins);
        SetInt(manager, "minActiveEnemies", randomize ? 1 : maxEnemies);
        SetInt(manager, "maxActiveEnemies", maxEnemies);
        SetFloat(manager, "coinReward", 1.0f);
        SetFloat(manager, "enemyKillReward", 3.0f);
        SetFloat(manager, "finalCompletionReward", 10.0f);
        SetFloat(manager, "prematureGoalPenalty", -2.0f);
        SetFloat(manager, "enemySideHitPenalty", -6.0f);
        SetBool(manager, "debugLogs", false);

        return manager;
    }

    private static void ConfigureScoreAttackRandomCoinPlacement(ScoreAttackManager manager, Transform goal)
    {
        SetObjectReference(manager, "goal", goal);
        SetVector2(manager, "coinRandomXRange", new Vector2(2.0f, 13.0f));
        SetFloat(manager, "coinPlatformTopY", 0f);
        SetFloat(manager, "coinVerticalOffset", 1.2f);
        SetFloat(manager, "minCoinSpacing", 2.0f);
        SetFloat(manager, "coinEdgeMargin", 1.0f);
        SetFloat(manager, "minCoinDistanceFromAndroid", 2.0f);
        SetFloat(manager, "minCoinDistanceFromGoal", 2.5f);
        SetInt(manager, "maxCoinPlacementAttempts", 30);
    }

    private static GameObject CreateScoreAttackGoal(Vector3 position, ScoreAttackManager manager)
    {
        GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal = goalPrefab != null
            ? PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject
            : new GameObject("Goal");

        if (goal == null)
        {
            throw new System.InvalidOperationException("Failed to create ScoreAttack goal.");
        }

        goal.name = "ScoreAttackGoal_Locked";
        goal.tag = "Goal";
        goal.transform.position = position;
        goal.transform.localScale = new Vector3(1.2f, 2.4f, 1f);

        Collider2D collider = goal.GetComponent<Collider2D>();

        if (collider == null)
        {
            collider = goal.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;

        ScoreAttackGoalLock goalLock = goal.GetComponent<ScoreAttackGoalLock>();

        if (goalLock == null)
        {
            goalLock = goal.AddComponent<ScoreAttackGoalLock>();
        }

        goalLock.SetManager(manager);
        return goal;
    }

    private static void CreateScoreAttackCoin(Transform parent, string name, Vector3 position, Sprite sprite, ScoreAttackManager manager)
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

    private static void CreateScoreAttackAndroid(Transform parent, string name, Vector3 position, Sprite fallbackSprite, ScoreAttackManager manager)
    {
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        GameObject android = enemyPrefab != null
            ? PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject
            : CreateFallbackAndroid(fallbackSprite);

        if (android == null)
        {
            return;
        }

        android.name = name;
        android.transform.SetParent(parent, false);
        android.transform.position = position;
        android.transform.localScale = new Vector3(0.95f, 1.2f, 1f);

        Collider2D collider = android.GetComponent<Collider2D>();

        if (collider == null)
        {
            collider = android.AddComponent<BoxCollider2D>();
        }

        collider.enabled = true;
        collider.isTrigger = true;

        Rigidbody2D rb = android.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = android.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        DisableDemoEnemyScripts(android);

        ScoreAttackAndroid androidScript = android.GetComponent<ScoreAttackAndroid>();

        if (androidScript == null)
        {
            androidScript = android.AddComponent<ScoreAttackAndroid>();
        }

        androidScript.enabled = true;
        androidScript.SetManager(manager);
    }

    private static GameObject CreateFallbackAndroid(Sprite sprite)
    {
        GameObject enemy = new GameObject("ScoreAttackAndroid_Fallback");
        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.58f, 0.64f, 0.68f, 1f);
        renderer.sortingOrder = 7;

        enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<Rigidbody2D>();
        return enemy;
    }

    private static void DisableDemoEnemyScripts(GameObject android)
    {
        DemoEnemyHazard[] hazards = android.GetComponentsInChildren<DemoEnemyHazard>(true);

        for (int i = 0; i < hazards.Length; i++)
        {
            hazards[i].enabled = false;
        }

        StompableAndroidEnemy[] stompEnemies = android.GetComponentsInChildren<StompableAndroidEnemy>(true);

        for (int i = 0; i < stompEnemies.Length; i++)
        {
            stompEnemies[i].enabled = false;
        }

        StompableAndroidStompZone[] stompZones = android.GetComponentsInChildren<StompableAndroidStompZone>(true);

        for (int i = 0; i < stompZones.Length; i++)
        {
            stompZones[i].enabled = false;
        }

        StompableAndroidSideHazard[] sideHazards = android.GetComponentsInChildren<StompableAndroidSideHazard>(true);

        for (int i = 0; i < sideHazards.Length; i++)
        {
            sideHazards[i].enabled = false;
        }

        EdgeRunnerEnemyMarker marker = android.GetComponent<EdgeRunnerEnemyMarker>();

        if (marker != null)
        {
            marker.SetAffectsAgent(false);
        }
    }

    private static GameObject CreatePlatformWithTop(Transform parent, string name, float centerX, float topY, Vector2 size, Sprite sprite)
    {
        GameObject platform = new GameObject(name);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, topY - size.y * 0.5f, 0f);
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.18f, 0.27f, 0.32f, 1f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;

        return platform;
    }

    private static void CreateDeathZone(string name, float centerX, float width)
    {
        GameObject deathZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefabPath);
        GameObject deathZone = deathZonePrefab != null
            ? PrefabUtility.InstantiatePrefab(deathZonePrefab) as GameObject
            : new GameObject(name);

        if (deathZone == null)
        {
            return;
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

    private static void CreateEvaluationManager(string name, GameObject player, string modelLabel, string evaluationLabel)
    {
        GameObject evaluationObject = new GameObject(name);
        EdgeRunnerEvaluationManager evaluationManager = evaluationObject.AddComponent<EdgeRunnerEvaluationManager>();
        SetBool(evaluationManager, "enableEvaluation", false);
        SetInt(evaluationManager, "targetEpisodes", evaluationLabel.Contains("100") ? 100 : 50);
        SetBool(evaluationManager, "stopPlayModeWhenFinished", true);
        SetString(evaluationManager, "modelLabel", modelLabel);
        SetString(evaluationManager, "evaluationLabel", evaluationLabel);
        SetObjectReference(evaluationManager, "agentV5", player.GetComponent<EdgeRunnerAgentV5>());
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(8f, 5.5f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.0f;
        camera.backgroundColor = new Color(0.055f, 0.08f, 0.13f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.AddComponent<AudioListener>();

        DemoCameraFollow2D follow = cameraObject.AddComponent<DemoCameraFollow2D>();
        follow.SetTarget(target);
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

    private static void SaveScene(Scene scene, string scenePath, string label)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 {label} scene: {scenePath}");
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

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"Serialized property '{propertyName}' not found on {target.name}.");
            return;
        }

        property.vector2Value = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
