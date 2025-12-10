using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Sistema de revivir jugadores. Cuando un jugador llega a 0 HP, entra en estado "downed"
/// y puede ser revivido por otro jugador antes de que expire el temporizador.
/// </summary>
public class PlayerReviveSystem : MonoBehaviour
{
    [Header("Revive Settings")]
    [Tooltip("Tiempo en segundos antes de morir definitivamente")]
    public float timeToRevive = 10f;
    
    [Tooltip("Tiempo necesario que otro jugador debe mantener el botón de interacción para revivir")]
    public float reviveHoldTime = 3f;
    
    [Tooltip("Rango en el que otro jugador puede revivir")]
    public float reviveRange = 2f;
    
    [Tooltip("Vida con la que revive el jugador (porcentaje de HP máximo)")]
    [Range(0f, 1f)]
    public float reviveHealthPercent = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Objeto visual que se muestra cuando el jugador está caído (opcional)")]
    public GameObject downedVisual;
    
    [Tooltip("Color del modelo cuando está caído")]
    public Color downedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool drawGizmos = true;

    // Estados
    public enum ReviveState
    {
        Alive,      // Jugador vivo y activo
        Downed,     // Jugador caído, esperando revivir
        Dead        // Jugador muerto definitivamente
    }

    private ReviveState currentState = ReviveState.Alive;
    private float downedTimer = 0f;
    private float reviveProgress = 0f;
    private Player reviverPlayer = null; // Jugador que está reviviendo
    
    // Referencias
    private EntityStats entityStats;
    private Player player;
    private Renderer[] renderers;
    private Color[] originalColors;

    // Eventos
    public System.Action OnPlayerDowned;
    public System.Action OnPlayerRevived;
    public System.Action OnPlayerDead;
    public System.Action<float> OnReviveProgressChanged; // Para UI de progreso

    void Awake()
    {
        entityStats = GetComponent<EntityStats>();
        player = GetComponent<Player>();
        renderers = GetComponentsInChildren<Renderer>();
        
        // Guardar colores originales
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
        }

        if (downedVisual != null)
            downedVisual.SetActive(false);
    }

    void Update()
    {
        if (currentState == ReviveState.Downed)
        {
            UpdateDownedState();
        }
    }

    /// <summary>
    /// Llamado cuando el jugador llega a 0 HP
    /// </summary>
    public void EnterDownedState()
    {
        if (currentState != ReviveState.Alive) return;

        currentState = ReviveState.Downed;
        downedTimer = timeToRevive;
        reviveProgress = 0f;

        // Desactivar control del jugador
        if (player != null)
            player.activeControl = false;

        // Activar visual de caído
        if (downedVisual != null)
            downedVisual.SetActive(true);

        // Cambiar color del modelo
        SetModelColor(downedColor);

        OnPlayerDowned?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Jugador caído. Tiempo para revivir: {timeToRevive}s");
    }

    /// <summary>
    /// Actualiza el estado cuando el jugador está caído
    /// </summary>
    private void UpdateDownedState()
    {
        downedTimer -= Time.deltaTime;

        if (downedTimer <= 0f)
        {
            // Tiempo agotado, morir definitivamente
            Die();
            return;
        }

        // Buscar jugadores cercanos que puedan revivir
        CheckForReviver();
    }

    /// <summary>
    /// Busca jugadores cercanos que puedan revivir
    /// </summary>
    private void CheckForReviver()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, reviveRange);
        
        foreach (var col in nearbyColliders)
        {
            Player otherPlayer = col.GetComponent<Player>();
            if (otherPlayer == null || otherPlayer == player) continue;

            PlayerReviveSystem otherReviveSystem = otherPlayer.GetComponent<PlayerReviveSystem>();
            if (otherReviveSystem == null || otherReviveSystem.currentState != ReviveState.Alive) continue;

            // Verificar si el otro jugador está presionando el botón de interacción
            PlayerInput otherInput = otherPlayer.GetComponent<PlayerInput>();
            if (otherInput != null)
            {
                InputAction interactAction = otherInput.actions["Interact"];
                if (interactAction != null && interactAction.IsPressed())
                {
                    // Incrementar progreso de revivir
                    reviveProgress += Time.deltaTime;
                    reviverPlayer = otherPlayer;
                    
                    OnReviveProgressChanged?.Invoke(reviveProgress / reviveHoldTime);

                    if (showDebugLogs && Time.frameCount % 30 == 0)
                        Debug.Log($"[{gameObject.name}] Siendo revivido por {otherPlayer.name}. Progreso: {reviveProgress:F1}/{reviveHoldTime}");

                    if (reviveProgress >= reviveHoldTime)
                    {
                        Revive();
                        return;
                    }
                }
                else
                {
                    // Resetear progreso si suelta el botón
                    if (reviveProgress > 0f)
                    {
                        reviveProgress = 0f;
                        reviverPlayer = null;
                        OnReviveProgressChanged?.Invoke(0f);
                    }
                }
            }
        }

        // Si no hay nadie reviviendo, resetear progreso
        if (reviverPlayer == null && reviveProgress > 0f)
        {
            reviveProgress = 0f;
            OnReviveProgressChanged?.Invoke(0f);
        }
    }

    /// <summary>
    /// Revive al jugador
    /// </summary>
    private void Revive()
    {
        currentState = ReviveState.Alive;
        
        // Restaurar HP
        int reviveHP = Mathf.RoundToInt(entityStats.MaxHP * reviveHealthPercent);
        entityStats.curHP = reviveHP;

        // Reactivar control
        if (player != null)
            player.activeControl = true;

        // Desactivar visual de caído
        if (downedVisual != null)
            downedVisual.SetActive(false);

        // Restaurar color del modelo
        RestoreModelColor();

        reviveProgress = 0f;

        OnPlayerRevived?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Jugador revivido por {(reviverPlayer != null ? reviverPlayer.name : "desconocido")}");

        reviverPlayer = null;
    }

    /// <summary>
    /// Mata definitivamente al jugador
    /// </summary>
    private void Die()
    {
        currentState = ReviveState.Dead;
        
        OnPlayerDead?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Jugador muerto definitivamente.");

        // Desactivar el GameObject o destruirlo
        Destroy(gameObject);
    }

    /// <summary>
    /// Cambia el color del modelo
    /// </summary>
    private void SetModelColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = color;
        }
    }

    /// <summary>
    /// Restaura el color original del modelo
    /// </summary>
    private void RestoreModelColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
    }

    // Getters públicos
    public ReviveState GetState() => currentState;
    public float GetDownedTimeRemaining() => downedTimer;
    public float GetReviveProgress() => reviveProgress / reviveHoldTime;
    public bool IsDowned() => currentState == ReviveState.Downed;
    public bool IsAlive() => currentState == ReviveState.Alive;

    void OnDrawGizmos()
    {
        if (!drawGizmos || currentState != ReviveState.Downed) return;

        // Dibujar rango de revivir
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reviveRange);
    }
}
