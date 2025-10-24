using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Game.Combat;

public class MeleeBase : MonoBehaviour
{

    [Header("Ataque")]
    public float attackCooldown = 0.3f;
    public int comboCount = 3;
    public float comboResetTime = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;
    [Tooltip("Offset local de la hitbox respecto al centro del personaje (en espacio local)")]
    public Vector3 attackOffset = new Vector3(0, 0, 2f);

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
    public BoomerangAttack.AimMode boomerangAimMode = BoomerangAttack.AimMode.Forward;
    public Vector3 boomerangDirection = new Vector3(0, 0, 1);
    public Vector3 boomerangTargetWorld;

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

    private MeleeComboController combo;

    private AttackInputBridge inputBridge;

    // Estado del boomerang en runtime para gizmos
    private bool boomerangActive = false;
    private Vector3 boomerangCurCenter;
    private Quaternion boomerangRotation;
    private HitboxConfig boomerangCfgRuntime;

    private bool isBoomerangActive = false; // Nueva variable de estado

    void Awake()
    {
        combo = GetComponent<MeleeComboController>();
        if (combo == null) combo = gameObject.AddComponent<MeleeComboController>();
        combo.Configure(attackCooldown, comboCount, comboResetTime);
        inputBridge = GetComponent<AttackInputBridge>();
        if (inputBridge == null) inputBridge = gameObject.AddComponent<AttackInputBridge>();
    }

    void OnEnable()
    {
        if (inputBridge != null) inputBridge.OnAttack += OnAttackPerformed;
    }

    void OnDisable()
    {
        if (inputBridge != null) inputBridge.OnAttack -= OnAttackPerformed;
    }

    private void OnAttackPerformed()
    {
        TryAttack();
    }

    void Update()
    {
        combo?.Tick();
    }

    void TryAttack()
    {
        // Prevent attack if player has a collectible in hands
        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null && inventory.HasItems())
        {
            Debug.Log("No puedes atacar mientras llevas un coleccionable.");
            return;
        }

        // ===== PREVENIR ATAQUES SI BOOMERANG ESTÁ ACTIVO =====
        if (isBoomerangActive)
        {
            return;
        }
        
        int step = combo.StartAttackAndGetStep();
        if (step < 0) return; // aún en cooldown

        HitboxConfig cfg = GetHitboxConfig(step);

        // Tercer ataque: boomerang
        if (step == 2)
        {
            StartCoroutine(DoBoomerangAttack(cfg));
            return;
        }

        // Ataque instantáneo
        var hits = HitboxDetector.Detect(cfg, transform, enemyLayer);
        int finalDamage = cfg.damageOverride >= 0 ? cfg.damageOverride : attackDamage;
        foreach (var h in hits)
        {
            DamageApplier.ApplyDamage(h, finalDamage, cfg, transform, step);
        }
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
            // Dirección del boomerang
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

        // Etiqueta del paso de combo
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"Combo Step: {gizmoPreviewStep}");
        #endif

        HitboxGizmos.Draw(transform, cfg, (Application.isPlaying && boomerangActive) ? (Vector3?)center : null, (Application.isPlaying && boomerangActive) ? (Quaternion?)rot : null);
    }

    HitboxConfig GetHitboxConfig(int comboIndex)
    {
        if (comboHitboxes != null && comboHitboxes.Count > 0)
        {
            int idx = Mathf.Clamp(comboIndex, 0, comboHitboxes.Count - 1);
            var cfg = comboHitboxes[idx];
            if (cfg != null) return cfg;
        }

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

    IEnumerator DoBoomerangAttack(HitboxConfig cfg)
    {
        // ===== MARCAR COMO ACTIVO =====
        isBoomerangActive = true;
        
        var boomer = gameObject.GetComponent<BoomerangAttack>();
        if (boomer == null) boomer = gameObject.AddComponent<BoomerangAttack>();
    boomer.distance = boomerangDistance;
    boomer.pauseTime = boomerangPauseTime;
    boomer.aimMode = boomerangAimMode;
    boomer.directionWorld = boomerangDirection;
    boomer.targetWorld = boomerangTargetWorld;
    // Adaptar velocidad clásica a ticks
    boomer.tickDistance = boomerangSpeed * boomer.tickInterval;

        boomerangActive = true;
        boomerangCfgRuntime = CloneConfig(cfg);

        var startCenter = transform.TransformPoint(cfg.offset);
        var forward = GetBoomerangDirectionFrom(startCenter);
        boomerangRotation = Quaternion.LookRotation(forward, Vector3.up);

        yield return StartCoroutine(boomer.Execute(
            cfg,
            transform,
            enemyLayer,
            OnBoomerangHit,
            (pos, rot) => { boomerangCurCenter = pos; boomerangRotation = rot; }
        ));

        // ===== DESMARCAR COMO ACTIVO =====
        isBoomerangActive = false;
        boomerangActive = false;
        boomerangCfgRuntime = null;
    }

    void OnBoomerangHit(Collider h)
    {
        if (h == null) return;
        int finalDamage = boomerangCfgRuntime != null && boomerangCfgRuntime.damageOverride >= 0 ? 
                         boomerangCfgRuntime.damageOverride : attackDamage;
        DamageApplier.ApplyDamage(h, finalDamage, boomerangCfgRuntime ?? DefaultConfig(), transform, 2); // Paso 2 = tercer ataque
    }

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
            damageOverride = c.damageOverride,
            damageMultiplier = c.damageMultiplier,
            damageType = c.damageType,
            effects = c.effects,
            knockbackForce = c.knockbackForce,
            knockbackDirection = c.knockbackDirection
        };
    }

    Vector3 GetBoomerangDirectionFrom(Vector3 startCenter)
    {
        Vector3 dir;
        switch (boomerangAimMode)
        {
            case BoomerangAttack.AimMode.DirectionVector:
                dir = boomerangDirection; // mundo
                break;
            case BoomerangAttack.AimMode.WorldTarget:
                dir = boomerangTargetWorld - startCenter; // hacia el objetivo
                break;
            case BoomerangAttack.AimMode.Forward:
            default:
                dir = transform.forward;
                // ===== AÑADIR COMPENSACIÓN IGUAL QUE EN BoomerangAttack.cs =====
                dir = Quaternion.Euler(0, -90, 0) * dir;
                break;
        }
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) 
        {
            dir = transform.forward;
            // ===== COMPENSACIÓN TAMBIÉN EN EL FALLBACK =====
            dir = Quaternion.Euler(0, -90, 0) * dir;
        }
        return dir.normalized;
    }
}
