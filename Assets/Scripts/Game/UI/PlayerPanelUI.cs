using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPanelUI : MonoBehaviour
{
    public Image characterImage;
    public TMP_Text playerNameText;
    public Image healthBar;
    public Transform potionsParent;
    public GameObject potionIconPrefab;
    private int maxHealth;

    public void Setup(PlayerSelectionDataSO.PlayerInfo info, PlayerStatsData stats)
    {
        if (stats != null)
        {
            characterImage.sprite = stats.PlayerIcon;
            playerNameText.text = stats.PlayerName;
            maxHealth = stats.MaxHealth;
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

    public void UpdatePotionIcons(int potionCount)
    {
        if (potionsParent == null || potionIconPrefab == null) return;
        // Elimina iconos previos
        for (int i = potionsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(potionsParent.GetChild(i).gameObject);
        }
        // Instancia los iconos según la cantidad de pociones, desplazados
        float offset = 40f; // píxeles de separación entre iconos
        for (int i = 0; i < potionCount; i++)
        {
            var icon = Instantiate(potionIconPrefab, potionsParent);
            var rect = icon.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(i * offset, 0);
        }
    }
}
