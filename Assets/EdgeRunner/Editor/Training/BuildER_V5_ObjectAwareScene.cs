using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_ObjectAwareScene
{
    private const float LowCoinRunCoinY = 0.85f;
    private const float LowCoinRunHeightThreshold = 0.45f;
    private const float LowCoinRunJumpPenalty = -0.05f;
    private const float HighCoinJumpCoinY = 2.55f;
    private const float HighCoinJumpFirstCoinX = 3.5f;
    private const float HighCoinJumpSecondCoinX = 12.5f;
    private const float HighCoinJumpGoalX = 18f;
    private const float HighCoinJumpMinimumCoinSpacing = 8.5f;
    private const float HighCoinJumpSameJumpPenalty = -2f;
    private const float StaticAndroidAvoidAndroidX = 6.5f;
    private const float StaticAndroidAvoidGoalX = 13.5f;
    private const float StaticAndroidAvoidSideHitPenalty = -6f;
    private const float StaticAndroidAvoidStompReward = 0.5f;
    private const float StaticAndroidStompAndroidX = 6.5f;
    private const float StaticAndroidStompGoalX = 13.5f;
    private const float StaticAndroidStompReward = 5f;
    private const float StaticAndroidStompSideHitPenalty = -6f;
    private const float StaticAndroidStompMissedPenalty = -3f;
    private const float StaticAndroidStompLockedGoalPenalty = -2f;
    private const float StaticAndroidStompApproachReward = 0.01f;
    private const float StaticAndroidStompWindowReward = 0.05f;
    private const float StaticAndroidStompWindowRange = 3.5f;
    private const float StaticAndroidStompMissedForwardMargin = 2.5f;
    private const float MixedWarmupLowCoinX = 3.5f;
    private const float MixedWarmupLowCoinY = 0.85f;
    private const float MixedWarmupHighCoinX = 7f;
    private const float MixedWarmupHighCoinY = 2.55f;
    private const float MixedWarmupAndroidX = 11.5f;
    private const float MixedWarmupGoalX = 18f;
    private const float MixedRandomWarmupLowCoinMinX = 3f;
    private const float MixedRandomWarmupLowCoinMaxX = 4.2f;
    private const float MixedRandomWarmupHighCoinMinX = 8.5f;
    private const float MixedRandomWarmupHighCoinMaxX = 10.5f;
    private const float MixedRandomWarmupAndroidMinX = 14f;
    private const float MixedRandomWarmupAndroidMaxX = 16f;
    private const float MixedRandomWarmupGoalMinX = 20f;
    private const float MixedRandomWarmupGoalMaxX = 22f;
    private const float MixedRandomWarmupMinimumLowCoinHighCoinDistance = 4f;
    private const float MixedRandomWarmupMinimumHighCoinAndroidDistance = 3f;
    private const float FinalRandomFirstGapMin = 1.5f;
    private const float FinalRandomFirstGapMax = 2.2f;
    private const float FinalRandomSecondGapMin = 2f;
    private const float FinalRandomSecondGapMax = 2.8f;
    private const float FinalRandomMinimumLowHighDistance = 4f;
    private const float FinalRandomMinimumHighAndroidDistance = 3f;
    private const float FinalRandomMinimumAndroidGoalDistance = 3f;
    private const float FinalRandomPlayerStartX = 0f;
    private const float FinalRandomStartPlatformLeftX = -2f;
    private const float FinalRandomStartPlatformWidth = 15f;
    private const float FinalRandomRecoveryPlatformWidth = 6f;
    private const float FinalRandomHighPlatformWidth = 8f;
    private const float FinalRandomFinalPlatformWidth = 10f;
    private const float FinalRandomMinFlatRunBeforeLowCoin = 4f;
    private const float FinalRandomMinFlatRunAfterLowCoin = 2f;
    private const float FinalRandomMinLowCoinGapEdgeDistance = 3f;
    private const float FinalRandomMinLowCoinLandingZoneDistance = 3f;
    private const float FinalRandomLedgeStuckGraceTime = 0.5f;
    private const float FinalRandomLedgeStuckMinYBelowGround = 0.25f;
    private const float FinalRandomLedgeStuckMaxVelocity = 0.25f;
    private const float FinalRandomLedgeStuckProgressEpsilon = 0.03f;
    private const float FinalRandomLedgeStuckPenalty = -4f;

    private const string TraversalScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_TraversalBase.unity";
    private const string LowCoinRunScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_LowCoinRun.unity";
    private const string HighCoinJumpScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_HighCoinJump.unity";
    private const string StaticAndroidAvoidScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_StaticAndroidAvoid.unity";
    private const string StaticAndroidStompScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_StaticAndroidStomp.unity";
    private const string MixedWarmupScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_MixedWarmup.unity";
    private const string MixedRandomWarmupScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_MixedRandomWarmup.unity";
    private const string FinalRandomScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_FinalRandom.unity";
    private const string PlayerPrefabPath =
        "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath =
        "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidEnemyPrefabPath =
        "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";
    private const string AgentNoFrictionMaterialPath =
        "Assets/EdgeRunner/Physics/Agent_NoFriction.physicsMaterial2D";

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
        CreateCoin(root.transform, "LowCoinRun_Coin_01", new Vector3(3.5f, LowCoinRunCoinY, 0f), sprite, manager);
        CreateCoin(root.transform, "LowCoinRun_Coin_02", new Vector3(6f, LowCoinRunCoinY, 0f), sprite, manager);
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
        CreatePlatform(root.transform, "HighCoinJump_Platform", 9f, 0f, 24f, sprite);

        ScoreAttackManager manager = CreateCoinManager(root.transform);
        GameObject goal = CreateLockedGoal(
            "Goal_ScoreMaxOA_HighCoinJump",
            new Vector3(HighCoinJumpGoalX, 1.2f, 0f),
            manager);
        GameObject player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureCoinPhasePlayer(
            player,
            manager,
            EdgeRunnerObjectAwarePhase.HighCoinJump,
            false);
        CreateCoin(
            root.transform,
            "HighCoinJump_Coin_01",
            new Vector3(HighCoinJumpFirstCoinX, HighCoinJumpCoinY, 0f),
            sprite,
            manager);
        CreateCoin(
            root.transform,
            "HighCoinJump_Coin_02",
            new Vector3(HighCoinJumpSecondCoinX, HighCoinJumpCoinY, 0f),
            sprite,
            manager);
        CreateDeathZone(9f, 36f, "DeathZone_ScoreMaxOA_HighCoinJump");
        CreateCamera(player.transform);
        ValidateCoinPhase(scene, player, EdgeRunnerObjectAwarePhase.HighCoinJump, 2);
        SaveAndKeepOpen(scene, HighCoinJumpScenePath, player, "HighCoinJump");
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA StaticAndroidAvoid")]
    public static void BuildStaticAndroidAvoid()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildStaticAndroidAvoidContents(scene, out GameObject player);
        SaveAndKeepOpen(scene, StaticAndroidAvoidScenePath, player, "StaticAndroidAvoid");
    }

    private static void BuildStaticAndroidAvoidContents(Scene scene, out GameObject player)
    {
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_StaticAndroidAvoid");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "StaticAndroidAvoid_Platform", 7f, 0f, 18f, sprite);

        ScoreAttackManager manager = CreateStaticAndroidAvoidManager(root.transform);
        GameObject goal = CreateGoal(new Vector3(StaticAndroidAvoidGoalX, 1.2f, 0f));
        goal.name = "Goal_ScoreMaxOA_StaticAndroidAvoid";
        goal.transform.localScale = new Vector3(1.2f, 2.4f, 1f);
        SetObjectReference(manager, "goal", goal.transform);

        player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureStaticAndroidAvoidPlayer(player, manager);
        GameObject android = CreateStaticAndroid(
            root.transform,
            "StaticAndroidAvoid_Android_01",
            new Vector3(StaticAndroidAvoidAndroidX, 1.02f, 0f),
            sprite,
            manager);

        CreateDeathZone(7f, 26f, "DeathZone_ScoreMaxOA_StaticAndroidAvoid");
        CreateCamera(player.transform);
        ValidateStaticAndroidAvoid(scene, player, manager, android, goal);
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA StaticAndroidStomp")]
    public static void BuildStaticAndroidStomp()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildStaticAndroidStompContents(scene, out GameObject player);
        SaveAndKeepOpen(scene, StaticAndroidStompScenePath, player, "StaticAndroidStomp");
    }

    private static void BuildStaticAndroidStompContents(Scene scene, out GameObject player)
    {
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_StaticAndroidStomp");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "StaticAndroidStomp_Platform", 7f, 0f, 18f, sprite);

        ScoreAttackManager manager = CreateStaticAndroidStompManager(root.transform);
        GameObject goal = CreateLockedGoal(
            "Goal_ScoreMaxOA_StaticAndroidStomp",
            new Vector3(StaticAndroidStompGoalX, 1.2f, 0f),
            manager);
        player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureStaticAndroidStompPlayer(player, manager);
        GameObject android = CreateStaticAndroid(
            root.transform,
            "StaticAndroidStomp_Android_01",
            new Vector3(StaticAndroidStompAndroidX, 1.02f, 0f),
            sprite,
            manager);

        CreateDeathZone(7f, 26f, "DeathZone_ScoreMaxOA_StaticAndroidStomp");
        CreateCamera(player.transform);
        ValidateStaticAndroidStomp(scene, player, manager, android, goal);
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA MixedWarmup")]
    public static void BuildMixedWarmup()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildMixedWarmupContents(scene, out GameObject player);
        SaveAndKeepOpen(scene, MixedWarmupScenePath, player, "MixedWarmup");
    }

    private static void BuildMixedWarmupContents(Scene scene, out GameObject player)
    {
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_MixedWarmup");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "MixedWarmup_Platform", 9f, 0f, 22f, sprite);

        ScoreAttackManager manager = CreateMixedWarmupManager(root.transform);
        GameObject goal = CreateLockedGoal(
            "Goal_ScoreMaxOA_MixedWarmup",
            new Vector3(MixedWarmupGoalX, 1.2f, 0f),
            manager);
        player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureMixedWarmupPlayer(player, manager);
        CreateCoin(
            root.transform,
            "MixedWarmup_LowCoin",
            new Vector3(MixedWarmupLowCoinX, MixedWarmupLowCoinY, 0f),
            sprite,
            manager);
        CreateCoin(
            root.transform,
            "MixedWarmup_HighCoin",
            new Vector3(MixedWarmupHighCoinX, MixedWarmupHighCoinY, 0f),
            sprite,
            manager);
        GameObject android = CreateStaticAndroid(
            root.transform,
            "MixedWarmup_Android_01",
            new Vector3(MixedWarmupAndroidX, 1.02f, 0f),
            sprite,
            manager);

        CreateDeathZone(9f, 30f, "DeathZone_ScoreMaxOA_MixedWarmup");
        CreateCamera(player.transform);
        ValidateMixedWarmup(scene, player, manager, android, goal);
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA MixedRandomWarmup")]
    public static void BuildMixedRandomWarmup()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildMixedRandomWarmupContents(scene, out GameObject player);
        SaveAndKeepOpen(scene, MixedRandomWarmupScenePath, player, "MixedRandomWarmup");
    }

    private static void BuildMixedRandomWarmupContents(Scene scene, out GameObject player)
    {
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_MixedRandomWarmup");
        Sprite sprite = GetSharedSprite();
        CreatePlatform(root.transform, "MixedRandomWarmup_Platform", 11f, 0f, 28f, sprite);

        ScoreAttackManager manager = CreateMixedRandomWarmupManager(root.transform);
        GameObject goal = CreateLockedGoal(
            "Goal_ScoreMaxOA_MixedRandomWarmup",
            new Vector3(21f, 1.2f, 0f),
            manager);
        player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);

        ScoreAttackCoin lowCoin = CreateCoin(
            root.transform,
            "MixedRandomWarmup_LowCoin",
            new Vector3(3.6f, MixedWarmupLowCoinY, 0f),
            sprite,
            manager);
        ScoreAttackCoin highCoin = CreateCoin(
            root.transform,
            "MixedRandomWarmup_HighCoin",
            new Vector3(9.5f, MixedWarmupHighCoinY, 0f),
            sprite,
            manager);
        GameObject android = CreateStaticAndroid(
            root.transform,
            "MixedRandomWarmup_Android_01",
            new Vector3(15f, 1.02f, 0f),
            sprite,
            manager);

        ScoreMaxOAMixedRandomWarmupRandomizer randomizer =
            manager.gameObject.AddComponent<ScoreMaxOAMixedRandomWarmupRandomizer>();
        ConfigureMixedRandomWarmupRandomizer(
            randomizer,
            lowCoin,
            highCoin,
            android.GetComponent<ScoreAttackAndroid>(),
            goal.transform);
        ConfigureMixedRandomWarmupPlayer(player, manager, randomizer);

        CreateDeathZone(11f, 36f, "DeathZone_ScoreMaxOA_MixedRandomWarmup");
        CreateCamera(player.transform);
        ValidateMixedRandomWarmup(scene, player, manager, randomizer, android, goal);
    }

    [MenuItem("EdgeRunner/Training/ObjectAware/Build ScoreMaxOA FinalRandom")]
    public static void BuildFinalRandom()
    {
        if (!CanReplaceOpenScenes())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildFinalRandomContents(scene, out GameObject player);
        SaveAndKeepOpen(scene, FinalRandomScenePath, player, "FinalRandom");
    }

    private static void BuildFinalRandomContents(Scene scene, out GameObject player)
    {
        GameObject root = new GameObject("ER_V5_ScoreMaxOA_FinalRandom");
        Sprite sprite = GetSharedSprite();

        Transform startPlatform =
            CreatePlatform(root.transform, "FinalRandom_StartSafeLow", 5.5f, 0f, 15f, sprite);
        Transform lowPlatform =
            CreatePlatform(root.transform, "FinalRandom_Recovery_A", 17.8f, 0f, 6f, sprite);
        Transform highPlatform =
            CreatePlatform(root.transform, "FinalRandom_HighRecovery", 27.2f, 0f, 8f, sprite);
        Transform finalPlatform =
            CreatePlatform(root.transform, "FinalRandom_AndroidGoal", 35.7f, 0f, 10f, sprite);

        ScoreAttackManager manager = CreateFinalRandomManager(root.transform);
        GameObject goal = CreateLockedGoal(
            "Goal_ScoreMaxOA_FinalRandom",
            new Vector3(39.2f, 1.2f, 0f),
            manager);
        player = CreateObjectAwarePlayer(new Vector3(0f, 1.15f, 0f), goal.transform);

        ScoreAttackCoin[] lowCoins =
        {
            CreateCoin(
                root.transform,
                "FinalRandom_LowCoin_01",
                new Vector3(5f, MixedWarmupLowCoinY, 0f),
                sprite,
                manager),
            CreateCoin(
                root.transform,
                "FinalRandom_LowCoin_02",
                new Vector3(9f, MixedWarmupLowCoinY, 0f),
                sprite,
                manager)
        };
        ScoreAttackCoin[] highCoins =
        {
            CreateCoin(
                root.transform,
                "FinalRandom_HighCoin_01",
                new Vector3(25f, MixedWarmupHighCoinY, 0f),
                sprite,
                manager),
            CreateCoin(
                root.transform,
                "FinalRandom_HighCoin_02",
                new Vector3(29f, MixedWarmupHighCoinY, 0f),
                sprite,
                manager)
        };
        GameObject android = CreateStaticAndroid(
            root.transform,
            "FinalRandom_Android_01",
            new Vector3(35.2f, 1.02f, 0f),
            sprite,
            manager);

        ScoreMaxOAFinalRandomizer randomizer =
            manager.gameObject.AddComponent<ScoreMaxOAFinalRandomizer>();
        ConfigureFinalRandomizer(
            randomizer,
            manager,
            lowCoins,
            highCoins,
            android.GetComponent<ScoreAttackAndroid>(),
            goal.transform,
            startPlatform,
            lowPlatform,
            highPlatform,
            finalPlatform);
        ConfigureFinalRandomPlayer(player, manager, randomizer);

        CreateDeathZone(20f, 60f, "DeathZone_ScoreMaxOA_FinalRandom");
        CreateCamera(player.transform);
        ValidateFinalRandom(scene, player, manager, randomizer, android, goal);
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
        SetBool(agent, "enforceLowCoinRunGroundCollection", false);
        SetBool(agent, "requireGroundedBetweenHighCoins", false);
        SetFloat(agent, "sameJumpSecondCoinPenalty", HighCoinJumpSameJumpPenalty);
        SetBool(agent, "endEpisodeOnSameJumpSecondCoin", false);

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

        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty enforceGroundCollection = serializedAgent.FindProperty(
            "enforceLowCoinRunGroundCollection");
        SerializedProperty requireHighCoinLanding = serializedAgent.FindProperty(
            "requireGroundedBetweenHighCoins");
        SerializedProperty sameJumpPenalty = serializedAgent.FindProperty(
            "sameJumpSecondCoinPenalty");
        SerializedProperty endOnSameJump = serializedAgent.FindProperty(
            "endEpisodeOnSameJumpSecondCoin");
        bool isLowCoinRun = expectedPhase == EdgeRunnerObjectAwarePhase.LowCoinRun;
        bool isHighCoinJump = expectedPhase == EdgeRunnerObjectAwarePhase.HighCoinJump;
        if (contextualJumpMask == null ||
            contextualJumpMask.boolValue != isLowCoinRun ||
            enforceGroundCollection == null ||
            enforceGroundCollection.boolValue != isLowCoinRun ||
            requireHighCoinLanding == null ||
            requireHighCoinLanding.boolValue != isHighCoinJump ||
            sameJumpPenalty == null ||
            Mathf.Abs(sameJumpPenalty.floatValue - HighCoinJumpSameJumpPenalty) > 0.0001f ||
            endOnSameJump == null ||
            endOnSameJump.boolValue != isHighCoinJump)
        {
            throw new System.InvalidOperationException(
                $"{phaseName} has the wrong coin-phase curriculum flags.");
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

        if (isLowCoinRun)
        {
            ValidateLowCoinRunConfiguration(scene, player, agent);
        }
        else if (expectedPhase == EdgeRunnerObjectAwarePhase.HighCoinJump)
        {
            ValidateHighCoinJumpConfiguration(scene, player, agent);
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateLowCoinRunConfiguration(
        Scene scene,
        GameObject player,
        EdgeRunnerAgentV5ScoreMaxObjectAware agent)
    {
        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty threshold = serializedAgent.FindProperty(
            "lowCoinHeightThreshold");
        SerializedProperty jumpPenalty = serializedAgent.FindProperty(
            "lowCoinUnnecessaryJumpPenalty");

        if (rewardShaping == null || !rewardShaping.boolValue ||
            missedCoinEnd == null || !missedCoinEnd.boolValue ||
            threshold == null ||
            Mathf.Abs(threshold.floatValue - LowCoinRunHeightThreshold) > 0.0001f ||
            jumpPenalty == null || jumpPenalty.floatValue > -0.03f)
        {
            throw new System.InvalidOperationException(
                "LowCoinRun requires reward shaping, missed-coin reset, threshold 0.45, " +
                "and an unnecessary-jump penalty of at least -0.03.");
        }

        ScoreAttackCoin[] coins = GetSceneComponents<ScoreAttackCoin>(scene);
        ScoreAttackManager[] managers = GetSceneComponents<ScoreAttackManager>(scene);
        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        BoxCollider2D platformCollider = null;
        BoxCollider2D[] colliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.name == "LowCoinRun_Platform")
            {
                platformCollider = colliders[i];
                break;
            }
        }

        if (coins.Length != 2 || managers.Length != 1 || goalLocks.Length != 1 ||
            playerCollider == null || platformCollider == null)
        {
            throw new System.InvalidOperationException(
                "LowCoinRun requires two coins, one manager, one GoalLock, " +
                "the player collider, and one continuous LowCoinRun platform.");
        }

        Physics2D.SyncTransforms();
        float playerScaleY = Mathf.Abs(player.transform.lossyScale.y);
        float playerHalfHeight = playerCollider.size.y * playerScaleY * 0.5f;
        float playerGroundedCenterY =
            platformCollider.bounds.max.y + playerHalfHeight -
            playerCollider.offset.y * playerScaleY;
        float playerGroundedTopY = playerGroundedCenterY + playerHalfHeight;
        ScoreAttackCoin firstCoin = null;
        ScoreAttackCoin secondCoin = null;

        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            CircleCollider2D coinCollider = coin.GetComponent<CircleCollider2D>();
            float dy = coin.transform.position.y - playerGroundedCenterY;
            float coinRadius = coinCollider != null
                ? coinCollider.radius * Mathf.Abs(coin.transform.lossyScale.y)
                : float.PositiveInfinity;
            SerializedObject serializedCoin = new SerializedObject(coin);
            SerializedProperty coinManager = serializedCoin.FindProperty("manager");

            if (dy > threshold.floatValue + 0.0001f ||
                coinCollider == null ||
                coin.transform.position.y - coinRadius > playerGroundedTopY + 0.0001f ||
                coinManager == null || coinManager.objectReferenceValue != managers[0])
            {
                throw new System.InvalidOperationException(
                    $"{coin.name} is not a run-collectable low coin: " +
                    $"grounded-center dy={dy:F3}, threshold={threshold.floatValue:F3}.");
            }

            if (coin.name == "LowCoinRun_Coin_01")
            {
                firstCoin = coin;
            }
            else if (coin.name == "LowCoinRun_Coin_02")
            {
                secondCoin = coin;
            }
        }

        ScoreAttackGoalLock goalLock = goalLocks[0];
        SerializedObject serializedGoalLock = new SerializedObject(goalLock);
        SerializedProperty goalManager = serializedGoalLock.FindProperty("manager");
        SerializedObject serializedManager = new SerializedObject(managers[0]);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty managerGoal = serializedManager.FindProperty("goal");

        if (firstCoin == null || secondCoin == null ||
            firstCoin.transform.position.x >= secondCoin.transform.position.x ||
            !goalLock.enabled || goalManager == null ||
            goalManager.objectReferenceValue != managers[0] ||
            managerAgent == null || managerAgent.objectReferenceValue != agent ||
            managerGoal == null || managerGoal.objectReferenceValue != goalLock.transform)
        {
            throw new System.InvalidOperationException(
                "LowCoinRun must select Coin_01 before Coin_02 and keep its GoalLock " +
                "connected to the ObjectAware manager until both coins are collected.");
        }

        float pathMinX = Mathf.Min(player.transform.position.x, firstCoin.transform.position.x);
        float pathMaxX = Mathf.Max(goalLock.transform.position.x, secondCoin.transform.position.x);
        if (platformCollider.bounds.min.x > pathMinX ||
            platformCollider.bounds.max.x < pathMaxX)
        {
            throw new System.InvalidOperationException(
                "LowCoinRun strict no-jump mode requires one continuous platform " +
                "from spawn through both coins and the Goal.");
        }
    }

    private static void ValidateHighCoinJumpConfiguration(
        Scene scene,
        GameObject player,
        EdgeRunnerAgentV5ScoreMaxObjectAware agent)
    {
        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty threshold = serializedAgent.FindProperty(
            "lowCoinHeightThreshold");
        SerializedProperty jumpWindow = serializedAgent.FindProperty(
            "highCoinJumpWindowX");
        SerializedProperty jumpForce = serializedAgent.FindProperty("jumpForce");
        ScoreAttackCoin[] coins = GetSceneComponents<ScoreAttackCoin>(scene);
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        BoxCollider2D platformCollider = null;
        BoxCollider2D[] colliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.name == "HighCoinJump_Platform")
            {
                platformCollider = colliders[i];
                break;
            }
        }

        if (threshold == null || jumpWindow == null || jumpForce == null ||
            coins.Length != 2 || playerCollider == null || playerBody == null ||
            platformCollider == null)
        {
            throw new System.InvalidOperationException(
                "HighCoinJump is missing its classification settings, two coins, " +
                "player physics, or continuous platform.");
        }

        Physics2D.SyncTransforms();
        float playerScaleY = Mathf.Abs(player.transform.lossyScale.y);
        float playerHalfHeight = playerCollider.size.y * playerScaleY * 0.5f;
        float playerGroundedCenterY =
            platformCollider.bounds.max.y + playerHalfHeight -
            playerCollider.offset.y * playerScaleY;
        float effectiveGravity =
            Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, playerBody.gravityScale);
        float maximumJumpCenterY =
            playerGroundedCenterY +
            jumpForce.floatValue * jumpForce.floatValue / (2f * effectiveGravity);
        ScoreAttackCoin firstCoin = null;
        ScoreAttackCoin secondCoin = null;

        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            CircleCollider2D coinCollider = coin.GetComponent<CircleCollider2D>();
            float dy = coin.transform.position.y - playerGroundedCenterY;
            float coinRadius = coinCollider != null
                ? coinCollider.radius * Mathf.Abs(coin.transform.lossyScale.y)
                : 0f;
            float minimumPlayerCenterForCollection =
                coin.transform.position.y - playerHalfHeight - coinRadius;

            if (dy <= threshold.floatValue ||
                coinCollider == null ||
                minimumPlayerCenterForCollection > maximumJumpCenterY)
            {
                throw new System.InvalidOperationException(
                    $"{coin.name} must remain high but reachable with the normal jump: " +
                    $"grounded-center dy={dy:F3}, threshold={threshold.floatValue:F3}.");
            }

            if (coin.name == "HighCoinJump_Coin_01")
            {
                firstCoin = coin;
            }
            else if (coin.name == "HighCoinJump_Coin_02")
            {
                secondCoin = coin;
            }
        }

        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        if (firstCoin == null || secondCoin == null || goalLocks.Length != 1)
        {
            throw new System.InvalidOperationException(
                "HighCoinJump requires named Coin_01/Coin_02 and one GoalLock.");
        }

        float coinSpacing = secondCoin.transform.position.x - firstCoin.transform.position.x;
        float firstCoinDistance = firstCoin.transform.position.x - player.transform.position.x;
        float pathMinX = Mathf.Min(player.transform.position.x, firstCoin.transform.position.x);
        float pathMaxX = Mathf.Max(goalLocks[0].transform.position.x, secondCoin.transform.position.x);
        if (coinSpacing < HighCoinJumpMinimumCoinSpacing ||
            firstCoinDistance <= jumpWindow.floatValue ||
            jumpWindow.floatValue <= 0f ||
            jumpWindow.floatValue >= coinSpacing ||
            goalLocks[0].transform.position.x <= secondCoin.transform.position.x ||
            platformCollider.bounds.min.x > pathMinX ||
            platformCollider.bounds.max.x < pathMaxX)
        {
            throw new System.InvalidOperationException(
                "HighCoinJump must require two separate jump windows on one safe platform, " +
                "followed by a Goal beyond Coin_02.");
        }
    }

    private static void ValidateStaticAndroidAvoid(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        GameObject android,
        GameObject goal)
    {
        const string phaseName = "StaticAndroidAvoid";
        ValidateObjectAwarePlayer(player, phaseName);

        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            CountSceneComponents<ScoreAttackCoin>(scene) != 0 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 1 ||
            CountSceneComponents<ScoreAttackGoalLock>(scene) != 0 ||
            CountSceneComponents<DemoAndroidPatrol>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidAvoid requires one static Android, zero coins, one manager, " +
                "no patrol, and no GoalLock.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        ScoreAttackAndroid androidComponent = android != null
            ? android.GetComponent<ScoreAttackAndroid>()
            : null;
        Rigidbody2D androidBody = android != null ? android.GetComponent<Rigidbody2D>() : null;
        Collider2D androidCollider = android != null ? android.GetComponent<Collider2D>() : null;
        BoxCollider2D platform = null;
        BoxCollider2D[] colliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.name == "StaticAndroidAvoid_Platform")
            {
                platform = colliders[i];
                break;
            }
        }

        if (agent == null || manager == null || androidComponent == null ||
            androidBody == null || androidBody.bodyType != RigidbodyType2D.Kinematic ||
            androidCollider == null || !androidCollider.isTrigger || platform == null ||
            goal == null)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidAvoid is missing its agent, static Android physics, platform, or Goal.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty assignedManager = serializedAgent.FindProperty("scoreAttackManager");
        SerializedProperty objectAwareGoal = serializedAgent.FindProperty("objectAwareGoal");
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty debugObservationCount = serializedAgent.FindProperty(
            "debugObjectAwareObservationCount");
        SerializedProperty debugNextObjective = serializedAgent.FindProperty(
            "debugObjectAwareNextObjective");
        SerializedProperty debugJumpContext = serializedAgent.FindProperty(
            "debugObjectAwareJumpContext");
        SerializedProperty debugGizmos = serializedAgent.FindProperty(
            "debugObjectAwareGizmos");

        if (phase == null ||
            phase.enumValueIndex != (int)EdgeRunnerObjectAwarePhase.StaticAndroidAvoid ||
            assignedManager == null || assignedManager.objectReferenceValue != manager ||
            objectAwareGoal == null || objectAwareGoal.objectReferenceValue != goal.transform ||
            rewardShaping == null || rewardShaping.boolValue ||
            missedCoinEnd == null || missedCoinEnd.boolValue ||
            contextualJumpMask == null || contextualJumpMask.boolValue ||
            debugObservationCount == null || debugObservationCount.boolValue ||
            debugNextObjective == null || debugNextObjective.boolValue ||
            debugJumpContext == null || debugJumpContext.boolValue ||
            debugGizmos == null || debugGizmos.boolValue)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidAvoid has the wrong ObjectAware phase, references, or debug flags.");
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty requireEnemies = serializedManager.FindProperty(
            "requireEnemiesForGoal");
        SerializedProperty randomize = serializedManager.FindProperty(
            "randomizeObjectPositionsOnReset");
        SerializedProperty minCoins = serializedManager.FindProperty("minActiveCoins");
        SerializedProperty maxCoins = serializedManager.FindProperty("maxActiveCoins");
        SerializedProperty minEnemies = serializedManager.FindProperty("minActiveEnemies");
        SerializedProperty maxEnemies = serializedManager.FindProperty("maxActiveEnemies");
        SerializedProperty stompReward = serializedManager.FindProperty("enemyKillReward");
        SerializedProperty sideHitPenalty = serializedManager.FindProperty("enemySideHitPenalty");
        SerializedProperty finalReward = serializedManager.FindProperty("finalCompletionReward");

        if (managerAgent == null || managerAgent.objectReferenceValue != agent ||
            requireEnemies == null || requireEnemies.boolValue ||
            randomize == null || randomize.boolValue ||
            minCoins == null || minCoins.intValue != 0 ||
            maxCoins == null || maxCoins.intValue != 0 ||
            minEnemies == null || minEnemies.intValue != 1 ||
            maxEnemies == null || maxEnemies.intValue != 1 ||
            stompReward == null ||
            Mathf.Abs(stompReward.floatValue - StaticAndroidAvoidStompReward) > 0.0001f ||
            sideHitPenalty == null ||
            Mathf.Abs(sideHitPenalty.floatValue - StaticAndroidAvoidSideHitPenalty) > 0.0001f ||
            finalReward == null || Mathf.Abs(finalReward.floatValue) > 0.0001f)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidAvoid manager must reset one optional Android with stomp=0.5, " +
                "sideHit=-6, and no manager Goal reward.");
        }

        SerializedObject serializedAndroid = new SerializedObject(androidComponent);
        SerializedProperty androidManager = serializedAndroid.FindProperty("manager");
        float pathMinX = Mathf.Min(player.transform.position.x, android.transform.position.x);
        float pathMaxX = Mathf.Max(goal.transform.position.x, android.transform.position.x);
        bool layoutValid =
            Mathf.Abs(android.transform.position.x - StaticAndroidAvoidAndroidX) <= 0.0001f &&
            Mathf.Abs(goal.transform.position.x - StaticAndroidAvoidGoalX) <= 0.0001f &&
            player.transform.position.x < android.transform.position.x &&
            android.transform.position.x < goal.transform.position.x &&
            platform.bounds.min.x <= pathMinX &&
            platform.bounds.max.x >= pathMaxX;

        if (androidManager == null || androidManager.objectReferenceValue != manager || !layoutValid)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidAvoid Android/Goal ordering or manager reference is invalid.");
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateStaticAndroidStomp(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        GameObject android,
        GameObject goal)
    {
        const string phaseName = "StaticAndroidStomp";
        ValidateObjectAwarePlayer(player, phaseName);

        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            CountSceneComponents<ScoreAttackCoin>(scene) != 0 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 1 ||
            goalLocks.Length != 1 ||
            CountSceneComponents<DemoAndroidPatrol>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidStomp requires one static Android, zero coins, one manager, " +
                "one GoalLock, and no patrol.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        ScoreAttackAndroid androidComponent = android != null
            ? android.GetComponent<ScoreAttackAndroid>()
            : null;
        Rigidbody2D androidBody = android != null ? android.GetComponent<Rigidbody2D>() : null;
        Collider2D androidCollider = android != null ? android.GetComponent<Collider2D>() : null;
        BoxCollider2D platform = null;
        BoxCollider2D[] colliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject.name == "StaticAndroidStomp_Platform")
            {
                platform = colliders[i];
                break;
            }
        }

        if (agent == null || manager == null || androidComponent == null ||
            androidBody == null || androidBody.bodyType != RigidbodyType2D.Kinematic ||
            androidCollider == null || !androidCollider.isTrigger || platform == null ||
            goal == null)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidStomp is missing its agent, static Android physics, platform, or Goal.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty assignedManager = serializedAgent.FindProperty("scoreAttackManager");
        SerializedProperty objectAwareGoal = serializedAgent.FindProperty("objectAwareGoal");
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty approachReward = serializedAgent.FindProperty("enemyApproachReward");
        SerializedProperty stompWindowReward = serializedAgent.FindProperty(
            "enemyStompWindowReward");
        SerializedProperty stompWindowRange = serializedAgent.FindProperty(
            "enemyStompWindowHorizontalRange");
        SerializedProperty missedEnemyPenalty = serializedAgent.FindProperty(
            "missedEnemyPenalty");
        SerializedProperty missedEnemyMargin = serializedAgent.FindProperty(
            "missedEnemyForwardMargin");
        SerializedProperty endOnMissedEnemy = serializedAgent.FindProperty(
            "endEpisodeOnMissedEnemy");
        SerializedProperty jumpSpam = serializedAgent.FindProperty("jumpSpamPenalty");
        SerializedProperty debugObservationCount = serializedAgent.FindProperty(
            "debugObjectAwareObservationCount");
        SerializedProperty debugNextObjective = serializedAgent.FindProperty(
            "debugObjectAwareNextObjective");
        SerializedProperty debugJumpContext = serializedAgent.FindProperty(
            "debugObjectAwareJumpContext");
        SerializedProperty debugGizmos = serializedAgent.FindProperty(
            "debugObjectAwareGizmos");

        if (phase == null ||
            phase.enumValueIndex != (int)EdgeRunnerObjectAwarePhase.StaticAndroidStomp ||
            assignedManager == null || assignedManager.objectReferenceValue != manager ||
            objectAwareGoal == null || objectAwareGoal.objectReferenceValue != goal.transform ||
            rewardShaping == null || !rewardShaping.boolValue ||
            missedCoinEnd == null || missedCoinEnd.boolValue ||
            contextualJumpMask == null || contextualJumpMask.boolValue ||
            approachReward == null ||
            Mathf.Abs(approachReward.floatValue - StaticAndroidStompApproachReward) > 0.0001f ||
            stompWindowReward == null ||
            Mathf.Abs(stompWindowReward.floatValue - StaticAndroidStompWindowReward) > 0.0001f ||
            stompWindowRange == null ||
            Mathf.Abs(stompWindowRange.floatValue - StaticAndroidStompWindowRange) > 0.0001f ||
            missedEnemyPenalty == null ||
            Mathf.Abs(missedEnemyPenalty.floatValue - StaticAndroidStompMissedPenalty) > 0.0001f ||
            missedEnemyMargin == null ||
            Mathf.Abs(missedEnemyMargin.floatValue - StaticAndroidStompMissedForwardMargin) > 0.0001f ||
            endOnMissedEnemy == null || !endOnMissedEnemy.boolValue ||
            jumpSpam == null || Mathf.Abs(jumpSpam.floatValue + 0.01f) > 0.0001f ||
            debugObservationCount == null || debugObservationCount.boolValue ||
            debugNextObjective == null || debugNextObjective.boolValue ||
            debugJumpContext == null || debugJumpContext.boolValue ||
            debugGizmos == null || debugGizmos.boolValue)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidStomp has the wrong phase, shaping, missed-enemy rule, or debug flags.");
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty requireEnemies = serializedManager.FindProperty(
            "requireEnemiesForGoal");
        SerializedProperty randomize = serializedManager.FindProperty(
            "randomizeObjectPositionsOnReset");
        SerializedProperty minCoins = serializedManager.FindProperty("minActiveCoins");
        SerializedProperty maxCoins = serializedManager.FindProperty("maxActiveCoins");
        SerializedProperty minEnemies = serializedManager.FindProperty("minActiveEnemies");
        SerializedProperty maxEnemies = serializedManager.FindProperty("maxActiveEnemies");
        SerializedProperty stompReward = serializedManager.FindProperty("enemyKillReward");
        SerializedProperty sideHitPenalty = serializedManager.FindProperty("enemySideHitPenalty");
        SerializedProperty finalReward = serializedManager.FindProperty("finalCompletionReward");
        SerializedProperty lockedGoalPenalty = serializedManager.FindProperty(
            "prematureGoalPenalty");
        SerializedProperty endOnLockedGoal = serializedManager.FindProperty(
            "endEpisodeOnPrematureGoal");

        if (managerAgent == null || managerAgent.objectReferenceValue != agent ||
            requireEnemies == null || !requireEnemies.boolValue ||
            randomize == null || randomize.boolValue ||
            minCoins == null || minCoins.intValue != 0 ||
            maxCoins == null || maxCoins.intValue != 0 ||
            minEnemies == null || minEnemies.intValue != 1 ||
            maxEnemies == null || maxEnemies.intValue != 1 ||
            stompReward == null ||
            Mathf.Abs(stompReward.floatValue - StaticAndroidStompReward) > 0.0001f ||
            sideHitPenalty == null ||
            Mathf.Abs(sideHitPenalty.floatValue - StaticAndroidStompSideHitPenalty) > 0.0001f ||
            finalReward == null || Mathf.Abs(finalReward.floatValue - 10f) > 0.0001f ||
            lockedGoalPenalty == null ||
            Mathf.Abs(lockedGoalPenalty.floatValue - StaticAndroidStompLockedGoalPenalty) > 0.0001f ||
            endOnLockedGoal == null || !endOnLockedGoal.boolValue)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidStomp manager must require the Android, reward stomp=5, " +
                "penalize sideHit=-6/lockedGoal=-2, and end on locked Goal.");
        }

        SerializedObject serializedAndroid = new SerializedObject(androidComponent);
        SerializedProperty androidManager = serializedAndroid.FindProperty("manager");
        SerializedObject serializedGoalLock = new SerializedObject(goalLocks[0]);
        SerializedProperty goalLockManager = serializedGoalLock.FindProperty("manager");
        float pathMinX = Mathf.Min(player.transform.position.x, android.transform.position.x);
        float pathMaxX = Mathf.Max(goal.transform.position.x, android.transform.position.x);
        bool layoutValid =
            Mathf.Abs(android.transform.position.x - StaticAndroidStompAndroidX) <= 0.0001f &&
            Mathf.Abs(goal.transform.position.x - StaticAndroidStompGoalX) <= 0.0001f &&
            player.transform.position.x < android.transform.position.x &&
            android.transform.position.x < goal.transform.position.x &&
            platform.bounds.min.x <= pathMinX &&
            platform.bounds.max.x >= pathMaxX;

        if (androidManager == null || androidManager.objectReferenceValue != manager ||
            goalLockManager == null || goalLockManager.objectReferenceValue != manager ||
            !layoutValid)
        {
            throw new System.InvalidOperationException(
                "StaticAndroidStomp Android/Goal ordering or manager references are invalid.");
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateMixedWarmup(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        GameObject android,
        GameObject goal)
    {
        const string phaseName = "MixedWarmup";
        ValidateObjectAwarePlayer(player, phaseName);

        ScoreAttackCoin[] coins = GetSceneComponents<ScoreAttackCoin>(scene);
        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            coins.Length != 2 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 1 ||
            goalLocks.Length != 1 ||
            CountSceneComponents<DemoAndroidPatrol>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                "MixedWarmup requires two coins, one static Android, one manager, " +
                "one GoalLock, and no patrol.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        ScoreAttackAndroid androidComponent = android != null
            ? android.GetComponent<ScoreAttackAndroid>()
            : null;
        Rigidbody2D androidBody = android != null ? android.GetComponent<Rigidbody2D>() : null;
        Collider2D androidCollider = android != null ? android.GetComponent<Collider2D>() : null;
        BoxCollider2D platform = null;
        BoxCollider2D[] boxColliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < boxColliders.Length; i++)
        {
            if (boxColliders[i].gameObject.name == "MixedWarmup_Platform")
            {
                platform = boxColliders[i];
                break;
            }
        }

        if (agent == null || playerCollider == null || playerBody == null ||
            manager == null || androidComponent == null || androidBody == null ||
            androidBody.bodyType != RigidbodyType2D.Kinematic ||
            androidCollider == null || !androidCollider.isTrigger || platform == null ||
            goal == null)
        {
            throw new System.InvalidOperationException(
                "MixedWarmup is missing its agent, physics, static Android, platform, or Goal.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty assignedManager = serializedAgent.FindProperty("scoreAttackManager");
        SerializedProperty objectAwareGoal = serializedAgent.FindProperty("objectAwareGoal");
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty enforceLowGroundCollection = serializedAgent.FindProperty(
            "enforceLowCoinRunGroundCollection");
        SerializedProperty requireHighLanding = serializedAgent.FindProperty(
            "requireGroundedBetweenHighCoins");
        SerializedProperty threshold = serializedAgent.FindProperty("lowCoinHeightThreshold");
        SerializedProperty jumpForce = serializedAgent.FindProperty("jumpForce");
        SerializedProperty lowJumpPenalty = serializedAgent.FindProperty(
            "lowCoinUnnecessaryJumpPenalty");
        SerializedProperty missedCoinPenalty = serializedAgent.FindProperty("missedCoinPenalty");
        SerializedProperty missedEnemyPenalty = serializedAgent.FindProperty("missedEnemyPenalty");
        SerializedProperty endOnMissedEnemy = serializedAgent.FindProperty(
            "endEpisodeOnMissedEnemy");
        SerializedProperty debugObservationCount = serializedAgent.FindProperty(
            "debugObjectAwareObservationCount");
        SerializedProperty debugNextObjective = serializedAgent.FindProperty(
            "debugObjectAwareNextObjective");
        SerializedProperty debugJumpContext = serializedAgent.FindProperty(
            "debugObjectAwareJumpContext");
        SerializedProperty debugGizmos = serializedAgent.FindProperty(
            "debugObjectAwareGizmos");

        if (phase == null || phase.enumValueIndex != (int)EdgeRunnerObjectAwarePhase.MixedWarmup ||
            assignedManager == null || assignedManager.objectReferenceValue != manager ||
            objectAwareGoal == null || objectAwareGoal.objectReferenceValue != goal.transform ||
            rewardShaping == null || !rewardShaping.boolValue ||
            missedCoinEnd == null || !missedCoinEnd.boolValue ||
            contextualJumpMask == null || !contextualJumpMask.boolValue ||
            enforceLowGroundCollection == null || enforceLowGroundCollection.boolValue ||
            requireHighLanding == null || requireHighLanding.boolValue ||
            threshold == null ||
            Mathf.Abs(threshold.floatValue - LowCoinRunHeightThreshold) > 0.0001f ||
            jumpForce == null ||
            lowJumpPenalty == null ||
            Mathf.Abs(lowJumpPenalty.floatValue - LowCoinRunJumpPenalty) > 0.0001f ||
            missedCoinPenalty == null || Mathf.Abs(missedCoinPenalty.floatValue + 2f) > 0.0001f ||
            missedEnemyPenalty == null ||
            Mathf.Abs(missedEnemyPenalty.floatValue - StaticAndroidStompMissedPenalty) > 0.0001f ||
            endOnMissedEnemy == null || !endOnMissedEnemy.boolValue ||
            debugObservationCount == null || debugObservationCount.boolValue ||
            debugNextObjective == null || debugNextObjective.boolValue ||
            debugJumpContext == null || debugJumpContext.boolValue ||
            debugGizmos == null || debugGizmos.boolValue)
        {
            throw new System.InvalidOperationException(
                "MixedWarmup has the wrong phase, contextual mask, shaping, or debug flags.");
        }

        Physics2D.SyncTransforms();
        float playerScaleY = Mathf.Abs(player.transform.lossyScale.y);
        float playerHalfHeight = playerCollider.size.y * playerScaleY * 0.5f;
        float playerGroundedCenterY =
            platform.bounds.max.y + playerHalfHeight - playerCollider.offset.y * playerScaleY;
        float playerGroundedTopY = playerGroundedCenterY + playerHalfHeight;
        float effectiveGravity =
            Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, playerBody.gravityScale);
        float maximumJumpCenterY =
            playerGroundedCenterY +
            jumpForce.floatValue * jumpForce.floatValue / (2f * effectiveGravity);
        ScoreAttackCoin lowCoin = null;
        ScoreAttackCoin highCoin = null;

        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            CircleCollider2D coinCollider = coin.GetComponent<CircleCollider2D>();
            SerializedObject serializedCoin = new SerializedObject(coin);
            SerializedProperty coinManager = serializedCoin.FindProperty("manager");
            if (coinCollider == null || coinManager == null || coinManager.objectReferenceValue != manager)
            {
                throw new System.InvalidOperationException(
                    $"{coin.name} is missing its collider or manager reference.");
            }

            float coinRadius =
                coinCollider.radius * Mathf.Abs(coin.transform.lossyScale.y);
            float dy = coin.transform.position.y - playerGroundedCenterY;
            if (coin.name == "MixedWarmup_LowCoin")
            {
                lowCoin = coin;
                if (dy > threshold.floatValue ||
                    coin.transform.position.y - coinRadius > playerGroundedTopY + 0.0001f)
                {
                    throw new System.InvalidOperationException(
                        $"MixedWarmup low coin must be low and ground-collectable; dy={dy:F3}.");
                }
            }
            else if (coin.name == "MixedWarmup_HighCoin")
            {
                highCoin = coin;
                float minimumPlayerCenterForCollection =
                    coin.transform.position.y - playerHalfHeight - coinRadius;
                if (dy <= threshold.floatValue ||
                    minimumPlayerCenterForCollection > maximumJumpCenterY)
                {
                    throw new System.InvalidOperationException(
                        $"MixedWarmup high coin must be high and reachable; dy={dy:F3}.");
                }
            }
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty requireEnemies = serializedManager.FindProperty("requireEnemiesForGoal");
        SerializedProperty minCoins = serializedManager.FindProperty("minActiveCoins");
        SerializedProperty maxCoins = serializedManager.FindProperty("maxActiveCoins");
        SerializedProperty minEnemies = serializedManager.FindProperty("minActiveEnemies");
        SerializedProperty maxEnemies = serializedManager.FindProperty("maxActiveEnemies");
        SerializedProperty coinReward = serializedManager.FindProperty("coinReward");
        SerializedProperty stompReward = serializedManager.FindProperty("enemyKillReward");
        SerializedProperty sideHitPenalty = serializedManager.FindProperty("enemySideHitPenalty");
        SerializedProperty finalReward = serializedManager.FindProperty("finalCompletionReward");
        SerializedProperty lockedGoalPenalty = serializedManager.FindProperty("prematureGoalPenalty");
        SerializedProperty endOnLockedGoal = serializedManager.FindProperty(
            "endEpisodeOnPrematureGoal");

        if (managerAgent == null || managerAgent.objectReferenceValue != agent ||
            requireEnemies == null || !requireEnemies.boolValue ||
            minCoins == null || minCoins.intValue != 2 ||
            maxCoins == null || maxCoins.intValue != 2 ||
            minEnemies == null || minEnemies.intValue != 1 ||
            maxEnemies == null || maxEnemies.intValue != 1 ||
            coinReward == null || Mathf.Abs(coinReward.floatValue - 2f) > 0.0001f ||
            stompReward == null || Mathf.Abs(stompReward.floatValue - 5f) > 0.0001f ||
            sideHitPenalty == null || Mathf.Abs(sideHitPenalty.floatValue + 6f) > 0.0001f ||
            finalReward == null || Mathf.Abs(finalReward.floatValue - 10f) > 0.0001f ||
            lockedGoalPenalty == null ||
            Mathf.Abs(lockedGoalPenalty.floatValue + 2f) > 0.0001f ||
            endOnLockedGoal == null || !endOnLockedGoal.boolValue)
        {
            throw new System.InvalidOperationException(
                "MixedWarmup manager rewards, counts, or GoalLock rules are invalid.");
        }

        SerializedObject serializedAndroid = new SerializedObject(androidComponent);
        SerializedProperty androidManager = serializedAndroid.FindProperty("manager");
        SerializedObject serializedGoalLock = new SerializedObject(goalLocks[0]);
        SerializedProperty goalLockManager = serializedGoalLock.FindProperty("manager");
        float pathMinX = Mathf.Min(player.transform.position.x, MixedWarmupLowCoinX);
        float pathMaxX = Mathf.Max(goal.transform.position.x, MixedWarmupAndroidX);
        bool layoutValid =
            lowCoin != null && highCoin != null &&
            Mathf.Abs(lowCoin.transform.position.x - MixedWarmupLowCoinX) <= 0.0001f &&
            Mathf.Abs(highCoin.transform.position.x - MixedWarmupHighCoinX) <= 0.0001f &&
            Mathf.Abs(android.transform.position.x - MixedWarmupAndroidX) <= 0.0001f &&
            Mathf.Abs(goal.transform.position.x - MixedWarmupGoalX) <= 0.0001f &&
            player.transform.position.x < lowCoin.transform.position.x &&
            lowCoin.transform.position.x < highCoin.transform.position.x &&
            highCoin.transform.position.x < android.transform.position.x &&
            android.transform.position.x < goal.transform.position.x &&
            platform.bounds.min.x <= pathMinX &&
            platform.bounds.max.x >= pathMaxX;

        if (androidManager == null || androidManager.objectReferenceValue != manager ||
            goalLockManager == null || goalLockManager.objectReferenceValue != manager ||
            !layoutValid)
        {
            throw new System.InvalidOperationException(
                "MixedWarmup ordered objective layout or manager references are invalid.");
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateMixedRandomWarmup(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        ScoreMaxOAMixedRandomWarmupRandomizer randomizer,
        GameObject android,
        GameObject goal)
    {
        const string phaseName = "MixedRandomWarmup";
        ValidateObjectAwarePlayer(player, phaseName);

        ScoreAttackCoin[] coins = GetSceneComponents<ScoreAttackCoin>(scene);
        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            CountSceneComponents<ScoreMaxOAMixedRandomWarmupRandomizer>(scene) != 1 ||
            coins.Length != 2 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 1 ||
            goalLocks.Length != 1 ||
            CountSceneComponents<DemoAndroidPatrol>(scene) != 0 ||
            CountSceneComponents<EdgeRunnerAgentV5ScoreMax>(scene) != 0 ||
            CountSceneComponents<EdgeRunnerAgentV5EnemyAware>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup requires two coins, one static Android, one manager, " +
                "one runtime randomizer, one GoalLock, no patrol, and no legacy agents.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        ScoreAttackAndroid androidComponent = android != null
            ? android.GetComponent<ScoreAttackAndroid>()
            : null;
        Rigidbody2D androidBody = android != null ? android.GetComponent<Rigidbody2D>() : null;
        Collider2D androidCollider = android != null ? android.GetComponent<Collider2D>() : null;
        BoxCollider2D platform = null;
        BoxCollider2D[] boxColliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < boxColliders.Length; i++)
        {
            if (boxColliders[i].gameObject.name == "MixedRandomWarmup_Platform")
            {
                platform = boxColliders[i];
                break;
            }
        }

        if (agent == null || playerCollider == null || playerBody == null ||
            manager == null || randomizer == null || androidComponent == null ||
            androidBody == null || androidBody.bodyType != RigidbodyType2D.Kinematic ||
            androidCollider == null || !androidCollider.isTrigger || platform == null ||
            goal == null)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup is missing its agent, physics, randomizer, platform, or Goal.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty assignedManager = serializedAgent.FindProperty("scoreAttackManager");
        SerializedProperty assignedRandomizer = serializedAgent.FindProperty(
            "mixedRandomWarmupRandomizer");
        SerializedProperty objectAwareGoal = serializedAgent.FindProperty("objectAwareGoal");
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty enforceLowGroundCollection = serializedAgent.FindProperty(
            "enforceLowCoinRunGroundCollection");
        SerializedProperty requireHighLanding = serializedAgent.FindProperty(
            "requireGroundedBetweenHighCoins");
        SerializedProperty requireGroundedLowCoin = serializedAgent.FindProperty(
            "requireGroundedLowCoin");
        SerializedProperty airborneLowCoinPenalty = serializedAgent.FindProperty(
            "airborneLowCoinPenalty");
        SerializedProperty endOnAirborneLowCoin = serializedAgent.FindProperty(
            "endEpisodeOnAirborneLowCoin");
        SerializedProperty requireLowHighLanding = serializedAgent.FindProperty(
            "requireGroundedBetweenLowAndHigh");
        SerializedProperty sameJumpHighCoinPenalty = serializedAgent.FindProperty(
            "sameJumpHighCoinPenalty");
        SerializedProperty endOnSameJumpHighCoin = serializedAgent.FindProperty(
            "endEpisodeOnSameJumpHighCoin");
        SerializedProperty threshold = serializedAgent.FindProperty("lowCoinHeightThreshold");
        SerializedProperty jumpForce = serializedAgent.FindProperty("jumpForce");
        SerializedProperty lowJumpPenalty = serializedAgent.FindProperty(
            "lowCoinUnnecessaryJumpPenalty");
        SerializedProperty missedCoinPenalty = serializedAgent.FindProperty("missedCoinPenalty");
        SerializedProperty missedEnemyPenalty = serializedAgent.FindProperty("missedEnemyPenalty");
        SerializedProperty endOnMissedEnemy = serializedAgent.FindProperty(
            "endEpisodeOnMissedEnemy");
        SerializedProperty debugObservationCount = serializedAgent.FindProperty(
            "debugObjectAwareObservationCount");
        SerializedProperty debugNextObjective = serializedAgent.FindProperty(
            "debugObjectAwareNextObjective");
        SerializedProperty debugJumpContext = serializedAgent.FindProperty(
            "debugObjectAwareJumpContext");
        SerializedProperty debugGizmos = serializedAgent.FindProperty(
            "debugObjectAwareGizmos");

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        EdgeRunnerAgentV5 prefabAgent = playerPrefab != null
            ? playerPrefab.GetComponent<EdgeRunnerAgentV5>()
            : null;
        SerializedProperty prefabJumpForce = prefabAgent != null
            ? new SerializedObject(prefabAgent).FindProperty("jumpForce")
            : null;

        if (phase == null ||
            phase.enumValueIndex != (int)EdgeRunnerObjectAwarePhase.MixedRandomWarmup ||
            assignedManager == null || assignedManager.objectReferenceValue != manager ||
            assignedRandomizer == null || assignedRandomizer.objectReferenceValue != randomizer ||
            objectAwareGoal == null || objectAwareGoal.objectReferenceValue != goal.transform ||
            rewardShaping == null || !rewardShaping.boolValue ||
            missedCoinEnd == null || !missedCoinEnd.boolValue ||
            contextualJumpMask == null || !contextualJumpMask.boolValue ||
            enforceLowGroundCollection == null || enforceLowGroundCollection.boolValue ||
            requireHighLanding == null || requireHighLanding.boolValue ||
            requireGroundedLowCoin == null || !requireGroundedLowCoin.boolValue ||
            airborneLowCoinPenalty == null ||
            Mathf.Abs(airborneLowCoinPenalty.floatValue + 2f) > 0.0001f ||
            endOnAirborneLowCoin == null || !endOnAirborneLowCoin.boolValue ||
            requireLowHighLanding == null || !requireLowHighLanding.boolValue ||
            sameJumpHighCoinPenalty == null ||
            Mathf.Abs(sameJumpHighCoinPenalty.floatValue + 2f) > 0.0001f ||
            endOnSameJumpHighCoin == null || !endOnSameJumpHighCoin.boolValue ||
            threshold == null ||
            Mathf.Abs(threshold.floatValue - LowCoinRunHeightThreshold) > 0.0001f ||
            jumpForce == null || prefabJumpForce == null ||
            Mathf.Abs(jumpForce.floatValue - prefabJumpForce.floatValue) > 0.0001f ||
            lowJumpPenalty == null ||
            Mathf.Abs(lowJumpPenalty.floatValue - LowCoinRunJumpPenalty) > 0.0001f ||
            missedCoinPenalty == null || Mathf.Abs(missedCoinPenalty.floatValue + 2f) > 0.0001f ||
            missedEnemyPenalty == null ||
            Mathf.Abs(missedEnemyPenalty.floatValue - StaticAndroidStompMissedPenalty) > 0.0001f ||
            endOnMissedEnemy == null || !endOnMissedEnemy.boolValue ||
            debugObservationCount == null || debugObservationCount.boolValue ||
            debugNextObjective == null || debugNextObjective.boolValue ||
            debugJumpContext == null || debugJumpContext.boolValue ||
            debugGizmos == null || debugGizmos.boolValue)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup has the wrong phase, references, shaping, jumpForce, or debug flags.");
        }

        Physics2D.SyncTransforms();
        float playerScaleY = Mathf.Abs(player.transform.lossyScale.y);
        float playerHalfHeight = playerCollider.size.y * playerScaleY * 0.5f;
        float playerGroundedCenterY =
            platform.bounds.max.y + playerHalfHeight - playerCollider.offset.y * playerScaleY;
        float playerGroundedTopY = playerGroundedCenterY + playerHalfHeight;
        float effectiveGravity =
            Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, playerBody.gravityScale);
        float maximumJumpCenterY =
            playerGroundedCenterY +
            jumpForce.floatValue * jumpForce.floatValue / (2f * effectiveGravity);
        ScoreAttackCoin lowCoin = null;
        ScoreAttackCoin highCoin = null;

        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            CircleCollider2D coinCollider = coin.GetComponent<CircleCollider2D>();
            SerializedObject serializedCoin = new SerializedObject(coin);
            SerializedProperty coinManager = serializedCoin.FindProperty("manager");
            if (coinCollider == null || coinManager == null ||
                coinManager.objectReferenceValue != manager)
            {
                throw new System.InvalidOperationException(
                    $"{coin.name} is missing its collider or manager reference.");
            }

            float coinRadius = coinCollider.radius * Mathf.Abs(coin.transform.lossyScale.y);
            float dy = coin.transform.position.y - playerGroundedCenterY;
            if (coin.name == "MixedRandomWarmup_LowCoin")
            {
                lowCoin = coin;
                if (dy > threshold.floatValue ||
                    coin.transform.position.y - coinRadius > playerGroundedTopY + 0.0001f)
                {
                    throw new System.InvalidOperationException(
                        $"MixedRandomWarmup low coin must stay low and ground-collectable; dy={dy:F3}.");
                }
            }
            else if (coin.name == "MixedRandomWarmup_HighCoin")
            {
                highCoin = coin;
                float minimumPlayerCenterForCollection =
                    coin.transform.position.y - playerHalfHeight - coinRadius;
                if (dy <= threshold.floatValue ||
                    minimumPlayerCenterForCollection > maximumJumpCenterY)
                {
                    throw new System.InvalidOperationException(
                        $"MixedRandomWarmup high coin must stay high and reachable; dy={dy:F3}.");
                }
            }
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty resetOnStart = serializedManager.FindProperty("resetOnStart");
        SerializedProperty managerRandomization = serializedManager.FindProperty(
            "randomizeObjectPositionsOnReset");
        SerializedProperty requireEnemies = serializedManager.FindProperty("requireEnemiesForGoal");
        SerializedProperty minCoins = serializedManager.FindProperty("minActiveCoins");
        SerializedProperty maxCoins = serializedManager.FindProperty("maxActiveCoins");
        SerializedProperty minEnemies = serializedManager.FindProperty("minActiveEnemies");
        SerializedProperty maxEnemies = serializedManager.FindProperty("maxActiveEnemies");
        SerializedProperty coinReward = serializedManager.FindProperty("coinReward");
        SerializedProperty stompReward = serializedManager.FindProperty("enemyKillReward");
        SerializedProperty sideHitPenalty = serializedManager.FindProperty("enemySideHitPenalty");
        SerializedProperty finalReward = serializedManager.FindProperty("finalCompletionReward");
        SerializedProperty lockedGoalPenalty = serializedManager.FindProperty("prematureGoalPenalty");
        SerializedProperty endOnLockedGoal = serializedManager.FindProperty(
            "endEpisodeOnPrematureGoal");

        if (managerAgent == null || managerAgent.objectReferenceValue != agent ||
            resetOnStart == null || resetOnStart.boolValue ||
            managerRandomization == null || managerRandomization.boolValue ||
            requireEnemies == null || !requireEnemies.boolValue ||
            minCoins == null || minCoins.intValue != 2 ||
            maxCoins == null || maxCoins.intValue != 2 ||
            minEnemies == null || minEnemies.intValue != 1 ||
            maxEnemies == null || maxEnemies.intValue != 1 ||
            coinReward == null || Mathf.Abs(coinReward.floatValue - 2f) > 0.0001f ||
            stompReward == null || Mathf.Abs(stompReward.floatValue - 5f) > 0.0001f ||
            sideHitPenalty == null || Mathf.Abs(sideHitPenalty.floatValue + 6f) > 0.0001f ||
            finalReward == null || Mathf.Abs(finalReward.floatValue - 10f) > 0.0001f ||
            lockedGoalPenalty == null ||
            Mathf.Abs(lockedGoalPenalty.floatValue + 2f) > 0.0001f ||
            endOnLockedGoal == null || !endOnLockedGoal.boolValue)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup manager rewards, counts, reset, or GoalLock rules are invalid.");
        }

        SerializedObject serializedRandomizer = new SerializedObject(randomizer);
        SerializedProperty randomLowCoin = serializedRandomizer.FindProperty("lowCoin");
        SerializedProperty randomHighCoin = serializedRandomizer.FindProperty("highCoin");
        SerializedProperty randomAndroid = serializedRandomizer.FindProperty("android");
        SerializedProperty randomGoal = serializedRandomizer.FindProperty("goal");
        SerializedProperty lowRange = serializedRandomizer.FindProperty("lowCoinXRange");
        SerializedProperty highRange = serializedRandomizer.FindProperty("highCoinXRange");
        SerializedProperty androidRange = serializedRandomizer.FindProperty("androidXRange");
        SerializedProperty goalRange = serializedRandomizer.FindProperty("goalXRange");
        SerializedProperty lowY = serializedRandomizer.FindProperty("lowCoinY");
        SerializedProperty highY = serializedRandomizer.FindProperty("highCoinY");
        SerializedProperty androidY = serializedRandomizer.FindProperty("androidY");
        SerializedProperty goalY = serializedRandomizer.FindProperty("goalY");
        SerializedProperty minimumLowHighSpacing = serializedRandomizer.FindProperty(
            "minimumLowCoinToHighCoinDistance");
        SerializedProperty minimumSpacing = serializedRandomizer.FindProperty(
            "minimumHighCoinToAndroidDistance");
        SerializedProperty debugRandomPositions = serializedRandomizer.FindProperty(
            "debugObjectAwareMixedRandomPositions");

        Vector2 expectedLowRange = new Vector2(
            MixedRandomWarmupLowCoinMinX,
            MixedRandomWarmupLowCoinMaxX);
        Vector2 expectedHighRange = new Vector2(
            MixedRandomWarmupHighCoinMinX,
            MixedRandomWarmupHighCoinMaxX);
        Vector2 expectedAndroidRange = new Vector2(
            MixedRandomWarmupAndroidMinX,
            MixedRandomWarmupAndroidMaxX);
        Vector2 expectedGoalRange = new Vector2(
            MixedRandomWarmupGoalMinX,
            MixedRandomWarmupGoalMaxX);

        if (lowCoin == null || highCoin == null ||
            randomLowCoin == null || randomLowCoin.objectReferenceValue != lowCoin ||
            randomHighCoin == null || randomHighCoin.objectReferenceValue != highCoin ||
            randomAndroid == null || randomAndroid.objectReferenceValue != androidComponent ||
            randomGoal == null || randomGoal.objectReferenceValue != goal.transform ||
            lowRange == null || lowRange.vector2Value != expectedLowRange ||
            highRange == null || highRange.vector2Value != expectedHighRange ||
            androidRange == null || androidRange.vector2Value != expectedAndroidRange ||
            goalRange == null || goalRange.vector2Value != expectedGoalRange ||
            lowY == null || Mathf.Abs(lowY.floatValue - MixedWarmupLowCoinY) > 0.0001f ||
            highY == null || Mathf.Abs(highY.floatValue - MixedWarmupHighCoinY) > 0.0001f ||
            androidY == null || Mathf.Abs(androidY.floatValue - 1.02f) > 0.0001f ||
            goalY == null || Mathf.Abs(goalY.floatValue - 1.2f) > 0.0001f ||
            minimumLowHighSpacing == null ||
            minimumLowHighSpacing.floatValue <
                MixedRandomWarmupMinimumLowCoinHighCoinDistance ||
            minimumSpacing == null ||
            minimumSpacing.floatValue < MixedRandomWarmupMinimumHighCoinAndroidDistance ||
            debugRandomPositions == null || debugRandomPositions.boolValue)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup runtime randomizer ranges, references, heights, or debug are invalid.");
        }

        SerializedObject serializedAndroid = new SerializedObject(androidComponent);
        SerializedProperty androidManager = serializedAndroid.FindProperty("manager");
        SerializedObject serializedGoalLock = new SerializedObject(goalLocks[0]);
        SerializedProperty goalLockManager = serializedGoalLock.FindProperty("manager");
        bool rangesPreserveOrder =
            MixedRandomWarmupLowCoinMaxX < MixedRandomWarmupHighCoinMinX &&
            MixedRandomWarmupHighCoinMinX - MixedRandomWarmupLowCoinMaxX >=
                MixedRandomWarmupMinimumLowCoinHighCoinDistance &&
            MixedRandomWarmupLowCoinMaxX +
                MixedRandomWarmupMinimumLowCoinHighCoinDistance <=
                MixedRandomWarmupHighCoinMaxX &&
            MixedRandomWarmupHighCoinMaxX +
                MixedRandomWarmupMinimumHighCoinAndroidDistance <=
                MixedRandomWarmupAndroidMaxX &&
            MixedRandomWarmupAndroidMaxX < MixedRandomWarmupGoalMinX;
        bool baseLayoutValid =
            player.transform.position.x < lowCoin.transform.position.x &&
            lowCoin.transform.position.x < highCoin.transform.position.x &&
            highCoin.transform.position.x < android.transform.position.x &&
            android.transform.position.x < goal.transform.position.x &&
            platform.bounds.min.x <= player.transform.position.x &&
            platform.bounds.max.x >= MixedRandomWarmupGoalMaxX;

        if (androidManager == null || androidManager.objectReferenceValue != manager ||
            goalLockManager == null || goalLockManager.objectReferenceValue != manager ||
            !rangesPreserveOrder || !baseLayoutValid)
        {
            throw new System.InvalidOperationException(
                "MixedRandomWarmup objective ordering, platform coverage, or manager references are invalid.");
        }

        ValidateCommonSceneObjects(scene, phaseName);
    }

    private static void ValidateFinalRandom(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        ScoreMaxOAFinalRandomizer randomizer,
        GameObject android,
        GameObject goal)
    {
        const string phaseName = "FinalRandom";
        ValidateObjectAwarePlayer(player, phaseName);

        ScoreAttackCoin[] coins = GetSceneComponents<ScoreAttackCoin>(scene);
        ScoreAttackGoalLock[] goalLocks = GetSceneComponents<ScoreAttackGoalLock>(scene);
        if (CountSceneComponents<ScoreAttackManager>(scene) != 1 ||
            CountSceneComponents<ScoreMaxOAFinalRandomizer>(scene) != 1 ||
            coins.Length != 4 ||
            CountSceneComponents<ScoreAttackAndroid>(scene) != 1 ||
            goalLocks.Length != 1 ||
            CountSceneComponents<DemoAndroidPatrol>(scene) != 0 ||
            CountSceneComponents<EdgeRunnerAgentV5ScoreMax>(scene) != 0 ||
            CountSceneComponents<EdgeRunnerAgentV5EnemyAware>(scene) != 0)
        {
            throw new System.InvalidOperationException(
                "FinalRandom requires four coin slots, one static Android, one manager, " +
                "one runtime randomizer, one GoalLock, no patrol, and no legacy agents.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        ScoreAttackAndroid androidComponent = android != null
            ? android.GetComponent<ScoreAttackAndroid>()
            : null;
        Rigidbody2D androidBody = android != null ? android.GetComponent<Rigidbody2D>() : null;
        Collider2D androidCollider = android != null ? android.GetComponent<Collider2D>() : null;

        Dictionary<string, BoxCollider2D> platforms = new Dictionary<string, BoxCollider2D>();
        BoxCollider2D[] boxColliders = GetSceneComponents<BoxCollider2D>(scene);
        for (int i = 0; i < boxColliders.Length; i++)
        {
            string objectName = boxColliders[i].gameObject.name;
            if (objectName.StartsWith("FinalRandom_", System.StringComparison.Ordinal))
            {
                platforms[objectName] = boxColliders[i];
            }
        }

        if (agent == null || playerCollider == null || playerBody == null ||
            manager == null || randomizer == null || androidComponent == null ||
            androidBody == null || androidBody.bodyType != RigidbodyType2D.Kinematic ||
            androidCollider == null || !androidCollider.isTrigger || goal == null ||
            !platforms.ContainsKey("FinalRandom_StartSafeLow") ||
            !platforms.ContainsKey("FinalRandom_Recovery_A") ||
            !platforms.ContainsKey("FinalRandom_HighRecovery") ||
            !platforms.ContainsKey("FinalRandom_AndroidGoal"))
        {
            throw new System.InvalidOperationException(
                "FinalRandom is missing its agent, physics, static Android, Goal, or platforms.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty assignedManager = serializedAgent.FindProperty("scoreAttackManager");
        SerializedProperty assignedRandomizer = serializedAgent.FindProperty("finalRandomizer");
        SerializedProperty objectAwareGoal = serializedAgent.FindProperty("objectAwareGoal");
        SerializedProperty rewardShaping = serializedAgent.FindProperty(
            "enableObjectAwareRewardShaping");
        SerializedProperty missedCoinEnd = serializedAgent.FindProperty(
            "enableMissedCoinEpisodeEnd");
        SerializedProperty contextualJumpMask = serializedAgent.FindProperty(
            "enableContextualJumpMask");
        SerializedProperty requireGroundedLowCoin = serializedAgent.FindProperty(
            "requireGroundedLowCoin");
        SerializedProperty airborneLowCoinPenalty = serializedAgent.FindProperty(
            "airborneLowCoinPenalty");
        SerializedProperty endOnAirborneLowCoin = serializedAgent.FindProperty(
            "endEpisodeOnAirborneLowCoin");
        SerializedProperty requireLowHighLanding = serializedAgent.FindProperty(
            "requireGroundedBetweenLowAndHigh");
        SerializedProperty sameJumpHighCoinPenalty = serializedAgent.FindProperty(
            "sameJumpHighCoinPenalty");
        SerializedProperty endOnSameJumpHighCoin = serializedAgent.FindProperty(
            "endEpisodeOnSameJumpHighCoin");
        SerializedProperty antiLedgeEnabled = serializedAgent.FindProperty(
            "enableAntiLedgeStuckFailSafe");
        SerializedProperty ledgeGraceTime = serializedAgent.FindProperty(
            "ledgeStuckGraceTime");
        SerializedProperty ledgeMinY = serializedAgent.FindProperty(
            "ledgeStuckMinYBelowGround");
        SerializedProperty ledgeMaxVelocity = serializedAgent.FindProperty(
            "ledgeStuckMaxVelocity");
        SerializedProperty ledgeProgressEpsilon = serializedAgent.FindProperty(
            "ledgeStuckProgressEpsilon");
        SerializedProperty ledgePenalty = serializedAgent.FindProperty("ledgeStuckPenalty");
        SerializedProperty debugAntiLedge = serializedAgent.FindProperty("debugAntiLedgeStuck");
        SerializedProperty threshold = serializedAgent.FindProperty("lowCoinHeightThreshold");
        SerializedProperty jumpForce = serializedAgent.FindProperty("jumpForce");
        SerializedProperty missedCoinPenalty = serializedAgent.FindProperty("missedCoinPenalty");
        SerializedProperty missedEnemyPenalty = serializedAgent.FindProperty("missedEnemyPenalty");
        SerializedProperty endOnMissedEnemy = serializedAgent.FindProperty(
            "endEpisodeOnMissedEnemy");
        SerializedProperty debugObservationCount = serializedAgent.FindProperty(
            "debugObjectAwareObservationCount");
        SerializedProperty debugNextObjective = serializedAgent.FindProperty(
            "debugObjectAwareNextObjective");
        SerializedProperty debugJumpContext = serializedAgent.FindProperty(
            "debugObjectAwareJumpContext");
        SerializedProperty debugGizmos = serializedAgent.FindProperty(
            "debugObjectAwareGizmos");

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        EdgeRunnerAgentV5 prefabAgent = playerPrefab != null
            ? playerPrefab.GetComponent<EdgeRunnerAgentV5>()
            : null;
        SerializedProperty prefabJumpForce = prefabAgent != null
            ? new SerializedObject(prefabAgent).FindProperty("jumpForce")
            : null;
        PhysicsMaterial2D noFrictionMaterial =
            AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(AgentNoFrictionMaterialPath);

        if (phase == null || phase.enumValueIndex != (int)EdgeRunnerObjectAwarePhase.FinalRandom ||
            assignedManager == null || assignedManager.objectReferenceValue != manager ||
            assignedRandomizer == null || assignedRandomizer.objectReferenceValue != randomizer ||
            objectAwareGoal == null || objectAwareGoal.objectReferenceValue != goal.transform ||
            rewardShaping == null || !rewardShaping.boolValue ||
            missedCoinEnd == null || !missedCoinEnd.boolValue ||
            contextualJumpMask == null || !contextualJumpMask.boolValue ||
            requireGroundedLowCoin == null || !requireGroundedLowCoin.boolValue ||
            airborneLowCoinPenalty == null ||
            Mathf.Abs(airborneLowCoinPenalty.floatValue + 2f) > 0.0001f ||
            endOnAirborneLowCoin == null || !endOnAirborneLowCoin.boolValue ||
            requireLowHighLanding == null || !requireLowHighLanding.boolValue ||
            sameJumpHighCoinPenalty == null ||
            Mathf.Abs(sameJumpHighCoinPenalty.floatValue + 2f) > 0.0001f ||
            endOnSameJumpHighCoin == null || !endOnSameJumpHighCoin.boolValue ||
            antiLedgeEnabled == null || !antiLedgeEnabled.boolValue ||
            ledgeGraceTime == null ||
            Mathf.Abs(ledgeGraceTime.floatValue - FinalRandomLedgeStuckGraceTime) > 0.0001f ||
            ledgeMinY == null ||
            Mathf.Abs(ledgeMinY.floatValue - FinalRandomLedgeStuckMinYBelowGround) > 0.0001f ||
            ledgeMaxVelocity == null ||
            Mathf.Abs(ledgeMaxVelocity.floatValue - FinalRandomLedgeStuckMaxVelocity) > 0.0001f ||
            ledgeProgressEpsilon == null ||
            Mathf.Abs(
                ledgeProgressEpsilon.floatValue - FinalRandomLedgeStuckProgressEpsilon) >
                0.0001f ||
            ledgePenalty == null ||
            Mathf.Abs(ledgePenalty.floatValue - FinalRandomLedgeStuckPenalty) > 0.0001f ||
            debugAntiLedge == null || debugAntiLedge.boolValue ||
            noFrictionMaterial == null || playerCollider.sharedMaterial != noFrictionMaterial ||
            Mathf.Abs(noFrictionMaterial.friction) > 0.0001f ||
            threshold == null ||
            Mathf.Abs(threshold.floatValue - LowCoinRunHeightThreshold) > 0.0001f ||
            jumpForce == null || prefabJumpForce == null ||
            Mathf.Abs(jumpForce.floatValue - prefabJumpForce.floatValue) > 0.0001f ||
            missedCoinPenalty == null || Mathf.Abs(missedCoinPenalty.floatValue + 2f) > 0.0001f ||
            missedEnemyPenalty == null ||
            Mathf.Abs(missedEnemyPenalty.floatValue - StaticAndroidStompMissedPenalty) > 0.0001f ||
            endOnMissedEnemy == null || !endOnMissedEnemy.boolValue ||
            debugObservationCount == null || debugObservationCount.boolValue ||
            debugNextObjective == null || debugNextObjective.boolValue ||
            debugJumpContext == null || debugJumpContext.boolValue ||
            debugGizmos == null || debugGizmos.boolValue)
        {
            throw new System.InvalidOperationException(
                "FinalRandom has invalid phase, references, gates, anti-ledge fail-safe, " +
                "friction, jumpForce, or debug flags.");
        }

        Physics2D.SyncTransforms();
        BoxCollider2D startPlatform = platforms["FinalRandom_StartSafeLow"];
        BoxCollider2D recoveryPlatform = platforms["FinalRandom_Recovery_A"];
        BoxCollider2D highPlatform = platforms["FinalRandom_HighRecovery"];
        BoxCollider2D finalPlatformCollider = platforms["FinalRandom_AndroidGoal"];
        if (Mathf.Abs(startPlatform.bounds.size.x - FinalRandomStartPlatformWidth) > 0.01f ||
            Mathf.Abs(recoveryPlatform.bounds.size.x - FinalRandomRecoveryPlatformWidth) > 0.01f ||
            Mathf.Abs(highPlatform.bounds.size.x - FinalRandomHighPlatformWidth) > 0.01f ||
            Mathf.Abs(finalPlatformCollider.bounds.size.x - FinalRandomFinalPlatformWidth) > 0.01f)
        {
            throw new System.InvalidOperationException(
                "FinalRandom platform widths do not preserve the safe-low and recovery zones.");
        }

        float playerScaleY = Mathf.Abs(player.transform.lossyScale.y);
        float playerHalfHeight = playerCollider.size.y * playerScaleY * 0.5f;
        float groundedCenterY =
            startPlatform.bounds.max.y + playerHalfHeight -
            playerCollider.offset.y * playerScaleY;
        float groundedTopY = groundedCenterY + playerHalfHeight;
        float effectiveGravity =
            Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, playerBody.gravityScale);
        float maximumJumpCenterY =
            groundedCenterY + jumpForce.floatValue * jumpForce.floatValue / (2f * effectiveGravity);
        int lowCoinCount = 0;
        int highCoinCount = 0;
        ScoreAttackCoin firstLowCoin = null;
        ScoreAttackCoin secondLowCoin = null;

        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            CircleCollider2D coinCollider = coin.GetComponent<CircleCollider2D>();
            SerializedProperty coinManager =
                new SerializedObject(coin).FindProperty("manager");
            if (coinCollider == null || coinManager == null ||
                coinManager.objectReferenceValue != manager)
            {
                throw new System.InvalidOperationException(
                    $"{coin.name} is missing its collider or manager reference.");
            }

            float coinRadius = coinCollider.radius * Mathf.Abs(coin.transform.lossyScale.y);
            float dy = coin.transform.position.y - groundedCenterY;
            if (coin.name.StartsWith("FinalRandom_LowCoin_", System.StringComparison.Ordinal))
            {
                lowCoinCount++;
                if (coin.name == "FinalRandom_LowCoin_01")
                {
                    firstLowCoin = coin;
                }
                else if (coin.name == "FinalRandom_LowCoin_02")
                {
                    secondLowCoin = coin;
                }

                float flatRunBefore = coin.transform.position.x - player.transform.position.x;
                float flatRunAfter = startPlatform.bounds.max.x - coin.transform.position.x;
                float gapEdgeDistance = startPlatform.bounds.max.x - coin.transform.position.x;
                float landingZoneClearance = coin.transform.position.x -
                    (startPlatform.bounds.min.x + FinalRandomMinLowCoinLandingZoneDistance);
                if (dy > threshold.floatValue ||
                    coin.transform.position.y - coinRadius > groundedTopY + 0.0001f ||
                    coin.transform.position.x <= startPlatform.bounds.min.x ||
                    coin.transform.position.x >= startPlatform.bounds.max.x ||
                    flatRunBefore < FinalRandomMinFlatRunBeforeLowCoin ||
                    flatRunAfter < FinalRandomMinFlatRunAfterLowCoin ||
                    gapEdgeDistance < FinalRandomMinLowCoinGapEdgeDistance ||
                    landingZoneClearance < 0f)
                {
                    throw new System.InvalidOperationException(
                        $"{coin.name} must be low in the initial safe-flat zone; " +
                        $"dy={dy:F3}, flatBefore={flatRunBefore:F2}, " +
                        $"flatAfter={flatRunAfter:F2}, gapEdge={gapEdgeDistance:F2}, " +
                        $"landingClearance={landingZoneClearance:F2}.");
                }
            }
            else if (coin.name.StartsWith("FinalRandom_HighCoin_", System.StringComparison.Ordinal))
            {
                highCoinCount++;
                float minimumCenterForCollection =
                    coin.transform.position.y - playerHalfHeight - coinRadius;
                if (dy <= threshold.floatValue ||
                    minimumCenterForCollection > maximumJumpCenterY ||
                    coin.transform.position.x <= highPlatform.bounds.min.x ||
                    coin.transform.position.x >= highPlatform.bounds.max.x)
                {
                    throw new System.InvalidOperationException(
                        $"{coin.name} must be high and reachable; dy={dy:F3}.");
                }
            }
        }

        if (lowCoinCount != 2 || highCoinCount != 2 ||
            firstLowCoin == null || secondLowCoin == null ||
            firstLowCoin.transform.position.x < 4f ||
            firstLowCoin.transform.position.x > 6f ||
            secondLowCoin.transform.position.x < 8f ||
            secondLowCoin.transform.position.x > 10f ||
            firstLowCoin.transform.position.x >= secondLowCoin.transform.position.x)
        {
            throw new System.InvalidOperationException(
                "FinalRandom requires ordered low slots at x=4-6 and x=8-10 " +
                "on the initial safe-flat platform, plus two high slots.");
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty managerAgent = serializedManager.FindProperty("agent");
        SerializedProperty resetOnStart = serializedManager.FindProperty("resetOnStart");
        SerializedProperty managerRandomization = serializedManager.FindProperty(
            "randomizeObjectPositionsOnReset");
        SerializedProperty requireEnemies = serializedManager.FindProperty("requireEnemiesForGoal");
        SerializedProperty minCoins = serializedManager.FindProperty("minActiveCoins");
        SerializedProperty maxCoins = serializedManager.FindProperty("maxActiveCoins");
        SerializedProperty minEnemies = serializedManager.FindProperty("minActiveEnemies");
        SerializedProperty maxEnemies = serializedManager.FindProperty("maxActiveEnemies");
        SerializedProperty coinReward = serializedManager.FindProperty("coinReward");
        SerializedProperty stompReward = serializedManager.FindProperty("enemyKillReward");
        SerializedProperty sideHitPenalty = serializedManager.FindProperty("enemySideHitPenalty");
        SerializedProperty finalReward = serializedManager.FindProperty("finalCompletionReward");
        SerializedProperty lockedGoalPenalty = serializedManager.FindProperty("prematureGoalPenalty");
        SerializedProperty endOnLockedGoal = serializedManager.FindProperty(
            "endEpisodeOnPrematureGoal");

        if (managerAgent == null || managerAgent.objectReferenceValue != agent ||
            resetOnStart == null || resetOnStart.boolValue ||
            managerRandomization == null || managerRandomization.boolValue ||
            requireEnemies == null || !requireEnemies.boolValue ||
            minCoins == null || minCoins.intValue != 2 ||
            maxCoins == null || maxCoins.intValue != 4 ||
            minEnemies == null || minEnemies.intValue != 1 ||
            maxEnemies == null || maxEnemies.intValue != 1 ||
            coinReward == null || Mathf.Abs(coinReward.floatValue - 2f) > 0.0001f ||
            stompReward == null || Mathf.Abs(stompReward.floatValue - 5f) > 0.0001f ||
            sideHitPenalty == null || Mathf.Abs(sideHitPenalty.floatValue + 6f) > 0.0001f ||
            finalReward == null || Mathf.Abs(finalReward.floatValue - 10f) > 0.0001f ||
            lockedGoalPenalty == null ||
            Mathf.Abs(lockedGoalPenalty.floatValue + 2f) > 0.0001f ||
            endOnLockedGoal == null || !endOnLockedGoal.boolValue)
        {
            throw new System.InvalidOperationException(
                "FinalRandom manager rewards, counts, reset, or GoalLock rules are invalid.");
        }

        SerializedObject serializedRandomizer = new SerializedObject(randomizer);
        SerializedProperty randomManager = serializedRandomizer.FindProperty("manager");
        SerializedProperty randomLowCoins = serializedRandomizer.FindProperty("lowCoins");
        SerializedProperty randomHighCoins = serializedRandomizer.FindProperty("highCoins");
        SerializedProperty randomAndroid = serializedRandomizer.FindProperty("android");
        SerializedProperty randomGoal = serializedRandomizer.FindProperty("goal");
        SerializedProperty randomStartPlatform = serializedRandomizer.FindProperty("startPlatform");
        SerializedProperty randomLowPlatform = serializedRandomizer.FindProperty("lowPlatform");
        SerializedProperty randomHighPlatform = serializedRandomizer.FindProperty(
            "highRecoveryPlatform");
        SerializedProperty randomFinalPlatform = serializedRandomizer.FindProperty("finalPlatform");
        SerializedProperty gapOne = serializedRandomizer.FindProperty("firstGapWidthRange");
        SerializedProperty gapTwo = serializedRandomizer.FindProperty("secondGapWidthRange");
        SerializedProperty minimumLowHigh = serializedRandomizer.FindProperty(
            "minimumLowCoinToHighCoinDistance");
        SerializedProperty minimumHighAndroid = serializedRandomizer.FindProperty(
            "minimumHighCoinToAndroidDistance");
        SerializedProperty minimumAndroidGoal = serializedRandomizer.FindProperty(
            "minimumAndroidToGoalDistance");
        SerializedProperty randomStartLeft = serializedRandomizer.FindProperty(
            "startPlatformLeftX");
        SerializedProperty randomStartWidth = serializedRandomizer.FindProperty(
            "startPlatformWidth");
        SerializedProperty randomRecoveryWidth = serializedRandomizer.FindProperty(
            "lowPlatformWidth");
        SerializedProperty randomHighWidth = serializedRandomizer.FindProperty(
            "highRecoveryPlatformWidth");
        SerializedProperty randomFinalWidth = serializedRandomizer.FindProperty(
            "finalPlatformWidth");
        SerializedProperty randomPlayerStartX = serializedRandomizer.FindProperty("playerStartX");
        SerializedProperty firstLowRange = serializedRandomizer.FindProperty(
            "firstLowCoinXRange");
        SerializedProperty secondLowRange = serializedRandomizer.FindProperty(
            "secondLowCoinXRange");
        SerializedProperty minFlatBefore = serializedRandomizer.FindProperty(
            "minFlatRunBeforeLowCoin");
        SerializedProperty minFlatAfter = serializedRandomizer.FindProperty(
            "minFlatRunAfterLowCoin");
        SerializedProperty minGapEdge = serializedRandomizer.FindProperty(
            "minLowCoinDistanceFromGapEdge");
        SerializedProperty minLandingZone = serializedRandomizer.FindProperty(
            "minLowCoinDistanceFromLandingZone");
        SerializedProperty firstLowSafe = serializedRandomizer.FindProperty("firstLowCoinSafe");
        SerializedProperty debugPositions = serializedRandomizer.FindProperty(
            "debugObjectAwareFinalRandomPositions");

        bool randomArraysValid =
            randomLowCoins != null && randomLowCoins.arraySize == 2 &&
            randomHighCoins != null && randomHighCoins.arraySize == 2 &&
            randomLowCoins.GetArrayElementAtIndex(0).objectReferenceValue != null &&
            randomLowCoins.GetArrayElementAtIndex(0).objectReferenceValue.name ==
                "FinalRandom_LowCoin_01" &&
            randomLowCoins.GetArrayElementAtIndex(1).objectReferenceValue != null &&
            randomLowCoins.GetArrayElementAtIndex(1).objectReferenceValue.name ==
                "FinalRandom_LowCoin_02" &&
            randomHighCoins.GetArrayElementAtIndex(0).objectReferenceValue != null &&
            randomHighCoins.GetArrayElementAtIndex(0).objectReferenceValue.name ==
                "FinalRandom_HighCoin_01" &&
            randomHighCoins.GetArrayElementAtIndex(1).objectReferenceValue != null &&
            randomHighCoins.GetArrayElementAtIndex(1).objectReferenceValue.name ==
                "FinalRandom_HighCoin_02";
        if (randomManager == null || randomManager.objectReferenceValue != manager ||
            !randomArraysValid ||
            randomAndroid == null || randomAndroid.objectReferenceValue != androidComponent ||
            randomGoal == null || randomGoal.objectReferenceValue != goal.transform ||
            randomStartPlatform == null ||
            randomStartPlatform.objectReferenceValue !=
                platforms["FinalRandom_StartSafeLow"].transform ||
            randomLowPlatform == null ||
            randomLowPlatform.objectReferenceValue !=
                platforms["FinalRandom_Recovery_A"].transform ||
            randomHighPlatform == null ||
            randomHighPlatform.objectReferenceValue !=
                platforms["FinalRandom_HighRecovery"].transform ||
            randomFinalPlatform == null ||
            randomFinalPlatform.objectReferenceValue !=
                platforms["FinalRandom_AndroidGoal"].transform ||
            gapOne == null ||
            gapOne.vector2Value != new Vector2(FinalRandomFirstGapMin, FinalRandomFirstGapMax) ||
            gapTwo == null ||
            gapTwo.vector2Value != new Vector2(FinalRandomSecondGapMin, FinalRandomSecondGapMax) ||
            gapOne.vector2Value.y > 2.2f || gapTwo.vector2Value.y > 2.8f ||
            minimumLowHigh == null ||
            minimumLowHigh.floatValue < FinalRandomMinimumLowHighDistance ||
            minimumHighAndroid == null ||
            minimumHighAndroid.floatValue < FinalRandomMinimumHighAndroidDistance ||
            minimumAndroidGoal == null ||
            minimumAndroidGoal.floatValue < FinalRandomMinimumAndroidGoalDistance ||
            randomStartLeft == null ||
            Mathf.Abs(randomStartLeft.floatValue - FinalRandomStartPlatformLeftX) > 0.0001f ||
            randomStartWidth == null ||
            Mathf.Abs(randomStartWidth.floatValue - FinalRandomStartPlatformWidth) > 0.0001f ||
            randomRecoveryWidth == null ||
            Mathf.Abs(randomRecoveryWidth.floatValue - FinalRandomRecoveryPlatformWidth) > 0.0001f ||
            randomHighWidth == null ||
            Mathf.Abs(randomHighWidth.floatValue - FinalRandomHighPlatformWidth) > 0.0001f ||
            randomFinalWidth == null ||
            Mathf.Abs(randomFinalWidth.floatValue - FinalRandomFinalPlatformWidth) > 0.0001f ||
            randomPlayerStartX == null ||
            Mathf.Abs(randomPlayerStartX.floatValue - FinalRandomPlayerStartX) > 0.0001f ||
            firstLowRange == null || firstLowRange.vector2Value != new Vector2(4f, 6f) ||
            secondLowRange == null || secondLowRange.vector2Value != new Vector2(8f, 10f) ||
            minFlatBefore == null ||
            minFlatBefore.floatValue < FinalRandomMinFlatRunBeforeLowCoin ||
            minFlatAfter == null ||
            minFlatAfter.floatValue < FinalRandomMinFlatRunAfterLowCoin ||
            minGapEdge == null ||
            minGapEdge.floatValue < FinalRandomMinLowCoinGapEdgeDistance ||
            minLandingZone == null ||
            minLandingZone.floatValue < FinalRandomMinLowCoinLandingZoneDistance ||
            firstLowSafe == null || !firstLowSafe.boolValue ||
            debugPositions == null || debugPositions.boolValue)
        {
            throw new System.InvalidOperationException(
                "FinalRandom runtime references, safe-low zone, gap ranges, spacing, or debug " +
                "are invalid.");
        }

        SerializedProperty androidManager =
            new SerializedObject(androidComponent).FindProperty("manager");
        SerializedProperty goalLockManager =
            new SerializedObject(goalLocks[0]).FindProperty("manager");
        if (androidManager == null || androidManager.objectReferenceValue != manager ||
            goalLockManager == null || goalLockManager.objectReferenceValue != manager ||
            goal.transform.position.x <= android.transform.position.x)
        {
            throw new System.InvalidOperationException(
                "FinalRandom Android/Goal ordering or manager references are invalid.");
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
        return GetSceneComponents<T>(scene).Length;
    }

    private static T[] GetSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> components = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            components.AddRange(roots[i].GetComponentsInChildren<T>(true));
        }

        return components.ToArray();
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

    private static ScoreAttackManager CreateStaticAndroidAvoidManager(Transform parent)
    {
        GameObject managerObject = new GameObject("ScoreMaxOA_StaticAndroidAvoid_Manager");
        managerObject.transform.SetParent(parent, false);
        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetBool(manager, "requireEnemiesForGoal", false);
        SetInt(manager, "minActiveCoins", 0);
        SetInt(manager, "maxActiveCoins", 0);
        SetInt(manager, "minActiveEnemies", 1);
        SetInt(manager, "maxActiveEnemies", 1);
        SetFloat(manager, "coinReward", 0f);
        SetFloat(manager, "enemyKillReward", StaticAndroidAvoidStompReward);
        SetFloat(manager, "enemySideHitPenalty", StaticAndroidAvoidSideHitPenalty);
        SetFloat(manager, "finalCompletionReward", 0f);
        SetFloat(manager, "prematureGoalPenalty", 0f);
        SetBool(manager, "endEpisodeOnPrematureGoal", false);
        SetBool(manager, "debugLogs", false);
        return manager;
    }

    private static ScoreAttackManager CreateStaticAndroidStompManager(Transform parent)
    {
        GameObject managerObject = new GameObject("ScoreMaxOA_StaticAndroidStomp_Manager");
        managerObject.transform.SetParent(parent, false);
        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetBool(manager, "requireEnemiesForGoal", true);
        SetInt(manager, "minActiveCoins", 0);
        SetInt(manager, "maxActiveCoins", 0);
        SetInt(manager, "minActiveEnemies", 1);
        SetInt(manager, "maxActiveEnemies", 1);
        SetFloat(manager, "coinReward", 0f);
        SetFloat(manager, "enemyKillReward", StaticAndroidStompReward);
        SetFloat(manager, "enemySideHitPenalty", StaticAndroidStompSideHitPenalty);
        SetFloat(manager, "finalCompletionReward", 10f);
        SetFloat(manager, "prematureGoalPenalty", StaticAndroidStompLockedGoalPenalty);
        SetBool(manager, "endEpisodeOnPrematureGoal", true);
        SetBool(manager, "debugLogs", false);
        return manager;
    }

    private static ScoreAttackManager CreateMixedWarmupManager(Transform parent)
    {
        GameObject managerObject = new GameObject("ScoreMaxOA_MixedWarmup_Manager");
        managerObject.transform.SetParent(parent, false);
        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetBool(manager, "requireEnemiesForGoal", true);
        SetInt(manager, "minActiveCoins", 2);
        SetInt(manager, "maxActiveCoins", 2);
        SetInt(manager, "minActiveEnemies", 1);
        SetInt(manager, "maxActiveEnemies", 1);
        SetFloat(manager, "coinReward", 2f);
        SetFloat(manager, "enemyKillReward", StaticAndroidStompReward);
        SetFloat(manager, "enemySideHitPenalty", StaticAndroidStompSideHitPenalty);
        SetFloat(manager, "finalCompletionReward", 10f);
        SetFloat(manager, "prematureGoalPenalty", StaticAndroidStompLockedGoalPenalty);
        SetBool(manager, "endEpisodeOnPrematureGoal", true);
        SetBool(manager, "debugLogs", false);
        return manager;
    }

    private static ScoreAttackManager CreateMixedRandomWarmupManager(Transform parent)
    {
        ScoreAttackManager manager = CreateMixedWarmupManager(parent);
        manager.gameObject.name = "ScoreMaxOA_MixedRandomWarmup_Manager";

        // OnEpisodeBegin performs the manager reset first and the dedicated
        // randomizer then moves every ordered objective exactly once.
        SetBool(manager, "resetOnStart", false);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        return manager;
    }

    private static ScoreAttackManager CreateFinalRandomManager(Transform parent)
    {
        ScoreAttackManager manager = CreateMixedWarmupManager(parent);
        manager.gameObject.name = "ScoreMaxOA_FinalRandom_Manager";
        SetBool(manager, "resetOnStart", false);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetInt(manager, "minActiveCoins", 2);
        SetInt(manager, "maxActiveCoins", 4);
        SetInt(manager, "minActiveEnemies", 1);
        SetInt(manager, "maxActiveEnemies", 1);
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
        SetBool(
            agent,
            "enforceLowCoinRunGroundCollection",
            phase == EdgeRunnerObjectAwarePhase.LowCoinRun);
        SetBool(
            agent,
            "requireGroundedBetweenHighCoins",
            phase == EdgeRunnerObjectAwarePhase.HighCoinJump);
        SetFloat(agent, "sameJumpSecondCoinPenalty", HighCoinJumpSameJumpPenalty);
        SetBool(
            agent,
            "endEpisodeOnSameJumpSecondCoin",
            phase == EdgeRunnerObjectAwarePhase.HighCoinJump);
        SetFloat(agent, "missedCoinPenalty", -2f);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "lowCoinHeightThreshold", LowCoinRunHeightThreshold);
        SetFloat(agent, "lowCoinRunWindowX", 3f);
        SetFloat(agent, "highCoinJumpWindowX", 2.25f);
        SetFloat(agent, "lowCoinGroundApproachReward", 0.01f);
        SetFloat(agent, "lowCoinGroundedAlignmentReward", 0.005f);
        SetFloat(
            agent,
            "lowCoinUnnecessaryJumpPenalty",
            phase == EdgeRunnerObjectAwarePhase.LowCoinRun
                ? LowCoinRunJumpPenalty
                : -0.02f);
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

    private static void ConfigureStaticAndroidAvoidPlayer(
        GameObject player,
        ScoreAttackManager manager)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetObjectReference(manager, "agent", agent);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "maskUselessJumps", false);
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.StaticAndroidAvoid);
        SetBool(agent, "enableObjectAwareRewardShaping", false);
        SetBool(agent, "enableMissedCoinEpisodeEnd", false);
        SetBool(agent, "enableContextualJumpMask", false);
        SetBool(agent, "enforceLowCoinRunGroundCollection", false);
        SetBool(agent, "requireGroundedBetweenHighCoins", false);
        SetBool(agent, "endEpisodeOnSameJumpSecondCoin", false);
        SetFloat(agent, "androidContextWindowX", 3.5f);
        SetFloat(agent, "androidVerticalTolerance", 1.5f);

        SetFloat(agent, "goalReward", 10f);
        SetFloat(agent, "stepPenalty", -0.0003f);
        SetFloat(agent, "progressRewardScale", 0.05f);
        SetFloat(agent, "maxProgressRewardPerStep", 0.05f);
        SetFloat(agent, "milestoneReward", 0.02f);
        SetFloat(agent, "backtrackPenalty", -0.006f);
        SetFloat(agent, "jumpPenalty", -0.0002f);
        SetFloat(agent, "idleJumpPenalty", -0.01f);
        SetFloat(agent, "flatGroundJumpPenalty", 0f);
        SetFloat(agent, "earlyGapJumpPenalty", 0f);
        SetFloat(agent, "uselessJumpPenalty", 0f);
        SetFloat(agent, "gapJumpReward", 0f);
        SetFloat(agent, "gapLandingReward", 0f);
        SetFloat(agent, "lowMomentumJumpPenalty", 0f);
        SetFloat(agent, "forwardActionReward", 0.003f);
        SetFloat(agent, "forwardVelocityReward", 0.002f);
        SetFloat(agent, "idlePenalty", -0.002f);
        SetFloat(agent, "wrongDirectionActionPenalty", -0.006f);
        SetFloat(agent, "distanceProgressRewardScale", 0.08f);
        SetFloat(agent, "maxDistanceProgressReward", 0.08f);
        SetFloat(agent, "distanceRegressionPenaltyScale", 0.04f);
        SetFloat(agent, "maxDistanceRegressionPenalty", -0.04f);
        SetFloat(agent, "noProgressTimeLimit", 10f);
        SetFloat(agent, "stuckTimeLimit", 10f);
        SetFloat(agent, "maxEpisodeTime", 45f);

        SetBool(agent, "debugObjectAwareObservationCount", false);
        SetBool(agent, "debugObjectAwareNextObjective", false);
        SetBool(agent, "debugObjectAwareJumpContext", false);
        SetBool(agent, "debugObjectAwareGizmos", false);
    }

    private static void ConfigureStaticAndroidStompPlayer(
        GameObject player,
        ScoreAttackManager manager)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetObjectReference(manager, "agent", agent);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "maskUselessJumps", false);
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.StaticAndroidStomp);
        SetBool(agent, "enableObjectAwareRewardShaping", true);
        SetBool(agent, "enableMissedCoinEpisodeEnd", false);
        SetBool(agent, "enableContextualJumpMask", false);
        SetBool(agent, "enforceLowCoinRunGroundCollection", false);
        SetBool(agent, "requireGroundedBetweenHighCoins", false);
        SetBool(agent, "endEpisodeOnSameJumpSecondCoin", false);
        SetFloat(agent, "androidContextWindowX", StaticAndroidStompWindowRange);
        SetFloat(agent, "androidVerticalTolerance", 1.5f);
        SetFloat(agent, "enemyApproachReward", StaticAndroidStompApproachReward);
        SetFloat(agent, "enemyStompWindowReward", StaticAndroidStompWindowReward);
        SetFloat(agent, "enemyStompWindowHorizontalRange", StaticAndroidStompWindowRange);
        SetFloat(agent, "missedEnemyPenalty", StaticAndroidStompMissedPenalty);
        SetFloat(agent, "missedEnemyForwardMargin", StaticAndroidStompMissedForwardMargin);
        SetBool(agent, "endEpisodeOnMissedEnemy", true);
        SetFloat(agent, "jumpSpamPenalty", -0.01f);

        // The Android is the curriculum objective until stomped; disable Goal progress
        // shaping so the policy is not rewarded for simply running past it.
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

    private static void ConfigureMixedWarmupPlayer(
        GameObject player,
        ScoreAttackManager manager)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetObjectReference(manager, "agent", agent);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "maskUselessJumps", false);
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.MixedWarmup);
        SetBool(agent, "enableObjectAwareRewardShaping", true);
        SetBool(agent, "enableMissedCoinEpisodeEnd", true);
        SetBool(agent, "enableContextualJumpMask", true);
        SetBool(agent, "enforceLowCoinRunGroundCollection", false);
        SetBool(agent, "requireGroundedBetweenHighCoins", false);
        SetBool(agent, "endEpisodeOnSameJumpSecondCoin", false);
        SetFloat(agent, "lowCoinHeightThreshold", LowCoinRunHeightThreshold);
        SetFloat(agent, "lowCoinRunWindowX", 3f);
        SetFloat(agent, "highCoinJumpWindowX", 2.25f);
        SetFloat(agent, "androidContextWindowX", StaticAndroidStompWindowRange);
        SetFloat(agent, "androidVerticalTolerance", 1.5f);
        SetFloat(agent, "lowCoinGroundApproachReward", 0.01f);
        SetFloat(agent, "lowCoinGroundedAlignmentReward", 0.005f);
        SetFloat(agent, "lowCoinUnnecessaryJumpPenalty", LowCoinRunJumpPenalty);
        SetFloat(agent, "highCoinApproachReward", 0.01f);
        SetFloat(agent, "highCoinJumpCueReward", 0.04f);
        SetFloat(agent, "earlyJumpPenalty", -0.01f);
        SetFloat(agent, "jumpSpamPenalty", -0.01f);
        SetFloat(agent, "missedCoinPenalty", -2f);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "enemyApproachReward", StaticAndroidStompApproachReward);
        SetFloat(agent, "enemyStompWindowReward", StaticAndroidStompWindowReward);
        SetFloat(agent, "enemyStompWindowHorizontalRange", StaticAndroidStompWindowRange);
        SetFloat(agent, "missedEnemyPenalty", StaticAndroidStompMissedPenalty);
        SetFloat(agent, "missedEnemyForwardMargin", StaticAndroidStompMissedForwardMargin);
        SetBool(agent, "endEpisodeOnMissedEnemy", true);

        // Ordered ObjectAware objectives provide progress shaping. Keep the base Goal
        // shaping neutral until every coin and the Android are complete.
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
        SetFloat(agent, "noProgressTimeLimit", 20f);
        SetFloat(agent, "stuckTimeLimit", 20f);
        SetFloat(agent, "maxEpisodeTime", 90f);

        SetBool(agent, "debugObjectAwareObservationCount", false);
        SetBool(agent, "debugObjectAwareNextObjective", false);
        SetBool(agent, "debugObjectAwareJumpContext", false);
        SetBool(agent, "debugObjectAwareGizmos", false);
    }

    private static void ConfigureMixedRandomWarmupPlayer(
        GameObject player,
        ScoreAttackManager manager,
        ScoreMaxOAMixedRandomWarmupRandomizer randomizer)
    {
        ConfigureMixedWarmupPlayer(player, manager);
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.MixedRandomWarmup);
        SetObjectReference(agent, "mixedRandomWarmupRandomizer", randomizer);
        SetBool(agent, "requireGroundedLowCoin", true);
        SetFloat(agent, "airborneLowCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnAirborneLowCoin", true);
        SetBool(agent, "requireGroundedBetweenLowAndHigh", true);
        SetFloat(agent, "sameJumpHighCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnSameJumpHighCoin", true);
    }

    private static void ConfigureMixedRandomWarmupRandomizer(
        ScoreMaxOAMixedRandomWarmupRandomizer randomizer,
        ScoreAttackCoin lowCoin,
        ScoreAttackCoin highCoin,
        ScoreAttackAndroid android,
        Transform goal)
    {
        SetObjectReference(randomizer, "lowCoin", lowCoin);
        SetObjectReference(randomizer, "highCoin", highCoin);
        SetObjectReference(randomizer, "android", android);
        SetObjectReference(randomizer, "goal", goal);
        SetVector2(
            randomizer,
            "lowCoinXRange",
            new Vector2(MixedRandomWarmupLowCoinMinX, MixedRandomWarmupLowCoinMaxX));
        SetVector2(
            randomizer,
            "highCoinXRange",
            new Vector2(MixedRandomWarmupHighCoinMinX, MixedRandomWarmupHighCoinMaxX));
        SetVector2(
            randomizer,
            "androidXRange",
            new Vector2(MixedRandomWarmupAndroidMinX, MixedRandomWarmupAndroidMaxX));
        SetVector2(
            randomizer,
            "goalXRange",
            new Vector2(MixedRandomWarmupGoalMinX, MixedRandomWarmupGoalMaxX));
        SetFloat(randomizer, "lowCoinY", MixedWarmupLowCoinY);
        SetFloat(randomizer, "highCoinY", MixedWarmupHighCoinY);
        SetFloat(randomizer, "androidY", 1.02f);
        SetFloat(randomizer, "goalY", 1.2f);
        SetFloat(
            randomizer,
            "minimumLowCoinToHighCoinDistance",
            MixedRandomWarmupMinimumLowCoinHighCoinDistance);
        SetFloat(
            randomizer,
            "minimumHighCoinToAndroidDistance",
            MixedRandomWarmupMinimumHighCoinAndroidDistance);
        SetBool(randomizer, "debugObjectAwareMixedRandomPositions", false);
    }

    private static void ConfigureFinalRandomPlayer(
        GameObject player,
        ScoreAttackManager manager,
        ScoreMaxOAFinalRandomizer randomizer)
    {
        ConfigureMixedWarmupPlayer(player, manager);
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.FinalRandom);
        SetObjectReference(agent, "finalRandomizer", randomizer);
        SetBool(agent, "requireGroundedLowCoin", true);
        SetFloat(agent, "airborneLowCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnAirborneLowCoin", true);
        SetBool(agent, "requireGroundedBetweenLowAndHigh", true);
        SetFloat(agent, "sameJumpHighCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnSameJumpHighCoin", true);
        SetBool(agent, "enableAntiLedgeStuckFailSafe", true);
        SetFloat(agent, "ledgeStuckGraceTime", FinalRandomLedgeStuckGraceTime);
        SetFloat(agent, "ledgeStuckMinYBelowGround", FinalRandomLedgeStuckMinYBelowGround);
        SetFloat(agent, "ledgeStuckMaxVelocity", FinalRandomLedgeStuckMaxVelocity);
        SetFloat(agent, "ledgeStuckProgressEpsilon", FinalRandomLedgeStuckProgressEpsilon);
        SetFloat(agent, "ledgeStuckPenalty", FinalRandomLedgeStuckPenalty);
        SetBool(agent, "debugAntiLedgeStuck", false);

        PhysicsMaterial2D noFrictionMaterial =
            AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(AgentNoFrictionMaterialPath);
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        if (noFrictionMaterial == null || playerCollider == null)
        {
            throw new System.InvalidOperationException(
                "FinalRandom requires Agent_NoFriction and the player BoxCollider2D.");
        }

        playerCollider.sharedMaterial = noFrictionMaterial;
    }

    private static void ConfigureFinalRandomizer(
        ScoreMaxOAFinalRandomizer randomizer,
        ScoreAttackManager manager,
        ScoreAttackCoin[] lowCoins,
        ScoreAttackCoin[] highCoins,
        ScoreAttackAndroid android,
        Transform goal,
        Transform startPlatform,
        Transform lowPlatform,
        Transform highPlatform,
        Transform finalPlatform)
    {
        SetObjectReference(randomizer, "manager", manager);
        SetObjectArray(randomizer, "lowCoins", lowCoins);
        SetObjectArray(randomizer, "highCoins", highCoins);
        SetObjectReference(randomizer, "android", android);
        SetObjectReference(randomizer, "goal", goal);
        SetObjectReference(randomizer, "startPlatform", startPlatform);
        SetObjectReference(randomizer, "lowPlatform", lowPlatform);
        SetObjectReference(randomizer, "highRecoveryPlatform", highPlatform);
        SetObjectReference(randomizer, "finalPlatform", finalPlatform);
        SetVector2(
            randomizer,
            "firstGapWidthRange",
            new Vector2(FinalRandomFirstGapMin, FinalRandomFirstGapMax));
        SetVector2(
            randomizer,
            "secondGapWidthRange",
            new Vector2(FinalRandomSecondGapMin, FinalRandomSecondGapMax));
        SetFloat(randomizer, "startPlatformLeftX", FinalRandomStartPlatformLeftX);
        SetFloat(randomizer, "startPlatformWidth", FinalRandomStartPlatformWidth);
        SetFloat(randomizer, "lowPlatformWidth", FinalRandomRecoveryPlatformWidth);
        SetFloat(randomizer, "highRecoveryPlatformWidth", FinalRandomHighPlatformWidth);
        SetFloat(randomizer, "finalPlatformWidth", FinalRandomFinalPlatformWidth);
        SetFloat(randomizer, "finalPlatformOverlap", 0.5f);
        SetFloat(randomizer, "platformCenterY", -0.2f);
        SetFloat(randomizer, "platformHeight", 0.4f);
        SetFloat(randomizer, "lowCoinY", MixedWarmupLowCoinY);
        SetFloat(randomizer, "highCoinY", MixedWarmupHighCoinY);
        SetFloat(randomizer, "androidY", 1.02f);
        SetFloat(randomizer, "goalY", 1.2f);
        SetFloat(randomizer, "objectiveXJitter", 0.25f);
        SetFloat(
            randomizer,
            "minimumLowCoinToHighCoinDistance",
            FinalRandomMinimumLowHighDistance);
        SetFloat(
            randomizer,
            "minimumHighCoinToAndroidDistance",
            FinalRandomMinimumHighAndroidDistance);
        SetFloat(
            randomizer,
            "minimumAndroidToGoalDistance",
            FinalRandomMinimumAndroidGoalDistance);
        SetFloat(randomizer, "playerStartX", FinalRandomPlayerStartX);
        SetVector2(randomizer, "firstLowCoinXRange", new Vector2(4f, 6f));
        SetVector2(randomizer, "secondLowCoinXRange", new Vector2(8f, 10f));
        SetFloat(
            randomizer,
            "minFlatRunBeforeLowCoin",
            FinalRandomMinFlatRunBeforeLowCoin);
        SetFloat(
            randomizer,
            "minFlatRunAfterLowCoin",
            FinalRandomMinFlatRunAfterLowCoin);
        SetFloat(
            randomizer,
            "minLowCoinDistanceFromGapEdge",
            FinalRandomMinLowCoinGapEdgeDistance);
        SetFloat(
            randomizer,
            "minLowCoinDistanceFromLandingZone",
            FinalRandomMinLowCoinLandingZoneDistance);
        SetBool(randomizer, "firstLowCoinSafe", true);
        SetBool(randomizer, "debugObjectAwareFinalRandomPositions", false);
    }

    private static GameObject CreateLockedGoal(
        string name,
        Vector3 position,
        ScoreAttackManager manager)
    {
        GameObject goal = CreateGoal(position);
        goal.name = name;
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

    private static ScoreAttackCoin CreateCoin(
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
        return coinScript;
    }

    private static GameObject CreateStaticAndroid(
        Transform parent,
        string name,
        Vector3 position,
        Sprite fallbackSprite,
        ScoreAttackManager manager)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
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

        DisableDemoAndroidScripts(android);
        RemoveAndroidPatrol(android);

        ScoreAttackAndroid androidComponent = android.GetComponent<ScoreAttackAndroid>();
        if (androidComponent == null)
        {
            androidComponent = android.AddComponent<ScoreAttackAndroid>();
        }

        androidComponent.enabled = true;
        androidComponent.SetManager(manager);
        SetFloat(androidComponent, "stompHeightOffset", 0.10f);
        SetFloat(androidComponent, "stompTopTolerance", 0.45f);
        SetBool(androidComponent, "debugLogs", false);
        return android;
    }

    private static GameObject CreateFallbackAndroid(Sprite sprite)
    {
        GameObject android = new GameObject("StaticAndroidAvoid_Fallback");
        SpriteRenderer renderer = android.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.58f, 0.64f, 0.68f, 1f);
        renderer.sortingOrder = 7;
        android.AddComponent<BoxCollider2D>();
        android.AddComponent<Rigidbody2D>();
        return android;
    }

    private static void RemoveAndroidPatrol(GameObject android)
    {
        DemoAndroidPatrol[] patrols = android.GetComponentsInChildren<DemoAndroidPatrol>(true);
        for (int i = 0; i < patrols.Length; i++)
        {
            if (patrols[i] != null)
            {
                Object.DestroyImmediate(patrols[i]);
            }
        }
    }

    private static void DisableDemoAndroidScripts(GameObject android)
    {
        DemoEnemyHazard[] hazards = android.GetComponentsInChildren<DemoEnemyHazard>(true);
        for (int i = 0; i < hazards.Length; i++)
        {
            hazards[i].enabled = false;
        }

        StompableAndroidEnemy[] stompEnemies =
            android.GetComponentsInChildren<StompableAndroidEnemy>(true);
        for (int i = 0; i < stompEnemies.Length; i++)
        {
            stompEnemies[i].enabled = false;
        }

        StompableAndroidStompZone[] stompZones =
            android.GetComponentsInChildren<StompableAndroidStompZone>(true);
        for (int i = 0; i < stompZones.Length; i++)
        {
            stompZones[i].enabled = false;
        }

        StompableAndroidSideHazard[] sideHazards =
            android.GetComponentsInChildren<StompableAndroidSideHazard>(true);
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

    private static Transform CreatePlatform(
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
        return platform.transform;
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

    private static void SetObjectArray(
        Object target,
        string propertyName,
        Object[] values)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            throw new System.InvalidOperationException(
                $"Serialized array '{propertyName}' was not found on {target.name}.");
        }

        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

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

    private static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on {target.name}.");
        }

        property.vector2Value = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

}
