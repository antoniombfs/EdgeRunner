using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Serialization;

public class EdgeRunnerAgentV5EnemyAware : Agent
{
    /*
     * EdgeRunner V5 Enemy-Aware vector observations with default sensor counts:
     * - 12 agent/goal state observations.
     * - 12 terrain summary observations.
     * - 17 forward terrain profile samples.
     * - 7 backward terrain profile samples.
     * - 7 vertical clearance samples.
     * - 8 enemy awareness observations.
     *
     * Default Vector Observation Space Size = 12 + 12 + 17 + 7 + 7 + 8 = 63.
     * Formula if sensor counts change:
     * Vector Observation Space Size =
     *     BaseObservationCount + TerrainSummaryObservationCount
     *     + forwardTerrainSampleCount + backwardTerrainSampleCount + verticalSensorCount
     *     + EnemyObservationCount.
     */

    private const int BaseObservationCount = 12;
    private const int TerrainSummaryObservationCount = 12;
    private const int EnemyObservationCount = 8;
    public const int DefaultExpectedObservationSize = 63;

    private const int MoveLeftAction = 0;
    private const int StopAction = 1;
    private const int MoveRightAction = 2;
    private const int NoJumpAction = 0;
    private const int JumpAction = 1;
    private const int NoSprintAction = 0;
    private const int SprintAction = 1;
    private const float HorizontalGoalDirectionDeadZone = 0.05f;
    private const float EnemyActionMaskMaxNearGroundVelocityY = 0.1f;
    private const int EnemyRayCount = 4;
    private const int EnemyRayFrontLow = 0;
    private const int EnemyRayFrontMid = 1;
    private const int EnemyRayBackMid = 2;
    private const int EnemyRayDownForward = 3;
    // front rays = avoidance/patrol threat; back ray = patrol awareness; down_forward = future stomp awareness.
    private static readonly float[] EnemyRayVerticalOffsets = { 0.1f, 0.5f, 0.5f, 0.35f };

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private GapGenerator gapGenerator;
    [SerializeField] private bool useMixedLevelGenerator = false;
    [SerializeField] private MixedLevelGenerator mixedLevelGenerator;
    [SerializeField] private EdgeRunnerEvaluationManager evaluationManager;

    [Header("Movement")]
    [SerializeField] private float normalMoveSpeed = 8.0f;
    [SerializeField] private float sprintMoveSpeed = 11.0f;
    [SerializeField] private float jumpForce = 12.8f;
    [SerializeField] private bool allowJump = true;
    [SerializeField] private bool allowSprint = true;

    [Header("Jump Forgiveness")]
    [SerializeField] private bool useCoyoteTime = true;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private bool useJumpBuffer = true;
    [SerializeField] private float jumpBufferTime = 0.10f;
    [SerializeField] private bool useJumpBufferForAgent = false;
    [SerializeField] private bool requireJumpReleaseBeforeNextJump = true;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [FormerlySerializedAs("groundCheckRadius")]
    [SerializeField] private float groundCheckRange = 0.15f;

    [Header("Terrain Sensors")]
    [SerializeField] private int forwardTerrainSampleCount = 17;
    [SerializeField] private float forwardSensorRange = 14.0f;
    [SerializeField] private int backwardTerrainSampleCount = 7;
    [SerializeField] private float backwardSensorRange = 5.0f;
    [SerializeField] private int verticalSensorCount = 7;
    [SerializeField] private float verticalSensorRange = 5.0f;
    [SerializeField] private float gapSensorRange = 14.0f;
    [SerializeField] private float firstTerrainSampleOffset = 0.35f;
    [SerializeField] private float immediateAheadDistance = 0.7f;
    [FormerlySerializedAs("scanDownDistance")]
    [SerializeField] private float frontDownSensorRange = 5.0f;
    [SerializeField] private float sensorVerticalOffset = 0.15f;
    [SerializeField] private float safeDropThreshold = 0.35f;
    [SerializeField] private float maxExpectedGapWidth = 4.5f;
    [SerializeField] private float maxExpectedHeightDelta = 2f;

    [Header("Wall Detection")]
    [FormerlySerializedAs("wallRayDistance")]
    [SerializeField] private float wallSensorRange = 1.2f;
    [SerializeField] private float wallRayVerticalOffset = 0.15f;

    [Header("Jump Decision Control")]
    [SerializeField] private bool maskUselessJumps = true;
    [SerializeField] private float maxGapDistanceForUsefulJump = 1.75f;
    [SerializeField] private float minGapDistanceForUsefulJump = 0.05f;
    [SerializeField] private float earlyGapJumpPenalty = -0.025f;
    [SerializeField] private float uselessJumpPenalty = -0.04f;
    [SerializeField] private bool useAdaptiveJumpWindow = true;
    [SerializeField] private bool requireLandingForUsefulGapJump = true;
    [SerializeField] private float smallGapWidthReference = 1.5f;
    [SerializeField] private float largeGapWidthReference = 2.8f;
    [SerializeField] private float smallGapMaxJumpDistance = 2.05f;
    [SerializeField] private float largeGapMaxJumpDistance = 2.65f;

    [Header("Movement Action Masking")]
    [SerializeField] private bool maskMoveAwayFromGoal = false;
    [SerializeField] private bool allowBacktrackingForPositioning = true;
    [SerializeField] private float maxAllowedBacktrackDistance = 3.0f;
    [SerializeField] private bool allowIdleAction = true;

    [Header("Rewards")]
    [SerializeField] private float goalReward = 10f;
    [SerializeField] private float deathPenalty = -4f;
    [SerializeField] private float stuckPenalty = -1f;
    [SerializeField] private float stepPenalty = -0.0003f;
    [SerializeField] private float progressRewardScale = 0.05f;
    [SerializeField] private float maxProgressRewardPerStep = 0.05f;
    [SerializeField] private float milestoneReward = 0.02f;
    [SerializeField] private float backtrackPenalty = -0.006f;
    [SerializeField] private float jumpPenalty = -0.0002f;
    [SerializeField] private float idleJumpPenalty = -0.01f;
    [SerializeField] private float flatGroundJumpPenalty = -0.015f;
    [SerializeField] private float gapJumpReward = 0.06f;
    [SerializeField] private float gapLandingReward = 0.25f;
    [SerializeField] private float lowMomentumJumpPenalty = -0.01f;
    [SerializeField] private float minJumpMomentum = 0.35f;

    [Header("Locomotion Rewards")]
    [SerializeField] private float forwardActionReward = 0.003f;
    [SerializeField] private float forwardVelocityReward = 0.002f;
    [SerializeField] private float idlePenalty = -0.002f;
    [SerializeField] private float wrongDirectionActionPenalty = -0.006f;
    [SerializeField] private float minUsefulMoveInput = 0.1f;
    [SerializeField] private float minUsefulForwardVelocity = 0.15f;

    [Header("Distance Progress Reward")]
    [SerializeField] private float distanceProgressRewardScale = 0.08f;
    [SerializeField] private float maxDistanceProgressReward = 0.08f;
    [SerializeField] private float distanceRegressionPenaltyScale = 0.04f;
    [SerializeField] private float maxDistanceRegressionPenalty = -0.04f;

    [Header("Early Failure Control")]
    [SerializeField] private float minDistanceProgressForReset = 0.05f;
    [SerializeField] private float noProgressTimeLimit = 8f;

    [Header("Episode Control")]
    [SerializeField] private float stuckTimeLimit = 8f;
    [SerializeField] private float bestXProgressThreshold = 0.25f;
    [SerializeField] private float maxEpisodeTime = 45f;

    [Header("Micro Curriculum Controls")]
    [SerializeField] private bool enableRetreatPenalty = false;
    [SerializeField] private float retreatPenalty = -0.02f;
    [SerializeField] private float retreatEndDistance = 2.0f;
    [SerializeField] private bool enableShortMicroTimeout = false;
    [SerializeField] private float microTimeoutSeconds = 6.0f;
    [SerializeField] private float microTimeoutPenalty = -2.0f;

    [Header("Goal Detection")]
    [SerializeField] private float goalReachDistance = 0.75f;

    [Header("Enemy Awareness")]
    [Tooltip("Adds 8 vector observations for nearby Android/Cyborg hazards. Disable only for debugging.")]
    [SerializeField] private bool enableEnemyAwareness = true;
    [SerializeField] private string enemyTag = "Enemy";
    [FormerlySerializedAs("enemySensorRangeX")]
    [SerializeField] private float enemyDetectionRangeX = 12f;
    [FormerlySerializedAs("enemySensorRangeY")]
    [SerializeField] private float enemyDetectionRangeY = 5f;
    [SerializeField] private int enemyObservationSlots = 2;
    [SerializeField] private bool useEnemyRayObservations = false;
    [SerializeField] private float enemyHitPenalty = -2.5f;
    [SerializeField] private bool rewardPassedEnemies = true;
    [SerializeField] private float enemyPassReward = 0.35f;
    [SerializeField] private float enemyPassMargin = 0.9f;
    [SerializeField] private float enemyDangerProximityPenalty = -0.005f;
    [SerializeField] private float enemyDangerProximityHorizontalRange = 1.5f;
    [SerializeField] private float enemyDangerProximityVerticalTolerance = 1.25f;
    [SerializeField] private float enemyApproachPenalty = -0.01f;
    [SerializeField] private float enemyJumpCueReward = 0.05f;
    [SerializeField] private float earlyEnemyJumpPenalty = 0f;
    [SerializeField] private bool enableJumpCommitReward = false;
    [SerializeField] private float jumpCommitReward = 1.0f;
    [SerializeField] private float enemyAvoidanceWindowX = 3.0f;
    [SerializeField] private float enemyVerticalDangerTolerance = 1.0f;
    [SerializeField] private float enemyJumpCueMinUpVelocity = 0.5f;
    [SerializeField] private bool disableProgressRewardNearEnemy = false;

    [Header("Enemy Action Masking")]
    [SerializeField] private bool maskForwardActionNearEnemy = false;
    [SerializeField] private float enemyActionMaskWindowX = 5.0f;
    [SerializeField] private float enemyActionMaskVerticalTolerance = 1.2f;
    [SerializeField] private bool forceJumpActionNearEnemy = false;
    [SerializeField] private float enemyForcedJumpWindowX = 2.5f;
    [SerializeField] private float enemyForcedJumpMinDistance = 2.6f;
    [SerializeField] private float enemyForcedJumpMaxDistance = 3.8f;
    [SerializeField] private float enemyForcedJumpVerticalTolerance = 1.2f;
    [SerializeField] private bool forceJumpOnlyOncePerEnemy = true;
    [SerializeField] private bool enableJumpCommitMask = false;
    [SerializeField] private bool jumpCommitOnlyOncePerEnemy = true;
    [SerializeField] private float jumpCommitMinDistance = 2.6f;
    [SerializeField] private float jumpCommitMaxDistance = 3.8f;
    [SerializeField] private bool debugEnemyActionMask = false;
    [SerializeField] private bool debugForcedJumpMask = false;
    [SerializeField] private bool debugForcedJumpTiming = false;
    [SerializeField] private bool debugJumpCommitMask = false;
    [SerializeField] private bool maskPrematureEnemyJumps = false;
    [SerializeField] private float prematureJumpMinThreatDistance = 2.6f;
    [SerializeField] private float prematureJumpMaxThreatDistance = 3.8f;
    [SerializeField] private bool debugPrematureJumpMask = false;
    [SerializeField] private bool useRuleBasedCommitTest = false;
    [SerializeField] private bool enableAirCommitAfterJump = false;
    [SerializeField] private float airCommitDuration = 0.75f;
    [SerializeField] private bool airCommitUntilEnemyPassed = true;
    [SerializeField] private bool debugAirCommit = false;

    [Header("Episode Start Settle")]
    [SerializeField] private bool waitUntilGroundedOnEpisodeStart = false;
    [SerializeField] private float episodeStartSettleMaxSeconds = 1.0f;
    [SerializeField] private bool episodeStartSettleFreezeMovement = true;
    [SerializeField] private bool debugEpisodeStartSettle = false;

    [Header("Debug")]
    [SerializeField] private bool debugV5Actions = false;
    [SerializeField] private bool debugEnemyAwareActions = false;
    [SerializeField] private bool debugEnemyAwareProgress = false;
    [SerializeField] private float debugActionLogInterval = 1.0f;
    [SerializeField] private bool debugEnemyObservations = false;
    [SerializeField] private bool debugEnemyRewards = false;
    [SerializeField] private bool debugEnemyRayObservations = false;
    [SerializeField] private bool debugTrainingActionStats = false;
    [SerializeField] private int debugTrainingActionStatsInterval = 1000;
    [SerializeField] private bool debugActionTrace = false;
    [SerializeField] private float debugActionTraceInterval = 0.25f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float bestXReached;
    private float timeSinceBestXProgress;
    private float episodeTime;
    private float previousGoalDistanceX;
    private float previousDistanceToGoal;
    private float bestDistanceToGoal;
    private float lastDistanceImprovement;
    private float timeSinceDistanceProgress;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private int lastJumpAction;
    private int trainingActionDecisionCount;
    private int trainingActionJumpCount;
    private int trainingActionJumpNearEnemyCount;
    private int trainingActionForcedJumpMaskCount;
    private int trainingActionMaskedNoJumpCount;
    private int trainingActionJumpAttemptCount;
    private int trainingActionJumpAppliedCount;
    private int actionTraceJumpsAppliedCount;
    private int trainingActionJumpBlockedNotGroundedCount;
    private int trainingActionEarlyJumpCount;
    private int trainingActionSweetSpotJumpCount;
    private int trainingActionLateJumpCount;
    private int trainingActionJumpCommitMaskCount;
    private int trainingActionJumpCommitAppliedCount;
    private int trainingActionCommitMaskActiveCount;
    private int trainingActionCommitMaskButMoveNotRightCount;
    private int trainingActionCommitMaskButJumpNotSelectedCount;
    private int trainingActionCommitMaskRightJumpSelectedCount;
    private int trainingActionCommitMaskRightJumpAppliedCount;
    private int trainingActionAirCommitStartsCount;
    private int trainingActionAirCommitActiveStepsCount;
    private int trainingActionAirCommitEndsPassedCount;
    private int trainingActionAirCommitEndsDurationCount;
    private int trainingActionAirCommitEndsHitCount;
    private int trainingActionPrematureJumpMaskCount;
    private int trainingActionPrematureJumpAttemptCount;
    private int trainingActionAllowedSweetSpotJumpCount;
    private int trainingActionRightJumpActionCount;
    private int trainingActionRetreatActionCount;
    private int trainingActionStallActionCount;
    private int trainingActionMicroTimeoutCount;
    private int lastEnemyActionMaskDebugFrame = -1;
    private int lastForcedJumpMaskDebugFrame = -1;
    private int lastForcedJumpTimingDebugFrame = -1;
    private int lastJumpCommitMaskDebugFrame = -1;
    private int lastJumpCommitMaskActiveDebugFrame = -1;
    private int lastJumpCommitAppliedDebugFrame = -1;
    private int lastPrematureJumpMaskDebugFrame = -1;
    private int lastRightActionNegativeVelocityWarningFrame = -1;

    private bool wasGroundedLastStep;
    private bool jumpedForGap;
    private bool crossedGapInAir;
    private bool jumpConsumedUntilLanding;
    private bool leftGroundAfterJump;
    private bool waitingForJumpRelease;
    private bool heuristicJumpPressedThisStep;
    private bool episodeEnding;
    private bool warnedGoalMissingThisEpisode;
    private bool lastMaskLeftBlocked;
    private bool lastMaskRightBlocked;
    private bool lastMaskStopBlocked;
    private bool jumpCommitMaskActiveForDecision;
    private bool episodeStartSettling;
    private bool episodeStartSettleTimedOut;
    private bool airCommitActive;
    private EnemyRayProbe jumpCommitThreatForDecision;
    private Transform airCommitEnemyTransform;
    private string airCommitEnemyName = "none";
    private int lastEpisodeEndFrame = -999;
    private string lastEndReason = "None";
    private float episodeStartRealtime;
    private float episodeStartSettleElapsed;
    private float airCommitEndTime;
    private float nextDebugActionLogTime;
    private float nextDebugProgressLogTime;
    private float nextDebugEnemyRewardLogTime;
    private float nextDebugEnemyRayLogTime;
    private float nextDebugActionTraceTime;
    private float nextEpisodeStartSettleLogTime;
    private float nextAirCommitLogTime;
    private readonly HashSet<Transform> rewardedEnemyTransforms = new HashSet<Transform>();
    private readonly HashSet<Transform> jumpCueRewardedEnemyTransforms = new HashSet<Transform>();
    private readonly HashSet<Transform> forcedJumpMaskedEnemyTransforms = new HashSet<Transform>();
    private readonly HashSet<Transform> jumpCommitMaskedEnemyTransforms = new HashSet<Transform>();
    private readonly HashSet<Transform> jumpCommitRewardedEnemyTransforms = new HashSet<Transform>();
    private readonly List<EnemyCandidate> enemyObservationCandidates = new List<EnemyCandidate>(2);

