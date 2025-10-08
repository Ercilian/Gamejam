using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarFuelSystem : MonoBehaviour
{
    [Header("Combustible")]
    public float currentDiesel = 20f;
    public float maxDiesel = 100f;
    
    [Header("Deposit of Items")]
    public Transform depositPoint;
    public string depositPrompt = "Press F to deposit wood";

    [Header("Debug")]
    public bool showDebugLogs = true;
    public float logConsumptionEvery = 5f; // Log cada X unidades consumidas
    
    private bool playerInDepositRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private MovCarro movCarro;
    
    // Variables para controlar el logging de consumo
    private float lastLoggedDiesel;

    // Jugadores en la zona de empuje
    private List<GameObject> jugadoresEmpuje = new List<GameObject>();
    
    void Start()
    {
        // Buscar MovCarro en el GameObject padre
        movCarro = GetComponentInParent<MovCarro>();
        
        if (!movCarro)
        {
            Debug.LogError("[CarFuelSystem] No se encontró MovCarro en el GameObject padre!");
        }
        
        // Inicializar el último diesel loggeado
        lastLoggedDiesel = currentDiesel;
        
        if (showDebugLogs)
            Debug.Log($"[CarFuelSystem] Inicializado con {currentDiesel} diesel");
    }

    void Update()
    {
        if (playerInDepositRange && nearbyPlayerInventory && Input.GetKeyDown(KeyCode.F))
        {
            DepositItems();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory.HasItems())
        {
            playerInDepositRange = true;
            nearbyPlayerInventory = playerInventory;
            
            if (showDebugLogs)
            {
                Debug.Log($"[CarFuelSystem] 🎯 Jugador en rango con {playerInventory.GetCarriedItemCount()} items");
                Debug.Log($"[CarFuelSystem] 💡 {depositPrompt}");
            }
        }

        // Empuje: añadir jugador si tiene el tag Player
        if (other.CompareTag("Player") && !jugadoresEmpuje.Contains(other.gameObject))
        {
            jugadoresEmpuje.Add(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory == nearbyPlayerInventory)
        {
            playerInDepositRange = false;
            nearbyPlayerInventory = null;
            
            if (showDebugLogs)
                Debug.Log("[CarFuelSystem] ❌ Jugador salió del rango de depósito");
        }

        // Empuje: quitar jugador si sale
        if (other.CompareTag("Player") && jugadoresEmpuje.Contains(other.gameObject))
        {
            jugadoresEmpuje.Remove(other.gameObject);
        }
    }

    void DepositItems()
    {
        if (!nearbyPlayerInventory) return;
        
        int itemCount = nearbyPlayerInventory.GetCarriedItemCount();
        
        if (nearbyPlayerInventory.DepositItems(this))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CarFuelSystem] ✅ {itemCount} items depositados!");
                Debug.Log($"[CarFuelSystem] ⛽ Diesel: {currentDiesel:F1}/{maxDiesel:F1}");
                Debug.Log($"[CarFuelSystem] 📊 Nivel: {(GetDieselPercentage() * 100):F0}%");
            }
        }
    }

    public void AddDiesel(float amount)
    {
        float prevDiesel = currentDiesel;
        currentDiesel = Mathf.Min(currentDiesel + amount, maxDiesel);
        
        if (movCarro) movCarro.OnFuelChanged(currentDiesel, maxDiesel);
        
        // Actualizar referencia para logging cuando se añade diesel
        lastLoggedDiesel = currentDiesel;
        
        if (showDebugLogs)
            Debug.Log($"[CarFuelSystem] ⛽ +{amount} diesel ({prevDiesel:F1} → {currentDiesel:F1})");
    }

    public void ConsumeDiesel(float amount)
    {
        float prevDiesel = currentDiesel;
        currentDiesel = Mathf.Max(currentDiesel - amount, 0f);
        
        if (movCarro) movCarro.OnFuelChanged(currentDiesel, maxDiesel);
        
        // Solo loggear cada X unidades consumidas
        if (showDebugLogs)
        {
            float dieselConsumed = lastLoggedDiesel - currentDiesel;
            
            // Si hemos consumido más de logConsumptionEvery unidades desde el último log
            if (dieselConsumed >= logConsumptionEvery)
            {
                Debug.Log($"[CarFuelSystem] 🔥 -{dieselConsumed:F1} diesel ({lastLoggedDiesel:F1} → {currentDiesel:F1})");
                lastLoggedDiesel = currentDiesel;
            }
            
            // Siempre loggear eventos importantes
            if (currentDiesel <= 0f)
            {
                Debug.Log("[CarFuelSystem] ⚠️ ¡SIN COMBUSTIBLE!");
                lastLoggedDiesel = currentDiesel; // Reset para evitar spam
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

    // Getter para MovCarro
    public List<GameObject> GetJugadoresEmpujando()
    {
        return jugadoresEmpuje;
    }
}