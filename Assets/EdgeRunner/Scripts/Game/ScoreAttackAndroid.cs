using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreAttackAndroid : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private ScoreAttackManager manager;
    [SerializeField] private float stompHeightOffset = 0.15f;
    [SerializeField] private float stompTopTolerance = 0.35f;
    [SerializeField] private float maxUpwardVelocityForStomp = 1.0f;
    [SerializeField] private float bounceForce = 8.0f;
    [SerializeField] private Color deadColor = new Color(0.25f, 0.28f, 0.32f, 1f);
    [SerializeField] private Vector3 deadScaleMultiplier = new Vector3(1.1f, 0.35f, 1f);
    [SerializeField] private bool debugLogs = false;

    private Collider2D ownCollider;
    private Rigidbody2D ownBody;
    private DemoAndroidPatrol patrol;
    private SpriteRenderer[] spriteRenderers;
    private Color[] initialColors;
    private Vector3 initialScale;
    private bool alive = true;
    private bool defeated;

    public Vector3 InitialPosition { get; private set; }
    public bool IsAlive => alive;

    public void SetManager(ScoreAttackManager newManager)
    {
        manager = newManager;

        if (manager != null)
        {
            manager.RegisterEnemy(this);
        }
    }

    private void Awake()
    {
        InitialPosition = transform.position;
        initialScale = transform.localScale;
        ownCollider = GetComponent<Collider2D>();

        ownBody = GetComponent<Rigidbody2D>();
        if (ownBody != null)
        {
            ownBody.bodyType = RigidbodyType2D.Kinematic;
            ownBody.gravityScale = 0f;
            ownBody.freezeRotation = true;
        }

        patrol = GetComponent<DemoAndroidPatrol>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            initialColors[i] = spriteRenderers[i].color;
        }

        DisableDemoHazards();
        ConfigureScoreAttackColliders(true);
    }

    private void Start()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<ScoreAttackManager>();
        }

        if (manager != null)
        {
            manager.RegisterEnemy(this);
        }
    }

    private void OnEnable()
    {
        DisableDemoHazards();
        ConfigureScoreAttackColliders(alive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleContact(other);
    }

    public void ResetForNewRun()
    {
        ResetForNewEpisode(true, InitialPosition);
    }

    public void ResetForNewEpisode(bool active, Vector3 position)
    {
        gameObject.SetActive(active);
        transform.position = position;
        transform.localScale = initialScale;
        alive = active;
        defeated = !active;

        if (patrol != null)
        {
            patrol.ResetPatrolToInitial();
            patrol.enabled = active;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = active;
                spriteRenderers[i].color = initialColors[i];
            }
        }

        DisableDemoHazards();
        ConfigureScoreAttackColliders(active);
    }

    private void TryHandleContact(Collider2D other)
    {
        if (!alive || defeated || other == null)
        {
            return;
        }

        EdgeRunnerAgentV5 agent = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agent == null)
        {
            return;
        }

        Rigidbody2D playerBody = agent.GetComponent<Rigidbody2D>();
        Bounds playerBounds = GetPlayerBounds(agent, other);

        if (IsValidStomp(playerBounds, playerBody))
        {
            KillByStomp(agent, playerBody);
            return;
        }

        if (IsPotentialTopContact(playerBounds, playerBody))
        {
            return;
        }

        if (manager != null)
        {
            manager.HandleEnemySideHit(this, agent);
        }
    }

    private Bounds GetPlayerBounds(EdgeRunnerAgentV5 agent, Collider2D fallbackCollider)
    {
        Bounds combinedBounds = fallbackCollider.bounds;
        Collider2D[] playerColliders = agent.GetComponentsInChildren<Collider2D>();
        bool hasBounds = false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];

            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = playerCollider.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(playerCollider.bounds);
            }
        }

        return combinedBounds;
    }

    private bool IsValidStomp(Bounds playerBounds, Rigidbody2D playerBody)
    {
        if (playerBody == null || ownCollider == null)
        {
            return false;
        }

        Bounds enemyBounds = ownCollider.bounds;
        float playerFeetY = playerBounds.min.y;
        float enemyTopY = enemyBounds.max.y;
        bool playerAboveCenter = playerBounds.center.y > enemyBounds.center.y + stompHeightOffset;
        bool feetNearTop = playerFeetY >= enemyTopY - stompTopTolerance;
        bool fallingOrNearlyFalling = playerBody.linearVelocity.y <= maxUpwardVelocityForStomp;

        return (playerAboveCenter || feetNearTop) && fallingOrNearlyFalling;
    }

    private bool IsPotentialTopContact(Bounds playerBounds, Rigidbody2D playerBody)
    {
        if (playerBody == null || ownCollider == null)
        {
            return false;
        }

        Bounds enemyBounds = ownCollider.bounds;
        bool playerAboveCenter = playerBounds.center.y > enemyBounds.center.y + stompHeightOffset;
        bool feetNearTop = playerBounds.min.y >= enemyBounds.max.y - stompTopTolerance;
        bool fallingOrNearlyFalling = playerBody.linearVelocity.y <= maxUpwardVelocityForStomp;

        return (playerAboveCenter || feetNearTop) && fallingOrNearlyFalling;
    }

    private void KillByStomp(EdgeRunnerAgentV5 agent, Rigidbody2D playerBody)
    {
        // In Manual mode, skip the agent's ordered-curriculum accept/reject check entirely.
        // TryAcceptScoreAttackAndroidStomp only accepts a stomp when this Android is exactly
        // the model's "expected next objective" in trained order — a free-form human player
        // has no reason to approach Androids/coins in that order. IsValidStomp (checked by the
        // caller before KillByStomp is invoked) already confirmed this is a real, valid stomp.
        if (!FinalDemoController.IsManualControlActive &&
            agent is EdgeRunnerAgentV5ScoreMaxObjectAware objectAwareAgent &&
            !objectAwareAgent.TryAcceptScoreAttackAndroidStomp(this))
        {
            return;
        }

        alive = false;
        defeated = true;
        ConfigureScoreAttackColliders(false);

        if (patrol != null)
        {
            patrol.enabled = false;
        }

        transform.localScale = new Vector3(
            initialScale.x * deadScaleMultiplier.x,
            initialScale.y * deadScaleMultiplier.y,
            initialScale.z * deadScaleMultiplier.z);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = deadColor;
            }
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, bounceForce);
        }

        if (manager != null)
        {
            manager.KillEnemy(this, agent);
        }

        if (debugLogs)
        {
            Debug.Log("[SCORE ATTACK] Android stomped.", this);
        }
    }

    private void ConfigureScoreAttackColliders(bool active)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null)
            {
                continue;
            }

            if (collider == ownCollider)
            {
                collider.enabled = active;
                collider.isTrigger = true;
            }
            else
            {
                collider.enabled = false;
            }
        }
    }

    private void DisableDemoHazards()
    {
        DemoEnemyHazard[] hazards = GetComponentsInChildren<DemoEnemyHazard>(true);

        for (int i = 0; i < hazards.Length; i++)
        {
            hazards[i].SetAffectsAgent(false);
            hazards[i].enabled = false;
        }

        StompableAndroidEnemy[] stompEnemies = GetComponentsInChildren<StompableAndroidEnemy>(true);

        for (int i = 0; i < stompEnemies.Length; i++)
        {
            stompEnemies[i].enabled = false;
        }

        StompableAndroidStompZone[] stompZones = GetComponentsInChildren<StompableAndroidStompZone>(true);

        for (int i = 0; i < stompZones.Length; i++)
        {
            stompZones[i].enabled = false;
        }

        StompableAndroidSideHazard[] sideHazards = GetComponentsInChildren<StompableAndroidSideHazard>(true);

        for (int i = 0; i < sideHazards.Length; i++)
        {
            sideHazards[i].enabled = false;
        }

        EdgeRunnerEnemyMarker marker = GetComponent<EdgeRunnerEnemyMarker>();

        if (marker != null)
        {
            marker.SetAffectsAgent(false);
        }
    }
}
