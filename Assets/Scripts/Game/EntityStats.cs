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
    
    [Header("Animation")]
    [Tooltip("Nombres de los triggers de animación de daño. Se elegirá uno al azar.")]
    public string[] damageTriggerNames = new string[] { "Hit" };
    protected Animator animator;
    
    [Header("Audio")]
    [Tooltip("Sonidos de daño. Se reproducirá uno al azar cuando reciba daño.")]
    public AudioClip[] damageSounds;
    [Tooltip("Volumen de los sonidos de daño (0-1)")]
    [Range(0f, 1f)]
    public float damageVolume = 1f;
    protected AudioSource audioSource;
    
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
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // Si no hay AudioSource, agregar uno
        if (audioSource == null && damageSounds != null && damageSounds.Length > 0)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
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
            speed = enemyStatsData.VelocidadBase;
            maxShield = enemyStatsData.MaxShield;
            attackDamage = enemyStatsData.AttackDamage;
            defense = enemyStatsData.Defense;

        }

    }

    public virtual void TakeDamage(int amount) // Method to take damage
    {
        if (amount <= 0) return;

        // Check if this is a Player and if they are dashing (invulnerable)
        if (this is Player player)
        {
            Dash dashComponent = player.GetComponent<Dash>();
            if (dashComponent != null && dashComponent.IsDashing)
            {
                Debug.Log($"[{gameObject.name}] ¡Daño esquivado durante el dash!");
                return; // No damage during dash
            }
        }

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
            // Activar animación de daño
            PlayDamageAnimation();
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
    public virtual void OnEntityDeath()
    {
        // Comportamiento por defecto: destruir el GameObject
        Destroy(gameObject);
    }


   // public virtual void Die(DamageInfo finalDamage) // Method to handle death
  //  {
  //      Destroy(gameObject);
  //  }

    public virtual void Heal(int amount)
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
    
    /// <summary>
    /// Activa un trigger de animación de daño aleatorio si existe un animator
    /// y reproduce un sonido de daño aleatorio si existe
    /// </summary>
    protected virtual void PlayDamageAnimation()
    {
        // Reproducir animación
        if (animator != null && damageTriggerNames != null && damageTriggerNames.Length > 0)
        {
            // Elegir un trigger aleatorio del array
            string randomTrigger = damageTriggerNames[Random.Range(0, damageTriggerNames.Length)];
            
            if (!string.IsNullOrEmpty(randomTrigger))
            {
                animator.SetTrigger(randomTrigger);
            }
        }
        
        // Reproducir sonido
        if (audioSource != null && damageSounds != null && damageSounds.Length > 0)
        {
            // Elegir un sonido aleatorio del array
            AudioClip randomClip = damageSounds[Random.Range(0, damageSounds.Length)];
            
            if (randomClip != null)
            {
                audioSource.PlayOneShot(randomClip, damageVolume);
            }
        }
    }
}
