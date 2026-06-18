using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DemoAndroidPatrol : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float patrolDistance = 3.5f;
    [SerializeField] private bool moveRightOnStart = true;
    [SerializeField] private SpriteRenderer spriteRendererToFlip;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 initialPosition;
    private int direction;
    private bool capturedInitialPosition;

    public void Configure(float newSpeed, float newPatrolDistance)
    {
        speed = newSpeed;
        patrolDistance = newPatrolDistance;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (spriteRendererToFlip == null)
        {
            spriteRendererToFlip = GetComponentInChildren<SpriteRenderer>();
        }

        CaptureInitialPosition();
    }

    private void OnEnable()
    {
        CaptureInitialPosition();
        startPosition = transform.position;
        direction = moveRightOnStart ? 1 : -1;
    }

    private void FixedUpdate()
    {
        float halfDistance = Mathf.Max(0.1f, patrolDistance) * 0.5f;
        float left = startPosition.x - halfDistance;
        float right = startPosition.x + halfDistance;

        Vector2 nextPosition = rb.position + Vector2.right * direction * speed * Time.fixedDeltaTime;

        if (nextPosition.x >= right)
        {
            nextPosition.x = right;
            direction = -1;
        }
        else if (nextPosition.x <= left)
        {
            nextPosition.x = left;
            direction = 1;
        }

        rb.MovePosition(nextPosition);

        if (spriteRendererToFlip != null)
        {
            spriteRendererToFlip.flipX = direction < 0;
        }
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        patrolDistance = Mathf.Max(0.1f, patrolDistance);
    }

    public void ResetForNewRun()
    {
        ResetPatrolToInitial();
    }

    public void ResetPatrolToInitial()
    {
        CaptureInitialPosition();
        startPosition = initialPosition;
        direction = moveRightOnStart ? 1 : -1;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.position = initialPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = new Vector3(initialPosition.x, initialPosition.y, transform.position.z);
    }

    private void CaptureInitialPosition()
    {
        if (capturedInitialPosition)
        {
            return;
        }

        initialPosition = transform.position;
        capturedInitialPosition = true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)startPosition : transform.position;
        float halfDistance = Mathf.Max(0.1f, patrolDistance) * 0.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            center + Vector3.left * halfDistance,
            center + Vector3.right * halfDistance
        );
    }
}
