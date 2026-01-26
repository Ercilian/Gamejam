using UnityEngine;
using System.Collections;
using Game.Enemies;

public class EnemyMortar : EnemyAttack
{
    [Header("Configuración de Mortero")]
    [SerializeField] private int mortarProjectileCount = 3;
    [SerializeField] private float mortarRandomRadius = 8f; // Radio del área aleatoria de caída
    [SerializeField] private Vector3 mortarAttackAreaCenter = Vector3.zero; // Centro del área de morteros (relativo al enemigo)
    [SerializeField] private float mortarFallDuration = 2f; // Tiempo que tarda en caer el mortero
    [SerializeField] private float mortarMaxHeight = 15f; // Altura máxima del mortero en el aire
    [SerializeField] private float mortarDamageRadius = 3f; // Radio del área de daño del mortero
    [SerializeField] private float delayBetweenProjectiles = 0.2f; // Delay entre cada proyectil lanzado
    [SerializeField] private float impactGroundHeight = 0f; // Altura Y donde impactan los morteros
    
    [Header("Prefabs")]
    [SerializeField] private GameObject mortarProjectilePrefab;
    
    [Header("Efectos")]
    [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0, 2f, 0);
    
    private bool isAttacking = false;
    
    private void Update()
    {
        // Si el enemigo está muerto, no hacer nada
        if (enemyStats != null && enemyStats.CurrentHP <= 0)
            return;
        
        // Buscar jugador cercano
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;
        
        Transform target = player.transform;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Si está en rango y puede atacar
        if (distanceToTarget <= attackRange && CanAttack() && !isAttacking)
        {
            TryAttack(target);
        }
    }
    
    protected override void ExecuteAttack(Transform target)
    {
        StartCoroutine(MortarAttack(target));
    }
    
    private IEnumerator MortarAttack(Transform target)
    {
        isAttacking = true;
        
        if (showDebugLogs)
            Debug.Log($"[{name}] Ataque de mortero iniciado!");
        
        // Centro del área de morteros en el mundo
        Vector3 areaCenter = target.position + mortarAttackAreaCenter;
        
        for (int i = 0; i < mortarProjectileCount; i++)
        {
            // Generar posición aleatoria dentro del área
            Vector2 randomOffset = Random.insideUnitCircle * mortarRandomRadius;
            Vector3 randomPosition = areaCenter + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            SpawnMortarProjectile(randomPosition);
            
            yield return new WaitForSeconds(delayBetweenProjectiles);
        }
        
        isAttacking = false;
    }
    
    private void SpawnMortarProjectile(Vector3 impactPosition)
    {
        if (mortarProjectilePrefab == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[{name}] mortarProjectilePrefab no asignado!");
            return;
        }
        
        Vector3 spawnPos = transform.position + projectileSpawnOffset;
        GameObject projectile = Instantiate(mortarProjectilePrefab, spawnPos, Quaternion.identity);
        
        // Asegurar que el impacto sea en el suelo
        impactPosition.y = impactGroundHeight;
        
        // Animar el proyectil como parábola
        StartCoroutine(AnimateMortarProjectile(projectile, spawnPos, impactPosition));
        
        // Crear el área de impacto en el suelo
        CreateMortarImpactArea(impactPosition);
    }
    
    private IEnumerator AnimateMortarProjectile(GameObject projectile, Vector3 startPos, Vector3 endPos)
    {
        float duration = mortarFallDuration;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && projectile != null)
        {
            float t = elapsedTime / duration;
            
            // Interpolación horizontal
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);
            
            // Parábola vertical (sube y baja)
            float arcHeight = Mathf.Sin(t * Mathf.PI) * mortarMaxHeight;
            
            projectile.transform.position = horizontalPos + Vector3.up * arcHeight;
            
            // Rotar hacia la dirección de movimiento
            Vector3 direction = (endPos - startPos).normalized;
            if (direction != Vector3.zero)
            {
                projectile.transform.rotation = Quaternion.LookRotation(direction);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Destruir el proyectil al terminar la trayectoria
        if (projectile != null)
        {
            Destroy(projectile);
        }
    }
    
    private void CreateMortarImpactArea(Vector3 impactPosition)
    {
        // Crear un GameObject para el área de impacto
        GameObject impactArea = new GameObject("MortarImpactArea");
        impactArea.transform.position = impactPosition;
        
        // Agregar el componente de daño
        MortarImpactArea mortarArea = impactArea.AddComponent<MortarImpactArea>();
        mortarArea.SetDamageRadius(mortarDamageRadius);
        mortarArea.SetDelayBeforeDamage(mortarFallDuration);
        // Usar el daño del ScriptableObject si está disponible, sino usar baseDamage
        int damage = (enemyStats != null) ? enemyStats.AttackDamage : baseDamage;
        mortarArea.SetDamageAmount(damage);
        
        // Visualizar el área con LineRenderer
        LineRenderer lineRenderer = impactArea.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.5f, 0f, 0.8f); // Naranja
        lineRenderer.endColor = new Color(1f, 0.5f, 0f, 0.8f);
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.useWorldSpace = false;
        
        // Dibujar círculo en el suelo
        int segments = 40;
        Vector3[] positions = new Vector3[segments + 1];
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            positions[i] = new Vector3(Mathf.Cos(angle) * mortarDamageRadius, 0f, Mathf.Sin(angle) * mortarDamageRadius);
        }
        
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        
        if (showDebugLogs)
            Debug.Log($"[{name}] Área de impacto de mortero creada en: {impactPosition}");
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!drawGizmos) return;
        
        // Dibujar rango de ataque
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
