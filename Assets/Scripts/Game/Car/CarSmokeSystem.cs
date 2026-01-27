using UnityEngine;

public class CarSmokeSystem : MonoBehaviour
{
    [Header("Smoke System Configuration")]
    public ParticleSystem smokeParticleSystem;
    public CarFuelSystem manualFuelSystemReference; // Referencia manual opcional
    
    [Header("Movement-Based Settings")]
    [Range(0f, 50f)]
    public float baseEmissionRate = 15f; // Emisión base cuando está parado
    [Range(0f, 100f)]
    public float movingEmissionRate = 35f; // Emisión cuando se mueve
    [Range(0f, 20f)]
    public float velocityInfluence = 5f; // Influencia de la velocidad en la dirección del humo
    
    [Header("Steam Train Effect")]
    [Range(0f, 15f)]
    public float initialUpwardForce = 8f; // Fuerza inicial hacia arriba (como vapor)
    [Range(0f, 1f)]
    public float upwardForceDuration = 0.3f; // Duración de la fuerza hacia arriba (0-1, porcentaje del lifetime)
    
    [Header("Wind Effect")]
    public Vector3 windDirection = Vector3.back; // Dirección del viento (por defecto hacia atrás)
    [Range(0f, 10f)]
    public float windStrength = 2f;
    [Range(0f, 1f)]
    public float windStartTime = 0.2f; // Cuándo comienza el efecto del viento (0-1, porcentaje del lifetime)
    
    [Header("Smoke Expansion Settings")]
    [Range(0.3f, 3f)]
    public float initialSmokeSize = 1f; // Tamaño inicial del humo (controlado por curva)
    [Range(1f, 5f)]
    public float finalSmokeSize = 2.5f; // Tamaño final del humo (expansión)
    [Range(0f, 1f)]
    public float expansionStartTime = 0.4f; // Cuándo empieza la expansión (0-1)
    
    [Header("Low Fuel Effect")]
    [Range(0f, 100f)]
    public float lowFuelThreshold = 20f; // Porcentaje de combustible para activar el efecto
    
    [Header("Sputter Timing (Random Ranges)")]
    public Vector2 sputterOnTimeRange = new Vector2(0.2f, 0.8f); // Rango tiempo encendido (min, max)
    public Vector2 sputterOffTimeRange = new Vector2(0.4f, 2.0f); // Rango tiempo apagado (min, max)
    public Vector2 lowFuelIntensityRange = new Vector2(0.2f, 0.6f); // Rango intensidad humo (min, max)
    
    [Header("Lifetime Settings")]
    [Range(1f, 10f)]
    public float smokeLifetime = 3f; // Tiempo de vida del humo

    private MovCar carMovement;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.SizeOverLifetimeModule sizeOverLifetime;
    
    // Curvas para el efecto de tren de vapor
    private AnimationCurve upwardForceCurve;
    private AnimationCurve windForceCurve;
    private AnimationCurve expansionCurve;
    
    // Variables para el efecto de bajo combustible
    private CarFuelSystem fuelSystem;
    private bool isLowFuel = false;
    private bool isSputtering = false;
    private float sputterTimer = 0f;
    private bool engineCurrentlyOn = true;
    
    // Valores aleatorios actuales para el efecto de bajo combustible
    private float currentSputterOnTime = 0.3f;
    private float currentSputterOffTime = 1.2f;
    private float currentLowFuelIntensity = 0.4f;
    
    // Curvas para cuando está parado (sin viento horizontal)
    private AnimationCurve stoppedHorizontalCurve;
    
