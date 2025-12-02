using UnityEngine;
using UnityEngine.InputSystem;

public class Dash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Dash Default Direction")]
    public Vector3 defaultDashDirection = Vector3.forward; // Dirección por defecto si no hay input

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection;
    private Vector3 dashStart;
    private Vector3 dashEnd;

    private PlayerInput playerInput;
    private InputAction dashAction;
    private InputAction moveAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            dashAction = playerInput.actions["Dash"];
            moveAction = playerInput.actions["Move"];
        }
    }

    void OnEnable()
    {
        if (dashAction != null)
            dashAction.performed += OnDashPerformed;
    }

    void OnDisable()
    {
        if (dashAction != null)
            dashAction.performed -= OnDashPerformed;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimer / dashDuration);
            float curveT = dashCurve.Evaluate(t);
            transform.position = Vector3.Lerp(dashStart, dashEnd, curveT);
            if (t >= 1f)
            {
                isDashing = false;
            }
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (isDashing || cooldownTimer > 0f) return;

        // Dirección de movimiento actual
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);
        if (dir.sqrMagnitude < 0.1f)
        {
            // Dash en la dirección elegida por el usuario (local)
            dir = transform.TransformDirection(defaultDashDirection.normalized);
        }
        dashDirection = dir.normalized;
        dashStart = transform.position;
        dashEnd = dashStart + dashDirection * dashDistance;
        dashTimer = 0f;
        isDashing = true;
        cooldownTimer = dashCooldown;
    }
}
