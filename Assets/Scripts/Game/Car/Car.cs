using UnityEngine;
using Game.Combat;

public class Car : EntityStats
{
    private Options options;

    public override void OnEntityDeath()
    {
        options = FindObjectOfType<Options>();
        options.GameOver();
        base.OnEntityDeath();

    }
    /*
    public override void TakeDamage(DamageInfo damageInfo )
    {
        base.TakeDamage(damageInfo);
    }
    */
}
