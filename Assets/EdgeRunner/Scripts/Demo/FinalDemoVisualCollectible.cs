using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FinalDemoVisualCollectible : MonoBehaviour
{
    [SerializeField] private FinalDemoController controller;
    [SerializeField] private float collectEffectDuration = 0.16f;
    [SerializeField] private float idleSpinSpeed = 75f;

    private Collider2D triggerCollider;
    private SpriteRenderer[] renderers;
    private Color[] initialColors;
    private Vector3 initialScale;
    private Quaternion initialRotation;
    private bool collected;

    public bool IsCollected => collected;

    public void Configure(FinalDemoController targetController)
    {
        controller = targetController;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        initialColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            initialColors[i] = renderers[i].color;
        }
        initialScale = transform.localScale;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (!collected)
        {
            transform.Rotate(0f, 0f, idleSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<EdgeRunnerAgentV5>() == null)
        {
            return;
        }

        collected = true;
        triggerCollider.enabled = false;
        if (controller == null)
        {
            controller = FindAnyObjectByType<FinalDemoController>();
        }
        controller?.NotifyVisualCollectibleCollected();
        Debug.Log($"[FINAL DEMO COLLECTIBLE] Collected {name}.");
        StartCoroutine(PlayCollectEffect());
    }

    private IEnumerator PlayCollectEffect()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, collectEffectDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = initialScale * Mathf.Lerp(1f, 1.65f, t);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = initialColors[i];
                color.a *= 1f - t;
                renderers[i].color = color;
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void ResetForNewRun()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        collected = false;
        transform.localScale = initialScale;
        transform.rotation = initialRotation;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].color = initialColors[i];
        }
    }
}
