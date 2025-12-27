using UnityEngine;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "Game/Player Stats Data")]
public class PlayerStatsData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] string playerName;
    [TextArea(2, 4)]
    [SerializeField] string description;
    [SerializeField] Sprite playerIcon;

    [Header("Core Stats")]
    [SerializeField] int maxHealth = 100;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] int maxShield = 50;

    [Header("Combat Stats")]
    [SerializeField] int attackDamage = 20;
    [SerializeField] int defense = 10;

    [Header("Special Abilities")]
    [SerializeField] int inventoryCapacity = 3;
    [SerializeField] float pushStrength = 1f; // Multiplier for pushing objects
    
    [Header("Visual/Audio")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Color playerColor = Color.white;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip hurtSound;
    [SerializeField] AudioClip deathSound;

    [Header("Combo Sounds")]
    [Tooltip("Sonidos para cada paso del combo (índice 0 = primer golpe, 1 = segundo golpe, 2 = tercer golpe)")]
    [SerializeField] AudioClip[] comboSounds = new AudioClip[3];

    // Public getters
    public string PlayerName => playerName;
    public string Description => description;
    public Sprite PlayerIcon => playerIcon;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int MaxShield => maxShield;
    public int AttackDamage => attackDamage;
    public int Defense => defense;
    public int InventoryCapacity => inventoryCapacity;
    public float PushStrength => pushStrength;
    public GameObject PlayerPrefab => playerPrefab;
    public Color PlayerColor => playerColor;
    public AudioClip AttackSound => attackSound;
    public AudioClip HurtSound => hurtSound;
    public AudioClip DeathSound => deathSound;
    public AudioClip[] ComboSounds => comboSounds;

    // Método helper para obtener el sonido de un paso específico del combo
    public AudioClip GetComboSound(int comboStep)
    {
        if (comboSounds == null || comboStep < 0 || comboStep >= comboSounds.Length)
            return null;
        return comboSounds[comboStep];
    }
}