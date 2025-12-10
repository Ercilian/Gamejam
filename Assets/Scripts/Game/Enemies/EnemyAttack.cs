using UnityEngine;
using Game.Enemies;

/// <summary>
/// Clase base para los ataques de enemigos.
/// Los scripts hijos heredarán de esta clase para implementar diferentes tipos de ataque.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Daño base del ataque (se sobrescribe si hay ScriptableObject en Enemy)")]
    public int baseDamage = 10;
    
    [Tooltip("Cooldown entre ataques en segundos")]
    public float attackCooldown = 1.5f;
    
    [Tooltip("Rango de ataque")]
    public float attackRange = 2f;
    
    [Tooltip("Layer del jugador para detectar colisiones")]
    public LayerMask playerLayer;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool drawGizmos = true;

    // Referencias
    protected Enemy enemyStats;
    protected float lastAttackTime = -999f;

    protected virtual void Awake()
    {
        enemyStats = GetComponent<Enemy>();
        if (enemyStats == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyAttack requiere un componente Enemy en el mismo GameObject.");
        }
        else
        {
            // Usar el daño del ScriptableObject si está disponible
            if (enemyStats.StatsData != null)
            {
                baseDamage = enemyStats.AttackDamage;
                if (showDebugLogs) Debug.Log($"[{gameObject.name}] Daño de ataque cargado desde ScriptableObject: {baseDamage}");
            }
        }
    }

    /// <summary>
    /// Verifica si el enemigo puede atacar (respeta el cooldown)
    /// </summary>
    public virtual bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    /// <summary>
    /// Intenta realizar un ataque. Los hijos deben sobrescribir este método para implementar su lógica.
    /// </summary>
    /// <param name="target">Transform del objetivo (usualmente el jugador)</param>
    /// <returns>True si el ataque se ejecutó exitosamente</returns>
    public virtual bool TryAttack(Transform target)
    {
        if (!CanAttack())
        {
            if (showDebugLogs) Debug.Log($"[{gameObject.name}] Ataque en cooldown.");
            return false;
        }

        if (target == null)
        {
            if (showDebugLogs) Debug.LogWarning($"[{gameObject.name}] No hay objetivo para atacar.");
            return false;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange)
        {
            if (showDebugLogs) Debug.Log($"[{gameObject.name}] Objetivo fuera de rango: {distance:F2} > {attackRange}");
            return false;
        }

        // Ejecutar el ataque
        ExecuteAttack(target);
        lastAttackTime = Time.time;
        return true;
    }

    /// <summary>
    /// Ejecuta la lógica del ataque. Los hijos deben sobrescribir este método.
    /// </summary>
    protected virtual void ExecuteAttack(Transform target)
    {
        // Lógica base: aplicar daño directo al objetivo
        ApplyDamageToTarget(target, baseDamage);
    }

    /// <summary>
    /// Aplica daño a un objetivo específico
    /// </summary>
    protected void ApplyDamageToTarget(Transform target, int damage)
    {
        EntityStats targetStats = target.GetComponent<EntityStats>();
        if (targetStats != null)
        {
            targetStats.TakeDamage(damage);
            if (showDebugLogs) Debug.Log($"[{gameObject.name}] Aplicó {damage} de daño a {target.name}");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning($"[{gameObject.name}] El objetivo {target.name} no tiene EntityStats.");
        }
    }

    /// <summary>
    /// Detecta todos los jugadores en el rango de ataque
    /// </summary>
    protected Collider[] DetectPlayersInRange(Vector3 position, float range)
    {
        return Physics.OverlapSphere(position, range, playerLayer, QueryTriggerInteraction.Collide);
    }

    /// <summary>
    /// Obtiene el jugador más cercano en el rango
    /// </summary>
    protected Transform GetClosestPlayerInRange()
    {
        Collider[] players = DetectPlayersInRange(transform.position, attackRange);
        if (players.Length == 0) return null;

        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = player.transform;
            }
        }

        return closest;
    }

    protected virtual void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Dibujar el rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
