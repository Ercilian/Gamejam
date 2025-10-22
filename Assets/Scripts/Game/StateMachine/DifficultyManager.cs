using UnityEngine;
using System;

public class DifficultyManager : MonoBehaviour
{
    [Header("References")]
    public DifficultyBarScroll difficultyBar;
    
    [System.Serializable]
    public class DifficultyLevel
    {
        public string name = "Easy";
        [Range(0f, 1f)]
        public float triggerProgress = 0.25f; // 0 a 1 (25% = 0.25)
    }
    
        public DifficultyLevel[] difficultyLevels = {
        new DifficultyLevel { name = "Easy", triggerProgress = 0f },
        new DifficultyLevel { name = "Medium", triggerProgress = 0.25f },
        new DifficultyLevel { name = "Hard", triggerProgress = 0.6f },
        new DifficultyLevel { name = "Extreme", triggerProgress = 0.85f }
    };
    
    [Header("Runtime Info")]
    [SerializeField] private int currentDifficultyIndex = 0;
    [SerializeField] private string currentDifficultyName = "Easy";
    public EnemySpawner enemyspawner;
    
    // Events
    public static event Action<DifficultyLevel, int> OnDifficultyChanged;

    void Start()
    {
        enemyspawner.StartSpawning();
        if (difficultyBar == null)
            difficultyBar = FindFirstObjectByType<DifficultyBarScroll>();
            
        if (difficultyBar != null)
        {
            // Suscribirse al evento de progreso
            difficultyBar.OnProgressChanged += HandleProgressChanged;
        }
        
        // Establecer dificultad inicial
        ApplyDifficulty(0);
        
    }
    
    void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (difficultyBar != null)
        {
            difficultyBar.OnProgressChanged -= HandleProgressChanged;
        }
    }
    
    void HandleProgressChanged(float progress)
    {
        // Encontrar el nivel de dificultad que corresponde a este progreso
        int targetIndex = GetTargetDifficultyIndex(progress);
        
        if (targetIndex > currentDifficultyIndex)
        {
            AdvanceToDifficulty(targetIndex);
        }
    }
    
    int GetTargetDifficultyIndex(float progress)
    {
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (progress >= difficultyLevels[i].triggerProgress)
            {
                return i;
            }
        }
        return 0;
    }
    
    void AdvanceToDifficulty(int newIndex)
    {
        if (newIndex < 0 || newIndex >= difficultyLevels.Length) return;
        if (newIndex <= currentDifficultyIndex) return;
        
        int previousIndex = currentDifficultyIndex;
        currentDifficultyIndex = newIndex;
        
        ApplyDifficulty(newIndex);
        
        Debug.Log($"[DifficultyManager] *** DIFFICULTY INCREASED *** {difficultyLevels[previousIndex].name} -> {difficultyLevels[newIndex].name}");
        
        // Disparar evento
        OnDifficultyChanged?.Invoke(difficultyLevels[newIndex], newIndex);
    }
    
    void ApplyDifficulty(int index)
    {
        if (index < 0 || index >= difficultyLevels.Length) return;
        
        var difficulty = difficultyLevels[index];
        currentDifficultyName = difficulty.name;
        
        Debug.Log($"[DifficultyManager] Applied difficulty: {difficulty.name}");
    }
    
    // ===== GETTERS =====
    public DifficultyLevel GetCurrentDifficulty() => difficultyLevels[currentDifficultyIndex];
    public int GetCurrentDifficultyIndex() => currentDifficultyIndex;
    public string GetCurrentDifficultyName() => currentDifficultyName;
}
