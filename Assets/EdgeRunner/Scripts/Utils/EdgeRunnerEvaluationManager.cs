using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum EdgeRunnerEpisodeEndReason
{
    Success,
    Fell,
    EnemyHit,
    NoProgress,
    Stuck,
    Timeout,
    Other,
    ObjectAwareAirborneLowCoin,
    ObjectAwareSameJumpHighCoin,
    ObjectAwareMissedCoin,
    ObjectAwareMissedEnemy,
    ObjectAwareAntiLedgeStuck,
    ObjectAwareBlockedGoal
}

public struct EdgeRunnerObjectAwareEvaluationSnapshot
{
    public bool valid;
    public bool goalReached;
    public bool allCoinsCollected;
    public bool androidStomped;
    public bool fullObjectiveSuccess;
    public int coinsCollected;
    public int coinsRemaining;
    public int enemiesKilled;
    public int enemiesRemaining;
    public int prematureGoalTouches;
}

public class EdgeRunnerEvaluationManager : MonoBehaviour
{
    [Header("Evaluation")]
    [SerializeField] private bool enableEvaluation = true;
    [SerializeField] private int targetEpisodes = 20;
    [SerializeField] private bool stopPlayModeWhenFinished = true;
    [SerializeField] private bool saveCsvReport = true;
    [SerializeField] private bool saveTxtSummary = true;
    [SerializeField] private float evaluationTimeScale = 3f;
    [SerializeField] private int evaluationRandomSeed = -1;

    [Header("Optional ObjectAware Report")]
    [SerializeField] private bool includeObjectAwareMetrics = false;
    [SerializeField] private bool saveJsonReport = false;
    [SerializeField] private string reportSubfolder = string.Empty;

    [Header("References")]
    [SerializeField] private EdgeRunnerAgentV3 agent;
    [SerializeField] private EdgeRunnerAgentV5 agentV5;
    [SerializeField] private EdgeRunnerAgentV5EnemyAware agentV5EnemyAware;
    [SerializeField] private EdgeRunnerAgentV5EnemiesTransfer agentV5EnemiesTransfer;
    [SerializeField] private GapGenerator gapGenerator;
    [SerializeField] private MixedLevelGenerator mixedLevelGenerator;

    [Header("Labels")]
    [SerializeField] private string modelLabel = "UnnamedModel";
    [SerializeField] private string evaluationLabel = "UnnamedEvaluation";

    private readonly List<EpisodeRecord> episodeRecords = new List<EpisodeRecord>();

    private bool evaluationStarted;
    private bool evaluationFinished;
    private bool currentEpisodeOpen;
    private bool currentEpisodeClosed;
    private bool stopRequested;

    private int currentEpisodeIndex;
    private float episodeStartTime;
    private Vector3 episodeStartPosition;
    private float maxXReached;
    private Agent activeAgent;
    private bool loggedAgentSelection;
    private bool loggedMultipleAgentWarning;

    private void Awake()
    {
        if (evaluationRandomSeed >= 0)
        {
            UnityEngine.Random.InitState(evaluationRandomSeed);
        }
    }

    private void Start()
    {
        if (!enableEvaluation)
        {
            return;
        }

        EnsureAgentReference();
        EnsureMixedLevelGeneratorReference();
        EnsureGapGeneratorReference();
        StartEvaluationIfNeeded();
    }

    private void Update()
    {
        if (!enableEvaluation)
        {
            return;
        }

        Agent currentAgent = GetActiveAgent();

        if (currentEpisodeOpen && !currentEpisodeClosed && currentAgent != null)
        {
            maxXReached = Mathf.Max(maxXReached, currentAgent.transform.position.x);
        }

        if (stopRequested)
        {
            StopPlayModeOrQuit();
        }
    }

    public void NotifyEpisodeStarted(EdgeRunnerAgentV3 sourceAgent)
    {
        NotifyEpisodeStartedInternal(sourceAgent);
    }

    public void NotifyEpisodeStarted(EdgeRunnerAgentV5 sourceAgent)
    {
        NotifyEpisodeStartedInternal(sourceAgent);
    }

    public void NotifyEpisodeStarted(EdgeRunnerAgentV5EnemyAware sourceAgent)
    {
        NotifyEpisodeStartedInternal(sourceAgent);
    }

    public void NotifyEpisodeStarted(EdgeRunnerAgentV5EnemiesTransfer sourceAgent)
    {
        NotifyEpisodeStartedInternal(sourceAgent);
    }

    private void NotifyEpisodeStartedInternal(Agent sourceAgent)
    {
        if (!enableEvaluation || evaluationFinished || sourceAgent == null)
        {
            return;
        }

        RegisterSourceAgent(sourceAgent);

        if (activeAgent != sourceAgent)
        {
            return;
        }

        StartEvaluationIfNeeded();

        if (currentEpisodeOpen && !currentEpisodeClosed)
        {
            NotifyEpisodeEndedInternal(sourceAgent, EdgeRunnerEpisodeEndReason.Other);

            if (evaluationFinished)
            {
                return;
            }
        }

        if (episodeRecords.Count >= targetEpisodes)
        {
            FinishEvaluation();
            return;
        }

        currentEpisodeIndex = episodeRecords.Count + 1;
        episodeStartTime = Time.time;
        episodeStartPosition = sourceAgent.transform.position;
        maxXReached = episodeStartPosition.x;
        currentEpisodeOpen = true;
        currentEpisodeClosed = false;

        Debug.Log($"Episode {currentEpisodeIndex}/{targetEpisodes} started.");
    }

    public void NotifyEpisodeEnded(EdgeRunnerAgentV3 sourceAgent, EdgeRunnerEpisodeEndReason reason)
    {
        NotifyEpisodeEndedInternal(sourceAgent, reason);
    }

    public void NotifyEpisodeEnded(EdgeRunnerAgentV5 sourceAgent, EdgeRunnerEpisodeEndReason reason)
    {
        NotifyEpisodeEndedInternal(sourceAgent, reason);
    }

    public void NotifyEpisodeEnded(EdgeRunnerAgentV5EnemyAware sourceAgent, EdgeRunnerEpisodeEndReason reason)
    {
        NotifyEpisodeEndedInternal(sourceAgent, reason);
    }

    public void NotifyEpisodeEnded(EdgeRunnerAgentV5EnemiesTransfer sourceAgent, EdgeRunnerEpisodeEndReason reason)
    {
        NotifyEpisodeEndedInternal(sourceAgent, reason);
    }

    private void NotifyEpisodeEndedInternal(Agent sourceAgent, EdgeRunnerEpisodeEndReason reason)
    {
        if (!enableEvaluation || evaluationFinished || sourceAgent == null)
        {
            return;
        }

        RegisterSourceAgent(sourceAgent);

        if (activeAgent != sourceAgent || !currentEpisodeOpen || currentEpisodeClosed)
        {
            return;
        }

        Vector3 finalPosition = sourceAgent.transform.position;
        float duration = Mathf.Max(0f, Time.time - episodeStartTime);

        if (reason == EdgeRunnerEpisodeEndReason.Fell && duration < 0.05f)
        {
            currentEpisodeOpen = false;
            currentEpisodeClosed = true;
            Debug.LogWarning("Ignored suspicious instant Fell episode.");
            return;
        }

        bool instantBlockedGoalAtSpawn =
            reason == EdgeRunnerEpisodeEndReason.ObjectAwareBlockedGoal &&
            duration < 0.05f &&
            Mathf.Abs(finalPosition.x - episodeStartPosition.x) < 0.1f;
        if (instantBlockedGoalAtSpawn)
        {
            currentEpisodeOpen = false;
            currentEpisodeClosed = true;
            Debug.LogWarning(
                "Ignored duplicate Goal callback immediately after an ObjectAware episode reset.");
            return;
        }

        float reward = sourceAgent.GetCumulativeReward();
        bool success = reason == EdgeRunnerEpisodeEndReason.Success;
        NearestGapSnapshot nearestGap = FindNearestGap(finalPosition.x);
        NearestSegmentSnapshot nearestSegment = FindNearestSegment(finalPosition.x);
        SegmentContextSnapshot segmentContext = FindSegmentContext(finalPosition.x, nearestSegment);
        SegmentGenerationStatsSnapshot generationStats = FindSegmentGenerationStats();
        bool groundedAtEnd = IsAgentGroundedForEvaluation(sourceAgent);
        Vector2 velocityAtEnd = GetAgentVelocityForEvaluation(sourceAgent);
        EdgeRunnerObjectAwareEvaluationSnapshot objectAwareSnapshot =
            CaptureObjectAwareSnapshot(sourceAgent, success);

        maxXReached = Mathf.Max(maxXReached, finalPosition.x);

        episodeRecords.Add(new EpisodeRecord
        {
            episodeIndex = currentEpisodeIndex,
            reason = reason,
            reward = reward,
            duration = duration,
            startX = episodeStartPosition.x,
            finalX = finalPosition.x,
            maxXReached = maxXReached,
            success = success,
            nearestGapIndex = nearestGap.index,
            nearestGapStartX = nearestGap.startX,
            nearestGapEndX = nearestGap.endX,
            nearestGapWidth = nearestGap.width,
            distanceToGapStart = nearestGap.distanceToStart,
            distanceToGapEnd = nearestGap.distanceToEnd,
            nearestSegmentIndex = nearestSegment.index,
            nearestSegmentType = nearestSegment.type,
            nearestSegmentStartX = nearestSegment.startX,
            nearestSegmentEndX = nearestSegment.endX,
            nearestSegmentY = nearestSegment.y,
            distanceToSegmentStart = nearestSegment.distanceToStart,
            distanceToSegmentEnd = nearestSegment.distanceToEnd,
            previousSegmentIndex = segmentContext.previousIndex,
            previousSegmentType = segmentContext.previousType,
            currentSegmentIndex = segmentContext.currentIndex,
            currentSegmentType = segmentContext.currentType,
            nextSegmentIndex = segmentContext.nextIndex,
            nextSegmentType = segmentContext.nextType,
            distanceToCurrentSegmentEnd = segmentContext.distanceToCurrentEnd,
            distanceToNextSegmentStart = segmentContext.distanceToNextStart,
            flatSegmentCount = generationStats.flatSegmentCount,
            gapSegmentCount = generationStats.gapSegmentCount,
            stepUpSegmentCount = generationStats.stepUpSegmentCount,
            stepDownSegmentCount = generationStats.stepDownSegmentCount,
            safeDropSegmentCount = generationStats.safeDropSegmentCount,
            platformChainSegmentCount = generationStats.platformChainSegmentCount,
            platformChainGroupCount = generationStats.platformChainGroupCount,
            platformChainInternalGapWidthTotal = generationStats.platformChainInternalGapWidthTotal,
            platformChainInternalGapSampleCount = generationStats.platformChainInternalGapSampleCount,
            stepUpHeightDeltaTotal = generationStats.stepUpHeightDeltaTotal,
            stepUpHeightDeltaSampleCount = generationStats.stepUpHeightDeltaSampleCount,
            maxStepUpHeightDelta = generationStats.maxStepUpHeightDelta,
            groundedAtEnd = groundedAtEnd,
            velocityXAtEnd = velocityAtEnd.x,
            velocityYAtEnd = velocityAtEnd.y,
            objectAwareSnapshotValid = objectAwareSnapshot.valid,
            goalReached = objectAwareSnapshot.goalReached,
            allCoinsCollected = objectAwareSnapshot.allCoinsCollected,
            androidStomped = objectAwareSnapshot.androidStomped,
            fullObjectiveSuccess = objectAwareSnapshot.fullObjectiveSuccess,
            coinsCollected = objectAwareSnapshot.coinsCollected,
            coinsRemaining = objectAwareSnapshot.coinsRemaining,
            enemiesKilled = objectAwareSnapshot.enemiesKilled,
            enemiesRemaining = objectAwareSnapshot.enemiesRemaining,
            prematureGoalTouches = objectAwareSnapshot.prematureGoalTouches
        });

        currentEpisodeOpen = false;
        currentEpisodeClosed = true;

        Debug.Log(
            $"Episode {currentEpisodeIndex}/{targetEpisodes} ended: " +
            $"{(success ? "Success" : "Failure")} ({reason}). " +
            $"Completed episodes: {episodeRecords.Count}/{targetEpisodes}. " +
            $"reward={reward:F3}, duration={duration:F2}s, maxX={maxXReached:F2}"
        );

        if (episodeRecords.Count >= targetEpisodes)
        {
            FinishEvaluation();
        }
    }

