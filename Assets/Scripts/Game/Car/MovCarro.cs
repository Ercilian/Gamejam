using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovCarro : MonoBehaviour
{
    [Header("Movimiento")]
    public Vector3 direccion = Vector3.forward;
    public float velocidad = 1f;
    public float velocidadAumentada = 2f;
    public float velocidadLenta = 0.5f;
    
    [Header("Consumo de Combustible")]
    public float consumoPorSegundo = 1f;
    public bool consumirCombustible = true;
    
    [Header("Empujar")]
    public float velocidadEmpuje = 0.5f; // Velocidad base con 1 jugador
    public float velocidadEmpujeDos = 0.85f; // Velocidad con 2 jugadores
    
    private CarFuelSystem fuelSystem;
    private bool enMovimiento = false;
    private Coroutine corrutinaConsumo;
    private bool empujando = false;
    private int jugadoresEmpujandoCount = 0; // Contador de jugadores empujando

    // Getters públicos para otros scripts
    public bool EstáMoviéndose() => enMovimiento;
    public float GetVelocidadActualPublic() => GetVelocidadActual();
    public int GetJugadoresEmpujandoCount() => jugadoresEmpujandoCount; // NUEVO: Para debug/UI


    void Start()
    {
        // Buscar CarFuelSystem en este GameObject Y en los hijos
        fuelSystem = GetComponent<CarFuelSystem>();
        if (!fuelSystem)
        {
            fuelSystem = GetComponentInChildren<CarFuelSystem>();
        }
        
        if (!fuelSystem)
        {
            Debug.LogError("[MovCarro] No se encontró CarFuelSystem en este GameObject ni en sus hijos!");
            return;
        }
        else
        {
            Debug.Log($"[MovCarro] CarFuelSystem encontrado en: {fuelSystem.gameObject.name}");
        }
        
        // Iniciar consumo de combustible
        if (consumirCombustible)
        {
            corrutinaConsumo = StartCoroutine(ConsumoCombustible());
        }
    }

    void Update()
    {
        // Verificar si hay combustible
        if (!fuelSystem || !fuelSystem.HasFuel())
        {
            enMovimiento = false;

            // Obtener jugadores en la zona de empuje desde CarFuelSystem
            var jugadoresEmpujando = fuelSystem.GetJugadoresEmpujando();
            jugadoresEmpujandoCount = 0; // Reset contador
            
            if (jugadoresEmpujando.Count > 0)
            {
                foreach (var jugador in jugadoresEmpujando)
                {
                    if (jugador != null)
                    {
                        var input = jugador.GetComponent<PlayerInputEmpuje>();
                        if (input != null)
                        {
                            input.SeguirObjeto(transform, GetVelocidadEmpuje(1)); // Velocidad base para seguir
                            
                            // Verificar si ESTE jugador específico está empujando
                            if (input.EstoyEmpujandoYo())
                            {
                                jugadoresEmpujandoCount++; // Incrementar contador
                            }
                        }
                    }
                }
                
                // Mover el carro solo si al menos uno está empujando
                if (jugadoresEmpujandoCount > 0)
                {
                    if (!empujando)
                    {
                        Debug.Log($"[MovCarro] El coche está siendo empujado por {jugadoresEmpujandoCount} jugador(es).");
                        empujando = true;
                    }
                    
                    // NUEVO: Velocidad basada en el número de jugadores empujando
                    float velocidadActualEmpuje = GetVelocidadEmpuje(jugadoresEmpujandoCount);
                    transform.Translate(direccion.normalized * velocidadActualEmpuje * Time.deltaTime, Space.World);
                    
                    // Log solo cuando cambia el número de jugadores
                    if (Time.frameCount % 60 == 0) // Log cada segundo aprox
                    {
                        Debug.Log($"[MovCarro] {jugadoresEmpujandoCount} jugador(es) empujando - Velocidad: {velocidadActualEmpuje:F2}");
                    }
                }
                else
                {
                    if (empujando)
                    {
                        Debug.Log("[MovCarro] El coche ha dejado de ser empujado.");
                        empujando = false;
                    }
                }
            }
            else
            {
                if (empujando)
                {
                    Debug.Log("[MovCarro] No hay jugadores en la zona de empuje.");
                    empujando = false;
                }
                jugadoresEmpujandoCount = 0;
            }
            return; // Si no hay diesel, no mover el carro normalmente
        }

        // Mover el objeto si hay combustible
        enMovimiento = true;  
        empujando = false;
        jugadoresEmpujandoCount = 0;
        
        // Reactivar control de todos los jugadores cuando hay combustible
        var jugadoresEmpujandoConCombustible = fuelSystem.GetJugadoresEmpujando();
        foreach (var jugador in jugadoresEmpujandoConCombustible)
        {
            jugador?.GetComponent<PlayerInputEmpuje>()?.ActivarControl();
        }
        
        float velocidadActual = GetVelocidadActual();
        transform.Translate(direccion.normalized * velocidadActual * Time.deltaTime, Space.World);
    }

    // NUEVO: Método para calcular velocidad basada en número de jugadores empujando
    private float GetVelocidadEmpuje(int numJugadores)
    {
        switch (numJugadores)
        {
            case 1:
                return velocidadEmpuje; // Velocidad base
            case 2:
                return velocidadEmpujeDos; // Un poco más rápido
            default:
                return velocidadEmpuje; // Fallback
        }
    }

    private float GetVelocidadActual()
    {
        if (!fuelSystem) return 0f;
        
        float porcentajeCombustible = fuelSystem.GetDieselPercentage();
        
        // Aumentar velocidad si queda poco combustible
        if (porcentajeCombustible < 0.2f) // Menos del 20%
        {
            return velocidadAumentada;
        }
        if (porcentajeCombustible >= 0.8f) // Más del 80%
        {
            return velocidadLenta; // Velocidad reducida
        }
        
        return velocidad;
    }

    private IEnumerator ConsumoCombustible()
    {
        while (fuelSystem && fuelSystem.HasFuel())
        {
            yield return new WaitForSeconds(1f);

            // Solo consumir si el carro se está moviendo
            if (enMovimiento && fuelSystem.HasFuel())
            {
                fuelSystem.ConsumeDiesel(consumoPorSegundo);
            }
        }

        Debug.Log("[MovCarro] ¡Se acabó el diesel! El carro se ha detenido.");
        corrutinaConsumo = null;
    }

    // Método llamado por CarFuelSystem cuando cambia el combustible
    public void OnFuelChanged(float currentFuel, float maxFuel)
    {
        float fuelPercentage = currentFuel / maxFuel;

        // Si el combustible acaba de pasar de 0 a >0, reiniciar la corrutina de consumo
        if (currentFuel > 0f && corrutinaConsumo == null && consumirCombustible)
        {
            Debug.Log("[MovCarro] Combustible repuesto, reanudando consumo.");
            corrutinaConsumo = StartCoroutine(ConsumoCombustible());
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
            enMovimiento = false;
        }
    }

    void OnDestroy()
    {
        // Detener corrutina al destruir el objeto
        if (corrutinaConsumo != null)
        {
            StopCoroutine(corrutinaConsumo);
        }
    }

}