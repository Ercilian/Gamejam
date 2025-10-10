using UnityEngine;
using UnityEngine.InputSystem;

public class WorldCollectible : MonoBehaviour
{
    [Header("Configuración")]
    public CollectibleData collectibleData;
    
    [Header("Efectos")]
    public ParticleSystem collectEffect;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private Vector3 startPosition;
    private bool playerInRange = false;
    private PlayerInventory nearbyPlayer;
    private PlayerInput nearbyPlayerInput; // Referencia al Input System

    void Start()
    {
        startPosition = transform.position; // Save the initial position
    }

    void Update()
    {
        if (playerInRange && nearbyPlayer && nearbyPlayerInput) // Both player and input must be valid
        {
            var interactAction = nearbyPlayerInput.actions["Interact"]; // Ensure this matches your Input Action name
            if (interactAction != null && interactAction.WasPressedThisFrame()) // Check if the action was pressed this frame
            {
                CollectItem();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if entering object has PlayerInventory
        if (!playerInventory)
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no es un jugador (sin PlayerInventory)");
            return;
        }
        
        PlayerInput playerInput = other.GetComponent<PlayerInput>(); // Check for PlayerInput
        if (!playerInput)
        {
            Debug.LogWarning($"[WorldCollectible] {other.gameObject.name} no tiene PlayerInput component!");
            return;
        }
        
        var interactAction = playerInput.actions["Interact"]; // Check for Interact action
        if (interactAction == null)
        {
            Debug.LogWarning($"[WorldCollectible] {other.gameObject.name} no tiene acción 'Interact' configurada!");
            return;
        }
        
        if (!playerInventory.CanCarryItem(collectibleData)) // Check if the player can carry more items
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no puede cargar más items");
            return;
        }
        
        // If all checks pass, set the player in range to be able to carry the item
        playerInRange = true;
        nearbyPlayer = playerInventory;
        nearbyPlayerInput = playerInput;
    }

    void OnTriggerExit(Collider other) // To disable the posibility to collect when leaving the area
    {
        if (!other || !other.gameObject) // Null check
            return;
            
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if exiting object has PlayerInventory
        if (playerInventory && playerInventory == nearbyPlayer) // Only clear if it's the same player
        {
            playerInRange = false;
            nearbyPlayer = null;
            nearbyPlayerInput = null;
        }
    }

    void CollectItem() // Method to collect the item
    {       
        if (nearbyPlayer.CanCarryItem(collectibleData)) // Double-check if the player can carry the item
        {
            nearbyPlayer.PickupItem(collectibleData); // Add item to the player's inventory
            
            // Effects
            if (collectEffect) 
            {
                collectEffect.Play();
            }
            if (collectibleData.collectSound) 
            {
                AudioSource.PlayClipAtPoint(collectibleData.collectSound, transform.position);
            }            
            Destroy(gameObject, collectEffect ? 0.5f : 0.1f); // Destroy the item after a short delay to allow effects to play
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[WorldCollectible] ⚠️ El jugador no puede cargar más items");
        }
    }
}