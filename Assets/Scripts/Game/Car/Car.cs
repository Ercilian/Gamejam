using UnityEngine;
using Game.Combat;
using System.Collections;

public class Car : EntityStats
{

    [SerializeField] private GameObject explosionVFXPrefab;
    
    [SerializeField] private float delayBeforeGameOver = 2f;
    
    private Options options;

    public override void OnEntityDeath()
    {
        options = FindObjectOfType<Options>();
        StartCoroutine(DeathSequence());
    }
    
    private IEnumerator DeathSequence()
    {
        if (explosionVFXPrefab != null)
        {
            GameObject explosion = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(explosion, 3.5f); 
            }
        }
        
        // Esperar 2 segundos antes de ocultar el camión
        yield return new WaitForSeconds(2f);
        
        // Ocultar el camión visualmente (desactivar renderizado)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Esperar antes de mostrar Game Over
        yield return new WaitForSeconds(delayBeforeGameOver);
        
        // Mostrar Game Over
        if (options != null)
        {
            options.GameOver();
        }
        
        base.OnEntityDeath();
        
        // Destruir el GameObject del camión
        Destroy(gameObject);
    }
    
    /*
    public override void TakeDamage(DamageInfo damageInfo )
    {
        base.TakeDamage(damageInfo);
    }
    */
}
