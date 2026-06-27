using System.Collections.Generic;
using System.Reflection;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EdgeRunnerAgentV5ScoreMax : EdgeRunnerAgentV5
{
    private const int ScoreMaxExtraObservationCount = 28;
    public new const int DefaultExpectedObservationSize =
        EdgeRunnerAgentV5.DefaultExpectedObservationSize + ScoreMaxExtraObservationCount;

    [Header("ScoreMax References")]
    [SerializeField] private Rigidbody2D scoreMaxRb;
    [SerializeField] private ScoreAttackManager scoreMaxManager;
    [SerializeField] private Transform scoreMaxGoal;
    [SerializeField] private LayerMask scoreMaxEnemyRayMask = ~0;

    [Header("ScoreMax Observation Normalization")]
    [SerializeField] private float maxObjectiveDistance = 30f;
    [SerializeField] private float maxObjectiveHeight = 12f;
    [SerializeField] private float maxCoinCountForObservation = 8f;
    [SerializeField] private float maxEnemyCountForObservation = 4f;

    [Header("ScoreMax Objective Progress Reward")]
    [SerializeField] private float progressToNextObjectiveRewardScale = 0.025f;
    [SerializeField] private float maxProgressToNextObjectiveReward = 0.025f;
    [SerializeField] private float maxRegressionFromNextObjectivePenalty = -0.015f;

    [Header("ScoreMax Coin Tutorial")]
    [SerializeField] private float missedCoinPenalty = 0f;
    [SerializeField] private bool endEpisodeOnMissedCoinIntro = false;
    [SerializeField] private float missedCoinForwardMargin = 2f;
    [SerializeField] private float coinJumpCueReward = 0f;
    [SerializeField] private float coinJumpCueHorizontalRange = 1.5f;
    [SerializeField] private float coinJumpCueMinVerticalOffset = 0.15f;
    [SerializeField] private bool detectAnyMissedObjectiveBehind = false;

    [Header("ScoreMax Enemy Tutorial")]
    [SerializeField] private float missedEnemyPenalty = 0f;
    [SerializeField] private bool endEpisodeOnMissedEnemyEasy = false;
    [SerializeField] private float missedEnemyForwardMargin = 2.5f;
    [SerializeField] private bool requireCoinsCompleteBeforeMissedEnemyCheck = true;
    [SerializeField] private float enemyStompCueReward = 0f;
    [SerializeField] private float enemyStompCueHorizontalRange = 2.25f;

    [Header("ScoreMax Contextual Shaping")]
    [SerializeField] private bool enableScoreMaxContextualShaping = false;
    [SerializeField] private float scoreMaxUselessJumpPenalty = -0.003f;
    [SerializeField] private float lowCoinHeightThreshold = 0.45f;
    [SerializeField] private float lowCoinApproachRange = 3.0f;
    [SerializeField] private float groundCoinApproachReward = 0f;
    [SerializeField] private float lowCoinUnnecessaryJumpPenalty = 0f;
    [SerializeField] private float contextualCoinJumpRange = 2.25f;
    [SerializeField] private float contextualEnemyJumpRange = 2.75f;
    [SerializeField] private float contextualGapProbeDistance = 1.25f;
    [SerializeField] private float contextualGapProbeDepth = 2.0f;
    [SerializeField] private float enemyStompAlignmentReward = 0.08f;
    [SerializeField] private float enemyStompAlignmentHorizontalTolerance = 0.9f;
    [SerializeField] private float enemyStompAlignmentMinHeight = 0.35f;
    [SerializeField] private float enemyStompAlignmentMaxUpwardVelocity = 0.1f;

    [Header("ScoreMax Enemy Rays")]
    [SerializeField] private float enemyRayRange = 6f;
    [SerializeField] private float frontLowRayHeight = 0.35f;
    [SerializeField] private float frontMidRayHeight = 1.0f;
    [SerializeField] private float downForwardRayOffsetX = 1.2f;
    [SerializeField] private float downForwardRayHeight = 1.0f;

    [Header("ScoreMax Debug")]
    [SerializeField] private bool debugScoreMaxObservations = false;
    [SerializeField] private bool debugScoreMaxObservationCount = false;
    [SerializeField] private bool debugScoreMaxObjectives = false;
    [SerializeField] private bool debugScoreMaxHeuristicInput = false;
    [SerializeField] private bool debugScoreMaxGroundCheck = false;
    [SerializeField] private bool debugScoreMaxRewards = false;
    [SerializeField] private bool debugScoreMaxNextObjective = false;
    [SerializeField] private float debugScoreMaxLogInterval = 1.0f;

    private static readonly FieldInfo BaseRigidbodyField =
        typeof(EdgeRunnerAgentV5).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseGoalField =
        typeof(EdgeRunnerAgentV5).GetField("goal", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseGroundCheckField =
        typeof(EdgeRunnerAgentV5).GetField("groundCheck", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseGroundLayerField =
        typeof(EdgeRunnerAgentV5).GetField("groundLayer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseGroundCheckRangeField =
        typeof(EdgeRunnerAgentV5).GetField("groundCheckRange", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseCoyoteTimerField =
        typeof(EdgeRunnerAgentV5).GetField("coyoteTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseScoreAttackManagerField =
        typeof(EdgeRunnerAgentV5).GetField("scoreAttackManager", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo BaseIsGroundedMethod =
        typeof(EdgeRunnerAgentV5).GetMethod("IsGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo VectorSensorObservationsField =
        typeof(VectorSensor).GetField("m_Observations", BindingFlags.Instance | BindingFlags.NonPublic);

    private float previousNextObjectiveDistance = -1f;
    private Transform previousNextObjectiveTarget;
    private ScoreMaxObjectiveType previousNextObjectiveType = ScoreMaxObjectiveType.None;
    private Transform rewardedCoinJumpCueTarget;
    private Transform rewardedEnemyStompCueTarget;
    private Transform rewardedEnemyStompAlignmentTarget;
    private float nextScoreMaxDebugLogTime;
    private float nextScoreMaxObjectiveDebugLogTime;
    private float nextScoreMaxHeuristicDebugLogTime;
    private float nextScoreMaxGroundDebugLogTime;
    private float nextScoreMaxNextObjectiveDebugLogTime;
    private bool loggedScoreMaxObservationCountThisEpisode;
    private bool warnedScoreMaxObservationMismatchThisEpisode;
    private bool missedCoinIntroPenaltyApplied;
    private bool missedEnemyEasyPenaltyApplied;

    public override void Initialize()
    {
        base.Initialize();
        ResolveScoreMaxReferences();
    }

    public override void OnEpisodeBegin()
    {
        ResolveScoreMaxReferences();
        base.OnEpisodeBegin();
        ResolveScoreMaxReferences();
        previousNextObjectiveDistance = -1f;
        previousNextObjectiveTarget = null;
        previousNextObjectiveType = ScoreMaxObjectiveType.None;
        rewardedCoinJumpCueTarget = null;
        rewardedEnemyStompCueTarget = null;
        rewardedEnemyStompAlignmentTarget = null;
        missedCoinIntroPenaltyApplied = false;
        missedEnemyEasyPenaltyApplied = false;
        loggedScoreMaxObservationCountThisEpisode = false;
        warnedScoreMaxObservationMismatchThisEpisode = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        ResolveScoreMaxReferences();
        int startObservationCount = GetObservationCount(sensor);

        base.CollectObservations(sensor);

        int afterBaseObservationCount = GetObservationCount(sensor);
        ResolveScoreMaxReferences();

        int coinsRemaining = scoreMaxManager != null ? scoreMaxManager.CoinsRemaining : 0;
        int enemiesRemaining = scoreMaxManager != null ? scoreMaxManager.EnemiesRemaining : 0;
        bool objectivesComplete = scoreMaxManager != null && scoreMaxManager.ObjectivesComplete;

        sensor.AddObservation(NormalizePositive(coinsRemaining, maxCoinCountForObservation));
        sensor.AddObservation(NormalizePositive(enemiesRemaining, maxEnemyCountForObservation));
        sensor.AddObservation(objectivesComplete ? 1f : 0f);

        AddTargetObservations(sensor, TryGetNearestCoinTransform(out Transform nearestCoin) ? nearestCoin : null);
        AddTargetObservations(sensor, TryGetNearestEnemyTransform(out Transform nearestEnemy) ? nearestEnemy : null);
        AddGoalObservations(sensor);
        AddNextObjectiveObservations(sensor);
        AddEnemyRayObservations(sensor);

        int finalObservationCount = GetObservationCount(sensor);
        ValidateScoreMaxObservationCount(
            startObservationCount,
            afterBaseObservationCount,
            finalObservationCount);

        if (debugScoreMaxObservations && Time.time >= nextScoreMaxDebugLogTime)
        {
            nextScoreMaxDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
            Debug.Log(
                $"[SCOREMAX OBS] coins={coinsRemaining} enemies={enemiesRemaining} " +
                $"complete={objectivesComplete} expectedObs={DefaultExpectedObservationSize}",
                this);
        }

        LogScoreMaxObjectiveDebug(coinsRemaining, enemiesRemaining, objectivesComplete);
        LogScoreMaxNextObjectiveDebug(coinsRemaining, enemiesRemaining);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        base.WriteDiscreteActionMask(actionMask);

        if (actionMask == null)
        {
            return;
        }

        if (IsScoreMaxHeuristicOnly() || ShouldAllowScoreMaxObjectiveJump())
        {
            // ScoreMax needs jumps for coins and stomps even when the base V5
            // gap mask considers the jump unnecessary.
            actionMask.SetActionEnabled(1, 1, true);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);

        if (debugScoreMaxHeuristicInput && Time.time >= nextScoreMaxHeuristicDebugLogTime)
        {
            nextScoreMaxHeuristicDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
            ActionSegment<int> actions = actionsOut.DiscreteActions;
            int moveAction = actions.Length > 0 ? actions[0] : -1;
            int jumpAction = actions.Length > 1 ? actions[1] : -1;
            int sprintAction = actions.Length > 2 ? actions[2] : -1;

            Debug.Log(
                $"[SCOREMAX HEURISTIC] move={moveAction} jump={jumpAction} sprint={sprintAction} " +
                $"grounded={IsScoreMaxGroundedForDebug()}",
                this);
        }

        LogScoreMaxGroundCheckDebug();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        ResolveScoreMaxReferences();
        ApplyCoinJumpCueReward(actions);
        ApplyEnemyStompCueReward(actions);
        ApplyContextualJumpPenalty(actions);

        base.OnActionReceived(actions);

        ApplyEnemyStompAlignmentReward();
        ApplyNextObjectiveProgressReward(actions);
        ApplyMissedCoinIntroRule();
        ApplyMissedEnemyEasyRule();
    }

    private void ResolveScoreMaxReferences()
    {
        if (scoreMaxRb == null)
        {
            scoreMaxRb = GetComponent<Rigidbody2D>();
        }

        if (scoreMaxManager == null)
        {
            scoreMaxManager = FindAnyObjectByType<ScoreAttackManager>();
        }

        if (scoreMaxGoal == null && scoreMaxManager != null)
        {
            scoreMaxGoal = scoreMaxManager.Goal;
        }

        if (scoreMaxGoal == null)
        {
            ScoreAttackGoalLock goalLock = FindAnyObjectByType<ScoreAttackGoalLock>();

            if (goalLock != null)
            {
                scoreMaxGoal = goalLock.transform;
            }
        }

        EnsureBaseGroundCheckReference();
        SyncBaseScoreMaxReferences();
    }

    private void EnsureBaseGroundCheckReference()
    {
        if (GetBaseGroundCheck() != null)
        {
            return;
        }

        Transform groundCheck = FindChildByName(transform, "GroundCheck");

        if (groundCheck != null)
        {
            BaseGroundCheckField?.SetValue(this, groundCheck);
        }
    }

    private void SyncBaseScoreMaxReferences()
    {
        if (scoreMaxRb != null)
        {
            BaseRigidbodyField?.SetValue(this, scoreMaxRb);
        }

        if (scoreMaxGoal != null)
        {
            BaseGoalField?.SetValue(this, scoreMaxGoal);
        }

        if (scoreMaxManager != null)
        {
            BaseScoreAttackManagerField?.SetValue(this, scoreMaxManager);
        }
    }

    private void ValidateScoreMaxObservationCount(
        int startObservationCount,
        int afterBaseObservationCount,
        int finalObservationCount)
    {
        if (startObservationCount < 0 || afterBaseObservationCount < 0 || finalObservationCount < 0)
        {
            if (debugScoreMaxObservationCount && !loggedScoreMaxObservationCountThisEpisode)
            {
                loggedScoreMaxObservationCountThisEpisode = true;
                Debug.Log(
                    $"[SCOREMAX OBS] count=unknown expected={DefaultExpectedObservationSize} " +
                    $"baseExpected={EdgeRunnerAgentV5.DefaultExpectedObservationSize} " +
                    $"extraExpected={ScoreMaxExtraObservationCount}",
                    this);
            }

            return;
        }

        int baseCount = afterBaseObservationCount - startObservationCount;
        int extraCount = finalObservationCount - afterBaseObservationCount;
        int totalCount = finalObservationCount - startObservationCount;

        if ((totalCount != DefaultExpectedObservationSize || extraCount != ScoreMaxExtraObservationCount) &&
            !warnedScoreMaxObservationMismatchThisEpisode)
        {
            warnedScoreMaxObservationMismatchThisEpisode = true;
            Debug.LogWarning(
                $"[SCOREMAX OBS] count={totalCount} expected={DefaultExpectedObservationSize} " +
                $"base={baseCount} extra={extraCount} expectedExtra={ScoreMaxExtraObservationCount}",
                this);
        }

        if (debugScoreMaxObservationCount && !loggedScoreMaxObservationCountThisEpisode)
        {
            loggedScoreMaxObservationCountThisEpisode = true;
            Debug.Log(
                $"[SCOREMAX OBS] count={totalCount} expected={DefaultExpectedObservationSize} " +
                $"base={baseCount} extra={extraCount}",
                this);
        }
    }

    private static int GetObservationCount(VectorSensor sensor)
    {
        if (sensor == null)
        {
            return -1;
        }

        if (VectorSensorObservationsField?.GetValue(sensor) is ICollection<float> observations)
        {
            return observations.Count;
        }

        return -1;
    }

    private void LogScoreMaxObjectiveDebug(
        int coinsRemaining,
        int enemiesRemaining,
        bool objectivesComplete)
    {
        if (!debugScoreMaxObjectives || Time.time < nextScoreMaxObjectiveDebugLogTime)
        {
            return;
        }

        nextScoreMaxObjectiveDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
        ScoreAttackCoin nearestCoin = null;
        ScoreAttackAndroid nearestEnemy = null;
        Transform nextObjective = null;
        ScoreMaxObjectiveType objectiveType = ScoreMaxObjectiveType.None;
        float coinDistance = -1f;
        float enemyDistance = -1f;
        float nextDistance = -1f;
        bool hasCoin = scoreMaxManager != null &&
                       scoreMaxManager.TryGetNearestActiveCoin(transform.position, out nearestCoin, out coinDistance);
        bool hasEnemy = scoreMaxManager != null &&
                        scoreMaxManager.TryGetNearestActiveEnemy(transform.position, out nearestEnemy, out enemyDistance);
        bool hasNextObjective = scoreMaxManager != null &&
                                scoreMaxManager.TryGetRecommendedNextObjective(
                                    transform.position,
                                    out nextObjective,
                                    out objectiveType,
                                    out nextDistance);

        Vector2 coinDelta = hasCoin ? (Vector2)(nearestCoin.transform.position - transform.position) : Vector2.zero;
        Vector2 enemyDelta = hasEnemy ? (Vector2)(nearestEnemy.transform.position - transform.position) : Vector2.zero;
        Vector2 nextDelta = hasNextObjective ? (Vector2)(nextObjective.position - transform.position) : Vector2.zero;

        Debug.Log(
            $"[SCOREMAX OBJ] coins={coinsRemaining} enemies={enemiesRemaining} goalUnlocked={objectivesComplete}\n" +
            $"nearestCoin exists={hasCoin} dx={coinDelta.x:F2} dy={coinDelta.y:F2} dist={(hasCoin ? coinDistance : -1f):F2}\n" +
            $"nearestEnemy exists={hasEnemy} dx={enemyDelta.x:F2} dy={enemyDelta.y:F2} dist={(hasEnemy ? enemyDistance : -1f):F2}\n" +
            $"nextObjective type={FormatScoreMaxObjectiveType(objectiveType)} dx={nextDelta.x:F2} dy={nextDelta.y:F2} dist={(hasNextObjective ? nextDistance : -1f):F2}",
            this);
    }

    private bool ShouldAllowScoreMaxObjectiveJump()
    {
        ResolveScoreMaxReferences();

        if (scoreMaxManager == null ||
            !CanScoreMaxJumpFromGroundOrCoyote() ||
            !scoreMaxManager.TryGetRecommendedNextObjective(
                transform.position,
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        Vector2 delta = target.position - transform.position;

        if (objectiveType == ScoreMaxObjectiveType.Coin)
        {
            float minimumJumpHeight = enableScoreMaxContextualShaping
                ? Mathf.Max(lowCoinHeightThreshold, coinJumpCueMinVerticalOffset)
                : 0.15f;
            return delta.y > minimumJumpHeight;
        }

        if (objectiveType == ScoreMaxObjectiveType.Enemy)
        {
            return delta.x >= -0.5f && delta.y > -0.75f;
        }

        return false;
    }

    private bool CanScoreMaxJumpFromGroundOrCoyote()
    {
        return IsScoreMaxGroundedForDebug() || GetBaseCoyoteTimer() > 0f;
    }

    private static string FormatScoreMaxObjectiveType(ScoreMaxObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            ScoreMaxObjectiveType.Coin => "coin",
            ScoreMaxObjectiveType.Enemy => "enemy",
            ScoreMaxObjectiveType.Goal => "goal",
            _ => "none"
        };
    }

    private bool IsScoreMaxHeuristicOnly()
    {
        Unity.MLAgents.Policies.BehaviorParameters behaviorParameters =
            GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();

        return behaviorParameters != null &&
               behaviorParameters.BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
    }

    private bool IsScoreMaxGroundedForDebug()
    {
        if (BaseIsGroundedMethod?.Invoke(this, null) is bool grounded)
        {
            return grounded;
        }

        return false;
    }

    private void LogScoreMaxGroundCheckDebug()
    {
        if (!debugScoreMaxGroundCheck || Time.time < nextScoreMaxGroundDebugLogTime)
        {
            return;
        }

        nextScoreMaxGroundDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
        Transform groundCheck = GetBaseGroundCheck();
        LayerMask groundLayer = GetBaseGroundLayer();
        float groundCheckRange = GetBaseGroundCheckRange();
        Collider2D collider = null;

        if (groundCheck != null)
        {
            collider = Physics2D.OverlapCircle(groundCheck.position, groundCheckRange, groundLayer);
        }

        Vector2 velocity = scoreMaxRb != null ? scoreMaxRb.linearVelocity : Vector2.zero;
        string groundCheckInfo = groundCheck != null
            ? $"{groundCheck.name} pos={groundCheck.position}"
            : "null";
        string colliderInfo = collider != null
            ? $"{collider.name} layer={LayerMask.LayerToName(collider.gameObject.layer)} trigger={collider.isTrigger}"
            : "none";

        Debug.Log(
            $"[SCOREMAX GROUND] grounded={IsScoreMaxGroundedForDebug()} " +
            $"coyote={GetBaseCoyoteTimer():F3} rbVel={velocity} " +
            $"groundCheck={groundCheckInfo} range={groundCheckRange:F3} " +
            $"layerMask={groundLayer.value} collider={colliderInfo}",
            this);
    }

    private Transform GetBaseGroundCheck()
    {
        return BaseGroundCheckField?.GetValue(this) as Transform;
    }

    private LayerMask GetBaseGroundLayer()
    {
        if (BaseGroundLayerField?.GetValue(this) is LayerMask groundLayer)
        {
            return groundLayer;
        }

        return default;
    }

    private float GetBaseGroundCheckRange()
    {
        if (BaseGroundCheckRangeField?.GetValue(this) is float range)
        {
            return Mathf.Max(0.01f, range);
        }

        return 0.15f;
    }

    private float GetBaseCoyoteTimer()
    {
        if (BaseCoyoteTimerField?.GetValue(this) is float coyoteTimer)
        {
            return coyoteTimer;
        }

        return 0f;
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

    private void AddTargetObservations(VectorSensor sensor, Transform target)
    {
        if (target == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector2 delta = target.position - transform.position;
        sensor.AddObservation(1f);
        sensor.AddObservation(NormalizeSigned(delta.x, maxObjectiveDistance));
        sensor.AddObservation(NormalizeSigned(delta.y, maxObjectiveHeight));
        sensor.AddObservation(NormalizePositive(delta.magnitude, maxObjectiveDistance));
    }

    private void AddGoalObservations(VectorSensor sensor)
    {
        Transform goalTransform = scoreMaxGoal != null
            ? scoreMaxGoal
            : scoreMaxManager != null
                ? scoreMaxManager.Goal
                : null;

        if (goalTransform == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector2 delta = goalTransform.position - transform.position;
        sensor.AddObservation(NormalizeSigned(delta.x, maxObjectiveDistance));
        sensor.AddObservation(NormalizeSigned(delta.y, maxObjectiveHeight));
        sensor.AddObservation(NormalizePositive(delta.magnitude, maxObjectiveDistance));
    }

    private void AddNextObjectiveObservations(VectorSensor sensor)
    {
        if (scoreMaxManager == null ||
            !scoreMaxManager.TryGetRecommendedNextObjective(
                transform.position,
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector2 delta = target.position - transform.position;
        sensor.AddObservation(objectiveType == ScoreMaxObjectiveType.Coin ? 1f : 0f);
        sensor.AddObservation(objectiveType == ScoreMaxObjectiveType.Enemy ? 1f : 0f);
        sensor.AddObservation(objectiveType == ScoreMaxObjectiveType.Goal ? 1f : 0f);
        sensor.AddObservation(NormalizeSigned(delta.x, maxObjectiveDistance));
        sensor.AddObservation(NormalizeSigned(delta.y, maxObjectiveHeight));
        sensor.AddObservation(NormalizePositive(delta.magnitude, maxObjectiveDistance));
    }

    private void AddEnemyRayObservations(VectorSensor sensor)
    {
        float direction = GetScoreMaxForwardDirection();
        AddEnemyRay(sensor, new Vector2(direction, 0f), new Vector2(direction * 0.25f, frontLowRayHeight), enemyRayRange);
        AddEnemyRay(sensor, new Vector2(direction, 0f), new Vector2(direction * 0.25f, frontMidRayHeight), enemyRayRange);
        AddEnemyRay(sensor, Vector2.down, new Vector2(direction * downForwardRayOffsetX, downForwardRayHeight), enemyRayRange);
        AddEnemyRay(sensor, new Vector2(-direction, 0f), new Vector2(-direction * 0.25f, frontMidRayHeight), enemyRayRange);
    }

    private void AddEnemyRay(VectorSensor sensor, Vector2 direction, Vector2 originOffset, float range)
    {
        Vector2 origin = (Vector2)transform.position + originOffset;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction.normalized, Mathf.Max(0.1f, range), scoreMaxEnemyRayMask);
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;

            if (collider == null || collider.transform.IsChildOf(transform))
            {
                continue;
            }

            ScoreAttackAndroid enemy = collider.GetComponentInParent<ScoreAttackAndroid>();

            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hits[i].distance);
        }

        bool hitEnemy = !float.IsPositiveInfinity(nearestDistance);
        sensor.AddObservation(hitEnemy ? 1f : 0f);
        sensor.AddObservation(hitEnemy ? NormalizePositive(nearestDistance, range) : 1f);
    }

    private bool TryGetNearestCoinTransform(out Transform target)
    {
        target = null;

        if (scoreMaxManager == null ||
            !scoreMaxManager.TryGetNearestActiveCoin(transform.position, out ScoreAttackCoin coin, out _))
        {
            return false;
        }

        target = coin.transform;
        return true;
    }

    private bool TryGetNearestEnemyTransform(out Transform target)
    {
        target = null;

        if (scoreMaxManager == null ||
            !scoreMaxManager.TryGetNearestActiveEnemy(transform.position, out ScoreAttackAndroid enemy, out _))
        {
            return false;
        }

        target = enemy.transform;
        return true;
    }

    private bool TryGetCurrentNextObjectiveState(
        out Transform target,
        out ScoreMaxObjectiveType objectiveType,
        out float distance)
    {
        target = null;
        objectiveType = ScoreMaxObjectiveType.None;
        distance = -1f;

        if (scoreMaxManager == null ||
            !scoreMaxManager.TryGetRecommendedNextObjective(
                transform.position,
                out target,
                out objectiveType,
                out distance))
        {
            return false;
        }

        if (objectiveType == ScoreMaxObjectiveType.Goal && !scoreMaxManager.ObjectivesComplete)
        {
            target = null;
            objectiveType = ScoreMaxObjectiveType.None;
            distance = -1f;
            return false;
        }

        return target != null && distance >= 0f;
    }

    private void LogScoreMaxNextObjectiveDebug(int coinsRemaining, int enemiesRemaining)
    {
        if (!debugScoreMaxNextObjective || Time.time < nextScoreMaxNextObjectiveDebugLogTime)
        {
            return;
        }

        nextScoreMaxNextObjectiveDebugLogTime =
            Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);

        if (!TryGetCurrentNextObjectiveState(
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out float distance))
        {
            Debug.Log(
                $"[SCOREMAX NEXT OBJECTIVE] type=none coinsRemaining={coinsRemaining} " +
                $"enemiesRemaining={enemiesRemaining}",
                this);
            return;
        }

        Vector2 delta = target.position - transform.position;
        bool isAhead = delta.x * GetScoreMaxForwardDirection() >= 0f;
        string coinClassification = objectiveType == ScoreMaxObjectiveType.Coin
            ? $" coinClass={(IsLowCoinDelta(delta.y) ? "low" : "jump")}"
            : string.Empty;
        Debug.Log(
            $"[SCOREMAX NEXT OBJECTIVE] type={FormatScoreMaxObjectiveType(objectiveType)} " +
            $"dx={delta.x:F2} dy={delta.y:F2} dist={distance:F2} " +
            $"position={(isAhead ? "ahead" : "behind")}{coinClassification} " +
            $"coinsRemaining={coinsRemaining} enemiesRemaining={enemiesRemaining}",
            this);
    }

    private void ApplyContextualJumpPenalty(ActionBuffers actions)
    {
        if (!enableScoreMaxContextualShaping ||
            scoreMaxUselessJumpPenalty >= 0f ||
            !CanScoreMaxJumpFromGroundOrCoyote())
        {
            return;
        }

        ActionSegment<int> discreteActions = actions.DiscreteActions;
        int jumpAction = discreteActions.Length > 1 ? discreteActions[1] : 0;
        if (jumpAction != 1)
        {
            return;
        }

        bool usefulObjectiveJump = false;
        bool lowCoinNearby = false;
        if (TryGetCurrentNextObjectiveState(
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            Vector2 delta = target.position - transform.position;
            float forwardDistance = delta.x * GetScoreMaxForwardDirection();

            if (objectiveType == ScoreMaxObjectiveType.Coin)
            {
                lowCoinNearby =
                    IsLowCoinDelta(delta.y) &&
                    forwardDistance >= 0f &&
                    forwardDistance <= lowCoinApproachRange;
                usefulObjectiveJump =
                    !IsLowCoinDelta(delta.y) &&
                    forwardDistance >= 0f &&
                    forwardDistance <= contextualCoinJumpRange &&
                    delta.y > Mathf.Max(lowCoinHeightThreshold, coinJumpCueMinVerticalOffset);
            }
            else if (objectiveType == ScoreMaxObjectiveType.Enemy)
            {
                usefulObjectiveJump =
                    forwardDistance >= 0f &&
                    forwardDistance <= contextualEnemyJumpRange &&
                    Mathf.Abs(delta.y) <= 1.5f;
            }
        }

        if (usefulObjectiveJump || HasContextualGapAhead())
        {
            return;
        }

        if (lowCoinNearby && lowCoinUnnecessaryJumpPenalty < 0f)
        {
            AddReward(lowCoinUnnecessaryJumpPenalty);
            if (debugScoreMaxRewards)
            {
                Debug.Log(
                    $"[SCOREMAX LOW COIN JUMP] penalty={lowCoinUnnecessaryJumpPenalty:F3}",
                    this);
            }

            return;
        }

        AddReward(scoreMaxUselessJumpPenalty);
        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX USELESS JUMP] penalty={scoreMaxUselessJumpPenalty:F3}",
                this);
        }
    }

    private bool HasContextualGapAhead()
    {
        if (scoreMaxRb == null)
        {
            return false;
        }

        float direction = GetScoreMaxForwardDirection();
        Vector2 origin = scoreMaxRb.position + new Vector2(
            direction * Mathf.Max(0.1f, contextualGapProbeDistance),
            0.1f);
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            Mathf.Max(0.2f, contextualGapProbeDepth),
            GetBaseGroundLayer());
        return hit.collider == null;
    }

    private void ApplyEnemyStompAlignmentReward()
    {
        if (!enableScoreMaxContextualShaping ||
            enemyStompAlignmentReward <= 0f ||
            scoreMaxRb == null ||
            !TryGetCurrentNextObjectiveState(
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            return;
        }

        if (objectiveType != ScoreMaxObjectiveType.Enemy ||
            target == null ||
            rewardedEnemyStompAlignmentTarget == target)
        {
            return;
        }

        Vector2 delta = transform.position - target.position;
        bool aboveEnemy = delta.y >= enemyStompAlignmentMinHeight;
        bool horizontallyAligned =
            Mathf.Abs(delta.x) <= enemyStompAlignmentHorizontalTolerance;
        bool descendingOrNearApex =
            scoreMaxRb.linearVelocity.y <= enemyStompAlignmentMaxUpwardVelocity;

        if (!aboveEnemy || !horizontallyAligned || !descendingOrNearApex)
        {
            return;
        }

        AddReward(enemyStompAlignmentReward);
        rewardedEnemyStompAlignmentTarget = target;

        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX STOMP ALIGNMENT] reward={enemyStompAlignmentReward:F3} " +
                $"enemy={target.name} dx={delta.x:F2} dy={delta.y:F2}",
                this);
        }
    }

    private void ApplyCoinJumpCueReward(ActionBuffers actions)
    {
        if (coinJumpCueReward <= 0f ||
            scoreMaxManager == null ||
            !CanScoreMaxJumpFromGroundOrCoyote())
        {
            return;
        }

        ActionSegment<int> discreteActions = actions.DiscreteActions;
        int jumpAction = discreteActions.Length > 1 ? discreteActions[1] : 0;
        if (jumpAction != 1 ||
            !TryGetCurrentNextObjectiveState(
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            return;
        }

        if (objectiveType != ScoreMaxObjectiveType.Coin ||
            target == null ||
            rewardedCoinJumpCueTarget == target)
        {
            return;
        }

        Vector2 delta = target.position - transform.position;
        float forwardDistance = delta.x * GetScoreMaxForwardDirection();
        if (delta.y <= Mathf.Max(lowCoinHeightThreshold, coinJumpCueMinVerticalOffset) ||
            forwardDistance < 0f ||
            forwardDistance > coinJumpCueHorizontalRange)
        {
            return;
        }

        AddReward(coinJumpCueReward);
        rewardedCoinJumpCueTarget = target;

        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX COIN JUMP CUE] reward={coinJumpCueReward:F3} " +
                $"coin={target.name} dx={delta.x:F2} dy={delta.y:F2}",
                this);
        }
    }

    private void ApplyMissedCoinIntroRule()
    {
        if ((Mathf.Approximately(missedCoinPenalty, 0f) && !endEpisodeOnMissedCoinIntro) ||
            scoreMaxManager == null ||
            missedCoinIntroPenaltyApplied ||
            scoreMaxManager.CoinsRemaining <= 0 ||
            !TryGetCoinForMissedObjectiveCheck(out ScoreAttackCoin nearestCoin))
        {
            return;
        }

        Transform coinTransform = nearestCoin != null ? nearestCoin.transform : null;
        if (coinTransform == null)
        {
            return;
        }

        float forwardDirection = GetScoreMaxForwardDirection();
        float missedCoinDelta = (transform.position.x - coinTransform.position.x) * forwardDirection;
        if (missedCoinDelta <= Mathf.Max(0f, missedCoinForwardMargin))
        {
            return;
        }

        missedCoinIntroPenaltyApplied = true;
        if (!Mathf.Approximately(missedCoinPenalty, 0f))
        {
            AddReward(missedCoinPenalty);
        }

        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX MISSED COIN] penalty={missedCoinPenalty:F3} " +
                $"endEpisode={endEpisodeOnMissedCoinIntro} coin={coinTransform.name} " +
                $"margin={missedCoinForwardMargin:F2} playerX={transform.position.x:F2} coinX={coinTransform.position.x:F2}",
                this);
        }

        if (endEpisodeOnMissedCoinIntro)
        {
            EndEpisode();
        }
    }

    private void ApplyEnemyStompCueReward(ActionBuffers actions)
    {
        if (enemyStompCueReward <= 0f ||
            scoreMaxManager == null ||
            scoreMaxManager.CoinsRemaining > 0 ||
            !CanScoreMaxJumpFromGroundOrCoyote())
        {
            return;
        }

        ActionSegment<int> discreteActions = actions.DiscreteActions;
        int jumpAction = discreteActions.Length > 1 ? discreteActions[1] : 0;
        if (jumpAction != 1 ||
            !TryGetCurrentNextObjectiveState(
                out Transform target,
                out ScoreMaxObjectiveType objectiveType,
                out _))
        {
            return;
        }

        if (objectiveType != ScoreMaxObjectiveType.Enemy ||
            target == null ||
            rewardedEnemyStompCueTarget == target)
        {
            return;
        }

        Vector2 delta = target.position - transform.position;
        float forwardDistance = delta.x * GetScoreMaxForwardDirection();
        if (forwardDistance < 0f ||
            forwardDistance > Mathf.Max(0f, enemyStompCueHorizontalRange) ||
            Mathf.Abs(delta.y) > 1.5f)
        {
            return;
        }

        AddReward(enemyStompCueReward);
        rewardedEnemyStompCueTarget = target;

        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX STOMP CUE] reward={enemyStompCueReward:F3} " +
                $"enemy={target.name} dx={delta.x:F2} dy={delta.y:F2}",
                this);
        }
    }

    private void ApplyMissedEnemyEasyRule()
    {
        if ((Mathf.Approximately(missedEnemyPenalty, 0f) && !endEpisodeOnMissedEnemyEasy) ||
            scoreMaxManager == null ||
            missedEnemyEasyPenaltyApplied ||
            (requireCoinsCompleteBeforeMissedEnemyCheck && scoreMaxManager.CoinsRemaining > 0) ||
            scoreMaxManager.EnemiesRemaining <= 0 ||
            !TryGetEnemyForMissedObjectiveCheck(out ScoreAttackAndroid nearestEnemy))
        {
            return;
        }

        Transform enemyTransform = nearestEnemy != null ? nearestEnemy.transform : null;
        if (enemyTransform == null)
        {
            return;
        }

        float forwardDirection = GetScoreMaxForwardDirection();
        float missedEnemyDelta = (transform.position.x - enemyTransform.position.x) * forwardDirection;
        if (missedEnemyDelta <= Mathf.Max(0f, missedEnemyForwardMargin))
        {
            return;
        }

        missedEnemyEasyPenaltyApplied = true;
        if (!Mathf.Approximately(missedEnemyPenalty, 0f))
        {
            AddReward(missedEnemyPenalty);
        }

        if (debugScoreMaxRewards)
        {
            Debug.Log(
                $"[SCOREMAX MISSED ENEMY] penalty={missedEnemyPenalty:F3} " +
                $"endEpisode={endEpisodeOnMissedEnemyEasy} enemy={enemyTransform.name} " +
                $"margin={missedEnemyForwardMargin:F2} playerX={transform.position.x:F2} " +
                $"enemyX={enemyTransform.position.x:F2}",
                this);
        }

        if (endEpisodeOnMissedEnemyEasy)
        {
            EndEpisode();
        }
    }

    private bool TryGetCoinForMissedObjectiveCheck(out ScoreAttackCoin coin)
    {
        coin = null;
        if (scoreMaxManager == null)
        {
            return false;
        }

        if (!detectAnyMissedObjectiveBehind)
        {
            return scoreMaxManager.TryGetNearestActiveCoin(transform.position, out coin, out _);
        }

        float forwardDirection = GetScoreMaxForwardDirection();
        float greatestMissedDistance = float.NegativeInfinity;
        IReadOnlyList<ScoreAttackCoin> coins = scoreMaxManager.Coins;

        for (int i = 0; i < coins.Count; i++)
        {
            ScoreAttackCoin candidate = coins[i];
            if (candidate == null || !candidate.IsAvailable)
            {
                continue;
            }

            float missedDistance =
                (transform.position.x - candidate.transform.position.x) * forwardDirection;
            if (missedDistance > greatestMissedDistance)
            {
                greatestMissedDistance = missedDistance;
                coin = candidate;
            }
        }

        return coin != null;
    }

    private bool TryGetEnemyForMissedObjectiveCheck(out ScoreAttackAndroid enemy)
    {
        enemy = null;
        if (scoreMaxManager == null)
        {
            return false;
        }

        if (!detectAnyMissedObjectiveBehind)
        {
            return scoreMaxManager.TryGetNearestActiveEnemy(transform.position, out enemy, out _);
        }

        float forwardDirection = GetScoreMaxForwardDirection();
        float greatestMissedDistance = float.NegativeInfinity;
        IReadOnlyList<ScoreAttackAndroid> enemies = scoreMaxManager.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            ScoreAttackAndroid candidate = enemies[i];
            if (candidate == null || !candidate.IsAlive || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            float missedDistance =
                (transform.position.x - candidate.transform.position.x) * forwardDirection;
            if (missedDistance > greatestMissedDistance)
            {
                greatestMissedDistance = missedDistance;
                enemy = candidate;
            }
        }

        return enemy != null;
    }

    private void ApplyNextObjectiveProgressReward(ActionBuffers actions)
    {
        if (!TryGetCurrentNextObjectiveState(
                out Transform currentTarget,
                out ScoreMaxObjectiveType currentObjectiveType,
                out float currentDistance))
        {
            previousNextObjectiveDistance = -1f;
            previousNextObjectiveTarget = null;
            previousNextObjectiveType = ScoreMaxObjectiveType.None;
            return;
        }

        bool objectiveChanged =
            previousNextObjectiveTarget != currentTarget ||
            previousNextObjectiveType != currentObjectiveType;
        if (previousNextObjectiveDistance < 0f || objectiveChanged)
        {
            previousNextObjectiveDistance = currentDistance;
            previousNextObjectiveTarget = currentTarget;
            previousNextObjectiveType = currentObjectiveType;

            if (debugScoreMaxRewards && Time.time >= nextScoreMaxDebugLogTime)
            {
                nextScoreMaxDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
                Debug.Log(
                    $"[SCOREMAX PROGRESS] objective={FormatScoreMaxObjectiveType(currentObjectiveType)} " +
                    $"reset=True prev={currentDistance:F2} current={currentDistance:F2} delta=0.000 reward=0.000",
                    this);
            }

            return;
        }

        float previousDistance = previousNextObjectiveDistance;
        float delta = previousDistance - currentDistance;
        float reward = 0f;

        if (delta > 0f)
        {
            reward = Mathf.Clamp(
                delta * progressToNextObjectiveRewardScale,
                0f,
                maxProgressToNextObjectiveReward);
        }
        else if (delta < 0f)
        {
            reward = Mathf.Clamp(
                delta * progressToNextObjectiveRewardScale,
                maxRegressionFromNextObjectivePenalty,
                0f);
        }

        float groundCoinReward = CalculateGroundCoinApproachReward(
            actions,
            currentTarget,
            currentObjectiveType,
            delta);
        reward += groundCoinReward;

        if (!Mathf.Approximately(reward, 0f))
        {
            AddReward(reward);
        }

        if (debugScoreMaxRewards && Time.time >= nextScoreMaxDebugLogTime)
        {
            nextScoreMaxDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
            Debug.Log(
                $"[SCOREMAX PROGRESS] objective={FormatScoreMaxObjectiveType(currentObjectiveType)} " +
                $"prev={previousDistance:F2} current={currentDistance:F2} delta={delta:F3} " +
                $"groundCoinReward={groundCoinReward:F3} reward={reward:F3}",
                this);
        }

        previousNextObjectiveDistance = currentDistance;
        previousNextObjectiveTarget = currentTarget;
        previousNextObjectiveType = currentObjectiveType;
    }

    private float CalculateGroundCoinApproachReward(
        ActionBuffers actions,
        Transform target,
        ScoreMaxObjectiveType objectiveType,
        float progressDelta)
    {
        if (!enableScoreMaxContextualShaping ||
            groundCoinApproachReward <= 0f ||
            objectiveType != ScoreMaxObjectiveType.Coin ||
            target == null ||
            progressDelta <= 0f ||
            !IsScoreMaxGroundedForDebug())
        {
            return 0f;
        }

        ActionSegment<int> discreteActions = actions.DiscreteActions;
        int jumpAction = discreteActions.Length > 1 ? discreteActions[1] : 0;
        Vector2 objectiveDelta = target.position - transform.position;
        float forwardDistance = objectiveDelta.x * GetScoreMaxForwardDirection();
        if (jumpAction == 1 ||
            !IsLowCoinDelta(objectiveDelta.y) ||
            forwardDistance <= 0f ||
            forwardDistance > lowCoinApproachRange)
        {
            return 0f;
        }

        return groundCoinApproachReward * Mathf.Clamp01(progressDelta / 0.1f);
    }

    private bool IsLowCoinDelta(float deltaY)
    {
        return deltaY <= Mathf.Max(0f, lowCoinHeightThreshold);
    }

    private float GetScoreMaxForwardDirection()
    {
        Transform goalTransform = scoreMaxGoal != null
            ? scoreMaxGoal
            : scoreMaxManager != null
                ? scoreMaxManager.Goal
                : null;

        if (goalTransform == null)
        {
            return 1f;
        }

        float dx = goalTransform.position.x - transform.position.x;
        return Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : 1f;
    }

    private static float NormalizeSigned(float value, float maxAbsValue)
    {
        return Mathf.Clamp(value / Mathf.Max(0.0001f, maxAbsValue), -1f, 1f);
    }

    private static float NormalizePositive(float value, float maxValue)
    {
        return Mathf.Clamp01(value / Mathf.Max(0.0001f, maxValue));
    }

    private void OnValidate()
    {
        maxObjectiveDistance = Mathf.Max(1f, maxObjectiveDistance);
        maxObjectiveHeight = Mathf.Max(1f, maxObjectiveHeight);
        maxCoinCountForObservation = Mathf.Max(1f, maxCoinCountForObservation);
        maxEnemyCountForObservation = Mathf.Max(1f, maxEnemyCountForObservation);
        maxProgressToNextObjectiveReward = Mathf.Max(0f, maxProgressToNextObjectiveReward);
        maxRegressionFromNextObjectivePenalty = Mathf.Min(0f, maxRegressionFromNextObjectivePenalty);
        scoreMaxUselessJumpPenalty = Mathf.Min(0f, scoreMaxUselessJumpPenalty);
        lowCoinHeightThreshold = Mathf.Max(0f, lowCoinHeightThreshold);
        lowCoinApproachRange = Mathf.Max(0f, lowCoinApproachRange);
        groundCoinApproachReward = Mathf.Max(0f, groundCoinApproachReward);
        lowCoinUnnecessaryJumpPenalty = Mathf.Min(0f, lowCoinUnnecessaryJumpPenalty);
        contextualCoinJumpRange = Mathf.Max(0f, contextualCoinJumpRange);
        contextualEnemyJumpRange = Mathf.Max(0f, contextualEnemyJumpRange);
        contextualGapProbeDistance = Mathf.Max(0.1f, contextualGapProbeDistance);
        contextualGapProbeDepth = Mathf.Max(0.2f, contextualGapProbeDepth);
        enemyStompAlignmentHorizontalTolerance =
            Mathf.Max(0f, enemyStompAlignmentHorizontalTolerance);
        missedCoinForwardMargin = Mathf.Max(0f, missedCoinForwardMargin);
        coinJumpCueHorizontalRange = Mathf.Max(0f, coinJumpCueHorizontalRange);
        missedEnemyForwardMargin = Mathf.Max(0f, missedEnemyForwardMargin);
        enemyStompCueHorizontalRange = Mathf.Max(0f, enemyStompCueHorizontalRange);
        enemyRayRange = Mathf.Max(0.1f, enemyRayRange);
        debugScoreMaxLogInterval = Mathf.Max(0.05f, debugScoreMaxLogInterval);
    }
}
