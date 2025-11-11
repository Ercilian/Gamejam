using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotSimple : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject idleState;     // Panel "Press Any Button"
    public GameObject joinedState;   // Panel "PLAYER X"
    public TMP_Text playerText;      // Texto del número de jugador

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

        if (debugLogs)
            Debug.Log($"[Slot {slotIndex}] Personaje instanciado en {(anchor == transform ? "slotTransform" : anchor.name)}");
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

    public bool IsJoined => isJoined;
    public int SlotIndex => slotIndex;
}