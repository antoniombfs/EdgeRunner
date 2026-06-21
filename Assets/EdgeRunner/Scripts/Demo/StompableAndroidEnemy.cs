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
    [SerializeField] private float sidePollDamageDelay = 0.05f;

    [Header("Contact Zones")]
    [SerializeField] private Collider2D stompZoneCollider;
    [SerializeField] private Collider2D sideHazardLeftCollider;
    [SerializeField] private Collider2D sideHazardRightCollider;

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
    private Collider2D[] sideHazardColliders;
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private bool alive = true;
    private float nextAllowedSideHitTime;
    private float ignoreBodyDamageUntil;
    private float leftPollContactStartTime = -1f;
    private float rightPollContactStartTime = -1f;
    private bool hasWarnedInvalidSideSource;
    private bool warnedDemoHazardDisabled;
    private readonly Collider2D[] pollResults = new Collider2D[12];

    public bool IsAlive => alive;
    public bool AffectsAgent => affectsAgent;
    public float CurrentCenterX => GetAndroidBounds().center.x;

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

    public void SetContactColliders(Collider2D newStompZone, Collider2D newSideHazardLeft, Collider2D newSideHazardRight)
    {
        stompZoneCollider = newStompZone;
        sideHazardLeftCollider = newSideHazardLeft;
        sideHazardRightCollider = newSideHazardRight;
    }

    private void Awake()
    {
        ownCollider = GetComponent<Collider2D>();

        if (ownCollider != null)
        {
            ownCollider.isTrigger = true;
            ownCollider.enabled = false;
        }

        patrol = GetComponent<DemoAndroidPatrol>();
        DisableDemoEnemyHazardIfPresent();
        demoHazard = GetComponent<DemoEnemyHazard>();
        enemyMarker = GetComponent<EdgeRunnerEnemyMarker>();

        if (enemyMarker == null)
        {
            enemyMarker = gameObject.AddComponent<EdgeRunnerEnemyMarker>();
        }

        enemyMarker.SetAffectsAgent(false);

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialColors = new Color[spriteRenderers.Length];
        RefreshStompZones();
        RefreshSideHazards();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            initialColors[i] = spriteRenderers[i].color;
        }

        initialScale = transform.localScale;
        initialPosition = transform.position;
    }

    private void OnEnable()
    {
        DisableDemoEnemyHazardIfPresent();
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

    public void ResetForNewRun()
    {
        DisableDemoEnemyHazardIfPresent();

        alive = true;
        nextAllowedSideHitTime = 0f;
        ignoreBodyDamageUntil = 0f;
        leftPollContactStartTime = -1f;
        rightPollContactStartTime = -1f;
        hasWarnedInvalidSideSource = false;
        transform.localScale = initialScale;
        transform.position = initialPosition;

        if (ownCollider != null)
        {
            ownCollider.isTrigger = true;
            ownCollider.enabled = false;
        }

        if (patrol != null)
        {
            patrol.ResetPatrolToInitial();
            patrol.enabled = true;
        }

        SetStompZonesEnabled(true);
        SetSideHazardsEnabled(true);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = true;
                spriteRenderers[i].color = initialColors[i];
            }
        }
    }

    private void DisableDemoEnemyHazardIfPresent()
    {
        DemoEnemyHazard hazard = GetComponent<DemoEnemyHazard>();

        if (hazard == null)
        {
            demoHazard = null;
            return;
        }

        hazard.SetAffectsAgent(false);
        hazard.enabled = false;
        demoHazard = hazard;

        if (!warnedDemoHazardDisabled)
        {
            Debug.LogWarning($"[ANDROID] Disabled DemoEnemyHazard on stompable android {name}", this);
            warnedDemoHazardDisabled = true;
        }
    }

    private void FixedUpdate()
    {
        PollContactZones();
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

    private bool TryStompInternal(Collider2D playerCollider, bool fromStompZone)
    {
        LogStompDiagnostic("[STOMP] TryStomp called", playerCollider);

        if (fromStompZone)
        {
            LogStomp("[STOMP] StompZone_Top entered");
        }

        if (!alive)
        {
            LogStomp("STOMP REFUSED: android dead");
            LogStompDiagnostic("[STOMP] Failed: android dead", playerCollider);
            return false;
        }

        if (playerCollider == null || !IsPlayer(playerCollider))
        {
            LogStompDiagnostic("[STOMP] Failed: missing or non-player collider", playerCollider);
            return false;
        }

        Rigidbody2D playerBody = playerCollider.GetComponentInParent<Rigidbody2D>();

        if (!IsValidStomp(playerCollider, playerBody, out string rejectReason))
        {
            LogStomp($"STOMP REFUSED: {rejectReason}");
            LogStompDiagnostic($"[STOMP] Failed: {rejectReason}", playerCollider);
            return false;
        }

        LogStomp("[STOMP] Success");
        LogStompDiagnostic("[STOMP] Success", playerCollider);
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
        Bounds enemyBounds = GetAndroidBounds();
        float enemyTopY = enemyBounds.max.y;
        float enemyCenterY = enemyBounds.center.y;

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
        SetSideHazardsEnabled(false);

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

    public void HandleSideContact(GameObject player)
    {
        HandleSideContact(player, null);
    }

    public void HandleSideContact(GameObject player, Component source)
    {
        Collider2D playerCollider = player != null ? player.GetComponentInChildren<Collider2D>() : null;
        HandleSideContact(playerCollider, source);
    }

    public void HandleSideContact(Collider2D other)
    {
        HandleSideContact(other, null);
    }

    public void HandleSideContact(Collider2D other, Component source)
    {
        LogSideContactDiagnostic(other, source);

        if (!IsValidSideHazardSource(source))
        {
            WarnInvalidSideSource(source);
            return;
        }

        string sideLabel = GetSideHazardLabel(source);

        if (!alive)
        {
            LogStomp($"{sideLabel} ignored: android dead");
            return;
        }

        if (other == null || !IsPlayer(other))
        {
            return;
        }

        LogStomp($"[SIDE] SideHazard contact: {sideLabel}");

        if (Time.time < ignoreBodyDamageUntil)
        {
            LogStomp($"{sideLabel} ignored: stomp grace active");
            return;
        }

        if (IsPossibleStompFromSide(other))
        {
            if (TryStomp(other))
            {
                LogStomp("[SIDE HAZARD] Converted side contact to stomp");
                return;
            }

            LogStomp("[SIDE HAZARD] Ignored because possible stomp");
            return;
        }

        if (IsPlayerClearlyAbove(other))
        {
            LogStomp("[SIDE HAZARD] Ignored because possible stomp");
            return;
        }

        if (!harmfulOnSideContact || !affectsAgent || Time.time < nextAllowedSideHitTime)
        {
            return;
        }

        nextAllowedSideHitTime = Time.time + sideHitCooldown;
        LogStomp("[SIDE HAZARD] Damage applied");

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

    private bool IsPlayerClearlyAbove(Collider2D playerCollider)
    {
        if (playerCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        float playerFeetY = playerBounds.min.y;
        float playerCenterY = playerBounds.center.y;
        Bounds enemyBounds = GetAndroidBounds();
        float enemyTopY = enemyBounds.max.y;
        float enemyCenterY = enemyBounds.center.y;

        return playerCenterY > enemyCenterY + stompHeightOffset ||
               playerFeetY > enemyTopY - stompTopTolerance;
    }

    private bool IsPossibleStompFromSide(Collider2D playerCollider)
    {
        if (playerCollider == null || !alive)
        {
            return false;
        }

        Rigidbody2D playerBody = playerCollider.GetComponentInParent<Rigidbody2D>();

        if (playerBody == null || playerBody.linearVelocity.y > 0.5f)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds enemyBounds = GetAndroidBounds();

        bool playerBottomNearTop = playerBounds.min.y >= enemyBounds.max.y - 0.45f;
        bool playerCenterAbove = playerBounds.center.y > enemyBounds.center.y;

        return playerBottomNearTop || playerCenterAbove;
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

            if (stompZoneColliders[i] != null &&
                (zones[i].name == "StompZone_Top" || stompZoneCollider == null))
            {
                stompZoneCollider = stompZoneColliders[i];
            }
        }
    }

    private void RefreshSideHazards()
    {
        StompableAndroidSideHazard[] hazards = GetComponentsInChildren<StompableAndroidSideHazard>(true);
        sideHazardColliders = new Collider2D[hazards.Length];

        for (int i = 0; i < hazards.Length; i++)
        {
            if (hazards[i] == null)
            {
                continue;
            }

            hazards[i].Configure(this);
            sideHazardColliders[i] = hazards[i].GetComponent<Collider2D>();

            if (sideHazardColliders[i] == null)
            {
                continue;
            }

            if (hazards[i].name == "SideHazard_Left")
            {
                sideHazardLeftCollider = sideHazardColliders[i];
            }
            else if (hazards[i].name == "SideHazard_Right")
            {
                sideHazardRightCollider = sideHazardColliders[i];
            }
        }
    }

    private void PollContactZones()
    {
        if (!alive)
        {
            ResetPollTimers();
            return;
        }

        Collider2D stompPlayer = FindPlayerInZone(stompZoneCollider);

        if (stompPlayer != null)
        {
            LogStomp("[STOMP POLL] Player detected in StompZone");
            TryStomp(stompPlayer);
            ResetPollTimers();
            return;
        }

        bool handledLeft = PollSideHazard(sideHazardLeftCollider, true, ref leftPollContactStartTime, "Left");
        bool handledRight = PollSideHazard(sideHazardRightCollider, false, ref rightPollContactStartTime, "Right");

        if (!handledLeft)
        {
            leftPollContactStartTime = -1f;
        }

        if (!handledRight)
        {
            rightPollContactStartTime = -1f;
        }
    }

    private bool PollSideHazard(Collider2D sideCollider, bool isLeft, ref float contactStartTime, string label)
    {
        Collider2D playerCollider = FindPlayerInZone(sideCollider);

        if (playerCollider == null)
        {
            return false;
        }

        LogStomp($"[SIDE POLL] Player detected in {label}");

        if (!IsPlayerOnExpectedSide(playerCollider, isLeft))
        {
            LogStomp($"[SIDE POLL] Ignored wrong side: {label}");
            return false;
        }

        if (IsPossibleStompFromSide(playerCollider) || IsPlayerClearlyAbove(playerCollider))
        {
            LogStomp("[SIDE POLL] Ignored because stomp possible");
            return true;
        }

        if (contactStartTime < 0f)
        {
            contactStartTime = Time.time;
            return true;
        }

        if (Time.time - contactStartTime < sidePollDamageDelay)
        {
            return true;
        }

        StompableAndroidSideHazard source = sideCollider != null
            ? sideCollider.GetComponent<StompableAndroidSideHazard>()
            : null;

        HandleSideContact(playerCollider, source);
        return true;
    }

    private Collider2D FindPlayerInZone(Collider2D zoneCollider)
    {
        if (zoneCollider == null || !zoneCollider.enabled || !zoneCollider.gameObject.activeInHierarchy)
        {
            return null;
        }

        Bounds zoneBounds = zoneCollider.bounds;
        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false
        };
        int hitCount = Physics2D.OverlapBox(zoneBounds.center, zoneBounds.size, 0f, contactFilter, pollResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = pollResults[i];

            if (hit == null || hit == zoneCollider || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (IsPlayer(hit))
            {
                return hit;
            }
        }

        return null;
    }

    private bool IsPlayerOnExpectedSide(Collider2D playerCollider, bool isLeft)
    {
        if (playerCollider == null)
        {
            return false;
        }

        float playerCenterX = playerCollider.bounds.center.x;
        float androidCenterX = CurrentCenterX;

        return isLeft
            ? playerCenterX < androidCenterX
            : playerCenterX > androidCenterX;
    }

    private void ResetPollTimers()
    {
        leftPollContactStartTime = -1f;
        rightPollContactStartTime = -1f;
    }

    private bool IsValidSideHazardSource(Component source)
    {
        StompableAndroidSideHazard sideHazard = source as StompableAndroidSideHazard;

        if (sideHazard == null)
        {
            return false;
        }

        if (sideHazard.GetComponentInParent<StompableAndroidEnemy>() != this)
        {
            return false;
        }

        string sourceName = sideHazard.gameObject.name;
        return sourceName == "SideHazard_Left" || sourceName == "SideHazard_Right";
    }

    private string GetSideHazardLabel(Component source)
    {
        StompableAndroidSideHazard sideHazard = source as StompableAndroidSideHazard;
        return sideHazard != null ? sideHazard.SideLabel : "[SIDE] SideHazard contact";
    }

    private void WarnInvalidSideSource(Component source)
    {
        string sourceName = source != null ? source.name : "null";

        if (!hasWarnedInvalidSideSource)
        {
            Debug.LogWarning($"StompableAndroidEnemy '{name}': [SIDE] Ignored invalid side contact source: {sourceName}", this);
            hasWarnedInvalidSideSource = true;
        }
        else
        {
            LogStomp($"[SIDE] Ignored invalid side contact source: {sourceName}");
        }
    }

    private void LogSideContactDiagnostic(Collider2D playerCollider, Component source)
    {
        if (!debugStomp)
        {
            return;
        }

        Debug.LogWarning(
            "[SIDE CONTACT]\n" +
            $"source={DescribeComponent(source)}\n" +
            $"player position={GetColliderPosition(playerCollider)}\n" +
            $"android position={transform.position}\n" +
            System.Environment.StackTrace,
            this
        );
    }

    private void LogStompDiagnostic(string message, Collider2D playerCollider)
    {
        if (!debugStomp)
        {
            return;
        }

        Debug.Log(
            $"{message}\n" +
            $"player={DescribeCollider(playerCollider)}\n" +
            $"player position={GetColliderPosition(playerCollider)}\n" +
            $"android position={transform.position}\n" +
            System.Environment.StackTrace,
            this
        );
    }

    private static string DescribeComponent(Component component)
    {
        if (component == null)
        {
            return "null";
        }

        return $"{component.GetType().Name} on {component.gameObject.name}";
    }

    private static string DescribeCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return "null";
        }

        return $"{collider.GetType().Name} on {collider.gameObject.name}";
    }

    private static Vector3 GetColliderPosition(Collider2D collider)
    {
        return collider != null ? collider.transform.position : Vector3.zero;
    }

    private Bounds GetAndroidBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];

                if (spriteRenderer == null || !spriteRenderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = spriteRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(spriteRenderer.bounds);
                }
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        if (ownCollider != null && ownCollider.enabled)
        {
            return ownCollider.bounds;
        }

        Vector3 fallbackSize = new Vector3(
            Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.1f),
            Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.1f),
            0.1f
        );

        return new Bounds(transform.position, fallbackSize);
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

    private void SetSideHazardsEnabled(bool value)
    {
        if (sideHazardColliders == null || sideHazardColliders.Length == 0)
        {
            RefreshSideHazards();
        }

        if (sideHazardColliders == null)
        {
            return;
        }

        for (int i = 0; i < sideHazardColliders.Length; i++)
        {
            if (sideHazardColliders[i] != null)
            {
                sideHazardColliders[i].enabled = value;
                sideHazardColliders[i].isTrigger = true;
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
        sidePollDamageDelay = Mathf.Max(0f, sidePollDamageDelay);
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
