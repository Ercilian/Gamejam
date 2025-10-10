using UnityEngine;

namespace Game.Combat
{
    public interface IDamageable
    {
        void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
    }

    public static class DamageApplier
    {
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
                // Fallback temporal: destruir si no implementa daño
                Object.Destroy(target.gameObject);
            }
        }
    }
}
