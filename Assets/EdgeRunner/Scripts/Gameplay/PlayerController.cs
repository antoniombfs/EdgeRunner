using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Base Form")]
    [SerializeField] private float baseMoveSpeed = 6f;
    [SerializeField] private float baseJumpForce = 12f;

    [Header("Speed Form")]
    [SerializeField] private float speedMoveSpeed = 10f;
    [SerializeField] private float speedJumpForce = 13.5f;

    [Header("General")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float transformCooldown = 1f;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private SpriteRenderer spriteRenderer;

    private bool isSpeedForm = false;
    private float cooldownTimer = 0f;

    public bool IsSpeedForm => isSpeedForm;
    public float CooldownRemaining => cooldownTimer;
    public string CurrentFormName => isSpeedForm ? "SPEED" : "BASE";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisuals();
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleTransformation();

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer < 0f)
            {
                cooldownTimer = 0f;
            }
        }
    }

    private void HandleMovement()
    {
        float move = Input.GetAxisRaw("Horizontal");
        float currentMoveSpeed = isSpeedForm ? speedMoveSpeed : baseMoveSpeed;

        rb.linearVelocity = new Vector2(move * currentMoveSpeed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        bool isGrounded = Physics2D.BoxCast(
            boxCol.bounds.center,
            boxCol.bounds.size,
            0f,
            Vector2.down,
            0.05f,
            groundLayer
        );

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            float currentJumpForce = isSpeedForm ? speedJumpForce : baseJumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
        }
    }

    private void HandleTransformation()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && cooldownTimer <= 0f)
        {
            isSpeedForm = !isSpeedForm;
            cooldownTimer = transformCooldown;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (isSpeedForm)
        {
            spriteRenderer.color = Color.yellow;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }
}