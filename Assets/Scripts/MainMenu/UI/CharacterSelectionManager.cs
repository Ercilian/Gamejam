using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Settings")]
    public PlayerInputManager playerInputManager;
    public PlayerSlotSimple[] playerSlots = new PlayerSlotSimple[4];
    public GameObject selectCharacterPanel;
    public GameObject[] characterPrefabs;
    public TMPro.TMP_Text countdownText;
    public PlayerSelectionDataSO selectionDataSO;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip joinSound;
    public AudioClip leaveSound;
    public AudioClip confirmSound;
    public AudioClip unconfirmSound;
    public AudioClip countdownBeepSound;
    public AudioClip hoverSound;
    public AudioClip errorSound;

    private Dictionary<int, PlayerInput> activePlayers = new Dictionary<int, PlayerInput>();
    private Coroutine countdownCoroutine;




    // ========================================================================================= Methods ========================================================================================




    void Awake() // Initialize the character selection manager
    {
        if (!playerInputManager)
        {
            playerInputManager = GetComponent<PlayerInputManager>();
        }

        for (int i = 0; i < playerSlots.Length; i++)
        {
                playerSlots[i].Initialize(i);
                playerSlots[i].manager = this;
        }
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable() // Subscribe to player join/leave events
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
        }
    }

    void OnDisable() // Unsubscribe from player join/leave events
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }


    // ========================================================================================= Player Input Management ===========================================================================

    public void OnPlayerJoined(PlayerInput playerInput) // Handle a new player joining
    {
        if (playerInput.devices.Count == 0)
        {
            Debug.LogWarning("[CharacterSelection] Ignored: PlayerInput has no devices.");
            return;
        }

        audioSource.PlayOneShot(joinSound);

        // Cancel countdown if a new player joins during countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            audioSource.Stop(); // Stop any beep in progress
            if (countdownText != null)
                countdownText.text = "";
            Debug.Log("[CharacterSelection] Countdown cancelled because a new player joined.");
        }

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (!playerSlots[i].IsJoined)
            {
                Debug.Log($"[CharacterSelection] Player assigned to slot {i} with device: {playerInput.devices[0].name}");
                playerSlots[i].SetJoinedState(true);
                activePlayers[i] = playerInput;
                playerSlots[i].playerInput = playerInput;

                var uiMap = playerInput.actions.FindActionMap("UI", true);
                if (uiMap != null)
                {
                    var moveLeft = uiMap.FindAction("MoveLeft");
                    var moveRight = uiMap.FindAction("MoveRight");
                    var disconnect = uiMap.FindAction("Disconnect", false);
                    var confirm = uiMap.FindAction("Confirm");
                    var unconfirm = uiMap.FindAction("Unconfirm", false);

                    int playerIndex = playerInput.playerIndex;
                    if (playerIndex >= 0 && playerIndex < playerSlots.Length)
                    {
                        moveLeft.performed -= playerSlots[playerIndex].OnLeftArrowPressed;
                        moveRight.performed -= playerSlots[playerIndex].OnRightArrowPressed;
                        confirm.performed -= playerSlots[playerIndex].OnConfirmPressed;

                        moveLeft.performed += playerSlots[playerIndex].OnLeftArrowPressed;
                        moveRight.performed += playerSlots[playerIndex].OnRightArrowPressed;
                        confirm.performed += playerSlots[playerIndex].OnConfirmPressed;

                        if (disconnect != null)
                        {
                            disconnect.performed += ctx =>
                            {
                                Debug.Log($"[CharacterSelection] Player {playerIndex} disconnected by input.");
                                Destroy(playerInput.gameObject);
                            };
                        }

                        confirm.performed += ctx =>
                        {
                            if (selectCharacterPanel.activeSelf
                                && playerSlots[playerIndex] != null
                                && playerSlots[playerIndex].IsJoined
                                && !playerSlots[playerIndex].IsConfirmed)
                            {
                                playerSlots[playerIndex].OnConfirmPressed();
                            }
                        };

                        if (unconfirm != null)
                        {
                            unconfirm.performed += ctx =>
                            {
                                if (selectCharacterPanel.activeSelf
                                    && playerSlots[playerIndex] != null
                                    && playerSlots[playerIndex].IsJoined
                                    && playerSlots[playerIndex].IsConfirmed)
                                {
                                    playerSlots[playerIndex].OnUnconfirmPressed();
                                }
                            };
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Not found Action Map 'UI'.");
                }
                return;
            }
        }
        return;
    }

    public void OnPlayerLeft(PlayerInput playerInput) // Handle a player leaving
    {
        audioSource.PlayOneShot(leaveSound);
        int playerIndex = playerInput.playerIndex;
        Debug.Log($"[CharacterSelection] Player {playerIndex} disconnected");

        if (playerIndex >= 0 && playerIndex < playerSlots.Length)
        {
            playerSlots[playerIndex].SetJoinedState(false);
            activePlayers.Remove(playerIndex);
        }
    }


    // ========================================================================================= Selection State ====================================================================================

    public void ResetSelection() // Reset the character selection state
    {
        var playersToRemove = new List<PlayerInput>(activePlayers.Values);
        foreach (var playerInput in playersToRemove)
        {
            if (playerInput != null)
                Destroy(playerInput.gameObject);
        }
        activePlayers.Clear();

        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].SetJoinedState(false);
            playerSlots[i].ResetSlotState();
        }
        Debug.Log("[CharacterSelection] Character selection reset.");
    }

    private bool AllPlayersConfirmed() // Check if all joined players have confirmed their selection
    {
        int joinedCount = 0;
        int confirmedCount = 0;

        foreach (var slot in playerSlots)
        {
            if (slot.IsJoined)
            {
                joinedCount++;
                if (slot.IsConfirmed)
                    confirmedCount++;
            }
        }
        return joinedCount > 0 && confirmedCount == joinedCount;
    }

    public void OnPlayerConfirmed() // Handle a player confirming their selection
    {
        audioSource.PlayOneShot(confirmSound);
        if (AllPlayersConfirmed())
        {
            if (countdownCoroutine == null)
                countdownCoroutine = StartCoroutine(StartCountdownAndLoadScene());
        }
    }

    public void OnPlayerUnconfirmed() // Handle a player unconfirming their selection
    {
        audioSource.PlayOneShot(unconfirmSound);
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            audioSource.Stop(); // Detener cualquier beep en curso
            if (countdownText != null)
                countdownText.text = "";
            Debug.Log("[CharacterSelection] Countdown cancelled by a player.");
        }
    }

    // ========================================================================================= Scene Transition ==================================================================================

    private IEnumerator StartCountdownAndLoadScene() // Start countdown and load the main scene
    {
        float countdown = 3f;
        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = $" {Mathf.CeilToInt(countdown)}...";
            Debug.Log($"Starting in {Mathf.CeilToInt(countdown)}...");
            audioSource.clip = countdownBeepSound;
            audioSource.Play();
            yield return new WaitForSeconds(1f);
            audioSource.Stop();
            countdown -= 1f;
        }

        if (countdownText != null)
            countdownText.text = "";

        SaveConfirmedPlayersToSO();
        SceneManager.LoadScene("MainScene");
    }

    public void SaveConfirmedPlayersToSO() // Save confirmed player selections to the ScriptableObject
    {
        selectionDataSO.Clear();
        for (int i = 0; i < playerSlots.Length; i++)
        {
            var slot = playerSlots[i];
            if (slot.IsConfirmed)
            {
                var info = new PlayerSelectionDataSO.PlayerInfo();
                info.slotIndex = i;
                info.characterIndex = slot.selectedCharacterIndex;
                if (slot.playerInput != null && slot.playerInput.devices.Count > 0)
                    info.inputDeviceId = slot.playerInput.devices[0].deviceId.ToString();
                selectionDataSO.selectedPlayers.Add(info);
            }
        }
    }

    public void PlayHoverSound()
    {
        audioSource.PlayOneShot(hoverSound);
    }
}

    