using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreAttackCoin : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private ScoreAttackManager manager;
    [SerializeField] private bool disableOnCollect = true;

    private Collider2D ownCollider;
    private SpriteRenderer[] spriteRenderers;
    private bool collected;

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
        if (collected)
        {
            return;
        }

        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            return;
        }

        collected = true;

        if (manager != null)
        {
            manager.CollectCoin(this, agent);
        }

        if (disableOnCollect)
        {
            SetVisualsAndCollider(false);
        }
    }

    public void ResetForNewRun()
    {
        ResetForNewEpisode(true, InitialPosition);
    }

    public void ResetForNewEpisode(bool active, Vector3 position)
    {
        transform.position = position;
        collected = false;
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
