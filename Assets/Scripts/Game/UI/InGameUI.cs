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
        foreach (Transform child in panelParent)
            Destroy(child.gameObject);

        foreach (var playerInfo in playerSelectionDataSO.selectedPlayers)
        {
            GameObject panel = Instantiate(playerPanelPrefab, panelParent);
            var stats = allPlayerStats[playerInfo.characterIndex];
            panel.GetComponent<PlayerPanelUI>().Setup(playerInfo, stats);
        }
    }
}
