using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SpeedRunObstacleHazard : MonoBehaviour
{
    [SerializeField] private bool affectsAgent = true;

    public bool AffectsAgent => affectsAgent;

    public void SetAffectsAgent(bool value)
    {
        affectsAgent = value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleContact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
        {
            TryHandleContact(collision.collider);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision != null)
        {
            TryHandleContact(collision.collider);
        }
    }

    private void TryHandleContact(Collider2D other)
    {
        if (!affectsAgent || other == null)
        {
            return;
        }

        EdgeRunnerAgentV5SpeedRunObstacleAware agent =
            other.GetComponentInParent<EdgeRunnerAgentV5SpeedRunObstacleAware>();

        if (agent != null)
        {
            agent.NotifyObstacleCollision(this);
        }
    }
}
