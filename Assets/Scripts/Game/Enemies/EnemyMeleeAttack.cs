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
    
    [Tooltip("Retraso en segundos antes de aplicar el daño (para sincronizar con la animación)")]
    public float attackDamageDelay = 0.3f;

    [Header("Sistema de Camión")]
    [Tooltip("Tag del camión para buscar automáticamente")]
    [SerializeField] private string carTag = "Car";
    
    private GameObject carGameObject;

    [Header("Effects")]
    [Tooltip("Efecto de partículas al golpear (opcional)")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Audio del golpe (opcional)")]
    public AudioClip hitSound;

    public enum MeleeShape { Sphere, Box, Cone }

    protected override void Awake()
    {
        base.Awake();
        FindCar();
    }
    
    private void FindCar()
    {
        GameObject carByTag = GameObject.FindGameObjectWithTag(carTag);
        if (carByTag != null)
        {
            carGameObject = carByTag;
            if (showDebugLogs)
                Debug.Log($"[{gameObject.name}] Camión encontrado por tag '{carTag}': {carGameObject.name}");
            return;
        }
        
        MovCar movCar = FindFirstObjectByType<MovCar>();
        if (movCar != null)
        {
            carGameObject = movCar.gameObject;
            if (showDebugLogs)
                Debug.Log($"[{gameObject.name}] Camión encontrado por componente MovCar: {carGameObject.name}");
            return;
        }
        
        if (showDebugLogs)
            Debug.LogWarning($"[{gameObject.name}] No se encontró el camión.");
    }
    
    private void Update()
    {
        // Si puede atacar, buscar objetivo
        if (CanAttack())
        {
            // Buscar jugadores en rango
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            bool hayJugadoresEnRango = false;
            
            foreach (GameObject player in allPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= attackRange)
                {
                    hayJugadoresEnRango = true;
                    break;
                }
            }
            
            // Si no hay jugadores en rango y hay camión, atacar al camión
            if (!hayJugadoresEnRango && carGameObject != null)
            {
                float distanceToCar = Vector3.Distance(transform.position, carGameObject.transform.position);
                if (distanceToCar <= attackRange)
                {
                    if (showDebugLogs)
                        Debug.Log($"[{gameObject.name}] No hay jugadores en rango, atacando al camión");
                    TryAttack(carGameObject.transform);
                }
            }
        }
    }

    protected override void ExecuteAttack(Transform target)
    {
        animator.SetTrigger("Attack");
        
        // Aplicar el daño después del retraso configurado para sincronizar con la animación
        StartCoroutine(DelayedAttack());
    }

    /// <summary>
    /// Ejecuta el ataque después del retraso configurado
    /// </summary>
    private System.Collections.IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(attackDamageDelay);
        
        
        // Detectar todos los jugadores y el camión en el área de ataque
        Collider[] hitTargets = DetectTargetsInAttackArea();

        if (hitTargets.Length == 0)
        {
            if (showDebugLogs) Debug.Log($"[{gameObject.name}] Ataque melee falló, no hay objetivos en el área.");
            yield break;
        }

        // Aplicar daño a todos los objetivos detectados
        foreach (var targetCollider in hitTargets)
        {
            ApplyDamageToTarget(targetCollider.transform, baseDamage);
            
            // Efectos visuales/sonoros
            SpawnHitEffect(targetCollider.transform.position);
            PlayHitSound();
        }

        if (showDebugLogs) Debug.Log($"[{gameObject.name}] Ataque melee golpeó a {hitTargets.Length} objetivo(s).");
    }

    /// <summary>
    /// Detecta jugadores y camión en el área de ataque según la forma configurada
    /// </summary>
    private Collider[] DetectTargetsInAttackArea()
    {
        Vector3 attackPosition = transform.position + transform.TransformDirection(attackOffset);
        Collider[] allColliders;

        switch (attackShape)
        {
            case MeleeShape.Sphere:
                allColliders = Physics.OverlapSphere(attackPosition, attackRange, ~0, QueryTriggerInteraction.Collide);
                break;

            case MeleeShape.Box:
                Quaternion rotation = transform.rotation;
                allColliders = Physics.OverlapBox(attackPosition, attackBoxSize * 0.5f, rotation, ~0, QueryTriggerInteraction.Collide);
                break;

            case MeleeShape.Cone:
                allColliders = DetectTargetsInCone(attackPosition);
                break;

            default:
                return new Collider[0];
        }
        
        // Filtrar solo jugadores y camión
        System.Collections.Generic.List<Collider> validTargets = new System.Collections.Generic.List<Collider>();
        foreach (var collider in allColliders)
        {
            if (collider.CompareTag("Player") || collider.CompareTag("Car"))
            {
                validTargets.Add(collider);
            }
        }
        
        return validTargets.ToArray();
    }

    /// <summary>
    /// Detecta jugadores y camión en un cono delante del enemigo
    /// </summary>
    private Collider[] DetectTargetsInCone(Vector3 position)
    {
        Collider[] candidates = Physics.OverlapSphere(position, attackRange, ~0, QueryTriggerInteraction.Collide);
        System.Collections.Generic.List<Collider> validTargets = new System.Collections.Generic.List<Collider>();

        Vector3 forward = transform.forward;
        float halfAngle = coneAngle * 0.5f;

        foreach (var candidate in candidates)
        {
            // Solo considerar jugadores y camión
            if (!candidate.CompareTag("Player") && !candidate.CompareTag("Car"))
                continue;
                
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
