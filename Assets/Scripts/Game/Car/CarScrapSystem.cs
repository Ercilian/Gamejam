using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarScrapSystem : MonoBehaviour, ISwappable
{
    [Header("Scrap)")]
    public int currentScrap = 0;
    public int maxScrap = 9999;
    
    [Header("Scrap Deposit Point")]
    public Transform scrapDepositPoint;
    public string scrapDepositPrompt = "Press 'Attack' to deposit scrap";
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private bool playerInScrapRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private bool isSwapping = false;

    // ================================================= Methods =================================================

    void Update()
    {
        if (playerInScrapRange && nearbyPlayerInventory && nearbyPlayerInput && !isSwapping)
        {
            var attackAction = nearbyPlayerInput.actions["Attack"];
            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                if (nearbyPlayerInventory.DepositScrapItems(this))
                {
                    if (showDebugLogs)
                        Debug.Log($"[CarScrapSystem] ✅ Scrap depositado! Total: {currentScrap}");
                    
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

    void ClearPlayerInteraction()
    {
        playerInScrapRange = false;
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
                Debug.Log($"[CarScrapSystem] ❌ No tienes suficiente scrap! (Necesitas {amount}, tienes {currentScrap})");
            return false;
        }
    }

    // Getters públicos
    public int GetCurrentScrap() => currentScrap;
    public int GetMaxScrap() => maxScrap;
    public float GetScrapPercentage() => maxScrap > 0 ? (float)currentScrap / maxScrap : 0f;

    // Evento para notificar cambios de scrap (útil para UI)
    public System.Action<int> OnScrapChanged;
}
