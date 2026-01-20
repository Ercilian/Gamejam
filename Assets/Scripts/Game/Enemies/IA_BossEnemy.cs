using UnityEngine;

public class IA_BossEnemy : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad cuando persigue al jugador")]
    public float velocidadPersecucion = 5f;
    [Tooltip("Distancia para detectar al jugador")]
    public float rangoDeteccion = 15f;
    [Tooltip("Distancia para perder al jugador")]
    public float rangoPerdida = 20f;
    [Tooltip("Distancia mínima al objetivo antes de detenerse")]
    public float distanciaMinima = 1f;
    [Tooltip("Velocidad de rotación (mayor = gira más rápido)")]
    public float velocidadRotacion = 8f;
    
    [Header("Objetivos")]
    [Tooltip("Tag del jugador a perseguir")]
    public string tagJugador = "Player";
    
    [Header("Debug")]
    public bool mostrarDebug = false;
    public bool mostrarGizmos = true;
    
    private Transform jugadorObjetivo;
    private Vector3 ultimaPosicionJugador;
    private bool persiguiendo = false;
    private Rigidbody rb;
    
    // Componentes opcionales
    private Animator animator;
    private EnemyAttack enemyAttack;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    void Update()
    {
        // Buscar al jugador más cercano
        Transform jugadorCercano = BuscarJugadorCercano();
        
        if (jugadorCercano != null)
        {
            jugadorObjetivo = jugadorCercano;
            ultimaPosicionJugador = jugadorCercano.position;
            persiguiendo = true;
            
            if (mostrarDebug)
                Debug.Log($"[{name}] Jugador detectado: {jugadorCercano.name}");
        }
        else if (persiguiendo && jugadorObjetivo != null)
        {
            // Verificar si el jugador está demasiado lejos
            float distancia = Vector3.Distance(transform.position, jugadorObjetivo.position);
            if (distancia > rangoPerdida)
            {
                jugadorObjetivo = null;
                persiguiendo = false;
                
                if (mostrarDebug)
                    Debug.Log($"[{name}] Jugador demasiado lejos, dejando de perseguir");
            }
        }
        
        // Si hay jugador objetivo, perseguirlo
        if (persiguiendo && jugadorObjetivo != null)
        {
            // Intentar atacar si está en rango
            if (enemyAttack != null && enemyAttack.CanAttack())
            {
                enemyAttack.TryAttack(jugadorObjetivo);
            }
            
            MoverHacia(jugadorObjetivo.position, velocidadPersecucion);
        }
        else if (jugadorObjetivo != null)
        {
            // Seguir la última posición conocida
            MoverHacia(ultimaPosicionJugador, velocidadPersecucion);
        }
        else
        {
            // No hay jugador, quedarse quieto
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
        }
        
        // Actualizar animación si existe
        if (animator != null)
        {
            float velocidadActual = rb != null ? rb.linearVelocity.magnitude : 0f;
            animator.SetFloat("Velocidad", velocidadActual);
            animator.SetBool("Persiguiendo", persiguiendo);
        }
    }
    
    Transform BuscarJugadorCercano()
    {
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
        
        if (mostrarDebug)
            Debug.Log($"[{name}] Persiguiendo: distancia={distancia:F2}, dirección={direccionDeseada}, velocidad={velocidad}");
        
        // No moverse si está muy cerca
        if (distancia < distanciaMinima)
        {
            if (rb != null)
                rb.linearVelocity = Vector3.zero;
            return;
        }
        
        // Movimiento usando Transform directamente (más confiable)
        Vector3 movimiento = direccionDeseada * velocidad * Time.deltaTime;
        transform.position += movimiento;
        
        // También intentar usar Rigidbody si existe
        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocidadDeseada = direccionDeseada * velocidad;
            velocidadDeseada.y = rb.linearVelocity.y;
            rb.linearVelocity = velocidadDeseada;
        }
        
        // Rotar hacia la dirección
        if (direccionDeseada.magnitude > 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionDeseada);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadRotacion);
        }
    }
    
    public bool EstaPersiguiendo() => persiguiendo;
    public Transform GetJugadorObjetivo() => jugadorObjetivo;
    public float GetDistanciaAJugador()
    {
        if (jugadorObjetivo != null)
            return Vector3.Distance(transform.position, jugadorObjetivo.position);
        
        return float.MaxValue;
    }
    
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;
        
        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        
        // Rango de pérdida
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoPerdida);
        
        // Línea hacia el jugador
        if (Application.isPlaying && jugadorObjetivo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, jugadorObjetivo.position);
        }
    }
}
