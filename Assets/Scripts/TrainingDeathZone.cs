using UnityEngine;

public class TrainingDeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgent agent = other.GetComponent<EdgeRunnerAgent>();
        if (agent != null)
        {
            agent.FellOffMap();
        }
    }
}