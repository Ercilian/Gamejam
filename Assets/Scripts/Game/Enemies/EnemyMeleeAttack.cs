using UnityEngine;

/// <summary>
/// Ataque cuerpo a cuerpo del enemigo. 
/// Realiza un golpe simple al jugador más cercano en rango.
/// </summary>
public class EnemyMeleeAttack : EnemyAttack
{
    [Header("Melee Attack Settings")]
    [Tooltip("Forma del área de ataque")]
    public MeleeShape attackShape = MeleeShape.Sphere;
    
    [Tooltip("Tamaño del área de ataque (para Box)")]
    public Vector3 attackBoxSize = new Vector3(1.5f, 1.5f, 2f);
    
    [Tooltip("Offset local del área de ataque desde el transform")]
    public Vector3 attackOffset = new Vector3(0f, 0f, 1f);
    
    [Tooltip("Ángulo del cono de ataque (solo para Cone)")]
    public float coneAngle = 90f;

    [Header("Effects")]
    [Tooltip("Efecto de partículas al golpear (opcional)")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Audio del golpe (opcional)")]
    public AudioClip hitSound;

    public enum MeleeShape { Sphere, Box, Cone }

    protected override void ExecuteAttack(Transform target)
    {
        // Detectar todos los jugadores en el área de ataque
        Collider[] hitPlayers = DetectPlayersInAttackArea();

        if (hitPlayers.Length == 0)
        {
            if (showDebugLogs) Debug.Log($"[{gameObject.name}] Ataque melee falló, no hay jugadores en el área.");
            return;
        }

        // Aplicar daño a todos los jugadores detectados
        foreach (var playerCollider in hitPlayers)
        {
            ApplyDamageToTarget(playerCollider.transform, baseDamage);
            
            // Efectos visuales/sonoros
            SpawnHitEffect(playerCollider.transform.position);
            PlayHitSound();
        }

        if (showDebugLogs) Debug.Log($"[{gameObject.name}] Ataque melee golpeó a {hitPlayers.Length} jugador(es).");
    }

    /// <summary>
    /// Detecta jugadores en el área de ataque según la forma configurada
    /// </summary>
    private Collider[] DetectPlayersInAttackArea()
    {
        Vector3 attackPosition = transform.position + transform.TransformDirection(attackOffset);

        switch (attackShape)
        {
            case MeleeShape.Sphere:
                return Physics.OverlapSphere(attackPosition, attackRange, playerLayer, QueryTriggerInteraction.Collide);

            case MeleeShape.Box:
                Quaternion rotation = transform.rotation;
                return Physics.OverlapBox(attackPosition, attackBoxSize * 0.5f, rotation, playerLayer, QueryTriggerInteraction.Collide);

            case MeleeShape.Cone:
                return DetectPlayersInCone(attackPosition);

            default:
                return new Collider[0];
        }
    }

    /// <summary>
    /// Detecta jugadores en un cono delante del enemigo
    /// </summary>
    private Collider[] DetectPlayersInCone(Vector3 position)
    {
        Collider[] candidates = Physics.OverlapSphere(position, attackRange, playerLayer, QueryTriggerInteraction.Collide);
        System.Collections.Generic.List<Collider> validTargets = new System.Collections.Generic.List<Collider>();

        Vector3 forward = transform.forward;
        float halfAngle = coneAngle * 0.5f;

        foreach (var candidate in candidates)
        {
            Vector3 directionToTarget = (candidate.transform.position - position).normalized;
            float angle = Vector3.Angle(forward, directionToTarget);
            
            if (angle <= halfAngle)
            {
                validTargets.Add(candidate);
            }
        }

        return validTargets.ToArray();
    }

    /// <summary>
    /// Instancia el efecto visual del golpe
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f); // Destruir después de 2 segundos
        }
    }

    /// <summary>
    /// Reproduce el sonido del golpe
    /// </summary>
    private void PlayHitSound()
    {
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!drawGizmos) return;

        Vector3 attackPosition = transform.position + transform.TransformDirection(attackOffset);
        Gizmos.color = Color.yellow;

        switch (attackShape)
        {
            case MeleeShape.Sphere:
                Gizmos.DrawWireSphere(attackPosition, attackRange);
                break;

            case MeleeShape.Box:
                Gizmos.matrix = Matrix4x4.TRS(attackPosition, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
                Gizmos.matrix = Matrix4x4.identity;
                break;

            case MeleeShape.Cone:
                DrawConGizmo(attackPosition);
                break;
        }
    }

    /// <summary>
    /// Dibuja un cono en los Gizmos
    /// </summary>
    private void DrawConGizmo(Vector3 position)
    {
        Vector3 forward = transform.forward;
        float halfAngle = coneAngle * 0.5f;

        // Líneas del cono
        Vector3 right = Quaternion.AngleAxis(halfAngle, transform.up) * forward * attackRange;
        Vector3 left = Quaternion.AngleAxis(-halfAngle, transform.up) * forward * attackRange;

        Gizmos.DrawLine(position, position + right);
        Gizmos.DrawLine(position, position + left);
        Gizmos.DrawLine(position + right, position + left);
    }
}
