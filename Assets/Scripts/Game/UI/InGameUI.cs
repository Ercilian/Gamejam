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
            Debug.Log($"[InGameUI] Hay {inventories.Length} PlayerInventory en escena");
            bool foundInventory = false;
            foreach (var inv in inventories)
            {
                if (inv == null)
                {
                    Debug.LogWarning($"[InGameUI] PlayerInventory es null");
                    continue;
                }
                if (inv.entityStats == null)
                {
                    Debug.LogWarning($"[InGameUI] entityStats es null en un PlayerInventory");
                    continue;
                }
                if (inv.entityStats.StatsData == null)
                {
                    Debug.LogWarning($"[InGameUI] StatsData es null en entityStats de {inv.entityStats}");
                    continue;
                }
                string invName = inv.entityStats.StatsData.PlayerName;
                Debug.Log($"[InGameUI] Comparando panel {stats.PlayerName} con inventario {invName}");
                if (stats.PlayerName == invName)
                {
                    Debug.Log($"[InGameUI] Asociando panel de {stats.PlayerName} con inventario de {invName}, pociones iniciales: {inv.potions.Count}");
                    inv.OnPotionsChanged += (count) => {
                        Debug.Log($"[InGameUI] Evento OnPotionsChanged recibido para {stats.PlayerName}: {count}");
                        panelUI.UpdatePotionIcons(count);
                    };
                    // Forzar actualización inicial tras la suscripción
                    panelUI.UpdatePotionIcons(inv.potions.Count);
                    foundInventory = true;
                    break;
                }
            }
            if (!foundInventory)
            {
                Debug.LogWarning($"[InGameUI] No se encontró PlayerInventory para el panel de {stats.PlayerName}");
            }
        }
    }
}
