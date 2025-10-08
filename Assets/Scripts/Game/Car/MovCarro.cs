using UnityEngine;
using System.Collections;

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
    
    private CarFuelSystem fuelSystem;
    private bool enMovimiento = false;
    private Coroutine corrutinaConsumo;

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
            return; // Si no hay diesel, no mover el carro
        }

        // Mover el objeto si hay combustible
        enMovimiento = true;
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
    }

    // Método llamado por CarFuelSystem cuando cambia el combustible
    public void OnFuelChanged(float currentFuel, float maxFuel)
    {
        float fuelPercentage = currentFuel / maxFuel;
        
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