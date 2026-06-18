using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DemoGoalTrigger : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private DemoHUD hud;
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private bool notifyAgent = true;

    private bool completed;

    public void SetHud(DemoHUD newHud)
    {
        hud = newHud;
    }

    public void SetScoreManager(EdgeRunnerScoreManager newScoreManager)
    {
        scoreManager = newScoreManager;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed)
        {
            return;
        }

        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null && !other.CompareTag("Player"))
        {
            return;
        }

        completed = true;

        if (hud != null)
        {
            hud.ShowLevelComplete();
        }

        if (scoreManager != null)
        {
            scoreManager.RegisterGoalReached();
        }

        if (notifyAgent && agent != null)
        {
            agent.GoalReached();
        }
    }

    public void ResetForNewRun()
    {
        completed = false;

        if (hud != null)
        {
            hud.HideStatus();
        }
    }
}
