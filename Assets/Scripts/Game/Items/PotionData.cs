using UnityEngine;

public enum PotionEffectType
{
    Heal,
    Shield,
    DamageBoost
}

[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Potion")]
public class PotionData : ScriptableObject
{
    public string potionName;
    public Sprite icon;
    public PotionEffectType effectType;
    public int effectAmount = 25; // Vida, escudo o daño extra según el tipo
    public float duration = 0f;   // Para efectos temporales (como daño boost)
}
