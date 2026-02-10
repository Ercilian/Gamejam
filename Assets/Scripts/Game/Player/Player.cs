using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;
using System.Collections;

public class Player : EntityStats

{
    private PlayerPanelUI playerPanelUI;
    [Header("Identidad de personaje")]
    public int characterIndex = -1; // Se asigna al instanciar
    
    // Permite asignar el panel de UI correcto desde fuera
    public void SetPlayerPanelUI(PlayerPanelUI panel)
    {
        playerPanelUI = panel;
        // Actualiza la barra de vida al valor actual al asignar el panel
        if (playerPanelUI != null)
            playerPanelUI.SetHealth(curHP);
    }

    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private InputAction healAction;
    private PlayerInputPush playerInputPush;
    private ComboHitboxController comboHitboxController;

    private float rotationSpeed = 10f; // Velocidad de rotación del jugador
    private Vector2 movementInput;

    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control
    [HideInInspector] public bool isCinematicRun = false; // Forzar animación de correr en cinemática

    private Rigidbody rb; // o CharacterController, según tu sistema

    // ============ Tiempo de Gracia (Invulnerabilidad Temporal) ============
    [Header("Invulnerabilidad")]
    [SerializeField] private float gracePeriodDuration = 1.5f; // Duración del tiempo de gracia en segundos
    private bool isInGracePeriod = false; // Indica si está en tiempo de gracia

    // Propiedad pública para verificar si está en invulnerabilidad
    public bool IsInvulnerable => isInGracePeriod;

    // ============ Estado de muerte ============
    public bool isDowned = false; // Indica si el jugador está muerto o en estado downed
    public bool isDead = false;




    // ================================================= Methods =================================================




    protected override void Awake()
    {
        base.Awake();
        playerInventory = GetComponent<PlayerInventory>();
        playerInput = GetComponent<PlayerInput>();
        playerInputPush = GetComponent<PlayerInputPush>();
        if (playerInput != null)
            healAction = playerInput.actions["Heal"];

        // Buscar el UI en la escena (puedes cambiar esto si tienes varios jugadores)
        comboHitboxController = GetComponent<ComboHitboxController>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // o el componente que uses para mover
        // animator ya se obtiene en EntityStats.Awake()
        // playerPanelUI se asigna ahora desde InGameUI, no aquí
    }

    private void Update()
    {
        if (activeControl)
        {
            // Movimiento en espacio mundial usando la variable 'speed' heredada de EntityStats
            Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * speed * Time.deltaTime;
            transform.position += movement;
            
            // Rotación hacia la dirección del movimiento
            if (movementInput != Vector2.zero)
            {
                Vector3 direction = new Vector3(movementInput.x, 0, movementInput.y);
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        if (healAction != null && healAction.WasPressedThisFrame())
        {
            playerInventory.UsePotion();
        }

        // Calcular velocidad para el blend tree (0 = idle, 1 = caminando)
        float movementMagnitude = movementInput.magnitude;
        if (isCinematicRun)
        {
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", movementMagnitude);
        }

        if (playerInputPush != null)
            animator.SetBool("IsPushing", playerInputPush.ImPushing());
        else
            animator.SetBool("IsPushing", false);
    }
    /// <summary>
    /// Habilita o deshabilita el input del jugador
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        activeControl = enabled;
        if (!enabled)
        {
            movementInput = Vector2.zero;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

    /// <summary>
    /// Sobrescribe TakeDamage para implementar tiempo de gracia (invulnerabilidad temporal)
    /// </summary>
    public override void TakeDamage(int amount)
    {
        // Si está muerto o en estado downed, ignorar el daño
        if (isDowned)
        {
            return;
        }
        // Si está en tiempo de gracia, ignorar el daño
        if (isInGracePeriod)
        {
            return;
        }

        // Aplicar daño normal desde la clase base
        base.TakeDamage(amount);

        // Actualizar la barra de vida en el UI
        if (playerPanelUI != null)
        {
            playerPanelUI.SetHealth(curHP); // curHP es de EntityStats
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] playerPanelUI es null al intentar actualizar la vida");
        }

        // Iniciar el tiempo de gracia
        StartCoroutine(GracePeriodCoroutine());
    }

    public override void Heal(int amount)
    {
        base.Heal(amount);

        // Actualizar la barra de vida en el UI
        if (playerPanelUI != null)
        {
            playerPanelUI.SetHealth(curHP); // curHP es de EntityStats
        }
    }
    /// <summary>
    /// Corrutina que gestiona el tiempo de gracia (invulnerabilidad temporal)
    /// </summary>
    private IEnumerator GracePeriodCoroutine()
    {
        isInGracePeriod = true;

        // Aquí puedes añadir feedback visual (parpadeo, cambio de color, etc.)
        // Por ejemplo, cambiar el color del player o hacerlo semitransparente
        VisualGracePeriodFeedback(true);

        yield return new WaitForSeconds(gracePeriodDuration);

        isInGracePeriod = false;
        VisualGracePeriodFeedback(false);
    }

    /// <summary>
    /// Proporciona feedback visual durante el tiempo de gracia (parpadeo)
    /// </summary>
    private void VisualGracePeriodFeedback(bool isGracing)
    {
        if (isGracing)
        {
            // Puedes cambiar el color del jugador para indicar invulnerabilidad
            // Ejemplo: cambiar a un color más transparente o diferente
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(FlashPlayerDuringGracePeriod(renderer));
            }
        }
    }

    /// <summary>
    /// Hace parpadear el jugador durante el tiempo de gracia
    /// </summary>
    private IEnumerator FlashPlayerDuringGracePeriod(Renderer renderer)
    {
        float elapsedTime = 0f;
        Color originalColor = renderer.material.color;

        while (elapsedTime < gracePeriodDuration)
        {
            // Cambiar entre visible e invisible (parpadeo)
            float alpha = Mathf.PingPong(elapsedTime * 5f, 1f); // Parpadea 5 veces por segundo
            Color newColor = originalColor;
            newColor.a = alpha;
            renderer.material.color = newColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Restaurar el color original
        renderer.material.color = originalColor;
    }

    /// <summary>
    /// Sobrescribe la lógica de muerte para usar el sistema de revive
    /// </summary>
    public override void OnEntityDeath()
    {
        playerInventory.ClearInventory();
        isDowned = true;
        PlayerReviveSystem reviveSystem = GetComponent<PlayerReviveSystem>();
        if (reviveSystem != null)
        {   
            animator.SetTrigger("IsDead");
            // Entrar en estado "downed" en lugar de morir inmediatamente
            reviveSystem.EnterDownedState();
            comboHitboxController.enabled = false;
        }
        else
        {
            // Si no hay sistema de revive, usar comportamiento por defecto
            base.OnEntityDeath();
        }

        // Actualizar la barra de vida en el UI (vida 0)
        if (playerPanelUI != null)
            playerPanelUI.SetHealth(0);
    }
}
