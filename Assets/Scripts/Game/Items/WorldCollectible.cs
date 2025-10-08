using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldCollectible : MonoBehaviour
{
    [Header("Configuración")]
    public CollectibleData collectibleData;
    
    [Header("Efectos")]
    public ParticleSystem collectEffect;
    public float bobSpeed = 1f; // Velocidad de flotación
    public float bobHeight = 0.5f; // Altura de flotación
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private Vector3 startPosition;
    private bool playerInRange = false;
    private PlayerInventory nearbyPlayer;

    void Start()
    {
        startPosition = transform.position;
        
        // Verificar que tenemos los datos necesarios
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
        
        // Detectar input de recolección
        if (playerInRange && nearbyPlayer && Input.GetKeyDown(KeyCode.E))
        {
            CollectItem();
        }
        
        // Debug en pantalla (opcional)
        if (playerInRange && showDebugLogs)
        {
            Debug.Log($"[WorldCollectible] 💡 Presiona E para recoger {(collectibleData ? collectibleData.itemName : "item")}");
        }
    }

    void FloatAnimation()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        // VERIFICACIÓN SEGURA: Verificar que other no es null
        if (!other)
        {
            Debug.LogWarning("[WorldCollectible] OnTriggerEnter recibió un Collider null!");
            return;
        }
        
        // VERIFICACIÓN SEGURA: Verificar que other.gameObject existe
        if (!other.gameObject)
        {
            Debug.LogWarning("[WorldCollectible] El Collider no tiene GameObject asociado!");
            return;
        }
        
        // VERIFICACIÓN SEGURA: Intentar obtener PlayerInventory
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        
        // Si no tiene PlayerInventory, no es un jugador
        if (!playerInventory)
        {
            if (showDebugLogs)
                Debug.Log($"[WorldCollectible] {other.gameObject.name} no es un jugador (sin PlayerInventory)");
            return;
        }
        
        // VERIFICACIÓN SEGURA: Verificar que collectibleData existe
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
        
        if (showDebugLogs)
            Debug.Log($"[WorldCollectible] 🎯 {other.gameObject.name} en rango de {collectibleData.itemName}");
    }

    void OnTriggerExit(Collider other)
    {
        // VERIFICACIÓN SEGURA
        if (!other || !other.gameObject)
            return;
            
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory && playerInventory == nearbyPlayer)
        {
            playerInRange = false;
            nearbyPlayer = null;
            
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