using UnityEngine;
using UnityEngine.InputSystem;


public class ShopLever : MonoBehaviour
{
    private bool leverActivated = false;
    private bool playerInRange = false;
    private PlayerInput nearbyPlayerInput;
    private ShopStopTrigger shopStopTrigger;

    void Start()
    {
        shopStopTrigger = FindFirstObjectByType<ShopStopTrigger>();
    }
    void Update()
    {
        if (playerInRange && nearbyPlayerInput != null && !leverActivated)
        {
            var interactAction = nearbyPlayerInput.actions["Interact"];
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                ActivateLever();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInRange = true;
            nearbyPlayerInput = playerInput;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null && playerInput == nearbyPlayerInput)
        {
            playerInRange = false;
            nearbyPlayerInput = null;
        }
    }

    public void ActivateLever()
    {
        if (leverActivated)
        {
            return;
        }
        leverActivated = true;
        shopStopTrigger.unlockShop = false;


        // Busca el coche por tag
        GameObject carObj = GameObject.FindGameObjectWithTag("Car");
        if (carObj != null)
        {
            MovCar movCar = carObj.GetComponent<MovCar>();
            if (movCar != null)
            {
                movCar.enabled = true; // Reactiva el script de movimiento
                Debug.Log("Palanca activada: el coche puede avanzar.");
            }
        }

    }
}
