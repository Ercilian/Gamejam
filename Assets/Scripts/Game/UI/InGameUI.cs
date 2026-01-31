using UnityEngine;
using System.Collections.Generic;

public class InGameUI : MonoBehaviour
{
    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private Transform panelParent;
    public PlayerSelectionDataSO playerSelectionDataSO;
    public List<PlayerStatsData> allPlayerStats;

    void Start()
    {
    }
    public void UpdatePlayerPanels() // Method to update player panels based on selected players
    {
        foreach (var playerInfo in playerSelectionDataSO.selectedPlayers)
        {
            GameObject panel = Instantiate(playerPanelPrefab, panelParent); // Create a new panel for each selected player
            var stats = allPlayerStats[playerInfo.characterIndex]; // Get the corresponding stats for the player
            var panelUI = panel.GetComponent<PlayerPanelUI>();
            panelUI.Setup(playerInfo, stats); // Setup the panel with player info and stats

            // Buscar el PlayerInventory correspondiente en la escena
            PlayerInventory[] inventories = FindObjectsOfType<PlayerInventory>();
            bool foundInventory = false;
            foreach (var inv in inventories)
            {
                string invName = inv.entityStats.StatsData.PlayerName;
                if (stats.PlayerName == invName)
                {
                    inv.OnPotionsChanged += (count) => {
                        panelUI.UpdatePotionIcons(count);
                    };
                    // Forzar actualización inicial tras la suscripción
                    panelUI.UpdatePotionIcons(inv.potions.Count);
                    foundInventory = true;
                    break;
                }
            }
        }
    }
}
