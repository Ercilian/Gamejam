using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerSlotSimple : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject idleState;
    public GameObject joinedState;
    public Button confirmButton;
    public Button leftArrowButton;
    public Button rightArrowButton;


    [Header("Preview del personaje")]
    public GameObject defaultCharacterPrefab;
    public Transform worldPreviewAnchor;
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEuler = new Vector3(0, 180, 0);
    public float previewScale = 1f;


    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Others")]    
    public int selectedCharacterIndex = 0;
    public CharacterSelectionManager manager;
    
    private bool isConfirmed = false;
    private int slotIndex;
    private bool isJoined = false;
    private GameObject spawnedCharacter;
    private GameObject currentPreviewInstance;
    private float joinTime = -1f;
    public bool IsConfirmed => isConfirmed;
    public bool IsJoined => isJoined;
    public int SlotIndex => slotIndex;

    public void Initialize(int index)
    {
        slotIndex = index;

        // Auto-buscar componentes si no están asignados
        if (!idleState || !joinedState) AutoFindComponents();
        TryAutoFindAnchor();

        SetJoinedState(false);
    }

    public void SetJoinedState(bool joined)
    {
        isJoined = joined;
        joinTime = joined ? Time.time : -1f;

        if (idleState) idleState.SetActive(!joined);
        if (joinedState) joinedState.SetActive(joined);


        if (joined) SpawnPreview();
        else DespawnPreview();

        if (debugLogs)
            Debug.Log($"[Slot {slotIndex}] Estado cambiado a: {(joined ? "JOINED" : "IDLE")}");
    }

    private void SpawnPreview()
    {
        if (spawnedCharacter) return;

        if (!defaultCharacterPrefab)
        {
            if (debugLogs) Debug.LogWarning($"[Slot {slotIndex}] Sin defaultCharacterPrefab asignado.");
            return;
        }

        // Si no hay anchor, intentamos encontrar uno por nombre
        if (!worldPreviewAnchor) TryAutoFindAnchor();
        var anchor = worldPreviewAnchor ? worldPreviewAnchor : transform;

        spawnedCharacter = Instantiate(defaultCharacterPrefab, anchor);
        spawnedCharacter.transform.localPosition = previewLocalPosition;
        spawnedCharacter.transform.localEulerAngles = previewLocalEuler;
        spawnedCharacter.transform.localScale = Vector3.one * previewScale;

    }

    private void DespawnPreview()
    {
        if (spawnedCharacter)
        {
            Destroy(spawnedCharacter);
            spawnedCharacter = null;
        }
    }

    private void AutoFindComponents()
    {
        foreach (Transform child in transform)
        {
            string childName = child.name.ToLower();
            if (!idleState && childName.Contains("idle")) idleState = child.gameObject;
            if (!joinedState && (childName.Contains("joined") || childName.Contains("player"))) joinedState = child.gameObject;
        }
    }

    private void TryAutoFindAnchor()
    {
        // Busca en la escena objetos llamados "PreviewAnchor_X" o "PreviewAnchor (X)"
        var byExact = GameObject.Find($"PreviewAnchor_{slotIndex + 1}");
        if (!byExact) byExact = GameObject.Find($"PreviewAnchor ({slotIndex + 1})");
        if (!byExact) byExact = GameObject.Find($"PreviewAnchor{slotIndex + 1}");
        if (byExact) worldPreviewAnchor = byExact.transform;
    }

    private void OnDisable() => DespawnPreview();

    public void ShowCharacterPreview(GameObject prefab)
    {
        // Destruye el preview anterior si existe
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);

        // Instancia el nuevo preview en el anchor
        if (worldPreviewAnchor != null && prefab != null)
        {
            currentPreviewInstance = Instantiate(prefab, worldPreviewAnchor);
            // Ajusta posición/escala si es necesario
        }
        else
        {
            Debug.LogWarning($"[Slot {slotIndex}] Prefab nulo o anchor no encontrado.");
        }
    }

    public void ChangeCharacter(int direction, GameObject[] characterPrefabs)
    {
        selectedCharacterIndex = (selectedCharacterIndex + direction + characterPrefabs.Length) % characterPrefabs.Length;
        Debug.Log($"[Slot {slotIndex}] Cambiando a índice {selectedCharacterIndex}: {characterPrefabs[selectedCharacterIndex]?.name}");
        ShowCharacterPreview(characterPrefabs[selectedCharacterIndex]);
    }

    public void OnLeftArrowPressed()
    {
        Debug.Log($"[Slot {slotIndex}] LeftArrow PRESSED");
        if (isConfirmed) return;
        if (manager != null)
            ChangeCharacter(-1, manager.characterPrefabs);
    }

    public void OnRightArrowPressed()
    {
        if (isConfirmed) return; // No permite cambiar si está confirmado
        if (manager != null)
            ChangeCharacter(1, manager.characterPrefabs);
    }

    public void OnConfirmPressed()
    {
        // Solo permite confirmar si han pasado al menos 0.2 segundos desde que se unió
        if (joinTime > 0 && Time.time - joinTime < 0.2f)
            return;

        if (!isConfirmed)
        {
            isConfirmed = true;
            confirmButton.interactable = false;
            if (leftArrowButton) leftArrowButton.interactable = false;
            if (rightArrowButton) rightArrowButton.interactable = false;
            // Cambia el color o muestra "Listo"
            if (debugLogs) Debug.Log($"[Slot {slotIndex}] Selección confirmada.");

            if (manager != null)
                manager.OnPlayerConfirmed();
        }
    }

    public void OnUnconfirmPressed()
    {
        if (isConfirmed)
        {
            isConfirmed = false;
            confirmButton.interactable = true;
            if (leftArrowButton) leftArrowButton.interactable = true;
            if (rightArrowButton) rightArrowButton.interactable = true;
            manager.OnPlayerUnconfirmed();
            // Vuelve al estado normal
            if (debugLogs) Debug.Log($"[Slot {slotIndex}] Selección desconfirmada.");
        }
    }

    public void ResetSlotState()
    {
        isConfirmed = false;
        selectedCharacterIndex = 0;
        if (confirmButton) confirmButton.interactable = true;
        if (leftArrowButton) leftArrowButton.interactable = true;
        if (rightArrowButton) rightArrowButton.interactable = true;
        // Opcional: limpia preview, colores, etc.
    }

    public void OnLeftArrowPressed(InputAction.CallbackContext ctx)
    {
        OnLeftArrowPressed();
    }

    public void OnRightArrowPressed(InputAction.CallbackContext ctx)
    {
        OnRightArrowPressed();
    }

    public void OnConfirmPressed(InputAction.CallbackContext ctx)
    {
        OnConfirmPressed();
    }

}