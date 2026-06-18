using System.Collections.Generic;
using UnityEngine;

public class DemoEnergyCellCounter : MonoBehaviour, IEdgeRunnerResettable
{
    [SerializeField] private DemoHUD hud;
    [SerializeField] private EdgeRunnerScoreManager scoreManager;

    private readonly HashSet<DemoEnergyCell> registeredCells = new HashSet<DemoEnergyCell>();
    private int collectedCount;

    public int CollectedCount => collectedCount;
    public int TotalCount => registeredCells.Count;

    public void SetHud(DemoHUD newHud)
    {
        hud = newHud;
        UpdateHud();
    }

    public void SetScoreManager(EdgeRunnerScoreManager newScoreManager)
    {
        scoreManager = newScoreManager;
    }

    public void RegisterCell(DemoEnergyCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if (registeredCells.Add(cell))
        {
            if (scoreManager != null)
            {
                scoreManager.RegisterEnergyCell(cell);
            }

            UpdateHud();
        }
    }

    public void CollectCell(DemoEnergyCell cell)
    {
        RegisterCell(cell);
        collectedCount = Mathf.Clamp(collectedCount + 1, 0, registeredCells.Count);
        UpdateHud();
    }

    private void Start()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<EdgeRunnerScoreManager>();
        }

        DemoEnergyCell[] cells = GetComponentsInChildren<DemoEnergyCell>(true);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].SetCounter(this);
            cells[i].SetScoreManager(scoreManager);
            RegisterCell(cells[i]);
        }

        UpdateHud();
    }

    public void ResetForNewRun()
    {
        collectedCount = 0;
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (hud != null)
        {
            hud.SetEnergyCells(collectedCount, registeredCells.Count);
        }
    }
}
