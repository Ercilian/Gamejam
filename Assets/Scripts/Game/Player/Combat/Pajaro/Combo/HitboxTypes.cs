using UnityEngine;

namespace Game.Combat
{
    public enum HitboxShape { Box, Sphere, Capsule, Sector }
    
    public enum DamageType 
    { 
        Normal,     // Daño normal
        Crítico,    // Daño crítico (más efectos visuales/sonoros)
        Fuego,      // Daño de fuego
        Hielo,      // Daño de hielo
        Eléctrico,  // Daño eléctrico
        Perforante  // Daño que atraviesa armadura
    }
    
    [System.Flags]
    public enum DamageEffects
    {
        None = 0,
        Knockback = 1 << 0,      // Empuja al enemigo
        Stun = 1 << 1,           // Aturde temporalmente
        Burn = 1 << 2,           // Daño por tiempo (fuego)
        Freeze = 1 << 3,         // Ralentiza movimiento
        Electrify = 1 << 4,      // Paraliza brevemente
        ArmorBreak = 1 << 5      // Reduce defensa
    }

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
        
        [Header("Configuración de Daño Avanzada")]
        [Tooltip("Multiplicador de daño para este golpe (ej: 1.0 = daño normal, 1.5 = 50% más daño)")]
        public float damageMultiplier = 1.0f;
        [Tooltip("Tipo de daño para efectos especiales (Normal, Crítico, Elemento, etc.)")]
        public DamageType damageType = DamageType.Normal;
        [Tooltip("Efectos especiales que aplica este golpe")]
        public DamageEffects effects = DamageEffects.None;
        [Tooltip("Fuerza del knockback aplicado")]
        public float knockbackForce = 5f;
        [Tooltip("Dirección del knockback (Vector3.zero usa la dirección del golpe)")]
        public Vector3 knockbackDirection = Vector3.zero;
    }
}
