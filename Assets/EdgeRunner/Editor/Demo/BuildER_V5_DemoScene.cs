using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildER_V5_DemoScene
{
    private const string ScenePath = "Assets/EdgeRunner/Scenes/Prototypes/ER_V5_DemoScene.unity";
    private const string PlayerPrefabPath = "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string GroundPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/GroundSegment.prefab";
    private const string GoalPrefabPath = "Assets/EdgeRunner/Prefabs/Environment/Goal.prefab";
    private const string DeathZonePrefabPath = "Assets/EdgeRunner/Prefabs/Environment/DeathZone.prefab";
    private const string ModelPath = "Assets/EdgeRunner/ML/Models/Candidates/ER_V5_GoalRelative_V5Gen_EasyBridge01_Final_Test.onnx";
    private const string DemoPrefabFolder = "Assets/EdgeRunner/Prefabs/Demo";
    private const string EnergyCellPrefabPath = DemoPrefabFolder + "/EnergyCell.prefab";
    private const string AndroidEnemyPrefabPath = DemoPrefabFolder + "/DemoAndroidEnemy.prefab";

    [MenuItem("EdgeRunner/Demo/Build ER_V5_DemoScene")]
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
        GameObject energyCellPrefab = CreateOrUpdateEnergyCellPrefab(sharedSprite);
        GameObject androidEnemyPrefab = CreateOrUpdateAndroidEnemyPrefab(sharedSprite);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject systemsRoot = new GameObject("DemoRunSystems");
        GameObject levelRoot = new GameObject("CleanPlatformerDemo");
        GameObject collectiblesRoot = new GameObject("DemoCollectiblesRoot");
        GameObject enemiesRoot = new GameObject("DemoEnemiesRoot");

        EdgeRunnerScoreManager scoreManager = CreateScoreManager(systemsRoot.transform);
        DemoHUD hud = CreateDemoUI(scoreManager);
        GameObject player = CreatePlayer();
        GameObject goal = CreateGoal(hud, scoreManager);

        ConfigurePlayer(player, goal.transform, scoreManager);
        CreateCamera(player.transform);
        CreateLevel(levelRoot.transform, sharedSprite);
        CreateEnergyCells(collectiblesRoot.transform, energyCellPrefab, hud, scoreManager);
        CreateEnemies(enemiesRoot.transform, androidEnemyPrefab, scoreManager);
        CreateDeathZone();

        Selection.activeObject = player;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built EdgeRunner V5 demo scene: {ScenePath}");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/EdgeRunner/Scenes", "Prototypes");
        EnsureFolder("Assets/EdgeRunner", "Prefabs");
        EnsureFolder("Assets/EdgeRunner/Prefabs", "Demo");
        EnsureFolder("Assets/EdgeRunner", "Scripts");
        EnsureFolder("Assets/EdgeRunner/Scripts", "Demo");
        EnsureFolder("Assets/EdgeRunner/Scripts", "Game");
        EnsureFolder("Assets/EdgeRunner", "Editor");
        EnsureFolder("Assets/EdgeRunner/Editor", "Demo");
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

    private static EdgeRunnerScoreManager CreateScoreManager(Transform parent)
    {
        GameObject scoreObject = new GameObject("ScoreManager");
        scoreObject.transform.SetParent(parent, false);

        EdgeRunnerScoreManager scoreManager = scoreObject.AddComponent<EdgeRunnerScoreManager>();
        EdgeRunnerRunResetManager resetManager = scoreObject.AddComponent<EdgeRunnerRunResetManager>();
        SetObjectReference(resetManager, "scoreManager", scoreManager);
        return scoreManager;
    }

    private static DemoHUD CreateDemoUI(EdgeRunnerScoreManager scoreManager)
    {
        GameObject canvasObject = new GameObject("DemoUI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        DemoHUD hud = canvasObject.AddComponent<DemoHUD>();
        EdgeRunnerHUD gameplayHud = canvasObject.AddComponent<EdgeRunnerHUD>();

        Text energyText = CreateUIText(
            "EnergyCellsText",
            canvasObject.transform,
            "Energy Cells: 0/0",
            new Vector2(24f, -24f),
            TextAnchor.UpperLeft,
            32,
            new Color(0.85f, 1f, 1f, 1f)
        );

        Text androidsText = CreateUIText(
            "AndroidsText",
            canvasObject.transform,
            "Androids: 0/0",
            new Vector2(24f, -64f),
            TextAnchor.UpperLeft,
            28,
            new Color(1f, 0.78f, 0.72f, 1f)
        );

        Text scoreText = CreateUIText(
            "ScoreText",
            canvasObject.transform,
            "Score: 0",
            new Vector2(24f, -104f),
            TextAnchor.UpperLeft,
            28,
            new Color(1f, 0.92f, 0.35f, 1f)
        );

        Text timeText = CreateUIText(
            "TimeText",
            canvasObject.transform,
            "Time: 0.00s",
            new Vector2(24f, -144f),
            TextAnchor.UpperLeft,
            28,
            new Color(0.85f, 0.95f, 1f, 1f)
        );

        Text statusText = CreateUIText(
            "StatusText",
            canvasObject.transform,
            "Level Complete",
            new Vector2(0f, -96f),
            TextAnchor.UpperCenter,
            46,
            new Color(1f, 0.9f, 0.25f, 1f)
        );
        statusText.enabled = false;

        SetObjectReference(hud, "energyCellsText", energyText);
        SetObjectReference(hud, "statusText", statusText);
        SetObjectReference(hud, "gameplayHud", gameplayHud);

        SetObjectReference(gameplayHud, "scoreManager", scoreManager);
        SetObjectReference(gameplayHud, "energyCellsText", energyText);
        SetObjectReference(gameplayHud, "androidsText", androidsText);
        SetObjectReference(gameplayHud, "scoreText", scoreText);
        SetObjectReference(gameplayHud, "timeText", timeText);
        SetObjectReference(gameplayHud, "statusText", statusText);
        gameplayHud.SetScoreManager(scoreManager);
        hud.SetGameplayHud(gameplayHud);

        return hud;
    }

    private static Text CreateUIText(
        string name,
        Transform parent,
        string text,
        Vector2 anchoredPosition,
        TextAnchor alignment,
        int fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = alignment == TextAnchor.UpperLeft
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = alignment == TextAnchor.UpperLeft
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = alignment == TextAnchor.UpperLeft
            ? new Vector2(620f, 64f)
            : new Vector2(720f, 76f);

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = alignment;
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.raycastTarget = false;
        uiText.font = GetBuiltinFont();

        return uiText;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static GameObject CreatePlayer()
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
        player.transform.position = new Vector3(-8f, 1.15f, 0f);
        return player;
    }

    private static void ConfigurePlayer(GameObject player, Transform goal, EdgeRunnerScoreManager scoreManager)
    {
        EdgeRunnerAgentV5 agent = player.GetComponent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            throw new System.InvalidOperationException("Player_V5 prefab does not contain EdgeRunnerAgentV5.");
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
            AssetDatabase.ImportAsset(ModelPath);
            ModelAsset model = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);

            if (model == null)
            {
                Debug.LogWarning($"Could not load model asset at {ModelPath}. Player remains configured for EdgeRunnerV5 but has no model assigned.");
            }
            else
            {
                behavior.Model = model;
            }

            behavior.BehaviorName = "EdgeRunnerV5";
            behavior.BehaviorType = BehaviorType.InferenceOnly;
        }

        ConfigureSprintVisual(player, rb);
        ConfigurePlayerDamageHandler(player, scoreManager);
    }

    private static void ConfigurePlayerDamageHandler(GameObject player, EdgeRunnerScoreManager scoreManager)
    {
        DemoPlayerDamageHandler damageHandler = player.GetComponent<DemoPlayerDamageHandler>();

        if (damageHandler == null)
        {
            damageHandler = player.AddComponent<DemoPlayerDamageHandler>();
        }

        EdgeRunnerRunResetManager resetManager = scoreManager != null
            ? scoreManager.GetComponent<EdgeRunnerRunResetManager>()
            : null;

        damageHandler.Configure(resetManager, scoreManager, true);
    }

    private static void ConfigureSprintVisual(GameObject player, Rigidbody2D rb)
    {
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
        cameraObject.transform.position = new Vector3(-4f, 3.2f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.2f;
        camera.backgroundColor = new Color(0.055f, 0.08f, 0.13f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.AddComponent<AudioListener>();
        DemoCameraFollow2D follow = cameraObject.AddComponent<DemoCameraFollow2D>();
        follow.SetTarget(target);
    }

    private static void CreateLevel(Transform root, Sprite sprite)
    {
        GameObject backgroundRoot = new GameObject("Background");
        backgroundRoot.transform.SetParent(root, false);

        CreateBackdropBand(backgroundRoot.transform, "FarSkyBand", new Vector2(68f, 4.8f), new Vector2(190f, 18f), new Color(0.05f, 0.10f, 0.17f, 1f));
        CreateBackdropBand(backgroundRoot.transform, "NeonHorizon", new Vector2(68f, -1.1f), new Vector2(190f, 0.18f), new Color(0.1f, 0.95f, 1f, 0.45f));

        GameObject platformRoot = new GameObject("Platforms");
        platformRoot.transform.SetParent(root, false);

        Transform section01 = CreateSection(platformRoot.transform, "Section_01_Start");
        Transform section02 = CreateSection(platformRoot.transform, "Section_02_GapSequence");
        Transform section03 = CreateSection(platformRoot.transform, "Section_03_LongRunWithCells");
        Transform section04 = CreateSection(platformRoot.transform, "Section_04_AndroidVisualSideArea");
        Transform section05 = CreateSection(platformRoot.transform, "Section_05_FinalGaps");
        Transform section06 = CreateSection(platformRoot.transform, "Section_06_Goal");

        CreatePlatformWithTop(section01, "StartPlatform_22u", -3f, 0f, new Vector2(22f, 0.4f), new Color(0.16f, 0.22f, 0.26f, 1f), sprite);
        CreatePlatformWithTop(section02, "Landing_1_14u", 19.5f, 0f, new Vector2(14f, 0.4f), new Color(0.18f, 0.25f, 0.30f, 1f), sprite);
        CreatePlatformWithTop(section02, "Landing_2_16u", 39.5f, 0f, new Vector2(16f, 0.4f), new Color(0.18f, 0.26f, 0.31f, 1f), sprite);
        CreatePlatformWithTop(section03, "LongRun_1_22u", 62.5f, 0f, new Vector2(22f, 0.4f), new Color(0.18f, 0.27f, 0.32f, 1f), sprite);
        CreatePlatformWithTop(section04, "Landing_3_16u", 86.5f, 0f, new Vector2(16f, 0.4f), new Color(0.18f, 0.28f, 0.33f, 1f), sprite);
        CreatePlatformWithTop(section04, "AndroidStompPlatform_A_12u", 84f, 2.5f, new Vector2(12f, 0.4f), new Color(0.15f, 0.25f, 0.31f, 1f), sprite);
        CreatePlatformWithTop(section05, "LongRun_2_22u", 109.5f, 0f, new Vector2(22f, 0.4f), new Color(0.19f, 0.29f, 0.34f, 1f), sprite);
        CreatePlatformWithTop(section05, "AndroidStompPlatform_B_14u", 116f, 2.5f, new Vector2(14f, 0.4f), new Color(0.15f, 0.25f, 0.31f, 1f), sprite);
        CreatePlatformWithTop(section06, "GoalPlatform_22u", 136.5f, 0f, new Vector2(22f, 0.45f), new Color(0.22f, 0.29f, 0.36f, 1f), sprite);

        CreatePlatformTrim(platformRoot.transform, sprite);
    }

    private static void CreatePlatformTrim(Transform root, Sprite sprite)
    {
        CreateVisualStrip(root, "StartPlatform_EdgeLight", new Vector2(-3f, 0.06f), new Vector2(21.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "Landing_1_EdgeLight", new Vector2(19.5f, 0.06f), new Vector2(13.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "Landing_2_EdgeLight", new Vector2(39.5f, 0.06f), new Vector2(15.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "LongRun_1_EdgeLight", new Vector2(62.5f, 0.06f), new Vector2(21.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "Landing_3_EdgeLight", new Vector2(86.5f, 0.06f), new Vector2(15.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "AndroidStompPlatform_A_EdgeLight", new Vector2(84f, 2.56f), new Vector2(11.6f, 0.08f), new Color(1f, 0.22f, 0.16f, 1f), sprite);
        CreateVisualStrip(root, "LongRun_2_EdgeLight", new Vector2(109.5f, 0.06f), new Vector2(21.6f, 0.08f), new Color(0.1f, 0.85f, 1f, 1f), sprite);
        CreateVisualStrip(root, "AndroidStompPlatform_B_EdgeLight", new Vector2(116f, 2.56f), new Vector2(13.6f, 0.08f), new Color(1f, 0.22f, 0.16f, 1f), sprite);
        CreateVisualStrip(root, "GoalPlatform_EdgeLight", new Vector2(136.5f, 0.06f), new Vector2(21.6f, 0.08f), new Color(0.25f, 1f, 0.55f, 1f), sprite);
    }

    private static Transform CreateSection(Transform parent, string name)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        return section.transform;
    }

    private static GameObject CreatePlatform(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        Sprite sprite)
    {
        GameObject platform = new GameObject(name);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(position.x, position.y, 0f);
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;

        return platform;
    }

    private static GameObject CreatePlatformWithTop(
        Transform parent,
        string name,
        float centerX,
        float topY,
        Vector2 size,
        Color color,
        Sprite sprite)
    {
        float centerY = topY - size.y * 0.5f;
        return CreatePlatform(parent, name, new Vector2(centerX, centerY), size, color, sprite);
    }

    private static void CreateVisualStrip(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        Sprite sprite)
    {
        GameObject strip = new GameObject(name);
        strip.transform.SetParent(parent, false);
        strip.transform.position = new Vector3(position.x, position.y, -0.02f);
        strip.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 2;
    }

    private static void CreateBackdropBand(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject band = new GameObject(name);
        band.transform.SetParent(parent, false);
        band.transform.position = new Vector3(position.x, position.y, 2f);
        band.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = band.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSharedSprite();
        renderer.color = color;
        renderer.sortingOrder = -20;
    }

    private static void CreateEnergyCells(Transform root, GameObject prefab, DemoHUD hud, EdgeRunnerScoreManager scoreManager)
    {
        DemoEnergyCellCounter counter = root.gameObject.AddComponent<DemoEnergyCellCounter>();
        counter.SetHud(hud);
        counter.SetScoreManager(scoreManager);

        Vector2[] positions =
        {
            new Vector2(-11f, 0.8f),
            new Vector2(-6f, 0.8f),
            new Vector2(-1f, 0.8f),
            new Vector2(5f, 0.8f),
            new Vector2(9.2f, 1.15f),
            new Vector2(10.5f, 1.45f),
            new Vector2(11.8f, 1.15f),
            new Vector2(17f, 0.8f),
            new Vector2(23f, 0.8f),
            new Vector2(27.8f, 1.15f),
            new Vector2(29f, 1.45f),
            new Vector2(30.2f, 1.15f),
            new Vector2(34f, 0.8f),
            new Vector2(40f, 0.8f),
            new Vector2(54f, 0.8f),
            new Vector2(61f, 0.8f),
            new Vector2(68f, 0.8f),
            new Vector2(74.6f, 1.15f),
            new Vector2(76f, 1.45f),
            new Vector2(77.4f, 1.15f),
            new Vector2(83f, 0.8f),
            new Vector2(90f, 0.8f),
            new Vector2(103f, 0.8f),
            new Vector2(111f, 0.8f),
            new Vector2(118f, 0.8f),
            new Vector2(121.6f, 1.15f),
            new Vector2(123f, 1.45f),
            new Vector2(124.4f, 1.15f),
            new Vector2(132f, 0.8f),
            new Vector2(140f, 0.8f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject cell = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (cell == null)
            {
                continue;
            }

            cell.name = $"EnergyCell_{i + 1:00}";
            cell.transform.SetParent(root, false);
            cell.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

            DemoEnergyCell energyCell = cell.GetComponent<DemoEnergyCell>();

            if (energyCell != null)
            {
                energyCell.SetCounter(counter);
                energyCell.SetScoreManager(scoreManager);
            }
        }
    }

    private static void CreateEnemies(Transform root, GameObject prefab, EdgeRunnerScoreManager scoreManager)
    {
        CreateEnemy(root, prefab, "ScoreAttack_StompableAndroid_A", new Vector2(84f, 3.12f), 1.2f, 4.5f, true, scoreManager);
        CreateEnemy(root, prefab, "ScoreAttack_StompableAndroid_B", new Vector2(116f, 3.12f), 1.4f, 5.0f, true, scoreManager);
    }

    private static void CreateEnemy(
        Transform root,
        GameObject prefab,
        string name,
        Vector2 position,
        float speed,
        float patrolDistance,
        bool affectsAgent,
        EdgeRunnerScoreManager scoreManager)
    {
        GameObject enemy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (enemy == null)
        {
            return;
        }

        enemy.name = name;
        enemy.transform.SetParent(root, false);
        enemy.transform.position = new Vector3(position.x, position.y, 0f);

        ConfigureAndroidBodyCollider(enemy);

        DemoAndroidPatrol patrol = enemy.GetComponent<DemoAndroidPatrol>();

        if (patrol != null)
        {
            patrol.Configure(speed, patrolDistance);
        }

        DemoEnemyHazard hazard = enemy.GetComponent<DemoEnemyHazard>();

        if (hazard != null)
        {
            hazard.SetAffectsAgent(false);
            hazard.enabled = false;
        }

        StompableAndroidEnemy stompable = enemy.GetComponent<StompableAndroidEnemy>();

        if (stompable == null)
        {
            stompable = enemy.AddComponent<StompableAndroidEnemy>();
        }

        stompable.SetScoreManager(scoreManager);
        stompable.SetAffectsAgent(affectsAgent);
        SetBool(stompable, "harmfulOnSideContact", true);
        SetInt(stompable, "killPoints", 50);
        SetFloat(stompable, "bounceForce", 9f);
        SetFloat(stompable, "stompRequiredVerticalVelocity", -0.05f);
        SetFloat(stompable, "stompHeightOffset", 0.15f);
        SetFloat(stompable, "stompTopTolerance", 0.35f);
        SetFloat(stompable, "maxUpwardVelocityForStomp", 1f);
        SetFloat(stompable, "stompDamageGraceTime", 0.2f);
        SetBool(stompable, "debugStomp", false);
        EnsureAndroidStompZone(enemy.transform, stompable);

        EdgeRunnerEnemyMarker marker = enemy.GetComponent<EdgeRunnerEnemyMarker>();

        if (marker != null)
        {
            marker.SetAffectsAgent(false);
        }
    }

    private static void ConfigureAndroidBodyCollider(GameObject enemy)
    {
        BoxCollider2D bodyCollider = enemy.GetComponent<BoxCollider2D>();

        if (bodyCollider == null)
        {
            bodyCollider = enemy.AddComponent<BoxCollider2D>();
        }

        bodyCollider.isTrigger = true;
        bodyCollider.size = new Vector2(0.9f, 0.55f);
        bodyCollider.offset = new Vector2(0f, -0.22f);
    }

    private static void EnsureAndroidStompZone(Transform enemyRoot, StompableAndroidEnemy stompable)
    {
        Transform existingZone = enemyRoot.Find("StompZone");
        GameObject zone = existingZone != null
            ? existingZone.gameObject
            : new GameObject("StompZone");

        zone.layer = enemyRoot.gameObject.layer;
        zone.transform.SetParent(enemyRoot, false);
        zone.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        zone.transform.localRotation = Quaternion.identity;
        zone.transform.localScale = Vector3.one;

        BoxCollider2D zoneCollider = zone.GetComponent<BoxCollider2D>();

        if (zoneCollider == null)
        {
            zoneCollider = zone.AddComponent<BoxCollider2D>();
        }

        zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector2(1.35f, 0.45f);
        zoneCollider.offset = Vector2.zero;

        StompableAndroidStompZone stompZone = zone.GetComponent<StompableAndroidStompZone>();

        if (stompZone == null)
        {
            stompZone = zone.AddComponent<StompableAndroidStompZone>();
        }

        stompZone.Configure(stompable);
    }

    private static GameObject CreateGoal(DemoHUD hud, EdgeRunnerScoreManager scoreManager)
    {
        GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        GameObject goal;

        if (goalPrefab != null)
        {
            goal = PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject;
        }
        else
        {
            goal = new GameObject("Goal");
            goal.AddComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer renderer = goal.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSharedSprite();
            renderer.color = new Color(0.2f, 1f, 0.45f, 1f);
        }

        if (goal == null)
        {
            throw new System.InvalidOperationException("Failed to create Goal.");
        }

        goal.name = "Goal";
        goal.transform.position = new Vector3(142f, 1.1f, 0f);
        goal.transform.localScale = new Vector3(1.2f, 2.8f, 1f);

        DemoGoalTrigger demoGoal = goal.GetComponent<DemoGoalTrigger>();

        if (demoGoal == null)
        {
            demoGoal = goal.AddComponent<DemoGoalTrigger>();
        }

        demoGoal.SetHud(hud);
        demoGoal.SetScoreManager(scoreManager);
        CreateGoalBeacon(goal.transform);

        return goal;
    }

    private static void CreateGoalBeacon(Transform goal)
    {
        Sprite sprite = GetSharedSprite();

        GameObject halo = new GameObject("GoalHalo");
        halo.transform.SetParent(goal, false);
        halo.transform.localPosition = new Vector3(0f, 0.65f, -0.03f);
        halo.transform.localScale = new Vector3(1.6f, 0.18f, 1f);

        SpriteRenderer haloRenderer = halo.AddComponent<SpriteRenderer>();
        haloRenderer.sprite = sprite;
        haloRenderer.color = new Color(0.2f, 1f, 0.7f, 0.85f);
        haloRenderer.sortingOrder = 4;
    }

    private static void CreateDeathZone()
    {
        GameObject deathZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefabPath);
        GameObject deathZone;

        if (deathZonePrefab != null)
        {
            deathZone = PrefabUtility.InstantiatePrefab(deathZonePrefab) as GameObject;
        }
        else
        {
            deathZone = new GameObject("DeathZone");
            deathZone.AddComponent<BoxCollider2D>().isTrigger = true;
            deathZone.AddComponent<DeathZone>();
        }

        if (deathZone == null)
        {
            return;
        }

        deathZone.name = "DeathZone";
        deathZone.transform.position = new Vector3(68f, -7f, 0f);
        deathZone.transform.localScale = new Vector3(190f, 1f, 1f);

        if (deathZone.GetComponent<TrainingDeathZone>() == null)
        {
            deathZone.AddComponent<TrainingDeathZone>();
        }
    }

    private static GameObject CreateOrUpdateEnergyCellPrefab(Sprite sprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnergyCellPrefabPath);

        if (existing != null)
        {
            return existing;
        }

        GameObject cell = new GameObject("EnergyCell");
        cell.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

        SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.1f, 0.95f, 1f, 1f);
        renderer.sortingOrder = 8;

        CircleCollider2D collider = cell.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.6f;

        cell.AddComponent<DemoEnergyCell>();

        GameObject core = new GameObject("CoreGlow");
        core.transform.SetParent(cell.transform, false);
        core.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        SpriteRenderer coreRenderer = core.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = sprite;
        coreRenderer.color = new Color(1f, 0.9f, 0.18f, 1f);
        coreRenderer.sortingOrder = 9;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cell, EnergyCellPrefabPath);
        Object.DestroyImmediate(cell);
        return prefab;
    }

    private static GameObject CreateOrUpdateAndroidEnemyPrefab(Sprite sprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AndroidEnemyPrefabPath);

        if (existing != null)
        {
            return existing;
        }

        GameObject enemy = new GameObject("DemoAndroidEnemy");

        SpriteRenderer bodyRenderer = enemy.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = sprite;
        bodyRenderer.color = new Color(0.55f, 0.62f, 0.68f, 1f);
        bodyRenderer.sortingOrder = 6;

        enemy.transform.localScale = new Vector3(0.9f, 1.25f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        enemy.AddComponent<BoxCollider2D>();
        ConfigureAndroidBodyCollider(enemy);

        enemy.AddComponent<DemoAndroidPatrol>();
        enemy.AddComponent<DemoEnemyHazard>();
        enemy.AddComponent<EdgeRunnerEnemyMarker>();
        StompableAndroidEnemy stompable = enemy.AddComponent<StompableAndroidEnemy>();
        EnsureAndroidStompZone(enemy.transform, stompable);

        CreateEnemyEye(enemy.transform, "LeftEye", new Vector3(-0.18f, 0.18f, -0.03f), sprite);
        CreateEnemyEye(enemy.transform, "RightEye", new Vector3(0.18f, 0.18f, -0.03f), sprite);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, AndroidEnemyPrefabPath);
        Object.DestroyImmediate(enemy);
        return prefab;
    }

    private static void CreateEnemyEye(Transform parent, string name, Vector3 localPosition, Sprite sprite)
    {
        GameObject eye = new GameObject(name);
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = new Vector3(0.16f, 0.12f, 1f);

        SpriteRenderer renderer = eye.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.12f, 0.08f, 1f);
        renderer.sortingOrder = 7;
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
            Debug.LogWarning($"Could not find serialized int property '{propertyName}' on {target.name}.");
            return;
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
            Debug.LogWarning($"Could not find serialized float property '{propertyName}' on {target.name}.");
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
