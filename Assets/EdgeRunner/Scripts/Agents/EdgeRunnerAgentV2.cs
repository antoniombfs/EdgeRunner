using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// EdgeRunner Agent — versão "Mario-like"
///
/// Mudanças principais em relação à versão anterior:
///
/// 1. RAYCAST ARRAY (antecipação do terreno)
///    O agente agora "vê" o terreno à frente em 5 pontos diferentes,
///    a distâncias crescentes. Em vez de saber apenas "há chão aqui",
///    sabe "há chão a 0.5u, a 1u, a 1.5u, a 2u e a 2.5u".
///    Isto permite generalizar para qualquer layout sem precisar de
///    uma scene específica por cada situação nova.
///    Space Size: 8 → 14 (5 raios de chão + velocidade a que se move)
///
/// 2. SALTO COM MOMENTUM
///    O agente não é penalizado por saltar — é recompensado por saltar
///    ENQUANTO se move. Saltar parado é penalizado levemente.
///    Isto incentiva o comportamento natural de Mario: correr e saltar.
///
/// 3. REWARD ORIENTADA A FLUXO
///    Substituída a progressReward baseada em distância euclidiana
///    por uma baseada em velocidade horizontal real (velX).
///    O agente aprende que andar rápido para a frente é bom,
///    em vez de aprender só "estou mais perto do goal".
///
/// 4. ELIMINADAS PENALIZAÇÕES COMPORTAMENTAIS GRANULARES
///    Removido: unnecessaryJumpPenalty com multiplicador, dropJumpPenalty granular.
///    O agente aprende a não saltar desnecessariamente porque isso
///    o atrasa (velocidade = 0 no ar sem ganho de X), não porque é punido.
///
/// ATENÇÃO: Space Size deve ser atualizado para 14 no Behavior Parameters.
/// </summary>
public class EdgeRunnerAgentV2 : Agent
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

    [Header("Terrain Scan (Raycast Array)")]
    // Número de raios para a frente — cada um deteta solo, safe drop ou void
    [SerializeField] private int scanRayCount = 5;
    // Primeiro raio começa a esta distância horizontal
    [SerializeField] private float scanStartOffset = 0.5f;
    // Cada raio seguinte avança este passo
    [SerializeField] private float scanStepSize = 0.5f;
    // Até onde cada raio desce à procura de solo
    [SerializeField] private float scanDownDistance = 4.0f;
    // Acima deste valor de queda vertical, conta como safe drop
    [SerializeField] private float safeDropThreshold = 0.5f;

    [Header("Wall Detection")]
    [SerializeField] private float forwardRayDistance = 0.7f;

    [Header("Rewards")]
    // Recompensa por velocidade horizontal (fluxo Mario-like)
    [SerializeField] private float velocityRewardScale = 0.003f;
    // Recompensa extra por chegar ao goal
    [SerializeField] private float goalReward = 2.0f;
    // Penalização por morrer
    [SerializeField] private float deathPenalty = -1f;
    // Penalização passiva por step (pressão de tempo)
    [SerializeField] private float stepPenalty = -0.0003f;
    // Penalização leve por saltar completamente parado
    [SerializeField] private float idleJumpPenalty = -0.005f;
    // Penalização por ficar preso
    [SerializeField] private float stuckPenalty = -0.3f;
    // Recompensa por atingir novo melhor X (milestone)
    [SerializeField] private float milestoneReward = 0.05f;

    [Header("Episode Control")]
    [SerializeField] private float stuckTimeLimit = 4f;
    [SerializeField] private float bestXProgressThreshold = 0.3f;
    [SerializeField] private float maxEpisodeTime = 30f;

    // --- estado interno ---
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float bestXReached;
    private float timeSinceBestXProgress;
    private float episodeTime;

    // ──────────────────────────────────────────────────────────────────────
    // INICIALIZAÇÃO
    // ──────────────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        bestXReached = transform.position.x;
        timeSinceBestXProgress = 0f;
        episodeTime = 0f;
    }

    // ──────────────────────────────────────────────────────────────────────
    // OBSERVAÇÕES  (Space Size = 14)
    //
    //  0      velX normalizado
    //  1      velY normalizado
    //  2      grounded (0/1)
    //  3      toGoal.x normalizado
    //  4      toGoal.y normalizado
    //  5      wallAhead (0/1)
    //  6-10   terrainScan[0..4]  (-1 = void, 0 = safe drop, 1 = solo imediato)
    //  11-12  (reservado para futuras obs — podes ignorar por agora)
    //         na prática são velX²  e  isAirborne, úteis para momentum
    //  13     speedRatio (velX / moveSpeed, outra vez mas como obs de contexto)
    //
    //  NOTA: O array de scan é o coração desta versão.
    //  Cada elemento codifica o que o agente "vê" naquele ponto à frente:
    //    +1  → há solo logo abaixo (pode andar / aterrar)
    //     0  → há solo mais abaixo (safe drop, pode cair)
    //    -1  → não há nada (void / gap obrigatório)
    // ──────────────────────────────────────────────────────────────────────

    public override void CollectObservations(VectorSensor sensor)
    {
        float velX = rb != null ? rb.linearVelocity.x / moveSpeed : 0f;
        float velY = rb != null ? rb.linearVelocity.y / 15f : 0f;
        bool grounded = IsGrounded();
        bool wallAhead = IsWallAhead();

        Vector2 toGoal = goal != null
            ? (Vector2)(goal.position - transform.position)
            : Vector2.zero;

        // obs 0-5
        sensor.AddObservation(velX);
        sensor.AddObservation(velY);
        sensor.AddObservation(grounded ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp(toGoal.x / 20f, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toGoal.y / 10f, -1f, 1f));
        sensor.AddObservation(wallAhead ? 1f : 0f);

        // obs 6-10: terrain scan array
        float direction = GetForwardDirection();
        for (int i = 0; i < scanRayCount; i++)
        {
            float offset = scanStartOffset + i * scanStepSize;
            float scanValue = ScanTerrain(direction, offset);
            sensor.AddObservation(scanValue);
        }

        // obs 11-13: contexto de momentum
        sensor.AddObservation(velX * velX);                         // rapidez² (sempre positivo)
        sensor.AddObservation(grounded ? 0f : 1f);                  // airborne flag
        sensor.AddObservation(Mathf.Abs(velX));                     // rapidez absoluta
    }

    // ──────────────────────────────────────────────────────────────────────
    // SCAN DE TERRENO
    //
    // Retorna:
    //   +1  solo imediato (dentro de downRayDistance/2)
    //    0  safe drop (solo mais abaixo mas existe)
    //   -1  void / gap
    // ──────────────────────────────────────────────────────────────────────

    private float ScanTerrain(float direction, float forwardOffset)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(direction * forwardOffset, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, scanDownDistance, groundLayer);

        if (hit.collider == null)
            return -1f; // void

        float drop = transform.position.y - hit.point.y;

        if (drop < safeDropThreshold)
            return 1f; // solo imediato (pode andar)
        else
            return 0f; // safe drop
    }

    // ──────────────────────────────────────────────────────────────────────
    // MÁSCARA DE AÇÕES
    // Impede salto no ar (igual à versão anterior)
    // ──────────────────────────────────────────────────────────────────────

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!IsGrounded())
            actionMask.SetActionEnabled(1, 1, false);
    }

    // ──────────────────────────────────────────────────────────────────────
    // LÓGICA PRINCIPAL
    // ──────────────────────────────────────────────────────────────────────

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (rb == null) return;

        episodeTime += Time.fixedDeltaTime;

        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];

        // --- movimento horizontal ---
        float moveX = moveAction switch
        {
            1 => -1f,
            2 => 1f,
            _ => 0f
        };
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        bool grounded = IsGrounded();

        // --- salto ---
        if (jumpAction == 1 && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Penaliza saltar completamente parado (sem momentum)
            // Mario nunca salta parado sem razão
            if (Mathf.Abs(moveX) < 0.1f)
                AddReward(idleJumpPenalty);
        }

        // --- reward de velocidade (fluxo Mario-like) ---
        // Recompensa por avançar em direção ao goal a boa velocidade.
        // O agente aprende que manter momentum é bom, parar é mau.
        if (goal != null)
        {
            float forwardVel = rb.linearVelocity.x * GetForwardDirection();
            if (forwardVel > 0f)
                AddReward(forwardVel / moveSpeed * velocityRewardScale);
        }

        // --- penalização de step ---
        AddReward(stepPenalty);

        // --- stuck detector por melhor X ---
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

    // ──────────────────────────────────────────────────────────────────────
    // HEURISTIC (controlo manual para teste)
    // ──────────────────────────────────────────────────────────────────────

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        d[1] = 0;

        float h = Input.GetAxisRaw("Horizontal");
        if (h < -0.1f) d[0] = 1;
        else if (h > 0.1f) d[0] = 2;

        if (Input.GetKey(KeyCode.Space)) d[1] = 1;
    }

    // ──────────────────────────────────────────────────────────────────────
    // EVENTOS EXTERNOS
    // ──────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────
    // HELPERS DE DETEÇÃO
    // ──────────────────────────────────────────────────────────────────────

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
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
        if (goal == null) return 1f;
        return goal.position.x >= transform.position.x ? 1f : -1f;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GIZMOS (visualização no editor)
    // ──────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        float dir = goal != null
            ? (goal.position.x >= transform.position.x ? 1f : -1f)
            : 1f;

        // Wall ray
        Gizmos.color = Color.cyan;
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.1f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector2(dir, 0f) * forwardRayDistance);

        // Terrain scan rays
        for (int i = 0; i < scanRayCount; i++)
        {
            float offset = scanStartOffset + i * scanStepSize;
            Vector2 origin = (Vector2)transform.position + new Vector2(dir * offset, 0.1f);
            float scanVal = ScanTerrain(dir, offset);

            Gizmos.color = scanVal > 0.5f ? Color.yellow   // solo imediato
                         : scanVal > -0.5f ? Color.magenta   // safe drop
                         : Color.red;      // void

            Gizmos.DrawLine(origin, origin + Vector2.down * scanDownDistance);
            Gizmos.DrawSphere(origin, 0.05f);
        }
    }
}
