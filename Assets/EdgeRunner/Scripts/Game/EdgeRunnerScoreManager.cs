using System;
using System.Collections.Generic;
using UnityEngine;

public class EdgeRunnerScoreManager : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private EdgeRunnerRunMode runMode = EdgeRunnerRunMode.ScoreAttack;

    [Header("Scoring")]
    [SerializeField] private int energyCellPoints = 10;
    [SerializeField] private int androidKillPoints = 50;
    [SerializeField] private float timePenaltyPerSecond = 0f;

    [Header("Runtime")]
    [SerializeField] private bool resetOnStart = true;
    [SerializeField] private bool debugLogs = false;

    private readonly HashSet<DemoEnergyCell> registeredEnergyCells = new HashSet<DemoEnergyCell>();
    private readonly HashSet<StompableAndroidEnemy> registeredEnemies = new HashSet<StompableAndroidEnemy>();

    private bool goalReached;
    private float runStartTime;
    private float lockedRunTime;

    public event Action<EdgeRunnerScoreManager> ScoreChanged;
    public event Action<EdgeRunnerScoreManager> RunReset;
    public event Action<EdgeRunnerScoreManager> GoalReached;

    public EdgeRunnerRunMode RunMode => runMode;
    public int Score { get; private set; }
    public int EnergyCellsCollected { get; private set; }
    public int TotalEnergyCells => registeredEnergyCells.Count;
    public int AndroidsKilled { get; private set; }
    public int TotalAndroids => registeredEnemies.Count;
    public bool HasGoalBeenReached => goalReached;

    public float RunTime
    {
        get
        {
            if (goalReached)
            {
                return lockedRunTime;
            }

            return Mathf.Max(0f, Time.time - runStartTime);
        }
    }

    public int EnergyCellPoints => energyCellPoints;
    public int AndroidKillPoints => androidKillPoints;

    private void Awake()
    {
        runStartTime = Time.time;
    }

    private void Start()
    {
        RegisterSceneObjects();

        if (resetOnStart)
        {
            ResetRun(false);
        }
        else
        {
            NotifyScoreChanged();
        }
    }

    private void Update()
    {
        if (!goalReached)
        {
            NotifyScoreChanged();
        }
    }

    public void RegisterEnergyCell(DemoEnergyCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if (registeredEnergyCells.Add(cell))
        {
            NotifyScoreChanged();
        }
    }

    public void RegisterEnemy(StompableAndroidEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (registeredEnemies.Add(enemy))
        {
            NotifyScoreChanged();
        }
    }

    public void ResetRun()
    {
        ResetRun(true);
    }

    public void ResetRun(bool resetObjects)
    {
        Score = 0;
        EnergyCellsCollected = 0;
        AndroidsKilled = 0;
        goalReached = false;
        lockedRunTime = 0f;
        runStartTime = Time.time;

        RegisterSceneObjects();

        if (resetObjects)
        {
            ResetRegisteredObjects();
        }

        if (debugLogs)
        {
            Debug.Log("EdgeRunnerScoreManager: run reset.");
        }

        RunReset?.Invoke(this);
        NotifyScoreChanged();
    }

    public void AddEnergyCell(int points)
    {
        EnergyCellsCollected = Mathf.Clamp(EnergyCellsCollected + 1, 0, Mathf.Max(1, TotalEnergyCells));

        if (runMode == EdgeRunnerRunMode.ScoreAttack)
        {
            Score += Mathf.Max(0, points);
        }

        if (debugLogs)
        {
            Debug.Log($"EdgeRunnerScoreManager: energy cell collected. Score={Score}");
        }

        NotifyScoreChanged();
    }

    public void AddEnergyCell()
    {
        AddEnergyCell(energyCellPoints);
    }

    public void AddEnemyKill(int points)
    {
        AndroidsKilled = Mathf.Clamp(AndroidsKilled + 1, 0, Mathf.Max(1, TotalAndroids));

        if (runMode == EdgeRunnerRunMode.ScoreAttack)
        {
            Score += Mathf.Max(0, points);
        }

        if (debugLogs)
        {
            Debug.Log($"EdgeRunnerScoreManager: enemy killed. Score={Score}");
        }

        NotifyScoreChanged();
    }

    public void AddEnemyKill()
    {
        AddEnemyKill(androidKillPoints);
    }

    public void RegisterGoalReached()
    {
        if (goalReached)
        {
            return;
        }

        goalReached = true;
        lockedRunTime = Mathf.Max(0f, Time.time - runStartTime);

        if (runMode == EdgeRunnerRunMode.ScoreAttack && timePenaltyPerSecond > 0f)
        {
            Score -= Mathf.RoundToInt(lockedRunTime * timePenaltyPerSecond);
        }

        if (debugLogs)
        {
            Debug.Log($"EdgeRunnerScoreManager: goal reached. Score={Score}, Time={lockedRunTime:F2}s");
        }

        GoalReached?.Invoke(this);
        NotifyScoreChanged();
    }

    private void RegisterSceneObjects()
    {
        DemoEnergyCell[] cells = FindObjectsByType<DemoEnergyCell>(FindObjectsInactive.Include);

        for (int i = 0; i < cells.Length; i++)
        {
            RegisterEnergyCell(cells[i]);
        }

        StompableAndroidEnemy[] enemies = FindObjectsByType<StompableAndroidEnemy>(FindObjectsInactive.Include);

        for (int i = 0; i < enemies.Length; i++)
        {
            RegisterEnemy(enemies[i]);
        }
    }

    private void ResetRegisteredObjects()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null || behaviours[i] == this)
            {
                continue;
            }

            if (behaviours[i] is IEdgeRunnerResettable resettable)
            {
                resettable.ResetForNewRun();
            }
        }
    }

    private void NotifyScoreChanged()
    {
        ScoreChanged?.Invoke(this);
    }

    private void OnValidate()
    {
        energyCellPoints = Mathf.Max(0, energyCellPoints);
        androidKillPoints = Mathf.Max(0, androidKillPoints);
        timePenaltyPerSecond = Mathf.Max(0f, timePenaltyPerSecond);
    }
}
