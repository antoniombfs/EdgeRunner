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

    [Header("ScoreMax Enemy Rays")]
    [SerializeField] private float enemyRayRange = 6f;
    [SerializeField] private float frontLowRayHeight = 0.35f;
    [SerializeField] private float frontMidRayHeight = 1.0f;
    [SerializeField] private float downForwardRayOffsetX = 1.2f;
    [SerializeField] private float downForwardRayHeight = 1.0f;

    [Header("ScoreMax Debug")]
    [SerializeField] private bool debugScoreMaxObservations = false;
    [SerializeField] private bool debugScoreMaxObservationCount = false;
    [SerializeField] private bool debugScoreMaxHeuristicInput = false;
    [SerializeField] private bool debugScoreMaxGroundCheck = false;
    [SerializeField] private bool debugScoreMaxRewards = false;
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
    private float nextScoreMaxDebugLogTime;
    private float nextScoreMaxHeuristicDebugLogTime;
    private float nextScoreMaxGroundDebugLogTime;
    private bool loggedScoreMaxObservationCountThisEpisode;
    private bool warnedScoreMaxObservationMismatchThisEpisode;

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
        previousNextObjectiveDistance = GetCurrentNextObjectiveDistance();
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
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        base.WriteDiscreteActionMask(actionMask);

        if (actionMask == null || !IsScoreMaxHeuristicOnly())
        {
            return;
        }

        // ScoreMax needs manual jumps for stomp/collectible testing even when
        // the base V5 gap mask considers the jump unnecessary.
        actionMask.SetActionEnabled(1, 1, true);
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
        float beforeDistance = GetCurrentNextObjectiveDistance();

        base.OnActionReceived(actions);

        ApplyNextObjectiveProgressReward(beforeDistance);
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

    private float GetCurrentNextObjectiveDistance()
    {
        if (scoreMaxManager == null ||
            !scoreMaxManager.TryGetRecommendedNextObjective(
                transform.position,
                out _,
                out _,
                out float distance))
        {
            return -1f;
        }

        return distance;
    }

    private void ApplyNextObjectiveProgressReward(float beforeDistance)
    {
        if (scoreMaxManager == null || beforeDistance < 0f)
        {
            previousNextObjectiveDistance = GetCurrentNextObjectiveDistance();
            return;
        }

        float afterDistance = GetCurrentNextObjectiveDistance();

        if (afterDistance < 0f)
        {
            previousNextObjectiveDistance = afterDistance;
            return;
        }

        float delta = beforeDistance - afterDistance;

        if (delta > 0f)
        {
            AddReward(Mathf.Clamp(
                delta * progressToNextObjectiveRewardScale,
                0f,
                maxProgressToNextObjectiveReward));
        }
        else if (delta < 0f)
        {
            AddReward(Mathf.Clamp(
                delta * progressToNextObjectiveRewardScale,
                maxRegressionFromNextObjectivePenalty,
                0f));
        }

        if (debugScoreMaxRewards && Time.time >= nextScoreMaxDebugLogTime)
        {
            nextScoreMaxDebugLogTime = Time.time + Mathf.Max(0.05f, debugScoreMaxLogInterval);
            Debug.Log(
                $"[SCOREMAX NEXT OBJECTIVE] before={beforeDistance:F2} after={afterDistance:F2} delta={delta:F3}",
                this);
        }

        previousNextObjectiveDistance = afterDistance;
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
        enemyRayRange = Mathf.Max(0.1f, enemyRayRange);
        debugScoreMaxLogInterval = Mathf.Max(0.05f, debugScoreMaxLogInterval);
    }
}
