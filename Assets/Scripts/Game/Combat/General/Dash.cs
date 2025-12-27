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

    [Header("Dash VFX")]
    public GameObject dashSmokeVFXPrefab; // Asigna el prefab en el inspector

    [Header("Collision Detection")]
    public LayerMask collisionLayers = -1; // Capas a detectar (por defecto todas)
    public float collisionCheckRadius = 0.5f; // Radio para SphereCast

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
            Vector3 targetPosition = Vector3.Lerp(dashStart, dashEnd, curveT);
            
            // Verificar si hay colisión en el camino hacia la nueva posición
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            float moveDistance = Vector3.Distance(transform.position, targetPosition);
            
            RaycastHit hit;
            Vector3 checkOrigin = transform.position + Vector3.up * collisionCheckRadius;
            
            if (Physics.SphereCast(checkOrigin, collisionCheckRadius, moveDirection, out hit, moveDistance + collisionCheckRadius, collisionLayers))
            {
                // Detener el dash si hay una colisión
                transform.position = transform.position + moveDirection * Mathf.Max(0, hit.distance - collisionCheckRadius);
                isDashing = false;
            }
            else
            {
                transform.position = targetPosition;
            }
            
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
        
        // Detectar colisiones antes de establecer dashEnd
        float actualDashDistance = dashDistance;
        RaycastHit hit;
        Vector3 sphereCastOrigin = dashStart + Vector3.up * collisionCheckRadius;
        
        // Verificar si ya está tocando algo en la dirección del dash desde la posición actual
        Vector3 currentCheckPos = dashStart + Vector3.up * collisionCheckRadius;
        Vector3 forwardCheckPos = currentCheckPos + dashDirection * collisionCheckRadius * 0.1f;
        
        // Usar OverlapSphere para detectar colisiones inmediatas
        Collider[] nearbyColliders = Physics.OverlapSphere(forwardCheckPos, collisionCheckRadius, collisionLayers, QueryTriggerInteraction.Ignore);
        
        // Filtrar el propio collider del jugador
        bool hasImmediateObstacle = false;
        foreach (Collider col in nearbyColliders)
        {
            if (col.transform != transform && !col.transform.IsChildOf(transform))
            {
                hasImmediateObstacle = true;
                break;
            }
        }
        
        if (hasImmediateObstacle)
        {
            // Hay una pared inmediatamente delante, no hacer dash
            return;
        }
        
        // Si no hay nada inmediato, hacer SphereCast normal
        if (Physics.SphereCast(sphereCastOrigin, collisionCheckRadius, dashDirection, out hit, dashDistance + collisionCheckRadius, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Filtrar si el hit es el propio jugador
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                // Si hay un obstáculo, ajustar la distancia del dash para detenerse antes del obstáculo
                actualDashDistance = Mathf.Max(0, hit.distance - collisionCheckRadius);
            }
        }
        
        // Solo iniciar el dash si hay distancia válida
        if (actualDashDistance < 0.1f)
        {
            return; // No hacer dash si no hay espacio
        }
        
        dashEnd = dashStart + dashDirection * actualDashDistance;
        dashTimer = 0f;
        isDashing = true;
        cooldownTimer = dashCooldown;

            if (dashSmokeVFXPrefab != null)
            {
                Vector3 vfxPosition = Vector3.Lerp(dashStart, dashEnd, 0.3f); // Puedes ajustar el 0.3f
                GameObject vfxInstance = Instantiate(dashSmokeVFXPrefab, vfxPosition, Quaternion.identity);
                DashSmokeVFX vfxScript = vfxInstance.GetComponent<DashSmokeVFX>();
                if (vfxScript != null)
                {
                    vfxScript.PlayDashSmoke(dashDirection);
                }
            }
    }
}
