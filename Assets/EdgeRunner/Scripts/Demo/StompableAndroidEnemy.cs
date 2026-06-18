using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StompableAndroidEnemy : MonoBehaviour, IEdgeRunnerResettable
{
    [Header("Scoring")]
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private int killPoints = 50;

    [Header("Stomp")]
    [SerializeField] private float stompRequiredVerticalVelocity = -0.05f;
    [SerializeField] private float stompHeightOffset = 0.15f;
    [SerializeField] private float stompTopTolerance = 0.35f;
    [SerializeField] private float maxUpwardVelocityForStomp = 1f;
    [SerializeField] private float bounceForce = 8f;
    [SerializeField] private float stompDamageGraceTime = 0.2f;

    [Header("Damage")]
    [SerializeField] private bool harmfulOnSideContact = true;
    [SerializeField] private bool affectsAgent = true;
    [SerializeField] private float sideHitCooldown = 0.5f;

    [Header("Visual")]
    [SerializeField] private bool disableColliderWhenDead = true;
    [SerializeField] private bool disablePatrolWhenDead = true;
    [SerializeField] private Color deadColor = new Color(0.22f, 0.25f, 0.28f, 1f);
    [SerializeField] private Vector3 deadScaleMultiplier = new Vector3(1.1f, 0.35f, 1f);

    [Header("Debug")]
    [SerializeField] private bool debugStomp = false;

    private Collider2D ownCollider;
    private DemoAndroidPatrol patrol;
    private DemoEnemyHazard demoHazard;
    private EdgeRunnerEnemyMarker enemyMarker;
    private SpriteRenderer[] spriteRenderers;
    private Color[] initialColors;
    private Collider2D[] stompZoneColliders;
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private bool alive = true;
    private float nextAllowedSideHitTime;
    private float ignoreBodyDamageUntil;

    public bool IsAlive => alive;
    public bool AffectsAgent => affectsAgent;

    public void SetScoreManager(EdgeRunnerScoreManager newScoreManager)
    {
        scoreManager = newScoreManager;

        if (scoreManager != null)
        {
            scoreManager.RegisterEnemy(this);
        }
    }

    public void SetAffectsAgent(bool value)
    {
        affectsAgent = value;

        if (enemyMarker != null)
        {
            enemyMarker.SetAffectsAgent(false);
        }
    }

    private void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = true;

        patrol = GetComponent<DemoAndroidPatrol>();
        demoHazard = GetComponent<DemoEnemyHazard>();
        enemyMarker = GetComponent<EdgeRunnerEnemyMarker>();

        if (enemyMarker == null)
        {
            enemyMarker = gameObject.AddComponent<EdgeRunnerEnemyMarker>();
        }

        enemyMarker.SetAffectsAgent(false);

        if (demoHazard != null)
        {
            demoHazard.enabled = false;
            demoHazard.SetAffectsAgent(false);
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialColors = new Color[spriteRenderers.Length];
        RefreshStompZones();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            initialColors[i] = spriteRenderers[i].color;
        }

        initialScale = transform.localScale;
        initialPosition = transform.position;
    }

    private void Start()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }

        if (scoreManager != null)
        {
            scoreManager.RegisterEnemy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleBodyContact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleBodyContact(collision.collider);
    }

    public void ResetForNewRun()
    {
        alive = true;
        nextAllowedSideHitTime = 0f;
        ignoreBodyDamageUntil = 0f;
        transform.localScale = initialScale;
        transform.position = initialPosition;

        if (ownCollider != null)
        {
            ownCollider.enabled = true;
            ownCollider.isTrigger = true;
        }

        if (patrol != null)
        {
            patrol.ResetPatrolToInitial();
            patrol.enabled = true;
        }

        SetStompZonesEnabled(true);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = true;
                spriteRenderers[i].color = initialColors[i];
            }
        }
    }

    public bool TryStomp(GameObject player)
    {
        Collider2D playerCollider = player != null ? player.GetComponentInChildren<Collider2D>() : null;
        return TryStomp(playerCollider);
    }

    public bool TryStomp(Collider2D playerCollider)
    {
        return TryStompInternal(playerCollider, true);
    }

    public void HandleBodyContact(GameObject player)
    {
        Collider2D playerCollider = player != null ? player.GetComponentInChildren<Collider2D>() : null;
        HandleBodyContact(playerCollider);
    }

    public void HandleBodyContact(Collider2D other)
    {
        if (!alive)
        {
            LogStomp("BODY CONTACT ignored: android dead");
            return;
        }

        if (other == null || !IsPlayer(other))
        {
            return;
        }

        LogStomp("BODY CONTACT");

        if (Time.time < ignoreBodyDamageUntil)
        {
            LogStomp("BODY CONTACT ignored: stomp grace active");
            return;
        }

        if (TryStompFromBody(other))
        {
            LogStomp("BODY CONTACT CONVERTED TO STOMP");
            return;
        }

        LogStomp("SIDE DAMAGE");
        HandleSideContact(other);
    }

    private bool TryStompFromBody(Collider2D playerCollider)
    {
        return TryStompInternal(playerCollider, false);
    }

    private bool TryStompInternal(Collider2D playerCollider, bool fromStompZone)
    {
        if (fromStompZone)
        {
            LogStomp("STOMP ZONE ENTER");
        }

        if (!alive)
        {
            LogStomp("STOMP REFUSED: android dead");
            return false;
        }

        if (playerCollider == null || !IsPlayer(playerCollider))
        {
            return false;
        }

        Rigidbody2D playerBody = playerCollider.GetComponentInParent<Rigidbody2D>();

        if (!IsValidStomp(playerCollider, playerBody, out string rejectReason))
        {
            LogStomp($"STOMP REFUSED: {rejectReason}");
            return false;
        }

        LogStomp("VALID STOMP");
        KillByStomp(playerBody);
        return true;
    }

    private bool IsValidStomp(Collider2D playerCollider, Rigidbody2D playerBody, out string rejectReason)
    {
        if (!alive)
        {
            rejectReason = "android dead";
            return false;
        }

        if (playerCollider == null)
        {
            rejectReason = "missing player collider";
            return false;
        }

        if (playerBody == null)
        {
            rejectReason = "missing rigidbody";
            return false;
        }

        float playerVelocityY = playerBody.linearVelocity.y;
        float allowedUpwardVelocity = Mathf.Max(maxUpwardVelocityForStomp, stompRequiredVerticalVelocity);

        if (playerVelocityY > allowedUpwardVelocity)
        {
            rejectReason = "velocity too high upward";
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        float playerFeetY = playerBounds.min.y;
        float playerCenterY = playerBounds.center.y;
        float enemyTopY = ownCollider != null ? ownCollider.bounds.max.y : transform.position.y;
        float enemyCenterY = ownCollider != null ? ownCollider.bounds.center.y : transform.position.y;

        bool playerCenterAbove = playerCenterY > enemyCenterY + stompHeightOffset;
        bool playerFeetNearTop = playerFeetY > enemyTopY - stompTopTolerance;

        if (!playerCenterAbove && !playerFeetNearTop)
        {
            rejectReason = "player not above";
            return false;
        }

        rejectReason = string.Empty;
        return true;
    }

    private void KillByStomp(Rigidbody2D playerBody)
    {
        alive = false;
        ignoreBodyDamageUntil = Time.time + stompDamageGraceTime;

        if (scoreManager != null)
        {
            int points = killPoints >= 0 ? killPoints : scoreManager.AndroidKillPoints;
            scoreManager.AddEnemyKill(points);
        }

        if (playerBody != null && bounceForce > 0f)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, bounceForce);
        }

        if (disablePatrolWhenDead && patrol != null)
        {
            patrol.enabled = false;
        }

        if (disableColliderWhenDead && ownCollider != null)
        {
            ownCollider.enabled = false;
        }

        SetStompZonesEnabled(false);

        transform.localScale = new Vector3(
            initialScale.x * deadScaleMultiplier.x,
            initialScale.y * deadScaleMultiplier.y,
            initialScale.z * deadScaleMultiplier.z
        );

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = deadColor;
            }
        }
    }

    private void HandleSideContact(Collider2D other)
    {
        if (!harmfulOnSideContact || !affectsAgent || Time.time < nextAllowedSideHitTime)
        {
            return;
        }

        nextAllowedSideHitTime = Time.time + sideHitCooldown;

        DemoPlayerDamageHandler damageHandler = other.GetComponentInParent<DemoPlayerDamageHandler>();

        if (damageHandler != null && damageHandler.TakeDamage(this))
        {
            return;
        }

        EdgeRunnerAgentV5EnemyAware enemyAwareAgent = other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>();

        if (enemyAwareAgent != null)
        {
            enemyAwareAgent.EnemyHit(this);
            return;
        }

        EdgeRunnerAgentV5EnemiesTransfer transferAgent = other.GetComponentInParent<EdgeRunnerAgentV5EnemiesTransfer>();

        if (transferAgent != null)
        {
            transferAgent.FellOffMap();
            return;
        }

        EdgeRunnerAgentV5 agentV5 = other.GetComponentInParent<EdgeRunnerAgentV5>();

        if (agentV5 != null)
        {
            agentV5.FellOffMap();
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.GetComponentInParent<EdgeRunnerAgentV5>() != null ||
               other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>() != null ||
               other.GetComponentInParent<EdgeRunnerAgentV5EnemiesTransfer>() != null ||
               other.GetComponentInParent<DemoPlayerDamageHandler>() != null ||
               other.CompareTag("Player");
    }

    private void RefreshStompZones()
    {
        StompableAndroidStompZone[] zones = GetComponentsInChildren<StompableAndroidStompZone>(true);
        stompZoneColliders = new Collider2D[zones.Length];

        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null)
            {
                continue;
            }

            zones[i].Configure(this);
            stompZoneColliders[i] = zones[i].GetComponent<Collider2D>();
        }
    }

    private void SetStompZonesEnabled(bool value)
    {
        if (stompZoneColliders == null || stompZoneColliders.Length == 0)
        {
            RefreshStompZones();
        }

        if (stompZoneColliders == null)
        {
            return;
        }

        for (int i = 0; i < stompZoneColliders.Length; i++)
        {
            if (stompZoneColliders[i] != null)
            {
                stompZoneColliders[i].enabled = value;
                stompZoneColliders[i].isTrigger = true;
            }
        }
    }

    private void OnValidate()
    {
        bounceForce = Mathf.Max(0f, bounceForce);
        stompHeightOffset = Mathf.Max(0f, stompHeightOffset);
        stompTopTolerance = Mathf.Max(0f, stompTopTolerance);
        maxUpwardVelocityForStomp = Mathf.Max(0f, maxUpwardVelocityForStomp);
        sideHitCooldown = Mathf.Max(0f, sideHitCooldown);
        stompDamageGraceTime = Mathf.Max(0f, stompDamageGraceTime);
    }

    private void LogStomp(string message)
    {
        if (debugStomp)
        {
            Debug.Log($"StompableAndroidEnemy '{name}': {message}", this);
        }
    }
}
