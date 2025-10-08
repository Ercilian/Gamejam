using UnityEngine;
using UnityEngine.InputSystem;

public class WorldCollectible : MonoBehaviour
{
    [Header("Configuración")]
    public CollectibleData collectibleData;
    
    [Header("Efectos")]
    public ParticleSystem collectEffect;
    public float bobSpeed = 1f;
    public float bobHeight = 0.5f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private Vector3 startPosition;
    private bool playerInRange = false;
    private PlayerInventory nearbyPlayer;
    private PlayerInput nearbyPlayerInput; // Referencia al Input System

    void Start()
    {
        startPosition = transform.position;
        
        if (!collectibleData)
        {
            Debug.LogError($"[WorldCollectible] {gameObject.name} no tiene CollectibleData asignado!");
        }
        
        if (showDebugLogs)
            Debug.Log($"[WorldCollectible] {gameObject.name} inicializado con {(collectibleData ? collectibleData.itemName : "SIN DATOS")}");
    }

    void Update()
    {
        // Efecto de flotación
        FloatAnimation();
        
        // Detectar input de recolección usando Input System
        if (playerInRange && nearbyPlayer && nearbyPlayerInput)
        {
            var interactAction = nearbyPlayerInput.actions["Interact"];
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                CollectItem();
            }
        }
        
        // Mostrar prompt en consola (temporal hasta que tengas UI)
        if (playerInRange && showDebugLogs && collectibleData)
        {
            if (Time.frameCount % 120 == 0) // Cada 2 segundos aprox
            {
                Debug.Log($"[WorldCollectible] 💡 {collectibleData.collectPrompt}");
            }
        }
    }

    void FloatAnimation()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other)
        {
            Debug.LogWarning("[WorldCollectible] OnTriggerEnter recibió un Collider null!");
            return;
        }
        
        if (!other.gameObject)
        {
            Debug.LogWarning("[WorldCollectible] El Collider no tiene GameObject asociado!");
            return;
        }
        
        // Buscar PlayerInventory
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (!playerInventory)
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no es un jugador (sin PlayerInventory)");
            return;
        }
        
        // Buscar PlayerInput para el Input System
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (!playerInput)
        {
            Debug.LogWarning($"[WorldCollectible] {other.gameObject.name} no tiene PlayerInput component!");
            return;
        }
        
        // Verificar que tiene la acción Interact
        var interactAction = playerInput.actions["Interact"];
        if (interactAction == null)
        {
            Debug.LogWarning($"[WorldCollectible] {other.gameObject.name} no tiene acción 'Interact' configurada!");
            return;
        }
        
        if (!collectibleData)
        {
            Debug.LogError($"[WorldCollectible] {gameObject.name} no puede ser recogido: falta CollectibleData!");
            return;
        }
        
        // Verificar si el jugador puede cargar más items
        if (!playerInventory.CanCarryItem(collectibleData))
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no puede cargar más items");
            return;
        }
        
        // ¡Todo bien! El jugador está en rango
        playerInRange = true;
        nearbyPlayer = playerInventory;
        nearbyPlayerInput = playerInput;
        
        if (showDebugLogs)
            Debug.Log($"[WorldCollectible] 🎯 {other.gameObject.name} en rango de {collectibleData.itemName}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other || !other.gameObject)
            return;
            
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory && playerInventory == nearbyPlayer)
        {
            playerInRange = false;
            nearbyPlayer = null;
            nearbyPlayerInput = null;
            
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] ❌ {other.gameObject.name} salió del rango");
        }
    }

    void CollectItem()
    {
        if (!nearbyPlayer || !collectibleData)
        {
            Debug.LogWarning("[WorldCollectible] No se puede recoger: nearbyPlayer o collectibleData es null");
            return;
        }
        
        // Verificar nuevamente que el jugador puede cargar el item
        if (nearbyPlayer.CanCarryItem(collectibleData))
        {
            // Dar el item al jugador
            nearbyPlayer.PickupItem(collectibleData);
            
            // Efectos
            if (collectEffect) 
            {
                collectEffect.Play();
            }
            
            if (collectibleData.collectSound) 
            {
                AudioSource.PlayClipAtPoint(collectibleData.collectSound, transform.position);
            }
            
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] ✅ {nearbyPlayer.gameObject.name} recogió {collectibleData.itemName}");
            
            // Destruir el objeto después de un pequeño delay
            Destroy(gameObject, collectEffect ? 0.5f : 0.1f);
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[WorldCollectible] ⚠️ El jugador no puede cargar más items");
        }
    }
}