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
    [SerializeField] private float enemyHitPenalty = -2.5f;
    [SerializeField] private bool rewardPassedEnemies = true;
    [SerializeField] private float enemyPassReward = 0.35f;
    [SerializeField] private float enemyPassMargin = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool debugV5Actions = false;
    [SerializeField] private float debugActionLogInterval = 1.0f;
    [SerializeField] private bool debugEnemyObservations = false;

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
    private int lastEpisodeEndFrame = -999;
    private float episodeStartRealtime;
    private float nextDebugActionLogTime;
    private readonly HashSet<Transform> rewardedEnemyTransforms = new HashSet<Transform>();
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
        episodeStartRealtime = Time.realtimeSinceStartup;
        nextDebugActionLogTime = 0f;
        rewardedEnemyTransforms.Clear();

        NotifyEvaluationEpisodeStarted();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float direction = GetForwardDirection();
        TerrainAnalysis terrain = AnalyzeTerrain(direction);
        List<EnemyCandidate> enemyObservation = AnalyzeEnemies(direction);
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
        AddEnemyObservations(sensor, enemyObservation);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        float direction = GetForwardDirection();
        bool blockLeft = false;
        bool blockRight = false;
        bool blockStop = false;

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

        bool grounded = IsGrounded();
        bool canJumpImmediately =
            allowJump &&
            CanJumpFromGroundState(grounded) &&
            !jumpConsumedUntilLanding &&
            !waitingForJumpRelease;

        if (!canJumpImmediately)
        {
            actionMask.SetActionEnabled(1, JumpAction, false);
        }
        else if (maskUselessJumps)
        {
            TerrainAnalysis terrain = AnalyzeTerrain(direction);

            if (!IsUsefulJumpSituation(terrain))
            {
                actionMask.SetActionEnabled(1, JumpAction, false);
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

        episodeTime += Time.fixedDeltaTime;

        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];
        int sprintAction = actions.DiscreteActions.Length > 2
            ? actions.DiscreteActions[2]
            : NoSprintAction;

        float direction = GetForwardDirection();
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

        ApplyDistanceProgressReward();
        ApplyLocomotionReward(direction, moveX, grounded);

        bool jumpRequested = jumpAction == JumpAction;
        bool jumpPressedThisStep = jumpRequested && lastJumpAction == NoJumpAction;
        bool bufferedJumpPressed = heuristicJumpPressedThisStep || (useJumpBufferForAgent && jumpPressedThisStep);
        UpdateJumpForgivenessTimers(grounded, jumpRequested, bufferedJumpPressed);

        if (ShouldExecuteJump(grounded, jumpRequested))
        {
            HandleJumpReward(moveX, direction, terrain);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpConsumedUntilLanding = true;
            leftGroundAfterJump = !grounded;
            waitingForJumpRelease = requireJumpReleaseBeforeNextJump && jumpRequested;
        }

        lastJumpAction = jumpAction;
        heuristicJumpPressedThisStep = false;

        TrackGapLandingReward(direction, grounded);
        ApplyProgressRewards(direction, moveX);
        RewardPassedEnemies(direction);

        AddReward(stepPenalty);

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

        float h = Input.GetAxisRaw("Horizontal");

        if (h < -0.1f)
        {
            d[0] = MoveLeftAction;
        }
        else if (h > 0.1f)
        {
            d[0] = MoveRightAction;
        }

        heuristicJumpPressedThisStep = allowJump && Input.GetKeyDown(KeyCode.Space);

        if (heuristicJumpPressedThisStep)
        {
            d[1] = JumpAction;
        }

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (allowSprint && sprintHeld && Mathf.Abs(h) > 0.1f)
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
        if (!debugV5Actions || Time.time < nextDebugActionLogTime)
        {
            return;
        }

        nextDebugActionLogTime = Time.time + Mathf.Max(0.05f, debugActionLogInterval);
        float distanceToGoal = goal != null ? Vector2.Distance(transform.position, goal.position) : -1f;
        Vector2 currentVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

        Debug.Log(
            "EdgeRunnerAgentV5EnemyAware actions: " +
            $"move={moveAction}, jump={jumpAction}, sprint={sprintAction}, " +
            $"horizontalInput={horizontalInput:F1}, speed={activeMoveSpeed:F2}, " +
            $"linearVelocity=({currentVelocity.x:F2}, {currentVelocity.y:F2}), " +
            $"goalDirectionX={goalDirectionX:F1}, distanceToGoal={distanceToGoal:F2}, " +
            $"maskLeft={lastMaskLeftBlocked}, maskRight={lastMaskRightBlocked}, maskStop={lastMaskStopBlocked}, " +
            $"grounded={grounded}",
            this
        );
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

        episodeEnding = true;
        lastEpisodeEndFrame = Time.frameCount;

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

    private void ApplyLocomotionReward(float direction, float moveX, bool grounded)
    {
        bool hasHorizontalGoalDirection = HasMeaningfulHorizontalGoalDirection();
        float forwardVelocity = rb.linearVelocity.x * direction;

        if (hasHorizontalGoalDirection && moveX * direction > minUsefulMoveInput)
        {
            AddReward(forwardActionReward);
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

        if (hasHorizontalGoalDirection && forwardVelocity > minUsefulForwardVelocity)
        {
            AddReward(Mathf.Clamp01(forwardVelocity / GetMaxHorizontalSpeed()) * forwardVelocityReward);
        }
    }

    private void ApplyDistanceProgressReward()
    {
        if (goal == null)
        {
            lastDistanceImprovement = 0f;
            return;
        }

        float currentDistance = GetDistanceToGoal();
        float delta = previousDistanceToGoal - currentDistance;
        lastDistanceImprovement = delta;

        if (delta > 0f)
        {
            float rewardScale = Mathf.Max(distanceProgressRewardScale, progressRewardScale);
            float rewardCap = Mathf.Max(maxDistanceProgressReward, maxProgressRewardPerStep);
            float reward = Mathf.Clamp(delta * rewardScale, 0f, rewardCap);
            AddReward(reward);
        }
        else if (delta < 0f)
        {
            float penalty = Mathf.Clamp(delta * distanceRegressionPenaltyScale, maxDistanceRegressionPenalty, 0f);
            AddReward(penalty);
        }

        if (currentDistance < bestDistanceToGoal - minDistanceProgressForReset)
        {
            bestDistanceToGoal = currentDistance;
            timeSinceDistanceProgress = 0f;
            AddReward(milestoneReward);
        }
        else
        {
            timeSinceDistanceProgress += Time.fixedDeltaTime;
        }

        previousDistanceToGoal = currentDistance;
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

            if (marker != null && marker.AffectsAgent)
            {
                TryRewardPassedEnemy(marker.transform, direction);
            }
        }

        DemoEnemyHazard[] demoHazards = FindObjectsByType<DemoEnemyHazard>(FindObjectsInactive.Exclude);

        for (int i = 0; i < demoHazards.Length; i++)
        {
            DemoEnemyHazard hazard = demoHazards[i];

            if (hazard != null && hazard.AffectsAgent)
            {
                TryRewardPassedEnemy(hazard.transform, direction);
            }
        }
    }

    private void TryRewardPassedEnemy(Transform enemyTransform, float direction)
    {
        if (enemyTransform == null)
        {
            return;
        }

        if (rewardedEnemyTransforms.Contains(enemyTransform))
        {
            return;
        }

        Vector2 deltaFromEnemy = (Vector2)(transform.position - enemyTransform.position);
        float passedDistance = deltaFromEnemy.x * direction;

        if (passedDistance < enemyPassMargin || Mathf.Abs(deltaFromEnemy.y) > enemyDetectionRangeY)
        {
            return;
        }

        rewardedEnemyTransforms.Add(enemyTransform);
        AddReward(enemyPassReward);
    }

    private void HandleEnemyHit(Component enemy)
    {
        if (episodeEnding)
        {
            return;
        }

        AddReward(enemyHitPenalty);
        string enemyName = enemy != null ? enemy.name : "unknown enemy";
        Debug.LogWarning($"EdgeRunnerAgentV5EnemyAware: EnemyHit by {enemyName}; ending episode.");
        TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.EnemyHit);
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
        enemyDetectionRangeX = Mathf.Max(0.1f, enemyDetectionRangeX);
        enemyDetectionRangeY = Mathf.Max(0.1f, enemyDetectionRangeY);
        enemyObservationSlots = Mathf.Clamp(enemyObservationSlots, 1, EnemyObservationCount / 4);
        enemyHitPenalty = Mathf.Min(0f, enemyHitPenalty);
        enemyPassReward = Mathf.Max(0f, enemyPassReward);
        enemyPassMargin = Mathf.Max(0f, enemyPassMargin);
        debugActionLogInterval = Mathf.Max(0.05f, debugActionLogInterval);
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
