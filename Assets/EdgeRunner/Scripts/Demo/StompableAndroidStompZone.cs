using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StompableAndroidStompZone : MonoBehaviour
{
    [SerializeField] private StompableAndroidEnemy parentEnemy;
    [SerializeField] private bool debugStompZone = false;

    private Collider2D zoneCollider;

    public void Configure(StompableAndroidEnemy enemy)
    {
        parentEnemy = enemy;
        EnsureCollider();
    }

    private void Awake()
    {
        EnsureCollider();

        if (parentEnemy == null)
        {
            parentEnemy = GetComponentInParent<StompableAndroidEnemy>();
        }
    }

    private void Reset()
    {
        EnsureCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleStomp(other, "enter");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleStomp(other, "stay");
    }

    private void TryHandleStomp(Collider2D other, string phase)
    {
        if (debugStompZone)
        {
            Debug.Log(
                $"[STOMP ZONE RAW] phase={phase} other={DescribeCollider(other)} tag={DescribeTag(other)} layer={DescribeLayer(other)}",
                this
            );
        }

        EnsureParentEnemy();

        if (parentEnemy == null)
        {
            Debug.LogError("[STOMP ZONE] Missing parent enemy reference", this);
            return;
        }

        if (!IsPlayer(other))
        {
            return;
        }

        if (debugStompZone)
        {
            Debug.Log($"[STOMP ZONE] {phase} by {other.name}", this);
        }

        parentEnemy.TryStomp(other);
    }

    private void EnsureParentEnemy()
    {
        if (parentEnemy == null)
        {
            parentEnemy = GetComponentInParent<StompableAndroidEnemy>();
        }
    }

    private void EnsureCollider()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
        }

        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
            zoneCollider.isTrigger = true;
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other != null &&
               (other.GetComponentInParent<EdgeRunnerAgentV5>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV5EnemyAware>() != null ||
                other.GetComponentInParent<EdgeRunnerAgentV5EnemiesTransfer>() != null ||
                other.GetComponentInParent<DemoPlayerDamageHandler>() != null ||
                other.CompareTag("Player"));
    }

    private static string DescribeCollider(Collider2D other)
    {
        if (other == null)
        {
            return "null";
        }

        return $"{other.GetType().Name} on {other.gameObject.name}";
    }

    private static string DescribeTag(Collider2D other)
    {
        return other != null ? other.tag : "null";
    }

    private static string DescribeLayer(Collider2D other)
    {
        if (other == null)
        {
            return "null";
        }

        int layer = other.gameObject.layer;
        string layerName = LayerMask.LayerToName(layer);
        return string.IsNullOrEmpty(layerName) ? layer.ToString() : $"{layerName} ({layer})";
    }
}
