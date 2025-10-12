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
    private PlayerInventory nearbyPlayer;
    private PlayerInput nearbyPlayerInput; // Referencia al Input System

    void Start()
    {
        startPosition = transform.position; // Save the initial position
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if entering object has PlayerInventory
        if (!playerInventory)
        {
            return;
        }
        
        PlayerInput playerInput = other.GetComponent<PlayerInput>(); // Check for PlayerInput
        if (!playerInput)
        {
            Debug.LogWarning($"[WorldCollectible] {other.gameObject.name} no tiene PlayerInput component!");
            return;
        }
        
        if (!playerInventory.CanCarryItem(collectibleData)) // Check if the player can carry more items
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no puede cargar más items");
            return;
        }
        
        // If all checks pass, set the player reference
        nearbyPlayer = playerInventory;
        nearbyPlayerInput = playerInput;
        
        // Add the collectible reference to the player's inventory for interaction
        playerInventory.SetNearbyCollectible(this);
    }

    void OnTriggerExit(Collider other) // To disable the posibility to collect when leaving the area
    {
        if (!other || !other.gameObject) // Null check
            return;
            
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if exiting object has PlayerInventory
        if (playerInventory && playerInventory == nearbyPlayer) // Only clear if it's the same player
        {
            nearbyPlayer = null;
            nearbyPlayerInput = null;
            
            // Remove the collectible reference from the player's inventory
            playerInventory.SetNearbyCollectible(null);
        }
    }

    public void CollectItem() // Method to collect the item (now public)
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