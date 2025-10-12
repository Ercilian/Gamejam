using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CarPotionsSystem : MonoBehaviour, ISwappable
{
    [Header("Materials Required")]
    public int plantsRequired = 4;
    public int fuelRequired = 1;
    
    [Header("Current Materials")]
    public int currentPlants = 0;
    public int currentFuel = 0;
    
    [Header("Potions")]
    public int currentPotions = 0;
    public int maxPotions = 1;
    
    [Header("Brewing")]
    public float brewingTime = 10f;
    public bool isBrewing = false;
    
    [Header("Deposit Points")]
    public Transform plantDepositPoint;
    public Transform fuelDepositPoint;
    public string plantDepositPrompt = "Press 'Attack' to deposit plants";
    public string fuelDepositPrompt = "Press 'Attack' to deposit fuel";
    public string brewingPrompt = "Brewing potion...";
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool playerInPlantRange = false;
    private bool playerInFuelRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private bool isSwapping = false;

    // ================================================= Methods =================================================

    void Update()
    {
        if (!isSwapping)
        {
            HandlePlayerInput();
            CheckForAutoBrewing();
        }
    }

    void HandlePlayerInput()
    {
        if (nearbyPlayerInventory == null || nearbyPlayerInput == null) return;

        var attackAction = nearbyPlayerInput.actions["Attack"];
        if (attackAction == null || !attackAction.WasPressedThisFrame()) return;

        bool deposited = false;

        // Handle plant deposits
        if (playerInPlantRange && nearbyPlayerInventory.GetFirstItemType() == CollectibleData.ItemType.Moss)
        {
            if (nearbyPlayerInventory.DepositPlantItems(this))
            {
                deposited = true;
                if (showDebugLogs)
                    Debug.Log($"[CarPotionsSystem] 🌿 Plants deposited! Current: {currentPlants}/{plantsRequired}");
            }
        }
        // Handle fuel deposits
        else if (playerInFuelRange && nearbyPlayerInventory.GetFirstItemType() == CollectibleData.ItemType.Diesel)
        {
            if (nearbyPlayerInventory.DepositFuelItems(this))
            {
                deposited = true;
                if (showDebugLogs)
                    Debug.Log($"[CarPotionsSystem] ⛽ Fuel deposited! Current: {currentFuel}/{fuelRequired}");
            }
        }

        if (deposited)
        {
            ClearPlayerInteraction();
        }
    }

    void CheckForAutoBrewing()
    {
        if (!isBrewing && CanBrewPotion())
        {
            StartCoroutine(BrewPotion());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isSwapping) return;
        
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null || !playerInventory.HasItems()) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;
        
        var attackAction = playerInput.actions["Attack"];
        if (attackAction == null) return;

        var itemType = playerInventory.GetFirstItemType();
        
        // Check if player has plants (moss) and we need plants
        if (itemType == CollectibleData.ItemType.Moss && currentPlants < plantsRequired)
        {
            playerInPlantRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
        }
        // Check if player has fuel and we need fuel
        else if (itemType == CollectibleData.ItemType.Diesel && currentFuel < fuelRequired)
        {
            playerInFuelRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory == nearbyPlayerInventory)
        {
            ClearPlayerInteraction();
        }
    }

    void ClearPlayerInteraction()
    {
        playerInPlantRange = false;
        playerInFuelRange = false;
        nearbyPlayerInventory = null;
        nearbyPlayerInput = null;
    }

    // ============== ISwappable Implementation ==============
    public void OnSwapStarted()
    {
        isSwapping = true;
        ClearPlayerInteraction();
    }

    public void OnSwapCompleted()
    {
        isSwapping = false;
    }

    public void AddPlants(int amount)
    {
        int prevPlants = currentPlants;
        currentPlants = Mathf.Min(currentPlants + amount, plantsRequired);
        
        if (showDebugLogs)
            Debug.Log($"[CarPotionsSystem] 🌿 +{amount} plants ({prevPlants} → {currentPlants})");
    }

    public void AddFuel(int amount)
    {
        int prevFuel = currentFuel;
        currentFuel = Mathf.Min(currentFuel + amount, fuelRequired);
        
        if (showDebugLogs)
            Debug.Log($"[CarPotionsSystem] ⛽ +{amount} fuel ({prevFuel} → {currentFuel})");
    }

    bool CanBrewPotion()
    {
        return currentPlants >= plantsRequired && 
               currentFuel >= fuelRequired && 
               currentPotions < maxPotions;
    }

    IEnumerator BrewPotion()
    {
        if (!CanBrewPotion()) yield break;

        isBrewing = true;
        
        // Consume materials
        currentPlants -= plantsRequired;
        currentFuel -= fuelRequired;
        
        if (showDebugLogs)
            Debug.Log($"[CarPotionsSystem] 🧪 Started brewing potion! Time: {brewingTime}s");
        
        yield return new WaitForSeconds(brewingTime);
        
        // Create potion
        currentPotions++;
        isBrewing = false;
        
        if (showDebugLogs)
            Debug.Log($"[CarPotionsSystem] ✅ Potion brewed! Total potions: {currentPotions}");
        
        OnPotionBrewed?.Invoke(currentPotions);
    }

    // Public getters
    public int GetCurrentPlants() => currentPlants;
    public int GetCurrentFuel() => currentFuel;
    public int GetCurrentPotions() => currentPotions;
    public int GetMaxPotions() => maxPotions;
    public bool IsBrewing() => isBrewing;
    public float GetBrewingProgress()
    {
        return isBrewing ? 0.5f : 0f; // Placeholder
    }

    // Events
    public System.Action<int> OnPotionBrewed;
}
