using UnityEngine;

public class EdgeRunnerRunResetManager : MonoBehaviour
{
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private bool allowKeyboardReset = true;
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }
    }

    private void Update()
    {
        if (allowKeyboardReset && Input.GetKeyDown(resetKey))
        {
            ResetRun();
        }
    }

    public void ResetRun()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetRun();
        }
    }
}
