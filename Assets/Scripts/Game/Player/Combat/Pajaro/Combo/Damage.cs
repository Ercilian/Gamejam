using UnityEngine;

namespace Game.Combat
{
    public enum DamageType { Normal, Crítico, Fuego, Hielo, Eléctrico, Perforante }
    [System.Flags]
    public enum DamageEffects { None = 0, Knockback = 1, Stun = 2, Burn = 4, Freeze = 8 }

    [System.Serializable]
    public struct DamageInfo
    {
        public int baseDamage;
        public float finalDamage;
        public DamageType damageType;
        public DamageEffects effects;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public float knockbackForce;
        public Vector3 knockbackDirection;
        public Transform attacker;
        public int comboStep;

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
