using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarFuelSystem : MonoBehaviour
{
    [Header("Fuel (Diesel)")]
    public float currentDiesel = 20f;
    public float maxDiesel = 100f;
    
    [Header("Item Deposition")]
    public Transform depositPoint;
    public string depositPrompt = "Press Attack to deposit items";

    [Header("Debug")]
    public bool showDebugLogs = true;
    public float logConsumptionEvery = 5f;
    
    private bool playerInDepositRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private MovCar movCar;
    private float lastLoggedDiesel;
    private List<GameObject> pushingPlayers = new List<GameObject>();




    // ================================================= Methods =================================================




    void Start()
    {
        movCar = GetComponentInParent<MovCar>(); // Check parent for MovCar
        lastLoggedDiesel = currentDiesel; // Inicializar el último diesel registrado
    }

    void Update()
    {
        if (playerInDepositRange && nearbyPlayerInventory && nearbyPlayerInput) // Check if player is in range and has inventory
        {
            var attackAction = nearbyPlayerInput.actions["Attack"]; // Get the Attack action
            if (attackAction != null && attackAction.WasPressedThisFrame()) // Check if Attack was pressed
            {
                if (nearbyPlayerInventory.DepositDieselItems(this)) // Attempt to deposit diesel items
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[CarFuelSystem] ⛽ Diesel: {currentDiesel:F1}/{maxDiesel:F1}");
                    }
                    // Clean up state after depositing
                    playerInDepositRange = false;
                    nearbyPlayerInventory = null;
                    nearbyPlayerInput = null;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if the collider has a PlayerInventory
        
        if (playerInventory == null) return; // If no inventory, exit
        if (!pushingPlayers.Contains(other.gameObject)) // Add to pushing players list
        {
            pushingPlayers.Add(other.gameObject); // Add player to pushing list
        }
        
        if (playerInventory.HasItems()) // Only allow deposit if the player has items
        {
            PlayerInput playerInput = other.GetComponent<PlayerInput>(); // Get PlayerInput component
            if (playerInput == null) return;
            var attackAction = playerInput.actions["Attack"]; // Get the Attack action
            if (attackAction == null) return;

            // Set state to allow depositing
            playerInDepositRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (pushingPlayers.Contains(other.gameObject)) // Remove from pushing players list
        {
            pushingPlayers.Remove(other.gameObject);
        }
        
        // Clean up deposit state if the player leaves
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory == nearbyPlayerInventory)
        {
            playerInDepositRange = false;
            nearbyPlayerInventory = null;
            nearbyPlayerInput = null;
        }
    }

    public void AddDiesel(float amount) // Method to add diesel to the car
    {
        float prevDiesel = currentDiesel; // Store previous diesel amount
        currentDiesel = Mathf.Min(currentDiesel + amount, maxDiesel); // Add diesel but not exceed max

        if (movCar) movCar.OnFuelChanged(currentDiesel, maxDiesel); // Notify MovCar of fuel change
        lastLoggedDiesel = currentDiesel;
        
        if (showDebugLogs)
            Debug.Log($"[CarFuelSystem] ⛽ +{amount} diesel ({prevDiesel:F1} → {currentDiesel:F1})");
    }

    public void ConsumeDiesel(float amount) // Method to consume diesel when the car is moving
    {
        float prevDiesel = currentDiesel; // Store previous diesel amount
        currentDiesel = Mathf.Max(currentDiesel - amount, 0f); // Subtract diesel but not go below 0

        if (movCar) movCar.OnFuelChanged(currentDiesel, maxDiesel); // Notify MovCar of fuel change
        if (showDebugLogs)
        {
            float dieselConsumed = lastLoggedDiesel - currentDiesel;            
            if (currentDiesel <= 0f)
            {
                Debug.Log("[CarFuelSystem] ⚠️ ¡SIN COMBUSTIBLE!");
                lastLoggedDiesel = currentDiesel;
            }
            else if (GetDieselPercentage() < 0.2f && prevDiesel >= maxDiesel * 0.2f)
            {
                Debug.Log("[CarFuelSystem] ⚠️ ¡Combustible bajo!");
            }
        }
    }

    // Getters públicos
    public float GetCurrentDiesel() => currentDiesel;
    public float GetMaxDiesel() => maxDiesel;
    public float GetDieselPercentage() => maxDiesel > 0 ? currentDiesel / maxDiesel : 0f;
    public bool HasFuel() => currentDiesel > 0f;
    public List<GameObject> GetPlayersPushing() => pushingPlayers;
}