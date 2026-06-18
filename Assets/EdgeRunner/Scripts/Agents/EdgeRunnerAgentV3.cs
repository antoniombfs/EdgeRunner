using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Serialization;

public class EdgeRunnerAgentV3 : Agent
{
    /*
     * Expected Vector Observation Space Size with the default terrainSampleCount = 11:
     * 6 base observations + 10 terrain summary observations + 11 terrain profile samples = 27.
     *
     * Formula if terrainSampleCount changes:
     * Vector Observation Space Size = 16 + terrainSampleCount.
     */

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private GapGenerator gapGenerator;
    [SerializeField] private bool useMixedLevelGenerator = false;
    [SerializeField] private MixedLevelGenerator mixedLevelGenerator;
    [SerializeField] private EdgeRunnerEvaluationManager evaluationManager;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private bool allowJump = true;

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
    [FormerlySerializedAs("terrainLookAheadDistance")]
    [SerializeField] private float forwardSensorRange = 9f;
    [SerializeField] private float gapSensorRange = 9f;
    [SerializeField] private int terrainSampleCount = 11;
    [SerializeField] private float firstTerrainSampleOffset = 0.35f;
    [SerializeField] private float immediateAheadDistance = 0.7f;
    [FormerlySerializedAs("scanDownDistance")]
    [SerializeField] private float frontDownSensorRange = 3.75f;
    [SerializeField] private float sensorVerticalOffset = 0.15f;
    [SerializeField] private float safeDropThreshold = 0.35f;
    [SerializeField] private float maxExpectedGapWidth = 4.5f;
    [SerializeField] private float maxExpectedHeightDelta = 2f;

    [Header("Wall Detection")]
    [FormerlySerializedAs("wallRayDistance")]
    [SerializeField] private float wallSensorRange = 1.05f;
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
    [SerializeField] private bool maskMoveAwayFromGoal = true;
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

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float bestXReached;
    private float timeSinceBestXProgress;
    private float episodeTime;
    private float previousGoalDistanceX;
    private float previousDistanceToGoal;
    private float bestDistanceToGoal;
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
    private int lastEpisodeEndFrame = -999;
    private float episodeStartRealtime;

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
            evaluationManager = FindObjectOfType<EdgeRunnerEvaluationManager>();
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
        timeSinceDistanceProgress = 0f;
        wasGroundedLastStep = IsGrounded();
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        lastJumpAction = 0;
        jumpedForGap = false;
        crossedGapInAir = false;
        jumpConsumedUntilLanding = false;
        leftGroundAfterJump = false;
        waitingForJumpRelease = false;
        heuristicJumpPressedThisStep = false;
        episodeEnding = false;
        episodeStartRealtime = Time.realtimeSinceStartup;

        NotifyEvaluationEpisodeStarted();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float direction = GetForwardDirection();
        TerrainAnalysis terrain = AnalyzeTerrain(direction);

        float velX = rb != null ? rb.linearVelocity.x / moveSpeed : 0f;
        float velY = rb != null ? rb.linearVelocity.y / 15f : 0f;
        bool grounded = IsGrounded();

        Vector2 toGoal = goal != null
            ? (Vector2)(goal.position - transform.position)
            : Vector2.zero;

