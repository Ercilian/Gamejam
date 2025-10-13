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
    
    // Componentes opcionales
    private Animator animator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

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
                if (mostrarDebug) Debug.Log($"[{name}] Camión encontrado por tag '{camionTag}': {camion.name}");
            }
            else if (mostrarDebug)
            {
                Debug.LogWarning($"[{name}] No se encontró camión con tag '{camionTag}'.");
            }
        }

        if (camion != null)
            ultimaPosicionCamion = camion.position;
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
                Debug.Log($"[{name}] ¡Jugador detectado! Cambiando a persecución: {jugadorCercano.name}");
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
                Debug.Log($"[{name}] Jugador objetivo perdido, volviendo al camión");
            return;
        }
        
        float distanciaAJugador = Vector3.Distance(transform.position, jugadorObjetivo.position);
        
        // Verificar si el jugador está muy lejos
        if (distanciaAJugador > rangoPerdida)
        {
            // Perder al jugador y volver al camión
            if (mostrarDebug)
                Debug.Log($"[{name}] Jugador muy lejos ({distanciaAJugador:F1}m), volviendo al camión");
            
            jugadorObjetivo = null;
            estadoActual = EstadoIA.IrAlCamion;
            return;
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
    Vector3 direccion = (objetivo - transform.position).normalized;
    float distancia = Vector3.Distance(transform.position, objetivo);
        // No moverse si está muy cerca
        if (distancia < distanciaMinima)
        {
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
            return;
        }
        
        if (rb != null)
        {
            // Movimiento con Rigidbody (recomendado)
            Vector3 velocidadDeseada = direccion * velocidad;
            velocidadDeseada.y = rb.linearVelocity.y; // Mantener velocidad Y (gravedad)
            rb.linearVelocity = velocidadDeseada;
        }
        else
        {
            // Movimiento con Transform (alternativo)
            Vector3 movimiento = direccion * velocidad * Time.deltaTime;
            transform.position += movimiento;
        }
        
        // Rotar hacia el objetivo
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
        }
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
