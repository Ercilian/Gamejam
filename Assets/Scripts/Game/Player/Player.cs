using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;

public class Player : EntityStats
{
    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private InputAction healAction;

    private float rotationSpeed = 10f; // Velocidad de rotación del jugador
    private Vector2 movementInput;
    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control

    private Animator animator;
    private Rigidbody rb; // o CharacterController, según tu sistema




    // ================================================= Methods =================================================




    protected override void Awake()
    {
        base.Awake();
        playerInventory = GetComponent<PlayerInventory>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            healAction = playerInput.actions["Heal"];
    }

    void Start()
    {
        animator = GetComponent<Animator>();
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

        float currentSpeed = rb.linearVelocity.magnitude;
        animator.SetFloat("Moving", currentSpeed);
    }

    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

    /// <summary>
    /// Sobrescribe la lógica de muerte para usar el sistema de revive
    /// </summary>
    protected override void OnEntityDeath()
    {
        PlayerReviveSystem reviveSystem = GetComponent<PlayerReviveSystem>();
        if (reviveSystem != null)
        {
            // Entrar en estado "downed" en lugar de morir inmediatamente
            reviveSystem.EnterDownedState();
        }
        else
        {
            // Si no hay sistema de revive, usar comportamiento por defecto
            base.OnEntityDeath();
        }
    }

    // Si necesitas lógica especial al morir:
   /* public override void Die(DamageInfo finalDamage)
    {
        base.Die(finalDamage); // Llama a la lógica base (desactivar GameObject y eventos)
        // Aquí puedes añadir animaciones, sonidos, respawn, etc.
    }

    // Si necesitas lógica especial al recibir daño:
    public override void TakeDamage(DamageInfo damageInfo)
    {
        base.TakeDamage(damageInfo);
        // Aquí puedes añadir feedback visual, sonido, etc.
    }
    */
    
}
