using UnityEngine;

[DisallowMultipleComponent]
public class DemoManualPlayerController : MonoBehaviour, IEdgeRunnerResettable
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement")]
    [SerializeField] private float normalMoveSpeed = 8f;
    [SerializeField] private float sprintMoveSpeed = 11f;
    [SerializeField] private float jumpForce = 12.8f;

    [Header("Jump Forgiveness")]
    [SerializeField] private float coyoteTime = 0.22f;
    [SerializeField] private float jumpBufferTime = 0.18f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckWidth = 0.90f;
    [SerializeField] private float groundCheckHeight = 0.16f;
    [SerializeField] private float groundCheckExtraDistance = 0.04f;

    [Header("Debug")]
    [SerializeField] private bool debugManualInput = false;

    private readonly Collider2D[] groundHits = new Collider2D[12];
    private float inputX;
    private bool sprintHeld;
    private bool jumpHeld;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool pendingJumpRequest;
    private bool loggedCurrentJumpBlocked;

    public void Configure(Rigidbody2D newRb, Collider2D newBodyCollider, LayerMask newGroundLayer)
    {
        rb = newRb;
        bodyCollider = newBodyCollider;
        groundLayer = newGroundLayer;
        ResetControllerState();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetControllerState();
    }

    private void OnDisable()
    {
        if (pendingJumpRequest && !loggedCurrentJumpBlocked)
        {
            LogJumpBlocked("controller disabled", GetGroundCheckInfo());
        }
    }

    private void Update()
    {
        inputX = GetHorizontalInput();
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        jumpHeld = IsJumpHeld();

        if (IsJumpPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
            pendingJumpRequest = true;
            loggedCurrentJumpBlocked = false;
            LogManualJumpInput();
        }
        else if (jumpHeld && jumpBufferCounter > 0f)
        {
            jumpBufferCounter = Mathf.Max(jumpBufferCounter, Time.fixedDeltaTime);
        }
    }

    private void FixedUpdate()
    {
        ResolveReferences();

        if (rb == null)
        {
            LogJumpBlockedIfPending("missing Rigidbody2D", GetGroundCheckInfo());
            return;
        }

        float fixedDeltaTime = Time.fixedDeltaTime;
        GroundCheckInfo groundInfo = GetGroundCheckInfo();
        bool grounded = groundInfo.grounded;

        coyoteCounter = grounded
            ? coyoteTime
            : Mathf.Max(0f, coyoteCounter - fixedDeltaTime);

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            pendingJumpRequest = false;
            loggedCurrentJumpBlocked = false;
            LogManualJumpExecuted(groundInfo);
        }
        else if (jumpBufferCounter > 0f)
        {
            string blockReason = GetJumpBlockReason(groundInfo);
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - fixedDeltaTime);

            if (jumpBufferCounter <= 0f)
            {
                LogJumpBlockedIfPending(blockReason, groundInfo);
            }
        }

        float activeSpeed = sprintHeld && Mathf.Abs(inputX) > 0.01f
            ? sprintMoveSpeed
            : normalMoveSpeed;

        rb.linearVelocity = new Vector2(inputX * activeSpeed, rb.linearVelocity.y);
    }

    public void ResetForNewRun()
    {
        ResetControllerState();
    }

    public void ResetControllerState()
    {
        inputX = 0f;
        sprintHeld = false;
        jumpHeld = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        pendingJumpRequest = false;
        loggedCurrentJumpBlocked = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void ResolveReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponentInChildren<Collider2D>();
        }
    }

    private GroundCheckInfo GetGroundCheckInfo()
    {
        if (bodyCollider == null)
        {
            return new GroundCheckInfo
            {
                center = transform.position,
                size = new Vector2(groundCheckWidth, groundCheckHeight),
                groundLayerInvalid = groundLayer.value == 0
            };
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 checkCenter = new Vector2(
            bounds.center.x,
            bounds.min.y - groundCheckExtraDistance
        );
        Vector2 checkSize = new Vector2(
            Mathf.Max(groundCheckWidth, 0.01f),
            Mathf.Max(groundCheckHeight, 0.01f)
        );

        GroundCheckInfo info = new GroundCheckInfo
        {
            center = checkCenter,
            size = checkSize,
            groundLayerInvalid = groundLayer.value == 0
        };

        if (info.groundLayerInvalid)
        {
            return info;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapBox(
            checkCenter,
            checkSize,
            0f,
            filter,
            groundHits
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = groundHits[i];
            groundHits[i] = null;

            if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            info.grounded = true;
            info.groundCollider = hit;
            return info;
        }

        return info;
    }

    private string GetJumpBlockReason(GroundCheckInfo groundInfo)
    {
        if (!enabled)
        {
            return "controller disabled";
        }

        if (rb == null)
        {
            return "missing Rigidbody2D";
        }

        if (bodyCollider == null)
        {
            return "missing Collider2D";
        }

        if (groundInfo.groundLayerInvalid)
        {
            return "groundLayer invalid";
        }

        if (!groundInfo.grounded && coyoteCounter <= 0f)
        {
            return "no ground and no coyote";
        }

        return "unknown";
    }

    private void LogManualJumpInput()
    {
        if (!debugManualInput)
        {
            return;
        }

        GroundCheckInfo groundInfo = GetGroundCheckInfo();

        Debug.Log(
            "[MANUAL JUMP INPUT]\n" +
            BuildJumpDebugMessage(groundInfo),
            this
        );
    }

    private void LogManualJumpExecuted(GroundCheckInfo groundInfo)
    {
        if (!debugManualInput)
        {
            return;
        }

        Debug.Log(
            "[MANUAL JUMP EXECUTED]\n" +
            BuildJumpDebugMessage(groundInfo),
            this
        );
    }

    private void LogJumpBlockedIfPending(string reason, GroundCheckInfo groundInfo)
    {
        if (!pendingJumpRequest || loggedCurrentJumpBlocked)
        {
            return;
        }

        LogJumpBlocked(reason, groundInfo);
    }

    private void LogJumpBlocked(string reason, GroundCheckInfo groundInfo)
    {
        if (!debugManualInput)
        {
            return;
        }

        Debug.LogWarning(
            $"[MANUAL JUMP BLOCKED: {reason}]\n" +
            BuildJumpDebugMessage(groundInfo),
            this
        );

        loggedCurrentJumpBlocked = true;
        pendingJumpRequest = false;
    }

    private string BuildJumpDebugMessage(GroundCheckInfo groundInfo)
    {
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;

        return
            $"frame={Time.frameCount}\n" +
            $"grounded={groundInfo.grounded}\n" +
            $"coyoteCounter={coyoteCounter:F3}\n" +
            $"jumpBufferCounter={jumpBufferCounter:F3}\n" +
            $"rb.velocity={velocity}\n" +
            $"groundCheck position={groundInfo.center} size={groundInfo.size}\n" +
            $"groundLayer={FormatGroundLayerMask()}\n" +
            $"groundCollider={DescribeCollider(groundInfo.groundCollider)}";
    }

    private string FormatGroundLayerMask()
    {
        if (groundLayer.value == 0)
        {
            return "None (0)";
        }

        return groundLayer.value.ToString();
    }

    private static string DescribeCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return "none";
        }

        int layer = collider.gameObject.layer;
        string layerName = LayerMask.LayerToName(layer);
        string displayLayer = string.IsNullOrEmpty(layerName) ? layer.ToString() : $"{layerName} ({layer})";
        return $"{collider.GetType().Name} on {collider.gameObject.name} layer={displayLayer}";
    }

    private static bool IsJumpPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.UpArrow);
    }

    private static bool IsJumpHeld()
    {
        return Input.GetKey(KeyCode.Space) ||
               Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.UpArrow);
    }

    private static float GetHorizontalInput()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input += 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private void OnValidate()
    {
        normalMoveSpeed = Mathf.Max(0f, normalMoveSpeed);
        sprintMoveSpeed = Mathf.Max(0f, sprintMoveSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        groundCheckWidth = Mathf.Max(0.01f, groundCheckWidth);
        groundCheckHeight = Mathf.Max(0.01f, groundCheckHeight);
        groundCheckExtraDistance = Mathf.Max(0f, groundCheckExtraDistance);
    }

    private struct GroundCheckInfo
    {
        public bool grounded;
        public bool groundLayerInvalid;
        public Collider2D groundCollider;
        public Vector2 center;
        public Vector2 size;
    }
}
