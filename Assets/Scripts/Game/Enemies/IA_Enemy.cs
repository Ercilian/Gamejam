using UnityEngine;
using UnityEngine.Serialization;

public class IA_Enemy : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad cuando va hacia el camión")]
    public float velocidadBase = 2f;
    [Tooltip("Velocidad cuando persigue al jugador")]
    public float velocidadPersecucion = 5f;
    [Tooltip("Distancia para detectar al jugador")]
    public float rangoDeteccion = 8f;
    [Tooltip("Distancia para perder al jugador y volver al camión")]
    public float rangoPerdida = 12f;
    [Tooltip("Distancia mínima al objetivo antes de detenerse")]
    public float distanciaMinima = 1f;
    
    [Header("Evitación de Obstáculos")]
    [Tooltip("Detectar y evitar obstáculos")]
    public bool evitarObstaculos = true;
    [Tooltip("Distancia para detectar obstáculos")]
    public float distanciaDeteccionObstaculo = 2.5f;
    [Tooltip("Radio del SphereCast para mejor detección")]
    public float radioDeteccion = 0.3f;
    [Tooltip("Ángulos de los rayos de detección")]
    public float[] angulosDeteccion = { 0f, 25f, -25f, 50f, -50f, 75f, -75f };
    [Tooltip("Layers que se consideran obstáculos")]
    public LayerMask capasObstaculos = -1;
    [Tooltip("Distancia mínima a mantener de las paredes")]
    public float distanciaSeguridad = 1.5f;
    [Tooltip("Fuerza de repulsión cuando está cerca de obstáculos")]
    public float fuerzaRepulsion = 2f;
    [Tooltip("Detectar obstáculos laterales")]
    public bool detectarLaterales = true;
    [Tooltip("Distancia de detección lateral")]
    public float distanciaDeteccionLateral = 1f;
    [Tooltip("Tiempo para detectar si está atascado")]
    public float tiempoDeteccionAtasco = 1f;
    [Tooltip("Distancia mínima de movimiento para no considerarse atascado")]
    public float distanciaMinMovimiento = 0.1f;
    [Tooltip("Velocidad de suavizado de dirección (menor = más suave)")]
    public float suavidadDireccion = 0.15f;
    [Tooltip("Velocidad de rotación (mayor = gira más rápido)")]
    public float velocidadRotacion = 8f;
    [Tooltip("Tiempo mínimo manteniendo una dirección de esquive")]
    public float tiempoPersistenciaEsquive = 0.5f;
    
    [Header("Objetivos")]
    [Tooltip("Tag del camión a seguir (selecciona el tag en el desplegable)")]
    [IAEnemyTagSelector]
    public string camionTag = "Car";
    
    private Transform camion; // caché en runtime
    [Tooltip("Tag del jugador a perseguir")]
    public string tagJugador = "Player";
    
    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool mostrarGizmos = true;
    
    // Estados internos
    private enum EstadoIA
    {
        IrAlCamion,      // Comportamiento por defecto
        PerseguirJugador, // Cuando detecta un jugador
        Patrullando      // Para futuras expansiones
    }
    
    private EstadoIA estadoActual = EstadoIA.IrAlCamion;
    private Transform jugadorObjetivo;
    // Eliminado camionTransform; usamos directamente 'camion'
    private Vector3 ultimaPosicionCamion;
    private float tiempoUltimaDeteccion;
    private Rigidbody rb;
    
    // Sistema anti-atasco
    private Vector3 posicionAnterior;
    private float tiempoEnMismaPosicion;
    private Vector3 direccionAlternativa;
    private float tiempoUsandoDireccionAlternativa;
    
    // Sistema de suavizado
    private Vector3 direccionActual;
    private Vector3 velocidadSuavizado; // Para SmoothDamp
    private Vector3 ultimaDireccionEsquive;
    private float tiempoUltimaEsquive;
    
    // Componentes opcionales
    private Animator animator;
    private EnemyAttack enemyAttack; // Componente de ataque
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        enemyAttack = GetComponent<EnemyAttack>();

        // Encontrar camión por tag seleccionado
        if (!string.IsNullOrEmpty(camionTag))
        {
            GameObject camionObj = GameObject.FindGameObjectWithTag(camionTag);
            if (camionObj == null)
            {
                // Fallback: probar tags comunes
                camionObj = GameObject.FindGameObjectWithTag("Car");
                if (camionObj == null) camionObj = GameObject.FindWithTag("Camion");
            }
            if (camionObj != null)
            {
                camion = camionObj.transform;
            }
            else if (mostrarDebug)
            {
                Debug.LogWarning($"[{name}] No se encontró camión con tag '{camionTag}'.");
            }
        }

        if (camion != null)
            ultimaPosicionCamion = camion.position;
        
        posicionAnterior = transform.position;
        direccionActual = transform.forward;
    }
    void Update()
    {
        // Actualizar posición del camión
        if (camion != null)
            ultimaPosicionCamion = camion.position;

        // Máquina de estados
        switch (estadoActual)
        {
            case EstadoIA.IrAlCamion:
                ActualizarEstadoCamion();
                break;
            case EstadoIA.PerseguirJugador:
                ActualizarEstadoPersecucion();
                break;
        }

        // Actualizar animación si existe
        if (animator != null)
        {
            float velocidadActual = rb != null ? rb.linearVelocity.magnitude : 0f;
            animator.SetFloat("Velocidad", velocidadActual);
            animator.SetBool("Persiguiendo", estadoActual == EstadoIA.PerseguirJugador);
        }
    }
    
    void ActualizarEstadoCamion()
    {
        // Buscar jugadores cercanos
        Transform jugadorCercano = BuscarJugadorCercano();
        
        if (jugadorCercano != null)
        {
            // Cambiar a persecución
            jugadorObjetivo = jugadorCercano;
            estadoActual = EstadoIA.PerseguirJugador;
            tiempoUltimaDeteccion = Time.time;
            
            if (mostrarDebug)
            return;
        }
        
        // Moverse hacia el camión
        if (camion != null)
        {
            MoverHacia(ultimaPosicionCamion, velocidadBase);
        }
        else
        {
            // Si no hay camión, quedarse quieto o buscar uno
            if (mostrarDebug && Time.frameCount % 60 == 0) // Log cada segundo aprox
                Debug.LogWarning($"[{name}] No hay camión asignado");
        }
    }
    
    void ActualizarEstadoPersecucion()
    {
        if (jugadorObjetivo == null)
        {
            // Jugador desapareció, volver al camión
            estadoActual = EstadoIA.IrAlCamion;
            if (mostrarDebug)
            return;
        }
        
        // Verificar si el jugador está caído (downed)
        PlayerReviveSystem reviveSystem = jugadorObjetivo.GetComponent<PlayerReviveSystem>();
        if (reviveSystem != null && reviveSystem.IsDowned())
        {
            // Jugador está caído, dejar de perseguirlo y volver al camión
            if (mostrarDebug)
                Debug.Log($"[{name}] Jugador {jugadorObjetivo.name} está caído, volviendo al camión.");
            
            jugadorObjetivo = null;
            estadoActual = EstadoIA.IrAlCamion;
            return;
        }
        
        float distanciaAJugador = Vector3.Distance(transform.position, jugadorObjetivo.position);
        
        // Verificar si el jugador está muy lejos
        if (distanciaAJugador > rangoPerdida)
        {
            // Perder al jugador y volver al camión
            
            jugadorObjetivo = null;
            estadoActual = EstadoIA.IrAlCamion;
            return;
        }
        
        // Intentar atacar si está en rango
        if (enemyAttack != null && enemyAttack.CanAttack())
        {
            bool atacoExitosamente = enemyAttack.TryAttack(jugadorObjetivo);
            if (atacoExitosamente && mostrarDebug)
            {
                Debug.Log($"[{name}] Atacó al jugador: {jugadorObjetivo.name}");
            }
        }
        
        // Perseguir al jugador
        MoverHacia(jugadorObjetivo.position, velocidadPersecucion);
        tiempoUltimaDeteccion = Time.time;
    }
    
    Transform BuscarJugadorCercano()
    {
        // Buscar todos los objetos con el tag de jugador
        GameObject[] jugadores = GameObject.FindGameObjectsWithTag(tagJugador);
        
        Transform jugadorMasCercano = null;
        float distanciaMenor = rangoDeteccion;
        
        foreach (GameObject jugador in jugadores)
        {
            if (jugador == null) continue;
            
            // Ignorar jugadores caídos
            PlayerReviveSystem reviveSystem = jugador.GetComponent<PlayerReviveSystem>();
            if (reviveSystem != null && reviveSystem.IsDowned())
                continue;
            
            float distancia = Vector3.Distance(transform.position, jugador.transform.position);
            
            if (distancia < distanciaMenor)
            {
                distanciaMenor = distancia;
                jugadorMasCercano = jugador.transform;
            }
        }
        
        return jugadorMasCercano;
    }
    
    void MoverHacia(Vector3 objetivo, float velocidad)
    {
        Vector3 direccionDeseada = (objetivo - transform.position).normalized;
        float distancia = Vector3.Distance(transform.position, objetivo);
        
        // No moverse si está muy cerca
        if (distancia < distanciaMinima)
        {
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
            return;
        }
        
        // Detectar si está atascado
        float distanciaMovida = Vector3.Distance(transform.position, posicionAnterior);
        if (distanciaMovida < distanciaMinMovimiento)
        {
            tiempoEnMismaPosicion += Time.deltaTime;
            
            // Si lleva mucho tiempo atascado, buscar dirección alternativa
            if (tiempoEnMismaPosicion > tiempoDeteccionAtasco)
            {
                if (tiempoUsandoDireccionAlternativa <= 0)
                {
                    // Calcular nueva dirección alternativa (perpendicular + hacia objetivo)
                    Vector3 perpendicular = new Vector3(-direccionDeseada.z, 0, direccionDeseada.x);
                    if (Random.value > 0.5f) perpendicular = -perpendicular;
                    
                    direccionAlternativa = (perpendicular + direccionDeseada * 0.3f).normalized;
                    tiempoUsandoDireccionAlternativa = 2f; // Usar durante 2 segundos
                    
                    if (mostrarDebug)
                        Debug.Log($"[{name}] ¡Atascado! Usando dirección alternativa");
                }
            }
        }
        else
        {
            // Se está moviendo correctamente
            tiempoEnMismaPosicion = 0;
        }
        
        posicionAnterior = transform.position;
        
        Vector3 direccionFinal = direccionDeseada;
        
        // Usar dirección alternativa si está activa
        if (tiempoUsandoDireccionAlternativa > 0)
        {
            direccionFinal = direccionAlternativa;
            tiempoUsandoDireccionAlternativa -= Time.deltaTime;
        }
        // Aplicar evitación de obstáculos si está activado
        else if (evitarObstaculos)
        {
            Vector3 direccionEsquive = DetectarYEvitarObstaculos(direccionDeseada);
            Vector3 direccionRepulsion = Vector3.zero;
            
            // Detectar obstáculos laterales para mantenerse alejado de paredes
            if (detectarLaterales)
            {
                direccionRepulsion = DetectarObstaculosLaterales();
            }
            
            if (direccionEsquive != Vector3.zero)
            {
                // Mantener persistencia: si acabamos de esquivar, continuar en esa dirección
                if (Time.time - tiempoUltimaEsquive < tiempoPersistenciaEsquive)
                {
                    direccionFinal = Vector3.Lerp(direccionDeseada, ultimaDireccionEsquive, 0.7f).normalized;
                }
                else
                {
                    // Nueva dirección de esquive MÁS AGRESIVA (mayor peso)
                    ultimaDireccionEsquive = direccionEsquive;
                    tiempoUltimaEsquive = Time.time;
                    direccionFinal = Vector3.Lerp(direccionDeseada, direccionEsquive, 0.85f).normalized;
                }
            }
            else if (Time.time - tiempoUltimaEsquive < tiempoPersistenciaEsquive)
            {
                // Mantener un poco la dirección anterior aunque ya no haya obstáculo
                direccionFinal = Vector3.Lerp(direccionDeseada, ultimaDireccionEsquive, 0.3f).normalized;
            }
            
            // Aplicar repulsión lateral si hay paredes cerca
            if (direccionRepulsion != Vector3.zero)
            {
                direccionFinal = (direccionFinal + direccionRepulsion * fuerzaRepulsion).normalized;
            }
        }
        
        // Suavizar la dirección usando SmoothDamp para transiciones fluidas
        direccionActual = Vector3.SmoothDamp(direccionActual, direccionFinal, ref velocidadSuavizado, suavidadDireccion);
        
        if (rb != null)
        {
            // Movimiento con Rigidbody (recomendado)
            Vector3 velocidadDeseada = direccionActual * velocidad;
            velocidadDeseada.y = rb.linearVelocity.y; // Mantener velocidad Y (gravedad)
            rb.linearVelocity = velocidadDeseada;
        }
        else
        {
            // Movimiento con Transform (alternativo)
            Vector3 movimiento = direccionActual * velocidad * Time.deltaTime;
            transform.position += movimiento;
        }
        
        // Rotar hacia la dirección actual de manera suave
        if (direccionActual.magnitude > 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionActual);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadRotacion);
        }
    }
    
    // Sistema de evitación de obstáculos mejorado con SphereCast
    Vector3 DetectarYEvitarObstaculos(Vector3 direccionDeseada)
    {
        Vector3 origenDeteccion = transform.position + Vector3.up * 0.5f;
        
        // Primero verificar si el camino frontal está libre
        bool frontalBloqueado = Physics.SphereCast(origenDeteccion, 
                                                    radioDeteccion, 
                                                    direccionDeseada, 
                                                    out RaycastHit hitFrontal,
                                                    distanciaDeteccionObstaculo, 
                                                    capasObstaculos);
        
        // Verificar si está demasiado cerca incluso sin estar bloqueado
        bool muyCerca = frontalBloqueado && hitFrontal.distance < distanciaSeguridad;
        
        if (!frontalBloqueado)
        {
            return Vector3.zero; // Camino libre, no necesita esquivar
        }
        
        // Hay obstáculo al frente, buscar mejor alternativa
        Vector3 mejorDireccion = Vector3.zero;
        float mejorPeso = 0f;
        
        // Probar múltiples direcciones
        for (int i = 0; i < angulosDeteccion.Length; i++)
        {
            float angulo = angulosDeteccion[i];
            if (Mathf.Approximately(angulo, 0f)) continue; // Ya probamos frontal
            
            Vector3 direccionPrueba = Quaternion.Euler(0, angulo, 0) * direccionDeseada;
            
            // Usar SphereCast para mejor detección
            bool hayObstaculo = Physics.SphereCast(origenDeteccion, 
                                                   radioDeteccion, 
                                                   direccionPrueba, 
                                                   out RaycastHit hit,
                                                   distanciaDeteccionObstaculo, 
                                                   capasObstaculos);
            
            if (!hayObstaculo)
            {
                // No hay obstáculo: calcular peso (preferir direcciones más alejadas cuando está muy cerca)
                float peso;
                if (muyCerca)
                {
                    // Muy cerca: priorizar ángulos más pronunciados para alejarse
                    peso = Mathf.Abs(angulo) / 90f; // Invertido: mayor ángulo = mejor
                }
                else
                {
                    // Normal: preferir direcciones cercanas a la original
                    peso = 1f - (Mathf.Abs(angulo) / 90f);
                }
                
                if (peso > mejorPeso)
                {
                    mejorPeso = peso;
                    mejorDireccion = direccionPrueba;
                }
            }
            else
            {
                // Hay obstáculo pero más lejos: considerar si es la mejor opción parcial
                if (hit.distance > distanciaSeguridad)
                {
                    float peso = (hit.distance / distanciaDeteccionObstaculo) * 0.4f;
                    if (peso > mejorPeso)
                    {
                        mejorPeso = peso;
                        mejorDireccion = direccionPrueba;
                    }
                }
            }
            
            // Debug visual
            if (mostrarGizmos)
            {
                Color colorRayo = hayObstaculo ? Color.red : Color.green;
                Debug.DrawRay(origenDeteccion, 
                             direccionPrueba * distanciaDeteccionObstaculo, 
                             colorRayo);
            }
        }
        
        // Debug del obstáculo frontal
        if (mostrarGizmos)
        {
            Debug.DrawLine(origenDeteccion, hitFrontal.point, muyCerca ? Color.magenta : Color.yellow);
        }
        
        return mejorDireccion;
    }
    
    // Detectar obstáculos laterales para mantenerse alejado de paredes
    Vector3 DetectarObstaculosLaterales()
    {
        Vector3 repulsion = Vector3.zero;
        Vector3 origenDeteccion = transform.position + Vector3.up * 0.5f;
        Vector3 derecha = transform.right;
        Vector3 izquierda = -transform.right;
        
        // Detectar a la derecha
        if (Physics.Raycast(origenDeteccion, derecha, out RaycastHit hitDerecha, distanciaDeteccionLateral, capasObstaculos))
        {
            // Empujar hacia la izquierda
            float fuerzaDerecha = 1f - (hitDerecha.distance / distanciaDeteccionLateral);
            repulsion -= derecha * fuerzaDerecha;
            
            if (mostrarGizmos)
                Debug.DrawRay(origenDeteccion, derecha * hitDerecha.distance, Color.cyan);
        }
        
        // Detectar a la izquierda
        if (Physics.Raycast(origenDeteccion, izquierda, out RaycastHit hitIzquierda, distanciaDeteccionLateral, capasObstaculos))
        {
            // Empujar hacia la derecha
            float fuerzaIzquierda = 1f - (hitIzquierda.distance / distanciaDeteccionLateral);
            repulsion -= izquierda * fuerzaIzquierda;
            
            if (mostrarGizmos)
                Debug.DrawRay(origenDeteccion, izquierda * hitIzquierda.distance, Color.cyan);
        }
        
        return repulsion;
    }
    
    // Método público para que otros scripts puedan forzar un estado
    public void ForzarPerseguirJugador(Transform jugador)
    {
        if (jugador != null)
        {
            jugadorObjetivo = jugador;
            estadoActual = EstadoIA.PerseguirJugador;
            tiempoUltimaDeteccion = Time.time;
            
            if (mostrarDebug)
                Debug.Log($"[{name}] Forzado a perseguir: {jugador.name}");
        }
    }
    
    public void ForzarIrAlCamion()
    {
        jugadorObjetivo = null;
        estadoActual = EstadoIA.IrAlCamion;
        
        if (mostrarDebug)
            Debug.Log($"[{name}] Forzado a ir al camión");
    }
    
    // Getters para otros scripts
    public bool EstaPersiguiendoJugador() => estadoActual == EstadoIA.PerseguirJugador;
    public Transform GetJugadorObjetivo() => jugadorObjetivo;
    public float GetDistanciaAObjetivo()
    {
        if (estadoActual == EstadoIA.PerseguirJugador && jugadorObjetivo != null)
            return Vector3.Distance(transform.position, jugadorObjetivo.position);
        else if (camion != null)
            return Vector3.Distance(transform.position, camion.position);
        
        return float.MaxValue;
    }
    
    // Gizmos para debug visual
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;
        
        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        
        // Rango de pérdida
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoPerdida);
        
        // Línea hacia el objetivo
        if (Application.isPlaying)
        {
            if (estadoActual == EstadoIA.PerseguirJugador && jugadorObjetivo != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, jugadorObjetivo.position);
            }
            else if (camion != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, camion.position);
            }
        }
        
        // Estado actual
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Estado: {estadoActual}");
        }
        #endif
    }
}

// Atributo y Drawer locales para selector de Tag en este mismo script
public class IAEnemyTagSelectorAttribute : System.Attribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(IAEnemyTagSelectorAttribute))]
public class IAEnemyTagSelectorDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == UnityEditor.SerializedPropertyType.String)
        {
            UnityEditor.EditorGUI.BeginProperty(position, label, property);
            property.stringValue = UnityEditor.EditorGUI.TagField(position, label, property.stringValue);
            UnityEditor.EditorGUI.EndProperty();
        }
        else
        {
            UnityEditor.EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
