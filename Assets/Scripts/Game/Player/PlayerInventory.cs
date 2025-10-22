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

    [Header("Pociones")]
    public int maxPotions = 2;
    public int currentPotions = 2;
    public int potionHealthRestore = 25;





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

    }

    void CreateVisualItem(CollectibleData item)
    {
        if (!item.itemPrefab || !itemHoldPoint) return;
        GameObject visualItem = Instantiate(item.itemPrefab);

        // Desactivar collider ANTES de hacer parenting o mover el objeto
        Collider itemCollider = visualItem.GetComponent<Collider>();
        if (itemCollider) itemCollider.enabled = false;

        visualItem.transform.SetParent(itemHoldPoint);
        Vector3 stackPosition = Vector3.up * (visualItems.Count * itemStackOffset);
        visualItem.transform.localPosition = stackPosition;

        // Desactivar física mientras lo llevas
        Rigidbody rb = visualItem.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        WorldCollectible worldScript = visualItem.GetComponent<WorldCollectible>();
        if (worldScript) Destroy(worldScript);

        visualItems.Add(visualItem);
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

    public void DropItems()
    {
        Debug.Log($"[PlayerInventory] DropItems called! carriedItems.Count: {carriedItems.Count}");
        if (carriedItems.Count == 0)
        {
            Debug.Log("[PlayerInventory] No items to drop!");
            return;
        }

        Vector3 dropPosition = transform.position + transform.forward * 1.5f;

        for (int i = 0; i < carriedItems.Count; i++)
        {
            CollectibleData item = carriedItems[i];

            if (item.itemPrefab != null)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.8f, 0.8f),
                    0.1f,
                    Random.Range(-0.8f, 0.8f)
                );

                GameObject droppedItem = Instantiate(item.itemPrefab, dropPosition + randomOffset, Quaternion.identity);

                // Activar física al soltar
                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                WorldCollectible worldCollectible = droppedItem.GetComponent<WorldCollectible>();
                if (worldCollectible == null)
                {
                    worldCollectible = droppedItem.AddComponent<WorldCollectible>();
                }
                worldCollectible.collectibleData = item;

                Collider itemCollider = droppedItem.GetComponent<Collider>();
                if (itemCollider) itemCollider.enabled = true;
            }

            if (i < visualItems.Count && visualItems[i] != null)
            {
                Destroy(visualItems[i]);
            }
        }

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


    public bool AddPotion() // Method to add a potion
    {
        if (currentPotions >= maxPotions)
        {
            Debug.Log("[PlayerInventory] No puedes llevar más pociones.");
            return false;
        }
        currentPotions++;
        Debug.Log($"[PlayerInventory] Poción añadida. Pociones actuales: {currentPotions}/{maxPotions}");
        return true;
    }

    public bool UsePotion() // Method to use a potion
    {
        if (currentPotions <= 0)
        {
            Debug.Log("[PlayerInventory] No tienes pociones para usar.");
            return false;
        }
        currentPotions--;
        Debug.Log($"[PlayerInventory] Poción usada. Pociones restantes: {currentPotions}/{maxPotions}");
        return true;
    }


}