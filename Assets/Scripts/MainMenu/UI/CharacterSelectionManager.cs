using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Configuración")]
    public PlayerInputManager playerInputManager;
    public PlayerSlotSimple[] playerSlots = new PlayerSlotSimple[4];
    
    [Header("Debug")]
    public bool debugLogs = true;
    
    [Header("Personajes jugables")]
    public GameObject[] characterPrefabs; // Asigna los 4 prefabs en el inspector
    
    private Dictionary<int, PlayerInput> activePlayers = new Dictionary<int, PlayerInput>();

    void Awake()
    {
        // Auto-asignar PlayerInputManager si no está asignado
        if (!playerInputManager)
        {
            playerInputManager = GetComponent<PlayerInputManager>();
            if (debugLogs && playerInputManager)
                Debug.Log("[CharacterSelection] PlayerInputManager auto-asignado");
        }
        
        // Inicializar slots
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
            {
                playerSlots[i].Initialize(i);
                playerSlots[i].manager = this; // Asigna el manager
            }
            else if (debugLogs)
            {
                Debug.LogWarning($"[CharacterSelection] Slot {i} no asignado en el inspector");
            }
        }
    }

    void OnEnable()
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
            
            if (debugLogs)
                Debug.Log("[CharacterSelection] Eventos suscritos");
                
            // Sincronizar jugadores ya existentes
            SyncExistingPlayers();
        }
        else if (debugLogs)
        {
            Debug.LogError("[CharacterSelection] No se encontró PlayerInputManager!");
        }
    }

    void OnDisable()
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // Ignora si el dispositivo principal es Mouse
        if (playerInput.devices.Count > 0 && playerInput.devices[0] is UnityEngine.InputSystem.Mouse)
        {
            Debug.Log("[CharacterSelection] Ignorado: No se puede unir con Mouse.");
            return;
        }

        // Busca el primer slot libre
        int slotIndex = -1;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null && !playerSlots[i].IsJoined)
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex == -1)
        {
            Debug.LogWarning("[CharacterSelection] No hay slots libres para el jugador.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[CharacterSelection] Jugador asignado al slot {slotIndex} con device: {playerInput.devices[0].name}");

        playerSlots[slotIndex].SetJoinedState(true);
        activePlayers[slotIndex] = playerInput;
    }

    void OnPlayerLeft(PlayerInput playerInput)
    {
        int playerIndex = playerInput.playerIndex;
        
        if (debugLogs)
            Debug.Log($"[CharacterSelection] Jugador {playerIndex} se desconectó");
        
        if (playerIndex >= 0 && playerIndex < playerSlots.Length)
        {
            if (playerSlots[playerIndex] != null)
            {
                playerSlots[playerIndex].SetJoinedState(false);
            }
            activePlayers.Remove(playerIndex);
        }
    }

    void SyncExistingPlayers()
    {
        if (debugLogs)
            Debug.Log($"[CharacterSelection] Sincronizando jugadores existentes. Total: {PlayerInput.all.Count}");
            
        foreach (var playerInput in PlayerInput.all)
        {
            if (!activePlayers.ContainsKey(playerInput.playerIndex))
            {
                OnPlayerJoined(playerInput);
            }
        }
    }

    public int GetJoinedPlayersCount()
    {
        return activePlayers.Count;
    }

    public PlayerInput[] GetActivePlayers()
    {
        PlayerInput[] players = new PlayerInput[activePlayers.Count];
        activePlayers.Values.CopyTo(players, 0);
        return players;
    }
}