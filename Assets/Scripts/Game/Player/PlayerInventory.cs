using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Configuración")]
    public int maxCarryCapacity = 3;
    public Transform itemHoldPoint; //Empty where items are visually held on the players (need to change to the hands)
    public float itemStackOffset = 0.3f; // Offset to objects in the stack
    
    private List<CollectibleData> carriedItems = new List<CollectibleData>(); //List of carried items
    private List<GameObject> visualItems = new List<GameObject>(); // List of instantiated visual items




    // ================================================= Methods =================================================




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