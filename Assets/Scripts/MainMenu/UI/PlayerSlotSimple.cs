using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotSimple : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject idleState;     // Panel "Press Any Button"
    public GameObject joinedState;   // Panel "PLAYER X"
    public TMP_Text playerText;      // Texto del número de jugador
    public Button confirmButton;
    public Button leftArrowButton;
    public Button rightArrowButton;
    private bool isConfirmed = false;

    [Header("Preview del personaje")]
    public GameObject defaultCharacterPrefab;   // Asigna aquí tu prefab del personaje
    public Transform worldPreviewAnchor;        // Empty en la escena, centrado entre las flechas
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalEuler = new Vector3(0, 180, 0);
    public float previewScale = 1f;

    [Header("Debug")]
    public bool debugLogs = true;

    private int slotIndex;
    private bool isJoined = false;
    private GameObject spawnedCharacter;
    public int selectedCharacterIndex = 0;
    private GameObject currentPreviewInstance;

    private float joinTime = -1f;

    public CharacterSelectionManager manager;

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

        if (joined && playerText) playerText.text = $"PLAYER {slotIndex + 1}";

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
        if (!playerText && joinedState) playerText = joinedState.GetComponentInChildren<TMP_Text>();
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
    }

    public void ChangeCharacter(int direction, GameObject[] characterPrefabs)
    {
        // direction: -1 para izquierda, +1 para derecha
        selectedCharacterIndex = (selectedCharacterIndex + direction + characterPrefabs.Length) % characterPrefabs.Length;
        ShowCharacterPreview(characterPrefabs[selectedCharacterIndex]);
    }

    public void OnLeftArrowPressed()
    {
        if (isConfirmed) return; // No permite cambiar si está confirmado
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
            if (playerText) playerText.text = $"PLAYER {slotIndex + 1} ¡Listo!";
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
            // Vuelve al estado normal
            if (playerText) playerText.text = $"PLAYER {slotIndex + 1}";
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
        if (playerText) playerText.text = $"PLAYER {slotIndex + 1}";
        // Opcional: limpia preview, colores, etc.
    }

}