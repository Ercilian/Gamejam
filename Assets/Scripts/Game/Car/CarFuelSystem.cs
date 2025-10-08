using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarFuelSystem : MonoBehaviour
{
    [Header("Combustible")]
    public float currentDiesel = 20f;
    public float maxDiesel = 100f;
    
    [Header("Depósito de Items")]
    public Transform depositPoint;
    public string depositPrompt = "Presiona Clic Izquierdo para depositar items";
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public float logConsumptionEvery = 5f;
    
    private bool playerInDepositRange = false;
    private PlayerInventory nearbyPlayerInventory;
    private PlayerInput nearbyPlayerInput;
    private MovCarro movCarro;
    private float lastLoggedDiesel;
    private List<GameObject> jugadoresEmpuje = new List<GameObject>();

    void Start()
    {
        movCarro = GetComponentInParent<MovCarro>();
        
        if (!movCarro)
        {
            Debug.LogError("[CarFuelSystem] No se encontró MovCarro en el GameObject padre!");
        }
        
        // CORREGIDO: lastLoggedDiesal → lastLoggedDiesel
        lastLoggedDiesel = currentDiesel;
        
        if (showDebugLogs)
            Debug.Log($"[CarFuelSystem] Inicializado con {currentDiesel} diesel");
    }

    void Update()
    {
        // Detectar input de depósito usando acción "Attack"
        if (playerInDepositRange && nearbyPlayerInventory && nearbyPlayerInput)
        {
            var attackAction = nearbyPlayerInput.actions["Attack"];
            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                DepositItems();
            }
        }
        
        // Mostrar prompt en consola solo si tiene items
        if (playerInDepositRange && showDebugLogs && nearbyPlayerInventory && nearbyPlayerInventory.HasItems())
        {
            if (Time.frameCount % 120 == 0) // Cada 2 segundos aprox
            {
                Debug.Log($"[CarFuelSystem] 💡 {depositPrompt} ({nearbyPlayerInventory.GetCarriedItemCount()} items)");
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
                Debug.Log($"[CarFuelSystem] {other.gameObject.name} no es un jugador (sin PlayerInventory)");
            return;
        }
        
        // CAMBIO CLAVE: Añadir a la lista de empuje SIEMPRE
        if (!jugadoresEmpuje.Contains(other.gameObject))
        {
            jugadoresEmpuje.Add(other.gameObject);
            if (showDebugLogs)
                Debug.Log($"[CarFuelSystem] 🎯 {other.gameObject.name} entró en zona de empuje");
        }
        
        // Solo configurar depósito si tiene items
        if (playerInventory.HasItems())
        {
            // Buscar PlayerInput para el Input System
            PlayerInput playerInput = other.GetComponent<PlayerInput>();
            if (!playerInput)
            {
                Debug.LogWarning($"[CarFuelSystem] {other.gameObject.name} no tiene PlayerInput component!");
                return;
            }
            
            // Verificar que tiene la acción Attack
            var attackAction = playerInput.actions["Attack"];
            if (attackAction == null)
            {
                Debug.LogWarning($"[CarFuelSystem] {other.gameObject.name} no tiene acción 'Attack' configurada!");
                return;
            }
            
            // Configurar para depósito
            playerInDepositRange = true;
            nearbyPlayerInventory = playerInventory;
            nearbyPlayerInput = playerInput;
            
            if (showDebugLogs)
            {
                Debug.Log($"[CarFuelSystem] 🎒 {other.gameObject.name} listo para depositar {playerInventory.GetCarriedItemCount()} items");
            }
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[CarFuelSystem] {other.gameObject.name} en zona de empuje (sin items para depositar)");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Remover de la lista de empuje
        if (jugadoresEmpuje.Contains(other.gameObject))
        {
            jugadoresEmpuje.Remove(other.gameObject);
            if (showDebugLogs)
                Debug.Log($"[CarFuelSystem] ❌ {other.gameObject.name} salió de la zona de empuje");
        }
        
        // Limpiar depósito si es el jugador que se va
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory == nearbyPlayerInventory)
        {
            playerInDepositRange = false;
            nearbyPlayerInventory = null;
            nearbyPlayerInput = null;
            
            if (showDebugLogs)
                Debug.Log($"[CarFuelSystem] 📦 {other.gameObject.name} salió del rango de depósito");
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
                Debug.Log($"[CarFuelSystem] ✅ {itemCount} items depositados con Attack!");
                Debug.Log($"[CarFuelSystem] ⛽ Diesel: {currentDiesel:F1}/{maxDiesel:F1}");
                Debug.Log($"[CarFuelSystem] 📊 Nivel: {(GetDieselPercentage() * 100):F0}%");
            }
            
            // Limpiar estado de depósito después de depositar
            playerInDepositRange = false;
            nearbyPlayerInventory = null;
            nearbyPlayerInput = null;
        }
    }

    public void AddDiesel(float amount)
    {
        float prevDiesel = currentDiesel;
        currentDiesel = Mathf.Min(currentDiesel + amount, maxDiesel);
        
        if (movCarro) movCarro.OnFuelChanged(currentDiesel, maxDiesel);
        
        lastLoggedDiesel = currentDiesel;
        
        if (showDebugLogs)
            Debug.Log($"[CarFuelSystem] ⛽ +{amount} diesel ({prevDiesel:F1} → {currentDiesel:F1})");
    }

    public void ConsumeDiesel(float amount)
    {
        float prevDiesel = currentDiesel;
        currentDiesel = Mathf.Max(currentDiesel - amount, 0f);
        
        if (movCarro) movCarro.OnFuelChanged(currentDiesel, maxDiesel);
        
        if (showDebugLogs)
        {
            float dieselConsumed = lastLoggedDiesel - currentDiesel;
            
            if (dieselConsumed >= logConsumptionEvery)
            {
                Debug.Log($"[CarFuelSystem] 🔥 -{dieselConsumed:F1} diesel ({lastLoggedDiesel:F1} → {currentDiesel:F1})");
                lastLoggedDiesel = currentDiesel;
            }
            
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
    public List<GameObject> GetJugadoresEmpujando() => jugadoresEmpuje;
}