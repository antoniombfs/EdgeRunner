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
        string reason = accepted ? "accepted" : "rejected_by_agent";
        string currentObjective = "unknown";
        if (objectAwareAgent != null)
        {
            objectAwareAgent.GetLastCoinCollectionDecision(
                out isNextObjective,
                out grounded,
                out reason,
                out currentObjective);
        }

        int collectedCount = manager != null ? manager.CoinsCollected : -1;
        Debug.Log(
            $"[COIN COLLECTION] source={contactSource} coinName={name} " +
            $"coinType={coinType} isNextObjective={isNextObjective} " +
            $"grounded={grounded} accepted={accepted} reason={reason} " +
            $"currentObjective={currentObjective} collectedCount={collectedCount}",
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
