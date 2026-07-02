using UnityEngine;

/// <summary>
/// Presentation-only patrol. It deliberately uses no Collider2D, Rigidbody2D,
/// hazard, reward, or ML-Agents component.
/// </summary>
public sealed class FinalDemoVisualPatrol : MonoBehaviour
{
    [SerializeField] private float patrolMinX;
    [SerializeField] private float patrolMaxX;
    [SerializeField] private float speed = 0.7f;
    [SerializeField] private bool patrolEnabled = true;

    private float startTime;
    private float fixedY;

    public float PatrolMinX => patrolMinX;
    public float PatrolMaxX => patrolMaxX;
    public bool PatrolEnabled => patrolEnabled;

    public void Configure(float minX, float maxX, float newSpeed, bool enabledByDefault)
    {
        patrolMinX = Mathf.Min(minX, maxX);
        patrolMaxX = Mathf.Max(minX, maxX);
        speed = Mathf.Max(0f, newSpeed);
        patrolEnabled = enabledByDefault;
    }

    private void OnEnable()
    {
        fixedY = transform.position.y;
        startTime = Time.time;
    }

    private void Update()
    {
        if (!patrolEnabled || patrolMaxX - patrolMinX < 0.1f)
        {
            return;
        }

        float distance = patrolMaxX - patrolMinX;
        float travelled = (Time.time - startTime) * speed;
        float x = patrolMinX + Mathf.PingPong(travelled, distance);
        transform.position = new Vector3(x, fixedY, transform.position.z);
    }
}
