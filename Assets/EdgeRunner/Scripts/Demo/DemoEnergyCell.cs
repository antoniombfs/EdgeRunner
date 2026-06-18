using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DemoEnergyCell : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private DemoEnergyCellCounter counter;
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private bool disableOnCollect = true;
    [SerializeField] private int pointOverride = -1;

    private bool collected;

    public void SetCounter(DemoEnergyCellCounter newCounter)
    {
        counter = newCounter;

        if (counter != null)
        {
            counter.RegisterCell(this);
        }
    }

    public void SetScoreManager(EdgeRunnerScoreManager newScoreManager)
    {
        scoreManager = newScoreManager;

        if (scoreManager != null)
        {
            scoreManager.RegisterEnergyCell(this);
        }
    }

    private void Reset()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void Start()
    {
        if (counter == null)
        {
            counter = FindAnyObjectByType<DemoEnergyCellCounter>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }

        if (counter != null)
        {
            counter.RegisterCell(this);
        }

        if (scoreManager != null)
        {
            scoreManager.RegisterEnergyCell(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !IsPlayer(other))
        {
            return;
        }

        collected = true;

        if (counter != null)
        {
            counter.CollectCell(this);
        }

        if (scoreManager != null)
        {
            int points = pointOverride >= 0 ? pointOverride : scoreManager.EnergyCellPoints;
            scoreManager.AddEnergyCell(points);
        }

        if (disableOnCollect)
        {
            gameObject.SetActive(false);
        }
    }

    public void ResetForNewRun()
    {
        collected = false;

        if (disableOnCollect && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.GetComponentInParent<EdgeRunnerAgentV5>() != null ||
               other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>() != null ||
               other.CompareTag("Player");
    }
}
