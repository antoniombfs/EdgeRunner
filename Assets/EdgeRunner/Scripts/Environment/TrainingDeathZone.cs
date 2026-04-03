using UnityEngine;

public class TrainingDeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var agentV2 = other.GetComponentInParent<EdgeRunnerAgentV2>();
        if (agentV2 != null)
        {
            agentV2.FellOffMap();
            return;
        }

        var agentV1 = other.GetComponentInParent<EdgeRunnerAgent>();
        if (agentV1 != null)
        {
            agentV1.FellOffMap();
        }
    }
}