using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.Combat
{
    public class BoomerangAttack : MonoBehaviour
    {
        public enum AimMode { Forward, DirectionVector, WorldTarget }

        public float distance = 5f;
        public float speed = 10f;
        public float pauseTime = 1f;
        public AimMode aimMode = AimMode.Forward;
        public Vector3 directionWorld = Vector3.forward;
        public Vector3 targetWorld = Vector3.zero;

        public IEnumerator Execute(
            HitboxConfig cfg,
            Transform origin,
            LayerMask enemyLayer,
            System.Action<Collider> onHit,
            System.Action<Vector3, Quaternion> onTick = null)
        {
            Vector3 startCenter = origin.TransformPoint(cfg.offset);
            
            HashSet<Collider> hitSet = new HashSet<Collider>();

            // Out
            Vector3 cur = startCenter;
            

            
            // ===== CALCULAR DIRECCIÓN JUSTO AHORA (FRAME ACTUAL) =====
            Vector3 dir = GetDirection(origin, startCenter);
            Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 maxCenter = startCenter + dir * distance;
            
            while ((maxCenter - cur).sqrMagnitude > 0.0004f)
            {

                
                float step = speed * Time.deltaTime;
                cur = Vector3.MoveTowards(cur, maxCenter, step);
                var hits = HitboxDetector.DetectAt(cfg, cur, rotation, enemyLayer);
                foreach (var h in hits)
                {
                    if (h == null || hitSet.Contains(h)) continue;
                    hitSet.Add(h);
                    onHit?.Invoke(h);
                }
                onTick?.Invoke(cur, rotation);
                yield return null;
            }

            // Pause
            float t = 0f;
            while (t < pauseTime)
            {
                t += Time.deltaTime;
                var hits = HitboxDetector.DetectAt(cfg, cur, rotation, enemyLayer);
                foreach (var h in hits)
                {
                    if (h == null || hitSet.Contains(h)) continue;
                    hitSet.Add(h);
                    onHit?.Invoke(h);
                }
                onTick?.Invoke(cur, rotation);
                yield return null;
            }

            // Back
            while ((startCenter - cur).sqrMagnitude > 0.0004f)
            {
                startCenter = origin.TransformPoint(cfg.offset);
                float step = speed * Time.deltaTime;
                cur = Vector3.MoveTowards(cur, startCenter, step);
                var hits = HitboxDetector.DetectAt(cfg, cur, rotation, enemyLayer);
                foreach (var h in hits)
                {
                    if (h == null || hitSet.Contains(h)) continue;
                    hitSet.Add(h);
                    onHit?.Invoke(h);
                }
                onTick?.Invoke(cur, rotation);
                yield return null;
            }
        }

        Vector3 GetDirection(Transform origin, Vector3 from)
        {
            Vector3 dir;
            switch (aimMode)
            {
                case AimMode.DirectionVector:
                    dir = directionWorld; 
                    break;
                case AimMode.WorldTarget:
                    dir = targetWorld - from; 
                    break;
                case AimMode.Forward:
                default:
                    // USAR LA DIRECCIÓN ACTUAL DEL PERSONAJE EN ESTE MOMENTO
                    dir = origin.forward; // Esto se calcula AHORA, no cuando se presionó el botón
                    // Compensar la rotación del personaje
                    dir = Quaternion.Euler(0, -90, 0) * dir;
                    break;
            }
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) 
            {
                // Fallback también usa dirección actual
                dir = Quaternion.Euler(0, -90, 0) * origin.forward;
            }
            return dir.normalized;
        }

    }
}
