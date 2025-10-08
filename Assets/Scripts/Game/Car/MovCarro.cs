using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovCarro : MonoBehaviour
{
    [Header("Movimiento")]
    public Vector3 direccion = Vector3.forward; // Dirección del movimiento
    public float velocidad = 1f; // Velocidad del cubo
    public float velocidadAumentada = 2f; // Velocidad cuando queda poco combustible
    public float velocidadLenta = 0.5f; // Velocidad cuando hay mucho combustible
    
    [Header("Consumo de Combustible")]
    public float consumoPorSegundo = 1f; // Diesel que consume por segundo
    public bool consumirCombustible = true;
    
    [Header("Empujar")]
    public float velocidadEmpuje = 0.3f; // Velocidad al empujar
    public KeyCode botonEmpujar = KeyCode.E; // Botón para empujar

    private CarFuelSystem fuelSystem;
    private bool enMovimiento = false;
    private Coroutine corrutinaConsumo;
    private bool empujando = false;

    void Start()
    {
        //  Buscar CarFuelSystem en este GameObject Y en los hijos
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
            if (jugadoresEmpujando.Count > 0)
            {
                bool alguienEmpuja = false;
                foreach (var jugador in jugadoresEmpujando)
                {
                    if (jugador != null)
                    {
                        var input = jugador.GetComponent<PlayerInputEmpuje>();
                        if (input != null && input.EstaEmpujando(botonEmpujar))
                        {
                            if (!empujando)
                                Debug.Log($"[MovCarro] {jugador.name} ha comenzado a empujar el coche.");
                            alguienEmpuja = true;
                            input.DesactivarControl();
                            input.SeguirObjeto(transform, velocidadEmpuje);
                        }
                        else
                        {
                            if (empujando)
                                Debug.Log($"[MovCarro] {jugador.name} ha dejado de empujar el coche.");
                            input?.ActivarControl();
                        }
                    }
                }
                if (alguienEmpuja)
                {
                    if (!empujando)
                        Debug.Log("[MovCarro] El coche está siendo empujado.");
                    empujando = true;
                    transform.Translate(direccion.normalized * velocidadEmpuje * Time.deltaTime, Space.World);
                }
                else
                {
                    if (empujando)
                        Debug.Log("[MovCarro] El coche ha dejado de ser empujado.");
                    empujando = false;
                }
            }
            else
            {
                if (empujando)
                    Debug.Log("[MovCarro] No hay jugadores en la zona de empuje.");
                empujando = false;
            }
            return; // Si no hay diesel, no mover el carro normalmente
        }

        // Mover el objeto si hay combustible
        enMovimiento = true;
        empujando = false;
        // Obtener jugadores en la zona de empuje desde CarFuelSystem
        var jugadoresEmpujandoConCombustible = fuelSystem.GetJugadoresEmpujando();
        foreach (var jugador in jugadoresEmpujandoConCombustible)
        {
            jugador?.GetComponent<PlayerInputEmpuje>()?.ActivarControl();
        }
        float velocidadActual = GetVelocidadActual();
        transform.Translate(direccion.normalized * velocidadActual * Time.deltaTime, Space.World);
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
        corrutinaConsumo = null; // <- Añade esto para saber que la corrutina terminó
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
            Debug.Log("[MovCarro] ¡Combustible bajo! Velocidad auementada.");
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

    // Getters públicos para otros scripts
    public bool EstáMoviéndose() => enMovimiento;
    public float GetVelocidadActualPublic() => GetVelocidadActual();
}