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

    [Header("Audio")]
    public AudioClip lever;
    public AudioClip swapSound;
    private AudioSource audioSource;


    // ===== PRIVATE FIELDS =====
    private bool isSwapping = false;
    private List<ISwappable> swappableComponents = new List<ISwappable>();
    private bool playerInRange = false;
    private PlayerInput nearbyPlayerInput;

    // ===== PUBLIC GETTERS =====
    public bool IsSwapping() => isSwapping;




    // ================================================= Methods =================================================


    

    void Awake() // Cache all ISwappable components on this GameObject
    {
        var swappables = GetComponents<ISwappable>();
        swappableComponents.AddRange(swappables);
        audioSource = GetComponent<AudioSource>();
    }

    void Update() // Check for player input to swap modules
    {
        if (playerInRange && nearbyPlayerInput && !isSwapping) // If player is in range and not currently swapping
        {
            var jumpAction = nearbyPlayerInput.actions["Jump"];
            if (jumpAction != null && jumpAction.WasPressedThisFrame()) // If the jump action was pressed
            {
                audioSource.PlayOneShot(lever);
                StartCoroutine(SwapModules()); // Start the swap coroutine
            }
        }
    }

    void OnTriggerEnter(Collider other) // Detect player entering the swap area
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>(); 
        if (playerInput != null)
        {
            playerInRange = true;
            nearbyPlayerInput = playerInput;
        }
    }

    void OnTriggerExit(Collider other) // Detect player leaving the swap area
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null && playerInput == nearbyPlayerInput)
        {
            playerInRange = false;
            nearbyPlayerInput = null;
        }
    }

    private IEnumerator SwapModules() // Coroutine to handle the swap process
    {
        audioSource.PlayOneShot(swapSound);
        isSwapping = true;
        Debug.Log("[CarModuleSwapper] 🔄 Intercambiando módulos...");

        foreach (var swappable in swappableComponents) // Notify all components that the swap is starting
        {
            swappable.OnSwapStarted();
        }

        // Deactivate colliders during the swap animation
        scrapCollider.enabled = false;
        plantCollider.enabled = false;

        yield return new WaitForSeconds(swapAnimationTime); // Wait for the swap animation duration

        // Swap the positions of the colliders
        Vector3 tempPosition = scrapCollider.transform.position;
        scrapCollider.transform.position = plantCollider.transform.position;
        plantCollider.transform.position = tempPosition;

        // Reactivate colliders
        scrapCollider.enabled = true;
        plantCollider.enabled = true;

        foreach (var swappable in swappableComponents) // Notify all components that the swap has completed
        {
            swappable.OnSwapCompleted();
        }

        isSwapping = false;
    }
}

// Interface para los componentes que pueden ser afectados por el swap
public interface ISwappable
{
    void OnSwapStarted();
    void OnSwapCompleted();
}