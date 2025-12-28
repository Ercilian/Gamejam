using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Combat
{
    /// <summary>
    /// Reusable combo + per-step hitbox controller.
    /// Attach to any character to configure a combo made of N steps.
    /// For each step you can configure a hitbox (box or sphere), a detection window
    /// and whether the step is a boomerang (uses boomerang settings).
    ///
    /// This script only handles timing and detection. It does NOT apply damage.
    /// It exposes events that you should subscribe to in order to apply damage,
    /// play VFX, or spawn hitbox prefabs.
    /// </summary>
    [ExecuteAlways]
    public class ComboHitboxController : MonoBehaviour
    {
        [Header("Combo Step Multipliers")]
        [Tooltip("Multiplicador de daño por cada paso del combo. Index 0 = paso 1, etc.")]
        public List<float> stepMultipliers = new List<float>() { 1f, 1.5f, 2f };
            // Referencia al EntityStats del atacante
            private EntityStats attackerStats;
        [Header("Combo timing")]
        [Tooltip("Cooldown between attacks (seconds)")]
        public float attackCooldown = 0.25f;
        [Tooltip("Time after the last attack to reset the combo")]
        public float comboResetTime = 1f;

        [Header("Steps")]
        [Tooltip("Per-step configuration. Index 0 = step 1, index 1 = step 2, ...")]
        public List<StepConfig> steps = new List<StepConfig>() { new StepConfig(), new StepConfig(), new StepConfig() };

        [Header("Global hit filtering")]
        [Tooltip("Which layers are considered hittable by step hitboxes and boomerang")]
        public LayerMask enemyLayer;

        [Header("Boomerang defaults")]
        [Tooltip("Distance the boomerang travels from the origin")]
        public float boomerangDistance = 5f;
        [Tooltip("Distance advanced per tick while the boomerang moves")]
        public float boomerangTickDistance = 0.5f;
        [Tooltip("Seconds between boomerang ticks")]
        public float boomerangTickInterval = 0.08f;
        [Tooltip("Pause time at the boomerang apex")]
        public float boomerangPauseTime = 0.6f;

        [Header("Debug / Visualization")]
        public bool drawGizmos = true;
    [Tooltip("When true, show only the selected preview step in the editor Gizmos")]
    public bool previewOnly = false;
    [Tooltip("Index of the step to preview in the inspector (clamped to steps count)")]
    public int previewStep = 0;
    [Tooltip("When playing, draw the active combo step hitbox instead of the preview (if true)")]
    public bool previewInPlayMode = true;
    [Tooltip("Enable runtime debug logs for combo/hitbox events")]
    public bool showDebugLogs = false;

        [Header("Input")]
        [Tooltip("Optional: InputActionReference for the Attack action. If empty, the script will try to find a PlayerInput and use actions['Attack'].")]
        public InputActionReference attackActionRef;
        
        [Header("Animators")]
        [Tooltip("Referencia opcional al arma con su propio Animator. Si no se asigna, solo se usará el animator del personaje.")]
        public Animator weaponAnimator;

        // runtime
        PlayerInput _playerInput;
        InputAction _attackAction;
        Animator _animator; // Animator del personaje
        Animator _weaponAnimator; // Animator del arma
        private AudioSource audioSource;
        [SerializeField] public PlayerStatsData playerStatsData;


        // Public events
        public event Action<int> OnAttackStep; // invoked when a step begins (step index)
        public event Action<Collider, int> OnStepHit; // invoked when a collider is detected for a step
        public event Action OnBoomerangStarted;
        public event Action<Vector3, Quaternion, int> OnBoomerangTick; // pos, rot, step
        public event Action OnBoomerangEnded;

        // Internal state
    // use -1 to represent idle (no step selected). This lets the first click
    // advance to step 0 predictably when StartAttack increments before use.
    int currentStep = -1;
        float lastAttackTime = -999f;
        float lastComboTime = -999f;
        bool boomerangActive = false;

        // track running coroutines per step so we can cancel duplicates
        Dictionary<int, Coroutine> runningStepCoroutines = new Dictionary<int, Coroutine>();
        Coroutine boomerangCoroutine = null;
    // runtime preview of boomerang tick position (used for runtime Gizmo drawing)
    Vector3 boomerangPreviewPos = Vector3.zero;
    Quaternion boomerangPreviewRot = Quaternion.identity;
    int boomerangPreviewStep = -1;
    bool boomerangPreviewActive = false;

        [Serializable]
        public class StepConfig
        {
            [Header("Animation")]
            [Tooltip("Nombre del trigger/parámetro de animación para este paso del combo")]
            public string animationTrigger = "";
            
            [Header("Detection Settings")]
            [Tooltip("Si está activo, este paso realiza comprobaciones por ticks durante la ventana; si es false se detecta solo una vez cuando se activa el paso.")]
            public bool useTicks = true;
            [Tooltip("If true, this step uses the boomerang behaviour instead of a single hitbox")]
            public bool isBoomerang = false;
            
            [Header("Hitbox Shape")]
            [Tooltip("Shape used for detection (Box or Sphere)")]
            public Shape shape = Shape.Box;
            [Tooltip("Local offset from the transform to place the detection center")]
            public Vector3 offset = new Vector3(0f, 0f, 1.2f);
            [Tooltip("Local Euler rotation (degrees) applied to the hitbox around the character. Useful to rotate boxes on a specific axis.")]
            public Vector3 localEuler = Vector3.zero;
            [Tooltip("Box size (only used if shape == Box)")]
            public Vector3 boxSize = new Vector3(1f, 1f, 1f);
            [Tooltip("Sphere radius (only used if shape == Sphere)")]
            public float sphereRadius = 1f;
            [Tooltip("Sector radius (only used if shape == Sector)")]
            public float sectorRadius = 1f;
            [Tooltip("Sector angle in degrees (only used if shape == Sector)")]
            public float sectorAngle = 90f;
            
            [Header("Timing")]
            [Tooltip("Duration in seconds of the detection window. Only used if UseTicks = true.")]
            public float windowDuration = 0.08f;
            [Tooltip("Interval between checks while the window is open (seconds). Only used if UseTicks = true.")]
            public float tickInterval = 0.04f;
        }

    public enum Shape { Box, Sphere, Sector }

        void Awake()
                    
        {
            // ensure PlayerInput and Attack action are set up
            _playerInput = GetComponent<PlayerInput>();
            attackerStats = GetComponent<EntityStats>();
            audioSource = GetComponent<AudioSource>();
            
            // Obtener el Animator del personaje (primero busca en este GameObject, luego en hijos)
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
                if (_animator != null && showDebugLogs)
                    Debug.Log($"[Combo] Animator de personaje encontrado en hijo: {_animator.gameObject.name}");
            }
            
            if (_animator == null)
            {
                Debug.LogWarning($"[Combo] No se encontró Animator del personaje en {gameObject.name} ni en sus hijos");
            }
            
            // Asignar el animator del arma desde el inspector (si fue configurado)
            _weaponAnimator = weaponAnimator;
            if (_weaponAnimator != null && showDebugLogs)
            {
                Debug.Log($"[Combo] Animator de arma asignado: {_weaponAnimator.gameObject.name}");
            }
        
        }

        void Update()
        {
            // reset combo to idle (-1) if idle time exceeded
            if (currentStep >= 0 && Time.time - lastComboTime > comboResetTime)
            {
                currentStep = -1;
            }
        }

        public bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown && !boomerangActive;
        }

        void OnEnable()
                    // Suscribirse al evento de golpe para aplicar daño

        {
            OnStepHit += ApplyDamageToTarget;
            // try to get PlayerInput from same GameObject
            _playerInput = GetComponent<PlayerInput>();
            if (attackActionRef != null)
                _attackAction = attackActionRef.action;
            else if (_playerInput != null && _playerInput.actions != null)
            {
                try { _attackAction = _playerInput.actions["Attack"]; } catch { _attackAction = null; }
            }

            if (_attackAction != null)
            {
                _attackAction.performed += OnAttackPerformed;
                if (!_attackAction.enabled) _attackAction.Enable();
            }
        }

        void OnDisable()
        {
            OnStepHit -= ApplyDamageToTarget;
        }
            // Desuscribirse del evento


        private void ApplyDamageToTarget(Collider target, int step)
        {
            if (target == null) return;
            
            var stats = target.GetComponent<EntityStats>();
            if (stats != null && attackerStats != null)
            {
                float multiplier = 1f;
                if (stepMultipliers != null && step >= 0 && step < stepMultipliers.Count)
                    multiplier = stepMultipliers[step];
                int damage = Mathf.RoundToInt(attackerStats.AttackDamage * multiplier);
                stats.TakeDamage(damage);
                if (showDebugLogs)
                    Debug.Log($"[Combo] {target.name} recibió {damage} de daño por combo step {step} (multiplier={multiplier})");
            }
        }
        

        void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            // Simply invoke StartAttack - StartAttack itself handles cooldowns and boomerang state.
            StartAttack();
        }

        /// <summary>
        /// Starts an attack. Returns the step index triggered or -1 if on cooldown.
        /// </summary>
        public int StartAttack()
        {
            if (!CanAttack())
            {
                if (showDebugLogs) Debug.Log($"[Combo] StartAttack rejected: canAttack={CanAttack()} timeSinceLastAttack={Time.time - lastAttackTime} boomerangActive={boomerangActive}");
                return -1;
            }

            lastAttackTime = Time.time;
            lastComboTime = Time.time;

            // If idle (-1), advance to 0 on first press. Otherwise advance to next step.
            if (currentStep < 0)
                currentStep = 0;
            else
                currentStep = (currentStep + 1) % Math.Max(1, steps.Count);

            int step = currentStep;

            OnAttackStep?.Invoke(step);
            
            // Reproducir el sonido específico del paso del combo
            if (playerStatsData != null && audioSource != null)
            {
                AudioClip comboSound = playerStatsData.GetComboSound(step);
                if (comboSound != null)
                {
                    audioSource.PlayOneShot(comboSound);
                    if (showDebugLogs)
                        Debug.Log($"[Combo] Reproduciendo sonido para paso {step}");
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning($"[Combo] No hay sonido asignado para el paso {step}");
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[Combo] StartAttack triggered. returnedStep={step} newCurrentStep={currentStep} time={Time.time}");
                var cfgdbg = (step >= 0 && step < steps.Count) ? steps[step] : null;
                if (cfgdbg != null) Debug.Log($"[Combo] step {step} isBoomerang={cfgdbg.isBoomerang} shape={cfgdbg.shape}");
            }

            var cfg = (step >= 0 && step < steps.Count) ? steps[step] : null;
            if (cfg != null)
            {
                // Activar la animación para este paso del combo
                PlayStepAnimation(cfg);
            }
            
            if (cfg != null)
            {
                if (cfg.isBoomerang)
                {
                    if (!boomerangActive)
                    {
                        boomerangCoroutine = StartCoroutine(DoBoomerang(step, cfg));
                    }
                }
                else
                {
                    // detection for the step
                    ExecuteStepDetection(step, cfg);
                }
            }

            return step;
        }

        /// <summary>
        /// Force-reset the combo state.
        /// </summary>
        public void ResetCombo()
        {
            currentStep = -1;
            lastAttackTime = -999f;
            lastComboTime = -999f;
            // stop running step coroutines
            foreach (var kv in runningStepCoroutines)
            {
                if (kv.Value != null) StopCoroutine(kv.Value);
            }
            runningStepCoroutines.Clear();

            if (boomerangCoroutine != null)
            {
                StopCoroutine(boomerangCoroutine);
                boomerangCoroutine = null;
                boomerangActive = false;
            }

            if (showDebugLogs) Debug.Log("[Combo] ResetCombo called");
        }

        void ExecuteStepDetection(int step, StepConfig cfg)
        {
            // cancel existing coroutine for this step (if any)
            if (runningStepCoroutines.TryGetValue(step, out var running) && running != null)
            {
                StopCoroutine(running);
                runningStepCoroutines.Remove(step);
            }

            // If useTicks is true and windowDuration > 0, perform ticked detection; otherwise detect once
            if (cfg.useTicks && cfg.windowDuration > 0f)
            {
                var c = StartCoroutine(DetectWindow(cfg, step));
                runningStepCoroutines[step] = c;
            }
            else
            {
                DoDetectOnce(cfg, step);
            }
        }

        void DoDetectOnce(StepConfig cfg, int step)
        {
            // Rotate the offset around the character according to localEuler, so the hitbox
            // orbits the character rather than rotating around its own center.
            Vector3 rotatedOffset = transform.rotation * Quaternion.Euler(cfg.localEuler) * cfg.offset;
            Vector3 center = transform.position + rotatedOffset;
            Collider[] hits = null;
            if (cfg.shape == Shape.Box)
            {
                Vector3 half = cfg.boxSize * 0.5f;
                Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                hits = Physics.OverlapBox(center, half, rot, enemyLayer, QueryTriggerInteraction.Collide);
            }
            else if (cfg.shape == Shape.Sphere)
            {
                hits = Physics.OverlapSphere(center, cfg.sphereRadius, enemyLayer, QueryTriggerInteraction.Collide);
            }
            else // Sector
            {
                // get candidates in radius then filter by angle
                hits = Physics.OverlapSphere(center, cfg.sectorRadius, enemyLayer, QueryTriggerInteraction.Collide);
            }

            if (hits == null || hits.Length == 0) return;

            if (cfg.shape == Shape.Sector)
            {
                // compute forward direction taking into account localEuler
                Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                Vector3 forward = rot * Vector3.forward;
                float halfAngle = cfg.sectorAngle * 0.5f;
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    // use closest point to center for better accuracy
                    Vector3 point = h.ClosestPoint(center);
                    Vector3 to = point - center;
                    if (to.sqrMagnitude < 0.0001f) { OnStepHit?.Invoke(h, step); continue; }
                    float ang = Vector3.Angle(forward, to);
                    if (ang <= halfAngle)
                    {
                                OnStepHit?.Invoke(h, step);
                                if (showDebugLogs) Debug.Log($"[Combo] OnStepHit sector: step={step} hit={h.name}");
                    }
                }
            }
            else
            {
                foreach (var h in hits)
                {
                    if (h == null) continue;
                            OnStepHit?.Invoke(h, step);
                            if (showDebugLogs) Debug.Log($"[Combo] OnStepHit: step={step} hit={h.name}");
                }
            }
        }

        IEnumerator DetectWindow(StepConfig cfg, int step)
        {
            float elapsed = 0f;
            HashSet<Collider> hitSet = new HashSet<Collider>();
            while (elapsed < cfg.windowDuration)
            {
                Vector3 rotatedOffset = transform.rotation * Quaternion.Euler(cfg.localEuler) * cfg.offset;
                Vector3 center = transform.position + rotatedOffset;
                Collider[] hits = null;
                if (cfg.shape == Shape.Box)
                {
                    Vector3 half = cfg.boxSize * 0.5f;
                    Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                    hits = Physics.OverlapBox(center, half, rot, enemyLayer, QueryTriggerInteraction.Collide);
                }
                else if (cfg.shape == Shape.Sphere)
                {
                    hits = Physics.OverlapSphere(center, cfg.sphereRadius, enemyLayer, QueryTriggerInteraction.Collide);
                }
                else // Sector
                {
                    hits = Physics.OverlapSphere(center, cfg.sectorRadius, enemyLayer, QueryTriggerInteraction.Collide);
                }

                if (hits != null && hits.Length > 0)
                {
                    if (cfg.shape == Shape.Sector)
                    {
                        Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                        Vector3 forward = rot * Vector3.forward;
                        float halfAngle = cfg.sectorAngle * 0.5f;
                        foreach (var h in hits)
                        {
                            if (h == null) continue;
                            if (hitSet.Contains(h)) continue;
                            Vector3 point = h.ClosestPoint(center);
                            Vector3 to = point - center;
                            if (to.sqrMagnitude < 0.0001f)
                            {
                                hitSet.Add(h);
                                OnStepHit?.Invoke(h, step);
                                continue;
                            }
                            float ang = Vector3.Angle(forward, to);
                            if (ang <= halfAngle)
                            {
                                hitSet.Add(h);
                                OnStepHit?.Invoke(h, step);
                            }
                        }
                    }
                    else
                    {
                        foreach (var h in hits)
                        {
                            if (h == null) continue;
                            if (hitSet.Contains(h)) continue;
                            hitSet.Add(h);
                            OnStepHit?.Invoke(h, step);
                        }
                    }
                }
                // wait until next tick
                yield return new WaitForSeconds(Math.Max(0.001f, cfg.tickInterval));
                elapsed += cfg.tickInterval;
            }

            // remove the coroutine entry if it exists
            if (runningStepCoroutines.ContainsKey(step)) runningStepCoroutines.Remove(step);
        }

        IEnumerator DoBoomerang(int comboStep, StepConfig cfg)
        {
            boomerangActive = true;
            OnBoomerangStarted?.Invoke();

            // Respect per-step localEuler when placing and aiming the boomerang
            Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
            Vector3 startCenter = transform.position + (rot * cfg.offset);
            Vector3 dir = rot * Vector3.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Vector3 maxCenter = startCenter + dir.normalized * boomerangDistance;

            Vector3 cur = startCenter;
            HashSet<Collider> hitSet = new HashSet<Collider>();

            // Out
            while ((maxCenter - cur).sqrMagnitude > 0.0004f)
            {
                cur = Vector3.MoveTowards(cur, maxCenter, boomerangTickDistance);
                OnBoomerangTick?.Invoke(cur, rot, comboStep);
                // update preview state for runtime gizmo
                boomerangPreviewActive = true;
                boomerangPreviewPos = cur;
                boomerangPreviewRot = rot;
                boomerangPreviewStep = comboStep;
                // detect
                var hits = Physics.OverlapSphere(cur, Mathf.Max(0.01f, cfg.sphereRadius), enemyLayer, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    foreach (var h in hits)
                    {
                        if (h == null) continue;
                        if (hitSet.Contains(h)) continue;
                        hitSet.Add(h);
                        OnStepHit?.Invoke(h, comboStep);
                    }
                }

                yield return new WaitForSeconds(boomerangTickInterval);
            }

            // pause at apex
            float t = 0f;
            while (t < boomerangPauseTime)
            {
                OnBoomerangTick?.Invoke(cur, rot, comboStep);
                // update preview state for runtime gizmo
                boomerangPreviewActive = true;
                boomerangPreviewPos = cur;
                boomerangPreviewRot = rot;
                boomerangPreviewStep = comboStep;
                var hits = Physics.OverlapSphere(cur, Mathf.Max(0.01f, cfg.sphereRadius), enemyLayer, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    foreach (var h in hits)
                    {
                        if (h == null) continue;
                        if (hitSet.Contains(h)) continue;
                        hitSet.Add(h);
                        OnStepHit?.Invoke(h, comboStep);
                    }
                }
                yield return new WaitForSeconds(boomerangTickInterval);
                t += boomerangTickInterval;
            }

            // back: ahora solo termina cuando colisiona con el jugador
            bool boomerangReturned = false;
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            if (myColliders == null || myColliders.Length == 0 && showDebugLogs)
                Debug.LogWarning($"[Combo] No se encontró ningún collider en el jugador para la detección del boomerang.");
            while (!boomerangReturned)
            {
                startCenter = transform.TransformPoint(cfg.offset);
                cur = Vector3.MoveTowards(cur, startCenter, boomerangTickDistance);
                OnBoomerangTick?.Invoke(cur, rot, comboStep);
                // update preview state for runtime gizmo
                boomerangPreviewActive = true;
                boomerangPreviewPos = cur;
                boomerangPreviewRot = rot;
                boomerangPreviewStep = comboStep;
                var hits = Physics.OverlapSphere(cur, Mathf.Max(0.01f, cfg.sphereRadius), enemyLayer, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    foreach (var h in hits)
                    {
                        if (h == null) continue;
                        if (hitSet.Contains(h)) continue;
                        hitSet.Add(h);
                        OnStepHit?.Invoke(h, comboStep);
                    }
                }
                // Detectar colisión con cualquier collider del jugador (sin filtro de layer para asegurar detección)
                var playerHits = Physics.OverlapSphere(cur, Mathf.Max(0.2f, cfg.sphereRadius), ~0, QueryTriggerInteraction.Collide);
                foreach (var h in playerHits)
                {
                    foreach (var myCol in myColliders)
                    {
                        if (h == myCol)
                        {
                            boomerangReturned = true;
                            if (showDebugLogs) Debug.Log($"[Combo] Boomerang ha vuelto al jugador: {gameObject.name} en posición {cur}");
                            break;
                        }
                    }
                    if (boomerangReturned) break;
                }
                
                // Debug: mostrar distancia al jugador
                if (showDebugLogs && !boomerangReturned)
                {
                    float distToPlayer = Vector3.Distance(cur, transform.position);
                    if (distToPlayer < 1f)
                        Debug.Log($"[Combo] Boomerang cerca del jugador: distancia={distToPlayer:F3} radio={cfg.sphereRadius:F3}");
                }
                
                yield return new WaitForSeconds(boomerangTickInterval);
            }

            boomerangActive = false;
            boomerangCoroutine = null;
            // MANTENER gizmo activo hasta el final del frame
            StartCoroutine(DeactivateBoomerangPreviewNextFrame());
            // After boomerang completes, return to idle state until the player clicks again.
            currentStep = -1;
            OnBoomerangEnded?.Invoke();
        // Desactiva el gizmo de boomerang al final del frame para que se vea correctamente en runtime
        IEnumerator DeactivateBoomerangPreviewNextFrame()
        {
            yield return null;
            boomerangPreviewActive = false;
            boomerangPreviewStep = -1;
        }
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos || steps == null || steps.Count == 0) return;

            int stepToDraw = previewStep;
            if (Application.isPlaying && previewInPlayMode)
            {
                if (currentStep < 0)
                {
                    // if we're idle and previewOnly is false, don't draw any runtime step
                    if (!previewOnly) return;
                    stepToDraw = Mathf.Clamp(previewStep, 0, steps.Count - 1);
                }
                else
                {
                    stepToDraw = Mathf.Clamp(currentStep, 0, steps.Count - 1);
                }
            }
            else
            {
                stepToDraw = Mathf.Clamp(previewStep, 0, steps.Count - 1);
            }

            var cfg = steps[stepToDraw];
            Vector3 rotatedOffset = transform.rotation * Quaternion.Euler(cfg.localEuler) * cfg.offset;
            Vector3 center = transform.position + rotatedOffset;

            // Boomerang preview: start -> apex and tick markers
            if (cfg.isBoomerang)
            {
                Quaternion bRot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                Vector3 startCenter = transform.position + (bRot * cfg.offset);
                Vector3 dir = bRot * Vector3.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
                Vector3 maxCenter = startCenter + dir.normalized * boomerangDistance;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startCenter, maxCenter);

                // draw ticks along the path
                float tickStep = Mathf.Max(0.001f, boomerangTickDistance);
                Vector3 cur = startCenter;
                int safety = 0;
                while ((maxCenter - cur).sqrMagnitude > 0.0004f && safety < 1024)
                {
                    cur = Vector3.MoveTowards(cur, maxCenter, tickStep);
                    Gizmos.DrawWireSphere(cur, 0.06f);
                    safety++;
                }
                Gizmos.DrawWireSphere(maxCenter, 0.1f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(startCenter + Vector3.up * 0.2f, "Boomerang start");
                UnityEditor.Handles.Label(maxCenter + Vector3.up * 0.2f, "Boomerang apex");
#endif
            }

            Gizmos.color = Color.cyan;
            // If boomerang preview active and matches this step, draw moving hitbox instead
            if (Application.isPlaying && boomerangPreviewActive && boomerangPreviewStep == stepToDraw)
            {
                // draw preview sphere at boomerangPreviewPos using cfg.sphereRadius
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(boomerangPreviewPos, cfg.shape == Shape.Sector ? cfg.sectorRadius : Mathf.Max(0.01f, cfg.sphereRadius));
            }
            else if (cfg.shape == Shape.Box)
            {
                Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, cfg.boxSize);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (cfg.shape == Shape.Sphere)
            {
                Gizmos.DrawWireSphere(center, cfg.sphereRadius);
            }
            else if (cfg.shape == Shape.Sector)
            {
                // draw sector using Handles for the arc
                Quaternion rot = transform.rotation * Quaternion.Euler(cfg.localEuler);
                Vector3 forward = rot * Vector3.forward;
                float halfAngle = cfg.sectorAngle * 0.5f;

                // draw wire arc and radius lines
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.cyan;
                Vector3 startDir = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
                UnityEditor.Handles.DrawWireArc(center, Vector3.up, startDir, cfg.sectorAngle, cfg.sectorRadius);
                // radius lines
                Gizmos.DrawLine(center, center + Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * cfg.sectorRadius);
                Gizmos.DrawLine(center, center + Quaternion.AngleAxis(halfAngle, Vector3.up) * forward * cfg.sectorRadius);
#endif
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(center + Vector3.up * 0.2f, $"Step {stepToDraw}");
#endif
        }

        /// <summary>
        /// Reproduce la animación configurada para un paso del combo
        /// </summary>
        void PlayStepAnimation(StepConfig cfg)
        {
            if (string.IsNullOrEmpty(cfg.animationTrigger))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[Combo] No hay trigger de animación configurado para este paso");
                return;
            }

            // Activar animación del personaje
            if (_animator != null)
            {
                ActivateAnimatorTrigger(_animator, cfg.animationTrigger, "personaje");
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"[Combo] No hay Animator de personaje asignado en {gameObject.name}");
            }

            // Activar animación del arma (si existe)
            if (_weaponAnimator != null)
            {
                ActivateAnimatorTrigger(_weaponAnimator, cfg.animationTrigger, "arma");
            }
        }

        void ActivateAnimatorTrigger(Animator animator, string triggerName, string animatorType)
        {
            if (animator == null) return;

            // Verificar que el Animator está habilitado
            if (!animator.enabled)
            {
                
                return;
            }

            // Verificar que el Animator tiene un RuntimeAnimatorController
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[Combo] El Animator de {animatorType} no tiene un Controller asignado en {animator.gameObject.name}");
                return;
            }

            // Verificar si el trigger existe en el Animator
            bool triggerExists = false;
            foreach (var param in animator.parameters)
            {
                if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    triggerExists = true;
                    break;
                }
            }

            if (!triggerExists)
            {
                Debug.LogError($"[Combo] El trigger '{triggerName}' no existe en el Animator Controller de {animatorType} ({animator.gameObject.name})");
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[Combo] === ACTIVANDO TRIGGER EN {animatorType.ToUpper()} ===");
                Debug.Log($"[Combo] GameObject: {animator.gameObject.name}");
                Debug.Log($"[Combo] Controller: {animator.runtimeAnimatorController.name}");
                Debug.Log($"[Combo] Animator enabled: {animator.enabled}");
            }

            // Resetear el trigger primero por si quedó activo de antes
            animator.ResetTrigger(triggerName);
            
            // Activar el trigger de animación
            animator.SetTrigger(triggerName);
            
            // Forzar actualización del Animator para procesar el trigger inmediatamente
            animator.Update(0f);

            if (showDebugLogs)
            {
                Debug.Log($"[Combo] ✓ Trigger '{triggerName}' activado en {animatorType}");
                Debug.Log($"[Combo] ¿Está en transición?: {animator.IsInTransition(0)}");
                
                if (animator.IsInTransition(0))
                {
                    var nextState = animator.GetNextAnimatorStateInfo(0);
                    Debug.Log($"[Combo] ✓ ¡TRANSICIÓN INICIADA! Hash destino: {nextState.fullPathHash}");
                }
                else
                {
                    Debug.LogWarning($"[Combo] {animatorType} no está en transición aún");
                }
            }
        }

        void OnValidate()
        {
            // keep comboCount (implicit) in sync with steps list convenience
            if (steps != null && steps.Count > 0)
            {
                // nothing else needed here, but this ensures the list is visible/editable
            }
            // clamp previewStep
            if (steps == null || steps.Count == 0)
                previewStep = 0;
            else
                previewStep = Mathf.Clamp(previewStep, 0, steps.Count - 1);
        }

        [ContextMenu("Next Preview Step")]
        void NextPreviewStep()
        {
            if (steps == null || steps.Count == 0) return;
            previewStep = (previewStep + 1) % steps.Count;
        }

        [ContextMenu("Previous Preview Step")]
        void PrevPreviewStep()
        {
            if (steps == null || steps.Count == 0) return;
            previewStep = (previewStep - 1 + steps.Count) % steps.Count;
        }
    }
}
