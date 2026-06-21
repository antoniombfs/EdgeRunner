using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EdgeRunnerEnemyMarker : MonoBehaviour
{
    [Header("Enemy State")]
    [SerializeField] private bool affectsAgent = true;
    [SerializeField] private bool isActiveEnemy = true;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private bool isDangerous = true;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Collider2D enemyCollider;

    [Header("Contact")]
    [SerializeField] private float hitCooldown = 0.5f;

    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private Vector2 measuredVelocity;
    private float nextAllowedHitTime;

    public bool AffectsAgent => affectsAgent;
    public bool IsActiveEnemy => isActiveEnemy;
    public bool IsAlive => isAlive;
    public bool IsDangerous => isDangerous;
    public bool IsObservable => affectsAgent && isActiveEnemy && isAlive && gameObject.activeInHierarchy;
    public Transform ObservationTransform => visualRoot != null ? visualRoot : transform;
    public Collider2D EnemyCollider => enemyCollider;
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

    public void SetAlive(bool value)
    {
        isAlive = value;
    }

    public Vector2 GetObservationPosition()
    {
        if (enemyCollider != null)
        {
            return enemyCollider.bounds.center;
        }

        return (Vector2)ObservationTransform.position;
    }

    public Bounds GetObservationBounds()
    {
        if (enemyCollider != null)
        {
            return enemyCollider.bounds;
        }

        return new Bounds(GetObservationPosition(), Vector3.zero);
    }

    private void Reset()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();

        if (ownCollider != null)
        {
            enemyCollider = ownCollider;
            ownCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ResolveCollider();

        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;
        }
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
        if (!affectsAgent || !isActiveEnemy || !isAlive || !isDangerous || Time.time < nextAllowedHitTime)
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
        ResolveCollider();
    }

    private void ResolveCollider()
    {
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider2D>();
        }
    }
}
