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
    public int currentGreen = 0;
    public int currentRed = 0;
    public int currentBlue = 0;
    public int currentDiesel = 0;

    [Header("Potions")]
    public int currentPotions = 0;
    public int maxPotions = 1;

    [Header("Potion Recipes")]
    public List<PotionRecipe> potionRecipes = new List<PotionRecipe>();

    [Header("Deposit Points")]
    public Transform plantDepositPoint;
    public Transform fuelDepositPoint;
    public string plantDepositPrompt = "Press 'Attack' to deposit plants";
    public string fuelDepositPrompt = "Press 'Attack' to deposit fuel";
    public string brewingPrompt = "Brewing potion...";

    [Header("Potion Creation")]
    public float brewingTime = 10f;
    public bool isBrewing = false;
    public PotionData healPotionData;
    public Transform potionSpawnPoint;

    [Header("Audio")]
    public AudioClip brewPotionSound;
    private AudioSource audioSource;
    
    // ===== PRIVATE FIELDS =====
    private bool playerInPlantRange = false;
    private bool playerInFuelRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private bool isSwapping = false;
    private PotionData brewedPotion = null;

    // ===== PUBLIC GETTERS =====
    public int GetCurrentPotions() => currentPotions;
    public int GetMaxPotions() => maxPotions;
    public bool IsBrewing() => isBrewing;
    public float GetBrewingProgress() => isBrewing ? 0.5f : 0f;

    // ===== EVENTS =====
    public System.Action<int> OnPotionBrewed;




    // ================================================= Methods =================================================




    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Handle player input and auto brewing if not swapping
        if (!isSwapping)
        {
            HandlePlayerInput();
            CheckForAutoBrewing();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isSwapping) return;

        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        // Guarda la referencia siempre
        nearbyPlayerInventory = playerInventory;
        nearbyPlayerInput = playerInput;

        // Ahora solo marcas los rangos si lleva algo
        if (playerInventory.HasItems())
        {
            var itemType = playerInventory.GetFirstItemType();
            if (itemType == CollectibleData.ItemType.PlantRed ||
                itemType == CollectibleData.ItemType.PlantGreen ||
                itemType == CollectibleData.ItemType.PlantBlue)
            {
                playerInPlantRange = true;
            }
            else if (itemType == CollectibleData.ItemType.Diesel && currentDiesel < fuelRequired)
            {
                playerInFuelRange = true;
            }
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

    private void HandlePlayerInput()
    {
        if (nearbyPlayerInventory == null || nearbyPlayerInput == null) return;

        var interactAction = nearbyPlayerInput.actions["Interact"];
        if (interactAction == null || !interactAction.WasPressedThisFrame()) return;

        bool deposited = false;

        // Pick up potion if available (prioridad)
        if (brewedPotion != null && currentPotions > 0)
        {
            nearbyPlayerInventory.AddPotion(brewedPotion);
            Debug.Log($"[CarPotionsSystem] Player picked up potion: {brewedPotion.potionName}");
            brewedPotion = null;
            currentPotions = 0;
            deposited = true;
        }
        // Deposit items only if player has something
        else if (nearbyPlayerInventory.HasItems())
        {
            var firstItemType = nearbyPlayerInventory.GetFirstItemType();
            if (playerInPlantRange && (firstItemType == CollectibleData.ItemType.PlantRed || firstItemType == CollectibleData.ItemType.PlantGreen || firstItemType == CollectibleData.ItemType.PlantBlue))
            {
                if (nearbyPlayerInventory.DepositPlantItems(this))
                {
                    deposited = true;
                    Debug.Log($"[CarPotionsSystem] 🌿 Plants deposited! Green: {currentGreen}, Red: {currentRed}, Blue: {currentBlue}");
                }
            }
            else if (playerInFuelRange && firstItemType == CollectibleData.ItemType.Diesel)
            {
                if (nearbyPlayerInventory.DepositFuelItems(this))
                {
                    deposited = true;
                }
            }
        }

        if (deposited)
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

    // ============== POTION TABLE MANAGEMENT METHODS ==============

    public void AddFuel(int amount, CollectibleData data = null)
    {
        int prevDiesel = currentDiesel;
        currentDiesel = Mathf.Min(currentDiesel + amount, fuelRequired);

        // Sonido de depósito
        if (data != null && data.depositSound != null)
            audioSource.PlayOneShot(data.depositSound);

        Debug.Log($"[CarPotionsSystem] ⛽ Fuel deposited! Current: {currentDiesel}/{fuelRequired}");
        CheckForAutoBrewing();
    }

    public void AddIngredient(CollectibleData.ItemType type, int amount, CollectibleData data = null)
    {
        // Sonido de depósito
        if (data != null && data.depositSound != null)
            audioSource.PlayOneShot(data.depositSound);

        switch(type)
        {
            case CollectibleData.ItemType.Diesel:
                currentDiesel += amount;
                break;
            case CollectibleData.ItemType.PlantGreen:
                currentGreen += amount;
                break;
            case CollectibleData.ItemType.PlantRed:
                currentRed += amount;
                break;
            case CollectibleData.ItemType.PlantBlue:
                currentBlue += amount;
                break;
        }
        CheckForAutoBrewing();
    }

    private void CheckForAutoBrewing()
    {
        int totalPlants = currentGreen + currentRed + currentBlue;
        if (!isBrewing && currentDiesel >= 1 && totalPlants >= 2 && currentPotions < maxPotions)
        {
            PotionData potion = GetPotionFromIngredients();
            if (potion != null)
            {
                StartCoroutine(BrewPotion(potion));
            }
        }
    }

    private PotionData GetPotionFromIngredients()
    {
        Debug.Log($"Total recetas: {potionRecipes.Count}");
        foreach (var recipe in potionRecipes)
        {
            Debug.Log($"Comparando: Diesel {currentDiesel}/{recipe.requiredDiesel}, Green {currentGreen}/{recipe.requiredGreen}, Red {currentRed}/{recipe.requiredRed}, Blue {currentBlue}/{recipe.requiredBlue}");
            if (currentDiesel == recipe.requiredDiesel &&
                currentGreen == recipe.requiredGreen &&
                currentRed == recipe.requiredRed &&
                currentBlue == recipe.requiredBlue)
            {
                Debug.Log("¡Receta encontrada!");
                return recipe.resultPotion;
            }
        }
        Debug.Log("No hay receta válida para estos ingredientes.");
        return null;
    }

    private IEnumerator BrewPotion(PotionData potion)
    {
        audioSource.PlayOneShot(brewPotionSound);
        isBrewing = true;
        Debug.Log("Brewing...");

        yield return new WaitForSeconds(brewingTime);

        Debug.Log($"Potion brewed: {potion.potionName}");

        brewedPotion = potion;
        currentPotions = 1; 
        Debug.Log($"Potion brewed and ready to pick up: {potion.potionName}");

        currentDiesel = 0;
        currentGreen = 0;
        currentRed = 0;
        currentBlue = 0;

        isBrewing = false;
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



}
