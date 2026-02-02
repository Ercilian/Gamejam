using UnityEngine;
using System.Collections;
using Game.Enemies;

public class EnemyMortar : EnemyAttack
{
    [Header("Sistema de Objetivos")]
    [SerializeField] [Range(0f, 1f)] private float chanceToAttackCar = 0.3f; // Probabilidad de atacar al camión después del primer ataque
    [SerializeField] private string carTag = "Car"; // Tag del camión para buscar automáticamente
    
    [Header("Sistema de Reset por Daño")]
    [SerializeField] private bool enableDamageReset = true; // Activar reset por daño
    [SerializeField] private int damageThreshold = 50; // Daño necesario para resetear el ataque
    
    private GameObject carGameObject;
    private int accumulatedDamage = 0; // Daño acumulado desde el último reset
    private int lastKnownHealth; // Para trackear cambios de salud
    
    [Header("Configuración de Mortero")]
    [SerializeField] private float attackDelayBeforeFire = 4f; // Tiempo de espera antes de disparar
    [SerializeField] private int mortarProjectileCount = 3;
    [SerializeField] private float mortarDispersionRadius = 2f; // Radio de dispersión alrededor del objetivo
    [SerializeField] private float mortarFallDuration = 2f; // Tiempo que tarda en caer el mortero
    [SerializeField] private float mortarMaxHeight = 15f; // Altura máxima del mortero en el aire
    [SerializeField] private float mortarDamageRadius = 3f; // Radio del área de daño del mortero
    [SerializeField] private float delayBetweenProjectiles = 0.2f; // Delay entre cada proyectil lanzado
    [SerializeField] private float impactGroundHeight = 0f; // Altura Y donde impactan los morteros
    
    [Header("Prefabs")]
    [SerializeField] private GameObject mortarProjectilePrefab;
    [SerializeField] private GameObject mortarSmokeVFXPrefab;
    
    [Header("Efectos")]
    [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0, 2f, 0);
    
    private bool isAttacking = false;
    private bool isFirstAttack = true;
    private Coroutine currentAttackCoroutine;
    private IA_Enemy iaEnemy; // Referencia al componente de IA
    
    private void Start()
    {
        FindCar();
        
        // Obtener referencia al componente IA_Enemy
        iaEnemy = GetComponent<IA_Enemy>();
        if (iaEnemy == null)
        {
            Debug.LogWarning($"[{name}] No se encontró componente IA_Enemy. El enemigo no se detendrá al atacar.");
        }
        
        // Inicializar tracking de salud
        if (enemyStats != null)
        {
            lastKnownHealth = enemyStats.CurrentHP;
        }
    }
    
    private void FindCar()
    {
        // Método 1: Buscar por tag
        GameObject carByTag = GameObject.FindGameObjectWithTag(carTag);
        if (carByTag != null)
        {
            carGameObject = carByTag;
            if (showDebugLogs)
                Debug.Log($"[{name}] Camión encontrado por tag '{carTag}': {carGameObject.name}");
            return;
        }
        
        // Método 2: Buscar por componente MovCar
        MovCar movCar = FindFirstObjectByType<MovCar>();
        if (movCar != null)
        {
            carGameObject = movCar.gameObject;
            if (showDebugLogs)
                Debug.Log($"[{name}] Camión encontrado por componente MovCar: {carGameObject.name}");
            return;
        }
        
        // Método 3: Buscar por componente CarFuelSystem
        CarFuelSystem carFuel = FindFirstObjectByType<CarFuelSystem>();
        if (carFuel != null)
        {
            carGameObject = carFuel.gameObject;
            if (showDebugLogs)
                Debug.Log($"[{name}] Camión encontrado por componente CarFuelSystem: {carGameObject.name}");
            return;
        }
        
        if (showDebugLogs)
            Debug.LogWarning($"[{name}] No se encontró el camión. Intenta asignar el tag '{carTag}' al camión.");
    }
    
    private void CheckDamageThreshold()
    {
        if (!enableDamageReset || enemyStats == null)
            return;
        
        int currentHealth = enemyStats.CurrentHP;
        
        // Detectar si se recibió daño
        if (currentHealth < lastKnownHealth)
        {
            int damageTaken = lastKnownHealth - currentHealth;
            accumulatedDamage += damageTaken;
            
            if (showDebugLogs)
                Debug.Log($"[{name}] Daño recibido: {damageTaken}, Acumulado: {accumulatedDamage}/{damageThreshold}");
            
            // Si se alcanzó el threshold, resetear
            if (accumulatedDamage >= damageThreshold)
            {
                ResetAttackPattern();
            }
        }
        
        lastKnownHealth = currentHealth;
    }
    
    private void ResetAttackPattern()
    {
        // Cancelar el ataque en curso si existe
        if (currentAttackCoroutine != null && isAttacking)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
            isAttacking = false;
            
            // Reactivar movimiento si fue cancelado
            if (iaEnemy != null)
            {
                iaEnemy.enabled = true;
            }
            
            if (showDebugLogs)
                Debug.Log($"[{name}] ❌ Ataque cancelado por daño");
        }
        
