using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public float speed = 5;
    private Vector2 movementInput;

    // Permite que otros scripts activen/desactiven el control
    [HideInInspector] public bool controlActivo = true;

    private void Update()
    {
        if (controlActivo)
        {
            transform.Translate(new Vector3(movementInput.x, 0, movementInput.y) * speed * Time.deltaTime);
        }
    }
    
    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>();

}
