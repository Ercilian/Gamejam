using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovCarro : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 direction = Vector3.forward;
    public float speed = 1f;
    public float fastspeed = 2f;
    public float slowspeed = 0.5f;
    
    [Header("Speed Transition")]
    public float speedTransitionRate = 2f; // Velocidad de transición entre velocidades
    
    [Header("Combustible Consumption")]
    public float fuelConsumptionPerSecond = 1f;
    public bool isFuelConsumed = true;
    
    [Header("Push Settings")]
    public float pushSpeed = 0.5f;
    public float pushSpeedTwo = 0.85f;
    
    private CarFuelSystem fuelSystem;
    private bool ismoving = false;
    private Coroutine consumeCoroutine;
    private bool isPushing = false;
    private int playersPushingCount = 0;
    private float currentActualSpeed = 0f; // Velocidad actual interpolada

    // Getters públicos para otros scripts
    public bool IsMoving() => ismoving;
    public float GetCurrentSpeedPublic() => currentActualSpeed; // Devuelve la velocidad interpolada actual
    public int GetPlayersPushingCount() => playersPushingCount;




    // ================================================= Methods =================================================




    void Start()
    {
        fuelSystem = GetComponent<CarFuelSystem>(); // Get CarFuelSystem on this GameObject
        
        if (!fuelSystem) // If not found, check children
        {
            fuelSystem = GetComponentInChildren<CarFuelSystem>();
        }
        
        if (isFuelConsumed) // Start fuel consumption if enabled
        {
            consumeCoroutine = StartCoroutine(ConsumeFuel());
        }
    }

    void Update()
    {
        float targetSpeed = 0f; // Velocidad objetivo
        
        if (!fuelSystem || !fuelSystem.HasFuel()) // If no fuel, stop moving normally
        {
            ismoving = false;
            targetSpeed = 0f; // Objetivo es detenerse
            var playersPushing = fuelSystem.GetPlayersPushing(); // Get players in push zone
            playersPushingCount = 0; // Reset counter

            if (playersPushing.Count > 0) // If there are players in the push zone, handle pushing
            {
                foreach (var player in playersPushing) // Set them to follow the car
                {
                    if (player != null)
                    {
                        var input = player.GetComponent<PlayerInputEmpuje>(); // Get their push input component
                        if (input != null)
                        {
                            input.FollowObject(transform, GetPushSpeed(1)); // Make them follow the car at push speed
                            if (input.ImPushing()) // Check if they are pushing
                            {
                                playersPushingCount++; // Increment counter
                            }
                        }
                    }
                }
                
                if (playersPushingCount > 0) // If at least one player is pushing, move the car
                {
                    if (!isPushing)
                    {
                        Debug.Log($"[MovCarro] El coche está siendo empujado por {playersPushingCount} jugador(es).");
                        isPushing = true;
                    }
                    targetSpeed = GetPushSpeed(playersPushingCount); // Objetivo es velocidad de empuje
                }
            }
            else
            {
                playersPushingCount = 0;
            }
        }
        else
        {
            // Move normally if there is fuel
            ismoving = true;  
            isPushing = false;
            playersPushingCount = 0;

            // Reactive control for all players when there is fuel
            var playersPushingWithFuel = fuelSystem.GetPlayersPushing();
            foreach (var player in playersPushingWithFuel)
            {
                player?.GetComponent<PlayerInputEmpuje>()?.ActivateControl();
            }
            
            targetSpeed = GetCurrentSpeed(); // Objetivo es velocidad basada en combustible
        }
        
        // Interpolar suavemente hacia la velocidad objetivo
        currentActualSpeed = Mathf.MoveTowards(currentActualSpeed, targetSpeed, speedTransitionRate * Time.deltaTime);
        
        // Mover el coche con la velocidad interpolada
        if (currentActualSpeed > 0.01f) // Solo mover si hay velocidad significativa
        {
            transform.Translate(direction.normalized * currentActualSpeed * Time.deltaTime, Space.World);
        }
    }

    private float GetPushSpeed(int numPlayers) // Method to determine push speed based on number of players
    {
        switch (numPlayers)
        {
            case 1:
                return pushSpeed; // Base speed
            case 2:
                return pushSpeedTwo; // Slightly faster
            default:
                return pushSpeed; // Fallback
        }
    }

    private float GetCurrentSpeed() // Method to determine current speed based on fuel level
    {
        if (!fuelSystem) return 0f; // Safety check
        
        float dieselPercentage = fuelSystem.GetDieselPercentage(); // Get current fuel percentage
        if (dieselPercentage < 0.2f) // Increase speed if below 20%
        {
            return fastspeed;
        }
        if (dieselPercentage >= 0.8f) // Decrease speed if above 80%
        {
            return slowspeed;
        }
        return speed;
    }

    private IEnumerator ConsumeFuel() // Coroutine to consume fuel over time
    {
        while (fuelSystem && fuelSystem.HasFuel()) // While there is fuel
        {
            yield return new WaitForSeconds(1f);

            if (ismoving && fuelSystem.HasFuel()) // Only consume fuel if moving
            {
                fuelSystem.ConsumeDiesel(fuelConsumptionPerSecond);
            }
        }
        consumeCoroutine = null;
    }

    public void OnFuelChanged(float currentFuel, float maxFuel) // Method called by CarFuelSystem when fuel changes
    {
        float fuelPercentage = currentFuel / maxFuel;

        if (currentFuel > 0f && consumeCoroutine == null && isFuelConsumed) // Restart consumption if fuel is added from zero
        {
            Debug.Log("[MovCarro] Combustible repuesto, reanudando consumo.");
            consumeCoroutine = StartCoroutine(ConsumeFuel());
        }
    }

    void OnDestroy()
    {
        // Detener corrutina al destruir el objeto
        if (consumeCoroutine != null)
        {
            StopCoroutine(consumeCoroutine);
        }
    }

}