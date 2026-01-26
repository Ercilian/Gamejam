using UnityEngine;

public class DifficultyBarScroll : MonoBehaviour
{
    [Header("References")]
    public RectTransform bar; // Imagen interna (4096x25)

    [Header("Speed Configuration")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 5f, 1f, 40f);
    
    [Header("Speed Multiplier")]
    public float speedMultiplier = 10f;
    
    public float maxScrollDistance = 4096f;

    [Header("Runtime Info (Read Only)")]
    [SerializeField]
    private float currentSpeed = 0f;
    
    [SerializeField]
    private float currentProgress = 0f;

    [SerializeField]
    private float currentX = 0f;

    // ===== EVENT FOR PROGRESS CHANGE =====
    public System.Action<float> OnProgressChanged; // Event for progress change (0 to 1)

    // ===== GETTERS PÚBLICOS PARA OTROS SCRIPTS =====

    /// <summary>
    /// Velocidad actual en píxeles por segundo
    /// </summary>
    public float GetCurrentSpeed() => currentSpeed;
    
    /// <summary>
    /// Progreso actual de 0 a 1 (0% a 100%)
    /// </summary>
    public float GetCurrentProgress() => currentProgress;
    
    /// <summary>
    /// Posición X actual en píxeles
    /// </summary>
    public float GetCurrentPosition() => currentX;
    
    /// <summary>
    /// Porcentaje completado (0 a 100)
    /// </summary>
    public float GetPercentageComplete() => currentProgress * 100f;
    
    /// <summary>
    /// ¿Ha llegado al final?
    /// </summary>
    public bool IsComplete() => currentProgress >= 1f;

    void Start()
    {
        // Calcular distancia máxima automáticamente si no está configurada
        if (maxScrollDistance <= 0f)
        {
            maxScrollDistance = bar.rect.width - ((RectTransform)transform).rect.width;
        }
    }

    void Update()
    {
        // ===== CALCULAR VALORES ACTUALES =====
        currentSpeed = GetSpeedForPosition(currentX);
        float delta = currentSpeed * Time.deltaTime;
        currentX += delta;
        
        // Calcular límites
        float maxDistance = bar.rect.width - ((RectTransform)transform).rect.width;
        currentX = Mathf.Clamp(currentX, 0f, maxDistance);
        
        // Actualizar progreso actual
        currentProgress = currentX / maxDistance;
        
        // Mover la barra
        bar.anchoredPosition = new Vector2(-currentX, 0f);
        
        // Enviar evento de progreso
        OnProgressChanged?.Invoke(currentProgress);
    }

    // ===== MÉTODOS ÚTILES ADICIONALES =====
    
    /// <summary>
    /// Resetear la barra al inicio
    /// </summary>
    public void ResetToStart()
    {
        currentX = 0f;
        currentProgress = 0f;
        currentSpeed = 0f;
        if (bar != null)
            bar.anchoredPosition = new Vector2(0f, 0f);
    }
    
    /// <summary>
    /// Pausar/despausar el scroll
    /// </summary>
    public void SetPaused(bool paused)
    {
        enabled = !paused;
    }
    
    /// <summary>
    /// Establecer velocidad multiplicador en runtime
    /// </summary>
    public void SetSpeedMultiplier(float newMultiplier)
    {
        speedMultiplier = newMultiplier;
    }
    
    private float GetSpeedForPosition(float position)
    {
        float progress = position / (bar.rect.width - ((RectTransform)transform).rect.width);
        return speedCurve.Evaluate(progress) * speedMultiplier;
    }
}
