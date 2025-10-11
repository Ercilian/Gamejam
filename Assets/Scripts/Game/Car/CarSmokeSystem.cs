using UnityEngine;

public class CarSmokeSystem : MonoBehaviour
{
    [Header("Smoke System Configuration")]
    public ParticleSystem smokeParticleSystem;
    
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
    
    [Header("Lifetime Settings")]
    [Range(1f, 10f)]
    public float smokeLifetime = 3f; // Tiempo de vida del humo
    
    private MovCarro carMovement;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.SizeOverLifetimeModule sizeOverLifetime;
    
    // Curvas para el efecto de tren de vapor
    private AnimationCurve upwardForceCurve;
    private AnimationCurve windForceCurve;
    private AnimationCurve expansionCurve;
    
    void Start()
    {
        // Obtener referencia al MovCarro
        carMovement = GetComponentInParent<MovCarro>();
        if (!carMovement)
            carMovement = GetComponent<MovCarro>();
            
        if (!carMovement)
        {
            Debug.LogError("CarSmokeSystem: No se encontró MovCarro component!");
            return;
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
        
        Debug.Log("🚂 ParticleSystem configurado correctamente");
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
        
        Debug.Log("🚂 Curvas creadas - Fuerza: " + initialUpwardForce + ", Expansión: " + initialSmokeSize + " → " + finalSmokeSize);
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
            
            Debug.Log("🚂 Configuración inicial completa - Velocidad Y configurada con curva");
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
            
            Debug.Log("🌪️ Expansión ABSOLUTA configurada - Inicial: " + initialSmokeSize + ", Final: " + finalSmokeSize);
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
            
        UpdateSmokeBasedOnMovement();
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
        
        emission.rateOverTime = targetEmissionRate;
        
        // Actualizar las curvas de velocidad con efecto de tren de vapor
        UpdateSteamTrainEffect(isMoving, currentSpeed);
    }
    
    void UpdateSteamTrainEffect(bool isMoving, float currentSpeed)
    {
        // Configurar velocidad Y (vertical) - siempre tiene la fuerza inicial hacia arriba
        ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve();
        yVelocity.mode = ParticleSystemCurveMode.Curve;
        yVelocity.curve = upwardForceCurve;
        yVelocity.curveMultiplier = initialUpwardForce;
        velocityOverLifetime.y = yVelocity;
        
        // Configurar velocidad X y Z (horizontal) - efecto del viento
        Vector3 effectiveWindDirection = windDirection;
        float effectiveWindStrength = windStrength;
        
        if (isMoving)
        {
            // Calcular dirección opuesta al movimiento del carro para mayor realismo
            Vector3 carVelocity = transform.parent.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
            if (carVelocity.magnitude > 0.1f)
            {
                Vector3 oppositeDirection = -carVelocity.normalized;
                effectiveWindDirection = (oppositeDirection * velocityInfluence + windDirection).normalized;
                effectiveWindStrength = windStrength + currentSpeed * 0.5f;
            }
        }
        
        // Configurar curvas para X y Z con efecto del viento
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
        
        // Debug para verificar que está funcionando
        if (Time.frameCount % 60 == 0) // Solo cada 60 frames para no spam
        {
            Debug.Log($"🚂 Vapor actualizado - Fuerza arriba: {initialUpwardForce}, Viento: {effectiveWindStrength}, Movimiento: {isMoving}");
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
    
    // Método para probar el efecto de vapor en el editor
    [ContextMenu("Test Steam Effect")]
    public void TestSteamEffect()
    {
        if (!smokeParticleSystem)
        {
            Debug.LogError("🚂 No hay ParticleSystem asignado!");
            return;
        }
        
        if (!Application.isPlaying)
        {
            Debug.LogWarning("🚂 El juego debe estar ejecutándose para probar el efecto");
            return;
        }
        
        Debug.Log("🚂 Probando efecto de vapor...");
        
        // Forzar valores altos para prueba
        initialSmokeSize = 3f;
        baseEmissionRate = 40f;
        movingEmissionRate = 70f;
        
        // Reinicializar completamente
        SetupParticleSystem();
        
        // Forzar emisión alta para ver el efecto
        if (emission.enabled)
        {
            emission.rateOverTime = movingEmissionRate;
            Debug.Log("🚂 PRUEBA - Tamaño: " + initialSmokeSize + ", Emisión: " + movingEmissionRate);
        }
    }
    
    // Método para ajustar parámetros en tiempo real
    public void UpdateSteamParameters(float upwardForce, float windStrength, float upwardDuration)
    {
        initialUpwardForce = upwardForce;
        this.windStrength = windStrength;
        upwardForceDuration = upwardDuration;
        
        if (Application.isPlaying)
        {
            CreateSteamTrainCurves();
            SetupVelocityOverLifetime();
        }
        
        Debug.Log($"🚂 Parámetros actualizados - Fuerza: {upwardForce}, Viento: {windStrength}, Duración: {upwardDuration}");
    }
    
    // Método para ajustar la intensidad visual rápidamente
    [ContextMenu("Increase Smoke Thickness")]
    public void IncreaseSmokeThickness()
    {
        initialSmokeSize = Mathf.Min(initialSmokeSize + 0.2f, 3f);
        if (Application.isPlaying)
        {
            mainModule.startSize = initialSmokeSize;
            CreateSteamTrainCurves();
            SetupSmokeExpansion();
        }
        Debug.Log($"🌪️ Grosor aumentado a: {initialSmokeSize}");
    }
    
    [ContextMenu("Decrease Smoke Thickness")]
    public void DecreaseSmokeThickness()
    {
        initialSmokeSize = Mathf.Max(initialSmokeSize - 0.2f, 0.5f);
        if (Application.isPlaying)
        {
            mainModule.startSize = initialSmokeSize;
            CreateSteamTrainCurves();
            SetupSmokeExpansion();
        }
        Debug.Log($"🌪️ Grosor reducido a: {initialSmokeSize}");
    }
    
    [ContextMenu("Make Smoke VERY Thick")]
    public void MakeSmokeVeryThick()
    {
        initialSmokeSize = 3.5f;
        finalSmokeSize = 8f;
        baseEmissionRate = 40f;
        movingEmissionRate = 70f;
        
        if (Application.isPlaying)
        {
            mainModule.startSize = initialSmokeSize;
            emission.rateOverTime = baseEmissionRate;
            CreateSteamTrainCurves();
            SetupSmokeExpansion();
        }
        Debug.Log($"🔥 HUMO MUY GRUESO - Tamaño: {initialSmokeSize}, Emisión: {baseEmissionRate}");
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