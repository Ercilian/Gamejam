using UnityEngine;
using Game.Combat;

public class Car : EntityStats
{

    public override void Die(DamageInfo finalDamage)
    {
        base.Die(finalDamage);
    }

    public override void TakeDamage(DamageInfo damageInfo )
    {
        base.TakeDamage(damageInfo);
    }
}
