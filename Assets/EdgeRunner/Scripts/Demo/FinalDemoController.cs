using System;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalDemoController : MonoBehaviour
{
    public const string MenuScene = "ER_FinalDemo_Menu";
    public const string RandomScene = FinalDemoRandomSpeedRun.SceneName;
    public const string RandomMaxScoreScene = FinalDemoRandomMaxScore.SceneName;

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
        "MaxScore Hard",
        "Random SpeedRun",
        "Random MaxScore"
    };

    [SerializeField] private int levelIndex = -1;
    [SerializeField] private string modelLabel = "";
    [SerializeField] private string levelDescription = "";

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle statusStyle;
    private GUIStyle menuPanelStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle speedButtonStyle;
    private GUIStyle maxScoreButtonStyle;
    private GUIStyle menuSpeedColumnStyle;
    private GUIStyle menuMaxScoreColumnStyle;
    private GUIStyle menuFooterStyle;
    private GUIStyle menuHeaderStyle;
    private GUIStyle menuMaxScoreHeaderStyle;
    private GUIStyle menuSubtitleStyle;
    private GUIStyle menuModeDescriptionStyle;
    private GUIStyle menuControlsStyle;
    private GUIStyle menuKeycapStyle;
    private GUIStyle menuControlLabelStyle;
    private GUIStyle menuKeyHintStyle;
    private GUIStyle hudPanelStyle;
    private GUIStyle hudTitleStyle;
    private GUIStyle hudModeStyle;
    private GUIStyle hudStatLabelStyle;
    private GUIStyle hudStatValueStyle;
    private GUIStyle hudControlsStyle;
    private GUIStyle menuBadgeStyle;
    private GUIStyle menuMaxBadgeStyle;
    private GUIStyle hudBadgeStyle;
    private GUIStyle hudCaptionStyle;
    private GUIStyle menuTaglineStyle;
    private GUIStyle menuRandomSpeedButtonStyle;
    private GUIStyle menuRandomScoreButtonStyle;
    private Texture2D whiteTexture;
    private Texture2D menuLogo;
    private Texture2D borderTexture;
    private Texture2D maxScoreBorderTexture;
    private Texture2D menuBackdropTexture;
    private Texture2D menuHorizonTexture;
    private Texture2D menuLightTexture;
    private Texture2D menuSpeedGlowTexture;
    private Texture2D menuMaxScoreGlowTexture;
    private Texture2D menuCyanHaloTexture;
    private Texture2D menuMagentaHaloTexture;
    private Texture2D menuMoonTexture;
    private Texture2D speedAccentTexture;
    private Texture2D maxScoreAccentTexture;
    private int completedRuns;
    private float lastCompletionTime = -100f;
    private bool telemetryEnabled;
    private float nextTelemetryTime;
    private EdgeRunnerAgentV5 trackedAgent;
    // Persists across scene loads (menu -> level, level -> level via N/1-6/R) so the choice
    // made in the menu keeps applying without needing to revisit the menu each time.
    // Agent (false) is the default and exactly matches pre-existing behavior when untouched.
    private static bool manualControlEnabled = false;

    // Read-only, so nothing outside this class can flip it — only ScoreAttackCoin/
    // ScoreAttackAndroid check this, to bypass EdgeRunnerAgentV5ScoreMaxObjectAware's ordered-
    // curriculum accept/reject checks (trained-order enforcement that a free-form human player
    // cannot be expected to follow) while Manual is active. Agent mode never reads true here,
    // so its behavior is completely unaffected.
    public static bool IsManualControlActive => manualControlEnabled;
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

    // Same restart path as the R key (and already used by Random Speed/Score's own reload).
    // Reusing it for Manual's goal/DeathZone handling means Manual gets an identical, already
    // proven reset — repositioning, ScoreAttackManager, coins/Androids and the manual
    // controller are all re-created fresh by the scene reload, with zero dependency on
    // EdgeRunnerAgentV5's own EndEpisode()/OnEpisodeBegin() pipeline.
    public void RestartCurrentLevel()
    {
        if (levelIndex == 6)
        {
            FinalDemoRandomSpeedRun.ReloadSameSeed();
        }
        else if (levelIndex == 7)
        {
            FinalDemoRandomMaxScore.ReloadSameSeed();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Set while a Manual goal-completion restart is pending; checked every frame from Update()
    // (see below) instead of a StartCoroutine/WaitForSeconds. WaitForSeconds is driven by
    // Time.timeScale, and DeathZone's own Manual path (hotfix10, confirmed working) calls
    // RestartCurrentLevel() immediately with no delay at all — the coroutine/delay was the one
    // structural difference between the two paths, so this removes that dependency entirely
    // and reuses the exact same Update() loop that already reliably drives HUD/hotkeys/
    // ApplyControlMode() every frame in this class.
    private bool manualGoalCompletionPending;
    private float manualGoalCompletionRestartTime;

    // Called by ScoreAttackGoalLock instead of EdgeRunnerAgentV5.GoalReached() when Manual is
    // active. Manual cannot rely on the Agent's own EndEpisode()/OnEpisodeBegin() reset (that
    // pipeline runs the ObjectAware curriculum's own internal state, which a free-form human
    // player does not keep consistent — see ScoreAttackCoin/ScoreAttackAndroid's Manual bypass
    // for the same reasoning). Shows the same "OBJETIVO CONCLUÍDO" banner as Agent mode, then
    // reuses the exact same restart path as the R key/DeathZone after a short delay so the
    // banner is actually visible before the scene reloads.
    public void CompleteLevelManual()
    {
        if (manualGoalCompletionPending)
        {
            return;
        }

        manualGoalCompletionPending = true;
        manualGoalCompletionRestartTime = Time.unscaledTime + 2.5f;
        NotifyGoalReached();
    }

    // Tracks whether ApplyControlMode has already done its one-time setup (adding/configuring
    // DemoManualPlayerController and, for MaxScore, DemoSprintVisual) for the current level.
    private bool controlModeInitialized;
    private bool warnedDecisionRequesterReenabled;
    private bool warnedStartupGateReenabled;

    // Switches between Agent (inference) and Manual control.
    //
    // hotfix7 disabled the whole Agent component (trackedAgent.enabled = false) while Manual
    // was active. That broke MaxScore: ScoreAttackCoin/ScoreAttackAndroid call methods directly
    // on EdgeRunnerAgentV5ScoreMaxObjectAware (TryAcceptScoreAttackCoinCollection /
    // TryAcceptScoreAttackAndroidStomp) to decide whether a pickup/stomp counts, and those
    // methods drive the ObjectAware curriculum's "next expected objective" bookkeeping and the
    // reward/episode pipeline inherited from ML-Agents' Agent base class. Disabling the whole
    // component mid-scene disrupts that bookkeeping (observed: high coins triggering a reset,
    // the next low coin becoming uncollectable, Androids no longer stomping, any contact with
    // an Android resetting the level) — plain C# method calls on a disabled component still run,
    // but the Agent's own Unity/ML-Agents lifecycle (which that bookkeeping depends on) does not.
    //
    // Root cause of the original "auto-jump" bug this was trying to fix: for MaxScore levels,
    // FinalDemoMaxScoreStartupGate (Assets/EdgeRunner/Scripts/Demo/FinalDemoMaxScoreStartupGate.cs)
    // intentionally re-enables the agent component and its DecisionRequester over its own
    // delayed coroutine (1-2 frames after scene start), silently overwriting a one-shot disable.
    // The fix here is narrower: leave the Agent component enabled (so MaxScore's coin/Android/
    // stomp/GoalLock logic keeps working exactly as before), and only ever block decision-making
    // itself — disabling DecisionRequester (so OnActionReceived is never invoked) and, on
    // MaxScore, also disabling FinalDemoMaxScoreStartupGate so it can't re-enable that
    // DecisionRequester after the fact. Both are re-asserted every frame from Update(), so
    // nothing can silently undo the Manual choice.
    private void ApplyControlMode()
    {
        if (trackedAgent == null)
        {
            trackedAgent = FindAnyObjectByType<EdgeRunnerAgentV5>();
        }
        if (trackedAgent == null)
        {
            return;
        }

        DecisionRequester decisionRequester = trackedAgent.GetComponent<DecisionRequester>();
        DemoManualPlayerController manualController = trackedAgent.GetComponent<DemoManualPlayerController>();
        // Only present on MaxScore levels; re-enables the agent/requester itself a couple of
        // frames after scene start, so it must be neutralized too while Manual is active.
        FinalDemoMaxScoreStartupGate startupGate = trackedAgent.GetComponent<FinalDemoMaxScoreStartupGate>();

        if (manualControlEnabled)
        {
            // Diagnostic: confirms (once) whenever something re-enabled either component behind
            // our back since the last frame, before we force it off again this frame.
            if (decisionRequester != null && decisionRequester.enabled && !warnedDecisionRequesterReenabled)
            {
                Debug.LogWarning("[MANUAL DIAGNOSTIC] DecisionRequester was re-enabled while Manual is active; forcing it off again.");
                warnedDecisionRequesterReenabled = true;
            }
            if (startupGate != null && startupGate.enabled && !warnedStartupGateReenabled)
            {
                Debug.LogWarning("[MANUAL DIAGNOSTIC] FinalDemoMaxScoreStartupGate was (re-)enabled while Manual is active; forcing it off again.");
                warnedStartupGateReenabled = true;
            }

            if (decisionRequester != null)
            {
                decisionRequester.enabled = false;
            }
            if (startupGate != null)
            {
                startupGate.enabled = false;
            }

            if (!controlModeInitialized)
            {
                Rigidbody2D rb = trackedAgent.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                if (manualController == null)
                {
                    manualController = trackedAgent.gameObject.AddComponent<DemoManualPlayerController>();
                }
                Collider2D bodyCollider = trackedAgent.GetComponent<Collider2D>();
                LayerMask groundMask = 1 << LayerMask.NameToLayer("Ground");
                manualController.Configure(rb, bodyCollider, groundMask);
                manualController.enabled = true;

                // MaxScore's reference-scene player never has a DemoSprintVisual (only the
                // SpeedRun builder adds one) — add it lazily here so Manual mode shows the same
                // sprint feedback in both modes. It reads raw Rigidbody2D speed, so it works
                // correctly regardless of whether Manual or the agent drives movement. Only
                // done inside this Manual-only branch so MaxScore's Agent-mode look is untouched.
                if (trackedAgent.GetComponent<DemoSprintVisual>() == null)
                {
                    AddSprintVisualIfMissing(trackedAgent.gameObject, rb);
                }

                controlModeInitialized = true;
            }
        }
        else
        {
            if (manualController != null)
            {
                manualController.enabled = false;
            }
            if (decisionRequester != null)
            {
                decisionRequester.enabled = true;
            }
            // trackedAgent.enabled is deliberately never touched here — it must stay enabled
            // at all times (see comment above ApplyControlMode).
            controlModeInitialized = false;
        }
    }

    // Mirrors BuildER_FinalDemo.ConfigureSprintVisual (used at scene-build time for SpeedRun)
    // as closely as possible, minus the editor-only AssetDatabase material lookup — this runs
    // in the built Player, so the trail reuses the player sprite's own material instead.
    private static void AddSprintVisualIfMissing(GameObject player, Rigidbody2D rb)
    {
        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
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
        if (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
        {
            trail.sharedMaterial = spriteRenderer.sharedMaterial;
        }

        DemoSprintVisual sprintVisual = player.AddComponent<DemoSprintVisual>();
        sprintVisual.Configure(rb, spriteRenderer, trail);
    }

    private void Start()
    {
        ApplyRuntimeVisualPolish();

        if (levelIndex >= 0)
        {
            telemetryEnabled = HasCommandLineFlag("-telemetry");
            levelStartUnscaledTime = Time.unscaledTime;
            fpsSampleStart = Time.unscaledTime;
            cachedScoreAttackManager = FindAnyObjectByType<ScoreAttackManager>();
            ApplyControlMode();
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

        // Control mode (Agent/Manual) is only toggled from the menu, to avoid adding another
        // gameplay hotkey; the choice then persists into whichever level is loaded next.
        if (levelIndex < 0 && Input.GetKeyDown(KeyCode.C))
        {
            manualControlEnabled = !manualControlEnabled;
        }

        // Re-asserted every frame (not just once in Start()) so nothing — including
        // FinalDemoMaxScoreStartupGate's own delayed re-enable — can silently undo the
        // Manual/Agent choice after the fact.
        if (levelIndex >= 0)
        {
            ApplyControlMode();
        }

        if (manualGoalCompletionPending && Time.unscaledTime >= manualGoalCompletionRestartTime)
        {
            manualGoalCompletionPending = false;
            RestartCurrentLevel();
        }

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
            RestartCurrentLevel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            FinalDemoRandomSpeedRun.LoadNewRandomScene();
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            FinalDemoRandomMaxScore.LoadNewRandomScene();
            return;
        }

        if (Input.GetKeyDown(KeyCode.N) && levelIndex >= 0)
        {
            LoadLevel(levelIndex >= LevelScenes.Length ? 0 : (levelIndex + 1) % LevelScenes.Length);
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
        if (menuBackdropTexture != null)
        {
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                  menuBackdropTexture,
                  ScaleMode.StretchToFill);
        }

        DrawMenuAtmosphere();

        float width = Mathf.Min(1360f, Screen.width - 36f);
        float height = Mathf.Min(788f, Screen.height - 24f);
        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
              height);
        GUI.Box(panel, GUIContent.none, menuPanelStyle);
        DrawBorder(panel, 1f);
        DrawMenuCorners(panel);

        // ---- Hero panel (framed, left) ----
        float heroWidth = Mathf.Clamp(panel.width * 0.25f, 258f, 330f);
        Rect heroArea = new Rect(panel.x + 24f, panel.y + 24f, heroWidth, panel.height - 48f);
        DrawSolid(heroArea, new Color(0.02f, 0.06f, 0.12f, 0.55f));
        DrawBorder(heroArea, 1f, borderTexture);
        DrawTopAccent(heroArea, speedAccentTexture);
        DrawMenuHero(heroArea);

        // ---- Control mode toggle: Agent (default, inference) / Manual (optional) ----
        // Same button style/column as the RANDOM RUNS section below, so it reads as part of
        // the same UI language instead of a default-Unity control bolted on top of the art.
        GUI.Label(new Rect(heroArea.x + 20f, heroArea.yMax - 302f, heroArea.width - 40f, 18f), "CONTROL MODE", hudCaptionStyle);
        DrawSolid(new Rect(heroArea.x + 20f, heroArea.yMax - 281f, heroArea.width - 40f, 1f), new Color(0.2f, 0.62f, 0.82f, 0.4f));
        Rect controlModeButton = new Rect(heroArea.x + 20f, heroArea.yMax - 270f, heroArea.width - 40f, 52f);
        string controlModeLabel = manualControlEnabled ? "C   CONTROL: MANUAL" : "C   CONTROL: AGENT";
        if (GUI.Button(controlModeButton, controlModeLabel, menuRandomSpeedButtonStyle))
        {
            manualControlEnabled = !manualControlEnabled;
            FinalDemoAudioSystem.Play(FinalDemoAudioCue.UIClick);
        }

        GUI.Label(new Rect(heroArea.x + 20f, heroArea.yMax - 210f, heroArea.width - 40f, 18f), "RANDOM RUNS", hudCaptionStyle);
        DrawSolid(new Rect(heroArea.x + 20f, heroArea.yMax - 189f, heroArea.width - 40f, 1f), new Color(0.2f, 0.62f, 0.82f, 0.4f));
        Rect randomButton = new Rect(heroArea.x + 20f, heroArea.yMax - 178f, heroArea.width - 40f, 52f);
        if (GUI.Button(randomButton, "G   RANDOM SPEED", menuRandomSpeedButtonStyle))
        {
            FinalDemoAudioSystem.Play(FinalDemoAudioCue.UIClick);
            FinalDemoRandomSpeedRun.LoadNewRandomScene();
        }
        Rect randomMaxButton = new Rect(heroArea.x + 20f, heroArea.yMax - 118f, heroArea.width - 40f, 52f);
        if (GUI.Button(randomMaxButton, "H   RANDOM SCORE", menuRandomScoreButtonStyle))
        {
            FinalDemoAudioSystem.Play(FinalDemoAudioCue.UIClick);
            FinalDemoRandomMaxScore.LoadNewRandomScene();
        }

        // ---- Title header band ----
        float contentX = heroArea.xMax + 32f;
        float contentWidth = panel.xMax - contentX - 32f;
        Rect headerBand = new Rect(contentX, panel.y + 24f, contentWidth, 118f);
        DrawSolid(headerBand, new Color(0.03f, 0.09f, 0.16f, 0.5f));
        if (menuCyanHaloTexture != null)
        {
            GUI.DrawTexture(new Rect(contentX - 14f, panel.y + 8f, 420f, 116f), menuCyanHaloTexture);
        }
        GUI.Label(new Rect(contentX + 8f, panel.y + 28f, contentWidth, 74f), "EDGERUNNERS", titleStyle);
        DrawSolid(new Rect(contentX + 10f, panel.y + 100f, 300f, 3f), new Color(0.14f, 0.92f, 1f, 0.95f));
        GUI.Label(
            new Rect(contentX + 10f, panel.y + 105f, contentWidth, 30f),
            "FINAL AI DEMO   ·   6 handcrafted levels + 2 random runs",
            menuSubtitleStyle);
        DrawSolid(new Rect(contentX, panel.y + 150f, contentWidth, 1f), new Color(0.2f, 0.62f, 0.82f, 0.45f));

        // ---- Mode columns ----
        float columnGap = 28f;
        float columnWidth = (contentWidth - columnGap) * 0.5f;
        float leftX = contentX;
        float rightX = contentX + columnWidth + columnGap;
        float headerY = panel.y + 164f;
        // Footer height follows the actual wrapped chip layout (never a fixed guess), so the
        // footer box is always tall enough and never clips a row of controls against its edge.
        ChipLayout footerChipLayout = ComputeChipRows(contentWidth - 24f, ControlKeys, ControlLabels);
        float footerHeight = Mathf.Max(66f, footerChipLayout.Height + 40f);
        float footerY = panel.yMax - footerHeight - 24f;
        float columnHeight = footerY - headerY - 18f;

        DrawModeColumn(
            new Rect(leftX, headerY, columnWidth, columnHeight),
            menuSpeedColumnStyle, speedAccentTexture, menuSpeedGlowTexture, menuCyanHaloTexture,
            new Color(0.12f, 0.9f, 1f), "SPEEDRUN", "KEYS 1–3", "Goal · gaps · obstacles",
            speedButtonStyle, menuHeaderStyle, menuBadgeStyle, 0);
        DrawModeColumn(
            new Rect(rightX, headerY, columnWidth, columnHeight),
            menuMaxScoreColumnStyle, maxScoreAccentTexture, menuMaxScoreGlowTexture, menuMagentaHaloTexture,
            new Color(1f, 0.28f, 0.64f), "MAXSCORE", "KEYS 4–6", "Coins · Androids · GoalLock",
            maxScoreButtonStyle, menuMaxScoreHeaderStyle, menuMaxBadgeStyle, 3);

        // ---- Footer controls ----
        Rect footer = new Rect(contentX, footerY, contentWidth, footerHeight);
        GUI.Box(footer, GUIContent.none, menuFooterStyle);
        DrawTopAccent(footer, menuHorizonTexture);
        DrawMenuControls(footer);
    }

    private void DrawModeColumn(
        Rect col, GUIStyle cardStyle, Texture2D accent, Texture2D glow, Texture2D halo,
        Color accentColor, string title, string keys, string desc,
        GUIStyle buttonStyle, GUIStyle headerStyle, GUIStyle badgeStyle, int baseIndex)
    {
        if (glow != null)
        {
            GUI.DrawTexture(new Rect(col.x - 3f, col.y - 3f, col.width + 6f, 10f), glow);
        }
        GUI.Box(col, GUIContent.none, cardStyle);
        DrawTopAccent(col, accent);
        DrawSideAccent(col, accent);

        // Colored header band with title + a keys badge chip.
        DrawSolid(new Rect(col.x + 1f, col.y + 4f, col.width - 2f, 52f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.13f));
        DrawSolid(new Rect(col.x + 1f, col.y + 55f, col.width - 2f, 1f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f));
        GUI.Label(new Rect(col.x + 20f, col.y + 9f, col.width - 108f, 28f), title, headerStyle);
        GUI.Label(new Rect(col.x + 21f, col.y + 35f, col.width - 42f, 18f), desc, menuModeDescriptionStyle);
        Rect badge = new Rect(col.xMax - 98f, col.y + 14f, 78f, 24f);
        DrawSolid(badge, new Color(accentColor.r, accentColor.g, accentColor.b, 0.92f));
        GUI.Label(badge, keys, badgeStyle);

        string[] difficulty = { "EASY", "NORMAL", "HARD" };
        float top = col.y + 68f;
        float gap = 16f;
        float bh = Mathf.Clamp((col.height - 68f - gap * 2f - 8f) / 3f, 58f, 94f);
        for (int row = 0; row < 3; row++)
        {
            Rect b = new Rect(col.x + 16f, top + row * (bh + gap), col.width - 32f, bh);
            if (DrawMenuLevelButton(b, difficulty[row], buttonStyle, accent, halo, accentColor, row + 1))
            {
                FinalDemoAudioSystem.Play(FinalDemoAudioCue.UIClick);
                LoadLevel(baseIndex + row);
            }
        }
    }

    private void DrawLevelOverlay()
    {
        const float margin = 22f;
        bool maxScoreMode = (levelIndex >= 3 && levelIndex < 6) || levelIndex == 7;
        Texture2D modeAccent = maxScoreMode ? maxScoreAccentTexture : speedAccentTexture;
        Texture2D modeBorder = maxScoreMode ? maxScoreBorderTexture : borderTexture;
        Color accentColor = maxScoreMode ? new Color(1f, 0.28f, 0.64f) : new Color(0.12f, 0.9f, 1f);

        // ---- Top-left identity panel: icon + level + mode badge + time ----
        float identityWidth = Mathf.Min(474f, Screen.width - margin * 2f);
        Rect identityPanel = new Rect(margin, margin, identityWidth, 96f);
        GUI.Box(identityPanel, GUIContent.none, hudPanelStyle);
        DrawBorder(identityPanel, 1f, modeBorder);
        DrawSideAccent(identityPanel, modeAccent);
        if (menuLogo != null)
        {
            GUI.DrawTexture(
                new Rect(identityPanel.x + 12f, identityPanel.y + 12f, 52f, identityPanel.height - 24f),
                menuLogo, ScaleMode.ScaleToFit, true);
        }
        float textX = identityPanel.x + 76f;
        GUI.Label(new Rect(textX, identityPanel.y + 11f, identityPanel.width - 86f, 32f), LevelTitles[levelIndex], hudTitleStyle);
        bool speedRunMode = levelIndex < 3 || levelIndex == 6;
        Rect badge = new Rect(textX, identityPanel.y + 50f, 94f, 24f);
        DrawSolid(badge, accentColor);
        GUI.Label(badge, speedRunMode ? "SPEEDRUN" : "MAXSCORE", hudBadgeStyle);
        string seed = levelIndex == 6
            ? $"Seed {FinalDemoRandomSpeedRun.CurrentSeed}"
            : levelIndex == 7
                ? $"Seed {FinalDemoRandomMaxScore.CurrentSeed}"
                : string.Empty;
        string timeText = $"TEMPO {elapsedTimeText}" + (seed.Length > 0 ? "   ·   " + seed : string.Empty);
        GUI.Label(new Rect(textX + 106f, identityPanel.y + 52f, identityPanel.width - 116f, 20f), timeText, hudCaptionStyle);

        // ---- Top-right objective panel with progress bar ----
        const float statsWidth = 326f;
        float statsX = Screen.width - margin - statsWidth;
        float statsY = margin;
        bool stackPanels = statsX < identityPanel.xMax + 12f;
        if (stackPanels) { statsX = margin; statsY = identityPanel.yMax + 8f; }
        Rect statsPanel = new Rect(statsX, statsY, statsWidth, 96f);
        GUI.Box(statsPanel, GUIContent.none, hudPanelStyle);
        DrawBorder(statsPanel, 1f, modeBorder);
        DrawSideAccent(statsPanel, modeAccent);
        DrawObjectiveStats(statsPanel, accentColor);

        if (showFps)
        {
            Rect fpsPanel = new Rect(statsPanel.xMax - 104f, statsPanel.yMax + 8f, 104f, 30f);
            GUI.Box(fpsPanel, GUIContent.none, hudPanelStyle);
            DrawBorder(fpsPanel, 1f, modeBorder);
            GUI.Label(fpsPanel, fpsText, hudControlsStyle);
        }

        // ---- Controls: discreet corner hint + hold-TAB help overlay (no permanent bar) ----
        if (Input.GetKey(KeyCode.Tab) || Input.GetKey(KeyCode.Slash))
        {
            DrawControlsHelpOverlay(modeAccent, modeBorder);
        }
        else
        {
            DrawHudHint(modeAccent);
        }

        if (completedRuns > 0 && Time.unscaledTime - lastCompletionTime < 4f)
        {
            Rect status = new Rect((Screen.width - 400f) * 0.5f, 112f, 400f, 48f);
            GUI.Box(status, $"OBJETIVO CONCLUÍDO  ·  Volta {completedRuns}", statusStyle);
        }
    }

    private const float ChipHeight = 24f;
    private const float ChipInnerGap = 7f;
    private const float ChipItemGap = 14f;
    private const float ChipRowGap = 8f;
    private const float ChipKeyPad = 14f;
    private const float ChipLabelSafetyPad = 4f;

    // Shared, short labels for both the menu footer and the in-game controls overlay —
    // kept identical in both places so the wrap-safe layout never has to fit long strings.
    // The Agent/Manual toggle now lives as its own big button in the menu's hero column
    // instead of a chip here, so it isn't duplicated/hidden in two places.
    private static readonly string[] ControlKeys = { "1–6", "G", "H", "R", "N", "M/Esc", "F", "U", "+/-" };
    private static readonly string[] ControlLabels = { "Level", "Speed", "Score", "Restart", "Next", "Menu", "FPS", "Audio", "Vol" };

    private readonly struct ChipLayout
    {
        public readonly List<List<int>> Rows;
        public readonly float[] KeyWidths;
        public readonly float[] LabelWidths;
        public ChipLayout(List<List<int>> rows, float[] keyWidths, float[] labelWidths)
        {
            Rows = rows;
            KeyWidths = keyWidths;
            LabelWidths = labelWidths;
        }
        public float Height => Rows.Count * ChipHeight + Mathf.Max(0, Rows.Count - 1) * ChipRowGap;
    }

    // Measures chips against the available width and wraps onto extra rows instead of ever
    // letting a key/label spill past its container — this is what "cuts off" text before.
    private ChipLayout ComputeChipRows(float maxWidth, string[] keys, string[] labels)
    {
        float[] keyWidths = new float[keys.Length];
        float[] labelWidths = new float[keys.Length];
        float[] itemWidths = new float[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            keyWidths[i] = Mathf.Max(24f, menuKeycapStyle.CalcSize(new GUIContent(keys[i])).x + ChipKeyPad);
            labelWidths[i] = menuControlLabelStyle.CalcSize(new GUIContent(labels[i])).x + ChipLabelSafetyPad;
            itemWidths[i] = keyWidths[i] + ChipInnerGap + labelWidths[i];
        }

        List<List<int>> rows = new List<List<int>>();
        List<int> current = new List<int>();
        float rowWidth = 0f;
        for (int i = 0; i < keys.Length; i++)
        {
            float additional = itemWidths[i] + (current.Count > 0 ? ChipItemGap : 0f);
            if (current.Count > 0 && rowWidth + additional > maxWidth)
            {
                rows.Add(current);
                current = new List<int>();
                rowWidth = 0f;
                additional = itemWidths[i];
            }
            current.Add(i);
            rowWidth += additional;
        }
        if (current.Count > 0)
        {
            rows.Add(current);
        }
        return new ChipLayout(rows, keyWidths, labelWidths);
    }

    private void DrawChipRows(Rect area, ChipLayout layout, string[] keys, string[] labels)
    {
        float y = area.y;
        for (int r = 0; r < layout.Rows.Count; r++)
        {
            List<int> row = layout.Rows[r];
            float rowWidth = -ChipItemGap;
            for (int k = 0; k < row.Count; k++)
            {
                int idx = row[k];
                rowWidth += layout.KeyWidths[idx] + ChipInnerGap + layout.LabelWidths[idx] + ChipItemGap;
            }
            float x = area.center.x - rowWidth * 0.5f;
            for (int k = 0; k < row.Count; k++)
            {
                int idx = row[k];
                GUI.Box(new Rect(x, y, layout.KeyWidths[idx], ChipHeight), keys[idx], menuKeycapStyle);
                GUI.Label(
                    new Rect(x + layout.KeyWidths[idx] + ChipInnerGap, y, layout.LabelWidths[idx], ChipHeight),
                    labels[idx],
                    menuControlLabelStyle);
                x += layout.KeyWidths[idx] + ChipInnerGap + layout.LabelWidths[idx] + ChipItemGap;
            }
            y += ChipHeight + ChipRowGap;
        }
    }

    private void DrawHudHint(Texture2D modeAccent)
    {
        const float margin = 22f;
        const string hint = "TAB Controls   ·   M Menu   ·   R Restart";
        float w = hudCaptionStyle.CalcSize(new GUIContent(hint)).x + 30f;
        const float h = 26f;
        Rect r = new Rect(margin, Screen.height - margin - h, w, h);
        GUI.Box(r, GUIContent.none, hudPanelStyle);
        DrawSideAccent(r, modeAccent);
        GUI.Label(new Rect(r.x + 14f, r.y, w - 18f, h), hint, hudCaptionStyle);
    }

    private void DrawControlsHelpOverlay(Texture2D modeAccent, Texture2D modeBorder)
    {
        const float screenMargin = 30f;
        // Extra clearance beyond screenMargin so the border/top-accent/corner-bracket
        // decorations (drawn flush with the panel's own edges) never touch the screen edge.
        const float visualBleed = 12f;
        const float margin = screenMargin + visualBleed;
        const float titleTopPadding = 16f;
        const float titleHeight = 40f;
        const float titleAreaHeight = 60f;
        const float chipsTopPad = 12f;
        const float bottomPad = 20f;
        const float contentPadding = 44f; // 22px of inner padding on each side of the panel

        DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.02f, 0.04f, 0.42f));

        string[] labels = ControlLabels;

        // First pass (unwrapped): measure every chip with the styles/font actually active at
        // runtime — the Standalone Player can measure text a bit wider than the Editor preview,
        // so we size the panel off this real measurement instead of assuming the Editor's numbers.
        ChipLayout unwrapped = ComputeChipRows(float.MaxValue, ControlKeys, labels);
        float widestItemWidth = 0f;
        for (int i = 0; i < ControlKeys.Length; i++)
        {
            widestItemWidth = Mathf.Max(widestItemWidth, unwrapped.KeyWidths[i] + ChipInnerGap + unwrapped.LabelWidths[i]);
        }

        // The panel must never be narrower than the single widest chip (otherwise that chip
        // alone would spill past the border no matter how the rows wrap), and never wider than
        // the screen minus margins. Only once both bounds are known do we pick the final width.
        float maxPanelWidth = Screen.width - margin * 2f;
        float minPanelWidth = widestItemWidth + contentPadding;
        float w = Mathf.Clamp(1160f, minPanelWidth, Mathf.Max(minPanelWidth, maxPanelWidth));

        // Second pass: wrap the chips into rows using the final, screen-safe panel width.
        ChipLayout layout = ComputeChipRows(w - contentPadding, ControlKeys, labels);
        float h = titleAreaHeight + chipsTopPad + layout.Height + bottomPad;

        // Anchored near the bottom/centre of the screen, but always fully clamped within it —
        // never lets the panel or any of its edge decorations spill past the screen bounds.
        float maxX = Mathf.Max(margin, Screen.width - w - margin);
        float x = Mathf.Clamp((Screen.width - w) * 0.5f, margin, maxX);
        float maxY = Mathf.Max(margin, Screen.height - h - margin);
        float y = Mathf.Clamp(Screen.height - h - 54f, margin, maxY);
        Rect panel = new Rect(x, y, w, h);
        GUI.Box(panel, GUIContent.none, hudPanelStyle);
        DrawBorder(panel, 1f, modeBorder);
        DrawTopAccent(panel, modeAccent);
        DrawMenuCorners(panel);
        // Dedicated, vertically-centred title style: the shared hudTitleStyle (also used by the
        // top-left level title) has no explicit alignment, and the Standalone Player can measure/
        // render that font a hair taller than the Editor preview, clipping the top of "CONTROLS"
        // against the rect's top edge. Middle-anchoring plus a taller rect removes that dependency.
        GUIStyle controlsTitleStyle = new GUIStyle(hudTitleStyle) { alignment = TextAnchor.MiddleLeft };
        Rect titleRect = new Rect(panel.x + 22f, panel.y + titleTopPadding, 260f, titleHeight);
        GUI.Label(titleRect, "CONTROLS", controlsTitleStyle);
        // Centred within the full title band (not just a thin sliver next to the title) and
        // given a generous height — the previous, tighter Rect here (and a second cramped line
        // below it) is exactly what clipped in the Standalone Player at 16:9. The current
        // Agent/Manual mode now shows as its own chip below instead of a second squeezed line.
        GUI.Label(
            new Rect(panel.xMax - 240f, panel.y + (titleAreaHeight - 22f) * 0.5f, 220f, 22f),
            "Hold TAB to view",
            hudCaptionStyle);
        DrawSolid(new Rect(panel.x + 22f, panel.y + titleAreaHeight, panel.width - 44f, 1f), new Color(0.2f, 0.62f, 0.82f, 0.5f));
        DrawChipRows(
            new Rect(panel.x, panel.y + titleAreaHeight + chipsTopPad, panel.width, layout.Height),
            layout, ControlKeys, labels);
    }

    private void DrawObjectiveStats(Rect panel, Color accent)
    {
        float x = panel.x + 14f;
        float width = panel.width - 28f;
        if (levelIndex < 3 || levelIndex == 6)
        {
            GUI.Label(new Rect(x, panel.y + 10f, width, 20f), "POWERCELLS", hudStatLabelStyle);
            int total = visualCollectibleTotal;
            string value = total > 0 ? $"{visualCollectiblesCollected}/{total}" : "—";
            GUI.Label(new Rect(x, panel.y + 28f, width, 40f), value, hudStatValueStyle);
            if (total > 0)
            {
                DrawProgressBar(new Rect(x, panel.yMax - 18f, width, 10f), visualCollectiblesCollected / (float)total, accent);
            }
            return;
        }

        if (cachedScoreAttackManager == null)
        {
            GUI.Label(new Rect(x, panel.y + 30f, width, 26f), "Objetivos a carregar…", hudModeStyle);
            return;
        }

        int coins = cachedScoreAttackManager.CoinsCollected;
        int coinTotal = coins + cachedScoreAttackManager.CoinsRemaining;
        int androids = cachedScoreAttackManager.EnemiesRemaining;
        GUI.Label(new Rect(x, panel.y + 9f, 130f, 20f), "MOEDAS", hudStatLabelStyle);
        GUI.Label(new Rect(x + 128f, panel.y + 11f, width - 128f, 18f), $"ANDROIDS  {androids}", hudCaptionStyle);
        GUI.Label(new Rect(x, panel.y + 27f, width, 40f), $"{coins}/{coinTotal}", hudStatValueStyle);
        if (coinTotal > 0)
        {
            DrawProgressBar(new Rect(x, panel.yMax - 18f, width, 10f), coins / (float)coinTotal, accent);
        }
    }

    private void ApplyRuntimeVisualPolish()
    {
        bool maxScoreMode = (levelIndex >= 3 && levelIndex < 6) || levelIndex == 7;
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = maxScoreMode
                ? new Color(0.032f, 0.018f, 0.065f, 1f)
                : new Color(0.018f, 0.045f, 0.075f, 1f);
        }

        GameObject farSkyObject = GameObject.Find("FarSky");
        SpriteRenderer farSky = farSkyObject != null
            ? farSkyObject.GetComponent<SpriteRenderer>()
            : null;
        if (farSky == null || farSky.sprite == null)
        {
            return;
        }

        farSky.color = maxScoreMode
            ? new Color(0.065f, 0.035f, 0.115f, 1f)
            : new Color(0.025f, 0.07f, 0.115f, 1f);

        if (levelIndex < 0)
        {
            SetDecorativeRendererVisible("DistantAtmosphereGlow", false);
            SetDecorativeRendererVisible("NeonHorizon", false);
            return;
        }

        TintDecorativeRenderer(
            "DistantAtmosphereGlow",
            maxScoreMode
                ? new Color(0.35f, 0.08f, 0.36f, 0.22f)
                : new Color(0.04f, 0.38f, 0.5f, 0.19f));
        TintDecorativeRenderer(
            "NeonHorizon",
            maxScoreMode
                ? new Color(1f, 0.26f, 0.64f, 0.56f)
                : new Color(0.08f, 0.95f, 1f, 0.54f));
        PolishExistingDecorativeAccents(maxScoreMode);
        CreateRuntimeSkyline(farSky, maxScoreMode);
    }

    private static void SetDecorativeRendererVisible(string objectName, bool visible)
    {
        GameObject target = GameObject.Find(objectName);
        SpriteRenderer renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private static void TintDecorativeRenderer(string objectName, Color color)
    {
        GameObject target = GameObject.Find(objectName);
        SpriteRenderer renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    private static void PolishExistingDecorativeAccents(bool maxScoreMode)
    {
        Color routeAccent = maxScoreMode
            ? new Color(1f, 0.31f, 0.68f, 0.88f)
            : new Color(0.08f, 0.94f, 1f, 0.88f);
        Color backgroundAccent = maxScoreMode
            ? new Color(0.76f, 0.24f, 0.72f, 0.64f)
            : new Color(0.18f, 0.7f, 0.82f, 0.64f);

        SpriteRenderer[] renderers =
            FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            string objectName = renderer.gameObject.name;

            if (objectName.EndsWith("_NeonTop", StringComparison.Ordinal))
            {
                renderer.color = routeAccent;
            }
            else if (objectName.StartsWith("VisualPatrolDeckEdge_", StringComparison.Ordinal))
            {
                renderer.color = backgroundAccent;
            }
        }
    }

    private static void CreateRuntimeSkyline(SpriteRenderer farSky, bool maxScoreMode)
    {
        if (GameObject.Find("FinalDemo_RuntimePolish") != null)
        {
            return;
        }

        Bounds skyBounds = farSky.bounds;
        if (skyBounds.size.x < 4f)
        {
            return;
        }

        GameObject root = new GameObject("FinalDemo_RuntimePolish");
        int buildingCount = Mathf.Clamp(Mathf.CeilToInt(skyBounds.size.x / 13f), 8, 24);
        Color buildingColor = maxScoreMode
            ? new Color(0.075f, 0.035f, 0.105f, 0.92f)
            : new Color(0.025f, 0.075f, 0.105f, 0.92f);
        Color windowColor = maxScoreMode
            ? new Color(1f, 0.28f, 0.66f, 0.3f)
            : new Color(0.12f, 0.9f, 1f, 0.28f);

        for (int i = 0; i < buildingCount; i++)
        {
            float t = (i + 0.5f) / buildingCount;
            float x = Mathf.Lerp(skyBounds.min.x, skyBounds.max.x, t);
            float width = 2.8f + Mathf.PingPong(i * 1.17f + 0.4f, 2.2f);
            float height = 1.8f + Mathf.PingPong(i * 1.73f + 0.6f, 3.4f);
            float baseY = -1.02f;
            CreateDecorativeSprite(
                root.transform,
                $"SkylineBuilding_{i:00}",
                farSky.sprite,
                new Vector2(x, baseY + height * 0.5f),
                new Vector2(width, height),
                buildingColor,
                -24);
            CreateDecorativeSprite(
                root.transform,
                $"SkylineLight_{i:00}",
                farSky.sprite,
                new Vector2(x + width * 0.22f, baseY + height * 0.56f),
                new Vector2(0.08f, height * 0.38f),
                windowColor,
                -23);
        }

        int starCount = Mathf.Clamp(buildingCount, 10, 20);
        for (int i = 0; i < starCount; i++)
        {
            float t = (i + 0.35f) / starCount;
            float x = Mathf.Lerp(skyBounds.min.x, skyBounds.max.x, t);
            float y = 3.2f + Mathf.PingPong(i * 1.91f + 0.8f, 4.8f);
            float size = 0.035f + (i % 3) * 0.018f;
            CreateDecorativeSprite(
                root.transform,
                $"SkyLight_{i:00}",
                farSky.sprite,
                new Vector2(x, y),
                new Vector2(size, size),
                windowColor,
                -22);
        }
    }

    private static void CreateDecorativeSprite(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(parent, false);
        visual.transform.position = new Vector3(position.x, position.y, 1.5f);
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 62,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.85f, 0.99f, 1f) }
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
        Texture2D panelTexture = MakeTexture(new Color(0.016f, 0.038f, 0.078f, 0.96f));
        Texture2D buttonTexture = MakeTexture(new Color(0.04f, 0.16f, 0.23f, 0.98f));
        Texture2D speedButtonHover = MakeTexture(new Color(0.06f, 0.4f, 0.5f, 1f));
        Texture2D speedButtonActive = MakeTexture(new Color(0.14f, 0.6f, 0.7f, 1f));
        Texture2D maxScoreButtonTexture = MakeTexture(new Color(0.19f, 0.06f, 0.23f, 0.98f));
        Texture2D maxScoreButtonHover = MakeTexture(new Color(0.45f, 0.1f, 0.46f, 1f));
        Texture2D maxScoreButtonActive = MakeTexture(new Color(0.66f, 0.17f, 0.6f, 1f));
        menuBackdropTexture = MakeVerticalGradient(
            new Color(0.03f, 0.06f, 0.14f, 0.82f),
            new Color(0.004f, 0.008f, 0.028f, 0.97f));
        menuHorizonTexture = MakeTexture(new Color(0.24f, 0.72f, 0.88f, 0.5f));
        menuLightTexture = MakeTexture(new Color(0.32f, 0.92f, 1f, 0.48f));
        menuSpeedGlowTexture = MakeTexture(new Color(0.08f, 0.82f, 1f, 0.16f));
        menuMaxScoreGlowTexture = MakeTexture(new Color(1f, 0.18f, 0.66f, 0.16f));
        menuCyanHaloTexture = MakeRadialGlowTexture(new Color(0.08f, 0.88f, 1f, 0.42f));
        menuMagentaHaloTexture = MakeRadialGlowTexture(new Color(1f, 0.18f, 0.66f, 0.32f));
        menuMoonTexture = MakePixelMoonTexture();
        speedAccentTexture = MakeTexture(new Color(0.12f, 0.9f, 1f, 0.95f));
        maxScoreAccentTexture = MakeTexture(new Color(1f, 0.28f, 0.64f, 0.95f));
        borderTexture = MakeTexture(new Color(0.12f, 0.9f, 1f, 0.9f));
        maxScoreBorderTexture = MakeTexture(new Color(1f, 0.3f, 0.66f, 0.88f));
        menuPanelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture }
        };
        menuSpeedColumnStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeVerticalGradient(new Color(0.03f, 0.13f, 0.2f, 0.78f), new Color(0.012f, 0.06f, 0.1f, 0.82f)) }
        };
        menuMaxScoreColumnStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeVerticalGradient(new Color(0.17f, 0.04f, 0.19f, 0.78f), new Color(0.08f, 0.02f, 0.1f, 0.82f)) }
        };
        menuFooterStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.03f, 0.08f, 0.12f, 0.9f)) }
        };
        hudPanelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeVerticalGradient(new Color(0.02f, 0.05f, 0.09f, 0.82f), new Color(0.008f, 0.022f, 0.045f, 0.88f)) }
        };
        hudTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.55f, 0.97f, 1f) }
        };
        hudModeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.66f, 0.8f, 0.92f) }
        };
        hudStatLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.72f, 0.9f, 1f) }
        };
        hudStatValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.84f, 0.24f) }
        };
        hudControlsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.66f, 0.8f, 0.9f) }
        };
        menuButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(26, 12, 0, 0),
            normal = { background = buttonTexture, textColor = new Color(0.86f, 0.97f, 1f) },
            hover = { background = speedButtonHover, textColor = Color.white },
            active = { background = speedButtonActive, textColor = Color.white }
        };
        speedButtonStyle = new GUIStyle(menuButtonStyle);
        maxScoreButtonStyle = new GUIStyle(menuButtonStyle);
        maxScoreButtonStyle.normal.background = maxScoreButtonTexture;
        maxScoreButtonStyle.normal.textColor = new Color(1f, 0.9f, 0.98f);
        maxScoreButtonStyle.hover.background = maxScoreButtonHover;
        maxScoreButtonStyle.hover.textColor = Color.white;
        maxScoreButtonStyle.active.background = maxScoreButtonActive;
        maxScoreButtonStyle.active.textColor = Color.white;
        // Dedicated (smaller) font for the Random buttons — their label is longer than
        // EASY/NORMAL/HARD, so they get their own style instead of shrinking those buttons.
        menuRandomSpeedButtonStyle = new GUIStyle(speedButtonStyle) { fontSize = 18 };
        menuRandomScoreButtonStyle = new GUIStyle(maxScoreButtonStyle) { fontSize = 18 };
        menuHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.42f, 0.92f, 1f) }
        };
        menuMaxScoreHeaderStyle = new GUIStyle(menuHeaderStyle);
        menuMaxScoreHeaderStyle.normal.textColor = new Color(1f, 0.42f, 0.72f);
        menuSubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.78f, 0.88f, 0.95f) }
        };
        menuModeDescriptionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.72f, 0.82f, 0.9f) }
        };
        menuControlsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.93f, 1f) }
        };
        menuKeycapStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                background = MakeTexture(new Color(0.07f, 0.18f, 0.24f, 0.96f)),
                textColor = new Color(0.82f, 0.98f, 1f)
            }
        };
        menuControlLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.76f, 0.88f, 0.95f) }
        };
        menuKeyHintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.55f, 0.68f, 0.76f) }
        };
        whiteTexture = MakeTexture(Color.white);
        menuBadgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.02f, 0.06f, 0.09f) }
        };
        menuMaxBadgeStyle = new GUIStyle(menuBadgeStyle);
        hudBadgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.03f, 0.06f, 0.1f) }
        };
        hudCaptionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.6f, 0.76f, 0.88f) }
        };
        menuTaglineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.62f, 0.86f, 1f) }
        };
    }

    private void DrawSolid(Rect rect, Color color)
    {
        if (whiteTexture == null) return;
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = previous;
    }

    private void DrawProgressBar(Rect rect, float fraction, Color fill)
    {
        DrawSolid(rect, new Color(0.02f, 0.05f, 0.08f, 0.9f));
        DrawSolid(new Rect(rect.x, rect.y, rect.width, 1f), new Color(fill.r, fill.g, fill.b, 0.25f));
        float w = Mathf.Clamp01(fraction) * (rect.width - 2f);
        if (w > 0.5f)
        {
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, w, rect.height - 2f), fill);
        }
    }

    private void DrawMenuAtmosphere()
    {
        if (menuHorizonTexture == null || menuLightTexture == null)
        {
            return;
        }

        float horizonY = Screen.height * 0.7f;
        DrawMenuMoon();

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.28f);
        GUI.DrawTexture(new Rect(0f, horizonY, Screen.width, 1f), menuHorizonTexture);
        GUI.color = previousColor;

        for (int i = 0; i < 18; i++)
        {
            float x = Mathf.Repeat(i * 157f + 41f, Mathf.Max(1f, Screen.width));
            float y = Mathf.Repeat(i * 83f + 29f, Mathf.Max(1f, horizonY - 24f));
            float size = 2f + i % 3;
            Texture2D halo = i % 5 == 0 ? menuMagentaHaloTexture : menuCyanHaloTexture;
            if (halo != null)
            {
                float haloSize = 16f + size * 3f;
                GUI.DrawTexture(
                    new Rect(x - haloSize * 0.5f, y - haloSize * 0.5f, haloSize, haloSize),
                    halo);
            }
            GUI.DrawTexture(new Rect(x, y, size, size), menuLightTexture);
        }
    }

    private void DrawMenuMoon()
    {
        if (menuMoonTexture == null)
        {
            return;
        }

        float moonSize = Mathf.Clamp(Screen.height * 0.17f, 112f, 176f);
        float x = Screen.width - moonSize - Mathf.Max(34f, Screen.width * 0.045f);
        float y = Mathf.Max(24f, Screen.height * 0.045f);
        Rect moonRect = new Rect(x, y, moonSize, moonSize);

        if (menuCyanHaloTexture != null)
        {
            float haloSize = moonSize * 2.15f;
            GUI.DrawTexture(
                new Rect(
                    moonRect.center.x - haloSize * 0.5f,
                    moonRect.center.y - haloSize * 0.5f,
                    haloSize,
                    haloSize),
                menuCyanHaloTexture);
        }
        GUI.DrawTexture(moonRect, menuMoonTexture, ScaleMode.StretchToFill, true);
    }

    private void DrawMenuHero(Rect area)
    {
        if (menuLogo != null)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.98f);
            GUI.DrawTexture(
                new Rect(area.x + 4f, area.y + 8f, area.width - 8f, area.height - 42f),
                menuLogo,
                ScaleMode.ScaleToFit,
                true);
            GUI.color = previousColor;
        }

        if (menuCyanHaloTexture != null)
        {
            GUI.DrawTexture(
                new Rect(area.x - 10f, area.y + 65f, area.width + 20f, area.height - 155f),
                menuCyanHaloTexture);
        }

        if (speedAccentTexture != null)
        {
            GUI.DrawTexture(new Rect(area.x + 3f, area.y + 52f, 1f, area.height - 118f), speedAccentTexture);
            GUI.DrawTexture(new Rect(area.x + 3f, area.y + 52f, 32f, 1f), speedAccentTexture);
            GUI.DrawTexture(new Rect(area.x + 3f, area.yMax - 66f, 32f, 1f), speedAccentTexture);
            GUI.DrawTexture(new Rect(area.xMax - 4f, area.y + 88f, 1f, area.height - 190f), speedAccentTexture);
        }

        if (menuHorizonTexture != null)
        {
            float centerX = area.center.x;
            GUI.DrawTexture(new Rect(centerX - 72f, area.yMax - 42f, 144f, 2f), menuHorizonTexture);
            GUI.DrawTexture(new Rect(centerX - 44f, area.yMax - 35f, 88f, 1f), menuHorizonTexture);
        }

        GUI.Label(
            new Rect(area.x, area.yMax - 31f, area.width, 24f),
            "RUN  ·  ADAPT  ·  EVOLVE",
            menuControlsStyle);
    }

    private void DrawMenuControls(Rect footer)
    {
        string[] labels = ControlLabels;

        // Measured, wrap-safe layout: never lets a chip/label spill past the footer edge,
        // regardless of window size (the build's window is resizable).
        ChipLayout layout = ComputeChipRows(footer.width - 24f, ControlKeys, labels);
        float y = footer.center.y - layout.Height * 0.5f;
        for (int r = 0; r < layout.Rows.Count; r++)
        {
            List<int> row = layout.Rows[r];
            float rowWidth = -ChipItemGap;
            for (int k = 0; k < row.Count; k++)
            {
                int idx = row[k];
                rowWidth += layout.KeyWidths[idx] + ChipInnerGap + layout.LabelWidths[idx] + ChipItemGap;
            }
            float x = footer.center.x - rowWidth * 0.5f;
            for (int k = 0; k < row.Count; k++)
            {
                int idx = row[k];
                Rect keyRect = new Rect(x, y, layout.KeyWidths[idx], ChipHeight);
                if (menuCyanHaloTexture != null)
                {
                    GUI.DrawTexture(
                        new Rect(keyRect.x - 4f, keyRect.y - 4f, keyRect.width + 8f, keyRect.height + 8f),
                        menuCyanHaloTexture);
                }
                GUI.Box(keyRect, ControlKeys[idx], menuKeycapStyle);
                GUI.Label(
                    new Rect(keyRect.xMax + ChipInnerGap, y, layout.LabelWidths[idx], ChipHeight),
                    labels[idx],
                    menuControlLabelStyle);
                x += layout.KeyWidths[idx] + ChipInnerGap + layout.LabelWidths[idx] + ChipItemGap;
            }
            y += ChipHeight + ChipRowGap;
        }
    }

    private bool DrawMenuLevelButton(
        Rect rect, string label, GUIStyle style, Texture2D accent, Texture2D glow, Color accentColor, int difficulty)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        if (hovered && glow != null)
        {
            GUI.DrawTexture(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f), glow);
        }
        bool pressed = GUI.Button(rect, label, style);
        // Left accent bar (thicker on hover) reads as a real, selectable button edge.
        DrawSolid(new Rect(rect.x, rect.y + 6f, hovered ? 7f : 4f, rect.height - 12f), accentColor);
        // Difficulty pips on the right (1 = Easy, 3 = Hard).
        const float pip = 9f;
        const float pipGap = 6f;
        float px = rect.xMax - 20f - (3f * pip + 2f * pipGap);
        float py = rect.center.y - pip * 0.5f;
        for (int i = 0; i < 3; i++)
        {
            Color c = i < difficulty ? accentColor : new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f);
            DrawSolid(new Rect(px + i * (pip + pipGap), py, pip, pip), c);
        }
        return pressed;
    }

    private void DrawMenuCorners(Rect rect)
    {
        if (speedAccentTexture == null || maxScoreAccentTexture == null)
        {
            return;
        }

        const float length = 34f;
        const float thickness = 3f;
        GUI.DrawTexture(new Rect(rect.x, rect.y, length, thickness), speedAccentTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, length), speedAccentTexture);
        GUI.DrawTexture(new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness), maxScoreAccentTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length), maxScoreAccentTexture);
    }

    private static void DrawTopAccent(Rect rect, Texture2D accent)
    {
        if (accent != null)
        {
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 3f), accent);
        }
    }

    private static void DrawSideAccent(Rect rect, Texture2D accent)
    {
        if (accent != null)
        {
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 6f, 3f, rect.height - 12f), accent);
        }
    }

    private void DrawBorder(Rect rect, float thickness)
    {
        DrawBorder(rect, thickness, borderTexture);
    }

    private static void DrawBorder(Rect rect, float thickness, Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), texture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), texture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), texture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), texture);
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

    private static Texture2D MakeVerticalGradient(Color top, Color bottom)
    {
        const int height = 128;
        Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < height; y++)
        {
            texture.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D MakeRadialGlowTexture(Color color)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float falloff = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.4f);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * falloff));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D MakePixelMoonTexture()
    {
        const int pixelSize = 16;
        const int blockSize = 8;
        const int size = pixelSize * blockSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Color[,] pixelMap = new Color[pixelSize, pixelSize];
        Color clear = Color.clear;
        Color rim = new Color(0.93f, 0.99f, 1f, 0.98f);
        Color bright = new Color(0.75f, 0.93f, 1f, 0.95f);
        Color mid = new Color(0.46f, 0.75f, 0.92f, 0.92f);
        Color dark = new Color(0.22f, 0.44f, 0.58f, 0.88f);
        Color crater = new Color(0.16f, 0.34f, 0.46f, 0.82f);

        for (int py = 0; py < pixelSize; py++)
        {
            for (int px = 0; px < pixelSize; px++)
            {
                bool inside =
                    (py >= 2 && py <= 13 && px >= 4 && px <= 11) ||
                    (py >= 4 && py <= 11 && px >= 2 && px <= 13) ||
                    (py >= 3 && py <= 12 && px >= 3 && px <= 12);

                if (!inside)
                {
                    pixelMap[px, py] = clear;
                    continue;
                }

                bool rimPixel = px == 2 || px == 13 || py == 2 || py == 13 || px == 3 || px == 12 || py == 3 || py == 12;
                bool lowerShadow = px >= 9 || py >= 10;
                Color baseColor = rimPixel ? rim : (lowerShadow ? mid : bright);
                pixelMap[px, py] = baseColor;
            }
        }

        pixelMap[5, 5] = crater;
        pixelMap[6, 5] = dark;
        pixelMap[5, 6] = dark;
        pixelMap[9, 4] = dark;
        pixelMap[10, 4] = crater;
        pixelMap[9, 5] = crater;
        pixelMap[7, 8] = crater;
        pixelMap[8, 8] = dark;
        pixelMap[7, 9] = dark;
        pixelMap[10, 9] = crater;
        pixelMap[11, 10] = dark;
        pixelMap[4, 10] = crater;
        pixelMap[5, 11] = dark;
        pixelMap[8, 12] = new Color(0.58f, 0.84f, 0.98f, 0.9f);

        for (int py = 0; py < pixelSize; py++)
        {
            for (int px = 0; px < pixelSize; px++)
            {
                Color source = pixelMap[px, py];
                for (int by = 0; by < blockSize; by++)
                {
                    for (int bx = 0; bx < blockSize; bx++)
                    {
                        texture.SetPixel(px * blockSize + bx, py * blockSize + by, source);
                    }
                }
            }
        }

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
