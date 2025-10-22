using UnityEngine;
using UnityEngine.Events;
using Game.Combat;


public class EntityStats : MonoBehaviour, IDamageable // Use the interface to ensure it can take damage
{

    [Header("Stats")]
    [SerializeField] protected int maxHP = 100;

    [SerializeField] protected int curHP = 100;

    [SerializeField] protected float speed = 5f;


    public int CurrentHP => curHP;
    public int MaxHP
    {
        get => maxHP;
        set => maxHP = value;
    }
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    protected virtual void Awake()
    {
        curHP = maxHP; // Start with full health when created
    }

    public virtual void TakeDamage(DamageInfo damageInfo) // Method to take damage
    {
        curHP = Mathf.Max(0, curHP - Mathf.RoundToInt(damageInfo.finalDamage)); // Reduce health but not below 0

        if (curHP <= 0)
        {
            Die(damageInfo); // If health is 0 or less, die
        }
    }


    public virtual bool IsAlive() // Method to check if entity is alive
    {
        return curHP > 0;
    }


    public virtual void Die(DamageInfo finalDamage) // Method to handle death
    {
        Destroy(gameObject);
    }
}
