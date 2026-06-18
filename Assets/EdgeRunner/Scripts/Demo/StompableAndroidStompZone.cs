using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StompableAndroidStompZone : MonoBehaviour
{
    [SerializeField] private StompableAndroidEnemy parentEnemy;

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
        if (parentEnemy != null)
        {
            parentEnemy.TryStomp(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (parentEnemy != null)
        {
            parentEnemy.TryStomp(other);
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
            zoneCollider.isTrigger = true;
        }
    }
}
