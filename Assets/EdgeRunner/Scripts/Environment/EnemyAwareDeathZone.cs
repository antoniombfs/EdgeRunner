using UnityEngine;

public class EnemyAwareDeathZone : MonoBehaviour
{
    private void Awake()
    {
        DeathZone gameplayDeathZone = GetComponent<DeathZone>();

        if (gameplayDeathZone != null)
        {
            gameplayDeathZone.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgentV5EnemyAware agent = other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();

        if (agent != null)
        {
            agent.FellOffMap();
        }
    }
}
