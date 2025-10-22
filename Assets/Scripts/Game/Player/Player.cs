using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;

public class Player : EntityStats
{
    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private InputAction healAction;

    public float rotationSpeed = 10f; // Velocidad de rotación del jugador
    private Vector2 movementInput;
    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control




    // ================================================= Methods =================================================




    protected override void Awake()
    {
        base.Awake();
        playerInventory = GetComponent<PlayerInventory>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            healAction = playerInput.actions["Heal"];
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
                // Ajusta la rotación agregando un offset (prueba con 90, 180, -90 grados en Y)
                targetRotation *= Quaternion.Euler(0, 90, 0); // Cambia el 90 por el valor que necesites
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        if (healAction != null && healAction.WasPressedThisFrame())
        {
            playerInventory.UsePotion();
        }
    }
    
    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

    // Si necesitas lógica especial al morir:
    public override void Die(DamageInfo finalDamage)
    {
        base.Die(finalDamage); // Llama a la lógica base (desactivar GameObject y eventos)
        // Aquí puedes añadir animaciones, sonidos, respawn, etc.
    }

    // Si necesitas lógica especial al recibir daño:
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        // Aquí puedes añadir feedback visual, sonido, etc.
    }
}
