using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalDemoController : MonoBehaviour
{
    public const string MenuScene = "ER_FinalDemo_Menu";

    public static readonly string[] LevelScenes =
    {
        "ER_FinalDemo_SpeedRun_Easy",
        "ER_FinalDemo_SpeedRun_Normal",
        "ER_FinalDemo_SpeedRun_Hard",
        "ER_FinalDemo_MaxScore_Easy",
        "ER_FinalDemo_MaxScore_Normal",
        "ER_FinalDemo_MaxScore_Hard"
    };

    private static readonly string[] LevelTitles =
    {
        "SpeedRun Easy",
        "SpeedRun Normal",
        "SpeedRun Hard",
        "MaxScore Easy",
        "MaxScore Normal",
        "MaxScore Hard"
    };

    [SerializeField] private int levelIndex = -1;
    [SerializeField] private string modelLabel = "";
    [SerializeField] private string levelDescription = "";

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle statusStyle;
    private GUIStyle menuPanelStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle menuHeaderStyle;
    private GUIStyle menuSubtitleStyle;
    private GUIStyle hudPanelStyle;
    private GUIStyle hudTitleStyle;
    private GUIStyle hudModeStyle;
    private GUIStyle hudStatLabelStyle;
    private GUIStyle hudStatValueStyle;
    private GUIStyle hudControlsStyle;
    private Texture2D menuLogo;
    private Texture2D borderTexture;
    private int completedRuns;
    private float lastCompletionTime = -100f;
    private bool telemetryEnabled;
    private float nextTelemetryTime;
    private EdgeRunnerAgentV5 trackedAgent;
    private int visualCollectiblesCollected;
    [SerializeField]
    private int visualCollectibleTotal;
    private ScoreAttackManager cachedScoreAttackManager;
    private float levelStartUnscaledTime;
    private float nextElapsedTextUpdate;
    private string elapsedTimeText = "00:00";
    private bool showFps;
    private int fpsFrameCount;
    private float fpsSampleStart;
    private string fpsText = "FPS --";

    public void Configure(int index, string model, string description)
    {
        levelIndex = index;
        modelLabel = model;
        levelDescription = description;
    }

    public void ConfigureVisualCollectibles(int total)
    {
        visualCollectibleTotal = Mathf.Max(0, total);
        visualCollectiblesCollected = 0;
    }

    public void NotifyVisualCollectibleCollected()
    {
        visualCollectiblesCollected = Mathf.Clamp(
            visualCollectiblesCollected + 1,
            0,
            visualCollectibleTotal);
    }

    public void NotifyGoalReached()
    {
        completedRuns++;
        lastCompletionTime = Time.unscaledTime;
        Debug.Log($"[FINAL DEMO SUCCESS] Level {levelIndex + 1} completed. Run={completedRuns}.");
    }

    private void Start()
    {
        if (levelIndex >= 0)
        {
            telemetryEnabled = HasCommandLineFlag("-telemetry");
            levelStartUnscaledTime = Time.unscaledTime;
            fpsSampleStart = Time.unscaledTime;
            cachedScoreAttackManager = FindAnyObjectByType<ScoreAttackManager>();
            UpdateHudMetrics(true);
            Debug.Log(
                $"[FINAL DEMO RUNTIME] Loaded level {levelIndex + 1}: " +
                $"{LevelTitles[levelIndex]} | {modelLabel}");
            return;
        }

        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(arguments[i], "-level", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(arguments[i + 1], out int requestedLevel) &&
                requestedLevel >= 1 && requestedLevel <= LevelScenes.Length)
            {
                LoadLevel(requestedLevel - 1);
                return;
            }
        }
    }

    private void Update()
    {
        WriteTelemetryIfEnabled();
        UpdateHudMetrics(false);

        if (levelIndex >= 0 && Input.GetKeyDown(KeyCode.F))
        {
            showFps = !showFps;
            fpsFrameCount = 0;
            fpsSampleStart = Time.unscaledTime;
            fpsText = "FPS --";
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
        {
            SceneManager.LoadScene(MenuScene);
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (Input.GetKeyDown(KeyCode.N) && levelIndex >= 0)
        {
            LoadLevel((levelIndex + 1) % LevelScenes.Length);
        }

        for (int i = 0; i < LevelScenes.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                LoadLevel(i);
                return;
            }
        }
    }

    private void UpdateHudMetrics(bool force)
    {
        if (levelIndex < 0)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (force || now >= nextElapsedTextUpdate)
        {
            int elapsedSeconds = Mathf.Max(0, Mathf.FloorToInt(now - levelStartUnscaledTime));
            elapsedTimeText = $"{elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}";
            nextElapsedTextUpdate = now + 0.25f;
        }

        if (!showFps)
        {
            return;
        }

        fpsFrameCount++;
        float sampleDuration = now - fpsSampleStart;
        if (sampleDuration >= 0.5f)
        {
            fpsText = $"FPS {Mathf.RoundToInt(fpsFrameCount / sampleDuration)}";
            fpsFrameCount = 0;
            fpsSampleStart = now;
        }
    }

    private void WriteTelemetryIfEnabled()
    {
        if (!telemetryEnabled || levelIndex < 0 || Time.unscaledTime < nextTelemetryTime)
        {
            return;
        }

        nextTelemetryTime = Time.unscaledTime + 5f;
        if (trackedAgent == null)
        {
            trackedAgent = FindAnyObjectByType<EdgeRunnerAgentV5>();
        }
        if (trackedAgent == null)
        {
            Debug.LogWarning($"[FINAL DEMO TELEMETRY] Level {levelIndex + 1}: agent missing.");
            return;
        }

        Vector2 velocity = trackedAgent.GetCurrentVelocityForEvaluation();
        ScoreAttackManager manager = FindAnyObjectByType<ScoreAttackManager>();
        DemoSprintVisual sprintVisual = trackedAgent.GetComponent<DemoSprintVisual>();
        string objectives = manager == null
            ? string.Empty
            : $" coins={manager.CoinsCollected}/7 androids={manager.EnemiesKilled}/2";
        string sprint = sprintVisual == null
            ? string.Empty
            : $" sprintVisual={sprintVisual.IsSprintVisualActive}";
        string visualCells = visualCollectibleTotal <= 0
            ? string.Empty
            : $" visualCells={visualCollectiblesCollected}/{visualCollectibleTotal}";
        Debug.Log(
            $"[FINAL DEMO TELEMETRY] Level {levelIndex + 1}: " +
            $"x={trackedAgent.transform.position.x:F2} y={trackedAgent.transform.position.y:F2} " +
            $"vx={velocity.x:F2} vy={velocity.y:F2}{objectives}{sprint}{visualCells}");
    }

    private static bool HasCommandLineFlag(string flag)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (string.Equals(arguments[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (levelIndex < 0)
        {
            DrawMenu();
        }
        else
        {
            DrawLevelOverlay();
        }
    }

    private void DrawMenu()
    {
        float width = Mathf.Min(1040f, Screen.width - 36f);
        float height = Mathf.Min(650f, Screen.height - 28f);
        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);
        GUI.Box(panel, GUIContent.none, menuPanelStyle);
        DrawBorder(panel, 2f);

        float logoWidth = Mathf.Clamp(panel.width * 0.27f, 190f, 270f);
        Rect logoRect = new Rect(panel.x + 28f, panel.y + 32f, logoWidth, panel.height - 64f);
        if (menuLogo != null)
        {
            GUI.DrawTexture(logoRect, menuLogo, ScaleMode.ScaleToFit, true);
        }

        float contentX = logoRect.xMax + 30f;
        float contentWidth = panel.xMax - contentX - 30f;
        GUI.Label(new Rect(contentX, panel.y + 38f, contentWidth, 42f), "EDGERUNNERS", titleStyle);
        GUI.Label(
            new Rect(contentX, panel.y + 82f, contentWidth, 56f),
            "FINAL AI DEMO  ·  6 HANDCRAFTED LEVELS\nEscolha um desafio. O agente joga autonomamente.",
            menuSubtitleStyle);

        float columnGap = 18f;
        float columnWidth = (contentWidth - columnGap) * 0.5f;
        float leftX = contentX;
        float rightX = contentX + columnWidth + columnGap;
        float headerY = panel.y + 160f;
        GUI.Label(new Rect(leftX, headerY, columnWidth, 34f), "SPEEDRUN  ·  55 OBS", menuHeaderStyle);
        GUI.Label(new Rect(rightX, headerY, columnWidth, 34f), "MAXSCORE  ·  111 OBS", menuHeaderStyle);

        string[] difficulty = { "EASY", "NORMAL", "HARD" };
        for (int row = 0; row < 3; row++)
        {
            float y = headerY + 42f + row * 76f;
            if (GUI.Button(new Rect(leftX, y, columnWidth, 60f),
                $"{row + 1}   {difficulty[row]}", menuButtonStyle))
            {
                LoadLevel(row);
            }
            if (GUI.Button(new Rect(rightX, y, columnWidth, 60f),
                $"{row + 4}   {difficulty[row]}", menuButtonStyle))
            {
                LoadLevel(row + 3);
            }
        }

        GUI.Label(
            new Rect(contentX, panel.yMax - 74f, contentWidth, 48f),
            "1–6  nível     R  reiniciar     N  seguinte     M / Esc  menu",
            menuSubtitleStyle);
    }

    private void DrawLevelOverlay()
    {
        const float margin = 16f;
        float identityWidth = Mathf.Min(420f, Screen.width - margin * 2f);
        Rect identityPanel = new Rect(margin, margin, identityWidth, 74f);
        GUI.Box(identityPanel, GUIContent.none, hudPanelStyle);
        DrawBorder(identityPanel, 1f);
        if (menuLogo != null)
        {
            GUI.DrawTexture(
                new Rect(identityPanel.x + 8f, identityPanel.y + 7f, 46f, identityPanel.height - 14f),
                menuLogo,
                ScaleMode.ScaleToFit,
                true);
        }

        float textX = identityPanel.x + 64f;
        GUI.Label(
            new Rect(textX, identityPanel.y + 8f, identityPanel.width - 72f, 32f),
            LevelTitles[levelIndex],
            hudTitleStyle);
        string mode = levelIndex < 3 ? "SPEEDRUN" : "MAXSCORE";
        GUI.Label(
            new Rect(textX, identityPanel.y + 41f, identityPanel.width - 72f, 24f),
            $"{mode}  ·  Tempo {elapsedTimeText}",
            hudModeStyle);

        const float statsWidth = 270f;
        float statsX = Screen.width - margin - statsWidth;
        float statsY = margin;
        bool stackPanels = statsX < identityPanel.xMax + 12f;
        if (stackPanels)
        {
            statsX = margin;
            statsY = identityPanel.yMax + 8f;
        }
        Rect statsPanel = new Rect(statsX, statsY, statsWidth, 74f);
        GUI.Box(statsPanel, GUIContent.none, hudPanelStyle);
        DrawBorder(statsPanel, 1f);
        DrawObjectiveStats(statsPanel);

        float controlsY = stackPanels ? statsPanel.yMax + 8f : identityPanel.yMax + 8f;
        Rect controlsPanel = new Rect(margin, controlsY, Mathf.Min(420f, Screen.width - margin * 2f), 30f);
        GUI.Box(controlsPanel, GUIContent.none, hudPanelStyle);
        GUI.Label(
            new Rect(controlsPanel.x + 10f, controlsPanel.y + 2f, controlsPanel.width - 20f, 26f),
            "R  Reiniciar  |  N  Próximo  |  M/Esc  Menu  |  F  FPS",
            hudControlsStyle);

        if (showFps)
        {
            Rect fpsPanel = new Rect(statsPanel.xMax - 88f, statsPanel.yMax + 8f, 88f, 28f);
            GUI.Box(fpsPanel, GUIContent.none, hudPanelStyle);
            GUI.Label(fpsPanel, fpsText, hudControlsStyle);
        }

        if (completedRuns > 0 && Time.unscaledTime - lastCompletionTime < 4f)
        {
            Rect status = new Rect((Screen.width - 380f) * 0.5f, 18f, 380f, 44f);
            GUI.Box(status, $"OBJETIVO CONCLUÍDO  ·  Volta {completedRuns}", statusStyle);
        }
    }

    private void DrawObjectiveStats(Rect panel)
    {
        float x = panel.x + 14f;
        float width = panel.width - 28f;
        if (levelIndex < 3)
        {
            GUI.Label(new Rect(x, panel.y + 9f, width, 22f), "POWERCELLS", hudStatLabelStyle);
            string value = visualCollectibleTotal > 0
                ? $"{visualCollectiblesCollected}/{visualCollectibleTotal}"
                : "—";
            GUI.Label(new Rect(x, panel.y + 30f, width, 34f), value, hudStatValueStyle);
            return;
        }

        if (cachedScoreAttackManager == null)
        {
            GUI.Label(new Rect(x, panel.y + 24f, width, 26f), "Objetivos a carregar…", hudModeStyle);
            return;
        }

        int coinTotal = cachedScoreAttackManager.CoinsCollected + cachedScoreAttackManager.CoinsRemaining;
        GUI.Label(
            new Rect(x, panel.y + 8f, width, 25f),
            $"MOEDAS  {cachedScoreAttackManager.CoinsCollected}/{coinTotal}",
            hudStatLabelStyle);
        GUI.Label(
            new Rect(x, panel.y + 38f, width, 25f),
            $"ANDROIDS RESTANTES  {cachedScoreAttackManager.EnemiesRemaining}",
            hudStatLabelStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.35f, 0.95f, 1f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            normal = { textColor = new Color(0.88f, 0.94f, 1f) }
        };
        statusStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.35f, 1f, 0.58f) }
        };
        menuLogo = Resources.Load<Texture2D>("DemoFinal/EdgeRunners_Logo");
        Texture2D panelTexture = MakeTexture(new Color(0.025f, 0.055f, 0.095f, 0.96f));
        Texture2D buttonTexture = MakeTexture(new Color(0.06f, 0.16f, 0.23f, 0.98f));
        Texture2D buttonHoverTexture = MakeTexture(new Color(0.08f, 0.34f, 0.43f, 1f));
        Texture2D buttonActiveTexture = MakeTexture(new Color(0.18f, 0.55f, 0.62f, 1f));
        borderTexture = MakeTexture(new Color(0.12f, 0.9f, 1f, 0.9f));
        menuPanelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture }
        };
        hudPanelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.02f, 0.05f, 0.085f, 0.88f)) }
        };
        hudTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.35f, 0.95f, 1f) }
        };
        hudModeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.7f, 0.82f, 0.9f) }
        };
        hudStatLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.78f, 0.94f, 1f) }
        };
        hudStatValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.82f, 0.2f) }
        };
        hudControlsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.66f, 0.8f, 0.9f) }
        };
        menuButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(22, 12, 0, 0),
            normal = { background = buttonTexture, textColor = new Color(0.86f, 0.97f, 1f) },
            hover = { background = buttonHoverTexture, textColor = Color.white },
            active = { background = buttonActiveTexture, textColor = Color.white }
        };
        menuHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.38f, 0.9f, 1f) }
        };
        menuSubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.68f, 0.78f, 0.86f) }
        };
    }

    private void DrawBorder(Rect rect, float thickness)
    {
        if (borderTexture == null)
        {
            return;
        }
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), borderTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), borderTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), borderTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), borderTexture);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static void LoadLevel(int index)
    {
        if (index >= 0 && index < LevelScenes.Length)
        {
            SceneManager.LoadScene(LevelScenes[index]);
        }
    }
}
