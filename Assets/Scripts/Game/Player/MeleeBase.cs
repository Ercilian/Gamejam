using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MeleeBase : MonoBehaviour
{
    public enum HitboxShape { Box, Sphere, Capsule, Sector }

    [Header("Ataque")]
    public float attackCooldown = 0.3f;
    public int comboCount = 3;
    public float comboResetTime = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;
    [Tooltip("Offset local de la hitbox respecto al centro del personaje (en espacio local)")]
    public Vector3 attackOffset = new Vector3(0, 0, 2f); // Por defecto enfrente

    [Header("Hitbox por defecto (controlada por Gizmos)")]
    public HitboxShape hitboxShape = HitboxShape.Box;
    [Tooltip("Rotación local (Euler) adicional para orientar la hitbox respecto al personaje")]
    public Vector3 hitboxLocalEuler;
    [Tooltip("Escala visual y multiplicador para Box. No se aplica a Sphere/Capsule/Sector")]
    public Vector3 hitboxScale = Vector3.one;
    [Tooltip("Tamaño de la Box (usado para Box + Gizmos)")]
    public Vector3 boxSize = new Vector3(1f, 1f, 1f);
    [Tooltip("Radio de la Sphere (usado para Sphere + Gizmos)")]
    public float sphereRadius = 1f;
    [Tooltip("Altura total de la Capsule (usado para Capsule + Gizmos)")]
    public float capsuleHeight = 2f;
    [Tooltip("Radio de la Capsule (usado para Capsule + Gizmos)")]
    public float capsuleRadius = 0.5f;
    [Tooltip("Radio del Sector/Arco (usado para Sector + Gizmos)")]
    public float sectorRadius = 2f;
    [Tooltip("Ángulo del Sector en grados (usado para Sector + Gizmos)")]
    [Range(0f, 360f)] public float sectorAngleDeg = 90f;
    [Tooltip("Altura del Sector (tolerancia vertical)")]
    public float sectorHeight = 1.5f;
    [Tooltip("Resolución del arco en Gizmos (más pasos = arco más suave)")]
    [Range(4, 128)] public int sectorGizmoSteps = 24;

    [System.Serializable]
    public class HitboxConfig
    {
        [Header("Transformación")]
        public Vector3 offset = new Vector3(0, 0, 2f);
        public Vector3 euler;

        [Header("Forma")]
        public HitboxShape shape = HitboxShape.Box;

        [Header("Box")]
        public Vector3 boxSize = new Vector3(1f, 1f, 1f);
        public Vector3 boxScale = Vector3.one;

        [Header("Sphere")]
        public float sphereRadius = 1f;

        [Header("Capsule")]
        public float capsuleHeight = 2f;
        public float capsuleRadius = 0.5f;

        [Header("Sector")]
        public float sectorRadius = 2f;
        [Range(0f, 360f)] public float sectorAngleDeg = 90f;
        public float sectorHeight = 1.5f;
        [Range(4, 128)] public int sectorGizmoSteps = 24;

        [Header("Daño")]
        [Tooltip("Si es >= 0, reemplaza attackDamage")]
        public int damageOverride = -1;
    }

    [Header("Hitboxes por paso de combo (opcional)")]
    [Tooltip("Si se deja vacío, se usa la configuración por defecto. Índice 0 = primer ataque, 1 = segundo, etc.")]
    public List<HitboxConfig> comboHitboxes = new List<HitboxConfig>();

    [Header("Gizmos")]
    [Tooltip("Paso del combo a previsualizar en Scene (0..comboCount-1)")]
    public int gizmoPreviewStep = 0;

    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private float comboTimer = 0f;

    private UnityEngine.InputSystem.PlayerInput playerInput;
    private UnityEngine.InputSystem.InputAction attackAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            attackAction = playerInput.actions["Attack"];
        }
    }

    void OnEnable()
    {
        if (attackAction != null)
        {
            attackAction.performed += OnAttackPerformed;
            if (!attackAction.enabled) attackAction.Enable();
        }
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        TryAttack();
    }

    void Update()
    {
        // Reset combo si pasa mucho tiempo sin atacar
        if (currentCombo > 0 && Time.time - comboTimer > comboResetTime)
        {
            currentCombo = 0;
        }
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        comboTimer = Time.time;
        currentCombo = (currentCombo + 1) % comboCount;

        // Disparar animación si aplica (por combo)
        // animator.SetTrigger($"Attack{currentCombo}");

        // Seleccionar configuración de hitbox para este paso
        HitboxConfig cfg = GetHitboxConfig(currentCombo);

        // Detección según la forma seleccionada (coincide con los Gizmos)
        var hits = DetectHits(cfg);
        foreach (var h in hits)
        {
            // TODO: Reemplaza por tu sistema de daño (EnemyHealth, IDamageable, etc.)
            // var health = h.GetComponent<EnemyHealth>();
            // if (health) health.TakeDamage(attackDamage);
            Destroy(h.gameObject);
        }
    }

    // Gizmos: vista previa de colocación/orientación/forma de la hitbox
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        HitboxConfig cfg = GetHitboxConfigForGizmos();
        Vector3 center = transform.TransformPoint(cfg.offset);
        Quaternion rot = transform.rotation * Quaternion.Euler(cfg.euler);

        switch (cfg.shape)
        {
            case HitboxShape.Box:
            {
                Vector3 size = Vector3.Scale(cfg.boxSize, cfg.boxScale == Vector3.zero ? Vector3.one : cfg.boxScale);
                Matrix4x4 m = Matrix4x4.TRS(center, rot, Vector3.one);
                var prev = Gizmos.matrix; Gizmos.matrix = m;
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = prev;
                break;
            }
            case HitboxShape.Sphere:
            {
                Gizmos.DrawWireSphere(center, cfg.sphereRadius);
                break;
            }
            case HitboxShape.Capsule:
            {
                DrawWireCapsule(center, rot, cfg.capsuleHeight, cfg.capsuleRadius);
                break;
            }
            case HitboxShape.Sector:
            {
                DrawWireSector(center, rot, cfg.sectorRadius, cfg.sectorAngleDeg, cfg.sectorHeight, cfg.sectorGizmoSteps);
                break;
            }
        }
    }

    HitboxConfig GetHitboxConfig(int comboIndex)
    {
        if (comboHitboxes != null && comboHitboxes.Count > 0)
        {
            int idx = Mathf.Clamp(comboIndex, 0, comboHitboxes.Count - 1);
            var cfg = comboHitboxes[idx];
            if (cfg != null) return cfg;
        }

        // Fallback a configuración por defecto
        return DefaultConfig();
    }

    HitboxConfig GetHitboxConfigForGizmos()
    {
        if (comboHitboxes != null && comboHitboxes.Count > 0)
        {
            int idx = Mathf.Clamp(gizmoPreviewStep, 0, comboHitboxes.Count - 1);
            var cfg = comboHitboxes[idx];
            if (cfg != null) return cfg;
        }
        return DefaultConfig();
    }

    HitboxConfig DefaultConfig()
    {
        return new HitboxConfig
        {
            offset = attackOffset,
            euler = hitboxLocalEuler,
            shape = hitboxShape,
            boxSize = boxSize,
            boxScale = hitboxScale,
            sphereRadius = sphereRadius,
            capsuleHeight = capsuleHeight,
            capsuleRadius = capsuleRadius,
            sectorRadius = sectorRadius,
            sectorAngleDeg = sectorAngleDeg,
            sectorHeight = sectorHeight,
            sectorGizmoSteps = sectorGizmoSteps,
            damageOverride = -1
        };
    }

    Collider[] DetectHits(HitboxConfig cfg)
    {
        Vector3 center = transform.TransformPoint(cfg.offset);
        Quaternion rotation = transform.rotation * Quaternion.Euler(cfg.euler);

        switch (cfg.shape)
        {
            case HitboxShape.Box:
            {
                Vector3 size = Vector3.Scale(cfg.boxSize, cfg.boxScale == Vector3.zero ? Vector3.one : cfg.boxScale);
                Vector3 halfExtents = size * 0.5f;
                return Physics.OverlapBox(center, halfExtents, rotation, enemyLayer, QueryTriggerInteraction.Collide);
            }
            case HitboxShape.Sphere:
            {
                return Physics.OverlapSphere(center, cfg.sphereRadius, enemyLayer, QueryTriggerInteraction.Collide);
            }
            case HitboxShape.Capsule:
            {
                Vector3 up = rotation * Vector3.up;
                float half = Mathf.Max(0f, (cfg.capsuleHeight * 0.5f) - cfg.capsuleRadius);
                Vector3 p0 = center + up * half;
                Vector3 p1 = center - up * half;
                return Physics.OverlapCapsule(p0, p1, cfg.capsuleRadius, enemyLayer, QueryTriggerInteraction.Collide);
            }
            case HitboxShape.Sector:
            default:
            {
                var candidates = Physics.OverlapSphere(center, cfg.sectorRadius, enemyLayer, QueryTriggerInteraction.Collide);
                if (candidates == null || candidates.Length == 0) return candidates;

                Vector3 fwd = rotation * Vector3.forward;
                float halfAngle = cfg.sectorAngleDeg * 0.5f;
                List<Collider> list = new List<Collider>(candidates.Length);
                foreach (var c in candidates)
                {
                    if (c == null) continue;
                    Vector3 to = c.bounds.center - center;
                    if (Mathf.Abs(to.y) > cfg.sectorHeight * 0.5f) continue;
                    Vector3 toFlat = new Vector3(to.x, 0f, to.z);
                    Vector3 fwdFlat = new Vector3(fwd.x, 0f, fwd.z);
                    if (toFlat.sqrMagnitude < 0.0001f) continue;
                    float ang = Vector3.Angle(fwdFlat, toFlat);
                    if (ang <= halfAngle)
                        list.Add(c);
                }
                return list.ToArray();
            }
        }
    }

    // Gizmo helpers
    void DrawWireCapsule(Vector3 center, Quaternion rot, float height, float radius)
    {
        // Aproximación: dibuja dos esferas y líneas
        Vector3 up = rot * Vector3.up;
        float half = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 p0 = center + up * half;
        Vector3 p1 = center - up * half;
        Gizmos.DrawWireSphere(p0, radius);
        Gizmos.DrawWireSphere(p1, radius);
        // Conectar con 4 líneas cardinales
        Vector3 right = rot * Vector3.right;
        Vector3 fwd = rot * Vector3.forward;
        Gizmos.DrawLine(p0 + right * radius, p1 + right * radius);
        Gizmos.DrawLine(p0 - right * radius, p1 - right * radius);
        Gizmos.DrawLine(p0 + fwd * radius, p1 + fwd * radius);
        Gizmos.DrawLine(p0 - fwd * radius, p1 - fwd * radius);
    }

    void DrawWireSector(Vector3 center, Quaternion rot, float radius, float angleDeg, float height, int steps)
    {
        // Dibuja arco en plano XZ y una caja de altura para referencia
        Vector3 fwd = rot * Vector3.forward;
        Vector3 right = rot * Vector3.right;
        float half = angleDeg * 0.5f;
        float start = -half;
        float step = Mathf.Max(1f, angleDeg / Mathf.Max(4, steps));

        Vector3 prevPoint = center + Quaternion.AngleAxis(start, Vector3.up) * fwd * radius;
        for (float a = start + step; a <= half + 0.001f; a += step)
        {
            Vector3 nextPoint = center + Quaternion.AngleAxis(a, Vector3.up) * fwd * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        // Líneas de los bordes del sector
        Vector3 edgeL = center + Quaternion.AngleAxis(-half, Vector3.up) * fwd * radius;
        Vector3 edgeR = center + Quaternion.AngleAxis(half, Vector3.up) * fwd * radius;
        Gizmos.DrawLine(center, edgeL);
        Gizmos.DrawLine(center, edgeR);

        // Altura (rectángulo de referencia)
        Vector3 up = Vector3.up * (height * 0.5f);
        Gizmos.DrawLine(center - up, center + up);
    }

    // (Sin prefab) La hitbox real es esta caja calculada; los Gizmos muestran su posición/orientación/tamaño.
}
