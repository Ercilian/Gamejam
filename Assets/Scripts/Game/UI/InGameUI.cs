using UnityEngine;
using System.Collections.Generic;

public class InGameUI : MonoBehaviour
{
    [SerializeField] private GameObject playerPanelPrefab; // Asigna tu prefab en el inspector
    [SerializeField] private Transform panelParent; // El objeto con el Horizontal Layout Group
    public PlayerSelectionDataSO playerSelectionDataSO; // Asigna en el inspector


    void Start()
    {
        UpdatePlayerPanels();
    }
    void UpdatePlayerPanels()
    {
        foreach (Transform child in panelParent)
            Destroy(child.gameObject);

        foreach (var playerInfo in playerSelectionDataSO.selectedPlayers)
        {
            GameObject panel = Instantiate(playerPanelPrefab, panelParent);
        }
    }
}
