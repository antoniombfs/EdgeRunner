using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EdgeRunnerEnemyMarker : MonoBehaviour
{
    [SerializeField] private bool affectsAgent = true;
    [SerializeField] private float hitCooldown = 0.5f;

    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private Vector2 measuredVelocity;
    private float nextAllowedHitTime;

    public bool AffectsAgent => affectsAgent;
    public Vector2 CurrentVelocity
    {
        get
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                return rb.linearVelocity;
            }

            return measuredVelocity;
        }
    }

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
        rb = GetComponent<Rigidbody2D>();

        Collider2D ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        measuredVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        Vector2 currentPosition = transform.position;
        float deltaTime = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        measuredVelocity = (currentPosition - previousPosition) / deltaTime;
        previousPosition = currentPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandlePlayer(collision.collider);
    }

    private void TryHandlePlayer(Collider2D other)
    {
        if (!affectsAgent || Time.time < nextAllowedHitTime)
        {
            return;
        }

        EdgeRunnerAgentV5EnemyAware enemyAwareAgent = other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();

        if (enemyAwareAgent == null)
        {
            return;
        }

        nextAllowedHitTime = Time.time + hitCooldown;
        enemyAwareAgent.EnemyHit(this);
    }

    private void OnValidate()
    {
        hitCooldown = Mathf.Max(0f, hitCooldown);
    }
}
