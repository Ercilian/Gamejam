using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Game.Enemies;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Interval (seconds)")]
    public float spawnInterval = 5f;

    [Header("Max Enemies Per Point (0 = infinito)")]
    public int maxEnemies = 0;

    // Control de spawn por punto
    private Dictionary<EnemySpawnPoint, float> nextSpawnTimeByPoint = new Dictionary<EnemySpawnPoint, float>();
    private Dictionary<EnemySpawnPoint, int> spawnedCountByPoint = new Dictionary<EnemySpawnPoint, int>();
    [Header("Enemy Prefabs")]
    public GameObject RatPrefab;
    public GameObject RatElitePrefab;
    public GameObject ChickenPrefab;
    public GameObject ChickenElitePrefab;
    public GameObject TurtlePrefab;
    public GameObject TurtleElitePrefab;

    [Header("Probabilidad de spawn por tipo (%)")]
    [Range(0f, 100f)] public float ratSpawnChance = 33f;
    [Range(0f, 100f)] public float ratEliteSpawnChance = 0f;
    [Range(0f, 100f)] public float chickenSpawnChance = 33f;
    [Range(0f, 100f)] public float chickenEliteSpawnChance = 0f;
    [Range(0f, 100f)] public float turtleSpawnChance = 34f;
    [Range(0f, 100f)] public float turtleEliteSpawnChance = 0f;
    
    [Header("Spawn Points (auto-detect)")]
    private EnemySpawnPoint[] spawnPoints;

    [Header("Car/Truck Reference")]
    public Transform carTransform;
    
    [System.Serializable]
    public class DifficultySettings
    {
        public float spawnInterval = 3f;
        
        public float healthMultiplier = 1f;
        
        [Range(0f, 100f)]
        public float elitePercentage = 20f;
    }
    
    [SerializeField]
    private DifficultySettings[] difficultyConfigs = {
        // Easy
        new DifficultySettings { spawnInterval = 4f, healthMultiplier = 1f, elitePercentage = 10f },
        // Medium  
        new DifficultySettings { spawnInterval = 3f, healthMultiplier = 1.3f, elitePercentage = 20f },
        // Hard
        new DifficultySettings { spawnInterval = 2f, healthMultiplier = 1.6f, elitePercentage = 35f },
        // Extreme
        new DifficultySettings { spawnInterval = 1.5f, healthMultiplier = 2f, elitePercentage = 50f }
    };
    
    [Header("Runtime Info")]
    [SerializeField] private int currentDifficulty = 0;
    [SerializeField] private float nextSpawnTime = 0f;
    [SerializeField] public bool isSpawning = false;
    
    private DifficultySettings currentSettings;
    



    //====================================== UNITY METHODS ======================================



    
    void Awake()
    {
        ApplyDifficulty(0);
        // No buscar aquí, se buscarán dinámicamente en Update
    }
    
    void Start()
    {
        // Suscribirse al cambio de dificultad
        DifficultyManager.OnDifficultyChanged += OnDifficultyChanged;
        
    }
    
    void OnDestroy()
    {
        DifficultyManager.OnDifficultyChanged -= OnDifficultyChanged;
    }
    
    void Update()
    {
        if (!isSpawning || carTransform == null) return;

        // Buscar dinámicamente todos los EnemySpawnPoint activos en la escena
        spawnPoints = FindObjectsOfType<EnemySpawnPoint>();
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;

            // Inicializar si es necesario
            if (!nextSpawnTimeByPoint.ContainsKey(sp)) nextSpawnTimeByPoint[sp] = 0f;
            if (!spawnedCountByPoint.ContainsKey(sp)) spawnedCountByPoint[sp] = 0;

            // Controlar máximo de enemigos
            if (maxEnemies > 0 && spawnedCountByPoint[sp] >= maxEnemies) continue;
            // Controlar intervalo
            if (Time.time < nextSpawnTimeByPoint[sp]) continue;

            sp.TryActivate(carTransform, (point) => {
                nextSpawnTimeByPoint[sp] = Time.time + spawnInterval;
                spawnedCountByPoint[sp]++;
                OnSpawnPointActivated(point);
            });
        }
    }
    
    void OnDifficultyChanged(DifficultyManager.DifficultyLevel newDifficulty, int difficultyIndex)
    {
        ApplyDifficulty(difficultyIndex);
        Debug.Log($"[EnemySpawner] Difficulty changed to: {newDifficulty.name}");
    }
    
    void ApplyDifficulty(int difficultyIndex)
    {
        if (difficultyIndex >= 0 && difficultyIndex < difficultyConfigs.Length)
        {
            currentDifficulty = difficultyIndex;
            currentSettings = difficultyConfigs[difficultyIndex];
            
            Debug.Log($"[EnemySpawner] Applied settings - Interval: {currentSettings.spawnInterval}s, " +
                     $"Health: x{currentSettings.healthMultiplier}, Elite: {currentSettings.elitePercentage}%");
        }
        else
        {
            // ===== FALLBACK PARA ÍNDICES INVÁLIDOS =====
            Debug.LogError($"[EnemySpawner] Invalid difficulty index: {difficultyIndex}, using index 0");
            currentDifficulty = 0;
            currentSettings = difficultyConfigs[0];
        }
    }
    
    void SpawnEnemy()
    {
        // No se usa más, el spawn ahora es por punto activado
        // (mantener método para compatibilidad, pero vacío)
        return;
    }

    // Nuevo método: llamado cuando un punto se activa
    void OnSpawnPointActivated(EnemySpawnPoint sp)
    {
        // Selección de prefab según probabilidades
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;
        GameObject prefabToSpawn = null;

        cumulative += ratSpawnChance;
        if (roll < cumulative && RatPrefab != null)
            prefabToSpawn = RatPrefab;
        else {
            cumulative += ratEliteSpawnChance;
            if (roll < cumulative && RatElitePrefab != null)
                prefabToSpawn = RatElitePrefab;
            else {
                cumulative += chickenSpawnChance;
                if (roll < cumulative && ChickenPrefab != null)
                    prefabToSpawn = ChickenPrefab;
                else {
                    cumulative += chickenEliteSpawnChance;
                    if (roll < cumulative && ChickenElitePrefab != null)
                        prefabToSpawn = ChickenElitePrefab;
                    else {
                        cumulative += turtleSpawnChance;
                        if (roll < cumulative && TurtlePrefab != null)
                            prefabToSpawn = TurtlePrefab;
                        else {
                            cumulative += turtleEliteSpawnChance;
                            if (roll < cumulative && TurtleElitePrefab != null)
                                prefabToSpawn = TurtleElitePrefab;
                        }
                    }
                }
            }
        }

        if (prefabToSpawn == null) return;
        GameObject enemy = Instantiate(prefabToSpawn, sp.transform.position, sp.transform.rotation);
        // Aquí puedes aplicar la dificultad al enemigo si lo deseas
        // Ejemplo:
        // Enemy enemyHealth = enemy.GetComponent<Enemy>();
        // if (enemyHealth != null)
        //     enemyHealth.MaxHP = Mathf.RoundToInt(enemyHealth.MaxHP * currentSettings.healthMultiplier);
    }
    
    void ScheduleNextSpawn()
    {
        // ===== VALIDAR QUE CURRENTSSETTINGS NO SEA NULL =====
        if (currentSettings == null)
        {
            Debug.LogWarning("[EnemySpawner] CurrentSettings is null, applying default difficulty");
            ApplyDifficulty(0); // Aplicar configuración por defecto
        }
        
        nextSpawnTime = Time.time + currentSettings.spawnInterval;
    }
    
    // ===== PUBLIC METHODS =====
    
    public void StartSpawning()
    {    // Resetear contadores de spawn
        nextSpawnTimeByPoint.Clear();
        spawnedCountByPoint.Clear();
    
        // ===== VALIDAR CONFIGURACIÓN ANTES DE EMPEZAR =====
        if (currentSettings == null)
        {
            Debug.LogWarning("[EnemySpawner] Starting spawning but no settings applied, using default");
            ApplyDifficulty(0);
        }
        
        isSpawning = true;
        ScheduleNextSpawn();
        Debug.Log("[EnemySpawner] Spawning started!");
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("[EnemySpawner] Spawning stopped!");
    }
}
