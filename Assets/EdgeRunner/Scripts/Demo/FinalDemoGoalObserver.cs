using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FinalDemoGoalObserver : MonoBehaviour
{
    [SerializeField] private FinalDemoController controller;
    [SerializeField] private ScoreAttackManager scoreAttackManager;
    private int lastNotificationFrame = -1;

    public void Configure(FinalDemoController newController, ScoreAttackManager newScoreAttackManager)
    {
        controller = newController;
        scoreAttackManager = newScoreAttackManager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (lastNotificationFrame == Time.frameCount ||
            other.GetComponentInParent<EdgeRunnerAgentV5>() == null)
        {
            return;
        }

        if (scoreAttackManager == null)
        {
            scoreAttackManager = FindAnyObjectByType<ScoreAttackManager>();
        }
        if (scoreAttackManager != null && !scoreAttackManager.ObjectivesComplete)
        {
            return;
        }

        lastNotificationFrame = Time.frameCount;
        if (controller == null)
        {
            controller = FindAnyObjectByType<FinalDemoController>();
        }

        controller?.NotifyGoalReached();
    }
}
