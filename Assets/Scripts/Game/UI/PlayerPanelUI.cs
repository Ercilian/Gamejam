using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPanelUI : MonoBehaviour
{
    public Image characterImage;
    public TMP_Text playerNameText;
    public Image healthBar;
    public TMP_Text healthText;
    private int maxHealth;

    public void Setup(PlayerSelectionDataSO.PlayerInfo info, PlayerStatsData stats)
    {
        if (stats != null)
        {
            characterImage.sprite = stats.PlayerIcon;
            playerNameText.text = stats.PlayerName;
            maxHealth = stats.MaxHealth;
            healthText.text = $"{maxHealth} / {maxHealth}";
            SetHealth(maxHealth);
        }
        else
        {
            characterImage.enabled = false;
            playerNameText.text = "???";
            healthBar.fillAmount = 0f;
        }
    }

    public void SetHealth(int currentHealth)
    {
        if (maxHealth > 0)
            healthBar.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
    }
}
