using UnityEngine;

public class DemoCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(4f, 2.5f, -10f);
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private bool lockZ = true;
    [SerializeField] private float fixedZ = -10f;

    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (lockZ)
        {
            desiredPosition.z = fixedZ;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            Mathf.Max(0.01f, smoothTime)
        );
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0.01f, smoothTime);
    }
}
