using UnityEngine;
using UnityEngine.Events;
using Game.Combat;
using System.Collections;


public class EntityStats : MonoBehaviour // Use the interface to ensure it can take damage
{

    [Header("Stats")]
    [SerializeField] protected int maxHP = 100;
    [SerializeField] protected int curHP = 100;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected int curShield = 0;
    [SerializeField] protected int maxShield = 50;
    [SerializeField] protected int attackDamage = 20;
    [SerializeField] protected int defense = 10;

    [Header("Optional Stats Data")]
    [SerializeField] protected PlayerStatsData statsData; // Optional ScriptableObject for stats

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
    public int CurrentShield => curShield;
    public int MaxShield => maxShield;
    public int AttackDamage => attackDamage;
    public int Defense => defense;
    public PlayerStatsData StatsData => statsData;

    protected virtual void Awake()
    {
        ApplyStatsData(); // Apply ScriptableObject stats if available
        curHP = maxHP; // Start with full health when created
    }

    // Method to apply stats from ScriptableObject
    protected virtual void ApplyStatsData()
    {
        if (statsData != null)
        {
            maxHP = statsData.MaxHealth;
            speed = statsData.MoveSpeed;
            maxShield = statsData.MaxShield;
            attackDamage = statsData.AttackDamage;
            defense = statsData.Defense;

            // Apply other stats specific to Player if this is a Player
            if (this is Player player && player.TryGetComponent<PlayerInventory>(out var inventory))
            {
                // You can modify inventory capacity here if PlayerInventory has a setter
                // inventory.maxCarryCapacity = statsData.InventoryCapacity;
            }
        }
    }

   /* public virtual void TakeDamage(DamageInfo damageInfo) // Method to take damage
    {
        // Calculate damage reduction based on defense
        float damageReduction = defense * 0.5f; // Each defense point reduces 0.5 damage
        float finalDamage = Mathf.Max(1, damageInfo.finalDamage - damageReduction); // Minimum 1 damage
        
        curHP = Mathf.Max(0, curHP - Mathf.RoundToInt(finalDamage)); // Reduce health but not below 0

        if (curHP <= 0)
        {
            Die(damageInfo); // If health is 0 or less, die
        }
    }
    */


    public virtual bool IsAlive() // Method to check if entity is alive
    {
        return curHP > 0;
    }


   // public virtual void Die(DamageInfo finalDamage) // Method to handle death
  //  {
  //      Destroy(gameObject);
  //  }

    public void Heal(int amount)
    {
        curHP = Mathf.Min(curHP + amount, maxHP);
        // Actualiza la UI de vida si tienes
    }

    public void AddShield(int amount)
    {
        curShield += amount;
        // Actualiza la UI de escudo si tienes
    }

    public IEnumerator DamageBoost(int amount, float duration)
    {
        // Aplica el boost de daño aquí
        // Por ejemplo, damage += amount;
        yield return new WaitForSeconds(duration);
        // Revertir el boost de daño aquí
        // damage -= amount;
    }
}
