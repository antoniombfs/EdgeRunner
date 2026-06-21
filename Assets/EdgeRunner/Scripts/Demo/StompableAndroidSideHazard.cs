using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StompableAndroidSideHazard : MonoBehaviour
{
    public enum Side
    {
        Left,
        Right
    }

    [SerializeField] private StompableAndroidEnemy parentEnemy;
    [SerializeField] private string sideLabel = "SIDE HAZARD";
    [SerializeField] private Side side = Side.Left;
    [SerializeField] private float sideDamageDelay = 0.05f;
    [SerializeField] private bool debugSideHazard = false;

    private Collider2D hazardCollider;
    private readonly Dictionary<Collider2D, float> contactStartTimes = new Dictionary<Collider2D, float>();

    public string SideLabel => sideLabel;
    public Side HazardSide => side;

    public void Configure(StompableAndroidEnemy enemy)
    {
        parentEnemy = enemy;
        InferSideFromName();
        EnsureCollider();
    }

    public void Configure(StompableAndroidEnemy enemy, string label)
    {
        parentEnemy = enemy;
        sideLabel = label;
        InferSideFromName();
        EnsureCollider();
    }

    public void Configure(StompableAndroidEnemy enemy, string label, Side newSide)
    {
        parentEnemy = enemy;
        sideLabel = label;
        side = newSide;
        EnsureCollider();
    }

    private void Awake()
    {
        EnsureCollider();

        if (parentEnemy == null)
        {
            parentEnemy = GetComponentInParent<StompableAndroidEnemy>();
        }

        InferSideFromName();
    }

    private void Reset()
    {
        EnsureCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        contactStartTimes[other] = Time.time;

        if (debugSideHazard)
        {
            Debug.LogWarning(
                $"[SIDE HAZARD] {side} entered by {DescribeCollider(other)} side name={sideLabel} object={name}\n" +
                System.Environment.StackTrace,
                this
            );
        }

        TryForwardDelayedContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryForwardDelayedContact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        contactStartTimes.Remove(other);
    }

    private void TryForwardDelayedContact(Collider2D other)
    {
        if (parentEnemy == null || other == null)
        {
            return;
        }

        if (!IsCorrectSide(other))
        {
            contactStartTimes.Remove(other);

            if (debugSideHazard)
            {
                Debug.Log($"[SIDE HAZARD] Ignored wrong side: hazard={side} player={DescribeCollider(other)}", this);
            }

            return;
        }

        if (!contactStartTimes.TryGetValue(other, out float firstContactTime))
        {
            firstContactTime = Time.time;
            contactStartTimes[other] = firstContactTime;
        }

        if (Time.time - firstContactTime < sideDamageDelay)
        {
            return;
        }

        if (!parentEnemy.IsAlive)
        {
            return;
        }

        parentEnemy.HandleSideContact(other, this);
    }

    private bool IsCorrectSide(Collider2D other)
    {
        float playerCenterX = other.bounds.center.x;
        float androidCenterX = parentEnemy != null ? parentEnemy.CurrentCenterX : transform.position.x;

        return side == Side.Left
            ? playerCenterX < androidCenterX
            : playerCenterX > androidCenterX;
    }

    private void EnsureCollider()
    {
        if (hazardCollider == null)
        {
            hazardCollider = GetComponent<Collider2D>();
        }

        if (hazardCollider != null)
        {
            hazardCollider.isTrigger = true;
        }
    }

    private void InferSideFromName()
    {
        if (name.Contains("Right"))
        {
            side = Side.Right;
        }
        else if (name.Contains("Left"))
        {
            side = Side.Left;
        }
    }

    private static string DescribeCollider(Collider2D other)
    {
        if (other == null)
        {
            return "null";
        }

        return $"{other.GetType().Name} on {other.gameObject.name}";
    }

    private void OnValidate()
    {
        sideDamageDelay = Mathf.Max(0f, sideDamageDelay);
    }
}
