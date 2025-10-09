using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public float speed = 5;
    private Vector2 movementInput;
    [HideInInspector] public bool activeControl = true; // Allow external scripts (like PlayerInputEmpuje) to enable/disable control




    // ================================================= Methods =================================================




    private void Update()
    {
        if (activeControl)
        {
            transform.Translate(new Vector3(movementInput.x, 0, movementInput.y) * speed * Time.deltaTime); // Move the player based on input
        }
    }
    
    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>(); // Called by Input System

}
