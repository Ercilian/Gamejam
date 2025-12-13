using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarScrapSystem : MonoBehaviour, ISwappable
{
    [Header("Scrap")]
    public int currentScrap = 0;
    public int maxScrap = 9999;

    [Header("Scrap Deposit Point")]
    public Transform scrapDepositPoint;
    public string scrapDepositPrompt = "Press 'Attack' to deposit scrap";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ===== PRIVATE FIELDS =====
    private bool playerInScrapRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private bool isSwapping = false;

    // ===== PUBLIC GETTERS =====
    public int GetCurrentScrap() => currentScrap;
    public int GetMaxScrap() => maxScrap;
    public float GetScrapPercentage() => maxScrap > 0 ? (float)currentScrap / maxScrap : 0f;

    // ===== EVENTS =====
    public System.Action<int> OnScrapChanged;




    // ================================================= Methods =================================================




    void Update()
    {
        // Check if player is in range, has inventory and input, and not swapping
        if (playerInScrapRange && nearbyPlayerInventory && nearbyPlayerInput && !isSwapping)
        {
            var attackAction = nearbyPlayerInput.actions["Attack"];
            if (attackAction != null && attackAction.WasPressedThisFrame()) // Deposit scrap on 'Attack' input
            {
                if (nearbyPlayerInventory.DepositScrapItems(this))
                {
                    if (showDebugLogs)
                        Debug.Log($"[CarScrapSystem] ✅ Scrap deposited! Total: {currentScrap}");

                    ClearPlayerInteraction();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isSwapping) return;

        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;
        if (playerInventory.HasItems() && playerInventory.GetFirstItemType() == CollectibleData.ItemType.Scrap)
        {
            PlayerInput playerInput = other.GetComponent<PlayerInput>();
            if (playerInput == null) return;
            var attackAction = playerInput.actions["Attack"];
            if (attackAction == null) return;

            playerInScrapRange = true;
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
        playerInScrapRange = false;
        nearbyPlayerInventory = null;
        nearbyPlayerInput = null;
    }

    // ============== SCRAP MANAGEMENT METHODS ==============


    public void AddScrap(int amount)
    {
        int prevScrap = currentScrap;
        currentScrap = Mathf.Min(currentScrap + amount, maxScrap);

        if (showDebugLogs)
            Debug.Log($"[CarScrapSystem] 💰 +{amount} scrap ({prevScrap} → {currentScrap})");

        OnScrapChanged?.Invoke(currentScrap);
    }

    public bool CanAfford(int cost)
    {
        return currentScrap >= cost;
    }

    public bool SpendScrap(int amount)
    {
        if (CanAfford(amount))
        {
            int prevScrap = currentScrap;
            currentScrap -= amount;

            if (showDebugLogs)
                Debug.Log($"[CarScrapSystem] 💸 -{amount} scrap ({prevScrap} → {currentScrap})");

            OnScrapChanged?.Invoke(currentScrap);
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[CarScrapSystem] ❌ Not enough scrap! (Need {amount}, have {currentScrap})");
            return false;
        }
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
