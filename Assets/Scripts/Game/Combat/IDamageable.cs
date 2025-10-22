using UnityEngine;

namespace Game.Combat
{
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
}