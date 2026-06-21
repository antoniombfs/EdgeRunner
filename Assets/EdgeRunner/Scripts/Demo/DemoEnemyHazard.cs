using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DemoEnemyHazard : MonoBehaviour
{
    [SerializeField] private bool affectsAgent = true;
    [SerializeField] private float hitCooldown = 0.5f;

    private float nextAllowedHitTime;

    public bool AffectsAgent => affectsAgent;

    public void SetAffectsAgent(bool value)
    {
        affectsAgent = value;
    }

    private void Reset()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LogHazardContact("[ENEMY HAZARD] triggered by", other);
        TryHandlePlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        LogHazardContact("[ENEMY HAZARD] collided with", collision.collider);
        TryHandlePlayer(collision.collider);
    }

    private void TryHandlePlayer(Collider2D other)
    {
        if (Time.time < nextAllowedHitTime)
        {
            return;
        }

        DemoPlayerDamageHandler damageHandler = other.GetComponentInParent<DemoPlayerDamageHandler>();

        if (damageHandler != null)
        {
            nextAllowedHitTime = Time.time + hitCooldown;
            damageHandler.TakeDamage(this);
            return;
        }

        EdgeRunnerAgentV5EnemyAware enemyAwareAgent = other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();

        if (enemyAwareAgent != null)
        {
            nextAllowedHitTime = Time.time + hitCooldown;

            if (affectsAgent)
            {
                enemyAwareAgent.EnemyHit(this);
            }
            else
            {
                Debug.LogWarning("DemoEnemyHazard touched EdgeRunnerAgentV5EnemyAware, but affectsAgent is disabled.");
            }

            return;
        }

        EdgeRunnerAgentV5EnemiesTransfer transferAgent = other.GetComponentInParent<EdgeRunnerAgentV5EnemiesTransfer>();

        if (transferAgent != null)
        {
            nextAllowedHitTime = Time.time + hitCooldown;

            if (affectsAgent)
            {
                transferAgent.FellOffMap();
            }
            else
            {
                Debug.LogWarning("DemoEnemyHazard touched EdgeRunnerAgentV5EnemiesTransfer, but affectsAgent is disabled.");
            }

            return;
        }

        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            return;
        }

        nextAllowedHitTime = Time.time + hitCooldown;

        if (affectsAgent)
        {
            agent.FellOffMap();
        }
        else
        {
            Debug.LogWarning("DemoEnemyHazard touched EdgeRunnerAgentV5, but affectsAgent is disabled.");
        }
    }

    private void OnValidate()
    {
        hitCooldown = Mathf.Max(0f, hitCooldown);
    }

    private void LogHazardContact(string prefix, Collider2D other)
    {
        Debug.LogWarning(
            $"{prefix} {DescribeCollider(other)}\n" +
            System.Environment.StackTrace,
            this
        );
    }

    private static string DescribeCollider(Collider2D other)
    {
        if (other == null)
        {
            return "null";
        }

        return $"{other.GetType().Name} on {other.gameObject.name}";
    }
}
