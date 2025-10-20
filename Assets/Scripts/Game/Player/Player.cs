using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    public float speed = 5;
    public float rotationSpeed = 10f; // Velocidad de rotación del jugador
    private Vector2 movementInput;
    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control




    // ================================================= Methods =================================================




    private void Update()
    {
        if (activeControl)
        {
            // Movimiento en espacio mundial
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
    }
    
    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

}
