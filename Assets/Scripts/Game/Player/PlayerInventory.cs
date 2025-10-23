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
    public List<PotionData> potions = new List<PotionData>(); // Las pociones que tienes
    public PotionData defaultHealPotion; // <-- Asigna aquí tu poción de curación en el inspector

    public EntityStats entityStats; // Asigna en el inspector




    // ================================================= Methods =================================================




    private void Awake()
    {
        if (entityStats == null)
            entityStats = GetComponent<EntityStats>();
    }

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

        if (potions.Count == 0 && defaultHealPotion != null)
        {
            for (int i = 0; i < maxPotions; i++)
            {
                potions.Add(defaultHealPotion);
            }
            UpdatePotionUI();
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

    private bool 
    DepositItemsByType(CollectibleData.ItemType itemType, System.Action<int> onDeposit) // Generic method to deposit items of a specific type
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
            CollectibleData.ItemType.PlantRed => item.mossValue,
            CollectibleData.ItemType.PlantGreen => item.mossValue,
            CollectibleData.ItemType.PlantBlue => item.mossValue,
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
        if (carriedItems.Count == 0) return false;

        // Contadores para cada tipo
        int red = 0, green = 0, blue = 0;

        // Cuenta y elimina cada tipo de planta
        for (int i = carriedItems.Count - 1; i >= 0; i--)
        {
            var type = carriedItems[i].type;
            if (type == CollectibleData.ItemType.PlantRed ||
                type == CollectibleData.ItemType.PlantGreen ||
                type == CollectibleData.ItemType.PlantBlue)
            {
                if (type == CollectibleData.ItemType.PlantRed) red += GetItemValue(carriedItems[i]);
                if (type == CollectibleData.ItemType.PlantGreen) green += GetItemValue(carriedItems[i]);
                if (type == CollectibleData.ItemType.PlantBlue) blue += GetItemValue(carriedItems[i]);

                if (visualItems.Count > i && visualItems[i] != null)
                    Destroy(visualItems[i]);
                if (visualItems.Count > i)
                    visualItems.RemoveAt(i);
                carriedItems.RemoveAt(i);
            }
        }

        // Deposita cada tipo por separado
        if (red > 0) carPotionsSystem.AddIngredient(CollectibleData.ItemType.PlantRed, red);
        if (green > 0) carPotionsSystem.AddIngredient(CollectibleData.ItemType.PlantGreen, green);
        if (blue > 0) carPotionsSystem.AddIngredient(CollectibleData.ItemType.PlantBlue, blue);

        return (red + green + blue) > 0;
    }

    public bool DepositFuelItems(CarPotionsSystem carPotionsSystem) // Specific method to deposit fuel items for potions
    {
        return DepositItemsByType(CollectibleData.ItemType.Diesel,
            value => carPotionsSystem.AddFuel(value));
    }

    public void DropItems()
    {
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


    public bool AddPotion(PotionData potion) // Method to add a potion
    {
        if (potions.Count < maxPotions)
        {
            potions.Add(potion);
            UpdatePotionUI();
            return true;
        }
        return false;
    }

    public bool UsePotion() // Method to use a potion
    {
        if (potions.Count == 0) return false;

        PotionData potion = potions[0];
        potions.RemoveAt(0);

        ApplyPotionEffect(potion);
        UpdatePotionUI();
        return true;
    }

    // Aplica el efecto de la poción
    private void ApplyPotionEffect(PotionData potion)
    {
        switch (potion.effectType)
        {
            case PotionEffectType.Heal:
                entityStats.Heal(potion.effectAmount);
                break;
            case PotionEffectType.Shield:
                entityStats.AddShield(potion.effectAmount); // Implementa este método en EntityStats
                break;
            case PotionEffectType.DamageBoost:
                StartCoroutine(entityStats.DamageBoost(potion.effectAmount, potion.duration)); // Implementa este método en EntityStats
                break;
        }
    }

    public void UpdatePotionUI()
    {
        // Actualiza la interfaz según potions.Count
    }

    // Método para depositar varios tipos de ítems a la vez (por ejemplo, todas las plantas)
    private bool DepositItemsByTypes(CollectibleData.ItemType[] itemTypes, System.Action<int> onDeposit)
    {
        if (carriedItems.Count == 0) return false;
        int totalValue = 0;
        for (int i = carriedItems.Count - 1; i >= 0; i--)
        {
            foreach (var type in itemTypes)
            {
                if (carriedItems[i].type == type)
                {
                    totalValue += GetItemValue(carriedItems[i]);
                    if (visualItems.Count > i && visualItems[i] != null)
                        Destroy(visualItems[i]);
                    if (visualItems.Count > i)
                        visualItems.RemoveAt(i);
                    carriedItems.RemoveAt(i);
                    break; // Sale del foreach para evitar doble eliminación
                }
            }
        }
        if (totalValue > 0)
        {
            onDeposit(totalValue);
            return true;
        }
        return false;
    }
}