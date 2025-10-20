using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputPush : MonoBehaviour
{
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    [Header("Variables to follow the car")]
    private Transform targetToFollow = null;
    private float followSpeed = 0f;
    private Player playerController;
    private bool isPushingNow = false;
    public bool activeControl = true;

    // Input System variables
    private PlayerInput playerInput;
    private InputAction interactAction;




    // ================================================= Methods =================================================




    void Awake()
    {
        playerController = GetComponent<Player>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"]; // Ensure this matches your Input Action name
        }
    }

    public void DeactivateControl() // Allow external scripts to disable control (MovCarro)
    {
        if (activeControl)
        {
            activeControl = false;
            if (playerController != null) playerController.activeControl = false; // Disable player control
        }
    }

    public void ActivateControl() // Allow external scripts to enable control (MovCarro)
    {
        if (!activeControl)
        {
            activeControl = true;
            targetToFollow = null;
            isPushingNow = false;
            if (playerController != null) playerController.activeControl = true; // Return control to player
        }
    }

    public void FollowObject(Transform target, float speed) // Allow external scripts to set the target to follow (MovCarro)
    {
        targetToFollow = target;
        followSpeed = speed;
    }

    public bool IsPushing() // Check if the player is currently pressing the "Interact" button
    {
        if (interactAction == null) return false;
        bool pulsando = interactAction.IsPressed();        
        return pulsando;
    }

    public bool ImPushing()
    {
        return isPushingNow;
    }

    void Update()
    {
        if (targetToFollow != null)
        {
            bool wantsToPush = IsPushing();
            
            if (wantsToPush && !isPushingNow) // Start pushing
            {
                isPushingNow = true;
                DeactivateControl();
                
                if (showDebugLogs)
                    Debug.Log($"[PlayerInputEmpuje] 🚗 {gameObject.name} start to push");
            }
            else if (!wantsToPush && isPushingNow) // Stop pushing
            {
                isPushingNow = false;
                ActivateControl();
                
                if (showDebugLogs)
                    Debug.Log($"[PlayerInputEmpuje] ❌ {gameObject.name} stop pushing");
            }
        }
        else
        {
            if (isPushingNow)
            {
                isPushingNow = false;
                ActivateControl();
            }
        }

        if (!activeControl && targetToFollow != null && isPushingNow) // Move towards the car only if I am the one pushing
        {
            Vector3 targetPosition = targetToFollow.position - targetToFollow.forward * 1.2f; // Offset behind the car
            targetPosition.y = transform.position.y;
            targetPosition.z = transform.position.z;

            Vector3 direction = (targetPosition - transform.position).normalized;
            
            if (Mathf.Abs(targetPosition.x - transform.position.x) > 0.01f)
            {
                transform.position += direction * followSpeed * Time.deltaTime;
            }
        }
    }
}
