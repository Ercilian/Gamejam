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
    private AudioSource audioSource;
    public AudioClip joinSound;
    public AudioClip leaveSound;
    public AudioClip confirmSound;
    public AudioClip unconfirmSound;
    public AudioClip countdownBeepSound;
    public AudioClip hoverSound;

    private Dictionary<int, PlayerInput> activePlayers = new Dictionary<int, PlayerInput>();
    private Coroutine countdownCoroutine;

    public int GetJoinedPlayersCount() => activePlayers.Count;


    // ========================================================================================= Methods ========================================================================================




    void Awake()
    {
        if (!playerInputManager)
        {
            playerInputManager = GetComponent<PlayerInputManager>();
        }

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
            {
                playerSlots[i].Initialize(i);
                playerSlots[i].manager = this;
            }
        }
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
            SyncExistingPlayers();
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

    void Update()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            var slot = playerSlots[i];
            if (slot != null && slot.IsJoined)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    slot.OnLeftArrowPressed();
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    slot.OnRightArrowPressed();
            }
        }
    }

    // ========================================================================================= Player Input Management ===========================================================================

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (playerInput.devices.Count > 0 && playerInput.devices[0] is Mouse)
        {
            Debug.Log("[CharacterSelection] Ignored: Cannot join with Mouse.");
            return;
        }

        audioSource.PlayOneShot(joinSound);
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
            Debug.LogWarning("[CharacterSelection] No free slots available for the player.");
            return;
        }

        Debug.Log($"[CharacterSelection] Player assigned to slot {slotIndex} with device: {playerInput.devices[0].name}");

        playerSlots[slotIndex].SetJoinedState(true);
        activePlayers[slotIndex] = playerInput;
        playerSlots[slotIndex].playerInput = playerInput;

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
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        audioSource.PlayOneShot(leaveSound);
        int playerIndex = playerInput.playerIndex;
        Debug.Log($"[CharacterSelection] Player {playerIndex} disconnected");

        if (playerIndex >= 0 && playerIndex < playerSlots.Length)
        {
            if (playerSlots[playerIndex] != null)
                playerSlots[playerIndex].SetJoinedState(false);
            activePlayers.Remove(playerIndex);
        }
    }

    void SyncExistingPlayers()
    {
        Debug.Log($"[CharacterSelection] Syncing existing players. Total: {PlayerInput.all.Count}");
        foreach (var playerInput in PlayerInput.all)
        {
            if (!activePlayers.ContainsKey(playerInput.playerIndex))
                OnPlayerJoined(playerInput);
        }
    }


    public PlayerInput[] GetActivePlayers()
    {
        PlayerInput[] players = new PlayerInput[activePlayers.Count];
        activePlayers.Values.CopyTo(players, 0);
        return players;
    }

    // ========================================================================================= Selection State ====================================================================================

    public void ResetSelection()
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
            if (playerSlots[i] != null)
            {
                playerSlots[i].SetJoinedState(false);
                playerSlots[i].ResetSlotState();
            }
        }
        Debug.Log("[CharacterSelection] Character selection reset.");
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
        return joinedCount > 0 && confirmedCount == joinedCount;
    }

    public void OnPlayerConfirmed()
    {
        audioSource.PlayOneShot(confirmSound);
        if (AllPlayersConfirmed())
        {
            if (countdownCoroutine == null)
                countdownCoroutine = StartCoroutine(StartCountdownAndLoadScene());
        }
    }

    public void OnPlayerUnconfirmed()
    {
        audioSource.PlayOneShot(unconfirmSound);
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            if (countdownText != null)
                countdownText.text = "";
            Debug.Log("[CharacterSelection] Countdown cancelled by a player.");
        }
    }

    // ========================================================================================= Scene Transition ==================================================================================

    private IEnumerator StartCountdownAndLoadScene()
    {
        audioSource.PlayOneShot(countdownBeepSound);
        float countdown = 3f;
        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = $" {Mathf.CeilToInt(countdown)}...";
            Debug.Log($"Starting in {Mathf.CeilToInt(countdown)}...");
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        if (countdownText != null)
            countdownText.text = "";

        SaveConfirmedPlayersToSO();
        SceneManager.LoadScene("MainScene");
    }

    public void SaveConfirmedPlayersToSO()
    {
        selectionDataSO.Clear();
        for (int i = 0; i < playerSlots.Length; i++)
        {
            var slot = playerSlots[i];
            if (slot != null && slot.IsConfirmed)
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

    