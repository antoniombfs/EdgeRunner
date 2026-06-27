using System.Collections.Generic;
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
    private const string SpeedRunnerFinalRandomScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_SpeedRunnerFinalRandom.unity";
    private const string ScoreMaxCoinIntroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxCoinIntro.unity";
    private const string ScoreMaxStompIntroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxStompIntro.unity";
    private const string ScoreMaxIntroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxIntro.unity";
    private const string ScoreMaxEasyScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxEasy.unity";
    private const string ScoreMaxRandomWarmupScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxRandomWarmup.unity";
    private const string ScoreMaxFinalRandomScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxFinalRandom.unity";

    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidEnemyPrefabPath = "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";
    private const string NoFrictionMaterialPath = "Assets/EdgeRunner/Physics/NoFriction2D.physicsMaterial2D";

    private const float FinalMinPlatformWidth = 4.8f;
    private const float FinalMinLandingPlatformWidth = 5.0f;
    private const float FinalMinRecoveryPlatformWidth = 5.0f;
    private const float FinalMinGapWidth = 2.8f;
    private const float FinalMaxGapWidthSpeedRunner = 3.2f;
    private const float FinalMaxGapWidthScoreMax = 3.0f;
    private const float FinalMaxVerticalStep = 1.2f;
    private const float FinalGoalPlatformWidth = 10.0f;
    private const float FinalSafeEdgeMargin = 1.0f;
    private const float ScoreMaxFinalCoinCollectionRadius = 0.52f;
    private const bool ScoreMaxMergeSameHeightPlatforms = true;
    private const float ScoreMaxPlatformMergeYTolerance = 0.05f;
    private const float ScoreMaxPlatformMergeGapTolerance = 0.25f;
    private static readonly Vector2 FinalGoalTriggerSize = new Vector2(2.5f, 7.0f);
    private static readonly bool ScoreMaxDebugPlatformMerge = false;

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

    [MenuItem("EdgeRunner/Training/V5/Build SpeedRunnerFinalRandom")]
    public static void BuildSpeedRunnerFinalRandomFromMenu()
    {
        BuildSpeedRunnerFinalRandomScene();
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

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxCoinIntro")]
    public static void BuildScoreMaxCoinIntroFromMenu()
    {
        BuildScoreMaxCoinIntroScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxStompIntro")]
    public static void BuildScoreMaxStompIntroFromMenu()
    {
        BuildScoreMaxStompIntroScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxIntro")]
    public static void BuildScoreMaxIntroFromMenu()
    {
        BuildScoreMaxIntroScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxEasy")]
    public static void BuildScoreMaxEasyFromMenu()
    {
        BuildScoreMaxEasyScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxRandomWarmup")]
    public static void BuildScoreMaxRandomWarmupFromMenu()
    {
        BuildScoreMaxRandomWarmupScene();
    }

    [MenuItem("EdgeRunner/Training/V5/Build ScoreMaxFinalRandom")]
    public static void BuildScoreMaxFinalRandomFromMenu()
    {
        BuildScoreMaxFinalRandomScene();
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

    public static void BuildSpeedRunnerFinalRandomScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PhysicsMaterial2D noFrictionMaterial = CreateOrUpdateNoFrictionMaterial();
        GameObject root = new GameObject("ER_V5_SpeedRunnerFinalRandom");
        List<FinalPlatformBlock> platforms = BuildSpeedRunnerFinalLayout(root.transform, sprite, noFrictionMaterial);
        FinalPlatformBlock finalPlatform = platforms[platforms.Count - 1];
        GameObject goal = CreateFinalGoal(
            "SpeedRunnerFinalRandom_Goal",
            new Vector3(finalPlatform.CenterX, finalPlatform.TopY + 1.1f, 0f),
            FinalGoalTriggerSize);
        GameObject player = CreatePlayer(new Vector3(1f, 1.5f, 0f));

        ConfigureDirectSpeedRunnerPlayer(player, goal.transform, noFrictionMaterial);
        ConfigureSpeedRunnerSprintVisual(player);
        CreateCamera(player.transform);
        CreateDeathZone("DeathZone_SpeedRunnerFinalRandom", GetLayoutCenterX(platforms), GetLayoutWidth(platforms) + 20f);
        CreateEvaluationManager("SpeedRunnerFinalRandom_Evaluation", player, "SpeedRunnerFinalRandom", "Eval100");
        StripScoreAttackAndEnemyObjectsFromSpeedRunnerFinalScene();

        Selection.activeObject = player;
        SaveScene(scene, SpeedRunnerFinalRandomScenePath, "SpeedRunnerFinalRandom");
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

    public static void BuildScoreMaxCoinIntroScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxCoinIntro");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, false, 1, 0);
        GameObject goal = CreateScoreAttackGoal(new Vector3(12f, 1.1f, 0f), manager);
        GameObject player = CreateScoreMaxPlayer(new Vector3(0f, 1.15f, 0f));

        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        ConfigureScoreMaxCoinIntroDiagnostics(player);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreMaxCoinIntro_Platform", 7f, 0f, new Vector2(30f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreMaxCoinIntro_Coin_01", new Vector3(3.5f, 1.35f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreMaxCoinIntro", 7f, 40f);
        CreateEvaluationManager("ScoreMaxCoinIntro_Evaluation", player, "ScoreMaxCoinIntro", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxCoinIntroScenePath, "ScoreMaxCoinIntro");
    }

    public static void BuildScoreMaxStompIntroScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxStompIntro");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, false, 0, 1);
        GameObject goal = CreateScoreAttackGoal(new Vector3(10f, 1.1f, 0f), manager);
        GameObject player = CreateScoreMaxPlayer(new Vector3(0f, 1.15f, 0f));

        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreMaxStompIntro_Platform", 5f, 0f, new Vector2(24f, 0.4f), sprite);
        CreateScoreAttackAndroid(root.transform, "ScoreMaxStompIntro_Android_01", new Vector3(5.5f, 1.02f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreMaxStompIntro", 5f, 34f);
        CreateEvaluationManager("ScoreMaxStompIntro_Evaluation", player, "ScoreMaxStompIntro", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxStompIntroScenePath, "ScoreMaxStompIntro");
    }

    public static void BuildScoreMaxIntroScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxIntro");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, false, 1, 1);
        GameObject goal = CreateScoreAttackGoal(new Vector3(11.5f, 1.1f, 0f), manager);
        GameObject player = CreateScoreMaxPlayer(new Vector3(0f, 1.15f, 0f));

        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreMaxIntro_Platform", 5.75f, 0f, new Vector2(27f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreMaxIntro_Coin_01", new Vector3(3f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackAndroid(root.transform, "ScoreMaxIntro_Android_01", new Vector3(6f, 1.02f, 0f), sprite, manager);
        CreateDeathZone("DeathZone_ScoreMaxIntro", 5.75f, 38f);
        CreateEvaluationManager("ScoreMaxIntro_Evaluation", player, "ScoreMaxIntro", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxIntroScenePath, "ScoreMaxIntro");
    }

    public static void BuildScoreMaxEasyScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxEasy");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, false, 2, 1);
        GameObject goal = CreateScoreAttackGoal(new Vector3(15f, 1.1f, 0f), manager);
        GameObject player = CreateScoreMaxPlayer(new Vector3(0f, 1.15f, 0f));

        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        ConfigureScoreMaxEasyTutorial(player);
        CreateCamera(player.transform);
        CreatePlatformWithTop(root.transform, "ScoreMaxEasy_Platform", 7.5f, 0f, new Vector2(34f, 0.4f), sprite);
        CreateScoreAttackCoin(root.transform, "ScoreMaxEasy_Coin_01", new Vector3(3.5f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackCoin(root.transform, "ScoreMaxEasy_Coin_02", new Vector3(6f, 1.55f, 0f), sprite, manager);
        CreateScoreAttackAndroid(
            root.transform,
            "ScoreMaxEasy_Android_01",
            new Vector3(10f, 1.02f, 0f),
            sprite,
            manager,
            enablePatrol: false);
        CreateDeathZone("DeathZone_ScoreMaxEasy", 7.5f, 44f);
        CreateEvaluationManager("ScoreMaxEasy_Evaluation", player, "ScoreMaxEasy", "Eval50");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxEasyScenePath, "ScoreMaxEasy");
    }

    public static void BuildScoreMaxRandomWarmupScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ER_V5_ScoreMaxRandomWarmup");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, true, 2, 1);
        GameObject goal = CreateScoreAttackGoal(new Vector3(15f, 1.1f, 0f), manager);
        ConfigureGoalTrigger(goal, new Vector2(2.5f, 7.0f));
        ConfigureScoreMaxRandomWarmupManager(manager, goal.transform);

        GameObject player = CreateScoreMaxPlayer(new Vector3(0f, 1.15f, 0f));
        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        ConfigureScoreMaxRandomWarmupTutorial(player);
        CreateCamera(player.transform);

        CreatePlatformWithTop(
            root.transform,
            "ScoreMaxRandomWarmup_Platform",
            8f,
            0f,
            new Vector2(38f, 0.4f),
            sprite);
        CreateScoreAttackCoin(
            root.transform,
            "ScoreMaxRandomWarmup_Coin_01",
            new Vector3(3.5f, 1.5f, 0f),
            sprite,
            manager);
        CreateScoreAttackCoin(
            root.transform,
            "ScoreMaxRandomWarmup_Coin_02",
            new Vector3(6.5f, 1.5f, 0f),
            sprite,
            manager);
        CreateScoreAttackAndroid(
            root.transform,
            "ScoreMaxRandomWarmup_Android_01",
            new Vector3(10.75f, 1.02f, 0f),
            sprite,
            manager,
            enablePatrol: false);
        CreateDeathZone("DeathZone_ScoreMaxRandomWarmup", 8f, 48f);
        CreateEvaluationManager(
            "ScoreMaxRandomWarmup_Evaluation",
            player,
            "ScoreMaxRandomWarmup",
            "Eval100");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxRandomWarmupScenePath, "ScoreMaxRandomWarmup");
    }

    public static void BuildScoreMaxFinalRandomScene()
    {
        EnsureFolders();

        Sprite sprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PhysicsMaterial2D noFrictionMaterial = CreateOrUpdateNoFrictionMaterial();
        GameObject root = new GameObject("ER_V5_ScoreMaxFinalRandom");
        ScoreAttackManager manager = CreateScoreMaxManager(root.transform, false, 4, 2);
        List<FinalPlatformBlock> platforms = BuildScoreMaxFinalLayout(root.transform, sprite, noFrictionMaterial);
        FinalPlatformBlock finalPlatform = platforms[platforms.Count - 1];
        GameObject goal = CreateScoreAttackGoal(
            new Vector3(finalPlatform.CenterX, finalPlatform.TopY + 1.1f, 0f),
            manager);
        ConfigureGoalTrigger(goal, FinalGoalTriggerSize);
        SetObjectReference(manager, "goal", goal.transform);
        ConfigureScoreMaxFinalRandomManager(manager);

        GameObject player = CreateScoreMaxPlayer(new Vector3(1f, 1.5f, 0f));
        ConfigureScoreMaxPlayer(player, goal.transform, manager);
        ConfigureScoreMaxFinalRandomEpisodeLimits(player);
        PlaceScoreMaxObjectives(root.transform, platforms, sprite, manager, goal.transform);
        CreateCamera(player.transform);
        CreateDeathZone("DeathZone_ScoreMaxFinalRandom", GetLayoutCenterX(platforms), GetLayoutWidth(platforms) + 20f);
        CreateEvaluationManager("ScoreMaxFinalRandom_Evaluation", player, "ScoreMaxFinalRandom", "Eval100");

        Selection.activeObject = player;
        SaveScene(scene, ScoreMaxFinalRandomScenePath, "ScoreMaxFinalRandom");
    }

    private static List<FinalPlatformBlock> BuildSpeedRunnerFinalLayout(
        Transform root,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        List<FinalPlatformBlock> platforms = new List<FinalPlatformBlock>();
        float cursorX = -1f;
        float topY = 0f;

        FinalPlatformBlock start = AddFinalPlatform(
            platforms,
            root,
            "StartFlat_FlatRun",
            cursorX,
            topY,
            14.5f,
            sprite,
            noFrictionMaterial);
        cursorX = start.MaxX;

        AppendGapPlatform(
            platforms,
            root,
            "MediumGap_LandingRecovery_A",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthSpeedRunner,
            11.0f,
            sprite,
            noFrictionMaterial);

        AppendGapPlatform(
            platforms,
            root,
            "PlatformChain_Start",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthSpeedRunner,
            5.2f,
            sprite,
            noFrictionMaterial);
        AppendPlatformChain(platforms, root, "PlatformChain", ref cursorX, ref topY, FinalMaxGapWidthSpeedRunner, sprite, noFrictionMaterial);

        AppendSafeDrop(
            platforms,
            root,
            "SafeDrop_FlatRun_B",
            ref cursorX,
            ref topY,
            FinalMaxGapWidthSpeedRunner,
            13.3f,
            sprite,
            noFrictionMaterial);
        AppendGapPlatform(
            platforms,
            root,
            "MediumGap_LandingRecovery_B",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthSpeedRunner,
            11.4f,
            sprite,
            noFrictionMaterial);
        AppendGapPlatform(
            platforms,
            root,
            "FinalGoalPlatform",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthSpeedRunner,
            FinalGoalPlatformWidth,
            sprite,
            noFrictionMaterial);

        return platforms;
    }

    private static List<FinalPlatformBlock> BuildScoreMaxFinalLayout(
        Transform root,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        List<FinalPlatformBlock> platforms = new List<FinalPlatformBlock>();
        float cursorX = -1f;
        float topY = 0f;

        FinalPlatformBlock start = AddFinalPlatform(
            platforms,
            root,
            "StartFlat",
            cursorX,
            topY,
            10.0f,
            sprite,
            noFrictionMaterial);
        cursorX = start.MaxX;

        AppendContinuousPlatform(platforms, root, "FlatRun_CoinLine", ref cursorX, topY, 8.0f, sprite, noFrictionMaterial);
        AppendGapPlatform(
            platforms,
            root,
            "MediumGap_AndroidLanding_A",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthScoreMax,
            8.0f,
            sprite,
            noFrictionMaterial);
        AppendRecoveryPlatform(platforms, root, "RecoveryPlatform_A", ref cursorX, topY, sprite, noFrictionMaterial);

        AppendSafeDrop(platforms, root, "SafeDrop_CoinPocket", ref cursorX, ref topY, FinalMaxGapWidthScoreMax, 8.0f, sprite, noFrictionMaterial);
        AppendPlatformChain(platforms, root, "PlatformChain_CoinArc", ref cursorX, ref topY, FinalMaxGapWidthScoreMax, sprite, noFrictionMaterial);
        AppendRecoveryPlatform(platforms, root, "RecoveryPlatform_B", ref cursorX, topY, sprite, noFrictionMaterial);
        AppendGapPlatform(
            platforms,
            root,
            "AndroidPlatform_FinalApproach",
            ref cursorX,
            topY,
            FinalMinGapWidth,
            FinalMaxGapWidthScoreMax,
            8.0f,
            sprite,
            noFrictionMaterial);
        AppendRecoveryPlatform(platforms, root, "RecoveryPlatform_C", ref cursorX, topY, sprite, noFrictionMaterial);
        AppendFinalGoalPlatform(platforms, root, "FinalLockedGoalPlatform", ref cursorX, topY, sprite, noFrictionMaterial);

        ApplyScoreMaxPlatformColliderMerging(root, platforms, noFrictionMaterial);
        return platforms;
    }

    private static void AppendContinuousPlatform(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        float topY,
        float width,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        FinalPlatformBlock platform = AddFinalPlatform(
            platforms,
            root,
            name,
            cursorX,
            topY,
            Mathf.Max(FinalMinPlatformWidth, width),
            sprite,
            noFrictionMaterial);
        cursorX = platform.MaxX;
    }

    private static void AppendGapPlatform(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        float topY,
        float minGap,
        float maxGap,
        float width,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        cursorX += Random.Range(Mathf.Max(FinalMinGapWidth, minGap), Mathf.Max(FinalMinGapWidth, maxGap));
        FinalPlatformBlock platform = AddFinalPlatform(
            platforms,
            root,
            name,
            cursorX,
            topY,
            Mathf.Max(FinalMinLandingPlatformWidth, width),
            sprite,
            noFrictionMaterial);
        cursorX = platform.MaxX;
    }

    private static void AppendStaircase(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string baseName,
        ref float cursorX,
        ref float topY,
        bool up,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        for (int i = 0; i < 3; i++)
        {
            topY += up ? 0.35f : -0.35f;
            topY = Mathf.Clamp(topY, -1.0f, 2.2f);
            AppendContinuousPlatform(
                platforms,
                root,
                $"{baseName}_{i + 1}",
                ref cursorX,
                topY,
                FinalMinPlatformWidth,
                sprite,
                noFrictionMaterial);
        }
    }

    private static void AppendSafeDrop(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        ref float topY,
        float maxGap,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        AppendSafeDrop(
            platforms,
            root,
            name,
            ref cursorX,
            ref topY,
            maxGap,
            6.8f,
            sprite,
            noFrictionMaterial);
    }

    private static void AppendSafeDrop(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        ref float topY,
        float maxGap,
        float width,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        topY = Mathf.Max(-1.0f, topY - Random.Range(0.7f, FinalMaxVerticalStep));
        AppendGapPlatform(
            platforms,
            root,
            name,
            ref cursorX,
            topY,
            FinalMinGapWidth,
            maxGap,
            width,
            sprite,
            noFrictionMaterial);
    }

    private static void AppendPlatformChain(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string baseName,
        ref float cursorX,
        ref float topY,
        float maxGap,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        for (int i = 0; i < 2; i++)
        {
            topY += Random.Range(-0.35f, 0.35f);
            topY = Mathf.Clamp(topY, -1.0f, 2.2f);
            AppendGapPlatform(
                platforms,
                root,
                $"{baseName}_{i + 1}",
                ref cursorX,
                topY,
                FinalMinGapWidth,
                maxGap,
                FinalMinLandingPlatformWidth,
                sprite,
                noFrictionMaterial);
        }
    }

    private static void AppendRecoveryPlatform(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        float topY,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        AppendContinuousPlatform(
            platforms,
            root,
            name,
            ref cursorX,
            topY,
            FinalMinRecoveryPlatformWidth,
            sprite,
            noFrictionMaterial);
    }

    private static void AppendFinalGoalPlatform(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        ref float cursorX,
        float topY,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        AppendContinuousPlatform(
            platforms,
            root,
            name,
            ref cursorX,
            topY,
            FinalGoalPlatformWidth,
            sprite,
            noFrictionMaterial);
    }

    private static FinalPlatformBlock AddFinalPlatform(
        List<FinalPlatformBlock> platforms,
        Transform root,
        string name,
        float startX,
        float topY,
        float width,
        Sprite sprite,
        PhysicsMaterial2D noFrictionMaterial)
    {
        float clampedWidth = Mathf.Max(FinalMinPlatformWidth, width);
        float centerX = startX + clampedWidth * 0.5f;
        GameObject platform = CreatePlatformWithTop(
            root,
            name,
            centerX,
            topY,
            new Vector2(clampedWidth, 0.4f),
            sprite);
        ApplyPhysicsMaterialToColliders(platform, noFrictionMaterial);

        FinalPlatformBlock block = new FinalPlatformBlock(name, centerX, topY, clampedWidth);
        platforms.Add(block);
        return block;
    }

    private static void ApplyScoreMaxPlatformColliderMerging(
        Transform root,
        List<FinalPlatformBlock> platforms,
        PhysicsMaterial2D noFrictionMaterial)
    {
        if (!ScoreMaxMergeSameHeightPlatforms || root == null || platforms == null || platforms.Count < 2)
        {
            return;
        }

        int beforeColliderCount = platforms.Count;
        int disabledPlatformColliderCount = 0;
        int mergedColliderCount = 0;
        bool[] coveredByMergedCollider = new bool[platforms.Count];

        for (int i = 0; i < platforms.Count; i++)
        {
            int groupStart = i;
            int groupEnd = i;
            float minX = platforms[i].MinX;
            float maxX = platforms[i].MaxX;
            float topY = platforms[i].TopY;

            while (groupEnd + 1 < platforms.Count && CanMergeScoreMaxPlatforms(platforms[groupEnd], platforms[groupEnd + 1]))
            {
                groupEnd++;
                minX = Mathf.Min(minX, platforms[groupEnd].MinX);
                maxX = Mathf.Max(maxX, platforms[groupEnd].MaxX);
                topY = Mathf.Max(topY, platforms[groupEnd].TopY);
            }

            if (groupEnd > groupStart)
            {
                DisablePlatformColliders(root, platforms, groupStart, groupEnd);

                for (int mergedIndex = groupStart; mergedIndex <= groupEnd; mergedIndex++)
                {
                    coveredByMergedCollider[mergedIndex] = true;
                }

                disabledPlatformColliderCount += groupEnd - groupStart + 1;
                CreateMergedScoreMaxPlatformCollider(
                    root,
                    $"ScoreMaxMergedCollider_{mergedColliderCount + 1:00}",
                    minX,
                    maxX,
                    topY,
                    noFrictionMaterial);
                mergedColliderCount++;
            }

            i = groupEnd;
        }

        if (ScoreMaxDebugPlatformMerge)
        {
            int afterColliderCount = beforeColliderCount - disabledPlatformColliderCount + mergedColliderCount;
            Debug.Log(
                $"[SCOREMAX PLATFORM MERGE] before={beforeColliderCount} " +
                $"mergedRuns={mergedColliderCount} after={afterColliderCount}");
            WarnIfScoreMaxPlatformSeamsRemain(platforms, coveredByMergedCollider);
        }
    }

    private static bool CanMergeScoreMaxPlatforms(FinalPlatformBlock current, FinalPlatformBlock next)
    {
        float yDelta = Mathf.Abs(current.TopY - next.TopY);
        float gap = next.MinX - current.MaxX;

        return yDelta <= ScoreMaxPlatformMergeYTolerance &&
               gap >= -ScoreMaxPlatformMergeGapTolerance &&
               gap <= ScoreMaxPlatformMergeGapTolerance;
    }

    private static void DisablePlatformColliders(
        Transform root,
        List<FinalPlatformBlock> platforms,
        int groupStart,
        int groupEnd)
    {
        for (int i = groupStart; i <= groupEnd; i++)
        {
            Transform platform = root.Find(platforms[i].Name);

            if (platform == null)
            {
                continue;
            }

            Collider2D[] colliders = platform.GetComponents<Collider2D>();

            for (int j = 0; j < colliders.Length; j++)
            {
                colliders[j].enabled = false;
            }
        }
    }

    private static void CreateMergedScoreMaxPlatformCollider(
        Transform root,
        string name,
        float minX,
        float maxX,
        float topY,
        PhysicsMaterial2D noFrictionMaterial)
    {
        GameObject platformCollider = new GameObject(name);
        platformCollider.layer = LayerMask.NameToLayer("Ground");
        platformCollider.transform.SetParent(root, false);
        platformCollider.transform.position = new Vector3((minX + maxX) * 0.5f, topY - 0.2f, 0f);

        BoxCollider2D collider = platformCollider.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(Mathf.Max(0.1f, maxX - minX), 0.4f);
        collider.isTrigger = false;
        collider.sharedMaterial = noFrictionMaterial;
    }

    private static void WarnIfScoreMaxPlatformSeamsRemain(
        List<FinalPlatformBlock> platforms,
        bool[] coveredByMergedCollider)
    {
        for (int i = 0; i < platforms.Count - 1; i++)
        {
            bool handledByMergedCollider =
                coveredByMergedCollider != null &&
                i < coveredByMergedCollider.Length - 1 &&
                coveredByMergedCollider[i] &&
                coveredByMergedCollider[i + 1];

            if (!handledByMergedCollider && CanMergeScoreMaxPlatforms(platforms[i], platforms[i + 1]))
            {
                Debug.LogWarning(
                    $"[SCOREMAX PLATFORM MERGE] Near seam remains between " +
                    $"{platforms[i].Name} and {platforms[i + 1].Name}.");
            }
        }
    }

    private static void PlaceScoreMaxObjectives(
        Transform root,
        List<FinalPlatformBlock> platforms,
        Sprite sprite,
        ScoreAttackManager manager,
        Transform goal)
    {
        List<FinalPlatformBlock> safePlatforms = GetObjectivePlatforms(platforms, goal);

        if (safePlatforms.Count == 0)
        {
            return;
        }

        FinalPlatformBlock coinLine = FindPlatformByName(safePlatforms, "FlatRun_CoinLine", safePlatforms[0]);
        FinalPlatformBlock coinArc = FindPlatformByName(
            safePlatforms,
            "PlatformChain_CoinArc_1",
            safePlatforms[Mathf.Min(1, safePlatforms.Count - 1)]);
        FinalPlatformBlock coinPocket = FindPlatformByName(
            safePlatforms,
            "SafeDrop_CoinPocket",
            safePlatforms[Mathf.Min(2, safePlatforms.Count - 1)]);
        FinalPlatformBlock androidAfterGap = FindPlatformByName(
            safePlatforms,
            "MediumGap_AndroidLanding_A",
            safePlatforms[Mathf.Min(2, safePlatforms.Count - 1)]);
        FinalPlatformBlock androidPlatform = FindPlatformByName(
            safePlatforms,
            "AndroidPlatform_FinalApproach",
            safePlatforms[Mathf.Min(3, safePlatforms.Count - 1)]);

        CreateScoreAttackCoin(
            root,
            "ScoreMax_CoinLine_01",
            new Vector3(coinLine.SafeMinX + 1.0f, coinLine.TopY + 1.2f, 0f),
            sprite,
            manager,
            ScoreMaxFinalCoinCollectionRadius);
        CreateScoreAttackCoin(
            root,
            "ScoreMax_CoinLine_02",
            new Vector3(coinLine.SafeMinX + 3.2f, coinLine.TopY + 1.2f, 0f),
            sprite,
            manager,
            ScoreMaxFinalCoinCollectionRadius);
        CreateScoreAttackCoin(
            root,
            "ScoreMax_CoinArc_01",
            new Vector3(coinArc.CenterX, coinArc.TopY + 1.65f, 0f),
            sprite,
            manager,
            ScoreMaxFinalCoinCollectionRadius);
        CreateScoreAttackCoin(
            root,
            "ScoreMax_CoinPocket_01",
            new Vector3(coinPocket.CenterX, coinPocket.TopY + 1.2f, 0f),
            sprite,
            manager,
            ScoreMaxFinalCoinCollectionRadius);

        CreateScoreAttackAndroid(
            root,
            "ScoreMax_AndroidAfterGap_01",
            new Vector3(androidAfterGap.CenterX, androidAfterGap.TopY + 1.02f, 0f),
            sprite,
            manager);
        CreateScoreAttackAndroid(
            root,
            "ScoreMax_AndroidPlatform_02",
            new Vector3(androidPlatform.CenterX, androidPlatform.TopY + 1.02f, 0f),
            sprite,
            manager);
    }

    private static List<FinalPlatformBlock> GetObjectivePlatforms(List<FinalPlatformBlock> platforms, Transform goal)
    {
        List<FinalPlatformBlock> safePlatforms = new List<FinalPlatformBlock>();

        for (int i = 1; i < platforms.Count - 1; i++)
        {
            FinalPlatformBlock platform = platforms[i];

            if (platform.Width < FinalMinLandingPlatformWidth)
            {
                continue;
            }

            if (goal != null && Mathf.Abs(platform.CenterX - goal.position.x) < 5.0f)
            {
                continue;
            }

            safePlatforms.Add(platform);
        }

        return safePlatforms;
    }

    private static FinalPlatformBlock FindPlatformByName(
        List<FinalPlatformBlock> platforms,
        string name,
        FinalPlatformBlock fallback)
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            if (platforms[i].Name == name)
            {
                return platforms[i];
            }
        }

        return fallback;
    }

    private static float GetLayoutCenterX(List<FinalPlatformBlock> platforms)
    {
        if (platforms.Count == 0)
        {
            return 0f;
        }

        return (platforms[0].MinX + platforms[platforms.Count - 1].MaxX) * 0.5f;
    }

    private static float GetLayoutWidth(List<FinalPlatformBlock> platforms)
    {
        if (platforms.Count == 0)
        {
            return 40f;
        }

        return Mathf.Max(40f, platforms[platforms.Count - 1].MaxX - platforms[0].MinX);
    }

    private readonly struct FinalPlatformBlock
    {
        public FinalPlatformBlock(string name, float centerX, float topY, float width)
        {
            Name = name;
            CenterX = centerX;
            TopY = topY;
            Width = width;
        }

        public string Name { get; }
        public float CenterX { get; }
        public float TopY { get; }
        public float Width { get; }
        public float MinX => CenterX - Width * 0.5f;
        public float MaxX => CenterX + Width * 0.5f;
        public float SafeMinX => MinX + FinalSafeEdgeMargin;
        public float SafeMaxX => MaxX - FinalSafeEdgeMargin;
    }

    private static void StripScoreAttackAndEnemyObjectsFromSpeedRunnerFinalScene()
    {
        ScoreAttackManager[] managers = Object.FindObjectsByType<ScoreAttackManager>(FindObjectsInactive.Include);

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null)
            {
                Object.DestroyImmediate(managers[i].gameObject);
            }
        }

        ScoreAttackCoin[] coins = Object.FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);

        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] != null)
            {
                Object.DestroyImmediate(coins[i].gameObject);
            }
        }

        ScoreAttackAndroid[] androids = Object.FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include);

        for (int i = 0; i < androids.Length; i++)
        {
            if (androids[i] != null)
            {
                Object.DestroyImmediate(androids[i].gameObject);
            }
        }

        ScoreAttackGoalLock[] goalLocks = Object.FindObjectsByType<ScoreAttackGoalLock>(FindObjectsInactive.Include);

        for (int i = 0; i < goalLocks.Length; i++)
        {
            if (goalLocks[i] != null)
            {
                Object.DestroyImmediate(goalLocks[i]);
            }
        }

        DemoEnemyHazard[] hazards = Object.FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Include);

        for (int i = 0; i < hazards.Length; i++)
        {
            if (hazards[i] != null)
            {
                Object.DestroyImmediate(hazards[i]);
            }
        }
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

    private static GameObject CreateScoreMaxPlayer(Vector3 position)
    {
        GameObject player = CreatePlayer(position);
        EnsureGroundCheckTransform(player);
        EnsureOnlyScoreMaxAgent(player);
        player.name = "Player_V5_ScoreMax";
        return player;
    }

    private static Transform EnsureGroundCheckTransform(GameObject player)
    {
        Transform groundCheck = FindChildByName(player.transform, "GroundCheck");

        if (groundCheck != null)
        {
            return groundCheck;
        }

        GameObject groundCheckObject = new GameObject("GroundCheck");
        groundCheckObject.transform.SetParent(player.transform, false);
        groundCheckObject.transform.localPosition = new Vector3(0f, -0.55f, 0f);
        return groundCheckObject.transform;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildByName(child, childName);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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
        SetBool(agent, "enableSpeedRunnerMode", true);
        SetBool(agent, "forceSprintInSpeedRunner", true);
        SetFloat(agent, "speedRunnerSprintReward", 0.0015f);
        SetBool(agent, "debugSpeedRunnerSprint", false);
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

    private static void ConfigureDirectSpeedRunnerPlayer(
        GameObject player,
        Transform goal,
        PhysicsMaterial2D noFrictionMaterial)
    {
        EdgeRunnerAgentV5 agent = RequireAgent(player);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Transform groundCheck = EnsureGroundCheckTransform(player);

        SetObjectReference(agent, "rb", rb);
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "groundCheck", groundCheck);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetObjectReference(agent, "mixedLevelGenerator", null);
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
        ApplyPhysicsMaterialToColliders(player, noFrictionMaterial);

        ConfigureBehavior(player, BehaviorType.Default);
        EnsureDecisionRequester(player, true);
    }

    private static void ConfigureSpeedRunnerSprintVisual(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        TrailRenderer trail = player.GetComponent<TrailRenderer>();

        if (trail == null)
        {
            trail = player.AddComponent<TrailRenderer>();
        }

        trail.time = 0.22f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.02f;
        trail.startColor = new Color(0.3f, 0.95f, 1f, 0.65f);
        trail.endColor = new Color(0.3f, 0.95f, 1f, 0f);
        trail.sortingOrder = 3;
        trail.emitting = false;

        Shader spriteShader = Shader.Find("Sprites/Default");

        if (spriteShader != null)
        {
            trail.sharedMaterial = new Material(spriteShader);
        }

        DemoSprintVisual sprintVisual = player.GetComponent<DemoSprintVisual>();

        if (sprintVisual == null)
        {
            sprintVisual = player.AddComponent<DemoSprintVisual>();
        }

        sprintVisual.enabled = true;
        sprintVisual.Configure(rb, spriteRenderer, trail);
        SetFloat(sprintVisual, "sprintOnThreshold", 9.6f);
        SetFloat(sprintVisual, "sprintOffThreshold", 7.8f);
        SetFloat(sprintVisual, "minSprintVisualTime", 0.35f);
        SetBool(sprintVisual, "enableTrail", true);
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

    private static void ConfigureScoreMaxPlayer(GameObject player, Transform goal, ScoreAttackManager manager)
    {
        EdgeRunnerAgentV5ScoreMax agent = EnsureOnlyScoreMaxAgent(player);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        SetObjectReference(agent, "rb", rb);
        SetObjectReference(agent, "goal", goal);
        SetObjectReference(agent, "gapGenerator", null);
        SetBool(agent, "useMixedLevelGenerator", false);
        SetObjectReference(agent, "mixedLevelGenerator", null);
        SetObjectReference(agent, "evaluationManager", null);
        SetObjectReference(agent, "scoreAttackManager", manager);
        SetInt(agent, "groundLayer", LayerMask.GetMask("Ground"));
        SetFloat(agent, "goalReward", 0.0f);
        SetFloat(agent, "deathPenalty", -5.0f);
        SetFloat(agent, "stepPenalty", -0.0015f);
        SetFloat(agent, "progressRewardScale", 0.0f);
        SetFloat(agent, "maxProgressRewardPerStep", 0.0f);
        SetFloat(agent, "milestoneReward", 0.0f);
        SetFloat(agent, "backtrackPenalty", 0.0f);
        SetFloat(agent, "jumpPenalty", 0.0f);
        SetFloat(agent, "idleJumpPenalty", 0.0f);
        SetFloat(agent, "flatGroundJumpPenalty", 0.0f);
        SetFloat(agent, "earlyGapJumpPenalty", 0.0f);
        SetFloat(agent, "uselessJumpPenalty", 0.0f);
        SetFloat(agent, "gapJumpReward", 0.0f);
        SetFloat(agent, "gapLandingReward", 0.0f);
        SetFloat(agent, "lowMomentumJumpPenalty", 0.0f);
        SetFloat(agent, "forwardActionReward", 0.0f);
        SetFloat(agent, "forwardVelocityReward", 0.0f);
        SetFloat(agent, "wrongDirectionActionPenalty", 0.0f);
        SetFloat(agent, "distanceProgressRewardScale", 0.0f);
        SetFloat(agent, "maxDistanceProgressReward", 0.0f);
        SetFloat(agent, "distanceRegressionPenaltyScale", 0.0f);
        SetFloat(agent, "maxDistanceRegressionPenalty", 0.0f);
        SetFloat(agent, "idlePenalty", -0.001f);
        SetFloat(agent, "noProgressTimeLimit", 12.0f);
        SetFloat(agent, "stuckTimeLimit", 12.0f);
        SetFloat(agent, "maxEpisodeTime", 70.0f);
        SetBool(agent, "disableTrainingEpisodeEndsInDemo", false);
        SetBool(agent, "disableAgentMovementInDemo", false);
        SetBool(agent, "enableSpeedRunnerMode", false);
        SetBool(agent, "forceSprintInSpeedRunner", false);
        SetBool(agent, "debugSpeedRunnerSprint", false);
        SetBool(agent, "enableLedgeUnstuck", false);
        SetBool(agent, "debugLedgeUnstuck", false);
        SetObjectReference(agent, "scoreMaxRb", rb);
        SetObjectReference(agent, "scoreMaxManager", manager);
        SetObjectReference(agent, "scoreMaxGoal", goal);
        SetInt(agent, "scoreMaxEnemyRayMask", ~0);
        SetFloat(agent, "progressToNextObjectiveRewardScale", 0.020f);
        SetFloat(agent, "maxProgressToNextObjectiveReward", 0.025f);
        SetFloat(agent, "maxRegressionFromNextObjectivePenalty", -0.015f);
        SetBool(agent, "debugScoreMaxObservations", false);
        SetBool(agent, "debugScoreMaxObservationCount", false);
        SetBool(agent, "debugScoreMaxObjectives", false);
        SetBool(agent, "debugScoreMaxHeuristicInput", false);
        SetBool(agent, "debugScoreMaxGroundCheck", false);
        SetBool(agent, "debugScoreMaxRewards", false);
        SetObjectReference(manager, "goal", goal);
        SetObjectReference(manager, "agent", agent);

        ConfigureScoreMaxBehavior(player, BehaviorType.Default);
        EnsureDecisionRequester(player, true);
    }

    private static void ConfigureScoreMaxCoinIntroDiagnostics(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax agent = RequireScoreMaxAgent(player);

        SetBool(agent, "debugScoreMaxObjectives", true);
        SetBool(agent, "debugScoreMaxRewards", true);
        SetBool(agent, "maskUselessJumps", false);
        SetFloat(agent, "missedCoinPenalty", -2.0f);
        SetBool(agent, "endEpisodeOnMissedCoinIntro", true);
        SetFloat(agent, "missedCoinForwardMargin", 2.0f);
        SetFloat(agent, "coinJumpCueReward", 0.3f);
        SetFloat(agent, "coinJumpCueHorizontalRange", 1.75f);
        SetFloat(agent, "coinJumpCueMinVerticalOffset", 0.1f);
    }

    private static void ConfigureScoreMaxEasyTutorial(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax agent = RequireScoreMaxAgent(player);

        SetFloat(agent, "missedCoinPenalty", -2.0f);
        SetBool(agent, "endEpisodeOnMissedCoinIntro", true);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "missedEnemyPenalty", -3.0f);
        SetBool(agent, "endEpisodeOnMissedEnemyEasy", true);
        SetFloat(agent, "missedEnemyForwardMargin", 2.5f);
        SetFloat(agent, "enemyStompCueReward", 0.3f);
        SetFloat(agent, "enemyStompCueHorizontalRange", 2.25f);
    }

    private static void ConfigureScoreMaxRandomWarmupTutorial(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax agent = RequireScoreMaxAgent(player);

        SetFloat(agent, "missedCoinPenalty", -2.0f);
        SetBool(agent, "endEpisodeOnMissedCoinIntro", true);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetFloat(agent, "missedEnemyPenalty", -3.0f);
        SetBool(agent, "endEpisodeOnMissedEnemyEasy", true);
        SetFloat(agent, "missedEnemyForwardMargin", 2.5f);
        SetFloat(agent, "coinJumpCueReward", 0.2f);
        SetFloat(agent, "coinJumpCueHorizontalRange", 1.75f);
        SetFloat(agent, "coinJumpCueMinVerticalOffset", 0.1f);
        SetFloat(agent, "enemyStompCueReward", 0.3f);
        SetFloat(agent, "enemyStompCueHorizontalRange", 2.25f);
    }

    private static void ConfigureScoreMaxRandomWarmupManager(
        ScoreAttackManager manager,
        Transform goal)
    {
        SetObjectReference(manager, "goal", goal);
        SetInt(manager, "minActiveCoins", 2);
        SetInt(manager, "maxActiveCoins", 2);
        SetInt(manager, "minActiveEnemies", 1);
        SetInt(manager, "maxActiveEnemies", 1);
        SetBool(manager, "useOrderedCoinXSlots", true);
        SetVector2(manager, "firstCoinXRange", new Vector2(3.0f, 4.5f));
        SetVector2(manager, "secondCoinXRange", new Vector2(5.8f, 7.2f));
        SetVector2(manager, "enemyRandomXRange", new Vector2(10.0f, 11.5f));
        SetFloat(manager, "coinPlatformTopY", 0f);
        SetFloat(manager, "coinVerticalOffset", 1.5f);
        SetFloat(manager, "minCoinSpacing", 2.0f);
        SetFloat(manager, "minCoinDistanceFromAndroid", 3.0f);
        SetFloat(manager, "minCoinDistanceFromGoal", 4.0f);
        SetFloat(manager, "prematureGoalPenalty", -2.0f);
        SetBool(manager, "endEpisodeOnPrematureGoal", true);
        SetBool(manager, "debugScoreMaxRandomWarmupPositions", false);
        SetInt(manager, "maxCoinPlacementAttempts", 30);
    }

    private static void ConfigureScoreMaxFinalRandomEpisodeLimits(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax agent = RequireScoreMaxAgent(player);

        SetFloat(agent, "noProgressTimeLimit", 60.0f);
        SetFloat(agent, "stuckTimeLimit", 35.0f);
        SetFloat(agent, "maxEpisodeTime", 240.0f);
        SetBool(agent, "disableTrainingEpisodeEndsInHeuristic", true);
        SetBool(agent, "debugEpisodeResetReason", true);
        SetBool(agent, "debugScoreMaxObservationCount", true);
        SetBool(agent, "debugScoreMaxHeuristicInput", true);
        SetBool(agent, "debugScoreMaxGroundCheck", true);
        SetFloat(agent, "missedCoinPenalty", -2.0f);
        SetBool(agent, "endEpisodeOnMissedCoinIntro", true);
        SetFloat(agent, "missedCoinForwardMargin", 2.5f);
        SetBool(agent, "detectAnyMissedObjectiveBehind", true);
        SetFloat(agent, "missedEnemyPenalty", -3.0f);
        SetBool(agent, "endEpisodeOnMissedEnemyEasy", true);
        SetFloat(agent, "missedEnemyForwardMargin", 2.5f);
        SetBool(agent, "requireCoinsCompleteBeforeMissedEnemyCheck", false);
        SetFloat(agent, "coinJumpCueReward", 0.04f);
        SetFloat(agent, "coinJumpCueHorizontalRange", 2.25f);
        SetFloat(agent, "coinJumpCueMinVerticalOffset", 0.45f);
        SetFloat(agent, "enemyStompCueReward", 0.05f);
        SetFloat(agent, "enemyStompCueHorizontalRange", 2.75f);
        SetBool(agent, "enableScoreMaxContextualShaping", true);
        SetFloat(agent, "scoreMaxUselessJumpPenalty", -0.01f);
        SetFloat(agent, "lowCoinHeightThreshold", 0.45f);
        SetFloat(agent, "lowCoinApproachRange", 3.0f);
        SetFloat(agent, "groundCoinApproachReward", 0.01f);
        SetFloat(agent, "lowCoinUnnecessaryJumpPenalty", -0.01f);
        SetFloat(agent, "contextualCoinJumpRange", 2.25f);
        SetFloat(agent, "contextualEnemyJumpRange", 2.75f);
        SetFloat(agent, "enemyStompAlignmentReward", 0.08f);
        SetFloat(agent, "enemyStompAlignmentHorizontalTolerance", 0.9f);
        SetFloat(agent, "enemyStompAlignmentMinHeight", 0.35f);
        SetFloat(agent, "enemyStompAlignmentMaxUpwardVelocity", 0.1f);
        SetBool(agent, "debugScoreMaxNextObjective", false);
    }

    private static void ConfigureScoreMaxFinalRandomManager(ScoreAttackManager manager)
    {
        SetFloat(manager, "prematureGoalPenalty", -2.0f);
        SetBool(manager, "endEpisodeOnPrematureGoal", true);
        SetBool(manager, "preferForwardCoinObjectives", true);
        SetFloat(manager, "forwardCoinObjectiveTolerance", 0.25f);
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

    private static EdgeRunnerAgentV5ScoreMax EnsureOnlyScoreMaxAgent(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax scoreMaxAgent = null;
        EdgeRunnerAgentV5[] agents = player.GetComponents<EdgeRunnerAgentV5>();

        for (int i = 0; i < agents.Length; i++)
        {
            EdgeRunnerAgentV5 agent = agents[i];

            if (agent == null)
            {
                continue;
            }

            if (agent is EdgeRunnerAgentV5ScoreMax candidate)
            {
                if (scoreMaxAgent == null)
                {
                    scoreMaxAgent = candidate;
                }
                else
                {
                    Object.DestroyImmediate(candidate);
                }

                continue;
            }

            if (agent.GetType() == typeof(EdgeRunnerAgentV5))
            {
                Object.DestroyImmediate(agent);
            }
        }

        if (scoreMaxAgent == null)
        {
            scoreMaxAgent = player.AddComponent<EdgeRunnerAgentV5ScoreMax>();
        }

        return scoreMaxAgent;
    }

    private static EdgeRunnerAgentV5ScoreMax RequireScoreMaxAgent(GameObject player)
    {
        EdgeRunnerAgentV5ScoreMax agent = player.GetComponent<EdgeRunnerAgentV5ScoreMax>();

        if (agent == null)
        {
            throw new System.InvalidOperationException("ScoreMax player instance does not contain EdgeRunnerAgentV5ScoreMax.");
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

    private static void ConfigureScoreMaxBehavior(GameObject player, BehaviorType behaviorType)
    {
        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();

        if (behavior == null)
        {
            behavior = player.AddComponent<BehaviorParameters>();
        }

        behavior.BehaviorName = "EdgeRunnerV5ScoreMax";
        behavior.BehaviorType = behaviorType;

        SerializedObject serializedObject = new SerializedObject(behavior);
        SerializedProperty vectorObservationSize = serializedObject.FindProperty("m_BrainParameters.VectorObservationSize");

        if (vectorObservationSize != null)
        {
            vectorObservationSize.intValue = EdgeRunnerAgentV5ScoreMax.DefaultExpectedObservationSize;
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

    private static ScoreAttackManager CreateScoreMaxManager(Transform parent, bool randomize, int maxCoins, int maxEnemies)
    {
        ScoreAttackManager manager = CreateScoreAttackManager(parent, randomize, maxCoins, maxEnemies);

        SetFloat(manager, "coinReward", 2.0f);
        SetFloat(manager, "enemyKillReward", 4.0f);
        SetFloat(manager, "finalCompletionReward", 10.0f);
        SetFloat(manager, "prematureGoalPenalty", -1.0f);
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

    private static GameObject CreateFinalGoal(string name, Vector3 position, Vector2 triggerSize)
    {
        GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal = goalPrefab != null
            ? PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject
            : new GameObject(name);

        if (goal == null)
        {
            throw new System.InvalidOperationException("Failed to create final goal.");
        }

        goal.name = name;
        goal.tag = "Goal";
        goal.transform.position = position;
        goal.transform.localScale = Vector3.one;
        ConfigureGoalTrigger(goal, triggerSize);
        return goal;
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

    private static void ConfigureGoalTrigger(GameObject goal, Vector2 triggerSize)
    {
        Collider2D[] colliders = goal.GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] is BoxCollider2D)
            {
                continue;
            }

            colliders[i].enabled = false;
        }

        BoxCollider2D boxCollider = goal.GetComponent<BoxCollider2D>();

        if (boxCollider == null)
        {
            boxCollider = goal.AddComponent<BoxCollider2D>();
        }

        boxCollider.enabled = true;
        boxCollider.isTrigger = true;
        boxCollider.size = triggerSize;
        boxCollider.offset = new Vector2(0f, triggerSize.y * 0.5f - 1.1f);
    }

    private static void CreateScoreAttackCoin(
        Transform parent,
        string name,
        Vector3 position,
        Sprite sprite,
        ScoreAttackManager manager,
        float collectionRadius = 0.45f)
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
        collider.radius = Mathf.Max(0.05f, collectionRadius);

        ScoreAttackCoin coinScript = coin.AddComponent<ScoreAttackCoin>();
        coinScript.SetManager(manager);
    }

    private static void CreateScoreAttackAndroid(
        Transform parent,
        string name,
        Vector3 position,
        Sprite fallbackSprite,
        ScoreAttackManager manager,
        bool enablePatrol = true)
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
        if (!enablePatrol)
        {
            RemoveAndroidPatrol(android);
        }

        ScoreAttackAndroid androidScript = android.GetComponent<ScoreAttackAndroid>();

        if (androidScript == null)
        {
            androidScript = android.AddComponent<ScoreAttackAndroid>();
        }

        androidScript.enabled = true;
        androidScript.SetManager(manager);

        if (!enablePatrol)
        {
            SetFloat(androidScript, "stompHeightOffset", 0.10f);
            SetFloat(androidScript, "stompTopTolerance", 0.45f);
        }
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
