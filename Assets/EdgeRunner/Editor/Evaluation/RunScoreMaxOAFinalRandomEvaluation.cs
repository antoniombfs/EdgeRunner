using Unity.MLAgents.Policies;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RunScoreMaxOAFinalRandomEvaluation
{
    private const string ScenePath =
        "Assets/EdgeRunner/Scenes/Training/ER_V5_ScoreMaxOA_FinalRandom.unity";
    private const string Model250kPath =
        "Assets/EdgeRunner/ML/Models/FinalCandidates/FINAL_ScoreMaxOA_FinalRandom02_250k.onnx";
    private const string Model326kPath =
        "Assets/EdgeRunner/ML/Models/FinalCandidates/FINAL_ScoreMaxOA_FinalRandom02_326k.onnx";
    private const string RunSessionKey = "EdgeRunner.ScoreMaxOA.FinalRandom02.EvaluationRunning";
    private const int TargetEpisodes = 50;
    private const int EvaluationSeed = 250002;
    private const float EvaluationTimeScale = 20f;

    static RunScoreMaxOAFinalRandomEvaluation()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("EdgeRunner/Evaluation/ObjectAware/Evaluate FinalRandom02 250k (50 Episodes)")]
    public static void RunFinalRandom02_250k_50Episodes()
    {
        RunEvaluation(
            Model250kPath,
            "FINAL_ScoreMaxOA_FinalRandom02_250k",
            "ScoreMaxOA_FinalRandom02_250k");
    }

    [MenuItem("EdgeRunner/Evaluation/ObjectAware/Evaluate FinalRandom02 326k (50 Episodes)")]
    public static void RunFinalRandom02_326k_50Episodes()
    {
        RunEvaluation(
            Model326kPath,
            "FINAL_ScoreMaxOA_FinalRandom02_326k",
            "ScoreMaxOA_FinalRandom02_326k");
    }

    private static void RunEvaluation(
        string modelPath,
        string modelLabel,
        string reportSubfolder)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new System.InvalidOperationException(
                "Cannot start the FinalRandom evaluation while Unity is entering or in Play Mode.");
        }

        ModelAsset modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(modelPath);
        if (modelAsset == null)
        {
            throw new System.IO.FileNotFoundException(
                "FinalRandom02 evaluation model was not found.",
                modelPath);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EdgeRunnerAgentV5ScoreMaxObjectAware[] agents =
            UnityEngine.Object.FindObjectsByType<EdgeRunnerAgentV5ScoreMaxObjectAware>(
                FindObjectsInactive.Exclude);
        if (agents.Length != 1)
        {
            throw new System.InvalidOperationException(
                $"FinalRandom evaluation requires exactly one ObjectAware agent; found {agents.Length}.");
        }

        EdgeRunnerAgentV5ScoreMaxObjectAware agent = agents[0];
        BehaviorParameters behavior = agent.GetComponent<BehaviorParameters>();
        ValidateObjectAwareContract(agent, behavior);
        ConfigureInferenceModel(behavior, modelAsset);

        EdgeRunnerEvaluationManager evaluationManager =
            UnityEngine.Object.FindAnyObjectByType<EdgeRunnerEvaluationManager>();
        if (evaluationManager == null)
        {
            GameObject evaluationObject = new GameObject("ScoreMaxOA_FinalRandom_Evaluation");
            SceneManager.MoveGameObjectToScene(evaluationObject, scene);
            evaluationManager = evaluationObject.AddComponent<EdgeRunnerEvaluationManager>();
        }

        ConfigureEvaluationManager(evaluationManager, agent, modelLabel, reportSubfolder);
        SessionState.SetBool(RunSessionKey, true);
        Debug.Log(
            $"[OBJECT AWARE EVALUATION] Starting {TargetEpisodes} inference episodes. " +
            $"Model='{modelPath}', Scene='{ScenePath}', Seed={EvaluationSeed}.");
        EditorApplication.isPlaying = true;
    }

    private static void ValidateObjectAwareContract(
        EdgeRunnerAgentV5ScoreMaxObjectAware agent,
        BehaviorParameters behavior)
    {
        if (agent == null || behavior == null)
        {
            throw new System.InvalidOperationException(
                "FinalRandom evaluation is missing its ObjectAware agent or BehaviorParameters.");
        }

        SerializedObject serializedAgent = new SerializedObject(agent);
        SerializedProperty phase = serializedAgent.FindProperty("objectAwarePhase");
        SerializedObject serializedBehavior = new SerializedObject(behavior);
        SerializedProperty observationSize = serializedBehavior.FindProperty(
            "m_BrainParameters.VectorObservationSize");
        SerializedProperty continuousActions = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.m_NumContinuousActions");
        SerializedProperty branchSizes = serializedBehavior.FindProperty(
            "m_BrainParameters.m_ActionSpec.BranchSizes");

        bool validBranches = branchSizes != null &&
            branchSizes.arraySize == 3 &&
            branchSizes.GetArrayElementAtIndex(0).intValue == 3 &&
            branchSizes.GetArrayElementAtIndex(1).intValue == 2 &&
            branchSizes.GetArrayElementAtIndex(2).intValue == 2;
        bool valid =
            phase != null &&
            phase.enumValueIndex == (int)EdgeRunnerObjectAwarePhase.FinalRandom &&
            behavior.BehaviorName == "EdgeRunnerV5ScoreMaxObjectAware" &&
            observationSize != null &&
            observationSize.intValue ==
                EdgeRunnerAgentV5ScoreMaxObjectAware.DefaultExpectedObservationSize &&
            continuousActions != null &&
            continuousActions.intValue == 0 &&
            validBranches;

        if (!valid)
        {
            throw new System.InvalidOperationException(
                "FinalRandom evaluation contract must be FinalRandom, 111 observations, " +
                "Behavior EdgeRunnerV5ScoreMaxObjectAware, and branches [3,2,2].");
        }
    }

    private static void ConfigureInferenceModel(
        BehaviorParameters behavior,
        ModelAsset modelAsset)
    {
        behavior.Model = modelAsset;
        behavior.BehaviorType = BehaviorType.InferenceOnly;
        if (behavior.Model != modelAsset)
        {
            throw new System.InvalidOperationException(
                "BehaviorParameters did not retain the FinalRandom02 ModelAsset reference.");
        }

        EditorUtility.SetDirty(behavior);
    }

    private static void ConfigureEvaluationManager(
        EdgeRunnerEvaluationManager evaluationManager,
        EdgeRunnerAgentV5ScoreMaxObjectAware agent,
        string modelLabel,
        string reportSubfolder)
    {
        SerializedObject serializedManager = new SerializedObject(evaluationManager);
        SetBool(serializedManager, "enableEvaluation", true);
        SetInt(serializedManager, "targetEpisodes", TargetEpisodes);
        SetBool(serializedManager, "stopPlayModeWhenFinished", true);
        SetBool(serializedManager, "saveCsvReport", true);
        SetBool(serializedManager, "saveTxtSummary", true);
        SetFloat(serializedManager, "evaluationTimeScale", EvaluationTimeScale);
        SetInt(serializedManager, "evaluationRandomSeed", EvaluationSeed);
        SetObjectReference(serializedManager, "agentV5", agent);
        SetString(
            serializedManager,
            "modelLabel",
            modelLabel);
        SetString(
            serializedManager,
            "evaluationLabel",
            "ER_V5_ScoreMaxOA_FinalRandom_Eval50");
        SetBool(serializedManager, "includeObjectAwareMetrics", true);
        SetBool(serializedManager, "saveJsonReport", true);
        SetString(serializedManager, "reportSubfolder", reportSubfolder);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(evaluationManager);
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(RunSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(RunSessionKey, false);
        if (Application.isBatchMode)
        {
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
            return;
        }

        EditorApplication.delayCall += () =>
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void SetBool(SerializedObject target, string propertyName, bool value)
    {
        SerializedProperty property = RequireProperty(target, propertyName);
        property.boolValue = value;
    }

    private static void SetInt(SerializedObject target, string propertyName, int value)
    {
        SerializedProperty property = RequireProperty(target, propertyName);
        property.intValue = value;
    }

    private static void SetFloat(SerializedObject target, string propertyName, float value)
    {
        SerializedProperty property = RequireProperty(target, propertyName);
        property.floatValue = value;
    }

    private static void SetString(SerializedObject target, string propertyName, string value)
    {
        SerializedProperty property = RequireProperty(target, propertyName);
        property.stringValue = value;
    }

    private static void SetObjectReference(
        SerializedObject target,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = RequireProperty(target, propertyName);
        property.objectReferenceValue = value;
    }

    private static SerializedProperty RequireProperty(
        SerializedObject target,
        string propertyName)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Missing serialized property '{propertyName}' on {target.targetObject.GetType().Name}.");
        }

        return property;
    }
}
