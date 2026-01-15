using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    #region Configuration
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private ParticleSystem hitEffectPrefab;
    #endregion

    #region State
    private bool hasHit = false;
    #endregion

    #region Initialization
    private void Start()
    {
        // Verificar si este GameObject tiene FrogCombat (es el boss)
        FrogCombat bossCombat = GetComponent<FrogCombat>();
        if (bossCombat != null)
        {
            // Este es el boss, no destruir
            Debug.Log("[BossProjectile] Detectado en boss, ignorando auto-destrucción");
            return;
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
        if (hasHit)
            return;

        // Verificar si es el jugador por tag o layer
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EntityStats playerStats = collision.GetComponent<EntityStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(projectileDamage);
                Debug.Log($"[BossProjectile] ¡Proyectil impactó al jugador! Daño: {projectileDamage}");
                
                hasHit = true;
                OnHit(collision);
            }
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
}