    public int ExpectedObservationSize =>
        BaseObservationCount +
        TerrainSummaryObservationCount +
        Mathf.Max(1, forwardTerrainSampleCount) +
        Mathf.Max(1, backwardTerrainSampleCount) +
        Mathf.Max(1, verticalSensorCount) +
        EnemyObservationCount;

    public override void Initialize()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (evaluationManager == null)
        {
            evaluationManager = FindAnyObjectByType<EdgeRunnerEvaluationManager>();
        }
    }

    public override void OnEpisodeBegin()
    {
        if (useMixedLevelGenerator && mixedLevelGenerator != null)
        {
            mixedLevelGenerator.GenerateEpisode();

            transform.position = mixedLevelGenerator.AgentSpawnPosition;
            transform.rotation = startRotation;
            goal = mixedLevelGenerator.CurrentGoal;

            if (goal == null)
            {
                Debug.LogWarning("EdgeRunnerAgentV5EnemyAware: MixedLevelGenerator generated episode but CurrentGoal is null.");
            }
        }
        else if (gapGenerator != null)
        {
            gapGenerator.GenerateEpisode();

            transform.position = gapGenerator.AgentSpawnPosition;
            transform.rotation = startRotation;
            goal = gapGenerator.CurrentGoal;
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        float direction = GetForwardDirection();
        bestXReached = transform.position.x * direction;
        timeSinceBestXProgress = 0f;
        episodeTime = 0f;
        previousGoalDistanceX = GetGoalDistanceX();
        previousDistanceToGoal = GetDistanceToGoal();
        bestDistanceToGoal = previousDistanceToGoal;
        lastDistanceImprovement = 0f;
        timeSinceDistanceProgress = 0f;
        wasGroundedLastStep = IsGrounded();
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        lastJumpAction = NoJumpAction;
        jumpedForGap = false;
        crossedGapInAir = false;
        jumpConsumedUntilLanding = false;
        leftGroundAfterJump = false;
        waitingForJumpRelease = false;
        heuristicJumpPressedThisStep = false;
        episodeEnding = false;
        warnedGoalMissingThisEpisode = false;
        lastMaskLeftBlocked = false;
        lastMaskRightBlocked = false;
        lastMaskStopBlocked = false;
        jumpCommitMaskActiveForDecision = false;
        episodeStartSettling = waitUntilGroundedOnEpisodeStart;
        episodeStartSettleTimedOut = false;
        airCommitActive = false;
        airCommitEnemyTransform = null;
        airCommitEnemyName = "none";
        airCommitEndTime = 0f;
        nextAirCommitLogTime = 0f;
        jumpCommitThreatForDecision = default;
        episodeStartRealtime = Time.realtimeSinceStartup;
        episodeStartSettleElapsed = 0f;
        nextDebugActionLogTime = 0f;
        nextDebugProgressLogTime = 0f;
        nextDebugEnemyRewardLogTime = 0f;
        nextDebugEnemyRayLogTime = 0f;
        nextDebugActionTraceTime = 0f;
        nextEpisodeStartSettleLogTime = 0f;
        rewardedEnemyTransforms.Clear();
        jumpCueRewardedEnemyTransforms.Clear();
        forcedJumpMaskedEnemyTransforms.Clear();
        jumpCommitMaskedEnemyTransforms.Clear();
        jumpCommitRewardedEnemyTransforms.Clear();

        NotifyEvaluationEpisodeStarted();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float direction = GetForwardDirection();
        TerrainAnalysis terrain = AnalyzeTerrain(direction);
        List<EnemyCandidate> enemyObservation = useEnemyRayObservations
            ? null
            : AnalyzeEnemies(direction);
        float maxHorizontalSpeed = GetMaxHorizontalSpeed();

        float velX = rb != null ? rb.linearVelocity.x / maxHorizontalSpeed : 0f;
        float velY = rb != null ? rb.linearVelocity.y / 15f : 0f;
        bool grounded = IsGrounded();
        bool coyoteActive = !grounded && useCoyoteTime && coyoteTimer > 0f;
        bool canJumpNow = allowJump && CanJumpFromGroundState(grounded) && !jumpConsumedUntilLanding && !waitingForJumpRelease;
        bool movingBackwardRelativeToGoal = rb != null && rb.linearVelocity.x * direction < -minUsefulForwardVelocity;

        Vector2 toGoal = goal != null
            ? (Vector2)(goal.position - transform.position)
            : Vector2.zero;

        Vector2 goalDirection = toGoal.sqrMagnitude > 0.0001f
            ? toGoal.normalized
            : Vector2.zero;

        // 12 agent/goal state observations.
        sensor.AddObservation(Mathf.Clamp(velX, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(velY, -1f, 1f));
        sensor.AddObservation(grounded ? 1f : 0f);
        sensor.AddObservation(coyoteActive ? 1f : 0f);
        sensor.AddObservation(canJumpNow ? 1f : 0f);
        sensor.AddObservation(allowSprint ? 1f : 0f);
        sensor.AddObservation(movingBackwardRelativeToGoal ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp(toGoal.x / 30f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toGoal.y / 15f, -1f, 1f));
        sensor.AddObservation(goalDirection.x);
        sensor.AddObservation(goalDirection.y);
        sensor.AddObservation(Mathf.Clamp01(toGoal.magnitude / 35f));

        // 12 terrain summary observations.
        sensor.AddObservation(terrain.hasGroundImmediatelyAhead ? 1f : 0f);
        sensor.AddObservation(terrain.distanceToGapStartNormalized);
        sensor.AddObservation(terrain.hasGapAhead ? 1f : 0f);
        sensor.AddObservation(terrain.distanceToLandingNormalized);
        sensor.AddObservation(terrain.estimatedGapWidthNormalized);
        sensor.AddObservation(terrain.landingDeltaYNormalized);
        sensor.AddObservation(terrain.hasLanding ? 1f : 0f);
        sensor.AddObservation(terrain.safeDropAhead ? 1f : 0f);
        sensor.AddObservation(terrain.flatGroundAhead ? 1f : 0f);
        sensor.AddObservation(terrain.wallAhead ? 1f : 0f);
        sensor.AddObservation(terrain.wallDistanceNormalized);
        sensor.AddObservation(terrain.nextGapOrObstacleDistanceNormalized);

        // forwardTerrainSampleCount forward profile observations. Default: 17.
        for (int i = 0; i < terrain.forwardSamples.Length; i++)
        {
            sensor.AddObservation(terrain.forwardSamples[i]);
        }

        // backwardTerrainSampleCount backward profile observations. Default: 7.
        for (int i = 0; i < terrain.backwardSamples.Length; i++)
        {
            sensor.AddObservation(terrain.backwardSamples[i]);
        }

        // verticalSensorCount vertical clearance observations. Default: 7.
        for (int i = 0; i < terrain.verticalSamples.Length; i++)
        {
            sensor.AddObservation(terrain.verticalSamples[i]);
        }

        // 8 enemy awareness observations. Default total vector size: 63.
        if (useEnemyRayObservations)
        {
            AddEnemyRayObservations(sensor, direction);
        }
        else
        {
            AddEnemyObservations(sensor, enemyObservation);
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        float direction = GetForwardDirection();
        RefreshAirCommitState(direction);
        bool grounded = IsGrounded();
        jumpCommitMaskActiveForDecision = false;
        jumpCommitThreatForDecision = default;
        bool blockLeft = false;
        bool blockRight = false;
        bool blockStop = false;
        bool enemyForwardMaskActive = ShouldMaskForwardActionNearEnemy(
            direction,
            grounded,
            out int forwardActionToMask,
            out string maskedEnemyName
        );
        bool forcedJumpMaskActive = ShouldForceJumpActionNearEnemy(
            direction,
            grounded,
            out EnemyRayProbe forcedJumpThreat
        );
        bool jumpCommitMaskActive = ShouldApplyJumpCommitMask(
            direction,
            grounded,
            out EnemyRayProbe jumpCommitThreat
        );

        if (ShouldLimitBacktracking(direction))
        {
            if (direction > 0f)
            {
                blockLeft = true;
            }
            else
            {
                blockRight = true;
            }
        }

        if (!allowIdleAction)
        {
            blockStop = true;
        }

        if (enemyForwardMaskActive)
        {
            if (forwardActionToMask == MoveRightAction)
            {
                blockRight = true;
            }
            else if (forwardActionToMask == MoveLeftAction)
            {
                blockLeft = true;
            }
        }

        if (jumpCommitMaskActive)
        {
            blockLeft = true;
            blockRight = false;
            blockStop = true;
        }

        if (IsAirCommitActive())
        {
            blockLeft = true;
            blockRight = false;
            blockStop = true;
        }

        // Keep horizontal movement fail-open: the trainer must never be left with stop only.
        if (blockLeft && blockRight)
        {
            blockLeft = false;
            blockRight = false;
        }

        if (blockLeft)
        {
            actionMask.SetActionEnabled(0, MoveLeftAction, false);
        }

        if (blockRight)
        {
            actionMask.SetActionEnabled(0, MoveRightAction, false);
        }

        if (blockStop)
        {
            actionMask.SetActionEnabled(0, StopAction, false);
        }

        lastMaskLeftBlocked = blockLeft;
        lastMaskRightBlocked = blockRight;
        lastMaskStopBlocked = blockStop;

        bool forwardActionWasMasked =
            enemyForwardMaskActive &&
            ((forwardActionToMask == MoveRightAction && blockRight) ||
             (forwardActionToMask == MoveLeftAction && blockLeft));

        if (forwardActionWasMasked)
        {
            LogEnemyActionMask(maskedEnemyName);
        }

        bool canJumpImmediately =
            allowJump &&
            CanJumpFromGroundState(grounded) &&
            !jumpConsumedUntilLanding &&
            !waitingForJumpRelease;

        if (!canJumpImmediately)
        {
            actionMask.SetActionEnabled(1, JumpAction, false);
        }
        else if (jumpCommitMaskActive)
        {
            actionMask.SetActionEnabled(1, NoJumpAction, false);
            RecordJumpCommitMaskActivated();
            RecordNoJumpMasked();
            MarkJumpCommitMaskApplied(jumpCommitThreat);
            jumpCommitMaskActiveForDecision = true;
            jumpCommitThreatForDecision = jumpCommitThreat;
            LogJumpCommitMask(jumpCommitThreat);
            LogJumpCommitMaskActive(jumpCommitThreat, blockLeft, blockStop);
        }
        else if (forcedJumpMaskActive)
        {
            actionMask.SetActionEnabled(1, NoJumpAction, false);
            RecordForcedJumpMaskActivated();
            RecordNoJumpMasked();
            MarkForcedJumpApplied(forcedJumpThreat);
            LogForcedJumpMask(forcedJumpThreat);
        }
        else if (ShouldMaskPrematureEnemyJump(direction, out EnemyRayProbe prematureJumpThreat, out string prematureJumpReason))
        {
            actionMask.SetActionEnabled(1, JumpAction, false);
            RecordPrematureJumpMask();
            LogPrematureJumpMask(prematureJumpThreat, prematureJumpReason);
        }
        else if (maskUselessJumps && !enableJumpCommitMask && !enableAirCommitAfterJump)
        {
            // JumpCommit/AirCommit tutorial phases use jump to clear enemies, not terrain gaps.
            bool keepJumpAvailableForEnemyMask = forwardActionWasMasked;

            if (!keepJumpAvailableForEnemyMask)
            {
                TerrainAnalysis terrain = AnalyzeTerrain(direction);

                if (!IsUsefulJumpSituation(terrain))
                {
                    actionMask.SetActionEnabled(1, JumpAction, false);
                }
            }
        }

        if (!allowSprint)
        {
            actionMask.SetActionEnabled(2, SprintAction, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (rb == null)
        {
            return;
        }

        if (episodeEnding)
        {
            return;
        }

        if (CheckGoalReachedByDistance())
        {
            return;
        }

        // Action mapping: branch 0 = movement (0 left, 1 stop, 2 right),
        // branch 1 = jump (0 no jump, 1 jump), branch 2 = sprint (0 off, 1 on).
        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];
        int sprintAction = actions.DiscreteActions.Length > 2
            ? actions.DiscreteActions[2]
            : NoSprintAction;

        float direction = GetForwardDirection();
        bool groundedBeforeAction = IsGrounded();

        if (TryHandleEpisodeStartSettle(direction, groundedBeforeAction))
        {
            lastJumpAction = NoJumpAction;
            heuristicJumpPressedThisStep = false;
            return;
        }

        episodeTime += Time.fixedDeltaTime;

        RefreshAirCommitState(direction);
        bool airCommitForcedMovementThisDecision = ApplyAirCommitActions(ref moveAction);
        bool ruleBasedCommitJumpThisDecision = false;
        EnemyRayProbe ruleBasedCommitThreatThisDecision = default;

        if (!airCommitForcedMovementThisDecision)
        {
            ruleBasedCommitJumpThisDecision = ApplyRuleBasedCommitTestActions(
                direction,
                groundedBeforeAction,
                ref moveAction,
                ref jumpAction,
                ref sprintAction,
                out ruleBasedCommitThreatThisDecision
            );
        }

        bool commitMaskActiveThisDecision = jumpCommitMaskActiveForDecision;
        EnemyRayProbe commitMaskThreatThisDecision = jumpCommitThreatForDecision;
        TerrainAnalysis terrain = AnalyzeTerrain(direction);

        float moveX = moveAction switch
        {
            MoveLeftAction => -1f,
            MoveRightAction => 1f,
            _ => 0f
        };

        bool sprintRequested =
            sprintAction == SprintAction &&
            allowSprint &&
            Mathf.Abs(moveX) > 0.1f;

        float activeMoveSpeed = sprintRequested ? sprintMoveSpeed : normalMoveSpeed;
        rb.linearVelocity = new Vector2(moveX * activeMoveSpeed, rb.linearVelocity.y);

        bool grounded = IsGrounded();
        LogV5ActionDebug(
            moveAction,
            jumpAction,
            sprintAction,
            moveX,
            activeMoveSpeed,
            direction,
            grounded
        );
        TrackTrainingActionStats(moveAction, jumpAction, direction, grounded);

        bool suppressProgressRewardNearEnemy = ShouldSuppressProgressRewardNearEnemy(direction);
        ApplyDistanceProgressReward(suppressProgressRewardNearEnemy);
        ApplyLocomotionReward(direction, moveX, grounded, suppressProgressRewardNearEnemy);

        bool jumpRequested = jumpAction == JumpAction;
        bool jumpPressedThisStep = jumpRequested && lastJumpAction == NoJumpAction;
        bool bufferedJumpPressed = heuristicJumpPressedThisStep || (useJumpBufferForAgent && jumpPressedThisStep);
        UpdateJumpForgivenessTimers(grounded, jumpRequested, bufferedJumpPressed);
        bool jumpExecutionRequested = jumpRequested || (useJumpBuffer && jumpBufferTimer > 0f);
        bool shouldExecuteJump = ShouldExecuteJump(grounded, jumpRequested);
        RecordJumpExecutionStats(jumpRequested, jumpExecutionRequested, shouldExecuteJump, grounded);

        if (shouldExecuteJump)
        {
            HandleJumpReward(moveX, direction, terrain);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpConsumedUntilLanding = true;
            leftGroundAfterJump = !grounded;
            waitingForJumpRelease = requireJumpReleaseBeforeNextJump && jumpRequested;
        }

        if (shouldExecuteJump)
        {
            actionTraceJumpsAppliedCount++;
        }

        TrackJumpCommitActionOutcome(
            commitMaskActiveThisDecision,
            moveAction,
            jumpAction,
            shouldExecuteJump
        );
        MaybeStartAirCommitAfterRightJump(
            commitMaskActiveThisDecision,
            ruleBasedCommitJumpThisDecision,
            ruleBasedCommitJumpThisDecision ? ruleBasedCommitThreatThisDecision : commitMaskThreatThisDecision,
            direction,
            moveAction,
            jumpAction,
            shouldExecuteJump
        );
        TrackAirCommitActiveStep();
        LogRightActionNegativeVelocityWarning(moveAction);
        LogActionTrace(
            moveAction,
            jumpAction,
            sprintAction,
            commitMaskActiveThisDecision,
            commitMaskThreatThisDecision,
            direction,
            grounded,
            shouldExecuteJump
        );
        ApplyJumpCommitRewardIfNeeded(moveAction, jumpAction, shouldExecuteJump);

        lastJumpAction = jumpAction;
        heuristicJumpPressedThisStep = false;

        TrackGapLandingReward(direction, grounded);
        ApplyProgressRewards(direction, moveX);
        ApplyEnemyAvoidanceCueRewards(direction, moveX, jumpRequested, grounded);
        ApplyEnemyDangerProximityPenalty(direction);
        RewardPassedEnemies(direction);
        ApplyRetreatPenalty(direction);

        AddReward(stepPenalty);

        if (CheckMicroTimeout())
        {
            return;
        }

        if (timeSinceDistanceProgress >= noProgressTimeLimit)
        {
            AddReward(stuckPenalty);
            TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.NoProgress);
            return;
        }

        if (timeSinceBestXProgress >= stuckTimeLimit)
        {
            AddReward(stuckPenalty);
            TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Stuck);
            return;
        }

        if (episodeTime >= maxEpisodeTime)
        {
            TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Timeout);
        }
    }

    private void FixedUpdate()
    {
        CheckGoalReachedByDistance();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = StopAction;
        d[1] = NoJumpAction;
        d[2] = NoSprintAction;

        bool moveLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool moveRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (moveLeft && !moveRight)
        {
            d[0] = MoveLeftAction;
        }
        else if (moveRight && !moveLeft)
        {
            d[0] = MoveRightAction;
        }

        bool jumpHeld =
            Input.GetKey(KeyCode.Space) ||
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow);

        heuristicJumpPressedThisStep =
            allowJump &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.W) ||
             Input.GetKeyDown(KeyCode.UpArrow));

        if (allowJump && jumpHeld)
        {
            d[1] = JumpAction;
        }

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (sprintHeld)
        {
            d[2] = SprintAction;
        }
    }

    private void UpdateJumpForgivenessTimers(bool grounded, bool jumpRequested, bool bufferedJumpPressed)
    {
        if (!jumpRequested)
        {
            waitingForJumpRelease = false;
        }

        if (jumpConsumedUntilLanding)
        {
            if (grounded && leftGroundAfterJump)
            {
                jumpConsumedUntilLanding = false;
                leftGroundAfterJump = false;
            }
            else if (!grounded)
            {
                leftGroundAfterJump = true;
            }
        }

        if (useCoyoteTime)
        {
            coyoteTimer = grounded && !jumpConsumedUntilLanding
                ? coyoteTime
                : Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);
        }
        else
        {
            coyoteTimer = 0f;
        }

        if (useJumpBuffer)
        {
            jumpBufferTimer = bufferedJumpPressed && !waitingForJumpRelease
                ? jumpBufferTime
                : Mathf.Max(0f, jumpBufferTimer - Time.fixedDeltaTime);
        }
        else
        {
            jumpBufferTimer = 0f;
        }
    }

    private bool ShouldExecuteJump(bool grounded, bool jumpRequested)
    {
        if (!allowJump ||
            jumpConsumedUntilLanding ||
            waitingForJumpRelease ||
            !CanJumpFromGroundState(grounded))
        {
            return false;
        }

        return jumpRequested || (useJumpBuffer && jumpBufferTimer > 0f);
    }

    private bool CanJumpFromGroundState(bool grounded)
    {
        return grounded || (useCoyoteTime && coyoteTimer > 0f);
    }

    private void LogV5ActionDebug(
        int moveAction,
        int jumpAction,
        int sprintAction,
        float horizontalInput,
        float activeMoveSpeed,
        float goalDirectionX,
        bool grounded)
    {
        if ((!debugV5Actions && !debugEnemyAwareActions) || Time.time < nextDebugActionLogTime)
        {
            return;
        }

        nextDebugActionLogTime = Time.time + Mathf.Max(0.05f, debugActionLogInterval);
        float distanceToGoal = goal != null ? Vector2.Distance(transform.position, goal.position) : -1f;
        Vector2 currentVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

        Debug.Log(
            "[ENEMY AWARE ACTION] " +
            $"move={moveAction}, jump={jumpAction}, sprint={sprintAction}, " +
            $"horizontalInput={horizontalInput:F1}, speed={activeMoveSpeed:F2}, " +
            $"linearVelocity=({currentVelocity.x:F2}, {currentVelocity.y:F2}), " +
            $"goalDirectionX={goalDirectionX:F1}, distanceToGoal={distanceToGoal:F2}, " +
            $"maskLeft={lastMaskLeftBlocked}, maskRight={lastMaskRightBlocked}, maskStop={lastMaskStopBlocked}, " +
            $"grounded={grounded}",
            this
        );
    }

    private bool ShouldMaskForwardActionNearEnemy(
        float direction,
        bool grounded,
        out int forwardActionToMask,
        out string enemyName)
    {
        forwardActionToMask = GetForwardMoveAction(direction);
        enemyName = string.Empty;

        if (!enableEnemyAwareness || !maskForwardActionNearEnemy || episodeEnding)
        {
            return false;
        }

        if (!CanJumpFromGroundState(grounded) || jumpConsumedUntilLanding || waitingForJumpRelease)
        {
            return false;
        }

        if (useEnemyRayObservations)
        {
            if (TryFindEnemyRayThreat(direction, enemyActionMaskWindowX, EnemyRayThreatMode.FrontOnly, out EnemyRayProbe rayThreat))
            {
                enemyName = rayThreat.enemyName;
                return true;
            }

            return false;
        }

        return TryFindDangerousEnemyAhead(
            direction,
            enemyActionMaskWindowX,
            enemyActionMaskVerticalTolerance,
            out _,
            out _,
            out enemyName,
            out _
        );
    }

    private bool ShouldForceJumpActionNearEnemy(
        float direction,
        bool grounded,
        out EnemyRayProbe threat)
    {
        threat = default;

        if (!enableEnemyAwareness || !useEnemyRayObservations || !forceJumpActionNearEnemy || episodeEnding)
        {
            return false;
        }

        if (!IsGroundedOrNearlyGroundedForActionMask(grounded))
        {
            return false;
        }

        if (!TryFindEnemyRayThreat(direction, enemyForcedJumpWindowX, EnemyRayThreatMode.FrontOnly, out threat))
        {
            return false;
        }

        EnemyJumpTimingZone timingZone = GetEnemyJumpTimingZone(threat.distance);

        if (threat.enemyTransform != null)
        {
            float verticalDelta = Mathf.Abs(transform.position.y - threat.enemyTransform.position.y);

            if (verticalDelta > enemyForcedJumpVerticalTolerance)
            {
                return false;
            }
        }

        if (timingZone != EnemyJumpTimingZone.SweetSpot)
        {
            LogEnemyJumpTiming(threat, timingZone, false);
            return false;
        }

        if (forceJumpOnlyOncePerEnemy &&
            threat.enemyTransform != null &&
            forcedJumpMaskedEnemyTransforms.Contains(threat.enemyTransform))
        {
            LogEnemyJumpTiming(threat, timingZone, false);
            return false;
        }

        LogEnemyJumpTiming(threat, timingZone, true);
        return true;
    }

    private bool ShouldApplyJumpCommitMask(
        float direction,
        bool grounded,
        out EnemyRayProbe threat)
    {
        threat = default;

        if (!enableEnemyAwareness || !useEnemyRayObservations || !enableJumpCommitMask || episodeEnding)
        {
            return false;
        }

        if (!CanJumpFromGroundState(grounded) || jumpConsumedUntilLanding || waitingForJumpRelease)
        {
            return false;
        }

        float windowX = Mathf.Max(jumpCommitMaxDistance, enemyForcedJumpWindowX);

        if (!TryFindEnemyRayThreat(direction, windowX, EnemyRayThreatMode.FrontOnly, out threat))
        {
            return false;
        }

        if (threat.distance < jumpCommitMinDistance || threat.distance > jumpCommitMaxDistance)
        {
            return false;
        }

        if (threat.enemyTransform != null)
        {
            float verticalDelta = Mathf.Abs(transform.position.y - threat.enemyTransform.position.y);

            if (verticalDelta > enemyForcedJumpVerticalTolerance)
            {
                return false;
            }
        }

        if (jumpCommitOnlyOncePerEnemy &&
            threat.enemyTransform != null &&
            jumpCommitMaskedEnemyTransforms.Contains(threat.enemyTransform))
        {
            return false;
        }

        return true;
    }

    private bool ShouldMaskPrematureEnemyJump(
        float direction,
        out EnemyRayProbe threat,
        out string reason)
    {
        threat = default;
        reason = string.Empty;

        if (!enableEnemyAwareness ||
            !useEnemyRayObservations ||
            !maskPrematureEnemyJumps ||
            episodeEnding)
        {
            return false;
        }

        if (!TryFindEnemyRayThreat(direction, enemyDetectionRangeX, EnemyRayThreatMode.FrontOnly, out threat))
        {
            reason = "no_threat";
            return true;
        }

        if (threat.distance > prematureJumpMaxThreatDistance)
        {
            reason = "too_early";
            return true;
        }

        return false;
    }

    private bool TryHandleEpisodeStartSettle(float direction, bool grounded)
    {
        if (!episodeStartSettling)
        {
            return false;
        }

        if (CanJumpFromGroundState(grounded))
        {
            FinishEpisodeStartSettle();
            return false;
        }

        episodeStartSettleElapsed += Time.fixedDeltaTime;

        if (episodeStartSettleElapsed >= episodeStartSettleMaxSeconds)
        {
            episodeStartSettling = false;
            episodeStartSettleTimedOut = true;
            Debug.LogWarning(
                "[EPISODE START SETTLE] timed out before grounded " +
                $"after {episodeStartSettleElapsed:F2}s pos={FormatVector3(transform.position)} " +
                $"vel={FormatVector2(rb != null ? rb.linearVelocity : Vector2.zero)}",
                this
            );
            return false;
        }

        if (episodeStartSettleFreezeMovement && rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        LogEpisodeStartSettleWaiting();
        LogActionTrace(
            StopAction,
            NoJumpAction,
            NoSprintAction,
            false,
            default,
            direction,
            grounded,
            false
        );
        return true;
    }

    private void FinishEpisodeStartSettle()
    {
        if (!episodeStartSettling)
        {
            return;
        }

        episodeStartSettling = false;

        if (rb != null && episodeStartSettleFreezeMovement)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (debugEpisodeStartSettle)
        {
            Debug.Log(
                $"[EPISODE START SETTLE] grounded after {episodeStartSettleElapsed:F2}s " +
                $"pos={FormatVector3(transform.position)} vel={FormatVector2(rb != null ? rb.linearVelocity : Vector2.zero)}",
                this
            );
        }
    }

    private void LogEpisodeStartSettleWaiting()
    {
        if (!debugEpisodeStartSettle || Time.time < nextEpisodeStartSettleLogTime)
        {
            return;
        }

        nextEpisodeStartSettleLogTime = Time.time + 0.25f;
        Debug.Log(
            "[EPISODE START SETTLE] waiting grounded " +
            $"pos={FormatVector3(transform.position)} " +
            $"vel={FormatVector2(rb != null ? rb.linearVelocity : Vector2.zero)}",
            this
        );
    }

    private bool ApplyAirCommitActions(ref int moveAction)
    {
        if (!IsAirCommitActive())
        {
            return false;
        }

        moveAction = MoveRightAction;
        return true;
    }

    private bool ApplyRuleBasedCommitTestActions(
        float direction,
        bool grounded,
        ref int moveAction,
        ref int jumpAction,
        ref int sprintAction,
        out EnemyRayProbe commitThreat)
    {
        commitThreat = default;

        if (!useRuleBasedCommitTest || episodeEnding)
        {
            return false;
        }

        if (!CanJumpFromGroundState(grounded))
        {
            moveAction = StopAction;
            jumpAction = NoJumpAction;
            sprintAction = NoSprintAction;
            return false;
        }

        moveAction = GetForwardMoveAction(direction);
        bool shouldJump = ShouldUseRuleBasedCommitJump(direction, grounded, out commitThreat);
        jumpAction = shouldJump
            ? JumpAction
            : NoJumpAction;
        sprintAction = NoSprintAction;
        return shouldJump;
    }

    private bool ShouldUseRuleBasedCommitJump(
        float direction,
        bool grounded,
        out EnemyRayProbe threat)
    {
        threat = default;

        if (!enableEnemyAwareness || !useEnemyRayObservations)
        {
            return false;
        }

        if (!CanJumpFromGroundState(grounded) || jumpConsumedUntilLanding || waitingForJumpRelease)
        {
            return false;
        }

        float windowX = Mathf.Max(jumpCommitMaxDistance, enemyForcedJumpWindowX);

        if (!TryFindEnemyRayThreat(direction, windowX, EnemyRayThreatMode.FrontOnly, out threat))
        {
            return false;
        }

        if (threat.distance < jumpCommitMinDistance || threat.distance > jumpCommitMaxDistance)
        {
            return false;
        }

        if (threat.enemyTransform != null)
        {
            float verticalDelta = Mathf.Abs(transform.position.y - threat.enemyTransform.position.y);

            if (verticalDelta > enemyForcedJumpVerticalTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private void MaybeStartAirCommitAfterRightJump(
        bool commitMaskActive,
        bool ruleBasedCommitJump,
        EnemyRayProbe threat,
        float direction,
        int moveAction,
        int jumpAction,
        bool jumpApplied)
    {
        if (!enableAirCommitAfterJump ||
            !jumpApplied ||
            jumpAction != JumpAction)
        {
            return;
        }

        bool guidedCommitJump = commitMaskActive || ruleBasedCommitJump;

        if (guidedCommitJump && moveAction != MoveRightAction)
        {
            return;
        }

        if (!guidedCommitJump && !TryFindVoluntaryAirCommitThreat(direction, out threat))
        {
            return;
        }

        if (!threat.hasHit && useEnemyRayObservations)
        {
            float windowX = Mathf.Max(jumpCommitMaxDistance, enemyForcedJumpWindowX);
            TryFindEnemyRayThreat(direction, windowX, EnemyRayThreatMode.FrontOnly, out threat);
        }

        StartAirCommit(threat);
    }

    private bool TryFindVoluntaryAirCommitThreat(float direction, out EnemyRayProbe threat)
    {
        threat = default;

        if (!enableEnemyAwareness || !useEnemyRayObservations)
        {
            return false;
        }

        float minDistance = maskPrematureEnemyJumps
            ? prematureJumpMinThreatDistance
            : jumpCommitMinDistance;
        float maxDistance = maskPrematureEnemyJumps
            ? prematureJumpMaxThreatDistance
            : jumpCommitMaxDistance;

        if (!TryFindEnemyRayThreat(direction, maxDistance, EnemyRayThreatMode.FrontOnly, out threat))
        {
            return false;
        }

        if (threat.distance < minDistance || threat.distance > maxDistance)
        {
            return false;
        }

        if (threat.enemyTransform != null)
        {
            float verticalDelta = Mathf.Abs(transform.position.y - threat.enemyTransform.position.y);

            if (verticalDelta > enemyForcedJumpVerticalTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private void StartAirCommit(EnemyRayProbe threat)
    {
        if (!enableAirCommitAfterJump)
        {
            return;
        }

        airCommitActive = true;
        airCommitEndTime = Time.time + Mathf.Max(0.01f, airCommitDuration);
        airCommitEnemyTransform = threat.enemyTransform;
        airCommitEnemyName = string.IsNullOrEmpty(threat.enemyName)
            ? "unknown"
            : threat.enemyName;
        nextAirCommitLogTime = 0f;

        if (debugTrainingActionStats)
        {
            trainingActionAirCommitStartsCount++;
        }

        if (debugAirCommit)
        {
            Debug.Log(
                "[ENEMY AIR COMMIT START] " +
                $"enemy={airCommitEnemyName} duration={airCommitDuration:F2}",
                this
            );
        }
    }

    private void RefreshAirCommitState(float direction)
    {
        if (!airCommitActive)
        {
            return;
        }

        if (airCommitUntilEnemyPassed && HasAirCommitEnemyPassed(direction))
        {
            EndAirCommit("passed");
            return;
        }

        if (Time.time >= airCommitEndTime)
        {
            EndAirCommit("duration");
            return;
        }

        if (debugAirCommit && Time.time >= nextAirCommitLogTime)
        {
            nextAirCommitLogTime = Time.time + 0.25f;
            Debug.Log(
                "[ENEMY AIR COMMIT ACTIVE] " +
                $"remaining={GetAirCommitRemainingTime():F2} enemy={airCommitEnemyName}",
                this
            );
        }
    }

    private bool HasAirCommitEnemyPassed(float direction)
    {
        if (airCommitEnemyTransform == null)
        {
            return false;
        }

        float passedDistance = (transform.position.x - airCommitEnemyTransform.position.x) * direction;
        return passedDistance >= enemyPassMargin;
    }

    private void EndAirCommit(string reason)
    {
        if (!airCommitActive)
        {
            return;
        }

        if (debugTrainingActionStats)
        {
            if (reason == "passed")
            {
                trainingActionAirCommitEndsPassedCount++;
            }
            else if (reason == "duration")
            {
                trainingActionAirCommitEndsDurationCount++;
            }
            else if (reason == "hit")
            {
                trainingActionAirCommitEndsHitCount++;
            }
        }

        if (debugAirCommit)
        {
            Debug.Log(
                $"[ENEMY AIR COMMIT END] reason={reason} enemy={airCommitEnemyName}",
                this
            );
        }

        airCommitActive = false;
        airCommitEnemyTransform = null;
        airCommitEnemyName = "none";
        airCommitEndTime = 0f;
    }

    private bool IsAirCommitActive()
    {
        return enableAirCommitAfterJump && airCommitActive;
    }

    private float GetAirCommitRemainingTime()
    {
        return IsAirCommitActive()
            ? Mathf.Max(0f, airCommitEndTime - Time.time)
            : 0f;
    }

    private void TrackAirCommitActiveStep()
    {
        if (debugTrainingActionStats && IsAirCommitActive())
        {
            trainingActionAirCommitActiveStepsCount++;
        }
    }

    private int GetForwardMoveAction(float direction)
    {
        return direction >= 0f ? MoveRightAction : MoveLeftAction;
    }

    private bool IsGroundedOrNearlyGrounded(bool grounded)
    {
        if (grounded)
        {
            return true;
        }

        return rb != null && Mathf.Abs(rb.linearVelocity.y) <= enemyJumpCueMinUpVelocity;
    }

    private bool IsGroundedOrNearlyGroundedForActionMask(bool grounded)
    {
        if (grounded)
        {
            return true;
        }

        return rb != null && Mathf.Abs(rb.linearVelocity.y) <= EnemyActionMaskMaxNearGroundVelocityY;
    }

    private void RecordForcedJumpMaskActivated()
    {
        if (debugTrainingActionStats)
        {
            trainingActionForcedJumpMaskCount++;
        }
    }

    private void RecordNoJumpMasked()
    {
        if (debugTrainingActionStats)
        {
            trainingActionMaskedNoJumpCount++;
        }
    }

    private void RecordJumpCommitMaskActivated()
    {
        if (debugTrainingActionStats)
        {
            trainingActionJumpCommitMaskCount++;
            trainingActionCommitMaskActiveCount++;
        }
    }

    private void RecordPrematureJumpMask()
    {
        if (debugTrainingActionStats)
        {
            trainingActionPrematureJumpMaskCount++;
        }
    }

    private void MarkForcedJumpApplied(EnemyRayProbe threat)
    {
        if (!forceJumpOnlyOncePerEnemy || threat.enemyTransform == null)
        {
            return;
        }

        forcedJumpMaskedEnemyTransforms.Add(threat.enemyTransform);
    }

    private void MarkJumpCommitMaskApplied(EnemyRayProbe threat)
    {
        if (!jumpCommitOnlyOncePerEnemy || threat.enemyTransform == null)
        {
            return;
        }

        jumpCommitMaskedEnemyTransforms.Add(threat.enemyTransform);
    }

    private EnemyJumpTimingZone GetEnemyJumpTimingZone(float distance)
    {
        if (distance > enemyForcedJumpMaxDistance)
        {
            return EnemyJumpTimingZone.TooEarly;
        }

        if (distance < enemyForcedJumpMinDistance)
        {
            return EnemyJumpTimingZone.TooLate;
        }

        return EnemyJumpTimingZone.SweetSpot;
    }

    private string GetEnemyJumpTimingZoneName(EnemyJumpTimingZone zone)
    {
        switch (zone)
        {
            case EnemyJumpTimingZone.TooEarly:
                return "too_early";
            case EnemyJumpTimingZone.SweetSpot:
                return "sweet_spot";
            case EnemyJumpTimingZone.TooLate:
                return "too_late";
            default:
                return "unknown";
        }
    }

    private void LogEnemyJumpTiming(EnemyRayProbe threat, EnemyJumpTimingZone zone, bool forced)
    {
        if (!debugForcedJumpTiming || lastForcedJumpTimingDebugFrame == Time.frameCount)
        {
            return;
        }

        lastForcedJumpTimingDebugFrame = Time.frameCount;
        Debug.Log(
            "[ENEMY JUMP TIMING] " +
            $"dist={threat.distance:F2} " +
            $"zone={GetEnemyJumpTimingZoneName(zone)} " +
            $"forced={forced} " +
            $"enemy={threat.enemyName}",
            this
        );
    }

    private void RecordJumpExecutionStats(
        bool jumpRequested,
        bool jumpExecutionRequested,
        bool jumpApplied,
        bool grounded)
    {
        if (!debugTrainingActionStats)
        {
            return;
        }

        if (jumpExecutionRequested)
        {
            trainingActionJumpAttemptCount++;
        }

        if (jumpApplied)
        {
            trainingActionJumpAppliedCount++;
        }
        else if (jumpRequested && !CanJumpFromGroundState(grounded))
        {
            trainingActionJumpBlockedNotGroundedCount++;
        }
    }

    private void ApplyJumpCommitRewardIfNeeded(int moveAction, int jumpAction, bool jumpApplied)
    {
        if (!jumpCommitMaskActiveForDecision)
        {
            return;
        }

        bool rightJumpApplied =
            moveAction == MoveRightAction &&
            jumpAction == JumpAction &&
            jumpApplied;

        if (!rightJumpApplied)
        {
            jumpCommitMaskActiveForDecision = false;
            jumpCommitThreatForDecision = default;
            return;
        }

        if (debugTrainingActionStats)
        {
            trainingActionJumpCommitAppliedCount++;
        }

        LogJumpCommitApplied(jumpCommitThreatForDecision);

        Transform enemyTransform = jumpCommitThreatForDecision.enemyTransform;

        if (enableJumpCommitReward &&
            jumpCommitReward > 0f &&
            enemyTransform != null &&
            !jumpCommitRewardedEnemyTransforms.Contains(enemyTransform))
        {
            jumpCommitRewardedEnemyTransforms.Add(enemyTransform);
            AddReward(jumpCommitReward);
        }

        jumpCommitMaskActiveForDecision = false;
        jumpCommitThreatForDecision = default;
    }

    private void LogEnemyActionMask(string enemyName)
    {
        if (!debugEnemyActionMask || lastEnemyActionMaskDebugFrame == Time.frameCount)
        {
            return;
        }

        lastEnemyActionMaskDebugFrame = Time.frameCount;
        Debug.Log($"[ENEMY ACTION MASK] masked forward action near enemy={enemyName}", this);
    }

    private void LogForcedJumpMask(EnemyRayProbe threat)
    {
        if (!debugForcedJumpMask || lastForcedJumpMaskDebugFrame == Time.frameCount)
        {
            return;
        }

        lastForcedJumpMaskDebugFrame = Time.frameCount;
        Debug.Log(
            "[ENEMY FORCED JUMP MASK] " +
            $"forced jump near enemy ray={threat.rayName} " +
            $"dist={threat.distance:F2} enemy={threat.enemyName}",
            this
        );
    }

    private void LogJumpCommitApplied(EnemyRayProbe threat)
    {
        if (!debugJumpCommitMask || lastJumpCommitAppliedDebugFrame == Time.frameCount)
        {
            return;
        }

        lastJumpCommitAppliedDebugFrame = Time.frameCount;
        Debug.Log(
            "[ENEMY JUMP COMMIT] " +
            $"right+jump applied dist={threat.distance:F2} enemy={threat.enemyName}",
            this
        );
    }

    private void LogJumpCommitMask(EnemyRayProbe threat)
    {
        if (!debugJumpCommitMask || lastJumpCommitMaskDebugFrame == Time.frameCount)
        {
            return;
        }

        lastJumpCommitMaskDebugFrame = Time.frameCount;
        Debug.Log(
            "[ENEMY JUMP COMMIT MASK] " +
            $"forced right+jump dist={threat.distance:F2} enemy={threat.enemyName}",
            this
        );
    }

    private void LogJumpCommitMaskActive(EnemyRayProbe threat, bool blockLeft, bool blockStop)
    {
        if (!debugJumpCommitMask || lastJumpCommitMaskActiveDebugFrame == Time.frameCount)
        {
            return;
        }

        lastJumpCommitMaskActiveDebugFrame = Time.frameCount;
        Debug.Log(
            "[JUMP COMMIT MASK ACTIVE]\n" +
            $"dist={threat.distance:F2}\n" +
            $"masked branch0 left={blockLeft.ToString().ToLowerInvariant()}\n" +
            $"masked branch0 stop={blockStop.ToString().ToLowerInvariant()}\n" +
            $"branch0 right available={(!lastMaskRightBlocked).ToString().ToLowerInvariant()}\n" +
            "masked branch1 noJump=true\n" +
            "branch1 jump available=true",
            this
        );
    }

    private void LogPrematureJumpMask(EnemyRayProbe threat, string reason)
    {
        if (!debugPrematureJumpMask || lastPrematureJumpMaskDebugFrame == Time.frameCount)
        {
            return;
        }

        lastPrematureJumpMaskDebugFrame = Time.frameCount;
        string distance = threat.hasHit ? threat.distance.ToString("F2") : "none";
        Debug.Log(
            "[ENEMY PREMATURE JUMP MASK] " +
            $"masked jump reason={reason} dist={distance}",
            this
        );
    }

    private void TrackJumpCommitActionOutcome(
        bool commitMaskActive,
        int moveAction,
        int jumpAction,
        bool jumpApplied)
    {
        if (!debugTrainingActionStats || !commitMaskActive)
        {
            return;
        }

        if (moveAction != MoveRightAction)
        {
            trainingActionCommitMaskButMoveNotRightCount++;
        }

        if (jumpAction != JumpAction)
        {
            trainingActionCommitMaskButJumpNotSelectedCount++;
        }

        bool rightJumpSelected = moveAction == MoveRightAction && jumpAction == JumpAction;

        if (rightJumpSelected)
        {
            trainingActionCommitMaskRightJumpSelectedCount++;
        }

        if (rightJumpSelected && jumpApplied)
        {
            trainingActionCommitMaskRightJumpAppliedCount++;
        }
    }

    private void LogRightActionNegativeVelocityWarning(int moveAction)
    {
        if (!debugActionTrace || moveAction != MoveRightAction || rb == null)
        {
            return;
        }

        if (rb.linearVelocity.x >= -0.01f || lastRightActionNegativeVelocityWarningFrame == Time.frameCount)
        {
            return;
        }

        lastRightActionNegativeVelocityWarningFrame = Time.frameCount;
        Debug.LogWarning(
            "[EA PHYSICS WARNING] right action but negative velocity after action " +
            $"velocity={FormatVector2(rb.linearVelocity)}",
            this
        );
    }

    private void LogActionTrace(
        int moveAction,
        int jumpAction,
        int sprintAction,
        bool commitMaskActive,
        EnemyRayProbe commitMaskThreat,
        float direction,
        bool grounded,
        bool jumpAppliedThisStep)
    {
        if (!debugActionTrace || Time.time < nextDebugActionTraceTime)
        {
            return;
        }

        nextDebugActionTraceTime = Time.time + Mathf.Max(0.01f, debugActionTraceInterval);

        EnemyRayProbe frontThreat = commitMaskThreat;
        bool hasFrontThreat = frontThreat.hasHit;

        if (!hasFrontThreat && useEnemyRayObservations)
        {
            hasFrontThreat = TryFindEnemyRayThreat(
                direction,
                enemyDetectionRangeX,
                EnemyRayThreatMode.FrontOnly,
                out frontThreat
            );
        }

        string frontDist = hasFrontThreat ? frontThreat.distance.ToString("F2") : "none";
        string enemyPos = hasFrontThreat && frontThreat.enemyTransform != null
            ? FormatVector3(frontThreat.enemyTransform.position)
            : "none";
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        bool coyoteAvailable = useCoyoteTime && coyoteTimer > 0f;

        Debug.Log(
            "[EA TRACE] " +
            $"step={StepCount} " +
            $"moveAction={moveAction} " +
            $"jumpAction={jumpAction} " +
            $"sprintAction={sprintAction} " +
            $"commitMaskActive={commitMaskActive} " +
            $"frontThreat={hasFrontThreat} " +
            $"frontDist={frontDist} " +
            $"grounded={grounded} " +
            $"coyoteAvailable={coyoteAvailable} " +
            $"episodeStartSettling={episodeStartSettling} " +
            $"settleTime={episodeStartSettleElapsed:F2} " +
            $"settleTimedOut={episodeStartSettleTimedOut} " +
            $"airCommitActive={IsAirCommitActive()} " +
            $"airCommitRemaining={GetAirCommitRemainingTime():F2} " +
            $"airCommitEnemy={airCommitEnemyName} " +
            $"rbVel={FormatVector2(velocity)} " +
            $"agentPos={FormatVector3(transform.position)} " +
            $"enemyPos={enemyPos} " +
            $"jumpAppliedThisStep={jumpAppliedThisStep} " +
            $"jumpsApplied={actionTraceJumpsAppliedCount} " +
            $"lastEndReason={lastEndReason}",
            this
        );
    }

    private string FormatVector2(Vector2 value)
    {
        return $"({value.x:F2},{value.y:F2})";
    }

    private string FormatVector3(Vector3 value)
    {
        return $"({value.x:F2},{value.y:F2})";
    }

    private void TrackTrainingActionStats(
        int moveAction,
        int jumpAction,
        float direction,
        bool grounded)
    {
        if (!debugTrainingActionStats)
        {
            return;
        }

        trainingActionDecisionCount++;

        bool jumpSelected = jumpAction == JumpAction;

        if (jumpSelected)
        {
            trainingActionJumpCount++;
        }

        if (moveAction == MoveRightAction && jumpAction == JumpAction)
        {
            trainingActionRightJumpActionCount++;
        }

        float moveXForStats = moveAction switch
        {
            MoveLeftAction => -1f,
            MoveRightAction => 1f,
            _ => 0f
        };

        if (moveXForStats * direction < -minUsefulMoveInput)
        {
            trainingActionRetreatActionCount++;
        }

        if (moveAction == StopAction)
        {
            trainingActionStallActionCount++;
        }

        float enemyStatsWindowX = GetEnemyActionStatsWindowX();
        bool enemyAhead = useEnemyRayObservations
            ? TryFindEnemyRayThreat(direction, enemyStatsWindowX, EnemyRayThreatMode.FrontOnly, out _)
            : TryFindDangerousEnemyAhead(
                direction,
                enemyStatsWindowX,
                enemyActionMaskVerticalTolerance,
                out _,
                out _,
                out _,
                out _
            );

        if (enemyAhead && jumpSelected)
        {
            trainingActionJumpNearEnemyCount++;
        }

        if (jumpSelected && useEnemyRayObservations &&
            TryFindEnemyRayThreat(direction, enemyForcedJumpWindowX, EnemyRayThreatMode.FrontOnly, out EnemyRayProbe jumpTimingThreat))
        {
            RecordEnemyJumpTimingStats(jumpTimingThreat.distance);
        }

        TrackPrematureJumpStats(direction, jumpSelected);

        int interval = Mathf.Max(1, debugTrainingActionStatsInterval);

        if (trainingActionDecisionCount % interval == 0)
        {
            Debug.Log(
                "[EA ACTIONS] " +
                $"decisions={trainingActionDecisionCount} " +
                $"jumps={trainingActionJumpCount} " +
                $"jumpsApplied={trainingActionJumpAppliedCount} " +
                $"jumpCommitMask={trainingActionJumpCommitMaskCount} " +
                $"jumpCommitApplied={trainingActionJumpCommitAppliedCount} " +
                $"rightJumpActions={trainingActionRightJumpActionCount} " +
                $"airCommitStarts={trainingActionAirCommitStartsCount} " +
                $"airCommitActiveSteps={trainingActionAirCommitActiveStepsCount} " +
                $"airCommitEndsPassed={trainingActionAirCommitEndsPassedCount} " +
                $"airCommitEndsDuration={trainingActionAirCommitEndsDurationCount} " +
                $"airCommitEndsHit={trainingActionAirCommitEndsHitCount} " +
                $"prematureJumpMasks={trainingActionPrematureJumpMaskCount} " +
                $"prematureJumpAttempts={trainingActionPrematureJumpAttemptCount} " +
                $"allowedSweetSpotJumps={trainingActionAllowedSweetSpotJumpCount} " +
                $"retreatActions={trainingActionRetreatActionCount} " +
                $"stallActions={trainingActionStallActionCount} " +
                $"microTimeouts={trainingActionMicroTimeoutCount} " +
                $"commitMaskActive={trainingActionCommitMaskActiveCount} " +
                $"commitMaskButMoveNotRight={trainingActionCommitMaskButMoveNotRightCount} " +
                $"commitMaskButJumpNotSelected={trainingActionCommitMaskButJumpNotSelectedCount} " +
                $"commitMaskRightJumpSelected={trainingActionCommitMaskRightJumpSelectedCount} " +
                $"commitMaskRightJumpApplied={trainingActionCommitMaskRightJumpAppliedCount} " +
                $"jumpsNearEnemy={trainingActionJumpNearEnemyCount} " +
                $"forcedMask={trainingActionForcedJumpMaskCount} " +
                $"maskedNoJump={trainingActionMaskedNoJumpCount} " +
                $"jumpAttempts={trainingActionJumpAttemptCount} " +
                $"jumpBlockedNotGrounded={trainingActionJumpBlockedNotGroundedCount} " +
                $"earlyJumps={trainingActionEarlyJumpCount} " +
                $"sweetSpotJumps={trainingActionSweetSpotJumpCount} " +
                $"lateJumps={trainingActionLateJumpCount}",
                this
            );
        }
    }

    private void TrackPrematureJumpStats(float direction, bool jumpSelected)
    {
        if (!jumpSelected || !maskPrematureEnemyJumps || !useEnemyRayObservations)
        {
            return;
        }

        if (!TryFindEnemyRayThreat(direction, enemyDetectionRangeX, EnemyRayThreatMode.FrontOnly, out EnemyRayProbe threat))
        {
            trainingActionPrematureJumpAttemptCount++;
            return;
        }

        if (threat.distance > prematureJumpMaxThreatDistance)
        {
            trainingActionPrematureJumpAttemptCount++;
        }
        else if (threat.distance >= prematureJumpMinThreatDistance)
        {
            trainingActionAllowedSweetSpotJumpCount++;
        }
    }

    private void RecordEnemyJumpTimingStats(float distance)
    {
        switch (GetEnemyJumpTimingZone(distance))
        {
            case EnemyJumpTimingZone.TooEarly:
                trainingActionEarlyJumpCount++;
                break;
            case EnemyJumpTimingZone.SweetSpot:
                trainingActionSweetSpotJumpCount++;
                break;
            case EnemyJumpTimingZone.TooLate:
                trainingActionLateJumpCount++;
                break;
        }
    }

    private float GetEnemyActionStatsWindowX()
    {
        return forceJumpActionNearEnemy
            ? Mathf.Max(enemyActionMaskWindowX, enemyForcedJumpWindowX)
            : enemyActionMaskWindowX;
    }

    private void ApplyRetreatPenalty(float direction)
    {
        if (!enableRetreatPenalty || retreatPenalty >= 0f)
        {
            return;
        }

        float backwardDistanceFromSpawn = (startPosition.x - transform.position.x) * direction;

        if (backwardDistanceFromSpawn <= retreatEndDistance)
        {
            return;
        }

        AddReward(retreatPenalty);
        LogEnemyRewardEvent(
            $"[MICRO RETREAT] penalty={retreatPenalty:F4} backDistance={backwardDistanceFromSpawn:F2}",
            true
        );
    }

    private bool CheckMicroTimeout()
    {
        if (!enableShortMicroTimeout || episodeEnding || episodeTime < microTimeoutSeconds)
        {
            return false;
        }

        if (rewardedEnemyTransforms.Count > 0)
        {
            return false;
        }

        AddReward(microTimeoutPenalty);

        if (debugTrainingActionStats)
        {
            trainingActionMicroTimeoutCount++;
        }

        LogEnemyRewardEvent($"[MICRO TIMEOUT] penalty={microTimeoutPenalty:F3}", false);
        TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Timeout);
        return true;
    }

    private bool ShouldLimitBacktracking(float direction)
    {
        if (!maskMoveAwayFromGoal)
        {
            return false;
        }

        if (!HasMeaningfulHorizontalGoalDirection())
        {
            return false;
        }

        if (!allowBacktrackingForPositioning)
        {
            return true;
        }

        float signedProgress = transform.position.x * direction;
        return signedProgress < bestXReached - maxAllowedBacktrackDistance;
    }

    public void GoalReached()
    {
        CompleteGoalEpisode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsGoalCollider(other))
        {
            CompleteGoalEpisode();
        }
    }

    public void FellOffMap()
    {
        if (Time.realtimeSinceStartup - episodeStartRealtime < 0.05f)
        {
            Debug.LogWarning("Ignored FellOffMap immediately after episode reset.");
            return;
        }

        AddReward(deathPenalty);
        TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Fell);
    }

    public void EnemyHit(EdgeRunnerEnemyMarker enemy)
    {
        HandleEnemyHit(enemy);
    }

    public void EnemyHit(DemoEnemyHazard enemy)
    {
        HandleEnemyHit(enemy);
    }

    public void EnemyHit(Component enemy)
    {
        HandleEnemyHit(enemy);
    }

    public bool IsCurrentlyGroundedForEvaluation()
    {
        return IsGrounded();
    }

    public Vector2 GetCurrentVelocityForEvaluation()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private void NotifyEvaluationEpisodeStarted()
    {
        if (evaluationManager != null)
        {
            evaluationManager.NotifyEpisodeStarted(this);
        }
    }

    private void NotifyEvaluationEpisodeEnded(EdgeRunnerEpisodeEndReason reason)
    {
        if (evaluationManager != null)
        {
            evaluationManager.NotifyEpisodeEnded(this, reason);
        }
    }

    private bool CheckGoalReachedByDistance()
    {
        if (episodeEnding)
        {
            return false;
        }

        if (goal == null)
        {
            WarnGoalMissingOnce();
            return false;
        }

        float reachDistance = Mathf.Max(0.01f, goalReachDistance);
        float distanceToGoal = Vector2.Distance(transform.position, goal.position);

        if (distanceToGoal > reachDistance)
        {
            return false;
        }

        return CompleteGoalEpisode();
    }

    private bool CompleteGoalEpisode()
    {
        if (episodeEnding)
        {
            return false;
        }

        AddReward(goalReward);
        return TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Success);
    }

    private bool IsGoalCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        Transform otherTransform = other.transform;

        if (goal != null &&
            (otherTransform == goal ||
             otherTransform.IsChildOf(goal) ||
             goal.IsChildOf(otherTransform)))
        {
            return true;
        }

        return other.gameObject.tag == "Goal";
    }

    private void WarnGoalMissingOnce()
    {
        if (warnedGoalMissingThisEpisode)
        {
            return;
        }

        warnedGoalMissingThisEpisode = true;
        Debug.LogWarning("EdgeRunnerAgentV5EnemyAware: goal is null during episode; success detection is waiting for a generated or assigned goal.");
    }

    private bool TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason reason)
    {
        if (episodeEnding)
        {
            return false;
        }

        if (Time.frameCount == lastEpisodeEndFrame)
        {
            return false;
        }

        EndAirCommit(reason == EdgeRunnerEpisodeEndReason.EnemyHit ? "hit" : "reset");
        episodeEnding = true;
        lastEpisodeEndFrame = Time.frameCount;
        lastEndReason = reason.ToString();

        NotifyEvaluationEpisodeEnded(reason);
        EndEpisode();
        return true;
    }

    private void HandleJumpReward(float moveX, float direction, TerrainAnalysis terrain)
    {
        float forwardSpeed = rb.linearVelocity.x * direction / GetMaxHorizontalSpeed();

        AddReward(jumpPenalty);

        if (Mathf.Abs(moveX) < 0.1f)
        {
            AddReward(idleJumpPenalty);
        }

        bool gapCloseEnough = IsGapCloseEnoughForJump(terrain);

        if (gapCloseEnough)
        {
            jumpedForGap = true;

            if (forwardSpeed >= minJumpMomentum)
            {
                AddReward(gapJumpReward * Mathf.Clamp01(forwardSpeed));
            }
            else
            {
                AddReward(lowMomentumJumpPenalty);
            }

            return;
        }

        if (terrain.hasGapAhead && !gapCloseEnough)
        {
            AddReward(earlyGapJumpPenalty);
            return;
        }

        if (terrain.flatGroundAhead && !terrain.wallAhead)
        {
            AddReward(flatGroundJumpPenalty);
            AddReward(uselessJumpPenalty);
        }
    }

    private float GetGapStartDistance(TerrainAnalysis terrain)
    {
        return terrain.distanceToGapStartNormalized * gapSensorRange;
    }

    private float GetEstimatedGapWidth(TerrainAnalysis terrain)
    {
        return terrain.estimatedGapWidthNormalized * maxExpectedGapWidth;
    }

    private float GetMaxUsefulJumpDistance(TerrainAnalysis terrain)
    {
        if (!useAdaptiveJumpWindow)
        {
            return maxGapDistanceForUsefulJump;
        }

        float estimatedGapWidth = GetEstimatedGapWidth(terrain);
        float t = Mathf.InverseLerp(smallGapWidthReference, largeGapWidthReference, estimatedGapWidth);

        return Mathf.Lerp(smallGapMaxJumpDistance, largeGapMaxJumpDistance, t);
    }

    private bool IsGapCloseEnoughForJump(TerrainAnalysis terrain)
    {
        if (!terrain.hasGapAhead)
        {
            return false;
        }

        if (requireLandingForUsefulGapJump && !terrain.hasLanding)
        {
            return false;
        }

        float gapDistance = GetGapStartDistance(terrain);
        float maxUsefulDistance = GetMaxUsefulJumpDistance(terrain);

        return gapDistance >= minGapDistanceForUsefulJump &&
               gapDistance <= maxUsefulDistance;
    }

    private bool IsUsefulJumpSituation(TerrainAnalysis terrain)
    {
        if (terrain.wallAhead)
        {
            return true;
        }

        if (IsGapCloseEnoughForJump(terrain))
        {
            return true;
        }

        return false;
    }

    private void ApplyLocomotionReward(float direction, float moveX, bool grounded, bool suppressPositiveProgressReward)
    {
        bool hasHorizontalGoalDirection = HasMeaningfulHorizontalGoalDirection();
        float forwardVelocity = rb.linearVelocity.x * direction;

        if (hasHorizontalGoalDirection && moveX * direction > minUsefulMoveInput)
        {
            if (!suppressPositiveProgressReward)
            {
                AddReward(forwardActionReward);
            }
        }
        else if (hasHorizontalGoalDirection &&
                 moveX * direction < -minUsefulMoveInput &&
                 lastDistanceImprovement <= 0f)
        {
            AddReward(wrongDirectionActionPenalty);
        }
        else if (grounded)
        {
            AddReward(idlePenalty);
        }

        if (!suppressPositiveProgressReward &&
            hasHorizontalGoalDirection &&
            forwardVelocity > minUsefulForwardVelocity)
        {
            AddReward(Mathf.Clamp01(forwardVelocity / GetMaxHorizontalSpeed()) * forwardVelocityReward);
        }
    }

    private void ApplyDistanceProgressReward(bool suppressPositiveProgressReward)
    {
        if (goal == null)
        {
            lastDistanceImprovement = 0f;
            return;
        }

        float currentDistance = GetDistanceToGoal();
        float delta = previousDistanceToGoal - currentDistance;
        lastDistanceImprovement = delta;
        float progressReward = 0f;

        if (delta > 0f)
        {
            if (!suppressPositiveProgressReward)
            {
                float rewardScale = Mathf.Max(distanceProgressRewardScale, progressRewardScale);
                float rewardCap = Mathf.Max(maxDistanceProgressReward, maxProgressRewardPerStep);
                progressReward = Mathf.Clamp(delta * rewardScale, 0f, rewardCap);
                AddReward(progressReward);
            }
        }
        else if (delta < 0f)
        {
            progressReward = Mathf.Clamp(delta * distanceRegressionPenaltyScale, maxDistanceRegressionPenalty, 0f);
            AddReward(progressReward);
        }

        if (currentDistance < bestDistanceToGoal - minDistanceProgressForReset)
        {
            bestDistanceToGoal = currentDistance;
            timeSinceDistanceProgress = 0f;

            if (!suppressPositiveProgressReward)
            {
                AddReward(milestoneReward);
            }
        }
        else
        {
            timeSinceDistanceProgress += Time.fixedDeltaTime;
        }

        previousDistanceToGoal = currentDistance;
        LogEnemyAwareProgress(currentDistance, delta, progressReward);
    }

    private void LogEnemyAwareProgress(float distanceToGoal, float progressDelta, float progressReward)
    {
        if (!debugEnemyAwareProgress || Time.time < nextDebugProgressLogTime)
        {
            return;
        }

        nextDebugProgressLogTime = Time.time + Mathf.Max(0.05f, debugActionLogInterval);
        Debug.Log(
            "[ENEMY AWARE PROGRESS] " +
            $"distanceToGoal={distanceToGoal:F3}, " +
            $"delta={progressDelta:F3}, " +
            $"progressReward={progressReward:F4}, " +
            $"position=({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2}), " +
            $"bestDistanceToGoal={bestDistanceToGoal:F3}",
            this
        );
    }

    private void TrackGapLandingReward(float direction, bool grounded)
    {
        bool overEmptySpace = !ProbeGround(direction, 0f).hasGround;

        if (!grounded && jumpedForGap && overEmptySpace)
        {
            crossedGapInAir = true;
        }

        if (!wasGroundedLastStep && grounded)
        {
            if (jumpedForGap && crossedGapInAir)
            {
                AddReward(gapLandingReward);
            }

            jumpedForGap = false;
            crossedGapInAir = false;
        }

        wasGroundedLastStep = grounded;
    }

    private void ApplyProgressRewards(float direction, float moveX)
    {
        float currentGoalDistanceX = GetGoalDistanceX();
        previousGoalDistanceX = currentGoalDistanceX;

        if (HasMeaningfulHorizontalGoalDirection() &&
            moveX * direction < -0.1f &&
            lastDistanceImprovement <= 0f)
        {
            AddReward(backtrackPenalty);
        }

        if (HasMeaningfulHorizontalGoalDirection())
        {
            float forwardX = transform.position.x * direction;

            if (forwardX > bestXReached + bestXProgressThreshold)
            {
                bestXReached = forwardX;
            }
            else
            {
                bestXReached = Mathf.Max(bestXReached, forwardX);
            }
        }

        if (lastDistanceImprovement > 0f)
        {
            timeSinceBestXProgress = 0f;
        }
        else
        {
            timeSinceBestXProgress += Time.fixedDeltaTime;
        }
    }

    private void AddEnemyObservations(VectorSensor sensor, List<EnemyCandidate> candidates)
    {
        int maxSlots = EnemyObservationCount / 4;
        int activeSlots = Mathf.Clamp(enemyObservationSlots, 1, maxSlots);
        float maxDistance = Mathf.Sqrt(
            enemyDetectionRangeX * enemyDetectionRangeX +
            enemyDetectionRangeY * enemyDetectionRangeY
        );

        for (int slot = 0; slot < maxSlots; slot++)
        {
            if (slot >= activeSlots || slot >= candidates.Count)
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                continue;
            }

            EnemyCandidate candidate = candidates[slot];
            sensor.AddObservation(NormalizeSignedDistance(candidate.delta.x, enemyDetectionRangeX));
            sensor.AddObservation(NormalizeSignedDistance(candidate.delta.y, enemyDetectionRangeY));
            sensor.AddObservation(NormalizeDistance(candidate.delta.magnitude, maxDistance));
            sensor.AddObservation(candidate.isDangerous ? 1f : 0f);
        }
    }

    private void AddEnemyRayObservations(VectorSensor sensor, float direction)
    {
        float range = Mathf.Max(0.1f, enemyDetectionRangeX);
        int frontHits = 0;
        int backHits = 0;
        int downHits = 0;
        float minFrontDistance = range;
        float minBackDistance = range;
        float minDownDistance = range;

        for (int i = 0; i < EnemyRayCount; i++)
        {
            EnemyRayProbe probe = ProbeEnemyRay(direction, i, range);
            sensor.AddObservation(probe.hasHit ? 1f : 0f);
            sensor.AddObservation(probe.hasHit ? probe.distanceNormalized : 1f);

            if (!probe.hasHit)
            {
                continue;
            }

            if (IsFrontEnemyRay(i))
            {
                frontHits++;
                minFrontDistance = Mathf.Min(minFrontDistance, probe.distance);
            }
            else if (i == EnemyRayBackMid)
            {
                backHits++;
                minBackDistance = Mathf.Min(minBackDistance, probe.distance);
            }
            else if (i == EnemyRayDownForward)
            {
                downHits++;
                minDownDistance = Mathf.Min(minDownDistance, probe.distance);
            }
        }

        LogEnemyRayObservations(
            frontHits,
            backHits,
            downHits,
            frontHits > 0 ? minFrontDistance : -1f,
            backHits > 0 ? minBackDistance : -1f,
            downHits > 0 ? minDownDistance : -1f
        );
    }

    private void LogEnemyRayObservations(
        int frontHits,
        int backHits,
        int downHits,
        float minFrontDistance,
        float minBackDistance,
        float minDownDistance)
    {
        if (!debugEnemyRayObservations || Time.time < nextDebugEnemyRayLogTime)
        {
            return;
        }

        nextDebugEnemyRayLogTime = Time.time + 0.5f;
        Debug.Log(
            "[ENEMY RAYS] " +
            $"frontHits={frontHits} backHits={backHits} downHits={downHits} " +
            $"minFrontDist={minFrontDistance:F3} minBackDist={minBackDistance:F3} minDownDist={minDownDistance:F3}",
            this
        );
    }

    private EnemyRayProbe ProbeEnemyRay(float direction, int rayIndex, float range)
    {
        range = Mathf.Max(0.1f, range);
        Vector2 rayDirection = GetEnemyRayDirection(direction, rayIndex);
        float verticalOffset = GetEnemyRayVerticalOffset(rayIndex);
        Vector2 origin = (Vector2)transform.position + new Vector2(0f, verticalOffset);
        EnemyRayProbe bestProbe = new EnemyRayProbe
        {
            hasHit = false,
            rayIndex = rayIndex,
            origin = origin,
            point = origin + rayDirection * range,
            distance = range,
            distanceNormalized = 1f,
            enemyTransform = null,
            enemyName = string.Empty,
            rayName = GetEnemyRayName(rayIndex)
        };

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, rayDirection, range);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            EdgeRunnerEnemyMarker marker = hit.collider.GetComponentInParent<EdgeRunnerEnemyMarker>();

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            if (hit.distance < bestProbe.distance)
            {
                bestProbe = CreateEnemyRayHitProbe(origin, hit.point, hit.distance, range, marker, rayIndex);
            }
        }

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            if (!TryIntersectEnemyBounds(marker, origin, rayDirection, range, out float distance, out Vector2 point))
            {
                continue;
            }

            if (distance < bestProbe.distance)
            {
                bestProbe = CreateEnemyRayHitProbe(origin, point, distance, range, marker, rayIndex);
            }
        }

        return bestProbe;
    }

    private Vector2 GetEnemyRayDirection(float direction, int rayIndex)
    {
        float directionSign = direction >= 0f ? 1f : -1f;

        if (rayIndex == EnemyRayBackMid)
        {
            return new Vector2(-directionSign, 0f);
        }

        if (rayIndex == EnemyRayDownForward)
        {
            return new Vector2(directionSign * 0.65f, -1f).normalized;
        }

        return new Vector2(directionSign, 0f);
    }

    private float GetEnemyRayVerticalOffset(int rayIndex)
    {
        if (rayIndex < 0 || rayIndex >= EnemyRayVerticalOffsets.Length)
        {
            return 0.5f;
        }

        return EnemyRayVerticalOffsets[rayIndex];
    }

    private string GetEnemyRayName(int rayIndex)
    {
        return rayIndex switch
        {
            EnemyRayFrontLow => "front_low",
            EnemyRayFrontMid => "front_mid",
            EnemyRayBackMid => "back_mid",
            EnemyRayDownForward => "down_forward",
            _ => "unknown"
        };
    }

    private bool IsFrontEnemyRay(int rayIndex)
    {
        return rayIndex == EnemyRayFrontLow || rayIndex == EnemyRayFrontMid;
    }

    private EnemyRayProbe CreateEnemyRayHitProbe(
        Vector2 origin,
        Vector2 point,
        float distance,
        float range,
        EdgeRunnerEnemyMarker marker,
        int rayIndex)
    {
        Transform enemyTransform = marker.ObservationTransform != null
            ? marker.ObservationTransform
            : marker.transform;

        return new EnemyRayProbe
        {
            hasHit = true,
            rayIndex = rayIndex,
            origin = origin,
            point = point,
            distance = distance,
            distanceNormalized = NormalizeDistance(distance, range),
            enemyTransform = enemyTransform,
            enemyName = marker.name,
            rayName = GetEnemyRayName(rayIndex)
        };
    }

    private bool TryIntersectEnemyBounds(
        EdgeRunnerEnemyMarker marker,
        Vector2 origin,
        Vector2 rayDirection,
        float range,
        out float distance,
        out Vector2 point)
    {
        distance = 0f;
        point = origin;

        Bounds bounds = marker.GetObservationBounds();

        if (bounds.size == Vector3.zero)
        {
            Vector2 markerPosition = marker.GetObservationPosition();
            bounds = new Bounds(markerPosition, new Vector3(0.2f, 0.2f, 0.2f));
        }

        Vector2 normalizedDirection = rayDirection.normalized;
        float tMin = 0f;
        float tMax = range;

        if (!UpdateRayBoundsInterval(origin.x, normalizedDirection.x, bounds.min.x, bounds.max.x, ref tMin, ref tMax) ||
            !UpdateRayBoundsInterval(origin.y, normalizedDirection.y, bounds.min.y, bounds.max.y, ref tMin, ref tMax))
        {
            return false;
        }

        distance = Mathf.Max(0f, tMin);

        if (distance < 0f || distance > range)
        {
            return false;
        }

        point = origin + normalizedDirection * distance;
        return true;
    }

    private bool UpdateRayBoundsInterval(
        float origin,
        float direction,
        float min,
        float max,
        ref float tMin,
        ref float tMax)
    {
        if (Mathf.Abs(direction) < 0.0001f)
        {
            return origin >= min && origin <= max;
        }

        float t1 = (min - origin) / direction;
        float t2 = (max - origin) / direction;

        if (t1 > t2)
        {
            float temp = t1;
            t1 = t2;
            t2 = temp;
        }

        tMin = Mathf.Max(tMin, t1);
        tMax = Mathf.Min(tMax, t2);
        return tMin <= tMax;
    }

    private bool IsOwnCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        EdgeRunnerAgentV5EnemyAware owner = collider.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();
        return owner == this;
    }

    private bool TryFindEnemyRayThreat(
        float direction,
        float maxDistance,
        EnemyRayThreatMode threatMode,
        out EnemyRayProbe bestThreat)
    {
        bestThreat = default;
        float range = Mathf.Max(0.1f, enemyDetectionRangeX);
        float threatDistance = Mathf.Clamp(maxDistance, 0f, range);
        bool foundThreat = false;

        for (int i = 0; i < EnemyRayCount; i++)
        {
            if (!ShouldUseEnemyRayForThreat(i, threatMode))
            {
                continue;
            }

            EnemyRayProbe probe = ProbeEnemyRay(direction, i, range);

            if (!probe.hasHit || probe.distance > threatDistance)
            {
                continue;
            }

            if (!foundThreat || probe.distance < bestThreat.distance)
            {
                bestThreat = probe;
                foundThreat = true;
            }
        }

        return foundThreat;
    }

    private bool ShouldUseEnemyRayForThreat(int rayIndex, EnemyRayThreatMode threatMode)
    {
        switch (threatMode)
        {
            case EnemyRayThreatMode.FrontOnly:
                return IsFrontEnemyRay(rayIndex);
            case EnemyRayThreatMode.FrontOrDownForward:
                return IsFrontEnemyRay(rayIndex) || rayIndex == EnemyRayDownForward;
            case EnemyRayThreatMode.Any:
                return true;
            default:
                return false;
        }
    }

    private List<EnemyCandidate> AnalyzeEnemies(float direction)
    {
        enemyObservationCandidates.Clear();

        if (!enableEnemyAwareness)
        {
            return enemyObservationCandidates;
        }

        HashSet<Transform> seenEnemies = new HashSet<Transform>();

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable)
            {
                continue;
            }

            ConsiderEnemyCandidate(
                marker.ObservationTransform,
                marker.CurrentVelocity,
                marker.IsDangerous,
                direction,
                seenEnemies,
                enemyObservationCandidates
            );
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard == null || !hazard.AffectsAgent)
            {
                continue;
            }

            ConsiderEnemyCandidate(
                hazard.transform,
                GetEnemyVelocity(hazard.transform),
                true,
                direction,
                seenEnemies,
                enemyObservationCandidates
            );
        }

        AddTaggedEnemyCandidates(
            direction,
            seenEnemies,
            enemyObservationCandidates
        );

        enemyObservationCandidates.Sort(CompareEnemyCandidates);
        TrimEnemyCandidatesToSlots();
        LogEnemyObservations(enemyObservationCandidates);
        return enemyObservationCandidates;
    }

    private void AddTaggedEnemyCandidates(
        float direction,
        HashSet<Transform> seenEnemies,
        List<EnemyCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(enemyTag))
        {
            return;
        }

        GameObject[] taggedEnemies;

        try
        {
            taggedEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
        }
        catch (UnityException)
        {
            return;
        }

        for (int i = 0; i < taggedEnemies.Length; i++)
        {
            GameObject taggedEnemy = taggedEnemies[i];

            if (taggedEnemy == null || !taggedEnemy.activeInHierarchy)
            {
                continue;
            }

            ConsiderEnemyCandidate(
                taggedEnemy.transform,
                GetEnemyVelocity(taggedEnemy.transform),
                true,
                direction,
                seenEnemies,
                candidates
            );
        }
    }

    private void ConsiderEnemyCandidate(
        Transform enemyTransform,
        Vector2 enemyVelocity,
        bool isDangerous,
        float direction,
        HashSet<Transform> seenEnemies,
        List<EnemyCandidate> candidates)
    {
        if (enemyTransform == null)
        {
            return;
        }

        if (!seenEnemies.Add(enemyTransform))
        {
            return;
        }

        Vector2 delta = (Vector2)(enemyTransform.position - transform.position);

        if (Mathf.Abs(delta.x) > enemyDetectionRangeX || Mathf.Abs(delta.y) > enemyDetectionRangeY)
        {
            return;
        }

        float distanceSqr = delta.sqrMagnitude;
        float forwardDistance = delta.x * direction;
        float priority = forwardDistance >= 0f
            ? distanceSqr
            : distanceSqr + enemyDetectionRangeX * enemyDetectionRangeX;

        candidates.Add(new EnemyCandidate
        {
            exists = true,
            transform = enemyTransform,
            delta = delta,
            velocity = enemyVelocity,
            forwardDistance = Mathf.Max(0f, forwardDistance),
            priority = priority,
            isDangerous = isDangerous
        });
    }

    private int CompareEnemyCandidates(EnemyCandidate a, EnemyCandidate b)
    {
        int priorityComparison = a.priority.CompareTo(b.priority);

        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return a.delta.sqrMagnitude.CompareTo(b.delta.sqrMagnitude);
    }

    private void TrimEnemyCandidatesToSlots()
    {
        int maxSlots = EnemyObservationCount / 4;
        int activeSlots = Mathf.Clamp(enemyObservationSlots, 1, maxSlots);

        if (enemyObservationCandidates.Count > activeSlots)
        {
            enemyObservationCandidates.RemoveRange(activeSlots, enemyObservationCandidates.Count - activeSlots);
        }
    }

    private void LogEnemyObservations(List<EnemyCandidate> candidates)
    {
        if (!debugEnemyObservations)
        {
            return;
        }

        int maxSlots = EnemyObservationCount / 4;
        int activeSlots = Mathf.Clamp(enemyObservationSlots, 1, maxSlots);
        string message = $"[ENEMY OBS] detected={candidates.Count} slots={activeSlots}";

        for (int i = 0; i < activeSlots; i++)
        {
            if (i >= candidates.Count)
            {
                message += $"\nslot {i}: empty";
                continue;
            }

            EnemyCandidate candidate = candidates[i];
            string enemyName = candidate.transform != null ? candidate.transform.name : "unknown";
            message +=
                $"\nslot {i}: {enemyName} " +
                $"dx={NormalizeSignedDistance(candidate.delta.x, enemyDetectionRangeX):F3} " +
                $"dy={NormalizeSignedDistance(candidate.delta.y, enemyDetectionRangeY):F3} " +
                $"dist={NormalizeDistance(candidate.delta.magnitude, Mathf.Sqrt(enemyDetectionRangeX * enemyDetectionRangeX + enemyDetectionRangeY * enemyDetectionRangeY)):F3} " +
                $"danger={(candidate.isDangerous ? 1 : 0)}";
        }

        Debug.Log(message, this);
    }

    private void ApplyEnemyAvoidanceCueRewards(
        float direction,
        float moveX,
        bool jumpRequested,
        bool grounded)
    {
        if (!enableEnemyAwareness || episodeEnding)
        {
            return;
        }

        if (useEnemyRayObservations)
        {
            TryApplyEnemyRayAvoidanceCueRewards(direction, moveX, jumpRequested, grounded);
            return;
        }

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            TryApplyEnemyAvoidanceCueRewards(
                marker.ObservationTransform,
                marker.GetObservationPosition(),
                marker.name,
                direction,
                moveX,
                jumpRequested,
                grounded
            );
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard == null || !hazard.AffectsAgent)
            {
                continue;
            }

            TryApplyEnemyAvoidanceCueRewards(
                hazard.transform,
                hazard.transform.position,
                hazard.name,
                direction,
                moveX,
                jumpRequested,
                grounded
            );
        }
    }

    private void TryApplyEnemyRayAvoidanceCueRewards(
        float direction,
        float moveX,
        bool jumpRequested,
        bool grounded)
    {
        bool guidedJumpCue = forceJumpActionNearEnemy || enableJumpCommitMask;
        EnemyRayThreatMode jumpThreatMode = guidedJumpCue
            ? EnemyRayThreatMode.FrontOnly
            : EnemyRayThreatMode.FrontOrDownForward;
        bool hasJumpThreat = TryFindEnemyRayThreat(
            direction,
            enemyAvoidanceWindowX,
            jumpThreatMode,
            out EnemyRayProbe jumpThreat
        );
        bool hasFrontThreat = TryFindEnemyRayThreat(
            direction,
            enemyAvoidanceWindowX,
            EnemyRayThreatMode.FrontOnly,
            out EnemyRayProbe frontThreat
        );

        if (!hasJumpThreat)
        {
            return;
        }

        Transform enemyTransform = jumpThreat.enemyTransform;

        if (enemyTransform == null)
        {
            return;
        }

        bool movingTowardEnemy = moveX * direction > minUsefulMoveInput;
        bool hasUpwardAvoidanceVelocity = rb != null && rb.linearVelocity.y >= enemyJumpCueMinUpVelocity;
        bool jumpCueActive = jumpRequested || hasUpwardAvoidanceVelocity;
        bool jumpThreatInRewardWindow = !guidedJumpCue || IsGuidedEnemyJumpThreatInRewardWindow(jumpThreat.distance);

        if (jumpCueActive &&
            jumpThreatInRewardWindow &&
            enemyJumpCueReward > 0f &&
            !jumpCueRewardedEnemyTransforms.Contains(enemyTransform))
        {
            jumpCueRewardedEnemyTransforms.Add(enemyTransform);
            AddReward(enemyJumpCueReward);
            LogEnemyRewardEvent(
                $"[ENEMY JUMP CUE] reward={enemyJumpCueReward:F3} enemy={jumpThreat.enemyName} ray={jumpThreat.rayName} rayDist={jumpThreat.distance:F3}",
                false
            );
        }

        if (forceJumpActionNearEnemy &&
            jumpRequested &&
            earlyEnemyJumpPenalty < 0f &&
            hasFrontThreat &&
            GetEnemyJumpTimingZone(frontThreat.distance) == EnemyJumpTimingZone.TooEarly)
        {
            AddReward(earlyEnemyJumpPenalty);
            LogEnemyRewardEvent(
                $"[ENEMY EARLY JUMP] penalty={earlyEnemyJumpPenalty:F4} enemy={frontThreat.enemyName} ray={frontThreat.rayName} rayDist={frontThreat.distance:F3}",
                true
            );
        }

        bool groundedOrNearlyGrounded = IsGroundedOrNearlyGrounded(grounded);

        if (hasFrontThreat && movingTowardEnemy && groundedOrNearlyGrounded && !jumpCueActive && enemyApproachPenalty < 0f)
        {
            AddReward(enemyApproachPenalty);
            LogEnemyRewardEvent(
                $"[ENEMY APPROACH] penalty={enemyApproachPenalty:F4} enemy={frontThreat.enemyName} ray={frontThreat.rayName} rayDist={frontThreat.distance:F3}",
                true
            );
        }
    }

    private void TryApplyEnemyAvoidanceCueRewards(
        Transform enemyTransform,
        Vector2 enemyPosition,
        string enemyName,
        float direction,
        float moveX,
        bool jumpRequested,
        bool grounded)
    {
        if (enemyTransform == null || rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return;
        }

        if (!IsEnemyInAvoidanceWindow(enemyTransform, enemyPosition, direction, out float forwardDistanceToEnemy))
        {
            return;
        }

        bool movingTowardEnemy = moveX * direction > minUsefulMoveInput;
        bool hasUpwardAvoidanceVelocity = rb != null && rb.linearVelocity.y >= enemyJumpCueMinUpVelocity;
        bool jumpCueActive = jumpRequested || hasUpwardAvoidanceVelocity;

        if (jumpCueActive && enemyJumpCueReward > 0f && !jumpCueRewardedEnemyTransforms.Contains(enemyTransform))
        {
            jumpCueRewardedEnemyTransforms.Add(enemyTransform);
            AddReward(enemyJumpCueReward);
            LogEnemyRewardEvent(
                $"[ENEMY JUMP CUE] reward={enemyJumpCueReward:F3} enemy={enemyName} distance={forwardDistanceToEnemy:F3}",
                false
            );
        }

        bool groundedOrNearlyGrounded = grounded || (rb != null && Mathf.Abs(rb.linearVelocity.y) <= enemyJumpCueMinUpVelocity);

        if (movingTowardEnemy && groundedOrNearlyGrounded && !jumpCueActive && enemyApproachPenalty < 0f)
        {
            AddReward(enemyApproachPenalty);
            LogEnemyRewardEvent(
                $"[ENEMY APPROACH] penalty={enemyApproachPenalty:F4} enemy={enemyName} distance={forwardDistanceToEnemy:F3}",
                true
            );
        }
    }

    private bool ShouldSuppressProgressRewardNearEnemy(float direction)
    {
        if (!enableEnemyAwareness || !disableProgressRewardNearEnemy || episodeEnding)
        {
            return false;
        }

        if (useEnemyRayObservations &&
            TryFindEnemyRayThreat(direction, enemyAvoidanceWindowX, EnemyRayThreatMode.FrontOrDownForward, out EnemyRayProbe threat))
        {
            LogEnemyRewardEvent($"[ENEMY PROGRESS SUPPRESSED] enemy={threat.enemyName} ray={threat.rayName}", true);
            return true;
        }

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            if (IsEnemyInAvoidanceWindow(marker.ObservationTransform, marker.GetObservationPosition(), direction, out _))
            {
                LogEnemyRewardEvent($"[ENEMY PROGRESS SUPPRESSED] enemy={marker.name}", true);
                return true;
            }
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard == null || !hazard.AffectsAgent)
            {
                continue;
            }

            if (IsEnemyInAvoidanceWindow(hazard.transform, hazard.transform.position, direction, out _))
            {
                LogEnemyRewardEvent($"[ENEMY PROGRESS SUPPRESSED] enemy={hazard.name}", true);
                return true;
            }
        }

        return false;
    }

    private bool IsGuidedEnemyJumpThreatInRewardWindow(float distance)
    {
        if (enableJumpCommitMask)
        {
            return distance >= jumpCommitMinDistance && distance <= jumpCommitMaxDistance;
        }

        return GetEnemyJumpTimingZone(distance) == EnemyJumpTimingZone.SweetSpot;
    }

    private bool TryFindDangerousEnemyAhead(
        float direction,
        float windowX,
        float verticalTolerance,
        out Transform enemyTransform,
        out Vector2 enemyPosition,
        out string enemyName,
        out float forwardDistanceToEnemy)
    {
        enemyTransform = null;
        enemyPosition = Vector2.zero;
        enemyName = string.Empty;
        forwardDistanceToEnemy = 0f;

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            Transform observationTransform = marker.ObservationTransform != null
                ? marker.ObservationTransform
                : marker.transform;
            Vector2 markerPosition = marker.GetObservationPosition();

            if (IsDangerousEnemyAhead(
                observationTransform,
                markerPosition,
                direction,
                windowX,
                verticalTolerance,
                out forwardDistanceToEnemy))
            {
                enemyTransform = observationTransform;
                enemyPosition = markerPosition;
                enemyName = marker.name;
                return true;
            }
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard == null || !hazard.AffectsAgent)
            {
                continue;
            }

            if (IsDangerousEnemyAhead(
                hazard.transform,
                hazard.transform.position,
                direction,
                windowX,
                verticalTolerance,
                out forwardDistanceToEnemy))
            {
                enemyTransform = hazard.transform;
                enemyPosition = hazard.transform.position;
                enemyName = hazard.name;
                return true;
            }
        }

        return false;
    }

    private bool IsDangerousEnemyAhead(
        Transform enemyTransform,
        Vector2 enemyPosition,
        float direction,
        float windowX,
        float verticalTolerance,
        out float forwardDistanceToEnemy)
    {
        forwardDistanceToEnemy = 0f;

        if (enemyTransform == null || rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return false;
        }

        Vector2 deltaToEnemy = enemyPosition - (Vector2)transform.position;
        forwardDistanceToEnemy = deltaToEnemy.x * direction;

        if (forwardDistanceToEnemy <= 0f || forwardDistanceToEnemy > windowX)
        {
            return false;
        }

        return HasDangerousVerticalOverlap(enemyPosition, verticalTolerance);
    }

    private bool IsEnemyInAvoidanceWindow(
        Transform enemyTransform,
        Vector2 enemyPosition,
        float direction,
        out float forwardDistanceToEnemy)
    {
        forwardDistanceToEnemy = 0f;

        if (enemyTransform == null || rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return false;
        }

        Vector2 deltaToEnemy = enemyPosition - (Vector2)transform.position;
        forwardDistanceToEnemy = deltaToEnemy.x * direction;

        if (forwardDistanceToEnemy <= 0f || forwardDistanceToEnemy > enemyAvoidanceWindowX)
        {
            return false;
        }

        return HasDangerousVerticalOverlap(enemyPosition);
    }

    private void RewardPassedEnemies(float direction)
    {
        if (!enableEnemyAwareness || !rewardPassedEnemies || enemyPassReward <= 0f)
        {
            return;
        }

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker != null && marker.IsObservable && marker.IsDangerous)
            {
                TryRewardPassedEnemy(marker.ObservationTransform, marker.GetObservationPosition(), marker.name, direction);
            }
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard != null && hazard.AffectsAgent)
            {
                TryRewardPassedEnemy(hazard.transform, hazard.transform.position, hazard.name, direction);
            }
        }
    }

    private void TryRewardPassedEnemy(Transform enemyTransform, Vector2 enemyPosition, string enemyName, float direction)
    {
        if (enemyTransform == null)
        {
            return;
        }

        if (rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return;
        }

        Vector2 deltaFromEnemy = (Vector2)transform.position - enemyPosition;
        float passedDistance = deltaFromEnemy.x * direction;

        if (passedDistance < enemyPassMargin || Mathf.Abs(deltaFromEnemy.y) > enemyDetectionRangeY)
        {
            return;
        }

        rewardedEnemyTransforms.Add(enemyTransform);
        AddReward(enemyPassReward);
        LogEnemyRewardEvent($"[ENEMY PASS] reward={enemyPassReward:F3} enemy={enemyName}", false);
    }

    private void ApplyEnemyDangerProximityPenalty(float direction)
    {
        if (!enableEnemyAwareness || enemyDangerProximityPenalty >= 0f)
        {
            return;
        }

        EdgeRunnerEnemyMarker[] markers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);

        for (int i = 0; i < markers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = markers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            TryApplyEnemyDangerProximityPenalty(
                marker.ObservationTransform,
                marker.GetObservationPosition(),
                marker.name,
                direction
            );
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard == null || !hazard.AffectsAgent)
            {
                continue;
            }

            TryApplyEnemyDangerProximityPenalty(hazard.transform, hazard.transform.position, hazard.name, direction);
        }
    }

    private void TryApplyEnemyDangerProximityPenalty(
        Transform enemyTransform,
        Vector2 enemyPosition,
        string enemyName,
        float direction)
    {
        if (enemyTransform == null || rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return;
        }

        Vector2 deltaFromEnemy = (Vector2)transform.position - enemyPosition;
        float passedDistance = deltaFromEnemy.x * direction;

        if (passedDistance >= enemyPassMargin)
        {
            return;
        }

        float horizontalDistance = Mathf.Abs(deltaFromEnemy.x);

        if (horizontalDistance > enemyDangerProximityHorizontalRange || !HasDangerousVerticalOverlap(enemyPosition))
        {
            return;
        }

        AddReward(enemyDangerProximityPenalty);
        LogEnemyRewardEvent(
            $"[ENEMY PROXIMITY] penalty={enemyDangerProximityPenalty:F4} enemy={enemyName} distance={horizontalDistance:F3}",
            true
        );
    }

    private bool HasDangerousVerticalOverlap(Vector2 enemyPosition)
    {
        float tolerance = enemyVerticalDangerTolerance > 0f
            ? enemyVerticalDangerTolerance
            : enemyDangerProximityVerticalTolerance;
        return HasDangerousVerticalOverlap(enemyPosition, tolerance);
    }

    private bool HasDangerousVerticalOverlap(Vector2 enemyPosition, float tolerance)
    {
        tolerance = Mathf.Max(0f, tolerance);
        return Mathf.Abs(transform.position.y - enemyPosition.y) <= tolerance;
    }

    private void HandleEnemyHit(Component enemy)
    {
        if (episodeEnding)
        {
            return;
        }

        AddReward(enemyHitPenalty);
        string enemyName = enemy != null ? enemy.name : "unknown enemy";
        LogEnemyRewardEvent($"[ENEMY HIT] penalty={enemyHitPenalty:F3} enemy={enemyName}", false);
        Debug.LogWarning($"EdgeRunnerAgentV5EnemyAware: EnemyHit by {enemyName}; ending episode.");
        TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.EnemyHit);
    }

    private void LogEnemyRewardEvent(string message, bool rateLimited)
    {
        if (!debugEnemyRewards)
        {
            return;
        }

        if (rateLimited && Time.time < nextDebugEnemyRewardLogTime)
        {
            return;
        }

        if (rateLimited)
        {
            nextDebugEnemyRewardLogTime = Time.time + Mathf.Max(0.05f, debugActionLogInterval);
        }

        Debug.Log(message, this);
    }

    private Vector2 GetEnemyVelocity(Transform enemyTransform)
    {
        if (enemyTransform == null)
        {
            return Vector2.zero;
        }

        Rigidbody2D enemyBody = enemyTransform.GetComponent<Rigidbody2D>();
        return enemyBody != null ? enemyBody.linearVelocity : Vector2.zero;
    }

    private float NormalizeSignedDistance(float value, float maxAbsValue)
    {
        return Mathf.Clamp(
            value / Mathf.Max(0.0001f, maxAbsValue),
            -1f,
            1f
        );
    }

    private TerrainAnalysis AnalyzeTerrain(float direction)
    {
        int forwardCount = Mathf.Max(1, forwardTerrainSampleCount);
        int backwardCount = Mathf.Max(1, backwardTerrainSampleCount);
        int verticalCount = Mathf.Max(1, verticalSensorCount);
        WallProbe wallProbe = ProbeWall(direction);

        TerrainAnalysis analysis = new TerrainAnalysis(forwardCount, backwardCount, verticalCount)
        {
            distanceToGapStartNormalized = 1f,
            distanceToLandingNormalized = 1f,
            estimatedGapWidthNormalized = 0f,
            landingDeltaYNormalized = 0f,
            wallAhead = wallProbe.hasWall,
            wallDistanceNormalized = wallProbe.distanceNormalized,
            nextGapOrObstacleDistanceNormalized = 1f,
            flatGroundAhead = true
        };

        GroundProbe immediateProbe = ProbeGround(direction, immediateAheadDistance);
        analysis.hasGroundImmediatelyAhead =
            immediateProbe.hasGround &&
            Mathf.Abs(immediateProbe.deltaY) <= maxExpectedHeightDelta;

        float scanStep = GetFineScanStep(forwardCount, gapSensorRange);
        float gapStart = -1f;
        float landingDistance = -1f;
        GroundProbe landingProbe = default;

        for (float offset = firstTerrainSampleOffset; offset <= gapSensorRange + 0.001f; offset += scanStep)
        {
            GroundProbe probe = ProbeGround(direction, offset);
            bool isFlat = probe.hasGround && Mathf.Abs(probe.deltaY) <= safeDropThreshold;
            bool isSafeLowerGround =
                probe.hasGround &&
                probe.deltaY < -safeDropThreshold &&
                Mathf.Abs(probe.deltaY) <= maxExpectedHeightDelta;

            if (!isFlat)
            {
                analysis.flatGroundAhead = false;
            }

            if (isSafeLowerGround)
            {
                analysis.safeDropAhead = true;
            }

            if (!probe.hasGround && gapStart < 0f)
            {
                gapStart = offset;
                analysis.hasGapAhead = true;
            }
            else if (gapStart >= 0f && probe.hasGround)
            {
                landingDistance = offset;
                landingProbe = probe;
                analysis.hasLanding = true;
                break;
            }
        }

        if (analysis.wallAhead)
        {
            analysis.flatGroundAhead = false;
        }

        if (analysis.hasGapAhead)
        {
            analysis.distanceToGapStartNormalized = NormalizeDistance(gapStart, gapSensorRange);

            float gapEnd = analysis.hasLanding ? landingDistance : gapSensorRange;
            float estimatedGapWidth = Mathf.Max(0f, gapEnd - gapStart);
            analysis.estimatedGapWidthNormalized = NormalizeDistance(estimatedGapWidth, maxExpectedGapWidth);

            if (analysis.hasLanding)
            {
                analysis.distanceToLandingNormalized = NormalizeDistance(landingDistance, gapSensorRange);
                analysis.landingDeltaYNormalized = NormalizeHeight(landingProbe.deltaY);
            }
        }

        float nextHazardDistance = gapSensorRange;

        if (analysis.hasGapAhead)
        {
            nextHazardDistance = Mathf.Min(nextHazardDistance, gapStart);
        }

        if (analysis.wallAhead)
        {
            nextHazardDistance = Mathf.Min(nextHazardDistance, wallProbe.distance);
        }

        analysis.nextGapOrObstacleDistanceNormalized = NormalizeDistance(nextHazardDistance, gapSensorRange);

        for (int i = 0; i < forwardCount; i++)
        {
            float t = forwardCount == 1 ? 0f : (float)i / (forwardCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, forwardSensorRange, t);
            GroundProbe probe = ProbeGround(direction, offset);
            analysis.forwardSamples[i] = GetTerrainSampleValue(probe);
        }

        for (int i = 0; i < backwardCount; i++)
        {
            float t = backwardCount == 1 ? 0f : (float)i / (backwardCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, backwardSensorRange, t);
            GroundProbe probe = ProbeGround(-direction, offset);
            analysis.backwardSamples[i] = GetTerrainSampleValue(probe);
        }

        for (int i = 0; i < verticalCount; i++)
        {
            float t = verticalCount == 1 ? 0f : (float)i / (verticalCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, forwardSensorRange, t);
            VerticalProbe probe = ProbeVerticalClearance(direction, offset);
            analysis.verticalSamples[i] = GetVerticalSampleValue(probe);
        }

        return analysis;
    }

    private GroundProbe ProbeGround(float direction, float forwardOffset)
    {
        Vector2 origin =
            (Vector2)transform.position +
            new Vector2(direction * forwardOffset, sensorVerticalOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, frontDownSensorRange, groundLayer);

        if (hit.collider == null)
        {
            return new GroundProbe
            {
                hasGround = false,
                origin = origin,
                point = origin + Vector2.down * frontDownSensorRange,
                deltaY = 0f
            };
        }

        float referenceY = groundCheck != null ? groundCheck.position.y : transform.position.y;

        return new GroundProbe
        {
            hasGround = true,
            origin = origin,
            point = hit.point,
            deltaY = hit.point.y - referenceY
        };
    }

    private VerticalProbe ProbeVerticalClearance(float direction, float forwardOffset)
    {
        Vector2 origin =
            (Vector2)transform.position +
            new Vector2(direction * forwardOffset, sensorVerticalOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, verticalSensorRange, groundLayer);

        if (hit.collider == null)
        {
            return new VerticalProbe
            {
                hasHit = false,
                origin = origin,
                point = origin + Vector2.up * verticalSensorRange,
                distance = verticalSensorRange
            };
        }

        return new VerticalProbe
        {
            hasHit = true,
            origin = origin,
            point = hit.point,
            distance = hit.distance
        };
    }

    private WallProbe ProbeWall(float direction)
    {
        Vector2 origin =
            (Vector2)transform.position +
            new Vector2(0f, wallRayVerticalOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0f), wallSensorRange, groundLayer);

        if (hit.collider == null)
        {
            return new WallProbe
            {
                hasWall = false,
                origin = origin,
                point = origin + new Vector2(direction, 0f) * wallSensorRange,
                distance = wallSensorRange,
                distanceNormalized = 1f
            };
        }

        return new WallProbe
        {
            hasWall = true,
            origin = origin,
            point = hit.point,
            distance = hit.distance,
            distanceNormalized = NormalizeDistance(hit.distance, wallSensorRange)
        };
    }

    private float GetTerrainSampleValue(GroundProbe probe)
    {
        if (!probe.hasGround)
        {
            return -1f;
        }

        if (Mathf.Abs(probe.deltaY) <= safeDropThreshold)
        {
            return 1f;
        }

        if (probe.deltaY > safeDropThreshold)
        {
            return 0.5f;
        }

        if (Mathf.Abs(probe.deltaY) <= maxExpectedHeightDelta)
        {
            return 0f;
        }

        return -1f;
    }

    private float GetVerticalSampleValue(VerticalProbe probe)
    {
        if (!probe.hasHit)
        {
            return 1f;
        }

        return Mathf.Clamp01(probe.distance / Mathf.Max(0.0001f, verticalSensorRange));
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRange, groundLayer);
    }

    private bool IsWallAhead(float direction)
    {
        return ProbeWall(direction).hasWall;
    }

    private float GetForwardDirection()
    {
        return GetGoalHorizontalDirection();
    }

    private float GetGoalHorizontalDirection()
    {
        if (goal == null)
        {
            return 1f;
        }

        float goalDirectionX = goal.position.x - transform.position.x;

        if (Mathf.Abs(goalDirectionX) <= HorizontalGoalDirectionDeadZone)
        {
            return 1f;
        }

        return Mathf.Sign(goalDirectionX);
    }

    private bool HasMeaningfulHorizontalGoalDirection()
    {
        return goal != null &&
               Mathf.Abs(goal.position.x - transform.position.x) > HorizontalGoalDirectionDeadZone;
    }

    private float GetGoalDistanceX()
    {
        if (goal == null)
        {
            return 0f;
        }

        return Mathf.Abs(goal.position.x - transform.position.x);
    }

    private float GetDistanceToGoal()
    {
        if (goal == null)
        {
            return 0f;
        }

        return Vector2.Distance(transform.position, goal.position);
    }

    private float GetMaxHorizontalSpeed()
    {
        return Mathf.Max(0.0001f, normalMoveSpeed, sprintMoveSpeed);
    }

    private float NormalizeDistance(float value, float maxValue)
    {
        return Mathf.Clamp01(value / Mathf.Max(0.0001f, maxValue));
    }

    private float NormalizeHeight(float value)
    {
        return Mathf.Clamp(
            value / Mathf.Max(0.0001f, maxExpectedHeightDelta),
            -1f,
            1f
        );
    }

    private float GetFineScanStep(int sampleCount, float sensorRange)
    {
        return Mathf.Max(0.1f, sensorRange / Mathf.Max(2f, sampleCount * 2f));
    }

    private void OnValidate()
    {
        normalMoveSpeed = Mathf.Max(0.1f, normalMoveSpeed);
        sprintMoveSpeed = Mathf.Max(normalMoveSpeed, sprintMoveSpeed);
        jumpForce = Mathf.Max(0.1f, jumpForce);
        groundCheckRange = Mathf.Max(0.01f, groundCheckRange);
        forwardTerrainSampleCount = Mathf.Max(1, forwardTerrainSampleCount);
        backwardTerrainSampleCount = Mathf.Max(1, backwardTerrainSampleCount);
        verticalSensorCount = Mathf.Max(1, verticalSensorCount);
        forwardSensorRange = Mathf.Max(0.1f, forwardSensorRange);
        backwardSensorRange = Mathf.Max(0.1f, backwardSensorRange);
        verticalSensorRange = Mathf.Max(0.1f, verticalSensorRange);
        gapSensorRange = Mathf.Max(0.1f, gapSensorRange);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        maxAllowedBacktrackDistance = Mathf.Max(0f, maxAllowedBacktrackDistance);
        firstTerrainSampleOffset = Mathf.Max(0f, firstTerrainSampleOffset);
        immediateAheadDistance = Mathf.Max(0f, immediateAheadDistance);
        frontDownSensorRange = Mathf.Max(0.1f, frontDownSensorRange);
        safeDropThreshold = Mathf.Max(0.01f, safeDropThreshold);
        maxExpectedGapWidth = Mathf.Max(0.1f, maxExpectedGapWidth);
        maxExpectedHeightDelta = Mathf.Max(0.1f, maxExpectedHeightDelta);
        wallSensorRange = Mathf.Max(0f, wallSensorRange);
        goalReachDistance = Mathf.Max(0.01f, goalReachDistance);
        retreatPenalty = Mathf.Min(0f, retreatPenalty);
        retreatEndDistance = Mathf.Max(0f, retreatEndDistance);
        microTimeoutSeconds = Mathf.Max(0f, microTimeoutSeconds);
        microTimeoutPenalty = Mathf.Min(0f, microTimeoutPenalty);
        enemyDetectionRangeX = Mathf.Max(0.1f, enemyDetectionRangeX);
        enemyDetectionRangeY = Mathf.Max(0.1f, enemyDetectionRangeY);
        enemyObservationSlots = Mathf.Clamp(enemyObservationSlots, 1, EnemyObservationCount / 4);
        enemyHitPenalty = Mathf.Min(0f, enemyHitPenalty);
        enemyPassReward = Mathf.Max(0f, enemyPassReward);
        enemyPassMargin = Mathf.Max(0f, enemyPassMargin);
        enemyDangerProximityPenalty = Mathf.Min(0f, enemyDangerProximityPenalty);
        enemyDangerProximityHorizontalRange = Mathf.Max(0f, enemyDangerProximityHorizontalRange);
        enemyDangerProximityVerticalTolerance = Mathf.Max(0f, enemyDangerProximityVerticalTolerance);
        enemyApproachPenalty = Mathf.Min(0f, enemyApproachPenalty);
        enemyJumpCueReward = Mathf.Max(0f, enemyJumpCueReward);
        earlyEnemyJumpPenalty = Mathf.Min(0f, earlyEnemyJumpPenalty);
        jumpCommitReward = Mathf.Max(0f, jumpCommitReward);
        enemyAvoidanceWindowX = Mathf.Max(0f, enemyAvoidanceWindowX);
        enemyVerticalDangerTolerance = Mathf.Max(0f, enemyVerticalDangerTolerance);
        enemyJumpCueMinUpVelocity = Mathf.Max(0f, enemyJumpCueMinUpVelocity);
        enemyActionMaskWindowX = Mathf.Max(0f, enemyActionMaskWindowX);
        enemyActionMaskVerticalTolerance = Mathf.Max(0f, enemyActionMaskVerticalTolerance);
        enemyForcedJumpWindowX = Mathf.Max(0f, enemyForcedJumpWindowX);
        enemyForcedJumpMinDistance = Mathf.Max(0f, enemyForcedJumpMinDistance);
        enemyForcedJumpMaxDistance = Mathf.Max(enemyForcedJumpMinDistance, enemyForcedJumpMaxDistance);
        enemyForcedJumpVerticalTolerance = Mathf.Max(0f, enemyForcedJumpVerticalTolerance);
        jumpCommitMinDistance = Mathf.Max(0f, jumpCommitMinDistance);
        jumpCommitMaxDistance = Mathf.Max(jumpCommitMinDistance, jumpCommitMaxDistance);
        prematureJumpMinThreatDistance = Mathf.Max(0f, prematureJumpMinThreatDistance);
        prematureJumpMaxThreatDistance = Mathf.Max(prematureJumpMinThreatDistance, prematureJumpMaxThreatDistance);
        episodeStartSettleMaxSeconds = Mathf.Max(0.01f, episodeStartSettleMaxSeconds);
        airCommitDuration = Mathf.Max(0.01f, airCommitDuration);
        debugActionLogInterval = Mathf.Max(0.05f, debugActionLogInterval);
        debugActionTraceInterval = Mathf.Max(0.01f, debugActionTraceInterval);
        debugTrainingActionStatsInterval = Mathf.Max(1, debugTrainingActionStatsInterval);
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRange);
        }

        float direction = goal != null
            ? (goal.position.x >= transform.position.x ? 1f : -1f)
            : 1f;

        WallProbe wallProbe = ProbeWall(direction);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(wallProbe.origin, wallProbe.origin + new Vector2(direction, 0f) * wallSensorRange);

        if (wallProbe.hasWall)
        {
            Gizmos.DrawSphere(wallProbe.point, 0.06f);
        }

        GroundProbe immediateProbe = ProbeGround(direction, immediateAheadDistance);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(immediateProbe.origin, immediateProbe.origin + Vector2.down * frontDownSensorRange);
        Gizmos.DrawSphere(immediateProbe.origin, 0.05f);

        if (immediateProbe.hasGround)
        {
            Gizmos.DrawSphere(immediateProbe.point, 0.06f);
        }

        int forwardCount = Mathf.Max(1, forwardTerrainSampleCount);
        int backwardCount = Mathf.Max(1, backwardTerrainSampleCount);
        int verticalCount = Mathf.Max(1, verticalSensorCount);
        float gapScanStep = GetFineScanStep(forwardCount, gapSensorRange);

        Gizmos.color = Color.blue;
        for (float offset = firstTerrainSampleOffset; offset <= gapSensorRange + 0.001f; offset += gapScanStep)
        {
            GroundProbe probe = ProbeGround(direction, offset);
            Gizmos.DrawLine(probe.origin, probe.origin + Vector2.down * frontDownSensorRange);
        }

        for (int i = 0; i < forwardCount; i++)
        {
            float t = forwardCount == 1 ? 0f : (float)i / (forwardCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, forwardSensorRange, t);
            GroundProbe probe = ProbeGround(direction, offset);
            float sampleValue = GetTerrainSampleValue(probe);

            Gizmos.color = GetSampleGizmoColor(sampleValue);
            Gizmos.DrawLine(probe.origin, probe.origin + Vector2.down * frontDownSensorRange);
            Gizmos.DrawSphere(probe.origin, 0.05f);

            if (probe.hasGround)
            {
                Gizmos.DrawSphere(probe.point, 0.06f);
            }
        }

        for (int i = 0; i < backwardCount; i++)
        {
            float t = backwardCount == 1 ? 0f : (float)i / (backwardCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, backwardSensorRange, t);
            GroundProbe probe = ProbeGround(-direction, offset);
            float sampleValue = GetTerrainSampleValue(probe);

            Gizmos.color = GetBackwardSampleGizmoColor(sampleValue);
            Gizmos.DrawLine(probe.origin, probe.origin + Vector2.down * frontDownSensorRange);
            Gizmos.DrawSphere(probe.origin, 0.05f);

            if (probe.hasGround)
            {
                Gizmos.DrawSphere(probe.point, 0.06f);
            }
        }

        for (int i = 0; i < verticalCount; i++)
        {
            float t = verticalCount == 1 ? 0f : (float)i / (verticalCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, forwardSensorRange, t);
            VerticalProbe probe = ProbeVerticalClearance(direction, offset);

            Gizmos.color = GetVerticalGizmoColor(probe);
            Gizmos.DrawLine(probe.origin, probe.origin + Vector2.up * verticalSensorRange);
            Gizmos.DrawSphere(probe.origin, 0.05f);

            if (probe.hasHit)
            {
                Gizmos.DrawSphere(probe.point, 0.06f);
            }
        }

        if (debugEnemyRayObservations)
        {
            DrawEnemyRayGizmos(direction);
        }
    }

    private void DrawEnemyRayGizmos(float direction)
    {
        float range = Mathf.Max(0.1f, enemyDetectionRangeX);

        for (int i = 0; i < EnemyRayCount; i++)
        {
            EnemyRayProbe probe = ProbeEnemyRay(direction, i, range);
            Gizmos.color = GetEnemyRayGizmoColor(i, probe.hasHit);
            Gizmos.DrawLine(probe.origin, probe.point);
            Gizmos.DrawSphere(probe.origin, 0.05f);

            if (probe.hasHit)
            {
                Gizmos.DrawSphere(probe.point, 0.08f);
            }
        }
    }

    private Color GetEnemyRayGizmoColor(int rayIndex, bool hasHit)
    {
        if (IsFrontEnemyRay(rayIndex))
        {
            return hasHit ? new Color(1f, 0.2f, 0.1f) : new Color(1f, 0.75f, 0.15f);
        }

        if (rayIndex == EnemyRayBackMid)
        {
            return hasHit ? new Color(0.2f, 0.55f, 1f) : new Color(0.45f, 0.8f, 1f);
        }

        if (rayIndex == EnemyRayDownForward)
        {
            return hasHit ? new Color(1f, 0.15f, 0.9f) : new Color(0.95f, 0.55f, 1f);
        }

        return hasHit ? Color.red : Color.gray;
    }

    private Color GetSampleGizmoColor(float sampleValue)
    {
        if (sampleValue > 0.75f)
        {
            return Color.green;
        }

        if (sampleValue > 0.25f)
        {
            return Color.yellow;
        }

        if (sampleValue >= 0f)
        {
            return Color.magenta;
        }

        return Color.red;
    }

    private Color GetBackwardSampleGizmoColor(float sampleValue)
    {
        if (sampleValue > 0.75f)
        {
            return new Color(0.2f, 0.8f, 1f);
        }

        if (sampleValue > 0.25f)
        {
            return new Color(1f, 0.75f, 0.1f);
        }

        if (sampleValue >= 0f)
        {
            return new Color(0.8f, 0.3f, 1f);
        }

        return new Color(1f, 0.25f, 0.25f);
    }

    private Color GetVerticalGizmoColor(VerticalProbe probe)
    {
        return probe.hasHit
            ? new Color(1f, 0.4f, 0.9f)
            : new Color(0.45f, 0.9f, 1f);
    }

    private struct GroundProbe
    {
        public bool hasGround;
        public Vector2 origin;
        public Vector2 point;
        public float deltaY;
    }

    private struct VerticalProbe
    {
        public bool hasHit;
        public Vector2 origin;
        public Vector2 point;
        public float distance;
    }

    private struct WallProbe
    {
        public bool hasWall;
        public Vector2 origin;
        public Vector2 point;
        public float distance;
        public float distanceNormalized;
    }

    private struct EnemyRayProbe
    {
        public bool hasHit;
        public int rayIndex;
        public Vector2 origin;
        public Vector2 point;
        public float distance;
        public float distanceNormalized;
        public Transform enemyTransform;
        public string enemyName;
        public string rayName;
    }

    private enum EnemyRayThreatMode
    {
        FrontOnly,
        FrontOrDownForward,
        Any
    }

    private enum EnemyJumpTimingZone
    {
        TooEarly,
        SweetSpot,
        TooLate
    }

    private struct EnemyCandidate
    {
        public bool exists;
        public Transform transform;
        public Vector2 delta;
        public Vector2 velocity;
        public float forwardDistance;
        public float priority;
        public bool isDangerous;
    }

    private struct TerrainAnalysis
    {
        public bool hasGroundImmediatelyAhead;
        public float distanceToGapStartNormalized;
        public bool hasGapAhead;
        public float distanceToLandingNormalized;
        public float estimatedGapWidthNormalized;
        public float landingDeltaYNormalized;
        public bool hasLanding;
        public bool safeDropAhead;
        public bool flatGroundAhead;
        public bool wallAhead;
        public float wallDistanceNormalized;
        public float nextGapOrObstacleDistanceNormalized;
        public float[] forwardSamples;
        public float[] backwardSamples;
        public float[] verticalSamples;

        public TerrainAnalysis(int forwardSampleCount, int backwardSampleCount, int verticalSampleCount)
        {
            hasGroundImmediatelyAhead = false;
            distanceToGapStartNormalized = 1f;
            hasGapAhead = false;
            distanceToLandingNormalized = 1f;
            estimatedGapWidthNormalized = 0f;
            landingDeltaYNormalized = 0f;
            hasLanding = false;
            safeDropAhead = false;
            flatGroundAhead = false;
            wallAhead = false;
            wallDistanceNormalized = 1f;
            nextGapOrObstacleDistanceNormalized = 1f;
            forwardSamples = new float[forwardSampleCount];
            backwardSamples = new float[backwardSampleCount];
            verticalSamples = new float[verticalSampleCount];
        }
    }
}
