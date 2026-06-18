using UnityEngine;
using UnityEngine.UI;

public class EdgeRunnerHUD : MonoBehaviour
{
    [SerializeField] private EdgeRunnerScoreManager scoreManager;
    [SerializeField] private Text energyCellsText;
    [SerializeField] private Text androidsText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text statusText;
    [SerializeField] private string levelCompleteText = "Level Complete";

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= HandleScoreChanged;
            scoreManager.GoalReached -= HandleGoalReached;
            scoreManager.RunReset -= HandleRunReset;
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void SetScoreManager(EdgeRunnerScoreManager newScoreManager)
    {
        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= HandleScoreChanged;
            scoreManager.GoalReached -= HandleGoalReached;
            scoreManager.RunReset -= HandleRunReset;
        }

        scoreManager = newScoreManager;
        Subscribe();
        Refresh();
    }

    public void ShowLevelComplete()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = levelCompleteText;
        statusText.enabled = true;
    }

    public void HideStatus()
    {
        if (statusText != null)
        {
            statusText.enabled = false;
        }
    }

    private void Subscribe()
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.ScoreChanged -= HandleScoreChanged;
        scoreManager.GoalReached -= HandleGoalReached;
        scoreManager.RunReset -= HandleRunReset;
        scoreManager.ScoreChanged += HandleScoreChanged;
        scoreManager.GoalReached += HandleGoalReached;
        scoreManager.RunReset += HandleRunReset;
    }

    private void HandleScoreChanged(EdgeRunnerScoreManager manager)
    {
        Refresh();
    }

    private void HandleGoalReached(EdgeRunnerScoreManager manager)
    {
        ShowLevelComplete();
    }

    private void HandleRunReset(EdgeRunnerScoreManager manager)
    {
        HideStatus();
        Refresh();
    }

    private void Refresh()
    {
        if (scoreManager == null)
        {
            return;
        }

        if (energyCellsText != null)
        {
            energyCellsText.text = $"Energy Cells: {scoreManager.EnergyCellsCollected}/{scoreManager.TotalEnergyCells}";
        }

        if (androidsText != null)
        {
            androidsText.text = $"Androids: {scoreManager.AndroidsKilled}/{scoreManager.TotalAndroids}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Score: {scoreManager.Score}";
        }

        if (timeText != null)
        {
            timeText.text = $"Time: {scoreManager.RunTime:0.00}s";
        }
    }
}