        // 6 base observations.
        sensor.AddObservation(Mathf.Clamp(velX, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(velY, -1f, 1f));
        sensor.AddObservation(grounded ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp(toGoal.x / 20f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toGoal.y / 10f, -1f, 1f));
        sensor.AddObservation(terrain.wallAhead ? 1f : 0f);

        // 10 terrain summary observations.
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

        // terrainSampleCount profile observations. Default: 11.
        for (int i = 0; i < terrain.samples.Length; i++)
        {
            sensor.AddObservation(terrain.samples[i]);
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        float direction = GetForwardDirection();

        if (maskMoveAwayFromGoal)
        {
            if (direction > 0f)
            {
                actionMask.SetActionEnabled(0, 1, false);
            }
            else
            {
                actionMask.SetActionEnabled(0, 2, false);
            }
        }

        if (!allowIdleAction)
        {
            actionMask.SetActionEnabled(0, 0, false);
        }

        bool grounded = IsGrounded();
        bool canJumpImmediately = CanJumpFromGroundState(grounded);

        if (!allowJump || !canJumpImmediately || waitingForJumpRelease)
        {
            actionMask.SetActionEnabled(1, 1, false);
            return;
        }

        if (maskUselessJumps && canJumpImmediately)
        {
            TerrainAnalysis terrain = AnalyzeTerrain(direction);

            if (!IsUsefulJumpSituation(terrain))
            {
                actionMask.SetActionEnabled(1, 1, false);
            }
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

        episodeTime += Time.fixedDeltaTime;

        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];

        float direction = GetForwardDirection();
        TerrainAnalysis terrain = AnalyzeTerrain(direction);

        float moveX = moveAction switch
        {
            1 => -1f,
            2 => 1f,
            _ => 0f
        };

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        bool grounded = IsGrounded();
        ApplyLocomotionReward(direction, moveX, grounded);
        ApplyDistanceProgressReward();

        bool jumpRequested = jumpAction == 1;
        bool jumpPressedThisStep = jumpRequested && lastJumpAction == 0;
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        d[1] = 0;

        float h = Input.GetAxisRaw("Horizontal");

        if (h < -0.1f)
        {
            d[0] = 1;
        }
        else if (h > 0.1f)
        {
            d[0] = 2;
        }

        heuristicJumpPressedThisStep = allowJump && Input.GetKeyDown(KeyCode.Space);

        if (heuristicJumpPressedThisStep)
        {
            d[1] = 1;
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

    public void GoalReached()
    {
        AddReward(goalReward);
        TryEndEpisodeSafely(EdgeRunnerEpisodeEndReason.Success);
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
        float forwardSpeed = rb.linearVelocity.x * direction / moveSpeed;

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
        float forwardVelocity = rb.linearVelocity.x * direction;

        if (moveX * direction > minUsefulMoveInput)
        {
            AddReward(forwardActionReward);
        }
        else if (moveX * direction < -minUsefulMoveInput)
        {
            AddReward(wrongDirectionActionPenalty);
        }
        else if (grounded)
        {
            AddReward(idlePenalty);
        }

        if (forwardVelocity > minUsefulForwardVelocity)
        {
            AddReward(Mathf.Clamp01(forwardVelocity / moveSpeed) * forwardVelocityReward);
        }
    }

    private void ApplyDistanceProgressReward()
    {
        if (goal == null)
        {
            return;
        }

        float currentDistance = GetDistanceToGoal();
        float delta = previousDistanceToGoal - currentDistance;

        if (delta > 0f)
        {
            float reward = Mathf.Clamp(delta * distanceProgressRewardScale, 0f, maxDistanceProgressReward);
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
        float progressDelta = previousGoalDistanceX - currentGoalDistanceX;

        if (progressDelta > 0f)
        {
            AddReward(Mathf.Min(progressDelta * progressRewardScale, maxProgressRewardPerStep));
        }

        previousGoalDistanceX = currentGoalDistanceX;

        if (moveX * direction < -0.1f)
        {
            AddReward(backtrackPenalty);
        }

        float forwardX = transform.position.x * direction;

        if (forwardX > bestXReached + bestXProgressThreshold)
        {
            AddReward(milestoneReward);
            bestXReached = forwardX;
            timeSinceBestXProgress = 0f;
        }
        else
        {
            timeSinceBestXProgress += Time.fixedDeltaTime;
        }
    }

    private TerrainAnalysis AnalyzeTerrain(float direction)
    {
        int sampleCount = Mathf.Max(1, terrainSampleCount);
        TerrainAnalysis analysis = new TerrainAnalysis(sampleCount)
        {
            distanceToGapStartNormalized = 1f,
            distanceToLandingNormalized = 1f,
            estimatedGapWidthNormalized = 0f,
            landingDeltaYNormalized = 0f,
            wallAhead = IsWallAhead(direction),
            flatGroundAhead = true
        };

        GroundProbe immediateProbe = ProbeGround(direction, immediateAheadDistance);
        analysis.hasGroundImmediatelyAhead =
            immediateProbe.hasGround &&
            Mathf.Abs(immediateProbe.deltaY) <= maxExpectedHeightDelta;

        float scanStep = GetFineScanStep(sampleCount, gapSensorRange);
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

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
            float offset = Mathf.Lerp(firstTerrainSampleOffset, forwardSensorRange, t);
            GroundProbe probe = ProbeGround(direction, offset);
            analysis.samples[i] = GetTerrainSampleValue(probe);
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
        Vector2 origin =
            (Vector2)transform.position +
            new Vector2(0f, wallRayVerticalOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0f), wallSensorRange, groundLayer);
        return hit.collider != null;
    }

    private float GetForwardDirection()
    {
        if (goal == null)
        {
            return 1f;
        }

        return goal.position.x >= transform.position.x ? 1f : -1f;
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
        groundCheckRange = Mathf.Max(0.01f, groundCheckRange);
        terrainSampleCount = Mathf.Max(1, terrainSampleCount);
        forwardSensorRange = Mathf.Max(0.1f, forwardSensorRange);
        gapSensorRange = Mathf.Max(0.1f, gapSensorRange);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        firstTerrainSampleOffset = Mathf.Max(0f, firstTerrainSampleOffset);
        immediateAheadDistance = Mathf.Max(0f, immediateAheadDistance);
        frontDownSensorRange = Mathf.Max(0.1f, frontDownSensorRange);
        safeDropThreshold = Mathf.Max(0.01f, safeDropThreshold);
        maxExpectedGapWidth = Mathf.Max(0.1f, maxExpectedGapWidth);
        maxExpectedHeightDelta = Mathf.Max(0.1f, maxExpectedHeightDelta);
        wallSensorRange = Mathf.Max(0f, wallSensorRange);
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

        Vector2 wallOrigin =
            (Vector2)transform.position +
            new Vector2(0f, wallRayVerticalOffset);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector2(direction, 0f) * wallSensorRange);

        GroundProbe immediateProbe = ProbeGround(direction, immediateAheadDistance);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(immediateProbe.origin, immediateProbe.origin + Vector2.down * frontDownSensorRange);
        Gizmos.DrawSphere(immediateProbe.origin, 0.05f);

        if (immediateProbe.hasGround)
        {
            Gizmos.DrawSphere(immediateProbe.point, 0.06f);
        }

        int sampleCount = Mathf.Max(1, terrainSampleCount);
        float gapScanStep = GetFineScanStep(sampleCount, gapSensorRange);

        Gizmos.color = Color.blue;
        for (float offset = firstTerrainSampleOffset; offset <= gapSensorRange + 0.001f; offset += gapScanStep)
        {
            GroundProbe probe = ProbeGround(direction, offset);
            Gizmos.DrawLine(probe.origin, probe.origin + Vector2.down * frontDownSensorRange);
        }

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
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

    private struct GroundProbe
    {
        public bool hasGround;
        public Vector2 origin;
        public Vector2 point;
        public float deltaY;
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
        public float[] samples;

        public TerrainAnalysis(int sampleCount)
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
            samples = new float[sampleCount];
        }
    }
}
