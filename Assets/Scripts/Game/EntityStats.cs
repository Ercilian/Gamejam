using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for all damageable entities.
/// Provides basic HP, speed and hooks for damage and death.
/// Inherit and override TakeDamage/Die when custom behaviour is needed.
/// </summary>
public class EntityStats : MonoBehaviour
{
    [Header("Stats")]

    [SerializeField] protected int maxHP = 100;

    [SerializeField] protected int curHP = 100;

    [SerializeField] protected float speed = 5f;


    [Header("Events")]

    public UnityEvent<int> onTakeDamage;

    public UnityEvent onDie;

    public int CurrentHP => curHP;

    public int MaxHP => maxHP;

    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    protected virtual void Reset()
    {
        // Initialize current HP to max on add/reset in editor
        curHP = maxHP;
    }

    protected virtual void Awake()
    {
        // Ensure curHP is within valid range at start
        curHP = Mathf.Clamp(curHP, 0, maxHP);
    }

    /// <summary>
    /// Apply damage to this entity. This method is virtual so derived classes
    /// can override to add armor, damage types, feedback, etc.
    /// Negative damage will heal the entity.
    /// </summary> 

    public virtual void TakeDamage(int amount)
    {
        if (amount == 0 || !IsAlive()) return;

        // If negative, treat as heal
        curHP = Mathf.Clamp(curHP - amount, 0, maxHP);

        // Fire damage event (listeners can react in inspector)
        onTakeDamage?.Invoke(amount);

        if (curHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Returns true if the entity has more than 0 HP.
    /// </summary>
    public virtual bool IsAlive()
    {
        return curHP > 0;
    }

    /// <summary>
    /// Called when the entity reaches 0 HP. Override to implement custom death behaviour
    /// (animations, dropping loot, disabling components). Base implementation invokes the
    /// onDie UnityEvent and disables the GameObject.
    /// </summary>
    public virtual void Die()
    {
        // Invoke listeners attached in inspector/scripts
        onDie?.Invoke();

        // Default behaviour: deactivate the GameObject
        // Derived classes can call base.Die() if they still want this behaviour.
        gameObject.SetActive(false);
    }
}
