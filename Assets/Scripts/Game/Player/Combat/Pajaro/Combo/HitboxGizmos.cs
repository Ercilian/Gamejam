using UnityEngine;

namespace Game.Combat
{
    public static class HitboxGizmos
    {
        public static void Draw(Transform origin, HitboxConfig cfg, Vector3? overrideCenter = null, Quaternion? overrideRotation = null)
        {
            Vector3 center = overrideCenter ?? origin.TransformPoint(cfg.offset);
            Quaternion rot = overrideRotation ?? (origin.rotation * Quaternion.Euler(cfg.euler));

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

        public static void DrawWireCapsule(Vector3 center, Quaternion rot, float height, float radius)
        {
            Vector3 up = rot * Vector3.up;
            float half = Mathf.Max(0f, (height * 0.5f) - radius);
            Vector3 p0 = center + up * half;
            Vector3 p1 = center - up * half;
            Gizmos.DrawWireSphere(p0, radius);
            Gizmos.DrawWireSphere(p1, radius);
            Vector3 right = rot * Vector3.right;
            Vector3 fwd = rot * Vector3.forward;
            Gizmos.DrawLine(p0 + right * radius, p1 + right * radius);
            Gizmos.DrawLine(p0 - right * radius, p1 - right * radius);
            Gizmos.DrawLine(p0 + fwd * radius, p1 + fwd * radius);
            Gizmos.DrawLine(p0 - fwd * radius, p1 - fwd * radius);
        }

        public static void DrawWireSector(Vector3 center, Quaternion rot, float radius, float angleDeg, float height, int steps)
        {
            Vector3 fwd = rot * Vector3.forward;
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
            Vector3 edgeL = center + Quaternion.AngleAxis(-half, Vector3.up) * fwd * radius;
            Vector3 edgeR = center + Quaternion.AngleAxis(half, Vector3.up) * fwd * radius;
            Gizmos.DrawLine(center, edgeL);
            Gizmos.DrawLine(center, edgeR);

            Vector3 up = Vector3.up * (height * 0.5f);
            Gizmos.DrawLine(center - up, center + up);
        }
    }
}
