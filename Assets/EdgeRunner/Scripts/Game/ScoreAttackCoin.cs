using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreAttackCoin : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private ScoreAttackManager manager;
    [SerializeField] private bool disableOnCollect = true;
    [SerializeField] private bool enableTriggerStayFallback = false;
    [SerializeField] private bool debugCoinCollection = false;

    private Collider2D ownCollider;
    private SpriteRenderer[] spriteRenderers;
    private bool collected;
    private int lastCollectionAttemptFrame = -1;

    public Vector3 InitialPosition { get; private set; }
    public bool IsCollected => collected;
    public bool IsAvailable => gameObject.activeInHierarchy && !collected && ownCollider != null && ownCollider.enabled;

    public void SetManager(ScoreAttackManager newManager)
    {
        manager = newManager;

        if (manager != null)
        {
            manager.RegisterCoin(this);
        }
    }

    private void Awake()
    {
        InitialPosition = transform.position;
        ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<ScoreAttackManager>();
        }

        if (manager != null)
        {
            manager.RegisterCoin(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other, "OnTriggerEnter2D");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (enableTriggerStayFallback)
        {
            TryCollect(other, "OnTriggerStay2D");
        }
    }

    private void TryCollect(Collider2D other, string contactSource)
    {
        if (collected ||
            (enableTriggerStayFallback && lastCollectionAttemptFrame == Time.frameCount))
        {
            return;
        }

        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            return;
        }

        if (enableTriggerStayFallback)
        {
            lastCollectionAttemptFrame = Time.frameCount;
        }
        EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent =
            agent as EdgeRunnerAgentV5ScoreMaxObjectAware;
        bool accepted = objectAwareAgent == null ||
            objectAwareAgent.TryAcceptScoreAttackCoinCollection(this);
        if (!accepted)
        {
            LogCollectionAttempt(objectAwareAgent, contactSource, false);
            return;
        }

        collected = true;

        if (manager != null)
        {
            manager.CollectCoin(this, agent);
        }

        LogCollectionAttempt(objectAwareAgent, contactSource, true);

        if (disableOnCollect)
        {
            SetVisualsAndCollider(false);
        }
    }

    private void LogCollectionAttempt(
        EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent,
        string contactSource,
        bool accepted)
    {
        if (!debugCoinCollection)
        {
            return;
        }

        string coinType = name.Contains("LowCoin")
            ? "low"
            : name.Contains("HighCoin")
                ? "high"
                : "unknown";
        bool isNextObjective = accepted;
        bool grounded = objectAwareAgent != null &&
            objectAwareAgent.IsCurrentlyGroundedForEvaluation();
        bool groundedByProbe = false;
        string reason = accepted ? "accepted" : "rejected_by_agent";
        string currentObjective = "unknown";
        string objectAwarePhase = "none";
        if (objectAwareAgent != null)
        {
            objectAwareAgent.GetLastCoinCollectionDecision(
                out isNextObjective,
                out grounded,
                out groundedByProbe,
                out reason,
                out currentObjective,
                out objectAwarePhase);
        }

        int collectedCount = manager != null ? manager.CoinsCollected : -1;
        Vector3 agentPosition = objectAwareAgent != null
            ? objectAwareAgent.transform.position
            : Vector3.zero;
        float distanceToCoin = objectAwareAgent != null
            ? Vector2.Distance(agentPosition, transform.position)
            : -1f;
        Vector2 velocity = objectAwareAgent != null
            ? objectAwareAgent.GetCurrentVelocityForEvaluation()
            : Vector2.zero;
        Debug.Log(
            $"[COIN COLLECTION] source={contactSource} coinName={name} " +
            $"coinType={coinType} coinPosition={transform.position} " +
            $"agentPosition={agentPosition} distanceToCoin={distanceToCoin:F3} " +
            $"grounded={grounded} groundedByProbe={groundedByProbe} " +
            $"verticalVelocity={velocity.y:F3} horizontalVelocity={velocity.x:F3} " +
            $"isNextObjective={isNextObjective} currentObjectiveName={currentObjective} " +
            $"accepted={accepted} rejectionReason={reason} " +
            $"objectAwarePhase={objectAwarePhase} " +
            $"fallbackEnabled={enableTriggerStayFallback} overlapConfirmed=true " +
            $"collectedCount={collectedCount}",
            this);
    }

    public void ResetForNewRun()
    {
        ResetForNewEpisode(true, InitialPosition);
    }

    public void ResetForNewEpisode(bool active, Vector3 position)
    {
        transform.position = position;
        collected = false;
        lastCollectionAttemptFrame = -1;
        gameObject.SetActive(active);
        SetVisualsAndCollider(active);
    }

    private void SetVisualsAndCollider(bool active)
    {
        if (ownCollider != null)
        {
            ownCollider.enabled = active;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = active;
            }
        }
    }
}
