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

    // ===== PRIVATE FIELDS =====
    private bool playerInPlantRange = false;
    private bool playerInFuelRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private bool isSwapping = false;

    // ===== PUBLIC GETTERS =====
    public int GetCurrentPlants() => currentPlants;
    public int GetCurrentFuel() => currentFuel;
    public int GetCurrentPotions() => currentPotions;
    public int GetMaxPotions() => maxPotions;
    public bool IsBrewing() => isBrewing;
    public float GetBrewingProgress() => isBrewing ? 0.5f : 0f; // Placeholder

    // Events
    public System.Action<int> OnPotionBrewed;

    // ================================================= Methods =================================================

    void Update()
    {
        // Handle player input and auto brewing if not swapping
        if (!isSwapping)
        {
            HandlePlayerInput();
            CheckForAutoBrewing();
        }
    }

    private void HandlePlayerInput()
    {
        // Check if player is nearby and presses the deposit button
        if (nearbyPlayerInventory == null || nearbyPlayerInput == null) return;

        var attackAction = nearbyPlayerInput.actions["Attack"];
        if (attackAction == null || !attackAction.WasPressedThisFrame()) return;

        bool deposited = false;

        // Deposit plants
        if (playerInPlantRange && nearbyPlayerInventory.GetFirstItemType() == CollectibleData.ItemType.Moss)
        {
            if (nearbyPlayerInventory.DepositPlantItems(this))
            {
                deposited = true;
                Debug.Log($"[CarPotionsSystem] 🌿 Plants deposited! Current: {currentPlants}/{plantsRequired}");
            }
        }
        // Deposit fuel
        else if (playerInFuelRange && nearbyPlayerInventory.GetFirstItemType() == CollectibleData.ItemType.Diesel)
        {
            if (nearbyPlayerInventory.DepositFuelItems(this))
            {
                deposited = true;
                Debug.Log($"[CarPotionsSystem] ⛽ Fuel deposited! Current: {currentFuel}/{fuelRequired}");
            }
        }

        if (deposited)
        {
            ClearPlayerInteraction();
        }
    }

    private void CheckForAutoBrewing()
    {
        // Automatically start brewing if possible
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

        // Check if player has plants and they are needed
        if (itemType == CollectibleData.ItemType.Moss && currentPlants < plantsRequired)
        {
            playerInPlantRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
        }
        // Check if player has fuel and it is needed
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

    private void ClearPlayerInteraction()
    {
        // Clear player interaction state
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
    }

    public void AddFuel(int amount)
    {
        int prevFuel = currentFuel;
        currentFuel = Mathf.Min(currentFuel + amount, fuelRequired);
    }

    private bool CanBrewPotion()
    {
        // Check if there are enough materials and space for a potion
        return currentPlants >= plantsRequired &&
               currentFuel >= fuelRequired &&
               currentPotions < maxPotions;
    }

    private IEnumerator BrewPotion()
    {
        if (!CanBrewPotion()) yield break;

        isBrewing = true;

        // Consume materials
        currentPlants -= plantsRequired;
        currentFuel -= fuelRequired;

        Debug.Log($"[CarPotionsSystem] 🧪 Started brewing potion! Time: {brewingTime}s");

        yield return new WaitForSeconds(brewingTime);

        // Create potion
        currentPotions++;
        isBrewing = false;

        Debug.Log($"[CarPotionsSystem] ✅ Potion brewed! Total potions: {currentPotions}");

        OnPotionBrewed?.Invoke(currentPotions);
    }
}
