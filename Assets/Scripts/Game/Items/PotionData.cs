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
    public int effectAmount = 25;
    public int effectAmount2 = 25;
    public float duration = 0f; 
    public GameObject vfxPrefab;
}
