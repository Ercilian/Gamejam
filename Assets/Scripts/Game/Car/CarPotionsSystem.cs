using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class CarPotionsSystem : MonoBehaviour, ISwappable
{
    [Header("Materials Required")]
    public int plantsRequired = 2;
    public int fuelRequired = 1;

    [Header("Current Materials")]
    public int currentPlants = 0;
    public int currentFuel = 0;
    public int currentGreen = 0;
    public int currentBlue = 0;
    public int currentDiesel = 0;

    [Header("Potions")]
    public int currentPotions = 0;
    public int maxPotions = 1;

    [Header("Potion Recipes")]
    public List<PotionRecipe> potionRecipes = new List<PotionRecipe>(); // Lista de recetas de pociones

    [Header("Deposit Points")]
    public Transform plantDepositPoint;
    public Transform fuelDepositPoint;
    public string plantDepositPrompt = "Press 'Attack' to deposit plants";
    public string fuelDepositPrompt = "Press 'Attack' to deposit fuel";
    public string brewingPrompt = "Brewing potion...";

    [Header("Potion Creation")]
    public float brewingTime = 10f;
    public bool isBrewing = false;
    public PotionData healPotionData; // ScriptableObject de la poción de curación
    public Transform potionSpawnPoint; // Punto donde aparecerá la poción
    
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
        currentDiesel -= 1; // O los que pida la receta
        currentGreen -= 1;
        currentBlue -= 0;

        Debug.Log($"[CarPotionsSystem] 🧪 Started brewing potion! Time: {brewingTime}s");

        yield return new WaitForSeconds(brewingTime);

        var collectible = GetComponent<WorldCollectible>();
        var potion = GetPotionFromIngredients();
        if (collectible && potion)
        {
            collectible.potionData = potion;
            collectible.collectibleData = null;
        }

        // Limpia los ingredientes usados
        currentDiesel = 0;
        currentGreen = 0;
        currentBlue = 0;

        isBrewing = false;
        Debug.Log($"[CarPotionsSystem] ✅ Potion brewed and ready to collect!");

        OnPotionBrewed?.Invoke(currentPotions);
    }

    private PotionData GetPotionFromIngredients()
    {
        foreach (var recipe in potionRecipes)
        {
            if (currentDiesel == recipe.requiredDiesel &&
                currentGreen == recipe.requiredGreen &&
                currentBlue == recipe.requiredBlue)
            {
                return recipe.resultPotion;
            }
        }
        return null; // No hay receta válida
    }
}
