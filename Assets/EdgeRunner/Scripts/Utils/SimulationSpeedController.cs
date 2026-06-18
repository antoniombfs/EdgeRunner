using UnityEngine;

public class SimulationSpeedController : MonoBehaviour
{
    [Header("Simulation Speed")]
    [SerializeField] private float startTimeScale = 1f;
    [SerializeField] private bool applyOnStart = true;

    private void Start()
    {
        if (applyOnStart)
        {
            SetTimeScale(startTimeScale);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetTimeScale(1f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetTimeScale(2f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetTimeScale(3f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetTimeScale(5f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetTimeScale(10f);
        }
    }

    private void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Debug.Log("Simulation Time Scale set to: " + scale);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}