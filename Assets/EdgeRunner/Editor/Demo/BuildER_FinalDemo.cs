using System.Collections.Generic;
using System.IO;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_FinalDemo
{
    private const string SceneFolder = "Assets/EdgeRunner/Scenes/DemoFinal";
    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidPrefabPath = "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";
    private const string FinalLongReferenceScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_FinalLongChallenge.unity";
    private const string NoFrictionPath = "Assets/EdgeRunner/Physics/Agent_NoFriction.physicsMaterial2D";
    private const string SpeedModelPath = "Assets/EdgeRunner/ML/Models/FinalCandidates/FINAL_GoalRunner_Base_EasyBridge.onnx";
    private const string MaxScoreModelPath = "Assets/EdgeRunner/ML/Models/FinalCandidates/FINAL_ScoreMaxOA_FinalLongChallenge_BEST_200k.onnx";
    private const string MenuLogoPath = "Assets/Resources/DemoFinal/EdgeRunners_Logo.jpg";
    private const string OutputPath = "Builds/EdgeRunner_FinalDemo/EdgeRunner_FinalDemo.exe";

    private static readonly string[] SceneNames =
    {
        FinalDemoController.MenuScene,
        "ER_FinalDemo_SpeedRun_Easy",
        "ER_FinalDemo_SpeedRun_Normal",
        "ER_FinalDemo_SpeedRun_Hard",
        "ER_FinalDemo_MaxScore_Easy",
        "ER_FinalDemo_MaxScore_Normal",
        "ER_FinalDemo_MaxScore_Hard"
    };

    private static readonly float[] SpeedGoalX = { 110f, 163f, 211f };

    private readonly struct PlatformSpec
    {
        public readonly string Name;
        public readonly float X;
        public readonly float TopY;
        public readonly float Width;

        public PlatformSpec(string name, float x, float topY, float width)
        {
            Name = name;
            X = x;
            TopY = topY;
            Width = width;
        }
    }

    private sealed class MaxScoreObjectives
    {
        public readonly Vector2[] LowCoins = new Vector2[4];
        public readonly Vector2[] HighCoins = new Vector2[3];
        public readonly Vector2[] Androids = new Vector2[2];
        public float GoalX;
        public float GoalY;
        public float HighCoin01LandingGateX;
        public float Android01LandingGateX;
        public float Android02LandingGateX;
    }

    [MenuItem("EdgeRunner/Demo Final/Build All Levels %#f")]
    public static void BuildAllLevels()
    {
        BuildMenuAndSpeedRunLevels();
        RequireAsset<ModelAsset>(MaxScoreModelPath);

        BuildMaxScoreSceneFromReference(3, CreateMaxEasy(), CreateMaxEasyObjectives(), "Sequência funcional preservada, com cenário vertical e patrulha visual em background.");
        BuildMaxScoreSceneFromReference(4, CreateMaxNormal(), CreateMaxNormalObjectives(), "Ritmo funcional preservado, mais camadas visuais e recuperações bem marcadas.");
        BuildMaxScoreSceneFromReference(5, CreateMaxHard(), CreateMaxHardObjectives(), "Sete powercells, dois stomps e cenário neon mais variado sem alterar a sequência.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FINAL DEMO] Built menu and six handcrafted levels.");
    }

    [MenuItem("EdgeRunner/Demo Final/Rebuild Menu + SpeedRun")]
    public static void BuildMenuAndSpeedRunLevels()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes", "DemoFinal");
        RequireAsset<GameObject>(PlayerPrefabPath);
        RequireAsset<ModelAsset>(SpeedModelPath);
        RequireAsset<Texture2D>(MenuLogoPath);
        ValidateSpeedLayoutSet();

        BuildMenuScene();
        BuildSpeedScene(0, CreateSpeedEasy(), SpeedGoalX[0], 0.55f, "110 m · gaps simples, ondulação suave e zonas largas de recuperação.");
        BuildSpeedScene(1, CreateSpeedNormal(), SpeedGoalX[1], 0.55f, "163 m · subidas e descidas curtas, gaps médios e ritmo mais variado.");
        BuildSpeedScene(2, CreateSpeedHard(), SpeedGoalX[2], 0.75f, "211 m · maior verticalidade controlada e sequências de salto mais longas.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FINAL DEMO] Rebuilt menu and three SpeedRun levels.");
    }

    [MenuItem("EdgeRunner/Demo Final/Build Windows Application %#g")]
    public static void BuildWindowsApplication()
    {
        BuildAllLevels();
        string[] scenes = new string[SceneNames.Length];
        for (int i = 0; i < SceneNames.Length; i++)
        {
            scenes[i] = $"{SceneFolder}/{SceneNames[i]}.unity";
            if (!File.Exists(scenes[i]))
            {
                throw new FileNotFoundException("Final demo scene is missing.", scenes[i]);
            }
        }

        string fullOutputPath = Path.GetFullPath(OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));

        int oldWidth = PlayerSettings.defaultScreenWidth;
        int oldHeight = PlayerSettings.defaultScreenHeight;
        FullScreenMode oldMode = PlayerSettings.fullScreenMode;
        bool oldResizable = PlayerSettings.resizableWindow;
        string oldProductName = PlayerSettings.productName;
        NamedBuildTarget standaloneTarget = NamedBuildTarget.Standalone;
        Texture2D[] oldStandaloneIcons =
            PlayerSettings.GetIcons(standaloneTarget, IconKind.Application);
        try
        {
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.productName = "EdgeRunners";
            Texture2D logoIcon = RequireAsset<Texture2D>(MenuLogoPath);
            int[] iconSizes = PlayerSettings.GetIconSizes(standaloneTarget, IconKind.Application);
            if (iconSizes.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "Standalone Windows reported no application icon sizes.");
            }
            Texture2D[] applicationIcons = new Texture2D[iconSizes.Length];
            for (int i = 0; i < applicationIcons.Length; i++)
            {
                applicationIcons[i] = logoIcon;
            }
            PlayerSettings.SetIcons(standaloneTarget, applicationIcons, IconKind.Application);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"Final demo build failed: {report.summary.result}, errors={report.summary.totalErrors}.");
            }

            Debug.Log($"[FINAL DEMO BUILD] Created '{fullOutputPath}' ({report.summary.totalSize} bytes).");
        }
        finally
        {
            PlayerSettings.defaultScreenWidth = oldWidth;
            PlayerSettings.defaultScreenHeight = oldHeight;
            PlayerSettings.fullScreenMode = oldMode;
            PlayerSettings.resizableWindow = oldResizable;
            PlayerSettings.productName = oldProductName;
            PlayerSettings.SetIcons(standaloneTarget, oldStandaloneIcons, IconKind.Application);
            AssetDatabase.SaveAssets();
        }
    }

    public static void BuildAllLevelsFromCommandLine()
    {
        BuildAllLevels();
    }

    public static void BuildMenuAndSpeedRunLevelsFromCommandLine()
    {
        BuildMenuAndSpeedRunLevels();
    }

    public static void BuildWindowsApplicationFromCommandLine()
    {
        BuildWindowsApplication();
    }

    private static void BuildMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateBackdrop(0f, 90f, 0);
        CreateMenuCamera();
        GameObject controllerObject = new GameObject("FinalDemoController");
        controllerObject.AddComponent<FinalDemoController>().Configure(-1, string.Empty, string.Empty);
        SaveScene(scene, 0);
    }

    private static void BuildSpeedScene(
        int levelIndex,
        PlatformSpec[] platforms,
        float goalX,
        float goalY,
        string description)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject(SceneNames[levelIndex + 1]);
        Sprite sprite = GetSharedSprite();
        CreateBackdrop(goalX * 0.5f, goalX + 35f, levelIndex + 1);
        CreatePlatforms(root.transform, platforms, sprite, false);
        CreateSecondaryPlatforms(root.transform, platforms, sprite, levelIndex);

        FinalDemoController controller = CreateLevelController(
            levelIndex,
            "FINAL_GoalRunner_Base_EasyBridge · EdgeRunnerV5 · 55 observações",
            description);
        int visualCollectibleCount = CreateSpeedCollectibles(
            root.transform,
            platforms,
            sprite,
            levelIndex,
            controller);
        controller.ConfigureVisualCollectibles(visualCollectibleCount);
        CreateVisualPatrolAndroids(root.transform, platforms, sprite, levelIndex, false);
        GameObject goal = CreateGoal(new Vector2(goalX, goalY + 1.2f), controller, null);
        GameObject player = CreateSpeedPlayer(new Vector3(0f, 1.15f, 0f), goal.transform);
        ConfigureSprintVisual(player);
        CreateCamera(player.transform);
        CreateDeathZone(goalX * 0.5f, goalX + 50f);

        ValidateSpeedScene(scene, player, goalX, platforms);
        SaveScene(scene, levelIndex + 1);
    }

    private static void BuildMaxScoreScene(
        int levelIndex,
        PlatformSpec[] platforms,
        MaxScoreObjectives objectives,
        string description)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject(SceneNames[levelIndex + 1]);
        Sprite sprite = GetSharedSprite();
        CreateBackdrop(objectives.GoalX * 0.5f, objectives.GoalX + 35f, levelIndex + 1);
        CreatePlatforms(root.transform, platforms, sprite, true);
        CreateSecondaryPlatforms(root.transform, platforms, sprite, levelIndex);

        FinalDemoController controller = CreateLevelController(
            levelIndex,
            "FINAL_ScoreMaxOA_FinalLongChallenge_BEST_200k · ObjectAware · 111 observações",
            description);
        ScoreAttackManager manager = CreateMaxScoreManager(root.transform);
        GameObject goal = CreateGoal(
            new Vector2(objectives.GoalX, objectives.GoalY + 1.2f), controller, manager);
        GameObject player = CreateObjectAwarePlayer(
            new Vector3(0f, 1.15f, 0f), goal.transform, manager, objectives);
        SetObjectReference(manager, "agent", player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>());
        SetObjectReference(manager, "goal", goal.transform);

        for (int i = 0; i < objectives.LowCoins.Length; i++)
        {
            CreateCoin(root.transform, $"FinalLongChallenge_LowCoin_{i + 1:00}", objectives.LowCoins[i], sprite, manager);
        }
        for (int i = 0; i < objectives.HighCoins.Length; i++)
        {
            CreateCoin(root.transform, $"FinalLongChallenge_HighCoin_{i + 1:00}", objectives.HighCoins[i], sprite, manager);
        }
        for (int i = 0; i < objectives.Androids.Length; i++)
        {
            CreateInteractiveAndroid(
                root.transform,
                $"FinalLongChallenge_Android_{i + 1:00}",
                objectives.Androids[i],
                sprite,
                manager);
        }

        CreateCamera(player.transform);
        CreateDeathZone(objectives.GoalX * 0.5f, objectives.GoalX + 50f);
        ValidateMaxScoreScene(scene, player, manager, objectives, platforms);
        SaveScene(scene, levelIndex + 1);
    }

    private static void BuildMaxScoreSceneFromReference(
        int levelIndex,
        PlatformSpec[] visualPlatforms,
        MaxScoreObjectives objectives,
        string description)
    {
        Scene scene = EditorSceneManager.OpenScene(FinalLongReferenceScenePath, OpenSceneMode.Single);
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            Object.FindAnyObjectByType<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        ScoreAttackManager manager = Object.FindAnyObjectByType<ScoreAttackManager>();
        ScoreAttackGoalLock goalLock = Object.FindAnyObjectByType<ScoreAttackGoalLock>();
        if (agent == null || manager == null || goalLock == null)
        {
            throw new System.InvalidOperationException(
                "FinalLong reference scene is missing its validated agent, manager, or GoalLock.");
        }

        ApplyReferencePlatformDifficulty(levelIndex);
        NormalizeMaxScoreCollectibles();
        ApplyUnifiedMaxScorePowerCellVisuals();
        ConfigureBehavior(agent.gameObject, "EdgeRunnerV5ScoreMaxObjectAware", 111, MaxScoreModelPath);
        EnableDecisions(agent.gameObject);
        ConfigureMaxScoreStartup(agent);

        FinalDemoController controller = CreateLevelController(
            levelIndex,
            "FINAL_ScoreMaxOA_FinalLongChallenge_BEST_200k · ObjectAware · 111 observações",
            description);
        GameObject observerZone = new GameObject("FinalDemoGoalObserverZone");
        observerZone.transform.position = goalLock.transform.position + new Vector3(-1.5f, 0f, 0f);
        BoxCollider2D observerCollider = observerZone.AddComponent<BoxCollider2D>();
        observerCollider.isTrigger = true;
        observerCollider.size = new Vector2(3f, 4f);
        FinalDemoGoalObserver observer = observerZone.AddComponent<FinalDemoGoalObserver>();
        observer.Configure(controller, manager);

        GameObject artRoot = new GameObject("FinalDemo_Handcrafted_Art");
        Sprite sprite = GetSharedSprite();
        CreateBackdrop(objectives.GoalX * 0.5f, objectives.GoalX + 35f, levelIndex + 1);
        CreateSecondaryPlatforms(artRoot.transform, visualPlatforms, sprite, levelIndex);
        CreateVisualPatrolAndroids(artRoot.transform, visualPlatforms, sprite, levelIndex, true);

        Camera camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            camera.backgroundColor = new Color(0.04f, 0.065f, 0.11f, 1f);
            DemoCameraFollow2D follow = camera.GetComponent<DemoCameraFollow2D>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<DemoCameraFollow2D>();
            }
            follow.SetTarget(agent.transform);
            SetVector3(follow, "offset", new Vector3(4.8f, 2.8f, -10f));
            SetFloat(follow, "smoothTime", 0.14f);
        }

        ValidateMaxScoreScene(scene, agent.gameObject, manager, objectives, visualPlatforms);
        SaveScene(scene, levelIndex + 1);
    }

    private static void ApplyReferencePlatformDifficulty(int levelIndex)
    {
        string[] names =
        {
            "FinalLongChallenge_Zone1_Start",
            "FinalLongChallenge_Zone1_Recovery",
            "FinalLongChallenge_Zone2_AndroidRecovery",
            "FinalLongChallenge_Zone4_AndroidHigh",
            "FinalLongChallenge_FinalRecovery",
            "FinalLongChallenge_GoalPlatform"
        };
        float[][] widths =
        {
            new[] { 17f, 20f, 43f, 33f, 9f, 11f },
            new[] { 16f, 19f, 41.6f, 30.5f, 8f, 9.7f },
            new[] { 16f, 18f, 42f, 30f, 7.5f, 9f }
        };
        int difficulty = Mathf.Clamp(levelIndex - 3, 0, 2);
        for (int i = 0; i < names.Length; i++)
        {
            GameObject platform = GameObject.Find(names[i]);
            if (platform == null)
            {
                throw new System.InvalidOperationException(
                    $"FinalLong reference platform is missing: {names[i]}.");
            }
            Vector3 scale = platform.transform.localScale;
            scale.x = widths[difficulty][i];
            platform.transform.localScale = scale;
        }
    }

    private static void NormalizeMaxScoreCollectibles()
    {
        ScoreAttackCoin[] coins = Object.FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);
        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            bool lowCoin = coin.name.StartsWith("FinalLongChallenge_LowCoin_", System.StringComparison.Ordinal);
            bool highCoin = coin.name.StartsWith("FinalLongChallenge_HighCoin_", System.StringComparison.Ordinal);
            if (!lowCoin && !highCoin)
            {
                continue;
            }

            if (!TryGetSupportingPlatformTop(coin.transform.position.x, coin.transform.position.y, out float topY))
            {
                throw new System.InvalidOperationException(
                    $"Could not find a supporting platform below MaxScore coin '{coin.name}'.");
            }
            Vector3 position = coin.transform.position;
            position.y = topY + (lowCoin ? 0.95f : 2.65f);
            coin.transform.position = position;
        }
        Physics2D.SyncTransforms();
    }

    private static void ApplyUnifiedMaxScorePowerCellVisuals()
    {
        Sprite sprite = GetSharedSprite();
        ScoreAttackCoin[] coins = Object.FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);
        for (int i = 0; i < coins.Length; i++)
        {
            ConfigurePowerCellVisual(coins[i].gameObject, sprite, 0.58f, 8);
        }
        Physics2D.SyncTransforms();
    }

    private static FinalDemoController CreateLevelController(int index, string model, string description)
    {
        GameObject controllerObject = new GameObject("FinalDemoController");
        FinalDemoController controller = controllerObject.AddComponent<FinalDemoController>();
        controller.Configure(index, model, description);
        return controller;
    }

    private static GameObject CreateSpeedPlayer(Vector3 position, Transform goal)
    {
        GameObject player = InstantiatePrefab(PlayerPrefabPath, "Player_V5_FinalDemo_SpeedRun");
        player.transform.position = position;
        EdgeRunnerAgentV5 agent = player.GetComponent<EdgeRunnerAgentV5>();
        if (agent == null)
        {
            throw new System.InvalidOperationException("Player_V5 has no EdgeRunnerAgentV5.");
        }

        ConfigureBaseAgent(agent, player, goal, null);
        ConfigureBehavior(player, "EdgeRunnerV5", 55, SpeedModelPath);
        EnableDecisions(player);
        return player;
    }

    private static GameObject CreateObjectAwarePlayer(
        Vector3 position,
        Transform goal,
        ScoreAttackManager manager,
        MaxScoreObjectives objectives)
    {
        GameObject player = InstantiatePrefab(PlayerPrefabPath, "Player_V5_FinalDemo_ObjectAware");
        player.transform.position = position;
        EdgeRunnerAgentV5 baseAgent = player.GetComponent<EdgeRunnerAgentV5>();
        string serializedBase = baseAgent != null ? JsonUtility.ToJson(baseAgent) : string.Empty;
        Transform groundCheck = null;
        if (baseAgent != null)
        {
            groundCheck = new SerializedObject(baseAgent).FindProperty("groundCheck")?.objectReferenceValue as Transform;
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent = player.AddComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        if (!string.IsNullOrEmpty(serializedBase))
        {
            JsonUtility.FromJsonOverwrite(serializedBase, agent);
        }
        if (baseAgent != null)
        {
            Object.DestroyImmediate(baseAgent);
        }

        ConfigureBaseAgent(agent, player, goal, manager);
        SetObjectReference(agent, "groundCheck", groundCheck);
        SetObjectReference(agent, "objectAwareGoal", goal);
        SetObjectReference(agent, "objectAwareRigidbody", player.GetComponent<Rigidbody2D>());
        SetObjectReference(agent, "objectAwareScoreAttackManager", manager);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(agent, "maskUselessJumps", false);
        SetInt(agent, "objectAwarePhase", (int)EdgeRunnerObjectAwarePhase.FinalLongChallenge);
        SetBool(agent, "enableObjectAwareRewardShaping", true);
        SetBool(agent, "enableMissedCoinEpisodeEnd", true);
        SetBool(agent, "enableContextualJumpMask", true);
        SetBool(agent, "enforceLowCoinRunGroundCollection", false);
        SetBool(agent, "requireGroundedBetweenHighCoins", false);
        SetBool(agent, "endEpisodeOnSameJumpSecondCoin", false);
        SetFloat(agent, "lowCoinHeightThreshold", 0.45f);
        SetFloat(agent, "lowCoinRunWindowX", 3f);
        SetFloat(agent, "highCoinJumpWindowX", 2.25f);
        SetFloat(agent, "androidContextWindowX", 3.5f);
        SetFloat(agent, "androidVerticalTolerance", 1.5f);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "enemyStompWindowHorizontalRange", 3.5f);
        SetFloat(agent, "missedEnemyForwardMargin", 2.5f);
        SetBool(agent, "endEpisodeOnMissedEnemy", true);
        SetFloat(agent, "maxObjectiveDistance", 100f);
        SetBool(agent, "requireGroundedLowCoin", true);
        SetFloat(agent, "airborneLowCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnAirborneLowCoin", true);
        SetBool(agent, "requireGroundedBetweenLowAndHigh", true);
        SetFloat(agent, "sameJumpHighCoinPenalty", -2f);
        SetBool(agent, "endEpisodeOnSameJumpHighCoin", true);
        SetBool(agent, "enableHighCoinApproachDiscipline", true);
        SetFloat(agent, "highCoinEarlyJumpDistance", 4f);
        SetFloat(agent, "highCoinJumpWindowDistanceMin", 1f);
        SetFloat(agent, "highCoinJumpWindowDistanceMax", 3f);
        SetBool(agent, "enableFinalLongGroundedTraversalDiscipline", true);
        SetFloat(agent, "minSafeFlatDistanceForGroundedDiscipline", 3f);
        SetFloat(agent, "jumpPurposeWindowDistance", 3.5f);
        SetBool(agent, "enableFinalLongContextualJumpMask", true);
        SetFloat(agent, "finalLongJumpMaskSafeFlatDistance", 3f);
        SetFloat(agent, "finalLongJumpMaskHighCoinWindowMin", 1f);
        SetFloat(agent, "finalLongJumpMaskHighCoinWindowMax", 3.5f);
        SetFloat(agent, "finalLongJumpMaskAndroidWindow", 3.5f);
        SetFloat(agent, "finalLongPostLandingGroundedRunRequired", 1f);
        SetBool(agent, "enableAntiLedgeStuckFailSafe", true);
        SetFloat(agent, "ledgeStuckGraceTime", 0.5f);
        SetFloat(agent, "ledgeStuckMinYBelowGround", 0.25f);
        SetFloat(agent, "ledgeStuckMaxVelocity", 0.25f);
        SetFloat(agent, "ledgeStuckProgressEpsilon", 0.03f);
        SetFloat(agent, "ledgeStuckPenalty", -4f);
        SetFloat(agent, "finalLongHighCoin01LandingGateX", objectives.HighCoin01LandingGateX);
        SetFloat(agent, "finalLongAndroid01LandingGateX", objectives.Android01LandingGateX);
        SetFloat(agent, "finalLongAndroid02LandingGateX", objectives.Android02LandingGateX);
        SetFloat(agent, "noProgressTimeLimit", 30f);
        SetFloat(agent, "stuckTimeLimit", 30f);
        SetFloat(agent, "maxEpisodeTime", 180f);
        SetBool(agent, "debugObjectAwareObservationCount", false);
        SetBool(agent, "debugObjectAwareFinalLongValidation", false);
        SetBool(agent, "debugFinalLongFailureReason", false);

        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        if (noFriction != null && playerCollider != null)
        {
            playerCollider.sharedMaterial = noFriction;
        }
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        ConfigureBehavior(player, "EdgeRunnerV5ScoreMaxObjectAware", 111, MaxScoreModelPath);
        EnableDecisions(player);
        return player;
    }

    private static void ConfigureBaseAgent(
        EdgeRunnerAgentV5 agent,
        GameObject player,
        Transform goal,
        ScoreAttackManager manager)
    {
        SetObjectReference(agent, "rb", player.GetComponent<Rigidbody2D>());
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "gapGenerator", null);
        SetObjectReference(agent, "mixedLevelGenerator", null);
        SetObjectReference(agent, "evaluationManager", null);
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetBool(agent, "disableTrainingEpisodeEndsInDemo", false);
        SetBool(agent, "disableAgentMovementInDemo", false);
        SetBool(agent, "debugJump", false);
        SetBool(agent, "debugEpisodeStackTraces", false);
        SetBool(agent, "debugEpisodeResetReason", false);
        SetFloat(agent, "noProgressTimeLimit", 30f);
        SetFloat(agent, "stuckTimeLimit", 30f);
        SetFloat(agent, "maxEpisodeTime", 210f);
    }

    private static void ConfigureBehavior(GameObject player, string behaviorName, int observations, string modelPath)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            behavior = player.AddComponent<BehaviorParameters>();
        }

        ModelAsset model = RequireAsset<ModelAsset>(modelPath);
        SerializedObject serialized = new SerializedObject(behavior);
        serialized.FindProperty("m_BehaviorName").stringValue = behaviorName;
        serialized.FindProperty("m_BehaviorType").enumValueIndex = (int)BehaviorType.InferenceOnly;
        serialized.FindProperty("m_Model").objectReferenceValue = model;
        serialized.FindProperty("m_BrainParameters.VectorObservationSize").intValue = observations;
        serialized.FindProperty("m_BrainParameters.m_ActionSpec.m_NumContinuousActions").intValue = 0;
        SerializedProperty branches = serialized.FindProperty("m_BrainParameters.m_ActionSpec.BranchSizes");
        branches.arraySize = 3;
        branches.GetArrayElementAtIndex(0).intValue = 3;
        branches.GetArrayElementAtIndex(1).intValue = 2;
        branches.GetArrayElementAtIndex(2).intValue = 2;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnableDecisions(GameObject player)
    {
        DecisionRequester requester = player.GetComponent<DecisionRequester>();
        if (requester == null)
        {
            requester = player.AddComponent<DecisionRequester>();
        }
        requester.enabled = true;
    }

    private static void ConfigureMaxScoreStartup(EdgeRunnerAgentV5ScoreMaxObjectAware agent)
    {
        DecisionRequester requester = agent.GetComponent<DecisionRequester>();
        FinalDemoMaxScoreStartupGate gate = agent.GetComponent<FinalDemoMaxScoreStartupGate>();
        if (gate == null)
        {
            gate = agent.gameObject.AddComponent<FinalDemoMaxScoreStartupGate>();
        }
        gate.Configure(agent, requester);
    }

    private static void ConfigureSprintVisual(GameObject player)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        DemoSprintVisual sprintVisual = player.GetComponent<DemoSprintVisual>();
        if (sprintVisual == null)
        {
            sprintVisual = player.AddComponent<DemoSprintVisual>();
        }

        TrailRenderer trail = player.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = player.AddComponent<TrailRenderer>();
        }
        trail.time = 0.28f;
        trail.startWidth = 0.30f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.05f;
        trail.numCornerVertices = 3;
        trail.numCapVertices = 2;
        trail.autodestruct = false;
        trail.enabled = true;
        trail.emitting = false;
        trail.startColor = new Color(0.25f, 0.95f, 1f, 0.68f);
        trail.endColor = new Color(0.18f, 0.55f, 1f, 0f);
        Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (material != null)
        {
            trail.sharedMaterial = material;
        }

        sprintVisual.Configure(body, spriteRenderer, trail);
        SetFloat(sprintVisual, "sprintOnThreshold", 9.4f);
        SetFloat(sprintVisual, "sprintOffThreshold", 8.2f);
        SetFloat(sprintVisual, "minSprintVisualTime", 0.35f);
        SetBool(sprintVisual, "enableTrail", true);
    }

    private static ScoreAttackManager CreateMaxScoreManager(Transform parent)
    {
        GameObject managerObject = new GameObject("FinalDemo_MaxScore_Manager");
        managerObject.transform.SetParent(parent, false);
        ScoreAttackManager manager = managerObject.AddComponent<ScoreAttackManager>();
        SetBool(manager, "resetOnStart", true);
        SetBool(manager, "randomizeObjectPositionsOnReset", false);
        SetBool(manager, "requireEnemiesForGoal", true);
        SetBool(manager, "endEpisodeOnPrematureGoal", true);
        SetBool(manager, "preferForwardCoinObjectives", true);
        SetInt(manager, "minActiveCoins", 7);
        SetInt(manager, "maxActiveCoins", 7);
        SetInt(manager, "minActiveEnemies", 2);
        SetInt(manager, "maxActiveEnemies", 2);
        SetFloat(manager, "coinReward", 2f);
        SetFloat(manager, "enemyKillReward", 5f);
        SetFloat(manager, "enemySideHitPenalty", -6f);
        SetFloat(manager, "finalCompletionReward", 15f);
        SetFloat(manager, "prematureGoalPenalty", -2f);
        SetBool(manager, "debugLogs", false);
        return manager;
    }

    private static GameObject CreateGoal(
        Vector2 position,
        FinalDemoController controller,
        ScoreAttackManager manager)
    {
        GameObject goal = InstantiatePrefab(GoalPrefabPath, "Goal_FinalDemo");
        goal.transform.position = new Vector3(position.x, position.y, 0f);
        Collider2D collider = goal.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = goal.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        FinalDemoGoalObserver observer = goal.AddComponent<FinalDemoGoalObserver>();
        observer.Configure(controller, manager);
        if (manager != null)
        {
            ScoreAttackGoalLock goalLock = goal.AddComponent<ScoreAttackGoalLock>();
            goalLock.SetManager(manager);
        }
        return goal;
    }

    private static void CreateCoin(
        Transform parent,
        string name,
        Vector2 position,
        Sprite sprite,
        ScoreAttackManager manager)
    {
        GameObject coin = new GameObject(name);
        coin.transform.SetParent(parent, false);
        coin.transform.position = new Vector3(position.x, position.y, 0f);
        ConfigurePowerCellVisual(coin, sprite, 0.58f, 8);
        CircleCollider2D collider = coin.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.65f;
        ScoreAttackCoin component = coin.AddComponent<ScoreAttackCoin>();
        component.SetManager(manager);
        SetBool(component, "enableTriggerStayFallback", true);
        SetBool(component, "debugCoinCollection", false);
    }

    private static void CreateInteractiveAndroid(
        Transform parent,
        string name,
        Vector2 position,
        Sprite sprite,
        ScoreAttackManager manager)
    {
        GameObject android = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidPrefabPath) != null
            ? InstantiatePrefab(AndroidPrefabPath, name)
            : CreateSimpleAndroid(name, sprite, new Color(0.62f, 0.68f, 0.72f, 1f));
        android.transform.SetParent(parent, false);
        android.transform.position = new Vector3(position.x, position.y, 0f);
        android.transform.localScale = new Vector3(0.95f, 1.2f, 1f);

        DisableAndroidScripts(android);
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

        ScoreAttackAndroid scoreAndroid = android.GetComponent<ScoreAttackAndroid>();
        if (scoreAndroid == null)
        {
            scoreAndroid = android.AddComponent<ScoreAttackAndroid>();
        }
        scoreAndroid.enabled = true;
        scoreAndroid.SetManager(manager);
        SetFloat(scoreAndroid, "stompHeightOffset", 0.10f);
        SetFloat(scoreAndroid, "stompTopTolerance", 0.45f);
        SetBool(scoreAndroid, "debugLogs", false);
    }

    private static void DisableAndroidScripts(GameObject android)
    {
        foreach (DemoAndroidPatrol component in android.GetComponentsInChildren<DemoAndroidPatrol>(true))
            Object.DestroyImmediate(component);
        foreach (DemoEnemyHazard component in android.GetComponentsInChildren<DemoEnemyHazard>(true))
            component.enabled = false;
        foreach (StompableAndroidEnemy component in android.GetComponentsInChildren<StompableAndroidEnemy>(true))
            component.enabled = false;
        foreach (StompableAndroidStompZone component in android.GetComponentsInChildren<StompableAndroidStompZone>(true))
            component.enabled = false;
        foreach (StompableAndroidSideHazard component in android.GetComponentsInChildren<StompableAndroidSideHazard>(true))
            component.enabled = false;
        EdgeRunnerEnemyMarker marker = android.GetComponent<EdgeRunnerEnemyMarker>();
        if (marker != null) marker.SetAffectsAgent(false);
    }

    private static void CreatePlatforms(Transform parent, PlatformSpec[] specs, Sprite sprite, bool maxScore)
    {
        GameObject platformsRoot = new GameObject("HandcraftedPlatforms");
        platformsRoot.transform.SetParent(parent, false);
        for (int i = 0; i < specs.Length; i++)
        {
            PlatformSpec spec = specs[i];
            Color color = maxScore
                ? new Color(0.19f + (i % 3) * 0.015f, 0.22f + (i % 2) * 0.025f, 0.31f, 1f)
                : new Color(0.15f + (i % 3) * 0.018f, 0.23f, 0.29f + (i % 2) * 0.02f, 1f);
            CreatePlatform(platformsRoot.transform, spec, sprite, color);
            CreateVisualStrip(
                platformsRoot.transform,
                spec.Name + "_NeonTop",
                new Vector2(spec.X, spec.TopY + 0.055f),
                new Vector2(spec.Width - 0.25f, 0.09f),
                maxScore ? new Color(1f, 0.55f, 0.12f, 1f) : new Color(0.1f, 0.88f, 1f, 1f),
                sprite,
                3);
        }
    }

    private static void CreatePlatform(Transform parent, PlatformSpec spec, Sprite sprite, Color color)
    {
        GameObject platform = new GameObject(spec.Name);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(spec.X, spec.TopY - 0.22f, 0f);
        platform.transform.localScale = new Vector3(spec.Width, 0.44f, 1f);
        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = Vector2.one;
    }

    private static void CreateSecondaryPlatforms(Transform parent, PlatformSpec[] specs, Sprite sprite, int seed)
    {
        GameObject secondary = new GameObject("Secondary_Decorative_Platforms");
        secondary.transform.SetParent(parent, false);
        for (int i = 1; i < specs.Length; i += 2)
        {
            PlatformSpec spec = specs[i];
            float y = spec.TopY + 3.6f + ((i + seed) % 3) * 0.8f;
            float x = spec.X + (((i + seed) % 2 == 0) ? -1.5f : 1.5f);
            CreateVisualStrip(
                secondary.transform,
                $"Skywalk_{i:00}",
                new Vector2(x, y),
                new Vector2(Mathf.Clamp(spec.Width * 0.55f, 3f, 7f), 0.28f),
                new Color(0.13f, 0.22f, 0.31f, 0.8f),
                sprite,
                -2);
            CreateVisualStrip(
                secondary.transform,
                $"Skywalk_{i:00}_Edge",
                new Vector2(x, y + 0.18f),
                new Vector2(Mathf.Clamp(spec.Width * 0.52f, 2.8f, 6.8f), 0.06f),
                new Color(0.62f, 0.25f, 1f, 0.65f),
                sprite,
                -1);
        }
    }

    private static int CreateSpeedCollectibles(
        Transform parent,
        PlatformSpec[] specs,
        Sprite sprite,
        int levelIndex,
        FinalDemoController controller)
    {
        GameObject decorations = new GameObject("Speed_Collectible_PowerCells");
        decorations.transform.SetParent(parent, false);
        int count = 0;
        int stride = levelIndex == 0 ? 3 : 2;
        for (int i = 2; i < specs.Length - 1; i += stride)
        {
            PlatformSpec spec = specs[i];
            GameObject coin = new GameObject($"SpeedPowerCell_{i:00}");
            coin.transform.SetParent(decorations.transform, false);
            coin.layer = LayerMask.NameToLayer("Ignore Raycast");
            coin.transform.position = new Vector3(spec.X, spec.TopY + 1.25f, 0.8f);
            ConfigurePowerCellVisual(coin, sprite, 0.48f, 8);
            CircleCollider2D trigger = coin.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.05f;
            FinalDemoVisualCollectible collectible = coin.AddComponent<FinalDemoVisualCollectible>();
            collectible.Configure(controller);
            count++;
        }
        return count;
    }

    private static void ConfigurePowerCellVisual(
        GameObject powerCell,
        Sprite sprite,
        float scale,
        int sortingOrder)
    {
        powerCell.transform.localScale = new Vector3(scale, scale, 1f);
        powerCell.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        SpriteRenderer renderer = powerCell.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = powerCell.AddComponent<SpriteRenderer>();
        }
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.82f, 0.18f, 0.96f);
        renderer.sortingOrder = sortingOrder;

        Transform existingHalo = powerCell.transform.Find("FinalDemoPowerCellHalo");
        GameObject halo = existingHalo != null
            ? existingHalo.gameObject
            : new GameObject("FinalDemoPowerCellHalo");
        halo.transform.SetParent(powerCell.transform, false);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localRotation = Quaternion.identity;
        halo.transform.localScale = new Vector3(1.75f, 1.75f, 1f);
        SpriteRenderer haloRenderer = halo.GetComponent<SpriteRenderer>();
        if (haloRenderer == null)
        {
            haloRenderer = halo.AddComponent<SpriteRenderer>();
        }
        haloRenderer.sprite = sprite;
        haloRenderer.color = new Color(1f, 0.62f, 0.08f, 0.18f);
        haloRenderer.sortingOrder = sortingOrder - 1;
    }

    private static void CreateVisualPatrolAndroids(
        Transform parent,
        PlatformSpec[] specs,
        Sprite sprite,
        int seed,
        bool maxScore)
    {
        GameObject root = new GameObject("VisualOnly_Android_Patrols");
        root.transform.SetParent(parent, false);
        int count = maxScore || seed == 0 ? 1 : 2;
        for (int i = 0; i < count; i++)
        {
            int specIndex = Mathf.Clamp(2 + i * Mathf.Max(2, specs.Length / 3), 1, specs.Length - 2);
            PlatformSpec spec = specs[specIndex];
            float deckWidth = Mathf.Clamp(spec.Width * 0.55f, 4.8f, 7.5f);
            float deckY = spec.TopY + 4.25f + ((seed + i) % 2) * 0.45f;
            float deckX = spec.X + (((seed + i) % 2 == 0) ? -1.2f : 1.2f);
            CreateVisualStrip(
                root.transform,
                $"VisualPatrolDeck_{i + 1:00}",
                new Vector2(deckX, deckY),
                new Vector2(deckWidth, 0.24f),
                new Color(0.12f, 0.25f, 0.34f, 0.82f),
                sprite,
                -5);
            CreateVisualStrip(
                root.transform,
                $"VisualPatrolDeckEdge_{i + 1:00}",
                new Vector2(deckX, deckY + 0.16f),
                new Vector2(deckWidth * 0.94f, 0.05f),
                new Color(0.12f, 0.92f, 1f, 0.72f),
                sprite,
                -4);

            float minX = deckX - deckWidth * 0.5f + 0.65f;
            float maxX = deckX + deckWidth * 0.5f - 0.65f;
            GameObject android = CreateSimpleAndroid(
                $"VisualPatrolAndroid_{i + 1:00}",
                sprite,
                new Color(0.3f, 0.62f, 0.72f, 0.72f));
            android.transform.SetParent(root.transform, false);
            android.transform.position = new Vector3(minX, deckY + 0.46f, 1.2f);
            android.transform.localScale = new Vector3(0.5f, 0.68f, 1f);
            foreach (SpriteRenderer renderer in android.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder -= 8;
            }
            FinalDemoVisualPatrol patrol = android.AddComponent<FinalDemoVisualPatrol>();
            patrol.Configure(minX, maxX, 0.55f + i * 0.12f, true);
        }
    }

    private static void CreateNeonObstacles(Transform parent, PlatformSpec[] specs, Sprite sprite, int seed)
    {
        GameObject props = new GameObject("Neon_Props_And_Obstacles");
        props.transform.SetParent(parent, false);
        for (int i = 2; i < specs.Length; i += 3)
        {
            PlatformSpec spec = specs[i];
            float side = ((i + seed) % 2 == 0) ? -1f : 1f;
            float x = spec.X + side * Mathf.Max(1f, spec.Width * 0.33f);
            CreateVisualStrip(props.transform, $"Beacon_{i:00}", new Vector2(x, spec.TopY + 1.1f),
                new Vector2(0.16f, 2.1f), new Color(1f, 0.2f, 0.4f, 0.78f), sprite, 1);
        }
    }

    private static void CreateBackdrop(float centerX, float width, int palette)
    {
        Sprite sprite = GetSharedSprite();
        GameObject root = new GameObject("FinalDemo_Background");
        Color sky = palette >= 4
            ? new Color(0.07f, 0.055f, 0.13f, 1f)
            : new Color(0.045f, 0.085f, 0.14f, 1f);
        CreateVisualStrip(root.transform, "FarSky", new Vector2(centerX, 4f), new Vector2(width, 22f), sky, sprite, -30);
        CreateVisualStrip(root.transform, "NeonHorizon", new Vector2(centerX, -1.05f), new Vector2(width, 0.14f),
            palette >= 4 ? new Color(1f, 0.28f, 0.55f, 0.42f) : new Color(0.1f, 0.95f, 1f, 0.42f), sprite, -20);
        CreateVisualStrip(
            root.transform,
            "DistantAtmosphereGlow",
            new Vector2(centerX, 1.4f),
            new Vector2(width * 0.92f, 3.4f),
            palette >= 4
                ? new Color(0.22f, 0.08f, 0.28f, 0.18f)
                : new Color(0.04f, 0.28f, 0.38f, 0.16f),
            sprite,
            -26);
    }

    private static void CreateAmbientAndroids(
        Transform parent,
        Sprite sprite,
        float levelLength,
        int seed,
        bool maxScore = false)
    {
        GameObject root = new GameObject("Ambient_Androids_NonInteractive");
        root.transform.SetParent(parent, false);
        int count = maxScore ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            GameObject android = CreateSimpleAndroid(
                $"AmbientAndroid_{i + 1:00}",
                sprite,
                new Color(0.32f, 0.38f, 0.5f, 0.72f));
            android.transform.SetParent(root.transform, false);
            android.transform.position = new Vector3(
                levelLength * (0.22f + i * 0.27f),
                4.6f + ((i + seed) % 2) * 1.2f,
                1f);
            android.transform.localScale = new Vector3(0.55f, 0.7f, 1f);
            FinalDemoAmbientMover mover = android.AddComponent<FinalDemoAmbientMover>();
            mover.Configure(1.2f + 0.35f * i, 0.45f + 0.08f * i, seed + i * 1.7f);
        }
    }

    private static GameObject CreateSimpleAndroid(string name, Sprite sprite, Color color)
    {
        GameObject root = new GameObject(name);
        SpriteRenderer body = root.AddComponent<SpriteRenderer>();
        body.sprite = sprite;
        body.color = color;
        body.sortingOrder = 4;
        root.transform.localScale = new Vector3(0.9f, 1.1f, 1f);

        for (int i = 0; i < 2; i++)
        {
            GameObject eye = new GameObject(i == 0 ? "Eye_L" : "Eye_R");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(i == 0 ? -0.22f : 0.22f, 0.12f, -0.02f);
            eye.transform.localScale = new Vector3(0.14f, 0.12f, 1f);
            SpriteRenderer eyeRenderer = eye.AddComponent<SpriteRenderer>();
            eyeRenderer.sprite = sprite;
            eyeRenderer.color = new Color(1f, 0.18f, 0.12f, color.a);
            eyeRenderer.sortingOrder = 5;
        }
        return root;
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(4f, 3.2f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.8f;
        camera.backgroundColor = new Color(0.04f, 0.065f, 0.11f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        DemoCameraFollow2D follow = cameraObject.AddComponent<DemoCameraFollow2D>();
        follow.SetTarget(target);
        SetVector3(follow, "offset", new Vector3(4.8f, 2.8f, -10f));
        SetFloat(follow, "smoothTime", 0.14f);
    }

    private static void CreateMenuCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.035f, 0.06f, 0.11f, 1f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateDeathZone(float centerX, float width)
    {
        GameObject zone = InstantiatePrefab(DeathZonePrefabPath, "DeathZone_FinalDemo");
        zone.transform.position = new Vector3(centerX, -7f, 0f);
        zone.transform.localScale = new Vector3(width, 1f, 1f);
    }

    private static void CreateVisualStrip(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        Sprite sprite,
        int sortingOrder)
    {
        GameObject strip = new GameObject(name);
        strip.transform.SetParent(parent, false);
        strip.transform.position = new Vector3(position.x, position.y, 1.5f);
        strip.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static void ValidateSpeedScene(Scene scene, GameObject player, float goalX, PlatformSpec[] platforms)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        if (scene.GetRootGameObjects().Length == 0 ||
            player.GetComponents<EdgeRunnerAgentV5>().Length != 1 ||
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>() != null ||
            behavior == null || behavior.BehaviorName != "EdgeRunnerV5" ||
            behavior.BehaviorType != BehaviorType.InferenceOnly ||
            behavior.Model != RequireAsset<ModelAsset>(SpeedModelPath) ||
            player.GetComponent<DemoSprintVisual>() == null ||
            player.GetComponent<TrailRenderer>() == null ||
            goalX < 100f || platforms.Length < 9)
        {
            throw new System.InvalidOperationException("SpeedRun final demo contract validation failed.");
        }
        if (Object.FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include).Length != 0)
        {
            throw new System.InvalidOperationException("SpeedRun demo must not contain active Android hazards.");
        }
        if (Object.FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include).Length != 0)
        {
            throw new System.InvalidOperationException(
                "SpeedRun visual coins must not use ScoreAttackCoin or affect the objective.");
        }
        ValidateSpeedCollectibles();
        ValidateVisualPatrols(false);
        ValidatePlatformFlow(platforms, 5.0f);
        ValidateCleanVisualDecorations();
    }

    private static void ValidateSpeedCollectibles()
    {
        FinalDemoVisualCollectible[] collectibles =
            Object.FindObjectsByType<FinalDemoVisualCollectible>(FindObjectsInactive.Include);
        for (int i = 0; i < collectibles.Length; i++)
        {
            FinalDemoVisualCollectible collectible = collectibles[i];
            Collider2D trigger = collectible.GetComponent<Collider2D>();
            if (trigger == null || !trigger.enabled || !trigger.isTrigger ||
                collectible.gameObject.layer != LayerMask.NameToLayer("Ignore Raycast") ||
                collectible.GetComponent<Rigidbody2D>() != null ||
                collectible.GetComponent<ScoreAttackCoin>() != null)
            {
                throw new System.InvalidOperationException(
                    $"SpeedRun powercell '{collectible.name}' is not an isolated visual trigger.");
            }
        }
        if (collectibles.Length < 2)
        {
            throw new System.InvalidOperationException(
                "Each SpeedRun must contain collectible visual powercells.");
        }
    }

    private static void ValidateMaxScoreScene(
        Scene scene,
        GameObject player,
        ScoreAttackManager manager,
        MaxScoreObjectives objectives,
        PlatformSpec[] platforms)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();
        EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent =
            player.GetComponent<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        DecisionRequester requester = player.GetComponent<DecisionRequester>();
        FinalDemoMaxScoreStartupGate startupGate =
            player.GetComponent<FinalDemoMaxScoreStartupGate>();
        ScoreAttackCoin[] coins = Object.FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);
        ScoreAttackAndroid[] androids = Object.FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include);
        if (!scene.IsValid() || player.GetComponents<EdgeRunnerAgentV5>().Length != 1 ||
            objectAwareAgent == null ||
            behavior == null || behavior.BehaviorName != "EdgeRunnerV5ScoreMaxObjectAware" ||
            behavior.BehaviorType != BehaviorType.InferenceOnly ||
            behavior.Model != RequireAsset<ModelAsset>(MaxScoreModelPath) ||
            manager == null || coins.Length != 7 || androids.Length != 2 ||
            requester == null || startupGate == null ||
            startupGate.Agent != objectAwareAgent || startupGate.DecisionRequester != requester ||
            objectAwareAgent.enabled || requester.enabled ||
            Object.FindObjectsByType<ScoreAttackGoalLock>(FindObjectsInactive.Include).Length != 1 ||
            objectives.GoalX < 105f)
        {
            throw new System.InvalidOperationException("MaxScore final demo contract validation failed.");
        }
        ValidatePlatformFlow(platforms, 4.5f);
        ValidateMaxScoreObjectPlacement(coins, androids);
        ValidateVisualPatrols(true);
        ValidateCleanVisualDecorations();
    }

    private static void ValidateMaxScoreObjectPlacement(
        ScoreAttackCoin[] coins,
        ScoreAttackAndroid[] androids)
    {
        for (int i = 0; i < coins.Length; i++)
        {
            ScoreAttackCoin coin = coins[i];
            Collider2D coinCollider = coin.GetComponent<Collider2D>();
            SpriteRenderer coinRenderer = coin.GetComponent<SpriteRenderer>();
            if (coinCollider == null || coinRenderer == null ||
                coin.transform.Find("FinalDemoPowerCellHalo") == null ||
                !TryGetSupportingPlatformTop(coin.transform.position.x, coin.transform.position.y, out float topY))
            {
                throw new System.InvalidOperationException(
                    $"MaxScore coin '{coin.name}' has no collider, renderer, or supporting platform.");
            }

            float colliderClearance = coinCollider.bounds.min.y - topY;
            float visualClearance = coinRenderer.bounds.min.y - topY;
            float centerHeight = coin.transform.position.y - topY;
            bool lowCoin = coin.name.StartsWith("FinalLongChallenge_LowCoin_", System.StringComparison.Ordinal);
            bool highCoin = coin.name.StartsWith("FinalLongChallenge_HighCoin_", System.StringComparison.Ordinal);
            bool validHeight = lowCoin
                ? centerHeight >= 0.80f && centerHeight <= 1.20f
                : highCoin && centerHeight >= 2.35f && centerHeight <= 2.90f;
            if (!validHeight || colliderClearance < 0.08f || visualClearance < 0.02f)
            {
                throw new System.InvalidOperationException(
                    $"MaxScore coin '{coin.name}' intersects or sits ambiguously above its platform: " +
                    $"centerHeight={centerHeight:F2}, colliderClearance={colliderClearance:F2}, " +
                    $"visualClearance={visualClearance:F2}.");
            }
        }

        for (int i = 0; i < androids.Length; i++)
        {
            ScoreAttackAndroid android = androids[i];
            Collider2D collider = android.GetComponent<Collider2D>();
            if (collider == null || !collider.enabled || !collider.isTrigger ||
                android.GetComponent<FinalDemoVisualPatrol>() != null ||
                android.GetComponent<DemoAndroidPatrol>() != null ||
                !TryGetSupportingPlatformTop(android.transform.position.x, android.transform.position.y, out float topY))
            {
                throw new System.InvalidOperationException(
                    $"Functional Android '{android.name}' is not stomp-ready on a platform.");
            }
            float anchorHeight = android.transform.position.y - topY;
            if (anchorHeight < 0.85f || anchorHeight > 1.20f)
            {
                throw new System.InvalidOperationException(
                    $"Functional Android '{android.name}' appears to float: anchorHeight={anchorHeight:F2}.");
            }
        }
    }

    private static void ValidateVisualPatrols(bool maxScore)
    {
        FinalDemoVisualPatrol[] patrols =
            Object.FindObjectsByType<FinalDemoVisualPatrol>(FindObjectsInactive.Include);
        if (patrols.Length < 1)
        {
            throw new System.InvalidOperationException(
                "Final demo requires at least one presentation-only Android patrol.");
        }
        for (int i = 0; i < patrols.Length; i++)
        {
            FinalDemoVisualPatrol patrol = patrols[i];
            if (!patrol.PatrolEnabled || patrol.PatrolMaxX <= patrol.PatrolMinX ||
                patrol.transform.position.y < 3.8f ||
                patrol.GetComponentInChildren<Collider2D>(true) != null ||
                patrol.GetComponentInChildren<Rigidbody2D>(true) != null ||
                patrol.GetComponentInChildren<ScoreAttackAndroid>(true) != null ||
                patrol.GetComponentInChildren<DemoEnemyHazard>(true) != null)
            {
                throw new System.InvalidOperationException(
                    $"Visual Android patrol '{patrol.name}' could interfere with gameplay or float.");
            }
        }
        if (!maxScore && Object.FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include).Length != 0)
        {
            throw new System.InvalidOperationException(
                "SpeedRun must contain presentation-only Androids, never functional Androids.");
        }
    }

    private static bool TryGetSupportingPlatformTop(float x, float objectY, out float topY)
    {
        topY = float.NegativeInfinity;
        int groundLayer = LayerMask.NameToLayer("Ground");
        Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger ||
                collider.gameObject.layer != groundLayer)
            {
                continue;
            }
            Bounds bounds = collider.bounds;
            if (x < bounds.min.x - 0.01f || x > bounds.max.x + 0.01f ||
                bounds.max.y > objectY + 0.05f)
            {
                continue;
            }
            topY = Mathf.Max(topY, bounds.max.y);
        }
        return !float.IsNegativeInfinity(topY);
    }

    private static void ValidateCleanVisualDecorations()
    {
        if (Object.FindObjectsByType<FinalDemoAmbientMover>(FindObjectsInactive.Include).Length != 0)
        {
            throw new System.InvalidOperationException(
                "Final demo contains floating ambient Android decorations.");
        }
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            string objectName = transforms[i].name;
            if (objectName.StartsWith("Beacon_", System.StringComparison.Ordinal) ||
                objectName.StartsWith("CityBlock_", System.StringComparison.Ordinal))
            {
                throw new System.InvalidOperationException(
                    $"Final demo contains obsolete placeholder decoration '{objectName}'.");
            }
        }
    }

    private static void ValidateSpeedLayoutSet()
    {
        PlatformSpec[] easy = CreateSpeedEasy();
        PlatformSpec[] normal = CreateSpeedNormal();
        PlatformSpec[] hard = CreateSpeedHard();
        if (easy.Length != 9 || normal.Length != 13 || hard.Length != 16 ||
            easy.Length >= normal.Length || normal.Length >= hard.Length ||
            easy[easy.Length - 1].X >= normal[normal.Length - 1].X ||
            normal[normal.Length - 1].X >= hard[hard.Length - 1].X ||
            SpeedGoalX[0] < 100f || SpeedGoalX[0] > 120f ||
            SpeedGoalX[1] < 140f || SpeedGoalX[1] > 170f ||
            SpeedGoalX[2] < 180f || SpeedGoalX[2] > 220f)
        {
            throw new System.InvalidOperationException(
                "SpeedRun Easy, Normal, and Hard must have clearly distinct lengths and platform counts.");
        }
    }

    private static void ValidatePlatformFlow(PlatformSpec[] platforms, float maxGap)
    {
        float previousRight = platforms[0].X + platforms[0].Width * 0.5f;
        for (int i = 1; i < platforms.Length; i++)
        {
            float left = platforms[i].X - platforms[i].Width * 0.5f;
            float gap = left - previousRight;
            if (gap < 0.45f || gap > maxGap || Mathf.Abs(platforms[i].TopY - platforms[i - 1].TopY) > 2.4f)
            {
                throw new System.InvalidOperationException(
                    $"Unsafe handcrafted transition before {platforms[i].Name}: gap={gap:F2}.");
            }
            previousRight = platforms[i].X + platforms[i].Width * 0.5f;
        }
    }

    private static void SaveScene(Scene scene, int sceneIndex)
    {
        string path = $"{SceneFolder}/{SceneNames[sceneIndex]}.unity";
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path))
        {
            throw new System.InvalidOperationException($"Could not save final demo scene at {path}.");
        }
        Debug.Log($"[FINAL DEMO] Validated and saved {path}");
    }

    private static GameObject InstantiatePrefab(string path, string name)
    {
        GameObject prefab = RequireAsset<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException($"Could not instantiate {path}.");
        }
        instance.name = name;
        return instance;
    }

    private static T RequireAsset<T>(string path) where T : Object
    {
        AssetDatabase.ImportAsset(path);
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new FileNotFoundException($"Required asset was not found: {path}", path);
        }
        return asset;
    }

    private static Sprite GetSharedSprite()
    {
        GameObject ground = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);
        SpriteRenderer renderer = ground != null ? ground.GetComponent<SpriteRenderer>() : null;
        return renderer != null && renderer.sprite != null
            ? renderer.sprite
            : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SetSerializedValue(target, propertyName, property => property.boolValue = value);
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SetSerializedValue(target, propertyName, property => property.intValue = value);
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SetSerializedValue(target, propertyName, property => property.floatValue = value);
    }

    private static void SetVector3(Object target, string propertyName, Vector3 value)
    {
        SetSerializedValue(target, propertyName, property => property.vector3Value = value);
    }

    private static void SetSerializedValue(Object target, string propertyName, System.Action<SerializedProperty> setter)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            setter(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static PlatformSpec[] CreateSpeedEasy() => new[]
    {
        new PlatformSpec("E01_StartDeck", 6f, 0f, 16f),
        new PlatformSpec("E02_FirstLanding", 19f, 0.2f, 7f),
        new PlatformSpec("E03_SafeRise", 29.5f, 0.6f, 10f),
        new PlatformSpec("E04_GentleDrop", 41f, 0.15f, 9f),
        new PlatformSpec("E05_WideRecovery", 52.5f, 0.75f, 10f),
        new PlatformSpec("E06_MidLanding", 65f, 0.3f, 9f),
        new PlatformSpec("E07_UpperRun", 77f, 0.9f, 10f),
        new PlatformSpec("E08_FinalRecovery", 90f, 0.4f, 10f),
        new PlatformSpec("E09_GoalDeck", 104.5f, 0.55f, 15f)
    };

    private static PlatformSpec[] CreateSpeedNormal() => new[]
    {
        new PlatformSpec("N01_StartDeck", 6f, 0f, 16f),
        new PlatformSpec("N02_FirstStep", 20f, 0.4f, 8f),
        new PlatformSpec("N03_UpperRoof", 31.5f, 0.9f, 11f),
        new PlatformSpec("N04_LowLanding", 44f, 0.25f, 8f),
        new PlatformSpec("N05_LongClimb", 56.5f, 1.1f, 11f),
        new PlatformSpec("N06_MidDrop", 69.5f, 0.55f, 9f),
        new PlatformSpec("N07_WideRecovery", 82.5f, 0.1f, 11f),
        new PlatformSpec("N08_SecondRise", 95.5f, 0.75f, 9f),
        new PlatformSpec("N09_HighRun", 108.5f, 1.25f, 11f),
        new PlatformSpec("N10_DeepRecovery", 121.5f, 0.45f, 9f),
        new PlatformSpec("N11_FinalRise", 134.5f, 0.9f, 11f),
        new PlatformSpec("N12_LastDrop", 147.5f, 0.2f, 9f),
        new PlatformSpec("N13_GoalDeck", 160f, 0.55f, 14f)
    };

    private static PlatformSpec[] CreateSpeedHard() => new[]
    {
        new PlatformSpec("H01_StartDeck", 6f, 0f, 16f),
        new PlatformSpec("H02_NarrowRise", 21f, 0.6f, 8f),
        new PlatformSpec("H03_HighRun", 33f, 1.2f, 10f),
        new PlatformSpec("H04_DeepDrop", 46f, 0.3f, 10f),
        new PlatformSpec("H05_ClimbA", 59.5f, 1f, 11f),
        new PlatformSpec("H06_ClimbB", 73f, 1.6f, 10f),
        new PlatformSpec("H07_LongDrop", 86.5f, 0.7f, 11f),
        new PlatformSpec("H08_LowBridge", 100f, 0f, 10f),
        new PlatformSpec("H09_RecoveryRise", 113.5f, 0.9f, 11f),
        new PlatformSpec("H10_HighSequenceA", 127f, 1.5f, 10f),
        new PlatformSpec("H11_HighSequenceB", 140.5f, 0.5f, 11f),
        new PlatformSpec("H12_UpperRecovery", 154f, 1.2f, 10f),
        new PlatformSpec("H13_LowSequence", 167.5f, 0.2f, 11f),
        new PlatformSpec("H14_FinalRise", 181f, 1f, 10f),
        new PlatformSpec("H15_LastLanding", 194.5f, 0.45f, 11f),
        new PlatformSpec("H16_GoalDeck", 208f, 0.75f, 14f)
    };

    private static PlatformSpec[] CreateMaxEasy() => new[]
    {
        new PlatformSpec("ME01_Start", 6f, 0f, 17f),
        new PlatformSpec("ME02_HighCoin01", 25.5f, 0.35f, 20f),
        new PlatformSpec("ME03_Android01", 58f, -0.15f, 43f),
        new PlatformSpec("ME04_HighCoin03", 97f, 0.55f, 33f),
        new PlatformSpec("ME05_Recovery", 118.5f, 0.1f, 9f),
        new PlatformSpec("ME06_Goal", 129.5f, 0.4f, 11f)
    };

    private static MaxScoreObjectives CreateMaxEasyObjectives() => new MaxScoreObjectives
    {
        LowCoins = { [0] = new Vector2(4f, 0.85f), [1] = new Vector2(8f, 0.85f), [2] = new Vector2(31f, 0.85f), [3] = new Vector2(75f, 0.85f) },
        HighCoins = { [0] = new Vector2(21.5f, 2.55f), [1] = new Vector2(64.1f, 2.55f), [2] = new Vector2(108.5f, 2.55f) },
        Androids = { [0] = new Vector2(42.4f, 1.02f), [1] = new Vector2(86.8f, 1.02f) },
        GoalX = 131.5f, GoalY = 0f,
        HighCoin01LandingGateX = 16.5f, Android01LandingGateX = 37.9f, Android02LandingGateX = 82.3f
    };

    private static PlatformSpec[] CreateMaxNormal() => new[]
    {
        new PlatformSpec("MN01_Start", 6f, 0f, 16f),
        new PlatformSpec("MN02_HighCoin01", 25.5f, 0.55f, 19f),
        new PlatformSpec("MN03_LongMiddle", 58.2f, -0.25f, 41.6f),
        new PlatformSpec("MN04_Android02HighCoin03", 97.05f, 0.8f, 30.5f),
        new PlatformSpec("MN05_Recovery", 118.5f, 0.15f, 8f),
        new PlatformSpec("MN06_Goal", 129.65f, 0.5f, 9.7f)
    };

    private static MaxScoreObjectives CreateMaxNormalObjectives() => new MaxScoreObjectives
    {
        LowCoins = { [0] = new Vector2(4f, 0.85f), [1] = new Vector2(8f, 0.85f), [2] = new Vector2(31f, 0.85f), [3] = new Vector2(75f, 0.85f) },
        HighCoins = { [0] = new Vector2(21.5f, 2.55f), [1] = new Vector2(64.1f, 2.55f), [2] = new Vector2(108.5f, 2.55f) },
        Androids = { [0] = new Vector2(42.4f, 1.02f), [1] = new Vector2(86.8f, 1.02f) },
        GoalX = 131.5f, GoalY = 0f,
        HighCoin01LandingGateX = 16.5f, Android01LandingGateX = 37.9f, Android02LandingGateX = 82.3f
    };

    private static PlatformSpec[] CreateMaxHard() => new[]
    {
        new PlatformSpec("MH01_Start", 6f, 0f, 16f),
        new PlatformSpec("MH02_HighCoin01", 25.5f, 0.7f, 18f),
        new PlatformSpec("MH03_LongMiddle", 58.2f, -0.35f, 42f),
        new PlatformSpec("MH04_Android02HighCoin03", 97.05f, 1f, 30f),
        new PlatformSpec("MH05_Recovery", 118.5f, 0.1f, 7.5f),
        new PlatformSpec("MH06_Goal", 129.65f, 0.65f, 9f)
    };

    private static MaxScoreObjectives CreateMaxHardObjectives() => new MaxScoreObjectives
    {
        LowCoins = { [0] = new Vector2(4f, 0.85f), [1] = new Vector2(8f, 0.85f), [2] = new Vector2(31f, 0.85f), [3] = new Vector2(75f, 0.85f) },
        HighCoins = { [0] = new Vector2(21.5f, 2.55f), [1] = new Vector2(64.1f, 2.55f), [2] = new Vector2(108.5f, 2.55f) },
        Androids = { [0] = new Vector2(42.4f, 1.02f), [1] = new Vector2(86.8f, 1.02f) },
        GoalX = 131.5f, GoalY = 0f,
        HighCoin01LandingGateX = 16.5f, Android01LandingGateX = 37.9f, Android02LandingGateX = 82.3f
    };
}
