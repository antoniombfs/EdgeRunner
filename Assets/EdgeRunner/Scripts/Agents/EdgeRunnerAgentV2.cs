using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class EdgeRunnerAgentV2 : Agent
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform groundCheck;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Scene Options")]
    [SerializeField] private bool allowJump = true;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Terrain Scan (Raycast Array)")]
    [SerializeField] private int scanRayCount = 5;
    [SerializeField] private float scanStartOffset = 0.5f;
    [SerializeField] private float scanStepSize = 0.5f;
    [SerializeField] private float scanDownDistance = 1.5f;
    [SerializeField] private float safeDropThreshold = 0.25f;

    [Header("Wall Detection")]
    [SerializeField] private float forwardRayDistance = 0.7f;

    [Header("Rewards")]
    [SerializeField] private float velocityRewardScale = 0.0015f;
    [SerializeField] private float goalReward = 5.0f;
    [SerializeField] private float deathPenalty = -2.0f;
    [SerializeField] private float stepPenalty = -0.0003f;
    [SerializeField] private float idleJumpPenalty = -0.01f;
    [SerializeField] private float jumpPenalty = -0.0002f;
    [SerializeField] private float flatGroundJumpPenalty = -0.015f;
    [SerializeField] private float gapJumpReward = 0.06f;
    [SerializeField] private float stuckPenalty = -0.3f;
    [SerializeField] private float milestoneReward = 0.004f;
    [SerializeField] private float minJumpMomentum = 0.35f;

    [SerializeField] private float backtrackPenalty = -0.002f;
    [SerializeField] private float backtrackMargin = 0.35f;

    [Header("Episode Control")]
    [SerializeField] private float stuckTimeLimit = 8f;
    [SerializeField] private float bestXProgressThreshold = 0.25f;
    [SerializeField] private float maxEpisodeTime = 45f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float bestXReached;
    private float timeSinceBestXProgress;
    private float episodeTime;

    private bool wasGroundedLastStep;
    private bool crossedGapInAir;

    public override void Initialize()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        bestXReached = transform.position.x;
        timeSinceBestXProgress = 0f;
        episodeTime = 0f;

        wasGroundedLastStep = IsGrounded();
        crossedGapInAir = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float velX = rb != null ? rb.linearVelocity.x / moveSpeed : 0f;
        float velY = rb != null ? rb.linearVelocity.y / 15f : 0f;
        bool grounded = IsGrounded();
        bool wallAhead = IsWallAhead();

        Vector2 toGoal = goal != null
            ? (Vector2)(goal.position - transform.position)
            : Vector2.zero;

        sensor.AddObservation(velX);
        sensor.AddObservation(velY);
        sensor.AddObservation(grounded ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp(toGoal.x / 20f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toGoal.y / 10f, -1f, 1f));
        sensor.AddObservation(wallAhead ? 1f : 0f);

        float direction = GetForwardDirection();
        for (int i = 0; i < scanRayCount; i++)
        {
            float offset = scanStartOffset + i * scanStepSize;
            float scanValue = ScanTerrain(direction, offset);
            sensor.AddObservation(scanValue);
        }

        sensor.AddObservation(velX * velX);
        sensor.AddObservation(grounded ? 0f : 1f);
        sensor.AddObservation(Mathf.Abs(velX));
    }

    private float ScanTerrain(float direction, float forwardOffset)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(direction * forwardOffset, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, scanDownDistance, groundLayer);

        if (hit.collider == null)
            return -1f;

        float referenceY = groundCheck != null ? groundCheck.position.y : transform.position.y;
        float drop = referenceY - hit.point.y;

        if (drop < safeDropThreshold)
            return 1f;

        return 0f;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!allowJump || !IsGrounded())
            actionMask.SetActionEnabled(1, 1, false);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (rb == null)
            return;

        episodeTime += Time.fixedDeltaTime;

        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];

        float moveX = moveAction switch
        {
            1 => -1f,
            2 => 1f,
            _ => 0f
        };

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        bool grounded = IsGrounded();

        float direction = GetForwardDirection();
        float scan0Now = ScanTerrain(direction, scanStartOffset);
        float scan1Now = ScanTerrain(direction, scanStartOffset + scanStepSize);

        if (!grounded && (scan0Now < 0f || scan1Now < 0f))
        {
            crossedGapInAir = true;
        }

        if (!wasGroundedLastStep && grounded && crossedGapInAir)
        {
            AddReward(0.25f);
            crossedGapInAir = false;
        }

        wasGroundedLastStep = grounded;

        if (moveX < 0f && transform.position.x < bestXReached - backtrackMargin)
            AddReward(backtrackPenalty);

        if (allowJump && jumpAction == 1 && grounded)
        {
            float currentForwardSpeed = rb.linearVelocity.x * direction / moveSpeed;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            AddReward(jumpPenalty);

            if (Mathf.Abs(moveX) < 0.1f)
                AddReward(idleJumpPenalty);

            if (!IsWallAhead())
            {
                float scan0 = ScanTerrain(direction, scanStartOffset);
                float scan1 = ScanTerrain(direction, scanStartOffset + scanStepSize);
                float scan2 = ScanTerrain(direction, scanStartOffset + scanStepSize * 2f);

                bool terrainIsFlat = scan0 > 0.5f && scan1 > 0.5f && scan2 > 0.5f;
                bool gapAhead = scan0 < 0f || scan1 < 0f || scan2 < 0f;

                if (terrainIsFlat)
                {
                    AddReward(flatGroundJumpPenalty);
                }
                else if (gapAhead)
                {
                    if (currentForwardSpeed >= minJumpMomentum)
                    {
                        AddReward(0.05f + currentForwardSpeed * gapJumpReward);
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                }
            }
        }

        if (goal != null)
        {
            float forwardVel = rb.linearVelocity.x * direction;
            if (forwardVel > 0f)
                AddReward(forwardVel / moveSpeed * velocityRewardScale);
        }

        AddReward(stepPenalty);

        if (transform.position.x > bestXReached + bestXProgressThreshold)
        {
            AddReward(milestoneReward);
            bestXReached = transform.position.x;
            timeSinceBestXProgress = 0f;
        }
        else
        {
            timeSinceBestXProgress += Time.fixedDeltaTime;
        }

        if (timeSinceBestXProgress >= stuckTimeLimit)
        {
            AddReward(stuckPenalty);
            EndEpisode();
            return;
        }

        if (episodeTime >= maxEpisodeTime)
            EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        d[1] = 0;

        float h = Input.GetAxisRaw("Horizontal");

        if (h < -0.1f) d[0] = 1;
        else if (h > 0.1f) d[0] = 2;

        if (allowJump && Input.GetKey(KeyCode.Space))
            d[1] = 1;
    }

    public void GoalReached()
    {
        AddReward(goalReward);
        EndEpisode();
    }

    public void FellOffMap()
    {
        AddReward(deathPenalty);
        EndEpisode();
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private bool IsWallAhead()
    {
        float direction = GetForwardDirection();
        Vector2 origin = (Vector2)transform.position + new Vector2(0f, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0f), forwardRayDistance, groundLayer);
        return hit.collider != null;
    }

    private float GetForwardDirection()
    {
        if (goal == null)
            return 1f;

        return goal.position.x >= transform.position.x ? 1f : -1f;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        float dir = goal != null
            ? (goal.position.x >= transform.position.x ? 1f : -1f)
            : 1f;

        Gizmos.color = Color.cyan;
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.1f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector2(dir, 0f) * forwardRayDistance);

        for (int i = 0; i < scanRayCount; i++)
        {
            float offset = scanStartOffset + i * scanStepSize;
            Vector2 origin = (Vector2)transform.position + new Vector2(dir * offset, 0.1f);
            float scanVal = ScanTerrain(dir, offset);

            Gizmos.color = scanVal > 0.5f ? Color.yellow
                         : scanVal > -0.5f ? Color.magenta
                         : Color.red;

            Gizmos.DrawLine(origin, origin + Vector2.down * scanDownDistance);
            Gizmos.DrawSphere(origin, 0.05f);
        }
    }
}