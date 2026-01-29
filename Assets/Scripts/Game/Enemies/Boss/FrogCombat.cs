using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Enemies;

[System.Serializable]
public class AttackPhaseConfig
{
    public bool enableInPhase1 = false;
    public bool enableInPhase2 = false;
}

[System.Serializable]
public class SpiralPatternConfig
{
    [Header("DNA Hélice Doble")]
    public int helixSegments = 8;
    public int projectilesPerSegment = 2;
    public float spreadAngle = 10f;
    public float rotationSpeed = 30f;
    public int bridgeCount = 4;
    public int bridgeProjectiles = 3;
    public float bridgeSpread = 8f;
}

[System.Serializable]
public class WavePatternConfig
{
    [Header("Pétalos de Flor")]
    public int petalCount = 6;
    public int bulletsPerPetal = 8;
    public float petalWidth = 45f;
    public float curveFactor = 2f;
    public int centerRingBullets = 12;
    public float centerRingSpeed = 0.6f;
}

[System.Serializable]
public class RotatingStarConfig
{
    [Header("Estrella Rotante")]
    public int starArms = 5;
    public int bulletsPerArm = 4;
    public float armSpread = 5f;
    public float speedIncrement = 0.5f;
    public float rotationSpeed = 30f;
}

[System.Serializable]
public class ZigzagConfig
{
    [Header("Patrón Zigzag")]
    public int zigzagLines = 4;
    public int bulletsPerLine = 10;
    public float zigzagAmplitude = 3f;
    public float zigzagFrequency = 2f;
    public float spreadAngle = 60f;
    public float speedVariation = 0.3f;
}

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject enemyPrefab;
    [Tooltip("Cantidad de este tipo de enemigo a spawnear")]
    public int cantidad = 1;
}

