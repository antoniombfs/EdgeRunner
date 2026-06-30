using System.IO;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildScoreMaxOAFinalLongChallengePlayer
{
    private const string ScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_FinalLongChallenge.unity";
    private const string PlayerPrefabPath =
        "Assets/EdgeRunner/Prefabs/Agent/Player_V5.prefab";
    private const string OutputDirectory = "Builds/ScoreMaxOA_FinalLongChallenge";
    private const string OutputExecutable =
        "EdgeRunner_ScoreMaxOA_FinalLongChallenge.exe";

    [MenuItem("EdgeRunner/Build/Training/Build ScoreMaxOA FinalLongChallenge")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);

        string outputDirectory = Path.GetFullPath(OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, OutputExecutable);

        int previousWidth = PlayerSettings.defaultScreenWidth;
        int previousHeight = PlayerSettings.defaultScreenHeight;
        FullScreenMode previousFullScreenMode = PlayerSettings.fullScreenMode;
        bool previousResizableWindow = PlayerSettings.resizableWindow;
        bool previousRunInBackground = PlayerSettings.runInBackground;

        try
        {
            PlayerSettings.defaultScreenWidth = 640;
            PlayerSettings.defaultScreenHeight = 360;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"FinalLongChallenge training build failed: {summary.result}, " +
                    $"errors={summary.totalErrors}, warnings={summary.totalWarnings}.");
            }

            Debug.Log(
                $"[FINAL LONG TRAINING BUILD] Created '{outputPath}' " +
                $"size={summary.totalSize} bytes time={summary.totalTime}.");
        }
        finally
        {
            PlayerSettings.defaultScreenWidth = previousWidth;
            PlayerSettings.defaultScreenHeight = previousHeight;
            PlayerSettings.fullScreenMode = previousFullScreenMode;
            PlayerSettings.resizableWindow = previousResizableWindow;
            PlayerSettings.runInBackground = previousRunInBackground;
            AssetDatabase.SaveAssets();
        }
    }

    private static void ValidateScene(Scene scene)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware[] agents =
            Object.FindObjectsByType<EdgeRunnerAgentV5ScoreMaxObjectAware>(
                FindObjectsInactive.Include);
        ScoreAttackCoin[] coins = Object.FindObjectsByType<ScoreAttackCoin>(
            FindObjectsInactive.Include);
        ScoreAttackAndroid[] androids = Object.FindObjectsByType<ScoreAttackAndroid>(
            FindObjectsInactive.Include);
        if (agents.Length != 1 || coins.Length != 7 || androids.Length != 2 ||
            Object.FindObjectsByType<ScoreAttackGoalLock>(FindObjectsInactive.Include).Length != 1 ||
            Object.FindObjectsByType<DeathZone>(FindObjectsInactive.Include).Length != 1 ||
            Object.FindObjectsByType<DemoAndroidPatrol>(FindObjectsInactive.Include).Length != 0)
        {
            throw new System.InvalidOperationException(
                "FinalLongChallenge build requires one ObjectAware agent, seven coins, " +
                "two static Androids, one GoalLock, one DeathZone, and no patrol.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent = agents[0];
        BehaviorParameters behavior = agent.GetComponent<BehaviorParameters>();
        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedObject serializedBehavior = new SerializedObject(behavior);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedProperty antiLedge = serializedAgent.FindProperty(
            "enableAntiLedgeStuckFailSafe");
        SerializedProperty jumpForce = serializedAgent.FindProperty("jumpForce");
        SerializedProperty observationSize = serializedBehavior.FindProperty(
            "m_BrainParameters.VectorObservationSize");
        SerializedProperty continuousActions = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.m_NumContinuousActions");
        SerializedProperty branches = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.BranchSizes");

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        EdgeRunnerAgentV5 prefabAgent = playerPrefab != null
            ? playerPrefab.GetComponent<EdgeRunnerAgentV5>()
            : null;
        SerializedProperty prefabJumpForce = prefabAgent != null
            ? new SerializedObject(prefabAgent).FindProperty("jumpForce")
            : null;
        bool validBranches = branches != null && branches.arraySize == 3 &&
            branches.GetArrayElementAtIndex(0).intValue == 3 &&
            branches.GetArrayElementAtIndex(1).intValue == 2 &&
            branches.GetArrayElementAtIndex(2).intValue == 2;
        bool validContract = behavior != null &&
            behavior.BehaviorName == "EdgeRunnerV5ScoreMaxObjectAware" &&
            behavior.BehaviorType == BehaviorType.Default && behavior.Model == null &&
            observationSize != null && observationSize.intValue == 111 &&
            continuousActions != null && continuousActions.intValue == 0 && validBranches &&
            phase != null &&
            phase.enumValueIndex == (int)EdgeRunnerObjectAwarePhase.FinalLongChallenge &&
            antiLedge != null && antiLedge.boolValue &&
            jumpForce != null && prefabJumpForce != null &&
            Mathf.Abs(jumpForce.floatValue - prefabJumpForce.floatValue) <= 0.0001f;
        if (!validContract)
        {
            throw new System.InvalidOperationException(
                "FinalLongChallenge build contract must be Default/Model None, " +
                "111 observations, [3,2,2], anti-ledge enabled, and unchanged jumpForce.");
        }

        int lowCoins = 0;
        int highCoins = 0;
        for (int i = 0; i < coins.Length; i++)
        {
            SerializedObject serializedCoin = new SerializedObject(coins[i]);
            SerializedProperty fallback = serializedCoin.FindProperty(
                "enableTriggerStayFallback");
            Collider2D collider = coins[i].GetComponent<Collider2D>();
            if (!coins[i].gameObject.activeSelf || !coins[i].enabled ||
                collider == null || !collider.enabled || !collider.isTrigger ||
                fallback == null || !fallback.boolValue)
            {
                throw new System.InvalidOperationException(
                    $"FinalLongChallenge coin '{coins[i].name}' is not training-ready.");
            }

            if (coins[i].name.Contains("LowCoin")) lowCoins++;
            else if (coins[i].name.Contains("HighCoin")) highCoins++;
        }

        if (lowCoins != 4 || highCoins != 3)
        {
            throw new System.InvalidOperationException(
                $"FinalLongChallenge requires 4 low and 3 high coins; got {lowCoins}/{highCoins}.");
        }

        Debug.Log(
            "[FINAL LONG TRAINING BUILD] Scene validated: Default/None, obs=111, " +
            "actions=[3,2,2], low=4, high=3, Androids=2, GoalLock=true, " +
            "DeathZone=true, antiLedge=true, jumpForce unchanged.");
    }
}
