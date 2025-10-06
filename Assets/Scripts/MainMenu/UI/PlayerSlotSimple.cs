using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotSimple : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject idleState;     // Panel que muestra "Press Any Button"
    public GameObject joinedState;   // Panel que muestra "PLAYER X"
    public TMP_Text playerText;      // Texto que muestra el número de jugador

    [Header("Debug")]
    public bool debugLogs = true;
    
    private int slotIndex;
    private bool isJoined = false;

    public void Initialize(int index)
    {
        slotIndex = index;
        SetJoinedState(false);
        
        // Auto-buscar componentes si no están asignados
        if (!idleState || !joinedState)
        {
            AutoFindComponents();
        }
        
        if (debugLogs) 
            Debug.Log($"[Slot {slotIndex}] Inicializado. Idle={idleState != null}, Joined={joinedState != null}");
    }

    public void SetJoinedState(bool joined)
    {
        isJoined = joined;
        
        if (idleState) idleState.SetActive(!joined);
        if (joinedState) joinedState.SetActive(joined);
        
        if (joined && playerText)
        {
            playerText.text = $"PLAYER {slotIndex + 1}";
        }
        
        if (debugLogs)
            Debug.Log($"[Slot {slotIndex}] Estado cambiado a: {(joined ? "JOINED" : "IDLE")}");
    }

    private void AutoFindComponents()
    {
        // Buscar componentes por nombre
        foreach (Transform child in transform)
        {
            string childName = child.name.ToLower();
            
            if (!idleState && childName.Contains("idle"))
                idleState = child.gameObject;
                
            if (!joinedState && (childName.Contains("joined") || childName.Contains("player")))
                joinedState = child.gameObject;
        }
        
        // Buscar texto del jugador
        if (!playerText && joinedState)
        {
            playerText = joinedState.GetComponentInChildren<TMP_Text>();
        }
    }

    public bool IsJoined => isJoined;
    public int SlotIndex => slotIndex;
}