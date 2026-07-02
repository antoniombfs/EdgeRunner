using UnityEngine;

public class FinalDemoAmbientMover : MonoBehaviour
{
    [SerializeField] private float travel = 2f;
    [SerializeField] private float speed = 0.65f;
    [SerializeField] private float phase;

    private Vector3 origin;

    public void Configure(float newTravel, float newSpeed, float newPhase)
    {
        travel = newTravel;
        speed = newSpeed;
        phase = newPhase;
    }

    private void Awake()
    {
        origin = transform.position;
    }

    private void Update()
    {
        transform.position = origin + Vector3.right * (Mathf.Sin(Time.time * speed + phase) * travel);
    }
}
