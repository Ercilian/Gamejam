using UnityEngine;

[CreateAssetMenu(fileName = "New Collectible", menuName = "Game/Collectible Data")]
public class CollectibleData : ScriptableObject
// This ScriptableObject holds data for the collectible items in the game.
{
    [Header("Collectible Info")]
    public string itemName;
    public string itemID;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public ItemType type;
    public enum ItemType
    {
        Diesel,
        Scrap,
        PlantRed,
        PlantGreen,
        PlantBlue
    }
    
    [Header("Diesel Properties")]
    public int dieselValue;
    public float collectRange;
    public string collectPrompt = "Press E to collect diesel";

    [Header("Scrap Properties")]
    public int scrapValue;
    public float scrapCollectRange;
    public string scrapCollectPrompt = "Press E to collect scrap";

    [Header("Plant Properties")]
    public int plantValue;
    public float plantCollectRange;
    public string plantCollectPrompt = "Press E to collect plant";

    [Header("Audio")]
    public AudioClip collectSound;
    public AudioClip depositSound;
}