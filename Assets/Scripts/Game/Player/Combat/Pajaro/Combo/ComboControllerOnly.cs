using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Controlador mínimo de combos: gestiona la secuencia de ataques, cooldowns
    /// y el comportamiento de boomerang en el tercer paso.
    ///
    /// Este script NO aplica daño. En su lugar emite eventos que otros sistemas
    /// (por ejemplo DamageApplier) pueden suscribir para aplicar efectos/daño.
    /// </summary>
    public class ComboControllerOnly : MonoBehaviour
    {
        [Header("Combo")]
        [Tooltip("Cooldown entre ataques (segundos)")]
        public float attackCooldown = 0.3f;
        [Tooltip("Número de pasos del combo (ej: 3)")]
        public int comboCount = 3;
        [Tooltip("Tiempo tras el último ataque para resetear el combo (segundos)")]
        public float comboResetTime = 1f;

        [Header("Boomerang (tercer paso)")]
    public float boomerangDistance = 5f;
    public float boomerangTickDistance = 0.5f;
    public float boomerangTickInterval = 0.1f;
    public float boomerangPauseTime = 1f;
    [Tooltip("Offset local desde el transform para el origen del boomerang")]
    public Vector3 boomerangOffset = Vector3.zero;
    [Header("Detección simple")]
    [Tooltip("LayerMask que define qué colliders pueden ser detectados por la colisión simple")]
    public LayerMask enemyLayer;
    [Tooltip("Radio usado para OverlapSphere durante la trayectoria del boomerang. <=0 desactiva la detección automática.")]
    public float boomerangHitRadius = 0.5f;

        public enum AimMode { Forward, DirectionVector, WorldTarget }
        public AimMode boomerangAimMode = AimMode.Forward;
        public Vector3 boomerangDirectionWorld = Vector3.forward;
        public Vector3 boomerangTargetWorld = Vector3.zero;

        // Estado interno
        int currentStep = 0;
        float lastAttackTime = -999f;
        float lastComboTime = -999f;
        bool boomerangActive = false;

        // Eventos públicos para que otros sistemas se enganchen
        // OnAttackStep: invocado cuando se inicia un ataque; parámetro = índice del paso (0..comboCount-1)
        public event Action<int> OnAttackStep;
        // OnBoomerangStarted/Tick/Ended: invocados solo para el tercer paso (índice 2)
        public event Action OnBoomerangStarted;
        // pos, rot, comboStep
    public event Action<Vector3, Quaternion, int> OnBoomerangTick;
    public event Action OnBoomerangEnded;
    /// <summary>
    /// Evento que notifica colliders detectados por el boomerang (filtrados por enemyLayer).
    /// Parámetros: Collider detectado, comboStep.
    /// </summary>
    public event Action<Collider, int> OnBoomerangHit;

        void Update()
        {
            // Si el tiempo desde el último combo supera el reset, reiniciamos el paso
            if (currentStep > 0 && Time.time - lastComboTime > comboResetTime)
            {
                ResetCombo();
            }
        }

        /// <summary>
        /// Comprueba si se puede iniciar un nuevo ataque según el cooldown.
        /// </summary>
        public bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown;
        }

        /// <summary>
        /// Método público para iniciar un ataque (por ejemplo desde Input). Devuelve
        /// el índice del paso que se ha ejecutado (0..comboCount-1) o -1 si está en cooldown.
        /// </summary>
        public int StartAttack()
        {
            if (!CanAttack()) return -1;

            lastAttackTime = Time.time;
            lastComboTime = Time.time;

            int step = currentStep;
            currentStep = (currentStep + 1) % Math.Max(1, comboCount);

            // Emitir evento para que el sistema de efectos/animaciones/daño reaccione
            OnAttackStep?.Invoke(step);

            // Si es el tercer paso (índice comboCount-1) lanzamos boomerang
            if (step == comboCount - 1)
            {
                if (!boomerangActive)
                    StartCoroutine(DoBoomerang(step));
            }

            return step;
        }

        /// <summary>
        /// Forzar reset del combo (por ejemplo al ser interrumpido).
        /// </summary>
        public void ResetCombo()
        {
            currentStep = 0;
        }

        IEnumerator DoBoomerang(int comboStep)
        {
            boomerangActive = true;
            OnBoomerangStarted?.Invoke();

            Vector3 startCenter = transform.TransformPoint(boomerangOffset);

            // calcular dirección según modo
            Vector3 dir = GetBoomerangDirection(startCenter);
            Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 maxCenter = startCenter + dir * boomerangDistance;

            Vector3 cur = startCenter;
            // HashSet para evitar hits duplicados sobre el mismo collider durante esta ejecución
            HashSet<Collider> hitSet = new HashSet<Collider>();

            // Ida
            while ((maxCenter - cur).sqrMagnitude > 0.0004f)
            {
                cur = Vector3.MoveTowards(cur, maxCenter, boomerangTickDistance);
                OnBoomerangTick?.Invoke(cur, rotation, comboStep);
                // Detección simple por radio en la capa indicada
                if (boomerangHitRadius > 0f)
                {
                    var hits = Physics.OverlapSphere(cur, boomerangHitRadius, enemyLayer, QueryTriggerInteraction.Collide);
                    if (hits != null && hits.Length > 0)
                    {
                        foreach (var h in hits)
                        {
                            if (h == null) continue;
                            if (hitSet.Contains(h)) continue;
                            hitSet.Add(h);
                            OnBoomerangHit?.Invoke(h, comboStep);
                        }
                    }
                }
                yield return new WaitForSeconds(boomerangTickInterval);
            }

            // Pausa en punta
            float t = 0f;
            while (t < boomerangPauseTime)
            {
                OnBoomerangTick?.Invoke(cur, rotation, comboStep);
                if (boomerangHitRadius > 0f)
                {
                    var hits = Physics.OverlapSphere(cur, boomerangHitRadius, enemyLayer, QueryTriggerInteraction.Collide);
                    if (hits != null && hits.Length > 0)
                    {
                        foreach (var h in hits)
                        {
                            if (h == null) continue;
                            if (hitSet.Contains(h)) continue;
                            hitSet.Add(h);
                            OnBoomerangHit?.Invoke(h, comboStep);
                        }
                    }
                }
                yield return new WaitForSeconds(boomerangTickInterval);
                t += boomerangTickInterval;
            }

            // Vuelta
            while ((startCenter - cur).sqrMagnitude > 0.0004f)
            {
                startCenter = transform.TransformPoint(boomerangOffset);
                cur = Vector3.MoveTowards(cur, startCenter, boomerangTickDistance);
                OnBoomerangTick?.Invoke(cur, rotation, comboStep);
                if (boomerangHitRadius > 0f)
                {
                    var hits = Physics.OverlapSphere(cur, boomerangHitRadius, enemyLayer, QueryTriggerInteraction.Collide);
                    if (hits != null && hits.Length > 0)
                    {
                        foreach (var h in hits)
                        {
                            if (h == null) continue;
                            if (hitSet.Contains(h)) continue;
                            hitSet.Add(h);
                            OnBoomerangHit?.Invoke(h, comboStep);
                        }
                    }
                }
                yield return new WaitForSeconds(boomerangTickInterval);
            }

            boomerangActive = false;
            OnBoomerangEnded?.Invoke();
        }

        Vector3 GetBoomerangDirection(Vector3 from)
        {
            Vector3 dir;
            switch (boomerangAimMode)
            {
                case AimMode.DirectionVector:
                    dir = boomerangDirectionWorld;
                    break;
                case AimMode.WorldTarget:
                    dir = boomerangTargetWorld - from;
                    break;
                case AimMode.Forward:
                default:
                    dir = transform.forward;
                    // mantener plano horizontal
                    break;
            }
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            return dir.normalized;
        }
    }
}