public class FrogCombat : Enemy
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
        PushAttack,
        AdvancedBulletHell
    }
    
    private enum AdvancedPattern
    {
        Spiral,
        Wave,
        RotatingStar,
        Zigzag
    }
    #endregion

    #region Boss Configuration
    [Header("Boss Phase")]
    [Tooltip("Porcentaje de vida restante para cambiar a fase 2 (ej: 0.4 = 40% de vida)")]
    [SerializeField] private float phaseTransitionPercentage = 0.4f;
    
    [Header("Combat Start")]
    [Tooltip("Posiciones donde aparecerán los jugadores al iniciar el combate")]
    [SerializeField] private Transform[] playerStartPositions;
    
    [Header("Attack Configuration")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float bulletSpeed = 8f;
    
    [Header("Bullet Hell Settings")]
    [SerializeField] private int bulletPatternCount = 8;
    [SerializeField] private int bulletHellBurstCount = 1; // Cantidad de ráfagas por ataque
    [SerializeField] private float delayBetweenBursts = 0.5f; // Delay entre ráfagas
    [SerializeField] private float bulletHellCooldown = 3f;
    [SerializeField] private float bulletHellBaseDirection = 0f; // 0 = hacia adelante, 90 = arriba, etc
    [SerializeField] private float bulletHellArcHeight = 2f; // Altura del arco de los proyectiles.
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Bullet Hell")]
    [SerializeField] private AttackPhaseConfig bulletHellPhaseConfig = new AttackPhaseConfig();
    
    [Header("Advanced Bullet Hell Settings")]
    [SerializeField] private int advancedWaveCount = 5;
    [SerializeField] private float advancedWaveDelay = 0.15f;
    [SerializeField] private float advancedBulletSpeed = 6f;
    [SerializeField] private float advancedCooldown = 5f;
    [Space(10)]
    [Tooltip("Configuración individual para cada patrón avanzado")]
    [SerializeField] private SpiralPatternConfig spiralConfig = new SpiralPatternConfig();
    [SerializeField] private WavePatternConfig waveConfig = new WavePatternConfig();
    [SerializeField] private RotatingStarConfig starConfig = new RotatingStarConfig();
    [SerializeField] private ZigzagConfig zigzagConfig = new ZigzagConfig();
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Advanced Bullet Hell")]
    [SerializeField] private AttackPhaseConfig advancedBulletHellPhaseConfig = new AttackPhaseConfig();
    
    [Header("Jump Attack Settings")]
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 1.5f;
    [SerializeField] private float jumpCooldown = 4f;
    [SerializeField] private float jumpImpactRadius = 6f;
    [SerializeField] private float jumpImpactDamage = 30f;
    [SerializeField] private float jumpImpactPushForce = 15f;
    [Space(10)]
    [Tooltip("Offset de rotación para corregir el forward del modelo (ej: 90, 180, -90)")]
    [SerializeField] private float modelRotationOffset = 0f;
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Jump Attack")]
    [SerializeField] private AttackPhaseConfig jumpAttackPhaseConfig;
    
    [Header("Push Attack Settings")]
    [SerializeField] private float pushDetectionRadius = 4f;
    [SerializeField] private float pushForce = 20f;
    [SerializeField] private float pushCooldown = 4.5f;
    [SerializeField] private float pushChargeDuration = 1f;
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Push Attack")]
    [SerializeField] private AttackPhaseConfig pushAttackPhaseConfig = new AttackPhaseConfig();
    
    [Header("Mortar Attack Settings")]
    [SerializeField] private int mortarProjectileCount = 6;
    [SerializeField] private float mortarCooldown = 4f;
    [SerializeField] private float mortarRandomRadius = 15f; // Radio del área aleatoria de caída
    [SerializeField] private Vector3 mortarAttackAreaCenter = Vector3.zero; // Centro del área de morteros (relativo al boss)
    [SerializeField] private float mortarFallDuration = 2f; // Tiempo que tarda en caer el mortero
    [SerializeField] private float mortarMaxHeight = 25f; // Altura máxima del mortero en el aire
    [SerializeField] private float mortarDamageRadius = 3f; // Radio del área de daño del mortero
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Mortar Attack")]
    [SerializeField] private AttackPhaseConfig mortarAttackPhaseConfig = new AttackPhaseConfig();
    
    [Header("Enemy Spawning")]
    [Tooltip("Configuración de los 3 tipos de enemigos a spawnear")]
    [SerializeField] private EnemySpawnConfig[] enemyTypes = new EnemySpawnConfig[3];
    [SerializeField] private Transform[] enemySpawnPoints; // Puntos de spawn de enemigos
    [SerializeField] private float spawnCooldown = 5f;
    [SerializeField] private float delayBetweenEnemySpawns = 0.5f; // Delay entre spawns de enemigos
    [Space(10)]
    [Tooltip("Configurar en qué fases está disponible Spawn Enemies")]
    [SerializeField] private AttackPhaseConfig spawnEnemiesPhaseConfig = new AttackPhaseConfig();
    
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
    private BossPhase currentPhase = BossPhase.Phase1;
    private bool isDefeated = false;
    private bool isJumping = false;
    private float lastJumpTime = 0f;
    private float lastPushTime = 0f;
    
    private float lastAttackTime = 0f;
    private float lastBulletHellTime = 0f;
    private float lastMortarTime = 0f;
    private float lastSpawnTime = 0f;
    private float lastAdvancedBulletHellTime = 0f;
    private AdvancedPattern lastUsedPattern = AdvancedPattern.Spiral;
    
    private float bossStartTime = 0f;
    private bool hasStartedCombat = false;
    private const float INITIAL_DELAY = 7f; // Delay antes de empezar a atacar
    
    private Animator animator;
    private Rigidbody bossRb; // Rigidbody específico del boss para evitar conflicto con la clase base
    #endregion

    #region Initialization
    protected override void Awake()
    {
        base.Awake(); // Llamar a Enemy.Awake() que llama a EntityStats.Awake()
        bossRb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        
        // Configurar Rigidbody para que el boss sea estático pero pueda moverse con scripts
        if (bossRb != null)
        {
            bossRb.isKinematic = true;
            bossRb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        // Inicializar jumpAttackPhaseConfig si es null o para asegurar valores serializados
        if (jumpAttackPhaseConfig == null)
        {
            jumpAttackPhaseConfig = new AttackPhaseConfig { enableInPhase1 = false, enableInPhase2 = false };
        }
    }
    
    private void Start()
    {
        InitializeBoss();
    }

    private void InitializeBoss()
    {
        // NO usar DontDestroyOnLoad para mantener las referencias de la escena correctas
        
        if (targetTransform == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
                targetTransform = player.transform;
        }
        
        // Verificar configuración de ataques
        Debug.Log($"[FrogBoss] Boss initialized with {curHP}/{maxHP} HP");
        Debug.Log($"[FrogBoss] Boss comenzará a atacar en {INITIAL_DELAY} segundos");
        Debug.Log($"[FrogBoss] Jump Attack Config - Phase1: {jumpAttackPhaseConfig.enableInPhase1}, Phase2: {jumpAttackPhaseConfig.enableInPhase2}");
        
        // Verificar spawn points
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            Debug.Log($"[FrogBoss] {enemySpawnPoints.Length} enemy spawn points configurados");
        }
        else
        {
            Debug.LogWarning("[FrogBoss] No hay enemy spawn points configurados!");
        }
        
        // Iniciar corrutina de inicio de combate
        StartCoroutine(InitializeCombat());
    }
    
    private IEnumerator InitializeCombat()
    {
        bossStartTime = Time.time;
        hasStartedCombat = false;
        
        // Teletransportar jugadores inmediatamente al inicio
        TeleportPlayersToStartPositions();
        
        // Esperar el delay inicial antes de comenzar a atacar
        Debug.Log($"[FrogBoss] Esperando {INITIAL_DELAY} segundos antes de comenzar el combate...");
        yield return new WaitForSeconds(INITIAL_DELAY);
        
        hasStartedCombat = true;
        Debug.Log("[FrogBoss] ¡El boss comienza a atacar!");
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Este método se ejecuta en el Editor cuando cambian valores en el Inspector
        // Fuerza la sincronización correcta de los valores serializados
    }
#endif
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
        float healthPercentage = (float)curHP / maxHP;
        
        if (currentPhase == BossPhase.Phase1 && healthPercentage <= phaseTransitionPercentage)
        {
            TransitionToPhase2();
        }
    }

    private void HandleMovement()
    {
        // El boss es estático, no se mueve horizontalmente
        // Durante los saltos, la posición se controla manualmente en el coroutine
        if (!isJumping && bossRb != null)
        {
            // Para un Rigidbody kinematic, usar MovePosition en lugar de linearVelocity
            bossRb.MovePosition(transform.position);
        }
    }

    private void HandleAttackPattern()
    {
        // Verificar si el combate ya ha comenzado
        if (!hasStartedCombat)
            return;
        
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
        
        // Bullet Hell
        if (IsAttackEnabledInCurrentPhase(bulletHellPhaseConfig) && Time.time - lastBulletHellTime > bulletHellCooldown)
            attacks.Add(AttackType.BulletHell);
        
        // Jump Attack - Verificación estricta con validación adicional
        if (jumpAttackPhaseConfig != null && IsAttackEnabledInCurrentPhase(jumpAttackPhaseConfig) && Time.time - lastJumpTime > jumpCooldown)
            attacks.Add(AttackType.JumpAttack);
        
        // Mortar Attack
        if (IsAttackEnabledInCurrentPhase(mortarAttackPhaseConfig) && Time.time - lastMortarTime > mortarCooldown)
            attacks.Add(AttackType.MortarAttack);
        
        // Push Attack solo cuando el jugador está cerca
        if (IsAttackEnabledInCurrentPhase(pushAttackPhaseConfig) && Time.time - lastPushTime > pushCooldown && distanceToPlayer <= pushDetectionRadius * 1.5f)
            attacks.Add(AttackType.PushAttack);
        
        // Spawn Enemies
        if (IsAttackEnabledInCurrentPhase(spawnEnemiesPhaseConfig) && Time.time - lastSpawnTime > spawnCooldown)
            attacks.Add(AttackType.SpawnEnemies);
        
        // Advanced Bullet Hell
        if (IsAttackEnabledInCurrentPhase(advancedBulletHellPhaseConfig) && Time.time - lastAdvancedBulletHellTime > advancedCooldown)
            attacks.Add(AttackType.AdvancedBulletHell);
        
        return attacks;
    }
    
    /// <summary>
    /// Verifica si un ataque está habilitado en la fase actual del boss
    /// </summary>
    private bool IsAttackEnabledInCurrentPhase(AttackPhaseConfig config)
    {
        if (config == null)
            return false;
            
        return currentPhase == BossPhase.Phase1 ? config.enableInPhase1 : config.enableInPhase2;
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
                // Verificación adicional de seguridad antes de ejecutar
                if (IsAttackEnabledInCurrentPhase(jumpAttackPhaseConfig))
                {
                    StartCoroutine(JumpAttack());
                }
                else
                {
                    Debug.LogWarning("[FrogBoss] Jump Attack fue seleccionado pero está deshabilitado en la configuración!");
                }
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
            case AttackType.AdvancedBulletHell:
                StartCoroutine(AdvancedBulletHellAttack());
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
        
        yield return new WaitForSeconds(0.3f);
        
        // Generar múltiples ráfagas según la configuración
        for (int burstIndex = 0; burstIndex < bulletHellBurstCount; burstIndex++)
        {
            // Patrón de arco 
            float arcWidth = 120f; // Ancho del arco en grados
            float startAngle = bulletHellBaseDirection - arcWidth * 0.5f; // Comienza a la izquierda del arco
            
            for (int i = 0; i < bulletPatternCount; i++)
            {
                float angle = startAngle + (arcWidth / (bulletPatternCount - 1)) * i;
                Vector2 direction = GetDirectionFromAngle(angle);
                SpawnProjectile(direction, bulletSpeed);
            }
            
            // Esperar antes de la siguiente ráfaga
            if (burstIndex < bulletHellBurstCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenBursts);
            }
        }
        
        yield return new WaitForSeconds(bulletHellCooldown * 0.5f);
        
        // Segunda fase en Phase 2 - arco expandido con múltiples ráfagas
        
    }

    private IEnumerator JumpAttack()
    {
        lastJumpTime = Time.time;
        
        // Determinar el jugador objetivo antes de empezar
        Transform targetPlayer = null;
        if (targetTransform != null)
        {
            targetPlayer = targetTransform;
        }
        
        // ROTAR HACIA EL JUGADOR ANTES DE SALTAR
        if (targetPlayer != null)
        {
            Vector3 directionToPlayer = (targetPlayer.position - transform.position);
            directionToPlayer.y = 0;
            
            if (directionToPlayer.magnitude > 0.1f)
            {
                float angleToPlayer = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;
                float finalAngle = angleToPlayer + modelRotationOffset;
                
                Quaternion targetRotation = Quaternion.Euler(0, finalAngle, 0);
                float rotationDuration = 0.5f;
                float elapsedRotationTime = 0f;
                Quaternion startRotation = transform.rotation;
                
                while (elapsedRotationTime < rotationDuration)
                {
                    transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedRotationTime / rotationDuration);
                    elapsedRotationTime += Time.deltaTime;
                    yield return null;
                }
                
                transform.rotation = targetRotation;
                Debug.Log($"[FrogBoss] Boss rotado hacia el jugador ANTES de saltar. Ángulo: {finalAngle}°");
            }
        }
        
        // Pequeña pausa después de rotar
        yield return new WaitForSeconds(0.2f);
        
        PlayAttackEffect();
        PlayAttackSFX();
        
        isJumping = true;
        
        // Desactivar restricciones temporalmente para el salto
        if (bossRb != null)
        {
            bossRb.isKinematic = false;
            bossRb.constraints = RigidbodyConstraints.None;
        }
        bossRb.linearVelocity = Vector3.zero;
        
        Vector3 bossStartPosition = transform.position;
        
        // Calcular posición de destino del jugador
        Vector3 jumpTargetPosition = bossStartPosition;
        
        if (targetPlayer != null)
        {
            jumpTargetPosition = targetPlayer.position;
        }
        
        // Ajustar la altura del destino al suelo
        jumpTargetPosition.y = bossStartPosition.y;
        
        // Crear el área de aterrizaje visual antes de saltar
        GameObject landingAreaMarker = CreateJumpLandingArea(jumpTargetPosition);
        
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
        bool hasPrePushed = false; // Flag para evitar empujes múltiples
        List<GameObject> prePushedPlayers = new List<GameObject>(); // Lista de jugadores ya empujados
        
        while (elapsedTime < jumpDuration * 0.5f)
        {
            float t = elapsedTime / (jumpDuration * 0.5f);
            transform.position = Vector3.Lerp(peakPosition, jumpTargetPosition, t);
            
            // Cuando está cerca del suelo (80% de caída), empujar a los jugadores hacia arriba anticipadamente UNA SOLA VEZ
            if (t >= 0.8f && !hasPrePushed)
            {
                hasPrePushed = true;
                Collider[] preHitColliders = Physics.OverlapSphere(jumpTargetPosition, jumpImpactRadius);
                foreach (Collider collider in preHitColliders)
                {
                    if (collider.gameObject != gameObject)
                    {
                        if (collider.CompareTag("Player") || collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                        {
                            Rigidbody playerRb = collider.GetComponent<Rigidbody>();
                            if (playerRb != null)
                            {
                                // Empujar fuertemente hacia arriba antes del impacto
                                Vector3 horizontalDirection = (collider.transform.position - jumpTargetPosition);
                                horizontalDirection.y = 0;
                                
                                if (horizontalDirection.magnitude < 0.5f)
                                {
                                    // Si está en el centro, empujar en dirección aleatoria
                                    float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                                    horizontalDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * 2f;
                                }
                                else
                                {
                                    horizontalDirection = horizontalDirection.normalized * 2f;
                                }
                                
                                // Empuje solo horizontal, sin componente vertical
                                Vector3 prePushForce = horizontalDirection;
                                playerRb.AddForce(prePushForce * jumpImpactPushForce * 0.7f, ForceMode.Impulse);
                                
                                // Registrar que este jugador fue pre-empujado
                                prePushedPlayers.Add(collider.gameObject);
                            }
                        }
                    }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Asegurar posición final ligeramente por encima del suelo
        jumpTargetPosition.y += 0.1f;
        transform.position = jumpTargetPosition;
        
        // Destruir el marcador de área
        if (landingAreaMarker != null)
        {
            Destroy(landingAreaMarker);
        }
        
        // Aplicar daño y empuje en área al aterrizar
        Collider[] hitColliders = Physics.OverlapSphere(jumpTargetPosition, jumpImpactRadius);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject != gameObject)
            {
                if (collider.CompareTag("Player") || collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    // Aplicar daño
                    EntityStats entityStats = collider.GetComponent<EntityStats>();
                    if (entityStats != null)
                    {
                        entityStats.TakeDamage((int)jumpImpactDamage);
                    }
                    
                    // Aplicar empuje SOLO si el jugador NO fue pre-empujado
                    if (!prePushedPlayers.Contains(collider.gameObject))
                    {
                        Rigidbody playerRb = collider.GetComponent<Rigidbody>();
                        if (playerRb != null)
                        {
                            // Calcular dirección horizontal
                            Vector3 horizontalDirection = (collider.transform.position - transform.position);
                            horizontalDirection.y = 0; // Ignorar diferencia vertical
                            
                            float horizontalDistance = horizontalDirection.magnitude;
                            
                            // Si el jugador está muy cerca del centro, empujar en dirección aleatoria
                            if (horizontalDistance < 0.5f)
                            {
                                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                                horizontalDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
                            }
                            else
                            {
                                horizontalDirection = horizontalDirection.normalized;
                            }
                            
                            // Empuje solo horizontal, sin componente vertical
                            Vector3 pushDirection = horizontalDirection;
                            
                            playerRb.AddForce(pushDirection * jumpImpactPushForce, ForceMode.Impulse);
                        }
                    }
                }
            }
        }
        
        // Impacto al aterrizar - efectos
        PlayAttackEffect();
        PlayAttackSFX();
        
        // El boss se queda en esta nueva posición
        bossRb.linearVelocity = Vector3.zero;
        isJumping = false;
        
        // Restaurar restricciones de física para mantener la nueva posición
        if (bossRb != null)
        {
            bossRb.isKinematic = true;
            bossRb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        Debug.Log($"[FrogBoss] Boss aterrizó en nueva posición: {transform.position}. El combate continúa desde aquí!");
        
        // Esperar un momento antes de girar hacia el jugador
        yield return new WaitForSeconds(0.3f);
        
        // Rotar hacia el jugador objetivo DESPUÉS de aterrizar y estabilizarse
        Transform playerToFace = targetPlayer;
        
        // Si no hay targetPlayer guardado, buscar al jugador más cercano
        if (playerToFace == null)
        {
            Player nearestPlayer = FindFirstObjectByType<Player>();
            if (nearestPlayer != null)
            {
                playerToFace = nearestPlayer.transform;
            }
        }
        
        if (playerToFace != null)
        {
            Vector3 directionToPlayer = (playerToFace.position - transform.position);
            directionToPlayer.y = 0; // Mantener rotación solo en el plano horizontal
            
            Debug.Log($"[FrogBoss] Boss pos: {transform.position}, Jugador pos: {playerToFace.position}, Dirección: {directionToPlayer}");
            
            if (directionToPlayer.magnitude > 0.1f)
            {
                // Calcular el ángulo hacia el jugador
                float angleToPlayer = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;
                
                // Aplicar el offset de rotación del modelo
                float finalAngle = angleToPlayer + modelRotationOffset;
                
                // Rotación gradual hacia el jugador
                Quaternion targetRotation = Quaternion.Euler(0, finalAngle, 0);
                float rotationDuration = 0.7f; // Duración de la rotación en segundos
                float elapsedRotationTime = 0f;
                Quaternion startRotation = transform.rotation;
                
                while (elapsedRotationTime < rotationDuration)
                {
                    transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedRotationTime / rotationDuration);
                    elapsedRotationTime += Time.deltaTime;
                    yield return null;
                }
                
                // Asegurar rotación final exacta
                transform.rotation = targetRotation;
                
                Debug.Log($"[FrogBoss] Boss rotado hacia el jugador. Ángulo calculado: {angleToPlayer}°, Ángulo final con offset: {finalAngle}°");
            }
            else
            {
                Debug.LogWarning("[FrogBoss] El jugador está muy cerca del boss para calcular dirección");
            }
        }
        else
        {
            Debug.LogWarning("[FrogBoss] No se encontró jugador para rotar hacia él");
        }
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
                    }
                }
            }
        }
    }
    #endregion

    #region Projectile Management
    private void SpawnProjectile(Vector2 direction, float speed)
    {
        if (projectilePrefab == null) return;
        
        Vector3 spawnPos = transform.position + bulletHellSpawnOffset;
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        // Desactivar Rigidbody del proyectil si existe
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
            Vector3 newPosition = horizontalPos + Vector3.up * arcY;
            projectile.transform.position = newPosition;
            
            // Calcular dirección de movimiento considerando el arco
            if (dir3D != Vector3.zero)
            {
                // Calcular la derivada de la altura para obtener la pendiente del arco
                float arcDerivative = Mathf.Cos(t * Mathf.PI) * arcHeight;
                Vector3 movementDirection = dir3D + Vector3.up * arcDerivative / travelDistance;
                movementDirection.Normalize();
                
                // Rotar hacia la dirección de movimiento actual (incluyendo componente vertical del arco)
                projectile.transform.rotation = Quaternion.LookRotation(movementDirection);
            }
            
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
    
    private GameObject CreateJumpLandingArea(Vector3 landingPosition)
    {
        // Crear un GameObject para el área de aterrizaje
        GameObject landingArea = new GameObject("JumpLandingArea");
        landingArea.transform.position = landingPosition;
        
        // Visualizar el área con LineRenderer
        LineRenderer lineRenderer = landingArea.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0f, 0f, 0.9f); // Rojo para el salto
        lineRenderer.endColor = new Color(1f, 0f, 0f, 0.9f);
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.useWorldSpace = false;
        
        // Dibujar círculo en el suelo
        int segments = 40;
        Vector3[] positions = new Vector3[segments + 1];
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * jumpImpactRadius;
            float z = Mathf.Sin(angle) * jumpImpactRadius;
            positions[i] = new Vector3(x, 0.1f, z);
        }
        
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        
        Debug.Log($"[FrogBoss] Área de aterrizaje marcada en {landingPosition}");
        
        return landingArea;
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
    }
    #endregion

    #region Phase Transition
    private void TransitionToPhase2()
    {
        currentPhase = BossPhase.Phase2;
        bulletSpeed *= 1.1f;
        attackCooldown *= 0.8f;
        
        PlayAttackEffect();
        Debug.Log("[FrogBoss] ¡TRANSICIÓN A FASE 2! El boss está más fuerte!");
    }
    
    private void TeleportPlayersToStartPositions()
    {
        if (playerStartPositions == null || playerStartPositions.Length == 0)
        {
            Debug.LogWarning("[FrogBoss] No hay posiciones de inicio configuradas para los jugadores");
            return;
        }
        
        // Buscar todos los jugadores en la escena
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        
        if (players.Length == 0)
        {
            Debug.LogWarning("[FrogBoss] No se encontraron jugadores para teletransportar");
            return;
        }
        
        Debug.Log($"[FrogBoss] Teletransportando {players.Length} jugador(es) a posiciones de inicio");
        
        for (int i = 0; i < players.Length; i++)
        {
            // Usar posiciones cíclicamente si hay más jugadores que posiciones
            int positionIndex = i % playerStartPositions.Length;
            Transform startPos = playerStartPositions[positionIndex];
            
            if (startPos != null)
            {
                // Teletransportar jugador
                players[i].transform.position = startPos.position;
                players[i].transform.rotation = startPos.rotation;
                
                // Resetear velocidad del Rigidbody si tiene
                Rigidbody playerRb = players[i].GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }
                
                Debug.Log($"[FrogBoss] Jugador {i + 1} teletransportado a {startPos.position}");
            }
        }
    }
    #endregion

    #region Damage & Health
    public override void OnEntityDeath()
    {
        if (isDefeated)
            return; // Prevenir múltiples llamadas
        
        isDefeated = true;
        
        if (bossRb != null)
        {
            bossRb.linearVelocity = Vector3.zero;
            bossRb.isKinematic = true;
        }
        
        Debug.Log("[FrogBoss] ¡EL BOSS HA SIDO DERROTADO!");
        
        // Efecto de derrota
        if (attackEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
        }
        
        // No llamar a base.OnEntityDeath() para no dropear items como enemigo normal
        // El boss debe tener su propio sistema de recompensas aquí si lo deseas
        
        Destroy(gameObject, 3f); // Destruir después de 3 segundos para mostrar el efecto
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
    
    private AdvancedPattern GetRandomPattern()
    {
        // Crear lista de todos los patrones disponibles
        List<AdvancedPattern> availablePatterns = new List<AdvancedPattern>
        {
            AdvancedPattern.Spiral,
            AdvancedPattern.Wave,
            AdvancedPattern.RotatingStar,
            AdvancedPattern.Zigzag
        };
        
        // Remover el último patrón usado para evitar repetición
        availablePatterns.Remove(lastUsedPattern);
        
        // Seleccionar aleatoriamente de los restantes
        int randomIndex = Random.Range(0, availablePatterns.Count);
        return availablePatterns[randomIndex];
    }

    private IEnumerator AdvancedBulletHellAttack()
    {
        lastAdvancedBulletHellTime = Time.time;
        animator?.SetTrigger("BulletHellAttack");
        yield return new WaitForSeconds(0.5f);
        
        PlayAttackEffect();
        PlayAttackSFX();
        
        // Seleccionar un patrón aleatorio diferente al último usado
        AdvancedPattern selectedPattern = GetRandomPattern();
        
        Debug.Log($"[FrogBoss] Advanced Bullet Hell - Pattern: {selectedPattern}");
        
        yield return new WaitForSeconds(0.3f);
        
        switch (selectedPattern)
        {
            case AdvancedPattern.Spiral:
                yield return StartCoroutine(SpiralPattern());
                break;
            case AdvancedPattern.Wave:
                yield return StartCoroutine(WavePattern());
                break;
            case AdvancedPattern.RotatingStar:
                yield return StartCoroutine(RotatingStarPattern());
                break;
            case AdvancedPattern.Zigzag:
                yield return StartCoroutine(ZigzagPattern());
                break;
        }
        
        lastUsedPattern = selectedPattern;
    }
    
    private IEnumerator SpiralPattern()
    {
        Debug.Log("[FrogBoss] Ejecutando patrón DNA Hélice Doble");
        float currentRotation = 0f;
        
        for (int wave = 0; wave < advancedWaveCount; wave++)
        {
            // Crear dos hélices entrelazadas
            for (int helix = 0; helix < 2; helix++)
            {
                float helixDirection = helix == 0 ? 1f : -1f; // Una horaria, otra antihoraria
                float helixOffset = helix * 180f; // Offset inicial de 180 grados
                
                for (int i = 0; i < spiralConfig.helixSegments; i++)
                {
                    float t = (float)i / spiralConfig.helixSegments;
                    
                    // Ángulo que sigue una trayectoria helicoidal
                    float angle = helixOffset + (currentRotation + t * 360f) * helixDirection;
                    
                    // Disparar múltiples proyectiles por segmento para densidad
                    for (int j = 0; j < spiralConfig.projectilesPerSegment; j++)
                    {
                        float spreadAngle = angle + (j - spiralConfig.projectilesPerSegment / 2f) * spiralConfig.spreadAngle;
                        Vector2 direction = GetDirectionFromAngle(spreadAngle);
                        
                        // Velocidad ondulatoria basada en la posición en la hélice
                        float speedWave = Mathf.Sin(t * Mathf.PI * 2f) * 0.2f + 1f;
                        SpawnProjectile(direction, advancedBulletSpeed * speedWave);
                    }
                }
            }
            
            // Puentes conectores entre las dos hélices (cada 2 olas)
            if (wave % 2 == 0)
            {
                for (int i = 0; i < spiralConfig.bridgeCount; i++)
                {
                    float bridgeAngle = (360f / spiralConfig.bridgeCount) * i + currentRotation * 0.5f;
                    
                    // Disparar línea de proyectiles como "puente"
                    for (int j = 0; j < spiralConfig.bridgeProjectiles; j++)
                    {
                        float angle = bridgeAngle + (j - spiralConfig.bridgeProjectiles / 2f) * spiralConfig.bridgeSpread;
                        Vector2 direction = GetDirectionFromAngle(angle);
                        SpawnProjectile(direction, advancedBulletSpeed * 0.75f);
                    }
                }
            }
            
            currentRotation += spiralConfig.rotationSpeed * 0.8f;
            yield return new WaitForSeconds(advancedWaveDelay);
        }
    }
    
    private IEnumerator WavePattern()
    {
        Debug.Log("[FrogBoss] Ejecutando patrón Pétalos de Flor");
        
        for (int wave = 0; wave < advancedWaveCount; wave++)
        {
            float waveRotation = wave * (360f / advancedWaveCount);
            
            // Crear pétalos usando funciones trigonométricas
            for (int petal = 0; petal < waveConfig.petalCount; petal++)
            {
                float petalAngle = (360f / waveConfig.petalCount) * petal + waveRotation;
                
                // Cada pétalo tiene múltiples proyectiles formando una curva
                for (int i = 0; i < waveConfig.bulletsPerPetal; i++)
                {
                    float t = (float)i / (waveConfig.bulletsPerPetal - 1); // Normalizado 0-1
                    
                    // Función de rosa polar para crear pétalos: r = sin(k * θ)
                    float curveAngle = t * 90f; // Ángulo dentro del pétalo (0-90 grados)
                    float radius = Mathf.Sin(curveAngle * Mathf.Deg2Rad * waveConfig.curveFactor); // Factor de curvatura
                    
                    // Calcular desplazamiento lateral basado en la curva
                    float lateralOffset = radius * waveConfig.petalWidth; // Ancho del pétalo
                    float finalAngle = petalAngle + lateralOffset - waveConfig.petalWidth / 2f; // Centrar el pétalo
                    
                    Vector2 direction = GetDirectionFromAngle(finalAngle);
                    
                    // Velocidad variable: más lento al inicio y final del pétalo, más rápido en el medio
                    float speedMultiplier = Mathf.Sin(t * Mathf.PI) * 0.5f + 0.75f;
                    SpawnProjectile(direction, advancedBulletSpeed * speedMultiplier);
                }
            }
            
            // Anillo central para dar más densidad
            if (wave % 2 == 1)
            {
                for (int i = 0; i < waveConfig.centerRingBullets; i++)
                {
                    float angle = (360f / waveConfig.centerRingBullets) * i + waveRotation * 0.5f;
                    Vector2 direction = GetDirectionFromAngle(angle);
                    SpawnProjectile(direction, advancedBulletSpeed * waveConfig.centerRingSpeed);
                }
            }
            
            yield return new WaitForSeconds(advancedWaveDelay);
        }
    }
    
    private IEnumerator RotatingStarPattern()
    {
        Debug.Log("[FrogBoss] Ejecutando patrón Estrella Rotante");
        float currentRotation = 0f;
        
        for (int wave = 0; wave < advancedWaveCount; wave++)
        {
            for (int arm = 0; arm < starConfig.starArms; arm++)
            {
                float armAngle = (360f / starConfig.starArms) * arm + currentRotation;
                
                for (int i = 0; i < starConfig.bulletsPerArm; i++)
                {
                    float spreadAngle = armAngle + (i - starConfig.bulletsPerArm / 2) * starConfig.armSpread;
                    Vector2 direction = GetDirectionFromAngle(spreadAngle);
                    SpawnProjectile(direction, advancedBulletSpeed + i * starConfig.speedIncrement);
                }
            }
            
            currentRotation += starConfig.rotationSpeed;
            yield return new WaitForSeconds(advancedWaveDelay);
        }
    }
    
    private IEnumerator ZigzagPattern()
    {
        Debug.Log("[FrogBoss] Ejecutando patrón Zigzag");
        float currentRotation = 0f;
        
        for (int wave = 0; wave < advancedWaveCount; wave++)
        {
            // Calcular ángulo base hacia el jugador
            float baseAngle = 0f;
            if (targetTransform != null)
            {
                Vector3 directionToPlayer = targetTransform.position - transform.position;
                baseAngle = Mathf.Atan2(directionToPlayer.z, directionToPlayer.x) * Mathf.Rad2Deg;
            }
            
            // Crear múltiples líneas zigzag
            for (int line = 0; line < zigzagConfig.zigzagLines; line++)
            {
                // Distribuir las líneas en un cono
                float lineAngleOffset = (line - (zigzagConfig.zigzagLines - 1) / 2f) * (zigzagConfig.spreadAngle / zigzagConfig.zigzagLines);
                float lineAngle = baseAngle + lineAngleOffset + currentRotation;
                
                // Crear zigzag a lo largo de esta línea
                for (int i = 0; i < zigzagConfig.bulletsPerLine; i++)
                {
                    float t = (float)i / zigzagConfig.bulletsPerLine;
                    
                    // Calcular desplazamiento zigzag (onda sinusoidal)
                    float zigzagOffset = Mathf.Sin(t * Mathf.PI * zigzagConfig.zigzagFrequency) * zigzagConfig.zigzagAmplitude;
                    
                    // Aplicar el zigzag perpendicular a la dirección de la línea
                    float finalAngle = lineAngle + zigzagOffset;
                    Vector2 direction = GetDirectionFromAngle(finalAngle);
                    
                    // Variar velocidad ligeramente para crear efecto de onda
                    float speedVariation = 1f + Mathf.Sin(t * Mathf.PI * 2f) * zigzagConfig.speedVariation;
                    float speed = advancedBulletSpeed * speedVariation;
                    
                    SpawnProjectile(direction, speed);
                }
            }
            
            // Rotar ligeramente el patrón completo cada ola
            currentRotation += 15f;
            yield return new WaitForSeconds(advancedWaveDelay);
        }
    }
    
    private IEnumerator SpawnEnemies()
    {
        lastSpawnTime = Time.time;
        PlayAttackEffect();
        PlayAttackSFX();
        
        // Verificar si hay puntos de spawn definidos
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[FrogBoss] No enemy spawn points assigned!");
            yield break;
        }
        
        // Verificar si hay tipos de enemigos configurados
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogWarning("[FrogBoss] No enemy types configured!");
            yield break;
        }
        
        // Contar el total de enemigos a spawnear
        int totalEnemies = 0;
        foreach (var enemyType in enemyTypes)
        {
            if (enemyType != null && enemyType.enemyPrefab != null)
            {
                totalEnemies += enemyType.cantidad;
            }
        }
        
        Debug.Log($"[FrogBoss] Spawning {totalEnemies} enemies total!");
        
        int spawnedCount = 0;
        
        // Spawnear cada tipo de enemigo según su configuración
        for (int typeIndex = 0; typeIndex < enemyTypes.Length; typeIndex++)
        {
            EnemySpawnConfig enemyConfig = enemyTypes[typeIndex];
            
            // Validar que el tipo está configurado correctamente
            if (enemyConfig == null || enemyConfig.enemyPrefab == null || enemyConfig.cantidad <= 0)
            {
                continue;
            }
            
            // Spawnear la cantidad especificada de este tipo
            for (int i = 0; i < enemyConfig.cantidad; i++)
            {
                // Seleccionar un punto de spawn aleatorio
                Transform randomSpawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                Vector3 spawnPosition = randomSpawnPoint.position;
                
                GameObject enemy = Instantiate(enemyConfig.enemyPrefab, spawnPosition, Quaternion.identity);
                spawnedCount++;
                Debug.Log($"[FrogBoss] Enemy {spawnedCount}/{totalEnemies} (Type {typeIndex + 1}) spawned at {spawnPosition}");
                
                // Esperar antes de spawnear el siguiente enemigo
                if (spawnedCount < totalEnemies)
                {
                    yield return new WaitForSeconds(delayBetweenEnemySpawns);
                }
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
