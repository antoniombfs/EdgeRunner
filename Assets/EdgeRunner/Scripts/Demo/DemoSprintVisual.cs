using UnityEngine;

[DisallowMultipleComponent]
public class DemoSprintVisual : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float sprintOnThreshold = 9.6f;
    [SerializeField] private float sprintOffThreshold = 7.8f;
    [SerializeField] private float minSprintVisualTime = 0.45f;
    [SerializeField] private float speedSmoothTime = 0.15f;
    [SerializeField] private float colorLerpSpeed = 8f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color sprintColor = new Color(0.35f, 0.95f, 1f, 1f);
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private TrailRenderer trailRenderer;

    private bool isSprintVisualActive;
    private float smoothedAbsSpeed;
    private float sprintVisualActivatedAt;

    public bool IsSprintVisualActive => isSprintVisualActive;

    public void Configure(Rigidbody2D newRb, SpriteRenderer newSpriteRenderer, TrailRenderer newTrailRenderer)
    {
        rb = newRb;
        spriteRenderer = newSpriteRenderer;
        trailRenderer = newTrailRenderer;

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }

        smoothedAbsSpeed = 0f;
        ApplySprintVisual(false, true);
        ApplyTrailState();
    }

    private void Awake()
    {
        ResolveReferences();
        smoothedAbsSpeed = 0f;
        ApplySprintVisual(false, true);
        ApplyTrailState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        smoothedAbsSpeed = 0f;
        ApplySprintVisual(false, true);
        ApplyTrailState();
    }

    private void Update()
    {
        ResolveReferences();

        float horizontalSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        float speedLerp = speedSmoothTime <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / speedSmoothTime);
        smoothedAbsSpeed = Mathf.Lerp(smoothedAbsSpeed, horizontalSpeed, speedLerp);

        if (!isSprintVisualActive)
        {
            if (rb != null && smoothedAbsSpeed >= sprintOnThreshold)
            {
                ApplySprintVisual(true, false);
            }
        }
        else
        {
            bool canTurnOff = Time.time - sprintVisualActivatedAt >= minSprintVisualTime;

            if (smoothedAbsSpeed <= sprintOffThreshold && canTurnOff)
            {
                ApplySprintVisual(false, false);
            }
        }

        ApplySpriteColor();
        ApplyTrailState();
    }

    private void OnDisable()
    {
        ApplySprintVisual(false, true);
        ApplyTrailState();
    }

    private void ResolveReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
    }

    private void ApplySprintVisual(bool sprintActive, bool applyColorImmediately)
    {
        if (sprintActive && !isSprintVisualActive)
        {
            sprintVisualActivatedAt = Time.time;
        }

        isSprintVisualActive = sprintActive;

        if (applyColorImmediately && spriteRenderer != null)
        {
            spriteRenderer.color = sprintActive ? sprintColor : normalColor;
        }
    }

    private void ApplySpriteColor()
    {
        if (spriteRenderer != null)
        {
            Color targetColor = isSprintVisualActive ? sprintColor : normalColor;
            float colorLerp = colorLerpSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime);
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, colorLerp);
        }
    }

    private void ApplyTrailState()
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.enabled = true;
        trailRenderer.emitting = isSprintVisualActive && enableTrail;
    }

    private void OnValidate()
    {
        sprintOnThreshold = Mathf.Max(0.01f, sprintOnThreshold);
        sprintOffThreshold = Mathf.Clamp(sprintOffThreshold, 0.01f, sprintOnThreshold);
        minSprintVisualTime = Mathf.Max(0f, minSprintVisualTime);
        speedSmoothTime = Mathf.Max(0f, speedSmoothTime);
        colorLerpSpeed = Mathf.Max(0f, colorLerpSpeed);
    }
}
