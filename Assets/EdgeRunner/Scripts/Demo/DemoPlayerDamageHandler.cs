using UnityEngine;

public class DemoPlayerDamageHandler : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private EdgeRunnerRunResetManager runResetManager;
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private bool resetRunOnDamage = true;
    [SerializeField] private float damageCooldown = 0.35f;
    [SerializeField] private bool debugDamageStackTraces = false;

    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasSpawn;
    private float nextAllowedDamageTime;

    public void Configure(
        EdgeRunnerRunResetManager newRunResetManager,
        EdgeRunnerScoreManager newScoreManager,
        bool shouldResetRunOnDamage)
    {
        runResetManager = newRunResetManager;
        scoreManager = newScoreManager;
        resetRunOnDamage = shouldResetRunOnDamage;
        CaptureSpawn();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CaptureSpawn();
    }

    public bool TakeDamage(Component source)
    {
        if (debugDamageStackTraces)
        {
            Debug.LogWarning(
                "[RESET SOURCE] DemoPlayerDamageHandler.TakeDamage\n" +
                $"source={DescribeSource(source)}\n" +
                System.Environment.StackTrace,
                this
            );
        }

        if (Time.time < nextAllowedDamageTime)
        {
            return true;
        }

        nextAllowedDamageTime = Time.time + damageCooldown;

        if (resetRunOnDamage)
        {
            EnsureReferences();

            if (runResetManager != null)
            {
                runResetManager.ResetRun();
                return true;
            }

            if (scoreManager != null)
            {
                scoreManager.ResetRun();
                return true;
            }
        }

        ResetForNewRun();
        return true;
    }

    public void ResetForNewRun()
    {
        CaptureSpawn();
        nextAllowedDamageTime = 0f;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation.eulerAngles.z;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
    }

    private void EnsureReferences()
    {
        if (runResetManager == null)
        {
            runResetManager = FindAnyObjectByType<EdgeRunnerRunResetManager>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }
    }

    private void CaptureSpawn()
    {
        if (hasSpawn)
        {
            return;
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasSpawn = true;
    }

    private static string DescribeSource(Component source)
    {
        if (source == null)
        {
            return "null";
        }

        return $"{source.GetType().Name} on {source.gameObject.name}";
    }

    private void OnValidate()
    {
        damageCooldown = Mathf.Max(0f, damageCooldown);
    }
}