    void Start()
    {
        // Obtener referencia al MovCar
        carMovement = GetComponentInParent<MovCar>();
        if (!carMovement)
            carMovement = GetComponent<MovCar>();

        if (!carMovement)
        {
            Debug.LogError("CarSmokeSystem: No se encontró MovCar component!");
            return;
        }
        
        // Obtener referencia al CarFuelSystem - primero comprobar referencia manual
        fuelSystem = manualFuelSystemReference;
        
        // Si no hay referencia manual, buscar automáticamente
        if (!fuelSystem)
            fuelSystem = GetComponentInParent<CarFuelSystem>();
        if (!fuelSystem)
            fuelSystem = GetComponent<CarFuelSystem>();
        if (!fuelSystem)
            fuelSystem = GetComponentInChildren<CarFuelSystem>();
        if (!fuelSystem && transform.parent != null)
        {
            // Buscar en hermanos (mismo nivel de jerarquía)
            fuelSystem = transform.parent.GetComponentInChildren<CarFuelSystem>();
        }
        if (!fuelSystem)
        {
            // Buscar en toda la jerarquía del carro
            Transform carRoot = transform;
            while (carRoot.parent != null && carRoot.parent.name.ToLower().Contains("car"))
            {
                carRoot = carRoot.parent;
            }
            fuelSystem = carRoot.GetComponentInChildren<CarFuelSystem>();
        }
        
        // Si no se asignó manualmente, buscar el ParticleSystem
        if (!smokeParticleSystem)
            smokeParticleSystem = GetComponent<ParticleSystem>();
            
        if (!smokeParticleSystem)
        {
            Debug.LogError("CarSmokeSystem: No se encontró ParticleSystem!");
            return;
        }
        
        // Configurar el sistema de partículas
        SetupParticleSystem();
    }
    
    void SetupParticleSystem()
    {
        if (!smokeParticleSystem)
        {
            Debug.LogError("🚂 No hay ParticleSystem asignado!");
            return;
        }
        
        // Configurar módulos del sistema de partículas (obtenerlos del ParticleSystem)
        mainModule = smokeParticleSystem.main;
        emission = smokeParticleSystem.emission;
        velocityOverLifetime = smokeParticleSystem.velocityOverLifetime;
        shapeModule = smokeParticleSystem.shape;
        sizeOverLifetime = smokeParticleSystem.sizeOverLifetime;
        
        // Configuración principal
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World; // ¡MUY IMPORTANTE!
        mainModule.startLifetime = smokeLifetime;
        mainModule.startSpeed = 0.5f; // Velocidad inicial más baja
        mainModule.startSize = 1f; // Base de 1, el tamaño real se controla con Size Over Lifetime
        
        // Configurar emisión
        emission.enabled = true;
        emission.rateOverTime = baseEmissionRate;
        
        // Configurar forma de emisión (desde el tubo de escape)
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Cone;
        shapeModule.angle = 25f; // Ángulo un poco más amplio para el efecto vapor
        shapeModule.radius = 0.1f;
        
        // Habilitar velocity over lifetime ANTES de configurarlo
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        
        // Habilitar size over lifetime para la expansión
        sizeOverLifetime.enabled = true;
        
        // Crear curvas para el efecto de tren de vapor
        CreateSteamTrainCurves();
        
        // Configurar velocidad sobre tiempo de vida con las curvas
        SetupVelocityOverLifetime();
        
        // Configurar expansión del humo
        SetupSmokeExpansion();
        
    }
    
    void CreateSteamTrainCurves()
    {
        // Curva para fuerza hacia arriba: fuerte al inicio, se reduce gradualmente
        upwardForceCurve = new AnimationCurve();
        upwardForceCurve.AddKey(0f, 1f);    // Al inicio: fuerza máxima hacia arriba
        upwardForceCurve.AddKey(upwardForceDuration, 0.3f); // Gradualmente se reduce
        upwardForceCurve.AddKey(1f, 0f);    // Al final: sin fuerza hacia arriba
        
        // Curva para efecto del viento: empieza después y aumenta gradualmente
        windForceCurve = new AnimationCurve();
        windForceCurve.AddKey(0f, 0f);           // Al inicio: sin viento
        windForceCurve.AddKey(windStartTime, 0.1f); // Comienza suavemente
        windForceCurve.AddKey(0.7f, 1f);        // Máximo efecto del viento
        windForceCurve.AddKey(1f, 0.8f);        // Se mantiene fuerte hasta el final
        
        // Curva para expansión del humo: valores ABSOLUTOS, no proporcionales
        expansionCurve = new AnimationCurve();
        expansionCurve.AddKey(0f, initialSmokeSize);                    // Al inicio: tamaño inicial exacto
        expansionCurve.AddKey(expansionStartTime, initialSmokeSize);    // Se mantiene igual hasta el tiempo de expansión
        expansionCurve.AddKey(expansionStartTime + 0.1f, initialSmokeSize * 1.2f); // Expansión suave
        expansionCurve.AddKey(0.8f, finalSmokeSize);                   // Expansión principal al tamaño final
        expansionCurve.AddKey(1f, finalSmokeSize);                     // Mantiene el tamaño final
        
        // Curva horizontal para coche parado: sin movimiento horizontal
        stoppedHorizontalCurve = new AnimationCurve();
        stoppedHorizontalCurve.AddKey(0f, 0f);    // Sin movimiento horizontal
        stoppedHorizontalCurve.AddKey(1f, 0f);    // Sin movimiento horizontal
    }
    
