using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TextMeshProUGUI formText;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private void Update()
    {
        if (playerController == null) return;

        formText.text = "Forma: " + playerController.CurrentFormName;

        if (playerController.IsSpeedForm)
        {
            formText.color = Color.yellow;
        }
        else
        {
            formText.color = Color.white;
        }

        if (playerController.CooldownRemaining > 0f)
        {
            cooldownText.text = "Cooldown: " + playerController.CooldownRemaining.ToString("F1") + "s";
        }
        else
        {
            cooldownText.text = "Cooldown: pronto";
        }
    }
}