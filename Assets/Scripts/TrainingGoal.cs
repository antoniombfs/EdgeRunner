using UnityEngine;

public class TrainingGoal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        EdgeRunnerAgent agent = other.GetComponent<EdgeRunnerAgent>();
        if (agent != null)
        {
            agent.GoalReached();
        }
    }
}