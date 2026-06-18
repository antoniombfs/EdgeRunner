using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgentV3 agentV3 = other.GetComponentInParent<EdgeRunnerAgentV3>();

        if (agentV3 != null)
        {
            Debug.Log("AGENT V3 CHEGOU AO GOAL");

            agentV3.GoalReached();
            return;
        }

        EdgeRunnerAgentV2 agent = other.GetComponentInParent<EdgeRunnerAgentV2>();

        if (agent != null)
        {
            Debug.Log("AGENT CHEGOU AO GOAL");

            agent.GoalReached();
        }
    }
}
