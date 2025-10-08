using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarScrapSystem : MonoBehaviour
{
    [Header("Scrap (Moneda)")]
    public int currentScrap = 0;
    public int maxScrap = 9999; // Límite máximo (opcional)
    
    [Header("Depósito de Scrap")]
    public Transform scrapDepositPoint;
    public string scrapDepositPrompt = "Presiona Clic Izquierdo para depositar scrap";
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private bool playerInScrapRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;

    void Start()
    {
        if (showDebugLogs)
            Debug.Log($"[CarScrapSystem] Inicializado con {currentScrap} scrap");
    }

    void Update()
    {
        // Detectar input de depósito usando acción "Attack"
        if (playerInScrapRange && nearbyPlayerInventory && nearbyPlayerInput)
        {
            var attackAction = nearbyPlayerInput.actions["Attack"];
            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                DepositScrap();
            }
        }
        
        // Mostrar prompt en consola solo si tiene scrap
        if (playerInScrapRange && showDebugLogs && nearbyPlayerInventory && nearbyPlayerInventory.HasScrapItems())
        {
            if (Time.frameCount % 120 == 0) // Cada 2 segundos aprox
            {
                int scrapCount = nearbyPlayerInventory.GetCarriedScrapCount();
                Debug.Log($"[CarScrapSystem] 💡 {scrapDepositPrompt} ({scrapCount} scrap)");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Buscar PlayerInventory
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (!playerInventory)
        {
            if (showDebugLogs)
                Debug.Log($"[CarScrapSystem] {other.gameObject.name} no es un jugador (sin PlayerInventory)");
            return;
        }
        
        // Solo configurar depósito si tiene scrap
        if (playerInventory.HasScrapItems())
        {
            // Buscar PlayerInput para el Input System
            PlayerInput playerInput = other.GetComponent<PlayerInput>();
            if (!playerInput)
            {
                Debug.LogWarning($"[CarScrapSystem] {other.gameObject.name} no tiene PlayerInput component!");
                return;
            }
            
            // Verificar que tiene la acción Attack
            var attackAction = playerInput.actions["Attack"];
            if (attackAction == null)
            {
                Debug.LogWarning($"[CarScrapSystem] {other.gameObject.name} no tiene acción 'Attack' configurada!");
                return;
            }
            
            // Configurar para depósito
            playerInScrapRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
            
            if (showDebugLogs)
            {
                Debug.Log($"[CarScrapSystem] 🔩 {other.gameObject.name} listo para depositar {playerInventory.GetCarriedScrapCount()} scrap");
            }
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[CarScrapSystem] {other.gameObject.name} no tiene scrap para depositar");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Limpiar depósito si es el jugador que se va
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory == nearbyPlayerInventory)
        {
            playerInScrapRange = false;
            nearbyPlayerInventory = null;
            nearbyPlayerInput = null;
            
            if (showDebugLogs)
                Debug.Log($"[CarScrapSystem] ❌ {other.gameObject.name} salió del rango de depósito de scrap");
        }
    }

    void DepositScrap()
    {
        if (!nearbyPlayerInventory) return;
        
        int scrapCount = nearbyPlayerInventory.GetCarriedScrapCount();
        
        if (nearbyPlayerInventory.DepositScrapItems(this))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CarScrapSystem] ✅ {scrapCount} scrap depositado!");
                Debug.Log($"[CarScrapSystem] 💰 Scrap total: {currentScrap}");
            }
            
            // Limpiar estado de depósito después de depositar
            playerInScrapRange = false;
            nearbyPlayerInventory = null;
            nearbyPlayerInput = null;
        }
    }

    public void AddScrap(int amount)
    {
        int prevScrap = currentScrap;
        currentScrap = Mathf.Min(currentScrap + amount, maxScrap);
        
        if (showDebugLogs)
            Debug.Log($"[CarScrapSystem] 💰 +{amount} scrap ({prevScrap} → {currentScrap})");
        
        // Aquí puedes triggear eventos de UI, sonidos, etc.
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
