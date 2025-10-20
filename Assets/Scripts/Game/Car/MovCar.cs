using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovCar : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 direction = Vector3.forward;
    public float speed = 1f;
    public float fastspeed = 2f;
    public float slowspeed = 0.5f;
    
    [Header("Speed Transition")]
    public float speedTransitionRate = 2f; // Velocidad de transición entre velocidades
    
    [Header("Path Following System (Future Implementation)")]
    [Tooltip("Enable this when you have your map ready with waypoints")]
    public bool usePathFollowing = false;
    [Tooltip("Drag GameObjects here to mark the path (3-5 key points in curves)")]
    public Transform[] pathPoints;
    [Tooltip("Distance to consider a waypoint 'reached' (2-4 recommended)")]
    public float reachDistance = 3f;
    [Tooltip("How smooth the curves are (1=sharp, 3=very smooth)")]
    public float pathSmoothness = 2f;
    
    [Header("Combustible Consumption")]
    public float fuelConsumptionPerSecond = 1f;
    public bool isFuelConsumed = true;
    
    [Header("Push Settings")]
    public float pushSpeed = 0.5f;
    public float pushSpeedTwo = 0.85f;
    
    [Header("Rotation Settings")]
    [Tooltip("How fast the car rotates to face movement direction")]
    public float rotationSpeed = 5f;
    
    private CarFuelSystem fuelSystem;
    private bool ismoving = false;
    private Coroutine consumeCoroutine;
    private bool isPushing = false;
    private int playersPushingCount = 0;
    private float currentActualSpeed = 0f; // Velocidad actual interpolada

    // ===== PATH FOLLOWING VARIABLES (FOR FUTURE USE) =====
    private int currentPathIndex = 0;
    private Vector3 currentTarget;

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
        
        // ===== PATH FOLLOWING INITIALIZATION (FOR FUTURE USE) =====
        InitializePathFollowing();
    }

    void Update()
    {
        float targetSpeed = 0f; // Velocidad objetivo
        
        // ===== GET MOVEMENT DIRECTION (DYNAMIC BASED ON PATH/FIXED) =====
        Vector3 moveDirection = GetMovementDirection();
        
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
                        var input = player.GetComponent<PlayerInputPush>(); // Get their push input component
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
                player?.GetComponent<PlayerInputPush>()?.ActivateControl();
            }
            
            targetSpeed = GetCurrentSpeed(); // Objetivo es velocidad basada en combustible
        }
        
        // Interpolar suavemente hacia la velocidad objetivo
        currentActualSpeed = Mathf.MoveTowards(currentActualSpeed, targetSpeed, speedTransitionRate * Time.deltaTime);
        
        // Mover el coche con la velocidad interpolada Y dirección dinámica
        if (currentActualSpeed > 0.01f) // Solo mover si hay velocidad significativa
        {
            // ===== ROTATE CAR TO FACE MOVEMENT DIRECTION =====
            RotateCarTowardsDirection(moveDirection);
            
            transform.Translate(moveDirection * currentActualSpeed * Time.deltaTime, Space.World);
        }
    }

    // ===== PATH FOLLOWING METHODS (READY FOR FUTURE USE) =====
    
    private void InitializePathFollowing()
    {
        if (usePathFollowing && pathPoints != null && pathPoints.Length > 0)
        {
            currentTarget = pathPoints[0].position;
            Debug.Log("[MovCarro] Path Following initialized with " + pathPoints.Length + " waypoints.");
        }
        else
        {
            currentTarget = Vector3.zero; // Asegurar que esté en cero
            Debug.Log("[MovCarro] Path Following NOT initialized - using fixed direction");
        }
    }
    
    private Vector3 GetMovementDirection()
    {
        // If path following is disabled, use fixed direction
        if (!usePathFollowing)
        {
            Debug.Log("[MovCarro] Using fixed direction: " + direction.normalized);
            return direction.normalized;
        }
        
        // Path following is enabled - check if we have waypoints
        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.Log("[MovCarro] Path following enabled but no waypoints - using fixed direction");
            return direction.normalized;
        }
        
        // Update waypoint progress
        UpdateWaypointProgress();
        
        // Calculate smooth direction towards current target
        return CalculateSmoothDirection();
    }
    
    private void UpdateWaypointProgress()
    {
        if (currentPathIndex < pathPoints.Length)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget);
            
            if (distanceToTarget <= reachDistance)
            {
                currentPathIndex++;
                if (currentPathIndex < pathPoints.Length)
                {
                    currentTarget = pathPoints[currentPathIndex].position;
                    Debug.Log($"[MovCarro] Reached waypoint {currentPathIndex - 1}, moving to waypoint {currentPathIndex}");
                }
                else
                {
                    Debug.Log("[MovCarro] Reached final waypoint!");
                }
            }
        }
    }
    
    private Vector3 CalculateSmoothDirection()
    {
        // Calculate direction towards current target
        Vector3 targetDirection = (currentTarget - transform.position).normalized;
                
        // Usar directamente la dirección (sin suavizado por ahora para debug)
        return targetDirection;
    }

    // ===== ORIGINAL METHODS (UNCHANGED) =====

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

    // ===== DEBUG/UTILITY METHODS (FOR FUTURE USE) =====
    
    public Vector3 GetCurrentTarget()
    {
        return currentTarget;
    }
    
    public int GetCurrentWaypointIndex()
    {
        return currentPathIndex;
    }
    
    void OnDrawGizmos()
    {
        // Only draw gizmos if path following is enabled
        if (!usePathFollowing || pathPoints == null) return;
        
        // Draw waypoints and connections
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null)
            {
                // Draw waypoint sphere
                Gizmos.DrawWireSphere(pathPoints[i].position, reachDistance);
                
                // Draw line to next waypoint
                if (i > 0 && pathPoints[i-1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i-1].position, pathPoints[i].position);
                }
            }
        }
        
        // Highlight current target in play mode
        if (Application.isPlaying && usePathFollowing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget, reachDistance * 1.2f);
            
            // Draw line from car to current target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }

    // ===== NEW METHOD: CAR ROTATION =====
    private void RotateCarTowardsDirection(Vector3 moveDirection)
    {
        if (moveDirection != Vector3.zero)
        {
            // Calcular la rotación objetivo
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            // COMPENSAR los 90 grados de rotación inicial
            targetRotation *= Quaternion.Euler(0, -90, 0); // Ajusta según tu carro
            
            // Interpolar suavemente hacia la rotación objetivo
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
    }
}