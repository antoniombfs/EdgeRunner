using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
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
            Debug.LogWarning("DeathZone.cs reloading current scene for normal gameplay Player.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
