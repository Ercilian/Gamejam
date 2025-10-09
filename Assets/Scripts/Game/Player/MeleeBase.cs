using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

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

    [Header("Boomerang (solo tercer ataque)")]
    [Tooltip("Distancia que recorre la hitbox antes de volver")] public float boomerangDistance = 5f;
    [Tooltip("Velocidad de ida y vuelta de la hitbox")] public float boomerangSpeed = 10f;
    [Tooltip("Tiempo suspendido en el aire en el punto máximo")] public float boomerangPauseTime = 1f;
    
    
    public enum BoomerangAimMode { Forward, DirectionVector, WorldTarget }
    [Tooltip("Modo de apuntado del boomerang")] public BoomerangAimMode boomerangAimMode = BoomerangAimMode.Forward;
    [Tooltip("Vector de dirección en mundo cuando el modo es DirectionVector")] public Vector3 boomerangDirection = new Vector3(0, 0, 1);
    [Tooltip("Punto objetivo en coordenadas de mundo cuando el modo es WorldTarget")] public Vector3 boomerangTargetWorld;

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
    [Tooltip("Si está activo y el paso previsualizado es el 3º, la hitbox se anima como boomerang")] public bool previewBoomerangGizmos = true;
    [Tooltip("Dibuja los gizmos también en la Game view durante Play (activa el botón Gizmos en la Game view)")]
    public bool showGizmosInGameView = true;
    [Tooltip("Mostrar la hitbox en tiempo real del boomerang mientras está activo en Play")]
    public bool showRuntimeBoomerangHitbox = true;

    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private float comboTimer = 0f;

    private UnityEngine.InputSystem.PlayerInput playerInput;
    private UnityEngine.InputSystem.InputAction attackAction;

    // Estado de boomerang en runtime para dibujar en Game view
    private bool boomerangActive = false;
    private Vector3 boomerangCurCenter;
    private Quaternion boomerangRotation;
    private HitboxConfig boomerangCfgRuntime;

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
        // Usar el paso actual (0 = primer ataque, 1 = segundo, 2 = tercero)
        int step = currentCombo;

        // Disparar animación si aplica (por combo)
        // animator.SetTrigger($"Attack{currentCombo}");

        // Seleccionar configuración de hitbox para este paso
        HitboxConfig cfg = GetHitboxConfig(step);

        // Si es el tercer ataque (índice 2), la hitbox se comporta como un boomerang
        if (step == 2)
        {
            StartCoroutine(DoBoomerangAttack(cfg));
            // Avanza el combo tras ejecutar el ataque
            currentCombo = (currentCombo + 1) % comboCount;
            return;
        }

        // Ataques normales (detección instantánea)
        var hits = DetectHits(cfg);
        foreach (var h in hits)
        {
            // TODO: Reemplaza por tu sistema de daño (EnemyHealth, IDamageable, etc.)
            // var health = h.GetComponent<EnemyHealth>();
            // if (health) health.TakeDamage(attackDamage);
            Destroy(h.gameObject);
        }
        // Avanza el combo tras ejecutar el ataque
        currentCombo = (currentCombo + 1) % comboCount;
    }

    // Gizmos: dibuja en Scene y también en Game view (si está activado el botón Gizmos)
    void OnDrawGizmos()
    {
        if (Application.isPlaying && !showGizmosInGameView) return;

        Gizmos.color = Color.red;
        HitboxConfig cfg = GetHitboxConfigForGizmos();
        Vector3 center = transform.TransformPoint(cfg.offset);
        Quaternion rot = transform.rotation * Quaternion.Euler(cfg.euler);

        // En runtime: si el boomerang está activo, dibujar su hitbox real
        if (Application.isPlaying && showRuntimeBoomerangHitbox && boomerangActive && boomerangCfgRuntime != null)
        {
            cfg = boomerangCfgRuntime;
            center = boomerangCurCenter;
            rot = boomerangRotation;
        }

        // Si se está previsualizando el tercer ataque y la opción está activa, animar la posición como boomerang
        if (!(Application.isPlaying && boomerangActive) && previewBoomerangGizmos && gizmoPreviewStep == 2)
        {
            Vector3 startCenter = transform.TransformPoint(cfg.offset);
            // Dirección preferida para el boomerang (apuntado por coordenadas)
            Vector3 forward = GetBoomerangDirectionFrom(startCenter);
            Vector3 maxCenter = startCenter + forward * boomerangDistance;

            float outTime = Mathf.Max(0.0001f, boomerangDistance / Mathf.Max(0.0001f, boomerangSpeed));
            float pause = Mathf.Max(0f, boomerangPauseTime);
            float backTime = outTime;
            float total = outTime + pause + backTime;

            float t = Mathf.Repeat(Time.realtimeSinceStartup, total);
            if (t < outTime)
            {
                float k = t / outTime;
                center = Vector3.Lerp(startCenter, maxCenter, k);
            }
            else if (t < outTime + pause)
            {
                center = maxCenter;
            }
            else
            {
                float k = (t - outTime - pause) / backTime;
                center = Vector3.Lerp(maxCenter, startCenter, k);
            }

            // Trayectoria de referencia
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
            Gizmos.DrawLine(startCenter, maxCenter);
            Gizmos.color = Color.red;
        }

    // Dibuja la etiqueta flotante con el paso de combo
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"Combo Step: {gizmoPreviewStep}");
        #endif

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

        return DetectHitsAt(cfg, center, rotation);
    }

    // Detección en una posición/rotación específicas (para la trayectoria del boomerang)
    Collider[] DetectHitsAt(HitboxConfig cfg, Vector3 center, Quaternion rotation)
    {
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

    IEnumerator DoBoomerangAttack(HitboxConfig cfg)
    {
        // Dirección basada en coordenadas (o forward del jugador)
        Vector3 startCenter = transform.TransformPoint(cfg.offset);
        Vector3 forward = GetBoomerangDirectionFrom(startCenter);
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 maxCenter = startCenter + forward * boomerangDistance;

        // Para no golpear varias veces a los mismos enemigos
        HashSet<Collider> hitSet = new HashSet<Collider>();

        // Inicia estado de dibujo runtime
        boomerangActive = true;
        boomerangRotation = rotation;
        boomerangCurCenter = startCenter;
        boomerangCfgRuntime = CloneConfig(cfg);

        // Fase 1: ir hacia adelante
        Vector3 cur = startCenter;
        while ((maxCenter - cur).sqrMagnitude > 0.0004f)
        {
            float step = boomerangSpeed * Time.deltaTime;
            cur = Vector3.MoveTowards(cur, maxCenter, step);
            boomerangCurCenter = cur;
            var hits = DetectHitsAt(cfg, cur, rotation);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (hitSet.Contains(h)) continue;
                hitSet.Add(h);
                // TODO: Reemplaza por tu sistema de daño
                // var health = h.GetComponent<EnemyHealth>();
                // if (health) health.TakeDamage(cfg.damageOverride >= 0 ? cfg.damageOverride : attackDamage);
                Destroy(h.gameObject);
            }
            yield return null;
        }

        // Fase 2: pausa en el aire
        float t = 0f;
        while (t < boomerangPauseTime)
        {
            t += Time.deltaTime;
            boomerangCurCenter = cur;
            var hits = DetectHitsAt(cfg, cur, rotation);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (hitSet.Contains(h)) continue;
                hitSet.Add(h);
                // TODO: Reemplaza por tu sistema de daño
                // var health = h.GetComponent<EnemyHealth>();
                // if (health) health.TakeDamage(cfg.damageOverride >= 0 ? cfg.damageOverride : attackDamage);
                Destroy(h.gameObject);
            }
            yield return null;
        }

        // Fase 3: regreso al jugador
        while ((startCenter - cur).sqrMagnitude > 0.0004f)
        {
            // Posición objetivo de regreso puede ser dinámica por si el jugador se mueve
            startCenter = transform.TransformPoint(cfg.offset);
            float step = boomerangSpeed * Time.deltaTime;
            cur = Vector3.MoveTowards(cur, startCenter, step);
            boomerangCurCenter = cur;
            var hits = DetectHitsAt(cfg, cur, rotation);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (hitSet.Contains(h)) continue;
                hitSet.Add(h);
                // TODO: Reemplaza por tu sistema de daño
                // var health = h.GetComponent<EnemyHealth>();
                // if (health) health.TakeDamage(cfg.damageOverride >= 0 ? cfg.damageOverride : attackDamage);
                Destroy(h.gameObject);
            }
            yield return null;
        }

        // Finaliza estado runtime
        boomerangActive = false;
        boomerangCfgRuntime = null;
    }

    // Clonar configuración para uso en gizmos runtime
    HitboxConfig CloneConfig(HitboxConfig c)
    {
        return new HitboxConfig
        {
            offset = c.offset,
            euler = c.euler,
            shape = c.shape,
            boxSize = c.boxSize,
            boxScale = c.boxScale,
            sphereRadius = c.sphereRadius,
            capsuleHeight = c.capsuleHeight,
            capsuleRadius = c.capsuleRadius,
            sectorRadius = c.sectorRadius,
            sectorAngleDeg = c.sectorAngleDeg,
            sectorHeight = c.sectorHeight,
            sectorGizmoSteps = c.sectorGizmoSteps,
            damageOverride = c.damageOverride
        };
    }

    // Dirección del boomerang basada en coordenadas y posición de inicio
    Vector3 GetBoomerangDirectionFrom(Vector3 startCenter)
    {
        Vector3 dir;
        switch (boomerangAimMode)
        {
            case BoomerangAimMode.DirectionVector:
                dir = boomerangDirection; // mundo
                break;
            case BoomerangAimMode.WorldTarget:
                dir = boomerangTargetWorld - startCenter; // hacia el objetivo
                break;
            case BoomerangAimMode.Forward:
            default:
                dir = transform.forward;
                break;
        }
        // Mantener el movimiento en plano XZ a menos que se quiera parábola
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        return dir.normalized;
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
