using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnerStart : MonoBehaviour
{
    public Transform[] spawnPoints; // Asigna los puntos de spawn en el inspector
    public GameObject[] playerPrefabs; // Asigna los prefabs en el mismo orden que los characterIndex
    public PlayerSelectionDataSO selectionDataSO; // Asigna el ScriptableObject en el inspector

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < selectionDataSO.selectedPlayers.Count; i++)
        {
            var info = selectionDataSO.selectedPlayers[i];
            var prefab = playerPrefabs[info.characterIndex];
            var spawn = spawnPoints.Length > i ? spawnPoints[i] : spawnPoints[0];

            // Instancia el jugador
            var playerObj = Instantiate(prefab, spawn.position, spawn.rotation);

            // Asigna el input device si usas PlayerInput
            var playerInput = playerObj.GetComponent<PlayerInput>();
            if (playerInput != null && !string.IsNullOrEmpty(info.inputDeviceId))
            {
                foreach (var device in InputSystem.devices)
                {
                    if (device.deviceId.ToString() == info.inputDeviceId)
                    {
                        playerInput.SwitchCurrentControlScheme(device);
                        break;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
