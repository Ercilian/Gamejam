using UnityEngine;
using Game.Enemies;

public class RangedEnemyAttack : EnemyAttack
{
    [Header("Sistema de Ataque Ranged")]
    [Tooltip("Punto desde donde sale el disparo")]
    public Transform puntoDisparo;
    
    [Tooltip("Tiempo de aviso antes de disparar")]
    public float tiempoAntesDeDanio = 3f;
    
    [Tooltip("Tiempo antes del disparo en que la línea se congela (para dar oportunidad de esquivar)")]
    public float tiempoCongelacion = 1.5f;
    
    [Tooltip("Duración de la animación del impacto")]
    public float duracionEfectoRaycast = 0.3f;

    private LineRenderer lineRenderer;
    private Transform objetivoActual;
    private bool mostrandoAviso = false;
    private float tiempoInicioAviso;
    private Vector3 direccionCongelada;
    private Vector3 puntoFinalCongelado;
    private bool direccionEstaCongelada = false;

    protected override void Awake()
    {
        base.Awake();
        
        if (puntoDisparo == null)
            puntoDisparo = transform;
        
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        // Actualizar la línea de aviso durante la fase de advertencia
        if (mostrandoAviso && objetivoActual != null)
        {
            // Verificar que lineRenderer existe
            if (lineRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] LineRenderer es null en Update");
                mostrandoAviso = false;
                return;
            }
            
            // Asegurar que está habilitado
            if (!lineRenderer.enabled)
            {
                lineRenderer.enabled = true;
            }
            
            // Calcular tiempo transcurrido desde el inicio del aviso
            float tiempoTranscurrido = Time.time - tiempoInicioAviso;
            float tiempoRestante = tiempoAntesDeDanio - tiempoTranscurrido;
            
            // Si queda menos tiempo que tiempoCongelacion, congelar la dirección
            if (tiempoRestante <= tiempoCongelacion && !direccionEstaCongelada)
            {
                direccionCongelada = (objetivoActual.position - puntoDisparo.position).normalized;
                puntoFinalCongelado = puntoDisparo.position + direccionCongelada * attackRange;
                direccionEstaCongelada = true;
                Debug.Log($"[{gameObject.name}] 🔒 DIRECCIÓN CONGELADA - Tiempo restante: {tiempoRestante:F2}s, Dirección: {direccionCongelada}");
            }
            
            // Usar posición congelada o seguir al objetivo
            Vector3 puntoFinal;
            if (direccionEstaCongelada)
            {
                puntoFinal = puntoFinalCongelado;
            }
            else
            {
                Vector3 direccion = (objetivoActual.position - puntoDisparo.position).normalized;
                puntoFinal = puntoDisparo.position + direccion * attackRange;
            }
            
            // Actualizar posición de la línea
            lineRenderer.SetPosition(0, puntoDisparo.position);
            lineRenderer.SetPosition(1, puntoFinal);
            
            // Color diferente si está congelada (rojo) o siguiendo (amarillo/naranja)
            if (direccionEstaCongelada)
            {
                // Rojo intenso cuando está congelada
                lineRenderer.startColor = Color.red;
                lineRenderer.endColor = Color.red;
            }
            else
            {
                // Parpadeo amarillo/naranja mientras sigue
                float parpadeo = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
                
                Color colorParpadeo = Color.Lerp(
                    new Color(1f, 0.5f, 0f, 1f),  // Naranja
                    new Color(1f, 1f, 0f, 1f),    // Amarillo brillante
                    parpadeo
                );
                
                lineRenderer.startColor = colorParpadeo;
                lineRenderer.endColor = colorParpadeo;
            }
        }
    }

    protected override void ExecuteAttack(Transform target)
    {
        animator.SetTrigger("Attack");
        Debug.Log($"[{gameObject.name}] ========== EXECUTEATACK LLAMADO ==========");
        
        if (target == null)
        {
            Debug.Log($"[{gameObject.name}] ERROR: Target es null");
            return;
        }
        
        objetivoActual = target;
        mostrandoAviso = true;
        tiempoInicioAviso = Time.time;
        direccionEstaCongelada = false; // Reset congelación
        
        // Mostrar línea de aviso
        Vector3 direccion = (target.position - puntoDisparo.position).normalized;
        Vector3 puntoFinal = puntoDisparo.position + direccion * attackRange;
        
        lineRenderer.enabled = true;
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.SetPosition(0, puntoDisparo.position);
        lineRenderer.SetPosition(1, puntoFinal);
        
        Debug.Log($"[{gameObject.name}] ⚠️ AVISO ACTIVO - Línea habilitada, esperando {tiempoAntesDeDanio}s");
        Debug.Log($"[{gameObject.name}] Se congelará en {tiempoAntesDeDanio - tiempoCongelacion:F2}s");
        
        // Disparar después del delay
        Invoke(nameof(Disparar), tiempoAntesDeDanio);
    }

    private void Disparar()
    {
        Debug.Log($"[{gameObject.name}] ========== DISPARAR LLAMADO ==========");
        mostrandoAviso = false;
        
        if (objetivoActual == null)
        {
            lineRenderer.enabled = false;
            Debug.Log($"[{gameObject.name}] ERROR: Objetivo perdido");
            return;
        }
        
        Debug.Log($"[{gameObject.name}] 🔫 DISPARANDO");
        
        // Usar la dirección congelada si existe, si no recalcular
        Vector3 direccion = direccionEstaCongelada ? direccionCongelada : (objetivoActual.position - puntoDisparo.position).normalized;
        
        Debug.Log($"[{gameObject.name}] Raycast desde {puntoDisparo.position} dirección {direccion} rango {attackRange}");
        Debug.Log($"[{gameObject.name}] Usando dirección congelada: {direccionEstaCongelada}");
        
        bool hit = Physics.Raycast(puntoDisparo.position, direccion, out RaycastHit hitInfo, attackRange);
        Debug.Log($"[{gameObject.name}] Raycast result: {hit}");
        
        if (hit)
        {
            Debug.Log($"[{gameObject.name}] Impactó: {hitInfo.transform.name} (Buscando: {objetivoActual.name})");
        }
        
        if (hit && hitInfo.transform == objetivoActual)
        {
            // IMPACTO
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
            lineRenderer.SetPosition(0, puntoDisparo.position);
            lineRenderer.SetPosition(1, hitInfo.point);
            
            Debug.Log($"[{gameObject.name}] 🎯 IMPACTO CONFIRMADO a {objetivoActual.name}");
            
            // Aplicar daño
            EntityStats stats = objetivoActual.GetComponent<EntityStats>();
            Debug.Log($"[{gameObject.name}] EntityStats encontrado: {stats != null}");
            
            if (stats != null)
            {
                int damage = (enemyStats != null) ? enemyStats.AttackDamage : baseDamage;
                Debug.Log($"[{gameObject.name}] Daño calculado: {damage}");
                Debug.Log($"[{gameObject.name}] LLAMANDO TakeDamage({damage})...");
                stats.TakeDamage(damage);
                Debug.Log($"[{gameObject.name}] ✅ TakeDamage EJECUTADO");
            }
        }
        else
        {
            // FALLÓ
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.SetPosition(0, puntoDisparo.position);
            lineRenderer.SetPosition(1, puntoDisparo.position + direccion * attackRange);
            
            Debug.Log($"[{gameObject.name}] ❌ FALLÓ - Jugador esquivó");
        }
        
        Invoke(nameof(OcultarLinea), duracionEfectoRaycast);
        objetivoActual = null;
        direccionEstaCongelada = false;
    }

    private void OcultarLinea()
    {
        lineRenderer.enabled = false;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!drawGizmos) return;
        
        Gizmos.color = Color.cyan;
        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Gizmos.DrawWireSphere(origen, attackRange);
    }
}

