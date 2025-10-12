using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class CarModuleSwapper : MonoBehaviour
{
    [Header("Module Swap Configuration")]
    public Collider scrapCollider;
    public Collider plantCollider;
    public float swapAnimationTime = 1f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private bool isSwapping = false;
    private List<ISwappable> swappableComponents = new List<ISwappable>();
    private bool playerInRange = false;
    private PlayerInput nearbyPlayerInput;

    void Awake()
    {
        // Encuentra todos los componentes que implementan ISwappable
        var swappables = GetComponents<ISwappable>();
        swappableComponents.AddRange(swappables);
    }

    void Update()
    {
        if (playerInRange && nearbyPlayerInput && !isSwapping)
        {
            var jumpAction = nearbyPlayerInput.actions["Jump"];
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                StartCoroutine(SwapModules());
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInRange = true;
            nearbyPlayerInput = playerInput;
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null && playerInput == nearbyPlayerInput)
        {
            playerInRange = false;
            nearbyPlayerInput = null;
        }
    }

    IEnumerator SwapModules()
    {
        isSwapping = true;
        
        if (showDebugLogs)
            Debug.Log("[CarModuleSwapper] 🔄 Intercambiando módulos...");
        
        // Notifica a todos los componentes que se va a hacer el swap
        foreach (var swappable in swappableComponents)
        {
            swappable.OnSwapStarted();
        }
        
        // Desactiva temporalmente los colliders durante la animación
        scrapCollider.enabled = false;
        plantCollider.enabled = false;
        
        // Espera a que termine la animación
        yield return new WaitForSeconds(swapAnimationTime);
        
        // Intercambia las posiciones de los colliders
        Vector3 tempPosition = scrapCollider.transform.position;
        scrapCollider.transform.position = plantCollider.transform.position;
        plantCollider.transform.position = tempPosition;
        
        // Reactiva los colliders
        scrapCollider.enabled = true;
        plantCollider.enabled = true;
        
        // Notifica a todos los componentes que el swap terminó
        foreach (var swappable in swappableComponents)
        {
            swappable.OnSwapCompleted();
        }
        
        if (showDebugLogs)
            Debug.Log("[CarModuleSwapper] ✅ Módulos intercambiados!");
        
        isSwapping = false;
    }

    public bool IsSwapping() => isSwapping;
}

// Interface para los componentes que pueden ser afectados por el swap
public interface ISwappable
{
    void OnSwapStarted();
    void OnSwapCompleted();
}