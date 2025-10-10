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

    // Getters públicos para otros scripts
    public bool IsMoving() => ismoving;
    public float GetCurrentSpeedPublic() => GetCurrentSpeed();
    public int GetPlayersPushingCount() => playersPushingCount;


    void Start()
    {
        fuelSystem = GetComponent<CarFuelSystem>(); // Get CarFuelSystem on this GameObject
        
        if (!fuelSystem) // If not found, check children
        {
            fuelSystem = GetComponentInChildren<CarFuelSystem>();
        }
        
        if (isFuelConsumed) // Start fuel consumption if enabled
        {
            consumeCoroutine = StartCoroutine(ConsumoCombustible());
        }
    }

    void Update()
    {
        if (!fuelSystem || !fuelSystem.HasFuel()) // If no fuel, stop moving normally
        {
            ismoving = false;

            // Obtener jugadores en la zona de empuje desde CarFuelSystem
            var jugadoresEmpujando = fuelSystem.GetJugadoresEmpujando();
            playersPushingCount = 0; // Reset contador
            
            if (jugadoresEmpujando.Count > 0)
            {
                foreach (var jugador in jugadoresEmpujando)
                {
                    if (jugador != null)
                    {
                        var input = jugador.GetComponent<PlayerInputEmpuje>();
                        if (input != null)
                        {
                            input.FollowObject(transform, GetVelocidadEmpuje(1)); // Velocidad base para seguir
                            
                            // Verificar si ESTE jugador específico está empujando
                            if (input.ImPushing())
                            {
                                playersPushingCount++; // Incrementar contador
                            }
                        }
                    }
                }
                
                // Mover el carro solo si al menos uno está empujando
                if (playersPushingCount > 0)
                {
                    if (!isPushing)
                    {
                        Debug.Log($"[MovCarro] El coche está siendo empujado por {playersPushingCount} jugador(es).");
                        isPushing = true;
                    }
                    
                    // NUEVO: Velocidad basada en el número de jugadores empujando
                    float velocidadActualEmpuje = GetVelocidadEmpuje(playersPushingCount);
                    transform.Translate(direction.normalized * velocidadActualEmpuje * Time.deltaTime, Space.World);
                    
                    // Log solo cuando cambia el número de jugadores
                    if (Time.frameCount % 60 == 0) // Log cada segundo aprox
                    {
                        Debug.Log($"[MovCarro] {playersPushingCount} jugador(es) empujando - Velocidad: {velocidadActualEmpuje:F2}");
                    }
                }
                else
                {
                    if (isPushing)
                    {
                        Debug.Log("[MovCarro] El coche ha dejado de ser empujado.");
                        isPushing = false;
                    }
                }
            }
            else
            {
                if (isPushing)
                {
                    Debug.Log("[MovCarro] No hay jugadores en la zona de empuje.");
                    isPushing = false;
                }
                playersPushingCount = 0;
            }
            return; // Si no hay diesel, no mover el carro normalmente
        }

        // Mover el objeto si hay combustible
        ismoving = true;  
        isPushing = false;
        playersPushingCount = 0;
        
        // Reactivar control de todos los jugadores cuando hay combustible
        var jugadoresEmpujandoConCombustible = fuelSystem.GetJugadoresEmpujando();
        foreach (var jugador in jugadoresEmpujandoConCombustible)
        {
            jugador?.GetComponent<PlayerInputEmpuje>()?.ActivateControl();
        }
        
        float velocidadActual = GetCurrentSpeed();
        transform.Translate(direction.normalized * velocidadActual * Time.deltaTime, Space.World);
    }

    // NUEVO: Método para calcular velocidad basada en número de jugadores empujando
    private float GetVelocidadEmpuje(int numJugadores)
    {
        switch (numJugadores)
        {
            case 1:
                return pushSpeed; // Velocidad base
            case 2:
                return pushSpeedTwo; // Un poco más rápido
            default:
                return pushSpeed; // Fallback
        }
    }

    private float GetCurrentSpeed()
    {
        if (!fuelSystem) return 0f;
        
        float porcentajeCombustible = fuelSystem.GetDieselPercentage();
        
        // Aumentar velocidad si queda poco combustible
        if (porcentajeCombustible < 0.2f) // Menos del 20%
        {
            return fastspeed;
        }
        if (porcentajeCombustible >= 0.8f) // Más del 80%
        {
            return slowspeed; // Velocidad reducida
        }
        
        return speed;
    }

    private IEnumerator ConsumoCombustible()
    {
        while (fuelSystem && fuelSystem.HasFuel())
        {
            yield return new WaitForSeconds(1f);

            // Solo consumir si el carro se está moviendo
            if (ismoving && fuelSystem.HasFuel())
            {
                fuelSystem.ConsumeDiesel(fuelConsumptionPerSecond);
            }
        }

        Debug.Log("[MovCarro] ¡Se acabó el diesel! El carro se ha detenido.");
        consumeCoroutine = null;
    }

    // Método llamado por CarFuelSystem cuando cambia el combustible
    public void OnFuelChanged(float currentFuel, float maxFuel)
    {
        float fuelPercentage = currentFuel / maxFuel;

        // Si el combustible acaba de pasar de 0 a >0, reiniciar la corrutina de consumo
        if (currentFuel > 0f && consumeCoroutine == null && isFuelConsumed)
        {
            Debug.Log("[MovCarro] Combustible repuesto, reanudando consumo.");
            consumeCoroutine = StartCoroutine(ConsumoCombustible());
        }

        if (fuelPercentage < 0.2f && fuelPercentage > 0f)
        {
            Debug.Log("[MovCarro] ¡Combustible bajo! Velocidad aumentada.");
        }
        if (fuelPercentage >= 0.8f)
        {
            Debug.Log("[MovCarro] Combustible alto. Velocidad lenta.");
        }
        else if (currentFuel <= 0f)
        {
            Debug.Log("[MovCarro] ¡Sin combustible! Carro detenido.");
            ismoving = false;
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