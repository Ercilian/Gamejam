using UnityEngine;
using UnityEngine.InputSystem;

//Script to handle player spawning at designated spawn points

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] spawnPoints; // Array of spawn points in the scene
    private int nextSpawnIndex = 0;
    public float spawnDistance = 2f; // Distancia en unidades desde el objeto spawn

    private void OnEnable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
        }
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        // Si tienes varios puntos de spawn, usa el siguiente libre
        if (spawnPoints.Length > 0)
        {
            Transform spawn = spawnPoints[nextSpawnIndex];
            // Calcula la posición a x unidades en la dirección forward del spawn
            Vector3 spawnOffset = spawn.position + spawn.forward * spawnDistance;
            player.transform.position = spawnOffset;
            player.transform.rotation = spawn.rotation;

            // Avanza al siguiente spawn para el próximo jugador
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        }
    }
}
