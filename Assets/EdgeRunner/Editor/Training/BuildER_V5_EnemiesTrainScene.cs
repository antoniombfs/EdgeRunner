using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildER_V5_EnemiesTrainScene
{
    private const string ScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_Train.unity";
    private const string NavWarmupScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_NavWarmup.unity";
    private const string StaticIntroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntro.unity";
    private const string StaticIntroJumpCueScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroJumpCue.unity";
    private const string StaticIntroForcedJumpScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroForcedJump.unity";
    private const string StaticIntroEnemyRaysScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRays.unity";
    private const string StaticIntroEnemyRaysApproachGateScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysApproachGate.unity";
    private const string StaticIntroEnemyRaysPrematureAirScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_StaticIntroEnemyRaysPrematureAir.unity";
    private const string AvoidanceMicroScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicro.unity";
    private const string AvoidanceMicroMaskedScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroMasked.unity";
    private const string AvoidanceMicroEnemyRaysScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRays.unity";
    private const string AvoidanceMicroEnemyRaysForcedJumpScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysForcedJump.unity";
    private const string AvoidanceMicroEnemyRaysSweetSpotStartScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysSweetSpotStart.unity";
    private const string AvoidanceMicroEnemyRaysJumpCommitTutorialScenePath = "Assets/EdgeRunner/Scenes/Training/ER_V5_Enemies_AvoidanceMicroEnemyRaysJumpCommitTutorial.unity";
    private const string DemoDirectoryPath = "Assets/EdgeRunner/Demos";
    private const string ForcedJumpDemoName = "EnemyAware_ForcedJump_Demo";
    private const string DemonstrationRecorderTypeName = "Unity.MLAgents.Demonstrations.DemonstrationRecorder, Unity.MLAgents";
    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5_Enemies.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string AndroidEnemyPrefabPath = "Assets/EdgeRunner/Prefabs/Demo/DemoAndroidEnemy.prefab";
    private const float MicroGroundedPlayerY = 0.51f;
    private static readonly bool showSprintVisualInTraining = false;

    [MenuItem("EdgeRunner/Training/Build ER_V5_Enemies_Train")]
    public static void BuildFromMenu()
    {
        BuildEnemyIntroScene();
    }

    public static void BuildSceneFromCommandLine()
    {
        BuildEnemyIntroScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build NavWarmup")]
    public static void BuildNavWarmupFromMenu()
    {
        BuildNavWarmupScene();
    }

    public static void BuildNavWarmupSceneFromCommandLine()
    {
        BuildNavWarmupScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntro")]
    public static void BuildStaticIntroFromMenu()
    {
        BuildStaticIntroScene();
    }

    public static void BuildStaticIntroSceneFromCommandLine()
    {
        BuildStaticIntroScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntroJumpCue")]
    public static void BuildStaticIntroJumpCueFromMenu()
    {
        BuildStaticIntroJumpCueScene();
    }

    public static void BuildStaticIntroJumpCueSceneFromCommandLine()
    {
        BuildStaticIntroJumpCueScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntroForcedJump")]
    public static void BuildStaticIntroForcedJumpFromMenu()
    {
        BuildStaticIntroForcedJumpScene();
    }

    public static void BuildStaticIntroForcedJumpSceneFromCommandLine()
    {
        BuildStaticIntroForcedJumpScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntroEnemyRays")]
    public static void BuildStaticIntroEnemyRaysFromMenu()
    {
        BuildStaticIntroEnemyRaysScene();
    }

    public static void BuildStaticIntroEnemyRaysSceneFromCommandLine()
    {
        BuildStaticIntroEnemyRaysScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntroEnemyRaysApproachGate")]
    public static void BuildStaticIntroEnemyRaysApproachGateFromMenu()
    {
        BuildStaticIntroEnemyRaysApproachGateScene();
    }

    public static void BuildStaticIntroEnemyRaysApproachGateSceneFromCommandLine()
    {
        BuildStaticIntroEnemyRaysApproachGateScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build StaticIntroEnemyRaysPrematureAir")]
    public static void BuildStaticIntroEnemyRaysPrematureAirFromMenu()
    {
        BuildStaticIntroEnemyRaysPrematureAirScene();
    }

    public static void BuildStaticIntroEnemyRaysPrematureAirSceneFromCommandLine()
    {
        BuildStaticIntroEnemyRaysPrematureAirScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicro")]
    public static void BuildAvoidanceMicroFromMenu()
    {
        BuildAvoidanceMicroScene();
    }

    public static void BuildAvoidanceMicroSceneFromCommandLine()
    {
        BuildAvoidanceMicroScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicroMasked")]
    public static void BuildAvoidanceMicroMaskedFromMenu()
    {
        BuildAvoidanceMicroMaskedScene();
    }

    public static void BuildAvoidanceMicroMaskedSceneFromCommandLine()
    {
        BuildAvoidanceMicroMaskedScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicroEnemyRays")]
    public static void BuildAvoidanceMicroEnemyRaysFromMenu()
    {
        BuildAvoidanceMicroEnemyRaysScene();
    }

    public static void BuildAvoidanceMicroEnemyRaysSceneFromCommandLine()
    {
        BuildAvoidanceMicroEnemyRaysScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicroEnemyRaysForcedJump")]
    public static void BuildAvoidanceMicroEnemyRaysForcedJumpFromMenu()
    {
        BuildAvoidanceMicroEnemyRaysForcedJumpScene();
    }

    public static void BuildAvoidanceMicroEnemyRaysForcedJumpSceneFromCommandLine()
    {
        BuildAvoidanceMicroEnemyRaysForcedJumpScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicroEnemyRaysSweetSpotStart")]
    public static void BuildAvoidanceMicroEnemyRaysSweetSpotStartFromMenu()
    {
        BuildAvoidanceMicroEnemyRaysSweetSpotStartScene();
    }

    public static void BuildAvoidanceMicroEnemyRaysSweetSpotStartSceneFromCommandLine()
    {
        BuildAvoidanceMicroEnemyRaysSweetSpotStartScene();
    }

    [MenuItem("EdgeRunner/Training/EnemyAware/Build AvoidanceMicroEnemyRaysJumpCommitTutorial")]
    public static void BuildAvoidanceMicroEnemyRaysJumpCommitTutorialFromMenu()
    {
        BuildAvoidanceMicroEnemyRaysJumpCommitTutorialScene();
    }

    public static void BuildAvoidanceMicroEnemyRaysJumpCommitTutorialSceneFromCommandLine()
    {
        BuildAvoidanceMicroEnemyRaysJumpCommitTutorialScene();
    }

    private static void BuildEnemyIntroScene()
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

    private static void BuildNavWarmupScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_NavWarmup");
        GameObject player = CreatePlayer(new Vector3(-10f, 1.15f, 0f));
        GameObject goal = CreateGoal(new Vector3(34f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureNavWarmup: true);
        CreateCamera(player.transform);
        CreateNavWarmupLevel(root.transform, sharedSprite);
        CreateDeathZone(12f, 80f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, NavWarmupScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware NavWarmup scene: {NavWarmupScenePath}");
    }

    private static void BuildStaticIntroScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntro");
        GameObject player = CreatePlayer(new Vector3(-10f, 1.15f, 0f));
        GameObject goal = CreateGoal(new Vector3(48f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntro: true);
        CreateCamera(player.transform);
        CreateStaticIntroLevel(root.transform, sharedSprite);
        CreateStaticIntroEnemy(root.transform, sharedSprite);
        CreateDeathZone(18f, 100f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntro scene: {StaticIntroScenePath}");
    }

    private static void BuildStaticIntroJumpCueScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntroJumpCue");
        GameObject player = CreatePlayer(new Vector3(-10f, 1.15f, 0f));
        GameObject goal = CreateGoal(new Vector3(40f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntroJumpCue: true);
        CreateCamera(player.transform);
        CreateStaticIntroJumpCueLevel(root.transform, sharedSprite);
        CreateStaticIntroJumpCueEnemy(root.transform, sharedSprite);
        CreateDeathZone(16f, 90f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroJumpCueScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntroJumpCue scene: {StaticIntroJumpCueScenePath}");
    }

    private static void BuildStaticIntroForcedJumpScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntroForcedJump");
        GameObject player = CreatePlayer(new Vector3(-12f, 1.15f, 0f));
        GameObject goal = CreateGoal(new Vector3(44f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntroForcedJump: true);
        ConfigureStaticIntroForcedJumpDemonstrationRecorder(player);
        CreateCamera(player.transform);
        CreateStaticIntroForcedJumpLevel(root.transform, sharedSprite);
        CreateStaticIntroForcedJumpEnemy(root.transform, sharedSprite);
        CreateDeathZone(16f, 95f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroForcedJumpScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntroForcedJump scene: {StaticIntroForcedJumpScenePath}");
    }

    private static void BuildStaticIntroEnemyRaysScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntroEnemyRays");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(15f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntroEnemyRays: true);
        CreateCamera(player.transform);
        CreateStaticIntroEnemyRaysLevel(root.transform, sharedSprite);
        CreateStaticIntroEnemyRaysEnemy(root.transform, sharedSprite);
        CreateDeathZone(7.5f, 36f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroEnemyRaysScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntroEnemyRays scene: {StaticIntroEnemyRaysScenePath}");
    }

    private static void BuildStaticIntroEnemyRaysApproachGateScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntroEnemyRaysApproachGate");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(15f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntroEnemyRaysApproachGate: true);
        CreateCamera(player.transform);
        CreateStaticIntroEnemyRaysApproachGateLevel(root.transform, sharedSprite);
        CreateStaticIntroEnemyRaysApproachGateEnemy(root.transform, sharedSprite);
        CreateDeathZone(7.5f, 36f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroEnemyRaysApproachGateScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntroEnemyRaysApproachGate scene: {StaticIntroEnemyRaysApproachGateScenePath}");
    }

    private static void BuildStaticIntroEnemyRaysPrematureAirScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_StaticIntroEnemyRaysPrematureAir");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(15f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureStaticIntroEnemyRaysPrematureAir: true);
        CreateCamera(player.transform);
        CreateStaticIntroEnemyRaysPrematureAirLevel(root.transform, sharedSprite);
        CreateStaticIntroEnemyRaysPrematureAirEnemy(root.transform, sharedSprite);
        CreateDeathZone(7.5f, 36f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, StaticIntroEnemyRaysPrematureAirScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware StaticIntroEnemyRaysPrematureAir scene: {StaticIntroEnemyRaysPrematureAirScenePath}");
    }

    private static void BuildAvoidanceMicroScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicro");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(10f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicro: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite);
        CreateDeathZone(5f, 28f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicro scene: {AvoidanceMicroScenePath}");
    }

    private static void BuildAvoidanceMicroMaskedScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicroMasked");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(11f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicroMasked: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite, "Android_EnemyAware_AvoidanceMicroMasked01", 6f);
        CreateDeathZone(5.5f, 30f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroMaskedScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicroMasked scene: {AvoidanceMicroMaskedScenePath}");
    }

    private static void BuildAvoidanceMicroEnemyRaysScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicroEnemyRays");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(11f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicroEnemyRays: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite, "Android_EnemyAware_AvoidanceMicroEnemyRays01", 6f);
        CreateDeathZone(5.5f, 30f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroEnemyRaysScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicroEnemyRays scene: {AvoidanceMicroEnemyRaysScenePath}");
    }

    private static void BuildAvoidanceMicroEnemyRaysForcedJumpScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicroEnemyRaysForcedJump");
        GameObject player = CreatePlayer(new Vector3(0f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(11f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicroEnemyRaysForcedJump: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite, "Android_EnemyAware_AvoidanceMicroEnemyRaysForcedJump01", 6f);
        CreateDeathZone(5.5f, 30f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroEnemyRaysForcedJumpScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicroEnemyRaysForcedJump scene: {AvoidanceMicroEnemyRaysForcedJumpScenePath}");
    }

    private static void BuildAvoidanceMicroEnemyRaysSweetSpotStartScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicroEnemyRaysSweetSpotStart");
        GameObject player = CreatePlayer(new Vector3(2.8f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(10.5f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicroEnemyRaysSweetSpotStart: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite, "Android_EnemyAware_AvoidanceMicroEnemyRaysSweetSpotStart01", 6f);
        CreateDeathZone(5.5f, 30f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroEnemyRaysSweetSpotStartScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicroEnemyRaysSweetSpotStart scene: {AvoidanceMicroEnemyRaysSweetSpotStartScenePath}");
    }

    private static void BuildAvoidanceMicroEnemyRaysJumpCommitTutorialScene()
    {
        EnsureFolders();

        Sprite sharedSprite = GetSharedSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("ER_V5_Enemies_AvoidanceMicroEnemyRaysJumpCommitTutorial");
        GameObject player = CreatePlayer(new Vector3(2.8f, MicroGroundedPlayerY, 0f));
        GameObject goal = CreateGoal(new Vector3(10.5f, 1.1f, 0f));

        ConfigurePlayer(player, goal.transform, configureAvoidanceMicroEnemyRaysJumpCommitTutorial: true);
        CreateCamera(player.transform);
        CreateAvoidanceMicroLevel(root.transform, sharedSprite);
        CreateAvoidanceMicroEnemy(root.transform, sharedSprite, "Android_EnemyAware_AvoidanceMicroEnemyRaysJumpCommitTutorial01", 6f);
        CreateDeathZone(5.5f, 30f);

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvoidanceMicroEnemyRaysJumpCommitTutorialScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 enemy-aware AvoidanceMicroEnemyRaysJumpCommitTutorial scene: {AvoidanceMicroEnemyRaysJumpCommitTutorialScenePath}");
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
        return CreatePlayer(new Vector3(-10f, 1.15f, 0f));
    }

    private static GameObject CreatePlayer(Vector3 position)
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
        player.transform.position = position;
        return player;
    }

    private static void ConfigurePlayer(GameObject player, Transform goal)
    {
        ConfigurePlayer(player, goal, configureNavWarmup: false);
    }

    private static void ConfigurePlayer(
        GameObject player,
        Transform goal,
        bool configureNavWarmup = false,
        bool configureStaticIntro = false,
        bool configureStaticIntroJumpCue = false,
        bool configureStaticIntroForcedJump = false,
        bool configureStaticIntroEnemyRays = false,
        bool configureStaticIntroEnemyRaysApproachGate = false,
        bool configureStaticIntroEnemyRaysPrematureAir = false,
        bool configureAvoidanceMicro = false,
        bool configureAvoidanceMicroMasked = false,
        bool configureAvoidanceMicroEnemyRays = false,
        bool configureAvoidanceMicroEnemyRaysForcedJump = false,
        bool configureAvoidanceMicroEnemyRaysSweetSpotStart = false,
        bool configureAvoidanceMicroEnemyRaysJumpCommitTutorial = false)
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

        if (configureNavWarmup)
        {
            ConfigureNavWarmupAgent(agent);
        }

        if (configureStaticIntro)
        {
            ConfigureStaticIntroAgent(agent);
        }

        if (configureStaticIntroJumpCue)
        {
            ConfigureStaticIntroJumpCueAgent(agent);
        }

        if (configureStaticIntroForcedJump)
        {
            ConfigureStaticIntroForcedJumpAgent(agent);
        }

        if (configureStaticIntroEnemyRays)
        {
            ConfigureStaticIntroEnemyRaysAgent(agent);
        }

        if (configureStaticIntroEnemyRaysApproachGate)
        {
            ConfigureStaticIntroEnemyRaysApproachGateAgent(agent);
        }

        if (configureStaticIntroEnemyRaysPrematureAir)
        {
            ConfigureStaticIntroEnemyRaysPrematureAirAgent(agent);
        }

        if (configureAvoidanceMicro)
        {
            ConfigureAvoidanceMicroAgent(agent);
        }

        if (configureAvoidanceMicroMasked)
        {
            ConfigureAvoidanceMicroMaskedAgent(agent);
        }

        if (configureAvoidanceMicroEnemyRays)
        {
            ConfigureAvoidanceMicroEnemyRaysAgent(agent);
        }

        if (configureAvoidanceMicroEnemyRaysForcedJump)
        {
            ConfigureAvoidanceMicroEnemyRaysForcedJumpAgent(agent);
        }

        if (configureAvoidanceMicroEnemyRaysSweetSpotStart)
        {
            ConfigureAvoidanceMicroEnemyRaysSweetSpotStartAgent(agent);
        }

        if (configureAvoidanceMicroEnemyRaysJumpCommitTutorial)
        {
            ConfigureAvoidanceMicroEnemyRaysJumpCommitTutorialAgent(agent);
        }

        BehaviorParameters behavior = player.GetComponent<BehaviorParameters>();

        if (behavior != null)
        {
            behavior.BehaviorName = "EdgeRunnerV5Enemies";
            behavior.BehaviorType = BehaviorType.Default;
            ConfigureBehaviorBrain(behavior);
        }

        ConfigureSprintVisual(player, rb);
    }

    private static void ConfigureNavWarmupAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetFloat(agent, "noProgressTimeLimit", 12f);
        SetFloat(agent, "stuckTimeLimit", 12f);
        SetFloat(agent, "maxEpisodeTime", 60f);
        SetFloat(agent, "distanceProgressRewardScale", 0.10f);
        SetFloat(agent, "maxDistanceProgressReward", 0.10f);
        SetFloat(agent, "progressRewardScale", 0.08f);
        SetFloat(agent, "maxProgressRewardPerStep", 0.08f);
        SetFloat(agent, "stepPenalty", -0.0002f);
        SetFloat(agent, "minDistanceProgressForReset", 0.03f);
        SetBool(agent, "rewardPassedEnemies", false);
    }

    private static void ConfigureStaticIntroAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetFloat(agent, "enemyHitPenalty", -5.0f);
        SetFloat(agent, "enemyPassReward", 0.5f);
        SetFloat(agent, "enemyPassMargin", 1.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", -0.005f);
        SetFloat(agent, "enemyDetectionRangeX", 15f);
        SetFloat(agent, "enemyDetectionRangeY", 6f);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureStaticIntroJumpCueAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetFloat(agent, "enemyHitPenalty", -10.0f);
        SetFloat(agent, "enemyPassReward", 1.0f);
        SetFloat(agent, "enemyPassMargin", 1.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", -0.005f);
        SetFloat(agent, "enemyApproachPenalty", -0.01f);
        SetFloat(agent, "enemyJumpCueReward", 0.05f);
        SetFloat(agent, "enemyAvoidanceWindowX", 3.0f);
        SetFloat(agent, "enemyVerticalDangerTolerance", 1.0f);
        SetFloat(agent, "enemyJumpCueMinUpVelocity", 0.5f);
        SetFloat(agent, "enemyDetectionRangeX", 15f);
        SetFloat(agent, "enemyDetectionRangeY", 6f);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureStaticIntroForcedJumpAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetFloat(agent, "enemyHitPenalty", -15.0f);
        SetFloat(agent, "enemyPassReward", 2.0f);
        SetFloat(agent, "enemyPassMargin", 1.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", -0.005f);
        SetFloat(agent, "enemyApproachPenalty", -0.03f);
        SetFloat(agent, "enemyJumpCueReward", 0.20f);
        SetFloat(agent, "enemyAvoidanceWindowX", 5.0f);
        SetFloat(agent, "enemyVerticalDangerTolerance", 1.2f);
        SetFloat(agent, "enemyJumpCueMinUpVelocity", 0.2f);
        SetFloat(agent, "enemyDetectionRangeX", 18f);
        SetFloat(agent, "enemyDetectionRangeY", 6f);
        SetBool(agent, "disableProgressRewardNearEnemy", true);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureStaticIntroEnemyRaysAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetBool(agent, "enableEnemyAwareness", true);
        SetBool(agent, "useEnemyRayObservations", true);
        SetBool(agent, "forceJumpActionNearEnemy", false);
        SetBool(agent, "enableJumpCommitMask", false);
        SetBool(agent, "enableAirCommitAfterJump", false);
        SetBool(agent, "maskForwardActionNearEnemy", false);
        SetBool(agent, "useRuleBasedCommitTest", false);
        SetBool(agent, "disableProgressRewardNearEnemy", false);
        SetBool(agent, "maskUselessJumps", false);
        SetBool(agent, "enableJumpCommitReward", false);
        SetBool(agent, "waitUntilGroundedOnEpisodeStart", true);
        SetFloat(agent, "episodeStartSettleMaxSeconds", 1.0f);
        SetBool(agent, "episodeStartSettleFreezeMovement", true);
        SetFloat(agent, "enemyHitPenalty", -8.0f);
        SetFloat(agent, "enemyPassReward", 6.0f);
        SetFloat(agent, "enemyJumpCueReward", 0.5f);
        SetFloat(agent, "enemyApproachPenalty", 0.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", 0.0f);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "enableRetreatPenalty", true);
        SetFloat(agent, "retreatPenalty", -0.02f);
        SetFloat(agent, "retreatEndDistance", 2.0f);
        SetBool(agent, "enableShortMicroTimeout", false);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRayObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
        SetBool(agent, "debugForcedJumpMask", false);
        SetBool(agent, "debugJumpCommitMask", false);
        SetBool(agent, "debugAirCommit", false);
    }

    private static void ConfigureStaticIntroEnemyRaysApproachGateAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureStaticIntroEnemyRaysAgent(agent);
        SetBool(agent, "maskPrematureEnemyJumps", true);
        SetFloat(agent, "prematureJumpMinThreatDistance", 2.6f);
        SetFloat(agent, "prematureJumpMaxThreatDistance", 3.8f);
        SetBool(agent, "debugPrematureJumpMask", false);
        SetBool(agent, "enableJumpCommitMask", true);
        SetBool(agent, "enableJumpCommitReward", true);
        SetBool(agent, "enableAirCommitAfterJump", true);
        SetFloat(agent, "airCommitDuration", 0.75f);
        SetBool(agent, "airCommitUntilEnemyPassed", true);
        SetBool(agent, "forceJumpActionNearEnemy", false);
        SetBool(agent, "maskForwardActionNearEnemy", false);
        SetBool(agent, "useRuleBasedCommitTest", false);
        SetBool(agent, "disableProgressRewardNearEnemy", false);
        SetBool(agent, "maskUselessJumps", false);
        SetBool(agent, "waitUntilGroundedOnEpisodeStart", true);
        SetFloat(agent, "episodeStartSettleMaxSeconds", 1.0f);
        SetBool(agent, "episodeStartSettleFreezeMovement", true);
        SetFloat(agent, "enemyHitPenalty", -6.0f);
        SetFloat(agent, "enemyPassReward", 8.0f);
        SetFloat(agent, "enemyJumpCueReward", 1.0f);
        SetFloat(agent, "jumpCommitReward", 1.0f);
        SetFloat(agent, "enemyApproachPenalty", 0.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", 0.0f);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "enableRetreatPenalty", true);
        SetFloat(agent, "retreatPenalty", -0.02f);
        SetFloat(agent, "retreatEndDistance", 2.0f);
        SetBool(agent, "enableShortMicroTimeout", true);
        SetFloat(agent, "microTimeoutSeconds", 8.0f);
        SetFloat(agent, "microTimeoutPenalty", -3.0f);
        SetFloat(agent, "jumpCommitMinDistance", 2.6f);
        SetFloat(agent, "jumpCommitMaxDistance", 3.8f);
        SetBool(agent, "jumpCommitOnlyOncePerEnemy", true);
        SetBool(agent, "debugJumpCommitMask", false);
        SetBool(agent, "debugAirCommit", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureStaticIntroEnemyRaysPrematureAirAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureStaticIntroEnemyRaysAgent(agent);
        SetBool(agent, "maskPrematureEnemyJumps", true);
        SetFloat(agent, "prematureJumpMinThreatDistance", 2.6f);
        SetFloat(agent, "prematureJumpMaxThreatDistance", 3.8f);
        SetBool(agent, "debugPrematureJumpMask", false);
        SetBool(agent, "enableJumpCommitMask", false);
        SetBool(agent, "enableJumpCommitReward", false);
        SetBool(agent, "enableAirCommitAfterJump", true);
        SetFloat(agent, "airCommitDuration", 0.75f);
        SetBool(agent, "airCommitUntilEnemyPassed", true);
        SetBool(agent, "forceJumpActionNearEnemy", false);
        SetBool(agent, "maskForwardActionNearEnemy", false);
        SetBool(agent, "useRuleBasedCommitTest", false);
        SetBool(agent, "disableProgressRewardNearEnemy", false);
        SetBool(agent, "maskUselessJumps", false);
        SetBool(agent, "waitUntilGroundedOnEpisodeStart", true);
        SetFloat(agent, "episodeStartSettleMaxSeconds", 1.0f);
        SetBool(agent, "episodeStartSettleFreezeMovement", true);
        SetFloat(agent, "enemyHitPenalty", -6.0f);
        SetFloat(agent, "enemyPassReward", 8.0f);
        SetFloat(agent, "enemyJumpCueReward", 1.0f);
        SetFloat(agent, "jumpCommitReward", 0.0f);
        SetFloat(agent, "enemyApproachPenalty", 0.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", 0.0f);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "enableRetreatPenalty", true);
        SetFloat(agent, "retreatPenalty", -0.02f);
        SetFloat(agent, "retreatEndDistance", 2.0f);
        SetBool(agent, "enableShortMicroTimeout", true);
        SetFloat(agent, "microTimeoutSeconds", 8.0f);
        SetFloat(agent, "microTimeoutPenalty", -3.0f);
        SetFloat(agent, "jumpCommitMinDistance", 2.6f);
        SetFloat(agent, "jumpCommitMaxDistance", 3.8f);
        SetBool(agent, "debugJumpCommitMask", false);
        SetBool(agent, "debugAirCommit", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureAvoidanceMicroAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        SetFloat(agent, "enemyHitPenalty", -20.0f);
        SetFloat(agent, "enemyPassReward", 3.0f);
        SetFloat(agent, "enemyPassMargin", 1.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", -0.01f);
        SetFloat(agent, "enemyApproachPenalty", -0.05f);
        SetFloat(agent, "enemyJumpCueReward", 0.3f);
        SetFloat(agent, "enemyAvoidanceWindowX", 5.0f);
        SetFloat(agent, "enemyVerticalDangerTolerance", 1.2f);
        SetFloat(agent, "enemyJumpCueMinUpVelocity", 0.2f);
        SetFloat(agent, "enemyDetectionRangeX", 18f);
        SetFloat(agent, "enemyDetectionRangeY", 6f);
        SetBool(agent, "disableProgressRewardNearEnemy", true);
        SetBool(agent, "rewardPassedEnemies", true);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureAvoidanceMicroMaskedAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureAvoidanceMicroAgent(agent);
        SetFloat(agent, "enemyHitPenalty", -20.0f);
        SetFloat(agent, "enemyPassReward", 3.0f);
        SetFloat(agent, "enemyJumpCueReward", 0.4f);
        SetFloat(agent, "enemyApproachPenalty", -0.05f);
        SetFloat(agent, "enemyAvoidanceWindowX", 3.5f);
        SetBool(agent, "disableProgressRewardNearEnemy", true);
        SetBool(agent, "maskForwardActionNearEnemy", true);
        SetFloat(agent, "enemyActionMaskWindowX", 2.5f);
        SetFloat(agent, "enemyActionMaskVerticalTolerance", 1.2f);
        SetBool(agent, "debugEnemyActionMask", false);
        SetBool(agent, "debugTrainingActionStats", false);
        SetInt(agent, "debugTrainingActionStatsInterval", 1000);
    }

    private static void ConfigureAvoidanceMicroEnemyRaysAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureAvoidanceMicroMaskedAgent(agent);
        SetBool(agent, "useEnemyRayObservations", true);
        SetBool(agent, "debugEnemyRayObservations", false);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "waitUntilGroundedOnEpisodeStart", true);
        SetFloat(agent, "episodeStartSettleMaxSeconds", 1.0f);
        SetBool(agent, "episodeStartSettleFreezeMovement", true);
        SetBool(agent, "debugEpisodeStartSettle", false);
    }

    private static void ConfigureAvoidanceMicroEnemyRaysForcedJumpAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureAvoidanceMicroEnemyRaysAgent(agent);
        SetBool(agent, "forceJumpActionNearEnemy", true);
        SetBool(agent, "forceJumpOnlyOncePerEnemy", true);
        SetBool(agent, "debugForcedJumpMask", false);
        SetBool(agent, "debugForcedJumpTiming", false);
        SetBool(agent, "maskForwardActionNearEnemy", false);
        SetBool(agent, "disableProgressRewardNearEnemy", true);
        SetFloat(agent, "enemyHitPenalty", -20.0f);
        SetFloat(agent, "enemyPassReward", 3.0f);
        SetFloat(agent, "enemyJumpCueReward", 0.8f);
        SetFloat(agent, "earlyEnemyJumpPenalty", 0f);
        SetFloat(agent, "enemyApproachPenalty", -0.05f);
        SetFloat(agent, "enemyDangerProximityPenalty", -0.01f);
        SetFloat(agent, "enemyAvoidanceWindowX", 4.5f);
        SetFloat(agent, "enemyForcedJumpWindowX", 4.5f);
        SetFloat(agent, "enemyForcedJumpMinDistance", 2.6f);
        SetFloat(agent, "enemyForcedJumpMaxDistance", 3.8f);
        SetFloat(agent, "enemyForcedJumpVerticalTolerance", 1.2f);
        SetFloat(agent, "enemyJumpCueMinUpVelocity", 0.2f);
        SetFloat(agent, "enemyDetectionRangeX", 18f);
        SetFloat(agent, "enemyDetectionRangeY", 6f);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureAvoidanceMicroEnemyRaysSweetSpotStartAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureAvoidanceMicroEnemyRaysForcedJumpAgent(agent);
        SetBool(agent, "disableProgressRewardNearEnemy", false);
        SetBool(agent, "enableRetreatPenalty", true);
        SetBool(agent, "enableShortMicroTimeout", true);
        SetFloat(agent, "enemyHitPenalty", -8.0f);
        SetFloat(agent, "enemyPassReward", 6.0f);
        SetFloat(agent, "enemyJumpCueReward", 1.0f);
        SetFloat(agent, "enemyApproachPenalty", 0f);
        SetFloat(agent, "enemyDangerProximityPenalty", 0f);
        SetFloat(agent, "enemyAvoidanceWindowX", 4.5f);
        SetFloat(agent, "enemyForcedJumpWindowX", 4.5f);
        SetFloat(agent, "enemyForcedJumpMinDistance", 2.6f);
        SetFloat(agent, "enemyForcedJumpMaxDistance", 3.8f);
        SetFloat(agent, "retreatPenalty", -0.02f);
        SetFloat(agent, "retreatEndDistance", 2.0f);
        SetFloat(agent, "microTimeoutSeconds", 6.0f);
        SetFloat(agent, "microTimeoutPenalty", -2.0f);
    }

    private static void ConfigureAvoidanceMicroEnemyRaysJumpCommitTutorialAgent(EdgeRunnerAgentV5EnemyAware agent)
    {
        ConfigureAvoidanceMicroEnemyRaysSweetSpotStartAgent(agent);
        SetBool(agent, "useEnemyRayObservations", true);
        SetBool(agent, "enableJumpCommitMask", true);
        SetBool(agent, "maskUselessJumps", false);
        SetBool(agent, "debugJumpCommitMask", false);
        SetBool(agent, "debugActionTrace", false);
        SetFloat(agent, "debugActionTraceInterval", 0.25f);
        SetBool(agent, "useRuleBasedCommitTest", false);
        SetBool(agent, "enableAirCommitAfterJump", true);
        SetFloat(agent, "airCommitDuration", 0.75f);
        SetBool(agent, "airCommitUntilEnemyPassed", true);
        SetBool(agent, "debugAirCommit", false);
        SetBool(agent, "jumpCommitOnlyOncePerEnemy", true);
        SetFloat(agent, "jumpCommitMinDistance", 2.6f);
        SetFloat(agent, "jumpCommitMaxDistance", 3.8f);
        SetBool(agent, "enableJumpCommitReward", true);
        SetFloat(agent, "jumpCommitReward", 1.0f);
        SetBool(agent, "forceJumpActionNearEnemy", false);
        SetBool(agent, "maskForwardActionNearEnemy", false);
        SetBool(agent, "disableProgressRewardNearEnemy", false);
        SetFloat(agent, "enemyJumpCueReward", 1.0f);
        SetFloat(agent, "enemyPassReward", 8.0f);
        SetFloat(agent, "enemyHitPenalty", -5.0f);
        SetFloat(agent, "enemyApproachPenalty", 0.0f);
        SetFloat(agent, "enemyDangerProximityPenalty", 0.0f);
        SetBool(agent, "enableRetreatPenalty", true);
        SetFloat(agent, "retreatPenalty", -0.05f);
        SetFloat(agent, "retreatEndDistance", 1.0f);
        SetBool(agent, "enableShortMicroTimeout", true);
        SetFloat(agent, "microTimeoutSeconds", 4.0f);
        SetFloat(agent, "microTimeoutPenalty", -3.0f);
        SetBool(agent, "debugEnemyObservations", false);
        SetBool(agent, "debugEnemyRayObservations", false);
        SetBool(agent, "debugEnemyRewards", false);
    }

    private static void ConfigureStaticIntroForcedJumpDemonstrationRecorder(GameObject player)
    {
        System.Type recorderType = FindDemonstrationRecorderType();

        if (recorderType == null)
        {
            Debug.LogWarning("ML-Agents DemonstrationRecorder type was not found. StaticIntroForcedJump will be built without a demonstration recorder.");
            return;
        }

        EnsureFolder("Assets/EdgeRunner", "Demos");

        Component recorder = player.GetComponent(recorderType);

        if (recorder == null)
        {
            recorder = player.AddComponent(recorderType);
        }

        SetBool(recorder, "Record", false);
        SetInt(recorder, "NumStepsToRecord", 0);
        SetString(recorder, "DemonstrationName", ForcedJumpDemoName);
        SetString(recorder, "DemonstrationDirectory", DemoDirectoryPath);
    }

    private static System.Type FindDemonstrationRecorderType()
    {
        System.Type recorderType = System.Type.GetType(DemonstrationRecorderTypeName);

        if (recorderType != null)
        {
            return recorderType;
        }

        System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            recorderType = assemblies[i].GetType("Unity.MLAgents.Demonstrations.DemonstrationRecorder");

            if (recorderType != null)
            {
                return recorderType;
            }
        }

        return null;
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

    private static void ConfigureSprintVisual(GameObject player, Rigidbody2D rb)
    {
        if (!showSprintVisualInTraining)
        {
            return;
        }

        DemoSprintVisual sprintVisual = player.GetComponent<DemoSprintVisual>();

        if (sprintVisual == null)
        {
            sprintVisual = player.AddComponent<DemoSprintVisual>();
        }

        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        TrailRenderer trail = player.GetComponent<TrailRenderer>();

        if (trail == null)
        {
            trail = player.AddComponent<TrailRenderer>();
        }

        trail.time = 0.2f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.autodestruct = false;
        trail.emitting = false;
        trail.enabled = false;
        trail.startColor = new Color(0.35f, 0.95f, 1f, 0.55f);
        trail.endColor = new Color(0.35f, 0.95f, 1f, 0f);

        Material trailMaterial = GetTrailMaterial();

        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }

        sprintVisual.Configure(rb, spriteRenderer, trail);
    }

    private static Material GetTrailMaterial()
    {
        Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) : null;
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

    private static void CreateNavWarmupLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("NavWarmup_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "NavWarmup_StartPlatform_Wide", -3f, 0f, new Vector2(18f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "NavWarmup_SecondPlatform_Close", 13.4f, 0f, new Vector2(13.6f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "NavWarmup_SmallGap_Landing", 25.8f, 0f, new Vector2(10.8f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "NavWarmup_GoalPad_Wide", 34f, 0f, new Vector2(8f, 0.4f), sprite);
    }

    private static void CreateStaticIntroLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntro_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntro_StartPlatform_Wide", -3f, 0f, new Vector2(18f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "StaticIntro_Approach_Wide", 14f, 0f, new Vector2(14f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "StaticIntro_EnemyPlatform_Wide", 31f, 0f, new Vector2(24f, 0.4f), sprite);
        CreatePlatformWithTop(platformRoot.transform, "StaticIntro_GoalPad_Wide", 48f, 0f, new Vector2(10f, 0.4f), sprite);
    }

    private static void CreateStaticIntroJumpCueLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntroJumpCue_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntroJumpCue_LongFlatPlatform", 16f, 0f, new Vector2(58f, 0.4f), sprite);
    }

    private static void CreateStaticIntroForcedJumpLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntroForcedJump_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntroForcedJump_LongFlatPlatform", 16f, 0f, new Vector2(66f, 0.4f), sprite);
    }

    private static void CreateStaticIntroEnemyRaysLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntroEnemyRays_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntroEnemyRays_LongFlatPlatform", 7.5f, 0f, new Vector2(28f, 0.4f), sprite);
    }

    private static void CreateStaticIntroEnemyRaysApproachGateLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntroEnemyRaysApproachGate_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntroEnemyRaysApproachGate_LongFlatPlatform", 7.5f, 0f, new Vector2(28f, 0.4f), sprite);
    }

    private static void CreateStaticIntroEnemyRaysPrematureAirLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("StaticIntroEnemyRaysPrematureAir_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "StaticIntroEnemyRaysPrematureAir_LongFlatPlatform", 7.5f, 0f, new Vector2(28f, 0.4f), sprite);
    }

    private static void CreateAvoidanceMicroLevel(Transform root, Sprite sprite)
    {
        GameObject platformRoot = new GameObject("AvoidanceMicro_Platforms");
        platformRoot.transform.SetParent(root, false);

        CreatePlatformWithTop(platformRoot.transform, "AvoidanceMicro_LongFlatPlatform", 5f, 0f, new Vector2(22f, 0.4f), sprite);
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
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_Intro01",
            new Vector3(38f, 1.02f, 0f),
            0.8f,
            2.2f
        );
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_Intro02",
            new Vector3(56f, 1.02f, 0f),
            1.0f,
            2.8f
        );
    }

    private static void CreateStaticIntroEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntro_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntro01",
            new Vector3(31f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateStaticIntroJumpCueEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntroJumpCue_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntroJumpCue01",
            new Vector3(18f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateStaticIntroForcedJumpEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntroForcedJump_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntroForcedJump01",
            new Vector3(22f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateStaticIntroEnemyRaysEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntroEnemyRays_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntroEnemyRays01",
            new Vector3(7.5f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateStaticIntroEnemyRaysApproachGateEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntroEnemyRaysApproachGate_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntroEnemyRaysApproachGate01",
            new Vector3(7.5f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateStaticIntroEnemyRaysPrematureAirEnemy(Transform root, Sprite fallbackSprite)
    {
        GameObject enemyRoot = new GameObject("StaticIntroEnemyRaysPrematureAir_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            "Android_EnemyAware_StaticIntroEnemyRaysPrematureAir01",
            new Vector3(7.5f, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateAvoidanceMicroEnemy(
        Transform root,
        Sprite fallbackSprite,
        string enemyName = "Android_EnemyAware_AvoidanceMicro01",
        float enemyX = 5f)
    {
        GameObject enemyRoot = new GameObject("AvoidanceMicro_Enemy");
        enemyRoot.transform.SetParent(root, false);

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);
        CreateEnemyAwareAndroid(
            enemyRoot.transform,
            enemyPrefab,
            fallbackSprite,
            enemyName,
            new Vector3(enemyX, 1.02f, 0f),
            0f,
            0.1f,
            enablePatrol: false
        );
    }

    private static void CreateEnemyAwareAndroid(
        Transform parent,
        GameObject enemyPrefab,
        Sprite fallbackSprite,
        string enemyName,
        Vector3 position,
        float patrolSpeed,
        float patrolDistance,
        bool enablePatrol = true)
    {
        GameObject enemy = enemyPrefab != null
            ? PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject
            : CreateFallbackEnemy(fallbackSprite);

        if (enemy == null)
        {
            return;
        }

        enemy.name = enemyName;
        enemy.transform.SetParent(parent, false);
        enemy.transform.position = position;

        Collider2D collider = enemy.GetComponent<Collider2D>();

        if (collider == null)
        {
            collider = enemy.AddComponent<BoxCollider2D>();
        }

        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = true;
        }

        DemoAndroidPatrol patrol = enemy.GetComponent<DemoAndroidPatrol>();

        if (patrol != null)
        {
            patrol.Configure(
                enablePatrol ? patrolSpeed : 0f,
                enablePatrol ? patrolDistance : 0.1f
            );
            patrol.enabled = enablePatrol;
        }

        DemoEnemyHazard[] demoHazards = enemy.GetComponentsInChildren<DemoEnemyHazard>(true);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard demoHazard = demoHazards[i];
            demoHazard.SetAffectsAgent(false);
            demoHazard.enabled = false;
        }

        DisableDemoStompComponents(enemy);

        Rigidbody2D enemyBody = enemy.GetComponent<Rigidbody2D>();

        if (enemyBody != null)
        {
            enemyBody.bodyType = RigidbodyType2D.Kinematic;
            enemyBody.gravityScale = 0f;
            enemyBody.freezeRotation = true;
        }

        EdgeRunnerEnemyMarker marker = enemy.GetComponent<EdgeRunnerEnemyMarker>();

        if (marker == null)
        {
            marker = enemy.AddComponent<EdgeRunnerEnemyMarker>();
        }

        marker.SetAffectsAgent(true);
        SetBool(marker, "isActiveEnemy", true);
        SetBool(marker, "isAlive", true);
        SetBool(marker, "isDangerous", true);
        SetObjectReference(marker, "enemyCollider", collider);
        SetObjectReference(marker, "visualRoot", enemy.transform);
    }

    private static void DisableDemoStompComponents(GameObject enemy)
    {
        MonoBehaviour[] behaviours = enemy.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "StompableAndroidEnemy" ||
                typeName == "StompableAndroidStompZone" ||
                typeName == "StompableAndroidSideHazard")
            {
                behaviour.enabled = false;
            }
        }
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
        return CreateGoal(new Vector3(72f, 1.1f, 0f));
    }

    private static GameObject CreateGoal(Vector3 position)
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
        goal.transform.position = position;
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
        CreateDeathZone(32f, 110f);
    }

    private static void CreateDeathZone(float centerX, float width)
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
        deathZone.transform.position = new Vector3(centerX, -7f, 0f);
        deathZone.transform.localScale = new Vector3(width, 1f, 1f);

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
}