        // Reiniciar el cooldown completo - tiene que esperar todo el cooldown antes de atacar
        lastAttackTime = Time.time;
        
        // Resetear daño acumulado
        accumulatedDamage = 0;
        
        if (showDebugLogs)
            Debug.Log($"[{name}] 🔄 COOLDOWN REINICIADO - Debe esperar {attackCooldown}s antes del próximo ataque");
    }
    
    private void Update()
    {
        // Si el enemigo está muerto, no hacer nada
        if (enemyStats != null && enemyStats.CurrentHP <= 0)
            return;
        
        // Verificar si se ha recibido daño y actualizar el threshold
        CheckDamageThreshold();
        
        // Si no puede atacar o está atacando, no hacer nada
        if (!CanAttack() || isAttacking)
            return;
        
        // Re-buscar camión si se perdió la referencia
        if (carGameObject == null)
        {
            FindCar();
        }
        
        // Buscar todos los jugadores en rango
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        System.Collections.Generic.List<Transform> playersInRange = new System.Collections.Generic.List<Transform>();
        
        foreach (GameObject player in allPlayers)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange)
            {
                playersInRange.Add(player.transform);
            }
        }
        
        // Si es el primer ataque, requiere jugadores en rango
        if (isFirstAttack && playersInRange.Count == 0)
            return;
        
        // Después del primer ataque, puede atacar al camión aunque no haya jugadores en rango
        if (!isFirstAttack && playersInRange.Count == 0 && carGameObject != null)
        {
            // Atacar directamente al camión si no hay jugadores en rango
            if (showDebugLogs)
                Debug.Log($"[{name}] No hay jugadores en rango, atacando al camión por defecto");
            TryAttack(carGameObject.transform);
            return;
        }
        
        // Si hay jugadores en rango, decidir objetivo normalmente
        if (playersInRange.Count > 0)
        {
            Transform finalTarget = DecideTarget(playersInRange);
            
            if (showDebugLogs && finalTarget != null)
                Debug.Log($"[{name}] 🎯 Final Target decidido en Update: {finalTarget.name}");
            
            if (finalTarget != null)
            {
                TryAttack(finalTarget);
            }
        }
    }
    
    /// <summary>
    /// Sobrescribe TryAttack para ignorar el target del IA_Enemy y usar nuestra propia lógica de decisión
    /// </summary>
    public override bool TryAttack(Transform target)
    {
        if (showDebugLogs)
            Debug.Log($"[{name}] 👁️ TryAttack llamado (probablemente por IA_Enemy) con target: {(target != null ? target.name : "NULL")} - IGNORANDO y usando DecideTarget");
        
        // No usar el target que nos pasa el IA, decidir nuestro propio objetivo
        if (!CanAttack())
        {
            if (showDebugLogs) Debug.Log($"[{name}] Ataque en cooldown.");
            return false;
        }
        
        // Buscar jugadores en rango
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        System.Collections.Generic.List<Transform> playersInRange = new System.Collections.Generic.List<Transform>();
        
        foreach (GameObject player in allPlayers)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange)
            {
                playersInRange.Add(player.transform);
            }
        }
        
        // Si es primer ataque y no hay jugadores, no atacar
        if (isFirstAttack && playersInRange.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log($"[{name}] Primer ataque pero no hay jugadores en rango");
            return false;
        }
        
        // Si no es primer ataque y no hay jugadores, puede atacar al camión
        if (!isFirstAttack && playersInRange.Count == 0 && carGameObject != null)
        {
            if (showDebugLogs)
                Debug.Log($"[{name}] No hay jugadores, atacando al camión por defecto");
            
            // Llamar al TryAttack base con el camión
            return base.TryAttack(carGameObject.transform);
        }
        
        // Decidir objetivo usando nuestra lógica
        if (playersInRange.Count > 0)
        {
            Transform ourTarget = DecideTarget(playersInRange);
            
            if (showDebugLogs && ourTarget != null)
                Debug.Log($"[{name}] ✅ Target decidido por EnemyMortar: {ourTarget.name}");
            
            if (ourTarget != null)
            {
                // Llamar al TryAttack base con nuestro target
                return base.TryAttack(ourTarget);
            }
        }
        
        return false;
    }
    
    private Transform DecideTarget(System.Collections.Generic.List<Transform> playersInRange)
    {
        if (showDebugLogs)
            Debug.Log($"[{name}] DecideTarget llamado - isFirstAttack: {isFirstAttack}, chanceToAttackCar: {chanceToAttackCar}, playersInRange: {playersInRange.Count}");
        
        // SIEMPRE: Primer ataque debe ir a un jugador, nunca al camión
        if (isFirstAttack)
        {
            if (playersInRange.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[{name}] Primer ataque - No hay jugadores en rango!");
                return null;
            }
            
            int randomIndex = Random.Range(0, playersInRange.Count);
            Transform randomPlayer = playersInRange[randomIndex];
            if (showDebugLogs)
                Debug.Log($"[{name}] ⭐ PRIMER ATAQUE - Objetivo: Jugador aleatorio ({randomPlayer.name})");
            return randomPlayer;
        }
        
        // Después del primer ataque, decidir por probabilidad
        float randomValue = Random.value;
        
        if (showDebugLogs)
            Debug.Log($"[{name}] Random value: {randomValue:F3}, chanceToAttackCar: {chanceToAttackCar:F3}, comparación: {randomValue} <= {chanceToAttackCar} = {randomValue <= chanceToAttackCar}");
        
        if (randomValue <= chanceToAttackCar && carGameObject != null)
        {
            // Atacar al camión
            if (showDebugLogs)
                Debug.Log($"[{name}] 🚗 Ataque al CAMIÓN (probabilidad: {randomValue:F2} <= {chanceToAttackCar:F2})");
            return carGameObject.transform;
        }
        else
        {
            // Atacar a un jugador aleatorio en rango
            if (playersInRange.Count == 0)
            {
                // Si no hay jugadores, atacar al camión como fallback
                if (carGameObject != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[{name}] No hay jugadores en rango, atacando al camión por defecto");
                    return carGameObject.transform;
                }
                
                if (showDebugLogs)
                    Debug.LogWarning($"[{name}] No hay jugadores en rango ni camión disponible");
                return null;
            }
            
            int randomIndex = Random.Range(0, playersInRange.Count);
            Transform randomPlayer = playersInRange[randomIndex];
            if (showDebugLogs)
                Debug.Log($"[{name}] 👤 Ataque al JUGADOR ({randomPlayer.name}) (probabilidad: {randomValue:F2} > {chanceToAttackCar:F2})");
            return randomPlayer;
        }
    }
    
    protected override void ExecuteAttack(Transform target)
    {
        animator.SetTrigger("Attack");

        // Marcar que ya no es el primer ataque SOLO después de que inicie el ataque
        if (showDebugLogs)
        {
            Debug.Log($"[{name}] ExecuteAttack - isFirstAttack antes: {isFirstAttack}");
            Debug.Log($"[{name}] 📍 ExecuteAttack recibió target: {(target != null ? target.name : "NULL")}");
        }
        
        // Solo cambiar isFirstAttack en el PRIMER ataque absoluto
        if (isFirstAttack)
        {
            isFirstAttack = false;
            if (showDebugLogs)
                Debug.Log($"[{name}] Primer ataque completado, isFirstAttack ahora es false permanentemente");
        }
        
        // Guardar referencia de la coroutine para poder cancelarla
        currentAttackCoroutine = StartCoroutine(MortarAttack(target));
    }
    
    private IEnumerator MortarAttack(Transform target)
    {
        isAttacking = true;
        
        // Detener el movimiento del enemigo
        if (iaEnemy != null)
        {
            iaEnemy.enabled = false;
            if (showDebugLogs)
                Debug.Log($"[{name}] 🛑 Movimiento detenido para atacar");
        }
        
        // Detener la velocidad del rigidbody si existe
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[{name}] 💣 Ataque de mortero iniciado!");
            Debug.Log($"[{name}] ⏱️ Esperando {attackDelayBeforeFire}s antes de disparar...");
        }
        
        // Esperar el delay configurado antes de disparar
        yield return new WaitForSeconds(attackDelayBeforeFire);
        
        if (showDebugLogs)
        {
            Debug.Log($"[{name}] 🎯 Target: {target.name} en posición {target.position}");
        }
        
        for (int i = 0; i < mortarProjectileCount; i++)
        {
            // Los morteros caen directamente sobre el objetivo con dispersión mínima
            Vector2 randomOffset = Random.insideUnitCircle * mortarDispersionRadius;
            Vector3 targetPosition = target.position + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            if (showDebugLogs)
                Debug.Log($"[{name}] Proyectil {i+1}/{mortarProjectileCount} lanzado hacia {target.name}: {targetPosition}");
            
            SpawnMortarProjectile(targetPosition);
            
            yield return new WaitForSeconds(delayBetweenProjectiles);
        }
        
        // Reactivar el movimiento del enemigo
        if (iaEnemy != null)
        {
            iaEnemy.enabled = true;
            if (showDebugLogs)
                Debug.Log($"[{name}] ✅ Movimiento reactivado");
        }
        
        isAttacking = false;
        currentAttackCoroutine = null; // Limpiar referencia
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
        
        // Añadir componente independiente que anima el proyectil
        MortarProjectileAnimator animator = projectile.AddComponent<MortarProjectileAnimator>();
        animator.Initialize(spawnPos, impactPosition, mortarFallDuration, mortarMaxHeight);
        
        // Crear el área de impacto en el suelo
        CreateMortarImpactArea(impactPosition);
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
        
        // Asignar prefab de humo si está disponible
        if (mortarSmokeVFXPrefab != null)
        {
            mortarArea.SetSmokeVFX(mortarSmokeVFXPrefab);
        }
        
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
