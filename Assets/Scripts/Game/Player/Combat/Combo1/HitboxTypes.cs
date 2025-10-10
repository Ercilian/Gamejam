using UnityEngine;

namespace Game.Combat
{
    public enum HitboxShape { Box, Sphere, Capsule, Sector }

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
}
