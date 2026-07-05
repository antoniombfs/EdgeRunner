using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreAttackGoalLock : MonoBehaviour
{
    [SerializeField] private ScoreAttackManager manager;
    private FinalDemoController demoController;
    private bool manualCompletionTriggered;

    private void Awake()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void Start()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<ScoreAttackManager>();
        }
    }

    public void SetManager(ScoreAttackManager newManager)
    {
        manager = newManager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleGoalContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleGoalContact(other);
    }

    private void TryHandleGoalContact(Collider2D other)
    {
        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            return;
        }

        if (manager != null && !manager.ObjectivesComplete)
        {
            manager.TryHandleGoalReached(agent);
            return;
        }

        // Manual mode does not go through EdgeRunnerAgentV5.GoalReached() -> EndEpisode() ->
        // OnEpisodeBegin(). That pipeline resets the ObjectAware curriculum's own internal
        // state, which a free-form human player does not keep consistent with what the
        // pipeline expects (the same reasoning as the Manual bypass in ScoreAttackCoin /
        // ScoreAttackAndroid). Instead, reuse FinalDemoController's own restart path directly.
        if (FinalDemoController.IsManualControlActive)
        {
            if (manualCompletionTriggered)
            {
                return;
            }
            manualCompletionTriggered = true;

            if (demoController == null)
            {
                demoController = FindAnyObjectByType<FinalDemoController>();
            }
            demoController?.CompleteLevelManual();
            return;
        }

        agent.GoalReached();
    }
}
