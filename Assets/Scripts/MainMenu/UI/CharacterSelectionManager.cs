using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
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
    public GameObject SelectCharacterPanel; // Asigna el panel en el inspector
    public TMPro.TMP_Text countdownText; // Añade esto en CharacterSelectionManager
    
    private Dictionary<int, PlayerInput> activePlayers = new Dictionary<int, PlayerInput>();
    private Coroutine countdownCoroutine; // Guarda la referencia

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

        // Añade listeners para las acciones de izquierda/derecha y desconexión
        var actions = playerInput.actions;
        var uiMap = playerInput.actions.FindActionMap("UI", true);
        if (uiMap != null)
        {
            var moveLeft = uiMap.FindAction("MoveLeft");
            var moveRight = uiMap.FindAction("MoveRight");
            var disconnect = uiMap.FindAction("Disconnect", false);
            var confirm = uiMap.FindAction("Confirm");
            var unconfirm = uiMap.FindAction("Unconfirm", false); // Usa el nombre de tu acción

            int playerIndex = playerInput.playerIndex;
            if (playerIndex >= 0 && playerIndex < playerSlots.Length)
            {
                // Desuscribe primero
                moveLeft.performed -= playerSlots[playerIndex].OnLeftArrowPressed;
                moveRight.performed -= playerSlots[playerIndex].OnRightArrowPressed;
                confirm.performed -= playerSlots[playerIndex].OnConfirmPressed;

                // Suscribe después
                moveLeft.performed += playerSlots[playerIndex].OnLeftArrowPressed;
                moveRight.performed += playerSlots[playerIndex].OnRightArrowPressed;
                confirm.performed += playerSlots[playerIndex].OnConfirmPressed;

                // Listener para desconectar
                if (disconnect != null)
                {
                    disconnect.performed += ctx => {
                        if (debugLogs)
                            Debug.Log($"[CharacterSelection] Jugador {playerIndex} desconectado por input.");
                        Destroy(playerInput.gameObject); // <-- Elimina el jugador correctamente
                    };
                }

                // Suscribe el evento para el botón Ready
                confirm.performed += ctx =>
                {
                    if (SelectCharacterPanel.activeSelf // Solo si está activa la selección
                        && playerSlots[playerIndex] != null
                        && playerSlots[playerIndex].IsJoined
                        && !playerSlots[playerIndex].IsConfirmed)
                    {
                        playerSlots[playerIndex].OnConfirmPressed();
                    }
                };

                // Suscribe el evento para el botón Unready
                if (unconfirm != null)
                {
                    unconfirm.performed += ctx =>
                    {
                        if (SelectCharacterPanel.activeSelf
                            && playerSlots[playerIndex] != null
                            && playerSlots[playerIndex].IsJoined
                            && playerSlots[playerIndex].IsConfirmed) // Solo si está confirmado
                        {
                            playerSlots[playerIndex].OnUnconfirmPressed();
                        }
                    };
                }
            }
        }
        else
        {
            Debug.LogWarning("No se encontró el Action Map 'UI'.");
        }
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

    void Update()
    {
        // Solo si hay jugadores unidos
        for (int i = 0; i < playerSlots.Length; i++)
        {
            var slot = playerSlots[i];
            if (slot != null && slot.IsJoined)
            {
                // Puedes usar Input.GetKeyDown o el sistema InputSystem
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    slot.OnLeftArrowPressed();
                }
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    slot.OnRightArrowPressed();
                }
                // Si usas InputSystem, puedes mapear acciones y llamarlas aquí
            }
        }
    }

    public void ResetSelection()
    {
        // Haz una copia de los PlayerInput activos antes de destruirlos
        var playersToRemove = new List<PlayerInput>(activePlayers.Values);
        foreach (var playerInput in playersToRemove)
        {
            if (playerInput != null)
                Destroy(playerInput.gameObject);
        }
        activePlayers.Clear();

        // Resetea los slots visuales y de estado
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
            {
                playerSlots[i].SetJoinedState(false);
                playerSlots[i].ResetSlotState();
            }
        }

        if (debugLogs)
            Debug.Log("[CharacterSelection] Selección de personajes reiniciada.");
    }

    private bool AllPlayersConfirmed()
    {
        int joinedCount = 0;
        int confirmedCount = 0;

        foreach (var slot in playerSlots)
        {
            if (slot != null && slot.IsJoined)
            {
                joinedCount++;
                if (slot.IsConfirmed)
                    confirmedCount++;
            }
        }
        // Solo inicia si todos los que están joined están confirmados y hay al menos uno
        return joinedCount > 0 && confirmedCount == joinedCount;
    }

    public void OnPlayerConfirmed()
    {
        if (AllPlayersConfirmed())
        {
            // Solo inicia si no está ya corriendo
            if (countdownCoroutine == null)
                countdownCoroutine = StartCoroutine(StartCountdownAndLoadScene());
        }
    }

    public void OnPlayerUnconfirmed()
    {
        // Si alguien cancela, detén la cuenta atrás
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            if (countdownText != null)
                countdownText.text = ""; // Limpia el texto
            Debug.Log("[CharacterSelection] Cuenta atrás cancelada por un jugador.");
        }
    }

    private IEnumerator StartCountdownAndLoadScene()
    {
        float countdown = 3f;
        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = $" {Mathf.CeilToInt(countdown)}...";
            Debug.Log($"Comenzando en {Mathf.CeilToInt(countdown)}...");
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        if (countdownText != null)
            countdownText.text = ""; // Limpia el texto al terminar

        SceneManager.LoadScene("MainScene");
    }
}