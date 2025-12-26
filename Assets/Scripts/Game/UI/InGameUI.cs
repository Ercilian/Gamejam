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
        UpdatePlayerPanels();
    }
    void UpdatePlayerPanels() // Method to update player panels based on selected players
    {
        foreach (var playerInfo in playerSelectionDataSO.selectedPlayers)
        {
            GameObject panel = Instantiate(playerPanelPrefab, panelParent); // Create a new panel for each selected player
            var stats = allPlayerStats[playerInfo.characterIndex]; // Get the corresponding stats for the player
            panel.GetComponent<PlayerPanelUI>().Setup(playerInfo, stats); // Setup the panel with player info and stats
        }
    }
}
