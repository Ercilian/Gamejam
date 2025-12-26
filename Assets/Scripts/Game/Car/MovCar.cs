using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

public class MovCar : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1f;
    public float fastspeed = 1.2f;
    public float slowspeed = 0.8f;
    public float cur_speed = 0f;
    public float speedTransitionRate = 2f;
    public float pushSpeed = 0.5f;
    public float pushSpeedTwo = 0.85f;

    [Header("Path Following System")]
    public Transform[] pathPoints;
    public float reachDistance = 3f;
    public float pathSmoothness = 3f;
    public float rotationSpeed = 5f;

    [Header("Combustible Consumption")]
    public float fuelConsumptionPerSecond = 1f;
    public bool isFuelConsumed = true;

    [Header("Motor Sound (Single Clip)")]
    public AudioClip motorLoopClip;
    public float pitchMin = 0.8f;
    public float pitchMax = 1.5f;
    public float maxSpeedForPitch = 2.0f; // Ajusta esto a la velocidad máxima real del coche

    private AudioSource motorAudioSource;

    // ===== PRIVATE FIELDS =====
    private CarFuelSystem fuelSystem;
    private Coroutine consumeCoroutine;
    private float currentActualSpeed = 0f;
    private int currentPathIndex = 0;
    private Vector3 currentTarget;
    private bool ismoving = false;
    private Vector3 lastDirection = Vector3.forward;

    // ===== PUBLIC GETTERS =====
    public bool IsMoving() => ismoving;
    public float GetCurrentSpeedPublic() => currentActualSpeed;
    public Vector3 GetCurrentTarget() => currentTarget;
    public int GetCurrentWaypointIndex() => currentPathIndex;


    

    // ================================================= Methods =================================================




    void Start() // Search for fuel system, initialize path following, and setup motor sound
    {
        fuelSystem = GetComponent<CarFuelSystem>() ?? GetComponentInChildren<CarFuelSystem>(); // Get fuel system component

        if (isFuelConsumed) // Start fuel consumption if enabled
            consumeCoroutine = StartCoroutine(ConsumeFuel());

        InitializePathFollowing();

        // Motor sound setup
        motorAudioSource = GetComponent<AudioSource>();
        motorAudioSource.clip = motorLoopClip;
        motorAudioSource.Play();
    }

    void Update() // Main update loop for movement and sound
    {
        float targetSpeed = 0f;
        Vector3 moveDirection = GetMovementDirection(); // Get direction towards current target

        if (!fuelSystem.HasFuel()) // Stop if no fuel
        {
            ismoving = false;
            targetSpeed = HandlePush();
        }
        else // Move normally if has fuel
        {
            ismoving = true;
            ActivatePushers();
            targetSpeed = GetCurrentSpeed();
        }

        currentActualSpeed = Mathf.MoveTowards(currentActualSpeed, targetSpeed, speedTransitionRate * Time.deltaTime); // Smooth speed transition

        cur_speed = currentActualSpeed;

        if (currentActualSpeed > 0.01f)
        {
            RotateCarTowardsDirection(moveDirection);
            transform.Translate(moveDirection * currentActualSpeed * Time.deltaTime, Space.World);
        }

        UpdateMotorPitch(currentActualSpeed); // Update motor sound pitch based on speed
    }


    // ===== MOTOR SOUND SYSTEM (Single Clip, Pitch Only) =====

    private void UpdateMotorPitch(float speed) // Update motor sound pitch based on current speed
    {
        float t = Mathf.Clamp01(speed / maxSpeedForPitch); // Normalized speed (0 to 1)
        motorAudioSource.pitch = Mathf.Lerp(pitchMin, pitchMax, t); // Interpolate pitch
    }


    // ===== PATH FOLLOWING METHODS =====

    private void InitializePathFollowing() // Setup initial target for path following
    {
        currentTarget = pathPoints[0].position;
    }

    private Vector3 GetMovementDirection() // Calculate movement direction towards current target
    {
        UpdateWaypointProgress(); // Call to update waypoint if reached
        return CalculateSmoothDirection(); // Return smoothed direction
    }

    private void UpdateWaypointProgress() // Check if reached current waypoint and update to next
    {
        if (currentPathIndex < pathPoints.Length) // Ensure index is within bounds
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget); // Calculate distance to current target

            if (distanceToTarget <= reachDistance) // Check if within reach distance
            {
                currentPathIndex++;
                if (currentPathIndex < pathPoints.Length)
                {
                    currentTarget = pathPoints[currentPathIndex].position;
                }
                else
                {
                    Debug.Log("[MovCarro] Reached final waypoint!");
                }
            }
        }
    }

    private Vector3 CalculateSmoothDirection() // Smooth direction calculation towards current target
    {
        Vector3 targetDirection = (currentTarget - transform.position).normalized;
        lastDirection = Vector3.Lerp(lastDirection, targetDirection, Time.deltaTime * pathSmoothness);
        return lastDirection.normalized;
    }
    

    // ===== ORIGINAL METHODS =====

    private float HandlePush()
    {
        var playersPushing = fuelSystem?.GetPlayersPushing() ?? new List<GameObject>();
        int pushingCount = 0;
        foreach (var player in playersPushing)
        {
            var input = player?.GetComponent<PlayerInputPush>();
            input?.FollowObject(transform, GetPushSpeed(1));
            if (input?.ImPushing() == true)
                pushingCount++;
        }
        return pushingCount > 0 ? GetPushSpeed(pushingCount) : 0f;
    }

    private void ActivatePushers()
    {
        foreach (var player in fuelSystem.GetPlayersPushing())
            player?.GetComponent<PlayerInputPush>()?.ActivateControl();
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
                return pushSpeed;
        }
    }

    private float GetCurrentSpeed()
    {
        float dieselPercentage = fuelSystem.GetDieselPercentage();
        if (dieselPercentage < 0.2f)
        {
            return fastspeed;
        }
        if (dieselPercentage >= 0.8f)
        {
            return slowspeed;
        }
        return speed;
    }

    private IEnumerator ConsumeFuel()
    {
        while (fuelSystem && fuelSystem.HasFuel())
        {
            yield return new WaitForSeconds(1f);

            if (ismoving && fuelSystem.HasFuel())
            {
                fuelSystem.ConsumeDiesel(fuelConsumptionPerSecond);
            }
        }
        consumeCoroutine = null;
    }

    public void OnFuelChanged(float currentFuel, float maxFuel)
    {
        float fuelPercentage = currentFuel / maxFuel;

        if (currentFuel > 0f && consumeCoroutine == null && isFuelConsumed)
        {
            Debug.Log("[MovCarro] Combustible repuesto, reanudando consumo.");
            consumeCoroutine = StartCoroutine(ConsumeFuel());
        }
    }

    void OnDestroy()
    {
        if (consumeCoroutine != null)
        {
            StopCoroutine(consumeCoroutine);
        }
    }

    // ===== DEBUG/UTILITY METHODS =====

    void OnDrawGizmos()
    {
        if (pathPoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null)
            {
                Gizmos.DrawWireSphere(pathPoints[i].position, reachDistance);
                if (i > 0 && pathPoints[i - 1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i - 1].position, pathPoints[i].position);
                }
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget, reachDistance * 1.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }

    // ===== CAR ROTATION =====
    private void RotateCarTowardsDirection(Vector3 moveDirection)
    {
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            targetRotation *= Quaternion.Euler(0, -90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}