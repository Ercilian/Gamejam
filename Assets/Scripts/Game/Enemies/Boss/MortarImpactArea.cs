using UnityEngine;
using System.Collections;

public class MortarImpactArea : MonoBehaviour
{
    #region Configuration
    [SerializeField] private int damageAmount = 30;
    [SerializeField] private float delayBeforeDamage = 0.8f; // Tiempo antes de que el mortero explota
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private LineRenderer areaVisualization;
    private AudioClip impactSound;
    private AudioSource audioSource;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.3f;
    private bool shouldShake = false;
    private GameObject smokeVFXPrefab;
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
    
    public void SetImpactSound(AudioClip sound, AudioSource source)
    {
        impactSound = sound;
        audioSource = source;
    }
    
    public void SetShakeParameters(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }
    
    public void SetShouldShake(bool shake)
    {
        shouldShake = shake;
    }
    
    public void SetSmokeVFX(GameObject vfxPrefab)
    {
        smokeVFXPrefab = vfxPrefab;
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
        
        // Reproducir sonido de impacto del mortero
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
        
        // Instanciar partículas de humo
        if (smokeVFXPrefab != null)
        {
            GameObject smokeInstance = Instantiate(smokeVFXPrefab, transform.position, Quaternion.identity);
            ParticleSystem particleSystem = smokeInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Destroy(smokeInstance, duration);
            }
            else
            {
                Destroy(smokeInstance, 5f); // Fallback: destruir después de 5 segundos
            }
        }
        
        // Camera shake al impactar (solo si está activado)
        if (shouldShake)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                StartCoroutine(ShakeAndDestroy(mainCamera.transform));
                return; // No destruir todavía, esperar al shake
            }
        }
        
        // Detectar al jugador y al camión en el área
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
            // Verificar si es el camión
            else if (collider.CompareTag("Car"))
            {
                EntityStats carStats = collider.GetComponent<EntityStats>();
                if (carStats != null)
                {
                    carStats.TakeDamage(damageAmount);
                    Debug.Log($"[MortarImpactArea] ¡Mortero impactó al camión! Daño: {damageAmount}");
                }
            }
        }
        
        // Destruir el área después de haber causado daño
        Destroy(gameObject);
    }
    
    private IEnumerator ShakeAndDestroy(Transform cameraTransform)
    {
        // Instanciar partículas de humo
        if (smokeVFXPrefab != null)
        {
            GameObject smokeInstance = Instantiate(smokeVFXPrefab, transform.position, Quaternion.identity);
            ParticleSystem particleSystem = smokeInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Destroy(smokeInstance, duration);
            }
            else
            {
                Destroy(smokeInstance, 5f); // Fallback: destruir después de 5 segundos
            }
        }
        
        // Aplicar daño primero
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
            // Verificar si es el camión
            else if (collider.CompareTag("Car"))
            {
                EntityStats carStats = collider.GetComponent<EntityStats>();
                if (carStats != null)
                {
                    carStats.TakeDamage(damageAmount);
                    Debug.Log($"[MortarImpactArea] ¡Mortero impactó al camión! Daño: {damageAmount}");
                }
            }
        }
        
        // Hacer el shake
        yield return StartCoroutine(CameraShake(cameraTransform, shakeDuration, shakeIntensity));
        
        // Destruir después del shake
        Destroy(gameObject);
    }
    #endregion

    #region Camera Shake
    private IEnumerator CameraShake(Transform cameraTransform, float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
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
