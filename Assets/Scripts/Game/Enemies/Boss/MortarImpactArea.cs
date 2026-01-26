using UnityEngine;
using System.Collections;

public class MortarImpactArea : MonoBehaviour
{
    #region Configuration
    [SerializeField] private int damageAmount = 30;
    [SerializeField] private float delayBeforeDamage = 0.8f; // Tiempo antes de que el mortero explota
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private LineRenderer areaVisualization;
    #endregion

    #region State
    private bool hasDealtDamage = false;
    #endregion

    #region Initialization
    private void Start()
    {
        Debug.Log("[MortarImpactArea] Área de impacto de mortero creada en: " + transform.position);
        
        // Iniciar la secuencia de daño después del delay
        StartCoroutine(DealDamageAfterDelay());
    }
    #endregion

    #region Setters
    public void SetDamageRadius(float radius)
    {
        damageRadius = radius;
    }

    public void SetDelayBeforeDamage(float delay)
    {
        delayBeforeDamage = delay;
    }

    public void SetDamageAmount(int damage)
    {
        damageAmount = damage;
    }
    #endregion

    #region Damage Logic
    private IEnumerator DealDamageAfterDelay()
    {
        // Esperar a que el mortero explote
        yield return new WaitForSeconds(delayBeforeDamage);
        
        if (!hasDealtDamage)
        {
            DealDamage();
        }
    }

    private void DealDamage()
    {
        hasDealtDamage = true;
        
        // Detectar al jugador en el área
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);
        
        foreach (Collider collider in hitColliders)
        {
            // Verificar si es el jugador
            if (collider.CompareTag("Player") || collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                EntityStats playerStats = collider.GetComponent<EntityStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damageAmount);
                    Debug.Log($"[MortarImpactArea] ¡Mortero impactó al jugador! Daño: {damageAmount}");
                }
            }
        }
        
        // Destruir el área después de haber causado daño
        Destroy(gameObject);
    }
    #endregion

    #region Gizmos & Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Naranja semi-transparente
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
    #endregion
}