    void SetupVelocityOverLifetime()
    {
        if (!smokeParticleSystem || upwardForceCurve == null)
        {
            Debug.LogWarning("🚂 No se puede configurar VelocityOverLifetime - faltan componentes");
            return;
        }
        
        // Verificar que el módulo esté disponible
        try
        {
            // Configurar velocidad sobre tiempo de vida
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            
            // Configurar curvas para cada eje
            // Y (vertical): Fuerza hacia arriba al inicio
            ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve();
            yVelocity.mode = ParticleSystemCurveMode.Curve;
            yVelocity.curve = upwardForceCurve;
            yVelocity.curveMultiplier = initialUpwardForce;
            velocityOverLifetime.y = yVelocity;            
        }
        catch (System.Exception e)
        {
            Debug.LogError("🚂 Error configurando VelocityOverLifetime: " + e.Message);
        }
    }
    
    void SetupSmokeExpansion()
    {
        if (!smokeParticleSystem || expansionCurve == null)
        {
            Debug.LogWarning("🌪️ No se puede configurar expansión - faltan componentes");
            return;
        }
        
        try
        {
            // Configurar el tamaño sobre tiempo de vida
            sizeOverLifetime.enabled = true;
            
            // Crear MinMaxCurve para el tamaño con valores ABSOLUTOS
            ParticleSystem.MinMaxCurve sizeCurve = new ParticleSystem.MinMaxCurve();
            sizeCurve.mode = ParticleSystemCurveMode.Curve;
            sizeCurve.curve = expansionCurve;
            sizeCurve.curveMultiplier = 1f; // La curva ya tiene los valores absolutos
            
            // IMPORTANTE: También configurar el startSize del main module a 1, 
            // porque Size Over Lifetime multiplica el startSize
            mainModule.startSize = 1f; // Base de 1 para que la curva use valores absolutos
            
            // Aplicar la curva al tamaño
            sizeOverLifetime.size = sizeCurve;
        }
        catch (System.Exception e)
        {
            Debug.LogError("🌪️ Error configurando expansión: " + e.Message);
        }
    }
    
    void Update()
    {
        if (!carMovement || !smokeParticleSystem)
            return;
            
        // Actualizar estado de combustible y efecto de motor tosiendo
        UpdateLowFuelEffect();
        
        // Actualizar humo solo si el motor está "encendido" o no hay efecto de bajo combustible
        if (engineCurrentlyOn || !isSputtering)
        {
            UpdateSmokeBasedOnMovement();
        }
        else
        {
            // Motor "apagado" - reducir drasticamente o parar el humo
            emission.rateOverTime = 0f;
        }
    }
    
