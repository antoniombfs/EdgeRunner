using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryResetRunIfAvailable(other))
        {
            return;
        }

        EdgeRunnerAgentV5 agentV5 = other.GetComponentInParent<EdgeRunnerAgentV5>();
        if (agentV5 != null)
        {
            Debug.LogWarning("DeathZone.cs intercepted EdgeRunnerAgentV5. Calling FellOffMap instead of reloading scene.");
            agentV5.FellOffMap();
            return;
        }

        EdgeRunnerAgentV3 agentV3 = other.GetComponentInParent<EdgeRunnerAgentV3>();
        if (agentV3 != null)
        {
            Debug.LogWarning("DeathZone.cs intercepted EdgeRunnerAgentV3. Calling FellOffMap instead of reloading scene.");
            agentV3.FellOffMap();
            return;
        }

        EdgeRunnerAgentV2 agentV2 = other.GetComponentInParent<EdgeRunnerAgentV2>();
        if (agentV2 != null)
        {
            Debug.LogWarning("DeathZone.cs intercepted EdgeRunnerAgentV2. Calling FellOffMap instead of reloading scene.");
            agentV2.FellOffMap();
            return;
        }

        EdgeRunnerAgent agentV1 = other.GetComponentInParent<EdgeRunnerAgent>();
        if (agentV1 != null)
        {
            Debug.LogWarning("DeathZone.cs intercepted EdgeRunnerAgent. Calling FellOffMap instead of reloading scene.");
            agentV1.FellOffMap();
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.LogWarning("DeathZone.cs found Player without an EdgeRunner agent or run reset manager. Scene reload skipped.");
        }
    }

    public static bool TryResetRunIfAvailable(Collider2D other)
    {
        if (!IsPlayerLikeCollider(other))
        {
            return false;
        }

        EdgeRunnerRunResetManager resetManager = FindAnyObjectByType<EdgeRunnerRunResetManager>();

        if (resetManager == null)
        {
            return false;
        }

        resetManager.ResetRun();
        return true;
    }

    private static bool IsPlayerLikeCollider(Collider2D other)
    {
        return other != null &&
               (other.GetComponentInParent<EdgeRunnerAgentV5>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV5EnemiesTransfer>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV3>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV2>() != null ||
                other.GetComponentInParent<EdgeRunnerAgent>() != null ||
                other.GetComponentInParent<DemoPlayerDamageHandler>() != null ||
                other.CompareTag("Player"));
    }
}
