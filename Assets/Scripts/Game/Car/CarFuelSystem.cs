using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarFuelSystem : MonoBehaviour
// Manages the fuel system of the car, including diesel levels and item deposition
{
    [Header("Diesel Settings")]
    public float cur_Diesel = 20f;
    public float max_Diesel = 100f;
    
    [Header("Item Deposition")]
    public Transform depositPoint;
    public string depositPrompt = "Press Attack to deposit items";

    [Header("Audio")]
    private AudioSource audioSource;

    
    // ===== PRIVATE FIELDS =====
    private bool playerInDepositRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private MovCar movCar;
    private float lastLoggedDiesel;
    private List<GameObject> pushingPlayers = new List<GameObject>();

    // ===== PUBLIC GETTERS =====
    public float GetCurrentDiesel() => cur_Diesel;
    public float GetMaxDiesel() => max_Diesel;
    public float GetDieselPercentage() => max_Diesel > 0 ? cur_Diesel / max_Diesel : 0f;
    public bool HasFuel() => cur_Diesel > 0f;
    public List<GameObject> GetPlayersPushing() => pushingPlayers;




    // ================================================= Methods =================================================




    void Start() // Search for MovCar component, audio source and initialize last logged diesel
    {
        movCar = GetComponentInParent<MovCar>();
        lastLoggedDiesel = cur_Diesel;
        audioSource = GetComponent<AudioSource>();
    }

    void Update() // Handle item deposition
    {
        if (playerInDepositRange && nearbyPlayerInventory && nearbyPlayerInput) // Check if player is in range and has inventory
        {
            var InteractAction = nearbyPlayerInput.actions["Interact"]; // Get the Interact action
            if (InteractAction.WasPressedThisFrame()) // Check if Interact was pressed
            {
                if (nearbyPlayerInventory.DepositDieselItems(this)) // Attempt to deposit diesel items
                {
                    // Clean up state after depositing
                    playerInDepositRange = false;
                    nearbyPlayerInventory = null;
                    nearbyPlayerInput = null;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other) // Handle player entering deposit/push range
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>(); // Check if the collider has a PlayerInventory
        if (playerInventory == null) return; // If no inventory, exit

        if (playerInventory.HasItems()) // Only allow deposit if the player has items
        {
            PlayerInput playerInput = other.GetComponent<PlayerInput>(); // Get PlayerInput component
            if (playerInput == null) return;
            var InteractAction = playerInput.actions["Interact"]; // Get the Interact action

            // Set state to allow depositing
            playerInDepositRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
        }
        
        // Handle pushing (only if player has NO items)
        if (!playerInventory.HasItems()) // Only allow pushing if the player has no items
        {
            if (!pushingPlayers.Contains(other.gameObject)) // Add to pushing players list
            {
                pushingPlayers.Add(other.gameObject); // Add player to pushing list
            }
        }
    }

    void OnTriggerExit(Collider other) // Handle player exiting deposit/push range
    {
        // Remove from pushing players list (regardless of items)
        if (pushingPlayers.Contains(other.gameObject))
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


    // ===== FUEL MANAGEMENT METHODS =====

    public void AddDiesel(float amount, CollectibleData data = null) // Method to add diesel to the car
    {
        if (data != null && data.depositSound != null) // Play deposit sound if provided (when all sounds are set, remove null check)
        {
            audioSource.PlayOneShot(data.depositSound);
        }

        float prevDiesel = cur_Diesel; // Store previous diesel amount
        cur_Diesel = Mathf.Min(cur_Diesel + amount, max_Diesel); // Add diesel but not exceed max

        if (movCar) movCar.OnFuelChanged(cur_Diesel, max_Diesel); // Notify MovCar of fuel change
        lastLoggedDiesel = cur_Diesel;
        Debug.Log($"[CarFuelSystem] ⛽ +{amount} diesel ({prevDiesel:F1} → {cur_Diesel:F1})");
    }

    public void ConsumeDiesel(float amount) // Method to consume diesel when the car is moving
    {
        float prevDiesel = cur_Diesel; // Store previous diesel amount
        cur_Diesel = Mathf.Max(cur_Diesel - amount, 0f); // Subtract diesel but not go below 0

        if (movCar) movCar.OnFuelChanged(cur_Diesel, max_Diesel); // Notify MovCar of fuel change
        {
            float dieselConsumed = lastLoggedDiesel - cur_Diesel;            
            if (cur_Diesel <= 0f)
            {
                Debug.Log("[CarFuelSystem] ⚠️ ¡SIN COMBUSTIBLE!");
                lastLoggedDiesel = cur_Diesel;
            }
            else if (GetDieselPercentage() < 0.2f && prevDiesel >= max_Diesel * 0.2f)
            {
                Debug.Log("[CarFuelSystem] ⚠️ ¡Combustible bajo!");
            }
        }
    }


}