using UnityEngine;

public class TrainingDeathZone : MonoBehaviour
{
    private void Awake()
    {
        DeathZone gameplayDeathZone = GetComponent<DeathZone>();

        if (gameplayDeathZone != null)
        {
            gameplayDeathZone.enabled = false;
            Debug.Log("TrainingDeathZone disabled gameplay DeathZone scene reload on this object.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var agentV5 = other.GetComponentInParent<EdgeRunnerAgentV5>();
        if (agentV5 != null)
        {
            agentV5.FellOffMap();
            return;
        }

        var agentV3 = other.GetComponentInParent<EdgeRunnerAgentV3>();
        if (agentV3 != null)
        {
            agentV3.FellOffMap();
            return;
        }

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
