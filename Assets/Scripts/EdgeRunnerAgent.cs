using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class EdgeRunnerAgent : Agent
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform groundCheck;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Forward Detection")]
    [SerializeField] private float frontOffset = 0.6f;
    [SerializeField] private float downRayDistance = 1.2f;
    [SerializeField] private float forwardRayDistance = 0.7f;

    [Header("Rewards")]
    [SerializeField] private float progressRewardMultiplier = 0.03f;   // Aumentado: sinal mais forte para avançar
    [SerializeField] private float stepPenalty = -0.001f;              // Aumentado: pressiona mais para ser eficiente
    [SerializeField] private float unnecessaryJumpPenalty = -0.02f;    // Aumentado: mais dor por saltar à toa
    [SerializeField] private float necessaryJumpPenalty = -0.001f;
    [SerializeField] private float goalReward = 1.0f;                  // Normalizado para escala ML-Agents
    [SerializeField] private float deathPenalty = -1.0f;

    // FIX 1: Contador de saltos desnecessários consecutivos para penalização progressiva
    private int consecutiveUnnecessaryJumps = 0;

    // FIX 2: Rastrear se está a fazer progresso ou está parado
    private float timeSinceLastProgress = 0f;
    private float lastRecordedX = 0f;
    [SerializeField] private float stuckTimeLimit = 3f;               // segundos sem avançar → termina episódio
    [SerializeField] private float stuckDistanceThreshold = 0.3f;     // distância mínima para considerar "progresso"

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float previousDistanceToGoal;
    private float episodeTime = 0f;
    [SerializeField] private float maxEpisodeTime = 20f;              // timeout para forçar fim de episódio

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

        if (goal != null)
            previousDistanceToGoal = Vector2.Distance(transform.position, goal.position);
        else
            previousDistanceToGoal = 0f;

        // Reset de estado
        consecutiveUnnecessaryJumps = 0;
        timeSinceLastProgress = 0f;
        lastRecordedX = transform.position.x;
        episodeTime = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float velX = 0f;
        float velY = 0f;

        if (rb != null)
        {
            velX = rb.linearVelocity.x / moveSpeed;    // FIX 3: normalizar pela moveSpeed, não por constante
            velY = rb.linearVelocity.y / 15f;
        }

        bool grounded = IsGrounded();
        bool groundAhead = IsGroundAhead();
        bool wallAhead = IsWallAhead();

        if (goal == null)
        {
            sensor.AddObservation(velX);
            sensor.AddObservation(velY);
            sensor.AddObservation(grounded ? 1f : 0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(groundAhead ? 1f : 0f);
            sensor.AddObservation(wallAhead ? 1f : 0f);
            return;
        }

        Vector2 toGoal = goal.position - transform.position;

        sensor.AddObservation(velX);
        sensor.AddObservation(velY);
        sensor.AddObservation(grounded ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp(toGoal.x / 20f, -1f, 1f));  // FIX 4: clamp para evitar valores fora do esperado
        sensor.AddObservation(Mathf.Clamp(toGoal.y / 10f, -1f, 1f));
        sensor.AddObservation(groundAhead ? 1f : 0f);
        sensor.AddObservation(wallAhead ? 1f : 0f);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!IsGrounded())
        {
            actionMask.SetActionEnabled(1, 1, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (rb == null)
            return;

        episodeTime += Time.fixedDeltaTime;

        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];

        float moveX = 0f;

        switch (moveAction)
        {
            case 0: moveX = 0f; break;
            case 1: moveX = -1f; break;
            case 2: moveX = 1f; break;
        }

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        bool grounded = IsGrounded();

        // --- Lógica de salto ---
        if (jumpAction == 1 && grounded)
        {
            bool groundAhead = IsGroundAhead();
            bool wallAhead = IsWallAhead();

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (groundAhead && !wallAhead)
            {
                // FIX 5: penalização progressiva — quanto mais saltos desnecessários consecutivos, maior a punição
                consecutiveUnnecessaryJumps++;
                float penalty = unnecessaryJumpPenalty * Mathf.Min(consecutiveUnnecessaryJumps, 3);
                AddReward(penalty);
            }
            else
            {
                consecutiveUnnecessaryJumps = 0;
                AddReward(necessaryJumpPenalty);
            }
        }
        else if (grounded)
        {
            // FIX 6: reset do contador quando não salta (comportamento correto)
            consecutiveUnnecessaryJumps = 0;
        }

        // --- Reward de progresso ---
        if (goal != null)
        {
            float currentDistanceToGoal = Vector2.Distance(transform.position, goal.position);
            float distanceImprovement = previousDistanceToGoal - currentDistanceToGoal;

            AddReward(distanceImprovement * progressRewardMultiplier);
            previousDistanceToGoal = currentDistanceToGoal;
        }

        // --- Step penalty ---
        AddReward(stepPenalty);

        // --- FIX 7: Deteção de "stuck" — terminar episódio se não há progresso ---
        float movedDistance = Mathf.Abs(transform.position.x - lastRecordedX);
        if (movedDistance > stuckDistanceThreshold)
        {
            timeSinceLastProgress = 0f;
            lastRecordedX = transform.position.x;
        }
        else
        {
            timeSinceLastProgress += Time.fixedDeltaTime;
        }

        if (timeSinceLastProgress >= stuckTimeLimit)
        {
            AddReward(-0.3f);  // penalizar por ficar preso
            EndEpisode();
        }

        // --- FIX 8: Timeout global do episódio ---
        if (episodeTime >= maxEpisodeTime)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = 0;
        discreteActions[1] = 0;

        float horizontal = Input.GetAxisRaw("Horizontal");

        if (horizontal < -0.1f)
            discreteActions[0] = 1;
        else if (horizontal > 0.1f)
            discreteActions[0] = 2;

        if (Input.GetKey(KeyCode.Space))
            discreteActions[1] = 1;
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

    private bool IsGroundAhead()
    {
        float direction = GetForwardDirection();

        Vector2 origin = (Vector2)transform.position + new Vector2(direction * frontOffset, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, downRayDistance, groundLayer);

        return hit.collider != null;
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        float direction = 1f;
        if (goal != null)
            direction = goal.position.x >= transform.position.x ? 1f : -1f;

        Vector2 groundAheadOrigin = (Vector2)transform.position + new Vector2(direction * frontOffset, 0f);
        Vector2 wallAheadOrigin = (Vector2)transform.position + new Vector2(0f, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundAheadOrigin, groundAheadOrigin + Vector2.down * downRayDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(wallAheadOrigin, wallAheadOrigin + new Vector2(direction, 0f) * forwardRayDistance);
    }
}