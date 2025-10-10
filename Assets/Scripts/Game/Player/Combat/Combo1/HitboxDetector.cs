using UnityEngine;
using System.Collections.Generic;

namespace Game.Combat
{
    public static class HitboxDetector
    {
        public static Collider[] Detect(HitboxConfig cfg, Transform origin, LayerMask enemyLayer)
        {
            Vector3 center = origin.TransformPoint(cfg.offset);
            Quaternion rotation = origin.rotation * Quaternion.Euler(cfg.euler);
            return DetectAt(cfg, center, rotation, enemyLayer);
        }

        public static Collider[] DetectAt(HitboxConfig cfg, Vector3 center, Quaternion rotation, LayerMask enemyLayer)
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
                    return Physics.OverlapSphere(center, cfg.sphereRadius, enemyLayer, QueryTriggerInteraction.Collide);
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
    }
}
