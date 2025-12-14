using UnityEngine;
using UnityEngine.Events;
using Game.Combat;
using System.Collections;


public class EntityStats : MonoBehaviour // Use the interface to ensure it can take damage
{
    // Protected fields for stats
    public int curHP;
    public int maxHP = 100;
    public float speed = 5f;
    public int curShield;
    public int maxShield;
    public int attackDamage = 10;
    protected int defense;

    protected int finalDamage;
    [Header("Optional Stats Data")]
    [SerializeField] protected PlayerStatsData statsData; // Optional ScriptableObject for stats
    [SerializeField] protected EnemyStatsData enemyStatsData; // Optional ScriptableObject for enemy stats
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
        curHP = maxHP; // Start with full health when created (after applying stats)
        curShield = maxShield; // Start with full shield when created
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
            
            Debug.Log($"[{gameObject.name}] Aplicando stats de{statsData.name}: maxHP={maxHP}, speed={speed}, attackDamage={attackDamage}");

            // Apply other stats specific to Player if this is a Player
            if (this is Player player && player.TryGetComponent<PlayerInventory>(out var inventory))
            {
                // You can modify inventory capacity here if PlayerInventory has a setter
                // inventory.maxCarryCapacity = statsData.InventoryCapacity;
            }
        }
        else if (enemyStatsData != null)
        {
            maxHP = enemyStatsData.MaxHealth;
            speed = enemyStatsData.MoveSpeed;
            maxShield = enemyStatsData.MaxShield;
            attackDamage = enemyStatsData.AttackDamage;
            defense = enemyStatsData.Defense;

            Debug.Log($"[{gameObject.name}] Aplicando stats desde Enemy ScriptableObject: {enemyStatsData.name}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No hay ScriptableObject asignado en Stats Data. Usando valores por defecto.");
        }
    }

    public void TakeDamage(int amount) // Method to take damage
    {
        if (amount <= 0) return;

        int remainingDamage = amount;

        // Primero aplica el daño al escudo si hay escudo disponible
        if (curShield > 0)
        {
            if (curShield >= remainingDamage)
            {
                // El escudo absorbe todo el daño
                curShield -= remainingDamage;
                remainingDamage = 0;
                Debug.Log($"[{gameObject.name}] Escudo absorbió {amount} de daño. Escudo restante: {curShield}");
            }
            else
            {
                // El escudo absorbe parte del daño y se rompe
                remainingDamage -= curShield;
                Debug.Log($"[{gameObject.name}] Escudo roto! Absorbió {curShield} de daño. Daño restante: {remainingDamage}");
                curShield = 0;
            }
        }

        // Si queda daño después del escudo, aplícalo a la vida
        if (remainingDamage > 0)
        {
            curHP -= remainingDamage;
            Debug.Log($"[{gameObject.name}] Recibió {remainingDamage} de daño a la vida. HP restante: {curHP}");
        }

        IsAlive();
    }
    


    public virtual bool IsAlive() // Method to check if entity is alive
    {
        if (curHP <= 0)
        {
            curHP = 0;
            OnEntityDeath(); // Llamar a método virtual para que las clases hijas puedan sobrescribir
            return false; // Entity is dead
        }
        return true;
         // Entity is alive
         
    }

    /// <summary>
    /// Llamado cuando la entidad muere. Las clases hijas pueden sobrescribir esto.
    /// </summary>
    protected virtual void OnEntityDeath()
    {
        // Comportamiento por defecto: destruir el GameObject
        Destroy(gameObject);
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
