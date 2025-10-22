using UnityEngine;
using Game.Combat;

public class HealthCar : EntityStats
{

    public override void Die(DamageInfo finalDamage)
    {
        base.Die(finalDamage);
        // Aquí puedes añadir explosión, game over, etc.
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        // Feedback visual, sonido, etc.
    }
}
