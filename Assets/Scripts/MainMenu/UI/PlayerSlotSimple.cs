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

    [Header("Character Preview")]
    public GameObject defaultCharacterPrefab;
    public Transform worldPreviewAnchor;
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEuler = new Vector3(0, 180, 0);
    public float previewScale = 1f;

    [Header("Slot Management")]
    public int selectedCharacterIndex = 0;
    public CharacterSelectionManager manager;
    public PlayerInput playerInput;

    
//========= PRIVATE VARIABLES ==============
    private bool isConfirmed = false;
    private int slotIndex;
    private bool isJoined = false;
    private GameObject spawnedCharacter;
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
    }


    // ================================================= Slot State Management ===========================================

    public void SetJoinedState(bool joined) // Change the joined state of the slot
    {
        isJoined = joined;
        joinTime = joined ? Time.time : -1f;

        if (idleState) idleState.SetActive(!joined);
        if (joinedState) joinedState.SetActive(joined);

        if (joined) SpawnPreview();
        else DespawnPreview();

        Debug.Log($"[Slot {slotIndex}] Estado cambiado a: {(joined ? "JOINED" : "IDLE")}");
    }

    public void ResetSlotState() // Reset the slot to its initial state
    {
        isConfirmed = false;
        selectedCharacterIndex = 0;
        confirmButton.interactable = true;
        leftArrowButton.interactable = true;
        rightArrowButton.interactable = true;
        DespawnPreview();
    }

    // ================================================= Preview Management ==============================================

    private void SpawnPreview() // Spawn the default character preview
    {
        if (spawnedCharacter || !defaultCharacterPrefab) return;
        var anchor = worldPreviewAnchor ? worldPreviewAnchor : transform;

        spawnedCharacter = Instantiate(defaultCharacterPrefab, anchor);
        spawnedCharacter.transform.localPosition = previewLocalPosition;
        spawnedCharacter.transform.localEulerAngles = previewLocalEuler;
        spawnedCharacter.transform.localScale = Vector3.one * previewScale;
    }

    private void DespawnPreview() // Despawn the current character preview
    {
        if (spawnedCharacter)
        {
            Destroy(spawnedCharacter);
            spawnedCharacter = null;
        }
        if (currentPreviewInstance)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }
    }

    public void ShowCharacterPreview(GameObject prefab) // Show a specific character preview
    {
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);

        if (worldPreviewAnchor != null && prefab != null)
        {
            currentPreviewInstance = Instantiate(prefab, worldPreviewAnchor);
        }
        else
        {
            Debug.LogWarning($"[Slot {slotIndex}] Prefab nulo o anchor no encontrado.");
        }
    }

    // ================================================= Character Selection =============================================

    public void ChangeCharacter(int direction, GameObject[] characterPrefabs) // Change the selected character
    {
        selectedCharacterIndex = (selectedCharacterIndex + direction + characterPrefabs.Length) % characterPrefabs.Length;
        Debug.Log($"[Slot {slotIndex}] Cambiando a índice {selectedCharacterIndex}: {characterPrefabs[selectedCharacterIndex]?.name}");
        ShowCharacterPreview(characterPrefabs[selectedCharacterIndex]);
    }

    // ================================================= UI Events ======================================================

    public void OnLeftArrowPressed() // Handle left arrow button press
    {
        manager.PlayHoverSound();
        Debug.Log($"[Slot {slotIndex}] LeftArrow PRESSED");
        if (isConfirmed) return;
        ChangeCharacter(-1, manager.characterPrefabs);
    }

    public void OnRightArrowPressed() // Handle right arrow button press
    {
        manager.PlayHoverSound();
        Debug.Log($"[Slot {slotIndex}] RightArrow PRESSED");
        if (isConfirmed) return;
        ChangeCharacter(1, manager.characterPrefabs);
    }

    public void OnConfirmPressed() // Handle confirm button press
    {
        if (joinTime > 0 && Time.time - joinTime < 0.2f)
            return;

        if (!isConfirmed)
        {
            isConfirmed = true;
            confirmButton.interactable = false;
            leftArrowButton.interactable = false;
            rightArrowButton.interactable = false;
            Debug.Log($"[Slot {slotIndex}] Selección confirmada.");
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
            Debug.Log($"[Slot {slotIndex}] Selección desconfirmada.");
        }
    }


    public void OnLeftArrowPressed(InputAction.CallbackContext ctx) => OnLeftArrowPressed();
    public void OnRightArrowPressed(InputAction.CallbackContext ctx) => OnRightArrowPressed();
    public void OnConfirmPressed(InputAction.CallbackContext ctx) => OnConfirmPressed();
    private void OnDisable() => DespawnPreview();
}