    private void StartEvaluationIfNeeded()
    {
        if (evaluationStarted)
        {
            return;
        }

        targetEpisodes = Mathf.Max(1, targetEpisodes);
        Time.timeScale = Mathf.Max(0.01f, evaluationTimeScale);
        evaluationStarted = true;

        Debug.Log(
            $"Evaluation started. Target episodes: {targetEpisodes}. " +
            $"Model='{modelLabel}', Evaluation='{evaluationLabel}'."
        );
    }

    private void FinishEvaluation()
    {
        if (evaluationFinished)
        {
            return;
        }

        evaluationFinished = true;
        currentEpisodeOpen = false;
        currentEpisodeClosed = true;

        string summary = BuildSummary();
        Debug.Log(summary);

        LogIgnoredReportToggleWarnings();
        SaveReports(summary);

        if (stopPlayModeWhenFinished)
        {
            stopRequested = true;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private string BuildSummary()
    {
        int total = episodeRecords.Count;
        int successes = CountReason(EdgeRunnerEpisodeEndReason.Success);
        int failures = total - successes;
        float successRate = total > 0 ? successes * 100f / total : 0f;

        float averageReward = Average(record => record.reward);
        float averageDuration = Average(record => record.duration);
        float averageFinalX = Average(record => record.finalX);
        float averageMaxX = Average(record => record.maxXReached);
        float averageFailureFinalX = AverageFailure(record => record.finalX);
        float averageNearestFailedGapWidth = AverageFailureWithGap(record => record.nearestGapWidth);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("=== EdgeRunner Evaluation Report ===");
        builder.AppendLine($"Model: {modelLabel}");
        builder.AppendLine($"Evaluation: {evaluationLabel}");
        builder.AppendLine($"Episodes: {total}");
        builder.AppendLine($"Successes: {successes}");
        builder.AppendLine($"Failures: {failures}");
        builder.AppendLine($"Success Rate: {successRate:F1}%");
        builder.AppendLine("Failures:");
        builder.AppendLine($"- Fell: {CountReason(EdgeRunnerEpisodeEndReason.Fell)}");
        builder.AppendLine($"- EnemyHit: {CountReason(EdgeRunnerEpisodeEndReason.EnemyHit)}");
        builder.AppendLine($"- NoProgress: {CountReason(EdgeRunnerEpisodeEndReason.NoProgress)}");
        builder.AppendLine($"- Timeout: {CountReason(EdgeRunnerEpisodeEndReason.Timeout)}");
        builder.AppendLine($"- Stuck: {CountReason(EdgeRunnerEpisodeEndReason.Stuck)}");
        builder.AppendLine($"- Other: {CountReason(EdgeRunnerEpisodeEndReason.Other)}");
        builder.AppendLine($"Average Reward: {averageReward:F3}");
        builder.AppendLine($"Average Duration: {averageDuration:F2}s");
        if (includeObjectAwareMetrics)
        {
            int fullObjectiveSuccesses = CountRecords(record => record.fullObjectiveSuccess);
            float fullObjectiveSuccessRate = total > 0
                ? fullObjectiveSuccesses * 100f / total
                : 0f;
            builder.AppendLine("ObjectAware FinalRandom Metrics:");
            builder.AppendLine($"- Full Objective Successes: {fullObjectiveSuccesses}");
            builder.AppendLine($"- Full Objective Success Rate: {fullObjectiveSuccessRate:F1}%");
            builder.AppendLine($"- Goal Reached: {CountRecords(record => record.goalReached)}");
            builder.AppendLine($"- All Coins Collected: {CountRecords(record => record.allCoinsCollected)}");
            builder.AppendLine($"- Android Stomped: {CountRecords(record => record.androidStomped)}");
            builder.AppendLine($"- Gap/DeathZone Failures: {CountReason(EdgeRunnerEpisodeEndReason.Fell)}");
            builder.AppendLine($"- Anti Ledge Stuck Failures: {CountReason(EdgeRunnerEpisodeEndReason.ObjectAwareAntiLedgeStuck)}");
            builder.AppendLine($"- Blocked Goal Failures: {CountReason(EdgeRunnerEpisodeEndReason.ObjectAwareBlockedGoal)}");
            builder.AppendLine($"- Timeout Failures: {CountReason(EdgeRunnerEpisodeEndReason.Timeout)}");
        }
        builder.AppendLine($"Average Final X: {averageFinalX:F2}");
        builder.AppendLine($"Average Max X: {averageMaxX:F2}");
        builder.AppendLine($"Average Failure Final X: {averageFailureFinalX:F2}");
        builder.AppendLine("Failures By Nearest Gap Index:");
        AppendFailuresByNearestGapIndex(builder);
        builder.AppendLine("Failures By Nearest Segment Type:");
        AppendFailuresByNearestSegmentType(builder);
        builder.AppendLine("Failures By Nearest Segment Index:");
        AppendFailuresByNearestSegmentIndex(builder);
        builder.AppendLine("Failures By Current Segment Type:");
        AppendFailuresByCurrentSegmentType(builder);
        builder.AppendLine("Failures By Next Segment Type:");
        AppendFailuresByNextSegmentType(builder);
        builder.AppendLine("Failures By Transition:");
        AppendFailuresByTransition(builder);
        builder.AppendLine($"Average Nearest Failed Gap Width: {averageNearestFailedGapWidth:F2}");
        AppendSegmentGenerationStatistics(builder);
        AppendRuntimeConfigurationSnapshot(builder);

        return builder.ToString();
    }

    private void SaveReports(string summary)
    {
        string folder = GetReportFolder();
        if (!string.IsNullOrWhiteSpace(reportSubfolder))
        {
            folder = Path.Combine(folder, SanitizeFileName(reportSubfolder));
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string safeModelLabel = SanitizeFileName(modelLabel);
        string safeEvaluationLabel = SanitizeFileName(evaluationLabel);
        string baseName = $"EdgeRunnerEval_{timestamp}_{safeModelLabel}_{safeEvaluationLabel}";
        string txtPath = Path.Combine(folder, baseName + ".txt");
        string csvPath = Path.Combine(folder, baseName + ".csv");
        string jsonPath = Path.Combine(folder, baseName + ".json");

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(txtPath, summary, Encoding.UTF8);
            Debug.Log("TXT saved to: " + txtPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save TXT summary to: {txtPath}\n{exception}");
        }

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(csvPath, BuildCsv(), Encoding.UTF8);
            Debug.Log("CSV saved to: " + csvPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save CSV report to: {csvPath}\n{exception}");
        }

        if (!saveJsonReport)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(jsonPath, BuildJson(), Encoding.UTF8);
            Debug.Log("JSON saved to: " + jsonPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save JSON report to: {jsonPath}\n{exception}");
        }
    }

    private void LogIgnoredReportToggleWarnings()
    {
        if (saveTxtSummary && saveCsvReport)
        {
            return;
        }

        Debug.LogWarning(
            "EdgeRunnerEvaluationManager: saveTxtSummary/saveCsvReport toggles are ignored at evaluation finish; " +
            "TXT and CSV reports are always attempted."
        );
    }

    private string BuildCsv()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("episodeIndex,result,reason,reward,duration,startX,finalX,maxXReached,success,nearestGapIndex,nearestGapStartX,nearestGapEndX,nearestGapWidth,distanceToGapStart,distanceToGapEnd,nearestSegmentIndex,nearestSegmentType,nearestSegmentStartX,nearestSegmentEndX,nearestSegmentY,distanceToSegmentStart,distanceToSegmentEnd,previousSegmentIndex,previousSegmentType,currentSegmentIndex,currentSegmentType,nextSegmentIndex,nextSegmentType,distanceToCurrentSegmentEnd,distanceToNextSegmentStart,groundedAtEnd,velocityXAtEnd,velocityYAtEnd");
        if (includeObjectAwareMetrics)
        {
            builder.Append(",goalReached,allCoinsCollected,androidStomped,fullObjectiveSuccess,coinsCollected,coinsRemaining,enemiesKilled,enemiesRemaining,prematureGoalTouches");
        }
        builder.AppendLine();

        foreach (EpisodeRecord record in episodeRecords)
        {
            string result = record.success ? "Success" : "Failure";

            builder.Append(record.episodeIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(result);
            builder.Append(',');
            builder.Append(record.reason);
            builder.Append(',');
            builder.Append(record.reward.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.duration.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.startX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.finalX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.maxXReached.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.success ? "true" : "false");
            builder.Append(',');
            builder.Append(record.nearestGapIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestGapStartX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestGapEndX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestGapWidth.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.distanceToGapStart.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.distanceToGapEnd.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestSegmentIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(SanitizeCsvValue(record.nearestSegmentType));
            builder.Append(',');
            builder.Append(record.nearestSegmentStartX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestSegmentEndX.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.nearestSegmentY.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.distanceToSegmentStart.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.distanceToSegmentEnd.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.previousSegmentIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(SanitizeCsvValue(record.previousSegmentType));
            builder.Append(',');
            builder.Append(record.currentSegmentIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(SanitizeCsvValue(record.currentSegmentType));
            builder.Append(',');
            builder.Append(record.nextSegmentIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(SanitizeCsvValue(record.nextSegmentType));
            builder.Append(',');
            builder.Append(record.distanceToCurrentSegmentEnd.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.distanceToNextSegmentStart.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.groundedAtEnd ? "true" : "false");
            builder.Append(',');
            builder.Append(record.velocityXAtEnd.ToString("F6", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(record.velocityYAtEnd.ToString("F6", CultureInfo.InvariantCulture));
            if (includeObjectAwareMetrics)
            {
                builder.Append(',');
                builder.Append(record.goalReached ? "true" : "false");
                builder.Append(',');
                builder.Append(record.allCoinsCollected ? "true" : "false");
                builder.Append(',');
                builder.Append(record.androidStomped ? "true" : "false");
                builder.Append(',');
                builder.Append(record.fullObjectiveSuccess ? "true" : "false");
                builder.Append(',');
                builder.Append(record.coinsCollected.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(record.coinsRemaining.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(record.enemiesKilled.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(record.enemiesRemaining.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(record.prematureGoalTouches.ToString(CultureInfo.InvariantCulture));
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string BuildJson()
    {
        int total = episodeRecords.Count;
        int successes = CountReason(EdgeRunnerEpisodeEndReason.Success);
        int fullObjectiveSuccesses = CountRecords(record => record.fullObjectiveSuccess);
        float successRate = total > 0 ? successes * 100f / total : 0f;
        float fullObjectiveSuccessRate = total > 0
            ? fullObjectiveSuccesses * 100f / total
            : 0f;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        AppendJsonStringProperty(builder, "model", modelLabel, true, 2);
        AppendJsonStringProperty(builder, "evaluation", evaluationLabel, true, 2);
        AppendJsonNumberProperty(builder, "evaluationRandomSeed", evaluationRandomSeed, true, 2);
        AppendJsonNumberProperty(builder, "totalEpisodes", total, true, 2);
        AppendJsonNumberProperty(builder, "successRate", successRate, true, 2);
        AppendJsonNumberProperty(
            builder,
            "fullObjectiveSuccessRate",
            fullObjectiveSuccessRate,
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "goalReachedCount",
            CountRecords(record => record.goalReached),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "allCoinsCollectedCount",
            CountRecords(record => record.allCoinsCollected),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "androidStompedCount",
            CountRecords(record => record.androidStomped),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "deathsByGapOrDeathZone",
            CountReason(EdgeRunnerEpisodeEndReason.Fell),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "antiLedgeStuckFailures",
            CountReason(EdgeRunnerEpisodeEndReason.ObjectAwareAntiLedgeStuck),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "blockedGoalFailures",
            CountReason(EdgeRunnerEpisodeEndReason.ObjectAwareBlockedGoal),
            true,
            2);
        AppendJsonNumberProperty(
            builder,
            "timeoutFailures",
            CountReason(EdgeRunnerEpisodeEndReason.Timeout),
            true,
            2);
        AppendJsonNumberProperty(builder, "averageEpisodeTime", Average(record => record.duration), true, 2);
        AppendJsonNumberProperty(builder, "averageReward", Average(record => record.reward), true, 2);
        builder.AppendLine("  \"episodes\": [");

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];
            builder.AppendLine("    {");
            AppendJsonNumberProperty(builder, "episodeIndex", record.episodeIndex, true, 6);
            AppendJsonStringProperty(builder, "reason", record.reason.ToString(), true, 6);
            AppendJsonBooleanProperty(builder, "success", record.success, true, 6);
            AppendJsonNumberProperty(builder, "reward", record.reward, true, 6);
            AppendJsonNumberProperty(builder, "duration", record.duration, true, 6);
            AppendJsonBooleanProperty(builder, "goalReached", record.goalReached, true, 6);
            AppendJsonBooleanProperty(
                builder,
                "allCoinsCollected",
                record.allCoinsCollected,
                true,
                6);
            AppendJsonBooleanProperty(builder, "androidStomped", record.androidStomped, true, 6);
            AppendJsonBooleanProperty(
                builder,
                "fullObjectiveSuccess",
                record.fullObjectiveSuccess,
                true,
                6);
            AppendJsonNumberProperty(builder, "coinsCollected", record.coinsCollected, true, 6);
            AppendJsonNumberProperty(builder, "coinsRemaining", record.coinsRemaining, true, 6);
            AppendJsonNumberProperty(builder, "enemiesKilled", record.enemiesKilled, true, 6);
            AppendJsonNumberProperty(builder, "enemiesRemaining", record.enemiesRemaining, false, 6);
            builder.Append("    }");
            builder.AppendLine(i + 1 < episodeRecords.Count ? "," : string.Empty);
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private void AppendJsonStringProperty(
        StringBuilder builder,
        string name,
        string value,
        bool trailingComma,
        int indent)
    {
        builder.Append(' ', indent);
        builder.Append('"');
        builder.Append(EscapeJson(name));
        builder.Append("\": \"");
        builder.Append(EscapeJson(value));
        builder.Append('"');
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private void AppendJsonNumberProperty(
        StringBuilder builder,
        string name,
        float value,
        bool trailingComma,
        int indent)
    {
        builder.Append(' ', indent);
        builder.Append('"');
        builder.Append(EscapeJson(name));
        builder.Append("\": ");
        builder.Append(value.ToString("0.######", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private void AppendJsonBooleanProperty(
        StringBuilder builder,
        string name,
        bool value,
        bool trailingComma,
        int indent)
    {
        builder.Append(' ', indent);
        builder.Append('"');
        builder.Append(EscapeJson(name));
        builder.Append("\": ");
        builder.Append(value ? "true" : "false");
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private string SanitizeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(",") && !value.Contains("\"") && !value.Contains("\n") && !value.Contains("\r"))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private string GetReportFolder()
    {
#if UNITY_EDITOR
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, "EvaluationReports");
#else
        return Path.Combine(Application.persistentDataPath, "EvaluationReports");
#endif
    }

    private string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unlabeled";
        }

        string sanitized = value.Trim().Replace(' ', '_');
        char[] invalidChars = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalidChars.Length; i++)
        {
            sanitized = sanitized.Replace(invalidChars[i], '_');
        }

        return sanitized;
    }

    private int CountReason(EdgeRunnerEpisodeEndReason reason)
    {
        int count = 0;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            if (episodeRecords[i].reason == reason)
            {
                count++;
            }
        }

        return count;
    }

    private int CountRecords(Func<EpisodeRecord, bool> predicate)
    {
        int count = 0;
        for (int i = 0; i < episodeRecords.Count; i++)
        {
            if (predicate(episodeRecords[i]))
            {
                count++;
            }
        }

        return count;
    }

    private NearestGapSnapshot FindNearestGap(float finalX)
    {
        EnsureMixedLevelGeneratorReference();
        EnsureGapGeneratorReference();

        if (mixedLevelGenerator != null && mixedLevelGenerator.CurrentGaps != null && mixedLevelGenerator.CurrentGaps.Count > 0)
        {
            MixedGapInfo nearestMixedGap = mixedLevelGenerator.CurrentGaps[0];
            float nearestMixedDistance = GetDistanceToGap(finalX, nearestMixedGap.startX, nearestMixedGap.endX);

            for (int i = 1; i < mixedLevelGenerator.CurrentGaps.Count; i++)
            {
                MixedGapInfo candidate = mixedLevelGenerator.CurrentGaps[i];
                float distance = GetDistanceToGap(finalX, candidate.startX, candidate.endX);

                if (distance < nearestMixedDistance)
                {
                    nearestMixedGap = candidate;
                    nearestMixedDistance = distance;
                }
            }

            return new NearestGapSnapshot
            {
                index = nearestMixedGap.index,
                startX = nearestMixedGap.startX,
                endX = nearestMixedGap.endX,
                width = nearestMixedGap.width,
                distanceToStart = Mathf.Abs(finalX - nearestMixedGap.startX),
                distanceToEnd = Mathf.Abs(finalX - nearestMixedGap.endX)
            };
        }

        if (gapGenerator == null || gapGenerator.CurrentGaps == null || gapGenerator.CurrentGaps.Count == 0)
        {
            return NearestGapSnapshot.Empty;
        }

        GeneratedGapInfo nearestGap = gapGenerator.CurrentGaps[0];
        float nearestDistance = GetDistanceToGap(finalX, nearestGap.startX, nearestGap.endX);

        for (int i = 1; i < gapGenerator.CurrentGaps.Count; i++)
        {
            GeneratedGapInfo candidate = gapGenerator.CurrentGaps[i];
            float distance = GetDistanceToGap(finalX, candidate.startX, candidate.endX);

            if (distance < nearestDistance)
            {
                nearestGap = candidate;
                nearestDistance = distance;
            }
        }

        return new NearestGapSnapshot
        {
            index = nearestGap.index,
            startX = nearestGap.startX,
            endX = nearestGap.endX,
            width = nearestGap.width,
            distanceToStart = Mathf.Abs(finalX - nearestGap.startX),
            distanceToEnd = Mathf.Abs(finalX - nearestGap.endX)
        };
    }

    private float GetDistanceToGap(float x, float gapStartX, float gapEndX)
    {
        if (x < gapStartX)
        {
            return gapStartX - x;
        }

        if (x > gapEndX)
        {
            return x - gapEndX;
        }

        return 0f;
    }

    private NearestSegmentSnapshot FindNearestSegment(float finalX)
    {
        EnsureMixedLevelGeneratorReference();

        if (mixedLevelGenerator == null ||
            mixedLevelGenerator.CurrentSegments == null ||
            mixedLevelGenerator.CurrentSegments.Count == 0)
        {
            return NearestSegmentSnapshot.Empty;
        }

        MixedSegmentInfo nearestSegment = mixedLevelGenerator.CurrentSegments[0];
        float nearestDistance = GetDistanceToRange(finalX, nearestSegment.startX, nearestSegment.endX);

        for (int i = 1; i < mixedLevelGenerator.CurrentSegments.Count; i++)
        {
            MixedSegmentInfo candidate = mixedLevelGenerator.CurrentSegments[i];
            float distance = GetDistanceToRange(finalX, candidate.startX, candidate.endX);

            if (distance < nearestDistance)
            {
                nearestSegment = candidate;
                nearestDistance = distance;
            }
        }

        return new NearestSegmentSnapshot
        {
            index = nearestSegment.index,
            type = nearestSegment.type,
            startX = nearestSegment.startX,
            endX = nearestSegment.endX,
            y = nearestSegment.y,
            distanceToStart = Mathf.Abs(finalX - nearestSegment.startX),
            distanceToEnd = Mathf.Abs(finalX - nearestSegment.endX)
        };
    }

    private SegmentContextSnapshot FindSegmentContext(float finalX, NearestSegmentSnapshot nearestSegment)
    {
        EnsureMixedLevelGeneratorReference();

        if (mixedLevelGenerator == null ||
            mixedLevelGenerator.CurrentSegments == null ||
            mixedLevelGenerator.CurrentSegments.Count == 0)
        {
            return SegmentContextSnapshot.Empty;
        }

        IReadOnlyList<MixedSegmentInfo> segments = mixedLevelGenerator.CurrentSegments;
        int currentListIndex = FindContainingSegmentListIndex(segments, finalX);

        if (currentListIndex < 0)
        {
            currentListIndex = FindSegmentListIndex(segments, nearestSegment.index);
        }

        if (currentListIndex < 0)
        {
            return SegmentContextSnapshot.Empty;
        }

        MixedSegmentInfo currentSegment = segments[currentListIndex];
        SegmentContextSnapshot snapshot = SegmentContextSnapshot.Empty;
        snapshot.currentIndex = currentSegment.index;
        snapshot.currentType = currentSegment.type;
        snapshot.distanceToCurrentEnd = Mathf.Abs(finalX - currentSegment.endX);

        int previousListIndex = currentListIndex - 1;
        if (previousListIndex >= 0)
        {
            MixedSegmentInfo previousSegment = segments[previousListIndex];
            snapshot.previousIndex = previousSegment.index;
            snapshot.previousType = previousSegment.type;
        }

        int nextListIndex = currentListIndex + 1;
        if (nextListIndex < segments.Count)
        {
            MixedSegmentInfo nextSegment = segments[nextListIndex];
            snapshot.nextIndex = nextSegment.index;
            snapshot.nextType = nextSegment.type;
            snapshot.distanceToNextStart = Mathf.Abs(finalX - nextSegment.startX);
        }

        return snapshot;
    }

    private int FindContainingSegmentListIndex(IReadOnlyList<MixedSegmentInfo> segments, float finalX)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (SegmentContainsX(segments[i], finalX))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindSegmentListIndex(IReadOnlyList<MixedSegmentInfo> segments, int segmentIndex)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].index == segmentIndex)
            {
                return i;
            }
        }

        return -1;
    }

    private bool SegmentContainsX(MixedSegmentInfo segment, float x)
    {
        float minX = Mathf.Min(segment.startX, segment.endX);
        float maxX = Mathf.Max(segment.startX, segment.endX);
        return x >= minX && x <= maxX;
    }

    private SegmentGenerationStatsSnapshot FindSegmentGenerationStats()
    {
        EnsureMixedLevelGeneratorReference();

        if (mixedLevelGenerator == null ||
            mixedLevelGenerator.CurrentSegments == null ||
            mixedLevelGenerator.CurrentSegments.Count == 0)
        {
            return SegmentGenerationStatsSnapshot.Empty;
        }

        IReadOnlyList<MixedSegmentInfo> segments = mixedLevelGenerator.CurrentSegments;
        SegmentGenerationStatsSnapshot stats = SegmentGenerationStatsSnapshot.Empty;

        for (int i = 0; i < segments.Count; i++)
        {
            MixedSegmentInfo segment = segments[i];

            switch (segment.type)
            {
                case "FlatSegment":
                    stats.flatSegmentCount++;
                    break;
                case "GapSegment":
                    stats.gapSegmentCount++;
                    break;
                case "StepUpSegment":
                    stats.stepUpSegmentCount++;
                    AddStepUpHeightDelta(segments, i, ref stats);
                    break;
                case "StepDownSegment":
                    stats.stepDownSegmentCount++;
                    break;
                case "SafeDropSegment":
                    stats.safeDropSegmentCount++;
                    break;
                case "PlatformChainSegment":
                    stats.platformChainSegmentCount++;
                    AddPlatformChainStats(segments, i, ref stats);
                    break;
            }
        }

        return stats;
    }

    private void AddPlatformChainStats(
        IReadOnlyList<MixedSegmentInfo> segments,
        int platformChainListIndex,
        ref SegmentGenerationStatsSnapshot stats)
    {
        if (platformChainListIndex <= 0 ||
            !string.Equals(segments[platformChainListIndex - 1].type, "PlatformChainSegment", StringComparison.Ordinal))
        {
            stats.platformChainGroupCount++;
            return;
        }

        float internalGapWidth = segments[platformChainListIndex].startX -
            segments[platformChainListIndex - 1].endX;
        stats.platformChainInternalGapWidthTotal += Mathf.Max(0f, internalGapWidth);
        stats.platformChainInternalGapSampleCount++;
    }

    private void AddStepUpHeightDelta(
        IReadOnlyList<MixedSegmentInfo> segments,
        int stepUpListIndex,
        ref SegmentGenerationStatsSnapshot stats)
    {
        for (int i = stepUpListIndex - 1; i >= 0; i--)
        {
            MixedSegmentInfo previous = segments[i];

            if (string.Equals(previous.type, "GoalSegment", StringComparison.Ordinal))
            {
                continue;
            }

            float heightDelta = segments[stepUpListIndex].y - previous.y;
            stats.stepUpHeightDeltaTotal += heightDelta;
            stats.stepUpHeightDeltaSampleCount++;
            stats.maxStepUpHeightDelta = stats.stepUpHeightDeltaSampleCount == 1
                ? heightDelta
                : Mathf.Max(stats.maxStepUpHeightDelta, heightDelta);
            return;
        }
    }

    private float GetDistanceToRange(float x, float startX, float endX)
    {
        float minX = Mathf.Min(startX, endX);
        float maxX = Mathf.Max(startX, endX);

        if (x < minX)
        {
            return minX - x;
        }

        if (x > maxX)
        {
            return x - maxX;
        }

        return 0f;
    }

    private float Average(Func<EpisodeRecord, float> selector)
    {
        if (episodeRecords.Count == 0)
        {
            return 0f;
        }

        float total = 0f;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            total += selector(episodeRecords[i]);
        }

        return total / episodeRecords.Count;
    }

    private float AverageFailure(Func<EpisodeRecord, float> selector)
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            if (!episodeRecords[i].success)
            {
                total += selector(episodeRecords[i]);
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float AverageFailureWithGap(Func<EpisodeRecord, float> selector)
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            if (!episodeRecords[i].success && episodeRecords[i].nearestGapIndex >= 0)
            {
                total += selector(episodeRecords[i]);
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float AverageStepUpHeightDelta()
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            total += episodeRecords[i].stepUpHeightDeltaTotal;
            count += episodeRecords[i].stepUpHeightDeltaSampleCount;
        }

        return count > 0 ? total / count : 0f;
    }

    private float MaxStepUpHeightDelta()
    {
        bool found = false;
        float max = 0f;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            if (episodeRecords[i].stepUpHeightDeltaSampleCount <= 0)
            {
                continue;
            }

            max = found ? Mathf.Max(max, episodeRecords[i].maxStepUpHeightDelta) : episodeRecords[i].maxStepUpHeightDelta;
            found = true;
        }

        return found ? max : 0f;
    }

    private float AveragePlatformChainPiecesPerChain()
    {
        float pieces = 0f;
        float chains = 0f;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            pieces += episodeRecords[i].platformChainSegmentCount;
            chains += episodeRecords[i].platformChainGroupCount;
        }

        return chains > 0f ? pieces / chains : 0f;
    }

    private float AveragePlatformChainInternalGapWidth()
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            total += episodeRecords[i].platformChainInternalGapWidthTotal;
            count += episodeRecords[i].platformChainInternalGapSampleCount;
        }

        return count > 0 ? total / count : 0f;
    }

    private void AppendSegmentGenerationStatistics(StringBuilder builder)
    {
        builder.AppendLine("Segment Generation Statistics:");
        builder.AppendLine($"Average StepUpSegment Count Per Episode: {Average(record => record.stepUpSegmentCount):F2}");
        builder.AppendLine($"Average StepDownSegment Count Per Episode: {Average(record => record.stepDownSegmentCount):F2}");
        builder.AppendLine($"Average SafeDropSegment Count Per Episode: {Average(record => record.safeDropSegmentCount):F2}");
        builder.AppendLine($"Average GapSegment Count Per Episode: {Average(record => record.gapSegmentCount):F2}");
        builder.AppendLine($"Average PlatformChainSegment Count Per Episode: {Average(record => record.platformChainSegmentCount):F2}");
        builder.AppendLine($"Average PlatformChain Pieces Per Chain: {AveragePlatformChainPiecesPerChain():F2}");
        builder.AppendLine($"Average PlatformChain Internal Gap Width: {AveragePlatformChainInternalGapWidth():F2}");
        builder.AppendLine($"Average FlatSegment Count Per Episode: {Average(record => record.flatSegmentCount):F2}");
        builder.AppendLine($"Average StepUp Height Delta: {AverageStepUpHeightDelta():F2}");
        builder.AppendLine($"Max StepUp Height Delta: {MaxStepUpHeightDelta():F2}");
    }

    private void AppendRuntimeConfigurationSnapshot(StringBuilder builder)
    {
        EnsureAgentReference();
        EnsureMixedLevelGeneratorReference();
        Agent snapshotAgent = GetActiveAgent();

        MixedLevelGenerator agentMixedLevelGenerator = GetFieldValue<MixedLevelGenerator>(
            snapshotAgent,
            "mixedLevelGenerator",
            null
        );
        BehaviorParameters behaviorParameters = snapshotAgent != null ? snapshotAgent.GetComponent<BehaviorParameters>() : null;
        UnityEngine.Object loadedModel = GetFieldValue<UnityEngine.Object>(behaviorParameters, "m_Model", null);
        string behaviorName = GetFieldValue(behaviorParameters, "m_BehaviorName", "None");

        builder.AppendLine("=== Runtime Configuration Snapshot ===");
        builder.AppendLine($"Scene Name: {SceneManager.GetActiveScene().name}");
        builder.AppendLine($"Model Label: {modelLabel}");
        builder.AppendLine($"Evaluation Label: {evaluationLabel}");
        builder.AppendLine($"Time.timeScale Used: {FormatFloat(Time.timeScale)}");
        builder.AppendLine($"Evaluation Time Scale Setting: {FormatFloat(evaluationTimeScale)}");
        builder.AppendLine($"Generate On Start: {FormatBool(GetFieldValue(mixedLevelGenerator, "generateOnStart", false))}");
        builder.AppendLine($"useMixedLevelGenerator: {FormatBool(GetFieldValue(snapshotAgent, "useMixedLevelGenerator", false))}");
        builder.AppendLine($"Agent MixedLevelGenerator GameObject: {GetObjectName(agentMixedLevelGenerator)}");
        builder.AppendLine($"EvaluationManager MixedLevelGenerator GameObject: {GetObjectName(mixedLevelGenerator)}");
        builder.AppendLine($"MixedLevelGenerator References Match: {FormatBool(agentMixedLevelGenerator != null && agentMixedLevelGenerator == mixedLevelGenerator)}");
        builder.AppendLine($"GapGenerator GameObject: {GetObjectName(gapGenerator)}");
        builder.AppendLine($"GapGenerator Active In Hierarchy: {FormatBool(gapGenerator != null && gapGenerator.gameObject.activeInHierarchy)}");

        AppendMixedLevelGeneratorSnapshot(builder);
        AppendAgentSnapshot(builder);

        builder.AppendLine($"Behavior Name: {behaviorName}");
        builder.AppendLine($"ONNX Model: {(loadedModel != null ? loadedModel.name : "None")}");
    }

    private void AppendMixedLevelGeneratorSnapshot(StringBuilder builder)
    {
        if (mixedLevelGenerator == null)
        {
            builder.AppendLine("MixedLevelGenerator: None");
            return;
        }

        builder.AppendLine("MixedLevelGenerator Settings:");
        builder.AppendLine($"- Min Segments: {GetFieldValue(mixedLevelGenerator, "minSegments", 0)}");
        builder.AppendLine($"- Max Segments: {GetFieldValue(mixedLevelGenerator, "maxSegments", 0)}");
        builder.AppendLine($"- Min Platform Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minPlatformWidth", 0f))}");
        builder.AppendLine($"- Max Platform Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxPlatformWidth", 0f))}");
        builder.AppendLine($"- Min Gap Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minGapWidth", 0f))}");
        builder.AppendLine($"- Max Gap Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxGapWidth", 0f))}");
        builder.AppendLine($"- Min Height Delta: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minHeightDelta", 0f))}");
        builder.AppendLine($"- Max Height Delta: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxHeightDelta", 0f))}");
        builder.AppendLine($"- Min Drop Height: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minDropHeight", 0f))}");
        builder.AppendLine($"- Max Drop Height: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxDropHeight", 0f))}");
        builder.AppendLine($"- Min Platform Chain Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minPlatformChainWidth", 0f))}");
        builder.AppendLine($"- Max Platform Chain Width: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxPlatformChainWidth", 0f))}");
        builder.AppendLine($"- Min Platform Chain Pieces: {GetFieldValue(mixedLevelGenerator, "minPlatformChainPieces", 0)}");
        builder.AppendLine($"- Max Platform Chain Pieces: {GetFieldValue(mixedLevelGenerator, "maxPlatformChainPieces", 0)}");
        builder.AppendLine($"- Min Platform Chain Gap: {FormatFloat(GetFieldValue(mixedLevelGenerator, "minPlatformChainGap", 0f))}");
        builder.AppendLine($"- Max Platform Chain Gap: {FormatFloat(GetFieldValue(mixedLevelGenerator, "maxPlatformChainGap", 0f))}");
        builder.AppendLine($"- Max Platform Chains Per Episode: {GetFieldValue(mixedLevelGenerator, "maxPlatformChainsPerEpisode", 0)}");
        builder.AppendLine($"- Prevent Consecutive Platform Chains: {FormatBool(GetFieldValue(mixedLevelGenerator, "preventConsecutivePlatformChains", false))}");
        builder.AppendLine($"- Avoid Hard Segment After Platform Chain: {FormatBool(GetFieldValue(mixedLevelGenerator, "avoidHardSegmentAfterPlatformChain", false))}");
        builder.AppendLine($"- Vertical Safety Limit: {FormatFloat(GetFieldValue(mixedLevelGenerator, "verticalSafetyLimit", 0f))}");
        builder.AppendLine($"- Agent Spawn Position: {FormatVector2(GetFieldValue(mixedLevelGenerator, "agentSpawnPosition", Vector2.zero))}");
        builder.AppendLine($"- Start X: {FormatFloat(GetFieldValue(mixedLevelGenerator, "startX", 0f))}");
        builder.AppendLine($"- Start Y: {FormatFloat(GetFieldValue(mixedLevelGenerator, "startY", 0f))}");
        builder.AppendLine($"- Platform Height Scale: {FormatFloat(GetFieldValue(mixedLevelGenerator, "platformHeightScale", 0f))}");
        builder.AppendLine($"- Goal X Offset From End: {FormatFloat(GetFieldValue(mixedLevelGenerator, "goalXOffsetFromEnd", 0f))}");
        builder.AppendLine($"- Goal Y Offset: {FormatFloat(GetFieldValue(mixedLevelGenerator, "goalYOffset", 0f))}");
        builder.AppendLine($"- Enable FlatSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enableFlatSegment", false))}");
        builder.AppendLine($"- Enable GapSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enableGapSegment", false))}");
        builder.AppendLine($"- Enable StepUpSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enableStepUpSegment", false))}");
        builder.AppendLine($"- Enable StepDownSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enableStepDownSegment", false))}");
        builder.AppendLine($"- Enable SafeDropSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enableSafeDropSegment", false))}");
        builder.AppendLine($"- Enable PlatformChainSegment: {FormatBool(GetFieldValue(mixedLevelGenerator, "enablePlatformChainSegment", false))}");
    }

    private void AppendAgentSnapshot(StringBuilder builder)
    {
        Agent snapshotAgent = GetActiveAgent();

        if (snapshotAgent == null)
        {
            builder.AppendLine("Agent: None");
            return;
        }

        builder.AppendLine("Agent Settings:");

        if (snapshotAgent is EdgeRunnerAgentV5EnemyAware)
        {
            builder.AppendLine("- Agent Version: V5EnemyAware");
            builder.AppendLine($"- Expected Observation Space Size: {EdgeRunnerAgentV5EnemyAware.DefaultExpectedObservationSize}");
            builder.AppendLine("- Expected Actions/Branches: [3, 2, 2]");
            builder.AppendLine($"- normalMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "normalMoveSpeed", 0f))}");
            builder.AppendLine($"- sprintMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "sprintMoveSpeed", 0f))}");
            builder.AppendLine($"- allowSprint: {FormatBool(GetFieldValue(snapshotAgent, "allowSprint", false))}");
            builder.AppendLine($"- jumpForce: {FormatFloat(GetFieldValue(snapshotAgent, "jumpForce", 0f))}");
            builder.AppendLine($"- useCoyoteTime: {FormatBool(GetFieldValue(snapshotAgent, "useCoyoteTime", false))}");
            builder.AppendLine($"- coyoteTime: {FormatFloat(GetFieldValue(snapshotAgent, "coyoteTime", 0f))}");
            builder.AppendLine($"- useJumpBuffer: {FormatBool(GetFieldValue(snapshotAgent, "useJumpBuffer", false))}");
            builder.AppendLine($"- requireJumpReleaseBeforeNextJump: {FormatBool(GetFieldValue(snapshotAgent, "requireJumpReleaseBeforeNextJump", false))}");
            builder.AppendLine($"- forwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "forwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- backwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "backwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- verticalSensorCount: {GetFieldValue(snapshotAgent, "verticalSensorCount", 0)}");
            builder.AppendLine($"- enemySensorRangeX: {FormatFloat(GetFieldValue(snapshotAgent, "enemySensorRangeX", 0f))}");
            builder.AppendLine($"- enemySensorRangeY: {FormatFloat(GetFieldValue(snapshotAgent, "enemySensorRangeY", 0f))}");
            builder.AppendLine($"- enemyHitPenalty: {FormatFloat(GetFieldValue(snapshotAgent, "enemyHitPenalty", 0f))}");
            builder.AppendLine($"- enemyPassReward: {FormatFloat(GetFieldValue(snapshotAgent, "enemyPassReward", 0f))}");
            return;
        }

        if (snapshotAgent is EdgeRunnerAgentV5EnemiesTransfer)
        {
            builder.AppendLine("- Agent Version: V5EnemiesTransfer");
            builder.AppendLine($"- Expected Observation Space Size: {EdgeRunnerAgentV5EnemiesTransfer.DefaultExpectedObservationSize}");
            builder.AppendLine("- Expected Actions/Branches: [3, 2, 2]");
            builder.AppendLine($"- normalMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "normalMoveSpeed", 0f))}");
            builder.AppendLine($"- sprintMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "sprintMoveSpeed", 0f))}");
            builder.AppendLine($"- allowSprint: {FormatBool(GetFieldValue(snapshotAgent, "allowSprint", false))}");
            builder.AppendLine($"- jumpForce: {FormatFloat(GetFieldValue(snapshotAgent, "jumpForce", 0f))}");
            builder.AppendLine($"- useCoyoteTime: {FormatBool(GetFieldValue(snapshotAgent, "useCoyoteTime", false))}");
            builder.AppendLine($"- coyoteTime: {FormatFloat(GetFieldValue(snapshotAgent, "coyoteTime", 0f))}");
            builder.AppendLine($"- useJumpBuffer: {FormatBool(GetFieldValue(snapshotAgent, "useJumpBuffer", false))}");
            builder.AppendLine($"- useJumpBufferForAgent: {FormatBool(GetFieldValue(snapshotAgent, "useJumpBufferForAgent", false))}");
            builder.AppendLine($"- requireJumpReleaseBeforeNextJump: {FormatBool(GetFieldValue(snapshotAgent, "requireJumpReleaseBeforeNextJump", false))}");
            builder.AppendLine($"- forwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "forwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- backwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "backwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- verticalSensorCount: {GetFieldValue(snapshotAgent, "verticalSensorCount", 0)}");
            builder.AppendLine($"- enemyHitPenalty: {FormatFloat(GetFieldValue(snapshotAgent, "enemyHitPenalty", 0f))}");
            builder.AppendLine($"- enemyPassReward: {FormatFloat(GetFieldValue(snapshotAgent, "enemyPassReward", 0f))}");
            return;
        }

        if (snapshotAgent is EdgeRunnerAgentV5)
        {
            builder.AppendLine("- Agent Version: V5");
            builder.AppendLine("- Expected Observation Space Size: 55");
            builder.AppendLine("- Expected Actions/Branches: [3, 2, 2]");
            builder.AppendLine($"- normalMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "normalMoveSpeed", 0f))}");
            builder.AppendLine($"- sprintMoveSpeed: {FormatFloat(GetFieldValue(snapshotAgent, "sprintMoveSpeed", 0f))}");
            builder.AppendLine($"- allowSprint: {FormatBool(GetFieldValue(snapshotAgent, "allowSprint", false))}");
            builder.AppendLine($"- jumpForce: {FormatFloat(GetFieldValue(snapshotAgent, "jumpForce", 0f))}");
            builder.AppendLine($"- useCoyoteTime: {FormatBool(GetFieldValue(snapshotAgent, "useCoyoteTime", false))}");
            builder.AppendLine($"- coyoteTime: {FormatFloat(GetFieldValue(snapshotAgent, "coyoteTime", 0f))}");
            builder.AppendLine($"- useJumpBuffer: {FormatBool(GetFieldValue(snapshotAgent, "useJumpBuffer", false))}");
            builder.AppendLine($"- useJumpBufferForAgent: {FormatBool(GetFieldValue(snapshotAgent, "useJumpBufferForAgent", false))}");
            builder.AppendLine($"- requireJumpReleaseBeforeNextJump: {FormatBool(GetFieldValue(snapshotAgent, "requireJumpReleaseBeforeNextJump", false))}");
            builder.AppendLine($"- forwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "forwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- forwardSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "forwardSensorRange", 0f))}");
            builder.AppendLine($"- backwardTerrainSampleCount: {GetFieldValue(snapshotAgent, "backwardTerrainSampleCount", 0)}");
            builder.AppendLine($"- backwardSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "backwardSensorRange", 0f))}");
            builder.AppendLine($"- verticalSensorCount: {GetFieldValue(snapshotAgent, "verticalSensorCount", 0)}");
            builder.AppendLine($"- verticalSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "verticalSensorRange", 0f))}");
            builder.AppendLine($"- gapSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "gapSensorRange", 0f))}");
            builder.AppendLine($"- frontDownSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "frontDownSensorRange", 0f))}");
            builder.AppendLine($"- wallSensorRange: {FormatFloat(GetFieldValue(snapshotAgent, "wallSensorRange", 0f))}");
            builder.AppendLine($"- groundCheckRange: {FormatFloat(GetFieldValue(snapshotAgent, "groundCheckRange", 0f))}");
            builder.AppendLine($"- allowBacktrackingForPositioning: {FormatBool(GetFieldValue(snapshotAgent, "allowBacktrackingForPositioning", false))}");
            builder.AppendLine($"- maxAllowedBacktrackDistance: {FormatFloat(GetFieldValue(snapshotAgent, "maxAllowedBacktrackDistance", 0f))}");
            return;
        }

        builder.AppendLine($"- Jump Force: {FormatFloat(GetFieldValue(snapshotAgent, "jumpForce", 0f))}");
        builder.AppendLine($"- Move Speed: {FormatFloat(GetFieldValue(snapshotAgent, "moveSpeed", 0f))}");
        builder.AppendLine($"- Use Adaptive Jump Window: {FormatBool(GetFieldValue(snapshotAgent, "useAdaptiveJumpWindow", false))}");
        builder.AppendLine($"- Require Landing For Useful Gap Jump: {FormatBool(GetFieldValue(snapshotAgent, "requireLandingForUsefulGapJump", false))}");
        builder.AppendLine($"- Max Gap Distance For Useful Jump: {FormatFloat(GetFieldValue(snapshotAgent, "maxGapDistanceForUsefulJump", 0f))}");
        builder.AppendLine($"- Allow Jump: {FormatBool(GetFieldValue(snapshotAgent, "allowJump", false))}");
        builder.AppendLine($"- Mask Useless Jumps: {FormatBool(GetFieldValue(snapshotAgent, "maskUselessJumps", false))}");
        builder.AppendLine($"- Mask Move Away From Goal: {FormatBool(GetFieldValue(snapshotAgent, "maskMoveAwayFromGoal", false))}");
        builder.AppendLine($"- Allow Idle Action: {FormatBool(GetFieldValue(snapshotAgent, "allowIdleAction", false))}");
    }

    private T GetFieldValue<T>(object source, string fieldName, T fallback)
    {
        if (source == null)
        {
            return fallback;
        }

        FieldInfo field = source.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null)
        {
            return fallback;
        }

        object value = field.GetValue(source);

        if (value is T typedValue)
        {
            return typedValue;
        }

        return fallback;
    }

    private string GetObjectName(Component component)
    {
        return component != null ? component.gameObject.name : "None";
    }

    private string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private string FormatFloat(float value)
    {
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }

    private string FormatVector2(Vector2 value)
    {
        return $"({FormatFloat(value.x)}, {FormatFloat(value.y)})";
    }

    private void AppendFailuresByNearestGapIndex(StringBuilder builder)
    {
        Dictionary<int, int> failuresByGap = new Dictionary<int, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            if (!failuresByGap.ContainsKey(record.nearestGapIndex))
            {
                failuresByGap[record.nearestGapIndex] = 0;
            }

            failuresByGap[record.nearestGapIndex]++;
        }

        if (failuresByGap.Count == 0)
        {
            builder.AppendLine("- None: 0");
            return;
        }

        List<int> sortedKeys = new List<int>(failuresByGap.Keys);
        sortedKeys.Sort();

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            int gapIndex = sortedKeys[i];
            string label = gapIndex >= 0 ? gapIndex.ToString(CultureInfo.InvariantCulture) : "None";
            builder.AppendLine($"- {label}: {failuresByGap[gapIndex]}");
        }
    }

    private void AppendFailuresByNearestSegmentType(StringBuilder builder)
    {
        Dictionary<string, int> failuresBySegmentType = new Dictionary<string, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            string segmentType = string.IsNullOrWhiteSpace(record.nearestSegmentType)
                ? "None"
                : record.nearestSegmentType;

            if (!failuresBySegmentType.ContainsKey(segmentType))
            {
                failuresBySegmentType[segmentType] = 0;
            }

            failuresBySegmentType[segmentType]++;
        }

        if (failuresBySegmentType.Count == 0)
        {
            builder.AppendLine("- None: 0");
            return;
        }

        List<string> sortedKeys = new List<string>(failuresBySegmentType.Keys);
        sortedKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            string segmentType = sortedKeys[i];
            builder.AppendLine($"- {segmentType}: {failuresBySegmentType[segmentType]}");
        }
    }

    private void AppendFailuresByNearestSegmentIndex(StringBuilder builder)
    {
        Dictionary<int, int> failuresBySegmentIndex = new Dictionary<int, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            if (!failuresBySegmentIndex.ContainsKey(record.nearestSegmentIndex))
            {
                failuresBySegmentIndex[record.nearestSegmentIndex] = 0;
            }

            failuresBySegmentIndex[record.nearestSegmentIndex]++;
        }

        if (failuresBySegmentIndex.Count == 0)
        {
            builder.AppendLine("- None: 0");
            return;
        }

        List<int> sortedKeys = new List<int>(failuresBySegmentIndex.Keys);
        sortedKeys.Sort();

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            int segmentIndex = sortedKeys[i];
            string label = segmentIndex >= 0 ? segmentIndex.ToString(CultureInfo.InvariantCulture) : "None";
            builder.AppendLine($"- {label}: {failuresBySegmentIndex[segmentIndex]}");
        }
    }

    private void AppendFailuresByCurrentSegmentType(StringBuilder builder)
    {
        Dictionary<string, int> failuresBySegmentType = new Dictionary<string, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            string segmentType = SegmentTypeOrNone(record.currentSegmentType);

            if (!failuresBySegmentType.ContainsKey(segmentType))
            {
                failuresBySegmentType[segmentType] = 0;
            }

            failuresBySegmentType[segmentType]++;
        }

        AppendSortedStringCounts(builder, failuresBySegmentType);
    }

    private void AppendFailuresByNextSegmentType(StringBuilder builder)
    {
        Dictionary<string, int> failuresBySegmentType = new Dictionary<string, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            string segmentType = SegmentTypeOrNone(record.nextSegmentType);

            if (!failuresBySegmentType.ContainsKey(segmentType))
            {
                failuresBySegmentType[segmentType] = 0;
            }

            failuresBySegmentType[segmentType]++;
        }

        AppendSortedStringCounts(builder, failuresBySegmentType);
    }

    private void AppendFailuresByTransition(StringBuilder builder)
    {
        Dictionary<string, int> failuresByTransition = new Dictionary<string, int>();

        for (int i = 0; i < episodeRecords.Count; i++)
        {
            EpisodeRecord record = episodeRecords[i];

            if (record.success)
            {
                continue;
            }

            string transition = SegmentTypeOrNone(record.currentSegmentType) +
                " -> " +
                SegmentTypeOrNone(record.nextSegmentType);

            if (!failuresByTransition.ContainsKey(transition))
            {
                failuresByTransition[transition] = 0;
            }

            failuresByTransition[transition]++;
        }

        AppendSortedStringCounts(builder, failuresByTransition);
    }

    private void AppendSortedStringCounts(StringBuilder builder, Dictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            builder.AppendLine("- None: 0");
            return;
        }

        List<string> sortedKeys = new List<string>(counts.Keys);
        sortedKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            string key = sortedKeys[i];
            builder.AppendLine($"- {key}: {counts[key]}");
        }
    }

    private string SegmentTypeOrNone(string segmentType)
    {
        return string.IsNullOrWhiteSpace(segmentType) ? "None" : segmentType;
    }

    private void EnsureAgentReference()
    {
        if (activeAgent != null)
        {
            return;
        }

        if (agent != null || agentV5 != null || agentV5EnemyAware != null || agentV5EnemiesTransfer != null)
        {
            Agent selectedAssignedAgent = SelectPreferredAgent(agentV5EnemyAware, agentV5EnemiesTransfer, agentV5, agent);

            if (selectedAssignedAgent != null)
            {
                SetActiveAgent(selectedAssignedAgent);
                return;
            }
        }

        EdgeRunnerAgentV5EnemyAware foundV5EnemyAware = FindFirstEnabledAgent(
            FindObjectsByType<EdgeRunnerAgentV5EnemyAware>(FindObjectsInactive.Include)
        );
        EdgeRunnerAgentV5EnemiesTransfer foundV5EnemiesTransfer = FindFirstEnabledAgent(
            FindObjectsByType<EdgeRunnerAgentV5EnemiesTransfer>(FindObjectsInactive.Include)
        );
        EdgeRunnerAgentV5 foundV5 = FindFirstEnabledAgent(
            FindObjectsByType<EdgeRunnerAgentV5>(FindObjectsInactive.Include)
        );
        EdgeRunnerAgentV3 foundV3 = FindFirstEnabledAgent(
            FindObjectsByType<EdgeRunnerAgentV3>(FindObjectsInactive.Include)
        );

        if ((foundV5EnemyAware != null || foundV5EnemiesTransfer != null || foundV5 != null) && foundV3 != null && !loggedMultipleAgentWarning)
        {
            loggedMultipleAgentWarning = true;
            Debug.LogWarning(
                "EdgeRunnerEvaluationManager: found multiple EdgeRunner agent versions; " +
                "using the active/enabled preferred agent."
            );
        }

        Agent selectedFoundAgent = SelectPreferredAgent(foundV5EnemyAware, foundV5EnemiesTransfer, foundV5, foundV3);

        if (selectedFoundAgent != null)
        {
            SetActiveAgent(selectedFoundAgent);
        }
    }

    private Agent GetActiveAgent()
    {
        EnsureAgentReference();
        return activeAgent;
    }

    private void RegisterSourceAgent(Agent sourceAgent)
    {
        if (sourceAgent == null)
        {
            return;
        }

        if (activeAgent == null)
        {
            EnsureAgentReference();
        }

        if (sourceAgent is EdgeRunnerAgentV5EnemyAware sourceAgentV5EnemyAware)
        {
            if (agentV5EnemyAware == null)
            {
                agentV5EnemyAware = sourceAgentV5EnemyAware;
            }
        }
        else if (sourceAgent is EdgeRunnerAgentV5EnemiesTransfer sourceAgentV5EnemiesTransfer)
        {
            if (agentV5EnemiesTransfer == null)
            {
                agentV5EnemiesTransfer = sourceAgentV5EnemiesTransfer;
            }
        }
        else if (sourceAgent is EdgeRunnerAgentV5 sourceAgentV5)
        {
            if (agentV5 == null)
            {
                agentV5 = sourceAgentV5;
            }
        }
        else if (sourceAgent is EdgeRunnerAgentV3 sourceAgentV3)
        {
            if (agent == null)
            {
                agent = sourceAgentV3;
            }
        }

        if (activeAgent == null)
        {
            SetActiveAgent(sourceAgent);
        }
    }

    private Agent SelectPreferredAgent(
        EdgeRunnerAgentV5EnemyAware candidateV5EnemyAware,
        EdgeRunnerAgentV5EnemiesTransfer candidateV5EnemiesTransfer,
        EdgeRunnerAgentV5 candidateV5,
        EdgeRunnerAgentV3 candidateV3)
    {
        bool v5EnemyAwareUsable = IsAgentUsable(candidateV5EnemyAware);
        bool v5EnemiesTransferUsable = IsAgentUsable(candidateV5EnemiesTransfer);
        bool v5Usable = IsAgentUsable(candidateV5);
        bool v3Usable = IsAgentUsable(candidateV3);

        if (v5EnemyAwareUsable)
        {
            return candidateV5EnemyAware;
        }

        if (v5EnemiesTransferUsable)
        {
            return candidateV5EnemiesTransfer;
        }

        if (v5Usable)
        {
            return candidateV5;
        }

        if (v3Usable)
        {
            return candidateV3;
        }

        if (candidateV5EnemyAware != null)
        {
            return candidateV5EnemyAware;
        }

        if (candidateV5EnemiesTransfer != null)
        {
            return candidateV5EnemiesTransfer;
        }

        if (candidateV5 != null)
        {
            return candidateV5;
        }

        return candidateV3;
    }

    private T FindFirstEnabledAgent<T>(T[] candidates) where T : Behaviour
    {
        if (candidates == null || candidates.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            if (IsAgentUsable(candidates[i]))
            {
                return candidates[i];
            }
        }

        return candidates[0];
    }

    private bool IsAgentUsable(Behaviour candidate)
    {
        return candidate != null && candidate.isActiveAndEnabled;
    }

    private void SetActiveAgent(Agent selectedAgent)
    {
        activeAgent = selectedAgent;

        if (selectedAgent is EdgeRunnerAgentV5EnemyAware selectedV5EnemyAware)
        {
            agentV5EnemyAware = selectedV5EnemyAware;
        }
        else if (selectedAgent is EdgeRunnerAgentV5EnemiesTransfer selectedV5EnemiesTransfer)
        {
            agentV5EnemiesTransfer = selectedV5EnemiesTransfer;
        }
        else if (selectedAgent is EdgeRunnerAgentV5 selectedV5)
        {
            agentV5 = selectedV5;
        }
        else if (selectedAgent is EdgeRunnerAgentV3 selectedV3)
        {
            agent = selectedV3;
        }

        LogActiveAgentSelection();
    }

    private void LogActiveAgentSelection()
    {
        if (loggedAgentSelection || activeAgent == null)
        {
            return;
        }

        loggedAgentSelection = true;

        if (activeAgent is EdgeRunnerAgentV5EnemyAware)
        {
            Debug.Log("EdgeRunnerEvaluationManager: using EdgeRunnerAgentV5EnemyAware.");
            return;
        }

        if (activeAgent is EdgeRunnerAgentV5EnemiesTransfer)
        {
            Debug.Log("EdgeRunnerEvaluationManager: using EdgeRunnerAgentV5EnemiesTransfer.");
            return;
        }

        if (activeAgent is EdgeRunnerAgentV5)
        {
            Debug.Log("EdgeRunnerEvaluationManager: using EdgeRunnerAgentV5.");
            return;
        }

        if (activeAgent is EdgeRunnerAgentV3)
        {
            Debug.Log("EdgeRunnerEvaluationManager: using EdgeRunnerAgentV3.");
            return;
        }

        Debug.Log($"EdgeRunnerEvaluationManager: using {activeAgent.GetType().Name}.");
    }

    private bool IsAgentGroundedForEvaluation(Agent sourceAgent)
    {
        if (sourceAgent is EdgeRunnerAgentV5EnemyAware sourceAgentV5EnemyAware)
        {
            return sourceAgentV5EnemyAware.IsCurrentlyGroundedForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV5EnemiesTransfer sourceAgentV5EnemiesTransfer)
        {
            return sourceAgentV5EnemiesTransfer.IsCurrentlyGroundedForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV5 sourceAgentV5)
        {
            return sourceAgentV5.IsCurrentlyGroundedForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV3 sourceAgentV3)
        {
            return sourceAgentV3.IsCurrentlyGroundedForEvaluation();
        }

        return false;
    }

    private Vector2 GetAgentVelocityForEvaluation(Agent sourceAgent)
    {
        if (sourceAgent is EdgeRunnerAgentV5EnemyAware sourceAgentV5EnemyAware)
        {
            return sourceAgentV5EnemyAware.GetCurrentVelocityForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV5EnemiesTransfer sourceAgentV5EnemiesTransfer)
        {
            return sourceAgentV5EnemiesTransfer.GetCurrentVelocityForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV5 sourceAgentV5)
        {
            return sourceAgentV5.GetCurrentVelocityForEvaluation();
        }

        if (sourceAgent is EdgeRunnerAgentV3 sourceAgentV3)
        {
            return sourceAgentV3.GetCurrentVelocityForEvaluation();
        }

        return Vector2.zero;
    }

    private EdgeRunnerObjectAwareEvaluationSnapshot CaptureObjectAwareSnapshot(
        Agent sourceAgent,
        bool goalReached)
    {
        if (!includeObjectAwareMetrics ||
            !(sourceAgent is EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent))
        {
            return default;
        }

        return objectAwareAgent.GetEvaluationSnapshot(goalReached);
    }

    private void EnsureGapGeneratorReference()
    {
        if (gapGenerator != null)
        {
            return;
        }

        gapGenerator = FindObjectOfType<GapGenerator>();
    }

    private void EnsureMixedLevelGeneratorReference()
    {
        if (mixedLevelGenerator != null)
        {
            return;
        }

        mixedLevelGenerator = FindObjectOfType<MixedLevelGenerator>();
    }

    private void StopPlayModeOrQuit()
    {
        Time.timeScale = 1f;
        stopRequested = false;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private struct EpisodeRecord
    {
        public int episodeIndex;
        public EdgeRunnerEpisodeEndReason reason;
        public float reward;
        public float duration;
        public float startX;
        public float finalX;
        public float maxXReached;
        public bool success;
        public int nearestGapIndex;
        public float nearestGapStartX;
        public float nearestGapEndX;
        public float nearestGapWidth;
        public float distanceToGapStart;
        public float distanceToGapEnd;
        public int nearestSegmentIndex;
        public string nearestSegmentType;
        public float nearestSegmentStartX;
        public float nearestSegmentEndX;
        public float nearestSegmentY;
        public float distanceToSegmentStart;
        public float distanceToSegmentEnd;
        public int previousSegmentIndex;
        public string previousSegmentType;
        public int currentSegmentIndex;
        public string currentSegmentType;
        public int nextSegmentIndex;
        public string nextSegmentType;
        public float distanceToCurrentSegmentEnd;
        public float distanceToNextSegmentStart;
        public int flatSegmentCount;
        public int gapSegmentCount;
        public int stepUpSegmentCount;
        public int stepDownSegmentCount;
        public int safeDropSegmentCount;
        public int platformChainSegmentCount;
        public int platformChainGroupCount;
        public float platformChainInternalGapWidthTotal;
        public int platformChainInternalGapSampleCount;
        public float stepUpHeightDeltaTotal;
        public int stepUpHeightDeltaSampleCount;
        public float maxStepUpHeightDelta;
        public bool groundedAtEnd;
        public float velocityXAtEnd;
        public float velocityYAtEnd;
        public bool objectAwareSnapshotValid;
        public bool goalReached;
        public bool allCoinsCollected;
        public bool androidStomped;
        public bool fullObjectiveSuccess;
        public int coinsCollected;
        public int coinsRemaining;
        public int enemiesKilled;
        public int enemiesRemaining;
        public int prematureGoalTouches;
    }

    private struct NearestGapSnapshot
    {
        public int index;
        public float startX;
        public float endX;
        public float width;
        public float distanceToStart;
        public float distanceToEnd;

        public static NearestGapSnapshot Empty => new NearestGapSnapshot
        {
            index = -1,
            startX = float.NaN,
            endX = float.NaN,
            width = float.NaN,
            distanceToStart = float.NaN,
            distanceToEnd = float.NaN
        };
    }

    private struct NearestSegmentSnapshot
    {
        public int index;
        public string type;
        public float startX;
        public float endX;
        public float y;
        public float distanceToStart;
        public float distanceToEnd;

        public static NearestSegmentSnapshot Empty => new NearestSegmentSnapshot
        {
            index = -1,
            type = string.Empty,
            startX = float.NaN,
            endX = float.NaN,
            y = float.NaN,
            distanceToStart = float.NaN,
            distanceToEnd = float.NaN
        };
    }

    private struct SegmentContextSnapshot
    {
        public int previousIndex;
        public string previousType;
        public int currentIndex;
        public string currentType;
        public int nextIndex;
        public string nextType;
        public float distanceToCurrentEnd;
        public float distanceToNextStart;

        public static SegmentContextSnapshot Empty => new SegmentContextSnapshot
        {
            previousIndex = -1,
            previousType = string.Empty,
            currentIndex = -1,
            currentType = string.Empty,
            nextIndex = -1,
            nextType = string.Empty,
            distanceToCurrentEnd = float.NaN,
            distanceToNextStart = float.NaN
        };
    }

    private struct SegmentGenerationStatsSnapshot
    {
        public int flatSegmentCount;
        public int gapSegmentCount;
        public int stepUpSegmentCount;
        public int stepDownSegmentCount;
        public int safeDropSegmentCount;
        public int platformChainSegmentCount;
        public int platformChainGroupCount;
        public float platformChainInternalGapWidthTotal;
        public int platformChainInternalGapSampleCount;
        public float stepUpHeightDeltaTotal;
        public int stepUpHeightDeltaSampleCount;
        public float maxStepUpHeightDelta;

        public static SegmentGenerationStatsSnapshot Empty => new SegmentGenerationStatsSnapshot
        {
            flatSegmentCount = 0,
            gapSegmentCount = 0,
            stepUpSegmentCount = 0,
            stepDownSegmentCount = 0,
            safeDropSegmentCount = 0,
            platformChainSegmentCount = 0,
            platformChainGroupCount = 0,
            platformChainInternalGapWidthTotal = 0f,
            platformChainInternalGapSampleCount = 0,
            stepUpHeightDeltaTotal = 0f,
            stepUpHeightDeltaSampleCount = 0,
            maxStepUpHeightDelta = 0f
        };
    }
}
