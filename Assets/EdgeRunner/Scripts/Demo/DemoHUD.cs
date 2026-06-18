using UnityEngine;
using UnityEngine.UI;

public class DemoHUD : MonoBehaviour
{
    [SerializeField] private Text energyCellsText;
    [SerializeField] private Text statusText;
    [SerializeField] private EdgeRunnerHUD gameplayHud;
    [SerializeField] private string energyLabel = "Energy Cells";
    [SerializeField] private string levelCompleteText = "Level Complete";

    public void SetGameplayHud(EdgeRunnerHUD newGameplayHud)
    {
        gameplayHud = newGameplayHud;
    }

    public void SetEnergyCells(int collected, int total)
    {
        if (energyCellsText != null)
        {
            energyCellsText.text = $"{energyLabel}: {collected}/{total}";
        }
    }

    public void ShowLevelComplete()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = levelCompleteText;
        statusText.enabled = true;

        if (gameplayHud != null)
        {
            gameplayHud.ShowLevelComplete();
        }
    }

    public void HideStatus()
    {
        if (statusText != null)
        {
            statusText.enabled = false;
        }

        if (gameplayHud != null)
        {
            gameplayHud.HideStatus();
        }
    }
}
