using UnityEngine;

public enum PotionEffectType
{
    Heal,
    Shield,
    DamageBoost,
    None
}

[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Potion")]
public class PotionData : ScriptableObject
{
    public string potionName;
    public Sprite icon;
    public PotionEffectType effectType;
    public PotionEffectType effectType2;
    public int effectAmount = 25; // Vida, escudo o daño extra según el tipo
    public int effectAmount2 = 25; // Segundo efecto
    public float duration = 0f;   // Para efectos temporales (como daño boost)
}
