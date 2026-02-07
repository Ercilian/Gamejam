using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


public class PlayerSlotSimple : MonoBehaviour
{
    [Header("UI References")]
    public GameObject idleState;
    public GameObject joinedState;
    public Button confirmButton;
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Player Title")]
    public TMP_Text playerTitleText;
    public Color[] characterColors;

    [Header("Character Preview")]
    public GameObject defaultCharacterPrefab;
    public Transform worldPreviewAnchor;
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEuler = new Vector3(0, 180, 0);
    public float previewScale;
    public TMP_Text characterInfo;

    [Header("Player Stats Data")]
    public PlayerStatsData[] playerStatsDataArray;

    [Header("Slot Management")]
    public int selectedCharacterIndex = 0;
    public CharacterSelectionManager manager;
    public PlayerInput playerInput;

    
//========= PRIVATE VARIABLES ==============
    private bool isConfirmed = false;
    private int slotIndex;
    private bool isJoined = false;
    private GameObject currentPreviewInstance;
    private float joinTime = -1f;

//========= PUBLIC GETTERS ==============
    public bool IsConfirmed => isConfirmed;
    public bool IsJoined => isJoined;
    public int SlotIndex => slotIndex;




    // ========================================================================================= Methods ========================================================================================




    public void Initialize(int index)
    {
        slotIndex = index;
        SetJoinedState(false);
        UpdatePlayerTitleColor(0);
    }


    // ================================================= Slot State Management ===========================================

    public void SetJoinedState(bool joined) // Change the joined state of the slot
    {
        isJoined = joined;
        joinTime = joined ? Time.time : -1f;

        if (idleState) idleState.SetActive(!joined);
        if (joinedState) joinedState.SetActive(joined);


        if (joined)
        {
            if (manager != null && manager.characterPrefabs != null && defaultCharacterPrefab != null)
            {
                int foundIndex = -1;
                for (int i = 0; i < manager.characterPrefabs.Length; i++)
                {
                    if (manager.characterPrefabs[i] == defaultCharacterPrefab)
                    {
                        foundIndex = i;
                        break;
                    }
                }
                if (foundIndex != -1)
                {
                    selectedCharacterIndex = foundIndex;
                }
                else
                {
                    selectedCharacterIndex = 0; // fallback
                }
            }
            SpawnPreview();
            UpdateCharacterDescription(selectedCharacterIndex);
            UpdatePlayerTitleColor(selectedCharacterIndex);
        }
        else
        {
            DespawnPreview();
            if (characterInfo) characterInfo.text = "";
            UpdatePlayerTitleColor(0); // Reset color to default (first character)
        }

    }

    public void ResetSlotState() // Reset the slot to its initial state
    {
        isConfirmed = false;
        selectedCharacterIndex = 0;
        confirmButton.interactable = true;
        leftArrowButton.interactable = true;
        rightArrowButton.interactable = true;
        DespawnPreview();
        if (characterInfo) characterInfo.text = "";
    }

    // ================================================= Preview Management ==============================================

    private void SpawnPreview() // Spawn the default character preview
    {
        if (currentPreviewInstance || !defaultCharacterPrefab) return;
        var anchor = worldPreviewAnchor ? worldPreviewAnchor : transform;
        currentPreviewInstance = Instantiate(defaultCharacterPrefab, anchor);
        currentPreviewInstance.transform.localPosition = previewLocalPosition;
        currentPreviewInstance.transform.localEulerAngles = previewLocalEuler;
        currentPreviewInstance.transform.localScale = Vector3.one * previewScale;
    }

