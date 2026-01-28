using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Stats/EnemyStats")]
public class EnemyStatsData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] string enemyName;
    [TextArea(2, 4)]
    [SerializeField] string description;
    [SerializeField] Sprite enemyIcon;

    [Header("Core Stats")]
    [SerializeField] int maxHealth = 100;
    [SerializeField] float velocidadBase = 2f; // Velocidad normal hacia el camión
    [SerializeField] float velocidadPersecucion = 5f; // Velocidad al perseguir jugador

    [SerializeField] int maxShield = 50;
    [SerializeField] float rangoDeteccion = 10f;
    [SerializeField] float rangoPerdida = 12f;

    [Header("Combat Stats")]
    [SerializeField] int attackDamage = 15;
    [SerializeField] int defense = 5;
    [SerializeField] float maxKnockback = 1f;
    [SerializeField] float maxStunDuration = 2f;

    [Header("Visual/Audio")]
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] Color enemyColor = Color.red;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip hurtSound;
    [SerializeField] AudioClip deathSound;

    // Public getters
    public string EnemyName => enemyName;
    public string Description => description;
    public Sprite EnemyIcon => enemyIcon;
    public int MaxHealth => maxHealth;
    public float VelocidadBase => velocidadBase;
    public float VelocidadPersecucion => velocidadPersecucion;
    public int MaxShield => maxShield;
    public int AttackDamage => attackDamage;
    public int Defense => defense;
    public GameObject EnemyPrefab => enemyPrefab;
    public Color EnemyColor => enemyColor;
    public AudioClip AttackSound => attackSound;
    public AudioClip HurtSound => hurtSound;
    public AudioClip DeathSound => deathSound;
    public float RangoDeteccion => rangoDeteccion;
    public float RangoPerdida => rangoPerdida;
    public float MaxKnockback => maxKnockback;
    public float MaxStunDuration => maxStunDuration;
}