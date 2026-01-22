using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;
using System.Collections;

public class Player : EntityStats
{
    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private InputAction healAction;
    private PlayerInputPush playerInputPush;

    private float rotationSpeed = 10f; // Velocidad de rotación del jugador
    private Vector2 movementInput;
    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control

    private Animator animator;
    private Rigidbody rb; // o CharacterController, según tu sistema

    // ============ Tiempo de Gracia (Invulnerabilidad Temporal) ============
    [Header("Invulnerabilidad")]
    [SerializeField] private float gracePeriodDuration = 1.5f; // Duración del tiempo de gracia en segundos
    private bool isInGracePeriod = false; // Indica si está en tiempo de gracia

    // Propiedad pública para verificar si está en invulnerabilidad
    public bool IsInvulnerable => isInGracePeriod;




    // ================================================= Methods =================================================




    protected override void Awake()
    {
        base.Awake();
        playerInventory = GetComponent<PlayerInventory>();
        playerInput = GetComponent<PlayerInput>();
        playerInputPush = GetComponent<PlayerInputPush>();
        if (playerInput != null)
            healAction = playerInput.actions["Heal"];
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>(); // Busca en hijos (armature)
        rb = GetComponent<Rigidbody>(); // o el componente que uses para mover
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
        animator.SetFloat("Speed", movementMagnitude);

        if (playerInputPush != null)
            animator.SetBool("IsPushing", playerInputPush.ImPushing());
        else
            animator.SetBool("IsPushing", false);
    }

    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

    /// <summary>
    /// Sobrescribe TakeDamage para implementar tiempo de gracia (invulnerabilidad temporal)
    /// </summary>
    public override void TakeDamage(int amount)
    {
        // Si está en tiempo de gracia, ignorar el daño
        if (isInGracePeriod)
        {
            Debug.Log($"[{gameObject.name}] ¡Tiempo de gracia activo! Daño esquivado.");
            return;
        }

        // Aplicar daño normal desde la clase base
        base.TakeDamage(amount);

        // Iniciar el tiempo de gracia
        StartCoroutine(GracePeriodCoroutine());
    }

    /// <summary>
    /// Corrutina que gestiona el tiempo de gracia (invulnerabilidad temporal)
    /// </summary>
    private IEnumerator GracePeriodCoroutine()
    {
        isInGracePeriod = true;
        Debug.Log($"[{gameObject.name}] Tiempo de gracia iniciado por {gracePeriodDuration} segundos");

        // Aquí puedes añadir feedback visual (parpadeo, cambio de color, etc.)
        // Por ejemplo, cambiar el color del player o hacerlo semitransparente
        VisualGracePeriodFeedback(true);

        yield return new WaitForSeconds(gracePeriodDuration);

        isInGracePeriod = false;
        VisualGracePeriodFeedback(false);
        Debug.Log($"[{gameObject.name}] Tiempo de gracia finalizado. Vulnerable nuevamente.");
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
        PlayerReviveSystem reviveSystem = GetComponent<PlayerReviveSystem>();
        if (reviveSystem != null)
        {   
            animator.SetTrigger("IsDead");
            // Entrar en estado "downed" en lugar de morir inmediatamente
            reviveSystem.EnterDownedState();
        }
        else
        {
            // Si no hay sistema de revive, usar comportamiento por defecto
            base.OnEntityDeath();
        }
    }
}
