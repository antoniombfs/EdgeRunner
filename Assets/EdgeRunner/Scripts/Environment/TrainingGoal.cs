using UnityEngine;

public class TrainingGoal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgentV3 agentV3 = other.GetComponentInParent<EdgeRunnerAgentV3>();
        if (agentV3 != null)
        {
            agentV3.GoalReached();
            return;
        }

        EdgeRunnerAgent agentV1 = other.GetComponentInParent<EdgeRunnerAgent>();
        if (agentV1 != null)
        {
            agentV1.GoalReached();
            return;
        }

        EdgeRunnerAgentV2 agentV2 = other.GetComponentInParent<EdgeRunnerAgentV2>();
        if (agentV2 != null)
        {
            agentV2.GoalReached();
        }
    }
}