    private void DespawnPreview() // Despawn the current character preview
    {
        if (currentPreviewInstance)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }
    }

    public void ShowCharacterPreview(GameObject prefab) // Show a specific character preview
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }
        if (worldPreviewAnchor != null && prefab != null)
        {
            currentPreviewInstance = Instantiate(prefab, worldPreviewAnchor);
            currentPreviewInstance.transform.localPosition = previewLocalPosition;
            currentPreviewInstance.transform.localEulerAngles = previewLocalEuler;
            currentPreviewInstance.transform.localScale = Vector3.one * previewScale;

            int index = -1;
            if (manager != null && manager.characterPrefabs != null)
            {
                for (int i = 0; i < manager.characterPrefabs.Length; i++)
                {
                    if (manager.characterPrefabs[i] == prefab)
                    {
                        index = i;
                        break;
                    }
                }
            }
            UpdateCharacterDescription(index);
            UpdatePlayerTitleColor(index);
        }
        else
        {
            Debug.LogWarning($"[Slot {slotIndex}] Prefab nulo o anchor no encontrado.");
            if (characterInfo) characterInfo.text = "";
        }
    }

    // ================================================= Character Selection =============================================

    public void ChangeCharacter(int direction, GameObject[] characterPrefabs) // Change the selected character
    {
        selectedCharacterIndex = (selectedCharacterIndex + direction + characterPrefabs.Length) % characterPrefabs.Length;
        Debug.Log($"[Slot {slotIndex}] Cambiando a índice {selectedCharacterIndex}: {characterPrefabs[selectedCharacterIndex]?.name}");
        ShowCharacterPreview(characterPrefabs[selectedCharacterIndex]);
        UpdatePlayerTitleColor(selectedCharacterIndex);
    }
    private void UpdatePlayerTitleColor(int characterIndex)
    {
        if (playerTitleText != null && characterColors != null && characterIndex >= 0 && characterIndex < characterColors.Length)
        {
            playerTitleText.color = characterColors[characterIndex];
        }
    }

    private void UpdateCharacterDescription(int index)
    {
        if (characterInfo == null) return;
        if (playerStatsDataArray != null && index >= 0 && index < playerStatsDataArray.Length && playerStatsDataArray[index] != null)
        {
            characterInfo.text = playerStatsDataArray[index].Description;
        }
        else
        {
            characterInfo.text = "";
        }
    }

    // ================================================= UI Events ======================================================

    public void OnLeftArrowPressed() // Handle left arrow button press
    {
        manager.PlayHoverSound();
        if (isConfirmed) return;
        ChangeCharacter(-1, manager.characterPrefabs);
    }

    public void OnRightArrowPressed() // Handle right arrow button press
    {
        manager.PlayHoverSound();
        if (isConfirmed) return;
        ChangeCharacter(1, manager.characterPrefabs);
    }

    public void OnConfirmPressed() // Handle confirm button press
    {
        if (joinTime > 0 && Time.time - joinTime < 0.2f)
            return;

        if (!isConfirmed)
        {
            // Verificar si el personaje ya está confirmado por otro jugador
            for (int i = 0; i < manager.playerSlots.Length; i++)
            {
                if (i != slotIndex && manager.playerSlots[i].IsConfirmed && manager.playerSlots[i].selectedCharacterIndex == selectedCharacterIndex)
                {
                    // Personaje ya confirmado por otro jugador
                    manager.audioSource.PlayOneShot(manager.errorSound); // Sonido de error
                    return;
                }
            }
            isConfirmed = true;
            confirmButton.interactable = false;
            leftArrowButton.interactable = false;
            rightArrowButton.interactable = false;
            manager.OnPlayerConfirmed();
        }
    }

    public void OnUnconfirmPressed() // Handle unconfirm action
    {
        if (isConfirmed)
        {
            isConfirmed = false;
            confirmButton.interactable = true;
            leftArrowButton.interactable = true;
            rightArrowButton.interactable = true;
            manager.OnPlayerUnconfirmed();
        }
    }


    public void OnLeftArrowPressed(InputAction.CallbackContext ctx) => OnLeftArrowPressed();
    public void OnRightArrowPressed(InputAction.CallbackContext ctx) => OnRightArrowPressed();
    public void OnConfirmPressed(InputAction.CallbackContext ctx) => OnConfirmPressed();
    private void OnDisable() => DespawnPreview();
}