using UnityEngine;

public class TrainingDeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgent agentV1 = other.GetComponent<EdgeRunnerAgent>();
        if (agentV1 != null)
        {
            agentV1.FellOffMap();
            return;
        }

        EdgeRunnerAgentV2 agentV2 = other.GetComponent<EdgeRunnerAgentV2>();
        if (agentV2 != null)
        {
            agentV2.FellOffMap();
        }
    }
}
