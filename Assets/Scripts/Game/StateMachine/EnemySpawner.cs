using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Enemies;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject normalEnemyPrefab;
    public GameObject eliteEnemyPrefab;
    
    [Header("Spawn Points")]  
    public Transform[] spawnPoints;
    
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
    [SerializeField] private bool isSpawning = false;
    
    private DifficultySettings currentSettings;
    
    void Awake()
    {
        ApplyDifficulty(0);
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
        if (!isSpawning) return;
        
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            ScheduleNextSpawn();
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
        // Filtrar solo los spawnPoints válidos (no destruidos)
        List<Transform> validSpawnPoints = new List<Transform>();
        foreach (var sp in spawnPoints)
        {
            if (sp != null)
                validSpawnPoints.Add(sp);
        }
        if (validSpawnPoints.Count == 0) return;

        // Elegir punto de spawn aleatorio
        Transform spawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];

        // Decidir si spawnar elite o normal
        bool spawnElite = Random.Range(0f, 100f) < currentSettings.elitePercentage;
        GameObject prefabToSpawn = spawnElite ? eliteEnemyPrefab : normalEnemyPrefab;

        if (prefabToSpawn == null) return;

        // Spawnear enemigo
        GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

        // Aplicar multiplicador de vida
        Enemy enemyHealth = enemy.GetComponent<Enemy>();
        if (enemyHealth != null)
        {
            enemyHealth.MaxHP = Mathf.RoundToInt(enemyHealth.MaxHP * currentSettings.healthMultiplier);
        }
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
    {
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