    void UpdateLowFuelEffect()
    {
        if (!fuelSystem) return;
        
        // Verificar si el combustible está bajo
        float fuelPercentage = fuelSystem.GetDieselPercentage() * 100f;
        bool shouldBeLowFuel = fuelPercentage <= lowFuelThreshold && fuelPercentage > 0f;
        
        // Activar/desactivar efecto de bajo combustible
        if (shouldBeLowFuel && !isLowFuel)
        {
            // Combustible se volvió bajo - activar efecto
            isLowFuel = true;
            isSputtering = true;
            sputterTimer = 0f;
            engineCurrentlyOn = true;
            // Generar valores aleatorios iniciales
            GenerateRandomSputterValues();
        }
        else if (!shouldBeLowFuel && isLowFuel)
        {
            // Combustible ya no está bajo - desactivar efecto
            isLowFuel = false;
            isSputtering = false;
            engineCurrentlyOn = true;
        }
        
        // Si no hay combustible, motor completamente apagado
        if (fuelPercentage <= 0f)
        {
            isLowFuel = false;
            isSputtering = false;
            engineCurrentlyOn = false;
            return;
        }
        
        // Gestionar el ciclo de encendido/apagado del motor
        if (isSputtering)
        {
            sputterTimer += Time.deltaTime;
            
            if (engineCurrentlyOn)
            {
                // Motor encendido - verificar si debe apagarse
                if (sputterTimer >= currentSputterOnTime)
                {
                    engineCurrentlyOn = false;
                    sputterTimer = 0f;
                    // Generar nuevo tiempo aleatorio para estar apagado
                    GenerateRandomSputterValues();
                }
            }
            else
            {
                // Motor apagado - verificar si debe encenderse
                if (sputterTimer >= currentSputterOffTime)
                {
                    engineCurrentlyOn = true;
                    sputterTimer = 0f;
                    // Generar nuevo tiempo aleatorio para estar encendido
                    GenerateRandomSputterValues();
                }
            }
        }
    }
    
    void GenerateRandomSputterValues()
    {
        // Generar valores aleatorios dentro de los rangos especificados
        currentSputterOnTime = Random.Range(sputterOnTimeRange.x, sputterOnTimeRange.y);
        currentSputterOffTime = Random.Range(sputterOffTimeRange.x, sputterOffTimeRange.y);
        currentLowFuelIntensity = Random.Range(lowFuelIntensityRange.x, lowFuelIntensityRange.y);
        
    }
    
    void UpdateSmokeBasedOnMovement()
    {
        bool isMoving = carMovement.IsMoving();
        float currentSpeed = carMovement.GetCurrentSpeedPublic();
        
        // Ajustar emisión basada en movimiento
        float targetEmissionRate = isMoving ? movingEmissionRate : baseEmissionRate;
        
        // Aumentar emisión con la velocidad
        if (isMoving)
        {
            targetEmissionRate += currentSpeed * 5f; // Multiplicador para hacer más visible el efecto
        }
        
        // Aplicar reducción si el combustible está bajo
        if (isLowFuel && engineCurrentlyOn)
        {
            targetEmissionRate *= currentLowFuelIntensity; // Usar intensidad aleatoria actual
        }
        
        emission.rateOverTime = targetEmissionRate;
        
        // Actualizar las curvas de velocidad con efecto de tren de vapor
        UpdateSteamTrainEffect(isMoving, currentSpeed);
    }
    
