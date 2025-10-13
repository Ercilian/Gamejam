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
    
    void Start()
    {
        // Suscribirse al cambio de dificultad
        DifficultyManager.OnDifficultyChanged += OnDifficultyChanged;
        
        // Configurar dificultad inicial
        ApplyDifficulty(0);
        
        Debug.Log("[EnemySpawner] Initialized");
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
    }
    
    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;
        
        // Elegir punto de spawn aleatorio
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // Decidir si spawnar elite o normal
        bool spawnElite = Random.Range(0f, 100f) < currentSettings.elitePercentage;
        GameObject prefabToSpawn = spawnElite ? eliteEnemyPrefab : normalEnemyPrefab;
        
        if (prefabToSpawn == null) return;
        
        // Spawnear enemigo
        GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        // Aplicar multiplicador de vida
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = Mathf.RoundToInt(enemyHealth.maxHealth * currentSettings.healthMultiplier);
        }
        
        string enemyType = spawnElite ? "Elite" : "Normal";
    }
    
    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + currentSettings.spawnInterval;
    }
    
    // ===== PUBLIC METHODS =====
    
    public void StartSpawning()
    {
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
