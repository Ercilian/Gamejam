using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FrogCombat : MonoBehaviour
{
    #region Enumerations
    private enum BossPhase
    {
        Phase1,
        Phase2
    }

    private enum AttackType
    {
        BulletHell,
        JumpAttack,
        MortarAttack,
        SpawnEnemies,
        PushAttack
    }
    #endregion

    #region Boss Configuration
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float phaseTransitionHealth = 80f;
    
    [Header("Attack Configuration")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float mortarSpeed = 5f;
    
    [Header("Bullet Hell Settings")]
    [SerializeField] private int bulletPatternCount = 8;
    [SerializeField] private float bulletHellCooldown = 3f;
    [SerializeField] private float bulletHellBaseDirection = 0f; // 0 = hacia adelante, 90 = arriba, etc
    [SerializeField] private float bulletHellArcHeight = 2f; // Altura del arco de los proyectiles
    [SerializeField] private bool enableBulletHell = true;
    
    [Header("Jump Attack Settings")]
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 1.5f;
    [SerializeField] private float jumpCooldown = 4f;
    [SerializeField] private float jumpImpactRadius = 6f;
    [SerializeField] private float jumpImpactDamage = 30f;
    [SerializeField] private bool enableJumpAttack = true;
    
    [Header("Push Attack Settings")]
    [SerializeField] private float pushDetectionRadius = 4f;
    [SerializeField] private float pushForce = 20f;
    [SerializeField] private float pushCooldown = 4.5f;
    [SerializeField] private float pushChargeDuration = 1f;
    [SerializeField] private bool enablePushAttack = true;
    
    [Header("Mortar Attack Settings")]
    [SerializeField] private int mortarProjectileCount = 6;
    [SerializeField] private float mortarCooldown = 4f;
    [SerializeField] private float mortarRandomRadius = 15f; // Radio del área aleatoria de caída
    [SerializeField] private Vector3 mortarAttackAreaCenter = Vector3.zero; // Centro del área de morteros (relativo al boss)
    [SerializeField] private float mortarFallDuration = 2f; // Tiempo que tarda en caer el mortero
    [SerializeField] private float mortarMaxHeight = 25f; // Altura máxima del mortero en el aire
    [SerializeField] private float mortarDamageRadius = 3f; // Radio del área de daño del mortero
    [SerializeField] private bool enableMortarAttack = true;
    
    [Header("Enemy Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemiesToSpawn = 3;
    [SerializeField] private Transform[] enemySpawnPoints; // Puntos de spawn de enemigos
    [SerializeField] private float spawnCooldown = 5f;
    [SerializeField] private float delayBetweenEnemySpawns = 0.5f; // Delay entre spawns de enemigos
    [SerializeField] private bool enableSpawnEnemies = true;
    
    [Header("Projectile Configuration")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject mortarProjectilePrefab;
    [SerializeField] private Vector3 bulletHellSpawnOffset = Vector3.zero; // Offset para bullet hell
    [SerializeField] private Vector3 projectileSpawnOffset = Vector3.zero; // Offset general (heredado)
    
    [Header("UI & Effects")]
    [SerializeField] private ParticleSystem attackEffectPrefab;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioSource audioSource;
    #endregion

    #region State Variables
    private float currentHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private bool isDefeated = false;
    private bool isJumping = false;
    private float lastJumpTime = 0f;
    private float lastPushTime = 0f;
    
    private float lastAttackTime = 0f;
    private float lastBulletHellTime = 0f;
    private float lastMortarTime = 0f;
    private float lastSpawnTime = 0f;
    
    private Rigidbody rb;
    private Animator animator;
    #endregion

    #region Initialization
    private void Start()
    {
        InitializeBoss();
        animator = GetComponentInChildren<Animator>();
    }

    private void InitializeBoss()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        
        // Proteger el boss de ser destruido entre escenas
        DontDestroyOnLoad(gameObject);
        
        // Configurar Rigidbody para que el boss sea estático pero pueda moverse con scripts
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        if (targetTransform == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
                targetTransform = player.transform;
        }
        
        Debug.Log($"[FrogBoss] Boss initialized with {maxHealth} HP");
    }
    #endregion

    #region Update Loop
    private void Update()
    {
        if (isDefeated)
            return;
        
        UpdateBossPhase();
        HandleMovement();
        HandleAttackPattern();
    }

    private void UpdateBossPhase()
    {
        if (currentPhase == BossPhase.Phase1 && currentHealth <= phaseTransitionHealth)
        {
            TransitionToPhase2();
        }
    }

    private void HandleMovement()
    {
        // El boss es estático, no se mueve horizontalmente
        // Durante los saltos, la posición se controla manualmente en el coroutine
        if (!isJumping && rb != null)
        {
            // Para un Rigidbody kinematic, usar MovePosition en lugar de linearVelocity
            rb.MovePosition(transform.position);
        }
    }

    private void HandleAttackPattern()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;
        
        if (timeSinceLastAttack < attackCooldown)
            return;
        
        List<AttackType> availableAttacks = GetAvailableAttacks();
        
        if (availableAttacks.Count > 0)
        {
            AttackType selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];
            ExecuteAttack(selectedAttack);
            lastAttackTime = Time.time;
        }
    }

    private List<AttackType> GetAvailableAttacks()
    {
        List<AttackType> attacks = new List<AttackType>();
        float distanceToPlayer = targetTransform != null ? Vector3.Distance(transform.position, targetTransform.position) : float.MaxValue;
        
        if (enableBulletHell && Time.time - lastBulletHellTime > bulletHellCooldown)
            attacks.Add(AttackType.BulletHell);
        
        if (enableJumpAttack && Time.time - lastJumpTime > jumpCooldown)
            attacks.Add(AttackType.JumpAttack);
        
        if (enableMortarAttack && Time.time - lastMortarTime > mortarCooldown)
            attacks.Add(AttackType.MortarAttack);
        
        // Push Attack solo cuando el jugador está cerca
        if (enablePushAttack && Time.time - lastPushTime > pushCooldown && distanceToPlayer <= pushDetectionRadius * 1.5f)
            attacks.Add(AttackType.PushAttack);
        
        if (enableSpawnEnemies && Time.time - lastSpawnTime > spawnCooldown)
            attacks.Add(AttackType.SpawnEnemies);
        
        return attacks;
    }
    #endregion

    #region Attack Execution
    private void ExecuteAttack(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.BulletHell:
                StartCoroutine(BulletHellAttack());
                break;
            case AttackType.JumpAttack:
                StartCoroutine(JumpAttack());
                break;
            case AttackType.MortarAttack:
                StartCoroutine(MortarAttack());
                break;
            case AttackType.PushAttack:
                StartCoroutine(PushAttack());
                break;
            case AttackType.SpawnEnemies:
                StartCoroutine(SpawnEnemies());
                break;
        }
    }

    private IEnumerator BulletHellAttack()
    {
        animator.SetTrigger("BulletHellAttack");
        yield return new WaitForSeconds(0.5f); // Esperar a que la animación comience
        lastBulletHellTime = Time.time;
        PlayAttackEffect();
        PlayAttackSFX();
        
        Debug.Log("[FrogBoss] Bullet Hell Attack - Arc Pattern!");
        
        yield return new WaitForSeconds(0.3f);
        
        // Patrón de arco (Mega Satan style) - bolas salen en un arco frontal
        float arcWidth = 120f; // Ancho del arco en grados
        float startAngle = bulletHellBaseDirection - arcWidth * 0.5f; // Comienza a la izquierda del arco
        
        for (int i = 0; i < bulletPatternCount; i++)
        {
            float angle = startAngle + (arcWidth / (bulletPatternCount - 1)) * i;
            Vector2 direction = GetDirectionFromAngle(angle);
            SpawnProjectile(direction, bulletSpeed, false);
        }
        
        yield return new WaitForSeconds(bulletHellCooldown * 0.5f);
        
        // Segunda ola en Phase 2 - arco expandido
        if (currentPhase == BossPhase.Phase2)
        {
            float phase2ArcWidth = 150f; // Arco más amplio en fase 2
            float phase2StartAngle = bulletHellBaseDirection - phase2ArcWidth * 0.5f;
            
            for (int i = 0; i < bulletPatternCount; i++)
            {
                float angle = phase2StartAngle + (phase2ArcWidth / (bulletPatternCount - 1)) * i;
                Vector2 direction = GetDirectionFromAngle(angle);
                
                SpawnProjectile(direction, bulletSpeed, false);
            }
        }
    }

    private IEnumerator JumpAttack()
    {
        lastJumpTime = Time.time;
        PlayAttackEffect();
        PlayAttackSFX();
        
        Debug.Log("[FrogBoss] Jump Attack - Saltando hacia el jugador!");
        
        isJumping = true;
        
        // Desactivar restricciones temporalmente para el salto
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
        }
        rb.linearVelocity = Vector3.zero;
        
        Vector3 bossStartPosition = transform.position;
        Vector3 jumpTargetPosition = targetTransform != null ? targetTransform.position : bossStartPosition;
        
        Vector3 midPoint = (bossStartPosition + jumpTargetPosition) * 0.5f;
        Vector3 peakPosition = midPoint + Vector3.up * jumpHeight;
        
        // Fase de subida hacia el jugador
        float elapsedTime = 0f;
        while (elapsedTime < jumpDuration * 0.5f)
        {
            float t = elapsedTime / (jumpDuration * 0.5f);
            transform.position = Vector3.Lerp(bossStartPosition, peakPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Fase de caída hacia el jugador
        elapsedTime = 0f;
        while (elapsedTime < jumpDuration * 0.5f)
        {
            float t = elapsedTime / (jumpDuration * 0.5f);
            transform.position = Vector3.Lerp(peakPosition, jumpTargetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Aplicar daño en área sin cambiar la posición del boss
        Collider[] hitColliders = Physics.OverlapSphere(jumpTargetPosition, jumpImpactRadius);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject != gameObject)
            {
                if (collider.CompareTag("Player") || collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    EntityStats entityStats = collider.GetComponent<EntityStats>();
                    if (entityStats != null)
                    {
                        entityStats.TakeDamage((int)jumpImpactDamage);
                        Debug.Log($"[FrogBoss] Jump impact hit {collider.gameObject.name} for {jumpImpactDamage} damage!");
                    }
                }
            }
        }
        
        // Impacto al aterrizar - efecto en la posición del salto, pero boss permanece en el aire
        PlayAttackEffect();
        PlayAttackSFX();
        
        // Pausa después del impacto
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[FrogBoss] Saltando de vuelta a posición original!");
        
        // Posición intermedia para el regreso
        Vector3 returnPeakPosition = (jumpTargetPosition + bossStartPosition) * 0.5f + Vector3.up * (jumpHeight * 0.8f);
        
        // Subida de regreso
        elapsedTime = 0f;
        while (elapsedTime < jumpDuration * 0.5f)
        {
            float t = elapsedTime / (jumpDuration * 0.5f);
            transform.position = Vector3.Lerp(jumpTargetPosition, returnPeakPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Caída de regreso
        elapsedTime = 0f;
        while (elapsedTime < jumpDuration * 0.5f)
        {
            float t = elapsedTime / (jumpDuration * 0.5f);
            transform.position = Vector3.Lerp(returnPeakPosition, bossStartPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Asegurar que vuelve a la posición original
        transform.position = bossStartPosition;
        rb.linearVelocity = Vector3.zero;
        isJumping = false;
        
        // Restaurar restricciones de física
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        Debug.Log("[FrogBoss] Volvió a posición fija!");
    }

    private IEnumerator MortarAttack()
    {
        animator.SetTrigger("MortarAttack");
        yield return new WaitForSeconds(1.5f); // Esperar a que la animación comience
        lastMortarTime = Time.time;
        PlayAttackEffect();
        PlayAttackSFX();
        
        Debug.Log("[FrogBoss] Mortar Attack!");
        
        // Centro del área de morteros en el mundo
        Vector3 areaCenter = transform.position + mortarAttackAreaCenter;
        
        for (int i = 0; i < mortarProjectileCount; i++)
        {
            // Generar posición aleatoria dentro del área
            Vector2 randomOffset = Random.insideUnitCircle * mortarRandomRadius;
            Vector3 randomPosition = areaCenter + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            SpawnMortarProjectile(randomPosition);
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator PushAttack()
    {
        animator.SetTrigger("PushAttack");
        yield return new WaitForSeconds(0.2f); // Esperar a que la animación comience
        lastPushTime = Time.time;
        PlayAttackSFX();
        
        Debug.Log("[FrogBoss] Push Attack!");
        
        // Fase de carga - mostrar área de ataque
        VisualizeAttackArea();
        
        float chargeElapsed = 0f;
        while (chargeElapsed < pushChargeDuration)
        {
            chargeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ejecutar el empuje
        PlayAttackEffect();
        PlayAttackSFX();
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pushDetectionRadius);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject != gameObject)
            {
                // Verificar si es el jugador por tag o layer
                if (collider.CompareTag("Player") || collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Rigidbody targetRb = collider.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        Vector3 pushDirection = (collider.transform.position - transform.position).normalized;
                        pushDirection.y = 0; // Solo empuje horizontal
                        targetRb.linearVelocity = new Vector3(pushDirection.x * pushForce, targetRb.linearVelocity.y, pushDirection.z * pushForce);
                        
                        Debug.Log($"[FrogBoss] Pushed {collider.gameObject.name}!");
                    }
                }
            }
        }
    }
    #endregion

    #region Projectile Management
    private void SpawnProjectile(Vector2 direction, float speed, bool isMortar)
    {
        if (projectilePrefab == null) return;
        
        Vector3 spawnPos = transform.position + bulletHellSpawnOffset;
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        // Desactivar Rigidbody si existe
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            projRb.isKinematic = true;
            projRb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        // Iniciar corrutina de trayectoria de arco
        StartCoroutine(AnimateProjectileArc(projectile, direction, speed, bulletHellArcHeight));
    }

    private IEnumerator AnimateProjectileArc(GameObject projectile, Vector2 direction, float speed, float arcHeight)
    {
        Vector3 startPos = projectile.transform.position;
        Vector3 dir3D = new Vector3(direction.x, 0, direction.y).normalized;
        
        // Distancia de viaje del proyectil
        float travelDistance = 50f;
        float duration = travelDistance / speed;
        
        float elapsedTime = 0f;
        float damageRadius = 1.5f; // Radio para detectar al jugador
        bool hasDamagedPlayer = false;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            
            // Posición horizontal (línea recta)
            Vector3 horizontalPos = startPos + dir3D * (travelDistance * t);
            
            // Altura del arco (parábola)
            float arcY = Mathf.Sin(t * Mathf.PI) * arcHeight;
            
            // Posición final
            projectile.transform.position = horizontalPos + Vector3.up * arcY;
            
            // Rotar hacia la dirección de movimiento
            if (dir3D != Vector3.zero)
                projectile.transform.rotation = Quaternion.LookRotation(dir3D);
            
            // Detectar colisión con el jugador por distancia
            if (targetTransform != null && !hasDamagedPlayer)
            {
                float distanceToPlayer = Vector3.Distance(projectile.transform.position, targetTransform.position);
                if (distanceToPlayer < damageRadius)
                {
                    // Verificar si el jugador está en dash
                    Dash dashComponent = targetTransform.GetComponent<Dash>();
                    if (dashComponent != null && dashComponent.IsDashing)
                    {
                        Debug.Log("[FrogBoss] Proyectil tocó jugador en dash - sin daño");
                    }
                    else
                    {
                        // Causar daño al jugador
                        EntityStats playerStats = targetTransform.GetComponent<EntityStats>();
                        if (playerStats != null)
                        {
                            playerStats.TakeDamage(15); // Daño de 15
                            Debug.Log($"[FrogBoss] ¡Proyectil impactó al jugador! Daño: 15");
                            hasDamagedPlayer = true;
                            
                            // Destruir proyectil inmediatamente al impactar
                            Destroy(projectile);
                            yield break; // Salir de la corrutina
                        }
                    }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Destruir proyectil al final de la trayectoria si no impactó
        Destroy(projectile);
    }

    private void SpawnMortarProjectile(Vector3 impactPosition)
    {
        if (mortarProjectilePrefab == null)
            return;
        
        Vector3 spawnPos = transform.position + projectileSpawnOffset;
        GameObject projectile = Instantiate(mortarProjectilePrefab, spawnPos, Quaternion.identity);
        
        // Asegurar que el impacto sea en el suelo
        impactPosition.y = spawnPos.y;
        
        // Animar el proyectil como parábola pura
        StartCoroutine(AnimateMortarProjectile(projectile, spawnPos, impactPosition));
        
        // Crear el área de impacto en el suelo
        CreateMortarImpactArea(impactPosition);
    }
    
    private IEnumerator AnimateMortarProjectile(GameObject projectile, Vector3 startPos, Vector3 endPos)
    {
        float duration = mortarFallDuration; // Usar el valor ajustable
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && projectile != null)
        {
            float t = elapsedTime / duration;
            
            // Interpolación horizontal
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);
            
            // Parábola vertical (sube y baja) - usa la altura máxima ajustable
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
        
        Debug.Log("[FrogBoss] Área de impacto de mortero creada en: " + impactPosition);
    }
    #endregion

    #region Phase Transition
    private void TransitionToPhase2()
    {
        currentPhase = BossPhase.Phase2;
        bulletSpeed *= 1.1f;
        mortarSpeed *= 1.1f;
        attackCooldown *= 0.8f;
        
        PlayAttackEffect();
        Debug.Log("[FrogBoss] ¡TRANSICIÓN A FASE 2! El boss está más fuerte!");
    }
    #endregion

    #region Damage & Health
    public void TakeDamage(float damage)
    {
        if (isDefeated)
            return; // Ignorar daño si el boss ya fue derrotado
        
        currentHealth -= damage;
        
        Debug.Log($"[FrogBoss] Damage taken: {damage}. HP: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DefeatedBoss();
        }
    }

    private void DefeatedBoss()
    {
        if (isDefeated)
            return; // Prevenir múltiples llamadas
        
        isDefeated = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        Debug.Log("[FrogBoss] ¡EL BOSS HA SIDO DERROTADO!");
        
        // Efecto de derrota
        if (attackEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
        }
        
        gameObject.SetActive(false);
    }
    #endregion

    #region Utility Methods
    private Vector2 GetDirectionFromAngle(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void PlayAttackEffect()
    {
        if (attackEffectPrefab == null)
            return;
        
        ParticleSystem effect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity);
        Destroy(effect.gameObject, 1f);
    }

    private void PlayAttackSFX()
    {
        if (audioSource != null && attackSFX != null)
        {
            audioSource.PlayOneShot(attackSFX);
        }
    }

    private void VisualizeAttackArea()
    {
        StartCoroutine(ShowAttackAreaVisualization());
    }

    private IEnumerator ShowAttackAreaVisualization()
    {
        // Crear un GameObject temporal para visualizar el área
        GameObject areaVisual = new GameObject("PushAttackArea");
        areaVisual.transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
        
        // Agregar un LineRenderer para dibujar el círculo
        LineRenderer lineRenderer = areaVisual.AddComponent<LineRenderer>();
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
            positions[i] = new Vector3(Mathf.Cos(angle) * pushDetectionRadius, 0f, Mathf.Sin(angle) * pushDetectionRadius);
        }
        
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        
        // Mostrar el área durante la carga
        yield return new WaitForSeconds(pushChargeDuration);
        
        // Destruir el área visual
        Destroy(areaVisual);
    }

    private IEnumerator SpawnEnemies()
    {
        lastSpawnTime = Time.time;
        PlayAttackEffect();
        PlayAttackSFX();
        
        Debug.Log($"[FrogBoss] Spawning {enemiesToSpawn} enemies!");
        
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[FrogBoss] Enemy prefab not assigned!");
            yield break;
        }
        
        // Verificar si hay puntos de spawn definidos
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[FrogBoss] No enemy spawn points assigned!");
            yield break;
        }
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Seleccionar un punto de spawn aleatorio
            Transform randomSpawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            Vector3 spawnPosition = randomSpawnPoint.position;
            
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[FrogBoss] Enemy {i + 1} spawned at {spawnPosition}");
            
            // Esperar antes de spawnear el siguiente enemigo
            if (i < enemiesToSpawn - 1)
            {
                yield return new WaitForSeconds(delayBetweenEnemySpawns);
            }
        }
    }
    #endregion

    #region Gizmos & Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpImpactRadius);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pushDetectionRadius);
    }
    #endregion
}
