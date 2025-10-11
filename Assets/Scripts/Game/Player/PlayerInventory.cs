using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Configuración")]
    public int maxCarryCapacity = 3;
    public Transform itemHoldPoint; //Empty where items are visually held on the players (need to change to the hands)
    public float itemStackOffset = 0.3f; // Offset to objects in the stack
    
    private List<CollectibleData> carriedItems = new List<CollectibleData>(); //List of carried items
    private List<GameObject> visualItems = new List<GameObject>(); // List of instantiated visual items
    private PlayerInput playerInput; // Reference to PlayerInput component
    private WorldCollectible nearbyCollectible; // Reference to nearby collectible




    // ================================================= Methods =================================================

    void Start()
    {
        playerInput = GetComponent<PlayerInput>(); // Get PlayerInput reference
        
        // Subscribe to the Crouch action event
        if (playerInput != null)
        {
            var crouchAction = playerInput.actions["Crouch"];
            if (crouchAction != null)
            {
                crouchAction.performed += OnCrouchPressed;
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerInput != null)
        {
            var crouchAction = playerInput.actions["Crouch"];
            if (crouchAction != null)
            {
                crouchAction.performed -= OnCrouchPressed;
            }
        }
    }

    void Update()
    {
        // Handle interact manually (reliable method)
        if (playerInput != null && nearbyCollectible != null)
        {
            var interactAction = playerInput.actions["Interact"];
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                nearbyCollectible.CollectItem();
            }
        }
    }



    void OnCrouchPressed(InputAction.CallbackContext context)
    {
        Debug.Log($"[PlayerInventory] 🐅 OnCrouchPressed called! carriedItems.Count: {carriedItems.Count}");
        
        if (carriedItems.Count > 0)
        {
            DropItems();
        }
    }



    public void SetNearbyCollectible(WorldCollectible collectible)
    {
        nearbyCollectible = collectible;
    }

    public bool CanCarryItem(CollectibleData item) // Boolean to check if the player can carry the item
    {
        if (carriedItems.Count == 0) return true; // if no items, can carry any item
        return carriedItems.Count < maxCarryCapacity && item.type == carriedItems[0].type; // if has items, can only carry same type and not exceed capacity
    }

    public void PickupItem(CollectibleData item) // Method to pick up an item
    {
        if (!CanCarryItem(item)) return; // Check if can carry the item
        carriedItems.Add(item); // Add item to the carried list
        CreateVisualItem(item); // Create the visual representation of the item
        
        Debug.Log($"[PlayerInventory] Recogido {item.itemName}. Total: {carriedItems.Count}/{maxCarryCapacity}");
    }

    void CreateVisualItem(CollectibleData item) // Method to create the visual representation of the item
    {
        if (!item.itemPrefab || !itemHoldPoint) return; // Check if prefab and hold point are assigned
        GameObject visualItem = Instantiate(item.itemPrefab, itemHoldPoint); // Instantiate the item prefab
        Vector3 stackPosition = Vector3.up * (visualItems.Count * itemStackOffset); // Put the item in the stack
        visualItem.transform.localPosition = stackPosition;
        
        Collider itemCollider = visualItem.GetComponent<Collider>(); // Desactivating collider
        if (itemCollider) itemCollider.enabled = false;
        
        WorldCollectible worldScript = visualItem.GetComponent<WorldCollectible>(); // Remove WorldCollectible script to avoid conflicts
        if (worldScript) Destroy(worldScript);
        
        visualItems.Add(visualItem); // Add to the visual items list
    }

    private bool DepositItemsByType(CollectibleData.ItemType itemType, System.Action<int> onDeposit) // Generic method to deposit items of a specific type
    {
        if (carriedItems.Count == 0) return false; // No items to deposit
        int totalValue = 0;
        for (int i = carriedItems.Count - 1; i >= 0; i--) // Iterate backwards to safely remove items
        {
            if (carriedItems[i].type == itemType) // Check if the item is of the specified type
            {
                totalValue += GetItemValue(carriedItems[i]); // Accumulate the value
                if (visualItems.Count > i && visualItems[i] != null) //Destroy visual item if exists
                    Destroy(visualItems[i]);
                if (visualItems.Count > i)
                    visualItems.RemoveAt(i);
                carriedItems.RemoveAt(i); // Remove item from the carried list
            }
        }
        if (totalValue > 0) // If any items were deposited
        {
            onDeposit(totalValue); // Call the provided action with the total value
            return true;
        }
        return false;
    }

    private int GetItemValue(CollectibleData item) // Get the value of a specific item based on its type
    {
        return item.type switch
        {
            CollectibleData.ItemType.Diesel => item.dieselValue,
            CollectibleData.ItemType.Scrap => item.scrapValue,
            CollectibleData.ItemType.Moss => item.mossValue,
            _ => 0
        };
    }

    public bool DepositDieselItems(CarFuelSystem carFuelSystem) // Specific method to deposit diesel items
    {
        return DepositItemsByType(CollectibleData.ItemType.Diesel, 
            value => carFuelSystem.AddDiesel(value));
    }

    public bool DepositScrapItems(CarScrapSystem carScrapSystem) // Specific method to deposit scrap items
    {
        return DepositItemsByType(CollectibleData.ItemType.Scrap, 
            value => carScrapSystem.AddScrap(value));
    }

    public bool DepositPlantItems(CarPotionsSystem carPotionsSystem) // Specific method to deposit plant/moss items
    {
        return DepositItemsByType(CollectibleData.ItemType.Moss, 
            value => carPotionsSystem.AddPlants(value));
    }

    public bool DepositFuelItems(CarPotionsSystem carPotionsSystem) // Specific method to deposit fuel items for potions
    {
        return DepositItemsByType(CollectibleData.ItemType.Diesel, 
            value => carPotionsSystem.AddFuel(value));
    }

    public void DropItems() // Method to drop all carried items to the ground
    {
        Debug.Log($"[PlayerInventory] DropItems called! carriedItems.Count: {carriedItems.Count}");
        if (carriedItems.Count == 0) 
        {
            Debug.Log("[PlayerInventory] No items to drop!");
            return;
        }

        Vector3 dropPosition = transform.position + transform.forward * 1.5f; // Drop items in front of player
        
        for (int i = 0; i < carriedItems.Count; i++)
        {
            CollectibleData item = carriedItems[i];
            
            // Create world collectible object
            if (item.itemPrefab != null)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.8f, 0.8f), 
                    0.1f, 
                    Random.Range(-0.8f, 0.8f)
                ); // Add some randomness to avoid stacking
                
                GameObject droppedItem = Instantiate(item.itemPrefab, dropPosition + randomOffset, Quaternion.identity);
                
                // Ensure the dropped item has a WorldCollectible component
                WorldCollectible worldCollectible = droppedItem.GetComponent<WorldCollectible>();
                if (worldCollectible == null)
                {
                    worldCollectible = droppedItem.AddComponent<WorldCollectible>();
                }
                worldCollectible.collectibleData = item;
                
                // Enable collider for pickup
                Collider itemCollider = droppedItem.GetComponent<Collider>();
                if (itemCollider) itemCollider.enabled = true;
            }
            
            // Destroy visual item
            if (i < visualItems.Count && visualItems[i] != null)
            {
                Destroy(visualItems[i]);
            }
        }
        
        // Clear inventory
        carriedItems.Clear();
        visualItems.Clear();
        
        Debug.Log($"[PlayerInventory] Items dropped to the ground!");
    }

    void ClearInventory() // Method to clear the inventory (used on player death or similar) (NEED TO CHANGE THIS)
    {
        carriedItems.Clear();
        foreach (var visualItem in visualItems)
        {
            if (visualItem) Destroy(visualItem);
        }
        visualItems.Clear();
    }


    public int GetCarriedItemCount() => carriedItems.Count; // Get the number of carried items
    public bool HasItems() => carriedItems.Count > 0; // Check if the player has any items
    public CollectibleData.ItemType GetFirstItemType() // Get the type of the first item (all items are the same type)
    {
        return carriedItems.Count > 0 ? carriedItems[0].type : CollectibleData.ItemType.Diesel;
    }



}