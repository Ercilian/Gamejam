using UnityEngine;

namespace Game.Combat
{
    [System.Serializable]
    public struct DamageInfo
    {
        public int baseDamage;              // Daño base
        public float finalDamage;           // Daño final después de multiplicadores
        public DamageType damageType;       // Tipo de daño
        public DamageEffects effects;       // Efectos aplicados
        public Vector3 hitPoint;            // Punto de impacto
        public Vector3 hitDirection;        // Dirección del golpe
        public float knockbackForce;        // Fuerza del knockback
        public Vector3 knockbackDirection;  // Dirección del knockback
        public Transform attacker;          // Quien atacó
        public int comboStep;               // Paso del combo (0, 1, 2...)
        
        public static DamageInfo Create(int baseDamage, HitboxConfig config, Vector3 hitPoint, 
                                      Vector3 hitDirection, Transform attacker, int comboStep)
        {
            return new DamageInfo
            {
                baseDamage = baseDamage,
                finalDamage = baseDamage * config.damageMultiplier,
                damageType = config.damageType,
                effects = config.effects,
                hitPoint = hitPoint,
                hitDirection = hitDirection,
                knockbackForce = config.knockbackForce,
                knockbackDirection = config.knockbackDirection == Vector3.zero ? hitDirection : config.knockbackDirection,
                attacker = attacker,
                comboStep = comboStep
            };
        }
    }
    
    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
        
        // Método de compatibilidad hacia atrás
        void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            DamageInfo info = new DamageInfo
            {
                baseDamage = amount,
                finalDamage = amount,
                damageType = DamageType.Normal,
                effects = DamageEffects.None,
                hitPoint = hitPoint,
                hitDirection = hitNormal,
                knockbackForce = 0f,
                knockbackDirection = Vector3.zero,
                attacker = null,
                comboStep = 0
            };
            TakeDamage(info);
        }
    }

    public static class DamageApplier
    {
        public static void ApplyDamage(Collider target, int baseDamage, HitboxConfig config, 
                                     Transform attacker, int comboStep)
        {
            if (target == null) return;
            
            var dmg = target.GetComponentInParent<IDamageable>() ?? target.GetComponent<IDamageable>() as IDamageable;
            if (dmg != null)
            {
                Vector3 hitPoint = target.bounds.center;
                Vector3 hitDirection = (target.transform.position - attacker.position).normalized;
                
                DamageInfo damageInfo = DamageInfo.Create(baseDamage, config, hitPoint, 
                                                        hitDirection, attacker, comboStep);
                dmg.TakeDamage(damageInfo);
            }
            else
            {
                // Fallback temporal: destruir si no implementa daño
                Debug.Log($"Destruyendo {target.name} - Daño: {baseDamage * config.damageMultiplier} (Combo paso {comboStep + 1})");
                Object.Destroy(target.gameObject);
            }
        }
        
        // Método de compatibilidad hacia atrás
        public static void ApplyDamage(Collider target, int amount)
        {
            if (target == null) return;
            var dmg = target.GetComponentInParent<IDamageable>() ?? target.GetComponent<IDamageable>() as IDamageable;
            if (dmg != null)
            {
                Vector3 point = target.bounds.center;
                dmg.TakeDamage(amount, point, Vector3.zero);
            }
            else
            {
                Debug.Log($"Destruyendo {target.name} - Daño: {amount}");
                Object.Destroy(target.gameObject);
            }
        }
    }
}