    void UpdateSteamTrainEffect(bool isMoving, float currentSpeed)
    {
        // Verificar que las curvas estén creadas
        if (upwardForceCurve == null || stoppedHorizontalCurve == null)
        {
            CreateSteamTrainCurves();
        }
        
        // Configurar velocidad Y (vertical) - siempre tiene la fuerza inicial hacia arriba
        ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve();
        yVelocity.mode = ParticleSystemCurveMode.Curve;
        yVelocity.curve = upwardForceCurve;
        yVelocity.curveMultiplier = initialUpwardForce;
        velocityOverLifetime.y = yVelocity;
        
        // Configurar velocidad X y Z (horizontal) - depende del movimiento
        if (isMoving)
        {
            // COCHE EN MOVIMIENTO: Humo va hacia atrás por el viento del movimiento
            Vector3 effectiveWindDirection = windDirection;
            float effectiveWindStrength = windStrength;
            
            // ===== BUSCAR EL RIGIDBODY DEL CARRO CORRECTAMENTE =====
            Rigidbody carRigidbody = null;
            
            // Opción 1: Buscar en el MovCarro que ya tenemos
            if (carMovement != null)
            {
                carRigidbody = carMovement.GetComponent<Rigidbody>();
            }
            
            // Opción 2: Si no se encuentra, buscar en los padres
            if (carRigidbody == null)
            {
                Transform current = transform;
                while (current != null && carRigidbody == null)
                {
                    carRigidbody = current.GetComponent<Rigidbody>();
                    current = current.parent;
                }
            }
            
            // Calcular dirección opuesta al movimiento del carro
            if (carRigidbody != null && carRigidbody.linearVelocity.magnitude > 0.1f)
            {
                Vector3 oppositeDirection = -carRigidbody.linearVelocity.normalized;
                effectiveWindDirection = (oppositeDirection * velocityInfluence + windDirection).normalized;
                effectiveWindStrength = windStrength + currentSpeed * 0.5f;
            }
            else
            {
                // Si no hay Rigidbody o no se mueve, usar dirección del transform del carro
                if (carMovement != null)
                {
                    Vector3 carForward = carMovement.transform.forward;
                    Vector3 oppositeDirection = -carForward;
                    effectiveWindDirection = (oppositeDirection * velocityInfluence + windDirection).normalized;
                    effectiveWindStrength = windStrength + currentSpeed * 0.5f;
                }
            }
            
            // Aplicar viento horizontal cuando se mueve
            ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve();
            xVelocity.mode = ParticleSystemCurveMode.Curve;
            xVelocity.curve = windForceCurve;
            xVelocity.curveMultiplier = effectiveWindDirection.x * effectiveWindStrength;
            velocityOverLifetime.x = xVelocity;
            
            ParticleSystem.MinMaxCurve zVelocity = new ParticleSystem.MinMaxCurve();
            zVelocity.mode = ParticleSystemCurveMode.Curve;
            zVelocity.curve = windForceCurve;
            zVelocity.curveMultiplier = effectiveWindDirection.z * effectiveWindStrength;
            velocityOverLifetime.z = zVelocity;
        }
        else
        {
            // COCHE PARADO: Humo sube verticalmente (sin viento horizontal)
            // Usar curvas para mantener consistencia en el modo
            ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve();
            xVelocity.mode = ParticleSystemCurveMode.Curve;
            xVelocity.curve = stoppedHorizontalCurve;
            xVelocity.curveMultiplier = 0f; // Sin movimiento horizontal
            velocityOverLifetime.x = xVelocity;
            
            ParticleSystem.MinMaxCurve zVelocity = new ParticleSystem.MinMaxCurve();
            zVelocity.mode = ParticleSystemCurveMode.Curve;
            zVelocity.curve = stoppedHorizontalCurve;
            zVelocity.curveMultiplier = 0f; // Sin movimiento horizontal
            velocityOverLifetime.z = zVelocity;
        }
    }
    
    // Método público para activar/desactivar el humo
    public void SetSmokeActive(bool active)
    {
        if (smokeParticleSystem)
        {
            if (active && !smokeParticleSystem.isPlaying)
                smokeParticleSystem.Play();
            else if (!active && smokeParticleSystem.isPlaying)
                smokeParticleSystem.Stop();
        }
    }
    
    // Configurar intensidad del humo manualmente
    public void SetSmokeIntensity(float intensity)
    {
        if (smokeParticleSystem)
        {
            emission.rateOverTime = baseEmissionRate * intensity;
        }
    }

    // Método público para otros scripts
    public bool IsEngineRunning()
    {
        return engineCurrentlyOn;
    }
    
    public bool IsLowOnFuel()
    {
        return isLowFuel;
    }
    
    void OnValidate()
    {
        // Actualizar configuración en tiempo real en el editor
        if (Application.isPlaying && smokeParticleSystem && velocityOverLifetime.enabled)
        {
            // Solo actualizar si ya está completamente inicializado
            try
            {
                CreateSteamTrainCurves();
                SetupVelocityOverLifetime();
                SetupSmokeExpansion();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("🚂 OnValidate: No se pudo actualizar - " + e.Message);
            }
        }
    }
}