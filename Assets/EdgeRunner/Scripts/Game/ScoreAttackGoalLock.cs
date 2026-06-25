using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreAttackGoalLock : MonoBehaviour
{
    [SerializeField] private ScoreAttackManager manager;

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

        agent.GoalReached();
    }
}
