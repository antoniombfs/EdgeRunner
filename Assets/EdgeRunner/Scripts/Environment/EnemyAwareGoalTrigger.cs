using UnityEngine;

public class EnemyAwareGoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgentV5EnemyAware agent = other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();

        if (agent != null)
        {
            agent.GoalReached();
        }
    }
}
