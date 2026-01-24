using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    #region Configuration
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private ParticleSystem hitEffectPrefab;
    [Header("Hitbox Configuration")]
    [SerializeField] [Range(0.1f, 1.0f)] private float hitboxScale = 0.6f;
    [SerializeField] private bool showHitboxGizmo = true;
    #endregion

    #region State
    private bool hasHit = false;
    private Collider projectileCollider;
    #endregion

    #region Initialization
    private void Awake()
    {
        Debug.Log("[BossProjectile] Awake - Proyectil creado");
    }

    private void Start()
    {
        Debug.Log("[BossProjectile] Start() - Inicializando proyectil");
        
        // Verificar si este GameObject tiene FrogCombat (es el boss)
        FrogCombat bossCombat = GetComponent<FrogCombat>();
        if (bossCombat != null)
        {
            // Este es el boss, no destruir
            Debug.Log("[BossProjectile] Detectado en boss, ignorando auto-destrucción");
            return;
        }
        
        // Asignar tag de proyectil para que el dash pueda atravesarlo
        gameObject.tag = "Projectile";
        
        projectileCollider = GetComponent<Collider>();
        
        // Asegurar que el collider es un trigger para detectar colisiones
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
            
            // Escalar la hitbox del collider para hacerla más pequeña
            if (projectileCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius *= hitboxScale;
                Debug.Log($"[BossProjectile] SphereCollider escalado a {hitboxScale * 100}% del tamaño original");
            }
            else if (projectileCollider is BoxCollider boxCollider)
            {
                boxCollider.size *= hitboxScale;
                Debug.Log($"[BossProjectile] BoxCollider escalado a {hitboxScale * 100}% del tamaño original");
            }
            else if (projectileCollider is CapsuleCollider capsuleCollider)
            {
                capsuleCollider.radius *= hitboxScale;
                capsuleCollider.height *= hitboxScale;
                Debug.Log($"[BossProjectile] CapsuleCollider escalado a {hitboxScale * 100}% del tamaño original");
            }
            
            Debug.Log("[BossProjectile] Collider configurado como trigger");
        }
        else
        {
            Debug.LogWarning("[BossProjectile] No se encontró Collider en el proyectil");
        }
        
        // Asegurar que haya un Rigidbody para que los triggers funcionen
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("[BossProjectile] Se añadió Rigidbody al proyectil");
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
            Debug.Log("[BossProjectile] Rigidbody configurado como kinematic");
        }
        
        // Destruir el proyectil después de un tiempo si no impacta nada
        Destroy(gameObject, lifetime);
    }
    #endregion

    #region Collision Detection
    private void OnTriggerEnter(Collider collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject.GetComponent<Collider>());
    }

    private void HandleCollision(Collider collision)
    {
        Debug.Log($"[BossProjectile] Colisión detectada con: {collision.gameObject.name}, Tag: {collision.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        if (hasHit)
        {
            Debug.Log("[BossProjectile] Ya fue golpeado, ignorando");
            return;
        }

        // Verificar si es el jugador por tag o layer
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("[BossProjectile] Detectado jugador");
            
            // Verificar si el jugador está en invulnerabilidad temporal - si es así, ignorar colisión
            Player playerComponent = collision.GetComponent<Player>();
            if (playerComponent != null && playerComponent.IsInvulnerable)
            {
                Debug.Log("[BossProjectile] Jugador en invulnerabilidad - ignorando colisión con el proyectil");
                // Ignorar la colisión entre el proyectil y el jugador invulnerable
                Physics.IgnoreCollision(projectileCollider, collision, true);
                return; // No destruir, dejar que el proyectil continúe
            }
            
            // Verificar si el jugador está en dash - si es así, no causar daño
            Dash dashComponent = collision.GetComponent<Dash>();
            if (dashComponent != null && dashComponent.IsDashing)
            {
                Debug.Log("[BossProjectile] Jugador en dash - proyectil atravesado sin daño");
                return; // No causar daño, solo pasar
            }
            
            EntityStats playerStats = collision.GetComponent<EntityStats>();
            if (playerStats != null)
            {
                Debug.Log($"[BossProjectile] Causando daño de {projectileDamage}");
                playerStats.TakeDamage(projectileDamage);
                Debug.Log($"[BossProjectile] ¡Proyectil impactó al jugador! Daño: {projectileDamage}");
                
                hasHit = true;
                OnHit(collision);
            }
            else
            {
                Debug.LogWarning("[BossProjectile] No se encontró EntityStats en el jugador");
            }
        }
        else
        {
            Debug.Log("[BossProjectile] No es el jugador");
        }
    }

    private void OnHit(Collider collision)
    {
        // Crear efecto de impacto
        if (hitEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // Destruir proyectil si está configurado
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Getters/Setters
    public int ProjectileDamage
    {
        get => projectileDamage;
        set => projectileDamage = value;
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        if (!showHitboxGizmo) return;

        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = Color.yellow;

        if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
        }
        else if (col is BoxCollider boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is CapsuleCollider capsuleCol)
        {
            // Simplified capsule visualization
            Gizmos.DrawWireSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
        }
    }
    #endregion
}
